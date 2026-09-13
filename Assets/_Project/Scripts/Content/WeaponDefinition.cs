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
        // DirectHit leaves both splash values at zero; Cleave needs a radius and a fraction of damage in (0, 1].
        [SerializeField] private WeaponBehavior _behavior;
        [SerializeField, Min(0f)] private float _splashRadius;
        [SerializeField, Range(0f, 1f)] private float _splashFraction;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Interval => _interval;
        public float Range => _range;
        public float InitialDelay => _initialDelay;
        public WeaponBehavior Behavior => _behavior;
        public float SplashRadius => _splashRadius;
        public float SplashFraction => _splashFraction;

        public WeaponRuntime CreateRuntime() =>
            new WeaponRuntime(_damage, _interval, _range, _initialDelay, _behavior, _splashRadius, _splashFraction);
    }
}
