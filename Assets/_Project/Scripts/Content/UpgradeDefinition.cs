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
        // The magnitude at each tier: green (_amount), orange (_rareAmount), red (_epicAmount) and gold (_goldAmount),
        // authored as numbers rather than derived so one card can differ from the rule; the shipped cards are written
        // from RarityTable.Multiplier, 1 / 1.25 / 1.5 / 2 times green (docs/40 section 7.1). A card authored before a
        // tier existed carries nothing for it and falls back to the tier below, so it behaves exactly as it did.
        [SerializeField] private float _amount;
        [SerializeField] private float _rareAmount;
        [SerializeField] private float _epicAmount;
        [SerializeField] private float _goldAmount;
        [SerializeField, Min(1)] private int _maxStacks = 1;
        // A card that must hold a stack before this one can be offered; none for most cards.
        [SerializeField] private UpgradeDefinition _requires;
        // A card that must be full before this one can be offered: this card is that card's evolution. None for most.
        [SerializeField] private UpgradeDefinition _evolvesFrom;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string DescriptionFormat => _descriptionFormat;
        public UpgradeStat Stat => _stat;
        public ModifierOperation Operation => _operation;
        public float Amount => _amount;
        public int MaxStacks => _maxStacks;

        public UpgradeDefinition Requires => _requires;
        public UpgradeDefinition EvolvesFrom => _evolvesFrom;

        public float RareAmount => _rareAmount != 0f ? _rareAmount : _amount;
        public float EpicAmount => _epicAmount != 0f ? _epicAmount : RareAmount;
        public float GoldAmount => _goldAmount != 0f ? _goldAmount : EpicAmount;

        public UpgradeOption CreateOption() =>
            new UpgradeOption(_id, _displayName, _descriptionFormat, _stat,
                new StatModifier(_operation, _amount),
                new StatModifier(_operation, RareAmount),
                new StatModifier(_operation, EpicAmount),
                new StatModifier(_operation, GoldAmount),
                _maxStacks, _requires != null ? _requires.Id : null,
                _evolvesFrom != null ? _evolvesFrom.Id : null);
    }
}
