namespace Cryptforge.Combat
{
    public enum WeaponBehavior
    {
        // Strikes the target only.
        DirectHit,
        // Also strikes the nearest other enemy within the splash radius of the target, for a fraction of the damage.
        Cleave,
        // Also strikes every other enemy within the splash radius of the target, for a fraction of the damage.
        Area
    }
}
