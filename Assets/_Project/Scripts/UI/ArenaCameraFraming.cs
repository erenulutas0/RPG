using UnityEngine;

namespace Cryptforge.UI
{
    // Keeps the arena between the HUD's top and bottom blocks on any portrait aspect ratio and safe area. It frames again
    // whenever the screen or its safe area changes.
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCameraFraming : MonoBehaviour
    {
        // The lowest element of the top HUD block and the highest element of the bottom block.
        [SerializeField] private RectTransform _topHud;
        [SerializeField] private RectTransform _bottomHud;
        // The world area that must stay visible: from below the hero's feet to beyond the deepest pack slot, and the widest
        // slot either side of the centre line.
        [SerializeField] private float _bottom = -0.7f;
        [SerializeField] private float _top = 4.5f;
        [SerializeField, Min(0.1f)] private float _halfWidth = 2.6f;

        private readonly Vector3[] _corners = new Vector3[4];
        private Camera _camera;
        private int _framedWidth;
        private int _framedHeight;
        private Rect _framedSafeArea;

        public RectTransform TopHud => _topHud;
        public RectTransform BottomHud => _bottomHud;
        public float Bottom => _bottom;
        public float Top => _top;
        public float HalfWidth => _halfWidth;

        // Frames the arena for a screen of the given size whose free rows run from bandBottom to bandTop.
        public void Frame(float screenWidth, float screenHeight, float bandBottom, float bandTop)
        {
            ArenaFraming.Fit(screenWidth, screenHeight, bandBottom, bandTop, _bottom, _top, _halfWidth,
                out float orthographicSize, out float cameraY);
            _camera.orthographicSize = orthographicSize;
            Vector3 position = transform.position;
            transform.position = new Vector3(position.x, cameraY, position.z);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_topHud == null || _bottomHud == null || !(_top > _bottom))
            {
                Debug.LogError("ArenaCameraFraming needs both HUD blocks and a top above its bottom.", this);
                enabled = false;
            }
        }

        // After every Update, so the safe area fitter has already moved the HUD.
        private void LateUpdate()
        {
            if (Screen.width == _framedWidth && Screen.height == _framedHeight && Screen.safeArea == _framedSafeArea)
                return;

            _framedWidth = Screen.width;
            _framedHeight = Screen.height;
            _framedSafeArea = Screen.safeArea;
            if (_framedWidth <= 0 || _framedHeight <= 0)
                return;

            // Overlay canvases place their corners in screen pixels.
            Canvas.ForceUpdateCanvases();
            _topHud.GetWorldCorners(_corners);
            float bandTop = _corners[0].y;
            _bottomHud.GetWorldCorners(_corners);
            float bandBottom = _corners[1].y;
            Frame(_framedWidth, _framedHeight, bandBottom, bandTop);
        }
    }
}
