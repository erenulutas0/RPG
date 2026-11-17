using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cryptforge.Combat;

namespace Cryptforge.Progression
{
    // What the equipped gear adds up to (docs/54 section 7): each slot's main stat by its basma level, plus every line,
    // summed per stat and clamped at the stat's cap; and the power index, DPS x EHP - 1. All in hundredths and integers, so
    // the scene, the simulation and the model read the same numbers.
    //
    // And what it does to a run (docs/54 phase 5, section 20): Apply turns a loadout's clamped totals into modifiers on the
    // run's own stats, on the paths the cards and tomes already ride - the weapon's stats granted to every weapon the hero
    // holds or will hold (WeaponLoadout.AddGlobal, as a tome's are), the hero's on HeroStats. One body, never inlined, that
    // CombatSetup and the Descent simulation both call as the run begins: two copies of float arithmetic in two assemblies
    // have already disagreed in the last bit once (docs/21, the movement budget). A stat the run has no rule for yet is
    // inert (RunStat). An ungeared run (GearLoadout.None) is today's run to the bit: Apply adds nothing to it.
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

        // ---- The run (docs/54 phase 5) ----

        // How a gear stat reaches a run: the UpgradeStat whose path it rides and the operation, as a card carrying that stat
        // would. False for a stat the run has no rule for yet, which Apply leaves inert: damage vs elites and bosses (a
        // weapon's elite and gate-boss shares are fixed by its id, WeaponLoadout.TryAdd), burn damage (BurnRule's damage a
        // stack is a constant), pickup radius (GroundDrops.PickupRadius is a constant), ability cooldown (AbilityRuntime's
        // cooldown is fixed when it is made) and healing received (the chest's mend, the forge's heal and Second Wind read no
        // stat). Each would be a new rule of the run; phase 5 maps gear onto the rules there are.
        public static bool RunStat(GearStat stat, out UpgradeStat runStat, out ModifierOperation operation)
        {
            switch (stat)
            {
                case GearStat.Damage:
                    runStat = UpgradeStat.Damage;
                    operation = ModifierOperation.Percent;
                    return true;
                case GearStat.AttackSpeed:
                    runStat = UpgradeStat.AttackSpeed;
                    operation = ModifierOperation.Percent;
                    return true;
                case GearStat.CritChance:
                    runStat = UpgradeStat.CritChance;
                    operation = ModifierOperation.Flat;
                    return true;
                case GearStat.MaxHealth:
                    runStat = UpgradeStat.MaxHealth;
                    operation = ModifierOperation.Percent;
                    return true;
                case GearStat.Armour:
                    runStat = UpgradeStat.Armor;
                    operation = ModifierOperation.Flat;
                    return true;
                case GearStat.Area:
                    runStat = UpgradeStat.Area;
                    operation = ModifierOperation.Percent;
                    return true;
                case GearStat.MoveSpeed:
                    runStat = UpgradeStat.MoveSpeed;
                    operation = ModifierOperation.Percent;
                    return true;
                case GearStat.Luck:
                    runStat = UpgradeStat.Luck;
                    operation = ModifierOperation.Flat;
                    return true;
                default:
                    runStat = UpgradeStat.Damage;
                    operation = ModifierOperation.Flat;
                    return false;
            }
        }

        // The amount of the modifier a clamped total makes, in its run stat's units: a percent as a fraction (1000, +10 %,
        // is 0.1), crit points as a chance (300, three points, is 0.03), luck in points (500 is 5); 0 for an inert stat.
        // Armour is the share of a hit taken away (section 7.1: 400 is four points, 4 % less), while the run's armour is a
        // pool with diminishing returns (HeroStats.DamageReduction = armour / (armour + ArmorSoftness)); the points become
        // the pool that takes exactly that share off an unarmoured hero, ArmorSoftness x r / (1 - r): 4.17 for four
        // points. Beside armour from cards the same pool takes a little less off, as any armour there does.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float RunAmount(GearStat stat, int total)
        {
            switch (stat)
            {
                case GearStat.Damage:
                case GearStat.AttackSpeed:
                case GearStat.CritChance:
                case GearStat.MaxHealth:
                case GearStat.Area:
                case GearStat.MoveSpeed:
                    return total / 10000f;
                case GearStat.Armour:
                {
                    float share = total / 10000f;
                    return HeroStats.ArmorSoftness * share / (1f - share);
                }
                case GearStat.Luck:
                    return total / 100f;
                default:
                    return 0f;
            }
        }

        // The loadout on the run, once, as it begins: every stat with a value and a run rule becomes one modifier, in
        // GearStat's order - the hero's on hero; the weapon's granted to every weapon of loadout (the foundry's, which
        // replays it onto each weapon picked up later, so a second weapon wears the gear too) or, where the run keeps no
        // loadout (the hall arena), onto weapon alone. An empty loadout, or a stat at 0, adds no modifier at all, so an
        // ungeared run is today's to the bit. Returns how many modifiers it added.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Apply(GearLoadout gear, HeroStats hero, WeaponLoadout loadout, WeaponRuntime weapon = null)
        {
            if (gear == null || gear.IsEmpty)
                return 0;

            int applied = 0;
            for (int i = 0; i < GearStatIds.Count; i++)
            {
                var stat = (GearStat)i;
                int total = gear.Total(stat);
                if (total <= 0 || !RunStat(stat, out UpgradeStat runStat, out ModifierOperation operation))
                    continue;

                var modifier = new StatModifier(operation, RunAmount(stat, total));
                if (HeroStats.Owns(runStat))
                {
                    if (hero == null)
                        continue;
                    hero.AddModifier(runStat, modifier);
                }
                else if (loadout != null)
                {
                    loadout.AddGlobal(runStat, modifier);
                }
                else if (weapon != null)
                {
                    weapon.AddModifier(runStat, modifier);
                }
                else
                {
                    continue;
                }
                applied++;
            }
            return applied;
        }
    }
}
