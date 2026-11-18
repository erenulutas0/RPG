using System.Collections.Generic;
using System.Text;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;

namespace Cryptforge.Economy
{
    // The result screen's loot line as plain text (docs/54 phase 3; the Anvil's own screen is phase 4): what the run
    // banked - materials and boosters by count, items by name and rarity - and what a death took. In the language the game
    // speaks (Loc, docs/60; the names are GearText's), never the phone's culture (CultureTextTests).
    public static class LootText
    {
        public static string Name(MaterialKind kind) => GearText.MaterialName(kind);
        public static string Name(BoosterKind kind) => GearText.BoosterName(kind);

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
                Count(kept, GearText.MaterialName((MaterialKind)i), count);
                if (settlement != null)
                    Count(gone, GearText.MaterialName((MaterialKind)i), settlement.MaterialsLost[i]);
            }
            for (int i = 0; i < GearCurrency.BoosterCount; i++)
            {
                int count = loot.SecuredBooster((BoosterKind)i) + (settlement != null ? settlement.BoostersKept[i] : loot.UnsecuredBooster((BoosterKind)i));
                Count(kept, GearText.BoosterName((BoosterKind)i), count);
                if (settlement != null)
                    Count(gone, GearText.BoosterName((BoosterKind)i), settlement.BoostersLost[i]);
            }
            IReadOnlyList<ItemDrop> items = loot.Items;
            for (int i = 0; i < items.Count; i++)
            {
                bool melted = itemDeposits != null && itemDeposits.Melted.Contains(items[i]);
                string rarity = GearText.RarityName(items[i].Rarity);
                Separate(kept).Append(melted
                    ? Loc.Format("loot.item_melted", "{name} ({rarity}, melted)", ("name", items[i].DisplayName), ("rarity", rarity))
                    : Loc.Format("loot.item", "{name} ({rarity})", ("name", items[i].DisplayName), ("rarity", rarity)));
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
            // Two keys rather than a plural rule: Turkish counts without one ("1 eşya", "2 eşya").
            if (stored > 0)
                Separate(kept).Append(stored == 1 ? Loc.Text("loot.one_item", "1 item")
                    : Loc.Format("loot.items", "{count} items", ("count", stored)));
            if (melted > 0)
                Separate(kept).Append(Loc.Format("loot.melted", "{count} melted", ("count", melted)));
            banked = kept.ToString();
            lost = gone.ToString();
        }

        private static void Short(StringBuilder text, string name, int count)
        {
            if (count > 0)
                Separate(text).Append(Loc.Format("loot.short_count", "{name} {count}", ("name", name), ("count", count)));
        }

        private static void Count(StringBuilder text, string name, int count)
        {
            if (count > 0)
                Separate(text).Append(Loc.Format("loot.count", "{name} x{count}", ("name", name), ("count", count)));
        }

        private static StringBuilder Separate(StringBuilder text) => text.Length > 0 ? text.Append(", ") : text;
    }
}
