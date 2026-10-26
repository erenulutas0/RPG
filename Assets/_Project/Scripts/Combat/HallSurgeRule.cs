using System;

namespace Cryptforge.Combat
{
    // The foundry's halls run on past their authored waves (owner's choice, 2026-10-25, after the pacing read of the
    // phone's log: floors of 83-126 s against a 240-360 s target, a hall's wave a 5 s beat). A floor names how many surge
    // waves each of its combat rooms gains on the foundry, after its authored ones. Surge k repeats one of the room's
    // later waves, counting back from its last, with k + 1 more of that wave's last enemy, so every surge is bigger than
    // the one before it, up to a full formation. Pure, so the waves a hall will send can be tested without a scene.
    public static class HallSurgeRule
    {
        // The authored wave surge k repeats.
        public static int SourceWave(int authoredWaves, int surge)
        {
            if (authoredWaves < 1)
                throw new ArgumentOutOfRangeException(nameof(authoredWaves));
            if (surge < 0)
                throw new ArgumentOutOfRangeException(nameof(surge));
            return authoredWaves - 1 - surge % authoredWaves;
        }

        // How many enemies surge k sends when the wave it repeats has sourceSize.
        public static int Size(int sourceSize, int surge) => Math.Min(PackLayout.MaxFormationSize, sourceSize + surge + 1);
    }
}
