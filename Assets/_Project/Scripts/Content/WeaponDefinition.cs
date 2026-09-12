using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Content
{
    [CreateAssetMenu(menuName = "Cryptforge/Weapon Definition")]
    public sealed class WeaponDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField, Min(0.01f)] private float _damage;
        [SerializeField, Min(0.01f)] private float _interval;
        [SerializeField, Min(0.01f)] private float _range;
        // Windup before the first attack of each fight; 0 attacks immediately.
        [SerializeField, Min(0f)] private float _initialDelay;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Interval => _interval;
        public float Range => _range;
        public float InitialDelay => _initialDelay;

        public WeaponRuntime CreateRuntime() => new WeaponRuntime(_damage, _interval, _range, _initialDelay);
    }
}
