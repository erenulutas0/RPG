using System;
using Cryptforge.Art;
using Cryptforge.Combat;

namespace Cryptforge.Core
{
    // How the development autopilot steps in a fight on the foundry: the arithmetic DevelopmentAutopilot and the Descent
    // simulation's copy of it (DescentSimulation.RunFoundry) share, as one body, so the scene and the simulation choose the
    // same step on the same frame (the reason ArenaFloor.FloorDistanceSquared is one body, docs/21).
    public static class FootworkRule
    {
        // The points round the Warden's mark an escape chooses among.
        public const int EscapePoints = 16;
        // An escape point's walk, as a share of the escape's radius, counts this much against it: twice the whole range of
        // the preference for away from the Warden (from straight away to straight at it, -1..1), so once the hero is half
        // a radius out it keeps to its side of the mark rather than turning back across it.
        public const float EscapeWalkWeight = 2f;
        // An escape point with no room round it counts this much against it, on the same scale.
        public const float EscapeRoomPenalty = .5f;
        // Room: this much more clearance than the hero's own edge margin, in floor units.
        public const float Room = 1f;

        // A kite stride ahead of (x, y): the push (a unit vector away from what presses the hero) turned by turn, as the
        // autopilot circles a crowd; or, where that runs into a wall or a work island, the first of the push turned the
        // other way, then twice and three times turn either way, that the hero can stand on and walk to in a straight line:
        // so it slides along the wall or turns away from it rather than standing against it. False when no turn has room.
        public static bool Kite(float x, float y, float pushX, float pushY, float turn, float stride,
            out float gx, out float gy)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = turn * (i / 2 + 1) * (i % 2 == 0 ? 1f : -1f);
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                gx = x + (pushX * cos - pushY * sin) * stride;
                gy = y + (pushX * sin + pushY * cos) * stride;
                if (Clear(x, y, gx, gy))
                    return true;
            }
            gx = x;
            gy = y;
            return false;
        }

        // The escape from the Warden's mark: of EscapePoints points radius out from the mark that the hero can stand on
        // and walk to in a straight line, the one farthest round from the Warden (wardenX, wardenY) less its walk as a share
        // of radius, one without room round it counting against it. With the Warden on the mark, the nearest. False when
        // no point can be walked to.
        public static bool EscapeMark(float x, float y, float markX, float markY, float radius, float wardenX,
            float wardenY, out float gx, out float gy)
        {
            gx = x;
            gy = y;
            float awayX = markX - wardenX;
            float awayY = markY - wardenY;
            Normalize(ref awayX, ref awayY);
            float best = float.NegativeInfinity;
            for (int k = 0; k < EscapePoints; k++)
            {
                float angle = k * (2f * 3.14159274f / EscapePoints);
                float dx = (float)Math.Cos(angle);
                float dy = (float)Math.Sin(angle);
                float px = markX + dx * radius;
                float py = markY + dy * radius;
                if (!Clear(x, y, px, py))
                    continue;
                float score = dx * awayX + dy * awayY -
                              EscapeWalkWeight * (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(px, py, x, y)) / radius;
                if (!FoundryMap.Contains(px, py, HeroMotion.EdgeMargin + Room))
                    score -= EscapeRoomPenalty;
                if (score <= best)
                    continue;
                best = score;
                gx = px;
                gy = py;
            }
            return !float.IsNegativeInfinity(best);
        }

        // A goal the hero can stand on and walk to in a straight line.
        private static bool Clear(float x, float y, float tx, float ty) =>
            FoundryMap.Contains(tx, ty, HeroMotion.EdgeMargin) && FoundryMap.Segment(x, y, tx, ty, HeroMotion.EdgeMargin);

        // UnityEngine.Vector2.Normalize: divided by its magnitude, or zero below Vector2.kEpsilon.
        private static void Normalize(ref float x, ref float y)
        {
            float magnitude = (float)Math.Sqrt(x * x + y * y);
            if (magnitude > 1e-5f)
            {
                x /= magnitude;
                y /= magnitude;
            }
            else
            {
                x = 0f;
                y = 0f;
            }
        }
    }
}
