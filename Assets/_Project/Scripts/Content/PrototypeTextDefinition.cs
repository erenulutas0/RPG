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
        [SerializeField] private string _fighting;
        [SerializeField] private string _victory;
        [SerializeField] private string _healthFormat;
        [SerializeField] private string _weaponFormat;
        [SerializeField] private string _attackCountFormat;

        public string Title => _title;
        public string Subtitle => _subtitle;
        public string Fighting => _fighting;
        public string Victory => _victory;
        public string HealthFormat => _healthFormat;
        public string WeaponFormat => _weaponFormat;
        public string AttackCountFormat => _attackCountFormat;
    }
}
