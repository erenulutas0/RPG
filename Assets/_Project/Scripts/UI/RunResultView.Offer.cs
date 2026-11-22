using System;
using System.Collections.Generic;
using Cryptforge.Localisation;
using Cryptforge.Monetisation;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The result screen's one rewarded offer (docs/58 sections 1, 3, 5a and 7.4) over the launch's RewardOfferCoordinator
    // (MonetisationRuntime). Show opens it for the run - its id, its banked gold, CombatSetup.ResultSaved as the check that
    // the result is on disk, and the scene's PlayerProfile.Deposit as where the bonus goes - and draws one button from the
    // offer's state: Video "Watch a video: +N gold", Skip "Skip owned: +N gold" (the skip's owner, never a video), NoAd "No
    // video right now" (shown and tappable: it asks for one); any other state shows no button. A tap is the coordinator's
    // Take. Its Changed draws the button again, and the gold label (the banked gold and the bonus once credited) and the
    // forge's hint with it; LastResult gives the plain words of how this run's video ended. Under the button, while the
    // offer stands and the skip is not owned, a small "Skip reward videos" link (SkipVideosButton). Restarting or leaving
    // closes the offer (declined, or as its ask or its failure ended).
    //
    // The scene lays the ledger out for the summary alone. When the offer shows, its row takes OfferBand units under the
    // gold: the parts above the gold label's foot rise by half of it, the parts below sink by half, and the surface grows
    // by all of it, so the ledger stays centred and fits the shortest safe area (1760 units, 105 to spare at each end). The
    // row stays for the rest of the screen, its words in the button's place once the offer is over, so nothing jumps.
    public sealed partial class RunResultView
    {
        public const string OfferButtonName = "Offer Button";
        public const string OfferNoteName = "Offer Note";
        public const string SkipLinkName = "Skip Videos Link";
        // The room the offer's row takes under the gold.
        public const float OfferBand = 180f;
        private const float OfferGap = 12f;
        private const float OfferButtonWidth = 760f;
        private const float OfferButtonHeight = 100f;
        private const float LinkGap = 6f;
        private const float LinkWidth = 760f;
        private const float LinkHeight = 52f;
        private const int OfferFontSize = 38;
        private const int NoteFontSize = 30;
        private const int LinkFontSize = 28;
        private const string SurfaceName = "Result Surface";

        private RewardOfferCoordinator _offer;
        private string _offerRunId;
        private bool _offerSubscribed;
        private Button _offerButton;
        private Text _offerLabel;
        private Text _offerNote;
        private SkipVideosButton _skipLink;
        private bool _offerRow;
        // The panel's own parts where the scene placed them, before the row moved them, and on which side of the gold
        // label's foot each stands; the surface and its size.
        private List<RectTransform> _laidOut;
        private List<Vector2> _laidOutAt;
        private List<bool> _laidOutAbove;
        private RectTransform _surface;
        private Vector2 _surfaceSize;

        // For tests and captures.
        public Button OfferButton => _offerButton;
        public bool OfferButtonShown => _offerButton != null && _offerButton.gameObject.activeSelf;
        public string OfferButtonText => _offerLabel != null ? _offerLabel.text : string.Empty;
        public bool OfferNoteShown => _offerNote != null && _offerNote.gameObject.activeSelf;
        public string OfferNoteText => _offerNote != null ? _offerNote.text : string.Empty;
        public SkipVideosButton SkipLink => _skipLink;
        public bool SkipLinkShown => _skipLink != null && _skipLink.gameObject.activeSelf;
        // The offer's row has made room under the gold.
        public bool OfferRowShown => _offerRow;
        // The offer's state as last drawn; None while no result is open.
        public RewardOfferState OfferState { get; private set; }
        // The run whose offer this screen opened; null before the result shows and once it closed.
        public string OfferRunId => _offerRunId;

        // The bonus credited to this screen's run, which the gold label adds to the banked gold; 0 before a credit.
        private int OfferCreditedGold =>
            _offer != null && _offerRunId != null && _offer.LastCredited &&
            string.Equals(_offer.LastResultRunId, _offerRunId, StringComparison.Ordinal) &&
            string.Equals(_offer.RunId, _offerRunId, StringComparison.Ordinal)
                ? _offer.BonusGold
                : 0;

        // The offer's button, its words and the link, built hidden beside the gold label (Start).
        private void BuildOffer()
        {
            RectTransform gold = _goldLabel.rectTransform;
            if (!(gold.parent is RectTransform area))
                return;

            var root = new GameObject(OfferButtonName, typeof(RectTransform), typeof(Image), typeof(Button));
            root.layer = area.gameObject.layer;
            var rect = (RectTransform)root.transform;
            rect.SetParent(area, false);
            rect.SetSiblingIndex(gold.GetSiblingIndex() + 1);
            _offerButton = root.GetComponent<Button>();
            _offerButton.targetGraphic = root.GetComponent<Image>();
            // The forge button's secondary plate: the offer is never the screen's first answer.
            LanguageToggleView.PaintPlate(_offerButton);
            _offerLabel = Words(rect, OfferButtonName + " Label", _forgeButtonLabel, OfferFontSize);
            Stretch(_offerLabel.rectTransform, new Vector2(24f, 8f), new Vector2(-24f, -8f));
            _offerButton.onClick.AddListener(OnOfferTapped);
            root.SetActive(false);

            _offerNote = Words(area, OfferNoteName, _goldLabel, NoteFontSize);
            _offerNote.transform.SetSiblingIndex(rect.GetSiblingIndex() + 1);
            _offerNote.gameObject.SetActive(false);

            _skipLink = SkipVideosButton.Create(area, SkipLinkName, _forgeHintLabel, LinkFontSize, false, gold.anchorMin,
                gold.anchorMax, new Vector2(.5f, .5f), Vector2.zero, new Vector2(LinkWidth, LinkHeight));
            _skipLink.transform.SetSiblingIndex(_offerNote.transform.GetSiblingIndex() + 1);
            _skipLink.gameObject.SetActive(false);
        }

        // Show: the offer opens for this run. Its row makes room when the offer shows at once (a later state never asks
        // for it: a result unsaved or too poor stays so, and an owner's purchase turns Video into Skip in the same row).
        private void OpenOffer()
        {
            if (_offerButton == null)
                return;
            CloseOffer();
            RewardOfferCoordinator offer = MonetisationRuntime.Offer;
            string runId = _setup.RunId;
            RewardOfferState state = offer.Open(runId, _setup.Run.GoldBanked, () => _setup.ResultSaved, _setup.Profile.Deposit);
            _offer = offer;
            _offerRunId = runId;
            offer.Changed += OnOfferChanged;
            MonetisationRuntime.Changed += OnOfferChanged;
            Loc.Changed += OnOfferChanged;
            _offerSubscribed = true;
            LayoutForOffer(RewardOfferRule.IsShown(state));
            RedrawOffer();
        }

        private void OnOfferChanged() => RedrawOffer();

        private void RedrawOffer()
        {
            if (_offerButton == null)
                return;
            bool open = _offer != null && _offerRunId != null && string.Equals(_offer.RunId, _offerRunId, StringComparison.Ordinal);
            RewardOfferState state = open ? _offer.Refresh() : RewardOfferState.None;
            OfferState = state;
            bool offered = RewardOfferRule.IsShown(state);
            bool button = _offerRow && offered;
            if (button)
                _offerLabel.text = RewardOfferText.Button(state, _offer.BonusGold);
            SetShown(_offerButton.gameObject, button);

            // How this run's video ended, never the last run's; nothing while a video is on screen.
            bool ours = open && _offer.LastResult.HasValue &&
                        string.Equals(_offer.LastResultRunId, _offerRunId, StringComparison.Ordinal);
            string words = _offerRow && ours && state != RewardOfferState.Showing
                ? RewardOfferText.Outcome(_offer.LastResult.Value, _offer.LastCredited, offered, _offer.BonusGold)
                : string.Empty;
            // While the offer still stands (a failure's retry) the words take the link's line; once it is over, the
            // button's place.
            if (_offerRow)
                Place(_offerNote.rectTransform, button ? LinkCentre() : ButtonCentre(), button ? LinkWidth : OfferButtonWidth,
                    button ? LinkHeight : OfferButtonHeight);
            if (_offerNote.text != words)
                _offerNote.text = words;
            SetShown(_offerNote.gameObject, words.Length > 0);

            bool link = button && words.Length == 0 && !MonetisationRuntime.OwnsSkip;
            SetShown(_skipLink.gameObject, link);
            if (link)
                _skipLink.Refresh();

            if (open)
            {
                DrawGold();
                _forgeHintLabel.text = DescribeForge();
            }
        }

        private void OnOfferTapped()
        {
            if (_awaitingInputDelay || _offerButton == null || !_offerButton.interactable || _offer == null ||
                !string.Equals(_offer.RunId, _offerRunId, StringComparison.Ordinal))
                return;
            _offer.Take();
            RedrawOffer();
        }

        // The input delay that guards the screen's two buttons guards the offer and its link too.
        private void SetOfferInteractable(bool interactable)
        {
            if (_offerButton != null)
                _offerButton.interactable = interactable;
            if (_skipLink != null)
                _skipLink.SetInputEnabled(interactable);
        }

        // The screen is left (Restart) or goes with its scene: its offer ends (RewardOfferCoordinator.Close) and the lines
        // that wrote reach the file now. Only this screen's own: a later screen's offer is never closed from here.
        private void CloseOffer()
        {
            RewardOfferCoordinator offer = _offer;
            string runId = _offerRunId;
            if (_offerSubscribed)
            {
                _offerSubscribed = false;
                if (offer != null)
                    offer.Changed -= OnOfferChanged;
                MonetisationRuntime.Changed -= OnOfferChanged;
                Loc.Changed -= OnOfferChanged;
            }
            _offer = null;
            _offerRunId = null;
            OfferState = RewardOfferState.None;
            if (offer != null && runId != null && string.Equals(offer.RunId, runId, StringComparison.Ordinal))
            {
                offer.Close();
                MonetisationRuntime.Flush();
            }
        }

        private void DestroyOffer()
        {
            CloseOffer();
            if (_offerButton != null)
                _offerButton.onClick.RemoveListener(OnOfferTapped);
        }

        // Makes room for the row under the gold, or gives it back.
        private void LayoutForOffer(bool row)
        {
            if (row == _offerRow)
                return;
            if (_laidOut == null)
                CaptureLayout();
            float half = row ? OfferBand / 2f : 0f;
            for (int i = 0; i < _laidOut.Count; i++)
            {
                if (_laidOut[i] != null)
                    _laidOut[i].anchoredPosition = _laidOutAt[i] + new Vector2(0f, _laidOutAbove[i] ? half : -half);
            }
            if (_surface != null)
                _surface.sizeDelta = _surfaceSize + new Vector2(0f, row ? OfferBand : 0f);
            _offerRow = row;
            if (!row)
                return;
            Place((RectTransform)_offerButton.transform, ButtonCentre(), OfferButtonWidth, OfferButtonHeight);
            Place((RectTransform)_skipLink.transform, LinkCentre(), LinkWidth, LinkHeight);
            Place(_offerNote.rectTransform, ButtonCentre(), OfferButtonWidth, OfferButtonHeight);
        }

        // Every part of the safe area but the surface and the row's own, as the scene and the painted screens placed them,
        // and whether its middle stands above the gold label's foot. Taken once, at the first result: every Start has run.
        private void CaptureLayout()
        {
            RectTransform gold = _goldLabel.rectTransform;
            var area = (RectTransform)gold.parent;
            float foot = gold.localPosition.y + gold.rect.yMin;
            _surface = area.Find(SurfaceName) as RectTransform;
            _surfaceSize = _surface != null ? _surface.sizeDelta : Vector2.zero;
            _laidOut = new List<RectTransform>();
            _laidOutAt = new List<Vector2>();
            _laidOutAbove = new List<bool>();
            for (int i = 0; i < area.childCount; i++)
            {
                if (!(area.GetChild(i) is RectTransform child) || child == _surface || child == _offerButton.transform ||
                    child == _offerNote.transform || child == _skipLink.transform)
                    continue;
                _laidOut.Add(child);
                _laidOutAt.Add(child.anchoredPosition);
                _laidOutAbove.Add(child.localPosition.y + child.rect.center.y > foot);
            }
        }

        // The row's lines, in the gold label's frame: the button under the gold's foot, the link under the button.
        private float GoldFoot()
        {
            RectTransform gold = _goldLabel.rectTransform;
            return gold.anchoredPosition.y + gold.rect.yMin;
        }

        private float ButtonCentre() => GoldFoot() - OfferGap - OfferButtonHeight / 2f;

        private float LinkCentre() => GoldFoot() - OfferGap - OfferButtonHeight - LinkGap - LinkHeight / 2f;

        private void Place(RectTransform rect, float centreY, float width, float height)
        {
            RectTransform gold = _goldLabel.rectTransform;
            Vector2 middle = (gold.anchorMin + gold.anchorMax) / 2f;
            rect.anchorMin = rect.anchorMax = middle;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(gold.anchoredPosition.x + gold.rect.center.x, centreY);
        }

        private static void SetShown(GameObject part, bool shown)
        {
            if (part.activeSelf != shown)
                part.SetActive(shown);
        }

        // A label in the style of another (its font, style and colour) at size, best fit down to half of it.
        private static Text Words(Transform parent, string name, Text style, int size)
        {
            var labelObject = new GameObject(name, typeof(RectTransform));
            labelObject.layer = parent.gameObject.layer;
            labelObject.transform.SetParent(parent, false);
            var label = labelObject.AddComponent<Text>();
            label.font = style.font != null ? style.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontStyle = style.fontStyle;
            label.color = style.color;
            label.fontSize = size;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = Mathf.Max(14, size / 2);
            label.resizeTextMaxSize = size;
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(.5f, .5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
