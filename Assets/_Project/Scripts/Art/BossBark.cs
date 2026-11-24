using System;
using Cryptforge.Combat;
using Cryptforge.Localisation;

namespace Cryptforge.Art
{
    // When a gate boss speaks (docs/44 section 1.8; docs/64 stage 1, 2026-11-23): as it walks out of the gate, with the first
    // mark of each of its phases, and as it falls.
    public enum BarkMoment
    {
        Spawn,
        Telegraph,
        Death
    }

    // The gate bosses' short lines as barks: docs/44 section 1.8's spawn, telegraph and death lines, each shown as a plate over
    // the boss for Seconds (inside the 1.5-2 s the owner asked for), the telegraph line at most once a phase, and the plate
    // never over the mark that is down (Clear). Pure - the lines, the bark's opacity over its life and the placement test - so
    // CombatChecks holds them without a scene; BossBarkView shows them. Visual only: nothing here is read by a rule, a weapon
    // or the simulation.
    public static class BossBark
    {
        // A bark's life, its fade in and its fade out, in scaled seconds (a pause holds it, as it holds the fight).
        public const float Seconds = 1.8f;
        public const float FadeIn = .15f;
        public const float FadeOut = .3f;
        // How far a plate keeps from a mark's edge, in floor units, so the two never touch on the phone.
        public const float MarkMargin = .15f;

        // The gate bosses that speak, by their EnemyDefinition id.
        public const string WardenId = "enemy_forge_warden";
        public const string RegentId = "enemy_glass_regent";
        public const string MyrrId = "enemy_rootmother_myrr";

        // docs/44 section 1.8's lines in English, and their keys: bark.<boss>.<moment>.
        private static readonly LocText WardenSpawn = new LocText("bark.forge_warden.spawn", "Shift's not over.");
        private static readonly LocText WardenTelegraph = new LocText("bark.forge_warden.telegraph", "Stand clear of the mark.");
        private static readonly LocText WardenDeath = new LocText("bark.forge_warden.death", "Door... was never locked for you.");
        private static readonly LocText RegentSpawn = new LocText("bark.glass_regent.spawn", "Which one of you is original?");
        private static readonly LocText RegentTelegraph = new LocText("bark.glass_regent.telegraph", "Hold still. I need your face.");
        private static readonly LocText RegentDeath = new LocText("bark.glass_regent.death", "I kept the wrong one...");
        // Rootmother Myrr's (docs/64 section 1; her scene build, 2026-11-25).
        private static readonly LocText MyrrSpawn = new LocText("bark.rootmother_myrr.spawn", "Your tomorrow can still take root.");
        private static readonly LocText MyrrTelegraph = new LocText("bark.rootmother_myrr.telegraph", "Be still. Be planted.");
        private static readonly LocText MyrrDeath = new LocText("bark.rootmother_myrr.death", "Something... is growing underneath.");

        // The line a boss says at a moment; false for a boss with no lines (an enemy that is no gate boss, or one whose lines
        // are not written yet).
        public static bool TryLine(string bossId, BarkMoment moment, out LocText line)
        {
            switch (bossId)
            {
                case WardenId:
                    line = moment == BarkMoment.Spawn ? WardenSpawn : moment == BarkMoment.Telegraph ? WardenTelegraph : WardenDeath;
                    return true;
                case RegentId:
                    line = moment == BarkMoment.Spawn ? RegentSpawn : moment == BarkMoment.Telegraph ? RegentTelegraph : RegentDeath;
                    return true;
                case MyrrId:
                    line = moment == BarkMoment.Spawn ? MyrrSpawn : moment == BarkMoment.Telegraph ? MyrrTelegraph : MyrrDeath;
                    return true;
                default:
                    line = default;
                    return false;
            }
        }

        // Every line written, for the checks (the table, the Turkish fit).
        public static readonly LocText[] Lines =
        {
            WardenSpawn, WardenTelegraph, WardenDeath, RegentSpawn, RegentTelegraph, RegentDeath, MyrrSpawn, MyrrTelegraph,
            MyrrDeath
        };

        // Whether a bark elapsed seconds old still shows.
        public static bool Shows(float elapsed) => elapsed >= 0f && elapsed < Seconds;

        // A bark's opacity elapsed seconds into its life: rising over FadeIn, held, falling over its last FadeOut, 0 outside it.
        public static float Alpha(float elapsed)
        {
            if (!Shows(elapsed))
                return 0f;
            if (elapsed < FadeIn)
                return elapsed / FadeIn;
            float left = Seconds - elapsed;
            return left < FadeOut ? left / FadeOut : 1f;
        }

        // Whether a plate standing over [left, right] x [bottom, top] in world units, lifted by lift (the foundry's surface
        // under the mark: WardenSlamView draws the mark at its floor spot raised so), keeps clear of a circle mark of radius
        // on the floor at (markX, markY) by MarkMargin. The floor is seen at ArenaFloor.DepthScale, so the plate is read on
        // the floor's own scale. The Warden's circle (WardenRule.SlamRadius) and Myrr's snare (MyrrRule.SnareRadius, its
        // centre read each frame as it creeps) alike.
        public static bool ClearOfCircle(float left, float right, float bottom, float top, float lift, float markX,
            float markY, float radius)
        {
            ToFloor(bottom, top, lift, out float floorBottom, out float floorTop);
            float nx = Clamp(markX, left, right) - markX;
            float ny = Clamp(markY, floorBottom, floorTop) - markY;
            float reach = radius + MarkMargin;
            return nx * nx + ny * ny > reach * reach;
        }

        // Whether the same plate keeps clear of a lane on the floor - from (fromX, fromY) along the unit (dirX, dirY) for
        // length, halfWidth either side of its line (RegentLanes) - by MarkMargin.
        public static bool ClearOfLane(float left, float right, float bottom, float top, float lift, float fromX, float fromY,
            float dirX, float dirY, float length, float halfWidth)
        {
            ToFloor(bottom, top, lift, out float floorBottom, out float floorTop);
            float toX = fromX + dirX * length, toY = fromY + dirY * length;
            float reach = halfWidth + MarkMargin;
            return SegmentToBoxSquared(fromX, fromY, toX, toY, left, right, floorBottom, floorTop) > reach * reach;
        }

        // A world height range as the floor's depths, the lift taken off first.
        private static void ToFloor(float bottom, float top, float lift, out float floorBottom, out float floorTop)
        {
            floorBottom = ArenaFloor.FloorY(bottom - lift);
            floorTop = ArenaFloor.FloorY(top - lift);
        }

        // The squared distance from the segment (ax, ay)-(bx, by) to the box [left, right] x [bottom, top]: 0 where they meet,
        // else the least of its ends' distances to the box and the box's corners' distances to it (two convex shapes apart
        // are nearest at a vertex of one).
        public static float SegmentToBoxSquared(float ax, float ay, float bx, float by, float left, float right,
            float bottom, float top)
        {
            if (SegmentMeetsBox(ax, ay, bx, by, left, right, bottom, top))
                return 0f;
            float best = PointToBoxSquared(ax, ay, left, right, bottom, top);
            best = Math.Min(best, PointToBoxSquared(bx, by, left, right, bottom, top));
            best = Math.Min(best, PointToSegmentSquared(left, bottom, ax, ay, bx, by));
            best = Math.Min(best, PointToSegmentSquared(left, top, ax, ay, bx, by));
            best = Math.Min(best, PointToSegmentSquared(right, bottom, ax, ay, bx, by));
            best = Math.Min(best, PointToSegmentSquared(right, top, ax, ay, bx, by));
            return best;
        }

        // Liang-Barsky: whether any part of the segment lies inside the box.
        private static bool SegmentMeetsBox(float ax, float ay, float bx, float by, float left, float right, float bottom,
            float top)
        {
            float dx = bx - ax, dy = by - ay;
            float enter = 0f, leave = 1f;
            return Clip(-dx, ax - left, ref enter, ref leave) && Clip(dx, right - ax, ref enter, ref leave) &&
                   Clip(-dy, ay - bottom, ref enter, ref leave) && Clip(dy, top - ay, ref enter, ref leave);
        }

        private static bool Clip(float p, float q, ref float enter, ref float leave)
        {
            if (p == 0f)
                return q >= 0f;
            float r = q / p;
            if (p < 0f)
            {
                if (r > leave)
                    return false;
                if (r > enter)
                    enter = r;
            }
            else
            {
                if (r < enter)
                    return false;
                if (r < leave)
                    leave = r;
            }
            return true;
        }

        private static float PointToBoxSquared(float x, float y, float left, float right, float bottom, float top)
        {
            float dx = Clamp(x, left, right) - x, dy = Clamp(y, bottom, top) - y;
            return dx * dx + dy * dy;
        }

        private static float PointToSegmentSquared(float x, float y, float ax, float ay, float bx, float by)
        {
            float dx = bx - ax, dy = by - ay;
            float length = dx * dx + dy * dy;
            float t = length > 0f ? ((x - ax) * dx + (y - ay) * dy) / length : 0f;
            t = Clamp(t, 0f, 1f);
            float cx = ax + dx * t - x, cy = ay + dy * t - y;
            return cx * cx + cy * cy;
        }

        private static float Clamp(float value, float low, float high) => value < low ? low : value > high ? high : value;
    }

    // What a fight's boss has said: its spawn line once, its telegraph line with the first mark of phase one and again with
    // the first of phase two (at most once a phase), its death line once. A new boss is a new memory.
    public sealed class BossBarkMemory
    {
        private bool _spawned;
        private bool _toldPhaseOne;
        private bool _toldPhaseTwo;
        private bool _died;

        // How many telegraph lines this fight has said (0-2).
        public int TelegraphsSaid => (_toldPhaseOne ? 1 : 0) + (_toldPhaseTwo ? 1 : 0);

        public void Reset()
        {
            _spawned = false;
            _toldPhaseOne = false;
            _toldPhaseTwo = false;
            _died = false;
        }

        // Whether the boss speaks at this moment now; a line is said once (the telegraph's once a phase).
        public bool Says(BarkMoment moment, bool phaseTwo)
        {
            switch (moment)
            {
                case BarkMoment.Spawn:
                    return Spend(ref _spawned);
                case BarkMoment.Telegraph:
                    return phaseTwo ? Spend(ref _toldPhaseTwo) : Spend(ref _toldPhaseOne);
                default:
                    return Spend(ref _died);
            }
        }

        private static bool Spend(ref bool said)
        {
            if (said)
                return false;
            said = true;
            return true;
        }
    }
}
