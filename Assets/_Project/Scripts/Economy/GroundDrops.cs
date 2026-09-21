using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;

namespace Cryptforge.Economy
{
    // Rewards on the ground (docs/27, S6d): in the Descent a kill's experience and gold fall where the enemy fell, and the
    // hero picks them up by walking over them.
    public static class DropRule
    {
        // How close the hero's feet must come to a drop, in floor units, to pick it up. Chosen by the owner from the three
        // docs/37 measured: at one unit a drop lies just outside where an enemy stops to strike, so nothing falls into the
        // hero's lap - a hero who stands never picks anything up and loses the Descent, and one who walks to its kills
        // finishes it more surely than the game before drops did. At two, a standing hero picked up four in five anyway and
        // walking changed nothing, which is what the criterion written for this constant would have shipped.
        public const float PickupRadius = 1f;

        // One body for the distance, never inlined, so the scene's assembly and the simulation's pick up on the same frame.
        public static bool IsWithinReach(float heroX, float heroY, float dropX, float dropY) =>
            ArenaFloor.FloorDistanceSquared(heroX, heroY, dropX, dropY) <= PickupRadius * PickupRadius;
    }

    // The drops lying on a floor, in the order they fell. Collecting pays the run exactly what the kill would have paid;
    // pure, so the scene and the Descent simulation hold the same drops and pick them up in the same order.
    public sealed class GroundDrops
    {
        public readonly struct Drop
        {
            public float X { get; }
            public float Y { get; }
            public int Experience { get; }
            public int Gold { get; }

            public Drop(float x, float y, int experience, int gold)
            {
                X = x;
                Y = y;
                Experience = experience;
                Gold = gold;
            }
        }

        private readonly RunState _run;
        private readonly List<Drop> _drops = new List<Drop>();

        public GroundDrops(RunState run)
        {
            _run = run ?? throw new ArgumentNullException(nameof(run));
        }

        public int Count => _drops.Count;
        public Drop At(int index) => _drops[index];

        // A drop fell. Raised after it lies, for whatever shows it.
        public event Action<Drop> Dropped;
        // A drop is about to pay the run - raised before the experience and gold are added, so a recorder can tell
        // kill gold from any other gold - and then it is gone.
        public event Action<Drop> Collecting;
        // The drops changed: one fell, some were picked up, or the floor was swept.
        public event Action Changed;

        public void Add(float x, float y, int experience, int gold)
        {
            if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                throw new ArgumentOutOfRangeException(nameof(x));
            if (experience < 0)
                throw new ArgumentOutOfRangeException(nameof(experience));
            if (gold < 0)
                throw new ArgumentOutOfRangeException(nameof(gold));

            var drop = new Drop(x, y, experience, gold);
            _drops.Add(drop);
            Dropped?.Invoke(drop);
            Changed?.Invoke();
        }

        // Picks up every drop within reach of the hero, oldest first, and returns how many. Nothing is picked up once the
        // run has ended: what lies on the floor at a death was never the hero's.
        public int CollectNear(float heroX, float heroY)
        {
            if (_run.HasEnded || _drops.Count == 0)
                return 0;

            int collected = 0;
            for (int i = 0; i < _drops.Count;)
            {
                Drop drop = _drops[i];
                if (!DropRule.IsWithinReach(heroX, heroY, drop.X, drop.Y))
                {
                    i++;
                    continue;
                }
                _drops.RemoveAt(i);
                Pay(drop);
                collected++;
            }
            if (collected > 0)
                Changed?.Invoke();
            return collected;
        }

        // The floor's last enemy has fallen: everything still lying flies to the hero, oldest first, so a floor pays at its
        // end exactly what its kills paid.
        public int CollectAll()
        {
            if (_run.HasEnded || _drops.Count == 0)
                return 0;

            int collected = _drops.Count;
            var sweep = new List<Drop>(_drops);
            _drops.Clear();
            foreach (Drop drop in sweep)
                Pay(drop);
            Changed?.Invoke();
            return collected;
        }

        private void Pay(Drop drop)
        {
            Collecting?.Invoke(drop);
            _run.AddExperience(drop.Experience);
            _run.AddGold(drop.Gold);
        }
    }
}
