using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The cut-out puppet's facing and its turn (docs/45 section 4 B): the facing the travel asks for is taken at once,
    // and the figure on show follows it through a turn a tenth of a second long that narrows, swaps at its narrowest and
    // widens again, the head ahead of the body.
    public sealed class PuppetFacingTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheFacingAskedForIsTakenAtOnceAndShownThroughAShortTurn()
        {
            var facing = new PuppetFacing();
            Assert.That(facing.Face(-1f, 0f), Is.True);
            Assert.That(facing.Mirrored, Is.True, "Whatever reads the facing has it at once.");
            Assert.That(facing.ShownMirrored, Is.False, "The figure on show turns to it.");
            Assert.That(facing.IsTurning, Is.True);
            Assert.That(facing.Width, Is.EqualTo(1f), "from its own width.");

            int frames = 0, swaps = 0;
            float atSwap = 1f, previous = 1f;
            while (!facing.ShownMirrored && frames < 60)
            {
                if (facing.Step(Frame))
                {
                    swaps++;
                    atSwap = facing.Width;
                }
                frames++;
                if (!facing.ShownMirrored)
                {
                    Assert.That(facing.Width, Is.LessThan(previous), "It narrows toward the swap.");
                    previous = facing.Width;
                }
            }
            Assert.That(frames, Is.InRange(2, 4), "It swaps halfway, three frames in at 60 frames a second.");
            Assert.That(swaps, Is.EqualTo(1));
            Assert.That(atSwap, Is.EqualTo(PuppetFacing.NarrowestWidth).Within(0.1f), "at its narrowest.");
            while (facing.IsTurning && frames < 60)
            {
                Assert.That(facing.Step(Frame), Is.False, "One swap a turn.");
                frames++;
            }
            Assert.That(frames * Frame, Is.EqualTo(PuppetFacing.TurnSeconds).Within(Frame + 1e-4f),
                "The whole turn takes a tenth of a second.");
            Assert.That(facing.Width, Is.EqualTo(1f), "and leaves the figure at its own width.");
            Assert.That(facing.HeadWidth, Is.EqualTo(1f));
            Assert.That(facing.HeadMirrored, Is.False);
            Assert.That(facing.Mirrored && facing.ShownMirrored, Is.True);
        }

        [Test]
        public void TurningUpOrDownTheScreenSwapsTheFacingsTheSameWay()
        {
            var facing = new PuppetFacing();
            facing.Face(0.3f, -1f);
            Assert.That(facing.Front && !facing.Mirrored, Is.True, "Toward the camera asks for the front at once.");
            int frames = 0;
            while (!facing.ShownFront && frames < 60)
            {
                facing.Step(Frame);
                frames++;
            }
            Assert.That(frames, Is.InRange(2, 4));
            while (facing.IsTurning && frames < 60)
            {
                facing.Step(Frame);
                frames++;
            }
            Assert.That(frames, Is.InRange(5, 7));
            Assert.That(facing.ShownFront && !facing.ShownMirrored, Is.True);
        }

        [Test]
        public void TheHeadLooksRoundBeforeTheBodyTurns()
        {
            var facing = new PuppetFacing();
            facing.Face(-1f, 0f);
            facing.Step(Frame);
            Assert.That(facing.HeadWidth, Is.LessThan(facing.Width), "The head narrows ahead of the body.");
            bool headFirst = false;
            for (int frame = 0; frame < 10 && !facing.ShownMirrored; frame++)
            {
                if (facing.HeadMirrored)
                    headFirst = true;
                facing.Step(Frame);
            }
            Assert.That(headFirst, Is.True, "and shows the new mirror a frame before the body swaps.");
            Assert.That(facing.HeadMirrored, Is.False, "Once the body has swapped, the head turns with it.");
        }

        [Test]
        public void ATurnBackBeforeTheSwapNeverShowsTheOtherFacing()
        {
            var facing = new PuppetFacing();
            facing.Face(-1f, 0f);
            facing.Step(Frame);
            float narrowed = facing.Width;
            Assert.That(facing.Face(1f, 0f), Is.True);
            Assert.That(facing.Mirrored, Is.False, "The stick came back: asked for at once.");
            Assert.That(facing.Width, Is.EqualTo(narrowed), "No jump in width.");
            for (int frame = 0; frame < 20 && facing.IsTurning; frame++)
            {
                facing.Step(Frame);
                Assert.That(facing.ShownMirrored, Is.False, "It widens back without ever swapping.");
            }
            Assert.That(facing.IsTurning, Is.False);
            Assert.That(facing.Width, Is.EqualTo(1f));
        }

        [Test]
        public void ANewFacingAfterTheSwapNarrowsAgainFromTheSameWidth()
        {
            var facing = new PuppetFacing();
            facing.Face(-1f, 0f);
            for (int frame = 0; frame < 4; frame++)
                facing.Step(Frame);
            Assert.That(facing.ShownMirrored, Is.True, "Swapped, and widening.");
            float width = facing.Width;
            facing.Face(-0.3f, -1f);
            Assert.That(facing.Width, Is.EqualTo(width).Within(1e-5f), "No jump in width.");
            int frames = 0;
            while (facing.IsTurning && frames < 60)
            {
                facing.Step(Frame);
                frames++;
            }
            Assert.That(facing.ShownFront && facing.ShownMirrored, Is.True, "It narrows again and swaps to the front.");
            Assert.That(frames, Is.LessThanOrEqualTo(7));
            Assert.That(facing.Width, Is.EqualTo(1f));
        }

        [Test]
        public void EachAxisKeepsItsFacingInsideTheDeadBand()
        {
            var facing = new PuppetFacing();
            Assert.That(facing.Face(1f, 0.1f), Is.False, "Nearly level travel keeps the rear.");
            Assert.That(facing.Face(-0.1f, 1f), Is.False, "Nearly upright travel keeps the mirror.");
            Assert.That(facing.Face(0f, 0f), Is.False, "No travel, no facing.");
            Assert.That(facing.Face(float.NaN, 1f), Is.False);
            Assert.That(facing.Face(-0.2f, -1f), Is.True);
            Assert.That(facing.Front && facing.Mirrored, Is.True);
        }

        [Test]
        public void APausedFrameHoldsTheTurnAndSettleShowsAtOnce()
        {
            var facing = new PuppetFacing();
            facing.Face(0f, -1f);
            facing.Step(Frame);
            float progress = facing.Progress;
            facing.Step(0f);
            facing.Step(-1f);
            facing.Step(float.NaN);
            facing.Step(float.PositiveInfinity);
            Assert.That(facing.Progress, Is.EqualTo(progress), "A paused combat freezes the turn.");
            facing.Settle();
            Assert.That(facing.ShownFront, Is.True, "Settling shows the facing asked for at once.");
            Assert.That(facing.IsTurning, Is.False);
            Assert.That(facing.Width, Is.EqualTo(1f));
        }
    }
}
