using UnityEngine;

namespace Cryptforge.Content
{
    [CreateAssetMenu(menuName = "Cryptforge/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int _experiencePerKill;
        [SerializeField, Min(1)] private int _experiencePerLevel = 10;
        [SerializeField, Min(1)] private int _upgradeChoiceCount = 2;

        public int ExperiencePerKill => _experiencePerKill;
        public int ExperiencePerLevel => _experiencePerLevel;
        public int UpgradeChoiceCount => _upgradeChoiceCount;
    }
}
