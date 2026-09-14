using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class ArenaGeometryTests
    {
        [Test]
        public void TheRhombusCornersAndCoordinatesAgree()
        {
            var geometry = new ArenaGeometry(-3f, 14f, 3.3f);
            Assert.That(geometry.Middle, Is.EqualTo(5.5f));
            Assert.That(geometry.HalfDepth, Is.EqualTo(8.5f));
            Assert.That(geometry.WorldBottom, Is.EqualTo(ArenaFloor.WorldY(-3f)));
            Assert.That(geometry.WorldTop, Is.EqualTo(7f));

            geometry.TopPoint(0f, 0f, out float x, out float y);
            Assert.That((x, y), Is.EqualTo((0f, -1.5f)), "(0, 0) is the near corner.");
            geometry.TopPoint(1f, 1f, out x, out y);
            Assert.That((x, y), Is.EqualTo((0f, 7f)), "(1, 1) is the far corner.");
            geometry.TopPoint(1f, 0f, out x, out y);
            Assert.That(x, Is.EqualTo(3.3f));
            Assert.That(y, Is.EqualTo(geometry.WorldMiddle));
            geometry.TopPoint(0f, 1f, out x, out _);
            Assert.That(x, Is.EqualTo(-3.3f));

            geometry.RhombusCoordinates(0f, 5.5f, out float s, out float t);
            Assert.That((s, t), Is.EqualTo((0.5f, 0.5f)), "The centre of the top.");
            geometry.RhombusCoordinates(3.3f, 5.5f, out s, out t);
            Assert.That((s, t), Is.EqualTo((1f, 0f)));
            geometry.RhombusCoordinates(-1.65f, 1.25f, out s, out t);
            Assert.That(s, Is.EqualTo(0f).Within(1e-5f));
            Assert.That(t, Is.EqualTo(0.5f).Within(1e-5f));
        }

        [Test]
        public void EveryPackSlotLiesOnThePlatformAndTheVoidDoesNot()
        {
            var geometry = new ArenaGeometry(-3f, 14f, 3.3f);
            Assert.That(geometry.IsOnPlatform(0f, 0f), Is.True, "The hero stands on the platform.");
            Assert.That(geometry.IsOnPlatform(0f, -1f), Is.True);
            for (int count = 1; count <= PackLayout.MaxPackSize; count++)
            {
                for (int slot = 0; slot < count; slot++)
                {
                    PackLayout.Offset(slot, count, 1f, out float x, out float y);
                    Assert.That(geometry.IsOnPlatform(x, 6f + y), Is.True, $"{count} enemies, slot {slot}");
                }
            }
            Assert.That(geometry.IsOnPlatform(3.5f, 5.5f), Is.False);
            Assert.That(geometry.IsOnPlatform(0f, 14.1f), Is.False);
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArenaGeometry(3f, 3f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArenaGeometry(-3f, 14f, 0f));
        }
    }
}
