using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // docs/63 section 13's Cinder Saw painted (Tools/ArtReview/BuildBlenderCinderSaw.py renders the blade,
    // build_cinder_saw_vfx.py packs it and paints the edge ring): the strike set the Gameplay scene borrows, how the
    // sheet both strips share imports and what it costs on the phone, and what each paints, read back from the sheet as
    // painted (the imported texture is compressed and unreadable). The blade is a dark iron disc half a unit across with
    // a light hub, holes through its plate and hot teeth; its glow and the ring are yellow-white, never the red-orange
    // docs/52 section 2.1 rule 3 keeps for what hurts on a timer, nor the orchid docs/51 gives the enemies.
    public sealed class PaintedCinderSawTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/FoundryStrikes/FoundryStrikeArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The sheet as ASTC 4x4 (a byte a texel): the Saw may spend a quarter of a MiB on the phone.
        private const int BudgetBytes = 262144;

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
        public void TheSceneBorrowsAStrikeSetThatCarriesTheSawsBladeAndRingOnOneSheet()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.HasCinderSaw, Is.True, "The Saw's blade and ring ride the strike set.");
            Assert.That(_set.IsValid && _set.HasStaffBlast, Is.True, "The slash, the spark and the Staff's hit are as they were.");
            Assert.That(File.ReadAllText(ScenePath), Does.Contain(AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatEffectsView borrows it.");

            SkillEffectArtSet.Strip blade = _set.SawBlade, ring = _set.SawRing;
            Assert.That(ring.Sheet, Is.SameAs(blade.Sheet), "One sheet, so the blade and its ring batch.");
            Assert.That(blade.Sheet, Is.Not.SameAs(_set.StaffRing.Sheet), "The Staff keeps its own sheet.");
            Assert.That((blade.Sheet.width, blade.Sheet.height), Is.EqualTo((blade.SheetWidth, blade.SheetHeight)),
                "It imports at the size it was painted, so its cells cut true.");
            Assert.That((ring.SheetWidth, ring.SheetHeight), Is.EqualTo((blade.SheetWidth, blade.SheetHeight)));
            Assert.That(blade.Length, Is.InRange(6, 8), "A short loop of the blade turning.");
            Assert.That(ring.Length, Is.GreaterThanOrEqualTo(2));
            foreach (SkillEffectArtSet.Cell cell in blade.Frames)
                Assert.That((cell.Width, cell.Height), Is.EqualTo((blade.Frames[0].Width, blade.Frames[0].Height)),
                    "Every frame of the loop cut alike, so it turns about one point.");

            // No cell reaches another, even by the texel bilinear filtering reads past its edge.
            var taken = new List<RectInt>();
            foreach (SkillEffectArtSet.Strip strip in new[] { blade, ring })
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

        // The closest painted effect flipbook's import, setting for setting: the Staff's painted sheet.
        [Test]
        public void TheSheetImportsAsTheStaffsPaintedSheetWithinBudget()
        {
            SkillEffectArtSet.Strip strip = _set.SawBlade;
            string path = AssetDatabase.GetAssetPath(strip.Sheet);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var staff = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(_set.StaffRing.Sheet));
            Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite).And.EqualTo(staff.textureType), path);
            Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear).And.EqualTo(staff.filterMode),
                $"{path}: smooth, as the painted actors.");
            Assert.That(importer.mipmapEnabled, Is.False.And.EqualTo(staff.mipmapEnabled), path);
            Assert.That(importer.alphaIsTransparency, Is.True.And.EqualTo(staff.alphaIsTransparency), path);
            Assert.That(importer.isReadable, Is.False.And.EqualTo(staff.isReadable), path);
            Assert.That(importer.sRGBTexture, Is.True.And.EqualTo(staff.sRGBTexture), path);
            Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None).And.EqualTo(staff.npotScale),
                $"{path}: cut as painted.");
            Assert.That(importer.wrapMode, Is.EqualTo(staff.wrapMode), path);
            Assert.That(importer.spriteImportMode, Is.EqualTo(staff.spriteImportMode), path);
            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            TextureImporterPlatformSettings staffAndroid = staff.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden, Is.True, path);
            Assert.That(android.format, Is.EqualTo(TextureImporterFormat.ASTC_4x4).And.EqualTo(staffAndroid.format), path);
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(strip.SheetWidth, strip.SheetHeight)),
                $"{path}: the phone gets it whole.");
            Assert.That(strip.SheetWidth % 4 == 0 && strip.SheetHeight % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
            Assert.That((long)strip.SheetWidth * strip.SheetHeight, Is.LessThanOrEqualTo(BudgetBytes),
                "A quarter of a MiB on the phone at most.");
        }

        [Test]
        public void TheBladeIsAHalfUnitIronDiscWithAHubHolesAndHotTeeth()
        {
            SkillEffectArtSet.Strip blade = _set.SawBlade;
            Assert.That(blade.Pivot.x, Is.EqualTo(.5f).Within(.02f), "It pins at its axle.");
            Assert.That(blade.Pivot.y, Is.EqualTo(.5f).Within(.02f));
            float unit = blade.TexelsPerUnit;
            for (int i = 0; i < blade.Length; i++)
            {
                Color32[] texels = Frame(blade, i, out int width, out int height);
                string at = $"Frame {i}";
                Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                float cx = blade.Pivot.x * width, cy = blade.Pivot.y * height;
                float reach = 0f;
                int holes = 0, hotTeeth = 0;
                var plate = new List<float>();
                var hub = new List<float>();
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Color32 c = texels[y * width + x];
                        float r = Mathf.Sqrt((x + .5f - cx) * (x + .5f - cx) + (y + .5f - cy) * (y + .5f - cy)) / unit;
                        if (c.a > 230)
                            reach = Mathf.Max(reach, r);
                        if (c.a > 230 && r > .075f && r < .17f)
                            plate.Add(Luminance(c));
                        if (c.a > 230 && r > .03f && r < .055f)
                            hub.Add(Luminance(c));
                        if (c.a < 40 && r > .1f && r < .145f)
                            holes++;
                        if (c.a > 200 && Luminance(c) > 200f && r > .19f)
                            hotTeeth++;
                    }
                }
                Assert.That(2f * reach, Is.InRange(.45f, .55f), $"{at}: half a unit across, tooth to tooth.");
                Color32 axle = texels[Mathf.FloorToInt(cy) * width + Mathf.FloorToInt(cx)];
                Assert.That(axle.a > 200 && Luminance(axle) < 60f, Is.True, $"{at}: a dark socket at the axle.");
                Assert.That(Median(hub), Is.GreaterThan(140f), $"{at}: round a light steel hub.");
                Assert.That(Median(plate), Is.LessThan(95f), $"{at}: a dark iron plate.");
                Assert.That(holes, Is.GreaterThan(15), $"{at}: the floor seen through the lightening holes.");
                Assert.That(hotTeeth, Is.GreaterThan(150), $"{at}: hot teeth all round.");
            }
            // It turns: every frame differs from the next, the last from the first too, so the loop never stalls.
            for (int i = 0; i < blade.Length; i++)
            {
                Color32[] a = Frame(blade, i, out _, out _), b = Frame(blade, (i + 1) % blade.Length, out _, out _);
                int moved = 0;
                for (int k = 0; k < a.Length; k++)
                    if (Mathf.Abs(a[k].a - b[k].a) > 64)
                        moved++;
                Assert.That(moved, Is.GreaterThan(60), $"Frame {i} turns into the next.");
            }
        }

        [Test]
        public void TheSawsGlowIsYellowWhiteNeverTheTelegraphsRedOrangeNorTheEnemiesOrchid()
        {
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.SawBlade, _set.SawRing })
            {
                string name = strip == _set.SawBlade ? "The blade" : "The ring";
                int glowing = 0;
                for (int i = 0; i < strip.Length; i++)
                {
                    Color32[] texels = Frame(strip, i, out _, out _);
                    foreach (Color32 c in texels)
                    {
                        if (c.a <= 32)
                            continue;
                        Color.RGBToHSV(c, out float h, out float s, out float v);
                        float hue = h * 360f;
                        bool vivid = s >= .35f && v >= .35f;
                        Assert.That(vivid && (hue < 30f || hue >= 345f), Is.False,
                            $"{name}, frame {i}: {c} is red-orange (docs/52 section 2.1 rule 3).");
                        Assert.That(vivid && hue >= 280f && hue <= 330f, Is.False, $"{name}, frame {i}: {c} is the enemies' orchid.");
                        // Every bright, coloured texel is the glow's straw to yellow, never toward ember.
                        if (c.a > 64 && v > .7f && s >= .2f)
                        {
                            glowing++;
                            Assert.That(hue, Is.InRange(38f, 62f), $"{name}, frame {i}: {c} is not the yellow-white glow.");
                        }
                    }
                }
                Assert.That(glowing, Is.GreaterThan(0), $"{name}: it glows.");
            }
        }

        [Test]
        public void TheRingIsAUnitFloorEllipseThatThinsAsItFades()
        {
            SkillEffectArtSet.Strip ring = _set.SawRing;
            Assert.That(ring.Pivot.x, Is.EqualTo(.5f).Within(.02f), "It pins at its centre, on the hero's feet.");
            Assert.That(ring.Pivot.y, Is.EqualTo(.5f).Within(.02f));
            int lastAlpha = int.MaxValue;
            for (int i = 0; i < ring.Length; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                string at = $"Frame {i}";
                Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                int cx = Mathf.RoundToInt(ring.Pivot.x * width), cy = Mathf.RoundToInt(ring.Pivot.y * height);
                Assert.That(texels[cy * width + cx].a, Is.Zero, $"{at}: the hero stands in a clear middle.");
                // The view scales it as the Staff's ring, whose contour lies a world unit out at scale 1: so must this
                // one's line, across the floor and, half as far, up it (ArenaFloor.DepthScale).
                float right = (Brightest(texels, width, cx, cy, 1, 0) - cx) / ring.TexelsPerUnit;
                float left = (cx - Brightest(texels, width, cx, cy, -1, 0)) / ring.TexelsPerUnit;
                float near = (cy - Brightest(texels, width, cx, cy, 0, -1)) / ring.TexelsPerUnit;
                Assert.That(right, Is.EqualTo(1f).Within(.02f), $"{at}: its line a unit out to the right.");
                Assert.That(left, Is.EqualTo(1f).Within(.02f), $"{at}: and to the left.");
                Assert.That(near, Is.EqualTo(ArenaFloor.DepthScale).Within(.02f), $"{at}: half as far toward the camera.");
                int alpha = TotalAlpha(texels);
                Assert.That(alpha, Is.LessThan(lastAlpha), $"{at}: fainter than the frame before.");
                lastAlpha = alpha;
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

        // Along a ray from the centre, the texel whose luminance (by its cover) is the highest: the ring's line.
        private static int Brightest(Color32[] texels, int width, int cx, int cy, int dx, int dy)
        {
            int best = cx * dx * dx + cy * dy * dy;
            float most = -1f;
            int height = texels.Length / width;
            for (int x = cx, y = cy; x >= 0 && y >= 0 && x < width && y < height; x += dx, y += dy)
            {
                Color32 c = texels[y * width + x];
                float light = Luminance(c) * c.a;
                if (light > most)
                {
                    most = light;
                    best = dx != 0 ? x : y;
                }
            }
            return best;
        }

        private static float Median(List<float> values)
        {
            Assert.That(values.Count, Is.GreaterThan(0));
            values.Sort();
            return values[values.Count / 2];
        }

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;

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
