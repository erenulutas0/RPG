using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // One leg as it was painted, for the bob: the knee from the hip, and the heel and toe of the boot's sole from the
    // knee, in world units for the figure facing right. PuppetRig reads it off the parts; all zero is a reference leg.
    public struct PuppetLeg
    {
        public float KneeX;
        public float KneeY;
        public float HeelX;
        public float HeelY;
        public float ToeX;
        public float ToeY;

        // A real leg's knee hangs below its hip.
        public readonly bool IsSet => KneeY < 0f;
    }

    public struct PuppetLegs
    {
        public PuppetLeg Right;
        public PuppetLeg Left;
    }

    // Everything a puppet's pose depends on in one frame. The owner of the clocks (PuppetMotion) hands them over; the gait
    // itself remembers nothing, so the same input always gives the same pose. A default input is the standing rest pose.
    public struct PuppetGaitInput
    {
        // Where the stride stands, 0 to 1 over a cycle of two steps: 0 is the right foot's contact, 0.5 the left's.
        // Distance drives it (PuppetGait.Advance), never time, so the feet keep pace with the floor at any speed.
        public float WalkPhase;
        // 0 standing, 1 walking. Ramped (PuppetGait.Blend) so starting and stopping ease in and out.
        public float WalkWeight;
        // Seconds of the idle, wrapped into one IdleCycle so the sines never lose precision.
        public float IdleTime;
        // The strike playing: 0 at the attack's damage event, 1 once the arms are back on the walk.
        public PuppetStrike Strike;
        public float StrikeProgress;
        // Which hand jabs a Thrust: the daggers alternate.
        public bool LeftLeads;
        // The next strike being drawn back before its damage event: 0 at rest, 1 at the top of the draw.
        public PuppetStrike Windup;
        public float WindupProgress;
        // The ability's cast: crouch, raise and slam, 0 to 1 over CastSeconds.
        public bool Casting;
        public float CastProgress;
        // What is left of a hit's flinch: 1 at the hit, easing to 0.
        public float Flinch;
        // 0 alive, rising to 1 as the figure collapses.
        public float DeathProgress;
        // The follow-through springs (PuppetMotion): the body's lurch past a stop or behind a start, and the cloth's,
        // which is slower and longer. Positive is forward; zero at rest and through a steady walk.
        public float Surge;
        public float Drape;
        // The painted legs, so the bob keeps the supporting boot on its own floor line; default is a reference leg.
        public PuppetLegs Legs;
    }

    // How a cut-out puppet moves (docs/45 section 4 B), pure and stateless, so the motion is tested without Unity and
    // every character shares it. A distance-driven jog with weight - the hips drop as each foot lands and rise as they
    // pass, the shoulders and arms swing against the legs, the cape and skirt flutter behind - an idle that breathes and
    // shifts its weight, strikes drawn back before the hit and followed through after it on the arms and chest only, the
    // ability's crouch-raise-slam, a flinch that snaps back and rebounds, and a collapse that buckles and then falls.
    //
    // Only float multiplies, adds and Math.Sin / Cos / Sqrt / Exp, each formula in one body that is never inlined: this
    // project has already paid for two copies of float arithmetic disagreeing in the last bit (docs/21). The puppet is
    // presentation and touches no combat state, but a pose that is a pure function of its input keeps captures and tests
    // repeatable.
    //
    // Angles are degrees in PuppetPose's terms (positive forward, flexion positive), offsets from the painted pose, for a
    // figure facing screen-right. Every amount here was tuned on the Vanguard at the game's framing, where it stands about
    // 165 phone pixels tall (1.375 world units at 120 pixels a unit).
    public static class PuppetGait
    {
        private const float TwoPi = 6.28318548f;
        private const float DegToRad = 0.0174532924f;

        // ---- The stride ----

        // World floor units a full cycle of two steps covers. The hero walks 2.5 units a second in the hall and 3.5 on
        // the foundry - two and a half heights a second, a jog - so 1.6 gives three steps a second in the hall and four
        // and a half on the foundry: quick steps for the short armoured legs, never a skating glide.
        public const float StrideLength = 1.6f;
        // 1 / StrideLength, exact in binary, so the phase advances by a multiply.
        private const float StridesPerUnit = 0.625f;
        // How fast the walk weight ramps, per second: an eighth of a second from standing to the full stride and back,
        // quick enough that a tap still shows a step, slow enough that stopping settles instead of snapping.
        public const float WalkBlendPerSecond = 8f;

        // The legs over one cycle, sixteen keys from the right foot's contact; the left leg plays them half a cycle later.
        // Each foot lands reaching forward with the knee nearly straight (the contact), takes the weight on a bending knee
        // (the down), carries the body over it while it sweeps back near-linearly so the planted boot keeps pace with the
        // floor (the passing), pushes off behind (the up), then folds the knee and swings through faster than it swept
        // back. The planted sweep is about half a unit, what the floor travels across the screen under a foot in its
        // stance at the stride above, walking on the diagonal the three-quarter facings show.
        public const int StrideKeys = 16;
        // The stance keys were solved on the Vanguard's near leg so the boot's sole sweeps back at an even pace, the
        // knee's give included: a planted boot that stays planted.
        private static readonly float[] ThighKeys =
        {
            26f, 24.9f, 20.9f, 11.5f, 1.4f, -7.1f, -14.2f, -19.1f, -22f, -22f, -14f, -3f, 8.5f, 18f, 25f, 27.5f
        };
        // The swing knee folds no further than about forty degrees: a cut-out shin turns in the screen's plane, so a
        // deeper fold reads, at this three-quarter view, as a kick back rather than a lifted foot.
        private static readonly float[] KneeKeys =
        {
            4f, 12f, 16f, 13f, 9f, 7f, 7f, 10f, 16f, 25f, 33f, 38f, 34f, 24f, 12f, 5f
        };
        // The thigh's swing at contact: the widest a stride spreads the legs.
        public const float HipSwing = 26f;
        // A standing knee is never locked straight; a soft knee reads as weight.
        public const float KneeRest = 3f;
        // The geometric hip drop: a leg spread by its swing, or shortened by its bent knee, reaches less far below the hip,
        // so the pelvis sinks until the boot that reaches lowest meets its floor - lowest just after each contact,
        // highest as the feet pass, with no foot sinking into the floor or hovering over it. The boot's sole is a heel
        // and a toe, so a foot thrown forward lands on its heel and one behind rolls onto its toe. Without painted legs
        // (PuppetGaitInput.Legs), a reference leg about the Vanguard's (world units).
        public const float ThighReach = 0.12f;
        public const float ShinReach = 0.44f;
        private const float HeelBack = 0.07f;
        private const float ToeAhead = 0.05f;
        // How much of an arm's walking swing the wrist takes back, so a carried weapon stays near its resting angle
        // instead of fanning with the arm.
        public const float WristCarry = 0.7f;
        // The torso's lean into the travel, and how much deeper it dips as each foot takes the weight.
        public const float WalkLean = 6f;
        public const float LeanBob = 1.5f;
        // The share of the lean the head takes back, so the gaze stays level while the body leans, and its small nod as
        // the hips drop, a little after them.
        public const float HeadSteady = 0.6f;
        public const float HeadNod = 2f;
        // The arms' counter-swing: each opposite its own side's leg, a little behind it, so it reads as swung, not
        // driven. Smaller than the legs, so a weapon hand stays near the hip; the elbow folds further as the arm swings
        // forward, as a jogger's does.
        public const float ArmSwing = 18f;
        private const float ArmLag = 0.04f;
        public const float ElbowSwing = 18f;
        // The arms at rest hang a touch forward with soft elbows: ready, not slack.
        public const float ShoulderRest = 2f;
        public const float ElbowRest = 4f;
        // The shoulders turn against the hips, the near shoulder back as the near foot lands (world units each shoulder
        // joint moves); the hips turn with the legs.
        public const float ShoulderTwist = 0.022f;
        public const float HipTurn = 0.012f;
        // The cape's trail behind a running figure and its flutter, once a step, lagging the bob; the skirt's, smaller.
        // Both stay small: the painted cape hangs most of the figure's height from its hood, so a degree moves its hem
        // two texels.
        public const float CapeTrail = 4.5f;
        public const float CapeFlutter = 2.5f;
        private const float CapeLagRadians = 1.1f;
        public const float SkirtTrail = 1.5f;
        public const float SkirtFlutter = 2f;
        private const float SkirtLagRadians = 0.6f;

        // ---- The follow-through (PuppetMotion's springs) ----

        // A Surge of 1 - the lurch of a hard stop - leans the torso this far forward and swings both arms forward; the
        // knees take it with a dip. A Drape of 1 swings the cape's and skirt's hems forward.
        public const float SurgeLean = 9f;
        public const float SurgeArms = 12f;
        public const float SurgeKnee = 8f;
        public const float DrapeCape = 9f;
        public const float DrapeSkirt = 4f;

        // ---- The idle ----

        // One breath every 2.8 seconds: a resting rate, slow enough not to read as panting.
        public const float BreathPeriod = 2.8f;
        // 2 pi / BreathPeriod.
        private const float BreathRadiansPerSecond = 2.24399475f;
        // The weight shifts from one foot to the other once every two breaths.
        public const float IdleCycle = 5.6f;
        private const float ShiftRadiansPerSecond = 1.12199738f;
        // The chest's rise either side of rest (world units): about a texel, so it shows as a lift of the shoulders
        // rather than a hop.
        public const float BreathLift = 0.008f;
        // The shoulders, the head and the cape follow the breath by this many degrees either side.
        public const float BreathShoulder = 1.5f;
        public const float BreathHead = 1f;
        public const float BreathCape = 1f;
        // The weight shift: the pelvis travels this far either side (world units, a texel), the legs lean to keep the
        // boots planted, and the knee that gives up the weight softens.
        public const float WeightShift = 0.008f;
        public const float WeightKnee = 4f;
        private const float WeightHipTurn = 0.004f;

        // ---- The strikes ----

        // Each strike's timeline from its damage event: the blade is already halfway through its blow on the event's own
        // frame and lands a frame later (HitAt), so damage is never shown before a delayed windup; it follows through
        // until FollowAt, holds until StrikeHold and eases back onto the walk. The draw before it is PuppetMotion's: the
        // weapon rises as its cooldown runs out with a target in reach, so the top of the draw meets the event.
        public const float HitAt = 0.12f;
        private const float FollowAt = 0.22f;
        public const float StrikeHold = 0.4f;
        // The longest a strike plays, and its share of the weapon's interval below that, so the arms are back on the walk
        // before the next attack even with Tempo: a 0.5-second staff strikes for 0.3 seconds and rests for the remainder.
        public const float MaxStrikeSeconds = 0.3f;
        public const float StrikeShareOfInterval = 0.6f;
        // The draw: the longest it takes, and its share of the interval below that.
        public const float MaxWindupSeconds = 0.14f;
        public const float WindupShareOfInterval = 0.25f;

        // Per strike, four poses of the arms and chest: drawn back (the windup's top), passing (the frame of the damage
        // event, the blade halfway), the hit and the follow-through. Columns: right shoulder, elbow, wrist; left shoulder,
        // elbow, wrist; lean (degrees); twist (world units the right shoulder leads). The Sword is drawn pointing up and
        // forward from the fist, the Staff upright, the Daggers up; the wrists turn them from there.
        //   Swing: raised high, the blade cocked back over the helmet; chopped down through the target in front; carried
        //   on down and across; the shield arm guards forward throughout.
        //   Cast: the staff lifted and tipped back; its head thrust at the target, the free palm following.
        //   Thrust: both blades drawn back to the hips; the leading one jabbed level along its arm, the other cocked.
        private const int StrikeColumns = 8;
        private static readonly float[] SwingPoses =
        {
            125f, 20f, -55f, 30f, 45f, 0f, -6f, -0.03f,
            95f, 10f, -70f, 32f, 45f, 0f, 2f, 0f,
            45f, 4f, -105f, 28f, 50f, 0f, 10f, 0.035f,
            22f, 14f, -122f, 22f, 50f, 0f, 9f, 0.04f,
        };
        private static readonly float[] CastPoses =
        {
            40f, 55f, 25f, 15f, 30f, 0f, -5f, -0.015f,
            62f, 25f, -15f, 35f, 20f, 10f, 2f, 0.01f,
            78f, 4f, -48f, 55f, 8f, 12f, 7f, 0.025f,
            68f, 10f, -38f, 45f, 12f, 8f, 5f, 0.02f,
        };
        private static readonly float[] ThrustPoses =
        {
            -28f, 82f, -118f, -24f, 78f, -112f, -4f, -0.01f,
            40f, 40f, -140f, -26f, 80f, -112f, 2f, 0.02f,
            80f, 3f, -168f, -30f, 82f, -112f, 8f, 0.035f,
            70f, 8f, -160f, -20f, 70f, -100f, 6f, 0.03f,
        };

        // ---- The ability's cast ----

        // The Forge Burst: a quick crouch and raise, the slam a tenth of a second after the button (the shockwave is still
        // racing out from the feet as it lands), a held impact with a small rebound, then up again. Columns: thighs,
        // knees, lean, head, both shoulders, both elbows, the weapon wrist, one row for each of CastKeyTimes. A stride
        // under the slam shortens by CastStride so the crouched legs stay under the body.
        public const float CastSeconds = 0.55f;
        private const float OffHandRaise = 0.65f;
        private const float CastStride = 0.4f;
        private const int CastColumns = 7;
        private static readonly float[] CastKeyTimes = { 0f, 0.07f, 0.14f, 0.2f, 0.32f, 0.44f, 0.58f, 1f };
        private static readonly float[] CastKeys =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f,
            10f, 26f, 16f, 6f, -22f, 35f, 0f,
            -2f, 6f, -10f, -10f, 160f, 22f, 25f,
            22f, 48f, 32f, 14f, 38f, 6f, -95f,
            14f, 34f, 22f, 10f, 44f, 10f, -85f,
            20f, 44f, 28f, 12f, 40f, 8f, -92f,
            8f, 16f, 10f, 4f, 16f, 6f, -40f,
            0f, 0f, 0f, 0f, 0f, 0f, 0f,
        };

        // ---- The flinch ----

        // How long a hit's flinch lasts: a little longer than the hit flash (0.14 s), so the body is still recoiling as
        // the flash ends.
        public const float FlinchSeconds = 0.26f;
        // At the hit the torso snaps back, the head further, the arms flare back and the pelvis jolts back; then the
        // body rebounds a little past upright and settles. Legs keep walking.
        public const float FlinchLean = 18f;
        public const float FlinchHead = 12f;
        public const float FlinchArms = 14f;
        public const float FlinchShift = 0.025f;
        // The rebound: how fast the snap decays and how fast it swings back past upright, per unit of flinch time.
        private const float FlinchDecay = 4.5f;
        private const float FlinchSwing = 7.5f;

        // ---- The collapse ----

        // A death plays for this long, then the figure lies still.
        public const float DeathSeconds = 1f;
        // A stagger back from the killing blow, the knees buckling into a kneel, then the figure falling backward about
        // its feet, away from what it faced, under gravity - slow to start, fast at the end - to almost flat, the arms
        // flung up by the landing and settling. Each phase's start and its length, as shares of the collapse.
        private const float StaggerLength = 0.2f;
        private const float BuckleStart = 0.08f;
        private const float BuckleLength = 0.3f;
        private const float ToppleStart = 0.3f;
        private const float ToppleLength = 0.5f;
        private const float LandSettle = 0.2f;
        public const float FallAngle = 84f;
        // The kneeling pose the knees buckle into, then the lying pose the fall ends in, channel by channel: lift (world
        // units), lean, head, cape, hips, knees, shoulders, elbows. Lying, the arms are thrown forward by the fall and the
        // back arches as it lands.
        private static readonly float[] Kneel = { -0.12f, 22f, 25f, 10f, 30f, 75f, 10f, 25f };
        private static readonly float[] Lying = { -0.14f, -8f, -10f, 14f, 15f, 40f, 70f, 35f };

        // The standing rest pose: legs soft, arms hanging slightly forward, nothing breathing.
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

        // How long before its damage event a strike starts drawing back, for a weapon attacking every interval seconds:
        // a strike and the draw before the next one never overlap by more than their eases.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float WindupSeconds(float interval)
        {
            if (!(interval > 0f) || float.IsInfinity(interval))
                return MaxWindupSeconds;
            float share = interval * WindupShareOfInterval;
            return share < MaxWindupSeconds ? share : MaxWindupSeconds;
        }

        // The pose for one frame. The walk and the idle are blended by the walk weight; the follow-through rides on both;
        // a windup and a strike replace the arms and turn the chest; a cast takes the arms and bends the legs; the
        // flinch bends the upper body; the collapse takes over everything.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static PuppetPose Evaluate(PuppetGaitInput input)
        {
            float phase = Clamp01(input.WalkPhase);
            float theta = phase * TwoPi;
            float c = (float)Math.Cos(theta);
            float walk = Ease(Clamp01(input.WalkWeight));
            float still = 1f - walk;
            float idle = float.IsNaN(input.IdleTime) || float.IsInfinity(input.IdleTime) ? 0f : input.IdleTime;
            float breath = still * (float)Math.Sin(idle * BreathRadiansPerSecond);
            float weight = still * (float)Math.Sin(idle * ShiftRadiansPerSecond);
            float surge = Finite(input.Surge);
            float drape = Finite(input.Drape);

            var pose = new PuppetPose();
            // The legs: each plays the keys from its own contact, the left half a cycle after the right.
            float left = phase + 0.5f;
            if (left >= 1f)
                left -= 1f;
            pose.HipRight = walk * Keyed(ThighKeys, phase);
            pose.HipLeft = walk * Keyed(ThighKeys, left);
            pose.KneeRight = KneeRest + walk * (Keyed(KneeKeys, phase) - KneeRest);
            pose.KneeLeft = KneeRest + walk * (Keyed(KneeKeys, left) - KneeRest);
            // Standing, the weight rolls from foot to foot: the pelvis drifts, the legs lean to keep the boots where
            // they are, the unweighted knee softens.
            pose.Shift = WeightShift * weight;
            float plant = WeightShift * weight / (ThighReach + ShinReach) / DegToRad;
            pose.HipRight -= plant;
            pose.HipLeft -= plant;
            pose.KneeRight += WeightKnee * (weight < 0f ? -weight : 0f);
            pose.KneeLeft += WeightKnee * (weight > 0f ? weight : 0f);
            // The lurch of a stop lands in the knees.
            float dip = surge > 0f ? SurgeKnee * surge * still : 0f;
            pose.KneeRight += dip;
            pose.KneeLeft += dip;
            if (input.Casting)
                CastLegs(ref pose, Clamp01(input.CastProgress));
            pose.Lift = Bob(input.Legs, pose.HipRight, pose.KneeRight, pose.HipLeft, pose.KneeLeft);

            // Twice a cycle, a little after each contact: the down.
            float down = (float)Math.Cos(2f * theta - 0.75f);
            pose.Lean = walk * (WalkLean + LeanBob * down) + SurgeLean * surge;
            pose.Head = walk * HeadNod * down - HeadSteady * pose.Lean + BreathHead * breath;
            pose.Breath = BreathLift * breath;
            pose.Twist = -walk * ShoulderTwist * c;
            pose.HipTwist = walk * HipTurn * c + WeightHipTurn * weight;
            pose.Cape = walk * (CapeTrail + CapeFlutter * (float)Math.Sin(2f * theta - CapeLagRadians))
                + BreathCape * breath - DrapeCape * drape;
            pose.Skirt = walk * (SkirtTrail + SkirtFlutter * (float)Math.Sin(2f * theta - SkirtLagRadians))
                - DrapeSkirt * drape;
            // Each arm swings against its own side's leg, a little behind it.
            float arm = walk * (float)Math.Cos(theta - ArmLag * TwoPi);
            pose.ShoulderRight = ShoulderRest - ArmSwing * arm + BreathShoulder * breath + SurgeArms * surge;
            pose.ShoulderLeft = ShoulderRest + ArmSwing * arm + BreathShoulder * breath + SurgeArms * surge;
            pose.ElbowRight = ElbowRest + walk * ElbowSwing * 0.5f + ElbowSwing * 0.5f * -arm;
            pose.ElbowLeft = ElbowRest + walk * ElbowSwing * 0.5f + ElbowSwing * 0.5f * arm;
            pose.WristRight = -WristCarry * (pose.ShoulderRight - ShoulderRest + pose.ElbowRight - ElbowRest);
            pose.WristLeft = -WristCarry * (pose.ShoulderLeft - ShoulderRest + pose.ElbowLeft - ElbowRest);

            if (input.Windup != PuppetStrike.None && input.WindupProgress > 0f)
                Windup(ref pose, input.Windup, Clamp01(input.WindupProgress), input.LeftLeads);
            if (input.Strike != PuppetStrike.None)
                Strike(ref pose, input.Strike, Clamp01(input.StrikeProgress), input.LeftLeads);
            if (input.Casting)
                CastArms(ref pose, Clamp01(input.CastProgress));

            float flinch = Clamp01(input.Flinch);
            if (flinch > 0f)
                Flinch(ref pose, flinch);

            float death = Clamp01(input.DeathProgress);
            if (death > 0f)
                Collapse(ref pose, death);
            return pose;
        }

        // The pelvis's rise above its rest height for these leg angles: of the two boots, the one that reaches lowest
        // below its hip, against where it reached at rest, stands on its floor line - each leg on its own, since seen in
        // three-quarter the far boot stands higher on the screen than the near one.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Bob(PuppetLegs legs, float hipRight, float kneeRight, float hipLeft, float kneeLeft)
        {
            PuppetLeg right = legs.Right.IsSet ? legs.Right : Reference;
            PuppetLeg left = legs.Left.IsSet ? legs.Left : Reference;
            // How far each boot has risen toward its hip since rest: the smaller rise carries the weight. A hard minimum,
            // so the supporting boot never sinks into its floor and the handover to the other is the foot strike.
            float a = Lowest(right, hipRight, kneeRight) - Lowest(right, 0f, KneeRest);
            float b = Lowest(left, hipLeft, kneeLeft) - Lowest(left, 0f, KneeRest);
            return -(a < b ? a : b);
        }

        private static readonly PuppetLeg Reference = new PuppetLeg
        {
            KneeY = -ThighReach, HeelX = -HeelBack, HeelY = -ShinReach, ToeX = ToeAhead, ToeY = -ShinReach
        };

        // The height of the boot's lowest point relative to its hip, the thigh swung by hip degrees and the shin folded
        // by knee degrees from the painted leg (both turns counter-clockwise for a figure facing right).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Lowest(PuppetLeg leg, float hip, float knee)
        {
            float h = hip * DegToRad;
            float s = (hip - knee) * DegToRad;
            float sh = (float)Math.Sin(h), ch = (float)Math.Cos(h);
            float ss = (float)Math.Sin(s), cs = (float)Math.Cos(s);
            float kneeY = leg.KneeX * sh + leg.KneeY * ch;
            float heel = leg.HeelX * ss + leg.HeelY * cs;
            float toe = leg.ToeX * ss + leg.ToeY * cs;
            return kneeY + (heel < toe ? heel : toe);
        }

        // The draw before a strike, over whatever the walk gave the arms: eased out, so the weapon rises quickly and
        // settles at the top, the held breath before the blow.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Windup(ref PuppetPose pose, PuppetStrike strike, float progress, bool leftLeads)
        {
            float[] poses = Poses(strike);
            if (poses == null)
                return;
            float t = 1f - (1f - progress) * (1f - progress);
            Apply(ref pose, poses, 0, 0, 0f, t, strike == PuppetStrike.Thrust && leftLeads);
            if (t > pose.Wield)
                pose.Wield = t;
        }

        // The strike over the walk, the windup or both: the passing pose at the event, the hit a frame later, the
        // follow-through, a hold, then an ease back onto whatever the arms were doing.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Strike(ref PuppetPose pose, PuppetStrike strike, float progress, bool leftLeads)
        {
            float[] poses = Poses(strike);
            if (poses == null)
                return;
            bool mirror = strike == PuppetStrike.Thrust && leftLeads;
            // Shown fully until the hold ends, then handed back to the walk.
            float hold = progress < StrikeHold ? 1f : 1f - Ease((progress - StrikeHold) / (1f - StrikeHold));
            if (progress < HitAt)
                Apply(ref pose, poses, 1, 2, progress / HitAt, hold, mirror);
            else if (progress < FollowAt)
                Apply(ref pose, poses, 2, 3, Ease((progress - HitAt) / (FollowAt - HitAt)), hold, mirror);
            else
                Apply(ref pose, poses, 3, 3, 0f, hold, mirror);
            if (hold > pose.Wield)
                pose.Wield = hold;
        }

        private static float[] Poses(PuppetStrike strike)
        {
            switch (strike)
            {
                case PuppetStrike.Swing:
                    return SwingPoses;
                case PuppetStrike.Cast:
                    return CastPoses;
                case PuppetStrike.Thrust:
                    return ThrustPoses;
                default:
                    return null;
            }
        }

        // Moves the arms and chest by weight toward the strike's pose t of the way from one of its rows to another. A
        // left-led thrust swaps the arms and turns the chest the other way.
        private static void Apply(ref PuppetPose pose, float[] poses, int from, int to, float t, float weight, bool mirror)
        {
            int a = from * StrikeColumns;
            int b = to * StrikeColumns;
            int right = mirror ? 3 : 0;
            int left = mirror ? 0 : 3;
            pose.ShoulderRight += weight * (Mix(poses, a, b, right, t) - pose.ShoulderRight);
            pose.ElbowRight += weight * (Mix(poses, a, b, right + 1, t) - pose.ElbowRight);
            pose.WristRight += weight * (Mix(poses, a, b, right + 2, t) - pose.WristRight);
            pose.ShoulderLeft += weight * (Mix(poses, a, b, left, t) - pose.ShoulderLeft);
            pose.ElbowLeft += weight * (Mix(poses, a, b, left + 1, t) - pose.ElbowLeft);
            pose.WristLeft += weight * (Mix(poses, a, b, left + 2, t) - pose.WristLeft);
            float lean = Mix(poses, a, b, 6, t);
            pose.Lean += weight * lean;
            pose.Head -= weight * HeadSteady * lean;
            float twist = Mix(poses, a, b, 7, t);
            pose.Twist += weight * (mirror ? -twist : twist);
        }

        private static float Mix(float[] poses, int a, int b, int column, float t) =>
            poses[a + column] + t * (poses[b + column] - poses[a + column]);

        // The Forge Burst's pose at this progress, key to key: thighs, knees, lean, head, shoulders, elbows, wrist.
        private static float CastKey(float progress, int column)
        {
            int key = 0;
            while (key < CastKeyTimes.Length - 2 && progress >= CastKeyTimes[key + 1])
                key++;
            float span = CastKeyTimes[key + 1] - CastKeyTimes[key];
            float t = span > 0f ? Ease(Clamp01((progress - CastKeyTimes[key]) / span)) : 1f;
            int a = key * CastColumns + column;
            return CastKeys[a] + t * (CastKeys[a + CastColumns] - CastKeys[a]);
        }

        // How much the cast owns the figure: all of it but its first and last moments, where it blends from and back to
        // whatever the walk and any strike gave.
        private static float CastHold(float progress) =>
            progress < CastKeyTimes[1] ? Ease(progress / CastKeyTimes[1])
            : progress > CastKeyTimes[6] ? 1f - Ease((progress - CastKeyTimes[6]) / (1f - CastKeyTimes[6])) : 1f;

        // The Forge Burst bends the legs into its crouch while they keep a shortened stride.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CastLegs(ref PuppetPose pose, float progress)
        {
            float stride = 1f - CastStride * CastHold(progress);
            float thigh = CastKey(progress, 0);
            float knee = CastKey(progress, 1);
            pose.HipRight = stride * pose.HipRight + thigh;
            pose.HipLeft = stride * pose.HipLeft + thigh;
            pose.KneeRight += knee;
            pose.KneeLeft += knee;
        }

        // The arms are the cast's alone while it holds; the chest pitches with the slam.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void CastArms(ref PuppetPose pose, float progress)
        {
            float lean = CastKey(progress, 2);
            float own = CastHold(progress);
            float shoulder = CastKey(progress, 4);
            float elbow = CastKey(progress, 5);
            float wrist = CastKey(progress, 6);
            pose.Lean += lean;
            pose.Head += CastKey(progress, 3) - HeadSteady * lean;
            pose.ShoulderRight += own * (shoulder - pose.ShoulderRight);
            // The off hand follows the raise only part of the way, so a shield stays beside the helmet, not over it.
            pose.ShoulderLeft += own * (OffHandRaise * shoulder - pose.ShoulderLeft);
            pose.ElbowRight += own * (elbow - pose.ElbowRight);
            pose.ElbowLeft += own * (elbow - pose.ElbowLeft);
            pose.WristRight += own * (wrist - pose.WristRight);
            pose.WristLeft -= own * pose.WristLeft;
            pose.Twist -= own * pose.Twist;
            if (own > pose.Wield)
                pose.Wield = own;
        }

        // The hit's recoil over the upper body: a snap back that decays as it swings once a little past upright.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Flinch(ref PuppetPose pose, float flinch)
        {
            float time = 1f - flinch;
            float recoil = flinch * (float)Math.Exp(-FlinchDecay * time) * (float)Math.Cos(FlinchSwing * time);
            pose.Lean -= FlinchLean * recoil;
            pose.Head -= FlinchHead * recoil;
            pose.ShoulderRight -= FlinchArms * recoil;
            pose.ShoulderLeft -= FlinchArms * recoil;
            pose.ElbowRight += 0.5f * FlinchArms * recoil;
            pose.ElbowLeft += 0.5f * FlinchArms * recoil;
            pose.Shift -= FlinchShift * recoil;
        }

        // The collapse over the living pose: a stagger, the knees buckling into a kneel, then the fall into the lying
        // pose, accelerating as it goes, and the arms and head settling once it lands.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Collapse(ref PuppetPose pose, float death)
        {
            // The killing blow's recoil, over before the knees have gone far.
            float stagger = death < StaggerLength ? (float)Math.Sin(death / StaggerLength * 3.14159274f) : 0f;
            float buckle = Ease(Clamp01((death - BuckleStart) / BuckleLength));
            float drop = Clamp01((death - ToppleStart) / ToppleLength);
            // Gravity: the fall gathers speed, so its angle grows with the square of its time.
            float topple = drop * drop;
            float landed = Clamp01((death - ToppleStart - ToppleLength) / LandSettle);
            // The landing throws the arms and head past the lying pose; they settle back over the last of the collapse.
            float bounce = landed > 0f ? (1f - landed) * (float)Math.Sin(landed * 3.14159274f * 2f) : 0f;
            pose.Fall = topple * FallAngle;
            pose.Lean -= 10f * stagger * (1f - buckle);
            pose.Lift += buckle * (Kneel[0] + topple * (Lying[0] - Kneel[0]) - pose.Lift);
            pose.Lean += buckle * (Kneel[1] + topple * (Lying[1] - Kneel[1]) - pose.Lean);
            pose.Head += buckle * (Kneel[2] + topple * (Lying[2] - Kneel[2]) - pose.Head) - 8f * bounce;
            pose.Cape += buckle * (Kneel[3] + topple * (Lying[3] - Kneel[3]) - pose.Cape);
            pose.Skirt -= buckle * pose.Skirt;
            pose.Breath -= buckle * pose.Breath;
            pose.Shift -= buckle * pose.Shift;
            pose.Twist -= buckle * pose.Twist;
            pose.HipTwist -= buckle * pose.HipTwist;
            float hip = Kneel[4] + topple * (Lying[4] - Kneel[4]);
            float knee = Kneel[5] + topple * (Lying[5] - Kneel[5]);
            float shoulder = Kneel[6] + topple * (Lying[6] - Kneel[6]) + 18f * bounce;
            float elbow = Kneel[7] + topple * (Lying[7] - Kneel[7]);
            pose.HipLeft += buckle * (hip - pose.HipLeft);
            pose.HipRight += buckle * (hip - pose.HipRight);
            pose.KneeLeft += buckle * (knee - pose.KneeLeft);
            pose.KneeRight += buckle * (knee - pose.KneeRight);
            pose.ShoulderLeft += buckle * (shoulder - pose.ShoulderLeft) - 12f * stagger * (1f - buckle);
            pose.ShoulderRight += buckle * (shoulder - pose.ShoulderRight) - 12f * stagger * (1f - buckle);
            pose.ElbowLeft += buckle * (elbow - pose.ElbowLeft);
            pose.ElbowRight += buckle * (elbow - pose.ElbowRight);
            pose.WristLeft -= buckle * pose.WristLeft;
            pose.WristRight -= buckle * pose.WristRight;
        }

        // A periodic Catmull-Rom curve through StrideKeys evenly spaced keys: smooth through every key, so a stride has no
        // corners.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Keyed(float[] keys, float phase)
        {
            float u = phase * StrideKeys;
            int i = (int)u;
            float f = u - i;
            float p0 = keys[(i + StrideKeys - 1) % StrideKeys];
            float p1 = keys[i % StrideKeys];
            float p2 = keys[(i + 1) % StrideKeys];
            float p3 = keys[(i + 2) % StrideKeys];
            return 0.5f * (2f * p1 + (p2 - p0) * f + (2f * p0 - 5f * p1 + 4f * p2 - p3) * f * f
                + (3f * p1 - p0 - 3f * p2 + p3) * f * f * f);
        }

        // Smoothstep: eases a 0..1 ramp in and out.
        private static float Ease(float t) => t * t * (3f - 2f * t);

        private static float Clamp01(float value) => value > 0f ? (value < 1f ? value : 1f) : 0f;

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }
}
