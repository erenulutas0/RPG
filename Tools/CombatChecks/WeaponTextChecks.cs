using System.Globalization;
using System.IO;
using System.Linq;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Daggers' card names their share on the elites (EliteRule; docs/41, status 2026-11-11): the forge's description
    // of the weapon - the rack's text and the run's offer of it (WeaponDefinition.CreateForgeOption and CreateCard) - read
    // from Weapon_Daggers.asset and formatted as the game formats it, says "Double damage to elites" while the share is
    // two. WeaponContentTests holds the same through Unity's asset database; this reads the asset's text where Unity is
    // not run.
    public sealed class WeaponTextChecks
    {
        [Test]
        public void TheDaggersCardSaysTheyDealDoubleToElites()
        {
            string asset = Path.Combine(RepositoryRoot(), "Assets", "_Project", "Data", "Weapons", "Weapon_Daggers.asset");
            string[] lines = File.ReadAllLines(asset);
            string Field(string name) =>
                lines.Select(l => l.Trim()).First(l => l.StartsWith(name + ":", System.StringComparison.Ordinal)).Substring(name.Length + 1).Trim();
            float Number(string name) => float.Parse(Field(name), CultureInfo.InvariantCulture);

            string format = Field("_forgeDescriptionFormat");
            Assert.That(format, Does.StartWith("'").And.EndWith("'"), "a single-quoted YAML string, as the asset writes it");
            format = format.Substring(1, format.Length - 2).Replace("''", "'");
            string description = string.Format(CultureInfo.InvariantCulture, format, Number("_damage"), Number("_interval"),
                Number("_splashFraction") * 100f, Number("_critMultiplier") * 100f,
                int.Parse(Field("_critEvery"), CultureInfo.InvariantCulture));

            Assert.That(Field("_id"), Is.EqualTo(EliteRule.DaggersId));
            Assert.That(EliteRule.DaggersShare, Is.EqualTo(2f), "double, as the card says");
            Assert.That(description, Is.EqualTo("6 damage every 0.5s. Crits for 200% once every 3 strikes. Double damage to elites"));
        }

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            Assert.That(directory, Is.Not.Null, "the repository holding Assets/_Project");
            return directory;
        }
    }
}
