using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The gate bosses' barks (docs/44 section 1.8; docs/64 stage 1, 2026-11-23; BossBarkView): the Forge Warden and the Glass
    // Regent each say their spawn line as they walk out of the gate, their telegraph line with the first mark of phase one
    // and again with the first of phase two - never more - and their death line as they fall; a bark shows for 1.5-2 s; its
    // plate never stands over the mark that is down (the Warden's circle, the Regent's lanes); and every line fits its plate
    // in Turkish. Visual only: the mark, the hold and the blow are the slam service's.
    public sealed class BossBarkFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private WardenSlamService _service;
        private BossBarkView _bark;
        private ArenaView _arena;
        private Language? _before;
        // Frames on which a shown plate stood over the mark down (with where), for the checks.
        private readonly List<string> _overMark = new List<string>();

        [UnitySetUp] public IEnumerator Begin()
        {
            _before = Loc.Override;
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown] public IEnumerator End()
        {
            Loc.Override = _before;
            Time.timeScale = 1;
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Boss bark cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The gate's boss alone (the Warden's proof floor, or the Regent's), the hero standing and striking nothing. Returns
        // once the bark view is found; the boss may still be walking out.
        private IEnumerator Load(string floor)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, floor);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _service = _setup.Warden;
            _arena = Object.FindFirstObjectByType<ArenaView>();
            _bark = _setup.GetComponent<BossBarkView>();
            Assert.That(_bark, Is.Not.Null, "The bark view is set up with the scene.");
        }

        // The hero standing through beat after beat: healed between them, so no blow ends the run under the test.
        private static void Heal()
        {
            Health hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            if (hero != null && hero.IsAlive)
                hero.Heal(hero.Maximum);
        }

        private void ResolveUpgrades()
        {
            for (int i = 0; i < 8 && _setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
        }

        // Frames until the condition holds or seconds of real time pass, each frame's plate checked against the mark.
        private IEnumerator Until(System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                ResolveUpgrades();
                yield return null;
                CheckPlate();
            }
        }

        private IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
                CheckPlate();
            }
        }

        // A shown plate over a mark that is down, judged on the screen's own corners against the service's mark (as
        // WardenSlamView draws it: the circle at the mark, or the lanes of the beat).
        private void CheckPlate()
        {
            if (_bark.ShownAlpha <= 0f || !_service.IsFighting || !_service.Clock.IsMarked)
                return;
            if (!_bark.TryPlateWorldRect(out float left, out float right, out float bottom, out float top))
                return;
            var p = new Vector3(_service.MarkX, ArenaFloor.WorldY(_service.MarkY), 0f);
            float lift = _arena != null && _arena.IsFoundryRegion ? FoundrySurface.Lift(p, _arena.transform.position) : 0f;
            bool clear;
            if (_service.MarksLanes)
            {
                clear = true;
                RegentLanes lanes = _service.Lanes;
                for (int i = 0; i < lanes.Count; i++)
                {
                    lanes.Direction(i, out float dx, out float dy);
                    clear &= BossBark.ClearOfLane(left, right, bottom, top, lift, lanes.OriginX(i), lanes.OriginY(i), dx, dy,
                        lanes.LengthOf(i), lanes.Width * .5f - .01f);
                }
            }
            else
                clear = BossBark.ClearOfCircle(left, right, bottom, top, lift, _service.MarkX, _service.MarkY,
                    WardenRule.SlamRadius - .01f);
            if (!clear)
                _overMark.Add($"[{left:0.00}, {right:0.00}] x [{bottom:0.00}, {top:0.00}] over the mark at ({_service.MarkX:0.00}, {_service.MarkY:0.00})");
        }

        // How long the bark now showing shows, in game seconds (60 frames a second).
        private IEnumerator MeasureBark(float[] seconds)
        {
            int frames = 0;
            while (_bark.IsBarking && frames < 600)
            {
                yield return null;
                CheckPlate();
                frames++;
            }
            seconds[0] = frames / 60f;
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheWardenBarksAsItWalksOutAtEachPhasesFirstMarkAndAsItFalls()
        {
            yield return Load("floor_gate_boss_proof");
            yield return Until(() => _bark.SpawnsSaid > 0, 30f);
            Assert.That(_bark.SpawnsSaid, Is.EqualTo(1), "It speaks as it walks out of the gate.");
            Assert.That(_bark.LastMoment, Is.EqualTo(BarkMoment.Spawn));
            Assert.That(_bark.LastLine, Is.EqualTo("Shift's not over."));
            Assert.That(_bark.IsBarking, Is.True);
            yield return Frames(15);
            Assert.That(_bark.ShownAlpha, Is.GreaterThan(.9f), "Over the boss at full once faded in.");
            Assert.That(_bark.UsesPaintedPlate, Is.True, "On the HUD's smoked plate.");
            var shown = new float[1];
            yield return MeasureBark(shown);
            Assert.That(shown[0] + .25f, Is.InRange(1.5f, 2.05f), "A bark shows 1.5-2 s.");
            Assert.That(_bark.ShownAlpha, Is.Zero, "Then it is gone.");

            yield return Until(() => _service.IsFighting && _service.Clock.IsMarked, WardenRule.SlamEvery + 10f);
            Assert.That(_service.Clock.IsMarked, Is.True, "A mark goes down.");
            Assert.That(_bark.TelegraphBarks, Is.EqualTo(1), "Its first mark says the telegraph line.");
            Assert.That(_bark.LastLine, Is.EqualTo("Stand clear of the mark."));
            yield return MeasureBark(shown);
            Assert.That(shown[0], Is.InRange(1.4f, 2.05f));
            // Phase one's next mark says nothing.
            yield return Until(() => !_service.Clock.IsMarked, 5f);
            yield return Until(() => _service.Clock.IsMarked, WardenRule.SlamEvery + 5f);
            Assert.That(_service.Clock.IsMarked, Is.True, "A second mark goes down.");
            Assert.That(_bark.TelegraphBarks, Is.EqualTo(1), "The telegraph line at most once a phase.");
            yield return Until(() => !_service.Clock.IsMarked, 5f);

            // Phase two: its first mark says it again, its next does not.
            Heal();
            Health boss = _service.Boss;
            boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
            yield return Until(() => _service.IsEnraged, 2f);
            Assert.That(_service.IsEnraged, Is.True);
            yield return Until(() => _service.Clock.IsMarked, WardenRule.SlamEveryEnraged + 5f);
            Assert.That(_bark.TelegraphBarks, Is.EqualTo(2), "Phase two's first mark says it once more.");
            Assert.That(_bark.TelegraphsSaid, Is.EqualTo(2));
            yield return Until(() => !_service.Clock.IsMarked, 5f);
            Heal();
            yield return Until(() => _service.Clock.IsMarked, WardenRule.SlamEveryEnraged + 5f);
            Assert.That(_bark.TelegraphBarks, Is.EqualTo(2), "Not with phase two's next mark.");

            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            yield return null;
            Assert.That(_bark.DeathsSaid, Is.EqualTo(1), "It speaks as it falls.");
            Assert.That(_bark.LastLine, Is.EqualTo("Door... was never locked for you."));
            yield return Frames(15);
            Assert.That(_bark.ShownAlpha, Is.GreaterThan(.9f), "Where it fell.");
            Assert.That(_overMark, Is.Empty, string.Join("\n", _overMark));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(180000)]
        public IEnumerator TheRegentSaysItsOwnLinesAndItsPlateKeepsOffItsLanes()
        {
            yield return Load("floor_regent_proof");
            yield return Until(() => _bark.SpawnsSaid > 0, 30f);
            Assert.That(_bark.LastLine, Is.EqualTo("Which one of you is original?"));
            yield return Until(() => _service.IsFighting && _service.Clock.IsMarked, RegentRule.Every + 10f);
            Assert.That(_service.MarksLanes, Is.True, "The Regent's lane is down.");
            Assert.That(_bark.LastLine, Is.EqualTo("Hold still. I need your face."));
            // Beat after beat, the Regent walking up to the standing hero: a plate shown through each lane, to be judged
            // against it (the line again, laid and placed as a bark is).
            for (int beat = 0; beat < 3; beat++)
            {
                yield return Until(() => !_service.Clock.IsMarked, 5f);
                yield return Until(() => _service.Clock.IsMarked, RegentRule.Every + 5f);
                Assert.That(_service.Clock.IsMarked, Is.True, "Beat " + beat);
                _bark.SayForCheck(BarkMoment.Telegraph);
                yield return Until(() => !_service.Clock.IsMarked, 5f);
                Heal();
            }
            Assert.That(_bark.TelegraphBarks, Is.EqualTo(1), "Its own telegraph bark once in phase one.");
            Health boss = _service.Boss;
            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            yield return null;
            Assert.That(_bark.LastLine, Is.EqualTo("I kept the wrong one..."));
            Assert.That(_overMark, Is.Empty, string.Join("\n", _overMark));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EveryLineFitsItsPlateInTurkish()
        {
            Loc.Override = Language.Turkish;
            yield return Load("floor_gate_boss_proof");
            yield return Until(() => _bark.SpawnsSaid > 0, 30f);
            Assert.That(_bark.LastLine, Is.EqualTo("Vardiya bitmedi."), "The Warden speaks Turkish.");
            var problems = new List<string>();
            float canvasWidth = ((RectTransform)_bark.Plate.parent).rect.width;
            foreach (LocText line in BossBark.Lines)
            {
                _bark.ShowForCheck(line);
                yield return null;
                Assert.That(_bark.Label.text, Is.Not.EqualTo(line.English), line.Key + " shows its Turkish.");
                Assert.That(_bark.Label.text, Is.EqualTo(Loc.Text(line.Key)), line.Key);
                TurkishScreensTests.Check(_bark.Plate, line.Key, problems);
                Assert.That(_bark.Plate.rect.width, Is.LessThanOrEqualTo(BossBarkView.MaxWidth + .5f), line.Key + " within the plate's widest.");
                Assert.That(_bark.Plate.rect.width, Is.LessThan(canvasWidth * .7f), line.Key + " leaves the screen's sides.");
                Assert.That(_bark.Plate.rect.height, Is.LessThan(150f), line.Key + " at most two lines.");
            }
            Assert.That(problems, Is.Empty, string.Join("\n", problems));
            TurkishScreensTests.AssertNoKeyShows();
            // The development proof floor's room has no Turkish row on purpose (docs/60: development copies fall back to English),
            // so its fallback warning is expected here; the barks' own words are checked above.
            // LogAssert.NoUnexpectedReceived() is left out for this proof floor.
        }
    }
}
