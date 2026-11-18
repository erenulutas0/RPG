using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Glass Regent, floor 2's gate boss (docs/50 section 6; the lead's decisions 2026-11-12): the Forge Warden's fight
    // on the Warden's clock (WardenSlamClock: a mark every 6 s, every 4 s below half its health, its weapon held from the
    // mark until RecoverySeconds after the blow) with a longer telegraph, Telegraph, and a sight line for its mark. As
    // the mark goes down the Regent marks a LANE from where it stands through where the hero stands, LaneLength long - and
    // always at least LanePast beyond the hero's spot - and LaneWidth wide; when it fills, shards run down it and a hero
    // still inside takes LaneShare of its maximum health. The
    // dodge is a sidestep across the lane (FootworkRule.Sidestep), not a run out of a circle: a step along the lane stays in
    // it. Its phase two is the Warden's (the faster beat, the horde thickening round it). Its rod is held from the mark
    // until the recovery after the blow, as the Warden's hammer - but only for a hero beyond its feet as the mark goes down
    // (FeetReach, HoldsRod; docs/50 "The Regent's bite for the standing hero", 2026-11-17): a hero at its feet is struck
    // through the cast.
    //
    // The share of the gate is the Warden's lever (FloorDefinition.FoundryGateHealthShare, WardenRule.GateHealthShareOn):
    // floor 2 sets 7.0 for the Regent (docs/50, round 2 on the holdout seeds 201-400: careful P50 49 / 46 s, in band as the
    // Warden's 50 / 46). The lane's 10 % and its 1.7 s telegraph are docs/50's "Regent" sweep under the lead's relative
    // rule (VaultsEnemiesResearch, seeds 1-400, typical hands, the shipped Sentry beside it, every lane reaching through the
    // hero's spot since 2026-11-15): the clumsy cohort completes the four-floor Descent 309 times in 800 against the Forge
    // Warden's 306, careful clears stay within 3 on both ranges and the fight in band. Of the three tunings that pass (6 %
    // and 1.5 s, 10 % and 1.7 s, 8 % and 1.7 s) the lead chose this one (2026-11-15): their careful-minus-standing gaps lie
    // inside the noise, and it has the most room under the rule with a lane that still matters beside the Warden's 20 %
    // circle. 10 % and 1.5 s, cleared before the lanes reached, now cost 5. Measured, not chosen: change any of them only
    // with a re-measurement.
    //
    // Pure: the scene's WardenSlamService and the foundry simulation (DescentSimulation.Vaults.cs) mark and judge the same
    // RegentLanes, and the lane test is one body that is never inlined, so the scene and the simulation round alike (the
    // stamina divergence's lesson, docs/21: never copy distance or angle arithmetic across assemblies). Lanes are marks on
    // the floor, not projectiles (docs/42 section 4), drawn red-orange (#FF5A3C) and filled with the clock's
    // TelegraphProgress (docs/49 rule 4) by WardenSlamView.
    public static class RegentRule
    {
        public const float LaneWidth = 1.2f;
        public const float LaneLength = 10f;
        public const float LaneShare = .1f;
        // The clock (WardenSlamClock): the Warden's own beat, phase two's included, with a telegraph of its own - longer
        // than the Warden's 1.1 s, the time a sidestep across a line needs to be read and walked (docs/50 "Regent"). Above
        // the 0.8-1.2 s a phone telegraph is read in: the owner's play on the S23 is to judge it, and it may come down once
        // the device read is in (re-measured with the lane's share).
        public const float Every = WardenRule.SlamEvery;
        public const float EveryPhaseTwo = WardenRule.SlamEveryEnraged;
        public const float Telegraph = 1.7f;
        // At most this many bodies cast a lane on one beat.
        public const int MaxLanes = 3;
        // How far past the hero's spot a lane reaches at the least, where the hero stands farther from its caster than
        // LaneLength allows: the lane runs through the mark and LanePast beyond it, so every lane runs through the hero's
        // spot however far the hero stands. Without it the fight's first lane - cast while the Regent still walks out of
        // the gate, about 12 units from a hero that waited for it - fell short of the hero and missed it, the lane drawn
        // short of the mark (the Unity run of GlassRegentFlowTests, 2026-11-15). Beyond the 3.2 units the escapes and the
        // sidestep reach, so a step along a far lane stays in it as a step along a near one does. Measured with the rest
        // of the tuning (docs/50 "Regent").
        public const float LanePast = 4f;
        // The rod at its feet (docs/50 "The Regent's bite for the standing hero", 2026-11-17): the Warden's clock holds the
        // boss's weapon from the mark until RecoverySeconds after the blow - the Warden's hammer raised and then in the
        // floor, the window a hero that read the mark strikes back in. The Regent casts its lane with its mirror, and its
        // longer telegraph made that hold 3.2 s of every 6 s beat (4 s in phase two): a hero that stood at its side was
        // safe from its rod for most of the fight, the standing cohort's whole easing against the Forge Warden. So the
        // Regent holds its rod for a beat only when the mark goes down with the hero beyond FeetReach of it; a hero at its
        // feet is struck through the cast and the shards' run (HoldsRod). FeetReach lies a fifth of a unit inside the Glass
        // Rod's 1.7: the rod's whole reach also caught a hero kiting at its edge (the clumsy cohort's straight kite stood
        // 1.5-1.7 from the Regent at 13 % of its marks, the standing hero within 1.5 at 37 %).
        public const float FeetReach = 1.5f;

        // A lane from (ax, ay) through (mx, my), length long from (ax, ay) and halfWidth either side of its line: whether
        // (px, py) stands in it. A lane whose two points meet runs toward the floor's front (0, -1).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool InLane(float px, float py, float ax, float ay, float mx, float my, float length, float halfWidth)
        {
            float dx = mx - ax, dy = my - ay;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > 1e-5f)
            {
                dx /= d;
                dy /= d;
            }
            else
            {
                dx = 0f;
                dy = -1f;
            }
            float along = (px - ax) * dx + (py - ay) * dy;
            if (along < 0f)
                along = 0f;
            else if (along > length)
                along = length;
            float cx = ax + dx * along - px, cy = ay + dy * along - py;
            return cx * cx + cy * cy <= halfWidth * halfWidth;
        }

        // How long a lane from (ax, ay) through (mx, my) is: length, or the distance to the mark and past beyond it where
        // that is farther. A past below zero keeps length whatever the distance (the lane before 2026-11-15, for a
        // comparison). One body, never inlined, for the scene and the simulation, as InLane.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float LaneLengthThrough(float ax, float ay, float mx, float my, float length, float past)
        {
            if (past < 0f)
                return length;
            float dx = mx - ax, dy = my - ay;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            return Math.Max(length, d + past);
        }

        // Whether a hero at (x, y) stands within walk of (fromX, fromY) - where the last blow found it: it has stood since.
        // The squares are cast to float, as PollenRule.Near's: Unity's Mono keeps a product wider than .NET does.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Stood(float x, float y, float fromX, float fromY, float walk)
        {
            float dx = x - fromX, dy = y - fromY;
            return (float)(dx * dx + dy * dy) <= (float)(walk * walk);
        }

        // Whether a hero at (x, y) stands at the Regent's feet, the Regent at (regentX, regentY): within reach of it. The
        // squares are cast to float, as PollenRule.Near's (Unity's Mono keeps a product wider than .NET does).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AtItsFeet(float x, float y, float regentX, float regentY, float reach)
        {
            float dx = x - regentX, dy = y - regentY;
            return (float)(dx * dx + dy * dy) <= (float)(reach * reach);
        }

        // Whether the Regent holds its rod now: as the clock holds the Warden's weapon (clockHolds: from the mark until the
        // recovery after the blow), but never for a hero at its feet.
        public static bool HoldsRod(bool clockHolds, bool atItsFeet) => clockHolds && !atItsFeet;

        // (bx, by) turned degrees round (mx, my), counter-clockwise on the floor.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void TurnedRound(float bx, float by, float mx, float my, float degrees, out float x, out float y)
        {
            double angle = degrees * Math.PI / 180.0;
            float cos = (float)Math.Cos(angle), sin = (float)Math.Sin(angle);
            float ox = bx - mx, oy = by - my;
            x = mx + ox * cos - oy * sin;
            y = my + ox * sin + oy * cos;
        }

        // The unit direction of a lane from (ax, ay) through (mx, my), as InLane reads it: for the lanes' look.
        public static void LaneDirection(float ax, float ay, float mx, float my, out float dx, out float dy)
        {
            dx = mx - ax;
            dy = my - ay;
            float d = (float)Math.Sqrt(dx * dx + dy * dy);
            if (d > 1e-5f)
            {
                dx /= d;
                dy /= d;
            }
            else
            {
                dx = 0f;
                dy = -1f;
            }
        }
    }

    // The lanes of one beat of the Regent's clock: where the mark went down (the hero's spot) and where each body that casts
    // a lane stood as it went down, every lane from its body through the mark and on (RegentRule.LaneLengthThrough). Fixed
    // as the mark goes down - a mark on the floor, not a beam that follows its caster. The width, length and reach past the
    // mark are RegentRule's unless a measurement hands others (the simulation's levers).
    public sealed class RegentLanes
    {
        private readonly float[] _x = new float[RegentRule.MaxLanes];
        private readonly float[] _y = new float[RegentRule.MaxLanes];
        private readonly float[] _length = new float[RegentRule.MaxLanes];

        public RegentLanes() : this(RegentRule.LaneWidth, RegentRule.LaneLength)
        {
        }

        public RegentLanes(float width, float length, float past = RegentRule.LanePast)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (float.IsNaN(length) || float.IsInfinity(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length));
            if (float.IsNaN(past) || float.IsInfinity(past))
                throw new ArgumentOutOfRangeException(nameof(past));
            Width = width;
            Length = length;
            Past = past;
        }

        public float Width { get; }
        // A lane's length from its caster where the mark stands within it.
        public float Length { get; }
        // How far past the mark a lane reaches at the least (below zero: no farther than Length).
        public float Past { get; }
        // How many lanes are down, 0 with none.
        public int Count { get; private set; }
        // Where the mark went down: every lane runs through it.
        public float MarkX { get; private set; }
        public float MarkY { get; private set; }

        public float OriginX(int lane) => _x[Index(lane)];
        public float OriginY(int lane) => _y[Index(lane)];
        // A lane's length from its caster: Length, or through the mark and Past beyond it.
        public float LengthOf(int lane) => _length[Index(lane)];

        // A new beat's mark at (markX, markY), with no lane yet.
        public void Mark(float markX, float markY)
        {
            Count = 0;
            MarkX = markX;
            MarkY = markY;
        }

        // A body at (x, y) casts a lane through the mark; false, and no lane, once RegentRule.MaxLanes are down.
        public bool Add(float x, float y)
        {
            if (Count >= _x.Length)
                return false;
            _x[Count] = x;
            _y[Count] = y;
            _length[Count] = RegentRule.LaneLengthThrough(x, y, MarkX, MarkY, Length, Past);
            Count++;
            return true;
        }

        // Whether the beat's mark went down at its first caster's feet: the hero's spot (the mark) within reach of where the
        // lane's first body (the Regent) stood (RegentRule.AtItsFeet). False with no lane down.
        public bool AtItsFeet(float reach) =>
            Count > 0 && RegentRule.AtItsFeet(MarkX, MarkY, _x[0], _y[0], reach);

        // The beat is over (its blow landed, or the fight stopped): no lane is down.
        public void Clear() => Count = 0;

        // Whether (x, y) stands in any lane down, each widened by margin either side.
        public bool Holds(float x, float y, float margin)
        {
            float half = Width * .5f + margin;
            for (int i = 0; i < Count; i++)
                if (RegentRule.InLane(x, y, _x[i], _y[i], MarkX, MarkY, _length[i], half))
                    return true;
            return false;
        }

        // The unit direction of a lane, from its body through the mark (RegentRule.LaneDirection).
        public void Direction(int lane, out float dx, out float dy) =>
            RegentRule.LaneDirection(OriginX(lane), OriginY(lane), MarkX, MarkY, out dx, out dy);

        private int Index(int lane)
        {
            if (lane < 0 || lane >= Count)
                throw new ArgumentOutOfRangeException(nameof(lane));
            return lane;
        }
    }
}
