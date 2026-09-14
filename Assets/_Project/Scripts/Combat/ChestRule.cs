using System;
using Cryptforge.Art;

namespace Cryptforge.Combat
{
    public enum ChestRewardKind
    {
        Heal = 0,
        Gold = 1
    }

    public readonly struct ChestReward
    {
        public readonly ChestRewardKind Kind;
        // Fraction of maximum health restored, for a heal.
        public readonly float HealFraction;
        // Base gold before the floor modifier, for gold.
        public readonly int Gold;

        public ChestReward(ChestRewardKind kind, float healFraction, int gold)
        {
            Kind = kind;
            HealFraction = healFraction;
            Gold = gold;
        }
    }

    // Where a combat room's chest stands and what it holds. One chest per combat room, on a spot that circles the arena's
    // centre room by room, so the hero has to leave the middle to take it. Pure, so tests and the scene agree.
    public static class ChestRule
    {
        // How close the hero's feet must come to open the chest, in floor units.
        public const float PickupRadius = 0.75f;
        public const float HealFraction = 0.25f;
        public const int GoldReward = 10;
        // The chest's distance from the arena's centre, or less on a small platform: just inside the edge of what the
        // following camera shows (2.3 units either side of the hero, less the chest's own half width), so a chest is always in view.
        public const float PreferredDistance = 1.8f;

        public static void SpotFor(int roomIndex, ArenaGeometry geometry, out float floorX, out float floorY)
        {
            if (roomIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(roomIndex));

            float distance = Math.Min(PreferredDistance, Math.Min(geometry.HalfWidth, geometry.HalfDepth) * 0.45f);
            switch (roomIndex % 4)
            {
                case 0:
                    floorX = distance;
                    floorY = geometry.Middle;
                    break;
                case 1:
                    floorX = 0f;
                    floorY = geometry.Middle + distance;
                    break;
                case 2:
                    floorX = -distance;
                    floorY = geometry.Middle;
                    break;
                default:
                    floorX = 0f;
                    floorY = geometry.Middle - distance;
                    break;
            }
        }

        // Even rooms heal, odd rooms pay: the first room's chest mends what the first pack cost.
        public static ChestReward RewardFor(int roomIndex)
        {
            if (roomIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(roomIndex));
            return roomIndex % 2 == 0
                ? new ChestReward(ChestRewardKind.Heal, HealFraction, 0)
                : new ChestReward(ChestRewardKind.Gold, 0f, GoldReward);
        }

        public static bool IsWithinReach(float heroX, float heroY, float chestX, float chestY)
        {
            float dx = heroX - chestX;
            float dy = heroY - chestY;
            return dx * dx + dy * dy <= PickupRadius * PickupRadius;
        }
    }
}
