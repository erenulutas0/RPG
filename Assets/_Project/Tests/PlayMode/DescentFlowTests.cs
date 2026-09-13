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
    // Drives the Descent through Floor_EmberHalls into Floor_QuicksilverVaults by applying lethal damage to each pack and
    // resolving choices through RunChoices: Mend at every forge, and the scripted decision at the checkpoint.
    public sealed class DescentFlowTests
    {
        private const int CheckpointDescend = 1;
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
            Scene empty = SceneManager.CreateScene("Descent Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator DescendingSecuresTheGoldAndScalesTheNextFloor()
        {
            // The test deals every blow, so exact health and gold are not disturbed by the hero's cleaves.
            _hero.GetComponent<AttackController>().enabled = false;
            yield return AdvanceToCheckpoint();
            _hero.ApplyDamage(new DamageContext(30f));
            float wounded = _hero.Current;

            Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, CheckpointDescend), Is.True);
            Assert.That(_setup.Run.HasEnded, Is.False);
            Assert.That(_setup.Run.SecuredGold, Is.EqualTo(109));
            Assert.That(_setup.Run.UnsecuredGold, Is.Zero);
            Assert.That(_setup.Choices.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_encounters.FloorNumber, Is.EqualTo(2));
            Assert.That(_encounters.IsDescending, Is.True);
            Assert.That(Label("Gold Label"), Is.EqualTo("Gold 109  (0 at risk)"));
            Assert.That(Label("Floor Label"), Does.StartWith("Floor 2"));
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(109), "Secured gold is banked in the saved profile as soon as the hero descends.");
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));
            yield return null;
            Assert.That(_hero.Current, Is.EqualTo(wounded), "Descending does not heal.");

            yield return AdvanceToFloorRoom(2, 1);
            Health grunt = _encounters.WaveEnemyAt(0);
            Health mite = _encounters.WaveEnemyAt(1);
            AttackController strike = grunt.GetComponent<AttackController>();
            Assert.That(_encounters.CurrentRoom.DisplayName, Is.EqualTo("Mercury Stair"));
            Assert.That(_encounters.CurrentDefinition.DisplayName, Is.EqualTo("Grunt"));
            Assert.That(grunt.Maximum, Is.EqualTo(70f).Within(1e-3f), "Floor 2 health tier: 50 x 1.4.");
            Assert.That(mite.Maximum, Is.EqualTo(21f).Within(1e-3f));
            Assert.That(strike.Weapon.Damage, Is.EqualTo(9f).Within(1e-4f), "6 x 1.25 tier x 1.2 Cursed Gold.");
            Assert.That(_encounters.GoldRewardOf(grunt), Is.EqualTo(8), "5 gold +50%, rounded away from zero.");
            Assert.That(_encounters.GoldRewardOf(mite), Is.EqualTo(2));
            Assert.That(Label("Floor Label"), Does.Contain("Floor 2").And.Contain("Room 1/6").And.Contain("Mercury Stair"));
            Assert.That(Label("Enemy Label"), Is.EqualTo("Grunt  |  70 / 70 HP  (+1 more)"));

            grunt.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(_setup.Run.Gold, Is.EqualTo(117));
            Assert.That(_setup.Run.UnsecuredGold, Is.EqualTo(8));
            Assert.That(Label("Gold Label"), Is.EqualTo("Gold 117  (8 at risk)"));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator DyingOnTheNextFloorLosesHalfTheUnsecuredGoldAndNamesTheLastAttacker()
        {
            _hero.GetComponent<AttackController>().enabled = false;
            yield return AdvanceToCheckpoint();
            _setup.Choices.TrySelect(_setup.Choices.Current, CheckpointDescend);
            yield return AdvanceToFloorRoom(2, 1);
            _encounters.WaveEnemyAt(0).ApplyDamage(new DamageContext(PackTestUtility.Lethal));

            // The Grunt fell before it could strike; the Cinder Mite beside it bites on its first frame.
            float deadline = Time.realtimeSinceStartup + 3f;
            while ((_setup.LastAttacker == null || _setup.LastAttacker.DisplayName != "Cinder Mite") && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_setup.LastAttacker.DisplayName, Is.EqualTo("Cinder Mite"));

            _hero.ApplyDamage(new DamageContext(1000f));
            yield return null;

            Assert.That(_setup.Run.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(_setup.Run.GoldLost, Is.EqualTo(4), "Half of the 8 unsecured gold, rounded down.");
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(113));
            Assert.That(_result.IsOpen, Is.True);
            Assert.That(Label("Result Title"), Is.EqualTo("Defeated"));
            Assert.That(Label("Cause Label"), Does.Contain("Cinder Mite").And.Contain("Mercury Stair"));
            Assert.That(Label("Progress Label"), Does.Contain("Floor 2").And.Contain("6 rooms"));
            Assert.That(Label("Result Gold Label"), Is.EqualTo("Gold banked: 113  |  lost: 4"));
            Assert.That(TestProfile.ReadSaved().Gold, Is.EqualTo(113), "The 109 banked at the checkpoint plus the 4 kept.");
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 113: Second Wind is ready to forge"));
        }

        [UnityTest]
        public IEnumerator ClearingBothFloorsCompletesTheDescent()
        {
            var floorTwo = new List<string>();
            _encounters.EncounterStarted += () =>
            {
                if (_encounters.FloorNumber == 2)
                    floorTwo.Add(PackTestUtility.Describe(_encounters));
            };
            float deadline = Time.realtimeSinceStartup + 120f;
            while (!_setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                ChoicePrompt prompt = _setup.Choices.Current;
                if (prompt != null && prompt.Kind == ChoiceKind.Checkpoint)
                {
                    _setup.Choices.TrySelect(prompt, CheckpointDescend);
                }
                else if (_encounters.IsInNonCombatRoom && prompt != null && prompt.Kind == ChoiceKind.Forge)
                {
                    if (_encounters.FloorNumber == 2)
                        floorTwo.Add($"{_encounters.RoomNumber}:{_encounters.CurrentRoom.DisplayName}:forge");
                    _setup.Choices.TrySelect(prompt, 0);
                }
                else if (!_encounters.IsInNonCombatRoom)
                {
                    PackTestUtility.KillWave(_encounters);
                }
                yield return null;
            }

            Assert.That(floorTwo, Is.EqualTo(new[]
            {
                "1.1:Mercury Stair:Grunt+Cinder Mite", "1.2:Mercury Stair:Cinder Mite+Cinder Mite+Cinder Mite",
                "2.1:Cold Crucible:Tank+Cinder Mite", "2.2:Cold Crucible:Runner+Cinder Mite+Cinder Mite",
                "3.1:Vault Gate:Grunt+Cinder Mite+Cinder Mite", "4:The Deep Forge:forge",
                "5.1:Sentry Hall:Grunt Captain+Cinder Mite", "6.1:Warden's Vault:Forge Warden"
            }));
            yield return null;

            Assert.That(_setup.Run.Outcome, Is.EqualTo(RunOutcome.Victory));
            Assert.That(_encounters.TotalRoomsCleared, Is.EqualTo(12));
            Assert.That(_setup.Run.Gold, Is.EqualTo(270), "Floor 1 pays 109 gold and floor 2 pays 161 with Cursed Gold.");
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(270));
            Assert.That(_setup.Run.GoldLost, Is.Zero);
            Assert.That(_setup.Choices.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_result.IsOpen, Is.True);
            Assert.That(Label("Result Title"), Is.EqualTo("Descent complete"));
            Assert.That(Label("Cause Label"), Does.Contain("Forge Warden").And.Contain("Quicksilver Vaults"));
            Assert.That(Label("Progress Label"), Does.Contain("Floor 2").And.Contain("12 rooms"));
            Assert.That(Label("Result Gold Label"), Is.EqualTo("Gold banked: 270"));
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(270));
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(2));
            LogAssert.NoUnexpectedReceived();
        }

        // Clears floor 1 with Mend at the forge and resolves the boss level-up, leaving the checkpoint open.
        private IEnumerator AdvanceToCheckpoint()
        {
            float deadline = Time.realtimeSinceStartup + 60f;
            while (Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                ChoicePrompt prompt = _setup.Choices.Current;
                if (prompt != null && prompt.Kind == ChoiceKind.Checkpoint)
                    yield break;

                if (_encounters.IsInNonCombatRoom && prompt != null && prompt.Kind == ChoiceKind.Forge)
                    _setup.Choices.TrySelect(prompt, 0);
                else if (!_encounters.IsInNonCombatRoom)
                    PackTestUtility.KillWave(_encounters);
                yield return null;
            }

            Assert.Fail($"The floor 1 checkpoint was not reached; stopped in room {_encounters.RoomNumber}.");
        }
        private IEnumerator AdvanceToFloorRoom(int floorNumber, int roomNumber)
        {
            float deadline = Time.realtimeSinceStartup + 20f;
            while (Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                bool atLiveEnemy = !_encounters.IsInNonCombatRoom && _encounters.CurrentEnemy != null && _encounters.CurrentEnemy.IsAlive;
                if (_encounters.FloorNumber == floorNumber && _encounters.RoomNumber == roomNumber && atLiveEnemy)
                    yield break;
                yield return null;
            }

            Assert.Fail($"Floor {floorNumber} room {roomNumber} was not reached; stopped on floor {_encounters.FloorNumber} room {_encounters.RoomNumber}.");
        }

        private void ChoosePendingUpgrades()
        {
            while (_setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
        }

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;
    }
}
