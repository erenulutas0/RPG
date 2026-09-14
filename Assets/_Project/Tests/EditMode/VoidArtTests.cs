using System;
using System.Collections.Generic;
using System.Linq;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class VoidArtTests
    {
        // The scene's platform: ArenaView's defaults.
        private static readonly ArenaGeometry SceneGeometry = new ArenaGeometry(-9f, 9f, 9f);

        [Test]
        public void IslandsAreOutlinedRockWithEmberLightsAndAnchorsInsideTheCanvas()
        {
            VoidScene scene = VoidLayout.Create();
            Assert.That(scene.Islands.Length, Is.GreaterThanOrEqualTo(3), "Islands on both sides of the platform.");
            foreach (IslandPlacement placement in scene.Islands)
            {
                IslandArt island = VoidArt.DrawIsland(placement.Spec);
                PixelCanvas canvas = island.Canvas;
                Assert.That((canvas.Width, canvas.Height), Is.EqualTo((placement.Spec.Width, placement.Spec.Height)));
                Assert.That(canvas.Get(0, 0), Is.EqualTo(Rgba.Transparent), "The corners stay void.");
                Assert.That(canvas.Get(canvas.Width - 1, canvas.Height - 1), Is.EqualTo(Rgba.Transparent));
                Assert.That(IsOutlined(canvas), Is.True, "Every visible pixel on the silhouette's edge is the outline colour.");
                Assert.That(Count(canvas, PixelPalette.RockLight), Is.GreaterThan(40), "Lit slabs and spire edges.");
                Assert.That(Count(canvas, PixelPalette.Rock) + Count(canvas, PixelPalette.RockDark), Is.GreaterThan(200), "Two rock faces.");
                Assert.That(Count(canvas, PixelPalette.EmberDark) + Count(canvas, PixelPalette.Ember), Is.GreaterThan(8), "Ember seams and glints.");
                Assert.That(Count(canvas, PixelPalette.EmberLight) + Count(canvas, PixelPalette.EmberHot), Is.GreaterThan(0), "A lit window or seam.");
                Assert.That(canvas.Pixels, Has.None.EqualTo(Rgba.White), "No pure white in the backdrop.");

                Assert.That(island.LanternX, Is.InRange(2, canvas.Width - 3));
                Assert.That(island.LanternY, Is.InRange(1, canvas.Height));
                Assert.That(canvas.Get(island.LanternX, island.LanternY - 1), Is.EqualTo(PixelPalette.Brass), "The flame stands on a brass bowl.");
                Assert.That(island.PlateRow, Is.InRange(1, canvas.Height - 2));
                // Spires stand on the plate's centre line, so the row is slabs plus spire faces: all stone tones.
                int stone = 0;
                for (int x = 0; x < canvas.Width; x++)
                {
                    Rgba pixel = canvas.Get(x, island.PlateRow);
                    if (pixel == PixelPalette.RockLight || pixel == PixelPalette.Rock || pixel == PixelPalette.RockDark)
                        stone++;
                }
                Assert.That(stone, Is.GreaterThanOrEqualTo(placement.Spec.PlateWidth * 3 / 4), "The plate's centre row is stone across its full width.");
                Assert.That(island.UndersideRow(canvas.Width / 2), Is.InRange(0, island.PlateRow), "Stalactites hang under the plate's centre.");
                Assert.That(island.UndersideRow(-1), Is.EqualTo(-1));
            }
            Assert.Throws<ArgumentOutOfRangeException>(() => VoidArt.DrawIsland(new IslandSpec { Width = 20, Height = 20, PlateWidth = 20, PlateHalfHeight = 3, WallHeight = 2, PlateBottom = 4 }));
        }

        [Test]
        public void LanternFlamesSparklesAndStarsKeepToTheirTones()
        {
            PixelCanvas tall = VoidArt.DrawLantern(0);
            PixelCanvas lean = VoidArt.DrawLantern(1);
            Assert.That((tall.Width, tall.Height), Is.EqualTo((VoidArt.LanternWidth, VoidArt.LanternHeight)));
            Assert.That(Count(tall, PixelPalette.EmberHot), Is.GreaterThan(3));
            Assert.That(Count(tall, PixelPalette.EmberLight), Is.GreaterThan(6));
            Assert.That(tall.Get(VoidArt.LanternWidth / 2, 0), Is.EqualTo(PixelPalette.Ember), "The flame's base sits on the bottom row.");
            Assert.That(Differences(tall, lean), Is.GreaterThan(4), "The two frames flicker.");
            Assert.That(tall.Get(0, 0).A, Is.LessThan(255), "The halo is translucent.");

            PixelCanvas bright = VoidArt.DrawSparkle(true);
            PixelCanvas dim = VoidArt.DrawSparkle(false);
            Assert.That((bright.Width, bright.Height), Is.EqualTo((VoidArt.SparkleSize, VoidArt.SparkleSize)));
            Assert.That(bright.Get(4, 4), Is.EqualTo(PixelPalette.StarWhite));
            Assert.That(bright.Get(0, 0), Is.EqualTo(Rgba.Transparent));
            Assert.That(dim.CountVisible(), Is.LessThan(bright.CountVisible()), "The dim frame shrinks the sparkle.");

            var starTones = new HashSet<Rgba> { PixelPalette.StarWhite, PixelPalette.StarLilac, PixelPalette.StarDim };
            PixelCanvas layerA = VoidArt.DrawStarField(120, 96, 5, 0);
            PixelCanvas layerB = VoidArt.DrawStarField(120, 96, 5, 1);
            int shared = 0;
            for (int i = 0; i < layerA.Pixels.Length; i++)
            {
                if (layerA.Pixels[i].A > 0 && layerB.Pixels[i].A > 0)
                    shared++;
                if (layerA.Pixels[i].A > 0)
                    Assert.That(starTones, Does.Contain(layerA.Pixels[i]));
            }
            Assert.That(shared, Is.EqualTo(0), "Each cell's star belongs to one layer so the layers twinkle against each other.");
            Assert.That(layerA.CountVisible() + layerB.CountVisible(), Is.InRange(20, 200), "Sparse stars: 80 cells, about half of them lit.");
            Assert.That(layerA.Pixels, Is.EqualTo(VoidArt.DrawStarField(120, 96, 5, 0).Pixels), "Deterministic.");
        }

        [Test]
        public void ChainsRepeatTheirLinkEverySixRows()
        {
            PixelCanvas chain = VoidArt.DrawChain(5);
            Assert.That((chain.Width, chain.Height), Is.EqualTo((VoidArt.ChainWidth, 5 * VoidArt.ChainLinkPeriod - 1)));
            var tones = new HashSet<Rgba> { PixelPalette.Chain, PixelPalette.ChainLight, PixelPalette.Outline };
            foreach (Rgba pixel in chain.Pixels)
            {
                if (pixel.A > 0)
                    Assert.That(tones, Does.Contain(pixel));
            }
            for (int y = 0; y + VoidArt.ChainLinkPeriod < chain.Height; y++)
            {
                for (int x = 0; x < chain.Width; x++)
                    Assert.That(chain.Get(x, y + VoidArt.ChainLinkPeriod), Is.EqualTo(chain.Get(x, y)), $"Column {x} row {y} repeats one link lower.");
            }
            Assert.That(chain.Get(1, chain.Height - 1), Is.EqualTo(PixelPalette.ChainLight), "The top link hangs from the canvas top.");
            Assert.That(chain.Get(1, chain.Height - 3), Is.EqualTo(Rgba.Transparent), "Links are hollow.");
            Assert.That(chain.Get(1, 0), Is.Not.EqualTo(Rgba.Transparent), "The bottom link closes on the bottom row.");
            Assert.Throws<ArgumentOutOfRangeException>(() => VoidArt.DrawChain(0));
        }

        [Test]
        public void RingHalvesAreDisjointStoneBlocksAroundATransparentCentre()
        {
            PixelCanvas front = VoidArt.DrawRing(60, 24, true);
            PixelCanvas back = VoidArt.DrawRing(60, 24, false);
            Assert.That((front.Width, front.Height), Is.EqualTo((60, 24)));
            Assert.That(front.Get(30, 12), Is.EqualTo(Rgba.Transparent), "The island shows through the ring's centre.");
            Assert.That(back.Get(30, 12), Is.EqualTo(Rgba.Transparent));
            Assert.That(front.Get(0, 0), Is.EqualTo(Rgba.Transparent));
            Assert.That(back.Get(59, 23), Is.EqualTo(Rgba.Transparent));

            int overlap = 0;
            for (int i = 0; i < front.Pixels.Length; i++)
            {
                if (front.Pixels[i].A > 0 && back.Pixels[i].A > 0)
                    overlap++;
            }
            Assert.That(overlap, Is.EqualTo(0), "The halves split on the centre line so the island sits between them.");
            for (int x = 0; x < 60; x++)
            {
                for (int y = 12; y < 24; y++)
                    Assert.That(front.Get(x, y), Is.EqualTo(Rgba.Transparent), "The front half stays below the centre line.");
                for (int y = 0; y < 12; y++)
                    Assert.That(back.Get(x, y), Is.EqualTo(Rgba.Transparent), "The back half stays above it.");
            }
            Assert.That(front.CountVisible(), Is.InRange(150, 400), "A thin band, VoidArt.RingBand texels thick.");
            var ringTones = new HashSet<Rgba> { PixelPalette.Outline, PixelPalette.Rock, PixelPalette.RockDark, PixelPalette.RockLight, PixelPalette.BrassDark, PixelPalette.Ember };
            foreach (Rgba pixel in front.Pixels.Concat(back.Pixels))
            {
                if (pixel.A > 0)
                    Assert.That(ringTones, Does.Contain(pixel), pixel.ToString());
            }
            int litTop = Count(front, PixelPalette.RockLight) + Count(front, PixelPalette.BrassDark);
            Assert.That(litTop, Is.GreaterThan(40), "A lit top row runs along the whole front arc.");
            Assert.That(Count(front, PixelPalette.BrassDark), Is.GreaterThan(8), "Brass trim on part of the top.");
            Assert.That(Count(front, PixelPalette.Ember), Is.GreaterThan(0), "Ember glints in a few seams.");
            Assert.That(Count(front, PixelPalette.Outline), Is.LessThan(front.CountVisible() * 2 / 3), "The rims and seams never swallow the lit band.");
            Assert.That(Count(back, PixelPalette.RockLight) + Count(back, PixelPalette.BrassDark), Is.EqualTo(0), "The far half falls into shadow.");
            Assert.That(Count(back, PixelPalette.Rock) + Count(back, PixelPalette.RockDark), Is.GreaterThan(60));
            Assert.That(front.Pixels, Is.EqualTo(VoidArt.DrawRing(60, 24, true).Pixels), "Deterministic.");
            Assert.Throws<ArgumentOutOfRangeException>(() => VoidArt.DrawRing(60, 70, true));
        }

        [Test]
        public void TheGalaxyAndHazeAreDitheredNebulaTonesThatFadeAtTheRim()
        {
            PixelCanvas galaxy = VoidArt.DrawGalaxy(34, 11);
            Assert.That((galaxy.Width, galaxy.Height), Is.EqualTo((69, 69)));
            Rgba centre = galaxy.Get(34, 34);
            Assert.That(centre, Is.EqualTo(PixelPalette.StarLilac).Or.EqualTo(PixelPalette.NebulaGlow), "A bright core.");
            Assert.That(galaxy.Get(0, 0), Is.EqualTo(Rgba.Transparent));
            Assert.That(galaxy.Get(34, 0), Is.EqualTo(Rgba.Transparent), "Flattened: the top and bottom rows are empty.");
            var nebulaTones = new HashSet<Rgba> { PixelPalette.NebulaViolet, PixelPalette.NebulaGlow, PixelPalette.StarLilac, PixelPalette.StarWhite };
            int visible = 0;
            foreach (Rgba pixel in galaxy.Pixels)
            {
                if (pixel.A == 0)
                    continue;
                visible++;
                Assert.That(nebulaTones, Does.Contain(pixel.WithAlpha(255)), pixel.ToString());
            }
            Assert.That(visible, Is.InRange(galaxy.Pixels.Length * 15 / 100, galaxy.Pixels.Length * 60 / 100), "Dithered arms fade into the void.");
            Assert.That(Count(galaxy, PixelPalette.NebulaGlow), Is.GreaterThan(60), "Bright arm pixels beyond the core.");
            Assert.That(galaxy.Pixels, Is.EqualTo(VoidArt.DrawGalaxy(34, 11).Pixels), "Deterministic.");

            PixelCanvas haze = VoidArt.DrawHaze(48, 32, 21);
            Assert.That((haze.Width, haze.Height), Is.EqualTo((48, 32)));
            Assert.That(haze.Get(0, 0), Is.EqualTo(Rgba.Transparent));
            Assert.That(haze.Get(47, 31), Is.EqualTo(Rgba.Transparent));
            Assert.That(haze.CountVisible(), Is.InRange(haze.Pixels.Length / 10, haze.Pixels.Length * 7 / 10));
            foreach (Rgba pixel in haze.Pixels)
                Assert.That(pixel.A, Is.LessThan(160), "Haze never covers what is behind it.");
        }

        [Test]
        public void EveryPropClearsThePlatformDiamondAndTheLayoutIsDeterministic()
        {
            Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, 0f, 2.2f, 0f), Is.False, "The top's centre.");
            Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, 0f, -7.9f, 0f), Is.False, "The keel's point.");
            Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, 0f, 4.6f, 0f), Is.True, "Above the far corner.");
            Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, -2.6f, 4.2f, 0f), Is.True, "The void beside the far corner.");
            Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, -9.3f, 0f, 0f), Is.True, "Beyond the left corner.");
            Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, 0f, 4.6f, 0.3f), Is.False, "A margin grows the diamond.");

            // Islands drift ±0.05 units, so a 0.2 margin keeps every texel off the platform at every moment.
            const float margin = 0.2f;
            VoidScene scene = VoidLayout.Create();
            var centre = (0.5f, 0.5f);
            var bottomCentre = (0.5f, 0f);
            var topCentre = (0.5f, 1f);
            foreach (IslandPlacement island in scene.Islands)
            {
                IslandArt art = VoidArt.DrawIsland(island.Spec);
                AssertClear(art.Canvas, island.X, island.Y, bottomCentre, margin, "island");
                if (island.HasRing)
                {
                    float ringY = island.RingCentreY(art);
                    AssertClear(VoidArt.DrawRing(island.RingWidth, island.RingHeight, true), island.X, ringY, centre, margin, "ring");
                    AssertClear(VoidArt.DrawRing(island.RingWidth, island.RingHeight, false), island.X, ringY, centre, margin, "ring");
                }
                foreach (ChainSpec chain in island.Chains)
                {
                    island.ChainTop(art, chain, out float chainX, out float chainY);
                    Assert.That(chainY, Is.GreaterThan(island.Y).And.LessThan(ringY(island, art)), "Chains hang from the underside.");
                    AssertClear(VoidArt.DrawChain(chain.Links), chainX, chainY, topCentre, margin, "chain");
                }
                float lanternX = island.X + (art.LanternX + 0.5f - island.Spec.Width / 2f) / VoidArt.TexelsPerUnit;
                float lanternY = island.Y + art.LanternY / (float)VoidArt.TexelsPerUnit;
                AssertClear(VoidArt.DrawLantern(0), lanternX, lanternY, bottomCentre, margin, "lantern");
                Assert.That(Math.Abs(island.X), Is.LessThan(-VoidScene.StarFieldLeft), "Inside the star field.");
                Assert.That(island.Y, Is.InRange(VoidScene.StarFieldBottom, VoidScene.StarFieldBottom + VoidScene.StarFieldHeight));
            }
            foreach (RubblePlacement rubble in scene.Rubble)
                AssertClear(VoidArt.DrawRubble(rubble.Size, rubble.Seed), rubble.X, rubble.Y, centre, margin, "rubble");
            foreach (SparklePlacement sparkle in scene.Sparkles)
                AssertClear(VoidArt.DrawSparkle(true), sparkle.X, sparkle.Y, centre, margin, "sparkle");

            VoidScene again = VoidLayout.Create();
            Assert.That(again.Islands.Select(i => (i.X, i.Y, i.RingWidth, i.Chains.Length)), Is.EqualTo(scene.Islands.Select(i => (i.X, i.Y, i.RingWidth, i.Chains.Length))));
            Assert.That(again.Rubble.Select(r => (r.X, r.Y, r.Size)), Is.EqualTo(scene.Rubble.Select(r => (r.X, r.Y, r.Size))));
            for (int i = 0; i < scene.Islands.Length; i++)
                Assert.That(VoidArt.DrawIsland(again.Islands[i].Spec).Canvas.Pixels, Is.EqualTo(VoidArt.DrawIsland(scene.Islands[i].Spec).Canvas.Pixels), "Islands draw the same every run.");
        }

        private static float ringY(IslandPlacement island, IslandArt art) => island.RingCentreY(art);

        // Maps every visible texel of a sprite placed at a world position (with a normalised pivot) and checks it lies
        // off the platform's silhouette.
        private static void AssertClear(PixelCanvas canvas, float worldX, float worldY, (float X, float Y) pivot, float margin, string what)
        {
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    if (canvas.Get(x, y).A == 0)
                        continue;
                    float wx = worldX + (x + 0.5f - canvas.Width * pivot.X) / VoidArt.TexelsPerUnit;
                    float wy = worldY + (y + 0.5f - canvas.Height * pivot.Y) / VoidArt.TexelsPerUnit;
                    Assert.That(VoidLayout.IsClearOfPlatform(SceneGeometry, wx, wy, margin), Is.True,
                        $"The {what} texel ({x}, {y}) at world ({wx:F2}, {wy:F2}) lies on the platform.");
                }
            }
        }

        // True when every visible pixel with a transparent 4-neighbour inside the canvas is the outline colour.
        private static bool IsOutlined(PixelCanvas canvas)
        {
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    Rgba pixel = canvas.Get(x, y);
                    if (pixel.A == 0 || pixel == PixelPalette.Outline)
                        continue;
                    if (Bare(canvas, x - 1, y) || Bare(canvas, x + 1, y) || Bare(canvas, x, y - 1) || Bare(canvas, x, y + 1))
                        return false;
                }
            }
            return true;
        }

        private static bool Bare(PixelCanvas canvas, int x, int y) => canvas.Contains(x, y) && canvas.Get(x, y).A == 0;

        private static int Count(PixelCanvas canvas, Rgba color)
        {
            int count = 0;
            foreach (Rgba pixel in canvas.Pixels)
            {
                if (pixel == color)
                    count++;
            }
            return count;
        }

        private static int Differences(PixelCanvas a, PixelCanvas b)
        {
            int count = 0;
            for (int i = 0; i < a.Pixels.Length; i++)
            {
                if (a.Pixels[i] != b.Pixels[i])
                    count++;
            }
            return count;
        }
    }
}
