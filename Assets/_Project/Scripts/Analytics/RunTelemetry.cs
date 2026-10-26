using System;
using System.Collections.Generic;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;

namespace Cryptforge.Analytics
{
    // Records one run as schema v1 events from the gameplay events that already exist, so telemetry never needs a hook
    // inside combat. It only reads game state: every handler builds fields from values the services already expose and
    // hands them to the session, and nothing here can raise a gameplay event. The composition root creates it before its
    // own reactions subscribe, so a checkpoint choice, kill or floor clear is logged before the run ends or descends in
    // response. Kills and progress arrive through the Record/Observe calls; the scene forwards them from Unity components.
    // Every call is cheap and allocation-free unless an event is written, because progress arrives on every hit.
    public sealed class RunTelemetry : IDisposable
    {
        // How many seconds of the run's active time lie between two horde_sample lines.
        public const double HordeSampleSeconds = 5.0;

        private const string ResultCleared = "cleared";

        private enum TimeKind
        {
            Active,
            Choice,
            Pause
        }

        // Seconds split the way the report reads them: fighting and walking, choosing, and the player's own pause.
        private sealed class TimeSplit
        {
            public double Active;
            public double Choice;
            public double Pause;

            public void Add(TimeKind kind, double seconds)
            {
                switch (kind)
                {
                    case TimeKind.Pause:
                        Pause += seconds;
                        break;
                    case TimeKind.Choice:
                        Choice += seconds;
                        break;
                    default:
                        Active += seconds;
                        break;
                }
            }

            public void Reset()
            {
                Active = 0;
                Choice = 0;
                Pause = 0;
            }
        }

        private readonly TelemetrySession _session;
        private readonly RunState _run;
        private readonly RunPause _pause;
        private readonly RunChoices _choices;
        private readonly UpgradeService _upgrades;
        private readonly ForgeService _forge;
        private readonly CheckpointService _checkpoint;
        private readonly RelicShop _relics;
        private readonly WeaponShop _weapons;
        private readonly string _heroId;
        private readonly string _weaponId;
        private readonly string _relicId;
        private readonly string _abilityId;
        private readonly int _forgeGold;
        private readonly int _deepestFloorCleared;
        private readonly int _seed;
        private readonly Func<string> _killerId;
        private readonly Func<float> _heroHealth;
        private readonly Func<float> _heroMaximumHealth;
        private readonly Func<int> _pulses;
        private readonly StringBuilder _choiceIds = new StringBuilder();
        private readonly StringBuilder _choiceRarities = new StringBuilder();
        private readonly StringBuilder _choiceCategories = new StringBuilder();
        // run_end's loadout lists in category mode, built one after the other.
        private readonly StringBuilder _loadoutList = new StringBuilder();

        private readonly TimeSplit _runTime = new TimeSplit();
        // Resets at each floor_complete, so a floor's time includes the checkpoint and descent that led into it.
        private readonly TimeSplit _floorTime = new TimeSplit();
        // Resets at each room_start; time between a closed room and the next one counts for the floor and run only.
        private readonly TimeSplit _roomTime = new TimeSplit();

        private bool _subscribed;
        private bool _disposed;
        private bool _begun;
        // Set once run_end or run_interrupted is written; nothing of this run is recorded afterwards.
        private bool _closed;
        private string _runId;

        private double _backgroundSeconds;
        private int _kills;
        private int _floorsCompleted;
        private int _roomsCompleted;
        private int _lastFloorCompleted;
        private int _floorRoomsCompleted;
        private bool _firstKillReported;
        private UpgradeOffer _reportedOffer;

        // The last room that started, kept after it closes so a late progress notification for it cannot reopen it.
        private bool _roomOpen;
        private int _roomFloor;
        private int _roomIndex;
        private RoomKind _roomKind;
        private int _roomKills;
        private int _roomGoldKills;
        private int _roomGoldChests;
        private string _bossId;

        // The ordinary wave that spawned last and has not fallen yet, and the run's active seconds when it spawned.
        private bool _waveOpen;
        private double _waveActiveAtStart;

        // The foundry floor's continuous horde: whether a horde is being sampled, the run's active seconds at which its next
        // horde_sample is due, its spawns and deaths since the last sample, and the run's horde deaths for run_end.
        private bool _hordeSampling;
        private double _hordeSampleDue;
        private int _hordeSpawnedWindow;
        private int _hordeKillsWindow;
        private int _hordeKills;

        // Gold attribution. The scene reports a kill before the reward service pays it and a chest after it paid, both
        // within one call chain: gold that a reported kill announced is kill gold, the rest waits for the chest report.
        private int _goldSeen;
        private int _killGoldExpected;
        private int _chestGoldPending;

        private bool _inBackground;
        private double _backgroundSince;
        private bool _ignoreNextTick;

        private bool IsRecording => _begun && !_closed && !_disposed;

        // Validates the wiring once, at scene start, so a missing reference fails there and never inside a handler.
        public RunTelemetry(RunTelemetryContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _session = Require(context.Session, nameof(context.Session));
            _run = Require(context.Run, nameof(context.Run));
            _pause = Require(context.Pause, nameof(context.Pause));
            _choices = Require(context.Choices, nameof(context.Choices));
            _upgrades = Require(context.Upgrades, nameof(context.Upgrades));
            _forge = Require(context.Forge, nameof(context.Forge));
            _checkpoint = Require(context.Checkpoint, nameof(context.Checkpoint));
            _killerId = Require(context.KillerId, nameof(context.KillerId));
            _heroHealth = Require(context.HeroHealth, nameof(context.HeroHealth));
            _heroMaximumHealth = Require(context.HeroMaximumHealth, nameof(context.HeroMaximumHealth));
            _pulses = context.Pulses;
            _relics = context.Relics;
            _weapons = context.Weapons;
            _heroId = RequireId(context.HeroId, nameof(context.HeroId));
            _weaponId = RequireId(context.WeaponId, nameof(context.WeaponId));
            _abilityId = RequireId(context.AbilityId, nameof(context.AbilityId));
            if (context.RelicId != null && context.RelicId.Length == 0)
                throw new ArgumentException("RunTelemetryContext.RelicId is either null or not empty.", nameof(context));
            _relicId = context.RelicId;
            if (context.ForgeGold < 0)
                throw new ArgumentOutOfRangeException(nameof(context), "RunTelemetryContext.ForgeGold cannot be negative.");
            if (context.DeepestFloorCleared < 0)
                throw new ArgumentOutOfRangeException(nameof(context), "RunTelemetryContext.DeepestFloorCleared cannot be negative.");
            _forgeGold = context.ForgeGold;
            _deepestFloorCleared = context.DeepestFloorCleared;
            _seed = context.Seed;
            // Emitting before session_start throws in the session; refusing here keeps that out of every handler.
            if (!_session.HasStarted)
                throw new ArgumentException("Start the telemetry session before recording a run.", nameof(context));

            Subscribe();
        }

        // Starts the run's log. Call before the first wave spawns so run_start precedes every event of the run.
        public void Begin()
        {
            if (_disposed || _begun)
                return;

            _begun = true;
            _runId = _session.BeginRun();
            _goldSeen = _run.Gold;
            _session.Emit("run_start", new TelemetryFields()
                .Add("run_ordinal", _session.RunOrdinal)
                .Add("hero_id", _heroId)
                .Add("weapon_id", _weaponId)
                .Add("relic_id", _relicId)
                .Add("ability_id", _abilityId)
                .Add("forge_gold", _forgeGold)
                .Add("deepest_floor", _deepestFloorCleared)
                .Add("seed", _seed));

            // A run that already ended missed its Ended event; close it now so the log never shows it as still running.
            if (_run.HasEnded)
            {
                ReportRunEnd();
                return;
            }
            ReportUpgradeOfferIfNew();
        }

        // Called on every encounter progress notification, which includes every hit, so it only compares numbers unless
        // the room changed. A room number below 1 (between floors) is not a room.
        // Called each time a continuous swarm's ramp moves to its next step, and once as the room opens, so a run's depth
        // and the ramp it climbed can be read back. step is one-based - what a wave number used to mean - and seconds is
        // how long the hero has stood in the room. Ordinary rooms never call it.
        public void RecordSwarmStep(int step, float seconds, float difficulty)
        {
            if (!IsRecording)
                return;

            _session.Emit("swarm_step", new TelemetryFields()
                .Add("step", step)
                .Add("seconds", (double)seconds)
                .Add("difficulty", (double)difficulty)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth())
                .Add("level", _run.Level)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        // Called as the hero walks out of a swarm by standing on its exit: how long it stayed, and what it had when it went.
        public void RecordSwarmExit(float seconds, int step)
        {
            if (!IsRecording)
                return;

            _session.Emit("swarm_exit", new TelemetryFields()
                .Add("seconds", (double)seconds)
                .Add("step", step)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth())
                .Add("level", _run.Level)
                .Add("kills", _kills)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        // The foundry's map mechanics (2026-10-23/24), so a device session shows how they play: how long a marked hall took
        // to reach, which altar was taken at what level and difficulty, and what each pit eruption burnt.
        public void RecordHallReached(float walkSeconds)
        {
            if (!IsRecording)
                return;

            _session.Emit("hall_reached", new TelemetryFields()
                .Add("walk_sec", (double)walkSeconds)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth())
                .Add("level", _run.Level)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        public void RecordAltarTaken(int altar, float difficulty)
        {
            if (!IsRecording)
                return;

            _session.Emit("altar_taken", new TelemetryFields()
                .Add("altar", altar)
                .Add("difficulty", (double)difficulty)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth())
                .Add("level", _run.Level)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        public void RecordPitEruption(bool heroBurnt, int enemiesBurnt, int enemiesKilled)
        {
            if (!IsRecording)
                return;

            _session.Emit("pit_eruption", new TelemetryFields()
                .Add("hero_burnt", heroBurnt)
                .Add("enemies_burnt", enemiesBurnt)
                .Add("enemies_killed", enemiesKilled)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth())
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        // An ordinary room's waves, so a floor's pacing can be read wave by wave: called as a wave spawns, after the
        // progress that opened its room, and once as its last enemy falls, before the floor moves on, so a wave's lines sit
        // between its room's room_start and room_complete. wave is one-based and waveCount counts a hall's surges too. A
        // swarm room reports swarm_step instead and never calls these.
        public void RecordWaveStart(int wave, int waveCount, int enemyCount)
        {
            if (!IsRecording)
                return;

            _waveOpen = true;
            _waveActiveAtStart = _runTime.Active;
            _session.Emit("wave_start", new TelemetryFields()
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex)
                .Add("wave_index", wave)
                .Add("wave_count", waveCount)
                .Add("enemy_count", enemyCount)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
        }

        // active_sec is the run's active time since the wave spawned; enemy_hits counts the hits its enemies took. A clear
        // without a wave this recorder saw spawn (one that began before Begin, or in a room that has since changed) is
        // not reported, since its time is unknown.
        public void RecordWaveClear(int wave, int waveCount, int enemyHits)
        {
            if (!IsRecording || !_waveOpen)
                return;

            _waveOpen = false;
            _session.Emit("wave_clear", new TelemetryFields()
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex)
                .Add("wave_index", wave)
                .Add("wave_count", waveCount)
                .Add("active_sec", _runTime.Active - _waveActiveAtStart)
                .Add("enemy_hits", enemyHits)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
        }

        // The foundry floor's continuous horde beside the node waves (HordeDirector), sampled so its crowd can be tuned
        // from the device log. The scene reports each horde enemy walking in, and each horde death after its RecordKill.
        public void RecordHordeSpawn()
        {
            if (IsRecording)
                _hordeSpawnedWindow++;
        }

        public void RecordHordeKill()
        {
            if (!IsRecording)
                return;

            _hordeKills++;
            _hordeKillsWindow++;
        }

        // Once per frame while a horde stands, after Tick. The first call starts the cadence; horde_sample is then written
        // every HordeSampleSeconds of the run's active time, which stops for a choice, the player's pause and the
        // background, so a frozen run writes none, and only inside an open room. A long frame writes one sample, not a
        // burst. The window counts are the spawns and horde deaths since the previous sample (or since the horde began).
        // Called every frame, so it only compares numbers unless a sample is due.
        public void ObserveHorde(HordeState state, int hordeAlive, int hordeTarget, int waveAlive)
        {
            if (!IsRecording)
                return;

            if (!_hordeSampling)
            {
                _hordeSampling = true;
                _hordeSampleDue = _runTime.Active + HordeSampleSeconds;
                return;
            }
            if (_runTime.Active < _hordeSampleDue || !_roomOpen || _inBackground || _pause.IsPlayerPaused || _pause.IsChoiceOpen)
                return;

            _session.Emit("horde_sample", new TelemetryFields()
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex)
                .Add("state", HordeStateName(state))
                .Add("horde_alive", hordeAlive)
                .Add("horde_target", hordeTarget)
                .Add("wave_alive", waveAlive)
                .Add("horde_spawned_window", _hordeSpawnedWindow)
                .Add("horde_kills_window", _hordeKillsWindow)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
            _hordeSpawnedWindow = 0;
            _hordeKillsWindow = 0;
            _hordeSampleDue += HordeSampleSeconds;
            if (_hordeSampleDue <= _runTime.Active)
                _hordeSampleDue = _runTime.Active + HordeSampleSeconds;
        }

        // The horde has left the floor (the gate opened, the floor moved on, the scene is going): sampling stops, and the
        // next horde starts its own cadence and window. Its deaths stay in the run's horde_kills.
        public void RecordHordeReleased()
        {
            _hordeSampling = false;
            _hordeSpawnedWindow = 0;
            _hordeKillsWindow = 0;
        }

        public void ObserveProgress(int floorNumber, string floorId, int roomNumber, RoomKind roomKind, string toughestEnemyId)
        {
            if (!IsRecording || floorNumber < 1 || roomNumber < 1)
                return;

            if (floorNumber != _roomFloor || roomNumber != _roomIndex)
            {
                if (_roomOpen)
                    CloseRoom(ResultCleared, false);
                OpenRoom(floorNumber, floorId, roomNumber, roomKind);
            }

            // The boss wave may spawn after the room was first seen; the first id seen names the boss.
            if (_roomOpen && _roomKind == RoomKind.Boss && _bossId == null && !string.IsNullOrEmpty(toughestEnemyId))
            {
                _bossId = toughestEnemyId;
                _session.Emit("boss_start", new TelemetryFields()
                    .Add("floor_index", _roomFloor)
                    .Add("room_index", _roomIndex)
                    .Add("enemy_id", _bossId)
                    .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
            }
        }

        // A drop is about to pay the run: its gold is kill gold, which is how a room's report tells it from a chest's.
        public void RecordDropCollecting(int gold)
        {
            if (!IsRecording || gold <= 0)
                return;
            _killGoldExpected += gold;
        }

        // Called when an enemy is defeated, before the reward service pays for it; gold is the kill's reward, or zero for a
        // kill whose reward fell to the floor and will be reported as it is picked up.
        public void RecordKill(string enemyId, int gold)
        {
            if (!IsRecording)
                return;

            _kills++;
            if (_roomOpen)
                _roomKills++;
            if (gold > 0)
                _killGoldExpected += gold;
            if (_firstKillReported)
                return;

            _firstKillReported = true;
            _session.Emit("first_kill", new TelemetryFields()
                .Add("enemy_id", enemyId)
                .Add("active_sec", _runTime.Active)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        // Called after a chest applied its reward and granted whatever book it held, so the gold it added has already
        // been seen through GoldChanged. bookId is null for the chests that held none.
        public void RecordChestOpened(ChestRewardKind reward, float healFraction, string bookId = null,
            UpgradeRarity bookRarity = UpgradeRarity.Common)
        {
            if (!IsRecording)
                return;

            int gold = reward == ChestRewardKind.Gold ? _chestGoldPending : 0;
            _chestGoldPending = 0;
            if (_roomOpen)
                _roomGoldChests += gold;
            _session.Emit("chest_opened", new TelemetryFields()
                .Add("reward", RewardName(reward))
                .Add("gold", gold)
                .Add("heal_fraction", (double)healFraction)
                .Add("book", bookId ?? "none")
                .Add("book_rarity", bookId != null ? bookRarity.ToString() : "none")
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth())
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        public void RecordAbilityUsed()
        {
            if (!IsRecording)
                return;

            _session.Emit("ability_used", new TelemetryFields()
                .Add("ability_id", _abilityId)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex)
                .Add("room_active_sec", _roomOpen ? _roomTime.Active : 0.0));
        }

        // Called when a floor's last room is resolved, before the scene opens the checkpoint or ends the Descent, so
        // floor_complete precedes extract_choice and run_end. Each floor is reported once.
        public void RecordFloorCleared(int floorNumber, string floorId)
        {
            if (!IsRecording || floorNumber < 1 || floorNumber <= _lastFloorCompleted)
                return;

            if (_roomOpen)
                CloseRoom(ResultCleared, false);
            _lastFloorCompleted = floorNumber;
            _floorsCompleted++;
            _session.Emit("floor_complete", new TelemetryFields()
                .Add("floor_index", floorNumber)
                .Add("floor_id", floorId)
                .Add("active_sec", _floorTime.Active)
                .Add("choice_sec", _floorTime.Choice)
                .Add("pause_sec", _floorTime.Pause)
                .Add("rooms", _floorRoomsCompleted));
            _floorTime.Reset();
            _floorRoomsCompleted = 0;
        }

        // Once per frame. The player's pause and open choices count on unscaled time because scaled time stops for them;
        // everything else in a run counts on scaled time. Bad deltas are skipped rather than rejected: this runs in Update.
        public void Tick(float scaledDeltaTime, float unscaledDeltaTime)
        {
            if (_disposed)
                return;

            if (_ignoreNextTick)
                _ignoreNextTick = false;
            else if (IsRecording && !_inBackground)
                Accumulate(scaledDeltaTime, unscaledDeltaTime);
            _session.FlushIfDue();
        }

        // Leaving the app writes what is buffered, since the OS may end the process without another frame. Returning
        // adds the gap to background time and skips the next tick, whose delta covers the same gap.
        public void ApplicationPaused(bool paused)
        {
            if (_disposed)
                return;

            if (paused)
            {
                if (_inBackground)
                    return;

                _inBackground = true;
                _backgroundSince = _session.Clock.Seconds;
                _session.Emit("app_background", new TelemetryFields().Add("run_active", IsRecording));
                _session.Flush();
                return;
            }

            if (!_inBackground)
                return;

            double gap = CurrentBackgroundGap();
            _inBackground = false;
            _ignoreNextTick = true;
            if (IsRecording)
                _backgroundSeconds += gap;
            _session.Emit("app_foreground", new TelemetryFields().Add("background_sec", gap));
        }

        // The scene is going away. An unfinished run is reported as interrupted, never as ended, so a closed app or a
        // restart does not look like a result. Unsubscribing first means nothing later reaches this recorder.
        public void Dispose()
        {
            if (_disposed)
                return;

            Unsubscribe();
            if (_begun && !_closed)
            {
                _closed = true;
                // Stamped with this run's id: the next scene may already have begun its run on the shared session.
                _session.EmitForRun(_runId, "run_interrupted", new TelemetryFields()
                    .Add("reason", "scene_unloaded")
                    .Add("floor_index", _roomFloor)
                    .Add("room_index", _roomIndex)
                    .Add("active_sec", _runTime.Active)
                    .Add("choice_sec", _runTime.Choice)
                    .Add("pause_sec", _runTime.Pause)
                    .Add("background_sec", RunBackgroundSeconds()));
                EndSessionRun();
            }
            _disposed = true;
            _session.Flush();
        }

        private void Subscribe()
        {
            _run.Ended += OnRunEnded;
            _run.GoldChanged += OnGoldChanged;
            _choices.Changed += OnChoicesChanged;
            _upgrades.OfferChanged += OnUpgradeOfferChanged;
            _upgrades.Selected += OnUpgradeSelected;
            _upgrades.WeaponAcquired += OnWeaponAcquired;
            _forge.Selected += OnForgeSelected;
            _checkpoint.Chosen += OnCheckpointChosen;
            if (_relics != null)
                _relics.Forged += OnRelicForged;
            if (_weapons != null)
                _weapons.Forged += OnWeaponForged;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;

            _run.Ended -= OnRunEnded;
            _run.GoldChanged -= OnGoldChanged;
            _choices.Changed -= OnChoicesChanged;
            _upgrades.OfferChanged -= OnUpgradeOfferChanged;
            _upgrades.Selected -= OnUpgradeSelected;
            _upgrades.WeaponAcquired -= OnWeaponAcquired;
            _forge.Selected -= OnForgeSelected;
            _checkpoint.Chosen -= OnCheckpointChosen;
            if (_relics != null)
                _relics.Forged -= OnRelicForged;
            if (_weapons != null)
                _weapons.Forged -= OnWeaponForged;
            _subscribed = false;
        }

        private void Accumulate(float scaledDeltaTime, float unscaledDeltaTime)
        {
            TimeKind kind;
            float delta;
            if (_pause.IsPlayerPaused)
            {
                kind = TimeKind.Pause;
                delta = unscaledDeltaTime;
            }
            else if (_pause.IsChoiceOpen)
            {
                kind = TimeKind.Choice;
                delta = unscaledDeltaTime;
            }
            else
            {
                kind = TimeKind.Active;
                delta = scaledDeltaTime;
            }

            if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0f)
                return;

            _runTime.Add(kind, delta);
            _floorTime.Add(kind, delta);
            if (_roomOpen)
                _roomTime.Add(kind, delta);
        }

        private void OpenRoom(int floorNumber, string floorId, int roomNumber, RoomKind roomKind)
        {
            _roomOpen = true;
            _roomFloor = floorNumber;
            _roomIndex = roomNumber;
            _roomKind = roomKind;
            _roomKills = 0;
            _roomGoldKills = 0;
            _roomGoldChests = 0;
            _bossId = null;
            _waveOpen = false;
            _roomTime.Reset();
            _session.Emit("room_start", new TelemetryFields()
                .Add("floor_index", floorNumber)
                .Add("floor_id", floorId)
                .Add("room_index", roomNumber)
                .Add("room_kind", RoomKindName(roomKind))
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
        }

        // The room's earnings directly precede its room_complete, and a boss room's boss_end follows it.
        private void CloseRoom(string result, bool heroDied)
        {
            _roomOpen = false;
            if (_roomGoldKills > 0)
                ReportRoomGold("kills", _roomGoldKills);
            if (_roomGoldChests > 0)
                ReportRoomGold("chest", _roomGoldChests);

            _session.Emit("room_complete", new TelemetryFields()
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex)
                .Add("room_kind", RoomKindName(_roomKind))
                .Add("result", result)
                .Add("active_sec", _roomTime.Active)
                .Add("choice_sec", _roomTime.Choice)
                .Add("pause_sec", _roomTime.Pause)
                .Add("kills", _roomKills)
                .Add("gold_kills", _roomGoldKills)
                .Add("gold_chests", _roomGoldChests)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
            if (result == ResultCleared)
            {
                _roomsCompleted++;
                _floorRoomsCompleted++;
            }

            if (_bossId == null)
                return;
            _session.Emit("boss_end", new TelemetryFields()
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex)
                .Add("enemy_id", _bossId)
                .Add("result", heroDied ? "hero_died" : "defeated")
                .Add("active_sec", _roomTime.Active)
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth()));
        }

        private void ReportRoomGold(string source, int amount)
        {
            _session.Emit("currency_earned", new TelemetryFields()
                .Add("currency", "run_gold")
                .Add("source", source)
                .Add("amount", amount)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        private void OnRunEnded()
        {
            if (IsRecording)
                ReportRunEnd();
        }

        // The open room closes with the run's result, then the banked gold, then run_end as the run's last line.
        private void ReportRunEnd()
        {
            _closed = true;
            RunOutcome outcome = _run.Outcome;
            if (_roomOpen)
                CloseRoom(ResultName(outcome), outcome == RunOutcome.Defeat);

            if (_run.GoldBanked > 0)
            {
                _session.Emit("currency_earned", new TelemetryFields()
                    .Add("currency", "forge_gold")
                    .Add("source", "run_bank")
                    .Add("amount", _run.GoldBanked));
            }

            var end = new TelemetryFields()
                .Add("result", ResultName(outcome))
                .Add("cause", CauseOf(outcome))
                .Add("floors_completed", _floorsCompleted)
                .Add("rooms_completed", _roomsCompleted)
                .Add("run_level", _run.Level)
                .Add("kills", _kills)
                .Add("horde_kills", _hordeKills)
                .Add("upgrades_applied", _run.UpgradesApplied)
                .Add("pulses", _pulses != null ? _pulses() : 0)
                .Add("gold", _run.Gold)
                .Add("gold_banked", _run.GoldBanked)
                .Add("gold_lost", _run.GoldLost)
                .Add("active_sec", _runTime.Active)
                .Add("choice_sec", _runTime.Choice)
                .Add("pause_sec", _runTime.Pause)
                .Add("background_sec", RunBackgroundSeconds())
                .AddHeroHealth(_heroHealth(), _heroMaximumHealth());
            if (_upgrades.IsCategoryMode)
                AddBuild(end);
            _session.Emit("run_end", end);
            EndSessionRun();
            _session.Flush();
        }

        // The build a category-mode run ended with, after every key a legacy run_end writes: the weapons in slot order with
        // their levels, the tomes held in catalogue order with theirs, and every evolution taken (a weapon's or a general
        // card's, each of which raised the difficulty).
        private void AddBuild(TelemetryFields fields)
        {
            WeaponLoadout loadout = _upgrades.Loadout;
            _loadoutList.Clear();
            for (int i = 0; i < loadout.Count; i++)
                AppendItem(_loadoutList, i, loadout.At(i).WeaponId);
            fields.Add("weapon_ids", _loadoutList.ToString());
            _loadoutList.Clear();
            for (int i = 0; i < loadout.Count; i++)
                AppendItem(_loadoutList, i).Append(loadout.At(i).Level);
            fields.Add("weapon_levels", _loadoutList.ToString());

            IReadOnlyList<UpgradeOption> tomes = _upgrades.Tomes;
            _loadoutList.Clear();
            int held = 0;
            for (int i = 0; i < tomes.Count; i++)
                if (_upgrades.TomeLevel(tomes[i].Id) > 0)
                    AppendItem(_loadoutList, held++, tomes[i].Id);
            fields.Add("tome_ids", _loadoutList.ToString());
            _loadoutList.Clear();
            held = 0;
            for (int i = 0; i < tomes.Count; i++)
            {
                int level = _upgrades.TomeLevel(tomes[i].Id);
                if (level > 0)
                    AppendItem(_loadoutList, held++).Append(level);
            }
            fields.Add("tome_levels", _loadoutList.ToString());
            fields.Add("evolutions", _upgrades.EvolutionsTaken);
        }

        // Every pulse the run's weapons fired, for RunTelemetryContext.Pulses: each slot's in a category-mode loadout, or
        // the one weapon's without a loadout. Read once, at run_end.
        public static int PulsesFired(WeaponLoadout loadout, WeaponRuntime weapon)
        {
            if (loadout == null)
                return weapon != null ? weapon.PulsesFired : 0;

            int pulses = 0;
            for (int i = 0; i < loadout.Count; i++)
                pulses += loadout.At(i).Runtime.PulsesFired;
            return pulses;
        }

        // Only this recorder's run is closed on the session, so a recorder disposed late cannot strip a newer run's id.
        private void EndSessionRun()
        {
            if (_session.RunId == _runId)
                _session.EndRun();
        }

        private void OnGoldChanged()
        {
            if (!IsRecording)
                return;

            int gained = _run.Gold - _goldSeen;
            _goldSeen = _run.Gold;
            // Securing gold at a checkpoint raises GoldChanged without adding any.
            if (gained <= 0)
                return;

            int fromKills = Math.Min(gained, _killGoldExpected);
            _killGoldExpected -= fromKills;
            if (_roomOpen)
                _roomGoldKills += fromKills;
            _chestGoldPending += gained - fromKills;
        }

        // A choice freezes the run and is where a player may leave the app, so buffered lines are written now.
        private void OnChoicesChanged()
        {
            if (!_choices.IsOpen)
                return;

            // RunChoices hears an upgrade offer before this recorder does; report it first so the flush includes it.
            ReportUpgradeOfferIfNew();
            _session.Flush();
        }

        private void OnUpgradeOfferChanged() => ReportUpgradeOfferIfNew();

        private void ReportUpgradeOfferIfNew()
        {
            UpgradeOffer offer = _upgrades.CurrentOffer;
            if (!IsRecording || offer == null || ReferenceEquals(offer, _reportedOffer))
                return;

            _reportedOffer = offer;
            bool categories = _upgrades.IsCategoryMode;
            _choiceIds.Clear();
            _choiceRarities.Clear();
            _choiceCategories.Clear();
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                AppendItem(_choiceIds, i, offer.Choices[i].Id);
                AppendItem(_choiceRarities, i, offer.RarityAt(i).ToString());
                if (categories)
                    AppendItem(_choiceCategories, i, offer.Choices[i].Category.ToString());
            }

            var fields = new TelemetryFields()
                .Add("choice_ids", _choiceIds.ToString())
                .Add("run_level", _run.Level)
                .Add("pending", _run.PendingUpgrades)
                .Add("offer_index", offer.Index)
                .Add("choice_rarities", _choiceRarities.ToString());
            // Category mode only, and after every legacy key, so a legacy line keeps exactly the keys it always had.
            if (categories)
                fields.Add("choice_categories", _choiceCategories.ToString());
            _session.Emit("upgrade_offered", fields);
        }

        // A comma before every item but the first, so each list reads like choice_ids.
        private static StringBuilder AppendItem(StringBuilder list, int index)
        {
            if (index > 0)
                list.Append(',');
            return list;
        }

        private static void AppendItem(StringBuilder list, int index, string item) => AppendItem(list, index).Append(item);

        private void OnUpgradeSelected(UpgradeOption option, int slot)
        {
            if (!IsRecording || option == null)
                return;

            UpgradeOffer from = _upgrades.LastSelected;

            var fields = new TelemetryFields()
                .Add("upgrade_id", option.Id)
                .Add("choice_slot", slot)
                .Add("run_level", _run.Level)
                .Add("stacks", _upgrades.StacksOf(option))
                .Add("offer_index", from != null ? from.Index : -1)
                .Add("rarity", from != null ? from.RarityAt(slot).ToString() : "Common");
            if (_upgrades.IsCategoryMode)
                AddCategoryPick(fields, option);
            _session.Emit("upgrade_selected", fields);
        }

        // Category mode only, after every legacy key: the kind of pick and, once it is applied, the level of what it grew -
        // the weapon (named, since a line's id is the line's own) or the tome. An evolution raises no level, so its line
        // carries its weapon's level as it stands. A general card's level is its stacks, already written.
        private void AddCategoryPick(TelemetryFields fields, UpgradeOption option)
        {
            fields.Add("category", option.Category.ToString());
            switch (option.Category)
            {
                case OfferCategory.NewWeapon:
                case OfferCategory.WeaponUpgrade:
                {
                    string weaponId = WeaponIdOf(option);
                    WeaponLoadout loadout = _upgrades.Loadout;
                    int slot = weaponId != null ? loadout.IndexOf(weaponId) : -1;
                    fields.Add("weapon_id", weaponId)
                        .Add("weapon_level", slot >= 0 ? loadout.At(slot).Level : 0);
                    break;
                }
                case OfferCategory.NewTome:
                case OfferCategory.TomeUpgrade:
                    fields.Add("tome_level", _upgrades.StacksOf(option));
                    break;
            }
        }

        // The weapon a weapon card belongs to: its Acquire, one of its lines or its evolution. Null for any other card.
        private string WeaponIdOf(UpgradeOption option)
        {
            IReadOnlyList<WeaponCard> cards = _upgrades.WeaponCards;
            for (int i = 0; i < cards.Count; i++)
            {
                WeaponCard card = cards[i];
                if (option == card.Acquire || option == card.Evolution)
                    return card.Id;
                for (int j = 0; j < card.Lines.Count; j++)
                    if (option == card.Lines[j])
                        return card.Id;
            }
            return null;
        }

        // A weapon taken into the loadout. The service raises it while applying the pick, so this line precedes the
        // upgrade_selected that took it. slot is the loadout's; slot 0 is the weapon the run started with, which run_start
        // names and which no weapon_acquired reports.
        private void OnWeaponAcquired(WeaponCard card, WeaponRuntime runtime)
        {
            if (!IsRecording || card == null || !_upgrades.IsCategoryMode)
                return;

            _session.Emit("weapon_acquired", new TelemetryFields()
                .Add("weapon_id", card.Id)
                .Add("slot", _upgrades.Loadout.IndexOf(card.Id))
                .Add("run_level", _run.Level)
                .Add("floor_index", _roomFloor)
                .Add("room_index", _roomIndex));
        }

        private void OnForgeSelected(ForgeOption option, int slot)
        {
            if (!IsRecording || option == null)
                return;

            _session.Emit("forge_selected", new TelemetryFields()
                .Add("option_id", option.Id)
                .Add("choice_slot", slot)
                .Add("floor_index", _roomFloor));
        }

        private void OnCheckpointChosen(CheckpointKind kind)
        {
            if (!IsRecording)
                return;

            _session.Emit("extract_choice", new TelemetryFields()
                .Add("choice", ChoiceName(kind))
                .Add("floor_index", _roomFloor)
                .Add("gold", _run.Gold)
                .Add("gold_unsecured", _run.UnsecuredGold));
        }

        // Purchases happen in the Relic Forge after the run ended, so these lines carry no run id.
        private void OnRelicForged(RelicOption relic)
        {
            if (!_disposed && relic != null)
                ReportSpent("relic", relic.Id, relic.Price);
        }

        private void OnWeaponForged(WeaponOption weapon)
        {
            if (!_disposed && weapon != null)
                ReportSpent("weapon", weapon.Id, weapon.Price);
        }

        private void ReportSpent(string sink, string itemId, int amount)
        {
            _session.Emit("currency_spent", new TelemetryFields()
                .Add("currency", "forge_gold")
                .Add("sink", sink)
                .Add("item_id", itemId)
                .Add("amount", amount));
        }

        private double CurrentBackgroundGap()
        {
            return _inBackground ? Math.Max(0.0, _session.Clock.Seconds - _backgroundSince) : 0.0;
        }

        // Includes a background gap still open when the run closes, which a later return then no longer adds.
        private double RunBackgroundSeconds()
        {
            return _backgroundSeconds + CurrentBackgroundGap();
        }

        private string CauseOf(RunOutcome outcome)
        {
            switch (outcome)
            {
                case RunOutcome.Victory:
                    return "cleared";
                case RunOutcome.Extracted:
                    return "extracted";
                default:
                    string killer = _killerId();
                    return string.IsNullOrEmpty(killer) ? "killed" : "killed_by:" + killer;
            }
        }

        private static string ResultName(RunOutcome outcome)
        {
            switch (outcome)
            {
                case RunOutcome.Victory:
                    return "victory";
                case RunOutcome.Extracted:
                    return "extracted";
                default:
                    return "defeat";
            }
        }

        private static string RoomKindName(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Combat:
                    return "combat";
                case RoomKind.Forge:
                    return "forge";
                case RoomKind.Elite:
                    return "elite";
                case RoomKind.Boss:
                    return "boss";
                case RoomKind.Swarm:
                    return "swarm";
                default:
                    return "unknown";
            }
        }

        private static string HordeStateName(HordeState state)
        {
            switch (state)
            {
                case HordeState.Ramp:
                    return "ramp";
                case HordeState.Horde:
                    return "horde";
                case HordeState.Pressure:
                    return "pressure";
                case HordeState.Boss:
                    return "boss";
                default:
                    return "unknown";
            }
        }

        // A swarm room's two ride on the same event as the boss checkpoint, because they are the same decision asked in
        // a different room.
        private static string ChoiceName(CheckpointKind kind)
        {
            switch (kind)
            {
                case CheckpointKind.Extract:
                    return "extract";
                case CheckpointKind.Descend:
                    return "descend";
                default:
                    return "unknown";
            }
        }

        private static string RewardName(ChestRewardKind reward)
        {
            switch (reward)
            {
                case ChestRewardKind.Heal:
                    return "heal";
                case ChestRewardKind.Gold:
                    return "gold";
                default:
                    return "unknown";
            }
        }

        private static T Require<T>(T value, string name) where T : class
        {
            if (value == null)
                throw new ArgumentException($"RunTelemetryContext.{name} is required.", "context");
            return value;
        }

        private static string RequireId(string value, string name)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"RunTelemetryContext.{name} is required.", "context");
            return value;
        }
    }

    // The hero's health as every event writes it: the current value beside the maximum, which health cards raise, so a
    // log reads health as a share wherever it reads it.
    internal static class HeroHealthFields
    {
        public static TelemetryFields AddHeroHealth(this TelemetryFields fields, float current, float maximum) =>
            fields.Add("hero_hp", (double)current).Add("hero_max_hp", (double)maximum);
    }
}
