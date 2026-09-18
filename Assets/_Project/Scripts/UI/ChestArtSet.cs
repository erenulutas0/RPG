using UnityEngine;

namespace Cryptforge.UI
{
    // Imported asset-owned sprites: views borrow these and never destroy their textures.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Chest")]
    public sealed class ChestArtSet : ScriptableObject
    {
        [SerializeField] private Sprite _closed;
        [SerializeField] private Sprite _opening;
        [SerializeField] private Sprite _open;
        public Sprite Closed => _closed;
        public Sprite Opening => _opening;
        public Sprite Open => _open;
        public bool IsValid => _closed != null && _opening != null && _open != null;
    }
}
