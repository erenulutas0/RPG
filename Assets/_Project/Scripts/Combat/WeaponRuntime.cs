using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
        // A shrinking card can take a circle down to nothing, never inside out.
        private const float MinimumArea = 0f;

        // A rolled crit needs a multiplier even on a weapon whose pattern authors none, and doubling is the rhythm
        // the Daggers already use.
        private const float RolledCritMultiplier = 2f;

        private readonly ModifiableStat _damage;
        private readonly ModifiableStat _attackSpeed;
        private readonly ModifiableStat _range;
        private readonly ModifiableStat _critChance;
        // The share of this weapon's damage a pulse deals; 0 on every weapon until a card grants it (PulseRule).
        private readonly ModifiableStat _pulseDamage;
        // The share of a critical strike that splinters near its target; 0 until a card grants it (CritSplashRule).
        private readonly ModifiableStat _critSplash;
        // What every circle this weapon draws is multiplied by - the splash round its target, the pulse round its
        // attacker, the splinter round a crit - 1 until a card or a tome widens it (docs/42, the Reach tome). One
        // factor for all three, so Reach is worth something to the Daggers once they splinter, and nothing until then.
        private readonly ModifiableStat _area;
        private readonly float _baseInterval;
        private float _cooldown;

        public float Damage => _damage.Value;
        public float Interval => _baseInterval / _attackSpeed.Value;
        public float Range => _range.Value;
        // The chance, in [0, 1], that an attack crits on top of the pattern's fixed rhythm.
        public float CritChance => _critChance.Value;
        // The share of Damage a pulse deals, 0 for no pulse at all.
        public float PulseDamage => _pulseDamage.Value;
        // How many pulses this weapon has fired, for a report that wants to tell a pulse's work from a strike's.
        public int PulsesFired { get; private set; }
        // The share of a critical strike that splinters, 0 for no splinter at all.
        public float CritSplash => _critSplash.Value;
        public int SplintersFired { get; private set; }
        // The factor on every radius below, 1 for a weapon nothing has widened.
        public float Area => _area.Value;

        // The three circles, each its authored radius times Area. The scene's AttackController, the simulation and the
        // strike art all read them here and nowhere else, and each is one body that is never inlined: a radius is
        // squared into a reach comparison the moment it is read, and two copies of float arithmetic in two assemblies
        // have already disagreed in the last bit once (docs/21, the movement budget). At Area 1 each is its authored
        // radius exactly, since multiplying by 1 is exact.
        public float SplashRadius
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => Pattern.SplashRadius * _area.Value;
        }

        public float PulseRadius
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => PulseRule.Radius * _area.Value;
        }

        public float CritSplashRadius
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => CritSplashRule.Radius * _area.Value;
        }

        // The run seed the crit roll is drawn from; 0 for every weapon that never rolls, which is all of them until a
        // card grants a chance. A weapon in a loadout's later slot is handed a seed of its own (WeaponLoadout.SeedFor).
        public int Seed { get; set; }
        public AttackPattern Pattern { get; }
        // Successful attacks so far; the critical rhythm counts them.
        public int AttacksMade { get; private set; }
        public bool IsReady => _cooldown <= 0f;
        // Read-only presentation timing; observing it cannot advance cadence or apply damage.
        public float CooldownRemaining => _cooldown;
        public event Action StatsChanged;
        // Raised after a pulse has landed on everything it reached, for whatever draws it later. A pulse that found
        // nothing still raises it: the pulse happened, and the art owner decides whether an empty one is worth showing.
        public event Action Pulsed;

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
            _pulseDamage = new ModifiableStat(0f, 0f);
            _critSplash = new ModifiableStat(0f, 0f);
            _area = new ModifiableStat(1f, MinimumArea);
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

        // Whether a stat is this weapon's to carry rather than the hero's (HeroStats.Owns is the other half).
        public static bool Owns(UpgradeStat stat) =>
            stat == UpgradeStat.Damage || stat == UpgradeStat.AttackSpeed || stat == UpgradeStat.Range ||
            stat == UpgradeStat.CritChance || stat == UpgradeStat.PulseDamage || stat == UpgradeStat.CritSplash ||
            stat == UpgradeStat.Area;

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
                case UpgradeStat.PulseDamage:
                    _pulseDamage.AddModifier(modifier);
                    break;
                case UpgradeStat.CritSplash:
                    _critSplash.AddModifier(modifier);
                    break;
                case UpgradeStat.Area:
                    _area.AddModifier(modifier);
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

        // source is the attacker, passed on so targets know who hit them. nearby lists other enemies within
        // SplashRadius of the target, nearest first: a cleave strikes the first living one, an area attack every living
        // one, and a direct hit ignores the list. inPulseRadius lists the enemies within PulseRadius of the attacker
        // itself, which is a different circle from nearby's and drawn round a different centre; a caller whose weapon
        // has no pulse may leave it null, and every enemy weapon does. inCritSplashRadius is a third circle, round the
        // target again but at CritSplashRadius rather than the splash's, for the splinter a critical strike throws;
        // null unless a card granted one. The caller gathers all three, so this method never reads a radius itself.
        public bool TryAttack(IDamageable target, IDamageable source = null, IReadOnlyList<IDamageable> nearby = null,
            IReadOnlyList<IDamageable> inPulseRadius = null, IReadOnlyList<IDamageable> inCritSplashRadius = null)
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
            if (Pattern.Behavior != WeaponBehavior.DirectHit && nearby != null)
            {
                for (int i = 0; i < nearby.Count; i++)
                {
                    IDamageable other = nearby[i];
                    if (other == null || ReferenceEquals(other, target) || !other.IsAlive)
                        continue;
                    other.ApplyDamage(new DamageContext(damage * Pattern.SplashFraction, source, critical));
                    if (Pattern.Behavior == WeaponBehavior.Cleave)
                        break;
                }
            }

            // A critical strike splinters where it landed, off the critical damage it just dealt.
            Splinter(critical, damage, source, target, inCritSplashRadius);

            // The pulse, last and outside everything above: it reads the strike's number but not its damage, so a crit
            // can be the strike that triggers one and the pulse still lands flat. It is not a strike itself - no
            // cadence, no rhythm, no pulse of its own - and it reaches whatever stands near the attacker rather than
            // near what the attacker hit.
            Pulse(source, inPulseRadius);
            return true;
        }

        private void Splinter(bool critical, float criticalDamage, IDamageable source, IDamageable target,
            IReadOnlyList<IDamageable> inCritSplashRadius)
        {
            if (!CritSplashRule.Splinters(critical, CritSplash))
                return;

            SplintersFired++;
            float amount = CritSplashRule.DamageOf(criticalDamage, CritSplash);
            if (inCritSplashRadius == null || amount <= 0f)
                return;

            for (int i = 0; i < inCritSplashRadius.Count; i++)
            {
                IDamageable caught = inCritSplashRadius[i];
                // Never the struck enemy again: it took the crit itself.
                if (caught != null && !ReferenceEquals(caught, target) && caught.IsAlive)
                    caught.ApplyDamage(new DamageContext(amount, source));
            }
        }

        private void Pulse(IDamageable source, IReadOnlyList<IDamageable> inPulseRadius)
        {
            if (!PulseRule.Pulses(AttacksMade, PulseDamage))
                return;

            PulsesFired++;
            float amount = PulseRule.DamageOf(Damage, PulseDamage);
            if (inPulseRadius != null && amount > 0f)
            {
                for (int i = 0; i < inPulseRadius.Count; i++)
                {
                    IDamageable caught = inPulseRadius[i];
                    if (caught != null && caught.IsAlive)
                        caught.ApplyDamage(new DamageContext(amount, source));
                }
            }

            Pulsed?.Invoke();
        }

        private static void RequirePositiveFinite(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value <= 0f)
                throw new ArgumentOutOfRangeException(name);
        }
    }
}
