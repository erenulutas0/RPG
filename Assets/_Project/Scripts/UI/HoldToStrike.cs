using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Cryptforge.UI
{
    // A press that must be held (docs/54 section 11: the Anvil's strike, 0.4 s) so a stray tap never spends gold: a finger
    // down starts the fill, lifting or sliding off before it is full cancels it, and a full fill raises Completed once. The
    // button only lends its interactable state and its look; its click does nothing. Unscaled time, so a paused run's
    // clock cannot stall it. Tests and captures press and release it directly.
    public sealed class HoldToStrike : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public const float Seconds = .4f;

        private Button _button;
        private Image _fill;
        private float _since;

        public bool IsHolding { get; private set; }
        public float Progress => IsHolding ? Mathf.Clamp01((Time.unscaledTime - _since) / Seconds) : 0f;
        public event Action Completed;

        internal void Initialize(Button button, Image fill)
        {
            _button = button;
            _fill = fill;
            Show(0f);
        }

        public void Press()
        {
            if (_button != null && (!_button.interactable || !_button.isActiveAndEnabled))
                return;
            IsHolding = true;
            _since = Time.unscaledTime;
            Show(0f);
        }

        public void Release()
        {
            IsHolding = false;
            Show(0f);
        }

        public void OnPointerDown(PointerEventData eventData) => Press();
        public void OnPointerUp(PointerEventData eventData) => Release();
        public void OnPointerExit(PointerEventData eventData) => Release();

        private void Update()
        {
            if (!IsHolding)
                return;
            if (_button != null && !_button.interactable)
            {
                Release();
                return;
            }
            float progress = Progress;
            Show(progress);
            if (progress < 1f)
                return;
            IsHolding = false;
            Show(0f);
            Completed?.Invoke();
        }

        private void OnDisable() => Release();

        private void Show(float progress)
        {
            if (_fill == null)
                return;
            _fill.fillAmount = progress;
            _fill.enabled = progress > 0f;
        }
    }
}
