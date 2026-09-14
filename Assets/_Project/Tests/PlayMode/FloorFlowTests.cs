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
    // Walks Floor_EmberHalls quickly by applying lethal damage to each pack and resolving choices through RunChoices.
    // Card UI itself is covered by UpgradeFlowTests.
    public sealed class FloorFlowTests
    {
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
        public IEnumerator FloorFollowsTheAuthoredPacksToTheCheckpointAndExtractBanksTheGold()
        {
            // The first wave spawned during scene load, before this test could subscribe.
            var seen = new List<string> { PackTestUtility.Describe(_encounters) };
            _encounters.EncounterStarted += () => seen.Add(PackTestUtility.Describe(_encounters));
            bool forgeSeen = false;
            float deadline = Time.realtimeSinceStartup + 60f;
            while (!_encounters.IsFloorCleared && Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                ChoicePrompt prompt = _setup.Choices.Current;
                if (_encounters.IsInNonCombatRoom && prompt != null && prompt.Kind == ChoiceKind.Forge)
                {
                    if (!forgeSeen)
                        seen.Add($"{_encounters.RoomNumber}:{_encounters.CurrentRoom.DisplayName}:forge");
                    forgeSeen = true;
                    Assert.That(_encounters.CurrentEnemy, Is.Null);
                    Assert.That(Time.timeScale, Is.Zero, "The forge visit pauses the floor.");
                    Assert.That(Label("Floor Label"), Does.Contain("Room 4/6").And.Contain("The Forge"));
                    _setup.Choices.TrySelect(prompt, 0);
                }
                else if (!_encounters.IsInNonCombatRoom)
                {
                    PackTestUtility.KillWave(_encounters);
                }
                yield return null;
            }

            Assert.That(seen, Is.EqualTo(new[]
            {
                "1.1:Ember Hall:Grunt+Cinder Mite", "1.2:Ember Hall:Cinder Mite+Cinder Mite+Cinder Mite",
                "1.3:Ember Hall:Grunt+Cinder Mite+Cinder Mite+Cinder Mite",
                "2.1:Cinder Walk:Runner+Cinder Mite+Cinder Mite", "2.2:Cinder Walk:Cinder Mite+Cinder Mite+Cinder Mite+Cinder Mite",
                "2.3:Cinder Walk:Tank+Cinder Mite+Cinder Mite",
                "3.1:Slag Gate:Grunt+Runner+Cinder Mite+Cinder Mite", "3.2:Slag Gate:Tank+Cinder Mite+Cinder Mite",
                "4:The Forge:forge", "5.1:Captain's Post:Grunt Captain+Grunt+Cinder Mite+Cinder Mite", "6.1:Warden's Crucible:Forge Warden"
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
            Assert.That(checkpoint.Cards[0].Description, Is.EqualTo("Bank all 116 gold and end the run"));
            Assert.That(checkpoint.Cards[1].Name, Is.EqualTo("Descend"));
            Assert.That(checkpoint.Cards[1].Description, Is.EqualTo("Secure 116 gold. Quicksilver Vaults: +20% enemy damage, +50% gold"));
            Assert.That(Label("Gold Label"), Is.EqualTo("Gold 116  (116 at risk)"));
            Assert.That(_result.IsOpen, Is.False);

            Assert.That(_setup.Choices.TrySelect(checkpoint, 0), Is.True);
            Assert.That(_setup.Choices.TrySelect(checkpoint, 1), Is.False, "The checkpoint decision is taken once.");
            yield return null;

            Assert.That(_setup.Run.Outcome, Is.EqualTo(RunOutcome.Extracted));
            Assert.That(_setup.Run.GoldBanked, Is.EqualTo(116));
            Assert.That(_setup.Run.GoldLost, Is.Zero);
            Assert.That(_encounters.FloorNumber, Is.EqualTo(1));
            Assert.That(_setup.Choices.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(_result.IsOpen, Is.True);
            Assert.That(Label("Result Title"), Is.EqualTo("Extracted"));
            Assert.That(Label("Cause Label"), Does.Contain("escaped").And.Contain("Ember Halls"));
            Assert.That(Label("Progress Label"), Does.Contain("Floor 1").And.Contain("6 rooms"));
            Assert.That(Label("Result Gold Label"), Is.EqualTo("Gold banked: 116"));
            Assert.That(Label("Forge Hint Label"), Is.EqualTo("Forge gold 116: Second Wind is ready to forge"));
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(116), "Extracting banks the run's gold in the saved profile.");
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator PacksEnterInFormationWalkInAndTheHeroFacesTheNearestFirst()
        {
            // The test deals every blow, so the hero's own cleaves cannot change the order under test.
            _hero.GetComponent<AttackController>().enabled = false;
            var spawned = new List<Vector3>();
            _encounters.EncounterStarted += () =>
            {
                if (_encounters.RoomNumber != 1 || _encounters.WaveNumber != 2)
                    return;
                for (int i = 0; i < _encounters.WaveEnemyCount; i++)
                    spawned.Add(_encounters.WaveEnemyAt(i).transform.position);
            };
            yield return AdvanceToWave(1, 2);
            Assert.That(_encounters.WaveEnemyCount, Is.EqualTo(3));
            Health centre = _encounters.WaveEnemyAt(0);
            Health left = _encounters.WaveEnemyAt(1);
            Health right = _encounters.WaveEnemyAt(2);

            // The front slot enters 6 floor units from the hero and the other two 0.8 behind it and 1.4 to each side; depth
            // shows at half length on screen.
            Assert.That(spawned.Count, Is.EqualTo(3));
            AssertPosition(spawned[0], 0f, 3f);
            AssertPosition(spawned[1], -1.4f, 3.4f);
            AssertPosition(spawned[2], 1.4f, 3.4f);
            Assert.That(_encounters.CurrentEnemy, Is.SameAs(centre));
            Assert.That(Label("Enemy Label"), Does.StartWith("Cinder Mite").And.Contain("(+2 more)"));

            yield return new WaitForSeconds(0.5f);
            Assert.That(centre.transform.position.x, Is.EqualTo(0f), "The front mite walks straight at the hero.");
            Assert.That(centre.transform.position.y, Is.LessThan(spawned[0].y));
            Assert.That(left.transform.position.x, Is.GreaterThan(spawned[1].x), "The side mites close in from their side.");
            Assert.That(left.transform.position.y, Is.LessThan(spawned[1].y));
            Assert.That(right.transform.position.x, Is.EqualTo(-left.transform.position.x), "Mirrored slots walk mirrored paths.");
            Assert.That(right.transform.position.y, Is.EqualTo(left.transform.position.y));
            Assert.That(_encounters.CurrentEnemy, Is.SameAs(centre));

            centre.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(_encounters.CurrentEnemy, Is.SameAs(left), "At equal distance the earlier slot is next.");
            Assert.That(_encounters.AliveEnemyCount, Is.EqualTo(2));
            Assert.That(_encounters.IsCleared, Is.False);
            Assert.That(Label("Enemy Label"), Does.Contain("(+1 more)"));

            left.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            right.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(_encounters.IsCleared, Is.True, "The wave clears when its last enemy falls.");
            Assert.That(_encounters.CurrentEnemy, Is.SameAs(right));
            Assert.That(Label("Status Label"), Does.StartWith("3 enemies defeated in"));
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
            Health captain = _encounters.WaveEnemyAt(0);
            Assert.That(_encounters.DefinitionOf(captain).DisplayName, Is.EqualTo("Grunt Captain"));
            Assert.That(captain.Maximum, Is.EqualTo(140f));
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

            warden.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            yield return null;
            Assert.That(_setup.Run.HasEnded, Is.False);
            ChoosePendingUpgrades();
            Assert.That(_setup.Choices.Current.Kind, Is.EqualTo(ChoiceKind.Checkpoint));
            Assert.That(_result.IsOpen, Is.False);
        }

        private static void AssertPosition(Vector3 position, float x, float y)
        {
            Assert.That(position.x, Is.EqualTo(x).Within(1e-4f));
            Assert.That(position.y, Is.EqualTo(y).Within(1e-4f));
            Assert.That(position.z, Is.Zero);
        }

        private IEnumerator AdvanceToRoom(int roomNumber) => AdvanceToWave(roomNumber, 1);

        // Stops at the forge prompt of a forge room, or when the given wave of the room has a live enemy.
        private IEnumerator AdvanceToWave(int roomNumber, int waveNumber)
        {
            float deadline = Time.realtimeSinceStartup + 45f;
            while (Time.realtimeSinceStartup < deadline)
            {
                ChoosePendingUpgrades();
                ChoicePrompt prompt = _setup.Choices.Current;
                bool atForge = _encounters.IsInNonCombatRoom && prompt != null && prompt.Kind == ChoiceKind.Forge;
                bool atLiveWave = !_encounters.IsInNonCombatRoom && PackTestUtility.AnyAlive(_encounters);
                if (_encounters.RoomNumber == roomNumber && (atForge || (atLiveWave && _encounters.WaveNumber == waveNumber)))
                    yield break;

                if (atForge)
                    _setup.Choices.TrySelect(prompt, 0);
                else if (atLiveWave)
                    PackTestUtility.KillWave(_encounters);
                yield return null;
            }

            Assert.Fail($"Room {roomNumber} wave {waveNumber} was not reached; stopped in room {_encounters.RoomNumber} wave {_encounters.WaveNumber}.");
        }

        private void ChoosePendingUpgrades()
        {
            while (_setup.Choices.Current != null && _setup.Choices.Current.Kind == ChoiceKind.Upgrade)
                _setup.Choices.TrySelect(_setup.Choices.Current, 0);
        }

        private static string Label(string name) => GameObject.Find(name).GetComponent<Text>().text;
    }
}
