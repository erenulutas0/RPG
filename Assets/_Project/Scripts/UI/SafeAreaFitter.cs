using UnityEngine;

namespace Cryptforge.UI
{
    // The Android player renders behind display cutouts, so HUD content is kept inside Screen.safeArea.
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _appliedArea;
        private int _appliedWidth;
        private int _appliedHeight;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void Update()
        {
            if (Screen.safeArea != _appliedArea || Screen.width != _appliedWidth || Screen.height != _appliedHeight)
                Apply();
        }

        private void Apply()
        {
            _appliedArea = Screen.safeArea;
            _appliedWidth = Screen.width;
            _appliedHeight = Screen.height;
            if (_appliedWidth <= 0 || _appliedHeight <= 0)
                return;

            var size = new Vector2(_appliedWidth, _appliedHeight);
            _rect.anchorMin = _appliedArea.position / size;
            _rect.anchorMax = (_appliedArea.position + _appliedArea.size) / size;
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
