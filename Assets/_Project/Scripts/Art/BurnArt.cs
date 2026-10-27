using System;
using Cryptforge.Combat;

namespace Cryptforge.Art
{
    // The Cinder Arcanist's effects (docs/45), drawn from code like EffectArt: the flame a burning enemy carries, one size
    // for each Burn stack; the Crucible Vent's eruption column and the embers it throws up; and the ember that hops from a
    // fallen enemy to each neighbour its Ember Memory sets alight. Pure and deterministic, so every frame is tested without
    // Unity; CombatEffectsView turns them into sprites once at startup. Row 0 of a canvas is its bottom.
    //
    // Drawn at TexelsPerUnit, twice the placeholder grid: these sit on the painted enemies (128 texels a unit), where a
    // flame on the 32-texel grid read as a block of four-pixel squares over the body.
    public static class BurnArt
    {
        public const float TexelsPerUnit = 64f;

        // The Arcanist's ember at the heart of every flame, warm yellow where it burns hottest and crimson at its edges.
        // The soot outline is what keeps an orange flame apart from the Foundry's lava and its orange Cinder Mites.
        public static readonly Rgba Ember = Rgba.FromHex("#E77232");
        public static readonly Rgba Crimson = Rgba.FromHex("#B73E48");
        public static readonly Rgba Highlight = Rgba.FromHex("#FFC861");
        public static readonly Rgba WhiteHot = PixelPalette.EmberHot;
        public static readonly Rgba Soot = PixelPalette.SlagDark;

        public const int FlameFrameCount = 4;
        // Transparent texels between the cells of the flame sheet, so a frame drawn between whole pixels never samples
        // its neighbour.
        public const int SheetGutter = 1;
        public const int VentColumnWidth = 31;
        public const int VentColumnHeight = 68;
        public const int VentEmberWidth = 21;
        public const int VentEmberHeight = 29;
        public const int EmberArcWidth = 19;
        public const int EmberArcHeight = 7;
        // The arc ember's head, where the view pins the sprite, so the tail trails behind the point it flies through.
        public const int EmberArcHeadX = 14;

        // Frame by frame: which way the tip leans, and how much taller or shorter the flame stands.
        private static readonly int[] Sway = { -1, 0, 1, 0 };
        private static readonly int[] Flicker = { 0, 2, 0, -2 };
        // Heat above which a flame of 1-5 stacks turns warm yellow, and white-hot: more stacks burn hotter, so a full burn
        // reads across the floor and a single stack stays a small ember-orange lick.
        private static readonly float[] HighlightHeats = { .66f, .6f, .54f, .5f, .46f };
        private static readonly float[] CoreHeats = { 2f, 2f, .86f, .8f, .74f };

        // Half the flame's width at its widest, either side of its centre column.
        public static int FlameHalfWidth(int stacks)
        {
            CheckStacks(stacks);
            return 2 + (stacks + 1) / 2;
        }

        // Base to tip at rest; it grows three texels a stack.
        public static int FlameTongueHeight(int stacks)
        {
            CheckStacks(stacks);
            return 7 + 3 * stacks;
        }

        // A flame frame's canvas: the widest row, the licks beside it and an outline texel each side; the tallest flicker,
        // the outline over the tip and the rows for the spark that leaves it. The view pins it at its bottom centre.
        public static int FlameWidth(int stacks) => 2 * FlameHalfWidth(stacks) + 5;
        public static int FlameHeight(int stacks) => FlameTongueHeight(stacks) + 7;

        // A burning enemy's flame at this many stacks, one frame of its flicker: the tip sways left, stands up taller,
        // sways right and settles, and from two stacks a spark leaves it every other frame. From three stacks a lick dances
        // beside it, from five one on each side, and the top forks in two. The foot stands open, without an outline, on the
        // enemy's body.
        public static PixelCanvas FlameFrame(int stacks, int frame)
        {
            CheckStacks(stacks);
            if (frame < 0 || frame >= FlameFrameCount)
                throw new ArgumentOutOfRangeException(nameof(frame));

            var canvas = new PixelCanvas(FlameWidth(stacks), FlameHeight(stacks));
            int column = canvas.Width / 2;
            float centre = column + .5f;
            int half = FlameHalfWidth(stacks);
            float height = FlameTongueHeight(stacks) + Flicker[frame];
            float sway = Sway[frame] * (1f + stacks * .4f);
            float highlight = HighlightHeats[stacks - 1];
            float core = CoreHeats[stacks - 1];
            if (stacks >= 3)
            {
                // The lick rises on the side the tip leans away from: tall on the lean, low as the flame stands up.
                float side = frame < 2 ? 1f : -1f;
                Tongue(canvas, centre + side * (half - .5f), 0, height * (frame % 2 == 0 ? .56f : .44f), 1.5f, side * 3f,
                    .8f, highlight, core);
                if (stacks >= 5)
                    Tongue(canvas, centre - side * (half - .5f), 0, height * (frame % 2 == 0 ? .38f : .5f), 1.5f, -side * 3f,
                        .8f, highlight, core);
            }
            Tongue(canvas, centre, 0, height, half, sway, .9f, highlight, core);
            if (stacks >= 5)
                // A second, shorter tip beside the first, so a full burn forks rather than standing as one cone.
                Tongue(canvas, centre - (frame < 2 ? 1.5f : -1.5f), (int)(height * .35f), height * .5f, 2f, -sway * .5f,
                    .8f, highlight, core);
            canvas.Outline(Soot);
            if (stacks >= 2 && frame % 2 == 1)
            {
                // Clear of the outline over the tip, so it reads as a spark that has left, not a bump on the flame.
                int y = (int)height + (frame == 1 ? 3 : 5);
                Rgba spark = stacks >= 3 ? Highlight : Ember;
                int x = column + (frame == 1 ? 2 : -2);
                canvas.Set(x, y, spark);
                if (stacks >= 4)
                    canvas.Set(x, y + 1, spark);
            }
            return canvas;
        }

        // The four frames of one stack count's flicker.
        public static PixelCanvas[] FlameFrames(int stacks)
        {
            var frames = new PixelCanvas[FlameFrameCount];
            for (int frame = 0; frame < frames.Length; frame++)
                frames[frame] = FlameFrame(stacks, frame);
            return frames;
        }

        // Every flame frame on one sheet, so a crowd of burning enemies shares one texture and its flames batch: the row
        // of each stack count holds its frames left to right, the rows stacked from one stack at the bottom; FlameCell
        // gives each frame's corner.
        public static PixelCanvas FlameSheet()
        {
            var sheet = new PixelCanvas(FlameSheetWidth, FlameSheetHeight);
            for (int stacks = 1; stacks <= BurnRule.MaxStacks; stacks++)
            {
                for (int frame = 0; frame < FlameFrameCount; frame++)
                {
                    FlameCell(stacks, frame, out int x, out int y);
                    sheet.Blit(FlameFrame(stacks, frame), x, y);
                }
            }
            return sheet;
        }

        public static int FlameSheetWidth => FlameFrameCount * (FlameWidth(BurnRule.MaxStacks) + SheetGutter) - SheetGutter;

        public static int FlameSheetHeight
        {
            get
            {
                FlameCell(BurnRule.MaxStacks, 0, out _, out int y);
                return y + FlameHeight(BurnRule.MaxStacks);
            }
        }

        // The bottom-left corner of a flame frame on the sheet; its size is FlameWidth and FlameHeight of its stacks.
        public static void FlameCell(int stacks, int frame, out int x, out int y)
        {
            CheckStacks(stacks);
            if (frame < 0 || frame >= FlameFrameCount)
                throw new ArgumentOutOfRangeException(nameof(frame));
            x = frame * (FlameWidth(BurnRule.MaxStacks) + SheetGutter);
            y = 0;
            for (int below = 1; below < stacks; below++)
                y += FlameHeight(below) + SheetGutter;
        }

        // One Crucible Vent eruption, standing on the floor where the burning enemy stood: a wide white-hot flare at its
        // feet, a column of flame, the column breaking into rising tongues and embers, then the embers alone over a
        // guttering foot. The view scales it by the stacks the eruption spent.
        public static PixelCanvas[] VentColumnFrames()
        {
            float centre = VentColumnWidth / 2 + .5f;

            var flare = new PixelCanvas(VentColumnWidth, VentColumnHeight);
            Tongue(flare, centre - 8f, 0, 11f, 2.5f, -4f, .85f, .45f, .75f);
            Tongue(flare, centre + 8f, 0, 11f, 2.5f, 4f, .85f, .45f, .75f);
            Tongue(flare, centre, 0, 18f, 11f, 0f, .7f, .34f, .56f);
            flare.Outline(Soot);
            Motes(flare, new[] { 3, 26, 9, 21 }, new[] { 14, 15, 21, 22 }, 2, Highlight);

            var column = new PixelCanvas(VentColumnWidth, VentColumnHeight);
            Tongue(column, centre - 7f, 0, 20f, 3.5f, -3f, .85f, .45f, .75f);
            Tongue(column, centre + 7f, 0, 24f, 3.5f, 3f, .85f, .45f, .75f);
            Tongue(column, centre, 0, 60f, 7f, 0f, .45f, .26f, .5f);
            column.Outline(Soot);

            var breaking = new PixelCanvas(VentColumnWidth, VentColumnHeight);
            Tongue(breaking, centre, 0, 32f, 5.5f, 0f, .6f, .32f, .62f);
            Tongue(breaking, centre - 2f, 37, 15f, 4.5f, 2f, .8f, .42f, .75f);
            Tongue(breaking, centre + 2f, 54, 10f, 2.5f, -1f, .9f, .5f, 2f);
            Motes(breaking, new[] { 5, 24, 9, 22, 20 }, new[] { 40, 46, 57, 61, 33 }, 3, Highlight);
            breaking.Outline(Soot);
            Motes(breaking, new[] { 3, 27, 18, 8 }, new[] { 30, 34, 66, 50 }, 1, Ember);

            var embers = new PixelCanvas(VentColumnWidth, VentColumnHeight);
            Tongue(embers, centre, 0, 9f, 4.5f, 0f, .9f, 2f, 2f);
            Motes(embers, new[] { 4, 24, 11, 20, 15, 7 }, new[] { 47, 52, 60, 63, 37, 30 }, 3, Ember);
            embers.Outline(Soot);
            Motes(embers, new[] { 7, 26, 16, 2 }, new[] { 34, 40, 54, 58 }, 2, Crimson);
            return new[] { flare, column, breaking, embers };
        }

        // The embers an eruption throws up, which the view sends rising: hot as they leave, then cooling to crimson and
        // thinning out. Each frame lifts them two texels, so they flicker as they rise.
        public static PixelCanvas[] VentEmberFrames()
        {
            int[] xs = { 3, 12, 6, 16, 9, 1 };
            int[] ys = { 3, 10, 18, 2, 22, 13 };
            Rgba[] large = { Highlight, Ember, Crimson };
            Rgba[] small = { WhiteHot, Highlight, Ember };
            var frames = new PixelCanvas[large.Length];
            for (int frame = 0; frame < frames.Length; frame++)
            {
                var canvas = new PixelCanvas(VentEmberWidth, VentEmberHeight);
                // The large three keep a soot edge; the small ones are single sparks, and the last frame has lost one.
                for (int i = 0; i < 3; i++)
                    canvas.FillRect(xs[i], ys[i] + 2 * frame, 3, 3, large[frame]);
                canvas.Outline(Soot);
                for (int i = 3; i < (frame < 2 ? xs.Length : xs.Length - 1); i++)
                    canvas.FillRect(xs[i], ys[i] + 2 * frame, 2, 2, small[frame]);
                frames[frame] = canvas;
            }
            return frames;
        }

        // The ember Ember Memory passes on (BurnStatus.Spread): a white-hot head and a tail cooling from ember to crimson,
        // facing +x so the view can turn it along its flight. The tail flickers between a whole one and a broken one.
        public static PixelCanvas[] EmberArcFrames()
        {
            var whole = new PixelCanvas(EmberArcWidth, EmberArcHeight);
            ArcHead(whole);
            whole.FillRect(8, 3, 4, 1, Ember);
            whole.FillRect(9, 2, 3, 3, Ember);
            whole.FillRect(4, 3, 4, 1, Crimson);
            whole.Outline(Soot);

            var broken = new PixelCanvas(EmberArcWidth, EmberArcHeight);
            ArcHead(broken);
            broken.FillRect(7, 3, 5, 1, Ember);
            broken.FillRect(10, 2, 2, 3, Ember);
            broken.FillRect(5, 3, 1, 1, Crimson);
            broken.FillRect(2, 3, 2, 1, Crimson);
            broken.Outline(Soot);
            return new[] { whole, broken };
        }

        private static void ArcHead(PixelCanvas canvas)
        {
            canvas.FillRect(EmberArcHeadX - 2, 1, 4, 5, Highlight);
            canvas.FillRect(EmberArcHeadX - 3, 2, 6, 3, Highlight);
            canvas.FillRect(EmberArcHeadX - 1, 2, 2, 3, WhiteHot);
        }

        // One tongue of flame standing on baseY: rounded at its foot, widest a quarter of the way up, narrowing to a tip
        // that leans by sway texels; a taper under one keeps it wide higher up, as a column. Its outermost texel and its
        // tip are the crimson edge; inside, heat falls off toward its edges and its top and picks ember, warm yellow or
        // white-hot.
        private static void Tongue(PixelCanvas canvas, float centreX, int baseY, float height, float halfWidth, float sway,
            float taper, float highlightHeat, float coreHeat)
        {
            if (height < 1f)
                return;
            int top = baseY + (int)Math.Ceiling(height);
            for (int y = Math.Max(0, baseY); y < top && y < canvas.Height; y++)
            {
                float t = (y - baseY + .5f) / height;
                if (t > 1f)
                    break;
                float profile = t < .22f ? .72f + 1.27f * t : (float)Math.Pow((1f - t) / .78f, taper);
                float extent = halfWidth * profile + .5f;
                float middle = centreX + sway * t * t;
                for (int x = 0; x < canvas.Width; x++)
                {
                    float dx = Math.Abs(x + .5f - middle);
                    if (dx > extent)
                        continue;
                    float heat = (1f - dx / extent) * (1f - .8f * t);
                    Rgba tone;
                    if (dx > extent - 1f || t > .9f)
                        tone = Crimson;
                    else if (heat >= coreHeat)
                        tone = WhiteHot;
                    else if (heat >= highlightHeat)
                        tone = Highlight;
                    else
                        tone = Ember;
                    // A tongue drawn over another keeps the other's hotter heart: licks never paint crimson into a flame.
                    if (tone == Crimson && canvas.Get(x, y).A > 0)
                        continue;
                    canvas.Set(x, y, tone);
                }
            }
        }

        // Square motes of the given size at the given bottom-left corners.
        private static void Motes(PixelCanvas canvas, int[] xs, int[] ys, int size, Rgba color)
        {
            for (int i = 0; i < xs.Length; i++)
                canvas.FillRect(xs[i], ys[i], size, size, color);
        }

        private static void CheckStacks(int stacks)
        {
            if (stacks < 1 || stacks > BurnRule.MaxStacks)
                throw new ArgumentOutOfRangeException(nameof(stacks));
        }
    }
}
