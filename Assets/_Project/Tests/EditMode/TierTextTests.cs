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
                Does.Contain(TierText.Name(DifficultyTier.Inferno)).And.Not.Contain("Ascent"));
            Assert.That(TierText.BeforeFloor(DifficultyTier.Abyssal, "Kat 1"),
                Is.EqualTo("<color=#EEBF6C>" + TierText.Name(DifficultyTier.Abyssal) + "</color>  ·  Kat 1"));
            Assert.That(_warnings, Is.Empty, string.Join("\n", _warnings));
        }

        // The line under the tier row in English, byte for byte as the tiers branch wrote it with its own formats - the hero
        // climbing since 2026-11-22: "the Ascent" where it said "the Descent" (docs/60 section 10).
        [Test]
        public void InEnglishTheTierLineIsAsItWas()
        {
            Speak(Language.English);
            Assert.That(MapChoiceView.DescribeTier(DifficultyTier.Forged, 0),
                Is.EqualTo("Forged: the Ascent as it was first forged; rewards x1\nBuilt for no gear. Your gear: +0%"));
            Assert.That(MapChoiceView.DescribeTier(DifficultyTier.Tempered, 1470),
                Is.EqualTo("Tempered: foes +8% health, +2% damage; rewards x1.25; item luck +10\nBuilt for gear +15% (a +5 set). Your gear: +14.7%"));
            Assert.That(MapChoiceView.DescribeTier(DifficultyTier.Inferno, 0),
                Is.EqualTo("Inferno: foes +15% health, +10% damage; rewards x1.5; item luck +20\nBuilt for gear +31% (a +9 set). Your gear: +0%"));
            Assert.That(MapChoiceView.DescribeTier(DifficultyTier.Abyssal, 4190),
                Is.EqualTo("Abyssal: foes +20% health, +14% damage; rewards x2; item luck +30\nBuilt for gear +42% (+9 with good efsun). Your gear: +41.9%"));
            Assert.That(MapChoiceView.Describe(1), Is.EqualTo(
                "Lava-lit halls where every Ascent begins. The Forge Warden keeps the gate.\nFloor 1. A fresh hero, the whole Ascent ahead."));
            Assert.That((MapChoiceView.HeroAndMapTitle, MapChoiceView.SelectedText, MapChoiceView.ChooseText, MapChoiceView.LockedWord),
                Is.EqualTo(("Choose your hero and map", "Selected", "Tap to choose", "Locked")));
            Assert.That(_warnings, Is.Empty, string.Join("\n", _warnings));
        }

        // In Turkish the percent sign stands before the number, the decimals take a comma, and every word is the table's.
        [Test]
        public void InTurkishTheTierLinePutsThePercentSignFirstWithDecimalCommas()
        {
            Speak(Language.Turkish);
            string forged = MapChoiceView.DescribeTier(DifficultyTier.Forged, 0);
            Assert.That(forged, Does.StartWith(TierText.Name(DifficultyTier.Forged) + ":").And.EndWith("+%0"));
            string tempered = MapChoiceView.DescribeTier(DifficultyTier.Tempered, 1470);
            Assert.That(tempered, Does.StartWith(TierText.Name(DifficultyTier.Tempered) + ":").And.Contain("+%8").And.Contain("+%2")
                .And.Contain("x1,25").And.Contain("+10").And.Contain("+%15 (+5 set)").And.EndWith("+%14,7"));
            Assert.That(tempered, Does.Not.Contain("%+").And.Not.Contain("14.7").And.Not.Contain("1.25").And.Not.Contain("foes"));
            Assert.That(tempered.Split('\n').Length, Is.EqualTo(2), "two lines");
            Assert.That(MapChoiceView.DescribeTier(DifficultyTier.Abyssal, 4190),
                Does.Contain("+%20").And.Contain("x2;").And.Contain("+%42").And.EndWith("+%41,9"));
            Assert.That(MapChoiceView.Describe(2), Does.EndWith("Kat 2. 31. seviyeden başla: önce 31 seviye seç."));
            Assert.That(MapChoiceView.Describe(1), Does.Contain("Ocak Bekçisi").And.EndWith("Kat 1. Taze bir kahraman, önünde koca bir Tırmanış."));
            Assert.That((MapChoiceView.MapTitle, MapChoiceView.SelectedText, MapChoiceView.LockedWord),
                Is.EqualTo(("Haritanı seç", "Seçildi", "Kilitli")));
            Assert.That(_warnings, Is.Empty, string.Join("\n", _warnings));
        }
    }
}
