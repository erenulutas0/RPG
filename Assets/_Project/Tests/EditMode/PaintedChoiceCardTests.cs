using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The run's choice cards painted (Tools/ArtReview/build_choice_card_art.py): the set the Gameplay scene's RunChoiceView
    // holds, how its sheet imports and what it costs on the phone, the 9-slice borders and how far a card's glow may
    // reach past it, and what the sprites paint, read back from the sheet as painted (the imported texture is compressed
    // and unreadable): a dark face for the labels, the icons' navy in every well, each tier's own colour, the evolution
    // outranking gold, and the press painted light for the button's colours to tint.
    public sealed class PaintedChoiceCardTests
    {
        private const string SetPath = "Assets/_Project/Art/UI/ChoiceCards/ChoiceCardArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string GoldIconPath = "Assets/_Project/Art/UI/UI_Icon_Gold_v1.png";
        // Every card and panel sprite together, as ASTC 4x4 (a byte a texel): about 0.6 MB on the phone at most.
        private const long BudgetBytes = 629145;
        // The stacked cards stand 28 apart, so a glow keeps to half that above and below a card; a card's sides stand 48
        // in from the surface's edge, 30 from its frame's face.
        private const float StackedGap = 28f;
        private const float SideRoom = 30f;

        private ChoiceCardArtSet _art;
        private Texture2D _painted;

        [OneTimeSetUp]
        public void Load()
        {
            _art = AssetDatabase.LoadAssetAtPath<ChoiceCardArtSet>(SetPath);
        }

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
        }

        private IEnumerable<(string name, ChoiceCardArtSet.Treatment look)> Treatments()
        {
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
                yield return (((UpgradeRarity)tier).ToString(), _art.Tier((UpgradeRarity)tier));
            yield return ("Evolution", _art.Evolution);
            yield return ("Neutral", _art.Neutral);
            yield return ("Extract", _art.Extract);
            yield return ("Climb", _art.Climb);
        }

        private IEnumerable<Sprite> Sprites()
        {
            yield return _art.Backdrop;
            yield return _art.Surface;
            yield return _art.TitlePlate;
            yield return _art.TitleCrest;
            yield return _art.Description;
            yield return _art.WellLip;
            yield return _art.Press;
            foreach ((string _, ChoiceCardArtSet.Treatment look) in Treatments())
            {
                yield return look.Frame;
                if (look.Aura != null)
                    yield return look.Aura;
                if (look.Crest != null)
                    yield return look.Crest;
                yield return look.NamePlate;
                yield return look.Well;
                yield return look.Sigil;
            }
        }

        [Test]
        public void TheSceneHoldsACompleteSetOnOneSheet()
        {
            Assert.That(_art, Is.Not.Null, SetPath);
            Assert.That(_art.IsValid, Is.True, "Every card and panel sprite is there, on one sheet, with its borders.");
            Assert.That(File.ReadAllText(ScenePath),
                Does.Contain("_cardArt: {fileID: 11400000, guid: " + AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's RunChoiceView holds the set.");
            var frames = new HashSet<Sprite>();
            foreach ((string name, ChoiceCardArtSet.Treatment look) in Treatments())
            {
                Assert.That(look, Is.Not.Null, name);
                Assert.That(frames.Add(look.Frame), Is.True, $"{name}: a frame of its own.");
                Assert.That(look.PressColour.a, Is.GreaterThan(.5f), $"{name}: a press that shows.");
            }
            foreach (Sprite sprite in Sprites())
            {
                Assert.That(sprite.texture, Is.SameAs(_art.Surface.texture), $"{sprite.name}: one sheet, so the cards batch.");
                Assert.That(sprite.name, Does.Not.StartWith("UI_Icon_"), "Never mistaken for a card's icon.");
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), $"{sprite.name}: painted at its size on the 1080-wide canvas.");
            }
            // Gold and the evolution wear auras and crests; the other tiers and the neutral cards do not.
            foreach ((string name, ChoiceCardArtSet.Treatment look) in Treatments())
            {
                bool crowned = look == _art.Tier(UpgradeRarity.Gold) || look == _art.Evolution;
                Assert.That(look.Aura != null, Is.EqualTo(crowned), $"{name}: an aura only for gold and the evolution.");
                Assert.That(look.Crest != null, Is.EqualTo(crowned), $"{name}: a crest only for gold and the evolution.");
            }
            Assert.That(_art.Tier((UpgradeRarity)RarityTable.TierCount), Is.Null, "No treatment outside the four tiers.");
        }

        [Test]
        public void TheSheetImportsSmoothUnmippedAndAsAstcWithinBudget()
        {
            Texture2D sheet = _art.Surface.texture;
            string path = AssetDatabase.GetAssetPath(sheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), $"{path}: cut into its sprites.");
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), $"{path}: smooth, as the painted HUD.");
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.isReadable, Is.False, path);
            Assert.That(importer.sRGBTexture, Is.True, path);
            Assert.That(importer.wrapMode, Is.EqualTo(TextureWrapMode.Clamp), path);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), $"{path}: cut as painted.");
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True, path);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
            Texture2D painted = Painted();
            Assert.That((sheet.width, sheet.height), Is.EqualTo((painted.width, painted.height)), $"{path}: imported at its painted size.");
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(sheet.width, sheet.height)), $"{path}: the phone gets it whole.");
            Assert.That(sheet.width % 4 == 0 && sheet.height % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
            Assert.That((long)sheet.width * sheet.height, Is.LessThanOrEqualTo(BudgetBytes), "About 0.6 MB on the phone at most.");
        }

        [Test]
        public void FramesPlatesWellsAndThePressAreNineSlicedAndGlowsKeepToTheirRoom()
        {
            var sliced = new List<Sprite> { _art.Surface, _art.TitlePlate, _art.Description, _art.WellLip, _art.Press };
            foreach ((string _, ChoiceCardArtSet.Treatment look) in Treatments())
            {
                sliced.Add(look.Frame);
                sliced.Add(look.Well);
            }
            foreach (Sprite sprite in sliced)
            {
                Vector4 b = sprite.border;
                Assert.That(b.x > 0 && b.y > 0 && b.z > 0 && b.w > 0, Is.True, $"{sprite.name}: cut on all four sides.");
                Assert.That(b.x + b.z, Is.LessThan(sprite.rect.width), $"{sprite.name}: a middle left to stretch across.");
                Assert.That(b.y + b.w, Is.LessThan(sprite.rect.height), $"{sprite.name}: and up.");
            }
            foreach ((string name, ChoiceCardArtSet.Treatment look) in Treatments())
            {
                // A stacked card is 190 tall: its frame's corners must fit it twice over.
                Assert.That(look.Frame.border.y + look.Frame.border.w, Is.LessThan(190f), $"{name}: fits a stacked card.");
                Assert.That(look.Frame.border, Is.EqualTo(_art.Press.border), $"{name}: the press lies exactly over its frame.");
                Vector4 nb = look.NamePlate.border;
                Assert.That(nb.x > 0 && nb.z > 0 && nb.x + nb.z < look.NamePlate.rect.width, Is.True,
                    $"{name}: the name plate keeps its ends as it stretches.");
                if (look.Aura == null)
                    continue;
                Vector4 o = look.AuraOutset, ab = look.Aura.border;
                Assert.That(ab.x > o.x && ab.y > o.y && ab.z > o.z && ab.w > o.w, Is.True,
                    $"{name}: what reaches past the card ({o}) lies in the aura's borders ({ab}), never stretched.");
                Assert.That(Mathf.Max(o.y, o.w), Is.LessThanOrEqualTo(StackedGap / 2f), $"{name}: clear of the card above and below.");
                Assert.That(Mathf.Max(o.x, o.z), Is.LessThanOrEqualTo(SideRoom), $"{name}: inside the surface's frame.");
                Assert.That(ab.x + ab.z, Is.LessThan(look.Aura.rect.width), name);
                Assert.That(ab.y + ab.w, Is.LessThan(look.Aura.rect.height), name);
            }
            // The crest stands on the card's top edge: its pivot inside it, and no more than half the gap above the edge.
            foreach (Sprite crest in new[] { _art.Tier(UpgradeRarity.Gold).Crest, _art.Evolution.Crest })
            {
                float above = crest.rect.height - crest.pivot.y;
                Assert.That(crest.pivot.y, Is.GreaterThan(0f).And.LessThan(crest.rect.height), crest.name);
                Assert.That(above, Is.LessThanOrEqualTo(StackedGap / 2f + 2f), $"{crest.name}: {above} above the card's edge.");
            }
            Assert.That(_art.WellMargin, Is.InRange(4f, 24f), "The well frames the icon, inside the frame's corner brackets.");
            Assert.That(_art.SigilScale, Is.InRange(.5f, 1f), "The sigil sits inside the well's floor.");
        }

        [Test]
        public void TheLabelsReadOnADarkFaceAndEveryWellIsTheIconsNavy()
        {
            // The painted icons carry the old panel's navy as their background: a well of another colour would show them
            // as squares.
            var icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(icon.LoadImage(File.ReadAllBytes(GoldIconPath)), Is.True);
                Color32 navy = icon.GetPixel(4, 4);
                foreach ((string name, ChoiceCardArtSet.Treatment look) in Treatments())
                {
                    Color32 floor = At(look.Well, .5f, .5f);
                    Assert.That(Max(floor, navy), Is.LessThanOrEqualTo(3), $"{name}: the well's floor {floor} is the icons' navy {navy}.");
                    Color32 face = At(look.Frame, .5f, .5f);
                    Assert.That(face.a, Is.EqualTo(255), $"{name}: an opaque face.");
                    Assert.That(Luminance(face), Is.LessThan(30f), $"{name}: a dark face {face}, so every label reads on it.");
                }
                Assert.That(Max(At(_art.Neutral.Frame, .5f, .5f), navy), Is.LessThanOrEqualTo(3),
                    "The forge's neutral card keeps the HUD's navy face.");
                Assert.That(Max(At(_art.Surface, .5f, .5f), navy), Is.LessThanOrEqualTo(3), "So does the surface.");
            }
            finally
            {
                Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void EachTierReadsInItsOwnColourAndTheEvolutionOutranksGold()
        {
            // The enamel inlay round the frame, half-way down its left side, 8.5 in from its edge.
            Color32 Inlay(ChoiceCardArtSet.Treatment look) => At(look.Frame, 8.5f / look.Frame.rect.width, .5f);
            Color32 green = Inlay(_art.Tier(UpgradeRarity.Common));
            Color32 orange = Inlay(_art.Tier(UpgradeRarity.Rare));
            Color32 red = Inlay(_art.Tier(UpgradeRarity.Epic));
            Color32 gold = Inlay(_art.Tier(UpgradeRarity.Gold));
            Assert.That(green.g > green.r + 60 && green.g > green.b + 40, Is.True, $"Green's inlay is green, not {green}.");
            Assert.That(orange.r > orange.g + 50 && orange.g > orange.b + 50, Is.True, $"Orange's is orange, not {orange}.");
            Assert.That(red.r > red.g + 120 && red.r > red.b + 100, Is.True, $"Red's is red, not {red}.");
            Assert.That(gold.r > gold.b + 40 && gold.g > gold.b + 40 && gold.g > orange.g + 50, Is.True,
                $"Gold's is gold, far from orange's {orange}, not {gold}.");
            // Every tier's stone in its well is its own colour too.
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
            {
                Color32 stone = At(_art.Tier((UpgradeRarity)tier).Sigil, .5f, .5f);
                Color tierColour = Cryptforge.UI.RunChoiceView.RarityColour((UpgradeRarity)tier);
                Assert.That(HueDistance(stone, tierColour), Is.LessThan(.09f),
                    $"{(UpgradeRarity)tier}'s stone {stone} is its tier's colour {(Color32)tierColour}.");
            }
            // The evolution reaches further past its card than gold, on every side, and its crest is wider.
            ChoiceCardArtSet.Treatment goldLook = _art.Tier(UpgradeRarity.Gold), evolution = _art.Evolution;
            for (int side = 0; side < 4; side++)
                Assert.That(evolution.AuraOutset[side], Is.GreaterThanOrEqualTo(goldLook.AuraOutset[side]), $"Side {side}.");
            Assert.That(evolution.AuraOutset.x, Is.GreaterThan(goldLook.AuraOutset.x), "Its horns sweep out past gold's glow.");
            Assert.That(evolution.Crest.rect.width, Is.GreaterThan(goldLook.Crest.rect.width * 3f), "A crown of fire, not a clasp.");
        }

        [Test]
        public void ThePressAndTheSharedPartsArePaintedForTheirTints()
        {
            // The press is painted white wherever it shows, so the button's colours alone light or shade a card; it is
            // faint over the face and strongest just inside the rim.
            Texture2D painted = Painted();
            Rect press = _art.Press.rect;
            for (int y = (int)press.yMin; y < (int)press.yMax; y += 3)
                for (int x = (int)press.xMin; x < (int)press.xMax; x += 3)
                {
                    Color32 c = painted.GetPixel(x, y);
                    if (c.a > 0)
                        Assert.That(Mathf.Min(c.r, Mathf.Min(c.g, c.b)), Is.GreaterThanOrEqualTo(250), $"The press at ({x}, {y}) is {c}.");
                }
            Color32 middle = At(_art.Press, .5f, .5f);
            Color32 inside = At(_art.Press, 14f / press.width, .5f);
            Assert.That(middle.a, Is.InRange(10, 60), "A faint veil over the face.");
            Assert.That(inside.a, Is.GreaterThan(middle.a + 120), "A light falling inward from the rim.");
            Assert.That(_art.DisabledColour.a, Is.GreaterThan(.4f), "A waiting card is visibly shaded.");
            Assert.That(Luminance(_art.DisabledColour), Is.LessThan(.1f * 255f), "In shade, not in light.");
            // The backdrop is the old dim, deepest at the screen's corners; opaque enough that the run below stays behind.
            Color32 centre = At(_art.Backdrop, .5f, .5f), corner = At(_art.Backdrop, .01f, .99f);
            Assert.That(centre.a, Is.InRange(190, 235), $"{centre}");
            Assert.That(corner.a, Is.GreaterThan(centre.a), "Deeper at the corners.");
            // Its edge texels are repeated past its rect on the sheet, so a stretched backdrop never fades at the screen's
            // edges.
            Rect back = _art.Backdrop.rect;
            Assert.That(painted.GetPixel((int)back.xMin - 1, (int)back.center.y).a, Is.EqualTo(painted.GetPixel((int)back.xMin, (int)back.center.y).a));
            Assert.That(painted.GetPixel((int)back.center.x, (int)back.yMax).a, Is.EqualTo(painted.GetPixel((int)back.center.x, (int)back.yMax - 1).a));
        }

        // A texel of a sprite, at a share of its width and height from its bottom left, as painted.
        private Color32 At(Sprite sprite, float u, float v)
        {
            Rect r = sprite.rect;
            return Painted().GetPixel((int)(r.x + Mathf.Clamp(u * r.width, 0f, r.width - 1f)),
                (int)(r.y + Mathf.Clamp(v * r.height, 0f, r.height - 1f)));
        }

        // The sheet as painted, from its PNG.
        private Texture2D Painted()
        {
            if (_painted == null)
            {
                _painted = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(_painted.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(_art.Surface.texture))), Is.True);
            }
            return _painted;
        }

        private static float HueDistance(Color32 a, Color b)
        {
            Color.RGBToHSV(a, out float ha, out _, out _);
            Color.RGBToHSV(b, out float hb, out _, out _);
            float d = Mathf.Abs(ha - hb);
            return Mathf.Min(d, 1f - d);
        }

        private static int Max(Color32 a, Color32 b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;

        private static float Luminance(Color c) => Luminance((Color32)c);
    }
}
