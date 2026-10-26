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
        // The weapon's own table (docs/42, PR 3): what a later pick of it can give, one line drawn per offer. Each line is
        // an ordinary upgrade card, so its tiers and text come with it. A line on Damage is authored Flat, never Percent:
        // per-weapon damage then adds to the base before every tome's Force multiplies it, the report's per-weapon-first,
        // global-bucket-second order (40 section 5.3), and a content test holds every asset to it.
        [SerializeField] private UpgradeDefinition[] _levelUps;
        // What the weapon evolves into, and the id of the tome it evolves with; none for a weapon with no evolution yet.
        // The evolution's own _requires and _evolvesFrom stay empty: its gate is this weapon's level and the tome's
        // (EvolutionRule.WeaponUnlocked), and WeaponCard makes it evolve from this weapon.
        [SerializeField] private UpgradeDefinition _evolution;
        [SerializeField] private string _evolvesWithTome;

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
        public int LevelUpCount => _levelUps != null ? _levelUps.Length : 0;
        public UpgradeDefinition LevelUpAt(int index) => _levelUps[index];
        public UpgradeDefinition Evolution => _evolution;
        // Null when the weapon evolves with no tome.
        public string EvolvesWithTome => string.IsNullOrEmpty(_evolvesWithTome) ? null : _evolvesWithTome;

        public WeaponRuntime CreateRuntime() =>
            new WeaponRuntime(_damage, _interval, _range, _initialDelay,
                new AttackPattern(_behavior, _splashRadius, _splashFraction, _critEvery, _critMultiplier));

        public WeaponOption CreateForgeOption() =>
            new WeaponOption(_id, _displayName,
                string.Format(_forgeDescriptionFormat ?? string.Empty, _damage, _interval, _splashFraction * 100f,
                    _critMultiplier * 100f, _critEvery),
                _forgePrice);

        // The weapon as a run's offers see it: the pure card UpgradeService draws from, so the service never holds this
        // asset. Its text is the forge's description of the weapon.
        public WeaponCard CreateCard()
        {
            var lines = new UpgradeOption[LevelUpCount];
            for (int i = 0; i < lines.Length; i++)
            {
                if (_levelUps[i] == null)
                    throw new System.InvalidOperationException($"Weapon {_id} has an empty level-up line at {i}.");
                lines[i] = _levelUps[i].CreateOption();
            }
            return new WeaponCard(_id, _displayName, CreateForgeOption().Description, CreateRuntime, lines,
                _evolution != null ? _evolution.CreateOption() : null, EvolvesWithTome);
        }
    }
}
