using System;
using Cryptforge.Art;
using Cryptforge.Progression;

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
        // Whether this chest also holds a book. A book is granted on top of the reward above rather than instead of it,
        // so the kind still says what the chest pays and whatever shows a chest opening needs to know nothing new.
        public readonly bool HoldsBook;

        public ChestReward(ChestRewardKind kind, float healFraction, int gold, bool holdsBook = false)
        {
            Kind = kind;
            HealFraction = healFraction;
            Gold = gold;
            HoldsBook = holdsBook;
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
        // How many chests in a hundred also hold a book, measured in docs/28 rather than chosen: a Descent places about
        // ten chests against eleven level-ups, so this share decides whether a build comes from the walk or from the
        // levels. The lowest share at which every measured run found at least one book, and the one that still lets a
        // hero who fetches them die. What it holds is about two books a Descent, not the percentage - a longer run
        // brings this number down with it (docs/28, and S6 in docs/27).
        public const int BookPercent = 25;

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

        // Even rooms heal, odd rooms pay: the first room's chest mends what the first pack cost. chestIndex counts the
        // chests placed across the whole run, not the rooms of one floor, so the run's seed decides each chest once.
        public static ChestReward RewardFor(int roomIndex, int chestIndex, int seed)
        {
            if (roomIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(roomIndex));
            bool book = HoldsBook(chestIndex, seed);
            return roomIndex % 2 == 0
                ? new ChestReward(ChestRewardKind.Heal, HealFraction, 0, book)
                : new ChestReward(ChestRewardKind.Gold, 0f, GoldReward, book);
        }

        // Whether the chest at this run ordinal also holds a book. Drawn from the chest stream, so a chest opened out of
        // order - or never opened at all - cannot move what the next one holds.
        public static bool HoldsBook(int chestIndex, int seed)
        {
            if (chestIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(chestIndex));
            // A share of zero holds no books at all, since no draw below a hundred is below nothing.
            return RunRandom.Stream(seed, RunRandom.Chests, chestIndex).NextBelow(100) < BookPercent;
        }

        public static bool IsWithinReach(float heroX, float heroY, float chestX, float chestY)
        {
            float dx = heroX - chestX;
            float dy = heroY - chestY;
            return dx * dx + dy * dy <= PickupRadius * PickupRadius;
        }
    }
}
