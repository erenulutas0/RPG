using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
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
        [SerializeField] private Text _floorLabel;
        [SerializeField] private Text _weaponLabel;
        [SerializeField] private Text _relicLabel;
        [SerializeField] private Text _enemyLabel;
        [SerializeField] private Text _heroLabel;
        [SerializeField] private Text _statusLabel;
        [SerializeField] private Text _goldLabel;
        [SerializeField] private Text _experienceLabel;
        [SerializeField] private Image _enemyBar;
        [SerializeField] private Image _heroBar;
        [SerializeField] private Image _experienceBar;
        private bool _subscribed;

        // Run state exists after CombatSetup.Awake, so Start is the earliest safe subscription point.
        private void Start()
        {
            if (AnyMissing(_setup, _encounters, _hero, _heroDefinition, _text, _titleLabel, _floorLabel,
                    _weaponLabel, _relicLabel, _enemyLabel, _heroLabel, _statusLabel, _goldLabel, _experienceLabel, _enemyBar,
                    _heroBar, _experienceBar) ||
                _heroDefinition.StartingWeapon == null || _setup.Run == null || _encounters.Floor == null)
            {
                Debug.LogError("RunHud is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            _titleLabel.text = _text.Title;
            _hero.Changed += RefreshHero;
            _encounters.ProgressChanged += RefreshEncounter;
            _setup.Weapon.StatsChanged += RefreshWeapon;
            _setup.Run.ExperienceChanged += RefreshExperience;
            _setup.Run.GoldChanged += RefreshGold;
            if (_setup.Relic != null)
                _setup.Relic.Triggered += RefreshRelic;
            _subscribed = true;
            RefreshHero();
            RefreshEncounter();
            RefreshWeapon();
            RefreshExperience();
            RefreshGold();
            RefreshRelic();
        }

        private void RefreshRelic()
        {
            RelicRuntime relic = _setup.Relic;
            if (relic == null)
                _relicLabel.text = string.Empty;
            else if (relic.Triggers == 0)
                _relicLabel.text = string.Format(_text.RelicFormat, relic.Relic.DisplayName);
            else
                _relicLabel.text = string.Format(_text.RelicTriggeredFormat, relic.Relic.DisplayName, relic.Triggers);
        }

        private void RefreshHero()
        {
            _heroLabel.text = string.Format(_text.HealthFormat, _heroDefinition.DisplayName, _hero.Current, _hero.Maximum);
            _heroBar.fillAmount = Fraction(_hero.Current, _hero.Maximum);
        }

        private void RefreshEncounter()
        {
            RoomDefinition room = _encounters.CurrentRoom;
            _floorLabel.text = string.Format(_text.FloorProgressFormat, _encounters.FloorNumber,
                Mathf.Max(1, _encounters.RoomNumber), _encounters.RoomCount, room != null ? room.DisplayName : string.Empty);

            if (_encounters.IsInNonCombatRoom)
            {
                _enemyLabel.text = room != null ? room.DisplayName : string.Empty;
                _enemyBar.fillAmount = 0f;
                _statusLabel.text = _text.ForgeStatus;
                return;
            }

            EnemyDefinition definition = _encounters.CurrentDefinition;
            if (definition == null)
                return;

            // The bar follows the hero's current target; the label counts the rest of the pack still standing.
            Health enemy = _encounters.CurrentEnemy;
            float current = enemy != null ? enemy.Current : 0f;
            float maximum = enemy != null ? enemy.Maximum : definition.MaximumHealth;
            int othersAlive = _encounters.AliveEnemyCount - (enemy != null && enemy.IsAlive ? 1 : 0);
            _enemyLabel.text = othersAlive > 0
                ? string.Format(_text.PackHealthFormat, definition.DisplayName, current, maximum, othersAlive)
                : string.Format(_text.HealthFormat, definition.DisplayName, current, maximum);
            _enemyBar.fillAmount = Fraction(current, maximum);
            EnemyDefinition enraged = _encounters.EnragedDefinition;
            if (_encounters.IsCleared && _encounters.WaveEnemyCount > 1)
                _statusLabel.text = string.Format(_text.PackVictoryFormat, _encounters.WaveEnemyCount, _encounters.HitsTaken, _encounters.Elapsed);
            else if (_encounters.IsCleared)
                _statusLabel.text = string.Format(_text.VictoryFormat, definition.DisplayName, _encounters.HitsTaken, _encounters.Elapsed);
            else if (enraged != null)
                _statusLabel.text = string.Format(_text.EnragedFormat, enraged.DisplayName);
            else
                _statusLabel.text = string.Format(_text.WaveFormat, _encounters.WaveNumber, _encounters.WaveCount);
        }

        private void RefreshGold()
        {
            _goldLabel.text = string.Format(_text.GoldFormat, _setup.Run.Gold, _setup.Run.UnsecuredGold);
        }

        private void RefreshWeapon()
        {
            _weaponLabel.text = string.Format(_text.WeaponFormat, _setup.HeroWeapon.DisplayName,
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
            _setup.Run.GoldChanged -= RefreshGold;
            if (_setup.Relic != null)
                _setup.Relic.Triggered -= RefreshRelic;
        }
    }
}
