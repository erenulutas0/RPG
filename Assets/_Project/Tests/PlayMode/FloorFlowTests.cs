using System.Collections;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Cryptforge.Tests
{
    // Walks Floor_EmberHalls quickly by applying lethal damage to each enemy and resolving choices through RunChoices.
    // Card UI itself is covered by UpgradeFlowTests.
    public sealed class FloorFlowTests
    {
        private const float Lethal = 100000f;
        private CombatSetup _setup;
        private Health _hero;
        private EncounterController _encounters;
        private RunResultView _result;

        [UnitySetUp]
        public IEnumerator LoadGameplay()
        {
            TestProfile.Begin();
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _encounters = Object.FindFirstObjectByType<EncounterController>();
            _result = Object.FindFirstObjectByType<RunResultView>();
        }

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            Scene gameplay = SceneManager.GetActiveScene();
            Scene empty = SceneManager.CreateScene("Floor Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator FloorFollowsTheAuthoredRoomsToTheCheckpointAndExtractBanksTheGold()
        {
            var seen = new List<string>();
            float deadline = Time.realtimeSinceStartup + 40f;
            while (!_encounters.IsFloorCleared && Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                ChoicePrompt prompt = _setup.Choices.Current;
                if (_encounters.IsInNonCombatRoom && prompt != null && prompt.Kind == ChoiceKind.Forge)
                {
                    seen.Add($"{_encounters.RoomNumber}:{_encounters.CurrentRoom.DisplayName}:forge");
                    Assert.That(_encounters.CurrentEnemy, Is.Null);
                    Assert.That(Time.timeScale, Is.Zero, "The forge visit pauses the floor.");
                    Assert.That(Label("Floor Label"), Does.Contain("Room 4/6").And.Contain("The Forge"));
                    _setup.Choices.TrySelect(prompt, 0);
                }
                else if (!_encounters.IsInNonCombatRoom && _encounters.CurrentEnemy != null && _encounters.CurrentEnemy.IsAlive)
                {
                    seen.Add($"{_encounters.RoomNumber}.{_encounters.WaveNumber}:{_encounters.CurrentRoom.DisplayName}:{_encounters.CurrentDefinition.DisplayName}");
                    _encounters.CurrentEnemy.ApplyDamage(new DamageContext(Lethal));
                }
                yield return null;
            }

            Assert.That(seen, Is.EqualTo(new[]
            {
                "1.1:Ember Hall:Grunt", "1.2:Ember Hall:Runner", "2.1:Cinder Walk:Runner", "2.2:Cinder Walk:Grunt",
                "3.1:Slag Gate:Tank", "4:The Forge:forge", "5.1:Captain's Post:Grunt Captain",
                "6.1:Warden's Crucible:Forge Warden"
            }));
            yield return null;

            // The boss level-up comes first; the Extract/Descend decision waits behind it.
            ChoosePendingUpgrades();
            ChoicePrompt checkpoint = _setup.Choices.Current;
            Assert.That(_setup.Run.HasEnded, Is.False, "Floor 1 is not the end of the Descent.");
            Assert.That(_encounters.RoomsCleared, Is.EqualTo(6));
            Assert.That(checkpoint, Is.Not.Null);
            Assert.That(checkpoint.Kind, Is.EqualTo(ChoiceKind.Checkpoint));
            Assert.That(Time.timeScale, Is.Zero, "The checkpoint pauses the run.");
            Assert.That(Label("Choice Title"), Is.EqualTo("Checkpoint: extract or descend?"));
            Assert.That(checkpoint.Cards[0].Name, Is.EqualTo("Extract"));
            Assert.That(checkpoint.Cards[0].Description, Is.EqualTo("Bank all 96 gold and end the run"));
            Assert.That(checkpoint.Cards[1].Name, Is.EqualTo("Descend"));
            Assert.That(checkpoint.Cards[1].Description, Is.EqualTo("Secure 96 gold. Quicksilver Vaults: +20% enemy damage, +50% gold"));
            Assert.That(Label("Gold Label"), Is.EqualTo("Gold 96  (96 at risk)"));
            Assert.That(_result.IsOpen, Is.False);

            Assert.That(_setup.Choices.TrySelect(checkpoint, 0), Is.True);
            Assert.That(_setup.Choices.TrySelect(checkpoint, 1), Is.False, "The checkpoint decision is taken once.");
            yield return null;

            Assert.That(_setup.Run.Outcome, Is.EqualTo(RunOutcome.Extracted));
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(96));
            Assert.That(_setup.Run.GoldLost, Is.Zero);
            Assert.That(_encounters.FloorNumber, Is.EqualTo(1));
            Assert.That(_setup.Choices.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_result.IsOpen, Is.True);
            Assert.That(Label("Result Title"), Is.EqualTo("Extracted"));
            Assert.That(Label("Cause Label"), Does.Contain("escaped").And.Contain("Ember Halls"));
            Assert.That(Label("Progress Label"), Does.Contain("Floor 1").And.Contain("6 rooms"));
            Assert.That(Label("Result Gold Label"), Is.EqualTo("Gold banked: 96"));
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 96: Second Wind is ready to forge"));
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(96), "Extracting banks the run's gold in the saved profile.");
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator MendRestoresFortyPercentHealthAndTheFloorContinues()
        {
            yield return AdvanceToRoom(4);
            _hero.ApplyDamage(new DamageContext(70f));
            float wounded = _hero.Current;
            ChoicePrompt prompt = _setup.Choices.Current;
            Assert.That(prompt.Kind, Is.EqualTo(ChoiceKind.Forge));
            Assert.That(prompt.Cards[0].Name, Is.EqualTo("Mend"));

            Assert.That(_setup.Choices.TrySelect(prompt, 0), Is.True);
            Assert.That(_setup.Choices.TrySelect(prompt, 0), Is.False);
            Assert.That(_hero.Current, Is.EqualTo(Mathf.Min(100f, wounded + 40f)));
            Assert.That(_setup.Choices.IsOpen, Is.False);

            yield return AdvanceToRoom(5);
            Assert.That(_encounters.CurrentDefinition.DisplayName, Is.EqualTo("Grunt Captain"));
            Assert.That(_encounters.CurrentEnemy.Maximum, Is.EqualTo(140f));
        }

        [UnityTest]
        public IEnumerator TemperOpensOneExtraUpgradeChoice()
        {
            yield return AdvanceToRoom(4);
            int applied = _setup.Run.UpgradesApplied;
            float heroHealth = _hero.Current;

            _setup.Choices.TrySelect(_setup.Choices.Current, 1);

            Assert.That(_setup.Choices.Current, Is.Not.Null);
            Assert.That(_setup.Choices.Current.Kind, Is.EqualTo(ChoiceKind.Upgrade));
            Assert.That(Time.timeScale, Is.Zero, "The hand-over to the bonus upgrade keeps the game paused.");
            Assert.That(Label("Choice Title"), Does.Contain("Level up"));
            _setup.Choices.TrySelect(_setup.Choices.Current, 0);
            Assert.That(_setup.Run.UpgradesApplied, Is.EqualTo(applied + 1));
            Assert.That(_setup.Run.BonusUpgrades, Is.EqualTo(1));
            Assert.That(_hero.Current, Is.EqualTo(heroHealth));
        }

        [UnityTest]
        public IEnumerator WardenEnragesAtHalfHealthThenItsFallOpensTheCheckpoint()
        {
            yield return AdvanceToRoom(6);
            Health warden = _encounters.CurrentEnemy;
            AttackController hammer = warden.GetComponent<AttackController>();
            Assert.That(_encounters.CurrentDefinition.DisplayName, Is.EqualTo("Forge Warden"));
            Assert.That(hammer.Weapon.Interval, Is.EqualTo(1.8f).Within(1e-4f));

            warden.ApplyDamage(new DamageContext(warden.Current - 151f));
            Assert.That(_encounters.IsCurrentEnemyEnraged, Is.False);
            warden.ApplyDamage(new DamageContext(2f));

            Assert.That(_encounters.IsCurrentEnemyEnraged, Is.True);
            Assert.That(hammer.Weapon.Interval, Is.EqualTo(0.9f).Within(1e-4f), "+100% attack speed halves the interval.");
            Assert.That(Label("Status Label"), Does.Contain("enraged"));

            warden.ApplyDamage(new DamageContext(Lethal));
            yield return null;
            Assert.That(_setup.Run.HasEnded, Is.False);
            ChoosePendingUpgrades();
            Assert.That(_setup.Choices.Current.Kind, Is.EqualTo(ChoiceKind.Checkpoint));
            Assert.That(_result.IsOpen, Is.False);
        }

        private IEnumerator AdvanceToRoom(int roomNumber)
        {
            float deadline = Time.realtimeSinceStartup + 30f;
            while (Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                ChoicePrompt prompt = _setup.Choices.Current;
                bool atForge = _encounters.IsInNonCombatRoom && prompt != null && prompt.Kind == ChoiceKind.Forge;
                bool atLiveEnemy = !_encounters.IsInNonCombatRoom && _encounters.CurrentEnemy != null && _encounters.CurrentEnemy.IsAlive;
                if (_encounters.RoomNumber == roomNumber && (atForge || atLiveEnemy))
                    yield break;

                if (atForge)
                    _setup.Choices.TrySelect(prompt, 0);
                else if (atLiveEnemy)
                    _encounters.CurrentEnemy.ApplyDamage(new DamageContext(Lethal));
                yield return null;
            }

            Assert.Fail($"Room {roomNumber} was not reached; stopped in room {_encounters.RoomNumber}.");
        }

        private void ChoosePendingUpgrades()
        {
            while (_setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
        }

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;
    }
}
