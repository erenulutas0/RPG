using System;

namespace Cryptforge.Combat
{
    // When a strike pulses and how far the pulse reaches. Pure, so the scene and the Descent simulation pulse on the
    // same swings.
    //
    // The contract, agreed in docs/27 before any of it was written and worth restating where it is enforced. Every Nth
    // strike pulses. A pulse is not a strike: it advances no cadence, counts toward no rhythm, and cannot pulse again,
    // so it never chains and a kill it lands starts nothing. A crit can be the strike that triggers one, but the pulse
    // itself never crits and its damage is never multiplied. An area strike counts once however many enemies it hits,
    // because the strike is the swing and not the landing. Relic damage is not a strike, which holds by construction:
    // relics call ApplyDamage and never TryAttack.
    //
    // The pulse is centred on the attacker rather than on what it hit, which is what makes it a short-reach weapon's
    // card: the Daggers fight from 1.8 units every half second, so they stand inside their own pulse and trigger it
    // twice as often as the Sword, while the Staff already owns the crowd and gains least.
    public static class PulseRule
    {
        // Every this-many-th strike pulses. Measured in docs/30.
        public const int Every = 4;
        // How far a pulse reaches from the attacker, in floor units. Measured in docs/30.
        public const float Radius = 2.2f;

        // Whether the strike at this number pulses. attackNumber counts from 1, as the critical rhythm does.
        public static bool Pulses(int attackNumber, float pulseShare)
        {
            if (attackNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(attackNumber));

            return pulseShare > 0f && attackNumber % Every == 0;
        }

        // What a pulse deals: a share of the weapon's damage, never its critical damage.
        public static float DamageOf(float weaponDamage, float pulseShare)
        {
            if (float.IsNaN(weaponDamage) || float.IsInfinity(weaponDamage) || weaponDamage < 0f)
                throw new ArgumentOutOfRangeException(nameof(weaponDamage));
            if (float.IsNaN(pulseShare) || float.IsInfinity(pulseShare) || pulseShare < 0f)
                throw new ArgumentOutOfRangeException(nameof(pulseShare));

            return weaponDamage * pulseShare;
        }
    }
}
