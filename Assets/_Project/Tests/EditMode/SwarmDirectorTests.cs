using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The continuous swarm's rules (docs/27, S6c), each one a thing the scene and the simulation both rely on.
    public sealed class SwarmDirectorTests
    {
        private static readonly int[] Ladder = { 3, 3, 4, 5 };

        [Test]
        public void TheOpeningIsTheLadderFirstWaveWhole()
        {
            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);

            Assert.That(spawns.Count, Is.EqualTo(3));
            for (int i = 0; i < 3; i++)
            {
                Assert.That(spawns[i].Slot, Is.EqualTo(i));
                Assert.That((spawns[i].Wave, spawns[i].Index), Is.EqualTo((0, i)));
                Assert.That(spawns[i].Difficulty, Is.EqualTo(1f), "The room opens at the difficulty its floor authored.");
            }
            Assert.That(director.Slots, Is.EqualTo(3));
            Assert.That(director.Spawned, Is.EqualTo(3));
            Assert.Throws<InvalidOperationException>(() => director.Open(spawns), "A swarm opens once.");
        }

        [Test]
        public void AFallenPlaceWaitsItsBreathAndThenTakesItsRoleInTheWaveTheClockIsOn()
        {
            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);

            director.Fell(1);
            director.Tick(SwarmRule.RespawnSeconds * 0.5f, 2, spawns);
            Assert.That(spawns, Is.Empty, "Half a breath is not enough.");

            director.Tick(SwarmRule.RespawnSeconds * 0.5f, 2, spawns);
            Assert.That(spawns.Count, Is.EqualTo(1));
            Assert.That(spawns[0].Slot, Is.EqualTo(1), "The fallen place is the one that fills.");
            int wave = SwarmRule.LadderWaveAt(director.Seconds, Ladder.Length);
            Assert.That((spawns[0].Wave, spawns[0].Index), Is.EqualTo((wave, 1)),
                "Place one is the second enemy of whichever wave the clock has reached.");
        }

        [Test]
        public void TheCrowdHardensWithTheClockAndNotWithTheKilling()
        {
            // The ladder by the clock: one wave every LadderSeconds, the last for as long as the room lasts.
            float wave = SwarmRule.LadderSeconds;
            Assert.That(SwarmRule.LadderWaveAt(0f, 4), Is.Zero);
            Assert.That(SwarmRule.LadderWaveAt(wave * 0.99f, 4), Is.Zero, "The first wave lasts a whole wave.");
            Assert.That(SwarmRule.LadderWaveAt(wave * 2.5f, 4), Is.EqualTo(2));
            Assert.That(SwarmRule.LadderWaveAt(wave * 1000f, 4), Is.EqualTo(3), "Past the ladder its last wave holds.");
            Assert.Throws<ArgumentOutOfRangeException>(() => SwarmRule.LadderWaveAt(-1f, 4));
            Assert.Throws<ArgumentOutOfRangeException>(() => SwarmRule.LadderWaveAt(0f, 0));

            // Two rooms at the same minute, one whose hero has killed nothing and one whose hero has killed everything
            // the moment it could: the place that fills now takes the same enemy in both, because the minute decides it.
            var idle = new SwarmDirector(Ladder);
            var busy = new SwarmDirector(Ladder);
            var idleSpawns = new List<SwarmDirector.Spawn>();
            var busySpawns = new List<SwarmDirector.Spawn>();
            idle.Open(idleSpawns);
            busy.Open(busySpawns);
            const float Frame = 1f / 60f;
            int busyAlive = 3;
            for (int f = 0; f < (int)(wave * 3.5f / Frame); f++)
            {
                idle.Tick(Frame, 3, idleSpawns);
                busy.Tick(Frame, busyAlive, busySpawns);
                busyAlive += busySpawns.Count;
                for (int slot = 0; slot < busy.Slots && busyAlive > 0; slot++)
                {
                    busy.Fell(slot);
                    busyAlive--;
                }
            }
            Assert.That(busy.Spawned, Is.GreaterThan(idle.Spawned * 2), "One room killed far more than the other.");
            Assert.That(busy.Wave, Is.EqualTo(idle.Wave), "Both are on the wave their minute calls for.");
            Assert.That(idle.Slots, Is.EqualTo(busy.Slots), "And both have opened the places that wave holds.");
            Assert.That(idle.Slots, Is.EqualTo(Ladder[3]));
        }

        [Test]
        public void ANewPlaceOpensWhenTheClockReachesABiggerWaveEvenIfNothingDied()
        {
            var director = new SwarmDirector(new[] { 2, 3 });
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);
            Assert.That(director.Slots, Is.EqualTo(2));

            director.Tick(SwarmRule.LadderSeconds * 0.5f, 2, spawns);
            Assert.That(spawns, Is.Empty, "The first wave's two while its time lasts.");

            director.Tick(SwarmRule.LadderSeconds * 0.5f, 2, spawns);
            Assert.That(director.Slots, Is.EqualTo(3), "The second wave holds three, so a third place opens.");
            Assert.That(spawns.Count, Is.EqualTo(1));
            Assert.That((spawns[0].Slot, spawns[0].Wave, spawns[0].Index), Is.EqualTo((2, 1, 2)),
                "Into the new place walks the second wave's third enemy.");
            Assert.That(director.CapAt(0f), Is.EqualTo(2));
            Assert.That(director.CapAt(SwarmRule.LadderSeconds), Is.EqualTo(3));
        }

        [Test]
        public void LevelUpSittingsFallDueOnTheRoomsClock()
        {
            float every = SwarmRule.OfferEverySeconds;
            Assert.That(SwarmRule.OfferFallsDue(0f, every * 0.5f), Is.False);
            Assert.That(SwarmRule.OfferFallsDue(every * 0.5f, every), Is.True, "Due as the clock reaches the interval.");
            Assert.That(SwarmRule.OfferFallsDue(every, every * 1.5f), Is.False, "And not again until the next one.");
            Assert.Throws<ArgumentOutOfRangeException>(() => SwarmRule.OfferFallsDue(2f, 1f));

            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);
            int due = 0;
            const float Frame = 1f / 60f;
            // Half a second past the third, so a clock summed from sixty-ones never falls a hair short of it.
            for (int f = 0; f < (int)((every * 3f + 0.5f) / Frame); f++)
            {
                director.Tick(Frame, 3, spawns);
                if (director.OfferDue)
                    due++;
            }
            Assert.That(due, Is.EqualTo(3), "Once per interval of the room's clock, on one frame each.");
        }

        [Test]
        public void TheRampRunsOnTheClockAndAnEnemyKeepsTheDifficultyItWasBornWith()
        {
            Assert.That(SwarmRule.DifficultyAtSeconds(0f), Is.EqualTo(1f));
            Assert.That(SwarmRule.DifficultyAtSeconds(SwarmRule.StepSeconds * 0.99f), Is.EqualTo(1f),
                "The first step lasts a whole step.");
            Assert.That(SwarmRule.DifficultyAtSeconds(SwarmRule.StepSeconds * 3f), Is.EqualTo(SwarmRule.DifficultyAt(3)));

            var director = new SwarmDirector(Ladder);
            var spawns = new List<SwarmDirector.Spawn>();
            director.Open(spawns);
            director.Tick(SwarmRule.StepSeconds * 4f, 3, spawns);
            director.Fell(0);
            director.Tick(SwarmRule.RespawnSeconds, 2, spawns);
            Assert.That(spawns.Count, Is.EqualTo(1));
            Assert.That(spawns[0].Difficulty, Is.EqualTo(SwarmRule.DifficultyAtSeconds(director.Seconds)),
                "An enemy that walks in late walks in harder.");
            Assert.That(spawns[0].Difficulty, Is.GreaterThan(1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => SwarmRule.DifficultyAtSeconds(-1f));
        }

        [Test]
        public void TheExitIsHeldNotTouched()
        {
            var arena = new ArenaGeometry(-9f, 9f, 9f);
            ExitRule.SpotFor(arena, out float x, out float y);
            Assert.That(x, Is.Zero);
            Assert.That(y, Is.GreaterThan(ChestRule.PreferredDistance), "The way out stands well beyond the chest.");
            Assert.That(arena.IsOnPlatform(x, y, HeroMotion.EdgeMargin), Is.True, "And it can be reached.");

            // The hold counts only while the hero stands still on it: a frame of moving, on the exit or off it, is a
            // frame of not holding, which is what keeps a kiting hero from leaving by running circles across it.
            var dwell = new ExitDwell();
            Assert.That(dwell.Step(ExitRule.DwellSeconds * 0.6f, true), Is.False);
            Assert.That(dwell.Step(0.1f, false), Is.False, "Moving or stepping off starts the count again.");
            Assert.That(dwell.Held, Is.Zero);
            Assert.That(dwell.Step(ExitRule.DwellSeconds * 0.6f, true), Is.False);
            Assert.That(dwell.Step(ExitRule.DwellSeconds * 0.6f, true), Is.True, "A full second, unbroken, leaves.");
            Assert.That(dwell.Left, Is.True);

            Assert.That(ExitRule.IsWithinReach(x + 0.5f, y, x, y), Is.True);
            Assert.That(ExitRule.IsWithinReach(x + 1.5f, y, x, y), Is.False);

            var small = new ArenaGeometry(-3f, 3f, 3f);
            ExitRule.SpotFor(small, out _, out float smallY);
            Assert.That(small.IsOnPlatform(0f, smallY, HeroMotion.EdgeMargin), Is.True, "A small arena keeps it reachable.");
        }

        [Test]
        public void AFallenPlaceInThePackCanBeFilledAgainAndALivingOneCannot()
        {
            var motion = new PackMotion(0.9f, 1f, new ArenaGeometry(-9f, 9f, 9f), 0f, 0f);
            var first = new HealthState(10f);
            var second = new HealthState(10f);
            int slot = motion.Add(first, new EntryPlacement(EntrySide.Far, 0f, 0f, 5f), 2f, 1.5f);
            Assert.Throws<InvalidOperationException>(
                () => motion.Replace(slot, second, new EntryPlacement(EntrySide.Far, 0f, 0f, 5f), 2f, 1.5f),
                "A living enemy keeps its place.");

            first.ApplyDamage(new DamageContext(10f));
            motion.Replace(slot, second, new EntryPlacement(EntrySide.Near, 0f, 1f, -4f), 2f, 1.5f);
            Assert.That(motion.Count, Is.EqualTo(1), "Filling a place adds no place.");
            Assert.That((motion.XOf(slot), motion.YOf(slot)), Is.EqualTo((1f, -4f)), "The newcomer enters where it was placed.");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => motion.Replace(3, second, new EntryPlacement(EntrySide.Far, 0f, 0f, 5f), 2f, 1.5f));
        }
    }
}
