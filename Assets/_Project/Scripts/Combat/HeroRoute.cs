using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    // What a route sees at the end of a frame: where the hero stands, how far its weapon reaches, the platform it walks on
    // and the wave it fights. One view is filled again every frame and handed to the route, so a route that keeps no state
    // of its own allocates nothing while a fight runs. Everything is in floor units, as everywhere else in the arena.
    public sealed class RouteView
    {
        public float HeroX;
        public float HeroY;
        public float HeroReach;
        public ArenaGeometry Platform;
        // Enemies in the wave, alive or not; the arrays below hold them in slot order, so Alive says which still fight.
        public int Count;
        public readonly float[] EnemyX = new float[PackLayout.MaxPackSize];
        public readonly float[] EnemyY = new float[PackLayout.MaxPackSize];
        public readonly float[] EnemyReach = new float[PackLayout.MaxPackSize];
        public readonly bool[] Alive = new bool[PackLayout.MaxPackSize];
        // Fight seconds so far, so a timed script knows where in its plan it is.
        public float Seconds;
        // This room's chest: where it stands, and whether it is still closed. A route that wants what a chest holds has
        // to walk onto it, which is the whole reason a chest stands away from the middle.
        public float ChestX;
        public float ChestY;
        public bool ChestClosed;
        // A swarm room's way out, which the hero leaves by standing still on (ExitRule). HasExit is false, and the spot
        // meaningless, anywhere else.
        public bool HasExit;
        public float ExitX;
        public float ExitY;
        // The Descent's rewards lying on the floor (docs/27, S6d), oldest first; a route sees at most MaxDrops of them.
        public const int MaxDrops = 64;
        public int DropCount;
        public readonly float[] DropX = new float[MaxDrops];
        public readonly float[] DropY = new float[MaxDrops];
    }

    // How the hero walks, decided once per frame: the player's drag in the scene, a written-down script or an automatic
    // player stand-in in a simulation or a test driver. Pure, so the Descent simulation and the scene's PlayMode driver
    // steer the hero identically. Every route rejects a null view.
    public interface IHeroRoute
    {
        // A steer for the next frame, its length the steer's strength up to one (HeroMotion's convention). Deterministic:
        // the same view always gives the same answer, so a routed run replays frame for frame.
        void Steer(RouteView view, out float steerX, out float steerY);
    }

    // The hero that goes for the chest: it walks straight at this room's chest while the chest is still closed and hands
    // the frame to another route once it has it. A player stand-in for measuring what a chest is worth, and the one route
    // that deliberately leaves the middle of the arena, which is what a chest is for.
    public sealed class ChestRoute : IHeroRoute
    {
        private readonly IHeroRoute _afterwards;

        // afterwards is how the hero behaves once the chest is open, or standing still when none is given.
        public ChestRoute(IHeroRoute afterwards = null)
        {
            _afterwards = afterwards;
        }

        public void Steer(RouteView view, out float steerX, out float steerY)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            float dx = view.ChestX - view.HeroX;
            float dy = view.ChestY - view.HeroY;
            // One body for the distance, never inlined, so the scene's assembly and the simulation's cannot disagree
            // about the last bit of it and walk two different walks (docs/21).
            float distanceSquared = ArenaFloor.FloorDistanceSquared(view.ChestX, view.ChestY, view.HeroX, view.HeroY);
            if (!view.ChestClosed || distanceSquared <= 0f)
            {
                if (_afterwards != null)
                {
                    _afterwards.Steer(view, out steerX, out steerY);
                    return;
                }

                steerX = 0f;
                steerY = 0f;
                return;
            }

            float length = (float)Math.Sqrt(distanceSquared);
            steerX = dx / length;
            steerY = dy / length;
        }
    }

    // The hero that leaves a swarm: it plays the route it is given - or stands - until leaveAt seconds have passed, then
    // walks straight at the exit and stops the moment it is standing on it, because the hold only counts while it is
    // still (ExitRule). A player stand-in for measuring what leaving costs, and the route the scene's parity case walks.
    //
    // Until then it does what a player who can see the exit does: it never stops on it. The route it plays is blind to
    // the exit, and the first measurement of docs/35 had about one such hero in eleven leave by accident, because
    // standing still to fight is what a hero does most of the time and the exit is simply part of the floor. So while it
    // means to stay, a stand on the exit becomes a step off it. Outside a swarm there is no exit and it only plays the
    // route.
    public sealed class ExitRoute : IHeroRoute
    {
        private readonly float _leaveAt;
        private readonly IHeroRoute _meanwhile;

        public ExitRoute(float leaveAt, IHeroRoute meanwhile = null)
        {
            if (float.IsNaN(leaveAt) || leaveAt < 0f)
                throw new ArgumentOutOfRangeException(nameof(leaveAt));
            _leaveAt = leaveAt;
            _meanwhile = meanwhile;
        }

        public void Steer(RouteView view, out float steerX, out float steerY)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            if (!view.HasExit || view.Seconds < _leaveAt)
            {
                steerX = 0f;
                steerY = 0f;
                _meanwhile?.Steer(view, out steerX, out steerY);
                if (view.HasExit && steerX * steerX + steerY * steerY < StandsBelow * StandsBelow &&
                    ExitRule.IsWithinReach(view.HeroX, view.HeroY, view.ExitX, view.ExitY))
                    StepOff(view, out steerX, out steerY);
                return;
            }

            if (ExitRule.IsWithinReach(view.HeroX, view.HeroY, view.ExitX, view.ExitY))
            {
                steerX = 0f;
                steerY = 0f;
                return;
            }

            // One body for the distance, never inlined, so the scene's assembly and the simulation's walk the same walk.
            float distanceSquared = ArenaFloor.FloorDistanceSquared(view.ExitX, view.ExitY, view.HeroX, view.HeroY);
            float length = (float)Math.Sqrt(distanceSquared);
            steerX = (view.ExitX - view.HeroX) / length;
            steerY = (view.ExitY - view.HeroY) / length;
        }

        // HeroMotion's dead band: a steer shorter than this holds still.
        private const float StandsBelow = 0.05f;

        // Straight away from the exit's centre, or back toward the arena's middle from dead on it.
        private static void StepOff(RouteView view, out float steerX, out float steerY)
        {
            float distanceSquared = ArenaFloor.FloorDistanceSquared(view.HeroX, view.HeroY, view.ExitX, view.ExitY);
            if (distanceSquared <= 0f)
            {
                steerX = 0f;
                steerY = view.ExitY < view.Platform.Middle ? 1f : -1f;
                return;
            }
            float length = (float)Math.Sqrt(distanceSquared);
            steerX = (view.HeroX - view.ExitX) / length;
            steerY = (view.HeroY - view.ExitY) / length;
        }
    }

    // The hero that fetches its drops: whenever nothing living is within its reach plus a margin, it walks straight at
    // the nearest reward lying on the floor; otherwise it plays the route it is given. The stand-in docs/37 measures what
    // walking for rewards is worth with, and the route the scene's parity case fetches with.
    public sealed class FetchRoute : IHeroRoute
    {
        private readonly IHeroRoute _meanwhile;
        private readonly float _margin;

        public FetchRoute(IHeroRoute meanwhile = null, float margin = 1.5f)
        {
            if (float.IsNaN(margin) || margin < 0f)
                throw new ArgumentOutOfRangeException(nameof(margin));
            _meanwhile = meanwhile;
            _margin = margin;
        }

        public void Steer(RouteView view, out float steerX, out float steerY)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            if (view.DropCount > 0 && !AnyWithin(view, view.HeroReach + _margin))
            {
                int nearest = -1;
                float best = float.MaxValue;
                for (int i = 0; i < view.DropCount && i < RouteView.MaxDrops; i++)
                {
                    float distanceSquared = ArenaFloor.FloorDistanceSquared(view.DropX[i], view.DropY[i], view.HeroX, view.HeroY);
                    if (distanceSquared < best)
                    {
                        best = distanceSquared;
                        nearest = i;
                    }
                }
                if (nearest >= 0 && best > 0f)
                {
                    float length = (float)Math.Sqrt(best);
                    steerX = (view.DropX[nearest] - view.HeroX) / length;
                    steerY = (view.DropY[nearest] - view.HeroY) / length;
                    return;
                }
            }

            steerX = 0f;
            steerY = 0f;
            _meanwhile?.Steer(view, out steerX, out steerY);
        }

        private static bool AnyWithin(RouteView view, float distance)
        {
            float limit = distance * distance;
            for (int i = 0; i < view.Count; i++)
            {
                if (!view.Alive[i])
                    continue;
                if (ArenaFloor.FloorDistanceSquared(view.EnemyX[i], view.EnemyY[i], view.HeroX, view.HeroY) <= limit)
                    return true;
            }
            return false;
        }
    }

    // The hero that never walks: the balance baseline every Descent number was measured against, kept as a route so a
    // standing run and a walking one go through exactly the same code.
    public sealed class StationaryRoute : IHeroRoute
    {
        public void Steer(RouteView view, out float steerX, out float steerY)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            steerX = 0f;
            steerY = 0f;
        }
    }
}
