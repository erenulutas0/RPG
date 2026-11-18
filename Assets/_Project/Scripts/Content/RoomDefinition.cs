using System;
using Cryptforge.Combat;
using Cryptforge.Localisation;
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

        // A room has no id (it is authored inline in its floor): its localisation key is made from its English name,
        // "room.captains_post.name" (ContentKeys.Room). The field is the English.
        public string DisplayName => Loc.Text(_nameKey ??= ContentKeys.Room(_displayName), _displayName);
        [NonSerialized] private string _nameKey;
        public RoomKind Kind => _kind;
        public int WaveCount => _waves?.Length ?? 0;
        public int ForgeOptionCount => _forgeOptions?.Length ?? 0;

        public WaveDefinition WaveAt(int index) => _waves[index];

        public ForgeOptionDefinition ForgeOptionAt(int index) => _forgeOptions[index];
    }
}
