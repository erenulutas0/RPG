using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // Everything a puppet's pose depends on in one frame. The animator owns the clocks and hands them over; the gait itself
    // remembers nothing, so the same input always gives the same pose. A default input is the standing rest pose.
    public struct PuppetGaitInput
    {
        // Where the stride stands, 0 to 1 over a cycle of two steps: 0 is the right foot's contact, 0.5 the left's.
        // Distance drives it (PuppetGait.Advance), never time, so the feet keep pace with the floor at any speed.
        public float WalkPhase;
        // 0 standing, 1 walking. The animator ramps it (PuppetGait.Blend) so starting and stopping ease in and out.
        public float WalkWeight;
        // Seconds of the idle breath; the animator wraps it into one BreathPeriod so the sine never loses precision.
        public float IdleTime;
        public PuppetStrike Strike;
        // 0 at the attack's damage event, 1 once the arms are back on the walk.
        public float StrikeProgress;
        // What is left of a hit's flinch: 1 at the hit, easing to 0.
        public float Flinch;
        // 0 alive, rising to 1 as the figure collapses.
        public float DeathProgress;
    }

    // How a cut-out puppet moves (docs/45 section 4 B): a distance-driven walk with weight - a bob at each contact, a lean
    // into the travel, a cape that lags - an idle breath, an attack overlay on the arms only, a flinch and a collapse.
    // Pure and stateless, so the walk can be tested without Unity and every character shares it.
    //
    // Only float multiplies, adds and Math.Sin / Math.Cos, each formula in one body that is never inlined: this project has
    // already paid for two copies of float arithmetic disagreeing in the last bit (docs/21). The puppet is presentation and
    // touches no combat state, but a pose that is a pure function of its input keeps captures and tests repeatable.
    //
    // Angles are degrees in PuppetPose's terms (positive forward, flexion positive) for a figure facing screen-right.
    public static class PuppetGait
    {
        private const float TwoPi = 6.28318548f;

        // ---- The walk ----

        // World floor units a full cycle of two steps covers. The hero is about 1.25 units tall and walks 2.5 units a
        // second in the hall and 3.5 on the foundry: 1.6 gives three steps a second in the hall and four and a half on the
        // foundry, a brisk jog. The swing is short (HipSwing), so a longer stride would read as a glide and a shorter one
        // as scurrying.
        public const float StrideLength = 1.6f;
        // 1 / StrideLength, exact in binary, so the phase advances by a multiply.
        private const float StridesPerUnit = 0.625f;
        // How fast the walk weight ramps, per second: an eighth of a second from standing to the full stride and back,
        // quick enough that a tap still shows a step, slow enough that stopping settles instead of snapping.
        public const float WalkBlendPerSecond = 8f;
        // Thigh swing either side of the hip at the contacts. At the placeholder's 0.47-unit leg it puts the feet about
        // 0.38 units apart: a grounded step that reads at 80 pixels without the scissor look of a side-view walk, which a
        // three-quarter top-down figure would not show.
        public const float HipSwing = 24f;
        // A standing knee is never locked straight; a soft knee reads as weight.
        public const float KneeRest = 4f;
        // The stance knee's extra give while walking, absorbing each contact.
        public const float KneeStance = 6f;
        // The swing knee's extra fold at the passing pose: at 60 degrees in all the shin lifts the foot about a quarter
        // of its length, enough clearance to see which foot is travelling.
        public const float KneeLift = 50f;
        // How far the hips drop at each contact (world units, about one texel at 32 pixels a unit). Legs spread by
        // HipSwing shorten by leg x (1 - cos 24 degrees), about 0.04 units: dropping a little less keeps both soles
        // touching the floor instead of the stance foot sinking.
        public const float BobDepth = 0.03f;
        // The torso's lean into the travel. Small: the three-quarter camera already tilts the figure toward the viewer.
        public const float WalkLean = 5f;
        // The share of the lean the head takes back, so the gaze stays level while the body leans.
        public const float HeadSteady = 0.6f;
        // The head's small forward nod at each contact, following the drop of the hips.
        public const float HeadNod = 2f;
        // The arms' counter-swing: opposite the legs, as people walk; smaller than the legs so a weapon hand stays near
        // the hip where the weapons were drawn to hang.
        public const float ArmSwing = 18f;
        // The arms at rest hang a touch forward with soft elbows: ready, not slack.
        public const float ShoulderRest = 6f;
        public const float ElbowRest = 10f;
        // The extra elbow fold as an arm swings forward.
        public const float ElbowSwing = 16f;
        // The cape's trail behind a walking figure, and its flutter at twice the stride (one flap per step).
        public const float CapeTrail = 14f;
        public const float CapeFlutter = 5f;
        // How far the flutter lags behind the step, in radians of the doubled stride: about a seventh of a step, so the
        // cloth answers the bob instead of moving with it.
        private const float CapeLagRadians = 0.9f;

        // ---- The breath ----

        // One breath every 2.4 seconds: a resting rate, slow enough not to read as panting.
        public const float BreathPeriod = 2.4f;
        // 2 pi / BreathPeriod.
        private const float BreathRadiansPerSecond = 2.61799383f;
        // The chest's rise either side of rest (world units): under a texel, so it shows as a shimmer of the shoulders
        // rather than a hop.
        public const float BreathLift = 0.025f;
        // The shoulders and the cape follow the breath by this many degrees either side.
        public const float BreathShoulder = 2f;
        public const float BreathCape = 2f;

        // ---- The strike ----

        // The arms hold the strike's pose for this share of the strike, then ease back onto the walk. Damage has already
        // happened at the event, so the pose shows the hit at once and never begins a delayed windup.
        public const float StrikeHold = 0.3f;
        private const float StrikeRecoverRate = 1f / (1f - StrikeHold);
        // The follow-through rises and falls once inside the hold: pi / StrikeHold radians of progress.
        private const float FollowRadiansPerProgress = 3.14159274f / StrikeHold;
        // The longest a strike plays, and its share of the weapon's interval below that, so the arms are back on the walk
        // before the next attack even with Tempo: a 0.5-second staff strikes for 0.28 seconds and rests for the remainder.
        public const float MaxStrikeSeconds = 0.28f;
        public const float StrikeShareOfInterval = 0.65f;

        // Per strike, the arms' pose at the hit: right shoulder, elbow, wrist, then the left's; then the follow-through
        // added at its peak to the right shoulder, the right wrist and the left shoulder. Wrists carry the weapon: the
        // Sword's blade lands forward and down along the arm (the sprite is drawn pointing up and forward, so the wrist
        // turns it 110 degrees down) and keeps travelling a little; the Staff leans 15 degrees forward with the free palm
        // out; the Daggers jab both blades straight along the arms.
        private static readonly float[] StrikeArms =
        {
            // Swing: the shield arm comes up in front as a guard.
            40f, 5f, -110f, 30f, 55f, 0f, -12f, -25f, 0f,
            // Cast
            55f, 0f, -70f, 65f, 15f, 20f, 5f, -10f, 0f,
            // Thrust
            70f, 0f, -135f, 70f, 0f, -135f, 10f, 0f, 10f,
        };
        private const int StrikeColumns = 9;

        // ---- The flinch ----

        // How long a hit's flinch lasts: a little longer than the hit flash (0.14 s), so the body is still recoiling as
        // the flash ends.
        public const float FlinchSeconds = 0.22f;
        // At the hit the torso snaps back, the head further, the arms flare back; legs keep walking.
        public const float FlinchLean = 12f;
        public const float FlinchHead = 10f;
        public const float FlinchArms = 10f;

        // ---- The collapse ----

        // A death plays for this long, then the figure lies still.
        public const float DeathSeconds = 0.8f;
        // The knees give in the first 35 per cent; from a quarter of the way the figure topples backward about its feet,
        // away from what it faced, to almost flat.
        private const float BuckleRate = 1f / 0.35f;
        private const float ToppleStart = 0.25f;
        private const float ToppleRate = 1f / 0.75f;
        public const float FallAngle = 84f;
        // The kneeling pose the knees buckle into, then the lying pose the topple ends in, channel by channel: lift
        // (world units), lean, head, cape, hips, knees, shoulders, elbows. Lying, the arms are thrown forward by the fall
        // and the back arches as it lands.
        private static readonly float[] Kneel = { -0.12f, 22f, 25f, 20f, 30f, 75f, 10f, 25f };
        private static readonly float[] Lying = { -0.14f, -8f, -10f, 30f, 15f, 40f, 70f, 35f };

        // The standing rest pose: legs straight but soft, arms hanging slightly forward, nothing breathing.
        public static PuppetPose Rest => Evaluate(default);

        // The stride's phase after travelling this far along the floor: one cycle every StrideLength, wrapped to [0, 1).
        // Nothing moves for a travel that is not a positive finite distance.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Advance(float phase, float travel)
        {
            if (float.IsNaN(phase) || float.IsInfinity(phase) || phase < 0f)
                phase = 0f;
            if (travel > 0f && !float.IsInfinity(travel))
                phase += travel * StridesPerUnit;
            return phase - (float)Math.Floor(phase);
        }

        // The walk weight after dt seconds of walking or standing: a linear ramp, clamped to [0, 1]. Evaluate eases it.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Blend(float weight, bool walking, float dt)
        {
            if (!(dt > 0f) || float.IsInfinity(dt))
                return Clamp01(weight);
            float step = dt * WalkBlendPerSecond;
            return Clamp01(walking ? weight + step : weight - step);
        }

        // How long a strike plays for a weapon attacking every interval seconds.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float StrikeSeconds(float interval)
        {
            if (!(interval > 0f) || float.IsInfinity(interval))
                return MaxStrikeSeconds;
            float share = interval * StrikeShareOfInterval;
            return share < MaxStrikeSeconds ? share : MaxStrikeSeconds;
        }

        // The pose for one frame. The walk and the breath are blended by the walk weight; the strike replaces the arms
        // only; the flinch bends the upper body only; the collapse takes over everything.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static PuppetPose Evaluate(PuppetGaitInput input)
        {
            float theta = Clamp01(input.WalkPhase) * TwoPi;
            float c = (float)Math.Cos(theta);
            float s = (float)Math.Sin(theta);
            float walk = Ease(Clamp01(input.WalkWeight));
            float idle = float.IsNaN(input.IdleTime) || float.IsInfinity(input.IdleTime) ? 0f : input.IdleTime;
            float breath = (1f - walk) * (float)Math.Sin(idle * BreathRadiansPerSecond);
            // Each foot swings through while the sine is on its side: the left from its back contact at 0 to its front
            // contact at 0.5, the right from 0.5 to 1. Squared, so the knee leaves the stance bend smoothly.
            float swingLeft = s > 0f ? s : 0f;
            float swingRight = s < 0f ? -s : 0f;

            var pose = new PuppetPose();
            pose.HipRight = walk * HipSwing * c;
            pose.HipLeft = -pose.HipRight;
            pose.KneeLeft = KneeRest + walk * (KneeStance + KneeLift * swingLeft * swingLeft);
            pose.KneeRight = KneeRest + walk * (KneeStance + KneeLift * swingRight * swingRight);
            // Lowest at both contacts, where the legs are spread; highest as the feet pass.
            pose.Lift = -walk * BobDepth * c * c;
            pose.Lean = walk * WalkLean;
            pose.Head = walk * HeadNod * c * c - HeadSteady * pose.Lean;
            pose.Breath = BreathLift * breath;
            pose.Cape = walk * (CapeTrail + CapeFlutter * (float)Math.Sin(2f * theta - CapeLagRadians)) + BreathCape * breath;
            // Each arm swings against its own side's leg.
            float armSwing = walk * ArmSwing * c;
            pose.ShoulderRight = ShoulderRest - armSwing + BreathShoulder * breath;
            pose.ShoulderLeft = ShoulderRest + armSwing + BreathShoulder * breath;
            pose.ElbowRight = ElbowRest + walk * ElbowSwing * (0.5f - 0.5f * c);
            pose.ElbowLeft = ElbowRest + walk * ElbowSwing * (0.5f + 0.5f * c);

            if (input.Strike != PuppetStrike.None)
                Strike(ref pose, input.Strike, Clamp01(input.StrikeProgress));

            float flinch = Clamp01(input.Flinch);
            flinch *= flinch;
            pose.Lean -= FlinchLean * flinch;
            pose.Head -= FlinchHead * flinch;
            pose.ShoulderRight -= FlinchArms * flinch;
            pose.ShoulderLeft -= FlinchArms * flinch;

            float death = Clamp01(input.DeathProgress);
            if (death > 0f)
                Collapse(ref pose, death);
            return pose;
        }

        // The attack's arms over whatever the walk gave them; nothing below the shoulders' parents changes.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Strike(ref PuppetPose pose, PuppetStrike strike, float progress)
        {
            int row;
            switch (strike)
            {
                case PuppetStrike.Swing:
                    row = 0;
                    break;
                case PuppetStrike.Cast:
                    row = 1;
                    break;
                case PuppetStrike.Thrust:
                    row = 2;
                    break;
                default:
                    return;
            }
            int at = row * StrikeColumns;
            float hold = progress < StrikeHold ? 1f : 1f - Ease(Clamp01((progress - StrikeHold) * StrikeRecoverRate));
            float follow = progress < StrikeHold ? (float)Math.Sin(progress * FollowRadiansPerProgress) : 0f;
            pose.ShoulderRight += hold * (StrikeArms[at] - pose.ShoulderRight) + follow * StrikeArms[at + 6];
            pose.ElbowRight += hold * (StrikeArms[at + 1] - pose.ElbowRight);
            pose.WristRight += hold * (StrikeArms[at + 2] - pose.WristRight) + follow * StrikeArms[at + 7];
            pose.ShoulderLeft += hold * (StrikeArms[at + 3] - pose.ShoulderLeft) + follow * StrikeArms[at + 8];
            pose.ElbowLeft += hold * (StrikeArms[at + 4] - pose.ElbowLeft);
            pose.WristLeft += hold * (StrikeArms[at + 5] - pose.WristLeft);
        }

        // The collapse over the living pose: the knees buckle into a kneel, then the figure topples into the lying pose.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Collapse(ref PuppetPose pose, float death)
        {
            float buckle = Ease(Clamp01(death * BuckleRate));
            float topple = Ease(Clamp01((death - ToppleStart) * ToppleRate));
            pose.Fall = topple * FallAngle;
            pose.Lift += buckle * (Kneel[0] + topple * (Lying[0] - Kneel[0]) - pose.Lift);
            pose.Lean += buckle * (Kneel[1] + topple * (Lying[1] - Kneel[1]) - pose.Lean);
            pose.Head += buckle * (Kneel[2] + topple * (Lying[2] - Kneel[2]) - pose.Head);
            pose.Cape += buckle * (Kneel[3] + topple * (Lying[3] - Kneel[3]) - pose.Cape);
            pose.Breath -= buckle * pose.Breath;
            float hip = Kneel[4] + topple * (Lying[4] - Kneel[4]);
            float knee = Kneel[5] + topple * (Lying[5] - Kneel[5]);
            float shoulder = Kneel[6] + topple * (Lying[6] - Kneel[6]);
            float elbow = Kneel[7] + topple * (Lying[7] - Kneel[7]);
            pose.HipLeft += buckle * (hip - pose.HipLeft);
            pose.HipRight += buckle * (hip - pose.HipRight);
            pose.KneeLeft += buckle * (knee - pose.KneeLeft);
            pose.KneeRight += buckle * (knee - pose.KneeRight);
            pose.ShoulderLeft += buckle * (shoulder - pose.ShoulderLeft);
            pose.ShoulderRight += buckle * (shoulder - pose.ShoulderRight);
            pose.ElbowLeft += buckle * (elbow - pose.ElbowLeft);
            pose.ElbowRight += buckle * (elbow - pose.ElbowRight);
            pose.WristLeft -= buckle * pose.WristLeft;
            pose.WristRight -= buckle * pose.WristRight;
        }

        // Smoothstep: eases a 0..1 ramp in and out.
        private static float Ease(float t) => t * t * (3f - 2f * t);

        private static float Clamp01(float value) => value > 0f ? (value < 1f ? value : 1f) : 0f;
    }
}
