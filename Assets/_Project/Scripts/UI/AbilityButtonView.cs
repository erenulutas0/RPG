using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The round ability buttons at the bottom right (docs/57 section 5.2). The scene's button is slot 1, the hero's shipped
    // ability: a tap fires it; a radial fill drains as the cooldown runs. Slots 2 and 3 (docs/57's skills) are copies of it
    // made in code - its painted ring, cooldown band, icon well and countdown, scaled to 176 of its 234 units - on an arc
    // 245 units from its centre, up (skill 2, at (-90, 341)) and to the left (skill 3, at (-306, 160)), anchored
    // bottom-right with pivot (1, 0) as it is: the right thumb's sweep from its rest on skill 1, while the left thumb keeps
    // the stick (two-handed play, the owner's answer of 2026-11-18). A slot shows:
    //   - live: the run carries the skill (CombatSetup.CarriesSkill: forged at the Forge, or development/skills.txt) - its
    //     icon, its cooldown and a tap that fires it; Shield Rush along the stick's heading at the tap (no finger down: the
    //     nearest enemy, AbilityController.Rush);
    //   - locked: its floor's first clear has opened it but the Forge has not forged it for this hero - the icon dimmed
    //     under a padlock, no tap;
    //   - nothing: not yet opened (a new player sees one button, then two, then three); the copy is made the moment its
    //     floor is cleared, mid-run included.
    // A button only answers while the run is live, no choice is open, the run is not paused and the fight is not held (the
    // boss's reveal: AbilityController.FightHold), which greys all three.
    public sealed class AbilityButtonView : MonoBehaviour
    {
        // docs/57 section 5.2 on the 1080-wide canvas: the skill buttons' size and their anchored positions.
        public const float SkillSize = 176f;
        public static readonly Vector2 SkillTwoPosition = new Vector2(-90f, 341f);
        public static readonly Vector2 SkillThreePosition = new Vector2(-306f, 160f);
        // The copies' object names: "Skill 2 Button", and each part's "Skill 2 " and its own name.
        public const string SkillButtonNameFormat = "Skill {0} Button";
        private static readonly Color LockedIcon = new Color(.42f, .42f, .48f, .75f);

        // How a skill slot stands on the HUD.
        public enum SlotState
        {
            Hidden = 0,
            Locked = 1,
            Live = 2
        }

        [SerializeField] private CombatSetup _setup;
        [SerializeField] private AbilityController _controller;
        [SerializeField] private Button _button;
        [SerializeField] private Image _cooldownFill;
        [SerializeField] private Image _icon;
        [SerializeField] private Text _countdownLabel;
        [SerializeField] private Color _readyColor = Color.white;
        [SerializeField] private Color _coolingColor = new Color(1f, 1f, 1f, 0.35f);
        // 1 for the scene's button; 2 and 3 for the copies slot 1 makes.
        [SerializeField, Range(1, 3)] private int _slot = 1;
        private Sprite _glyph;
        private Sprite _lockSprite;
        private Image _lock;
        private HeroMovementInput _movement;
        private bool _subscribed;
        private int _shownSeconds = -1;
        private SlotState _shownState = (SlotState)(-1);
        // Slot 1's copies, made when each slot first shows.
        private AbilityButtonView _skillTwo;
        private AbilityButtonView _skillThree;

        public int Slot => _slot;
        // The runtime this button fires; null for a slot the run does not carry.
        public AbilityRuntime Runtime => _controller != null ? _controller.SkillAt(_slot) : null;
        public SlotState State => _slot == 1 ? SlotState.Live : StateOf(Runtime != null, HeroHasSkill, _slot, Deepest);
        public bool IsLocked => State == SlotState.Locked;
        // The lock's padlock, shown on a locked slot.
        public Image LockImage => _lock;
        public Image Icon => _icon;
        public Image CooldownFill => _cooldownFill;
        public Button Button => _button;
        // Slot 1's copy for slot 2 or 3 once it has shown; null before that, and on the copies themselves.
        public AbilityButtonView SkillButton(int slot) => slot == 2 ? _skillTwo : slot == 3 ? _skillThree : slot == 1 ? this : null;

        public bool IsUsable
        {
            get
            {
                AbilityRuntime ability = Runtime;
                return _setup != null && _controller != null && ability != null && ability.IsReady && !_controller.IsHeld &&
                       !_setup.Run.HasEnded && !_setup.Choices.IsOpen && !_setup.Pause.IsPlayerPaused;
            }
        }

        // Whether the fight's hold greys the button (AbilityController.IsHeld: the boss's reveal holds the fight). It takes no
        // tap then, so a tap on it - on any of the three shown, a locked one too - is the reveal's to skip
        // (BossRevealView.HeldButtons).
        internal bool IsHeld => _controller != null && _controller.IsHeld && (_slot == 1 || gameObject.activeInHierarchy);
        // Where the button stands on screen.
        internal RectTransform ButtonRect => _button != null ? _button.transform as RectTransform : null;

        // docs/57 section 5.2: a slot the run carries is live; one its floor's first clear has opened (and the hero has) is
        // locked until the Forge forges it; any other shows nothing.
        public static SlotState StateOf(bool carried, bool heroHasSkill, int slot, int deepestFloorCleared)
        {
            if (carried)
                return SlotState.Live;
            return heroHasSkill && HeroSkillUnlock.IsForgeable(slot) && HeroSkillUnlock.IsOpen(slot, deepestFloorCleared)
                ? SlotState.Locked
                : SlotState.Hidden;
        }

        // Where slot 2's and slot 3's buttons stand, anchored bottom-right with pivot (1, 0).
        public static Vector2 PositionOf(int slot) => slot == 3 ? SkillThreePosition : SkillTwoPosition;

        private bool HeroHasSkill => _setup != null && _setup.Hero != null && _setup.Hero.SkillAt(_slot) != null;
        private int Deepest => _setup != null && _setup.Profile != null ? _setup.Profile.DeepestFloorCleared : 0;

        private void Start()
        {
            if (_setup == null || _setup.Run == null || _controller == null || _controller.Ability == null || _button == null ||
                _cooldownFill == null || _icon == null)
            {
                Debug.LogError("AbilityButtonView is missing a scene reference or the ability.", this);
                enabled = false;
                return;
            }

            // The run's hero brings its own skill's art (docs/45: the Arcanist's crucible beside the Vanguard's anvil;
            // docs/57: each skill's own icon).
            AbilityDefinition skill = _setup.Hero != null ? _setup.Hero.SkillAt(_slot) : null;
            Sprite own = skill != null ? skill.Icon : null;
            if (own != null)
                _icon.sprite = own;
            // Imported UI artwork belongs to the asset database. Only own and destroy the procedural fallback.
            if (_icon.sprite == null)
            {
                _glyph = PixelSpriteFactory.CreateSprite(AbilityArt.DrawGlyph(), "Ability Glyph", PixelSpriteFactory.Centre);
                _icon.sprite = _glyph;
            }
            if (_slot > 1)
                _movement = _controller.GetComponent<HeroMovementInput>();
            _button.onClick.AddListener(OnTap);
            _subscribed = true;
            Refresh();
        }

        private void Update()
        {
            // Slot 1 makes the copies after every Start of the scene (the painted HUD has dressed its ring by then), and
            // the moment a slot's floor is cleared mid-run.
            if (_slot == 1)
            {
                if (_skillTwo == null && StateOf(_controller.SkillAt(2) != null, HeroHas(2), 2, Deepest) != SlotState.Hidden)
                    _skillTwo = MakeSkillButton(2);
                if (_skillThree == null && StateOf(_controller.SkillAt(3) != null, HeroHas(3), 3, Deepest) != SlotState.Hidden)
                    _skillThree = MakeSkillButton(3);
            }
            Refresh();
        }

        private bool HeroHas(int slot) => _setup.Hero != null && _setup.Hero.SkillAt(slot) != null;

        // A copy of this button for slot 2 or 3: its parts as they stand (the painted ring and band), its size scaled to
        // SkillSize about its bottom-right pivot, its parts renamed so a search for the scene's button finds slot 1 alone.
        private AbilityButtonView MakeSkillButton(int slot)
        {
            GameObject copy = Instantiate(gameObject, transform.parent);
            copy.name = string.Format(System.Globalization.CultureInfo.InvariantCulture, SkillButtonNameFormat, slot);
            string prefix = "Skill " + slot.ToString(System.Globalization.CultureInfo.InvariantCulture) + " ";
            foreach (Transform part in copy.GetComponentsInChildren<Transform>(true))
            {
                if (part != copy.transform)
                    part.name = prefix + part.name.Replace("Ability ", string.Empty);
            }
            var rect = (RectTransform)copy.transform;
            var source = (RectTransform)transform;
            rect.anchorMin = source.anchorMin;
            rect.anchorMax = source.anchorMax;
            rect.pivot = source.pivot;
            rect.sizeDelta = source.sizeDelta;
            float scale = source.sizeDelta.x > 0f ? SkillSize / source.sizeDelta.x : 1f;
            rect.localScale = new Vector3(scale, scale, 1f);
            rect.anchoredPosition = PositionOf(slot);
            // Behind slot 1 in the draw order, so its ring is never covered.
            copy.transform.SetSiblingIndex(transform.GetSiblingIndex());
            var view = copy.GetComponent<AbilityButtonView>();
            view._slot = slot;
            view.MakeLock();
            return view;
        }

        // The padlock over a locked slot's dimmed icon: a procedural glyph (AbilityArt.DrawLock), owned by this button.
        private void MakeLock()
        {
            _lockSprite = PixelSpriteFactory.CreateSprite(AbilityArt.DrawLock(), "Skill Lock", PixelSpriteFactory.Centre);
            var lockObject = new GameObject("Skill " + _slot + " Lock", typeof(RectTransform));
            lockObject.layer = gameObject.layer;
            var rect = (RectTransform)lockObject.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(96f, 96f);
            _lock = lockObject.AddComponent<Image>();
            _lock.sprite = _lockSprite;
            _lock.preserveAspect = true;
            _lock.raycastTarget = false;
            _lock.enabled = false;
        }

        private void Refresh()
        {
            SlotState state = State;
            if (state != _shownState)
            {
                _shownState = state;
                if (gameObject.activeSelf != (state != SlotState.Hidden))
                    gameObject.SetActive(state != SlotState.Hidden);
                if (_lock != null)
                    _lock.enabled = state == SlotState.Locked;
            }
            AbilityRuntime ability = Runtime;
            if (ability == null)
            {
                _cooldownFill.fillAmount = 0f;
                _button.interactable = false;
                _icon.color = LockedIcon;
                SetCountdown(0);
                return;
            }
            _cooldownFill.fillAmount = ability.Cooldown > 0f ? ability.Remaining / ability.Cooldown : 0f;
            bool usable = IsUsable;
            _button.interactable = usable;
            _icon.color = usable ? _readyColor : _coolingColor;
            SetCountdown(Mathf.CeilToInt(ability.Remaining));
        }

        private void SetCountdown(int seconds)
        {
            if (_countdownLabel != null && seconds != _shownSeconds)
            {
                _shownSeconds = seconds;
                _countdownLabel.text = seconds > 0 ? HudText.Count(seconds) : string.Empty;
            }
        }

        // Slot 1 as it always fired; a skill along the stick's heading at the tap, which only the Rush reads (two hands: the
        // left thumb steers while the right one taps).
        private void OnTap()
        {
            if (!IsUsable)
                return;
            if (_slot == 1)
                _controller.TryUse();
            else
                _controller.TryUse(_slot, _movement != null ? _movement.Steer : Vector2.zero);
        }

        private void OnDestroy()
        {
            if (_subscribed && _button != null)
                _button.onClick.RemoveListener(OnTap);
            PixelSpriteFactory.Destroy(_glyph);
            PixelSpriteFactory.Destroy(_lockSprite);
        }
    }
}
