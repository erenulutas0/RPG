namespace Cryptforge.Progression
{
    // The four things the Anvil upgrades (docs/54 section 3.1, the lead's decision 2026-11-16): the forged weapon itself
    // and the three slots that drop. The value is what is saved, so append, never reorder.
    public enum GearSlot
    {
        Weapon = 0,
        Armour = 1,
        Amulet = 2,
        Ring = 3
    }
}
