using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The hero walks the arena by drag: the packs chase, the rim holds, and the chests on the floor open under its feet.
    public sealed class ArenaWalkTests
    {
        private CombatSetup _setup;
        private Health _hero;
        private HeroMovementInput _movement;
        private EncounterController _encounters;
        private ChestSpawner _chests;
        private CombatEffectsView _effects;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _movement = _hero.GetComponent<HeroMovementInput>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _chests = Object.FindFirstObjectByType<ChestSpawner>();
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Walk Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator TheHeroWalksAtItsSpeedThePackChasesAndTheRimHolds()
        {
            _hero.GetComponent<AttackController>().enabled = false;
            Health grunt = _encounters.WaveEnemyAt(0);
            Assert.That(_movement.IsMoving, Is.False);
            Assert.That(_hero.transform.position, Is.EqualTo(Vector3.zero), "The run starts at the arena's centre.");

            // Up the screen at full steer: depth shows at half length, so a second's walk of 2.5 floor units rises 1.25.
            _movement.Hold(new Vector2(0f, 1f));
            yield return new WaitForSeconds(1f);
            Assert.That(_movement.IsMoving, Is.True);
            Assert.That(_hero.transform.position.x, Is.EqualTo(0f).Within(1e-3f));
            Assert.That(_hero.transform.position.y, Is.EqualTo(1.25f).Within(0.15f));
            Assert.That(_encounters.HeroFloorY, Is.EqualTo(2.5f).Within(0.3f));
            _movement.Release();
            yield return null;
            Assert.That(_movement.IsMoving, Is.False, "Lifting the finger stops the hero.");

            // The Grunt from the far corner keeps walking toward wherever the hero stands, and stops inside its reach of it.
            float deadline = Time.realtimeSinceStartup + 10f;
            while (grunt.GetComponent<AttackController>().AttackCount == 0 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Vector3 offset = grunt.transform.position - _hero.transform.position;
            Assert.That(ArenaFloor.DistanceSquared(offset.x, offset.y), Is.LessThanOrEqualTo(1.5f * 1.5f), "The Grunt caught up and struck.");

            // Walking right for long enough ends at the rim, inside the platform's margin, never in the void.
            _movement.Hold(new Vector2(1f, 0f));
            yield return new WaitForSeconds(4.5f);
            _movement.Release();
            Assert.That(_hero.transform.position.x, Is.EqualTo(9f - HeroMotion.EdgeMargin).Within(0.15f));
            Assert.That(Object.FindFirstObjectByType<ArenaView>().IsOnPlatform(_encounters.HeroFloorX, _encounters.HeroFloorY), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        // With the hero standing at the right corner, the next wave enters only from where the platform is, out of reach, and
        // every enemy walks in and stops on the platform to strike: none steps over the void.
        [UnityTest]
        public IEnumerator AtTheRimTheNextPackEntersFromThePlatformAndEveryEnemyStopsOnIt()
        {
            _hero.GetComponent<AttackController>().enabled = false;
            ArenaView arena = Object.FindFirstObjectByType<ArenaView>();
            _movement.Hold(new Vector2(1f, 0f));
            yield return new WaitForSeconds(4f);
            _movement.Release();
            float heroX = _encounters.HeroFloorX;
            float heroY = _encounters.HeroFloorY;
            Assert.That(heroX, Is.EqualTo(9f - HeroMotion.EdgeMargin).Within(0.15f), "The hero stands at the right corner.");

            // The first pack falls, the level-up is taken, and the next wave enters with the hero still at the corner.
            int firstWave = _encounters.WaveNumber;
            PackTestUtility.KillWave(_encounters);
            float deadline = Time.realtimeSinceStartup + 10f;
            while ((_encounters.WaveNumber == firstWave || !PackTestUtility.AnyAlive(_encounters)) && Time.realtimeSinceStartup < deadline)
            {
                while (_setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade)
                    _setup.Choices.TrySelect(_setup.Choices.Current, 0);
                yield return null;
            }
            Assert.That(_encounters.WaveNumber, Is.EqualTo(firstWave + 1), "The next wave entered.");

            int count = _encounters.WaveEnemyCount;
            for (int i = 0; i < count; i++)
            {
                Vector3 at = _encounters.WaveEnemyAt(i).transform.position;
                float x = at.x;
                float y = ArenaFloor.FloorY(at.y);
                Assert.That(arena.Geometry.IsOnPlatform(x, y, HeroMotion.EdgeMargin), Is.True, $"Enemy {i} enters on the platform.");
                Assert.That(x, Is.LessThan(heroX), $"Enemy {i} enters from the platform's side of the hero.");
                float dx = x - heroX;
                float dy = y - heroY;
                Assert.That(dx * dx + dy * dy, Is.GreaterThan(4.5f * 4.5f), $"Enemy {i} enters out of reach.");
            }

            deadline = Time.realtimeSinceStartup + 10f;
            bool allStruck = false;
            while (!allStruck && Time.realtimeSinceStartup < deadline)
            {
                allStruck = true;
                for (int i = 0; i < count; i++)
                {
                    Health enemy = _encounters.WaveEnemyAt(i);
                    Vector3 at = enemy.transform.position;
                    Assert.That(arena.Geometry.IsOnPlatform(at.x, ArenaFloor.FloorY(at.y), HeroMotion.EdgeMargin - 0.01f), Is.True,
                        $"Enemy {i} never steps over the void.");
                    if (enemy.GetComponent<AttackController>().AttackCount == 0)
                        allStruck = false;
                }
                yield return null;
            }
            Assert.That(allStruck, Is.True, "Every enemy reached the hero at the corner and struck.");
            Assert.That(_encounters.HeroFloorX, Is.EqualTo(heroX), "The hero never moved.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheFirstRoomsChestMendsAQuarterWhenTheHeroWalksOntoIt()
        {
            _hero.GetComponent<AttackController>().enabled = false;
            Assert.That(_chests.HasChest, Is.True, "A chest waits from the first wave.");
            Assert.That(_chests.IsOpen, Is.False);
            Assert.That((_chests.ChestFloorX, _chests.ChestFloorY), Is.EqualTo((1.8f, 0f)), "Room 1's chest stands to the right of the centre.");
            var chest = GameObject.Find("Chest").GetComponent<SpriteRenderer>();
            Assert.That(chest.enabled, Is.True);
            Assert.That(chest.sprite.name, Is.EqualTo("Chest Closed"));
            Assert.That(chest.transform.position, Is.EqualTo(new Vector3(1.8f, 0f, 0f)));

            _hero.ApplyDamage(new DamageContext(40f));
            float wounded = _hero.Current;
            _movement.Hold(new Vector2(1f, 0f));
            float deadline = Time.realtimeSinceStartup + 6f;
            while (!_chests.IsOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            _movement.Release();

            Assert.That(_chests.IsOpen, Is.True, "Walking onto the chest opens it.");
            Assert.That(_chests.ChestsOpened, Is.EqualTo(1));
            Assert.That(chest.sprite.name, Is.EqualTo("Chest Open"));
            Assert.That(_hero.Current, Is.GreaterThan(wounded), "The first room's chest mends.");
            Assert.That(_effects.ActiveNumberCount, Is.GreaterThan(0), "The heal shows its amount.");
            Assert.That(_setup.Run.Gold, Is.Zero, "No gold from a mending chest.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheSecondRoomsChestPaysGoldAtTheFloorsRate()
        {
            _hero.GetComponent<AttackController>().enabled = false;
            float deadline = Time.realtimeSinceStartup + 45f;
            while (_encounters.RoomNumber < 2 && Time.realtimeSinceStartup < deadline)
            {
                while (_setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade)
                    _setup.Choices.TrySelect(_setup.Choices.Current, 0);
                if (!_encounters.IsInNonCombatRoom && PackTestUtility.AnyAlive(_encounters))
                    PackTestUtility.KillWave(_encounters);
                yield return null;
            }
            Assert.That(_encounters.RoomNumber, Is.EqualTo(2), "Cinder Walk was reached.");
            yield return null;
            Assert.That((_chests.ChestFloorX, _chests.ChestFloorY), Is.EqualTo((0f, 1.8f)), "Room 2's chest stands ahead of the centre.");
            Assert.That(_chests.IsOpen, Is.False);
            int gold = _setup.Run.Gold;

            _movement.Hold(new Vector2(0f, 1f));
            deadline = Time.realtimeSinceStartup + 6f;
            while (!_chests.IsOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            _movement.Release();

            Assert.That(_chests.IsOpen, Is.True);
            Assert.That(_setup.Run.Gold, Is.EqualTo(gold + 10), "Ten gold on floor 1, where gold is not modified.");
            Assert.That(_setup.Run.UnsecuredGold, Is.GreaterThanOrEqualTo(10), "Chest gold is at risk like any other.");
        }
    }
}
