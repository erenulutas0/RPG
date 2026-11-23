namespace Cryptforge.Combat
{
    // Appended, never renumbered: a WeaponDefinition serializes its behaviour by number.
    public enum WeaponBehavior
    {
        // Strikes the target only.
        DirectHit,
        // Also strikes the nearest other enemy within the splash radius of the target, for a fraction of the damage.
        Cleave,
        // Also strikes every other enemy within the splash radius of the target, for a fraction of the damage.
        Area,
        // Strikes every enemy within the splash radius of the wielder itself, for a fraction of the damage, whenever one
        // stands inside that circle: an aura (docs/63's Cinder Saw). The circle is drawn round the attacker, as a pulse's
        // is, not round what it struck; the strike's target is the nearest enemy inside it (WeaponRuntime.TargetReach).
        Aura
    }
}
