using System.Globalization;
using System.Threading;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Anvil's words and numbers (docs/54 phase 4): a value with its unit, a line, a main stat towards its next level,
    // gold with its separator - all in the invariant culture, whatever the phone's (a Turkish phone wrote "0,45" once,
    // CultureTextTests). Also in Tools/CombatChecks.
    public sealed class GearTextTests
    {
        [Test]
        public void NumbersAreInvariantOnATurkishPhone()
        {
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                Assert.That(GearText.Number(250), Is.EqualTo("2.5"));
                Assert.That(GearText.Number(300), Is.EqualTo("3"));
                Assert.That(GearText.Number(40), Is.EqualTo("0.4"));
                Assert.That(GearText.Number(125), Is.EqualTo("1.25"));
                Assert.That(GearText.Gold(12345), Is.EqualTo("12,345"));
                Assert.That(GearText.Value(GearStat.Damage, 250), Is.EqualTo("+2.5%"));
                Assert.That(GearText.Value(GearStat.CritChance, 80), Is.EqualTo("+0.8 pts"));
                Assert.That(GearText.Value(GearStat.Luck, 300), Is.EqualTo("+3"));
                Assert.That(GearText.Value(GearStat.AbilityCooldown, 400), Is.EqualTo("-4%"));
                Assert.That(GearText.Line(new EfsunLine(GearStat.AttackSpeed, 3)), Is.EqualTo("Attack speed +1.5%"));
                Assert.That(GearText.MainStatStep(GearSlot.Weapon, 7), Is.EqualTo("Damage +7% > +8.5%"));
                Assert.That(GearText.MainStatStep(GearSlot.Ring, 9), Is.EqualTo("Crit chance +3 pts  (max)"));
                Assert.That(GearText.Level(7), Is.EqualTo("+7"));
                Assert.That(GearText.Percent(22), Is.EqualTo("22%"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        [Test]
        public void EveryStatRaritySlotMaterialAndBoosterHasAName()
        {
            for (int i = 0; i < GearStatIds.Count; i++)
                Assert.That(GearText.StatName((GearStat)i), Is.Not.Empty);
            Assert.That(GearText.RarityName(UpgradeRarity.Gold), Is.EqualTo("Gold"));
            Assert.That(GearText.SlotName(GearSlot.Amulet), Is.EqualTo("Amulet"));
            Assert.That(GearText.MaterialName(MaterialKind.GloamSpore), Is.EqualTo("Gloam Spore"));
            Assert.That(GearText.MaterialShortName(MaterialKind.QuicksilverBead), Is.EqualTo("Bead"));
            Assert.That(GearText.BoosterName(BoosterKind.MastersHammer), Is.EqualTo("Master's Hammer"));
            Assert.That(GearText.BoosterShortName(BoosterKind.BindingRune), Is.EqualTo("Rune"));
        }
    }
}
