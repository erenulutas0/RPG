using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // Imported, shared assets. Views borrow these sprites and never destroy them on a scene reload.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Vanguard Set")]
    public sealed class VanguardArtSet : ScriptableObject
    {
        [Serializable]
        public struct Frame
        {
            public Sprite Body;
            public Sprite Flash;
            public Vector2 RightHand;
            public Vector2 LeftHand;
        }

        // Idle, contact A, passing A, contact B, passing B, windup, strike, recovery.
        [SerializeField] private Frame[] _frames;
        [SerializeField] private Sprite _sword;
        [SerializeField] private Sprite _shield;
        public Sprite Sword => _sword;
        public Sprite Shield => _shield;
        public Frame GetFrame(int index) => _frames[index];
        public bool IsValid
        {
            get
            {
                if (_frames == null || _frames.Length != 8 || _sword == null || _shield == null) return false;
                foreach (var frame in _frames) if (frame.Body == null || frame.Flash == null) return false;
                return true;
            }
        }
        public Sprite FlashOf(Sprite body)
        {
            if (_frames == null) return null;
            foreach (var frame in _frames) if (frame.Body == body) return frame.Flash;
            return null;
        }
    }
}
