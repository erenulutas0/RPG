using System.Collections.Generic;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The difficulty tiers' words (docs/54 section 21) in the player's language (docs/60 pass 2): in English TierText says
    // exactly what DifficultyTiers' English says - what the tier row, the HUD, the pause and the result showed before pass
    // 2 - and in Turkish the table's words with the language's numbers, no key falling back. Each test leaves Loc as it
    // found it.
    public sealed class TierTextTests
    {
        private static readonly DifficultyTier[] All =
            { DifficultyTier.Forged, DifficultyTier.Tempered, DifficultyTier.Inferno, DifficultyTier.Abyssal };

        private Language? _override;
        private LocTable _table;
        private System.Action<string> _warn;
        private readonly List<string> _warnings = new List<string>();

        [SetUp]
        public void Remember()
        {
            _override = Loc.Override;
            _table = Loc.Table;
            _warn = Loc.Warn;
            _warnings.Clear();
        }

        [TearDown]
        public void Restore()
        {
            Loc.UseTable(_table);
            Loc.Override = _override;
            Loc.Warn = _warn;
        }

        private static LocTable RealTable() => LocalisationChecks.LoadTable(LocalisationChecks.Root());

        private void Speak(Language language)
        {
            Loc.UseTable(RealTable());
            Loc.Override = language;
            Loc.Warn = _warnings.Add;
        }

        [Test]
        public void InEnglishTheTiersWordsAreDifficultyTiersOwn()
        {
            Speak(Language.English);
            foreach (DifficultyTier tier in All)
            {
                Assert.That(TierText.Name(tier), Is.EqualTo(DifficultyTiers.Name(tier)), tier.ToString());
                Assert.That(TierText.UnlockHint(tier), Is.EqualTo(DifficultyTiers.UnlockHint(tier)), tier.ToString());
                Assert.That(TierText.Reward(tier), Is.EqualTo(DifficultyTiers.RewardText(tier)), tier.ToString());
            }
            Assert.That(TierText.BeforeFloor(DifficultyTier.Forged, "Floor 1"), Is.EqualTo("Floor 1"), "Forged's line as it was.");
            Assert.That(TierText.AfterTitle("Defeated", DifficultyTier.Forged), Is.EqualTo("Defeated"));
            Assert.That(TierText.AfterTitle("Defeated", DifficultyTier.Tempered),
                Is.EqualTo("Defeated  ·  <color=#E89848>Tempered</color>"));
            Assert.That(_warnings, Is.Empty, string.Join("\n", _warnings));
        }

        [Test]
        public void InTurkishTheTiersSpeakTheTablesWordsWithTurkishNumbers()
        {
            Speak(Language.Turkish);
            foreach (DifficultyTier tier in All)
            {
                // The key is the tier's own id: "tier.tempered".
                string key = "tier." + DifficultyTiers.Id(tier);
                Assert.That(Loc.Table.TryGet(key, out LocEntry entry), Is.True, key);
                Assert.That(TierText.Name(tier), Is.EqualTo(entry.Turkish).And.Not.EqualTo(DifficultyTiers.Name(tier)), key);
            }
            Assert.That(TierText.Reward(DifficultyTier.Forged), Is.EqualTo("x1"));
            Assert.That(TierText.Reward(DifficultyTier.Tempered), Is.EqualTo("x1,25"), "The decimal comma.");
            Assert.That(TierText.Reward(DifficultyTier.Inferno), Is.EqualTo("x1,5"));
            Assert.That(TierText.UnlockHint(DifficultyTier.Forged), Is.Empty);
            Assert.That(TierText.UnlockHint(DifficultyTier.Abyssal),
                Does.Contain(TierText.Name(DifficultyTier.Inferno)).And.Not.Contain("Descent"));
            Assert.That(TierText.BeforeFloor(DifficultyTier.Abyssal, "Kat 1"),
                Is.EqualTo("<color=#EEBF6C>" + TierText.Name(DifficultyTier.Abyssal) + "</color>  ·  Kat 1"));
            Assert.That(_warnings, Is.Empty, string.Join("\n", _warnings));
        }
    }
}
