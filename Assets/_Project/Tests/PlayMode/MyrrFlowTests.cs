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
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // Rootmother Myrr in the scene (MyrrRule, RootmotherMyrr, WardenSlamService; docs/64 sections 3-5 and 16), on
    // Resources/Development/Floor_MyrrProof: the foundry's gate, Myrr alone, no horde. Her mark is a root snare at the hero's
    // spot, on the Warden's clock with her 1.2 s telegraph, that creeps after the hero at MyrrRule.CreepSpeed: a hero who
    // steps out of it and stops is overtaken, one who stands is caught where it stood, one who keeps walking leaves it. The
    // roots take MyrrRule.SnareShare of the hero's maximum and plant it for MyrrRule.PlantSeconds - HeroMovementInput walks
    // it nowhere while its weapons still strike - and her hook is free through that recovery. A snare that goes down with
    // the hero at her feet (MyrrRule.FeetReach) holds no hook. WardenSlamView draws the moving ring (no Warden's ring, no
    // lane), her orchid feet ring and the planted roots. Phase two at half health quickens her beat and grows no saplings
    // (she ships without them); she withers as she falls. And the development autopilot, the careful pilot, walks out of her
    // snares (MyrrRule.EscapeReach), as the simulation's pilot does. Game seconds throughout: 60 frames a second.
    public sealed class MyrrFlowTests
    {
        private const float Frame = 1f / 60f;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Myrr cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private static IEnumerator Load(bool autopilot)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_myrr_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (autopilot)
            {
                File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
                LogAssert.Expect(LogType.Log, new Regex(@"^\[Autopilot\] driving the hero as the careful cohort"));
            }
            Time.captureDeltaTime = Frame;
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

        private static float FloorDistance(Vector2 a, float x, float y) => Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(a.x, a.y, x, y));

        // A floor heading square to the line from Myrr to the hero, on side (+1 or -1): walking it keeps clear of her feet.
        private static Vector2 Across(EncounterController encounter, Health boss, Transform hero, float side)
        {
            Vector2 at = HeroFloor(hero);
            if (!encounter.TryFloorPositionOf(boss, out float bx, out float by))
                return new Vector2(side, 0f);
            Vector2 away = new Vector2(at.x - bx, at.y - by);
            if (away.sqrMagnitude < 1e-4f)
                return new Vector2(side, 0f);
            away.Normalize();
            return new Vector2(-away.y, away.x) * side;
        }

        // Her snare: it creeps after a hero who steps out and stops and catches it; a hero who stands is caught where it
        // stood; one who keeps walking leaves it. The caught hero is planted for a second - the stick walks it nowhere - and
        // her hook is free through the recovery. Then phase two quickens her beat, grows nothing, and she withers.
        [UnityTest, Timeout(240000)] public IEnumerator HerSnareCreepsCatchesAStillHeroPlantsItAndAWalkingHeroEscapes()
        {
            yield return Load(false);
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            var heroInput = Object.FindFirstObjectByType<HeroMovementInput>();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            WardenSlamService service = setup.Warden;
            var view = setup.GetComponent<WardenSlamView>();
            var feel = setup.GetComponent<BossFeelView>();
            var hero = heroInput.GetComponent<Health>();
            Transform heroAt = heroInput.transform;

            yield return Until(setup, () => service.IsFighting && !encounter.IsRevealHeld, 30f);
            Assert.That(service.IsFighting, Is.True, "Myrr is fought at the gate");
            Health boss = service.Boss;
            Assert.That(encounter.DefinitionOf(boss).Id, Is.EqualTo("enemy_rootmother_myrr"));
            Assert.That(service.MarksSnare, Is.True, "her marks are root snares");
            Assert.That(service.MarksLanes, Is.False);
            Assert.That(service.Myrr, Is.SameAs(boss.GetComponent<RootmotherMyrr>()));
            Assert.That(boss.Maximum, Is.EqualTo(encounter.DefinitionOf(boss).MaximumHealth * WardenRule.GateHealthShare).Within(1f),
                "the gate's share (the proof floor sets none of its own)");
            Assert.That(service.Clock.Telegraph, Is.EqualTo(MyrrRule.Telegraph), "her beat");
            var hook = boss.GetComponent<AttackController>();
            yield return null;
            Assert.That(view.IsFeetShown, Is.True, "her orchid feet ring while she is fought");

            int marks = 0;
            bool? hit = null;
            float slammedAt = 0f;
            service.Marked += () => marks++;
            service.Slammed += h =>
            {
                hit = h;
                slammedAt = Time.time;
            };

            // The first snare: down at the hero's spot, her hook held, the ring drawn - and no Warden's ring, no lane.
            yield return Until(setup, () => marks > 0, MyrrRule.Every + 5f);
            Assert.That(marks, Is.EqualTo(1));
            Vector2 at = HeroFloor(heroAt);
            RootSnare snare = service.Snare;
            Assert.That(snare.IsDown, Is.True);
            Assert.That(FloorDistance(at, snare.X, snare.Y), Is.LessThan(.1f), "where the hero stood");
            Assert.That((service.MarkX, service.MarkY), Is.EqualTo((snare.X, snare.Y)), "the mark is the snare's centre");
            Assert.That(snare.Radius, Is.EqualTo(MyrrRule.SnareRadius));
            Assert.That(snare.CreepSpeed, Is.EqualTo(MyrrRule.CreepSpeed));
            Assert.That(service.HookAtHerFeet, Is.False, "the hero far from her");
            Assert.That(hook.IsHeld, Is.True, "her hook held from the snare, as the Warden's hammer");
            yield return null;
            Assert.That(view.IsSnareShown, Is.True, "the snare shows on the floor");
            Assert.That(view.IsMarkShown, Is.False, "and no Warden's ring");
            Assert.That(view.ShownLanes, Is.Zero, "nor a lane");
            float fillBefore = view.SnareFillScale;

            // Step out and stop: the hero walks out of the ring, square to her line, and stands - the circle's habit.
            float start = Time.time;
            heroInput.Hold(Across(encounter, boss, heroAt, 1f));
            while (FloorDistance(HeroFloor(heroAt), snare.X, snare.Y) <= MyrrRule.SnareRadius + .05f &&
                   Time.time - start < MyrrRule.Telegraph)
                yield return null;
            heroInput.Release();
            at = HeroFloor(heroAt);
            Assert.That(FloorDistance(at, snare.X, snare.Y), Is.GreaterThan(MyrrRule.SnareRadius), "the hero stepped out of the ring");
            Assert.That(hit, Is.Null, "before the roots erupt");
            Assert.That(snare.Crept, Is.GreaterThan(0f), "it creeps after the hero");
            float gap = FloorDistance(at, snare.X, snare.Y);
            yield return null;
            yield return null;
            Assert.That(FloorDistance(HeroFloor(heroAt), snare.X, snare.Y), Is.LessThan(gap), "closing on the hero that stopped");
            Assert.That(view.SnareShownAt.x, Is.EqualTo(service.MarkX).Within(.05f), "drawn where it has crept");
            Assert.That(view.SnareShownAt.y, Is.EqualTo(service.MarkY).Within(.05f));
            Assert.That(view.SnareFillScale, Is.GreaterThan(fillBefore), "its fill grows with the telegraph");
            Assert.That(view.SnareTipReach, Is.GreaterThan(0f), "its orchid root tips out on its rim");
            float full = hero.Current;
            yield return Until(setup, () => hit.HasValue, MyrrRule.Telegraph + 3f);
            Assert.That(hit, Is.True, "the snare overtook the hero that stopped");
            Assert.That(snare.Crept, Is.LessThanOrEqualTo(MyrrRule.CreepSpeed * MyrrRule.Telegraph + .05f), "never faster than its creep");
            Assert.That(hero.Current, Is.EqualTo(full - hero.Maximum * MyrrRule.SnareShare).Within(.5f), "a tenth of the hero's maximum");
            Assert.That(snare.IsDown, Is.False, "the roots erupt once");

            // Planted: the hero stands where the roots caught it for a second however the stick steers; her hook is free.
            Assert.That(service.IsHeroPlanted, Is.True);
            Assert.That(heroInput.IsPlanted, Is.True);
            Assert.That(service.HookFree, Is.True);
            Assert.That(hook.IsHeld, Is.False, "her hook free through the recovery after the roots caught the hero");
            Vector3 caughtAt = heroAt.position;
            heroInput.Hold(Across(encounter, boss, heroAt, -1f));
            int plantedFrames = 0;
            bool rootsShown = false;
            while (service.IsHeroPlanted && plantedFrames < 120)
            {
                Assert.That(heroAt.position, Is.EqualTo(caughtAt), "planted: no walk");
                Assert.That(hook.IsHeld, Is.False, "the hook stays free while the hero is planted");
                plantedFrames++;
                yield return null;
                rootsShown |= view.IsPlantedShown;
            }
            Assert.That(plantedFrames * Frame, Is.EqualTo(MyrrRule.PlantSeconds).Within(2.5f * Frame), "planted for the plant's second");
            Assert.That(rootsShown, Is.True, "the roots round the caught hero's feet");
            yield return null;
            yield return null;
            Assert.That(heroAt.position, Is.Not.EqualTo(caughtAt), "then it walks again");
            heroInput.Release();
            yield return null;
            Assert.That(view.IsPlantedShown, Is.False, "the roots gone with the plant");

            // A still hero: the snare goes down on it, never moves, and the roots catch it where it stands.
            hit = null;
            yield return Until(setup, () => marks > 1, MyrrRule.Every + 5f);
            Assert.That(marks, Is.EqualTo(2));
            full = hero.Current;
            yield return Until(setup, () => hit.HasValue, MyrrRule.Telegraph + 3f);
            Assert.That(hit, Is.True, "a hero who stands in the snare is caught");
            Assert.That(snare.Crept, Is.LessThan(.01f), "on a hero that stood, the snare never moved");
            Assert.That(hero.Current, Is.EqualTo(full - hero.Maximum * MyrrRule.SnareShare).Within(.5f));
            Assert.That(service.IsHeroPlanted, Is.True);
            yield return Until(setup, () => !service.IsHeroPlanted, MyrrRule.PlantSeconds + 2f);

            // A walking hero: it keeps walking from the snare's first frame to the eruption and leaves it behind.
            hit = null;
            yield return Until(setup, () => marks > 2, MyrrRule.Every + 5f);
            Assert.That(marks, Is.EqualTo(3));
            Vector2 from = HeroFloor(heroAt);
            heroInput.Hold(Across(encounter, boss, heroAt, 1f));
            full = hero.Current;
            yield return Until(setup, () => hit.HasValue, MyrrRule.Telegraph + 3f);
            heroInput.Release();
            Assert.That(FloorDistance(HeroFloor(heroAt), from.x, from.y), Is.GreaterThan(2.5f), "the hero walked on");
            Assert.That(hit, Is.False, "a hero who keeps walking leaves the snare");
            Assert.That(snare.Crept, Is.GreaterThan(MyrrRule.CreepSpeed * MyrrRule.Telegraph * .8f), "after it all the way");
            Assert.That(hero.Current, Is.EqualTo(full).Within(.01f));
            Assert.That(service.IsHeroPlanted, Is.False, "not planted");

            // Phase two at half health: faster snares, her room's horde thickens, phase two's bloom - and no saplings.
            int enemies = encounter.AliveEnemyCount;
            boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
            yield return null;
            yield return null;
            Assert.That(service.IsEnraged, Is.True);
            Assert.That(encounter.IsBossEnraged, Is.True, "her room's horde thickens");
            Assert.That(feel.ShowsPhaseTwo, Is.True, "her phase two blooms");
            hit = null;
            yield return Until(setup, () => hit.HasValue, MyrrRule.Every + 5f);
            Assert.That(hit.HasValue, Is.True, "a snare erupts in phase two");
            float blowAt = slammedAt;
            int marked = marks;
            float markedAt = 0f;
            service.Marked += () => markedAt = Time.time;
            yield return Until(setup, () => marks > marked, MyrrRule.Every + 5f);
            Assert.That(marks, Is.GreaterThan(marked));
            Assert.That(markedAt - blowAt, Is.EqualTo(MyrrRule.EveryPhaseTwo - MyrrRule.Telegraph).Within(3f * Frame),
                "her next snare on phase two's beat");
            Assert.That(encounter.AliveEnemyCount, Is.EqualTo(enemies), "no saplings: she ships without them");

            // She falls: her fight is over, the snare and her feet ring gone, and she withers.
            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            yield return null;
            yield return null;
            Assert.That(service.IsFighting, Is.False);
            Assert.That(feel.LastDeathWithered, Is.True, "she withers");
            Assert.That(feel.LastDeathShattered, Is.False);
            Assert.That(view.IsFeetShown, Is.False);
            yield return Until(setup, () => !view.IsSnareShown, 2f);
            Assert.That(view.IsSnareShown, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        // Keeps the standing hero at a floor offset from Myrr (its input is off: nothing else moves it).
        private static void Beside(EncounterController encounter, Health boss, Transform hero, float dx, float dy)
        {
            if (encounter.TryFloorPositionOf(boss, out float bx, out float by))
                hero.position = new Vector3(bx + dx, ArenaFloor.WorldY(by + dy), hero.position.z);
        }

        // Her feet (MyrrRule.FeetReach, MyrrRule.HoldsHook): a snare that goes down with the hero at her feet holds no hook -
        // she strikes it through the telegraph and raises no windup, her feet ring pulsing - and roots that catch it free
        // the hook through the recovery, while the planted hero's own weapons still strike her. A snare that goes down with
        // the hero beyond her feet holds the hook from the snare, as the Warden holds its hammer.
        [UnityTest, Timeout(120000)] public IEnumerator HerFeetHoldNoHookAndThePlantedHerosWeaponsStillStrike()
        {
            yield return Load(false);
            var heroInput = Object.FindFirstObjectByType<HeroMovementInput>();
            heroInput.enabled = false;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            WardenSlamService service = setup.Warden;
            var view = setup.GetComponent<WardenSlamView>();
            var hero = heroInput.GetComponent<Health>();
            var heroAttack = heroInput.GetComponent<AttackController>();
            Transform heroAt = heroInput.transform;

            yield return Until(setup, () => service.IsFighting && !encounter.IsRevealHeld, 30f);
            Assert.That(service.MarksSnare, Is.True);
            Health boss = service.Boss;
            var hook = boss.GetComponent<AttackController>();
            Assert.That(hook.enabled, Is.True);
            Assert.That(heroAttack.enabled, Is.True);
            var look = boss.GetComponent<EnemyLookView>();
            int marks = 0, hookStrikes = 0, plantedStrikes = 0, plantedFrames = 0;
            bool? hit = null;
            service.Marked += () => marks++;
            service.Slammed += h => hit = h;
            hook.Struck += _ => hookStrikes++;
            heroAttack.Struck += target =>
            {
                if (target == boss && service.IsHeroPlanted)
                    plantedStrikes++;
            };
            yield return null;
            Assert.That(view.IsFeetShown, Is.True, "her feet ring while she is fought");
            Color orchid = view.FeetRingLine.startColor;
            Assert.That(orchid.r, Is.GreaterThan(orchid.b), "orchid, not the Regent's violet");
            Assert.That(orchid.b, Is.GreaterThan(orchid.g), "a creature's colour, not the telegraph's red-orange");
            Assert.That(view.IsFeetPulsing, Is.False);

            // The first snare goes down with the hero at her feet, 0.8 from her.
            float deadline = Time.realtimeSinceStartup + MyrrRule.Every + 10f;
            while (marks == 0 && Time.realtimeSinceStartup < deadline)
            {
                Beside(encounter, boss, heroAt, .8f, 0f);
                if (hero.IsAlive)
                    hero.Heal(hero.Maximum);
                yield return null;
            }
            Assert.That(marks, Is.EqualTo(1));
            Assert.That(service.HookAtHerFeet, Is.True, "the snare went down at her feet");
            Assert.That(service.HookFree, Is.True);
            Assert.That(hook.IsHeld, Is.False, "no hook held for a hero at her feet");
            yield return null;
            Assert.That(view.IsFeetPulsing, Is.True, "her feet ring pulses through the beat");
            if (look != null)
                Assert.That(look.IsWindingUp, Is.False, "and no windup: her hook keeps swinging");
            // The hero stands where the snare went down: the roots catch it there and plant it at her feet. Through the
            // telegraph and the recovery after (the clock's hold) her hook stays free and strikes, and the planted hero's
            // own weapons still strike her.
            int before = hookStrikes;
            deadline = Time.realtimeSinceStartup + MyrrRule.Telegraph + WardenRule.RecoverySeconds + 3f;
            while (service.Clock.HoldsWeapon && Time.realtimeSinceStartup < deadline)
            {
                Assert.That(hook.IsHeld, Is.False, "through the snare's telegraph and the recovery");
                if (service.IsHeroPlanted)
                    plantedFrames++;
                if (hero.IsAlive)
                    hero.Heal(hero.Maximum);
                yield return null;
            }
            Assert.That(hit, Is.True, "the roots caught the hero standing at her feet");
            Assert.That(hookStrikes, Is.GreaterThan(before), "her hook struck the hero at her feet through the beat");
            Assert.That(plantedFrames * Frame, Is.EqualTo(MyrrRule.PlantSeconds).Within(2.5f * Frame), "planted for the plant's second");
            Assert.That(plantedStrikes, Is.GreaterThan(0), "the planted hero's weapons still strike");

            // The next snare goes down with the hero 4 units off: her hook held from the snare, as the Warden's hammer.
            deadline = Time.realtimeSinceStartup + MyrrRule.Every + 5f;
            while (marks == 1 && Time.realtimeSinceStartup < deadline)
            {
                Beside(encounter, boss, heroAt, 4f, 0f);
                if (hero.IsAlive)
                    hero.Heal(hero.Maximum);
                yield return null;
            }
            Assert.That(marks, Is.EqualTo(2));
            Assert.That(service.HookAtHerFeet, Is.False, "the hero beyond her feet");
            Assert.That(hook.IsHeld, Is.True, "her hook held from the snare");
            yield return null;
            Assert.That(view.IsFeetPulsing, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        // The development autopilot, the careful pilot, reads her snare as the simulation's pilot does - out of it by
        // MyrrRule.EscapeReach from its centre as it creeps - and keeps walking: three snares, at most one catch.
        [UnityTest, Timeout(150000)] public IEnumerator TheAutopilotWalksOutOfHerSnares()
        {
            yield return Load(true);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            Assert.That(Object.FindFirstObjectByType<DevelopmentAutopilot>(), Is.Not.Null, "the autopilot drives the hero");
            EncounterController encounter = setup.Encounters;
            WardenSlamService service = setup.Warden;
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            yield return Until(setup, () => service.IsFighting, 30f);
            Assert.That(service.MarksSnare, Is.True);

            int blows = 0, hits = 0, onTheHero = 0;
            service.Marked += () =>
            {
                if (MyrrRule.InSnare(encounter.HeroFloorX, encounter.HeroFloorY, service.Snare.X, service.Snare.Y, MyrrRule.SnareRadius))
                    onTheHero++;
            };
            service.Slammed += h =>
            {
                blows++;
                if (h)
                    hits++;
            };
            yield return Until(setup, () => blows >= 3 || !hero.IsAlive || !service.IsFighting, 4f * MyrrRule.Every + 15f);
            Assert.That(blows, Is.GreaterThanOrEqualTo(3), "three of her beats");
            Assert.That(onTheHero, Is.EqualTo(blows), "every snare went down on the hero");
            Assert.That(hits, Is.LessThanOrEqualTo(1), $"the careful pilot walked out of her snares: {hits} of {blows} caught it");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
