using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
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
    // Floor 3 (Gloam Gardens, docs/51) through the descent as played (EncounterController.EnterFloor): floor 2 puts on the
    // ice and starts working out the garden's dress while it plays (PrepareFloorTheme), the descent onto floor 3 binds it
    // - the garden's paving and masonry, its Resources/Gloam variants, no ice prop shown, floor 1's renderers and
    // vertices exactly (phase 1 adds no geometry) - and leaving the ice releases its variants, so floors 1-3 never hold
    // two variant sets. The enemies stand up in the bloomed palette. Internals are reached by reflection.
    public sealed class GloamThemeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Gloam theme cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static void Count(FoundryRegionView region, out int renderers, out int vertices)
        {
            renderers = 0; vertices = 0;
            foreach (var r in region.GetComponentsInChildren<MeshRenderer>())
            {
                renderers++;
                vertices += r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            }
        }

        // Every texture the region's renderers are bound to through their property blocks.
        private static HashSet<Texture> Bound(FoundryRegionView region)
        {
            var bound = new HashSet<Texture>();
            var block = new MaterialPropertyBlock();
            foreach (var r in region.GetComponentsInChildren<MeshRenderer>(true))
            {
                r.GetPropertyBlock(block);
                if (block.isEmpty)
                    continue;
                Texture texture = block.GetTexture("_MainTex");
                if (texture != null)
                    bound.Add(texture);
            }
            return bound;
        }

        // The shown renderers with a block (floor 2's hidden props keep their own coping block).
        private static int Blocked(FoundryRegionView region)
        {
            int blocked = 0;
            var block = new MaterialPropertyBlock();
            foreach (var r in region.GetComponentsInChildren<MeshRenderer>())
            {
                r.GetPropertyBlock(block);
                if (!block.isEmpty)
                    blocked++;
            }
            return blocked;
        }

        private const string Growth = "Garden roots, fungi and moss";

        // Every growth vertex lies off the walkable floor (inside the hero's edge margin at most: the moss cushion over
        // the rim) and every uv inside the atlas.
        private static void AssertGrowthOffTheLanes(Mesh mesh)
        {
            System.Type surface = typeof(FoundryRegionView).Assembly.GetType("Cryptforge.UI.FoundrySurface");
            MethodInfo toDesign = surface.GetMethod("ToDesign", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (Vector3 p in mesh.vertices)
            {
                Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y), Is.False,
                    "The growth has a non-finite vertex.");
                var args = new object[] { p.x, p.y, 0f, 0f };
                toDesign.Invoke(null, args);
                FoundryMap.ToFloor((float)args[2], (float)args[3], out float x, out float y);
                Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.False,
                    $"A growth vertex at design ({args[2]}, {args[3]}) lies on walkable floor.");
            }
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            Assert.That(uvs.Count, Is.EqualTo(mesh.vertexCount));
            foreach (Vector2 uv in uvs)
                Assert.That(uv.x >= 0f && uv.x <= 1f && uv.y >= 0f && uv.y <= 1f, Is.True, "A growth uv outside the atlas: " + uv);
        }

        // The melt plays once, on the descent from floor 2 onto floor 3, on the camera and never the region; it takes no
        // input, a frame of it allocates nothing, and it is over within its duration.
        [UnityTest, Timeout(180000)]
        public IEnumerator TheMeltPlaysOnceOnTheDescentToFloorThree()
        {
            Assert.That(ArenaView.MeltsOnEntry(3, FloorTheme.Gloam), Is.True);
            Assert.That(ArenaView.MeltsOnEntry(2, FloorTheme.Ice), Is.False);
            Assert.That(ArenaView.MeltsOnEntry(3, FloorTheme.Ice), Is.False);
            Assert.That(ArenaView.MeltsOnEntry(4, FloorTheme.Foundry), Is.False);
            yield return LoadFoundry();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            encounters.enabled = false;
            Assert.That(arena.GloamTransition, Is.Null, "Floor 1's start builds no melt.");
            FloorDefinition second = encounters.Floor.NextFloor, third = second.NextFloor;
            MethodInfo enter = typeof(EncounterController).GetMethod("EnterFloor", Private);
            MethodInfo number = typeof(EncounterController).GetProperty("FloorNumber").GetSetMethod(true);
            number.Invoke(encounters, new object[] { 2 });
            enter.Invoke(encounters, new object[] { second });
            Assert.That(arena.GloamTransition, Is.Null, "Floor 2 shows no melt.");
            number.Invoke(encounters, new object[] { 3 });
            enter.Invoke(encounters, new object[] { third });
            GloamTransitionView melt = arena.GloamTransition;
            Assert.That(melt, Is.Not.Null, "Landing on floor 3 plays the melt.");
            PropertyInfo plays = typeof(GloamTransitionView).GetProperty("Plays", Private);
            PropertyInfo playing = typeof(GloamTransitionView).GetProperty("IsPlaying", Private);
            Assert.That((int)plays.GetValue(melt), Is.EqualTo(1));
            Assert.That((bool)playing.GetValue(melt), Is.True);
            enter.Invoke(encounters, new object[] { third });
            Assert.That((int)plays.GetValue(melt), Is.EqualTo(1), "The melt plays once.");
            Assert.That(arena.PlayGloamTransition(), Is.False, "Asked again, it does not play again.");
            Assert.That(melt.transform.parent.GetComponent<Camera>(), Is.Not.Null);
            Assert.That(melt.GetComponentInParent<FoundryRegionView>(), Is.Null);
            Assert.That(melt.GetComponentInChildren<Canvas>(), Is.Null);
            foreach (Component component in melt.GetComponentsInChildren<Component>(true))
                Assert.That(component.GetType().Name.Contains("Collider"), Is.False, "The melt takes no input.");
            Assert.That(melt.GetComponent<MeshRenderer>().enabled, Is.True);
            // Every position finite while it plays.
            foreach (Vector3 p in melt.GetComponent<MeshFilter>().sharedMesh.vertices)
                Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y), Is.False);

            var advance = (System.Action<float>)System.Delegate.CreateDelegate(typeof(System.Action<float>), melt,
                typeof(GloamTransitionView).GetMethod("Advance", Private));
            advance(.01f);
            UnityEngine.Profiling.Recorder recorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            for (int i = 0; i < 5; i++)
                advance(.01f);
            recorder.enabled = false;
            recorder.CollectFromAllThreads();
            Assert.That(recorder.sampleBlockCount, Is.EqualTo(0), "Playing the melt allocates nothing.");

            float until = Time.realtimeSinceStartup + 1.4f;
            while ((bool)playing.GetValue(melt) && Time.realtimeSinceStartup < until)
                yield return null;
            Assert.That((bool)playing.GetValue(melt), Is.False, "The melt is over within its duration.");
            Assert.That(melt.GetComponent<MeshRenderer>().enabled, Is.False);
        }

        private static IEnumerator LoadFoundry()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator FloorTwoPreparesTheGardenAndTheDescentBindsItAndReleasesTheIce()
        {
            yield return LoadFoundry();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.IsFoundryRegion, Is.True);
            encounters.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            FoundryRegionView region = arena.Region;
            FloorDefinition second = encounters.Floor.NextFloor;
            Assert.That(second, Is.Not.Null);
            FloorDefinition third = second.NextFloor;
            Assert.That(third, Is.Not.Null);
            Assert.That(third.Theme, Is.EqualTo(FloorTheme.Gloam));
            Count(region, out int foundryRenderers, out int foundryVertices);
            Assert.That(region.HeldVariantCount(FloorTheme.Gloam), Is.EqualTo(0), "Floor 1 loads nothing of floor 3's.");

            // The descent onto floor 2: the ice put on, and floor 3's dress started behind it.
            MethodInfo enter = typeof(EncounterController).GetMethod("EnterFloor", Private);
            MethodInfo number = typeof(EncounterController).GetProperty("FloorNumber").GetSetMethod(true);
            number.Invoke(encounters, new object[] { 2 });
            enter.Invoke(encounters, new object[] { second });
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Ice));
            Assert.That(region.HeldVariantCount(FloorTheme.Ice), Is.GreaterThan(0), "The ice's variants are bound on floor 2.");
            var iceTextures = new List<Texture>();
            foreach (Texture texture in Bound(region))
                if (texture != Texture2D.whiteTexture && texture != arena.PlatformMaterials.IcePaving && texture != arena.PlatformMaterials.IceMasonry)
                    iceTextures.Add(texture);
            Assert.That(iceTextures.Count, Is.GreaterThan(0), "Floor 2 binds Resources/Ice variants.");
            int frames = 0;
            while (!arena.IsFloorThemeReady(FloorTheme.Gloam) && frames < 600)
            {
                frames++;
                yield return null;
            }
            Assert.That(arena.IsFloorThemeReady(FloorTheme.Gloam), Is.True, "The garden's dress is ready within 600 frames of floor 2.");
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Ice), "Preparing changes nothing on show.");
            // The garden's growth is built during floor 2, hidden.
            Transform growth = region.transform.Find(Growth);
            Assert.That(growth, Is.Not.Null, "Floor 3's growth is built while floor 2 plays.");
            Assert.That(growth.gameObject.activeSelf, Is.False, "Floor 2 never shows the garden's growth.");
            Assert.That(region.HeldVariantCount(FloorTheme.Gloam), Is.GreaterThan(0), "Floor 3's variants are loaded ahead.");
            foreach (Texture texture in iceTextures)
                Assert.That(Bound(region).Contains(texture), Is.True, "Floor 2 still wears its ice while the garden is prepared.");

            // The descent onto floor 3: the garden bound, the ice props hidden, the ice's variants released.
            number.Invoke(encounters, new object[] { 3 });
            enter.Invoke(encounters, new object[] { third });
            yield return null;
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Gloam));
            Assert.That(region.Theme, Is.EqualTo(FloorTheme.Gloam));
            Assert.That(ArenaView.FrostsOnEntry(3, FloorTheme.Gloam), Is.False, "The frost creep is floor 2's; the garden has its melt.");
            foreach (string ice in new[] { "Ice crystals and icicles", "Frozen mirror panels", "Snow drifts" })
            {
                Transform prop = region.transform.Find(ice);
                Assert.That(prop == null || !prop.gameObject.activeSelf, Is.True, ice + " is floor 2's alone.");
            }
            // Floor 1's region plus the growth's one renderer, inside the crystals' cap and docs/51 section 8's ceiling.
            Assert.That(growth.gameObject.activeInHierarchy, Is.True, "The garden's growth stands on floor 3.");
            int growthVertices = growth.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            Assert.That(growthVertices, Is.GreaterThan(0).And.LessThanOrEqualTo(6000), "The growth stays under IceCrystalVertexCap.");
            Count(region, out int gloamRenderers, out int gloamVertices);
            Assert.That(gloamRenderers, Is.EqualTo(foundryRenderers + 1), "Floor 3's renderers are floor 1's and the growth's.");
            Assert.That(gloamVertices, Is.EqualTo(foundryVertices + growthVertices), "Floor 3's vertices are floor 1's and the growth's.");
            Assert.That(gloamVertices, Is.LessThanOrEqualTo(174000), "Floor 3 stays under the region's vertex ceiling (docs/51 section 8).");
            var growthBlock = new MaterialPropertyBlock();
            growth.GetComponent<MeshRenderer>().GetPropertyBlock(growthBlock);
            Texture atlas = growthBlock.GetTexture("_MainTex");
            Assert.That(atlas != null && atlas.name == "GloamGrowth", Is.True, "The growth samples pass 2's atlas.");
            AssertGrowthOffTheLanes(growth.GetComponent<MeshFilter>().sharedMesh);
            HashSet<Texture> bound = Bound(region);
            Assert.That(bound.Contains(arena.PlatformMaterials.GloamPaving), Is.True, "The court wears GloamPavingV1.");
            Assert.That(bound.Contains(arena.PlatformMaterials.GloamMasonry), Is.True, "The walls wear GloamMasonryV1.");
            Assert.That(bound.Contains(arena.PlatformMaterials.IcePaving) || bound.Contains(arena.PlatformMaterials.IceMasonry), Is.False);
            foreach (Texture texture in iceTextures)
                Assert.That(bound.Contains(texture), Is.False, "No renderer keeps an ice variant.");
            // The release runs over the frames after the change, never inside it.
            int releaseFrames = 0;
            while (region.HeldVariantCount(FloorTheme.Ice) > 0 && releaseFrames++ < 120)
                yield return null;
            Assert.That(region.HeldVariantCount(FloorTheme.Ice), Is.EqualTo(0), "Leaving the ice releases its variants.");
            int streamed = 0;
            foreach (var r in region.GetComponentsInChildren<MeshRenderer>())
                if (r.additionalVertexStreams != null)
                {
                    streamed++;
                    Assert.That(r.additionalVertexStreams.vertexCount, Is.EqualTo(r.GetComponent<MeshFilter>().sharedMesh.vertexCount), r.name);
                }
            Assert.That(streamed, Is.GreaterThan(1), "The garden's colours ride on the renderers' streams.");
            Assert.That(region.HeldVariantCount(FloorTheme.Gloam), Is.GreaterThan(0));
            Assert.That(arena.IsFloorThemeReady(FloorTheme.Ice), Is.False, "The ice is no longer held ready.");

            // Back to the foundry (a new run): nothing bound, nothing of the garden held.
            arena.ApplyFloorTheme(FloorTheme.Foundry);
            yield return null;
            Assert.That(Blocked(region), Is.EqualTo(0), "The foundry wears its own materials.");
            foreach (var r in region.GetComponentsInChildren<MeshRenderer>())
                Assert.That(r.additionalVertexStreams, Is.Null, r.name + " is back on the foundry's colours.");
            int frames2 = 0;
            while (region.HeldVariantCount(FloorTheme.Gloam) > 0 && frames2++ < 120)
                yield return null;
            Assert.That(region.HeldVariantCount(FloorTheme.Gloam), Is.EqualTo(0));
            Assert.That(growth.gameObject.activeSelf, Is.False, "The foundry hides the garden's growth.");
            Count(region, out int againRenderers, out int againVertices);
            Assert.That(againRenderers, Is.EqualTo(foundryRenderers));
            Assert.That(againVertices, Is.EqualTo(foundryVertices));
        }

        // The look on its own: the garden's copy for Gloam, the frosted one for Ice, the painted set on the foundry, each
        // redrawn in place.
        [UnityTest]
        public IEnumerator TheLookTakesTheFloorsCopy()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Runner.prefab");
            var body = Object.Instantiate(prefab);
            try
            {
                body.GetComponent<AttackController>().enabled = false;
                body.GetComponent<Health>().Initialize(10f);
                yield return null;
                var look = body.GetComponent<EnemyLookView>();
                var painted = (EnemyArtSet)typeof(EnemyLookView).GetField("_paintedArt", Private).GetValue(look);
                MethodInfo use = typeof(EnemyLookView).GetMethod("UseTheme", Private);
                PropertyInfo shown = typeof(EnemyLookView).GetProperty("ShownTheme", Private);
                PropertyInfo frost = typeof(EnemyLookView).GetProperty("ShowsFrost", Private);
                Assert.That(look.PaintedArt, Is.SameAs(painted));
                use.Invoke(look, new object[] { FloorTheme.Gloam });
                Assert.That(look.PaintedArt, Is.SameAs(painted.ForTheme(FloorTheme.Gloam)));
                Assert.That((FloorTheme)shown.GetValue(look), Is.EqualTo(FloorTheme.Gloam));
                Assert.That((bool)frost.GetValue(look), Is.False, "Bloomed is not frosted.");
                Assert.That(look.PaintedArt.FlashOf(look.Body.sprite), Is.Not.Null, "The drawing is the bloomed set's.");
                Assert.That(painted.FlashOf(look.Body.sprite), Is.Null, "Not the painted set's.");
                use.Invoke(look, new object[] { FloorTheme.Ice });
                Assert.That(look.PaintedArt, Is.SameAs(painted.Frosted));
                Assert.That((bool)frost.GetValue(look), Is.True);
                use.Invoke(look, new object[] { FloorTheme.Foundry });
                Assert.That(look.PaintedArt, Is.SameAs(painted));
                Assert.That((FloorTheme)shown.GetValue(look), Is.EqualTo(FloorTheme.Foundry));
            }
            finally
            {
                Object.Destroy(body);
            }
#else
            yield return null;
#endif
        }

        // The encounter stands floor 3's horde up bloomed (EncounterController.CreateEnemy passes the floor's theme).
        [UnityTest, Timeout(240000)]
        public IEnumerator FloorThreesHordeStandsUpBloomed()
        {
            yield return LoadFoundry();
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            Time.captureDeltaTime = 1f / 60;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            FloorDefinition first = encounters.Floor;
            FloorDefinition third = first.NextFloor.NextFloor;
            Assert.That(third.Theme, Is.EqualTo(FloorTheme.Gloam));
            FieldInfo current = typeof(EncounterController).GetField("_currentFloor", Private);
            PropertyInfo shown = typeof(EnemyLookView).GetProperty("ShownTheme", Private);
            int bloomed = 0, other = 0;
            System.Action<int> spawned = slot =>
            {
                Health enemy = encounters.HordeEnemyAt(slot);
                if (enemy == null || enemy.GetComponent<EnemyLookView>().PaintedArt == null)
                    return;
                if ((FloorTheme)shown.GetValue(enemy.GetComponent<EnemyLookView>()) == FloorTheme.Gloam) bloomed++;
                else other++;
            };
            float wait = Time.realtimeSinceStartup + 30;
            while (encounters.HordeEnemyCount == 0 && Time.realtimeSinceStartup < wait)
                yield return null;
            current.SetValue(encounters, third);
            encounters.HordeEnemySpawned += spawned;
            try
            {
                float deadline = Time.realtimeSinceStartup + 60;
                while (bloomed < 3 && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
                {
                    for (int i = 0; i < 4 && setup.Choices.IsOpen; i++)
                        setup.Choices.TrySelect(setup.Choices.Current, 0);
                    if (hero.IsAlive)
                        hero.Heal(hero.Maximum);
                    for (int i = 0; i < encounters.HordeEnemyCount; i++)
                    {
                        Health enemy = encounters.HordeEnemyAt(i);
                        if (enemy != null && enemy.IsAlive)
                            enemy.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
                    }
                    yield return null;
                }
            }
            finally
            {
                encounters.HordeEnemySpawned -= spawned;
                current.SetValue(encounters, first);
            }
            Assert.That(bloomed, Is.GreaterThanOrEqualTo(3), "Floor 3's horde stands up bloomed.");
            Assert.That(other, Is.EqualTo(0), "No painted or frosted body on floor 3.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
