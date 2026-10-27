using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // One leg as it was painted: the hip from the pelvis's joint, the knee from the hip, and the heel and toe of the
    // boot's sole from the knee, in world units for the figure facing right. PuppetRig reads it off the parts; all zero
    // is a reference leg.
    public struct PuppetLeg
    {
        public float HipX;
        public float HipY;
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
        // Which way the travel runs on the floor, in radians in the figure's own terms: 0 straight across the screen the
        // way the figure faces, positive turning up the screen (away from the camera), negative down it; past a quarter
        // turn either way the figure walks backward. The floor's depth shows at FloorDepth on the screen, so a stride off
        // the screen's axis is foreshortened: the legs swing across the screen by its cosine and step up or down it for
        // the rest. PuppetMotion eases it.
        public float Heading;
        // Seconds of the idle, wrapped into one IdleCycle so the sines never lose precision.
        public float IdleTime;
        // The strike playing: 0 at the attack's damage event, 1 once the arms are back on the walk.
        public PuppetStrike Strike;
        public float StrikeProgress;
        // Which hand jabs a Thrust: the daggers alternate.
        public bool LeftLeads;
        // Whether the front three-quarter is on show rather than the rear: the sword's and the staff's arcs differ there.
        public bool Front;
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
    // every character shares it. A distance-driven run with weight - each boot planted on the floor while the floor
    // carries it back, the hips dropping as it takes the weight and lifting through the flight, the shoulders and arms
    // swinging against the legs, the cape and skirt fluttering behind - an idle that breathes and shifts its weight,
    // strikes drawn back before the hit and followed through after it on the arms and chest only, the ability's slam, a
    // flinch that snaps back and rebounds, and a collapse that buckles and then falls.
    //
    // Only float multiplies, adds and Math.Sin / Cos / Sqrt / Exp / Asin / Atan2, each formula in one body that is never
    // inlined: this project has already paid for two copies of float arithmetic disagreeing in the last bit (docs/21).
    // The puppet is presentation and touches no combat state, but a pose that is a pure function of its input keeps
    // captures and tests repeatable.
    //
    // Angles are degrees in PuppetPose's terms (positive forward, flexion positive), offsets from the painted pose, for a
    // figure facing screen-right. Every amount here was tuned on the Vanguard at the game's framing, where it stands about
    // 165 phone pixels tall (1.375 world units at 120 pixels a unit).
    public static class PuppetGait
    {
        private const float Pi = 3.14159274f;
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
        // ArenaFloor.DepthScale: a floor unit of depth shows as half a unit up the screen.
        public const float FloorDepth = 0.5f;

        // A run with weight. Each boot lands on its heel ahead of the hip, takes the weight on a bending knee (the down),
        // rolls onto its toe and pushes off behind, held on the floor the whole time while the floor carries it back:
        // the stance, StanceShare of the cycle, the right boot's from 0 and the left's from a half. Then it swings
        // forward, both boots off the floor between one push-off and the next strike (the flight). The stride's floor
        // travel is set by StrideLength, so a boot down for a whole step would have to reach most of a leg's length
        // either side of the hip; the flight is what lets the short armoured legs keep pace with the floor instead of
        // skating over it.
        public const float StanceShare = 0.3f;
        // That share is for a leg as long on the screen as the Vanguard's near leg (world units, hip to floor line); a
        // leg drawn shorter takes a shorter stance (Share), within these bounds.
        private const float StanceLeg = 0.54f;
        private const float MinStanceShare = 0.12f;
        private const float MaxStanceShare = 0.4f;
        // How much shorter the stance is for a stride straight up or down the screen, and how far such a stance moves
        // toward the far side of the rest place (Stance).
        private const float EndOnStance = 0.37f;
        private const float DepthBias = 0.3f;
        // The heel lands this much further ahead of its rest place than the toe pushes off behind its own (floor
        // units): a runner reaches out a little more than it pushes back.
        private const float ReachAhead = 0.06f;
        // The run's bob: the hips this far under their rest height at mid-stance, as the planted leg takes the weight,
        // and FlightHop over it at mid-flight (world units). Four texels in all: weight, not a hop.
        public const float BobDrop = 0.025f;
        // The stance knee: nearly straight at the strike, giving most at mid-stance, firming for the push-off (degrees).
        private const float KneeStrike = 6f;
        private const float KneeGive = 24f;
        private const float KneePush = 12f;
        // The swing knee folds this far over the ease from the push-off's knee to the strike's: no further than about
        // forty degrees in all, since a cut-out shin turns in the screen's plane and a deeper fold reads, at this
        // three-quarter view, as a kick back rather than a lifted foot.
        public const float SwingFold = 30f;
        // Off the screen's axis the legs turn by their share across the screen, but the swing knee keeps at least this
        // share of its fold, so a boot walking straight up or down the screen still tips as it lifts; and the leg lifts
        // this much more there, where turning gives it no clearance (world units).
        private const float FoldFloor = 0.35f;
        private const float StepLift = 0.015f;
        // Every swing lifts its leg this much at mid-swing besides (world units): the painted boots' heels stand behind
        // their shins, so swinging a leg forward first swings its heel down, and the hips ride lowest while the other
        // boot takes the weight.
        private const float SwingLift = 0.04f;
        // The hips' rise over their rest height at mid-flight (world units): a texel, a lift rather than a hop.
        public const float FlightHop = 0.008f;
        // How far a walk pulls the far boot in toward the near one from the painted stance. The painting stands wide, and
        // seen in three-quarter that puts the far boot sixteen texels up the screen: on the move the figure looked lifted
        // off its shadow whenever the far boot alone was down, and the near leg's back-swing crossed toward it.
        public const float TrackNarrowing = 0.6f;
        // Across the screen it moves less, since the far thigh's painted top hides behind the cape seen from behind and
        // behind the skirt's side from the front, and further in it would come out from under them.
        public const float TrackNarrowingAcross = 0.3f;
        // A standing knee is never locked straight; a soft knee reads as weight.
        public const float KneeRest = 3f;
        // Without painted legs (PuppetGaitInput.Legs), a reference leg about the Vanguard's (world units): the knee below
        // the hip, and the heel behind and the toe ahead under it.
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
        // The Swing seen from the front (PuppetGaitInput.Front). The near arm hangs behind the body's centre there, so the
        // rear's draw - the arm swung forward and up - carried the blade up across the helmet: this one draws it back and
        // up behind the near shoulder instead, the blade cocked up and back, and the blow crosses the chest below the
        // helmet, level at the damage event, down through the target, down and across after. The shield arm braces at
        // the side, lifting a little rather than swinging out from the body, which the far arm cannot pass in front of.
        private static readonly float[] FrontSwingPoses =
        {
            -150f, 30f, -156f, 8f, 25f, 0f, -6f, -0.03f,
            100f, 10f, -168f, 8f, 25f, 0f, 2f, 0f,
            50f, 4f, -149f, 6f, 28f, 0f, 10f, 0.035f,
            25f, 14f, -160f, 4f, 25f, 0f, 9f, 0.04f,
        };
        // The staff's draw tips its head back only a little past where the blow starts: drawn further, it turned the
        // staff head-down at the hip and flipped it upright on the damage event's frame.
        private static readonly float[] CastPoses =
        {
            45f, 45f, -20f, 15f, 30f, 0f, -5f, -0.015f,
            62f, 25f, -15f, 35f, 20f, 10f, 2f, 0.01f,
            78f, 4f, -48f, 55f, 8f, 12f, 7f, 0.025f,
            68f, 10f, -38f, 45f, 12f, 8f, 5f, 0.02f,
        };
        // The Cast seen from the front: the rear's thrust brought the near hand across to the chest, and the upright staff
        // stood in front of the helmet. Here the hand stays low at the near side and the staff's head tips back from the
        // helmet, then is pushed up toward the target and held, the free palm following behind the body.
        private static readonly float[] FrontCastPoses =
        {
            -15f, 45f, 5f, 15f, 30f, 0f, -5f, -0.015f,
            -5f, 40f, -8f, 25f, 20f, 10f, 2f, 0.01f,
            5f, 35f, -21f, 40f, 8f, 12f, 7f, 0.025f,
            2f, 38f, -19f, 32f, 12f, 8f, 5f, 0.02f,
        };
        private static readonly float[] ThrustPoses =
        {
            -28f, 82f, -118f, -24f, 78f, -112f, -4f, -0.01f,
            40f, 40f, -140f, -26f, 80f, -112f, 2f, 0.02f,
            80f, 3f, -168f, -30f, 82f, -112f, 8f, 0.035f,
            70f, 8f, -160f, -20f, 70f, -100f, 6f, 0.03f,
        };

        // ---- The ability's cast ----

        // The Forge Burst. Its shockwave races out from the feet on the button's own frame, so the slam has no wind-up to
        // play: the body drops straight into the impact - crouched, leaning in, the weapon driven point-down into the
        // floor - within a frame of the press (ImpactAt), sinks a little further with the recoil, holds, and only then
        // rises, drawing the weapon up out of the floor and in front as it straightens - never back over the shoulder,
        // where it would read as the next blow's wind-up - and settles onto the walk. Columns: thighs, knees, lean, head,
        // both shoulders, both elbows, the weapon wrist, one row for each of CastKeyTimes. A stride under the slam bends
        // its knees a little, so its planted boot holds the floor while the hips sink (CastStrideKnee).
        public const float CastSeconds = 0.55f;
        // The impact's share of the cast: 0.022 seconds. PuppetMotion.Cast runs in the button's Update and the frame's
        // own tick follows, so the impact is mostly on screen on the press's frame and whole on the next.
        public const float ImpactAt = 0.04f;
        private const float OffHandRaise = 0.65f;
        // A stride takes only this share of the crouch's knee: the hips sink by the standing crouch's depth and the
        // planted leg steps up under the skirt to meet them, since the Vanguard's thigh is too short for a bent knee alone
        // to shorten the leg without throwing the thigh out level.
        private const float CastStrideKnee = 0.25f;
        // How much of the slam's lean the cape takes back as it hangs.
        private const float CapeHang = 0.35f;
        private const int CastColumns = 7;
        private static readonly float[] CastKeyTimes = { 0f, ImpactAt, 0.11f, 0.22f, 0.42f, 0.68f, 1f };
        private static readonly float[] CastKeys =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f,
            22f, 48f, 32f, 14f, 40f, 6f, -95f,
            26f, 56f, 36f, 16f, 34f, 4f, -100f,
            20f, 44f, 28f, 12f, 42f, 8f, -90f,
            6f, 14f, 6f, 2f, 38f, 18f, -48f,
            2f, 6f, 2f, 0f, 12f, 10f, -16f,
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
            pose.HipTwist = walk * HipTurn * c + WeightHipTurn * weight;
            // Standing, the weight rolls from foot to foot: the pelvis drifts, the legs lean to keep the boots where
            // they are, the unweighted knee softens.
            pose.Shift = WeightShift * weight;
            float plant = WeightShift * weight / (ThighReach + ShinReach) / DegToRad;
            pose.HipRight = -plant;
            pose.HipLeft = -plant;
            pose.KneeRight = KneeRest + WeightKnee * (weight < 0f ? -weight : 0f);
            pose.KneeLeft = KneeRest + WeightKnee * (weight > 0f ? weight : 0f);
            // The lurch of a stop lands in the knees.
            float dip = surge > 0f ? SurgeKnee * surge * still : 0f;
            pose.KneeRight += dip;
            pose.KneeLeft += dip;
            float castKnee = 0f;
            if (input.Casting)
                castKnee = CastLegs(ref pose, Clamp01(input.CastProgress));
            pose.Lift = Bob(input.Legs, pose.HipRight, pose.KneeRight, pose.HipLeft, pose.KneeLeft);
            pose.RightPlanted = pose.LeftPlanted = true;
            // Walking, the stride takes the legs over, eased in from the standing pose.
            if (walk > 0f)
                Walk(ref pose, input.Legs, phase, Finite(input.Heading), CastStrideKnee * castKnee, walk);

            // Twice a cycle, at each mid-stance: the down.
            float down = (float)Math.Cos(2f * theta - DownRadians);
            pose.Lean = walk * (WalkLean + LeanBob * down) + SurgeLean * surge;
            pose.Head = walk * HeadNod * down - HeadSteady * pose.Lean + BreathHead * breath;
            pose.Breath = BreathLift * breath;
            pose.Twist = -walk * ShoulderTwist * c;
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
                Windup(ref pose, input.Windup, Clamp01(input.WindupProgress), input.LeftLeads, input.Front);
            if (input.Strike != PuppetStrike.None)
                Strike(ref pose, input.Strike, Clamp01(input.StrikeProgress), input.LeftLeads, input.Front);
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
        private static void Windup(ref PuppetPose pose, PuppetStrike strike, float progress, bool leftLeads, bool front)
        {
            float[] poses = Poses(strike, front);
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
        private static void Strike(ref PuppetPose pose, PuppetStrike strike, float progress, bool leftLeads, bool front)
        {
            float[] poses = Poses(strike, front);
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

        private static float[] Poses(PuppetStrike strike, bool front)
        {
            switch (strike)
            {
                case PuppetStrike.Swing:
                    return front ? FrontSwingPoses : SwingPoses;
                case PuppetStrike.Cast:
                    return front ? FrontCastPoses : CastPoses;
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

        // How much the cast owns the figure: all of it but its first moment, the drop into the impact, and its last key,
        // where it blends back to whatever the walk and any strike gave.
        private static float CastHold(float progress)
        {
            float settle = CastKeyTimes[CastKeyTimes.Length - 2];
            return progress < CastKeyTimes[1] ? Ease(progress / CastKeyTimes[1])
                : progress > settle ? 1f - Ease((progress - settle) / (1f - settle)) : 1f;
        }

        // The Forge Burst bends the standing legs into its crouch. A stride takes a share of the knees' flexion instead
        // (returned; CastStrideKnee), so its planted boot keeps holding the floor while the hips sink.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float CastLegs(ref PuppetPose pose, float progress)
        {
            float thigh = CastKey(progress, 0);
            float knee = CastKey(progress, 1);
            pose.HipRight += thigh;
            pose.HipLeft += thigh;
            pose.KneeRight += knee;
            pose.KneeLeft += knee;
            return knee;
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
            // The cape hangs from the shoulders, so it tips less than the chest: tipped all the way, its hem swung up and
            // back off the far leg it covers from behind.
            pose.Cape -= CapeHang * lean;
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
            // The stride's steps let go as the knees buckle: the kneel and the fall are the painted legs'.
            pose.LegRiseRight -= buckle * pose.LegRiseRight;
            pose.LegRiseLeft -= buckle * pose.LegRiseLeft;
            pose.LegShiftRight -= buckle * pose.LegShiftRight;
            pose.LegShiftLeft -= buckle * pose.LegShiftLeft;
            pose.FloorRight -= buckle * pose.FloorRight;
            pose.FloorLeft -= buckle * pose.FloorLeft;
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

        // ---- The stride's legs ----

        // One leg's share of a stride: its thigh's and knee's screen angles (degrees), how far the whole leg steps up the
        // screen (world units) and how much of that is the boot's depth rather than the planted leg reaching its floor
        // under the run's bob, and whether its boot is planted.
        private struct Stride
        {
            public float Hip;
            public float Knee;
            public float Step;
            public float Depth;
            public bool Planted;
        }

        // A thousandth of a cycle: the step over which a stance's pace is read at its ends.
        private const float Pace = 0.001f;
        // The share of the push-off's sweep the swinging thigh keeps: it slows at once, so it never kicks far back.
        private const float PushOffPace = 0.4f;
        // Each mid-stance, twice a cycle, in radians of the doubled stride angle.
        private const float DownRadians = TwoPi * StanceShare;

        // The stride over the standing legs, by the walk's weight: each leg's thigh and knee as the screen shows them at
        // this heading and its step up or down the screen, the far leg pulled in under the body, and the run's bob on
        // whatever the standing legs gave the hips (a cast's crouch).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Walk(ref PuppetPose pose, PuppetLegs legs, float phase, float heading, float castKnee, float walk)
        {
            PuppetLeg right = legs.Right.IsSet ? legs.Right : Reference;
            PuppetLeg left = legs.Left.IsSet ? legs.Left : Reference;
            float depth = FloorDepth * (float)Math.Sin(heading);
            var run = new Run
            {
                Across = (float)Math.Cos(heading),
                Depth = depth,
                RightShare = Share(right, depth),
                LeftShare = Share(left, depth),
                Crouch = pose.Lift,
                CastKnee = castKnee
            };
            float leftPhase = phase + 0.5f;
            if (leftPhase >= 1f)
                leftPhase -= 1f;
            Stride r = Leg(right, phase, 0f, run.RightShare, run);
            Stride l = Leg(left, leftPhase, 0.5f, run.LeftShare, run);
            Narrowing(right, left, out float narrowX, out float narrowY);
            pose.HipRight += walk * (r.Hip - pose.HipRight);
            pose.HipLeft += walk * (l.Hip - pose.HipLeft);
            pose.KneeRight += walk * (r.Knee - pose.KneeRight);
            pose.KneeLeft += walk * (l.Knee - pose.KneeLeft);
            pose.Lift += walk * Bob(phase, run);
            pose.LegRiseRight = walk * r.Step;
            pose.LegRiseLeft = walk * (l.Step + narrowY);
            pose.LegShiftLeft = walk * narrowX;
            pose.FloorRight = walk * r.Depth;
            pose.FloorLeft = walk * (l.Depth + narrowY);
            if (walk >= 0.5f)
            {
                pose.RightPlanted = r.Planted;
                pose.LeftPlanted = l.Planted;
            }
        }

        // What every leg of one stride shares: the heading's share across the screen and up it, each leg's stance share,
        // and what the standing legs already did to the hips and knees (a cast's crouch).
        private struct Run
        {
            public float Across;
            public float Depth;
            public float RightShare;
            public float LeftShare;
            public float Crouch;
            public float CastKnee;
        }

        // How much of the cycle a leg's boot spends planted: StanceShare for a leg as long on the screen as the Vanguard's
        // near leg, a shorter stance for a shorter leg, so both swing through the same angle - seen in three-quarter the
        // far leg is painted reaching back in depth and stands a fifth shorter on the screen, and with the near leg's
        // stance it had to swing half as far again to keep pace with the same floor. A stride running up or down the
        // screen takes shorter stances still (EndOnStance): its planted boot has to travel with the floor along the
        // leg, which a cut-out can only show by stepping the whole leg out from under the skirt.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Share(PuppetLeg leg, float depth)
        {
            float endOn = depth / FloorDepth;
            endOn *= endOn;
            float share = StanceShare * -RestLowest(leg) / StanceLeg * (1f - EndOnStance * endOn);
            return share < MinStanceShare ? MinStanceShare : share > MaxStanceShare ? MaxStanceShare : share;
        }

        // The run's bob on the hips, from the step's own stance: lowest at mid-stance as the planted leg takes the weight,
        // highest at mid-flight. Each half of a step - the stance and the flight - spans half a turn of the curve, so a
        // leg with a shorter stance still lands on the same height.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Bob(float phase, Run run)
        {
            float step = phase < 0.5f ? phase : phase - 0.5f;
            float share = phase < 0.5f ? run.RightShare : run.LeftShare;
            float turn = step < share ? 0.5f * step / share : 0.5f + 0.5f * (step - share) / (0.5f - share);
            float low = 0.5f + 0.5f * (float)Math.Cos(TwoPi * (turn - 0.25f));
            return FlightHop - (FlightHop + BobDrop) * low;
        }

        // One leg at its own point q of the cycle, 0 at its heel strike (offset is where that falls in the right leg's
        // cycle): planted through its stance, then swinging to the next strike - its thigh, knee and step each eased
        // from the push-off to the strike, both ends' pace kept so neither shows a corner, the knee folding on top and,
        // off the screen's axis, the leg lifting where turning gives it no clearance.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Stride Leg(PuppetLeg leg, float q, float offset, float share, Run run)
        {
            if (q < share)
                return Stance(leg, q, offset, share, run);
            Stride off = Stance(leg, share, offset, share, run);
            Stride beforeOff = Stance(leg, share - Pace, offset, share, run);
            Stride land = Stance(leg, 0f, offset + 1f, share, run);
            Stride afterLand = Stance(leg, Pace, offset + 1f, share, run);
            float span = 1f - share;
            float u = (q - share) / span;
            float scale = span / Pace;
            float bump = (float)Math.Sin(u * Pi);
            bump *= bump;
            float across = run.Across < 0f ? -run.Across : run.Across;
            // The thigh leaves the push-off slowing its sweep back and reaches the strike already sweeping back with the
            // floor, so the boot lands at the floor's pace. The leg's step leaves and meets its stances at rest: its
            // depth eases from behind to ahead, and the stretch a planted leg needed to reach the floor under the bob
            // lets go in mid-swing, where the folded leg hangs at its own length.
            float depth = Hermite(off.Depth, land.Depth, 0f, 0f, u);
            float reach = Hermite(off.Step - off.Depth, land.Step - land.Depth, 0f, 0f, u);
            return new Stride
            {
                Hip = Hermite(off.Hip, land.Hip, PushOffPace * (off.Hip - beforeOff.Hip) * scale,
                    (afterLand.Hip - land.Hip) * scale, u),
                Knee = Hermite(off.Knee, land.Knee, (off.Knee - beforeOff.Knee) * scale,
                        (afterLand.Knee - land.Knee) * scale, u)
                    + SwingFold * (across > FoldFloor ? across : FoldFloor) * bump,
                Step = depth + reach * (1f - bump) + (SwingLift + (1f - across) * StepLift) * bump,
                Depth = depth
            };
        }

        // One boot's stance at q, 0 its heel strike, share its push-off. The knee gives as designed and the thigh turns
        // so the boot's pivot - its heel, then its toe once the sole has rolled flat - is where the floor has carried it,
        // as the screen shows it at this heading: its reach across the screen foreshortened, the knee with it, and the
        // whole leg stepped up or down the screen so the pivot stands on the floor at its depth, under hips that ride
        // the run's bob rather than the leg's reach (the leg's step takes up the difference, a texel or two). The hips'
        // turn moves the hip, so the boot is held against it too.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Stride Stance(PuppetLeg leg, float q, float offset, float share, Run run)
        {
            float v = q / share;
            // The give peaks at mid-stance and eases at both ends, so the swings either side need no sudden knee.
            float give = (float)Math.Sin(v * Pi);
            float knee = KneeStrike + (KneePush - KneeStrike) * v + KneeGive * give * give + run.CastKnee;
            float sole = SoleLength(leg);
            // Centred on where the boot stands at rest, so each painted leg swings about its own painted angle: the heel
            // lands land ahead of its rest place, and the floor carries it, then the toe, back.
            float restHeel = Ahead(leg, leg.HeelX, leg.HeelY, 0f, KneeRest);
            float restToe = Ahead(leg, leg.ToeX, leg.ToeY, 0f, KneeRest);
            float land = (StrideLength * share + restToe - restHeel - sole + ReachAhead) * 0.5f;
            // Up or down the screen the stance moves toward the far side of the rest place - ahead walking away from the
            // camera, behind walking toward it - so the planted boot steps its leg up under the skirt more than it draws
            // it out below: up reads as a leg reaching away, and far enough down the thigh's hidden top would show.
            land += DepthBias * 0.5f * StrideLength * share * run.Depth / FloorDepth;
            float carried = land - StrideLength * q;
            float twist = HipTurn * (float)Math.Cos(q * TwoPi);
            float across = run.Across < 0f ? -run.Across : run.Across;
            float screenKnee = across * knee;
            float hip = Pivot(leg, screenKnee, restHeel + run.Across * carried - twist, sole, out float pivot);
            float phase = q + offset;
            float hips = Bob(phase - (float)Math.Floor(phase), run) + run.Crouch;
            return new Stride
            {
                Hip = hip,
                Knee = screenKnee,
                Step = RestLowest(leg) + run.Depth * carried - hips - pivot,
                Depth = run.Depth * carried,
                Planted = true
            };
        }

        // Where a boot's sole ends - heel, then toe - are for this pose, from where the painting put its hip (world units,
        // the figure facing right): the pelvis's shift and bob, the leg's own step and the hips' turn, then the thigh and
        // the knee, as PuppetRig hangs them. So a test can follow a planted boot from frame to frame.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Sole(in PuppetPose pose, PuppetLeg leg, bool right, out float heelX, out float heelY,
            out float toeX, out float toeY)
        {
            PuppetLeg l = leg.IsSet ? leg : Reference;
            PuppetBone thigh = right ? PuppetBone.ThighRight : PuppetBone.ThighLeft;
            float hipX = pose.Shift + pose.Forward(thigh);
            float hipY = pose.Lift + pose.Rise(thigh);
            float hip = right ? pose.HipRight : pose.HipLeft;
            float knee = right ? pose.KneeRight : pose.KneeLeft;
            heelX = hipX + Ahead(l, l.HeelX, l.HeelY, hip, knee);
            heelY = hipY + Height(l, l.HeelX, l.HeelY, hip, knee);
            toeX = hipX + Ahead(l, l.ToeX, l.ToeY, hip, knee);
            toeY = hipY + Height(l, l.ToeX, l.ToeY, hip, knee);
        }

        // The height of the boot's lowest point under its painted hip when it stands as painted: its floor line.
        public static float FloorLine(PuppetLeg leg) => RestLowest(leg.IsSet ? leg : Reference);

        // The thigh angle that stands the boot on its pivot at reach ahead of the hip, the knee folded by knee degrees:
        // the heel while it is the lower end of the sole, else the toe, a sole's length further on since the toe came
        // down where the flat sole put it. The pivot's height under the hip comes out too.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Pivot(PuppetLeg leg, float knee, float reach, float sole, out float height)
        {
            float hip = Reach(leg, leg.HeelX, leg.HeelY, knee, reach);
            float heel = Height(leg, leg.HeelX, leg.HeelY, hip, knee);
            float toe = Height(leg, leg.ToeX, leg.ToeY, hip, knee);
            if (toe >= heel)
            {
                height = heel;
                return hip;
            }
            hip = Reach(leg, leg.ToeX, leg.ToeY, knee, reach + sole);
            height = Height(leg, leg.ToeX, leg.ToeY, hip, knee);
            return hip;
        }

        // The thigh angle (degrees) that puts the boot's point (x, y from the knee) at reach ahead of the hip with the
        // knee folded by knee degrees. Turned h, the point is A cos h + B sin h ahead, which is R sin(h + a): so
        // h = asin(reach / R) - a, on the branch where the leg hangs.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Reach(PuppetLeg leg, float x, float y, float knee, float reach)
        {
            float k = knee * DegToRad;
            float ck = (float)Math.Cos(k), sk = (float)Math.Sin(k);
            float a = leg.KneeX + x * ck + y * sk;
            float b = -leg.KneeY + x * sk - y * ck;
            float s = reach / (float)Math.Sqrt(a * a + b * b);
            s = s > 1f ? 1f : s < -1f ? -1f : s;
            return ((float)Math.Asin(s) - (float)Math.Atan2(a, b)) / DegToRad;
        }

        // The height of the boot's point (x, y from the knee) relative to the hip, the thigh turned hip degrees and the
        // knee folded knee degrees (both turns counter-clockwise for a figure facing right).
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Height(PuppetLeg leg, float x, float y, float hip, float knee)
        {
            float h = hip * DegToRad;
            float s = (hip - knee) * DegToRad;
            return leg.KneeX * (float)Math.Sin(h) + leg.KneeY * (float)Math.Cos(h) + x * (float)Math.Sin(s)
                + y * (float)Math.Cos(s);
        }

        // How far ahead of the hip the boot's point is, turned as for Height.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static float Ahead(PuppetLeg leg, float x, float y, float hip, float knee)
        {
            float h = hip * DegToRad;
            float s = (hip - knee) * DegToRad;
            return leg.KneeX * (float)Math.Cos(h) - leg.KneeY * (float)Math.Sin(h) + x * (float)Math.Cos(s)
                - y * (float)Math.Sin(s);
        }

        // The heel-to-toe length of the boot's sole: how far its pivot moves on as it rolls flat.
        private static float SoleLength(PuppetLeg leg)
        {
            float x = leg.ToeX - leg.HeelX, y = leg.ToeY - leg.HeelY;
            return (float)Math.Sqrt(x * x + y * y);
        }

        // The height of the boot's lowest point under its hip as it stands at rest: its floor line.
        private static float RestLowest(PuppetLeg leg) => Lowest(leg, 0f, KneeRest);

        // How far a walk moves the far (left) leg toward the near one: the given share of the way from where its boot
        // stands at rest to where the near boot does.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void Narrowing(PuppetLeg near, PuppetLeg far, out float x, out float y)
        {
            RestContact(near, out float nearX, out float nearY);
            RestContact(far, out float farX, out float farY);
            float across = nearX - farX;
            x = across < 0f ? TrackNarrowingAcross * across : 0f;
            y = TrackNarrowing * (nearY - farY);
        }

        // Where the boot stands at rest, from the pelvis's joint: across the screen and on its floor line.
        private static void RestContact(PuppetLeg leg, out float x, out float y)
        {
            float heel = Height(leg, leg.HeelX, leg.HeelY, 0f, KneeRest);
            float toe = Height(leg, leg.ToeX, leg.ToeY, 0f, KneeRest);
            // Across the screen the boot stands at its sole's middle: a boot painted toe-down would otherwise seem to
            // stand at its toe.
            float heelAhead = Ahead(leg, leg.HeelX, leg.HeelY, 0f, KneeRest);
            float toeAhead = Ahead(leg, leg.ToeX, leg.ToeY, 0f, KneeRest);
            x = leg.HipX + 0.5f * (heelAhead + toeAhead);
            y = leg.HipY + (heel <= toe ? heel : toe);
        }

        // A cubic from a to b over t in [0, 1], leaving a at slope da and reaching b at slope db.
        private static float Hermite(float a, float b, float da, float db, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return (2f * t3 - 3f * t2 + 1f) * a + (t3 - 2f * t2 + t) * da + (3f * t2 - 2f * t3) * b + (t3 - t2) * db;
        }

        // Smoothstep: eases a 0..1 ramp in and out.
        private static float Ease(float t) => t * t * (3f - 2f * t);

        private static float Clamp01(float value) => value > 0f ? (value < 1f ? value : 1f) : 0f;

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }
}
