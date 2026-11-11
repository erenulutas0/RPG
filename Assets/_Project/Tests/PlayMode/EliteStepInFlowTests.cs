using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The Daggers are the elites' weapon (docs/41, status 2026-11-11): in the scene, an elite room's Grunt Captain is an
    // elite the hero's strikes are handed (EncounterController.Elites, AttackController.Elites) and the Daggers land on it
    // double (EliteRule); and the development autopilot, the careful pilot, steps in on it while the Daggers are its best
    // weapon on it (AutopilotCohort.Habit.StepsIn, FootworkRule.StepIn) - it holds the Captain within the Daggers' reach
    // rather than kiting it from 1.75, outside their 1.8. On Resources/Development/Floor_EliteProof: one elite room, a
    // Captain alone, no horde.
    public sealed class EliteStepInFlowTests
    {
        [UnitySetUp] public IEnumerator Begin()
        {
            TestProfile.Begin(new PlayerProfile(0, null, null, 0, new[] { EliteRule.DaggersId }, EliteRule.DaggersId));
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Elite step-in cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(90000)] public IEnumerator TheAutopilotStepsInOnACaptainWithTheDaggersAndTheyLandDouble()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_elite_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            var input = Object.FindFirstObjectByType<HeroMovementInput>();
            var hero = input.GetComponent<Health>();
            var attack = input.GetComponent<AttackController>();
            Assert.That(Object.FindFirstObjectByType<DevelopmentAutopilot>(), Is.Not.Null, "the autopilot drives the hero");
            Assert.That(setup.Upgrades.Loadout.At(0).WeaponId, Is.EqualTo(EliteRule.DaggersId), "the run starts with the Daggers");
            WeaponRuntime daggers = setup.Upgrades.Loadout.At(0).Runtime;
            Assert.That(daggers.EliteShare, Is.EqualTo(EliteRule.DaggersShare));

            float deadline = Time.realtimeSinceStartup + 20f;
            while (encounter.EliteLeader == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Health captain = encounter.EliteLeader;
            Assert.That(captain, Is.Not.Null, "the elite room's Captain walks in");
            Assert.That(encounter.Elites, Has.Member(captain), "it is an elite");
            Assert.That(attack.Elites, Is.SameAs(encounter.Elites), "the hero's strikes are handed the elites standing");

            // Every Daggers landing on it is double its own (a crit double again).
            int landings = 0, doubled = 0;
            captain.Damaged += context =>
            {
                landings++;
                float plain = daggers.Damage * EliteRule.DaggersShare;
                if (Mathf.Abs(context.Amount - plain) < .01f || Mathf.Abs(context.Amount - plain * 2f) < .01f)
                    doubled++;
            };

            // While it stands within the step-in's range and the hero is safe, the hero holds it within the Daggers'
            // reach: the frames it is, against the frames it could be.
            int safe = 0, inReach = 0;
            deadline = Time.realtimeSinceStartup + 30f;
            while (captain.IsAlive && hero.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                if (!setup.Choices.IsOpen && hero.Current >= hero.Maximum * FootworkRule.StepInHealth &&
                    encounter.TryFloorPositionOf(captain, out float cx, out float cy))
                {
                    float d = Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(input.Motion.X, input.Motion.Y, cx, cy));
                    if (d <= FootworkRule.StepInRange)
                    {
                        safe++;
                        if (d <= daggers.Range + .05f)
                            inReach++;
                    }
                }
                yield return null;
            }
            Assert.That(safe, Is.GreaterThan(30), "the Captain stood within the step-in's range a while");
            Assert.That(inReach, Is.GreaterThan(safe / 2), $"held within the Daggers' reach in {inReach} of {safe} frames");
            Assert.That(landings, Is.GreaterThan(0), "the Daggers reached it");
            Assert.That(doubled, Is.GreaterThan(0), "and landed double on it");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
