using System;
using Cryptforge.Combat;

namespace Cryptforge.Progression
{
    // One run's copy of the equipped relic. The scene reports each hit the hero takes; the relic reacts through the same
    // damage and heal paths as combat, so rewards, enrage and death rules still apply.
    public sealed class RelicRuntime
    {
        private bool _secondWindSpent;

        public RelicOption Relic { get; }
        public int Triggers { get; private set; }
        public event Action Triggered;

        public RelicRuntime(RelicOption relic)
        {
            Relic = relic ?? throw new ArgumentNullException(nameof(relic));
        }

        // Call after the hero lost health to attacker. A killing blow never triggers a relic.
        public bool OnHeroDamaged(IHealable hero, IDamageable attacker, float heroWeaponDamage)
        {
            if (hero == null)
                throw new ArgumentNullException(nameof(hero));
            if (float.IsNaN(heroWeaponDamage) || float.IsInfinity(heroWeaponDamage) || heroWeaponDamage < 0f)
                throw new ArgumentOutOfRangeException(nameof(heroWeaponDamage));
            if (!hero.IsAlive)
                return false;

            // Each branch records the trigger before its side effect, so a re-entrant callback sees a consistent count.
            switch (Relic.Effect)
            {
                case RelicEffect.CounterStrike:
                    if (attacker == null || !attacker.IsAlive || heroWeaponDamage == 0f)
                        return false;
                    Triggers++;
                    attacker.ApplyDamage(new DamageContext(heroWeaponDamage * Relic.Amount));
                    break;
                case RelicEffect.SecondWind:
                    if (_secondWindSpent || hero.Current > hero.Maximum * Relic.Threshold)
                        return false;
                    _secondWindSpent = true;
                    Triggers++;
                    hero.Heal(hero.Maximum * Relic.Amount);
                    break;
                default:
                    return false;
            }

            Triggered?.Invoke();
            return true;
        }
    }
}
