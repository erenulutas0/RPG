using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Follows within room bounds at a fixed width, keeping the hero and nearby threats in the free HUD band.
    // Near a rim it favours the room interior instead of centring empty space. Reframes for screen/safe-area changes.
    [RequireComponent(typeof(Camera))]
    public sealed class ArenaCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [SerializeField] private ArenaView _arena;
        // The lowest element of the top HUD block and the highest element of the bottom block.
        [SerializeField] private RectTransform _topHud;
        [SerializeField] private RectTransform _bottomHud;
        // World units visible across the screen.
        [SerializeField, Min(1f)] private float _visibleWidth = 4.6f;
        // How quickly the camera closes on the hero, per second; the first frame snaps.
        [SerializeField, Min(0.1f)] private float _closingRate = 8f;

        private readonly Vector3[] _corners = new Vector3[4];
        private Camera _camera;
        private int _framedWidth;
        private int _framedHeight;
        private Rect _framedSafeArea;
        private float _offsetY;
        private float _bandHeight;
        private bool _snapped;

        public Transform Target => _target;
        public RectTransform TopHud => _topHud;
        public RectTransform BottomHud => _bottomHud;
        public float VisibleWidth => _visibleWidth;
        // How far below the free band's world centre the camera sits; rim framing can move the hero within that band.
        public float OffsetY => _offsetY;

        // Frames for a screen of the given size whose free rows run from bandBottom to bandTop.
        public void Frame(float screenWidth, float screenHeight, float bandBottom, float bandTop)
        {
            FollowFraming.Fit(screenWidth, screenHeight, bandBottom, bandTop, _visibleWidth, out float size, out _offsetY);
            _bandHeight = (bandTop - bandBottom >= screenHeight * FollowFraming.MinimumBandShare
                ? bandTop - bandBottom : screenHeight) * _visibleWidth / screenWidth;
            _camera.orthographicSize = size;
            Snap();
        }

        // Where the camera wants to be for the hero's current position.
        public Vector3 Goal()
        {
            Vector3 position = transform.position;
            if (_arena == null || _bandHeight <= 0f)
                return new Vector3(_target.position.x, _target.position.y - _offsetY, position.z);
            Vector3 origin = _arena.transform.position;
            FollowFraming.ConstrainToArena(_arena.Geometry, _target.position.x - origin.x, _target.position.y - origin.y,
                _visibleWidth, _bandHeight, out float x, out float y);
            return new Vector3(x + origin.x, y + origin.y - _offsetY, position.z);
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_target == null || _topHud == null || _bottomHud == null)
            {
                Debug.LogError("ArenaCameraFollow needs a target and both HUD blocks.", this);
                enabled = false;
            }
        }

        // After every Update, so the hero has moved and the safe area fitter has placed the HUD.
        private void LateUpdate()
        {
            if (Screen.width != _framedWidth || Screen.height != _framedHeight || Screen.safeArea != _framedSafeArea)
            {
                _framedWidth = Screen.width;
                _framedHeight = Screen.height;
                _framedSafeArea = Screen.safeArea;
                if (_framedWidth > 0 && _framedHeight > 0)
                {
                    // Overlay canvases place their corners in screen pixels.
                    Canvas.ForceUpdateCanvases();
                    _topHud.GetWorldCorners(_corners);
                    float bandTop = _corners[0].y;
                    _bottomHud.GetWorldCorners(_corners);
                    float bandBottom = _corners[1].y;
                    Frame(_framedWidth, _framedHeight, bandBottom, bandTop);
                    return;
                }
            }

            if (!_snapped)
            {
                Snap();
                return;
            }
            Vector3 goal = Goal();
            float t = 1f - Mathf.Exp(-_closingRate * Time.unscaledDeltaTime);
            transform.position = Vector3.Lerp(transform.position, goal, t);
        }

        private void Snap()
        {
            transform.position = Goal();
            _snapped = true;
        }
    }
}
