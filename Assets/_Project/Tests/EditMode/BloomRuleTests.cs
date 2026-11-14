using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Cinder Bloom's bark-and-bloom cycle (BloomRule, BloomClock; docs/52 section 5): closed 3.5 s - its bark lets a
    // quarter of every landing through, it walks and mauls - then open 2.5 s - rooted, taking every landing whole - from
    // the moment it walks in. Pure, one body for a later scene and the foundry simulation (simulation research only today).
    public sealed class BloomRuleTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void TheCycleIsDocs52sStartingValues()
        {
            Assert.That(BloomRule.ClosedSeconds, Is.EqualTo(3.5f));
            Assert.That(BloomRule.OpenSeconds, Is.EqualTo(2.5f));
            Assert.That(BloomRule.BarkShare, Is.EqualTo(.25f));
        }

        [Test]
        public void ItWalksInClosedAndOpensAfterTheClosedPhase()
        {
            Assert.That(BloomRule.Open(0f, 3.5f, 2.5f), Is.False);
            Assert.That(BloomRule.Open(3.49f, 3.5f, 2.5f), Is.False);
            Assert.That(BloomRule.Open(3.5f, 3.5f, 2.5f), Is.True);
            Assert.That(BloomRule.Open(5.99f, 3.5f, 2.5f), Is.True);
            // The second cycle: closed again from 6 s, open from 9.5 s.
            Assert.That(BloomRule.Open(6.01f, 3.5f, 2.5f), Is.False);
            Assert.That(BloomRule.Open(9.51f, 3.5f, 2.5f), Is.True);
        }

        [Test]
        public void WindowsAreCountedAsTheyBegin()
        {
            Assert.That(BloomRule.WindowsBegun(3f, 3.5f, 2.5f), Is.EqualTo(0));
            Assert.That(BloomRule.WindowsBegun(3.5f, 3.5f, 2.5f), Is.EqualTo(1));
            Assert.That(BloomRule.WindowsBegun(8f, 3.5f, 2.5f), Is.EqualTo(1));
            Assert.That(BloomRule.WindowsBegun(9.6f, 3.5f, 2.5f), Is.EqualTo(2));
            Assert.That(BloomRule.WindowsBegun(15.6f, 3.5f, 2.5f), Is.EqualTo(3));
        }

        [Test]
        public void TheBarkBluntsOnlyWhileClosed()
        {
            Assert.That(BloomRule.DamageOn(40f, false, .25f), Is.EqualTo(10f));
            Assert.That(BloomRule.DamageOn(40f, true, .25f), Is.EqualTo(40f));
        }

        [Test]
        public void ASwingIsDoneWhenItsWindupIs()
        {
            Assert.That(BloomRule.SwingDone(.49f, .5f), Is.False);
            Assert.That(BloomRule.SwingDone(.5f, .5f), Is.True);
        }

        [Test]
        public void TheOpenBloomIsRooted()
        {
            Assert.That(BloomRule.SpeedOf(1.2f, false), Is.EqualTo(1.2f));
            Assert.That(BloomRule.SpeedOf(1.2f, true), Is.EqualTo(0f));
        }

        [Test]
        public void TheClockOpensOnItsOwnTimeAtAnyFrameRate()
        {
            foreach (float frame in new[] { Frame, 1f / 30f, 1f / 120f })
            {
                var clock = new BloomClock();
                float seconds = 0f, firstOpen = -1f, reclosed = -1f;
                while (seconds < 7f)
                {
                    clock.Step(frame);
                    seconds += frame;
                    if (firstOpen < 0f && clock.IsOpen)
                        firstOpen = seconds;
                    if (firstOpen >= 0f && reclosed < 0f && !clock.IsOpen)
                        reclosed = seconds;
                }
                Assert.That(firstOpen, Is.EqualTo(3.5f).Within(frame + 1e-4f), $"opens at 3.5 s ({1f / frame:0} fps)");
                Assert.That(reclosed - firstOpen, Is.EqualTo(2.5f).Within(2f * frame + 1e-4f), $"open 2.5 s ({1f / frame:0} fps)");
            }
        }

        [Test]
        public void TheClockLandsAndSlowsByItsPhase()
        {
            var clock = new BloomClock(3.5f, 2.5f, .25f);
            Assert.That(clock.Land(20f), Is.EqualTo(5f));
            Assert.That(clock.Speed(1.2f), Is.EqualTo(1.2f));
            clock.Step(4f);
            Assert.That(clock.IsOpen, Is.True);
            Assert.That(clock.WindowsBegun, Is.EqualTo(1));
            Assert.That(clock.Land(20f), Is.EqualTo(20f));
            Assert.That(clock.Speed(1.2f), Is.EqualTo(0f));
            clock.Reset();
            Assert.That(clock.IsOpen, Is.False);
            Assert.That(clock.Age, Is.EqualTo(0f));
        }

        [Test]
        public void AClockRefusesAnImpossibleTuning()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new BloomClock(-1f, 2.5f, .25f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BloomClock(0f, 0f, .25f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BloomClock(3.5f, 2.5f, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new BloomClock().Step(-Frame));
        }
    }
}
