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
        [SerializeField] private UpgradeStat _stat;
        [SerializeField] private ModifierOperation _operation;
        [SerializeField] private float _amount;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        // A card that must hold a stack before this one can be offered; none for most cards.
        [SerializeField] private UpgradeDefinition _requires;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string DescriptionFormat => _descriptionFormat;
        public UpgradeStat Stat => _stat;
        public ModifierOperation Operation => _operation;
        public float Amount => _amount;
        public int MaxStacks => _maxStacks;

        public UpgradeDefinition Requires => _requires;

        public UpgradeOption CreateOption() =>
            new UpgradeOption(_id, _displayName, _descriptionFormat, _stat, new StatModifier(_operation, _amount), _maxStacks,
                _requires != null ? _requires.Id : null);
    }
}
