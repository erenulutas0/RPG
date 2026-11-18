using System.Collections.Generic;
using Cryptforge.Localisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The string table against itself and the code (docs/60 section 5), read from the files: the Unity EditMode suite and
    // Tools/CombatChecks run it alike. LocalisationAssetTests (EditMode) hold the asset-derived English to the assets.
    public sealed class LocalisationTableTests
    {
        private static string _root;
        private static LocTable _table;

        private static string Root => _root ??= LocalisationChecks.Root();
        private static LocTable TheTable => _table ??= LocalisationChecks.LoadTable(Root);

        [Test]
        public void EveryRowHasItsEnglishAndItsTurkishWithTheSamePlaceholdersAndTags()
        {
            List<string> problems = LocalisationChecks.RowProblems(TheTable);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            Assert.That(TheTable.Count, Is.GreaterThan(300), "The table read whole.");
        }

        [Test]
        public void EveryKeyTheCodeNamesIsInTheTableWithTheCodesEnglishAndEveryRowIsUsed()
        {
            List<LocalisationChecks.CodeKey> uses = LocalisationChecks.ScanScripts(Root);
            Assert.That(uses.Count, Is.GreaterThan(150), "The scripts were scanned.");
            List<string> problems = LocalisationChecks.CodeProblems(TheTable, uses, LocalisationChecks.AssetKeys(Root));
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // The checks themselves, on a small table: each kind of fault is found.
        [Test]
        public void TheChecksFindEachKindOfFault()
        {
            LocTable table = LocTable.Parse(LocTable.Header + "\n" +
                "a.no_tr\tEnglish\t\n" +
                "a.placeholder\tGold {gold}\tAltın {altin}\n" +
                "a.tag\t<size=20>A</size>\t<size=20>A\n" +
                "a.other_tag\t<b>A</b>\t<i>A</i>\n" +
                "a.space\tA \tA\n" +
                "a.fine\tGold {0:0.0}\t{0:0,0} altın\n" +
                "a.unused\tA\tB\n" +
                "a.english\tCode\tKod\n");
            List<string> rows = LocalisationChecks.RowProblems(table);
            Assert.That(rows.Count, Is.EqualTo(6), string.Join("\n", rows));
            Assert.That(string.Join("\n", rows), Does.Contain("a.no_tr").And.Contain("a.placeholder").And.Contain("a.tag")
                .And.Contain("a.other_tag").And.Contain("a.space"));

            List<LocalisationChecks.CodeKey> uses = LocalisationChecks.Scan(
                "// Loc.Text(\"a.in_comment\", \"x\")\n" +
                "Loc.Text(\"a.fine\", \"Gold {0:0.0}\"); Loc.Format(\"a.english\", \"Not the code\", 1);\n" +
                "new LocText(\"a.missing\", \"Missing\"); Loc.Text(\"a.no_tr\", _field);\n" +
                "Loc.Text(\"a.placeholder\", \"Gold {gold}\"); Loc.Text(\"a.tag\", \"<size=20>A</size>\");\n" +
                "Loc.Text(\"a.other_tag\", \"<b>A</b>\"); Loc.Text(\"a.space\", \"A \\u0020\");", "Test.cs");
            Assert.That(uses.Count, Is.EqualTo(8), "The comment names no key.");
            List<string> code = LocalisationChecks.CodeProblems(table, uses, new HashSet<string>());
            Assert.That(code.Count, Is.EqualTo(4), string.Join("\n", code));
            Assert.That(string.Join("\n", code), Does.Contain("a.missing").And.Contain("a.english").And.Contain("a.unused")
                .And.Contain("a.space"));
        }

        [Test]
        public void ARoomsKeyIsMadeFromItsEnglishName()
        {
            Assert.That(ContentKeys.Room("Captain's Post"), Is.EqualTo("room.captains_post.name"));
            Assert.That(ContentKeys.Room("The Deep Forge"), Is.EqualTo("room.the_deep_forge.name"));
            Assert.That(ContentKeys.Room(null), Is.Null);
            Assert.That(ContentKeys.Key("enemy_grunt", ContentKeys.Name), Is.EqualTo("enemy_grunt.name"));
        }
    }
}
