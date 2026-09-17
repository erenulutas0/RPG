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
        public Material Floor => _floor;
        public Material Coping => _coping;
        public Material Wall => _wall;
        public bool IsValid => Complete(_floor) && Complete(_coping) && Complete(_wall);
        private static bool Complete(Material material) => material != null && material.mainTexture != null;
        public Material ForPart(int index) => index == 0 ? _floor : index < 5 ? _coping : _wall;
    }
}
