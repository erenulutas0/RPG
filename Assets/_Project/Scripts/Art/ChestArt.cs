namespace Cryptforge.Art
{
    // The forge chest that waits on the floor: a dark iron-bound box with brass bands, closed or thrown open with a glow
    // inside. Pivot at the bottom centre, feet on row 0.
    public static class ChestArt
    {
        public const int Width = 24;
        public const int Height = 22;

        private static readonly Rgba Wood = Rgba.FromHex("#4A2E27");
        private static readonly Rgba WoodDark = Rgba.FromHex("#2E1A18");
        private static readonly Rgba WoodLight = Rgba.FromHex("#6B4433");

        public static PixelCanvas Draw(bool open)
        {
            var canvas = new PixelCanvas(Width, Height);
            // A flat shadow under the box.
            canvas.FillEllipse(Width / 2, 1, 10, 1, PixelPalette.Outline.WithAlpha(90));
            // The body: three planks with brass bands at the sides and a band across the middle.
            canvas.FillRect(2, 2, 20, 11, Wood);
            canvas.FillRect(2, 2, 20, 1, WoodDark);
            canvas.FillRect(2, 6, 20, 1, WoodDark);
            canvas.FillRect(2, 10, 20, 1, WoodDark);
            canvas.FillRect(3, 11, 18, 2, WoodLight);
            canvas.FillRect(2, 2, 2, 11, PixelPalette.Brass);
            canvas.FillRect(20, 2, 2, 11, PixelPalette.Brass);
            canvas.FillRect(2, 2, 2, 1, PixelPalette.BrassDark);
            canvas.FillRect(20, 2, 2, 1, PixelPalette.BrassDark);
            canvas.FillRect(10, 2, 4, 11, PixelPalette.Brass);
            canvas.FillRect(10, 2, 4, 1, PixelPalette.BrassDark);

            if (open)
            {
                // The lid stands up behind the box; the glow of what is inside spills over the rim.
                canvas.FillRect(3, 14, 18, 7, WoodDark);
                canvas.FillRect(3, 20, 18, 1, WoodLight);
                canvas.FillRect(3, 14, 2, 7, PixelPalette.Brass);
                canvas.FillRect(19, 14, 2, 7, PixelPalette.Brass);
                canvas.FillRect(4, 13, 16, 1, PixelPalette.CoinLight);
                canvas.FillRect(6, 12, 12, 1, PixelPalette.Coin);
                canvas.FillRect(8, 14, 8, 2, PixelPalette.CoinLight);
                canvas.Dither(6, 14, 12, 3, PixelPalette.CoinLight.WithAlpha(120));
            }
            else
            {
                // A rounded lid with a brass lock plate.
                canvas.FillRect(2, 13, 20, 4, Wood);
                canvas.FillRect(3, 17, 18, 1, WoodLight);
                canvas.FillRect(4, 18, 16, 1, WoodLight);
                canvas.FillRect(2, 13, 2, 4, PixelPalette.Brass);
                canvas.FillRect(20, 13, 2, 4, PixelPalette.Brass);
                canvas.FillRect(10, 13, 4, 4, PixelPalette.Brass);
                canvas.FillRect(11, 11, 2, 3, PixelPalette.BrassLight);
                canvas.FillRect(11, 12, 2, 1, PixelPalette.Outline);
            }
            canvas.Outline(PixelPalette.Outline);
            return canvas;
        }
    }
}
