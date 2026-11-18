using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cryptforge.Content;
using Cryptforge.Localisation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The string table against the content assets (docs/60 section 5), Editor only: the table loads from Resources as the
    // game loads it; every getter of the game's own words (PrototypeTextDefinition) and of the content's (enemies, floors,
    // rooms, items, cards, relics, weapons, heroes) reads its own key; and the table's English for each is the asset's.
    public sealed class LocalisationAssetTests
    {
        private const string Data = "Assets/_Project/Data";
        private const string Development = "Assets/_Project/Data/Development";
        private const string Marker = "«{0}»";

        private Language? _override;
        private LocTable _loaded;
        private Action<string> _warn;

        [SetUp]
        public void Remember()
        {
            _override = Loc.Override;
            _loaded = Loc.Table;
            _warn = Loc.Warn;
        }

        [TearDown]
        public void Restore()
        {
            Loc.UseTable(_loaded);
            Loc.Override = _override;
            Loc.Warn = _warn;
        }

        private static LocTable RealTable() => LocalisationChecks.LoadTable(LocalisationChecks.Root());

        [Test]
        public void TheTableLoadsFromResourcesAsTheGameLoadsIt()
        {
            var asset = Resources.Load<TextAsset>(LocalisationRuntime.TableResource);
            Assert.That(asset, Is.Not.Null, "Resources/" + LocalisationRuntime.TableResource + ".txt imports as a TextAsset.");
            LocTable table = LocTable.Parse(asset.text);
            Assert.That(table.Problems, Is.Empty, string.Join("\n", table.Problems));
            Assert.That(table.Count, Is.EqualTo(RealTable().Count));
            Assert.That(table.TryGet("prototype.pause_title", out LocEntry pause), Is.True);
            Assert.That(pause.Turkish, Is.EqualTo("Duraklatıldı"), "UTF-8 read whole: the dotless i survives the import.");
        }

        [Test]
        public void EveryGetterOfTheGamesOwnWordsReadsItsOwnKey()
        {
            var text = ScriptableObject.CreateInstance<PrototypeTextDefinition>();
            try
            {
                FieldInfo[] fields = typeof(PrototypeTextDefinition).GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(f => f.FieldType == typeof(string) && f.GetCustomAttribute<SerializeField>() != null).ToArray();
                PropertyInfo[] properties = typeof(PrototypeTextDefinition).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                    .Where(p => p.PropertyType == typeof(string)).ToArray();
                Assert.That(properties.Length, Is.EqualTo(fields.Length), "One getter for every field.");
                foreach (FieldInfo field in fields)
                    field.SetValue(text, "English " + field.Name);
                Loc.UseTable(MarkerTable(properties.Select(p => PrototypeKey(p.Name))));
                Loc.Override = Language.Turkish;
                foreach (PropertyInfo property in properties)
                {
                    string key = PrototypeKey(property.Name);
                    Assert.That(property.GetValue(text), Is.EqualTo(string.Format(Marker, key)), property.Name + " reads " + key);
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(text);
            }
        }

        [Test]
        public void TheGamesOwnEnglishIsTheTables()
        {
            var text = AssetDatabase.LoadAssetAtPath<PrototypeTextDefinition>(Data + "/UI/PrototypeText.asset");
            Assert.That(text, Is.Not.Null);
            LocTable table = RealTable();
            Loc.Override = Language.English;
            var problems = new List<string>();
            foreach (PropertyInfo property in typeof(PrototypeTextDefinition).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                         .Where(p => p.PropertyType == typeof(string)))
                Expect(table, PrototypeKey(property.Name), (string)property.GetValue(text), problems);
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // Every player-facing content asset's words have their row, with the asset's English. Development copies and the
        // enemies' own weapons (no forge card) are on no screen and have none.
        [Test]
        public void EveryContentAssetsWordsAreInTheTableWithTheirEnglish()
        {
            LocTable table = RealTable();
            var problems = new List<string>();
            int checkedAssets = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { Data }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.StartsWith(Development, StringComparison.Ordinal))
                    continue;
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null || asset is PrototypeTextDefinition)
                    continue;
                var serialized = new SerializedObject(asset);
                string id = serialized.FindProperty("_id")?.stringValue;
                if (string.IsNullOrEmpty(id))
                    continue;
                if (asset is WeaponDefinition && string.IsNullOrEmpty(serialized.FindProperty("_forgeDescriptionFormat")?.stringValue))
                    continue;
                checkedAssets++;
                Field(table, serialized, id, "_displayName", ContentKeys.Name, problems);
                Field(table, serialized, id, "_description", ContentKeys.Description, problems);
                Field(table, serialized, id, "_descriptionFormat", ContentKeys.Description, problems);
                Field(table, serialized, id, "_forgeDescriptionFormat", ContentKeys.Description, problems);
                if (asset is HeroDefinition)
                {
                    Field(table, serialized, id, "_subtitle", "subtitle", problems);
                    Field(table, serialized, id, "_role", "role", problems);
                    Field(table, serialized, id, "_unlockHint", "unlock_hint", problems);
                }
                if (asset is FloorDefinition floor)
                    for (int i = 0; i < floor.RoomCount; i++)
                    {
                        string name = serialized.FindProperty("_rooms").GetArrayElementAtIndex(i).FindPropertyRelative("_displayName").stringValue;
                        Expect(table, ContentKeys.Room(name), name, problems);
                    }
            }
            Assert.That(checkedAssets, Is.GreaterThan(50), "The content was found.");
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
        }

        // Each kind of content reads its words through its keys: with a table whose Turkish is each key in marks, every
        // getter says its key.
        [Test]
        public void EveryContentGetterReadsItsOwnKey()
        {
            var enemy = Load<EnemyDefinition>("Enemies/Enemy_ForgeWarden.asset");
            var floor = Load<FloorDefinition>("Floors/Floor_EmberHalls.asset");
            var modifier = Load<FloorModifierDefinition>("Floors/Modifier_CursedGold.asset");
            var forge = Load<ForgeOptionDefinition>("Forge/Forge_Mend.asset");
            var item = Load<ItemDefinition>("Items/Item_CinderRing.asset");
            var relic = Load<RelicDefinition>("Relics/Relic_SecondWind.asset");
            var upgrade = Load<UpgradeDefinition>("Upgrades/Upgrade_Range.asset");
            var weapon = Load<WeaponDefinition>("Weapons/Weapon_Daggers.asset");
            var hero = Load<HeroDefinition>("Heroes/Hero_CinderArcanist.asset");
            Loc.Override = Language.English;
            string room = floor.RoomAt(0).DisplayName;
            var keys = new List<string>
            {
                enemy.Id + ".name", floor.Id + ".name", ContentKeys.Room(room), modifier.Id + ".name", modifier.Id + ".description",
                forge.Id + ".name", forge.Id + ".description", item.Id + ".name", relic.Id + ".name", relic.Id + ".description",
                upgrade.Id + ".name", upgrade.Id + ".description", weapon.Id + ".name", weapon.Id + ".description",
                hero.Id + ".name", hero.Id + ".subtitle", hero.Id + ".role", hero.Id + ".unlock_hint"
            };
            Loc.UseTable(MarkerTable(keys));
            Loc.Override = Language.Turkish;
            string M(string key) => string.Format(Marker, key);

            Assert.That(enemy.DisplayName, Is.EqualTo(M(enemy.Id + ".name")));
            Assert.That(floor.DisplayName, Is.EqualTo(M(floor.Id + ".name")));
            Assert.That(floor.RoomAt(0).DisplayName, Is.EqualTo(M(ContentKeys.Room(room))));
            Assert.That((modifier.DisplayName, modifier.Description), Is.EqualTo((M(modifier.Id + ".name"), M(modifier.Id + ".description"))));
            Assert.That((forge.DisplayName, forge.DescriptionFormat), Is.EqualTo((M(forge.Id + ".name"), M(forge.Id + ".description"))));
            Assert.That(forge.CreateOption().DisplayName, Is.EqualTo(M(forge.Id + ".name")), "The option is made in the language.");
            Assert.That(item.DisplayName, Is.EqualTo(M(item.Id + ".name")));
            Assert.That(item.CreateTemplate().DisplayName, Is.EqualTo(M(item.Id + ".name")));
            Assert.That((relic.DisplayName, relic.DescriptionFormat), Is.EqualTo((M(relic.Id + ".name"), M(relic.Id + ".description"))));
            Assert.That(relic.CreateOption().DisplayName, Is.EqualTo(M(relic.Id + ".name")));
            Assert.That((upgrade.DisplayName, upgrade.DescriptionFormat), Is.EqualTo((M(upgrade.Id + ".name"), M(upgrade.Id + ".description"))));
            Assert.That(upgrade.CreateOption().DisplayName, Is.EqualTo(M(upgrade.Id + ".name")));
            Assert.That((weapon.DisplayName, weapon.ForgeDescriptionFormat), Is.EqualTo((M(weapon.Id + ".name"), M(weapon.Id + ".description"))));
            Assert.That(weapon.CreateForgeOption().Description, Is.EqualTo(M(weapon.Id + ".description")));
            Assert.That((HeroText.Name(hero), HeroText.Subtitle(hero), HeroText.Role(hero), HeroText.UnlockHint(hero)),
                Is.EqualTo((M(hero.Id + ".name"), M(hero.Id + ".subtitle"), M(hero.Id + ".role"), M(hero.Id + ".unlock_hint"))));
        }

        // In English nothing changes: the getters give the assets' own words, as before the table existed.
        [Test]
        public void EnglishIsTheAssetsOwnWords()
        {
            Loc.UseTable(RealTable());
            Loc.Override = Language.English;
            var enemy = Load<EnemyDefinition>("Enemies/Enemy_ForgeWarden.asset");
            var weapon = Load<WeaponDefinition>("Weapons/Weapon_Daggers.asset");
            Assert.That(enemy.DisplayName, Is.EqualTo(new SerializedObject(enemy).FindProperty("_displayName").stringValue));
            Assert.That(weapon.CreateForgeOption().Description, Does.Contain("every 0.5s"));

            Loc.Override = Language.Turkish;
            Assert.That(enemy.DisplayName, Is.EqualTo("Ocak Bekçisi"));
            Assert.That(weapon.CreateForgeOption().Description, Does.Contain("0,5 sn"), "Turkish numbers in Turkish words.");
        }

        private static T Load<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(Data + "/" + path);
            Assert.That(asset, Is.Not.Null, path);
            return asset;
        }

        private static void Field(LocTable table, SerializedObject asset, string id, string field, string suffix, List<string> problems)
        {
            SerializedProperty property = asset.FindProperty(field);
            if (property == null || property.propertyType != SerializedPropertyType.String || string.IsNullOrEmpty(property.stringValue))
                return;
            Expect(table, ContentKeys.Key(id, suffix), property.stringValue, problems);
        }

        private static void Expect(LocTable table, string key, string english, List<string> problems)
        {
            if (string.IsNullOrEmpty(english))
                return;
            if (!table.TryGet(key, out LocEntry entry))
                problems.Add($"{key} is missing from the table (English \"{LocTable.Escape(english)}\").");
            else if (entry.English != english)
                problems.Add($"{key} (line {entry.Line}): the table's English \"{LocTable.Escape(entry.English)}\" is not the asset's \"{LocTable.Escape(english)}\".");
        }

        // "PauseTitle" is prototype.pause_title.
        private static string PrototypeKey(string property)
        {
            var key = new StringBuilder("prototype.");
            for (int i = 0; i < property.Length; i++)
            {
                char c = property[i];
                if (c >= 'A' && c <= 'Z')
                {
                    if (i > 0)
                        key.Append('_');
                    key.Append((char)(c - 'A' + 'a'));
                }
                else
                {
                    key.Append(c);
                }
            }
            return key.ToString();
        }

        private static LocTable MarkerTable(IEnumerable<string> keys)
        {
            var text = new StringBuilder(LocTable.Header).Append('\n');
            foreach (string key in keys.Distinct())
                text.Append(key).Append("\tEnglish\t").Append(string.Format(Marker, key)).Append('\n');
            return LocTable.Parse(text.ToString());
        }
    }
}
