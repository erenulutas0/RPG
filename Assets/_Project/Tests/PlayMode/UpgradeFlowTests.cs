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
            WriteRunSeed(CardSeed);
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
            Button damage = _buttons[SlotFor(UpgradeStat.Damage)];

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

            int slot = SlotFor(UpgradeStat.AttackSpeed);
            // The card comes at a tier now, so the expected cadence is read from the card that was actually offered
            // rather than from the asset's common magnitude.
            UpgradeOffer offered = _setup.Upgrades.CurrentOffer;
            float granted = offered.Choices[slot].ModifierFor(offered.RarityAt(slot)).Amount;
            Tap(_buttons[slot]);
            yield return null;

            Assert.That(granted, Is.GreaterThanOrEqualTo(0.5f), "Quickened Grip is +50% at its commonest.");
            Assert.That(_setup.Weapon.Interval, Is.EqualTo(0.8f / (1f + granted)).Within(1e-4f));
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
            // Two swings on the mite from the right corner, then five on the Grunt from the far corner.
            Assert.That(_encounters.HitsTaken, Is.EqualTo(7));

            Tap(_buttons[SlotFor(UpgradeStat.Damage)]);
            int swings = _attack.AttackCount;
            yield return WaitForEncounter(2);

            // Wave 2 is three Cinder Mites from the right, near and left corners, all 5 floor units out.
            Health second = _encounters.CurrentEnemy;
            Assert.That(_encounters.WaveEnemyCount, Is.EqualTo(3));
            Assert.That(second, Is.Not.SameAs(first));
            Assert.That(first == null, Is.True, "The defeated enemy is destroyed when the next wave starts.");
            Assert.That(second.Maximum, Is.EqualTo(15f));
            Assert.That(second, Is.SameAs(_encounters.WaveEnemyAt(0)), "At equal distance the earlier slot is the target.");
            Assert.That(_targeting.Acquire(_setup.Weapon.Range), Is.Null, "The pack enters out of reach.");

            yield return WaitForClear();
            Assert.That(_attack.AttackCount - swings, Is.EqualTo(3),
                "15 damage kills the right mite; the next swing kills the near one and cleaves the left one for 9; a third finishes it.");
            Assert.That(_encounters.HitsTaken, Is.EqualTo(4));
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

            Tap(_buttons[SlotFor(UpgradeStat.AttackSpeed)]);
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

        // The pool holds five cards and a level-up shows two of them, drawn from the run's seed, so the card a test
        // needs is not certain to be in the first offer. Pending offers are taken until it appears; a run earns several
        // level-ups before this is called, and the test fails loudly if it never comes.
        // Measured: this seed offers Tempered Edge in the first level-up and Quickened Grip within two, so these tests
        // read a card rather than a draw. What an arbitrary seed offers is DescentSeedTests' business.
        private const int CardSeed = 3;

        private static void WriteRunSeed(int seed)
        {
            string path = DevelopmentStart.RunSeedPath;
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            System.IO.File.WriteAllText(path, seed.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private int SlotFor(UpgradeStat stat)
        {
            for (int guard = 0; guard < 16; guard++)
            {
                UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
                Assert.That(offer, Is.Not.Null, $"No {stat} upgrade was offered before the offers ran out.");
                for (int i = 0; i < offer.Choices.Count; i++)
                {
                    if (offer.Choices[i].Stat == stat)
                        return i;
                }

                Assert.That(_setup.Upgrades.TrySelect(offer, 0), Is.True, "Taking a card to reach the next offer.");
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
            Assert.That(sword.Range, Is.EqualTo(2.1f));
#endif
        }
    }
}
