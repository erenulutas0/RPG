using Cryptforge.Combat;
using Cryptforge.Progression;
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
        // DirectHit leaves both splash values at zero; Cleave and Area need a radius and a fraction of damage in (0, 1].
        [SerializeField] private WeaponBehavior _behavior;
        [SerializeField, Min(0f)] private float _splashRadius;
        [SerializeField, Range(0f, 1f)] private float _splashFraction;
        // Every Nth attack crits for the multiplier (above 1); leave both at 0 for no crits.
        [SerializeField, Min(0)] private int _critEvery;
        [SerializeField, Min(0f)] private float _critMultiplier;
        // Only hero weapons listed in CombatSetup are sold in the Relic Forge. The description format receives damage,
        // interval, splash fraction and crit multiplier as whole percentages, and the crit rhythm.
        [SerializeField] private string _forgeDescriptionFormat;
        [SerializeField, Min(0)] private int _forgePrice;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Interval => _interval;
        public float Range => _range;
        public float InitialDelay => _initialDelay;
        public WeaponBehavior Behavior => _behavior;
        public float SplashRadius => _splashRadius;
        public float SplashFraction => _splashFraction;
        public int CritEvery => _critEvery;
        public float CritMultiplier => _critMultiplier;
        public int ForgePrice => _forgePrice;

        public WeaponRuntime CreateRuntime() =>
            new WeaponRuntime(_damage, _interval, _range, _initialDelay,
                new AttackPattern(_behavior, _splashRadius, _splashFraction, _critEvery, _critMultiplier));

        public WeaponOption CreateForgeOption() =>
            new WeaponOption(_id, _displayName,
                string.Format(_forgeDescriptionFormat ?? string.Empty, _damage, _interval, _splashFraction * 100f,
                    _critMultiplier * 100f, _critEvery),
                _forgePrice);
    }
}
