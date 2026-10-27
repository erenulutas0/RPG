using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The combat feedback's painted art (painted by Tools/ArtReview/build_combat_feedback_vfx.py): the set the Gameplay
    // scene borrows, how its one sheet imports and what it costs on the phone, the numerals' layout against the pixel
    // numerals', and what each strip paints, read back from the sheet as painted (the imported texture is compressed and
    // unreadable).
    public sealed class PaintedFeedbackEffectTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/Feedback/CombatFeedbackArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // Everything the set paints, as ASTC 4x4 (a byte a texel): the feedback effects may spend up to 2 MB.
        private const int BudgetBytes = 2 * 1024 * 1024;
        private const float PixelTexel = 1f / PixelSpriteFactory.PixelsPerUnit;

        private CombatFeedbackArtSet _set;
        private Texture2D _painted;

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<CombatFeedbackArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
            _painted = null;
        }

        private IEnumerable<SkillEffectArtSet.Strip> Strips()
        {
            yield return _set.DoubleSlash;
            yield return _set.CritStar;
            yield return _set.Smoke;
            yield return _set.Coin;
            yield return _set.Digits;
            yield return _set.CritDigits;
        }

        [Test]
        public void TheSceneBorrowsACompleteSetOnOneSheet()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid, Is.True);
            Assert.That(File.ReadAllText(ScenePath), Does.Contain(AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatEffectsView borrows it.");

            Texture2D sheet = _set.DoubleSlash.Sheet;
            foreach (SkillEffectArtSet.Strip strip in Strips())
            {
                Assert.That(strip.Sheet, Is.SameAs(sheet), "One sheet, so the strikes, puffs, coins and numbers batch.");
                Assert.That((strip.Sheet.width, strip.Sheet.height), Is.EqualTo((strip.SheetWidth, strip.SheetHeight)),
                    "It imports at the size it was painted, so its cells cut true.");
            }
            Assert.That(_set.DoubleSlash.Length, Is.GreaterThan(EffectArt.DoubleSlashFrames().Length), "Smoother than the pixel cuts.");
            Assert.That(_set.CritStar.Length, Is.GreaterThan(EffectArt.StarBurstFrames().Length));
            Assert.That(_set.Smoke.Length, Is.GreaterThan(EffectArt.SmokeFrames().Length));
            Assert.That(_set.Coin.Length, Is.GreaterThanOrEqualTo(6), "A spin, not the pixel coin's face and edge.");

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
            SkillEffectArtSet.Strip strip = _set.DoubleSlash;
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
            Assert.That((long)strip.SheetWidth * strip.SheetHeight, Is.LessThan(BudgetBytes), "Well under 2 MB on the phone.");
        }

        [Test]
        public void TheNumeralsKeepThePixelNumeralsSizeAndSpacing()
        {
            var fonts = new[] { (_set.Digits, _set.DigitStride, 1), (_set.CritDigits, _set.CritDigitStride, EffectArt.CritNumberScale) };
            foreach ((SkillEffectArtSet.Strip font, float stride, int scale) in fonts)
            {
                string name = scale == 1 ? "A numeral" : "A crit numeral";
                Assert.That(font.Length, Is.EqualTo(CombatFeedbackArtSet.NumeralCount));
                Assert.That(font.Pivot, Is.EqualTo(Vector2.zero), $"{name} pins at its bottom-left corner, set by stride.");
                Assert.That(stride, Is.EqualTo(EffectArt.DigitStride * scale * PixelTexel).Within(1e-5f),
                    $"{name} keeps the pixel numerals' spacing.");
                float pixelHeight = (EffectArt.DigitHeight + 2) * scale * PixelTexel;
                float pixelWidth = (EffectArt.DigitWidth + 2) * scale * PixelTexel;
                int strideTexels = Mathf.RoundToInt(stride * font.TexelsPerUnit);
                int widestInk = 0, firstFill = int.MaxValue;
                var seen = new List<Color32[]>();
                for (int digit = 0; digit < font.Length; digit++)
                {
                    Color32[] texels = Frame(font, digit, out int width, out int height);
                    Assert.That(ClearBorder(texels, width, height), Is.True, $"{name} {digit}: a clear texel round the cell.");
                    Bounds(texels, width, height, 128, out int left, out int bottom, out int right, out int top);
                    float inkHeight = (top - bottom + 1) / font.TexelsPerUnit;
                    Assert.That(inkHeight / pixelHeight, Is.InRange(.88f, 1.12f), $"{name} {digit}: as tall as the pixel one on screen.");
                    if (digit == 0)
                        Assert.That((right - left + 1) / font.TexelsPerUnit / pixelWidth, Is.InRange(.85f, 1.15f), $"{name}: as wide.");
                    // Centred across its cell, so a row of cells centres its number on the point it rises from.
                    Assert.That(left + right + 1 - width, Is.InRange(-2 * scale, 2 * scale), $"{name} {digit}: centred in its cell.");
                    widestInk = Mathf.Max(widestInk, Rightmost(texels, width, height, 40));
                    firstFill = Mathf.Min(firstFill, Leftmost(texels, width, height, IsFill));
                    foreach (Color32[] other in seen)
                        Assert.That(Differ(texels, other), Is.True, $"{name} {digit} repeats another numeral.");
                    seen.Add(texels);
                }
                // The neighbour's outline and shadow never reach a numeral's light fill, whichever draws last.
                Assert.That(widestInk - strideTexels, Is.LessThan(firstFill), $"{name}: neighbours keep clear of each other's fill.");
            }
            float normal = _set.Digits.Frames[0].Height / _set.Digits.TexelsPerUnit;
            float crit = _set.CritDigits.Frames[0].Height / _set.CritDigits.TexelsPerUnit;
            Assert.That(crit / normal, Is.InRange(1.8f, 2.2f), "A crit is twice the size, as the pixel crit is.");
        }

        [Test]
        public void TheNumeralsAreLightOnADarkOutlineHitsWhiteAndCritsGold()
        {
            for (int digit = 0; digit < CombatFeedbackArtSet.NumeralCount; digit++)
            {
                foreach (bool critical in new[] { false, true })
                {
                    SkillEffectArtSet.Strip font = critical ? _set.CritDigits : _set.Digits;
                    Color32[] texels = Frame(font, digit, out int width, out int height);
                    string at = $"{(critical ? "Crit numeral" : "Numeral")} {digit}";
                    int fill = 0, warm = 0, gold = 0, outline = 0, hot = 0, enclosed = 0;
                    float fillLight = 0f, outlineLight = 0f;
                    Bounds(texels, width, height, 128, out _, out int bottom, out _, out int top);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            Color32 c = texels[y * width + x];
                            if (c.a > 200 && Luminance(c) < 60f)
                            {
                                outline++;
                                outlineLight += Luminance(c);
                            }
                            if (!IsFill(c))
                                continue;
                            fill++;
                            fillLight += Luminance(c);
                            if (c.r >= c.b)
                                warm++;
                            if (c.r > 200 && c.r >= c.g && c.g > c.b && c.b < 190)
                                gold++;
                            if (c.r > 245 && c.g > 230 && c.b > 150 && y > (bottom + top) / 2)
                                hot++;
                            // Every texel of the fill is ringed by the outline: none of it touches the floor directly.
                            if (Alpha(texels, width, height, x - 1, y) > 100 && Alpha(texels, width, height, x + 1, y) > 100 &&
                                Alpha(texels, width, height, x, y - 1) > 100 && Alpha(texels, width, height, x, y + 1) > 100)
                                enclosed++;
                        }
                    }
                    Assert.That(fill, Is.GreaterThan(0), at);
                    Assert.That(outline, Is.GreaterThan(fill / 3), $"{at}: a dark outline, for the lava.");
                    Assert.That(fillLight / fill - outlineLight / outline, Is.GreaterThan(150f), $"{at}: light on dark.");
                    Assert.That(enclosed, Is.EqualTo(fill), $"{at}: the outline rings the whole fill.");
                    // Only where the fill blends into the soot outline at its very edge may a texel lean cool.
                    Assert.That(warm, Is.GreaterThan(fill * 19 / 20), $"{at}: white to pale warm, never cold.");
                    if (critical)
                    {
                        Assert.That(gold, Is.GreaterThan(fill * 3 / 5), $"{at}: warm gold.");
                        Assert.That(hot, Is.GreaterThan(0), $"{at}: a hot highlight across its upper strokes.");
                    }
                    else
                        Assert.That(gold, Is.LessThan(fill / 5), $"{at}: white, not gold.");
                }
            }
            // The counters stay open: the floor shows through the middle of a 0, so it never reads as a blot.
            SkillEffectArtSet.Strip numerals = _set.Digits;
            Color32[] zero = Frame(numerals, 0, out int w, out int h);
            Bounds(zero, w, h, 128, out int l, out int b, out int r, out int t);
            Assert.That(zero[(b + t) / 2 * w + (l + r) / 2].a, Is.LessThan(128), "A 0's counter is open.");
            Assert.That(Count(Frame(numerals, 1, out _, out _), IsFill), Is.LessThan(Count(Frame(numerals, 8, out _, out _), IsFill)),
                "A 1 is lighter than an 8.");
        }

        [Test]
        public void TheDoubleSlashCutsDownThenCrossesItAndThinsOut()
        {
            SkillEffectArtSet.Strip cuts = _set.DoubleSlash;
            Assert.That(cuts.Pivot.x, Is.EqualTo(.5f).Within(.05f), "Centred on the struck body, as the pixel cuts.");
            Assert.That(cuts.Pivot.y, Is.EqualTo(.5f).Within(.05f));
            int crossing = cuts.Length / 2;
            int lastVisible = 0, crossingVisible = 0;
            for (int i = 0; i < cuts.Length; i++)
            {
                Color32[] texels = Frame(cuts, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"Frame {i}: a clear texel round the cell.");
                // The first cut runs down from the upper left; the second rises from the lower left to cross it.
                bool down = Near(texels, width, height, cuts, -.2f, .17f) > 100;
                bool up = Near(texels, width, height, cuts, -.2f, -.16f) > 100;
                if (i == 0)
                {
                    Assert.That(down, Is.True, "The first cut leads.");
                    Assert.That(up, Is.False, "The second has not begun.");
                }
                if (i == crossing)
                {
                    Assert.That(down && up, Is.True, "Then the cuts cross.");
                    Assert.That(Near(texels, width, height, cuts, .2f, .17f) > 100 && Near(texels, width, height, cuts, .2f, -.16f) > 100,
                        Is.True, "Each the whole way across.");
                    Assert.That(Count(texels, IsNearWhite), Is.GreaterThan(0), "White at their cores and where they cross.");
                    Assert.That(Count(texels, IsSteel), Is.GreaterThan(Count(texels, IsVisible) / 3), "Steel blue, as the sword's slash.");
                    crossingVisible = Count(texels, IsVisible);
                }
                if (i == cuts.Length - 1)
                    lastVisible = Count(texels, IsVisible);
            }
            Assert.That(lastVisible, Is.LessThan(crossingVisible / 3), "Then thins out.");
        }

        [Test]
        public void TheCritStarFlashesWhiteHotInWarmGoldThenScatters()
        {
            SkillEffectArtSet.Strip star = _set.CritStar;
            Assert.That(star.Pivot.x, Is.EqualTo(.5f).Within(.02f));
            Assert.That(star.Pivot.y, Is.EqualTo(.5f).Within(.02f));
            for (int i = 0; i < star.Length; i++)
            {
                Color32[] texels = Frame(star, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"Frame {i}: a clear texel round the cell.");
                if (i > 1)
                    continue;
                Color32 centre = texels[Mathf.RoundToInt(star.Pivot.y * height) * width + Mathf.RoundToInt(star.Pivot.x * width)];
                Assert.That(IsNearWhite(centre) && centre.a > 200, Is.True, $"Frame {i}: a white-hot heart.");
                Assert.That(Count(texels, IsGold), Is.GreaterThan(0), $"Frame {i}: warm gold.");
                foreach (Vector2 ray in new[] { Vector2.up, Vector2.down, Vector2.left, Vector2.right })
                    Assert.That(Near(texels, width, height, star, ray.x * .2f, ray.y * .2f), Is.GreaterThan(128),
                        $"Frame {i}: a long ray toward {ray}.");
            }
            Color32[] first = Frame(star, 0, out _, out _);
            Color32[] last = Frame(star, star.Length - 1, out int w, out int h);
            Assert.That(MaxAlpha(last), Is.LessThanOrEqualTo(MaxAlpha(first)));
            Assert.That(Count(last, IsVisible), Is.LessThan(Count(first, IsVisible)), "It scatters and is gone.");
            // Its rays have drawn in by then, so what shows far out is the motes it flung.
            float flung = 0f;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (last[y * w + x].a > 60)
                        flung = Mathf.Max(flung, Vector2.Distance(new Vector2(x, y), new Vector2(star.Pivot.x * w, star.Pivot.y * h)));
                }
            }
            Assert.That(flung / star.TexelsPerUnit, Is.GreaterThan(.28f), "Into motes flung out past its rays.");
        }

        [Test]
        public void TheSmokeIsSootLitFromAboveWarmedByEmbersAndThinsAway()
        {
            SkillEffectArtSet.Strip smoke = _set.Smoke;
            // Painted in the pixel smoke's unit frame, so the view's death and arrival scales size it as they sized that.
            float pixelWidest = 0f;
            foreach (PixelCanvas frame in EffectArt.SmokeFrames())
                pixelWidest = Mathf.Max(pixelWidest, InkWidth(frame) * PixelTexel);
            float widest = 0f;
            int fullest = 0, fullestAt = 0;
            for (int i = 0; i < smoke.Length; i++)
            {
                Color32[] texels = Frame(smoke, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"Frame {i}: a clear texel round the cell.");
                Bounds(texels, width, height, 40, out int left, out _, out int right, out _);
                widest = Mathf.Max(widest, (right - left + 1) / smoke.TexelsPerUnit);
                int alpha = TotalAlpha(texels);
                if (alpha > fullest)
                {
                    fullest = alpha;
                    fullestAt = i;
                }
                if (i < smoke.Length - 1)
                    Assert.That(Count(texels, IsSoot), Is.GreaterThan(0), $"Frame {i}: soot.");
            }
            Assert.That(widest / pixelWidest, Is.InRange(.8f, 1.25f), "As large as the pixel puff it replaces.");
            Color32[] fresh = Frame(smoke, 0, out _, out _);
            Assert.That(Count(fresh, c => c.a > 128 && c.r > 220 && c.g > 120 && c.b < 120), Is.GreaterThan(0),
                "A hot ember heart as it bursts.");
            Assert.That(Count(Frame(smoke, 2, out _, out _), c => c.a > 128 && c.r > 220 && c.g > 150 && c.r > c.b + 60),
                Is.GreaterThan(0), "An ember fleck thrown up out of it.");
            // Lit from above: over the frame where it is thickest and its heart has cooled, the top of the cloud is lighter.
            Color32[] lit = Frame(smoke, smoke.Length / 2, out int lw, out int lh);
            Bounds(lit, lw, lh, 128, out _, out int bottom, out _, out int top);
            int third = (top - bottom) / 3;
            Assert.That(MeanLuminance(lit, lw, top - third, top), Is.GreaterThan(MeanLuminance(lit, lw, bottom, bottom + third)),
                "Lit from above.");
            Assert.That(fullestAt, Is.LessThan(smoke.Length - 2));
            Assert.That(TotalAlpha(Frame(smoke, smoke.Length - 1, out _, out _)), Is.LessThan(fullest / 2),
                "It thins away, so its end is never a pop.");
        }

        [Test]
        public void TheCoinSpinsAWholeTurnInGoldWithADarkEdgeAndAGlint()
        {
            SkillEffectArtSet.Strip coin = _set.Coin;
            // The coin itself, without the glint that flashes over it.
            System.Func<Color32, bool> body = c => IsVisible(c) && !IsNearWhite(c);
            var widths = new int[coin.Length];
            for (int i = 0; i < coin.Length; i++)
            {
                Color32[] texels = Frame(coin, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"Frame {i}: a clear texel round the cell.");
                Bounds(texels, width, height, body, out int left, out _, out int right, out _);
                widths[i] = right - left + 1;
                Assert.That(Count(texels, IsSoot), Is.GreaterThan(0), $"Frame {i}: a dark edge, for the lava.");
                Assert.That(Count(texels, IsGold), Is.GreaterThan(0), $"Frame {i}: gold.");
            }
            Color32[] face = Frame(coin, 0, out int w, out int h);
            Bounds(face, w, h, body, out int l, out int b, out int r, out int t);
            // Pinned at the middle of the coin, as the pixel coin is, whatever its glint adds to the cell.
            Assert.That((l + r + 1) * .5f, Is.EqualTo(coin.Pivot.x * w).Within(1.5f), "It flies from its centre.");
            Assert.That((b + t + 1) * .5f, Is.EqualTo(coin.Pivot.y * h).Within(1.5f));
            float size = (t - b + 1) / coin.TexelsPerUnit;
            Assert.That(size / (EffectArt.CoinSize * PixelTexel), Is.InRange(.85f, 1.25f), "The pixel coin's size.");
            Assert.That(widths[0], Is.EqualTo(t - b + 1).Within(3), "Face on, it is round.");
            Assert.That(widths[coin.Length / 4] * 3, Is.LessThan(widths[0]), "A quarter turn on, it shows its edge.");
            Assert.That(Count(face, IsGold), Is.GreaterThan(Count(face, IsVisible) / 2), "Its face is gold.");
            Assert.That(Count(face, IsNearWhite), Is.GreaterThan(0), "It glints as its face meets the light.");
            Assert.That(Differ(face, Frame(coin, coin.Length - 1, out _, out _)), Is.True, "The loop never repeats a frame.");
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

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;
        private static bool IsVisible(Color32 c) => c.a > 128;
        private static bool IsFill(Color32 c) => c.a > 200 && Luminance(c) > 150f;
        private static bool IsNearWhite(Color32 c) => c.a > 128 && c.r > 220 && c.g > 220 && c.b > 200;
        private static bool IsGold(Color32 c) => c.a > 128 && c.r > 200 && c.g > 130 && c.b < 130;
        private static bool IsSteel(Color32 c) => c.a > 128 && c.b > c.r + 40;
        private static bool IsSoot(Color32 c) => c.a > 80 && Luminance(c) < 70f;

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

        private static int TotalAlpha(Color32[] texels)
        {
            int total = 0;
            foreach (Color32 texel in texels)
                total += texel.a;
            return total;
        }

        private static int MaxAlpha(Color32[] texels)
        {
            int max = 0;
            foreach (Color32 texel in texels)
                max = Mathf.Max(max, texel.a);
            return max;
        }

        private static int Alpha(Color32[] texels, int width, int height, int x, int y) =>
            x < 0 || y < 0 || x >= width || y >= height ? 0 : texels[y * width + x].a;

        // The most opaque texel within a few texels of a point given in world units from the strip's pivot.
        private static int Near(Color32[] texels, int width, int height, SkillEffectArtSet.Strip strip, float x, float y,
            int radius = 3)
        {
            int cx = Mathf.RoundToInt(strip.Pivot.x * width + x * strip.TexelsPerUnit);
            int cy = Mathf.RoundToInt(strip.Pivot.y * height + y * strip.TexelsPerUnit);
            int max = 0;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                    max = Mathf.Max(max, Alpha(texels, width, height, cx + dx, cy + dy));
            }
            return max;
        }

        // The box of texels more opaque than the threshold, row 0 at the bottom.
        private static void Bounds(Color32[] texels, int width, int height, int threshold, out int left, out int bottom,
            out int right, out int top) =>
            Bounds(texels, width, height, c => c.a > threshold, out left, out bottom, out right, out top);

        private static void Bounds(Color32[] texels, int width, int height, System.Func<Color32, bool> test, out int left,
            out int bottom, out int right, out int top)
        {
            left = width;
            bottom = height;
            right = -1;
            top = -1;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!test(texels[y * width + x]))
                        continue;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                    bottom = Mathf.Min(bottom, y);
                    top = Mathf.Max(top, y);
                }
            }
        }

        private static int Rightmost(Color32[] texels, int width, int height, int threshold)
        {
            Bounds(texels, width, height, threshold, out _, out _, out int right, out _);
            return right;
        }

        private static int Leftmost(Color32[] texels, int width, int height, System.Func<Color32, bool> test)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (test(texels[y * width + x]))
                        return x;
                }
            }
            return width;
        }

        private static float MeanLuminance(Color32[] texels, int width, int fromRow, int toRow)
        {
            float sum = 0f;
            int count = 0;
            for (int y = fromRow; y <= toRow; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 c = texels[y * width + x];
                    if (c.a <= 128)
                        continue;
                    sum += Luminance(c);
                    count++;
                }
            }
            return count > 0 ? sum / count : 0f;
        }

        private static int InkWidth(PixelCanvas canvas)
        {
            int left = canvas.Width, right = -1;
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (canvas.Get(x, y).IsTransparent)
                        continue;
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                }
            }
            return right - left + 1;
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

        private static bool Differ(Color32[] a, Color32[] b)
        {
            if (a.Length != b.Length)
                return true;
            for (int i = 0; i < a.Length; i++)
            {
                if (!a[i].Equals(b[i]))
                    return true;
            }
            return false;
        }
    }
}
