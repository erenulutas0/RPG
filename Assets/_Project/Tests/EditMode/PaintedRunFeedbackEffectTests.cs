using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The run's growth painted (painted by Tools/ArtReview/build_run_feedback_vfx.py): the set the Gameplay scene's
    // CombatSetup hands RunFeedbackView, how its one sheet imports and what it costs on the phone, and what each strip
    // paints, read back from the sheet as painted (the imported texture is compressed and unreadable).
    public sealed class PaintedRunFeedbackEffectTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/RunFeedback/RunFeedbackArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The sheet as ASTC 4x4 (a byte a texel): the run's growth may spend about half a megabyte on the phone.
        private const int BudgetBytes = 512 * 1024;

        private RunFeedbackArtSet _set;
        private Texture2D _painted;

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<RunFeedbackArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
            _painted = null;
        }

        private IEnumerable<SkillEffectArtSet.Strip> Strips()
        {
            yield return _set.LevelRing;
            yield return _set.CardRing;
            yield return _set.CardCore;
            yield return _set.EmberRing;
            yield return _set.GoldSpark;
            yield return _set.EmberSpark;
        }

        [Test]
        public void TheSceneHandsTheViewACompleteSetOnOneSheet()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid, Is.True);
            string scene = File.ReadAllText(ScenePath);
            Assert.That(scene, Does.Contain("_runFeedbackArt: {fileID: 11400000, guid: " + AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatSetup hands it to the view.");

            Texture2D sheet = _set.LevelRing.Sheet;
            foreach (SkillEffectArtSet.Strip strip in Strips())
            {
                Assert.That(strip.Sheet, Is.SameAs(sheet), "One sheet, so every ring on the floor batches.");
                Assert.That((strip.Sheet.width, strip.Sheet.height), Is.EqualTo((strip.SheetWidth, strip.SheetHeight)),
                    "It imports at the size it was painted, so its cells cut true.");
            }
            Assert.That(_set.LevelRing.Length, Is.GreaterThanOrEqualTo(4), "Painted frames, where the line had one shape.");
            Assert.That(_set.CardRing.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(_set.CardCore.Length, Is.GreaterThanOrEqualTo(3));
            Assert.That(_set.EmberRing.Length, Is.GreaterThanOrEqualTo(4));
            Assert.That(_set.GoldSpark.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(_set.EmberSpark.Length, Is.EqualTo(_set.GoldSpark.Length), "The two sparks fade alike.");

            // No cell reaches another, even by the texel bilinear filtering reads past its edge.
            var taken = new List<RectInt>();
            foreach (SkillEffectArtSet.Strip strip in Strips())
            {
                foreach (SkillEffectArtSet.Cell cell in strip.Frames)
                {
                    var grown = new RectInt(cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2);
                    foreach (RectInt other in taken)
                        Assert.That(grown.Overlaps(other), Is.False, $"{grown} overlaps {other}.");
                    taken.Add(new RectInt(cell.X, cell.Y, cell.Width, cell.Height));
                }
            }
        }

        [Test]
        public void TheSheetImportsSmoothUnmippedAndAsAstcWithinBudget()
        {
            SkillEffectArtSet.Strip strip = _set.CardRing;
            string path = AssetDatabase.GetAssetPath(strip.Sheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), $"{path}: smooth, as the painted actors.");
            Assert.That(importer.mipmapEnabled, Is.False, path);
            Assert.That(importer.alphaIsTransparency, Is.True, path);
            Assert.That(importer.isReadable, Is.False, path);
            Assert.That(importer.sRGBTexture, Is.True, path);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None), $"{path}: cut as painted.");
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True, path);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4), path);
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(strip.SheetWidth, strip.SheetHeight)),
                $"{path}: the phone gets it whole.");
            Assert.That(strip.SheetWidth % 4 == 0 && strip.SheetHeight % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
            Assert.That((long)strip.SheetWidth * strip.SheetHeight, Is.LessThanOrEqualTo(BudgetBytes),
                "Half a megabyte on the phone at most.");
        }

        // The view scales every ring to the outer edge of the line it stands for, whose contour at scale 1 lies a world
        // unit out across the floor and, half as far, up it (ArenaFloor.DepthScale): so must each ring's leading edge,
        // the outermost of its light. The card's ring and core are crisp shapes with nothing light past it; the level
        // ring's glints and the ember ring's spatter fly off it, but never far.
        [Test]
        public void EveryRingIsAUnitFloorEllipseCentredOnTheFeet()
        {
            foreach ((SkillEffectArtSet.Strip ring, string name, System.Func<Color32, bool> edge, bool crisp) in new
                         (SkillEffectArtSet.Strip, string, System.Func<Color32, bool>, bool)[]
                     {
                         (_set.LevelRing, "The level ring", c => c.a > 128 && Luminance(c) > 150f, false),
                         (_set.CardRing, "The card ring", c => c.a > 128 && c.r > 160, true),
                         (_set.CardCore, "The card's core", c => c.a > 128 && c.r > 160, true),
                         (_set.EmberRing, "The ember ring", c => c.a > 128 && Luminance(c) > 160f, false),
                     })
            {
                Assert.That(ring.Pivot.x, Is.EqualTo(.5f).Within(.02f), $"{name} pins at its centre, on the hero's feet.");
                Assert.That(ring.Pivot.y, Is.EqualTo(.5f).Within(.08f), name);
                float tolerance = 3f / ring.TexelsPerUnit;
                for (int i = 0; i < 2; i++)
                {
                    Color32[] texels = Frame(ring, i, out int width, out int height);
                    string at = $"{name}, frame {i}";
                    Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                    int cx = Mathf.RoundToInt(ring.Pivot.x * width), cy = Mathf.RoundToInt(ring.Pivot.y * height);
                    float right = (OutermostAlong(texels, width, height, cx, cy, 1, 0, edge) - cx) / ring.TexelsPerUnit;
                    float left = (cx - OutermostAlong(texels, width, height, cx, cy, -1, 0, edge)) / ring.TexelsPerUnit;
                    float near = (cy - OutermostAlong(texels, width, height, cx, cy, 0, -1, edge)) / ring.TexelsPerUnit;
                    Assert.That(right, Is.EqualTo(1f).Within(tolerance), $"{at}: its leading edge a unit out to the right.");
                    Assert.That(left, Is.EqualTo(1f).Within(tolerance), $"{at}: and to the left.");
                    Assert.That(near, Is.EqualTo(ArenaFloor.DepthScale).Within(tolerance),
                        $"{at}: half as far toward the camera.");
                }
                Assert.That(ring.Frames[0].Width * .5f / ring.TexelsPerUnit, Is.InRange(1f, 1.1f),
                    $"{name}: a unit ring with room for its edge and what flies off it.");
                if (!crisp)
                    continue;
                // Nothing light of any frame lies past the contour: only the soft glow and the dark rim.
                for (int i = 0; i < ring.Length; i++)
                {
                    Color32[] texels = Frame(ring, i, out int width, out int height);
                    float cx = ring.Pivot.x * width, cy = ring.Pivot.y * height;
                    float widest = 0f;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (!edge(texels[y * width + x]))
                                continue;
                            float u = (x + .5f - cx) / ring.TexelsPerUnit, v = (y + .5f - cy) / ring.TexelsPerUnit;
                            widest = Mathf.Max(widest, Mathf.Sqrt(u * u + v / ArenaFloor.DepthScale * (v / ArenaFloor.DepthScale)));
                        }
                    }
                    Assert.That(widest, Is.LessThan(1f + tolerance * 1.5f), $"{name}, frame {i}: nothing bright past the contour.");
                }
            }
        }

        [Test]
        public void TheCardRingAndCoreArePaintedLightAndNeutralSoEveryTierKeepsItsOwnHue()
        {
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.CardRing, _set.CardCore })
            {
                string name = strip == _set.CardRing ? "The card ring" : "The card's core";
                for (int i = 0; i < strip.Length; i++)
                {
                    Color32[] texels = Frame(strip, i, out _, out _);
                    int brightest = 0, opaque = 0;
                    foreach (Color32 c in texels)
                    {
                        if (c.a < 24)
                            continue;
                        Assert.That(Mathf.Max(Mathf.Abs(c.r - c.g), Mathf.Abs(c.g - c.b), Mathf.Abs(c.r - c.b)),
                            Is.LessThanOrEqualTo(2), $"{name}, frame {i}: grey, so the view's tint is the only hue.");
                        if (c.a > 200)
                        {
                            opaque++;
                            brightest = Mathf.Max(brightest, c.r);
                        }
                    }
                    Assert.That(opaque, Is.GreaterThan(0), $"{name}, frame {i}: a solid band.");
                    Assert.That(brightest, Is.GreaterThanOrEqualTo(250), $"{name}, frame {i}: lit to white, so the tint shows whole.");
                }
            }
            // Tinted, the band is the tier's own colour, from its full value at the lit line to a shade of it.
            Color32[] band = Frame(_set.CardRing, 2, out int w, out int h);
            for (int tier = 0; tier < RarityTable.TierCount; tier++)
            {
                Color colour = RunChoiceView.RarityColour((UpgradeRarity)tier);
                int solid = 0, dim = 0;
                Color lit = Color.black;
                foreach (Color32 c in band)
                {
                    if (c.a <= 200 || c.r < 120)
                        continue;
                    solid++;
                    Color shown = colour * (c.r / 255f);
                    if (shown.maxColorComponent > lit.maxColorComponent)
                        lit = shown;
                    if (c.r < 180)
                        dim++;
                }
                Assert.That(lit.r, Is.EqualTo(colour.r).Within(.02f), $"{(UpgradeRarity)tier}: the lit line is the tier's colour.");
                Assert.That(lit.g, Is.EqualTo(colour.g).Within(.02f));
                Assert.That(lit.b, Is.EqualTo(colour.b).Within(.02f));
                Assert.That(dim, Is.LessThan(solid / 4), $"{(UpgradeRarity)tier}: never a muddy shade of it.");
            }
        }

        [Test]
        public void TheCardRingStandsOnADarkRimAndItsCoreFlashesWholeThenOpens()
        {
            // The light band is what reads on the dark stone, the dark rim outside it what reads on the orange lava; the
            // first frame has none, as the core still flashes over it.
            SkillEffectArtSet.Strip ring = _set.CardRing;
            for (int i = 0; i < ring.Length - 1; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                int cx = Mathf.RoundToInt(ring.Pivot.x * width), cy = Mathf.RoundToInt(ring.Pivot.y * height);
                bool dark = false;
                for (int x = cx + Mathf.RoundToInt(.99f * ring.TexelsPerUnit); x < width; x++)
                {
                    Color32 c = texels[cy * width + x];
                    dark |= c.a > 64 && c.r < 60;
                }
                Assert.That(dark, Is.EqualTo(i > 0), $"Frame {i}: {(i > 0 ? "a dark rim" : "no rim yet")} outside the band.");
                Assert.That(texels[cy * width + cx].a, Is.LessThan(16), $"Frame {i}: the hero stands in a clear middle.");
            }

            SkillEffectArtSet.Strip core = _set.CardCore;
            Color32[] first = Frame(core, 0, out int cw, out int ch);
            Color32 heart = first[Mathf.RoundToInt(core.Pivot.y * ch) * cw + Mathf.RoundToInt(core.Pivot.x * cw)];
            Assert.That(heart.a > 230 && heart.r > 230, Is.True, "The core flashes whole on the floor at the feet.");
            Color32[] last = Frame(core, core.Length - 1, out cw, out ch);
            heart = last[Mathf.RoundToInt(core.Pivot.y * ch) * cw + Mathf.RoundToInt(core.Pivot.x * cw)];
            Assert.That(heart.a, Is.LessThan(16), "Then opens into a ring.");
            Assert.That(Count(last, IsVisible), Is.LessThan(Count(first, IsVisible) / 2), "And thins as it goes.");
        }

        [Test]
        public void TheLevelRingIsWarmGoldLightAndTheEmberRingTheForgesEmber()
        {
            // Light, not a band in a tier's colour: warm gold round a line of white gold, never a hue of the four tiers
            // alone, and only a faint shadow outside it.
            SkillEffectArtSet.Strip level = _set.LevelRing;
            for (int i = 0; i < level.Length; i++)
            {
                Color32[] texels = Frame(level, i, out _, out _);
                int visible = Count(texels, IsVisible);
                Assert.That(Count(texels, c => IsVisible(c) && c.r >= c.g && c.g >= c.b && c.r > 200), Is.GreaterThan(visible * 9 / 10),
                    $"The level ring, frame {i}: warm gold.");
                Assert.That(Count(texels, c => c.a > 128 && Luminance(c) < 70f), Is.Zero, $"The level ring, frame {i}: no dark rim.");
                if (i < 3)
                    Assert.That(Count(texels, c => c.a > 200 && c.r > 245 && c.g > 235 && c.b > 190), Is.GreaterThan(0),
                        $"The level ring, frame {i}: a line of white-gold light.");
            }
            // Forge metal: white-hot as it lights, cooling to ember and crimson, never green or blue.
            SkillEffectArtSet.Strip ember = _set.EmberRing;
            for (int i = 0; i < ember.Length; i++)
            {
                Color32[] texels = Frame(ember, i, out _, out _);
                Assert.That(Count(texels, c => IsVisible(c) && (c.g > c.r || c.b > c.r)), Is.Zero,
                    $"The ember ring, frame {i}: red always leads, never a cool hue.");
                if (i == 0)
                    Assert.That(Count(texels, c => c.a > 200 && c.r > 245 && c.g > 225), Is.GreaterThan(0), "It lights white-hot.");
            }
            Color32[] cooled = Frame(ember, ember.Length - 1, out _, out _);
            Assert.That(Count(cooled, c => c.a > 200 && c.r > 245 && c.g > 225), Is.Zero, "And cools as it fades.");
        }

        [Test]
        public void TheSparksAreStreaksWithTheirHeadsAtThePivotGoldAndEmber()
        {
            Assert.That(_set.SparkWidth, Is.EqualTo(.05f).Within(1e-4f), "Painted as wide as the level-up's rising line.");
            foreach (SkillEffectArtSet.Strip spark in new[] { _set.GoldSpark, _set.EmberSpark })
            {
                string name = spark == _set.GoldSpark ? "The gold spark" : "The ember spark";
                for (int i = 0; i < spark.Length; i++)
                {
                    Color32[] texels = Frame(spark, i, out int width, out int height);
                    string at = $"{name}, frame {i}";
                    Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                    int hx = Mathf.RoundToInt(spark.Pivot.x * width - .5f), hy = Mathf.RoundToInt(spark.Pivot.y * height - .5f);
                    Color32 head = texels[hy * width + hx];
                    Assert.That(head.a > 220 && Luminance(head) > 200f, Is.True, $"{at}: a hot head at the pivot.");
                    // Its tail trails behind it along -x, and little lies ahead of the head but its glow.
                    int behind = 0, ahead = 0;
                    for (int x = 0; x < width; x++)
                    {
                        if (texels[hy * width + x].a <= 64)
                            continue;
                        if (x < hx - 4)
                            behind++;
                        else if (x > hx + 4)
                            ahead++;
                    }
                    Assert.That(behind, Is.GreaterThan(3 * Mathf.Max(ahead, 1)), $"{at}: a tail behind the head.");
                    Assert.That(spark.Frames[i].Width / spark.TexelsPerUnit, Is.LessThan(.3f), $"{at}: a spark, not a flare.");
                }
                Color32[] hot = Frame(spark, 0, out _, out _);
                if (spark == _set.GoldSpark)
                    Assert.That(Count(hot, c => IsVisible(c) && !(c.r >= c.g && c.g >= c.b)), Is.Zero, "Gold light, warm throughout.");
                else
                    Assert.That(Count(hot, c => IsVisible(c) && c.r > c.g + 40 && c.g > c.b), Is.GreaterThan(0), "Ember round its heart.");
            }
        }

        // The texels of one frame, row 0 at the bottom, from the sheet as painted.
        private Color32[] Frame(SkillEffectArtSet.Strip strip, int index, out int width, out int height)
        {
            if (_painted == null)
            {
                _painted = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(_painted.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(strip.Sheet))), Is.True);
            }
            SkillEffectArtSet.Cell cell = strip.Frames[index];
            width = cell.Width;
            height = cell.Height;
            Color[] pixels = _painted.GetPixels(cell.X, cell.Y, cell.Width, cell.Height);
            var texels = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                texels[i] = pixels[i];
            return texels;
        }

        // The texel furthest from the centre along a direction that the test accepts, in texels from the frame's edge.
        private static int OutermostAlong(Color32[] texels, int width, int height, int cx, int cy, int dx, int dy,
            System.Func<Color32, bool> accept)
        {
            int x = dx > 0 ? width - 1 : dx < 0 ? 0 : cx;
            int y = dy > 0 ? height - 1 : dy < 0 ? 0 : cy;
            while ((x != cx || y != cy) && !accept(texels[y * width + x]))
            {
                x -= dx;
                y -= dy;
            }
            return dx != 0 ? x : y;
        }

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;
        private static bool IsVisible(Color32 c) => c.a > 128;

        private static int Count(Color32[] texels, System.Func<Color32, bool> test)
        {
            int count = 0;
            foreach (Color32 texel in texels)
            {
                if (test(texel))
                    count++;
            }
            return count;
        }

        private static bool ClearBorder(Color32[] texels, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                if (texels[x].a > 0 || texels[(height - 1) * width + x].a > 0)
                    return false;
            }
            for (int y = 0; y < height; y++)
            {
                if (texels[y * width].a > 0 || texels[y * width + width - 1].a > 0)
                    return false;
            }
            return true;
        }
    }
}
