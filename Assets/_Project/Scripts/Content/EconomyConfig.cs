using UnityEngine;

namespace Cryptforge.Content
{
    [CreateAssetMenu(menuName = "Cryptforge/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int _experiencePerKill;
        [SerializeField, Min(1)] private int _experiencePerLevel = 10;
        [SerializeField, Min(1)] private int _upgradeChoiceCount = 2;
        // Fraction of gold earned since the last checkpoint that a defeat loses (05_ECONOMY_BALANCING: start at 50%).
        [SerializeField, Range(0f, 1f)] private float _atRiskGoldLoss = 0.5f;

        public int ExperiencePerKill => _experiencePerKill;
        public int ExperiencePerLevel => _experiencePerLevel;
        public int UpgradeChoiceCount => _upgradeChoiceCount;
        public float AtRiskGoldLoss => _atRiskGoldLoss;
    }
}
