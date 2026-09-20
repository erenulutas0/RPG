using System;

namespace Cryptforge.Combat
{
    // When a critical strike splinters and how far. Pure, so the scene and the Descent simulation splinter on the same
    // swings.
    //
    // The card this serves is the Daggers, and the reason it can be is that crits are not evenly distributed. The
    // Daggers author a crit every third strike and swing every half second; the Sword and the Staff author no rhythm at
    // all and crit only from the chance Keen Edge grants. So the Daggers arrive critting twice a second having spent
    // nothing, while the Sword buys three cards to crit a quarter of the time at two thirds of the cadence. The Forge
    // Pulse had no such gate and belonged to whichever weapon struck hardest (docs/30); this one is gated on something a
    // light weapon owns outright.
    //
    // The contract, as agreed in docs/27 before this was written. Only a critical strike splinters, from the rhythm or
    // from a roll. A splinter is not a strike: no cadence, no rhythm, it cannot splinter again, and a kill it lands
    // starts nothing. It never reports itself critical, so a view that flashes crits flashes the strike and not its
    // spray. It is dealt off the critical damage rather than the plain damage, because the card is about crits and
    // should grow with a build that invests in them. An area weapon's splash and a splinter both land: the Staff's
    // splash already reached those enemies, and that overlap is deliberate and is what the measurement watches.
    public static class CritSplashRule
    {
        // How far a splinter reaches from the struck enemy, in floor units. Measured in docs/31.
        public const float Radius = 2f;

        // Whether this strike splinters: it has to have been critical, and the weapon has to carry a share.
        public static bool Splinters(bool critical, float splashShare) => critical && splashShare > 0f;

        // What a splinter deals: a share of what the critical strike itself dealt.
        public static float DamageOf(float criticalDamage, float splashShare)
        {
            if (float.IsNaN(criticalDamage) || float.IsInfinity(criticalDamage) || criticalDamage < 0f)
                throw new ArgumentOutOfRangeException(nameof(criticalDamage));
            if (float.IsNaN(splashShare) || float.IsInfinity(splashShare) || splashShare < 0f)
                throw new ArgumentOutOfRangeException(nameof(splashShare));

            return criticalDamage * splashShare;
        }
    }
}
