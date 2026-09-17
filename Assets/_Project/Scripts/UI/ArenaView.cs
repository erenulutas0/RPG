using Cryptforge.Art;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.UI
{
    // The foundry arena chosen in `19`: an optional imported cavern behind the procedural floating forge platform,
    // built at startup as children of this object. It also makes the camera draw sprites higher on
    // the screen first, so a nearer combatant overlaps a farther one. Final art replaces the child views, not the game.
    public sealed class ArenaView : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        // Draws vertex colours and sprites; the built-in Sprites-Default material does.
        [SerializeField] private Material _material;
        // Optional imported environment plate; the procedural void remains a fallback for isolated art tests.
        [SerializeField] private Sprite _cavernBackdrop;
        [SerializeField] private PlatformMaterialSet _platformMaterials;
        // The platform top on the arena floor, in floor units with the hero at the origin: its near corner behind the hero,
        // its far corner beyond the deepest pack slot, and its half-width at the side corners.
        [SerializeField] private float _nearCorner = -3f;
        [SerializeField] private float _farCorner = 14f;
        [SerializeField, Min(0.5f)] private float _halfWidth = 3.3f;
        // The backdrop draws at this order and the platform just above it; every combatant sprite sits higher.
        [SerializeField] private int _sortingOrder = -20;

        public ArenaGeometry Geometry => new ArenaGeometry(_nearCorner, _farCorner, _halfWidth);
        public Material Material => _material;
        public PlatformMaterialSet PlatformMaterials => _platformMaterials;
        public int GroundEffectSortingOrder => _sortingOrder + 7;
        public VoidBackdropView Backdrop { get; private set; }
        public ForgePlatformView Platform { get; private set; }
        // The lowest renderer of the arena; every sprite in the scene draws above it.
        public MeshRenderer ArenaRenderer => Backdrop != null ? Backdrop.SkyRenderer : null;

        // True when the floor point lies on the platform's top.
        public bool IsOnPlatform(float floorX, float floorY) => Geometry.IsOnPlatform(floorX, floorY);

        private void Awake()
        {
            if (_camera == null || _material == null || !(_farCorner > _nearCorner))
            {
                Debug.LogError("ArenaView needs a camera, a vertex-colour material and a far corner beyond the near corner.", this);
                enabled = false;
                return;
            }

            _camera.transparencySortMode = TransparencySortMode.CustomAxis;
            _camera.transparencySortAxis = Vector3.up;

            ArenaGeometry geometry = Geometry;
            Backdrop = Child<VoidBackdropView>("Void Backdrop");
            using (PerformanceMarkers.BuildBackdrop.Auto())
                Backdrop.Build(geometry, _material, _sortingOrder, _cavernBackdrop, _camera);
            Platform = Child<ForgePlatformView>("Forge Platform");
            using (PerformanceMarkers.BuildPlatform.Auto())
                Platform.Build(geometry, _material, _sortingOrder + 5, _platformMaterials);
        }

        private T Child<T>(string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }
    }
}
