using UnityEngine;

namespace Cryptforge.Content
{
    // Kill experience and gold live on each EnemyDefinition.
    [CreateAssetMenu(menuName = "Cryptforge/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int _experiencePerLevel = 10;
        // Extra experience each further level costs; zero keeps every level at ExperiencePerLevel.
        [SerializeField, Min(0)] private int _experienceGrowth;
        [SerializeField, Min(1)] private int _upgradeChoiceCount = 2;
        // Fraction of gold earned since the last checkpoint that a defeat loses (05_ECONOMY_BALANCING: start at 50%).
        [SerializeField, Range(0f, 1f)] private float _atRiskGoldLoss = 0.5f;
        // The tomes a run on the foundry draws by category, with every weapon of the forge's catalogue as a card
        // (docs/42, PR 7). Empty, and the foundry offers the legacy cards as everywhere else.
        [SerializeField] private UpgradeDefinition[] _tomes;
        // Cards an offer shows in category mode: the report's three (docs/40 9.2).
        [SerializeField, Min(1)] private int _categoryChoiceCount = 3;

        public int ExperiencePerLevel => _experiencePerLevel;
        public int ExperienceGrowth => _experienceGrowth;
        public int UpgradeChoiceCount => _upgradeChoiceCount;
        public float AtRiskGoldLoss => _atRiskGoldLoss;
        public int TomeCount => _tomes?.Length ?? 0;
        public UpgradeDefinition TomeAt(int index) => _tomes[index];
        public int CategoryChoiceCount => _categoryChoiceCount;
    }
}
