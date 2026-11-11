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
            Debug.Log("[Autopilot] driving the hero as the " + _cohort.Name + " cohort (development/autopilot.txt" +
                      (mode.Length > 0 ? ": " + mode : string.Empty) + ")");
        }

        private void Update()
        {
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
            // The Warden's mark under the hero: out of it first, as a player who reads it does - away from the Warden, with
            // room, not across the glowing band if it can help it (FootworkRule.EscapeMark; from the mark itself, the
            // nearest way out, if the Warden cannot be found). The clumsy cohorts take the nearest way out, wherever the
            // Warden stands (AutopilotCohort.EscapeMark).
            WardenSlamService warden = _setup.Warden;
            if (_cohort.EscapesMarks && warden != null && warden.IsFighting && warden.Clock.IsMarked)
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
            // A node fight is drawn to the pit's rim while the pit runs, but not while a guarded band glows: then the hero
            // waits out of it. The horde alone is fought wherever the hero is going. Never in a swarm room, where the pit
            // does not run (PitEruptionService.IsRunning): the careful pilot spent about 44 s of the finale dragging the
            // swarm to a rim that never spills (the review of 2026-11-11).
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
            // Not the gate boss, the target is the Captain, an elite: the Daggers' share on it counts (EliteRule).
            return FootworkRule.StepIn(x, y, tx, ty, FootworkRule.BestSingleTargetReach(_setup.Upgrades.Loadout, boss, !boss),
                marked, glowing, _heroHealth.Current, _heroHealth.Maximum, out gx, out gy);
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
