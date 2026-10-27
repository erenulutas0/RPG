// Not part of the game: Tools/ArtReview/preview_vanguard_puppet.py compiles this with the pure puppet sources
// (Scripts/Art/PuppetPose.cs, PuppetGait.cs, PuppetMotion.cs) into a throwaway console app and reads what it prints.
// It plays scripted scenes through PuppetMotion at 60 frames a second - walking, standing, striking with the weapon's
// real interval and the draw PuppetAnimator gives it from the cooldown, casting, being hit, dying - and prints every
// frame's pose, floor position and facing as JSON lines, so the previewer draws exactly what the game would.
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
        }

        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.Error.WriteLine("usage: driver <scene> [weapon: sword|staff|daggers] [24 leg floats: rear, front]");
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
            if (args.Length >= 2 + 24)
            {
                for (int f = 0; f < 2; f++)
                {
                    legs[f].Right = Leg(args, 2 + f * 12);
                    legs[f].Left = Leg(args, 2 + f * 12 + 6);
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

        private static PuppetLeg Leg(string[] args, int at)
        {
            float F(int i) => float.Parse(args[at + i], CultureInfo.InvariantCulture);
            return new PuppetLeg { KneeX = F(0), KneeY = F(1), HeelX = F(2), HeelY = F(3), ToeX = F(4), ToeY = F(5) };
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
            motion.Legs = legs[0];
            var output = new StringBuilder();
            float x = 0f, y = 0f, time = 0f;
            float cooldown = 0f;
            bool front = false, mirrored = false;
            bool lit = false;
            int frame = 0;
            foreach (Segment segment in scene)
            {
                int frames = (int)Math.Round(segment.Seconds / Dt);
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
                    if (travel >= PuppetMotion.MinimumTravel)
                    {
                        bool wasFront = front;
                        Face(dx, dy, ref front, ref mirrored);
                        // PuppetAnimator shows the other facing's rig, whose legs the bob must stand on.
                        if (front != wasFront)
                            motion.Legs = legs[front ? 1 : 0];
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
                    if (segment.CastAt >= 0f && Math.Abs(local - segment.CastAt) < Dt * 0.5f)
                        motion.Cast();
                    bool hit = segment.HitAt >= 0f && Math.Abs(local - segment.HitAt) < Dt * 0.5f;
                    if (hit)
                        motion.Hit();
                    if (segment.DieAt >= 0f && Math.Abs(local - segment.DieAt) < Dt * 0.5f)
                        motion.Die();
                    motion.Tick(Dt, travel);
                    if (!motion.IsAttacking)
                        lit = false;
                    Write(output, frame, time, x, y, dx, dy, front, mirrored, weapon, attacked, hit, lit, motion);
                    time += Dt;
                }
            }
            Console.Out.Write(output.ToString());
        }

        // PuppetAnimator.Face: each axis keeps its facing inside a small dead band.
        private static void Face(float dx, float dy, ref bool front, ref bool mirrored)
        {
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 1e-4f)
                return;
            float nx = dx / length, ny = dy / length;
            if (Math.Abs(nx) > .15f)
                mirrored = nx < 0f;
            if (Math.Abs(ny) > .15f)
                front = ny < 0f;
        }

        private static void Write(StringBuilder output, int frame, float time, float x, float y, float dx, float dy,
            bool front, bool mirrored, string weapon, bool attacked, bool hit, bool lit, PuppetMotion motion)
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
            output.Append("\"front\":").Append(front ? "true" : "false").Append(',');
            output.Append("\"mirrored\":").Append(mirrored ? "true" : "false").Append(',');
            output.Append("\"weapon\":\"").Append(weapon).Append("\",");
            output.Append("\"attacked\":").Append(attacked ? "true" : "false").Append(',');
            output.Append("\"hit\":").Append(hit ? "true" : "false").Append(',');
            output.Append("\"lit\":").Append(lit ? "true" : "false").Append(',');
            output.Append("\"dying\":").Append(motion.IsDying ? "true" : "false").Append(',');
            Field(output, "walk", input.WalkWeight);
            Field(output, "phase", input.WalkPhase);
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
