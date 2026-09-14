using System;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Economy;
using Cryptforge.Progression;
using Cryptforge.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Cryptforge.Core
{
    // Scene composition and run lifecycle: which services exist, and what ends or continues the run.
    // Combat and presentation own their own updates/events.
    public sealed class CombatSetup : MonoBehaviour
    {
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EconomyConfig _economy;
        [SerializeField] private UpgradeDefinition[] _upgrades;
        [SerializeField] private RelicDefinition[] _relics;
        // Hero weapons sold in the Relic Forge, in card order; the hero's starting weapon must be one of them.
        [SerializeField] private WeaponDefinition[] _weapons;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private Health _hero;
        [SerializeField] private AttackController _attack;
        [SerializeField] private AbilityController _ability;
        [SerializeField] private RelicBehaviour _relicBehaviour;
        [SerializeField] private EncounterController _encounters;
        private RewardService _rewards;
        private ProfileStore _profileStore;
        private bool _timeFrozen;
        private bool _restarting;

        public RunState Run { get; private set; }
        public WeaponRuntime Weapon { get; private set; }
        // The hero's active ability this run, from the hero definition.
        public AbilityRuntime Ability { get; private set; }
        public UpgradeService Upgrades { get; private set; }
        public ForgeService Forge { get; private set; }
        public CheckpointService Checkpoint { get; private set; }
        public RunChoices Choices { get; private set; }
        public RunPause Pause { get; private set; }
        public PlayerProfile Profile { get; private set; }
        public RelicShop Relics { get; private set; }
        public WeaponShop Weapons { get; private set; }
        // The weapon the hero carries this run: the profile's equipped weapon, or the starting weapon.
        public WeaponDefinition HeroWeapon { get; private set; }
        public RunBank Bank { get; private set; }
        // Null when no relic is equipped.
        public RelicRuntime Relic { get; private set; }
        // The enemy whose hit last damaged the hero; after a defeat, the killer named on the result screen.
        public EnemyDefinition LastAttacker { get; private set; }

        private void Awake()
        {
            if (_heroDefinition == null || _economy == null || _text == null || _hero == null || _attack == null ||
                _ability == null || _relicBehaviour == null || _encounters == null || _heroDefinition.StartingWeapon == null ||
                _heroDefinition.Ability == null ||
                !AllPresent(_upgrades) || !AllPresent(_relics) || !AllPresent(_weapons) ||
                Array.IndexOf(_weapons, _heroDefinition.StartingWeapon) < 0)
            {
                Debug.LogError("CombatSetup is missing required scene or definition references.", this);
                enabled = false;
                return;
            }

            if (!ForgeCatalog.TryCreate(_relics, _weapons, out RelicOption[] relics, out WeaponOption[] weapons, out string catalogError))
            {
                Debug.LogError($"CombatSetup cannot build the Relic Forge catalog. {catalogError}", this);
                enabled = false;
                return;
            }

            Application.targetFrameRate = 60;
            // The profile is read from disk on every scene load, so a restart needs no persistent service object.
            _profileStore = new ProfileStore(ProfileLocation.Resolve());
            Profile = _profileStore.Load();
            ReportLoadProblems();
            Relics = new RelicShop(Profile, relics);
            Weapons = new WeaponShop(Profile, weapons, weapons[Array.IndexOf(_weapons, _heroDefinition.StartingWeapon)]);
            HeroWeapon = _weapons[Array.IndexOf(weapons, Weapons.Equipped)];

            _hero.Initialize(_heroDefinition.MaximumHealth);
            Weapon = HeroWeapon.CreateRuntime();
            _attack.Initialize(Weapon);
            Ability = _heroDefinition.Ability.CreateRuntime();
            _ability.Initialize(Ability);
            Run = new RunState(_economy.ExperiencePerLevel, _economy.AtRiskGoldLoss, _economy.ExperienceGrowth);
            _rewards = new RewardService(Run);
            // Created before any view subscribes to Run.Ended, so banked gold reaches the profile before results show.
            Bank = new RunBank(Run, Profile);
            Profile.Changed += SaveProfile;
            RelicOption equipped = Relics.Equipped;
            if (equipped != null)
            {
                Relic = new RelicRuntime(equipped);
                _relicBehaviour.Initialize(Relic);
            }

            var options = new UpgradeOption[_upgrades.Length];
            for (int i = 0; i < _upgrades.Length; i++)
                options[i] = _upgrades[i].CreateOption();
            Upgrades = new UpgradeService(Run, Weapon, options, _economy.UpgradeChoiceCount);
            Forge = new ForgeService(Run, _hero);
            Checkpoint = new CheckpointService(Run);
            Checkpoint.Chosen += OnCheckpointChosen;
            Choices = new RunChoices(Upgrades, Forge, Checkpoint);
            Pause = new RunPause(Run);
            Pause.Changed += ApplyPause;
            Choices.Changed += OnChoicesChanged;
            _hero.Damaged += OnHeroDamaged;
            _hero.Died += OnHeroDied;

            // Subscribe before the first spawn so every defeated enemy reaches the reward service.
            _encounters.EnemyDefeated += OnEnemyDefeated;
            _encounters.FloorCleared += OnFloorCleared;
            _encounters.Initialize(Choices, Forge);
        }

        // A reload rebuilds every runtime object from definitions and the saved profile, so nothing else carries over.
        public void RestartRun()
        {
            if (_restarting)
                return;

            _restarting = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(gameObject.scene.buildIndex);
        }

        private static bool AllPresent(UnityEngine.Object[] references)
        {
            if (references == null || references.Length == 0)
                return false;
            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] == null)
                    return false;
            }
            return true;
        }

        private void ReportLoadProblems()
        {
            if (_profileStore.LastLoadStatus != ProfileLoadStatus.Recovered && _profileStore.LastLoadStatus != ProfileLoadStatus.Reset)
                return;

            Debug.LogWarning($"Profile {_profileStore.LastLoadStatus} from {_profileStore.Directory}: " +
                string.Join("; ", _profileStore.LastLoadProblems), this);
        }

        // A failed write keeps the in-memory profile; the next change tries again.
        private void SaveProfile()
        {
            try
            {
                _profileStore.Save(Profile);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Debug.LogError($"Saving the profile to {_profileStore.Directory} failed: {exception}", this);
            }
        }

        private void OnEnemyDefeated(Health enemy)
        {
            EnemyDefinition definition = _encounters.DefinitionOf(enemy);
            _rewards.TryAwardKill(enemy, definition != null ? definition.ExperienceReward : 0, _encounters.GoldRewardOf(enemy));
        }

        // Damaged fires before Died, so the killer is known when the defeat ends the run.
        private void OnHeroDamaged(DamageContext context)
        {
            EnemyDefinition attacker = _encounters.DefinitionOf(context.Source);
            if (attacker != null)
                LastAttacker = attacker;
        }

        private void OnHeroDied() => Run.End(RunOutcome.Defeat);

        // The final floor ends the Descent; every earlier floor offers Extract or Descend.
        private void OnFloorCleared()
        {
            Profile.RecordFloorCleared(_encounters.FloorNumber);
            if (!_encounters.HasNextFloor)
            {
                Run.End(RunOutcome.Victory);
                return;
            }

            FloorDefinition next = _encounters.Floor.NextFloor;
            string modifier = next.Modifier != null ? next.Modifier.Description : _text.NoFloorModifier;
            Checkpoint.Open(new[]
            {
                new CheckpointOption(CheckpointKind.Extract, _text.ExtractName, string.Format(_text.ExtractDescriptionFormat, Run.Gold)),
                new CheckpointOption(CheckpointKind.Descend, _text.DescendName,
                    string.Format(_text.DescendDescriptionFormat, Run.Gold, next.DisplayName, modifier))
            });
        }

        private void OnCheckpointChosen(CheckpointKind kind)
        {
            if (kind == CheckpointKind.Extract)
            {
                Run.End(RunOutcome.Extracted);
                return;
            }

            Run.SecureGold();
            _encounters.DescendToNextFloor();
        }

        private void OnChoicesChanged() => Pause.SetChoiceOpen(Choices.IsOpen);

        // Leaving the app (a call, the home button) pauses the run so it waits for the player on return.
        private void OnApplicationPause(bool paused)
        {
            if (paused && Pause != null)
                Pause.TryPause();
        }

        // Scaled time freezes combat cadence while the run is frozen; uGUI input runs on unscaled time.
        private void ApplyPause()
        {
            bool frozen = Pause.IsFrozen;
            if (frozen == _timeFrozen)
                return;

            _timeFrozen = frozen;
            Time.timeScale = frozen ? 0f : 1f;
        }

        private void OnDestroy()
        {
            if (_hero != null)
            {
                _hero.Damaged -= OnHeroDamaged;
                _hero.Died -= OnHeroDied;
            }
            if (_encounters != null)
            {
                _encounters.EnemyDefeated -= OnEnemyDefeated;
                _encounters.FloorCleared -= OnFloorCleared;
            }
            if (Checkpoint != null)
                Checkpoint.Chosen -= OnCheckpointChosen;
            if (Choices != null)
                Choices.Changed -= OnChoicesChanged;
            if (Pause != null)
                Pause.Changed -= ApplyPause;
            if (Profile != null)
                Profile.Changed -= SaveProfile;
            if (_timeFrozen)
                Time.timeScale = 1f;
        }
    }
}
