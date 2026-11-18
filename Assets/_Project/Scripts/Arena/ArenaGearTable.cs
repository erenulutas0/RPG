using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // The gear's numbers as the arena resolves a snapshot (docs/54 sections 4 and 6; GearStats and EfsunRule in the game):
    // each slot's main stat by its + level, each efsun line's value by its tier, each stat's cap over the whole gear, and
    // the power index. A copy, because this assembly references nothing of the game's (a server module compiles it
    // alone); integer tables, not arithmetic, and ArenaTablesTests compares every entry with the game's own, so a retune
    // of the game's gear that is not carried here fails a test instead of drifting. The arena's rules hash covers them:
    // a gear retune moves stored snapshots as a rules change, which is what docs/55 section 3.6 asks.
    public static class ArenaGearTable
    {
        public const int StatCount = 13;
        public const int SlotCount = 4;
        public const int MaxLevel = 9;
        public const int TierCount = 5;
        public const int MaxLines = 5;
        // GearSlot's order: the weapon, the armour, the amulet, the ring.
        public const int WeaponSlot = 0;
        public const int ArmourSlot = 1;
        public const int AmuletSlot = 2;
        public const int RingSlot = 3;

        private static readonly string[] Ids =
        {
            "damage", "attack_speed", "crit", "health", "armour", "elite_damage", "burn_damage", "area", "move_speed",
            "pickup_radius", "ability_cooldown", "healing", "luck"
        };

        private static readonly int[] TenPercentCurve = { 0, 100, 200, 300, 400, 500, 600, 700, 850, 1000 };
        private static readonly int[] FivePercentCurve = { 0, 50, 100, 150, 200, 250, 300, 350, 425, 500 };
        private static readonly int[] CritCurve = { 0, 30, 60, 90, 120, 150, 180, 210, 255, 300 };
        private static readonly int[] Caps = { 1400, 700, 400, 1400, 400, 1200, 1500, 500, 600, 2500, 1000, 1500, 1000 };

        private static readonly int[][] LineValues =
        {
            new[] { 100, 150, 200, 250, 300 },
            new[] { 50, 100, 150, 200, 250 },
            new[] { 40, 60, 80, 100, 120 },
            new[] { 100, 150, 200, 250, 300 },
            new[] { 40, 60, 80, 100, 120 },
            new[] { 200, 300, 400, 500, 600 },
            new[] { 300, 450, 600, 750, 900 },
            new[] { 100, 150, 200, 250, 300 },
            new[] { 100, 150, 200, 250, 300 },
            new[] { 400, 600, 800, 1000, 1200 },
            new[] { 200, 300, 400, 500, 600 },
            new[] { 300, 450, 600, 750, 900 },
            new[] { 100, 200, 300, 400, 500 }
        };

        public static string Id(ArenaStat stat)
        {
            int index = (int)stat;
            if (index < 0 || index >= Ids.Length)
                throw new ArgumentOutOfRangeException(nameof(stat));
            return Ids[index];
        }

        // False for an empty or unknown id, which a snapshot then drops.
        public static bool TryParse(string id, out ArenaStat stat)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (string.Equals(Ids[i], id, StringComparison.Ordinal))
                {
                    stat = (ArenaStat)i;
                    return true;
                }
            }
            stat = ArenaStat.Damage;
            return false;
        }

        public static ArenaStat MainStatOf(int slot)
        {
            switch (slot)
            {
                case WeaponSlot:
                    return ArenaStat.Damage;
                case ArmourSlot:
                    return ArenaStat.MaxHealth;
                case AmuletSlot:
                    return ArenaStat.AttackSpeed;
                case RingSlot:
                    return ArenaStat.CritChance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        // The main stat's value at a + level, in hundredths (a percent, or points of crit).
        public static int MainValue(int slot, int level)
        {
            if (level < 0 || level > MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            switch (slot)
            {
                case WeaponSlot:
                case ArmourSlot:
                    return TenPercentCurve[level];
                case AmuletSlot:
                    return FivePercentCurve[level];
                case RingSlot:
                    return CritCurve[level];
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static int LineValue(ArenaStat stat, int tier)
        {
            int index = (int)stat;
            if (index < 0 || index >= LineValues.Length)
                throw new ArgumentOutOfRangeException(nameof(stat));
            if (tier < 1 || tier > TierCount)
                throw new ArgumentOutOfRangeException(nameof(tier));
            return LineValues[index][tier - 1];
        }

        public static int Cap(ArenaStat stat)
        {
            int index = (int)stat;
            if (index < 0 || index >= Caps.Length)
                throw new ArgumentOutOfRangeException(nameof(stat));
            return Caps[index];
        }

        // Per stat, the pieces' main stats and lines summed and clamped at the caps (GearStats.Totals' reading).
        public static int[] Totals(IEnumerable<ArenaGearPiece> pieces)
        {
            var totals = new int[StatCount];
            if (pieces != null)
            {
                foreach (ArenaGearPiece piece in pieces)
                {
                    if (piece == null)
                        continue;
                    totals[(int)MainStatOf(piece.Slot)] += MainValue(piece.Slot, piece.Level);
                    for (int i = 0; i < piece.Lines.Count; i++)
                        totals[(int)piece.Lines[i].Stat] += LineValue(piece.Lines[i].Stat, piece.Lines[i].Tier);
                }
            }
            for (int i = 0; i < totals.Length; i++)
                totals[i] = Math.Min(totals[i], Caps[i]);
            return totals;
        }

        // The power index in hundredths of a percent (GearStats.Power, docs/54 section 7.1): DPS x EHP - 1, fixed point.
        public static int Power(int[] totals)
        {
            if (totals == null || totals.Length != StatCount)
                throw new ArgumentException("Totals per stat are required.", nameof(totals));
            const long one = 10000;
            long dps = (one + totals[(int)ArenaStat.Damage]) * (one + totals[(int)ArenaStat.AttackSpeed]) / one;
            dps = dps * (one + totals[(int)ArenaStat.CritChance]) / one;
            long ehp = (one + totals[(int)ArenaStat.MaxHealth]) * one / (one - totals[(int)ArenaStat.Armour]);
            return (int)(dps * ehp / one - one);
        }

        // For the rules hash: every entry in a fixed order.
        internal static void HashInto(ref ulong hash)
        {
            for (int i = 0; i < Ids.Length; i++)
                hash = ArenaHash.Mix(hash, Ids[i]);
            foreach (int[] table in new[] { TenPercentCurve, FivePercentCurve, CritCurve, Caps })
                for (int i = 0; i < table.Length; i++)
                    hash = ArenaHash.Mix(hash, table[i]);
            for (int s = 0; s < LineValues.Length; s++)
                for (int t = 0; t < LineValues[s].Length; t++)
                    hash = ArenaHash.Mix(hash, LineValues[s][t]);
        }
    }
}
