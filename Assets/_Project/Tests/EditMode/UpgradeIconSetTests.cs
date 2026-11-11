using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Cryptforge.Art;
using Cryptforge.Content;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The upgrade cards' painted icons (Tools/ArtReview/build_upgrade_icons.py): every id the Gameplay scene's offers can
    // show - each upgrade card of its pool (the hall offers them all, the foundry the hero's), each weapon of its catalogue
    // with its lines and its evolution, each tome of its economy - has an icon in the set its RunChoiceView holds (the
    // hall's two legacy cards too, since 2026-11-12, so every card's tier ground shows round its icon; the scene's own
    // table keeps their old painted icons only as a fallback); the weapons, their evolutions and the tomes have socket icons for the loadout row too. The
    // sheet's import and its cost on the phone, and what the icons paint, read back from the sheet as painted (the
    // imported texture is compressed and unreadable): clear at their edges and the painted icons' navy laid over it,
    // their glyphs filling the square, and each evolution throwing more light than what it evolves from.
    public sealed class UpgradeIconSetTests
    {
        private const string SetPath = "Assets/_Project/Art/UI/UpgradeIcons/UpgradeIconSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string GoldIconPath = "Assets/_Project/Art/UI/UI_Icon_Gold_v1.png";
        // Every icon of the set together, as ASTC 4x4 (a byte a texel): about 0.8 MB on the phone at most.
        private const long BudgetBytes = 838860;
        private const int CardSize = 144;
        private const int SlotSize = 56;

        private UpgradeIconSet _set;
        private string _scene;
        private Texture2D _painted;
        private Color32 _navy;

        // What the scene's offers can show, from its CombatSetup and its RunChoiceView's table.
        private readonly List<string> _generalIds = new List<string>();
        private readonly List<string> _weaponIds = new List<string>();
        private readonly List<string> _lineIds = new List<string>();
        private readonly List<string> _evolutionIds = new List<string>();
        private readonly List<string> _tomeIds = new List<string>();
        private readonly List<string> _tableIds = new List<string>();
        // An evolution's id to the id it evolves from (a weapon's, or a general card's), for those both in the set.
        private readonly Dictionary<string, string> _evolvesFrom = new Dictionary<string, string>();

        [OneTimeSetUp]
        public void Load()
        {
            _set = AssetDatabase.LoadAssetAtPath<UpgradeIconSet>(SetPath);
            Assert.That(_set, Is.Not.Null, SetPath);
            _scene = File.ReadAllText(ScenePath);

            string setup = Block(_scene, "Assets/_Project/Scripts/Core/CombatSetup.cs");
            foreach (string guid in Guids(setup, "_upgrades", "{fileID"))
            {
                var card = Load<UpgradeDefinition>(guid);
                _generalIds.Add(card.Id);
                if (card.EvolvesFrom != null)
                    _evolvesFrom[card.Id] = card.EvolvesFrom.Id;
            }
            foreach (string guid in Guids(setup, "_weapons", "{fileID"))
            {
                var weapon = Load<WeaponDefinition>(guid);
                _weaponIds.Add(weapon.Id);
                for (int i = 0; i < weapon.LevelUpCount; i++)
                    _lineIds.Add(weapon.LevelUpAt(i).Id);
                if (weapon.Evolution != null)
                {
                    _evolutionIds.Add(weapon.Evolution.Id);
                    _evolvesFrom[weapon.Evolution.Id] = weapon.Id;
                }
            }
            Match economy = Regex.Match(setup, @"\n  _economy: \{fileID: 11400000, guid: ([0-9a-f]{32})");
            Assert.That(economy.Success, Is.True, "The CombatSetup holds its economy.");
            var tomes = Load<EconomyConfig>(economy.Groups[1].Value);
            for (int i = 0; i < tomes.TomeCount; i++)
                _tomeIds.Add(tomes.TomeAt(i).Id);
            string choices = Block(_scene, "Assets/_Project/Scripts/UI/RunChoiceView.cs");
            foreach (string guid in Guids(choices, "_upgradeIcons", "Definition:"))
                _tableIds.Add(Load<UpgradeDefinition>(guid).Id);

            Sprite any = _set.Count > 0 ? _set[0].Card : null;
            Assert.That(any, Is.Not.Null, "The set holds icons.");
            _painted = Painted(AssetDatabase.GetAssetPath(any.texture));
            Texture2D gold = Painted(GoldIconPath);
            _navy = gold.GetPixel(4, 4);
            Object.DestroyImmediate(gold);
        }

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
        }

        private IEnumerable<string> OfferableIds()
        {
            foreach (List<string> ids in new[] { _generalIds, _weaponIds, _lineIds, _evolutionIds, _tomeIds })
                foreach (string id in ids)
                    yield return id;
        }

        [Test]
        public void EveryIdAnOfferCanShowHasAnIcon()
        {
            Assert.That(_weaponIds, Is.EquivalentTo(new[] { "weapon_sword", "weapon_staff", "weapon_daggers" }),
                "The three hero weapons a foundry run can carry.");
            Assert.That(_lineIds.Count, Is.EqualTo(9), "Three lines on each of them.");
            Assert.That(_evolutionIds.Count, Is.EqualTo(3), "And an evolution each.");
            Assert.That(_tomeIds.Count, Is.EqualTo(5), "Five tomes.");
            Assert.That(_generalIds.Count, Is.EqualTo(14), "Fourteen general cards in the pool.");
            Assert.That(_tableIds, Is.EquivalentTo(new[] { "upgrade_damage", "upgrade_attack_speed" }),
                "The scene's own table keeps the two painted legacy icons, as a fallback the set now covers.");
            var offerable = new HashSet<string>(OfferableIds());
            Assert.That(offerable.Count, Is.EqualTo(34), "No id names two cards.");
            foreach (string id in offerable)
                Assert.That(_set.CardIconFor(id), Is.Not.Null, $"{id} has its icon in the set, the table's two included.");
            var inSet = new HashSet<string>();
            for (int i = 0; i < _set.Count; i++)
            {
                UpgradeIconSet.Icon icon = _set[i];
                Assert.That(inSet.Add(icon.Id), Is.True, $"{icon.Id} appears once.");
                Assert.That(offerable.Contains(icon.Id), Is.True, $"{icon.Id} is something an offer can show.");
                Assert.That(icon.Card, Is.Not.Null, $"{icon.Id} has its card icon.");
            }
            Assert.That(inSet.Count, Is.EqualTo(offerable.Count), "Every id an offer can show.");
        }

        [Test]
        public void TheWeaponsTheirEvolutionsAndTheTomesHaveSocketIconsAndNothingElseDoes()
        {
            var socketed = new HashSet<string>(_weaponIds);
            socketed.UnionWith(_evolutionIds);
            socketed.UnionWith(_tomeIds);
            for (int i = 0; i < _set.Count; i++)
            {
                UpgradeIconSet.Icon icon = _set[i];
                if (socketed.Contains(icon.Id))
                    Assert.That(icon.Slot, Is.Not.Null, $"{icon.Id}: the loadout row shows it.");
                else
                    Assert.That(icon.Slot, Is.Null, $"{icon.Id}: a line or a general card never sits in a socket.");
            }
            Assert.That(_set.SlotIconFor("weapon_sword"), Is.Not.SameAs(_set.SlotIconFor("evolution_sword")),
                "An evolved weapon's socket shows its evolution.");
            Assert.That(_set.CardIconFor("weapon_sword"), Is.Not.SameAs(_set.SlotIconFor("weapon_sword")),
                "A socket's icon is painted for its size, not the card's.");
        }

        [Test]
        public void TheLookupAnswersOnlyTheIdsItHolds()
        {
            Assert.That(_set.CardIconFor("no_such_card"), Is.Null);
            Assert.That(_set.CardIconFor(null), Is.Null);
            Assert.That(_set.CardIconFor(string.Empty), Is.Null);
            Assert.That(_set.SlotIconFor("line_sword_damage"), Is.Null, "A line has no socket icon.");
            foreach (string legacy in new[] { "upgrade_damage", "upgrade_attack_speed" })
                Assert.That(_set.CardIconFor(legacy).name, Is.EqualTo("UI_UpgradeIcons_" + legacy),
                    $"{legacy}: the set paints the hall's legacy card too, without its old navy square.");
            var empty = ScriptableObject.CreateInstance<UpgradeIconSet>();
            try
            {
                Assert.That(empty.Count, Is.Zero);
                Assert.That(empty.CardIconFor("weapon_sword"), Is.Null, "A set with nothing holds nothing.");
                Sprite card = _set.CardIconFor("weapon_sword");
                typeof(UpgradeIconSet).GetField("_icons", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(empty, new[] { new UpgradeIconSet.Icon("weapon_sword", card, null) });
                Assert.That(empty.CardIconFor("weapon_sword"), Is.SameAs(card));
                Assert.That(empty.SlotIconFor("weapon_sword"), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(empty);
            }
        }

        [Test]
        public void TheSceneWiresTheSetIntoTheCardsAndTheLoadoutRow()
        {
            string guid = AssetDatabase.AssetPathToGUID(SetPath);
            string reference = "{fileID: 11400000, guid: " + guid + ", type: 2}";
            Assert.That(Block(_scene, "Assets/_Project/Scripts/UI/RunChoiceView.cs"), Does.Contain("\n  _iconSet: " + reference),
                "The choice panel looks in the set before its table.");
            Assert.That(Block(_scene, "Assets/_Project/Scripts/UI/RunHud.cs"), Does.Contain("\n  _upgradeIcons: " + reference),
                "The HUD hands the set to the loadout row it builds.");
        }

        [Test]
        public void TheIconsAreOnOneSheetSquareAndAtTheirPaintedSize()
        {
            Texture2D sheet = _set[0].Card.texture;
            for (int i = 0; i < _set.Count; i++)
            {
                UpgradeIconSet.Icon icon = _set[i];
                foreach ((Sprite sprite, int size) in new[] { (icon.Card, CardSize), (icon.Slot, SlotSize) })
                {
                    if (sprite == null)
                        continue;
                    Assert.That(sprite.texture, Is.SameAs(sheet), $"{sprite.name}: one sheet for every icon.");
                    Assert.That(sprite.rect.size, Is.EqualTo(new Vector2(size, size)), $"{sprite.name}: square, as painted.");
                    Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), $"{sprite.name}: the canvas's own density.");
                    Assert.That(sprite.pivot, Is.EqualTo(new Vector2(size / 2f, size / 2f)), sprite.name);
                    Assert.That(sprite.border, Is.EqualTo(Vector4.zero), $"{sprite.name}: drawn whole, never sliced.");
                    Assert.That(sprite.name, Does.StartWith("UI_UpgradeIcons_" + icon.Id), sprite.name);
                    Assert.That(sprite.name, Does.Not.StartWith("UI_Icon_"), "Never mistaken for an ability's icon.");
                }
            }
        }

        [Test]
        public void TheSheetImportsSmoothUnmippedAndAsAstcWithinBudget()
        {
            Texture2D sheet = _set[0].Card.texture;
            string path = AssetDatabase.GetAssetPath(sheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), $"{path}: cut into its icons.");
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), $"{path}: smooth, as the painted UI.");
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.isReadable, Is.False, path);
            Assert.That(importer.sRGBTexture, Is.True, path);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), $"{path}: cut as painted.");
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True, path);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
            Assert.That((sheet.width, sheet.height), Is.EqualTo((_painted.width, _painted.height)), $"{path}: at its painted size.");
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(sheet.width, sheet.height)), $"{path}: whole on the phone.");
            Assert.That(sheet.width % 4 == 0 && sheet.height % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
            Assert.That((long)sheet.width * sheet.height, Is.LessThanOrEqualTo(BudgetBytes), "About 0.8 MB on the phone at most.");
            // The sprites never touch: two clear texels between neighbours, so bilinear filtering never reaches across.
            var rects = new List<Rect>();
            for (int i = 0; i < _set.Count; i++)
                foreach (Sprite sprite in new[] { _set[i].Card, _set[i].Slot })
                    if (sprite != null)
                        rects.Add(sprite.rect);
            for (int a = 0; a < rects.Count; a++)
                for (int b = a + 1; b < rects.Count; b++)
                {
                    Rect grown = new Rect(rects[a].x - 2, rects[a].y - 2, rects[a].width + 4, rects[a].height + 4);
                    Assert.That(grown.Overlaps(rects[b]), Is.False, $"{rects[a]} and {rects[b]} keep two texels apart.");
                }
        }

        [Test]
        public void EveryCardIconIsClearAtItsEdgeAndOnTheNavyItsFamilysOwn()
        {
            for (int i = 0; i < _set.Count; i++)
            {
                Sprite sprite = _set[i].Card;
                RectInt r = Texels(sprite);
                int clear = 0, ring = 0;
                for (int k = 0; k < r.width; k++)
                    foreach ((int x, int y) in new[] { (r.x + k, r.y), (r.x + k, r.yMax - 1), (r.x, r.y + k), (r.xMax - 1, r.y + k) })
                    {
                        Color32 c = _painted.GetPixel(x, y);
                        ring++;
                        if (c.a <= 4)
                            clear++;
                        Assert.That(Max(Over(c), _navy), Is.LessThanOrEqualTo(4),
                            $"{sprite.name}: laid on the navy, its edge is the navy - no square on a navy card.");
                    }
                Assert.That(clear, Is.EqualTo(ring), $"{sprite.name}: its edge is clear, so it shows no square on any face.");
            }
        }

        [Test]
        public void EveryCardIconsGlyphFillsItsSquareAndSitsInItsMiddle()
        {
            for (int i = 0; i < _set.Count; i++)
            {
                Sprite sprite = _set[i].Card;
                RectInt r = Texels(sprite);
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
                for (int y = r.y; y < r.yMax; y++)
                    for (int x = r.x; x < r.xMax; x++)
                        if (((Color32)_painted.GetPixel(x, y)).a > 230)
                        {
                            x0 = Mathf.Min(x0, x);
                            y0 = Mathf.Min(y0, y);
                            x1 = Mathf.Max(x1, x);
                            y1 = Mathf.Max(y1, y);
                        }
                Assert.That(x1, Is.GreaterThan(x0), $"{sprite.name}: a glyph is painted.");
                float span = Mathf.Max(x1 - x0 + 1, y1 - y0 + 1) / (float)r.width;
                Assert.That(span, Is.InRange(.76f, .96f), $"{sprite.name}: its glyph fills its square, reading at a glance.");
                Assert.That((x0 + x1 + 1) / 2f - r.center.x, Is.EqualTo(0f).Within(2f), $"{sprite.name}: centred across.");
                Assert.That((y0 + y1 + 1) / 2f - r.center.y, Is.EqualTo(0f).Within(2f), $"{sprite.name}: centred up and down.");
            }
        }

        [Test]
        public void EveryEvolutionThrowsMoreLightThanWhatItEvolvesFrom()
        {
            int pairs = 0;
            foreach (KeyValuePair<string, string> pair in _evolvesFrom)
            {
                Sprite evolved = _set.CardIconFor(pair.Key), from = _set.CardIconFor(pair.Value);
                Assert.That(evolved, Is.Not.Null, pair.Key);
                Assert.That(from, Is.Not.Null, pair.Value);
                float more = Light(evolved), less = Light(from);
                Assert.That(more, Is.GreaterThan(less * 1.4f),
                    $"{pair.Key} ({more:0.0}) is a richer version of {pair.Value} ({less:0.0}): its radiance and glow.");
                pairs++;
            }
            Assert.That(pairs, Is.EqualTo(6),
                "The three weapons' evolutions, Ironblood over Vital Surge, Ruinous Edge over Tempered Edge and Blurred Grip over Quickened Grip.");
        }

        // The light a card icon throws on the navy round its glyph: the mean, over its square, of how far its brightest
        // channel rises above the navy wherever the glyph is not.
        private float Light(Sprite sprite)
        {
            RectInt r = Texels(sprite);
            float sum = 0f;
            for (int y = r.y; y < r.yMax; y++)
                for (int x = r.x; x < r.xMax; x++)
                {
                    Color32 c = _painted.GetPixel(x, y);
                    if (c.a > 230)
                        continue;
                    Color32 o = Over(c);
                    sum += Mathf.Max(0, Mathf.Max(o.r - _navy.r, Mathf.Max(o.g - _navy.g, o.b - _navy.b)));
                }
            return sum / (r.width * r.height);
        }

        private Color32 Over(Color32 c)
        {
            float a = c.a / 255f;
            return new Color32((byte)Mathf.RoundToInt(c.r * a + _navy.r * (1 - a)), (byte)Mathf.RoundToInt(c.g * a + _navy.g * (1 - a)),
                (byte)Mathf.RoundToInt(c.b * a + _navy.b * (1 - a)), 255);
        }

        private static RectInt Texels(Sprite sprite) =>
            new RectInt((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);

        private static int Max(Color32 a, Color32 b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));

        private static T Load<T>(string guid) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            Assert.That(asset, Is.Not.Null, $"{typeof(T).Name} {guid}");
            return asset;
        }

        private static Texture2D Painted(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, path);
            return texture;
        }

        // The scene's block for the one component of this script.
        private static string Block(string scene, string scriptPath)
        {
            string key = "m_Script: {fileID: 11500000, guid: " + AssetDatabase.AssetPathToGUID(scriptPath) + ", type: 3}";
            int at = scene.IndexOf(key, StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), $"The scene holds a {Path.GetFileNameWithoutExtension(scriptPath)}.");
            Assert.That(scene.IndexOf(key, at + key.Length, StringComparison.Ordinal), Is.LessThan(0), "Only one.");
            int start = scene.LastIndexOf("\n--- !u!", at, StringComparison.Ordinal);
            int end = scene.IndexOf("\n--- !u!", at, StringComparison.Ordinal);
            return scene.Substring(start, (end < 0 ? scene.Length : end) - start);
        }

        // The GUIDs a list field of the block names, read off the lines that hold `marker`.
        private static List<string> Guids(string block, string field, string marker)
        {
            var guids = new List<string>();
            int at = block.IndexOf("\n  " + field + ":\n", StringComparison.Ordinal);
            Assert.That(at, Is.GreaterThanOrEqualTo(0), $"The block lists {field}.");
            string[] lines = block.Substring(at + field.Length + 5).Split('\n');
            foreach (string line in lines)
            {
                if (!line.StartsWith("  - ", StringComparison.Ordinal) && !line.StartsWith("    ", StringComparison.Ordinal))
                    break;
                if (!line.Contains(marker))
                    continue;
                Match m = Regex.Match(line, "guid: ([0-9a-f]{32})");
                if (m.Success)
                    guids.Add(m.Groups[1].Value);
            }
            return guids;
        }
    }
}
