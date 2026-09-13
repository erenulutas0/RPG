using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class RunPauseTests
    {
        [Test]
        public void PlayerPauseFreezesTheRunUntilResumed()
        {
            var pause = new RunPause(new RunState(10));
            int changes = 0;
            pause.Changed += () => changes++;
            Assert.That(pause.CanPause, Is.True);

            Assert.That(pause.TryPause(), Is.True);
            Assert.That(pause.TryPause(), Is.False, "Pausing twice changes nothing.");
            Assert.That(pause.IsFrozen, Is.True);
            Assert.That(pause.CanPause, Is.False);

            Assert.That(pause.TryResume(), Is.True);
            Assert.That(pause.TryResume(), Is.False);
            Assert.That(pause.IsFrozen, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void AnOpenChoiceHoldsTheRunAndCannotBePausedOnTop()
        {
            var pause = new RunPause(new RunState(10));
            int changes = 0;
            pause.Changed += () => changes++;

            pause.SetChoiceOpen(true);
            pause.SetChoiceOpen(true);
            Assert.That(pause.IsFrozen, Is.True);
            Assert.That(pause.TryPause(), Is.False, "Leaving the app during a choice keeps the choice in charge.");

            pause.SetChoiceOpen(false);
            Assert.That(pause.IsFrozen, Is.False);
            Assert.That(changes, Is.EqualTo(2));
        }

        [Test]
        public void TheRunKeepsRunningWhileEitherHoldRemains()
        {
            var pause = new RunPause(new RunState(10));
            pause.TryPause();
            pause.SetChoiceOpen(true);

            pause.TryResume();
            Assert.That(pause.IsFrozen, Is.True, "The choice still holds the run after the player resumes.");
            pause.SetChoiceOpen(false);
            Assert.That(pause.IsFrozen, Is.False);
        }

        [Test]
        public void EndingTheRunReleasesThePauseAndBlocksNewOnes()
        {
            var run = new RunState(10);
            var pause = new RunPause(run);
            pause.TryPause();
            int changes = 0;
            pause.Changed += () => changes++;

            run.End(RunOutcome.Defeat);

            Assert.That(pause.IsPlayerPaused, Is.False);
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(pause.TryPause(), Is.False);
            Assert.That(pause.CanPause, Is.False);
        }
    }
}
