using System;

namespace Cryptforge.Progression
{
    // The run's source of randomness, and the only one the game may use for anything that must reproduce: which cards
    // a level-up offers, later what a chest holds. Integer arithmetic only, so the Editor's Mono, the pure .NET runner
    // and the phone's IL2CPP cannot disagree about a single draw - a lesson paid for once already, when two copies of a
    // float expression in two assemblies struck different enemies (docs/21, the movement budget). System.Random is not
    // used because its sequence is an implementation detail that has differed between runtimes.
    //
    // Streams are derived, never consumed in sequence: Stream(seed, purpose, index) seeds a generator from all three, so
    // the third level-up of a run always sees the same cards whether or not a chest was opened before it. Each consumer
    // owns a purpose constant and counts its own index.
    public sealed class RunRandom
    {
        public const int Offers = 1;
        public const int Chests = 2;
        public const int Crits = 3;

        private uint _state;

        private RunRandom(uint state)
        {
            _state = state;
        }

        public static RunRandom Stream(int seed, int purpose, int index)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            uint state = Mix(unchecked((uint)seed));
            state = Mix(state ^ Mix(unchecked((uint)purpose * 0x9E3779B9u)));
            state = Mix(state ^ Mix(unchecked((uint)index * 0x85EBCA6Bu)));
            return new RunRandom(state);
        }

        // The next 32 bits. SplitMix over a Weyl sequence: every state yields a full-period, well-mixed word.
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
