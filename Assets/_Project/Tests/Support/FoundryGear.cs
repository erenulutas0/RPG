using System;
using System.Collections.Generic;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // docs/54 section 13's geared cohorts (phase 5): an autopilot cohort wearing one of section 7.2's modelled sets - all four
    // pieces (the hero's own weapon, an armour, an amulet, a ring) at one basma level, with no lines or with the "good"
    // four-line efsun on each (damage, crit, health and move speed at tier 3, less each piece's own main stat). Research only:
    // FoundryHero.Gear is GearLoadout.None unless a report wears one of these, so every shipped test, the batch and the gate
    // play ungeared. GearedCohortsResearch (Tools/CombatChecks) plays them by name (FOUNDRY_GEAR).
    internal sealed class GearedCohort
    {
        public string Name;
        public AutopilotCohort Cohort;
        public int Level;
        public bool GoodLines;

        // The hero wearing this cohort's set: its own weapon's gear and the three dropped pieces.
        public DescentSimulation.FoundryHero Wear(DescentSimulation.FoundryHero hero)
        {
            hero.Gear = GoodLines
                ? FoundryGear.Set(hero.Weapon.Id, Level, FoundryGear.GoodLines)
                : FoundryGear.Set(hero.Weapon.Id, Level);
            return hero;
        }

        public override string ToString() => Name;
    }

    internal static class FoundryGear
    {
        // Section 7.2's "good" efsun: damage, crit, health and move speed at tier 3 on every piece.
        public static readonly EfsunLine[] GoodLines =
        {
            new EfsunLine(GearStat.Damage, 3), new EfsunLine(GearStat.CritChance, 3), new EfsunLine(GearStat.MaxHealth, 3),
            new EfsunLine(GearStat.MoveSpeed, 3)
        };

        // Section 13's cohorts, in its order: careful at +5, +7, +9 and +9 with good lines, clumsy and the auto-farmer at +5;
        // and the auto-farmer's ladder further up, at +9 and +9 with good lines (the lead, 2026-11-17).
        public static readonly IReadOnlyList<GearedCohort> Cohorts = new[]
        {
            new GearedCohort { Name = "careful+5", Cohort = AutopilotCohort.Careful, Level = 5 },
            new GearedCohort { Name = "careful+7", Cohort = AutopilotCohort.Careful, Level = 7 },
            new GearedCohort { Name = "careful+9", Cohort = AutopilotCohort.Careful, Level = 9 },
            new GearedCohort { Name = "careful+9g", Cohort = AutopilotCohort.Careful, Level = 9, GoodLines = true },
            new GearedCohort { Name = "clumsy+5", Cohort = AutopilotCohort.Clumsy, Level = 5 },
            new GearedCohort { Name = "auto-farmer+5", Cohort = AutopilotCohort.AutoFarmer, Level = 5 },
            new GearedCohort { Name = "auto-farmer+9", Cohort = AutopilotCohort.AutoFarmer, Level = 9 },
            new GearedCohort { Name = "auto-farmer+9g", Cohort = AutopilotCohort.AutoFarmer, Level = 9, GoodLines = true }
        };

        // The modelled set (gear_economy.py's, GearStatsTests'): the weapon's gear and a Gold armour, amulet and ring, all
        // at level, each with linesOnEach less its own main stat (a piece cannot carry it; the sets hit the caps either way).
        public static GearLoadout Set(string weaponId, int level, params EfsunLine[] linesOnEach)
        {
            return new GearLoadout(new[]
            {
                new ItemInstance(0, weaponId, GearSlot.Weapon, UpgradeRarity.Rare, level, 0, Without(linesOnEach, GearStat.Damage)),
                new ItemInstance(1, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Gold, level, 0, Without(linesOnEach, GearStat.MaxHealth)),
                new ItemInstance(2, "item_ember_locket", GearSlot.Amulet, UpgradeRarity.Gold, level, 0, Without(linesOnEach, GearStat.AttackSpeed)),
                new ItemInstance(3, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Gold, level, 0, Without(linesOnEach, GearStat.CritChance))
            });
        }

        // FOUNDRY_GEAR's list: "all" (or nothing) for every cohort above, else the names separated by commas. An unknown name
        // throws, so a typo never measures the wrong thing.
        public static List<GearedCohort> Named(string list)
        {
            var named = new List<GearedCohort>();
            if (string.IsNullOrWhiteSpace(list) || list.Trim() == "all")
            {
                named.AddRange(Cohorts);
                return named;
            }
            foreach (string part in list.Split(','))
            {
                string name = part.Trim();
                if (name.Length == 0)
                    continue;
                GearedCohort found = null;
                foreach (GearedCohort cohort in Cohorts)
                {
                    if (cohort.Name == name)
                        found = cohort;
                }
                if (found == null)
                    throw new ArgumentException($"No geared cohort is named {name}.", nameof(list));
                named.Add(found);
            }
            return named;
        }

        private static List<EfsunLine> Without(EfsunLine[] lines, GearStat main)
        {
            var kept = new List<EfsunLine>();
            if (lines == null)
                return kept;
            foreach (EfsunLine line in lines)
            {
                if (line.Stat != main)
                    kept.Add(line);
            }
            return kept;
        }
    }
}
