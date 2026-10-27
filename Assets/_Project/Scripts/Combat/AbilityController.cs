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
        private readonly List<IDamageable> _splash = new List<IDamageable>();
        private readonly List<Health> _vented = new List<Health>();
        private readonly List<int> _ventedStacks = new List<int>();

        // A Vent's eruptions: the burning enemy, and the stacks it spent. For the effects and the tests.
        public event Action<Health, int> Vented;

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
            bool used = Ability.Kind == AbilityKind.Vent ? Vent() : Ability.TryUse(_owner, _targets);
            _targets.Clear();
            if (used)
                Used?.Invoke();
            return used;
        }

        // Crucible Vent (docs/45): every burning enemy in reach spends its stacks at once; each spent stack deals the
        // ability's damage to it and to every living enemy within BurnRule.VentSplashRadius of it. The stacks are all
        // spent before any damage lands, so an eruption that fells a neighbour never takes that neighbour's own stacks
        // with it unspent. A Vent with nothing burning in reach is still spent, as the player chose the moment.
        private bool Vent()
        {
            if (!Ability.TrySpend())
                return false;
            _vented.Clear();
            _ventedStacks.Clear();
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!(_targets[i] is Health enemy) || !enemy.IsAlive)
                    continue;
                BurnStatus burn = enemy.GetComponent<BurnStatus>();
                if (burn == null || !burn.IsBurning)
                    continue;
                _vented.Add(enemy);
                _ventedStacks.Add(burn.Consume());
            }
            for (int i = 0; i < _vented.Count; i++)
            {
                Health centre = _vented[i];
                float damage = BurnRule.VentDamage(_ventedStacks[i], Ability.Damage);
                _targeting.CollectNear(centre, BurnRule.VentSplashRadius, _splash);
                if (centre.IsAlive)
                    centre.ApplyDamage(new DamageContext(damage, _owner));
                for (int j = 0; j < _splash.Count; j++)
                {
                    IDamageable near = _splash[j];
                    if (near != null && near.IsAlive)
                        near.ApplyDamage(new DamageContext(damage, _owner));
                }
                _splash.Clear();
                Vented?.Invoke(centre, _ventedStacks[i]);
            }
            return true;
        }

        private void Update()
        {
            if (Ability == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || Time.deltaTime <= 0f)
                return;
            Ability.Tick(Time.deltaTime);
        }
    }
}
