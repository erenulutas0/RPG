using System;
using System.Runtime.CompilerServices;
using Cryptforge.Combat;
using Cryptforge.Economy;

namespace Cryptforge.Core
{
    // Which drop the development autopilot steps aside for between fights, and for how long a walk it does: the
    // arithmetic and the bookkeeping DevelopmentAutopilot and the Descent simulation's copy of it (DescentSimulation.
    // RunFoundry) share, as one body, so the scene and the simulation pick the same drop on the same frame - the reason
    // ArenaFloor.FloorDistanceSquared is one body (docs/21, the movement budget). The policy - how far a drop may lie, how
    // much longer it may make the way, how long a walk may spend on drops - is the autopilot's (DropDetour, DetourSlack,
    // DetourBudget), which the simulation copies and FoundrySimulationSceneValueTests holds to it.
    public static class DropDetourRule
    {
        // How much longer the way to (wayX, wayY) gets for walking to the drop first, in floor units: hero to drop to the
        // way's end, less hero straight to the way's end. Nothing for a drop on the straight line ahead, twice its distance
        // for one straight behind. Never inlined, like the distance it is made of.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float AddedPath(float heroX, float heroY, float dropX, float dropY, float wayX, float wayY)
        {
            float toDrop = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(dropX, dropY, heroX, heroY));
            float onward = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(wayX, wayY, dropX, dropY));
            float straight = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(wayX, wayY, heroX, heroY));
            return toDrop + onward - straight;
        }

        // The nearest drop closer than reach to (x, y), the first fallen of equals: the goal, and true. False, with the
        // goal left at (x, y), when there is none.
        public static bool Nearest(GroundDrops drops, float x, float y, float reach, out float gx, out float gy) =>
            Pick(drops, x, y, reach, false, 0f, 0f, 0f, out gx, out gy);

        // The same among the drops that add at most slack to the way to (wayX, wayY) (AddedPath): the drops beside the
        // way, not behind it.
        public static bool NearestOnTheWay(GroundDrops drops, float x, float y, float reach, float wayX, float wayY,
            float slack, out float gx, out float gy) =>
            Pick(drops, x, y, reach, true, wayX, wayY, slack, out gx, out gy);

        private static bool Pick(GroundDrops drops, float x, float y, float reach, bool onTheWay, float wayX, float wayY,
            float slack, out float gx, out float gy)
        {
            gx = x;
            gy = y;
            float best = reach * reach;
            bool found = false;
            for (int i = 0; drops != null && i < drops.Count; i++)
            {
                GroundDrops.Drop drop = drops.At(i);
                float d = ArenaFloor.FloorDistanceSquared(x, y, drop.X, drop.Y);
                if (d >= best || (onTheWay && AddedPath(x, y, drop.X, drop.Y, wayX, wayY) > slack))
                    continue;
                best = d;
                gx = drop.X;
                gy = drop.Y;
                found = true;
            }
            return found;
        }

        // One pilot's drops across a walk: on the way to an altar or a marked hall it steps aside only for a drop beside
        // the way, and only for the budget's seconds of the walk; a fight, or a hall newly marked, gives the walk after it
        // the whole budget. Every frame it steers at a drop is charged, and none is chosen once the budget is spent, so a
        // walk spends at most the budget and a frame on drops however many fall beside it, and since the hero loses no
        // more ground in that time than it walks, the drops hold a walk at most twice the budget beyond its way. The slack
        // alone does not bound it: a drop just within the slack on either side in turn holds the hero on the spot.
        public sealed class Walk
        {
            private readonly float _reach;
            private readonly float _slack;
            private readonly float _budget;
            private float _spent;
            private bool _hall;

            public Walk(float reach, float slack, float budget)
            {
                _reach = reach;
                _slack = slack;
                _budget = budget;
            }

            // The seconds of this walk spent steering at drops.
            public float Spent => _spent;

            // Every frame the pilot chooses, before anything else: whether a node wave stands and whether a hall stands
            // marked.
            public void Track(bool fighting, bool hallMarked)
            {
                if (fighting || (hallMarked && !_hall))
                    _spent = 0f;
                _hall = hallMarked;
            }

            // The drop to steer at on the way to (wayX, wayY): the nearest within reach adding at most the slack, while
            // the budget lasts, this frame's deltaTime charged to it. False, with the goal left at (x, y), once the budget
            // is spent or when no drop lies beside the way.
            public bool Choose(GroundDrops drops, float x, float y, float wayX, float wayY, float deltaTime,
                out float gx, out float gy)
            {
                gx = x;
                gy = y;
                if (_spent >= _budget || !NearestOnTheWay(drops, x, y, _reach, wayX, wayY, _slack, out gx, out gy))
                    return false;
                _spent += deltaTime;
                return true;
            }
        }
    }
}
