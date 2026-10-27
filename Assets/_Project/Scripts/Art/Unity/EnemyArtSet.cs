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
        // Optional, for a boss whose blow is telegraphed (the Forge Warden's slam): its raised pose, front then rear,
        // each looking left like the frames above. Empty on every other enemy, which never winds up.
        [SerializeField] private Frame[] _windup;
        // Optional, for a boss's second phase: the same poses burning hotter, index for index with the front, rear and
        // windup frames. Their flashes may be the first phase's own, since the silhouette is the same.
        [SerializeField] private Frame[] _enragedFront;
        [SerializeField] private Frame[] _enragedRear;
        [SerializeField] private Frame[] _enragedWindup;
        // Optional, with the windup: the raised hammer carried at a walk, so a boss that keeps coming through its
        // telegraph steps rather than glides - the walk's four quarters (contact A, passing A, contact B, passing B, as
        // WalkingQuarter steps them by distance), front then rear, each looking left. Its second phase's, index for
        // index, may wear the first phase's flashes.
        [SerializeField] private Frame[] _windupWalk;
        [SerializeField] private Frame[] _enragedWindupWalk;
        // Optional, for a boss's own fall in place of the two remains (CombatEffectsView): its drawings in order, front
        // then rear (without the rear's, the front's serve both, mirrored as the remains are), each looking left, and how
        // long each holds - the last until the remains fade.
        [SerializeField] private Sprite[] _fallFront;
        [SerializeField] private Sprite[] _fallRear;
        [SerializeField] private float[] _fallSeconds;
        // The fall as frames FallStep apart, built once for each facing.
        [NonSerialized] private Sprite[] _fallTimelineFront;
        [NonSerialized] private Sprite[] _fallTimelineRear;
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
        // A partly filled optional part counts as absent, so its enemy behaves as one without it.
        public bool HasWindup => _windup != null && _windup.Length == 2 && Filled(_windup);
        public bool HasEnragedFrames => Matches(_enragedFront, _front) && Matches(_enragedRear, _rear) &&
            (HasWindup ? Matches(_enragedWindup, _windup) : _enragedWindup == null || _enragedWindup.Length == 0);
        // The frame at index, from the second phase's when it is asked for and the set has one.
        public Frame GetFrame(int index, bool rear, bool enraged) =>
            enraged && HasEnragedFrames ? (rear ? _enragedRear : _enragedFront)[index] : GetFrame(index, rear);
        // Only with HasWindup.
        public Frame GetWindup(bool rear, bool enraged = false) =>
            (enraged && HasEnragedFrames ? _enragedWindup : _windup)[rear ? 1 : 0];
        // The walk's quarter at a travelled distance - 0 contact A, 1 passing A, 2 contact B, 3 passing B - as
        // WalkingFrame steps the walk's own frames (1, 4, 2, 5).
        public int WalkingQuarter(float distance) =>
            Mathf.Min(3, (int)(Mathf.Repeat(distance, _walkCycleDistance) / _walkCycleDistance * 4));
        // The raised hammer can be carried at a walk - in the second phase only if the set draws that phase's too.
        public bool HasWalkingWindup(bool enraged) => HasWindup && _windupWalk != null && _windupWalk.Length == 8 &&
            Filled(_windupWalk) && (!enraged || !HasEnragedFrames || Matches(_enragedWindupWalk, _windupWalk));
        // Only with HasWalkingWindup(enraged): the raised hammer at a walk, in a quarter.
        public Frame GetWalkingWindup(int quarter, bool rear, bool enraged = false) =>
            (enraged && HasEnragedFrames && Matches(_enragedWindupWalk, _windupWalk) ? _enragedWindupWalk : _windupWalk)
                [(rear ? 4 : 0) + quarter];
        // How far apart FallTimeline's frames are.
        public const float FallStep = .02f;
        public bool HasFall => Filled(_fallFront) && _fallSeconds != null && _fallSeconds.Length == _fallFront.Length &&
            AllPositive(_fallSeconds);
        public bool HasRearFall => HasFall && _fallRear != null && _fallRear.Length == _fallFront.Length && Filled(_fallRear);
        // Only with HasFall: a facing's drawings, how long each holds, and how long the fall plays, the last drawing's
        // hold included.
        public Sprite[] GetFall(bool rear) => rear && HasRearFall ? _fallRear : _fallFront;
        public float FallHold(int index) => _fallSeconds[index];
        public float FallSeconds
        {
            get
            {
                float total = 0f;
                foreach (float seconds in _fallSeconds) total += seconds;
                return total;
            }
        }
        // Only with HasFall: the drawing on show this long into the fall; the last holds once it is over.
        public Sprite FallFrameAt(float seconds, bool rear)
        {
            Sprite[] fall = GetFall(rear);
            for (int i = 0; i < fall.Length - 1; i++)
            {
                seconds -= _fallSeconds[i];
                if (seconds < 0f) return fall[i];
            }
            return fall[fall.Length - 1];
        }
        // Only with HasFall: the fall as frames FallStep apart over FallSeconds, for an effect that steps its frames
        // evenly (CombatEffectsView's remains).
        public Sprite[] FallTimeline(bool rear)
        {
            rear &= HasRearFall;
            Sprite[] timeline = rear ? _fallTimelineRear : _fallTimelineFront;
            if (timeline != null) return timeline;
            timeline = new Sprite[Mathf.Max(1, Mathf.CeilToInt(FallSeconds / FallStep - .001f))];
            for (int i = 0; i < timeline.Length; i++) timeline[i] = FallFrameAt((i + .5f) * FallStep, rear);
            if (rear) _fallTimelineRear = timeline;
            else _fallTimelineFront = timeline;
            return timeline;
        }
        private void OnValidate() => _fallTimelineFront = _fallTimelineRear = null;
        private static bool Complete(Frame[] frames)
        {
            if (frames == null || (frames.Length != 4 && frames.Length != 6)) return false;
            foreach (var f in frames) if (f.Body == null || f.Flash == null) return false;
            return true;
        }
        private static bool Filled(Frame[] frames)
        {
            foreach (var f in frames) if (f.Body == null || f.Flash == null) return false;
            return true;
        }
        private static bool Matches(Frame[] frames, Frame[] like) => frames != null && like != null && frames.Length == like.Length && Filled(frames);
        private static bool Filled(Sprite[] sprites)
        {
            if (sprites == null || sprites.Length == 0) return false;
            foreach (var s in sprites) if (s == null) return false;
            return true;
        }
        private static bool AllPositive(float[] values)
        {
            foreach (float v in values) if (!(v > 0f)) return false;
            return true;
        }
        public Sprite FlashOf(Sprite body)
        {
            if (_front != null) foreach (var f in _front) if (f.Body == body) return f.Flash;
            if (_rear != null) foreach (var f in _rear) if (f.Body == body) return f.Flash;
            // The optional poses: a boss's windup, carried at a walk too, and its second phase.
            if (FlashIn(_windup, body, out Sprite flash) || FlashIn(_enragedFront, body, out flash) ||
                FlashIn(_enragedRear, body, out flash) || FlashIn(_enragedWindup, body, out flash) ||
                FlashIn(_windupWalk, body, out flash) || FlashIn(_enragedWindupWalk, body, out flash)) return flash;
            return null;
        }
        private static bool FlashIn(Frame[] frames, Sprite body, out Sprite flash)
        {
            flash = null;
            if (frames == null) return false;
            foreach (var f in frames) if (f.Body == body) { flash = f.Flash; return true; }
            return false;
        }
    }
}
