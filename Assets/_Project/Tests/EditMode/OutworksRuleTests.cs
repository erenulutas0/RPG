using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/62's P0a, the Outworks probe's pure rules (OutworksRule, AnvilField, OutworksPhase, HeroLeap, OutworksPilot): the
    // sites stand on the court and clear of each other, an anvil captures after its charge and drains outside its ring, the
    // Keeper's key and the Bell open the stair and the stair is held standing still, a pad launches only a hero that stood
    // still on its centre for its beat - never one walking across it - and its arc carries the hero across the pit on a
    // straight ground track and lands it where no pad launches again, the pads stand off the walks round the pit, and the
    // pilot's choices.
    public sealed class OutworksRuleTests
    {
        private const float Step = 1f / 60f;

        private static void Floor(float u, float v, out float x, out float y) => FoundryMap.ToFloor(u, v, out x, out y);

        // ---- The sites ------------------------------------------------------------------------------------------------

        [Test]
        public void EverySiteStandsOnTheCourtWithTheHerosClearance()
        {
            for (int i = 0; i < OutworksRule.AnvilCount; i++)
            {
                OutworksRule.AnvilSite(i, out float x, out float y);
                Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.True, "anvil " + i);
            }
            for (int i = 0; i < OutworksRule.PadCount; i++)
            {
                OutworksRule.PadLaunch(i, out float x, out float y);
                Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.True, "pad launch " + i);
                OutworksRule.PadLanding(i, out x, out y);
                Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.True, "pad landing " + i);
            }
            for (int i = 0; i < OutworksRule.KeeperWaypointCount; i++)
            {
                OutworksRule.KeeperWaypoint(i, out float x, out float y);
                Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.True, "keeper waypoint " + i);
            }
            OutworksRule.StairSpot(out float sx, out float sy);
            Assert.That(FoundryMap.Contains(sx, sy, HeroMotion.EdgeMargin + OutworksRule.StairRadius), Is.True, "the stair");
        }

        [Test]
        public void TheAnvilsStandApartFromEachOtherTheHallsTheAltarsAndTheStair()
        {
            for (int i = 0; i < OutworksRule.AnvilCount; i++)
            {
                OutworksRule.AnvilSite(i, out float x, out float y);
                for (int j = 0; j < i; j++)
                {
                    OutworksRule.AnvilSite(j, out float ox, out float oy);
                    Assert.That(OutworksRule.Within(x, y, ox, oy, 12f), Is.False, $"anvils {j} and {i} at least 12 apart");
                }
                for (int a = 0; a < FoundryMap.AltarCount; a++)
                {
                    FoundryMap.AltarSite(a, out float ax, out float ay);
                    Assert.That(OutworksRule.Within(x, y, ax, ay, OutworksRule.AnvilRadius + AltarRule.Radius), Is.False,
                        $"anvil {i} clear of altar {a}");
                }
                for (int hall = 1; hall <= FoundryMap.RoomSiteCount; hall++)
                {
                    FoundryMap.RoomSite(0, hall, out float hx, out float hy);
                    Assert.That(OutworksRule.Within(x, y, hx, hy, OutworksRule.AnvilRadius + FoundryMap.RoomSiteRadius), Is.False,
                        $"anvil {i} clear of district {hall}");
                }
                OutworksRule.StairSpot(out float sx, out float sy);
                Assert.That(OutworksRule.Within(x, y, sx, sy, OutworksRule.AnvilRadius + OutworksRule.StairRadius), Is.False,
                    $"anvil {i} clear of the stair");
            }
        }

        // The line a walk round the pit takes: its edges with the pit's corners' relief (OutworksRule.PitCorner's 1.2).
        private const float RimWalk = 1.2f;

        [Test]
        public void EveryPadStandsOffTheWalksRoundThePit()
        {
            for (int pad = 0; pad < OutworksRule.PadCount; pad++)
            {
                OutworksRule.PadLaunch(pad, out float x, out float y);
                FoundryMap.ToDesign(x, y, out float u, out float v);
                // How far the pad's centre stands from the pit's box (0 inside it).
                float du = Math.Max(Math.Max(FoundryMap.PitWest - u, u - FoundryMap.PitEast), 0f);
                float dv = Math.Max(Math.Max(FoundryMap.PitSouth - v, v - FoundryMap.PitNorth), 0f);
                float off = (float)Math.Sqrt(du * du + dv * dv);
                Assert.That(off - RimWalk, Is.GreaterThan(OutworksRule.PadRadius + .8f),
                    $"pad {pad}'s ring clear of the line round the pit (it stands {off:0.0} off the pit)");
                for (int hall = 1; hall <= FoundryMap.RoomSiteCount; hall++)
                {
                    FoundryMap.RoomSite(0, hall, out float hx, out float hy);
                    Assert.That(OutworksRule.Within(x, y, hx, hy, OutworksRule.PadRadius + FoundryMap.RoomSiteRadius), Is.False,
                        $"pad {pad} clear of district {hall}");
                }
                for (int a = 0; a < OutworksRule.AnvilCount; a++)
                {
                    OutworksRule.AnvilSite(a, out float ax, out float ay);
                    Assert.That(OutworksRule.Within(x, y, ax, ay, OutworksRule.PadRadius + OutworksRule.AnvilRadius), Is.False,
                        $"pad {pad} clear of anvil {a}");
                }
            }
            Assert.That(OutworksRule.PadTriggerRadius, Is.LessThanOrEqualTo(OutworksRule.PadRadius * .5f),
                "the launch is the pad's centre, not its ring");
        }

        [Test]
        public void EachPadLeapsThePitAndLandsOutsideEveryPadsCircle()
        {
            for (int pad = 0; pad < OutworksRule.PadCount; pad++)
            {
                OutworksRule.PadLaunch(pad, out float x, out float y);
                OutworksRule.PadLanding(pad, out float tx, out float ty);
                Assert.That(FoundryMap.Segment(x, y, tx, ty, .1f), Is.False, $"pad {pad}'s straight way crosses the pit");
                for (int other = 0; other < OutworksRule.PadCount; other++)
                {
                    OutworksRule.PadLaunch(other, out float ox, out float oy);
                    Assert.That(OutworksRule.Within(tx, ty, ox, oy, OutworksRule.PadRadius + .5f), Is.False,
                        $"pad {pad}'s landing clear of pad {other}");
                    // No landing faces the way back: the pair's other pad stands well off it.
                    Assert.That(OutworksRule.Within(tx, ty, ox, oy, 5f), Is.False, $"pad {pad}'s landing 5 or more from pad {other}");
                }
            }
        }

        [Test]
        public void TheKeeperWalksOnAtTheWaypointFarthestFromTheHero()
        {
            int start = OutworksRule.KeeperStart(0f, 0f);
            OutworksRule.KeeperWaypoint(start, out float x, out float y);
            for (int i = 0; i < OutworksRule.KeeperWaypointCount; i++)
            {
                OutworksRule.KeeperWaypoint(i, out float ox, out float oy);
                Assert.That(x * x + y * y, Is.GreaterThanOrEqualTo(ox * ox + oy * oy), "waypoint " + i);
            }
        }

        // ---- The anvils -----------------------------------------------------------------------------------------------

        [Test]
        public void StandingInARingCapturesItAfterItsChargeOnce()
        {
            var field = new AnvilField();
            float x = field.XOf(1), y = field.YOf(1);
            int captured = -1, frames = 0;
            while (captured < 0 && frames < 1000)
            {
                captured = field.Step(x, y, Step);
                frames++;
            }
            Assert.That(captured, Is.EqualTo(1));
            Assert.That(frames * Step, Is.EqualTo(OutworksRule.AnvilChargeSeconds).Within(Step * 1.5f));
            Assert.That(field.IsCaptured(1), Is.True);
            Assert.That(field.Captured, Is.EqualTo(1));
            Assert.That(field.Standing, Is.EqualTo(-1), "a captured anvil holds no one");
            for (int i = 0; i < 120; i++)
                Assert.That(field.Step(x, y, Step), Is.EqualTo(-1), "it answers once");
        }

        [Test]
        public void MovingInsideTheRingStillChargesAndLeavingItDrainsAtHalfTheRate()
        {
            var field = new AnvilField();
            float x = field.XOf(0), y = field.YOf(0);
            float inside = OutworksRule.AnvilRadius * .8f;
            for (int i = 0; i < 120; i++)
            {
                float a = i * .2f;
                field.Step(x + (float)Math.Cos(a) * inside, y + (float)Math.Sin(a) * inside, Step);
            }
            Assert.That(field.ChargeOf(0), Is.EqualTo(120 * Step).Within(1e-3f), "circling inside the ring charges");
            Assert.That(field.Standing, Is.EqualTo(0));
            float charged = field.ChargeOf(0);
            for (int i = 0; i < 60; i++)
                field.Step(x + OutworksRule.AnvilRadius + 1f, y, Step);
            Assert.That(field.ChargeOf(0), Is.EqualTo(charged - 60 * Step * OutworksRule.AnvilDecayShare).Within(1e-3f));
            Assert.That(field.Standing, Is.EqualTo(-1));
            for (int i = 0; i < 600; i++)
                field.Step(x + OutworksRule.AnvilRadius + 1f, y, Step);
            Assert.That(field.ChargeOf(0), Is.EqualTo(0f), "it drains to nothing, never below");
        }

        // ---- The key, the Bell and the stair ---------------------------------------------------------------------------

        [Test]
        public void TheStairOpensWithTheKeepersKeyAndIsHeldStandingStill()
        {
            var phase = new OutworksPhase();
            Assert.That(phase.StairOpen, Is.False);
            Assert.That(phase.StepStair(2f, phase.StairX, phase.StairY, false), Is.False, "a sealed stair is not held");
            while (!phase.KeeperDue)
                phase.Tick(Step);
            Assert.That(phase.Seconds, Is.EqualTo(OutworksRule.KeeperSpawnSeconds).Within(Step * 1.5f));
            phase.KeeperArrived(OutworksRule.KeeperStart(0f, 0f));
            Assert.That(phase.Keeper, Is.EqualTo(KeeperState.Patrolling));
            phase.KeeperFell();
            Assert.That(phase.Keeper, Is.EqualTo(KeeperState.Fallen));
            Assert.That(phase.KeyHeld, Is.True);
            Assert.That(phase.StairOpen, Is.True, "the key opens the stair before the Bell");
            Assert.That(phase.BellRung, Is.False);
            // Moving on it, or standing off it, holds nothing.
            Assert.That(phase.StepStair(2f, phase.StairX, phase.StairY, true), Is.False);
            Assert.That(phase.StepStair(2f, phase.StairX + OutworksRule.StairRadius + .1f, phase.StairY, false), Is.False);
            float held = 0f;
            while (!phase.StepStair(Step, phase.StairX + .5f, phase.StairY, false))
                held += Step;
            Assert.That(held + Step, Is.EqualTo(ExitRule.DwellSeconds).Within(Step * 1.5f));
            Assert.That(phase.Left, Is.True);
            Assert.That(phase.Tick(OutworksRule.BellSeconds), Is.False, "a phase left by the key never rings the Bell");
        }

        [Test]
        public void TheBellOpensTheStairForEveryoneAndTheRoamingKeeperLeaves()
        {
            var phase = new OutworksPhase();
            phase.Tick(OutworksRule.KeeperSpawnSeconds);
            phase.KeeperArrived(0);
            bool rang = false;
            while (!rang)
                rang = phase.Tick(Step);
            Assert.That(phase.Seconds, Is.EqualTo(OutworksRule.BellSeconds).Within(Step * 1.5f));
            Assert.That(phase.BellRung, Is.True);
            Assert.That(phase.StairOpen, Is.True);
            Assert.That(phase.KeyHeld, Is.False);
            Assert.That(phase.Keeper, Is.EqualTo(KeeperState.Withdrawn));
            Assert.That(phase.Tick(Step), Is.False, "it rings once");
            phase.KeeperFell();
            Assert.That(phase.KeyHeld, Is.False, "a Keeper gone from the court drops no key");
        }

        [Test]
        public void AKeeperDueAfterTheBellNeverWalksOn()
        {
            var phase = new OutworksPhase(30f);
            phase.Tick(OutworksRule.KeeperSpawnSeconds + 1f);
            Assert.That(phase.BellRung, Is.True);
            Assert.That(phase.KeeperDue, Is.False);
        }

        [Test]
        public void TheKeeperPatrolsItsLoopAndTurnsWithinItsAggroRadiusOrWhenStruck()
        {
            var phase = new OutworksPhase();
            phase.Tick(OutworksRule.KeeperSpawnSeconds);
            phase.KeeperArrived(0);
            OutworksRule.KeeperWaypoint(0, out float wx, out float wy);
            float farX = wx + OutworksRule.KeeperAggroRadius + 10f;
            // At its waypoint, far from the hero: on to the next one.
            Assert.That(phase.KeeperGoal(wx, wy, farX, wy, false, out float gx, out float gy), Is.True);
            OutworksRule.KeeperWaypoint(1, out float nx, out float ny);
            Assert.That(gx, Is.EqualTo(nx).Within(1e-5f));
            Assert.That(gy, Is.EqualTo(ny).Within(1e-5f));
            Assert.That(phase.Keeper, Is.EqualTo(KeeperState.Patrolling));
            // Struck from afar: it turns on the hero.
            Assert.That(phase.KeeperGoal(wx, wy, farX, wy, true, out gx, out gy), Is.True);
            Assert.That(phase.Keeper, Is.EqualTo(KeeperState.Hunting));
            Assert.That(gx, Is.EqualTo(farX));

            var near = new OutworksPhase();
            near.Tick(OutworksRule.KeeperSpawnSeconds);
            near.KeeperArrived(2);
            OutworksRule.KeeperWaypoint(2, out float kx, out float ky);
            near.KeeperGoal(kx, ky, kx + OutworksRule.KeeperAggroRadius * .9f, ky, false, out _, out _);
            Assert.That(near.Keeper, Is.EqualTo(KeeperState.Hunting), "within its aggro radius it turns");
        }

        [Test]
        public void TheHordeFollowsThePhasesClock()
        {
            Assert.That(OutworksRule.HordeStateAt(0f), Is.EqualTo(HordeState.Ramp));
            Assert.That(OutworksRule.HordeStateAt(OutworksRule.HordeFrom), Is.EqualTo(HordeState.Horde));
            Assert.That(OutworksRule.HordeStateAt(OutworksRule.PressureFrom), Is.EqualTo(HordeState.Pressure));
            Assert.That(OutworksRule.HordeStateAt(OutworksRule.BellSeconds + 60f), Is.EqualTo(HordeState.Pressure));
        }

        [Test]
        public void TheBellsLadderNamesTheFloorsHordeRoles()
        {
            int[] sizes = OutworksRule.BellSizes();
            Assert.That(sizes.Length, Is.EqualTo(OutworksRule.BellWaveCount));
            for (int wave = 0; wave < sizes.Length; wave++)
            {
                Assert.That(sizes[wave], Is.InRange(1, PackLayout.MaxFormationSize));
                for (int i = 0; i < sizes[wave]; i++)
                {
                    Assert.That(OutworksRule.BellRole(wave, i, 3), Is.InRange(0, 2));
                    Assert.That(OutworksRule.BellRole(wave, i, 1), Is.EqualTo(0), "a one-role horde fields its one role");
                }
            }
        }

        // ---- The pad's launch and arc ---------------------------------------------------------------------------------

        // The hero standing still at (x, y) step after step until the pad launches it, or a second's steps; the steps it
        // took (the first only places the feet).
        private static int StandUntilLaunched(HeroLeap leap, float x, float y)
        {
            for (int frame = 1; frame <= 60; frame++)
                if (leap.TryLaunch(x, y, Step))
                    return frame;
            return -1;
        }

        [Test]
        public void APadLaunchesOnlyAHeroThatStoodStillOnItsCentreForItsBeat()
        {
            OutworksRule.PadLaunch(0, out float lx, out float ly);
            var leap = new HeroLeap();
            // On the ring but off the centre: never, however long it stands.
            float ring = (OutworksRule.PadTriggerRadius + OutworksRule.PadRadius) * .5f;
            Assert.That(StandUntilLaunched(leap, lx + ring, ly), Is.EqualTo(-1), "the ring is not the launch");
            Assert.That(leap.DwellPad, Is.EqualTo(-1));
            // On the centre, still: after the beat, to the step.
            int frames = StandUntilLaunched(leap, lx + OutworksRule.PadTriggerRadius * .5f, ly);
            Assert.That(leap.IsAirborne, Is.True, "stood still on the centre, it launches");
            Assert.That(frames - 1, Is.EqualTo((int)Math.Round(OutworksRule.PadDwellSeconds / Step)),
                "the beat, counted from the first still step");
            Assert.That(leap.Pad, Is.EqualTo(0));
            Assert.That(leap.Launches, Is.EqualTo(1));
        }

        [Test]
        public void WalkingAcrossAPadsCentreNeverLaunchesTheHero()
        {
            OutworksRule.PadLaunch(1, out float lx, out float ly);
            var leap = new HeroLeap();
            // Straight over the centre at every pace down to a tenth of the foundry's walk (3.5 units a second): each
            // step moves the feet, so the beat never starts.
            foreach (float pace in new[] { 3.5f, 1.4f, .35f })
            {
                float stride = pace * Step;
                for (float t = -1.5f; t <= 1.5f; t += stride)
                {
                    Assert.That(leap.TryLaunch(lx + t, ly + t * .3f, Step), Is.False, $"walking at {pace} across it");
                    if (Math.Abs(t) < OutworksRule.PadTriggerRadius * .5f)
                        Assert.That(leap.DwellPad, Is.EqualTo(1), "on its centre");
                }
            }
            Assert.That(leap.Launches, Is.EqualTo(0));
            // A pause on the centre short of the beat, then on: nothing either.
            for (int frame = 0; frame < (int)(OutworksRule.PadDwellSeconds / Step) - 2; frame++)
                Assert.That(leap.TryLaunch(lx, ly, Step), Is.False);
            Assert.That(leap.DwellShare, Is.GreaterThan(0f).And.LessThan(1f), "the beat fills while it stands");
            Assert.That(leap.TryLaunch(lx + .05f, ly, Step), Is.False, "a step starts the beat again");
            Assert.That(leap.DwellShare, Is.EqualTo(0f));
            Assert.That(leap.Launches, Is.EqualTo(0));
        }

        [Test]
        public void APadCarriesTheHeroAlongAStraightGroundTrackAndLandsIt()
        {
            OutworksRule.PadLaunch(0, out float lx, out float ly);
            OutworksRule.PadLanding(0, out float tx, out float ty);
            var motion = new HeroMotion(3.5f);
            motion.Place(lx + .2f, ly, ArenaGeometry.Foundry);
            var leap = new HeroLeap();
            Assert.That(StandUntilLaunched(leap, motion.X, motion.Y), Is.GreaterThan(0), "stood on the pad's centre it launches");
            Assert.That(leap.IsAirborne, Is.True);
            Assert.That(leap.Pad, Is.EqualTo(0));
            float peak = 0f;
            bool overThePit = false;
            int frames = 0;
            while (leap.IsAirborne && frames < 1000)
            {
                Assert.That(leap.Move(motion, Step), Is.True);
                frames++;
                peak = Math.Max(peak, leap.Lift);
                // On the straight line from the launch to the landing.
                float cross = (motion.X - lx) * (ty - ly) - (motion.Y - ly) * (tx - lx);
                Assert.That(Math.Abs(cross), Is.LessThan(1e-2f), "frame " + frames);
                overThePit |= !FoundryMap.Contains(motion.X, motion.Y, 0f);
            }
            Assert.That(frames * Step, Is.EqualTo(OutworksRule.PadSeconds).Within(Step * 1.5f));
            Assert.That(overThePit, Is.True, "the arc crosses the pit");
            Assert.That(motion.X, Is.EqualTo(tx).Within(1e-5f));
            Assert.That(motion.Y, Is.EqualTo(ty).Within(1e-5f));
            Assert.That(peak, Is.EqualTo(OutworksRule.PadHeight).Within(.02f), "the sprite peaks at the pad's height");
            Assert.That(leap.Lift, Is.EqualTo(0f), "landed, it stands on its ground");
            Assert.That(StandUntilLaunched(leap, motion.X, motion.Y), Is.EqualTo(-1), "a landing launches nothing");
            Assert.That(leap.Launches, Is.EqualTo(1));
        }

        [Test]
        public void APadLaunchesAgainOnlyOnceTheHeroHasSteppedOffIt()
        {
            OutworksRule.PadLaunch(2, out float lx, out float ly);
            var motion = new HeroMotion(3.5f);
            var leap = new HeroLeap();
            Assert.That(StandUntilLaunched(leap, lx, ly), Is.GreaterThan(0));
            leap.Land(motion);
            Assert.That(leap.IsAirborne, Is.False);
            Assert.That(StandUntilLaunched(leap, lx, ly), Is.EqualTo(-1), "still on the pad it left");
            Assert.That(leap.TryLaunch(lx + OutworksRule.PadRadius + 1f, ly, Step), Is.False);
            Assert.That(StandUntilLaunched(leap, lx, ly), Is.GreaterThan(0), "stepped off and back on, it launches");
            Assert.That(OutworksRule.ArcLift(0f), Is.EqualTo(0f));
            Assert.That(OutworksRule.ArcLift(1f), Is.EqualTo(0f));
            Assert.That(OutworksRule.ArcLift(.5f), Is.EqualTo(OutworksRule.PadHeight).Within(1e-5f));
        }

        [Test]
        public void APadTakesTheWalkAcrossThePitOnlyWhereItSavesIt()
        {
            // From the court's east, east of the pit, to the workyard's west: across the pit by the east pad.
            Floor(-1f, 8f, out float x, out float y);
            Floor(-26f, 8f, out float gx, out float gy);
            int pad = OutworksRule.PadShortcut(x, y, gx, gy);
            Assert.That(pad, Is.EqualTo(1), "the east-to-west pad");
            // A walk that does not cross the pit takes no pad.
            Floor(4f, -6f, out float ex, out float ey);
            Floor(10f, -12f, out float fx, out float fy);
            Assert.That(OutworksRule.PadShortcut(ex, ey, fx, fy), Is.EqualTo(-1));
            Assert.That(OutworksRule.WalkEstimate(x, y, gx, gy),
                Is.GreaterThan((float)Math.Sqrt((gx - x) * (gx - x) + (gy - y) * (gy - y))), "round the pit is longer");
        }

        // ---- The pilot ------------------------------------------------------------------------------------------------

        [Test]
        public void ThePilotGoesForTheNearestAnvilThenLeavesWithTheKeyOnceDone()
        {
            var phase = new OutworksPhase();
            OutworksPilot.Habits careful = OutworksPilot.Of(AutopilotCohort.Careful);
            OutworksPilot.Habits firstCard = OutworksPilot.Of(AutopilotCohort.FirstCard);
            float x = phase.Anvils.XOf(2) + 1f, y = phase.Anvils.YOf(2);
            Assert.That(OutworksPilot.Choose(phase, careful, x, y, false, 0f, 0f, null, out float gx, out float gy, out _),
                Is.EqualTo(OutworksPilot.Objective.Anvil));
            Assert.That(gx, Is.EqualTo(phase.Anvils.XOf(2)));
            Assert.That(gy, Is.EqualTo(phase.Anvils.YOf(2)));
            Assert.That(OutworksPilot.Leaving(phase, careful, x, y, null), Is.False, "the stair is sealed");
            phase.Tick(OutworksRule.KeeperSpawnSeconds);
            phase.KeeperArrived(0);
            phase.KeeperFell();
            Assert.That(OutworksPilot.Leaving(phase, careful, x, y, null), Is.False, "the careful pilot finishes its anvils");
            Assert.That(OutworksPilot.Leaving(phase, firstCard, x, y, null), Is.True, "the first-card pilot beelines");
            var noticed = new bool[phase.Anvils.Count];
            Assert.That(OutworksPilot.Leaving(phase, careful, x, y, noticed), Is.True, "with no anvil noticed it is done");
            Assert.That(OutworksPilot.Choose(phase, careful, x, y, false, 0f, 0f, noticed, out _, out _, out _),
                Is.EqualTo(OutworksPilot.Objective.None));
        }

        [Test]
        public void ThePilotsHabitsFollowTheirCohorts()
        {
            Assert.That(OutworksPilot.Of(AutopilotCohort.Careful).UsesPads, Is.True);
            Assert.That(OutworksPilot.Of(AutopilotCohort.Careful).HuntsKeeper, Is.True);
            Assert.That(OutworksPilot.Of(AutopilotCohort.Standing).HuntsKeeper, Is.False, "standing captures and stands");
            Assert.That(OutworksPilot.Of(AutopilotCohort.Clumsy).UsesPads, Is.False, "clumsy never takes a pad");
            Assert.That(float.IsPositiveInfinity(OutworksPilot.Of(AutopilotCohort.Clumsy).AnvilReach), Is.False,
                "clumsy misses the far anvils");
            Assert.That(OutworksPilot.Of(AutopilotCohort.AutoFarmer).HuntsKeeper, Is.False);
        }

        [Test]
        public void TheKeeperIsHuntedWhenNearOrWhenTheAnvilsAreDone()
        {
            var phase = new OutworksPhase();
            OutworksPilot.Habits careful = OutworksPilot.Of(AutopilotCohort.Careful);
            float x = phase.Anvils.XOf(0), y = phase.Anvils.YOf(0);
            Assert.That(OutworksPilot.Choose(phase, careful, x, y, true, x + 30f, y, null, out _, out _, out _),
                Is.EqualTo(OutworksPilot.Objective.Anvil), "a far Keeper waits for the anvils");
            Assert.That(OutworksPilot.Choose(phase, careful, x, y, true, x + 3f, y, null, out float gx, out _, out _),
                Is.EqualTo(OutworksPilot.Objective.Keeper), "a near one is hunted");
            Assert.That(gx, Is.EqualTo(x + 3f));
            Assert.That(OutworksPilot.Choose(phase, OutworksPilot.Of(AutopilotCohort.Standing), x, y, true, x + 3f, y, null,
                out _, out _, out _), Is.EqualTo(OutworksPilot.Objective.Anvil), "the standing pilot never hunts");
        }
    }
}
