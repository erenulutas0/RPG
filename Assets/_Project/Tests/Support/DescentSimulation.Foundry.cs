using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // The foundry's horde floor, as an opt-in beside the Descent (docs/41 steps 2 and 5): floor 1 of the scene's foundry
    // played by the scene's development autopilot, at 60 Hz, through the game's own pure rules. Nothing here is reached
    // by DescentSimulation.Run, so every number the hall arena and the Descent pin stays what it was.
    //
    // What it mirrors, frame by frame, in the scene's order: HeroMovementInput (-60) walks the hero on the steer the
    // autopilot held last frame; EncounterController (-50) steps the node wave's pack, then the horde's pack and its
    // HordeDirector, picks up the drops where the hero stands, waits for a marked hall to be reached, holds the opened gate,
    // runs the forge's transition and the advance delay; WardenSlamService (-40) marks, lands the blow and holds the gate
    // boss's weapon before anything attacks; then the default-order scripts: the hero's AttackController (every weapon in
    // slot order, Ember Memory on each strike), its AbilityController's cooldown, every enemy's AttackController, the burns,
    // AltarService, PitEruptionService, the chest, and DevelopmentAutopilot, which fires the ability and chooses next
    // frame's steer. An offer that opens is answered at the frame's end with the autopilot's own card score, and the frame
    // after it the hero stands, as the scene's autopilot releases the stick while a panel is open. The boss's arrival holds
    // the encounter for BossRevealView's turn, hold and turn back.
    //
    // The order of the default-order scripts is Unity's to choose and is not part of the game's contract; this one puts
    // the hero first and the enemies after it, as the Descent always has. A seed here is the same seed as the scene's -
    // the same offers, crits and horde draws for the same picks - but a run is not bit for bit the scene's: the autopilot's
    // vector arithmetic is UnityEngine's there and System.Math here, and the script order differs in places, so the
    // trajectories part within seconds and agreement is a matter of distributions (FoundryParity in the batch report).
    internal static partial class DescentSimulation
    {
        // Scene values the foundry floor reads that live on MonoBehaviours this assembly cannot compile under .NET. The
        // Unity-side FoundrySceneValueTests hold each to its source.
        // HeroMovementInput.FoundryPace: the foundry's walk is this much faster than the hall's.
        public const float FoundryPace = 1.4f;
        // EncounterController.GateMarchFactor and GateMarchRange: the gate boss crosses the court at 3.5 times its speed
        // until within 4 units of the hero.
        public const float GateMarchFactor = 3.5f;
        public const float GateMarchRange = 4f;
        // BossRevealView.TurnSeconds and HoldSeconds: the camera turns to the gate boss, holds, and turns back.
        public const float RevealTurnSeconds = 0.7f;
        public const float RevealHoldSeconds = 1.2f;
        // DevelopmentAutopilot's private policy constants.
        public const float PilotLureDepth = 1.3f;
        public const float PilotShelterDepth = PitEruptionRule.Reach + 1.4f;
        public const float PilotArrived = .35f;
        public const float PilotDropDetour = 4f;
        public const float PilotDetourSlack = 2f;
        public const float PilotDetourBudget = 12f;
        public const float PilotKiteReach = 1.6f;
        // 35 degrees as UnityEngine.Mathf.Deg2Rad (PI * 2 / 360 in float) turns it.
        public const float PilotKiteTurn = 35f * (3.14159274f * 2f / 360f);
        public const float PilotKiteStride = 2f;
        public const int PilotAbilityCrowd = 3;
        // PrototypeEconomy.asset's category-mode offers: three cards (EconomyConfig.CategoryChoiceCount).
        public const int FoundryChoiceCount = 3;
        // HordePacingMeasurement: the Editor measurement's frame and its floor limit.
        public const float FoundryFloorLimitSeconds = 900f;
        // The lead's engaged uptime (HordePacingMeasurement; docs/41, 2026-11-11): a strike of any weapon the hero holds
        // landed, or a hit taken, this recently - docs/40 section 8.4's attack and target-finding uptime, the "attack
        // uptime" of section 9.6. Before 2026-11-11 it read an enemy within the first weapon's reach and EngagedMargin
        // more, or a hit taken this recently; FoundryEngagement keeps that reading for the research.
        public const float EngagedSeconds = 1.5f;
        public const float EngagedMargin = 1f;
        // FoundryResult.DeathPhase of a death while the cleared room before the gate holds for the floor's clock.
        public const string GateWaitPhase = "gate wait";
        // The 30-second buckets of docs/40's time_bucket_summary.
        public const float FoundryBucketSeconds = 30f;

        // A hero as the foundry plays it: its definition's health, starting weapon (on its category base, as every
        // foundry run builds by category, docs/42) and ability, and Ember Memory for the Cinder Arcanist.
        internal sealed class FoundryHero
        {
            public string Id;
            public string Name;
            public float MaximumHealth;
            public HeroWeapon Weapon;
            public HeroAbility Ability;
            public bool EmberMemory;
        }

        // Data/Heroes/Hero_Vanguard.asset: 100 health, the Sword, the Forge Burst.
        public static FoundryHero Vanguard() => new FoundryHero
        {
            Id = "hero_vanguard", Name = "Vanguard", MaximumHealth = 100f, Weapon = Sword(), Ability = ForgeBurst()
        };

        // Data/Heroes/Hero_CinderArcanist.asset: Ilya Kest, 90 health, the Staff, Crucible Vent and Ember Memory.
        public static FoundryHero CinderArcanist() => new FoundryHero
        {
            Id = "hero_cinder_arcanist", Name = "Ilya", MaximumHealth = 90f, Weapon = Staff(), Ability = CrucibleVent(),
            EmberMemory = true
        };

        // Data/Abilities/Ability_CrucibleVent.asset.
        public static HeroAbility CrucibleVent() =>
            new HeroAbility { Damage = 8f, Radius = 3f, Cooldown = 14f, Kind = AbilityKind.Vent };

        // A foundry floor: its rooms (a forge is null), their kinds, the horde's roles in table order and the climb's floor
        // number, which ClimbScaling reads for the floor's tier.
        internal sealed class FoundryFloor
        {
            public string Id;
            public Enemy[][][] Rooms;
            public RoomKind[] Kinds;
            public Enemy[] HordeRoles;
            public int FloorNumber = 1;
            // Its modifier (FloorDefinition.Modifier) as FloorScaling reads it: enemy health and damage, and gold.
            public float ModifierHealthPercent;
            public float ModifierDamagePercent;
            public float ModifierGoldPercent;
            // Its foundry knobs (FloorDefinition.FoundryHallSurges, FoundryExtraElites, FoundryGateNotBeforeSeconds,
            // FoundryGateHealthShare - 0 for WardenRule.GateHealthShare, WardenRule.GateHealthShareOn). A floor after the
            // first plays these; the first plays the run's levers instead, whose defaults are its own.
            public int HallSurges;
            public int ExtraElites;
            public float GateNotBeforeSeconds;
            public float GateHealthShare;
            // The floor its checkpoint descends to (FloorDefinition.NextFloor); null where the climb goes on into a floor
            // this run does not play - floor 2's next is the finale's swarm room - or ends.
            public FoundryFloor Next;

            // This floor with the rooms a run's levers give it.
            public FoundryFloor Reshaped(Enemy[][][] rooms, RoomKind[] kinds)
            {
                var shaped = (FoundryFloor)MemberwiseClone();
                shaped.Rooms = rooms;
                shaped.Kinds = kinds;
                return shaped;
            }
        }

        // Data/Floors/Floor_EmberHalls.asset as the foundry plays it: the Descent's six rooms (EmberHalls, which the floor
        // tests already hold to the asset), climb-scaled from floor 1, its horde's three roles, its V0.1 knobs (as the
        // shipped levers' defaults too) and floor 2 below it.
        public static FoundryFloor FoundryFloorOne() => new FoundryFloor
        {
            Id = "floor_ember_halls",
            Rooms = EmberHalls.Rooms,
            Kinds = new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss },
            HordeRoles = new[] { Mite, Grunt, Runner },
            HallSurges = 2,
            ExtraElites = 1,
            GateNotBeforeSeconds = 210f,
            Next = FoundryFloorTwo()
        };

        // Data/Floors/Floor_QuicksilverVaults.asset, the climb's floor 2, as the foundry plays it: the Descent's floor 2
        // (QuicksilverVaults) - three halls, the Deep Forge, the Sentry Hall and the Warden's Vault, whose Forge Warden walks
        // out of the gate with two Cinder Mites - climb-scaled at floor 2, floor 1's three horde roles, Cursed Gold (+20%
        // enemy damage, +50% gold) and none of V0.1's surges, extra elite or gate time: the gate open as its room clears.
        // Its Warden's share is its own, 8.5 / 1.4 (the asset's 6.071429): the climb's x1.4 cancelled on its gate boss.
        public static FoundryFloor FoundryFloorTwo() => new FoundryFloor
        {
            Id = "floor_quicksilver_vaults",
            Rooms = QuicksilverVaults.Rooms,
            Kinds = new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss },
            HordeRoles = new[] { Mite, Grunt, Runner },
            FloorNumber = 2,
            GateHealthShare = 6.071429f,
            ModifierHealthPercent = 0f,
            ModifierDamagePercent = QuicksilverVaults.ModifierDamagePercent,
            ModifierGoldPercent = QuicksilverVaults.ModifierGoldPercent
        };

        // The floor's knobs, as parameters of a run rather than edits of the game: each default is the shipped value, read
        // from the game's own constant where there is one, so a run with no levers is the scene's floor.
        internal sealed class FoundryLevers
        {
            public string Name = "shipped";
            // Who plays the floor (AutopilotCohort, as development/autopilot.txt names one in the scene): the careful pilot,
            // or one of docs/41 step 5's and docs/40 section 9.4's cohorts - the same floor in another hand.
            public AutopilotCohort Cohort = AutopilotCohort.Careful;
            // FloorDefinition.FoundryHallSurges: surge waves each combat hall gains after its authored ones (HallSurgeRule).
            public int HallSurges = 2;
            // More copies of the floor's elite room before the gate; the district round gives the first the north court.
            public int ExtraElites = 1;
            // More combat halls after the floor's last, copies of its second and third halls in turn.
            public int ExtraHalls;
            // The gate boss's health against its authored on floor 1 (WardenRule.GateHealthShare, Floor_EmberHalls setting
            // none of its own). A floor after the first plays its own (FoundryFloor.GateHealthShare), or
            // LaterWardenHealthShare below.
            public float WardenHealthShare = WardenRule.GateHealthShare;
            // How long after a blow the Warden still holds its weapon (WardenRule.RecoverySeconds; it always holds it
            // through the telegraph); below zero it never holds it, as the floor was before 2026-11-03.
            public float WardenRecoverySeconds = WardenRule.RecoverySeconds;
            // docs/41 step 4's other candidates, off at the shipped floor (2026-11-03). Health that grows with the floor's
            // clock as the Warden walks out, as a horde enemy's does before the overrun (HordeClock.HealthGrowth), so that a
            // gate opening at WardenClockFrom seconds gives WardenHealthShare; 0 is off.
            public float WardenClockFrom;
            // A vulnerability window as damage: outside the WardenGuardOpenSeconds after each blow the Warden takes only
            // this share of what lands on it; 1 is off.
            public float WardenGuardShare = 1f;
            public float WardenGuardOpenSeconds;
            // A horde enemy's share of its authored experience (HordeRule.ExperienceShare) and of its health
            // (HordeRule.HealthShare).
            public float HordeExperienceShare = HordeRule.ExperienceShare;
            public float HordeHealthShare = HordeRule.HealthShare;
            // The crowd each director state aims for (HordeRule.TargetByState), null for the shipped one, and the fill rate
            // at the floor's first second (HordeRule.MaxSpawnsPerSecond, which the clock doubles by its end).
            public int[] HordeTargets;
            public float HordeFillRate = HordeRule.MaxSpawnsPerSecond;
            // Node waves' health against their authored, the boss's excepted.
            public float NodeHealthScale = 1f;
            // Hall objective timers: a combat hall's waves repeat in order until the hall has run this long, and the wave
            // standing then is finished; 0 is off. The elite and the boss are never held.
            public float HallHoldSeconds;
            // The gate does not open before the floor's clock reads this; 0 is off.
            public float GateNotBeforeSeconds = 210f;
            // docs/41's open decision 2 (2026-11-04): what else may open the gate, and what may fill its wait; each off at
            // the shipped floor. The gate opens once the floor's kills (horde and node waves) reach GateKillQuota, or once
            // the hero's level reaches GateOpenLevel - whichever comes first beside the clock above. With the clock off,
            // the gate holds for the quota or the level alone, and opens at the overrun (HordeClock.TargetSeconds) at the
            // latest. 0 is off.
            public int GateKillQuota;
            public int GateOpenLevel;
            // A siege while the gate holds: each time the standing wave is down, the floor's n-th combat hall (1-based)
            // sends its waves again in turn, its surges included; 0 is off, and the wait is the horde's alone.
            public int GateSiegeHall;
            // How much the horde's standing crowd grows by the overrun (HordeClock.TargetGrowth, 0.5): the crowd aims for
            // (1 + this * clock / 300) times its state's target.
            public float HordeTargetGrowth = HordeClock.TargetGrowth;
            public float LimitSeconds = FoundryFloorLimitSeconds;
            // How many floors of the climb a run plays: 1, floor 1 alone, as every report did before 2026-11-04; 2 goes on
            // after a floor-1 clear, as the autopilot answers the checkpoint with Descend, into floor 2 (FoundryFloor.Next),
            // whose result is floor 1's NextFloor. The knobs above are floor 1's: a floor after it plays its own asset's -
            // the gate boss's share among them - under the Warden's other levers and the horde's (LaterFloorLevers).
            public int Floors = 1;
            // Research only (docs/41's open question of floor 2's Warden; FoundryBatch.FloorTwoWardenLevers,
            // FOUNDRY_LEVERS=8): the gate boss on the floors after the first, instead of the floor's own share
            // (FoundryFloor.GateHealthShare) and the run's WardenRecoverySeconds, and the forge's Mend there healing this
            // share of the hero's maximum instead of Forge_Mend's 0.4. Each off by default (a share or a Mend at or below
            // 0, a recovery NaN): every floor after the first plays its asset's share, the run's recovery and the asset's
            // Mend.
            public float LaterWardenHealthShare = -1f;
            public float LaterWardenRecoverySeconds = float.NaN;
            public float LaterMendAmount = -1f;
            // Research only (docs/cloud/2026-11-04-sword-role.md, FoundryBatch.SwordLevers): a foundry-only Sword pattern,
            // read by this simulation alone and never by the scene, the hall arena or the Descent, whose Sword stays
            // Weapon_Sword.asset (docs/26, docs/30). Every default is the shipped Sword. The Sword's reach (0 keeps 2.1).
            public float SwordReach;
            // A sweep: every enemy within SwordSweepRadius of the hero and within SwordSweepArcDegrees (the whole arc,
            // centred on the target) takes SwordSweepShare of the strike, the target its full damage; 0 keeps the cleave.
            public float SwordSweepRadius;
            public float SwordSweepArcDegrees = 360f;
            public float SwordSweepShare = .6f;
            // The sweep only once the Sword has evolved (Resonant Edge); before it the shipped cleave.
            public bool SwordSweepAtEvolution;
            // The Sword's strike interval against the shipped 0.8 s (0.75 strikes a third more often).
            public float SwordIntervalScale = 1f;
            // Resonant Edge's pulse share against the shipped tiers.
            public float SwordEvolutionPulseScale = 1f;
            // A cleave on kill: each enemy a Sword strike fells bursts for this share onto the enemies near it
            // (KillBurstRule's circle, the Staff evolution's mechanic on the Sword from the start); 0 is off.
            public float SwordKillCleaveShare;
            // Knockback, modelled as a stagger: an enemy the Sword lands on (not the Warden) cannot strike for this many
            // units of its own walk (units / its speed seconds); 0 is off. The simulation's packs cannot be displaced.
            public float SwordKnockbackUnits;
            // What the Sword's landings deal, its procs included, by what they land on: the horde, the node waves
            // (halls and elites) and the Warden. The Warden's is on top of the shipped WardenRule.SwordShare (1.5 since
            // 2026-11-10): FoundryBatch.SwordLevers names absolute shares and divides them by it.
            public float SwordHordeDamage = 1f;
            public float SwordNodeDamage = 1f;
            public float SwordWardenDamage = 1f;

            // Research only, for the question what the Daggers would need: their reach (0 keeps 1.8) and their damage on
            // the Warden.
            public float DaggersReach;
            public float DaggersWardenDamage = 1f;
            // Research only, the Daggers' role (docs/41, status 2026-11-10 night; FoundryBatch.DaggersLevers): what the
            // Daggers' landings on the elites (an elite room's node wave) deal, and what their critical landings there deal,
            // on top of what they deal now. 1 is off.
            public float DaggersEliteDamage = 1f;
            public float DaggersEliteCritDamage = 1f;
            // The two above on an elite room's leader alone, its Grunt Captain, rather than its whole pack.
            public bool DaggersEliteLeaderOnly;

            public bool ReshapesSword => SwordReach > 0f || SwordSweepRadius > 0f || SwordIntervalScale != 1f ||
                                         SwordEvolutionPulseScale != 1f || SwordKillCleaveShare > 0f;

            // Research 2026-11-04 (docs/cloud/2026-11-04-difficulty.md), simulation only and off at the shipped floor.
            // A human's reaction, aim and attention on the cohort's habits (PilotRealism); null is the frame-perfect pilot.
            public PilotRealism Realism;
            // Structural candidates that separate a player who reads the floor from one who does not, each a mark on the
            // floor that lands a telegraph after it goes down, read by a cohort that EscapesMarks as it reads the
            // Warden's. The Warden's echo: after each blow a second mark where the hero stands then, landing this many
            // seconds later, with the slam's radius and WardenEchoShare of the hero's maximum health; 0 is off.
            public float WardenEchoTelegraph;
            public float WardenEchoShare = WardenRule.HeroShare;
            // Slag lobbed from the foundry's furnaces at the hero in the halls and the elite rooms (not at the Warden):
            // every SlagEvery seconds while a wave or the horde stands outside the Warden's fight, a mark of SlagRadius
            // where the hero stands, SlagShare of its maximum health a SlagTelegraph later - range does not help,
            // standing still does not either. 0 is off.
            public float SlagEvery;
            public float SlagTelegraph = 1f;
            public float SlagRadius = 1.6f;
            public float SlagShare = .08f;
            // The elite's stomp: an elite room's first enemy (the Grunt Captain), whenever the hero stands within
            // EliteStompReach of it and EliteStompEvery seconds have passed since its last, marks EliteStompRadius round
            // itself and deals EliteStompShare a telegraph later - a hero who trades at its reach must step out, one who
            // kites wide is never marked. Its first a second after it walks in. 0 is off.
            public float EliteStompEvery;
            public float EliteStompTelegraph = .9f;
            public float EliteStompRadius = 2.6f;
            public float EliteStompReach = 2.4f;
            public float EliteStompShare = .15f;
            // Surges that cut off the kite: a combat hall's surge waves enter AmbushDistance ahead of where the hero is
            // walking (behind it when it stands), its pack drawn in round that point, instead of the arena's corners.
            public bool AmbushSurges;
            public float AmbushDistance = 4f;
            // The difficulty research's tables only: count FoundryResult.TakenBy, the health the hero lost by place and blow.
            // Off, as every other report runs, TakenBy stays null and a hit on the hero builds no key; accounting only, no
            // number of the floor moves either way.
            public bool CountsHarmByPlace;
            // The careful pilot's cards (research 2026-11-10, CardScoreResearch): log every level-up offer - its cards, their
            // tiers, the one taken, and the hero's health, armour, level and holdings then - in FoundryResult.PickLog. Off,
            // as every other report runs, PickLog stays null; accounting only. And Picker, a picker in place of a cohort's
            // own scoring (AutopilotCohort.Pick) for that research's candidates, for a cohort that ScoresCards; null, the
            // default, plays the cohort's own.
            public bool LogsPicks;
            public Func<FoundryPick, UpgradeService, int> Picker;
            // Engaged uptime (research 2026-11-11, EngagedResearch): count FoundryResult.Engagement, the floor's seconds by
            // phase under each reading of engaged (FoundryEngagement.Rules). Off, as every other report runs, Engagement
            // stays null; accounting only.
            public bool CountsEngagement;

            public bool HasHazards => WardenEchoTelegraph > 0f || SlagEvery > 0f || EliteStompEvery > 0f;

            public bool TunesDirector => HordeTargets != null || HordeFillRate != HordeRule.MaxSpawnsPerSecond ||
                                         HordeTargetGrowth != HordeClock.TargetGrowth;

            public bool HoldsGate => GateNotBeforeSeconds > 0f || GateKillQuota > 0 || GateOpenLevel > 0;

            public FoundryLevers Copy(string name)
            {
                var copy = (FoundryLevers)MemberwiseClone();
                copy.Name = name;
                copy.HordeTargets = HordeTargets != null ? (int[])HordeTargets.Clone() : null;
                return copy;
            }
        }

        internal enum FoundryOutcome
        {
            Cleared,
            Died,
            Timeout
        }

        // Where the hero was hurt (research 2026-11-04; FoundryResult.LowestPhase and TakenBy): the Warden's fight, a
        // room's node fight, or the walk between (a room's hall marked, or nothing standing but the horde). None before a
        // hit.
        internal enum HarmPlace
        {
            None,
            Boss,
            Room,
            Between
        }

        // A HarmPlace by name, with its 1-based room: "boss", "room 3", "room 3 between"; null for None.
        internal static string PlaceName(HarmPlace place, int room)
        {
            if (place == HarmPlace.None)
                return null;
            if (place == HarmPlace.Boss)
                return "boss";
            string name = "room " + room.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return place == HarmPlace.Between ? name + " between" : name;
        }

        // One enemy's life, for the observed TTK: its kind, whether it was the horde's, from walking in to falling, and
        // from the first damage it took to falling (the time it stood under fire). And the killing blow, for docs/40
        // section 17.3's weapon_effectiveness: the weapon id that landed it (KilledByBurn, KilledByAbility or KilledByPit
        // otherwise), whether its evolution's proc landed it (the pulse, the splinter, the kill burst), and when.
        internal readonly struct FoundryKill
        {
            public const string KilledByBurn = "burn";
            public const string KilledByAbility = "ability";
            public const string KilledByPit = "pit";

            public readonly string Kind;
            public readonly bool Horde;
            public readonly float Lifetime;
            public readonly float UnderFire;
            public readonly string By;
            public readonly bool Proc;
            public readonly float At;

            public FoundryKill(string kind, bool horde, float lifetime, float underFire, string by = null, bool proc = false,
                float at = -1f)
            {
                Kind = kind;
                Horde = horde;
                Lifetime = lifetime;
                UnderFire = underFire;
                By = by;
                Proc = proc;
                At = at;
            }
        }

        // One weapon of the hero's loadout over a floor (docs/40 section 17.3's weapon_effectiveness): when it joined the
        // loadout and when its evolution was taken, in the floor's active seconds (-1 for never; a weapon carried down to
        // floor 2 joins it, evolved or not, on its first frame); its strikes and their own landings
        // (the target, the splash or the cleave), its evolution's proc landings; and what it took off the horde, the node
        // waves and the Warden, the procs' part of it, and what it dealt after it evolved. Accounting only: the run plays
        // the same with or without it. Its kills are the FoundryKills whose By is its Id.
        internal sealed class FoundryWeaponWork
        {
            public string Id;
            public float HeldFrom;
            public float EvolvedAt = -1f;
            public int Strikes;
            public int Landings;
            public int ProcLandings;
            public float DealtHorde;
            public float DealtNode;
            public float DealtWarden;
            // Of DealtNode, what it took off the elites: the node waves of the floor's elite rooms (RoomKind.Elite), the
            // Grunt Captain's pack - the third job the Daggers' research reads (FoundryBatch.RoleJobsOf).
            public float DealtElite;
            // Of DealtElite, what it took off the elite rooms' leaders, their Grunt Captains (research, docs/41 2026-11-10
            // night: the elite alone rather than its pack).
            public float DealtEliteLeader;
            public float DealtByProcs;
            public float DealtAfterEvolving;

            public float Dealt => DealtHorde + DealtNode + DealtWarden;
        }

        // The floor's active seconds by phase, and under each reading of engaged the seconds of each phase it counts
        // (FoundryLevers.CountsEngagement, research 2026-11-11). A phase: the walk to a marked hall, a node wave standing,
        // between (a room's advance delay, the forge, the descent), the gate's wait, the Warden's fight (its reveal
        // included) and the walk to the gate after it. Beside them, for the frames the reading before 2026-11-11 does not
        // count, how far the nearest enemy stood.
        internal sealed class FoundryEngagement
        {
            public const int Walk = 0;
            public const int Node = 1;
            public const int Between = 2;
            public const int Wait = 3;
            public const int Warden = 4;
            public const int Exit = 5;
            public static readonly string[] Phases = { "walk", "node fight", "between", "gate wait", "Warden", "exit" };

            // NearFirst is the gate's reading before 2026-11-11, Striking the gate's since (FoundryResult.Engaged).
            public const int NearFirst = 0;
            public const int Longest = 1;
            public const int Authored = 2;
            public const int InReach = 3;
            public const int Striking = 4;
            public const int Alive = 5;
            public const int NearOnly = 6;
            public const int HitOnly = 7;
            public static readonly string[] Rules =
            {
                "before 2026-11-11: an enemy within the first weapon's reach + 1, or a hit taken in the last 1.5 s",
                "an enemy within the longest weapon's reach + 1, or a hit taken in the last 1.5 s",
                "an enemy within the first weapon's authored reach + 1 (no reach line), or a hit taken in the last 1.5 s",
                "an enemy within the longest weapon's reach, or a hit taken in the last 1.5 s",
                "since 2026-11-11 (the gate's): a strike of any weapon landed, or a hit taken, in the last 1.5 s",
                "an enemy alive (combat uptime)",
                "an enemy within the first weapon's reach + 1 alone",
                "a hit taken in the last 1.5 s alone"
            };

            // The nearest enemy on the frames the reading before 2026-11-11 leaves out: none standing, then within each
            // distance.
            public static readonly float[] Distances = { 4f, 5f, 6f, 8f, float.PositiveInfinity };

            public readonly float[] Seconds = new float[Phases.Length];
            public readonly float[,] Engaged = new float[Rules.Length, Phases.Length];
            public readonly float[] Outside = new float[Distances.Length + 1];
            // The first weapon's reach and the longest weapon's reach as the floor ended.
            public float FirstReach;
            public float LongestReach;

            public float Total
            {
                get
                {
                    float sum = 0f;
                    for (int i = 0; i < Seconds.Length; i++)
                        sum += Seconds[i];
                    return sum;
                }
            }

            public float EngagedSeconds(int rule)
            {
                float sum = 0f;
                for (int i = 0; i < Phases.Length; i++)
                    sum += Engaged[rule, i];
                return sum;
            }

            public float Share(int rule)
            {
                float total = Total;
                return total > 0f ? EngagedSeconds(rule) / total : 0f;
            }
        }

        // One level-up offer as the pilot met it, and the card it took (FoundryLevers.LogsPicks, research 2026-11-10): the
        // floor's active seconds, the run's level, the room (1-based) and whether the gate boss was out, the hero's health,
        // maximum and armour, the weapons and tomes held, the offer's cards and tiers, and the slot taken (-1 until taken).
        internal sealed class FoundryPick
        {
            public float Seconds;
            public int Level;
            public int Room;
            public bool AtWarden;
            public float Health;
            public float Maximum;
            public float Armor;
            public int WeaponsHeld;
            public int TomesHeld;
            public UpgradeOffer Offer;
            public int Taken = -1;

            public bool Low => Health < Maximum * .5f;
            public UpgradeOption Card => Taken >= 0 ? Offer.Choices[Taken] : null;
            public UpgradeRarity Rarity => Taken >= 0 ? Offer.RarityAt(Taken) : UpgradeRarity.Common;
        }

        // What one foundry floor did, in the Editor measurement's terms (HordePacingMeasurement and the telemetry's
        // room_start, hall_reached, wave_clear and boss lines) and the report's (docs/41 step 5).
        internal sealed class FoundryResult
        {
            public int Seed;
            public string Hero;
            public string Levers;
            // The climb's floor this is, and the next floor's result when the run went down to it (FoundryLevers.Floors):
            // null when it did not - one floor asked for, this one not cleared, or none below it.
            public int Floor = 1;
            public FoundryResult NextFloor;
            public FoundryOutcome Outcome;
            // Active seconds from the floor's first frame - a later floor's, the checkpoint's Descend - to the gate held, a
            // death or the limit.
            public float FloorSeconds;
            // A later floor's descent: active seconds from the checkpoint to its first room's wave (EncounterController's
            // advance delay), part of FloorSeconds and of no room. 0 on floor 1.
            public float DescentSeconds;
            // Per room, by index: active seconds from the room's start to the next room's (or the floor's end), the walk to
            // its district (hall_reached), and its node-wave seconds; -1 for a room never entered.
            public float[] RoomSeconds;
            public float[] WalkSeconds;
            public float[] WaveSeconds;
            public RoomKind[] Kinds;
            // The Warden: its fight from walking out of the gate to falling, its health, what was left of it at the end,
            // and the walk from its fall to the gate held. -1 where the floor never got there.
            public float BossSeconds = -1f;
            // The Warden's own fight, from walking out of the gate to its fall: BossSeconds where it walks out alone, as on
            // floor 1; floor 2's walks out with two Cinder Mites, and its wave ends with the last of the three.
            public float WardenSeconds = -1f;
            public float WardenHealth;
            public float BossLeft = -1f;
            public float ExitSeconds = -1f;
            public float GateWaitSeconds;
            // The room before the gate cleared (whether or not the gate then held): the floor's clock, its kills and the
            // hero's level at that moment; -1 where the floor never got there.
            public float GateReadyClock = -1f;
            public int GateReadyKills = -1;
            public int GateReadyLevel = -1;
            // What filled the gate's wait (docs/41's open decision 2), in active seconds of it: a strike landed or a hit
            // taken in the last 1.5 s (the engaged rule, EngagedSeconds), and what the autopilot's steer was doing -
            // kiting, walking to a drop, fighting a node wave where it stood or drew it, walking elsewhere, or standing
            // still with nothing to do. And over the wait: the kills, the levels gained, the health lost, the drops
            // collected, the siege's waves (FoundryLevers.GateSiegeHall).
            public float WaitEngaged;
            public float WaitKite;
            public float WaitDrop;
            public float WaitFight;
            public float WaitWalk;
            public float WaitStill;
            public int WaitKills;
            public int WaitLevels;
            public float WaitTaken;
            public int WaitDrops;
            public int WaitSiegeWaves;
            // The level as the Warden walked out, and at the end after the floor's sweep (the measurement's "level").
            public int LevelAtGate = -1;
            public int Level;
            public float HeroHealth;
            public float HeroMaximum;
            // Combat uptime, the share of the floor's active seconds with an enemy alive; engaged uptime, the share with a
            // strike landed or a hit taken in the last EngagedSeconds.
            public float Uptime;
            public float Engaged;
            public int WaveKills;
            public int HordeKills;
            public int HordeSpawned;
            public int HordeSpawnsDropped;
            public float HordeMeanAlive;
            public int HordeMaxAlive;
            public float HordeMeanTarget;
            public float ClockAtGate = -1f;
            public float Clock;
            public int Slams;
            public int SlamHits;
            public int Eruptions;
            public int EruptionsOnHero;
            public int AbilityUses;
            // What the autopilot's steer was doing, in active seconds (its cohort's habits, AutopilotCohort): kiting,
            // leaving the Warden's mark, sheltering from a spill, drawing a node fight to the rim. Accounting only.
            public float KiteSeconds;
            public float EscapeSeconds;
            public float ShelterSeconds;
            public float LureSeconds;
            // Seconds the pilot held a single elite or the gate boss at its best weapon's reach (Habit.StepsIn).
            public float StepInSeconds;
            public int AltarsTaken;
            public int ChestsOpened;
            public int DropsCollected;
            public int DropsSwept;
            // Where a death happened: its room (1-based), the phase (walk, wave, forge, boss, exit) and what dealt the blow.
            public int DeathRoom = -1;
            public string DeathPhase;
            // Whether the run died at the Warden: after it walked out (the reveal included), before the exit - a hero felled
            // by the eruption that fells the Warden too among them, which the boss's wave clearing records as BossSeconds.
            public bool DiedAtWarden =>
                Outcome == FoundryOutcome.Died && LevelAtGate >= 0 && DeathPhase == "boss";
            public string Killer;
            // The first strike the hero landed, in active seconds.
            public float FirstStrikeSeconds = -1f;
            // Damage the hero took, by what dealt it, and the damage it dealt, by what dealt it.
            public float TakenFromWave;
            public float TakenFromHorde;
            public float TakenFromWarden;
            public float TakenFromSlams;
            public float TakenFromPit;
            public float DealtByWeapons;
            public float DealtByBurn;
            public float DealtByAbility;
            public float DealtByPit;
            // The Warden's fight as docs/41 step 4 budgets its health (HP_boss = D_focus * uptime * damageable_seconds):
            // what each source took off it; the seconds of the fight after the reveal, and of those the seconds it stood
            // within the reach of the hero's longest weapon; the hero's health as it walked out; and what the hero took in
            // the fight from the horde, the pit and the Warden's escort - floor 2's two Mites - (the Warden's own blows are
            // TakenFromWarden and TakenFromSlams).
            public float WardenFromWeapons;
            public float WardenFromBurn;
            public float WardenFromAbility;
            public float WardenFromPit;
            public float WardenFought;
            public float WardenInReach;
            // The elites' fights: active seconds an elite room's node wave stood with the hero alive, its walk-in included,
            // over the floor's elite rooms - what FoundryWeaponWork.DealtElite is read a second of.
            public float EliteFought;
            public float HeroHealthAtGate = -1f;
            public float HeroMaximumAtGate = -1f;
            public float FightTakenFromHorde;
            public float FightTakenFromPit;
            public float FightTakenFromEscort;
            // Offers and what was taken, by OfferCategory; weapons and tomes held at the gate and at the end.
            public int Offers;
            public int[] PicksByCategory = new int[OfferWeights.CategoryCount];
            public int[] OfferedByCategory = new int[OfferWeights.CategoryCount];
            public List<string> Picks = new List<string>();
            public int WeaponsAtGate = -1;
            public int TomesAtGate = -1;
            public string Weapons;
            public string Tomes;
            public int Evolutions;
            public float Difficulty;
            // The run's difficulty as the Warden walked out: what its health was multiplied by.
            public float DifficultyAtGate = -1f;
            public List<FoundryKill> Kills = new List<FoundryKill>();
            // Each weapon the hero held, in loadout order: the starting weapon first.
            public List<FoundryWeaponWork> WeaponWork = new List<FoundryWeaponWork>();
            // docs/40's time_bucket_summary, per FoundryBucketSeconds of active time: damage dealt, mean enemies standing,
            // kills, and the level at the bucket's end.
            public float[] BucketDamage;
            public float[] BucketAlive;
            public int[] BucketKills;
            public int[] BucketLevel;
            public int Buckets;

            // Research 2026-11-04: the hazards' marks (FoundryLevers' echo, slag, stomp) put down, those that landed on the
            // hero and what they took; the realistic pilot's lapses (PilotRealism); the hero's lowest health as a share of
            // its maximum, with the room (the 1-based room index, as DeathRoom), the phase and the blow that took it
            // there; and the health the hero lost, by place and blow ("room 3|horde Grunt", "boss|pit"), counted only when
            // a report asks for it (FoundryLevers.CountsHarmByPlace) and null otherwise.
            public int HazardMarks;
            public int HazardHits;
            public float TakenFromHazards;
            public int Lapses;
            public float LowestHealthShare = 1f;
            public int LowestRoom = -1;
            // The place of the lowest health, named as it is read ("boss", "room 3", "room 3 between"; null before a hit),
            // so that a hit builds no string.
            public string LowestPhase => PlaceName(LowestPlace, LowestRoom);
            public HarmPlace LowestPlace;
            public string LowestHarm;
            public Dictionary<string, float> TakenBy;
            // Every level-up offer and the card taken, when a report asks for it (FoundryLevers.LogsPicks); null otherwise.
            public List<FoundryPick> PickLog;
            // The floor's seconds by phase under each reading of engaged, when a report asks for it
            // (FoundryLevers.CountsEngagement); null otherwise.
            public FoundryEngagement Engagement;

            public bool Cleared => Outcome == FoundryOutcome.Cleared;

            public float WalkTotal
            {
                get
                {
                    float sum = 0f;
                    for (int i = 0; i < WalkSeconds.Length; i++)
                        if (WalkSeconds[i] > 0f)
                            sum += WalkSeconds[i];
                    return sum;
                }
            }

            // The non-boss rooms' active seconds less their walks and the gate wait: the node fights with their advance
            // delays, and the forge's second, as the research's floor_breakdown splits the telemetry (less room_complete's
            // gate_hold_sec). The wait is the room before the gate's, which its RoomSeconds include.
            public float NodeSeconds
            {
                get
                {
                    float sum = 0f;
                    for (int i = 0; i < RoomSeconds.Length; i++)
                    {
                        if (RoomSeconds[i] < 0f || Kinds[i] == RoomKind.Boss)
                            continue;
                        sum += RoomSeconds[i] - Math.Max(0f, WalkSeconds[i]);
                    }
                    return sum - GateWaitSeconds;
                }
            }
        }

        // One foundry floor for this hero on this seed, and the floors below it the levers ask for (FoundryLevers.Floors).
        // Null levers and floor are the scene's floor 1. trace, when given, hears the run as the scene's telemetry would
        // write it - one line an event, in the telemetry's own names and fields - so a seed can be laid beside the
        // Editor's copy of its events and the first difference found.
        public static FoundryResult RunFoundry(FoundryHero hero, int seed, FoundryLevers levers = null,
            FoundryFloor floor = null, Action<string> trace = null) =>
            new FoundryRun(hero ?? Vanguard(), floor ?? FoundryFloorOne(), levers ?? new FoundryLevers(), seed, trace).Play();

        // The levers a floor after the first plays: the run's Warden and horde levers, which stand for WardenRule's and
        // HordeRule's constants that the scene reads alike on every floor, with the floor's own asset knobs and none of the
        // floor-1 experiments (extra halls, held halls, node health).
        public const string SwordId = "weapon_sword";
        public const string DaggersId = "weapon_daggers";

        // FoundryLevers' Sword pattern on a weapon of the run (the hero's own or the catalogue's card): the Sword reshaped by
        // the research levers, any other weapon - and the Sword at the shipped levers - as it is. The sweep is an area
        // pattern whose circle HeroAttacks draws round the hero rather than the target.
        internal static HeroWeapon FoundrySword(HeroWeapon weapon, FoundryLevers levers)
        {
            if (weapon != null && weapon.Id == DaggersId && levers.DaggersReach > 0f)
            {
                HeroWeapon daggers = weapon.Copy();
                daggers.Range = levers.DaggersReach;
                return daggers;
            }
            if (weapon == null || weapon.Id != SwordId || !levers.ReshapesSword)
                return weapon;
            HeroWeapon sword = weapon.Copy();
            if (levers.SwordReach > 0f)
                sword.Range = levers.SwordReach;
            if (levers.SwordSweepRadius > 0f)
                sword.Pattern = new AttackPattern(WeaponBehavior.Area, levers.SwordSweepRadius, levers.SwordSweepShare);
            if (levers.SwordIntervalScale != 1f)
            {
                sword.Interval = ScaleOf(weapon.Interval, levers.SwordIntervalScale);
                sword.CategoryInterval = ScaleOf(weapon.CategoryInterval, levers.SwordIntervalScale);
            }
            if (levers.SwordEvolutionPulseScale != 1f)
            {
                float s = levers.SwordEvolutionPulseScale;
                sword.Evolution = Tiered("evolution_sword", "Resonant Edge",
                    "Every 4th strike bursts for {0:0%} damage nearby, and a harder world", UpgradeStat.PulseDamage,
                    ModifierOperation.Flat, ScaleOf(.4f, s), ScaleOf(.65f, s), ScaleOf(1f, s), ScaleOf(1.33f, s), 1);
            }
            if (levers.SwordKillCleaveShare > 0f)
                sword.KillBurstShare = levers.SwordKillCleaveShare;
            return sword;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static float ScaleOf(float value, float scale) => value * scale;

        internal static FoundryLevers LaterFloorLevers(FoundryLevers run, FoundryFloor floor)
        {
            FoundryLevers levers = run.Copy(run.Name);
            levers.HallSurges = floor.HallSurges;
            levers.ExtraElites = floor.ExtraElites;
            levers.GateNotBeforeSeconds = floor.GateNotBeforeSeconds;
            levers.ExtraHalls = 0;
            levers.HallHoldSeconds = 0f;
            levers.NodeHealthScale = 1f;
            levers.GateKillQuota = 0;
            levers.GateOpenLevel = 0;
            levers.GateSiegeHall = 0;
            // The floor's own gate share, as EncounterController.SpawnWave reads it, unless a research lever overrides it.
            levers.WardenHealthShare = run.LaterWardenHealthShare > 0f
                ? run.LaterWardenHealthShare
                : WardenRule.GateHealthShareOn(floor.GateHealthShare);
            if (!float.IsNaN(run.LaterWardenRecoverySeconds))
                levers.WardenRecoverySeconds = run.LaterWardenRecoverySeconds;
            return levers;
        }

        // EncounterController.LaterHalls: the districts a floor adds to the climb's round - its authored combat and elite
        // rooms after the first, the foundry's extra elites not counted (FoundryMap.RoomSite).
        internal static int LaterHalls(FoundryFloor floor)
        {
            int halls = 0;
            for (int i = 0; i < floor.Kinds.Length; i++)
                if (floor.Kinds[i] == RoomKind.Combat || floor.Kinds[i] == RoomKind.Elite)
                    halls++;
            return Math.Max(0, halls - 1);
        }

        // The floor a run's levers make: extra halls after the last, extra elites before the gate.
        private static FoundryFloor Shape(FoundryFloor floor, FoundryLevers levers)
        {
            if (levers.ExtraHalls <= 0 && levers.ExtraElites <= 0)
                return floor;
            var rooms = new List<Enemy[][]>();
            var kinds = new List<RoomKind>();
            int lastHall = -1, elite = -1, boss = -1;
            for (int i = 0; i < floor.Kinds.Length; i++)
            {
                if (floor.Kinds[i] == RoomKind.Combat)
                    lastHall = i;
                if (floor.Kinds[i] == RoomKind.Elite && elite < 0)
                    elite = i;
                if (floor.Kinds[i] == RoomKind.Boss)
                    boss = i;
            }
            for (int i = 0; i < floor.Kinds.Length; i++)
            {
                if (i == boss)
                    for (int e = 0; e < levers.ExtraElites && elite >= 0; e++)
                    {
                        rooms.Add(floor.Rooms[elite]);
                        kinds.Add(RoomKind.Elite);
                    }
                rooms.Add(floor.Rooms[i]);
                kinds.Add(floor.Kinds[i]);
                if (i == lastHall)
                    for (int h = 0; h < levers.ExtraHalls; h++)
                    {
                        int source = 1 + h % Math.Max(1, lastHall);
                        rooms.Add(floor.Rooms[Math.Min(source, lastHall)]);
                        kinds.Add(RoomKind.Combat);
                    }
            }
            return floor.Reshaped(rooms.ToArray(), kinds.ToArray());
        }

        // The hero, as the enemies' weapons and the pit see it: its health, with the last thing that hurt it remembered for
        // the death report.
        private sealed class FoundryHeroBody : IDamageable
        {
            public HealthState State;
            public FoundryRun Run;

            public bool IsAlive => State.IsAlive;

            public void ApplyDamage(DamageContext context) => Run.HeroDamaged(context);
        }

        // One enemy on the foundry floor: the scene's Health and the components on its prefab - its weapon, its enrage
        // and, once it first catches fire, its BurnStatus - with where it stands read from its own pack.
        private sealed class Foe : IDamageable
        {
            public Enemy Definition;
            public HealthState Health;
            public WeaponRuntime Weapon;
            public EnrageRule Enrage;
            public BurnState Burn;
            public bool BurnFresh;
            public bool Horde;
            public bool GateBoss;
            // Walked out of the gate beside the gate boss: floor 2's two Cinder Mites.
            public bool Escort;
            // Of an elite room's node wave (RoomKind.Elite): the Grunt Captain and its pack; and its first, the Captain.
            public bool Elite;
            public bool EliteLeader;
            public PackMotion Motion;
            public int Slot;
            public int Experience;
            public int Gold;
            public bool Defeated;
            // Its body has left the floor (the horde released, the next wave formed): nothing reports for it any more.
            public bool Released;
            public int SpawnFrame;
            public float SpawnedAt;
            public float FirstHitAt = -1f;
            // FoundryLevers.SwordKnockbackUnits (research): no strike before this run second.
            public float StaggeredUntil;
            // A realistic pilot's notice of it (PilotRealism): when it came within the kite reach, and when it last left it;
            // NaN for neither. And the elite's stomp clock (FoundryLevers.EliteStompEvery).
            public float NoticedAt = float.NaN;
            public float LostAt = float.NaN;
            public float StompClock = float.NaN;
            public FoundryRun Run;

            public bool IsAlive => Health.IsAlive;
            public float X => Motion.XOf(Slot);
            public float Y => Motion.YOf(Slot);

            public void ApplyDamage(DamageContext context) => Run.FoeDamaged(this, context);
        }

        // What dealt the damage being applied now, for the report's attribution.
        private enum DamageSource
        {
            Weapon,
            Burn,
            Ability,
            Pit
        }

        // A hazard's mark on the floor (FoundryLevers' echo, slag and stomp; research 2026-11-04): where it lands, how wide,
        // when it went down and lands, what it takes and from where it came (an escape steps away from that), and the
        // realistic pilot's misplaced step for it, drawn once.
        private sealed class HazardMark
        {
            public float X;
            public float Y;
            public float Radius;
            public float Onset;
            public float LandsAt;
            public float Share;
            public string Harm;
            public float SourceX;
            public float SourceY;
            public bool Aimed;
            public float AimX;
            public float AimY;
        }

        // The horde's director: the game's HordeDirector, or the tuned copy when a lever changes what it reads as constants.
        private sealed class HordeDriver
        {
            private readonly HordeDirector _shipped;
            private readonly TunedHordeDirector _tuned;

            public HordeDriver(FoundryLevers levers, int roles, int seed)
            {
                if (levers.TunesDirector)
                    _tuned = new TunedHordeDirector(HordeRule.SceneCap, roles, seed, levers.HordeTargets,
                        levers.HordeFillRate / HordeRule.MaxSpawnsPerSecond, levers.HordeTargetGrowth);
                else
                    _shipped = new HordeDirector(HordeRule.SceneCap, roles, seed, true);
                for (HordeState state = HordeState.Ramp; state <= HordeState.Boss; state++)
                {
                    var weights = new int[roles];
                    for (int role = 0; role < roles; role++)
                        weights[role] = HordeRule.Weight(state, role);
                    if (_tuned != null)
                        _tuned.SetRoleWeights(state, weights);
                    else
                        _shipped.SetRoleWeights(state, weights);
                }
            }

            public float Seconds => _tuned?.Seconds ?? _shipped.Seconds;
            public HordeState State => _tuned?.State ?? _shipped.State;
            public int TargetNow => _tuned?.TargetNow ?? _shipped.TargetNow;
            public float HealthMultiplier => _tuned?.HealthMultiplier ?? _shipped.HealthMultiplier;
            public float DamageMultiplier => _tuned?.DamageMultiplier ?? _shipped.DamageMultiplier;
            public int Spawned => _tuned?.Spawned ?? _shipped.Spawned;

            public void SetState(HordeState state)
            {
                if (_tuned != null)
                    _tuned.SetState(state);
                else
                    _shipped.SetState(state);
            }

            public void Fell()
            {
                if (_tuned != null)
                    _tuned.Fell();
                else
                    _shipped.Fell();
            }

            public void Tick(float deltaTime, int alive, int otherAlive, List<HordeDirector.Spawn> spawns)
            {
                if (_tuned != null)
                    _tuned.Tick(deltaTime, alive, otherAlive, spawns);
                else
                    _shipped.Tick(deltaTime, alive, otherAlive, spawns);
            }
        }

        private sealed class FoundryRun : INeighbourhood
        {
            private const float Step = 1f / 60f;
            private static readonly ArenaGeometry Map = ArenaGeometry.Foundry;

            private readonly FoundryHero _heroDefinition;
            // The floor being played: as its asset authors it, as the levers shape it, and the levers it plays (the run's
            // on floor 1, LaterFloorLevers below it); its result; and where the floor began on the run's active seconds.
            private FoundryFloor _authored;
            private FoundryFloor _floor;
            private FoundryLevers _levers;
            private readonly FoundryLevers _runLevers;
            private FoundryResult _result;
            private float _floorStartedAt;

            private readonly RunState _run;
            private readonly RewardService _rewards;
            private readonly GroundDrops _drops;
            private readonly WeaponLoadout _loadout;
            private readonly UpgradeService _upgrades;
            private readonly ForgeService _forge;
            private readonly RunChoices _choices;
            private readonly HeroStats _heroStats;
            private readonly HealthState _heroHealth;
            private readonly FoundryHeroBody _hero;
            private readonly HeroMotion _heroMotion;
            private readonly HeroStamina _stamina;
            private readonly AbilityRuntime _ability;
            private readonly AltarField _altars;
            private readonly PitEruptionClock _pit = new PitEruptionClock();
            private bool _pitRunning;
            private readonly WardenSlamClock _slam = new WardenSlamClock();
            private Foe _slamBoss;
            private bool _slamEnraged;
            private float _markX;
            private float _markY;

            // The floor's flow, as EncounterController keeps it.
            private FloorProgress _progress;
            private readonly EncounterProgress _fight = new EncounterProgress(AdvanceDelay);
            private int[] _wavesPerRoom;
            private bool _nonCombat;
            private float _transitionWait;
            // The checkpoint's Descend waits the advance delay before the next floor's first room; the districts the run's
            // earlier floors opened (EncounterController._hallsOnEarlierFloors).
            private bool _descending;
            // The cleared room before the gate holding for the floor's clock (FoundryLevers.GateNotBeforeSeconds; GateHolds,
            // its siege's waves included), for the death report: a death then is the gate's wait's, not its room's.
            private bool _gateHeld;
            private int _hallsOnEarlierFloors;
            private bool _roomSiteOpen;
            private float _siteX;
            private float _siteY;
            private float _siteWait;
            private FloorStep _siteStep;
            private bool _gateExitOpen;
            private float _exitX;
            private float _exitY;
            private ExitDwell _exitDwell;
            private float _lastHeroX;
            private float _lastHeroY;
            private bool _cleared;
            private int _waveOrdinal;
            private bool _gateBossWave;
            // The node wave standing is an elite room's (FoundryResult.EliteFought).
            private bool _eliteWave;
            private int _waveIndex = -1;
            private int _roomIndex = -1;
            private float _waveStartedAt;
            // A held hall (FoundryLevers.HallHoldSeconds): when it began, and the next wave its repeat sends.
            private float _hallStartedAt;
            private int _hallRepeatWave = -1;

            private readonly List<Foe> _wave = new List<Foe>(PackLayout.MaxPackSize);
            private PackMotion _waveMotion;
            private int _waveAlive;
            private readonly EntryPlacement[] _placements = new EntryPlacement[PackLayout.MaxPackSize];
            private readonly List<Enemy> _waveEnemies = new List<Enemy>(PackLayout.MaxPackSize);

            private HordeDriver _horde;
            private PackMotion _hordeMotion;
            private readonly List<Foe> _hordeFoes = new List<Foe>(PackLayout.MaxPackSize);
            private int _hordeAlive;
            private bool _bossEnraged;
            private readonly List<HordeDirector.Spawn> _hordeSpawns = new List<HordeDirector.Spawn>(8);
            private readonly Func<float, float, bool> _pitHeld;

            // Every enemy that has caught fire, in the order its BurnStatus was added.
            private readonly List<Foe> _burning = new List<Foe>();

            // BossRevealView: the encounter holds, and the hero's movement waits, for this many more frames.
            private readonly int _revealFrames;
            private int _revealLeft;
            private bool _revealHold;
            private bool _revealStarted;

            // The autopilot's route and the steer the hero holds into the next frame.
            private readonly FoundryPursuit _route = new FoundryPursuit();
            private float _steerX;
            private float _steerY;
            // The autopilot's drops across a walk (DropDetourRule.Walk).
            private readonly DropDetourRule.Walk _detour =
                new DropDetourRule.Walk(PilotDropDetour, PilotDetourSlack, PilotDetourBudget);
            // While the pit's band glows the autopilot steps into it only to leave the Warden's mark (PilotSteer).
            private bool _bandGuard;

            // The one chest, as ChestSpawner keeps it: a new one for each room of each floor, counted across the climb.
            private int _chestIndex = -1;
            private int _chestRoom = -1;
            private int _chestFloor = -1;
            private bool _chestPlaced;
            private bool _chestOpen;
            private float _chestX;
            private float _chestY;
            private ChestReward _chestReward;

            // Research 2026-11-04: the realistic pilot (FoundryLevers.Realism, null for the frame-perfect one), its dice and
            // its attention - the stick held until _lapseUntil, the next lapse - and its misplaced steps: for the Warden's
            // mark (by the slams counted when it was drawn), for a shelter (by the eruptions counted) and for a kite stride
            // (until _kiteAimUntil). The hazards' marks on the floor, the slag's clock, and the next wave's ambush.
            private readonly PilotRealism _realism;
            private readonly PilotDice _dice;
            private float _lapseUntil = float.NegativeInfinity;
            private float _nextLapseAt = float.PositiveInfinity;
            private int _markAimFor = -1;
            private float _markAimX;
            private float _markAimY;
            private int _shelterAimFor = -1;
            private float _shelterAimX;
            private float _shelterAimY;
            private float _kiteAimUntil = float.NegativeInfinity;
            private float _kiteAimX;
            private float _kiteAimY;
            private readonly List<HazardMark> _marks = new List<HazardMark>();
            private float _slagClock;
            private bool _ambushNext;
            private string _hazardHarm;

            private int _frame;
            private float _seconds;
            private float _lastHeroHit = float.NegativeInfinity;
            // The hero's last landed strike, of any weapon it holds: the engaged rule's (EngagedSeconds).
            private float _lastStrike = float.NegativeInfinity;
            private DamageSource _source;
            // The weapon striking now, for weapon_effectiveness: its work, its runtime and the procs it had fired before
            // this strike (a proc's counter rises before its damage lands, so any landing after it is the proc's).
            private FoundryWeaponWork _striking;
            private WeaponRuntime _strikingWeapon;
            private int _procsBeforeStrike;
            private Foe _lastAttacker;
            private string _lastHarm;
            private double _hordeAliveSum;
            private double _hordeTargetSum;
            private int _hordeFrames;
            private float _fighting;
            private float _engaged;
            // The gate's wait (FoundryLevers.HoldsGate, GateSiegeHall): whether this frame was part of it, whether it has
            // begun and a siege wave stands, and the run's tallies as it began.
            private bool _waitFrame;
            private bool _waitBegan;
            private bool _waitEnded;
            private bool _sieging;
            private int _siegeWave;
            private int _waitKillsFrom;
            private int _waitLevelFrom;
            private float _waitTakenFrom;
            private int _waitDropsFrom;
            // What the autopilot's steer did this frame (PilotGoal), for the wait's accounting.
            private PilotDoing _doing;

            private enum PilotDoing
            {
                Still,
                Kite,
                Drop,
                Fight,
                Walk
            }

            private readonly List<IDamageable> _nearby = new List<IDamageable>();
            private readonly List<IDamageable> _pulse = new List<IDamageable>();
            private readonly List<IDamageable> _crit = new List<IDamageable>();
            private readonly List<IDamageable> _burst = new List<IDamageable>();
            private readonly List<IDamageable> _spread = new List<IDamageable>();
            private readonly List<IDamageable> _abilityTargets = new List<IDamageable>();
            private readonly List<IDamageable> _ventSplash = new List<IDamageable>();
            private readonly List<float> _distances = new List<float>();
            private readonly Action<string> _trace;
            private int _roomKills;

            private void Emit(string line)
            {
                if (_trace != null)
                    _trace(_seconds.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) + " " + line);
            }

            private static string N(float value) => value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);

            // floor is the run's first floor as its asset authors it; levers shape it (Shape) and set the run.
            public FoundryRun(FoundryHero hero, FoundryFloor floor, FoundryLevers levers, int seed, Action<string> trace)
            {
                _trace = trace;
                _heroDefinition = hero;
                _authored = floor;
                _runLevers = levers;
                _levers = levers;
                _floor = Shape(floor, levers);
                _result = NewResult(seed);

                // CombatSetup.Awake on the foundry, in its order.
                _run = new RunState(ExperiencePerLevel, 0.5f, ExperienceGrowth, seed);
                _rewards = new RewardService(_run);
                _heroHealth = new HealthState(hero.MaximumHealth);
                _hero = new FoundryHeroBody { State = _heroHealth, Run = this };
                _stamina = HeroStamina.ForCurrentRules();
                _heroMotion = new HeroMotion(HeroSpeed * FoundryPace);
                _heroMotion.Place(0f, 0f, Map);
                _heroStats = new HeroStats(hero.MaximumHealth, _heroMotion.Speed, _stamina.Bar);
                _heroStats.Changed += () => HeroStatsBinding.Apply(_heroStats, _heroHealth, _heroMotion, _stamina);
                HeroStatsBinding.Apply(_heroStats, _heroHealth, _heroMotion, _stamina);
                // The Sword as the research levers shape it (FoundrySword; the shipped Sword at the shipped levers).
                HeroWeapon first = FoundrySword(hero.Weapon, levers);
                WeaponRuntime weapon = first.CreateCategoryRuntime();
                weapon.Seed = seed;
                _loadout = new WeaponLoadout(seed);
                _loadout.TryAdd(first.Id, weapon);
                HeroWeapon[] catalogue = WeaponCatalogue();
                var cards = new WeaponCard[catalogue.Length];
                for (int i = 0; i < cards.Length; i++)
                    cards[i] = FoundrySword(catalogue[i], levers).CreateCard();
                _upgrades = new UpgradeService(_run, weapon, Pool(), FoundryChoiceCount, _heroStats, _stamina.Enabled,
                    _loadout, cards, Tomes());
                _upgrades.OfferChanged += OnOfferChanged;
                _forge = new ForgeService(_run, _heroHealth);
                _choices = new RunChoices(_upgrades, _forge);
                _drops = new GroundDrops(_run);
                _ability = hero.Ability.CreateRuntime();
                var altarX = new float[FoundryMap.AltarCount];
                var altarY = new float[FoundryMap.AltarCount];
                for (int i = 0; i < altarX.Length; i++)
                    FoundryMap.AltarSite(i, out altarX[i], out altarY[i]);
                _altars = new AltarField(altarX, altarY);
                // PitEruptionService.Initialize: the rim's slag holds the packs while the pit warns, from the state its
                // Update left last frame, since EncounterController steps the packs before the pit's Update runs.
                _pitHeld = (x, y) => _pitRunning && _pit.IsWarning && PitEruptionRule.InBand(x, y);
                _revealFrames = RevealFrames();
                _realism = levers.Realism;
                if (_realism != null)
                {
                    _dice = new PilotDice(seed);
                    if (_realism.Lapses)
                        _nextLapseAt = _dice.Wait(_realism.LapsesPerMinute);
                }

                EnterFloor();
            }

            // What one floor did, sized to the floor as the levers shape it.
            private FoundryResult NewResult(int seed)
            {
                var result = new FoundryResult
                {
                    Seed = seed, Hero = _heroDefinition.Id, Levers = _levers.Name, Floor = _floor.FloorNumber,
                    Kinds = _floor.Kinds, RoomSeconds = Filled(_floor.Kinds.Length, -1f),
                    WalkSeconds = Filled(_floor.Kinds.Length, -1f), WaveSeconds = Filled(_floor.Kinds.Length, -1f),
                    TakenBy = _levers.CountsHarmByPlace ? new Dictionary<string, float>() : null,
                    PickLog = _levers.LogsPicks ? new List<FoundryPick>() : null,
                    Engagement = _levers.CountsEngagement ? new FoundryEngagement() : null
                };
                int buckets = (int)Math.Ceiling(_levers.LimitSeconds / FoundryBucketSeconds) + 1;
                result.BucketDamage = new float[buckets];
                result.BucketAlive = new float[buckets];
                result.BucketKills = new int[buckets];
                result.BucketLevel = new int[buckets];
                return result;
            }

            // EncounterController.EnterFloor: the floor's waves room by room (a combat hall's surges after its authored
            // waves), its progress through them, and its packs entering from the first corner again.
            private void EnterFloor()
            {
                _wavesPerRoom = new int[_floor.Rooms.Length];
                for (int i = 0; i < _wavesPerRoom.Length; i++)
                    _wavesPerRoom[i] = _floor.Rooms[i] == null ? 0 : _floor.Rooms[i].Length + SurgesIn(i);
                _progress = new FloorProgress(_wavesPerRoom);
                _waveOrdinal = 0;
            }

            // The checkpoint's Descend (CombatSetup.OnCheckpointChosen): the run's gold secured, then
            // EncounterController.DescendToNextFloor - the districts go on from this floor's authored halls, the next
            // floor's rooms, and the advance delay before its first room, through which no horde stands - and AltarService's
            // altars answer again on the new floor. Nothing else changes: the hero stands where it held the gate, with its
            // health, level, weapons, tomes and difficulty; the pit's clock stays where it stopped; the chests count on
            // across the climb; and the horde, gone with the gate's opening, comes back with the floor's first room on a
            // clock of its own.
            private void Descend()
            {
                _run.SecureGold();
                _hallsOnEarlierFloors += LaterHalls(_authored);
                _authored = _authored.Next;
                _levers = LaterFloorLevers(_runLevers, _authored);
                _floor = Shape(_authored, _levers);
                EnterFloor();
                _descending = true;
                _transitionWait = 0f;
                _altars.Reset();
                // The checkpoint was a choice: the autopilot let go of the stick while it stood open.
                _steerX = 0f;
                _steerY = 0f;

                // The new floor's own accounting.
                _cleared = false;
                _roomIndex = -1;
                _waveIndex = -1;
                _roomKills = 0;
                _bossFellAt = -1f;
                _floorStartedAt = _seconds;
                _fighting = 0f;
                _engaged = 0f;
                _waitBegan = false;
                _waitEnded = false;
                _sieging = false;
                _siegeWave = 0;
                _hordeAliveSum = 0;
                _hordeTargetSum = 0;
                _hordeFrames = 0;
                _result = NewResult(_run.Seed);
                if (_trace != null)
                    Emit($"floor_descend floor={_floor.FloorNumber} level={_run.Level} hp={N(_heroHealth.Current)}");
            }

            private float HeroX => _heroMotion.X;
            private float HeroY => _heroMotion.Y;

            // Active seconds since the floor began: since the run's start on floor 1, since the checkpoint below it.
            private float FloorTime => _seconds - _floorStartedAt;

            public FoundryResult Play()
            {
                // EncounterController.Initialize: the floor's first wave forms up round the hero before the first frame.
                AdvanceFloor();
                FoundryResult first = PlayFloor(), last = first;
                // CombatSetup.OnFloorCleared opens the checkpoint on a floor with a next one, once the sweep's level-ups are
                // answered (RunChoices offers them first), and the autopilot answers it with Descend.
                for (int floors = 1; floors < _runLevers.Floors && last.Cleared && _authored.Next != null; floors++)
                {
                    Descend();
                    last.NextFloor = PlayFloor();
                    last = last.NextFloor;
                }
                return first;
            }

            // The floor's frames until its gate is held, the hero falls or the floor's limit.
            private FoundryResult PlayFloor()
            {
                while (!_cleared && _heroHealth.IsAlive && FloorTime < _levers.LimitSeconds)
                    Frame();
                return Finish();
            }

            private void Frame()
            {
                _frame++;
                _seconds += Step;
                _revealStarted = false;

                // HeroMovementInput: the steer held from last frame, unless the boss's arrival has the stick.
                if (!_revealHold && _heroHealth.IsAlive)
                    _heroMotion.Move(_steerX, _steerY, Step * _stamina.SpeedFactor, Map);

                if (!_revealHold && _heroHealth.IsAlive)
                    Encounter();
                StepWarden();
                if (_levers.HasHazards)
                    StepHazards();

                HeroAttacks();
                if (_heroHealth.IsAlive)
                    _ability.Tick(Step);
                EnemyAttacks();
                TickBurns();
                StepAltars();
                StepPit();
                StepChest();
                Pilot();
                AnswerChoices();

                // The reveal's coroutine, after the frame's Updates.
                if (_revealHold && !_revealStarted && --_revealLeft <= 0)
                {
                    _revealHold = false;
                    _steerX = 0f;
                    _steerY = 0f;
                }
                for (int i = 0; i < _burning.Count; i++)
                    _burning[i].BurnFresh = false;
                Sample();
            }

            // ---------------------------------------------------------------------------------------------------------
            // EncounterController
            // ---------------------------------------------------------------------------------------------------------

            private void Encounter()
            {
                MovePack();
                TickHorde();
                _result.DropsCollected += _drops.CollectNear(HeroX, HeroY);

                if (_roomSiteOpen)
                {
                    _siteWait += Step;
                    if (!_choices.IsOpen &&
                        ExitRule.IsWithinReach(HeroX, HeroY, _siteX, _siteY, FoundryMap.RoomSiteRadius))
                    {
                        _roomSiteOpen = false;
                        _result.WalkSeconds[_siteStep.RoomIndex] = _siteWait;
                        if (_trace != null)
                            Emit($"hall_reached walk={N(_siteWait)} level={_run.Level} hp={N(_heroHealth.Current)}");
                        StartWave(_siteStep.RoomIndex, _siteStep.WaveIndex);
                    }
                    return;
                }

                if (_gateExitOpen)
                {
                    if (!_choices.IsOpen)
                        TickGateExit();
                    return;
                }

                bool canAdvance = !_choices.IsOpen;
                if (_descending || _nonCombat)
                {
                    if (!canAdvance)
                        return;
                    _transitionWait += Step;
                    if (_transitionWait < AdvanceDelay)
                        return;
                    if (_descending)
                    {
                        _descending = false;
                        _result.DescentSeconds = FloorTime;
                    }
                    AdvanceFloor();
                    return;
                }

                if (_progress.IsComplete)
                    return;
                bool ready = _fight.Tick(Step, canAdvance);
                // A siege wave of the gate's wait standing, or falling: the wait goes on.
                if (_sieging)
                {
                    _waitFrame = true;
                    _result.GateWaitSeconds += Step;
                }
                if (!ready)
                    return;
                if (_result.GateReadyClock < 0f && NextIsGate())
                {
                    _result.GateReadyClock = _horde != null ? _horde.Seconds : FloorTime;
                    _result.GateReadyKills = FloorKills;
                    _result.GateReadyLevel = _run.Level;
                }
                // FoundryLevers.GateNotBeforeSeconds, GateKillQuota, GateOpenLevel: the floor waits at the room before the
                // gate until its clock, its kills or the hero's level say so; GateSiegeHall sends waves meanwhile.
                if (GateHolds())
                {
                    if (!_waitBegan)
                        BeginWait();
                    if (!_sieging)
                    {
                        _waitFrame = true;
                        _result.GateWaitSeconds += Step;
                    }
                    _gateHeld = true;
                    if (_levers.GateSiegeHall > 0)
                        SiegeWave();
                    return;
                }
                _sieging = false;
                EndWait();
                AdvanceFloor();
            }

            private int FloorKills => _result.HordeKills + _result.WaveKills;

            private bool GateHolds()
            {
                if (!_levers.HoldsGate || _horde == null || !NextIsGate())
                    return false;
                float clock = _horde.Seconds;
                if (_levers.GateNotBeforeSeconds > 0f)
                {
                    if (clock >= _levers.GateNotBeforeSeconds)
                        return false;
                }
                else if (HordeClock.IsOvertime(clock))
                    return false;
                if (_levers.GateKillQuota > 0 && FloorKills >= _levers.GateKillQuota)
                    return false;
                if (_levers.GateOpenLevel > 0 && _run.Level >= _levers.GateOpenLevel)
                    return false;
                return true;
            }

            private void BeginWait()
            {
                _waitBegan = true;
                _waitKillsFrom = FloorKills;
                _waitLevelFrom = _run.Level;
                _waitTakenFrom = Taken;
                _waitDropsFrom = _result.DropsCollected;
            }

            private void EndWait()
            {
                if (!_waitBegan || _waitEnded)
                    return;
                _waitEnded = true;
                _result.WaitKills = FloorKills - _waitKillsFrom;
                _result.WaitLevels = _run.Level - _waitLevelFrom;
                _result.WaitTaken = Taken - _waitTakenFrom;
                _result.WaitDrops = _result.DropsCollected - _waitDropsFrom;
            }

            private float Taken => _result.TakenFromWave + _result.TakenFromHorde + _result.TakenFromWarden +
                                   _result.TakenFromSlams + _result.TakenFromPit + _result.TakenFromHazards;

            // FoundryLevers.GateSiegeHall: the hall's waves in turn, a new one as the last falls, as a held hall repeats its
            // own (RepeatsHall) - the floor's progress stays at the room before the gate meanwhile.
            private void SiegeWave()
            {
                int hall = -1;
                for (int i = 0, ordinal = 0; i < _floor.Kinds.Length; i++)
                {
                    if (_floor.Kinds[i] != RoomKind.Combat)
                        continue;
                    hall = i;
                    if (++ordinal == _levers.GateSiegeHall)
                        break;
                }
                if (hall < 0)
                    return;
                SpawnWave(WaveEnemies(hall, _siegeWave % _wavesPerRoom[hall]), false, false, false);
                _siegeWave++;
                _sieging = true;
                _result.WaitSiegeWaves++;
                if (_trace != null)
                    Emit($"gate_siege wave={_siegeWave} hall={hall + 1} clock={N(_horde.Seconds)}");
            }

            private void AdvanceFloor()
            {
                _gateHeld = false;
                if (RepeatsHall())
                    return;

                FloorStep step = _progress.Advance();
                switch (step.Kind)
                {
                    case FloorStepKind.Wave:
                        if (step.WaveIndex == 0)
                            BeginRoom(step.RoomIndex);
                        if (step.WaveIndex == 0 && TryRoomSite(step.RoomIndex, out _siteX, out _siteY))
                        {
                            ReleaseWave();
                            _siteStep = step;
                            _roomSiteOpen = true;
                            _siteWait = 0f;
                        }
                        else
                        {
                            StartWave(step.RoomIndex, step.WaveIndex);
                        }
                        break;
                    case FloorStepKind.NonCombatRoom:
                        BeginRoom(step.RoomIndex);
                        EnterForge();
                        break;
                    default:
                        _nonCombat = false;
                        OpenGateExit();
                        break;
                }
            }

            // A held hall has sent its last wave before its time is up: its waves begin again in order, and the floor stays
            // until a wave falls with the time run out. The floor's progress waits on the hall's last wave meanwhile, so the
            // advance that follows the time running out moves on to the next room.
            private bool RepeatsHall()
            {
                if (_levers.HallHoldSeconds <= 0f || _roomIndex < 0 || _progress.IsComplete ||
                    _floor.Kinds[_roomIndex] != RoomKind.Combat || _waveIndex < 0 || _gateBossWave)
                    return false;
                bool repeating = _hallRepeatWave >= 0;
                if (!repeating && _waveIndex != _wavesPerRoom[_roomIndex] - 1)
                    return false;
                if (_seconds - _hallStartedAt >= _levers.HallHoldSeconds)
                    return false;
                int next = repeating ? _hallRepeatWave : 0;
                _hallRepeatWave = (next + 1) % _wavesPerRoom[_roomIndex];
                StartWave(_roomIndex, next);
                return true;
            }

            // Whether the next step is the gate boss's wave: the room before it has sent its last wave.
            private bool NextIsGate()
            {
                int room = _progress.RoomIndex;
                return room >= 0 && room + 1 < _floor.Kinds.Length && _floor.Kinds[room + 1] == RoomKind.Boss &&
                       _progress.WaveIndex == _wavesPerRoom[room] - 1;
            }

            private void BeginRoom(int roomIndex)
            {
                if (_roomIndex >= 0 && _result.RoomSeconds[_roomIndex] < 0f)
                {
                    _result.RoomSeconds[_roomIndex] = _seconds - _roomStartedAt;
                    if (_trace != null)
                        Emit($"room_complete room={_roomIndex + 1} active={N(_seconds - _roomStartedAt)} kills={_roomKills} hp={N(_heroHealth.Current)}");
                }
                _roomKills = 0;
                if (_trace != null)
                    Emit($"room_start room={roomIndex + 1} kind={_floor.Kinds[roomIndex]} hp={N(_heroHealth.Current)}");
                _roomIndex = roomIndex;
                _roomStartedAt = _seconds;
                _hallRepeatWave = -1;
            }

            private float _roomStartedAt;

            // EncounterController.TryRoomSite: a hall or elite after the floor's first opens at its own district, the round
            // going on from the districts the run's earlier floors opened.
            private bool TryRoomSite(int roomIndex, out float x, out float y)
            {
                x = y = 0f;
                if (!IsHallKind(_floor.Kinds[roomIndex]))
                    return false;
                int ordinal = 0;
                for (int i = 0; i < roomIndex; i++)
                    if (IsHallKind(_floor.Kinds[i]))
                        ordinal++;
                if (ordinal == 0)
                    return false;
                FoundryMap.RoomSite(_hallsOnEarlierFloors, ordinal, out x, out y);
                return true;
            }

            private static bool IsHallKind(RoomKind kind) => kind == RoomKind.Combat || kind == RoomKind.Elite;

            private int SurgesIn(int roomIndex) => _floor.Kinds[roomIndex] == RoomKind.Combat ? _levers.HallSurges : 0;

            // EncounterController.WaveEnemies: an authored wave as authored, a surge as the wave it repeats with that wave's
            // last enemy added up to the surge's size.
            private List<Enemy> WaveEnemies(int roomIndex, int waveIndex)
            {
                _waveEnemies.Clear();
                Enemy[][] room = _floor.Rooms[roomIndex];
                int authored = room.Length;
                int surge = waveIndex - authored;
                Enemy[] wave = room[surge < 0 ? waveIndex : HallSurgeRule.SourceWave(authored, surge)];
                for (int e = 0; e < wave.Length; e++)
                    _waveEnemies.Add(wave[e]);
                if (surge >= 0)
                    for (int e = wave.Length; e < HallSurgeRule.Size(wave.Length, surge); e++)
                        _waveEnemies.Add(wave[wave.Length - 1]);
                return _waveEnemies;
            }

            private void StartWave(int roomIndex, int waveIndex)
            {
                RoomKind kind = _floor.Kinds[roomIndex];
                bool boss = kind == RoomKind.Boss;
                _ambushNext = _levers.AmbushSurges && kind == RoomKind.Combat && waveIndex >= _floor.Rooms[roomIndex].Length;
                SpawnWave(WaveEnemies(roomIndex, waveIndex), boss, boss, kind == RoomKind.Elite);
                _ambushNext = false;
                _waveIndex = waveIndex;
                if (waveIndex == 0 && _hallRepeatWave < 0)
                    _hallStartedAt = _seconds;
            }

            // EncounterController.SpawnWave on the foundry; elite, an elite room's wave (FoundryResult.EliteFought).
            private void SpawnWave(List<Enemy> definitions, bool atGate, bool boss, bool elite)
            {
                ReleaseWave();
                _nonCombat = false;
                int n = _floor.FloorNumber;
                float scale = _heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(ClimbScaling.Damage(n) * scale, _floor.ModifierDamagePercent);
                float floorHealth = ClimbScaling.Health(n) * (boss ? ClimbScaling.Boss(n) : 1f);
                int count = definitions.Count;
                float heroX = HeroX;
                float heroY = HeroY;
                _waveMotion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Map, heroX, heroY);
                EntrySides.Place(_waveOrdinal, count, heroX, heroY, Map, EntryDepth, FormationSpacing, BodySpacing,
                    _placements);
                _gateBossWave = atGate;
                if (atGate)
                    FoundryMap.PlaceAtGate(count, _placements);
                else if (_ambushNext)
                    Ambush(count, heroX, heroY);
                for (int i = 0; i < count; i++)
                {
                    Enemy definition = definitions[i];
                    float healthScale = floorHealth * scale * (atGate && i == 0 ? WardenHealthShare() : 1f);
                    if (!boss)
                        healthScale *= _levers.NodeHealthScale;
                    Foe foe = CreateFoe(definition,
                        FloorScaling.Health(definition.Health, healthScale, _floor.ModifierHealthPercent), damageBonus, false);
                    foe.Gold = FloorScaling.Gold(definition.Gold, _floor.ModifierGoldPercent);
                    foe.Experience = definition.Experience;
                    foe.Motion = _waveMotion;
                    foe.Slot = _waveMotion.Add(foe, _placements[i], definition.Speed, foe.Weapon.Range);
                    if (atGate)
                    {
                        _waveMotion.SetMarch(foe.Slot, GateMarchFactor, GateMarchRange);
                        foe.GateBoss = i == 0;
                        foe.Escort = i > 0;
                    }
                    foe.Elite = elite;
                    foe.EliteLeader = elite && i == 0;
                    _wave.Add(foe);
                }
                _eliteWave = elite;
                _waveAlive = count;
                _waveOrdinal++;
                _fight.Begin();
                _waveStartedAt = _seconds;
                EncounterStarted();
                if (atGate)
                    GateBossArrived();
            }

            // The floor's gate share (WardenRule.GateHealthShare on floor 1, a later floor's own through
            // WardenRule.GateHealthShareOn, LaterFloorLevers), or a lever's share, grown with the floor's clock where
            // FoundryLevers.WardenClockFrom asks for it.
            private float WardenHealthShare()
            {
                float share = _levers.WardenHealthShare;
                if (_levers.WardenClockFrom > 0f && _horde != null)
                    share *= ClockGrowth(_horde.Seconds) / ClockGrowth(_levers.WardenClockFrom);
                return share;
            }

            private static float ClockGrowth(float seconds) =>
                1f + HordeClock.HealthGrowth * Math.Min(seconds, HordeClock.TargetSeconds) / HordeClock.TargetSeconds;

            // EncounterController.CreateEnemy: the definition's weapon, hardened by the floor's damage.
            private Foe CreateFoe(Enemy definition, float health, float damageBonus, bool horde)
            {
                var foe = new Foe
                {
                    Definition = definition, Health = new HealthState(health), Horde = horde, Run = this,
                    Weapon = new WeaponRuntime(definition.Damage, definition.Interval, definition.Reach,
                        definition.InitialDelay),
                    Enrage = definition.EnrageAt > 0f ? new EnrageRule(definition.EnrageAt) : null,
                    SpawnFrame = _frame, SpawnedAt = _seconds
                };
                if (damageBonus != 0f)
                    foe.Weapon.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Percent, damageBonus));
                foe.Health.Died += () => FoeDied(foe);
                return foe;
            }

            // HeroMovementInput fills the bar on every wave (the bar is off under the current rules), and ChestSpawner places
            // a room's chest on its first wave.
            private void EncounterStarted()
            {
                _stamina.Fill();
                if (_chestRoom == _roomIndex && _chestFloor == _floor.FloorNumber)
                    return;
                _chestRoom = _roomIndex;
                _chestFloor = _floor.FloorNumber;
                _chestIndex++;
                ChestRule.SpotFor(_roomIndex, Map, out _chestX, out _chestY);
                _chestReward = ChestRule.RewardFor(_roomIndex, _chestIndex, _run.Seed);
                _chestPlaced = true;
                _chestOpen = false;
            }

            // BossRevealView: the encounter holds from this frame until the camera has turned back.
            private void GateBossArrived()
            {
                _revealHold = true;
                _revealStarted = true;
                _revealLeft = _revealFrames - 1;
                _result.LevelAtGate = _run.Level;
                _result.WeaponsAtGate = _loadout.Count;
                _result.TomesAtGate = _upgrades.TomesHeld;
                _result.WardenHealth = _wave[0].Health.Maximum;
                _result.HeroHealthAtGate = _heroHealth.Current;
                _result.HeroMaximumAtGate = _heroHealth.Maximum;
                _result.DifficultyAtGate = _heroStats.Difficulty;
                _result.ClockAtGate = _horde != null ? _horde.Seconds : 0f;
                if (_trace != null)
                    Emit($"boss_start hp={N(_heroHealth.Current)} level={_run.Level} warden={N(_result.WardenHealth)}");
                if (_revealLeft <= 0)
                    _revealHold = false;
            }

            // The reveal's frames at a fixed 1/60 s: the coroutine's elapsed time, in float, past the turn and the hold and
            // then past the turn back. The first iteration runs in the frame the boss walks out.
            private static int RevealFrames()
            {
                float elapsed = 0f;
                float returnFrom = -1f;
                for (int frames = 1; frames < 100000; frames++)
                {
                    elapsed += Step;
                    if (returnFrom < 0f && elapsed >= RevealTurnSeconds + RevealHoldSeconds)
                        returnFrom = elapsed;
                    if (returnFrom >= 0f && elapsed - returnFrom >= RevealTurnSeconds)
                        return frames;
                }
                throw new InvalidOperationException("The reveal never ends.");
            }

            private void ReleaseWave()
            {
                for (int i = 0; i < _wave.Count; i++)
                    _wave[i].Released = true;
                _wave.Clear();
                _waveMotion = null;
                _waveAlive = 0;
            }

            private void EnterForge()
            {
                ReleaseWave();
                _nonCombat = true;
                _transitionWait = 0f;
                // The room's options in the asset's order, the same on floor 1 and floor 2: Forge_Mend, Forge_Temper.
                ForgeOption mend = _floor.FloorNumber >= 2 && _levers.LaterMendAmount > 0f
                    ? new ForgeOption("forge_mend", "Mend", "Restore {0:0}% health", ForgeEffect.Heal, Math.Min(1f, _levers.LaterMendAmount))
                    : Mend();
                _forge.Open(new[] { mend, Temper() });
                if (_trace != null)
                    Emit("forge_opened");
            }

            private void OpenGateExit()
            {
                ReleaseHorde();
                _gateExitOpen = true;
                FoundryMap.GateExitSpot(out _exitX, out _exitY);
                _exitDwell = new ExitDwell();
                _lastHeroX = HeroX;
                _lastHeroY = HeroY;
            }

            private void TickGateExit()
            {
                float heroX = HeroX;
                float heroY = HeroY;
                bool moved = heroX != _lastHeroX || heroY != _lastHeroY;
                _lastHeroX = heroX;
                _lastHeroY = heroY;
                if (!_exitDwell.Step(Step, !moved && ExitRule.IsWithinReach(heroX, heroY, _exitX, _exitY, ExitRule.GateRadius)))
                    return;
                _gateExitOpen = false;
                // CombatSetup.OnFloorCleared: whatever still lies flies to the hero, and the floor is done.
                _result.DropsSwept += _drops.CollectAll();
                _cleared = true;
                if (_trace != null)
                    Emit($"room_complete room={_roomIndex + 1} active={N(_seconds - _roomStartedAt)} kills={_roomKills} hp={N(_heroHealth.Current)} (floor cleared, level {_run.Level})");
            }

            private void MovePack()
            {
                if (_waveMotion == null || _fight.IsCleared)
                    return;
                _waveMotion.Held = _pitHeld;
                _waveMotion.Step(Step, HeroX, HeroY);
            }

            // EncounterController.TickHorde: the horde stands from the floor's first step to the boss's fall - never on the
            // way down to the next floor - and the forge freezes it, from the forge's offer until the next hall's wave forms
            // up, as IsInNonCombatRoom reads.
            private void TickHorde()
            {
                bool wanted = !_progress.IsComplete && !_gateExitOpen && !_descending;
                if (!wanted)
                {
                    if (_horde != null)
                        ReleaseHorde();
                    return;
                }
                if (_horde == null)
                    BeginHorde();
                if (_choices.IsOpen || _nonCombat)
                    return;

                _hordeMotion.Held = _pitHeld;
                _hordeMotion.Step(Step, HeroX, HeroY);
                bool bossRoom = _floor.Kinds[_progress.RoomIndex] == RoomKind.Boss;
                HordeState state = bossRoom && _bossEnraged ? HordeState.Horde : HordeRule.StateFor(HallsCleared(), bossRoom);
                if (state != _horde.State)
                    _horde.SetState(state);
                _horde.Tick(Step, _hordeAlive, _waveAlive, _hordeSpawns);
                for (int i = 0; i < _hordeSpawns.Count; i++)
                    SpawnHordeFoe(_hordeSpawns[i]);
            }

            private int HallsCleared()
            {
                int cleared = 0;
                for (int i = 0; i < _progress.RoomIndex && i < _floor.Kinds.Length; i++)
                    if (IsHallKind(_floor.Kinds[i]))
                        cleared++;
                return cleared;
            }

            private void BeginHorde()
            {
                _horde = new HordeDriver(_levers, _floor.HordeRoles.Length, _run.Seed + _floor.FloorNumber);
                _bossEnraged = false;
                _hordeMotion = new PackMotion(BodySpacing, PackLayout.HalfWidth * FormationSpacing, Map, HeroX, HeroY);
            }

            private void SpawnHordeFoe(HordeDirector.Spawn spawn)
            {
                if (!TryHordeSpot(spawn.Ordinal, out float x, out float y))
                {
                    _result.HordeSpawnsDropped++;
                    return;
                }
                int slot = -1;
                for (int i = 0; i < _hordeFoes.Count; i++)
                {
                    if (_hordeFoes[i].Defeated)
                    {
                        slot = i;
                        break;
                    }
                }
                if (slot < 0 && _hordeMotion.Count >= PackLayout.MaxPackSize)
                {
                    _result.HordeSpawnsDropped++;
                    return;
                }

                Enemy definition = _floor.HordeRoles[spawn.Role];
                int n = _floor.FloorNumber;
                float scale = _heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(
                    ClimbScaling.Damage(n) * scale * HordeRule.DamageShare * _horde.DamageMultiplier, _floor.ModifierDamagePercent);
                Foe foe = CreateFoe(definition,
                    FloorScaling.Health(definition.Health,
                        ClimbScaling.Health(n) * scale * _levers.HordeHealthShare * _horde.HealthMultiplier,
                        _floor.ModifierHealthPercent),
                    damageBonus, true);
                // An overrun floor's horde pays nothing (HordeClock.PaysExperience); otherwise its share.
                foe.Experience = !HordeClock.PaysExperience(_horde.Seconds)
                    ? 0
                    : TunedHordeExperience.Experience(definition.Experience, _levers.HordeExperienceShare);
                foe.Motion = _hordeMotion;
                var placement = new EntryPlacement(EntrySides.SideOf(spawn.Ordinal, 0), 0f, x, y);
                if (slot >= 0)
                {
                    _hordeFoes[slot].Released = true;
                    _hordeMotion.Replace(slot, foe, placement, definition.Speed, foe.Weapon.Range);
                    _hordeFoes[slot] = foe;
                }
                else
                {
                    slot = _hordeMotion.Add(foe, placement, definition.Speed, foe.Weapon.Range);
                    _hordeFoes.Add(foe);
                }
                foe.Slot = slot;
                _hordeAlive++;
                _result.HordeSpawned++;
            }

            // EncounterController.TryHordeSpot: the first of the floor's spots on the floor a body clear of the horde.
            private bool TryHordeSpot(int ordinal, out float x, out float y)
            {
                float clearSquared = BodySpacing * BodySpacing;
                for (int attempt = 0; attempt < FoundryMap.HordeSpotAttempts; attempt++)
                {
                    if (!FoundryMap.HordeSpot(ordinal, attempt, HeroX, HeroY, out x, out y))
                        continue;
                    bool clear = true;
                    for (int i = 0; clear && i < _hordeFoes.Count; i++)
                    {
                        Foe other = _hordeFoes[i];
                        clear = other.Defeated ||
                                ArenaFloor.FloorDistanceSquared(x, y, _hordeMotion.XOf(other.Slot), _hordeMotion.YOf(other.Slot)) >=
                                clearSquared;
                    }
                    if (clear)
                        return true;
                }
                x = y = 0f;
                return false;
            }

            private void ReleaseHorde()
            {
                for (int i = 0; i < _hordeFoes.Count; i++)
                    _hordeFoes[i].Released = true;
                _hordeFoes.Clear();
                _horde = null;
                _hordeMotion = null;
                _hordeAlive = 0;
            }

            // ---------------------------------------------------------------------------------------------------------
            // Deaths and damage
            // ---------------------------------------------------------------------------------------------------------

            // Health.ApplyDamage on an enemy: whatever lands is attributed, the first hit remembered, enrage read.
            public void FoeDamaged(Foe foe, DamageContext context)
            {
                if (!foe.Health.IsAlive)
                    return;
                float before = foe.Health.Current;
                if (foe.FirstHitAt < 0f && context.Amount > 0f)
                    foe.FirstHitAt = _seconds;
                // FoundryLevers.WardenGuardShare: the Warden takes all of a blow only in the window after its own.
                if (foe.GateBoss && _levers.WardenGuardShare != 1f &&
                    (_slam.IsMarked || !_slam.HoldsWeaponFor(_levers.WardenGuardOpenSeconds)))
                    context = new DamageContext(context.Amount * _levers.WardenGuardShare, context.Source, context.IsCritical);
                DamageSource source = _source;
                FoundryWeaponWork striking = source == DamageSource.Weapon ? _striking : null;
                if (striking != null && striking.Id == DaggersId && foe.GateBoss && _levers.DaggersWardenDamage != 1f)
                    context = new DamageContext(ScaleOf(context.Amount, _levers.DaggersWardenDamage), context.Source, context.IsCritical);
                if (striking != null && striking.Id == DaggersId && (_levers.DaggersEliteLeaderOnly ? foe.EliteLeader : foe.Elite))
                {
                    float scale = context.IsCritical ? _levers.DaggersEliteDamage * _levers.DaggersEliteCritDamage : _levers.DaggersEliteDamage;
                    if (scale != 1f)
                        context = new DamageContext(ScaleOf(context.Amount, scale), context.Source, context.IsCritical);
                }
                if (striking != null && striking.Id == SwordId)
                {
                    float scale = SwordDamageScale(foe);
                    if (scale != 1f)
                        context = new DamageContext(ScaleOf(context.Amount, scale), context.Source, context.IsCritical);
                    if (_levers.SwordKnockbackUnits > 0f && !foe.GateBoss && foe.Definition.Speed > 0f)
                        foe.StaggeredUntil = Math.Max(foe.StaggeredUntil, _seconds + _levers.SwordKnockbackUnits / foe.Definition.Speed);
                }
                bool proc = striking != null && StrikeHasProcced();
                foe.Health.ApplyDamage(context);
                float dealt = before - foe.Health.Current;
                if (striking != null && dealt > 0f)
                    Attribute(striking, foe, dealt, proc);
                switch (source)
                {
                    case DamageSource.Weapon:
                        _result.DealtByWeapons += dealt;
                        if (foe.GateBoss)
                            _result.WardenFromWeapons += dealt;
                        break;
                    case DamageSource.Burn:
                        _result.DealtByBurn += dealt;
                        if (foe.GateBoss)
                            _result.WardenFromBurn += dealt;
                        break;
                    case DamageSource.Ability:
                        _result.DealtByAbility += dealt;
                        if (foe.GateBoss)
                            _result.WardenFromAbility += dealt;
                        break;
                    default:
                        _result.DealtByPit += dealt;
                        if (foe.GateBoss)
                            _result.WardenFromPit += dealt;
                        break;
                }
                _result.BucketDamage[Bucket()] += dealt;
                if (foe.Enrage != null && foe.Enrage.Evaluate(foe.Health.Current, foe.Health.Maximum))
                    foe.Weapon.AddModifier(UpgradeStat.AttackSpeed, new StatModifier(ModifierOperation.Percent, 1f));
            }

            // weapon_effectiveness: a landing of the striking weapon's, on what it fell, and whether its evolution's proc
            // dealt it.
            private void Attribute(FoundryWeaponWork work, Foe foe, float dealt, bool proc)
            {
                if (proc)
                {
                    work.ProcLandings++;
                    work.DealtByProcs += dealt;
                }
                else
                {
                    work.Landings++;
                }
                if (foe.GateBoss)
                    work.DealtWarden += dealt;
                else if (foe.Horde)
                    work.DealtHorde += dealt;
                else
                    work.DealtNode += dealt;
                if (foe.Elite)
                    work.DealtElite += dealt;
                if (foe.EliteLeader)
                    work.DealtEliteLeader += dealt;
                if (work.EvolvedAt >= 0f)
                    work.DealtAfterEvolving += dealt;
            }

            // Whether the weapon striking now has fired a pulse, a splinter or a kill burst since the strike began: each
            // counter rises before its damage lands, and all three come after the strike's own landings (WeaponRuntime).
            private bool StrikeHasProcced() =>
                _strikingWeapon != null && ProcsFired(_strikingWeapon) > _procsBeforeStrike;

            private static int ProcsFired(WeaponRuntime weapon) =>
                weapon.PulsesFired + weapon.SplintersFired + weapon.KillBurstsFired;

            // The work of the weapon in this loadout slot, from the frame it was first seen there.
            private FoundryWeaponWork WorkAt(int slot)
            {
                while (_result.WeaponWork.Count <= slot && _result.WeaponWork.Count < _loadout.Count)
                    _result.WeaponWork.Add(new FoundryWeaponWork
                    {
                        Id = _loadout.At(_result.WeaponWork.Count).WeaponId, HeldFrom = FloorTime
                    });
                return _result.WeaponWork[slot];
            }

            // Who landed the blow being applied now: the striking weapon, the burn, the ability or the pit.
            private string KillingBlow()
            {
                switch (_source)
                {
                    case DamageSource.Weapon:
                        return _striking?.Id;
                    case DamageSource.Burn:
                        return FoundryKill.KilledByBurn;
                    case DamageSource.Ability:
                        return FoundryKill.KilledByAbility;
                    default:
                        return FoundryKill.KilledByPit;
                }
            }

            // EncounterController.OnEnemyDied / OnHordeEnemyDied, CombatSetup.OnEnemyDefeated's drop, then the enemy's
            // BurnStatus.OnDied - the order their Died handlers were subscribed in.
            private void FoeDied(Foe foe)
            {
                if (foe.Released || foe.Defeated)
                    return;
                foe.Defeated = true;
                _result.Kills.Add(new FoundryKill(foe.Definition.Name, foe.Horde, _seconds - foe.SpawnedAt,
                    foe.FirstHitAt >= 0f ? _seconds - foe.FirstHitAt : 0f, KillingBlow(),
                    _source == DamageSource.Weapon && StrikeHasProcced(), FloorTime));
                _result.BucketKills[Bucket()]++;
                _roomKills++;
                if (foe.Horde)
                {
                    _hordeAlive--;
                    _result.HordeKills++;
                    _horde?.Fell();
                    _rewards.TryDropKill(foe, foe.Experience, 0, foe.X, foe.Y, _drops);
                }
                else
                {
                    _waveAlive--;
                    _result.WaveKills++;
                    if (foe.GateBoss)
                    {
                        _result.WardenSeconds = _seconds - _waveStartedAt;
                        _result.BossLeft = 0f;
                    }
                    if (_waveAlive == 0)
                        _fight.Clear();
                    _rewards.TryDropKill(foe, foe.Experience, foe.Gold, foe.X, foe.Y, _drops);
                    if (_waveAlive == 0)
                        WaveCleared(foe);
                    if (_waveAlive == 0 && _progress.IsFinalWave)
                        AdvanceFloor();
                }

                if (foe.Burn == null)
                    return;
                bool spreads = foe.Burn.Spreads;
                foe.Burn.Clear();
                if (!spreads)
                    return;
                CollectNear(foe, BurnRule.SpreadRadius, _spread);
                int count = Math.Min(BurnRule.SpreadTargets, _spread.Count);
                for (int i = 0; i < count; i++)
                    Ignite((Foe)_spread[i], BurnRule.SpreadStacks, true);
            }

            private void WaveCleared(Foe last)
            {
                float seconds = _seconds - _waveStartedAt;
                if (_trace != null)
                    Emit($"wave_clear room={_roomIndex + 1} wave={_waveIndex + 1} active={N(seconds)} hp={N(_heroHealth.Current)}");
                if (_roomIndex >= 0 && _floor.Kinds[_roomIndex] != RoomKind.Boss)
                    _result.WaveSeconds[_roomIndex] = Math.Max(0f, _result.WaveSeconds[_roomIndex]) + seconds;
                if (_gateBossWave)
                {
                    _result.BossSeconds = seconds;
                    _result.BossLeft = 0f;
                    _bossFellAt = _seconds;
                }
            }

            private float _bossFellAt = -1f;

            // BurnStatus.On and Ignite: a living enemy only; its first stack adds the status, which ticks from the next frame.
            private void Ignite(Foe foe, int stacks, bool spread)
            {
                if (foe == null || foe.Released || !foe.IsAlive)
                    return;
                if (foe.Burn == null)
                {
                    foe.Burn = new BurnState();
                    foe.BurnFresh = true;
                    _burning.Add(foe);
                }
                foe.Burn.Apply(stacks, spread);
            }

            // Health.ApplyDamage on the hero, and its Damaged event as the measurement and the death report read it.
            public void HeroDamaged(DamageContext context)
            {
                if (!_heroHealth.IsAlive || context.Amount == 0f)
                    return;
                float before = _heroHealth.Current;
                _heroHealth.ApplyDamage(context);
                float taken = before - _heroHealth.Current;
                if (taken <= 0f)
                    return;
                _lastHeroHit = _seconds;
                if (context.Source is Foe foe)
                {
                    _lastAttacker = foe;
                    _lastHarm = (foe.Horde ? "horde " : foe.GateBoss ? string.Empty : foe.Escort ? "escort " : "wave ") +
                                foe.Definition.Name;
                    if (foe.Horde && GateBoss != null)
                        _result.FightTakenFromHorde += taken;
                    if (foe.Escort && GateBoss != null)
                        _result.FightTakenFromEscort += taken;
                    if (foe.Horde)
                        _result.TakenFromHorde += taken;
                    else if (foe.GateBoss && _harmIsSlam)
                        _result.TakenFromSlams += taken;
                    else if (foe.GateBoss)
                        _result.TakenFromWarden += taken;
                    else
                        _result.TakenFromWave += taken;
                    if (_harmIsSlam)
                        _lastHarm = "Warden slam";
                }
                else if (_hazardHarm != null)
                {
                    _result.TakenFromHazards += taken;
                    _lastHarm = _hazardHarm;
                }
                else
                {
                    _result.TakenFromPit += taken;
                    if (GateBoss != null)
                        _result.FightTakenFromPit += taken;
                    _lastHarm = "pit";
                }
                HarmPlace place = Place();
                // FoundryLevers.CountsHarmByPlace: the health lost by place and blow, for the research's tables alone.
                if (_result.TakenBy != null)
                {
                    string key = PlaceName(place, _roomIndex + 1) + "|" + _lastHarm;
                    _result.TakenBy.TryGetValue(key, out float sum);
                    _result.TakenBy[key] = sum + taken;
                }
                float share = _heroHealth.Current / Math.Max(1f, _heroHealth.Maximum);
                if (share < _result.LowestHealthShare)
                {
                    _result.LowestHealthShare = share;
                    _result.LowestRoom = _roomIndex + 1;
                    _result.LowestPlace = place;
                    _result.LowestHarm = _lastHarm;
                }
                if (!_heroHealth.IsAlive && _result.DeathRoom < 0)
                    RecordDeath();
            }

            private bool _harmIsSlam;

            // Where the hero is hurt, for FoundryResult.LowestPhase and TakenBy: the Warden's fight, a room's node fight, or
            // the walk between (a room's hall marked, or nothing standing but the horde).
            private HarmPlace Place() =>
                _gateBossWave && _waveAlive > 0 ? HarmPlace.Boss : _waveAlive > 0 ? HarmPlace.Room : HarmPlace.Between;

            private void RecordDeath()
            {
                _result.DeathRoom = _roomIndex + 1;
                _result.Killer = _lastHarm;
                if (_trace != null)
                    Emit($"run_end defeat room={_roomIndex + 1} killer={_lastHarm} level={_run.Level}");
                if (_roomSiteOpen)
                    _result.DeathPhase = "walk";
                else if (_gateExitOpen)
                    _result.DeathPhase = "exit";
                else if (_descending)
                    _result.DeathPhase = "descent";
                else if (_nonCombat)
                    _result.DeathPhase = "forge";
                else if (_gateHeld)
                    _result.DeathPhase = GateWaitPhase;
                else if (_gateBossWave && _waveAlive > 0)
                    _result.DeathPhase = "boss";
                else if (_waveAlive > 0)
                    _result.DeathPhase = "wave " + (_waveIndex + 1);
                else
                    _result.DeathPhase = "between waves";
                // CombatSetup.OnHeroDied: the run ends, and with it every burn and every offer.
                _run.End(RunOutcome.Defeat);
            }

            // ---------------------------------------------------------------------------------------------------------
            // The hero's and the enemies' attacks, the burns
            // ---------------------------------------------------------------------------------------------------------

            // The hero's targets in the order Targeting holds them: the wave's, then the horde's living enemies.
            private int TargetCount => _wave.Count + _hordeFoes.Count;

            private Foe TargetAt(int index) => index < _wave.Count ? _wave[index] : _hordeFoes[index - _wave.Count];

            private static bool IsValid(Foe foe) => foe != null && !foe.Released && foe.IsAlive;

            // Targeting.Acquire for the hero: the nearest living target within range, the earlier on a tie.
            private Foe Acquire(float range)
            {
                Foe nearest = null;
                float nearestDistanceSquared = range * range;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe candidate = TargetAt(i);
                    if (!IsValid(candidate))
                        continue;
                    float distanceSquared = ArenaFloor.FloorDistanceSquared(candidate.X, candidate.Y, HeroX, HeroY);
                    if (nearest == null ? distanceSquared <= nearestDistanceSquared : distanceSquared < nearestDistanceSquared)
                    {
                        nearest = candidate;
                        nearestDistanceSquared = distanceSquared;
                    }
                }
                return nearest;
            }

            // Targeting.CollectNear: the other living targets within radius of the centre, nearest first, earlier on ties.
            private void CollectNear(Foe centre, float radius, List<IDamageable> results) =>
                CollectNear(centre, centre.X, centre.Y, radius, results);

            private void CollectNear(Foe centre, float originX, float originY, float radius, List<IDamageable> results)
            {
                results.Clear();
                _distances.Clear();
                float radiusSquared = radius * radius;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe candidate = TargetAt(i);
                    if (candidate == centre || !IsValid(candidate))
                        continue;
                    float distanceSquared = ArenaFloor.FloorDistanceSquared(candidate.X, candidate.Y, originX, originY);
                    if (distanceSquared > radiusSquared)
                        continue;
                    int insertAt = results.Count;
                    while (insertAt > 0 && _distances[insertAt - 1] > distanceSquared)
                        insertAt--;
                    results.Insert(insertAt, candidate);
                    _distances.Insert(insertAt, distanceSquared);
                }
            }

            // AttackController's kill-burst neighbourhood: the living targets near a fallen enemy, where it fell.
            IReadOnlyList<IDamageable> INeighbourhood.Near(IDamageable centre, float radius)
            {
                if (centre is Foe foe)
                    CollectNear(foe, radius, _burst);
                else
                    _burst.Clear();
                return _burst;
            }

            // AttackController.Update for the hero: every weapon in slot order, each ticking, acquiring its own target with
            // its own reach and gathering its circles; Ember Memory sets each struck enemy alight (CombatSetup.IgniteStruck).
            private void HeroAttacks()
            {
                if (!_heroHealth.IsAlive)
                    return;
                for (int slot = 0; slot < _loadout.Count; slot++)
                {
                    WeaponRuntime weapon = _loadout.At(slot).Runtime;
                    weapon.Tick(Step);
                    if (!weapon.IsReady)
                        continue;
                    Foe target = Acquire(weapon.Range);
                    if (target == null)
                        continue;
                    _nearby.Clear();
                    float splashRadius = weapon.SplashRadius;
                    if (_levers.SwordSweepRadius > 0f && WorkAt(slot).Id == SwordId)
                        SwordSweep(target, WorkAt(slot).EvolvedAt >= 0f);
                    else if (splashRadius > 0f)
                        CollectNear(target, splashRadius, _nearby);
                    _pulse.Clear();
                    if (weapon.PulseDamage > 0f)
                        CollectNear(null, HeroX, HeroY, weapon.PulseRadius, _pulse);
                    _crit.Clear();
                    if (weapon.CritSplash > 0f)
                        CollectNear(target, weapon.CritSplashRadius, _crit);
                    _source = DamageSource.Weapon;
                    _striking = WorkAt(slot);
                    _strikingWeapon = weapon;
                    _procsBeforeStrike = ProcsFired(weapon);
                    // AttackController.GateBoss: the gate boss WardenSlamService handed the hero's attacker at its step
                    // (StepWarden's _slamBoss), on which each weapon lands its share (WardenRule: the Sword's 1.5).
                    bool struck = weapon.TryAttack(target, _hero, _nearby, _pulse, _crit, weapon.KillBurst > 0f ? this : null,
                        _slamBoss);
                    if (struck)
                        _striking.Strikes++;
                    _striking = null;
                    _strikingWeapon = null;
                    if (!struck)
                        continue;
                    _lastStrike = _seconds;
                    if (_result.FirstStrikeSeconds < 0f)
                        _result.FirstStrikeSeconds = FloorTime;
                    if (_heroDefinition.EmberMemory)
                        Ignite(target, 1, false);
                }
            }

            // FoundryLevers' Sword sweep (research): the enemies within the sweep's circle round the hero and within its arc
            // centred on the target, nearest the hero first; before the Sword evolves, under SwordSweepAtEvolution, the
            // shipped cleave's one neighbour within 2 units of the target instead (the sweep's share is then the cleave's).
            private void SwordSweep(Foe target, bool evolved)
            {
                if (_levers.SwordSweepAtEvolution && !evolved)
                {
                    CollectNear(target, ShippedCleaveRadius, _nearby);
                    if (_nearby.Count > 1)
                        _nearby.RemoveRange(1, _nearby.Count - 1);
                    return;
                }
                CollectNear(target, HeroX, HeroY, _levers.SwordSweepRadius, _nearby);
                if (_levers.SwordSweepArcDegrees >= 360f)
                    return;
                float half = _levers.SwordSweepArcDegrees * .5f;
                for (int i = _nearby.Count - 1; i >= 0; i--)
                    if (!WithinArc((Foe)_nearby[i], target, half))
                        _nearby.RemoveAt(i);
            }

            private const float ShippedCleaveRadius = 2f;

            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            private bool WithinArc(Foe foe, Foe target, float halfDegrees)
            {
                float ax = target.X - HeroX, ay = target.Y - HeroY, bx = foe.X - HeroX, by = foe.Y - HeroY;
                float la = (float)Math.Sqrt(ax * ax + ay * ay), lb = (float)Math.Sqrt(bx * bx + by * by);
                if (la <= 0f || lb <= 0f)
                    return true;
                float cos = (ax * bx + ay * by) / (la * lb);
                return cos >= (float)Math.Cos(halfDegrees * Math.PI / 180.0);
            }

            // FoundryLevers' Sword damage by what it lands on (research); 1 at the shipped levers.
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
            private float SwordDamageScale(Foe foe) =>
                foe.GateBoss ? _levers.SwordWardenDamage : foe.Horde ? _levers.SwordHordeDamage : _levers.SwordNodeDamage;

            // Every enemy's AttackController: its weapon ticks, and strikes the hero once it stands in reach. An enemy
            // spawned this frame starts next frame, as a component instantiated in an Update does.
            private void EnemyAttacks()
            {
                for (int i = 0; i < _wave.Count && _heroHealth.IsAlive; i++)
                    EnemyAttack(_wave[i]);
                for (int i = 0; i < _hordeFoes.Count && _heroHealth.IsAlive; i++)
                    EnemyAttack(_hordeFoes[i]);
            }

            private void EnemyAttack(Foe foe)
            {
                if (foe.Released || !foe.IsAlive || foe.SpawnFrame == _frame)
                    return;
                // WardenSlamService holds the gate boss's AttackController from the mark until the hammer is out of the
                // floor: its weapon neither ticks nor strikes meanwhile, the mark's own frame included (StepWarden runs
                // first).
                if (ReferenceEquals(foe, _slamBoss) && _levers.WardenRecoverySeconds >= 0f &&
                    _slam.HoldsWeaponFor(_levers.WardenRecoverySeconds))
                    return;
                foe.Weapon.Tick(Step);
                if (!foe.Weapon.IsReady)
                    return;
                // FoundryLevers.SwordKnockbackUnits (research): knocked back, it cannot reach the hero yet.
                if (_seconds < foe.StaggeredUntil)
                    return;
                float reach = foe.Weapon.Range;
                if (ArenaFloor.FloorDistanceSquared(HeroX, HeroY, foe.X, foe.Y) > reach * reach)
                    return;
                _harmIsSlam = false;
                if (foe.Weapon.TryAttack(_hero, foe) && _trace != null && ReferenceEquals(foe, _slamBoss))
                    Emit($"warden_strike hp={N(_heroHealth.Current)}");
            }

            // BurnStatus.Update on every burning enemy, in the order the statuses were added; a status added this frame
            // first runs next frame. A burn is the hero's damage, and stops with the hero.
            private void TickBurns()
            {
                int count = _burning.Count;
                for (int i = 0; i < count; i++)
                {
                    Foe foe = _burning[i];
                    if (foe.BurnFresh || foe.Released || !foe.Burn.IsBurning || !foe.IsAlive)
                        continue;
                    if (!_heroHealth.IsAlive)
                    {
                        foe.Burn.Clear();
                        continue;
                    }
                    float damage = foe.Burn.Tick(Step);
                    if (damage <= 0f)
                        continue;
                    _source = DamageSource.Burn;
                    foe.ApplyDamage(new DamageContext(damage, _hero));
                }
                if (_burning.Count > 256)
                    _burning.RemoveAll(foe => foe.Released || !foe.IsAlive);
            }

            // ---------------------------------------------------------------------------------------------------------
            // The foundry's services
            // ---------------------------------------------------------------------------------------------------------

            // AltarService: staying on an unused altar long enough raises the difficulty and grants an offer.
            private void StepAltars()
            {
                if (_run.HasEnded || _choices.IsOpen)
                    return;
                int taken = _altars.Step(HeroX, HeroY, Step);
                if (taken < 0)
                    return;
                _result.AltarsTaken++;
                _heroStats.AddModifier(UpgradeStat.Difficulty, new StatModifier(ModifierOperation.Flat, AltarRule.DifficultyCost));
                if (_trace != null)
                    Emit($"altar_taken altar={taken} difficulty={N(_heroStats.Difficulty)} level={_run.Level} hp={N(_heroHealth.Current)}");
                _run.GrantBonusUpgrade();
            }

            // PitEruptionService: the clock runs while anything stands, and the spill burns whatever is on the rim.
            private void StepPit()
            {
                _pitRunning = !_run.HasEnded && _waveAlive + _hordeAlive > 0;
                if (!_pitRunning || _choices.IsOpen || _revealHold || !_pit.Step(Step))
                    return;
                _result.Eruptions++;
                bool heroBurnt = false;
                if (_heroHealth.IsAlive && PitEruptionRule.InBand(HeroX, HeroY))
                {
                    heroBurnt = true;
                    _result.EruptionsOnHero++;
                    _harmIsSlam = false;
                    _hero.ApplyDamage(new DamageContext(_heroHealth.Maximum * PitEruptionRule.HeroShare));
                }
                Foe gate = GateBoss;
                int burnt = 0, killed = 0;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (!IsValid(foe) || !PitEruptionRule.InBand(foe.X, foe.Y))
                        continue;
                    float share = ReferenceEquals(foe, gate) ? PitEruptionRule.BossShare : PitEruptionRule.EnemyShare;
                    _source = DamageSource.Pit;
                    foe.ApplyDamage(new DamageContext(foe.Health.Maximum * share));
                    burnt++;
                    if (!foe.IsAlive)
                        killed++;
                }
                if (_trace != null)
                    Emit($"pit_eruption hero_burnt={heroBurnt} burnt={burnt} killed={killed} hp={N(_heroHealth.Current)}");
            }

            // EncounterController.GateBoss: the gate wave's boss while it stands.
            private Foe GateBoss => _gateBossWave && _wave.Count > 0 && !_wave[0].Defeated ? _wave[0] : null;

            // WardenSlamService: the mark where the hero stands, the hammer a telegraph later, the horde thickening below
            // half health. Stepped where the service's order (-40) runs it, after the encounter and before anything
            // attacks, so the gate boss's weapon is held from the frame its mark goes down (EnemyAttack).
            private void StepWarden()
            {
                Foe gate = GateBoss;
                if (gate != _slamBoss)
                {
                    _slamBoss = gate;
                    _slamEnraged = false;
                    _bossEnraged = false;
                    _slam.Reset();
                }
                Foe boss = _slamBoss != null && _slamBoss.IsAlive ? _slamBoss : null;
                if (boss == null)
                    return;
                if (_run.HasEnded || _choices.IsOpen || _revealHold)
                    return;
                if (!_slamEnraged && WardenRule.IsEnraged(boss.Health.Current, boss.Health.Maximum))
                {
                    _slamEnraged = true;
                    _bossEnraged = true;
                }
                switch (_slam.Advance(Step, _slamEnraged))
                {
                    case WardenSlamClock.Step.Marked:
                        _markX = HeroX;
                        _markY = HeroY;
                        if (_trace != null)
                            Emit($"slam_mark enraged={_slamEnraged} x={N(_markX)} y={N(_markY)} hp={N(_heroHealth.Current)}");
                        break;
                    case WardenSlamClock.Step.Slammed:
                        _result.Slams++;
                        float distanceSquared = ArenaFloor.FloorDistanceSquared(HeroX, HeroY, _markX, _markY);
                        bool hit = _heroHealth.IsAlive && distanceSquared <= WardenRule.SlamRadius * WardenRule.SlamRadius;
                        if (hit)
                        {
                            _result.SlamHits++;
                            _harmIsSlam = true;
                            _hero.ApplyDamage(new DamageContext(_heroHealth.Maximum * WardenRule.HeroShare, boss));
                            _harmIsSlam = false;
                        }
                        if (_trace != null)
                            Emit($"slam_blow hit={hit} enraged={_slamEnraged} hp={N(_heroHealth.Current)}");
                        if (_levers.WardenEchoTelegraph > 0f && _heroHealth.IsAlive)
                            Mark(HeroX, HeroY, WardenRule.SlamRadius, _levers.WardenEchoTelegraph, _levers.WardenEchoShare,
                                "Warden echo", boss.X, boss.Y);
                        break;
                }
            }

            // ---------------------------------------------------------------------------------------------------------
            // Research 2026-11-04: the structural candidates' hazards (FoundryLevers), simulation only
            // ---------------------------------------------------------------------------------------------------------

            private void Mark(float x, float y, float radius, float telegraph, float share, string harm, float sourceX,
                float sourceY)
            {
                _marks.Add(new HazardMark
                {
                    X = x, Y = y, Radius = radius, Onset = _seconds, LandsAt = _seconds + telegraph, Share = share,
                    Harm = harm, SourceX = sourceX, SourceY = sourceY
                });
                _result.HazardMarks++;
                if (_trace != null)
                    Emit($"hazard_mark {harm} x={N(x)} y={N(y)} hp={N(_heroHealth.Current)}");
            }

            // The hazards' clocks and their marks landing, stepped beside the Warden's slam: paused while a choice is open
            // or the reveal holds, gone with the fight that put them down (an echo with its Warden; slag and a stomp once
            // nothing stands, or the Warden walks out). The slag's clock runs while a wave or the horde stands outside the
            // Warden's fight - the halls, the elite rooms and the wait at the gate: a node wave alone lasts a few seconds.
            private void StepHazards()
            {
                if (_run.HasEnded || _choices.IsOpen || _revealHold)
                    return;
                bool warden = _slamBoss != null && _slamBoss.IsAlive;
                bool node = _waveAlive + _hordeAlive > 0 && !warden;
                for (int i = _marks.Count - 1; i >= 0; i--)
                {
                    HazardMark mark = _marks[i];
                    bool echo = mark.Harm == "Warden echo";
                    if (echo ? !warden : !node)
                    {
                        _marks.RemoveAt(i);
                        continue;
                    }
                    if (_seconds < mark.LandsAt)
                        continue;
                    _marks.RemoveAt(i);
                    bool hit = _heroHealth.IsAlive &&
                               ArenaFloor.FloorDistanceSquared(HeroX, HeroY, mark.X, mark.Y) <= mark.Radius * mark.Radius;
                    if (!hit)
                        continue;
                    _result.HazardHits++;
                    _hazardHarm = mark.Harm;
                    _harmIsSlam = false;
                    _hero.ApplyDamage(new DamageContext(_heroHealth.Maximum * mark.Share));
                    _hazardHarm = null;
                }
                if (!node || !_heroHealth.IsAlive)
                {
                    _slagClock = 0f;
                    return;
                }
                if (_levers.SlagEvery > 0f)
                {
                    _slagClock += Step;
                    if (_slagClock >= _levers.SlagEvery)
                    {
                        _slagClock = 0f;
                        Mark(HeroX, HeroY, _levers.SlagRadius, _levers.SlagTelegraph, _levers.SlagShare, "slag", HeroX, HeroY);
                    }
                }
                if (_levers.EliteStompEvery > 0f && _waveAlive > 0 && _roomIndex >= 0 &&
                    _floor.Kinds[_roomIndex] == RoomKind.Elite && _wave.Count > 0)
                {
                    Foe elite = _wave[0];
                    if (IsValid(elite))
                    {
                        // The first stomp comes a second after the elite walks in: a Captain often falls inside three.
                        if (float.IsNaN(elite.StompClock))
                            elite.StompClock = _levers.EliteStompEvery - 1f;
                        elite.StompClock += Step;
                        float reach = _levers.EliteStompReach;
                        if (elite.StompClock >= _levers.EliteStompEvery &&
                            ArenaFloor.FloorDistanceSquared(HeroX, HeroY, elite.X, elite.Y) <= reach * reach)
                        {
                            elite.StompClock = 0f;
                            Mark(elite.X, elite.Y, _levers.EliteStompRadius, _levers.EliteStompTelegraph,
                                _levers.EliteStompShare, "elite stomp", elite.X, elite.Y);
                        }
                    }
                }
            }

            // FoundryLevers.AmbushSurges: the surge's pack drawn in round a point AmbushDistance ahead of the hero's walk
            // (behind it, from the floor's far side, when it stands), each enemy at half its corner offset from the pack's
            // centre; an enemy whose spot is off the floor or too near the hero keeps its corner.
            private void Ambush(int count, float heroX, float heroY)
            {
                float dx = _steerX;
                float dy = _steerY;
                if (dx * dx + dy * dy < 1e-6f)
                {
                    dx = 0f;
                    dy = -1f;
                }
                Normalize(ref dx, ref dy);
                float cx = 0f, cy = 0f;
                for (int i = 0; i < count; i++)
                {
                    cx += _placements[i].X;
                    cy += _placements[i].Y;
                }
                cx /= Math.Max(1, count);
                cy /= Math.Max(1, count);
                float ax = heroX + dx * _levers.AmbushDistance;
                float ay = heroY + dy * _levers.AmbushDistance;
                float minimum = EntrySides.MinimumEntryDistance;
                for (int i = 0; i < count; i++)
                {
                    EntryPlacement p = _placements[i];
                    float x = ax + (p.X - cx) * .5f;
                    float y = ay + (p.Y - cy) * .5f;
                    if (!FoundryMap.Contains(x, y, HeroMotion.EdgeMargin) ||
                        ArenaFloor.FloorDistanceSquared(x, y, heroX, heroY) < minimum * minimum)
                        continue;
                    _placements[i] = new EntryPlacement(p.Side, p.Lateral, x, y);
                }
            }

            // ChestSpawner.Update: the hero walks onto the closed chest.
            private void StepChest()
            {
                if (!_chestPlaced || _chestOpen || !_heroHealth.IsAlive || _run.HasEnded ||
                    !ChestRule.IsWithinReach(HeroX, HeroY, _chestX, _chestY))
                    return;
                _chestOpen = true;
                _result.ChestsOpened++;
                if (_trace != null)
                    Emit($"chest_opened reward={_chestReward.Kind} book={_chestReward.HoldsBook} hp={N(_heroHealth.Current)}");
                if (_chestReward.Kind == ChestRewardKind.Heal)
                    _heroHealth.Heal(_heroHealth.Maximum * _chestReward.HealFraction);
                else
                    _run.AddGold(FloorScaling.Gold(_chestReward.Gold, _floor.ModifierGoldPercent));
                if (_chestReward.HoldsBook)
                    _upgrades.TryGrantBook(_chestIndex, out _, out _);
            }

            // ---------------------------------------------------------------------------------------------------------
            // DevelopmentAutopilot
            // ---------------------------------------------------------------------------------------------------------

            private void Pilot()
            {
                _doing = PilotDoing.Still;
                if (_run.HasEnded || _choices.IsOpen)
                {
                    _steerX = 0f;
                    _steerY = 0f;
                    return;
                }
                // A realistic pilot looking away holds the stick where it was (PilotRealism).
                if (_realism != null && LooksAway())
                    return;
                if (_levers.Cohort.UsesTheAbility)
                    PilotAbility();
                float x = HeroX;
                float y = HeroY;
                if (PilotGoal(x, y, out float gx, out float gy, out float stop))
                    PilotSteer(x, y, gx, gy, stop);
                else
                {
                    _steerX = 0f;
                    _steerY = 0f;
                }
            }

            // TryAbility: three enemies in reach - burning ones for a Vent - or any one beside the gate boss.
            private void PilotAbility()
            {
                if (!_ability.IsReady || !_heroHealth.IsAlive)
                    return;
                CollectNear(null, HeroX, HeroY, _ability.Radius, _abilityTargets);
                int count = 0;
                for (int i = 0; i < _abilityTargets.Count; i++)
                {
                    var enemy = (Foe)_abilityTargets[i];
                    if (!enemy.IsAlive)
                        continue;
                    if (_ability.Kind == AbilityKind.Vent && (enemy.Burn == null || !enemy.Burn.IsBurning))
                        continue;
                    count++;
                }
                bool boss = _slamBoss != null && _slamBoss.IsAlive;
                if (count < PilotAbilityCrowd && !(boss && count > 0))
                    return;
                _result.AbilityUses++;
                if (_trace != null)
                    Emit($"ability_used room={_roomIndex + 1} room_active={N(_seconds - _roomStartedAt)} in_reach={count}");
                _source = DamageSource.Ability;
                if (_ability.Kind == AbilityKind.Vent)
                    Vent();
                else
                    _ability.TryUse(_hero, _abilityTargets);
            }

            // AbilityController.Vent: every burning enemy in reach spends its stacks at once, then each eruption lands on
            // it and on everything within the splash of it.
            private void Vent()
            {
                if (!_ability.TrySpend())
                    return;
                var vented = new List<Foe>();
                var stacks = new List<int>();
                for (int i = 0; i < _abilityTargets.Count; i++)
                {
                    var enemy = (Foe)_abilityTargets[i];
                    if (!enemy.IsAlive || enemy.Burn == null || !enemy.Burn.IsBurning)
                        continue;
                    vented.Add(enemy);
                    stacks.Add(enemy.Burn.Consume());
                }
                for (int i = 0; i < vented.Count; i++)
                {
                    Foe centre = vented[i];
                    float damage = BurnRule.VentDamage(stacks[i], _ability.Damage);
                    CollectNear(centre, BurnRule.VentSplashRadius, _ventSplash);
                    if (centre.IsAlive)
                        centre.ApplyDamage(new DamageContext(damage, _hero));
                    for (int j = 0; j < _ventSplash.Count; j++)
                    {
                        IDamageable near = _ventSplash[j];
                        if (near != null && near.IsAlive)
                            near.ApplyDamage(new DamageContext(damage, _hero));
                    }
                }
            }

            // DevelopmentAutopilot.Goal, its cohort's habits read in the same order (AutopilotCohort).
            private bool PilotGoal(float x, float y, out float gx, out float gy, out float stop)
            {
                AutopilotCohort cohort = _levers.Cohort;
                stop = PilotArrived;
                gx = x;
                gy = y;
                bool fighting = _waveAlive > 0;
                // A fight, or a hall newly marked, gives the walk after it its whole drop budget.
                _detour.Track(fighting, _roomSiteOpen);
                // While the pit's band glows a cohort that keeps out of it steps into it only to leave the Warden's mark
                // (PilotSteer).
                bool glowing = _pitRunning && _pit.IsWarning;
                // A realistic pilot reads the glow a reaction after it began.
                if (glowing && _realism != null &&
                    !Perceived(_seconds - _pit.WarningProgress * PitEruptionRule.Warning, _realism.GlowReaction))
                    glowing = false;
                if (_realism != null && _realism.FoeReaction > 0f)
                    NoticeFoes(cohort, x, y);
                bool guarded = cohort.Guards(glowing);
                _bandGuard = guarded;
                if (_gateExitOpen)
                {
                    gx = _exitX;
                    gy = _exitY;
                    _doing = PilotDoing.Walk;
                    return true;
                }
                if (cohort.EscapesMarks && _slamBoss != null && _slamBoss.IsAlive && _slam.IsMarked &&
                    (_realism == null ||
                     Perceived(_seconds - _slam.TelegraphProgress * WardenRule.Telegraph, _realism.MarkReaction)))
                {
                    float mx = x - _markX;
                    float my = y - _markY;
                    float reach = WardenRule.SlamRadius + .6f;
                    if (mx * mx + my * my < reach * reach &&
                        cohort.EscapeMark(x, y, _markX, _markY, reach + .4f, _slamBoss.X, _slamBoss.Y, glowing, out gx, out gy))
                    {
                        if (_realism != null && _realism.AimError > 0f)
                        {
                            if (_markAimFor != _result.Slams)
                            {
                                _markAimFor = _result.Slams;
                                _dice.Disc(_realism.AimError, out _markAimX, out _markAimY);
                            }
                            Aim(ref gx, ref gy, _markAimX, _markAimY);
                        }
                        stop = 0f;
                        _bandGuard = false;
                        _result.EscapeSeconds += Step;
                        _doing = PilotDoing.Walk;
                        return true;
                    }
                }
                if (cohort.EscapesMarks && _marks.Count > 0 && EscapeHazard(cohort, x, y, glowing, out gx, out gy))
                {
                    stop = 0f;
                    _bandGuard = false;
                    _result.EscapeSeconds += Step;
                    _doing = PilotDoing.Walk;
                    return true;
                }
                if (cohort.Shelters && glowing && PitEruptionRule.InBand(x, y))
                {
                    cohort.Shelter(x, y, PilotShelterDepth, out gx, out gy);
                    if (_realism != null && _realism.AimError > 0f)
                    {
                        if (_shelterAimFor != _result.Eruptions)
                        {
                            _shelterAimFor = _result.Eruptions;
                            _dice.Disc(_realism.AimError, out _shelterAimX, out _shelterAimY);
                        }
                        Aim(ref gx, ref gy, _shelterAimX, _shelterAimY);
                    }
                    _result.ShelterSeconds += Step;
                    _doing = PilotDoing.Walk;
                    return true;
                }
                // DevelopmentAutopilot's step-in (AutopilotCohort.Habit.StepsIn, FootworkRule.StepIn): a single elite room's
                // Captain - or the gate boss, with StepsInOnTheGateBoss - held at the best weapon's reach, when that weapon is
                // short and the pilot safe.
                if (cohort.StepsIn && StepInGoal(x, y, glowing, out gx, out gy))
                {
                    if (_realism != null && _realism.AimError > 0f)
                    {
                        if (_seconds >= _kiteAimUntil)
                        {
                            _kiteAimUntil = _seconds + _realism.KiteAimSeconds;
                            _dice.Disc(_realism.AimError, out _kiteAimX, out _kiteAimY);
                        }
                        Aim(ref gx, ref gy, _kiteAimX, _kiteAimY);
                    }
                    stop = FootworkRule.StepInStop;
                    _result.StepInSeconds += Step;
                    _doing = PilotDoing.Fight;
                    return true;
                }
                if (cohort.Kites && Threat(cohort, x, y, out float tx, out float ty) &&
                    cohort.Kite(x, y, tx, ty, PilotKiteTurn, PilotKiteStride, glowing, out gx, out gy))
                {
                    if (_realism != null && _realism.AimError > 0f)
                    {
                        if (_seconds >= _kiteAimUntil)
                        {
                            _kiteAimUntil = _seconds + _realism.KiteAimSeconds;
                            _dice.Disc(_realism.AimError, out _kiteAimX, out _kiteAimY);
                        }
                        Aim(ref gx, ref gy, _kiteAimX, _kiteAimY);
                    }
                    stop = 0f;
                    _result.KiteSeconds += Step;
                    _doing = PilotDoing.Kite;
                    return true;
                }
                // A node fight is drawn to the pit's rim, but not while a guarded band glows: then the hero waits out of it.
                if (fighting)
                {
                    _doing = PilotDoing.Fight;
                    if (guarded || !cohort.Lures)
                        return false;
                    FootworkRule.RimSpot(x, y, PilotLureDepth, out gx, out gy);
                    _result.LureSeconds += Step;
                    return true;
                }
                // Between fights: the altars, then the marked hall, with the drops beside the way there (the slack, the
                // budget); with nowhere to go, every drop in sight.
                bool altar = NearestAltar(x, y, out float wx, out float wy);
                bool hall = !altar && _roomSiteOpen;
                if (hall)
                {
                    wx = _siteX;
                    wy = _siteY;
                }
                if (!altar && !hall)
                {
                    if (DropDetourRule.Nearest(_drops, x, y, PilotDropDetour, out gx, out gy))
                    {
                        stop = DropRule.PickupRadius * .5f;
                        _doing = PilotDoing.Drop;
                        return true;
                    }
                }
                else if (_detour.Choose(_drops, x, y, wx, wy, Step, out gx, out gy))
                {
                    stop = DropRule.PickupRadius * .5f;
                    _doing = PilotDoing.Drop;
                    return true;
                }
                gx = wx;
                gy = wy;
                if (altar)
                {
                    stop = .1f;
                    _doing = PilotDoing.Walk;
                    return true;
                }
                if (hall)
                {
                    stop = FoundryMap.RoomSiteRadius * .5f;
                    _doing = PilotDoing.Walk;
                    return true;
                }
                return false;
            }

            // DevelopmentAutopilot.StepInGoal: the gate boss while it stands, for a cohort that steps in on it, else the elite
            // room's Captain while its wave stands (EncounterController.EliteLeader), against the loadout's best weapon on
            // it; marked while the Warden's mark - or a research hazard's - is down.
            private bool StepInGoal(float x, float y, bool glowing, out float gx, out float gy)
            {
                gx = x;
                gy = y;
                Foe target = _levers.Cohort.StepsInOnTheGateBoss ? GateBoss : null;
                bool boss = target != null;
                if (!boss && _eliteWave && _wave.Count > 0 && !_wave[0].Defeated)
                    target = _wave[0];
                if (target == null || !target.IsAlive)
                    return false;
                bool marked = (_slamBoss != null && _slamBoss.IsAlive && _slam.IsMarked) || _marks.Count > 0;
                return FootworkRule.StepIn(x, y, target.X, target.Y, FootworkRule.BestSingleTargetReach(_loadout, boss), marked,
                    glowing, _heroHealth.Current, _heroHealth.Maximum, out gx, out gy);
            }

            private bool NearestAltar(float x, float y, out float gx, out float gy)
            {
                gx = x;
                gy = y;
                float best = float.PositiveInfinity;
                for (int i = 0; i < _altars.Count; i++)
                {
                    if (_altars.IsUsed(i))
                        continue;
                    float d = ArenaFloor.FloorDistanceSquared(x, y, _altars.XOf(i), _altars.YOf(i));
                    if (d >= best)
                        continue;
                    best = d;
                    gx = _altars.XOf(i);
                    gy = _altars.YOf(i);
                }
                return !float.IsPositiveInfinity(best);
            }

            // UnityEngine.Vector2.Normalize: divided by its magnitude, or zero below Vector2.kEpsilon.
            private static void Normalize(ref float x, ref float y)
            {
                float magnitude = (float)Math.Sqrt(x * x + y * y);
                if (magnitude > 1e-5f)
                {
                    x /= magnitude;
                    y /= magnitude;
                }
                else
                {
                    x = 0f;
                    y = 0f;
                }
            }

            // The push of every living enemy within its kite reach (the cohort's AutopilotCohort.ThreatReach: the kite
            // reach, or its own reach and a margin), as a unit direction away from them.
            private bool Threat(AutopilotCohort cohort, float x, float y, out float tx, out float ty)
            {
                tx = ty = 0f;
                Foe gate = GateBoss;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe enemy = TargetAt(i);
                    if (!IsValid(enemy))
                        continue;
                    float px = enemy.X;
                    float py = enemy.Y;
                    float d = ArenaFloor.FloorDistanceSquared(x, y, px, py);
                    float reach = cohort.ThreatReach(PilotKiteReach, enemy.Definition.Reach, ReferenceEquals(enemy, gate));
                    if (d >= reach * reach || d < 1e-6f)
                        continue;
                    if (_realism != null && !Perceived(float.IsNaN(enemy.NoticedAt) ? _seconds : enemy.NoticedAt,
                            _realism.FoeReaction))
                        continue;
                    tx += (x - px) / d;
                    ty += (y - py) / d;
                }
                float length = (float)Math.Sqrt(tx * tx + ty * ty);
                if (length < 1e-4f)
                    return false;
                tx /= length;
                ty /= length;
                return true;
            }

            // Steer: stop within the stop distance; otherwise straight at the goal, round the work islands and the pit by
            // the foundry's pursuit graph when the straight line is blocked, at full strength.
            private void PilotSteer(float x, float y, float gx, float gy, float stop)
            {
                if (ArenaFloor.FloorDistanceSquared(x, y, gx, gy) <= stop * stop)
                {
                    _steerX = 0f;
                    _steerY = 0f;
                    return;
                }
                float tx = gx;
                float ty = gy;
                if (!FoundryMap.Segment(x, y, gx, gy, .6f))
                    _route.Target(x, y, gx, gy, ref tx, ref ty);
                float steerX = tx - x;
                float steerY = ty - y;
                if (steerX * steerX + steerY * steerY < 1e-6f || (_bandGuard && FootworkRule.StepsIntoBand(x, y, tx, ty)))
                {
                    _steerX = 0f;
                    _steerY = 0f;
                    return;
                }
                Normalize(ref steerX, ref steerY);
                _steerX = steerX;
                _steerY = steerY;
            }

            // The autopilot answers every open choice: the forge's first option, and an offer by its card score. The frame
            // after, the hero stands, as the stick was released while the panel was open.
            private void AnswerChoices()
            {
                bool answered = false;
                for (int guard = 0; guard < 64 && _choices.IsOpen && !_run.HasEnded; guard++)
                {
                    int slot = ChoiceSlot();
                    _choices.TrySelect(_choices.Current, slot);
                    answered = true;
                }
                if (!answered)
                    return;
                _steerX = 0f;
                _steerY = 0f;
                // A realistic pilot reads the floor again after the panel closes, the stick let go.
                if (_realism != null && _realism.CardLapseSeconds > 0f)
                    _lapseUntil = Math.Max(_lapseUntil, _seconds + _realism.CardLapseSeconds);
            }

            // ---------------------------------------------------------------------------------------------------------
            // Research 2026-11-04: the realistic pilot (PilotRealism), simulation only
            // ---------------------------------------------------------------------------------------------------------

            // Whether a threat that appeared at onset has been seen long enough to be answered: a reaction after it, or
            // after the end of the lapse it appeared in.
            private bool Perceived(float onset, float reaction) =>
                _seconds - Math.Max(onset, _lapseUntil) >= reaction - 1e-4f;

            // A lapse due starts now; true while one lasts (a card's included), the stick held.
            private bool LooksAway()
            {
                if (_realism.Lapses && _seconds >= _nextLapseAt)
                {
                    _lapseUntil = Math.Max(_lapseUntil, _seconds + _realism.LapseSeconds);
                    _nextLapseAt = _lapseUntil + _dice.Wait(_realism.LapsesPerMinute);
                    _result.Lapses++;
                }
                return _seconds < _lapseUntil;
            }

            // Each enemy within the cohort's kite reach is noticed when it comes in - or when the pilot looks back - and
            // forgotten once it has stayed out of it FoeMemory seconds.
            private void NoticeFoes(AutopilotCohort cohort, float x, float y)
            {
                Foe gate = GateBoss;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe enemy = TargetAt(i);
                    if (!IsValid(enemy))
                        continue;
                    float reach = cohort.ThreatReach(PilotKiteReach, enemy.Definition.Reach, ReferenceEquals(enemy, gate));
                    bool within = ArenaFloor.FloorDistanceSquared(x, y, enemy.X, enemy.Y) < reach * reach;
                    if (within)
                    {
                        if (float.IsNaN(enemy.NoticedAt))
                            enemy.NoticedAt = _seconds;
                        enemy.LostAt = float.NaN;
                    }
                    else if (!float.IsNaN(enemy.NoticedAt))
                    {
                        if (float.IsNaN(enemy.LostAt))
                            enemy.LostAt = _seconds;
                        else if (_seconds - enemy.LostAt > _realism.FoeMemory)
                            enemy.NoticedAt = float.NaN;
                    }
                }
            }

            // The misplaced step: the goal moved by the drawn offset, where the hero can still stand there.
            private static void Aim(ref float gx, ref float gy, float ox, float oy)
            {
                if (!FoundryMap.Contains(gx + ox, gy + oy, HeroMotion.EdgeMargin))
                    return;
                gx += ox;
                gy += oy;
            }

            // Out of the hazard mark the hero stands in (within its radius and .6) that lands first, once seen: as the
            // Warden's mark, away from where it came from (FootworkRule.EscapeMark; the slag's own centre gives the
            // nearest way out).
            private bool EscapeHazard(AutopilotCohort cohort, float x, float y, bool glowing, out float gx, out float gy)
            {
                gx = x;
                gy = y;
                HazardMark next = null;
                for (int i = 0; i < _marks.Count; i++)
                {
                    HazardMark mark = _marks[i];
                    float reach = mark.Radius + .6f;
                    if (ArenaFloor.FloorDistanceSquared(x, y, mark.X, mark.Y) >= reach * reach)
                        continue;
                    if (_realism != null && !Perceived(mark.Onset, _realism.MarkReaction))
                        continue;
                    if (next == null || mark.LandsAt < next.LandsAt)
                        next = mark;
                }
                if (next == null)
                    return false;
                if (!cohort.EscapeMark(x, y, next.X, next.Y, next.Radius + 1f, next.SourceX, next.SourceY, glowing, out gx,
                        out gy))
                    return false;
                if (_realism != null && _realism.AimError > 0f)
                {
                    if (!next.Aimed)
                    {
                        next.Aimed = true;
                        _dice.Disc(_realism.AimError, out next.AimX, out next.AimY);
                    }
                    Aim(ref gx, ref gy, next.AimX, next.AimY);
                }
                return true;
            }

            private void OnOfferChanged()
            {
                UpgradeOffer offer = _upgrades.CurrentOffer;
                if (offer == null)
                    return;
                _result.Offers++;
                for (int i = 0; i < offer.Choices.Count; i++)
                    _result.OfferedByCategory[(int)offer.Choices[i].Category]++;
                if (_trace != null)
                {
                    var ids = new List<string>();
                    var rarities = new List<string>();
                    for (int i = 0; i < offer.Choices.Count; i++)
                    {
                        ids.Add(offer.Choices[i].Id);
                        rarities.Add(offer.RarityAt(i).ToString());
                    }
                    Emit($"upgrade_offered {string.Join(",", ids)} level={_run.Level} {string.Join(",", rarities)}");
                }
            }

            private int ChoiceSlot()
            {
                UpgradeOffer offer = _upgrades.CurrentOffer;
                if (_choices.Current.Kind != ChoiceKind.Upgrade || offer == null)
                    return 0;
                // DevelopmentAutopilot.ChoiceSlot: the cohort's pick (AutopilotCohort.Pick, the CardPicker order or the
                // first card), health first when the hero is under half.
                FoundryPick logged = null;
                if (_result.PickLog != null || _levers.Picker != null)
                    logged = new FoundryPick
                    {
                        Seconds = FloorTime, Level = _run.Level, Room = _roomIndex + 1, AtWarden = GateBoss != null,
                        Health = _heroHealth.Current, Maximum = _heroHealth.Maximum, Armor = _heroStats.Armor,
                        WeaponsHeld = _upgrades.Loadout.Count, TomesHeld = _upgrades.TomesHeld, Offer = offer
                    };
                int best = _levers.Picker != null && _levers.Cohort.Has(AutopilotCohort.Habit.ScoresCards)
                    ? _levers.Picker(logged, _upgrades)
                    : _levers.Cohort.Pick(offer, _heroHealth.Current < _heroHealth.Maximum * .5f, _upgrades);
                if (_result.PickLog != null)
                {
                    logged.Taken = best;
                    _result.PickLog.Add(logged);
                }
                UpgradeOption pick = offer.Choices[best];
                if (_trace != null)
                    Emit($"upgrade_selected {pick.Id} level={_run.Level} hp={N(_heroHealth.Current)}/{N(_heroHealth.Maximum)}");
                _result.Picks.Add(pick.Id);
                _result.PicksByCategory[(int)pick.Category]++;
                return best;
            }

            // ---------------------------------------------------------------------------------------------------------
            // The measurement
            // ---------------------------------------------------------------------------------------------------------

            private int Bucket() => Math.Min(_result.BucketDamage.Length - 1, (int)(FloorTime / FoundryBucketSeconds));

            // HordePacingMeasurement's per-frame reading, after the frame.
            private void Sample()
            {
                int alive = _waveAlive + _hordeAlive;
                if (alive > 0)
                    _fighting += Step;
                bool engaged = _seconds - _lastHeroHit <= EngagedSeconds || _seconds - _lastStrike <= EngagedSeconds;
                if (engaged)
                    _engaged += Step;
                if (_result.Engagement != null)
                    CountEngagement(alive);
                if (_waitFrame)
                {
                    _waitFrame = false;
                    if (engaged)
                        _result.WaitEngaged += Step;
                    if (_doing == PilotDoing.Kite)
                        _result.WaitKite += Step;
                    else if (_doing == PilotDoing.Drop)
                        _result.WaitDrop += Step;
                    else if (_doing == PilotDoing.Fight)
                        _result.WaitFight += Step;
                    else if (_doing == PilotDoing.Walk)
                        _result.WaitWalk += Step;
                    else
                        _result.WaitStill += Step;
                }
                if (_eliteWave && _waveAlive > 0 && _heroHealth.IsAlive)
                    _result.EliteFought += Step;
                Foe warden = GateBoss;
                // What is left of the Warden as the frame ends: its clock is stepped before the frame's blows land on it.
                if (warden != null)
                    _result.BossLeft = warden.Health.Current / warden.Health.Maximum;
                if (warden != null && !_revealHold && _heroHealth.IsAlive)
                {
                    _result.WardenFought += Step;
                    float reach = 0f;
                    for (int i = 0; i < _loadout.Count; i++)
                        reach = Math.Max(reach, _loadout.At(i).Runtime.Range);
                    if (ArenaFloor.FloorDistanceSquared(warden.X, warden.Y, HeroX, HeroY) <= reach * reach)
                        _result.WardenInReach += Step;
                }
                // weapon_effectiveness: a weapon picked up this frame is held from now, and an evolution taken this frame
                // counts from now.
                if (_loadout.Count > 0)
                    WorkAt(_loadout.Count - 1);
                for (int i = 0; i < _result.WeaponWork.Count; i++)
                {
                    FoundryWeaponWork work = _result.WeaponWork[i];
                    if (work.EvolvedAt < 0f && _upgrades.HasEvolved(work.Id))
                        work.EvolvedAt = FloorTime;
                }
                _hordeAliveSum += _hordeAlive;
                _hordeTargetSum += _horde != null ? _horde.TargetNow : 0;
                _hordeFrames++;
                if (_hordeAlive > _result.HordeMaxAlive)
                    _result.HordeMaxAlive = _hordeAlive;
                if (_horde != null)
                    _result.Clock = Math.Max(_result.Clock, _horde.Seconds);
                int bucket = Bucket();
                _result.BucketAlive[bucket] += alive * Step;
                _result.BucketLevel[bucket] = _run.Level;
                _result.Buckets = bucket + 1;
                // Every frame's state, for laying a trace beside the Editor's horde_sample lines by their clock.
                if (_trace != null)
                    Emit($"frame clock={(_horde != null ? N(_horde.Seconds) : "-")} horde={_hordeAlive} target={(_horde != null ? _horde.TargetNow : 0)} " +
                         $"wave={_waveAlive} hp={N(_heroHealth.Current)} x={N(HeroX)} y={N(HeroY)} drops={_drops.Count} xp={_run.Experience}");
            }

            // FoundryEngagement (research only): this frame's phase, and each reading of engaged on it.
            private void CountEngagement(int alive)
            {
                FoundryEngagement e = _result.Engagement;
                int phase = _gateExitOpen ? FoundryEngagement.Exit
                    : _gateBossWave && (_waveAlive > 0 || _revealHold) ? FoundryEngagement.Warden
                    : _waitFrame ? FoundryEngagement.Wait
                    : _roomSiteOpen ? FoundryEngagement.Walk
                    : _waveAlive > 0 ? FoundryEngagement.Node
                    : FoundryEngagement.Between;
                e.Seconds[phase] += Step;
                bool hit = _seconds - _lastHeroHit <= EngagedSeconds;
                float first = _loadout.Primary.Range;
                float longest = 0f;
                for (int i = 0; i < _loadout.Count; i++)
                    longest = Math.Max(longest, _loadout.At(i).Runtime.Range);
                e.FirstReach = first;
                e.LongestReach = longest;
                float nearest = float.PositiveInfinity;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (IsValid(foe))
                        nearest = Math.Min(nearest, ArenaFloor.FloorDistanceSquared(foe.X, foe.Y, HeroX, HeroY));
                }
                nearest = (float)Math.Sqrt(nearest);
                bool before = hit || nearest <= first + EngagedMargin;
                bool[] rules =
                {
                    before,
                    hit || nearest <= longest + EngagedMargin,
                    hit || nearest <= _heroDefinition.Weapon.Range + EngagedMargin,
                    hit || nearest <= longest,
                    hit || _seconds - _lastStrike <= EngagedSeconds,
                    alive > 0,
                    nearest <= first + EngagedMargin,
                    hit
                };
                for (int r = 0; r < rules.Length; r++)
                    if (rules[r])
                        e.Engaged[r, phase] += Step;
                if (before)
                    return;
                if (alive == 0)
                {
                    e.Outside[0] += Step;
                    return;
                }
                for (int d = 0; d < FoundryEngagement.Distances.Length; d++)
                    if (nearest <= FoundryEngagement.Distances[d])
                    {
                        e.Outside[d + 1] += Step;
                        return;
                    }
            }

            private FoundryResult Finish()
            {
                if (_roomIndex >= 0 && _result.RoomSeconds[_roomIndex] < 0f)
                    _result.RoomSeconds[_roomIndex] = _seconds - _roomStartedAt;
                // A floor that ended on the way to a hall walked for as long as the hall stood marked.
                if (_roomSiteOpen && _result.WalkSeconds[_siteStep.RoomIndex] < 0f)
                    _result.WalkSeconds[_siteStep.RoomIndex] = _siteWait;
                _result.Outcome = _cleared ? FoundryOutcome.Cleared
                    : !_heroHealth.IsAlive ? FoundryOutcome.Died : FoundryOutcome.Timeout;
                float floorTime = FloorTime;
                _result.FloorSeconds = floorTime;
                if (_bossFellAt >= 0f && _cleared)
                    _result.ExitSeconds = _seconds - _bossFellAt;
                _result.Level = _run.Level;
                _result.HeroHealth = _heroHealth.Current;
                _result.HeroMaximum = _heroHealth.Maximum;
                _result.Uptime = floorTime > 0f ? _fighting / floorTime : 0f;
                _result.Engaged = floorTime > 0f ? _engaged / floorTime : 0f;
                _result.HordeMeanAlive = _hordeFrames > 0 ? (float)(_hordeAliveSum / _hordeFrames) : 0f;
                _result.HordeMeanTarget = _hordeFrames > 0 ? (float)(_hordeTargetSum / _hordeFrames) : 0f;
                _result.Evolutions = _upgrades.EvolutionsTaken;
                _result.Difficulty = _heroStats.Difficulty;
                var weapons = new List<string>();
                for (int i = 0; i < _loadout.Count; i++)
                    weapons.Add(_loadout.At(i).WeaponId.Replace("weapon_", string.Empty) + " " + _loadout.At(i).Level);
                _result.Weapons = string.Join(", ", weapons);
                var tomes = new List<string>();
                foreach (UpgradeOption tome in _upgrades.Tomes)
                    if (_upgrades.TomeLevel(tome.Id) > 0)
                        tomes.Add(tome.Id.Replace("tome_", string.Empty) + " " + _upgrades.TomeLevel(tome.Id));
                _result.Tomes = string.Join(", ", tomes);
                EndWait();
                if (_result.Outcome == FoundryOutcome.Died && _result.DeathRoom < 0)
                    _result.DeathRoom = _roomIndex + 1;
                return _result;
            }

            private static float[] Filled(int length, float value)
            {
                var array = new float[length];
                for (int i = 0; i < length; i++)
                    array[i] = value;
                return array;
            }
        }
    }
}
