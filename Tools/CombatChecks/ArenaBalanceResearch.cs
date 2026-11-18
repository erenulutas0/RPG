using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Arena;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/55 P0's balance on the arena's own core (Cryptforge.Arena, 2026-11-18), on demand:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~ArenaBalanceResearch"
    //
    // Each test writes its section to TestResults/arena-p0-<name>.md (or ARENA_OUT). Environment, optional: ARENA_SEEDS
    // (2000, the seeds a matchup), ARENA_DOMINANCE_SEEDS (2000, a build against the field), ARENA_LOOP_SEEDS (400, a build
    // a calibration pass), ARENA_LOOP_PASSES (12), ARENA_SWEEP_SEEDS (300, a build in the level and line-count sweeps),
    // ARENA_SWEEP_PASSES (5, the calibration before each sweep row), ARENA_PARALLEL (3 threads: other simulations share this
    // machine).
    //
    //   Normalisation  the calibration loop that sets the coefficients and the line values (from docs/57's starting
    //                  point), each line type against the damage line one on one, and the common + level, the number of
    //                  lines and the boons swept (each row calibrated first) against the dominance spread
    //   Coefficients   the round-robin of the four identities (hero x weapon) in ranked, uncoefficiented and as shipped,
    //                  hero against hero, how steep a coefficient is, the kite's backpedal, rounds and their length
    //   Dominance      every ranked build (identity x line types x policy) against the field; the top and the bottom
    //   OpenCurve      the open arena: a geared side against an ungeared one by gear power, equal gear across the range
    //                  (with the scaling burn and the run's flat one), and gaps between matched powers
    //   Cost           a best of three's cost, and the replay pin at scale (sequential against parallel, logs replayed)
    //   TrainingRivals the training rivals' handicap by rung (docs/55 section 10.10): a careful middle player's share by the
    //                  rival's handicap, the log-odds fit, the table and the share it measures at every rung
    //                  (ARENA_TRAINING_SEEDS, 4000 a grid point)
    //   OpenOffers     the open offers' expected chance: A's share by base power and ratio gap, any identity on either side,
    //                  the logistic fit and its table, and the three offers' cards against measured shares
    //                  (ARENA_OFFER_SEEDS, 1500 a cell)
    [Explicit("docs/55 P0's arena balance research: run it by name.")]
    public sealed class ArenaBalanceResearch
    {
        internal readonly struct Identity
        {
            public readonly string Hero;
            public readonly string Weapon;
            public readonly string Name;

            public Identity(string hero, string weapon, string name)
            {
                Hero = hero;
                Weapon = weapon;
                Name = name;
            }
        }

        internal static readonly Identity[] Identities =
        {
            new Identity(ArenaRules.Vanguard, ArenaRules.Sword, "Vanguard + Sword"),
            new Identity(ArenaRules.Vanguard, ArenaRules.Staff, "Vanguard + Staff"),
            new Identity(ArenaRules.Vanguard, ArenaRules.Daggers, "Vanguard + Daggers"),
            new Identity(ArenaRules.Ilya, ArenaRules.Staff, "Ilya + Staff")
        };

        private const int FieldPurpose = 901;

        private static int Env(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) && value > 0 ? value : fallback;

        private static int Threads => Env("ARENA_PARALLEL", 3);

        private static string P(double share) => (share * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        private static string F(double value, string format = "0.0") => value.ToString(format, CultureInfo.InvariantCulture);

        private static double Score(int winner) => winner == 0 ? 1 : winner == -1 ? 0.5 : 0;

        // Every combination of `count` distinct types from the pool, in the pool's order.
        internal static List<ArenaStat[]> Combinations(List<ArenaStat> pool, int count)
        {
            var all = new List<ArenaStat[]>();
            void Walk(int start, List<ArenaStat> picked)
            {
                if (picked.Count == count)
                {
                    all.Add(picked.ToArray());
                    return;
                }
                for (int i = start; i < pool.Count; i++)
                {
                    picked.Add(pool[i]);
                    Walk(i + 1, picked);
                    picked.RemoveAt(picked.Count - 1);
                }
            }
            Walk(0, new List<ArenaStat>());
            return all;
        }

        // Every ranked build of an identity: each line-type combination with each policy.
        internal static List<ArenaBuild> Builds(ArenaRules rules, Identity identity)
        {
            ArenaHero hero = rules.Hero(identity.Hero);
            var builds = new List<ArenaBuild>();
            foreach (ArenaStat[] lines in Combinations(rules.RankedPool(hero), rules.RankedLines))
            foreach (ArenaPolicy policy in new[] { ArenaPolicy.Careful, ArenaPolicy.Bold })
                builds.Add(ArenaBuild.Ranked(hero.Id, identity.Weapon, hero.SkillIds, policy, lines, rules.Version));
            return builds;
        }

        // A build of the identity drawn for a seed and a side: the field's sample.
        private static ArenaBuild Drawn(List<ArenaBuild> builds, int seed, int side) =>
            builds[ArenaRandom.Stream(seed, FieldPurpose, side).NextBelow(builds.Count)];

        // A's share of seeds won (draws half), best of three, over the seeds, in parallel.
        private static double Share(int[] seeds, Func<int, (ArenaFighter a, ArenaFighter b)> pair)
        {
            var scores = new double[seeds.Length];
            Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Threads }, i =>
            {
                (ArenaFighter a, ArenaFighter b) = pair(seeds[i]);
                scores[i] = Score(DuelCore.BestOfThree(a, b, seeds[i]).Winner);
            });
            return scores.Sum() / seeds.Length;
        }

        // X against Y, each side's build drawn per seed from its identity's ranked builds.
        private static double Cross(ArenaRules rules, Identity x, Identity y, int[] seeds)
        {
            List<ArenaBuild> bx = Builds(rules, x);
            List<ArenaBuild> by = Builds(rules, y);
            ArenaFighter[] fx = bx.Select(b => ArenaResolver.Resolve(b, rules)).ToArray();
            ArenaFighter[] fy = by.Select(b => ArenaResolver.Resolve(b, rules)).ToArray();
            return Share(seeds, s => (fx[ArenaRandom.Stream(s, FieldPurpose, 0).NextBelow(fx.Length)],
                fy[ArenaRandom.Stream(s, FieldPurpose, 1).NextBelow(fy.Length)]));
        }

        private static string Matrix(ArenaRules rules, int[] seeds)
        {
            var md = new StringBuilder();
            md.AppendLine("| A \\ B | " + string.Join(" | ", Identities.Select(i => i.Name)) + " | A against the field |");
            md.AppendLine("|---|" + string.Join("|", Identities.Select(_ => "---")) + "|---|");
            foreach (Identity x in Identities)
            {
                double[] row = Identities.Select(y => Cross(rules, x, y, seeds)).ToArray();
                md.AppendLine($"| {x.Name} | {string.Join(" | ", row.Select(P))} | {P(row.Average())} |");
            }
            return md.ToString();
        }

        [Test]
        public void Coefficients()
        {
            int[] seeds = Enumerable.Range(1, Env("ARENA_SEEDS", 2000)).ToArray();
            ArenaRules shipped = ArenaRules.Current;
            var md = new StringBuilder();
            md.AppendLine("# The arena's coefficients (docs/55 P0)");
            md.AppendLine();
            md.AppendLine($"Cryptforge.Arena, rules v{shipped.Version} (hash {ArenaHash.Hex(shipped.Hash)}), ranked: + level {shipped.RankedLevel}, " +
                          $"{shipped.RankedLines} line types, rank {shipped.SkillRank} skills, {shipped.BoonsDealt} boon a round, kiting at " +
                          $"{shipped.BackpedalPermille} per mille. Seeds 1-{seeds.Length}, best of three, A's wins (draws half); each side's line types " +
                          "and policy drawn per seed from its identity's ranked builds. The coefficients come from Normalisation's calibration loop.");
            md.AppendLine();

            md.AppendLine("## 1. Uncoefficiented: every hero and weapon at x1.000");
            md.AppendLine();
            ArenaRules bare = shipped;
            foreach (ArenaHero h in shipped.Heroes)
                bare = bare.WithHero(h.WithCoefficients(1000, 1000));
            foreach (ArenaWeapon w in shipped.Weapons)
                bare = bare.WithWeapon(w.WithCoefficient(1000));
            md.Append(Matrix(bare, seeds));
            md.AppendLine();

            md.AppendLine("## 2. The round-robin as shipped");
            md.AppendLine();
            md.AppendLine($"Staff x{F(shipped.Weapon(ArenaRules.Staff).DamagePermille / 1000.0, "0.000")}, Daggers x{F(shipped.Weapon(ArenaRules.Daggers).DamagePermille / 1000.0, "0.000")}, " +
                          $"Ilya x{F(shipped.Hero(ArenaRules.Ilya).DamagePermille / 1000.0, "0.000")} (the Vanguard and the Sword x1.000).");
            md.AppendLine();
            md.Append(Matrix(shipped, seeds));
            md.AppendLine();
            double vanguardOnIlya = Identities.Take(3).Average(x => Cross(shipped, x, Identities[3], seeds));
            double ilyaOnVanguard = Identities.Take(3).Average(y => Cross(shipped, Identities[3], y, seeds));
            md.AppendLine($"Hero against hero: the Vanguard (its three weapons evenly) against Ilya **{P(vanguardOnIlya)}**; Ilya against the " +
                          $"Vanguard **{P(ilyaOnVanguard)}**.");
            md.AppendLine();

            // 3. How steep: each coefficient a few per mille off, the identity against the field.
            md.AppendLine("## 3. How steep a coefficient is (the identity's mean against the four identities, one knob moved)");
            md.AppendLine();
            md.AppendLine("| Knob | -20 | -10 | as shipped | +10 | +20 (per mille) |");
            md.AppendLine("|---|---|---|---|---|---|");
            (string Label, int Who, Func<ArenaRules, int, ArenaRules> Move)[] knobs =
            {
                ("the Staff (Vanguard + Staff)", 1, (r, d) => r.WithWeapon(r.Weapon(ArenaRules.Staff).WithCoefficient(r.Weapon(ArenaRules.Staff).DamagePermille + d))),
                ("the Daggers (Vanguard + Daggers)", 2, (r, d) => r.WithWeapon(r.Weapon(ArenaRules.Daggers).WithCoefficient(r.Weapon(ArenaRules.Daggers).DamagePermille + d))),
                ("Ilya's damage (Ilya + Staff)", 3, (r, d) => r.WithHero(r.Hero(ArenaRules.Ilya).WithCoefficients(r.Hero(ArenaRules.Ilya).DamagePermille + d, 1000)))
            };
            foreach (var knob in knobs)
            {
                var cells = new[] { -20, -10, 0, 10, 20 }.Select(d =>
                {
                    ArenaRules r = knob.Move(shipped, d);
                    return P(Identities.Average(y => Cross(r, Identities[knob.Who], y, seeds)));
                });
                md.AppendLine($"| {knob.Label} | {string.Join(" | ", cells)} |");
            }
            md.AppendLine();

            // The kite's backpedal: a kiter that backs off as fast as a chaser comes on keeps any gap it opens, so a weapon
            // shorter than the kite's trigger (the Daggers' 1.8 against the Staff's 2.0) never lands until the wall.
            md.AppendLine("## 4. The kite's backpedal (the shipped coefficients; A's wins)");
            md.AppendLine();
            md.AppendLine("| Backpedal (per mille of speed) | Vanguard + Staff against + Daggers | Ilya against the Vanguard + Daggers | Vanguard + Sword against + Daggers | Vanguard + Staff against + Sword | Identities' means against the four |");
            md.AppendLine("|---|---|---|---|---|---|");
            foreach (int backpedal in new[] { 1000, 800, 600, 400 })
            {
                ArenaRules r = shipped.WithBackpedal(backpedal);
                string means = string.Join(", ", Identities.Select(x => P(Identities.Average(y => Cross(r, x, y, seeds)))));
                md.AppendLine($"| {backpedal}{(backpedal == shipped.BackpedalPermille ? " (shipped)" : "")} | {P(Cross(r, Identities[1], Identities[2], seeds))} | " +
                              $"{P(Cross(r, Identities[3], Identities[2], seeds))} | {P(Cross(r, Identities[0], Identities[2], seeds))} | " +
                              $"{P(Cross(r, Identities[1], Identities[0], seeds))} | {means} |");
            }
            md.AppendLine();

            md.AppendLine("## 5. Single rounds (no best of three) and the fight's length, as shipped, mirrors");
            md.AppendLine();
            md.AppendLine("| Identity | Round won by A | Rounds drawn | Round P10 / P50 / P90 s |");
            md.AppendLine("|---|---|---|---|");
            foreach (Identity x in Identities)
            {
                ArenaFighter[] fx = Builds(shipped, x).Select(b => ArenaResolver.Resolve(b, shipped)).ToArray();
                var rounds = new DuelRoundResult[seeds.Length];
                Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Threads }, i =>
                    rounds[i] = DuelCore.Round(fx[ArenaRandom.Stream(seeds[i], FieldPurpose, 0).NextBelow(fx.Length)],
                        fx[ArenaRandom.Stream(seeds[i], FieldPurpose, 1).NextBelow(fx.Length)], seeds[i]));
                double[] length = rounds.Select(r => r.Ticks / (double)DuelRules.TicksPerSecond).OrderBy(v => v).ToArray();
                md.AppendLine($"| {x.Name} | {P(rounds.Average(r => Score(r.Winner)))} | {P(rounds.Count(r => r.Winner < 0) / (double)rounds.Length)} | " +
                              $"{F(length[length.Length / 10])} / {F(length[length.Length / 2])} / {F(length[length.Length * 9 / 10])} |");
            }
            Write("arena-p0-coefficients.md", md.ToString());
        }

        [Test]
        public void Normalisation()
        {
            int[] seeds = Enumerable.Range(1, Env("ARENA_SEEDS", 2000)).ToArray();
            int[] sweep = Enumerable.Range(1, Env("ARENA_SWEEP_SEEDS", 300)).ToArray();
            int loopSeeds = Env("ARENA_LOOP_SEEDS", 400);
            ArenaRules shipped = ArenaRules.Current;
            var md = new StringBuilder();
            md.AppendLine("# The ranked normalisation (docs/55 P0)");
            md.AppendLine();
            md.AppendLine($"Rules v{shipped.Version} (hash {ArenaHash.Hex(shipped.Hash)}). Best of three, A's wins (draws half).");
            md.AppendLine();

            // 1. The calibration loop: from docs/57's starting point (Ilya x0.8, the Staff and the Daggers untouched, the
            // first guesses at the line values), every ranked build against the field; each identity's coefficient moves
            // its mean toward the Sword's, each line type's value moves its carriers' mean toward its non-carriers' (the
            // damage line the reference), damped, until they settle.
            md.AppendLine($"## 1. The calibration loop (+ level {shipped.RankedLevel}, {shipped.RankedLines} lines; {loopSeeds} seeds a build a pass)");
            md.AppendLine();
            ArenaStat[] tuned = Calibrated;
            ArenaRules r = shipped.WithWeapon(shipped.Weapon(ArenaRules.Staff).WithCoefficient(1000))
                .WithWeapon(shipped.Weapon(ArenaRules.Daggers).WithCoefficient(1000))
                .WithHero(shipped.Hero(ArenaRules.Ilya).WithCoefficients(800, 1000))
                .WithRankedLineValue(ArenaStat.AttackSpeed, 300).WithRankedLineValue(ArenaStat.CritChance, 150)
                .WithRankedLineValue(ArenaStat.MaxHealth, 300).WithRankedLineValue(ArenaStat.Armour, 150).WithRankedLineValue(ArenaStat.BurnDamage, 900);
            int passes = Env("ARENA_LOOP_PASSES", 12);
            int[] loop = Enumerable.Range(1, loopSeeds).ToArray();
            Calibrate(r, loop, passes, md);
            md.AppendLine();
            md.AppendLine("The shipped table takes the median of the settled passes (the last five): Staff " +
                          $"{shipped.Weapon(ArenaRules.Staff).DamagePermille}, Daggers {shipped.Weapon(ArenaRules.Daggers).DamagePermille}, Ilya " +
                          $"{shipped.Hero(ArenaRules.Ilya).DamagePermille}; " +
                          string.Join(", ", new[] { ArenaStat.Damage }.Concat(tuned).Select(t => $"{ArenaGearTable.Id(t)} {shipped.RankedLineValue(t)}")) + ".");
            md.AppendLine();

            // 2. One line against the damage line, one on one: what each identity makes of a line type at the shipped values.
            md.AppendLine($"## 2. One line type against the damage line, at the shipped values (one line a side, mirrors, policies drawn; {seeds.Length} seeds)");
            md.AppendLine();
            md.AppendLine("| Line (value, hundredths) | " + string.Join(" | ", Identities.Select(i => i.Name)) + " | Mean where it applies |");
            md.AppendLine("|---|" + string.Join("|", Identities.Select(_ => "---")) + "|---|");
            foreach ((ArenaStat type, int value) in tuned.Select(t => (t, shipped.RankedLineValue(t)))
                         .Concat(new[] { (ArenaStat.MoveSpeed, 500), (ArenaStat.MoveSpeed, 2000) }))
            {
                // The move-speed rows put the line back into the pool to show why it is out of it.
                ArenaRules one = shipped.WithRanked(shipped.RankedLevel, 1).WithRankedLineValue(type, value);
                var cells = new List<string>();
                var applied = new List<double>();
                foreach (Identity x in Identities)
                {
                    if (!one.InRankedPool(type, one.Hero(x.Hero)))
                    {
                        cells.Add("-");
                        continue;
                    }
                    double share = OneLine(one, x, type, ArenaStat.Damage, seeds);
                    applied.Add(share);
                    cells.Add(P(share));
                }
                md.AppendLine($"| {ArenaGearTable.Id(type)} {value} | {string.Join(" | ", cells)} | {P(applied.Average())} |");
            }
            md.AppendLine();

            // 3. The common + level and the number of lines against the dominance spread and the fight's length.
            md.AppendLine($"## 3. The common + level, the number of lines and the boons (every ranked build against the field, {sweep.Length} seeds a build)");
            md.AppendLine();
            md.AppendLine($"Each row is calibrated first (the loop of section 1, {Env("ARENA_SWEEP_PASSES", 5)} passes from the shipped table at " +
                          $"{loopSeeds} seeds a build), then measured on its own seeds; the knobs it settled on are listed.");
            md.AppendLine();
            md.AppendLine("| + level | Lines | Boons | Builds | Best build | Worst | Spread | Builds above 55 % | Settled: Staff, Daggers, Ilya; " +
                          string.Join(", ", tuned.Select(ArenaGearTable.Id)) + " | Rounds' P50 s | Mean power |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach ((int level, int lines, int boons) in new[] { (3, 3, 1), (5, 3, 1), (7, 3, 1), (9, 3, 1), (5, 2, 1), (5, 4, 1), (5, 3, 0) })
            {
                ArenaRules v = Calibrate(shipped.WithRanked(level, lines).WithBoonsDealt(boons), loop, Env("ARENA_SWEEP_PASSES", 5), null);
                List<(ArenaBuild build, double share, double seconds)> field = Field(v, sweep, 7);
                double[] seconds = field.Select(f => f.seconds).OrderBy(x => x).ToArray();
                double power = field.Average(f => ArenaResolver.Resolve(f.build, v).GearPower) / 100.0;
                string knobs = $"{v.Weapon(ArenaRules.Staff).DamagePermille}, {v.Weapon(ArenaRules.Daggers).DamagePermille}, " +
                               $"{v.Hero(ArenaRules.Ilya).DamagePermille}; {string.Join(", ", tuned.Select(t => v.RankedLineValue(t).ToString(CultureInfo.InvariantCulture)))}";
                md.AppendLine($"| +{level} | {lines} | {boons} | {field.Count} | {P(field.Max(f => f.share))} | {P(field.Min(f => f.share))} | " +
                              $"{F((field.Max(f => f.share) - field.Min(f => f.share)) * 100)} pts | {field.Count(f => f.share > 0.55)} | {knobs} | " +
                              $"{F(seconds[seconds.Length / 2])} | +{F(power)} % |");
            }
            md.AppendLine();
            Write("arena-p0-normalisation.md", md.ToString());
        }

        private static readonly ArenaStat[] Calibrated =
            { ArenaStat.AttackSpeed, ArenaStat.CritChance, ArenaStat.MaxHealth, ArenaStat.Armour, ArenaStat.BurnDamage };

        // The calibration loop: every ranked build against the field a pass; each identity's coefficient moves its mean
        // toward the Sword's (about 0.4 points a per mille, damped), each line type's value moves its carriers' mean toward
        // its non-carriers' (the damage line the reference; about 2 % of the value a point, to the nearest 25 hundredths).
        // The rules after the last pass; each pass a row of md when given.
        private static ArenaRules Calibrate(ArenaRules r, int[] loop, int passes, StringBuilder md)
        {
            ArenaStat[] tuned = Calibrated;
            if (md != null)
            {
                md.AppendLine("| Pass | Staff | Daggers | Ilya | " + string.Join(" | ", tuned.Select(ArenaGearTable.Id)) + " | Identities' means (Sword, Staff, Daggers, Ilya) | Best / worst build |");
                md.AppendLine("|---|---|---|---|" + string.Join("|", tuned.Select(_ => "---")) + "|---|---|");
            }
            for (int pass = 0; pass < passes; pass++)
            {
                List<(ArenaBuild build, double share, double seconds)> field = Field(r, loop);
                ArenaRules current = r;
                double Mean(Identity x) => field.Where(f => f.build.HeroId == x.Hero && f.build.WeaponId == x.Weapon).Average(f => f.share);
                double Gap(ArenaStat t) =>
                    field.Where(f => f.build.LineTypes.Contains(t)).Average(f => f.share) -
                    field.Where(f => !f.build.LineTypes.Contains(t) && current.InRankedPool(t, current.Hero(f.build.HeroId))).Average(f => f.share);
                double[] means = Identities.Select(Mean).ToArray();
                md?.AppendLine($"| {pass} | {r.Weapon(ArenaRules.Staff).DamagePermille} | {r.Weapon(ArenaRules.Daggers).DamagePermille} | " +
                               $"{r.Hero(ArenaRules.Ilya).DamagePermille} | {string.Join(" | ", tuned.Select(t => r.RankedLineValue(t).ToString(CultureInfo.InvariantCulture)))} | " +
                               $"{string.Join(", ", means.Select(P))} | {P(field.Max(f => f.share))} / {P(field.Min(f => f.share))} |");
                int Step(double mean) => (int)Math.Round((means[0] - mean) * 100 / 0.4 * 0.6);
                double reference = Gap(ArenaStat.Damage);
                double[] gaps = tuned.Select(Gap).ToArray();
                r = r.WithWeapon(r.Weapon(ArenaRules.Staff).WithCoefficient(r.Weapon(ArenaRules.Staff).DamagePermille + Step(means[1])))
                    .WithWeapon(r.Weapon(ArenaRules.Daggers).WithCoefficient(r.Weapon(ArenaRules.Daggers).DamagePermille + Step(means[2])))
                    .WithHero(r.Hero(ArenaRules.Ilya).WithCoefficients(r.Hero(ArenaRules.Ilya).DamagePermille + Step(means[3]), 1000));
                for (int i = 0; i < tuned.Length; i++)
                {
                    int v = r.RankedLineValue(tuned[i]);
                    int next = (int)Math.Round(v * (1 - (gaps[i] - reference) * 100 * 0.02) / 25.0) * 25;
                    r = r.WithRankedLineValue(tuned[i], Math.Max(25, next));
                }
            }
            return r;
        }

        // A carries one line of type a, B one of type b; same identity, each side's policy drawn per seed.
        private static double OneLine(ArenaRules rules, Identity x, ArenaStat a, ArenaStat b, int[] seeds)
        {
            ArenaHero hero = rules.Hero(x.Hero);
            ArenaFighter Make(ArenaStat line, ArenaPolicy policy) =>
                ArenaResolver.Resolve(ArenaBuild.Ranked(hero.Id, x.Weapon, hero.SkillIds, policy, new[] { line }, rules.Version), rules);
            ArenaFighter[] fa = { Make(a, ArenaPolicy.Careful), Make(a, ArenaPolicy.Bold) };
            ArenaFighter[] fb = { Make(b, ArenaPolicy.Careful), Make(b, ArenaPolicy.Bold) };
            return Share(seeds, s => (fa[ArenaRandom.Stream(s, FieldPurpose, 0).NextBelow(2)], fb[ArenaRandom.Stream(s, FieldPurpose, 1).NextBelow(2)]));
        }

        // Every ranked build against the field: the opponent each seed is an identity drawn evenly, then one of its builds
        // drawn evenly (so the Vanguard's three weapons do not crowd Ilya out). The share, and the rounds' P50 length.
        internal static List<(ArenaBuild build, double share, double seconds)> Field(ArenaRules rules, int[] seeds, int salt = 0)
        {
            List<ArenaBuild>[] byIdentity = Identities.Select(x => Builds(rules, x)).ToArray();
            ArenaFighter[][] fighters = byIdentity.Select(list => list.Select(b => ArenaResolver.Resolve(b, rules)).ToArray()).ToArray();
            var all = new List<(ArenaBuild build, ArenaFighter fighter)>();
            for (int i = 0; i < byIdentity.Length; i++)
                for (int j = 0; j < byIdentity[i].Count; j++)
                    all.Add((byIdentity[i][j], fighters[i][j]));
            var result = new (ArenaBuild build, double share, double seconds)[all.Count];
            Parallel.For(0, all.Count, new ParallelOptions { MaxDegreeOfParallelism = Threads }, k =>
            {
                double score = 0;
                var lengths = new List<double>(seeds.Length);
                foreach (int s in seeds)
                {
                    // The build's index enters the seed so builds do not all meet the same opponents in the same order.
                    int seed = ArenaRandom.Stream(s + salt * 1000003, FieldPurpose + 1, k).NextSeed();
                    ArenaRandom dice = ArenaRandom.Stream(seed, FieldPurpose, 2);
                    ArenaFighter[] identity = fighters[dice.NextBelow(fighters.Length)];
                    ArenaFighter foe = identity[dice.NextBelow(identity.Length)];
                    DuelMatch m = DuelCore.BestOfThree(all[k].fighter, foe, seed);
                    score += Score(m.Winner);
                    foreach (DuelRoundResult r in m.Rounds)
                        lengths.Add(r.Ticks / (double)DuelRules.TicksPerSecond);
                }
                lengths.Sort();
                result[k] = (all[k].build, score / seeds.Length, lengths[lengths.Count / 2]);
            });
            return result.ToList();
        }

        private static string Describe(ArenaBuild build, ArenaRules rules)
        {
            ArenaHero hero = rules.Hero(build.HeroId);
            ArenaWeapon weapon = rules.Weapon(build.WeaponId);
            return $"{hero.Name} + {weapon.Name}, {string.Join(", ", build.LineTypes.Select(ArenaGearTable.Id))}, {ArenaSnapshots.PolicyId(build.Policy)}";
        }

        [Test]
        public void Dominance()
        {
            int[] seeds = Enumerable.Range(1, Env("ARENA_DOMINANCE_SEEDS", 2000)).ToArray();
            ArenaRules rules = ArenaRules.Current;
            var watch = Stopwatch.StartNew();
            List<(ArenaBuild build, double share, double seconds)> field = Field(rules, seeds);
            watch.Stop();
            var md = new StringBuilder();
            md.AppendLine("# Ranked builds against the field (docs/55 P0)");
            md.AppendLine();
            md.AppendLine($"Rules v{rules.Version} (hash {ArenaHash.Hex(rules.Hash)}): + level {rules.RankedLevel}, {rules.RankedLines} line types, " +
                          $"{field.Count} builds (identity x line types x policy), {seeds.Length} best-of-threes each against an opponent drawn " +
                          "per seed (an identity evenly, then one of its builds evenly). The bar: no build above about 60 %. " +
                          $"{F(watch.Elapsed.TotalSeconds, "0")} s on {Threads} threads.");
            md.AppendLine();
            md.AppendLine("## Top 10");
            md.AppendLine();
            md.AppendLine("| # | Build | Against the field |");
            md.AppendLine("|---|---|---|");
            int rank = 1;
            foreach (var f in field.OrderByDescending(f => f.share).ThenBy(f => Describe(f.build, rules)).Take(10))
                md.AppendLine($"| {rank++} | {Describe(f.build, rules)} | {P(f.share)} |");
            md.AppendLine();
            md.AppendLine("## Bottom 5");
            md.AppendLine();
            md.AppendLine("| Build | Against the field |");
            md.AppendLine("|---|---|");
            foreach (var f in field.OrderBy(f => f.share).ThenBy(f => Describe(f.build, rules)).Take(5))
                md.AppendLine($"| {Describe(f.build, rules)} | {P(f.share)} |");
            md.AppendLine();
            md.AppendLine("## By identity and by line type");
            md.AppendLine();
            md.AppendLine("| Identity | Builds | Mean | Best | Worst |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (Identity x in Identities)
            {
                var mine = field.Where(f => f.build.HeroId == x.Hero && f.build.WeaponId == x.Weapon).ToArray();
                md.AppendLine($"| {x.Name} | {mine.Length} | {P(mine.Average(f => f.share))} | {P(mine.Max(f => f.share))} | {P(mine.Min(f => f.share))} |");
            }
            md.AppendLine();
            md.AppendLine("| Line type | Builds carrying it | Their mean | Builds without it | Their mean |");
            md.AppendLine("|---|---|---|---|---|");
            for (int i = 0; i < ArenaGearTable.StatCount; i++)
            {
                var stat = (ArenaStat)i;
                if (rules.RankedLineValue(stat) == 0)
                    continue;
                var with = field.Where(f => f.build.LineTypes.Contains(stat)).ToArray();
                var without = field.Where(f => !f.build.LineTypes.Contains(stat) && rules.InRankedPool(stat, rules.Hero(f.build.HeroId))).ToArray();
                md.AppendLine($"| {ArenaGearTable.Id(stat)} | {with.Length} | {P(with.Average(f => f.share))} | {without.Length} | {(without.Length > 0 ? P(without.Average(f => f.share)) : "-")} |");
            }
            md.AppendLine();
            md.AppendLine("| Policy | Mean |");
            md.AppendLine("|---|---|");
            foreach (ArenaPolicy policy in new[] { ArenaPolicy.Careful, ArenaPolicy.Bold })
                md.AppendLine($"| {ArenaSnapshots.PolicyId(policy)} | {P(field.Where(f => f.build.Policy == policy).Average(f => f.share))} |");
            Write("arena-p0-dominance.md", md.ToString());
            Assert.That(field.Max(f => f.share), Is.LessThanOrEqualTo(0.62), "a ranked build dominates the field");
        }

        [Test]
        public void OpenCurve()
        {
            int[] seeds = Enumerable.Range(1, Env("ARENA_SEEDS", 2000)).ToArray();
            ArenaRules rules = ArenaRules.Current;
            var md = new StringBuilder();
            md.AppendLine("# The open arena's gear curve (docs/55 P0)");
            md.AppendLine();
            md.AppendLine($"Rules v{rules.Version}. A wears gear made to a power index (ArenaRivals.GearAt), B wears none; same identity unless " +
                          "named; A's policy and B's drawn per seed; best of three, A's wins (draws half). The rows' gear is the ladder's own recipe.");
            md.AppendLine();
            int[] targets = { 0, 300, 600, 1000, 1500, 2000, 3000, 4000, 5000 };
            md.AppendLine("| Target | Gear power | Level | " + string.Join(" | ", Identities.Select(i => i.Name + " mirror")) + " | Ilya (A) against the Vanguard + Sword | Vanguard + Sword (A) against Ilya |");
            md.AppendLine("|---|---|---|" + string.Join("|", Identities.Select(_ => "---")) + "|---|---|");
            foreach (int target in targets)
            {
                List<ArenaGearPiece> gear = ArenaRivals.GearAt(target);
                int power = ArenaGearTable.Power(ArenaGearTable.Totals(gear));
                var cells = new List<string>();
                foreach (Identity x in Identities)
                    cells.Add(P(Geared(rules, x, gear, x, seeds)));
                cells.Add(P(Geared(rules, Identities[3], gear, Identities[0], seeds)));
                cells.Add(P(Geared(rules, Identities[0], gear, Identities[3], seeds)));
                md.AppendLine($"| +{F(target / 100.0)} % | +{F(power / 100.0)} % | +{gear[0].Level} | {string.Join(" | ", cells)} |");
            }
            md.AppendLine();
            md.AppendLine("Equal gear on both sides: does the hero balance tuned in ranked (at its + level and lines, about +24 % power) hold " +
                          "across the gear's range? As shipped (the burn grows with the output bonus), and with the run's flat burn - its " +
                          $"coefficients first calibrated in ranked the same way ({Env("ARENA_SWEEP_PASSES", 5)} passes of Normalisation's loop):");
            md.AppendLine();
            ArenaRules flat = Calibrate(rules.WithBurnScaling(false), Enumerable.Range(1, Env("ARENA_LOOP_SEEDS", 400)).ToArray(),
                Env("ARENA_SWEEP_PASSES", 5), null);
            md.AppendLine($"(Flat burn settled at Staff x{F(flat.Weapon(ArenaRules.Staff).DamagePermille / 1000.0, "0.000")}, Daggers " +
                          $"x{F(flat.Weapon(ArenaRules.Daggers).DamagePermille / 1000.0, "0.000")}, Ilya x{F(flat.Hero(ArenaRules.Ilya).DamagePermille / 1000.0, "0.000")}.)");
            md.AppendLine();
            md.AppendLine("| Burn | Both at | Ilya against the Vanguard + Sword | + Staff | + Daggers | Vanguard + Sword against + Staff | against + Daggers |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach ((string label, ArenaRules variant) in new[] { ("grows (shipped)", rules), ("flat (the run's)", flat) })
            {
                foreach (int target in new[] { 0, 1000, 2500, 4000 })
                {
                    List<ArenaGearPiece> gear = ArenaRivals.GearAt(target);
                    md.AppendLine($"| {label} | +{F(ArenaGearTable.Power(ArenaGearTable.Totals(gear)) / 100.0)} % | " +
                                  $"{P(GearedAgainst(variant, Identities[3], gear, gear, seeds, Identities[0]))} | " +
                                  $"{P(GearedAgainst(variant, Identities[3], gear, gear, seeds, Identities[1]))} | " +
                                  $"{P(GearedAgainst(variant, Identities[3], gear, gear, seeds, Identities[2]))} | " +
                                  $"{P(GearedAgainst(variant, Identities[0], gear, gear, seeds, Identities[1]))} | " +
                                  $"{P(GearedAgainst(variant, Identities[0], gear, gear, seeds, Identities[2]))} |");
                }
            }
            md.AppendLine();
            md.AppendLine("Matched by gear power: A against B with B at +20 % and A a little stronger (Vanguard + Sword mirror):");
            md.AppendLine();
            md.AppendLine("| A's power | B's power | Gap (A over B) | A's wins |");
            md.AppendLine("|---|---|---|---|");
            List<ArenaGearPiece> twenty = ArenaRivals.GearAt(2000);
            int basePower = ArenaGearTable.Power(ArenaGearTable.Totals(twenty));
            foreach (int target in new[] { 2000, 2200, 2500, 3000, 3500 })
            {
                List<ArenaGearPiece> gear = ArenaRivals.GearAt(target);
                int power = ArenaGearTable.Power(ArenaGearTable.Totals(gear));
                double share = GearedAgainst(rules, Identities[0], gear, twenty, seeds);
                md.AppendLine($"| +{F(power / 100.0)} % | +{F(basePower / 100.0)} % | {F((10000.0 + power) / (10000.0 + basePower) * 100 - 100)} % | {P(share)} |");
            }
            Write("arena-p0-open.md", md.ToString());
        }

        private static double Geared(ArenaRules rules, Identity x, List<ArenaGearPiece> gear, Identity y, int[] seeds) =>
            GearedAgainst(rules, x, gear, new List<ArenaGearPiece>(), seeds, y);

        private static double GearedAgainst(ArenaRules rules, Identity x, List<ArenaGearPiece> gearA, List<ArenaGearPiece> gearB, int[] seeds,
            Identity? against = null)
        {
            Identity y = against ?? x;
            ArenaHero hx = rules.Hero(x.Hero);
            ArenaHero hy = rules.Hero(y.Hero);
            ArenaFighter[] fa = new[] { ArenaPolicy.Careful, ArenaPolicy.Bold }
                .Select(p => ArenaResolver.Resolve(ArenaBuild.Open(hx.Id, x.Weapon, hx.SkillIds, p, gearA), rules)).ToArray();
            ArenaFighter[] fb = new[] { ArenaPolicy.Careful, ArenaPolicy.Bold }
                .Select(p => ArenaResolver.Resolve(ArenaBuild.Open(hy.Id, y.Weapon, hy.SkillIds, p, gearB), rules)).ToArray();
            return Share(seeds, s => (fa[ArenaRandom.Stream(s, FieldPurpose, 0).NextBelow(2)], fb[ArenaRandom.Stream(s, FieldPurpose, 1).NextBelow(2)]));
        }

        [Test]
        public void Cost()
        {
            int count = Env("ARENA_SEEDS", 2000);
            ArenaRules rules = ArenaRules.Current;
            ArenaBuild[] builds = Enumerable.Range(0, 64).Select(i => ArenaRivals.Ranked(7, i, rules)).ToArray();
            ArenaFighter[] fighters = builds.Select(b => ArenaResolver.Resolve(b, rules)).ToArray();
            (ArenaFighter, ArenaFighter) Pair(int s) => (fighters[s % 64], fighters[(s * 7 + 3) % 64]);
            for (int s = 1; s <= 300; s++)
                DuelCore.BestOfThree(Pair(s).Item1, Pair(s).Item2, s);
            var watch = Stopwatch.StartNew();
            long events = 0, ticks = 0;
            for (int s = 1; s <= count; s++)
            {
                DuelMatch m = DuelCore.BestOfThree(Pair(s).Item1, Pair(s).Item2, s);
                events += m.Events;
                ticks += m.TotalTicks;
            }
            watch.Stop();
            double perMatch = watch.Elapsed.TotalMilliseconds * 1000 / count;
            ulong[] once = Enumerable.Range(1, count).Select(s => DuelCore.BestOfThree(Pair(s).Item1, Pair(s).Item2, s).Fingerprint).ToArray();
            var twice = new ulong[count];
            Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = Threads }, i =>
                twice[i] = DuelCore.BestOfThree(Pair(i + 1).Item1, Pair(i + 1).Item2, i + 1).Fingerprint);
            int same = once.Zip(twice, (x, y) => x == y ? 1 : 0).Sum();
            int replayed = 0;
            long logBytes = 0;
            for (int s = 1; s <= Math.Min(count, 500); s++)
            {
                var log = new DuelLog();
                DuelMatch m = DuelCore.BestOfThree(Pair(s).Item1, Pair(s).Item2, s, log);
                List<DuelReplayRound> rounds = DuelReplay.Play(log);
                bool ok = log.Fingerprint() == m.Fingerprint && rounds.Count == m.Rounds.Count;
                for (int r = 0; ok && r < rounds.Count; r++)
                    ok = rounds[r].Winner == m.Rounds[r].Winner && rounds[r].HealthA == m.Rounds[r].HealthA && rounds[r].HealthB == m.Rounds[r].HealthB &&
                         rounds[r].PositionA == m.Rounds[r].PositionA && rounds[r].PositionB == m.Rounds[r].PositionB && rounds[r].Ticks == m.Rounds[r].Ticks;
                replayed += ok ? 1 : 0;
                logBytes += log.Count * 32L;
            }
            var md = new StringBuilder();
            md.AppendLine("# The arena core's cost and replays (docs/55 P0)");
            md.AppendLine();
            md.AppendLine($"Rules v{rules.Version} (hash {ArenaHash.Hex(rules.Hash)}); 64 generated ranked rivals paired over seeds 1-{count}.");
            md.AppendLine();
            md.AppendLine($"- A best of three: **{F(perMatch)} us** on one core of this machine (others' simulations running), " +
                          $"{F(ticks / (double)count / DuelRules.TicksPerSecond)} s of fighting and {F(events / (double)count, "0")} events on average.");
            md.AppendLine($"- Sequential against {Threads} threads: **{same} of {count}** fingerprints identical; {once.Distinct().Count()} distinct.");
            md.AppendLine($"- Logs replayed (health, winner, positions and the fingerprint from the log alone): **{replayed} of {Math.Min(count, 500)}**; " +
                          $"a log is {F(logBytes / (double)Math.Min(count, 500) / 1024, "0.0")} KB uncompressed at 32 bytes an event.");
            Write("arena-p0-cost.md", md.ToString());
            Assert.That(same, Is.EqualTo(count));
            Assert.That(replayed, Is.EqualTo(Math.Min(count, 500)));
        }

        // ---- docs/55 section 10.10: the training rivals' handicap and the open offers' chance ----

        // The logit and its inverse, for fits (research only; the shipped tables are integers).
        private static double Logit(double p) => Math.Log(p / (1 - p));

        private static double Logistic(double x) => 1 / (1 + Math.Exp(-x));

        // A careful player (an identity evenly, then one of its careful builds) against generated ranked rivals (drawn as
        // ArenaRivals draws them) wearing a handicap; the player's share of best-of-threes.
        private static double TrainingShare(ArenaRules rules, ArenaFighter[][] players, ArenaBuild[] rivals, int handicap, int[] seeds,
            int identity = -1)
        {
            ArenaFighter[] foes = rivals.Select(b => ArenaResolver.Resolve(b.WithHandicap(handicap, rules.Version), rules)).ToArray();
            return Share(seeds, s =>
            {
                ArenaRandom dice = ArenaRandom.Stream(s, FieldPurpose, 3);
                ArenaFighter[] pool = players[identity >= 0 ? identity : dice.NextBelow(players.Length)];
                return (pool[dice.NextBelow(pool.Length)], foes[dice.NextBelow(foes.Length)]);
            });
        }

        [Test]
        public void TrainingRivals()
        {
            int[] seeds = Enumerable.Range(1, Env("ARENA_TRAINING_SEEDS", 4000)).ToArray();
            int[] check = Enumerable.Range(1, Env("ARENA_SEEDS", 2000)).ToArray();
            ArenaRules rules = ArenaRules.Current;
            ArenaFighter[][] players = Identities.Select(x => Builds(rules, x).Where(b => b.Policy == ArenaPolicy.Careful)
                .Select(b => ArenaResolver.Resolve(b, rules)).ToArray()).ToArray();
            ArenaBuild[] rivals = Enumerable.Range(0, 600).Select(i => ArenaRivals.Ranked(4242, i, rules)).ToArray();
            int rungs = ArenaLadder.Size;
            var md = new StringBuilder();
            md.AppendLine("# The training rivals' handicap (docs/55 section 10.10)");
            md.AppendLine();
            md.AppendLine($"Rules v{rules.Version} (hash {ArenaHash.Hex(rules.Hash)}). The player: careful, an identity drawn evenly, then one of its " +
                          "careful ranked builds (the middle of the field by construction); the rival: one of 600 generated ranked rivals " +
                          "(ArenaRivals.Ranked: a hero evenly, a weapon it carries, a policy, three line types), its damage at the handicap. " +
                          "Best of three, the player's wins (draws half).");
            md.AppendLine();

            // 1. The player's share against the handicap over the range the table needs (75 % to 50 %), and a logistic fit
            // there (logit linear in the handicap; past the range the logit steepens, so the fit stays inside it).
            md.AppendLine($"## 1. The player's share by the rival's handicap ({seeds.Length} seeds a point)");
            md.AppendLine();
            md.AppendLine("| Rival's damage (per mille) | Player's wins | Logit |");
            md.AppendLine("|---|---|---|");
            int[] grid = { 1000, 994, 988, 982, 976, 970, 964 };
            var xs = new List<double>();
            var ys = new List<double>();
            foreach (int h in grid)
            {
                double w = TrainingShare(rules, players, rivals, h, seeds);
                md.AppendLine($"| {h} | {P(w)} | {F(Logit(w), "0.000")} |");
                xs.Add(1000 - h);
                ys.Add(Logit(w));
            }
            double mx = xs.Average(), my = ys.Average();
            double slope = xs.Zip(ys, (x, y) => (x - mx) * (y - my)).Sum() / xs.Sum(x => (x - mx) * (x - mx));
            double intercept = my - slope * mx;
            md.AppendLine();
            md.AppendLine($"Fit: logit(win) = {F(intercept, "0.000")} + {F(slope, "0.0000")} x (1000 - handicap) - each per mille off the " +
                          $"rival's damage is worth {F(slope * 25, "0.0")} points of the player's win near 50 %.");
            md.AppendLine();

            // 2. The curve: even in log-odds from 75 % at the bottom rung to 50 % at the top, inverted through the fit's slope
            // and anchored at the top - the top rung is a real rival, no handicap (the field's own balance makes it even).
            double top = Logit(0.75);
            var table = new int[rungs];
            for (int r = 0; r < rungs; r++)
            {
                double target = top * (rungs - 1 - r) / (rungs - 1);
                int h = (int)Math.Round(1000 - target / slope);
                table[r] = Math.Max(ArenaBuild.MinHandicap, Math.Min(ArenaBuild.NoHandicap, h));
            }
            md.AppendLine("## 2. The table (even in log-odds: each rung the same step in Elo terms) and the shares it measures");
            md.AppendLine();
            md.AppendLine($"Proposed: `{{ {string.Join(", ", table)} }}`; shipped: `{{ {string.Join(", ", Enumerable.Range(0, rules.TrainingRungs).Select(rules.TrainingHandicap))} }}`.");
            md.AppendLine();
            md.AppendLine($"| Rung | Rival's rating | Handicap (shipped) | Target | Measured ({check.Length} seeds) |");
            md.AppendLine("|---|---|---|---|---|");
            var measured = new double[rungs];
            for (int r = 0; r < rungs; r++)
            {
                double target = Logistic(top * (rungs - 1 - r) / (rungs - 1));
                measured[r] = TrainingShare(rules, players, rivals, rules.TrainingHandicap(r), check);
                md.AppendLine($"| {r} | {ArenaLadder.LowRating + (ArenaLadder.HighRating - ArenaLadder.LowRating) * r / (rungs - 1)} | " +
                              $"{rules.TrainingHandicap(r)} | {P(target)} | {P(measured[r])} |");
            }
            md.AppendLine();

            // 3. How far a player's identity moves it.
            md.AppendLine("## 3. By the player's identity (careful builds of one identity), at the bottom, middle and top rungs");
            md.AppendLine();
            md.AppendLine("| Player | Rung 0 | Rung 14 | Rung 29 |");
            md.AppendLine("|---|---|---|---|");
            for (int i = 0; i < Identities.Length; i++)
            {
                md.AppendLine($"| {Identities[i].Name} | {P(TrainingShare(rules, players, rivals, rules.TrainingHandicap(0), check, i))} | " +
                              $"{P(TrainingShare(rules, players, rivals, rules.TrainingHandicap(14), check, i))} | " +
                              $"{P(TrainingShare(rules, players, rivals, rules.TrainingHandicap(rungs - 1), check, i))} |");
            }
            Write("arena-p0-training.md", md.ToString());
            for (int r = 1; r < rungs; r++)
                Assert.That(rules.TrainingHandicap(r), Is.GreaterThanOrEqualTo(rules.TrainingHandicap(r - 1)), "the handicap eases rung by rung");
        }

        [Test]
        public void OpenOffers()
        {
            int[] seeds = Enumerable.Range(1, Env("ARENA_OFFER_SEEDS", 1500)).ToArray();
            int[] check = Enumerable.Range(1, Env("ARENA_SEEDS", 2000)).ToArray();
            ArenaRules rules = ArenaRules.Current;
            var md = new StringBuilder();
            md.AppendLine("# The open offers' expected chance (docs/55 section 10.10)");
            md.AppendLine();
            md.AppendLine($"Rules v{rules.Version} (hash {ArenaHash.Hex(rules.Hash)}). A wears gear made near a base power plus a gap " +
                          "(ArenaRivals.GearNear), B near the base; each side's identity drawn evenly from the four and its policy at random, " +
                          "as the offers' rivals are; best of three, A's wins (draws half). The gap is read as the offers read it: a ratio, " +
                          "(1 + A's power) / (1 + B's) - 1.");
            md.AppendLine();
            md.AppendLine($"## 1. A's wins by base and gap ({seeds.Length} seeds a cell)");
            md.AppendLine();
            int[] bases = { 500, 1500, 2500, 3500 };
            int[] gaps = { -800, -400, -200, -100, 0, 100, 200, 400, 800 };
            md.AppendLine("| Base | " + string.Join(" | ", gaps.Select(g => (g >= 0 ? "+" : "") + F(g / 100.0, "0.0") + " %")) + " |");
            md.AppendLine("|---|" + string.Join("|", gaps.Select(_ => "---")) + "|");
            var points = new List<(double gap, double win)>();
            foreach (int b in bases)
            {
                var cells = new List<string>();
                foreach (int g in gaps)
                {
                    List<ArenaGearPiece> gearA = ArenaRivals.GearNear(b + g);
                    List<ArenaGearPiece> gearB = ArenaRivals.GearNear(b);
                    int pa = ArenaGearTable.Power(ArenaGearTable.Totals(gearA));
                    int pb = ArenaGearTable.Power(ArenaGearTable.Totals(gearB));
                    double w = AnyIdentity(rules, gearA, gearB, seeds);
                    double ratio = ((10000.0 + pa) / (10000.0 + pb) - 1) * 100;
                    points.Add((ratio, w));
                    cells.Add($"{P(w)} ({F(ratio, "+0.0;-0.0;0.0")})");
                }
                md.AppendLine($"| +{F(b / 100.0, "0")} % | {string.Join(" | ", cells)} |");
            }
            md.AppendLine();
            md.AppendLine("(Each cell: A's wins, and in brackets the ratio gap in per cent the two gears actually made.)");
            md.AppendLine();

            // 2. The fit: a logistic in the ratio gap through 50 % at none, least squares on the logits of the cells inside
            // 2-98 %.
            var usable = points.Where(p => p.win > 0.02 && p.win < 0.98 && Math.Abs(p.gap) > 0.05).ToList();
            double s = usable.Sum(p => p.gap * Logit(p.win)) / usable.Sum(p => p.gap * p.gap);
            var table = new int[21];
            for (int i = 0; i < table.Length; i++)
                table[i] = (int)Math.Round(1000 * Logistic(s * i * ArenaRules.OpenChanceStep / 100.0));
            md.AppendLine("## 2. The fitted table");
            md.AppendLine();
            md.AppendLine($"logit(A's wins) = {F(s, "0.000")} x the ratio gap in per cent ({usable.Count} cells). A 2 % gap: " +
                          $"{P(Logistic(2 * s))}. 10.6's matched-power points on the Vanguard + Sword: a 1.7 % gap 67.8 % (the fit " +
                          $"{P(Logistic(1.7 * s))}), 4.1 % 83.0 % ({P(Logistic(4.1 * s))}), 8.2 % 94.4 % ({P(Logistic(8.2 * s))}).");
            md.AppendLine();
            md.AppendLine($"Proposed, per mille at gaps 0, 0.5, ... 10 %: `{{ {string.Join(", ", table)} }}`; shipped: " +
                          $"`{{ {string.Join(", ", Enumerable.Range(0, 21).Select(i => rules.OpenChance(i * ArenaRules.OpenChanceStep, 0)))} }}`.");
            md.AppendLine();

            // 3. The offers themselves, as a player sees them: the card's chance against the measured share.
            md.AppendLine($"## 3. The three offers at a few player powers: the card's chance against the measured share ({check.Length} seeds)");
            md.AppendLine();
            md.AppendLine("| Player's power | Offer | Rival's power | Card's chance | Measured |");
            md.AppendLine("|---|---|---|---|---|");
            var save = ArenaLadder.NewSave(55, rules);
            foreach (int power in new[] { 0, 1000, 2500, 4000, 5000 })
            {
                ArenaHero vanguard = rules.Hero(ArenaRules.Vanguard);
                ArenaBuild player = ArenaBuild.Open(vanguard.Id, ArenaRules.Sword, vanguard.SkillIds, ArenaPolicy.Careful, ArenaRivals.GearNear(power), rules.Version);
                foreach (ArenaOpenOffer offer in ArenaLadder.OpenOffers(save, player, rules))
                {
                    double w = AnyIdentity(rules, ArenaRivals.GearNear(power), offer.Build.Gear.ToList(), check);
                    md.AppendLine($"| +{F(offer.PlayerPower / 100.0)} % | {offer.Kind} | +{F(offer.RivalPower / 100.0)} % | " +
                                  $"{F(offer.ExpectedPermille / 10.0)} % | {P(w)} |");
                }
            }
            md.AppendLine();
            md.AppendLine("(The measured column draws both identities at random, as the table does; a card is for any rival.)");
            Write("arena-p0-offers.md", md.ToString());
        }

        // A in gearA against B in gearB, each side's identity drawn evenly from the four and its policy at random.
        private static double AnyIdentity(ArenaRules rules, List<ArenaGearPiece> gearA, List<ArenaGearPiece> gearB, int[] seeds)
        {
            ArenaFighter Make(Identity x, ArenaPolicy p, List<ArenaGearPiece> gear)
            {
                ArenaHero hero = rules.Hero(x.Hero);
                return ArenaResolver.Resolve(ArenaBuild.Open(hero.Id, x.Weapon, hero.SkillIds, p, gear, rules.Version), rules);
            }
            ArenaFighter[] fa = Identities.SelectMany(x => new[] { Make(x, ArenaPolicy.Careful, gearA), Make(x, ArenaPolicy.Bold, gearA) }).ToArray();
            ArenaFighter[] fb = Identities.SelectMany(x => new[] { Make(x, ArenaPolicy.Careful, gearB), Make(x, ArenaPolicy.Bold, gearB) }).ToArray();
            return Share(seeds, sd => (fa[ArenaRandom.Stream(sd, FieldPurpose, 0).NextBelow(fa.Length)],
                fb[ArenaRandom.Stream(sd, FieldPurpose, 1).NextBelow(fb.Length)]));
        }

        private static void Write(string name, string text)
        {
            string output = Environment.GetEnvironmentVariable("ARENA_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, name), text);
            TestContext.Progress.WriteLine(text);
        }

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }
    }
}
