using UnityEngine;

namespace Cryptforge.Content
{
    [CreateAssetMenu(menuName = "Cryptforge/Economy Config")]
    public sealed class EconomyConfig : ScriptableObject
    {
        [SerializeField, Min(0)] private int _experiencePerKill;

        public int ExperiencePerKill => _experiencePerKill;
    }
}
