using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/57's six skills (2026-11-17) as numbers the research reads: the Vanguard's Forge Burst, Iron Resolve and Shield
    // Rush, Ilya's Crucible Vent, Flame Sigil and Cinder Ward (and Backdraft, the third considered), in ArenaDuel's integer
    // units. The numbers are the document's proposals; nothing in the game reads them.
    internal static class HeroSkillKits
    {
        // A rank (docs/57 section 3, later): +6 % of the skill's own number a rank above the first, rank 5 = +24 %.
        public static int Ranked(int value, int rank) => value * (1000 + 60 * (rank - 1)) / 1000;

        // The Vanguard's shipped ability (Ability_ForgeBurst: 20 within 2.5, every 8 s), as docs/55's fighter carries it.
        public static DuelSkill ForgeBurst(int rank = 1) =>
            new DuelSkill("Forge Burst", DuelSkillKind.Strike, Ranked(20_000, rank), 8, 400);

        // +60 armour for 4 s every 20 s: 37.5 % less damage taken on a hero with no armour (60 / (60 + 100)).
        public static DuelSkill IronResolve(int rank = 1) =>
            new DuelSkill("Iron Resolve", DuelSkillKind.Guard, Ranked(375, rank), 20) { DurationTicks = 240 };

        // 4 units along the stick in 0.3 s, 15 to each enemy within 0.8 of the path, a 0.5 s daze, every 12 s.
        public static DuelSkill ShieldRush(int rank = 1) =>
            new DuelSkill("Shield Rush", DuelSkillKind.Rush, Ranked(15_000, rank), 12, 4_000) { StunTicks = 30 };

        // Ilya's shipped ability (Ability_CrucibleVent: 8 a stack within 3, every 14 s), read against Ember Memory's burn.
        public static DuelSkill CrucibleVent(int rank = 1) =>
            new DuelSkill("Crucible Vent", DuelSkillKind.Vent, Ranked(8_000, rank), 14, 600);

        // A 2.5-unit glyph at Ilya's feet for 5 s, every 20 s: whatever stands in it walks 40 % slower, its burn deals double,
        // and it catches a stack every 0.5 s. Shaped by the duel: at 2.0 it never caught a foe at its weapon's reach (the
        // Sword's 2.1, the Staff's 2.4), and a glyph that only added stacks added nothing, as the Staff's strikes hold a foe
        // at the 5-stack cap already. The slow is the Descent's reason for it: burn is 2-3 % of Ilya's damage in a run.
        public static DuelSkill FlameSigil(int rank = 1, bool slow = true) =>
            new DuelSkill("Flame Sigil", DuelSkillKind.Sigil, Ranked(1000, rank), 20, 2_500)
            {
                DurationTicks = 300, SlowPermille = slow ? Ranked(400, rank) : 0
            };

        // A ward of 30 % of maximum health for 4 s, every 20 s; every blow it takes gives the striker 2 burn stacks.
        public static DuelSkill CinderWard(int rank = 1) =>
            new DuelSkill("Cinder Ward", DuelSkillKind.Ward, Ranked(300, rank), 20) { DurationTicks = 240, Stacks = 2 };

        // The third skill considered for Ilya, kept as a measured alternative: a 2.5-unit ring: 6 now, 2 stacks, the walk
        // slowed 50 % for 2.5 s; every 16 s.
        public static DuelSkill Backdraft(int rank = 1) =>
            new DuelSkill("Backdraft", DuelSkillKind.Repel, Ranked(6_000, rank), 16, 2_500)
            {
                Stacks = 2, SlowPermille = 500, DurationTicks = Ranked(150, rank)
            };

        // The Vanguard with its first `skills` skills (1 = the shipped kit, docs/55's fighter).
        public static DuelFighter Vanguard(int skills, int rank = 1, bool bold = false, int cooldownPermille = 0)
        {
            DuelFighter f = DuelFighter.Vanguard();
            f.Skills = new[] { ForgeBurst(rank), IronResolve(rank), ShieldRush(rank) }.Take(skills).ToArray();
            f.Bold = bold;
            f.CooldownPermille = cooldownPermille;
            return f;
        }

        // Ilya with Ember Memory's burn and its first `skills` skills. burn false and one skill is docs/55's fighter, whose
        // Vent is a plain strike of 8 and who never burns.
        public static DuelFighter Ilya(int skills, bool burn = true, int rank = 1, bool bold = false, int cooldownPermille = 0)
        {
            DuelFighter f = DuelFighter.Ilya();
            if (!burn)
            {
                f.Skills = f.Skills.Take(Math.Min(1, skills)).ToArray();
                return f;
            }
            f.BurnOnHit = true;
            f.Skills = new[] { CrucibleVent(rank), FlameSigil(rank), CinderWard(rank) }.Take(skills).ToArray();
            f.Bold = bold;
            f.CooldownPermille = cooldownPermille;
            return f;
        }

        // A copy with the weapon's damage (and with it the skills' scaled damage) at `permille` of its own: the arena
        // coefficient docs/55 section 3.2 says every hero needs.
        public static DuelFighter WithDamage(DuelFighter fighter, int permille)
        {
            DuelFighter f = fighter.Copy();
            f.Damage = fighter.Damage * permille / 1000;
            return f;
        }
    }

    // In every plain `dotnet test`, cheap: docs/57's kinds leave docs/55's fights as they were, and a fight with the six
    // skills replays bit for bit.
    public sealed class HeroSkillDuelTests
    {
        // XOR of the fingerprints of docs/55's Vanguard against docs/55's Ilya on seeds 1-50, and of a +7 Vanguard against an
        // ungeared one: the value docs/55's core (main b0fa98c2) gives, computed from that file before docs/57's kinds were
        // appended. docs/55's report also re-runs byte for byte but its clocks.
        private const ulong Docs55Fingerprint = 0x75A0A73194BF1127;

        internal static ulong Docs55Fights()
        {
            ulong x = 0;
            DuelFighter geared = ArenaDuelResearch.Gears[3].On(DuelFighter.Vanguard());
            for (int seed = 1; seed <= 50; seed++)
            {
                x ^= ArenaDuel.Fight(DuelFighter.Vanguard(), DuelFighter.Ilya(), seed).Hash;
                x ^= ArenaDuel.Fight(geared, DuelFighter.Vanguard(), seed).Hash * 31;
            }
            return x;
        }

        [Test]
        public void Docs55sFightsAreUntouchedByTheHeroSkills()
        {
            Assert.That(Docs55Fights(), Is.EqualTo(Docs55Fingerprint));
        }

        [Test]
        public void AFightWithTheSixSkillsReplaysBitForBit()
        {
            DuelFighter a = HeroSkillKits.Vanguard(3);
            DuelFighter b = HeroSkillKits.Ilya(3);
            ulong[] first = Enumerable.Range(1, 100).Select(seed => ArenaDuel.Fight(a, b, seed).Hash).ToArray();
            var second = new ulong[100];
            Parallel.For(0, 100, i => second[i] = ArenaDuel.Fight(a, b, i + 1).Hash);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Distinct().Count(), Is.GreaterThan(80), "seeds should give different fights");
        }
    }

    // docs/57's numbers on demand:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsResearch.TheSixSkillsInTheDuel"
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~HeroSkillsResearch.TheShippedDescentAsTheSkillsWouldMeetIt"
    //
    // The duel: ARENA_SEEDS (2000); writes TestResults/hero-skills-duel.md. The Descent: FOUNDRY_SEEDS (100),
    // FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors), FOUNDRY_REALISM (typical); writes
    // TestResults/hero-skills-descent.md. Neither changes the game or the simulation: the duel plays the proposals in
    // ArenaDuel's core; the Descent plays the shipped simulation and reads what the new skills would meet there (the
    // shipped abilities' yield, the crowd, the damage taken) and replays its hero's health with Iron Resolve laid over it.
    [Explicit("docs/57's hero skills research: run it by name.")]
    public sealed class HeroSkillsResearch
    {
        private static int Env(string name, int fallback) =>
            int.TryParse(Environment.GetEnvironmentVariable(name), out int value) && value > 0 ? value : fallback;

        private static string P(double share) => (share * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        private static string F(double value, string format = "0.0") => value.ToString(format, CultureInfo.InvariantCulture);

        private static double Score(int winner) => winner == 0 ? 1 : winner == -1 ? 0.5 : 0;

        // A's share of seeds won (draws half) in single duels, and in best-of-three with no dealt skill.
        private static (double duel, double bo3, double seconds) Play(DuelFighter a, DuelFighter b, int[] seeds)
        {
            DuelResult[] fights = seeds.AsParallel().AsOrdered().Select(s => ArenaDuel.Fight(a, b, s)).ToArray();
            double duel = fights.Sum(r => Score(r.Winner)) / seeds.Length;
            double bo3 = seeds.AsParallel().Select(s => Score(ArenaDuel.BestOfThree(a, b, s, 0, out _))).Sum() / seeds.Length;
            double[] length = fights.Select(r => r.Ticks / (double)ArenaDuel.TicksPerSecond).OrderBy(x => x).ToArray();
            return (duel, bo3, length[length.Length / 2]);
        }

        private static string Cell((double duel, double bo3, double seconds) r) => $"{P(r.duel)} / {P(r.bo3)}";

        [Test]
        public void TheSixSkillsInTheDuel()
        {
            int count = Env("ARENA_SEEDS", 2000);
            int[] seeds = Enumerable.Range(1, count).ToArray();
            var md = new StringBuilder();
            md.AppendLine("# The six skills in the duel (docs/57)");
            md.AppendLine();
            md.AppendLine($"ArenaDuel's integer core (docs/55), seeds 1-{count}, health x{ArenaDuel.HealthScale}. A's wins, draws half: a single " +
                          "duel / a best of three with no dealt skill. Both sides careful unless a row says bold.");
            md.AppendLine();
            md.AppendLine($"docs/55's fights untouched: fingerprint 0x{HeroSkillDuelTests.Docs55Fights():X16}.");
            md.AppendLine();

            // 1. Each hero's kit against itself, and the gap an unlocked skill opens.
            md.AppendLine("## 1. Mirrors and the unlock gap");
            md.AppendLine();
            md.AppendLine("| Hero | 1 v 1 (P50 s) | 2 v 2 | 3 v 3 (P50 s) | 2 v 1 | 3 v 1 | 3 v 2 |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (string hero in new[] { "Vanguard", "Ilya" })
            {
                Func<int, DuelFighter> kit = k => hero == "Vanguard" ? HeroSkillKits.Vanguard(k) : HeroSkillKits.Ilya(k);
                var m1 = Play(kit(1), kit(1), seeds);
                var m3 = Play(kit(3), kit(3), seeds);
                md.AppendLine($"| {hero} | {Cell(m1)} ({F(m1.seconds)}) | {Cell(Play(kit(2), kit(2), seeds))} | {Cell(m3)} ({F(m3.seconds)}) | " +
                              $"{Cell(Play(kit(2), kit(1), seeds))} | {Cell(Play(kit(3), kit(1), seeds))} | {Cell(Play(kit(3), kit(2), seeds))} |");
            }
            md.AppendLine();
            md.AppendLine("Ilya's rows carry Ember Memory's burn (BurnOnHit); docs/55's Ilya never burned.");
            md.AppendLine();

            // 2. Each new skill alone, beside the shipped one, against the shipped kit.
            md.AppendLine("## 2. Each new skill alone (A: shipped + the skill; B: shipped)");
            md.AppendLine();
            md.AppendLine("| Skill | A's wins: duel / Bo3 |");
            md.AppendLine("|---|---|");
            DuelFighter v1 = HeroSkillKits.Vanguard(1);
            DuelFighter i1 = HeroSkillKits.Ilya(1);
            foreach (DuelSkill skill in new[] { HeroSkillKits.IronResolve(), HeroSkillKits.ShieldRush() })
            {
                DuelFighter a = v1.Copy();
                a.Skills = new[] { HeroSkillKits.ForgeBurst(), skill };
                md.AppendLine($"| Vanguard + {skill.Name} | {Cell(Play(a, v1, seeds))} |");
            }
            foreach ((string label, DuelSkill skill) in new[]
                     {
                         ("Flame Sigil", HeroSkillKits.FlameSigil()), ("Flame Sigil without its slow", HeroSkillKits.FlameSigil(slow: false)),
                         ("Cinder Ward", HeroSkillKits.CinderWard()), ("Backdraft (the third considered)", HeroSkillKits.Backdraft())
                     })
            {
                DuelFighter a = i1.Copy();
                a.Skills = new[] { HeroSkillKits.CrucibleVent(), skill };
                md.AppendLine($"| Ilya + {label} | {Cell(Play(a, i1, seeds))} |");
            }
            DuelFighter withBackdraft = HeroSkillKits.Ilya(2);
            withBackdraft.Skills = new[] { HeroSkillKits.CrucibleVent(), HeroSkillKits.FlameSigil(), HeroSkillKits.Backdraft() };
            md.AppendLine($"| Ilya's three with Backdraft against its three with the Ward | {Cell(Play(withBackdraft, HeroSkillKits.Ilya(3), seeds))} |");
            md.AppendLine();

            // 3. The cross-hero pair, and the arena coefficient that evens it.
            md.AppendLine("## 3. The Vanguard (A) against Ilya (B)");
            md.AppendLine();
            md.AppendLine("| Kits | A's wins: duel / Bo3 | Fight P50 |");
            md.AppendLine("|---|---|---|");
            (string, DuelFighter, DuelFighter)[] cross =
            {
                ("docs/55's fighters (no burn, one skill each)", DuelFighter.Vanguard(), DuelFighter.Ilya()),
                ("one skill each, Ilya burning", HeroSkillKits.Vanguard(1), HeroSkillKits.Ilya(1)),
                ("two skills each", HeroSkillKits.Vanguard(2), HeroSkillKits.Ilya(2)),
                ("three skills each", HeroSkillKits.Vanguard(3), HeroSkillKits.Ilya(3)),
                ("three skills each, both bold", HeroSkillKits.Vanguard(3, bold: true), HeroSkillKits.Ilya(3, bold: true))
            };
            foreach ((string name, DuelFighter a, DuelFighter b) in cross)
            {
                var r = Play(a, b, seeds);
                md.AppendLine($"| {name} | {Cell(r)} | {F(r.seconds)} s |");
            }
            md.AppendLine();
            md.AppendLine("Ilya's weapon damage (and the skills scaled with it) at a share of its own, three skills each - the per-hero arena " +
                          "coefficient a round-robin would tune:");
            md.AppendLine();
            md.AppendLine("| Ilya's damage | Vanguard's wins: duel / Bo3 |");
            md.AppendLine("|---|---|");
            foreach (int permille in new[] { 600, 700, 725, 750, 775, 800, 900, 1000 })
                md.AppendLine($"| x{F(permille / 1000.0, "0.000")} | {Cell(Play(HeroSkillKits.Vanguard(3), HeroSkillKits.WithDamage(HeroSkillKits.Ilya(3), permille), seeds))} |");
            md.AppendLine();

            // 4. Policies.
            md.AppendLine("## 4. Careful (A) against bold (B), three skills, mirrors");
            md.AppendLine();
            md.AppendLine("| Hero | Careful's wins: duel / Bo3 |");
            md.AppendLine("|---|---|");
            md.AppendLine($"| Vanguard | {Cell(Play(HeroSkillKits.Vanguard(3), HeroSkillKits.Vanguard(3, bold: true), seeds))} |");
            md.AppendLine($"| Ilya | {Cell(Play(HeroSkillKits.Ilya(3), HeroSkillKits.Ilya(3, bold: true), seeds))} |");
            md.AppendLine();

            // 5. The cooldown line and ranks: what a utility line and a skill's rank decide in an auto-fight.
            md.AppendLine("## 5. The cooldown line and ranks (A against an equal B without them, three skills, mirrors)");
            md.AppendLine();
            md.AppendLine("| A carries | Vanguard mirror: duel / Bo3 | Ilya mirror: duel / Bo3 |");
            md.AppendLine("|---|---|---|");
            foreach (int line in new[] { 40, 100 })
                md.AppendLine($"| cooldown line -{line / 10} % | {Cell(Play(HeroSkillKits.Vanguard(3, cooldownPermille: line), HeroSkillKits.Vanguard(3), seeds))} | " +
                              $"{Cell(Play(HeroSkillKits.Ilya(3, cooldownPermille: line), HeroSkillKits.Ilya(3), seeds))} |");
            foreach (int rank in new[] { 3, 5 })
                md.AppendLine($"| every skill at rank {rank} (+{6 * (rank - 1)} %) | {Cell(Play(HeroSkillKits.Vanguard(3, rank), HeroSkillKits.Vanguard(3), seeds))} | " +
                              $"{Cell(Play(HeroSkillKits.Ilya(3, rank: rank), HeroSkillKits.Ilya(3), seeds))} |");
            md.AppendLine($"| rank 5 and the line at -10 % | {Cell(Play(HeroSkillKits.Vanguard(3, 5, cooldownPermille: 100), HeroSkillKits.Vanguard(3), seeds))} | " +
                          $"{Cell(Play(HeroSkillKits.Ilya(3, rank: 5, cooldownPermille: 100), HeroSkillKits.Ilya(3), seeds))} |");
            md.AppendLine();

            // 6. Does a fuller kit soften docs/55's gear curve?
            md.AppendLine("## 6. Gear with one skill and with three (A geared, B ungeared, mirrors)");
            md.AppendLine();
            md.AppendLine("| A's gear | Vanguard 1 skill / 3 skills (duel) | Ilya 1 skill / 3 skills (duel) |");
            md.AppendLine("|---|---|---|");
            foreach (DuelGear gear in ArenaDuelResearch.Gears.Skip(1).Take(2))
            {
                double v1g = Play(gear.On(HeroSkillKits.Vanguard(1)), HeroSkillKits.Vanguard(1), seeds).duel;
                double v3g = Play(gear.On(HeroSkillKits.Vanguard(3)), HeroSkillKits.Vanguard(3), seeds).duel;
                double i1g = Play(gear.On(HeroSkillKits.Ilya(1)), HeroSkillKits.Ilya(1), seeds).duel;
                double i3g = Play(gear.On(HeroSkillKits.Ilya(3)), HeroSkillKits.Ilya(3), seeds).duel;
                md.AppendLine($"| {gear.Name} | {P(v1g)} / {P(v3g)} | {P(i1g)} / {P(i3g)} |");
            }
            md.AppendLine();

            Write("hero-skills-duel.md", md.ToString());
        }

        // ---------------------------------------------------------------------------------------------------------------
        // The Descent: what the skills would meet in the shipped game.

        // A guard laid over a run's health as the simulation played it: the same blows land (the enemies' play does not
        // answer the guard - a first-order replay), heals fill as they did up to the maximum. Iron Resolve cuts every blow
        // inside it by Reduction; Cinder Ward (ward set) absorbs blows inside it up to its share of the maximum health. The
        // guard opens on a blow when it is ready and the policy says so; it lasts Seconds and is ready again Cooldown after
        // it opened.
        private sealed class GuardReplay
        {
            public const float Seconds = 4f;
            public const float Cooldown = 20f;
            public const float Reduction = .375f;
            public const float WardShare = .3f;

            private readonly bool _reactive;
            private readonly bool _ward;
            private float _wardLeft;
            public float Prevented;
            private float _max;
            private float _previous = float.NaN;
            private float _health = float.NaN;
            private float _openUntil = float.NegativeInfinity;
            private float _readyAt;
            private int _floor = 1;
            private float _lastTime;

            public float Taken;
            public float Covered;
            public int Uses;
            public bool Died;
            public float DeathTime = -1f;
            public float[] TakenByFloor = new float[5];
            public float WardenTaken;
            private bool _atWarden;
            private readonly Queue<(float time, float blow)> _recent = new Queue<(float, float)>();
            private float _recentSum;

            // At a death: the replayed health left over the last 10 s's rate of damage - the seconds the guard would have
            // bought at the killing moment, at that moment's pace (0 when the replay dies too, NaN without a death).
            public float Reprieve => !Died ? float.NaN : _health <= 0f ? 0f : _health / Math.Max(1e-3f, _recentSum / 10f);

            // reactive: the careful policy, a blow with the health under 70 %; otherwise the first blow it is ready for.
            public GuardReplay(float maximum, bool reactive, bool ward = false)
            {
                _max = maximum;
                _reactive = reactive;
                _ward = ward;
            }

            public float HealthLeft => _health;

            public void Line(string line)
            {
                int space = line.IndexOf(' ');
                if (space < 0)
                    return;
                ReadOnlySpan<char> rest = line.AsSpan(space + 1);
                if (rest.StartsWith("frame "))
                {
                    float time = float.Parse(line.AsSpan(0, space), NumberStyles.Float, CultureInfo.InvariantCulture);
                    Frame(time, Value(rest, " hp="));
                }
                else if (rest.StartsWith("upgrade_selected "))
                {
                    int slash = rest.LastIndexOf('/');
                    if (slash > 0)
                        _max = float.Parse(rest.Slice(slash + 1), NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                else if (rest.StartsWith("floor_descend "))
                {
                    _floor = (int)Value(rest, "floor=");
                    _atWarden = false;
                }
                else if (rest.StartsWith("boss_start "))
                    _atWarden = true;
                else if (rest.StartsWith("room_complete ") && _atWarden)
                    _atWarden = false;
                else if (rest.StartsWith("run_end defeat"))
                {
                    // The killing blow lands after the frame's line: the health the hero still showed is the blow.
                    float time = float.Parse(line.AsSpan(0, space), NumberStyles.Float, CultureInfo.InvariantCulture);
                    if (!float.IsNaN(_previous) && _previous > 0f)
                        Frame(time, 0f);
                    Died = true;
                    DeathTime = time;
                }
            }

            private static float Value(ReadOnlySpan<char> line, string key)
            {
                int at = line.IndexOf(key.AsSpan());
                if (at < 0)
                    return float.NaN;
                ReadOnlySpan<char> v = line.Slice(at + key.Length);
                int end = v.IndexOf(' ');
                if (end >= 0)
                    v = v.Slice(0, end);
                return float.Parse(v, NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private void Frame(float time, float hp)
            {
                _lastTime = time;
                if (float.IsNaN(_previous))
                {
                    _previous = hp;
                    _health = hp;
                    return;
                }
                float delta = hp - _previous;
                _previous = hp;
                if (delta > 0f)
                {
                    _health = Math.Min(_max, _health + delta);
                    return;
                }
                if (delta == 0f)
                    return;
                float blow = -delta;
                Taken += blow;
                _recent.Enqueue((time, blow));
                _recentSum += blow;
                while (_recent.Count > 0 && _recent.Peek().time < time - 10f)
                    _recentSum -= _recent.Dequeue().blow;
                if (_floor >= 1 && _floor < TakenByFloor.Length)
                    TakenByFloor[_floor] += blow;
                if (_atWarden)
                    WardenTaken += blow;
                float cut = 0f;
                if (time < _openUntil)
                {
                    Covered += blow;
                    if (_ward)
                    {
                        cut = Math.Min(_wardLeft, blow);
                        _wardLeft -= cut;
                    }
                    else
                        cut = blow * Reduction;
                }
                Prevented += cut;
                _health -= blow - cut;
                if (time >= _readyAt && (!_reactive || _health < _max * .7f))
                {
                    _openUntil = time + Seconds;
                    _readyAt = time + Cooldown;
                    _wardLeft = WardShare * _max;
                    Uses++;
                }
            }
        }

        private sealed class RunRead
        {
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryResult Result;
            public GuardReplay Careful;
            public GuardReplay Clumsy;
            public GuardReplay WardCareful;
            public GuardReplay WardClumsy;
            public bool Completed;
            public int FloorsCleared;
            public int DiedOn;
            public string DeathPhase;
            public float Minutes;
        }

        [Test]
        public void TheShippedDescentAsTheSkillsWouldMeetIt()
        {
            int count = Env("FOUNDRY_SEEDS", 100);
            int first = Env("FOUNDRY_FIRST_SEED", 1);
            int parallel = Env("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            int[] seeds = FoundryBatch.Seeds(first, count);
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };
            var cohorts = new[] { AutopilotCohort.Careful, AutopilotCohort.Clumsy, AutopilotCohort.AutoFarmer };
            var runs = new List<RunRead>();
            foreach (AutopilotCohort cohort in cohorts)
            foreach (DescentSimulation.FoundryHero hero in heroes)
            {
                DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                    .Copy($"cohort {cohort.Name}, hero skills, {hands}");
                var read = new RunRead[seeds.Length];
                Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) }, i =>
                {
                    var careful = new GuardReplay(hero.MaximumHealth, true);
                    var clumsy = new GuardReplay(hero.MaximumHealth, false);
                    var wardCareful = new GuardReplay(hero.MaximumHealth, true, ward: true);
                    var wardClumsy = new GuardReplay(hero.MaximumHealth, false, ward: true);
                    // HERO_SKILLS_TRACE=<seed>: that seed's trace lines (frames apart) for every cohort and hero, to read a
                    // replay against the run it replays.
                    StringBuilder dump = seeds[i] == Env("HERO_SKILLS_TRACE", 0) ? new StringBuilder() : null;
                    DescentSimulation.FoundryResult result = DescentSimulation.RunFoundry(hero, seeds[i], levers,
                        DescentSimulation.GloamChain(levers), line =>
                        {
                            careful.Line(line);
                            clumsy.Line(line);
                            wardCareful.Line(line);
                            wardClumsy.Line(line);
                            if (dump != null && line.IndexOf(" frame ", StringComparison.Ordinal) < 0)
                                dump.AppendLine(line);
                        });
                    if (dump != null)
                        Write($"hero-skills-trace-{cohort.Name}-{hero.Id}-{seeds[i]}.txt", dump.ToString(), quiet: true);
                    read[i] = Read(cohort.Name, hero.Name, result, careful, clumsy);
                    read[i].WardCareful = wardCareful;
                    read[i].WardClumsy = wardClumsy;
                });
                runs.AddRange(read);
                TestContext.Progress.WriteLine($"{cohort.Name} {hero.Name}: done");
            }
            Write("hero-skills-descent.md", Report(runs, first, count, hands));
            Assert.That(runs.Count, Is.EqualTo(cohorts.Length * heroes.Length * count));
        }

        private static RunRead Read(string cohort, string hero, DescentSimulation.FoundryResult result, GuardReplay careful,
            GuardReplay clumsy)
        {
            var run = new RunRead { Cohort = cohort, Hero = hero, Result = result, Careful = careful, Clumsy = clumsy };
            for (DescentSimulation.FoundryResult floor = result; floor != null; floor = floor.NextFloor)
            {
                run.Minutes += floor.FloorSeconds / 60f;
                if (floor.Outcome == DescentSimulation.FoundryOutcome.Cleared)
                {
                    run.FloorsCleared++;
                    if (FoundryBatch.IsFinale(floor))
                        run.Completed = true;
                }
                else if (floor.Outcome == DescentSimulation.FoundryOutcome.Died)
                {
                    run.DiedOn = floor.Floor;
                    run.DeathPhase = floor.DeathPhase;
                }
            }
            return run;
        }

        private static IEnumerable<DescentSimulation.FoundryResult> Floors(DescentSimulation.FoundryResult r)
        {
            for (DescentSimulation.FoundryResult f = r; f != null; f = f.NextFloor)
                yield return f;
        }

        private static double Median(IEnumerable<double> values)
        {
            double[] v = values.Where(x => !double.IsNaN(x)).OrderBy(x => x).ToArray();
            return v.Length == 0 ? double.NaN : v[v.Length / 2];
        }

        private static string Report(List<RunRead> runs, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine("# The shipped Descent as docs/57's skills would meet it");
            md.AppendLine();
            md.AppendLine($"Seeds {first}-{first + count - 1}, the whole Descent (floors 1-3 and the finale), {hands} hands, the shipped kits " +
                          "(one ability each). Nothing here plays a new skill: it reads what one would meet.");
            md.AppendLine();

            md.AppendLine("## 1. Who finishes, and where the rest die");
            md.AppendLine();
            md.AppendLine("| Cohort | Hero | Cleared floor 2 / floor 3 (the skills' gates) | Completed | Died on floor 1 / 2 / 3 / finale | Deaths at the boss / in a hall's waves / elsewhere | Minutes P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => (r.Cohort, r.Hero)))
            {
                RunRead[] a = g.ToArray();
                string died = string.Join(" / ", Enumerable.Range(1, 4).Select(f => a.Count(r => r.DiedOn == f).ToString(CultureInfo.InvariantCulture)));
                bool InWaves(RunRead r) => r.DeathPhase != null && (r.DeathPhase.StartsWith("wave", StringComparison.Ordinal) || r.DeathPhase == "between waves");
                int boss = a.Count(r => r.DeathPhase == "boss");
                int wave = a.Count(InWaves);
                int other = a.Count(r => r.DeathPhase != null && r.DeathPhase != "boss" && !InWaves(r));
                md.AppendLine($"| {g.Key.Cohort} | {g.Key.Hero} | {a.Count(r => r.FloorsCleared >= 2)} / {a.Count(r => r.FloorsCleared >= 3)} of {a.Length} | " +
                              $"{a.Count(r => r.Completed)} | {died} | {boss} / {wave} / {other} | {F(Median(a.Select(r => (double)r.Minutes)))} |");
            }
            md.AppendLine();

            md.AppendLine("## 2. The floors: the shipped ability's yield, the crowd, the hurt (P50 over the runs that played the floor)");
            md.AppendLine();
            md.AppendLine("| Cohort | Hero | Floor | Floor s | Boss s | Ability uses a minute | Ability share of the hero's damage | Burn share | " +
                          "Damage an ability use | Mean enemies standing | Hero's damage a second | Damage taken a minute | Taken at the boss |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => (r.Cohort, r.Hero)))
            {
                for (int floorNumber = 1; floorNumber <= 3; floorNumber++)
                {
                    DescentSimulation.FoundryResult[] floors = g.SelectMany(r => Floors(r.Result)).Where(f => f.Floor == floorNumber).ToArray();
                    if (floors.Length == 0)
                        continue;
                    double Dealt(DescentSimulation.FoundryResult f) => f.DealtByWeapons + f.DealtByBurn + f.DealtByAbility + f.DealtByBleed;
                    double alive = Median(floors.Select(f => f.Buckets == 0 ? double.NaN :
                        f.BucketAlive.Take(f.Buckets).Sum() / Math.Max(1e-3, f.FloorSeconds)));
                    md.AppendLine($"| {g.Key.Cohort} | {g.Key.Hero} | {floorNumber} | {F(Median(floors.Select(f => (double)f.FloorSeconds)), "0")} | " +
                                  $"{F(Median(floors.Where(f => f.WardenSeconds >= 0f).Select(f => (double)f.WardenSeconds)), "0")} | " +
                                  $"{F(Median(floors.Select(f => f.AbilityUses * 60.0 / Math.Max(1f, f.FloorSeconds))))} | " +
                                  $"{P(Median(floors.Select(f => f.DealtByAbility / Math.Max(1e-3, Dealt(f)))))} | " +
                                  $"{P(Median(floors.Select(f => f.DealtByBurn / Math.Max(1e-3, Dealt(f)))))} | " +
                                  $"{F(Median(floors.Where(f => f.AbilityUses > 0).Select(f => (double)f.DealtByAbility / f.AbilityUses)))} | " +
                                  $"{F(alive)} | {F(Median(floors.Select(f => Dealt(f) / Math.Max(1f, f.FloorSeconds))))} | " +
                                  $"{F(Median(floors.Select(f => (f.TakenFromWave + f.TakenFromHorde + f.TakenFromWarden + f.TakenFromSlams + f.TakenFromPit + f.TakenFromHazards) * 60.0 / Math.Max(1f, f.FloorSeconds))))} | " +
                                  $"{F(Median(floors.Where(f => f.WardenSeconds >= 0f).Select(f => (double)(f.TakenFromWarden + f.TakenFromSlams + f.FightTakenFromHorde + f.FightTakenFromPit + f.FightTakenFromEscort))))} |");
                }
            }
            md.AppendLine();
            md.AppendLine("Damage an ability use: the Forge Burst deals 20 to each enemy in reach, so a use's damage over 20 is a floor on the enemies it " +
                          "caught (overkill lowers it); the Vent's is its stacks x 8 on each burning enemy and its 1.2-unit neighbours.");
            md.AppendLine();

            md.AppendLine($"## 3. A guard laid over the hero's health: Iron Resolve ({F(GuardReplay.Reduction * 100)} % less for {F(GuardReplay.Seconds, "0")} s) " +
                          $"and Cinder Ward (absorbs {F(GuardReplay.WardShare * 100, "0")} % of the maximum for {F(GuardReplay.Seconds, "0")} s), every {F(GuardReplay.Cooldown, "0")} s");
            md.AppendLine();
            md.AppendLine("A first-order replay: the same blows, cut inside the guard; the enemies do not answer it. Careful: a blow taken under 70 % " +
                          "of the maximum opens it; on cooldown: the first blow it is ready for (the clumsy cohort's rule). Every cohort and hero is " +
                          "replayed with both guards and both rules, the auto-farmer included (it uses no skill: its row is what a guard would be " +
                          "worth to it if it did). A replay with no feedback outlives nearly every death by the damage it took off earlier, so the " +
                          "death columns read the **seconds it buys at the killing moment**: the replayed health left over the last 10 s's rate of damage.");
            md.AppendLine();
            md.AppendLine("| Cohort | Hero | Guard, rule | Uses a run | Damage taken inside it | Damage prevented | EHP gained | Deaths | Seconds bought at a death P50 | Deaths given 5 s or more | Of those at a boss |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (var g in runs.GroupBy(r => (r.Cohort, r.Hero)))
            {
                foreach ((string policy, Func<RunRead, GuardReplay> pick) in new (string, Func<RunRead, GuardReplay>)[]
                         {
                             ("Iron Resolve, careful", r => r.Careful), ("Iron Resolve, on cooldown", r => r.Clumsy),
                             ("Cinder Ward, careful", r => r.WardCareful), ("Cinder Ward, on cooldown", r => r.WardClumsy)
                         })
                {
                    GuardReplay[] replays = g.Select(pick).ToArray();
                    double taken = replays.Sum(x => (double)x.Taken);
                    double covered = replays.Sum(x => (double)x.Covered);
                    double prevented = replays.Sum(x => (double)x.Prevented);
                    double share = taken > 0 ? covered / taken : 0;
                    double cut = taken > 0 ? prevented / taken : 0;
                    double ehp = 1 / (1 - cut) - 1;
                    RunRead[] died = g.Where(r => r.DiedOn > 0).ToArray();
                    double bought = Median(died.Select(r => (double)pick(r).Reprieve));
                    int given = died.Count(r => pick(r).Reprieve >= 5f);
                    int givenBoss = died.Count(r => pick(r).Reprieve >= 5f && r.DeathPhase == "boss");
                    md.AppendLine($"| {g.Key.Cohort} | {g.Key.Hero} | {policy} | {F(replays.Average(x => x.Uses))} | {P(share)} | {P(cut)} | +{P(ehp)} | " +
                                  $"{died.Length} | {(died.Length == 0 ? "-" : F(bought))} | {given} | {givenBoss} |");
                }
            }
            md.AppendLine();
            return md.ToString();
        }

        private static void Write(string name, string text, bool quiet = false)
        {
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            File.WriteAllText(Path.Combine(output, name), text);
            if (!quiet)
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
