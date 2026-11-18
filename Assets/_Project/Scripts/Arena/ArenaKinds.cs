namespace Cryptforge.Arena
{
    // The two arenas of docs/55 section 9, answer 5. Ranked: the build's identity is kept (hero, weapon, skills, the types
    // of its efsun lines) and everything that is owned or rolled - the + level, the number of lines, each line's value - is
    // the same for both sides. Open: the gear counts in full, as the profile holds it.
    public enum ArenaMode
    {
        Ranked = 0,
        Open = 1
    }

    // The autopilot's habit, part of the build (docs/55 section 3.2, Raid's AI settings). Careful waits for each skill's
    // moment (a guard under 70 %, the Vent at four stacks); bold uses each skill whenever it can land.
    public enum ArenaPolicy
    {
        Careful = 0,
        Bold = 1
    }

    // How a weapon's strike lands (the run's WeaponBehavior and AttackPattern). One on one, a cleave or a blast has no
    // second target: the pattern is drawn by the view, and only the Daggers' rhythm (every third strike a crit) changes
    // the numbers.
    public enum ArenaWeaponPattern
    {
        DirectHit = 0,
        Cleave = 1,
        Area = 2
    }

    // The six skills of docs/57 as the duel plays them.
    public enum ArenaSkillKind
    {
        // Forge Burst: Power (scaled) to the foe within Radius of the caster.
        Burst = 0,
        // Iron Resolve: damage taken -Power per mille for DurationTicks.
        Guard = 1,
        // Shield Rush: closes up to Radius on the foe at once (stopping at the weapon's reach), strikes Power (scaled) and
        // dazes the foe StunTicks.
        Rush = 2,
        // Crucible Vent: spends the foe's burn stacks, Power (scaled) each, when the foe is within Radius.
        Vent = 3,
        // Flame Sigil: a glyph of Radius round the caster's feet for DurationTicks; a foe standing in it walks SlowPermille
        // slower, catches a burn stack every BurnTickTicks of the glyph's life, and its burn deals Power per mille more.
        Sigil = 4,
        // Cinder Ward: absorbs Power per mille of the hero's own maximum health (never the arena's stretched one) for
        // DurationTicks; every blow it absorbs gives the striker Stacks burn stacks.
        Ward = 5
    }

    // The gear's stats in GearStat's order (docs/54 section 6.1), their ids GearStatIds' - the arena reads the profile's
    // lines by id, never by the game's enum, so it needs no reference to the game's assembly (ArenaTablesTests pins the
    // ids and the values against the game's). Which ones the arena reads: ArenaRules.ArenaReads.
    public enum ArenaStat
    {
        Damage = 0,
        AttackSpeed = 1,
        CritChance = 2,
        MaxHealth = 3,
        Armour = 4,
        EliteDamage = 5,
        BurnDamage = 6,
        Area = 7,
        MoveSpeed = 8,
        PickupRadius = 9,
        AbilityCooldown = 10,
        Healing = 11,
        Luck = 12
    }

    // What the "why" summary credits (DuelCredits): the source of a health swing in a fighter's favour - damage it dealt or
    // damage it kept off itself. Append, never reorder: telemetry writes the ids.
    public enum ArenaEffect
    {
        // The weapon's strikes, less the shares the bonuses below take of them.
        Weapon = 0,
        // The extra of critical strikes: the base 10 % chance and the Daggers' every-third rhythm.
        Crits = 1,
        ForgeBurst = 2,
        IronResolve = 3,
        ShieldRush = 4,
        CrucibleVent = 5,
        // The Sigil's heat (its burn's extra inside the glyph); the stacks it adds burn as Ember Memory's.
        FlameSigil = 6,
        // What the ward absorbed.
        CinderWard = 7,
        // Ember Memory's burn, Ilya's passive.
        EmberBurn = 8,
        // The build's bonuses - ranked: its efsun line types; open: its whole gear (main stats and lines).
        Damage = 9,
        AttackSpeed = 10,
        Crit = 11,
        Health = 12,
        Armour = 13,
        BurnDamage = 14,
        MoveSpeed = 15,
        AbilityCooldown = 16,
        // The round's dealt boon (docs/57 section 7: variety is dealt, not pressed).
        Boon = 17
    }
}
