using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // Shared imported sprites. Enemy views and pooled death effects borrow, never destroy, these assets.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Enemy Set")]
    public sealed class EnemyArtSet : ScriptableObject
    {
        [Serializable] public struct Frame { public Sprite Body; public Sprite Flash; }
        // Idle, step A, step B, strike; front/rear look left and mirror for right.
        [SerializeField] private Frame[] _front;
        [SerializeField] private Frame[] _rear;
        [SerializeField] private Sprite[] _death;
        public Sprite[] Death => _death;
        public Frame GetFrame(int index, bool rear) => (rear ? _rear : _front)[index];
        public bool IsValid => Complete(_front) && Complete(_rear) && _death != null && _death.Length == 2 && _death[0] != null && _death[1] != null;
        private static bool Complete(Frame[] frames)
        {
            if (frames == null || frames.Length != 4) return false;
            foreach (var f in frames) if (f.Body == null || f.Flash == null) return false;
            return true;
        }
        public Sprite FlashOf(Sprite body)
        {
            if (_front != null) foreach (var f in _front) if (f.Body == body) return f.Flash;
            if (_rear != null) foreach (var f in _rear) if (f.Body == body) return f.Flash;
            return null;
        }
    }
}
