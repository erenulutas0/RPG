using System;
using Cryptforge.Analytics;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/59 3.3: a background trip to a rewarded video is not a new session; any other trip ends the session as it
    // leaves, GameAnalytics' own rule, unless a grace is given.
    public sealed class SessionClockTests
    {
        [Test]
        public void AnOrdinaryTripEndsTheSessionAsTheAppLeaves()
        {
            var clock = new SessionClock();
            Assert.Throws<InvalidOperationException>(() => clock.Backgrounded(1), "Nothing before the first session.");
            Assert.That((clock.Ordinal, clock.HasStarted, clock.Length(5)), Is.EqualTo((0, false, 0.0)));

            clock.Start(10);
            clock.Start(99);
            Assert.That((clock.Ordinal, clock.StartedAt), Is.EqualTo((1, 10.0)), "Only the first Start counts.");

            Assert.That(clock.Backgrounded(70), Is.True, "Ended now: a backend is told at once.");
            Assert.That(clock.Backgrounded(71), Is.False, "The same trip.");
            Assert.That((clock.InBackground, clock.Length(500)), Is.EqualTo((true, 60.0)), "Up to the moment the app left.");

            SessionResume resume = clock.Foregrounded(72);
            Assert.That((resume.NewSession, resume.VideoTrip, resume.TripSeconds, resume.Ordinal, resume.EndedLength),
                Is.EqualTo((true, false, 2.0, 2, 60.0)));
            Assert.That((clock.Ordinal, clock.StartedAt, clock.InBackground, clock.Length(80)), Is.EqualTo((2, 72.0, false, 8.0)));

            SessionResume none = clock.Foregrounded(90);
            Assert.That((none.NewSession, none.TripSeconds, none.Ordinal), Is.EqualTo((false, 0.0, 2)), "A return without a trip.");
        }

        [Test]
        public void ABackgroundTripToAVideoIsNotANewSession()
        {
            var clock = new SessionClock();
            clock.Start(0);
            clock.VideoOpened();
            Assert.That(clock.VideoOpen, Is.True);
            Assert.That(clock.Backgrounded(100), Is.False, "A video trip does not end the session as it leaves.");
            SessionResume resume = clock.Foregrounded(135);
            clock.VideoClosed();
            Assert.That((resume.NewSession, resume.VideoTrip, resume.TripSeconds, resume.Ordinal), Is.EqualTo((false, true, 35.0, 1)));
            Assert.That((clock.Ordinal, clock.Length(140)), Is.EqualTo((1, 140.0)), "The video's time is the session's.");

            // The next ordinary trip is ordinary again.
            Assert.That(clock.Backgrounded(150), Is.True);
            Assert.That(clock.Foregrounded(151).NewSession, Is.True);
            Assert.That(clock.Ordinal, Is.EqualTo(2));
        }

        [Test]
        public void AVideoTripPastItsAllowanceEndsTheSessionAsOfLeaving()
        {
            var clock = new SessionClock(0, 300);
            clock.Start(0);
            clock.VideoOpened();
            Assert.That(clock.Backgrounded(50), Is.False);
            SessionResume atLimit = clock.Foregrounded(350);
            Assert.That((atLimit.NewSession, atLimit.TripSeconds), Is.EqualTo((false, 300.0)), "Exactly the allowance still holds.");

            Assert.That(clock.Backgrounded(400), Is.False, "The video is still open: its close has not arrived.");
            SessionResume past = clock.Foregrounded(1400);
            Assert.That((past.NewSession, past.VideoTrip, past.Ordinal, past.EndedLength), Is.EqualTo((true, true, 2, 400.0)));
            Assert.That(clock.StartedAt, Is.EqualTo(1400.0));

            // A video opened after the app left does not change the trip it is on.
            clock.VideoClosed();
            clock.Backgrounded(1500);
            clock.VideoOpened();
            Assert.That(clock.Foregrounded(1501).NewSession, Is.True);
        }

        [Test]
        public void AGraceKeepsShortOrdinaryTripsInTheSession()
        {
            var clock = new SessionClock(30, 300);
            clock.Start(0);
            Assert.That(clock.Backgrounded(10), Is.False, "With a grace the end is decided on return.");
            Assert.That(clock.Foregrounded(40).NewSession, Is.False, "30 s away is within the grace.");
            clock.Backgrounded(50);
            SessionResume past = clock.Foregrounded(81);
            Assert.That((past.NewSession, past.Ordinal, past.EndedLength), Is.EqualTo((true, 2, 50.0)));

            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionClock(-1, 300));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionClock(0, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SessionClock(double.PositiveInfinity, 300));
            Assert.That((SessionClock.DefaultGraceSeconds, SessionClock.DefaultVideoTripSeconds), Is.EqualTo((0.0, 300.0)));
        }
    }
}
