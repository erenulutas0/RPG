using System;
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
        private readonly Func<string> _killerId;
        private readonly Func<float> _heroHealth;
        private readonly StringBuilder _choiceIds = new StringBuilder();

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
                .Add("deepest_floor", _deepestFloorCleared));

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
                    .Add("enemy_id", _bossId));
            }
        }

        // Called when an enemy is defeated, before the reward service pays for it; gold is the kill's reward.
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

        // Called after a chest applied its reward, so the gold it added has already been seen through GoldChanged.
        public void RecordChestOpened(ChestRewardKind reward, float healFraction)
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
                .Add("hero_hp", (double)_heroHealth())
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
            _roomTime.Reset();
            _session.Emit("room_start", new TelemetryFields()
                .Add("floor_index", floorNumber)
                .Add("floor_id", floorId)
                .Add("room_index", roomNumber)
                .Add("room_kind", RoomKindName(roomKind)));
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
                .Add("hero_hp", (double)_heroHealth()));
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
                .Add("active_sec", _roomTime.Active));
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

            _session.Emit("run_end", new TelemetryFields()
                .Add("result", ResultName(outcome))
                .Add("cause", CauseOf(outcome))
                .Add("floors_completed", _floorsCompleted)
                .Add("rooms_completed", _roomsCompleted)
                .Add("run_level", _run.Level)
                .Add("kills", _kills)
                .Add("upgrades_applied", _run.UpgradesApplied)
                .Add("gold", _run.Gold)
                .Add("gold_banked", _run.GoldBanked)
                .Add("gold_lost", _run.GoldLost)
                .Add("active_sec", _runTime.Active)
                .Add("choice_sec", _runTime.Choice)
                .Add("pause_sec", _runTime.Pause)
                .Add("background_sec", RunBackgroundSeconds()));
            EndSessionRun();
            _session.Flush();
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
            _choiceIds.Clear();
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                if (i > 0)
                    _choiceIds.Append(',');
                _choiceIds.Append(offer.Choices[i].Id);
            }

            _session.Emit("upgrade_offered", new TelemetryFields()
                .Add("choice_ids", _choiceIds.ToString())
                .Add("run_level", _run.Level)
                .Add("pending", _run.PendingUpgrades));
        }

        private void OnUpgradeSelected(UpgradeOption option, int slot)
        {
            if (!IsRecording || option == null)
                return;

            _session.Emit("upgrade_selected", new TelemetryFields()
                .Add("upgrade_id", option.Id)
                .Add("choice_slot", slot)
                .Add("run_level", _run.Level)
                .Add("stacks", _upgrades.StacksOf(option)));
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
                .Add("choice", kind == CheckpointKind.Extract ? "extract" : "descend")
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
}
