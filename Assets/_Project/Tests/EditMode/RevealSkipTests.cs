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

        [Test]
        public void TheTouchesMustBeReadable()
        {
            Assert.Throws<ArgumentNullException>(() => RevealSkip.Skips(0, null, false, false));
        }
    }
}
