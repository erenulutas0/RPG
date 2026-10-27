// Not part of the game: Tools/ArtReview/preview_vanguard_puppet.py compiles this with the pure puppet sources
// (Scripts/Art/PuppetPose.cs, PuppetGait.cs, PuppetMotion.cs, PuppetFacing.cs) into a throwaway console app and reads
// what it prints. It plays scripted scenes through PuppetMotion and PuppetFacing at 60 frames a second - walking on
// every heading, turning, standing, striking with the weapon's real interval and the draw PuppetAnimator gives it from
// the cooldown, casting, being hit, dying - and prints every frame's pose, floor position and facing as JSON lines, so
// the previewer draws exactly what the game would.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Cryptforge.Art;

namespace Cryptforge.Preview
{
    public static class PuppetPreviewDriver
    {
        private const float Dt = 1f / 60f;
        // The arena's depth scale (ArenaFloor.DepthScale): floor y projects to half its length on screen.
        private const float DepthScale = 0.5f;

        // One stretch of a scene: how long, how the hero moves (floor units a second along a floor direction), and what
        // happens during it.
        private sealed class Segment
        {
            public float Seconds;
            public float Speed;
            public float DirX = 1f;
            public float DirY;
            // Seconds between attacks of the carried weapon while this segment plays; 0 for none.
            public float AttackInterval;
            public float CastAt = -1f;
            public float HitAt = -1f;
            public float DieAt = -1f;
            // A target stays in reach throughout, so the draw before each attack plays.
            public bool TargetInReach = true;
            // A standing hero turned to face this floor direction as the segment starts (PuppetAnimator.FaceAttack);
            // zero for none.
            public float FaceX;
            public float FaceY;
        }

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: driver <scene> [weapon: sword|staff|daggers] [32 leg floats: rear, front]");
                return 2;
            }
            string weapon = args.Length > 1 ? args[1] : "sword";
            PuppetStrike kind = weapon == "staff" ? PuppetStrike.Cast : weapon == "daggers" ? PuppetStrike.Thrust
                : PuppetStrike.Swing;
            float interval = weapon == "staff" ? 1.1f : weapon == "daggers" ? 0.5f : 0.8f;
            List<Segment> scene = Scene(args[0], interval);
            if (scene == null)
            {
                Console.Error.WriteLine("unknown scene " + args[0]);
                return 2;
            }
            // The painted legs of each facing (PuppetRig.Legs), as the previewer read them off the art set.
            var legs = new PuppetLegs[2];
            if (args.Length >= 2 + 2 * 2 * LegFloats)
            {
                for (int f = 0; f < 2; f++)
                {
                    legs[f].Right = Leg(args, 2 + f * 2 * LegFloats);
                    legs[f].Left = Leg(args, 2 + f * 2 * LegFloats + LegFloats);
                }
            }
            Play(scene, kind, interval, weapon, legs);
            return 0;
        }

        private static List<Segment> Scene(string name, float interval)
        {
            switch (name)
            {
                case "idle":
                    return new List<Segment> { new Segment { Seconds = 5.6f } };
                // Run in from standing, a steady stride, a stop and the settle: rightward from behind and from the front.
                case "walk-rear":
                    return Walk(0.6f, 0.7071f);
                case "walk-front":
                    return Walk(0.6f, -0.7071f);
                case "walk-rear-left":
                    return Walk(-0.6f, 0.7071f);
                case "walk-front-left":
                    return Walk(-0.6f, -0.7071f);
                // The eight headings of a stick, each run in, held and stopped: the planted boot should hold the floor on
                // every one (Tools/ArtReview's slip report measures it).
                case "heading-E":
                    return Walk(1f, 0f);
                case "heading-NE":
                    return Walk(0.7071f, 0.7071f);
                case "heading-N":
                    return Walk(0f, 1f);
                case "heading-NW":
                    return Walk(-0.7071f, 0.7071f);
                case "heading-W":
                    return Walk(-1f, 0f);
                case "heading-SW":
                    return Walk(-0.7071f, -0.7071f);
                case "heading-S":
                    return Walk(0f, -1f);
                case "heading-SE":
                    return Walk(0.7071f, -0.7071f);
                // Turns: reversing across the screen (the mirror), up and down it (rear and front), round the
                // diagonals (both at once), and standing to face a target on each side.
                case "turn-mirror":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 0.9f, Speed = 3.5f, DirX = 1f },
                        new Segment { Seconds = 0.9f, Speed = 3.5f, DirX = -1f },
                        new Segment { Seconds = 0.9f, Speed = 3.5f, DirX = 1f },
                        new Segment { Seconds = 0.6f },
                    };
                case "turn-front":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 0.8f, Speed = 3.5f, DirX = 0.3f, DirY = 0.954f },
                        new Segment { Seconds = 0.8f, Speed = 3.5f, DirX = 0.3f, DirY = -0.954f },
                        new Segment { Seconds = 0.8f, Speed = 3.5f, DirX = 0.3f, DirY = 0.954f },
                        new Segment { Seconds = 0.6f },
                    };
                case "turn-diagonal":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 0.7f, Speed = 3.5f, DirX = 0.7071f, DirY = 0.7071f },
                        new Segment { Seconds = 0.7f, Speed = 3.5f, DirX = 0.7071f, DirY = -0.7071f },
                        new Segment { Seconds = 0.7f, Speed = 3.5f, DirX = -0.7071f, DirY = -0.7071f },
                        new Segment { Seconds = 0.7f, Speed = 3.5f, DirX = -0.7071f, DirY = 0.7071f },
                        new Segment { Seconds = 0.7f, Speed = 3.5f, DirX = 0.7071f, DirY = -0.7071f },
                        new Segment { Seconds = 0.6f },
                    };
                case "turn-stand":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.6f },
                        new Segment { Seconds = 0.6f, FaceX = -1f },
                        new Segment { Seconds = 0.6f, FaceX = -0.5f, FaceY = -1f },
                        new Segment { Seconds = 0.6f, FaceX = 1f, FaceY = -1f },
                        new Segment { Seconds = 0.6f, FaceX = 1f, FaceY = 1f },
                        new Segment { Seconds = 0.6f },
                    };
                case "walk-hall":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 2.2f, Speed = 2.5f, DirX = 0.7071f, DirY = 0.7071f },
                        new Segment { Seconds = 1.0f },
                    };
                case "strike-walk":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 3.2f, Speed = 3.5f, DirX = 0.7071f, DirY = 0.7071f, AttackInterval = interval },
                        new Segment { Seconds = 0.8f, AttackInterval = interval },
                    };
                case "strike-stand":
                    return new List<Segment> { new Segment { Seconds = 3.2f, AttackInterval = interval } };
                case "strike-walk-front":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 3.2f, Speed = 3.5f, DirX = 0.7071f, DirY = -0.7071f, AttackInterval = interval },
                    };
                case "cast":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 1.2f, CastAt = 0.4f },
                        new Segment { Seconds = 1.6f, Speed = 3.5f, DirX = 0.7071f, DirY = 0.7071f, CastAt = 0.7f },
                        new Segment { Seconds = 0.8f },
                    };
                case "hit":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 1.0f, HitAt = 0.3f },
                        new Segment { Seconds = 1.4f, Speed = 3.5f, DirX = 0.7071f, DirY = 0.7071f, HitAt = 0.6f },
                        new Segment { Seconds = 0.6f },
                    };
                case "death":
                    return new List<Segment>
                    {
                        new Segment { Seconds = 0.3f },
                        new Segment { Seconds = 0.9f, Speed = 3.5f, DirX = 0.7071f, DirY = 0.7071f, DieAt = 0.6f },
                        new Segment { Seconds = 1.4f },
                    };
                default:
                    return null;
            }
        }

        // A leg on the command line: hip, knee, heel and toe, x then y each.
        private const int LegFloats = 8;

        private static PuppetLeg Leg(string[] args, int at)
        {
            float F(int i) => float.Parse(args[at + i], CultureInfo.InvariantCulture);
            return new PuppetLeg
            {
                HipX = F(0), HipY = F(1), KneeX = F(2), KneeY = F(3), HeelX = F(4), HeelY = F(5), ToeX = F(6), ToeY = F(7)
            };
        }

        private static List<Segment> Walk(float x, float y)
        {
            float length = (float)Math.Sqrt(x * x + y * y);
            return new List<Segment>
            {
                new Segment { Seconds = 0.5f },
                new Segment { Seconds = 2.4f, Speed = 3.5f, DirX = x / length, DirY = y / length },
                new Segment { Seconds = 1.2f },
            };
        }

        private static void Play(List<Segment> scene, PuppetStrike kind, float interval, string weapon, PuppetLegs[] legs)
        {
            var motion = new PuppetMotion();
            var facing = new PuppetFacing();
            motion.Legs = legs[0];
            var output = new StringBuilder();
            float x = 0f, y = 0f, time = 0f;
            float cooldown = 0f;
            bool strideMirrored = false;
            bool lit = false;
            int frame = 0;
            foreach (Segment segment in scene)
            {
                int frames = (int)Math.Round(segment.Seconds / Dt);
                // PuppetAnimator.FaceAttack: a standing hero turns to its target.
                if ((segment.FaceX != 0f || segment.FaceY != 0f) && !motion.IsWalking)
                    facing.Face(segment.FaceX, segment.FaceY);
                for (int i = 0; i < frames; i++, frame++)
                {
                    float local = i * Dt;
                    float dx = segment.Speed * segment.DirX * Dt;
                    float dy = segment.Speed * segment.DirY * Dt;
                    if (motion.IsDying)
                        dx = dy = 0f;
                    x += dx;
                    y += dy;
                    float travel = (float)Math.Sqrt(dx * dx + dy * dy);
                    // PuppetAnimator.Tick: the facing asked for at once, the facing on show through its turn, whose swap
                    // shows the other rig (and its legs) and mirrors the stride's heading.
                    if (travel >= PuppetMotion.MinimumTravel)
                        facing.Face(dx, dy);
                    if (facing.Step(Dt))
                    {
                        if (facing.ShownMirrored != strideMirrored)
                        {
                            strideMirrored = facing.ShownMirrored;
                            motion.Mirror();
                        }
                        motion.Legs = legs[facing.ShownFront ? 1 : 0];
                        motion.Front = facing.ShownFront;
                    }

                    // The weapon, as AttackController fires it: the cooldown runs, and a ready weapon with a target in
                    // reach attacks at once; PuppetAnimator reads the cooldown for the draw, after the attack.
                    bool attacked = false;
                    if (segment.AttackInterval > 0f && !motion.IsDying)
                    {
                        cooldown -= Dt;
                        if (cooldown <= 0f && segment.TargetInReach)
                        {
                            cooldown = segment.AttackInterval;
                            motion.Strike(kind, segment.AttackInterval);
                            attacked = true;
                            lit = true;
                        }
                        float window = PuppetGait.WindupSeconds(segment.AttackInterval);
                        motion.Anticipate(kind, segment.TargetInReach && cooldown > 0f && cooldown <= window
                            ? 1f - cooldown / window : 0f);
                    }
                    else
                        motion.Anticipate(PuppetStrike.None, 0f);
                    bool cast = segment.CastAt >= 0f && Math.Abs(local - segment.CastAt) < Dt * 0.5f;
                    if (cast)
                        motion.Cast();
                    bool hit = segment.HitAt >= 0f && Math.Abs(local - segment.HitAt) < Dt * 0.5f;
                    if (hit)
                        motion.Hit();
                    if (segment.DieAt >= 0f && Math.Abs(local - segment.DieAt) < Dt * 0.5f)
                        motion.Die();
                    // PuppetAnimator's heading: the floor's travel in the shown facing's own terms.
                    float heading = (float)Math.Atan2(segment.DirY, segment.DirX * (facing.ShownMirrored ? -1f : 1f));
                    motion.Tick(Dt, travel, heading);
                    if (!motion.IsAttacking)
                        lit = false;
                    Write(output, frame, time, x, y, dx, dy, facing, weapon, attacked, cast, hit, lit, motion);
                    time += Dt;
                }
            }
            Console.Out.Write(output.ToString());
        }

        private static void Write(StringBuilder output, int frame, float time, float x, float y, float dx, float dy,
            PuppetFacing facing, string weapon, bool attacked, bool cast, bool hit, bool lit, PuppetMotion motion)
        {
            PuppetPose p = motion.Pose;
            PuppetGaitInput input = motion.Input;
            output.Append('{');
            Field(output, "frame", frame);
            Field(output, "t", time);
            Field(output, "x", x);
            Field(output, "y", y * DepthScale);
            Field(output, "dx", dx);
            Field(output, "dy", dy * DepthScale);
            // The facing on show, which the previewer draws, and the turn's widths.
            output.Append("\"front\":").Append(facing.ShownFront ? "true" : "false").Append(',');
            output.Append("\"mirrored\":").Append(facing.ShownMirrored ? "true" : "false").Append(',');
            output.Append("\"askFront\":").Append(facing.Front ? "true" : "false").Append(',');
            output.Append("\"askMirrored\":").Append(facing.Mirrored ? "true" : "false").Append(',');
            output.Append("\"headMirrored\":").Append(facing.HeadMirrored ? "true" : "false").Append(',');
            Field(output, "width", facing.Width);
            Field(output, "headWidth", facing.HeadWidth);
            output.Append("\"weapon\":\"").Append(weapon).Append("\",");
            output.Append("\"attacked\":").Append(attacked ? "true" : "false").Append(',');
            output.Append("\"cast\":").Append(cast ? "true" : "false").Append(',');
            output.Append("\"hit\":").Append(hit ? "true" : "false").Append(',');
            output.Append("\"lit\":").Append(lit ? "true" : "false").Append(',');
            output.Append("\"dying\":").Append(motion.IsDying ? "true" : "false").Append(',');
            Field(output, "walk", input.WalkWeight);
            Field(output, "phase", input.WalkPhase);
            Field(output, "heading", input.Heading);
            output.Append("\"PlantedRight\":").Append(p.RightPlanted ? "true" : "false").Append(',');
            output.Append("\"PlantedLeft\":").Append(p.LeftPlanted ? "true" : "false").Append(',');
            Field(output, "LegRiseRight", p.LegRiseRight);
            Field(output, "LegRiseLeft", p.LegRiseLeft);
            Field(output, "LegShiftRight", p.LegShiftRight);
            Field(output, "LegShiftLeft", p.LegShiftLeft);
            Field(output, "Fall", p.Fall);
            Field(output, "Lift", p.Lift);
            Field(output, "Shift", p.Shift);
            Field(output, "Lean", p.Lean);
            Field(output, "Twist", p.Twist);
            Field(output, "HipTwist", p.HipTwist);
            Field(output, "Breath", p.Breath);
            Field(output, "Head", p.Head);
            Field(output, "Cape", p.Cape);
            Field(output, "Skirt", p.Skirt);
            Field(output, "HipLeft", p.HipLeft);
            Field(output, "HipRight", p.HipRight);
            Field(output, "KneeLeft", p.KneeLeft);
            Field(output, "KneeRight", p.KneeRight);
            Field(output, "ShoulderLeft", p.ShoulderLeft);
            Field(output, "ShoulderRight", p.ShoulderRight);
            Field(output, "ElbowLeft", p.ElbowLeft);
            Field(output, "ElbowRight", p.ElbowRight);
            Field(output, "WristLeft", p.WristLeft);
            Field(output, "Wield", p.Wield);
            output.Append("\"WristRight\":").Append(p.WristRight.ToString("R", CultureInfo.InvariantCulture));
            output.Append("}\n");
        }

        private static void Field(StringBuilder output, string name, float value) =>
            output.Append('"').Append(name).Append("\":").Append(value.ToString("R", CultureInfo.InvariantCulture)).Append(',');
    }
}
