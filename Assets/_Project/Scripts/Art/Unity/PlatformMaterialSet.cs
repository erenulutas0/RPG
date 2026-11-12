using Cryptforge.Content;
using UnityEngine;

namespace Cryptforge.Art
{
    // Asset-owned materials/textures. Views borrow these; only generated geometry has session ownership.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Platform Materials")]
    public sealed class PlatformMaterialSet : ScriptableObject
    {
        [SerializeField] private Material _floor;
        [SerializeField] private Material _coping;
        [SerializeField] private Material _wall;
        [SerializeField] private Material _gateFacade;
        [SerializeField] private Material _foundryWall;
        [SerializeField] private Material _furnaceFacade;
        [SerializeField] private Material _foundryPitLining;
        [SerializeField] private Material _foundryIron;
        [SerializeField] private Material _foundryCastIron;
        [SerializeField] private Material _foundryEmbers;
        [SerializeField] private Material _foundryPaving;
        [SerializeField] private Material _foundryFlagstone;
        [SerializeField] private Material _forgeKeeper;
        // Floor 2's ice textures (docs/49), swapped in by FoundryRegionView.ApplyTheme for the flagstone and masonry ones.
        [SerializeField] private Texture2D _icePaving;
        [SerializeField] private Texture2D _iceMasonry;
        public Texture2D IcePaving => _icePaving;
        public Texture2D IceMasonry => _iceMasonry;
        // Floor 2's painted backdrop, shown by ArenaView.ApplyFloorTheme in place of the foundry cavern plate.
        [SerializeField] private Sprite _iceBackdrop;
        public Sprite IceBackdrop => _iceBackdrop;
        // Floor 3's (docs/51): GloamPavingV1 and GloamMasonryV1, and its painted plate once it has one (pass 2; until
        // then the cavern plate is tinted, FoundryRegionView.Backdrop).
        [SerializeField] private Texture2D _gloamPaving;
        [SerializeField] private Texture2D _gloamMasonry;
        [SerializeField] private Sprite _gloamBackdrop;
        public Texture2D GloamPaving => _gloamPaving;
        public Texture2D GloamMasonry => _gloamMasonry;
        public Sprite GloamBackdrop => _gloamBackdrop;
        // A themed floor's paving, masonry and plate (FloorTheme); none on the foundry, which keeps its materials' own.
        public Texture2D PavingFor(FloorTheme theme) => theme == FloorTheme.Ice ? _icePaving : theme == FloorTheme.Gloam ? _gloamPaving : null;
        public Texture2D MasonryFor(FloorTheme theme) => theme == FloorTheme.Ice ? _iceMasonry : theme == FloorTheme.Gloam ? _gloamMasonry : null;
        public Sprite BackdropFor(FloorTheme theme) => theme == FloorTheme.Ice ? _iceBackdrop : theme == FloorTheme.Gloam ? _gloamBackdrop : null;
        public Material ForgeKeeper => _forgeKeeper;
        public Material Floor => _floor;
        public Material Coping => _coping;
        public Material Wall => _wall;
        public Material GateFacade => _gateFacade;
        public Material FoundryWall => _foundryWall != null ? _foundryWall : _wall;
        public Material FurnaceFacade => _furnaceFacade;
        public Material FoundryPitLining => _foundryPitLining != null ? _foundryPitLining : _furnaceFacade;
        public Material FoundryIron => _foundryIron != null ? _foundryIron : FoundryWall;
        public Material FoundryCastIron => _foundryCastIron != null ? _foundryCastIron : FoundryIron;
        public Material FoundryEmbers => _foundryEmbers;
        public Material FoundryPaving => _foundryPaving != null ? _foundryPaving : _floor;
        public Material FoundryFlagstone => _foundryFlagstone != null ? _foundryFlagstone : FoundryPaving;
        public bool IsValid => Complete(_floor) && Complete(_coping) && Complete(_wall);
        private static bool Complete(Material material) => material != null && material.mainTexture != null;
        public Material ForPart(int index) => index == 0 ? _floor : index < 5 ? _coping : _wall;
    }
}
