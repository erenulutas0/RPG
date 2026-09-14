using System;
using System.Collections.Generic;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PixelCanvasTests
    {
        [Test]
        public void ColoursParseFromHexAndCompositeSourceOver()
        {
            Rgba brass = Rgba.FromHex("#AC7243");
            Assert.That((brass.R, brass.G, brass.B, brass.A), Is.EqualTo(((byte)0xAC, (byte)0x72, (byte)0x43, (byte)255)));
            Assert.That(Rgba.FromHex("FE7B1A80").A, Is.EqualTo(128));
            Assert.That(brass.ToString(), Is.EqualTo("#AC7243"));
            Assert.That(brass.Scale(0.5f), Is.EqualTo(new Rgba(86, 57, 34)));
            Assert.That(Rgba.White.Scale(2f), Is.EqualTo(Rgba.White), "Lightening clamps at 255.");
            Assert.That(Rgba.Black.Lerp(Rgba.White, 0.5f), Is.EqualTo(new Rgba(128, 128, 128)));

            Assert.That(Rgba.Black.Under(Rgba.White), Is.EqualTo(Rgba.White), "An opaque colour replaces what is under it.");
            Assert.That(Rgba.Black.Under(Rgba.Transparent), Is.EqualTo(Rgba.Black));
            Rgba half = Rgba.Black.Under(Rgba.White.WithAlpha(128));
            Assert.That(half.R, Is.EqualTo(128).Within(1));
            Assert.That(half.A, Is.EqualTo(255));
            Rgba overClear = Rgba.Transparent.Under(new Rgba(200, 100, 0, 128));
            Assert.That(overClear, Is.EqualTo(new Rgba(200, 100, 0, 128)), "Over nothing, a translucent colour keeps its own alpha.");
            Assert.Throws<ArgumentException>(() => Rgba.FromHex("#12345"));
            Assert.Throws<ArgumentOutOfRangeException>(() => brass.Scale(-1f));
        }

        [Test]
        public void DrawingClipsToTheCanvasAndBlendsTranslucentColours()
        {
            var canvas = new PixelCanvas(8, 6);
            canvas.FillRect(-2, -2, 4, 4, Rgba.White);
            Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(1, 1), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(2, 2), Is.EqualTo(Rgba.Transparent));
            Assert.That(canvas.Get(-1, 0), Is.EqualTo(Rgba.Transparent), "Reads outside the canvas are transparent.");
            Assert.That(canvas.CountVisible(), Is.EqualTo(4));

            canvas.FillRect(0, 0, 8, 6, Rgba.Black.WithAlpha(128));
            Assert.That(canvas.Get(0, 0).R, Is.EqualTo(127).Within(1), "A translucent fill darkens what was there.");
            Assert.That(canvas.Get(5, 5), Is.EqualTo(Rgba.Black.WithAlpha(128)), "Over nothing it keeps its alpha.");

            canvas.Clear();
            canvas.Line(0, 0, 7, 5, Rgba.White);
            Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(7, 5), Is.EqualTo(Rgba.White));
            Assert.That(canvas.CountVisible(), Is.EqualTo(8), "One pixel per column along the long axis.");
            Assert.Throws<ArgumentOutOfRangeException>(() => new PixelCanvas(0, 4));
        }

        [Test]
        public void EllipsesTrianglesAndDitherFillTheExpectedPixels()
        {
            var canvas = new PixelCanvas(9, 9);
            canvas.FillEllipse(4, 4, 0, 0, Rgba.White);
            Assert.That(canvas.CountVisible(), Is.EqualTo(1), "A zero radius draws one pixel.");
            canvas.FillEllipse(4, 4, 3, 3, Rgba.White);
            Assert.That(canvas.Get(4, 1), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(1, 4), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(1, 1), Is.EqualTo(Rgba.Transparent), "The corners stay outside the circle.");
            Assert.That(canvas.Get(4, 0), Is.EqualTo(Rgba.Transparent));

            canvas.Clear();
            canvas.FillTriangle(0, 0, 8, 0, 0, 8, Rgba.White);
            Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(2, 2), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(8, 8), Is.EqualTo(Rgba.Transparent));
            canvas.FillTriangle(0, 8, 8, 0, 8, 8, Rgba.Black);
            Assert.That(canvas.Get(8, 8), Is.EqualTo(Rgba.Black), "Winding order does not matter.");

            canvas.Clear();
            canvas.Dither(0, 0, 4, 4, Rgba.White);
            Assert.That(canvas.CountVisible(), Is.EqualTo(8));
            Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.White));
            Assert.That(canvas.Get(1, 0), Is.EqualTo(Rgba.Transparent));
            canvas.Dither(0, 0, 4, 4, Rgba.Black, true);
            Assert.That(canvas.CountVisible(), Is.EqualTo(16), "The odd phase fills the other half.");
        }

        [Test]
        public void PixelMapsAreWrittenTopRowFirstAndOutlinesWrapVisiblePixels()
        {
            var palette = new Dictionary<char, Rgba> { ['#'] = Rgba.White, ['o'] = Rgba.Black };
            var canvas = new PixelCanvas(5, 5);
            canvas.BlitMap(new[] { ".#.", "o#o", ".#." }, palette, 1, 1);
            Assert.That(canvas.Get(2, 3), Is.EqualTo(Rgba.White), "The first row is the top of the map.");
            Assert.That(canvas.Get(1, 2), Is.EqualTo(Rgba.Black));
            Assert.That(canvas.Get(1, 3), Is.EqualTo(Rgba.Transparent), "'.' is transparent.");
            Assert.That(canvas.CountVisible(), Is.EqualTo(5));

            canvas.Outline(new Rgba(1, 2, 3));
            Assert.That(canvas.Get(2, 4), Is.EqualTo(new Rgba(1, 2, 3)));
            Assert.That(canvas.Get(0, 2), Is.EqualTo(new Rgba(1, 2, 3)));
            Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.Transparent), "Diagonal neighbours are not outlined.");
            Assert.That(canvas.Get(2, 2), Is.EqualTo(Rgba.White), "Visible pixels keep their colour.");
            Assert.That(canvas.CountVisible(), Is.EqualTo(13));

            PixelCanvas silhouette = canvas.Silhouette(Rgba.Black);
            Assert.That(silhouette.CountVisible(), Is.EqualTo(13));
            Assert.That(silhouette.Get(2, 2), Is.EqualTo(Rgba.Black));
            Assert.That(canvas.Get(2, 2), Is.EqualTo(Rgba.White), "The silhouette is a copy.");

            var asymmetric = new PixelCanvas(3, 1);
            asymmetric.Set(0, 0, Rgba.White);
            Assert.That(asymmetric.FlipHorizontal().Get(2, 0), Is.EqualTo(Rgba.White));
            var target = new PixelCanvas(4, 4);
            target.Blit(asymmetric, 1, 2);
            Assert.That(target.Get(1, 2), Is.EqualTo(Rgba.White));
        }

        [Test]
        public void NoiseIsDeterministicAndEvenlySpread()
        {
            Assert.That(PixelNoise.Value(3, 7, 11), Is.EqualTo(PixelNoise.Value(3, 7, 11)));
            Assert.That(PixelNoise.Value(3, 7, 11), Is.Not.EqualTo(PixelNoise.Value(3, 7, 12)));
            Assert.That(PixelNoise.Value(3, 7, 11), Is.Not.EqualTo(PixelNoise.Value(7, 3, 11)));

            int hits = 0;
            var buckets = new int[4];
            for (int x = 0; x < 100; x++)
            {
                for (int y = 0; y < 100; y++)
                {
                    float value = PixelNoise.Value(x, y, 5);
                    Assert.That(value, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
                    if (PixelNoise.Chance(x, y, 5, 0.25f))
                        hits++;
                    buckets[PixelNoise.Pick(x, y, 9, 4)]++;
                }
            }
            Assert.That(hits, Is.InRange(2200, 2800), "A quarter of the cells pass a 25% chance.");
            foreach (int bucket in buckets)
                Assert.That(bucket, Is.InRange(2200, 2800));
        }

        [Test]
        public void ThePaletteIsOpaqueAndDistinct()
        {
            var seen = new HashSet<Rgba>();
            foreach (var field in typeof(PixelPalette).GetFields())
            {
                var color = (Rgba)field.GetValue(null);
                Assert.That(color.A, Is.EqualTo(255), field.Name);
                seen.Add(color);
            }
            Assert.That(seen.Count, Is.GreaterThan(40), "Every material has its own tones.");
        }
    }
}
