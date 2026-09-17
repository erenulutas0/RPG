using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Optional painted materials over a retained keel/crystal/chain foundation. Without the set, use the pixel-art
    // platform. ArenaView supplies geometry; the session cache shares generated meshes/sprites across restarts.
    public sealed class ForgePlatformView : MonoBehaviour
    {
        // Seconds between overlay frames, and how deep the ember light breathes between flickers.
        [SerializeField, Min(0.05f)] private float _flickerInterval = 0.42f;
        [SerializeField, Range(0f, 1f)] private float _pulseDepth = 0.18f;
        [SerializeField, Min(0.1f)] private float _pulseRate = 2.6f;

        private PlatformSprites _sprites;
        private bool _shared;
        private PlatformMaterialMeshes _paintedMeshes;
        private bool _sharedMeshes;
        public bool UsesPaintedMaterials { get; private set; }
        private float _flickerTimer;
        private int _frame;

        // Foundation/main sprite and its light overlay. Painted meshes occupy the layer immediately above these.
        public SpriteRenderer PlatformRenderer { get; private set; }
        public SpriteRenderer LightsRenderer { get; private set; }

        // Builds the platform once; sortingOrder is just above the backdrop and below every combatant. The material is
        // the arena's vertex-colour material, which sprites do not need: both renderers keep the sprite default.
        public void Build(ArenaGeometry geometry, Material material, int sortingOrder, PlatformMaterialSet painted = null)
        {
            var layout = new PlatformLayout(geometry);
            UsesPaintedMaterials = painted != null && painted.IsValid;
            _sprites = UsesPaintedMaterials ? ArenaSpriteCache.Foundation(geometry, out _shared) : ArenaSpriteCache.Platform(geometry, out _shared);

            // Both sprites share the bottom-centre pivot at the canvas's world anchor, so their texels line up.
            var anchor = new Vector3(0f, layout.WorldBottom, 0f);
            PlatformRenderer = AddRenderer("Platform", _sprites.Surface, anchor, UsesPaintedMaterials ? sortingOrder - 1 : sortingOrder);
            LightsRenderer = AddRenderer("Platform Lights", _sprites.Light(0), anchor, UsesPaintedMaterials ? sortingOrder : sortingOrder + 1);
            if (UsesPaintedMaterials)
            {
                _paintedMeshes = ArenaSpriteCache.PaintedMeshes(geometry, out _sharedMeshes);
                for (int i = 0; i < _paintedMeshes.Count; i++)
                {
                    var part = new GameObject(_paintedMeshes[i].name);
                    part.transform.SetParent(transform, false);
                    part.AddComponent<MeshFilter>().sharedMesh = _paintedMeshes[i];
                    var renderer = part.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = painted.ForPart(i);
                    renderer.sortingOrder = sortingOrder + 1;
                }
            }
        }

        private void Update()
        {
            if (LightsRenderer == null)
                return;
            _flickerTimer += Time.deltaTime;
            if (_flickerTimer >= _flickerInterval)
            {
                _flickerTimer -= _flickerInterval;
                _frame = (_frame + 1) % PlatformArt.FrameCount;
                LightsRenderer.sprite = _sprites.Light(_frame);
            }
            // The ember light breathes by dimming toward red, so the static lava underneath shows through the dips.
            float pulse = 1f - _pulseDepth * (0.5f + 0.5f * Mathf.Sin(Time.time * _pulseRate));
            LightsRenderer.color = new Color(1f, pulse, pulse * pulse, 1f);
        }

        private void OnDestroy()
        {
            if (!_shared)
                _sprites?.Dispose();
            if (!_sharedMeshes)
                _paintedMeshes?.Dispose();
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
