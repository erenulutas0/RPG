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

        // The map choice (CombatSetup.IsChoosingMap): nothing runs yet, so leaving the app there pauses nothing - the
        // overlay opened under the map choice's canvas and the run began frozen (the run flow's review, 2026-11-26).
        [Test]
        public void ARunNotYetBegunCannotBePausedAndRunsOnceItBegins()
        {
            var pause = new RunPause(new RunState(10));
            int changes = 0;
            pause.Changed += () => changes++;

            pause.SetAwaitingStart(true);
            pause.SetAwaitingStart(true);
            Assert.That(pause.IsAwaitingStart, Is.True);
            Assert.That(pause.CanPause, Is.False);
            Assert.That(pause.TryPause(), Is.False, "Leaving the app on the map choice pauses nothing.");
            Assert.That(pause.IsPlayerPaused, Is.False);
            Assert.That(pause.IsFrozen, Is.False, "Waiting for the start freezes nothing.");

            pause.SetAwaitingStart(false);
            Assert.That(pause.CanPause, Is.True, "Once the run begins it can be paused.");
            Assert.That(pause.TryPause(), Is.True);
            Assert.That(changes, Is.EqualTo(3), "Each change of the start's wait, and the pause, are told once.");
        }
    }
}
