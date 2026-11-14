using System;
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
    // The Mirror Sentry in the scene (MirrorGuardRule, MirrorGuard, MirrorGuardService; docs/50 section 5 and the lead's
    // decisions 2026-11-12), on Resources/Development/Floor_SentryProof: one elite room, the Sentry alone, the foundry, no
    // horde, the hero with the Daggers and no autopilot. The hero stands and strikes: the mirror turns to it and every
    // landing from the front deals MirrorGuardRule.FrontShare of itself and reflects, when the rule reflects at all; then
    // the hero is set down behind it: the mirror turns after it at 45 degrees a second, never faster, and every landing
    // from behind is whole and reflects nothing. What the hero is reflected stays within MirrorGuardRule.ReflectCap of its
    // maximum a second - nothing as it ships (sentry-r0-f70) - and a blunted landing still flashes the mirror's violet
    // glint, never two within MirrorGuardService.BluntGlintSeconds.
    public sealed class MirrorSentryFlowTests
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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Mirror Sentry cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(120000)] public IEnumerator TheMirrorTurnsAtItsRateHalvesAndReflectsTheFrontAndTakesTheRearWhole()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_sentry_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;

            var setup = Object.FindFirstObjectByType<CombatSetup>();
            EncounterController encounter = setup.Encounters;
            var input = Object.FindFirstObjectByType<HeroMovementInput>();
            var hero = input.GetComponent<Health>();
            Assert.That(Object.FindFirstObjectByType<DevelopmentAutopilot>(), Is.Null, "no autopilot: the hero stands");
            Assert.That(setup.Mirrors, Is.Not.Null);
            Assert.That(setup.Mirrors.IsActive, Is.True, "the foundry steps the mirrors");
            Assert.That(setup.Upgrades.Loadout.At(0).WeaponId, Is.EqualTo(EliteRule.DaggersId));
            WeaponRuntime daggers = setup.Upgrades.Loadout.At(0).Runtime;

            float deadline = Time.realtimeSinceStartup + 20f;
            while (encounter.EliteLeader == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Health sentry = encounter.EliteLeader;
            Assert.That(sentry, Is.Not.Null, "the Sentry walks in");
            Assert.That(encounter.DefinitionOf(sentry).Id, Is.EqualTo("enemy_mirror_sentry"));
            Assert.That(encounter.Elites, Has.Member(sentry), "it leads the elite room: the Daggers' x2 is on it");
            var mirror = sentry.GetComponent<MirrorGuard>();
            Assert.That(mirror, Is.Not.Null, "it carries its mirror");
            Assert.That(Object.FindFirstObjectByType<MirrorGlintView>(), Is.Not.Null, "and the glint view stands");

            // Each landing of the hero's on it, judged as the guard judges it - the hero's place against the mirror's facing
            // at that moment - against the Daggers' landing on an elite, whole or its front share.
            int frontBlunted = 0, frontOther = 0, rearWhole = 0, rearOther = 0;
            sentry.Damaged += context =>
            {
                if (!ReferenceEquals(context.Source, hero) || context.IsTick ||
                    !encounter.TryFloorPositionOf(sentry, out float sx, out float sy))
                    return;
                bool front = mirror.State.IsGuarding && MirrorGuardRule.InFront(mirror.State.Facing, sx, sy,
                    encounter.HeroFloorX, encounter.HeroFloorY, MirrorGuardRule.ArcDegrees);
                float whole = daggers.Damage * EliteRule.DaggersShare * (context.IsCritical ? daggers.Pattern.CritMultiplier : 1f);
                if (front)
                {
                    if (Mathf.Abs(context.Amount - whole * MirrorGuardRule.FrontShare) < .01f)
                        frontBlunted++;
                    else
                        frontOther++;
                }
                else if (Mathf.Abs(context.Amount - whole) < .01f)
                    rearWhole++;
                else
                    rearOther++;
            };
            float reflected = 0f;
            int reflections = 0;
            setup.Mirrors.Reflected += (from, amount) =>
            {
                reflected += amount;
                reflections++;
            };
            // The mirror's look for a blunted landing (MirrorGuardService.Blunted): no damage, at most one a BluntGlintSeconds.
            int blunts = 0, tooSoon = 0, otherMirrors = 0;
            float lastBlunt = float.NegativeInfinity;
            setup.Mirrors.Blunted += from =>
            {
                if (!ReferenceEquals(from, mirror))
                    otherMirrors++;
                if (Time.time - lastBlunt < MirrorGuardService.BluntGlintSeconds - 1e-3f)
                    tooSoon++;
                lastBlunt = Time.time;
                blunts++;
            };
            var glints = Object.FindFirstObjectByType<MirrorGlintView>();

            // The mirror's turn, frame by frame, while it guards: never more than 45 degrees a second.
            float last = float.NaN, guardedSeconds = 0f, maximum = hero.Maximum;
            int turns = 0, atRate = 0, tooFast = 0;
            void Watch()
            {
                maximum = Mathf.Max(maximum, hero.Maximum);
                if (!mirror.State.IsGuarding)
                    return;
                float facing = mirror.State.Facing;
                if (!float.IsNaN(last) && Time.deltaTime > 0f)
                {
                    guardedSeconds += Time.deltaTime;
                    float step = MirrorGuardRule.TurnStep(MirrorGuardRule.TurnDegrees, Time.deltaTime);
                    float turned = Mathf.Abs(MirrorGuardRule.Wrap(facing - last));
                    turns++;
                    if (turned > step + 1e-4f)
                        tooFast++;
                    if (turned > step * .99f)
                        atRate++;
                }
                last = facing;
            }

            // The hero stands and the Sentry walks up to it: its landings meet the mirror.
            deadline = Time.realtimeSinceStartup + 25f;
            while (frontBlunted < 4 && sentry.IsAlive && hero.IsAlive && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
                Watch();
            }
            Assert.That(frontBlunted, Is.GreaterThanOrEqualTo(4), "landings from the front, each the front share of the Daggers' landing");
            Assert.That(rearWhole, Is.Zero, "the hero stood before the mirror throughout");

            // Set down behind it, a short step inside the Daggers' reach: the mirror has a half-turn to make.
            Assert.That(encounter.TryFloorPositionOf(sentry, out float x, out float y), Is.True);
            float behind = mirror.State.Facing + Mathf.PI;
            bool placed = false;
            foreach (float radius in new[] { 1.3f, 1.1f, 1.5f, .9f })
            {
                float bx = x + radius * Mathf.Cos(behind), by = y + radius * Mathf.Sin(behind);
                if (!ArenaGeometry.Foundry.IsOnPlatform(bx, by, HeroMotion.EdgeMargin) || !FoundryMap.Contains(bx, by, HeroMotion.EdgeMargin))
                    continue;
                input.Motion.Place(bx, by, ArenaGeometry.Foundry);
                // A standing hero's transform follows its motion only when it walks (HeroMovementInput), so set both.
                FoundryRunDriver.Place(hero, bx, by);
                placed = true;
                break;
            }
            Assert.That(placed, Is.True, "a spot behind the Sentry on the floor");
            // The shipped guard takes 70 % of a front landing (r0-f70), so the front phase can leave the Sentry under the
            // rear window's 20 % floor; it stands whole again so the rear phase has landings to measure.
            sentry.Heal(sentry.Maximum);
            int frontBefore = frontBlunted, reflectionsBefore = reflections;
            float rearUntil = Time.realtimeSinceStartup + 20f, rearSeconds = 0f;
            while (rearSeconds < 1.5f && sentry.IsAlive && sentry.Current > sentry.Maximum * .2f && hero.IsAlive &&
                   Time.realtimeSinceStartup < rearUntil)
            {
                yield return null;
                rearSeconds += Time.deltaTime;
                Watch();
            }
            Assert.That(rearWhole, Is.GreaterThan(0), "landings from behind, each the Daggers' whole landing (front " + frontBlunted + "/" + frontOther + ", rear other " + rearOther + ", rear s " + rearSeconds.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + ", sentry " + sentry.Current.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "/" + sentry.Maximum.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + ", hero alive " + hero.IsAlive + ", sentry alive " + sentry.IsAlive + ")");
            Assert.That(frontBlunted, Is.EqualTo(frontBefore), "none from the front while the mirror turned after the hero");
            Assert.That(reflections, Is.LessThanOrEqualTo(reflectionsBefore + 1),
                "nothing reflects from behind (one owed from the last front landing may still be paid)");
            Assert.That((frontOther, rearOther), Is.EqualTo((0, 0)), "every landing was the front share from the front or whole from behind");

            Assert.That(turns, Is.GreaterThan(60), "the mirror was watched turning for over a second");
            Assert.That(tooFast, Is.Zero, "it never turned faster than 45 degrees a second");
            Assert.That(atRate, Is.GreaterThan(30), "and it turned at its full rate after the hero, half a second and more");

            // The reflection: paid, and within its cap of the hero's maximum a second, its starting budget a second's worth.
            float share = MirrorGuardRule.ReflectShare, cap = MirrorGuardRule.ReflectCap;
            if (share > 0f && cap > 0f)
            {
                Assert.That(reflections, Is.GreaterThan(0), "the front landings reflected");
                Assert.That(reflected, Is.GreaterThan(0f));
            }
            else
            {
                // As it ships (sentry-r0-f70): the hero is never reflected anything; the mirror's glint still shows.
                Assert.That(reflections, Is.Zero, "a guard that reflects nothing pays nothing");
                Assert.That(setup.Mirrors.ReflectedTotal, Is.Zero, "no health reflected");
            }
            Assert.That(blunts, Is.GreaterThan(0), "a blunted landing flashed the mirror's violet glint");
            Assert.That(otherMirrors, Is.Zero, "from this Sentry's mirror");
            Assert.That(setup.Mirrors.BluntGlints, Is.EqualTo(blunts));
            Assert.That(tooSoon, Is.Zero, "never two glints within BluntGlintSeconds");
            Assert.That(blunts, Is.LessThanOrEqualTo(mirror.FrontLandings), "at most one a blunted landing");
            Assert.That(glints.GlintsBegun, Is.EqualTo(blunts + reflections), "every glint the view drew was a blunted landing's or a reflection's");
            Assert.That(reflected, Is.LessThanOrEqualTo(maximum * MirrorGuardRule.ReflectCap * (guardedSeconds + 1f) + 1e-3f),
                $"{reflected:0.00} reflected over {guardedSeconds:0.0} s guarding, the hero's maximum {maximum:0}");
            Assert.That(setup.Mirrors.ReflectedTotal, Is.EqualTo(reflected).Within(1e-4f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
