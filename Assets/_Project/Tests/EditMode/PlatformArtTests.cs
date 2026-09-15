using System.Linq;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PlatformArtTests
    {
        // The scene's platform: ArenaView's defaults.
        private static readonly ArenaGeometry SceneGeometry = new ArenaGeometry(-9f, 9f, 9f);

        [Test]
        public void LayoutFitsTheSceneGeometryOnASmallCanvas()
        {
            var layout = new PlatformLayout(SceneGeometry);
            // Half-width 9 units = 288 texels plus the outline margin either side, on an odd width with a centre column.
            Assert.That(layout.Width, Is.EqualTo(581));
            Assert.That(layout.HalfDepth, Is.EqualTo(144), "Half the top's depth: 4.5 world units.");
            Assert.That(layout.NearY, Is.EqualTo(16 + 70 + 42), "Under-hang, keel and faces below the near corner.");
            Assert.That(layout.FarY, Is.EqualTo(layout.NearY + 2 * layout.HalfDepth));
            Assert.That(layout.Height, Is.EqualTo(layout.FarY + 1 + PlatformLayout.Margin));
            Assert.That(layout.Height, Is.LessThanOrEqualTo(1024));

            PixelCanvas platform = PlatformArt.DrawPlatform(SceneGeometry);
            Assert.That((platform.Width, platform.Height), Is.EqualTo((layout.Width, layout.Height)));
            PixelCanvas lights = PlatformArt.DrawLights(SceneGeometry, 0);
            Assert.That((lights.Width, lights.Height), Is.EqualTo((layout.Width, layout.Height)), "The overlay shares the transform.");
        }

        [Test]
        public void WorldPointsMapToTheRightTexels()
        {
            var layout = new PlatformLayout(SceneGeometry);
            Assert.That(layout.TexelX(0f), Is.EqualTo(layout.CentreX), "The hero's column is the centre column.");
            Assert.That(layout.TexelY(SceneGeometry.WorldBottom), Is.EqualTo(layout.NearY), "The near corner's row.");
            Assert.That(layout.TexelY(SceneGeometry.WorldTop), Is.EqualTo(layout.FarY), "The far corner's row.");
            Assert.That(layout.TexelX(SceneGeometry.HalfWidth), Is.EqualTo(layout.CentreX + layout.HalfWidth));
            layout.TopTexel(1f, 1f, out int x, out int y);
            Assert.That((x, y), Is.EqualTo((layout.CentreX, layout.FarY)));
            layout.TopTexel(1f, 0f, out x, out y);
            Assert.That((x, y), Is.EqualTo((layout.CentreX + layout.HalfWidth, layout.MiddleY)));
            Assert.That(layout.IsOnTop(layout.TexelX(0f), layout.TexelY(0f)), Is.True, "The hero stands on the top.");
            Assert.That(layout.NearEdgeY(layout.CentreX), Is.EqualTo(layout.NearY));
            Assert.That(layout.NearEdgeY(layout.CentreX + layout.HalfWidth), Is.EqualTo(layout.MiddleY));
            Assert.That(layout.KeelEdgeY(layout.CentreX), Is.EqualTo(layout.TipY));
        }

        [Test]
        public void RhombusCornersAreBrassAndTheCentreIsTheEmblem()
        {
            var layout = new PlatformLayout(SceneGeometry);
            PixelCanvas canvas = PlatformArt.DrawPlatform(layout);
            Assert.That(canvas.Get(layout.CentreX, layout.FarY), Is.EqualTo(PixelPalette.Brass), "Far corner.");
            Assert.That(canvas.Get(layout.CentreX - layout.HalfWidth, layout.MiddleY), Is.EqualTo(PixelPalette.Brass), "Left corner.");
            Assert.That(canvas.Get(layout.CentreX + layout.HalfWidth, layout.MiddleY), Is.EqualTo(PixelPalette.Brass), "Right corner.");
            Assert.That(canvas.Get(layout.CentreX, layout.NearY), Is.EqualTo(PixelPalette.BrassLight), "The near corner catches the light.");
            Assert.That(canvas.Get(layout.CentreX, layout.MiddleY), Is.EqualTo(PixelPalette.BrassLight), "The emblem's heart.");
            Assert.That(canvas.Get(layout.CentreX, layout.MiddleY + 40), Is.EqualTo(PlatformArt.FloorInlay), "The far diagonal is unlit.");
            Assert.That(canvas.Get(layout.CentreX + 40, layout.MiddleY), Is.EqualTo(PlatformArt.FloorInlay), "The side diagonal is unlit.");
            // A one-texel dark outline hugs the silhouette.
            Assert.That(canvas.Get(layout.CentreX, layout.FarY + 1), Is.EqualTo(PixelPalette.Outline));
            Assert.That(canvas.Get(layout.CentreX - layout.HalfWidth - 1, layout.MiddleY), Is.EqualTo(PixelPalette.Outline));
        }

        [Test]
        public void OutsideTheTopAndBelowTheKeelIsTransparent()
        {
            var layout = new PlatformLayout(SceneGeometry);
            PixelCanvas canvas = PlatformArt.DrawPlatform(layout);
            Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.Transparent));
            Assert.That(canvas.Get(0, canvas.Height - 1), Is.EqualTo(Rgba.Transparent));
            Assert.That(canvas.Get(canvas.Width - 1, canvas.Height - 1), Is.EqualTo(Rgba.Transparent));
            Assert.That(canvas.Get(canvas.Width - 1, 0), Is.EqualTo(Rgba.Transparent));
            Assert.That(canvas.Get(1, layout.FarY - 10), Is.EqualTo(Rgba.Transparent), "Beside the far half of the top.");
            Assert.That(canvas.Get(canvas.Width - 2, layout.NearY), Is.EqualTo(Rgba.Transparent), "Beside the near half.");
            Assert.That(canvas.Get(layout.CentreX, 0), Is.EqualTo(Rgba.Transparent), "Under the crystal.");
            Assert.That(canvas.Get(layout.CentreX - layout.HalfWidth + 1, layout.NearY - 20), Is.EqualTo(Rgba.Transparent), "Below the keel's edge.");
            PixelCanvas lights = PlatformArt.DrawLights(layout, 1);
            Assert.That(lights.Get(layout.CentreX, layout.FarY - 10), Is.EqualTo(Rgba.Transparent), "The overlay leaves the tiles alone.");
        }

        [Test]
        public void TilesUseTwoStoneTonesPerHalfWithGroutAndFacesCarryLava()
        {
            PixelCanvas canvas = PlatformArt.DrawPlatform(SceneGeometry);
            Assert.That(Count(canvas, PlatformArt.FloorStone), Is.GreaterThan(3000), "Shared stone tone.");
            Assert.That(Count(canvas, PlatformArt.FloorLight), Is.GreaterThan(1500), "Near half's lighter tone.");
            Assert.That(Count(canvas, PlatformArt.FloorDark), Is.GreaterThan(1500), "Far half's darker tone.");
            Assert.That(Count(canvas, PlatformArt.FloorGrout), Is.GreaterThan(300), "Grout lines between the tiles.");
            Assert.That(PlatformArt.FloorLight.R - PlatformArt.FloorGrout.R, Is.LessThan(16), "Internal seams recede beneath actors.");
            Assert.That(Count(canvas, PixelPalette.Brass) + Count(canvas, PixelPalette.BrassDark), Is.GreaterThan(800), "Rim, diagonals and emblem.");
            Assert.That(Count(canvas, PixelPalette.Face) + Count(canvas, PixelPalette.FaceLight) + Count(canvas, PixelPalette.FaceDark), Is.GreaterThan(3000), "Stone faces and keel.");
            Assert.That(Count(canvas, PixelPalette.Lava), Is.GreaterThan(150), "Lava seams and the crystal's rind.");
            Assert.That(Count(canvas, PixelPalette.ChainLight), Is.GreaterThan(60), "Chains under the keel.");
            Assert.That(Count(canvas, PixelPalette.EmberHot), Is.EqualTo(0), "The brightest ember belongs to the overlay.");
        }

        [Test]
        public void OverlayFramesGlowAndDiffer()
        {
            PixelCanvas first = PlatformArt.DrawLights(SceneGeometry, 0);
            PixelCanvas second = PlatformArt.DrawLights(SceneGeometry, 1);
            Assert.That(Count(first, PixelPalette.EmberLight) + Count(first, PixelPalette.EmberHot), Is.GreaterThan(80), "Seam glow, flames and the crystal's heart.");
            Assert.That(Count(second, PixelPalette.EmberHot), Is.GreaterThan(Count(first, PixelPalette.EmberHot)), "The second frame burns hotter.");
            Assert.That(first.Pixels.SequenceEqual(second.Pixels), Is.False, "The frames differ.");
            Assert.That(first.CountVisible(), Is.LessThan(first.Width * first.Height / 3), "The overlay is mostly transparent.");
        }

        [Test]
        public void DrawingIsDeterministic()
        {
            PixelCanvas a = PlatformArt.DrawPlatform(SceneGeometry);
            PixelCanvas b = PlatformArt.DrawPlatform(SceneGeometry);
            Assert.That(a.Pixels.SequenceEqual(b.Pixels), Is.True);
            PixelCanvas la = PlatformArt.DrawLights(SceneGeometry, 1);
            PixelCanvas lb = PlatformArt.DrawLights(SceneGeometry, 1);
            Assert.That(la.Pixels.SequenceEqual(lb.Pixels), Is.True);
        }

        private static int Count(PixelCanvas canvas, Rgba color) => canvas.Pixels.Count(pixel => pixel == color);
    }
}
