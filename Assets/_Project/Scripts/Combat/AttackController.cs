using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    public sealed class AttackController : MonoBehaviour, INeighbourhood
    {
        [SerializeField] private Health _owner;
        [SerializeField] private Targeting _targeting;
        private readonly List<IDamageable> _nearby = new List<IDamageable>();
        // Enemies within the weapon's PulseRadius of this attacker, gathered only when it actually pulses. A different
        // circle from _nearby's and drawn round a different centre: the attacker, not what it hit.
        private readonly List<IDamageable> _inPulseRadius = new List<IDamageable>();
        // Enemies within the weapon's CritSplashRadius of the target, for the splinter a critical strike throws. Gathered
        // before the strike, because whether the strike crits is the weapon's business and not this controller's.
        private readonly List<IDamageable> _inCritSplashRadius = new List<IDamageable>();
        // Enemies within the weapon's KillBurstRadius of an enemy its strike has just felled, for that enemy's burst.
        // Gathered when the runtime asks (INeighbourhood), once per kill, because where a burst is centred is only known
        // once the strike has landed - and only asked for by a weapon that carries a burst, so an enemy's attack never
        // gathers it.
        private readonly List<IDamageable> _inKillBurstRadius = new List<IDamageable>();
        private WeaponRuntime _weapon;
        // The weapons after the first, in slot order; null on every attacker that carries one weapon, which is every
        // enemy, so a horde of forty pays one null check a frame for the hero's loadout - no list, no loop.
        private List<WeaponRuntime> _moreWeapons;

        // Strikes of the first weapon. The body swings with it alone (docs/42, risks): a hero with three weapons must
        // not play three swings, so Attacked, Struck and this count stay the first weapon's, as they were.
        public int AttackCount { get; private set; }
        // Null until Initialize; behaviours such as enrage modify this runtime, never the definition. Always slot 0.
        public WeaponRuntime Weapon => _weapon;
        // How many weapons this attacker fires, 1 once initialised.
        public int WeaponCount => _weapon == null ? 0 : 1 + (_moreWeapons?.Count ?? 0);
        public event Action Attacked;
        // The target of each successful attack, right after Attacked, so views can place a strike effect at it.
        public event Action<Health> Struck;
        // Every weapon's successful attack, the first's included and right after its Struck, with the weapon that made
        // it, for a view that draws each weapon's own strike.
        public event Action<Health, WeaponRuntime> StruckWith;

        public void Initialize(WeaponRuntime weapon)
        {
            if (_owner == null || _targeting == null)
                throw new InvalidOperationException("AttackController needs owner and targeting references.");
            if (_weapon != null)
                throw new InvalidOperationException("AttackController has already been initialized.");

            _weapon = weapon ?? throw new ArgumentNullException(nameof(weapon));
        }

        // Puts a further weapon in the next slot, fired after every weapon before it. The loadout decides what the hero
        // holds (WeaponLoadout); this only fires it, in the order the loadout added it.
        public void AddWeapon(WeaponRuntime weapon)
        {
            if (weapon == null)
                throw new ArgumentNullException(nameof(weapon));
            if (_weapon == null)
                throw new InvalidOperationException("AttackController needs Initialize before a second weapon.");
            if (WeaponCount == WeaponLoadout.MaxSlots)
                throw new InvalidOperationException($"An attacker fires at most {WeaponLoadout.MaxSlots} weapons.");
            if (ReferenceEquals(weapon, _weapon) || (_moreWeapons != null && _moreWeapons.Contains(weapon)))
                throw new InvalidOperationException("That weapon is already fired by this attacker.");

            if (_moreWeapons == null)
                _moreWeapons = new List<WeaponRuntime>(WeaponLoadout.MaxSlots - 1);
            _moreWeapons.Add(weapon);
        }

        // One Update for every weapon, in slot order: Unity does not order Update across components, and the simulation
        // fires the same slots in the same order (DescentSimulation.StrikeInSlotOrder). Each weapon acquires its own
        // target with its own reach after the slots before it have struck, so a later slot never strikes what an earlier
        // one felled this frame - Targeting skips the fallen.
        private void Update()
        {
            if (_weapon == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || Time.deltaTime <= 0f)
                return;

            float deltaTime = Time.deltaTime;
            Fire(_weapon, deltaTime, true);
            if (_moreWeapons == null)
                return;
            for (int i = 0; i < _moreWeapons.Count; i++)
                Fire(_moreWeapons[i], deltaTime, false);
        }

        private void Fire(WeaponRuntime weapon, float deltaTime, bool first)
        {
            weapon.Tick(deltaTime);
            if (!weapon.IsReady)
                return;

            Health target = _targeting.Acquire(weapon.Range);
            if (target == null)
                return;

            // Splash candidates are gathered only for a ready attack, into a reused list. The lists are shared by every
            // slot because each slot fills and spends them before the next one starts.
            _nearby.Clear();
            float splashRadius = weapon.SplashRadius;
            if (splashRadius > 0f)
                _targeting.CollectNear(target, splashRadius, _nearby);
            // The same, for the pulse, round this attacker. Nothing pulses until a card grants one, so this costs a
            // float comparison on every attack any enemy ever makes.
            _inPulseRadius.Clear();
            if (weapon.PulseDamage > 0f)
                _targeting.CollectNear(_owner, weapon.PulseRadius, _inPulseRadius);
            _inCritSplashRadius.Clear();
            if (weapon.CritSplash > 0f)
                _targeting.CollectNear(target, weapon.CritSplashRadius, _inCritSplashRadius);
            if (!weapon.TryAttack(target, _owner, _nearby, _inPulseRadius, _inCritSplashRadius,
                    weapon.KillBurst > 0f ? this : null))
                return;

            if (first)
            {
                AttackCount++;
                Attacked?.Invoke();
                Struck?.Invoke(target);
            }
            StruckWith?.Invoke(target, weapon);
        }

        // The fallen enemy is still where it fell: a body is hidden at once but retired only later, so its position is
        // the one the strike reached it at. Explicit, so the controller's own surface stays what it was.
        IReadOnlyList<IDamageable> INeighbourhood.Near(IDamageable centre, float radius)
        {
            _targeting.CollectNear(centre as Health, radius, _inKillBurstRadius);
            return _inKillBurstRadius;
        }
    }
}
