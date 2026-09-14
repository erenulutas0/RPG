using System;

namespace Cryptforge.Art
{
    // One pixel of placeholder art: straight (not premultiplied) 8-bit colour. Engine-independent so drawing code is
    // testable without Unity; PixelSpriteFactory turns canvases into sprites.
    public readonly struct Rgba : IEquatable<Rgba>
    {
        public static readonly Rgba Transparent = new Rgba(0, 0, 0, 0);
        public static readonly Rgba White = new Rgba(255, 255, 255);
        public static readonly Rgba Black = new Rgba(0, 0, 0);

        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public Rgba(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public bool IsOpaque => A == 255;
        public bool IsTransparent => A == 0;

        // "#RRGGBB" or "#RRGGBBAA", with or without the '#'.
        public static Rgba FromHex(string hex)
        {
            if (hex == null)
                throw new ArgumentNullException(nameof(hex));
            string digits = hex.StartsWith("#", StringComparison.Ordinal) ? hex.Substring(1) : hex;
            if (digits.Length != 6 && digits.Length != 8)
                throw new ArgumentException($"'{hex}' is not a #RRGGBB or #RRGGBBAA colour.", nameof(hex));

            byte r = Convert.ToByte(digits.Substring(0, 2), 16);
            byte g = Convert.ToByte(digits.Substring(2, 2), 16);
            byte b = Convert.ToByte(digits.Substring(4, 2), 16);
            byte a = digits.Length == 8 ? Convert.ToByte(digits.Substring(6, 2), 16) : (byte)255;
            return new Rgba(r, g, b, a);
        }

        // The same colour at another opacity.
        public Rgba WithAlpha(byte alpha) => new Rgba(R, G, B, alpha);

        // Multiplies the colour channels: 0.8 darkens, 1.2 lightens, clamped to 255.
        public Rgba Scale(float factor)
        {
            if (float.IsNaN(factor) || factor < 0f)
                throw new ArgumentOutOfRangeException(nameof(factor));
            return new Rgba(Clamp(R * factor), Clamp(G * factor), Clamp(B * factor), A);
        }

        // Linear blend toward another colour; t = 0 keeps this colour, t = 1 gives the other.
        public Rgba Lerp(Rgba other, float t)
        {
            if (float.IsNaN(t))
                throw new ArgumentOutOfRangeException(nameof(t));
            t = Math.Max(0f, Math.Min(1f, t));
            return new Rgba(Mix(R, other.R, t), Mix(G, other.G, t), Mix(B, other.B, t), Mix(A, other.A, t));
        }

        // Source-over compositing: the given colour drawn on top of this one.
        public Rgba Under(Rgba over)
        {
            if (over.A == 255 || A == 0)
                return over;
            if (over.A == 0)
                return this;

            float overAlpha = over.A / 255f;
            float underAlpha = A / 255f * (1f - overAlpha);
            float alpha = overAlpha + underAlpha;
            return new Rgba(
                Clamp((over.R * overAlpha + R * underAlpha) / alpha),
                Clamp((over.G * overAlpha + G * underAlpha) / alpha),
                Clamp((over.B * overAlpha + B * underAlpha) / alpha),
                Clamp(alpha * 255f));
        }

        public bool Equals(Rgba other) => R == other.R && G == other.G && B == other.B && A == other.A;
        public override bool Equals(object obj) => obj is Rgba other && Equals(other);
        public override int GetHashCode() => (R << 24) | (G << 16) | (B << 8) | A;
        public override string ToString() => A == 255 ? $"#{R:X2}{G:X2}{B:X2}" : $"#{R:X2}{G:X2}{B:X2}{A:X2}";
        public static bool operator ==(Rgba left, Rgba right) => left.Equals(right);
        public static bool operator !=(Rgba left, Rgba right) => !left.Equals(right);

        private static byte Mix(byte from, byte to, float t) => Clamp(from + (to - from) * t);

        private static byte Clamp(float value) => (byte)Math.Max(0f, Math.Min(255f, (float)Math.Round(value)));
    }
}
