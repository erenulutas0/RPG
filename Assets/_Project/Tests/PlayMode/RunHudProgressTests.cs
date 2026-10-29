using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using UnityEngine.UI;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Cryptforge.Tests
{
    // The HUD hears of every hit any enemy of the wave takes (EncounterController.ProgressChanged), several a frame in a
    // crowd and twice a second for every burning enemy. A hit that changes nothing the HUD shows - on an enemy that is not
    // the hero's target - formats no label and allocates nothing on its way through the views; a hit on the target still
    // shows its health on the frame it lands, and the floor and wave lines read as they did.
    public sealed class RunHudProgressTests
    {
        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Run HUD progress cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private static Text Label(string name) => GameObject.Find(name).GetComponent<Text>();

        [UnityTest, Timeout(120000)]
        public IEnumerator AHitThatChangesNothingTheHudShowsFormatsNothing()
        {
            var encounters = Object.FindFirstObjectByType<EncounterController>();
            for (int frame = 0; frame < 600 && encounters.AliveEnemyCount < 2; frame++)
                yield return null;
            Assert.That(encounters.AliveEnemyCount, Is.GreaterThanOrEqualTo(2), "The proof floor's wave stands.");
            // Nothing strikes from here on: the only hits are the test's.
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            yield return null;

            Health target = encounters.CurrentEnemy;
            Health other = null;
            for (int i = 0; i < encounters.WaveEnemyCount && other == null; i++)
            {
                Health enemy = encounters.WaveEnemyAt(i);
                if (enemy != null && enemy.IsAlive && enemy != target)
                    other = enemy;
            }
            Assert.That(target, Is.Not.Null);
            Assert.That(other, Is.Not.Null);
            Text floor = Label("Floor Label");
            Text enemyLabel = Label("Enemy Label");
            Text status = Label("Status Label");

            // The first hit makes the enemy's flash overlay; every later one is the steady state.
            other.ApplyDamage(new DamageContext(1f));
            string floorText = floor.text, enemyText = enemyLabel.text, statusText = status.text;
            Assert.That(() => other.ApplyDamage(new DamageContext(1f)), Is.Not.AllocatingGCMemory());
            Assert.That(encounters.CurrentEnemy, Is.SameAs(target), "The hit left the hero's target as it was.");
            Assert.That(floor.text, Is.EqualTo(floorText));
            Assert.That(enemyLabel.text, Is.EqualTo(enemyText));
            Assert.That(status.text, Is.EqualTo(statusText));
            Assert.That(floor.text, Does.StartWith("Floor "));
            Assert.That(status.text, Does.StartWith("Wave "));

            // The target's health is shown the moment it changes.
            target.ApplyDamage(new DamageContext(1f));
            Assert.That(enemyLabel.text, Is.Not.EqualTo(enemyText));
            Assert.That(enemyLabel.text, Does.Contain(target.Current.ToString("0") + " / " + target.Maximum.ToString("0") + " HP"));
            yield return null;
        }
    }
}
