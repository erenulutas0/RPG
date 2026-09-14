using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class ChestRuleTests
    {
        private static readonly ArenaGeometry Arena = new ArenaGeometry(-9f, 9f, 9f);

        [Test]
        public void ChestsCircleTheCentreRoomByRoomAndStayOnThePlatform()
        {
            ChestRule.SpotFor(0, Arena, out float x, out float y);
            Assert.That((x, y), Is.EqualTo((1.8f, 0f)), "The first room's chest waits to the right, just inside the camera's edge.");
            ChestRule.SpotFor(1, Arena, out x, out y);
            Assert.That((x, y), Is.EqualTo((0f, 1.8f)));
            ChestRule.SpotFor(2, Arena, out x, out y);
            Assert.That((x, y), Is.EqualTo((-1.8f, 0f)));
            ChestRule.SpotFor(3, Arena, out x, out y);
            Assert.That((x, y), Is.EqualTo((0f, -1.8f)));
            ChestRule.SpotFor(4, Arena, out x, out y);
            Assert.That((x, y), Is.EqualTo((1.8f, 0f)), "Round again.");
            for (int room = 0; room < 8; room++)
            {
                ChestRule.SpotFor(room, Arena, out x, out y);
                Assert.That(Arena.IsOnPlatform(x, y, HeroMotion.EdgeMargin), Is.True, $"Room {room}'s chest can be reached.");
            }

            var small = new ArenaGeometry(-3f, 3f, 3f);
            ChestRule.SpotFor(0, small, out x, out _);
            Assert.That(x, Is.EqualTo(1.35f).Within(1e-5f), "A small platform keeps its chests nearer.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ChestRule.SpotFor(-1, Arena, out _, out _));
        }

        [Test]
        public void EvenRoomsHealAndOddRoomsPayAndTheHeroMustStandOnTheChest()
        {
            ChestReward first = ChestRule.RewardFor(0);
            Assert.That(first.Kind, Is.EqualTo(ChestRewardKind.Heal));
            Assert.That(first.HealFraction, Is.EqualTo(0.25f));
            ChestReward second = ChestRule.RewardFor(1);
            Assert.That(second.Kind, Is.EqualTo(ChestRewardKind.Gold));
            Assert.That(second.Gold, Is.EqualTo(10));
            Assert.That(ChestRule.RewardFor(4).Kind, Is.EqualTo(ChestRewardKind.Heal));

            Assert.That(ChestRule.IsWithinReach(1.3f, 0.3f, 1.8f, 0f), Is.True, "Half a unit away opens it.");
            Assert.That(ChestRule.IsWithinReach(0.8f, 0f, 1.8f, 0f), Is.False, "A full unit away does not.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ChestRule.RewardFor(-1));
        }
    }
}
