using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // A damped spring for the puppet's follow-through: driven by how hard the stride is starting or stopping, it lurches
    // and settles back to zero. Plain state, stepped in fixed slices so a long frame cannot make it ring.
    public struct PuppetSpring
    {
        public float Value;
        public float Velocity;

        // Longest slice of one integration step: below this a spring of these stiffnesses is stable and smooth.
        private const float Slice = 1f / 120f;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Step(float dt, float drive, float omega, float damping, float gain)
        {
            while (dt > 0f)
            {
                float h = dt < Slice ? dt : Slice;
                Velocity += (gain * drive - omega * omega * Value - 2f * damping * omega * Velocity) * h;
                Value += Velocity * h;
                dt -= h;
            }
        }
    }

    // The puppet's clocks and follow-through without Unity (docs/45 section 4 B): the stride's distance-driven phase and
    // its eased weight, the idle's time, the strike, its draw, the cast, the flinch and the collapse, and two springs that
    // make a start lag and a stop lurch - the body quickly, the cape and skirt slower and further. PuppetAnimator feeds it
    // what the hero did each frame; Tools/ArtReview's previewer drives the same class, so a capture shows what the game
    // will play. Presentation only: it observes and never decides anything about combat.
    public sealed class PuppetMotion
    {
        // Below this a frame's floor travel is standing, as for VanguardAnimator.
        public const float MinimumTravel = .0001f;
        // A draw the target walked out of eases back down over this long.
        public const float WindupRelease = 0.12f;
        // The body's spring: quick and fairly firm, a lurch of about a fifth of a second; the cloth's: slower, looser,
        // so the hem swings on after the body has settled. Gains so a full stop lurches each to about one.
        public const float BodyOmega = 15f;
        public const float BodyDamping = 0.45f;
        public const float BodyGain = 20f;
        public const float ClothOmega = 9f;
        public const float ClothDamping = 0.3f;
        public const float ClothGain = 11f;

        private const float FlinchRate = 1f / PuppetGait.FlinchSeconds;
        private const float DeathRate = 1f / PuppetGait.DeathSeconds;
        private const float CastRate = 1f / PuppetGait.CastSeconds;

        private PuppetGaitInput _input;
        private PuppetSpring _body;
        private PuppetSpring _cloth;
        private float _strikeRate;
        private float _windupTarget;
        private PuppetStrike _windupKind;
        private bool _moving;
        private bool _dying;

        public PuppetMotion()
        {
            Pose = PuppetGait.Evaluate(_input);
        }

        // The pose this frame and the clocks it came from, so a test can rebuild it with PuppetGait.
        public PuppetPose Pose { get; private set; }
        public PuppetGaitInput Input => _input;
        public bool IsWalking => _moving;
        public bool IsAttacking => _input.Strike != PuppetStrike.None;
        public bool IsCasting => _input.Casting;
        public bool IsDying => _dying;

        // The painted legs of the facing on show (PuppetRig.Legs), so the bob keeps the supporting boot on its floor.
        public PuppetLegs Legs
        {
            get => _input.Legs;
            set
            {
                _input.Legs = value;
                Pose = PuppetGait.Evaluate(_input);
            }
        }

        // One live, unpaused frame of dt seconds in which the hero travelled this far along the floor. A paused combat
        // (no dt) changes nothing.
        public void Tick(float dt, float travel)
        {
            if (!(dt > 0f))
                return;
            if (IsDying)
            {
                _input.DeathProgress = Min1(_input.DeathProgress + dt * DeathRate);
                Pose = PuppetGait.Evaluate(_input);
                return;
            }

            _moving = travel >= MinimumTravel;
            if (_moving)
                _input.WalkPhase = PuppetGait.Advance(_input.WalkPhase, travel);
            float before = _input.WalkWeight;
            _input.WalkWeight = PuppetGait.Blend(before, _moving, dt);
            // Settled: the next walk starts on a contact, whatever the last one stopped on.
            if (_input.WalkWeight <= 0f)
                _input.WalkPhase = 0f;
            // How hard the stride is stopping (positive) or starting (negative), per second: what lurches the springs.
            float stopping = (before - _input.WalkWeight) / dt;
            _body.Step(dt, stopping, BodyOmega, BodyDamping, BodyGain);
            _cloth.Step(dt, stopping, ClothOmega, ClothDamping, ClothGain);
            _input.Surge = _body.Value;
            _input.Drape = _cloth.Value;

            _input.IdleTime += dt;
            if (_input.IdleTime >= PuppetGait.IdleCycle)
                _input.IdleTime -= PuppetGait.IdleCycle;

            if (_input.Strike != PuppetStrike.None)
            {
                _input.StrikeProgress += dt * _strikeRate;
                if (_input.StrikeProgress >= 1f)
                    EndStrike();
            }
            // The draw follows the weapon's cooldown up exactly and eases down if the target walks away, still the
            // weapon it was drawing while it does.
            if (_windupKind != PuppetStrike.None)
                _input.Windup = _windupKind;
            if (_windupTarget >= _input.WindupProgress)
                _input.WindupProgress = _windupTarget;
            else
            {
                float release = _input.WindupProgress - dt / WindupRelease;
                _input.WindupProgress = release > _windupTarget ? release : _windupTarget;
            }
            if (_input.WindupProgress <= 0f)
                _input.Windup = PuppetStrike.None;

            if (_input.Casting)
            {
                _input.CastProgress += dt * CastRate;
                if (_input.CastProgress >= 1f)
                {
                    _input.Casting = false;
                    _input.CastProgress = 0f;
                }
            }
            if (_input.Flinch > 0f)
            {
                _input.Flinch -= dt * FlinchRate;
                if (_input.Flinch < 0f)
                    _input.Flinch = 0f;
            }
            Pose = PuppetGait.Evaluate(_input);
        }

        // How far through its draw the next strike is, from the weapon's cooldown: 0 while nothing is coming. Read on the
        // next Tick.
        public void Anticipate(PuppetStrike strike, float progress)
        {
            _windupKind = strike;
            _windupTarget = strike == PuppetStrike.None || !(progress > 0f) ? 0f : Min1(progress);
        }

        // The damage has already happened at this event: the strike shows its blow at once, never a delayed windup, and
        // plays for its share of the weapon's interval. Daggers alternate their leading hand.
        public void Strike(PuppetStrike strike, float interval)
        {
            if (IsDying || strike == PuppetStrike.None)
                return;
            if (strike == PuppetStrike.Thrust)
                _input.LeftLeads = !_input.LeftLeads;
            _input.Strike = strike;
            _input.StrikeProgress = 0f;
            _strikeRate = 1f / PuppetGait.StrikeSeconds(interval);
            _input.Windup = PuppetStrike.None;
            _input.WindupProgress = 0f;
            _windupTarget = 0f;
            Pose = PuppetGait.Evaluate(_input);
        }

        // The ability fired: the crouch, raise and slam play over whatever else is happening.
        public void Cast()
        {
            if (IsDying)
                return;
            _input.Casting = true;
            _input.CastProgress = 0f;
            Pose = PuppetGait.Evaluate(_input);
        }

        public void Hit()
        {
            if (IsDying)
                return;
            _input.Flinch = 1f;
            Pose = PuppetGait.Evaluate(_input);
        }

        public void Die()
        {
            if (IsDying)
                return;
            EndStrike();
            _input.Windup = PuppetStrike.None;
            _input.WindupProgress = 0f;
            _windupTarget = 0f;
            _input.Casting = false;
            _input.CastProgress = 0f;
            _input.Flinch = 0f;
            _dying = true;
            Pose = PuppetGait.Evaluate(_input);
        }

        // Drops the strike, its draw and the cast and leaves the stride alone: a hero switched off mid-swing comes back
        // with its arms on the walk.
        public void Interrupt()
        {
            EndStrike();
            _input.Windup = PuppetStrike.None;
            _input.WindupProgress = 0f;
            _windupTarget = 0f;
            _input.Casting = false;
            _input.CastProgress = 0f;
            Pose = PuppetGait.Evaluate(_input);
        }

        // Back to standing: no stride, no strike, no draw, no cast, no flinch, nothing swinging.
        public void Reset()
        {
            EndStrike();
            _input.WalkWeight = 0f;
            _input.WalkPhase = 0f;
            _input.Windup = PuppetStrike.None;
            _input.WindupProgress = 0f;
            _windupTarget = 0f;
            _input.Casting = false;
            _input.CastProgress = 0f;
            _input.Flinch = 0f;
            _input.DeathProgress = 0f;
            _input.Surge = 0f;
            _input.Drape = 0f;
            _body = default;
            _cloth = default;
            _moving = false;
            _dying = false;
            Pose = PuppetGait.Evaluate(_input);
        }

        private void EndStrike()
        {
            _input.Strike = PuppetStrike.None;
            _input.StrikeProgress = 0f;
        }

        private static float Min1(float value) => value < 1f ? value : 1f;
    }
}
