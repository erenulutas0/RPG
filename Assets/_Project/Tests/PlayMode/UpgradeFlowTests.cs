using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
#if UNITY_EDITOR
using Cryptforge.Content;
using UnityEditor;
#endif

namespace Cryptforge.Tests
{
    public sealed class UpgradeFlowTests
    {
        private CombatSetup _setup;
        private AttackController _attack;
        private Targeting _targeting;
        private EncounterController _encounters;
        private RunChoiceView _view;
        private Button[] _buttons;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _attack = GameObject.Find("Vanguard").GetComponent<AttackController>();
            _targeting = _attack.GetComponent<Targeting>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _view = Object.FindFirstObjectByType<RunChoiceView>();
            _buttons = _view.GetComponentsInChildren<Button>(true);
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Upgrade Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator KillOpensTwoTouchChoicesAndPausesCombat()
        {
            Assert.That(_view.IsOpen, Is.False);
            Assert.That(_buttons[0].gameObject.activeInHierarchy, Is.False);
            yield return WaitForOfferInput();

            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            Assert.That(offer.Choices.Count, Is.EqualTo(2));
            Assert.That(_view.IsOpen, Is.True);
            Assert.That(Time.timeScale, Is.Zero);
            Assert.That(_setup.Run.PendingUpgrades, Is.EqualTo(1));
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                Assert.That(_buttons[i].gameObject.activeInHierarchy, Is.True);
                Assert.That(_buttons[i].interactable, Is.True);
                Assert.That(_buttons[i].GetComponentInChildren<Text>().text, Is.EqualTo(offer.Choices[i].DisplayName));
            }

            int hits = _attack.AttackCount;
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(_attack.AttackCount, Is.EqualTo(hits));
            Assert.That(_view.IsOpen, Is.True, "The choice waits for the player.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator RapidRepeatedTapsApplyDamageUpgradeExactlyOnce()
        {
            yield return WaitForOfferInput();
            Button damage = _buttons[SlotFor(WeaponStat.Damage)];

            Tap(damage);
            Tap(damage);
            damage.onClick.Invoke();
            yield return null;

            Assert.That(_setup.Weapon.Damage, Is.EqualTo(15f));
            Assert.That(_setup.Weapon.Interval, Is.EqualTo(0.8f).Within(1e-5f));
            Assert.That(_setup.Run.UpgradesApplied, Is.EqualTo(1));
            Assert.That(_setup.Upgrades.CurrentOffer, Is.Null);
            Assert.That(_view.IsOpen, Is.False);
            Assert.That(damage.gameObject.activeInHierarchy, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            AssertSourceSwordUnchanged();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AttackSpeedChoiceChangesRuntimeIntervalAndHudOnly()
        {
            Text weaponLabel = GameObject.Find("Weapon Label").GetComponent<Text>();
            yield return WaitForOfferInput();
            string before = weaponLabel.text;

            Tap(_buttons[SlotFor(WeaponStat.AttackSpeed)]);
            yield return null;

            // Upgrade_AttackSpeed.asset authors +50%: 0.8 s / 1.5.
            Assert.That(_setup.Weapon.Interval, Is.EqualTo(0.8f / 1.5f).Within(1e-4f));
            Assert.That(_setup.Weapon.Damage, Is.EqualTo(10f));
            Assert.That(weaponLabel.text, Is.Not.EqualTo(before));
            AssertSourceSwordUnchanged();
        }

        [UnityTest]
        public IEnumerator NoNextEncounterStartsWhileTheChoiceIsOpen()
        {
            yield return WaitForOfferInput();
            Health first = _encounters.CurrentEnemy;

            yield return new WaitForSecondsRealtime(1.5f);

            Assert.That(_encounters.EncounterNumber, Is.EqualTo(1));
            Assert.That(_encounters.CurrentEnemy, Is.SameAs(first));
            Assert.That(first != null, Is.True, "The defeated Grunt stays until the player chooses.");
        }

        [UnityTest]
        public IEnumerator DamageUpgradeCarriesIntoTheNextPackAndRewardsOnlyTheNewKills()
        {
            yield return WaitForOfferInput();
            Health first = _encounters.CurrentEnemy;
            // Two swings on the mite that walked in first, each cleaving the Grunt, then four on the Grunt.
            Assert.That(_encounters.HitsTaken, Is.EqualTo(8));

            Tap(_buttons[SlotFor(WeaponStat.Damage)]);
            int swings = _attack.AttackCount;
            yield return WaitForEncounter(2);

            // Wave 2 is three Cinder Mites, the front one straight ahead of the hero.
            Health second = _encounters.CurrentEnemy;
            Assert.That(_encounters.WaveEnemyCount, Is.EqualTo(3));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first == null, Is.True, "The defeated enemy is destroyed when the next wave starts.");
            Assert.That(second.Maximum, Is.EqualTo(15f));
            Assert.That(second, Is.SameAs(_encounters.WaveEnemyAt(0)), "The front slot walks in nearest.");
            Assert.That(_targeting.Acquire(_setup.Weapon.Range), Is.Null, "The pack enters out of reach.");

            yield return WaitForClear();
            Assert.That(_attack.AttackCount - swings, Is.EqualTo(3),
                "15 damage kills the front mite and cleaves a side one for 9; the next swing kills that one and cleaves the last, and a third finishes it.");
            Assert.That(_encounters.HitsTaken, Is.EqualTo(5));
            Assert.That(_setup.Run.Experience, Is.EqualTo(14), "Only the three new kills add experience, 1 each.");
            Assert.That(_setup.Run.Level, Is.EqualTo(1), "Level 2 needs 22.");
            Assert.That(_setup.Upgrades.CurrentOffer, Is.Null);
            AssertSourceSwordUnchanged();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AttackSpeedUpgradeShortensTheNextPackCadence()
        {
            yield return WaitForOfferInput();

            Tap(_buttons[SlotFor(WeaponStat.AttackSpeed)]);
            int swings = _attack.AttackCount;
            yield return WaitForEncounter(2);
            float deadline = Time.realtimeSinceStartup + 8f;
            while (_attack.AttackCount == swings && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_attack.AttackCount, Is.GreaterThan(swings), "The mites walk into reach.");
            float firstSwing = Time.time;
            yield return WaitForClear();

            // 10 damage with a 6-damage cleave needs four swings for three 15 HP mites, three intervals from first to last.
            Assert.That(_attack.AttackCount - swings, Is.EqualTo(4));
            Assert.That(Time.time - firstSwing, Is.LessThan(3 * 0.8f - 0.3f),
                $"Expected about {3 * _setup.Weapon.Interval:0.00} s from the first swing after the attack speed upgrade.");
        }

        [UnityTest]
        public IEnumerator TapsBeforeTheInputDelayAreIgnored()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (_setup.Upgrades.CurrentOffer == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_setup.Upgrades.CurrentOffer, Is.Not.Null);

            _buttons[0].onClick.Invoke();
            yield return null;
            Assert.That(_setup.Run.UpgradesApplied, Is.Zero);
            Assert.That(_view.IsOpen, Is.True);
        }

        private IEnumerator WaitForOfferInput()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (_setup.Upgrades.CurrentOffer == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(_setup.Upgrades.CurrentOffer, Is.Not.Null, "Clearing the first pack must open an upgrade offer.");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        private IEnumerator WaitForEncounter(int number)
        {
            float deadline = Time.realtimeSinceStartup + 6f;
            while (_encounters.EncounterNumber < number && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(_encounters.EncounterNumber, Is.EqualTo(number), "The next encounter must start after the choice.");
            // Let the previous enemy's deferred Destroy complete.
            yield return null;
        }

        private IEnumerator WaitForClear()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
            while (!_encounters.IsCleared && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(_encounters.IsCleared, Is.True, "The upgraded hero must clear the encounter.");
        }

        private int SlotFor(WeaponStat stat)
        {
            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                if (offer.Choices[i].Stat == stat)
                    return i;
            }

            Assert.Fail($"No {stat} upgrade was offered.");
            return -1;
        }

        private static void Tap(Button button) =>
            ExecuteEvents.Execute(button.gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerClickHandler);

        private static void AssertSourceSwordUnchanged()
        {
#if UNITY_EDITOR
            var sword = AssetDatabase.LoadAssetAtPath<WeaponDefinition>("Assets/_Project/Data/Weapons/Weapon_Sword.asset");
            Assert.That(sword.Damage, Is.EqualTo(10f));
            Assert.That(sword.Interval, Is.EqualTo(0.8f));
            Assert.That(sword.Range, Is.EqualTo(1.8f));
#endif
        }
    }
}
