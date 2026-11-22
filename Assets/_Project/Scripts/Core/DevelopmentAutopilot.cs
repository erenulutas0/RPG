using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using Cryptforge.Progression;
using Cryptforge.UI;
using UnityEngine;

namespace Cryptforge.Core
{
    // A development build's hands-off player for device checks of the foundry (DevelopmentStart.Autopilot): it walks the
    // hero to each marked hall, picks up the drops beside it and takes both altars between fights, kites any enemy that
    // comes close, draws a node fight to the pit's rim and steps out of the band while the rim glows, so the run's log
    // measures what the floor does to a hero who plays it. It answers every offer by its card score and every checkpoint
    // with Descend, and walks onto an open exit (a minute into the finale's swarm). It steers through
    // HeroMovementInput.Hold, so a finger on the screen still takes over. Never part of a player's build. A word of
    // development/autopilot.txt may name a cohort (AutopilotCohort: standing, first-card, clumsy, auto-farmer) that drops
    // some of these habits; the foundry simulation's pilot reads the same habits in the same order.
    public sealed class DevelopmentAutopilot : MonoBehaviour
    {
        private const float ChoiceDelay = .8f;
        // Design units beyond the pit's hole: where the hero lures (inside the band) and where it waits out a spill.
        private const float LureDepth = 1.3f;
        private const float ShelterDepth = PitEruptionRule.Reach + 1.4f;
        private const float Arrived = .35f;
        private const float SwarmStay = 60f;
        // How far between fights the autopilot steps aside for a drop. It was 10, and with a horde dropping experience all
        // round the hero it chased drops for minutes instead of walking to the next hall; the floor's end sweeps the rest.
        private const float DropDetour = 4f;
        // At 4 the horde still fell within reach of the hero all along a walk, and the autopilot chased one drop after
        // another, hundreds a walk, while the next hall waited minutes (the Editor's Ilya seed 22: 195 s to its second
        // hall). So while it has somewhere to go - an altar still to take or a marked hall - it steps aside only for a drop
        // beside the way, one adding at most DetourSlack to it (DropDetourRule.AddedPath): anything ahead within
        // DropDetour, a step to either side, nothing behind. The slack alone left a tail - a drop just within it on either
        // side in turn can hold the hero on the spot - so a walk also spends at most DetourBudget seconds steering at
        // drops, about the longest hundredth of the foundry simulation's walks under the slack alone; a fight or a newly
        // marked hall starts the count again. However thick the horde, the drops hold a walk at most twice DetourBudget
        // beyond its way (DropDetourRule.Walk). With nowhere to go - waiting on the gate's clock, between a hall's waves
        // once the altars are taken - nothing waits on the hero and it takes every drop within DropDetour, as before.
        private const float DetourSlack = 2f;
        private const float DetourBudget = 12f;
        private readonly DropDetourRule.Walk _detour = new DropDetourRule.Walk(DropDetour, DetourSlack, DetourBudget);
        // While the pit's band glows the autopilot steps into it only to leave the Warden's mark (Goal, Steer): the old
        // autopilot stepped out to shelter and was drawn straight back in to lure or kite, and four in five of the pit's
        // burns in the foundry simulation were that (2026-11-04, seeds 1-200 a hero).
        private bool _bandGuard;
        // Kiting, as a player in a crowd does (docs/41): every enemy within KiteReach - or within its own reach and a margin,
        // where that is longer (FootworkRule.ThreatReach) - pushes the hero away, the nearer the harder, and the hero walks
        // away from the push turned KiteTurn round it, so it circles the crowd instead of backing into a wall. Just beyond a
        // melee enemy's 1.5: at 2.2 it kited all the time and never stopped for a drop.
        private const float KiteReach = 1.6f;
        private const float KiteTurn = 35f * Mathf.Deg2Rad;
        private const float KiteStride = 2f;
        private CombatSetup _setup;
        private HeroMovementInput _movement;
        private Transform _hero;
        private readonly FoundryPursuit _route = new FoundryPursuit();
        private float _choiceWait;
        // development/autopilot.txt's measurement modes for the floor's clock (docs/21, the owner's run of 2026-10-26):
        // "linger" fights the horde and gathers every drop from the floor's second room on and never walks to the next
        // hall - a player farming the walk; "idle" stands still from the second room on - the phone left on the floor.
        // Either still answers every offer. Anything else plays the floor.
        private bool _linger;
        private bool _idle;
        // Who plays (AutopilotCohort.FromSwitch): a cohort's name among the file's words, the careful pilot otherwise.
        private AutopilotCohort _cohort = AutopilotCohort.Careful;
        // How far a lingering hero walks for a drop: the whole court, since it has nowhere else to go.
        private const float LingerDetour = 30f;
        // The ability fires once this many enemies stand in its reach - burning ones for a Vent - or at once beside the
        // gate boss, as a player saves a burst for a crowd (docs/45: the Cinder Arcanist is measured with its Vent).
        private const int AbilityCrowd = 3;
        private AbilityController _ability;
        private Targeting _targeting;
        private Health _heroHealth;
        private readonly System.Collections.Generic.List<IDamageable> _inReach = new System.Collections.Generic.List<IDamageable>();

        internal void Initialize(CombatSetup setup, HeroMovementInput movement, Transform hero)
        {
            _setup = setup;
            _movement = movement;
            _hero = hero;
            _ability = hero.GetComponent<AbilityController>();
            _targeting = hero.GetComponent<Targeting>();
            _heroHealth = hero.GetComponent<Health>();
            string mode = DevelopmentStart.AutopilotMode();
            string[] words = mode.Split((char[])null, System.StringSplitOptions.RemoveEmptyEntries);
            _linger = System.Array.IndexOf(words, "linger") >= 0;
            _idle = System.Array.IndexOf(words, "idle") >= 0;
            _cohort = AutopilotCohort.FromSwitch(mode);
            // docs/57's skills read the blows the hero takes (SkillFootwork's moments).
            if (_heroHealth != null)
                _heroHealth.Damaged += OnHeroHit;
            Debug.Log("[Autopilot] driving the hero as the " + _cohort.Name + " cohort (development/autopilot.txt" +
                      (mode.Length > 0 ? ": " + mode : string.Empty) + ")");
        }

        private void OnDestroy()
        {
            if (_heroHealth != null)
                _heroHealth.Damaged -= OnHeroHit;
        }

        private void Update()
        {
            _clock += Time.deltaTime;
            if (_movement == null || _setup.Run == null || _setup.Run.HasEnded)
            {
                _movement?.Release();
                return;
            }
            if (_setup.Choices.IsOpen)
            {
                _movement.Release();
                _choiceWait += Time.unscaledDeltaTime;
                if (_choiceWait >= ChoiceDelay)
                {
                    _choiceWait = 0f;
                    using (PerformanceMarkers.Pick.Auto())
                        _setup.Choices.TrySelect(_setup.Choices.Current, ChoiceSlot());
                }
                return;
            }
            _choiceWait = 0f;
            if (_idle && Lingering())
            {
                _movement.Release();
                return;
            }
            if (_cohort.UsesTheAbility)
                TryAbility();
            // docs/57's skills 2 and 3, where the run carries them (DevelopmentStart.SkillSlots).
            if (_cohort.UsesTheAbility && _ability != null && (_ability.SkillAt(2) != null || _ability.SkillAt(3) != null))
                TrySkills();
            float x = _hero.position.x, y = ArenaFloor.FloorY(_hero.position.y);
            if (Goal(x, y, out float gx, out float gy, out float stop))
                Steer(x, y, gx, gy, stop);
            else
                _movement.Release();
        }

        private void TryAbility()
        {
            // Not while the boss's reveal holds the fight: TryUse would refuse it (AbilityController.FightHold).
            if (_ability == null || _ability.Ability == null || !_ability.Ability.IsReady || _ability.IsHeld || _targeting == null)
                return;
            _targeting.CollectNear(_heroHealth, _ability.Ability.Radius, _inReach);
            int count = 0;
            for (int i = 0; i < _inReach.Count; i++)
            {
                if (!(_inReach[i] is Health enemy) || !enemy.IsAlive)
                    continue;
                // TryGetComponent: this runs every frame the Vent is ready, and a failed GetComponent allocates in the
                // Editor for each enemy in reach that has never burned.
                if (_ability.Ability.Kind == AbilityKind.Vent &&
                    (!enemy.TryGetComponent(out BurnStatus burn) || !burn.IsBurning))
                    continue;
                count++;
            }
            _inReach.Clear();
            bool boss = _setup.Warden != null && _setup.Warden.Boss != null && _setup.Warden.Boss.IsAlive;
            if (count >= AbilityCrowd || (boss && count > 0))
                _ability.TryUse();
        }

        // ---------------------------------------------------------------------------------------------------------------
        // docs/57's skills 2 and 3: SkillFootwork's moments, as the foundry simulation's pilot reads them (PilotSkills)
        // ---------------------------------------------------------------------------------------------------------------

        // Scaled seconds since the autopilot began, the blows' clock; the last blow the hero took and the one that opened
        // its fight.
        private float _clock;
        private float _lastBlowAt = float.NegativeInfinity;
        private float _fightOpenedAt = float.NegativeInfinity;

        private void OnHeroHit(DamageContext hit)
        {
            if (hit.Amount <= 0f)
                return;
            if (_clock - _lastBlowAt > SkillFootwork.FightGap)
                _fightOpenedAt = _clock;
            _lastBlowAt = _clock;
        }

        private void TrySkills()
        {
            if (_ability.IsHeld || _heroHealth == null || !_heroHealth.IsAlive)
                return;
            for (int slot = 2; slot <= AbilityController.SlotCount; slot++)
            {
                AbilityRuntime skill = _ability.SkillAt(slot);
                if (skill == null || !skill.IsReady || (skill.Kind == AbilityKind.Rush && _ability.Dash != null && _ability.Dash.IsDashing))
                    continue;
                float hx = 0f, hy = 0f;
                bool press = _cohort.TimesTheSkills ? Timed(skill, out hx, out hy) : OnCooldown(skill, out hx, out hy);
                if (press)
                    _ability.TryUse(slot, new Vector2(hx, hy));
            }
        }

        private bool Timed(AbilityRuntime skill, out float hx, out float hy)
        {
            hx = hy = 0f;
            float x = _hero.position.x, y = ArenaFloor.FloorY(_hero.position.y);
            float health = _heroHealth.Current, maximum = _heroHealth.Maximum;
            bool blow = _clock - _lastBlowAt <= SkillFootwork.BlowMemory;
            EncounterController encounter = _setup.Encounters;
            switch (skill.Kind)
            {
                case AbilityKind.Guard:
                    return BlowComing(x, y, skill.Duration) ||
                           (blow && SkillFootwork.GuardsTheBlow(_clock - _fightOpenedAt <= SkillFootwork.BlowMemory,
                               CountWithin(x, y, SkillFootwork.CrowdReach), health, maximum));
                case AbilityKind.Ward:
                    return BlowComing(x, y, skill.Duration) ||
                           (encounter.GateBoss == null && blow && SkillFootwork.WardsTheBlow(health, maximum));
                case AbilityKind.Sigil:
                {
                    Health gate = encounter.GateBoss;
                    bool beside = gate != null && encounter.TryFloorPositionOf(gate, out float gx, out float gy) &&
                                  SkillRule.Within(gx, gy, x, y, skill.Radius);
                    return SkillFootwork.DrawsTheSigil(CountWithin(x, y, skill.Radius), beside);
                }
                case AbilityKind.Rush:
                    return RushEscape(x, y, skill, out hx, out hy) || RushEngage(x, y, skill, out hx, out hy);
                default:
                    return false;
            }
        }

        // The gate boss's blow on its way to the hero and due within window: its mark (or the Regent's lanes) round the hero.
        private bool BlowComing(float x, float y, float window)
        {
            WardenSlamService warden = _setup.Warden;
            if (warden == null || !warden.IsFighting || !warden.Clock.IsMarked ||
                !SkillFootwork.BlowDueWithin(warden.Clock.TelegraphProgress, warden.Clock.Telegraph, window, Time.deltaTime))
                return false;
            return warden.MarksLanes
                ? warden.Lanes.Holds(x, y, 0f)
                : SkillRule.Within(x, y, warden.MarkX, warden.MarkY, WardenRule.SlamRadius);
        }

        private bool RushEscape(float x, float y, AbilityRuntime skill, out float hx, out float hy)
        {
            hx = hy = 0f;
            if (!SkillFootwork.Surrounded(CountWithin(x, y, SkillFootwork.EscapeReach), _heroHealth.Current, _heroHealth.Maximum))
                return false;
            bool glowing = _setup.Pit != null && _setup.Pit.IsActive && _setup.Pit.IsRunning && _setup.Pit.Clock.IsWarning;
            if (!Threat(x, y, out float tx, out float ty))
                return false;
            if (_cohort.Kite(x, y, tx, ty, KiteTurn, KiteStride, glowing, out float gx, out float gy))
            {
                hx = gx - x;
                hy = gy - y;
            }
            else
            {
                hx = tx;
                hy = ty;
            }
            float length = Mathf.Sqrt(hx * hx + hy * hy);
            if (length <= 1e-3f)
                return false;
            hx /= length;
            hy /= length;
            return !(_cohort.Guards(glowing) && FootworkRule.StepsIntoBand(x, y, x + hx * skill.Amount, y + hy * skill.Amount));
        }

        private bool RushEngage(float x, float y, AbilityRuntime skill, out float hx, out float hy)
        {
            hx = hy = 0f;
            EncounterController encounter = _setup.Encounters;
            WardenSlamService warden = _setup.Warden;
            Health target = SkillFootwork.RushesTheGateBoss && encounter.GateBoss != null && warden != null && warden.IsFighting &&
                            !warden.Clock.IsMarked && warden.Clock.HoldsWeapon
                ? encounter.GateBoss
                : encounter.EliteLeader;
            if (target == null || !target.IsAlive || !encounter.TryFloorPositionOf(target, out float tx, out float ty))
                return false;
            float distance = Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(tx, ty, x, y));
            WeaponRuntime first = _setup.Weapon;
            if (!SkillFootwork.WorthTheRush(distance, first != null ? first.Range : 0f, skill.Amount))
                return false;
            for (int i = 0; i < encounter.EnemyCount; i++)
            {
                Health other = encounter.EnemyAt(i);
                if (other == null || ReferenceEquals(other, target) || !other.IsAlive ||
                    !encounter.TryFloorPositionOf(other, out float ox, out float oy))
                    continue;
                if (SkillRule.Within(ox, oy, x, y, SkillFootwork.LoneClear))
                    return false;
            }
            hx = tx - x;
            hy = ty - y;
            return true;
        }

        // The clumsy cohort: on cooldown whenever an enemy stands within the skill's reach; the Rush toward the nearest.
        private bool OnCooldown(AbilityRuntime skill, out float hx, out float hy)
        {
            hx = hy = 0f;
            float x = _hero.position.x, y = ArenaFloor.FloorY(_hero.position.y);
            float reach = SkillFootwork.ReachOf(skill);
            if (!SkillFootwork.PressesOnCooldown(CountWithin(x, y, reach)))
                return false;
            if (skill.Kind != AbilityKind.Rush)
                return true;
            Health nearest = _targeting != null ? _targeting.Acquire(reach) : null;
            if (nearest == null)
                return false;
            hx = nearest.transform.position.x - x;
            hy = ArenaFloor.FloorY(nearest.transform.position.y) - y;
            return true;
        }

        private bool SigilLure(float x, float y, out float gx, out float gy)
        {
            gx = x;
            gy = y;
            SigilZone glyph = _ability != null ? _ability.Sigil : null;
            if (glyph == null || !glyph.IsLive || !_cohort.TimesTheSkills || !_cohort.Lures)
                return false;
            EncounterController encounter = _setup.Encounters;
            float cx = 0f, cy = 0f;
            int crowd = 0;
            for (int i = 0; i < encounter.EnemyCount; i++)
            {
                Health enemy = encounter.EnemyAt(i);
                if (enemy == null || !enemy.IsAlive || !encounter.TryFloorPositionOf(enemy, out float ex, out float ey) ||
                    !SkillRule.Within(ex, ey, x, y, SkillFootwork.LureReach))
                    continue;
                cx += ex;
                cy += ey;
                crowd++;
            }
            return crowd > 0 && SkillFootwork.AcrossTheGlyph(glyph.X, glyph.Y, glyph.Radius, cx / crowd, cy / crowd, out gx, out gy) &&
                   FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin);
        }

        private int CountWithin(float x, float y, float radius)
        {
            EncounterController encounter = _setup.Encounters;
            int count = 0;
            for (int i = 0; i < encounter.EnemyCount; i++)
            {
                Health enemy = encounter.EnemyAt(i);
                if (enemy != null && enemy.IsAlive && encounter.TryFloorPositionOf(enemy, out float ex, out float ey) &&
                    SkillRule.Within(ex, ey, x, y, radius))
                    count++;
            }
            return count;
        }

        private int ChoiceSlot()
        {
            CheckpointOffer offer = _setup.Checkpoint.CurrentOffer;
            if (_setup.Choices.Current.Kind == ChoiceKind.Checkpoint && offer != null)
                for (int i = 0; i < offer.Choices.Count; i++)
                    if (offer.Choices[i].Kind == CheckpointKind.Descend)
                        return i;
            UpgradeOffer upgrade = _setup.Upgrades.CurrentOffer;
            if (_setup.Choices.Current.Kind != ChoiceKind.Upgrade || upgrade == null || !_setup.Upgrades.IsCategoryMode)
                return 0;
            // A build, not the first card (docs/42's CardPicker order, AutopilotCohort.CardScore, each card at the tier it
            // is offered): health when the hero is low, an evolution, a new weapon while a slot is free, two tomes, a line
            // toward an evolution, the tomes' levels, the other lines, then the hero's cards, health and armour by their
            // tier. A third weapon scored 10, under every tome level, so the autopilot never took one and a Vanguard whose
            // second was the Daggers never got the Staff (the survival research, 2026-11-04); every line outranked the
            // tomes' levels until 2026-11-10 (docs/41). The first-card cohorts take the first card.
            Health hero = _hero.GetComponent<Health>();
            bool low = hero != null && hero.Current < hero.Maximum * .5f;
            return _cohort.Pick(upgrade, low, _setup.Upgrades);
        }

        private bool Goal(float x, float y, out float gx, out float gy, out float stop)
        {
            EncounterController encounter = _setup.Encounters;
            stop = Arrived;
            gx = x; gy = y;
            bool fighting = encounter.AliveEnemyCount > 0;
            // A fight, or a hall newly marked, gives the walk after it its whole drop budget.
            _detour.Track(fighting, encounter.IsRoomSiteOpen);
            // While the pit's band glows a cohort that keeps out of it (AutopilotCohort.Guards) steps into it only to leave
            // the Warden's mark (Steer).
            bool pit = _setup.Pit != null && _setup.Pit.IsActive;
            bool glowing = pit && _setup.Pit.IsRunning && _setup.Pit.Clock.IsWarning;
            bool guarded = _cohort.Guards(glowing);
            _bandGuard = guarded;
            // The finale's exit opens at once; the autopilot fights the swarm a while first, as a player after depth does.
            if (encounter.IsExitOpen && (!encounter.IsInSwarmRoom || encounter.SwarmSeconds >= SwarmStay))
            {
                gx = encounter.ExitFloorX; gy = encounter.ExitFloorY;
                return true;
            }
            // docs/62 P0a: leaving the Outworks, the stair before anything else (the simulation's OutworksLeaveGoal).
            if (OutworksLeaveGoal(x, y, out gx, out gy, out stop))
                return true;
            // The Warden's mark under the hero: out of it first, as a player who reads it does - away from the Warden, with
            // room, not across the glowing band if it can help it (FootworkRule.EscapeMark; from the mark itself, the
            // nearest way out, if the Warden cannot be found). The clumsy cohorts take the nearest way out, wherever the
            // Warden stands (AutopilotCohort.EscapeMark).
            WardenSlamService warden = _setup.Warden;
            // The Glass Regent's lanes (docs/50 section 6): a cohort that leaves the Warden's mark away from it sidesteps
            // across them instead (AutopilotCohort.Sidesteps, FootworkRule.Sidestep) - and, once clear of them, fights on;
            // the clumsy cohorts leave a lane's mark as a circle's, the nearest way, wherever it runs.
            bool lanes = warden != null && warden.MarksLanes && warden.Lanes.Count > 0 && _cohort.Sidesteps;
            if (_cohort.EscapesMarks && lanes && warden.IsFighting && warden.Clock.IsMarked &&
                FootworkRule.Sidestep(x, y, warden.Lanes, out gx, out gy))
            {
                stop = 0f;
                _bandGuard = false;
                return true;
            }
            if (_cohort.EscapesMarks && !lanes && warden != null && warden.IsFighting && warden.Clock.IsMarked)
            {
                float mx = x - warden.MarkX, my = y - warden.MarkY;
                float reach = WardenRule.SlamRadius + .6f;
                if (!encounter.TryFloorPositionOf(warden.Boss, out float bossX, out float bossY))
                {
                    bossX = warden.MarkX; bossY = warden.MarkY;
                }
                if (mx * mx + my * my < reach * reach &&
                    _cohort.EscapeMark(x, y, warden.MarkX, warden.MarkY, reach + .4f, bossX, bossY, glowing, out gx, out gy))
                {
                    stop = 0f;
                    _bandGuard = false;
                    return true;
                }
            }
            // A spill coming and the hero on the rim: shelter first, whatever is chasing (FootworkRule.Shelter; the rim
            // spot, wherever it lies, for a cohort that does not keep out of the glow).
            if (_cohort.Shelters && glowing && PitEruptionRule.InBand(x, y))
            {
                _cohort.Shelter(x, y, ShelterDepth, out gx, out gy);
                return true;
            }
            // Stepping in (AutopilotCohort.Habit.StepsIn, FootworkRule.StepIn), for a cohort built with it: a single elite
            // room's Captain - or the gate boss, with StepsInOnTheGateBoss - held at the best weapon's reach, when that weapon
            // is short and the hero safe.
            if (_cohort.StepsIn && StepInGoal(x, y, glowing, out gx, out gy))
            {
                stop = FootworkRule.StepInStop;
                return true;
            }
            // Kiting with room (FootworkRule.Kite): along a wall or turned away from it, never into it, nor into the band
            // while it glows; straight on for the clumsy cohorts, and not at all for the standing ones.
            if (_cohort.Kites && Threat(x, y, out float tx, out float ty) &&
                _cohort.Kite(x, y, tx, ty, KiteTurn, KiteStride, glowing, out gx, out gy))
            {
                stop = 0f;
                return true;
            }
            // docs/62 P0a: the Outworks' Keeper or nearest anvil after the kite (the simulation's OutworksObjectiveGoal).
            if (OutworksObjectiveGoal(x, y, out gx, out gy, out stop))
                return true;
            // A node fight is drawn to the pit's rim while the pit runs, but not while a guarded band glows: then the hero
            // waits out of it. The horde alone is fought wherever the hero is going. Never in a swarm room, where the pit
            // does not run (PitEruptionService.IsRunning): the careful pilot spent about 44 s of the finale dragging the
            // swarm to a rim that never spills (the review of 2026-11-11).
            // docs/57's glyph (a run with Flame Sigil): while it lives a node fight is held across it, as the simulation's pilot
            // holds it (SkillFootwork.AcrossTheGlyph).
            if (fighting && !guarded && SigilLure(x, y, out gx, out gy))
                return true;
            if (fighting && pit && _setup.Pit.IsRunning)
            {
                if (guarded || !_cohort.Lures)
                    return false;
                FootworkRule.RimSpot(x, y, LureDepth, out gx, out gy);
                return true;
            }
            if (fighting)
                return false;
            // Between fights, as a player would: the floor's altars, then the next hall, with the drops beside the way
            // there (DetourSlack, DetourBudget); with nowhere to go, or lingering, every drop in sight.
            bool lingering = _linger && Lingering();
            bool altar = NearestAltar(x, y, out float wx, out float wy);
            bool hall = !altar && encounter.IsRoomSiteOpen && !lingering;
            if (hall)
            {
                wx = encounter.RoomSiteFloorX; wy = encounter.RoomSiteFloorY;
            }
            if (lingering || (!altar && !hall))
            {
                if (DropDetourRule.Nearest(_setup.Drops, x, y, lingering ? LingerDetour : DropDetour, out gx, out gy))
                {
                    stop = DropRule.PickupRadius * .5f;
                    return true;
                }
            }
            else if (_detour.Choose(_setup.Drops, x, y, wx, wy, Time.deltaTime, out gx, out gy))
            {
                stop = DropRule.PickupRadius * .5f;
                return true;
            }
            gx = wx; gy = wy;
            if (altar)
            {
                stop = .1f;
                return true;
            }
            if (hall)
            {
                stop = FoundryMap.RoomSiteRadius * .5f;
                return true;
            }
            return false;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // docs/62 P0a: the Outworks phase (development/outworks.txt), as the simulation's pilot plays it
        // (DescentSimulation.Outworks.cs): the objective by OutworksPilot, a pad where it saves the walk and the cohort takes
        // pads, the drops beside the way; with nothing to go for, the altars, the drops in sight, else hold ground.
        // ---------------------------------------------------------------------------------------------------------------

        private bool OutworksLeaveGoal(float x, float y, out float gx, out float gy, out float stop)
        {
            gx = x; gy = y;
            stop = OutworksPilot.StairStop;
            EncounterController encounter = _setup.Encounters;
            if (!encounter.IsInOutworks)
                return false;
            OutworksPilot.Habits habits = OutworksPilot.Of(_cohort);
            if (!OutworksPilot.Leaving(encounter.Outworks, habits, x, y, null))
                return false;
            gx = encounter.Outworks.StairX; gy = encounter.Outworks.StairY;
            ByPad(habits, encounter, x, y, ref gx, ref gy, ref stop);
            return true;
        }

        private bool OutworksObjectiveGoal(float x, float y, out float gx, out float gy, out float stop)
        {
            gx = x; gy = y;
            stop = Arrived;
            EncounterController encounter = _setup.Encounters;
            if (!encounter.IsInOutworks)
                return false;
            OutworksPilot.Habits habits = OutworksPilot.Of(_cohort);
            Health keeper = encounter.KeeperLeader;
            float keeperX = 0f, keeperY = 0f;
            bool keeperStands = keeper != null && encounter.TryFloorPositionOf(keeper, out keeperX, out keeperY);
            OutworksPilot.Objective objective = OutworksPilot.Choose(encounter.Outworks, habits, x, y, keeperStands, keeperX,
                keeperY, null, out float wx, out float wy, out stop);
            if (objective == OutworksPilot.Objective.None)
            {
                if (NearestAltar(x, y, out gx, out gy))
                {
                    stop = .1f;
                    return true;
                }
                if (DropDetourRule.Nearest(_setup.Drops, x, y, DropDetour, out gx, out gy))
                {
                    stop = DropRule.PickupRadius * .5f;
                    return true;
                }
                gx = x; gy = y;
                stop = Arrived;
                return true;
            }
            float before = stop;
            gx = wx; gy = wy;
            ByPad(habits, encounter, x, y, ref gx, ref gy, ref stop);
            bool byPad = stop != before;
            bool inRing = objective == OutworksPilot.Objective.Anvil &&
                          OutworksRule.Within(x, y, wx, wy, OutworksRule.AnvilRadius);
            if (!byPad && !inRing && _detour.Choose(_setup.Drops, x, y, wx, wy, Time.deltaTime, out float dx, out float dy))
            {
                gx = dx; gy = dy;
                stop = DropRule.PickupRadius * .5f;
            }
            return true;
        }

        private static void ByPad(OutworksPilot.Habits habits, EncounterController encounter, float x, float y, ref float gx,
            ref float gy, ref float stop)
        {
            if (!habits.UsesPads || (encounter.Leap != null && encounter.Leap.IsAirborne))
                return;
            int pad = OutworksRule.PadShortcut(x, y, gx, gy);
            if (pad < 0)
                return;
            OutworksRule.PadLaunch(pad, out gx, out gy);
            stop = 0f;
        }

        // From the floor's second room on, in the walk to its next hall: where a measurement mode stops playing.
        private bool Lingering() => _setup.Encounters.RoomNumber >= 2 && _setup.Encounters.IsRoomSiteOpen &&
            !_setup.Encounters.IsInSwarmRoom;

        // The gate boss while it stands, for a cohort that steps in on it, else the elite room's Captain while its wave
        // stands, against the loadout's best weapon on it; marked while the Warden's mark is down.
        private bool StepInGoal(float x, float y, bool glowing, out float gx, out float gy)
        {
            gx = x; gy = y;
            EncounterController encounter = _setup.Encounters;
            Health target = _cohort.StepsInOnTheGateBoss ? encounter.GateBoss : null;
            bool boss = target != null;
            if (!boss)
                target = encounter.EliteLeader;
            if (target == null || !target.IsAlive || _heroHealth == null ||
                !encounter.TryFloorPositionOf(target, out float tx, out float ty))
                return false;
            WardenSlamService warden = _setup.Warden;
            bool marked = warden != null && warden.IsFighting && warden.Clock.IsMarked;
            // Not the gate boss, the target is the elite room's leader, an elite: the Daggers' share on it counts (EliteRule).
            bool stepped = FootworkRule.StepIn(x, y, tx, ty, FootworkRule.BestSingleTargetReach(_setup.Upgrades.Loadout, boss, !boss),
                marked, glowing, _heroHealth.Current, _heroHealth.Maximum, out gx, out gy);
            // The flank (docs/50 section 5): a Mirror Sentry's mirror is circled toward its rear at the same reach
            // (FootworkRule.Flank), once MirrorGuardService has turned it.
            if (stepped && !boss && target.TryGetComponent(out MirrorGuard mirror) && mirror.State.IsGuarding)
                FootworkRule.Flank(x, y, tx, ty, mirror.State.Facing, ref gx, ref gy);
            return stepped;
        }

        private bool NearestAltar(float x, float y, out float gx, out float gy)
        {
            gx = x; gy = y;
            AltarField altars = _setup.Altars != null && _setup.Altars.IsActive ? _setup.Altars.Field : null;
            if (altars == null)
                return false;
            float best = float.PositiveInfinity;
            for (int i = 0; i < altars.Count; i++)
            {
                if (altars.IsUsed(i))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, altars.XOf(i), altars.YOf(i));
                if (d >= best)
                    continue;
                best = d; gx = altars.XOf(i); gy = altars.YOf(i);
            }
            return !float.IsPositiveInfinity(best);
        }

        // The push of every living enemy within its kite reach (the cohort's AutopilotCohort.ThreatReach: KiteReach, or its
        // own reach and a margin), as a unit direction away from them; false when none is that close.
        private bool Threat(float x, float y, out float tx, out float ty)
        {
            EncounterController encounter = _setup.Encounters;
            tx = ty = 0f;
            for (int i = 0; i < encounter.EnemyCount; i++)
            {
                Health enemy = encounter.EnemyAt(i);
                if (enemy == null || !enemy.IsAlive || !encounter.TryFloorPositionOf(enemy, out float px, out float py))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, px, py);
                EnemyDefinition definition = encounter.DefinitionOf(enemy);
                float reach = definition != null && definition.Weapon != null
                    ? _cohort.ThreatReach(KiteReach, definition.Weapon.Range, ReferenceEquals(enemy, encounter.GateBoss))
                    : KiteReach;
                if (d >= reach * reach || d < 1e-6f)
                    continue;
                tx += (x - px) / d;
                ty += (y - py) / d;
            }
            float length = Mathf.Sqrt(tx * tx + ty * ty);
            if (length < 1e-4f)
                return false;
            tx /= length;
            ty /= length;
            return true;
        }

        private void Steer(float x, float y, float gx, float gy, float stop)
        {
            if (ArenaFloor.FloorDistanceSquared(x, y, gx, gy) <= stop * stop)
            {
                _movement.Release();
                return;
            }
            float tx = gx, ty = gy;
            if (!FoundryMap.Segment(x, y, gx, gy, .6f))
                _route.Target(x, y, gx, gy, ref tx, ref ty);
            var steer = new Vector2(tx - x, ty - y);
            if (steer.sqrMagnitude < 1e-6f || (_bandGuard && FootworkRule.StepsIntoBand(x, y, tx, ty)))
            {
                _movement.Release();
                return;
            }
            _movement.Hold(steer.normalized);
        }
    }
}
