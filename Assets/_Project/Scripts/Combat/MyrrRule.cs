using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // Rootmother Myrr, floor 3's own gate boss (docs/64; docs/44 section 1.8: the gardener who grafted lost futures onto
    // trees). Her fight keeps the Forge Warden's clock (WardenSlamClock: a beat every Every seconds after the last eruption,
    // EveryPhaseTwo below half her health, her weapon held from the mark until RecoverySeconds after the eruption) with a
    // mark of her own:
    // - THE ROOT SNARE ("Be still. Be planted."). As the beat's mark goes down at the hero's spot, a ring of SnareRadius
    //   (smaller than the Warden's 2.2) appears there and CREEPS after the hero at CreepSpeed, under half the hero's walk,
    //   through the Telegraph. When it fills, roots erupt in it: a hero inside takes SnareShare of its maximum health and is
    //   PLANTED for PlantSeconds - it cannot walk, though its weapons strike. A hero that steps out and stops is overtaken; a
    //   hero that keeps walking leaves it.
    // - HER FEET (the Glass Regent's lesson, docs/50 2026-11-17, built in from the start): she holds her Grafting Hook through
    //   the beat only for a hero beyond FeetReach of her as the mark goes down; a hero at her feet is struck through the
    //   cast. And a hero the roots caught is struck through the recovery too (HoldsHook): planted in her garden.
    // - PHASE TWO, THE GRAFTED SAPLINGS: at half health she plants Saplings saplings, and one more every SaplingEvery,
    //   each SaplingDistance to her side (SaplingSpot), rooted and harmless, SaplingShare of her maximum health. A sapling
    //   that stands SaplingGrow seconds grows into a Thornling (the Runner's body, docs/52 section 4, without its dive) that
    //   joins her fight; at most ThornlingsMost stand. When she falls, her garden withers.
    //
    // Floor 3's gate boss since 2026-11-25 (docs/64 section 15, the confirmation read of `mr:share=4.7:sap=0`; section 16,
    // the scene build): Floor_GloamGardens' gate at share 4.7, without her saplings (ShippedSaplings; the lead's answer 2),
    // the creeping snare, the 1.0 s plant with her hook free through it and the 1.2 s telegraph. The scene's
    // WardenSlamService steps a RootSnare and the foundry simulation (DescentSimulation.Myrr.cs) steps the same one at the
    // same point of its frame. Pure, and every product the scene and the simulation share is one body that is never
    // inlined, the squares cast to float (the stamina divergence's lesson, docs/21; PollenRule.Near's cast).
    public static class MyrrRule
    {
        public const float Every = WardenRule.SlamEvery;
        public const float EveryPhaseTwo = WardenRule.SlamEveryEnraged;
        // Inside the 0.8-1.2 s a phone telegraph is read in (WardenRule.Telegraph's note), its top: the ring moves.
        public const float Telegraph = 1.2f;
        public const float SnareRadius = 1.5f;
        public const float CreepSpeed = 1.5f;
        public const float SnareShare = .1f;
        public const float PlantSeconds = 1f;
        // The Regent's FeetReach (RegentRule.FeetReach): a fifth of a unit inside her hook's 1.7.
        public const float FeetReach = RegentRule.FeetReach;
        public const int Saplings = 2;
        public const float SaplingEvery = 8f;
        public const float SaplingGrow = 8f;
        public const float SaplingShare = .025f;
        public const float SaplingDistance = 3f;
        public const int ThornlingsMost = 3;
        public const int SaplingsMost = 4;
        // The saplings she plants at phase two as she ships: none (the lead's answer 2, docs/64 section 13; the confirmation
        // read `mr:share=4.7:sap=0`, section 15). Saplings stays docs/64 section 3's design, read by the research only.
        public const int ShippedSaplings = 0;
        // How far past her snare's edge the pilots read it as theirs to leave (the Warden's circle's margin, its 0.6): the
        // simulation's and DevelopmentAutopilot's escape from a snare of radius.
        public const float EscapeMargin = .6f;

        // When a hero the roots caught at now walks again: seconds later. One body for the scene's WardenSlamService (on
        // scaled time) and the simulation (on its run's seconds).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float PlantedUntil(float now, float seconds) => now + seconds;

        // Whether a hero planted until until stands planted at now: no walk.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsPlanted(float now, float until) => now < until;

        // How far from a snare of radius's centre the pilots step out of it: the radius and EscapeMargin.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float EscapeReach(float radius) => radius + EscapeMargin;

        // The ring's centre moved toward the hero at (heroX, heroY) by at most speed x deltaTime: (x, y) in, the new centre
        // out. It stops on the hero, never past.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Creep(ref float x, ref float y, float heroX, float heroY, float speed, float deltaTime)
        {
            float dx = heroX - x, dy = heroY - y;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            float step = speed * deltaTime;
            if (!(step > 0f) || distance <= 1e-6f)
                return;
            if (distance <= step)
            {
                x = heroX;
                y = heroY;
                return;
            }
            float share = step / distance;
            x += dx * share;
            y += dy * share;
        }

        // Whether the hero at (x, y) stands in a snare of radius centred at (snareX, snareY).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool InSnare(float x, float y, float snareX, float snareY, float radius) =>
            ArenaFloor.FloorDistanceSquared(x, y, snareX, snareY) <= (float)(radius * radius);

        // Whether a hero at (x, y) stands at Myrr's feet, she at (myrrX, myrrY): within reach of her.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AtHerFeet(float x, float y, float myrrX, float myrrY, float reach) =>
            (float)ArenaFloor.FloorDistanceSquared(x, y, myrrX, myrrY) <= (float)(reach * reach);

        // Whether Myrr holds her hook now: as the clock holds the Warden's hammer (from the mark until the recovery after the
        // eruption), but never for a hero at her feet as the beat's mark went down, nor through the recovery after roots
        // caught the hero (hookFree: either).
        public static bool HoldsHook(bool clockHolds, bool hookFree) => clockHolds && !hookFree;

        // The snare's damage: share of the hero's maximum health.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SnareDamage(float maximumHealth, float share) => maximumHealth * share;

        // Where a sapling roots: distance from Myrr at (myrrX, myrrY), square to the line from her to the hero at (heroX,
        // heroY) on side (+1 her left as she faces the hero, -1 her right). With the hero on her own spot, toward the floor's
        // side (x).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void SaplingSpot(float myrrX, float myrrY, float heroX, float heroY, float distance, int side,
            out float x, out float y)
        {
            float dx = heroX - myrrX, dy = heroY - myrrY;
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length > 1e-5f)
            {
                dx /= length;
                dy /= length;
            }
            else
            {
                dx = 0f;
                dy = -1f;
            }
            float s = side >= 0 ? 1f : -1f;
            x = myrrX - dy * s * distance;
            y = myrrY + dx * s * distance;
        }

        // Whether a sapling standing age seconds has grown into a Thornling.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Grown(float age, float grow) => age >= grow;

        // A sapling's health: share of Myrr's maximum, never under one.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SaplingHealth(float myrrMaximum, float share) => Math.Max(1f, myrrMaximum * share);
    }

    // One beat's root snare: down at the hero's spot as the mark goes down, creeping after the hero through the telegraph,
    // judged as the roots erupt. The scene's WardenSlamService and the simulation step the same one.
    public sealed class RootSnare
    {
        public RootSnare() : this(MyrrRule.SnareRadius, MyrrRule.CreepSpeed)
        {
        }

        public RootSnare(float radius, float creepSpeed)
        {
            if (!(radius > 0f) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (float.IsNaN(creepSpeed) || float.IsInfinity(creepSpeed) || creepSpeed < 0f)
                throw new ArgumentOutOfRangeException(nameof(creepSpeed));
            Radius = radius;
            CreepSpeed = creepSpeed;
        }

        public float Radius { get; }
        public float CreepSpeed { get; }
        public bool IsDown { get; private set; }
        // The ring's centre now, on the floor.
        public float X { get; private set; }
        public float Y { get; private set; }
        // How far it has crept since it went down.
        public float Crept { get; private set; }

        // The beat's mark: the ring down at (x, y), the hero's spot.
        public void Down(float x, float y)
        {
            IsDown = true;
            X = x;
            Y = y;
            Crept = 0f;
        }

        // One running frame of the telegraph: the ring after the hero at (heroX, heroY).
        public void Creep(float heroX, float heroY, float deltaTime)
        {
            if (!IsDown)
                return;
            float x = X, y = Y;
            MyrrRule.Creep(ref x, ref y, heroX, heroY, CreepSpeed, deltaTime);
            float dx = x - X, dy = y - Y;
            Crept += (float)Math.Sqrt(dx * dx + dy * dy);
            X = x;
            Y = y;
        }

        // The roots erupt: whether they catch a hero at (x, y). The ring is gone.
        public bool Erupt(float x, float y)
        {
            bool caught = IsDown && MyrrRule.InSnare(x, y, X, Y, Radius);
            IsDown = false;
            return caught;
        }

        // The beat is withdrawn (the fight stopped): no ring.
        public void Clear() => IsDown = false;
    }
}
