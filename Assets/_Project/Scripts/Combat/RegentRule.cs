using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Glass Regent, floor 2's gate boss (docs/50 section 6; the lead's decisions 2026-11-12): the Forge Warden's fight
    // on the Warden's clock (WardenSlamClock: a mark every 6 s, every 4 s below half its health, its weapon held from the
    // mark until RecoverySeconds after the blow) with a longer telegraph, Telegraph, and a sight line for its mark. As
    // the mark goes down the Regent marks a LANE from where it stands through where the hero stands, LaneLength long and
    // LaneWidth wide; when it fills, shards run down it and a hero still inside takes LaneShare of its maximum health. The
    // dodge is a sidestep across the lane (FootworkRule.Sidestep), not a run out of a circle: a step along the lane stays in
    // it. Its phase two is the Warden's (the faster beat, the horde thickening round it).
    //
    // The share of the gate is the Warden's lever (FloorDefinition.FoundryGateHealthShare, WardenRule.GateHealthShareOn):
    // floor 2 sets 7.0 for the Regent (docs/50, round 2 on the holdout seeds 201-400: careful P50 49 / 46 s, in band as the
    // Warden's 50 / 46). The lane's 10 % and its 1.5 s telegraph are docs/50's "Regent" sweep under the lead's relative
    // rule (VaultsEnemiesResearch, seeds 1-400, typical hands, the shipped Sentry beside it): the clumsy cohort completes
    // the four-floor Descent 306 times in 800 as with the Forge Warden (docs/50's starting 18 % and 1.1 s cost it 7 on
    // seeds 1-200 alone), careful clears stay within 3 on both ranges and the fight in band. Measured, not chosen: change
    // any of them only with a re-measurement.
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
        // than the Warden's 1.1 s, the time a sidestep across a line needs to be read and walked (docs/50 "Regent").
        public const float Every = WardenRule.SlamEvery;
        public const float EveryPhaseTwo = WardenRule.SlamEveryEnraged;
        public const float Telegraph = 1.5f;
        // At most this many bodies cast a lane on one beat.
        public const int MaxLanes = 3;

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
    // a lane stood as it went down, every lane from its body through the mark. Fixed as the mark goes down - a mark on the
    // floor, not a beam that follows its caster. The width and length are RegentRule's unless a measurement hands others
    // (the simulation's levers).
    public sealed class RegentLanes
    {
        private readonly float[] _x = new float[RegentRule.MaxLanes];
        private readonly float[] _y = new float[RegentRule.MaxLanes];

        public RegentLanes() : this(RegentRule.LaneWidth, RegentRule.LaneLength)
        {
        }

        public RegentLanes(float width, float length)
        {
            if (float.IsNaN(width) || float.IsInfinity(width) || width <= 0f)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (float.IsNaN(length) || float.IsInfinity(length) || length <= 0f)
                throw new ArgumentOutOfRangeException(nameof(length));
            Width = width;
            Length = length;
        }

        public float Width { get; }
        public float Length { get; }
        // How many lanes are down, 0 with none.
        public int Count { get; private set; }
        // Where the mark went down: every lane runs through it.
        public float MarkX { get; private set; }
        public float MarkY { get; private set; }

        public float OriginX(int lane) => _x[Index(lane)];
        public float OriginY(int lane) => _y[Index(lane)];

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
            Count++;
            return true;
        }

        // The beat is over (its blow landed, or the fight stopped): no lane is down.
        public void Clear() => Count = 0;

        // Whether (x, y) stands in any lane down, each widened by margin either side.
        public bool Holds(float x, float y, float margin)
        {
            float half = Width * .5f + margin;
            for (int i = 0; i < Count; i++)
                if (RegentRule.InLane(x, y, _x[i], _y[i], MarkX, MarkY, Length, half))
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
