using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // Generated rivals for P0's local arena (docs/55 section 6), labelled as such: ranked rivals are varied builds (a hero,
    // a weapon it carries, a policy, distinct line types from its pool) - as strong as anyone under normalisation; open
    // rivals add gear made to a fixed power index, so the open ladder climbs from ungeared to the gear's ceiling. Every
    // draw comes from the arena save's seed, one index a rival.
    public static class ArenaRivals
    {
        // The open ladder's top: about docs/54 section 7.2's ceiling (+50.6 %).
        public const int TopGearPower = 5000;

        // Placeholder names for the view to replace (content, not design).
        private static readonly string[] Names =
        {
            "Brann", "Sela", "Orrin", "Maelis", "Tavik", "Dunra", "Kesh", "Ilbet", "Rowan", "Vesk", "Harro", "Neve", "Cael",
            "Brisa", "Odo", "Thera", "Garn", "Lio", "Mira", "Senn", "Ulla", "Pell", "Yara", "Dax", "Fenn", "Ivo", "Jory",
            "Kael", "Lune", "Quill"
        };

        public static string Name(int index) => Names[((index % Names.Length) + Names.Length) % Names.Length];

        // A ranked rival: the identity drawn, the line types RankedLines distinct ones of the hero's pool.
        public static ArenaBuild Ranked(int seed, int index, ArenaRules rules = null)
        {
            rules = rules ?? ArenaRules.Current;
            ArenaRandom dice = ArenaRandom.Stream(seed, ArenaRandom.Rivals, index);
            ArenaHero hero = rules.Heroes[dice.NextBelow(rules.Heroes.Count)];
            string weapon = hero.WeaponIds[dice.NextBelow(hero.WeaponIds.Count)];
            var policy = (ArenaPolicy)dice.NextBelow(2);
            List<ArenaStat> pool = rules.RankedPool(hero);
            var lines = new List<ArenaStat>(rules.RankedLines);
            while (lines.Count < rules.RankedLines && pool.Count > 0)
            {
                int pick = dice.NextBelow(pool.Count);
                lines.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            return ArenaBuild.Ranked(hero.Id, weapon, hero.SkillIds, policy, lines, rules.Version);
        }

        // An open rival: the same kind of identity, wearing gear whose power index is as near the target (hundredths of a
        // percent) as the gear's tables allow from below.
        public static ArenaBuild Open(int seed, int index, int targetPower, ArenaRules rules = null)
        {
            ArenaBuild identity = Ranked(seed, index, rules);
            return identity.AsOpen(GearAt(targetPower));
        }

        // Gear at a power index: all four slots at the highest common + level within the target, then power lines (never a
        // slot's main stat, four a piece at most) at the highest tier that still fits, in a fixed order, until none fits.
        public static List<ArenaGearPiece> GearAt(int targetPower)
        {
            int level = 0;
            for (int l = ArenaGearTable.MaxLevel; l >= 0; l--)
            {
                if (ArenaGearTable.Power(ArenaGearTable.Totals(Pieces(l, null))) <= targetPower)
                {
                    level = l;
                    break;
                }
            }
            var lines = new List<ArenaGearLine>[ArenaGearTable.SlotCount];
            for (int slot = 0; slot < lines.Length; slot++)
                lines[slot] = new List<ArenaGearLine>();
            ArenaStat[] order = { ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.CritChance, ArenaStat.AttackSpeed, ArenaStat.Armour };
            bool added = true;
            while (added)
            {
                added = false;
                for (int slot = 0; slot < ArenaGearTable.SlotCount && !added; slot++)
                {
                    if (lines[slot].Count >= ArenaGearTable.MaxLines - 1)
                        continue;
                    foreach (ArenaStat stat in order)
                    {
                        if (stat == ArenaGearTable.MainStatOf(slot) || lines[slot].Exists(l => l.Stat == stat))
                            continue;
                        int[] before = ArenaGearTable.Totals(Pieces(level, lines));
                        for (int tier = ArenaGearTable.TierCount; tier >= 1; tier--)
                        {
                            lines[slot].Add(new ArenaGearLine(stat, tier));
                            int[] after = ArenaGearTable.Totals(Pieces(level, lines));
                            // A line the caps swallow whole adds nothing; one past the target is taken back.
                            if (ArenaGearTable.Power(after) <= targetPower && ArenaGearTable.Power(after) > ArenaGearTable.Power(before))
                            {
                                added = true;
                                break;
                            }
                            lines[slot].RemoveAt(lines[slot].Count - 1);
                        }
                        if (added)
                            break;
                    }
                }
            }
            return Pieces(level, lines);
        }

        private static List<ArenaGearPiece> Pieces(int level, List<ArenaGearLine>[] lines)
        {
            var pieces = new List<ArenaGearPiece>(ArenaGearTable.SlotCount);
            for (int slot = 0; slot < ArenaGearTable.SlotCount; slot++)
                pieces.Add(new ArenaGearPiece(slot, level, lines?[slot]));
            return pieces;
        }
    }
}
