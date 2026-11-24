using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // docs/57 section 5.2's one-line toast: the first run that carries a skill the Forge has forged for its hero names it
    // and says when to tap it - "Iron Resolve: tap when the blows come" - once per skill and profile
    // (PlayerProfile.MarkSkillToastShown, saved at once). The unlock is the tutorial: the new button stands on the HUD and
    // this line says what it is for.
    //   - When: once the run is live (the map chosen, no level-up or pause open, the fight not held by a boss's reveal) and
    //     has run LeadSeconds of it, so the run's first moment has passed; skill 2's first, then skill 3's after a short gap
    //     when one run brings both.
    //   - Where: a smoked plate across the HUD's lower third, just above the skill buttons' arc (skill 2's top stands at
    //     341 + 176 = 517 on the 1080-wide safe area), in the HUD's safe area; nothing of it catches a tap, so the stick
    //     and the buttons answer as ever.
    //   - How long: ShowSeconds of the run's own scaled time, faded in and out, so a level-up or a pause holds it with the
    //     fight and it is read whole; the run's end takes it away.
    // Only a forged skill is introduced: development/skills.txt's forced skills (the Editor's measurements) never are. The
    // plate is built the first time a toast shows, so a run with none to show makes nothing. Presentation only: it writes
    // the profile's flag and nothing else.
    public sealed class SkillToastView : MonoBehaviour
    {
        public const string PlateName = "Skill Toast";
        // Seconds of live run before the first toast, each toast's time, its fades, and the gap between two.
        public const float LeadSeconds = 1.5f;
        public const float ShowSeconds = 5f;
        private const float FadeIn = .25f;
        private const float FadeOut = .5f;
        private const float GapSeconds = .6f;
        // On the HUD's 1080-wide safe area: the plate's foot over the safe area's foot, its widest, its height and padding;
        // the line's size and its smallest best fit.
        internal const float Bottom = 560f;
        internal const float MaxWidth = 960f;
        internal const float Height = 84f;
        internal const float PadX = 30f;
        internal const float PadY = 12f;
        internal const int LineSize = 36;
        internal const int LineMinSize = 24;
        private static readonly Color Ivory = new Color(.933f, .910f, .851f, 1f);
        private static readonly Color Smoke = new Color(.07f, .06f, .09f, .82f);
        private static readonly Color Gold = new Color32(238, 191, 108, 255);

        private CombatSetup _setup;
        private AbilityController _controller;
        private HeroDefinition _hero;
        private readonly List<int> _pending = new List<int>();
        private float _live;
        private float _elapsed;
        private float _wait;
        private bool _showing;
        private RectTransform _plate;
        private Image _plateImage;
        private Text _label;
        private CanvasGroup _group;

        // For tests and captures: whether a toast shows, its line and its skill slot; the skills still to introduce.
        public bool IsShowing => _showing;
        public string ShownLine { get; private set; } = string.Empty;
        public int ShownSlot { get; private set; }
        public int PendingCount => _pending.Count;
        // How many toasts this run has shown.
        public int ShownCount { get; private set; }
        internal RectTransform Plate => _plate;
        internal Text Label => _label;

        // CombatSetup, once the run's skills are wired (StartSkills): the forged skills of the run's hero whose toast the
        // profile has not shown, in slot order.
        internal void Initialize(CombatSetup setup, AbilityController controller)
        {
            _setup = setup;
            _controller = controller;
            _hero = setup.Hero;
            _pending.Clear();
            for (int slot = HeroSkillUnlock.FirstSlot + 1; _hero != null && slot <= HeroSkillUnlock.LastSlot; slot++)
            {
                if (_hero.SkillAt(slot) != null && controller != null && controller.SkillAt(slot) != null &&
                    setup.Profile.HasForgedSkill(_hero.Id, slot) && !setup.Profile.HasShownSkillToast(_hero.Id, slot))
                    _pending.Add(slot);
            }
            enabled = _pending.Count > 0;
        }

        private bool RunIsLive =>
            !_setup.IsChoosingMap && _setup.Run != null && !_setup.Run.HasEnded && _setup.Pause != null && !_setup.Pause.IsFrozen &&
            (_controller == null || !_controller.IsHeld);

        private void Update()
        {
            if (_setup == null)
                return;
            float delta = Time.deltaTime;
            if (_showing)
            {
                _elapsed += delta;
                if (_setup.Run == null || _setup.Run.HasEnded || _elapsed >= ShowSeconds)
                {
                    Hide();
                    _wait = GapSeconds;
                    if (_pending.Count == 0)
                        enabled = false;
                    return;
                }
                _group.alpha = Mathf.Min(1f, Mathf.Min(_elapsed / FadeIn, (ShowSeconds - _elapsed) / FadeOut));
                return;
            }
            if (_pending.Count == 0 || !RunIsLive)
                return;
            _live += delta;
            if (_wait > 0f)
            {
                _wait -= delta;
                return;
            }
            if (_live >= LeadSeconds)
                Show(_pending[0]);
        }

        // The toast for the slot: the skill's name in the call's gold and when to tap it; the profile records it shown.
        private void Show(int slot)
        {
            _pending.RemoveAt(0);
            AbilityDefinition skill = _hero.SkillAt(slot);
            string hint = AbilityText.TapHint(skill);
            string name = "<color=#" + ColorUtility.ToHtmlStringRGB(Gold) + ">" + AbilityText.Name(skill) + "</color>";
            _setup.Profile.MarkSkillToastShown(_hero.Id, slot);
            if (hint.Length == 0 || !Build())
                return;
            ShownLine = Loc.Format("skills.toast", "{0}: {1}", name, hint);
            ShownSlot = slot;
            ShownCount++;
            TakePlate();
            _group.alpha = 0f;
            _plate.SetAsLastSibling();
            _plate.gameObject.SetActive(true);
            _label.text = ShownLine;
            // As wide as the line at its full size, within the widest; a longer line shrinks by best fit, or wraps.
            TextGenerationSettings settings = _label.GetGenerationSettings(Vector2.zero);
            settings.resizeTextForBestFit = false;
            settings.fontSize = LineSize;
            float scale = _label.pixelsPerUnit > 0f ? _label.pixelsPerUnit : 1f;
            float line = _label.cachedTextGeneratorForLayout.GetPreferredWidth(ShownLine, settings) / scale;
            _plate.sizeDelta = new Vector2(Mathf.Min(MaxWidth, line + 2f * PadX), Height);
            _elapsed = 0f;
            _showing = true;
        }

        private void Hide()
        {
            _showing = false;
            if (_plate != null)
                _plate.gameObject.SetActive(false);
        }

        // The plate in the HUD's safe area (the ability button's parent), built once; false without a HUD to put it in.
        private bool Build()
        {
            if (_plate != null)
                return true;
            RectTransform area = null;
            foreach (AbilityButtonView button in FindObjectsByType<AbilityButtonView>(FindObjectsSortMode.None))
                if (button.Slot == 1)
                    area = button.transform.parent as RectTransform;
            if (area == null)
                return false;

            var plate = new GameObject(PlateName, typeof(RectTransform));
            plate.layer = area.gameObject.layer;
            _plate = (RectTransform)plate.transform;
            _plate.SetParent(area, false);
            _plate.anchorMin = _plate.anchorMax = _plate.pivot = new Vector2(.5f, 0f);
            _plate.anchoredPosition = new Vector2(0f, Bottom);
            _plate.sizeDelta = new Vector2(MaxWidth, Height);
            _plateImage = plate.AddComponent<Image>();
            _plateImage.raycastTarget = false;
            _plateImage.color = Smoke;
            _group = plate.AddComponent<CanvasGroup>();
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var line = new GameObject(PlateName + " Line", typeof(RectTransform));
            line.layer = plate.layer;
            var lineRect = (RectTransform)line.transform;
            lineRect.SetParent(_plate, false);
            lineRect.anchorMin = Vector2.zero;
            lineRect.anchorMax = Vector2.one;
            lineRect.offsetMin = new Vector2(PadX, PadY);
            lineRect.offsetMax = new Vector2(-PadX, -PadY);
            _label = line.AddComponent<Text>();
            _label.raycastTarget = false;
            Text source = area.GetComponentInChildren<Text>(true);
            _label.font = source != null && source.font != null ? source.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _label.fontSize = LineSize;
            _label.color = Ivory;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = LineMinSize;
            _label.resizeTextMaxSize = LineSize;
            _label.supportRichText = true;
            plate.SetActive(false);
            return true;
        }

        // The painted HUD's smoked band (the boss's name plate, as the boss's barks wear it), or the flat smoke without it.
        private void TakePlate()
        {
            HudArtView hud = FindFirstObjectByType<HudArtView>();
            if (hud == null || !hud.UsesPaintedArt || hud.Art == null || hud.Art.BossNamePlate == null)
                return;
            _plateImage.sprite = hud.Art.BossNamePlate;
            _plateImage.type = Image.Type.Simple;
            _plateImage.color = Color.white;
        }

        private void OnDisable()
        {
            if (_showing)
                Hide();
        }
    }
}
