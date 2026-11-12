using Cryptforge.Art;
using Cryptforge.Content;
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
        [SerializeField] private Sprite _furnaceSprite;
        // The platform top on the arena floor, in floor units with the hero at the origin: its near corner behind the hero,
        // its far corner beyond the deepest pack slot, and its half-width at the side corners.
        [SerializeField] private float _nearCorner = -3f;
        [SerializeField] private float _farCorner = 14f;
        [SerializeField, Min(0.5f)] private float _halfWidth = 3.3f;
        // The backdrop draws at this order and the platform just above it; every combatant sprite sits higher.
        [SerializeField] private int _sortingOrder = -20;

        private bool _regionResolved;
        private bool _wideRegion;
        private bool _foundryRegion;
        public bool IsFoundryRegion { get { _ = IsWideRegionPreview; return _foundryRegion; } }
        public FoundryRegionView Region { get; private set; }
        public bool IsWideRegionPreview
        {
            get
            {
                // Resolve once, including when combat asks before this view's Awake. No file IO per frame.
                if (!_regionResolved) { _wideRegion = DevelopmentStart.WideRegionPreview(); _foundryRegion=DevelopmentStart.FoundryRegion(); _regionResolved = true; }
                return _wideRegion;
            }
        }
        public ArenaGeometry Geometry => IsFoundryRegion ? ArenaGeometry.Foundry : IsWideRegionPreview ? new ArenaGeometry(-27, 27, 18) :
            new ArenaGeometry(_nearCorner, _farCorner, _halfWidth);
        public Material Material => _material;
        public PlatformMaterialSet PlatformMaterials => _platformMaterials;
        public int GroundEffectSortingOrder => _sortingOrder + 7;
        public VoidBackdropView Backdrop { get; private set; }
        public ForgePlatformView Platform { get; private set; }
        public FoundrySceneryView Scenery { get; private set; }
        // The lowest renderer of the arena; every sprite in the scene draws above it.
        public MeshRenderer ArenaRenderer => Backdrop != null ? Backdrop.SkyRenderer : null;

        // True when the floor point lies on the platform's top.
        public bool IsOnPlatform(float floorX, float floorY) => Geometry.IsOnPlatform(floorX, floorY);

        private void Awake()
        {
            _awoken = true;
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
            if(IsFoundryRegion)
            {
                Region=Child<FoundryRegionView>("Playable Foundry");
                Region.Build(this,_furnaceSprite);
                // A preparation asked for before this Awake (CombatSetup.Awake's EnterFloor can run first).
                HandOverPreparation();
                _atmosphere=Child<FoundryAtmosphereView>("Foundry atmosphere");
                _atmosphere.Build(_material);
                return;
            }
            Platform = Child<ForgePlatformView>("Forge Platform");
            using (PerformanceMarkers.BuildPlatform.Auto())
                Platform.Build(geometry, _material, _sortingOrder + 5, _platformMaterials);
            if (IsWideRegionPreview && _furnaceSprite != null && _platformMaterials != null && _platformMaterials.IsValid)
            {
                Scenery = Child<FoundrySceneryView>("Foundry Scenery");
                Scenery.Build(geometry, _furnaceSprite, _platformMaterials.Floor, _sortingOrder + 6);
            }
        }

        // The floor's dress (FloorTheme): the region re-tints its own meshes and the backdrop's plate is tinted; Foundry
        // restores exactly what Awake built, and asking for the theme already on show does nothing.
        public FloorTheme Theme { get; private set; }

        // A floor ahead whose dress is not this one (EncounterController, as a floor starts): the region works it out over
        // the coming frames - colours graded off the main thread, textures loaded asynchronously - so the change itself
        // only binds it. Nothing on show changes.
        // The request is kept until the region exists: CombatSetup.Awake enters floor 1 (and asks for floor 2's dress)
        // before this view's own Awake has built the region whenever Unity happens to wake CombatSetup first.
        public void PrepareFloorTheme(FloorTheme theme)
        {
            if (theme == Theme)
                return;
            _preparing = theme;
            _preparationAsked = true;
            HandOverPreparation();
        }
        public bool IsFloorThemeReady(FloorTheme theme) => Region == null || Region.IsThemePrepared(theme);
        // True from the request until the region has the dress ready; false once awake with no region to dress.
        public bool IsPreparingFloorTheme => Region != null ? Region.IsPreparingTheme || (_preparationAsked && !Region.IsThemePrepared(_preparing))
            : _preparationAsked && !_awoken;
        private void HandOverPreparation()
        {
            if (!_preparationAsked || Region == null)
                return;
            if (_preparing != Theme)
                Region.PrepareTheme(_preparing, _platformMaterials);
        }
        private bool _awoken;
        private bool _preparationAsked;
        private FloorTheme _preparing;
        public void ApplyFloorTheme(FloorTheme theme)
        {
            if (theme == Theme)
                return;
            using (PerformanceMarkers.ThemeApply.Auto())
                DressFloor(theme);
        }

        private void DressFloor(FloorTheme theme)
        {
            Theme = theme;
            bool ice = theme == FloorTheme.Ice;
            if (Region != null)
                Region.ApplyTheme(theme, _platformMaterials);
            using var backdrop = PerformanceMarkers.ThemeBackdrop.Auto();
            if (_atmosphere != null)
                _atmosphere.SetCold(ice);
            // The hero's contact patch widens on the ice floor so the Vanguard keeps his footing against the slate (docs/49).
            var hero = FindFirstObjectByType<HeroLookView>();
            Transform shadow = hero != null ? hero.transform.Find("Contact Shadow") : null;
            if (shadow != null)
            {
                if (_heroShadow != shadow) { _heroShadow = shadow; _heroShadowScale = shadow.localScale; }
                shadow.localScale = ice ? _heroShadowScale * IceHeroShadow : _heroShadowScale;
            }
            if (Backdrop != null && Backdrop.CavernRenderer != null)
            {
                SpriteRenderer plate = Backdrop.CavernRenderer;
                if (!_cavernColourKept) { _cavernColour = plate.color; _cavernSprite = plate.sprite; _cavernColourKept = true; }
                Sprite painted = _platformMaterials != null ? _platformMaterials.IceBackdrop : null;
                // The ice vault's own painting where it exists (the same size and framing as the cavern's), else the
                // cavern tinted cold.
                plate.sprite = ice && painted != null ? painted : _cavernSprite;
                plate.color = ice && painted == null ? FoundryRegionView.IceBackdrop(_cavernColour) : _cavernColour;
                Backdrop.FrameCavern();
            }
        }
        // The Descent landing on floor 2 (EncounterController.EnterFloor): a short frost creep over the camera, once a run.
        // Made on first use, so floor 1 never builds it; never a child of the region, so the region's budget is unchanged.
        public FrostTransitionView FrostTransition { get; private set; }
        public bool PlayFrostTransition()
        {
            if (_frostPlayed || _camera == null || _material == null)
                return false;
            using var transition = PerformanceMarkers.ThemeTransition.Auto();
            _frostPlayed = true;
            if (FrostTransition == null)
            {
                var frost = new GameObject(FrostTransitionView.ObjectName);
                frost.transform.SetParent(_camera.transform, false);
                FrostTransition = frost.AddComponent<FrostTransitionView>();
                FrostTransition.Build(_camera, _material);
            }
            FrostTransition.Play();
            return true;
        }
        // Only the first floor's descent onto the frozen second floor shows the frost.
        public static bool FrostsOnEntry(int floorNumber, FloorTheme theme) => floorNumber == 2 && theme == FloorTheme.Ice;
        private bool _frostPlayed;
        private const float IceHeroShadow = 1.25f;
        private FoundryAtmosphereView _atmosphere;
        private Transform _heroShadow;
        private Vector3 _heroShadowScale;
        private Color _cavernColour;
        private Sprite _cavernSprite;
        private bool _cavernColourKept;

        private T Child<T>(string name) where T : Component
        {
            var child = new GameObject(name);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }
    }
}
