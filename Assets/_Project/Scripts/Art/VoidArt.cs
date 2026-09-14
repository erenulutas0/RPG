using System;
using System.Collections.Generic;

namespace Cryptforge.Art
{
    // A rock spire standing on an island's plate: its centre column relative to the plate's centre, its width and height
    // in texels, and how many rows behind the plate's centre line its base sits (a higher rise reads as farther back).
    public readonly struct SpireSpec
    {
        public readonly int Offset;
        public readonly int Width;
        public readonly int Height;
        public readonly int Rise;

        public SpireSpec(int offset, int width, int height, int rise)
        {
            Offset = offset;
            Width = width;
            Height = height;
            Rise = rise;
        }
    }

    // A chain hanging from an island's underside: the island column it hangs from and its length in links.
    public readonly struct ChainSpec
    {
        public readonly int Column;
        public readonly int Links;

        public ChainSpec(int column, int links)
        {
            Column = column;
            Links = links;
        }
    }

    // How to draw one floating island: a hexagonal slab plate with masonry walls, stalactite blocks hanging under it,
    // spires standing on it and a lantern brazier on one spire (or on the plate when LanternSpire is -1). Sizes are
    // texels; the plate's front corner wall ends on row PlateBottom and the stalactites hang below it.
    public sealed class IslandSpec
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int PlateWidth { get; set; }
        public int PlateHalfHeight { get; set; }
        public int WallHeight { get; set; }
        public int PlateBottom { get; set; }
        public int StalactiteDepth { get; set; }
        public int Seed { get; set; }
        public int LanternSpire { get; set; } = -1;
        public int LanternOffset { get; set; }
        public SpireSpec[] Spires { get; set; } = Array.Empty<SpireSpec>();
    }

    // A drawn island and the anchors other sprites attach to: the lantern flame stands on texel row LanternY centred on
    // column LanternX, orbital rings circle the plate on row PlateRow, chains hang from the lowest visible row of their
    // column.
    public sealed class IslandArt
    {
        public PixelCanvas Canvas { get; }
        public int LanternX { get; }
        public int LanternY { get; }
        public int PlateRow { get; }

        public IslandArt(PixelCanvas canvas, int lanternX, int lanternY, int plateRow)
        {
            Canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
            LanternX = lanternX;
            LanternY = lanternY;
            PlateRow = plateRow;
        }

        // The lowest visible row in the column, or -1 when the column is empty.
        public int UndersideRow(int column)
        {
            for (int y = 0; y < Canvas.Height; y++)
            {
                if (Canvas.Get(column, y).A > 0)
                    return y;
            }
            return -1;
        }
    }

    // An island in the scene: the world position of its canvas's bottom-centre pivot, an optional orbital ring (texel
    // size; 0 = none) centred on the plate row, chains, and the phase of its slow drift.
    public sealed class IslandPlacement
    {
        public float X { get; }
        public float Y { get; }
        public IslandSpec Spec { get; }
        public int RingWidth { get; }
        public int RingHeight { get; }
        public ChainSpec[] Chains { get; }
        public float DriftPhase { get; }

        public bool HasRing => RingWidth > 0 && RingHeight > 0;

        public IslandPlacement(float x, float y, IslandSpec spec, int ringWidth, int ringHeight, ChainSpec[] chains, float driftPhase)
        {
            X = x;
            Y = y;
            Spec = spec ?? throw new ArgumentNullException(nameof(spec));
            RingWidth = ringWidth;
            RingHeight = ringHeight;
            Chains = chains ?? Array.Empty<ChainSpec>();
            DriftPhase = driftPhase;
        }

        // World height of the ring's centre line (the plate's centre row) for a drawn island.
        public float RingCentreY(IslandArt art) => Y + (art.PlateRow + 0.5f) / VoidArt.TexelsPerUnit;

        // World position of a chain's top-centre: it hangs from the lowest visible row of its column.
        public void ChainTop(IslandArt art, ChainSpec chain, out float worldX, out float worldY)
        {
            int row = Math.Max(0, art.UndersideRow(chain.Column));
            worldX = X + (chain.Column + 0.5f - Spec.Width / 2f) / VoidArt.TexelsPerUnit;
            worldY = Y + row / (float)VoidArt.TexelsPerUnit;
        }
    }

    // A drifting rock: world centre, size in texels and noise seed.
    public readonly struct RubblePlacement
    {
        public readonly float X;
        public readonly float Y;
        public readonly int Size;
        public readonly int Seed;

        public RubblePlacement(float x, float y, int size, int seed)
        {
            X = x;
            Y = y;
            Size = size;
            Seed = seed;
        }
    }

    // A four-point star: world centre and the phase of its twinkle.
    public readonly struct SparklePlacement
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Phase;

        public SparklePlacement(float x, float y, float phase)
        {
            X = x;
            Y = y;
            Phase = phase;
        }
    }

    // A nebula haze cloud: world centre, canvas size in haze texels (VoidArt.HazeTexelsPerUnit) and noise seed.
    public readonly struct HazePlacement
    {
        public readonly float X;
        public readonly float Y;
        public readonly int Width;
        public readonly int Height;
        public readonly int Seed;

        public HazePlacement(float x, float y, int width, int height, int seed)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Seed = seed;
        }
    }

    // The spiral galaxy: world centre, radius in texels and noise seed.
    public readonly struct GalaxyPlacement
    {
        public readonly float X;
        public readonly float Y;
        public readonly int Radius;
        public readonly int Seed;

        public GalaxyPlacement(float x, float y, int radius, int seed)
        {
            X = x;
            Y = y;
            Radius = radius;
            Seed = seed;
        }
    }

    // Everything the backdrop places around the platform, in world units. Built fresh by VoidLayout.Create so nothing
    // mutable lives in statics.
    public sealed class VoidScene
    {
        // The star field's world rectangle; it is drawn at VoidArt.TexelsPerUnit with its pivot at the top-centre.
        public const float StarFieldLeft = -12f;
        public const float StarFieldBottom = -13f;
        public const float StarFieldWidth = 24f;
        public const float StarFieldHeight = 27f;
        public const int StarFieldSeed = 1409;

        public IslandPlacement[] Islands { get; }
        public RubblePlacement[] Rubble { get; }
        public SparklePlacement[] Sparkles { get; }
        public HazePlacement[] Hazes { get; }
        public GalaxyPlacement Galaxy { get; }

        public VoidScene(IslandPlacement[] islands, RubblePlacement[] rubble, SparklePlacement[] sparkles, HazePlacement[] hazes, GalaxyPlacement galaxy)
        {
            Islands = islands ?? throw new ArgumentNullException(nameof(islands));
            Rubble = rubble ?? throw new ArgumentNullException(nameof(rubble));
            Sparkles = sparkles ?? throw new ArgumentNullException(nameof(sparkles));
            Hazes = hazes ?? throw new ArgumentNullException(nameof(hazes));
            Galaxy = galaxy;
        }
    }

    // Where the void's props sit. The camera follows the hero over a platform whose silhouette (top, faces and keel) is a
    // wide diamond from (0, WorldTop) through (±HalfWidth, WorldMiddle) to (0, WorldBottom - KeelDepth): for the scene's
    // 9-unit arena that is x ±9 and y from 4.5 down to -8. The void shows above the far corner, beyond the side corners and
    // below the keel: that is where the islands, rings and galaxy go, and where the hero sees them when walking to a
    // corner. Props never enter the diamond (see IsClearOfPlatform).
    public static class VoidLayout
    {
        // How far the platform's faces and keel reach below the top's near corner (the platform task: ~1.3 + 2.2).
        public const float KeelDepth = 3.5f;

        public static VoidScene Create()
        {
            var spireIsland = new IslandSpec
            {
                Width = 52, Height = 104, PlateWidth = 40, PlateHalfHeight = 6, WallHeight = 5, PlateBottom = 30,
                StalactiteDepth = 26, Seed = 7, LanternSpire = 1,
                Spires = new[] { new SpireSpec(-11, 7, 34, 2), new SpireSpec(1, 9, 52, 0), new SpireSpec(13, 6, 26, -3) }
            };
            var slabIsland = new IslandSpec
            {
                Width = 40, Height = 72, PlateWidth = 30, PlateHalfHeight = 5, WallHeight = 4, PlateBottom = 22,
                StalactiteDepth = 18, Seed = 12, LanternSpire = 0,
                Spires = new[] { new SpireSpec(5, 7, 28, 0), new SpireSpec(-8, 4, 12, 2) }
            };
            var ringIsland = new IslandSpec
            {
                Width = 56, Height = 88, PlateWidth = 42, PlateHalfHeight = 6, WallHeight = 5, PlateBottom = 34,
                StalactiteDepth = 30, Seed = 19, LanternSpire = 0,
                Spires = new[] { new SpireSpec(-3, 8, 36, 0), new SpireSpec(9, 5, 18, 3), new SpireSpec(-14, 5, 14, -2) }
            };
            var perchIsland = new IslandSpec
            {
                Width = 40, Height = 64, PlateWidth = 28, PlateHalfHeight = 4, WallHeight = 4, PlateBottom = 24,
                StalactiteDepth = 20, Seed = 23, LanternSpire = 0,
                Spires = new[] { new SpireSpec(4, 6, 22, 0), new SpireSpec(-7, 4, 10, 2) }
            };

            var islands = new[]
            {
                // Above the far corner, to the left: the tall ringed spire island.
                new IslandPlacement(-3.4f, 5.4f, spireIsland, 60, 24, new[] { new ChainSpec(8, 5), new ChainSpec(18, 3) }, 0f),
                // Above the far corner, to the right: a slab with one lantern spire under the galaxy.
                new IslandPlacement(3.6f, 5.2f, slabIsland, 0, 0, new[] { new ChainSpec(30, 4) }, 1.9f),
                // Beyond the left corner: the ring island with the tall lantern spire.
                new IslandPlacement(-11.1f, -1.4f, ringIsland, 60, 24, new[] { new ChainSpec(40, 4), new ChainSpec(14, 6) }, 3.7f),
                // Beyond the right corner: a small perch with its own ring.
                new IslandPlacement(11.2f, -1.9f, perchIsland, 44, 18, new[] { new ChainSpec(8, 3) }, 5.2f)
            };
            var rubble = new[]
            {
                new RubblePlacement(-5.6f, -9.6f, 10, 1),
                new RubblePlacement(4.7f, -9.1f, 8, 2),
                new RubblePlacement(9.9f, 3.9f, 9, 3),
                new RubblePlacement(-2.4f, 8.1f, 8, 4),
                new RubblePlacement(1.1f, 7.7f, 7, 5),
                new RubblePlacement(6.4f, 6.9f, 9, 6),
                new RubblePlacement(-10.3f, -4.6f, 8, 7),
                new RubblePlacement(10.1f, -4.9f, 10, 8)
            };
            var sparkles = new[]
            {
                new SparklePlacement(-1.2f, 7.6f, 0f),
                new SparklePlacement(10.4f, 1.2f, 0.9f),
                new SparklePlacement(-9.8f, -6.0f, 1.7f),
                new SparklePlacement(1.4f, -9.3f, 2.6f),
                new SparklePlacement(-10.2f, 2.6f, 3.3f),
                new SparklePlacement(3.5f, 6.4f, 4.1f),
                new SparklePlacement(-6.4f, 6.2f, 4.9f),
                new SparklePlacement(6.0f, -9.8f, 5.6f),
                new SparklePlacement(9.9f, -5.4f, 6.4f)
            };
            var hazes = new[]
            {
                new HazePlacement(-7.0f, 7.0f, 48, 32, 21),
                new HazePlacement(-9.6f, -5.5f, 44, 36, 22),
                new HazePlacement(9.5f, -3.0f, 36, 44, 23)
            };
            return new VoidScene(islands, rubble, sparkles, hazes, new GalaxyPlacement(7.8f, 7.4f, 34, 11));
        }

        // True when a world point lies outside the platform's silhouette grown by the margin: the top rhombus plus the
        // faces and keel below it, taken together as a diamond from the far corner to the keel's point.
        public static bool IsClearOfPlatform(ArenaGeometry geometry, float worldX, float worldY, float margin)
        {
            if (margin < 0f || float.IsNaN(margin))
                throw new ArgumentOutOfRangeException(nameof(margin));

            float top = geometry.WorldTop + margin;
            float middle = geometry.WorldMiddle;
            float bottom = geometry.WorldBottom - KeelDepth - margin;
            float halfWidth = geometry.HalfWidth + margin;
            if (worldY >= top || worldY <= bottom)
                return true;
            float rowHalfWidth = worldY >= middle
                ? halfWidth * (top - worldY) / (top - middle)
                : halfWidth * (worldY - bottom) / (middle - bottom);
            return Math.Abs(worldX) >= rowHalfWidth;
        }
    }

    // Draws the astral void's props: floating rock islands, their orbital rings, chains and rubble, lantern flames,
    // star sparkles, star-field layers, nebula haze and the spiral galaxy. Everything is deterministic (PixelNoise) and
    // engine-free; VoidBackdropView turns the canvases into sprites.
    public static class VoidArt
    {
        public const int TexelsPerUnit = 32;
        // Haze clouds are drawn coarser so their dither reads as distant nebula rather than as noise.
        public const int HazeTexelsPerUnit = 16;
        public const int ChainWidth = 3;
        public const int ChainLinkPeriod = 6;
        public const int SparkleSize = 9;
        public const int LanternWidth = 13;
        public const int LanternHeight = 11;
        public const int LanternFrames = 2;
        public const int StarCell = 12;
        // Four texels: an outline on each rim, one row of dark side face and one lit brass-and-stone top row, so the
        // ring stays the thin dark halo of the mockup rather than a slab.
        public const int RingBand = 4;

        private static readonly Rgba Outline = PixelPalette.Outline;
        private static readonly Rgba RockLight = PixelPalette.RockLight;
        private static readonly Rgba Rock = PixelPalette.Rock;
        private static readonly Rgba RockDark = PixelPalette.RockDark;
        private static readonly Rgba EmberHot = PixelPalette.EmberHot;
        private static readonly Rgba EmberLight = PixelPalette.EmberLight;
        private static readonly Rgba Ember = PixelPalette.Ember;
        private static readonly Rgba EmberDark = PixelPalette.EmberDark;
        private static readonly Rgba Brass = PixelPalette.Brass;
        private static readonly Rgba BrassDark = PixelPalette.BrassDark;
        private static readonly Rgba Chain = PixelPalette.Chain;
        private static readonly Rgba ChainLight = PixelPalette.ChainLight;
        private static readonly Rgba StarWhite = PixelPalette.StarWhite;
        private static readonly Rgba StarLilac = PixelPalette.StarLilac;
        private static readonly Rgba StarDim = PixelPalette.StarDim;
        private static readonly Rgba NebulaGlow = PixelPalette.NebulaGlow;
        private static readonly Rgba NebulaViolet = PixelPalette.NebulaViolet;
        private static readonly Rgba NebulaHaze = PixelPalette.NebulaViolet.WithAlpha(140);
        private static readonly Rgba HazeThin = PixelPalette.NebulaViolet.WithAlpha(72);
        private static readonly Rgba HazeThick = PixelPalette.NebulaGlow.WithAlpha(100);

        // 4x4 Bayer matrix: the ordered dither that turns smooth intensity into flat tones.
        private static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        private static readonly string[] SparkleBright =
        {
            "....D....",
            "....L....",
            "....L....",
            "...DWD...",
            "DLLWWWLLD",
            "...DWD...",
            "....L....",
            "....L....",
            "....D...."
        };

        private static readonly string[] SparkleDim =
        {
            ".........",
            "....D....",
            "....D....",
            "....L....",
            ".DDLWLDD.",
            "....L....",
            "....D....",
            "....D....",
            "........."
        };

        private static readonly string[] FlameTall =
        {
            "...L...",
            "...L...",
            "..LLL..",
            "..LHL..",
            ".LLHLL.",
            ".LHHHL.",
            ".LLHLL.",
            "..EEE.."
        };

        private static readonly string[] FlameLean =
        {
            ".......",
            "..L....",
            "..LL...",
            "..LHL..",
            ".LLHLL.",
            ".LHHHL.",
            ".LLHLL.",
            "..EEE.."
        };

        // A floating island: stalactites, walls, slab plate, spires (back to front), brazier, then a hard outline around
        // the whole silhouette.
        public static IslandArt DrawIsland(IslandSpec spec)
        {
            if (spec == null)
                throw new ArgumentNullException(nameof(spec));
            if (spec.Width < 8 || spec.Height < 8 || spec.PlateWidth < 6 || spec.PlateWidth > spec.Width - 2)
                throw new ArgumentOutOfRangeException(nameof(spec), "The island needs a plate narrower than its canvas.");
            if (spec.PlateHalfHeight < 2 || spec.WallHeight < 1 || spec.PlateBottom < 1 || spec.StalactiteDepth < 0)
                throw new ArgumentOutOfRangeException(nameof(spec), "The plate, wall and stalactite sizes must be positive.");

            var canvas = new PixelCanvas(spec.Width, spec.Height);
            int cx = spec.Width / 2;
            int halfPlate = spec.PlateWidth / 2;
            int ph = spec.PlateHalfHeight;
            int plateCentre = spec.PlateBottom + spec.WallHeight + ph;

            // The plate's top face: a hexagon of stone slabs, full width around its centre line, tapering to the far and
            // near corners. plateLower remembers the face's lowest row per column so the walls follow its front edges.
            var plateLower = new int[spec.Width];
            for (int x = 0; x < plateLower.Length; x++)
                plateLower[x] = -1;
            for (int dy = -ph; dy <= ph; dy++)
            {
                int hw = HexagonHalfWidth(dy, ph, halfPlate);
                if (hw <= 0)
                    continue;
                int row = plateCentre + dy;
                int band = (dy + ph) / 3;
                for (int x = cx - hw; x < cx + hw; x++)
                {
                    bool groove = (x - cx + halfPlate + band * 3) % 6 == 5 || (dy + ph) % 3 == 2 && dy < ph - 1;
                    bool grain = !groove && PixelNoise.Chance(x, row, spec.Seed, 0.05f);
                    canvas.Set(x, row, groove || grain ? Rock : RockLight);
                    if (plateLower[x] < 0 || row < plateLower[x])
                        plateLower[x] = row;
                }
            }
            // The front rim is a darker edge with ember glints where the forge light catches the slabs.
            for (int x = cx - halfPlate; x < cx + halfPlate; x++)
            {
                if (plateLower[x] < 0)
                    continue;
                bool glint = (x - cx + halfPlate + PixelNoise.Pick(0, 0, spec.Seed, 6)) % 7 < 2;
                canvas.Set(x, plateLower[x], glint ? EmberDark : Rock);
            }

            DrawWalls(canvas, spec, cx, plateLower);
            DrawStalactites(canvas, spec, cx, halfPlate, plateLower);

            // Spires from the back of the plate to its front so nearer ones overlap farther ones.
            var order = new int[spec.Spires.Length];
            for (int i = 0; i < order.Length; i++)
                order[i] = i;
            Array.Sort(order, (a, b) => spec.Spires[b].Rise.CompareTo(spec.Spires[a].Rise));
            var capTops = new int[spec.Spires.Length];
            foreach (int i in order)
                capTops[i] = DrawSpire(canvas, spec.Spires[i], cx, plateCentre, spec.Seed, i, i == spec.LanternSpire);

            int lanternX;
            int lanternTop;
            if (spec.LanternSpire >= 0 && spec.LanternSpire < spec.Spires.Length)
            {
                SpireSpec lanternSpire = spec.Spires[spec.LanternSpire];
                lanternX = cx + lanternSpire.Offset - lanternSpire.Width / 2 + CapCentreOffset(lanternSpire.Width);
                lanternTop = capTops[spec.LanternSpire];
            }
            else
            {
                lanternX = cx + spec.LanternOffset;
                lanternTop = plateCentre;
            }
            // The brazier: a brass bowl the flame sprite stands on.
            canvas.FillRect(lanternX - 2, lanternTop + 1, 5, 1, BrassDark);
            canvas.FillRect(lanternX - 1, lanternTop + 2, 3, 1, Brass);
            canvas.Set(lanternX - 2, lanternTop + 2, Outline);
            canvas.Set(lanternX + 2, lanternTop + 2, Outline);

            canvas.Outline(Outline);
            return new IslandArt(canvas, lanternX, lanternTop + 3, plateCentre);
        }

        // One half of an orbital ring of stone blocks: the front half (rows below the centre line) draws over the island,
        // the back half behind it. Both are drawn on a canvas of the full ring size so they share one pivot.
        public static PixelCanvas DrawRing(int width, int height, bool front)
        {
            if (width < 16)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 8 || height > width)
                throw new ArgumentOutOfRangeException(nameof(height));

            var canvas = new PixelCanvas(width, height);
            int half = height / 2;
            int yMin = front ? 0 : half;
            int yMax = front ? half : height;
            // Blocks about nine texels long around the ellipse's parameter, so seams read as masonry without darkening
            // the band.
            int segments = Math.Max(6, (int)Math.Round((width + height) * 1.5 / 9.0));
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!OnRing(x, y, width, height))
                        continue;
                    bool edge = !OnRing(x - 1, y, width, height) || !OnRing(x + 1, y, width, height) ||
                                !OnRing(x, y - 1, width, height) || !OnRing(x, y + 1, width, height);
                    int segment = RingSegment(x, y, width, height, segments);
                    bool seam = OnRing(x + 1, y, width, height) && RingSegment(x + 1, y, width, height, segments) != segment ||
                                OnRing(x, y - 1, width, height) && RingSegment(x, y - 1, width, height, segments) != segment;
                    Rgba color;
                    if (edge || seam)
                        color = Outline;
                    else if (front)
                    {
                        // The row above the outer (lower) edge is the ring's side face; the rest its lit top.
                        bool side = !OnRing(x, y - 2, width, height);
                        bool innerEdge = !OnRing(x, y + 2, width, height);
                        if (side)
                            color = Rock;
                        else if (innerEdge && PixelNoise.Chance(segment, 1, 7, 0.5f))
                            color = BrassDark;
                        else
                            color = RockLight;
                    }
                    else
                    {
                        bool farRim = !OnRing(x, y + 2, width, height);
                        color = farRim ? RockDark : Rock;
                    }
                    canvas.Set(x, y, color);
                }
            }
            // Ember glints in a few seams of the front half, like the lit joints of the mockup's rings.
            if (front)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    for (int y = 1; y < half - 1; y++)
                    {
                        if (canvas.Get(x, y) != Outline || !IsRingTop(canvas.Get(x - 1, y)) || !IsRingTop(canvas.Get(x + 1, y)))
                            continue;
                        if (PixelNoise.Chance(x, y, 31, 0.4f))
                            canvas.Set(x, y, Ember);
                    }
                }
            }
            return canvas;
        }

        // A chain of upright links joined by edge-on ones, hanging from the top of the canvas.
        public static PixelCanvas DrawChain(int links)
        {
            if (links < 1 || links > 64)
                throw new ArgumentOutOfRangeException(nameof(links));

            var canvas = new PixelCanvas(ChainWidth, links * ChainLinkPeriod - 1);
            for (int link = 0; link < links; link++)
            {
                int top = canvas.Height - 1 - link * ChainLinkPeriod;
                canvas.Set(1, top, ChainLight);
                for (int y = top - 3; y < top; y++)
                {
                    canvas.Set(0, y, y == top - 3 ? Chain : ChainLight);
                    canvas.Set(2, y, Outline);
                }
                canvas.Set(1, top - 4, Outline);
                if (link < links - 1)
                    canvas.Set(1, top - 5, Chain);
            }
            return canvas;
        }

        // A small drifting rock: a lit top, two faces and a jagged bottom.
        public static PixelCanvas DrawRubble(int size, int seed)
        {
            if (size < 6 || size > 16)
                throw new ArgumentOutOfRangeException(nameof(size));

            var canvas = new PixelCanvas(size, size);
            int hw = size / 2 - 1;
            int cx = size / 2;
            int halfH = Math.Max(1, hw / 2);
            int topRow = size - 2 - halfH;
            canvas.FillRect(cx - hw, 2, hw, topRow - 2, Rock);
            canvas.FillRect(cx, 2, hw, topRow - 2, RockDark);
            canvas.FillRect(cx - hw + 1, 1, hw - 1, 1, Rock);
            canvas.FillRect(cx, 1, hw - 1, 1, RockDark);
            canvas.FillTriangle(cx - hw, topRow, cx + hw - 1, topRow, cx, topRow + halfH, RockLight);
            canvas.FillTriangle(cx - hw, topRow, cx + hw - 1, topRow, cx, topRow - halfH, RockLight);
            if (PixelNoise.Chance(size, 0, seed, 0.5f))
                canvas.Set(cx + 1, 2 + PixelNoise.Pick(size, 1, seed, Math.Max(1, topRow - 3)), EmberDark);
            canvas.Outline(Outline);
            return canvas;
        }

        // A lantern flame with a dithered ember halo; its base sits on the bottom row, centred. Frame 0 stands tall,
        // frame 1 leans, so swapping them flickers.
        public static PixelCanvas DrawLantern(int frame)
        {
            if (frame < 0 || frame >= LanternFrames)
                throw new ArgumentOutOfRangeException(nameof(frame));

            var canvas = new PixelCanvas(LanternWidth, LanternHeight);
            const int centreX = 6;
            const int centreY = 4;
            float reach = frame == 0 ? 6.5f : 5.5f;
            for (int y = 0; y < LanternHeight; y++)
            {
                for (int x = 0; x < LanternWidth; x++)
                {
                    float nx = (x - centreX) / reach;
                    float ny = (y - centreY) / reach;
                    float d = nx * nx + ny * ny;
                    bool even = ((x + y) & 1) == 0;
                    if (d <= 0.4f && !even)
                        canvas.Set(x, y, Ember.WithAlpha(80));
                    else if (d <= 1f && even)
                        canvas.Set(x, y, EmberDark.WithAlpha(96));
                }
            }
            canvas.BlitMap(frame == 0 ? FlameTall : FlameLean, EmberTones(), 3, 0);
            return canvas;
        }

        // A four-point star; the dim frame is the twinkle's other half.
        public static PixelCanvas DrawSparkle(bool bright)
        {
            var canvas = new PixelCanvas(SparkleSize, SparkleSize);
            canvas.BlitMap(bright ? SparkleBright : SparkleDim, StarTones(), 0, 0);
            return canvas;
        }

        // Sparse single-texel stars, one per grid cell at most; each cell belongs to one of two layers so the layers
        // can twinkle against each other.
        public static PixelCanvas DrawStarField(int width, int height, int seed, int layer)
        {
            if (width < StarCell || height < StarCell)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (layer < 0 || layer > 1)
                throw new ArgumentOutOfRangeException(nameof(layer));

            var canvas = new PixelCanvas(width, height);
            for (int cellY = 0; cellY * StarCell < height; cellY++)
            {
                for (int cellX = 0; cellX * StarCell < width; cellX++)
                {
                    if (PixelNoise.Pick(cellX, cellY, seed, 2) != layer || !PixelNoise.Chance(cellX, cellY, seed + 1, 0.55f))
                        continue;
                    int x = cellX * StarCell + 1 + PixelNoise.Pick(cellX, cellY, seed + 2, StarCell - 2);
                    int y = cellY * StarCell + 1 + PixelNoise.Pick(cellX, cellY, seed + 3, StarCell - 2);
                    float tone = PixelNoise.Value(cellX, cellY, seed + 4);
                    Rgba color = tone < 0.6f ? StarDim : tone < 0.9f ? StarLilac : StarWhite;
                    canvas.Set(x, y, color);
                    if (tone >= 0.6f && PixelNoise.Chance(cellX, cellY, seed + 5, 0.2f))
                    {
                        canvas.Set(x - 1, y, StarDim);
                        canvas.Set(x + 1, y, StarDim);
                        canvas.Set(x, y - 1, StarDim);
                        canvas.Set(x, y + 1, StarDim);
                    }
                }
            }
            return canvas;
        }

        // A two-armed spiral galaxy, flattened, quantised to four nebula tones with an ordered dither.
        public static PixelCanvas DrawGalaxy(int radius, int seed)
        {
            if (radius < 8 || radius > 120)
                throw new ArgumentOutOfRangeException(nameof(radius));

            int size = radius * 2 + 1;
            var canvas = new PixelCanvas(size, size);
            const double twist = 3.4;
            double phase = PixelNoise.Value(0, 0, seed) * Math.PI;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    double dx = x - radius;
                    double dy = (y - radius) / 0.78;
                    double rn = Math.Sqrt(dx * dx + dy * dy) / radius;
                    if (rn > 1.0)
                        continue;
                    double angle = Math.Atan2(dy, dx);
                    // Arms follow a logarithmic spiral; cubing narrows them so the gaps between turns stay dark.
                    double arm = 0.5 + 0.5 * Math.Cos(2.0 * angle - twist * Math.Log(rn + 0.06) + phase);
                    double arms = 1.25 * arm * arm * arm * (1.0 - rn);
                    double disc = 0.25 * (1.0 - rn) * (1.0 - rn);
                    double core = 1.2 * Math.Exp(-(rn * rn) / 0.014);
                    double intensity = Math.Min(1.0, arms + disc + core);
                    double bayer = Bayer[(y & 3) * 4 + (x & 3)] / 16.0;
                    int level = (int)Math.Floor(intensity * 4.0 + bayer);
                    if (level <= 0)
                        continue;
                    Rgba color = level >= 4 ? StarLilac : level == 3 ? NebulaGlow : level == 2 ? NebulaViolet : NebulaHaze;
                    canvas.Set(x, y, color);
                    if (level >= 2 && PixelNoise.Chance(x, y, seed + 7, 0.012f))
                        canvas.Set(x, y, StarWhite);
                }
            }
            return canvas;
        }

        // A soft nebula cloud: a few overlapping blobs quantised to two translucent violet tones with an ordered dither.
        public static PixelCanvas DrawHaze(int width, int height, int seed)
        {
            if (width < 8 || width > 256)
                throw new ArgumentOutOfRangeException(nameof(width));
            if (height < 8 || height > 256)
                throw new ArgumentOutOfRangeException(nameof(height));

            var canvas = new PixelCanvas(width, height);
            const int blobs = 5;
            var blobX = new double[blobs];
            var blobY = new double[blobs];
            var blobR = new double[blobs];
            // Blobs stay inside the canvas so the cloud's edge is its own soft rim, never a clipped straight line.
            for (int i = 0; i < blobs; i++)
            {
                blobX[i] = width * (0.35 + 0.3 * PixelNoise.Value(i, 0, seed));
                blobY[i] = height * (0.35 + 0.3 * PixelNoise.Value(i, 1, seed));
                blobR[i] = Math.Min(width, height) * (0.22 + 0.1 * PixelNoise.Value(i, 2, seed));
            }
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double intensity = 0.0;
                    for (int i = 0; i < blobs; i++)
                    {
                        double dx = (x - blobX[i]) / blobR[i];
                        double dy = (y - blobY[i]) / blobR[i];
                        double d = dx * dx + dy * dy;
                        if (d < 1.0)
                            intensity += (1.0 - d) * (1.0 - d) * 0.55;
                    }
                    double bayer = Bayer[(y & 3) * 4 + (x & 3)] / 16.0;
                    int level = (int)Math.Floor(Math.Min(1.0, intensity) * 2.0 + bayer);
                    if (level <= 0)
                        continue;
                    canvas.Set(x, y, level >= 2 ? HazeThick : HazeThin);
                }
            }
            return canvas;
        }

        private static int HexagonHalfWidth(int dy, int ph, int halfPlate)
        {
            int a = Math.Abs(dy);
            int flat = ph / 2;
            if (a <= flat)
                return halfPlate;
            return (int)Math.Round(halfPlate * (ph - a) / (double)(ph - flat));
        }

        // Masonry walls under the plate's front edges: courses of blocks that follow the slanted edge, two faces meeting
        // under the front corner, with ember glints in a few joints.
        private static void DrawWalls(PixelCanvas canvas, IslandSpec spec, int cx, int[] plateLower)
        {
            for (int x = 0; x < spec.Width; x++)
            {
                if (plateLower[x] < 0)
                    continue;
                for (int y = plateLower[x] - spec.WallHeight; y < plateLower[x]; y++)
                {
                    int depth = plateLower[x] - 1 - y;
                    int course = depth / 4;
                    bool mortar = depth % 4 == 3 || (x + course * 3) % 6 == 0;
                    Rgba tone;
                    if (mortar)
                        tone = depth % 4 == 3 && (x + course * 3) % 6 == 0 && PixelNoise.Chance(x, y, spec.Seed + 3, 0.3f) ? EmberDark : Outline;
                    else
                        tone = x < cx ? Rock : RockDark;
                    canvas.Set(x, y, tone);
                }
            }
        }

        // Stalactite blocks hanging under the walls: lit on the left, dark on the right, jagged tips, ember seams.
        private static void DrawStalactites(PixelCanvas canvas, IslandSpec spec, int cx, int halfPlate, int[] plateLower)
        {
            int x = cx - halfPlate + 1;
            int index = 0;
            while (x < cx + halfPlate - 3)
            {
                int width = 3 + PixelNoise.Pick(index, 0, spec.Seed, 3);
                if (x + width > cx + halfPlate - 1)
                    width = cx + halfPlate - 1 - x;
                if (width < 2)
                    break;
                int centre = x + width / 2;
                float envelope = 1f - Math.Abs(centre - cx) / (float)halfPlate;
                int top = plateLower[centre] - spec.WallHeight;
                int length = (int)Math.Round(spec.StalactiteDepth * (0.3f + 0.7f * envelope) * (0.7f + 0.3f * PixelNoise.Value(index, 1, spec.Seed)));
                length = Math.Min(Math.Max(3, length), top - 1);
                int tip = top - length;
                for (int y = tip; y < top; y++)
                {
                    int rowWidth = y == tip ? Math.Max(1, width - 2) : y == tip + 1 && width >= 4 ? width - 1 : width;
                    int left = x + (width - rowWidth) / 2;
                    int dark = Math.Max(1, rowWidth / 3);
                    canvas.FillRect(left, y, rowWidth - dark, 1, Rock);
                    canvas.FillRect(left + rowWidth - dark, y, dark, 1, (y - tip) % 5 == 4 ? Outline : RockDark);
                    if (rowWidth >= 3 && y > tip + length / 3)
                        canvas.Set(left, y, RockLight);
                }
                if (length >= 6 && PixelNoise.Chance(index, 2, spec.Seed, 0.45f))
                {
                    int seam = 3 + PixelNoise.Pick(index, 3, spec.Seed, 3);
                    int seamTop = top - 2;
                    canvas.FillRect(centre, seamTop - seam + 1, 1, seam, EmberDark);
                    canvas.Set(centre, seamTop - seam / 2, Ember);
                }
                x += width + 1;
                index++;
            }
        }

        // Draws one spire and returns the row of its cap's peak. Wider spires lose their right corner (a crumbled top)
        // and get an outlined corner between the lit and dark faces; the lantern spire carries a long glowing seam.
        private static int DrawSpire(PixelCanvas canvas, SpireSpec spire, int cx, int plateCentre, int seed, int index, bool lanternSpire)
        {
            if (spire.Width < 3 || spire.Height < 4)
                throw new ArgumentOutOfRangeException(nameof(spire), "A spire is at least 3 wide and 4 tall.");
            int w = spire.Width;
            int h = spire.Height;
            int x0 = cx + spire.Offset - w / 2;
            int baseRow = plateCentre + spire.Rise;
            int leftWidth = (w + 1) / 2;
            int rightX = x0 + leftWidth;
            int chip = w >= 6 ? 2 : 0;
            const int chipDepth = 3;
            int mainWidth = w - chip;

            for (int c = 0; c < w; c++)
            {
                int columnHeight = c >= w - chip ? h - chipDepth : h;
                Rgba tone = c < leftWidth ? Rock : RockDark;
                if (c == 0)
                    tone = RockLight;
                else if (c == leftWidth && w >= 6)
                    tone = Outline;
                canvas.FillRect(x0 + c, baseRow, 1, columnHeight, tone);
            }
            if (chip > 0)
                canvas.FillRect(x0 + w - chip, baseRow + h - chipDepth, chip, 1, Rock);

            if (h >= 10)
            {
                int step = 6 + (index & 1);
                for (int y = baseRow + 2 + PixelNoise.Pick(index, 0, seed, step); y < baseRow + h - 3; y += step)
                {
                    canvas.FillRect(x0 + 1, y, leftWidth - 1, 1, RockDark);
                    canvas.FillRect(rightX, y, w - leftWidth, 1, Outline);
                }
            }

            int crackColumn = w >= 6 ? rightX : x0 + leftWidth - 1;
            if (lanternSpire && h >= 16)
            {
                int seam = h * 6 / 10;
                canvas.FillRect(crackColumn, baseRow + 1, 1, seam, EmberDark);
                canvas.Set(crackColumn, baseRow + seam / 3, Ember);
                canvas.Set(crackColumn, baseRow + seam / 2, EmberLight);
                canvas.Set(crackColumn, baseRow + seam * 2 / 3, Ember);
            }
            else if (h >= 8 && PixelNoise.Chance(index, 1, seed, 0.65f))
            {
                int seam = 3 + PixelNoise.Pick(index, 2, seed, 4);
                int seamBottom = baseRow + 2 + PixelNoise.Pick(index, 3, seed, Math.Max(1, h - seam - 4));
                canvas.FillRect(crackColumn, seamBottom, 1, seam, EmberDark);
                canvas.Set(crackColumn, seamBottom + seam / 2, Ember);
                canvas.Set(crackColumn - 1, seamBottom + seam / 2 + 1, EmberDark);
            }
            if (h >= 14 && leftWidth >= 3 && PixelNoise.Chance(index, 4, seed, 0.6f))
            {
                int windowRow = baseRow + (int)(h * (0.35f + 0.35f * PixelNoise.Value(index, 5, seed)));
                canvas.FillRect(x0 + 1, windowRow, 2, 2, EmberLight);
                canvas.Set(x0 + 1, windowRow + 1, EmberHot);
            }

            int capLeft = x0;
            int capRight = x0 + mainWidth - 1;
            int capCentre = x0 + CapCentreOffset(w);
            int capRow = baseRow + h;
            int halfH = Math.Max(1, mainWidth / 4);
            canvas.FillTriangle(capLeft, capRow, capRight, capRow, capCentre, capRow + halfH, RockLight);
            canvas.FillTriangle(capLeft, capRow, capRight, capRow, capCentre, capRow - halfH, RockLight);
            return capRow + halfH;
        }

        // The cap sits on the columns left of the crumbled corner; its centre is the lantern's column.
        private static int CapCentreOffset(int width) => (width - (width >= 6 ? 2 : 0) - 1) / 2;

        private static bool IsRingTop(Rgba color) => color == RockLight || color == BrassDark;

        private static bool OnRing(int x, int y, int width, int height)
        {
            double cx = (width - 1) / 2.0;
            double cy = (height - 1) / 2.0;
            double rx = width / 2.0 - 1.0;
            double ry = height / 2.0 - 1.0;
            double dx = x - cx;
            double dy = y - cy;
            bool insideOuter = dx * dx / (rx * rx) + dy * dy / (ry * ry) <= 1.0;
            double ix = rx - RingBand;
            double iy = ry - RingBand;
            bool insideInner = iy > 0 && dx * dx / (ix * ix) + dy * dy / (iy * iy) <= 1.0;
            return insideOuter && !insideInner;
        }

        private static int RingSegment(int x, int y, int width, int height, int segments)
        {
            double cx = (width - 1) / 2.0;
            double cy = (height - 1) / 2.0;
            double rx = width / 2.0 - 1.0;
            double ry = height / 2.0 - 1.0;
            double angle = Math.Atan2((y - cy) / ry, (x - cx) / rx);
            int segment = (int)Math.Floor((angle + Math.PI) / (2.0 * Math.PI) * segments);
            return Math.Min(segments - 1, Math.Max(0, segment));
        }

        private static Dictionary<char, Rgba> EmberTones() => new Dictionary<char, Rgba>
        {
            ['H'] = EmberHot,
            ['L'] = EmberLight,
            ['E'] = Ember,
            ['D'] = EmberDark
        };

        private static Dictionary<char, Rgba> StarTones() => new Dictionary<char, Rgba>
        {
            ['W'] = StarWhite,
            ['L'] = StarLilac,
            ['D'] = StarDim
        };
    }
}
