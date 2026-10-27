using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Cinder Arcanist's effect art (docs/45): the flame of each Burn stack count, its sheet, the Vent's eruption and
    // embers, and the ember Ember Memory passes on.
    public sealed class BurnArtTests
    {
        [Test]
        public void EveryStackCountFlickersThroughFourDistinctOutlinedFrames()
        {
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                PixelCanvas[] frames = BurnArt.FlameFrames(stacks);
                Assert.That(frames.Length, Is.EqualTo(BurnArt.FlameFrameCount));
                for (int frame = 0; frame < frames.Length; frame++)
                {
                    PixelCanvas canvas = frames[frame];
                    string at = $"{stacks} stacks, frame {frame}";
                    Assert.That((canvas.Width, canvas.Height), Is.EqualTo((BurnArt.FlameWidth(stacks), BurnArt.FlameHeight(stacks))), at);
                    Assert.That(Has(canvas, BurnArt.Soot), Is.True, $"{at}: a soot outline, for the orange lava.");
                    Assert.That(Has(canvas, BurnArt.Ember), Is.True, $"{at}: an ember body.");
                    Assert.That(Has(canvas, BurnArt.Crimson), Is.True, $"{at}: crimson edges.");
                    Assert.That(canvas.Get(canvas.Width / 2, 0), Is.Not.EqualTo(BurnArt.Soot).And.Not.EqualTo(Rgba.Transparent),
                        $"{at}: the foot stands open on the body, flame and not outline.");
                    Assert.That(canvas.Get(0, canvas.Height - 1), Is.EqualTo(Rgba.Transparent), $"{at}: the top corners are clear.");
                    Assert.That(canvas.Get(canvas.Width - 1, canvas.Height - 1), Is.EqualTo(Rgba.Transparent));
                    Assert.That(Differ(canvas, frames[(frame + 1) % frames.Length]), Is.True, $"{at}: it flickers.");
                }
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => BurnArt.FlameFrame(0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BurnArt.FlameFrame(BurnRule.MaxStacks + 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => BurnArt.FlameFrame(1, BurnArt.FlameFrameCount));
        }

        [Test]
        public void MoreStacksBurnLargerAndHotter()
        {
            int lastSize = 0;
            int lastHeight = 0;
            float lastHeat = -1f;
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                PixelCanvas rest = BurnArt.FlameFrame(stacks, 0);
                int size = rest.CountVisible();
                int height = TopRow(rest) + 1;
                float heat = (float)(Count(rest, BurnArt.Highlight) + Count(rest, BurnArt.WhiteHot)) / size;
                Assert.That(size, Is.GreaterThan(lastSize), $"{stacks} stacks: a larger flame.");
                Assert.That(height, Is.GreaterThan(lastHeight), $"{stacks} stacks: a taller flame.");
                Assert.That(heat, Is.GreaterThan(lastHeat), $"{stacks} stacks: a larger share burns yellow.");
                lastSize = size;
                lastHeight = height;
                lastHeat = heat;
            }
            Assert.That(Has(BurnArt.FlameFrame(1, 0), BurnArt.WhiteHot), Is.False, "One stack is an ember lick, never white-hot.");
            Assert.That(Has(BurnArt.FlameFrame(BurnRule.MaxStacks, 0), BurnArt.WhiteHot), Is.True, "A full burn is white-hot.");
            Assert.That(BurnArt.FlameHeight(BurnRule.MaxStacks) / BurnArt.TexelsPerUnit, Is.LessThan(.5f),
                "Even a full burn stays under half a unit, so it never buries its enemy.");
        }

        [Test]
        public void TheSheetHoldsEveryFrameInItsOwnCell()
        {
            PixelCanvas sheet = BurnArt.FlameSheet();
            Assert.That((sheet.Width, sheet.Height), Is.EqualTo((BurnArt.FlameSheetWidth, BurnArt.FlameSheetHeight)));
            int cells = 0;
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                for (int frame = 0; frame < BurnArt.FlameFrameCount; frame++)
                {
                    BurnArt.FlameCell(stacks, frame, out int x, out int y);
                    PixelCanvas expected = BurnArt.FlameFrame(stacks, frame);
                    Assert.That(x + expected.Width, Is.LessThanOrEqualTo(sheet.Width));
                    Assert.That(y + expected.Height, Is.LessThanOrEqualTo(sheet.Height));
                    for (int py = 0; py < expected.Height; py++)
                    {
                        for (int px = 0; px < expected.Width; px++)
                            Assert.That(sheet.Get(x + px, y + py), Is.EqualTo(expected.Get(px, py)), $"{stacks}-{frame} at {px},{py}");
                    }
                    // The gutter above and to the right of a cell stays clear, so no frame samples its neighbour.
                    for (int py = 0; py < expected.Height; py++)
                        Assert.That(sheet.Get(x + expected.Width, y + py).A, Is.Zero);
                    for (int px = 0; px < expected.Width; px++)
                        Assert.That(sheet.Get(x + px, y + expected.Height).A, Is.Zero);
                    cells++;
                }
            }
            Assert.That(cells, Is.EqualTo(BurnRule.MaxStacks * BurnArt.FlameFrameCount));
        }

        [Test]
        public void TheVentFlaresRisesAsAColumnBreaksUpAndLeavesEmbers()
        {
            PixelCanvas[] frames = BurnArt.VentColumnFrames();
            Assert.That(frames.Length, Is.EqualTo(4));
            foreach (PixelCanvas frame in frames)
            {
                Assert.That((frame.Width, frame.Height), Is.EqualTo((BurnArt.VentColumnWidth, BurnArt.VentColumnHeight)));
                Assert.That(Has(frame, BurnArt.Soot), Is.True, "Outlined, for the orange lava.");
                Assert.That(frame.Get(frame.Width / 2, 0).A, Is.GreaterThan(0), "It stands on the floor at its foot.");
                Assert.That(frame.Get(0, frame.Height - 1), Is.EqualTo(Rgba.Transparent));
            }
            PixelCanvas flare = frames[0], column = frames[1], breaking = frames[2], embers = frames[3];
            Assert.That(Has(flare, BurnArt.WhiteHot) && Has(column, BurnArt.WhiteHot), Is.True, "The eruption is white-hot.");
            Assert.That(TopRow(column), Is.GreaterThan(TopRow(flare) + 20), "The column rises far over the flare.");
            Assert.That(TopRow(breaking), Is.GreaterThanOrEqualTo(TopRow(column)), "Its tongues and embers fly higher still.");
            Assert.That(ColumnHasGap(breaking, BurnArt.VentColumnWidth / 2), Is.True, "The column breaks into pieces.");
            Assert.That(Has(embers, BurnArt.WhiteHot) || Has(embers, BurnArt.Highlight), Is.False, "Then only cooling embers.");
            Assert.That(embers.CountVisible(), Is.LessThan(column.CountVisible()));
        }

        [Test]
        public void TheVentsEmbersRiseAndCool()
        {
            PixelCanvas[] frames = BurnArt.VentEmberFrames();
            Assert.That(frames.Length, Is.EqualTo(3));
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.That((frames[i].Width, frames[i].Height), Is.EqualTo((BurnArt.VentEmberWidth, BurnArt.VentEmberHeight)));
                Assert.That(Has(frames[i], BurnArt.Soot), Is.True);
                if (i > 0)
                    Assert.That(BottomRow(frames[i]), Is.GreaterThan(BottomRow(frames[i - 1])), $"Frame {i} has risen.");
            }
            Assert.That(Has(frames[0], BurnArt.Highlight), Is.True, "Hot as they leave.");
            Assert.That(Has(frames[2], BurnArt.Crimson), Is.True, "Crimson as they cool.");
            Assert.That(Has(frames[2], BurnArt.Highlight) || Has(frames[2], BurnArt.WhiteHot), Is.False);
            Assert.That(frames[2].CountVisible(), Is.LessThan(frames[0].CountVisible()), "And thinning out.");
        }

        [Test]
        public void ThePassedEmberHasAHotHeadAndACoolingTailBehindIt()
        {
            PixelCanvas[] frames = BurnArt.EmberArcFrames();
            Assert.That(frames.Length, Is.EqualTo(2));
            int middle = BurnArt.EmberArcHeight / 2;
            foreach (PixelCanvas frame in frames)
            {
                Assert.That((frame.Width, frame.Height), Is.EqualTo((BurnArt.EmberArcWidth, BurnArt.EmberArcHeight)));
                Assert.That(frame.Get(BurnArt.EmberArcHeadX, middle), Is.EqualTo(BurnArt.WhiteHot), "The head is white-hot.");
                Assert.That(Has(frame, BurnArt.Crimson), Is.True, "The tail cools to crimson.");
                Assert.That(Has(frame, BurnArt.Soot), Is.True);
                Assert.That(LeftmostVisible(frame, BurnArt.Crimson), Is.LessThan(BurnArt.EmberArcHeadX),
                    "The tail trails behind the head, which leads along +x.");
                Assert.That(frame.Get(frame.Width - 1, middle), Is.EqualTo(Rgba.Transparent), "Nothing leads the head.");
            }
            Assert.That(Differ(frames[0], frames[1]), Is.True, "The tail flickers.");
        }

        [Test]
        public void BurnArtIsDeterministic()
        {
            Assert.That(Same(BurnArt.FlameSheet(), BurnArt.FlameSheet()), Is.True);
            Assert.That(Same(BurnArt.VentColumnFrames()[2], BurnArt.VentColumnFrames()[2]), Is.True);
            Assert.That(Same(BurnArt.VentEmberFrames()[1], BurnArt.VentEmberFrames()[1]), Is.True);
            Assert.That(Same(BurnArt.EmberArcFrames()[1], BurnArt.EmberArcFrames()[1]), Is.True);
        }

        private static bool Has(PixelCanvas canvas, Rgba color) => Count(canvas, color) > 0;

        private static int Count(PixelCanvas canvas, Rgba color)
        {
            int count = 0;
            foreach (Rgba pixel in canvas.Pixels)
            {
                if (pixel == color)
                    count++;
            }
            return count;
        }

        private static int TopRow(PixelCanvas canvas)
        {
            for (int y = canvas.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (canvas.Get(x, y).A > 0)
                        return y;
                }
            }
            return -1;
        }

        private static int BottomRow(PixelCanvas canvas)
        {
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (canvas.Get(x, y).A > 0)
                        return y;
                }
            }
            return -1;
        }

        private static int LeftmostVisible(PixelCanvas canvas, Rgba color)
        {
            for (int x = 0; x < canvas.Width; x++)
            {
                for (int y = 0; y < canvas.Height; y++)
                {
                    if (canvas.Get(x, y) == color)
                        return x;
                }
            }
            return canvas.Width;
        }

        // Whether the column has a transparent run above its lowest drawn texel and below its highest.
        private static bool ColumnHasGap(PixelCanvas canvas, int x)
        {
            bool started = false;
            bool gap = false;
            for (int y = 0; y < canvas.Height; y++)
            {
                bool visible = canvas.Get(x, y).A > 0;
                if (visible && gap)
                    return true;
                if (visible)
                    started = true;
                else if (started)
                    gap = true;
            }
            return false;
        }

        private static bool Differ(PixelCanvas a, PixelCanvas b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
                return true;
            return !Same(a, b);
        }

        private static bool Same(PixelCanvas a, PixelCanvas b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
                return false;
            for (int i = 0; i < a.Pixels.Length; i++)
            {
                if (a.Pixels[i] != b.Pixels[i])
                    return false;
            }
            return true;
        }
    }
}
