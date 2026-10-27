using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The skills' painted effects (docs/45; painted by Tools/ArtReview/build_skill_vfx.py): the art set the Gameplay scene
    // borrows, how its sheets import and what they cost on the phone, and what each strip paints, read back from the
    // sheets as painted (the imported textures are compressed and unreadable).
    public sealed class PaintedSkillEffectTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/Skills/SkillEffectArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // Every painted skill sheet together as ASTC 4x4 (a byte a texel): well under the 3 MB the effects may spend.
        private const int BudgetBytes = 2 * 1024 * 1024;

        private SkillEffectArtSet _set;
        private readonly Dictionary<Texture2D, Texture2D> _painted = new Dictionary<Texture2D, Texture2D>();

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<SkillEffectArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            foreach (Texture2D texture in _painted.Values)
                Object.DestroyImmediate(texture);
            _painted.Clear();
        }

        private IEnumerable<SkillEffectArtSet.Strip> Strips()
        {
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
                yield return _set.Flames(stacks);
            yield return _set.EmberArc;
            yield return _set.VentColumn;
            yield return _set.VentEmbers;
            yield return _set.VentRing;
            yield return _set.Shockwave;
            yield return _set.ForgeSpark;
            yield return _set.ForgeFlash;
        }

        [Test]
        public void TheSceneBorrowsACompleteSetWithOneSheetPerFamily()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid, Is.True);
            Assert.That(File.ReadAllText(ScenePath), Does.Contain(AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatEffectsView borrows it.");

            Texture2D burn = _set.Flames(1).Sheet, vent = _set.VentColumn.Sheet, forge = _set.Shockwave.Sheet;
            Assert.That(new[] { burn, vent, forge }, Is.Unique, "A sheet for each effect family.");
            int frames = _set.Flames(1).Length;
            Assert.That(frames, Is.GreaterThanOrEqualTo(6), "A smooth loop, longer than the pixel flame's four frames.");
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                Assert.That(_set.Flames(stacks).Sheet, Is.SameAs(burn), "A crowd's flames, of any size, batch.");
                Assert.That(_set.Flames(stacks).Length, Is.EqualTo(frames), "Every stack count loops alike.");
            }
            Assert.That(_set.EmberArc.Sheet, Is.SameAs(burn));
            Assert.That(_set.VentEmbers.Sheet, Is.SameAs(vent));
            Assert.That(_set.VentRing.Sheet, Is.SameAs(vent));
            Assert.That(_set.ForgeSpark.Sheet, Is.SameAs(forge));
            Assert.That(_set.ForgeFlash.Sheet, Is.SameAs(forge));

            foreach (SkillEffectArtSet.Strip strip in Strips())
            {
                Assert.That((strip.Sheet.width, strip.Sheet.height), Is.EqualTo((strip.SheetWidth, strip.SheetHeight)),
                    $"{strip.Sheet.name} imports at the size it was painted, so its cells cut true.");
            }
            // No cell reaches another, even by the texel bilinear filtering reads past its edge.
            var cells = new Dictionary<Texture2D, List<RectInt>>();
            foreach (SkillEffectArtSet.Strip strip in Strips())
            {
                if (!cells.TryGetValue(strip.Sheet, out List<RectInt> taken))
                    cells[strip.Sheet] = taken = new List<RectInt>();
                foreach (SkillEffectArtSet.Cell cell in strip.Frames)
                {
                    var grown = new RectInt(cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2);
                    foreach (RectInt other in taken)
                        Assert.That(grown.Overlaps(other), Is.False, $"{strip.Sheet.name}: {grown} overlaps {other}.");
                    taken.Add(new RectInt(cell.X, cell.Y, cell.Width, cell.Height));
                }
            }
        }

        [Test]
        public void EachStripPinsWhereItsEffectStands()
        {
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                Vector2 pivot = _set.Flames(stacks).Pivot;
                Assert.That(pivot.x, Is.InRange(.4f, .6f), $"{stacks} stacks: under the middle of the body.");
                Assert.That(pivot.y, Is.LessThan(.1f), $"{stacks} stacks: at the enemy's feet, under its lowest tongue.");
            }
            Assert.That(_set.VentColumn.Pivot.y, Is.InRange(0f, .3f), "The column stands on the floor at its foot.");
            Assert.That(_set.VentColumn.Pivot.x, Is.EqualTo(.5f).Within(.05f));
            foreach (SkillEffectArtSet.Strip ring in new[] { _set.VentRing, _set.Shockwave })
                Assert.That(ring.Pivot.x, Is.EqualTo(.5f).Within(.02f), "A ring pins at its centre.");
            Assert.That(_set.Shockwave.Pivot.y, Is.EqualTo(.5f).Within(.02f));
            Assert.That(_set.EmberArc.Pivot.x, Is.GreaterThan(.7f), "The streaks pin at their heads, leading along +x.");
            Assert.That(_set.ForgeSpark.Pivot.x, Is.GreaterThan(.7f));

            // The rings are a world unit across their middle at scale 1, as the views scale them to the radius they mark.
            foreach (SkillEffectArtSet.Strip ring in new[] { _set.VentRing, _set.Shockwave })
            {
                float halfWidth = ring.Frames[0].Width * .5f / ring.TexelsPerUnit;
                Assert.That(halfWidth, Is.InRange(1f, 1.2f), $"{ring.Sheet.name}: a unit ring with room for its edge.");
            }
        }

        [Test]
        public void TheSheetsImportSmoothUnmippedAndAsAstcWithinBudget()
        {
            long bytes = 0;
            var seen = new HashSet<Texture2D>();
            foreach (SkillEffectArtSet.Strip strip in Strips())
            {
                if (!seen.Add(strip.Sheet))
                    continue;
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
                bytes += (long)strip.SheetWidth * strip.SheetHeight;
            }
            Assert.That(seen.Count, Is.EqualTo(3));
            Assert.That(bytes, Is.LessThan(BudgetBytes), "The painted skills stay well under 3 MB on the phone.");
        }

        [Test]
        public void TheFlameFramesItsBodyAndBurnsLargerAndHotterWithItsStacks()
        {
            int lastVisible = 0;
            float lastWarm = -1f;
            float lastTop = 0f;
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                SkillEffectArtSet.Strip strip = _set.Flames(stacks);
                int visible = 0, warm = 0, white = 0;
                for (int i = 0; i < strip.Length; i++)
                {
                    Color32[] texels = Frame(strip, i, out int width, out int height);
                    string at = $"{stacks} stacks, frame {i}";
                    Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                    Assert.That(Count(texels, IsSoot), Is.GreaterThan(0), $"{at}: a thin soot edge, for lava and fiery enemies.");
                    Assert.That(Count(texels, IsCrimson), Is.GreaterThan(0), $"{at}: crimson edges.");
                    Assert.That(Differ(texels, Frame(strip, (i + 1) % strip.Length, out _, out _)), Is.True, $"{at}: it flickers.");
                    visible += Count(texels, IsVisible);
                    warm += Count(texels, IsWarm);
                    white += Count(texels, IsWhiteHot);
                }
                float share = (float)warm / visible;
                float top = strip.Frames[0].Height * (1f - strip.Pivot.y) / strip.TexelsPerUnit;
                Assert.That(visible, Is.GreaterThan(lastVisible), $"{stacks} stacks: a larger flame.");
                Assert.That(share, Is.GreaterThan(lastWarm), $"{stacks} stacks: a larger share burns warm yellow.");
                Assert.That(top, Is.GreaterThan(lastTop), $"{stacks} stacks: it climbs higher.");
                if (stacks == 1)
                    Assert.That(white, Is.Zero, "One stack is an ember lick, never white-hot.");
                if (stacks == BurnRule.MaxStacks)
                {
                    Assert.That(white, Is.GreaterThan(0), "A full burn is white-hot at heart.");
                    Assert.That(top, Is.LessThan(1.8f), "It crowns a Grunt-sized body without burying the floor round it.");
                }
                lastVisible = visible;
                lastWarm = share;
                lastTop = top;
            }

            // The middle of the body stays clear enough to read: the painted Grunt's chest, 0.45 over its feet.
            SkillEffectArtSet.Strip full = _set.Flames(BurnRule.MaxStacks);
            Color32[] rest = Frame(full, 0, out int w, out int h);
            int cx = Mathf.RoundToInt(full.Pivot.x * w);
            int cy = Mathf.RoundToInt(full.Pivot.y * h + .45f * full.TexelsPerUnit);
            Assert.That(rest[cy * w + cx].a, Is.LessThan(128), "The flame frames the body rather than covering its chest.");
        }

        [Test]
        public void TheVentFlaresRisesAsAColumnBreaksUpAndLeavesEmbers()
        {
            SkillEffectArtSet.Strip column = _set.VentColumn;
            Assert.That(column.Length, Is.GreaterThanOrEqualTo(6));
            var tops = new int[column.Length];
            var visible = new int[column.Length];
            var hot = new int[column.Length];
            for (int i = 0; i < column.Length; i++)
            {
                Color32[] texels = Frame(column, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"Frame {i}: a clear texel round the cell.");
                tops[i] = TopRow(texels, width, height);
                visible[i] = Count(texels, IsVisible);
                hot[i] = Count(texels, IsWhiteHot);
                if (i < column.Length - 1)
                    Assert.That(Count(texels, IsSoot), Is.GreaterThan(0), $"Frame {i}: a soot edge, for the lava.");
            }
            Color32[] flare = Frame(column, 0, out int w, out int h);
            int footX = Mathf.RoundToInt(column.Pivot.x * w), footY = Mathf.RoundToInt(column.Pivot.y * h);
            Assert.That(flare[footY * w + footX].a, Is.GreaterThan(128), "It flares at the enemy's feet.");
            Assert.That(hot[0], Is.GreaterThan(0), "White-hot as it erupts.");
            int tallest = System.Array.IndexOf(visible, Max(visible));
            Assert.That(tops[tallest], Is.GreaterThan(tops[0] + 40), "The column rises far over the flare.");
            Assert.That(hot[column.Length - 1], Is.Zero, "Then only cooling embers.");
            Assert.That(visible[column.Length - 1], Is.LessThan(visible[tallest] / 4));

            SkillEffectArtSet.Strip embers = _set.VentEmbers;
            Assert.That(embers.Length, Is.GreaterThanOrEqualTo(3));
            int last = int.MaxValue;
            for (int i = 0; i < embers.Length; i++)
            {
                Color32[] texels = Frame(embers, i, out _, out _);
                int count = Count(texels, IsVisible);
                Assert.That(count, Is.LessThan(last), $"Frame {i}: the embers thin out.");
                last = count;
            }
            Assert.That(Count(Frame(embers, 0, out _, out _), IsWhiteHot), Is.GreaterThan(0), "Hot as they leave.");
            Color32[] cooled = Frame(embers, embers.Length - 1, out _, out _);
            Assert.That(Count(cooled, IsWarm), Is.Zero, "Crimson as they cool.");
            Assert.That(Count(cooled, IsCrimson), Is.GreaterThan(0));

            AssertHollowRing(_set.VentRing, "The cast ring");
            Assert.That(Count(Frame(_set.VentRing, 1, out _, out _), IsCrimson), Is.GreaterThan(0), "It burns in ember.");
        }

        [Test]
        public void TheEmberMemoryStreakLeadsWithAHotHeadAndACoolingTail()
        {
            SkillEffectArtSet.Strip arc = _set.EmberArc;
            for (int i = 0; i < arc.Length; i++)
            {
                Color32[] texels = Frame(arc, i, out int width, out int height);
                int headX = Mathf.RoundToInt(arc.Pivot.x * width), headY = Mathf.RoundToInt(arc.Pivot.y * height);
                Assert.That(IsWhiteHot(texels[headY * width + headX]), Is.True, $"Frame {i}: the head is white-hot.");
                Assert.That(Count(texels, IsCrimson), Is.GreaterThan(0), $"Frame {i}: the tail cools to crimson.");
                Assert.That(Count(texels, IsSoot), Is.GreaterThan(0), $"Frame {i}: a soot edge.");
                Assert.That(LeftmostVisible(texels, width, height), Is.LessThan(headX - .25f * arc.TexelsPerUnit),
                    $"Frame {i}: the tail trails behind the head.");
                Assert.That(RightmostVisible(texels, width, height), Is.LessThan(headX + .06f * arc.TexelsPerUnit),
                    $"Frame {i}: nothing leads the head.");
            }
            Assert.That(Differ(Frame(arc, 0, out _, out _), Frame(arc, 1, out _, out _)), Is.True, "The tail flickers.");
        }

        [Test]
        public void TheForgeBurstLeadsWhiteOverASootRimSparksSteelAndFlashes()
        {
            SkillEffectArtSet.Strip wave = _set.Shockwave;
            AssertHollowRing(wave, "The shockwave");
            Color32[] impact = Frame(wave, 0, out int w, out int h);
            int cy = Mathf.RoundToInt(wave.Pivot.y * h);
            int rim = Mathf.RoundToInt(wave.Pivot.x * w + wave.TexelsPerUnit);
            Assert.That(IsNearWhite(impact[cy * w + rim - 1]) || IsNearWhite(impact[cy * w + rim - 2]), Is.True,
                "A white leading edge at the rim.");
            bool soot = false;
            for (int x = rim; x < rim + 6 && x < w; x++)
                soot |= IsSoot(impact[cy * w + x]);
            Assert.That(soot, Is.True, "A soot rim outside it, for the orange lava.");
            Assert.That(Count(impact, IsSteel), Is.GreaterThan(Count(impact, IsVisible) / 3), "Steel blue.");
            int last = int.MaxValue;
            for (int i = 0; i < wave.Length; i++)
            {
                int count = Count(Frame(wave, i, out _, out _), IsVisible);
                Assert.That(count, Is.LessThan(last), $"Frame {i}: the band thins and breaks up.");
                last = count;
            }
            Assert.That(Count(Frame(wave, wave.Length - 1, out _, out _), IsNearWhite), Is.Zero, "Broken up, its white is gone.");

            SkillEffectArtSet.Strip spark = _set.ForgeSpark;
            last = int.MaxValue;
            for (int i = 0; i < spark.Length; i++)
            {
                Color32[] texels = Frame(spark, i, out int width, out int height);
                int headX = Mathf.RoundToInt(spark.Pivot.x * width), headY = Mathf.RoundToInt(spark.Pivot.y * height);
                Assert.That(IsNearWhite(texels[headY * width + headX]), Is.True, $"Frame {i}: a white-hot head.");
                Assert.That(Count(texels, IsSteel), Is.GreaterThan(0), $"Frame {i}: a steel-blue tail.");
                int count = Count(texels, IsVisible);
                Assert.That(count, Is.LessThan(last), $"Frame {i}: the streak shortens as it slows.");
                last = count;
            }

            SkillEffectArtSet.Strip flash = _set.ForgeFlash;
            Color32[] burst = Frame(flash, 0, out int fw, out int fh);
            Color32 centre = burst[Mathf.RoundToInt(flash.Pivot.y * fh) * fw + Mathf.RoundToInt(flash.Pivot.x * fw)];
            Assert.That(IsNearWhite(centre) && centre.a > 200, Is.True, "The flash is white at the hero.");
            Assert.That(MaxAlpha(Frame(flash, flash.Length - 1, out _, out _)), Is.LessThan(MaxAlpha(burst)), "And gone.");
        }

        private void AssertHollowRing(SkillEffectArtSet.Strip ring, string what)
        {
            for (int i = 0; i < ring.Length; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"{what}, frame {i}: a clear texel round the cell.");
                int cx = Mathf.RoundToInt(ring.Pivot.x * width), cy = Mathf.RoundToInt(ring.Pivot.y * height);
                Assert.That(texels[cy * width + cx].a, Is.LessThan(16), $"{what}, frame {i}: the hero stands in a clear centre.");
                // Whole until it breaks up, and half as deep as it is wide: a floor circle seen from above.
                if (i >= 2)
                    continue;
                int top = cy + Mathf.RoundToInt(.5f * ring.TexelsPerUnit);
                int side = cx + Mathf.RoundToInt(ring.TexelsPerUnit) - 2;
                Assert.That(MaxAlphaNear(texels, width, height, cx, top, 3), Is.GreaterThan(40), $"{what}, frame {i}: its far edge.");
                Assert.That(MaxAlphaNear(texels, width, height, side, cy, 3), Is.GreaterThan(40), $"{what}, frame {i}: its side.");
            }
        }

        // The texels of one frame, row 0 at the bottom, from the sheet as painted.
        private Color32[] Frame(SkillEffectArtSet.Strip strip, int index, out int width, out int height)
        {
            if (!_painted.TryGetValue(strip.Sheet, out Texture2D sheet))
            {
                sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                Assert.That(sheet.LoadImage(File.ReadAllBytes(AssetDatabase.GetAssetPath(strip.Sheet))), Is.True);
                _painted[strip.Sheet] = sheet;
            }
            SkillEffectArtSet.Cell cell = strip.Frames[index];
            width = cell.Width;
            height = cell.Height;
            Color[] pixels = sheet.GetPixels(cell.X, cell.Y, cell.Width, cell.Height);
            var texels = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
                texels[i] = pixels[i];
            return texels;
        }

        private static bool IsVisible(Color32 c) => c.a > 128;
        private static bool IsWarm(Color32 c) => c.a > 128 && c.r > 240 && c.g > 180;
        private static bool IsWhiteHot(Color32 c) => c.a > 200 && c.r > 245 && c.g > 230 && c.b > 170;
        private static bool IsNearWhite(Color32 c) => c.a > 128 && c.r > 220 && c.g > 225 && c.b > 230;
        private static bool IsCrimson(Color32 c) => c.a > 128 && c.r > c.g + 60 && c.b > c.g - 30 && c.r < 230;
        private static bool IsSteel(Color32 c) => c.a > 128 && c.b > c.r + 40;
        private static bool IsSoot(Color32 c) => c.a > 80 && .299f * c.r + .587f * c.g + .114f * c.b < 70f;

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

        private static int TopRow(Color32[] texels, int width, int height)
        {
            for (int y = height - 1; y >= 0; y--)
            {
                for (int x = 0; x < width; x++)
                {
                    if (texels[y * width + x].a > 40)
                        return y;
                }
            }
            return -1;
        }

        private static int LeftmostVisible(Color32[] texels, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (texels[y * width + x].a > 40)
                        return x;
                }
            }
            return width;
        }

        private static int RightmostVisible(Color32[] texels, int width, int height)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                for (int y = 0; y < height; y++)
                {
                    if (texels[y * width + x].a > 40)
                        return x;
                }
            }
            return -1;
        }

        private static int MaxAlpha(Color32[] texels)
        {
            int max = 0;
            foreach (Color32 texel in texels)
                max = Mathf.Max(max, texel.a);
            return max;
        }

        private static int MaxAlphaNear(Color32[] texels, int width, int height, int cx, int cy, int radius)
        {
            int max = 0;
            for (int y = Mathf.Max(0, cy - radius); y <= Mathf.Min(height - 1, cy + radius); y++)
            {
                for (int x = Mathf.Max(0, cx - radius); x <= Mathf.Min(width - 1, cx + radius); x++)
                    max = Mathf.Max(max, texels[y * width + x].a);
            }
            return max;
        }

        private static int Max(int[] values)
        {
            int max = int.MinValue;
            foreach (int value in values)
                max = Mathf.Max(max, value);
            return max;
        }
    }
}
