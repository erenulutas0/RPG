using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cryptforge.Art;
using Cryptforge.Progression;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The between-runs and pause screens painted (Tools/ArtReview/build_screen_art.py): the set the Gameplay scene's
    // ScreenArtView holds, how its sheet imports and what it costs on the phone, the 9-slice borders and that what a
    // slice stretches is plain, and what the sprites paint - the well the icons' own navy, each card state apart from the
    // others, the buttons' states, a victory alight and a defeat gone cold, the frames clear of the words - read back from
    // the sheet as painted (the imported texture is compressed and unreadable).
    public sealed class PaintedScreensTests
    {
        private const string SetPath = "Assets/_Project/Art/UI/Screens/ScreenArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string SecondWindPath = "Assets/_Project/Art/UI/UI_Icon_SecondWind_v1.png";
        // The whole sheet as ASTC 4x4 (a byte a texel): about 0.8 MB on the phone at most.
        private const long BudgetBytes = 838860;

        private ScreenArtSet _set;
        private readonly Dictionary<Texture2D, Texture2D> _painted = new Dictionary<Texture2D, Texture2D>();

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<ScreenArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            foreach (Texture2D texture in _painted.Values)
                Object.DestroyImmediate(texture);
            _painted.Clear();
        }

        private IEnumerable<Sprite> Sliced()
        {
            yield return _set.Surface;
            yield return _set.PausePlate;
            yield return _set.HeaderPlate;
            yield return _set.VictoryPlate;
            yield return _set.DefeatPlate;
            foreach (UnlockStatus status in Statuses)
                yield return _set.CardFor(status);
            foreach (Sprite button in Buttons())
                yield return button;
            yield return _set.PortraitFrame;
        }

        private IEnumerable<Sprite> Buttons()
        {
            yield return _set.Primary;
            yield return _set.PrimaryPressed;
            yield return _set.PrimaryDisabled;
            yield return _set.Secondary;
            yield return _set.SecondaryPressed;
            yield return _set.SecondaryDisabled;
        }

        private IEnumerable<Sprite> All()
        {
            foreach (Sprite sprite in Sliced())
                yield return sprite;
            foreach (Sprite sprite in new[] { _set.VictoryCrest, _set.DefeatCrest, _set.StateAccent, _set.IconWell, _set.PipLit,
                         _set.PipDark, _set.Rule, _set.Stud, _set.Seal })
                yield return sprite;
        }

        private static readonly UnlockStatus[] Statuses =
            { UnlockStatus.Equipped, UnlockStatus.Affordable, UnlockStatus.Owned, UnlockStatus.TooExpensive };

        [Test]
        public void TheSceneHoldsACompleteSetOnOneSheet()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid, Is.True, "Every sprite is there, on one sheet, with its borders.");
            Assert.That(File.ReadAllText(ScenePath), Does.Contain("_art: {fileID: 11400000, guid: " + AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's ScreenArtView holds the set.");
            var states = new HashSet<Sprite>();
            foreach (UnlockStatus status in Statuses)
                states.Add(_set.CardFor(status));
            Assert.That(states.Count, Is.EqualTo(4), "A frame for each of the four states.");
            foreach (Sprite sprite in All())
            {
                Assert.That(sprite.texture, Is.SameAs(_set.Surface.texture), $"{sprite.name}: one sheet, so the screens batch.");
                Assert.That(sprite.name, Does.Not.StartWith("UI_Icon_"), "Never mistaken for an ability's or a card's icon.");
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), $"{sprite.name}: painted at its size on the 1080-wide canvas.");
            }
            Assert.That(_set.VictoryCrest.rect.size, Is.EqualTo(_set.DefeatCrest.rect.size), "The crests stand alike.");
            Assert.That(_set.PipLit.rect.size, Is.EqualTo(_set.PipDark.rect.size));
        }

        [Test]
        public void TheSheetImportsSmoothUnmippedAndAsAstcWithinBudget()
        {
            Texture2D sheet = _set.Surface.texture;
            string path = AssetDatabase.GetAssetPath(sheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), $"{path}: cut into its sprites.");
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), $"{path}: smooth, as the painted HUD.");
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.isReadable, Is.False, path);
            Assert.That(importer.sRGBTexture, Is.True, path);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), $"{path}: cut as painted.");
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True, path);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
            Texture2D painted = Painted(sheet);
            Assert.That((sheet.width, sheet.height), Is.EqualTo((painted.width, painted.height)), $"{path}: imported at its painted size.");
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(sheet.width, sheet.height)), $"{path}: the phone gets it whole.");
            Assert.That(sheet.width % 4 == 0 && sheet.height % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
            Assert.That((long)sheet.width * sheet.height, Is.LessThanOrEqualTo(BudgetBytes), "About 0.8 MB on the phone at most.");
            Assert.That(sheet, Is.Not.SameAs(AssetDatabase.LoadAssetAtPath<HudArtSet>("Assets/_Project/Art/UI/Hud/HudArtSet.asset").Panel.texture),
                "The screens' own sheet: the HUD's stays as it was.");
        }

        [Test]
        public void FramesPlatesAndButtonsAreNineSlicedAndWhatTheyStretchIsPlain()
        {
            foreach (Sprite sprite in Sliced())
            {
                Vector4 b = sprite.border;
                Assert.That(b.x > 0 && b.y > 0 && b.z > 0 && b.w > 0, Is.True, $"{sprite.name}: cut on all four sides.");
                Assert.That(b.x + b.z, Is.LessThan(sprite.rect.width), $"{sprite.name}: a middle left to stretch across.");
                Assert.That(b.y + b.w, Is.LessThan(sprite.rect.height), $"{sprite.name}: and up.");
            }
            Assert.That(_set.Rule.border.x > 0 && _set.Rule.border.z > 0, Is.True, "The rule's tapered ends are kept as it stretches.");
            Assert.That(_set.StateAccent.border.y > 0 && _set.StateAccent.border.w > 0, Is.True, "And the state strip's rounded ends.");
            Assert.That(_set.StateAccent.rect.width, Is.EqualTo(4f), "As wide as the strip's rect.");
            // What a slice stretches across is plain: every column between the left and right borders alike, so no detail is
            // smeared (the portrait's niche is drawn at its own width and never stretched across).
            foreach (Sprite sprite in Sliced())
            {
                if (sprite == _set.PortraitFrame)
                    continue;
                AssertPlainBetween(sprite, true);
            }
            AssertPlainBetween(_set.Rule, true);
            // And what stretches up changes smoothly at most (a face's gradient), row to row.
            foreach (Sprite sprite in new[] { _set.Surface, _set.PausePlate, _set.PortraitFrame }.Concat(Buttons()).Concat(Cards()))
                AssertPlainBetween(sprite, false);
            Assert.That(_set.PortraitFrame.rect.width, Is.EqualTo(108f),
                "The niche's width: the chooser's portrait (95) and its outset (10 left, 3 right), never stretched across.");
        }

        private IEnumerable<Sprite> Cards()
        {
            foreach (UnlockStatus status in Statuses)
                yield return _set.CardFor(status);
        }

        [Test]
        public void TheIconWellIsTheIconsOwnNavyWhereThePictureLies()
        {
            // The relic pictures are painted on the panels' navy, not cut out: the well's inside must be that navy, flat,
            // wherever the picture's square lies, or the square would show.
            var icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(icon.LoadImage(File.ReadAllBytes(SecondWindPath)), Is.True);
                Color32 navy = icon.GetPixel(4, 4);
                Texture2D painted = Painted(_set.IconWell.texture);
                Rect well = _set.IconWell.rect;
                int margin = Mathf.RoundToInt(_set.IconWellMargin);
                Assert.That(well.width, Is.EqualTo(well.height), "Square, as the picture draws.");
                for (int y = margin; y < well.height - margin; y += 5)
                    for (int x = margin; x < well.width - margin; x += 5)
                    {
                        Color32 c = painted.GetPixel((int)well.x + x, (int)well.y + y);
                        Assert.That(Max(c, navy), Is.LessThanOrEqualTo(3), $"The well at ({x}, {y}) is the icon's navy {navy}, not {c}.");
                    }
                Color32 rim = painted.GetPixel((int)well.x + 3, (int)well.center.y);
                Assert.That(Luminance(rim), Is.GreaterThan(Luminance(navy) + 25f), "Round it, a rim catching the light.");
            }
            finally
            {
                Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void EachCardStateReadsApartBeforeItsWordsDo()
        {
            // Along each frame's top, at its middle: the rim (3 texels in) and the inlay (7 in); and a corner's stud.
            Color32 Rim(UnlockStatus s, int depth) => At(_set.CardFor(s), _set.CardFor(s).rect.width / 2f, depth);
            Color32 Stud(UnlockStatus s) => At(_set.CardFor(s), 9.5f, 9.5f);
            Color32 equipped = Rim(UnlockStatus.Equipped, 3), owned = Rim(UnlockStatus.Owned, 3), locked = Rim(UnlockStatus.TooExpensive, 3);
            Assert.That(equipped.r > equipped.b + 60 && Luminance(equipped) > 110f, Is.True, $"Equipped is gilded: {equipped}.");
            Color32 molten = Rim(UnlockStatus.Affordable, 7);
            Assert.That(molten.r > 200 && molten.g > 70 && molten.b < 110, Is.True, $"Affordable's inlay is molten: {molten}.");
            Color32 steel = Rim(UnlockStatus.Owned, 7);
            Assert.That(steel.b >= steel.r && Luminance(steel) > 90f, Is.True, $"Owned's inlay is cold steel: {steel}.");
            float lockedBrightest = 0f;
            for (int depth = 0; depth < 11; depth++)
                lockedBrightest = Mathf.Max(lockedBrightest, Luminance(Rim(UnlockStatus.TooExpensive, depth)));
            Assert.That(lockedBrightest, Is.LessThan(70f), "Locked is soot: no metal on its frame catches the light.");
            Assert.That(Luminance(locked), Is.LessThan(Luminance(owned)), "Darker than an owned card's iron.");
            Assert.That(Stud(UnlockStatus.Equipped).r, Is.GreaterThan(200), "Its corner embers burn.");
            Assert.That(Stud(UnlockStatus.Affordable).r, Is.GreaterThan(180));
            Color32 rivet = Stud(UnlockStatus.Owned);
            Assert.That(rivet.b >= rivet.r, Is.True, $"Owned is studded in steel: {rivet}.");
            Assert.That(Luminance(Stud(UnlockStatus.TooExpensive)), Is.LessThan(60f), "Locked's coals are dead.");
            // The faces: a worn card glows warm round the inside of its frame; a locked one is a shade darker.
            Color32 face = Centre(_set.CardFor(UnlockStatus.Owned));
            Color32 lockedFace = Centre(_set.CardFor(UnlockStatus.TooExpensive));
            Assert.That(Luminance(lockedFace), Is.LessThan(Luminance(face)), "Locked's face is darker.");
            Color32 inside = At(_set.CardFor(UnlockStatus.Equipped), 13f, _set.CardFor(UnlockStatus.Equipped).rect.height / 2f);
            Color32 insideOwned = At(_set.CardFor(UnlockStatus.Owned), 13f, _set.CardFor(UnlockStatus.Owned).rect.height / 2f);
            Assert.That(inside.r, Is.GreaterThan(insideOwned.r + 20), "A warm light inside the gilded frame.");
        }

        [Test]
        public void TheFramesKeepClearOfTheWords()
        {
            // A card's words start 38 texels in and its state strip 18: past 15 texels in from a card's side there is only
            // face (and a worn card's warm light), no metal.
            foreach (UnlockStatus status in Statuses)
            {
                Sprite card = _set.CardFor(status);
                Color32 c = At(card, 15f, card.rect.height / 2f);
                Assert.That(Luminance(c), Is.LessThan(50f), $"{card.name}: face, not frame, 15 texels in ({c}).");
                Assert.That(c.b, Is.GreaterThanOrEqualTo(c.g), $"{card.name}: the face's navy.");
            }
            // The forge's title stands 23 texels under its surface's top edge: 18 in from it, only the face.
            Color32 under = At(_set.Surface, _set.Surface.rect.width / 2f, 18f);
            Assert.That(Luminance(under), Is.LessThan(30f), $"The surface's frame keeps above the title ({under}).");
            Color32 face = Centre(_set.Surface);
            Assert.That(Max(face, new Color32(12, 18, 35, 255)), Is.LessThanOrEqualTo(3), $"Its face is the panels' navy ({face}).");
        }

        [Test]
        public void TheButtonsStatesReadApart()
        {
            foreach ((Sprite normal, Sprite pressed, Sprite disabled) in new[]
                     { (_set.Primary, _set.PrimaryPressed, _set.PrimaryDisabled), (_set.Secondary, _set.SecondaryPressed, _set.SecondaryDisabled) })
            {
                // Pressed, the face sinks: its top lies in the rim's shadow.
                float top = Luminance(At(normal, normal.rect.width / 2f, 15f));
                float pressedTop = Luminance(At(pressed, pressed.rect.width / 2f, 15f));
                Assert.That(pressedTop, Is.LessThan(top - 4f), $"{pressed.name}: sunk, its top in shadow.");
                // Disabled, the metal is dull: its gold inlay (7.5 texels in) is greyer than the live plate's.
                Color32 inlay = At(normal, normal.rect.width / 2f, 7.5f), dull = At(disabled, disabled.rect.width / 2f, 7.5f);
                Assert.That(Saturation(dull), Is.LessThan(Saturation(inlay) - .2f), $"{disabled.name}: dull metal ({dull} against {inlay}).");
                Assert.That(Saturation(Centre(disabled)), Is.LessThan(Saturation(Centre(normal)) + .02f), $"{disabled.name}: a cold face.");
            }
            Color32 primary = Centre(_set.Primary), secondary = Centre(_set.Secondary);
            Assert.That(primary.r - primary.b, Is.GreaterThan(secondary.r - secondary.b + 30), "The primary's face is hot iron, the secondary's cold.");
            Color32 foot = At(_set.Primary, _set.Primary.rect.width / 2f, _set.Primary.rect.height - 13f);
            Assert.That(foot.r, Is.GreaterThan(Centre(_set.Primary).r + 25), "The primary glows at its foot.");
            Assert.That(Luminance(At(_set.Primary, _set.Primary.rect.width / 2f, 3f)), Is.GreaterThan(110f), "In a gold rim.");
        }

        [Test]
        public void AVictoryIsAlightAndADefeatGoneCold()
        {
            int hot = Count(_set.VictoryCrest, c => c.a > 200 && c.r > 230 && c.g > 140);
            int cold = Count(_set.DefeatCrest, c => c.a > 200 && c.r > 230 && c.g > 140);
            Assert.That(hot, Is.GreaterThan(150), "The victory's crest burns.");
            Assert.That(cold, Is.Zero, "The defeat's flames have gone out.");
            Assert.That(Count(_set.DefeatCrest, c => c.a > 200), Is.GreaterThan(Count(_set.VictoryCrest, c => c.a > 200) / 3),
                "Yet they stand in the same shapes.");
            // The plates' rims, 3 texels into the body's top edge (which lies 17 under the plate's), at their middle.
            Color32 gold = At(_set.VictoryPlate, _set.VictoryPlate.rect.width / 2f, 20f);
            Color32 tarnish = At(_set.DefeatPlate, _set.DefeatPlate.rect.width / 2f, 20f);
            Assert.That(Luminance(gold), Is.GreaterThan(Luminance(tarnish) + 40f), $"Gilded ({gold}) against tarnished ({tarnish}).");
            Assert.That(Reddest(_set.VictoryPlate), Is.GreaterThan(230), "The victory plate's bosses blaze.");
            Assert.That(Reddest(_set.DefeatPlate), Is.LessThan(150), "The defeat's are dead.");
        }

        // Every column between the left and right borders alike (across), or every row between the top and foot borders
        // within a step of the next (up).
        private void AssertPlainBetween(Sprite sprite, bool across)
        {
            Texture2D painted = Painted(sprite.texture);
            Rect r = sprite.rect;
            Vector4 b = sprite.border;
            int worst = 0;
            if (across)
            {
                int x0 = (int)(r.x + b.x), x1 = (int)(r.xMax - b.z);
                for (int y = (int)r.y; y < (int)r.yMax; y++)
                {
                    Color32 first = painted.GetPixel(x0, y);
                    for (int x = x0 + 1; x < x1; x++)
                        worst = Mathf.Max(worst, MaxWithAlpha(painted.GetPixel(x, y), first));
                }
                Assert.That(worst, Is.LessThanOrEqualTo(2), $"{sprite.name}: plain across its stretched middle.");
            }
            else
            {
                int y0 = (int)(r.y + b.y), y1 = (int)(r.yMax - b.w);
                for (int x = (int)r.x; x < (int)r.xMax; x++)
                    for (int y = y0 + 1; y < y1; y++)
                        worst = Mathf.Max(worst, MaxWithAlpha(painted.GetPixel(x, y), painted.GetPixel(x, y - 1)));
                Assert.That(worst, Is.LessThanOrEqualTo(4), $"{sprite.name}: smooth up its stretched middle.");
            }
        }

        // A texel of the sprite as painted, x from its left and y from its top.
        private Color32 At(Sprite sprite, float x, float yFromTop)
        {
            Rect r = sprite.rect;
            return Painted(sprite.texture).GetPixel((int)(r.x + x), (int)(r.yMax - 1 - yFromTop));
        }

        private Color32 Centre(Sprite sprite) => At(sprite, sprite.rect.width / 2f, sprite.rect.height / 2f);

        // The reddest solid texel in the sprite's left border, where its boss lies (a clear texel carries colour bled from
        // its neighbours on the sheet, which is never drawn).
        private int Reddest(Sprite sprite)
        {
            Texture2D painted = Painted(sprite.texture);
            Rect r = sprite.rect;
            int reddest = 0;
            for (int y = (int)r.y; y < (int)r.yMax; y++)
                for (int x = (int)r.x; x < (int)(r.x + sprite.border.x); x++)
                {
                    Color32 c = painted.GetPixel(x, y);
                    if (c.a > 200)
                        reddest = Mathf.Max(reddest, c.r);
                }
            return reddest;
        }

        private int Count(Sprite sprite, System.Func<Color32, bool> test)
        {
            Texture2D painted = Painted(sprite.texture);
            Rect r = sprite.rect;
            int count = 0;
            for (int y = (int)r.y; y < (int)r.yMax; y++)
                for (int x = (int)r.x; x < (int)r.xMax; x++)
                    if (test(painted.GetPixel(x, y)))
                        count++;
            return count;
        }

        // The sheet as painted, from its PNG.
        private Texture2D Painted(Texture2D sheet)
        {
            if (!_painted.TryGetValue(sheet, out Texture2D painted))
            {
                painted = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(painted.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(sheet))), Is.True);
                _painted.Add(sheet, painted);
            }
            return painted;
        }

        private static int Max(Color32 a, Color32 b) =>
            Mathf.Max(Mathf.Abs(a.r - b.r), Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b)));

        private static int MaxWithAlpha(Color32 a, Color32 b) => a.a == 0 && b.a == 0 ? 0 : Mathf.Max(Max(a, b), Mathf.Abs(a.a - b.a));

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;

        private static float Saturation(Color32 c)
        {
            float max = Mathf.Max(c.r, Mathf.Max(c.g, c.b)) / 255f, min = Mathf.Min(c.r, Mathf.Min(c.g, c.b)) / 255f;
            return max <= 0f ? 0f : (max - min) / max;
        }
    }
}
