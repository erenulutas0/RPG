using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The Crucible Vent's painted eruption ring (painted by Tools/ArtReview/build_skill_vfx.py): the optional strip the
    // Gameplay scene's skill set carries, how its sheet imports and what it costs on the phone, and what it paints, read
    // back from the sheet as painted (the imported texture is compressed and unreadable).
    public sealed class PaintedVentSplashTests
    {
        private const string SetPath = "Assets/_Project/Art/Effects/Skills/SkillEffectArtSet.asset";
        private const string StrikeSetPath = "Assets/_Project/Art/Effects/FoundryStrikes/FoundryStrikeArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The sheet as ASTC 4x4 (a byte a texel): the eruption ring may spend about 0.4 MB on the phone, and every painted
        // skill sheet together stays under the 2 MB PaintedSkillEffectTests holds the three before it to.
        private const int BudgetBytes = 419430;
        private const int SkillsBudgetBytes = 2 * 1024 * 1024;

        private SkillEffectArtSet _set;
        private Texture2D _painted;

        [OneTimeSetUp]
        public void Load() => _set = AssetDatabase.LoadAssetAtPath<SkillEffectArtSet>(SetPath);

        [OneTimeTearDown]
        public void Unload()
        {
            if (_painted != null)
                Object.DestroyImmediate(_painted);
            _painted = null;
        }

        [Test]
        public void TheScenesSkillSetCarriesTheEruptionRingOnASheetOfItsOwn()
        {
            Assert.That(_set, Is.Not.Null, SetPath);
            Assert.That(_set.IsValid, Is.True, "The rest of the set is as it was.");
            Assert.That(_set.HasVentSplash, Is.True, "And the Vent's eruption ring rides beside it.");
            Assert.That(File.ReadAllText(ScenePath), Does.Contain(AssetDatabase.AssetPathToGUID(SetPath)),
                "The Gameplay scene's CombatEffectsView borrows it.");

            SkillEffectArtSet.Strip splash = _set.VentSplash;
            Assert.That(splash.Length, Is.GreaterThanOrEqualTo(5), "Painted frames, where the procedural ring had one.");
            var strikes = AssetDatabase.LoadAssetAtPath<StrikeArtSet>(StrikeSetPath);
            Assert.That(new[] { splash.Sheet, _set.Flames(1).Sheet, _set.VentColumn.Sheet, _set.Shockwave.Sheet,
                strikes.StaffRing.Sheet }, Is.Unique, "A sheet of its own, and never the Staff's.");
            Assert.That(AssetDatabase.GetAssetPath(splash.Sheet), Does.StartWith("Assets/_Project/Art/Effects/Skills/"),
                "Beside the other skill sheets.");
            Assert.That((splash.Sheet.width, splash.Sheet.height), Is.EqualTo((splash.SheetWidth, splash.SheetHeight)),
                "It imports at the size it was painted, so its cells cut true.");
            // Every frame is cut alike, and no cell reaches another, even by the texel bilinear filtering reads past its edge.
            var taken = new List<RectInt>();
            foreach (SkillEffectArtSet.Cell cell in splash.Frames)
            {
                Assert.That((cell.Width, cell.Height), Is.EqualTo((splash.Frames[0].Width, splash.Frames[0].Height)));
                var grown = new RectInt(cell.X - 1, cell.Y - 1, cell.Width + 2, cell.Height + 2);
                foreach (RectInt other in taken)
                    Assert.That(grown.Overlaps(other), Is.False, $"{grown} overlaps {other}.");
                taken.Add(new RectInt(cell.X, cell.Y, cell.Width, cell.Height));
            }
        }

        [Test]
        public void ItsSheetImportsAsTheOtherSkillSheetsDoWithinBudget()
        {
            SkillEffectArtSet.Strip splash = _set.VentSplash;
            string path = AssetDatabase.GetAssetPath(splash.Sheet);
            string ventPath = AssetDatabase.GetAssetPath(_set.VentColumn.Sheet);
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
            Assert.That(android.maxTextureSize, Is.GreaterThanOrEqualTo(Mathf.Max(splash.SheetWidth, splash.SheetHeight)),
                $"{path}: the phone gets it whole.");
            // And the very importer of the skill sheets beside it, line for line but for its own GUIDs and density.
            Assert.That(ImportSettings(path), Is.EqualTo(ImportSettings(ventPath)), $"{path} imports as {ventPath} does.");

            Assert.That(splash.SheetWidth % 4 == 0 && splash.SheetHeight % 4 == 0, Is.True, $"{path}: whole ASTC blocks.");
            Assert.That((long)splash.SheetWidth * splash.SheetHeight, Is.LessThanOrEqualTo(BudgetBytes),
                "About 0.4 MB on the phone at most.");
            long skills = 0;
            var seen = new HashSet<Texture2D>();
            foreach (SkillEffectArtSet.Strip strip in new[] { _set.Flames(1), _set.VentColumn, _set.Shockwave, splash })
            {
                if (seen.Add(strip.Sheet))
                    skills += (long)strip.SheetWidth * strip.SheetHeight;
            }
            Assert.That(seen.Count, Is.EqualTo(4));
            Assert.That(skills, Is.LessThan(SkillsBudgetBytes), "The painted skills together still stay under 2 MB.");
        }

        [Test]
        public void TheRingIsAUnitFloorEllipseHalfAsDeepAsItIsWide()
        {
            SkillEffectArtSet.Strip ring = _set.VentSplash;
            Assert.That(ring.Pivot.x, Is.EqualTo(.5f).Within(.02f), "It pins at its centre, on the enemy's feet.");
            float halfWidth = ring.Frames[0].Width * .5f / ring.TexelsPerUnit;
            Assert.That(halfWidth, Is.InRange(1f, 1.2f), "A unit ring with room for its rim and the embers it throws.");
            // The view grows it as it grows the procedural ring, whose contour lies a world unit out at scale 1, so the
            // splash it shows is the splash that strikes: while the band is whole its outer edge lies a unit out across the
            // floor and half as far toward the camera (ArenaFloor.DepthScale), at every angle round its near side. Its
            // far side is where its flames stand, up off the floor.
            for (int i = 0; i < 2; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                float cx = ring.Pivot.x * width, cy = ring.Pivot.y * height;
                var radii = new List<float>();
                for (int degree = -170; degree <= -10; degree += 2)
                {
                    float r = OutermostAlong(texels, width, height, cx, cy, ring.TexelsPerUnit, degree);
                    Assert.That(r, Is.InRange(.97f, 1.05f), $"Frame {i}: its edge a unit out along the floor at {degree} degrees.");
                    radii.Add(r);
                }
                radii.Sort();
                Assert.That(radii[radii.Count / 2], Is.EqualTo(1f).Within(.02f), $"Frame {i}: a unit out all round its near side.");
                // Straight toward the camera, in from the frame's near edge to the first texel past half alpha.
                int x = Mathf.RoundToInt(cx), y = 0;
                while (y < cy && texels[y * width + x].a <= 128)
                    y++;
                Assert.That((cy - y) / ring.TexelsPerUnit, Is.EqualTo(ArenaFloor.DepthScale).Within(.02f),
                    $"Frame {i}: half as far toward the camera.");
            }
        }

        [Test]
        public void ItBurnsInEmberNeverTheStaffsVioletOrTheVanguardsSteel()
        {
            // Violet is the Staff's hit, steel blue the Vanguard's Sword, Daggers and Forge Burst; ember is the Burn's and the
            // Vent's.
            SkillEffectArtSet.Strip ring = _set.VentSplash;
            for (int i = 0; i < ring.Length; i++)
            {
                Color32[] texels = Frame(ring, i, out _, out _);
                int visible = Count(texels, IsVisible);
                Assert.That(Count(texels, IsViolet), Is.Zero, $"Frame {i}: none of the Staff's violet.");
                Assert.That(Count(texels, IsSteel), Is.Zero, $"Frame {i}: none of the Vanguard's steel blue.");
                Assert.That(Count(texels, IsEmber), Is.GreaterThan(visible * 3 / 4), $"Frame {i}: ember.");
                if (i < 4)
                    Assert.That(Count(texels, IsSoot), Is.GreaterThan(0), $"Frame {i}: a soot rim, for the Foundry's lava.");
            }
        }

        [Test]
        public void ItIgnitesWhiteHotLeapsIntoFlameThenTearsIntoEmbersAndCools()
        {
            SkillEffectArtSet.Strip ring = _set.VentSplash;
            int last = ring.Length - 1;
            var visible = new int[ring.Length];
            var flames = new int[ring.Length];
            var lit = new float[ring.Length];
            for (int i = 0; i < ring.Length; i++)
            {
                Color32[] texels = Frame(ring, i, out int width, out int height);
                string at = $"Frame {i}";
                Assert.That(ClearBorder(texels, width, height), Is.True, $"{at}: a clear texel round the cell.");
                float cx = ring.Pivot.x * width, cy = ring.Pivot.y * height;
                Assert.That(texels[Mathf.RoundToInt(cy) * width + Mathf.RoundToInt(cx)].a, Is.LessThan(16),
                    $"{at}: the enemy and its column stand in a clear middle.");
                visible[i] = Count(texels, IsVisible);
                lit[i] = Lit(texels, width, height, cx, cy, ring.TexelsPerUnit);
                // What stands over the far side of the floor ellipse: its flames and the embers they throw.
                int over = Mathf.CeilToInt(cy + (ArenaFloor.DepthScale + .06f) * ring.TexelsPerUnit);
                for (int y = over; y < height; y++)
                    for (int x = 0; x < width; x++)
                        flames[i] += IsVisible(texels[y * width + x]) ? 1 : 0;
                if (i == 0)
                    Assert.That(Count(texels, IsWhiteHot), Is.GreaterThan(0), $"{at}: a white-hot line of ignition.");
                if (i < 2)
                {
                    Assert.That(lit[i], Is.GreaterThan(.97f), $"{at}: whole all the way round as it bursts out.");
                    Assert.That(Count(texels, IsHot), Is.GreaterThan(0), $"{at}: burning yellow-hot.");
                }
                if (i == last)
                {
                    Assert.That(Count(texels, IsHot), Is.Zero, $"{at}: cooled.");
                    Assert.That(Count(texels, IsCrimson), Is.GreaterThan(0), $"{at}: to the last crimson pieces.");
                }
            }
            Assert.That(flames[0], Is.LessThan(50), "It ignites low on the floor.");
            int tallest = Mathf.Max(flames[1], flames[2]);
            Assert.That(tallest, Is.GreaterThan(250), "Then flames leap up off its far side.");
            Assert.That(flames[last], Is.LessThan(tallest / 8), "And die down.");
            for (int i = 2; i <= last; i++)
            {
                Assert.That(lit[i], Is.LessThan(lit[i - 1]), $"Frame {i}: the band tears apart as it burns out.");
                Assert.That(visible[i], Is.LessThan(visible[i - 1]), $"Frame {i}: and thins.");
            }
            Assert.That(lit[last], Is.LessThan(.3f), "Down to its last burning pieces.");
            Assert.That(visible[last], Is.LessThan(visible[1] / 8));
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

        // A sheet's .meta without the lines that are its own: its GUID, its sprite's and its density.
        private static string[] ImportSettings(string path)
        {
            var kept = new List<string>();
            foreach (string line in File.ReadAllLines(path + ".meta"))
            {
                string setting = line.Trim();
                if (!setting.StartsWith("guid:") && !setting.StartsWith("spriteID:") && !setting.StartsWith("spritePixelsToUnits:"))
                    kept.Add(line);
            }
            return kept.ToArray();
        }

        // The floor radius, in units of the ring's radius across, of the outermost texel past half alpha along a ray out of
        // the centre at the given floor angle (ArenaFloor.DepthScale: a floor unit up the screen is half a unit).
        private static float OutermostAlong(Color32[] texels, int width, int height, float cx, float cy, float unit,
            int degree)
        {
            float a = degree * Mathf.Deg2Rad;
            for (float r = 1.25f; r > 0f; r -= .002f)
            {
                int x = Mathf.RoundToInt(cx + Mathf.Cos(a) * r * unit);
                int y = Mathf.RoundToInt(cy + Mathf.Sin(a) * r * unit * ArenaFloor.DepthScale);
                if (x >= 0 && y >= 0 && x < width && y < height && texels[y * width + x].a > 128)
                    return r;
            }
            return 0f;
        }

        // The share of the way round the floor ellipse its band burns: a degree is lit where ember stands across it, from
        // 0.86 to all of the radius.
        private static float Lit(Color32[] texels, int width, int height, float cx, float cy, float unit)
        {
            int count = 0;
            for (int degree = 0; degree < 360; degree++)
            {
                float a = degree * Mathf.Deg2Rad;
                bool on = false;
                for (float r = .86f; r <= 1.0001f && !on; r += .004f)
                {
                    int x = Mathf.RoundToInt(cx + Mathf.Cos(a) * r * unit);
                    int y = Mathf.RoundToInt(cy + Mathf.Sin(a) * r * unit * ArenaFloor.DepthScale);
                    on = x >= 0 && y >= 0 && x < width && y < height && IsEmber(texels[y * width + x]);
                }
                if (on)
                    count++;
            }
            return count / 360f;
        }

        private static bool IsVisible(Color32 c) => c.a > 128;
        // The Staff's crystal: blue well over green, and red over green too, so never the Vanguard's steel.
        private static bool IsViolet(Color32 c) => c.a > 128 && c.b > c.g + 40 && c.r > c.g;
        private static bool IsSteel(Color32 c) => c.a > 128 && c.b > c.r + 40 && c.g > c.r;
        // Fire, from crimson through ember to yellow: red well over both green and blue.
        private static bool IsEmber(Color32 c) => c.a > 128 && c.r > c.g + 40 && c.r > c.b + 40;
        private static bool IsHot(Color32 c) => c.a > 128 && c.r > 240 && c.g > 180;
        private static bool IsWhiteHot(Color32 c) => c.a > 200 && c.r > 245 && c.g > 230 && c.b > 170;
        private static bool IsCrimson(Color32 c) => c.a > 128 && c.r > c.g + 60 && c.b > c.g - 30 && c.r < 230;
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
    }
}
