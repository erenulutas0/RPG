using System.Globalization;
using System.Threading;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // The game's text is English, and its numbers were written in the phone's culture: a Turkish phone showed "+0,45 reach"
    // on a level-up card. Every player-facing format names the invariant culture; these run the card, the forge and the
    // checkpoint with the thread's culture set to tr-TR, whose decimal separator is a comma, and read a point.
    public sealed class CultureTextTests
    {
        private CultureInfo _culture;
        private CultureInfo _uiCulture;

        [SetUp]
        public void UseTurkish()
        {
            _culture = Thread.CurrentThread.CurrentCulture;
            _uiCulture = Thread.CurrentThread.CurrentUICulture;
            CultureInfo turkish = CultureInfo.GetCultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = turkish;
            Thread.CurrentThread.CurrentUICulture = turkish;
            Assert.That(string.Format("{0:0.00}", 0.45f), Is.EqualTo("0,45"), "tr-TR writes a comma, or this proves nothing");
        }

        [TearDown]
        public void RestoreCulture()
        {
            Thread.CurrentThread.CurrentCulture = _culture;
            Thread.CurrentThread.CurrentUICulture = _uiCulture;
        }

        [Test]
        public void ALevelUpCardWritesItsDecimalWithAPoint()
        {
            try
            {
                var run = new RunState(10);
                var upgrades = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[]
                {
                    new UpgradeOption("upgrade_range", "Long Reach", "+{0:0.0#} reach", UpgradeStat.Range,
                        new StatModifier(ModifierOperation.Flat, 0.45f), 3)
                }, 1);
                var choices = new RunChoices(upgrades, new ForgeService(run, new HealthState(100f)));

                run.AddExperience(10);

                Assert.That(choices.Current.Kind, Is.EqualTo(ChoiceKind.Upgrade));
                Assert.That(choices.Current.Cards[0].Description, Is.EqualTo("+0.45 reach"));
            }
            finally
            {
                RestoreCulture();
            }
        }

        [Test]
        public void TheForgeWritesTheDaggersIntervalWithAPoint()
        {
            try
            {
                var daggers = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/_Project/Data/Weapons/Weapon_Daggers.asset");
                Assert.That(daggers, Is.Not.Null);

                string forge = daggers.CreateForgeOption().Description;

                Assert.That(forge, Does.Contain("every 0.5s"));
                Assert.That(forge, Does.Not.Contain("0,5"));
            }
            finally
            {
                RestoreCulture();
            }
        }

        [Test]
        public void TheCheckpointWritesItsNumbersWithAPoint()
        {
            try
            {
                CheckpointOption[] options = CombatSetup.CheckpointOptions("Extract", "Bank {0:0.00} gold", "Descend",
                    "Secure {0:0.0} gold. {1}: {2}", 96, "Quicksilver Vaults", "Enemies move faster");

                Assert.That(options[0].Kind, Is.EqualTo(CheckpointKind.Extract));
                Assert.That(options[0].Description, Is.EqualTo("Bank 96.00 gold"));
                Assert.That(options[1].Kind, Is.EqualTo(CheckpointKind.Descend));
                Assert.That(options[1].Description, Is.EqualTo("Secure 96.0 gold. Quicksilver Vaults: Enemies move faster"));
            }
            finally
            {
                RestoreCulture();
            }
        }
    }
}
