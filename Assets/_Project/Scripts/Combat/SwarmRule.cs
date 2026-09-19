using System;

namespace Cryptforge.Combat
{
    // How a swarm room escalates. Pure, so the scene and the Descent simulation harden a wave identically.
    //
    // A swarm room authors its waves as a ladder - the ordinary WaveDefinition[] every combat room has - and hands them
    // out in order; once the ladder is spent its last wave repeats for as long as the player stays. That is the shape
    // docs/26 Part 6 measured: the pack grows from three to the ten the formations hold over about twelve waves, and
    // then it cannot grow again, because the arena holds ten enemies at a time. Past that point the only thing left to
    // escalate is each enemy, which is what the multiplier below does.
    //
    // The multiplier had to rise with the wave rather than sit at a constant. Measured flat over eighty waves and
    // twelve seeds: at 1.0 the Staff cleared every wave of every seed - an area weapon scales with a crowd, so there is
    // no ceiling without scaling - and at 1.5 every weapon died at wave seven, because the hero enters a swarm at level
    // one and the enemies start at their hardest. A ramp starts where the hero starts and hardens from there.
    public static class SwarmRule
    {
        // How much harder each wave is than the one before it, as a share of the enemy's authored health and damage.
        // Measured in docs/29 over sixteen seeds, three weapons and three walks: a flat room never kills a backing-off
        // hero at all, 0.05 and above cut the room down to about what a Descent already grants, and this is the rate at
        // which a swarm pays for itself - a couple of minutes, two to three times a Descent's level-ups, and nobody
        // immortal in forty-eight runs.
        public const float DifficultyPerWave = 0.02f;
        // How often the room asks whether to press on or leave. One is every wave, which is the plainest reading of
        // "leave when you like"; it is the thing to ask the owner about after playing, not something a simulation can
        // answer.
        public const int ExitEveryWaves = 1;

        // The wave of the room's ladder that this ordinal spawns. Past the ladder's end the last wave repeats, so a
        // swarm never runs out of enemies and never grows past what the arena can hold.
        public static int LadderIndex(int waveOrdinal, int waveCount)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));
            if (waveCount < 1)
                throw new ArgumentOutOfRangeException(nameof(waveCount));

            return waveOrdinal < waveCount ? waveOrdinal : waveCount - 1;
        }

        // What this wave multiplies the floor's enemy health and damage by. 1.0 on the first wave, so a swarm opens at
        // exactly the difficulty its floor authored, and rising from there.
        public static float DifficultyAt(int waveOrdinal)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));

            return 1f + waveOrdinal * DifficultyPerWave;
        }

        // Whether the room offers its leave-or-stay choice after the wave at this ordinal.
        public static bool OffersExitAfter(int waveOrdinal)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));

            return (waveOrdinal + 1) % ExitEveryWaves == 0;
        }
    }
}
