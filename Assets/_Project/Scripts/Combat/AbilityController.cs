using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.Combat
{
    // Fires the hero's abilities on request. Slot 1 is the hero's shipped ability: every living enemy within its radius of
    // the hero on the floor takes its damage at once (a Burst), or every burning one erupts (a Vent). Slots 2 and 3 are
    // docs/57's skills (AbilityKind Guard, Rush, Sigil, Ward), empty until a run is handed them (InitializeSkills): the guard
    // lays armour on the hero's sheet for its seconds (GuardArmor), the ward a pool on its health (HealthState.RaiseWard),
    // the Rush strikes along its path and dashes the hero down it (Dash, read by HeroMovementInput), the glyph slows, heats
    // and lights what stands in it on Burn's clock (Sigil, SlowStatus). Every body these read is the foundry simulation's
    // too (SkillRule, HeroDash, SigilZone, SlowState). The cooldowns run on scaled time, so a pause or an open choice holds
    // them.
    public sealed class AbilityController : MonoBehaviour
    {
        public const int SlotCount = 3;

        [SerializeField] private Health _owner;
        [SerializeField] private Targeting _targeting;
        private readonly List<IDamageable> _targets = new List<IDamageable>();
        private readonly List<IDamageable> _splash = new List<IDamageable>();
        private readonly List<Health> _vented = new List<Health>();
        private readonly List<int> _ventedStacks = new List<int>();
        private readonly AbilityRuntime[] _slots = new AbilityRuntime[SlotCount];
        private readonly bool[] _active = new bool[SlotCount];
        // The last heading the hero walked, for a Rush pressed with no finger down and nothing in its seek.
        private Vector2 _facing = new Vector2(0f, -1f);

        // A Vent's eruptions: the burning enemy, and the stacks it spent. For the effects and the tests.
        public event Action<Health, int> Vented;

        // Slot 1's runtime; null until Initialize.
        public AbilityRuntime Ability => _slots[0];
        public int UseCount => Ability?.UsesMade ?? 0;
        // Slot 1's use (the shipped button, its telemetry and its effects).
        public event Action Used;
        // A use of slot 2 or 3: the slot and its runtime (docs/57 phase 3's effects and telemetry listen here).
        public event Action<int, AbilityRuntime> SkillUsed;
        // A hold on every slot, asked on each use, as AttackController.FightHold is on each strike: while it answers true
        // TryUse refuses. The foundry's boss reveal holds the fight while the camera is at the gate (EncounterController.
        // FightHold), and the ability is part of the fight; the composition root hands it over. Null where nothing holds it.
        public Func<bool> FightHold { get; set; }
        public bool IsHeld => FightHold != null && FightHold();

        // What docs/57's skills reach, handed over by the composition root (CombatSetup); each null where nothing plays it.
        // The guard's armour on the hero's sheet (HeroStats.SetGuardArmor): the amount at the press, 0 as it ends.
        public Action<float> GuardArmor { get; set; }
        // Burn stacks set on an enemy as the hero's own (direct): the glyph's on the unburnt, Ember Memory's neighbourhood.
        public Action<Health, int> Ignite { get; set; }
        // The hero's first weapon, which a skill's damage scales by (SkillRule.Scaled).
        public Func<WeaponRuntime> FirstWeapon { get; set; }
        // Whether an enemy is the gate boss: slowed by half, never dazed.
        public Func<Health, bool> IsGateBoss { get; set; }
        // The stick's steer in floor units (HeroMovementInput.Steer): the Rush's heading while a finger is down.
        public Func<Vector2> Heading { get; set; }
        // The Rush's dash (HeroMovementInput reads it) and the glyph; null for a hero without them.
        public HeroDash Dash { get; private set; }
        public SigilZone Sigil { get; private set; }

        public void Initialize(AbilityRuntime ability)
        {
            if (_owner == null || _targeting == null)
                throw new InvalidOperationException("AbilityController needs owner and targeting references.");
            if (Ability != null)
                throw new InvalidOperationException("AbilityController has already been initialized.");

            _slots[0] = ability ?? throw new ArgumentNullException(nameof(ability));
        }

        // docs/57's skills 2 and 3 for this run, after Initialize; either may be null for a slot the run leaves empty.
        public void InitializeSkills(AbilityRuntime second, AbilityRuntime third)
        {
            if (Ability == null)
                throw new InvalidOperationException("AbilityController needs its first ability before its skills.");
            if (_slots[1] != null || _slots[2] != null)
                throw new InvalidOperationException("AbilityController's skills are already set.");
            _slots[1] = second;
            _slots[2] = third;
            for (int i = 1; i < SlotCount; i++)
            {
                if (_slots[i] == null)
                    continue;
                if (_slots[i].Kind == AbilityKind.Rush)
                    Dash = new HeroDash(_slots[i].Amount, _slots[i].Duration);
                if (_slots[i].Kind == AbilityKind.Sigil)
                    Sigil = new SigilZone();
            }
        }

        // The runtime in slot 1, 2 or 3; null for an empty slot.
        public AbilityRuntime SkillAt(int slot) => slot >= 1 && slot <= SlotCount ? _slots[slot - 1] : null;

        public bool TryUse() => TryUse(1);

        // A press of slot along heading (floor units): the development autopilot's Rush, which picks its way as the
        // simulation's pilot does rather than reading the stick. Every other kind ignores the heading.
        public bool TryUse(int slot, Vector2 heading)
        {
            _pressHeading = heading;
            try
            {
                return TryUse(slot);
            }
            finally
            {
                _pressHeading = Vector2.zero;
            }
        }

        private Vector2 _pressHeading;

        // False while cooling down, for an empty slot, before Initialize, once the hero has fallen, or while the fight is
        // held (FightHold).
        public bool TryUse(int slot)
        {
            AbilityRuntime ability = SkillAt(slot);
            if (ability == null || !_owner.isActiveAndEnabled || !_owner.IsAlive || !ability.IsReady || IsHeld)
                return false;

            bool used;
            switch (ability.Kind)
            {
                case AbilityKind.Vent:
                    _targeting.CollectNear(_owner, ability.Radius, _targets);
                    used = Vent(ability);
                    _targets.Clear();
                    break;
                case AbilityKind.Burst:
                    _targeting.CollectNear(_owner, ability.Radius, _targets);
                    used = ability.TryUse(_owner, _targets);
                    _targets.Clear();
                    break;
                default:
                    used = ability.TrySpend();
                    if (used)
                        Begin(ability);
                    break;
            }
            if (!used)
                return false;
            if (slot == 1)
                Used?.Invoke();
            else
                SkillUsed?.Invoke(slot, ability);
            return true;
        }

        // A skill's effect at the press.
        private void Begin(AbilityRuntime skill)
        {
            switch (skill.Kind)
            {
                case AbilityKind.Guard:
                    GuardArmor?.Invoke(skill.Amount);
                    break;
                case AbilityKind.Ward:
                    _owner.State.RaiseWard(SkillRule.WardOf(_owner.Maximum, skill.Amount));
                    break;
                case AbilityKind.Sigil:
                    Sigil.Begin(transform.position.x, ArenaFloor.FloorY(transform.position.y), skill.Radius, skill.Duration,
                        skill.Amount);
                    break;
                case AbilityKind.Rush:
                    Rush(skill);
                    break;
            }
        }

        // A skill's effect as it ends.
        private void End(AbilityRuntime skill)
        {
            if (skill.Kind == AbilityKind.Guard)
                GuardArmor?.Invoke(0f);
            else if (skill.Kind == AbilityKind.Ward)
                _owner.State.ClearWard();
        }

        // Shield Rush: along the stick's heading, or with no finger down at the nearest enemy within its seek, or the way
        // the hero last walked. Every enemy within the path's radius takes the damage scaled by the first weapon and, but the
        // gate boss, a daze; the dash itself begins at the next walk step (HeroMovementInput).
        private void Rush(AbilityRuntime skill)
        {
            float x = transform.position.x, y = ArenaFloor.FloorY(transform.position.y);
            Vector2 heading = _pressHeading.sqrMagnitude >= .05f * .05f ? _pressHeading
                : Heading != null ? Heading() : Vector2.zero;
            if (heading.sqrMagnitude < .05f * .05f)
            {
                Health nearest = _targeting.Acquire(SkillRule.RushSeek);
                heading = nearest != null
                    ? new Vector2(nearest.transform.position.x - x, ArenaFloor.FloorY(nearest.transform.position.y) - y)
                    : _facing;
            }
            Dash.Begin(heading.x, heading.y);
            float toX = x + Dash.HeadingX * skill.Amount, toY = y + Dash.HeadingY * skill.Amount;
            WeaponRuntime first = FirstWeapon?.Invoke();
            float damage = first != null ? SkillRule.Scaled(skill.Damage, first.Damage, first.BaseDamage) : skill.Damage;
            _targeting.CollectWithin((x + toX) * .5f, (y + toY) * .5f, skill.Amount * .5f + skill.Radius, _targets);
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!(_targets[i] is Health enemy) || !enemy.IsAlive)
                    continue;
                Vector3 at = enemy.transform.position;
                if (!SkillRule.OnPath(at.x, ArenaFloor.FloorY(at.y), x, y, toX, toY, skill.Radius))
                    continue;
                enemy.ApplyDamage(new DamageContext(damage, _owner));
                if (enemy.IsAlive && (IsGateBoss == null || !IsGateBoss(enemy)))
                    SlowStatus.On(enemy)?.State.Daze(SkillRule.DazeSeconds);
            }
            _targets.Clear();
        }

        // One tick of the glyph: every enemy inside slowed (half on the gate boss) and heated until the next, an unburnt one
        // lit with a direct stack.
        private void SigilTick()
        {
            _targeting.CollectWithin(Sigil.X, Sigil.Y, Sigil.Radius + .01f, _targets);
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!(_targets[i] is Health enemy) || !enemy.IsAlive)
                    continue;
                Vector3 at = enemy.transform.position;
                if (!Sigil.Contains(at.x, ArenaFloor.FloorY(at.y)))
                    continue;
                SlowStatus slow = SlowStatus.On(enemy);
                if (slow == null)
                    continue;
                slow.State.ApplySlow(SkillRule.SlowOn(Sigil.Slow, IsGateBoss != null && IsGateBoss(enemy)), SigilZone.HoldSeconds);
                slow.State.ApplyHeat(SkillRule.SigilHeat, SigilZone.HoldSeconds);
                if (!enemy.TryGetComponent(out BurnStatus burn) || !burn.IsBurning)
                    Ignite?.Invoke(enemy, SkillRule.SigilStacks);
            }
            _targets.Clear();
        }

        // Crucible Vent (docs/45): every burning enemy in reach spends its stacks at once; each spent stack deals the
        // ability's damage to it and to every living enemy within BurnRule.VentSplashRadius of it. The stacks are all
        // spent before any damage lands, so an eruption that fells a neighbour never takes that neighbour's own stacks
        // with it unspent. A Vent with nothing burning in reach is still spent, as the player chose the moment.
        private bool Vent(AbilityRuntime ability)
        {
            if (!ability.TrySpend())
                return false;
            _vented.Clear();
            _ventedStacks.Clear();
            for (int i = 0; i < _targets.Count; i++)
            {
                if (!(_targets[i] is Health enemy) || !enemy.IsAlive)
                    continue;
                // TryGetComponent: a failed GetComponent allocates in the Editor for every unburnt enemy in reach.
                if (!enemy.TryGetComponent(out BurnStatus burn) || !burn.IsBurning)
                    continue;
                _vented.Add(enemy);
                _ventedStacks.Add(burn.Consume());
            }
            for (int i = 0; i < _vented.Count; i++)
            {
                Health centre = _vented[i];
                float damage = BurnRule.VentDamage(_ventedStacks[i], ability.Damage);
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
            float deltaTime = Time.deltaTime;
            Ability.Tick(deltaTime);
            if (Heading != null)
            {
                Vector2 steer = Heading();
                if (steer.sqrMagnitude >= .05f * .05f)
                    _facing = steer;
            }
            for (int i = 1; i < SlotCount; i++)
            {
                AbilityRuntime skill = _slots[i];
                if (skill == null)
                    continue;
                _active[i] = skill.IsActive;
                skill.Tick(deltaTime);
                if (_active[i] && !skill.IsActive)
                    End(skill);
            }
            if (Sigil != null && Sigil.IsLive)
                for (int ticks = Sigil.Tick(deltaTime); ticks > 0; ticks--)
                    SigilTick();
        }
    }
}
