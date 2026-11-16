using System;
using System.Globalization;

namespace Cryptforge.Progression
{
    // The gear's words and numbers as the Anvil's screens show them (docs/54 phase 4): stat names, a value with its unit,
    // a line, a rarity, a material and a booster. English, and every number in the invariant culture, so a Turkish phone
    // writes "+2.5%" and never "+2,5%" (CultureTextTests' lesson). Pure, so CombatChecks reads it.
    public static class GearText
    {
        private static readonly string[] StatNames =
        {
            "Damage", "Attack speed", "Crit chance", "Max health", "Armour", "Damage vs elites", "Burn damage", "Area",
            "Move speed", "Pickup radius", "Ability cooldown", "Healing", "Luck"
        };

        private static readonly string[] RarityNames = { "Common", "Rare", "Epic", "Gold" };
        private static readonly string[] MaterialNames = { "Ember Shard", "Quicksilver Bead", "Gloam Spore" };
        private static readonly string[] MaterialShortNames = { "Ember", "Bead", "Spore" };
        private static readonly string[] BoosterNames = { "Forge Seal", "Master's Hammer", "Efsun Scroll", "Binding Rune" };
        private static readonly string[] BoosterShortNames = { "Seal", "Hammer", "Scroll", "Rune" };
        private static readonly string[] SlotNames = { "Weapon", "Armour", "Amulet", "Ring" };

        public static string StatName(GearStat stat) => StatNames[Index((int)stat, StatNames.Length, nameof(stat))];
        public static string RarityName(UpgradeRarity rarity) => RarityNames[Index((int)rarity, RarityNames.Length, nameof(rarity))];
        public static string MaterialName(MaterialKind kind) => MaterialNames[Index((int)kind, MaterialNames.Length, nameof(kind))];
        public static string MaterialShortName(MaterialKind kind) => MaterialShortNames[Index((int)kind, MaterialShortNames.Length, nameof(kind))];
        public static string BoosterName(BoosterKind kind) => BoosterNames[Index((int)kind, BoosterNames.Length, nameof(kind))];
        public static string BoosterShortName(BoosterKind kind) => BoosterShortNames[Index((int)kind, BoosterShortNames.Length, nameof(kind))];
        public static string SlotName(GearSlot slot) => SlotNames[Index((int)slot, SlotNames.Length, nameof(slot))];

        // A value in hundredths with its unit: "+2.5%", "+0.8 pts" for crit and armour, "+3" for luck, "-4%" for the
        // ability's cooldown (a line that shortens it).
        public static string Value(GearStat stat, int hundredths)
        {
            string number = Number(hundredths);
            switch (stat)
            {
                case GearStat.CritChance:
                case GearStat.Armour:
                    return "+" + number + " pts";
                case GearStat.Luck:
                    return "+" + number;
                case GearStat.AbilityCooldown:
                    return "-" + number + "%";
                default:
                    return "+" + number + "%";
            }
        }

        // "Damage +2%".
        public static string Line(EfsunLine line) => StatName(line.Stat) + " " + Value(line.Stat, line.Value);

        // A piece's main stat at its level: "Damage +7%"; and towards the next: "Damage +7% > +8.5%".
        public static string MainStat(GearSlot slot, int level)
        {
            GearStat stat = GearStats.MainStatOf(slot);
            return StatName(stat) + " " + Value(stat, GearStats.MainValue(slot, level));
        }

        public static string MainStatStep(GearSlot slot, int level)
        {
            GearStat stat = GearStats.MainStatOf(slot);
            if (level >= AnvilTable.MaxLevel)
                return MainStat(slot, level) + "  (max)";
            return StatName(stat) + " " + Value(stat, GearStats.MainValue(slot, level)) + " > " +
                   Value(stat, GearStats.MainValue(slot, level + 1));
        }

        // "+7".
        public static string Level(int level) => "+" + level.ToString(CultureInfo.InvariantCulture);

        // Gold with a thousands separator in the invariant culture: "12,345".
        public static string Gold(int gold) => gold.ToString("#,0", CultureInfo.InvariantCulture);

        public static string Percent(int percent) => percent.ToString(CultureInfo.InvariantCulture) + "%";

        // Hundredths as the shortest number: 250 "2.5", 300 "3", 40 "0.4", 125 "1.25".
        public static string Number(int hundredths)
        {
            int whole = Math.Abs(hundredths) / 100;
            int part = Math.Abs(hundredths) % 100;
            string text = whole.ToString(CultureInfo.InvariantCulture);
            if (part != 0)
                text += "." + (part % 10 == 0 ? (part / 10).ToString(CultureInfo.InvariantCulture) : part.ToString("00", CultureInfo.InvariantCulture));
            return hundredths < 0 ? "-" + text : text;
        }

        private static int Index(int index, int count, string name)
        {
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(name);
            return index;
        }
    }
}
