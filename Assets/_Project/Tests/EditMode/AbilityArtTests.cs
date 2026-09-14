using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class AbilityArtTests
    {
        [Test]
        public void TheGlyphIsAnOutlinedSpellBurst()
        {
            PixelCanvas glyph = AbilityArt.DrawGlyph();
            Assert.That((glyph.Width, glyph.Height), Is.EqualTo((AbilityArt.GlyphSize, AbilityArt.GlyphSize)));
            int centre = AbilityArt.GlyphSize / 2;
            Assert.That(glyph.Get(centre, centre), Is.EqualTo(PixelPalette.SpellLight), "A bright core.");
            Assert.That(glyph.Get(centre, centre + 7), Is.EqualTo(PixelPalette.Spell), "Points along the axes.");
            Assert.That(glyph.Get(centre, centre + 9), Is.EqualTo(PixelPalette.Outline), "An outline beyond the point.");
            Assert.That(glyph.Get(0, 0), Is.EqualTo(Rgba.Transparent), "Transparent corners.");
            Assert.That(glyph.CountVisible(), Is.InRange(60, 200));
        }

        [Test]
        public void TheRangeRingIsADashedIsometricEllipseAroundAClearCentre()
        {
            PixelCanvas ring = AbilityArt.DrawRangeRing(80);
            Assert.That(ring.Width, Is.EqualTo(2 * 80 + 2 * AbilityArt.RingThickness + 1));
            Assert.That(ring.Height, Is.EqualTo(80 + 2 * AbilityArt.RingThickness + 1), "Depth shows at half length on screen.");
            int cx = ring.Width / 2;
            int cy = ring.Height / 2;
            Assert.That(ring.Get(cx, cy), Is.EqualTo(Rgba.Transparent), "The hero stands in a clear centre.");
            Assert.That(ring.Get(cx + 80, cy), Is.EqualTo(PixelPalette.SpellLight), "The first dash starts at the right.");
            Assert.That(ring.Get(cx + 40, cy), Is.EqualTo(Rgba.Transparent), "Inside the ring is clear.");

            int drawn = 0;
            int gaps = 0;
            for (int x = cx - 80; x <= cx + 80; x++)
            {
                double inner = 1.0 - (double)(x - cx) * (x - cx) / (80.0 * 80.0);
                int y = cy + (int)Math.Round(Math.Sqrt(Math.Max(0.0, inner)) * 40.0);
                bool visible = ring.Get(x, y).A > 0 || ring.Get(x, y + 1).A > 0 || ring.Get(x, y - 1).A > 0;
                if (visible)
                    drawn++;
                else
                    gaps++;
            }
            Assert.That(drawn, Is.GreaterThan(60), "Most of the upper arc is drawn.");
            Assert.That(gaps, Is.GreaterThan(15), "Dashes leave gaps.");
            Assert.That(AbilityArt.DrawRangeRing(80).Pixels, Is.EqualTo(ring.Pixels), "Deterministic.");
            Assert.Throws<ArgumentOutOfRangeException>(() => AbilityArt.DrawRangeRing(2));
        }
    }
}
