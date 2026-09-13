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

        public int ExperiencePerLevel => _experiencePerLevel;
        public int ExperienceGrowth => _experienceGrowth;
        public int UpgradeChoiceCount => _upgradeChoiceCount;
        public float AtRiskGoldLoss => _atRiskGoldLoss;
    }
}
