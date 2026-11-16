using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Economy
{
    // The result screen's loot line as plain text (docs/54 phase 3; the Anvil's own screen is phase 4): what the run
    // banked - materials and boosters by count, items by name and rarity - and what a death took. Numbers in the invariant
    // culture, as all of the game's text (CultureTextTests).
    public static class LootText
    {
        private static readonly string[] MaterialNames = { "Ember Shard", "Quicksilver Bead", "Gloam Spore" };
        private static readonly string[] BoosterNames = { "Forge Seal", "Master's Hammer", "Efsun Scroll", "Binding Rune" };
        private static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Gold" };

        public static string Name(MaterialKind kind) => MaterialNames[(int)kind];
        public static string Name(BoosterKind kind) => BoosterNames[(int)kind];

        // "Ember Shard x4, Forge Seal x1, Cinder Ring (Rare)" for what was banked, and "Gloam Spore x1" for what was lost;
        // empty strings for none. An item a full bag melted reads "(Rare, melted)".
        public static void Describe(RunLoot loot, LootDeposit itemDeposits, out string banked, out string lost)
        {
            banked = string.Empty;
            lost = string.Empty;
            if (loot == null)
                return;
            LootSettlement settlement = loot.Settlement;
            var kept = new StringBuilder();
            var gone = new StringBuilder();
            for (int i = 0; i < GearCurrency.MaterialCount; i++)
            {
                int count = loot.SecuredMaterial((MaterialKind)i) + (settlement != null ? settlement.MaterialsKept[i] : loot.UnsecuredMaterial((MaterialKind)i));
                Count(kept, MaterialNames[i], count);
                if (settlement != null)
                    Count(gone, MaterialNames[i], settlement.MaterialsLost[i]);
            }
            for (int i = 0; i < GearCurrency.BoosterCount; i++)
            {
                int count = loot.SecuredBooster((BoosterKind)i) + (settlement != null ? settlement.BoostersKept[i] : loot.UnsecuredBooster((BoosterKind)i));
                Count(kept, BoosterNames[i], count);
                if (settlement != null)
                    Count(gone, BoosterNames[i], settlement.BoostersLost[i]);
            }
            IReadOnlyList<ItemDrop> items = loot.Items;
            for (int i = 0; i < items.Count; i++)
            {
                bool melted = itemDeposits != null && itemDeposits.Melted.Contains(items[i]);
                Separate(kept).Append(string.Format(CultureInfo.InvariantCulture, melted ? "{0} ({1}, melted)" : "{0} ({1})",
                    items[i].DisplayName, RarityNames[(int)items[i].Rarity]));
            }
            banked = kept.ToString();
            lost = gone.ToString();
        }

        // The result screen's short form, one line: "Ember 4, Bead 2, Seal 1, 2 items" banked and "Bead 1" lost; an item a
        // full bag melted counts as "1 melted". The Bag tab names each item; this only says how much the run brought.
        public static void Compact(RunLoot loot, LootDeposit itemDeposits, out string banked, out string lost)
        {
            banked = string.Empty;
            lost = string.Empty;
            if (loot == null)
                return;
            LootSettlement settlement = loot.Settlement;
            var kept = new StringBuilder();
            var gone = new StringBuilder();
            for (int i = 0; i < GearCurrency.MaterialCount; i++)
            {
                int count = loot.SecuredMaterial((MaterialKind)i) + (settlement != null ? settlement.MaterialsKept[i] : loot.UnsecuredMaterial((MaterialKind)i));
                Short(kept, GearText.MaterialShortName((MaterialKind)i), count);
                if (settlement != null)
                    Short(gone, GearText.MaterialShortName((MaterialKind)i), settlement.MaterialsLost[i]);
            }
            for (int i = 0; i < GearCurrency.BoosterCount; i++)
            {
                int count = loot.SecuredBooster((BoosterKind)i) + (settlement != null ? settlement.BoostersKept[i] : loot.UnsecuredBooster((BoosterKind)i));
                Short(kept, GearText.BoosterShortName((BoosterKind)i), count);
                if (settlement != null)
                    Short(gone, GearText.BoosterShortName((BoosterKind)i), settlement.BoostersLost[i]);
            }
            int melted = itemDeposits != null ? itemDeposits.Melted.Count : 0;
            int stored = loot.Items.Count - melted;
            if (stored > 0)
                Separate(kept).Append(stored == 1 ? "1 item" : stored.ToString(CultureInfo.InvariantCulture) + " items");
            if (melted > 0)
                Separate(kept).Append(melted.ToString(CultureInfo.InvariantCulture)).Append(" melted");
            banked = kept.ToString();
            lost = gone.ToString();
        }

        private static void Short(StringBuilder text, string name, int count)
        {
            if (count > 0)
                Separate(text).Append(name).Append(' ').Append(count.ToString(CultureInfo.InvariantCulture));
        }

        private static void Count(StringBuilder text, string name, int count)
        {
            if (count > 0)
                Separate(text).Append(string.Format(CultureInfo.InvariantCulture, "{0} x{1}", name, count));
        }

        private static StringBuilder Separate(StringBuilder text) => text.Length > 0 ? text.Append(", ") : text;
    }
}
