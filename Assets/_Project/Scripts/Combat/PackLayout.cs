using System;

namespace Cryptforge.Combat
{
    // Where each enemy of a wave enters the arena, as floor offsets from the pack's entry point in spacing units; +Y points
    // away from the hero. The first enemies take the front slots, so a small pack stays centred and a large one fans out
    // behind it: front pair, flanks, rear flanks, then a rear centre slot for the heaviest enemy. Eight to ten enemies form
    // staggered ranks 1.2 deep instead, so each enemy stands in the gap of the rank ahead: the front pair, a centre with
    // two flanks, a middle pair, then a rear centre (eight), a narrower rear pair (nine) or both (ten). Every formation is
    // symmetric about the centre line, keeps its slots at least 1.1 spacing units apart and at most 4 deep, so a whole wave
    // forming up on one corner still starts apart and inside the platform.
    public static class PackLayout
    {
        public const int MaxPackSize = 10;
        // The widest slot's distance from the centre line, in spacing units.
        public const float HalfWidth = 2f;

        private static readonly float[][] SlotX =
        {
            new[] { 0f },
            new[] { -0.9f, 0.9f },
            new[] { 0f, -1.4f, 1.4f },
            new[] { -0.9f, 0.9f, -2f, 2f },
            new[] { 0f, -1.4f, 1.4f, -0.8f, 0.8f },
            new[] { -0.9f, 0.9f, -2f, 2f, -0.9f, 0.9f },
            new[] { -0.9f, 0.9f, -2f, 2f, -2f, 2f, 0f },
            new[] { -0.9f, 0.9f, 0f, -1.8f, 1.8f, -0.9f, 0.9f, 0f },
            new[] { -0.9f, 0.9f, 0f, -1.8f, 1.8f, -0.9f, 0.9f, -1.6f, 1.6f },
            new[] { -0.9f, 0.9f, 0f, -1.8f, 1.8f, -0.9f, 0.9f, -1.6f, 1.6f, 0f }
        };

        private static readonly float[][] SlotY =
        {
            new[] { 0f },
            new[] { 0f, 0f },
            new[] { 0f, 0.8f, 0.8f },
            new[] { 0f, 0f, 1.2f, 1.2f },
            new[] { 0f, 0.8f, 0.8f, 1.8f, 1.8f },
            new[] { 0f, 0f, 1.2f, 1.2f, 2.4f, 2.4f },
            new[] { 0f, 0f, 1.2f, 1.2f, 2.6f, 2.6f, 2.8f },
            new[] { 0f, 0f, 1.2f, 1.2f, 1.2f, 2.4f, 2.4f, 3.6f },
            new[] { 0f, 0f, 1.2f, 1.2f, 1.2f, 2.4f, 2.4f, 3.6f, 3.6f },
            new[] { 0f, 0f, 1.2f, 1.2f, 1.2f, 2.4f, 2.4f, 3.6f, 3.6f, 3.6f }
        };

        public static void Offset(int index, int count, float spacing, out float x, out float y)
        {
            if (count < 1 || count > MaxPackSize)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!(spacing > 0f) || float.IsInfinity(spacing))
                throw new ArgumentOutOfRangeException(nameof(spacing));

            x = SlotX[count - 1][index] * spacing;
            y = SlotY[count - 1][index] * spacing;
        }
    }
}
