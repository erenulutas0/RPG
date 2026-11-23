using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The game-feel pass's timings and curves (docs/43 section 1, 2026-11-22), without Unity: which cue a later slot's
    // weapon plays and for how long, the hero's recoil and pop, a boss's tell through its telegraph, and how a crowd's
    // deaths are budgeted.
    public sealed class GameFeelTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void EachHeroWeaponHasItsOwnCue()
        {
            Assert.That(GameFeel.CueFor(WeaponBehavior.Cleave), Is.EqualTo(SlotCue.Flourish), "the Sword");
            Assert.That(GameFeel.CueFor(WeaponBehavior.Area), Is.EqualTo(SlotCue.Pulse), "the Staff");
            Assert.That(GameFeel.CueFor(WeaponBehavior.DirectHit), Is.EqualTo(SlotCue.Flick), "the Daggers");
            // docs/63 phase 2's Cinder Saw, an aura: the Staff's glow when it is picked up, and no cue on each of its five
            // strikes a second - its blade circling the hero is its look.
            Assert.That(GameFeel.CueFor(WeaponBehavior.Aura), Is.EqualTo(SlotCue.Pulse), "the Cinder Saw");
            Assert.That(GameFeel.CuesEachStrike(WeaponBehavior.Aura), Is.False);
            foreach (WeaponBehavior behavior in new[] { WeaponBehavior.Cleave, WeaponBehavior.Area, WeaponBehavior.DirectHit })
                Assert.That(GameFeel.CuesEachStrike(behavior), Is.True, behavior.ToString());
        }

        [Test]
        public void ACueEndsInsideItsWeaponsInterval()
        {
            foreach (SlotCue cue in new[] { SlotCue.Flourish, SlotCue.Pulse, SlotCue.Flick })
            {
                // The category intervals (docs/40 section 5.2): the Sword's 0.8 s, the Staff's 1.1 s, the Daggers' 0.3 s,
                // and a Tempo-quickened 0.15 s.
                foreach (float interval in new[] { 1.1f, .8f, .3f, .15f })
                {
                    float seconds = GameFeel.CueSeconds(cue, interval);
                    Assert.That(seconds, Is.LessThanOrEqualTo(System.Math.Max(GameFeel.MinimumCueSeconds,
                        interval * GameFeel.CueShare) + Tolerance), cue + " at " + interval);
                    Assert.That(seconds, Is.GreaterThanOrEqualTo(GameFeel.MinimumCueSeconds), cue + " at " + interval);
                }
            }
            Assert.That(GameFeel.CueSeconds(SlotCue.Flick, .3f), Is.EqualTo(GameFeel.FlickSeconds), "The Daggers' flick whole.");
            Assert.That(GameFeel.CueSeconds(SlotCue.Pulse, 0f), Is.EqualTo(GameFeel.PulseSeconds), "No interval: its own length.");
        }

        [Test]
        public void TheFlourishSweepsFromBehindTheShoulderToTheTarget()
        {
            Assert.That(GameFeel.FlourishAngle(0f), Is.EqualTo(GameFeel.FlourishFrom).Within(Tolerance));
            Assert.That(GameFeel.FlourishAngle(1f), Is.EqualTo(GameFeel.FlourishTo).Within(Tolerance));
            float last = GameFeel.FlourishAngle(0f);
            for (int i = 1; i <= 20; i++)
            {
                float angle = GameFeel.FlourishAngle(i / 20f);
                Assert.That(angle, Is.LessThan(last), "It only sweeps forward.");
                last = angle;
            }
            Assert.That(GameFeel.FlourishAngle(.3f), Is.LessThan(90f), "Over the top within the first third.");
        }

        [Test]
        public void ACuePopsInAndIsGoneAtItsEnd()
        {
            Assert.That(GameFeel.CueEnvelope(0f), Is.Zero);
            Assert.That(GameFeel.CueEnvelope(GameFeel.InShare), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(GameFeel.CueEnvelope(1f), Is.Zero.Within(Tolerance));
            for (int i = 0; i <= 20; i++)
                Assert.That(GameFeel.CueEnvelope(i / 20f), Is.InRange(0f, 1f));
        }

        [Test]
        public void ABlowRecoilsQuicklyAndSettles()
        {
            Assert.That(GameFeel.HitEnvelope(0f), Is.Zero);
            Assert.That(GameFeel.HitEnvelope(GameFeel.HitAttack), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(GameFeel.HitEnvelope(1f), Is.Zero);
            Assert.That(GameFeel.HitEnvelope(-.1f), Is.Zero);
            Assert.That(GameFeel.HitEnvelope(.99f), Is.LessThan(.01f));
        }

        [Test]
        public void ABlowFromTheSideSqueezesTheWidthAndOneFromInFrontTheHeight()
        {
            GameFeel.Squash(1f, 1f, 0f, out float sideX, out float sideY);
            Assert.That(sideX, Is.EqualTo(1f - GameFeel.SquashAlong).Within(Tolerance));
            Assert.That(sideY, Is.EqualTo(1f + GameFeel.SquashAcross).Within(Tolerance));
            GameFeel.Squash(1f, 0f, -1f, out float frontX, out float frontY);
            Assert.That(frontY, Is.EqualTo(1f - GameFeel.SquashAlong).Within(Tolerance));
            Assert.That(frontX, Is.EqualTo(1f + GameFeel.SquashAcross).Within(Tolerance));
            GameFeel.Squash(1f, -1f, 0f, out float leftX, out float leftY);
            Assert.That((leftX, leftY), Is.EqualTo((sideX, sideY)), "Either side alike.");
            GameFeel.Squash(0f, 1f, 0f, out float restX, out float restY);
            Assert.That((restX, restY), Is.EqualTo((1f, 1f)), "No blow, no squash.");
            GameFeel.Squash(1f, 0f, 0f, out restX, out restY);
            Assert.That((restX, restY), Is.EqualTo((1f, 1f)), "No direction, no squash.");
        }

        [Test]
        public void ALevelPopsUpAndSettles()
        {
            Assert.That(GameFeel.PopEnvelope(0f), Is.Zero.Within(Tolerance));
            Assert.That(GameFeel.PopEnvelope(.25f), Is.EqualTo(1f).Within(Tolerance), "Its peak a quarter of the way.");
            Assert.That(GameFeel.PopEnvelope(1f), Is.Zero);
            Assert.That(GameFeel.PopEnvelope(.95f), Is.LessThan(.25f));
        }

        [Test]
        public void ABossTellsTheBlowEarlyAndGathersToIt()
        {
            Assert.That(GameFeel.TellEase(0f), Is.Zero);
            Assert.That(GameFeel.TellEase(1f), Is.EqualTo(1f).Within(Tolerance));
            Assert.That(GameFeel.TellEase(.3f), Is.GreaterThan(.6f), "Most of the rise is told in the first third.");
            Assert.That(GameFeel.TellGlow(0f, 1f), Is.EqualTo(GameFeel.TellGlowFloor).Within(Tolerance), "On at the mark.");
            Assert.That(GameFeel.TellGlow(1f, 1f), Is.EqualTo(GameFeel.TellGlowPeak).Within(Tolerance));
            Assert.That(GameFeel.TellGlow(.5f, 0f), Is.LessThan(GameFeel.TellGlow(.5f, 1f)), "It beats with the mark.");
            Assert.That(GameFeel.TellGlow(1f, 1f), Is.LessThan(.6f), "Never so bright the mark stops being the clearest thing.");
        }

        [Test]
        public void ACrowdsDeathsThrowFewerMotes()
        {
            Assert.That(GameFeel.MotesPerDeath(1), Is.EqualTo(6));
            Assert.That(GameFeel.MotesPerDeath(GameFeel.FewDeaths), Is.EqualTo(6));
            Assert.That(GameFeel.MotesPerDeath(GameFeel.FewDeaths + 1), Is.EqualTo(3));
            Assert.That(GameFeel.MotesPerDeath(GameFeel.CrowdDeaths), Is.EqualTo(3));
            Assert.That(GameFeel.MotesPerDeath(GameFeel.CrowdDeaths + 1), Is.EqualTo(1), "Past about thirty at once: one.");
            Assert.That(GameFeel.EmbersPerArrival(1), Is.EqualTo(3));
            Assert.That(GameFeel.EmbersPerArrival(GameFeel.FewDeaths + 1), Is.EqualTo(1));
            // Forty deaths at once - the scene's whole crowd - cost at most forty motes more than one death's six.
            int total = 0;
            for (int n = 1; n <= 40; n++)
                total += GameFeel.MotesPerDeath(n);
            Assert.That(total, Is.EqualTo(10 * 6 + 20 * 3 + 10 * 1));
        }

        [Test]
        public void TheBudgetCountsOnlyItsWindow()
        {
            var budget = new FeelBudget(.3f, 8);
            Assert.That(budget.Recent(0f), Is.Zero);
            budget.Record(0f);
            budget.Record(.1f);
            budget.Record(.2f);
            Assert.That(budget.Recent(.2f), Is.EqualTo(3));
            Assert.That(budget.Recent(.45f), Is.EqualTo(1), "Only the one within 0.3 s.");
            Assert.That(budget.Recent(1f), Is.Zero);
            for (int i = 0; i < 20; i++)
                budget.Record(2f);
            Assert.That(budget.Recent(2f), Is.EqualTo(budget.Capacity), "A count past the ring reads as the ring.");
            budget.Clear();
            Assert.That(budget.Recent(2f), Is.Zero);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new FeelBudget(0f, 4));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new FeelBudget(1f, 0));
        }
    }
}
