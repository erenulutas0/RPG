namespace Cryptforge.Progression
{
    // What an upgrade card changes. Serialized by value in upgrade assets: append new stats, never reorder. Damage,
    // AttackSpeed, Range, CritChance, PulseDamage, CritSplash and Area live on WeaponRuntime; the rest live on HeroStats
    // (docs/27 slice S2; WeaponRuntime.Owns and HeroStats.Owns split them).
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
        Luck = 8,
        // The share of the weapon's damage a pulse deals, zero until a card grants it. Behavioural rather than a plain
        // number, but it rides this pipeline all the same, so nothing that reads an offer had to learn about it
        // (docs/27, S5a).
        PulseDamage = 9,
        // The share of a critical strike that splinters into everything near its target, zero until a card grants it.
        // Behavioural like PulseDamage, and on the same pipeline for the same reason (docs/27, S5c).
        CritSplash = 10,
        // What the world multiplies its enemies by. Starts at 1 and only ever rises, and only an evolution raises it:
        // the cost of a card that is itself a reward for a full build (docs/27, S5b). Megabonk keeps Difficulty on the
        // character sheet for the same reason - it is a thing about the run, and the run is what the sheet describes.
        Difficulty = 11,
        // The factor on every circle a weapon draws - its splash, its pulse, its splinter - 1 until something widens it.
        // The Reach tome's stat (docs/42); a weapon stat, so it rides the weapon's pipeline like Range does.
        Area = 12
    }
}
