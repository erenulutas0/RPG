using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Mirror Sentry's guard (MirrorGuardRule, MirrorGuardState; docs/50 section 5 and the lead's decisions 2026-11-12,
    // `sentry-t45-h125-c2-f50`): the mirror turns toward the hero at 45 degrees a second; a landing made while the hero
    // stands inside its 120-degree front arc deals half to it and reflects a fifth of the landing to the hero, the
    // reflection capped at 2 % of the hero's maximum a second for each Sentry; a flank or the rear takes the landing whole
    // and reflects nothing. Pure, one body for the scene's MirrorGuard and the foundry simulation.
    public sealed class MirrorGuardRuleTests
    {
        private const float Degree = (float)Math.PI / 180f;
        private const float Frame = 1f / 60f;
        // The behaviour is read on round 2's guard - half a front landing taken, a fifth reflected, capped at 2 % a second -
        // whatever the shipped tuning (TheGuardIsTheLeadsTuning holds that).
        private const float Front = .5f;
        private const float Share = .2f;
        private const float Cap = .02f;

        private static MirrorGuardState Tuned() => new MirrorGuardState(45f, 120f, Front, Share, Cap);

        // A Sentry at the origin, stepped once with the hero due east of it: it faces east, its budget full.
        private static MirrorGuardState FacingEast(float heroMaximum = 100f)
        {
            var guard = Tuned();
            guard.Step(Frame, 0f, 0f, 3f, 0f, heroMaximum);
            return guard;
        }

        // Where the hero stands at bearing degrees from the Sentry, at distance.
        private static void At(float degrees, float distance, out float x, out float y)
        {
            x = distance * (float)Math.Cos(degrees * Degree);
            y = distance * (float)Math.Sin(degrees * Degree);
        }

        [Test]
        public void TheGuardIsTheLeadsTuning()
        {
            Assert.That(MirrorGuardRule.TurnDegrees, Is.EqualTo(45f));
            Assert.That(MirrorGuardRule.ArcDegrees, Is.EqualTo(120f));
            Assert.That(MirrorGuardRule.FrontShare, Is.EqualTo(.5f));
            Assert.That(MirrorGuardRule.ReflectShare, Is.EqualTo(.2f));
            Assert.That(MirrorGuardRule.ReflectCap, Is.EqualTo(.02f));
            var guard = new MirrorGuardState();
            Assert.That(guard.ArcDegrees, Is.EqualTo(MirrorGuardRule.ArcDegrees));
        }

        // Unstepped - the frame it walks in, off the foundry - it guards nothing: the landing is whole and reflects nothing.
        [Test]
        public void UntilItsFirstStepItGuardsNothing()
        {
            var guard = Tuned();
            Assert.That(guard.IsGuarding, Is.False);
            Assert.That(float.IsNaN(guard.Facing) && float.IsNaN(guard.ReflectBudget), Is.True);
            Assert.That(guard.Land(10f, 0f, 0f, 1f, 0f, out bool front, out float reflected), Is.EqualTo(10f));
            Assert.That(front, Is.False);
            Assert.That(reflected, Is.Zero);
        }

        // Its first step faces the hero at once, wherever the hero stands; every step after turns it by at most 45 degrees
        // a second, the short way round, until it faces the hero.
        [Test]
        public void ItTurnsTowardTheHeroAtFortyFiveDegreesASecond()
        {
            var guard = Tuned();
            guard.Step(Frame, 0f, 0f, 0f, -2f, 100f);
            Assert.That(guard.Facing, Is.EqualTo(-90f * Degree).Within(1e-5f), "the first step faces the hero at once");

            guard = FacingEast();
            At(90f, 2f, out float hx, out float hy);
            for (int i = 0; i < 60; i++)
                guard.Step(Frame, 0f, 0f, hx, hy, 100f);
            Assert.That(guard.Facing / Degree, Is.EqualTo(45f).Within(.01f), "a second turns it 45 degrees");
            for (int i = 0; i < 30; i++)
                guard.Step(Frame, 0f, 0f, hx, hy, 100f);
            Assert.That(guard.Facing / Degree, Is.EqualTo(67.5f).Within(.01f), "half a second more, 22.5 more");
            for (int i = 0; i < 120; i++)
                guard.Step(Frame, 0f, 0f, hx, hy, 100f);
            Assert.That(guard.Facing / Degree, Is.EqualTo(90f).Within(1e-3f), "and it stops facing the hero");

            // The short way round: from east toward a hero at -100 degrees it turns clockwise.
            guard = FacingEast();
            At(-100f, 2f, out hx, out hy);
            guard.Step(.5f, 0f, 0f, hx, hy, 100f);
            Assert.That(guard.Facing / Degree, Is.EqualTo(-22.5f).Within(1e-3f));
            Assert.That(MirrorGuardRule.TurnStep(MirrorGuardRule.TurnDegrees, 1f), Is.EqualTo(45f * Degree).Within(1e-6f));
            // Across the back: facing 170 degrees toward a hero at -170 turns through 180, not back through 0.
            Assert.That(MirrorGuardRule.Turn(170f * Degree, 0f, 0f, (float)Math.Cos(-170f * Degree), (float)Math.Sin(-170f * Degree), 5f * Degree) / Degree,
                Is.EqualTo(175f).Within(1e-3f));
        }

        // The front arc is 120 degrees centred on the facing, judged by the hero's position: a hero at 59 degrees either
        // side stands in front, at 61 on the flank; behind it, the rear.
        [Test]
        public void TheFrontArcIsJudgedByTheHerosPositionAndItsEdgesHold()
        {
            foreach (float bearing in new[] { 0f, 30f, -30f, 59f, -59f })
            {
                At(bearing, 1.65f, out float hx, out float hy);
                Assert.That(MirrorGuardRule.InFront(0f, 0f, 0f, hx, hy, MirrorGuardRule.ArcDegrees), Is.True, bearing + " degrees");
            }
            foreach (float bearing in new[] { 61f, -61f, 90f, -90f, 135f, 180f, -150f })
            {
                At(bearing, 1.65f, out float hx, out float hy);
                Assert.That(MirrorGuardRule.InFront(0f, 0f, 0f, hx, hy, MirrorGuardRule.ArcDegrees), Is.False, bearing + " degrees");
            }
            // The facing turned, the arc turns with it; and the Sentry's own place is where it is measured from.
            At(170f, 1f, out float rx, out float ry);
            Assert.That(MirrorGuardRule.InFront(180f * Degree, 5f, 5f, 5f + rx, 5f + ry, MirrorGuardRule.ArcDegrees), Is.True);
            Assert.That(MirrorGuardRule.InFront(180f * Degree, 5f, 5f, 5f + 1f, 5f, MirrorGuardRule.ArcDegrees), Is.False);
        }

        // From the front the Sentry takes half of a landing; from a flank or the rear the whole of it.
        [Test]
        public void AFrontLandingIsHalvedAndAFlankOrRearLandingIsWhole()
        {
            // A hero of 200 health: a budget of 4, past the 2.8 this landing asks.
            MirrorGuardState guard = FacingEast(200f);
            At(20f, 1.6f, out float hx, out float hy);
            Assert.That(guard.Land(14f, 0f, 0f, hx, hy, out bool front, out float reflected), Is.EqualTo(7f));
            Assert.That(front, Is.True);
            Assert.That(reflected, Is.EqualTo(14f * Share).Within(1e-5f), "a fifth of the whole landing");

            guard = FacingEast();
            foreach (float bearing in new[] { 90f, -90f, 180f })
            {
                At(bearing, 1.6f, out hx, out hy);
                Assert.That(guard.Land(14f, 0f, 0f, hx, hy, out front, out reflected), Is.EqualTo(14f), bearing + " degrees");
                Assert.That(front, Is.False);
                Assert.That(reflected, Is.Zero, "nothing reflects from " + bearing + " degrees");
            }
            Assert.That(MirrorGuardRule.DamageOn(14f, true, Front), Is.EqualTo(7f));
            Assert.That(MirrorGuardRule.DamageOn(14f, false, Front), Is.EqualTo(14f));
        }

        // The reflection is capped at 2 % of the hero's maximum a second for each Sentry: a budget that starts full - one
        // second's cap - is spent by the landings and refills at the cap a second, never past it; a second Sentry has its own.
        [Test]
        public void TheReflectionIsCappedAtTwoPercentOfTheHerosMaximumASecondPerSentry()
        {
            const float maximum = 150f;
            float cap = maximum * Cap;
            Assert.That(cap, Is.EqualTo(3f).Within(1e-5f));
            MirrorGuardState guard = FacingEast(maximum);
            MirrorGuardState other = FacingEast(maximum);
            Assert.That(guard.ReflectBudget, Is.EqualTo(cap).Within(1e-5f), "full at its first step");

            // 10 asks 2, 10 more asks 2 of the 1 left, then nothing is left to pay.
            guard.Land(10f, 0f, 0f, 1f, 0f, out _, out float first);
            guard.Land(10f, 0f, 0f, 1f, 0f, out _, out float second);
            float dealt = guard.Land(10f, 0f, 0f, 1f, 0f, out bool front, out float third);
            Assert.That(first, Is.EqualTo(2f).Within(1e-5f));
            Assert.That(second, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(third, Is.Zero);
            Assert.That((front, dealt), Is.EqualTo((true, 5f)), "the guard still halves a landing it has no reflection left for");
            Assert.That(guard.ReflectBudget, Is.EqualTo(0f).Within(1e-5f));

            // The other Sentry's budget is its own.
            other.Land(10f, 0f, 0f, 1f, 0f, out _, out float otherFirst);
            Assert.That(otherFirst, Is.EqualTo(2f).Within(1e-5f));

            // Half a second refills half the cap; it never holds more than a second's.
            for (int i = 0; i < 30; i++)
                guard.Step(Frame, 0f, 0f, 1f, 0f, maximum);
            Assert.That(guard.ReflectBudget, Is.EqualTo(cap * .5f).Within(1e-4f));
            for (int i = 0; i < 300; i++)
                guard.Step(Frame, 0f, 0f, 1f, 0f, maximum);
            Assert.That(guard.ReflectBudget, Is.EqualTo(cap).Within(1e-5f), "full, and no more");

            // Over a long trade from the front, what it reflects a second never passes the cap.
            guard = FacingEast(maximum);
            float total = 0f;
            for (int i = 0; i < 600; i++)
            {
                guard.Step(Frame, 0f, 0f, 1f, 0f, maximum);
                guard.Land(30f, 0f, 0f, 1f, 0f, out _, out float paid);
                total += paid;
            }
            Assert.That(total, Is.LessThanOrEqualTo(cap * 11f + 1e-3f), "ten seconds and the starting budget");
            Assert.That(total, Is.GreaterThan(cap * 10f - 1e-3f), "and the budget is spent as it refills");
        }

        // A held clock - the boss's reveal, a level-up panel - is a clock not stepped: nothing turns and nothing refills.
        // A step of no time changes nothing either.
        [Test]
        public void AHeldClockNeitherTurnsNorRefills()
        {
            MirrorGuardState guard = FacingEast();
            guard.Land(10f, 0f, 0f, 1f, 0f, out _, out _);
            float facing = guard.Facing, budget = guard.ReflectBudget;
            At(90f, 2f, out float hx, out float hy);
            guard.Step(0f, 0f, 0f, hx, hy, 100f);
            Assert.That((guard.Facing, guard.ReflectBudget), Is.EqualTo((facing, budget)));
            Assert.Throws<ArgumentOutOfRangeException>(() => guard.Step(-Frame, 0f, 0f, hx, hy, 100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MirrorGuardState(float.NaN, 120f, .5f, .2f, .02f));

            // A body back on the floor starts unturned again.
            guard.Reset();
            Assert.That(guard.IsGuarding, Is.False);
        }

        // The reflection owed is banked and paid as one hit, from the last Sentry that reflected, on the next frame the fight
        // runs; while it holds (a level-up panel, the boss's reveal) nothing is paid and the bank keeps it (the lead's
        // decision 2026-11-12: the hero loses no health while it chooses).
        [Test]
        public void TheReflectionIsBankedWhileTheFightHoldsAndPaidAsItResumes()
        {
            var due = new MirrorReflectionDue<string>();
            Assert.That(due.Take(true, out string from), Is.Zero, "nothing owed");
            Assert.That(from, Is.Null);
            due.Owe(1.5f, "first");
            due.Owe(0f, "nobody");
            due.Owe(.5f, "second");
            Assert.That(due.Take(false, out from), Is.Zero, "held: nothing paid");
            Assert.That(due.Owed, Is.EqualTo(2f), "and nothing lost");
            due.Owe(1f, "second");
            Assert.That(due.Take(true, out from), Is.EqualTo(3f), "running: all of it at once");
            Assert.That(from, Is.EqualTo("second"), "from the last Sentry that reflected");
            Assert.That(due.Owed, Is.Zero);
            Assert.That(due.Take(true, out _), Is.Zero, "paid once");
            Assert.Throws<ArgumentOutOfRangeException>(() => due.Owe(-1f, "nobody"));
        }
    }
}
