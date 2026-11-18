using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // A build resolved under one table of rules: the hero, the weapon and the three skills it fights with, and per stat the
    // gear's clamped total the duel reads (the gear's hundredths: 300 is +3 %, or 3 points of crit or armour). Credited is
    // the part of each total the "why" summary names as the build's own: ranked, its line types (the common + level is the
    // same for both sides and reads as the weapon's); open, the whole gear.
    public sealed class ArenaFighter
    {
        private readonly ArenaSkill[] _skills;
        private readonly int[] _totals;
        private readonly int[] _credited;

        public ArenaRules Rules { get; }
        public ArenaBuild Build { get; }
        public ArenaMode Mode => Build.Mode;
        public ArenaHero Hero { get; }
        public ArenaWeapon Weapon { get; }
        public ArenaPolicy Policy => Build.Policy;
        // A training rival's handicap on its damage, per mille (1000 none): the build's, stamped by the ladder.
        public int HandicapPermille => Build.HandicapPermille;
        public IReadOnlyList<ArenaSkill> Skills => _skills;
        // Ranked: the line types it fights with, after the resolver's repair (distinct, in the pool, exactly RankedLines).
        public IReadOnlyList<ArenaStat> LineTypes { get; }
        // The power index of what it fights with (docs/54 section 7.1), in hundredths of a percent: the gear's in the open
        // arena, the common level's and the lines' in ranked.
        public int GearPower { get; }

        internal ArenaFighter(ArenaRules rules, ArenaBuild build, ArenaHero hero, ArenaWeapon weapon, ArenaSkill[] skills,
            int[] totals, int[] credited, ArenaStat[] lineTypes)
        {
            Rules = rules;
            Build = build;
            Hero = hero;
            Weapon = weapon;
            _skills = skills;
            _totals = totals;
            _credited = credited;
            LineTypes = lineTypes;
            GearPower = ArenaGearTable.Power(totals);
        }

        public int Total(ArenaStat stat) => _totals[(int)stat];
        public int Credited(ArenaStat stat) => _credited[(int)stat];
    }

    // Re-resolves a stored build under the fight's rules (docs/55 section 3.6). Repairs rather than rejects, as the profile
    // does: a weapon the hero cannot carry becomes its starting weapon, skills that are not the hero's are replaced by its
    // own in slot order, and ranked line types are made distinct, kept to the hero's pool and filled in the pool's order
    // up to RankedLines - so both sides always fight with the same number of lines, also for a snapshot made under an
    // older table. Only an unknown hero cannot be resolved.
    public static class ArenaResolver
    {
        public static ArenaFighter Resolve(ArenaBuild build, ArenaRules rules = null)
        {
            if (build == null)
                throw new ArgumentNullException(nameof(build));
            rules = rules ?? ArenaRules.Current;
            ArenaHero hero = rules.Hero(build.HeroId);
            if (hero == null)
                throw new ArgumentException($"Unknown hero '{build.HeroId}'.", nameof(build));
            ArenaWeapon weapon = rules.Weapon(hero.Carries(build.WeaponId) ? build.WeaponId : hero.StartingWeaponId);
            ArenaSkill[] skills = SkillsOf(hero, build.SkillIds, rules);

            int[] totals;
            int[] credited = new int[ArenaGearTable.StatCount];
            ArenaStat[] lineTypes = new ArenaStat[0];
            if (build.Mode == ArenaMode.Ranked)
            {
                lineTypes = RankedLines(hero, build.LineTypes, rules);
                var main = new int[ArenaGearTable.StatCount];
                for (int slot = 0; slot < ArenaGearTable.SlotCount; slot++)
                    main[(int)ArenaGearTable.MainStatOf(slot)] += ArenaGearTable.MainValue(slot, rules.RankedLevel);
                // No gear cap in ranked: the values are the arena's own and the same for both sides, and a cap would
                // make a line type dead at one + level and live at another (measured: the crit line stopped counting at
                // 2.5 points over the +5 ring's 1.5).
                totals = (int[])main.Clone();
                foreach (ArenaStat stat in lineTypes)
                {
                    totals[(int)stat] += rules.RankedLineValue(stat);
                    credited[(int)stat] += rules.RankedLineValue(stat);
                }
            }
            else
            {
                totals = ArenaGearTable.Totals(build.Gear);
                for (int i = 0; i < totals.Length; i++)
                    credited[i] = totals[i];
            }
            for (int i = 0; i < totals.Length; i++)
            {
                if (!ArenaRules.ArenaReads((ArenaStat)i, build.Mode))
                {
                    totals[i] = 0;
                    credited[i] = 0;
                }
            }
            return new ArenaFighter(rules, build, hero, weapon, skills, totals, credited, lineTypes);
        }

        private static ArenaSkill[] SkillsOf(ArenaHero hero, IReadOnlyList<string> wanted, ArenaRules rules)
        {
            var ids = new List<string>(DuelRules.SkillSlots);
            if (wanted != null)
            {
                foreach (string id in wanted)
                {
                    bool owned = false;
                    foreach (string own in hero.SkillIds)
                        owned |= own == id;
                    if (owned && !ids.Contains(id) && ids.Count < DuelRules.SkillSlots)
                        ids.Add(id);
                }
            }
            foreach (string own in hero.SkillIds)
            {
                if (ids.Count < DuelRules.SkillSlots && !ids.Contains(own))
                    ids.Add(own);
            }
            var skills = new ArenaSkill[ids.Count];
            for (int i = 0; i < skills.Length; i++)
            {
                skills[i] = rules.Skill(ids[i]);
                if (skills[i] == null)
                    throw new InvalidOperationException($"The rules have no skill '{ids[i]}' for {hero.Id}.");
            }
            return skills;
        }

        // The hero's ranked lines: the build's types that are in its pool, distinct, in its order, then the pool's
        // remaining types in order, RankedLines in all.
        public static ArenaStat[] RankedLines(ArenaHero hero, IReadOnlyList<ArenaStat> wanted, ArenaRules rules = null)
        {
            rules = rules ?? ArenaRules.Current;
            var lines = new List<ArenaStat>(rules.RankedLines);
            if (wanted != null)
            {
                foreach (ArenaStat stat in wanted)
                {
                    if (lines.Count < rules.RankedLines && rules.InRankedPool(stat, hero) && !lines.Contains(stat))
                        lines.Add(stat);
                }
            }
            foreach (ArenaStat stat in rules.RankedPool(hero))
            {
                if (lines.Count < rules.RankedLines && !lines.Contains(stat))
                    lines.Add(stat);
            }
            return lines.ToArray();
        }
    }
}
