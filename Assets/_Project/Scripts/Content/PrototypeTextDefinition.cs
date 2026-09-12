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
        [SerializeField] private string _attackCountFormat;
        [SerializeField] private string _experienceFormat;
        [SerializeField] private string _upgradeChoiceTitle;
        [SerializeField] private string _forgeChoiceTitle;
        [SerializeField] private string _resultVictoryTitle;
        [SerializeField] private string _resultDefeatTitle;
        [SerializeField] private string _resultVictoryCauseFormat;
        [SerializeField] private string _resultDefeatCauseFormat;
        [SerializeField] private string _resultProgressFormat;
        [SerializeField] private string _resultBuildFormat;
        [SerializeField] private string _resultUpgradeFormat;
        [SerializeField] private string _resultNoUpgrades;
        [SerializeField] private string _restartLabel;

        public string Title => _title;
        // {0} floor name, {1} room number, {2} room count, {3} room name.
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
        public string AttackCountFormat => _attackCountFormat;
        public string ExperienceFormat => _experienceFormat;
        public string UpgradeChoiceTitle => _upgradeChoiceTitle;
        public string ForgeChoiceTitle => _forgeChoiceTitle;
        public string ResultVictoryTitle => _resultVictoryTitle;
        public string ResultDefeatTitle => _resultDefeatTitle;
        // {0} hero name, {1} boss name, {2} floor name.
        public string ResultVictoryCauseFormat => _resultVictoryCauseFormat;
        // {0} hero name, {1} enemy name, {2} room name.
        public string ResultDefeatCauseFormat => _resultDefeatCauseFormat;
        // {0} rooms cleared, {1} room count, {2} level, {3} experience.
        public string ResultProgressFormat => _resultProgressFormat;
        // {0} joined upgrade entries or the no-upgrades text.
        public string ResultBuildFormat => _resultBuildFormat;
        // {0} upgrade name, {1} stacks.
        public string ResultUpgradeFormat => _resultUpgradeFormat;
        public string ResultNoUpgrades => _resultNoUpgrades;
        public string RestartLabel => _restartLabel;
    }
}
