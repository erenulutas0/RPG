using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    // docs/57's slow, daze and heat on one enemy in the scene (SlowState): added the first time a skill touches it - Flame
    // Sigil's glyph, Shield Rush's strike - and ticked on combat time. EncounterController hands Current to the enemy's pack
    // before each step (PackMotion.SetSlow), the enemy's AttackController holds its swing while IsDazed, and its BurnStatus
    // multiplies a tick by CurrentHeat: the simulation reads the same state in the same three places. A body leaving the
    // floor (the horde's pool) is cleared, so its next life walks unslowed.
    public sealed class SlowStatus : MonoBehaviour
    {
        // Every status in the scene: while there is none - every run without docs/57's skills - nothing asks for one.
        private static readonly List<SlowStatus> Live = new List<SlowStatus>();

        public static bool AnyLive => Live.Count > 0;

        public SlowState State { get; } = new SlowState();
        public float Current => State.Current;
        public bool IsDazed => State.IsDazed;
        public float CurrentHeat => State.CurrentHeat;

        // The enemy's status, added on its first touch; null for a missing, fallen or shelved enemy.
        public static SlowStatus On(Health enemy)
        {
            if (enemy == null || !enemy.IsAlive || !enemy.isActiveAndEnabled)
                return null;
            if (enemy.TryGetComponent(out SlowStatus status))
                return status;
            status = enemy.gameObject.AddComponent<SlowStatus>();
            Live.Add(status);
            if (enemy.TryGetComponent(out AttackController attack))
                attack.Slow = status;
            return status;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            if (deltaTime > 0f && !State.IsQuiet)
                State.Tick(deltaTime);
        }

        private void OnDisable() => State.Clear();

        private void OnDestroy() => Live.Remove(this);
    }
}
