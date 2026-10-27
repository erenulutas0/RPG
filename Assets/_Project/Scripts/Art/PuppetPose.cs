using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // Which channel of a PuppetPose turns a cut-out part (docs/45 section 4 B). A painted parts sheet names each piece's
    // bone once; the same pose then drives every character, however its parts are cut. Fixed parts (a belt, a pauldron)
    // only ride on their parent.
    public enum PuppetBone
    {
        Fixed,
        // Carries the walk's bob; the legs, torso and skirt hang from it.
        Pelvis,
        Torso,
        Head,
        Cape,
        Skirt,
        ThighLeft,
        ShinLeft,
        ThighRight,
        ShinRight,
        UpperArmLeft,
        ForearmLeft,
        HandLeft,
        UpperArmRight,
        ForearmRight,
        HandRight
    }

    // The arm motion an attack plays over the walk. Only the arms and the chest take it: the legs keep their stride (the
    // owner, 2026-10-26: a hero that froze its legs for every attack glided across the floor).
    public enum PuppetStrike
    {
        None,
        // The Sword: drawn back over the shoulder before the hit, then chopped forward and down through it.
        Swing,
        // The Staff: lifted, then its head thrust toward the target, the free hand opening after it.
        Cast,
        // The Daggers: both blades drawn back to the hips, then one jabbed forward, the other hand leading the next.
        Thrust
    }

    // One frame of a cut-out puppet: joint angles in degrees and a few offsets in world units, in the character's own
    // terms (positive is forward, flexion is positive), each relative to the pose the character was painted in. Everything
    // is described for a figure facing screen-right; the rig mirrors the whole puppet for leftward travel and for parts
    // painted facing left, so no value here knows about facing. Rotation turns these terms into the counter-clockwise
    // screen angle of each part, Rise and Forward into the offset of its joint.
    public struct PuppetPose
    {
        // The whole figure toppling backward about its feet, for death.
        public float Fall;
        // The pelvis above its rest height (world units): the walk's bob, the crouch of a cast, the collapse of a death.
        public float Lift;
        // The pelvis ahead of its rest place (world units): the idle's weight shift, the jolt of a hit.
        public float Shift;
        // The torso's pitch: positive leans forward, into the walk.
        public float Lean;
        // How far the chest turns the right shoulder forward (world units the right shoulder joint moves ahead, the left
        // as far back): the counter-rotation of the shoulders against the stride, the wind-up of a swing.
        public float Twist;
        // How far the hips turn the right hip forward (world units, the left as far back), with the stride.
        public float HipTwist;
        // The chest's rise above the pelvis (world units): the idle breath.
        public float Breath;
        // The head's nod relative to the torso: positive tips it forward.
        public float Head;
        // How far the cape trails behind the shoulders: positive swings its hem back.
        public float Cape;
        // How far the skirt trails behind the hips: positive swings its hem back.
        public float Skirt;
        // Thigh swing about the hip: positive carries the foot ahead of the hip.
        public float HipLeft;
        public float HipRight;
        // Knee flexion: zero is the leg as painted, positive folds the shin back under the thigh.
        public float KneeLeft;
        public float KneeRight;
        // Upper arm swing about the shoulder: positive carries the hand forward.
        public float ShoulderLeft;
        public float ShoulderRight;
        // Elbow flexion: zero is the arm as painted, positive folds the forearm forward.
        public float ElbowLeft;
        public float ElbowRight;
        // The hand's turn at the wrist: positive tips it forward and up. A held weapon turns with it.
        public float WristLeft;
        public float WristRight;
        // How far the held weapons are in use, 0 carried to 1 swinging: a facing that carries its weapons at another
        // angle than they are drawn (seen from the front the sword hangs point-down) turns them back as they are used.
        public float Wield;

        // The part's counter-clockwise screen angle in degrees for a figure facing screen-right. A limb hanging down swings
        // its far end forward (toward +x) when it turns counter-clockwise, so swings and elbows keep their sign; a knee
        // folds the shin back and a cape or skirt trails back, both clockwise; the torso and head point up, so leaning
        // them forward is clockwise too. One body, never inlined, so every caller turns the same float the same way.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public readonly float Rotation(PuppetBone bone)
        {
            switch (bone)
            {
                case PuppetBone.Torso:
                    return -Lean;
                case PuppetBone.Head:
                    return -Head;
                case PuppetBone.Cape:
                    return -Cape;
                case PuppetBone.Skirt:
                    return -Skirt;
                case PuppetBone.ThighLeft:
                    return HipLeft;
                case PuppetBone.ShinLeft:
                    return -KneeLeft;
                case PuppetBone.ThighRight:
                    return HipRight;
                case PuppetBone.ShinRight:
                    return -KneeRight;
                case PuppetBone.UpperArmLeft:
                    return ShoulderLeft;
                case PuppetBone.ForearmLeft:
                    return ElbowLeft;
                case PuppetBone.HandLeft:
                    return WristLeft;
                case PuppetBone.UpperArmRight:
                    return ShoulderRight;
                case PuppetBone.ForearmRight:
                    return ElbowRight;
                case PuppetBone.HandRight:
                    return WristRight;
                default:
                    return 0f;
            }
        }

        // How far the part's joint rises, in world units: the pelvis carries the bob and the torso the breath.
        public readonly float Rise(PuppetBone bone)
        {
            switch (bone)
            {
                case PuppetBone.Pelvis:
                    return Lift;
                case PuppetBone.Torso:
                    return Breath;
                default:
                    return 0f;
            }
        }

        // How far the part's joint moves forward, in world units, in its parent's frame: the pelvis carries the weight
        // shift, the hips and shoulders turn against each other. The near side is the figure's right in both painted
        // facings, so a positive twist brings the near shoulder or hip toward the travel.
        public readonly float Forward(PuppetBone bone)
        {
            switch (bone)
            {
                case PuppetBone.Pelvis:
                    return Shift;
                case PuppetBone.ThighRight:
                    return HipTwist;
                case PuppetBone.ThighLeft:
                    return -HipTwist;
                case PuppetBone.UpperArmRight:
                    return Twist;
                case PuppetBone.UpperArmLeft:
                    return -Twist;
                default:
                    return 0f;
            }
        }
    }
}
