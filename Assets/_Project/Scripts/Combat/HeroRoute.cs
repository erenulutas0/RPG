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
