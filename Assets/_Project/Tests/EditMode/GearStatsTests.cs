using System;
using System.Collections.Generic;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The power cap (docs/54 section 7): the main stats by basma level, the per-stat caps and the index, DPS x EHP - 1, at
    // the model's sets (gear_economy.py: +5 +15 %, +7 +21 %, +9 +31 %, +9 with good lines +42 %, the ceiling +51 %). Also in
    // Tools/CombatChecks.
    public sealed class GearStatsTests
    {
        private static ItemInstance[] Set(int level, params EfsunLine[] linesOnEach)
        {
            return new[]
            {
                new ItemInstance(0, "weapon_sword", GearSlot.Weapon, UpgradeRarity.Rare, level, 0, Without(linesOnEach, GearStat.Damage)),
                new ItemInstance(1, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Gold, level, 0, Without(linesOnEach, GearStat.MaxHealth)),
                new ItemInstance(2, "item_ember_locket", GearSlot.Amulet, UpgradeRarity.Gold, level, 0, Without(linesOnEach, GearStat.AttackSpeed)),
                new ItemInstance(3, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Gold, level, 0, Without(linesOnEach, GearStat.CritChance))
            };
        }

        // A piece cannot carry its own main stat as a line; the model's "every item" sets skip it there too (they hit the
        // caps either way).
        private static List<EfsunLine> Without(EfsunLine[] lines, GearStat main)
        {
            var kept = new List<EfsunLine>();
            foreach (EfsunLine line in lines)
                if (line.Stat != main)
                    kept.Add(line);
            return kept;
        }

        [Test]
        public void MainStatsFollowTheSlots()
        {
            Assert.That(GearStats.MainStatOf(GearSlot.Weapon), Is.EqualTo(GearStat.Damage));
            Assert.That(GearStats.MainStatOf(GearSlot.Armour), Is.EqualTo(GearStat.MaxHealth));
            Assert.That(GearStats.MainStatOf(GearSlot.Amulet), Is.EqualTo(GearStat.AttackSpeed));
            Assert.That(GearStats.MainStatOf(GearSlot.Ring), Is.EqualTo(GearStat.CritChance));
            Assert.That(GearStats.MainValue(GearSlot.Weapon, 9), Is.EqualTo(1000), "+10 % at +9.");
            Assert.That(GearStats.MainValue(GearSlot.Amulet, 9), Is.EqualTo(500));
            Assert.That(GearStats.MainValue(GearSlot.Ring, 9), Is.EqualTo(300), "+3 points at +9.");
            Assert.That(GearStats.MainValue(GearSlot.Armour, 0), Is.Zero);
        }

        [Test]
        public void NoGearIsNoPower()
        {
            Assert.That(GearStats.Power(GearStats.Totals(new ItemInstance[0])), Is.Zero);
            Assert.That(GearStats.Power(GearStats.Totals(Set(0))), Is.Zero);
        }

        [Test]
        public void TheSetsReadAsTheModelReadsThem()
        {
            Assert.That(GearStats.Power(GearStats.Totals(Set(5))) / 100.0, Is.EqualTo(15).Within(0.5));
            Assert.That(GearStats.Power(GearStats.Totals(Set(7))) / 100.0, Is.EqualTo(21).Within(0.5));
            Assert.That(GearStats.Power(GearStats.Totals(Set(9))) / 100.0, Is.EqualTo(31).Within(0.6));
            ItemInstance[] good = Set(9, new EfsunLine(GearStat.Damage, 3), new EfsunLine(GearStat.CritChance, 3),
                new EfsunLine(GearStat.MaxHealth, 3), new EfsunLine(GearStat.MoveSpeed, 3));
            Assert.That(GearStats.Power(GearStats.Totals(good)) / 100.0, Is.EqualTo(42).Within(0.6));
            ItemInstance[] perfect = Set(9, new EfsunLine(GearStat.Damage, 5), new EfsunLine(GearStat.AttackSpeed, 5),
                new EfsunLine(GearStat.CritChance, 5), new EfsunLine(GearStat.MaxHealth, 5), new EfsunLine(GearStat.Armour, 5));
            Assert.That(GearStats.Power(GearStats.Totals(perfect)) / 100.0, Is.EqualTo(51).Within(0.6), "The ceiling.");
        }

        [Test]
        public void CapsBindTheTotals()
        {
            ItemInstance[] perfect = Set(9, new EfsunLine(GearStat.Damage, 5), new EfsunLine(GearStat.Luck, 5));
            int[] raw = GearStats.Totals(perfect, false);
            int[] capped = GearStats.Totals(perfect);
            Assert.That(raw[(int)GearStat.Damage], Is.EqualTo(1000 + 3 * 300));
            Assert.That(capped[(int)GearStat.Damage], Is.EqualTo(GearStats.Cap(GearStat.Damage)));
            Assert.That(capped[(int)GearStat.Luck], Is.EqualTo(1000), "Four luck lines at 5 cap at 10.");
        }

        [Test]
        public void AnItemRefusesItsOwnMainStatAsALine()
        {
            Assert.Throws<ArgumentException>(() => new ItemInstance(1, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Rare, 0, 0,
                new[] { new EfsunLine(GearStat.MaxHealth, 1) }));
            Assert.Throws<ArgumentException>(() => new ItemInstance(1, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Rare, 0, 0,
                new[] { new EfsunLine(GearStat.Damage, 1), new EfsunLine(GearStat.Damage, 2) }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemInstance(0, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Rare, 0, 0, null),
                "A bag item has a uid.");
        }
    }
}
