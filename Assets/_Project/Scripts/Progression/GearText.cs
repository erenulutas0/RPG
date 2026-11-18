using System;
using System.Globalization;
using Cryptforge.Localisation;

namespace Cryptforge.Progression
{
    // The gear's words and numbers as the Anvil's screens show them (docs/54 phase 4): stat names, a value with its unit,
    // a line, a rarity, a material and a booster. In the language the game speaks (Loc, docs/60), its numbers in that
    // language's format on purpose: "+2.5%" and "12,345" in English, "+%2,5" and "12.345" in Turkish - never the phone's
    // culture (CultureTextTests' lesson). Pure, so CombatChecks reads it.
    public static class GearText
    {
        private static readonly LocText[] StatNames =
        {
            new LocText("gear.stat.damage", "Damage"),
            new LocText("gear.stat.attack_speed", "Attack speed"),
            new LocText("gear.stat.crit_chance", "Crit chance"),
            new LocText("gear.stat.max_health", "Max health"),
            new LocText("gear.stat.armour", "Armour"),
            new LocText("gear.stat.elite_damage", "Damage vs elites"),
            new LocText("gear.stat.burn_damage", "Burn damage"),
            new LocText("gear.stat.area", "Area"),
            new LocText("gear.stat.move_speed", "Move speed"),
            new LocText("gear.stat.pickup_radius", "Pickup radius"),
            new LocText("gear.stat.ability_cooldown", "Ability cooldown"),
            new LocText("gear.stat.healing", "Healing"),
            new LocText("gear.stat.luck", "Luck")
        };

        private static readonly LocText[] RarityNames =
        {
            new LocText("gear.rarity.common", "Common"),
            new LocText("gear.rarity.rare", "Rare"),
            new LocText("gear.rarity.epic", "Epic"),
            new LocText("gear.rarity.gold", "Gold")
        };

        private static readonly LocText[] MaterialNames =
        {
            new LocText("gear.material.ember_shard", "Ember Shard"),
            new LocText("gear.material.quicksilver_bead", "Quicksilver Bead"),
            new LocText("gear.material.gloam_spore", "Gloam Spore")
        };

        private static readonly LocText[] MaterialShortNames =
        {
            new LocText("gear.material.ember_shard_short", "Ember"),
            new LocText("gear.material.quicksilver_bead_short", "Bead"),
            new LocText("gear.material.gloam_spore_short", "Spore")
        };

        private static readonly LocText[] BoosterNames =
        {
            new LocText("gear.booster.forge_seal", "Forge Seal"),
            new LocText("gear.booster.masters_hammer", "Master's Hammer"),
            new LocText("gear.booster.efsun_scroll", "Efsun Scroll"),
            new LocText("gear.booster.binding_rune", "Binding Rune")
        };

        private static readonly LocText[] BoosterShortNames =
        {
            new LocText("gear.booster.forge_seal_short", "Seal"),
            new LocText("gear.booster.masters_hammer_short", "Hammer"),
            new LocText("gear.booster.efsun_scroll_short", "Scroll"),
            new LocText("gear.booster.binding_rune_short", "Rune")
        };

        private static readonly LocText[] SlotNames =
        {
            new LocText("gear.slot.weapon", "Weapon"),
            new LocText("gear.slot.armour", "Armour"),
            new LocText("gear.slot.amulet", "Amulet"),
            new LocText("gear.slot.ring", "Ring")
        };

        public static string StatName(GearStat stat) => StatNames[Index((int)stat, StatNames.Length, nameof(stat))].Get();
        public static string RarityName(UpgradeRarity rarity) => RarityNames[Index((int)rarity, RarityNames.Length, nameof(rarity))].Get();
        public static string MaterialName(MaterialKind kind) => MaterialNames[Index((int)kind, MaterialNames.Length, nameof(kind))].Get();
        public static string MaterialShortName(MaterialKind kind) => MaterialShortNames[Index((int)kind, MaterialShortNames.Length, nameof(kind))].Get();
        public static string BoosterName(BoosterKind kind) => BoosterNames[Index((int)kind, BoosterNames.Length, nameof(kind))].Get();
        public static string BoosterShortName(BoosterKind kind) => BoosterShortNames[Index((int)kind, BoosterShortNames.Length, nameof(kind))].Get();
        public static string SlotName(GearSlot slot) => SlotNames[Index((int)slot, SlotNames.Length, nameof(slot))].Get();

        // A value in hundredths with its unit: "+2.5%", "+0.8 pts" for crit and armour, "+3" for luck, "-4%" for the
        // ability's cooldown (a line that shortens it). Turkish puts the sign before the number: "+%2,5".
        public static string Value(GearStat stat, int hundredths)
        {
            string number = Number(hundredths);
            switch (stat)
            {
                case GearStat.CritChance:
                case GearStat.Armour:
                    return Loc.Format("gear.value.points", "+{value} pts", ("value", number));
                case GearStat.Luck:
                    return "+" + number;
                case GearStat.AbilityCooldown:
                    return Loc.Format("gear.value.percent_less", "-{value}%", ("value", number));
                default:
                    return Loc.Format("gear.value.percent", "+{value}%", ("value", number));
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
                return Loc.Format("gear.main_stat_max", "{stat}  (max)", ("stat", MainStat(slot, level)));
            return StatName(stat) + " " + Value(stat, GearStats.MainValue(slot, level)) + " > " +
                   Value(stat, GearStats.MainValue(slot, level + 1));
        }

        // "+7".
        public static string Level(int level) => "+" + level.ToString(CultureInfo.InvariantCulture);

        // Gold with a thousands separator in the language's format: "12,345" in English, "12.345" in Turkish.
        public static string Gold(int gold) => gold.ToString("#,0", Loc.Numbers);

        // "20%" in English, "%20" in Turkish.
        public static string Percent(int percent) =>
            Loc.Format("gear.percent", "{value}%", ("value", percent.ToString(CultureInfo.InvariantCulture)));

        // Hundredths as the shortest number: 250 "2.5", 300 "3", 40 "0.4", 125 "1.25"; a comma for the point in Turkish.
        public static string Number(int hundredths)
        {
            int whole = Math.Abs(hundredths) / 100;
            int part = Math.Abs(hundredths) % 100;
            string text = whole.ToString(CultureInfo.InvariantCulture);
            if (part != 0)
                text += NumberFormatInfo.GetInstance(Loc.Numbers).NumberDecimalSeparator +
                        (part % 10 == 0 ? (part / 10).ToString(CultureInfo.InvariantCulture) : part.ToString("00", CultureInfo.InvariantCulture));
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
