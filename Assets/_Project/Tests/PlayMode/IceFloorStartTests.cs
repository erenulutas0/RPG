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
    // A development start directly on floor 2 (DevelopmentStart's start-floor naming a floor of the authored Descent):
    // the encounter enters the ice floor in CombatSetup.Awake, possibly before ArenaView.Awake has built the region, and
    // the region still wears Ice once the scene has loaded (ArenaView keeps an early ApplyFloorTheme until Awake).
    public sealed class IceFloorStartTests
    {
        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Ice floor start cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator ADescentStartedOnFloorTwoWearsIceAfterLoad()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_quicksilver_vaults");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(encounters.Floor, Is.Not.Null);
            Assert.That(encounters.Floor.Id, Is.EqualTo("floor_quicksilver_vaults"), "The Descent starts on floor 2.");
            Assert.That(encounters.Floor.Theme, Is.EqualTo(FloorTheme.Ice));
            Assert.That(arena.IsFoundryRegion, Is.True);
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Ice), "The arena wears floor 2's theme.");
            Assert.That(arena.Region, Is.Not.Null);
            Assert.That(arena.Region.Theme, Is.EqualTo(FloorTheme.Ice), "The region itself is dressed in ice.");
            var crystals = arena.Region.transform.Find("Ice crystals and icicles");
            Assert.That(crystals != null && crystals.gameObject.activeInHierarchy, Is.True, "The ice crystals stand.");
        }

        // The same on floor 3 (docs/51): started on the garden, the region wears Gloam once the scene has loaded - its
        // paving bound, its variants held, nothing of the ice built or held.
        [UnityTest, Timeout(180000)]
        public IEnumerator ADescentStartedOnFloorThreeWearsGloamAfterLoad()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_gloam_gardens");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(encounters.Floor, Is.Not.Null);
            Assert.That(encounters.Floor.Id, Is.EqualTo("floor_gloam_gardens"), "The Descent starts on floor 3.");
            Assert.That(encounters.Floor.Theme, Is.EqualTo(FloorTheme.Gloam));
            Assert.That(arena.IsFoundryRegion, Is.True);
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Gloam), "The arena wears floor 3's theme.");
            Assert.That(arena.Region, Is.Not.Null);
            Assert.That(arena.Region.Theme, Is.EqualTo(FloorTheme.Gloam), "The region itself is dressed as the garden.");
            Assert.That(arena.Region.transform.Find("Ice crystals and icicles"), Is.Null, "Nothing of the ice is built.");
            Transform growth = arena.Region.transform.Find("Garden roots, fungi and moss");
            Assert.That(growth != null && growth.gameObject.activeInHierarchy, Is.True, "The garden's growth stands.");
            Assert.That(arena.Region.HeldVariantCount(FloorTheme.Ice), Is.EqualTo(0));
            Assert.That(arena.Region.HeldVariantCount(FloorTheme.Gloam), Is.GreaterThan(0), "The garden's variants are bound.");
            bool paved = false;
            var block = new MaterialPropertyBlock();
            foreach (var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.GetPropertyBlock(block);
                if (!block.isEmpty && block.GetTexture("_MainTex") == arena.PlatformMaterials.GloamPaving)
                    paved = true;
            }
            Assert.That(paved, Is.True, "The court wears GloamPavingV1.");
            Assert.That(arena.IsPreparingFloorTheme, Is.False, "Nothing left preparing: the finale wears the foundry.");
        }
    }
}
