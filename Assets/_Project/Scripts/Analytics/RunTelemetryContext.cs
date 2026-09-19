using System;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Analytics
{
    // Everything one run's recorder observes, gathered by the composition root. Plain fields so the scene can fill it in
    // one place; RunTelemetry copies and validates them once, so a later change here never reaches a recorder.
    public sealed class RunTelemetryContext
    {
        public TelemetrySession Session;
        public RunState Run;
        public RunPause Pause;
        public RunChoices Choices;
        public UpgradeService Upgrades;
        public ForgeService Forge;
        public CheckpointService Checkpoint;
        // The Relic Forge shops; either may be null when a test or scene has no forge.
        public RelicShop Relics;
        public WeaponShop Weapons;

        // Stable content ids of the run's loadout. RelicId is null when no relic is equipped.
        public string HeroId;
        public string WeaponId;
        public string RelicId;
        public string AbilityId;

        // PlayerProfile.Gold and DeepestFloorCleared as they were when the run started.
        public int ForgeGold;
        public int DeepestFloorCleared;
        // RunState.Seed, recorded so the run's offers can be reproduced from the log.
        public int Seed;

        // The id of the enemy that last hit the hero, or null; read once when a defeat ends the run.
        public Func<string> KillerId;
        // The hero's current health; read when a chest opens and when a room closes.
        public Func<float> HeroHealth;
    }
}
