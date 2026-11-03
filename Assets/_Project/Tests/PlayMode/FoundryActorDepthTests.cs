using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Cryptforge.Tests
{
    // The foundry places every actor on show in depth for each camera render (FoundryVisibilityView.ApplyActorDepth) and
    // puts it back after (RestoreActorDepth), so the pass runs every frame for the hero, every enemy and every drop. Once
    // its lists have grown and its silhouette materials exist it allocates nothing: reading each child's name to find the
    // hero's ability ring allocated a string for every child of every actor, a few kilobytes a frame with a crowd on the
    // floor. That the ring and the shadows still lie on the floor is FoundryElevationTests' (the ramp).
    public sealed class FoundryActorDepthTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry actor depth cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PlacingTheActorsInDepthAllocatesNothing()
        {
            var encounters = Object.FindFirstObjectByType<EncounterController>();
            for (int frame = 0; frame < 600 && encounters.AllAliveCount == 0; frame++)
                yield return null;
            Assert.That(encounters.AllAliveCount, Is.GreaterThan(0), "The proof floor's wave stands on the floor.");
            // Every newcomer's Start has run: its contact shadow and its bar are in place.
            for (int frame = 0; frame < 5; frame++)
                yield return null;

            var visibility = Object.FindFirstObjectByType<FoundryVisibilityView>();
            Assert.That(visibility, Is.Not.Null, "The foundry places its actors in depth.");
            Transform hero = Object.FindFirstObjectByType<HeroLookView>().transform;
            Assert.That(hero.Find(AbilityRingView.RingName), Is.Not.Null, "The hero carries its ability ring.");

            // The first pass makes the silhouette materials and grows the lists; every later frame pays the steady state.
            visibility.ApplyActorDepth();
            int placed = visibility.PlacedCount;
            visibility.RestoreActorDepth();
            Assert.That(placed, Is.GreaterThanOrEqualTo(encounters.AllAliveCount * 3),
                "Each enemy's body, bar and contact shadow are placed in depth.");
            Assert.That(() =>
            {
                visibility.ApplyActorDepth();
                visibility.RestoreActorDepth();
            }, Is.Not.AllocatingGCMemory());

            visibility.ApplyActorDepth();
            Assert.That(visibility.PlacedCount, Is.EqualTo(placed), "The same actors and effects are placed on every pass.");
            visibility.RestoreActorDepth();

            // The pass as the gameplay camera's render makes it, deciding each body's material from the depth cover
            // (FoundryDepthCover), allocates nothing either once the bodies wear what that render gives them.
            Camera camera = Camera.main;
            Assert.That(camera, Is.Not.Null);
            visibility.ApplyActorDepth(camera);
            TestContext.Out.WriteLine("The proof floor's wave for the gameplay camera: " + visibility.DescribeGate());
            Assert.That(visibility.IsGated, Is.True,
                "The gameplay camera sees the region flat, and the cover is made: " + visibility.DescribeGate());
            Assert.That(visibility.ShownBodies, Is.GreaterThanOrEqualTo(encounters.AllAliveCount));
            visibility.RestoreActorDepth();
            Assert.That(() =>
            {
                visibility.ApplyActorDepth(camera);
                visibility.RestoreActorDepth();
            }, Is.Not.AllocatingGCMemory());
        }
    }
}
