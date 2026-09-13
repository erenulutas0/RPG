using Cryptforge.Content;
using Cryptforge.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // The HUD pause button and the pause overlay. The button shows only while the player can pause (a live run with no
    // choice open); the overlay follows RunPause, so leaving the app also shows it on return.
    public sealed class PauseView : MonoBehaviour
    {
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
        private bool _subscribed;

        public bool IsOpen => _panel != null && _panel.activeSelf;

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

            _pauseButtonLabel.text = _text.PauseButtonLabel;
            _titleLabel.text = _text.PauseTitle;
            _hintLabel.text = _text.PauseHint;
            _resumeLabel.text = _text.ResumeLabel;
            _panel.SetActive(false);
            _pauseButton.onClick.AddListener(OnPause);
            _resumeButton.onClick.AddListener(OnResume);
            _setup.Pause.Changed += Refresh;
            _setup.Run.Ended += Refresh;
            _subscribed = true;
            Refresh();
        }

        private void Refresh()
        {
            bool paused = _setup.Pause.IsPlayerPaused;
            if (paused != _panel.activeSelf)
            {
                _panel.SetActive(paused);
                // A tap that lands as the overlay appears, for example on returning to the app, cannot resume at once.
                _resumeButton.interactable = false;
                _inputEnabledAt = Time.unscaledTime + _inputDelay;
                _awaitingInputDelay = paused;
            }
            _pauseButton.gameObject.SetActive(_setup.Pause.CanPause);
        }

        private void Update()
        {
            if (!_awaitingInputDelay || Time.unscaledTime < _inputEnabledAt)
                return;

            _awaitingInputDelay = false;
            _resumeButton.interactable = true;
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
