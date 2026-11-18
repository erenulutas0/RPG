using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // What the ability does when fired. Burst strikes every enemy in reach for Damage (the Vanguard's Forge Burst). Vent
    // is the Cinder Arcanist's Crucible Vent (docs/45): every burning enemy in reach spends its Burn stacks at once, and
    // Damage is what each spent stack deals to it and to the enemies beside it (BurnRule.VentDamage). The four after them
    // are docs/57's skills 2 and 3 (SkillRule holds their shared arithmetic): Guard is the Vanguard's Iron Resolve, Amount
    // armour for Duration seconds; Rush its Shield Rush, Amount floor units in Duration seconds along a heading (HeroDash),
    // Damage to every enemy within Radius of the path and a daze; Sigil Ilya's Flame Sigil, a glyph of Radius at its feet
    // for Duration seconds slowing the walk inside by the share Amount (SigilZone, SlowState); Ward its Cinder Ward, a pool
    // of Amount of the hero's own maximum health for Duration seconds (HealthState.RaiseWard). Append, never renumber: the
    // assets store the number.
    public enum AbilityKind
    {
        Burst = 0,
        Vent = 1,
        Guard = 2,
        Rush = 3,
        Sigil = 4,
        Ward = 5
    }

    // The hero's active ability this run: a burst that strikes every enemy within its radius of the hero, then cools down.
    // Plain state like WeaponRuntime; the scene's AbilityController and the Descent simulation both drive it.
    public sealed class AbilityRuntime
    {
        public float Damage { get; }
        // Floor units from the hero.
        public float Radius { get; }
        // As the definition authors it, or shorter by the gear's ability-cooldown line (ShortenCooldown, at the run's start).
        public float Cooldown { get; private set; }
        public AbilityKind Kind { get; }
        // docs/57's skills (AbilityKind): how long the effect holds - the guard's armour, the Rush's travel, the glyph, the
        // ward - and its size - armour, floor units, the share of the walk slowed, the share of the maximum health warded.
        // Both 0 for the Burst and the Vent, which act at once.
        public float Duration { get; }
        public float Amount { get; }
        // Seconds until the ability is ready again; zero means ready.
        public float Remaining { get; private set; }
        // Seconds the last use's effect still holds (Duration from each use; 0 for the Burst and the Vent): whoever plays
        // the skill reads IsActive turning false as the effect's end.
        public float ActiveLeft { get; private set; }
        public int UsesMade { get; private set; }

        public bool IsReady => Remaining <= 0f;
        public bool IsActive => ActiveLeft > 0f;

        public AbilityRuntime(float damage, float radius, float cooldown, AbilityKind kind = AbilityKind.Burst)
        {
            if (kind != AbilityKind.Burst && kind != AbilityKind.Vent)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (!(damage > 0f) || float.IsInfinity(damage))
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!(radius > 0f) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (!(cooldown > 0f) || float.IsInfinity(cooldown))
                throw new ArgumentOutOfRangeException(nameof(cooldown));

            Damage = damage;
            Radius = radius;
            Cooldown = cooldown;
            Kind = kind;
        }

        // One of docs/57's skills 2 and 3 (Guard, Rush, Sigil, Ward), each with what its kind needs: a Rush deals damage,
        // the others none; radius is the reach the skill reads (the guard's and the ward's crowd, the Rush's path either
        // side, the glyph's); duration and amount as Duration and Amount say. The Burst and the Vent keep the constructor
        // above.
        public static AbilityRuntime Skill(AbilityKind kind, float damage, float radius, float cooldown, float duration,
            float amount) => new AbilityRuntime(kind, damage, radius, cooldown, duration, amount);

        private AbilityRuntime(AbilityKind kind, float damage, float radius, float cooldown, float duration, float amount)
        {
            if (kind != AbilityKind.Guard && kind != AbilityKind.Rush && kind != AbilityKind.Sigil && kind != AbilityKind.Ward)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (float.IsNaN(damage) || float.IsInfinity(damage) || damage < 0f || (kind == AbilityKind.Rush && !(damage > 0f)))
                throw new ArgumentOutOfRangeException(nameof(damage));
            if (!(radius > 0f) || float.IsInfinity(radius))
                throw new ArgumentOutOfRangeException(nameof(radius));
            if (!(cooldown > 0f) || float.IsInfinity(cooldown))
                throw new ArgumentOutOfRangeException(nameof(cooldown));
            if (!(duration > 0f) || float.IsInfinity(duration))
                throw new ArgumentOutOfRangeException(nameof(duration));
            // The glyph's slow is a share of the walk, under 1 (a glyph never roots); a ward's a share of the maximum health
            // up to all of it; armour and floor units any positive amount.
            bool share = kind == AbilityKind.Sigil ? amount >= 0f && amount < 1f
                : kind == AbilityKind.Ward ? amount > 0f && amount <= 1f
                : amount > 0f && !float.IsInfinity(amount);
            if (!share)
                throw new ArgumentOutOfRangeException(nameof(amount));

            Kind = kind;
            Damage = damage;
            Radius = radius;
            Cooldown = cooldown;
            Duration = duration;
            Amount = amount;
        }

        // The gear's ability cooldown (docs/54 section 20): every cooldown from now on is factor of the authored one, factor
        // in (0, 1] - 0.9 at the line's cap of 10 %. Called once as the run begins (GearStats.Apply), before any use; a
        // cooldown already running keeps what it has left. One body, never inlined, for the scene and the simulation.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ShortenCooldown(float factor)
        {
            if (!(factor > 0f) || factor > 1f)
                throw new ArgumentOutOfRangeException(nameof(factor));
            Cooldown *= factor;
        }

        public void Tick(float deltaTime)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Remaining > 0f)
                Remaining = Math.Max(0f, Remaining - deltaTime);
            if (ActiveLeft > 0f)
                ActiveLeft = Math.Max(0f, ActiveLeft - deltaTime);
        }

        // Starts the cooldown without striking - the scene deals a Vent itself, burn by burn - and a skill's effect for its
        // Duration. False while cooling down.
        public bool TrySpend()
        {
            if (!IsReady)
                return false;
            Remaining = Cooldown;
            ActiveLeft = Duration;
            UsesMade++;
            return true;
        }

        // Strikes every living target and starts the cooldown; a ready burst with nothing in reach is still spent, as the
        // player chose the moment. False while cooling down.
        public bool TryUse(IDamageable source, IReadOnlyList<IDamageable> targets)
        {
            if (targets == null)
                throw new ArgumentNullException(nameof(targets));
            if (!IsReady)
                return false;

            Remaining = Cooldown;
            UsesMade++;
            for (int i = 0; i < targets.Count; i++)
            {
                IDamageable target = targets[i];
                if (target != null && target.IsAlive)
                    target.ApplyDamage(new DamageContext(Damage, source));
            }
            return true;
        }
    }
}
