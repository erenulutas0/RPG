using System;

namespace Cryptforge.Combat
{
    // How an attack lands beyond its base damage: the splash of its behavior and its critical rhythm.
    // The default value is a plain direct hit that never crits.
    public readonly struct AttackPattern
    {
        public WeaponBehavior Behavior { get; }
        // How far from the target splash reaches, and the fraction of the attack's damage it deals; zero for DirectHit.
        public float SplashRadius { get; }
        public float SplashFraction { get; }
        // Every CritEvery-th attack is critical and multiplies its damage, splash included. Zero never crits and keeps the
        // multiplier at zero. A fixed rhythm instead of a chance keeps fights readable and simulations exact.
        public int CritEvery { get; }
        public float CritMultiplier { get; }

        public AttackPattern(WeaponBehavior behavior, float splashRadius = 0f, float splashFraction = 0f, int critEvery = 0,
            float critMultiplier = 0f)
        {
            switch (behavior)
            {
                case WeaponBehavior.DirectHit:
                    if (splashRadius != 0f || splashFraction != 0f)
                        throw new ArgumentException("A direct-hit weapon has no splash.");
                    break;
                case WeaponBehavior.Cleave:
                case WeaponBehavior.Area:
                    if (float.IsNaN(splashRadius) || float.IsInfinity(splashRadius) || splashRadius <= 0f)
                        throw new ArgumentOutOfRangeException(nameof(splashRadius));
                    if (!(splashFraction > 0f && splashFraction <= 1f))
                        throw new ArgumentOutOfRangeException(nameof(splashFraction), "Splash deals a fraction in (0, 1].");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(behavior));
            }

            if (critEvery < 0)
                throw new ArgumentOutOfRangeException(nameof(critEvery));
            bool validMultiplier = critEvery == 0
                ? critMultiplier == 0f
                : critMultiplier > 1f && !float.IsInfinity(critMultiplier);
            if (!validMultiplier)
                throw new ArgumentOutOfRangeException(nameof(critMultiplier), "Crits multiply damage by more than 1; no crits keep 0.");

            Behavior = behavior;
            SplashRadius = splashRadius;
            SplashFraction = splashFraction;
            CritEvery = critEvery;
            CritMultiplier = critMultiplier;
        }

        // attackNumber counts from 1 for the first attack.
        public bool IsCritical(int attackNumber) => CritEvery > 0 && attackNumber % CritEvery == 0;
    }
}
