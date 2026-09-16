using System;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class ContactShadowArtTests
    {
        [Test]
        public void ShadowHasTransparentPaddingAndASymmetricTranslucentContactCentre()
        {
            PixelCanvas canvas = ContactShadowArt.Draw();
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    Rgba pixel = canvas.Get(x, y);
                    Assert.That(pixel, Is.EqualTo(canvas.Get(canvas.Width - x - 1, y)));
                    Assert.That(pixel, Is.EqualTo(canvas.Get(x, canvas.Height - y - 1)));
                    Assert.That(pixel.A, Is.LessThanOrEqualTo(132), "Never obscure floor information with opaque black.");
                    if (x == 0 || y == 0 || x == canvas.Width - 1 || y == canvas.Height - 1)
                        Assert.That(pixel.IsTransparent, Is.True);
                }
            }
            Assert.That(canvas.Get(16, 8).A, Is.GreaterThan(canvas.Get(28, 8).A));
            Assert.That(canvas.Get(28, 8).A, Is.GreaterThan(0));
        }

        [Test]
        public void FootprintsStayOnBothShiftedAndSymmetricPlatformsIncludingTheirRims()
        {
            foreach (ArenaGeometry geometry in new[] { new ArenaGeometry(-9, 9, 9), new ArenaGeometry(-3, 14, 3.3f) })
            {
                foreach (float width in new[] { .38f, .78f, 1.26f })
                {
                    for (int s = 0; s <= 20; s++)
                    {
                        for (int t = 0; t <= 20; t++)
                        {
                            geometry.TopPoint(s / 20f, t / 20f, out float x, out float y);
                            float fit = ContactShadowArt.Fit(geometry, x, y, width);
                            Assert.That(fit, Is.InRange(0f, 1f));
                            foreach (int dx in new[] { -1, 1 })
                            foreach (int dy in new[] { -1, 1 })
                            {
                                float px = x + dx * width * .5f * fit;
                                float py = y + dy * width * .25f * fit;
                                float bounds = Math.Abs(px) / geometry.HalfWidth +
                                    Math.Abs(py - geometry.WorldMiddle) / ((geometry.WorldTop - geometry.WorldBottom) * .5f);
                                Assert.That(bounds, Is.LessThanOrEqualTo(1.000001f), "All four quad corners stay on the floor.");
                            }
                        }
                    }
                    Assert.That(ContactShadowArt.Fit(geometry, 0, geometry.WorldMiddle, width), Is.EqualTo(1));
                    Assert.That(ContactShadowArt.Fit(geometry, geometry.HalfWidth + 1, geometry.WorldMiddle, width), Is.Zero);
                }
            }
        }
    }
}
