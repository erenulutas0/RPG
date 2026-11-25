using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Localisation;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The HUD pause button and the pause overlay. The button shows only while the player can pause (a live run with no
    // choice open); the overlay follows RunPause, so leaving the app also shows it on return. The overlay is the game's
    // settings for now: the language toggle stands between Resume and the hint (docs/60), and "Skip reward videos" (docs/58
    // 7.4, the store's one product) above it, as low key.
    public sealed class PauseView : MonoBehaviour
    {
        // The toggle under Resume: this far above its top edge, this tall, this much narrower on each side.
        private const float LanguageGap = 64f;
        private const float LanguageHeight = 140f;
        private const float LanguageInset = 160f;
        private const int LanguageFontSize = 48;
        // The skip's entry above the toggle: this far above its top edge, this tall, as narrow.
        private const float SkipGap = 24f;
        private const float SkipHeight = 120f;
        private const int SkipFontSize = 44;
        [SerializeField] private CombatSetup _setup;
        [SerializeField] private PrototypeTextDefinition _text;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Text _pauseButtonLabel;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text _titleLabel;
        [SerializeField] private Text _hintLabel;
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Text _resumeLabel;
        [SerializeField, Min(0f)] private float _inputDelay = 0.3f;
        private float _inputEnabledAt;
        private bool _awaitingInputDelay;
        // The delay is stamped again on the next Update (HoldInputOnReturn).
        private bool _restampDelay;
        private bool _subscribed;
        private LanguageToggleView _language;
        private SkipVideosButton _skip;

        public bool IsOpen => _panel != null && _panel.activeSelf;
        // The language toggle on the overlay, for tests and captures.
        public LanguageToggleView LanguageToggle => _language;
        // "Skip reward videos" on the overlay, for tests and captures.
        public SkipVideosButton SkipVideos => _skip;

        private void Start()
        {
            if (_setup == null || _setup.Pause == null || _setup.Run == null || _text == null || _pauseButton == null ||
                _pauseButtonLabel == null || _panel == null || _titleLabel == null || _hintLabel == null ||
                _resumeButton == null || _resumeLabel == null)
            {
                Debug.LogError("PauseView is missing a scene or content reference.", this);
                enabled = false;
                return;
            }

            LocalisedLabel.Bind(_pauseButtonLabel, () => _text.PauseButtonLabel);
            LocalisedLabel.Bind(_titleLabel, () => _text.PauseTitle);
            LocalisedLabel.Bind(_hintLabel, () => _text.PauseHint);
            LocalisedLabel.Bind(_resumeLabel, () => _text.ResumeLabel);
            BuildLanguageToggle();
            _panel.SetActive(false);
            _pauseButton.onClick.AddListener(OnPause);
            _resumeButton.onClick.AddListener(OnResume);
            _setup.Pause.Changed += Refresh;
            _setup.Run.Ended += Refresh;
            _subscribed = true;
            Refresh();
        }

        // Under Resume, in the resume button's parent and frame (bottom-anchored on the overlay's safe area), narrower and
        // lower-key, so Resume stays the overlay's first answer.
        private void BuildLanguageToggle()
        {
            var resume = (RectTransform)_resumeButton.transform;
            if (resume.parent is not RectTransform parent)
                return;
            _language = LanguageToggleView.Create(parent, _resumeLabel, LanguageFontSize, resume.anchorMin, resume.anchorMax,
                resume.pivot,
                new Vector2(resume.anchoredPosition.x, resume.anchoredPosition.y + resume.sizeDelta.y + LanguageGap),
                new Vector2(resume.sizeDelta.x - 2f * LanguageInset, LanguageHeight));
            // The settings' other entry, on the same plate and as wide, above the toggle and under the hint.
            float skipY = resume.anchoredPosition.y + resume.sizeDelta.y + LanguageGap + LanguageHeight + SkipGap;
            _skip = SkipVideosButton.Create(parent, SkipVideosButton.ObjectName, _resumeLabel, SkipFontSize, true,
                resume.anchorMin, resume.anchorMax, resume.pivot, new Vector2(resume.anchoredPosition.x, skipY),
                new Vector2(resume.sizeDelta.x - 2f * LanguageInset, SkipHeight));
        }

        private void Refresh()
        {
            bool paused = _setup.Pause.IsPlayerPaused;
            if (paused != _panel.activeSelf)
            {
                _panel.SetActive(paused);
                // A tap that lands as the overlay appears, for example on returning to the app, cannot resume at once.
                HoldInput(paused);
            }
            _pauseButton.gameObject.SetActive(_setup.Pause.CanPause);
        }

        // The overlay's buttons take no tap until the input delay has run, counted from now when awaiting.
        private void HoldInput(bool awaiting)
        {
            _resumeButton.interactable = false;
            if (_language != null)
                _language.Button.interactable = false;
            if (_skip != null)
                _skip.SetInputEnabled(false);
            _inputEnabledAt = Time.unscaledTime + _inputDelay;
            _awaitingInputDelay = awaiting;
            _restampDelay = false;
        }

        // Back in the app with the overlay open, its delay runs again. The one started as the app left (OnApplicationPause
        // (true) opens the overlay) ran out in the background, where unscaled time jumps, so the stray tap that brings the
        // player back (a call's end button) resumed at once.
        private void OnApplicationPause(bool paused)
        {
            if (!paused)
                HoldInputOnReturn();
        }

        private void OnApplicationFocus(bool focused)
        {
            if (focused)
                HoldInputOnReturn();
        }

        private void HoldInputOnReturn()
        {
            if (!_subscribed || !IsOpen)
                return;
            HoldInput(true);
            // The frame's clock may not have caught up with the time away yet: the delay counts from the first frame back.
            _restampDelay = true;
        }

        private void Update()
        {
            if (!_awaitingInputDelay)
                return;
            if (_restampDelay)
            {
                _restampDelay = false;
                _inputEnabledAt = Time.unscaledTime + _inputDelay;
                return;
            }
            if (Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            _resumeButton.interactable = true;
            if (_language != null)
                _language.Button.interactable = true;
            if (_skip != null)
                _skip.SetInputEnabled(true);
        }

        private void OnPause() => _setup.Pause.TryPause();

        private void OnResume()
        {
            if (_awaitingInputDelay || !_resumeButton.interactable)
                return;

            _setup.Pause.TryResume();
        }

        private void OnDestroy()
        {
            if (!_subscribed)
                return;

            _setup.Pause.Changed -= Refresh;
            _setup.Run.Ended -= Refresh;
            if (_pauseButton != null)
                _pauseButton.onClick.RemoveListener(OnPause);
            if (_resumeButton != null)
                _resumeButton.onClick.RemoveListener(OnResume);
        }
    }
}
