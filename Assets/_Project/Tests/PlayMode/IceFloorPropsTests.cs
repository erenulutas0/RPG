using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // Floor 2's phase-2 props (docs/49): the frozen mirror panels and the snow drifts stand on the ice floor only, and
    // floor 1's region keeps exactly its renderers and vertices; the frost transition plays once, on the descent from
    // floor 1 onto floor 2, takes no input and allocates nothing as it plays. Internals are reached by reflection.
    public sealed class IceFloorPropsTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags Constant = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Ice floor props cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static T Const<T>(Type type, string name) => (T)type.GetField(name, Constant).GetValue(null);

        // The region's shown budget, as the ice capture counts it: active mesh renderers and their vertices.
        private static void Count(FoundryRegionView region, out int renderers, out int vertices)
        {
            renderers = 0; vertices = 0;
            foreach (var r in region.GetComponentsInChildren<MeshRenderer>())
            {
                renderers++;
                vertices += r.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            }
        }

        private static IEnumerator LoadFoundry()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            Object.FindFirstObjectByType<CombatSetup>().Encounters.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator PanelsAndDriftsStandOnFloorTwoOnlyAndFloorOneKeepsItsCounts()
        {
            yield return LoadFoundry();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.IsFoundryRegion, Is.True);
            Assert.That(arena.Theme, Is.EqualTo(FloorTheme.Foundry));
            FoundryRegionView region = arena.Region;
            Type view = typeof(FoundryRegionView);
            string panelName = Const<string>(view, "IcePanelName"), driftName = Const<string>(view, "IceDriftName");
            Count(region, out int floorOneRenderers, out int floorOneVertices);
            // Floor 2's dress is prepared while floor 1 plays: its props may already stand, hidden.
            Transform early = region.transform.Find(panelName);
            Assert.That(early == null || !early.gameObject.activeSelf, Is.True, "Floor 1 never shows the mirror panels.");
            early = region.transform.Find(driftName);
            Assert.That(early == null || !early.gameObject.activeSelf, Is.True, "Floor 1 never shows the drifts.");

            arena.ApplyFloorTheme(FloorTheme.Ice);
            yield return null;
            Transform panels = region.transform.Find(panelName), drifts = region.transform.Find(driftName);
            Assert.That(panels, Is.Not.Null, "Floor 2 stands its mirror panels.");
            Assert.That(drifts, Is.Not.Null, "Floor 2 lays its drifts.");
            Assert.That(panels.gameObject.activeInHierarchy && drifts.gameObject.activeInHierarchy, Is.True);
            int panelVertices = panels.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            int driftVertices = drifts.GetComponent<MeshFilter>().sharedMesh.vertexCount;
            Assert.That(panelVertices, Is.GreaterThan(0).And.LessThanOrEqualTo(Const<int>(view, "IcePanelVertexCap")));
            Assert.That(driftVertices, Is.GreaterThan(0).And.LessThanOrEqualTo(Const<int>(view, "IceDriftVertexCap")));
            Assert.That(panels.GetComponent<MeshRenderer>().sortingOrder, Is.LessThan(0), "The panels draw before every actor.");
            Assert.That(drifts.GetComponent<MeshRenderer>().sortingOrder, Is.LessThan(arena.GroundEffectSortingOrder),
                "Telegraphs and contact shadows draw over the drifts.");
            // The project builds without the physics module (no Collider type here): nothing on the panels but the mesh.
            Assert.That(HasCollider(panels.gameObject), Is.False);
            // Every position finite, so each renderer has a valid world AABB (a NaN drift vertex once broke it).
            foreach (Transform prop in new[] { panels, drifts })
            {
                Mesh mesh = prop.GetComponent<MeshFilter>().sharedMesh;
                foreach (Vector3 p in mesh.vertices)
                    Assert.That(float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.y),
                        Is.False, prop.name + " has a non-finite vertex.");
                Assert.That(float.IsNaN(mesh.bounds.size.sqrMagnitude), Is.False, prop.name + "'s bounds are finite.");
            }
            Count(region, out _, out int floorTwoVertices);
            Assert.That(floorTwoVertices, Is.LessThan(170000), "Floor 2 stays under the ice pass's vertex line.");
            // Every drift vertex lies where no hero can stand: inside the edge margin, off every lane.
            Type surface = view.Assembly.GetType("Cryptforge.UI.FoundrySurface");
            MethodInfo toDesign = surface.GetMethod("ToDesign", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (Vector3 p in drifts.GetComponent<MeshFilter>().sharedMesh.vertices)
            {
                var args = new object[] { p.x, p.y, 0f, 0f };
                toDesign.Invoke(null, args);
                FoundryMap.ToFloor((float)args[2], (float)args[3], out float x, out float y);
                Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.False,
                    $"A drift vertex at design ({args[2]}, {args[3]}) lies on walkable floor.");
            }

            arena.ApplyFloorTheme(FloorTheme.Foundry);
            yield return null;
            Assert.That(panels.gameObject.activeSelf || drifts.gameObject.activeSelf, Is.False, "The foundry hides them.");
            Count(region, out int againRenderers, out int againVertices);
            Assert.That(againRenderers, Is.EqualTo(floorOneRenderers), "Floor 1's renderers are unchanged.");
            Assert.That(againVertices, Is.EqualTo(floorOneVertices), "Floor 1's vertices are unchanged.");
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheFrostPlaysOnceOnTheDescentToFloorTwo()
        {
            Assert.That(ArenaView.FrostsOnEntry(1, FloorTheme.Foundry), Is.False);
            Assert.That(ArenaView.FrostsOnEntry(2, FloorTheme.Ice), Is.True);
            Assert.That(ArenaView.FrostsOnEntry(3, FloorTheme.Ice), Is.False, "Only floor 1 -> floor 2.");
            Assert.That(ArenaView.FrostsOnEntry(2, FloorTheme.Foundry), Is.False);
            yield return LoadFoundry();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            Assert.That(arena.FrostTransition, Is.Null, "Floor 1's start shows no frost and builds nothing.");
            FloorDefinition second = encounters.Floor.NextFloor;
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Theme, Is.EqualTo(FloorTheme.Ice));

            // The descent's floor change (DescendToNextFloor: the floor number first, then EnterFloor), twice over.
            MethodInfo enter = typeof(EncounterController).GetMethod("EnterFloor", Private);
            MethodInfo number = typeof(EncounterController).GetProperty("FloorNumber").GetSetMethod(true);
            number.Invoke(encounters, new object[] { 2 });
            enter.Invoke(encounters, new object[] { second });
            FrostTransitionView frost = arena.FrostTransition;
            Assert.That(frost, Is.Not.Null, "Landing on floor 2 plays the frost.");
            PropertyInfo plays = typeof(FrostTransitionView).GetProperty("Plays", Private);
            PropertyInfo playing = typeof(FrostTransitionView).GetProperty("IsPlaying", Private);
            Assert.That((int)plays.GetValue(frost), Is.EqualTo(1));
            Assert.That((bool)playing.GetValue(frost), Is.True);
            enter.Invoke(encounters, new object[] { second });
            Assert.That((int)plays.GetValue(frost), Is.EqualTo(1), "The frost plays once.");
            Assert.That(arena.PlayFrostTransition(), Is.False, "Asked again, it does not play again.");

            // On the camera, never the region; it takes no input.
            Assert.That(frost.transform.parent.GetComponent<Camera>(), Is.Not.Null);
            Assert.That(frost.GetComponentInParent<FoundryRegionView>(), Is.Null);
            Assert.That(HasCollider(frost.gameObject), Is.False);
            Assert.That(frost.GetComponentInChildren<Canvas>(), Is.Null);
            Assert.That(frost.GetComponent<MeshRenderer>().enabled, Is.True);

            // A frame of the play allocates nothing.
            var advance = (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), frost,
                typeof(FrostTransitionView).GetMethod("Advance", Private));
            advance(.01f);
            Recorder recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            for (int i = 0; i < 5; i++)
                advance(.01f);
            recorder.enabled = false;
            recorder.CollectFromAllThreads();
            Assert.That(recorder.sampleBlockCount, Is.EqualTo(0), "Playing the frost allocates nothing.");

            // About 0.6 s, then gone.
            float until = Time.realtimeSinceStartup + 1.2f;
            while ((bool)playing.GetValue(frost) && Time.realtimeSinceStartup < until)
                yield return null;
            Assert.That((bool)playing.GetValue(frost), Is.False, "The frost is over within its duration.");
            Assert.That(frost.GetComponent<MeshRenderer>().enabled, Is.False);
            Assert.That((int)plays.GetValue(frost), Is.EqualTo(1));
        }

        // A collider of any kind, found by type name: the test assembly does not reference UnityEngine.PhysicsModule.
        private static bool HasCollider(GameObject root)
        {
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component != null && component.GetType().Name.Contains("Collider"))
                    return true;
            }
            return false;
        }
    }
}
