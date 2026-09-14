using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Fires the hero's ability on request: every living enemy within the ability's radius of the hero on the floor takes
    // its damage at once. The cooldown runs on scaled time, so a pause or an open choice holds it.
    public sealed class AbilityController : MonoBehaviour
    {
        [SerializeField] private Health _owner;
        [SerializeField] private Targeting _targeting;
        private readonly List<IDamageable> _targets = new List<IDamageable>();

        // Null until Initialize.
        public AbilityRuntime Ability { get; private set; }
        public int UseCount => Ability?.UsesMade ?? 0;
        public event Action Used;

        public void Initialize(AbilityRuntime ability)
        {
            if (_owner == null || _targeting == null)
                throw new InvalidOperationException("AbilityController needs owner and targeting references.");
            if (Ability != null)
                throw new InvalidOperationException("AbilityController has already been initialized.");

            Ability = ability ?? throw new ArgumentNullException(nameof(ability));
        }

        // False while cooling down, before Initialize, or once the hero has fallen.
        public bool TryUse()
        {
            if (Ability == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || !Ability.IsReady)
                return false;

            _targeting.CollectNear(_owner, Ability.Radius, _targets);
            bool used = Ability.TryUse(_owner, _targets);
            _targets.Clear();
            if (used)
                Used?.Invoke();
            return used;
        }

        private void Update()
        {
            if (Ability == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || Time.deltaTime <= 0f)
                return;
            Ability.Tick(Time.deltaTime);
        }
    }
}
