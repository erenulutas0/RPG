using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The development depth proof (DepthProofArrangement): with its flag the foundry stops on arrival with an enemy behind
    // the south-west boiler, the hero in front of it and a second enemy in the open court, and nothing then moves, falls,
    // pays or saves. Both enemies take spark-only strikes, healed at once, whose sparks stand on each one's feet. Without
    // the flag, or outside the foundry, the scene starts exactly as it always has. Every flag lives in the test's own
    // profile folder.
    public sealed class DepthProofArrangementTests
    {
        private const string GameplayScene = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The south-west boiler in the design plane (FoundryRegionView's BoilerCentres[2]) and its plinth's radius. Its
        // depth footprint is a square round that centre, whose left-right diagonal is level on screen at the centre's
        // height: feet above that height within the plinth's width stand behind the boiler, feet below it in front.
        private static readonly Vector2 SouthWestBoiler = new Vector2(17.9f, -4.08f);
        private const float BoilerPlinth = 2f;
        private Scene _scene;

        [SetUp] public void Begin() => TestProfile.Begin();

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1f;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Depth proof cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            // The profile folder goes with TestProfile.End; the flags go first, so no later load can meet them.
            Delete(DevelopmentStart.DepthProofPath);
            Delete(DevelopmentStart.RegionPreviewPath);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator FlagStopsTheFoundryWithAnEnemyBehindTheSouthWestBoiler()
        {
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            Write(DevelopmentStart.DepthProofPath, "on");
            LogAssert.Expect(LogType.Log, new Regex(@"^\[DepthProof\] arranged "));
            yield return Load();

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            var movement = Object.FindFirstObjectByType<HeroMovementInput>();
            var hero = movement.GetComponent<Health>();
            var chests = Object.FindFirstObjectByType<ChestSpawner>();
            EncounterController encounters = setup.Encounters;
            Assert.That(arena.IsFoundryRegion, Is.True);
            DepthProofArrangement proof = Proof();
            Assert.That(proof, Is.Not.Null, "The flag arms the arrangement on the foundry.");
            Health hidden = proof.Hidden;
            Assert.That(hidden, Is.Not.Null, "The first wave comes with the scene, so the arrangement is made at once.");
            Assert.That(InWave(encounters, hidden), Is.True, "The hidden enemy is one of the wave.");

            // Its feet stand where the capture puts the enemy: design (18.5, -1.2) on the apron behind the boiler.
            FoundryMap.ToFloor(18.5f, -1.2f, out float enemyX, out float enemyY);
            Vector3 feet = hidden.transform.position;
            Assert.That(feet.x, Is.EqualTo(enemyX).Within(1e-4f));
            Assert.That(feet.y, Is.EqualTo(ArenaFloor.WorldY(enemyY)).Within(1e-4f));
            AssertBehindBoiler(feet, "The enemy");

            Assert.That(chests.HasChest, Is.True, "The foundry's first room has a chest.");
            Assert.That(chests.enabled, Is.False, "Walking onto the chest opens nothing.");
            AssertBehindBoiler(chests.Body.transform.position, "The chest");
            Assert.That(setup.Drops.Count, Is.EqualTo(1));
            GroundDrops.Drop drop = setup.Drops.At(0);
            AssertBehindBoiler(new Vector3(drop.X, ArenaFloor.WorldY(drop.Y)), "The drop");
            Assert.That(setup.GetComponent<GroundDropView>().ShownCount, Is.EqualTo(1), "The drop is on show.");

            // The hero through its walk and its transform alike, on open floor in front of the boiler.
            FoundryMap.ToFloor(13f, -7.6f, out float heroX, out float heroY);
            Assert.That(movement.Motion.X, Is.EqualTo(heroX).Within(1e-4f));
            Assert.That(movement.Motion.Y, Is.EqualTo(heroY).Within(1e-4f));
            Assert.That(movement.transform.position.x, Is.EqualTo(heroX).Within(1e-4f));
            Assert.That(movement.transform.position.y, Is.EqualTo(ArenaFloor.WorldY(heroY)).Within(1e-4f));
            Assert.That(arena.Geometry.IsOnPlatform(heroX, heroY, HeroMotion.EdgeMargin), Is.True);
            BoilerOnScreen(out float boilerX, out float boilerY);
            Assert.That(Mathf.Abs(movement.transform.position.x - boilerX), Is.LessThan(BoilerPlinth));
            Assert.That(movement.transform.position.y, Is.LessThan(boilerY - ArenaFloor.WorldY(BoilerPlinth)),
                "The hero stands in front of the boiler's plinth.");

            // The wave's second enemy on open court floor in front of the hero, where nothing covers it, clear of him.
            Health exposed = proof.Exposed;
            Assert.That(exposed, Is.Not.Null, "The foundry's first wave has a second enemy for the open court.");
            Assert.That(exposed, Is.Not.SameAs(hidden));
            Assert.That(InWave(encounters, exposed), Is.True, "The court enemy is one of the wave.");
            FoundryMap.ToFloor(11f, -8.6f, out float courtX, out float courtY);
            Vector3 exposedFeet = exposed.transform.position;
            Assert.That(exposedFeet.x, Is.EqualTo(courtX).Within(1e-4f));
            Assert.That(exposedFeet.y, Is.EqualTo(ArenaFloor.WorldY(courtY)).Within(1e-4f));
            Assert.That(arena.Geometry.IsOnPlatform(courtX, courtY, HeroMotion.EdgeMargin), Is.True);
            Assert.That(exposedFeet.y, Is.LessThan(movement.transform.position.y), "The court enemy stands in front of the hero.");
            Assert.That(Vector2.Distance(exposedFeet, movement.transform.position), Is.GreaterThan(1f),
                "The court enemy stands clear of the hero.");

            Assert.That(encounters.enabled, Is.False);
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                Assert.That(attack.enabled, Is.False, attack.name + " must not strike.");

            // A second of play: nothing walks, attacks, falls, opens, pays or moves on.
            int alive = encounters.AliveEnemyCount;
            int room = encounters.RoomNumber;
            int gold = setup.Run.Gold;
            float health = hero.Current;
            Vector3 heroSpot = movement.transform.position;
            Vector3 chestSpot = chests.Body.transform.position;
            var spots = new Vector3[encounters.WaveEnemyCount];
            for (int i = 0; i < spots.Length; i++)
                spots[i] = encounters.WaveEnemyAt(i).transform.position;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return new WaitForSeconds(1f);
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            Assert.That(hidden.IsAlive, Is.True);
            for (int i = 0; i < spots.Length; i++)
            {
                Vector3 now = encounters.WaveEnemyAt(i).transform.position;
                Assert.That(now.x, Is.EqualTo(spots[i].x), "No enemy walks.");
                Assert.That(now.y, Is.EqualTo(spots[i].y), "No enemy walks.");
            }
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(alive));
            Assert.That(encounters.RoomNumber, Is.EqualTo(room));
            Assert.That(encounters.IsFloorCleared, Is.False);
            Assert.That(setup.Run.HasEnded, Is.False);
            Assert.That(setup.Run.Gold, Is.EqualTo(gold));
            Assert.That(setup.Drops.Count, Is.EqualTo(1), "The drop is never picked up.");
            Assert.That(chests.ChestsOpened, Is.Zero);
            Assert.That(chests.Body.transform.position.x, Is.EqualTo(chestSpot.x));
            Assert.That(chests.Body.transform.position.y, Is.EqualTo(chestSpot.y));
            Assert.That(hero.Current, Is.EqualTo(health), "Nobody strikes the hero.");
            Assert.That(movement.transform.position, Is.EqualTo(heroSpot));
            // Nor can anything fell the pack, so no last kill can clear the floor.
            hidden.ApplyDamage(new DamageContext(hidden.Maximum * 10f));
            Assert.That(hidden.IsAlive, Is.True);
            Assert.That(TestProfile.HasSavedFile, Is.False, "Nothing is banked or saved.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheStrikesShowASparkAtEachEnemysFeetAndChangeNothing()
        {
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            Write(DevelopmentStart.DepthProofPath, "on");
            LogAssert.Expect(LogType.Log, new Regex(@"^\[DepthProof\] arranged "));
            yield return Load();

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            var chests = Object.FindFirstObjectByType<ChestSpawner>();
            EncounterController encounters = setup.Encounters;
            DepthProofArrangement proof = Proof();
            Health hidden = proof.Hidden;
            Health exposed = proof.Exposed;
            Assert.That(hidden, Is.Not.Null);
            Assert.That(exposed, Is.Not.Null);
            AssertBehindBoiler(hidden.transform.position, "The hidden enemy");
            // The floor each enemy stands on, which is the ground its effects stand on and the foundry's depth for them.
            float hiddenFeet = hidden.transform.position.y;
            float exposedFeet = exposed.transform.position.y;

            int alive = encounters.AliveEnemyCount;
            int room = encounters.RoomNumber;
            int wave = encounters.WaveNumber;
            int gold = setup.Run.Gold;
            int experience = setup.Run.Experience;
            float heroHealth = hero.Current;
            int strikes = proof.Strikes;

            // A few seconds of strikes, looked at on every frame. Each shows a spark above the floor standing on the struck
            // enemy's feet - the hidden one's behind the boiler, the other's in the open court - and no number, and nothing
            // else is struck.
            bool sparkBehind = false;
            bool sparkInCourt = false;
            float watched = 10f * DepthProofArrangement.StrikeInterval;
            for (float elapsed = 0f; elapsed < watched; elapsed += Time.deltaTime)
            {
                yield return null;
                for (int slot = 0; slot < effects.EffectSlotCount; slot++)
                {
                    if (effects.AboveFloorEffect(slot, out float groundY) == null)
                        continue;
                    sparkBehind |= groundY == hiddenFeet;
                    sparkInCourt |= groundY == exposedFeet;
                    Assert.That(groundY == hiddenFeet || groundY == exposedFeet, Is.True,
                        "Only the two enemies of the proof are struck.");
                }
                Assert.That(effects.ActiveNumberCount, Is.Zero, "A strike rounds to no number.");
            }
            Assert.That(sparkBehind, Is.True, "A strike's spark stands on the hidden enemy's feet, where the boiler covers it.");
            Assert.That(sparkInCourt, Is.True, "A strike's spark stands on the court enemy's feet, in the open.");
            Assert.That(proof.Strikes - strikes, Is.GreaterThanOrEqualTo(5), "The strikes keep coming.");

            // Healed at once, they fell nobody, pay nothing and move nothing on.
            Assert.That(hidden.Current, Is.EqualTo(hidden.Maximum), "Each strike is healed at once.");
            Assert.That(exposed.Current, Is.EqualTo(exposed.Maximum), "Each strike is healed at once.");
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(alive));
            Assert.That(encounters.RoomNumber, Is.EqualTo(room));
            Assert.That(encounters.WaveNumber, Is.EqualTo(wave));
            Assert.That(encounters.IsCleared, Is.False);
            Assert.That(encounters.IsFloorCleared, Is.False);
            Assert.That(setup.Run.HasEnded, Is.False);
            Assert.That(setup.Run.Gold, Is.EqualTo(gold));
            Assert.That(setup.Run.Experience, Is.EqualTo(experience));
            Assert.That(hero.Current, Is.EqualTo(heroHealth), "Nobody strikes the hero.");
            Assert.That(setup.Drops.Count, Is.EqualTo(1), "The drop is never picked up.");
            Assert.That(chests.ChestsOpened, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(TestProfile.HasSavedFile, Is.False, "Nothing is banked or saved.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithoutTheFlagTheFoundryStartsAsEver()
        {
            Write(DevelopmentStart.RegionPreviewPath, "foundry");
            Assert.That(File.Exists(DevelopmentStart.DepthProofPath), Is.False, "This test writes no depth-proof file.");
            yield return Load();

            Assert.That(Object.FindFirstObjectByType<ArenaView>().IsFoundryRegion, Is.True);
            AssertUntouched();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator OutsideTheFoundryTheFlagDoesNothing()
        {
            Write(DevelopmentStart.DepthProofPath, "on");
            yield return Load();

            Assert.That(Object.FindFirstObjectByType<ArenaView>().IsFoundryRegion, Is.False);
            AssertUntouched();
            LogAssert.NoUnexpectedReceived();
        }

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync(GameplayScene);
            _scene = SceneManager.GetActiveScene();
            // The arrangement is made in the first frame's update, before anything else moves.
            yield return null;
            yield return null;
        }

        // The ordinary start: no arrangement exists, the fight runs, nothing is disabled, dropped or moved to a proof spot.
        private static void AssertUntouched()
        {
            Assert.That(Object.FindObjectsByType<DepthProofArrangement>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                Is.Empty);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounters = setup.Encounters;
            Assert.That(encounters.enabled, Is.True);
            Assert.That(encounters.WaveEnemyCount, Is.GreaterThan(0));
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                Assert.That(attack.enabled, Is.True);
            FoundryMap.ToFloor(18.5f, -1.2f, out float enemyX, out float enemyY);
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                Health enemy = encounters.WaveEnemyAt(i);
                Assert.That(enemy.enabled, Is.True);
                Vector3 p = enemy.transform.position;
                Assert.That(Vector2.Distance(new Vector2(p.x, p.y), new Vector2(enemyX, ArenaFloor.WorldY(enemyY))),
                    Is.GreaterThan(1f), "No enemy is moved behind the boiler.");
            }
            Assert.That(setup.Drops.Count, Is.Zero);
            Assert.That(Object.FindFirstObjectByType<ChestSpawner>().enabled, Is.True);
            var movement = Object.FindFirstObjectByType<HeroMovementInput>();
            FoundryMap.ToFloor(13f, -7.6f, out float heroX, out float heroY);
            Assert.That(Vector2.Distance(new Vector2(movement.Motion.X, movement.Motion.Y), new Vector2(heroX, heroY)),
                Is.GreaterThan(1f), "The hero starts where the scene puts him.");
        }

        private static bool InWave(EncounterController encounters, Health enemy)
        {
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
            {
                if (encounters.WaveEnemyAt(i) == enemy)
                    return true;
            }
            return false;
        }

        private static void AssertBehindBoiler(Vector3 feet, string what)
        {
            BoilerOnScreen(out float boilerX, out float boilerY);
            Assert.That(Mathf.Abs(feet.x - boilerX), Is.LessThan(BoilerPlinth), what + " stands within the boiler's width.");
            Assert.That(feet.y, Is.GreaterThan(boilerY), what + " stands behind the boiler.");
            Assert.That(feet.y, Is.LessThan(boilerY + ArenaFloor.WorldY(2f * BoilerPlinth)), what + " stands close behind it.");
        }

        private static void BoilerOnScreen(out float x, out float y)
        {
            FoundryMap.ToFloor(SouthWestBoiler.x, SouthWestBoiler.y, out x, out float floorY);
            y = ArenaFloor.WorldY(floorY);
        }

        private static DepthProofArrangement Proof()
        {
            GameObject holder = GameObject.Find(DepthProofArrangement.ObjectName);
            return holder != null ? holder.GetComponent<DepthProofArrangement>() : null;
        }

        private static void Write(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, text);
        }

        private static void Delete(string path)
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
