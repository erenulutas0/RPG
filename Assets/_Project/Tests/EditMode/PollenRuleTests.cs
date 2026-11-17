using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Pollen Wraith's pollen (PollenRule, PollenState; docs/52 section 3): it settles on a still hero, 0.10 a second for
    // each Wraith within 1.6 (at most three), falls 0.5 a second while the hero moves, and blooms at 1 for 6 % of the
    // hero's maximum health. Pure, one body for a later scene and the foundry simulation (simulation research only today).
    public sealed class PollenRuleTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void ThePollenIsDocs52sStartingValues()
        {
            Assert.That(PollenRule.Radius, Is.EqualTo(1.6f));
            Assert.That(PollenRule.RisePerWraith, Is.EqualTo(.10f));
            Assert.That(PollenRule.MostWraiths, Is.EqualTo(3));
            Assert.That(PollenRule.FallPerSecond, Is.EqualTo(.5f));
            Assert.That(PollenRule.BloomShare, Is.EqualTo(.06f));
            Assert.That(PollenRule.StillSpeed, Is.EqualTo(1f));
            Assert.That(PollenRule.SpeedWindow, Is.EqualTo(.5f));
        }

        [Test]
        public void ItRisesOnAStillHeroForEachWraithNearAtMostThree()
        {
            Assert.That(PollenRule.Step(0f, true, 1, .1f, 3, .5f, 1f), Is.EqualTo(.1f).Within(1e-6f));
            Assert.That(PollenRule.Step(0f, true, 3, .1f, 3, .5f, 1f), Is.EqualTo(.3f).Within(1e-6f));
            Assert.That(PollenRule.Step(0f, true, 7, .1f, 3, .5f, 1f), Is.EqualTo(.3f).Within(1e-6f), "three at most");
            Assert.That(PollenRule.Step(.4f, true, 0, .1f, 3, .5f, 1f), Is.EqualTo(.4f), "still with none near: it stays");
        }

        [Test]
        public void ItFallsWhileTheHeroMovesAndNeverBelowNothing()
        {
            Assert.That(PollenRule.Step(.8f, false, 3, .1f, 3, .5f, 1f), Is.EqualTo(.3f).Within(1e-6f), "moving sheds it whatever is near");
            Assert.That(PollenRule.Step(.2f, false, 0, .1f, 3, .5f, 1f), Is.EqualTo(0f));
        }

        [Test]
        public void TheNearCountIsTheRadiusInclusive()
        {
            Assert.That(PollenRule.Near(1.6f * 1.6f, 1.6f), Is.True);
            Assert.That(PollenRule.Near(1.61f * 1.61f, 1.6f), Is.False);
            Assert.That(PollenRule.Distance(3f, 4f), Is.EqualTo(5f));
        }

        [Test]
        public void TheSpeedIsSmoothedOverHalfASecond()
        {
            float s = 0f;
            for (int i = 0; i < 30; i++)
                s = PollenRule.Smoothed(s, 3.5f, Frame, .5f);
            Assert.That(s, Is.GreaterThan(3.5f * .6f).And.LessThan(3.5f), "half a second of walking: most of the way");
            Assert.That(PollenRule.Smoothed(2f, 0f, 1f, .5f), Is.EqualTo(0f), "a long frame goes all the way, no further");
            Assert.That(PollenRule.Still(.99f, 1f), Is.True);
            Assert.That(PollenRule.Still(1f, 1f), Is.False);
        }

        [Test]
        public void OneWraithBesideAStillHeroBloomsItInTenSecondsThreeInAThird()
        {
            Assert.That(SecondsToBloom(1), Is.EqualTo(10f).Within(2 * Frame));
            Assert.That(SecondsToBloom(3), Is.EqualTo(10f / 3f).Within(2 * Frame));
            Assert.That(PollenRule.SecondsToBloom(.5f, 1, .1f, 3), Is.EqualTo(5f).Within(1e-4f));
            Assert.That(PollenRule.SecondsToBloom(.5f, 0, .1f, 3), Is.EqualTo(float.PositiveInfinity));
            Assert.That(PollenRule.BloomDamage(120f, .06f), Is.EqualTo(7.2f).Within(1e-5f));
        }

        [Test]
        public void AWalkingHeroCarriesNothingAndTwoSecondsOfWalkShedAFullLoad()
        {
            var state = new PollenState();
            for (int i = 0; i < 600; i++)
                Assert.That(state.Step(Frame, 3.5f * Frame, 3), Is.False, "walking among three Wraiths");
            Assert.That(state.Pollen, Is.EqualTo(0f));

            state.Reset();
            for (int i = 0; i < 9 * 60; i++)
                state.Step(Frame, 0f, 1);
            Assert.That(state.Pollen, Is.GreaterThan(.85f));
            // The smoothed speed passes 1.0 after about a tenth of a second at 3.5, then the walk sheds 0.5 a second.
            for (int i = 0; i < 2 * 60; i++)
                state.Step(Frame, 3.5f * Frame, 1);
            Assert.That(state.Pollen, Is.LessThan(.05f));
        }

        [Test]
        public void ABloomReturnsThePollenToNothing()
        {
            var state = new PollenState();
            int bloomedAt = -1;
            for (int i = 0; i < 12 * 60 && bloomedAt < 0; i++)
                if (state.Step(Frame, 0f, 1))
                    bloomedAt = i;
            Assert.That(bloomedAt, Is.GreaterThan(0));
            Assert.That(state.Pollen, Is.EqualTo(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Step(0f, 0f, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PollenState(-.1f, 3, .5f, 1f, .5f));
        }

        // The redesign screen's pieces (research, docs/52's measured section): the unbroken stillness, the general step and
        // the cloud's life.
        [Test]
        public void TheRedesignScreensStepSettlesAndFeedsFromItsSources()
        {
            Assert.That(PollenRule.StillFor(1f, true, .5f), Is.EqualTo(1.5f));
            Assert.That(PollenRule.StillFor(1f, false, .5f), Is.EqualTo(0f), "a step breaks it");
            Assert.That(PollenRule.Advance(.2f, true, true, 5, .1f, 3, .5f, 1f), Is.EqualTo(.5f).Within(1e-6f), "three sources at most");
            Assert.That(PollenRule.Advance(.2f, false, true, 5, .1f, 3, .5f, 1f), Is.EqualTo(0f));
            Assert.That(PollenRule.Advance(.2f, false, false, 5, .1f, 3, .5f, 1f), Is.EqualTo(.2f));
            Assert.That(PollenRule.CloudStands(3.9f, 4f), Is.True);
            Assert.That(PollenRule.CloudStands(4f, 4f), Is.False);

            var settling = new PollenState();
            for (int i = 0; i < 89; i++)
                settling.Step(Frame, 0f, 1, false, 1.5f);
            Assert.That(settling.Pollen, Is.EqualTo(0f), "nothing before the hero has stood still 1.5 s");
            for (int i = 0; i < 60; i++)
                settling.Step(Frame, 0f, 1, false, 1.5f);
            Assert.That(settling.Pollen, Is.GreaterThan(.05f));

            var cloud = new PollenState();
            for (int i = 0; i < 60; i++)
                cloud.Step(Frame, 3.5f * Frame, 1, true, 0f);
            Assert.That(cloud.Pollen, Is.EqualTo(.1f).Within(1e-3f), "inside a cloud it rises, moving or not");
            for (int i = 0; i < 60; i++)
                cloud.Step(Frame, 0f, 0, true, 0f);
            Assert.That(cloud.Pollen, Is.EqualTo(0f), "outside every cloud it falls, still or not");
        }

        private static float SecondsToBloom(int wraiths)
        {
            var state = new PollenState();
            for (int i = 1; i < 60 * 60; i++)
                if (state.Step(Frame, 0f, wraiths))
                    return i * Frame;
            return float.PositiveInfinity;
        }
    }
}
