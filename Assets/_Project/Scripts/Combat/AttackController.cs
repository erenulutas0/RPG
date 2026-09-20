using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class AttackController : MonoBehaviour
    {
        [SerializeField] private Health _owner;
        [SerializeField] private Targeting _targeting;
        private readonly List<IDamageable> _nearby = new List<IDamageable>();
        // Enemies within PulseRule.Radius of this attacker, gathered only when its weapon actually pulses. A different
        // circle from _nearby's and drawn round a different centre: the attacker, not what it hit.
        private readonly List<IDamageable> _inPulseRadius = new List<IDamageable>();
        // Enemies within CritSplashRule.Radius of the target, for the splinter a critical strike throws. Gathered before
        // the strike, because whether the strike crits is the weapon's business and not this controller's.
        private readonly List<IDamageable> _inCritSplashRadius = new List<IDamageable>();
        private WeaponRuntime _weapon;

        public int AttackCount { get; private set; }
        // Null until Initialize; behaviours such as enrage modify this runtime, never the definition.
        public WeaponRuntime Weapon => _weapon;
        public event Action Attacked;
        // The target of each successful attack, right after Attacked, so views can place a strike effect at it.
        public event Action<Health> Struck;

        public void Initialize(WeaponRuntime weapon)
        {
            if (_owner == null || _targeting == null)
                throw new InvalidOperationException("AttackController needs owner and targeting references.");
            if (_weapon != null)
                throw new InvalidOperationException("AttackController has already been initialized.");

            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
        }

        private void Update()
        {
            if (_weapon == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || Time.deltaTime <= 0f)
                return;

            _weapon.Tick(Time.deltaTime);
            if (!_weapon.IsReady)
                return;

            Health target = _targeting.Acquire(_weapon.Range);
            if (target == null)
                return;

            // Splash candidates are gathered only for a ready attack, into a reused list.
            _nearby.Clear();
            float splashRadius = _weapon.Pattern.SplashRadius;
            if (splashRadius > 0f)
                _targeting.CollectNear(target, splashRadius, _nearby);
            // The same, for the pulse, round this attacker. Nothing pulses until a card grants one, so this costs a
            // float comparison on every attack any enemy ever makes.
            _inPulseRadius.Clear();
            if (_weapon.PulseDamage > 0f)
                _targeting.CollectNear(_owner, PulseRule.Radius, _inPulseRadius);
            _inCritSplashRadius.Clear();
            if (_weapon.CritSplash > 0f)
                _targeting.CollectNear(target, CritSplashRule.Radius, _inCritSplashRadius);
            if (_weapon.TryAttack(target, _owner, _nearby, _inPulseRadius, _inCritSplashRadius))
            {
                AttackCount++;
                Attacked?.Invoke();
                Struck?.Invoke(target);
            }
        }
    }
}
