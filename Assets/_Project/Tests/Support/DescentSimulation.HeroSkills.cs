using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // docs/57's skills 2 and 3 in the foundry simulation (phase 1, the owner's answers 2026-11-18): Iron Resolve and Shield
    // Rush for the Vanguard, Flame Sigil and Cinder Ward for Ilya, carried by a run whose FoundryLevers.Skills says so and
    // played by its cohort (SkillFootwork; AutopilotCohort.Habit.TimesTheSkills). Every shipped run carries skill 1 alone
    // (Skills 1): nothing below is made, drawn, traced or counted, so the default batch is the one it was to the byte.
    //
    // What the scene does, through the same bodies: the guard is armour on HeroStats for its seconds (SetGuardArmor, into
    // HealthState by HeroStatsBinding); the ward a pool in HealthState (RaiseWard; its WardAbsorbed sets the striker alight);
    // the Rush a HeroDash at the walk step and one capsule strike at the press (SkillRule.OnPath), its damage scaled by the
    // first weapon (SkillRule.Scaled), its daze a SlowState; the glyph a SigilZone on BurnRule's clock, whose ticks slow
    // (PackMotion.SetSlow), heat (a burn tick x SigilHeat) and light the enemies inside. The pilot presses a skill as
    // DevelopmentAutopilot does - a reaction after its moment came, never in a lapse (PilotRealism), never while the boss's
    // reveal holds the fight.
    internal static partial class DescentSimulation
    {
        // Data/Abilities/Ability_IronResolve.asset: +100 armour for 4 s, every 20 s; 2 is the crowd's reach its pilot reads.
        public static HeroAbility IronResolve() => new HeroAbility
        {
            Kind = AbilityKind.Guard, Damage = 0f, Radius = SkillFootwork.CrowdReach, Cooldown = 20f, Duration = 4f, Amount = 100f
        };

        // Data/Abilities/Ability_ShieldRush.asset: 4 units in 0.3 s, 15 (scaled) to every enemy within 0.8 of the path and a
        // 0.5 s daze, every 12 s.
        public static HeroAbility ShieldRush() => new HeroAbility
        {
            Kind = AbilityKind.Rush, Damage = 15f, Radius = .8f, Cooldown = 12f, Duration = .3f, Amount = 4f
        };

        // Data/Abilities/Ability_FlameSigil.asset: a glyph of 2.5 for 5 s slowing the walk inside 40 %, every 20 s.
        public static HeroAbility FlameSigil() => new HeroAbility
        {
            Kind = AbilityKind.Sigil, Damage = 0f, Radius = 2.5f, Cooldown = 20f, Duration = 5f, Amount = .4f
        };

        // Data/Abilities/Ability_CinderWard.asset: a ward of 20 % of the maximum health for 2 s, every 20 s.
        public static HeroAbility CinderWard() => new HeroAbility
        {
            Kind = AbilityKind.Ward, Damage = 0f, Radius = SkillFootwork.CrowdReach, Cooldown = 20f, Duration = 2f, Amount = .2f
        };

        // A hero's skills 2 and 3 as its HeroDefinition carries them, by its id; null for a hero without (the Riftblade).
        public static HeroAbility[] SkillsOf(FoundryHero hero)
        {
            if (hero == null)
                return null;
            if (hero.Id == "hero_vanguard")
                return new[] { IronResolve(), ShieldRush() };
            if (hero.Id == "hero_cinder_arcanist")
                return new[] { FlameSigil(), CinderWard() };
            return null;
        }

        internal sealed partial class FoundryLevers
        {
            // docs/57 section 6.1: how many of the hero's skills the run carries - 1, the shipped kit (skill 1 alone, every
            // report's default); 2 adds skill 2; 3 adds skill 3 as well. SkillTwoOff leaves skill 2 out of a run of 3 (skill 3
            // read beside skill 1 alone). Research levers, the simulation's alone until docs/57 phase 4 opens the skills.
            public int Skills = 1;
            public bool SkillTwoOff;
            // Research (phase 2's tuning): each skill's numbers as the run makes it, from its asset's (the delegate gets a
            // copy); null keeps the assets'.
            public Action<HeroAbility> TuneSkill;
            // Research: the share of the glyph's heat a gate boss takes (SkillRule.GateBossHeatShare, the scene's).
            public float SkillBossHeatShare = SkillRule.GateBossHeatShare;
            // Research (phase 2): the Rush stops on impact - RushImpactGap short of the first enemy on its path, striking up to
            // it (SkillRule.ImpactTravel) - rather than carrying the hero its whole length through what it meets; and the
            // careful pilot's engage on the gate boss in its window (SkillFootwork), on as section 3 has it.
            public bool RushStopsOnImpact = SkillRule.RushStopsOnImpact;
            public bool RushEngagesTheGateBoss = SkillFootwork.RushesTheGateBoss;
        }

        internal sealed partial class FoundryResult
        {
            // docs/57's skills 2 and 3 on this floor (index 0 skill 2, 1 skill 3): presses, and what each dealt (the Rush's
            // strikes; the glyph's extra burn is SigilBurnExtra, the ward's strikers' burns are DealtByBurn's).
            public int[] SkillUses = new int[2];
            public float[] SkillDealt = new float[2];
            // Iron Resolve: blows taken under it and what its armour kept off them. Cinder Ward: blows it took (some or all
            // of) and what it absorbed. Shield Rush: enemies its strikes landed on, and those it dazed. Flame Sigil: its ticks,
            // the enemies it slowed a tick (an enemy-tick each), the direct stacks it lit and what its heat added to burns.
            public int GuardedBlows;
            public float GuardPrevented;
            public int WardedBlows;
            public float WardAbsorbed;
            public int RushStruck;
            public int RushDazed;
            public int SigilTicks;
            public int SigilSlowed;
            public int SigilStacksLit;
            public float SigilBurnExtra;
            // Of the Rush's presses, those that were the escape out of a surround (the rest engaged, or were the clumsy
            // press at the nearest enemy).
            public int RushEscapes;
            // Seconds the pilot held a node fight across the glyph (SkillFootwork.AcrossTheGlyph).
            public float SigilLureSeconds;

            public float DealtBySkills => SkillDealt[0] + SkillDealt[1];
        }

        private sealed partial class Foe
        {
            // A skill's slow, daze and heat on it (SlowState): null until a skill first touches it.
            public SlowState Slow;
        }

        private sealed partial class FoundryRun
        {
            // The run's skills 2 and 3 (index 0, 1; null where the run leaves one out), null for a run of skill 1 alone.
            private AbilityRuntime[] _skills;
            // The Rush's dash, the press the next walk step takes and its heading; the glyph and the floor it was drawn on;
            // every enemy a skill has slowed, dazed or heated; the slot whose damage is being applied (Skill).
            private HeroDash _rush;
            private bool _rushPressed;
            private float _rushUnits;
            private float _rushX;
            private float _rushY;
            private SigilZone _sigil;
            private int _sigilFloor;
            private readonly List<Foe> _skillTouched = new List<Foe>();
            private int _skillSlot;
            // The pilot's read of the blows (SkillFootwork): the last one, the one that opened the fight; and since when each
            // skill's moment has held (PilotRealism: a press a reaction after it), NaN while not.
            private float _lastBlowAt = float.NegativeInfinity;
            private float _fightOpenedAt = float.NegativeInfinity;
            private float[] _skillSince;

            // CombatSetup's skills 2 and 3 as the run begins (phase 4 opens them; until then the lever): each made from its
            // asset's numbers (FoundryLevers.TuneSkill on a copy), its cooldown shortened by the gear's ability-cooldown line
            // as skill 1's is (GearStats.Apply). Nothing for a run of skill 1 alone, or for the Riftblade, whose dash stands
            // in the ability's place.
            private void BuildSkills()
            {
                if (_runLevers.Skills <= 1 || _rift != null)
                    return;
                HeroAbility[] own = SkillsOf(_heroDefinition);
                if (own == null)
                    return;
                _skills = new AbilityRuntime[2];
                _skillSince = new[] { float.NaN, float.NaN };
                float cooldown = GearStats.Factor(_heroDefinition.Gear, GearStat.AbilityCooldown);
                for (int i = 0; i < own.Length && i + 2 <= _runLevers.Skills; i++)
                {
                    if (i == 0 && _runLevers.SkillTwoOff)
                        continue;
                    HeroAbility made = own[i].Copy();
                    _runLevers.TuneSkill?.Invoke(made);
                    AbilityRuntime skill = made.CreateRuntime();
                    if (cooldown != 1f)
                        skill.ShortenCooldown(cooldown);
                    _skills[i] = skill;
                    if (skill.Kind == AbilityKind.Rush)
                        _rush = new HeroDash(skill.Amount, skill.Duration);
                    if (skill.Kind == AbilityKind.Ward)
                        _heroHealth.WardAbsorbed += WardTook;
                }
            }

            // HeroMovementInput's walk step for a hero with the Rush: a pressed Rush begins along its heading and, while it
            // travels, moves the hero its step (HeroDash.Move), the stick ignored. False when the walk is the stick's.
            private bool RushWalk()
            {
                if (_rushPressed)
                {
                    _rushPressed = false;
                    _rush.Begin(_rushX, _rushY, _rushUnits);
                }
                if (!_rush.IsDashing)
                    return false;
                _rush.Move(_heroMotion, Map, Step);
                return true;
            }

            // The skills' clocks, at the ability's place in the frame (the AbilityController's Update): an effect that ends -
            // the guard's armour, the ward - is taken off; the glyph ticks, then every touched enemy's slow is stepped and
            // handed to its pack, which reads it at its next step.
            private void TickSkills()
            {
                for (int i = 0; i < _skills.Length; i++)
                {
                    AbilityRuntime skill = _skills[i];
                    if (skill == null)
                        continue;
                    bool active = skill.IsActive;
                    skill.Tick(Step);
                    if (!active || skill.IsActive)
                        continue;
                    if (skill.Kind == AbilityKind.Guard)
                        _heroStats.SetGuardArmor(0f);
                    else if (skill.Kind == AbilityKind.Ward)
                        _heroHealth.ClearWard();
                }
                for (int i = 0; i < _skillTouched.Count; i++)
                    _skillTouched[i].Slow.Tick(Step);
                if (_sigil != null && _sigil.IsLive)
                {
                    if (_sigilFloor != _floor.FloorNumber)
                        _sigil.End();
                    else
                        for (int t = _sigil.Tick(Step); t > 0; t--)
                            SigilTick();
                }
                for (int i = _skillTouched.Count - 1; i >= 0; i--)
                {
                    Foe foe = _skillTouched[i];
                    if (foe.Released || !foe.IsAlive)
                    {
                        _skillTouched.RemoveAt(i);
                        continue;
                    }
                    foe.Motion.SetSlow(foe.Slot, foe.Slow.Current);
                    if (foe.Slow.IsQuiet)
                        _skillTouched.RemoveAt(i);
                }
            }

            private SlowState Touch(Foe foe)
            {
                if (foe.Slow == null)
                    foe.Slow = new SlowState();
                if (!_skillTouched.Contains(foe))
                    _skillTouched.Add(foe);
                return foe.Slow;
            }

            // One tick of the glyph: every enemy inside slowed (half on a gate boss) and heated until the next, an unburnt one
            // lit with a direct stack.
            private void SigilTick()
            {
                _result.SigilTicks++;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || !_sigil.Contains(foe.X, foe.Y))
                        continue;
                    SlowState slow = Touch(foe);
                    slow.ApplySlow(SkillRule.SlowOn(_sigil.Slow, foe.GateBoss), SigilZone.HoldSeconds);
                    slow.ApplyHeat(SkillRule.HeatOn(SkillRule.SigilHeat, foe.GateBoss, _runLevers.SkillBossHeatShare),
                        SigilZone.HoldSeconds);
                    _result.SigilSlowed++;
                    if (foe.Burn == null || !foe.Burn.IsBurning)
                    {
                        Ignite(foe, SkillRule.SigilStacks, false);
                        _result.SigilStacksLit++;
                    }
                }
            }

            // The glyph's lure (SkillFootwork.AcrossTheGlyph): a node fight held across the live glyph, for a cohort that times
            // the skills and lures, the spot on the floor.
            private bool SigilLure(AutopilotCohort cohort, float x, float y, out float gx, out float gy)
            {
                gx = x;
                gy = y;
                if (!_sigil.IsLive || _sigilFloor != _floor.FloorNumber || !cohort.TimesTheSkills || !cohort.Lures)
                    return false;
                float cx = 0f, cy = 0f;
                int crowd = 0;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || !SkillRule.Within(foe.X, foe.Y, x, y, SkillFootwork.LureReach))
                        continue;
                    cx += foe.X;
                    cy += foe.Y;
                    crowd++;
                }
                if (crowd == 0 || !SkillFootwork.AcrossTheGlyph(_sigil.X, _sigil.Y, _sigil.Radius, cx / crowd, cy / crowd, out gx, out gy) ||
                    !FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin))
                    return false;
                _result.SigilLureSeconds += Step;
                return true;
            }

            // A burn tick on an enemy inside the glyph: x SigilHeat (BurnRule.Scaled, the burn's own one body).
            private float Heated(Foe foe, float damage)
            {
                if (foe.Slow == null || !foe.Slow.IsHeated)
                    return damage;
                float heated = BurnRule.Scaled(damage, foe.Slow.CurrentHeat);
                _result.SigilBurnExtra += heated - damage;
                return heated;
            }

            // A daze holds the enemy's swing (its weapon's clock runs on), as the Sword's stagger did (EnemyAttack).
            private static bool Dazed(Foe foe) => foe.Slow != null && foe.Slow.IsDazed;

            // The ward took (some of) a blow: the striker - an enemy, never the pit - catches its direct stacks.
            private void WardTook(DamageContext context, float absorbed)
            {
                _result.WardedBlows++;
                _result.WardAbsorbed += absorbed;
                if (context.Source is Foe striker && IsValid(striker))
                    Ignite(striker, SkillRule.WardStacks, false);
            }

            // A blow the hero took (HeroDamaged, before the engaged clock reads it): the pilot's read of it, and the guard's
            // share of it.
            private void SkillsTookABlow(DamageContext context)
            {
                if (_seconds - _lastHeroHit > SkillFootwork.FightGap)
                    _fightOpenedAt = _seconds;
                _lastBlowAt = _seconds;
                if (_heroStats.GuardArmor > 0f)
                {
                    _result.GuardedBlows++;
                    float armour = _heroStats.Armor;
                    float without = armour / (armour + HeroStats.ArmorSoftness);
                    _result.GuardPrevented += context.Amount * (_heroStats.DamageReduction - without);
                }
            }

            // ---------------------------------------------------------------------------------------------------------
            // The pilot's presses (SkillFootwork)
            // ---------------------------------------------------------------------------------------------------------

            private void PilotSkills()
            {
                bool hold = !_heroHealth.IsAlive || _revealHold;
                AutopilotCohort cohort = _levers.Cohort;
                for (int i = 0; i < _skills.Length; i++)
                {
                    AbilityRuntime skill = _skills[i];
                    if (skill == null)
                        continue;
                    float hx = 0f, hy = 0f;
                    bool boss = false;
                    bool moment = !hold && skill.IsReady && !(skill.Kind == AbilityKind.Rush && (_rush.IsDashing || _rushPressed)) &&
                                  (cohort.TimesTheSkills ? Timed(skill, out hx, out hy, out boss) : OnCooldown(skill, out hx, out hy));
                    if (!moment)
                    {
                        _skillSince[i] = float.NaN;
                        continue;
                    }
                    if (float.IsNaN(_skillSince[i]))
                        _skillSince[i] = _seconds;
                    if (_realism != null &&
                        !Perceived(_skillSince[i], boss ? _realism.MarkReaction : _realism.FoeReaction))
                        continue;
                    _skillSince[i] = float.NaN;
                    UseSkill(i, hx, hy);
                }
            }

            // The careful pilot's moments (docs/57 section 3).
            private bool Timed(AbilityRuntime skill, out float hx, out float hy, out bool boss)
            {
                hx = hy = 0f;
                boss = false;
                float health = _heroHealth.Current, maximum = _heroHealth.Maximum;
                bool blow = _seconds - _lastBlowAt <= SkillFootwork.BlowMemory;
                switch (skill.Kind)
                {
                    case AbilityKind.Guard:
                        if (BlowComing(skill.Duration))
                            return boss = true;
                        return blow && SkillFootwork.GuardsTheBlow(_seconds - _fightOpenedAt <= SkillFootwork.BlowMemory,
                            CountWithin(SkillFootwork.CrowdReach), health, maximum);
                    case AbilityKind.Ward:
                        if (BlowComing(skill.Duration))
                            return boss = true;
                        return GateBoss == null && blow && SkillFootwork.WardsTheBlow(health, maximum);
                    case AbilityKind.Sigil:
                    {
                        Foe gate = GateBoss;
                        bool beside = gate != null && IsValid(gate) && SkillRule.Within(gate.X, gate.Y, HeroX, HeroY, skill.Radius);
                        return SkillFootwork.DrawsTheSigil(CountWithin(skill.Radius), beside);
                    }
                    case AbilityKind.Rush:
                        _rushEscape = false;
                        return RushEscape(skill, out hx, out hy) || RushEngage(skill, out hx, out hy);
                    default:
                        return false;
                }
            }

            // The gate boss's blow on its way to the hero and due within window: the Warden's mark (or the Regent's lanes)
            // round the hero, the telegraph far enough on - the escape has not cleared the hero out of it yet.
            private bool BlowComing(float window)
            {
                Foe boss = _slamBoss;
                if (boss == null || !boss.IsAlive || !_slam.IsMarked ||
                    !SkillFootwork.BlowDueWithin(_slam.TelegraphProgress, _slam.Telegraph, window, Step))
                    return false;
                if (IsRegent(boss))
                    return _regentLanes != null && _regentLanes.Holds(HeroX, HeroY, 0f);
                return SkillRule.Within(HeroX, HeroY, _markX, _markY, WardenRule.SlamRadius);
            }

            // Out of a surround: the kite's heading, unless it crosses a glowing band the cohort keeps out of.
            private bool RushEscape(AbilityRuntime skill, out float hx, out float hy)
            {
                hx = hy = 0f;
                float x = HeroX, y = HeroY;
                if (!SkillFootwork.Surrounded(CountWithin(SkillFootwork.EscapeReach), _heroHealth.Current, _heroHealth.Maximum))
                    return false;
                AutopilotCohort cohort = _levers.Cohort;
                bool glowing = GlowSeen();
                if (!Threat(cohort, x, y, out float tx, out float ty) || !KiteHeading(cohort, x, y, tx, ty, glowing, out hx, out hy))
                    return false;
                Normalize(ref hx, ref hy);
                if (cohort.Guards(glowing) && FootworkRule.StepsIntoBand(x, y, x + hx * skill.Amount, y + hy * skill.Amount))
                    return false;
                _rushEscape = true;
                return true;
            }

            private bool _rushEscape;

            // Onto a lone target: the elite room's Captain, or the gate boss in the window after its blow, beyond the first
            // weapon's reach and within the Rush's length, nothing else within LoneClear of the hero.
            private bool RushEngage(AbilityRuntime skill, out float hx, out float hy)
            {
                hx = hy = 0f;
                Foe target = null;
                Foe gate = GateBoss;
                if (gate != null && IsValid(gate) && _runLevers.RushEngagesTheGateBoss && !_slam.IsMarked &&
                    _slam.HoldsWeaponFor(_levers.WardenRecoverySeconds))
                    target = gate;
                else if (_eliteWave && _elites.Count > 0 && IsValid(_elites[0]))
                    target = _elites[0];
                if (target == null)
                    return false;
                float x = HeroX, y = HeroY;
                float distance = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(target.X, target.Y, x, y));
                float reach = _loadout.Primary != null ? _loadout.Primary.Range : 0f;
                if (!SkillFootwork.WorthTheRush(distance, reach, skill.Amount))
                    return false;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe other = TargetAt(i);
                    if (ReferenceEquals(other, target) || !IsValid(other))
                        continue;
                    if (SkillRule.Within(other.X, other.Y, x, y, SkillFootwork.LoneClear))
                        return false;
                }
                hx = target.X - x;
                hy = target.Y - y;
                return true;
            }

            // The clumsy pilot: on cooldown whenever an enemy stands within the skill's reach; the Rush toward the nearest.
            private bool OnCooldown(AbilityRuntime skill, out float hx, out float hy)
            {
                hx = hy = 0f;
                float reach = SkillFootwork.ReachOf(skill);
                if (!SkillFootwork.PressesOnCooldown(CountWithin(reach)))
                    return false;
                if (skill.Kind != AbilityKind.Rush)
                    return true;
                Foe nearest = Acquire(reach);
                if (nearest == null)
                    return false;
                hx = nearest.X - HeroX;
                hy = nearest.Y - HeroY;
                return true;
            }

            // The living enemies within radius of the hero (DevelopmentAutopilot counts the encounter's standing enemies the
            // same way).
            private int CountWithin(float radius)
            {
                int count = 0;
                float x = HeroX, y = HeroY;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (IsValid(foe) && SkillRule.Within(foe.X, foe.Y, x, y, radius))
                        count++;
                }
                return count;
            }

            // AbilityController.TryUse for slot index + 2.
            private void UseSkill(int index, float hx, float hy)
            {
                AbilityRuntime skill = _skills[index];
                bool escape = _rushEscape;
                _rushEscape = false;
                if (!skill.TrySpend())
                    return;
                _result.SkillUses[index]++;
                if (_trace != null)
                    Emit($"skill_used slot={index + 2} kind={skill.Kind} room={_roomIndex + 1} hp={N(_heroHealth.Current)}");
                switch (skill.Kind)
                {
                    case AbilityKind.Guard:
                        _heroStats.SetGuardArmor(skill.Amount);
                        break;
                    case AbilityKind.Ward:
                        _heroHealth.RaiseWard(SkillRule.WardOf(_heroHealth.Maximum, skill.Amount));
                        break;
                    case AbilityKind.Sigil:
                        _sigil ??= new SigilZone();
                        _sigil.Begin(HeroX, HeroY, skill.Radius, skill.Duration, skill.Amount);
                        _sigilFloor = _floor.FloorNumber;
                        break;
                    case AbilityKind.Rush:
                        if (escape)
                            _result.RushEscapes++;
                        Rush(index, skill, hx, hy);
                        break;
                }
            }

            // Shield Rush at the press: the strike along the path the dash will take (one capsule), each enemy on it struck
            // for the damage scaled by the first weapon and dazed (a gate boss never); the dash itself begins at the next walk
            // step.
            private void Rush(int index, AbilityRuntime skill, float hx, float hy)
            {
                Normalize(ref hx, ref hy);
                if (hx == 0f && hy == 0f)
                    hy = -1f;
                _rushPressed = true;
                _rushX = hx;
                _rushY = hy;
                _rushUnits = skill.Amount;
                float x = HeroX, y = HeroY;
                float toX = x + hx * skill.Amount, toY = y + hy * skill.Amount;
                WeaponRuntime first = _loadout.Primary;
                float damage = first != null ? SkillRule.Scaled(skill.Damage, first.Damage, first.BaseDamage) : skill.Damage;
                _abilityTargets.Clear();
                float impact = float.PositiveInfinity;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || !SkillRule.OnPath(foe.X, foe.Y, x, y, toX, toY, skill.Radius))
                        continue;
                    _abilityTargets.Add(foe);
                    // Only what stands ahead stops it; what stands beside the hero as it sets off is struck in passing.
                    float along = SkillRule.Along(foe.X, foe.Y, x, y, hx, hy);
                    if (along > 0f)
                        impact = Math.Min(impact, along);
                }
                // On impact (research): the dash halts short of the first enemy on its path and strikes only up to it.
                if (_runLevers.RushStopsOnImpact && !float.IsPositiveInfinity(impact))
                {
                    _rushUnits = SkillRule.ImpactTravel(impact, skill.Amount);
                    float reachX = x + hx * Math.Max(0f, impact), reachY = y + hy * Math.Max(0f, impact);
                    for (int i = _abilityTargets.Count - 1; i >= 0; i--)
                    {
                        var foe = (Foe)_abilityTargets[i];
                        if (!SkillRule.OnPath(foe.X, foe.Y, x, y, reachX, reachY, skill.Radius))
                            _abilityTargets.RemoveAt(i);
                    }
                }
                _source = DamageSource.Skill;
                _skillSlot = index;
                for (int i = 0; i < _abilityTargets.Count; i++)
                {
                    var foe = (Foe)_abilityTargets[i];
                    if (!foe.IsAlive)
                        continue;
                    _result.RushStruck++;
                    foe.ApplyDamage(new DamageContext(damage, _hero));
                    if (foe.IsAlive && !foe.GateBoss)
                    {
                        Touch(foe).Daze(SkillRule.DazeSeconds);
                        _result.RushDazed++;
                    }
                }
                _abilityTargets.Clear();
            }

            // The skill's damage landing (FoeDamaged's Skill).
            private void SkillDealt(Foe foe, float dealt)
            {
                _result.SkillDealt[_skillSlot] += dealt;
                if (foe.GateBoss)
                    _result.WardenFromSkills += dealt;
            }
        }

        internal sealed partial class FoundryResult
        {
            // What the skills took off the gate boss (the Rush's strikes).
            public float WardenFromSkills;
        }
    }
}
