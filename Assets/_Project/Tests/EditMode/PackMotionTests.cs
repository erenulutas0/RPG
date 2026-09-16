using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class PackMotionTests
    {
        private const float Frame = 1f / 60f;
        // EncounterController's _bodySpacing in Gameplay.unity: the closest two enemies may stand, and the spawn separation.
        private const float BodySpacing = 0.9f;
        private static readonly ArenaGeometry Arena = new ArenaGeometry(-9f, 9f, 9f);
        // A sweep of a grid in tenths lands some spots exactly on the rim's margin line, where whether the spot counts as on
        // the platform comes down to the last bit of a float and the Editor's runtime and the pure test runner disagree. The
        // sweeps keep this much clearance so they cover the same spots on both; the rim itself is swept by name below.
        private const float RimTieClearance = 0.001f;

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
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Place(0, PackLayout.MaxPackSize + 1, 0f, 0f, Arena, 5f, 1f,
                new EntryPlacement[PackLayout.MaxPackSize + 1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Place(0, 3, 0f, 0f, Arena, 5f, 1f, new EntryPlacement[2]));
        }

        // Away from the centre an enemy whose spot lies beyond the rim starts short of it, if that is still far enough from
        // the hero; at the rim there is no such room, so the corners on that side close and the open ones take their
        // enemies. Every enemy starts on the platform, out of reach, apart from the others.
        [Test]
        public void AtTheRimCornersOverTheVoidCloseAndThePackEntersFromWhereThePlatformIs()
        {
            var placements = new EntryPlacement[PackLayout.MaxPackSize];
            float rim = 9f - HeroMotion.EdgeMargin;
            AssertEntries(rim, 0f, 1 << (int)EntrySide.Left, "at the right corner");
            AssertEntries(6.2f, 0f, 1 << (int)EntrySide.Left, "most of the way to the right corner");
            AssertEntries(rim / 2f, rim / 2f, (1 << (int)EntrySide.Near) | (1 << (int)EntrySide.Left), "on the far-right edge");
            AssertEntries(0f, -rim, 1 << (int)EntrySide.Far, "at the near corner");
            AssertEntries(4.5f, 0f, EntrySides.AllSides, "halfway to the right corner, where every corner still has room");
            AssertEntries(2f, 0f, EntrySides.AllSides, "two units right of the centre, where every corner still fits");

            // Halfway to the right corner the right corner's enemy starts short of the rim, nearer than 5 but at least 3 away.
            EntrySides.Place(0, 4, 4.5f, 0f, Arena, 5f, 1f, placements);
            Assert.That(placements[1].Side, Is.EqualTo(EntrySide.Right));
            Assert.That(placements[1].X - 4.5f, Is.LessThan(5f).And.GreaterThanOrEqualTo(EntrySides.MinimumEntryDistance), "Drawn in short of the rim.");
            Assert.That(placements[1].Y, Is.EqualTo(0f));
            Assert.That(placements[3].Side, Is.EqualTo(EntrySide.Left));
            Assert.That(placements[3].X, Is.EqualTo(-0.5f), "The left corner's spot is on the platform and stays where it was.");

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
                            float minimum = EntrySides.MinimumEntryDistance;
                            Assert.That(dx * dx + dy * dy, Is.GreaterThanOrEqualTo(minimum * minimum - 1e-3f), where + " starts out of reach.");
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

        // The spawn separation: wherever the hero stands, every enemy of a wave of up to ten starts on the platform, out of
        // reach of the hero and at least a body's spacing from the enemies placed before it. Pulling a spot in short of the
        // rim can crowd it against another corner's spot, so the separation closes that corner too and the wave enters from
        // the corners that have room.
        [Test]
        public void WithTheSpawnSeparationEveryPackOfUpToTenStartsOnThePlatformOutOfReachAndApart()
        {
            float rim = 9f - HeroMotion.EdgeMargin;
            AssertSeparatedEntries(0f, 0f, "at the centre");
            AssertSeparatedEntries(4.5f, 0f, "halfway to the right corner");
            AssertSeparatedEntries(6.2f, 0f, "most of the way to the right corner");
            AssertSeparatedEntries(rim, 0f, "at the right corner");
            AssertSeparatedEntries(rim / 2f, rim / 2f, "on the far-right edge");
            AssertSeparatedEntries(0f, -rim, "at the near corner");

            // Every spot the hero can stand on, in steps of 0.3 floor units, for every wave and pack size.
            int failures = 0;
            string first = null;
            var swept = new EntryPlacement[PackLayout.MaxPackSize];
            for (int tenthsX = -84; tenthsX <= 84; tenthsX += 3)
            {
                for (int tenthsY = -84; tenthsY <= 84; tenthsY += 3)
                {
                    float heroX = tenthsX / 10f;
                    float heroY = tenthsY / 10f;
                    if (!Arena.IsOnPlatform(heroX, heroY, HeroMotion.EdgeMargin + RimTieClearance))
                        continue;
                    for (int wave = 0; wave < EntrySides.Count; wave++)
                    {
                        for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                        {
                            EntrySides.Place(wave, count, heroX, heroY, Arena, 5f, 1f, BodySpacing, swept);
                            string why = FirstBrokenEntry(swept, count, heroX, heroY);
                            if (why == null)
                                continue;
                            failures++;
                            first ??= $"Hero ({heroX}, {heroY}), wave {wave}, {count} enemies: {why}";
                        }
                    }
                }
            }
            Assert.That(failures, Is.Zero, $"Swept spawns broke a guarantee, first at {first}.");

            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Place(0, 1, 0f, 0f, Arena, 5f, 1f, -0.1f,
                new EntryPlacement[1]));
            Assert.Throws<ArgumentOutOfRangeException>(() => EntrySides.Place(0, 1, 0f, 0f, Arena, 5f, 1f, float.NaN,
                new EntryPlacement[1]));

            void AssertSeparatedEntries(float heroX, float heroY, string hero)
            {
                var placements = new EntryPlacement[PackLayout.MaxPackSize];
                for (int wave = 0; wave < EntrySides.Count; wave++)
                {
                    for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                    {
                        EntrySides.Place(wave, count, heroX, heroY, Arena, 5f, 1f, BodySpacing, placements);
                        Assert.That(FirstBrokenEntry(placements, count, heroX, heroY), Is.Null,
                            $"Hero {hero}, wave {wave}, {count} enemies");
                    }
                }
            }
        }

        // With the hero at the centre every corner has room for every pack size, so the separation closes nothing and the
        // wave enters exactly where it entered without it: the stationary balance and the scene parity cases do not move.
        [Test]
        public void AtTheCentreTheSpawnSeparationPlacesEveryPackExactlyWhereItWasPlacedBefore()
        {
            var plain = new EntryPlacement[PackLayout.MaxPackSize];
            var separated = new EntryPlacement[PackLayout.MaxPackSize];
            for (int wave = 0; wave < EntrySides.Count; wave++)
            {
                for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                {
                    EntrySides.Place(wave, count, 0f, 0f, Arena, 5f, 1f, plain);
                    EntrySides.Place(wave, count, 0f, 0f, Arena, 5f, 1f, BodySpacing, separated);
                    for (int slot = 0; slot < count; slot++)
                    {
                        string where = $"wave {wave}, {count} enemies, slot {slot}";
                        Assert.That(separated[slot].Side, Is.EqualTo(plain[slot].Side), where);
                        Assert.That(separated[slot].Lateral, Is.EqualTo(plain[slot].Lateral), where);
                        Assert.That((separated[slot].X, separated[slot].Y), Is.EqualTo((plain[slot].X, plain[slot].Y)), where);
                    }
                }
            }

            // Two units left of the near-left edge a pulled-in far-corner spot lands half a body from a right-corner one;
            // with the separation the crowded corner closes and the whole wave enters from the right corner instead.
            EntrySides.Place(0, 7, -6.4f, -1.8f, Arena, 5f, 1f, plain);
            Assert.That(SmallestGap(plain, 7), Is.LessThan(BodySpacing), "Without the separation two enemies start half a body apart.");
            EntrySides.Place(0, 7, -6.4f, -1.8f, Arena, 5f, 1f, BodySpacing, separated);
            Assert.That(SmallestGap(separated, 7), Is.GreaterThanOrEqualTo(BodySpacing));
            for (int slot = 0; slot < 7; slot++)
                Assert.That(separated[slot].Side, Is.EqualTo(EntrySide.Right), $"Slot {slot} enters from the one corner with room.");
        }

        // What the separation costs the game as it is authored: nothing at all. Every wave in Data/Floors holds five enemies
        // or fewer, and up to six the separation never moves a single spot, wherever the hero stands - so no encounter a
        // player can reach today enters anywhere but where it always did, whether the hero walks or never moves. From seven
        // up it does move layouts, and exactly the ones it has to: a layout moves if and only if the plain one would have
        // started two bodies overlapping, and every layout it moves it also separates.
        [Test]
        public void TheSpawnSeparationMovesAWaveOnlyWhereThePlainOneWouldStartOverlapping()
        {
            var plain = new EntryPlacement[PackLayout.MaxPackSize];
            var separated = new EntryPlacement[PackLayout.MaxPackSize];
            int moved = 0;
            int overlapping = 0;
            string firstDisagreement = null;
            for (int tenthsX = -84; tenthsX <= 84; tenthsX += 3)
            {
                for (int tenthsY = -84; tenthsY <= 84; tenthsY += 3)
                {
                    float heroX = tenthsX / 10f;
                    float heroY = tenthsY / 10f;
                    if (!Arena.IsOnPlatform(heroX, heroY, HeroMotion.EdgeMargin + RimTieClearance))
                        continue;
                    for (int wave = 0; wave < EntrySides.Count; wave++)
                    {
                        for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                        {
                            EntrySides.Place(wave, count, heroX, heroY, Arena, 5f, 1f, plain);
                            EntrySides.Place(wave, count, heroX, heroY, Arena, 5f, 1f, BodySpacing, separated);
                            bool differs = false;
                            for (int slot = 0; slot < count && !differs; slot++)
                                differs = separated[slot].Side != plain[slot].Side || separated[slot].X != plain[slot].X ||
                                    separated[slot].Y != plain[slot].Y;
                            string where = $"Hero ({heroX}, {heroY}), wave {wave}, {count} enemies";
                            Assert.That(differs && count <= 6, Is.False,
                                where + ": the separation moved a wave of six or fewer, which every authored wave is.");

                            bool overlaps = count > 1 && SmallestGap(plain, count) < BodySpacing;
                            if (differs)
                                moved++;
                            if (overlaps)
                                overlapping++;
                            if (differs != overlaps)
                                firstDisagreement ??= where + (differs
                                    ? ": moved a wave that was already a body apart."
                                    : ": left a wave that starts overlapping.");
                            if (differs)
                                Assert.That(SmallestGap(separated, count), Is.GreaterThanOrEqualTo(BodySpacing), where);
                        }
                    }
                }
            }
            Assert.That(firstDisagreement, Is.Null, "Moving and overlapping disagree, first at " + firstDisagreement);
            Assert.That(moved, Is.EqualTo(overlapping));
            Assert.That(moved, Is.GreaterThan(0), "Packs of seven and more do need the separation somewhere.");
        }

        // Ten enemies, two Grunts in front of eight Cinder Mites, walk in from where the separated wave placed them: with the
        // hero at the right corner the whole pack forms up on one corner, at the centre it comes from all four. No enemy ever
        // steps inside the spacing of an enemy nearer the hero, and none ever leaves the platform.
        [Test]
        public void ATenPackWalksInWithoutAnyEnemySteppingIntoTheSpacingOfANearerOne()
        {
            WalkInTenPack(9f - HeroMotion.EdgeMargin, 0f, -1f, 0f, "from the right corner");
            WalkInTenPack(0f, 0f, 1f, 0f, "from the centre");
        }

        private static void WalkInTenPack(float startX, float startY, float steerX, float steerY, string where)
        {
            const int count = PackLayout.MaxPackSize;
            var placements = new EntryPlacement[count];
            EntrySides.Place(0, count, startX, startY, Arena, 5f, 1f, BodySpacing, placements);
            var hero = new HeroMotion(2.5f);
            hero.Place(startX, startY, Arena);
            var motion = new PackMotion(BodySpacing, PackLayout.HalfWidth, Arena, startX, startY);
            var bodies = new HealthState[count];
            for (int i = 0; i < count; i++)
            {
                bodies[i] = new HealthState(i < 2 ? 50f : 15f);
                motion.Add(bodies[i], placements[i], i < 2 ? 1.6f : 2.2f, 1.5f);
            }

            var beforeX = new float[count];
            var beforeY = new float[count];
            var order = new int[count];
            int moves = 0;
            for (int frame = 0; frame < 900; frame++)
            {
                if (frame == 300)
                    bodies[0].ApplyDamage(new DamageContext(50f));
                if (frame >= 480 && frame < 600)
                    hero.Move(steerX, steerY, Frame, Arena);
                float heroX = hero.X;
                float heroY = hero.Y;

                // The living enemies nearest the hero first, an earlier slot first on equal distance: PackMotion's step order.
                int living = 0;
                for (int i = 0; i < count; i++)
                {
                    beforeX[i] = motion.XOf(i);
                    beforeY[i] = motion.YOf(i);
                    if (!bodies[i].IsAlive)
                        continue;
                    float distance = Squared(beforeX[i] - heroX) + Squared(beforeY[i] - heroY);
                    int insertAt = living;
                    while (insertAt > 0 && Squared(beforeX[order[insertAt - 1]] - heroX) + Squared(beforeY[order[insertAt - 1]] - heroY) > distance)
                    {
                        order[insertAt] = order[insertAt - 1];
                        insertAt--;
                    }
                    order[insertAt] = i;
                    living++;
                }

                motion.Step(Frame, heroX, heroY);
                for (int k = 0; k < living; k++)
                {
                    int i = order[k];
                    if (motion.XOf(i) == beforeX[i] && motion.YOf(i) == beforeY[i])
                        continue;
                    moves++;
                    Assert.That(Arena.IsOnPlatform(motion.XOf(i), motion.YOf(i), HeroMotion.EdgeMargin - 1e-3f), Is.True,
                        $"Walking in {where}, frame {frame}, slot {i} stays on the platform.");
                    for (int nearer = 0; nearer < k; nearer++)
                    {
                        int other = order[nearer];
                        float dx = motion.XOf(other) - motion.XOf(i);
                        float dy = motion.YOf(other) - motion.YOf(i);
                        Assert.That(dx * dx + dy * dy, Is.GreaterThanOrEqualTo(BodySpacing * BodySpacing),
                            $"Walking in {where}, frame {frame}: slot {i} stepped inside the spacing of slot {other}.");
                    }
                }
            }

            Assert.That(moves, Is.GreaterThan(0), $"The pack walked in {where}.");
            int arrived = 0;
            for (int i = 0; i < count; i++)
            {
                if (bodies[i].IsAlive && motion.HasArrived(i))
                    arrived++;
            }
            // Only as many enemies as the ring at their reach holds, three of the nine here, stand in reach at once; the rest
            // queue behind them at their spacing. The spacing rule only keeps an enemy out of the ring of the enemies nearer
            // the hero, so a nearer one may still step within a body of a farther one standing still.
            Assert.That(arrived, Is.GreaterThanOrEqualTo(3), $"The front of the pack reaches the hero {where}.");
        }

        // Why an entry breaks the spawn guarantees, or null when it keeps them: on the platform inside the hero's rim margin,
        // at least MinimumEntryDistance from the hero and a body's spacing from every earlier entry.
        private static string FirstBrokenEntry(EntryPlacement[] placements, int count, float heroX, float heroY)
        {
            for (int slot = 0; slot < count; slot++)
            {
                EntryPlacement entry = placements[slot];
                if (!Arena.IsOnPlatform(entry.X, entry.Y, HeroMotion.EdgeMargin))
                    return $"slot {slot} starts off the platform";
                float dx = entry.X - heroX;
                float dy = entry.Y - heroY;
                if (dx * dx + dy * dy < EntrySides.MinimumEntryDistance * EntrySides.MinimumEntryDistance - 1e-3f)
                    return $"slot {slot} starts within reach of the hero";
                for (int other = 0; other < slot; other++)
                {
                    float apartX = entry.X - placements[other].X;
                    float apartY = entry.Y - placements[other].Y;
                    if (apartX * apartX + apartY * apartY < BodySpacing * BodySpacing)
                        return $"slot {slot} starts inside slot {other}'s spacing";
                }
            }
            return null;
        }

        // The closest two entries of a wave stand, in floor units.
        private static float SmallestGap(EntryPlacement[] placements, int count)
        {
            float smallest = float.MaxValue;
            for (int slot = 1; slot < count; slot++)
            {
                for (int other = 0; other < slot; other++)
                {
                    float dx = placements[slot].X - placements[other].X;
                    float dy = placements[slot].Y - placements[other].Y;
                    smallest = Math.Min(smallest, (float)Math.Sqrt(dx * dx + dy * dy));
                }
            }
            return smallest;
        }

        private static float Squared(float value) => value * value;

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
