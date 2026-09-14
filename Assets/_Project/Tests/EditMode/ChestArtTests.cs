using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class ChestArtTests
    {
        [Test]
        public void TheChestIsAnOutlinedBrassBoundBoxThatGlowsWhenOpen()
        {
            PixelCanvas closed = ChestArt.Draw(false);
            PixelCanvas open = ChestArt.Draw(true);
            Assert.That((closed.Width, closed.Height), Is.EqualTo((ChestArt.Width, ChestArt.Height)));
            Assert.That((open.Width, open.Height), Is.EqualTo((ChestArt.Width, ChestArt.Height)));
            Assert.That(closed.Get(0, 0), Is.EqualTo(Rgba.Transparent), "Transparent corners.");
            Assert.That(closed.Get(ChestArt.Width - 1, ChestArt.Height - 1), Is.EqualTo(Rgba.Transparent));
            Assert.That(closed.Get(2, 5), Is.EqualTo(PixelPalette.Brass), "Brass bands at the sides.");
            Assert.That(closed.Get(11, 12), Is.EqualTo(PixelPalette.Outline), "The keyhole on the lock plate.");
            Assert.That(closed.Get(1, 5), Is.EqualTo(PixelPalette.Outline), "A one-texel outline hugs the box.");

            int coins = 0;
            foreach (Rgba pixel in open.Pixels)
            {
                if (pixel.WithAlpha(255) == PixelPalette.CoinLight || pixel == PixelPalette.Coin)
                    coins++;
            }
            Assert.That(coins, Is.GreaterThan(20), "The open chest glows with coins.");
            int closedCoins = 0;
            foreach (Rgba pixel in closed.Pixels)
            {
                if (pixel.WithAlpha(255) == PixelPalette.CoinLight || pixel == PixelPalette.Coin)
                    closedCoins++;
            }
            Assert.That(closedCoins, Is.Zero, "Closed, nothing shows.");
            Assert.That(open.Pixels, Is.Not.EqualTo(closed.Pixels));
            Assert.That(ChestArt.Draw(true).Pixels, Is.EqualTo(open.Pixels), "Deterministic.");
        }
    }
}
