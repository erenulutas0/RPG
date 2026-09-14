using System;

namespace Cryptforge.Art
{
    // The ability's placeholder art: the glyph on its button and the range ring drawn on the floor around the hero.
    public static class AbilityArt
    {
        public const int GlyphSize = 20;
        // Texels of ring line either side of the ideal ellipse, and the dash rhythm along it.
        public const int RingThickness = 2;
        public const int DashLength = 5;
        public const int DashGap = 3;

        private static readonly Rgba GlyphCore = PixelPalette.SpellLight;
        private static readonly Rgba GlyphEdge = PixelPalette.Spell;

        // A four-point burst: the glyph shown on the ability button.
        public static PixelCanvas DrawGlyph()
        {
            var canvas = new PixelCanvas(GlyphSize, GlyphSize);
            int centre = GlyphSize / 2;
            const int reach = 8;
            // The long points along the axes, the short ones on the diagonals.
            canvas.FillTriangle(centre - 2, centre, centre + 2, centre, centre, centre + reach, GlyphEdge);
            canvas.FillTriangle(centre - 2, centre, centre + 2, centre, centre, centre - reach, GlyphEdge);
            canvas.FillTriangle(centre, centre - 2, centre, centre + 2, centre + reach, centre, GlyphEdge);
            canvas.FillTriangle(centre, centre - 2, centre, centre + 2, centre - reach, centre, GlyphEdge);
            for (int d = 2; d <= 4; d++)
            {
                canvas.Set(centre + d, centre + d, GlyphEdge);
                canvas.Set(centre - d, centre + d, GlyphEdge);
                canvas.Set(centre + d, centre - d, GlyphEdge);
                canvas.Set(centre - d, centre - d, GlyphEdge);
            }
            canvas.FillEllipse(centre, centre, 2, 2, GlyphCore);
            canvas.FillRect(centre - 1, centre + 3, 2, 3, GlyphCore);
            canvas.FillRect(centre - 1, centre - 5, 2, 3, GlyphCore);
            canvas.Outline(PixelPalette.Outline);
            return canvas;
        }

        // The ring a floor circle of radiusTexels makes on screen: an ellipse twice as wide as it is tall, dashed so it
        // reads as a marker rather than a wall. Centred on the canvas; the hero's feet go at that centre.
        public static PixelCanvas DrawRangeRing(int radiusTexels)
        {
            if (radiusTexels < 4)
                throw new ArgumentOutOfRangeException(nameof(radiusTexels));

            int rx = radiusTexels;
            int ry = Math.Max(2, radiusTexels / 2);
            int width = 2 * rx + 2 * RingThickness + 1;
            int height = 2 * ry + 2 * RingThickness + 1;
            var canvas = new PixelCanvas(width, height);
            int cx = width / 2;
            int cy = height / 2;
            // Walk the ellipse by angle; the dash counter follows the arc length so dashes stay even all round.
            int steps = Math.Max(64, radiusTexels * 6);
            float dashPhase = 0f;
            float lastX = cx + rx;
            float lastY = cy;
            for (int i = 0; i <= steps; i++)
            {
                double angle = i * Math.PI * 2.0 / steps;
                float x = cx + (float)(Math.Cos(angle) * rx);
                float y = cy + (float)(Math.Sin(angle) * ry);
                dashPhase += (float)Math.Sqrt((x - lastX) * (x - lastX) + (y - lastY) * (y - lastY));
                lastX = x;
                lastY = y;
                if (dashPhase % (DashLength + DashGap) >= DashLength)
                    continue;
                int px = (int)Math.Round(x);
                int py = (int)Math.Round(y);
                canvas.Set(px, py, PixelPalette.SpellLight);
                canvas.Set(px, py + (py >= cy ? 1 : -1), PixelPalette.Spell);
            }
            return canvas;
        }
    }
}
