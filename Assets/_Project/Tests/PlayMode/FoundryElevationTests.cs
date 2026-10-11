using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class FoundryElevationTests
    {
        private Scene _scene;
        [SetUp] public void Begin() => TestProfile.Begin();
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Elevation cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
        [UnityTest] public IEnumerator ElevatedActorsEffectsAndCameraShareContactsWithoutMovingGameplayRoots()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene = SceneManager.GetActiveScene();
            yield return null;
            Time.timeScale = 0;
            var arena = Object.FindFirstObjectByType<ArenaView>();
            var visibility = arena.Region.GetComponent<FoundryVisibilityView>();
            var hero = Object.FindFirstObjectByType<HeroLookView>();
            var follow = Object.FindFirstObjectByType<ArenaCameraFollow>();
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var enemy = setup.Encounters.WaveEnemyAt(0);
            var enemyLook = enemy.GetComponent<EnemyLookView>();
            var chest = Object.FindFirstObjectByType<ChestSpawner>().Body;
            var ring = hero.GetComponent<AbilityRingView>().Ring;
            Sprite sourceRing = ring.sprite;
            Material sourceMaterial = ring.sharedMaterial;
            Sprite grid = null;
            Material groundMaterial = null;
            visibility.RestoreActorDepth();
            int raisedFloorVertices = 0;
            foreach (var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if (renderer.sharedMaterial != arena.PlatformMaterials.FoundryPaving) continue;
                Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
                var points = mesh.vertices; var uv = mesh.uv;
                for (int i = 0; i < points.Length; i++)
                {
                    // Paving UVs retain the original floor coordinate, independent of height.
                    float u = (uv[i].x - 1) * FoundryRegionView.PavingGrainSpan / (2 * FoundryMap.Axis);
                    float v = (uv[i].y - 1) * FoundryRegionView.PavingGrainSpan / (2 * FoundryMap.Axis);
                    if (u < FoundrySurface.RampStart + .01f) continue;
                    Assert.That(Vector3.Distance(points[i], FoundrySurface.Project(u, v)), Is.LessThan(.002f), $"Paving vertex {i}: UV={uv[i]}, design={u},{v}, point={points[i]}, target={FoundrySurface.Project(u,v)}");
                    raisedFloorVertices++;
                }
                // Correct corners alone cannot prove a flush triangle: a decoration
                // straddling the ramp crest can float or cut through between them.
                // Test actual paving triangle interiors against the surface and its UVs.
                var triangles = mesh.triangles;
                for (int t = 0; t < triangles.Length; t += 3)
                {
                    int a = triangles[t], b = triangles[t + 1], c = triangles[t + 2];
                    Vector2 centreUv = (uv[a] + uv[b] + uv[c]) / 3;
                    float u = (centreUv.x - 1) * FoundryRegionView.PavingGrainSpan / (2 * FoundryMap.Axis);
                    float v = (centreUv.y - 1) * FoundryRegionView.PavingGrainSpan / (2 * FoundryMap.Axis);
                    if (u < FoundrySurface.RampStart - 1 || u > FoundrySurface.RampEnd + 1) continue;
                    Vector3 centre = (points[a] + points[b] + points[c]) / 3;
                    Assert.That(Vector3.Distance(centre, FoundrySurface.Project(u, v)), Is.LessThan(.002f),
                        $"Paving triangle {t / 3} must stay flush through the ramp seams.");
                }
            }
            Assert.That(raisedFloorVertices, Is.GreaterThan(20), "Actual paving uses the shared raised surface.");

            foreach (float u in new[] { FoundrySurface.RampStart-1.5f, FoundrySurface.RampStart-1, FoundrySurface.RampStart, FoundrySurface.RampStart+1, FoundrySurface.RampEnd, FoundrySurface.RampStart, FoundrySurface.RampStart-1.5f })
            {
                FoundryMap.ToFloor(u, 5, out float x, out float y);
                Vector3 flat = arena.transform.position + new Vector3(x, y * .5f, 0);
                hero.transform.position = enemy.transform.position = chest.transform.position = flat;
                float lift = FoundrySurface.Lift(flat, arena.transform.position);
                var rest = new Dictionary<Transform, Vector3>();
                foreach (Transform child in hero.transform) rest.Add(child, child.localPosition);
                rest.Add(chest.transform, chest.transform.localPosition);
                Vector3 bodyRest = enemyLook.Body.transform.position, barRest = enemyLook.Bar.position;
                Assert.That(follow.Goal().y, Is.EqualTo(flat.y + lift - follow.OffsetY).Within(.0001f));
                visibility.ApplyActorDepth();
                Assert.That(hero.transform.position, Is.EqualTo(flat));
                Assert.That(enemy.transform.position, Is.EqualTo(flat));
                Assert.That(enemyLook.Body.transform.position.y, Is.EqualTo(bodyRest.y + lift).Within(.0001f));
                Assert.That(enemyLook.Bar.position.y, Is.EqualTo(barRest.y + lift).Within(.0001f));
                Assert.That(enemyLook.Bar.position.z, Is.EqualTo(barRest.z));
                Assert.That(chest.transform.position.y, Is.EqualTo(flat.y + lift).Within(.0001f));
                Assert.That(ring.transform.position.y, Is.EqualTo(flat.y), "Ground shader supplies pointwise height.");
                Assert.That(ring.sprite.vertices.Length, Is.GreaterThan(200));
                Assert.That(ring.sharedMaterial.shader.name, Is.EqualTo("Cryptforge/Foundry Ground"));
                if (grid != null) Assert.That(ring.sprite, Is.SameAs(grid), "No grid allocation per move.");
                grid = ring.sprite; groundMaterial = ring.sharedMaterial;
                visibility.RestoreActorDepth();
                foreach (var item in rest) Assert.That(item.Key.localPosition, Is.EqualTo(item.Value));
                Assert.That(ring.sprite, Is.SameAs(sourceRing));
                Assert.That(ring.sharedMaterial, Is.SameAs(sourceMaterial));
            }

            FoundryMap.ToFloor(FoundryMap.GateLandingCentre, 5, out float ex, out float ey);
            enemy.transform.position = new Vector3(ex, ey * .5f, 0);
            float enemyLift = FoundrySurface.Lift(enemy.transform.position, arena.transform.position);
            enemy.ApplyDamage(new DamageContext(1));
            var effectRest = new Dictionary<Transform, Vector3>();
            for (int i = 0; i < effects.EffectSlotCount; i++)
            {
                var effect = effects.SurfaceEffect(i, out var contact, out bool above, out _);
                if (effect != null && above && contact == enemy.transform.position) effectRest.Add(effect, effect.position);
            }
            var numberRest = new Dictionary<Transform, Vector3>();
            for (int i = 0; i < effects.NumberSlotCount; i++)
            {
                var number = effects.SurfaceNumber(i, out var contact);
                if (number != null && contact == enemy.transform.position) numberRest.Add(number, number.position);
            }
            Assert.That(effectRest.Count, Is.GreaterThan(0));
            Assert.That(numberRest.Count, Is.GreaterThan(0));
            visibility.ApplyActorDepth();
            foreach (var item in effectRest) Assert.That(item.Key.position.y, Is.EqualTo(item.Value.y + enemyLift).Within(.0001f));
            foreach (var item in numberRest)
            {
                Assert.That(item.Key.position.y, Is.EqualTo(item.Value.y + enemyLift).Within(.0001f));
                Assert.That(item.Key.position.z, Is.EqualTo(item.Value.z));
            }
            visibility.enabled = false;
            foreach (var item in effectRest) Assert.That(item.Key.position, Is.EqualTo(item.Value));
            foreach (var item in numberRest) Assert.That(item.Key.position, Is.EqualTo(item.Value));
            Assert.That(ring.sprite, Is.SameAs(sourceRing));
            Object.Destroy(visibility);
            yield return null;
            Assert.That(grid == null, Is.True, "Scene owns generated meshes/sprites.");
            Assert.That(groundMaterial == null, Is.True);
            Assert.That(sourceRing != null, Is.True, "Borrowed art survives.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
