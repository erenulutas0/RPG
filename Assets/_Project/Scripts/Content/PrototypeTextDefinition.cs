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
    }
}
