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
    // The game-feel pass, stage 2 (docs/43 section 1, 2026-11-22): the hero's reactions over what its look already plays
    // (HeroFeelView) - a blow squashes the figure away from it and shoves it back, then it recovers; death lets the
    // collapse play, then bursts into ash; a level pops the figure over a halo; a weapon picked up rises over the head
    // once the run moves. Visual only: the hero, its shadow and its weapons' loadouts never move.
    public sealed class HeroFeelTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private HeroFeelView _feel;
        private Health _hero;
        private CombatSetup _setup;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Hero feel cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        private IEnumerator Load(bool arcanist = false)
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (arcanist)
                File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            var movement = Object.FindFirstObjectByType<HeroMovementInput>();
            movement.enabled = false;
            _hero = movement.GetComponent<Health>();
            _feel = movement.GetComponent<HeroFeelView>();
            _setup = Object.FindFirstObjectByType<CombatSetup>();
        }

        private void ResolveChoices()
        {
            for (int i = 0; i < 8 && _setup.Choices.IsOpen; i++)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
        }

        // An attacker standing a unit to the hero's right on the floor.
        private Health AttackerOnTheRight()
        {
            var attacker = new GameObject("Hero feel attacker").AddComponent<Health>();
            attacker.Initialize(100f);
            attacker.transform.position = _hero.transform.position + Vector3.right;
            return attacker;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ABlowSquashesThePuppetAwayFromItThenItRecovers()
        {
            yield return Load();
            var puppet = _hero.GetComponent<PuppetAnimator>();
            Assert.That(puppet, Is.Not.Null, "The Vanguard wears the puppet.");
            Transform rear = puppet.RearRig.Root.transform, front = puppet.FrontRig.Root.transform;
            float rearSign = Mathf.Sign(rear.localScale.x), frontSign = Mathf.Sign(front.localScale.x);
            Vector3 heroAt = _hero.transform.position;
            Transform shadow = _hero.GetComponentInChildren<ContactShadowView>().transform;
            Vector3 shadowAt = shadow.localPosition;
            _hero.ApplyDamage(new DamageContext(1f, AttackerOnTheRight()));
            float shoved = 0f, narrowest = 1f, tallest = 1f;
            for (int i = 0; i < 8; i++)
            {
                yield return null;
                shoved = Mathf.Min(shoved, puppet.Root.localPosition.x);
                narrowest = Mathf.Min(narrowest, Mathf.Abs(puppet.ActiveRig.Root.transform.localScale.x));
                tallest = Mathf.Max(tallest, puppet.ActiveRig.Root.transform.localScale.y);
                Assert.That(_hero.transform.position, Is.EqualTo(heroAt), "The hero itself never moves.");
                Assert.That(shadow.localPosition, Is.EqualTo(shadowAt), "Nor does its shadow.");
            }
            Assert.That(shoved, Is.LessThan(-.03f), "Shoved left, away from a blow from the right.");
            Assert.That(narrowest, Is.LessThan(.95f), "A blow from the side squeezes the width.");
            Assert.That(tallest, Is.GreaterThan(1.02f), "And lifts the height.");
            for (int i = 0; i < 20; i++)
                yield return null;
            Assert.That(_feel.Recoil, Is.Zero);
            Assert.That(puppet.Root.localPosition, Is.EqualTo(Vector3.zero), "Back where it stood.");
            Assert.That(rear.localScale, Is.EqualTo(new Vector3(rearSign, 1f, 1f)), "Its own shape, its mirror kept.");
            Assert.That(front.localScale, Is.EqualTo(new Vector3(frontSign, 1f, 1f)));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator IlyasDrawingRecoilsAwayFromTheBlowToo()
        {
            yield return Load(true);
            var drawn = _hero.GetComponent<RenderedCharacterAnimator>();
            Assert.That(drawn, Is.Not.Null, "Ilya is drawn from her rendered set.");
            _hero.ApplyDamage(new DamageContext(1f, AttackerOnTheRight()));
            float shoved = 0f, narrowest = 1f;
            for (int i = 0; i < 8; i++)
            {
                yield return null;
                shoved = Mathf.Min(shoved, drawn.Root.localPosition.x);
                narrowest = Mathf.Min(narrowest, Mathf.Abs(drawn.Root.localScale.x));
            }
            Assert.That(shoved, Is.LessThan(-.03f));
            Assert.That(narrowest, Is.LessThan(.95f));
            for (int i = 0; i < 20; i++)
                yield return null;
            Assert.That(drawn.Root.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(Mathf.Abs(drawn.Root.localScale.x), Is.EqualTo(1f), "Only its mirror is left.");
            Assert.That(drawn.Root.localScale.y, Is.EqualTo(1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator DeathCollapsesThenBurstsIntoAshAndLeavesNothingBehind()
        {
            yield return Load();
            var puppet = _hero.GetComponent<PuppetAnimator>();
            int renderers = _feel.Root.GetComponentsInChildren<Renderer>(true).Length;
            _hero.ApplyDamage(new DamageContext(100000f));
            Assert.That(_feel.IsDissolving, Is.True, "The death is heard at once.");
            float waited = 0f;
            int streamed = 0;
            while (waited < HeroFeelView.AshDelay - .1f)
            {
                yield return null;
                waited += Time.deltaTime;
                streamed = Mathf.Max(streamed, _feel.ShownMotes);
                Assert.That(_feel.HasBurstIntoAsh, Is.False, "Not before the collapse is down.");
            }
            Assert.That(puppet.IsDying, Is.True, "The puppet's own collapse plays meanwhile.");
            Assert.That(streamed, Is.GreaterThan(0), "Ash streams off the falling body.");
            for (int i = 0; i < 12; i++)
                yield return null;
            Assert.That(_feel.HasBurstIntoAsh, Is.True, "Then the body bursts into ash.");
            Assert.That(_feel.ShownMotes, Is.GreaterThanOrEqualTo(10));
            for (int i = 0; i < 90; i++)
                yield return null;
            Assert.That(_feel.ShownMotes, Is.Zero, "The ash is gone within the second.");
            foreach (Renderer renderer in _feel.Root.GetComponentsInChildren<Renderer>())
                Assert.That(renderer.enabled, Is.False, renderer.name);
            Assert.That(_feel.Root.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(renderers), "Pooled, none made.");

            // The restart: the next run's hero has its own view and nothing of this one's.
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            var next = Object.FindFirstObjectByType<HeroFeelView>();
            Assert.That(Object.FindObjectsByType<HeroFeelView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(next.IsDissolving, Is.False);
            Assert.That(next.ShownMotes, Is.Zero);
            Assert.That(next.Root.GetComponentsInChildren<Renderer>(true).Length, Is.EqualTo(renderers));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator ALevelPopsTheFigureUpAndItSettles()
        {
            yield return Load();
            var puppet = _hero.GetComponent<PuppetAnimator>();
            RunState run = _setup.Run;
            int before = _feel.LevelPopsShown;
            run.AddExperience(run.ExperienceForNextLevel - run.Experience);
            Assert.That(_feel.LevelPopsShown, Is.EqualTo(before + 1), "A level heard.");
            ResolveChoices();
            float tallest = 1f, peak = 0f;
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                ResolveChoices();
                peak = Mathf.Max(peak, _feel.Pop);
                tallest = Mathf.Max(tallest, puppet.ActiveRig.Root.transform.localScale.y);
            }
            Assert.That(peak, Is.GreaterThan(.5f), "It popped.");
            Assert.That(tallest, Is.GreaterThan(1.04f), "The figure stretched up.");
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                ResolveChoices();
            }
            Assert.That(_feel.Pop, Is.Zero);
            Assert.That(puppet.ActiveRig.Root.transform.localScale.y, Is.EqualTo(1f), "And settled.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator APickedWeaponRisesOverTheHeroOnceTheRunMoves()
        {
            yield return Load();
            UpgradeService upgrades = _setup.Upgrades;
            Assert.That(upgrades.IsCategoryMode, Is.True);
            bool acquired = false;
            upgrades.WeaponAcquired += (card, runtime) => acquired = true;
            // Bonus offers until one shows a new weapon, taken; every other offer takes its first card.
            for (int i = 0; i < 40 && !acquired; i++)
            {
                if (upgrades.CurrentOffer == null)
                    _setup.Run.GrantBonusUpgrade();
                yield return null;
                UpgradeOffer offer = upgrades.CurrentOffer;
                if (offer == null)
                    continue;
                int slot = 0;
                for (int c = 0; c < offer.Choices.Count; c++)
                    if (offer.Choices[c].Category == OfferCategory.NewWeapon)
                    {
                        slot = c;
                        break;
                    }
                Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, slot), Is.True);
            }
            Assert.That(acquired, Is.True, "A new weapon came within forty offers.");
            Assert.That(_feel.PickupsShown, Is.GreaterThanOrEqualTo(1));
            Assert.That(_feel.IsPickupShowing, Is.True);
            ResolveChoices();
            float lowest = float.MaxValue, highest = float.MinValue;
            bool shown = false;
            for (int i = 0; i < 30 && _feel.IsPickupShowing; i++)
            {
                yield return null;
                ResolveChoices();
                SpriteRenderer ghost = _feel.PickupGhost;
                if (!ghost.enabled)
                    continue;
                shown = true;
                Assert.That(ghost.sprite, Is.Not.Null);
                Assert.That(ghost.color.a, Is.LessThanOrEqualTo(1f));
                float y = ghost.bounds.center.y - _hero.transform.position.y;
                lowest = Mathf.Min(lowest, y);
                highest = Mathf.Max(highest, y);
            }
            Assert.That(shown, Is.True, "The picked weapon shows once the run moves.");
            Assert.That(highest - lowest, Is.GreaterThan(.3f), "It rises.");
            Assert.That(lowest, Is.GreaterThan(.8f), "Over the shoulders, never across the face's middle.");
            for (int i = 0; i < 40 && _feel.IsPickupShowing; i++)
            {
                yield return null;
                ResolveChoices();
            }
            Assert.That(_feel.IsPickupShowing, Is.False, "Then it is gone.");
            Assert.That(_feel.PickupGhost.enabled, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
