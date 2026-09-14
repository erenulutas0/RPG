using System;
using System.Collections.Generic;

namespace Cryptforge.Art
{
    // A small RGBA bitmap for placeholder pixel art drawn from code. Row 0 is the bottom row, as Unity textures store
    // them; string pixel maps are given top row first, as they are written. Drawing outside the canvas is clipped.
    public sealed class PixelCanvas
    {
        public const char TransparentChar = '.';

        private readonly Rgba[] _pixels;

        public int Width { get; }
        public int Height { get; }

        public PixelCanvas(int width, int height)
        {
            if (width < 1 || width > 4096)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 1 || height > 4096)
                throw new ArgumentOutOfRangeException(nameof(height));

            Width = width;
            Height = height;
            _pixels = new Rgba[width * height];
        }

        // Pixels in texture order: row by row from the bottom-left corner. The array is the canvas's own.
        public Rgba[] Pixels => _pixels;

        public bool Contains(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public Rgba Get(int x, int y) => Contains(x, y) ? _pixels[y * Width + x] : Rgba.Transparent;

        // Replaces the pixel, alpha included.
        public void Set(int x, int y, Rgba color)
        {
            if (Contains(x, y))
                _pixels[y * Width + x] = color;
        }

        // Draws the colour over the pixel with source-over compositing.
        public void Blend(int x, int y, Rgba color)
        {
            if (!Contains(x, y) || color.IsTransparent)
                return;
            int index = y * Width + x;
            _pixels[index] = _pixels[index].Under(color);
        }

        public void Fill(Rgba color)
        {
            for (int i = 0; i < _pixels.Length; i++)
                _pixels[i] = color;
        }

        public void Clear() => Fill(Rgba.Transparent);

        // Solid rectangle with its bottom-left corner at (x, y); blends when the colour is translucent.
        public void FillRect(int x, int y, int width, int height, Rgba color)
        {
            for (int py = Math.Max(0, y); py < Math.Min(Height, y + height); py++)
            {
                for (int px = Math.Max(0, x); px < Math.Min(Width, x + width); px++)
                    Plot(px, py, color);
            }
        }

        // One-pixel frame just inside the rectangle.
        public void OutlineRect(int x, int y, int width, int height, Rgba color)
        {
            if (width < 1 || height < 1)
                return;
            FillRect(x, y, width, 1, color);
            FillRect(x, y + height - 1, width, 1, color);
            FillRect(x, y, 1, height, color);
            FillRect(x + width - 1, y, 1, height, color);
        }

        // Solid ellipse centred on (cx, cy) with the given radii, in pixels; a radius of 0 draws one pixel.
        public void FillEllipse(int cx, int cy, int radiusX, int radiusY, Rgba color)
        {
            if (radiusX < 0 || radiusY < 0)
                return;
            float rx = radiusX + 0.5f;
            float ry = radiusY + 0.5f;
            for (int py = cy - radiusY; py <= cy + radiusY; py++)
            {
                float ny = (py - cy) / ry;
                for (int px = cx - radiusX; px <= cx + radiusX; px++)
                {
                    float nx = (px - cx) / rx;
                    if (nx * nx + ny * ny <= 1f)
                        Plot(px, py, color);
                }
            }
        }

        // Filled triangle from three corners.
        public void FillTriangle(int x0, int y0, int x1, int y1, int x2, int y2, Rgba color)
        {
            int minX = Math.Max(0, Math.Min(x0, Math.Min(x1, x2)));
            int maxX = Math.Min(Width - 1, Math.Max(x0, Math.Max(x1, x2)));
            int minY = Math.Max(0, Math.Min(y0, Math.Min(y1, y2)));
            int maxY = Math.Min(Height - 1, Math.Max(y0, Math.Max(y1, y2)));
            long area = Edge(x0, y0, x1, y1, x2, y2);
            if (area == 0)
                return;
            for (int py = minY; py <= maxY; py++)
            {
                for (int px = minX; px <= maxX; px++)
                {
                    long w0 = Edge(x1, y1, x2, y2, px, py);
                    long w1 = Edge(x2, y2, x0, y0, px, py);
                    long w2 = Edge(x0, y0, x1, y1, px, py);
                    bool inside = area > 0 ? w0 >= 0 && w1 >= 0 && w2 >= 0 : w0 <= 0 && w1 <= 0 && w2 <= 0;
                    if (inside)
                        Plot(px, py, color);
                }
            }
        }

        // Bresenham line, both ends included.
        public void Line(int x0, int y0, int x1, int y1, Rgba color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = -Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int error = dx + dy;
            while (true)
            {
                Plot(x0, y0, color);
                if (x0 == x1 && y0 == y1)
                    return;
                int doubled = 2 * error;
                if (doubled >= dy)
                {
                    error += dy;
                    x0 += sx;
                }
                if (doubled <= dx)
                {
                    error += dx;
                    y0 += sy;
                }
            }
        }

        // Checkerboard of the colour over the rectangle: the classic pixel-art way to blend two tones.
        public void Dither(int x, int y, int width, int height, Rgba color, bool oddPhase = false)
        {
            for (int py = Math.Max(0, y); py < Math.Min(Height, y + height); py++)
            {
                for (int px = Math.Max(0, x); px < Math.Min(Width, x + width); px++)
                {
                    if (((px + py) & 1) == (oddPhase ? 1 : 0))
                        Plot(px, py, color);
                }
            }
        }

        // Draws another canvas over this one with its bottom-left corner at (x, y).
        public void Blit(PixelCanvas source, int x, int y)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            for (int sy = 0; sy < source.Height; sy++)
            {
                for (int sx = 0; sx < source.Width; sx++)
                    Blend(x + sx, y + sy, source._pixels[sy * source.Width + sx]);
            }
        }

        // Paints a pixel map: rows are written top row first, one character per pixel; '.' (or any character missing
        // from the palette) is transparent. The map's bottom-left corner lands at (x, y).
        public void BlitMap(IReadOnlyList<string> rows, IReadOnlyDictionary<char, Rgba> palette, int x, int y)
        {
            if (rows == null)
                throw new ArgumentNullException(nameof(rows));
            if (palette == null)
                throw new ArgumentNullException(nameof(palette));
            for (int row = 0; row < rows.Count; row++)
            {
                string line = rows[row] ?? string.Empty;
                int py = y + rows.Count - 1 - row;
                for (int column = 0; column < line.Length; column++)
                {
                    if (line[column] != TransparentChar && palette.TryGetValue(line[column], out Rgba color))
                        Blend(x + column, py, color);
                }
            }
        }

        // Adds a one-pixel outline: every transparent pixel next to (4-neighbour) a visible one takes the colour.
        public void Outline(Rgba color)
        {
            var visible = new bool[_pixels.Length];
            for (int i = 0; i < _pixels.Length; i++)
                visible[i] = _pixels[i].A > 0;
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (visible[y * Width + x])
                        continue;
                    if (IsVisible(visible, x - 1, y) || IsVisible(visible, x + 1, y) || IsVisible(visible, x, y - 1) || IsVisible(visible, x, y + 1))
                        _pixels[y * Width + x] = color;
                }
            }
        }

        // Every visible pixel in one colour, keeping its alpha: the hit-flash overlay of a sprite.
        public PixelCanvas Silhouette(Rgba color)
        {
            var result = new PixelCanvas(Width, Height);
            for (int i = 0; i < _pixels.Length; i++)
            {
                if (_pixels[i].A > 0)
                    result._pixels[i] = color.WithAlpha(_pixels[i].A);
            }
            return result;
        }

        public PixelCanvas FlipHorizontal()
        {
            var result = new PixelCanvas(Width, Height);
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                    result._pixels[y * Width + Width - 1 - x] = _pixels[y * Width + x];
            }
            return result;
        }

        public PixelCanvas Clone()
        {
            var result = new PixelCanvas(Width, Height);
            Array.Copy(_pixels, result._pixels, _pixels.Length);
            return result;
        }

        public int CountVisible()
        {
            int count = 0;
            for (int i = 0; i < _pixels.Length; i++)
            {
                if (_pixels[i].A > 0)
                    count++;
            }
            return count;
        }

        private void Plot(int x, int y, Rgba color)
        {
            if (color.IsOpaque)
                Set(x, y, color);
            else
                Blend(x, y, color);
        }

        private bool IsVisible(bool[] visible, int x, int y) => Contains(x, y) && visible[y * Width + x];

        private static long Edge(long ax, long ay, long bx, long by, long px, long py) => (bx - ax) * (py - ay) - (by - ay) * (px - ax);
    }
}
