using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The between-runs and pause screens painted (ScreenArtSet, Tools/ArtReview/build_screen_art.py), as HudArtView paints
    // the HUD: it restyles the scene's own images in place and lays painted parts beside them, placed by anchors and
    // offsets relative to the scene's own rects, so they follow the safe area as those rects do.
    //   - The Relic Forge: its surface; a nameplate behind its title; a rule under each section's header; each card's frame
    //     in the state RelicForgeView shows it in (CardShown) and its state strip; a well behind each relic's picture, the
    //     two dimmed together while the relic is out of reach; a seal on a rule in each place of the weapon rack a hero
    //     leaves empty (Ilya carries her Staff alone); the start button; and the hero chooser - its plate, a niche round
    //     its portrait and a pip a hero along its foot, the chosen one lit.
    //   - The run result: the ledger's surface; the title's plate and crest, gilded and alight for a victory or an
    //     extraction, cracked and gone cold for a defeat; the divider as a rule; its two buttons.
    //   - The pause: a plate behind its title and hint; its resume button.
    // Buttons swap to their painted pressed and disabled plates; cards keep a colour tint, neutral now that each state has
    // its own frame. No rect the layout or the tests read moves, no text changes and nothing painted catches a tap.
    // Without the set, or with a broken one, nothing is touched: today's look is the fallback.
    public sealed class ScreenArtView : MonoBehaviour
    {
        // Pressed, a card darkens; one that cannot be tapped (out of reach, or for the moment after each change) dims a
        // little. Its frame carries its state.
        private static readonly Color CardPressed = new Color(.8f, .8f, .8f, 1f);
        private static readonly Color CardDisabled = new Color(.86f, .86f, .86f, 1f);
        // A relic out of reach: its picture and its well dimmed alike, so the picture's own navy still meets the well's.
        private static readonly Color LockedPicture = new Color(.55f, .55f, .6f, 1f);

        [SerializeField] private ScreenArtSet _art;
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private RelicForgeView _forge;
        [SerializeField] private Image _forgeSurface;
        [SerializeField] private Text _forgeTitle;
        [SerializeField] private Text[] _forgeHeaders;
        // In RelicForgeView's order: card i shows item i.
        [SerializeField] private Button[] _weaponCards;
        [SerializeField] private Button[] _relicCards;
        [SerializeField] private Image[] _relicPictures;
        [SerializeField] private Image[] _stateAccents;
        [SerializeField] private Button _startButton;
        [SerializeField] private Image _resultSurface;
        [SerializeField] private Text _resultTitle;
        [SerializeField] private Image _resultDivider;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Button _forgeButton;
        [SerializeField] private Text _pauseTitle;
        [SerializeField] private Text _pauseHint;
        [SerializeField] private Button _resumeButton;
        private Image[] _wells = Array.Empty<Image>();
        private GameObject[] _emptyPlaces = Array.Empty<GameObject>();
        private readonly List<Image> _headerRules = new List<Image>();
        private readonly List<Image> _pips = new List<Image>();
        private HeroDefinition _pipHero;
        private bool _chooserChecked;
        private bool _rackChanged;
        private bool _subscribed;

        public bool UsesPaintedArt { get; private set; }
        public ScreenArtSet Art => _art;
        public Image HeaderPlate { get; private set; }
        public IReadOnlyList<Image> HeaderRules => _headerRules;
        // In the relic cards' order.
        public IReadOnlyList<Image> IconWells => _wells;
        public Image PortraitFrame { get; private set; }
        // One a hero of the roster, in its order.
        public IReadOnlyList<Image> HeroPips => _pips;
        public Image TitlePlate { get; private set; }
        public Image TitleCrest { get; private set; }
        public Image DividerRule { get; private set; }
        public Image PausePlate { get; private set; }

        // The painted mark in the weapon rack's place slot, shown while that place's card is hidden.
        public GameObject EmptyPlace(int slot) => slot >= 0 && slot < _emptyPlaces.Length ? _emptyPlaces[slot] : null;

        private void Start()
        {
            if (_art == null || !_art.IsValid)
            {
                enabled = false;
                return;
            }
            PaintForge();
            PaintResult();
            PaintPause();
            if (_forge != null)
                _forge.CardShown += Dress;
            if (_setup != null && _setup.Run != null)
            {
                _setup.Run.Ended += ShowOutcome;
                if (_setup.Run.HasEnded)
                    ShowOutcome();
            }
            _subscribed = true;
            UsesPaintedArt = true;
        }

        // After every Start, so the forge's hero chooser exists; and after the forge has refreshed, so the rack's empty
        // places and the pips show what the forge now does.
        private void LateUpdate()
        {
            if (!_chooserChecked)
            {
                _chooserChecked = true;
                PaintChooser();
            }
            if (!_rackChanged)
                return;
            _rackChanged = false;
            for (int i = 0; i < _emptyPlaces.Length; i++)
            {
                bool empty = _weaponCards[i] != null && !_weaponCards[i].gameObject.activeSelf;
                if (_emptyPlaces[i] != null && _emptyPlaces[i].activeSelf != empty)
                    _emptyPlaces[i].SetActive(empty);
            }
            ShowPips();
        }

        private void PaintForge()
        {
            Restyle(_forgeSurface, _art.Surface);
            if (_forgeTitle != null)
            {
                RectTransform title = _forgeTitle.rectTransform;
                HeaderPlate = Behind("Painted Forge Nameplate", title, _art.HeaderPlate, Image.Type.Sliced);
                CentreOn(HeaderPlate.rectTransform, title, new Vector2(_art.HeaderPlateWidth, _art.HeaderPlate.rect.height));
            }
            if (_forgeHeaders != null)
            {
                float height = _art.Rule.rect.height, lift = _art.HeaderRuleLift;
                foreach (Text header in _forgeHeaders)
                {
                    if (header == null)
                        continue;
                    Image rule = Child("Painted Header Rule", header.rectTransform, _art.Rule, Image.Type.Sliced);
                    Stretch(rule.rectTransform, Vector2.zero, Vector2.right, new Vector2(0f, lift - height / 2f),
                        new Vector2(0f, lift + height / 2f));
                    _headerRules.Add(rule);
                }
            }
            if (_weaponCards != null)
            {
                _emptyPlaces = new GameObject[_weaponCards.Length];
                for (int i = 0; i < _weaponCards.Length; i++)
                {
                    if (_weaponCards[i] == null)
                        continue;
                    PaintCard(_weaponCards[i]);
                    _emptyPlaces[i] = MakeEmptyPlace((RectTransform)_weaponCards[i].transform, i);
                }
            }
            if (_relicCards != null)
                foreach (Button card in _relicCards)
                    if (card != null)
                        PaintCard(card);
            if (_stateAccents != null)
                foreach (Image accent in _stateAccents)
                {
                    // RelicForgeView keeps colouring it in the state's colour.
                    if (accent == null)
                        continue;
                    accent.sprite = _art.StateAccent;
                    accent.type = Image.Type.Sliced;
                    accent.pixelsPerUnitMultiplier = 1f;
                }
            if (_relicPictures != null)
            {
                _wells = new Image[_relicPictures.Length];
                float margin = _art.IconWellMargin;
                for (int i = 0; i < _relicPictures.Length; i++)
                {
                    if (_relicPictures[i] == null)
                        continue;
                    RectTransform picture = _relicPictures[i].rectTransform;
                    // Fitted, keeping its aspect, to the picture's rect grown by the margin: it rings the square the
                    // picture draws (which keeps its own aspect) at any card height.
                    _wells[i] = Behind("Painted Icon Well", picture, _art.IconWell, Image.Type.Simple);
                    _wells[i].preserveAspect = true;
                    Around(_wells[i].rectTransform, picture, new Vector4(margin, margin, margin, margin));
                }
            }
            PaintButton(_startButton, _art.Primary, _art.PrimaryPressed, _art.PrimaryDisabled);
        }

        // The weapon rack's place slot, painted for when its card is hidden: a seal on a rule across its middle.
        private GameObject MakeEmptyPlace(RectTransform card, int slot)
        {
            var place = new GameObject($"Painted Empty Place {slot + 1}", typeof(RectTransform));
            place.layer = card.gameObject.layer;
            var rect = (RectTransform)place.transform;
            rect.SetParent(card.parent, false);
            rect.SetSiblingIndex(card.GetSiblingIndex());
            Around(rect, card, Vector4.zero);
            float share = _art.EmptySlotRuleShare, height = _art.Rule.rect.height;
            Image rule = Child("Painted Rule", rect, _art.Rule, Image.Type.Sliced);
            Stretch(rule.rectTransform, new Vector2(.5f - share / 2f, .5f), new Vector2(.5f + share / 2f, .5f),
                new Vector2(0f, -height / 2f), new Vector2(0f, height / 2f));
            Image seal = Child("Painted Seal", rect, _art.Seal, Image.Type.Simple);
            Centred(seal.rectTransform, _art.Seal.rect.size);
            place.SetActive(!card.gameObject.activeSelf);
            return place;
        }

        private void PaintChooser()
        {
            HeroChoiceView choice = _forge != null ? _forge.HeroChoice : null;
            if (choice == null || choice.Button == null)
                return;
            PaintButton(choice.Button, _art.Secondary, _art.SecondaryPressed, _art.SecondaryDisabled);
            if (choice.Portrait != null)
            {
                RectTransform portrait = choice.Portrait.rectTransform;
                PortraitFrame = Behind("Painted Portrait Frame", portrait, _art.PortraitFrame, Image.Type.Sliced);
                Around(PortraitFrame.rectTransform, portrait, _art.PortraitFrameOutset);
            }
            if (_setup == null)
                return;
            // Along the chooser's foot, under its words: one a hero, straddling the edge.
            var chooser = (RectTransform)choice.Button.transform;
            int count = _setup.Heroes.Count;
            float across = (HeroChoiceView.PortraitShare + 1f) / 2f;
            Vector2 size = _art.PipLit.rect.size;
            for (int i = 0; i < count; i++)
            {
                float x = (i - (count - 1) / 2f) * _art.PipSpacing;
                Image pip = Child($"Painted Hero Pip {i + 1}", chooser, _art.PipDark, Image.Type.Simple);
                Stretch(pip.rectTransform, new Vector2(across, 0f), new Vector2(across, 0f), new Vector2(x, 0f) - size / 2f,
                    new Vector2(x, 0f) + size / 2f);
                _pips.Add(pip);
            }
            ShowPips();
        }

        private void ShowPips()
        {
            if (_pips.Count == 0 || _forge == null || _forge.HeroChoice == null)
                return;
            HeroDefinition current = _forge.HeroChoice.Current;
            if (current == _pipHero)
                return;
            _pipHero = current;
            IReadOnlyList<HeroDefinition> heroes = _setup.Heroes;
            for (int i = 0; i < _pips.Count; i++)
                _pips[i].sprite = i < heroes.Count && heroes[i] != null && heroes[i] == current ? _art.PipLit : _art.PipDark;
        }

        private void PaintResult()
        {
            Restyle(_resultSurface, _art.Surface);
            if (_resultTitle != null)
            {
                RectTransform title = _resultTitle.rectTransform;
                TitlePlate = Behind("Painted Title Plate", title, _art.VictoryPlate, Image.Type.Sliced);
                Around(TitlePlate.rectTransform, title, _art.TitlePlateOutset);
                // Standing on the plate's top edge, its foot sunk into the plate's rim.
                TitleCrest = Child("Painted Title Crest", TitlePlate.rectTransform, _art.VictoryCrest, Image.Type.Simple);
                ShowOutcome();
            }
            if (_resultDivider != null)
            {
                // The flat line gives way to the rule; its rect stays.
                _resultDivider.enabled = false;
                RectTransform divider = _resultDivider.rectTransform;
                float height = _art.Rule.rect.height;
                DividerRule = Child("Painted Divider", divider, _art.Rule, Image.Type.Sliced);
                Stretch(DividerRule.rectTransform, new Vector2(0f, .5f), new Vector2(1f, .5f), new Vector2(0f, -height / 2f),
                    new Vector2(0f, height / 2f));
                Image stud = Child("Painted Divider Stud", divider, _art.Stud, Image.Type.Simple);
                Centred(stud.rectTransform, _art.Stud.rect.size);
            }
            PaintButton(_restartButton, _art.Primary, _art.PrimaryPressed, _art.PrimaryDisabled);
            PaintButton(_forgeButton, _art.Secondary, _art.SecondaryPressed, _art.SecondaryDisabled);
        }

        // The title's plate and crest for how the run ended: a defeat cracked and gone cold, a victory and an extraction
        // (which RunResultView colours alike) gilded and alight.
        private void ShowOutcome()
        {
            if (TitlePlate == null)
                return;
            bool lost = _setup != null && _setup.Run != null && _setup.Run.Outcome == RunOutcome.Defeat;
            TitlePlate.sprite = lost ? _art.DefeatPlate : _art.VictoryPlate;
            Sprite crest = lost ? _art.DefeatCrest : _art.VictoryCrest;
            TitleCrest.sprite = crest;
            Vector2 size = crest.rect.size;
            float sink = _art.CrestSink;
            Stretch(TitleCrest.rectTransform, new Vector2(.5f, 1f), new Vector2(.5f, 1f), new Vector2(-size.x / 2f, -sink),
                new Vector2(size.x / 2f, size.y - sink));
        }

        private void PaintPause()
        {
            if (_pauseTitle != null)
            {
                RectTransform title = _pauseTitle.rectTransform;
                Vector4 outset = _art.PausePlateOutset;
                PausePlate = Behind("Painted Pause Plate", title, _art.PausePlate, Image.Type.Sliced);
                Around(PausePlate.rectTransform, title, outset);
                RectTransform hint = _pauseHint != null ? _pauseHint.rectTransform : null;
                // Round the hint as well, when the two share their anchors (their offsets are then in one frame).
                if (hint != null && hint.parent == title.parent && hint.anchorMin == title.anchorMin &&
                    hint.anchorMax == title.anchorMax)
                {
                    PausePlate.rectTransform.offsetMin = Vector2.Min(title.offsetMin, hint.offsetMin) - new Vector2(outset.x, outset.y);
                    PausePlate.rectTransform.offsetMax = Vector2.Max(title.offsetMax, hint.offsetMax) + new Vector2(outset.z, outset.w);
                }
            }
            PaintButton(_resumeButton, _art.Primary, _art.PrimaryPressed, _art.PrimaryDisabled);
        }

        // The forge has shown a card in a state: its frame for that state and, for a relic, its picture and well dimmed
        // while it is out of reach.
        private void Dress(Button card, UnlockStatus status)
        {
            if (card == null)
                return;
            if (card.image != null)
                card.image.sprite = _art.CardFor(status);
            int slot = _relicCards != null ? Array.IndexOf(_relicCards, card) : -1;
            if (slot >= 0 && slot < _wells.Length && _wells[slot] != null)
            {
                Color tint = status == UnlockStatus.TooExpensive ? LockedPicture : Color.white;
                _wells[slot].color = tint;
                _relicPictures[slot].color = tint;
            }
            _rackChanged = true;
        }

        private void PaintCard(Button card)
        {
            Restyle(card.image, _art.CardFor(UnlockStatus.Owned));
            ColorBlock colours = card.colors;
            colours.normalColor = Color.white;
            colours.highlightedColor = Color.white;
            colours.selectedColor = Color.white;
            colours.pressedColor = CardPressed;
            colours.disabledColor = CardDisabled;
            card.colors = colours;
        }

        // A sprite swap onto the painted plates; the old colour tint's last colour goes with it.
        private static void PaintButton(Button button, Sprite normal, Sprite pressed, Sprite disabled)
        {
            if (button == null || !(button.targetGraphic is Image image))
                return;
            Restyle(image, normal);
            button.transition = Selectable.Transition.SpriteSwap;
            button.spriteState = new SpriteState { pressedSprite = pressed, disabledSprite = disabled };
            image.CrossFadeColor(Color.white, 0f, true, true);
        }

        private static void Restyle(Image image, Sprite sprite)
        {
            if (image == null)
                return;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.fillCenter = true;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
        }

        // A painted part drawn just before target (so under it), in its parent.
        private static Image Behind(string name, RectTransform target, Sprite sprite, Image.Type type)
        {
            Image image = Child(name, (RectTransform)target.parent, sprite, type);
            image.transform.SetSiblingIndex(target.GetSiblingIndex());
            return image;
        }

        // A painted part laid last in parent, never catching a tap.
        private static Image Child(string name, RectTransform parent, Sprite sprite, Image.Type type)
        {
            var part = new GameObject(name, typeof(RectTransform));
            part.layer = parent.gameObject.layer;
            part.transform.SetParent(parent, false);
            var image = part.AddComponent<Image>();
            image.sprite = sprite;
            image.type = type;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // The target's rect grown by the outset (left, bottom, right, top), on the target's own anchors.
        private static void Around(RectTransform rect, RectTransform target, Vector4 outset) =>
            Stretch(rect, target.anchorMin, target.anchorMax, target.offsetMin - new Vector2(outset.x, outset.y),
                target.offsetMax + new Vector2(outset.z, outset.w));

        // A fixed size centred on the target, anchored at the middle of its anchors.
        private static void CentreOn(RectTransform rect, RectTransform target, Vector2 size)
        {
            Vector2 middle = (target.anchorMin + target.anchorMax) / 2f;
            Vector2 centre = (target.offsetMin + target.offsetMax) / 2f;
            Stretch(rect, middle, middle, centre - size / 2f, centre + size / 2f);
        }

        // A fixed size at the middle of its parent.
        private static void Centred(RectTransform rect, Vector2 size)
        {
            var middle = new Vector2(.5f, .5f);
            Stretch(rect, middle, middle, -size / 2f, size / 2f);
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;
            if (_forge != null)
                _forge.CardShown -= Dress;
            if (_setup != null && _setup.Run != null)
                _setup.Run.Ended -= ShowOutcome;
        }
    }
}
