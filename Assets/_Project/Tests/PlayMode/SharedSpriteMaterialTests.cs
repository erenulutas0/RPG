using System.Collections;
using System.IO;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The views that draw plain lines and sprites - the run's feedback, the Warden's mark, the altars, the exit seal and the
    // pit's band - share one Sprites/Default material, the arena's (the built-in Sprites-Default every combat effect draws
    // with), rather than each making a copy of its own (SharedSpriteMaterial). Every LineRenderer in the game is one of
    // theirs, so every line on the floor, in the foundry and in an ordinary hall, draws with that one material, and so do
    // the run feedback's painted rings and sparks.
    public sealed class SharedSpriteMaterialTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Shared sprite material cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator TheFoundrysLinesAndPaintedFeedbackDrawWithTheArenasMaterial()
        {
            yield return AssertShared("foundry");
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AHallsLinesAndPaintedFeedbackDrawWithTheArenasMaterial()
        {
            yield return AssertShared("hall");
        }

        private static IEnumerator AssertShared(string region)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, region);
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;

            var arena = Object.FindFirstObjectByType<ArenaView>();
            Material shared = arena.Material;
            Assert.That(shared, Is.Not.Null);
            Assert.That(shared.shader.name, Is.EqualTo("Sprites/Default"));
            Assert.That(SharedSpriteMaterial.For(arena), Is.SameAs(shared), "The arena's own material is the shared one.");

            int lines = 0;
            foreach (var line in Object.FindObjectsByType<LineRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Assert.That(line.sharedMaterial, Is.SameAs(shared), line.name + " draws with the arena's material, not a copy.");
                lines++;
            }
            Assert.That(lines, Is.GreaterThan(0), "The exit seal and the Warden's mark are made up front.");

            var feedback = Object.FindFirstObjectByType<RunFeedbackView>();
            Assert.That(feedback, Is.Not.Null);
            Transform root = feedback.transform.Find("Run Feedback");
            Assert.That(root, Is.Not.Null);
            int slots = 0;
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                Assert.That(sprite.sharedMaterial, Is.SameAs(shared), sprite.name + " draws with the arena's material, not a copy.");
                slots++;
            }
            Assert.That(slots + lines, Is.GreaterThan(0));
        }
    }
}
