using System;

namespace Cryptforge.Arena
{
    // The arena's source of randomness: RunRandom's construction (SplitMix over a Weyl sequence, streams derived from
    // (seed, purpose, index), integer arithmetic only), carried in this assembly so it references nothing of the game's
    // and compiles alone - into the APK beside the game, and unchanged into a server module (docs/55 section 5.1).
    // Its draws never need to agree with a run's (the arena reads none of the run's streams), so this copy cannot break a
    // scene/simulation parity; ArenaRandomTests still pins it word for word to RunRandom, so the two never drift apart.
    // System.Random is not used: its sequence is an implementation detail that has differed between runtimes.
    public sealed class ArenaRandom
    {
        // The arena's purposes, one per consumer; each counts its own index.
        // A fighter's crit rolls, index the side.
        public const int Crits = 101;
        // A fighter's opening offset (its first swing and skills), index the side.
        public const int Openings = 102;
        // A round's seed from the match's, index the round.
        public const int Rounds = 103;
        // The boons dealt to a side in a round, index round x 2 + side.
        public const int Boons = 104;
        // A generated rival's build, index the rival.
        public const int Rivals = 105;
        // A ladder fight's seed, index the fight's number.
        public const int Fights = 106;

        private uint _state;

        private ArenaRandom(uint state)
        {
            _state = state;
        }

        public static ArenaRandom Stream(int seed, int purpose, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            uint state = Mix(unchecked((uint)seed));
            state = Mix(state ^ Mix(unchecked((uint)purpose * 0x9E3779B9u)));
            state = Mix(state ^ Mix(unchecked((uint)index * 0x85EBCA6Bu)));
            return new ArenaRandom(state);
        }

        public uint Next()
        {
            unchecked
            {
                _state += 0x9E3779B9u;
                return Mix(_state);
            }
        }

        // A value in [0, count), unbiased: draws that would favour the low residues are thrown away and drawn again.
        public int NextBelow(int count)
        {
            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));
            if (count == 1)
                return 0;

            uint bound = (uint)count;
            uint limit = uint.MaxValue - uint.MaxValue % bound;
            uint draw;
            do
            {
                draw = Next();
            }
            while (draw >= limit);
            return (int)(draw % bound);
        }

        // A non-negative int from the next word, for seeds.
        public int NextSeed() => (int)(Next() & 0x7FFFFFFFu);

        private static uint Mix(uint z)
        {
            unchecked
            {
                z = (z ^ (z >> 16)) * 0x85EBCA6Bu;
                z = (z ^ (z >> 13)) * 0xC2B2AE35u;
                return z ^ (z >> 16);
            }
        }
    }
}
