using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Cryptforge.Combat;
using Cryptforge.Economy;

namespace Cryptforge.Progression
{
    // What the equipped gear adds up to (docs/54 section 7): each slot's main stat by its basma level, plus every line,
    // summed per stat and clamped at the stat's cap; and the power index, DPS x EHP - 1. All in hundredths and integers, so
    // the scene, the simulation and the model read the same numbers.
    //
    // And what it does to a run (docs/54 phases 5 and 5b, section 20): Apply turns a loadout's clamped totals into the run's
    // own numbers. Damage, attack speed, area, health and move speed are a factor of their own on the final stat (the lead,
    // 2026-11-17: an item's "+14 %" is 14 % more whatever the tomes hold, ModifierOperation.Multiply); crit, armour and luck
    // are points added as the cards add theirs. The weapon's stats go to every weapon the hero holds or will hold
    // (WeaponLoadout.AddGlobal, the tomes' path), the hero's to HeroStats; damage vs elites and bosses multiplies every
    // weapon's elite and gate-boss shares (the Daggers' path); burn damage scales the hero's burns' ticks (BurnFactor, read
    // where a burn is lit); pickup radius widens GroundDrops' reach; ability cooldown shortens the ability's; healing
    // received scales every heal of the hero's HealthState. One body, never inlined, that CombatSetup and the Descent
    // simulation both call as the run begins: two copies of float arithmetic in two assemblies have already disagreed in
    // the last bit once (docs/21, the movement budget). An ungeared run (GearLoadout.None) is today's run to the bit: Apply
    // touches nothing.
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

        // ---- The run (docs/54 phases 5 and 5b) ----

        // How a gear stat reaches the run's stat sheet: the UpgradeStat whose path it rides and the operation - Multiply for
        // damage, attack speed, area, health and move speed (a factor of its own, the lead's decision of 2026-11-17), Flat
        // for crit, armour and luck (points, as the cards add theirs). False for the five stats that ride a rule of their own
        // rather than the sheet (Apply: damage vs elites and bosses, burn damage, pickup radius, ability cooldown, healing).
        public static bool RunStat(GearStat stat, out UpgradeStat runStat, out ModifierOperation operation)
        {
            switch (stat)
            {
                case GearStat.Damage:
                    runStat = UpgradeStat.Damage;
                    operation = ModifierOperation.Multiply;
                    return true;
                case GearStat.AttackSpeed:
                    runStat = UpgradeStat.AttackSpeed;
                    operation = ModifierOperation.Multiply;
                    return true;
                case GearStat.CritChance:
                    runStat = UpgradeStat.CritChance;
                    operation = ModifierOperation.Flat;
                    return true;
                case GearStat.MaxHealth:
                    runStat = UpgradeStat.MaxHealth;
                    operation = ModifierOperation.Multiply;
                    return true;
                case GearStat.Armour:
                    runStat = UpgradeStat.Armor;
                    operation = ModifierOperation.Flat;
                    return true;
                case GearStat.Area:
                    runStat = UpgradeStat.Area;
                    operation = ModifierOperation.Multiply;
                    return true;
                case GearStat.MoveSpeed:
                    runStat = UpgradeStat.MoveSpeed;
                    operation = ModifierOperation.Multiply;
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

        // The amount of a sheet modifier a clamped total makes, in its run stat's units: a percent as a fraction (1000,
        // +10 %, is 0.1; ModifiableStat multiplies by 1 + it), crit points as a chance (300, three points, is 0.03), luck in
        // points (500 is 5). Armour is the share of a hit taken away (section 7.1: 400 is four points, 4 % less), while the
        // run's armour is a pool with diminishing returns (HeroStats.DamageReduction = armour / (armour + ArmorSoftness)); the
        // points become the pool that takes exactly that share off an unarmoured hero, ArmorSoftness x r / (1 - r): 4.17
        // for four points. For the five rule stats, the fraction itself (Factor turns it into the factor).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float RunAmount(GearStat stat, int total)
        {
            switch (stat)
            {
                case GearStat.Armour:
                {
                    float share = total / 10000f;
                    return HeroStats.ArmorSoftness * share / (1f - share);
                }
                case GearStat.Luck:
                    return total / 100f;
                default:
                    return total / 10000f;
            }
        }

        // The factor a stat's clamped total makes on what it multiplies: 1 + the fraction (1.06 for +6 % damage vs elites),
        // and 1 - it for ability cooldown (0.9 at its 10 % cap); exactly 1 for a total of 0. Not for crit, armour or luck,
        // which add points. One body, never inlined, for Apply and for the burns the scene and the simulation light.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Factor(GearLoadout gear, GearStat stat)
        {
            if (stat == GearStat.CritChance || stat == GearStat.Armour || stat == GearStat.Luck)
                throw new ArgumentOutOfRangeException(nameof(stat), "Crit, armour and luck add points; they have no factor.");
            int total = gear != null ? gear.Total(stat) : 0;
            if (total <= 0)
                return 1f;
            float fraction = RunAmount(stat, total);
            return stat == GearStat.AbilityCooldown ? 1f - fraction : 1f + fraction;
        }

        // What every tick of the hero's burns is multiplied by (BurnState.DamageScale): read where a burn is lit - the
        // scene's Ember Memory (CombatSetup) and the simulation's - since a burn is made per enemy, long after the start.
        public static float BurnFactor(GearLoadout gear) => Factor(gear, GearStat.BurnDamage);

        // The loadout on the run, once, as it begins, in GearStat's order: each sheet stat with a value becomes one modifier -
        // the hero's on hero; the weapon's granted to every weapon of loadout (the foundry's, which replays it onto each
        // weapon picked up later, so a second weapon wears the gear too) or, where the run keeps no loadout (the hall
        // arena), onto weapon alone. Then the rules: damage vs elites and bosses on the loadout's shares (or the one
        // weapon's), healing received on health, ability cooldown on ability, pickup radius on drops. Any of the targets
        // may be null and is then left alone; burn damage is read where burns are lit (BurnFactor). An empty loadout, or a
        // stat at 0, touches nothing, so an ungeared run is today's to the bit. Returns how many stats it applied.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int Apply(GearLoadout gear, HeroStats hero, WeaponLoadout loadout, WeaponRuntime weapon = null,
            HealthState health = null, AbilityRuntime ability = null, GroundDrops drops = null)
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

            if (gear.Total(GearStat.EliteDamage) > 0 && (loadout != null || weapon != null))
            {
                float factor = Factor(gear, GearStat.EliteDamage);
                if (loadout != null)
                {
                    loadout.SetTargetShareFactor(factor);
                }
                else
                {
                    weapon.GateBossShare = WeaponLoadout.ShareOf(weapon.GateBossShare, factor);
                    weapon.EliteShare = WeaponLoadout.ShareOf(weapon.EliteShare, factor);
                }
                applied++;
            }
            if (gear.Total(GearStat.Healing) > 0 && health != null)
            {
                health.SetHealingReceived(Factor(gear, GearStat.Healing));
                applied++;
            }
            if (gear.Total(GearStat.AbilityCooldown) > 0 && ability != null)
            {
                ability.ShortenCooldown(Factor(gear, GearStat.AbilityCooldown));
                applied++;
            }
            if (gear.Total(GearStat.PickupRadius) > 0 && drops != null)
            {
                drops.SetPickupRadius(DropRule.PickupRadius * Factor(gear, GearStat.PickupRadius));
                applied++;
            }
            return applied;
        }
    }
}
