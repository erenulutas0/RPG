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
        [SerializeField, Min(0)] private float _visualTop;
        [SerializeField, Min(.01f)] private float _shadowWidth = .48f;
        [SerializeField, Min(.01f)] private float _walkCycleDistance = .55f;
        public float VisualTop => _visualTop > 0 ? _visualTop : GetFrame(0, false).Body.bounds.max.y;
        public float ShadowWidth => _shadowWidth;
        public float WalkCycleDistance => _walkCycleDistance;
        public bool HasPassingFrames => _front != null && _front.Length == 6 && _rear != null && _rear.Length == 6;
        // Optional appended passing A/B preserve the original idle/contact A/contact B/strike indices.
        public int WalkingFrame(float distance)
        {
            float phase = Mathf.Repeat(distance, _walkCycleDistance) / _walkCycleDistance;
            if (!HasPassingFrames) return phase < .5f ? 1 : 2;
            int quarter = Mathf.Min(3, (int)(phase * 4));
            return quarter == 0 ? 1 : quarter == 1 ? 4 : quarter == 2 ? 2 : 5;
        }
        public Sprite[] Death => _death;
        public Frame GetFrame(int index, bool rear) => (rear ? _rear : _front)[index];
        public bool IsValid => Complete(_front) && Complete(_rear) && _front.Length == _rear.Length && _walkCycleDistance > 0 && _shadowWidth > 0 && _death != null && _death.Length == 2 && _death[0] != null && _death[1] != null;
        private static bool Complete(Frame[] frames)
        {
            if (frames == null || (frames.Length != 4 && frames.Length != 6)) return false;
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
