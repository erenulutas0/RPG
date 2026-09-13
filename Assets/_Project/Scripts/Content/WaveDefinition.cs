using System;
using UnityEngine;

namespace Cryptforge.Content
{
    // One wave inside a room: the enemies that spawn together, in slot order (the first takes the centre).
    [Serializable]
    public sealed class WaveDefinition
    {
        [SerializeField] private EnemyDefinition[] _enemies;

        public int EnemyCount => _enemies?.Length ?? 0;

        public EnemyDefinition EnemyAt(int index) => _enemies[index];
    }
}
