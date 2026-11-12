using System;
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
    // Floor 2's enemies wear the frosted palette (docs/49, EnemyArtSet.Frosted), visual only: the encounter asks every
    // body as it stands up (EncounterController.CreateEnemy -> EnemyLookView.UseFrost), so a horde body shelved on one
    // floor and reused on the other takes the floor it walks onto. Floor 1 keeps the painted art.
    public sealed class EnemyFrostTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnitySetUp] public IEnumerator Begin() { TestProfile.Begin(); Time.timeScale = 1; yield return null; }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Enemy frost cleanup " + Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static bool ShowsFrost(EnemyLookView look) =>
            (bool)typeof(EnemyLookView).GetProperty("ShowsFrost", Private).GetValue(look);

        private static EnemyArtSet Painted(EnemyLookView look) =>
            (EnemyArtSet)typeof(EnemyLookView).GetField("_paintedArt", Private).GetValue(look);

        // The body wears the frosted copy's drawing (its flash found there, not in the painted set), or the painted set's.
        private static void AssertPalette(Health enemy, bool frosted, string when)
        {
            var look = enemy.GetComponent<EnemyLookView>();
            EnemyArtSet painted = Painted(look);
            Assert.That(painted, Is.Not.Null, enemy.name + " has painted art.");
            Assert.That(painted.Frosted, Is.Not.Null, enemy.name + "'s set has a frosted copy.");
            Sprite body = look.Body.sprite;
            Assert.That(ShowsFrost(look), Is.EqualTo(frosted), $"{enemy.name} {when}.");
            Assert.That(look.PaintedArt, Is.SameAs(frosted ? painted.Frosted : painted), $"{enemy.name}'s set {when}.");
            Assert.That(look.PaintedArt.FlashOf(body), Is.Not.Null, $"{enemy.name}'s drawing is its set's {when}.");
            Assert.That((frosted ? painted : painted.Frosted).FlashOf(body), Is.Null,
                $"{enemy.name}'s drawing is not the other palette's {when}.");
        }

        [UnityTest, Timeout(240000)]
        public IEnumerator FloorTwoEnemiesAreFrostedAndFloorOnesPaintedAcrossThePool()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            Time.captureDeltaTime = 1f / 60;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var encounters = setup.Encounters;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            FloorDefinition first = encounters.Floor;
            FloorDefinition second = first.NextFloor;
            Assert.That(first.Theme, Is.EqualTo(FloorTheme.Foundry));
            Assert.That(second, Is.Not.Null);
            Assert.That(second.Theme, Is.EqualTo(FloorTheme.Ice));
            FieldInfo current = typeof(EncounterController).GetField("_currentFloor", Private);

            var seenOn = new Dictionary<Health, bool>();
            bool frosting = false;
            int checkedSpawns = 0, crossed = 0;
            Action<int> spawned = slot =>
            {
                Health enemy = encounters.HordeEnemyAt(slot);
                if (enemy == null || enemy.GetComponent<EnemyLookView>().PaintedArt == null)
                    return;
                AssertPalette(enemy, frosting, frosting ? "on floor 2" : "on floor 1");
                checkedSpawns++;
                if (seenOn.TryGetValue(enemy, out bool wasFrosted) && wasFrosted != frosting)
                    crossed++;
                seenOn[enemy] = frosting;
            };
            // Floor 1's crowd as played: painted. It is known before the floors swap, so its bodies count as crossed if
            // they come back.
            float wait = Time.realtimeSinceStartup + 30;
            while (encounters.HordeEnemyCount == 0 && Time.realtimeSinceStartup < wait)
                yield return null;
            Assert.That(encounters.HordeEnemyCount, Is.GreaterThan(0), "Floor 1's horde walks in.");
            for (int i = 0; i < encounters.HordeEnemyCount; i++)
            {
                Health enemy = encounters.HordeEnemyAt(i);
                if (enemy == null || enemy.GetComponent<EnemyLookView>().PaintedArt == null)
                    continue;
                AssertPalette(enemy, false, "on floor 1");
                seenOn[enemy] = false;
            }
            encounters.HordeEnemySpawned += spawned;
            try
            {
                // The spawns as floor 2's (the floor the encounter stands its bodies up on), then floor 1's again: each
                // phase fells the horde until a body shelved in the last phase walks in again.
                foreach (bool phase in new[] { true, false })
                {
                    frosting = phase;
                    current.SetValue(encounters, phase ? second : first);
                    int crossedBefore = crossed;
                    float deadline = Time.realtimeSinceStartup + 60;
                    while (crossed == crossedBefore && !setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
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
                    Assert.That(crossed, Is.GreaterThan(crossedBefore), phase
                        ? "A body shelved on floor 1 walks onto floor 2 frosted."
                        : "A body shelved on floor 2 walks back onto floor 1 painted.");
                }
            }
            finally
            {
                encounters.HordeEnemySpawned -= spawned;
                current.SetValue(encounters, first);
            }
            Assert.That(checkedSpawns, Is.GreaterThan(0));
            LogAssert.NoUnexpectedReceived();
        }

        // The look on its own: frosting and thawing redraw the current pose from the other set in place, asking for the
        // palette on show changes nothing, and a reset body keeps the palette it was last given.
        [UnityTest]
        public IEnumerator TheLookSwapsPaletteInPlaceAndKeepsItThroughAReset()
        {
#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Grunt.prefab");
            var body = Object.Instantiate(prefab);
            try
            {
                body.GetComponent<AttackController>().enabled = false;
                body.GetComponent<Health>().Initialize(10f);
                yield return null;
                var look = body.GetComponent<EnemyLookView>();
                MethodInfo use = typeof(EnemyLookView).GetMethod("UseFrost", Private);
                MethodInfo reset = typeof(EnemyLookView).GetMethod("ResetForReuse", Private);
                Health health = body.GetComponent<Health>();
                AssertPalette(health, false, "as instantiated");
                use.Invoke(look, new object[] { true });
                AssertPalette(health, true, "frosted");
                Sprite frosted = look.Body.sprite;
                use.Invoke(look, new object[] { true });
                Assert.That(look.Body.sprite, Is.SameAs(frosted), "Asking again changes nothing.");
                body.SetActive(false);
                reset.Invoke(look, null);
                body.SetActive(true);
                AssertPalette(health, true, "frosted after a reset");
                use.Invoke(look, new object[] { false });
                AssertPalette(health, false, "thawed");
                Assert.That(look.SilhouetteOf(look.Body.sprite), Is.Not.Null, "The flash is found on either palette.");
            }
            finally
            {
                Object.Destroy(body);
            }
#else
            yield return null;
#endif
        }
    }
}
