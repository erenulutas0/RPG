using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Localisation;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The gate bosses' barks (docs/44 section 1.8; docs/64 stage 1, 2026-11-23), without Unity: each boss's three lines are
    // docs/44's English under their keys, a bark lasts 1.5-2 s and fades in and out, the telegraph line is said at most once
    // a phase, and the placement test keeps a plate off the Warden's circle and the Regent's lanes.
    public sealed class BossBarkRuleTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void EachGateBossSaysDocs44sLines()
        {
            Assert.That(Line(BossBark.WardenId, BarkMoment.Spawn), Is.EqualTo("Shift's not over."));
            Assert.That(Line(BossBark.WardenId, BarkMoment.Telegraph), Is.EqualTo("Stand clear of the mark."));
            Assert.That(Line(BossBark.WardenId, BarkMoment.Death), Is.EqualTo("Door... was never locked for you."));
            Assert.That(Line(BossBark.RegentId, BarkMoment.Spawn), Is.EqualTo("Which one of you is original?"));
            Assert.That(Line(BossBark.RegentId, BarkMoment.Telegraph), Is.EqualTo("Hold still. I need your face."));
            Assert.That(Line(BossBark.RegentId, BarkMoment.Death), Is.EqualTo("I kept the wrong one..."));
            Assert.That(BossBark.TryLine("enemy_grunt", BarkMoment.Spawn, out _), Is.False, "Only the gate bosses speak.");
            Assert.That(BossBark.TryLine(null, BarkMoment.Death, out _), Is.False);
            Assert.That(BossBark.Lines.Length, Is.EqualTo(6));
            foreach (LocText line in BossBark.Lines)
                Assert.That(line.Key, Does.StartWith("bark."), line.English);
        }

        private static string Line(string boss, BarkMoment moment)
        {
            Assert.That(BossBark.TryLine(boss, moment, out LocText line), Is.True, boss + " " + moment);
            return line.English;
        }

        [Test]
        public void ABarkLastsOneAndAHalfToTwoSecondsAndFades()
        {
            Assert.That(BossBark.Seconds, Is.InRange(1.5f, 2f), "The owner's 1.5-2 s.");
            Assert.That(BossBark.Alpha(-.01f), Is.Zero);
            Assert.That(BossBark.Alpha(0f), Is.Zero, "It fades in.");
            Assert.That(BossBark.Alpha(BossBark.FadeIn * .5f), Is.EqualTo(.5f).Within(Tolerance));
            Assert.That(BossBark.Alpha(BossBark.FadeIn), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(BossBark.Alpha(BossBark.Seconds * .5f), Is.EqualTo(1f), "Held at full.");
            Assert.That(BossBark.Alpha(BossBark.Seconds - BossBark.FadeOut * .5f), Is.EqualTo(.5f).Within(Tolerance), "It fades out.");
            Assert.That(BossBark.Alpha(BossBark.Seconds), Is.Zero, "Gone at its end.");
            Assert.That(BossBark.Shows(BossBark.Seconds - .001f), Is.True);
            Assert.That(BossBark.Shows(BossBark.Seconds), Is.False);
            Assert.That(BossBark.FadeIn + BossBark.FadeOut, Is.LessThan(BossBark.Seconds * .5f), "Mostly held, readable.");
        }

        [Test]
        public void TheTelegraphLineIsSaidAtMostOnceAPhase()
        {
            var memory = new BossBarkMemory();
            Assert.That(memory.Says(BarkMoment.Spawn, false), Is.True);
            Assert.That(memory.Says(BarkMoment.Spawn, false), Is.False, "The spawn line once.");
            Assert.That(memory.Says(BarkMoment.Telegraph, false), Is.True, "Phase one's first mark.");
            Assert.That(memory.Says(BarkMoment.Telegraph, false), Is.False, "Not with its second.");
            Assert.That(memory.Says(BarkMoment.Telegraph, false), Is.False);
            Assert.That(memory.TelegraphsSaid, Is.EqualTo(1));
            Assert.That(memory.Says(BarkMoment.Telegraph, true), Is.True, "Phase two's first mark.");
            Assert.That(memory.Says(BarkMoment.Telegraph, true), Is.False, "Not again.");
            Assert.That(memory.TelegraphsSaid, Is.EqualTo(2));
            Assert.That(memory.Says(BarkMoment.Death, false), Is.True);
            Assert.That(memory.Says(BarkMoment.Death, false), Is.False, "It falls once.");
            memory.Reset();
            Assert.That(memory.TelegraphsSaid, Is.Zero, "A new boss, a new memory.");
            Assert.That(memory.Says(BarkMoment.Spawn, false), Is.True);
            Assert.That(memory.Says(BarkMoment.Telegraph, true), Is.True, "A fight that opens in phase two still says it once.");
        }

        [Test]
        public void APlateKeepsClearOfTheWardensCircle()
        {
            const float r = WardenRule.SlamRadius;
            // The mark at the floor's (0, 0), no lift: its top edge is WorldY(2.2) = 1.1 up the screen.
            Assert.That(BossBark.ClearOfCircle(-1f, 1f, 1.3f, 1.6f, 0f, 0f, 0f, r), Is.True, "Over the circle's top.");
            Assert.That(BossBark.ClearOfCircle(-1f, 1f, 1.0f, 1.3f, 0f, 0f, 0f, r), Is.False, "Its foot on the circle.");
            Assert.That(BossBark.ClearOfCircle(-1f, 1f, -.2f, .2f, 0f, 0f, 0f, r), Is.False, "Over the mark's middle.");
            Assert.That(BossBark.ClearOfCircle(-1f, 1f, -1.6f, -1.2f, 0f, 0f, 0f, r), Is.True, "Under it.");
            Assert.That(BossBark.ClearOfCircle(2.5f, 4f, -.2f, .2f, 0f, 0f, 0f, r), Is.True, "Beside it, past its edge and margin.");
            Assert.That(BossBark.ClearOfCircle(2.3f, 4f, -.2f, .2f, 0f, 0f, 0f, r), Is.False, "Within the margin.");
            // The foundry lifts the mark onto its surface: the same plate raised as far is as clear.
            Assert.That(BossBark.ClearOfCircle(-1f, 1f, 2.3f, 2.6f, 1f, 0f, 0f, r), Is.True);
            Assert.That(BossBark.ClearOfCircle(-1f, 1f, 1.3f, 1.6f, 1f, 0f, 0f, r), Is.False, "Lifted, the mark rises under it.");
        }

        [Test]
        public void APlateKeepsClearOfTheRegentsLanes()
        {
            // A lane from (0, 0) straight up the floor, 10 long and 1.2 wide: up the screen to world height 5.
            float half = RegentRule.LaneWidth * .5f;
            Assert.That(BossBark.ClearOfLane(-1f, 1f, 1f, 1.3f, 0f, 0f, 0f, 0f, 1f, 10f, half), Is.False, "Across the lane.");
            Assert.That(BossBark.ClearOfLane(.8f, 2f, 1f, 1.3f, 0f, 0f, 0f, 0f, 1f, 10f, half), Is.True, "Beside it.");
            Assert.That(BossBark.ClearOfLane(.7f, 2f, 1f, 1.3f, 0f, 0f, 0f, 0f, 1f, 10f, half), Is.False, "Within its margin.");
            Assert.That(BossBark.ClearOfLane(-1f, 1f, 5.5f, 5.8f, 0f, 0f, 0f, 0f, 1f, 10f, half), Is.True, "Past its far end.");
            Assert.That(BossBark.ClearOfLane(-1f, 1f, 5.2f, 5.5f, 0f, 0f, 0f, 0f, 1f, 10f, half), Is.False, "Within the margin of its end.");
            Assert.That(BossBark.ClearOfLane(-1f, 1f, -.8f, -.5f, 0f, 0f, 0f, 0f, 1f, 10f, half), Is.True, "Behind its caster.");
            // A lane across the floor (along x) at depth 2 (world height 1): a plate over it is not clear, one above is.
            Assert.That(BossBark.ClearOfLane(1f, 2f, .9f, 1.1f, 0f, 0f, 2f, 1f, 0f, 10f, half), Is.False);
            Assert.That(BossBark.ClearOfLane(1f, 2f, 1.5f, 1.8f, 0f, 0f, 2f, 1f, 0f, 10f, half), Is.True);
            // A diagonal lane that crosses the plate without an end or a corner inside it.
            Assert.That(BossBark.ClearOfLane(-.5f, .5f, -.1f, .1f, 0f, -5f, -5f, .7071068f, .7071068f, 14f, .05f), Is.False);
        }

        [Test]
        public void SegmentToBoxDistanceIsExact()
        {
            Assert.That(BossBark.SegmentToBoxSquared(-3f, 0f, 3f, 0f, -1f, 1f, -1f, 1f), Is.Zero, "Through it.");
            Assert.That(BossBark.SegmentToBoxSquared(2f, -5f, 2f, 5f, -1f, 1f, -1f, 1f), Is.EqualTo(1f).Within(Tolerance), "Beside it.");
            Assert.That(BossBark.SegmentToBoxSquared(3f, 3f, 5f, 5f, -1f, 1f, -1f, 1f), Is.EqualTo(8f).Within(Tolerance), "Off a corner.");
            Assert.That(BossBark.SegmentToBoxSquared(0f, 0f, 0f, 0f, -1f, 1f, -1f, 1f), Is.Zero, "A point inside.");
        }
    }
}
