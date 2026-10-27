using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The puppet's clocks and follow-through (docs/45 section 4 B): what PuppetAnimator feeds PuppetGait, frame by frame,
    // without Unity.
    public sealed class PuppetMotionTests
    {
        private const float Frame = 1f / 60f;
        // The foundry's walking speed: floor units a frame.
        private const float Stride = 3.5f * Frame;

        private static void Run(PuppetMotion motion, float seconds, float travel)
        {
            for (float t = 0f; t < seconds - 1e-4f; t += Frame)
                motion.Tick(Frame, travel);
        }

        [Test]
        public void WalkingAdvancesTheStrideWithTheFloorAndStoppingSettlesOnAContact()
        {
            var motion = new PuppetMotion();
            Run(motion, 0.5f, Stride);
            Assert.That(motion.IsWalking, Is.True);
            Assert.That(motion.Input.WalkWeight, Is.EqualTo(1f));
            float expected = 0.5f * 3.5f / PuppetGait.StrideLength;
            Assert.That(motion.Input.WalkPhase, Is.EqualTo(expected - (float)Math.Floor(expected)).Within(0.01f),
                "The phase is the floor walked, not the time spent.");
            Run(motion, 0.3f, 0f);
            Assert.That(motion.IsWalking, Is.False);
            Assert.That(motion.Input.WalkWeight, Is.Zero, "The stride eases out in an eighth of a second.");
            Assert.That(motion.Input.WalkPhase, Is.Zero, "The next walk starts on a contact.");
        }

        [Test]
        public void AStopLurchesTheBodyAndSwingsTheCapeForwardThenBothSettle()
        {
            var motion = new PuppetMotion();
            Run(motion, 1f, Stride);
            Assert.That(Math.Abs(motion.Input.Surge), Is.LessThan(0.01f), "A steady stride holds no lurch.");
            float surge = 0f, drape = 0f, rebound = 0f;
            for (int frame = 0; frame < 120; frame++)
            {
                motion.Tick(Frame, 0f);
                surge = Math.Max(surge, motion.Input.Surge);
                drape = Math.Max(drape, motion.Input.Drape);
                rebound = Math.Min(rebound, motion.Input.Surge);
            }
            Assert.That(surge, Is.InRange(0.4f, 1.5f), "The body lurches forward past the stop.");
            Assert.That(drape, Is.InRange(0.4f, 1.5f), "and the cape swings on.");
            Assert.That(rebound, Is.LessThan(0f), "The body rocks back once.");
            Run(motion, 1.5f, 0f);
            Assert.That(Math.Abs(motion.Input.Surge), Is.LessThan(0.005f), "Then everything settles.");
            Assert.That(Math.Abs(motion.Input.Drape), Is.LessThan(0.02f));
            Assert.That(motion.Pose.Lean, Is.EqualTo(0f).Within(0.05f));

            // Starting is the other way round: the body and the cape are left behind for a moment.
            var start = new PuppetMotion();
            float behind = 0f;
            for (int frame = 0; frame < 20; frame++)
            {
                start.Tick(Frame, Stride);
                behind = Math.Min(behind, start.Input.Drape);
            }
            Assert.That(behind, Is.LessThan(-0.2f), "The cape trails as the stride starts.");
        }

        [Test]
        public void TheDrawFollowsTheCooldownUpAndEasesDownWhenTheTargetLeaves()
        {
            var motion = new PuppetMotion();
            motion.Anticipate(PuppetStrike.Swing, 0.5f);
            motion.Tick(Frame, 0f);
            Assert.That(motion.Input.Windup, Is.EqualTo(PuppetStrike.Swing));
            Assert.That(motion.Input.WindupProgress, Is.EqualTo(0.5f), "The draw meets the cooldown exactly.");
            // PuppetAnimator says nothing is coming once the target is out of reach.
            motion.Anticipate(PuppetStrike.None, 0f);
            motion.Tick(Frame, 0f);
            Assert.That(motion.Input.WindupProgress, Is.InRange(0.3f, 0.5f), "With no target it eases down...");
            Assert.That(motion.Input.Windup, Is.EqualTo(PuppetStrike.Swing), "still the sword it was drawing.");
            Assert.That(motion.Pose.ShoulderRight, Is.GreaterThan(PuppetGait.Rest.ShoulderRight + 10f), "No snap back.");
            Run(motion, PuppetMotion.WindupRelease, 0f);
            Assert.That(motion.Input.WindupProgress, Is.Zero, "...and is gone.");
            Assert.That(motion.Input.Windup, Is.EqualTo(PuppetStrike.None));
        }

        [Test]
        public void AStrikeSpendsTheDrawPlaysForItsShareOfTheIntervalAndEnds()
        {
            var motion = new PuppetMotion();
            motion.Anticipate(PuppetStrike.Swing, 1f);
            motion.Tick(Frame, 0f);
            motion.Strike(PuppetStrike.Swing, 0.8f);
            Assert.That(motion.IsAttacking, Is.True);
            Assert.That(motion.Input.StrikeProgress, Is.Zero, "The blow shows at the event.");
            Assert.That(motion.Input.WindupProgress, Is.Zero, "The draw is spent.");
            motion.Anticipate(PuppetStrike.None, 0f);
            Run(motion, PuppetGait.StrikeSeconds(0.8f) - 2 * Frame, 0f);
            Assert.That(motion.IsAttacking, Is.True);
            Run(motion, 3 * Frame, 0f);
            Assert.That(motion.IsAttacking, Is.False, "Back on the walk inside the interval.");
            Assert.That(motion.Pose.ShoulderRight, Is.EqualTo(PuppetGait.Evaluate(motion.Input).ShoulderRight));
        }

        [Test]
        public void TheDaggersAlternateTheirLeadingHand()
        {
            var motion = new PuppetMotion();
            motion.Strike(PuppetStrike.Thrust, 0.5f);
            bool first = motion.Input.LeftLeads;
            motion.Strike(PuppetStrike.Thrust, 0.5f);
            Assert.That(motion.Input.LeftLeads, Is.Not.EqualTo(first));
            motion.Strike(PuppetStrike.Swing, 0.8f);
            motion.Strike(PuppetStrike.Swing, 0.8f);
            Assert.That(motion.Input.LeftLeads, Is.Not.EqualTo(first), "Only a thrust changes hands.");
        }

        [Test]
        public void TheCastPlaysForCastSecondsOverWhateverElseIsHappening()
        {
            var motion = new PuppetMotion();
            Run(motion, 0.3f, Stride);
            motion.Cast();
            Assert.That(motion.IsCasting, Is.True);
            Run(motion, PuppetGait.CastSeconds - 2 * Frame, Stride);
            Assert.That(motion.IsCasting, Is.True);
            Assert.That(motion.IsWalking, Is.True, "The legs keep their stride through the slam.");
            Run(motion, 3 * Frame, Stride);
            Assert.That(motion.IsCasting, Is.False);
        }

        [Test]
        public void AHitFlinchesForFlinchSecondsAndDeathStopsEverything()
        {
            var motion = new PuppetMotion();
            motion.Hit();
            Assert.That(motion.Input.Flinch, Is.EqualTo(1f));
            Run(motion, PuppetGait.FlinchSeconds + Frame, 0f);
            Assert.That(motion.Input.Flinch, Is.Zero);

            motion.Strike(PuppetStrike.Swing, 0.8f);
            motion.Cast();
            motion.Die();
            Assert.That(motion.IsDying, Is.True);
            Assert.That(motion.IsAttacking, Is.False, "The strike lets go.");
            Assert.That(motion.IsCasting, Is.False);
            motion.Hit();
            motion.Strike(PuppetStrike.Swing, 0.8f);
            motion.Cast();
            Assert.That(motion.Input.Flinch, Is.Zero, "Nothing more lands on a falling figure.");
            Assert.That(motion.IsAttacking || motion.IsCasting, Is.False);
            Run(motion, PuppetGait.DeathSeconds + Frame, Stride);
            Assert.That(motion.Input.DeathProgress, Is.EqualTo(1f));
            Assert.That(motion.Pose.Fall, Is.EqualTo(PuppetGait.FallAngle).Within(1e-3f));
            Assert.That(motion.Input.WalkWeight, Is.Zero, "A falling figure does not walk on.");

            motion.Reset();
            Assert.That(motion.IsDying, Is.False);
            Assert.That(motion.Pose.Fall, Is.Zero, "Reset stands it up again.");
        }

        [Test]
        public void APausedCombatChangesNothing()
        {
            var motion = new PuppetMotion();
            Run(motion, 0.4f, Stride);
            motion.Strike(PuppetStrike.Cast, 1.1f);
            motion.Tick(Frame, Stride);
            PuppetGaitInput before = motion.Input;
            PuppetPose pose = motion.Pose;
            motion.Tick(0f, Stride);
            motion.Tick(-1f, Stride);
            motion.Tick(float.NaN, Stride);
            Assert.That(motion.Input, Is.EqualTo(before));
            Assert.That(motion.Pose, Is.EqualTo(pose));
        }

        [Test]
        public void TheLegsOfTheShownFacingCarryTheBob()
        {
            var motion = new PuppetMotion();
            Run(motion, 0.37f, Stride);
            float reference = motion.Pose.Lift;
            motion.Legs = new PuppetLegs
            {
                Right = new PuppetLeg { KneeY = -0.2f, HeelX = -0.08f, HeelY = -0.5f, ToeX = 0.08f, ToeY = -0.5f },
                Left = new PuppetLeg { KneeY = -0.2f, HeelX = -0.08f, HeelY = -0.5f, ToeX = 0.08f, ToeY = -0.5f }
            };
            Assert.That(motion.Pose.Lift, Is.Not.EqualTo(reference), "Longer legs drop further at the same stride.");
            Assert.That(motion.Pose, Is.EqualTo(PuppetGait.Evaluate(motion.Input)));
        }
    }
}
