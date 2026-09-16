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
