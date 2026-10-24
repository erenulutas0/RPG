using System;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.Analytics
{
    // Connects one scene's run recorder to the Unity side: encounter progress, kills, floor clears, the ability and the
    // chest reach the pure RunTelemetry through here, as do frames and the app leaving the foreground. The composition
    // root adds it at runtime (it is never placed in a scene) before its own reactions subscribe, and it goes away with
    // the scene, unsubscribing and disposing the recorder, so a restart never keeps an old subscription. It only reads
    // state and never logs: telemetry must not change a run or add console output that scene tests would flag.
    [AddComponentMenu("")]
    public sealed class RunTelemetryBridge : MonoBehaviour
    {
        private RunTelemetry _telemetry;
        private EncounterController _encounters;
        private AbilityController _ability;
        private ChestSpawner _chests;
        private GroundDrops _drops;

        // Builds the recorder from the context (which validates it before anything is added), adds the bridge to host and
        // subscribes. Call before the composition root subscribes to the same encounter events, so telemetry sees a kill
        // or floor clear before the reaction to it, and call Begin before the first wave spawns.
        public static RunTelemetryBridge Attach(GameObject host, RunTelemetryContext context, EncounterController encounters,
            AbilityController ability)
        {
            if (host == null)
                throw new ArgumentNullException(nameof(host));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (encounters == null)
                throw new ArgumentNullException(nameof(encounters));
            if (ability == null)
                throw new ArgumentNullException(nameof(ability));

            var telemetry = new RunTelemetry(context);
            RunTelemetryBridge bridge = host.AddComponent<RunTelemetryBridge>();
            bridge.Bind(telemetry, encounters, ability, FindChestSpawner(host.scene));
            return bridge;
        }

        // The Descent's drops: a dropped kill's gold is kill gold when it is picked up (docs/27, S6d). Before the
        // composition root's own subscriptions, like everything else here.
        public void WatchDrops(GroundDrops drops)
        {
            if (drops == null)
                throw new ArgumentNullException(nameof(drops));
            if (_drops != null)
                throw new InvalidOperationException("The bridge already watches the run's drops.");
            _drops = drops;
            _drops.Collecting += OnDropCollecting;
        }

        private void OnDropCollecting(GroundDrops.Drop drop) => _telemetry.RecordDropCollecting(drop.Gold);

        // The foundry's altars and pit: each altar taken and each eruption, as the composition root wires them up.
        public void WatchFoundry(Cryptforge.Core.AltarService altars, Cryptforge.Core.PitEruptionService pit, Func<float> difficulty)
        {
            if (altars != null)
                altars.Taken += altar => _telemetry.RecordAltarTaken(altar, difficulty());
            if (pit != null)
                pit.Erupted += () => _telemetry.RecordPitEruption(pit.LastHeroBurnt, pit.LastEnemiesBurnt, pit.LastEnemiesKilled);
        }

        // Writes run_start; the run's other events follow it.
        public void Begin()
        {
            if (_telemetry != null)
                _telemetry.Begin();
        }

        private void Bind(RunTelemetry telemetry, EncounterController encounters, AbilityController ability, ChestSpawner chests)
        {
            _telemetry = telemetry;
            _encounters = encounters;
            _ability = ability;
            _chests = chests;

            _encounters.EncounterStarted += OnProgressChanged;
            _encounters.SwarmStepped += OnSwarmStepped;
            _encounters.SwarmLeft += OnSwarmLeft;
            _encounters.HallReached += OnHallReached;
            _encounters.ProgressChanged += OnProgressChanged;
            _encounters.EnemyDefeated += OnEnemyDefeated;
            _encounters.FloorCleared += OnFloorCleared;
            _ability.Used += OnAbilityUsed;
            if (_chests != null)
                _chests.Opened += OnChestOpened;
        }

        // The spawner in this scene only: while a restart loads, the previous scene's spawner may not be destroyed yet.
        // A scene without chests is fine.
        private static ChestSpawner FindChestSpawner(Scene scene)
        {
            ChestSpawner[] spawners = FindObjectsByType<ChestSpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < spawners.Length; i++)
            {
                if (spawners[i].gameObject.scene == scene)
                    return spawners[i];
            }
            return null;
        }

        // Progress fires on every hit, so this only reads a few values; the toughest enemy is looked up for boss rooms
        // alone, the only rooms that report one. No room (before the first step or while descending) is nothing to report.
        private void OnProgressChanged()
        {
            RoomDefinition room = _encounters.CurrentRoom;
            if (room == null)
                return;

            FloorDefinition floor = _encounters.Floor;
            string toughestId = null;
            if (room.Kind == RoomKind.Boss)
            {
                EnemyDefinition toughest = _encounters.ToughestDefinition;
                toughestId = toughest != null ? toughest.Id : null;
            }
            _telemetry.ObserveProgress(_encounters.FloorNumber, floor != null ? floor.Id : null, _encounters.RoomNumber, room.Kind,
                toughestId);
        }

        // A continuous swarm reports each step of its ramp and the moment the hero walks out.
        private void OnSwarmStepped() =>
            _telemetry.RecordSwarmStep(_encounters.SwarmWave, _encounters.SwarmSeconds, _encounters.SwarmDifficulty);

        private void OnHallReached(float walkSeconds) => _telemetry.RecordHallReached(walkSeconds);

        private void OnSwarmLeft() => _telemetry.RecordSwarmExit(_encounters.SwarmSeconds, _encounters.SwarmWave);

        // Heard before the composition root pays the kill, which the recorder needs to tell kill gold from chest gold.
        private void OnEnemyDefeated(Health enemy)
        {
            EnemyDefinition definition = _encounters.DefinitionOf(enemy);
            // A kill in the Descent drops its gold, which is expected when it is picked up rather than now.
            int gold = _encounters.IsInSwarmRoom || _drops == null ? _encounters.GoldRewardOf(enemy) : 0;
            _telemetry.RecordKill(definition != null ? definition.Id : null, gold);
        }

        // Heard before the composition root opens the checkpoint or ends the Descent.
        private void OnFloorCleared()
        {
            FloorDefinition floor = _encounters.Floor;
            _telemetry.RecordFloorCleared(_encounters.FloorNumber, floor != null ? floor.Id : null);
        }

        private void OnAbilityUsed() => _telemetry.RecordAbilityUsed();

        // The spawner raises Opened after it applied the reward, so chest gold has already reached the run.
        private void OnChestOpened(ChestReward reward, Vector3 position) =>
            _telemetry.RecordChestOpened(reward.Kind, reward.HealFraction, _chests.LastBook?.Id,
                _chests.LastBookRarity);

        // Scaled time for the run's active seconds, unscaled for choices and the player's pause, which stop scaled time.
        private void Update()
        {
            if (_telemetry != null)
                // A pre-encounter presentation is not active combat time. Keep unscaled flush/pause handling.
                _telemetry.Tick(_encounters != null && _encounters.IsFirstEncounterPending ? 0f : Time.deltaTime,
                    Time.unscaledDeltaTime);
        }

        private void OnApplicationPause(bool paused)
        {
            if (_telemetry != null)
                _telemetry.ApplicationPaused(paused);
        }

        // Unsubscribes from the managed events even when a source component was destroyed first, then lets the recorder
        // close an unfinished run as interrupted and write what it buffered.
        private void OnDestroy()
        {
            if (!ReferenceEquals(_encounters, null))
            {
                _encounters.EncounterStarted -= OnProgressChanged;
                _encounters.SwarmStepped -= OnSwarmStepped;
                _encounters.SwarmLeft -= OnSwarmLeft;
                _encounters.HallReached -= OnHallReached;
                _encounters.ProgressChanged -= OnProgressChanged;
                _encounters.EnemyDefeated -= OnEnemyDefeated;
                _encounters.FloorCleared -= OnFloorCleared;
            }
            if (!ReferenceEquals(_ability, null))
                _ability.Used -= OnAbilityUsed;
            if (!ReferenceEquals(_chests, null))
                _chests.Opened -= OnChestOpened;
            if (_drops != null)
                _drops.Collecting -= OnDropCollecting;

            if (_telemetry != null)
            {
                _telemetry.Dispose();
                _telemetry = null;
            }
        }
    }
}
