using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.Content
{
    // Ordered enemies for consecutive encounters, repeating after the last entry. A stand-in for the floor and
    // room definitions of the Descent structure; there is no randomness or scaling yet.
    [CreateAssetMenu(menuName = "Cryptforge/Encounter Sequence")]
    public sealed class EncounterSequenceDefinition : ScriptableObject
    {
        [SerializeField] private EnemyDefinition[] _enemies;

        public int Count => _enemies?.Length ?? 0;

        public EnemyDefinition EnemyAt(int index) => _enemies[index];

        public EnemyDefinition EnemyFor(int encounterNumber) => _enemies[EncounterSchedule.IndexFor(encounterNumber, Count)];
    }
}
