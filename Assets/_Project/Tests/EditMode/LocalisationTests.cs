using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The localisation core (docs/60): the table's parsing, the lookup and its fallbacks, the formatter, the chosen
    // language's numbers and Turkish casing. Pure, so Tools/CombatChecks runs it too. Each test leaves Loc as it found it.
    public sealed class LocalisationTests
    {
        private Language? _override;
        private Language _chosen;
        private LocTable _table;
        private System.Action<string> _warn;

        [SetUp]
        public void Remember()
        {
            _override = Loc.Override;
            _chosen = Loc.Chosen;
            _table = Loc.Table;
            _warn = Loc.Warn;
        }

        [TearDown]
        public void Restore()
        {
            Loc.UseTable(_table);
            Loc.SetLanguage(_chosen);
            Loc.Override = _override;
            Loc.Warn = _warn;
        }

        private static LocTable Table(params string[] rows) => LocTable.Parse(LocTable.Header + "\n" + string.Join("\n", rows));

        [Test]
        public void TheTableReadsItsRowsAndSkipsCommentsBlankLinesAndASpreadsheetsQuotes()
        {
            LocTable table = LocTable.Parse("﻿# a comment\r\nkey\ten\ttr\tnote\r\n\r\n# ---- a section\r\n" +
                "anvil.title\tThe Anvil\tÖrs\tthe title\r\nhud.two\tA\\nB\t\"C \"\"D\"\"\"\r\n");

            Assert.That(table.Problems, Is.Empty);
            Assert.That(table.Count, Is.EqualTo(2));
            Assert.That(table.TryGet("anvil.title", out LocEntry title), Is.True);
            Assert.That((title.English, title.Turkish, title.Note, title.Line), Is.EqualTo(("The Anvil", "Örs", "the title", 5)));
            Assert.That(table.TryGet("hud.two", out LocEntry two), Is.True);
            Assert.That((two.English, two.Turkish, two.Note), Is.EqualTo(("A\nB", "C \"D\"", string.Empty)));
        }

        [Test]
        public void ABrokenRowIsNamedAndSkippedNeverThrown()
        {
            LocTable table = Table("Bad Key\tA\tB", "short.row\tA", "dup.key\tA\tB", "dup.key\tC\tD", "tab.inside\tA\tB\tnote\tstray");

            Assert.That(table.Count, Is.EqualTo(1));
            Assert.That(table.Contains("dup.key"), Is.True);
            Assert.That(table.Problems.Count, Is.EqualTo(4));
            Assert.That(LocTable.Parse("anvil.title\tThe Anvil\tÖrs").Problems[0], Does.Contain("header"));
            Assert.That(LocTable.Parse(string.Empty).Problems, Is.Not.Empty);
        }

        [Test]
        public void EscapingRoundTrips()
        {
            const string text = "a\\b\nc\td";
            Assert.That(LocTable.Unescape(LocTable.Escape(text)), Is.EqualTo(text));
            Assert.That(LocTable.IsKey("anvil.tab.bag"), Is.True);
            Assert.That(LocTable.IsKey("anvil"), Is.False);
            Assert.That(LocTable.IsKey("Anvil.Title"), Is.False);
            Assert.That(LocTable.IsKey("anvil..title"), Is.False);
        }

        [Test]
        public void EnglishIsTheSourcesTextWhateverTheTableSays()
        {
            Loc.UseTable(Table("anvil.title\tSomething else\tÖrs"));
            Loc.Override = Language.English;

            Assert.That(Loc.Text("anvil.title", "The Anvil"), Is.EqualTo("The Anvil"));
            Assert.That(Loc.Text("anvil.title", null), Is.Null, "Nothing authored stays nothing.");
            Assert.That(Loc.Text("anvil.title", string.Empty), Is.Empty);
        }

        [Test]
        public void TurkishFallsBackToTheEnglishThenTheKeyAndSaysSoOnce()
        {
            var warnings = new List<string>();
            Loc.Warn = warnings.Add;
            Loc.UseTable(Table("anvil.title\tThe Anvil\tÖrs", "anvil.empty\tEmpty\t", "table.only\tOnly English\t"));
            Loc.Override = Language.Turkish;

            Assert.That(Loc.Text("anvil.title", "The Anvil"), Is.EqualTo("Örs"));
            Assert.That(Loc.Text("anvil.empty", "Empty"), Is.EqualTo("Empty"), "A missing Turkish shows the English.");
            Assert.That(Loc.Text("anvil.empty", "Empty"), Is.EqualTo("Empty"));
            Assert.That(Loc.Text("not.there", "Fallback"), Is.EqualTo("Fallback"));
            Assert.That(Loc.Text("table.only"), Is.EqualTo("Only English"), "Without English in the code, the table's.");
            Assert.That(Loc.Text("not.there.either"), Is.EqualTo("not.there.either"), "Then the key itself.");
            Assert.That(warnings.Count, Is.EqualTo(4), string.Join("\n", warnings));
            Assert.That(warnings[0], Does.Contain("anvil.empty"));
        }

        [Test]
        public void ChangedFiresWhenTheLanguageTheGameSpeaksChanges()
        {
            Loc.Override = null;
            Loc.SetLanguage(Language.English);
            int changes = 0;
            System.Action count = () => changes++;
            Loc.Changed += count;
            try
            {
                Loc.SetLanguage(Language.English);
                Assert.That(changes, Is.EqualTo(0), "The same language is no change.");
                Loc.SetLanguage(Language.Turkish);
                Assert.That((changes, Loc.Language), Is.EqualTo((1, Language.Turkish)));
                Loc.Override = Language.Turkish;
                Assert.That(changes, Is.EqualTo(1), "An override to the language already spoken changes nothing.");
                Loc.Override = Language.English;
                Assert.That((changes, Loc.Language, Loc.Chosen), Is.EqualTo((2, Language.English, Language.Turkish)));
                Loc.Override = null;
                Assert.That((changes, Loc.Language), Is.EqualTo((3, Language.Turkish)));
            }
            finally
            {
                Loc.Changed -= count;
            }
        }

        [Test]
        public void TheFormatterFillsIndexedAndNamedPlaceholdersInTheLanguagesNumbers()
        {
            Loc.UseTable(Table("anvil.need\tNeed {gold} more gold\t{gold} altın daha gerekli", "hud.secs\t{0} in {1:0.0}s\t{0}, {1:0.0} sn"));
            Loc.Override = Language.English;
            Assert.That(Loc.Format("anvil.need", "Need {gold} more gold", ("gold", 1300)), Is.EqualTo("Need 1300 more gold"));
            Assert.That(Loc.Format("hud.secs", "{0} in {1:0.0}s", "Grunt", 4.25f), Is.EqualTo("Grunt in 4.3s"));

            Loc.Override = Language.Turkish;
            Assert.That(Loc.Format("anvil.need", "Need {gold} more gold", ("gold", 1300.5f.ToString("#,0.0", Loc.Numbers))),
                Is.EqualTo("1.300,5 altın daha gerekli"));
            Assert.That(Loc.Format("hud.secs", "{0} in {1:0.0}s", "Piyade", 4.25f), Is.EqualTo("Piyade, 4,3 sn"));
        }

        [Test]
        public void ATranslationThatCannotBeFilledFallsBackToTheEnglish()
        {
            var warnings = new List<string>();
            Loc.Warn = warnings.Add;
            Loc.UseTable(Table("anvil.bad\tGold {gold}\tAltın {altin}", "anvil.brace\tGold {gold}\tAltın {gold"));
            Loc.Override = Language.Turkish;

            Assert.That(Loc.Format("anvil.bad", "Gold {gold}", ("gold", 5)), Is.EqualTo("Gold 5"));
            Assert.That(Loc.Format("anvil.brace", "Gold {gold}", ("gold", 5)), Is.EqualTo("Gold 5"));
            Assert.That(warnings.Count, Is.EqualTo(2));
        }

        [Test]
        public void PlaceholdersAreReadWithTheirFormatsAndBracesEscape()
        {
            List<LocFormat.Placeholder> placeholders = LocFormat.Placeholders("{0}:{1:00} and {gold:#,0} {{not}} }}");
            Assert.That(placeholders.Count, Is.EqualTo(3));
            Assert.That(placeholders[1].ToString(), Is.EqualTo("{1:00}"));
            Assert.That(placeholders[2].Name, Is.EqualTo("gold"));
            Assert.That(placeholders[2].IsIndexed, Is.False);
            Assert.That(LocFormat.Placeholders("a { b"), Is.Null);
            Assert.That(LocFormat.Placeholders("a } b"), Is.Null);
            Assert.That(LocFormat.Placeholders("{Gold}"), Is.Null, "A name is lower-case snake_case.");
            Assert.That(LocFormat.TryApply("{{{0}}}", CultureInfo.InvariantCulture, new object[] { 7 }, null, out string text), Is.True);
            Assert.That(text, Is.EqualTo("{7}"));
            Assert.That(LocFormat.TryApply("{1}", CultureInfo.InvariantCulture, new object[] { 7 }, null, out _), Is.False);
        }

        [Test]
        public void TurkishNumbersUseACommaAndADotWhateverTheThreadsCulture()
        {
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                foreach (string culture in new[] { "en-US", "tr-TR", "de-DE" })
                {
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                    Assert.That(1300.5.ToString("#,0.0", Loc.NumbersFor(Language.Turkish)), Is.EqualTo("1.300,5"), culture);
                    Assert.That(1300.5.ToString("#,0.0", Loc.NumbersFor(Language.English)), Is.EqualTo("1,300.5"), culture);
                    Assert.That(0.25.ToString("%0", Loc.NumbersFor(Language.Turkish)), Is.EqualTo("%25"), culture);
                    Assert.That(0.25.ToString("0%", Loc.NumbersFor(Language.English)), Is.EqualTo("25%"), culture);
                }
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        [Test]
        public void TurkishUpperCaseDotsTheCapitalIAndKeepsTags()
        {
            Assert.That(Loc.UpperFor(Language.Turkish, "sonraki salon, ılık"), Is.EqualTo("SONRAKİ SALON, ILIK"));
            Assert.That(Loc.UpperFor(Language.English, "next hall"), Is.EqualTo("NEXT HALL"));
            Assert.That(Loc.UpperFor(Language.Turkish, "<size=34>istila</size> <color=#EEBF6C>i</color>"),
                Is.EqualTo("<size=34>İSTİLA</size> <color=#EEBF6C>İ</color>"));
            Assert.That(Loc.LowerFor(Language.Turkish, "ISTILA İZİ"), Is.EqualTo("ıstıla izi"));
            Assert.That(Loc.LowerFor(Language.English, "EXIT"), Is.EqualTo("exit"));
        }

        [Test]
        public void RichTextTagsAreReadInOrderAndMustClose()
        {
            Assert.That(RichText.TryTags("a <size=20>b</size> <color=#fff><b>c</b></color> x < y", out List<string> tags, out _), Is.True);
            Assert.That(tags, Is.EqualTo(new[] { "<size=20>", "</size>", "<color=#fff>", "<b>", "</b>", "</color>" }));
            Assert.That(RichText.TryTags("<size=20>b", out _, out string open), Is.False);
            Assert.That(open, Does.Contain("never closed"));
            Assert.That(RichText.TryTags("<b><i>x</b></i>", out _, out string crossed), Is.False);
            Assert.That(crossed, Does.Contain("closes"));
        }

        [Test]
        public void TheGearsWordsAndNumbersFollowTheLanguage()
        {
            Loc.UseTable(Table("gear.stat.damage\tDamage\tHasar", "gear.value.percent\t+{value}%\t+%{value}",
                "gear.value.points\t+{value} pts\t+{value} puan", "gear.percent\t{value}%\t%{value}"));
            Loc.Override = Language.English;
            Assert.That(GearText.StatName(GearStat.Damage) + " " + GearText.Value(GearStat.Damage, 250), Is.EqualTo("Damage +2.5%"));
            Assert.That(GearText.Gold(12345), Is.EqualTo("12,345"));
            Assert.That(GearText.Percent(20), Is.EqualTo("20%"));

            Loc.Override = Language.Turkish;
            Assert.That(GearText.StatName(GearStat.Damage) + " " + GearText.Value(GearStat.Damage, 250), Is.EqualTo("Hasar +%2,5"));
            Assert.That(GearText.Value(GearStat.CritChance, 80), Is.EqualTo("+0,8 puan"));
            Assert.That(GearText.Gold(12345), Is.EqualTo("12.345"));
            Assert.That(GearText.Percent(20), Is.EqualTo("%20"));
        }
    }
}
