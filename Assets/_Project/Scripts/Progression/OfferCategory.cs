namespace Cryptforge.Progression
{
    // What kind of pick a level-up card is, once a run carries a loadout (docs/42, the target model; the report's 40
    // section 9.2). Every card built before categories existed is General, which is how every offer the game has made
    // so far is still exactly what it was. Telemetry will write these names, and a card may be serialized with one, so:
    // append, never reorder.
    public enum OfferCategory
    {
        // One of the authored cards a run has always been offered - in category mode only those that write to the
        // hero, since a card on a weapon stat would buff slot 0 alone.
        General = 0,
        // A weapon the hero does not carry yet, taken into the loadout's next slot.
        NewWeapon = 1,
        // One line of a carried weapon's own table, applied to that weapon only, raising its level.
        WeaponUpgrade = 2,
        // A tome the hero does not hold yet: a grant to every weapon it carries and will carry.
        NewTome = 3,
        // Another level of a tome the hero already holds.
        TomeUpgrade = 4
    }
}
