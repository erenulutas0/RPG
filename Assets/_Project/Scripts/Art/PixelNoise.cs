namespace Cryptforge.Art
{
    // Deterministic per-pixel noise for stone grain, star fields and flicker: the same seed and coordinates always give
    // the same value, on every platform, so generated art never differs between the Editor, the phone and the tests.
    public static class PixelNoise
    {
        // A value in [0, 1) for the cell.
        public static float Value(int x, int y, int seed)
        {
            uint h = Hash(x, y, seed);
            return (h & 0xFFFFFF) / 16777216f;
        }

        // True with the given probability.
        public static bool Chance(int x, int y, int seed, float probability) => Value(x, y, seed) < probability;

        // An integer in [0, count).
        public static int Pick(int x, int y, int seed, int count) => count <= 0 ? 0 : (int)(Hash(x, y, seed) % (uint)count);

        private static uint Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint h = (uint)seed * 0x9E3779B1u;
                h ^= (uint)x * 0x85EBCA6Bu;
                h = (h << 13) | (h >> 19);
                h ^= (uint)y * 0xC2B2AE35u;
                h *= 0x27D4EB2Fu;
                h ^= h >> 15;
                h *= 0x165667B1u;
                h ^= h >> 13;
                return h;
            }
        }
    }
}
