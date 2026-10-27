using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The cut-out puppet's stride against the floor (docs/45 section 4 B): on every heading of the stick the planted
    // boot holds the floor as it scrolls past, a swinging boot clears it, and the far boot walks near the near one's
    // floor line. Slip is measured as Tools/ArtReview's previewer reports it: phone pixels a frame at 60 frames a second.
    public sealed class PuppetStrideTests
    {
        private const float Frame = 1f / 60f;
        // The game's framing: 120 phone pixels a world unit.
        private const float Pixels = 120f;
        // The Vanguard's parts are painted at 128 texels a unit.
        private const float Texels = 128f;

        // The stick's eight headings on the floor: east first, turning up the screen.
        private static readonly float[] HeadingX = { 1f, 0.7071f, 0f, -0.7071f, -1f, -0.7071f, 0f, 0.7071f };
        private static readonly float[] HeadingY = { 0f, 0.7071f, 1f, 0.7071f, 0f, -0.7071f, -1f, -0.7071f };

        private static PuppetLegs Legs(bool front) => front ? PuppetGaitTests.VanguardFront : PuppetGaitTests.Vanguard;

        [Test]
        public void ThePlantedBootHoldsTheFloorOnEveryHeading()
        {
            foreach (float speed in new[] { 2.5f, 3.5f })
            {
                for (int heading = 0; heading < HeadingX.Length; heading++)
                {
                    Walk(HeadingX[heading], HeadingY[heading], speed, out float mean, out float worst, out int planted);
                    string at = $"heading {heading * 45} degrees at {speed} units a second";
                    // The floor itself scrolls past at 2.5 to 7 phone pixels a frame; the old stride slipped 4 to 6.
                    Assert.That(planted, Is.GreaterThan(20), at + ": a boot is down for a good part of the run.");
                    Assert.That(mean, Is.LessThan(0.2f), at + ": the planted boot holds the floor.");
                    Assert.That(worst, Is.LessThan(0.3f), at + ": on every frame its sole does not roll over.");
                }
            }
        }

        // Runs the hero along a floor heading for two and a half seconds the way PuppetAnimator drives the puppet - the
        // facing asked for at once, shown through its turn, the stride's heading in the shown facing's terms - and
        // follows the planted boot's sole in the world from frame to frame. A planted boot pivots about one end, the
        // heel and then the toe as it rolls, so its slip is the less moved end's travel; on the frame the roll hands
        // over from heel to toe both ends move a little, which is the roll, so those frames count toward the mean but
        // not the worst.
        private static void Walk(float dirX, float dirY, float speed, out float mean, out float worst, out int planted)
        {
            var motion = new PuppetMotion();
            var facing = new PuppetFacing();
            motion.Legs = Legs(false);
            bool strideMirrored = false;
            float x = 0f, y = 0f;
            int lastBoot = -1;
            bool lastHeelDown = false;
            float lastHeelX = 0f, lastHeelY = 0f, lastToeX = 0f, lastToeY = 0f;
            double sum = 0d;
            worst = 0f;
            planted = 0;
            for (int frame = 0; frame < 150; frame++)
            {
                float dx = speed * dirX * Frame, dy = speed * dirY * Frame;
                x += dx;
                y += dy * PuppetGait.FloorDepth;
                facing.Face(dx, dy);
                if (facing.Step(Frame))
                {
                    if (facing.ShownMirrored != strideMirrored)
                    {
                        strideMirrored = facing.ShownMirrored;
                        motion.Mirror();
                    }
                    motion.Legs = Legs(facing.ShownFront);
                    motion.Front = facing.ShownFront;
                }
                motion.Tick(Frame, (float)Math.Sqrt(dx * dx + dy * dy),
                    (float)Math.Atan2(dy, facing.ShownMirrored ? -dx : dx));
                PuppetPose pose = motion.Pose;
                int boot = pose.RightPlanted ? 0 : pose.LeftPlanted ? 1 : -1;
                if (frame < 40 || motion.Input.WalkWeight < 1f || facing.IsTurning || boot < 0)
                {
                    lastBoot = -1;
                    continue;
                }
                PuppetLegs legs = Legs(facing.ShownFront);
                PuppetGait.Sole(pose, boot == 0 ? legs.Right : legs.Left, boot == 0, out float heelX, out float heelY,
                    out float toeX, out float toeY);
                float sign = facing.ShownMirrored ? -1f : 1f;
                heelX = x + sign * heelX;
                heelY = y + heelY;
                toeX = x + sign * toeX;
                toeY = y + toeY;
                bool heelDown = heelY <= toeY;
                if (boot == lastBoot)
                {
                    float heel = Distance(heelX - lastHeelX, heelY - lastHeelY) * Pixels;
                    float toe = Distance(toeX - lastToeX, toeY - lastToeY) * Pixels;
                    float slip = Math.Min(heel, toe);
                    sum += slip;
                    planted++;
                    if (heelDown == lastHeelDown)
                        worst = Math.Max(worst, slip);
                }
                lastBoot = boot;
                lastHeelDown = heelDown;
                lastHeelX = heelX;
                lastHeelY = heelY;
                lastToeX = toeX;
                lastToeY = toeY;
            }
            mean = planted > 0 ? (float)(sum / planted) : float.MaxValue;
        }

        private static float Distance(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        // Whatever the heading does to the legs on the screen, the boot that is down stands on its floor line - which a
        // stride up or down the screen carries with the boot's depth - and the one in the air stays above it.
        [Test]
        public void APlantedBootStandsOnItsFloorAndASwingingOneClearsItOnEveryHeading()
        {
            // The headings each painted facing shows, in its own terms: from behind level, up the screen and diagonally
            // up; from the front level, down and diagonally down.
            float[] headings = { 0f, 0.785f, 1.571f, 0f, -0.785f, -1.571f };
            for (int k = 0; k < headings.Length; k++)
            {
                bool front = k >= 3;
                PuppetLegs legs = Legs(front);
                for (float phase = 0f; phase < 1f; phase += 0.005f)
                {
                    var input = new PuppetGaitInput
                    {
                        WalkPhase = phase, WalkWeight = 1f, Heading = headings[k], Front = front, Legs = legs
                    };
                    PuppetPose pose = PuppetGait.Evaluate(input);
                    string at = $"{(front ? "front" : "rear")} heading {headings[k]} at phase {phase}";
                    float right = PuppetGaitTests.Height(pose, legs.Right, true) - pose.FloorRight;
                    float left = PuppetGaitTests.Height(pose, legs.Left, false) - pose.FloorLeft;
                    if (pose.RightPlanted)
                        Assert.That(right, Is.EqualTo(0f).Within(0.002f), at + ": the planted right boot.");
                    else
                        Assert.That(right, Is.GreaterThan(-0.004f), at + ": the swinging right boot.");
                    if (pose.LeftPlanted)
                        Assert.That(left, Is.EqualTo(0f).Within(0.002f), at + ": the planted left boot.");
                    else
                        Assert.That(left, Is.GreaterThan(-0.004f), at + ": the swinging left boot.");
                }
            }
        }

        // The painting stands wide, the far boot sixteen texels up the screen: walking, the far leg comes in under the
        // body to within a few texels of the near boot's line, so the figure stays on its shadow whichever boot is down.
        [Test]
        public void TheFarBootWalksNearTheNearOnesFloorLine()
        {
            foreach (bool front in new[] { false, true })
            {
                PuppetLegs legs = Legs(front);
                float near = legs.Right.HipY + PuppetGait.FloorLine(legs.Right);
                float far = legs.Left.HipY + PuppetGait.FloorLine(legs.Left);
                string facing = front ? "From the front" : "From behind";
                Assert.That((far - near) * Texels, Is.InRange(14f, 18f), facing + ", as painted, the far boot stands up the screen.");
                PuppetPose standing = PuppetGait.Evaluate(new PuppetGaitInput { Legs = legs });
                Assert.That(standing.FloorLeft, Is.Zero, "Standing keeps the painted stance.");
                Assert.That(standing.LegRiseLeft, Is.Zero);
                for (float phase = 0f; phase < 1f; phase += 0.05f)
                {
                    PuppetPose pose = PuppetGait.Evaluate(new PuppetGaitInput
                    {
                        WalkPhase = phase, WalkWeight = 1f, Legs = legs, Front = front
                    });
                    float gap = (far + pose.FloorLeft) - (near + pose.FloorRight);
                    Assert.That(gap * Texels, Is.InRange(2f, 7.5f),
                        facing + " on the move the far boot's line comes down near the near one's, a little behind it, at " + phase);
                }
            }
        }
    }
}
