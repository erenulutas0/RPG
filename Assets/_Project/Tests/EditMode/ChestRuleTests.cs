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
            ChestReward first = ChestRule.RewardFor(0, 0, 0);
            Assert.That(first.Kind, Is.EqualTo(ChestRewardKind.Heal));
            Assert.That(first.HealFraction, Is.EqualTo(0.25f));
            ChestReward second = ChestRule.RewardFor(1, 1, 0);
            Assert.That(second.Kind, Is.EqualTo(ChestRewardKind.Gold));
            Assert.That(second.Gold, Is.EqualTo(10));
            Assert.That(ChestRule.RewardFor(4, 4, 0).Kind, Is.EqualTo(ChestRewardKind.Heal));

            Assert.That(ChestRule.IsWithinReach(1.3f, 0.3f, 1.8f, 0f), Is.True, "Half a unit away opens it.");
            Assert.That(ChestRule.IsWithinReach(0.8f, 0f, 1.8f, 0f), Is.False, "A full unit away does not.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ChestRule.RewardFor(-1, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ChestRule.RewardFor(0, -1, 0));
        }

        [Test]
        public void ABookRidesOnTopOfTheRewardAtTheShareTheRuleClaims()
        {
            // A book never replaces what a chest pays, so no pinned Descent number moves because a chest held one.
            for (int chest = 0; chest < 12; chest++)
            {
                ChestReward reward = ChestRule.RewardFor(chest % 6, chest, 7);
                Assert.That(reward.Kind, Is.EqualTo(chest % 6 % 2 == 0 ? ChestRewardKind.Heal : ChestRewardKind.Gold));
                Assert.That(reward.HoldsBook, Is.EqualTo(ChestRule.HoldsBook(chest, 7)));
            }

            // The share over many chests of many runs, within the slack a draw of this size leaves.
            int books = 0;
            const int Seeds = 400;
            const int Chests = 12;
            for (int seed = 0; seed < Seeds; seed++)
            {
                for (int chest = 0; chest < Chests; chest++)
                {
                    if (ChestRule.HoldsBook(chest, seed))
                        books++;
                }
            }
            double share = books * 100.0 / (Seeds * Chests);
            Assert.That(share, Is.EqualTo(ChestRule.BookPercent).Within(3.0),
                $"{books} books over {Seeds * Chests} chests is {share:0.0}%, against a claimed {ChestRule.BookPercent}%.");
        }

        [Test]
        public void WhatAChestHoldsIsFixedByTheRunAndNotByTheOrderChestsAreOpened()
        {
            for (int chest = 0; chest < 12; chest++)
            {
                Assert.That(ChestRule.HoldsBook(chest, 11), Is.EqualTo(ChestRule.HoldsBook(chest, 11)),
                    "The same chest of the same run always holds the same thing.");
            }

            // Two runs disagree about at least one chest, or the seed is doing nothing.
            bool differs = false;
            for (int chest = 0; chest < 12 && !differs; chest++)
                differs = ChestRule.HoldsBook(chest, 11) != ChestRule.HoldsBook(chest, 12);
            Assert.That(differs, Is.True, "Two seeds lay their books out differently.");
        }
    }
}
