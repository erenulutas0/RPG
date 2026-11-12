using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // Floor 2's dress is worked out while floor 1 plays (FoundryRegionView.PrepareTheme, started by the encounter as floor 1
    // starts with an ice floor next): floor 1's look is untouched meanwhile and no ice prop is built, it is ready within a
    // few seconds of frames, and the change then dresses the region as before.
    public sealed class IceThemePreparationTests
    {
        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Ice preparation cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator FloorOnePreparesTheIceDressAndTheChangeOnlyBindsIt()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.IsFoundryRegion, Is.True);
            Assert.That(encounters.Floor.NextFloor, Is.Not.Null);
            Assert.That(encounters.Floor.NextFloor.Theme, Is.EqualTo(FloorTheme.Ice));
            encounters.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;

            int frames = 0;
            while (!arena.IsFloorThemeReady(FloorTheme.Ice) && frames < 600)
            {
                frames++;
                yield return null;
            }
            Assert.That(arena.IsFloorThemeReady(FloorTheme.Ice), Is.True, "The ice dress is ready within 600 frames of floor 1.");
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Foundry), "Preparing changes nothing on show.");
            Assert.That(arena.Region.transform.Find("Ice crystals and icicles"), Is.Null, "Floor 1 builds no ice prop.");

            arena.ApplyFloorTheme(FloorTheme.Ice);
            yield return null;
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Ice));
            Assert.That(arena.Region.Theme, Is.EqualTo(FloorTheme.Ice));
            var crystals = arena.Region.transform.Find("Ice crystals and icicles");
            Assert.That(crystals != null && crystals.gameObject.activeSelf, Is.True, "The change stands the crystals.");
            var block = new MaterialPropertyBlock();
            crystals.GetComponent<MeshRenderer>().GetPropertyBlock(block);
            Assert.That(block.GetTexture("_MainTex"), Is.EqualTo(Texture2D.whiteTexture), "The crystals take the coping's untextured dress.");
            int textured = 0;
            foreach (var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.GetPropertyBlock(block);
                if (!block.isEmpty) textured++;
            }
            Assert.That(textured, Is.GreaterThan(1), "The region's renderers are dressed for the ice.");

            arena.ApplyFloorTheme(FloorTheme.Foundry);
            yield return null;
            Assert.That(crystals.gameObject.activeSelf, Is.False);
        }
    }
}
