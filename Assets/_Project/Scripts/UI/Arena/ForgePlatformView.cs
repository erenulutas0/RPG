using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // The floating forge platform as pixel art: one sprite for the tiled top, corner towers, stone faces, keel, crystal
    // and chains, plus an overlay sprite whose frames flicker the lantern flames, lava seams and the crystal's heart.
    // Built by ArenaView at startup from the arena geometry; both sprites are drawn once by PlatformArt.
    public sealed class ForgePlatformView : MonoBehaviour
    {
        // Seconds between overlay frames, and how deep the ember light breathes between flickers.
        [SerializeField, Min(0.05f)] private float _flickerInterval = 0.42f;
        [SerializeField, Range(0f, 1f)] private float _pulseDepth = 0.18f;
        [SerializeField, Min(0.1f)] private float _pulseRate = 2.6f;

        private Sprite _platformSprite;
        private Sprite[] _lightFrames;
        private float _flickerTimer;
        private int _frame;

        // The main renderer (the given sorting order); the overlay draws one order above it.
        public SpriteRenderer PlatformRenderer { get; private set; }
        public SpriteRenderer LightsRenderer { get; private set; }

        // Builds the platform once; sortingOrder is just above the backdrop and below every combatant. The material is
        // the arena's vertex-colour material, which sprites do not need: both renderers keep the sprite default.
        public void Build(ArenaGeometry geometry, Material material, int sortingOrder)
        {
            var layout = new PlatformLayout(geometry);
            _platformSprite = PixelSpriteFactory.CreateSprite(PlatformArt.DrawPlatform(layout), "Forge Platform", PixelSpriteFactory.BottomCentre);
            _lightFrames = new Sprite[PlatformArt.FrameCount];
            for (int i = 0; i < _lightFrames.Length; i++)
                _lightFrames[i] = PixelSpriteFactory.CreateSprite(PlatformArt.DrawLights(layout, i), "Forge Platform Lights " + i, PixelSpriteFactory.BottomCentre);

            // Both sprites share the bottom-centre pivot at the canvas's world anchor, so their texels line up.
            var anchor = new Vector3(0f, layout.WorldBottom, 0f);
            PlatformRenderer = AddRenderer("Platform", _platformSprite, anchor, sortingOrder);
            LightsRenderer = AddRenderer("Platform Lights", _lightFrames[0], anchor, sortingOrder + 1);
        }

        private void Update()
        {
            if (LightsRenderer == null)
                return;
            _flickerTimer += Time.deltaTime;
            if (_flickerTimer >= _flickerInterval)
            {
                _flickerTimer -= _flickerInterval;
                _frame = (_frame + 1) % _lightFrames.Length;
                LightsRenderer.sprite = _lightFrames[_frame];
            }
            // The ember light breathes by dimming toward red, so the static lava underneath shows through the dips.
            float pulse = 1f - _pulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * _pulseRate));
            LightsRenderer.color = new Color(1f, pulse, pulse * pulse, 1f);
        }

        private void OnDestroy()
        {
            PixelSpriteFactory.Destroy(_platformSprite);
            if (_lightFrames == null)
                return;
            for (int i = 0; i < _lightFrames.Length; i++)
                PixelSpriteFactory.Destroy(_lightFrames[i]);
        }

        private SpriteRenderer AddRenderer(string name, Sprite sprite, Vector3 localPosition, int sortingOrder)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = localPosition;
            var renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
