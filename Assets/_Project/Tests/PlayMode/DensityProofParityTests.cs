using System;
using System.Collections;
using System.Globalization;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The density proof in the real scene: the wave the owner picked - two Grunts trailing eight Cinder Mites - fought by a
    // hero that stands still, by one that kites and by one that turns round every second and a half, against the same wave
    // in DescentSimulation. SimulationParityTests pins the standing Descent; this pins the walking one, which is the only
    // evidence that HeroMotion, PackMotion, EntrySides and the routes behave the same way in the scene as in the
    // simulation, and every claim about what kiting is worth rests on that. For the walking cases the hero's final floor
    // position must match too: the same outcome reached by a different walk would not prove the movement is mirrored. On
    // every frame, in the scene itself, no two living enemies stand within a body of each other.
    //
    // The scene reaches the proof wave through DevelopmentStart: the test writes development/start-floor.txt into its own
    // profile folder before the scene loads, so Gameplay.unity is never touched and a release build can never get here.
    public sealed class DensityProofParityTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ProofFloorId = "floor_density_proof";
        private const string AuthoredFloorId = "floor_ember_halls";
        private const int ProofWaveSize = 10;
        // As in SimulationParityTests: both sides reach identical health today, and the margin only absorbs a reordering of
        // hits inside one frame.
        private const float HealthTolerance = 1f;
        // A single frame of divergence moves the hero 2.5 / 60 = 0.042 floor units, forty times this, so a walk that passes
        // here is the simulation's walk and not a similar one.
        private const float PositionTolerance = 1e-3f;
        // Long enough for the slowest case the stage measured (Daggers kiting, 31.3 fight seconds) on a slow Editor, short
        // enough that a hung run fails instead of hanging the suite.
        private const float RunDeadline = 120f;

        private CombatSetup _setup;
        private Health _hero;
        private EncounterController _encounters;
        private HeroMovementInput _movement;
        private ArenaView _arena;
        private ChestSpawner _chests;
        private IHeroRoute _route;
        private readonly RouteView _view = new RouteView();
        private int _previousFrameRate;
        // What the scene looked like the instant it loaded, before its first Update: the floor it started on, its wave, and
        // how near the hero the nearest enemy entered. Recorded there and asserted in the test body, because an assertion
        // thrown inside a sceneLoaded callback is reported as a console error instead of a failure.
        private string _spawnFloorId;
        private int _spawnCount;
        private float _spawnElapsed;
        private Vector3 _spawnHeroAt;
        private float _closestSpawnDistance;
        private int _spawnFrame;
        // The wave and the floor the scene actually built, so the hand-written proof asset can be held against the floor
        // the simulation fights. Nothing else checks that the two describe the same encounter, and a wrong field in the
        // asset would otherwise show up only as an unexplained health mismatch at the end of the run.
        private EnemyDefinition[] _spawnDefinitions;
        private RoomKind _spawnRoomKind;
        private int _spawnRoomCount;
        private float _spawnHealthMultiplier;
        private float _spawnDamageMultiplier;
        private bool _spawnHasModifier;
        private bool _spawnHasNextFloor;
        // Frames on which the hero stood outside the margin HeroMotion keeps from the rim. It never should.
        private int _framesOffPlatform;
        // How close two living enemies of the wave came in the scene, squared, over every frame of the run.
        private float _closestEnemyGapSquared;
        // The most upgrades left unanswered at the end of a frame while the run was still live. The drain below empties
        // them every frame, so this stays 0 and the accounting at the end can only be explaining the run's last kill.
        private int _pendingWhileLive;

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            Application.targetFrameRate = _previousFrameRate;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Density Proof Cleanup");
            SceneManager.SetActiveScene(empty);
            if (gameplay.path == ScenePath)
                yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator SwordStandingOnTheProofFloorMatchesTheSimulation() =>
            PlayProof(null, DescentSimulation.Sword(), null);

        [UnityTest]
        public IEnumerator SwordKitingOnTheProofFloorMatchesTheSimulation() =>
            PlayProof(null, DescentSimulation.Sword(), new KiteRoute());

        [UnityTest]
        public IEnumerator SwordTurningRoundOnTheProofFloorMatchesTheSimulation() =>
            PlayProof(null, DescentSimulation.Sword(), DescentSimulation.TurningRound());

        [UnityTest]
        public IEnumerator StaffStandingOnTheProofFloorMatchesTheSimulation() =>
            PlayProof("weapon_staff", DescentSimulation.Staff(), null);

        [UnityTest]
        public IEnumerator StaffKitingOnTheProofFloorMatchesTheSimulation() =>
            PlayProof("weapon_staff", DescentSimulation.Staff(), new KiteRoute());

        [UnityTest]
        public IEnumerator StaffTurningRoundOnTheProofFloorMatchesTheSimulation() =>
            PlayProof("weapon_staff", DescentSimulation.Staff(), DescentSimulation.TurningRound());

        [UnityTest]
        public IEnumerator DaggersStandingOnTheProofFloorMatchesTheSimulation() =>
            PlayProof("weapon_daggers", DescentSimulation.Daggers(), null);

        [UnityTest]
        public IEnumerator DaggersKitingOnTheProofFloorMatchesTheSimulation() =>
            PlayProof("weapon_daggers", DescentSimulation.Daggers(), new KiteRoute());

        [UnityTest]
        public IEnumerator DaggersTurningRoundOnTheProofFloorMatchesTheSimulation() =>
            PlayProof("weapon_daggers", DescentSimulation.Daggers(), DescentSimulation.TurningRound());

        // The development hook must be invisible to a player: with no start-floor file the scene descends from the floor
        // Gameplay.unity carries, as every other test and every build does.
        [UnityTest]
        public IEnumerator WithoutTheStartFloorFileTheSceneStartsOnTheAuthoredFloor()
        {
            TestProfile.Begin();
            Assert.That(File.Exists(DevelopmentStart.StartFloorPath), Is.False, "This test writes no start-floor file.");
            _previousFrameRate = Application.targetFrameRate;
            Time.timeScale = 1f;
            AsyncOperation load = BeginLoad(null);
            yield return load;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            Assert.That(_encounters.Floor, Is.Not.Null);
            Assert.That(_encounters.Floor.Id, Is.EqualTo(AuthoredFloorId), "The authored Descent still starts the run.");
            Assert.That(_spawnFloorId, Is.EqualTo(AuthoredFloorId));
            Assert.That(_spawnCount, Is.EqualTo(2), "Ember Halls opens with its authored pair, not the proof wave.");
            LogAssert.NoUnexpectedReceived();
        }

        // route is null for a hero that stands. Both routes read nothing between calls, so the simulation and the scene can
        // share one.
        private IEnumerator PlayProof(string weaponId, DescentSimulation.HeroWeapon weapon, IHeroRoute route)
        {
            DescentSimulation.Result expected = DescentSimulation.Run(
                new[] { DescentSimulation.DensityProofTrailing }, 0, true, null, weapon, route: route);

            TestProfile.Begin(new PlayerProfile(0, null, null, 0,
                weaponId != null ? new[] { weaponId } : null, weaponId));
            WriteStartFloor(ProofFloorId);
            _previousFrameRate = Application.targetFrameRate;
            Time.timeScale = 1f;
            Time.captureDeltaTime = 1f / 60f;
            AsyncOperation load = BeginLoad(route);
            yield return load;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            // Game time keeps its fixed step, so frames may run as fast as the Editor renders them.
            Application.targetFrameRate = -1;

            // The scene's frame that ran while this coroutine was suspended is the simulation's first walking frame, and it
            // walked the steer held from the spawn state above. Anything else and the walk starts a frame behind.
            Assert.That(_spawnElapsed, Is.Zero,
                "The route was primed after the scene's first Update, so the hero lost a frame of steering.");
            Assert.That(Time.frameCount, Is.EqualTo(_spawnFrame),
                $"{Time.frameCount - _spawnFrame} frame(s) ran between the scene loading and this driver taking over; the " +
                "driver has to steer from the frame the wave spawns in, or the walk starts behind the simulation's. " +
                "Drive the route from a MonoBehaviour with [DefaultExecutionOrder(-70)] rather than loosening this.");
            Assert.That(_spawnFloorId, Is.EqualTo(ProofFloorId), "The scene started on the development proof floor.");
            Assert.That(_spawnCount, Is.EqualTo(ProofWaveSize), "The proof wave holds ten enemies.");
            Assert.That(_spawnHeroAt, Is.EqualTo(Vector3.zero), "The run starts at the arena's centre.");
            Assert.That(_closestSpawnDistance, Is.GreaterThanOrEqualTo(EntrySides.MinimumEntryDistance),
                "Every enemy entered at least the minimum entry distance from the hero, so none spawned already striking.");
            AssertTheProofWaveMatchesTheSimulation();

            int kills = 0;
            Action<Health> countKill = enemy => kills++;
            _encounters.EnemyDefeated += countKill;
            float deadline = Time.realtimeSinceStartup + RunDeadline;
            try
            {
                while (true)
                {
                    // Drained before the end check, the way the simulation applies an upgrade the moment the kill awards
                    // it. A refused selection ends the drain instead of spinning: the game withdraws an offer the moment
                    // the run ends, and the proof wave's last kill is usually the one that levels the hero.
                    while (_setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade &&
                        _setup.Choices.TrySelect(_setup.Choices.Current, 0))
                    {
                    }
                    if (_setup.Run.HasEnded || Time.realtimeSinceStartup >= deadline)
                        break;

                    // Every upgrade the run has earned so far is answered, every frame. Only the last kill's can be left,
                    // and only because the run ends with it, which is what the accounting assertion below allows for.
                    if (_setup.Run.PendingUpgrades > _pendingWhileLive)
                        _pendingWhileLive = _setup.Run.PendingUpgrades;
                    Observe();
                    Steer();
                    yield return null;
                }
            }
            finally
            {
                _encounters.EnemyDefeated -= countKill;
            }

            float heroFloorX = _encounters.HeroFloorX;
            float heroFloorY = _encounters.HeroFloorY;
            string report = string.Format(CultureInfo.InvariantCulture,
                "scene: {0}, {1:0.#} HP, {2} kills, gold {3} ({4} banked), level {5}, {6} upgrades applied and {7} left " +
                "pending, hero at ({8:0.###}, {9:0.###}); simulation: {10} floors cleared, {11:0.#} HP, {12} kills, " +
                "gold {13} ({14}), level {15}, {16} upgrades, hero at ({17:0.###}, {18:0.###}), {19:0.##} fight s",
                _setup.Run.Outcome, _hero.Current, kills, _setup.Run.Gold, _setup.Run.GoldBanked, _setup.Run.Level,
                _setup.Run.UpgradesApplied, _setup.Run.PendingUpgrades, heroFloorX, heroFloorY,
                expected.ClearedFloors, expected.HeroHealth, expected.Kills, expected.Gold, expected.GoldBanked,
                expected.Level, expected.UpgradesApplied, expected.HeroFloorX, expected.HeroFloorY, expected.FightSeconds);
            TestContext.WriteLine(report);

            Assert.That(_setup.Run.HasEnded, Is.True, "The proof encounter did not finish. " + report);
            Assert.That(_setup.Run.Outcome,
                Is.EqualTo(expected.ClearedFloors == 1 ? RunOutcome.Victory : RunOutcome.Defeat), report);
            Assert.That(kills, Is.EqualTo(expected.Kills), report);
            Assert.That(_setup.Run.Gold, Is.EqualTo(expected.Gold), report);
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(expected.GoldBanked), report);
            Assert.That(_setup.Run.Level, Is.EqualTo(expected.Level), report);
            Assert.That(_pendingWhileLive, Is.Zero,
                "An upgrade went unanswered while the run was still live, so the accounting below would be hiding it. " + report);
            // Every upgrade the simulation applied is either applied here or still pending. The two differ only at the
            // very end: UpgradeService withdraws a choice that is open when the run ends, and on this floor the last kill
            // is usually the Grunt whose experience levels the hero, while the simulation applies it inside the kill and
            // has no such moment. The assertion above proves nothing was left pending any earlier than that.
            //
            // It is a real gap in DescentSimulation, not only in this floor's accounting, and it can be closed: no wave of
            // any authored floor ends a run on a kill that applies an upgrade (measured over both Descent floors, all three
            // weapons, both card slots, Mend or Temper, with and without the burst - the run's last kill applies none, and
            // no death frame carries one either, because the pool is already spent by then). So teaching the simulation to
            // drop an upgrade granted by the kill that ends the run would move no existing balance number and no scene
            // parity case, and this test could then compare UpgradesApplied outright.
            Assert.That(_setup.Run.UpgradesApplied + _setup.Run.PendingUpgrades, Is.EqualTo(expected.UpgradesApplied), report);
            Assert.That(_hero.Current, Is.EqualTo(expected.HeroHealth).Within(HealthTolerance), report);
            Assert.That(_framesOffPlatform, Is.Zero, "The hero never stood outside the platform's margin. " + report);
            Assert.That(_closestEnemyGapSquared, Is.GreaterThanOrEqualTo(DescentSimulation.BodySpacing * DescentSimulation.BodySpacing),
                "Two living enemies stood within a body of each other in the scene. " + report);
            Assert.That(expected.ClosestEnemyGapSquared, Is.GreaterThanOrEqualTo(DescentSimulation.BodySpacing * DescentSimulation.BodySpacing),
                report);
            if (route != null)
            {
                Assert.That(expected.StrikesWhileMoving, Is.GreaterThan(0),
                    "A walking case has to be one that fights while it walks. " + report);
                Assert.That(heroFloorX, Is.EqualTo(expected.HeroFloorX).Within(PositionTolerance),
                    "The scene's hero walked a different route. " + report);
                Assert.That(heroFloorY, Is.EqualTo(expected.HeroFloorY).Within(PositionTolerance),
                    "The scene's hero walked a different route. " + report);
            }
            else
            {
                Assert.That(_hero.transform.position, Is.EqualTo(Vector3.zero),
                    "A hero with no driver never leaves the centre. " + report);
            }
            LogAssert.NoUnexpectedReceived();
        }

        // Starts the load with the driver already listening, so it takes hold of the scene between its Awake calls and its
        // very first Update: the driver has to answer with the steer the simulation's first frame answers with, or the walk
        // starts a frame behind and never catches up. The operation is yielded by the caller, never nested in another
        // enumerator, so no coroutine machinery can slip an extra frame in between.
        private AsyncOperation BeginLoad(IHeroRoute route)
        {
            _route = route;
            _framesOffPlatform = 0;
            _closestEnemyGapSquared = float.MaxValue;
            _pendingWhileLive = 0;
            SceneManager.sceneLoaded += OnSceneLoaded;
            return SceneManager.LoadSceneAsync(ScenePath);
        }

        // Runs when the loaded scene has had its Awake and OnEnable calls and no Update yet, which is where the spawn state
        // can be read and the first steer held.
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != ScenePath)
                return;

            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _movement = _hero.GetComponent<HeroMovementInput>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _arena = Object.FindFirstObjectByType<ArenaView>();
            // Chests are not part of the Descent simulation and a walking hero steps on one: room 1's chest stands 1.8 floor
            // units to the right of the centre and mends a quarter of the hero's health, which would make every comparison
            // below meaningless. Disabling the spawner before its first Update is the only difference between this scene and
            // the one a player sees.
            _chests = Object.FindFirstObjectByType<ChestSpawner>();
            if (_chests != null)
                _chests.enabled = false;

            _spawnFloorId = _encounters.Floor != null ? _encounters.Floor.Id : null;
            _spawnCount = _encounters.WaveEnemyCount;
            _spawnElapsed = _encounters.Elapsed;
            _spawnHeroAt = _hero.transform.position;
            _closestSpawnDistance = ClosestEnemyDistance();
            _spawnFrame = Time.frameCount;
            _spawnDefinitions = new EnemyDefinition[_spawnCount];
            for (int i = 0; i < _spawnCount; i++)
                _spawnDefinitions[i] = _encounters.DefinitionOf(_encounters.WaveEnemyAt(i));
            _spawnRoomKind = _encounters.CurrentRoom != null ? _encounters.CurrentRoom.Kind : RoomKind.Forge;
            _spawnRoomCount = _encounters.RoomCount;
            _spawnHealthMultiplier = _encounters.Floor != null ? _encounters.Floor.EnemyHealthMultiplier : 0f;
            _spawnDamageMultiplier = _encounters.Floor != null ? _encounters.Floor.EnemyDamageMultiplier : 0f;
            _spawnHasModifier = _encounters.Floor != null && _encounters.Floor.Modifier != null;
            _spawnHasNextFloor = _encounters.HasNextFloor;
            Steer();
        }

        // The proof floor asset against DescentSimulation.DensityProofTrailing, enemy by enemy: the comparison below is only
        // worth anything if the scene fights the wave the simulation fights. Everything the fight reads is checked, because
        // the asset is hand-written YAML that Unity had never parsed when it was authored.
        private void AssertTheProofWaveMatchesTheSimulation()
        {
            DescentSimulation.Enemy[][] waves = DescentSimulation.DensityProofTrailing.Rooms[0];
            DescentSimulation.Enemy[] pack = waves[0];
            Assert.That(_spawnRoomCount, Is.EqualTo(DescentSimulation.DensityProofTrailing.Rooms.Length),
                "One room, whose single wave clears the floor.");
            Assert.That(waves.Length, Is.EqualTo(1), "The simulation's proof floor holds one wave.");
            Assert.That(_spawnRoomKind, Is.EqualTo(RoomKind.Combat), "A normal room, not an elite, boss or forge room.");
            Assert.That(_spawnHealthMultiplier, Is.EqualTo(DescentSimulation.DensityProofTrailing.HealthMultiplier));
            Assert.That(_spawnDamageMultiplier, Is.EqualTo(DescentSimulation.DensityProofTrailing.DamageMultiplier));
            Assert.That(DescentSimulation.DensityProofTrailing.ModifierDamagePercent, Is.Zero);
            Assert.That(DescentSimulation.DensityProofTrailing.ModifierGoldPercent, Is.Zero);
            Assert.That(_spawnHasModifier, Is.False, "The simulation's proof floor carries no modifier.");
            Assert.That(_spawnHasNextFloor, Is.False, "Clearing the wave clears the floor and wins the run.");
            Assert.That(_spawnDefinitions.Length, Is.EqualTo(pack.Length), "The scene's wave is the simulation's wave.");

            for (int i = 0; i < pack.Length; i++)
            {
                EnemyDefinition definition = _spawnDefinitions[i];
                string where = $"Slot {i} is a {pack[i].Name}";
                Assert.That(definition, Is.Not.Null, where);
                Assert.That(definition.DisplayName, Is.EqualTo(pack[i].Name), where);
                Assert.That(definition.MaximumHealth, Is.EqualTo(pack[i].Health), where);
                Assert.That(definition.MoveSpeed, Is.EqualTo(pack[i].Speed), where);
                Assert.That(definition.GoldReward, Is.EqualTo(pack[i].Gold), where);
                Assert.That(definition.ExperienceReward, Is.EqualTo(pack[i].Experience), where);
                Assert.That(definition.Weapon.Range, Is.EqualTo(pack[i].Reach), where);
                Assert.That(definition.Weapon.Damage, Is.EqualTo(pack[i].Damage), where);
                Assert.That(definition.Weapon.Interval, Is.EqualTo(pack[i].Interval), where);
                Assert.That(definition.Weapon.InitialDelay, Is.EqualTo(pack[i].InitialDelay), where);
            }
        }

        private float ClosestEnemyDistance()
        {
            float heroX = _encounters.HeroFloorX;
            float heroY = _encounters.HeroFloorY;
            float closestSquared = float.MaxValue;
            for (int i = 0; i < _encounters.WaveEnemyCount; i++)
            {
                Vector3 at = _encounters.WaveEnemyAt(i).transform.position;
                float dx = at.x - heroX;
                float dy = ArenaFloor.FloorY(at.y) - heroY;
                float distanceSquared = dx * dx + dy * dy;
                if (distanceSquared < closestSquared)
                    closestSquared = distanceSquared;
            }
            return (float)Math.Sqrt(closestSquared);
        }

        // Fills the route's view from the scene at the end of a frame and holds the answer for the next one. A coroutine
        // resumes after every Update call of the frame, so the steer held here first moves the hero one frame later - the
        // same one-frame input lag DescentSimulation mirrors when it asks the route at the end of a frame and walks the
        // answer at the start of the next. A case with no route never touches HeroMovementInput at all.
        private void Steer()
        {
            if (_route == null)
                return;

            // The hero's transform is written from HeroMotion as (x, ArenaFloor.WorldY(y)) and every enemy's from PackMotion
            // the same way; halving and doubling are exact, so reading them back gives the simulation's floats, not near ones.
            Vector3 heroAt = _hero.transform.position;
            _view.HeroX = heroAt.x;
            _view.HeroY = ArenaFloor.FloorY(heroAt.y);
            _view.HeroReach = _setup.Weapon.Range;
            _view.Platform = _arena.Geometry;
            _view.Count = _encounters.WaveEnemyCount;
            // The proof floor is one wave, so the encounter's clock is the simulation's fight clock: both start at 0 on the
            // spawn frame and add the same fixed step on every frame after it. KiteRoute ignores it; TurningRound turns on it.
            _view.Seconds = _encounters.Elapsed;
            for (int i = 0; i < _view.Count; i++)
            {
                Health enemy = _encounters.WaveEnemyAt(i);
                Vector3 at = enemy.transform.position;
                _view.EnemyX[i] = at.x;
                _view.EnemyY[i] = ArenaFloor.FloorY(at.y);
                _view.EnemyReach[i] = _encounters.DefinitionOf(enemy).Weapon.Range;
                _view.Alive[i] = enemy.IsAlive;
            }

            _route.Steer(_view, out float steerX, out float steerY);
            _movement.Hold(new Vector2(steerX, steerY));
        }

        private void Observe()
        {
            if (!_arena.Geometry.IsOnPlatform(_encounters.HeroFloorX, _encounters.HeroFloorY, HeroMotion.EdgeMargin))
                _framesOffPlatform++;

            // Read back from the transforms, which PackMotion's floats reach exactly, so this is the spacing a player sees.
            for (int i = 0; i < _encounters.WaveEnemyCount; i++)
            {
                Health enemy = _encounters.WaveEnemyAt(i);
                if (!enemy.IsAlive)
                    continue;
                Vector3 at = enemy.transform.position;
                for (int j = 0; j < i; j++)
                {
                    Health other = _encounters.WaveEnemyAt(j);
                    if (!other.IsAlive)
                        continue;
                    Vector3 otherAt = other.transform.position;
                    float dx = at.x - otherAt.x;
                    float dy = ArenaFloor.FloorY(at.y) - ArenaFloor.FloorY(otherAt.y);
                    float apartSquared = dx * dx + dy * dy;
                    if (apartSquared < _closestEnemyGapSquared)
                        _closestEnemyGapSquared = apartSquared;
                }
            }
        }

        // The development hook the scene reads, written into this test's own profile folder, which TestProfile deletes.
        private static void WriteStartFloor(string floorId)
        {
            string path = DevelopmentStart.StartFloorPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, floorId + Environment.NewLine);
        }
    }
}
