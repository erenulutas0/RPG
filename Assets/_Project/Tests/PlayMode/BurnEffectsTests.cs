using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The skills' effects (docs/45): a burning enemy carries one flame, sized by its stacks, that follows it and goes out
    // with the burn, the fall or the body; each Crucible Vent eruption shows its column and embers; Ember Memory's spread
    // hops an ember to the neighbour it lights; and the Forge Burst sends a shockwave and sparks out beside its ring.
    public sealed class BurnEffectsTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private Health _hero;
        private AttackController _attack;
        private Targeting _targeting;
        private CombatEffectsView _effects;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Burn effects cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private IEnumerator Load(bool arcanist)
        {
            TestProfile.Begin();
            if (arcanist)
                File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _attack = _hero.GetComponent<AttackController>();
            _targeting = _hero.GetComponent<Targeting>();
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
        }

        // The encounter and every attack off, and the hero's targets these dummies alone, so a test places its own fight.
        private Health[] Dummies(params float[] offsets)
        {
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            var dummies = new Health[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                dummies[i] = new GameObject("Burn dummy " + i).AddComponent<Health>();
                dummies[i].Initialize(1000);
                dummies[i].transform.position = _hero.transform.position + Vector3.right * offsets[i];
            }
            _targeting.SetCandidates(dummies);
            return dummies;
        }

        // Every pooled effect on show whose sprite is one of the named frames.
        private static List<SpriteRenderer> Shown(string spritePrefix)
        {
            var shown = new List<SpriteRenderer>();
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite != null && renderer.sprite.name.StartsWith(spritePrefix))
                    shown.Add(renderer);
            return shown;
        }

        private static SpriteRenderer Nearest(List<SpriteRenderer> shown, Vector3 point)
        {
            SpriteRenderer nearest = null;
            foreach (var renderer in shown)
                if (nearest == null || Mathf.Abs(renderer.transform.position.x - point.x) < Mathf.Abs(nearest.transform.position.x - point.x))
                    nearest = renderer;
            return nearest;
        }

        private IEnumerator WaitForFlames(int count)
        {
            // A burn lit by no strike is found by its first tick, half a second in.
            for (int i = 0; i < 40 && _effects.ShownFlameCount < count; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator AStruckEnemyCarriesAFlameThatFollowsItAndGrowsWithItsStacks()
        {
            yield return Load(true);
            Health dummy = Dummies(.3f)[0];
            _attack.enabled = true;
            for (int i = 0; i < 120 && _attack.AttackCount == 0; i++) yield return null;
            _attack.enabled = false;
            Assert.That(_attack.AttackCount, Is.GreaterThan(0));
            yield return null;

            var burn = dummy.GetComponent<BurnStatus>();
            Assert.That(burn != null && burn.IsBurning, Is.True, "Ember Memory set it alight.");
            SpriteRenderer flame = _effects.BurnFlameOn(dummy);
            Assert.That(flame, Is.Not.Null, "The flame shows by the frame after the hit, not at the burn's first tick.");
            Assert.That(flame.sprite, Is.Not.Null);
            Assert.That(_effects.ShownFlameCount, Is.EqualTo(1));
            Assert.That(flame.sprite.rect.height, Is.EqualTo(BurnArt.FlameHeight(burn.Stacks)), "Sized by its stacks.");
            Vector3 feet = dummy.transform.position;
            float lift = flame.transform.position.y - feet.y;
            Assert.That(flame.transform.position.x, Is.EqualTo(feet.x).Within(1e-4f));
            Assert.That(lift, Is.InRange(0f, .6f), "On the body, above its feet.");

            dummy.transform.position += new Vector3(1f, .5f, 0f);
            yield return null;
            Assert.That(flame.transform.position.x, Is.EqualTo(dummy.transform.position.x).Within(1e-4f), "It follows its enemy.");
            Assert.That(flame.transform.position.y - dummy.transform.position.y, Is.EqualTo(lift).Within(1e-4f));

            BurnStatus.On(dummy).Ignite(BurnRule.MaxStacks, false, _hero, _attack);
            yield return null;
            Assert.That(_effects.BurnFlameOn(dummy), Is.SameAs(flame), "One renderer per burning enemy, however many stacks.");
            Assert.That(_effects.ShownFlameCount, Is.EqualTo(1));
            Assert.That(flame.sprite.rect.height, Is.EqualTo(BurnArt.FlameHeight(BurnRule.MaxStacks)), "A full burn stands tallest.");
            Sprite first = flame.sprite;
            bool flickered = false;
            for (int i = 0; i < 30 && !flickered; i++)
            {
                yield return null;
                flickered = flame.sprite != first;
            }
            Assert.That(flickered, Is.True, "It flickers.");

            // A pause holds the flicker, as it holds the burn.
            Time.timeScale = 0;
            yield return null;
            Sprite held = flame.sprite;
            for (int i = 0; i < 12; i++) yield return null;
            Assert.That(flame.sprite, Is.SameAs(held));
            Assert.That(flame.enabled, Is.True);
            Time.timeScale = 1;
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheFlameGuttersOutWithTheBurn()
        {
            yield return Load(true);
            Health dummy = Dummies(.3f)[0];
            BurnStatus.On(dummy).Ignite(1, false, _hero, _attack);
            yield return WaitForFlames(1);
            SpriteRenderer flame = _effects.BurnFlameOn(dummy);
            Assert.That(flame, Is.Not.Null, "A burn tick shows the flame of an enemy no strike lit.");

            var burn = dummy.GetComponent<BurnStatus>();
            float faintest = 1f;
            for (int frames = 0; burn.IsBurning && frames < 400; frames++)
            {
                Assert.That(_effects.BurnFlameOn(dummy), Is.SameAs(flame), "It burns as long as the burn does.");
                faintest = Mathf.Min(faintest, flame.color.a);
                yield return null;
            }
            Assert.That(burn.IsBurning, Is.False, "With nothing to refresh it, the burn ran out.");
            Assert.That(faintest, Is.LessThan(1f), "It gutters rather than blinking off.");
            yield return null;
            Assert.That(_effects.BurnFlameOn(dummy), Is.Null, "Out with the burn.");
            Assert.That(flame.enabled, Is.False);
            Assert.That(_effects.ShownFlameCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheFlameGoesWithItsFallenOrRetiredEnemy()
        {
            yield return Load(true);
            // Four units apart: the fall passes nothing to the other.
            Health[] dummies = Dummies(2f, -2f);
            foreach (Health dummy in dummies) BurnStatus.On(dummy).Ignite(2, false, _hero, _attack);
            yield return WaitForFlames(2);
            Assert.That(_effects.ShownFlameCount, Is.EqualTo(2));

            SpriteRenderer fallen = _effects.BurnFlameOn(dummies[0]);
            dummies[0].ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            yield return null;
            Assert.That(_effects.BurnFlameOn(dummies[0]), Is.Null, "The fall puts it out.");
            Assert.That(fallen.enabled, Is.False);
            SpriteRenderer retired = _effects.BurnFlameOn(dummies[1]);
            Assert.That(retired, Is.Not.Null, "The other burns on.");

            // The body is taken away while it still burns, as the horde retires a body or leaves the floor.
            Object.Destroy(dummies[1].gameObject);
            yield return null;
            yield return null;
            Assert.That(retired.enabled, Is.False, "A body taken away takes its flame.");
            Assert.That(_effects.ShownFlameCount, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheCrowdsFlamesStayWithinOnePoolAndWaitForAFreeOne()
        {
            yield return Load(true);
            int renderers = GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>(true).Length;
            int capacity = _effects.FlameCapacity;
            Assert.That(capacity, Is.EqualTo(PackLayout.MaxPackSize), "A flame for every enemy the scene lets stand.");
            var offsets = new float[capacity + 5];
            for (int i = 0; i < offsets.Length; i++) offsets[i] = 3f + i * .5f;
            Health[] dummies = Dummies(offsets);
            for (int i = 0; i < dummies.Length; i++) BurnStatus.On(dummies[i]).Ignite(1 + i % BurnRule.MaxStacks, false, _hero, _attack);
            yield return WaitForFlames(capacity);
            Assert.That(_effects.ShownFlameCount, Is.EqualTo(capacity), "Never more flames than the pool holds.");
            Assert.That(GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(renderers),
                "No renderer is made for a burn; the pool was built up front.");

            Health waiting = null;
            foreach (Health dummy in dummies)
                if (_effects.BurnFlameOn(dummy) == null) waiting = dummy;
            Assert.That(waiting, Is.Not.Null, "Five burn without a flame while every one is taken.");
            for (int i = 0; i < 5; i++)
            {
                Health lit = null;
                foreach (Health dummy in dummies)
                    if (dummy.IsAlive && _effects.BurnFlameOn(dummy) != null) { lit = dummy; break; }
                lit.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            }
            for (int i = 0; i < 40 && _effects.BurnFlameOn(waiting) == null; i++) yield return null;
            Assert.That(_effects.BurnFlameOn(waiting), Is.Not.Null, "A freed flame goes to a burning enemy that waited.");
            Assert.That(_effects.ShownFlameCount, Is.LessThanOrEqualTo(capacity));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ACrucibleVentEruptsAtEveryBurningEnemyItSpends()
        {
            yield return Load(true);
            Health[] dummies = Dummies(1f, -1f);
            Health a = dummies[0], b = dummies[1];
            BurnStatus.On(a).Ignite(3, false, _hero, _attack);
            BurnStatus.On(b).Ignite(1, false, _hero, _attack);
            yield return WaitForFlames(2);
            Assert.That(_effects.ShownFlameCount, Is.EqualTo(2));

            Assert.That(_hero.GetComponent<AbilityController>().TryUse(), Is.True);
            List<SpriteRenderer> columns = Shown("VFX Vent Column");
            Assert.That(columns.Count, Is.EqualTo(2), "A column at each enemy the Vent spent.");
            SpriteRenderer atA = Nearest(columns, a.transform.position), atB = Nearest(columns, b.transform.position);
            Assert.That(atA, Is.Not.SameAs(atB));
            Assert.That(atA.transform.position.x, Is.EqualTo(a.transform.position.x).Within(1e-4f), "It rises from the enemy's feet.");
            Assert.That(atA.transform.position.y, Is.EqualTo(a.transform.position.y).Within(1e-4f));
            Assert.That(atA.transform.localScale.x, Is.GreaterThan(atB.transform.localScale.x), "Three stacks erupt larger than one.");
            List<SpriteRenderer> embers = Shown("VFX Vent Embers");
            Assert.That(embers.Count, Is.EqualTo(3), "Two clusters of embers from three stacks, one from one.");
            var rings = new List<SpriteRenderer>();
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == StaffSplashArt.Get()) rings.Add(renderer);
            Assert.That(rings.Count, Is.EqualTo(2), "And an ember ring as wide as each splash.");
            Assert.That(Shown("VFX Forge").Count, Is.Zero, "The Vent's blast is its eruptions, not the Forge Burst's.");

            float risenFrom = embers[0].transform.position.y;
            for (int i = 0; i < 6; i++) yield return null;
            Assert.That(embers[0].transform.position.y, Is.GreaterThan(risenFrom), "The embers rise.");
            foreach (var ring in rings) Assert.That(ring.color.r, Is.GreaterThan(ring.color.b + .3f), "The ring stays ember as it grows.");
            Assert.That(_effects.BurnFlameOn(a), Is.Null, "The Vent spent the burn, so its flame is out.");
            Assert.That(_effects.BurnFlameOn(b), Is.Null);

            for (int i = 0; i < 40; i++) yield return null;
            Assert.That(Shown("VFX Vent").Count, Is.Zero, "The eruption is over in well under a second.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator EmberMemoryHopsAnEmberToEachNeighbourItLights()
        {
            yield return Load(true);
            Health[] dummies = Dummies(1f, 2f, 2.2f);
            Health fallen = dummies[0], b = dummies[1], c = dummies[2];
            BurnStatus.On(fallen).Ignite(1, false, _hero, _attack);
            fallen.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(b.GetComponent<BurnStatus>().IsBurning && c.GetComponent<BurnStatus>().IsBurning, Is.True);
            List<SpriteRenderer> arcs = Shown("VFX Ember Arc");
            Assert.That(arcs.Count, Is.EqualTo(2), "An ember to each neighbour it lit.");
            float start = arcs[0].transform.position.x;
            yield return null;
            Assert.That(arcs[0].transform.position.x, Is.GreaterThan(start), "Flying from the fallen toward its neighbour.");
            Assert.That(arcs[0].transform.position.x, Is.LessThan(c.transform.position.x));
            Assert.That(arcs[0].transform.localRotation, Is.Not.EqualTo(Quaternion.identity), "Turned along its arc.");
            Assert.That(_effects.BurnFlameOn(b), Is.Null, "The neighbour's flame waits for the ember to land.");

            for (int i = 0; i < 30 && (_effects.BurnFlameOn(b) == null || _effects.BurnFlameOn(c) == null); i++) yield return null;
            yield return null;
            Assert.That(_effects.BurnFlameOn(b), Is.Not.Null, "Then it burns.");
            Assert.That(_effects.BurnFlameOn(c), Is.Not.Null);
            Assert.That(Shown("VFX Ember Arc").Count, Is.Zero, "The embers have landed.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheForgeBurstSendsAShockwaveAndSparksBesideItsRing()
        {
            yield return Load(false);
            Dummies(1f);
            Assert.That(_hero.GetComponent<AbilityController>().TryUse(), Is.True);
            var ring = new List<SpriteRenderer>();
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == AbilityRingArt.Get()) ring.Add(renderer);
            Assert.That(ring.Count, Is.EqualTo(1), "The burst keeps its ring.");
            List<SpriteRenderer> waves = Shown("VFX Forge Shockwave");
            Assert.That(waves.Count, Is.EqualTo(1), "A shockwave from the hero.");
            Assert.That(waves[0].sortingOrder, Is.EqualTo(ring[0].sortingOrder), "On the floor with the ring, under the actors.");
            List<SpriteRenderer> sparks = Shown("VFX Forge Spark");
            Assert.That(sparks.Count, Is.EqualTo(CombatEffectsView.ForgeSparkCount), "And a spray of forge sparks.");
            Assert.That(Shown("VFX Vent").Count, Is.Zero);

            float size = waves[0].transform.localScale.x;
            float heroX = _hero.transform.position.x;
            foreach (var spark in sparks) Assert.That(spark.transform.position.x, Is.EqualTo(heroX).Within(1e-4f), "From the hero.");
            for (int i = 0; i < 6; i++) yield return null;
            Assert.That(waves[0].transform.localScale.x, Is.GreaterThan(size), "The shockwave races out.");
            float reach = _hero.GetComponent<AbilityController>().Ability.Radius;
            Assert.That(waves[0].transform.localScale.x * EffectArt.ShockwaveRadius / PixelSpriteFactory.PixelsPerUnit,
                Is.LessThanOrEqualTo(reach + 1e-3f), "Never past the burst's reach.");
            float spread = 0f;
            foreach (var spark in sparks) spread += Mathf.Abs(spark.transform.position.x - heroX) / sparks.Count;
            Assert.That(spread, Is.GreaterThan(.1f), "The sparks fly out all round.");

            for (int i = 0; i < 24; i++) yield return null;
            Assert.That(Shown("VFX Forge").Count, Is.Zero, "Over in under half a second.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
