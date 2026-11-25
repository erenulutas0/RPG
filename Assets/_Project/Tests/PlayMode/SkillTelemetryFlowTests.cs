using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Cryptforge.Analytics;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // docs/57 section 5.3's skills telemetry in the Gameplay scene, written to the test profile's telemetry folder and read
    // back as the phone's file would be: a tap on a cooling or held button is ability_refused; a press that began on a
    // button and dragged past the stick's dead zone is stick_on_button and walks nothing; a Burst, a Vent or a Rush with
    // nothing in its own reach, and a glyph that held nothing for its whole life, are ability_wasted; each slot's use is
    // ability_used under its own id and slot, run_start names the skills carried and run_end counts each slot's presses.
    // The fight is placed by hand where it matters (the encounter and every attack off, the hero's targets set), as
    // HeroSkillsFlowTests places it; the buttons are tapped as the EventSystem delivers a tap.
    public sealed class SkillTelemetryFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private const float Frame = 1f / 60;
        private CombatSetup _setup;
        private Health _hero;
        private AbilityController _ability;
        private AbilityButtonView _first;
        private HeroMovementInput _movement;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Skill telemetry cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // skills: development/skills.txt ("3" arms skills 2 and 3), or null for skill 1 alone.
        private IEnumerator Load(string heroId, string skills)
        {
            TestProfile.Begin();
            if (heroId != null)
                File.WriteAllText(DevelopmentStart.HeroPath, heroId);
            if (skills != null)
            {
                File.WriteAllText(DevelopmentStart.SkillsPath, skills);
                // The development file announces the skills it forces on (CombatSetup); that line is expected here.
                LogAssert.Expect(LogType.Log, new Regex(@"^\[Skills\] the run carries"));
            }
            Time.timeScale = 1;
            Time.captureDeltaTime = Frame;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            // Views bind in Start; slot 1 makes the skills' buttons in its first Update, and they start a frame later.
            yield return null;
            yield return null;
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _ability = _hero.GetComponent<AbilityController>();
            _movement = _hero.GetComponent<HeroMovementInput>();
            _first = GameObject.Find("Ability Button").GetComponent<AbilityButtonView>();
        }

        // The encounter and every attack off, and the hero's targets these alone.
        private void PlaceFight(params Health[] targets)
        {
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (AttackController attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            _ability.GetComponent<Targeting>().SetCandidates(targets);
        }

        private Health Dummy(float floorX, float floorY)
        {
            var dummy = new GameObject("Skill telemetry dummy").AddComponent<Health>();
            dummy.Initialize(1000);
            dummy.transform.position = _hero.transform.position + new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);
            return dummy;
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        // What the session has buffered, written, then the folder's lines (TelemetryFlowTests' reader).
        private static List<TelemetryFlowTests.Line> Log()
        {
            Assert.That(TelemetryRuntime.SessionFor(TelemetryLocation.Resolve()).Flush(), Is.True);
            return TelemetryFlowTests.ReadLog();
        }

        private static List<TelemetryFlowTests.Line> All(List<TelemetryFlowTests.Line> log, string name)
        {
            var found = new List<TelemetryFlowTests.Line>();
            foreach (TelemetryFlowTests.Line line in log)
            {
                if (line.Event == name)
                    found.Add(line);
            }
            return found;
        }

        private static string Describe(List<TelemetryFlowTests.Line> log)
        {
            var text = new System.Text.StringBuilder("Log:");
            foreach (TelemetryFlowTests.Line line in log)
                text.Append('\n').Append(line.Raw);
            return text.ToString();
        }

        // A tap on the Burst's button while it cools down fires nothing and writes ability_refused with the seconds left; one
        // while the fight is held writes it as held. Neither is a use.
        [UnityTest]
        public IEnumerator APressOnCooldownWritesAbilityRefused()
        {
            yield return Load(null, null);
            PlaceFight(Dummy(1f, 0f));
            Assert.That(_first.Button.interactable, Is.True);
            Tap(_first.Button);
            Assert.That(_ability.Ability.IsReady, Is.False, "the Burst fired");
            yield return null;
            Assert.That(_first.Button.interactable, Is.False, "greyed while it cools down");
            float left = _ability.Ability.Remaining;
            Tap(_first.Button);
            Assert.That(_ability.UseCount, Is.EqualTo(1), "the tap on the greyed button fired nothing");

            // The fight held, as the boss's reveal holds it (EncounterController.FightHold), greys every button.
            yield return Frames(2);
            System.Func<bool> hold = _ability.FightHold;
            _ability.FightHold = () => true;
            yield return null;
            Assert.That(_first.Button.interactable, Is.False);
            Tap(_first.Button);
            _ability.FightHold = hold;

            List<TelemetryFlowTests.Line> log = Log();
            List<TelemetryFlowTests.Line> used = All(log, "ability_used");
            Assert.That(used.Count, Is.EqualTo(1), Describe(log));
            Assert.That((used[0].Number("slot"), used[0].Text("ability_id")), Is.EqualTo((1.0, "ability_forge_burst")));
            List<TelemetryFlowTests.Line> refused = All(log, "ability_refused");
            Assert.That(refused.Count, Is.EqualTo(2), Describe(log));
            Assert.That((refused[0].Number("slot"), refused[0].Text("ability_id"), refused[0].Text("reason")),
                Is.EqualTo((1.0, "ability_forge_burst", "cooling")));
            Assert.That(refused[0].Number("cooldown_sec"), Is.EqualTo(left).Within(1e-3), "how early the press came");
            Assert.That(refused[0].Number("cooldown_sec"), Is.GreaterThan(7.0).And.LessThan(8.0));
            Assert.That(refused[1].Text("reason"), Is.EqualTo("held"), "the boss's reveal greys every button");
            Assert.That(All(log, "ability_wasted"), Is.Empty, "the Burst struck the dummy beside the hero");
            LogAssert.NoUnexpectedReceived();
        }

        // A press that lands on the Burst's button and drags is the button's (the EventSystem routes the drag to it), never
        // the stick's: the hero stays put. Past the stick's dead zone it writes stick_on_button, once a press; short of it,
        // nothing.
        [UnityTest]
        public IEnumerator ADragStartingOnAButtonWritesStickOnButtonAndDoesNotMoveTheHero()
        {
            yield return Load(null, null);
            PlaceFight();
            Canvas.ForceUpdateCanvases();
            var rect = (RectTransform)_first.Button.transform;
            Canvas root = rect.GetComponentInParent<Canvas>().rootCanvas;
            Camera camera = root.renderMode == RenderMode.ScreenSpaceOverlay ? null : root.worldCamera;
            Vector2 centre = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
            var pointer = new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left, pointerId = 0, position = centre, pressPosition = centre
            };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            Assert.That(hits.Count, Is.GreaterThan(0), "a touch there lands on the UI, which the stick never takes");
            GameObject dragged = ExecuteEvents.GetEventHandler<IDragHandler>(hits[0].gameObject);
            Assert.That(dragged, Is.SameAs(_first.gameObject), "the drag is the button's");
            GameObject pressed = ExecuteEvents.ExecuteHierarchy(hits[0].gameObject, pointer, ExecuteEvents.pointerDownHandler);

            Vector3 before = _hero.transform.position;
            float deadZone = _movement.DeadZonePixels;
            Assert.That(deadZone, Is.GreaterThan(0f));
            pointer.position = centre + new Vector2(deadZone * .6f, 0f);
            pointer.dragging = true;
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.dragHandler);
            Assert.That(All(Log(), "stick_on_button"), Is.Empty, "short of the dead zone: nothing the stick would have walked");
            pointer.position = centre + new Vector2(-deadZone * 2f, deadZone);
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.dragHandler);
            yield return null;
            pointer.position = centre + new Vector2(-deadZone * 3f, deadZone);
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.dragHandler);
            yield return Frames(10);
            Assert.That(_movement.IsDragging, Is.False, "the stick never took it");
            Assert.That(_movement.Steer, Is.EqualTo(Vector2.zero));
            Assert.That(_hero.transform.position, Is.EqualTo(before), "the hero did not move");
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.endDragHandler);
            ExecuteEvents.Execute(pressed, pointer, ExecuteEvents.pointerUpHandler);

            List<TelemetryFlowTests.Line> log = Log();
            List<TelemetryFlowTests.Line> drags = All(log, "stick_on_button");
            Assert.That(drags.Count, Is.EqualTo(1), "once a press. " + Describe(log));
            Assert.That((drags[0].Number("slot"), drags[0].Text("ability_id"), drags[0].Flag("stick_held")),
                Is.EqualTo((1.0, "ability_forge_burst", false)), "one thumb on the button, none on the stick");
            Assert.That(All(log, "ability_used"), Is.Empty, "a drag released off the button fires nothing");

            // The next press that drags is reported in its turn.
            pointer.position = centre;
            pointer.pressPosition = centre;
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.beginDragHandler);
            pointer.position = centre + new Vector2(0f, deadZone * 1.5f);
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(dragged, pointer, ExecuteEvents.endDragHandler);
            Assert.That(All(Log(), "stick_on_button").Count, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        // The Vanguard's Burst with no enemy within its 2.5 and its Rush with nothing on its path are wasted; a Burst that
        // strikes and Iron Resolve (which reaches nothing by design) are not.
        [UnityTest]
        public IEnumerator ABurstWithNoEnemyNearWritesAbilityWasted()
        {
            yield return Load(null, "3");
            Health far = Dummy(4f, 0f);
            PlaceFight(far);
            Assert.That(_ability.Ability.Kind, Is.EqualTo(AbilityKind.Burst));
            Tap(_first.Button);
            Assert.That(far.Current, Is.EqualTo(1000f), "4 from the hero: out of the Burst's reach");
            Assert.That(_ability.TryUse(3, Vector2.left), Is.True, "the Rush, away from it");
            Assert.That(_ability.TryUse(2), Is.True, "Iron Resolve");
            yield return Frames(30);

            // Its cooldown run down, the Burst again with an enemy beside the hero: struck, not wasted.
            Health near = Dummy(1f, 0f);
            _ability.GetComponent<Targeting>().SetCandidates(new[] { near });
            Time.captureDeltaTime = .25f;
            while (!_ability.Ability.IsReady)
                yield return null;
            Time.captureDeltaTime = Frame;
            yield return null;
            Tap(_first.Button);
            Assert.That(near.Current, Is.LessThan(1000f));

            List<TelemetryFlowTests.Line> log = Log();
            var used = new List<(double, string)>();
            foreach (TelemetryFlowTests.Line line in All(log, "ability_used"))
                used.Add((line.Number("slot"), line.Text("ability_id")));
            Assert.That(used, Is.EqualTo(new[]
            {
                (1.0, "ability_forge_burst"), (3.0, "ability_shield_rush"), (2.0, "ability_iron_resolve"), (1.0, "ability_forge_burst")
            }), Describe(log));
            List<TelemetryFlowTests.Line> wasted = All(log, "ability_wasted");
            Assert.That(wasted.Count, Is.EqualTo(2), Describe(log));
            Assert.That((wasted[0].Number("slot"), wasted[0].Text("ability_id")), Is.EqualTo((1.0, "ability_forge_burst")));
            Assert.That((wasted[1].Number("slot"), wasted[1].Text("ability_id")), Is.EqualTo((3.0, "ability_shield_rush")));
            Assert.That(wasted[0].Number("hero_max_hp"), Is.EqualTo(_hero.Maximum).Within(1e-3));
            Assert.That(log.IndexOf(wasted[0]), Is.EqualTo(log.IndexOf(All(log, "ability_used")[0]) + 1), "right after its use");
            LogAssert.NoUnexpectedReceived();
        }

        // Ilya's Vent with nothing burning in its reach is wasted at the press; her glyph is judged over its whole life - no
        // enemy inside for its 5 s writes ability_wasted as it goes out, not at the press; the ward reaches nothing by design.
        [UnityTest]
        public IEnumerator AVentWithNothingBurningAndAnEmptyGlyphAreWasted()
        {
            yield return Load(ArcanistId, "3");
            Health beside = Dummy(1f, 0f);
            PlaceFight(beside);
            Assert.That(_ability.Ability.Kind, Is.EqualTo(AbilityKind.Vent));
            Tap(_first.Button);
            Assert.That(_ability.Ability.IsReady, Is.False, "the Vent is spent, burning or not");
            Assert.That(All(Log(), "ability_wasted").Count, Is.EqualTo(1), "an unburnt enemy beside Ilya: nothing to erupt");

            _ability.GetComponent<Targeting>().SetCandidates(new Health[0]);
            Assert.That(_ability.TryUse(2), Is.True, "the glyph, with no enemy anywhere");
            Assert.That(_ability.TryUse(3), Is.True, "the ward");
            yield return null;
            Assert.That(All(Log(), "ability_wasted").Count, Is.EqualTo(1), "not at the press: the crowd may yet walk in");
            while (_ability.Sigil.IsLive)
                yield return null;
            yield return null;

            List<TelemetryFlowTests.Line> log = Log();
            List<TelemetryFlowTests.Line> wasted = All(log, "ability_wasted");
            Assert.That(wasted.Count, Is.EqualTo(2), Describe(log));
            Assert.That((wasted[0].Number("slot"), wasted[0].Text("ability_id")), Is.EqualTo((1.0, "ability_crucible_vent")));
            Assert.That((wasted[1].Number("slot"), wasted[1].Text("ability_id")), Is.EqualTo((2.0, "ability_flame_sigil")),
                "as it went out, nothing having stood in it");
            LogAssert.NoUnexpectedReceived();
        }

        // A glyph something stood in is not wasted.
        [UnityTest]
        public IEnumerator AGlyphAnEnemyStoodInIsNotWasted()
        {
            yield return Load(ArcanistId, "3");
            PlaceFight(Dummy(1f, 0f));
            Assert.That(_ability.TryUse(2), Is.True);
            while (_ability.Sigil.IsLive)
                yield return null;
            yield return null;
            List<TelemetryFlowTests.Line> log = Log();
            Assert.That(All(log, "ability_used").Count, Is.EqualTo(1), Describe(log));
            Assert.That(All(log, "ability_wasted"), Is.Empty, Describe(log));
            LogAssert.NoUnexpectedReceived();
        }

        // run_start names the skills the run carries; each tap on a live button is ability_used under its slot; a refused tap
        // is not a use; run_end's per-slot counts are the presses.
        [UnityTest]
        public IEnumerator RunEndsPerSlotCountsMatchThePresses()
        {
            yield return Load(null, "3");
            PlaceFight(Dummy(1f, 0f));
            AbilityButtonView second = _first.SkillButton(2);
            AbilityButtonView third = _first.SkillButton(3);
            Assert.That(second.State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Assert.That(third.State, Is.EqualTo(AbilityButtonView.SlotState.Live));
            Tap(_first.Button);
            Tap(second.Button);
            yield return null;
            Tap(second.Button);
            Tap(third.Button);
            yield return Frames(30);
            _hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            yield return null;
            Assert.That(_setup.Run.HasEnded, Is.True);

            List<TelemetryFlowTests.Line> log = Log();
            TelemetryFlowTests.Line start = All(log, "run_start")[0];
            Assert.That((start.Text("ability_id"), start.Text("skill_2_id"), start.Text("skill_3_id")),
                Is.EqualTo(("ability_forge_burst", "ability_iron_resolve", "ability_shield_rush")), Describe(log));
            var slots = new List<double>();
            foreach (TelemetryFlowTests.Line line in All(log, "ability_used"))
                slots.Add(line.Number("slot"));
            Assert.That(slots, Is.EqualTo(new[] { 1.0, 2.0, 3.0 }), Describe(log));
            List<TelemetryFlowTests.Line> refused = All(log, "ability_refused");
            Assert.That(refused.Count, Is.EqualTo(1), Describe(log));
            Assert.That((refused[0].Number("slot"), refused[0].Text("reason")), Is.EqualTo((2.0, "cooling")));
            List<TelemetryFlowTests.Line> ends = All(log, "run_end");
            Assert.That(ends.Count, Is.EqualTo(1), Describe(log));
            Assert.That((ends[0].Number("skill_1_uses"), ends[0].Number("skill_2_uses"), ends[0].Number("skill_3_uses")),
                Is.EqualTo((1.0, 1.0, 1.0)), "the presses, slot by slot");
            Assert.That(ends[0].Keys[ends[0].Keys.Count - 1], Is.EqualTo("skill_3_uses"), "last on the line");
            Assert.That(All(log, "session_start")[0].Number("schema"), Is.EqualTo(TelemetryJson.SchemaVersion));
            Assert.That(TelemetryJson.SchemaVersion, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
