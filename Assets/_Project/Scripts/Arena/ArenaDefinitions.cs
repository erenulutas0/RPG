using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // A hero as the arena reads it: the run-start kit's numbers in the duel's integer units (health in milli-points, speed
    // in milli-units a tick), its passive, its three skills, the weapons it may carry, and its arena coefficients - the
    // second balance table docs/55 section 3.2 says every arena needs, tuned by a round-robin (ArenaBalanceResearch), never
    // read by the Descent.
    public sealed class ArenaHero
    {
        public readonly string Id;
        public readonly string Name;
        // The hero's own maximum health (Hero_*.asset), milli-points. The arena's health is this x DuelRules.HealthScale x
        // HealthPermille; a skill that reads maximum health reads this one (docs/57 section 3.0).
        public readonly int Health;
        public readonly int SpeedPerTick;
        // Ember Memory (Ilya's passive): every weapon strike adds a burn stack.
        public readonly bool BurnOnHit;
        // The arena coefficients, per mille: on every point of damage the hero deals (strikes, skills, burn) and on its
        // arena health.
        public readonly int DamagePermille;
        public readonly int HealthPermille;
        private readonly string[] _skillIds;
        private readonly string[] _weaponIds;

        public ArenaHero(string id, string name, int health, int speedPerTick, bool burnOnHit, int damagePermille,
            int healthPermille, string[] skillIds, string[] weaponIds)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A hero id is required.", nameof(id));
            if (health <= 0 || speedPerTick <= 0 || damagePermille <= 0 || healthPermille <= 0)
                throw new ArgumentOutOfRangeException(nameof(health));
            if (skillIds == null || skillIds.Length != DuelRules.SkillSlots)
                throw new ArgumentException("A hero carries three skills.", nameof(skillIds));
            if (weaponIds == null || weaponIds.Length == 0)
                throw new ArgumentException("A hero carries at least one weapon.", nameof(weaponIds));
            Id = id;
            Name = name;
            Health = health;
            SpeedPerTick = speedPerTick;
            BurnOnHit = burnOnHit;
            DamagePermille = damagePermille;
            HealthPermille = healthPermille;
            _skillIds = (string[])skillIds.Clone();
            _weaponIds = (string[])weaponIds.Clone();
        }

        // The hero's three, in slot order (skill 1 the shipped one).
        public IReadOnlyList<string> SkillIds => _skillIds;
        // The first is the starting weapon.
        public IReadOnlyList<string> WeaponIds => _weaponIds;
        public string StartingWeaponId => _weaponIds[0];

        public bool Carries(string weaponId) => Array.IndexOf(_weaponIds, weaponId) >= 0;

        public ArenaHero WithCoefficients(int damagePermille, int healthPermille) =>
            new ArenaHero(Id, Name, Health, SpeedPerTick, BurnOnHit, damagePermille, healthPermille, _skillIds, _weaponIds);
    }

    // A weapon type and its attack pattern (Weapon_*.asset's category numbers: the damage a strike, the interval, the
    // reach) in the duel's units, and its arena coefficient on damage. A ranged weapon keeps its reach (Kites).
    public sealed class ArenaWeapon
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int Damage;
        public readonly int IntervalTicks;
        public readonly int Reach;
        public readonly bool Kites;
        public readonly ArenaWeaponPattern Pattern;
        // Every CritEvery-th strike is critical for RhythmPercent (the Daggers: every third, x2); 0 for none.
        public readonly int CritEvery;
        public readonly int RhythmPercent;
        public readonly int DamagePermille;

        public ArenaWeapon(string id, string name, int damage, int intervalTicks, int reach, bool kites,
            ArenaWeaponPattern pattern, int critEvery, int rhythmPercent, int damagePermille)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A weapon id is required.", nameof(id));
            if (damage <= 0 || intervalTicks <= 0 || reach <= 0 || damagePermille <= 0 || critEvery < 0)
                throw new ArgumentOutOfRangeException(nameof(damage));
            if ((critEvery == 0) != (rhythmPercent == 0))
                throw new ArgumentException("A rhythm needs both its count and its multiplier.", nameof(rhythmPercent));
            Id = id;
            Name = name;
            Damage = damage;
            IntervalTicks = intervalTicks;
            Reach = reach;
            Kites = kites;
            Pattern = pattern;
            CritEvery = critEvery;
            RhythmPercent = rhythmPercent;
            DamagePermille = damagePermille;
        }

        public ArenaWeapon WithCoefficient(int damagePermille) =>
            new ArenaWeapon(Id, Name, Damage, IntervalTicks, Reach, Kites, Pattern, CritEvery, RhythmPercent, damagePermille);
    }

    // One of docs/57's six skills at rank I; the duel plays it at ArenaRules.SkillRank (Power and SlowPermille grow by
    // DuelRules.RankStepPermille a rank, docs/57 section 4.3; never the cooldown).
    public sealed class ArenaSkill
    {
        public readonly string Id;
        public readonly string Name;
        public readonly ArenaSkillKind Kind;
        public readonly ArenaEffect Effect;
        public readonly int Power;
        public readonly int CooldownTicks;
        public readonly int Radius;
        public readonly int DurationTicks;
        public readonly int Stacks;
        public readonly int SlowPermille;
        public readonly int StunTicks;

        public ArenaSkill(string id, string name, ArenaSkillKind kind, ArenaEffect effect, int power, int cooldownSeconds,
            int radius = 0, int durationTicks = 0, int stacks = 0, int slowPermille = 0, int stunTicks = 0)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentException("A skill id is required.", nameof(id));
            if (power < 0 || cooldownSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(power));
            Id = id;
            Name = name;
            Kind = kind;
            Effect = effect;
            Power = power;
            CooldownTicks = cooldownSeconds * DuelRules.TicksPerSecond;
            Radius = radius;
            DurationTicks = durationTicks;
            Stacks = stacks;
            SlowPermille = slowPermille;
            StunTicks = stunTicks;
        }

        // A value at a rank: +6 % of the rank-I value a rank above the first (rank III +12 %, rank V +24 %).
        public static int AtRank(int value, int rank) =>
            (int)((long)value * (1000 + DuelRules.RankStepPermille * (rank - 1)) / 1000);
    }

    // A boon a round deals (docs/57 section 7, the lead's decision 8: Archero 2's variety comes from dealt boons, not more
    // buttons): a stat and its value in the gear's hundredths, for the round only, on top of the build and its caps.
    public sealed class ArenaBoon
    {
        public readonly string Id;
        public readonly string Name;
        public readonly ArenaStat Stat;
        public readonly int Hundredths;

        public ArenaBoon(string id, string name, ArenaStat stat, int hundredths)
        {
            Id = id;
            Name = name;
            Stat = stat;
            Hundredths = hundredths;
        }
    }
}
