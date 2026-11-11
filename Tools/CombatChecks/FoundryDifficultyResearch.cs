using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The difficulty research of 2026-11-04 (docs/cloud/2026-11-04-difficulty.md) on demand: the autopilot's cohorts with a
    // human's reaction, aim and attention (PilotRealism), what keeps careful Ilya alive, and the structural candidates
    // (FoundryLevers' Warden echo, slag, elite stomp and ambush surges), every cohort and both heroes on the same seeds.
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~FoundryDifficultyResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (the processors), FOUNDRY_OUT
    // (the repository's TestResults), DIFFICULTY_PARTS (comma separated, every part by default: reaction, threats, parts,
    // fine, why, staff, structural, structural2). Each part writes difficulty-<part>.md and the rows of every batch to
    // difficulty-<part>.csv. All eight take about 75 minutes on four cores at 200 seeds. The Warden echo's second round
    // (docs/41, status 2026-11-11), echo-share and echo-enraged, runs only when named there: see EchoWithHealth.
    [Explicit("The difficulty research's batches: run them by name.")]
    public sealed class FoundryDifficultyResearch
    {
        private static readonly float[] Reactions = { 0f, .15f, .25f, .35f, .5f };

        // The research's two humans (PilotRealism.Typical and Slow).
        private static PilotRealism Typical() => PilotRealism.Typical();
        private static PilotRealism Slow() => PilotRealism.Slow();

        private int _parallel;
        private int[] _seeds;

        [Test]
        public void TheDifficultyResearch()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            _parallel = Setting("FOUNDRY_PARALLEL", Environment.ProcessorCount);
            _seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string parts = Environment.GetEnvironmentVariable("DIFFICULTY_PARTS") ?? "reaction,threats,parts,fine,why,staff,structural,structural2";
            foreach (string part in parts.Split(',').Select(p => p.Trim()).Where(p => p.Length > 0))
            {
                var rows = new List<Row>();
                string md;
                switch (part)
                {
                    case "reaction": md = ReactionSweep(rows); break;
                    case "threats": md = Threats(rows); break;
                    case "parts": md = Parts(rows); break;
                    case "why": md = Why(rows); break;
                    case "structural": md = Structural(rows); break;
                    case "fine": md = Fine(rows); break;
                    case "staff": md = StaffParts(rows); break;
                    case "structural2": md = StructuralTuned(rows); break;
                    case "echo-share": md = EchoWithHealth(rows); break;
                    case "echo-enraged": md = EchoEnragedOnly(rows); break;
                    default: throw new ArgumentException("No such part: " + part);
                }
                File.WriteAllText(Path.Combine(output, $"difficulty-{part}.md"), md);
                File.WriteAllText(Path.Combine(output, $"difficulty-{part}.csv"), Csv(rows));
                TestContext.WriteLine(md);
            }
            Assert.Pass();
        }

        // ---------------------------------------------------------------------------------------------------------
        // The parts
        // ---------------------------------------------------------------------------------------------------------

        // (1) Every threat answered a reaction late, nothing else: each cohort's clears as the reaction grows.
        private string ReactionSweep(List<Row> rows)
        {
            var configs = Reactions.Select(r => (Name: r == 0f ? "frame-perfect" : $"reaction {r:0.00} s",
                Realism: r == 0f ? null : new PilotRealism { Name = $"reaction {r:0.00}", MarkReaction = r, GlowReaction = r, FoeReaction = r },
                Structure: (Action<DescentSimulation.FoundryLevers>)null)).ToList();
            configs.Add(("typical human: " + Describe(Typical()), Typical(), null));
            configs.Add(("slow human: " + Describe(Slow()), Slow(), null));
            return "## Reaction on every threat\n\n" + Matrix(configs, AutopilotCohort.Cohorts, rows);
        }

        // (1) The reaction between the sweep's steps where the careful cohort crosses 80-90 %, alone and with the typical
        // human's aim and attention.
        private string Fine(List<Row> rows)
        {
            // A perturbation too small to matter as skill - a step 0.05 units off - reads how far the floor's chaos alone
            // moves a cell: the noise floor of every comparison here.
            var configs = new List<(string, PilotRealism, Action<DescentSimulation.FoundryLevers>)>
            {
                ("noise floor: aim error 0.05", new PilotRealism { Name = "aim .05", AimError = .05f }, null)
            };
            foreach (float r in new[] { .30f, .40f, .45f })
                configs.Add(($"reaction {r:0.00} s", new PilotRealism { Name = $"reaction {r:0.00}", MarkReaction = r, GlowReaction = r, FoeReaction = r }, null));
            foreach (float r in new[] { .15f, .20f, .30f, .35f })
            {
                PilotRealism human = PilotRealism.Human(r, .5f, 4f, .4f, .5f);
                configs.Add(("typical's aim and attention, " + human.Name, human, null));
            }
            return "## Reaction, finer\n\n" + Matrix(configs, AutopilotCohort.Cohorts, rows);
        }

        // (1) Which threats the reaction matters for: one threat at a time, the others read at once.
        private string Threats(List<Row> rows)
        {
            var configs = new List<(string, PilotRealism, Action<DescentSimulation.FoundryLevers>)> { ("frame-perfect", null, null) };
            foreach (float r in new[] { .35f, .5f })
            {
                configs.Add(($"the Warden's mark only, {r:0.00} s", new PilotRealism { Name = "mark", MarkReaction = r }, null));
                configs.Add(($"the pit's glow only, {r:0.00} s", new PilotRealism { Name = "glow", GlowReaction = r }, null));
                configs.Add(($"enemies closing only, {r:0.00} s", new PilotRealism { Name = "foes", FoeReaction = r }, null));
            }
            return "## One threat at a time\n\n" + Matrix(configs, AutopilotCohort.Cohorts, rows);
        }

        // (1) Aim and attention alone, and with the reaction.
        private string Parts(List<Row> rows)
        {
            var configs = new List<(string, PilotRealism, Action<DescentSimulation.FoundryLevers>)>
            {
                ("frame-perfect", null, null),
                ("aim error 0.5", new PilotRealism { Name = "aim .5", AimError = .5f }, null),
                ("aim error 1.0", new PilotRealism { Name = "aim 1", AimError = 1f }, null),
                ("lapses 4/min x 0.4 s", new PilotRealism { Name = "lapses 4", LapsesPerMinute = 4f, LapseSeconds = .4f }, null),
                ("lapses 10/min x 0.6 s", new PilotRealism { Name = "lapses 10", LapsesPerMinute = 10f, LapseSeconds = .6f }, null),
                ("card lapse 0.5 s", new PilotRealism { Name = "card .5", CardLapseSeconds = .5f }, null),
                ("card lapse 1.0 s", new PilotRealism { Name = "card 1", CardLapseSeconds = 1f }, null),
                ("reaction 0.25 s alone", new PilotRealism { Name = "react .25", MarkReaction = .25f, GlowReaction = .25f, FoeReaction = .25f }, null),
                ("typical human: " + Describe(Typical()), Typical(), null),
            };
            return "## Aim and attention\n\n" + Matrix(configs, AutopilotCohort.Cohorts, rows);
        }

        // (2) Why Ilya is safe: where and by what each hero loses health, its near-deaths, and the hero swapped piece by
        // piece, frame-perfect and as the typical human.
        private string Why(List<Row> rows)
        {
            var md = new StringBuilder("## Why Ilya is safe\n\n");
            var cohorts = new[] { AutopilotCohort.Careful, AutopilotCohort.Standing, AutopilotCohort.Clumsy };
            foreach (PilotRealism realism in new[] { null, Typical() })
            {
                md.AppendLine($"### {(realism == null ? "Frame-perfect" : "The typical human")}: health lost by place and blow").AppendLine();
                md.AppendLine("Mean health lost a run (of every run), the five largest place-and-blow pairs, then the runs whose lowest health was under 25 % " +
                              "of the maximum and survived the floor (near-deaths) by where and what took them there, and the deaths.").AppendLine();
                md.AppendLine("| Cohort | Hero | Cleared | Health lost a run: total; largest place / blow | Near-deaths (< 25 %, survived): where / blow | Deaths: where / blow | Lowest health P10 / P50 |");
                md.AppendLine("|---|---|---|---|---|---|---|");
                foreach (AutopilotCohort cohort in cohorts)
                    foreach (DescentSimulation.FoundryHero hero in Heroes())
                    {
                        FoundryBatch.Batch b = Run(hero, Levers(cohort, realism, null, "why"));
                        rows.Add(Summarise(realism == null ? "frame-perfect" : "typical", cohort, b));
                        md.AppendLine($"| {cohort.Name} | {hero.Name} | {b.ClearedCount} | {TakenBy(b)} | {NearDeaths(b)} | {DeathsBy(b)} | " +
                                      $"{P(b.Runs.Select(r => 100f * r.LowestHealthShare), 10)} / {P(b.Runs.Select(r => 100f * r.LowestHealthShare), 50)} % |");
                    }
                md.AppendLine();
            }

            md.AppendLine("### The heroes swapped piece by piece").AppendLine();
            md.Append(VariantTable(Variants(), cohorts, rows));
            return md.ToString();
        }

        // (2) The Staff taken apart: its reach, its splash's radius and its splash's share, on Ilya.
        private string StaffParts(List<Row> rows)
        {
            var variants = new List<(string, DescentSimulation.FoundryHero)> { ("Ilya", DescentSimulation.CinderArcanist()) };
            DescentSimulation.FoundryHero Ilya(string name, float range, float splashRadius, float splashFraction)
            {
                DescentSimulation.FoundryHero hero = DescentSimulation.CinderArcanist();
                DescentSimulation.HeroWeapon staff = DescentSimulation.Staff();
                staff.Range = range;
                staff.Pattern = splashRadius > 0f
                    ? new Cryptforge.Combat.AttackPattern(Cryptforge.Combat.WeaponBehavior.Area, splashRadius, splashFraction)
                    : new Cryptforge.Combat.AttackPattern(Cryptforge.Combat.WeaponBehavior.DirectHit);
                hero.Weapon = staff;
                hero.Name = name;
                return hero;
            }
            foreach ((string name, float range, float radius, float fraction) in new[]
                     {
                         ("Ilya, Staff reach 2.1 (the Sword's)", 2.1f, 3.5f, .75f),
                         ("Ilya, Staff splash radius 2.0 (the Sword's cleave)", 2.4f, 2f, .75f),
                         ("Ilya, Staff splash share 0.4", 2.4f, 3.5f, .4f),
                         ("Ilya, Staff without splash", 2.4f, 0f, 0f),
                         ("Ilya, Staff reach 2.1, splash 2.0", 2.1f, 2f, .75f)
                     })
                variants.Add((name, Ilya(name, range, radius, fraction)));
            return "## The Staff taken apart\n\n" +
                   VariantTable(variants, new[] { AutopilotCohort.Careful, AutopilotCohort.Standing, AutopilotCohort.Clumsy }, rows);
        }

        private string VariantTable(IEnumerable<(string, DescentSimulation.FoundryHero)> variants, AutopilotCohort[] cohorts, List<Row> rows)
        {
            var md = new StringBuilder();
            md.AppendLine("Each row changes one thing of a hero's definition and plays the same seeds; clears for the careful, standing and clumsy cohorts, frame-perfect / typical human.").AppendLine();
            md.AppendLine("| Hero | Careful | Standing | Clumsy | Mean health lost (careful, frame-perfect / typical) |");
            md.AppendLine("|---|---|---|---|---|");
            foreach ((string name, DescentSimulation.FoundryHero hero) in variants)
            {
                var cells = new List<string>();
                string taken = "";
                foreach (AutopilotCohort cohort in cohorts)
                {
                    var pair = new List<FoundryBatch.Batch>();
                    foreach (PilotRealism realism in new[] { null, Typical() })
                    {
                        FoundryBatch.Batch b = Run(hero, Levers(cohort, realism, null, name));
                        rows.Add(Summarise((realism == null ? "frame-perfect " : "typical ") + name, cohort, b));
                        pair.Add(b);
                    }
                    cells.Add($"{pair[0].ClearedCount} / {pair[1].ClearedCount}");
                    if (cohort == AutopilotCohort.Careful)
                        taken = $"{Mean(pair[0].Runs.Select(Lost))} / {Mean(pair[1].Runs.Select(Lost))}";
                }
                md.AppendLine($"| {name} | {string.Join(" | ", cells)} | {taken} |");
            }
            md.AppendLine();
            return md.ToString();
        }

        // (3) The structural candidates, every cohort, frame-perfect and as the typical human.
        private string Structural(List<Row> rows)
        {
            var options = StructuralOptions();
            var md = new StringBuilder("## Structural candidates\n\n");
            foreach (PilotRealism realism in new[] { null, Typical(), Slow() })
            {
                var configs = options.Select(o => (o.Name, realism, o.Apply)).ToList();
                md.AppendLine($"### {(realism == null ? "Frame-perfect" : realism.Name + ": " + Describe(realism))}").AppendLine();
                md.Append(Matrix(configs, AutopilotCohort.Cohorts, rows));
            }
            return md.ToString();
        }

        // (3) The candidates the first round found worth tuning: an echo a human can read (the slam's own telegraph) at
        // half the blow, slag less often, and the two together.
        private string StructuralTuned(List<Row> rows)
        {
            var options = new List<(string, Action<DescentSimulation.FoundryLevers>)>
            {
                ("shipped", null),
                ("Warden echo 1.1 s, 10 %", l => { l.WardenEchoTelegraph = 1.1f; l.WardenEchoShare = .1f; }),
                ("Warden echo 1.1 s, 20 %", l => l.WardenEchoTelegraph = 1.1f),
                ("slag every 8 s", l => l.SlagEvery = 8f),
                ("slag every 8 s + echo 1.1 s, 10 %", l => { l.SlagEvery = 8f; l.WardenEchoTelegraph = 1.1f; l.WardenEchoShare = .1f; }),
            };
            var md = new StringBuilder("## Structural candidates, tuned\n\n");
            foreach (PilotRealism realism in new[] { null, Typical(), Slow() })
            {
                md.AppendLine($"### {(realism == null ? "Frame-perfect" : realism.Name + ": " + Describe(realism))}").AppendLine();
                md.Append(Matrix(options.Select(o => (o.Item1, realism, o.Item2)).ToList(), AutopilotCohort.Cohorts, rows));
            }
            return md.ToString();
        }

        // The shipped floor, then FoundryBatch.DifficultyLevers, as what each sets on a cohort's levers.
        internal static List<(string Name, Action<DescentSimulation.FoundryLevers> Apply)> StructuralOptions()
        {
            var options = new List<(string, Action<DescentSimulation.FoundryLevers>)> { ("shipped", null) };
            foreach (FoundryBatch.Lever lever in FoundryBatch.DifficultyLevers())
            {
                DescentSimulation.FoundryLevers set = lever.Levers;
                options.Add((lever.Name, l =>
                {
                    l.WardenEchoTelegraph = set.WardenEchoTelegraph;
                    l.SlagEvery = set.SlagEvery;
                    l.EliteStompEvery = set.EliteStompEvery;
                    l.AmbushSurges = set.AmbushSurges;
                }));
            }
            return options;
        }

        // (4) The Warden echo's second round (docs/41, status 2026-11-11, decision 4). The echo at the slam's own 1.1 s
        // telegraph with floor 1's gate boss on a lower health share (FoundryLevers.WardenHealthShare, the shipped
        // WardenRule.GateHealthShare 8.5), so that the frame-perfect careful fight can come back into docs/41 step 4's
        // 45-65 s while the echo still costs a human's hands. ECHO_HEALTH (8.5,7.5,7,6.5,6) and ECHO_SHARES (0.1,0.2) are
        // its grid; ECHO_REALISMS (frame-perfect,typical) its pilots, and ECHO_SHIPPED=0 leaves the shipped floor out of
        // either part. "none" empties a list.
        private string EchoWithHealth(List<Row> rows)
        {
            List<(string Name, Action<DescentSimulation.FoundryLevers> Apply)> options = EchoShipped();
            foreach (float health in Floats("ECHO_HEALTH", "8.5,7.5,7,6.5,6"))
                foreach (float share in Floats("ECHO_SHARES", "0.1,0.2"))
                    options.Add(($"echo 1.1 s, {Number(100f * share)} %, Warden share {Number(health)}", l =>
                    {
                        l.WardenEchoTelegraph = 1.1f;
                        l.WardenEchoShare = share;
                        l.WardenHealthShare = health;
                    }));
            return "## The Warden echo with the Warden's health re-read\n\n" + EchoTables(options, rows);
        }

        // (4) The echo after the enraged Warden's blows alone (FoundryLevers.WardenEchoEnragedOnly), the 2026-11-04
        // research's recommendation: ECHO_ENRAGED_SHARES (0.1,0.2,0.3) at ECHO_ENRAGED_HEALTH (8.5).
        private string EchoEnragedOnly(List<Row> rows)
        {
            List<(string Name, Action<DescentSimulation.FoundryLevers> Apply)> options = EchoShipped();
            foreach (float health in Floats("ECHO_ENRAGED_HEALTH", "8.5"))
                foreach (float share in Floats("ECHO_ENRAGED_SHARES", "0.1,0.2,0.3"))
                    options.Add(($"enraged-only echo 1.1 s, {Number(100f * share)} %, Warden share {Number(health)}", l =>
                    {
                        l.WardenEchoTelegraph = 1.1f;
                        l.WardenEchoShare = share;
                        l.WardenEchoEnragedOnly = true;
                        l.WardenHealthShare = health;
                    }));
            return "## The enraged-only Warden echo\n\n" + EchoTables(options, rows);
        }

        private static List<(string Name, Action<DescentSimulation.FoundryLevers> Apply)> EchoShipped() =>
            Setting("ECHO_SHIPPED", 1) != 0
                ? new List<(string, Action<DescentSimulation.FoundryLevers>)> { ("shipped", null) }
                : new List<(string, Action<DescentSimulation.FoundryLevers>)>();

        // Every option by every pilot, cohort and hero; then the clears, how far careful stands above the others with the
        // last pilot's hands, the careful cohort's Warden fight and floor, and every cell's detail.
        private string EchoTables(List<(string Name, Action<DescentSimulation.FoundryLevers> Apply)> options, List<Row> rows)
        {
            var pilots = new List<(string Name, PilotRealism Realism)>();
            foreach (string word in Words("ECHO_REALISMS", "frame-perfect,typical"))
                pilots.Add((word, word == "frame-perfect" ? null : PilotRealism.Named(word)));
            IReadOnlyList<AutopilotCohort> cohorts = AutopilotCohort.Cohorts;
            DescentSimulation.FoundryHero[] heroes = Heroes();
            var cells = new Dictionary<string, Row[]>();
            string Key(string option, string pilot, AutopilotCohort cohort) => option + "|" + pilot + "|" + cohort.Name;
            foreach ((string name, Action<DescentSimulation.FoundryLevers> apply) in options)
                foreach ((string pilot, PilotRealism realism) in pilots)
                    foreach (AutopilotCohort cohort in cohorts)
                    {
                        var pair = new Row[heroes.Length];
                        for (int h = 0; h < heroes.Length; h++)
                        {
                            pair[h] = Summarise($"{pilot}: {name}", cohort, Run(heroes[h], Levers(cohort, realism, apply, name)));
                            rows.Add(pair[h]);
                        }
                        cells[Key(name, pilot, cohort)] = pair;
                    }

            string Both(Row[] pair, Func<Row, string> read) => string.Join(" / ", pair.Select(read));
            var md = new StringBuilder();
            md.AppendLine($"Seeds {_seeds[0]}-{_seeds[_seeds.Length - 1]} a hero, floor 1, the same seeds in every cell; Vanguard / Ilya; " +
                          "pilots " + string.Join(" ; ", pilots.Select(p => p.Name)) + ".").AppendLine();
            md.AppendLine("### Clears of " + _seeds.Length.ToString(CultureInfo.InvariantCulture) + " (" +
                          string.Join(" ; ", pilots.Select(p => p.Name)) + ")").AppendLine();
            md.AppendLine("| Configuration | " + string.Join(" | ", cohorts.Select(c => c.Name)) + " |");
            md.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")));
            foreach ((string name, _) in options)
                md.AppendLine($"| {name} | " + string.Join(" | ", cohorts.Select(c =>
                    string.Join(" ; ", pilots.Select(p => Both(cells[Key(name, p.Name, c)], r => r.Cleared.ToString(CultureInfo.InvariantCulture)))))) + " |");
            md.AppendLine();

            string last = pilots[pilots.Count - 1].Name;
            md.AppendLine($"### How far careful clears above each other cohort ({last})").AppendLine();
            md.AppendLine("| Configuration | " + string.Join(" | ", cohorts.Skip(1).Select(c => "careful - " + c.Name)) + " |");
            md.AppendLine("|---|" + string.Concat(cohorts.Skip(1).Select(_ => "---|")));
            foreach ((string name, _) in options)
            {
                Row[] careful = cells[Key(name, last, cohorts[0])];
                md.AppendLine($"| {name} | " + string.Join(" | ", cohorts.Skip(1).Select(c =>
                {
                    Row[] other = cells[Key(name, last, c)];
                    return string.Join(" / ", Enumerable.Range(0, heroes.Length)
                        .Select(h => (careful[h].Cleared - other[h].Cleared).ToString("+0;-0;0", CultureInfo.InvariantCulture)));
                })) + " |");
            }
            md.AppendLine();

            md.AppendLine("### The careful cohort: the Warden fight and the floor").AppendLine();
            md.AppendLine("| Configuration | Pilot | Cleared | Warden fight P50 (P90) | Floor P50 (cleared P50) | Slam hits a Warden fight | Echo hits a Warden fight | Deaths at the Warden |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach ((string name, _) in options)
                foreach ((string pilot, _) in pilots)
                {
                    Row[] c = cells[Key(name, pilot, cohorts[0])];
                    md.AppendLine($"| {name} | {pilot} | {Both(c, r => r.Cleared.ToString(CultureInfo.InvariantCulture))} | " +
                                  $"{Both(c, r => $"{r.WardenP50} ({r.WardenP90})")} | {Both(c, r => $"{r.FloorP50} ({r.ClearedFloorP50})")} | " +
                                  $"{Both(c, r => r.SlamHits)} | {Both(c, r => r.EchoHits)} | {Both(c, r => r.WardenDeaths.ToString(CultureInfo.InvariantCulture))} |");
                }
            md.AppendLine();

            md.AppendLine("<details><summary>Every cell</summary>").AppendLine();
            md.AppendLine("| Configuration | Pilot | Cohort | Hero | Cleared | Warden fight P50 (P90) | Floor P50 (cleared P50) | Slam / echo hits a Warden fight | Deaths at the Warden | Deaths: where / blow |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|");
            foreach ((string name, _) in options)
                foreach ((string pilot, _) in pilots)
                    foreach (AutopilotCohort cohort in cohorts)
                        foreach (Row r in cells[Key(name, pilot, cohort)])
                            md.AppendLine($"| {name} | {pilot} | {cohort.Name} | {r.Hero} | {r.Cleared} | {r.WardenP50} ({r.WardenP90}) | " +
                                          $"{r.FloorP50} ({r.ClearedFloorP50}) | {r.SlamHits} / {r.EchoHits} | {r.WardenDeaths} | {r.Deaths} |");
            md.AppendLine().AppendLine("</details>").AppendLine();
            return md.ToString();
        }

        private static IEnumerable<string> Words(string name, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrWhiteSpace(value))
                value = fallback;
            return value.Trim() == "none"
                ? Enumerable.Empty<string>()
                : value.Split(',').Select(w => w.Trim()).Where(w => w.Length > 0).ToArray();
        }

        private static IEnumerable<float> Floats(string name, string fallback) =>
            Words(name, fallback).Select(w => float.Parse(w, NumberStyles.Float, CultureInfo.InvariantCulture)).ToArray();

        private static string Number(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        // ---------------------------------------------------------------------------------------------------------
        // Batches and tables
        // ---------------------------------------------------------------------------------------------------------

        private static DescentSimulation.FoundryHero[] Heroes() =>
            new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

        private static IEnumerable<(string, DescentSimulation.FoundryHero)> Variants()
        {
            yield return ("Vanguard", DescentSimulation.Vanguard());
            DescentSimulation.FoundryHero v = DescentSimulation.Vanguard();
            v.Weapon = DescentSimulation.Staff();
            v.Name = "Vanguard + Staff";
            yield return (v.Name, v);
            v = DescentSimulation.Vanguard();
            v.EmberMemory = true;
            v.Name = "Vanguard + Ember Memory";
            yield return (v.Name, v);
            v = DescentSimulation.Vanguard();
            v.Ability = DescentSimulation.CrucibleVent();
            v.Name = "Vanguard + Crucible Vent";
            yield return (v.Name, v);
            yield return ("Ilya", DescentSimulation.CinderArcanist());
            DescentSimulation.FoundryHero i = DescentSimulation.CinderArcanist();
            i.Weapon = DescentSimulation.Sword();
            i.Name = "Ilya + Sword";
            yield return (i.Name, i);
            i = DescentSimulation.CinderArcanist();
            i.EmberMemory = false;
            i.Name = "Ilya without Ember Memory";
            yield return (i.Name, i);
            i = DescentSimulation.CinderArcanist();
            i.Ability = DescentSimulation.ForgeBurst();
            i.Name = "Ilya + Forge Burst";
            yield return (i.Name, i);
            i = DescentSimulation.CinderArcanist();
            i.Weapon = DescentSimulation.Sword();
            i.EmberMemory = false;
            i.Name = "Ilya + Sword, no Ember Memory";
            yield return (i.Name, i);
        }

        private static DescentSimulation.FoundryLevers Levers(AutopilotCohort cohort, PilotRealism realism,
            Action<DescentSimulation.FoundryLevers> structure, string name)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy($"{name}, {cohort.Name}");
            levers.Cohort = cohort;
            levers.Realism = realism;
            // The tables read the health lost by place and blow (FoundryResult.TakenBy), counted only when asked.
            levers.CountsHarmByPlace = true;
            structure?.Invoke(levers);
            return levers;
        }

        private FoundryBatch.Batch Run(DescentSimulation.FoundryHero hero, DescentSimulation.FoundryLevers levers) =>
            FoundryBatch.Run(hero, levers, _seeds, _parallel);

        // Rows of configurations, a column a cohort with the Vanguard's and Ilya's clears; then each configuration's floor,
        // Warden and deaths for every cohort.
        private string Matrix(List<(string Name, PilotRealism Realism, Action<DescentSimulation.FoundryLevers> Apply)> configs,
            IReadOnlyList<AutopilotCohort> cohorts, List<Row> rows)
        {
            var md = new StringBuilder();
            var detail = new StringBuilder();
            md.AppendLine($"Clears of seeds {_seeds[0]}-{_seeds[_seeds.Length - 1]}, Vanguard / Ilya, the same seeds in every cell.").AppendLine();
            md.AppendLine("| Configuration | " + string.Join(" | ", cohorts.Select(c => c.Name)) + " |");
            md.AppendLine("|---|" + string.Concat(cohorts.Select(_ => "---|")));
            detail.AppendLine("| Configuration | Cohort | Hero | Cleared | Floor P50 (cleared P50) | Warden fight P50 | Slam hits / hazard hits a run | Lapses a run | Deaths: where / blow |");
            detail.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach ((string name, PilotRealism realism, Action<DescentSimulation.FoundryLevers> apply) in configs)
            {
                var cells = new List<string>();
                foreach (AutopilotCohort cohort in cohorts)
                {
                    var clears = new List<int>();
                    foreach (DescentSimulation.FoundryHero hero in Heroes())
                    {
                        FoundryBatch.Batch b = Run(hero, Levers(cohort, realism, apply, name));
                        Row row = Summarise(name, cohort, b);
                        rows.Add(row);
                        clears.Add(b.ClearedCount);
                        detail.AppendLine($"| {name} | {cohort.Name} | {hero.Name} | {row.Cleared} | {row.FloorP50} ({row.ClearedFloorP50}) | {row.WardenP50} | " +
                                          $"{row.SlamHits} / {row.HazardHits} | {row.Lapses} | {row.Deaths} |");
                    }
                    cells.Add(string.Join(" / ", clears));
                }
                md.AppendLine($"| {name} | {string.Join(" | ", cells)} |");
            }
            md.AppendLine().AppendLine("<details><summary>Each cell's floor, Warden and deaths</summary>").AppendLine();
            md.Append(detail).AppendLine().AppendLine("</details>").AppendLine();
            return md.ToString();
        }

        private sealed class Row
        {
            public string Config;
            public string Cohort;
            public string Hero;
            public int Cleared;
            public int Runs;
            public string FloorP50;
            public string ClearedFloorP50;
            public string WardenP50;
            public string SlamHits;
            public string HazardHits;
            public string Lapses;
            public string Deaths;
            // The echo's second round's columns: the Warden fight's P90, hazard hits (the echo's, where nothing else
            // marks) a run that reached the Warden, and the deaths there.
            public string WardenP90;
            public string EchoHits;
            public int WardenDeaths;
        }

        private static Row Summarise(string config, AutopilotCohort cohort, FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] felled = b.Runs.Where(r => r.BossSeconds >= 0f).ToArray();
            DescentSimulation.FoundryResult[] gated = b.Runs.Where(r => r.LevelAtGate >= 0).ToArray();
            return new Row
            {
                Config = config, Cohort = cohort.Name, Hero = b.Hero.Name, Cleared = b.ClearedCount, Runs = b.Runs.Length,
                FloorP50 = P(b.Runs.Select(r => r.FloorSeconds), 50), ClearedFloorP50 = P(b.Cleared.Select(r => r.FloorSeconds), 50),
                WardenP50 = P(felled.Select(r => r.BossSeconds), 50),
                SlamHits = Mean(gated.Select(r => (float)r.SlamHits), "0.00"), HazardHits = Mean(b.Runs.Select(r => (float)r.HazardHits), "0.0"),
                Lapses = Mean(b.Runs.Select(r => (float)r.Lapses), "0"), Deaths = DeathsBy(b),
                WardenP90 = P(felled.Select(r => r.BossSeconds), 90), EchoHits = Mean(gated.Select(r => (float)r.HazardHits), "0.00"),
                WardenDeaths = b.Runs.Count(r => r.DiedAtWarden)
            };
        }

        private static float Lost(DescentSimulation.FoundryResult r) => r.TakenBy.Values.Sum();

        // "room 3 x72, boss x20 / pit 36, horde Grunt 33".
        private static string DeathsBy(FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] died = b.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            if (died.Length == 0)
                return "-";
            return $"{died.Length}: {Top(died.Select(Room), 4)} / {Top(died.Select(r => r.Killer ?? "?"), 4)}";
        }

        private static string NearDeaths(FoundryBatch.Batch b)
        {
            DescentSimulation.FoundryResult[] near = b.Runs.Where(r => r.Outcome != DescentSimulation.FoundryOutcome.Died && r.LowestHealthShare < .25f).ToArray();
            if (near.Length == 0)
                return "0";
            return $"{near.Length}: {Top(near.Select(r => r.LowestPhase ?? "?"), 3)} / {Top(near.Select(r => r.LowestHarm ?? "?"), 3)}";
        }

        private static string TakenBy(FoundryBatch.Batch b)
        {
            var sums = new Dictionary<string, float>();
            foreach (DescentSimulation.FoundryResult r in b.Runs)
                foreach (KeyValuePair<string, float> pair in r.TakenBy)
                {
                    sums.TryGetValue(pair.Key, out float s);
                    sums[pair.Key] = s + pair.Value;
                }
            float n = Math.Max(1, b.Runs.Length);
            string top = string.Join(", ", sums.OrderByDescending(p => p.Value).Take(5)
                .Select(p => $"{p.Key.Replace("|", " / ")} {(p.Value / n).ToString("0", CultureInfo.InvariantCulture)}"));
            return $"{(sums.Values.Sum() / n).ToString("0", CultureInfo.InvariantCulture)}; {top}";
        }

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Boss ? "boss"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Elite ? $"elite {run.DeathRoom}"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        private static string Top(IEnumerable<string> keys, int take) =>
            string.Join(", ", keys.GroupBy(k => k).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal)
                .Take(take).Select(g => $"{g.Key} x{g.Count()}"));

        private static string Describe(PilotRealism r) =>
            PilotRealism.Describe(r.MarkReaction, r.AimError, r.LapsesPerMinute, r.LapseSeconds, r.CardLapseSeconds);

        private static string Csv(IEnumerable<Row> rows)
        {
            var csv = new StringBuilder("config,cohort,hero,cleared,runs,floor_p50,cleared_floor_p50,warden_p50,slam_hits,hazard_hits,lapses,deaths," +
                                        "warden_p90,hazard_hits_a_fight,warden_deaths\n");
            foreach (Row r in rows)
                csv.AppendLine(string.Join(",", new[] { r.Config, r.Cohort, r.Hero, r.Cleared.ToString(CultureInfo.InvariantCulture),
                    r.Runs.ToString(CultureInfo.InvariantCulture), r.FloorP50, r.ClearedFloorP50, r.WardenP50, r.SlamHits, r.HazardHits,
                    r.Lapses, r.Deaths, r.WardenP90, r.EchoHits, r.WardenDeaths.ToString(CultureInfo.InvariantCulture) }.Select(Quote)));
            return csv.ToString();
        }

        private static string Quote(string field) =>
            field.IndexOfAny(new[] { ',', '"' }) < 0 ? field : "\"" + field.Replace("\"", "\"\"") + "\"";

        private static string P(IEnumerable<float> values, double p)
        {
            double value = FoundryBatch.Percentile(values, p);
            return double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);
        }

        private static string Mean(IEnumerable<float> values, string format = "0")
        {
            float[] list = values.ToArray();
            return list.Length == 0 ? "-" : list.Average().ToString(format, CultureInfo.InvariantCulture);
        }

        private static int Setting(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) ? value : fallback;

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }
    }
}
