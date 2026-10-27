using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // The Cinder Arcanist, Ilya Kest (docs/45): the second hero, chosen on the forge, whose hits set enemies alight, whose
    // burn passes on a fall one generation deep, and whose Crucible Vent spends every burn in reach at once.
    public sealed class CinderArcanistTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private CombatSetup _setup;
        private Health _hero;
        private AttackController _attack;
        private Targeting _targeting;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Arcanist cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private IEnumerator Load(bool arcanist, PlayerProfile profile = null)
        {
            TestProfile.Begin(profile);
            if (arcanist)
                File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _attack = _hero.GetComponent<AttackController>();
            _targeting = _hero.GetComponent<Targeting>();
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

        // The name of the sprite the ability button shows.
        private static string AbilityIcon()
        {
            foreach (var image in GameObject.Find("Ability Button").GetComponentsInChildren<Image>(true))
                if (image.sprite != null && image.sprite.name.StartsWith("UI_Icon_"))
                    return image.sprite.name;
            return string.Empty;
        }

        [UnityTest]
        public IEnumerator TheArcanistPlaysWithItsOwnHealthAbilityAndName()
        {
            yield return Load(true);
            Assert.That(_setup.Hero.Id, Is.EqualTo(ArcanistId));
            Assert.That(_setup.Hero.Subtitle, Is.EqualTo("Ilya Kest"));
            Assert.That(_hero.Maximum, Is.EqualTo(90f), "The glass cannon.");
            Assert.That(_setup.Ability.Kind, Is.EqualTo(AbilityKind.Vent), "Crucible Vent on the one button.");
            Assert.That(_setup.Weapons.Equipped.Id, Is.EqualTo("weapon_staff"), "The Staff is the Arcanist's own.");
            Assert.That(GameObject.Find("Hero Label").GetComponent<Text>().text, Does.Contain("Cinder Arcanist"));
            Assert.That(AbilityIcon(), Does.Contain("CrucibleVent"), "The ability button wears the crucible.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheDefaultHeroStaysTheVanguardWithNothingAlight()
        {
            yield return Load(false);
            Assert.That(_setup.Hero.Id, Is.EqualTo("hero_vanguard"));
            Assert.That(_setup.Ability.Kind, Is.EqualTo(AbilityKind.Burst));
            Assert.That(AbilityIcon(), Does.Contain("ForgeBurst"), "And the Vanguard's button keeps its anvil.");
            Health[] dummies = Dummies(.3f);
            _attack.enabled = true;
            for (int i = 0; i < 90 && _attack.AttackCount == 0; i++) yield return null;
            Assert.That(_attack.AttackCount, Is.GreaterThan(0));
            Assert.That(dummies[0].GetComponent<BurnStatus>(), Is.Null, "Only Ember Memory sets enemies alight.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator HitsSetEnemiesAlightAndTheBurnTicksThemDown()
        {
            yield return Load(true);
            Health[] dummies = Dummies(.3f);
            _attack.enabled = true;
            for (int i = 0; i < 120 && _attack.AttackCount == 0; i++) yield return null;
            _attack.enabled = false;
            Assert.That(_attack.AttackCount, Is.GreaterThan(0));
            var burn = dummies[0].GetComponent<BurnStatus>();
            Assert.That(burn, Is.Not.Null, "The hit set it alight.");
            Assert.That(burn.Stacks, Is.GreaterThanOrEqualTo(1));
            Assert.That(burn.State.HasDirectStack, Is.True, "A stack from the hero's own hit.");
            float before = dummies[0].Current;
            int stacks = burn.Stacks;
            for (int i = 0; i < 66; i++) yield return null;
            Assert.That(before - dummies[0].Current, Is.GreaterThanOrEqualTo(BurnRule.TickDamage(stacks) * 2f - 1e-3f),
                "Two ticks in a little over a second, with no further hit.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AFallPassesTheBurnOnOnceAndOnlyOneGenerationDeep()
        {
            yield return Load(true);
            // a burns; b and c stand beside it; d stands beside b but out of a's reach; far stands apart.
            Health[] dummies = Dummies(1f, 2f, 2.2f, 3f, 8f);
            Health a = dummies[0], b = dummies[1], c = dummies[2], d = dummies[3], far = dummies[4];
            BurnStatus.On(a).Ignite(1, false, _hero, _attack);
            a.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            yield return null;
            Assert.That(b.GetComponent<BurnStatus>()?.IsBurning, Is.True, "Its neighbour caught it.");
            Assert.That(c.GetComponent<BurnStatus>()?.IsBurning, Is.True);
            Assert.That(b.GetComponent<BurnStatus>().State.HasDirectStack, Is.False, "As a spread stack.");
            Assert.That(d.GetComponent<BurnStatus>(), Is.Null, "Out of reach of the fall.");
            Assert.That(far.GetComponent<BurnStatus>(), Is.Null);
            b.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            yield return null;
            Assert.That(d.GetComponent<BurnStatus>(), Is.Null, "A spread burn passes nothing on.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CrucibleVentSpendsEveryBurnInReachAndErupts()
        {
            yield return Load(true);
            // a carries three stacks and c stands beside it unlit; b carries one on the hero's other side.
            Health[] dummies = Dummies(1f, -1f, 1.8f, 6f);
            Health a = dummies[0], b = dummies[1], c = dummies[2], outside = dummies[3];
            for (int i = 0; i < 3; i++) BurnStatus.On(a).Ignite(1, false, _hero, _attack);
            BurnStatus.On(b).Ignite(1, false, _hero, _attack);
            BurnStatus.On(outside).Ignite(2, false, _hero, _attack);
            var ability = _hero.GetComponent<AbilityController>();
            int vents = 0;
            ability.Vented += (enemy, stacks) => vents++;
            float perStack = _setup.Ability.Damage;
            Assert.That(ability.TryUse(), Is.True);
            Assert.That(vents, Is.EqualTo(2), "Two burning enemies in reach erupted.");
            Assert.That(a.GetComponent<BurnStatus>().IsBurning, Is.False, "Every stack spent.");
            Assert.That(b.GetComponent<BurnStatus>().IsBurning, Is.False);
            Assert.That(outside.GetComponent<BurnStatus>().Stacks, Is.EqualTo(2), "Out of reach keeps its burn.");
            Assert.That(a.Current, Is.EqualTo(1000f - 3 * perStack).Within(1e-3f));
            Assert.That(b.Current, Is.EqualTo(1000f - perStack).Within(1e-3f));
            Assert.That(c.Current, Is.EqualTo(1000f - 3 * perStack).Within(1e-3f), "The eruption reached its unlit neighbour.");
            Assert.That(ability.TryUse(), Is.False, "Then it cools down.");
            LogAssert.NoUnexpectedReceived();
        }

        // Review, 2026-10-27: a burn that borrowed the AttackController's neighbourhood refilled the very list a Cinder Nova
        // burst was still walking when the burst felled a burning enemy.
        [UnityTest]
        public IEnumerator ABurningFallNeverRefillsTheListAStrikeIsWalking()
        {
            yield return Load(true);
            Health[] dummies = Dummies(1f, 1.5f, 2f, 6f, 6.5f);
            Health burning = dummies[0];
            Assert.That(_setup.BurnNeighbourhood, Is.Not.Null.And.Not.SameAs(_attack), "A burn has a neighbourhood of its own.");
            var walking = ((INeighbourhood)_attack).Near(dummies[3], 1f);
            Assert.That(walking, Is.EqualTo(new IDamageable[] { dummies[4] }), "The strike's list, mid-walk.");
            BurnStatus.On(burning).Ignite(1, false, _hero, _setup.BurnNeighbourhood);
            burning.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(dummies[1].GetComponent<BurnStatus>()?.IsBurning, Is.True, "The fall still passed the burn on.");
            Assert.That(walking, Is.EqualTo(new IDamageable[] { dummies[4] }), "And left the strike's list alone.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator BurnsGoOutWhenTheHeroFallsOrTheRunEnds()
        {
            yield return Load(true);
            Health[] dummies = Dummies(1f, 3f);
            BurnStatus.On(dummies[0]).Ignite(3, false, _hero, _setup.BurnNeighbourhood);
            BurnStatus.On(dummies[1]).Ignite(3, false, _hero, _setup.BurnNeighbourhood);
            _hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            float before = dummies[0].Current;
            for (int i = 0; i < 70; i++) yield return null;
            Assert.That(dummies[0].Current, Is.EqualTo(before), "A fallen hero's burns deal nothing.");
            Assert.That(dummies[0].GetComponent<BurnStatus>().IsBurning, Is.False);
            Assert.That(dummies[1].GetComponent<BurnStatus>().IsBurning, Is.False, "The run's end put every burn out.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheForgeRackFollowsTheChosenHerosStartingWeapon()
        {
            yield return Load(false);
            var forge = Object.FindFirstObjectByType<RelicForgeView>();
            forge.Open();
            yield return null;
            Assert.That(_setup.Weapons.StartingWeapon.Id, Is.EqualTo("weapon_sword"));
            forge.HeroChoice.Cycle();
            Assert.That(_setup.Weapons.StartingWeapon.Id, Is.EqualTo("weapon_staff"), "The Arcanist's Staff is free.");
            Assert.That(_setup.Weapons.Equipped.Id, Is.EqualTo("weapon_staff"), "And carried, with nothing else equipped.");
            forge.HeroChoice.Cycle();
            Assert.That(_setup.Weapons.StartingWeapon.Id, Is.EqualTo("weapon_sword"), "Back with the Vanguard.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheForgeChoosesTheNextRunsHero()
        {
            yield return Load(false);
            var forge = Object.FindFirstObjectByType<RelicForgeView>();
            Assert.That(forge.HeroChoice, Is.Not.Null, "Two heroes: the forge carries a chooser.");
            forge.Open();
            yield return null;
            Assert.That(forge.HeroChoice.Current.Id, Is.EqualTo("hero_vanguard"));
            Assert.That(forge.HeroChoice.ShownText, Does.StartWith("Vanguard"));
            Sprite vanguard = forge.HeroChoice.ShownPortrait;
            Assert.That(vanguard, Is.Not.Null, "The chooser pictures the hero.");
            Assert.That(vanguard, Is.SameAs(forge.HeroChoice.Current.Portrait));
            forge.HeroChoice.Cycle();
            Assert.That(_setup.Profile.SelectedHeroId, Is.EqualTo(ArcanistId), "The profile remembers the choice.");
            Assert.That(forge.HeroChoice.ShownText, Does.StartWith("Cinder Arcanist").And.Contain("Ilya Kest"));
            Assert.That(forge.HeroChoice.ShownPortrait, Is.Not.Null.And.Not.SameAs(vanguard), "Ilya stands in its own art.");
            Assert.That(forge.HeroChoice.ShownPortrait.texture.name, Does.StartWith("CHR_CinderArcanist"));
            Assert.That(TestProfile.ReadSaved().SelectedHeroId, Is.EqualTo(ArcanistId), "And saves it.");
            forge.HeroChoice.Cycle();
            Assert.That(_setup.Profile.SelectedHeroId, Is.Null, "Back to the default, stored as no choice.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClearingTheFoundryWithTheStaffUnlocksTheArcanist()
        {
            yield return Load(false, new PlayerProfile(0, null, null, 0, new[] { "weapon_staff" }, "weapon_staff"));
            Assert.That(_setup.Profile.HasUnlockedHero(ArcanistId), Is.False);
            _setup.UnlockHeroesForTheFoundry();
            Assert.That(_setup.Profile.HasUnlockedHero(ArcanistId), Is.True, "The Staff carried through The Foundry.");
            Assert.That(TestProfile.ReadSaved().HasUnlockedHero(ArcanistId), Is.True);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ClearingTheFoundryWithTheSwordUnlocksNothing()
        {
            yield return Load(false);
            Assert.That(_setup.HeroWeapon.Id, Is.EqualTo("weapon_sword"));
            _setup.UnlockHeroesForTheFoundry();
            Assert.That(_setup.Profile.HasUnlockedHero(ArcanistId), Is.False);
            Assert.That(HeroUnlock.IsPlayable(false, false, false), Is.False, "A release build plays a locked hero never.");
            Assert.That(HeroUnlock.IsPlayable(false, true, false), Is.True);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
