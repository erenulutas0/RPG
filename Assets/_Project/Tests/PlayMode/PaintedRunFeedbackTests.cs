using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
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
    // The run's growth painted in the Gameplay scene (RunFeedbackArtSet, handed to the view by CombatSetup): a level-up
    // rings the hero's feet in painted light and sends gold sparks up, on scaled time, so it waits under the offer; a
    // taken card rings him in its tier's own colour after its bright core; an evolution doubles the ring with the ember
    // ring and lights him white; a weapon pops painted embers off his waist; and a view without the set, or with a broken
    // one, draws the lines, the painted ring standing exactly at the line's outer edge. RunFeedbackFlowTests covers the
    // view's timing and pools, unchanged by the paint.
    public sealed class PaintedRunFeedbackTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        // The painted sheet every frame is cut from.
        private const string SheetName = "VFX_RunFeedback";
        // RunFeedbackView's sparks draw over the combatants with the hit sparks, as its lines did.
        private const int SparkOrder = 6;
        private const int RingSlots = 8, SparkSlots = 32;
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private CombatSetup _setup;
        private RunFeedbackView _feedback;
        private Transform _root;
        private Transform _hero;
        private ArenaView _arena;

        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted run feedback cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The foundry, whose offers come by category, with the hero held still.
        private IEnumerator Load()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _feedback = Object.FindFirstObjectByType<RunFeedbackView>();
            _root = _feedback.transform.Find("Run Feedback");
            _hero = GameObject.Find("Vanguard").transform;
            _arena = Object.FindFirstObjectByType<ArenaView>();
            Assert.That(_root, Is.Not.Null);
            Assert.That(_feedback.UsesPaintedArt, Is.True, "The scene's CombatSetup hands the view its painted set.");
        }

        // Every painted frame on show under a view's root whose sprite is one of the named frames.
        private static List<SpriteRenderer> Shown(Transform root, string prefix)
        {
            var shown = new List<SpriteRenderer>();
            foreach (var sprite in root.GetComponentsInChildren<SpriteRenderer>())
                if (sprite.enabled && sprite.sprite != null && sprite.sprite.name.StartsWith(prefix)) shown.Add(sprite);
            return shown;
        }

        private List<SpriteRenderer> Shown(string prefix) => Shown(_root, prefix);

        private static int LinesShown(Transform root)
        {
            int shown = 0;
            foreach (var line in root.GetComponentsInChildren<LineRenderer>())
                if (line.enabled) shown++;
            return shown;
        }

        // The hero's feet as the view draws them: his root, raised on the foundry onto the surface under it.
        private Vector3 Feet()
        {
            Vector3 feet = _hero.position;
            feet.z = 0f;
            feet.y += FoundrySurface.Lift(feet, _arena.transform.position);
            return feet;
        }

        // Frames pass until the condition holds or the real time runs out, whatever the frame rate.
        private static IEnumerator Until(System.Func<bool> condition, float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < end)
                yield return null;
        }

        private static void AssertTint(SpriteRenderer sprite, Color colour, string because)
        {
            Assert.That(sprite.color.r, Is.EqualTo(colour.r).Within(1e-4f), because);
            Assert.That(sprite.color.g, Is.EqualTo(colour.g).Within(1e-4f), because);
            Assert.That(sprite.color.b, Is.EqualTo(colour.b).Within(1e-4f), because);
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ALevelUpRingsTheHerosFeetInPaintedLightAndWaitsUnderTheOffer()
        {
            yield return Load();
            Assert.That(_root.GetComponentsInChildren<LineRenderer>(true), Is.Empty, "Painted, the view makes no lines.");
            Assert.That(_root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(RingSlots + SparkSlots),
                "Its rings and sparks, pooled up front.");
            RunState run = _setup.Run;
            run.AddExperience(run.ExperienceForNextLevel - run.Experience);
            Assert.That(_feedback.ShownLevelUps, Is.EqualTo(1));
            List<SpriteRenderer> rings = Shown("VFX Level Ring ");
            Assert.That(rings.Count, Is.EqualTo(1), "A level rings the hero's feet in painted light.");
            SpriteRenderer ring = rings[0];
            Assert.That(ring.sprite.texture.name, Is.EqualTo(SheetName), "Cut from the painted sheet.");
            Assert.That(ring.sprite.name, Is.EqualTo("VFX Level Ring 0"), "From its first frame.");
            Assert.That(ring.sortingOrder, Is.EqualTo(_arena.GroundEffectSortingOrder), "On the floor's order, as the line.");
            Vector3 feet = Feet();
            Assert.That(ring.transform.position.x, Is.EqualTo(feet.x).Within(1e-3f), "At his feet.");
            Assert.That(ring.transform.position.y, Is.EqualTo(feet.y).Within(1e-3f), "Raised onto the surface he stands on.");
            Assert.That(ring.transform.localScale.x, Is.EqualTo(.2f + .5f * .1f).Within(1e-4f),
                "Out to the line's outer edge: its radius and half its width.");
            Assert.That(ring.transform.localScale.y, Is.EqualTo(ring.transform.localScale.x), "Its painted depth kept.");
            Assert.That(ring.color, Is.EqualTo(Color.white), "In its own gold, at full strength.");
            List<SpriteRenderer> sparks = Shown("VFX Gold Spark ");
            Assert.That(sparks.Count, Is.EqualTo(3), "Its first gold sparks rise; the rest follow in step.");
            foreach (SpriteRenderer spark in sparks)
            {
                Assert.That(spark.sprite.texture, Is.SameAs(ring.sprite.texture), "From the ring's sheet.");
                Assert.That(spark.sortingOrder, Is.EqualTo(SparkOrder), "Over the combatants.");
            }
            Assert.That(LinesShown(_root), Is.Zero);

            // The level opens an offer, and a second pick waiting holds the run after the first: the ring waits on scaled
            // time under the panel.
            run.GrantBonusUpgrade();
            yield return null;
            Assert.That(_setup.Pause.IsFrozen, Is.True);
            float held = ring.transform.localScale.x;
            Sprite heldFrame = ring.sprite;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(ring.enabled, Is.True, "Held under the panel.");
            Assert.That(ring.transform.localScale.x, Is.EqualTo(held).Within(1e-5f));
            Assert.That(ring.sprite, Is.SameAs(heldFrame));

            for (int i = 0; i < 4 && _setup.Choices.IsOpen; i++) _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            float spread = ring.transform.localScale.x;
            float risen = sparks[0].transform.position.y;
            yield return Until(() => !ReferenceEquals(ring.sprite, heldFrame), 1f);
            // A drop gathered meanwhile may have levelled the hero again and held the run on a new burst.
            if (_feedback.ShownLevelUps == 1 && !_setup.Choices.IsOpen)
            {
                Assert.That(ring.transform.localScale.x, Is.GreaterThan(spread), "It spreads once the run moves.");
                Assert.That(ring.sprite, Is.Not.SameAs(heldFrame), "Through its painted frames.");
                Assert.That(sparks[0].transform.position.y, Is.GreaterThan(risen), "Its sparks rise.");
                yield return Until(() => !_feedback.IsLevelUpShowing, 3f);
                if (_feedback.ShownLevelUps == 1 && !_setup.Choices.IsOpen)
                {
                    Assert.That(_feedback.IsLevelUpShowing, Is.False, "Its half second plays out.");
                    Assert.That(Shown("VFX Level Ring ").Count, Is.Zero, "And its painted frames with it.");
                }
            }
            for (int i = 0; i < 4 && _setup.Choices.IsOpen; i++) _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ATakenCardRingsTheHeroInItsTiersOwnColourAfterItsBrightCore()
        {
            yield return Load();
            UpgradeService upgrades = _setup.Upgrades;
            var seen = new bool[RarityTable.TierCount];
            int found = 0;
            for (int i = 0; i < 300 && found < seen.Length; i++)
            {
                if (upgrades.CurrentOffer == null) _setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer = upgrades.CurrentOffer;
                if (offer == null) continue;
                int slot = -1;
                for (int c = 0; c < offer.Choices.Count && slot < 0; c++)
                    if (!offer.Choices[c].IsEvolution && !seen[(int)offer.RarityAt(c)]) slot = c;
                if (slot < 0)
                {
                    Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, NotAnEvolution(offer)), Is.True);
                    continue;
                }

                // The last pick's flash plays out first, so this pick's frames are the only cards on show; a pick still
                // to come holds the run, so this one plays on unscaled time alone, as behind the next offer.
                yield return Until(() => !_feedback.IsCardFlashShowing, 3f);
                Assert.That(_feedback.IsCardFlashShowing, Is.False);
                UpgradeRarity rarity = offer.RarityAt(slot);
                Color colour = RunChoiceView.RarityColour(rarity);
                _setup.Run.GrantBonusUpgrade();
                Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, slot), Is.True);
                Assert.That(_feedback.LastFlashColour, Is.EqualTo(colour));
                List<SpriteRenderer> cores = Shown("VFX Card Core ");
                Assert.That(cores.Count, Is.EqualTo(1), $"{rarity}: its bright core first.");
                AssertTint(cores[0], Color.Lerp(colour, Color.white, .55f), $"{rarity}: the core in the tier's pale light.");
                Assert.That(cores[0].sortingOrder, Is.EqualTo(_arena.GroundEffectSortingOrder), $"{rarity}: on the floor.");
                Assert.That(Shown("VFX Card Ring ").Count, Is.Zero, $"{rarity}: its ring follows the core.");

                yield return Until(() => Shown("VFX Card Ring ").Count > 0, 1f);
                List<SpriteRenderer> rings = Shown("VFX Card Ring ");
                Assert.That(rings.Count, Is.EqualTo(1), $"{rarity}: then its ring.");
                SpriteRenderer ring = rings[0];
                Assert.That(ring.sprite.texture.name, Is.EqualTo(SheetName), $"{rarity}: cut from the painted sheet.");
                Assert.That(ring.sprite.texture, Is.SameAs(cores[0].sprite.texture), $"{rarity}: the core's sheet, so they batch.");
                AssertTint(ring, colour, $"{rarity}: in the tier's own colour.");
                Assert.That(ring.sortingOrder, Is.EqualTo(_arena.GroundEffectSortingOrder), $"{rarity}: on the floor.");
                // From its line's outer edge as it starts, the radius and half the width, out to the gold card's wider one.
                Assert.That(ring.transform.localScale.x, Is.InRange(.2f + .5f * .08f, rarity == UpgradeRarity.Gold ? 1.32f : .87f),
                    $"{rarity}: within the floor its line covered.");
                Assert.That(LinesShown(_root), Is.Zero);
                if (rarity == UpgradeRarity.Gold)
                    Assert.That(Shown("VFX Gold Spark ").Count, Is.GreaterThan(0), "A gold card sends gold sparks up.");
                seen[(int)rarity] = true;
                found++;
            }
            Assert.That(found, Is.EqualTo(seen.Length), "Every tier came within three hundred offers.");
            for (int i = 0; i < 4 && _setup.Choices.IsOpen; i++) _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            LogAssert.NoUnexpectedReceived();
        }

        private static int NotAnEvolution(UpgradeOffer offer)
        {
            for (int c = 0; c < offer.Choices.Count; c++)
                if (!offer.Choices[c].IsEvolution) return c;
            return 0;
        }

        // RunFeedbackFlowTests plays a real evolution out; here the view's own handler stages one on cue.
        [UnityTest, Timeout(90000)]
        public IEnumerator AnEvolutionDoublesThePaintedRingWithTheEmberRingAndLightsTheHero()
        {
            yield return Load();
            Color colour = RunChoiceView.RarityColour(UpgradeRarity.Epic);
            MethodInfo evolve = typeof(RunFeedbackView).GetMethod("ShowEvolution", Private);
            Assert.That(evolve, Is.Not.Null);
            evolve.Invoke(_feedback, new object[] { colour });
            Assert.That(_feedback.ShownEvolutions, Is.EqualTo(1));
            Assert.That(_feedback.IsHeroFlashing, Is.True, "The hero is lit white.");
            List<SpriteRenderer> cores = Shown("VFX Card Core ");
            Assert.That(cores.Count, Is.EqualTo(1));
            AssertTint(cores[0], Color.Lerp(colour, Color.white, .7f), "A white core in the card's tier.");

            yield return Until(() => Shown("VFX Ember Ring ").Count > 0, 1f);
            List<SpriteRenderer> embers = Shown("VFX Ember Ring ");
            Assert.That(embers.Count, Is.EqualTo(1), "The ember ring follows.");
            List<SpriteRenderer> rings = Shown("VFX Card Ring ");
            Assert.That(rings.Count, Is.EqualTo(1), "Doubling the card's own ring.");
            Assert.That(_feedback.ActiveEvolutionRings, Is.GreaterThanOrEqualTo(2));
            AssertTint(rings[0], colour, "The first in the card's tier.");
            AssertTint(embers[0], Color.white, "The second in its own forge ember.");
            Assert.That(embers[0].sprite.texture, Is.SameAs(rings[0].sprite.texture), "One sheet for both.");
            Assert.That(embers[0].sortingOrder, Is.EqualTo(rings[0].sortingOrder), "Both on the floor.");
            Assert.That(embers[0].transform.localScale.x, Is.LessThan(rings[0].transform.localScale.x), "Inside the card's.");
            Assert.That(Shown("VFX Ember Spark ").Count, Is.GreaterThan(0), "Ember sparks rise round him.");
            Assert.That(Shown("VFX Gold Spark ").Count, Is.Zero, "Ember, not gold.");

            yield return Until(() => _feedback.ActiveEvolutionRings == 0 && !_feedback.IsHeroFlashing &&
                !_feedback.IsCardFlashShowing, 3f);
            Assert.That(_feedback.IsHeroFlashing, Is.False, "The white fades.");
            Assert.That(_feedback.ActiveEvolutionRings, Is.Zero);
            Assert.That(Shown("VFX Card").Count + Shown("VFX Ember").Count, Is.Zero, "And every painted frame of it.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AWeaponPickedUpPopsPaintedEmbersOffTheWaist()
        {
            yield return Load();
            MethodInfo pop = typeof(RunFeedbackView).GetMethod("OnWeaponAcquired", Private);
            Assert.That(pop, Is.Not.Null);
            pop.Invoke(_feedback, new object[] { null, null });
            Assert.That(_feedback.ShownWeaponPops, Is.EqualTo(1));
            List<SpriteRenderer> rings = Shown("VFX Ember Ring ");
            Assert.That(rings.Count, Is.EqualTo(1), "A small ember ring at the feet.");
            Assert.That(rings[0].transform.localScale.x, Is.EqualTo(.1f + .5f * .06f).Within(1e-4f), "At its line's outer edge.");
            Assert.That(rings[0].sortingOrder, Is.EqualTo(_arena.GroundEffectSortingOrder));
            List<SpriteRenderer> embers = Shown("VFX Ember Spark ");
            Assert.That(embers.Count, Is.EqualTo(6), "Six embers pop off him.");
            var from = new Vector3[embers.Count];
            for (int i = 0; i < embers.Count; i++)
            {
                SpriteRenderer ember = embers[i];
                Assert.That(ember.transform.localPosition.x, Is.EqualTo(0f).Within(1e-4f), "From his waist.");
                Assert.That(ember.transform.localPosition.y, Is.EqualTo(.35f).Within(1e-4f));
                Assert.That(ember.transform.localEulerAngles.z, Is.InRange(20f, 160f), "Flying up and out.");
                Assert.That(ember.transform.localScale.x, Is.EqualTo(.045f / .05f).Within(1e-4f), "As wide as its line.");
                Assert.That(ember.sortingOrder, Is.EqualTo(SparkOrder));
                from[i] = ember.transform.localPosition;
            }
            yield return new WaitForSecondsRealtime(.08f);
            float left = 0f, right = 0f;
            for (int i = 0; i < embers.Count; i++)
            {
                Assert.That(embers[i].transform.localPosition.y, Is.GreaterThan(from[i].y), $"Ember {i} pops up.");
                left = Mathf.Min(left, embers[i].transform.localPosition.x);
                right = Mathf.Max(right, embers[i].transform.localPosition.x);
            }
            Assert.That(left < -.02f && right > .02f, Is.True, "Either way out.");
            yield return Until(() => Shown("VFX Ember").Count == 0, 3f);
            Assert.That(Shown("VFX Ember").Count, Is.Zero, "Gone in the embers' time.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator WithoutTheSetAViewDrawsItsLinesWhereThePaintedRingStands()
        {
            yield return Load();
            Health hero = _hero.GetComponent<Health>();
            // A second view over the same run, given no set, and a third given a set whose strips are gone.
            var lines = new GameObject("Line feedback").AddComponent<RunFeedbackView>();
            lines.Initialize(_setup.Run, _setup.Upgrades, hero);
            var broken = ScriptableObject.CreateInstance<RunFeedbackArtSet>();
            Assert.That(broken.IsValid, Is.False);
            var brokenView = new GameObject("Broken feedback").AddComponent<RunFeedbackView>();
            brokenView.Initialize(_setup.Run, _setup.Upgrades, hero, broken);
            foreach (RunFeedbackView view in new[] { lines, brokenView })
            {
                Transform root = view.transform.Find("Run Feedback");
                Assert.That(view.UsesPaintedArt, Is.False);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true), Is.Empty, "No painted frames.");
                Assert.That(root.GetComponentsInChildren<LineRenderer>(true).Length, Is.EqualTo(RingSlots + SparkSlots),
                    "Its rings and sparks are the lines, as before.");
            }

            // A level-up held for a sitting (a swarm room's way) plays out in the fight, so the run keeps moving.
            _setup.Upgrades.Hold();
            RunState run = _setup.Run;
            run.AddExperience(run.ExperienceForNextLevel - run.Experience);
            Assert.That(_setup.Choices.IsOpen, Is.False);
            Transform lineRoot = lines.transform.Find("Run Feedback");
            LineRenderer line = lineRoot.Find("Feedback Ring 0").GetComponent<LineRenderer>();
            Assert.That(line.enabled, Is.True, "The line rings his feet.");
            Assert.That(line.loop, Is.True);
            Assert.That(line.positionCount, Is.EqualTo(40));
            Assert.That(line.startWidth, Is.EqualTo(.1f).Within(1e-5f));
            Color gold = line.startColor;
            Assert.That(gold.r == 1f && Mathf.Abs(gold.g - .78f) < 1e-3f && Mathf.Abs(gold.b - .3f) < 1e-3f && gold.a == 1f, Is.True,
                "In the level's gold.");
            Assert.That(line.sortingOrder, Is.EqualTo(_arena.GroundEffectSortingOrder));
            Assert.That(LinesShown(lineRoot), Is.EqualTo(1 + 3), "The ring and the first of its sparks.");
            LineRenderer spark = lineRoot.Find("Feedback Spark 0").GetComponent<LineRenderer>();
            Assert.That(spark.enabled && spark.numCapVertices == 2 && spark.positionCount == 2, Is.True, "A spark is a line.");
            Assert.That(spark.sortingOrder, Is.EqualTo(SparkOrder));

            // Frame by frame, all its life, the painted ring stands exactly where the line's outer edge does, fading as it
            // fades, through its painted frames.
            SpriteRenderer painted = Shown("VFX Level Ring ")[0];
            var frames = new HashSet<Sprite>();
            float end = Time.realtimeSinceStartup + 3f;
            for (int i = 0; line.enabled && Time.realtimeSinceStartup < end; i++)
            {
                float radius = line.GetPosition(0).x;
                Assert.That(painted.enabled, Is.True, $"Frame {i}: on show while the line is.");
                Assert.That(painted.transform.localScale.x, Is.EqualTo(radius + .5f * line.startWidth).Within(1e-4f),
                    $"Frame {i}: the painted ring at the line's outer edge.");
                Assert.That(painted.color.a, Is.EqualTo(line.startColor.a).Within(1e-3f), $"Frame {i}: fading as the line.");
                frames.Add(painted.sprite);
                yield return null;
            }
            Assert.That(line.enabled, Is.False, "The line's half second is over.");
            Assert.That(painted.enabled, Is.False, "And the painted ring's with it.");
            Assert.That(frames.Count, Is.GreaterThanOrEqualTo(4), "Through its painted frames.");
            _setup.Upgrades.Release();
            for (int i = 0; i < 4 && _setup.Choices.IsOpen; i++) _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            Object.Destroy(lines.gameObject);
            Object.Destroy(brokenView.gameObject);
            Object.Destroy(broken);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
