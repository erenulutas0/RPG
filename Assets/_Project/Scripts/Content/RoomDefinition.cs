using System;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Content
{
    // Authored inline inside a FloorDefinition. Combat, Elite and Boss rooms list their waves in order, each wave a
    // pack of one to three enemies; Forge rooms list the options offered on the visit.
    [Serializable]
    public sealed class RoomDefinition
    {
        [SerializeField] private string _displayName;
        [SerializeField] private RoomKind _kind;
        [SerializeField] private WaveDefinition[] _waves;
        [SerializeField] private ForgeOptionDefinition[] _forgeOptions;

        public string DisplayName => _displayName;
        public RoomKind Kind => _kind;
        public int WaveCount => _waves?.Length ?? 0;
        public int ForgeOptionCount => _forgeOptions?.Length ?? 0;

        public WaveDefinition WaveAt(int index) => _waves[index];

        public ForgeOptionDefinition ForgeOptionAt(int index) => _forgeOptions[index];
    }
}
