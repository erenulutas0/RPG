using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // The weapon lines and tomes exist twice - as assets the scene reads and as the Descent simulation's restatement -
    // and numbers restated by hand drift (docs/21, the movement budget). These hold the two equal, card for card and
    // tier for tier, and hold the assets to the rule docs/42 sets for the report's 5.3: a weapon line on Damage is Flat.
    public sealed class WeaponContentTests
    {
        private static readonly string[] HeroWeapons = { "Sword", "Staff", "Daggers" };
        private static readonly string[] TomeAssets = { "Tome_Force", "Tome_Tempo", "Tome_Reach", "Tome_Precision", "Tome_Vitality" };

        private static WeaponDefinition Weapon(string name)
        {
            var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>($"Assets/_Project/Data/Weapons/Weapon_{name}.asset");
            Assert.That(weapon, Is.Not.Null, name);
            return weapon;
        }

        private static DescentSimulation.HeroWeapon Simulated(string name) => name switch
        {
            "Sword" => DescentSimulation.Sword(),
            "Staff" => DescentSimulation.Staff(),
            _ => DescentSimulation.Daggers()
        };

        private static void AssertSameCard(UpgradeOption expected, UpgradeOption actual, string where)
        {
            Assert.That(actual.Id, Is.EqualTo(expected.Id), where);
            Assert.That(actual.DisplayName, Is.EqualTo(expected.DisplayName), where);
            Assert.That(actual.DescriptionFormat, Is.EqualTo(expected.DescriptionFormat), where);
            Assert.That(actual.Stat, Is.EqualTo(expected.Stat), where);
            Assert.That(actual.MaxStacks, Is.EqualTo(expected.MaxStacks), where);
            foreach (UpgradeRarity rarity in new[] { UpgradeRarity.Common, UpgradeRarity.Rare, UpgradeRarity.Epic, UpgradeRarity.Gold })
            {
                Assert.That(actual.ModifierFor(rarity).Operation, Is.EqualTo(expected.ModifierFor(rarity).Operation),
                    $"{where} at {rarity}");
                Assert.That(actual.ModifierFor(rarity).Amount, Is.EqualTo(expected.ModifierFor(rarity).Amount),
                    $"{where} at {rarity}");
            }
        }

        [Test]
        public void TheSimulationsWeaponLinesAreTheAssets()
        {
            foreach (string name in HeroWeapons)
            {
                WeaponCard asset = Weapon(name).CreateCard();
                WeaponCard simulated = Simulated(name).CreateCard();
                Assert.That(asset.Id, Is.EqualTo(simulated.Id), name);
                Assert.That(asset.DisplayName, Is.EqualTo(simulated.DisplayName), name);
                Assert.That(asset.EvolvesWithTomeId, Is.EqualTo(simulated.EvolvesWithTomeId), name);
                Assert.That(asset.Lines.Count, Is.EqualTo(simulated.Lines.Count), name);
                for (int i = 0; i < asset.Lines.Count; i++)
                    AssertSameCard(simulated.Lines[i], asset.Lines[i], $"{name} line {i}");
            }
        }

        // The category-mode bases (docs/42, the owner's choice of 2026-10-26: the report's) as the simulation restates them.
        [Test]
        public void TheSimulationsCategoryBasesAreTheAssets()
        {
            foreach (string name in HeroWeapons)
            {
                Assert.That(Simulated(name).CreateCategoryRuntime().Damage, Is.EqualTo(Weapon(name).CategoryDamage), name);
                Assert.That(Simulated(name).CreateCategoryRuntime().Interval, Is.EqualTo(Weapon(name).CategoryInterval), name);
                Assert.That(Weapon(name).CreateCategoryRuntime().Damage, Is.EqualTo(Weapon(name).CategoryDamage), name);
            }
            Assert.That((Weapon("Sword").CategoryDamage, Weapon("Staff").CategoryDamage, Weapon("Daggers").CategoryDamage),
                Is.EqualTo((18f, 24f, 7f)), "The report's bases (docs/40 section 5.2).");
        }

        // The two V0.1 evolutions (docs/42, PR 5), and none on the Staff yet: as the simulation restates them, evolving
        // from their own weapon with the tome the asset names.
        [Test]
        public void TheSimulationsEvolutionsAreTheAssets()
        {
            foreach (string name in HeroWeapons)
            {
                WeaponCard asset = Weapon(name).CreateCard();
                WeaponCard simulated = Simulated(name).CreateCard();
                if (name == "Staff")
                {
                    Assert.That(asset.Evolution, Is.Null, "The Staff's evolution needs a behaviour that does not exist yet.");
                    Assert.That(simulated.Evolution, Is.Null);
                    continue;
                }

                Assert.That(asset.Evolution, Is.Not.Null, name);
                AssertSameCard(simulated.Evolution, asset.Evolution, $"{name} evolution");
                Assert.That(asset.Evolution.IsEvolution, Is.True, name);
                Assert.That(asset.Evolution.EvolvesFromId, Is.EqualTo(asset.Id), name);
                Assert.That(asset.Evolution.Category, Is.EqualTo(OfferCategory.WeaponUpgrade), name);
            }

            Assert.That(Weapon("Sword").Evolution.Stat, Is.EqualTo(UpgradeStat.PulseDamage));
            Assert.That(Weapon("Sword").EvolvesWithTome, Is.EqualTo("tome_force"));
            Assert.That(Weapon("Daggers").Evolution.Stat, Is.EqualTo(UpgradeStat.CritSplash));
            Assert.That(Weapon("Daggers").EvolvesWithTome, Is.EqualTo("tome_precision"));
        }

        // Each evolution is the general card that used to grant its stat, tier for tier: Forge Pulse for the Sword and
        // Splintering Crit for the Daggers, which leave the general pool in category mode for exactly this.
        [Test]
        public void EachEvolutionCarriesTheValuesOfTheCardItReplaces()
        {
            AssertSameTiers("Upgrade_Pulse", Weapon("Sword").Evolution);
            AssertSameTiers("Upgrade_CritSplash", Weapon("Daggers").Evolution);
        }

        private static void AssertSameTiers(string generalAsset, UpgradeDefinition evolution)
        {
            var general = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>($"Assets/_Project/Data/Upgrades/{generalAsset}.asset");
            Assert.That(general, Is.Not.Null, generalAsset);
            Assert.That(evolution.Stat, Is.EqualTo(general.Stat), generalAsset);
            Assert.That(evolution.Operation, Is.EqualTo(general.Operation), generalAsset);
            Assert.That(evolution.Amount, Is.EqualTo(general.Amount), generalAsset);
            Assert.That(evolution.RareAmount, Is.EqualTo(general.RareAmount), generalAsset);
            Assert.That(evolution.EpicAmount, Is.EqualTo(general.EpicAmount), generalAsset);
            Assert.That(evolution.GoldAmount, Is.EqualTo(general.GoldAmount), generalAsset);
            Assert.That(evolution.Requires, Is.Null, generalAsset);
            Assert.That(evolution.EvolvesFrom, Is.Null, generalAsset);
        }

        [Test]
        public void TheSimulationsTomesAreTheAssets()
        {
            UpgradeOption[] simulated = DescentSimulation.Tomes();
            Assert.That(simulated.Length, Is.EqualTo(TomeAssets.Length));
            for (int i = 0; i < TomeAssets.Length; i++)
            {
                var tome = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(
                    $"Assets/_Project/Data/Upgrades/Tomes/{TomeAssets[i]}.asset");
                Assert.That(tome, Is.Not.Null, TomeAssets[i]);
                AssertSameCard(simulated[i], tome.CreateOption(), TomeAssets[i]);
            }
        }

        // Every weapon asset that carries lines, not only the three the simulation restates, so a weapon added later is
        // held to the rule the moment it is authored.
        [Test]
        public void EveryWeaponLineOnDamageIsFlat()
        {
            int lines = 0;
            var ids = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:WeaponDefinition", new[] { "Assets/_Project/Data/Weapons" }))
            {
                var weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                for (int i = 0; i < weapon.LevelUpCount; i++)
                {
                    UpgradeDefinition line = weapon.LevelUpAt(i);
                    Assert.That(line, Is.Not.Null, $"{weapon.Id} line {i}");
                    Assert.That(ids.Add(line.Id), Is.True, $"{line.Id} is a line of two weapons");
                    if (line.Stat == UpgradeStat.Damage)
                        Assert.That(line.Operation, Is.EqualTo(ModifierOperation.Flat),
                            $"{weapon.Id}: {line.Id} would fall into the global damage bucket");
                    lines++;
                }
            }
            Assert.That(lines, Is.EqualTo(9), "Three lines on each of the Sword, the Staff and the Daggers.");
        }
    }
}
