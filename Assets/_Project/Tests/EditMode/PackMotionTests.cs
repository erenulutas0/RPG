using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PackMotionTests
    {
        private const float Frame = 1f / 60f;
        private static readonly ArenaGeometry Arena = new ArenaGeometry(-9f, 9f, 9f);

        [Test]
        public void AnEnemyWalksAtItsSpeedAndStopsJustInsideItsReach()
        {
            var motion = new PackMotion(0.9f, 2f);
            var grunt = new HealthState(50f);
            motion.Add(grunt, 0f, 6f, 2f, 1.2f);

            motion.Step(1f);
            Assert.That(motion.XOf(0), Is.EqualTo(0f).Within(1e-5f));
            Assert.That(motion.YOf(0), Is.EqualTo(4f).Within(1e-4f), "Two floor units in one second.");
            Assert.That(motion.HasArrived(0), Is.False);

            for (int frame = 0; frame < 300; frame++)
                motion.Step(Frame);
            Assert.That(motion.HasArrived(0), Is.True);
            Assert.That(motion.YOf(0), Is.EqualTo(1.2f - PackMotion.ReachMargin).Within(1e-4f));
            float arrivedAt = motion.YOf(0);
            motion.Step(1f);
            Assert.That(motion.YOf(0), Is.EqualTo(arrivedAt), "An arrived enemy stays where it stopped.");
            Assert.That(1.2f * 1.2f, Is.GreaterThan(motion.YOf(0) * motion.YOf(0)), "It stands inside its reach.");
        }

        [Test]
        public void TheEntrySideDecidesTheApproachAngle()
        {
            var motion = new PackMotion(0.9f, 2f);
            motion.Add(new HealthState(10f), -2f, 6f, 3f, 2.1f);
            motion.Add(new HealthState(10f), 0f, 7f, 3f, 2.1f);
            motion.Add(new HealthState(10f), 2f, 6f, 3f, 2.1f);
            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);

            // The widest entries approach from 60 degrees off the centre line, the centre one straight on.
            Assert.That(motion.XOf(0), Is.EqualTo(-2f * (float)Math.Sin(Math.PI / 3)).Within(1e-4f));
            Assert.That(motion.YOf(0), Is.EqualTo(2f * (float)Math.Cos(Math.PI / 3)).Within(1e-4f));
            Assert.That(motion.XOf(1), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(motion.YOf(1), Is.EqualTo(2f).Within(1e-4f));
            Assert.That(motion.XOf(2), Is.EqualTo(-motion.XOf(0)).Within(1e-4f));
        }

        [Test]
        public void EachCornerEntersFromItsOwnSideAndTheLateralOffsetTurnsWithIt()
        {
            var motion = new PackMotion(0.9f, 2f);
            motion.Add(new HealthState(10f), 1f, 5f, 2f, 1.4f, EntrySide.Far);
            motion.Add(new HealthState(10f), 1f, 5f, 2f, 1.4f, EntrySide.Right);
            motion.Add(new HealthState(10f), 1f, 5f, 2f, 1.4f, EntrySide.Near);
            motion.Add(new HealthState(10f), 1f, 5f, 2f, 1.4f, EntrySide.Left);

            Assert.That((motion.XOf(0), motion.YOf(0)), Is.EqualTo((1f, 5f)), "Far: ahead of the hero, offset to the right.");
            Assert.That((motion.XOf(1), motion.YOf(1)), Is.EqualTo((5f, -1f)), "Right: the offset turns a quarter turn with the side.");
            Assert.That((motion.XOf(2), motion.YOf(2)), Is.EqualTo((-1f, -5f)), "Near: below the hero.");
            Assert.That((motion.XOf(3), motion.YOf(3)), Is.EqualTo((-5f, 1f)), "Left.");

            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);
            for (int i = 0; i < 4; i++)
                Assert.That(motion.HasArrived(i), Is.True, $"Slot {i} walks in from its corner.");
            // Every enemy ends on its own side of the hero, 30 degrees off its corner's line toward its offset.
            Assert.That(motion.YOf(0), Is.GreaterThan(0.9f));
            Assert.That(motion.XOf(0), Is.GreaterThan(0.4f));
            Assert.That(motion.XOf(1), Is.GreaterThan(0.9f));
            Assert.That(motion.YOf(1), Is.LessThan(-0.4f));
            Assert.That(motion.YOf(2), Is.LessThan(-0.9f));
            Assert.That(motion.XOf(3), Is.LessThan(-0.9f));
            Assert.That(EntrySides.SideOf(0, 0), Is.EqualTo(EntrySide.Far));
            Assert.That(EntrySides.SideOf(1, 2), Is.EqualTo(EntrySide.Left), "Wave 1 starts one corner further.");
            EntrySides.Formation(1, 5, 6, out EntrySide side, out int index, out int count);
            Assert.That(side, Is.EqualTo(EntrySide.Near), "Slot 5 of wave 1 takes the near corner.");
            Assert.That((index, count), Is.EqualTo((1, 2)), "The second enemy of two on that corner.");
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Formation(0, 3, 3, out _, out _, out _));
        }

        [Test]
        public void ThePackFollowsAHeroWhoWalksAway()
        {
            var motion = new PackMotion(0.9f, 2f);
            motion.Add(new HealthState(10f), 0f, 3f, 2f, 1.2f);
            for (int frame = 0; frame < 120; frame++)
                motion.Step(Frame);
            Assert.That(motion.HasArrived(0), Is.True);

            // The hero steps three units to the right: the enemy is out of reach again and walks after it.
            motion.Step(Frame, 3f, 0f);
            Assert.That(motion.HasArrived(0), Is.False);
            Assert.That(motion.HeroX, Is.EqualTo(3f));
            for (int frame = 0; frame < 240; frame++)
                motion.Step(Frame, 3f, 0f);
            Assert.That(motion.HasArrived(0), Is.True);
            float dx = motion.XOf(0) - 3f;
            float dy = motion.YOf(0);
            Assert.That(Math.Sqrt(dx * dx + dy * dy), Is.EqualTo(1.1f).Within(1e-3f), "It stops just inside its reach of the new spot.");
            Assert.That(motion.YOf(0), Is.GreaterThan(0.9f), "It keeps to its own side, ahead of the hero.");
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Step(Frame, float.NaN, 0f));
        }

        // With the hero at the centre every corner fits its enemies, so a wave enters exactly where it entered before the
        // corners could close: the scene, the simulation and the balance built on them stay as they were.
        [Test]
        public void WithTheHeroAtTheCentreEveryCornerStaysOpenAndThePackFormsUpAsBefore()
        {
            var placements = new EntryPlacement[PackLayout.MaxPackSize];
            for (int wave = 0; wave < EntrySides.Count; wave++)
            {
                for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                {
                    EntrySides.Place(wave, count, 0f, 0f, Arena, 5f, 1f, placements);
                    for (int slot = 0; slot < count; slot++)
                    {
                        EntrySides.Formation(wave, slot, count, out EntrySide side, out int index, out int onSide);
                        PackLayout.Offset(index, onSide, 1f, out float lateral, out float depth);
                        var motion = new PackMotion(0.9f, 2f);
                        motion.Add(new HealthState(1f), lateral, 5f + depth, 1f, 1f, side);
                        string where = $"wave {wave}, {count} enemies, slot {slot}";
                        Assert.That(placements[slot].Side, Is.EqualTo(side), where);
                        Assert.That(placements[slot].Lateral, Is.EqualTo(lateral), where);
                        Assert.That((placements[slot].X, placements[slot].Y), Is.EqualTo((motion.XOf(0), motion.YOf(0))), where);
                    }
                }
            }

            // Over two open corners the slots take them in turn, and the next wave starts one open corner further.
            int nearAndLeft = (1 << (int)EntrySide.Near) | (1 << (int)EntrySide.Left);
            EntrySides.Formation(0, 0, 7, nearAndLeft, out EntrySide first, out int firstIndex, out int firstCount);
            Assert.That((first, firstIndex, firstCount), Is.EqualTo((EntrySide.Near, 0, 4)));
            EntrySides.Formation(0, 6, 7, nearAndLeft, out EntrySide last, out int lastIndex, out int lastCount);
            Assert.That((last, lastIndex, lastCount), Is.EqualTo((EntrySide.Near, 3, 4)), "Slot 6 is the fourth of four at the near corner.");
            EntrySides.Formation(1, 0, 7, nearAndLeft, out EntrySide turned, out _, out int turnedCount);
            Assert.That((turned, turnedCount), Is.EqualTo((EntrySide.Left, 4)));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Formation(0, 0, 1, 0, out _, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Formation(0, 0, 1, EntrySides.AllSides + 1, out _, out _, out _));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Formation(-1, 0, 1, EntrySides.AllSides, out _, out _, out _));
            Assert.Throws<ArgumentNullException>(() => EntrySides.Place(0, 1, 0f, 0f, Arena, 5f, 1f, null));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Place(0, 8, 0f, 0f, Arena, 5f, 1f, new EntryPlacement[8]));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Place(0, 3, 0f, 0f, Arena, 5f, 1f, new EntryPlacement[2]));
        }

        // A hero at the rim has no floor beyond it, so the corners on that side close for the wave and the open ones take
        // their enemies: every enemy still starts on the platform, out of reach, apart from the others.
        [Test]
        public void AtTheRimCornersOverTheVoidCloseAndThePackEntersFromWhereThePlatformIs()
        {
            var placements = new EntryPlacement[PackLayout.MaxPackSize];
            float rim = 9f - HeroMotion.EdgeMargin;
            AssertEntries(rim, 0f, 1 << (int)EntrySide.Left, "at the right corner");
            AssertEntries(rim / 2f, rim / 2f, (1 << (int)EntrySide.Near) | (1 << (int)EntrySide.Left), "on the far-right edge");
            AssertEntries(0f, -rim, 1 << (int)EntrySide.Far, "at the near corner");
            AssertEntries(2f, 0f, EntrySides.AllSides, "two units right of the centre, where every corner still fits");

            // A platform too small for the wave closes every corner; each enemy is drawn in toward the hero instead.
            var small = new ArenaGeometry(-3f, 3f, 3f);
            EntrySides.Place(0, 4, 0f, 0f, small, 5f, 1f, placements);
            for (int slot = 0; slot < 4; slot++)
                Assert.That(small.IsOnPlatform(placements[slot].X, placements[slot].Y, HeroMotion.EdgeMargin), Is.True, $"Small platform, slot {slot}.");

            void AssertEntries(float heroX, float heroY, int openSides, string hero)
            {
                int openCount = 0;
                for (int bits = openSides; bits != 0; bits &= bits - 1)
                    openCount++;
                for (int wave = 0; wave < EntrySides.Count; wave++)
                {
                    for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                    {
                        EntrySides.Place(wave, count, heroX, heroY, Arena, 5f, 1f, placements);
                        int used = 0;
                        for (int slot = 0; slot < count; slot++)
                        {
                            EntryPlacement entry = placements[slot];
                            string where = $"Hero {hero}, wave {wave}, {count} enemies, slot {slot}";
                            Assert.That(Arena.IsOnPlatform(entry.X, entry.Y, HeroMotion.EdgeMargin), Is.True, where);
                            float dx = entry.X - heroX;
                            float dy = entry.Y - heroY;
                            Assert.That(dx * dx + dy * dy, Is.GreaterThanOrEqualTo(25f - 1e-3f), where + " starts out of reach.");
                            for (int other = 0; other < slot; other++)
                            {
                                float apartX = entry.X - placements[other].X;
                                float apartY = entry.Y - placements[other].Y;
                                Assert.That(apartX * apartX + apartY * apartY, Is.GreaterThanOrEqualTo(0.9f * 0.9f), where + $" keeps apart from slot {other}.");
                            }
                            used |= 1 << (int)entry.Side;
                        }
                        Assert.That(used & ~openSides, Is.Zero, $"Hero {hero}, wave {wave}, {count} enemies: only the open corners.");
                        if (count >= openCount)
                            Assert.That(used, Is.EqualTo(openSides), $"Hero {hero}, wave {wave}, {count} enemies: every open corner sends some.");
                    }
                }
            }
        }

        // A Grunt entered at the far corner while the hero stood at the centre; the hero then walked out to the right corner.
        // Its own point beside the hero hangs over the void, so it turns round the hero onto the platform and stops there.
        [Test]
        public void AtTheRimAnEnemyTurnsRoundTheHeroAndStopsOnThePlatform()
        {
            float heroX = 9f - HeroMotion.EdgeMargin;
            var free = new PackMotion(0.9f, 2f);
            var bounded = new PackMotion(0.9f, 2f, Arena, 0f, 0f);
            free.Add(new HealthState(50f), 0f, 5f, 2f, 1.5f);
            bounded.Add(new HealthState(50f), 0f, 5f, 2f, 1.5f);
            for (int frame = 0; frame < 600; frame++)
            {
                free.Step(Frame, heroX, 0f);
                bounded.Step(Frame, heroX, 0f);
                Assert.That(Arena.IsOnPlatform(bounded.XOf(0), bounded.YOf(0), HeroMotion.EdgeMargin - 1e-3f), Is.True, $"Frame {frame}.");
            }

            Assert.That(Arena.IsOnPlatform(free.XOf(0), free.YOf(0)), Is.False, "Unbounded, it would stop beyond the rim, over the void.");
            float dx = bounded.XOf(0) - heroX;
            float dy = bounded.YOf(0);
            Assert.That(Math.Sqrt(dx * dx + dy * dy), Is.EqualTo(1.4f).Within(1e-3f), "It stops just inside its reach, as anywhere else.");
            Assert.That(bounded.XOf(0), Is.LessThan(heroX), "It stands on the platform's side of the hero.");
            Assert.That(bounded.YOf(0), Is.GreaterThan(0f), "It keeps to the side it came from.");
            Assert.That(Arena.IsOnPlatform(bounded.XOf(0), bounded.YOf(0), HeroMotion.EdgeMargin), Is.True);
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackMotion(0.9f, 2f, Arena, float.NaN, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => bounded.Add(new HealthState(1f), new EntryPlacement(EntrySide.Far, 0f, float.NaN, 0f), 1f, 1f));
        }

        [Test]
        public void AnEnemyWaitsBehindACloserOneAndWalksInWhenItFalls()
        {
            var motion = new PackMotion(0.9f, 2f);
            var front = new HealthState(10f);
            var rear = new HealthState(10f);
            motion.Add(rear, 0f, 7f, 2f, 1.2f);
            motion.Add(front, 0f, 6f, 2f, 1.2f);
            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);

            Assert.That(motion.HasArrived(1), Is.True, "The closer enemy walks in first, whatever its slot.");
            Assert.That(motion.HasArrived(0), Is.False, "The rear enemy waits in line.");
            Assert.That(motion.YOf(0) - motion.YOf(1), Is.GreaterThanOrEqualTo(0.9f - 1e-4f), "It keeps its spacing.");

            front.ApplyDamage(new DamageContext(10f));
            for (int frame = 0; frame < 600; frame++)
                motion.Step(Frame);
            Assert.That(motion.HasArrived(0), Is.True, "A fallen enemy no longer blocks the way.");
            Assert.That(motion.YOf(1), Is.EqualTo(1.1f).Within(1e-4f), "A dead enemy does not move.");
        }

        [Test]
        public void MotionSettingsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackMotion(-1f, 2f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PackMotion(0.9f, 0f));
            var motion = new PackMotion(0.9f, 2f);
            Assert.Throws<ArgumentNullException>(() => motion.Add(null, 0f, 6f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Add(new HealthState(1f), 0f, 6f, -1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Add(new HealthState(1f), 0f, 6f, 1f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Add(new HealthState(1f), 0f, 6f, 1f, 1f, (EntrySide)9));
            Assert.Throws<ArgumentOutOfRangeException>(() => motion.Step(-Frame));
            for (int i = 0; i < PackLayout.MaxPackSize; i++)
                motion.Add(new HealthState(1f), i, 6f, 1f, 1f);
            Assert.Throws<InvalidOperationException>(() => motion.Add(new HealthState(1f), 0f, 6f, 1f, 1f));
        }

        // Halving and doubling are exact, so the scene's world positions give the simulation's floor distances bit for bit.
        [Test]
        public void FloorDistancesFromWorldPositionsMatchFloorCoordinatesExactly()
        {
            float[] values = { 0f, 0.1f, -1.3f, 2.71828f, 5.9999f, -0.00037f, 6.123456f, 1.1f };
            foreach (float x in values)
            {
                foreach (float y in values)
                {
                    float expected = x * x + y * y;
                    Assert.That(ArenaFloor.DistanceSquared(x - 0f, ArenaFloor.WorldY(y) - 0f), Is.EqualTo(expected), $"({x}, {y})");
                    foreach (float otherY in values)
                    {
                        float floorDy = y - otherY;
                        Assert.That(ArenaFloor.DistanceSquared(x, ArenaFloor.WorldY(y) - ArenaFloor.WorldY(otherY)),
                            Is.EqualTo(x * x + floorDy * floorDy), $"({x}, {y} - {otherY})");
                    }
                }
            }
            Assert.That(ArenaFloor.WorldY(4f), Is.EqualTo(2f), "Depth shows at half length on screen.");
        }
    }
}
