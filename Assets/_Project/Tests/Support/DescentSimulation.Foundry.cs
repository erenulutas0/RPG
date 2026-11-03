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
    // runs the forge's transition and the advance delay; then the default-order scripts: the hero's AttackController (every
    // weapon in slot order, Ember Memory on each strike), its AbilityController's cooldown, every enemy's AttackController,
    // the burns, AltarService, PitEruptionService, WardenSlamService, the chest, and DevelopmentAutopilot, which fires the
    // ability and chooses next frame's steer. An offer that opens is answered at the frame's end with the autopilot's own
    // card score, and the frame after it the hero stands, as the scene's autopilot releases the stick while a panel is
    // open. The boss's arrival holds the encounter for BossRevealView's turn, hold and turn back.
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
        // The lead's engaged uptime (HordePacingMeasurement, 2026-10-29): an enemy within the first weapon's reach and
        // this much more, or a hit taken this recently.
        public const float EngagedMargin = 1f;
        public const float EngagedAfterHitSeconds = 1.5f;
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
        }

        // Data/Floors/Floor_EmberHalls.asset as the foundry plays it: the Descent's six rooms (EmberHalls, which the floor
        // tests already hold to the asset), climb-scaled from floor 1, its horde's three roles and no hall surges.
        public static FoundryFloor FoundryFloorOne() => new FoundryFloor
        {
            Id = "floor_ember_halls",
            Rooms = EmberHalls.Rooms,
            Kinds = new[] { RoomKind.Combat, RoomKind.Combat, RoomKind.Combat, RoomKind.Forge, RoomKind.Elite, RoomKind.Boss },
            HordeRoles = new[] { Mite, Grunt, Runner }
        };

        // The floor's knobs, as parameters of a run rather than edits of the game: each default is the shipped value, read
        // from the game's own constant where there is one, so a run with no levers is the scene's floor.
        internal sealed class FoundryLevers
        {
            public string Name = "shipped";
            // FloorDefinition.FoundryHallSurges: surge waves each combat hall gains after its authored ones (HallSurgeRule).
            public int HallSurges = 2;
            // More copies of the floor's elite room before the gate; the district round gives the first the north court.
            public int ExtraElites = 1;
            // More combat halls after the floor's last, copies of its second and third halls in turn.
            public int ExtraHalls;
            // The gate boss's health against its authored (WardenRule.GateHealthShare).
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
            public float LimitSeconds = FoundryFloorLimitSeconds;

            public bool TunesDirector => HordeTargets != null || HordeFillRate != HordeRule.MaxSpawnsPerSecond;

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

        // One enemy's life, for the observed TTK: its kind, whether it was the horde's, from walking in to falling, and
        // from the first damage it took to falling (the time it stood under fire).
        internal readonly struct FoundryKill
        {
            public readonly string Kind;
            public readonly bool Horde;
            public readonly float Lifetime;
            public readonly float UnderFire;

            public FoundryKill(string kind, bool horde, float lifetime, float underFire)
            {
                Kind = kind;
                Horde = horde;
                Lifetime = lifetime;
                UnderFire = underFire;
            }
        }

        // What one foundry floor did, in the Editor measurement's terms (HordePacingMeasurement and the telemetry's
        // room_start, hall_reached, wave_clear and boss lines) and the report's (docs/41 step 5).
        internal sealed class FoundryResult
        {
            public int Seed;
            public string Hero;
            public string Levers;
            public FoundryOutcome Outcome;
            // Active seconds from the floor's first frame to the gate held, a death or the limit.
            public float FloorSeconds;
            // Per room, by index: active seconds from the room's start to the next room's (or the floor's end), the walk to
            // its district (hall_reached), and its node-wave seconds; -1 for a room never entered.
            public float[] RoomSeconds;
            public float[] WalkSeconds;
            public float[] WaveSeconds;
            public RoomKind[] Kinds;
            // The Warden: its fight from walking out of the gate to falling, its health, what was left of it at the end,
            // and the walk from its fall to the gate held. -1 where the floor never got there.
            public float BossSeconds = -1f;
            public float WardenHealth;
            public float BossLeft = -1f;
            public float ExitSeconds = -1f;
            public float GateWaitSeconds;
            // The level as the Warden walked out, and at the end after the floor's sweep (the measurement's "level").
            public int LevelAtGate = -1;
            public int Level;
            public float HeroHealth;
            public float HeroMaximum;
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
            public int AltarsTaken;
            public int ChestsOpened;
            public int DropsCollected;
            public int DropsSwept;
            // Where a death happened: its room (1-based), the phase (walk, wave, forge, boss, exit) and what dealt the blow.
            public int DeathRoom = -1;
            public string DeathPhase;
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
            // the fight from the horde and the pit (the Warden's own blows are TakenFromWarden and TakenFromSlams).
            public float WardenFromWeapons;
            public float WardenFromBurn;
            public float WardenFromAbility;
            public float WardenFromPit;
            public float WardenFought;
            public float WardenInReach;
            public float HeroHealthAtGate = -1f;
            public float HeroMaximumAtGate = -1f;
            public float FightTakenFromHorde;
            public float FightTakenFromPit;
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
            // docs/40's time_bucket_summary, per FoundryBucketSeconds of active time: damage dealt, mean enemies standing,
            // kills, and the level at the bucket's end.
            public float[] BucketDamage;
            public float[] BucketAlive;
            public int[] BucketKills;
            public int[] BucketLevel;
            public int Buckets;

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

        // One foundry floor for this hero on this seed. Null levers and floor are the scene's floor 1. trace, when given,
        // hears the run as the scene's telemetry would write it - one line an event, in the telemetry's own names and
        // fields - so a seed can be laid beside the Editor's copy of its events and the first difference found.
        public static FoundryResult RunFoundry(FoundryHero hero, int seed, FoundryLevers levers = null,
            FoundryFloor floor = null, Action<string> trace = null) =>
            new FoundryRun(hero ?? Vanguard(), Shape(floor ?? FoundryFloorOne(), levers ?? new FoundryLevers()),
                levers ?? new FoundryLevers(), seed, trace).Play();

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
            return new FoundryFloor
            {
                Id = floor.Id, Rooms = rooms.ToArray(), Kinds = kinds.ToArray(), HordeRoles = floor.HordeRoles,
                FloorNumber = floor.FloorNumber
            };
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

        // The horde's director: the game's HordeDirector, or the tuned copy when a lever changes what it reads as constants.
        private sealed class HordeDriver
        {
            private readonly HordeDirector _shipped;
            private readonly TunedHordeDirector _tuned;

            public HordeDriver(FoundryLevers levers, int roles, int seed)
            {
                if (levers.TunesDirector)
                    _tuned = new TunedHordeDirector(HordeRule.SceneCap, roles, seed, levers.HordeTargets,
                        levers.HordeFillRate / HordeRule.MaxSpawnsPerSecond);
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
            private readonly FoundryFloor _floor;
            private readonly FoundryLevers _levers;
            private readonly FoundryResult _result;

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
            private readonly FloorProgress _progress;
            private readonly EncounterProgress _fight = new EncounterProgress(AdvanceDelay);
            private readonly int[] _wavesPerRoom;
            private bool _nonCombat;
            private float _transitionWait;
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

            // The one chest, as ChestSpawner keeps it.
            private int _chestIndex = -1;
            private int _chestRoom = -1;
            private bool _chestPlaced;
            private bool _chestOpen;
            private float _chestX;
            private float _chestY;
            private ChestReward _chestReward;

            private int _frame;
            private float _seconds;
            private float _lastHeroHit = float.NegativeInfinity;
            private DamageSource _source;
            private Foe _lastAttacker;
            private string _lastHarm;
            private double _hordeAliveSum;
            private double _hordeTargetSum;
            private int _hordeFrames;
            private float _fighting;
            private float _engaged;

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

            public FoundryRun(FoundryHero hero, FoundryFloor floor, FoundryLevers levers, int seed, Action<string> trace)
            {
                _trace = trace;
                _heroDefinition = hero;
                _floor = floor;
                _levers = levers;
                _result = new FoundryResult
                {
                    Seed = seed, Hero = hero.Id, Levers = levers.Name, Kinds = floor.Kinds,
                    RoomSeconds = Filled(floor.Kinds.Length, -1f), WalkSeconds = Filled(floor.Kinds.Length, -1f),
                    WaveSeconds = Filled(floor.Kinds.Length, -1f)
                };
                int buckets = (int)Math.Ceiling(levers.LimitSeconds / FoundryBucketSeconds) + 1;
                _result.BucketDamage = new float[buckets];
                _result.BucketAlive = new float[buckets];
                _result.BucketKills = new int[buckets];
                _result.BucketLevel = new int[buckets];

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
                WeaponRuntime weapon = hero.Weapon.CreateCategoryRuntime();
                weapon.Seed = seed;
                _loadout = new WeaponLoadout(seed);
                _loadout.TryAdd(hero.Weapon.Id, weapon);
                HeroWeapon[] catalogue = WeaponCatalogue();
                var cards = new WeaponCard[catalogue.Length];
                for (int i = 0; i < cards.Length; i++)
                    cards[i] = catalogue[i].CreateCard();
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

                _wavesPerRoom = new int[floor.Rooms.Length];
                for (int i = 0; i < _wavesPerRoom.Length; i++)
                    _wavesPerRoom[i] = floor.Rooms[i] == null ? 0 : floor.Rooms[i].Length + SurgesIn(i);
                _progress = new FloorProgress(_wavesPerRoom);
            }


            private float HeroX => _heroMotion.X;
            private float HeroY => _heroMotion.Y;

            public FoundryResult Play()
            {
                // EncounterController.Initialize: the floor's first wave forms up round the hero before the first frame.
                AdvanceFloor();
                while (!_cleared && _heroHealth.IsAlive && _seconds < _levers.LimitSeconds)
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

                HeroAttacks();
                if (_heroHealth.IsAlive)
                    _ability.Tick(Step);
                EnemyAttacks();
                TickBurns();
                StepAltars();
                StepPit();
                StepWarden();
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
                if (_nonCombat)
                {
                    if (!canAdvance)
                        return;
                    _transitionWait += Step;
                    if (_transitionWait < AdvanceDelay)
                        return;
                    AdvanceFloor();
                    return;
                }

                if (_progress.IsComplete || !_fight.Tick(Step, canAdvance))
                    return;
                // FoundryLevers.GateNotBeforeSeconds: the floor waits at the room before the gate until its clock says so.
                if (_levers.GateNotBeforeSeconds > 0f && NextIsGate() && _horde != null &&
                    _horde.Seconds < _levers.GateNotBeforeSeconds)
                {
                    _result.GateWaitSeconds += Step;
                    return;
                }
                AdvanceFloor();
            }

            private void AdvanceFloor()
            {
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

            // EncounterController.TryRoomSite: a hall or elite after the floor's first opens at its own district.
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
                FoundryMap.RoomSite(0, ordinal, out x, out y);
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
                SpawnWave(WaveEnemies(roomIndex, waveIndex), boss, boss);
                _waveIndex = waveIndex;
                if (waveIndex == 0 && _hallRepeatWave < 0)
                    _hallStartedAt = _seconds;
            }

            // EncounterController.SpawnWave on the foundry.
            private void SpawnWave(List<Enemy> definitions, bool atGate, bool boss)
            {
                ReleaseWave();
                _nonCombat = false;
                int n = _floor.FloorNumber;
                float scale = _heroStats.Difficulty;
                float damageBonus = FloorScaling.DamageBonus(ClimbScaling.Damage(n) * scale, 0f);
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
                for (int i = 0; i < count; i++)
                {
                    Enemy definition = definitions[i];
                    float healthScale = floorHealth * scale * (atGate && i == 0 ? WardenHealthShare() : 1f);
                    if (!boss)
                        healthScale *= _levers.NodeHealthScale;
                    Foe foe = CreateFoe(definition, FloorScaling.Health(definition.Health, healthScale, 0f), damageBonus,
                        false);
                    foe.Gold = FloorScaling.Gold(definition.Gold, 0f);
                    foe.Experience = definition.Experience;
                    foe.Motion = _waveMotion;
                    foe.Slot = _waveMotion.Add(foe, _placements[i], definition.Speed, foe.Weapon.Range);
                    if (atGate)
                    {
                        _waveMotion.SetMarch(foe.Slot, GateMarchFactor, GateMarchRange);
                        foe.GateBoss = i == 0;
                    }
                    _wave.Add(foe);
                }
                _waveAlive = count;
                _waveOrdinal++;
                _fight.Begin();
                _waveStartedAt = _seconds;
                EncounterStarted();
                if (atGate)
                    GateBossArrived();
            }

            // WardenRule.GateHealthShare, or a lever's share, grown with the floor's clock where FoundryLevers.WardenClockFrom
            // asks for it.
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
                if (_chestRoom == _roomIndex)
                    return;
                _chestRoom = _roomIndex;
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
                // The room's options in the asset's order: Forge_Mend, Forge_Temper.
                _forge.Open(new[] { Mend(), Temper() });
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

            // EncounterController.TickHorde: the horde stands from the floor's first step to the boss's fall, and the forge
            // freezes it - from the forge's offer until the next hall's wave forms up, as IsInNonCombatRoom reads.
            private void TickHorde()
            {
                bool wanted = !_progress.IsComplete && !_gateExitOpen;
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
                    ClimbScaling.Damage(n) * scale * HordeRule.DamageShare * _horde.DamageMultiplier, 0f);
                Foe foe = CreateFoe(definition,
                    FloorScaling.Health(definition.Health,
                        ClimbScaling.Health(n) * scale * _levers.HordeHealthShare * _horde.HealthMultiplier, 0f),
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
                foe.Health.ApplyDamage(context);
                float dealt = before - foe.Health.Current;
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

            // EncounterController.OnEnemyDied / OnHordeEnemyDied, CombatSetup.OnEnemyDefeated's drop, then the enemy's
            // BurnStatus.OnDied - the order their Died handlers were subscribed in.
            private void FoeDied(Foe foe)
            {
                if (foe.Released || foe.Defeated)
                    return;
                foe.Defeated = true;
                _result.Kills.Add(new FoundryKill(foe.Definition.Name, foe.Horde, _seconds - foe.SpawnedAt,
                    foe.FirstHitAt >= 0f ? _seconds - foe.FirstHitAt : 0f));
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
                    _lastHarm = (foe.Horde ? "horde " : foe.GateBoss ? string.Empty : "wave ") + foe.Definition.Name;
                    if (foe.Horde && GateBoss != null)
                        _result.FightTakenFromHorde += taken;
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
                else
                {
                    _result.TakenFromPit += taken;
                    if (GateBoss != null)
                        _result.FightTakenFromPit += taken;
                    _lastHarm = "pit";
                }
                if (!_heroHealth.IsAlive && _result.DeathRoom < 0)
                    RecordDeath();
            }

            private bool _harmIsSlam;

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
                else if (_nonCombat)
                    _result.DeathPhase = "forge";
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
                    if (splashRadius > 0f)
                        CollectNear(target, splashRadius, _nearby);
                    _pulse.Clear();
                    if (weapon.PulseDamage > 0f)
                        CollectNear(null, HeroX, HeroY, weapon.PulseRadius, _pulse);
                    _crit.Clear();
                    if (weapon.CritSplash > 0f)
                        CollectNear(target, weapon.CritSplashRadius, _crit);
                    _source = DamageSource.Weapon;
                    if (!weapon.TryAttack(target, _hero, _nearby, _pulse, _crit, weapon.KillBurst > 0f ? this : null))
                        continue;
                    if (_result.FirstStrikeSeconds < 0f)
                        _result.FirstStrikeSeconds = _seconds;
                    if (_heroDefinition.EmberMemory)
                        Ignite(target, 1, false);
                }
            }

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
                // floor: its weapon neither ticks nor strikes meanwhile.
                if (ReferenceEquals(foe, _slamBoss) && _levers.WardenRecoverySeconds >= 0f &&
                    _slam.HoldsWeaponFor(_levers.WardenRecoverySeconds))
                    return;
                foe.Weapon.Tick(Step);
                if (!foe.Weapon.IsReady)
                    return;
                float reach = foe.Weapon.Range;
                if (ArenaFloor.FloorDistanceSquared(HeroX, HeroY, foe.X, foe.Y) > reach * reach)
                    return;
                _harmIsSlam = false;
                foe.Weapon.TryAttack(_hero, foe);
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
            // half health.
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
                _result.BossLeft = boss.Health.Current / boss.Health.Maximum;
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
                        break;
                    case WardenSlamClock.Step.Slammed:
                        _result.Slams++;
                        float distanceSquared = ArenaFloor.FloorDistanceSquared(HeroX, HeroY, _markX, _markY);
                        if (_heroHealth.IsAlive && distanceSquared <= WardenRule.SlamRadius * WardenRule.SlamRadius)
                        {
                            _result.SlamHits++;
                            _harmIsSlam = true;
                            _hero.ApplyDamage(new DamageContext(_heroHealth.Maximum * WardenRule.HeroShare, boss));
                            _harmIsSlam = false;
                        }
                        break;
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
                    _run.AddGold(FloorScaling.Gold(_chestReward.Gold, 0f));
                if (_chestReward.HoldsBook)
                    _upgrades.TryGrantBook(_chestIndex, out _, out _);
            }

            // ---------------------------------------------------------------------------------------------------------
            // DevelopmentAutopilot
            // ---------------------------------------------------------------------------------------------------------

            private void Pilot()
            {
                if (_run.HasEnded || _choices.IsOpen)
                {
                    _steerX = 0f;
                    _steerY = 0f;
                    return;
                }
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

            private bool PilotGoal(float x, float y, out float gx, out float gy, out float stop)
            {
                stop = PilotArrived;
                gx = x;
                gy = y;
                bool fighting = _waveAlive > 0;
                // A fight, or a hall newly marked, gives the walk after it its whole drop budget.
                _detour.Track(fighting, _roomSiteOpen);
                // While the pit's band glows the hero steps into it only to leave the Warden's mark (PilotSteer).
                bool glowing = _pitRunning && _pit.IsWarning;
                _bandGuard = glowing;
                if (_gateExitOpen)
                {
                    gx = _exitX;
                    gy = _exitY;
                    return true;
                }
                if (_slamBoss != null && _slamBoss.IsAlive && _slam.IsMarked)
                {
                    float mx = x - _markX;
                    float my = y - _markY;
                    float reach = WardenRule.SlamRadius + .6f;
                    if (mx * mx + my * my < reach * reach &&
                        FootworkRule.EscapeMark(x, y, _markX, _markY, reach + .4f, _slamBoss.X, _slamBoss.Y, glowing, out gx, out gy))
                    {
                        stop = 0f;
                        _bandGuard = false;
                        return true;
                    }
                }
                if (glowing && PitEruptionRule.InBand(x, y))
                {
                    FootworkRule.Shelter(x, y, PilotShelterDepth, out gx, out gy);
                    return true;
                }
                if (Threat(x, y, out float tx, out float ty) &&
                    FootworkRule.Kite(x, y, tx, ty, PilotKiteTurn, PilotKiteStride, glowing, out gx, out gy))
                {
                    stop = 0f;
                    return true;
                }
                // A node fight is drawn to the pit's rim, but not while the band glows: then the hero waits out of it.
                if (fighting)
                {
                    if (glowing)
                        return false;
                    FootworkRule.RimSpot(x, y, PilotLureDepth, out gx, out gy);
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
                        return true;
                    }
                }
                else if (_detour.Choose(_drops, x, y, wx, wy, Step, out gx, out gy))
                {
                    stop = DropRule.PickupRadius * .5f;
                    return true;
                }
                gx = wx;
                gy = wy;
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

            // The push of every living enemy within the kite reach - the gate boss's own reach and a little more for it -
            // as a unit direction away from them.
            private bool Threat(float x, float y, out float tx, out float ty)
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
                    float reach = PilotKiteReach;
                    if (ReferenceEquals(enemy, gate))
                        reach = Math.Max(reach, enemy.Definition.Reach + .3f);
                    if (d >= reach * reach || d < 1e-6f)
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
                int best = 0;
                int bestScore = int.MinValue;
                for (int i = 0; i < offer.Choices.Count; i++)
                {
                    int score = CardScore(offer.Choices[i]);
                    if (score <= bestScore)
                        continue;
                    best = i;
                    bestScore = score;
                }
                UpgradeOption pick = offer.Choices[best];
                if (_trace != null)
                    Emit($"upgrade_selected {pick.Id} level={_run.Level} hp={N(_heroHealth.Current)}/{N(_heroHealth.Maximum)}");
                _result.Picks.Add(pick.Id);
                _result.PicksByCategory[(int)pick.Category]++;
                return best;
            }

            // DevelopmentAutopilot.CardScore: health when low, then the weapons it holds, a second weapon, two tomes, their
            // levels, then anything else.
            private int CardScore(UpgradeOption card)
            {
                bool low = _heroHealth.Current < _heroHealth.Maximum * .5f;
                if (low && card.Stat == UpgradeStat.MaxHealth)
                    return 100;
                switch (card.Category)
                {
                    case OfferCategory.WeaponUpgrade: return 50;
                    case OfferCategory.NewWeapon: return _upgrades.Loadout.Count < 2 ? 45 : 10;
                    case OfferCategory.NewTome: return _upgrades.TomesHeld < 2 ? 40 : 15;
                    case OfferCategory.TomeUpgrade: return 30;
                    default: return card.Stat == UpgradeStat.MaxHealth ? 25 : 20;
                }
            }

            // ---------------------------------------------------------------------------------------------------------
            // The measurement
            // ---------------------------------------------------------------------------------------------------------

            private int Bucket() => Math.Min(_result.BucketDamage.Length - 1, (int)(_seconds / FoundryBucketSeconds));

            // HordePacingMeasurement's per-frame reading, after the frame.
            private void Sample()
            {
                int alive = _waveAlive + _hordeAlive;
                if (alive > 0)
                    _fighting += Step;
                if (_seconds - _lastHeroHit <= EngagedAfterHitSeconds || EnemyNear(_loadout.Primary.Range + EngagedMargin))
                    _engaged += Step;
                Foe warden = GateBoss;
                if (warden != null && !_revealHold && _heroHealth.IsAlive)
                {
                    _result.WardenFought += Step;
                    float reach = 0f;
                    for (int i = 0; i < _loadout.Count; i++)
                        reach = Math.Max(reach, _loadout.At(i).Runtime.Range);
                    if (ArenaFloor.FloorDistanceSquared(warden.X, warden.Y, HeroX, HeroY) <= reach * reach)
                        _result.WardenInReach += Step;
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

            private bool EnemyNear(float reach)
            {
                float reachSquared = reach * reach;
                for (int i = 0; i < TargetCount; i++)
                {
                    Foe foe = TargetAt(i);
                    if (IsValid(foe) && ArenaFloor.FloorDistanceSquared(foe.X, foe.Y, HeroX, HeroY) <= reachSquared)
                        return true;
                }
                return false;
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
                _result.FloorSeconds = _seconds;
                if (_bossFellAt >= 0f && _cleared)
                    _result.ExitSeconds = _seconds - _bossFellAt;
                _result.Level = _run.Level;
                _result.HeroHealth = _heroHealth.Current;
                _result.HeroMaximum = _heroHealth.Maximum;
                _result.Uptime = _seconds > 0f ? _fighting / _seconds : 0f;
                _result.Engaged = _seconds > 0f ? _engaged / _seconds : 0f;
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
