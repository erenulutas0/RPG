using UnityEngine;

namespace Cryptforge.Content
{
    // A per-floor rule shown at the checkpoint before descending. Percents are fractions (0.2 = +20%) and apply
    // after the floor's scaling tier. Localization keys: <id>.name and <id>.description.
    [CreateAssetMenu(menuName = "Cryptforge/Floor Modifier Definition")]
    public sealed class FloorModifierDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private string _description;
        [SerializeField, Min(-0.9f)] private float _enemyHealthPercent;
        [SerializeField, Min(-0.9f)] private float _enemyDamagePercent;
        [SerializeField, Min(-0.9f)] private float _goldPercent;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public float EnemyHealthPercent => _enemyHealthPercent;
        public float EnemyDamagePercent => _enemyDamagePercent;
        public float GoldPercent => _goldPercent;
    }
}
