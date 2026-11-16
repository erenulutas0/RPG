namespace Cryptforge.Progression
{
    // The floor materials basma spends from +4 (docs/54 section 4.2): each floor of the Descent drops its own, and the
    // deeper the level, the deeper the floor it asks for. The value is what is saved and logged; append, never reorder.
    public enum MaterialKind
    {
        EmberShard = 0,
        QuicksilverBead = 1,
        GloamSpore = 2
    }

    // The boosters that drop in a run (docs/54 section 5). Append, never reorder.
    public enum BoosterKind
    {
        // +10 points on one strike.
        ForgeSeal = 0,
        // One strike up to +6 cannot fail.
        MastersHammer = 1,
        // One efsun reroll without gold.
        EfsunScroll = 2,
        // Adds a line to an item with fewer than five.
        BindingRune = 3
    }

    // Counts and stable ids for the two, for the save and telemetry.
    public static class GearCurrency
    {
        public const int MaterialCount = 3;
        public const int BoosterCount = 4;

        private static readonly string[] MaterialIds = { "ember_shard", "quicksilver_bead", "gloam_spore" };
        private static readonly string[] BoosterIds = { "forge_seal", "masters_hammer", "efsun_scroll", "binding_rune" };

        public static string Id(MaterialKind kind) => MaterialIds[(int)kind];
        public static string Id(BoosterKind kind) => BoosterIds[(int)kind];

        // The material a floor of the Descent drops: the Foundry's, the Vaults' and the Gardens'; false for the finale
        // and anything deeper, which drop none.
        public static bool TryMaterialOfFloor(int floorNumber, out MaterialKind kind)
        {
            switch (floorNumber)
            {
                case 1:
                    kind = MaterialKind.EmberShard;
                    return true;
                case 2:
                    kind = MaterialKind.QuicksilverBead;
                    return true;
                case 3:
                    kind = MaterialKind.GloamSpore;
                    return true;
                default:
                    kind = MaterialKind.EmberShard;
                    return false;
            }
        }
    }
}
