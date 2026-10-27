using System;

namespace Cryptforge.Art
{
    // One cut-out part of the placeholder puppet: its canvas, the texel its joint turns about, and where that joint sits.
    public sealed class PuppetPlaceholderPart
    {
        public PuppetPlaceholderPart(string name, PuppetBone bone, int parent, float jointX, float jointY, int sorting,
            PixelCanvas canvas, int pivotX, int pivotY)
        {
            Name = name;
            Bone = bone;
            Parent = parent;
            JointX = jointX;
            JointY = jointY;
            Sorting = sorting;
            Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            PivotX = pivotX;
            PivotY = pivotY;
        }

        public string Name { get; }
        public PuppetBone Bone { get; }
        // An earlier part of the same facing, or -1 for a part hung from the feet.
        public int Parent { get; }
        // The joint on the parent, in texels from the parent's pivot (from the feet for a root part).
        public float JointX { get; }
        public float JointY { get; }
        // Draw order over the hero body's sorting order.
        public int Sorting { get; }
        public PixelCanvas Canvas { get; }
        // The canvas texel the part turns about: the sprite's pivot.
        public int PivotX { get; }
        public int PivotY { get; }
    }

    // The puppet's stand-in parts until the owner's painted sheets arrive (docs/45 section 5, P2): plain shaded capsules and
    // ovals, two tones a facing - the near limbs, torso and head in one, the far limbs, pelvis and cape in the other - so a
    // capture shows which leg is which at every phase. The facings use different tones, so a capture also shows the turn.
    // Sized like the Vanguard (about 42 texels to the hood's peak, 1.3 units, at 32 texels a unit) with the weapon hand at
    // the hip, where HeroArt's weapons were drawn to hang. Both facings are drawn facing screen-right, as the rig expects.
    public static class PuppetPlaceholderArt
    {
        // The part order is the same for both facings, parents first.
        public const int Pelvis = 0;
        public const int Torso = 1;
        public const int Head = 2;
        public const int Cape = 3;
        public const int ThighLeft = 4;
        public const int ShinLeft = 5;
        public const int ThighRight = 6;
        public const int ShinRight = 7;
        public const int UpperArmLeft = 8;
        public const int ForearmLeft = 9;
        public const int HandLeft = 10;
        public const int UpperArmRight = 11;
        public const int ForearmRight = 12;
        public const int HandRight = 13;
        public const int PartCount = 14;

        // Where a weapon's grip sits in each hand, in texels from the wrist: the middle of the fist.
        public const float GripX = 0f;
        public const float GripY = -2f;
        // The held weapons' draw order: the right-hand weapon between the forearm and the fist that closes over its grip
        // (its staff orb one above), the left-hand item just in front of the far hand and still behind the torso.
        public const int RightItemSorting = 8;
        public const int LeftItemSorting = -9;

        // Rear three-quarter (facing up-right, the back to the camera): Ilya's ash-white robe and soot trim (docs/45).
        public static readonly Rgba RearNear = Rgba.FromHex("#D8CFC2");
        public static readonly Rgba RearFar = Rgba.FromHex("#4E4458");
        // Front three-quarter (facing down-right): warmer tones, so a capture shows the turn at a glance.
        public static readonly Rgba FrontNear = Rgba.FromHex("#E3C9A8");
        public static readonly Rgba FrontFar = Rgba.FromHex("#9A4A30");

        // The pelvis's centre above the feet. The hip sits a texel below it and the knee seven below that; seven more
        // put the soles' centres one and a half texels up, so the outline under them ends on the floor line, as the
        // Vanguard's boots stand on theirs.
        private const float PelvisHeight = 16.5f;
        private const int LimbRadius = 2;
        private const int ThighLength = 7;
        private const int ShinLength = 5;
        private const int UpperArmLength = 6;
        private const int ForearmLength = 5;
        private const int TorsoRadius = 5;
        private const int TorsoLength = 7;
        private const int HeadRadius = 6;

        // Every part of one facing, parents before children. A fresh set of canvases each call: the caller owns them.
        public static PuppetPlaceholderPart[] Draw(bool front)
        {
            Rgba near = front ? FrontNear : RearNear;
            Rgba far = front ? FrontFar : RearFar;
            // The near side is the hero's right in both facings: on screen-right seen from behind, on screen-left seen from
            // the front. The far hip and shoulder sit closer to the middle, foreshortened by the turn.
            float nearHip = front ? -3f : 3f;
            float farHip = front ? 2f : -2f;
            float nearShoulder = front ? -5f : 5f;
            float farShoulder = front ? 4f : -4f;
            var parts = new PuppetPlaceholderPart[PartCount];

            PixelCanvas pelvis = Oval(6, 3, far, out int px, out int py);
            parts[Pelvis] = new PuppetPlaceholderPart("Pelvis", PuppetBone.Pelvis, -1, 0f, PelvisHeight, -3, pelvis, px, py);
            PixelCanvas torso = Capsule(TorsoRadius, TorsoLength, 0, near, out int tx, out int tBottom, out _);
            parts[Torso] = new PuppetPlaceholderPart("Torso", PuppetBone.Torso, Pelvis, 0f, 2f, 0, torso, tx, tBottom);
            PixelCanvas head = DrawHead(near, far, front, out int hx, out int hy);
            parts[Head] = new PuppetPlaceholderPart("Head", PuppetBone.Head, Torso, 1f, 11f, 4, head, hx, hy);
            // Seen from behind the cape covers the back; seen from the front it hangs behind everything.
            PixelCanvas cape = DrawCape(far, out int cx, out int cy);
            parts[Cape] = new PuppetPlaceholderPart("Cape", PuppetBone.Cape, Torso, 0f, 10f, front ? -14 : 2, cape, cx, cy);

            parts[ThighLeft] = Limb("Thigh Left", PuppetBone.ThighLeft, Pelvis, farHip, -1f, -7, ThighLength, 0, far);
            parts[ShinLeft] = Limb("Shin Left", PuppetBone.ShinLeft, ThighLeft, 0f, -ThighLength, -8, ShinLength, 2, far);
            parts[ThighRight] = Limb("Thigh Right", PuppetBone.ThighRight, Pelvis, nearHip, -1f, -5, ThighLength, 0, near);
            parts[ShinRight] = Limb("Shin Right", PuppetBone.ShinRight, ThighRight, 0f, -ThighLength, -6, ShinLength, 2, near);

            parts[UpperArmLeft] = Limb("Upper Arm Left", PuppetBone.UpperArmLeft, Torso, farShoulder, 9f, -12, UpperArmLength, 0, far);
            parts[ForearmLeft] = Limb("Forearm Left", PuppetBone.ForearmLeft, UpperArmLeft, 0f, -UpperArmLength, -11, ForearmLength, 0, far);
            parts[HandLeft] = Hand("Hand Left", PuppetBone.HandLeft, ForearmLeft, -10, far);
            parts[UpperArmRight] = Limb("Upper Arm Right", PuppetBone.UpperArmRight, Torso, nearShoulder, 9f, 6, UpperArmLength, 0, near);
            parts[ForearmRight] = Limb("Forearm Right", PuppetBone.ForearmRight, UpperArmRight, 0f, -UpperArmLength, 7, ForearmLength, 0, near);
            parts[HandRight] = Hand("Hand Right", PuppetBone.HandRight, ForearmRight, 10, near);
            return parts;
        }

        // A limb hung from its top joint: a capsule whose upper cap turns about the joint and whose lower cap centre is the
        // next joint, length texels below. A shin gets a toe reaching forward.
        private static PuppetPlaceholderPart Limb(string name, PuppetBone bone, int parent, float jointX, float jointY,
            int sorting, int length, int toe, Rgba tone)
        {
            PixelCanvas canvas = Capsule(LimbRadius, length, toe, tone, out int x, out _, out int top);
            return new PuppetPlaceholderPart(name, bone, parent, jointX, jointY, sorting, canvas, x, top);
        }

        // A fist hung from the wrist at the forearm's lower cap; the grip is its middle.
        private static PuppetPlaceholderPart Hand(string name, PuppetBone bone, int parent, int sorting, Rgba tone)
        {
            var canvas = new PixelCanvas(7, 7);
            canvas.FillEllipse(3, 3, 2, 2, tone);
            Shade(canvas, tone);
            canvas.Outline(PixelPalette.Outline);
            return new PuppetPlaceholderPart(name, bone, parent, 0f, -ForearmLength, sorting, canvas, 3, 5);
        }

        // A vertical capsule of the given radius with its cap centres length texels apart, inside a one-texel outline margin.
        private static PixelCanvas Capsule(int radius, int length, int toe, Rgba tone, out int centreX, out int bottomY,
            out int topY)
        {
            int width = 2 * radius + 3 + toe;
            int height = 2 * radius + length + 3;
            var canvas = new PixelCanvas(width, height);
            centreX = radius + 1;
            bottomY = radius + 1;
            topY = bottomY + length;
            canvas.FillEllipse(centreX, topY, radius, radius, tone);
            canvas.FillEllipse(centreX, bottomY, radius, radius, tone);
            canvas.FillRect(1, bottomY, 2 * radius + 1, length + 1, tone);
            // A boot: the sole on the canvas's lowest drawn row, the toe reaching toe texels past the shin.
            if (toe > 0)
                canvas.FillEllipse(centreX + 1, 2, radius + toe - 1, 1, tone);
            Shade(canvas, tone);
            canvas.Outline(PixelPalette.Outline);
            return canvas;
        }

        private static PixelCanvas Oval(int radiusX, int radiusY, Rgba tone, out int centreX, out int centreY)
        {
            var canvas = new PixelCanvas(2 * radiusX + 3, 2 * radiusY + 3);
            centreX = radiusX + 1;
            centreY = radiusY + 1;
            canvas.FillEllipse(centreX, centreY, radiusX, radiusY, tone);
            Shade(canvas, tone);
            canvas.Outline(PixelPalette.Outline);
            return canvas;
        }

        // A round hood with Ilya's peak leaning back; from the front a face with two goggle lenses, from behind the hood's
        // seam. Turns about the neck, near its lower edge.
        private static PixelCanvas DrawHead(Rgba tone, Rgba trim, bool front, out int pivotX, out int pivotY)
        {
            int size = 2 * HeadRadius + 5;
            var canvas = new PixelCanvas(size, size);
            int centre = HeadRadius + 1;
            canvas.FillEllipse(centre, centre, HeadRadius, HeadRadius, tone);
            canvas.FillTriangle(centre - 3, centre + 3, centre + 2, centre + 5, centre - 5, centre + HeadRadius + 2, tone);
            Shade(canvas, tone);
            if (front)
            {
                canvas.FillEllipse(centre + 2, centre - 1, 3, 2, tone.Lerp(Rgba.White, 0.55f));
                canvas.Set(centre + 1, centre, PixelPalette.EmberLight);
                canvas.Set(centre + 4, centre, PixelPalette.Visor);
                canvas.Set(centre + 2, centre - 2, trim);
            }
            else
            {
                canvas.Line(centre - 1, centre + HeadRadius - 1, centre - 2, centre - HeadRadius + 2, trim);
            }
            canvas.Outline(PixelPalette.Outline);
            pivotX = centre;
            pivotY = centre - HeadRadius + 2;
            return canvas;
        }

        // A cape widening from the shoulders to a scalloped hem, hung from its top edge.
        private static PixelCanvas DrawCape(Rgba tone, out int pivotX, out int pivotY)
        {
            var canvas = new PixelCanvas(16, 18);
            canvas.FillTriangle(4, 16, 12, 16, 14, 1, tone);
            canvas.FillTriangle(4, 16, 14, 1, 2, 1, tone);
            canvas.Set(5, 1, Rgba.Transparent);
            canvas.Set(11, 1, Rgba.Transparent);
            Shade(canvas, tone);
            canvas.Outline(PixelPalette.Outline);
            pivotX = 8;
            pivotY = 15;
            return canvas;
        }

        // Lights the shape from the upper left: base-tone texels on an upper or left edge lighten, on a lower or right edge
        // darken, so each capsule reads as round at phone size.
        private static void Shade(PixelCanvas canvas, Rgba tone)
        {
            Rgba light = tone.Lerp(Rgba.White, 0.3f);
            Rgba dark = tone.Scale(0.65f);
            PixelCanvas source = canvas.Clone();
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (source.Get(x, y) != tone)
                        continue;
                    if (source.Get(x + 1, y).IsTransparent || source.Get(x, y - 1).IsTransparent)
                        canvas.Set(x, y, dark);
                    else if (source.Get(x - 1, y).IsTransparent || source.Get(x, y + 1).IsTransparent)
                        canvas.Set(x, y, light);
                }
            }
        }
    }
}
