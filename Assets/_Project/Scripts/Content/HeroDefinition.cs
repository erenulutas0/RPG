using UnityEngine;

namespace Cryptforge.Content
{
    [CreateAssetMenu(menuName = "Cryptforge/Hero Definition")]
    public sealed class HeroDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Min(1f)] private float _maximumHealth;
        [SerializeField] private WeaponDefinition _startingWeapon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float MaximumHealth => _maximumHealth;
        public WeaponDefinition StartingWeapon => _startingWeapon;
    }
}
