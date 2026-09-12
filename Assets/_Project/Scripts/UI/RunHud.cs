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
        [SerializeField] private Health _hero;
        [SerializeField] private Health _enemy;
        [SerializeField] private AttackController _attack;
        [SerializeField] private HeroDefinition _heroDefinition;
        [SerializeField] private EnemyDefinition _enemyDefinition;
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
            if (AnyMissing(_setup, _hero, _enemy, _attack, _heroDefinition, _enemyDefinition, _text, _titleLabel,
                    _subtitleLabel, _weaponLabel, _enemyLabel, _heroLabel, _statusLabel, _attackLabel,
                    _experienceLabel, _enemyBar, _heroBar, _experienceBar) ||
                _heroDefinition.StartingWeapon == null || _setup.Run == null)
            {
                Debug.LogError("RunHud is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _titleLabel.text = _text.Title;
            _subtitleLabel.text = _text.Subtitle;
            _hero.Changed += RefreshCombat;
            _enemy.Changed += RefreshCombat;
            _attack.Attacked += RefreshCombat;
            _setup.Weapon.StatsChanged += RefreshWeapon;
            _setup.Run.ExperienceChanged += RefreshExperience;
            _subscribed = true;
            RefreshCombat();
            RefreshWeapon();
            RefreshExperience();
        }

        private void RefreshCombat()
        {
            _heroLabel.text = string.Format(_text.HealthFormat, _heroDefinition.DisplayName, _hero.Current, _hero.Maximum);
            _enemyLabel.text = string.Format(_text.HealthFormat, _enemyDefinition.DisplayName, _enemy.Current, _enemy.Maximum);
            _heroBar.fillAmount = Fraction(_hero.Current, _hero.Maximum);
            _enemyBar.fillAmount = Fraction(_enemy.Current, _enemy.Maximum);
            _statusLabel.text = _enemy.IsAlive ? _text.Fighting : _text.Victory;
            _attackLabel.text = string.Format(_text.AttackCountFormat, _attack.AttackCount);
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

            _hero.Changed -= RefreshCombat;
            _enemy.Changed -= RefreshCombat;
            _attack.Attacked -= RefreshCombat;
            _setup.Weapon.StatsChanged -= RefreshWeapon;
            _setup.Run.ExperienceChanged -= RefreshExperience;
        }
    }
}
