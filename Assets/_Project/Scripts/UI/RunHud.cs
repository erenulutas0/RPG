using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // Read-only battle HUD. Text is formatted on state events, never per frame.
    public sealed class RunHud : MonoBehaviour
    {
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private EncounterController _encounters;
        [SerializeField] private Health _hero;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _subtitleLabel;
        [SerializeField] private Text _weaponLabel;
        [SerializeField] private Text _enemyLabel;
        [SerializeField] private Text _heroLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Text _attackLabel;
        [SerializeField] private Text _experienceLabel;
        [SerializeField] private Image _enemyBar;
        [SerializeField] private Image _heroBar;
        [SerializeField] private Image _experienceBar;
        private bool _subscribed;

        // Run state exists after CombatSetup.Awake, so Start is the earliest safe subscription point.
        private void Start()
        {
            if (AnyMissing(_setup, _encounters, _hero, _heroDefinition, _text, _titleLabel, _subtitleLabel,
                    _weaponLabel, _enemyLabel, _heroLabel, _statusLabel, _attackLabel, _experienceLabel, _enemyBar,
                    _heroBar, _experienceBar) ||
                _heroDefinition.StartingWeapon == null || _setup.Run == null || _encounters.CurrentDefinition == null)
            {
                Debug.LogError("RunHud is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _titleLabel.text = _text.Title;
            _subtitleLabel.text = _text.Subtitle;
            _hero.Changed += RefreshHero;
            _encounters.ProgressChanged += RefreshEncounter;
            _setup.Weapon.StatsChanged += RefreshWeapon;
            _setup.Run.ExperienceChanged += RefreshExperience;
            _subscribed = true;
            RefreshHero();
            RefreshEncounter();
            RefreshWeapon();
            RefreshExperience();
        }

        private void RefreshHero()
        {
            _heroLabel.text = string.Format(_text.HealthFormat, _heroDefinition.DisplayName, _hero.Current, _hero.Maximum);
            _heroBar.fillAmount = Fraction(_hero.Current, _hero.Maximum);
        }

        private void RefreshEncounter()
        {
            Health enemy = _encounters.CurrentEnemy;
            string enemyName = _encounters.CurrentDefinition.DisplayName;
            float current = enemy != null ? enemy.Current : 0f;
            float maximum = enemy != null ? enemy.Maximum : _encounters.CurrentDefinition.MaximumHealth;
            _enemyLabel.text = string.Format(_text.HealthFormat, enemyName, current, maximum);
            _enemyBar.fillAmount = Fraction(current, maximum);
            _attackLabel.text = string.Format(_text.AttackCountFormat, _encounters.HitsTaken);
            _statusLabel.text = _encounters.IsCleared
                ? string.Format(_text.VictoryFormat, enemyName, _encounters.HitsTaken, _encounters.Elapsed)
                : string.Format(_text.EncounterFormat, _encounters.EncounterNumber);
        }

        private void RefreshWeapon()
        {
            _weaponLabel.text = string.Format(_text.WeaponFormat, _heroDefinition.StartingWeapon.DisplayName,
                _setup.Weapon.Damage, _setup.Weapon.Interval);
        }

        private void RefreshExperience()
        {
            RunState run = _setup.Run;
            _experienceLabel.text = string.Format(_text.ExperienceFormat, run.Level, run.Experience, run.ExperienceForNextLevel);
            _experienceBar.fillAmount = Fraction(run.Experience - run.ExperienceForCurrentLevel,
                run.ExperienceForNextLevel - run.ExperienceForCurrentLevel);
        }

        private static float Fraction(float value, float maximum) =>
            maximum > 0f ? Mathf.Clamp01(value / maximum) : 0f;

        private static bool AnyMissing(params Object[] references)
        {
            for (int i = 0; i < references.Length; i++)
            {
                if (references[i] == null)
                    return true;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            _hero.Changed -= RefreshHero;
            if (_encounters != null)
                _encounters.ProgressChanged -= RefreshEncounter;
            _setup.Weapon.StatsChanged -= RefreshWeapon;
            _setup.Run.ExperienceChanged -= RefreshExperience;
        }
    }
}
