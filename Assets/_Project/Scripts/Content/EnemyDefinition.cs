using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Content
{
    [CreateAssetMenu(menuName = "Cryptforge/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Min(1f)] private float _maximumHealth;
        // Enemies attack through the same WeaponDefinition → WeaponRuntime path as the hero.
        [SerializeField] private WeaponDefinition _weapon;
        // Presentation and components; the prefab must carry Health, Targeting and AttackController.
        [SerializeField] private Health _prefab;
        // Base gold before the floor modifier.
        [SerializeField, Min(0)] private int _goldReward;
        // Experience per kill; packs make small enemies cheap so level-ups keep a steady pace.
        [SerializeField, Min(0)] private int _experienceReward;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float MaximumHealth => _maximumHealth;
        public WeaponDefinition Weapon => _weapon;
        public Health Prefab => _prefab;
        public int GoldReward => _goldReward;
        public int ExperienceReward => _experienceReward;
    }
}
