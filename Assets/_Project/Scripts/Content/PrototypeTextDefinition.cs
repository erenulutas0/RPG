using UnityEngine;

namespace Cryptforge.Content
{
    // Keys are prototype.<field name>, e.g. prototype.title. Content display names use <id>.name.
    // Keep text in data; a localization service is unnecessary for this single-language slice.
    [CreateAssetMenu(menuName = "Cryptforge/Prototype Text")]
    public sealed class PrototypeTextDefinition : ScriptableObject
    {
        [SerializeField] private string _title;
        [SerializeField] private string _subtitle;
        [SerializeField] private string _encounterFormat;
        [SerializeField] private string _victoryFormat;
        [SerializeField] private string _healthFormat;
        [SerializeField] private string _weaponFormat;
        [SerializeField] private string _attackCountFormat;
        [SerializeField] private string _experienceFormat;
        [SerializeField] private string _upgradeChoiceTitle;
        [SerializeField] private string _resultTitle;
        [SerializeField] private string _resultCauseFormat;
        [SerializeField] private string _resultProgressFormat;
        [SerializeField] private string _resultBuildFormat;
        [SerializeField] private string _resultUpgradeFormat;
        [SerializeField] private string _resultNoUpgrades;
        [SerializeField] private string _restartLabel;

        public string Title => _title;
        public string Subtitle => _subtitle;
        // {0} encounter number.
        public string EncounterFormat => _encounterFormat;
        // {0} enemy name, {1} hits this fight, {2} clear time in seconds.
        public string VictoryFormat => _victoryFormat;
        public string HealthFormat => _healthFormat;
        public string WeaponFormat => _weaponFormat;
        public string AttackCountFormat => _attackCountFormat;
        public string ExperienceFormat => _experienceFormat;
        public string UpgradeChoiceTitle => _upgradeChoiceTitle;
        public string ResultTitle => _resultTitle;
        // {0} hero name, {1} enemy name, {2} encounter number.
        public string ResultCauseFormat => _resultCauseFormat;
        // {0} encounters cleared, {1} level, {2} experience.
        public string ResultProgressFormat => _resultProgressFormat;
        // {0} joined upgrade entries or the no-upgrades text.
        public string ResultBuildFormat => _resultBuildFormat;
        // {0} upgrade name, {1} stacks.
        public string ResultUpgradeFormat => _resultUpgradeFormat;
        public string ResultNoUpgrades => _resultNoUpgrades;
        public string RestartLabel => _restartLabel;
    }
}
