using System.Globalization;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Content
{
    // A hero's active ability: a burst around the hero on a cooldown, or one of docs/57's skills 2 and 3 (AbilityKind).
    // Localization keys: <id>.name and <id>.description; the description format receives the damage, the radius, the
    // cooldown, the duration and the amount.
    [CreateAssetMenu(menuName = "Cryptforge/Ability Definition")]
    public sealed class AbilityDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _descriptionFormat;
        // A Burst's damage to each enemy in reach, a Vent's per Burn stack, a Rush's to each enemy on its path (scaled by
        // the hero's first weapon, SkillRule.Scaled); 0 for the skills that strike nothing (Guard, Sigil, Ward).
        [SerializeField, Min(0f)] private float _damage = 20f;
        // Floor units: a Burst's and a Vent's reach, a Rush's path either side, a glyph's radius, the crowd a guard's or a
        // ward's autopilot reads.
        [SerializeField, Min(0.1f)] private float _radius = 2.5f;
        [SerializeField, Min(0.1f)] private float _cooldown = 8f;
        // Burst: damage to everything in reach. Vent: damage per Burn stack each burning enemy in reach spends. Guard, Rush,
        // Sigil, Ward: docs/57's skills 2 and 3 (AbilityRuntime).
        [SerializeField] private AbilityKind _kind = AbilityKind.Burst;
        // docs/57's skills: how long the effect holds (the guard's armour, the Rush's travel, the glyph, the ward), and its
        // size (armour, floor units, the share of the walk slowed, the share of the hero's own maximum health warded). 0 for
        // the Burst and the Vent, which act at once.
        [SerializeField, Min(0f)] private float _duration;
        [SerializeField, Min(0f)] private float _amount;
        // The ability button's art: the Forge Burst anvil, the Crucible Vent crucible. Empty keeps the scene's icon.
        [SerializeField] private Sprite _icon;

        public string Id => _id;
        public string DisplayName => _displayName;
        public float Damage => _damage;
        public float Radius => _radius;
        public float Cooldown => _cooldown;
        public AbilityKind Kind => _kind;
        public float Duration => _duration;
        public float Amount => _amount;
        public Sprite Icon => _icon;
        // The description's English with its placeholders, for AbilityText to write in the language the game speaks.
        public string DescriptionFormat => _descriptionFormat ?? string.Empty;
        public string Description => string.Format(CultureInfo.InvariantCulture, _descriptionFormat ?? string.Empty, _damage, _radius,
            _cooldown, _duration, _amount);

        // A fresh runtime per run; the definition stays untouched by upgrades.
        public AbilityRuntime CreateRuntime() => _kind == AbilityKind.Burst || _kind == AbilityKind.Vent
            ? new AbilityRuntime(_damage, _radius, _cooldown, _kind)
            : AbilityRuntime.Skill(_kind, _damage, _radius, _cooldown, _duration, _amount);
    }
}
