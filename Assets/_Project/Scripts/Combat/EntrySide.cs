using System;

namespace Cryptforge.Combat
{
    // The four corners of the arena a pack can enter from: far is straight ahead of the hero (up the screen), right and
    // left are the side corners, near is below the hero.
    public enum EntrySide
    {
        Far = 0,
        Right = 1,
        Near = 2,
        Left = 3
    }

    // How a wave spreads its enemies over the corners: slot by slot round the arena, starting one corner further for each
    // wave, so a pack of four comes from every side at once and the packs of a floor circle the hero. Pure, so the scene
    // and the Descent simulation place every enemy identically.
    public static class EntrySides
    {
        public const int Count = 4;

        // The unit vector on the floor from the hero toward the side's corner.
        public static void Outward(EntrySide side, out float x, out float y)
        {
            switch (side)
            {
                case EntrySide.Far:
                    x = 0f;
                    y = 1f;
                    break;
                case EntrySide.Right:
                    x = 1f;
                    y = 0f;
                    break;
                case EntrySide.Near:
                    x = 0f;
                    y = -1f;
                    break;
                case EntrySide.Left:
                    x = -1f;
                    y = 0f;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side));
            }
        }

        // The unit vector a formation's lateral offset runs along: the outward vector turned a quarter turn clockwise, so
        // the far side's lateral is +x, as the formations were first laid out.
        public static void Lateral(EntrySide side, out float x, out float y)
        {
            Outward(side, out float outX, out float outY);
            x = outY;
            y = -outX;
        }

        public static EntrySide SideOf(int waveOrdinal, int slot)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));
            if (slot < 0)
                throw new ArgumentOutOfRangeException(nameof(slot));
            return (EntrySide)((waveOrdinal + slot) % Count);
        }

        // The side of a wave's slot, with the slot's index among the enemies that share the side and how many they are,
        // so PackLayout can form them up on their own corner.
        public static void Formation(int waveOrdinal, int slot, int count, out EntrySide side, out int indexOnSide, out int countOnSide)
        {
            if (count < 1 || slot >= count)
                throw new ArgumentOutOfRangeException(nameof(slot));

            side = SideOf(waveOrdinal, slot);
            indexOnSide = slot / Count;
            countOnSide = (count - slot % Count + Count - 1) / Count;
        }
    }
}
