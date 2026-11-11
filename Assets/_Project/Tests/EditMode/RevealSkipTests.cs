using System;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    // The boss's reveal is skipped by a new tap or click, as its "a tap skips" says, not by a finger already on the screen
    // (RevealSkip, the run flow's review of 2026-11-11). The legacy Input cannot be driven from a test, so the rule is held
    // here and BossRevealView reads each frame's input through it.
    public sealed class RevealSkipTests
    {
        private static Func<int, TouchPhase> Phases(params TouchPhase[] phases) => index => phases[index];

        [Test]
        public void AFingerAlreadyOnTheScreenDoesNotSkip()
        {
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Moved), false, false), Is.False, "Walking the hero.");
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Stationary), false, false), Is.False, "Holding it still.");
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Ended), false, false), Is.False, "Lifting it.");
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Canceled), false, false), Is.False);
            Assert.That(RevealSkip.Skips(0, Phases(), false, false), Is.False, "Nothing at all.");
        }

        [Test]
        public void ANewTapAClickOrSpaceSkips()
        {
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), false, false), Is.True, "A tap.");
            Assert.That(RevealSkip.Skips(2, Phases(TouchPhase.Moved, TouchPhase.Began), false, false), Is.True,
                "A second finger tapping while the first walks the hero.");
            Assert.That(RevealSkip.Skips(0, Phases(), true, false), Is.True, "A click.");
            Assert.That(RevealSkip.Skips(0, Phases(), false, true), Is.True, "Space.");
        }

        // A tap on the UI is the button's - the pause button - not a skip; another finger's new tap beside it still skips
        // (the run flow's review, 2026-11-11).
        [Test]
        public void ATapOnTheUiDoesNotSkip()
        {
            Func<int, bool> firstOverUi = index => index == 0;
            Func<int, bool> noneOverUi = index => false;
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), firstOverUi, false, false, false, false), Is.False,
                "A tap on the pause button.");
            Assert.That(RevealSkip.Skips(0, Phases(), null, true, true, false, false), Is.False, "A click on a button.");
            Assert.That(RevealSkip.Skips(2, Phases(TouchPhase.Began, TouchPhase.Began), firstOverUi, false, false, false, false),
                Is.True, "A second finger tapping the floor beside it.");
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), noneOverUi, false, false, false, false), Is.True,
                "A tap on the floor.");
            Assert.That(RevealSkip.Skips(0, Phases(), null, true, false, false, false), Is.True, "A click on the floor.");
        }

        // The ability button stands greyed while the reveal holds the fight (AbilityController.IsHeld) and takes no tap: a
        // tap on it skips as one on the floor does, where a tap on any other UI - the pause button - stays that UI's. Counted
        // as a tap on the UI, a tap on the greyed button did nothing at all (the run flow's review, 2026-11-11).
        [Test]
        public void ATapOnTheHeldAbilityButtonSkips()
        {
            Func<int, bool> overUi = index => true;
            Func<int, bool> firstOnHeld = index => index == 0;
            Func<int, bool> noneOnHeld = index => false;
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), overUi, firstOnHeld, false, false, false, false, false),
                Is.True, "A tap on the greyed ability button.");
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), overUi, noneOnHeld, false, false, false, false, false),
                Is.False, "A tap on the pause button.");
            Assert.That(RevealSkip.Skips(2, Phases(TouchPhase.Stationary, TouchPhase.Began), overUi, firstOnHeld, false, false,
                false, false, false), Is.False, "A tap on the pause button while a finger rests on the greyed one.");
            Assert.That(RevealSkip.Skips(0, Phases(), null, null, true, true, true, false, false), Is.True,
                "A click on the greyed ability button.");
            Assert.That(RevealSkip.Skips(0, Phases(), null, null, true, true, false, false, false), Is.False,
                "A click on the pause button.");
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), overUi, firstOnHeld, false, false, false, false, true),
                Is.False, "Nor while the game stands paused.");
        }

        // With a finger on the screen the mouse button is its echo (Input.simulateMouseWithTouches): the touches decide,
        // so the echo of a tap on a button does not skip either.
        [Test]
        public void TheMouseEchoOfATouchDoesNotDecide()
        {
            Func<int, bool> overUi = index => true;
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), overUi, true, false, false, false), Is.False);
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Moved), null, true, false, false, false), Is.False,
                "A walking finger's echo.");
        }

        // The reveal's view reads its input at a time scale of 0 too; nothing pressed while the game stands paused may latch
        // the skip for the moment play resumes.
        [Test]
        public void NothingSkipsWhileTheGameIsPaused()
        {
            Assert.That(RevealSkip.Skips(1, Phases(TouchPhase.Began), null, false, false, false, true), Is.False, "A tap.");
            Assert.That(RevealSkip.Skips(0, Phases(), null, true, false, false, true), Is.False, "A click.");
            Assert.That(RevealSkip.Skips(0, Phases(), null, false, false, true, true), Is.False, "Space.");
        }

        [Test]
        public void TheTouchesMustBeReadable()
        {
            Assert.Throws<ArgumentNullException>(() => RevealSkip.Skips(0, null, false, false));
            Assert.Throws<ArgumentNullException>(() => RevealSkip.Skips(0, null, null, false, false, false, true));
            Assert.Throws<ArgumentNullException>(() => RevealSkip.Skips(0, null, null, null, false, false, false, false, true));
        }
    }
}
