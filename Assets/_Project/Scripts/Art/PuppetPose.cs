using System.Runtime.CompilerServices;

namespace Cryptforge.Art
{
    // Which channel of a PuppetPose turns a cut-out part (docs/45 section 4 B). A painted parts sheet names each piece's
    // bone once; the same pose then drives every character, however its parts are cut. Fixed parts (a belt, a flask) only
    // ride on their parent.
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

    // The arm motion an attack plays over the walk. Only the arms take it: the legs keep their stride (the owner,
    // 2026-10-26: a hero that froze its legs for every attack glided across the floor).
    public enum PuppetStrike
    {
        None,
        // The Sword: the blade shown at the moment of the hit, forward and down, then a short follow-through.
        Swing,
        // The Staff: the weapon arm reaches forward and tips the staff, the free hand opens toward the target.
        Cast,
        // The Daggers: both arms jab forward along the blades.
        Thrust
    }

    // One frame of a cut-out puppet: joint angles in degrees and two small offsets in world units, in the character's own
    // terms (positive is forward, flexion is positive). Everything is described for a figure facing screen-right; the
    // rig mirrors the whole puppet for leftward travel and for parts painted facing left, so no value here knows about
    // facing. Rotation turns these terms into the counter-clockwise screen angle of each part.
    public struct PuppetPose
    {
        // The whole figure toppling backward about its feet, for death.
        public float Fall;
        // The pelvis above its rest height (world units): the walk's bob at each contact, the collapse of a death.
        public float Lift;
        // The torso's pitch: positive leans forward, into the walk.
        public float Lean;
        // The chest's rise above the pelvis (world units): the idle breath.
        public float Breath;
        // The head's nod relative to the torso: positive tips it forward.
        public float Head;
        // How far the cape trails behind the shoulders: positive swings its hem back.
        public float Cape;
        // Thigh swing about the hip: positive carries the foot ahead of the hip.
        public float HipLeft;
        public float HipRight;
        // Knee flexion: zero is a straight leg, positive folds the shin back under the thigh.
        public float KneeLeft;
        public float KneeRight;
        // Upper arm swing about the shoulder: positive carries the hand forward.
        public float ShoulderLeft;
        public float ShoulderRight;
        // Elbow flexion: zero is a straight arm, positive folds the forearm forward.
        public float ElbowLeft;
        public float ElbowRight;
        // The hand's turn at the wrist: positive tips it forward and up. A held weapon turns with it.
        public float WristLeft;
        public float WristRight;

        // The part's counter-clockwise screen angle in degrees for a figure facing screen-right. A limb hanging down swings
        // its far end forward (toward +x) when it turns counter-clockwise, so swings and elbows keep their sign; a knee
        // folds the shin back and a cape trails back, both clockwise; the torso and head point up, so leaning them forward
        // is clockwise too. One body, never inlined, so every caller turns the same float the same way.
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
                    // The skirt hangs from the pelvis and trails a quarter of what the cape does: cloth closer to the legs.
                    return -0.25f * Cape;
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

        // How far the part rises above its joint, in world units: the pelvis carries the bob and the torso the breath.
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
    }
}
