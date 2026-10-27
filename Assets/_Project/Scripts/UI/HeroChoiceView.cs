using System.Collections.Generic;
using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The hero for the next run, chosen on the Relic Forge panel (docs/45, the Cinder Arcanist). A tap moves to the next
    // playable hero and the profile remembers it; Start then reloads the scene with that hero. It shares the start
    // button's row - the chooser on the left, the start button narrowed to the right - and its look, so the scene does
    // not change. Built in code by RelicForgeView when the roster has more than one hero. When a hero is still locked,
    // the chooser's second line says how to unlock it. A hero with a portrait stands at the chooser's left.
    public sealed class HeroChoiceView : MonoBehaviour
    {
        // The share of the start row the chooser takes, from the left.
        public const float RowShare = .4f;
        // The share of the chooser its portrait takes, from the left; the name fills the rest.
        public const float PortraitShare = .3f;
        private CombatSetup _setup;
        private PrototypeTextDefinition _text;
        private Button _button;
        private Text _label;
        private RectTransform _labelRect;
        private Image _portrait;
        private readonly List<HeroDefinition> _playable = new List<HeroDefinition>();

        public Button Button => _button;
        public string ShownText => _label != null ? _label.text : string.Empty;
        // The portrait on show, or null when the current hero has none.
        public Sprite ShownPortrait => _portrait != null && _portrait.enabled ? _portrait.sprite : null;
        // The hero the next run will play.
        public HeroDefinition Current { get; private set; }

        internal void Initialize(CombatSetup setup, PrototypeTextDefinition text, Button start, Text startLabel)
        {
            _setup = setup; _text = text;
            var startRect = (RectTransform)start.transform;
            var chooser = new GameObject("Hero Choice", typeof(RectTransform), typeof(Image), typeof(Button));
            chooser.layer = start.gameObject.layer;
            var rect = (RectTransform)chooser.transform;
            rect.SetParent(startRect.parent, false);
            rect.SetSiblingIndex(startRect.GetSiblingIndex());
            // The row keeps the start button's height and outer margins; a 16 px gap splits it.
            float margin = -startRect.sizeDelta.x / 2f;
            rect.anchorMin = new Vector2(0f, startRect.anchorMin.y);
            rect.anchorMax = new Vector2(RowShare, startRect.anchorMax.y);
            rect.pivot = startRect.pivot;
            rect.offsetMin = new Vector2(margin, 0f);
            rect.offsetMax = new Vector2(-8f, 0f);
            startRect.anchorMin = new Vector2(RowShare, startRect.anchorMin.y);
            startRect.offsetMin = new Vector2(8f, startRect.offsetMin.y);
            startRect.offsetMax = new Vector2(-margin, startRect.offsetMax.y);

            var image = chooser.GetComponent<Image>();
            if (start.image != null)
            {
                image.sprite = start.image.sprite;
                image.type = start.image.type;
                image.color = start.image.color * new Color(.8f, .8f, .8f, 1f);
            }
            _button = chooser.GetComponent<Button>();
            _button.targetGraphic = image;
            _button.onClick.AddListener(Cycle);

            // The figure keeps its proportions inside the left share, standing on the button's lower edge.
            var portraitObject = new GameObject("Hero Choice Portrait", typeof(RectTransform), typeof(Image));
            portraitObject.layer = chooser.layer;
            var portraitRect = (RectTransform)portraitObject.transform;
            portraitRect.SetParent(rect, false);
            portraitRect.anchorMin = Vector2.zero;
            portraitRect.anchorMax = new Vector2(PortraitShare, 1f);
            portraitRect.offsetMin = new Vector2(10f, 6f);
            portraitRect.offsetMax = new Vector2(0f, -6f);
            _portrait = portraitObject.GetComponent<Image>();
            _portrait.preserveAspect = true;
            _portrait.raycastTarget = false;
            _portrait.enabled = false;

            var labelObject = new GameObject("Hero Choice Label", typeof(RectTransform));
            labelObject.layer = chooser.layer;
            var labelRect = (RectTransform)labelObject.transform;
            labelRect.SetParent(rect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(16f, 6f);
            labelRect.offsetMax = new Vector2(-16f, -6f);
            _labelRect = labelRect;
            _label = labelObject.AddComponent<Text>();
            _label.font = startLabel.font;
            _label.fontSize = 30;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 18;
            _label.resizeTextMaxSize = 30;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = startLabel.color;
            _label.raycastTarget = false;

            _setup.Profile.Changed += Refresh;
            Refresh();
        }

        public void SetInteractable(bool interactable)
        {
            if (_button != null)
                _button.interactable = interactable;
        }

        // The next playable hero after the current one, back to the first after the last.
        public void Cycle()
        {
            Collect();
            if (_playable.Count < 2)
                return;
            int at = _playable.IndexOf(Current);
            HeroDefinition next = _playable[(at + 1) % _playable.Count];
            // The default hero is stored as no choice, so a profile from before heroes and one that chose it read alike.
            _setup.Profile.SelectHero(next == _setup.Heroes[0] ? null : next.Id);
        }

        private void Collect()
        {
            _playable.Clear();
            IReadOnlyList<HeroDefinition> heroes = _setup.Heroes;
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null && _setup.IsHeroPlayable(heroes[i]))
                    _playable.Add(heroes[i]);
            }
            Current = _playable.Count > 0 ? _playable[0] : null;
            string chosen = _setup.Profile.SelectedHeroId;
            for (int i = 0; i < _playable.Count; i++)
            {
                if (_playable[i].Id == chosen)
                    Current = _playable[i];
            }
        }

        private void Refresh()
        {
            if (_label == null)
                return;
            Collect();
            if (Current == null)
                return;
            // A locked hero says how to unlock it on the second line; otherwise the chosen hero's own name does.
            string second = Current.Subtitle;
            IReadOnlyList<HeroDefinition> heroes = _setup.Heroes;
            for (int i = 0; i < heroes.Count; i++)
            {
                if (heroes[i] != null && !_setup.IsHeroPlayable(heroes[i]) && heroes[i].UnlockHint.Length > 0)
                {
                    second = string.Format(_text.HeroLockedFormat, heroes[i].DisplayName, heroes[i].UnlockHint);
                    break;
                }
            }
            _label.text = string.Format(_text.HeroChoiceFormat, Current.DisplayName, second);
            Sprite portrait = Current.Portrait;
            _portrait.sprite = portrait;
            _portrait.enabled = portrait != null;
            // With a portrait the name moves right of it; without one it keeps the whole chooser.
            _labelRect.anchorMin = new Vector2(portrait != null ? PortraitShare : 0f, 0f);
        }

        private void OnDestroy()
        {
            if (_setup != null && _setup.Profile != null)
                _setup.Profile.Changed -= Refresh;
        }
    }
}
