using Cryptforge.Progression;
using UnityEngine;

namespace Cryptforge.Content
{
    // Localization keys: <id>.name and <id>.description. The description format receives the
    // modifier amount, with Percent amounts shown as whole percentages.
    [CreateAssetMenu(menuName = "Cryptforge/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _descriptionFormat;
        [SerializeField] private WeaponStat _stat;
        [SerializeField] private ModifierOperation _operation;
        [SerializeField] private float _amount;
        [SerializeField, Min(1)] private int _maxStacks = 1;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string DescriptionFormat => _descriptionFormat;
        public WeaponStat Stat => _stat;
        public ModifierOperation Operation => _operation;
        public float Amount => _amount;
        public int MaxStacks => _maxStacks;

        public UpgradeOption CreateOption() =>
            new UpgradeOption(_id, _displayName, _descriptionFormat, _stat, new StatModifier(_operation, _amount), _maxStacks);
    }
}
