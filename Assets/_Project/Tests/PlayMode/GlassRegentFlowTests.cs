using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
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
    // The Glass Regent in the scene (RegentRule, GlassRegent, WardenSlamService; docs/50 section 6 and the lead's decisions
    // 2026-11-12), on Resources/Development/Floor_RegentProof: the foundry's gate, the Regent alone, no horde. Its mark is a
    // lane from where it stands through the hero's spot, on the Warden's clock with its weapon held from the mark: a hero
    // who stays on it, or steps along it, is caught for RegentRule.LaneShare of its maximum; one who steps across it is not.
    // WardenSlamView lays the lane - the Regent's two sprites - and no Warden's ring. And the development autopilot, the
    // careful pilot, sidesteps the lanes (FootworkRule.Sidestep). Since 2026-11-17 the Regent holds its rod through a beat
    // only for a hero beyond its feet (RegentRule.FeetReach): one at its feet is struck through the cast.
    public sealed class GlassRegentFlowTests
    {
        [UnitySetUp] public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Glass Regent cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static IEnumerator Load(bool autopilot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_regent_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (autopilot)
            {
                File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
                LogAssert.Expect(LogType.Log, new Regex(@"^\[Autopilot\] driving the hero as the careful cohort"));
            }
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
        }

        private static void ResolveUpgrades(CombatSetup setup)
        {
            for (int i = 0; i < 8 && setup.Choices.Current != null && setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
        }

        private static IEnumerator Until(CombatSetup setup, System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                ResolveUpgrades(setup);
                yield return null;
            }
        }

        private static Vector2 HeroFloor(Transform hero) => new Vector2(hero.position.x, ArenaFloor.FloorY(hero.position.y));

        // Moves the standing hero by a floor offset (its input is off: nothing else moves it).
        private static void Step(Transform hero, float dx, float dy) =>
            hero.position += new Vector3(dx, ArenaFloor.WorldY(dy), 0f);

        [UnityTest, Timeout(120000)] public IEnumerator TheRegentsLaneCatchesAHeroOnItOrAlongItAndNotOneAcrossIt()
        {
            yield return Load(false);
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            var heroInput = Object.FindFirstObjectByType<HeroMovementInput>();
            heroInput.enabled = false;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            WardenSlamService service = setup.Warden;
            var view = setup.GetComponent<WardenSlamView>();
            var hero = heroInput.GetComponent<Health>();
            Transform heroAt = heroInput.transform;

            yield return Until(setup, () => service.IsFighting && !encounter.IsRevealHeld, 30f);
            Assert.That(service.IsFighting, Is.True, "the Regent is fought at the gate");
            Health boss = service.Boss;
            Assert.That(encounter.DefinitionOf(boss).Id, Is.EqualTo("enemy_glass_regent"));
            Assert.That(service.MarksLanes, Is.True, "its marks are lanes");
            Assert.That(service.Regent, Is.SameAs(boss.GetComponent<GlassRegent>()));
            Assert.That(boss.Maximum, Is.EqualTo(encounter.DefinitionOf(boss).MaximumHealth * WardenRule.GateHealthShare).Within(1f),
                "the gate's share, as the Warden's");
            Assert.That(service.Clock.Telegraph, Is.EqualTo(RegentRule.Telegraph), "the Regent's beat");
            var rod = boss.GetComponent<AttackController>();

            int marks = 0;
            bool? hit = null;
            service.Marked += () => marks++;
            service.Slammed += h => hit = h;

            // The first mark: a lane from the Regent through the hero's spot, the lane drawn and no ring.
            yield return Until(setup, () => marks > 0, RegentRule.Every + 5f);
            Assert.That(marks, Is.EqualTo(1));
            Assert.That(service.Lanes.Count, Is.EqualTo(1), "one lane: the Regent's");
            Vector2 at = HeroFloor(heroAt);
            Assert.That(ArenaFloor.FloorDistanceSquared(service.Lanes.MarkX, service.Lanes.MarkY, at.x, at.y), Is.LessThan(.01f),
                "through where the hero stood");
            Assert.That(encounter.TryFloorPositionOf(boss, out float bx, out float by), Is.True);
            Assert.That(ArenaFloor.FloorDistanceSquared(service.Lanes.OriginX(0), service.Lanes.OriginY(0), bx, by), Is.LessThan(.01f),
                "from where the Regent stood");
            Assert.That(rod.IsHeld, Is.True, "its rod is held from the mark, as the Warden's hammer");
            // The fight's first lane is cast while the Regent still walks out of the gate, about 12 units from a hero that
            // waited for it: the lane reaches through the hero's spot and on all the same (RegentRule.LanePast).
            float reach = Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(service.Lanes.MarkX, service.Lanes.MarkY,
                service.Lanes.OriginX(0), service.Lanes.OriginY(0)));
            Assert.That(service.Lanes.LengthOf(0), Is.GreaterThanOrEqualTo(RegentRule.LaneLength));
            Assert.That(service.Lanes.LengthOf(0), Is.GreaterThanOrEqualTo(reach + RegentRule.LanePast - 1e-3f),
                $"past the hero's spot, {reach:0.0} units from the Regent");
            Assert.That(service.Lanes.Holds(at.x, at.y, 0f), Is.True, "the hero stands in it");
            yield return null;
            Assert.That(view.ShownLanes, Is.EqualTo(1), "the lane shows on the floor");
            Assert.That(view.IsMarkShown, Is.False, "and no Warden's ring");
            float fillBefore = view.LaneFillSize(0).y;
            yield return Until(setup, () => service.Clock.TelegraphProgress > .5f, 2f);
            Assert.That(view.LaneFillSize(0).y, Is.GreaterThan(fillBefore), "the fill grows across the lane with the telegraph");
            Assert.That(view.LaneFillSize(0).y, Is.LessThanOrEqualTo(RegentRule.LaneWidth + 1e-4f));
            Assert.That(view.LaneFillSize(0).x, Is.EqualTo(service.Lanes.LengthOf(0)).Within(1e-4f), "drawn as long as it reaches");

            // A hero who stays on the lane is caught.
            float full = hero.Current;
            yield return Until(setup, () => hit.HasValue, RegentRule.Telegraph + 3f);
            Assert.That(hit, Is.True, "a hero who stays on the lane is caught");
            Assert.That(hero.Current, Is.EqualTo(full - hero.Maximum * RegentRule.LaneShare).Within(.5f));
            Assert.That(service.Lanes.Count, Is.Zero, "the lane is gone with the blow");

            // The next lane: a step along it, away from the Regent, two units - still in it.
            hit = null;
            yield return Until(setup, () => marks > 1, RegentRule.Every + 5f);
            Assert.That(marks, Is.EqualTo(2));
            service.Lanes.Direction(0, out float dx, out float dy);
            Step(heroAt, dx * 2f, dy * 2f);
            at = HeroFloor(heroAt);
            Assert.That(service.Lanes.Holds(at.x, at.y, 0f), Is.True, "along the lane is still on it");
            full = hero.Current;
            yield return Until(setup, () => hit.HasValue, RegentRule.Telegraph + 3f);
            Assert.That(hit, Is.True, "running along the lane does not help");
            Assert.That(hero.Current, Is.EqualTo(full - hero.Maximum * RegentRule.LaneShare).Within(.5f));

            // The next: the sidestep - the nearest spot clear of it - and the shards miss.
            hit = null;
            yield return Until(setup, () => marks > 2, RegentRule.Every + 5f);
            Assert.That(marks, Is.EqualTo(3));
            at = HeroFloor(heroAt);
            Assert.That(FootworkRule.Sidestep(at.x, at.y, service.Lanes, out float gx, out float gy), Is.True);
            Step(heroAt, gx - at.x, gy - at.y);
            Assert.That(Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(gx, gy, at.x, at.y)), Is.LessThan(2f), "a short step across");
            full = hero.Current;
            yield return Until(setup, () => hit.HasValue, RegentRule.Telegraph + 3f);
            Assert.That(hit, Is.False, "a hero who steps across the lane is not caught");
            Assert.That(hero.Current, Is.EqualTo(full).Within(.01f));

            // Below half health it is enraged, as the Warden is.
            boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
            yield return null;
            yield return null;
            Assert.That(service.IsEnraged, Is.True);
            Assert.That(encounter.IsBossEnraged, Is.True, "its room's horde thickens");
            LogAssert.NoUnexpectedReceived();
        }

        // Keeps the standing hero at a floor offset from the Regent (its input is off: nothing else moves it).
        private static void Beside(EncounterController encounter, Health boss, Transform hero, float dx, float dy)
        {
            if (encounter.TryFloorPositionOf(boss, out float bx, out float by))
                hero.position = new Vector3(bx + dx, ArenaFloor.WorldY(by + dy), hero.position.z);
        }

        // The rod at its feet (RegentRule.FeetReach, RegentRule.HoldsRod; docs/50 "The Regent's bite for the standing hero",
        // 2026-11-17): a lane that goes down with the hero at the Regent's feet holds no rod - the Regent strikes it through
        // the cast and raises no windup, its feet ring pulsing; a lane that goes down with the hero beyond its feet holds the
        // rod from the mark until the shards' recovery, as the Warden holds its hammer.
        [UnityTest, Timeout(120000)] public IEnumerator TheRegentKeepsItsRodForAHeroAtItsFeet()
        {
            yield return Load(false);
            var heroInput = Object.FindFirstObjectByType<HeroMovementInput>();
            heroInput.enabled = false;
            // The hero's own weapons off, so the Regent stands through the beats; its rod stays on.
            heroInput.GetComponent<AttackController>().enabled = false;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            WardenSlamService service = setup.Warden;
            var view = setup.GetComponent<WardenSlamView>();
            Transform heroAt = heroInput.transform;

            yield return Until(setup, () => service.IsFighting && !encounter.IsRevealHeld, 30f);
            Assert.That(service.MarksLanes, Is.True);
            Health boss = service.Boss;
            var rod = boss.GetComponent<AttackController>();
            Assert.That(rod.enabled, Is.True);
            var look = boss.GetComponent<EnemyLookView>();
            int marks = 0, strikes = 0;
            service.Marked += () => marks++;
            rod.Struck += _ => strikes++;
            yield return null;
            Assert.That(view.IsFeetShown, Is.True, "the ring round its feet shows while it is fought");
            Assert.That(view.IsFeetPulsing, Is.False);

            // The first lane goes down with the hero at its feet, 0.8 from it.
            float deadline = Time.realtimeSinceStartup + RegentRule.Every + 5f;
            while (marks == 0 && Time.realtimeSinceStartup < deadline)
            {
                Beside(encounter, boss, heroAt, .8f, 0f);
                yield return null;
            }
            Assert.That(marks, Is.EqualTo(1));
            Assert.That(service.Lanes.AtItsFeet(RegentRule.FeetReach), Is.True, "the mark went down at its feet");
            Assert.That(service.RodAtItsFeet, Is.True);
            Assert.That(rod.IsHeld, Is.False, "no rod held for a hero at its feet");
            yield return null;
            Assert.That(view.IsFeetPulsing, Is.True, "the ring pulses through the beat");
            if (look != null)
                Assert.That(look.IsWindingUp, Is.False, "and no windup: the rod keeps swinging");
            int before = strikes;
            deadline = Time.realtimeSinceStartup + RegentRule.Telegraph + WardenRule.RecoverySeconds + 3f;
            while (service.Clock.HoldsWeapon && Time.realtimeSinceStartup < deadline)
            {
                Beside(encounter, boss, heroAt, .8f, 0f);
                Assert.That(rod.IsHeld, Is.False, "through the cast and the shards' run");
                yield return null;
            }
            Assert.That(strikes, Is.GreaterThan(before), "the rod struck the hero at its feet through the beat");

            // The next lane goes down with the hero 4 units off: the rod is held from the mark, as the Warden's hammer.
            deadline = Time.realtimeSinceStartup + RegentRule.Every + 5f;
            while (marks == 1 && Time.realtimeSinceStartup < deadline)
            {
                Beside(encounter, boss, heroAt, 4f, 0f);
                yield return null;
            }
            Assert.That(marks, Is.EqualTo(2));
            Assert.That(service.RodAtItsFeet, Is.False, "the hero beyond its feet");
            Assert.That(rod.IsHeld, Is.True, "the rod held from the mark");
            yield return null;
            Assert.That(view.IsFeetPulsing, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)] public IEnumerator TheAutopilotSidestepsTheRegentsLanes()
        {
            yield return Load(true);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(Object.FindFirstObjectByType<DevelopmentAutopilot>(), Is.Not.Null, "the autopilot drives the hero");
            EncounterController encounter = setup.Encounters;
            WardenSlamService service = setup.Warden;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            yield return Until(setup, () => service.IsFighting, 30f);
            Assert.That(service.MarksLanes, Is.True);

            // Each blow, and whether the hero stood in its lane as it went down (it does: the lane runs through its spot).
            int blows = 0, hits = 0, markedOnIt = 0;
            service.Marked += () =>
            {
                if (service.Lanes.Holds(encounter.HeroFloorX, encounter.HeroFloorY, 0f))
                    markedOnIt++;
            };
            service.Slammed += h =>
            {
                blows++;
                if (h)
                    hits++;
            };
            yield return Until(setup, () => blows >= 3 || !hero.IsAlive || !service.IsFighting, 4f * RegentRule.Every + 15f);
            Assert.That(blows, Is.GreaterThanOrEqualTo(3), "three of the Regent's beats");
            Assert.That(markedOnIt, Is.EqualTo(blows), "every lane ran through the hero's spot");
            Assert.That(hits, Is.LessThanOrEqualTo(1), $"the careful pilot stepped across the lanes: {hits} of {blows} caught it");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
