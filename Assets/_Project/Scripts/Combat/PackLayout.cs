using System;

namespace Cryptforge.Combat
{
    // Horizontal slots for one wave, in a single row in front of the hero. The first enemy takes the centre, the nearest
    // slot; with Targeting's earlier-wins tie-break the hero therefore always fights the first living enemy in order.
    public static class PackLayout
    {
        public const int MaxPackSize = 3;

        public static float OffsetX(int index, int count, float spacing)
        {
            if (count < 1 || count > MaxPackSize)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (index < 0 || index >= count)
                throw new ArgumentOutOfRangeException(nameof(index));
            if (!(spacing > 0f) || float.IsInfinity(spacing))
                throw new ArgumentOutOfRangeException(nameof(spacing));

            switch (count)
            {
                case 1:
                    return 0f;
                case 2:
                    return index == 0 ? -spacing / 2f : spacing / 2f;
                default:
                    return index == 0 ? 0f : index == 1 ? -spacing : spacing;
            }
        }
    }
}
