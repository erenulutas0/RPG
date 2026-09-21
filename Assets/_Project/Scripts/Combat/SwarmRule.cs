using System;
using System.Runtime.CompilerServices;

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
        // How many times harder each wave is than the one before it. Geometric, not linear, and that is the point of it
        // (docs/27 decision 6, docs/34): the hero grows by multiplying - damage times cadence times crits - and a world
        // that grows by adding a fixed amount a wave can never catch a build that multiplies, whatever the amount. The
        // linear two per cent this replaced left the owner's own builds alive to waves fifty-eight to ninety-six (docs/33).
        //
        // Measured against the calibrated player over 144 runs a rate: 1.02 takes the Staff's median from 53.5 to 41 and
        // the runs past wave eighty from nine to one, and leaves the early game where it was (early deaths within noise).
        // 1.025 took the last of those runs too and made the first fifteen waves measurably deadlier, so it was refused.
        // 1.03 and above are walls.
        public const float GrowthPerWave = 1.02f;
        // How many seconds of standing in the room make one step of the ramp. The swarm has no waves any more (docs/27,
        // S6c), so it hardens by the clock: at 1.02 a step every three seconds the room doubles in difficulty about every
        // hundred and five seconds. Measured in docs/35 against the calibrated player entering the finale the way the game
        // will, at the end of the Descent with its build: the Staff's median survival lands at 196 seconds.
        public const float StepSeconds = 3f;

        // How long a fallen enemy's place stays empty before a fresh one walks in. Measured with the step (docs/35), not
        // fixed at one second as the specification first had it: at the end of the Descent one, two and two and a half
        // all pass, a one-second refill costing the Sword about twenty seconds of median survival and the Staff and the
        // Daggers over thirty. Two keeps the room from ever clearing while a kill still buys a breath.
        public const float RespawnSeconds = 2f;

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

        // What this wave multiplies the floor's enemy health and damage by: 1.0 on the first wave, so a swarm opens at
        // exactly the difficulty its floor authored, and GrowthPerWave times the wave before on every wave after.
        //
        // Repeated float multiplication rather than Math.Pow, and in one body that is never inlined. Math.Pow is a
        // library call whose last bit has differed between the Editor's Mono, the .NET runner and the phone's IL2CPP, and
        // this project has already paid once for two copies of float arithmetic disagreeing in the last bit (docs/21, the
        // targeting divergence). An IEEE multiply is the same on every runtime, and a swarm is a few hundred waves at most.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DifficultyAt(int waveOrdinal)
        {
            if (waveOrdinal < 0)
                throw new ArgumentOutOfRangeException(nameof(waveOrdinal));

            float difficulty = 1f;
            for (int i = 0; i < waveOrdinal; i++)
                difficulty *= GrowthPerWave;
            return difficulty;
        }

        // The difficulty an enemy spawning at this moment takes: the geometric ramp, one step per StepSeconds of standing in
        // the room. An enemy keeps the difficulty it was born with, so the crowd round a hero who stays keeps hardening.
        public static float DifficultyAtSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || seconds < 0f)
                throw new ArgumentOutOfRangeException(nameof(seconds));

            return DifficultyAt((int)(seconds / StepSeconds));
        }
    }
}
