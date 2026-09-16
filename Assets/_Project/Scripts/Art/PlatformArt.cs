using System;
using System.Collections.Generic;

namespace Cryptforge.Art
{
    // Where the forge platform's parts land on its pixel canvas: the top rhombus, the near faces hanging below it, the
    // keel narrowing to the crystal, and the world position of the canvas so the sprite sits exactly on the arena floor.
    // Texel column CentreX is centred on world x = 0; row 0 starts at world y = WorldBottom.
    public readonly struct PlatformLayout
    {
        public const int TexelsPerUnit = 32;
        // Spare texels around the silhouette for the one-texel outline.
        public const int Margin = 2;
        // How far the near faces hang below the top, and the keel below them, in world units.
        public const float FaceDepth = 1.3f;
        public const float KeelDepth = 2.2f;
        // Room under the keel tip for the crystal's glow and the chain ends.
        public const float UnderHang = 0.5f;

        public readonly int Width;
        public readonly int Height;
        public readonly int CentreX;
        public readonly int HalfWidth;
        public readonly int HalfDepth;
        public readonly int NearY;
        public readonly int MiddleY;
        public readonly int FarY;
        public readonly int FaceHeight;
        public readonly int KeelHeight;
        public readonly int TipY;
        // World y of the bottom edge of row 0; the canvas is centred on world x = 0.
        public readonly float WorldBottom;

        public PlatformLayout(ArenaGeometry geometry)
        {
            HalfWidth = Round(geometry.HalfWidth * TexelsPerUnit);
            HalfDepth = Math.Max(1, Round((geometry.WorldMiddle - geometry.WorldBottom) * TexelsPerUnit));
            FaceHeight = Round(FaceDepth * TexelsPerUnit);
            KeelHeight = Round(KeelDepth * TexelsPerUnit);
            TipY = Round(UnderHang * TexelsPerUnit);
            NearY = TipY + KeelHeight + FaceHeight;
            MiddleY = NearY + HalfDepth;
            FarY = MiddleY + HalfDepth;
            CentreX = HalfWidth + Margin;
            Width = 2 * CentreX + 1;
            Height = FarY + 1 + Margin;
            WorldBottom = geometry.WorldBottom - (float)NearY / TexelsPerUnit;
        }

        // Texel column whose centre is nearest the world x; row containing the world y.
        public int TexelX(float worldX) => CentreX + (int)Math.Floor(worldX * TexelsPerUnit + 0.5f + 0.001f);
        public int TexelY(float worldY) => (int)Math.Floor((worldY - WorldBottom) * TexelsPerUnit + 0.001f);

        // The texel of a point on the top by rhombus coordinates (see ArenaGeometry.TopPoint).
        public void TopTexel(float s, float t, out int x, out int y)
        {
            x = CentreX + Round((s - t) * HalfWidth);
            y = NearY + Round((s + t) * HalfDepth);
        }

        public bool IsOnTop(int x, int y) => IsInRhombus(x, y, HalfWidth, HalfDepth);

        public bool IsInRhombus(int x, int y, int halfWidth, int halfDepth) =>
            Math.Abs(x - CentreX) * halfDepth + Math.Abs(y - MiddleY) * halfWidth <= halfWidth * halfDepth;

        // The row of the top's near edge above a column: the near corner at the centre, rising to the side corners.
        public int NearEdgeY(int x) => NearY + (int)Math.Round(HalfDepth * (double)Math.Abs(x - CentreX) / HalfWidth);

        // The row of the keel's lower edge below a column: the tip at the centre, rising to the faces' bottom corners.
        public int KeelEdgeY(int x) =>
            TipY + (int)Math.Round((MiddleY - FaceHeight - TipY) * (double)Math.Abs(x - CentreX) / HalfWidth);

        private static int Round(float value) => (int)Math.Floor(value + 0.5f);
    }

    // Draws the forge platform as pixel art: a tiled top with brass rim, diagonals and emblem, corner towers with
    // lanterns, stacked stone faces with lava seams, a keel that narrows to a burning crystal, and hanging chains.
    // DrawPlatform is the static sprite; DrawLights gives the animated overlay frames (flames, seam glow, crystal core).
    public static class PlatformArt
    {
        // Local floor palette: quiet enough beneath moving silhouettes without darkening actors or the rim.
        public static readonly Rgba FloorLight = new Rgba(47, 45, 61);
        public static readonly Rgba FloorStone = new Rgba(43, 42, 57);
        public static readonly Rgba FloorDark = new Rgba(40, 39, 53);
        public static readonly Rgba FloorGrout = new Rgba(34, 33, 46);
        public static readonly Rgba FloorInlay = new Rgba(79, 66, 53);
        public static readonly Rgba FloorBevel = new Rgba(53, 51, 65);
        public static readonly Rgba RimStone = new Rgba(55, 51, 61);
        public static readonly Rgba RimBevel = new Rgba(75, 66, 68);
        public const int FrameCount = 2;
        public const int TilesPerEdge = 16;

        private const int Seed = 2207;
        private const int CourseHeight = 7;
        private const int BlockWidth = 12;
        private const int KeelCourseHeight = 10;
        private const int KeelBlockWidth = 16;
        private const int CrystalHalfWidth = 13;
        private const int CrystalHeight = 42;
        private const int TowerWidth = 17;
        private const int TowerHeight = 23;
        private const int TowerCentre = 8;
        private const int LanternBottom = 13;
        private const int FlameBottom = 20;

        private static readonly float[] SeamPositions = { 0.3f, 0.58f, 0.84f };
        private static readonly float[] ChainPositions = { 0.34f, 0.62f };

        private static readonly string[] FlameA =
        {
            "..L..",
            ".LL..",
            ".LHL.",
            "LLHLL",
            "LHHHL",
            ".LEL.",
        };

        private static readonly string[] FlameB =
        {
            "..L..",
            "..LL.",
            ".LHL.",
            ".LHLL",
            "LHHHL",
            ".LEL.",
        };

        private static readonly Dictionary<char, Rgba> FlamePalette = new Dictionary<char, Rgba>
        {
            { 'L', PixelPalette.EmberLight },
            { 'H', PixelPalette.EmberHot },
            { 'E', PixelPalette.Ember },
        };

        public static PixelCanvas DrawPlatform(ArenaGeometry geometry) => DrawPlatform(new PlatformLayout(geometry));

        public static PixelCanvas DrawPlatform(PlatformLayout layout)
        {
            var canvas = new PixelCanvas(layout.Width, layout.Height);
            DrawKeel(canvas, layout);
            DrawFaces(canvas, layout);
            DrawChains(canvas, layout);
            DrawCrystal(canvas, layout);
            DrawTop(canvas, layout);
            canvas.Outline(PixelPalette.Outline);
            // The towers carry their own outline and sit over the rim, so they come after the silhouette's outline.
            DrawTowers(canvas, layout);
            return canvas;
        }

        public static PixelCanvas DrawLights(ArenaGeometry geometry, int frame) => DrawLights(new PlatformLayout(geometry), frame);

        // One frame of the animated overlay: the same size as the platform so the two sprites share a transform.
        public static PixelCanvas DrawLights(PlatformLayout layout, int frame)
        {
            if (frame < 0 || frame >= FrameCount)
                throw new ArgumentOutOfRangeException(nameof(frame));
            var canvas = new PixelCanvas(layout.Width, layout.Height);
            DrawCrystalGlow(canvas, layout, frame);
            DrawSeamGlow(canvas, layout, frame);
            DrawFlames(canvas, layout, frame);
            return canvas;
        }

        // Where each corner tower's canvas lands (bottom-left texel): left, right and near corners; the far corner stays
        // plain because it sits under the top HUD.
        public static void TowerOrigins(PlatformLayout layout, out int leftX, out int leftY, out int rightX, out int rightY, out int nearX, out int nearY)
        {
            leftX = layout.CentreX - layout.HalfWidth + 2;
            leftY = layout.MiddleY - 6;
            rightX = layout.CentreX + layout.HalfWidth - 2 - (TowerWidth - 1);
            rightY = layout.MiddleY - 6;
            nearX = layout.CentreX - TowerCentre;
            nearY = layout.NearY + 3;
        }

        // The top: two-tone tiles with grout, darker in the far half, a brass rim, brass diagonals and the emblem.
        private static void DrawTop(PixelCanvas canvas, PlatformLayout layout)
        {
            int cx = layout.CentreX;
            int hw = layout.HalfWidth;
            int hd = layout.HalfDepth;
            for (int y = layout.NearY; y <= layout.FarY; y++)
            {
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    if (!layout.IsOnTop(x, y))
                        continue;
                    float along = (y - layout.NearY) / (float)hd;
                    float across = (x - cx) / (float)hw;
                    float tileS = (along + across) * 0.5f * TilesPerEdge;
                    float tileT = (along - across) * 0.5f * TilesPerEdge;
                    int i = Clamp((int)tileS, 0, TilesPerEdge - 1);
                    int j = Clamp((int)tileT, 0, TilesPerEdge - 1);
                    bool far = i + j >= TilesPerEdge;
                    bool light = PixelNoise.Pick(i, j, Seed + 4, 2) == 0;
                    Rgba tone = far ? (light ? FloorStone : FloorDark) : (light ? FloorLight : FloorStone);
                    float u = tileS - i;
                    float v = tileT - j;
                    // Broad mineral patches, not screen-space noise: their edges stay inside individual slabs.
                    int patch = PixelNoise.Pick(i, j, Seed + 5, 4);
                    if (u > .18f && u < .78f && v > .22f && v < .76f &&
                        u + v * .45f > .42f + patch * .11f)
                        tone = tone == FloorLight ? FloorStone : FloorDark;
                    // A narrow upper bevel and a darker lower lip give the slab thickness without bright grid lines.
                    float bevel = TilesPerEdge / (float)hd * .55f;
                    if ((u > 1f - bevel || v > 1f - bevel) && u < .99f && v < .99f)
                        tone = FloorBevel;
                    else if (u < bevel || v < bevel)
                        tone = FloorGrout;
                    canvas.Set(x, y, tone);
                }
            }

            for (int k = 1; k < TilesPerEdge; k++)
            {
                float f = k / (float)TilesPerEdge;
                layout.TopTexel(f, 0f, out int x0, out int y0);
                layout.TopTexel(f, 1f, out int x1, out int y1);
                canvas.Line(x0, y0, x1, y1, FloorGrout);
                layout.TopTexel(0f, f, out x0, out y0);
                layout.TopTexel(1f, f, out x1, out y1);
                canvas.Line(x0, y0, x1, y1, FloorGrout);
            }

            // A few short, low-contrast chips break the perfect grid; never draw glowing cracks in walkable stone.
            for (int i = 1; i < TilesPerEdge - 1; i++)
            {
                for (int j = 1; j < TilesPerEdge - 1; j++)
                {
                    if (PixelNoise.Pick(i, j, Seed + 8, 7) != 0)
                        continue;
                    layout.TopTexel((i + .3f) / TilesPerEdge, (j + .3f) / TilesPerEdge, out int x, out int y);
                    canvas.Line(x - 3, y + 1, x, y, FloorGrout);
                    canvas.Line(x, y, x + 2, y - 2, FloorGrout);
                    canvas.Set(x + 3, y - 2, FloorBevel);
                }
            }

            // Flush coping stones inside the true edge; brass rails and regularly spaced clamps join the pieces.
            // These are material changes only: the walkable silhouette and its corner positions do not move.
            for (int y = layout.NearY; y <= layout.FarY; y++)
            {
                for (int x = cx - hw; x <= cx + hw; x++)
                {
                    if (!layout.IsOnTop(x, y))
                        continue;
                    if (!layout.IsInRhombus(x, y, hw - 12, hd - 8))
                    {
                        float distance = (x - cx) / (float)hw;
                        float edgePosition = Math.Abs(distance) * 8f;
                        float part = edgePosition - (float)Math.Floor(edgePosition);
                        bool rail = !layout.IsInRhombus(x, y, hw - 2, hd - 2);
                        bool clamp = part < .12f || part > .88f;
                        bool join = part > .48f && part < .54f;
                        bool outerEdge = !layout.IsInRhombus(x, y, hw - 1, hd - 1) && y < layout.MiddleY;
                        bool innerBevel = layout.IsInRhombus(x, y, hw - 10, hd - 6);
                        Rgba rim = join ? FloorGrout : (innerBevel ? RimBevel : RimStone);
                        if (clamp)
                            rim = innerBevel ? PixelPalette.BrassDark : PixelPalette.Brass;
                        if (rail)
                            rim = outerEdge ? PixelPalette.BrassLight : PixelPalette.Brass;
                        canvas.Set(x, y, rim);
                    }
                    else if (!layout.IsInRhombus(x, y, hw - 14, hd - 9))
                        canvas.Set(x, y, FloorGrout);
                }
            }

            // Inlaid, unlit brass: reserve bright metal for the rim and the small centre emblem.
            for (int y = layout.NearY + 5; y <= layout.FarY - 5; y++)
            {
                canvas.Set(cx - 1, y, FloorGrout);
                canvas.Set(cx + 1, y, FloorGrout);
                canvas.Set(cx, y, FloorInlay);
            }
            for (int x = cx - hw + 4; x <= cx + hw - 4; x++)
            {
                canvas.Set(x, layout.MiddleY - 1, FloorGrout);
                canvas.Set(x, layout.MiddleY + 1, FloorGrout);
                canvas.Set(x, layout.MiddleY, FloorInlay);
            }

            // The emblem where the lines cross: nested brass diamonds with a bright heart.
            FillRhombus(canvas, cx, layout.MiddleY, 14, 10, PixelPalette.BrassDark);
            FillRhombus(canvas, cx, layout.MiddleY, 12, 9, PixelPalette.Brass);
            FillRhombus(canvas, cx, layout.MiddleY, 9, 7, PixelPalette.BrassDark);
            FillRhombus(canvas, cx, layout.MiddleY, 5, 4, PixelPalette.Brass);
            FillRhombus(canvas, cx, layout.MiddleY, 2, 1, PixelPalette.BrassLight);
        }

        // The near faces: courses of blocks parallel to the top's near edges, bevelled, with lava seams cut down them.
        private static void DrawFaces(PixelCanvas canvas, PlatformLayout layout)
        {
            int cx = layout.CentreX;
            for (int x = cx - layout.HalfWidth; x <= cx + layout.HalfWidth; x++)
            {
                int edge = layout.NearEdgeY(x);
                bool left = x < cx;
                for (int depth = 0; depth < layout.FaceHeight; depth++)
                {
                    int y = edge - 1 - depth;
                    int course = depth / CourseHeight;
                    int row = depth % CourseHeight;
                    int shift = (course & 1) * (BlockWidth / 2);
                    int block = FloorDiv(x - cx + shift, BlockWidth);
                    Rgba tone;
                    if (row == CourseHeight - 1 || Mod(x - cx + shift, BlockWidth) == 0)
                        tone = PixelPalette.Outline;
                    else if (left)
                        tone = row == 0 || PixelNoise.Pick(block, course, Seed + 1, 4) == 0 ? PixelPalette.FaceLight : PixelPalette.Face;
                    else
                        tone = row == CourseHeight - 2 || PixelNoise.Pick(block, course + 64, Seed + 1, 4) == 0 ? PixelPalette.FaceDark : PixelPalette.Face;
                    canvas.Set(x, y, tone);
                }
            }

            foreach (int side in new[] { -1, 1 })
            {
                foreach (float position in SeamPositions)
                {
                    int x0 = cx + side * (int)Math.Round(position * layout.HalfWidth);
                    int edge = layout.NearEdgeY(x0);
                    for (int depth = 2; depth < layout.FaceHeight - 3; depth++)
                    {
                        int x = x0 + PixelNoise.Pick(x0, depth / 5, Seed + 2, 3) - 1;
                        canvas.Set(x, edge - 1 - depth, PixelPalette.Lava);
                        canvas.Set(x + 1, edge - 1 - depth, PixelPalette.Lava);
                    }
                }
                // Two veins run on through the keel toward the crystal.
                for (int k = 0; k < 2; k++)
                {
                    int x0 = cx + side * (int)Math.Round(SeamPositions[k] * layout.HalfWidth);
                    int y0 = layout.NearEdgeY(x0) - layout.FaceHeight - 1;
                    int x1 = cx + side * (5 + 3 * k);
                    int y1 = layout.TipY + CrystalHeight - 6 - 8 * k;
                    canvas.Line(x0, y0, x1, y1, PixelPalette.Lava);
                    canvas.Line(x0 + 1, y0, x1 + 1, y1, PixelPalette.Lava);
                }
            }
        }

        // The keel: larger, darker blocks under the faces, narrowing to the tip.
        private static void DrawKeel(PixelCanvas canvas, PlatformLayout layout)
        {
            int cx = layout.CentreX;
            for (int x = cx - layout.HalfWidth; x <= cx + layout.HalfWidth; x++)
            {
                int top = layout.NearEdgeY(x) - layout.FaceHeight;
                int bottom = layout.KeelEdgeY(x);
                for (int y = bottom; y < top; y++)
                {
                    int depth = top - 1 - y;
                    int course = depth / KeelCourseHeight;
                    int shift = (course & 1) * (KeelBlockWidth / 2);
                    int block = FloorDiv(x - cx + shift, KeelBlockWidth);
                    Rgba tone;
                    if (depth % KeelCourseHeight == KeelCourseHeight - 1 || Mod(x - cx + shift, KeelBlockWidth) == 0)
                        tone = PixelPalette.Outline;
                    else if (depth % KeelCourseHeight == 0 && x < cx)
                        tone = PixelPalette.Face;
                    else
                        tone = PixelNoise.Pick(block, course, Seed + 3, 3) == 0 ? PixelPalette.Face : PixelPalette.FaceDark;
                    canvas.Set(x, y, tone);
                }
            }
        }

        // Thin chains hanging from the keel's lower edge: lit links with dark gaps, outlined later with the silhouette.
        private static void DrawChains(PixelCanvas canvas, PlatformLayout layout)
        {
            foreach (int side in new[] { -1, 1 })
            {
                foreach (float position in ChainPositions)
                {
                    int x = layout.CentreX + side * (int)Math.Round(position * layout.HalfWidth);
                    int start = layout.KeelEdgeY(x) - 1;
                    int length = position < 0.5f ? 26 : 36;
                    for (int i = 0; i < length; i++)
                    {
                        canvas.Set(x, start - i, i % 4 < 3 ? PixelPalette.ChainLight : PixelPalette.Chain);
                        canvas.Set(x + 1, start - i, PixelPalette.Chain);
                    }
                }
            }
        }

        // The crystal at the keel's tip: dark lava around an ember core; the bright heart lives in the overlay.
        private static void DrawCrystal(PixelCanvas canvas, PlatformLayout layout)
        {
            int cx = layout.CentreX;
            int baseY = layout.TipY + CrystalHeight;
            int tip = layout.TipY;
            canvas.FillTriangle(cx - CrystalHalfWidth, baseY, cx + CrystalHalfWidth, baseY, cx, tip, PixelPalette.Lava);
            canvas.FillTriangle(cx - 9, baseY - 4, cx + 9, baseY - 4, cx, tip + 4, PixelPalette.Ember);
        }

        private static void DrawTowers(PixelCanvas canvas, PlatformLayout layout)
        {
            PixelCanvas tower = DrawTower();
            TowerOrigins(layout, out int leftX, out int leftY, out int rightX, out int rightY, out int nearX, out int nearY);
            canvas.Blit(tower, leftX, leftY);
            canvas.Blit(tower, rightX, rightY);
            canvas.Blit(tower, nearX, nearY);
        }

        // A corner tower: a small stone block with a brass-rimmed top and a lantern box, outlined on its own canvas.
        private static PixelCanvas DrawTower()
        {
            var tower = new PixelCanvas(TowerWidth, TowerHeight);
            int cx = TowerCentre;
            const int topCentreY = 14;
            const int topHalfWidth = 7;
            const int topHalfDepth = 4;
            for (int x = cx - topHalfWidth; x <= cx + topHalfWidth; x++)
            {
                int dx = Math.Abs(x - cx);
                int faceTop = topCentreY - (int)Math.Round(topHalfDepth * (double)(topHalfWidth - dx) / topHalfWidth);
                for (int y = 1; y < faceTop; y++)
                {
                    Rgba tone = x < cx ? PixelPalette.Stone : PixelPalette.StoneDark;
                    if (x == cx)
                        tone = PixelPalette.StoneLight;
                    else if ((faceTop - 1 - y) % 4 == 3)
                        tone = PixelPalette.Grout;
                    tower.Set(x, y, tone);
                }
            }
            for (int y = topCentreY - topHalfDepth; y <= topCentreY + topHalfDepth; y++)
            {
                for (int x = cx - topHalfWidth; x <= cx + topHalfWidth; x++)
                {
                    int value = Math.Abs(x - cx) * topHalfDepth + Math.Abs(y - topCentreY) * topHalfWidth;
                    if (value > topHalfWidth * topHalfDepth)
                        continue;
                    bool rim = Math.Abs(x - cx) * topHalfDepth + Math.Abs(y - topCentreY) * topHalfWidth > (topHalfWidth - 1) * (topHalfDepth - 1);
                    tower.Set(x, y, rim ? PixelPalette.Brass : PixelPalette.StoneLight);
                }
            }
            // The lantern: a dark brass box with a window, capped in brass; its flame is drawn by the overlay.
            tower.FillRect(cx - 2, LanternBottom, 5, 6, PixelPalette.BrassDark);
            tower.FillRect(cx - 1, LanternBottom + 1, 3, 4, PixelPalette.EmberDark);
            tower.FillRect(cx - 2, LanternBottom + 6, 5, 1, PixelPalette.Brass);
            tower.Outline(PixelPalette.Outline);
            return tower;
        }

        // Dithered ember glow around the crystal and its bright heart; the second frame swells a little.
        private static void DrawCrystalGlow(PixelCanvas canvas, PlatformLayout layout, int frame)
        {
            int cx = layout.CentreX;
            int cy = layout.TipY + CrystalHeight / 2;
            DitherEllipse(canvas, cx, cy, 30 + 3 * frame, 34 + 3 * frame, PixelPalette.Ember.WithAlpha(70), frame == 1);
            DitherEllipse(canvas, cx, cy, 18 + 2 * frame, 22 + 2 * frame, PixelPalette.EmberLight.WithAlpha(90), false);
            int baseY = layout.TipY + CrystalHeight;
            int tip = layout.TipY;
            canvas.FillTriangle(cx - 6, baseY - 8, cx + 6, baseY - 8, cx, tip + 8, PixelPalette.EmberLight);
            int heart = 3 + frame;
            canvas.FillTriangle(cx - heart, baseY - 12 - 2 * frame, cx + heart, baseY - 12 - 2 * frame, cx, tip + 12, PixelPalette.EmberHot);
        }

        // Lit stretches of the lava seams; the frames alternate which stretches burn bright.
        private static void DrawSeamGlow(PixelCanvas canvas, PlatformLayout layout, int frame)
        {
            int cx = layout.CentreX;
            foreach (int side in new[] { -1, 1 })
            {
                for (int k = 0; k < SeamPositions.Length; k++)
                {
                    int x0 = cx + side * (int)Math.Round(SeamPositions[k] * layout.HalfWidth);
                    int edge = layout.NearEdgeY(x0);
                    for (int depth = 2; depth < layout.FaceHeight - 3; depth++)
                    {
                        int segment = depth / 6;
                        if ((segment + frame + k) % 2 != 0)
                            continue;
                        int x = x0 + PixelNoise.Pick(x0, depth / 5, Seed + 2, 3) - 1;
                        bool hot = depth % 6 == 3 && frame == 1;
                        canvas.Set(x, edge - 1 - depth, hot ? PixelPalette.EmberHot : PixelPalette.EmberLight);
                        canvas.Set(x + 1, edge - 1 - depth, PixelPalette.Ember);
                    }
                }
            }
        }

        // Lantern flames and lit windows on the three corner towers.
        private static void DrawFlames(PixelCanvas canvas, PlatformLayout layout, int frame)
        {
            var flame = new PixelCanvas(7, 8);
            flame.BlitMap(frame == 0 ? FlameA : FlameB, FlamePalette, 1, 1);
            flame.Outline(PixelPalette.Outline);
            TowerOrigins(layout, out int leftX, out int leftY, out int rightX, out int rightY, out int nearX, out int nearY);
            DrawFlame(canvas, flame, leftX, leftY, frame);
            DrawFlame(canvas, flame, rightX, rightY, frame);
            DrawFlame(canvas, flame, nearX, nearY, frame);
        }

        private static void DrawFlame(PixelCanvas canvas, PixelCanvas flame, int towerX, int towerY, int frame)
        {
            canvas.FillRect(towerX + TowerCentre - 1, towerY + LanternBottom + 1, 3, 4, PixelPalette.EmberLight);
            if (frame == 1)
                canvas.FillRect(towerX + TowerCentre, towerY + LanternBottom + 2, 1, 2, PixelPalette.EmberHot);
            canvas.Blit(flame, towerX + TowerCentre - 3, towerY + FlameBottom - 1);
        }

        private static void FillRhombus(PixelCanvas canvas, int cx, int cy, int halfWidth, int halfDepth, Rgba color)
        {
            for (int y = cy - halfDepth; y <= cy + halfDepth; y++)
            {
                for (int x = cx - halfWidth; x <= cx + halfWidth; x++)
                {
                    if (Math.Abs(x - cx) * halfDepth + Math.Abs(y - cy) * halfWidth <= halfWidth * halfDepth)
                        canvas.Set(x, y, color);
                }
            }
        }

        // A checkerboard of a translucent colour inside an ellipse: the pixel-art way to draw a soft glow.
        private static void DitherEllipse(PixelCanvas canvas, int cx, int cy, int radiusX, int radiusY, Rgba color, bool oddPhase)
        {
            float fx = radiusX + 0.5f;
            float fy = radiusY + 0.5f;
            for (int y = cy - radiusY; y <= cy + radiusY; y++)
            {
                for (int x = cx - radiusX; x <= cx + radiusX; x++)
                {
                    if (((x + y) & 1) != (oddPhase ? 1 : 0))
                        continue;
                    float nx = (x - cx) / fx;
                    float ny = (y - cy) / fy;
                    if (nx * nx + ny * ny <= 1f)
                        canvas.Blend(x, y, color);
                }
            }
        }

        private static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;

        private static int Mod(int a, int n) => ((a % n) + n) % n;

        private static int FloorDiv(int a, int n) => (a - Mod(a, n)) / n;
    }
}
