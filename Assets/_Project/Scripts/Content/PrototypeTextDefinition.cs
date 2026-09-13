using UnityEngine;

namespace Cryptforge.Content
{
    // Keys are prototype.<field name>, e.g. prototype.title. Content display names use <id>.name.
    // Keep text in data; a localization service is unnecessary for this single-language slice.
    [CreateAssetMenu(menuName = "Cryptforge/Prototype Text")]
    public sealed class PrototypeTextDefinition : ScriptableObject
    {
        [SerializeField] private string _title;
        [SerializeField] private string _floorProgressFormat;
        [SerializeField] private string _waveFormat;
        [SerializeField] private string _victoryFormat;
        [SerializeField] private string _enragedFormat;
        [SerializeField] private string _forgeStatus;
        [SerializeField] private string _healthFormat;
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
        [SerializeField] private string _resultProgressFormat;
        [SerializeField] private string _resultGoldFormat;
        [SerializeField] private string _resultGoldLostFormat;
        [SerializeField] private string _resultBuildFormat;
        [SerializeField] private string _resultUpgradeFormat;
        [SerializeField] private string _resultNoUpgrades;
        [SerializeField] private string _restartLabel;
        [SerializeField] private string _relicFormat;
        [SerializeField] private string _relicTriggeredFormat;
        [SerializeField] private string _resultRelicFormat;
        [SerializeField] private string _forgeHintReadyFormat;
        [SerializeField] private string _forgeHintNextFormat;
        [SerializeField] private string _forgeHintCompleteFormat;
        [SerializeField] private string _forgeButtonLabel;
        [SerializeField] private string _forgeTitle;
        [SerializeField] private string _forgeStatusFormat;
        [SerializeField] private string _relicForgeFormat;
        [SerializeField] private string _relicNeedGoldFormat;
        [SerializeField] private string _relicOwnedLabel;
        [SerializeField] private string _relicEquippedLabel;
        [SerializeField] private string _startRunLabel;

        public string Title => _title;
        // {0} floor number, {1} room number, {2} room count, {3} room name.
        public string FloorProgressFormat => _floorProgressFormat;
        // {0} wave number, {1} wave count.
        public string WaveFormat => _waveFormat;
        // {0} enemy name, {1} hits this fight, {2} clear time in seconds.
        public string VictoryFormat => _victoryFormat;
        // {0} enemy name.
        public string EnragedFormat => _enragedFormat;
        public string ForgeStatus => _forgeStatus;
        public string HealthFormat => _healthFormat;
        public string WeaponFormat => _weaponFormat;
        // {0} gold this run, {1} gold at risk since the last checkpoint.
        public string GoldFormat => _goldFormat;
        public string ExperienceFormat => _experienceFormat;
        public string UpgradeChoiceTitle => _upgradeChoiceTitle;
        public string ForgeChoiceTitle => _forgeChoiceTitle;
        public string CheckpointChoiceTitle => _checkpointChoiceTitle;
        public string ExtractName => _extractName;
        // {0} gold this run.
        public string ExtractDescriptionFormat => _extractDescriptionFormat;
        public string DescendName => _descendName;
        // {0} gold this run, {1} next floor name, {2} next floor modifier description.
        public string DescendDescriptionFormat => _descendDescriptionFormat;
        public string NoFloorModifier => _noFloorModifier;
        public string ResultVictoryTitle => _resultVictoryTitle;
        public string ResultExtractedTitle => _resultExtractedTitle;
        public string ResultDefeatTitle => _resultDefeatTitle;
        // {0} hero name, {1} boss name, {2} floor name.
        public string ResultVictoryCauseFormat => _resultVictoryCauseFormat;
        // {0} hero name, {1} cleared floor name.
        public string ResultExtractedCauseFormat => _resultExtractedCauseFormat;
        // {0} hero name, {1} enemy name, {2} room name.
        public string ResultDefeatCauseFormat => _resultDefeatCauseFormat;
        // {0} floor number reached, {1} rooms cleared in total, {2} level, {3} experience.
        public string ResultProgressFormat => _resultProgressFormat;
        // {0} gold banked.
        public string ResultGoldFormat => _resultGoldFormat;
        // {0} gold banked, {1} gold lost.
        public string ResultGoldLostFormat => _resultGoldLostFormat;
        // {0} joined upgrade entries or the no-upgrades text.
        public string ResultBuildFormat => _resultBuildFormat;
        // {0} upgrade name, {1} stacks.
        public string ResultUpgradeFormat => _resultUpgradeFormat;
        public string ResultNoUpgrades => _resultNoUpgrades;
        public string RestartLabel => _restartLabel;
        // {0} relic name.
        public string RelicFormat => _relicFormat;
        // {0} relic name, {1} times it has triggered this run.
        public string RelicTriggeredFormat => _relicTriggeredFormat;
        // {0} relic name, {1} times it triggered; listed first in the result build.
        public string ResultRelicFormat => _resultRelicFormat;
        // {0} profile gold, {1} affordable relic name.
        public string ForgeHintReadyFormat => _forgeHintReadyFormat;
        // {0} profile gold, {1} cheapest unowned relic name, {2} its price.
        public string ForgeHintNextFormat => _forgeHintNextFormat;
        // {0} profile gold.
        public string ForgeHintCompleteFormat => _forgeHintCompleteFormat;
        public string ForgeButtonLabel => _forgeButtonLabel;
        public string ForgeTitle => _forgeTitle;
        // {0} profile gold, {1} deepest floor cleared.
        public string ForgeStatusFormat => _forgeStatusFormat;
        // {0} price.
        public string RelicForgeFormat => _relicForgeFormat;
        // {0} price, {1} gold still needed.
        public string RelicNeedGoldFormat => _relicNeedGoldFormat;
        public string RelicOwnedLabel => _relicOwnedLabel;
        public string RelicEquippedLabel => _relicEquippedLabel;
        public string StartRunLabel => _startRunLabel;
    }
}
