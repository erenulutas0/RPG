using Cryptforge.Combat;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // The Gloam Gardens' own enemies in the scene (docs/52): today the Cinder Bloom, floor 3's elite (BloomRule, CinderBloom;
    // `bloom-m20-h70-r1.2`, the lead's decisions 2026-11-15). Each running frame every standing Bloom's clock runs (not on the
    // frame it walks in); while open the Bloom is rooted (EncounterController.SetWalkShare, BloomRule.SpeedOf: its pack's
    // next step walks it at 0) and its maul is held (AttackController.IsHeld: no tick, no strike, as the Warden's through its
    // slam); closed, it walks and mauls as authored. The clock holds whenever the encounter holds - an open choice, the
    // boss's reveal, the player's pause - as the Warden's slams, the burns and the Sentry's mirror do. Only on the foundry:
    // off it no Bloom is ever stepped, so a hall arena's Bloom fights as authored.
    //
    // It runs after MirrorGuardService (-39), WardenSlamService (-40) and EncounterController (-50), which moves the packs
    // and brings each wave in, and before every script of the default order, every AttackController among them: an open
    // Bloom's maul is held from the very frame its window opens, and a landing of the frame meets the bark as the clock
    // stands - the point of the frame where the foundry simulation steps it (DescentSimulation.Gloam.cs, StepGloam after
    // StepVaults; FoundrySimulationSceneValueTests.TheScenesScriptOrderIsTheSimulations).
    [DefaultExecutionOrder(-38)]
    public sealed class GloamService : MonoBehaviour
    {
        private CombatSetup _setup;
        private Health _hero;

        public bool IsActive { get; private set; }

        internal void Initialize(CombatSetup setup, Health hero)
        {
            _setup = setup;
            _hero = hero;
            var arena = FindFirstObjectByType<ArenaView>();
            IsActive = arena != null && arena.IsFoundryRegion;
        }

        private void Update()
        {
            if (!IsActive || _setup == null || _setup.Run == null || _setup.Run.HasEnded)
                return;
            EncounterController encounter = _setup.Encounters;
            if (Time.deltaTime <= 0f || _setup.Choices.IsOpen || encounter.IsRevealHeld)
                return;
            int frame = Time.frameCount;
            for (int i = 0; i < CinderBloom.StandingCount; i++)
            {
                CinderBloom bloom = CinderBloom.StandingAt(i);
                Health body = bloom.Body;
                if (bloom.StoodUpFrame == frame || body == null || !body.IsAlive ||
                    !encounter.TryFloorPositionOf(body, out _, out _))
                    continue;
                bloom.Clock.Step(Time.deltaTime);
                bloom.IsCycling = true;
                bloom.Hero = _hero;
                bool open = bloom.Clock.IsOpen;
                encounter.SetWalkShare(body, bloom.Clock.Speed(1f));
                if (bloom.TryGetComponent(out AttackController maul))
                    maul.IsHeld = open;
            }
        }
    }
}
