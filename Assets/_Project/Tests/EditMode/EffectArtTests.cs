using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class EffectArtTests
    {
        [Test]
        public void DigitMapsAreThreeByFiveAndDistinct()
        {
            var seen = new HashSet<string>();
            for (int digit = 0; digit < 10; digit++)
            {
                IReadOnlyList<string> map = EffectArt.DigitMap(digit);
                Assert.That(map.Count, Is.EqualTo(EffectArt.DigitHeight), $"Digit {digit} is five rows tall.");
                foreach (string row in map)
                {
                    Assert.That(row.Length, Is.EqualTo(EffectArt.DigitWidth), $"Digit {digit} is three texels wide.");
                    Assert.That(row, Does.Match("^[#.]+$"), "Digit maps use only ink and transparency.");
                }
                Assert.That(seen.Add(string.Join("/", map)), $"Digit {digit} repeats another numeral.");
            }

            Assert.That(EffectArt.DigitCount(0), Is.EqualTo(1));
            Assert.That(EffectArt.DigitCount(128), Is.EqualTo(3));
            Assert.That(EffectArt.DigitCount(1234567), Is.EqualTo(EffectArt.MaxDigits), "Huge hits cap at MaxDigits.");
            Assert.That(EffectArt.DigitAt(128, 0), Is.EqualTo(8));
            Assert.That(EffectArt.DigitAt(128, 2), Is.EqualTo(1));
            Assert.That(EffectArt.DigitAt(1234567, 4), Is.EqualTo(9), "Capped values show all nines.");
        }

        [Test]
        public void AComposedNumberIsElevenTexelsWideWithOneTexelGaps()
        {
            PixelCanvas number = EffectArt.ComposeNumber(128, Rgba.White);
            Assert.That(number.Width, Is.EqualTo(11));
            Assert.That(number.Height, Is.EqualTo(EffectArt.DigitHeight));
            Assert.That(EffectArt.NumberWidth(3), Is.EqualTo(11));
            Assert.That(ColumnIsEmpty(number, 3), "The first gap column is transparent.");
            Assert.That(ColumnIsEmpty(number, 7), "The second gap column is transparent.");
            Assert.That(ColumnIsEmpty(number, 0), Is.False, "The numeral 1 starts in the first column.");
            Assert.That(number.Get(0, 4), Is.EqualTo(Rgba.Transparent), "The top-left of a 1 is open.");
            Assert.That(number.Get(1, 4), Is.EqualTo(Rgba.White), "The 1's stem is white.");
            Assert.That(number.Get(8, 0), Is.EqualTo(Rgba.White), "The 8's base is drawn.");

            PixelCanvas plain = EffectArt.NumberCanvas(128, false);
            Assert.That((plain.Width, plain.Height), Is.EqualTo((13, 7)), "An outlined number is one texel wider on each side.");
            Assert.That(plain.Get(0, 1), Is.EqualTo(PixelPalette.Outline));
            Assert.That(plain.Get(0, 0), Is.EqualTo(Rgba.Transparent), "The corner beside a 1's foot stays open.");
            Assert.That(plain.Get(2, 5), Is.EqualTo(Rgba.White));

            PixelCanvas crit = EffectArt.NumberCanvas(128, true);
            Assert.That((crit.Width, crit.Height), Is.EqualTo((26, 14)), "A crit number is drawn at twice the scale.");
            Assert.That(crit.Get(2, 2), Is.EqualTo(PixelPalette.CoinLight));
            Assert.That(crit.Get(3, 3), Is.EqualTo(PixelPalette.CoinLight), "Upscaling keeps whole texel blocks.");
            Assert.That(Has(crit, Rgba.White), Is.False, "Crit numbers are gold, not white.");

            PixelCanvas digit = EffectArt.DigitCanvas(7, Rgba.White);
            Assert.That((digit.Width, digit.Height), Is.EqualTo((5, 7)));
            Assert.That(digit.Get(3, 5), Is.EqualTo(Rgba.White), "The 7's top bar reaches the glyph's right edge.");
            Assert.That(digit.Get(4, 5), Is.EqualTo(PixelPalette.Outline), "One texel of outline frames the glyph.");
        }

        [Test]
        public void SlashFramesDifferAndUseSteelAndSpellTones()
        {
            PixelCanvas[] frames = EffectArt.SlashFrames();
            Assert.That(frames.Length, Is.EqualTo(3));
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.That((frames[i].Width, frames[i].Height), Is.EqualTo((EffectArt.SlashSize, EffectArt.SlashSize)));
                Assert.That(Has(frames[i], PixelPalette.SteelLight) || Has(frames[i], Rgba.White), $"Frame {i} has a bright blade.");
                Assert.That(Has(frames[i], PixelPalette.Spell), $"Frame {i} has a spell-blue edge.");
                Assert.That(frames[i].Get(0, 0), Is.EqualTo(Rgba.Transparent), $"Frame {i} keeps its corner clear.");
                Assert.That(frames[i].Get(31, 31), Is.EqualTo(Rgba.Transparent));
            }
            Assert.That(Differ(frames[0], frames[1]));
            Assert.That(Differ(frames[1], frames[2]));
            Assert.That(frames[1].CountVisible(), Is.GreaterThan(frames[0].CountVisible()), "The sweep grows into its full arc.");
            Assert.That(frames[2].CountVisible(), Is.LessThan(frames[1].CountVisible()), "The fading sweep thins out.");

            Assert.That(EffectArt.StrikeFor(WeaponBehavior.Cleave), Is.EqualTo(StrikeEffect.SwordSlash));
            Assert.That(EffectArt.StrikeFor(WeaponBehavior.Area), Is.EqualTo(StrikeEffect.StaffBlast));
            Assert.That(EffectArt.StrikeFor(WeaponBehavior.DirectHit), Is.EqualTo(StrikeEffect.DaggerSlash));
        }

        [Test]
        public void TheBlastRingExpandsAndKeepsItsCentreTransparent()
        {
            PixelCanvas[] frames = EffectArt.RingFrames();
            Assert.That(frames.Length, Is.EqualTo(3));
            int centreX = EffectArt.RingWidth / 2;
            int centreY = EffectArt.RingHeight / 2;
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.That((frames[i].Width, frames[i].Height), Is.EqualTo((EffectArt.RingWidth, EffectArt.RingHeight)));
                Assert.That(frames[i].Get(centreX, centreY), Is.EqualTo(Rgba.Transparent), $"Frame {i} is hollow.");
                Assert.That(frames[i].Get(centreX, centreY + 3), Is.EqualTo(Rgba.Transparent), $"Frame {i} is open around the target.");
                Assert.That(frames[i].Get(0, 0), Is.EqualTo(Rgba.Transparent));
            }
            Assert.That(frames[0].Get(centreX + 24, centreY), Is.EqualTo(PixelPalette.SpellLight), "The small ring's rim is light blue.");
            Assert.That(frames[1].Get(centreX + EffectArt.RingFullRadius, centreY).A, Is.GreaterThan(0), "The full ring reaches its radius.");
            Assert.That(frames[1].Get(centreX + 24, centreY), Is.EqualTo(Rgba.Transparent), "The full ring left the small radius behind.");
            Assert.That(Has(frames[1], PixelPalette.SpellLight));
            Assert.That(Has(frames[1], PixelPalette.Spell));
            Assert.That(Differ(frames[1], frames[2]));
            Assert.That(frames[2].CountVisible(), Is.LessThan(frames[1].CountVisible()), "The fading ring breaks up.");
        }

        [Test]
        public void StrikesSparksSmokeAndCoinsHaveTheirSizesAndTones()
        {
            PixelCanvas[] cuts = EffectArt.DoubleSlashFrames();
            Assert.That(cuts.Length, Is.EqualTo(3));
            foreach (PixelCanvas cut in cuts)
            {
                Assert.That((cut.Width, cut.Height), Is.EqualTo((EffectArt.DoubleSlashSize, EffectArt.DoubleSlashSize)));
                Assert.That(Has(cut, PixelPalette.SteelLight));
                Assert.That(Has(cut, PixelPalette.Spell));
            }
            Assert.That(cuts[1].CountVisible(), Is.GreaterThan(cuts[0].CountVisible()), "The second cut joins the first.");

            PixelCanvas[] stars = EffectArt.StarBurstFrames();
            Assert.That(stars.Length, Is.EqualTo(2));
            foreach (PixelCanvas star in stars)
            {
                Assert.That((star.Width, star.Height), Is.EqualTo((EffectArt.StarSize, EffectArt.StarSize)));
                Assert.That(Has(star, PixelPalette.CoinLight));
                Assert.That(Has(star, PixelPalette.Coin), "The star is edged in coin gold.");
            }
            Assert.That(Has(stars[0], Rgba.White), "The tight star has a white core.");
            Assert.That(Differ(stars[0], stars[1]));

            PixelCanvas[] sparks = EffectArt.SparkFrames();
            Assert.That(sparks.Length, Is.EqualTo(2));
            foreach (PixelCanvas spark in sparks)
            {
                Assert.That((spark.Width, spark.Height), Is.EqualTo((EffectArt.SparkSize, EffectArt.SparkSize)));
                Assert.That(Has(spark, PixelPalette.EmberLight));
            }
            Assert.That(Has(sparks[0], PixelPalette.EmberHot), "The burst has a hot core.");
            Assert.That(Differ(sparks[0], sparks[1]));

            PixelCanvas[] smoke = EffectArt.SmokeFrames();
            Assert.That(smoke.Length, Is.EqualTo(3));
            foreach (PixelCanvas puff in smoke)
            {
                Assert.That((puff.Width, puff.Height), Is.EqualTo((EffectArt.SmokeSize, EffectArt.SmokeSize)));
                Assert.That(Has(puff, PixelPalette.Outline), "Smoke is outlined like every prop.");
                Assert.That(Has(puff, EffectArt.Smoke));
                Assert.That(puff.Get(0, 0), Is.EqualTo(Rgba.Transparent));
            }
            Assert.That(Has(smoke[0], PixelPalette.EmberHot), "Fresh smoke carries hot embers.");
            Assert.That(Has(smoke[2], PixelPalette.Ember), "Old smoke carries cooling embers.");
            Assert.That(Differ(smoke[0], smoke[1]));
            Assert.That(Differ(smoke[1], smoke[2]));

            PixelCanvas[] coins = EffectArt.CoinFrames();
            Assert.That(coins.Length, Is.EqualTo(2));
            Assert.That((coins[0].Width, coins[0].Height), Is.EqualTo((EffectArt.CoinSize, EffectArt.CoinSize)));
            Assert.That((coins[1].Width, coins[1].Height), Is.EqualTo((EffectArt.CoinSize / 2, EffectArt.CoinSize)), "Edge on, the coin is half as wide.");
            foreach (PixelCanvas coin in coins)
            {
                Assert.That(Has(coin, PixelPalette.Coin));
                Assert.That(Has(coin, PixelPalette.CoinLight));
                Assert.That(Has(coin, PixelPalette.Outline));
                Assert.That(coin.Get(0, 0), Is.EqualTo(Rgba.Transparent), "Coins are round.");
            }
            Assert.That(Has(coins[0], PixelPalette.Brass), "The face carries a brass emblem.");
        }

        [Test]
        public void EffectsAreDeterministic()
        {
            Assert.That(SamePixels(EffectArt.SlashFrames()[1], EffectArt.SlashFrames()[1]));
            Assert.That(SamePixels(EffectArt.RingFrames()[2], EffectArt.RingFrames()[2]));
            Assert.That(SamePixels(EffectArt.SmokeFrames()[1], EffectArt.SmokeFrames()[1]));
            Assert.That(SamePixels(EffectArt.StarBurstFrames()[1], EffectArt.StarBurstFrames()[1]));
            Assert.That(SamePixels(EffectArt.NumberCanvas(97, true), EffectArt.NumberCanvas(97, true)));

            PixelCanvas scaled = EffectArt.Upscale(EffectArt.CoinFrames()[0], 3);
            Assert.That((scaled.Width, scaled.Height), Is.EqualTo((24, 24)));
            Assert.That(scaled.CountVisible(), Is.EqualTo(EffectArt.CoinFrames()[0].CountVisible() * 9));
        }

        private static bool Has(PixelCanvas canvas, Rgba color)
        {
            foreach (Rgba pixel in canvas.Pixels)
            {
                if (pixel == color)
                    return true;
            }
            return false;
        }

        private static bool ColumnIsEmpty(PixelCanvas canvas, int x)
        {
            for (int y = 0; y < canvas.Height; y++)
            {
                if (canvas.Get(x, y).A > 0)
                    return false;
            }
            return true;
        }

        private static bool SamePixels(PixelCanvas a, PixelCanvas b)
        {
            if (a.Width != b.Width || a.Height != b.Height)
                return false;
            for (int i = 0; i < a.Pixels.Length; i++)
            {
                if (a.Pixels[i] != b.Pixels[i])
                    return false;
            }
            return true;
        }

        private static bool Differ(PixelCanvas a, PixelCanvas b) => !SamePixels(a, b);
    }
}
