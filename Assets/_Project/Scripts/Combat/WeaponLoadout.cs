using System;
using System.Collections.Generic;
using Cryptforge.Progression;

namespace Cryptforge.Combat
{
    // The weapons one hero carries in a run, in the order it picked them up: slot 0 is the weapon it started with, and
    // at most MaxSlots are ever held (docs/42, the target model; the report's 40 5.1). Pure, so the scene's
    // AttackController and the Descent simulation fire the same slots in the same order.
    //
    // A weapon is never held twice: a repeat pick is a level-up, and the level lives here beside the runtime it
    // describes. What a tome grants is global rather than per weapon, so AddGlobal keeps a record and replays it onto
    // every weapon added later - a hero that picks a weapon up at minute five gets the Force it has been stacking since
    // minute one, as the report's global bucket requires (40 5.3, 6.2 rule 7).
    public sealed class WeaponLoadout
    {
        public const int MaxSlots = 3;
        public const int MaxLevel = 8;

        // One held weapon. Its id is the WeaponDefinition's, which is what makes a second pick of it a level-up.
        public sealed class Slot
        {
            public string WeaponId { get; }
            public WeaponRuntime Runtime { get; }
            // 1 when picked up, MaxLevel at most.
            public int Level { get; internal set; }

            internal Slot(string weaponId, WeaponRuntime runtime)
            {
                WeaponId = weaponId;
                Runtime = runtime;
                Level = 1;
            }
        }

        private readonly List<Slot> _slots = new List<Slot>(MaxSlots);
        private readonly List<UpgradeStat> _globalStats = new List<UpgradeStat>();
        private readonly List<StatModifier> _globalModifiers = new List<StatModifier>();
        private readonly int _runSeed;

        // runSeed is the run's: every crit a weapon rolls is drawn from the seed this hands it (SeedFor).
        public WeaponLoadout(int runSeed)
        {
            _runSeed = runSeed;
        }

        public int Count => _slots.Count;
        public bool IsFull => _slots.Count == MaxSlots;
        // The weapon the hero started with, which everything written for one weapon reads; null until the first TryAdd.
        public WeaponRuntime Primary => _slots.Count > 0 ? _slots[0].Runtime : null;

        public Slot At(int index)
        {
            if (index < 0 || index >= _slots.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _slots[index];
        }

        public bool Owns(string weaponId) => IndexOf(weaponId) >= 0;

        public int IndexOf(string weaponId)
        {
            for (int i = 0; i < _slots.Count; i++)
                if (_slots[i].WeaponId == weaponId)
                    return i;
            return -1;
        }

        // Puts a weapon in the next slot, seeds its crits for that slot and replays every global grant onto it. False,
        // and nothing changed, when every slot is held or the hero already carries this weapon.
        public bool TryAdd(string weaponId, WeaponRuntime runtime)
        {
            if (string.IsNullOrEmpty(weaponId))
                throw new ArgumentException("A held weapon needs its definition's id.", nameof(weaponId));
            if (runtime == null)
                throw new ArgumentNullException(nameof(runtime));
            if (IsFull || Owns(weaponId))
                return false;
            for (int i = 0; i < _slots.Count; i++)
                if (ReferenceEquals(_slots[i].Runtime, runtime))
                    throw new InvalidOperationException("One runtime cannot fill two slots.");

            runtime.Seed = SeedFor(_runSeed, _slots.Count);
            for (int i = 0; i < _globalStats.Count; i++)
                runtime.AddModifier(_globalStats[i], _globalModifiers[i]);
            _slots.Add(new Slot(weaponId, runtime));
            return true;
        }

        // A repeat pick of a held weapon. False, and nothing changed, when the weapon is not held or is at MaxLevel.
        public bool TryLevelUp(string weaponId)
        {
            int index = IndexOf(weaponId);
            if (index < 0 || _slots[index].Level >= MaxLevel)
                return false;

            _slots[index].Level++;
            return true;
        }

        // A grant to every weapon the hero holds and every weapon it will hold: applied now to each slot in order, and
        // remembered for TryAdd. Only a weapon stat can be global here; the hero's own go to HeroStats.
        public void AddGlobal(UpgradeStat stat, StatModifier modifier)
        {
            if (!WeaponRuntime.Owns(stat))
                throw new ArgumentOutOfRangeException(nameof(stat), "Only a weapon stat is granted to every weapon.");

            _globalStats.Add(stat);
            _globalModifiers.Add(modifier);
            for (int i = 0; i < _slots.Count; i++)
                _slots[i].Runtime.AddModifier(stat, modifier);
        }

        // The seed a weapon in this slot rolls its crits from. Slot 0 keeps the run's own, so a one-weapon run crits on
        // exactly the swings it always has. A later slot derives its own from the run's, because a crit is drawn from
        // (seed, attack number): two weapons sharing one seed would crit on their third attacks together, every run.
        public static int SeedFor(int runSeed, int slot)
        {
            if (slot < 0 || slot >= MaxSlots)
                throw new ArgumentOutOfRangeException(nameof(slot));

            return slot == 0 ? runSeed : unchecked((int)RunRandom.Stream(runSeed, RunRandom.WeaponSeeds, slot).Next());
        }
    }
}
