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
    // The game-feel pass, stage 4 (docs/43 section 1, 2026-11-22), on the foundry's gate boss (BossFeelView): the body
    // tells each blow - it rises, leans back and glows - from the very frame its mark goes down to the frame the blow
    // lands, on the clock's own progress, then kicks and settles back; phase two bursts and the glow runs hotter without
    // touching the body's colours; the Warden's fall opens in embers and ends in ash, the Regent shatters; the Regent's
    // violet feet ring reads on the floor over its shade. Visual only: the mark, the hold and the blow are the service's.
    public sealed class BossFeelTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private WardenSlamService _service;
        private BossFeelView _feel;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Boss feel cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The gate's boss alone: the Warden's proof floor, or the Regent's. Nothing attacks; the hero stands.
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
            _feel = _setup.GetComponent<BossFeelView>();
            Assert.That(_feel, Is.Not.Null, "The gate boss's feel view is set up with the scene.");
            yield return Until(() => _service.IsFighting && !_setup.Encounters.IsRevealHeld, 30f);
            Assert.That(_service.IsFighting, Is.True, "The gate's boss is fought.");
        }

        private void ResolveUpgrades()
        {
            for (int i = 0; i < 8 && _setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
        }

        private IEnumerator Until(System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                ResolveUpgrades();
                yield return null;
            }
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheWardensBodyTellsTheBlowFromItsMarkToItsSlam()
        {
            yield return Load("floor_gate_boss_proof");
            Health boss = _service.Boss;
            var look = boss.GetComponent<EnemyLookView>();
            Transform body = look.Body.transform;
            Vector3 rest = look.BodyRest;
            int renderers = _feel.Root.GetComponentsInChildren<Renderer>(true).Length;
            bool tellAtMark = false, tellAtSlam = true;
            bool? hit = null;
            float markX = 0f;
            System.Action marked = () =>
            {
                tellAtMark = _feel.IsTelling && _feel.TellProgress == 0f;
                markX = _service.MarkX;
            };
            System.Action<bool> slammed = h =>
            {
                hit = h;
                tellAtSlam = _feel.IsTelling;
            };
            _service.Marked += marked;
            _service.Slammed += slammed;
            yield return Until(() => _service.Clock.IsMarked, WardenRule.SlamEvery + 5f);
            Assert.That(_service.Clock.IsMarked, Is.True, "A mark goes down.");
            Assert.That(tellAtMark, Is.True, "The body's tell begins with the mark, on its frame.");
            float side = markX > boss.transform.position.x ? 1f : -1f;
            bool rose = false, leaned = false, glowed = false;
            while (_service.Clock.IsMarked && hit == null)
            {
                Assert.That(_feel.IsTelling, Is.True, "Told all through the telegraph.");
                Assert.That(_feel.TellProgress, Is.EqualTo(_service.Clock.TelegraphProgress), "On the clock's own progress.");
                yield return null;
                if (!_service.Clock.IsMarked)
                    break;
                if (_feel.TellProgress > .3f && look.IsWindingUp)
                {
                    rose |= body.localPosition.y > rest.y + .03f;
                    float lean = Mathf.DeltaAngle(0f, body.localEulerAngles.z);
                    leaned |= side * lean > 2f;
                }
                glowed |= _feel.GlowAlpha > 0f;
                Assert.That(_feel.GlowAlpha, Is.LessThanOrEqualTo(GameFeel.TellGlowPeak + 1e-4f), "Never brighter than the mark.");
            }
            yield return Until(() => hit != null, WardenRule.Telegraph + 2f);
            Assert.That(hit, Is.Not.Null, "The blow lands.");
            Assert.That(tellAtSlam, Is.False, "The tell ends on the blow's frame.");
            Assert.That(rose, Is.True, "It rose through the telegraph.");
            Assert.That(leaned, Is.True, "Leaning back, away from the mark.");
            Assert.That(glowed, Is.True, "Glowing.");
            Assert.That(_feel.IsKicking, Is.True, "The blow kicks the body.");
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.That(_feel.IsKicking, Is.False);
            Assert.That(body.localPosition, Is.EqualTo(rest), "Back at the look's rest.");
            Assert.That(body.localRotation, Is.EqualTo(Quaternion.identity), "Upright.");
            Color colour = look.Body.color;
            Assert.That(new Vector3(colour.r, colour.g, colour.b), Is.EqualTo(Vector3.one), "Its own colours, never tinted.");
            Assert.That(_feel.Root.GetComponentsInChildren<Renderer>(true).Length + 1, Is.GreaterThanOrEqualTo(renderers),
                "Nothing made (the glow rides under the boss while it is fought).");
            _service.Marked -= marked;
            _service.Slammed -= slammed;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator PhaseTwoBurstsAndTheGlowRunsHotterThenTheWardensFallEndsInAsh()
        {
            yield return Load("floor_gate_boss_proof");
            Health boss = _service.Boss;
            var look = boss.GetComponent<EnemyLookView>();
            Assert.That(_feel.ShowsPhaseTwo, Is.False);
            boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
            yield return null;
            yield return null;
            Assert.That(_service.IsEnraged, Is.True);
            Assert.That(_feel.ShowsPhaseTwo, Is.True, "Phase two shows.");
            Assert.That(_feel.PhaseTwoBursts, Is.EqualTo(1), "Once.");
            Assert.That(_feel.ShownRings, Is.GreaterThanOrEqualTo(1), "Its rings over the floor.");
            Assert.That(_feel.ShownParticles, Is.GreaterThanOrEqualTo(10), "Its embers.");
            Assert.That(_feel.GlowAlpha, Is.GreaterThan(0f), "The glow runs.");
            Color colour = look.Body.color;
            Assert.That(new Vector3(colour.r, colour.g, colour.b), Is.EqualTo(Vector3.one), "The tint is the glow's, not the body's.");
            for (int i = 0; i < 60; i++)
                yield return null;
            Assert.That(_feel.ShownRings, Is.Zero);
            Assert.That(_feel.GlowAlpha, Is.GreaterThan(0f), "Phase two's slow glow stays.");

            int renderers = _feel.Root.GetComponentsInChildren<Renderer>(true).Length;
            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            Assert.That(_feel.DeathsShown, Is.EqualTo(1));
            Assert.That(_feel.LastDeathShattered, Is.False, "The Warden falls in embers.");
            Assert.That(_feel.ShownParticles, Is.GreaterThanOrEqualTo(15));
            Assert.That(_feel.Glow.transform.parent, Is.SameAs(_feel.Root), "Its glow taken back from the fallen body.");
            Assert.That(_feel.Glow.enabled, Is.False);
            yield return Until(() => _feel.DeathEndsShown > 0, 5f);
            Assert.That(_feel.DeathEndsShown, Is.EqualTo(1), "Its remains dissolve in ash as its fall ends.");
            for (int i = 0; i < 120; i++)
                yield return null;
            Assert.That(_feel.ShownParticles, Is.Zero);
            Assert.That(_feel.ShownRings, Is.Zero);
            foreach (Renderer renderer in _feel.Root.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.enabled, Is.False, renderer.name);
            Assert.That(_feel.Root.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(renderers + 1),
                "Pooled: the glow came back, nothing else was made.");

            // The restart: one view and one root a scene.
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Assert.That(Object.FindObjectsByType<BossFeelView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            int roots = 0;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
                if (t.name == BossFeelView.RootName)
                    roots++;
            Assert.That(roots, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheRegentsCastReadsOnItsBodyAndItShattersAsItFalls()
        {
            yield return Load("floor_regent_proof");
            Health boss = _service.Boss;
            Assert.That(_service.MarksLanes, Is.True, "The Regent is fought.");
            bool tellAtMark = false;
            System.Action marked = () => tellAtMark = _feel.IsTelling;
            _service.Marked += marked;
            yield return Until(() => _service.Clock.IsMarked, RegentRule.Every + 5f);
            _service.Marked -= marked;
            Assert.That(tellAtMark, Is.True, "Its cast is told from the lane's frame.");
            float glow = 0f;
            Color shown = Color.clear;
            for (int i = 0; i < 20 && _service.Clock.IsMarked; i++)
            {
                yield return null;
                if (_feel.GlowAlpha > glow)
                {
                    glow = _feel.GlowAlpha;
                    shown = _feel.Glow.color;
                }
            }
            Assert.That(glow, Is.GreaterThan(0f), "The cast glows on its body.");
            Assert.That(shown.b, Is.GreaterThan(shown.r), "In glass violet, not the Warden's ember.");

            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            Assert.That(_feel.LastDeathShattered, Is.True, "The Regent shatters.");
            yield return null;
            int turned = 0;
            foreach (SpriteRenderer shard in _feel.Root.GetComponentsInChildren<SpriteRenderer>())
                if (shard.enabled && shard.sprite == FeelArt.Disc() && shard.transform.rotation != Quaternion.identity)
                    turned++;
            Assert.That(turned, Is.GreaterThanOrEqualTo(10), "Shards flung out, turned along their flight.");
            yield return Until(() => _feel.DeathEndsShown > 0, 5f);
            Assert.That(_feel.DeathEndsShown, Is.EqualTo(1), "Its remains end in glints.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheRegentsFeetRingReadsOnTheFloor()
        {
            yield return Load("floor_regent_proof");
            var view = _setup.GetComponent<WardenSlamView>();
            yield return null;
            Assert.That(view.IsFeetShown, Is.True, "The ring shows round its feet while it is fought.");
            LineRenderer ring = view.FeetRingLine, shade = view.FeetShadeLine;
            Assert.That(shade, Is.Not.Null);
            Assert.That(shade.gameObject.activeInHierarchy, Is.True, "Its shade under it.");
            if (!view.IsFeetPulsing)
            {
                Assert.That(ring.startColor.a, Is.GreaterThanOrEqualTo(WardenSlamView.FeetRest - 1e-4f), "Strong enough to read.");
                Assert.That(ring.startWidth, Is.GreaterThanOrEqualTo(WardenSlamView.FeetWidth - 1e-4f), "Wide enough to read.");
            }
            Assert.That(shade.startWidth, Is.GreaterThan(ring.startWidth), "The shade frames it.");
            Assert.That(shade.sortingOrder, Is.LessThan(ring.sortingOrder), "Under the ring.");
            Assert.That(ring.sortingOrder, Is.LessThan(GroundedSorting.ActorOrder), "Both under every actor.");
            Color violet = ring.startColor;
            Assert.That(violet.b, Is.GreaterThan(violet.g), "Still violet: it never hurts by itself.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
