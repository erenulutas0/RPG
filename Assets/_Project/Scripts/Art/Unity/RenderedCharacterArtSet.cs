using System;
using UnityEngine;

namespace Cryptforge.Art
{
    // A character pre-rendered from one 3D model (docs/45: Ilya Kest, Tools/ArtReview/BuildBlenderArcanist.py): every
    // motion in the five east facings, and optionally in the west three as well, every frame cut from shared atlases
    // with its pivot on the floor point under the feet, so nothing drifts or boils between frames or facings. A set
    // with the west facings (DrawsWest) is shown as drawn in all eight directions; one without them mirrors the east
    // ones for the west. The held weapon is part of the render; the hero's loadout renderers are hidden while it is
    // drawn. Imported, shared assets: views borrow the sprites and never destroy them.
    [CreateAssetMenu(menuName = "Cryptforge/Art/Rendered Character Set")]
    public sealed class RenderedCharacterArtSet : ScriptableObject
    {
        [Serializable]
        public struct Timing
        {
            public RenderedMotion Motion;
            // Frames a second for the timed motions; the walks are driven by distance and ignore it.
            public float FramesPerSecond;
            public bool Loops;
        }

        [Serializable]
        public struct Clip
        {
            public RenderedMotion Motion;
            public RenderedFacing Facing;
            public Sprite[] Frames;
        }

        // Floor units one walk cycle (two steps) covers in the render, so the feet keep pace with the floor.
        [SerializeField, Min(0.01f)] private float _strideLength = 1.2f;
        [SerializeField] private Timing[] _timings;
        [SerializeField] private Clip[] _clips;
        // Clip index per motion and facing, built on first use; -1 where the set has none.
        [NonSerialized] private int[] _lookup;

        public float StrideLength => _strideLength;

        // Every motion has a timing and a clip in each of the five east facings, all facings of a motion with the same
        // number of frames and no frame missing. The west three are optional but drawn whole or not at all: a set with
        // any of them has every motion in all three, so a half-packed west never mixes drawn and mirrored directions.
        public bool IsValid
        {
            get
            {
                if (_timings == null || _clips == null || !(_strideLength > 0f) || !EastComplete())
                    return false;
                int west = WestClips();
                return west == 0 || west == WestClipCount;
            }
        }

        // Every motion in the west three as well, complete: shown as drawn in all eight directions, nothing mirrored.
        public bool DrawsWest => IsValid && WestClips() == WestClipCount;

        private const int WestClipCount =
            RenderedCharacterFrames.MotionCount * (RenderedCharacterFrames.FacingCount - RenderedCharacterFrames.EastFacingCount);

        public int FrameCount(RenderedMotion motion)
        {
            int clip = ClipIndex(motion, RenderedFacing.South);
            return clip < 0 || _clips[clip].Frames == null ? 0 : _clips[clip].Frames.Length;
        }

        public Sprite Frame(RenderedMotion motion, RenderedFacing facing, int index)
        {
            int clip = ClipIndex(motion, facing);
            if (clip < 0)
                return null;
            Sprite[] frames = _clips[clip].Frames;
            return frames == null || frames.Length == 0 ? null : frames[Mathf.Clamp(index, 0, frames.Length - 1)];
        }

        // The timings in RenderedMotion order, for RenderedCharacterPlayer; frame counts come from the clips.
        public RenderedClipTiming[] Timings()
        {
            var timings = new RenderedClipTiming[RenderedCharacterFrames.MotionCount];
            for (int m = 0; m < timings.Length; m++)
            {
                int t = TimingIndex((RenderedMotion)m);
                timings[m] = new RenderedClipTiming(FrameCount((RenderedMotion)m),
                    t < 0 ? 0f : _timings[t].FramesPerSecond, t >= 0 && _timings[t].Loops);
            }
            return timings;
        }

        private bool EastComplete()
        {
            for (int m = 0; m < RenderedCharacterFrames.MotionCount; m++)
            {
                if (TimingIndex((RenderedMotion)m) < 0)
                    return false;
                int frames = CompleteFrames((RenderedMotion)m, RenderedFacing.South);
                if (frames <= 0)
                    return false;
                for (int f = 1; f < RenderedCharacterFrames.EastFacingCount; f++)
                    if (CompleteFrames((RenderedMotion)m, (RenderedFacing)f) != frames)
                        return false;
            }
            return true;
        }

        // How many of the west clips the set has complete, with their motion's frame count; -1 when one it has is not.
        private int WestClips()
        {
            int complete = 0;
            for (int m = 0; m < RenderedCharacterFrames.MotionCount; m++)
                for (int f = RenderedCharacterFrames.EastFacingCount; f < RenderedCharacterFrames.FacingCount; f++)
                {
                    if (ClipIndex((RenderedMotion)m, (RenderedFacing)f) < 0)
                        continue;
                    if (CompleteFrames((RenderedMotion)m, (RenderedFacing)f) != CompleteFrames((RenderedMotion)m, RenderedFacing.South))
                        return -1;
                    complete++;
                }
            return complete;
        }

        // A clip's frame count when it is there with every frame set; 0 otherwise.
        private int CompleteFrames(RenderedMotion motion, RenderedFacing facing)
        {
            int clip = ClipIndex(motion, facing);
            Sprite[] sprites = clip < 0 ? null : _clips[clip].Frames;
            if (sprites == null || sprites.Length == 0)
                return 0;
            foreach (Sprite sprite in sprites)
                if (sprite == null)
                    return 0;
            return sprites.Length;
        }

        private int TimingIndex(RenderedMotion motion)
        {
            if (_timings == null)
                return -1;
            for (int i = 0; i < _timings.Length; i++)
                if (_timings[i].Motion == motion)
                    return i;
            return -1;
        }

        private int ClipIndex(RenderedMotion motion, RenderedFacing facing)
        {
            if (_clips == null)
                return -1;
            if (_lookup == null)
            {
                var lookup = new int[RenderedCharacterFrames.MotionCount * RenderedCharacterFrames.FacingCount];
                for (int i = 0; i < lookup.Length; i++)
                    lookup[i] = -1;
                for (int i = 0; i < _clips.Length; i++)
                {
                    int m = (int)_clips[i].Motion, f = (int)_clips[i].Facing;
                    if (m >= 0 && m < RenderedCharacterFrames.MotionCount && f >= 0 && f < RenderedCharacterFrames.FacingCount)
                        lookup[m * RenderedCharacterFrames.FacingCount + f] = i;
                }
                _lookup = lookup;
            }
            int key = (int)motion * RenderedCharacterFrames.FacingCount + (int)facing;
            return key >= 0 && key < _lookup.Length ? _lookup[key] : -1;
        }

        // An edit in the Inspector rebuilds the lookup.
        private void OnValidate() => _lookup = null;
    }
}
