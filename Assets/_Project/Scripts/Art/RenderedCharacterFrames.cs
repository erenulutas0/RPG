using System;

namespace Cryptforge.Art
{
    // The five facings a pre-rendered character is drawn in (docs/45: Ilya, rendered from one Blender model). The game
    // mirrors the east ones for the west, so south-west, west and north-west come from south-east, east and north-east.
    public enum RenderedFacing
    {
        South = 0,
        SouthEast = 1,
        East = 2,
        NorthEast = 3,
        North = 4
    }

    // What a pre-rendered character can be doing. The walk is driven by distance, every other motion by time. The walk
    // strike is the walk's legs under the strike's arms: a walking hero keeps its stride while it attacks (the owner,
    // 2026-10-26: attacks froze the Vanguard's legs and it glided).
    public enum RenderedMotion
    {
        Idle = 0,
        Walk = 1,
        WalkStrike = 2,
        Strike = 3,
        Cast = 4,
        Hit = 5,
        Death = 6
    }

    // One motion's timing: its frame count, frames a second (unused by the distance-driven walks) and whether it loops.
    public struct RenderedClipTiming
    {
        public int Frames;
        public float FramesPerSecond;
        public bool Loops;

        public RenderedClipTiming(int frames, float framesPerSecond, bool loops)
        {
            Frames = frames;
            FramesPerSecond = framesPerSecond;
            Loops = loops;
        }

        // How long a one-shot plays, in seconds.
        public float Seconds => FramesPerSecond > 0f ? Frames / FramesPerSecond : 0f;
    }

    // Pure frame selection for a pre-rendered character: which of eight floor directions a displacement points in,
    // which rendered facing draws it, and which frame of a clip a distance or a clock shows. No Unity, so the rules are
    // tested in the .NET checks.
    public static class RenderedCharacterFrames
    {
        public const int FacingCount = 5;
        public const int MotionCount = 7;
        public const int SectorCount = 8;
        // Sectors count counter-clockwise from east on the floor: 2 is north (up the screen), 6 south (toward the camera).
        public const int EastSector = 0;
        public const int NorthSector = 2;
        public const int WestSector = 4;
        public const int SouthSector = 6;
        // A displacement must leave its sector by this many degrees before the facing turns, so travel along a
        // sector's edge never flickers between two drawings.
        public const float TurnMargin = 12f;
        // The longest a strike plays, and its share of the weapon's interval below that, so the arms are back before
        // the next attack even with Tempo (as PuppetGait): a 0.5-second staff strikes for its whole clip.
        public const float MaxStrikeSeconds = 0.25f;
        public const float StrikeShareOfInterval = 0.65f;

        private const float DegreesPerSector = 360f / SectorCount;

        // The sector a floor direction points in; a zero displacement has none (-1).
        public static int Sector(float floorX, float floorY)
        {
            if (floorX * floorX + floorY * floorY < 1e-12f)
                return -1;
            float degrees = Degrees(floorX, floorY);
            return (int)Math.Floor((degrees + DegreesPerSector * 0.5f) / DegreesPerSector) % SectorCount;
        }

        // The current sector while the direction stays within its edges plus the margin, otherwise the direction's own.
        public static int KeepSector(int current, float floorX, float floorY, float marginDegrees = TurnMargin)
        {
            int next = Sector(floorX, floorY);
            if (next < 0)
                return current;
            if (current < 0 || current >= SectorCount)
                return next;
            float off = Math.Abs(DeltaDegrees(Degrees(floorX, floorY), current * DegreesPerSector));
            return off <= DegreesPerSector * 0.5f + marginDegrees ? current : next;
        }

        // The rendered facing that draws a sector, and whether it is drawn mirrored (the west half of the compass).
        public static RenderedFacing FacingOf(int sector, out bool mirrored)
        {
            switch (sector)
            {
                case 0: mirrored = false; return RenderedFacing.East;
                case 1: mirrored = false; return RenderedFacing.NorthEast;
                case 2: mirrored = false; return RenderedFacing.North;
                case 3: mirrored = true; return RenderedFacing.NorthEast;
                case 4: mirrored = true; return RenderedFacing.East;
                case 5: mirrored = true; return RenderedFacing.SouthEast;
                case 7: mirrored = false; return RenderedFacing.SouthEast;
                default: mirrored = false; return RenderedFacing.South;
            }
        }

        // The walk's frame for the distance walked: the nearest of `frames` poses rendered evenly over one stride, the
        // first at the right foot's contact. Distance, never time, so the feet keep pace with the floor at any speed.
        public static int WalkFrame(float distance, float strideLength, int frames)
        {
            if (frames <= 0 || !(strideLength > 0f))
                return 0;
            double phase = distance / strideLength;
            phase -= Math.Floor(phase);
            return (int)Math.Floor(phase * frames + 0.5) % frames;
        }

        // The frame a clock shows: looping clips wrap, one-shots hold their last frame.
        public static int TimedFrame(float elapsed, float framesPerSecond, int frames, bool loops)
        {
            if (frames <= 0 || !(elapsed > 0f) || !(framesPerSecond > 0f))
                return 0;
            int frame = (int)Math.Floor(elapsed * framesPerSecond);
            return loops ? frame % frames : Math.Min(frame, frames - 1);
        }

        // The frame a progress from 0 (start) to 1 (end) shows on a clip of `frames`.
        public static int ProgressFrame(float progress, int frames)
        {
            if (frames <= 0 || !(progress > 0f))
                return 0;
            return Math.Min(frames - 1, (int)Math.Floor(progress * frames));
        }

        // How long a strike plays for a weapon attacking every `interval` seconds.
        public static float StrikeSeconds(float interval)
        {
            float share = interval > 0f ? interval * StrikeShareOfInterval : MaxStrikeSeconds;
            return Math.Min(MaxStrikeSeconds, share);
        }

        private static float Degrees(float x, float y)
        {
            float degrees = (float)(Math.Atan2(y, x) * (180.0 / Math.PI));
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private static float DeltaDegrees(float a, float b)
        {
            float d = (a - b) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }
    }

    // Everything a pre-rendered character decides each live tick, pure: the motion it shows, the facing it is drawn in,
    // whether that drawing is mirrored, and the frame. RenderedCharacterAnimator feeds it the floor displacement and the
    // combat events and draws what it says; it allocates nothing after construction.
    //
    // The rules, as VanguardAnimator's and PuppetAnimator's: travel turns the facing (with a margin), a standing strike
    // faces its target while a walking one keeps the travel facing, a strike never stops the legs (the walk strike), a
    // hit flinches only a hero standing idle (a walking or striking hero only flashes), the Vent's cast plays whole,
    // death plays once and holds, and a tick without time changes nothing.
    public sealed class RenderedCharacterPlayer
    {
        // Below this a tick's floor travel is standing, as for VanguardAnimator.
        public const float MinimumTravel = 0.0001f;
        // Standing this long ends the stride: the next walk starts again on a contact. Shorter breaks keep the stride,
        // so a one-frame stall in the steering never pops the legs back to the first frame.
        public const float StopGrace = 0.12f;

        private readonly RenderedClipTiming[] _timings;
        private readonly float _strideLength;
        private int _sector = RenderedCharacterFrames.SouthSector;
        private float _distance;
        private float _standing;
        private float _idleTime;
        private float _strikeRemaining;
        private float _strikeDuration;
        private float _castTime = -1f;
        private float _hitTime = -1f;
        private float _deathTime = -1f;

        // timings: one per RenderedMotion, in its order. strideLength: floor units of one walk cycle (two steps).
        public RenderedCharacterPlayer(RenderedClipTiming[] timings, float strideLength)
        {
            if (timings == null || timings.Length != RenderedCharacterFrames.MotionCount)
                throw new ArgumentException("One timing per rendered motion.", nameof(timings));
            if (!(strideLength > 0f))
                throw new ArgumentOutOfRangeException(nameof(strideLength));
            _timings = (RenderedClipTiming[])timings.Clone();
            _strideLength = strideLength;
            Show();
        }

        public RenderedMotion Motion { get; private set; }
        public int Frame { get; private set; }
        public RenderedFacing Facing { get; private set; }
        public bool Mirrored { get; private set; }
        public int Sector => _sector;
        public bool IsMoving { get; private set; }
        public bool IsStriking => _strikeRemaining > 0f;
        public bool IsCasting => _castTime >= 0f;
        public bool IsDying => _deathTime >= 0f;
        public float WalkDistance => _distance;

        public RenderedClipTiming Timing(RenderedMotion motion) => _timings[(int)motion];

        // One live tick: dt seconds and this tick's floor displacement (world y already divided by the depth scale).
        public void Step(float dt, float floorDx, float floorDy)
        {
            if (!(dt > 0f))
                return;
            if (IsDying)
            {
                _deathTime += dt;
                Show();
                return;
            }
            float travel = (float)Math.Sqrt(floorDx * floorDx + floorDy * floorDy);
            IsMoving = travel >= MinimumTravel;
            if (IsMoving)
            {
                _standing = 0f;
                _distance = (_distance + travel) % _strideLength;
                _sector = RenderedCharacterFrames.KeepSector(_sector, floorDx, floorDy);
                _idleTime = 0f;
                _hitTime = -1f;
            }
            else
            {
                _standing += dt;
                if (_standing >= StopGrace)
                    _distance = 0f;
            }
            if (_strikeRemaining > 0f)
                _strikeRemaining = Math.Max(0f, _strikeRemaining - dt);
            if (_castTime >= 0f)
            {
                _castTime += dt;
                if (_castTime >= Timing(RenderedMotion.Cast).Seconds)
                    _castTime = -1f;
            }
            if (_hitTime >= 0f)
            {
                _hitTime += dt;
                if (_hitTime >= Timing(RenderedMotion.Hit).Seconds)
                    _hitTime = -1f;
            }
            if (!IsMoving && !IsStriking && !IsCasting && _hitTime < 0f)
                _idleTime += dt;
            Show();
        }

        // The first weapon struck: the damage has landed, so the strike shows at once, never a delayed windup.
        public void Strike(float seconds)
        {
            if (IsDying)
                return;
            _strikeDuration = _strikeRemaining = Math.Max(seconds, 1e-3f);
            _hitTime = -1f;
            Show();
        }

        // A standing hero turns to what it struck; a walking one keeps its travel facing, so a nearby enemy cannot turn
        // the stride round every swing.
        public void FaceToward(float floorDx, float floorDy)
        {
            if (IsDying || IsMoving)
                return;
            int sector = RenderedCharacterFrames.Sector(floorDx, floorDy);
            if (sector < 0)
                return;
            _sector = sector;
            Show();
        }

        public void Cast()
        {
            if (IsDying)
                return;
            _castTime = 0f;
            _hitTime = -1f;
            Show();
        }

        // A hit flinches a hero standing idle; walking, striking or casting it only flashes.
        public void Hit()
        {
            if (IsDying || IsMoving || IsStriking || IsCasting)
                return;
            _hitTime = 0f;
            Show();
        }

        public void Die()
        {
            if (IsDying)
                return;
            _deathTime = 0f;
            _strikeRemaining = 0f;
            _castTime = -1f;
            _hitTime = -1f;
            IsMoving = false;
            Show();
        }

        // Back to standing idle where it faces; a fallen hero stays fallen.
        public void Reset()
        {
            if (IsDying)
                return;
            _distance = _standing = _idleTime = _strikeRemaining = 0f;
            _castTime = _hitTime = -1f;
            IsMoving = false;
            Show();
        }

        private void Show()
        {
            Facing = RenderedCharacterFrames.FacingOf(_sector, out bool mirrored);
            Mirrored = mirrored;
            if (IsDying)
            {
                Motion = RenderedMotion.Death;
                Frame = Timed(RenderedMotion.Death, _deathTime);
            }
            else if (IsCasting)
            {
                Motion = RenderedMotion.Cast;
                Frame = Timed(RenderedMotion.Cast, _castTime);
            }
            else if (IsStriking)
            {
                Motion = IsMoving ? RenderedMotion.WalkStrike : RenderedMotion.Strike;
                Frame = IsMoving
                    ? RenderedCharacterFrames.WalkFrame(_distance, _strideLength, Timing(RenderedMotion.WalkStrike).Frames)
                    : RenderedCharacterFrames.ProgressFrame(1f - _strikeRemaining / _strikeDuration,
                        Timing(RenderedMotion.Strike).Frames);
            }
            else if (_hitTime >= 0f)
            {
                Motion = RenderedMotion.Hit;
                Frame = Timed(RenderedMotion.Hit, _hitTime);
            }
            else if (IsMoving)
            {
                Motion = RenderedMotion.Walk;
                Frame = RenderedCharacterFrames.WalkFrame(_distance, _strideLength, Timing(RenderedMotion.Walk).Frames);
            }
            else
            {
                Motion = RenderedMotion.Idle;
                Frame = Timed(RenderedMotion.Idle, _idleTime);
            }
        }

        private int Timed(RenderedMotion motion, float elapsed)
        {
            RenderedClipTiming timing = Timing(motion);
            return RenderedCharacterFrames.TimedFrame(elapsed, timing.FramesPerSecond, timing.Frames, timing.Loops);
        }
    }
}
