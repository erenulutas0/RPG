using System;

namespace Cryptforge.Combat
{
    // One leg of a scripted walk: the steer the hero holds until the fight clock reaches UntilSeconds.
    public readonly struct RouteSegment
    {
        public readonly float UntilSeconds;
        public readonly float SteerX;
        public readonly float SteerY;

        public RouteSegment(float untilSeconds, float steerX, float steerY)
        {
            if (float.IsNaN(untilSeconds) || float.IsInfinity(untilSeconds) || untilSeconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(untilSeconds));
            if (float.IsNaN(steerX) || float.IsInfinity(steerX) || float.IsNaN(steerY) || float.IsInfinity(steerY))
                throw new ArgumentOutOfRangeException(nameof(steerX));
            // A steer is a direction whose length is its strength, so a longer one would ask for more than full speed.
            if (steerX * steerX + steerY * steerY > 1f + 1e-4f)
                throw new ArgumentOutOfRangeException(nameof(steerX), "A steer is at most one long.");

            UntilSeconds = untilSeconds;
            SteerX = steerX;
            SteerY = steerY;
        }
    }

    // A walk written down in advance: the segments in order, each holding its steer until the fight clock passes its own
    // UntilSeconds, and (0, 0) once the last one is over. It reads nothing but the clock, so the same script drives the
    // simulation and the scene's PlayMode driver through the same frames. Allocation-free once built.
    public sealed class ScriptedRoute : IHeroRoute
    {
        private readonly RouteSegment[] _segments;

        // The segments end in order; two segments may end at the same second, and the earlier one then never runs.
        public ScriptedRoute(params RouteSegment[] segments)
        {
            if (segments == null || segments.Length == 0)
                throw new ArgumentException("A scripted route needs at least one segment.", nameof(segments));

            float previous = 0f;
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i].UntilSeconds < previous)
                    throw new ArgumentOutOfRangeException(nameof(segments), "Segments end in order.");
                previous = segments[i].UntilSeconds;
            }
            // A copy, so a caller cannot rewrite the script after a run has started reading it.
            _segments = (RouteSegment[])segments.Clone();
        }

        public int SegmentCount => _segments.Length;
        // The second the script runs out and the hero stands still.
        public float Seconds => _segments[_segments.Length - 1].UntilSeconds;

        public void Steer(RouteView view, out float steerX, out float steerY)
        {
            if (view == null)
                throw new ArgumentNullException(nameof(view));

            for (int i = 0; i < _segments.Length; i++)
            {
                if (view.Seconds >= _segments[i].UntilSeconds)
                    continue;
                steerX = _segments[i].SteerX;
                steerY = _segments[i].SteerY;
                return;
            }

            steerX = 0f;
            steerY = 0f;
        }
    }
}
