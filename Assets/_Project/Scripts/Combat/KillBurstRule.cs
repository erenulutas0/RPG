using System;

namespace Cryptforge.Combat
{
    // When a kill bursts and how far. Pure, so the scene and the Descent simulation burst on the same kills.
    //
    // The card this serves is the Staff's evolution, Cinder Nova, which docs/42 left open: the Sword's evolution is the
    // Forge Pulse and the Daggers' the Splintering Crit, each measured as its weapon's card (docs/30, docs/31), and the
    // Staff gained least from both - +9 % from the pulse, +2 % from the splinter - because it already owns the crowd.
    // So its card is gated on what the Staff has more of than anyone: kills in one swing. An area strike fells a crowd
    // at once, a cleave one or two enemies and a direct hit one, so the same burst is worth most where the strike lands
    // widest. It evolves with Reach, which widens both the splash that does the felling and the burst that follows.
    //
    // The contract, in the style docs/27 set for the pulse and the splinter. Only a strike's kills burst: the struck
    // enemy and whatever the strike's own splash or cleave felled, each once, centred on where it fell. A burst is not a
    // strike: no cadence, no rhythm, and a kill it lands bursts nothing, so it never chains and one strike can burst at
    // most once for each enemy it felled. A pulse's kills and a splinter's start nothing either, as their own contracts
    // say. A burst never reaches the enemy it bursts from, which is already dead, and never reports itself critical. It
    // is dealt off the weapon's plain damage and never its critical damage, so a crit can be the blow that fells an
    // enemy and the burst still lands flat: this card grows with Reach, not with crits, and Precision stays the Daggers'
    // tome. An area weapon's splash and a burst both land, as a splash and a splinter do: the enemies round a fallen one
    // were usually inside the splash already.
    public static class KillBurstRule
    {
        // How far a burst reaches from the enemy that fell, in floor units, before the weapon's Area widens it. Not yet
        // measured: smaller than the splinter's 2.0 because one strike can burst once for every enemy it fells.
        public const float Radius = 1.6f;

        // Whether this enemy bursts: the strike has to have felled it, and the weapon has to carry a share.
        public static bool Bursts(bool felledByTheStrike, float burstShare) => felledByTheStrike && burstShare > 0f;

        // What a burst deals: a share of the weapon's damage, never its critical damage.
        public static float DamageOf(float weaponDamage, float burstShare)
        {
            if (float.IsNaN(weaponDamage) || float.IsInfinity(weaponDamage) || weaponDamage < 0f)
                throw new ArgumentOutOfRangeException(nameof(weaponDamage));
            if (float.IsNaN(burstShare) || float.IsInfinity(burstShare) || burstShare < 0f)
                throw new ArgumentOutOfRangeException(nameof(burstShare));

            return weaponDamage * burstShare;
        }
    }
}
