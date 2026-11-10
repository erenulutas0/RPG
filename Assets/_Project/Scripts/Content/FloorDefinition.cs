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
        // On the foundry, the floor clock (HordeClock) the gate waits for: the room before the gate boss holds once
        // cleared until the clock reads this many seconds, the horde pressing on meanwhile (V0.1 decision, the owner
        // 2026-11-02: a 4-6 minute floor). Zero, the gate opens as the room clears.
        [SerializeField, Min(0f)] private float _foundryGateNotBeforeSeconds;
        // On the foundry, how many more elite rooms - copies of the floor's first - stand before its gate boss (V0.1, the
        // owner 2026-11-02). The hall arena and the Descent play the authored rooms only.
        [SerializeField, Min(0)] private int _foundryExtraElites;
        // On the foundry, the gate boss's health against its authored on this floor (WardenRule.GateHealthShareOn): zero
        // takes WardenRule.GateHealthShare. Floor 2 sets 8.5 / 1.4, its climb's x1.4 cancelled on its Warden, so the fight
        // lands in docs/41 step 4's 45-65 s (docs/41, status 2026-11-10 later). The hall arena never reads it.
        [SerializeField, Min(0f)] private float _foundryGateHealthShare;
        // On the foundry, the roles of the floor's continuous horde (HordeDirector), lightest first: the director weights
        // the early roles in the floor's opening and the later ones as it presses. None, no horde.
        [SerializeField] private EnemyDefinition[] _foundryHorde;
        [SerializeField] private FloorModifierDefinition _modifier;
        [SerializeField] private FloorDefinition _nextFloor;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int RoomCount => _rooms?.Length ?? 0;
        public float EnemyHealthMultiplier => _enemyHealthMultiplier;
        public float EnemyDamageMultiplier => _enemyDamageMultiplier;
        public bool ClimbScaled => _climbScaled;
        public int FoundryHallSurges => _foundryHallSurges;
        public float FoundryGateNotBeforeSeconds => _foundryGateNotBeforeSeconds;
        public int FoundryExtraElites => _foundryExtraElites;
        public float FoundryGateHealthShare => _foundryGateHealthShare;
        public int HordeRoleCount => _foundryHorde?.Length ?? 0;
        public EnemyDefinition HordeRoleAt(int index) => _foundryHorde[index];
        public FloorModifierDefinition Modifier => _modifier;
        public FloorDefinition NextFloor => _nextFloor;

        public RoomDefinition RoomAt(int index) => _rooms[index];
    }
}
