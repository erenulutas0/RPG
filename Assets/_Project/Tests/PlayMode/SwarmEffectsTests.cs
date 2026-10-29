using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // A swarm room starts its encounter once and then fills fallen places for its whole life (EncounterController's
    // SwarmEnemySpawned): the effects follow every newcomer, so a refill's hits draw their numbers and its fall its
    // remains, as the opening ladder's do.
    public sealed class SwarmEffectsTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_swarm_proof");
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Swarm effects cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // Nobody strikes but the test, and the hero stays on its feet.
        private static void Hold(Health hero)
        {
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            if (hero.IsAlive)
                hero.Heal(hero.Maximum);
        }

        [UnityTest, Timeout(60000)]
        public IEnumerator ARefillsHitIsDrawnLikeTheOpeningWaves()
        {
            var encounters = Object.FindFirstObjectByType<EncounterController>();
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            float deadline = Time.realtimeSinceStartup + 20f;
            while (!(encounters.IsInSwarmRoom && encounters.WaveEnemyCount > 0) && Time.realtimeSinceStartup < deadline)
            {
                Hold(hero);
                yield return null;
            }
            Assert.That(encounters.IsInSwarmRoom, Is.True, "The proof floor opens on its swarm.");

            var opening = new HashSet<Health>();
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
                opening.Add(encounters.WaveEnemyAt(i));
            Health first = encounters.WaveEnemyAt(0);
            first.ApplyDamage(new DamageContext(first.Maximum * 10f));
            Assert.That(first.IsAlive, Is.False);

            Health newcomer = null;
            deadline = Time.realtimeSinceStartup + 20f;
            while (newcomer == null && Time.realtimeSinceStartup < deadline)
            {
                Hold(hero);
                yield return null;
                for (int i = 0; i < encounters.WaveEnemyCount; i++)
                {
                    Health enemy = encounters.WaveEnemyAt(i);
                    if (enemy != null && enemy.IsAlive && !opening.Contains(enemy))
                        newcomer = enemy;
                }
            }
            Assert.That(newcomer, Is.Not.Null, "The swarm fills the fallen place.");
            // Let the numbers of the opening kill play out, so the newcomer's own is the one counted.
            deadline = Time.realtimeSinceStartup + 5f;
            while (effects.ActiveNumberCount > 0 && Time.realtimeSinceStartup < deadline)
            {
                Hold(hero);
                yield return null;
            }
            int before = effects.ActiveNumberCount;
            newcomer.ApplyDamage(new DamageContext(3f));
            Assert.That(effects.ActiveNumberCount, Is.EqualTo(before + 1), "The refill's hit shows its number.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
