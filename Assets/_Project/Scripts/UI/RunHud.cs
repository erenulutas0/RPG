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
        // Compact combat presentation. The detailed readout remains available on the pause screen.
        [SerializeField] private CanvasGroup _details;
        [SerializeField] private Text _compactGold;
        [SerializeField] private Text _weaponName;
        [SerializeField] private Image[] _roomPips;
        [SerializeField] private CanvasGroup _bossGroup;
        [SerializeField] private Text _bossName;
        [SerializeField] private UpgradeDefinition[] _badgeDefinitions;
        [SerializeField] private GameObject[] _upgradeBadges;
        [SerializeField] private Text[] _upgradeCounts;
        [SerializeField] private GameObject _relicBadge;
        [SerializeField] private Image _relicIcon;
        [SerializeField] private Text _relicCount;
        [SerializeField] private Sprite _secondWindIcon;
        [SerializeField] private Sprite _counterweightIcon;
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
            _setup.Upgrades.OfferChanged += RefreshBadges;
            _setup.Pause.Changed += RefreshDetails;
            if (_setup.Relic != null)
                _setup.Relic.Triggered += RefreshRelic;
            _subscribed = true;
            RefreshHero();
            RefreshEncounter();
            RefreshWeapon();
            RefreshExperience();
            RefreshGold();
            RefreshRelic();
            RefreshBadges();
            RefreshDetails();
        }

        private void RefreshDetails()
        {
            if (_details == null)
                return;
            _details.alpha = _setup.Pause.IsPlayerPaused ? 1f : 0f;
            _details.blocksRaycasts = false;
            _details.interactable = false;
        }

        private void RefreshBadges()
        {
            if (_badgeDefinitions == null || _upgradeBadges == null || _upgradeCounts == null)
                return;
            int slots = Mathf.Min(_badgeDefinitions.Length, Mathf.Min(_upgradeBadges.Length, _upgradeCounts.Length));
            for (int i = 0; i < slots; i++)
            {
                int count = 0;
                if (_badgeDefinitions[i] != null)
                    foreach (UpgradeOption option in _setup.Upgrades.Pool)
                        if (option.Id == _badgeDefinitions[i].Id)
                            count = _setup.Upgrades.StacksOf(option);
                _upgradeBadges[i].SetActive(count > 0);
                _upgradeCounts[i].text = count.ToString();
            }
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
            if (_relicBadge != null)
            {
                _relicBadge.SetActive(relic != null);
                if (relic != null)
                {
                    bool secondWind = relic.Relic.Effect == RelicEffect.SecondWind;
                    _relicIcon.sprite = secondWind ? _secondWindIcon : _counterweightIcon;
                    _relicIcon.color = secondWind && relic.Triggers > 0 ? new Color(1f, 1f, 1f, 0.35f) : Color.white;
                    _relicCount.text = secondWind ? (relic.Triggers == 0 ? "1" : "0") : relic.Triggers.ToString();
                }
            }
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
            if (_roomPips != null)
                for (int i = 0; i < _roomPips.Length; i++)
                {
                    _roomPips[i].gameObject.SetActive(i < _encounters.RoomCount);
                    _roomPips[i].color = i < _encounters.RoomNumber
                        ? new Color32(218, 165, 84, 255) : new Color32(60, 60, 79, 255);
                }
            if (_bossGroup != null)
            {
                _bossGroup.alpha = room != null && room.Kind == RoomKind.Boss ? 1f : 0f;
                _bossName.text = _encounters.CurrentDefinition != null ? _encounters.CurrentDefinition.DisplayName : string.Empty;
            }

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
            if (_compactGold != null)
                _compactGold.text = _setup.Run.Gold.ToString();
        }

        private void RefreshWeapon()
        {
            _weaponLabel.text = string.Format(_text.WeaponFormat, _setup.HeroWeapon.DisplayName,
                _setup.Weapon.Damage, _setup.Weapon.Interval);
            if (_weaponName != null)
                _weaponName.text = _setup.HeroWeapon.DisplayName;
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
            _setup.Upgrades.OfferChanged -= RefreshBadges;
            _setup.Pause.Changed -= RefreshDetails;
            if (_setup.Relic != null)
                _setup.Relic.Triggered -= RefreshRelic;
        }
    }
}
