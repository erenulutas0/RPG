using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The gear's items' own painted icons (Tools/ArtReview/build_upgrade_icons.py, ITEMS, 2026-11-17): every armour,
    // amulet and ring the Descent drops names its icon on the items' sheet (Art/UI/ItemIcons), one of its own and none
    // of the upgrade cards' the bag borrowed before (the shield, the gauntlet, the crit sword); the sheet imports as the
    // cards' sheet does; each icon is clear at its edge and fills its square; a slot's three share a silhouette that the
    // other slots' do not, and differ by colour; and the Anvil shows an item's own icon, its slot's card icon when it has
    // none, and a weapon's card icon always. What the icons paint is read back from the sheet as painted (the imported
    // texture is compressed and unreadable).
    public sealed class ItemIconTests
    {
        private const string EconomyPath = "Assets/_Project/Data/Economy/PrototypeEconomy.asset";
        private const string SetPath = "Assets/_Project/Art/UI/UpgradeIcons/UpgradeIconSet.asset";
        private const string SheetPath = "Assets/_Project/Art/UI/ItemIcons/UI_ItemIcons.png";
        private const int Size = 144;
        // The nine together as ASTC 4x4 (a byte a texel): about 0.2 MB on the phone at most.
        private const long BudgetBytes = 200000;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();
        private UpgradeIconSet _set;
        private Texture2D _painted;

        [OneTimeSetUp]
        public void Load()
        {
            var economy = AssetDatabase.LoadAssetAtPath<EconomyConfig>(EconomyPath);
            Assert.That(economy, Is.Not.Null, EconomyPath);
            for (int i = 0; i < economy.ItemCount; i++)
                _items.Add(economy.ItemAt(i));
            _set = AssetDatabase.LoadAssetAtPath<UpgradeIconSet>(SetPath);
            Assert.That(_set, Is.Not.Null, SetPath);
            _painted = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.That(_painted.LoadImage(File.ReadAllBytes(SheetPath)), Is.True, SheetPath);
        }

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
        }

        [Test]
        public void EveryItemHasItsOwnIconAndNoneIsAnUpgradeCards()
        {
            Assert.That(_items.Count, Is.EqualTo(9), "Three floors, an armour, an amulet and a ring each.");
            var cards = new HashSet<Sprite>();
            for (int i = 0; i < _set.Count; i++)
            {
                cards.Add(_set[i].Card);
                if (_set[i].Slot != null)
                    cards.Add(_set[i].Slot);
            }
            var seen = new HashSet<Sprite>();
            foreach (ItemDefinition item in _items)
            {
                Sprite icon = item.Icon;
                Assert.That(icon, Is.Not.Null, $"{item.Id} has its icon.");
                Assert.That(icon.name, Is.EqualTo("UI_ItemIcons_" + item.Id), $"{item.Id}: its own, by its id.");
                Assert.That(AssetDatabase.GetAssetPath(icon), Is.EqualTo(SheetPath), $"{item.Id}: on the items' sheet.");
                Assert.That(seen.Add(icon), Is.True, $"{item.Id}: no other item shows it.");
                Assert.That(cards.Contains(icon), Is.False, $"{item.Id}: not an upgrade card's icon.");
                Assert.That(icon.texture, Is.Not.SameAs(_set[0].Card.texture), $"{item.Id}: not on the cards' sheet.");
            }
            foreach (string borrowed in new[] { "upgrade_armor", "upgrade_attack_speed", "upgrade_crit" })
                Assert.That(seen.Contains(_set.CardIconFor(borrowed)), Is.False, $"No item shows {borrowed}'s card any more.");
        }

        [Test]
        public void EveryItemDefinitionInTheProjectIsInTheEconomyAndHasAnIcon()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { "Assets/_Project" });
            Assert.That(guids.Length, Is.EqualTo(_items.Count), "Every item the project holds is one the economy drops.");
            foreach (string guid in guids)
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                Assert.That(_items, Does.Contain(item), item.Id);
                Assert.That(item.Icon, Is.Not.Null, $"{item.Id} has its icon.");
            }
        }

        [Test]
        public void TheSheetImportsAsTheCardsSheetDoesWithinBudget()
        {
            Texture2D sheet = _items[0].Icon.texture;
            var importer = (TextureImporter)AssetImporter.GetAtPath(SheetPath);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite));
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), "Cut into its icons.");
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), "Smooth, as the painted UI.");
            Assert.That(importer.mipmapEnabled, Is.False);
            Assert.That(importer.alphaIsTransparency, Is.True);
            Assert.That(importer.isReadable, Is.False);
            Assert.That(importer.sRGBTexture, Is.True);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), "Cut as painted.");
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4));
            Assert.That((sheet.width, sheet.height), Is.EqualTo((_painted.width, _painted.height)), "At its painted size.");
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(sheet.width, sheet.height)), "Whole on the phone.");
            Assert.That(sheet.width % 4 == 0 && sheet.height % 4 == 0, Is.True, "Whole ASTC blocks.");
            Assert.That((long)sheet.width * sheet.height, Is.LessThanOrEqualTo(BudgetBytes), "About 0.2 MB on the phone at most.");
            var rects = new List<Rect>();
            foreach (ItemDefinition item in _items)
            {
                Sprite icon = item.Icon;
                Assert.That(icon.texture, Is.SameAs(sheet), $"{icon.name}: one sheet for every item.");
                Assert.That(icon.rect.size, Is.EqualTo(new Vector2(Size, Size)), $"{icon.name}: the card icons' size.");
                Assert.That(icon.pixelsPerUnit, Is.EqualTo(100f), $"{icon.name}: the canvas's own density.");
                Assert.That(icon.pivot, Is.EqualTo(new Vector2(Size / 2f, Size / 2f)), icon.name);
                Assert.That(icon.border, Is.EqualTo(Vector4.zero), $"{icon.name}: drawn whole, never sliced.");
                rects.Add(icon.rect);
            }
            for (int a = 0; a < rects.Count; a++)
                for (int b = a + 1; b < rects.Count; b++)
                {
                    Rect grown = new Rect(rects[a].x - 2, rects[a].y - 2, rects[a].width + 4, rects[a].height + 4);
                    Assert.That(grown.Overlaps(rects[b]), Is.False, $"{rects[a]} and {rects[b]} keep two texels apart.");
                }
        }

        [Test]
        public void EveryIconIsClearAtItsEdgeAndItsGlyphFillsItsSquare()
        {
            foreach (ItemDefinition item in _items)
            {
                RectInt r = Texels(item.Icon);
                for (int k = 0; k < r.width; k++)
                    foreach ((int x, int y) in new[] { (r.x + k, r.y), (r.x + k, r.yMax - 1), (r.x, r.y + k), (r.xMax - 1, r.y + k) })
                        Assert.That(((Color32)_painted.GetPixel(x, y)).a, Is.LessThanOrEqualTo(4),
                            $"{item.Id}: its edge is clear, so it shows no square on any rarity's ground.");
                bool[] glyph = Glyph(item.Icon);
                int x0 = int.MaxValue, y0 = int.MaxValue, x1 = int.MinValue, y1 = int.MinValue;
                for (int y = 0; y < r.height; y++)
                    for (int x = 0; x < r.width; x++)
                        if (glyph[y * r.width + x])
                        {
                            x0 = Mathf.Min(x0, x);
                            y0 = Mathf.Min(y0, y);
                            x1 = Mathf.Max(x1, x);
                            y1 = Mathf.Max(y1, y);
                        }
                Assert.That(x1, Is.GreaterThan(x0), $"{item.Id}: a glyph is painted.");
                float span = Mathf.Max(x1 - x0 + 1, y1 - y0 + 1) / (float)r.width;
                Assert.That(span, Is.InRange(.76f, .96f), $"{item.Id}: its glyph fills its square, reading at the bag's 100.");
                Assert.That((x0 + x1 + 1) / 2f - r.width / 2f, Is.EqualTo(0f).Within(2f), $"{item.Id}: centred across.");
                Assert.That((y0 + y1 + 1) / 2f - r.height / 2f, Is.EqualTo(0f).Within(2f), $"{item.Id}: centred up and down.");
            }
        }

        // At a glance a slot is its silhouette: a mail shirt, a locket hanging from its chain, a ring. Every item's glyph
        // overlaps each of its own slot's more than it overlaps any other slot's item.
        [Test]
        public void ASlotsThreeShareASilhouetteTheOtherSlotsDoNot()
        {
            var glyphs = _items.ToDictionary(item => item, item => Glyph(item.Icon));
            foreach (ItemDefinition item in _items)
            {
                float own = float.MaxValue, other = 0f;
                string nearestOther = null;
                foreach (ItemDefinition next in _items)
                {
                    if (next == item)
                        continue;
                    float overlap = Overlap(glyphs[item], glyphs[next]);
                    if (next.Slot == item.Slot)
                        own = Mathf.Min(own, overlap);
                    else if (overlap > other)
                    {
                        other = overlap;
                        nearestOther = next.Id;
                    }
                }
                Assert.That(own, Is.GreaterThan(other),
                    $"{item.Id}: its slot's silhouette ({own:0.00}) is apart from every other slot's ({nearestOther}, {other:0.00}).");
            }
        }

        // Within a slot the three are their floors' materials, so their colours differ, not their shapes alone: no two of a
        // slot share a palette (the mean colour of their glyphs at least this far apart, 0-255 a channel).
        [Test]
        public void ASlotsThreeAreToldApartByTheirColour()
        {
            const float Apart = 16f;
            var means = _items.ToDictionary(item => item, item => MeanColour(item.Icon));
            for (int a = 0; a < _items.Count; a++)
                for (int b = a + 1; b < _items.Count; b++)
                {
                    if (_items[a].Slot != _items[b].Slot)
                        continue;
                    float distance = Vector3.Distance(means[_items[a]], means[_items[b]]);
                    Assert.That(distance, Is.GreaterThanOrEqualTo(Apart),
                        $"{_items[a].Id} and {_items[b].Id} are told apart by their colour ({distance:0.0}).");
                }
            var floors = new HashSet<(GearSlot, int)>(_items.Select(item => (item.Slot, item.Floor)));
            Assert.That(floors.Count, Is.EqualTo(_items.Count), "Each slot's three come from three floors.");
        }

        [Test]
        public void TheAnvilShowsAnItemsOwnIconElseItsSlotsCardAndAWeaponsCardAlways()
        {
            foreach (ItemDefinition item in _items)
            {
                Assert.That(AnvilView.IconOf(item.Slot, item.Id, item.Icon, _set), Is.SameAs(item.Icon), $"{item.Id}: its own.");
                Assert.That(AnvilView.IconOf(item.Slot, item.Id, item.Icon, null), Is.SameAs(item.Icon),
                    $"{item.Id}: its own even without the cards' set.");
            }
            Assert.That(AnvilView.IconOf(GearSlot.Armour, "item_new_mail", null, _set), Is.SameAs(_set.CardIconFor("upgrade_armor")),
                "An armour without its own icon shows the armour card's, as every armour did before.");
            Assert.That(AnvilView.IconOf(GearSlot.Amulet, "item_new_locket", null, _set),
                Is.SameAs(_set.CardIconFor("upgrade_attack_speed")), "An amulet without one, the attack speed card's.");
            Assert.That(AnvilView.IconOf(GearSlot.Ring, "item_new_ring", null, _set), Is.SameAs(_set.CardIconFor("upgrade_crit")),
                "A ring without one, the crit card's.");
            Assert.That(AnvilView.IconOf(GearSlot.Ring, "item_new_ring", null, null), Is.Null, "Neither: no icon.");
            Sprite sword = _set.CardIconFor("weapon_sword");
            Assert.That(sword, Is.Not.Null);
            Assert.That(AnvilView.IconOf(GearSlot.Weapon, "weapon_sword", _items[0].Icon, _set), Is.SameAs(sword),
                "A weapon shows its own card icon, never an item's.");
        }

        // The cells of the icon's glyph (painted opaque), row by row from its bottom.
        private bool[] Glyph(Sprite sprite)
        {
            RectInt r = Texels(sprite);
            var glyph = new bool[r.width * r.height];
            for (int y = 0; y < r.height; y++)
                for (int x = 0; x < r.width; x++)
                    glyph[y * r.width + x] = ((Color32)_painted.GetPixel(r.x + x, r.y + y)).a > 230;
            return glyph;
        }

        private static float Overlap(bool[] a, bool[] b)
        {
            int both = 0, either = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] && b[i])
                    both++;
                if (a[i] || b[i])
                    either++;
            }
            return either > 0 ? both / (float)either : 0f;
        }

        private Vector3 MeanColour(Sprite sprite)
        {
            RectInt r = Texels(sprite);
            Vector3 sum = Vector3.zero;
            int count = 0;
            for (int y = r.y; y < r.yMax; y++)
                for (int x = r.x; x < r.xMax; x++)
                {
                    Color32 c = _painted.GetPixel(x, y);
                    if (c.a <= 230)
                        continue;
                    sum += new Vector3(c.r, c.g, c.b);
                    count++;
                }
            return count > 0 ? sum / count : Vector3.zero;
        }

        private static RectInt Texels(Sprite sprite) =>
            new RectInt((int)sprite.rect.x, (int)sprite.rect.y, (int)sprite.rect.width, (int)sprite.rect.height);
    }
}
