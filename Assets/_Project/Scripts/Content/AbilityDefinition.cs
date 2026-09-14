using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Content
{
    // The hero's active ability: a burst around the hero on a cooldown. Localization keys: <id>.name and
    // <id>.description; the description format receives the damage, the radius and the cooldown.
    [CreateAssetMenu(menuName = "Cryptforge/Ability Definition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _descriptionFormat;
        [SerializeField, Min(0.1f)] private float _damage = 20f;
        // Floor units from the hero.
        [SerializeField, Min(0.1f)] private float _radius = 2.5f;
        [SerializeField, Min(0.1f)] private float _cooldown = 8f;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Radius => _radius;
        public float Cooldown => _cooldown;
        public string Description => string.Format(_descriptionFormat ?? string.Empty, _damage, _radius, _cooldown);

        // A fresh runtime per run; the definition stays untouched by upgrades.
        public AbilityRuntime CreateRuntime() => new AbilityRuntime(_damage, _radius, _cooldown);
    }
}
