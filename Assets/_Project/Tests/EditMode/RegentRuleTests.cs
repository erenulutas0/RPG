using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Glass Regent's lanes (RegentRule, RegentLanes; docs/50 section 6 and the lead's decisions 2026-11-12), its beat on
    // the Warden's clock (WardenSlamClock.Tune) and the pilot's sidestep across them (FootworkRule.Sidestep): one body each
    // for the scene's WardenSlamService and DevelopmentAutopilot and the foundry simulation. Places are given in the
    // foundry's design plane (FoundryMap.ToFloor turns them onto the floor without changing a distance), in the open court.
    public sealed class RegentRuleTests
    {
        private static void Floor(float u, float v, out float x, out float y) => FoundryMap.ToFloor(u, v, out x, out y);

        // The shipped tuning, pinned here alone: a new measured tuning moves only this test.
        [Test]
        public void TheShippedTuningIsTheMeasuredOne()
        {
            Assert.That(RegentRule.LaneWidth, Is.EqualTo(1.2f));
            Assert.That(RegentRule.LaneLength, Is.EqualTo(10f));
            Assert.That(RegentRule.LaneShare, Is.EqualTo(.1f));
            Assert.That(RegentRule.Every, Is.EqualTo(WardenRule.SlamEvery), "the Warden's beat");
            Assert.That(RegentRule.EveryPhaseTwo, Is.EqualTo(WardenRule.SlamEveryEnraged));
            Assert.That(RegentRule.Telegraph, Is.EqualTo(1.7f), "a telegraph of its own, longer than the Warden's");
            Assert.That(WardenRule.RecoverySeconds, Is.LessThan(RegentRule.EveryPhaseTwo - RegentRule.Telegraph),
                "phase two still leaves the Regent a moment to swing between a blow and the next lane");
            Assert.That(RegentRule.MaxLanes, Is.EqualTo(3), "the Regent and, in section 6's phase two, two copies");
            Assert.That(RegentRule.LanePast, Is.EqualTo(4f));
            Assert.That(RegentRule.LanePast, Is.GreaterThan(FootworkRule.SidestepRing(FootworkRule.SidestepRingCount - 1)),
                "a step along a far lane, as far as the sidestep or the circle's escape walks, stays in it");
        }

        // A lane runs from its body through the mark, LaneLength from the body, half its width either side of its line;
        // behind the body only the body's own spot, and past its far end nothing.
        [Test]
        public void ALaneRunsFromItsBodyThroughTheMarkToItsLength()
        {
            const float half = RegentRule.LaneWidth * .5f, length = RegentRule.LaneLength;
            // The Regent at the origin, the mark 2 units east: the lane runs east 10 units.
            Assert.That(RegentRule.InLane(1f, 0f, 0f, 0f, 2f, 0f, length, half), Is.True, "between the body and the mark");
            Assert.That(RegentRule.InLane(2f, 0f, 0f, 0f, 2f, 0f, length, half), Is.True, "on the mark");
            Assert.That(RegentRule.InLane(9.9f, 0f, 0f, 0f, 2f, 0f, length, half), Is.True, "past the mark, within its length");
            Assert.That(RegentRule.InLane(10.7f, 0f, 0f, 0f, 2f, 0f, length, half), Is.False, "past its far end");
            Assert.That(RegentRule.InLane(5f, half - .01f, 0f, 0f, 2f, 0f, length, half), Is.True, "just inside its edge");
            Assert.That(RegentRule.InLane(5f, half + .01f, 0f, 0f, 2f, 0f, length, half), Is.False, "just outside it");
            Assert.That(RegentRule.InLane(5f, -half - .01f, 0f, 0f, 2f, 0f, length, half), Is.False, "either side");
            Assert.That(RegentRule.InLane(-1f, 0f, 0f, 0f, 2f, 0f, length, half), Is.False, "behind the body");
            // Turned: the mark north-east of the body.
            float d = (float)Math.Sqrt(.5);
            Assert.That(RegentRule.InLane(4f * d, 4f * d, 0f, 0f, 2f * d, 2f * d, length, half), Is.True);
            Assert.That(RegentRule.InLane(4f * d - .5f, 4f * d + .5f, 0f, 0f, 2f * d, 2f * d, length, half), Is.False,
                "0.71 across a diagonal lane");
            // A body on the mark: the lane runs toward the floor's front.
            Assert.That(RegentRule.InLane(1f, -4f, 1f, 1f, 1f, 1f, length, half), Is.True);
            Assert.That(RegentRule.InLane(0f, -5f, 1f, 1f, 1f, 1f, length, half), Is.False);
            RegentRule.LaneDirection(1f, 1f, 1f, 1f, out float dx, out float dy);
            Assert.That((dx, dy), Is.EqualTo((0f, -1f)));
        }

        // A beat's lanes: marked through the hero's spot, one a body, at most MaxLanes; the hero is caught once in any of
        // them; the beat over, none is down.
        [Test]
        public void TheLanesOfABeatCatchTheHeroInAnyOfThem()
        {
            var lanes = new RegentLanes();
            Assert.That(lanes.Count, Is.Zero);
            Assert.That(lanes.Holds(0f, 0f, 0f), Is.False, "no lane before a mark");
            lanes.Mark(0f, 0f);
            Assert.That(lanes.Add(-3f, 0f), Is.True, "the Regent, west of the hero");
            Assert.That(lanes.Holds(2f, 0f, 0f), Is.True, "a step along the lane stays in it");
            Assert.That(lanes.Holds(0f, 1f, 0f), Is.False, "a step across leaves it");
            Assert.That(lanes.Holds(0f, .7f, 0f), Is.False);
            Assert.That(lanes.Holds(0f, .7f, .2f), Is.True, "widened by a margin");
            lanes.Direction(0, out float dx, out float dy);
            Assert.That(dx, Is.EqualTo(1f).Within(1e-6f));
            Assert.That(dy, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(lanes.Add(0f, 3f), Is.True, "a second body, north of the hero");
            Assert.That(lanes.Holds(0f, -2f, 0f), Is.True, "in the second lane");
            Assert.That(lanes.Add(3f, 3f), Is.True);
            Assert.That(lanes.Add(-3f, -3f), Is.False, "at most MaxLanes");
            Assert.That(lanes.Count, Is.EqualTo(RegentRule.MaxLanes));
            Assert.That(lanes.OriginX(1), Is.EqualTo(0f));
            Assert.That(lanes.OriginY(1), Is.EqualTo(3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => lanes.OriginX(3));
            lanes.Clear();
            Assert.That(lanes.Count, Is.Zero);
            Assert.That(lanes.Holds(2f, 0f, 0f), Is.False, "the beat over, nothing is down");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegentLanes(0f, 10f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegentLanes(1.2f, float.NaN));
        }

        // A lane always runs through the hero's spot: LaneLength from its caster where the hero stands within it, and
        // through the mark and LanePast beyond it where the hero stands farther - the fight's first lane, cast while the
        // Regent still walks out of the gate, fell short of a hero 12 units away before 2026-11-15. A past below zero is
        // that lane, for a comparison.
        [Test]
        public void ALaneRunsThroughTheHerosSpotHoweverFarTheHeroStands()
        {
            var lanes = new RegentLanes();
            lanes.Mark(0f, 0f);
            lanes.Add(-3f, 0f);
            Assert.That(lanes.LengthOf(0), Is.EqualTo(RegentRule.LaneLength), "a near hero: the lane's own length");
            Assert.That(lanes.Holds(6.9f, 0f, 0f), Is.True, "7 past the hero, within 10 of the Regent");
            Assert.That(lanes.Holds(7.7f, 0f, 0f), Is.False, "past its end (and its round end's half-width)");

            lanes.Mark(0f, 0f);
            lanes.Add(-12f, 0f);
            Assert.That(lanes.LengthOf(0), Is.EqualTo(12f + RegentRule.LanePast).Within(1e-4f), "a far hero: through it and on");
            Assert.That(lanes.Holds(0f, 0f, 0f), Is.True, "the hero's spot, 12 units from the Regent");
            Assert.That(lanes.Holds(3.2f, 0f, 0f), Is.True, "a step along it stays in it");
            Assert.That(lanes.Holds(RegentRule.LanePast + .7f, 0f, 0f), Is.False, "and it ends LanePast beyond the spot");
            Assert.That(RegentRule.LaneLengthThrough(-12f, 0f, 0f, 0f, RegentRule.LaneLength, RegentRule.LanePast),
                Is.EqualTo(lanes.LengthOf(0)), "one body for the lanes and their look");

            var clipped = new RegentLanes(RegentRule.LaneWidth, RegentRule.LaneLength, -1f);
            clipped.Mark(0f, 0f);
            clipped.Add(-12f, 0f);
            Assert.That(clipped.LengthOf(0), Is.EqualTo(RegentRule.LaneLength));
            Assert.That(clipped.Holds(0f, 0f, 0f), Is.False, "the lane before: it fell short of a far hero");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RegentLanes(1.2f, 10f, float.NaN));
        }

        // The Warden's clock keeps the Regent's beat when tuned to it; tuned back, it is the Warden's.
        [Test]
        public void TheClockKeepsTheBeatItIsTunedTo()
        {
            var clock = new WardenSlamClock();
            Assert.That(clock.Telegraph, Is.EqualTo(WardenRule.Telegraph), "the Warden's by default");
            clock.Tune(5f, 3.5f, 1.5f);
            clock.Reset();
            Assert.That(clock.Advance(5f - 1.5f - .01f, false), Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.Advance(.02f, false), Is.EqualTo(WardenSlamClock.Step.Marked), "a telegraph of 1.5 s before the 5 s blow");
            Assert.That(clock.Advance(.75f, false), Is.EqualTo(WardenSlamClock.Step.None));
            Assert.That(clock.TelegraphProgress, Is.EqualTo(.5f).Within(.02f));
            Assert.That(clock.Advance(.76f, false), Is.EqualTo(WardenSlamClock.Step.Slammed));
            Assert.That(clock.Advance(3.5f - 1.5f - .01f, true), Is.EqualTo(WardenSlamClock.Step.None), "phase two's beat");
            Assert.That(clock.Advance(.02f, true), Is.EqualTo(WardenSlamClock.Step.Marked));
            clock.Tune(RegentRule.Every, RegentRule.EveryPhaseTwo, RegentRule.Telegraph);
            clock.Reset();
            Assert.That(clock.Advance(RegentRule.Every - RegentRule.Telegraph + .01f, false), Is.EqualTo(WardenSlamClock.Step.Marked));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Tune(6f, 4f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Tune(1f, 4f, 1.1f), "a beat shorter than its telegraph");
            Assert.Throws<ArgumentOutOfRangeException>(() => clock.Tune(6f, float.NaN, 1.1f));
        }

        // The sidestep: a pilot on a lane steps across it to the nearest spot clear of it by SidestepMargin - a short step,
        // across rather than along - where the escape out of the Warden's circle runs along the lane and stays in it.
        [Test]
        public void ThePilotSidestepsAcrossALaneWhereTheCircleEscapeRunsAlongIt()
        {
            Floor(4f, -8f, out float x, out float y);
            Floor(1f, -8f, out float rx, out float ry);
            var lanes = new RegentLanes();
            lanes.Mark(x, y);
            lanes.Add(rx, ry);
            Assert.That(lanes.Holds(x, y, 0f), Is.True, "the hero stands on its mark");
            Assert.That(FootworkRule.Sidestep(x, y, lanes, out float gx, out float gy), Is.True);
            Assert.That(lanes.Holds(gx, gy, FootworkRule.SidestepMargin - 1e-4f), Is.False, "clear of the lane by the margin");
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.True);
            float step = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(gx, gy, x, y));
            Assert.That(step, Is.LessThanOrEqualTo(RegentRule.LaneWidth * .5f + FootworkRule.SidestepMargin + .5f),
                "the nearest ring with a clear spot: a step across");
            lanes.Direction(0, out float dx, out float dy);
            float along = Math.Abs((gx - x) * dx + (gy - y) * dy);
            float across = Math.Abs(-(gx - x) * dy + (gy - y) * dx);
            Assert.That(across, Is.GreaterThan(along), "across the lane, not along it");

            // Clear of the lanes, no step: the pilot fights on.
            Assert.That(FootworkRule.Sidestep(gx, gy, lanes, out float hx, out float hy), Is.False);
            Assert.That((hx, hy), Is.EqualTo((gx, gy)));
            Assert.That(FootworkRule.Sidestep(x, y, null, out _, out _), Is.False, "nothing down");

            // The Warden's escape, away from the body that marked: along the lane, and still in it.
            float reach = WardenRule.SlamRadius + .6f;
            Assert.That(FootworkRule.EscapeMark(x, y, x, y, reach + .4f, rx, ry, false, out float ex, out float ey), Is.True);
            Assert.That(lanes.Holds(ex, ey, 0f), Is.True, "the circle's escape runs down the lane");
        }

        // Rings and bearings: the sidestep reads the nearest ring that has a clear spot.
        [Test]
        public void TheSidestepsRingsGrowOutward()
        {
            Assert.That(FootworkRule.SidestepRingCount, Is.GreaterThan(1));
            for (int r = 1; r < FootworkRule.SidestepRingCount; r++)
                Assert.That(FootworkRule.SidestepRing(r), Is.GreaterThan(FootworkRule.SidestepRing(r - 1)));
            Assert.That(FootworkRule.SidestepRing(0), Is.LessThan(RegentRule.LaneWidth * .5f + FootworkRule.SidestepMargin),
                "the first ring cannot clear a lane the hero stands centred on");
            Assert.That(FootworkRule.SidestepRing(FootworkRule.SidestepRingCount - 1), Is.GreaterThan(RegentRule.LaneWidth + FootworkRule.SidestepMargin));
            Assert.That(FootworkRule.SidestepBearings, Is.GreaterThanOrEqualTo(8));
        }
    }
}
