using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The cut-out puppet's pose math (docs/45 section 4 B): the stride, the idle, the strike with its draw and
    // follow-through, the ability's slam, the flinch and the collapse, without Unity.
    public sealed class PuppetGaitTests
    {
        private const float Tolerance = 1e-3f;

        private static readonly PuppetStrike[] Strikes = { PuppetStrike.Swing, PuppetStrike.Cast, PuppetStrike.Thrust };

        private static PuppetGaitInput Walking(float phase) => new PuppetGaitInput { WalkPhase = phase, WalkWeight = 1f };

        // The Vanguard's legs as its art set paints them, from behind and from the front: the near leg longer on the
        // screen than the far one, whose boot stands sixteen texels up the screen.
        internal static readonly PuppetLegs Vanguard = new PuppetLegs
        {
            Right = new PuppetLeg { HipX = 0.0417f, HipY = -0.1679f, KneeX = -0.0049f, KneeY = -0.1042f, HeelX = -0.0613f, HeelY = -0.4375f, ToeX = 0.152f, ToeY = -0.3824f },
            Left = new PuppetLeg { HipX = -0.2512f, HipY = -0.1434f, KneeX = -0.0453f, KneeY = -0.1324f, HeelX = -0.0821f, HeelY = -0.3088f, ToeX = -0.0061f, ToeY = -0.3027f }
        };
        internal static readonly PuppetLegs VanguardFront = new PuppetLegs
        {
            Right = new PuppetLeg { HipX = -0.1287f, HipY = -0.174f, KneeX = -0.027f, KneeY = -0.1446f, HeelX = -0.0993f, HeelY = -0.4301f, ToeX = 0.0184f, ToeY = -0.4252f },
            Left = new PuppetLeg { HipX = 0.1667f, HipY = -0.1679f, KneeX = 0.0208f, KneeY = -0.1225f, HeelX = -0.0931f, HeelY = -0.3051f, ToeX = 0.1385f, ToeY = -0.3297f }
        };

        // The weapon's screen angle relative to the angle it is drawn at: every joint from the shoulder down turns it, and
        // the torso's lean (clockwise) carries the shoulder.
        private static float Blade(PuppetPose pose) => pose.ShoulderRight + pose.ElbowRight + pose.WristRight - pose.Lean;

        // How far the weapon arm's joints have moved between two poses, in degrees all told.
        private static float Arm(PuppetPose pose, PuppetPose from) =>
            Math.Abs(pose.ShoulderRight - from.ShoulderRight) + Math.Abs(pose.ElbowRight - from.ElbowRight)
            + Math.Abs(pose.WristRight - from.WristRight);

        [Test]
        public void TheTwoContactsMirrorEachOther()
        {
            PuppetPose a = PuppetGait.Evaluate(Walking(0f));
            PuppetPose b = PuppetGait.Evaluate(Walking(0.5f));

            Assert.That(a.HipRight, Is.GreaterThan(15f), "Contact A: the right foot lands ahead.");
            Assert.That(a.HipLeft, Is.LessThan(0f), "while the left, just off the floor, swings through behind.");
            Assert.That(a.RightPlanted && !a.LeftPlanted, Is.True, "The right boot takes the weight.");
            Assert.That(b.LeftPlanted && !b.RightPlanted, Is.True, "and half a cycle on the left.");
            Assert.That(b.HipLeft, Is.EqualTo(a.HipRight).Within(Tolerance));
            Assert.That(b.HipRight, Is.EqualTo(a.HipLeft).Within(Tolerance));
            Assert.That(b.KneeLeft, Is.EqualTo(a.KneeRight).Within(Tolerance));
            Assert.That(b.KneeRight, Is.EqualTo(a.KneeLeft).Within(Tolerance));
            Assert.That(b.LegRiseLeft, Is.EqualTo(a.LegRiseRight).Within(Tolerance));
            Assert.That(b.LegRiseRight, Is.EqualTo(a.LegRiseLeft).Within(Tolerance));
            Assert.That(b.ShoulderLeft, Is.EqualTo(a.ShoulderRight).Within(Tolerance));
            Assert.That(b.ShoulderRight, Is.EqualTo(a.ShoulderLeft).Within(Tolerance));
            Assert.That(b.ElbowLeft, Is.EqualTo(a.ElbowRight).Within(Tolerance));
            Assert.That(b.ElbowRight, Is.EqualTo(a.ElbowLeft).Within(Tolerance));
            Assert.That(b.WristLeft, Is.EqualTo(a.WristRight).Within(Tolerance));
            Assert.That(b.Lift, Is.EqualTo(a.Lift).Within(Tolerance));
            Assert.That(b.Lean, Is.EqualTo(a.Lean).Within(Tolerance));
            Assert.That(b.Head, Is.EqualTo(a.Head).Within(Tolerance));
            Assert.That(b.Cape, Is.EqualTo(a.Cape).Within(Tolerance));
            Assert.That(b.Skirt, Is.EqualTo(a.Skirt).Within(Tolerance));
            Assert.That(b.Twist, Is.EqualTo(-a.Twist).Within(Tolerance), "The shoulders turn the other way each step.");
            Assert.That(b.HipTwist, Is.EqualTo(-a.HipTwist).Within(Tolerance));

            Assert.That(a.ShoulderRight, Is.LessThan(a.ShoulderLeft), "Each arm swings against its own side's leg.");
            Assert.That(a.Twist, Is.LessThan(0f), "The right shoulder turns back as the right foot lands.");
            Assert.That(a.HipTwist, Is.GreaterThan(0f), "while the right hip turns forward with its leg.");
            Assert.That(a.Lean, Is.GreaterThan(0f), "A walking figure leans into its travel.");
        }

        // A run (docs/45 section 4 B): each boot is planted for its stance, sweeping back with the floor, then both are
        // off the floor for a flight while the one behind folds and swings through.
        [Test]
        public void EachBootSweepsBackPlantedThroughItsStanceThenFoldsAndSwingsThrough()
        {
            float previous = float.MaxValue, stanceKnee = 0f, pushOff = -1f;
            for (float phase = 0f; phase < 0.5f; phase += 0.005f)
            {
                PuppetPose pose = PuppetGait.Evaluate(Walking(phase));
                if (!pose.RightPlanted)
                {
                    pushOff = phase;
                    break;
                }
                Assert.That(pose.LeftPlanted, Is.False, "One boot down at a time, at " + phase);
                // The knee gives as the weight comes on, so the short thigh may tip forward a degree; the shin, which
                // carries the boot, only ever sweeps back.
                float shin = pose.HipRight - pose.KneeRight;
                Assert.That(shin, Is.LessThan(previous + 1e-3f), "The planted shin only sweeps back, at " + phase);
                previous = shin;
                stanceKnee = Math.Max(stanceKnee, pose.KneeRight);
            }
            Assert.That(pushOff, Is.InRange(0.2f, 0.4f), "A run's stance: about a third of the cycle.");
            Assert.That(stanceKnee, Is.InRange(20f, 40f), "The stance knee gives under the weight.");
            PuppetPose flight = PuppetGait.Evaluate(Walking((pushOff + 0.5f) * 0.5f));
            Assert.That(flight.RightPlanted || flight.LeftPlanted, Is.False, "Then both boots are off the floor.");
            PuppetPose swinging = PuppetGait.Evaluate(Walking(0.65f));
            Assert.That(swinging.KneeRight, Is.GreaterThan(stanceKnee + 5f), "The swing knee folds deeper than the stance's.");
            Assert.That(swinging.KneeRight, Is.LessThan(45f), "but no further than a cut-out shin reads as a lifted foot.");
            Assert.That(PuppetGait.Evaluate(Walking(0.85f)).HipRight, Is.GreaterThan(PuppetGait.Evaluate(Walking(0.6f)).HipRight),
                "and the thigh swings through to the next strike.");
        }

        // The run's bob rides the hips - lowest as a planted leg takes the weight, highest in the flight - and whatever the
        // hips do, a planted boot stands exactly on its floor line and a swinging one stays above it: rebuilt here from
        // the painted legs through PuppetGait.Sole, side on.
        [Test]
        public void ThePlantedBootStandsOnItsFloorAndTheBobReadsAsWeight()
        {
            foreach (PuppetLegs legs in new[] { Vanguard, VanguardFront })
            {
                float lowest = float.MaxValue, highest = float.MinValue;
                for (float phase = 0f; phase < 1f; phase += 0.01f)
                {
                    PuppetGaitInput input = Walking(phase);
                    input.Legs = legs;
                    PuppetPose pose = PuppetGait.Evaluate(input);
                    string at = "at phase " + phase;
                    float right = Height(pose, legs.Right, true) - pose.FloorRight;
                    float left = Height(pose, legs.Left, false) - pose.FloorLeft;
                    if (pose.RightPlanted)
                        Assert.That(right, Is.EqualTo(0f).Within(0.002f), at + ": the right boot stands on its floor.");
                    else
                        Assert.That(right, Is.GreaterThan(-0.004f), at + ": the swinging right boot clears it.");
                    if (pose.LeftPlanted)
                        Assert.That(left, Is.EqualTo(0f).Within(0.002f), at + ": the left boot stands on its floor.");
                    else
                        Assert.That(left, Is.GreaterThan(-0.004f), at + ": the swinging left boot clears it.");
                    Assert.That(Math.Max(right, left), Is.LessThan(0.15f), at + ": the other stays low.");
                    lowest = Math.Min(lowest, pose.Lift);
                    highest = Math.Max(highest, pose.Lift);
                }
                Assert.That(highest - lowest, Is.InRange(0.008f, 0.05f), "A bob that reads as weight, not a hop.");
                PuppetGaitInput rest = default;
                rest.Legs = legs;
                Assert.That(PuppetGait.Evaluate(rest).Lift, Is.EqualTo(0f).Within(1e-6f), "Standing, nothing drops.");
            }
        }

        // A boot's lowest point over the floor line it was painted on.
        internal static float Height(PuppetPose pose, PuppetLeg leg, bool right)
        {
            PuppetGait.Sole(pose, leg, right, out _, out float heel, out _, out float toe);
            return Math.Min(heel, toe) - PuppetGait.FloorLine(leg);
        }

        [Test]
        public void StandingStillOnlyBreathesAndShiftsItsWeight()
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
            Assert.That(rest.Shift, Is.Zero);
            Assert.That(rest.Wield, Is.Zero);

            float deepest = 0f, widest = 0f;
            for (float time = 0f; time < PuppetGait.IdleCycle; time += 0.05f)
            {
                PuppetPose idle = PuppetGait.Evaluate(new PuppetGaitInput { IdleTime = time });
                string at = "at " + time + " s";
                Assert.That(Math.Abs(idle.HipLeft), Is.LessThan(1.5f), at + ": the legs only lean to keep the boots planted.");
                Assert.That(idle.HipLeft, Is.EqualTo(idle.HipRight), at);
                Assert.That(idle.KneeLeft, Is.InRange(PuppetGait.KneeRest, PuppetGait.KneeRest + PuppetGait.WeightKnee), at);
                Assert.That(idle.KneeRight, Is.InRange(PuppetGait.KneeRest, PuppetGait.KneeRest + PuppetGait.WeightKnee), at);
                Assert.That(Math.Abs(idle.Lift), Is.LessThan(0.004f), at);
                Assert.That(idle.Lean, Is.Zero, at);
                Assert.That(idle.Breath, Is.InRange(-PuppetGait.BreathLift, PuppetGait.BreathLift), at);
                Assert.That(idle.Shift, Is.InRange(-PuppetGait.WeightShift, PuppetGait.WeightShift), at);
                Assert.That(idle.ShoulderLeft - rest.ShoulderLeft, Is.InRange(-PuppetGait.BreathShoulder, PuppetGait.BreathShoulder), at);
                Assert.That(idle.ShoulderRight - rest.ShoulderRight, Is.InRange(-PuppetGait.BreathShoulder, PuppetGait.BreathShoulder), at);
                Assert.That(idle.Cape - rest.Cape, Is.InRange(-PuppetGait.BreathCape, PuppetGait.BreathCape), at);
                deepest = Math.Max(deepest, Math.Abs(idle.Breath));
                widest = Math.Max(widest, Math.Abs(idle.Shift));
            }
            Assert.That(deepest, Is.GreaterThan(0.9f * PuppetGait.BreathLift), "The chest does rise and fall.");
            Assert.That(widest, Is.GreaterThan(0.9f * PuppetGait.WeightShift), "The weight does roll from foot to foot.");

            PuppetGaitInput stride = Walking(0.3f);
            PuppetGaitInput breathing = stride;
            breathing.IdleTime = 0.6f;
            Assert.That(PuppetGait.Evaluate(breathing).Breath, Is.Zero, "A full stride hides the breath.");
            Assert.That(PuppetGait.Evaluate(breathing).Shift, Is.Zero, "and the weight shift.");
            Assert.That(PuppetGait.Evaluate(breathing).ShoulderRight, Is.EqualTo(PuppetGait.Evaluate(stride).ShoulderRight));
        }

        [Test]
        public void AStrikeAndItsDrawMoveTheArmsAndChestButNeverTheLegs()
        {
            foreach (bool front in new[] { false, true })
            foreach (PuppetStrike strike in Strikes)
            {
                foreach (float phase in new[] { 0f, 0.13f, 0.25f, 0.5f, 0.8f })
                {
                    foreach (float weight in new[] { 0f, 0.5f, 1f })
                    {
                        PuppetGaitInput walking = Walking(phase);
                        walking.WalkWeight = weight;
                        walking.IdleTime = 0.4f;
                        walking.Front = front;
                        walking.Heading = front ? -0.7f : 0.7f;
                        PuppetPose bare = PuppetGait.Evaluate(walking);
                        foreach (float progress in new[] { 0f, 0.1f, 0.3f, 0.6f, 1f })
                        {
                            PuppetGaitInput struck = walking;
                            struck.Strike = strike;
                            struck.StrikeProgress = progress;
                            struck.Windup = strike;
                            struck.WindupProgress = 1f - progress;
                            PuppetPose pose = PuppetGait.Evaluate(struck);
                            string at = $"{strike} from the {(front ? "front" : "rear")} at phase {phase}, weight {weight}, progress {progress}";
                            Assert.That(pose.HipLeft, Is.EqualTo(bare.HipLeft), at);
                            Assert.That(pose.HipRight, Is.EqualTo(bare.HipRight), at);
                            Assert.That(pose.KneeLeft, Is.EqualTo(bare.KneeLeft), at);
                            Assert.That(pose.KneeRight, Is.EqualTo(bare.KneeRight), at);
                            Assert.That(pose.LegRiseLeft, Is.EqualTo(bare.LegRiseLeft), at);
                            Assert.That(pose.LegRiseRight, Is.EqualTo(bare.LegRiseRight), at);
                            Assert.That(pose.Lift, Is.EqualTo(bare.Lift), at);
                            Assert.That(pose.Shift, Is.EqualTo(bare.Shift), at);
                            Assert.That(pose.HipTwist, Is.EqualTo(bare.HipTwist), at);
                            Assert.That(pose.Cape, Is.EqualTo(bare.Cape), at);
                            Assert.That(pose.Skirt, Is.EqualTo(bare.Skirt), at);
                            Assert.That(pose.Breath, Is.EqualTo(bare.Breath), at);
                            Assert.That(pose.Fall, Is.EqualTo(bare.Fall), at);
                        }

                        PuppetGaitInput drawn = walking;
                        drawn.Windup = strike;
                        drawn.WindupProgress = 1f;
                        PuppetPose top = PuppetGait.Evaluate(drawn);
                        Assert.That(Arm(top, bare), Is.GreaterThan(15f), $"{strike}: the weapon is drawn back.");
                        Assert.That(top.Wield, Is.EqualTo(1f), $"{strike}: and held as drawn.");

                        PuppetGaitInput hit = walking;
                        hit.Strike = strike;
                        PuppetPose atHit = PuppetGait.Evaluate(hit);
                        Assert.That(Arm(atHit, bare), Is.GreaterThan(15f),
                            $"{strike}: the weapon arm shows the strike at the damage event.");
                        // A blade turns through its blow; the staff is thrust nearly upright, its head tipped at the target.
                        hit.StrikeProgress = PuppetGait.HitAt;
                        if (strike != PuppetStrike.Cast)
                            Assert.That(Math.Abs(Blade(PuppetGait.Evaluate(hit)) - Blade(bare)), Is.GreaterThan(20f),
                                $"{strike}: the blade has turned through the blow a frame later.");
                        hit.StrikeProgress = 1f;
                        PuppetPose done = PuppetGait.Evaluate(hit);
                        Assert.That(done.ShoulderRight, Is.EqualTo(bare.ShoulderRight).Within(Tolerance), $"{strike}: back on the walk.");
                        Assert.That(done.ShoulderLeft, Is.EqualTo(bare.ShoulderLeft).Within(Tolerance));
                        Assert.That(done.ElbowRight, Is.EqualTo(bare.ElbowRight).Within(Tolerance));
                        Assert.That(done.WristRight, Is.EqualTo(bare.WristRight).Within(Tolerance));
                        Assert.That(done.Lean, Is.EqualTo(bare.Lean).Within(Tolerance));
                        Assert.That(done.Twist, Is.EqualTo(bare.Twist).Within(Tolerance));
                        Assert.That(done.Wield, Is.EqualTo(0f).Within(Tolerance), $"{strike}: and carried again.");
                    }
                }
            }
        }

        [Test]
        public void TheSwordIsDrawnBackThenChoppedThroughTheHitAndFollowedThrough()
        {
            PuppetGaitInput input = Walking(0.1f);
            float carried = Blade(PuppetGait.Evaluate(input));
            input.Windup = PuppetStrike.Swing;
            input.WindupProgress = 1f;
            float drawn = Blade(PuppetGait.Evaluate(input));
            input.Windup = PuppetStrike.None;
            input.WindupProgress = 0f;
            input.Strike = PuppetStrike.Swing;
            float atEvent = Blade(PuppetGait.Evaluate(input));
            input.StrikeProgress = PuppetGait.HitAt;
            float hit = Blade(PuppetGait.Evaluate(input));
            input.StrikeProgress = PuppetGait.StrikeHold;
            float followed = Blade(PuppetGait.Evaluate(input));
            input.StrikeProgress = 0.7f;
            float returning = Blade(PuppetGait.Evaluate(input));
            input.StrikeProgress = 1f;
            float done = Blade(PuppetGait.Evaluate(input));

            Assert.That(drawn, Is.GreaterThan(carried + 60f), "Drawn back over the shoulder before the hit.");
            Assert.That(atEvent, Is.InRange(hit + 20f, drawn), "At the damage event the blade is halfway down.");
            Assert.That(hit, Is.LessThan(carried - 30f), "It chops forward and down through the target.");
            Assert.That(followed, Is.LessThan(hit), "and keeps travelling after it.");
            Assert.That(returning, Is.InRange(followed, done), "Then it eases back onto the walk.");
            Assert.That(done, Is.EqualTo(carried).Within(Tolerance));
        }

        // Seen from the front the near arm hangs behind the body's centre: the draw goes back and up behind the near
        // shoulder, never forward across the helmet as the rear's does, the blow still chops down through the target, and
        // the shield braces at the side instead of swinging out from it.
        [Test]
        public void SeenFromTheFrontTheSwordIsDrawnBackBehindTheShoulderAndTheShieldStaysBraced()
        {
            foreach (float phase in new[] { 0.1f, 0.4f, 0.7f })
            {
                PuppetGaitInput input = Walking(phase);
                input.Front = true;
                PuppetPose bare = PuppetGait.Evaluate(input);
                float carried = Blade(bare);
                for (float progress = 0.05f; progress <= 1.0001f; progress += 0.05f)
                {
                    input.Windup = PuppetStrike.Swing;
                    input.WindupProgress = progress;
                    PuppetPose drawing = PuppetGait.Evaluate(input);
                    string at = $"phase {phase}, draw {progress}";
                    Assert.That(drawing.ShoulderRight, Is.LessThan(bare.ShoulderRight + 1e-3f), at + ": the arm swings back, never forward.");
                    // Partway, the shield is still on its way from the walk's swing to its brace; never further out.
                    Assert.That(drawing.ShoulderLeft, Is.InRange(Math.Min(bare.ShoulderLeft, -5f), Math.Max(bare.ShoulderLeft, 20f)), at);
                    Assert.That(drawing.ElbowLeft, Is.LessThanOrEqualTo(Math.Max(bare.ElbowLeft, 30f)), at);
                }
                PuppetPose top = PuppetGait.Evaluate(input);
                Assert.That(top.ShoulderRight, Is.LessThan(-120f), "Drawn up behind the near shoulder.");
                Assert.That(top.ShoulderLeft, Is.InRange(-5f, 20f), "The shield braces at the side.");
                Assert.That(top.ElbowLeft, Is.LessThanOrEqualTo(30f));
                input.Windup = PuppetStrike.None;
                input.WindupProgress = 0f;
                input.Strike = PuppetStrike.Swing;
                for (float progress = 0f; progress <= PuppetGait.StrikeHold; progress += 0.02f)
                {
                    input.StrikeProgress = progress;
                    PuppetPose blow = PuppetGait.Evaluate(input);
                    Assert.That(blow.ShoulderRight, Is.InRange(20f, 105f), $"The blow crosses the chest, under the helmet, at {progress}.");
                    Assert.That(blow.ShoulderLeft, Is.InRange(-5f, 20f), "and the shield stays braced through it.");
                }
                input.StrikeProgress = PuppetGait.HitAt;
                Assert.That(Blade(PuppetGait.Evaluate(input)), Is.LessThan(carried - 60f), "It chops down through the target.");
                input.StrikeProgress = 1f;
                Assert.That(Blade(PuppetGait.Evaluate(input)), Is.EqualTo(carried).Within(Tolerance), "and is carried again.");
            }

            // The rear's draw is the one that goes forward and up over the helmet, as before.
            PuppetGaitInput rear = Walking(0.1f);
            rear.Windup = PuppetStrike.Swing;
            rear.WindupProgress = 1f;
            Assert.That(PuppetGait.Evaluate(rear).ShoulderRight, Is.GreaterThan(100f));
        }

        [Test]
        public void TheDaggersJabWithTheLeadingHandAndCockTheOther()
        {
            var right = new PuppetGaitInput { Strike = PuppetStrike.Thrust, StrikeProgress = 0.15f };
            var left = right;
            left.LeftLeads = true;
            PuppetPose a = PuppetGait.Evaluate(right);
            PuppetPose b = PuppetGait.Evaluate(left);
            Assert.That(a.ShoulderRight, Is.GreaterThan(a.ShoulderLeft + 60f), "The right blade jabs, the left stays cocked.");
            Assert.That(b.ShoulderLeft, Is.GreaterThan(b.ShoulderRight + 60f), "The next strike leads with the left.");
            Assert.That(b.Twist, Is.EqualTo(-a.Twist).Within(Tolerance), "The chest turns behind each.");
        }

        // The Forge Burst's shockwave leaves on the press's frame, so the slam plays no wind-up: the impact is on screen
        // within a frame or two, then the recoil, then the rise, with the weapon kept in front rather than raised again.
        [Test]
        public void TheForgeBurstDropsIntoItsImpactAtOnceThenRisesOverTheStride()
        {
            var standing = new PuppetGaitInput { Casting = true };
            float twoFrames = 2f / 60f / PuppetGait.CastSeconds;
            standing.CastProgress = twoFrames;
            PuppetPose impact = PuppetGait.Evaluate(standing);
            Assert.That(impact.Lift, Is.LessThan(-0.03f), "Two frames after the press the body has dropped into the slam.");
            Assert.That(Blade(impact), Is.LessThan(Blade(PuppetGait.Rest) - 60f), "with the weapon driven point-down.");
            Assert.That(PuppetGait.ImpactAt * PuppetGait.CastSeconds, Is.LessThan(1.5f / 60f), "The impact pose itself, inside a frame and a half.");
            standing.CastProgress = PuppetGait.ImpactAt;
            float impactArm = PuppetGait.Evaluate(standing).ShoulderRight;
            float deepest = 0f, deepestAt = 0f, highestArm = float.MinValue;
            bool raisedBeforeImpact = false;
            for (float progress = 0f; progress <= 1f; progress += 0.005f)
            {
                standing.CastProgress = progress;
                PuppetPose pose = PuppetGait.Evaluate(standing);
                if (pose.Lift < deepest)
                {
                    deepest = pose.Lift;
                    deepestAt = progress;
                }
                highestArm = Math.Max(highestArm, pose.ShoulderRight);
                if (progress <= PuppetGait.ImpactAt && pose.ShoulderRight > impactArm + 1e-3f)
                    raisedBeforeImpact = true;
            }
            Assert.That(raisedBeforeImpact, Is.False, "No raise before the blow: the arms go straight to it.");
            Assert.That(deepestAt * PuppetGait.CastSeconds, Is.LessThan(0.1f), "The crouch is deepest in the impact's recoil.");
            Assert.That(highestArm, Is.LessThan(90f), "The rise keeps the weapon in front, never back over the helmet.");
            standing.CastProgress = 0.6f;
            Assert.That(PuppetGait.Evaluate(standing).Lift, Is.GreaterThan(deepest + 0.025f), "Then the body rises.");
            standing.CastProgress = 1f;
            PuppetPose after = PuppetGait.Evaluate(standing);
            PuppetPose rest = PuppetGait.Rest;
            Assert.That(after.ShoulderRight, Is.EqualTo(rest.ShoulderRight).Within(Tolerance), "Then it stands again.");
            Assert.That(after.KneeLeft, Is.EqualTo(rest.KneeLeft).Within(Tolerance));
            Assert.That(after.Lift, Is.EqualTo(rest.Lift).Within(Tolerance));

            // On the move the legs keep striding through the slam, and the hips still sink into it.
            PuppetGaitInput a = Walking(0.1f), b = Walking(0.35f);
            a.Casting = b.Casting = true;
            a.CastProgress = b.CastProgress = 0.15f;
            Assert.That(Math.Abs(PuppetGait.Evaluate(a).HipLeft - PuppetGait.Evaluate(b).HipLeft), Is.GreaterThan(10f));
            Assert.That(PuppetGait.Evaluate(a).Lift, Is.LessThan(PuppetGait.Evaluate(Walking(0.1f)).Lift - 0.02f));
        }

        [Test]
        public void AFlinchSnapsTheUpperBodyBackReboundsAndTheLegsKeepWalking()
        {
            PuppetGaitInput walking = Walking(0.35f);
            PuppetPose bare = PuppetGait.Evaluate(walking);
            PuppetGaitInput hit = walking;
            hit.Flinch = 1f;
            PuppetPose flinch = PuppetGait.Evaluate(hit);

            Assert.That(flinch.Lean, Is.EqualTo(bare.Lean - PuppetGait.FlinchLean).Within(Tolerance), "The torso snaps back.");
            Assert.That(flinch.Head, Is.LessThan(bare.Head - 5f), "The head further.");
            Assert.That(flinch.ShoulderRight, Is.LessThan(bare.ShoulderRight));
            Assert.That(flinch.Shift, Is.LessThan(bare.Shift), "The hips jolt back.");
            Assert.That(flinch.HipLeft, Is.EqualTo(bare.HipLeft));
            Assert.That(flinch.KneeRight, Is.EqualTo(bare.KneeRight));
            Assert.That(flinch.Lift, Is.EqualTo(bare.Lift));
            float forward = float.MinValue;
            for (float left = 1f; left >= 0f; left -= 0.02f)
            {
                hit.Flinch = left;
                forward = Math.Max(forward, PuppetGait.Evaluate(hit).Lean - bare.Lean);
            }
            Assert.That(forward, Is.InRange(0.2f, 0.25f * PuppetGait.FlinchLean), "It rebounds a little past upright.");
            hit.Flinch = 0f;
            Assert.That(PuppetGait.Evaluate(hit).Lean, Is.EqualTo(bare.Lean).Within(Tolerance), "and settles.");
        }

        [Test]
        public void ACollapseKneelsThenFallsBackwardFasterAndFasterToNearlyFlat()
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
            Assert.That(kneeling.Fall, Is.LessThan(20f), "before the fall has gone far.");
            Assert.That(kneeling.Lift, Is.LessThan(-0.05f), "The hips sink.");

            dying.DeathProgress = 0.45f;
            float early = PuppetGait.Evaluate(dying).Fall;
            dying.DeathProgress = 0.6f;
            float middle = PuppetGait.Evaluate(dying).Fall;
            dying.DeathProgress = 0.75f;
            float late = PuppetGait.Evaluate(dying).Fall;
            Assert.That(late - middle, Is.GreaterThan(middle - early), "Gravity: the fall gathers speed.");

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
            var input = new PuppetGaitInput { Legs = Vanguard };
            foreach (PuppetStrike strike in new[] { PuppetStrike.None, PuppetStrike.Swing, PuppetStrike.Cast, PuppetStrike.Thrust })
            foreach (float weight in new[] { 0f, 0.4f, 1f })
            foreach (float progress in new[] { 0f, 0.15f, 0.5f, 1f })
            foreach (bool casting in new[] { false, true })
            foreach (float flinch in new[] { 0f, 1f })
            foreach (float death in new[] { 0f, 0.5f, 1f })
            foreach (float surge in new[] { -1f, 0f, 1.2f })
            for (float phase = 0f; phase < 1f; phase += 0.05f)
            {
                input.WalkPhase = phase;
                input.WalkWeight = weight;
                input.IdleTime = phase * PuppetGait.IdleCycle;
                input.Strike = strike;
                input.StrikeProgress = progress;
                input.Windup = strike;
                input.WindupProgress = 1f - progress;
                input.LeftLeads = phase > 0.5f;
                input.Front = phase > 0.3f && phase < 0.8f;
                input.Heading = (phase * 7f % 1f - 0.5f) * 3.4f;
                input.Legs = phase > 0.5f ? VanguardFront : Vanguard;
                input.Casting = casting;
                input.CastProgress = progress;
                input.Flinch = flinch;
                input.DeathProgress = death;
                input.Surge = surge;
                input.Drape = -surge;
                PuppetPose pose = PuppetGait.Evaluate(input);
                string at = $"{strike} phase {phase} weight {weight} progress {progress} cast {casting} flinch {flinch} death {death} surge {surge} heading {input.Heading}";
                foreach (float angle in new[] { pose.Lean, pose.Head, pose.Cape, pose.Skirt, pose.HipLeft, pose.HipRight,
                    pose.ShoulderLeft, pose.ShoulderRight, pose.WristLeft, pose.WristRight })
                    Assert.That(angle, Is.InRange(-180f, 180f), at);
                foreach (float flexion in new[] { pose.KneeLeft, pose.KneeRight, pose.ElbowLeft, pose.ElbowRight })
                    Assert.That(flexion, Is.InRange(0f, 150f), at);
                Assert.That(pose.Lift, Is.InRange(-0.2f, 0.05f), at);
                Assert.That(pose.Breath, Is.InRange(-PuppetGait.BreathLift, PuppetGait.BreathLift), at);
                Assert.That(pose.Fall, Is.InRange(0f, 90f), at);
                Assert.That(Math.Abs(pose.Shift), Is.LessThan(0.05f), at);
                Assert.That(Math.Abs(pose.Twist), Is.LessThan(0.08f), at);
                Assert.That(pose.Wield, Is.InRange(0f, 1f), at);
                Assert.That(Math.Abs(pose.HipLeft), Is.LessThanOrEqualTo(60f), at);
                Assert.That(Math.Abs(pose.HipRight), Is.LessThanOrEqualTo(60f), at);
                foreach (float step in new[] { pose.LegRiseLeft, pose.LegRiseRight, pose.FloorLeft, pose.FloorRight })
                    Assert.That(Math.Abs(step), Is.LessThan(0.2f), at + ": a leg steps a texel or two, never out of the skirt.");
                Assert.That(Math.Abs(pose.LegShiftLeft) + Math.Abs(pose.LegShiftRight), Is.LessThan(0.15f), at);
            }
        }

        [Test]
        public void TheSameInputAlwaysGivesTheSamePose()
        {
            var input = new PuppetGaitInput
            {
                WalkPhase = 0.37f, WalkWeight = 0.8f, IdleTime = 1.3f, Strike = PuppetStrike.Cast, StrikeProgress = 0.2f,
                Windup = PuppetStrike.Cast, WindupProgress = 0.4f, Casting = true, CastProgress = 0.3f, Flinch = 0.4f,
                Surge = 0.3f, Drape = -0.2f, Legs = Vanguard, Heading = 0.7f, Front = true
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
        public void AStrikeAndTheDrawBeforeTheNextFitInsideItsWeaponsInterval()
        {
            Assert.That(PuppetGait.StrikeSeconds(1f), Is.EqualTo(PuppetGait.MaxStrikeSeconds));
            Assert.That(PuppetGait.StrikeSeconds(0.25f), Is.LessThan(0.25f), "A fast weapon's arm is back before its next attack.");
            Assert.That(PuppetGait.StrikeSeconds(0.25f), Is.EqualTo(0.25f * PuppetGait.StrikeShareOfInterval).Within(1e-6f));
            Assert.That(PuppetGait.StrikeSeconds(0f), Is.EqualTo(PuppetGait.MaxStrikeSeconds));
            Assert.That(PuppetGait.StrikeSeconds(float.NaN), Is.EqualTo(PuppetGait.MaxStrikeSeconds));
            Assert.That(PuppetGait.WindupSeconds(2f), Is.EqualTo(PuppetGait.MaxWindupSeconds));
            Assert.That(PuppetGait.WindupSeconds(float.PositiveInfinity), Is.EqualTo(PuppetGait.MaxWindupSeconds));
            foreach (float interval in new[] { 0.2f, 0.3f, 0.5f, 0.8f, 1.1f, 2f })
                Assert.That(PuppetGait.StrikeSeconds(interval) + PuppetGait.WindupSeconds(interval), Is.LessThan(interval),
                    "Strike, rest, draw, strike: every " + interval + " s.");
        }

        [Test]
        public void EachBoneTurnsAndMovesByItsOwnChannel()
        {
            var pose = new PuppetPose
            {
                Fall = 1f, Lift = 0.02f, Shift = 0.03f, Lean = 3f, Twist = 0.04f, HipTwist = 0.05f, Breath = 0.01f, Head = 4f,
                Cape = 8f, Skirt = 2f, HipLeft = 5f, HipRight = 6f, KneeLeft = 7f, KneeRight = 9f, ShoulderLeft = 10f,
                ShoulderRight = 11f, ElbowLeft = 12f, ElbowRight = 13f, WristLeft = 14f, WristRight = 15f,
                LegRiseRight = 0.06f, LegRiseLeft = 0.07f, LegShiftRight = 0.01f, LegShiftLeft = 0.02f
            };
            // A hanging limb swings forward counter-clockwise; folding a knee, trailing a cape or a skirt and leaning the
            // upright torso and head forward are clockwise.
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
            Assert.That(pose.Rise(PuppetBone.ThighRight), Is.EqualTo(0.06f), "Each leg steps up or down the screen on its own.");
            Assert.That(pose.Rise(PuppetBone.ThighLeft), Is.EqualTo(0.07f));
            Assert.That(pose.Forward(PuppetBone.Pelvis), Is.EqualTo(0.03f), "The weight shift moves the whole body.");
            Assert.That(pose.Forward(PuppetBone.UpperArmRight), Is.EqualTo(0.04f), "The near shoulder turns forward...");
            Assert.That(pose.Forward(PuppetBone.UpperArmLeft), Is.EqualTo(-0.04f), "...as the far one turns back.");
            Assert.That(pose.Forward(PuppetBone.ThighRight), Is.EqualTo(0.06f).Within(1e-6f), "The hips' turn and the leg's own shift.");
            Assert.That(pose.Forward(PuppetBone.ThighLeft), Is.EqualTo(-0.03f).Within(1e-6f));
            Assert.That(pose.Forward(PuppetBone.Torso), Is.Zero);
            Assert.That(pose.Forward(PuppetBone.Fixed), Is.Zero);
        }
    }
}
