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
        [SerializeField] private Material _foundryEmbers;
        [SerializeField] private Material _foundryPaving;
        public Material Floor => _floor;
        public Material Coping => _coping;
        public Material Wall => _wall;
        public Material GateFacade => _gateFacade;
        public Material FoundryWall => _foundryWall != null ? _foundryWall : _wall;
        public Material FurnaceFacade => _furnaceFacade;
        public Material FoundryPitLining => _foundryPitLining != null ? _foundryPitLining : _furnaceFacade;
        public Material FoundryIron => _foundryIron != null ? _foundryIron : FoundryWall;
        public Material FoundryEmbers => _foundryEmbers;
        public Material FoundryPaving => _foundryPaving != null ? _foundryPaving : _floor;
        public bool IsValid => Complete(_floor) && Complete(_coping) && Complete(_wall);
        private static bool Complete(Material material) => material != null && material.mainTexture != null;
        public Material ForPart(int index) => index == 0 ? _floor : index < 5 ? _coping : _wall;
    }
}
