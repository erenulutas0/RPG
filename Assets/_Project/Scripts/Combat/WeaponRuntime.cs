using System;
using System.Collections.Generic;
using Cryptforge.Progression;

namespace Cryptforge.Combat
{
    // A per-attacker snapshot. Definitions are never mutated by combat; upgrades modify these runtime stats.
    public sealed class WeaponRuntime
    {
        // Safety floors, not balance values: damage cannot go negative and cadence cannot stall.
        private const float MinimumDamage = 0f;
        private const float MinimumAttackSpeed = 0.1f;
        private const float MinimumRange = 0.1f;

        // A rolled crit needs a multiplier even on a weapon whose pattern authors none, and doubling is the rhythm
        // the Daggers already use.
        private const float RolledCritMultiplier = 2f;

        private readonly ModifiableStat _damage;
        private readonly ModifiableStat _attackSpeed;
        private readonly ModifiableStat _range;
        private readonly ModifiableStat _critChance;
        private readonly float _baseInterval;
        private float _cooldown;

        public float Damage => _damage.Value;
        public float Interval => _baseInterval / _attackSpeed.Value;
        public float Range => _range.Value;
        // The chance, in [0, 1], that an attack crits on top of the pattern's fixed rhythm.
        public float CritChance => _critChance.Value;
        // The run seed the crit roll is drawn from; 0 for every weapon that never rolls, which is all of them until a
        // card grants a chance.
        public int Seed { get; set; }
        public AttackPattern Pattern { get; }
        // Successful attacks so far; the critical rhythm counts them.
        public int AttacksMade { get; private set; }
        public bool IsReady => _cooldown <= 0f;
        // Read-only presentation timing; observing it cannot advance cadence or apply damage.
        public float CooldownRemaining => _cooldown;
        public event Action StatsChanged;

        // initialDelay is a windup before the first attack only; later attacks follow the interval.
        public WeaponRuntime(float damage, float interval, float range, float initialDelay = 0f, AttackPattern pattern = default)
        {
            RequirePositiveFinite(damage, nameof(damage));
            RequirePositiveFinite(interval, nameof(interval));
            RequirePositiveFinite(range, nameof(range));
            if (float.IsNaN(initialDelay) || float.IsInfinity(initialDelay) || initialDelay < 0f)
                throw new ArgumentOutOfRangeException(nameof(initialDelay));

            _damage = new ModifiableStat(damage, MinimumDamage);
            _attackSpeed = new ModifiableStat(1f, MinimumAttackSpeed);
            _range = new ModifiableStat(range, MinimumRange);
            _critChance = new ModifiableStat(0f, 0f);
            _baseInterval = interval;
            _cooldown = initialDelay;
            Pattern = pattern;
        }

        private bool RollsCritical()
        {
            float chance = CritChance;
            if (chance <= 0f)
                return false;
            if (chance >= 1f)
                return true;

            const int Steps = 10000;
            return RunRandom.Stream(Seed, RunRandom.Crits, AttacksMade).NextBelow(Steps) < (int)(chance * Steps);
        }

        public void AddModifier(UpgradeStat stat, StatModifier modifier)
        {
            switch (stat)
            {
                case UpgradeStat.Damage:
                    _damage.AddModifier(modifier);
                    break;
                case UpgradeStat.AttackSpeed:
                    // An in-progress cooldown keeps its remaining time; the new cadence starts with the next attack.
                    _attackSpeed.AddModifier(modifier);
                    break;
                case UpgradeStat.Range:
                    _range.AddModifier(modifier);
                    break;
                case UpgradeStat.CritChance:
                    _critChance.AddModifier(modifier);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(stat));
            }

            StatsChanged?.Invoke();
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            _cooldown = Math.Max(0f, _cooldown - deltaTime);
        }

        // source is the attacker, passed on so targets know who hit them. nearby lists other enemies within the pattern's
        // splash radius of the target, nearest first: a cleave strikes the first living one, an area attack every living
        // one, and a direct hit ignores the list.
        public bool TryAttack(IDamageable target, IDamageable source = null, IReadOnlyList<IDamageable> nearby = null)
        {
            if (_cooldown > 0f || target == null || !target.IsAlive)
                return false;

            // Consume cadence before callbacks so an on-hit callback cannot attack recursively.
            _cooldown = Interval;
            AttacksMade++;
            // The fixed rhythm first, then the rolled chance: a rhythm crit never spends a draw, and a rolled one is a
            // function of the seed and the attack's number, so the scene and the simulation crit on the same swings.
            bool rhythm = Pattern.IsCritical(AttacksMade);
            bool rolled = !rhythm && RollsCritical();
            bool critical = rhythm || rolled;
            float multiplier = rhythm ? Pattern.CritMultiplier : RolledCritMultiplier;
            float damage = critical ? Damage * multiplier : Damage;
            target.ApplyDamage(new DamageContext(damage, source, critical));
            if (Pattern.Behavior == WeaponBehavior.DirectHit || nearby == null)
                return true;

            for (int i = 0; i < nearby.Count; i++)
            {
                IDamageable other = nearby[i];
                if (other == null || ReferenceEquals(other, target) || !other.IsAlive)
                    continue;
                other.ApplyDamage(new DamageContext(damage * Pattern.SplashFraction, source, critical));
                if (Pattern.Behavior == WeaponBehavior.Cleave)
                    break;
            }
            return true;
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
