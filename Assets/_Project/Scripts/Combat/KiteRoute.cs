using System;

namespace Cryptforge.Combat
{
    // The player stand-in the density proof measures a moving hero with: it backs away from everything close enough to hit
    // it, walks in when nothing is, and stands and strikes in between. Three rules, in order, every frame:
    //
    //  1. Threats are the living enemies nearer than their own reach plus ThreatMargin. With any threat the hero flees
    //     along the sum of the directions away from them, each weighted by 1 / distance^2, so the nearest threat decides
    //     most of the direction and a pack surrounding the hero pushes it out through its thinnest side. Near the rim a
    //     pull toward the platform's centre is added first, or the hero would flee into the void's edge and pin itself.
    //  2. Otherwise, if the nearest living enemy stands farther than the hero's reach less ApproachMargin, walk at it.
    //  3. Otherwise hold still: the enemy is inside the hero's reach and outside its own, the band the hero wants.
    //
    // Whether that band exists at all is the weapon's business, not this route's: it is empty unless the hero's reach
    // exceeds the enemy's by more than ThreatMargin + ApproachMargin, and then rules 1 and 2 alternate around it.
    // Deterministic, allocation-free and never NaN, so two runs of the same fight give the same numbers.
    public sealed class KiteRoute : IHeroRoute
    {
        // How far beyond an enemy's own reach the hero already treats it as a threat, in floor units.
        public const float ThreatMargin = 0.6f;
        // How near the rim the hero starts pulling back toward the platform's centre while it flees.
        public const float RimAvoidance = 1.5f;
        // The weight of that pull, against the flight from the threats, whose own weights sum to about 1 / distance.
        public const float RimPull = 1f;
        // How far inside its own reach the hero walks before it stops and strikes.
        public const float ApproachMargin = 0.2f;
        // A flight shorter than this counts as cancelled out and takes the tie-break instead.
        private const float Shortest = 1e-6f;

        public void Steer(RouteView view, out float steerX, out float steerY)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            int nearest = -1;
            int nearestThreat = -1;
            float nearestSquared = 0f;
            float nearestThreatSquared = 0f;
            float awayX = 0f;
            float awayY = 0f;
            for (int i = 0; i < view.Count; i++)
            {
                if (!view.Alive[i])
                    continue;

                // Away from the enemy, so the squared distance is the same either way round.
                float dx = view.HeroX - view.EnemyX[i];
                float dy = view.HeroY - view.EnemyY[i];
                float distanceSquared = dx * dx + dy * dy;
                if (nearest < 0 || distanceSquared < nearestSquared)
                {
                    nearest = i;
                    nearestSquared = distanceSquared;
                }

                float threat = view.EnemyReach[i] + ThreatMargin;
                if (distanceSquared >= threat * threat)
                    continue;
                if (nearestThreat < 0 || distanceSquared < nearestThreatSquared)
                {
                    nearestThreat = i;
                    nearestThreatSquared = distanceSquared;
                }
                float weight = 1f / Math.Max(distanceSquared, 0.01f);
                awayX += dx * weight;
                awayY += dy * weight;
            }

            if (nearest < 0)
            {
                // Nothing left to fight or flee.
                steerX = 0f;
                steerY = 0f;
                return;
            }

            if (nearestThreat >= 0)
            {
                Flee(view, nearestThreat, awayX, awayY, out steerX, out steerY);
                return;
            }

            float band = Math.Max(0f, view.HeroReach - ApproachMargin);
            if (nearestSquared > band * band)
            {
                Direction(view.EnemyX[nearest] - view.HeroX, view.EnemyY[nearest] - view.HeroY, out steerX, out steerY);
                return;
            }

            steerX = 0f;
            steerY = 0f;
        }

        // The flight itself: near the rim the pull toward the centre goes in before the direction is taken, so the hero
        // escapes along the rim instead of walking into it. A flight that cancels out turns a quarter turn clockwise off
        // the nearest threat, always the same way, and a threat standing on the hero sends it to +x: a steer, never NaN.
        private static void Flee(RouteView view, int nearestThreat, float awayX, float awayY, out float steerX, out float steerY)
        {
            if (!view.Platform.IsOnPlatform(view.HeroX, view.HeroY, HeroMotion.EdgeMargin + RimAvoidance))
            {
                Direction(0f - view.HeroX, view.Platform.Middle - view.HeroY, out float centreX, out float centreY);
                awayX += centreX * RimPull;
                awayY += centreY * RimPull;
            }

            Direction(awayX, awayY, out steerX, out steerY);
            if (steerX != 0f || steerY != 0f)
                return;

            float threatX = view.HeroX - view.EnemyX[nearestThreat];
            float threatY = view.HeroY - view.EnemyY[nearestThreat];
            Direction(-threatY, threatX, out steerX, out steerY);
            if (steerX == 0f && steerY == 0f)
                steerX = 1f;
        }

        // A vector as a steer of length one, or (0, 0) when it is too short to point anywhere.
        private static void Direction(float x, float y, out float steerX, out float steerY)
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            if (!(length > Shortest) || float.IsInfinity(length))
            {
                steerX = 0f;
                steerY = 0f;
                return;
            }

            steerX = x / length;
            steerY = y / length;
        }
    }
}
