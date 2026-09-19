namespace Cryptforge.Progression
{
    // What an upgrade card changes. Serialized by value in upgrade assets: append new stats, never reorder. Damage,
    // AttackSpeed, Range and CritChance live on WeaponRuntime; the rest live on HeroStats (docs/27 slice S2).
    public enum UpgradeStat
    {
        Damage = 0,
        AttackSpeed = 1,
        Range = 2,
        CritChance = 3,
        MaxHealth = 4,
        Armor = 5,
        MoveSpeed = 6,
        StaminaBar = 7,
        Luck = 8
    }
}
