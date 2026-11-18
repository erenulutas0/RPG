using System;
using System.Collections.Generic;

namespace Cryptforge.Arena
{
    // The duel's fixed frame (docs/55 sections 2.2 and 3.2): integers only - health and damage in milli-points, distances
    // in milli-units, 60 ticks a second, chances per mille. Every value here can move a result, so every one is in
    // ArenaRules.Hash.
    public static class DuelRules
    {
        public const int TicksPerSecond = 60;
        // The arena's health stretch for both sides: a run's hero dies to a level-1 foe in 5 s, no duel should.
        public const int HealthScale = 8;
        public const int LimitTicks = 90 * TicksPerSecond;
        // A straight lane, the walls at +-6 units, the fighters entering at +-4.
        public const int Wall = 6_000;
        public const int Entry = 4_000;
        // A kiting weapon steps back once the foe is this much inside its reach.
        public const int KiteSlack = 400;
        // Every hero starts with a 10 % crit so two equal builds are not decided by who swings first; crits strike x2.
        public const int BaseCritPermille = 100;
        public const int CritPercent = 200;
        // BurnRule in the duel's units: 5 stacks at most, a tick every 0.5 s of 3 points a second a stack, every stack's
        // 3 s refreshed by a new one. Flat: it scales with the hero's coefficient and the burn line, not the weapon.
        public const int BurnMaxStacks = 5;
        public const int BurnTickTicks = 30;
        public const int BurnTickMilli = 1_500;
        public const int BurnTicks = 3 * TicksPerSecond;
        // A rank's step on a skill's own number (docs/57 section 4.3).
        public const int RankStepPermille = 60;
        public const int SkillSlots = 3;
        // The careful policy: the Vent waits for this many stacks on the foe; the guard and the ward wait for health under
        // this share with the foe at hand (within its reach plus AtHand).
        public const int VentStacks = 4;
        public const int CarefulHealthPermille = 700;
        public const int AtHand = 500;
        // The Sigil is drawn as the foe comes on (careful: within the caster's reach plus this) or whenever the foe is in
        // the lane's half (bold).
        public const int SigilCarefulReach = 1_000;
        public const int SigilBoldReach = 6_000;
        // Best of three: up to five rounds while rounds draw; still level after five, a draw.
        public const int WinsNeeded = 2;
        public const int MaxRounds = 5;

        internal static void HashInto(ref ulong hash)
        {
            foreach (int value in new[]
                     {
                         TicksPerSecond, HealthScale, LimitTicks, Wall, Entry, KiteSlack, BaseCritPermille, CritPercent,
                         BurnMaxStacks, BurnTickTicks, BurnTickMilli, BurnTicks, RankStepPermille, SkillSlots, VentStacks,
                         CarefulHealthPermille, AtHand, SigilCarefulReach, SigilBoldReach, WinsNeeded, MaxRounds
                     })
                hash = ArenaHash.Mix(hash, value);
        }
    }

    // The arena's tunable table: the heroes with their coefficients, the weapons, the six skills, the ranked
    // normalisation (the common + level, the number of lines, each line type's value) and the boons. Versioned and hashed
    // (docs/55 section 3.6): Version is bumped by any change that can move a result; a match stores (Version, Hash), and
    // ArenaBuildTests pins both, so a change made without a bump fails. Current is the shipped table; research makes
    // variants with the With* methods, which keep the version and change the hash.
    public sealed class ArenaRules
    {
        public const int ShippedVersion = 1;

        public const string Vanguard = "hero_vanguard";
        public const string Ilya = "hero_cinder_arcanist";
        public const string Sword = "weapon_sword";
        public const string Staff = "weapon_staff";
        public const string Daggers = "weapon_daggers";
        public const string ForgeBurst = "ability_forge_burst";
        public const string IronResolve = "ability_iron_resolve";
        public const string ShieldRush = "ability_shield_rush";
        public const string CrucibleVent = "ability_crucible_vent";
        public const string FlameSigil = "ability_flame_sigil";
        public const string CinderWard = "ability_cinder_ward";

        public static readonly ArenaRules Current = Shipped();

        private ArenaHero[] _heroes;
        private ArenaWeapon[] _weapons;
        private ArenaSkill[] _skills;
        private int[] _rankedLineValues;
        private ArenaBoon[] _boons;

        public int Version { get; private set; }
        // The rank every skill plays at, in both modes (docs/57 decision 8: ranked at rank III; ranks are not built yet).
        public int SkillRank { get; private set; }
        // Ranked: every slot's + level, the same for both sides.
        public int RankedLevel { get; private set; }
        // Ranked: how many efsun line types a build picks (distinct types).
        public int RankedLines { get; private set; }
        // Boons dealt to each side every round.
        public int BoonsDealt { get; private set; }
        // A kiting weapon backs off at this share of its speed, per mille: a caster stepping back while it casts is slower
        // than a melee hero coming on, so a kite buys distance, not a whole lane of free strikes.
        public int BackpedalPermille { get; private set; }
        // Ember Memory's burn grows with the build's output bonus (DuelCore's burn tick); false is the run's flat burn,
        // kept as a research variant to show why the arena does not use it.
        public bool BurnScales { get; private set; }
        public ulong Hash { get; private set; }

        private ArenaRules()
        {
        }

        private void Seal()
        {
            if (RankedLevel < 0 || RankedLevel > ArenaGearTable.MaxLevel)
                throw new ArgumentOutOfRangeException(nameof(RankedLevel));
            if (RankedLines < 0 || RankedLines > ArenaGearTable.MaxLines)
                throw new ArgumentOutOfRangeException(nameof(RankedLines));
            if (BoonsDealt < 0 || BoonsDealt > _boons.Length)
                throw new ArgumentOutOfRangeException(nameof(BoonsDealt));
            if (BackpedalPermille < 0 || BackpedalPermille > 1000)
                throw new ArgumentOutOfRangeException(nameof(BackpedalPermille));
            Hash = ComputeHash();
        }

        // A research variant: this table with one change, the same version, its own hash.
        private ArenaRules With(Action<ArenaRules> change)
        {
            var copy = (ArenaRules)MemberwiseClone();
            change(copy);
            copy.Seal();
            return copy;
        }

        // The coefficients (Ilya's damage x0.859, the Staff's x1.023, the Daggers' x0.784; the Vanguard and the Sword the
        // reference) and the ranked line values are the settled medians of ArenaBalanceResearch.Normalisation's
        // calibration loop (docs/55 section 10): each identity even against the field, each line type's carriers even with
        // its non-carriers. An auto-duel is steep - ten per mille of a coefficient moves an identity five to eight points -
        // so they are kept to the per mille.
        private static ArenaRules Shipped()
        {
            var heroes = new[]
            {
                // Hero_Vanguard: 100 health; the Sword to start, the Staff and the Daggers from the Forge.
                new ArenaHero(Vanguard, "Vanguard", 100_000, 50, false, 1000, 1000,
                    new[] { ForgeBurst, IronResolve, ShieldRush }, new[] { Sword, Staff, Daggers }),
                // Hero_CinderArcanist: 90 health, Ember Memory, the Staff only (WeaponShop.OnlyStartingWeapon).
                new ArenaHero(Ilya, "Ilya", 90_000, 50, true, 859, 1000,
                    new[] { CrucibleVent, FlameSigil, CinderWard }, new[] { Staff })
            };
            var weapons = new[]
            {
                // Weapon_*.asset's category numbers: the Sword 18 every 0.8 s at 2.1, cleaving; the Staff 24 every 1.1 s at
                // 2.4, blasting, ranged; the Daggers 7 every 0.3 s at 1.8, every third strike a crit x2.
                new ArenaWeapon(Sword, "Sword", 18_000, 48, 2_100, false, ArenaWeaponPattern.Cleave, 0, 0, 1000),
                new ArenaWeapon(Staff, "Staff", 24_000, 66, 2_400, true, ArenaWeaponPattern.Area, 0, 0, 1023),
                new ArenaWeapon(Daggers, "Daggers", 7_000, 18, 1_800, false, ArenaWeaponPattern.DirectHit, 3, 200, 784)
            };
            var skills = new[]
            {
                // docs/57 section 3, rank I.
                new ArenaSkill(ForgeBurst, "Forge Burst", ArenaSkillKind.Burst, ArenaEffect.ForgeBurst, 20_000, 8, radius: 2_500),
                new ArenaSkill(IronResolve, "Iron Resolve", ArenaSkillKind.Guard, ArenaEffect.IronResolve, 500, 20, durationTicks: 240),
                new ArenaSkill(ShieldRush, "Shield Rush", ArenaSkillKind.Rush, ArenaEffect.ShieldRush, 15_000, 12, radius: 4_000,
                    stunTicks: 30),
                new ArenaSkill(CrucibleVent, "Crucible Vent", ArenaSkillKind.Vent, ArenaEffect.CrucibleVent, 8_000, 14, radius: 3_000),
                new ArenaSkill(FlameSigil, "Flame Sigil", ArenaSkillKind.Sigil, ArenaEffect.FlameSigil, 1_000, 20, radius: 2_500,
                    durationTicks: 300, slowPermille: 400),
                new ArenaSkill(CinderWard, "Cinder Ward", ArenaSkillKind.Ward, ArenaEffect.CinderWard, 200, 20, durationTicks: 120,
                    stacks: 2)
            };
            // The ranked pool, in the gear's hundredths: each line type's common value (damage the reference, the rest
            // calibrated against it); 0 keeps a stat out of the ranked pool. Move speed is out: a chaser keeps up with a
            // kiter by design, so speed buys nothing in the lane (measured: a move-speed line wins under a quarter against a
            // damage line at any value). The ability-cooldown line is out (docs/57 decision 8), and so are the lines with
            // nothing to act on one to one (damage vs elites, area, pickup radius, healing, luck).
            var lines = new int[ArenaGearTable.StatCount];
            lines[(int)ArenaStat.Damage] = 300;
            lines[(int)ArenaStat.AttackSpeed] = 300;
            lines[(int)ArenaStat.CritChance] = 325;
            lines[(int)ArenaStat.MaxHealth] = 300;
            lines[(int)ArenaStat.Armour] = 275;
            lines[(int)ArenaStat.BurnDamage] = 900;
            // Boons of one quality (about one line and a half each), one dealt to each side a round.
            var boons = new[]
            {
                new ArenaBoon("boon_force", "Force", ArenaStat.Damage, 600),
                new ArenaBoon("boon_tempo", "Tempo", ArenaStat.AttackSpeed, 600),
                new ArenaBoon("boon_vitality", "Vitality", ArenaStat.MaxHealth, 600),
                new ArenaBoon("boon_keen", "Keen", ArenaStat.CritChance, 300)
            };
            var rules = new ArenaRules
            {
                Version = ShippedVersion,
                _heroes = heroes,
                _weapons = weapons,
                _skills = skills,
                _rankedLineValues = lines,
                _boons = boons,
                SkillRank = 3,
                RankedLevel = 5,
                RankedLines = 3,
                BoonsDealt = 1,
                BackpedalPermille = 600,
                BurnScales = true
            };
            rules.Seal();
            return rules;
        }

        public IReadOnlyList<ArenaHero> Heroes => _heroes;
        public IReadOnlyList<ArenaWeapon> Weapons => _weapons;
        public IReadOnlyList<ArenaSkill> Skills => _skills;
        public IReadOnlyList<ArenaBoon> Boons => _boons;

        public ArenaHero Hero(string id) => Array.Find(_heroes, h => h.Id == id);
        public ArenaWeapon Weapon(string id) => Array.Find(_weapons, w => w.Id == id);
        public ArenaSkill Skill(string id) => Array.Find(_skills, s => s.Id == id);

        // A line type's ranked value in the gear's hundredths; 0 when the type is not in the ranked pool.
        public int RankedLineValue(ArenaStat stat) => _rankedLineValues[(int)stat];

        // Whether a hero may pick this line type in ranked: in the pool, and doing something for it (the burn line only
        // for a hero that burns).
        public bool InRankedPool(ArenaStat stat, ArenaHero hero) =>
            (int)stat >= 0 && (int)stat < ArenaGearTable.StatCount && _rankedLineValues[(int)stat] > 0 &&
            (stat != ArenaStat.BurnDamage || (hero != null && hero.BurnOnHit));

        // The ranked pool for a hero, in the stats' order.
        public List<ArenaStat> RankedPool(ArenaHero hero)
        {
            var pool = new List<ArenaStat>();
            for (int i = 0; i < ArenaGearTable.StatCount; i++)
            {
                if (InRankedPool((ArenaStat)i, hero))
                    pool.Add((ArenaStat)i);
            }
            return pool;
        }

        // What the duel reads of the gear. Damage vs elites, area, pickup radius, healing and luck have nothing to act on
        // in a one-on-one lane; the ability-cooldown line counts only in the open arena (docs/57 decision 8).
        public static bool ArenaReads(ArenaStat stat, ArenaMode mode)
        {
            switch (stat)
            {
                case ArenaStat.Damage:
                case ArenaStat.AttackSpeed:
                case ArenaStat.CritChance:
                case ArenaStat.MaxHealth:
                case ArenaStat.Armour:
                case ArenaStat.BurnDamage:
                case ArenaStat.MoveSpeed:
                    return true;
                case ArenaStat.AbilityCooldown:
                    return mode == ArenaMode.Open;
                default:
                    return false;
            }
        }

        // ---- Research variants: the same version, another hash. ----

        public ArenaRules WithHero(ArenaHero hero) => With(r => r._heroes = Replace(_heroes, hero, h => h.Id == hero.Id));

        public ArenaRules WithWeapon(ArenaWeapon weapon) => With(r => r._weapons = Replace(_weapons, weapon, w => w.Id == weapon.Id));

        public ArenaRules WithRankedLineValue(ArenaStat stat, int hundredths) => With(r =>
        {
            r._rankedLineValues = (int[])_rankedLineValues.Clone();
            r._rankedLineValues[(int)stat] = hundredths;
        });

        public ArenaRules WithRanked(int level, int lines) => With(r =>
        {
            r.RankedLevel = level;
            r.RankedLines = lines;
        });

        public ArenaRules WithBoonsDealt(int boons) => With(r => r.BoonsDealt = boons);

        public ArenaRules WithSkillRank(int rank) => With(r => r.SkillRank = rank);

        public ArenaRules WithBackpedal(int permille) => With(r => r.BackpedalPermille = permille);

        public ArenaRules WithBurnScaling(bool scales) => With(r => r.BurnScales = scales);

        private static T[] Replace<T>(T[] items, T item, Predicate<T> match)
        {
            var copy = (T[])items.Clone();
            int index = Array.FindIndex(copy, match);
            if (index < 0)
                throw new ArgumentException("No such entry to replace.", nameof(item));
            copy[index] = item;
            return copy;
        }

        private ulong ComputeHash()
        {
            ulong hash = ArenaHash.Seed;
            hash = ArenaHash.Mix(hash, Version);
            DuelRules.HashInto(ref hash);
            ArenaGearTable.HashInto(ref hash);
            foreach (ArenaHero h in _heroes)
            {
                hash = ArenaHash.Mix(hash, h.Id);
                hash = ArenaHash.Mix(hash, h.Health);
                hash = ArenaHash.Mix(hash, h.SpeedPerTick);
                hash = ArenaHash.Mix(hash, h.BurnOnHit ? 1 : 0);
                hash = ArenaHash.Mix(hash, h.DamagePermille);
                hash = ArenaHash.Mix(hash, h.HealthPermille);
                foreach (string id in h.SkillIds)
                    hash = ArenaHash.Mix(hash, id);
                foreach (string id in h.WeaponIds)
                    hash = ArenaHash.Mix(hash, id);
            }
            foreach (ArenaWeapon w in _weapons)
            {
                hash = ArenaHash.Mix(hash, w.Id);
                foreach (int value in new[] { w.Damage, w.IntervalTicks, w.Reach, w.Kites ? 1 : 0, (int)w.Pattern, w.CritEvery, w.RhythmPercent, w.DamagePermille })
                    hash = ArenaHash.Mix(hash, value);
            }
            foreach (ArenaSkill s in _skills)
            {
                hash = ArenaHash.Mix(hash, s.Id);
                foreach (int value in new[] { (int)s.Kind, (int)s.Effect, s.Power, s.CooldownTicks, s.Radius, s.DurationTicks, s.Stacks, s.SlowPermille, s.StunTicks })
                    hash = ArenaHash.Mix(hash, value);
            }
            foreach (int value in _rankedLineValues)
                hash = ArenaHash.Mix(hash, value);
            foreach (ArenaBoon b in _boons)
            {
                hash = ArenaHash.Mix(hash, b.Id);
                hash = ArenaHash.Mix(hash, (int)b.Stat);
                hash = ArenaHash.Mix(hash, b.Hundredths);
            }
            hash = ArenaHash.Mix(hash, SkillRank);
            hash = ArenaHash.Mix(hash, RankedLevel);
            hash = ArenaHash.Mix(hash, RankedLines);
            hash = ArenaHash.Mix(hash, BoonsDealt);
            hash = ArenaHash.Mix(hash, BackpedalPermille);
            hash = ArenaHash.Mix(hash, BurnScales ? 1 : 0);
            return hash;
        }
    }
}
