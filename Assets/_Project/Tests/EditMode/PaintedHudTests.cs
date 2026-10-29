using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The in-run HUD and the enemies' bars painted (Tools/ArtReview/build_hud_art.py): the two sets the Gameplay scene's
    // HudArtView and the enemy prefabs' EnemyLookView hold, how their sheets import and what they cost on the phone, the
    // 9-slice borders, the sizes every bar must keep, and what the sprites paint, read back from the sheets as painted
    // (the imported textures are compressed and unreadable); and the chunk's arithmetic (BarTrail).
    public sealed class PaintedHudTests
    {
        private const string HudSetPath = "Assets/_Project/Art/UI/Hud/HudArtSet.asset";
        private const string BarSetPath = "Assets/_Project/Art/UI/Hud/EnemyBarArtSet.asset";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string GruntPath = "Assets/_Project/Prefabs/Enemies/Grunt.prefab";
        private const string GoldIconPath = "Assets/_Project/Art/UI/UI_Icon_Gold_v1.png";
        // Every HUD and bar sprite together, as ASTC 4x4 (a byte a texel): about 0.8 MB on the phone at most.
        private const long BudgetBytes = 838860;

        private HudArtSet _hud;
        private EnemyBarArtSet _bars;
        private readonly Dictionary<Texture2D, Texture2D> _painted = new Dictionary<Texture2D, Texture2D>();

        [OneTimeSetUp]
        public void Load()
        {
            _hud = AssetDatabase.LoadAssetAtPath<HudArtSet>(HudSetPath);
            _bars = AssetDatabase.LoadAssetAtPath<EnemyBarArtSet>(BarSetPath);
        }

        [OneTimeTearDown]
        public void Unload()
        {
            foreach (Texture2D texture in _painted.Values)
                Object.DestroyImmediate(texture);
            _painted.Clear();
        }

        private IEnumerable<Sprite> HudSprites()
        {
            yield return _hud.Panel;
            yield return _hud.Button;
            yield return _hud.Pip;
            yield return _hud.WeaponSlot;
            yield return _hud.TomeSlot;
            yield return _hud.SlotAccent;
            yield return _hud.AbilityRing;
            yield return _hud.AbilityCooldown;
            yield return _hud.BossNamePlate;
            foreach (HudArtSet.Bar bar in new[] { _hud.Hero, _hud.Experience, _hud.Boss })
            {
                yield return bar.Trough;
                yield return bar.Fill;
                if (bar.Trail != null)
                    yield return bar.Trail;
                yield return bar.Glint;
                yield return bar.Frame;
            }
        }

        private IEnumerable<Sprite> BarSprites()
        {
            foreach (bool compact in new[] { false, true })
            {
                yield return _bars.BackFor(compact);
                yield return _bars.FillFor(compact);
                yield return _bars.TrailFor(compact);
            }
        }

        [Test]
        public void TheSceneAndTheEnemiesHoldCompleteSetsEachOnOneSheet()
        {
            Assert.That(_hud, Is.Not.Null, HudSetPath);
            Assert.That(_bars, Is.Not.Null, BarSetPath);
            Assert.That(_hud.IsValid, Is.True, "Every HUD sprite is there, on one sheet, with its borders.");
            Assert.That(_bars.IsValid, Is.True, "Every bar sprite is there, on one sheet.");
            Assert.That(File.ReadAllText(ScenePath), Does.Contain("_art: {fileID: 11400000, guid: " + AssetDatabase.AssetPathToGUID(HudSetPath)),
                "The Gameplay scene's HudArtView holds the HUD's set.");
            Assert.That(File.ReadAllText(GruntPath), Does.Contain("_barArt: {fileID: 11400000, guid: " + AssetDatabase.AssetPathToGUID(BarSetPath)),
                "The Grunt prefab, which every enemy varies, holds the bars' set.");
            Assert.That(_hud.Experience.Trail, Is.Null, "Experience only grows: no chunk.");
            Assert.That(_hud.Hero.Trail, Is.Not.Null);
            Assert.That(_hud.Boss.Trail, Is.Not.Null);
            foreach (Sprite sprite in HudSprites())
            {
                Assert.That(sprite.texture, Is.SameAs(_hud.Panel.texture), $"{sprite.name}: one sheet, so the HUD batches.");
                Assert.That(sprite.name, Does.Not.StartWith("UI_Icon_"), "Never mistaken for an ability's or a card's icon.");
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(100f), $"{sprite.name}: painted at its size on the 1080-wide canvas.");
            }
            foreach (Sprite sprite in BarSprites())
            {
                Assert.That(sprite.texture, Is.SameAs(_bars.BackFor(false).texture), $"{sprite.name}: one texture for every bar of a crowd.");
                Assert.That(sprite.pixelsPerUnit, Is.EqualTo(128f), $"{sprite.name}: at the painted actors' density.");
            }
            Assert.That(_bars.BackFor(false).texture, Is.Not.SameAs(_hud.Panel.texture));
        }

        [Test]
        public void TheSheetsImportSmoothUnmippedAndAsAstcWithinBudget()
        {
            long total = 0;
            foreach (Texture2D sheet in new[] { _hud.Panel.texture, _bars.BackFor(false).texture })
            {
                string path = AssetDatabase.GetAssetPath(sheet);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.textureType, Is.EqualTo(TextureImporterType.Sprite), path);
                Assert.That(importer.spriteImportMode, Is.EqualTo(SpriteImportMode.Multiple), $"{path}: cut into its sprites.");
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Bilinear), $"{path}: smooth, as the painted UI and actors.");
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
                total += (long)sheet.width * sheet.height;
            }
            Assert.That(total, Is.LessThanOrEqualTo(BudgetBytes), "About 0.8 MB on the phone at most, for the HUD and every bar.");
        }

        [Test]
        public void PanelsPlatesSocketsTroughsAndFramesAreNineSliced()
        {
            foreach (Sprite sprite in new[] { _hud.Panel, _hud.Button, _hud.WeaponSlot, _hud.TomeSlot })
            {
                Vector4 b = sprite.border;
                Assert.That(b.x > 0 && b.y > 0 && b.z > 0 && b.w > 0, Is.True, $"{sprite.name}: cut on all four sides.");
                Assert.That(b.x + b.z, Is.LessThan(sprite.rect.width), $"{sprite.name}: a middle left to stretch across.");
                Assert.That(b.y + b.w, Is.LessThan(sprite.rect.height), $"{sprite.name}: and up.");
            }
            foreach (Sprite sprite in new[] { _hud.Pip, _hud.SlotAccent, _hud.Hero.Trough, _hud.Experience.Trough, _hud.Boss.Trough })
            {
                Vector4 b = sprite.border;
                Assert.That(b.x > 0 && b.z > 0, Is.True, $"{sprite.name}: its ends kept as it stretches across.");
                Assert.That(b.x + b.z, Is.LessThan(sprite.rect.width), sprite.name);
            }
            foreach (HudArtSet.Bar bar in new[] { _hud.Hero, _hud.Experience, _hud.Boss })
            {
                Vector4 b = bar.Frame.border, outset = bar.FrameOutset;
                string name = bar.Frame.name;
                Assert.That(b.x >= outset.x && b.y >= outset.y && b.z >= outset.z && b.w >= outset.w, Is.True,
                    $"{name}: what reaches past the window ({outset}) lies in its borders ({b}), never stretched.");
                Assert.That(b.x + b.z, Is.LessThan(bar.Frame.rect.width), name);
                Assert.That(b.y + b.w, Is.LessThan(bar.Frame.rect.height), name);
                Assert.That(bar.Frame.rect.height - outset.y - outset.w, Is.EqualTo(bar.Fill.rect.height),
                    $"{name}: as tall as it is drawn round its fill, so its end caps never stretch.");
                Assert.That(bar.GlintWidth, Is.GreaterThan(0f).And.LessThan(bar.Fill.rect.width / 10f));
            }
            // The panel's frame keeps within the 14 texels between the top panel's right edge and the pause button.
            Texture2D painted = Painted(_hud.Panel.texture);
            Rect panel = _hud.Panel.rect;
            Color32 face = painted.GetPixel((int)panel.center.x, (int)panel.center.y);
            Color32 inside = painted.GetPixel((int)panel.xMax - 15, (int)panel.center.y);
            Assert.That(Luminance(inside), Is.LessThan(25f), "Fifteen texels in from its right edge no metal catches the light.");
            Assert.That(inside.b, Is.GreaterThanOrEqualTo(inside.r), "Only the face, in the frame's shade.");
        }

        [Test]
        public void ThePanelsFaceIsTheNavyThePaintedIconsCarry()
        {
            // The coin and the relic's icon are painted on the old panel's navy, not cut out: on a face of another colour
            // they would show as squares.
            var icon = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                Assert.That(icon.LoadImage(File.ReadAllBytes(GoldIconPath)), Is.True);
                Color32 navy = icon.GetPixel(4, 4);
                Texture2D painted = Painted(_hud.Panel.texture);
                Rect panel = _hud.Panel.rect;
                Color32 face = painted.GetPixel((int)panel.center.x, (int)panel.center.y);
                Assert.That(Max(face, navy), Is.LessThanOrEqualTo(3), $"The face {face} is the icons' navy {navy}.");
                // And flat: the relic badge's foot stands 19 texels in from the panel's bottom edge.
                Color32 low = painted.GetPixel((int)panel.center.x, (int)panel.yMin + 19);
                Assert.That(Max(low, navy), Is.LessThanOrEqualTo(3), "Still the navy where the relic badge stands lowest.");
            }
            finally
            {
                Object.DestroyImmediate(icon);
            }
        }

        [Test]
        public void TheBossBarsFrameKeepsToItsRectsFootAndRisesAboveIt()
        {
            HudArtSet.Bar boss = _hud.Boss;
            Assert.That(boss.FrameOutset.y, Is.EqualTo(-boss.Window.y),
                "The frame's foot is the bar's rect's foot, the row the camera's free band starts at.");
            Assert.That(boss.Window.y, Is.LessThan(0f), "So its fill is lifted inside the rect, over the frame's foot.");
            Assert.That(boss.Window.w, Is.GreaterThan(0f), "And grows above it.");
            Assert.That(boss.FrameOutset.w, Is.GreaterThan(_hud.Hero.FrameOutset.w * 2f), "Its end caps rise well above the bar.");
            Assert.That(boss.FrameOutset.x, Is.GreaterThan(_hud.Hero.FrameOutset.x * 2f), "Heavier ends than the hero's.");
            // Its window is the size the fill is painted at: 936 across, 16 high (the rect's 14, lifted 4 and grown 6).
            Assert.That(boss.Fill.rect.size, Is.EqualTo(new Vector2(936f, 14f + boss.Window.y + boss.Window.w)));
            Assert.That(_hud.Hero.Fill.rect.size, Is.EqualTo(new Vector2(698f, 22f)), "The hero bar's 698 by 22.");
            Assert.That(_hud.Experience.Fill.rect.size, Is.EqualTo(new Vector2(698f, 8f)), "The experience bar's 698 by 8.");
            Assert.That(_hud.Hero.Window, Is.EqualTo(Vector4.zero));
            Assert.That(_hud.Experience.Window, Is.EqualTo(Vector4.zero));
        }

        [Test]
        public void TheFillsReadAtAGlanceEachInItsOwnColourLitOnTop()
        {
            // Health is red, experience the gems' blue, the boss's molten: each lighter at its top than at its foot.
            AssertFill(_hud.Hero.Fill, "health", c => c.r > c.g + 60 && c.r > c.b + 60);
            AssertFill(_hud.Experience.Fill, "experience", c => c.b > c.r + 60 && c.b >= c.g);
            AssertFill(_hud.Boss.Fill, "the boss's", c => c.r > c.b + 60 && c.r >= c.g);
            AssertFill(_bars.FillFor(false), "an enemy's", c => c.r > c.g + 60 && c.r > c.b + 60);
            AssertFill(_bars.FillFor(true), "a Mite's", c => c.r > c.g + 60 && c.r > c.b + 60);
            // The chunk is paler than the fill it trails, so it reads as what was just lost.
            foreach ((Sprite trail, Sprite fill) in new[] { (_hud.Hero.Trail, _hud.Hero.Fill), (_hud.Boss.Trail, _hud.Boss.Fill),
                         (_bars.TrailFor(false), _bars.FillFor(false)) })
                Assert.That(MeanLuminance(trail), Is.GreaterThan(MeanLuminance(fill) + 60f), $"{trail.name} is paler than {fill.name}.");
        }

        [Test]
        public void EnemyBarsTakeThePixelBarsRoomAndTheMitesStaysEvenOnceSqueezed()
        {
            foreach (bool compact in new[] { false, true })
            {
                Sprite back = _bars.BackFor(compact), fill = _bars.FillFor(compact), trail = _bars.TrailFor(compact);
                string name = compact ? "The compact bar" : "The bar";
                Assert.That(back.bounds.size.x, Is.EqualTo(EnemyArt.HealthBarWidth / 32f).Within(1e-5f), $"{name}: the pixel bar's 26 texels across.");
                Assert.That(back.bounds.size.y, Is.EqualTo(EnemyArt.HealthBarHeight / 32f).Within(1e-5f), $"{name}: and 5 up.");
                Assert.That(fill.pivot, Is.EqualTo(new Vector2(0f, fill.rect.height / 2f)), $"{name}: the fill drains from its left edge.");
                Assert.That(trail.pivot, Is.EqualTo(fill.pivot));
                Assert.That(back.pivot, Is.EqualTo(back.rect.size / 2f), $"{name}: centred over the head.");
                Assert.That((back.rect.width - fill.rect.width) % 2f, Is.Zero, $"{name}: the fill centred in the channel.");
            }
            // Squeezed to 0.65 across and 0.8 up, the compact frame's sides are as thick as the full one's, within a texel.
            float side = (_bars.BackFor(false).rect.width - _bars.FillFor(false).rect.width) / 2f;
            float squeezed = (_bars.BackFor(true).rect.width - _bars.FillFor(true).rect.width) / 2f * .65f;
            Assert.That(squeezed, Is.EqualTo(side).Within(1f));
            Assert.That(_bars.FillFor(true).rect.width, Is.LessThan(_bars.FillFor(false).rect.width));
            // The backing's frame is dark where it meets the floor, so the bar parts from pale stone and orange lava alike.
            Texture2D painted = Painted(_bars.BackFor(false).texture);
            Rect rect = _bars.BackFor(false).rect;
            Color32 edge = painted.GetPixel((int)rect.center.x, (int)rect.yMin + 1);
            Assert.That(edge.a, Is.GreaterThan(200));
            Assert.That(Luminance(edge), Is.LessThan(40f), "A dark outline at its foot.");
            Color32 channel = painted.GetPixel((int)rect.center.x, (int)rect.center.y);
            Assert.That(Luminance(channel), Is.LessThan(30f), "A dark channel for the fill to drain into.");
        }

        [Test]
        public void TheChunkHoldsThenDrainsAndAHealLiftsIt()
        {
            var trail = new BarTrail();
            trail.Reset(1f);
            Assert.That(trail.IsMoving, Is.False);
            trail.Follow(.6f);
            Assert.That(trail.Shown, Is.EqualTo(1f), "A hit leaves the chunk where the bar stood.");
            Assert.That(trail.IsMoving, Is.True);
            trail.Tick(.2f);
            Assert.That(trail.Shown, Is.EqualTo(1f), "Held a moment.");
            trail.Follow(.5f);
            Assert.That(trail.Target, Is.EqualTo(.5f), "A second hit joins the chunk.");
            trail.Tick(.1f);
            Assert.That(trail.Shown, Is.EqualTo(1f), "Without restarting the hold.");
            float before = trail.Shown;
            trail.Tick(1f / 60f);
            Assert.That(trail.Shown, Is.LessThan(before), "Then it drains.");
            float seconds = 0f;
            while (trail.IsMoving && seconds < 2f)
            {
                trail.Tick(1f / 60f);
                seconds += 1f / 60f;
            }
            Assert.That(trail.Shown, Is.EqualTo(.5f), "Down to the fill.");
            Assert.That(seconds, Is.LessThan(.8f), "Within a second of the hold.");
            trail.Follow(.8f);
            Assert.That(trail.Shown, Is.EqualTo(.8f), "A heal lifts it with the fill.");
            Assert.That(trail.IsMoving, Is.False);
            trail.Tick(0f);
            trail.Follow(.7f);
            trail.Tick(0f);
            Assert.That(trail.Shown, Is.EqualTo(.8f), "A pause holds it.");
        }

        private void AssertFill(Sprite fill, string name, System.Func<Color32, bool> hue)
        {
            Texture2D painted = Painted(fill.texture);
            Rect r = fill.rect;
            int x = (int)(r.x + r.width * .37f);
            Color32 top = painted.GetPixel(x, (int)r.yMax - 2);
            Color32 middle = painted.GetPixel(x, (int)r.center.y);
            Color32 foot = painted.GetPixel(x, (int)r.yMin);
            Assert.That(top.a == 255 && middle.a == 255 && foot.a == 255, Is.True, $"{name} fill: solid across its window.");
            Assert.That(hue(middle), Is.True, $"{name} fill: its own colour, not {middle}.");
            Assert.That(Luminance(top), Is.GreaterThan(Luminance(foot) + 40f), $"{name} fill: lit on top, shaded at its foot.");
        }

        private float MeanLuminance(Sprite sprite)
        {
            Texture2D painted = Painted(sprite.texture);
            Rect r = sprite.rect;
            float sum = 0f;
            int count = 0;
            for (int y = (int)r.yMin; y < (int)r.yMax; y++)
            {
                Color32 c = painted.GetPixel((int)r.center.x, y);
                sum += Luminance(c);
                count++;
            }
            return sum / count;
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

        private static float Luminance(Color32 c) => .299f * c.r + .587f * c.g + .114f * c.b;
    }
}
