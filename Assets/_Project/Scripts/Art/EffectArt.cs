using System;
using System.Collections.Generic;
using Cryptforge.Combat;

namespace Cryptforge.Art
{
    // Which strike effect a hero weapon shows at the enemy it struck.
    public enum StrikeEffect
    {
        SwordSlash,
        StaffBlast,
        DaggerSlash
    }

    // Combat effect placeholders drawn from code in the astral foundry palette: the hero's strike effects, hit sparks, crit
    // bursts, death smoke, coins and the 3x5 numerals of floating damage numbers. Pure and deterministic so every frame can
    // be tested without Unity; CombatEffectsView turns them into sprites once at startup. Row 0 of a canvas is its bottom.
    public static class EffectArt
    {
        public const int DigitWidth = 3;
        public const int DigitHeight = 5;
        public const int DigitGap = 1;
        // Left edges of consecutive numerals are this far apart: the glyph plus its gap.
        public const int DigitStride = DigitWidth + DigitGap;
        // The most digits a damage number shows; larger hits are capped at all nines.
        public const int MaxDigits = 5;
        // Crit numbers are drawn at this integer scale of the digit grid.
        public const int CritNumberScale = 2;
        public const int SlashSize = 32;
        public const int RingWidth = 80;
        public const int RingHeight = 40;
        // The horizontal radius, in texels, of the blast ring at full size; the view scales the sprite to the splash.
        public const int RingFullRadius = 38;
        public const int DoubleSlashSize = 24;
        public const int StarSize = 24;
        public const int SparkSize = 16;
        public const int SmokeSize = 32;
        public const int CoinSize = 8;

        // Smoke tones: a lit maroon over the slag tones, so a puff still reads on the violet-grey tiles.
        public static readonly Rgba SmokeLight = Rgba.FromHex("#5C3C4C");
        public static readonly Rgba Smoke = PixelPalette.Slag;
        public static readonly Rgba SmokeDark = PixelPalette.SlagDark;

        private static readonly string[][] Digits =
        {
            new[] { "###", "#.#", "#.#", "#.#", "###" },
            new[] { ".#.", "##.", ".#.", ".#.", "###" },
            new[] { "###", "..#", "###", "#..", "###" },
            new[] { "###", "..#", "###", "..#", "###" },
            new[] { "#.#", "#.#", "###", "..#", "..#" },
            new[] { "###", "#..", "###", "..#", "###" },
            new[] { "###", "#..", "###", "#.#", "###" },
            new[] { "###", "..#", "..#", "..#", "..#" },
            new[] { "###", "#.#", "###", "#.#", "###" },
            new[] { "###", "#.#", "###", "..#", "###" }
        };

        // A compact ember burst, then the same sparks flung outward with the core gone.
        private static readonly string[][] SparkMaps =
        {
            new[]
            {
                "................",
                "................",
                ".......LL.......",
                ".......LL.......",
                "....E..LL..E....",
                ".....E.LL.E.....",
                ".......HH.......",
                "..LLLLHHHHLLLL..",
                "..LLLLHHHHLLLL..",
                ".......HH.......",
                ".....E.LL.E.....",
                "....E..LL..E....",
                ".......LL.......",
                ".......LL.......",
                "................",
                "................"
            },
            new[]
            {
                "................",
                ".......LL.......",
                ".......L........",
                "..E.........E...",
                "...E.......E....",
                "................",
                "......E..E......",
                ".LL..........LL.",
                ".LL..........LL.",
                "......E..E......",
                "................",
                "...E.......E....",
                "..E.........E...",
                "........L.......",
                ".......LL.......",
                "................"
            }
        };

        // A coin seen face on with a brass emblem, then edge on for the spin.
        private static readonly string[][] CoinMaps =
        {
            new[]
            {
                "..oooo..",
                ".oLLLCo.",
                "oLLCCCCo",
                "oLCCBCCo",
                "oLCBBBCo",
                "oCCCBCCo",
                ".oCCCCo.",
                "..oooo.."
            },
            new[]
            {
                ".oo.",
                "oLCo",
                "oLCo",
                "oLCo",
                "oLCo",
                "oLCo",
                "oLCo",
                ".oo."
            }
        };

        // The Sword cleaves, the Staff blasts an area and the Daggers strike one target.
        public static StrikeEffect StrikeFor(WeaponBehavior behavior)
        {
            switch (behavior)
            {
                case WeaponBehavior.Cleave:
                    return StrikeEffect.SwordSlash;
                case WeaponBehavior.Area:
                    return StrikeEffect.StaffBlast;
                default:
                    return StrikeEffect.DaggerSlash;
            }
        }

        // The 3x5 pixel map of a numeral, top row first.
        public static IReadOnlyList<string> DigitMap(int digit)
        {
            if (digit < 0 || digit > 9)
                throw new ArgumentOutOfRangeException(nameof(digit));
            return Digits[digit];
        }

        // The number of decimal digits a damage value shows, at least one and at most MaxDigits.
        public static int DigitCount(int value)
        {
            int count = 1;
            for (int rest = Math.Abs(value) / 10; rest > 0 && count < MaxDigits; rest /= 10)
                count++;
            return count;
        }

        // The digit at the given place of a value: 0 for the units, 1 for the tens. Values beyond MaxDigits show all nines.
        public static int DigitAt(int value, int place)
        {
            if (place < 0 || place >= MaxDigits)
                throw new ArgumentOutOfRangeException(nameof(place));
            int capped = Math.Min(Math.Abs(value), (int)Math.Pow(10, MaxDigits) - 1);
            for (int i = 0; i < place; i++)
                capped /= 10;
            return capped % 10;
        }

        // The texel width of a number of the given digit count with one-texel gaps between the digits.
        public static int NumberWidth(int digitCount)
        {
            if (digitCount < 1)
                throw new ArgumentOutOfRangeException(nameof(digitCount));
            return digitCount * DigitWidth + (digitCount - 1) * DigitGap;
        }

        // A whole number as one row of 3x5 numerals with one-texel gaps, without any outline.
        public static PixelCanvas ComposeNumber(int value, Rgba color)
        {
            int count = DigitCount(value);
            var canvas = new PixelCanvas(NumberWidth(count), DigitHeight);
            var palette = new Dictionary<char, Rgba> { ['#'] = color };
            for (int i = 0; i < count; i++)
            {
                int digit = DigitAt(value, count - 1 - i);
                canvas.BlitMap(Digits[digit], palette, i * (DigitWidth + DigitGap), 0);
            }
            return canvas;
        }

        // One numeral with a one-texel dark outline, 5x7 texels at scale 1; crit numbers use scale 2. Numerals placed
        // DigitStride texels apart overlap only their outlines, so a number can be composed from these at runtime.
        public static PixelCanvas DigitCanvas(int digit, Rgba color, int scale = 1)
        {
            var canvas = new PixelCanvas(DigitWidth + 2, DigitHeight + 2);
            canvas.BlitMap(DigitMap(digit), new Dictionary<char, Rgba> { ['#'] = color }, 1, 1);
            canvas.Outline(PixelPalette.Outline);
            return scale == 1 ? canvas : Upscale(canvas, scale);
        }

        // A whole floating damage number: white numerals for a hit, larger gold ones for a crit, with a one-texel dark
        // outline so they read over the tiles.
        public static PixelCanvas NumberCanvas(int value, bool critical)
        {
            PixelCanvas digits = ComposeNumber(value, critical ? PixelPalette.CoinLight : Rgba.White);
            var canvas = new PixelCanvas(digits.Width + 2, digits.Height + 2);
            canvas.Blit(digits, 1, 1);
            canvas.Outline(PixelPalette.Outline);
            return critical ? Upscale(canvas, CritNumberScale) : canvas;
        }

        // The sword's crescent slash: a short thick arc as the blade arrives, the full sweep, then the sweep thinning and
        // breaking up. The arc opens toward the hero (lower left) and bulges past the enemy's upper right.
        public static PixelCanvas[] SlashFrames() =>
            new[]
            {
                Crescent(0.55f, 3f, false),
                Crescent(1f, 3.5f, false),
                Crescent(1f, 2.6f, true)
            };

        // The staff's blast ring on the floor: an isometric ellipse (half as tall as wide) that expands from the target,
        // shows its four cardinal diamonds and an inner dashed ring at full size, then breaks up.
        public static PixelCanvas[] RingFrames() =>
            new[]
            {
                Ring(24, 12, false, false),
                Ring(RingFullRadius, RingFullRadius / 2, true, false),
                Ring(RingFullRadius, RingFullRadius / 2, true, true)
            };

        // The daggers' double slash: the first cut, the crossed pair, then both fading.
        public static PixelCanvas[] DoubleSlashFrames()
        {
            var first = new PixelCanvas(DoubleSlashSize, DoubleSlashSize);
            TaperedLine(first, 3f, 20f, 21f, 4f, 1.8f, 0.7f, false);
            first.Outline(PixelPalette.Spell);

            var cross = new PixelCanvas(DoubleSlashSize, DoubleSlashSize);
            TaperedLine(cross, 3f, 20f, 21f, 4f, 1.8f, 1f, false);
            TaperedLine(cross, 3f, 4f, 21f, 20f, 1.8f, 1f, false);
            cross.Outline(PixelPalette.Spell);

            var fade = new PixelCanvas(DoubleSlashSize, DoubleSlashSize);
            TaperedLine(fade, 3f, 20f, 21f, 4f, 1.4f, 1f, true);
            TaperedLine(fade, 3f, 4f, 21f, 20f, 1.4f, 1f, true);
            fade.Outline(PixelPalette.Spell);
            return new[] { first, cross, fade };
        }

        // The crit's gold star: a tight four-point star with a white core, then a wider hollow eight-point burst.
        public static PixelCanvas[] StarBurstFrames()
        {
            var tight = new PixelCanvas(StarSize, StarSize);
            Spikes(tight, 1, 8, 2, PixelPalette.CoinLight, true);
            Spikes(tight, 1, 4, 1, PixelPalette.CoinLight, false);
            Core(tight, 2, Rgba.White);
            tight.Outline(PixelPalette.Coin);

            var wide = new PixelCanvas(StarSize, StarSize);
            Spikes(wide, 4, 10, 1, PixelPalette.CoinLight, true);
            Spikes(wide, 3, 6, 1, PixelPalette.CoinLight, false);
            DiamondRing(wide, 3, PixelPalette.CoinLight);
            wide.Outline(PixelPalette.Coin);
            return new[] { tight, wide };
        }

        // The ember spark of any hit on an enemy.
        public static PixelCanvas[] SparkFrames()
        {
            var palette = new Dictionary<char, Rgba>
            {
                ['H'] = PixelPalette.EmberHot,
                ['L'] = PixelPalette.EmberLight,
                ['E'] = PixelPalette.Ember
            };
            var frames = new PixelCanvas[SparkMaps.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = new PixelCanvas(SparkSize, SparkSize);
                frames[i].BlitMap(SparkMaps[i], palette, 0, 0);
            }
            return frames;
        }

        // A dying enemy's smoke: a compact dark puff with a hot core, the puff swelling and rising, then thinning into wisps
        // with the embers cooling.
        public static PixelCanvas[] SmokeFrames()
        {
            var compact = new PixelCanvas(SmokeSize, SmokeSize);
            Puff(compact, 10, 9, 5, false);
            Puff(compact, 22, 9, 5, false);
            Puff(compact, 16, 11, 7, false);
            Puff(compact, 16, 17, 5, false);
            compact.Outline(PixelPalette.Outline);
            Embers(compact, new[] { 15, 16, 15, 16 }, new[] { 11, 11, 12, 12 }, PixelPalette.EmberHot);
            Embers(compact, new[] { 11, 20, 13, 18, 16 }, new[] { 8, 9, 15, 15, 20 }, PixelPalette.EmberLight);

            var swelling = new PixelCanvas(SmokeSize, SmokeSize);
            Puff(swelling, 9, 12, 7, false);
            Puff(swelling, 23, 12, 7, false);
            Puff(swelling, 16, 16, 8, false);
            Puff(swelling, 12, 22, 5, false);
            Puff(swelling, 20, 23, 5, false);
            swelling.Outline(PixelPalette.Outline);
            Embers(swelling, new[] { 16, 17 }, new[] { 16, 16 }, PixelPalette.EmberHot);
            Embers(swelling, new[] { 8, 24, 13, 20, 16, 11, 21 }, new[] { 14, 13, 24, 25, 9, 19, 19 }, PixelPalette.EmberLight);

            var wisps = new PixelCanvas(SmokeSize, SmokeSize);
            Puff(wisps, 7, 17, 5, true);
            Puff(wisps, 25, 17, 5, true);
            Puff(wisps, 16, 24, 6, true);
            Puff(wisps, 12, 10, 4, true);
            Puff(wisps, 21, 10, 4, true);
            wisps.Outline(PixelPalette.Outline);
            Embers(wisps, new[] { 4, 28, 16, 10, 22 }, new[] { 20, 20, 29, 6, 6 }, PixelPalette.Ember);
            return new[] { compact, swelling, wisps };
        }

        // A coin face on and edge on; alternating them spins the coin as it pops.
        public static PixelCanvas[] CoinFrames()
        {
            var palette = new Dictionary<char, Rgba>
            {
                ['o'] = PixelPalette.Outline,
                ['L'] = PixelPalette.CoinLight,
                ['C'] = PixelPalette.Coin,
                ['B'] = PixelPalette.Brass
            };
            var frames = new PixelCanvas[CoinMaps.Length];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = new PixelCanvas(CoinMaps[i][0].Length, CoinMaps[i].Length);
                frames[i].BlitMap(CoinMaps[i], palette, 0, 0);
            }
            return frames;
        }

        // Nearest-neighbour integer upscale, so a larger crit numeral keeps the pixel grid.
        public static PixelCanvas Upscale(PixelCanvas source, int factor)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (factor < 1)
                throw new ArgumentOutOfRangeException(nameof(factor));

            var result = new PixelCanvas(source.Width * factor, source.Height * factor);
            for (int y = 0; y < source.Height; y++)
            {
                for (int x = 0; x < source.Width; x++)
                    result.FillRect(x * factor, y * factor, factor, factor, source.Get(x, y));
            }
            return result;
        }

        // An arc of the slash circle: the sweep runs clockwise from 115 degrees (upper left) to -55 degrees (lower right),
        // covering the given fraction of it, and tapers to a point at both ends of what it covers.
        private static PixelCanvas Crescent(float covered, float halfThickness, bool fading)
        {
            const float sweepStart = 115f;
            const float sweepEnd = -55f;
            const float radius = 11f;
            var canvas = new PixelCanvas(SlashSize, SlashSize);
            float centre = SlashSize / 2f;
            for (int y = 0; y < SlashSize; y++)
            {
                for (int x = 0; x < SlashSize; x++)
                {
                    float dx = x + 0.5f - centre;
                    float dy = y + 0.5f - centre;
                    float t = (sweepStart - AngleDegrees(dx, dy)) / (sweepStart - sweepEnd);
                    if (t < 0f || t > covered)
                        continue;

                    float along = t / covered;
                    float thickness = Math.Max(0.5f, halfThickness * (float)Math.Sqrt(Math.Sin(Math.PI * along)));
                    float distance = Math.Abs(Distance(dx, dy) - radius);
                    if (distance > thickness)
                        continue;
                    if (fading && distance > thickness * 0.5f && ((x + y) & 1) == 1)
                        continue;

                    bool core = distance <= thickness * 0.4f && along > 0.15f && along < 0.85f;
                    canvas.Set(x, y, core ? Rgba.White : PixelPalette.SteelLight);
                }
            }
            canvas.Outline(PixelPalette.Spell);

            // Motes trailing the start of the sweep once it is under way.
            if (covered >= 1f)
            {
                canvas.Set(5, 26, PixelPalette.SpellLight);
                canvas.Set(3, 23, PixelPalette.SpellLight);
                canvas.Set(26, 5, PixelPalette.SpellLight);
            }
            return canvas;
        }

        // A floor ring: a light one-texel ellipse line with a dark outer edge; at full size, cardinal diamonds and a dashed
        // inner ring; when fading, the line breaks into a checker.
        private static PixelCanvas Ring(int radiusX, int radiusY, bool full, bool fading)
        {
            var canvas = new PixelCanvas(RingWidth, RingHeight);
            int centreX = RingWidth / 2;
            int centreY = RingHeight / 2;
            EllipseLine(canvas, centreX, centreY, radiusX, radiusY, fading ? PixelPalette.Spell : PixelPalette.SpellLight,
                fading ? PixelPalette.SteelDark : PixelPalette.Spell, false, fading);
            if (!full)
                return canvas;

            EllipseLine(canvas, centreX, centreY, (int)(radiusX * 0.72f), (int)(radiusY * 0.72f), PixelPalette.Spell,
                Rgba.Transparent, true, fading);
            Rgba edge = fading ? PixelPalette.Spell : PixelPalette.SpellLight;
            Rgba inner = fading ? PixelPalette.SteelDark : PixelPalette.Spell;
            Diamond(canvas, centreX, centreY + radiusY, edge, inner);
            Diamond(canvas, centreX, centreY - radiusY, edge, inner);
            Diamond(canvas, centreX - radiusX, centreY, edge, inner);
            Diamond(canvas, centreX + radiusX, centreY, edge, inner);
            return canvas;
        }

        // The one-texel boundary of an ellipse in the line colour, with the texels just outside in the edge colour. Dashed
        // lines keep alternating 15-degree segments; a fading line keeps a checker of its texels.
        private static void EllipseLine(PixelCanvas canvas, int centreX, int centreY, int radiusX, int radiusY, Rgba line,
            Rgba edge, bool dashed, bool fading)
        {
            var inside = new bool[canvas.Width * canvas.Height];
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    float nx = (x - centreX) / (radiusX + 0.5f);
                    float ny = (y - centreY) / (radiusY + 0.5f);
                    inside[y * canvas.Width + x] = nx * nx + ny * ny <= 1f;
                }
            }

            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    bool here = inside[y * canvas.Width + x];
                    bool neighbourDiffers = Differs(inside, canvas.Width, canvas.Height, x - 1, y, here) ||
                        Differs(inside, canvas.Width, canvas.Height, x + 1, y, here) ||
                        Differs(inside, canvas.Width, canvas.Height, x, y - 1, here) ||
                        Differs(inside, canvas.Width, canvas.Height, x, y + 1, here);
                    if (!neighbourDiffers)
                        continue;
                    if (dashed && ((int)((AngleDegrees(x - centreX, y - centreY) + 180f) / 15f) & 1) == 1)
                        continue;
                    if (fading && ((x + y) & 1) == 1)
                        continue;
                    canvas.Blend(x, y, here ? line : edge);
                }
            }
        }

        private static bool Differs(bool[] inside, int width, int height, int x, int y, bool from) =>
            x >= 0 && y >= 0 && x < width && y < height && inside[y * width + x] != from;

        // A small diamond marker: a light rim, a dark interior and a light centre texel.
        private static void Diamond(PixelCanvas canvas, int centreX, int centreY, Rgba rim, Rgba inner)
        {
            for (int dy = -2; dy <= 2; dy++)
            {
                for (int dx = -2; dx <= 2; dx++)
                {
                    int ring = Math.Abs(dx) + Math.Abs(dy);
                    if (ring > 2)
                        continue;
                    canvas.Set(centreX + dx, centreY + dy, ring == 2 || ring == 0 ? rim : inner);
                }
            }
        }

        // A straight cut from (x0, y0) to (x1, y1) that tapers to a point at both ends of the covered part, with a white core
        // through its middle. Fading keeps only a checker of the outer texels.
        private static void TaperedLine(PixelCanvas canvas, float x0, float y0, float x1, float y1, float halfThickness,
            float covered, bool fading)
        {
            float lengthX = x1 - x0;
            float lengthY = y1 - y0;
            float lengthSquared = lengthX * lengthX + lengthY * lengthY;
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    float px = x + 0.5f - x0;
                    float py = y + 0.5f - y0;
                    float u = (px * lengthX + py * lengthY) / lengthSquared;
                    if (u < 0f || u > covered)
                        continue;

                    float along = u / covered;
                    float thickness = Math.Max(0.5f, halfThickness * (float)Math.Sqrt(Math.Sin(Math.PI * along)));
                    float distance = Distance(px - u * lengthX, py - u * lengthY);
                    if (distance > thickness)
                        continue;
                    if (fading && distance > thickness * 0.5f && ((x + y) & 1) == 1)
                        continue;

                    bool core = distance <= thickness * 0.4f && along > 0.25f && along < 0.75f;
                    canvas.Blend(x, y, core ? Rgba.White : PixelPalette.SteelLight);
                }
            }
        }

        // Rays from the star's centre: the four cardinal or the four diagonal ones, from one distance to another. Cardinal
        // rays are two texels wide near the centre and one toward the tip.
        private static void Spikes(PixelCanvas canvas, int from, int to, int width, Rgba color, bool cardinal)
        {
            int centre = StarSize / 2;
            for (int distance = from; distance <= to; distance++)
            {
                int wide = width == 2 && distance <= to / 2 ? 1 : 0;
                if (cardinal)
                {
                    for (int side = -wide; side <= 0; side++)
                    {
                        canvas.Set(centre + side, centre + distance, color);
                        canvas.Set(centre + side, centre - 1 - distance, color);
                        canvas.Set(centre + distance, centre + side, color);
                        canvas.Set(centre - 1 - distance, centre + side, color);
                    }
                    continue;
                }

                canvas.Set(centre + distance, centre + distance, color);
                canvas.Set(centre - 1 - distance, centre + distance, color);
                canvas.Set(centre + distance, centre - 1 - distance, color);
                canvas.Set(centre - 1 - distance, centre - 1 - distance, color);
            }
        }

        // A square core of the given half size around the star's centre.
        private static void Core(PixelCanvas canvas, int half, Rgba color)
        {
            int centre = StarSize / 2;
            canvas.FillRect(centre - half, centre - half, half * 2, half * 2, color);
        }

        // A hollow diamond around the star's centre at the given Manhattan radius.
        private static void DiamondRing(PixelCanvas canvas, int radius, Rgba color)
        {
            int centre = StarSize / 2;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Math.Abs(dx) + Math.Abs(dy) != radius)
                        continue;
                    int x = dx < 0 ? centre - 1 + dx + 1 : centre + dx;
                    int y = dy < 0 ? centre - 1 + dy + 1 : centre + dy;
                    canvas.Set(x, y, color);
                }
            }
        }

        // One rounded smoke cloud lit from the upper left; wisps keep only a checker of their outer texels.
        private static void Puff(PixelCanvas canvas, int centreX, int centreY, int radius, bool wisp)
        {
            float extent = radius + 0.5f;
            for (int y = centreY - radius; y <= centreY + radius; y++)
            {
                for (int x = centreX - radius; x <= centreX + radius; x++)
                {
                    float dx = x - centreX;
                    float dy = y - centreY;
                    float depth = Distance(dx, dy) / extent;
                    if (depth > 1f)
                        continue;
                    if (wisp && depth > 0.55f && ((x + y) & 1) == 1)
                        continue;

                    Rgba tone = Smoke;
                    if (dy - dx > radius * 0.55f && depth < 0.9f)
                        tone = SmokeLight;
                    else if (dx - dy > radius * 0.4f && depth > 0.5f)
                        tone = SmokeDark;
                    canvas.Set(x, y, tone);
                }
            }
        }

        // Two-texel embers over the smoke at the given positions.
        private static void Embers(PixelCanvas canvas, int[] xs, int[] ys, Rgba color)
        {
            for (int i = 0; i < xs.Length; i++)
                canvas.FillRect(xs[i], ys[i], 2, 2, color);
        }

        private static float Distance(float x, float y) => (float)Math.Sqrt(x * x + y * y);

        // Degrees counter-clockwise from +x, in (-180, 180].
        private static float AngleDegrees(float x, float y) => (float)(Math.Atan2(y, x) * 180.0 / Math.PI);
    }
}
