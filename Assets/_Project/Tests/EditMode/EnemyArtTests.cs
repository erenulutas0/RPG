using System;
using System.Collections.Generic;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class EnemyArtTests
    {
        private static readonly EnemyLook[] Looks = (EnemyLook[])Enum.GetValues(typeof(EnemyLook));
        private static readonly EnemyPose[] Poses = (EnemyPose[])Enum.GetValues(typeof(EnemyPose));

        // Every tone EnemyArt may put on a canvas: the shared palette's materials plus its own added tones.
        private static readonly HashSet<Rgba> AllowedTones = new HashSet<Rgba>
        {
            Rgba.Transparent, PixelPalette.Outline, EnemyArt.GroundShadow,
            EnemyArt.SlagLight, PixelPalette.Slag, PixelPalette.SlagDark, PixelPalette.SlagCrack,
            EnemyArt.RedSlagLight, EnemyArt.RedSlag, EnemyArt.RedSlagDark,
            PixelPalette.StoneLight, PixelPalette.Stone, PixelPalette.StoneDark, PixelPalette.Grout,
            PixelPalette.SilverLight, PixelPalette.Silver, PixelPalette.SilverDark, PixelPalette.SteelShadow, PixelPalette.StarWhite,
            PixelPalette.BrassLight, PixelPalette.Brass, PixelPalette.BrassDark,
            PixelPalette.Ember, PixelPalette.EmberLight, PixelPalette.EmberHot,
            PixelPalette.Visor, PixelPalette.NebulaGlow,
        };

        [Test]
        public void EveryLookFitsItsTargetSizeWithAnOutlineAndTransparentCorners()
        {
            var maxima = new Dictionary<EnemyLook, (int width, int height)>
            {
                [EnemyLook.Grunt] = (32, 32),
                [EnemyLook.Runner] = (22, 34),
                [EnemyLook.Tank] = (46, 38),
                [EnemyLook.Captain] = (36, 42),
                [EnemyLook.Warden] = (49, 56),
                [EnemyLook.Mite] = (16, 18),
            };
            foreach (EnemyLook look in Looks)
            {
                foreach (EnemyPose pose in Poses)
                {
                    PixelCanvas canvas = EnemyArt.Draw(look, pose);
                    string tag = $"{look} {pose}";
                    Assert.That((canvas.Width, canvas.Height), Is.EqualTo((EnemyArt.WidthOf(look), EnemyArt.HeightOf(look))), tag);
                    Assert.That(canvas.Width, Is.LessThanOrEqualTo(maxima[look].width), $"{tag} width");
                    Assert.That(canvas.Height, Is.LessThanOrEqualTo(maxima[look].height), $"{tag} height");
                    Assert.That(canvas.Width % 2, Is.EqualTo(1), $"{tag}: odd width keeps the figure centred on its pivot.");
                    Assert.That(canvas.CountVisible(), Is.GreaterThan(canvas.Width * canvas.Height / 4), $"{tag} is mostly empty.");

                    foreach ((int x, int y) in new[] { (0, 0), (canvas.Width - 1, 0), (0, canvas.Height - 1), (canvas.Width - 1, canvas.Height - 1) })
                        Assert.That(canvas.Get(x, y), Is.EqualTo(Rgba.Transparent), $"{tag} corner ({x}, {y})");

                    // Row 0 is the flat ground shadow; the feet (and their outline) stand on it.
                    Assert.That(CountInRow(canvas, 0, EnemyArt.GroundShadow), Is.GreaterThan(4), $"{tag} shadow under the feet");
                    Assert.That(CountInRow(canvas, canvas.Height - 1, EnemyArt.GroundShadow), Is.EqualTo(0), $"{tag} shadow stays on the floor");
                    AssertOutlined(canvas, tag);
                }
            }
        }

        [Test]
        public void EveryLookUsesItsMaterialsFromThePalette()
        {
            var materials = new Dictionary<EnemyLook, Rgba[]>
            {
                [EnemyLook.Grunt] = new[] { PixelPalette.Slag, PixelPalette.SlagDark, PixelPalette.SlagCrack, PixelPalette.EmberHot },
                [EnemyLook.Runner] = new[] { PixelPalette.Slag, PixelPalette.SlagDark, PixelPalette.SlagCrack, PixelPalette.EmberHot },
                [EnemyLook.Tank] = new[] { PixelPalette.Slag, PixelPalette.Stone, PixelPalette.StoneLight, PixelPalette.Grout, PixelPalette.SlagCrack },
                [EnemyLook.Captain] = new[] { PixelPalette.Lava, EnemyArt.RedSlagDark, PixelPalette.BrassLight, PixelPalette.Brass, PixelPalette.SlagCrack },
                [EnemyLook.Warden] = new[] { PixelPalette.SilverLight, PixelPalette.Silver, PixelPalette.SilverDark, PixelPalette.Visor },
                [EnemyLook.Mite] = new[] { PixelPalette.SlagDark, PixelPalette.Ember, PixelPalette.EmberLight, PixelPalette.EmberHot },
            };
            foreach (EnemyLook look in Looks)
            {
                PixelCanvas canvas = EnemyArt.Draw(look, EnemyPose.IdleA);
                var used = new HashSet<Rgba>(canvas.Pixels);
                foreach (Rgba tone in used)
                    Assert.That(AllowedTones.Contains(tone), $"{look} paints {tone}, which is not a palette tone.");
                foreach (Rgba tone in materials[look])
                    Assert.That(used.Contains(tone), $"{look} lacks {tone}.");
                Assert.That(Count(canvas, PixelPalette.Outline), Is.GreaterThan(canvas.Width * 2), $"{look} outline is too short.");
            }
        }

        [Test]
        public void WardenWearsOneHorizontalVisorAndNoEyes()
        {
            foreach (EnemyPose pose in Poses)
            {
                PixelCanvas warden = EnemyArt.Draw(EnemyLook.Warden, pose);
                var visorRows = new List<int>();
                for (int y = 0; y < warden.Height; y++)
                {
                    if (CountInRow(warden, y, PixelPalette.Visor) > 0)
                        visorRows.Add(y);
                }
                Assert.That(visorRows.Count, Is.EqualTo(1), $"{pose}: the visor is one thin row.");
                int row = visorRows[0];
                Assert.That(LongestRun(warden, row, PixelPalette.Visor), Is.GreaterThanOrEqualTo(9), $"{pose}: the visor spans the faceplate.");
                Assert.That(row, Is.GreaterThan(warden.Height / 2), $"{pose}: the visor sits on the head.");
                Assert.That(CountInRow(warden, row - 1, PixelPalette.NebulaGlow), Is.GreaterThanOrEqualTo(7), $"{pose}: the visor has its shadow row.");

                foreach (Rgba ember in new[] { PixelPalette.EmberHot, PixelPalette.EmberLight, PixelPalette.Ember })
                    Assert.That(Count(warden, ember), Is.EqualTo(0), $"{pose}: the Warden has no ember eyes or grin.");
                Assert.That(Count(warden, PixelPalette.SilverLight) + Count(warden, PixelPalette.Silver), Is.GreaterThan(400), $"{pose}: mostly quicksilver.");
            }
        }

        [Test]
        public void MiteBurnsAFlameCrownOverADarkEmberBody()
        {
            foreach (EnemyPose pose in Poses)
            {
                PixelCanvas mite = EnemyArt.Draw(EnemyLook.Mite, pose);
                int crown = 0;
                for (int y = mite.Height - 6; y < mite.Height; y++)
                    crown += CountInRow(mite, y, PixelPalette.EmberLight) + CountInRow(mite, y, PixelPalette.EmberHot) + CountInRow(mite, y, PixelPalette.Ember);
                Assert.That(crown, Is.GreaterThanOrEqualTo(8), $"{pose}: the top rows are flame.");

                int body = 0;
                for (int y = 1; y <= 6; y++)
                    body += CountInRow(mite, y, PixelPalette.Slag) + CountInRow(mite, y, PixelPalette.SlagDark) + CountInRow(mite, y, EnemyArt.SlagLight);
                Assert.That(body, Is.GreaterThanOrEqualTo(8), $"{pose}: the low rows are dark slag.");
                Assert.That(Count(mite, PixelPalette.EmberHot), Is.GreaterThanOrEqualTo(2), $"{pose}: two bright eyes.");
            }
        }

        [Test]
        public void IdleFramesDifferSlightlyAndTheAttackFrameChangesTheSilhouette()
        {
            foreach (EnemyLook look in Looks)
            {
                PixelCanvas idleA = EnemyArt.Draw(look, EnemyPose.IdleA);
                PixelCanvas idleB = EnemyArt.Draw(look, EnemyPose.IdleB);
                PixelCanvas attack = EnemyArt.Draw(look, EnemyPose.Attack);

                int bob = Differences(idleA, idleB);
                Assert.That(bob, Is.GreaterThan(0), $"{look}: the idle frames must differ.");
                Assert.That(bob, Is.LessThan(idleA.Width * idleA.Height / 2), $"{look}: the idle bob is slight.");
                Assert.That(idleB.CountVisible(), Is.EqualTo(idleA.CountVisible()).Within(idleA.CountVisible() / 10), $"{look}: the bob keeps the mass.");

                int lunge = Differences(idleA.Silhouette(Rgba.White), attack.Silhouette(Rgba.White));
                Assert.That(lunge, Is.GreaterThan(0), $"{look}: the attack frame changes the shape, not only the colours.");
                AssertOutlined(attack, $"{look} Attack");
            }
        }

        [Test]
        public void HealthBarFillSitsInsideItsOutlinedBacking()
        {
            PixelCanvas back = EnemyArt.DrawHealthBarBack();
            PixelCanvas fill = EnemyArt.DrawHealthBarFill();
            Assert.That((back.Width, back.Height), Is.EqualTo((EnemyArt.HealthBarWidth, EnemyArt.HealthBarHeight)));
            Assert.That((fill.Width, fill.Height), Is.EqualTo((EnemyArt.HealthFillWidth, EnemyArt.HealthFillHeight)));
            Assert.That(EnemyArt.HealthFillWidth, Is.EqualTo(EnemyArt.HealthBarWidth - 2), "One texel of frame each side.");
            Assert.That(EnemyArt.HealthFillHeight, Is.EqualTo(EnemyArt.HealthBarHeight - 2));
            Assert.That(back.CountVisible(), Is.EqualTo(back.Width * back.Height), "The backing is solid.");
            Assert.That(fill.CountVisible(), Is.EqualTo(fill.Width * fill.Height), "The fill is solid.");

            for (int x = 0; x < back.Width; x++)
            {
                Assert.That(back.Get(x, 0), Is.EqualTo(PixelPalette.Outline));
                Assert.That(back.Get(x, back.Height - 1), Is.EqualTo(PixelPalette.Outline));
            }
            Assert.That(back.Get(0, 2), Is.EqualTo(PixelPalette.Outline));
            Assert.That(back.Get(back.Width - 1, 2), Is.EqualTo(PixelPalette.Outline));
            Assert.That(back.Get(1, 1), Is.EqualTo(PixelPalette.BarBack));
            Assert.That(back.Get(back.Width - 2, back.Height - 2), Is.EqualTo(PixelPalette.BarBack));

            Assert.That(CountInRow(fill, 1, PixelPalette.HealthRed), Is.EqualTo(fill.Width), "The middle row is the health red.");
            Assert.That(CountInRow(fill, 2, EnemyArt.HealthLight), Is.EqualTo(fill.Width), "A lit top edge.");
            Assert.That(CountInRow(fill, 0, EnemyArt.HealthShade), Is.EqualTo(fill.Width), "A shaded bottom edge.");
        }

        [Test]
        public void LooksAreIdenticalOnEveryDraw()
        {
            foreach (EnemyLook look in Looks)
            {
                foreach (EnemyPose pose in Poses)
                {
                    PixelCanvas first = EnemyArt.Draw(look, pose);
                    PixelCanvas second = EnemyArt.Draw(look, pose);
                    Assert.That(second.Pixels, Is.EqualTo(first.Pixels), $"{look} {pose} must not change between draws.");
                }
            }
            Assert.That(EnemyArt.DrawHealthBarFill().Pixels, Is.EqualTo(EnemyArt.DrawHealthBarFill().Pixels));
        }

        // Every opaque texel that touches a transparent or shadow texel (or the canvas edge) is outline: the figure is
        // fully ringed and nothing is clipped by the canvas.
        private static void AssertOutlined(PixelCanvas canvas, string tag)
        {
            int outline = 0;
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    Rgba pixel = canvas.Get(x, y);
                    if (!pixel.IsOpaque)
                        continue;
                    bool edge = !canvas.Get(x - 1, y).IsOpaque || !canvas.Get(x + 1, y).IsOpaque ||
                                !canvas.Get(x, y - 1).IsOpaque || !canvas.Get(x, y + 1).IsOpaque;
                    if (!edge)
                        continue;
                    Assert.That(pixel, Is.EqualTo(PixelPalette.Outline), $"{tag}: texel ({x}, {y}) on the edge is not outline.");
                    outline++;
                }
            }
            Assert.That(outline, Is.GreaterThan(canvas.Height * 2), $"{tag}: outline is too short.");
        }

        private static int Differences(PixelCanvas a, PixelCanvas b)
        {
            Assert.That((b.Width, b.Height), Is.EqualTo((a.Width, a.Height)));
            int count = 0;
            for (int i = 0; i < a.Pixels.Length; i++)
            {
                if (a.Pixels[i] != b.Pixels[i])
                    count++;
            }
            return count;
        }

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

        private static int CountInRow(PixelCanvas canvas, int y, Rgba color)
        {
            int count = 0;
            for (int x = 0; x < canvas.Width; x++)
            {
                if (canvas.Get(x, y) == color)
                    count++;
            }
            return count;
        }

        private static int LongestRun(PixelCanvas canvas, int y, Rgba color)
        {
            int best = 0;
            int run = 0;
            for (int x = 0; x < canvas.Width; x++)
            {
                run = canvas.Get(x, y) == color ? run + 1 : 0;
                best = Math.Max(best, run);
            }
            return best;
        }
    }
}
