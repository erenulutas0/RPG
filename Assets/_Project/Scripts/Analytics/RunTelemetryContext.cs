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
        // The Relic Forge's shop; null when a test or scene has no forge. It sells relics alone: the weapon rack is gone
        // (docs/63).
        public RelicShop Relics;
        // The run's loot and the Anvil (docs/54); either may be null, and a run without them writes none of their lines.
        public Economy.RunLoot Loot;
        public Anvil Anvil;

        // Stable content ids of the run's loadout. RelicId is null when no relic is equipped. AbilityId is skill 1's, the
        // hero's own; SkillTwoId and SkillThreeId the skills the run carries in slots 2 and 3 (docs/57: forged at the Forge,
        // or development/skills.txt), null for a slot it does not carry.
        public string HeroId;
        public string WeaponId;
        public string RelicId;
        public string AbilityId;
        public string SkillTwoId;
        public string SkillThreeId;

        // PlayerProfile.Gold and DeepestFloorCleared as they were when the run started.
        public int ForgeGold;
        public int DeepestFloorCleared;
        // RunState.Seed, recorded so the run's offers can be reproduced from the log.
        public int Seed;
        // The floor the run begins on: 1 (or 0, read as 1), or the deeper map the player chose (MapChoiceView); run_start
        // writes it as start_floor only when it is deeper than 1.
        public int StartFloor;
        // What the run wears (docs/54 phase 5): run_start writes gear_power and gear_levels when it wears anything that
        // changes the run (GearLoadout.IsEmpty false); null or empty for an ungeared run, whose run_start keeps its keys.
        public GearLoadout Gear;
        // The difficulty tier the run is played on (docs/54 section 21): run_start writes it as tier when it is not Forged.
        public DifficultyTier Tier;

        // The id of the enemy that last hit the hero, or null; read once when a defeat ends the run.
        public Func<string> KillerId;
        // The hero's current and maximum health, read together wherever an event reports health, so a log can read it as a
        // share of a maximum that health cards raise.
        public Func<float> HeroHealth;
        public Func<float> HeroMaximumHealth;
        // Pulses the hero's weapons have fired, for run_end: every slot's in category mode (RunTelemetry.PulsesFired).
        // Optional: a scene without a pulse reports none.
        public Func<int> Pulses;
    }
}
