using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // Which way a cut-out puppet faces, and how a change of facing is shown (docs/45 section 4 B). The facing the travel
    // or a target asks for is taken at once, so nothing that reads it ever waits on the art; the figure on show follows
    // it through a short turn instead of cutting to it: it narrows, swaps to the new facing at its narrowest and widens
    // again, the head a little ahead of the body, so a turn reads as a turn. Presentation only; PuppetAnimator and
    // Tools/ArtReview's preview driver share it, pure and allocation-free.
    public sealed class PuppetFacing
    {
        // A turn's length: under a tenth of a second, three frames to the swap at 60 frames a second, so it never reads
        // as a delay, but long enough to see.
        public const float TurnSeconds = 0.1f;
        // The figure's width at the swap: narrow enough that the facings trade places almost edge on, not so narrow
        // that it reads as a card flipping.
        public const float NarrowestWidth = 0.35f;
        // How far ahead of the body's turn the head's runs, as a share of the turn: it looks round first.
        public const float HeadLead = 0.18f;
        // Each axis keeps its facing inside this share of the travel's direction, so nearly level or nearly upright
        // travel never flickers between facings.
        public const float DeadBand = 0.15f;

        private const float Pi = 3.14159274f;

        private float _progress;
        // +1 turning toward the facing asked for, -1 turning back to the one on show before it ever swapped.
        private float _direction;
        private bool _turning;

        // The facing asked for: toward the camera (the front three-quarter) or away, and mirrored for leftward travel.
        public bool Front { get; private set; }
        public bool Mirrored { get; private set; }
        // The facing on show, which swaps to the one asked for halfway through a turn.
        public bool ShownFront { get; private set; }
        public bool ShownMirrored { get; private set; }
        public bool IsTurning => _turning;
        // How far through the turn the figure is, 0 to 1; 0 when none is under way.
        public float Progress => _progress;

        // The figure's width as a share of its own: 1 at rest, NarrowestWidth at the swap.
        public float Width => _turning ? Narrow(_progress) : 1f;

        // The head's width, on its own lead.
        public float HeadWidth => _turning ? Narrow(HeadProgress) : 1f;

        // Whether the head already shows the mirror the body is turning to, a moment before the body swaps.
        public bool HeadMirrored =>
            _turning && _direction > 0f && _progress < 0.5f && HeadProgress >= 0.5f && Mirrored != ShownMirrored;

        private float HeadProgress
        {
            get
            {
                float head = _progress + _direction * HeadLead;
                return head < 0f ? 0f : head > 1f ? 1f : head;
            }
        }

        // Faces the way the travel runs (floor units: across the screen, up it). Inside the dead band an axis keeps its
        // facing. Returns whether the facing asked for changed.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Face(float x, float y)
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            if (!(length > 1e-4f) || float.IsInfinity(length))
                return false;
            x /= length;
            y /= length;
            bool mirrored = Mirrored;
            bool front = Front;
            if (Math.Abs(x) > DeadBand)
                mirrored = x < 0f;
            if (Math.Abs(y) > DeadBand)
                front = y < 0f;
            return Turn(front, mirrored);
        }

        // Asks for a facing outright. A turn starts toward it; one already under way turns back if this is the facing
        // on show and it has not swapped yet, and heads on to it if it has swapped to another.
        public bool Turn(bool front, bool mirrored)
        {
            if (front == Front && mirrored == Mirrored)
                return false;
            Front = front;
            Mirrored = mirrored;
            bool shown = front == ShownFront && mirrored == ShownMirrored;
            if (!_turning)
            {
                if (shown)
                    return true;
                _turning = true;
                _progress = 0f;
                _direction = 1f;
            }
            else if (_progress < 0.5f)
                _direction = shown ? -1f : 1f;
            else if (!shown)
            {
                // Swapped already and widening: narrow again from the same width toward the newer facing.
                _progress = 1f - _progress;
                _direction = 1f;
            }
            return true;
        }

        // One frame of a turn. Returns true on the frame the facing on show swaps to the one asked for.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Step(float dt)
        {
            if (!_turning || !(dt > 0f) || float.IsInfinity(dt))
                return false;
            float before = _progress;
            _progress += _direction * dt / TurnSeconds;
            bool swapped = false;
            if (_direction > 0f && before < 0.5f && _progress >= 0.5f)
            {
                swapped = ShownFront != Front || ShownMirrored != Mirrored;
                ShownFront = Front;
                ShownMirrored = Mirrored;
            }
            if (_progress >= 1f || _progress <= 0f)
            {
                // Done, or turned back before the swap: whatever is asked for now and not yet shown starts afresh.
                _turning = false;
                _progress = 0f;
                _direction = 0f;
                if (ShownFront != Front || ShownMirrored != Mirrored)
                {
                    _turning = true;
                    _direction = 1f;
                }
            }
            return swapped;
        }

        // Shows the facing asked for at once, with no turn: a hero put back to standing.
        public void Settle()
        {
            ShownFront = Front;
            ShownMirrored = Mirrored;
            _turning = false;
            _progress = 0f;
            _direction = 0f;
        }

        // The width over a turn: narrowing to NarrowestWidth at its middle on a sine, quick through the swap.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Narrow(float progress) =>
            1f - (1f - NarrowestWidth) * (float)Math.Sin(progress * Pi);
    }
}
