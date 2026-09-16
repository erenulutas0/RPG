using UnityEngine;

namespace Cryptforge.Art
{
    [CreateAssetMenu(menuName = "Cryptforge/Art/Strike Set")]
    public sealed class StrikeArtSet : ScriptableObject
    {
        [SerializeField] private Sprite[] _slash;
        [SerializeField] private Sprite[] _spark;
        public Sprite[] Slash => _slash;
        public Sprite[] Spark => _spark;
        public bool IsValid => Complete(_slash) && Complete(_spark);
        private static bool Complete(Sprite[] frames)
        {
            if (frames == null || frames.Length != 4) return false;
            foreach (var frame in frames) if (frame == null) return false;
            return true;
        }
    }
}
