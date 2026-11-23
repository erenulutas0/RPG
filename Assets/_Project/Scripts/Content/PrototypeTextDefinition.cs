using Cryptforge.Localisation;
using UnityEngine;

namespace Cryptforge.Content
{
    // The game's own words. Each getter reads its field through Loc (docs/60): the field is the English - the asset's
    // value, or the initializer below where the asset has none - and the key is prototype.<the field in snake_case>, e.g.
    // prototype.title, prototype.exit_instruction; the string table carries the Turkish. Content display names use <id>.name
    // (ContentKeys). A new field needs its getter through Loc.Text and a row in Resources/Localisation/Strings.txt
    // (LocalisationAssetTests read every getter).
    [CreateAssetMenu(menuName = "Cryptforge/Prototype Text")]
    public sealed class PrototypeTextDefinition : ScriptableObject
    {
        [SerializeField] private string _title;
        [SerializeField] private string _exitInstruction = "EXIT - Stand still";
        [SerializeField] private string _exitDirection = "EXIT";
        public string ExitInstruction => Loc.Text("prototype.exit_instruction", _exitInstruction);
        public string ExitDirection => Loc.Text("prototype.exit_direction", _exitDirection);
        [SerializeField] private string _gateInstruction = "GATE - Stand still to climb";
        [SerializeField] private string _gateDirection = "GATE";
        public string GateInstruction => Loc.Text("prototype.gate_instruction", _gateInstruction);
        public string GateDirection => Loc.Text("prototype.gate_direction", _gateDirection);
        [SerializeField] private string _hallInstruction = "NEXT HALL - Step in";
        [SerializeField] private string _hallDirection = "NEXT HALL";
        public string HallInstruction => Loc.Text("prototype.hall_instruction", _hallInstruction);
        public string HallDirection => Loc.Text("prototype.hall_direction", _hallDirection);
        [SerializeField] private string _altarInstruction = "ALTAR - Step in: a power, +10% danger";
        public string AltarInstruction => Loc.Text("prototype.altar_instruction", _altarInstruction);
        [SerializeField] private string _dangerFormat = "Danger +{0}%";
        // {0} how much harder the run's enemies are than authored, in whole percent: 10 at difficulty 1.1.
        public string DangerFormat => Loc.Text("prototype.danger_format", _dangerFormat);
        // The floor's clock under the top panel (HordeClock, the owner's five-minute floor): {0} minutes and {1} seconds
        // left before the floor is overrun; once it is, how long ago.
        [SerializeField] private string _floorClockFormat = "{0}:{1:00}";
        [SerializeField] private string _overrunFormat = "OVERRUN +{0}:{1:00}";
        // Said by the floor clock while the cleared room before the gate holds for the floor's clock.
        [SerializeField] private string _gateOpensFormat = "Gate opens in {0}:{1:00}";
        public string FloorClockFormat => Loc.Text("prototype.floor_clock_format", _floorClockFormat);
        // The hero chooser on the Relic Forge panel (docs/45): {0} the hero, {1} the character's own name or, while a hero
        // is locked, HeroLockedFormat - {0} that hero, {1} how to unlock it.
        [SerializeField] private string _heroChoiceFormat = "{0}\n<size=20>{1}</size>";
        [SerializeField] private string _heroLockedFormat = "{0}: {1}";
        public string HeroChoiceFormat => Loc.Text("prototype.hero_choice_format", _heroChoiceFormat);
        public string HeroLockedFormat => Loc.Text("prototype.hero_locked_format", _heroLockedFormat);
        public string OverrunFormat => Loc.Text("prototype.overrun_format", _overrunFormat);
        public string GateOpensFormat => Loc.Text("prototype.gate_opens_format", _gateOpensFormat);
        // The loadout badges on the foundry's top panel (docs/42 section 4). {0} a weapon's or tome's short name; the
        // evolved form marks a weapon that has taken its evolution.
        [SerializeField] private string _loadoutEvolvedFormat = "{0}+";
        public string LoadoutEvolvedFormat => Loc.Text("prototype.loadout_evolved_format", _loadoutEvolvedFormat);
        // The kind line on a category-mode card, so a pick says what it adds to the build before its effect does.
        [SerializeField] private string _cardNewWeapon = "NEW WEAPON";
        [SerializeField] private string _cardNewTome = "NEW TOME";
        [SerializeField] private string _cardLevelUpFormat = "Lv {0} -> {1}";
        [SerializeField] private string _cardEvolution = "EVOLUTION";
        public string CardNewWeapon => Loc.Text("prototype.card_new_weapon", _cardNewWeapon);
        public string CardNewTome => Loc.Text("prototype.card_new_tome", _cardNewTome);
        // {0} the carried weapon's or held tome's level now, {1} the level the card takes it to.
        public string CardLevelUpFormat => Loc.Text("prototype.card_level_up_format", _cardLevelUpFormat);
        public string CardEvolution => Loc.Text("prototype.card_evolution", _cardEvolution);
        [SerializeField] private string _floorProgressFormat;
        [SerializeField] private string _waveFormat;
        [SerializeField] private string _victoryFormat;
        [SerializeField] private string _enragedFormat;
        [SerializeField] private string _forgeStatus;
        [SerializeField] private string _healthFormat;
        [SerializeField] private string _packHealthFormat;
        [SerializeField] private string _packVictoryFormat;
        [SerializeField] private string _weaponFormat;
        [SerializeField] private string _goldFormat;
        [SerializeField] private string _experienceFormat;
        [SerializeField] private string _upgradeChoiceTitle;
        [SerializeField] private string _forgeChoiceTitle;
        [SerializeField] private string _checkpointChoiceTitle;
        [SerializeField] private string _extractName;
        [SerializeField] private string _extractDescriptionFormat;
        [SerializeField] private string _descendName;
        [SerializeField] private string _descendDescriptionFormat;
        [SerializeField] private string _noFloorModifier;
        [SerializeField] private string _resultVictoryTitle;
        [SerializeField] private string _resultExtractedTitle;
        [SerializeField] private string _resultDefeatTitle;
        [SerializeField] private string _resultVictoryCauseFormat;
        [SerializeField] private string _resultExtractedCauseFormat;
        [SerializeField] private string _resultDefeatCauseFormat;
        [SerializeField] private string _resultFinaleCauseFormat = "{0} walked out of {1}";
        [SerializeField] private string _resultPitCauseFormat = "{0} was burnt by the pit's eruption in {1}";
        [SerializeField] private string _resultProgressFormat;
        [SerializeField] private string _resultGoldFormat;
        [SerializeField] private string _resultGoldLostFormat;
        // docs/54 phase 3: the run's loot as the build label's last line, plain text until the Anvil's screen (phase 4).
        [SerializeField] private string _resultLootFormat = "Loot: {0}";
        [SerializeField] private string _resultLootLostFormat = "Loot: {0}  |  lost: {1}";
        [SerializeField] private string _resultBuildFormat;
        [SerializeField] private string _resultUpgradeFormat;
        [SerializeField] private string _resultNoUpgrades;
        [SerializeField] private string _restartLabel;
        [SerializeField] private string _relicFormat;
        [SerializeField] private string _relicTriggeredFormat;
        [SerializeField] private string _resultRelicFormat;
        [SerializeField] private string _forgeHintReadyFormat;
        [SerializeField] private string _forgeHintNextFormat;
        [SerializeField] private string _forgeHintRelicsCompleteFormat = "Forge gold {0}: every relic is forged";
        [SerializeField] private string _forgeButtonLabel;
        [SerializeField] private string _forgeTitle;
        [SerializeField] private string _forgeStatusFormat;
        [SerializeField] private string _forgeWeaponsHeader;
        [SerializeField] private string _forgeOwnWeaponLabel = "Signature weapon";
        [SerializeField] private string _forgeRelicsHeader;
        [SerializeField] private string _unlockForgeFormat;
        [SerializeField] private string _unlockNeedGoldFormat;
        [SerializeField] private string _unlockOwnedLabel;
        [SerializeField] private string _unlockEquippedLabel;
        [SerializeField] private string _startRunLabel;
        [SerializeField] private string _pauseButtonLabel;
        [SerializeField] private string _pauseTitle;
        [SerializeField] private string _pauseHint;
        [SerializeField] private string _resumeLabel;

        public string Title => Loc.Text("prototype.title", _title);
        // {0} floor number, {1} room number, {2} room count, {3} room name.
        public string FloorProgressFormat => Loc.Text("prototype.floor_progress_format", _floorProgressFormat);
        // {0} wave number, {1} wave count.
        public string WaveFormat => Loc.Text("prototype.wave_format", _waveFormat);
        // {0} enemy name, {1} hits this fight, {2} clear time in seconds.
        public string VictoryFormat => Loc.Text("prototype.victory_format", _victoryFormat);
        // {0} enemy name.
        public string EnragedFormat => Loc.Text("prototype.enraged_format", _enragedFormat);
        public string ForgeStatus => Loc.Text("prototype.forge_status", _forgeStatus);
        public string HealthFormat => Loc.Text("prototype.health_format", _healthFormat);
        // {0} targeted enemy name, {1} its health, {2} its maximum, {3} other enemies still alive in the pack.
        public string PackHealthFormat => Loc.Text("prototype.pack_health_format", _packHealthFormat);
        // {0} enemies in the wave, {1} hits on the pack, {2} clear time in seconds.
        public string PackVictoryFormat => Loc.Text("prototype.pack_victory_format", _packVictoryFormat);
        public string WeaponFormat => Loc.Text("prototype.weapon_format", _weaponFormat);
        // {0} gold this run, {1} gold at risk since the last checkpoint.
        public string GoldFormat => Loc.Text("prototype.gold_format", _goldFormat);
        public string ExperienceFormat => Loc.Text("prototype.experience_format", _experienceFormat);
        public string UpgradeChoiceTitle => Loc.Text("prototype.upgrade_choice_title", _upgradeChoiceTitle);
        public string ForgeChoiceTitle => Loc.Text("prototype.forge_choice_title", _forgeChoiceTitle);
        public string CheckpointChoiceTitle => Loc.Text("prototype.checkpoint_choice_title", _checkpointChoiceTitle);
        public string ExtractName => Loc.Text("prototype.extract_name", _extractName);
        // {0} gold this run.
        public string ExtractDescriptionFormat => Loc.Text("prototype.extract_description_format", _extractDescriptionFormat);
        public string DescendName => Loc.Text("prototype.descend_name", _descendName);
        // {0} gold this run, {1} next floor name, {2} next floor modifier description.
        public string DescendDescriptionFormat => Loc.Text("prototype.descend_description_format", _descendDescriptionFormat);
        public string NoFloorModifier => Loc.Text("prototype.no_floor_modifier", _noFloorModifier);
        public string ResultVictoryTitle => Loc.Text("prototype.result_victory_title", _resultVictoryTitle);
        public string ResultExtractedTitle => Loc.Text("prototype.result_extracted_title", _resultExtractedTitle);
        public string ResultDefeatTitle => Loc.Text("prototype.result_defeat_title", _resultDefeatTitle);
        // {0} hero name, {1} boss name, {2} floor name.
        public string ResultVictoryCauseFormat => Loc.Text("prototype.result_victory_cause_format", _resultVictoryCauseFormat);
        // {0} hero name, {1} cleared floor name.
        public string ResultExtractedCauseFormat => Loc.Text("prototype.result_extracted_cause_format", _resultExtractedCauseFormat);
        // {0} hero name, {1} enemy name, {2} room name.
        public string ResultDefeatCauseFormat => Loc.Text("prototype.result_defeat_cause_format", _resultDefeatCauseFormat);
        // A victory with no boss to name - the Descent's finale, left by its exit with its pack gone with the room:
        // {0} hero name, {1} floor name.
        public string ResultFinaleCauseFormat => Loc.Text("prototype.result_finale_cause_format", _resultFinaleCauseFormat);
        // A defeat whose last blow was the foundry pit's eruption, which no enemy dealt: {0} hero name, {1} room name (the
        // floor's while no room stands).
        public string ResultPitCauseFormat => Loc.Text("prototype.result_pit_cause_format", _resultPitCauseFormat);
        // {0} floor number reached, {1} rooms cleared in total, {2} level, {3} experience.
        public string ResultProgressFormat => Loc.Text("prototype.result_progress_format", _resultProgressFormat);
        // {0} gold banked.
        public string ResultGoldFormat => Loc.Text("prototype.result_gold_format", _resultGoldFormat);
        // {0} gold banked, {1} gold lost.
        public string ResultGoldLostFormat => Loc.Text("prototype.result_gold_lost_format", _resultGoldLostFormat);
        // {0} the loot banked (LootText), {1} the loot a death took; "nothing" stands for an empty list beside a loss.
        public string ResultLootFormat => Loc.Text("prototype.result_loot_format", _resultLootFormat);
        public string ResultLootLostFormat => Loc.Text("prototype.result_loot_lost_format", _resultLootLostFormat);
        // {0} weapon name, {1} the relic and upgrade entries joined, or the no-upgrades text.
        public string ResultBuildFormat => Loc.Text("prototype.result_build_format", _resultBuildFormat);
        // {0} upgrade name, {1} stacks.
        public string ResultUpgradeFormat => Loc.Text("prototype.result_upgrade_format", _resultUpgradeFormat);
        public string ResultNoUpgrades => Loc.Text("prototype.result_no_upgrades", _resultNoUpgrades);
        public string RestartLabel => Loc.Text("prototype.restart_label", _restartLabel);
        // {0} relic name.
        public string RelicFormat => Loc.Text("prototype.relic_format", _relicFormat);
        // {0} relic name, {1} times it has triggered this run.
        public string RelicTriggeredFormat => Loc.Text("prototype.relic_triggered_format", _relicTriggeredFormat);
        // {0} relic name, {1} times it triggered; listed first in the result build.
        public string ResultRelicFormat => Loc.Text("prototype.result_relic_format", _resultRelicFormat);
        // {0} profile gold, {1} affordable relic name.
        public string ForgeHintReadyFormat => Loc.Text("prototype.forge_hint_ready_format", _forgeHintReadyFormat);
        // {0} profile gold, {1} cheapest unowned relic name, {2} its price.
        public string ForgeHintNextFormat => Loc.Text("prototype.forge_hint_next_format", _forgeHintNextFormat);
        // Every relic forged: the forge sells nothing else since its weapon rack went (docs/63). {0} profile gold.
        public string ForgeHintRelicsCompleteFormat => Loc.Text("prototype.forge_hint_relics_complete_format", _forgeHintRelicsCompleteFormat);
        public string ForgeButtonLabel => Loc.Text("prototype.forge_button_label", _forgeButtonLabel);
        public string ForgeTitle => Loc.Text("prototype.forge_title", _forgeTitle);
        // {0} profile gold, {1} deepest floor cleared.
        public string ForgeStatusFormat => Loc.Text("prototype.forge_status_format", _forgeStatusFormat);
        // The header over the next run's hero's own weapon (docs/63), and the state line of its card.
        public string ForgeWeaponsHeader => Loc.Text("prototype.forge_weapons_header", _forgeWeaponsHeader);
        public string ForgeOwnWeaponLabel => Loc.Text("prototype.forge_own_weapon_label", _forgeOwnWeaponLabel);
        public string ForgeRelicsHeader => Loc.Text("prototype.forge_relics_header", _forgeRelicsHeader);
        // A relic card's states. {0} price.
        public string UnlockForgeFormat => Loc.Text("prototype.unlock_forge_format", _unlockForgeFormat);
        // {0} price, {1} gold still needed.
        public string UnlockNeedGoldFormat => Loc.Text("prototype.unlock_need_gold_format", _unlockNeedGoldFormat);
        public string UnlockOwnedLabel => Loc.Text("prototype.unlock_owned_label", _unlockOwnedLabel);
        public string UnlockEquippedLabel => Loc.Text("prototype.unlock_equipped_label", _unlockEquippedLabel);
        public string StartRunLabel => Loc.Text("prototype.start_run_label", _startRunLabel);
        public string PauseButtonLabel => Loc.Text("prototype.pause_button_label", _pauseButtonLabel);
        public string PauseTitle => Loc.Text("prototype.pause_title", _pauseTitle);
        public string PauseHint => Loc.Text("prototype.pause_hint", _pauseHint);
        public string ResumeLabel => Loc.Text("prototype.resume_label", _resumeLabel);
    }
}
