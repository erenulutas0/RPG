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
        public IEnumerator DamageUpgradeCarriesIntoTheNextEncounterAndRewardsOnlyTheNewKill()
        {
            yield return WaitForOfferInput();
            Health first = _encounters.CurrentEnemy;
            Assert.That(_encounters.HitsTaken, Is.EqualTo(5));

            Tap(_buttons[SlotFor(WeaponStat.Damage)]);
            yield return WaitForEncounter(2);

            Health second = _encounters.CurrentEnemy;
            float maximum = _encounters.CurrentDefinition.MaximumHealth;
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first == null, Is.True, "The defeated enemy is destroyed when the next encounter starts.");
            Assert.That(second.Maximum, Is.EqualTo(maximum));
            Assert.That(second.IsAlive, Is.True);
            Assert.That(_targeting.Acquire(3f), Is.SameAs(second));

            yield return WaitForClear();
            Assert.That(_encounters.HitsTaken, Is.EqualTo(Mathf.CeilToInt(maximum / 15f)),
                "Every hit on the next enemy uses the upgraded 15 damage.");
            Assert.That(_setup.Run.Experience, Is.EqualTo(20), "Only the new kill adds experience.");
            Assert.That(_setup.Run.Level, Is.EqualTo(2));
            Assert.That(_setup.Upgrades.CurrentOffer, Is.Not.Null, "The second kill opens the next choice.");
            AssertSourceSwordUnchanged();
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AttackSpeedUpgradeShortensTheNextEncounterCadence()
        {
            yield return WaitForOfferInput();

            Tap(_buttons[SlotFor(WeaponStat.AttackSpeed)]);
            yield return WaitForEncounter(2);
            float maximum = _encounters.CurrentDefinition.MaximumHealth;
            yield return WaitForClear();

            int hits = Mathf.CeilToInt(maximum / 10f);
            Assert.That(_encounters.HitsTaken, Is.EqualTo(hits));
            // Hits land every _setup.Weapon.Interval; the unupgraded cadence would need (hits - 1) × 0.8 s.
            Assert.That(_encounters.Elapsed, Is.LessThan((hits - 1) * 0.8f - 0.15f),
                $"Expected about {(hits - 1) * _setup.Weapon.Interval:0.00} s after the attack speed upgrade.");
        }

        [UnityTest]
        public IEnumerator TapsBeforeTheInputDelayAreIgnored()
        {
            float deadline = Time.realtimeSinceStartup + 8f;
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
            float deadline = Time.realtimeSinceStartup + 8f;
            while (_setup.Upgrades.CurrentOffer == null && Time.realtimeSinceStartup < deadline)
                yield return null;

            Assert.That(_setup.Upgrades.CurrentOffer, Is.Not.Null, "Killing the Grunt must open an upgrade offer.");
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
            Assert.That(sword.Range, Is.EqualTo(3f));
#endif
        }
    }
}
