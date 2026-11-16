using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/55's numbers (2026-11-17): the prototype duel (ArenaDuel.cs) and the Descent simulation read as a "race" mode.
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~ArenaDuelResearch"
    //
    // Environment, optional: ARENA_SEEDS (2000), ARENA_RACE_SEEDS (20). Writes TestResults/arena-duel.md.
    // ArenaDuelTests (not explicit) pins the replay's determinism on a small batch in every plain `dotnet test`.
    public sealed class ArenaDuelTests
    {
        [Test]
        public void ADuelReplaysBitForBitInSequenceAndInParallel()
        {
            DuelFighter a = ArenaDuelResearch.Gears[3].On(DuelFighter.Vanguard());
            DuelFighter b = DuelFighter.Ilya();
            ulong[] first = Enumerable.Range(1, 200).Select(seed => ArenaDuel.Fight(a, b, seed).Hash).ToArray();
            var second = new ulong[200];
            Parallel.For(0, 200, i => second[i] = ArenaDuel.Fight(a, b, i + 1).Hash);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Distinct().Count(), Is.GreaterThan(150), "seeds should give different fights");
        }

        [Test]
        public void AMirrorDuelIsEven()
        {
            DuelFighter v = DuelFighter.Vanguard();
            DuelResult[] fights = Enumerable.Range(1, 400).Select(seed => ArenaDuel.Fight(v, v, seed)).ToArray();
            int wins = fights.Count(r => r.Winner == 0);
            int losses = fights.Count(r => r.Winner == 1);
            Assert.That(wins, Is.InRange(140, 260));
            Assert.That(losses, Is.InRange(140, 260));
        }
    }

    [Explicit("docs/55's PvP research: run it by name.")]
    public sealed class ArenaDuelResearch
    {
        // docs/54 section 7.2's sets, per mille (crit in tenths of a point), and a small edge.
        internal static readonly DuelGear[] Gears =
        {
            DuelGear.None,
            new DuelGear("small edge (+2 % dmg/hp)", 20, 10, 5, 20, 0),
            new DuelGear("all four +5", 50, 25, 15, 50, 0),
            new DuelGear("all four +7", 70, 35, 21, 70, 0),
            new DuelGear("all four +9", 100, 50, 30, 100, 0),
            new DuelGear("+9 good efsun", 140, 50, 40, 140, 0),
            new DuelGear("ceiling", 140, 70, 40, 140, 40)
        };

        private static int Env(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) && value > 0 ? value : fallback;

        private static string P(double share) => (share * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        [Test]
        public void Report()
        {
            int seeds = Env("ARENA_SEEDS", 2000);
            int raceSeeds = Env("ARENA_RACE_SEEDS", 20);
            int[] all = Enumerable.Range(1, seeds).ToArray();
            var md = new StringBuilder();
            md.AppendLine("# Arena duel research (docs/55)");
            md.AppendLine();
            md.AppendLine($"Seeds 1-{seeds}; health x{ArenaDuel.HealthScale}; limit {ArenaDuel.LimitTicks / ArenaDuel.TicksPerSecond} s.");
            md.AppendLine();

            // 1. Determinism: sequential against parallel, and twice.
            DuelFighter va = DuelFighter.Vanguard();
            DuelFighter il = DuelFighter.Ilya();
            ulong[] once = all.Select(s => ArenaDuel.Fight(va, il, s).Hash).ToArray();
            var twice = new ulong[seeds];
            Parallel.For(0, seeds, i => twice[i] = ArenaDuel.Fight(va, il, all[i]).Hash);
            int same = once.Zip(twice, (x, y) => x == y ? 1 : 0).Sum();
            md.AppendLine("## 1. Replays");
            md.AppendLine();
            md.AppendLine($"Vanguard against Ilya, sequential then parallel: **{same} of {seeds}** fingerprints identical; {once.Distinct().Count()} distinct fights.");
            md.AppendLine();

            // 2. Cost, after a warm-up so the JIT is not timed.
            for (int i = 1; i <= 300; i++)
                ArenaDuel.BestOfThree(va, il, i, 2, out _);
            var watch = Stopwatch.StartNew();
            long ticks = 0, events = 0;
            foreach (int s in all)
            {
                DuelResult r = ArenaDuel.Fight(va, il, s);
                ticks += r.Ticks;
                events += r.Events;
            }
            watch.Stop();
            double perDuel = watch.Elapsed.TotalMilliseconds * 1000 / seeds;
            watch.Restart();
            foreach (int s in all)
                ArenaDuel.BestOfThree(va, il, s, 2, out _);
            watch.Stop();
            double perBo3 = watch.Elapsed.TotalMilliseconds * 1000 / seeds;
            md.AppendLine("## 2. Cost of a re-simulation (one core of this machine)");
            md.AppendLine();
            md.AppendLine($"A duel: **{perDuel.ToString("0.0", CultureInfo.InvariantCulture)} us**, {(ticks / (double)seeds / ArenaDuel.TicksPerSecond).ToString("0.0", CultureInfo.InvariantCulture)} s of fight, {(events / (double)seeds).ToString("0", CultureInfo.InvariantCulture)} events (the replay's log) on average. A best of three with two drawn skills: **{perBo3.ToString("0.0", CultureInfo.InvariantCulture)} us**.");
            md.AppendLine();

            // 3. Gear gap against outcome.
            md.AppendLine("## 3. How far gear decides a fight (A geared, B ungeared; A's wins, draws count half)");
            md.AppendLine();
            md.AppendLine("| A's gear | Gear power | Vanguard mirror: duel / Bo3 drawn | Ilya mirror: duel / Bo3 drawn | Vanguard (A) vs Ilya: duel / Bo3 drawn | Ilya (A) vs Vanguard: duel / Bo3 drawn |");
            md.AppendLine("|---|---|---|---|---|---|");
            (string, DuelFighter, DuelFighter)[] pairs =
            {
                ("vv", va, va), ("ii", il, il), ("vi", va, il), ("iv", il, va)
            };
            foreach (DuelGear gear in Gears)
            {
                var cells = pairs.Select(p =>
                {
                    DuelFighter a = gear.On(p.Item2);
                    double duel = all.AsParallel().Select(s => Score(ArenaDuel.Fight(a, p.Item3, s).Winner)).Sum() / seeds;
                    double bo3 = all.AsParallel().Select(s => Score(ArenaDuel.BestOfThree(a, p.Item3, s, 2, out _))).Sum() / seeds;
                    return $"{P(duel)} / {P(bo3)}";
                });
                md.AppendLine($"| {gear.Name} | +{(gear.Power * 100).ToString("0.0", CultureInfo.InvariantCulture)} % | {string.Join(" | ", cells)} |");
            }
            md.AppendLine();
            double[] seconds = all.Select(s => ArenaDuel.Fight(va, va, s).Ticks / (double)ArenaDuel.TicksPerSecond).OrderBy(x => x).ToArray();
            double[] secondsVi = all.Select(s => ArenaDuel.Fight(va, il, s).Ticks / (double)ArenaDuel.TicksPerSecond).OrderBy(x => x).ToArray();
            md.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "Fight length, ungeared: Vanguard mirror P10 / P50 / P90 {0:0.0} / {1:0.0} / {2:0.0} s; Vanguard against Ilya {3:0.0} / {4:0.0} / {5:0.0} s.",
                seconds[seeds / 10], seconds[seeds / 2], seconds[seeds * 9 / 10], secondsVi[seeds / 10], secondsVi[seeds / 2], secondsVi[seeds * 9 / 10]));
            md.AppendLine();

            // 4. A balance patch against stored results: the Sword +2 % for every Vanguard, read on a mirror and on the closest
            // cross-hero pair of section 3 (Ilya at +9 against an ungeared Vanguard).
            DuelFighter patched = va.Copy();
            patched.Damage = patched.Damage * 102 / 100;
            DuelFighter ilya9 = Gears[4].On(il);
            int flippedMirror = all.AsParallel().Count(s => ArenaDuel.Fight(va, va, s).Winner != ArenaDuel.Fight(patched, patched, s).Winner);
            int flippedCross = all.AsParallel().Count(s => ArenaDuel.Fight(ilya9, va, s).Winner != ArenaDuel.Fight(ilya9, patched, s).Winner);
            int rehashed = all.AsParallel().Count(s => ArenaDuel.Fight(ilya9, va, s).Hash != ArenaDuel.Fight(ilya9, patched, s).Hash);
            md.AppendLine("## 4. A 2 % Sword patch against stored duels");
            md.AppendLine();
            md.AppendLine($"Winner changed: Vanguard mirror (both patched) **{flippedMirror} of {seeds} ({P(flippedMirror / (double)seeds)})**; Ilya +9 against the Vanguard **{flippedCross} of {seeds} ({P(flippedCross / (double)seeds)})**; fingerprint changed {rehashed} of {seeds}.");
            md.AppendLine();

            // 5. The race: the shipped floor 1, the careful pilot with human hands, as a server would re-run a claimed clear.
            PilotRealism realism = PilotRealism.Named("typical");
            DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(AutopilotCohort.Careful, 1, realism);
            int[] race = Enumerable.Range(1, raceSeeds).ToArray();
            watch.Restart();
            string[] first = race.Select(s => Fingerprint(DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), s, levers))).ToArray();
            watch.Stop();
            double perRace = watch.Elapsed.TotalMilliseconds / raceSeeds;
            var again = new string[raceSeeds];
            Parallel.For(0, raceSeeds, i => again[i] = Fingerprint(DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), race[i], levers)));
            int raceSame = first.Zip(again, (x, y) => x == y ? 1 : 0).Sum();
            md.AppendLine("## 5. The race: floor 1 of the Descent re-simulated (careful Vanguard, typical hands)");
            md.AppendLine();
            md.AppendLine($"**{perRace.ToString("0", CultureInfo.InvariantCulture)} ms** a floor on one core; {raceSame} of {raceSeeds} identical when re-run in parallel. Sample: {first[0]}.");

            string root = RepositoryRoot();
            string output = Path.Combine(root, "TestResults");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, "arena-duel.md"), md.ToString());
            TestContext.Progress.WriteLine(md.ToString());
            Assert.That(same, Is.EqualTo(seeds));
            Assert.That(raceSame, Is.EqualTo(raceSeeds));
        }

        private static double Score(int winner) => winner == 0 ? 1 : winner == -1 ? 0.5 : 0;

        private static string Fingerprint(DescentSimulation.FoundryResult r) =>
            r == null ? "-" :
            string.Join(":", r.Outcome, BitConverter.SingleToInt32Bits(r.FloorSeconds).ToString("X8", CultureInfo.InvariantCulture),
                BitConverter.SingleToInt32Bits(r.BossSeconds).ToString("X8", CultureInfo.InvariantCulture), r.GateReadyKills,
                r.GateReadyLevel, Fingerprint(r.NextFloor));

        private static string RepositoryRoot()
        {
            string directory = TestContext.CurrentContext.TestDirectory;
            while (directory != null && !Directory.Exists(Path.Combine(directory, "Assets", "_Project")))
                directory = Path.GetDirectoryName(directory);
            return directory ?? TestContext.CurrentContext.TestDirectory;
        }
    }
}
