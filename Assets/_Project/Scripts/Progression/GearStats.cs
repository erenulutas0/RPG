using System;
using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // What the equipped gear adds up to (docs/54 section 7): each slot's main stat by its basma level, plus every line,
    // summed per stat and clamped at the stat's cap; and the power index, DPS x EHP - 1. All in hundredths and integers, so
    // the scene, the simulation and the model read the same numbers. Not applied to a run yet: the run reads gear in docs/54
    // phase 5, through one body here, and until then every run - every shipped test included - plays ungeared.
    public static class GearStats
    {
        // A crit strikes for twice the damage (the power index's reading; the weapon's own multiplier is the run's).
        public const int CritMultiplierPercent = 200;

        // The main stat's value by basma level, in hundredths, for the weapon (damage %) and the armour (health %).
        private static readonly int[] TenPercentCurve = { 0, 100, 200, 300, 400, 500, 600, 700, 850, 1000 };
        // The amulet's attack speed %.
        private static readonly int[] FivePercentCurve = { 0, 50, 100, 150, 200, 250, 300, 350, 425, 500 };
        // The ring's crit chance, in points.
        private static readonly int[] CritCurve = { 0, 30, 60, 90, 120, 150, 180, 210, 255, 300 };

        // The whole gear's total per stat never counts past these (GearStat's order), main stats and lines together.
        private static readonly int[] Caps = { 1400, 700, 400, 1400, 400, 1200, 1500, 500, 600, 2500, 1000, 1500, 1000 };

        public static GearStat MainStatOf(GearSlot slot)
        {
            switch (slot)
            {
                case GearSlot.Weapon:
                    return GearStat.Damage;
                case GearSlot.Armour:
                    return GearStat.MaxHealth;
                case GearSlot.Amulet:
                    return GearStat.AttackSpeed;
                case GearSlot.Ring:
                    return GearStat.CritChance;
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static int MainValue(GearSlot slot, int level)
        {
            if (level < 0 || level > AnvilTable.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(level));
            switch (slot)
            {
                case GearSlot.Weapon:
                case GearSlot.Armour:
                    return TenPercentCurve[level];
                case GearSlot.Amulet:
                    return FivePercentCurve[level];
                case GearSlot.Ring:
                    return CritCurve[level];
                default:
                    throw new ArgumentOutOfRangeException(nameof(slot));
            }
        }

        public static int Cap(GearStat stat)
        {
            int index = (int)stat;
            if (index < 0 || index >= Caps.Length)
                throw new ArgumentOutOfRangeException(nameof(stat));
            return Caps[index];
        }

        // Per stat (GearStat's order), the sum of the pieces' main stats and lines; clamped at the caps unless asked not to.
        public static int[] Totals(IEnumerable<ItemInstance> pieces, bool clamp = true)
        {
            var totals = new int[GearStatIds.Count];
            if (pieces != null)
            {
                foreach (ItemInstance piece in pieces)
                {
                    if (piece == null)
                        continue;
                    totals[(int)piece.MainStat] += MainValue(piece.Slot, piece.Level);
                    for (int i = 0; i < piece.Lines.Count; i++)
                        totals[(int)piece.Lines[i].Stat] += piece.Lines[i].Value;
                }
            }
            if (clamp)
            {
                for (int i = 0; i < totals.Length; i++)
                    totals[i] = Math.Min(totals[i], Caps[i]);
            }
            return totals;
        }

        // The power index in hundredths of a percent (4200 is +42 %), from clamped totals:
        //   DPS = (1 + damage)(1 + attack speed)(1 + crit (multiplier - 1)),  EHP = (1 + health) / (1 - armour),
        //   power = DPS x EHP - 1.
        // Fixed point at 1/10000, each product rounded down, so the result is the same on every runtime.
        public static int Power(int[] totals)
        {
            if (totals == null || totals.Length != GearStatIds.Count)
                throw new ArgumentException("Totals per stat are required.", nameof(totals));
            const long one = 10000;
            long dps = (one + totals[(int)GearStat.Damage]) * (one + totals[(int)GearStat.AttackSpeed]) / one;
            dps = dps * (one + totals[(int)GearStat.CritChance] * (CritMultiplierPercent - 100) / 100) / one;
            long ehp = (one + totals[(int)GearStat.MaxHealth]) * one / (one - totals[(int)GearStat.Armour]);
            return (int)(dps * ehp / one - one);
        }
    }
}
