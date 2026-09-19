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
            // Optional, exactly registered crop of the existing front gauntlet; no runtime pixel generation.
            public Sprite Grip;
            public Vector2 GripOffset;
        }

        // Idle, contact A, passing A, contact B, passing B, windup, strike, recovery.
        [SerializeField] private Frame[] _frames;
        [SerializeField] private Frame[] _frontFrames;
        [SerializeField] private Sprite _sword;
        [SerializeField] private Sprite _shield;
        [SerializeField] private Sprite _staff;
        public Sprite Sword => _sword;
        public Sprite Shield => _shield;
        public Sprite Staff => _staff;
        public Frame GetFrame(int index) => _frames[index];
        public Frame GetFrame(int index, bool front) => front && HasFrontFrames ? _frontFrames[index] : _frames[index];
        public bool HasFrontFrames => Complete(_frontFrames);
        private static bool Complete(Frame[] frames)
        {
            if (frames == null || frames.Length != 8) return false;
            foreach (var frame in frames) if (frame.Body == null || frame.Flash == null) return false;
            return true;
        }
        public bool IsValid
        {
            get
            {
                return Complete(_frames) && _sword != null && _shield != null
                    && (_frontFrames == null || _frontFrames.Length == 0 || HasFrontFrames);
            }
        }
        public Sprite FlashOf(Sprite body)
        {
            if (_frames == null) return null;
            foreach (var frame in _frames) if (frame.Body == body) return frame.Flash;
            if (_frontFrames != null)
                foreach (var frame in _frontFrames) if (frame.Body == body) return frame.Flash;
            return null;
        }
    }
}
