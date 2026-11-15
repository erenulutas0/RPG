using System.Collections;
using System.IO;
using Cryptforge.Art;
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
    // The Cinder Bloom in the scene (BloomRule, CinderBloom, GloamService, CinderBloomView; docs/52 section 5 and the lead's
    // decisions 2026-11-15, `bloom-m20-h70-r1.2`), on Resources/Development/Floor_BloomProof: one elite room, the Bloom alone,
    // the foundry, no horde, the hero with the Daggers and no autopilot. The hero stands and strikes through a whole closed
    // phase and a whole open window, read in game seconds: while the Bloom is closed every landing deals BloomRule.BarkShare
    // of the Daggers' landing on an elite, while open the whole landing, and its maul never swings while open. When it opens
    // the hero is set down out of its reach: it stays rooted where it stood for the rest of the window, its open bloom turned
    // to the hero, and walks again once closed. Kept 1.5 from it, outside the maul's 1.2, the hero is never struck, and every
    // maul that lands finds the hero within 1.2. A level-up panel holds its clock and its place.
    public sealed class CinderBloomFlowTests
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
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Cinder Bloom cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(180000)] public IEnumerator TheBarkBluntsTheOpenBloomTakesAllRootsAndHoldsItsMaulAndAPanelHoldsTheCycle()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_bloom_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            var input = Object.FindFirstObjectByType<HeroMovementInput>();
            var hero = input.GetComponent<Health>();
            Assert.That(Object.FindFirstObjectByType<DevelopmentAutopilot>(), Is.Null, "no autopilot: the hero stands");
            Assert.That(setup.Gloam, Is.Not.Null);
            Assert.That(setup.Gloam.IsActive, Is.True, "the foundry steps the Blooms");
            Assert.That(setup.Upgrades.Loadout.At(0).WeaponId, Is.EqualTo(EliteRule.DaggersId));
            WeaponRuntime daggers = setup.Upgrades.Loadout.At(0).Runtime;

            float deadline = Time.realtimeSinceStartup + 20f;
            while (encounter.EliteLeader == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Health body = encounter.EliteLeader;
            Assert.That(body, Is.Not.Null, "the Bloom walks in");
            Assert.That(encounter.DefinitionOf(body).Id, Is.EqualTo("enemy_cinder_bloom"));
            Assert.That(encounter.Elites, Has.Member(body), "it leads the elite room: the Daggers' x2 is on it");
            var bloom = body.GetComponent<CinderBloom>();
            Assert.That(bloom, Is.Not.Null, "it carries its bark");
            var view = body.GetComponent<CinderBloomView>();
            Assert.That(view, Is.Not.Null, "and its open-state look");
            var maul = body.GetComponent<AttackController>();
            Assert.That(maul.Weapon.Range, Is.EqualTo(1.2f), "the Bark Maul reaches 1.2");

            // Every landing of the hero's on it, against the Daggers' landing on an elite, as the clock stood.
            int closedBlunted = 0, closedOther = 0, openWhole = 0, openOther = 0;
            body.Damaged += context =>
            {
                if (!ReferenceEquals(context.Source, hero) || context.IsTick || !bloom.IsCycling)
                    return;
                float whole = daggers.Damage * EliteRule.DaggersShare * (context.IsCritical ? daggers.Pattern.CritMultiplier : 1f);
                if (bloom.Clock.IsOpen)
                {
                    if (Mathf.Abs(context.Amount - whole) < .01f)
                        openWhole++;
                    else
                        openOther++;
                }
                else if (Mathf.Abs(context.Amount - whole * BloomRule.BarkShare) < .01f)
                    closedBlunted++;
                else
                    closedOther++;
            };
            // Every maul: where the hero stood, and whether the Bloom was open.
            int mauls = 0, maulsOpen = 0, maulsOutOfReach = 0;
            float farthestMaul = 0f;
            maul.Attacked += () =>
            {
                mauls++;
                if (bloom.IsOpen)
                    maulsOpen++;
                if (encounter.TryFloorPositionOf(body, out float bx, out float by))
                {
                    float d = Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(encounter.HeroFloorX, encounter.HeroFloorY, bx, by));
                    farthestMaul = Mathf.Max(farthestMaul, d);
                    if (d > maul.Weapon.Range + .02f)
                        maulsOutOfReach++;
                }
            };
            int struck = 0;
            hero.Damaged += context =>
            {
                if (ReferenceEquals(context.Source, body))
                    struck++;
            };
            int heldWhileOpen = 0, openFrames = 0, openFrameShown = 0, windowsWatched = 0;
            float openSeconds = 0f;
            bool wasOpen = false, openWatched = false;
            void Watch()
            {
                if (hero.IsAlive && hero.Current < hero.Maximum)
                    hero.Heal(hero.Maximum);
                bool open = bloom.IsOpen;
                // A window watched from its first frame to its close: game seconds, so a loaded machine's frames do not count.
                if (open && !wasOpen)
                    openWatched = true;
                if (!open && wasOpen && openWatched)
                    windowsWatched++;
                wasOpen = open;
                if (!open)
                    return;
                openFrames++;
                openSeconds += Time.deltaTime;
                if (maul.IsHeld)
                    heldWhileOpen++;
                if (view.OpenFrame >= 0)
                    openFrameShown++;
            }

            // The hero stands and the Bloom walks up to it: a closed phase, a whole open window, a closed phase again.
            deadline = Time.realtimeSinceStartup + 90f;
            while ((closedBlunted < 4 || openWhole < 3 || mauls < 1 || windowsWatched < 1) && body.IsAlive && hero.IsAlive &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Watch();
            }
            Assert.That(closedBlunted, Is.GreaterThanOrEqualTo(4), "landings on the closed bark, each a quarter of the Daggers' landing");
            Assert.That(openWhole, Is.GreaterThanOrEqualTo(3), "landings on the open bloom, each whole");
            Assert.That((closedOther, openOther), Is.EqualTo((0, 0)), "every landing was blunted while closed or whole while open");
            Assert.That(mauls, Is.GreaterThan(0), "the closed Bloom mauled the standing hero");
            Assert.That(maulsOpen, Is.Zero, "no maul while open");
            Assert.That(windowsWatched, Is.GreaterThanOrEqualTo(1), "a whole open window was watched");
            Assert.That(openSeconds, Is.GreaterThanOrEqualTo(BloomRule.OpenSeconds - .05f), $"open {openSeconds:0.00} s of game time a window or more");
            Assert.That(heldWhileOpen, Is.EqualTo(openFrames), "its maul held every open frame");
            // The look draws at the frame's end (LateUpdate), after this coroutine reads it: a window's first frame still shows
            // the closed body here, every later one the open state.
            Assert.That(openFrameShown, Is.GreaterThanOrEqualTo(openFrames - 3), "the open-state frames shown through the window");
            Assert.That(openFrameShown, Is.GreaterThan(0));
            Assert.That(view.Chips, Is.GreaterThan(0), "a blunted landing played the bark chip");

            // Rooted: as a window opens the hero is set down 3 out of its reach; the Bloom stands where it stood until the
            // window closes, then walks again. It stands whole again first, so the phases below have a Bloom to read.
            body.Heal(body.Maximum);
            deadline = Time.realtimeSinceStartup + 20f;
            while (!(bloom.IsOpen && bloom.Clock.OpenFor < .3f) && body.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Watch();
            }
            Assert.That(bloom.IsOpen, Is.True, "a window opened");
            yield return null; // the pack's step after the rooting
            Watch();
            Assert.That(encounter.TryFloorPositionOf(body, out float rx, out float ry), Is.True);
            Assert.That(PlaceAway(input, hero, rx, ry, 3f), Is.True, "a spot 3 from the Bloom on the floor");
            bool heroRight = encounter.HeroFloorX > rx;
            float drift = 0f;
            while (bloom.IsOpen && body.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Watch();
                Assert.That(encounter.TryFloorPositionOf(body, out float ox, out float oy), Is.True);
                drift = Mathf.Max(drift, Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(ox, oy, rx, ry)));
            }
            Assert.That(drift, Is.LessThan(1e-3f), "rooted for the rest of the window, the hero out of reach");
            Assert.That(view.FacesMirrored, Is.EqualTo(heroRight), "the rooted, open Bloom turned to the hero's side");
            float walkedUntil = Time.realtimeSinceStartup + 10f, walked = 0f;
            for (int frame = 0; frame < 60 && Time.realtimeSinceStartup < walkedUntil; frame++)
            {
                yield return null;
                Watch();
                if (encounter.TryFloorPositionOf(body, out float wx, out float wy))
                    walked = Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(wx, wy, rx, ry));
            }
            Assert.That(walked, Is.GreaterThan(.3f), "closed again, it walks after the hero");

            // The maul's reach: through a closed phase the hero is kept 1.5 from the Bloom, outside its 1.2; nothing lands.
            deadline = Time.realtimeSinceStartup + 20f;
            while (bloom.IsOpen && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Watch();
            }
            int struckBefore = struck, maulsBefore = mauls;
            float keptSeconds = 0f;
            while (!bloom.IsOpen && keptSeconds < 2.5f && body.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                if (encounter.TryFloorPositionOf(body, out float kx, out float ky))
                    PlaceAway(input, hero, kx, ky, 1.5f);
                yield return null;
                keptSeconds += Time.deltaTime;
                Watch();
            }
            Assert.That(keptSeconds, Is.GreaterThan(1.8f), "kept outside the maul for more than its interval");
            Assert.That(struck, Is.EqualTo(struckBefore), "no maul reaches 1.5");
            Assert.That(mauls, Is.EqualTo(maulsBefore), "it never swung at a hero outside its reach");
            Assert.That(maulsOutOfReach, Is.Zero, $"every maul found the hero within 1.2 (farthest {farthestMaul:0.00})");

            // A level-up panel holds the cycle: its clock and its place stand while the choice is open, and run on after.
            Assert.That(encounter.TryFloorPositionOf(body, out float px, out float py), Is.True);
            float age = bloom.Clock.Age;
            setup.Run.GrantBonusUpgrade();
            yield return null;
            Assert.That(setup.Choices.IsOpen, Is.True, "a choice is open");
            float heldAge = bloom.Clock.Age;
            Assert.That(heldAge - age, Is.LessThanOrEqualTo(1f / 60f + 1e-4f), "at most the frame the panel opened on");
            for (int frame = 0; frame < 90; frame++)
            {
                yield return null;
                Assert.That(bloom.Clock.Age, Is.EqualTo(heldAge), "the clock holds under the panel");
            }
            Assert.That(encounter.TryFloorPositionOf(body, out float hx, out float hy), Is.True);
            Assert.That(Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(hx, hy, px, py)), Is.LessThan(.05f), "and so does the Bloom");
            FoundryRunDriver.Keep(setup, hero);
            for (int frame = 0; frame < 30; frame++)
                yield return null;
            Assert.That(setup.Choices.IsOpen, Is.False);
            Assert.That(bloom.Clock.Age, Is.GreaterThan(heldAge + .3f), "the clock runs again once the choice is answered");
            LogAssert.NoUnexpectedReceived();
        }

        // Sets the hero down distance from (x, y) on the floor, the first bearing of a turn that stands on the platform.
        private static bool PlaceAway(HeroMovementInput input, Health hero, float x, float y, float distance)
        {
            for (int step = 0; step < 24; step++)
            {
                float angle = step * Mathf.PI / 12f;
                float hx = x + distance * Mathf.Cos(angle), hy = y + distance * Mathf.Sin(angle);
                if (!ArenaGeometry.Foundry.IsOnPlatform(hx, hy, HeroMotion.EdgeMargin) || !FoundryMap.Contains(hx, hy, HeroMotion.EdgeMargin))
                    continue;
                input.Motion.Place(hx, hy, ArenaGeometry.Foundry);
                // A standing hero's transform follows its motion only when it walks (HeroMovementInput), so set both.
                FoundryRunDriver.Place(hero, hx, hy);
                return true;
            }
            return false;
        }
    }
}
