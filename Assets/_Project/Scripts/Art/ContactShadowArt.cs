using System;

namespace Cryptforge.Art
{
    // One small ground ellipse shared by every actor. No light, outline, or baked body shape.
    public static class ContactShadowArt
    {
        public const int Width = 32;
        public const int Height = 16;

        public static PixelCanvas Draw()
        {
            var canvas = new PixelCanvas(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    float nx = (x + .5f - Width * .5f) / 15f;
                    float ny = (y + .5f - Height * .5f) / 7f;
                    float radius = nx * nx + ny * ny;
                    if (radius >= 1f)
                        continue;
                    byte alpha = radius < .18f ? (byte)132 : radius < .42f ? (byte)96 : radius < .72f ? (byte)58 : (byte)24;
                    canvas.Set(x, y, new Rgba(9, 10, 20, alpha));
                }
            }
            return canvas;
        }

        // Conservatively fit the whole sprite rectangle on the diamond. Near a rim the footprint contracts rather
        // than painting darkness onto the vertical face/void. Inputs are projected world units relative to the arena.
        public static float Fit(ArenaGeometry geometry, float x, float y, float width)
        {
            if (!(width > 0f) || float.IsInfinity(width))
                throw new ArgumentOutOfRangeException(nameof(width));
            float halfDepth = (geometry.WorldTop - geometry.WorldBottom) * .5f;
            float available = 1f - Math.Abs(x) / geometry.HalfWidth - Math.Abs(y - geometry.WorldMiddle) / halfDepth;
            float footprint = width * .5f / geometry.HalfWidth + width * .25f / halfDepth;
            return Math.Max(0f, Math.Min(1f, (available - .00001f) / footprint));
        }
    }
}
