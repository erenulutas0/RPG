using System;
using Cryptforge.Art;
using Cryptforge.Core;
using Cryptforge.Economy;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The development autopilot's drop detour (DropDetourRule), one body for the scene's autopilot and the foundry
    // simulation's copy of it: what a drop adds to the way, which drop each takes, and the walk's budget - which bounds a
    // walk however the drops fall, where the slack alone does not.
    public sealed class DropDetourRuleTests
    {
        private const float Reach = 4f;
        private const float Slack = 2f;
        private const float Frame = 1f / 60f;

        [Test]
        public void ADropAheadAddsNothingToTheWayAndOneBehindTwiceItsDistance()
        {
            Assert.That(DropDetourRule.AddedPath(0f, 0f, 3f, 0f, 10f, 0f), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(DropDetourRule.AddedPath(0f, 0f, -2f, 0f, 10f, 0f), Is.EqualTo(4f).Within(1e-5f));
            Assert.That(DropDetourRule.AddedPath(0f, 0f, 0f, 2f, 10f, 0f),
                Is.EqualTo(2f + (float)Math.Sqrt(104.0) - 10f).Within(1e-5f), "beside: there, and on from there");
            Assert.That(DropDetourRule.AddedPath(0f, 0f, 0f, -2f, 10f, 0f),
                Is.EqualTo(DropDetourRule.AddedPath(0f, 0f, 0f, 2f, 10f, 0f)), "either side alike");
        }

        [Test]
        public void OnTheWayTheNearestDropBesideItIsTakenNotANearerOneBehind()
        {
            var drops = new GroundDrops(new RunState(1000));
            drops.Add(-1.5f, 0f, 1, 0);
            drops.Add(3f, 0f, 1, 0);
            drops.Add(2.5f, .5f, 1, 0);
            drops.Add(4.5f, 0f, 1, 0);

            Assert.That(DropDetourRule.NearestOnTheWay(drops, 0f, 0f, Reach, 10f, 0f, Slack, out float gx, out float gy), Is.True);
            Assert.That((gx, gy), Is.EqualTo((2.5f, .5f)), "the nearer of the two ahead; the one behind adds 3");
            Assert.That(DropDetourRule.Nearest(drops, 0f, 0f, Reach, out gx, out gy), Is.True);
            Assert.That((gx, gy), Is.EqualTo((-1.5f, 0f)), "with nowhere to go, the nearest in reach");

            // Turned round, the way makes the drop behind the one beside it.
            Assert.That(DropDetourRule.NearestOnTheWay(drops, 0f, 0f, Reach, -10f, 0f, Slack, out gx, out gy), Is.True);
            Assert.That((gx, gy), Is.EqualTo((-1.5f, 0f)));
        }

        [Test]
        public void ADropIsBesideTheWayUpToTheSlackAndOutOfReachIsOutOfReach()
        {
            var within = new GroundDrops(new RunState(1000));
            within.Add(0f, 1.8f, 1, 0);
            Assert.That(DropDetourRule.AddedPath(0f, 0f, 0f, 1.8f, 10f, 0f), Is.LessThanOrEqualTo(Slack));
            Assert.That(DropDetourRule.NearestOnTheWay(within, 0f, 0f, Reach, 10f, 0f, Slack, out _, out _), Is.True);

            var beyond = new GroundDrops(new RunState(1000));
            beyond.Add(0f, 1.9f, 1, 0);
            Assert.That(DropDetourRule.AddedPath(0f, 0f, 0f, 1.9f, 10f, 0f), Is.GreaterThan(Slack));
            Assert.That(DropDetourRule.NearestOnTheWay(beyond, 0f, 0f, Reach, 10f, 0f, Slack, out float gx, out float gy), Is.False);
            Assert.That((gx, gy), Is.EqualTo((0f, 0f)), "no drop leaves the goal where the hero stands");

            var far = new GroundDrops(new RunState(1000));
            far.Add(Reach, 0f, 1, 0);
            Assert.That(DropDetourRule.NearestOnTheWay(far, 0f, 0f, Reach, 10f, 0f, Slack, out _, out _), Is.False, "on the way, but not closer than reach");
            Assert.That(DropDetourRule.Nearest(far, 0f, 0f, Reach, out _, out _), Is.False);
            Assert.That(DropDetourRule.Nearest(null, 0f, 0f, Reach, out _, out _), Is.False);
        }

        [Test]
        public void OfEqualDropsTheFirstFallenIsTaken()
        {
            var drops = new GroundDrops(new RunState(1000));
            drops.Add(0f, 2f, 1, 0);
            drops.Add(2f, 0f, 1, 0);
            drops.Add(0f, -2f, 1, 0);
            Assert.That(DropDetourRule.Nearest(drops, 0f, 0f, Reach, out float gx, out float gy), Is.True);
            Assert.That((gx, gy), Is.EqualTo((0f, 2f)));
        }

        // A walk is charged every frame it steers at a drop and chooses none once its budget is spent; a fight, or a hall
        // newly marked, gives the walk after it the whole budget, and a hall that stays marked does not.
        [Test]
        public void AWalkSpendsItsBudgetAndAFightOrANewHallRestoresIt()
        {
            var drops = new GroundDrops(new RunState(1000));
            drops.Add(2f, 0f, 1, 0);
            var walk = new DropDetourRule.Walk(Reach, Slack, 1f);
            walk.Track(false, true);
            for (int i = 0; i < 4; i++)
                Assert.That(walk.Choose(drops, 0f, 0f, 10f, 0f, .25f, out _, out _), Is.True, $"second {i * .25f}");
            Assert.That(walk.Spent, Is.EqualTo(1f));
            Assert.That(walk.Choose(drops, 0f, 0f, 10f, 0f, .25f, out float gx, out float gy), Is.False, "the budget is spent");
            Assert.That((gx, gy), Is.EqualTo((0f, 0f)));

            walk.Track(false, true);
            Assert.That(walk.Choose(drops, 0f, 0f, 10f, 0f, .25f, out _, out _), Is.False, "the same hall, still marked");

            walk.Track(true, false);
            Assert.That(walk.Spent, Is.Zero, "a fight");
            Assert.That(walk.Choose(drops, 0f, 0f, 10f, 0f, .25f, out _, out _), Is.True);

            walk.Track(false, false);
            Assert.That(walk.Spent, Is.EqualTo(.25f), "nothing marked, nothing fought: the count goes on");
            walk.Track(false, true);
            Assert.That(walk.Spent, Is.Zero, "a hall newly marked");
        }

        // The loop the budget ends: each time the hero picks a drop up, the next falls just within the slack on the other
        // side of its way, so it steps from side to side, a little further back each time, and never on. The slack alone
        // holds it there for good; with the budget it spends at most the budget on drops and reaches the hall within the
        // straight walk and twice the budget, since it cannot have lost more ground on the drops than it walked.
        [Test]
        public void HoweverTheDropsFallBesideTheWayAWalkReachesItsHallWithinTheBudget()
        {
            const float budget = 12f;
            const float speed = 3.5f;
            const float hallX = 18f;
            float bounded = Walk(budget, speed, hallX, 120f, out float spent);
            Assert.That(spent, Is.LessThanOrEqualTo(budget + Frame));
            Assert.That(bounded, Is.GreaterThan(budget), "the budget was spent on the drops");
            Assert.That(bounded, Is.LessThanOrEqualTo(2f * budget + (hallX - FoundryMap.RoomSiteRadius) / speed + 2f * Frame));

            Assert.That(Walk(float.PositiveInfinity, speed, hallX, 120f, out _), Is.NaN, "the slack alone: never there");
        }

        // A hero walking straight at its goal at speed, the drops falling one at a time across its way 1.8 floor units
        // out - an added path of 1.89 against 18 units to go, within the slack - and picked up as GroundDrops picks up.
        // The seconds to come within the hall's reach, or NaN by the limit.
        private static float Walk(float budget, float speed, float hallX, float limit, out float spent)
        {
            var drops = new GroundDrops(new RunState(1000));
            var walk = new DropDetourRule.Walk(Reach, Slack, budget);
            float x = 0f, y = 0f;
            int side = 1;
            spent = 0f;
            for (int frame = 1; frame * Frame <= limit; frame++)
            {
                walk.Track(false, true);
                if (drops.Count == 0)
                {
                    float wx = hallX - x, wy = -y;
                    float length = (float)Math.Sqrt(wx * wx + wy * wy);
                    drops.Add(x - wy / length * 1.8f * side, y + wx / length * 1.8f * side, 1, 0);
                    side = -side;
                }
                if (!walk.Choose(drops, x, y, hallX, 0f, Frame, out float gx, out float gy))
                {
                    gx = hallX;
                    gy = 0f;
                }
                float dx = gx - x, dy = gy - y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                float step = Math.Min(distance, speed * Frame);
                if (distance > 0f)
                {
                    x += dx / distance * step;
                    y += dy / distance * step;
                }
                drops.CollectNear(x, y);
                spent = walk.Spent;
                if ((hallX - x) * (hallX - x) + y * y <= FoundryMap.RoomSiteRadius * FoundryMap.RoomSiteRadius)
                    return frame * Frame;
            }
            return float.NaN;
        }
    }
}
