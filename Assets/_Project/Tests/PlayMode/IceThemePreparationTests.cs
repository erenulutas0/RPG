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
            Assert.That(arena.IsPreparingFloorTheme || arena.IsFloorThemeReady(FloorTheme.Ice), Is.True,
                "Floor 1's start asks for floor 2's dress, whichever of CombatSetup and ArenaView woke first.");
            encounters.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;

            int frames = 0;
            while (!arena.IsFloorThemeReady(FloorTheme.Ice) && frames < 600)
            {
                frames++;
                yield return null;
            }
            Assert.That(arena.IsFloorThemeReady(FloorTheme.Ice), Is.True, "The ice dress is ready within 600 frames of floor 1 " +
                "(preparing: " + arena.IsPreparingFloorTheme + ").");
            Debug.Log("[IceThemePreparation] the ice dress was ready " + frames + " frames after floor 1's first frame.");
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Foundry), "Preparing changes nothing on show.");
            // The ice props are built during the preparation, hidden, so the change only shows them.
            foreach (string prop in new[] { "Ice crystals and icicles", "Frozen mirror panels", "Snow drifts" })
            {
                Transform built = arena.Region.transform.Find(prop);
                Assert.That(built, Is.Not.Null, prop + " is built during floor 1.");
                Assert.That(built.gameObject.activeSelf, Is.False, "Floor 1 never shows " + prop + ".");
            }
            // The region's meshes keep the foundry's colours for good; no renderer carries a theme stream yet.
            foreach (var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>(true))
                Assert.That(renderer.additionalVertexStreams, Is.Null, renderer.name + " carries no theme colours on floor 1.");

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
            int streamed = 0;
            foreach (var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if (renderer.additionalVertexStreams != null)
                {
                    streamed++;
                    Assert.That(renderer.additionalVertexStreams.vertexCount,
                        Is.EqualTo(renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount), renderer.name + "'s stream fits its mesh.");
                }
            Assert.That(streamed, Is.GreaterThan(1), "The ice's colours ride on the renderers' streams.");

            arena.ApplyFloorTheme(FloorTheme.Foundry);
            yield return null;
            Assert.That(crystals.gameObject.activeSelf, Is.False);
            foreach (var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                Assert.That(renderer.additionalVertexStreams, Is.Null, renderer.name + " is back on the foundry's colours.");
                renderer.GetPropertyBlock(block);
                Assert.That(block.isEmpty, Is.True, renderer.name + " wears its own material again.");
            }
            // The ice's variants go over the frames after the change, never inside it.
            int released = 0;
            while (arena.Region.HeldVariantCount(FloorTheme.Ice) > 0 && released++ < 120)
                yield return null;
            Assert.That(arena.Region.HeldVariantCount(FloorTheme.Ice), Is.EqualTo(0), "The ice is released after the change.");
        }
    }
}
