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

        public string Id => _id;
        public string DisplayName => _displayName;
        public float MaximumHealth => _maximumHealth;
        public WeaponDefinition Weapon => _weapon;
        public Health Prefab => _prefab;
    }
}
