using System.Runtime.CompilerServices;

namespace Cryptforge.Tests
{
    // The Descent's length and the finale's heal (docs/47, research 2026-11-11): two research levers on the finale's
    // opening, off by default and read by this simulation alone - nothing of the scene heals the hero between floor 2's
    // Warden and the Final Swarm. FoundryLevers.FinaleHealShare heals a share of the hero's maximum as the swarm opens
    // (a Mend at the checkpoint, say); FoundryLevers.FinaleChestAtArrival moves the finale's own chest (ChestRule's room-0
    // chest, a quarter heal) from ChestRule.SpotFor to where the hero arrives, so the hero stands on it. Floor 2's later
    // gate and a third floor need no lever: DescentLengthResearch builds the floors' chain itself (FoundryFloor's knobs and
    // Next) and plays it through RunFoundry's floor.
    internal static partial class DescentSimulation
    {
        internal sealed partial class FoundryLevers
        {
            // Research only (docs/47): a heal of this share of the hero's maximum as the finale's swarm opens; 0 is off.
            public float FinaleHealShare;
            // Research only (docs/47): the finale's chest placed where the hero arrives; off, it stands at ChestRule.SpotFor.
            public bool FinaleChestAtArrival;
        }

        internal sealed partial class FoundryResult
        {
            // The health FoundryLevers.FinaleHealShare gave as the swarm opened (after SwarmEntryHealth was read); 0 without.
            public float SwarmHealed;
        }

        // The finale's opening heal: the share of the maximum, in one body.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static float FinaleHealOf(float maximum, float share) => share > 0f ? maximum * share : 0f;

        private sealed partial class FoundryRun
        {
            // EncounterController.BeginSwarm's research hooks, after the hero's entry is read and the room's chest placed:
            // the opening heal, and the chest moved under the hero's feet. Neither does anything at the default levers.
            private void FinaleArrival()
            {
                if (_levers.FinaleHealShare > 0f)
                {
                    float before = _heroHealth.Current;
                    _heroHealth.Heal(FinaleHealOf(_heroHealth.Maximum, _levers.FinaleHealShare));
                    _result.SwarmHealed = _heroHealth.Current - before;
                }
                if (_levers.FinaleChestAtArrival && _chestPlaced && !_chestOpen)
                {
                    _chestX = HeroX;
                    _chestY = HeroY;
                }
            }
        }
    }
}
