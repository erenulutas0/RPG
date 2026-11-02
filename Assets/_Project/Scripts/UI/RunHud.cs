using Cryptforge.Art;
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
        // Optional: the painted weapon and tome icons the loadout row's badges show.
        [SerializeField] private UpgradeIconSet _upgradeIcons;
        private LoadoutBadgesView _loadoutBadges;
        private bool _subscribed;
        // What the encounter's three labels were last formatted from, and the text each was given. Progress is reported
        // on every hit any enemy of the wave takes - several a frame in a crowd, and a burn ticks twice a second - while
        // what these labels say rarely changes, so a label is formatted again only when a value it shows has changed or
        // its text is no longer the one formatted here. The text is the same as formatting it every time.
        private string _floorText;
        private int _floorNumber;
        private int _floorRoom;
        private int _floorRooms;
        private string _floorRoomName;
        private string _enemyText;
        private string _enemyName;
        private float _enemyCurrent;
        private float _enemyMaximum;
        private int _enemyOthers;
        private string _statusText;
        private int _statusKind;
        private int _statusFirst;
        private int _statusSecond;
        private float _statusSeconds;
        private string _statusName;

        // The weapon and tome row on a category-mode run; null on the legacy cards. For tests.
        public LoadoutBadgesView LoadoutBadges => _loadoutBadges;
        // Whose health the enemy (boss) bar shows: the encounter's current enemy, which changes as the nearest one does.
        // Null when the bar shows nobody.
        internal Health ShownEnemy { get; private set; }
        // The floor's clock under the top panel; null without the compact top panel. For tests.
        public FloorClockView FloorClock { get; private set; }

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
            // The danger readout sits on the compact top panel, so it needs the compact gold count to style itself on.
            if (_compactGold != null)
                gameObject.AddComponent<DangerReadoutView>().Initialize(_setup, _text, _compactGold);
            // The floor's clock (HordeClock) under the top panel, wherever a horde runs.
            if (_compactGold != null)
                FloorClock = gameObject.AddComponent<FloorClockView>();
            if (FloorClock != null)
                FloorClock.Initialize(_setup, _encounters, _text, _compactGold);
            // A category-mode run carries up to three weapons and three tomes, which the legacy row - the forge weapon's
            // name and two card stacks category mode never offers - can say none of. The loadout row takes that band of
            // the top panel, so the name is hidden here and the legacy badges in RefreshBadges; the first weapon badge is
            // the forge's weapon, and its name stays on the pause screen's detailed readout.
            if (_setup.Upgrades.IsCategoryMode && _compactGold != null)
            {
                _loadoutBadges = gameObject.AddComponent<LoadoutBadgesView>();
                _loadoutBadges.Initialize(_setup, _text, _compactGold);
                if (_upgradeIcons != null)
                    _loadoutBadges.UseIcons(_upgradeIcons);
                if (_weaponName != null)
                    _weaponName.gameObject.SetActive(false);
            }
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
            // The loadout row sits where these do; they are hidden whatever the pool holds, so a later pool change can
            // never draw them over it.
            if (_loadoutBadges != null)
            {
                for (int i = 0; i < _upgradeBadges.Length; i++)
                    if (_upgradeBadges[i] != null)
                        _upgradeBadges[i].SetActive(false);
                return;
            }
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
            string name = _setup.Hero != null ? _setup.Hero.DisplayName : _heroDefinition.DisplayName;
            _heroLabel.text = string.Format(_text.HealthFormat, name, _hero.Current, _hero.Maximum);
            _heroBar.fillAmount = Fraction(_hero.Current, _hero.Maximum);
        }

        private void RefreshEncounter()
        {
            RoomDefinition room = _encounters.CurrentRoom;
            int floorNumber = _encounters.FloorNumber;
            int roomNumber = Mathf.Max(1, _encounters.RoomNumber);
            int rooms = _encounters.RoomCount;
            string roomName = room != null ? room.DisplayName : string.Empty;
            if (!ReferenceEquals(_floorLabel.text, _floorText) || floorNumber != _floorNumber || roomNumber != _floorRoom ||
                rooms != _floorRooms || !string.Equals(roomName, _floorRoomName, System.StringComparison.Ordinal))
            {
                _floorLabel.text = string.Format(_text.FloorProgressFormat, floorNumber, roomNumber, rooms, roomName);
                // Read back: the label keeps its old string when the new one has the same characters.
                _floorText = _floorLabel.text;
                _floorNumber = floorNumber;
                _floorRoom = roomNumber;
                _floorRooms = rooms;
                _floorRoomName = roomName;
            }
            if (_roomPips != null && _roomPips.Length > 0)
            {
                if (rooms > _roomPips.Length)
                    GrowRoomPips(rooms);
                int reached = _encounters.RoomNumber;
                for (int i = 0; i < _roomPips.Length; i++)
                {
                    _roomPips[i].gameObject.SetActive(i < rooms);
                    _roomPips[i].color = i < reached ? new Color32(218, 165, 84, 255) : new Color32(60, 60, 79, 255);
                }
            }
            if (_bossGroup != null)
            {
                _bossGroup.alpha = room != null && room.Kind == RoomKind.Boss ? 1f : 0f;
                _bossName.text = _encounters.CurrentDefinition != null ? _encounters.CurrentDefinition.DisplayName : string.Empty;
            }

            if (_encounters.IsInNonCombatRoom)
            {
                ShownEnemy = null;
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
            ShownEnemy = enemy;
            float current = enemy != null ? enemy.Current : 0f;
            float maximum = enemy != null ? enemy.Maximum : definition.MaximumHealth;
            int othersAlive = _encounters.AliveEnemyCount - (enemy != null && enemy.IsAlive ? 1 : 0);
            string name = definition.DisplayName;
            if (!ReferenceEquals(_enemyLabel.text, _enemyText) || othersAlive != _enemyOthers || current != _enemyCurrent ||
                maximum != _enemyMaximum || !string.Equals(name, _enemyName, System.StringComparison.Ordinal))
            {
                _enemyLabel.text = othersAlive > 0
                    ? string.Format(_text.PackHealthFormat, name, current, maximum, othersAlive)
                    : string.Format(_text.HealthFormat, name, current, maximum);
                _enemyText = _enemyLabel.text;
                _enemyName = name;
                _enemyCurrent = current;
                _enemyMaximum = maximum;
                _enemyOthers = othersAlive;
            }
            _enemyBar.fillAmount = Fraction(current, maximum);
            RefreshStatus(definition);
        }

        // One pip a room: a floor with more rooms than the scene has pips (the foundry's floor 1, its V0.1 elite before the
        // gate making seven) gets copies of the last pip, each a step further along the row at the scene's own spacing.
        // On the 1080-wide canvas the scene's six span x 52-614 and y 133-141 down from the safe area's top; a seventh
        // spans x 628-710: below the gold (to 106), above the relic badge and the loadout row (from 155), left of the
        // pause button (from 936) and inside the top surface (to 1056). The row has room for nine before the pause button.
        private void GrowRoomPips(int count)
        {
            int last = _roomPips.Length - 1;
            Vector2 step = last > 0
                ? _roomPips[last].rectTransform.anchoredPosition - _roomPips[last - 1].rectTransform.anchoredPosition
                : new Vector2(_roomPips[last].rectTransform.sizeDelta.x, 0f);
            var pips = new Image[count];
            System.Array.Copy(_roomPips, pips, _roomPips.Length);
            for (int i = _roomPips.Length; i < count; i++)
            {
                Image source = pips[i - 1];
                Image pip = Instantiate(source, source.transform.parent, false);
                pip.name = "Room Pip " + (i + 1);
                pip.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);
                pip.rectTransform.anchoredPosition = source.rectTransform.anchoredPosition + step;
                pips[i] = pip;
            }
            _roomPips = pips;
        }

        // The pips as shown, the scene's and any copies GrowRoomPips made: HudArtView paints the copies too.
        internal int RoomPipCount => _roomPips?.Length ?? 0;
        internal Image RoomPipAt(int index) => _roomPips[index];

        // The wave's line on the details: the pack's or the enemy's victory, an enrage, or the wave under way.
        private void RefreshStatus(EnemyDefinition definition)
        {
            EnemyDefinition enraged = _encounters.EnragedDefinition;
            int kind, first = 0, second = 0;
            float seconds = 0f;
            string name = null;
            if (_encounters.IsCleared && _encounters.WaveEnemyCount > 1)
            {
                kind = 1;
                first = _encounters.WaveEnemyCount;
                second = _encounters.HitsTaken;
                seconds = _encounters.Elapsed;
            }
            else if (_encounters.IsCleared)
            {
                kind = 2;
                name = definition.DisplayName;
                second = _encounters.HitsTaken;
                seconds = _encounters.Elapsed;
            }
            else if (enraged != null)
            {
                kind = 3;
                name = enraged.DisplayName;
            }
            else
            {
                kind = 4;
                first = _encounters.WaveNumber;
                second = _encounters.WaveCount;
            }
            if (ReferenceEquals(_statusLabel.text, _statusText) && kind == _statusKind && first == _statusFirst &&
                second == _statusSecond && seconds == _statusSeconds &&
                string.Equals(name, _statusName, System.StringComparison.Ordinal))
                return;
            _statusLabel.text = kind == 1 ? string.Format(_text.PackVictoryFormat, first, second, seconds)
                : kind == 2 ? string.Format(_text.VictoryFormat, name, second, seconds)
                : kind == 3 ? string.Format(_text.EnragedFormat, name)
                : string.Format(_text.WaveFormat, first, second);
            _statusText = _statusLabel.text;
            _statusKind = kind;
            _statusFirst = first;
            _statusSecond = second;
            _statusSeconds = seconds;
            _statusName = name;
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
