using UnityEngine;

namespace Cryptforge.Content
{
    // One Descent floor: rooms in order ending at the floor boss, a scaling tier, an optional modifier, and the floor
    // reached by descending from its checkpoint (none on the final floor).
    [CreateAssetMenu(menuName = "Cryptforge/Floor Definition")]
    public sealed class FloorDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private RoomDefinition[] _rooms;
        [SerializeField, Min(0.1f)] private float _enemyHealthMultiplier = 1f;
        [SerializeField, Min(0.1f)] private float _enemyDamageMultiplier = 1f;
        // A floor of the climb takes its tier from its height (ClimbScaling) instead of the two authored multipliers.
        [SerializeField] private bool _climbScaled;
        // On the foundry, how many surge waves each combat room sends after its authored ones (HallSurgeRule). The hall
        // arena and the Descent simulation play the authored waves only.
        [SerializeField, Min(0)] private int _foundryHallSurges;
        [SerializeField] private FloorModifierDefinition _modifier;
        [SerializeField] private FloorDefinition _nextFloor;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int RoomCount => _rooms?.Length ?? 0;
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;
        public float EnemyDamageMultiplier => _enemyDamageMultiplier;
        public bool ClimbScaled => _climbScaled;
        public int FoundryHallSurges => _foundryHallSurges;
        public FloorModifierDefinition Modifier => _modifier;
        public FloorDefinition NextFloor => _nextFloor;

        public RoomDefinition RoomAt(int index) => _rooms[index];
    }
}
