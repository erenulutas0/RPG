using UnityEngine;

namespace Cryptforge.Content
{
    // One Descent floor: rooms in order, ending at the floor boss. Biome, floor modifier and scaling tier are added
    // with the second floor.
    [CreateAssetMenu(menuName = "Cryptforge/Floor Definition")]
    public sealed class FloorDefinition : ScriptableObject
    {
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [SerializeField] private RoomDefinition[] _rooms;

        public string Id => _id;
        public string DisplayName => _displayName;
        public int RoomCount => _rooms?.Length ?? 0;

        public RoomDefinition RoomAt(int index) => _rooms[index];
    }
}
