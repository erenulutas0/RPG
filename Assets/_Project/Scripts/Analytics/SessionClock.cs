using System;

namespace Cryptforge.Analytics
{
    // What returning to the foreground did to the play session.
    public readonly struct SessionResume
    {
        // A new session began at the return: the trip outlasted its allowance.
        public bool NewSession { get; }
        // The trip began with a video on screen.
        public bool VideoTrip { get; }
        // How long the app was away, in the clock's seconds.
        public double TripSeconds { get; }
        // The session running now, counting from 1.
        public int Ordinal { get; }
        // When NewSession: the ended session's length, from its start to the moment the app left; 0 otherwise.
        public double EndedLength { get; }

        public SessionResume(bool newSession, bool videoTrip, double tripSeconds, int ordinal, double endedLength)
        {
            NewSession = newSession;
            VideoTrip = videoTrip;
            TripSeconds = tripSeconds;
            Ordinal = ordinal;
            EndedLength = endedLength;
        }
    }

    // The analytics' play sessions within one launch (docs/59 3.3: "background trips to a video must not count as a new
    // session"). GameAnalytics' default ends a session whenever the app goes to the background and starts one when it
    // returns; a rewarded video's full-screen activity sends the game to the background, so by that rule every video would
    // split a session in two. This clock keeps that rule for an ordinary trip (graceSeconds 0: the session ends as the app
    // leaves) and exempts a trip that began with a video on screen for up to videoTripSeconds; a longer one (the player
    // left the phone during the video) ends the session after all, as of the moment the app left. Pure: the caller passes
    // monotonic seconds (ITelemetryClock.Seconds), and drives a backend's manual sessions from the answers - Backgrounded
    // true: end the session now; Foregrounded with NewSession: start one. TelemetrySession's id stays the launch's.
    public sealed class SessionClock
    {
        // GameAnalytics' own rule: an ordinary trip ends the session at once.
        public const double DefaultGraceSeconds = 0;
        // A rewarded video runs 15-60 s with its end card; five minutes is room for a slow network and a closing tap.
        public const double DefaultVideoTripSeconds = 300;

        private readonly double _graceSeconds;
        private readonly double _videoTripSeconds;
        private double _leftAt;
        private bool _endedAtLeaving;
        private bool _tripIsVideo;

        public SessionClock(double graceSeconds = DefaultGraceSeconds, double videoTripSeconds = DefaultVideoTripSeconds)
        {
            if (!(graceSeconds >= 0) || double.IsInfinity(graceSeconds))
                throw new ArgumentOutOfRangeException(nameof(graceSeconds));
            if (!(videoTripSeconds >= 0) || double.IsInfinity(videoTripSeconds))
                throw new ArgumentOutOfRangeException(nameof(videoTripSeconds));
            _graceSeconds = graceSeconds;
            _videoTripSeconds = videoTripSeconds;
        }

        // The current session, counting from 1; 0 before Start.
        public int Ordinal { get; private set; }
        public bool HasStarted => Ordinal > 0;
        public double StartedAt { get; private set; }
        public bool InBackground { get; private set; }
        // A rewarded video is on screen (RewardOfferCoordinator: from the show to its result).
        public bool VideoOpen { get; private set; }

        // The launch's first session. Later calls change nothing.
        public void Start(double now)
        {
            if (HasStarted)
                return;
            Ordinal = 1;
            StartedAt = now;
        }

        public void VideoOpened()
        {
            VideoOpen = true;
        }

        public void VideoClosed()
        {
            VideoOpen = false;
        }

        // The app left the foreground (OnApplicationPause(true)). True when this trip ends the session now: an ordinary
        // trip with no grace. A second call before Foregrounded is the same trip and returns false.
        public bool Backgrounded(double now)
        {
            RequireStarted();
            if (InBackground)
                return false;
            InBackground = true;
            _leftAt = now;
            _endedAtLeaving = !VideoOpen && _graceSeconds <= 0;
            _tripIsVideo = VideoOpen;
            return _endedAtLeaving;
        }


        // The app came back (OnApplicationPause(false)). A return without a trip changes nothing.
        public SessionResume Foregrounded(double now)
        {
            RequireStarted();
            if (!InBackground)
                return new SessionResume(false, false, 0, Ordinal, 0);
            InBackground = false;
            double trip = Math.Max(0, now - _leftAt);
            bool video = _tripIsVideo;
            bool ends = _endedAtLeaving || trip > (video ? _videoTripSeconds : _graceSeconds);
            _tripIsVideo = false;
            _endedAtLeaving = false;
            if (!ends)
                return new SessionResume(false, video, trip, Ordinal, 0);
            double ended = Math.Max(0, _leftAt - StartedAt);
            Ordinal++;
            StartedAt = now;
            return new SessionResume(true, video, trip, Ordinal, ended);
        }

        // The current session's length so far: from its start to now, or to the moment the app left while it is away. A
        // video trip that kept the session counts in it: the player spent it in the game's own screen.
        public double Length(double now)
        {
            if (!HasStarted)
                return 0;
            return Math.Max(0, (InBackground ? _leftAt : now) - StartedAt);
        }

        private void RequireStarted()
        {
            if (!HasStarted)
                throw new InvalidOperationException("Start the session clock first.");
        }
    }
}
