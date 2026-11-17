using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Thornling's dive (ThornlingRule, ThornlingClock, ThornlingField; docs/52 section 4): after 3 s of walking, 2.5-5
    // from the hero, it dives; a ring of 1.2 at the hero's spot fills for 0.8 s and it erupts at the centre for 5 % of the
    // hero's maximum health, then is stuck 1 s. One ring on the floor, 2.5 s after the last eruption. Pure, one body for a
    // later scene and the foundry simulation (simulation research only today).
    public sealed class ThornlingRuleTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheDiveIsDocs52sStartingValues()
        {
            Assert.That(ThornlingRule.WalkSeconds, Is.EqualTo(3f));
            Assert.That((ThornlingRule.DiveMin, ThornlingRule.DiveMax), Is.EqualTo((2.5f, 5f)));
            Assert.That(ThornlingRule.Radius, Is.EqualTo(1.2f));
            Assert.That(ThornlingRule.Telegraph, Is.EqualTo(.8f));
            Assert.That(ThornlingRule.Share, Is.EqualTo(.05f));
            Assert.That(ThornlingRule.StuckSeconds, Is.EqualTo(1f));
            Assert.That(ThornlingRule.Gap, Is.EqualTo(2.5f));
        }

        [Test]
        public void ItDivesOnlyAfterItsWalkAndWithinTheDistances()
        {
            Assert.That(ThornlingRule.Dives(2.9f, 3f, 16f, 2.5f, 5f), Is.False, "walked too little");
            Assert.That(ThornlingRule.Dives(3f, 3f, 16f, 2.5f, 5f), Is.True);
            Assert.That(ThornlingRule.Dives(3f, 3f, 2.4f * 2.4f, 2.5f, 5f), Is.False, "too near");
            Assert.That(ThornlingRule.Dives(3f, 3f, 5.1f * 5.1f, 2.5f, 5f), Is.False, "too far");
            Assert.That(ThornlingRule.Dives(3f, 3f, 25f, 2.5f, 5f), Is.True, "the far edge inclusive");
        }

        [Test]
        public void TheRingIsACircleAtTheHerosSpot()
        {
            Assert.That(ThornlingRule.InRing(1.2f, 0f, 0f, 0f, 1.2f), Is.True);
            Assert.That(ThornlingRule.InRing(0.9f, 0.9f, 0f, 0f, 1.2f), Is.False);
            Assert.That(ThornlingRule.Quiet(true, true), Is.True, "research: no ring while the gate boss stands");
            Assert.That(ThornlingRule.Quiet(true, false) || ThornlingRule.Quiet(false, true), Is.False);
            Assert.That(ThornlingRule.Progress(.4f, .8f), Is.EqualTo(.5f));
            Assert.That(ThornlingRule.Progress(2f, .8f), Is.EqualTo(1f));
            Assert.That(ThornlingRule.EruptionDamage(120f, .05f), Is.EqualTo(6f).Within(1e-5f));
        }

        [Test]
        public void AThornlingWalksDivesEruptsAndIsStuck()
        {
            var clock = new ThornlingClock();
            Assert.That(clock.WouldDive(16f, 2.5f, 5f), Is.False, "it walked in just now");
            for (int i = 0; i < 3 * 60 + 1; i++)
                clock.Step(Frame);
            Assert.That(clock.WouldDive(16f, 2.5f, 5f), Is.True);
            clock.Dive();
            Assert.That((clock.IsBurrowed, clock.IsFree, clock.Speed(3f)), Is.EqualTo((true, false, 0f)));
            float walked = clock.Walked;
            clock.Step(1f);
            Assert.That(clock.Walked, Is.EqualTo(walked), "no walk under the ash");
            clock.Erupt();
            Assert.That((clock.IsBurrowed, clock.IsStuck, clock.Walked, clock.Speed(3f)), Is.EqualTo((false, true, 0f, 0f)));
            for (int i = 0; i < 59; i++)
                clock.Step(Frame);
            Assert.That(clock.IsStuck, Is.True);
            clock.Step(2 * Frame);
            Assert.That((clock.IsStuck, clock.IsFree, clock.Speed(3f), clock.Walked), Is.EqualTo((false, true, 3f, 0f)),
                "free again, its walk counted from its eruption");
        }

        [Test]
        public void TheFloorHoldsOneRingAndWaitsTheGapAfterAnEruption()
        {
            var field = new ThornlingField();
            Assert.That(field.MayRing, Is.True, "the first ring waits for nothing");
            field.Down(1f, 2f);
            Assert.That(field.MayRing, Is.False, "one ring at a time");
            Assert.Throws<InvalidOperationException>(() => field.Down(0f, 0f));
            int frames = 0;
            while (!field.Step(Frame))
                frames++;
            Assert.That((frames + 1) * Frame, Is.EqualTo(.8f).Within(1.5f * Frame), "the ring fills in its telegraph");
            Assert.That((field.IsDown, field.X, field.Y), Is.EqualTo((false, 1f, 2f)));
            Assert.That(field.Holds(1.5f, 2.5f, 1.2f), Is.True);
            Assert.That(field.MayRing, Is.False, "the gap after the eruption");
            for (int i = 0; i < 149; i++)
                field.Step(Frame);
            Assert.That(field.MayRing, Is.False);
            field.Step(2 * Frame);
            Assert.That(field.MayRing, Is.True, "2.5 s after the eruption");

            field.Down(0f, 0f);
            field.Cancel();
            Assert.That((field.IsDown, field.MayRing), Is.EqualTo((false, true)), "a cancelled ring restarts no gap");
        }
    }
}
