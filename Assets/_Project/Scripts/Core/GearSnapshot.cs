namespace Cryptforge.Core
{
    // The profile's gear as plain values, as the save holds it and before ProfileGear.Restore has repaired it (docs/54
    // section 10). Nothing here is trusted: a hand-edited or damaged save may put anything in any field.
    public sealed class GearSnapshot
    {
        public GearRecord[] Items = new GearRecord[0];
        public GearRecord[] Weapons = new GearRecord[0];
        // The armour's, the amulet's and the ring's equipped uid, in that order; 0 for an empty slot.
        public int[] EquippedUids = new int[ProfileGear.EquipSlotCount];
        // By MaterialKind and BoosterKind.
        public int[] Materials = new int[Progression.GearCurrency.MaterialCount];
        public int[] Boosters = new int[Progression.GearCurrency.BoosterCount];
        public int NextItemUid = 1;
        public int AnvilSeed;
        public int AnvilDraws;
        public bool FirstArmourGiven;
        public int HighestTierCompleted;
        public int SelectedTier;
        // A reroll shown and not yet kept or discarded: "item:<uid>" or "weapon:<id>", and the new lines.
        public string PendingRerollKey;
        public GearLineRecord[] PendingRerollLines = new GearLineRecord[0];
    }

    // One piece: a bag item (Slot 1-3, a positive Uid) or a weapon's gear (ItemId the weapon's id; Uid and Slot ignored).
    public sealed class GearRecord
    {
        public int Uid;
        public string ItemId;
        public int Slot;
        public int Rarity;
        public int Level;
        public int Pity;
        public GearLineRecord[] Lines = new GearLineRecord[0];
    }

    public struct GearLineRecord
    {
        public string Stat;
        public int Tier;

        public GearLineRecord(string stat, int tier)
        {
            Stat = stat;
            Tier = tier;
        }
    }
}
