using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Staff's painted hit (painted by Tools/ArtReview/build_staff_impact_vfx.py): the strike set the Gameplay scene
    // borrows, how the sheet its ring and burst share imports and what it costs on the phone, and what each paints, read
    // back from the sheet as painted (the imported texture is compressed and unreadable).
    public sealed class PaintedStaffImpactTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/FoundryStrikes/FoundryStrikeArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The sheet as ASTC 4x4 (a byte a texel): the Staff's hit may spend about 0.6 MB on the phone.
        private const int BudgetBytes = 629146;

        private StrikeArtSet _set;
        private Texture2D _painted;

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<StrikeArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
            _painted = null;
        }

        [Test]
        public void TheSceneBorrowsAStrikeSetThatCarriesTheStaffsHitOnOneSheet()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid, Is.True, "The painted slash and spark are as they were.");
            Assert.That(_set.HasStaffBlast, Is.True, "And the Staff's ring and burst ride beside them.");
            Assert.That(File.ReadAllText(ScenePath), Does.Contain(AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatEffectsView borrows it.");

            SkillEffectArtSet.Strip ring = _set.StaffRing, burst = _set.StaffBurst;
            Assert.That(burst.Sheet, Is.SameAs(ring.Sheet), "One sheet, so every Staff hit on screen batches.");
            Assert.That(ring.Sheet, Is.Not.SameAs(_set.Slash[0].texture), "The slash keeps its own textures.");
            Assert.That((ring.Sheet.width, ring.Sheet.height), Is.EqualTo((ring.SheetWidth, ring.SheetHeight)),
                "It imports at the size it was painted, so its cells cut true.");
            Assert.That((burst.SheetWidth, burst.SheetHeight), Is.EqualTo((ring.SheetWidth, ring.SheetHeight)));
            Assert.That(ring.Length, Is.GreaterThanOrEqualTo(4), "Painted frames, where the procedural ring had one.");
            Assert.That(burst.Length, Is.GreaterThanOrEqualTo(3));

            // No cell reaches another, even by the texel bilinear filtering reads past its edge.
            var taken = new List<RectInt>();
            foreach (SkillEffectArtSet.Strip strip in new[] { ring, burst })
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
            SkillEffectArtSet.Strip strip = _set.StaffRing;
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
                "About 0.6 MB on the phone at most.");
        }

        [Test]
        public void TheRingIsAUnitFloorEllipseHalfAsDeepAsItIsWide()
        {
            SkillEffectArtSet.Strip ring = _set.StaffRing;
            Assert.That(ring.Pivot.x, Is.EqualTo(.5f).Within(.02f), "It pins at its centre, on the target's feet.");
            // The view scales it as it scaled the procedural ring, whose contour lies a world unit out at scale 1: so must
            // this one's leading edge, across the floor and, half as far, up it (ArenaFloor.DepthScale).
            for (int i = 0; i < 2; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                Assert.That(ClearBorder(texels, width, height), Is.True, $"Frame {i}: a clear texel round the cell.");
                int cx = Mathf.RoundToInt(ring.Pivot.x * width), cy = Mathf.RoundToInt(ring.Pivot.y * height);
                float right = (OutermostAlong(texels, width, height, cx, cy, 1, 0) - cx) / ring.TexelsPerUnit;
                float left = (cx - OutermostAlong(texels, width, height, cx, cy, -1, 0)) / ring.TexelsPerUnit;
                float near = (cy - OutermostAlong(texels, width, height, cx, cy, 0, -1)) / ring.TexelsPerUnit;
                Assert.That(right, Is.EqualTo(1f).Within(.03f), $"Frame {i}: its edge a unit out to the right.");
                Assert.That(left, Is.EqualTo(1f).Within(.03f), $"Frame {i}: and to the left.");
                Assert.That(near, Is.EqualTo(ArenaFloor.DepthScale).Within(.025f), $"Frame {i}: half as far toward the camera.");
            }
            float halfWidth = ring.Frames[0].Width * .5f / ring.TexelsPerUnit;
            Assert.That(halfWidth, Is.InRange(1f, 1.15f), "A unit ring with room for its edge.");
        }

        [Test]
        public void TheRingFlashesWholeWithALightEdgeOnADarkRimThenBreaksIntoFourArcs()
        {
            SkillEffectArtSet.Strip ring = _set.StaffRing;
            int last = ring.Length - 1;
            int firstVisible = 0;
            for (int i = 0; i < ring.Length; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                string at = $"Frame {i}";
                Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                int cx = Mathf.RoundToInt(ring.Pivot.x * width), cy = Mathf.RoundToInt(ring.Pivot.y * height);
                Color32 centre = texels[cy * width + cx];
                if (i == 0)
                    Assert.That(centre.a > 128 && Luminance(centre) > 200f, Is.True, $"{at}: it flashes on the floor at the target's feet.");
                else
                    Assert.That(centre.a, Is.LessThan(16), $"{at}: then the target stands in a clear middle.");
                int arcs = Arcs(texels, width, height, ring, out float lit);
                if (i < 2)
                {
                    Assert.That(lit, Is.EqualTo(1f), $"{at}: whole all the way round while it flashes out.");
                    // The light edge is what reads on the dark stone, the dark rim outside it what reads on the lava.
                    bool light = false, dark = false;
                    for (int x = cx + Mathf.RoundToInt(.95f * ring.TexelsPerUnit); x < width; x++)
                    {
                        Color32 c = texels[cy * width + x];
                        light |= c.a > 160 && Luminance(c) > 200f;
                        dark |= c.a > 80 && Luminance(c) < 70f && x > cx + .99f * ring.TexelsPerUnit;
                    }
                    Assert.That(light, Is.True, $"{at}: a light leading edge.");
                    Assert.That(dark, Is.True, $"{at}: on a dark rim.");
                    if (i == 1)
                        firstVisible = Count(texels, IsVisible);
                }
                else
                {
                    // The procedural ring's four arcs, painted: they tear apart and swirl as the ring fades.
                    Assert.That(arcs, Is.EqualTo(4), $"{at}: four arcs.");
                    Assert.That(lit, Is.LessThan(i == last ? .75f : .97f), $"{at}: with gaps between them.");
                }
            }
            Assert.That(Count(Frame(ring, last, out _, out _), IsVisible), Is.LessThan(firstVisible / 2), "It thins as it fades.");
        }

        [Test]
        public void TheStaffsHitIsVioletNeverEmberOrSteel()
        {
            // Ember is Burn's, the Vent's and the hit sparks'; steel blue the Vanguard's Sword, Daggers and Forge Burst.
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.StaffRing, _set.StaffBurst })
            {
                string name = strip == _set.StaffRing ? "The ring" : "The burst";
                for (int i = 0; i < strip.Length; i++)
                {
                    Color32[] texels = Frame(strip, i, out _, out _);
                    int visible = Count(texels, IsVisible);
                    Assert.That(Count(texels, IsEmber), Is.Zero, $"{name}, frame {i}: no ember.");
                    Assert.That(Count(texels, IsSteel), Is.Zero, $"{name}, frame {i}: no steel blue.");
                    Assert.That(Count(texels, IsViolet), Is.GreaterThan(visible / 2), $"{name}, frame {i}: the Staff's violet.");
                }
            }
        }

        [Test]
        public void TheBurstFlashesWhiteHotOnTheBodyThenSpiralsOutAndIsGone()
        {
            SkillEffectArtSet.Strip burst = _set.StaffBurst;
            // Pinned at its heart, which the view centres on the struck body as the other strikes; its cell is cropped to its
            // wisps, so the pin need not be the cell's middle.
            Assert.That(burst.Pivot.x, Is.InRange(.3f, .7f));
            Assert.That(burst.Pivot.y, Is.InRange(.3f, .7f));
            Assert.That(burst.Frames[0].Width / burst.TexelsPerUnit, Is.LessThan(1f), "Smaller than the body it lands on.");
            float lastReach = 0f;
            int firstAlpha = 0;
            for (int i = 0; i < burst.Length; i++)
            {
                Color32[] texels = Frame(burst, i, out int width, out int height);
                string at = $"Frame {i}";
                Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                int cx = Mathf.RoundToInt(burst.Pivot.x * width), cy = Mathf.RoundToInt(burst.Pivot.y * height);
                Color32 centre = texels[cy * width + cx];
                if (i == 0)
                {
                    Assert.That(centre.a > 200 && Luminance(centre) > 230f, Is.True, $"{at}: a white-hot heart.");
                    firstAlpha = TotalAlpha(texels);
                }
                float reach = Reach(texels, width, height, cx, cy) / burst.TexelsPerUnit;
                Assert.That(reach, Is.GreaterThan(lastReach), $"{at}: it flies out.");
                lastReach = reach;
                if (i == burst.Length - 1)
                {
                    Assert.That(centre.a, Is.LessThan(64), $"{at}: its heart is spent.");
                    Assert.That(TotalAlpha(texels), Is.LessThan(firstAlpha / 2), $"{at}: and it is all but gone.");
                }
            }
            Assert.That(lastReach, Is.GreaterThan(.25f), "Its wisps fly well clear of the heart.");
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

        // How many separate arcs the band makes round the ring, and the share of the way round it covers: a degree is lit
        // where the band's violet stands across it, from 0.9 to 1.02 of the radius along the floor ellipse. The motes that
        // lift off the band are hot white, not violet, and a glint narrower than four degrees is never an arc.
        private static int Arcs(Color32[] texels, int width, int height, SkillEffectArtSet.Strip ring, out float lit)
        {
            const int Narrowest = 4;
            float cx = ring.Pivot.x * width, cy = ring.Pivot.y * height, unit = ring.TexelsPerUnit;
            var on = new bool[360];
            int count = 0;
            for (int degree = 0; degree < 360; degree++)
            {
                float a = degree * Mathf.Deg2Rad;
                for (float r = .9f; r <= 1.02f && !on[degree]; r += .004f)
                {
                    int x = Mathf.RoundToInt(cx + Mathf.Cos(a) * r * unit);
                    int y = Mathf.RoundToInt(cy + Mathf.Sin(a) * r * unit * ArenaFloor.DepthScale);
                    on[degree] = x >= 0 && y >= 0 && x < width && y < height && IsViolet(texels[y * width + x]);
                }
                if (on[degree])
                    count++;
            }
            lit = count / 360f;
            if (count == 360)
                return 1;
            int start = System.Array.IndexOf(on, false);
            int runs = 0, run = 0;
            for (int k = 1; k <= 360; k++)
            {
                if (on[(start + k) % 360])
                {
                    run++;
                    continue;
                }
                if (run >= Narrowest)
                    runs++;
                run = 0;
            }
            return runs;
        }

        // The texel furthest from the centre along a direction whose alpha is past half, in texels from the frame's edge.
        private static int OutermostAlong(Color32[] texels, int width, int height, int cx, int cy, int dx, int dy)
        {
            int x = dx > 0 ? width - 1 : dx < 0 ? 0 : cx;
            int y = dy > 0 ? height - 1 : dy < 0 ? 0 : cy;
            while ((x != cx || y != cy) && texels[y * width + x].a <= 128)
            {
                x -= dx;
                y -= dy;
            }
            return dx != 0 ? x : y;
        }

        private static float Reach(Color32[] texels, int width, int height, int cx, int cy)
        {
            float reach = 0f;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (texels[y * width + x].a > 100)
                        reach = Mathf.Max(reach, Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)));
                }
            }
            return reach;
        }

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;
        private static bool IsVisible(Color32 c) => c.a > 128;
        // The Staff's crystal: blue well over green, and red over green too, so never the Vanguard's steel.
        private static bool IsViolet(Color32 c) => c.a > 128 && c.b > c.g + 40 && c.r > c.g;
        private static bool IsEmber(Color32 c) => c.a > 128 && c.r > c.g + 40 && c.g > c.b;
        private static bool IsSteel(Color32 c) => c.a > 128 && c.b > c.r + 40 && c.g > c.r;

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
