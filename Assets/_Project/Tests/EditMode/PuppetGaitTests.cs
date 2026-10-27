using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The cut-out puppet's pose math (docs/45 section 4 B): the walk, the breath, the strike overlay, the flinch and the
    // collapse, without Unity.
    public sealed class PuppetGaitTests
    {
        private const float Tolerance = 1e-3f;

        private static readonly PuppetStrike[] Strikes = { PuppetStrike.Swing, PuppetStrike.Cast, PuppetStrike.Thrust };

        private static PuppetGaitInput Walking(float phase) => new PuppetGaitInput { WalkPhase = phase, WalkWeight = 1f };

        [Test]
        public void TheTwoContactsMirrorEachOther()
        {
            PuppetPose a = PuppetGait.Evaluate(Walking(0f));
            PuppetPose b = PuppetGait.Evaluate(Walking(0.5f));

            Assert.That(a.HipRight, Is.EqualTo(PuppetGait.HipSwing).Within(Tolerance), "Contact A: the right foot is ahead.");
            Assert.That(a.HipLeft, Is.EqualTo(-PuppetGait.HipSwing).Within(Tolerance), "and the left behind.");
            Assert.That(b.HipLeft, Is.EqualTo(a.HipRight).Within(Tolerance));
            Assert.That(b.HipRight, Is.EqualTo(a.HipLeft).Within(Tolerance));
            Assert.That(b.KneeLeft, Is.EqualTo(a.KneeRight).Within(Tolerance));
            Assert.That(b.KneeRight, Is.EqualTo(a.KneeLeft).Within(Tolerance));
            Assert.That(b.ShoulderLeft, Is.EqualTo(a.ShoulderRight).Within(Tolerance));
            Assert.That(b.ShoulderRight, Is.EqualTo(a.ShoulderLeft).Within(Tolerance));
            Assert.That(b.ElbowLeft, Is.EqualTo(a.ElbowRight).Within(Tolerance));
            Assert.That(b.ElbowRight, Is.EqualTo(a.ElbowLeft).Within(Tolerance));
            Assert.That(b.Lift, Is.EqualTo(a.Lift).Within(Tolerance));
            Assert.That(b.Lean, Is.EqualTo(a.Lean).Within(Tolerance));
            Assert.That(b.Head, Is.EqualTo(a.Head).Within(Tolerance));
            Assert.That(b.Cape, Is.EqualTo(a.Cape).Within(Tolerance));

            Assert.That(a.ShoulderRight, Is.LessThan(a.ShoulderLeft), "Each arm swings against its own side's leg.");
            Assert.That(a.Lift, Is.EqualTo(-PuppetGait.BobDepth).Within(Tolerance), "The hips drop at a contact.");
            Assert.That(a.Lean, Is.GreaterThan(0f), "A walking figure leans into its travel.");
        }

        [Test]
        public void ThePassingPosesLiftTheSwingKneeAndRaiseTheHips()
        {
            PuppetPose contact = PuppetGait.Evaluate(Walking(0f));
            PuppetPose leftPasses = PuppetGait.Evaluate(Walking(0.25f));
            PuppetPose rightPasses = PuppetGait.Evaluate(Walking(0.75f));

            Assert.That(leftPasses.KneeLeft, Is.GreaterThan(leftPasses.KneeRight + 30f), "The left foot swings through.");
            Assert.That(rightPasses.KneeRight, Is.GreaterThan(rightPasses.KneeLeft + 30f), "The right foot swings through.");
            Assert.That(leftPasses.HipLeft, Is.EqualTo(0f).Within(Tolerance), "The thighs pass each other under the hips.");
            Assert.That(rightPasses.HipRight, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(leftPasses.Lift, Is.GreaterThan(contact.Lift + 0.02f), "The hips are highest as the feet pass.");
            Assert.That(rightPasses.Lift, Is.GreaterThan(contact.Lift + 0.02f));
            // The swing foot travels forward: the left from its back contact at 0 to its front contact at 0.5.
            Assert.That(PuppetGait.Evaluate(Walking(0.3f)).HipLeft, Is.GreaterThan(PuppetGait.Evaluate(Walking(0.2f)).HipLeft));
            Assert.That(PuppetGait.Evaluate(Walking(0.8f)).HipRight, Is.GreaterThan(PuppetGait.Evaluate(Walking(0.7f)).HipRight));
            // The stance knee keeps a soft give but never folds like the swing knee.
            Assert.That(leftPasses.KneeRight, Is.EqualTo(PuppetGait.KneeRest + PuppetGait.KneeStance).Within(Tolerance));
        }

        [Test]
        public void StandingStillIsTheRestPoseWithinOneBreath()
        {
            PuppetPose rest = PuppetGait.Rest;
            Assert.That(rest.HipLeft, Is.Zero);
            Assert.That(rest.HipRight, Is.Zero);
            Assert.That(rest.KneeLeft, Is.EqualTo(PuppetGait.KneeRest));
            Assert.That(rest.ShoulderRight, Is.EqualTo(PuppetGait.ShoulderRest));
            Assert.That(rest.ElbowLeft, Is.EqualTo(PuppetGait.ElbowRest));
            Assert.That(rest.Lift, Is.Zero);
            Assert.That(rest.Lean, Is.Zero);
            Assert.That(rest.Fall, Is.Zero);
            Assert.That(rest.Breath, Is.Zero);

            float deepest = 0f;
            for (float time = 0f; time < PuppetGait.BreathPeriod; time += 0.05f)
            {
                PuppetPose idle = PuppetGait.Evaluate(new PuppetGaitInput { IdleTime = time });
                string at = "at " + time + " s";
                Assert.That(idle.HipLeft, Is.EqualTo(rest.HipLeft), at);
                Assert.That(idle.HipRight, Is.EqualTo(rest.HipRight), at);
                Assert.That(idle.KneeLeft, Is.EqualTo(rest.KneeLeft), at);
                Assert.That(idle.KneeRight, Is.EqualTo(rest.KneeRight), at);
                Assert.That(idle.Lift, Is.EqualTo(rest.Lift), at);
                Assert.That(idle.Lean, Is.EqualTo(rest.Lean), at);
                Assert.That(idle.Head, Is.EqualTo(rest.Head), at);
                Assert.That(idle.Breath, Is.InRange(-PuppetGait.BreathLift, PuppetGait.BreathLift), at);
                Assert.That(idle.ShoulderLeft - rest.ShoulderLeft, Is.InRange(-PuppetGait.BreathShoulder, PuppetGait.BreathShoulder), at);
                Assert.That(idle.ShoulderRight - rest.ShoulderRight, Is.InRange(-PuppetGait.BreathShoulder, PuppetGait.BreathShoulder), at);
                Assert.That(idle.Cape - rest.Cape, Is.InRange(-PuppetGait.BreathCape, PuppetGait.BreathCape), at);
                deepest = Math.Max(deepest, Math.Abs(idle.Breath));
            }
            Assert.That(deepest, Is.GreaterThan(0.9f * PuppetGait.BreathLift), "The chest does rise and fall.");

            PuppetGaitInput stride = Walking(0.3f);
            PuppetGaitInput breathing = stride;
            breathing.IdleTime = 0.6f;
            Assert.That(PuppetGait.Evaluate(breathing).Breath, Is.Zero, "A full stride hides the breath.");
            Assert.That(PuppetGait.Evaluate(breathing).ShoulderRight, Is.EqualTo(PuppetGait.Evaluate(stride).ShoulderRight));
        }

        [Test]
        public void AStrikeMovesTheArmsButNeverTheLegs()
        {
            foreach (PuppetStrike strike in Strikes)
            {
                foreach (float phase in new[] { 0f, 0.13f, 0.25f, 0.5f, 0.8f })
                {
                    foreach (float weight in new[] { 0f, 0.5f, 1f })
                    {
                        PuppetGaitInput walking = Walking(phase);
                        walking.WalkWeight = weight;
                        walking.IdleTime = 0.4f;
                        PuppetPose bare = PuppetGait.Evaluate(walking);
                        foreach (float progress in new[] { 0f, 0.1f, 0.3f, 0.6f, 1f })
                        {
                            PuppetGaitInput struck = walking;
                            struck.Strike = strike;
                            struck.StrikeProgress = progress;
                            PuppetPose pose = PuppetGait.Evaluate(struck);
                            string at = $"{strike} at phase {phase}, weight {weight}, progress {progress}";
                            Assert.That(pose.HipLeft, Is.EqualTo(bare.HipLeft), at);
                            Assert.That(pose.HipRight, Is.EqualTo(bare.HipRight), at);
                            Assert.That(pose.KneeLeft, Is.EqualTo(bare.KneeLeft), at);
                            Assert.That(pose.KneeRight, Is.EqualTo(bare.KneeRight), at);
                            Assert.That(pose.Lift, Is.EqualTo(bare.Lift), at);
                            Assert.That(pose.Lean, Is.EqualTo(bare.Lean), at);
                            Assert.That(pose.Head, Is.EqualTo(bare.Head), at);
                            Assert.That(pose.Cape, Is.EqualTo(bare.Cape), at);
                            Assert.That(pose.Breath, Is.EqualTo(bare.Breath), at);
                            Assert.That(pose.Fall, Is.EqualTo(bare.Fall), at);
                        }

                        PuppetGaitInput hit = walking;
                        hit.Strike = strike;
                        PuppetPose atHit = PuppetGait.Evaluate(hit);
                        Assert.That(Math.Abs(atHit.ShoulderRight - bare.ShoulderRight), Is.GreaterThan(10f),
                            $"{strike}: the weapon arm shows the strike at the damage event.");
                        Assert.That(Math.Abs(atHit.WristRight - bare.WristRight), Is.GreaterThan(20f), $"{strike}: the weapon turns.");
                        hit.StrikeProgress = 1f;
                        PuppetPose done = PuppetGait.Evaluate(hit);
                        Assert.That(done.ShoulderRight, Is.EqualTo(bare.ShoulderRight).Within(Tolerance), $"{strike}: back on the walk.");
                        Assert.That(done.ShoulderLeft, Is.EqualTo(bare.ShoulderLeft).Within(Tolerance));
                        Assert.That(done.ElbowRight, Is.EqualTo(bare.ElbowRight).Within(Tolerance));
                        Assert.That(done.WristRight, Is.EqualTo(bare.WristRight).Within(Tolerance));
                    }
                }
            }
        }

        [Test]
        public void TheSwingShowsTheHitAtOnceThenFollowsThroughAndReturns()
        {
            PuppetGaitInput input = Walking(0.1f);
            input.Strike = PuppetStrike.Swing;
            float wristAtHit = PuppetGait.Evaluate(input).WristRight;
            input.StrikeProgress = PuppetGait.StrikeHold * 0.5f;
            float wristFollowing = PuppetGait.Evaluate(input).WristRight;
            input.StrikeProgress = 0.65f;
            float wristReturning = PuppetGait.Evaluate(input).WristRight;
            input.StrikeProgress = 1f;
            float wristDone = PuppetGait.Evaluate(input).WristRight;

            Assert.That(wristFollowing, Is.LessThan(wristAtHit - 10f), "The blade keeps travelling down after the hit.");
            Assert.That(wristReturning, Is.InRange(wristAtHit, wristDone), "Then it eases back onto the walk.");
            Assert.That(wristDone, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void AFlinchBendsTheUpperBodyBackAndTheLegsKeepWalking()
        {
            PuppetGaitInput walking = Walking(0.35f);
            PuppetPose bare = PuppetGait.Evaluate(walking);
            PuppetGaitInput hit = walking;
            hit.Flinch = 1f;
            PuppetPose flinch = PuppetGait.Evaluate(hit);

            Assert.That(flinch.Lean, Is.EqualTo(bare.Lean - PuppetGait.FlinchLean).Within(Tolerance), "The torso snaps back.");
            Assert.That(flinch.Head, Is.LessThan(bare.Head - 5f), "The head further.");
            Assert.That(flinch.ShoulderRight, Is.LessThan(bare.ShoulderRight));
            Assert.That(flinch.HipLeft, Is.EqualTo(bare.HipLeft));
            Assert.That(flinch.KneeRight, Is.EqualTo(bare.KneeRight));
            Assert.That(flinch.Lift, Is.EqualTo(bare.Lift));
            hit.Flinch = 0.5f;
            Assert.That(PuppetGait.Evaluate(hit).Lean, Is.InRange(flinch.Lean, bare.Lean), "It eases back.");
        }

        [Test]
        public void ACollapseKneelsThenToppleBackwardNearlyFlat()
        {
            PuppetGaitInput dying = Walking(0.6f);
            dying.Strike = PuppetStrike.Swing;
            float previousFall = -1f;
            for (float death = 0f; death <= 1.0001f; death += 0.05f)
            {
                dying.DeathProgress = death;
                PuppetPose pose = PuppetGait.Evaluate(dying);
                Assert.That(pose.Fall, Is.GreaterThanOrEqualTo(previousFall), "The figure only ever falls further.");
                previousFall = pose.Fall;
            }

            dying.DeathProgress = 0.3f;
            PuppetPose kneeling = PuppetGait.Evaluate(dying);
            Assert.That(kneeling.KneeLeft, Is.GreaterThan(50f), "The knees give first.");
            Assert.That(kneeling.KneeRight, Is.GreaterThan(50f));
            Assert.That(kneeling.Fall, Is.LessThan(20f), "before the topple has gone far.");
            Assert.That(kneeling.Lift, Is.LessThan(-0.05f), "The hips sink.");

            dying.DeathProgress = 1f;
            PuppetPose lying = PuppetGait.Evaluate(dying);
            Assert.That(lying.Fall, Is.EqualTo(PuppetGait.FallAngle).Within(Tolerance));
            Assert.That(lying.Fall, Is.InRange(75f, 90f), "Nearly flat, never past the floor.");
            Assert.That(lying.WristRight, Is.EqualTo(0f).Within(Tolerance), "The strike lets go.");
            Assert.That(lying.HipLeft, Is.EqualTo(lying.HipRight).Within(Tolerance), "No stride is left.");
        }

        [Test]
        public void EveryPoseStaysInSaneRanges()
        {
            var input = new PuppetGaitInput();
            foreach (PuppetStrike strike in new[] { PuppetStrike.None, PuppetStrike.Swing, PuppetStrike.Cast, PuppetStrike.Thrust })
            foreach (float weight in new[] { 0f, 0.4f, 1f })
            foreach (float progress in new[] { 0f, 0.15f, 0.5f, 1f })
            foreach (float flinch in new[] { 0f, 1f })
            foreach (float death in new[] { 0f, 0.5f, 1f })
            foreach (float idle in new[] { 0f, 0.7f, 1.9f })
            for (float phase = 0f; phase < 1f; phase += 0.05f)
            {
                input.WalkPhase = phase;
                input.WalkWeight = weight;
                input.IdleTime = idle;
                input.Strike = strike;
                input.StrikeProgress = progress;
                input.Flinch = flinch;
                input.DeathProgress = death;
                PuppetPose pose = PuppetGait.Evaluate(input);
                string at = $"{strike} phase {phase} weight {weight} progress {progress} flinch {flinch} death {death} idle {idle}";
                foreach (float angle in new[] { pose.Lean, pose.Head, pose.Cape, pose.HipLeft, pose.HipRight,
                    pose.ShoulderLeft, pose.ShoulderRight, pose.WristLeft, pose.WristRight })
                    Assert.That(angle, Is.InRange(-180f, 180f), at);
                foreach (float flexion in new[] { pose.KneeLeft, pose.KneeRight, pose.ElbowLeft, pose.ElbowRight })
                    Assert.That(flexion, Is.InRange(0f, 150f), at);
                Assert.That(pose.Lift, Is.InRange(-0.2f, 0.05f), at);
                Assert.That(pose.Breath, Is.InRange(-PuppetGait.BreathLift, PuppetGait.BreathLift), at);
                Assert.That(pose.Fall, Is.InRange(0f, 90f), at);
                Assert.That(Math.Abs(pose.HipLeft), Is.LessThanOrEqualTo(Math.Max(PuppetGait.HipSwing, 30f) + Tolerance), at);
            }
        }

        [Test]
        public void TheSameInputAlwaysGivesTheSamePose()
        {
            var input = new PuppetGaitInput
            {
                WalkPhase = 0.37f, WalkWeight = 0.8f, IdleTime = 1.3f, Strike = PuppetStrike.Cast, StrikeProgress = 0.2f,
                Flinch = 0.4f
            };
            PuppetPose first = PuppetGait.Evaluate(input);
            PuppetPose second = PuppetGait.Evaluate(input);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void TheStrideAdvancesWithDistanceAndWraps()
        {
            Assert.That(PuppetGait.Advance(0f, PuppetGait.StrideLength * 0.25f), Is.EqualTo(0.25f).Within(1e-6f));
            Assert.That(PuppetGait.Advance(0.9f, PuppetGait.StrideLength * 0.2f), Is.EqualTo(0.1f).Within(1e-5f));
            Assert.That(PuppetGait.Advance(0.3f, PuppetGait.StrideLength), Is.EqualTo(0.3f).Within(1e-5f), "A whole stride.");
            Assert.That(PuppetGait.Advance(0.3f, 0f), Is.EqualTo(0.3f), "Standing does not step.");
            Assert.That(PuppetGait.Advance(0.3f, -1f), Is.EqualTo(0.3f));
            Assert.That(PuppetGait.Advance(0.3f, float.NaN), Is.EqualTo(0.3f));
            Assert.That(PuppetGait.Advance(0.3f, float.PositiveInfinity), Is.EqualTo(0.3f));
            Assert.That(PuppetGait.Advance(float.NaN, 0.1f), Is.InRange(0f, 1f));
            Assert.That(PuppetGait.Advance(0.5f, 1000.37f), Is.InRange(0f, 1f), "A long jump still lands inside a cycle.");

            // Many small frames and one long frame cover the same floor to the same phase.
            float phase = 0f;
            for (int frame = 0; frame < 120; frame++)
                phase = PuppetGait.Advance(phase, 0.0583f);
            Assert.That(phase, Is.EqualTo(PuppetGait.Advance(0f, 0.0583f * 120)).Within(1e-3f));
        }

        [Test]
        public void TheWalkWeightRampsInAnEighthOfASecondAndHoldsWhileTimeStands()
        {
            float weight = 0f;
            int frames = 0;
            while (weight < 1f && frames < 100)
            {
                weight = PuppetGait.Blend(weight, true, 1f / 60f);
                frames++;
            }
            Assert.That(frames, Is.EqualTo(8), "An eighth of a second at 60 frames a second.");
            Assert.That(PuppetGait.Blend(1f, true, 1f / 60f), Is.EqualTo(1f));
            Assert.That(PuppetGait.Blend(0.5f, false, 0f), Is.EqualTo(0.5f), "A paused combat holds the weight.");
            Assert.That(PuppetGait.Blend(0.5f, true, -1f), Is.EqualTo(0.5f));
            Assert.That(PuppetGait.Blend(0.05f, false, 1f / 60f), Is.Zero);
        }

        [Test]
        public void AStrikeEndsInsideItsWeaponsInterval()
        {
            Assert.That(PuppetGait.StrikeSeconds(1f), Is.EqualTo(PuppetGait.MaxStrikeSeconds));
            Assert.That(PuppetGait.StrikeSeconds(0.25f), Is.LessThan(0.25f), "A fast weapon's arm is back before its next attack.");
            Assert.That(PuppetGait.StrikeSeconds(0.25f), Is.EqualTo(0.25f * PuppetGait.StrikeShareOfInterval).Within(1e-6f));
            Assert.That(PuppetGait.StrikeSeconds(0f), Is.EqualTo(PuppetGait.MaxStrikeSeconds));
            Assert.That(PuppetGait.StrikeSeconds(float.NaN), Is.EqualTo(PuppetGait.MaxStrikeSeconds));
        }

        [Test]
        public void EachBoneTurnsByItsOwnChannel()
        {
            var pose = new PuppetPose
            {
                Fall = 1f, Lift = 0.02f, Lean = 3f, Breath = 0.01f, Head = 4f, Cape = 8f, HipLeft = 5f, HipRight = 6f,
                KneeLeft = 7f, KneeRight = 9f, ShoulderLeft = 10f, ShoulderRight = 11f, ElbowLeft = 12f, ElbowRight = 13f,
                WristLeft = 14f, WristRight = 15f
            };
            // A hanging limb swings forward counter-clockwise; folding a knee, trailing a cape and leaning the upright torso
            // and head forward are clockwise.
            Assert.That(pose.Rotation(PuppetBone.ThighLeft), Is.EqualTo(5f));
            Assert.That(pose.Rotation(PuppetBone.ThighRight), Is.EqualTo(6f));
            Assert.That(pose.Rotation(PuppetBone.ShinLeft), Is.EqualTo(-7f));
            Assert.That(pose.Rotation(PuppetBone.ShinRight), Is.EqualTo(-9f));
            Assert.That(pose.Rotation(PuppetBone.UpperArmLeft), Is.EqualTo(10f));
            Assert.That(pose.Rotation(PuppetBone.UpperArmRight), Is.EqualTo(11f));
            Assert.That(pose.Rotation(PuppetBone.ForearmLeft), Is.EqualTo(12f));
            Assert.That(pose.Rotation(PuppetBone.ForearmRight), Is.EqualTo(13f));
            Assert.That(pose.Rotation(PuppetBone.HandLeft), Is.EqualTo(14f));
            Assert.That(pose.Rotation(PuppetBone.HandRight), Is.EqualTo(15f));
            Assert.That(pose.Rotation(PuppetBone.Torso), Is.EqualTo(-3f));
            Assert.That(pose.Rotation(PuppetBone.Head), Is.EqualTo(-4f));
            Assert.That(pose.Rotation(PuppetBone.Cape), Is.EqualTo(-8f));
            Assert.That(pose.Rotation(PuppetBone.Skirt), Is.EqualTo(-2f));
            Assert.That(pose.Rotation(PuppetBone.Pelvis), Is.Zero, "The pelvis rides the bob; the root carries the fall.");
            Assert.That(pose.Rotation(PuppetBone.Fixed), Is.Zero);
            Assert.That(pose.Rise(PuppetBone.Pelvis), Is.EqualTo(0.02f));
            Assert.That(pose.Rise(PuppetBone.Torso), Is.EqualTo(0.01f));
            Assert.That(pose.Rise(PuppetBone.Head), Is.Zero);
        }
    }
}
