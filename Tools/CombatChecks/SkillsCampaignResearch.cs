using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/57 section 15: the skills' easing read again with the unlock as built (13.8, decision 1; 14.7's next read) and
    // the difficulty tiers live (docs/54 section 21, its lead decision 3: standing and the auto-farmer read beside it). A
    // per-pilot campaign: one cohort playing one hero on one profile, run after run on fresh seeds, with the profile's
    // real progression between the runs -
    //   - the run carries the skills the profile has forged for the hero (FoundryLevers.Skills 1, 2 or 3), on the tier it
    //     plays, wearing the gear it has (four items at their basma levels, main stats only, as gear_economy.py models);
    //   - the run's floors cleared open skills 2 and 3 for the profile (HeroSkillUnlock: floor 2's and floor 3's first
    //     clears), and its completion completes its tier (ProfileTiers' unlock chain);
    //   - the run banks its gold as the scene pays it (GearEconomyResearch.Read, with the tier's reward) and its floor
    //     materials and boosters as gear_economy.py's run_loot draws them;
    //   - at the Forge the pilot forges skill 2 for 300 and then skill 3 for 500 as soon as the purse allows, saving for an
    //     open skill before anything else (no Anvil while one waits, and all of the run's gold kept); with no skill
    //     waiting, a quarter of the run's gold goes to efsun (not modelled) and the rest to the Anvil, docs/54's habit:
    //     the weapon to +9, then the armour, the amulet, the ring, boosters as section 5 uses them (AnvilTable's odds,
    //     pity and ceilings);
    //   - it moves up one tier when docs/54 section 21's matched rule holds (gear_economy.py's "matched" policy, docs/58
    //     4a): the profile walked out of the Descent on its tier, and its gear power reached the next tier's matched set
    //     (DifficultyTiers.MatchedGearPower).
    // Beside each run that carries skill 2 or 3, the shadow: the same seed, gear and tier with skill 1 alone - what the
    // run would have been without the skills, at the same point of the same profile. The auto-farmer presses no skill, so
    // its shadow is its run (not played twice).
    //
    // Campaign k's run r plays seed FOUNDRY_FIRST_SEED + k x CAMPAIGN_RUNS + (r - 1), the same for every cohort and both
    // heroes; the block spent is FOUNDRY_FIRST_SEED .. + CAMPAIGN_COUNT x CAMPAIGN_RUNS - 1. Each campaign's loot and Anvil
    // dice are its own streams (cohort, hero, k), shared by every option, so options differ by their levers and what
    // follows from them.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~SkillsCampaignResearch"
    //
    // Environment, all optional: CAMPAIGN_COUNT (campaigns a cohort and hero, 10), CAMPAIGN_RUNS (runs a campaign, 100),
    // CAMPAIGN_OPTIONS (live; never - no skill ever forged, the gold all to the Anvil; the candidates below),
    // CAMPAIGN_COHORTS (clumsy, careful, first-card, standing, auto-farmer), CAMPAIGN_HEROES (Vanguard,Ilya),
    // CAMPAIGN_SHADOW (1; 0 plays no shadow), CAMPAIGN_RESUME (1 keeps the finished campaigns of the output's CSV and plays
    // the rest; with every campaign there it only writes the report), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the
    // processors), FOUNDRY_REALISM (typical), FOUNDRY_OUT (the repository's TestResults). Writes skills-campaign.csv (a row
    // a run, appended as each campaign ends and rewritten in order at the end) and skills-campaign.md.
    [Explicit("docs/57 section 15, the skills over a profile's campaign: run it by name.")]
    public sealed class SkillsCampaignResearch
    {
        private const float BandFrom = 45f;
        private const float BandTo = 65f;
        // docs/57 13.8, decision 1: the clumsy cohort's pooled completion with all three skills.
        private const double Ceiling = 60.0;
        // docs/58 4a's habit: a quarter of the gold to efsun once nothing else waits.
        private const int EfsunShareDivisor = 4;
        private const int BootstrapResamples = 10000;

        private sealed class Option
        {
            public string Key;
            public string What;
            // False: no skill is ever forged (the game before phases 3-4), the gold all to the Anvil.
            public bool Forges = true;
            // A candidate's numbers for skills 2 and 3 (FoundryLevers.TuneSkill), null for the assets'.
            public Action<DescentSimulation.HeroAbility> Tune;
            // Research: the most skills a run carries whatever was forged (2: skill 3 forged and paid for, never carried).
            public int MostSkills = 3;
        }

        private static Action<DescentSimulation.HeroAbility> Ward(float size, float window) => skill =>
        {
            if (skill.Kind != AbilityKind.Ward)
                return;
            skill.Amount = size;
            skill.Duration = window;
        };

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "live", What = "the skills as built: opened by the floors' first clears, forged at 300 and 500 gold, the shipped numbers" };
            yield return new Option { Key = "never", What = "no skill forged (the game before phases 3-4): the same campaign, the gold all to the Anvil", Forges = false };
            // The ward trim (13.8, decision 1: its size first, then its window), research on the spent seeds.
            yield return new Option { Key = "ward15", What = "as live, Cinder Ward absorbing 15 % of the maximum (shipped 20 %) for 2 s", Tune = Ward(.15f, 2f) };
            yield return new Option { Key = "ward10", What = "as live, Cinder Ward absorbing 10 % of the maximum for 2 s", Tune = Ward(.10f, 2f) };
            yield return new Option { Key = "ward20w15", What = "as live, Cinder Ward 20 % for 1.5 s (shipped 2 s)", Tune = Ward(.20f, 1.5f) };
            yield return new Option { Key = "ward15w15", What = "as live, Cinder Ward 15 % for 1.5 s", Tune = Ward(.15f, 1.5f) };
            yield return new Option { Key = "ward10w15", What = "as live, Cinder Ward 10 % for 1.5 s", Tune = Ward(.10f, 1.5f) };
            yield return new Option { Key = "ward10w1", What = "as live, Cinder Ward 10 % for 1 s", Tune = Ward(.10f, 1f) };
            yield return new Option { Key = "ward20w1", What = "as live, Cinder Ward 20 % for 1 s", Tune = Ward(.20f, 1f) };
            // A probe, not a candidate: what skill 3 is worth in the campaign (forged, never carried).
            yield return new Option { Key = "no-third", What = "as live, skill 3 forged but never carried (a probe of its worth)", MostSkills = 2 };
            yield return new Option
            {
                Key = "sigil30", What = "as live, Flame Sigil slowing 30 % (shipped 40 %)",
                Tune = skill =>
                {
                    if (skill.Kind == AbilityKind.Sigil)
                        skill.Amount = .3f;
                }
            };
            yield return new Option
            {
                Key = "sigil20", What = "as live, Flame Sigil slowing 20 %",
                Tune = skill =>
                {
                    if (skill.Kind == AbilityKind.Sigil)
                        skill.Amount = .2f;
                }
            };
        }

        // One profile between runs: what the Forge, the Anvil and the tier row read.
        private sealed class Profile
        {
            public int Deepest;
            public bool SkillTwo;
            public bool SkillThree;
            public int Gold;
            public readonly int[] Materials = new int[3];
            public int Seals;
            public int Hammers;
            public readonly int[] Levels = new int[4];
            public readonly int[] Pity = new int[4];
            public DifficultyTier Tier = DifficultyTier.Forged;
            public readonly bool[] Completed = new bool[DifficultyTiers.Count];

            public int Skills => 1 + (SkillTwo ? 1 : 0) + (SkillThree ? 1 : 0);

            // An open skill not yet forged (skill 3 only once skill 2 is): the purse saves for it.
            public bool Waiting =>
                (HeroSkillUnlock.IsOpen(2, Deepest) && !SkillTwo) ||
                (HeroSkillUnlock.IsOpen(3, Deepest) && SkillTwo && !SkillThree);
        }

        // One run of a campaign, as the CSV keeps it and the report reads it.
        private sealed class Row
        {
            public string Option;
            public string Cohort;
            public string Hero;
            public int Campaign;
            public int Run;
            public int Seed;
            public int Skills;
            public int Tier;
            public int Power;
            public int[] Levels = new int[4];
            public int GoldBefore;
            public int Banked;
            public int GoldAfter;
            public bool Completed;
            public int FloorsCleared;
            public int DiedOn;
            public string DeathPhase;
            public float Boss1 = -1f;
            public float Boss2 = -1f;
            public float Boss3 = -1f;
            public float Seconds;
            // The same run with skill 1 alone: -1 where not played (the run carries skill 1 alone, or the cohort presses none).
            public int Shadow = -1;
            public int ShadowFloors = -1;
            public float ShadowBoss1 = -1f;
            public float ShadowBoss2 = -1f;
            public float ShadowBoss3 = -1f;
            public float ShadowSeconds = -1f;

            public bool HasShadow => Shadow >= 0;
        }

        // A campaign's own dice (SplitMix64 on an FNV-1a hash of its tag): the same on every runtime and in every order.
        private sealed class Dice
        {
            private ulong _state;

            public Dice(string tag)
            {
                ulong hash = 14695981039346656037UL;
                foreach (char c in tag)
                {
                    hash ^= c;
                    hash *= 1099511628211UL;
                }
                _state = hash;
            }

            public double Next()
            {
                _state += 0x9E3779B97F4A7C15UL;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (z >> 11) * (1.0 / (1UL << 53));
            }
        }

        [Test]
        public void TheSkillsOverACampaign()
        {
            int count = Setting("CAMPAIGN_COUNT", 10);
            int runs = Setting("CAMPAIGN_RUNS", 100);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            bool shadows = Setting("CAMPAIGN_SHADOW", 1) != 0;
            bool resume = Setting("CAMPAIGN_RESUME", 0) != 0;
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";

            string[] keys = List("CAMPAIGN_OPTIONS", "live");
            Option[] options = keys.Select(k => Options().FirstOrDefault(o => o.Key == k)).ToArray();
            Assert.That(options, Has.None.Null, "every option named exists: " + string.Join(", ", keys));
            string[] cohortNames = List("CAMPAIGN_COHORTS", "clumsy,careful,first-card,standing,auto-farmer");
            AutopilotCohort[] cohorts = cohortNames.Select(n => AutopilotCohort.Cohorts.FirstOrDefault(c => c.Name == n)).ToArray();
            Assert.That(cohorts, Has.None.Null, "every cohort named exists: " + string.Join(", ", cohortNames));
            string[] heroNames = List("CAMPAIGN_HEROES", "Vanguard,Ilya");
            Func<DescentSimulation.FoundryHero>[] heroes = new Func<DescentSimulation.FoundryHero>[] { DescentSimulation.Vanguard, DescentSimulation.CinderArcanist }
                .Where(make => heroNames.Contains(make().Name)).ToArray();
            Assert.That(heroes.Length, Is.EqualTo(heroNames.Length), "every hero named exists");

            string csvPath = Path.Combine(output, "skills-campaign.csv");
            var rows = new List<Row>();
            var done = new HashSet<(string, string, string, int)>();
            if (resume && File.Exists(csvPath))
            {
                foreach (IGrouping<(string, string, string, int), Row> campaign in ReadCsv(csvPath)
                             .GroupBy(r => (r.Option, r.Cohort, r.Hero, r.Campaign)))
                {
                    Row[] kept = campaign.OrderBy(r => r.Run).ToArray();
                    bool whole = kept.Length == runs && kept.Select((r, i) => r.Run == i + 1 && r.Seed == SeedOf(first, runs, r.Campaign, r.Run)).All(ok => ok);
                    if (!whole || !keys.Contains(campaign.Key.Item1) || !cohortNames.Contains(campaign.Key.Item2) || campaign.Key.Item4 >= count)
                        continue;
                    rows.AddRange(kept);
                    done.Add(campaign.Key);
                }
                TestContext.Progress.WriteLine($"resumed {done.Count} campaigns from {csvPath}");
            }
            File.WriteAllText(csvPath, Csv(rows, hands, true));

            Dictionary<string, int> gold = GearEconomyResearch.BaseGold();
            float[] modifiers = GearEconomyResearch.Modifiers(DescentSimulation.FoundryFloorOne());
            var unknown = new HashSet<string>();
            // The levers a run plays: the cohort's whole Descent with its hands, the skills it carries, the option's numbers.
            var levers = new Dictionary<(string, string, int), DescentSimulation.FoundryLevers>();
            foreach (Option option in options)
            foreach (AutopilotCohort cohort in cohorts)
            for (int skills = 1; skills <= 3; skills++)
            {
                DescentSimulation.FoundryLevers made = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                    .Copy($"cohort {cohort.Name}, campaign {option.Key}, skills {skills}, {hands}");
                made.Skills = skills;
                if (option.Tune != null && skills > 1)
                    made.TuneSkill = option.Tune;
                levers[(option.Key, cohort.Name, skills)] = made;
            }

            // The work in the order asked (the cohorts' order first), each campaign whole on one thread.
            var work = new List<(Option Option, AutopilotCohort Cohort, Func<DescentSimulation.FoundryHero> Hero, int Campaign)>();
            foreach (AutopilotCohort cohort in cohorts)
            foreach (Option option in options)
            for (int k = 0; k < count; k++)
            foreach (Func<DescentSimulation.FoundryHero> hero in heroes)
                if (!done.Contains((option.Key, cohort.Name, hero().Name, k)))
                    work.Add((option, cohort, hero, k));

            var watch = System.Diagnostics.Stopwatch.StartNew();
            object sink = new object();
            int finished = 0;
            Parallel.ForEach(Partitioner.Create(work, EnumerablePartitionerOptions.NoBuffering),
                new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) }, w =>
                {
                    List<Row> campaign = Campaign(w.Option, w.Cohort, w.Hero, w.Campaign, first, runs, shadows, levers, modifiers, gold, unknown);
                    lock (sink)
                    {
                        rows.AddRange(campaign);
                        File.AppendAllText(csvPath, Csv(campaign, hands, false));
                        finished++;
                        TestContext.Progress.WriteLine($"{w.Option.Key} {w.Cohort.Name} {campaign[0].Hero} #{w.Campaign}: " +
                                                       $"{campaign.Count(r => r.Completed)} of {runs} completed ({finished} of {work.Count}, {watch.Elapsed.TotalMinutes:0.0} min)");
                    }
                });

            rows = rows.OrderBy(r => Array.IndexOf(keys, r.Option)).ThenBy(r => Array.IndexOf(cohortNames, r.Cohort))
                .ThenBy(r => Array.IndexOf(heroNames, r.Hero)).ThenBy(r => r.Campaign).ThenBy(r => r.Run).ToList();
            File.WriteAllText(csvPath, Csv(rows, hands, true));
            string report = Report(rows, options, cohortNames, heroNames, first, count, runs, hands, unknown);
            File.WriteAllText(Path.Combine(output, "skills-campaign.md"), report);
            TestContext.WriteLine(report);
            Assert.That(rows.Count, Is.EqualTo(options.Length * cohorts.Length * heroes.Length * count * runs), "every campaign ran whole");
        }

        private static int SeedOf(int first, int runs, int campaign, int run) => first + campaign * runs + (run - 1);

        // ---------------------------------------------------------------------------------------------------------------
        // One campaign
        // ---------------------------------------------------------------------------------------------------------------

        private static List<Row> Campaign(Option option, AutopilotCohort cohort, Func<DescentSimulation.FoundryHero> makeHero, int k,
            int first, int runs, bool shadows, Dictionary<(string, string, int), DescentSimulation.FoundryLevers> levers,
            float[] modifiers, Dictionary<string, int> gold, HashSet<string> unknown)
        {
            var profile = new Profile();
            DescentSimulation.FoundryHero template = makeHero();
            string heroName = template.Name;
            string weaponId = template.Weapon.Id;
            string tag = $"{cohort.Name}|{heroName}|{k.ToString(CultureInfo.InvariantCulture)}";
            var loot = new Dice(tag + "|loot");
            var anvil = new Dice(tag + "|anvil");
            bool presses = cohort != AutopilotCohort.AutoFarmer;
            var rows = new List<Row>(runs);
            for (int run = 1; run <= runs; run++)
            {
                int seed = SeedOf(first, runs, k, run);
                int skills = option.Forges ? Math.Min(profile.Skills, option.MostSkills) : 1;
                DescentSimulation.FoundryHero hero = Dress(makeHero(), profile);
                DescentSimulation.FoundryLevers played = levers[(option.Key, cohort.Name, skills)];
                DescentSimulation.FoundryResult result = DescentSimulation.RunFoundry(hero, seed, played, DescentSimulation.GloamChain(played));
                GearEconomyResearch.Run income = GearEconomyResearch.Read(cohort.Name, hero.Name, seed, result, modifiers, gold, unknown, profile.Tier);
                var row = new Row
                {
                    Option = option.Key, Cohort = cohort.Name, Hero = hero.Name, Campaign = k, Run = run, Seed = seed, Skills = skills,
                    Tier = (int)profile.Tier, Power = hero.Gear.Power, Levels = (int[])profile.Levels.Clone(), GoldBefore = profile.Gold,
                    Banked = income.Banked, Completed = income.Completed, FloorsCleared = income.FloorsCleared
                };
                Outcome(result, out row.DiedOn, out row.DeathPhase, out row.Boss1, out row.Boss2, out row.Boss3, out row.Seconds);
                if (skills > 1 && shadows && presses)
                {
                    DescentSimulation.FoundryHero bare = Dress(makeHero(), profile);
                    DescentSimulation.FoundryLevers one = levers[(option.Key, cohort.Name, 1)];
                    DescentSimulation.FoundryResult shadow = DescentSimulation.RunFoundry(bare, seed, one, DescentSimulation.GloamChain(one));
                    GearEconomyResearch.Run shadowIncome = GearEconomyResearch.Read(cohort.Name, bare.Name, seed, shadow, modifiers, gold, unknown, profile.Tier);
                    row.Shadow = shadowIncome.Completed ? 1 : 0;
                    row.ShadowFloors = shadowIncome.FloorsCleared;
                    Outcome(shadow, out _, out _, out row.ShadowBoss1, out row.ShadowBoss2, out row.ShadowBoss3, out row.ShadowSeconds);
                }
                else if (skills > 1 && shadows)
                {
                    // The auto-farmer presses no skill: its run is its shadow.
                    row.Shadow = row.Completed ? 1 : 0;
                    row.ShadowFloors = row.FloorsCleared;
                    row.ShadowBoss1 = row.Boss1;
                    row.ShadowBoss2 = row.Boss2;
                    row.ShadowBoss3 = row.Boss3;
                    row.ShadowSeconds = row.Seconds;
                }

                // The profile after the run: its clears open skills, its walk-out completes its tier, its loot is banked.
                profile.Deepest = Math.Max(profile.Deepest, income.FloorsCleared);
                if (income.Completed)
                    profile.Completed[(int)profile.Tier] = true;
                bool saving = option.Forges && profile.Waiting;
                profile.Gold += saving ? income.Banked : income.Banked - income.Banked / EfsunShareDivisor;
                Loot(profile, income, profile.Tier, loot);
                // The Forge: the skills first, as soon as the purse allows; the Anvil only while no skill waits.
                if (option.Forges)
                {
                    if (HeroSkillUnlock.IsOpen(2, profile.Deepest) && !profile.SkillTwo && profile.Gold >= HeroSkillUnlock.SkillTwoPrice)
                    {
                        profile.Gold -= HeroSkillUnlock.SkillTwoPrice;
                        profile.SkillTwo = true;
                    }
                    if (HeroSkillUnlock.IsOpen(3, profile.Deepest) && profile.SkillTwo && !profile.SkillThree && profile.Gold >= HeroSkillUnlock.SkillThreePrice)
                    {
                        profile.Gold -= HeroSkillUnlock.SkillThreePrice;
                        profile.SkillThree = true;
                    }
                }
                if (!(option.Forges && profile.Waiting))
                    Spend(profile, anvil);
                // The tier row: up one when the tier was walked out of and the gear reaches the next tier's matched set.
                if (profile.Tier < DifficultyTiers.Highest && profile.Completed[(int)profile.Tier] &&
                    Gear(weaponId, profile.Levels).Power >= DifficultyTiers.MatchedGearPower(profile.Tier + 1))
                    profile.Tier++;
                row.GoldAfter = profile.Gold;
                rows.Add(row);
            }
            return rows;
        }

        private static DescentSimulation.FoundryHero Dress(DescentSimulation.FoundryHero hero, Profile profile)
        {
            hero.Gear = Gear(hero.Weapon.Id, profile.Levels);
            hero.Tier = profile.Tier;
            return hero;
        }

        // The four items at their levels, main stats only (gear_economy.py's model rolls no efsun): the hero's own weapon and
        // FoundryGear's armour, amulet and ring. An item at +0 adds nothing and is left off, so an ungeared profile plays
        // GearLoadout.None, the shipped run to the bit.
        private static GearLoadout Gear(string weaponId, int[] levels)
        {
            var pieces = new List<ItemInstance>(4);
            if (levels[0] > 0)
                pieces.Add(new ItemInstance(0, weaponId, GearSlot.Weapon, UpgradeRarity.Rare, levels[0], 0, null));
            if (levels[1] > 0)
                pieces.Add(new ItemInstance(1, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Gold, levels[1], 0, null));
            if (levels[2] > 0)
                pieces.Add(new ItemInstance(2, "item_ember_locket", GearSlot.Amulet, UpgradeRarity.Gold, levels[2], 0, null));
            if (levels[3] > 0)
                pieces.Add(new ItemInstance(3, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Gold, levels[3], 0, null));
            return pieces.Count == 0 ? GearLoadout.None : new GearLoadout(pieces);
        }

        private static void Outcome(DescentSimulation.FoundryResult result, out int diedOn, out string phase, out float boss1,
            out float boss2, out float boss3, out float seconds)
        {
            diedOn = 0;
            phase = null;
            boss1 = boss2 = boss3 = -1f;
            seconds = 0f;
            int index = 0;
            for (DescentSimulation.FoundryResult floor = result; floor != null && index < 4; floor = floor.NextFloor, index++)
            {
                seconds += floor.FloorSeconds;
                if (floor.Outcome == DescentSimulation.FoundryOutcome.Died)
                {
                    diedOn = index + 1;
                    phase = floor.DeathPhase;
                }
                if (FoundryBatch.IsFinale(floor))
                    continue;
                if (index == 0)
                    boss1 = floor.WardenSeconds;
                else if (index == 1)
                    boss2 = floor.WardenSeconds;
                else if (index == 2)
                    boss3 = floor.WardenSeconds;
            }
            // Kept as the CSV keeps them (hundredths), so a report written from the CSV (CAMPAIGN_RESUME) is this one.
            boss1 = Hundredths(boss1);
            boss2 = Hundredths(boss2);
            boss3 = Hundredths(boss3);
            seconds = Hundredths(seconds);
        }

        private static float Hundredths(float value) =>
            value < 0f ? -1f : float.Parse(value.ToString("0.00", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

        // gear_economy.py's run_loot, the materials and boosters the Anvil reads (items, scrolls and runes are not modelled):
        // a floor cleared drops its two materials, each elite its one, the floor died on keeps half; the boosters' chances at
        // the elites and bosses; on a tier the amounts and chances times its reward, a fraction made whole by a draw.
        private static void Loot(Profile profile, GearEconomyResearch.Run income, DifficultyTier tier, Dice dice)
        {
            double reward = DifficultyTiers.RewardPercent(tier) / 100.0;
            int cleared = income.FloorsCleared;
            int diedOn = income.Completed ? 0 : cleared + 1;
            for (int floor = 1; floor <= 3; floor++)
            {
                if (floor > cleared && floor != diedOn)
                    continue;
                double keep = floor <= cleared ? 1.0 : 0.5;
                int material = floor - 1;
                if (floor <= cleared)
                    profile.Materials[material] += Whole(2 * reward, dice);
                int elites = income.FloorElites[floor - 1];
                int bosses = income.FloorBosses[floor - 1];
                int dropped = 0;
                for (int e = 0; e < elites; e++)
                    dropped += Whole(reward, dice);
                profile.Materials[material] += (int)(dropped * keep);
                for (int e = 0; e < elites; e++)
                {
                    if (dice.Next() < Math.Min(1.0, .08 * reward) * keep)
                        profile.Seals++;
                }
                for (int b = 0; b < bosses; b++)
                {
                    if (dice.Next() < Math.Min(1.0, .04 * reward) * keep)
                        profile.Seals++;
                    if (dice.Next() < Math.Min(1.0, .04 * reward) * keep)
                        profile.Hammers++;
                }
            }
        }

        private static int Whole(double amount, Dice dice)
        {
            int whole = (int)Math.Floor(amount);
            if (amount == whole)
                return whole;
            return whole + (dice.Next() < amount - whole ? 1 : 0);
        }

        // docs/54's habit at the Anvil (gear_economy.py's spend): strike the first item below +9 while the purse and the
        // materials allow - the Hammer on the +6 strike, a Seal on +9 and on +8 while three or more are kept.
        private static void Spend(Profile profile, Dice dice)
        {
            while (true)
            {
                int item = Array.FindIndex(profile.Levels, level => level < AnvilTable.MaxLevel);
                if (item < 0 || !Strike(profile, item, dice))
                    return;
            }
        }

        private static bool Strike(Profile profile, int item, Dice dice)
        {
            int target = profile.Levels[item] + 1;
            int cost = AnvilTable.GoldCost(target);
            bool needs = AnvilTable.TryMaterialCost(target, out MaterialKind kind, out int amount);
            if (profile.Gold < cost || (needs && profile.Materials[(int)kind] < amount))
                return false;
            profile.Gold -= cost;
            if (needs)
                profile.Materials[(int)kind] -= amount;
            int failures = profile.Pity[item];
            int ceiling = AnvilTable.CeilingFailures(target);
            int chance;
            if (ceiling > 0 && failures >= ceiling)
                chance = 100;
            else
            {
                chance = AnvilTable.BaseChancePercent(target) + AnvilTable.PityStepPercent(target) * failures;
                if (target == AnvilTable.HammerMaxLevel && chance < 100 && profile.Hammers >= 1)
                {
                    profile.Hammers--;
                    chance = 100;
                }
                else if (chance < 100)
                {
                    int seals = 0;
                    if (target == AnvilTable.MaxLevel)
                        seals = Math.Min(AnvilTable.SealsPerStrike, profile.Seals);
                    else if (target == AnvilTable.MaxLevel - 1 && profile.Seals >= 3)
                        seals = 1;
                    profile.Seals -= seals;
                    chance += AnvilTable.SealBonusPercent * seals;
                }
            }
            if (dice.Next() * 100 < chance)
            {
                profile.Levels[item]++;
                profile.Pity[item] = 0;
            }
            else
                profile.Pity[item]++;
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------------
        // The report
        // ---------------------------------------------------------------------------------------------------------------

        private static readonly (int From, int To)[] BandsOf100 =
            { (1, 1), (2, 3), (4, 5), (6, 10), (11, 20), (21, 40), (41, 60), (61, 80), (81, 100) };

        private static (int From, int To)[] Bands(int runs)
        {
            var bands = BandsOf100.Where(b => b.From <= runs).Select(b => (b.From, Math.Min(b.To, runs))).ToList();
            if (runs > 100)
                bands.Add((101, runs));
            return bands.ToArray();
        }

        private static string Report(List<Row> rows, Option[] options, string[] cohorts, string[] heroes, int first, int count, int runs,
            string hands, HashSet<string> unknown)
        {
            var md = new StringBuilder();
            int last = first + count * runs - 1;
            md.AppendLine($"# docs/57 section 15: the skills over a profile's campaign - {count} campaigns of {runs} runs a cohort and hero, seeds {first}-{last} ({hands})").AppendLine();
            md.AppendLine("A campaign is one cohort playing one hero on a fresh profile, run after run on fresh seeds: the run carries the skills the profile " +
                          "has forged (floor 2's and floor 3's first clears open skills 2 and 3; the Forge forges them at 300 and 500 gold as soon as " +
                          "the purse allows, saving for them first), on the tier the matched rule has it on (walked out of its tier and gear power at " +
                          "the next tier's matched set), wearing its gear (four items at their basma levels, main stats only; the Anvil works the " +
                          "weapon to +9 first, then the armour, amulet and ring, with a quarter of the gold to efsun once no skill waits). " +
                          "Completed: walked out of the finale. Shadow: the same seed, gear and tier with skill 1 alone - the run without the " +
                          "skills at the same point of the same profile (the auto-farmer presses no skill, so its run is its shadow). CIs: 90 %, " +
                          $"{BootstrapResamples.ToString("N0", CultureInfo.InvariantCulture)} resamples of the campaigns, both heroes of a campaign index together (they play the same seeds).").AppendLine();

            foreach (Option option in options)
            {
                Row[] mine = rows.Where(r => r.Option == option.Key).ToArray();
                if (mine.Length == 0)
                    continue;
                md.AppendLine($"## `{option.Key}`: {option.What}").AppendLine();
                Arrival(md, mine, cohorts, heroes, runs);
                ByState(md, mine, cohorts, heroes);
                TheCeiling(md, mine, runs);
                Curve(md, mine, cohorts, runs);
                Bosses(md, mine, cohorts, heroes);
            }

            if (options.Length > 1)
            {
                md.AppendLine("## The options side by side (all runs of the campaigns; both heroes)").AppendLine();
                md.AppendLine("| Cohort | " + string.Join(" | ", options.Select(o => $"`{o.Key}` completed")) + " | " +
                              string.Join(" | ", options.Select(o => $"`{o.Key}` all three: completed (runs)")) + " |");
                md.AppendLine("|---|" + string.Join("|", Enumerable.Repeat("---", options.Length * 2)) + "|");
                foreach (string cohort in cohorts)
                {
                    var cells = new List<string>();
                    foreach (Option option in options)
                    {
                        Row[] mine = rows.Where(r => r.Option == option.Key && r.Cohort == cohort).ToArray();
                        cells.Add(mine.Length == 0 ? "-" : Pct(mine.Count(r => r.Completed), mine.Length));
                    }
                    foreach (Option option in options)
                    {
                        Row[] three = rows.Where(r => r.Option == option.Key && r.Cohort == cohort && r.Skills == 3).ToArray();
                        cells.Add(three.Length == 0 ? "-" : $"{Pct(three.Count(r => r.Completed), three.Length)} ({three.Length})");
                    }
                    md.AppendLine($"| {cohort} | {string.Join(" | ", cells)} |");
                }
                md.AppendLine();
            }

            md.AppendLine("## The options").AppendLine();
            foreach (Option option in options)
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            md.AppendLine();
            if (unknown.Count > 0)
                md.AppendLine("Kinds without a base gold (counted as 0): " + string.Join(", ", unknown.OrderBy(s => s)) + ".").AppendLine();
            return md.ToString();
        }

        // When skills 2 and 3 arrive (the first run that carries each) and when the tiers do.
        private static void Arrival(StringBuilder md, Row[] rows, string[] cohorts, string[] heroes, int runs)
        {
            md.AppendLine("### When the skills and the tiers arrive").AppendLine();
            md.AppendLine($"The run index of the first run that carries skill 2, skill 3, and the first run on Tempered and on Inferno: P25 / P50 / P75 over the " +
                          $"campaigns (\">{runs}\" where a quarter or more of them had none within {runs} runs), and the share of campaigns that had it. " +
                          "Floor 2 first cleared: the run index of the run that opened skill 2.").AppendLine();
            md.AppendLine("| Cohort | Hero | Floor 2 first cleared | Skill 2 | Skill 3 | Tempered | Inferno |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            foreach (string cohort in cohorts)
            foreach (string hero in heroes.Concat(new[] { (string)null }))
            {
                Row[][] campaigns = Campaigns(rows, cohort, hero);
                if (campaigns.Length == 0)
                    continue;
                string Cell(Func<Row, bool> reached)
                {
                    int[] at = campaigns.Select(c =>
                    {
                        Row hit = c.FirstOrDefault(reached);
                        return hit == null ? runs + 1 : hit.Run;
                    }).OrderBy(v => v).ToArray();
                    int had = at.Count(v => v <= runs);
                    return $"{Q(at, .25, runs)} / {Q(at, .5, runs)} / {Q(at, .75, runs)} ({Pct(had, at.Length)})";
                }
                md.AppendLine($"| {cohort} | {hero ?? "both"} | {Cell(r => r.FloorsCleared >= 2)} | {Cell(r => r.Skills >= 2)} | " +
                              $"{Cell(r => r.Skills >= 3)} | {Cell(r => r.Tier >= (int)DifficultyTier.Tempered)} | " +
                              $"{Cell(r => r.Tier >= (int)DifficultyTier.Inferno)} |");
            }
            md.AppendLine();
        }

        // Completion before and after each skill arrives: the runs by the skills they carried, and their shadows.
        private static void ByState(StringBuilder md, Row[] rows, string[] cohorts, string[] heroes)
        {
            md.AppendLine("### Completion by the skills carried, and the same runs without them").AppendLine();
            md.AppendLine("Runs grouped by the skills they carried (skill 1 alone: before skill 2 arrived; skills 1-2: before skill 3; all three: after). " +
                          "With: the campaign's runs. Without: their shadows (the same seed, gear and tier, skill 1 alone). Change: with less without, " +
                          "in points, with its 90 % CI over the campaigns. A run before skill 2 is its own shadow.").AppendLine();
            md.AppendLine("| Cohort | Hero | Skill 1 alone: runs, completed | Skills 1-2: runs, with, without, change | All three: runs, with, without, change (90 % CI) |");
            md.AppendLine("|---|---|---|---|---|");
            foreach (string cohort in cohorts)
            foreach (string hero in heroes.Concat(new[] { (string)null }))
            {
                Row[] mine = rows.Where(r => r.Cohort == cohort && (hero == null || r.Hero == hero)).ToArray();
                if (mine.Length == 0)
                    continue;
                Row[] one = mine.Where(r => r.Skills == 1).ToArray();
                string State(int skills, bool ci)
                {
                    Row[] s = mine.Where(r => r.Skills == skills).ToArray();
                    if (s.Length == 0)
                        return "0";
                    Row[] paired = s.Where(r => r.HasShadow).ToArray();
                    if (paired.Length == 0)
                        return $"{s.Length}, {Pct(s.Count(r => r.Completed), s.Length)}, -, -";
                    double change = 100.0 * paired.Sum(r => (r.Completed ? 1 : 0) - r.Shadow) / paired.Length;
                    string interval = ci ? " (" + Interval(Change(mine, skills)) + ")" : string.Empty;
                    return $"{s.Length}, {Pct(s.Count(r => r.Completed), s.Length)}, {Pct(paired.Sum(r => r.Shadow), paired.Length)}, " +
                           $"{Signed(change)}{interval}";
                }
                md.AppendLine($"| {cohort} | {hero ?? "both"} | {one.Length}, {(one.Length == 0 ? "-" : Pct(one.Count(r => r.Completed), one.Length))} | " +
                              $"{State(2, false)} | {State(3, true)} |");
            }
            md.AppendLine();
        }

        // The bootstrap's statistic for the change with these skills: per campaign index, the paired runs' (with - without)
        // and their count, both heroes together.
        private static (double Point, double Low, double High) Change(Row[] rows, int skills)
        {
            var byCampaign = rows.Where(r => r.Skills == skills && r.HasShadow).GroupBy(r => r.Campaign)
                .Select(g => (Sum: (double)g.Sum(r => (r.Completed ? 1 : 0) - r.Shadow), N: (double)g.Count())).ToArray();
            return Bootstrap(byCampaign);
        }

        private static (double Point, double Low, double High) Rate(IEnumerable<Row> rows, Func<Row, bool> hit)
        {
            var byCampaign = rows.GroupBy(r => r.Campaign)
                .Select(g => (Sum: (double)g.Count(hit), N: (double)g.Count())).ToArray();
            return Bootstrap(byCampaign);
        }

        // Points (100 x sum / n) and the 90 % CI from resampling the campaigns; a fixed generator, so the table repeats.
        private static (double Point, double Low, double High) Bootstrap((double Sum, double N)[] campaigns)
        {
            if (campaigns.Length == 0 || campaigns.Sum(c => c.N) <= 0)
                return (double.NaN, double.NaN, double.NaN);
            double point = 100.0 * campaigns.Sum(c => c.Sum) / campaigns.Sum(c => c.N);
            var random = new Random(57);
            var boots = new double[BootstrapResamples];
            for (int b = 0; b < boots.Length; b++)
            {
                double sum = 0, n = 0;
                for (int i = 0; i < campaigns.Length; i++)
                {
                    (double s, double c) = campaigns[random.Next(campaigns.Length)];
                    sum += s;
                    n += c;
                }
                boots[b] = n > 0 ? 100.0 * sum / n : double.NaN;
            }
            Array.Sort(boots);
            return (point, boots[(int)(.05 * boots.Length)], boots[(int)(.95 * boots.Length) - 1]);
        }

        // docs/57 13.8's ceiling: the clumsy cohort's pooled completion with all three skills.
        private static void TheCeiling(StringBuilder md, Row[] rows, int runs)
        {
            Row[] clumsy = rows.Where(r => r.Cohort == "clumsy").ToArray();
            if (clumsy.Length == 0)
                return;
            md.AppendLine($"### The ceiling: the clumsy cohort's pooled completion with all three skills (13.8: {Ceiling:0} %)").AppendLine();
            md.AppendLine("Every clumsy run that carried all three skills, both heroes pooled, then split by hero, by the run's index in its campaign and by " +
                          "its tier; beside it the same runs without the skills (their shadows) and the change. 90 % CIs over the campaigns.").AppendLine();
            md.AppendLine("| Runs | Hero | Runs played | Completed with all three (90 % CI) | Without them (shadow) | Change, points (90 % CI) | Against 60 % |");
            md.AppendLine("|---|---|---|---|---|---|---|");
            void Line(string label, string hero, Row[] set)
            {
                if (set.Length == 0)
                    return;
                (double p, double lo, double hi) = Rate(set, r => r.Completed);
                Row[] paired = set.Where(r => r.HasShadow).ToArray();
                string without = paired.Length == 0 ? "-" : Pct(paired.Sum(r => r.Shadow), paired.Length);
                var change = Bootstrap(paired.GroupBy(r => r.Campaign)
                    .Select(g => (Sum: (double)g.Sum(r => (r.Completed ? 1 : 0) - r.Shadow), N: (double)g.Count())).ToArray());
                string verdict = double.IsNaN(p) ? "-" : p > Ceiling ? "**above**" : hi > Ceiling ? "below; the CI reaches it" : "below";
                md.AppendLine($"| {label} | {hero} | {set.Length} | **{F(p, "0.0")} %** ({F(lo, "0.0")} to {F(hi, "0.0")}) | {without} | " +
                              $"{Signed(change.Point)} ({Interval(change)}) | {verdict} |");
            }
            Row[] three = clumsy.Where(r => r.Skills == 3).ToArray();
            Line($"all, runs 1-{runs}", "both", three);
            foreach (string hero in three.Select(r => r.Hero).Distinct())
                Line($"all, runs 1-{runs}", hero, three.Where(r => r.Hero == hero).ToArray());
            foreach ((int from, int to) in Bands(runs))
                Line($"runs {from}-{to}", "both", three.Where(r => r.Run >= from && r.Run <= to).ToArray());
            if (runs > 60)
                Line($"runs 61-{runs}", "both", three.Where(r => r.Run > 60).ToArray());
            if (runs > 60)
                foreach (string hero in three.Select(r => r.Hero).Distinct())
                    Line($"runs 61-{runs}", hero, three.Where(r => r.Run > 60 && r.Hero == hero).ToArray());
            // By tier, each hero too: the heroes reach Tempered at different runs, so the pooled rows mix their shares.
            for (int tier = 0; tier < DifficultyTiers.Count; tier++)
            {
                Line($"on {DifficultyTiers.Name((DifficultyTier)tier)}", "both", three.Where(r => r.Tier == tier).ToArray());
                foreach (string hero in three.Select(r => r.Hero).Distinct())
                    Line($"on {DifficultyTiers.Name((DifficultyTier)tier)}", hero, three.Where(r => r.Tier == tier && r.Hero == hero).ToArray());
            }
            md.AppendLine("The pooled rows weigh the heroes by the runs each played there: Ilya reaches Tempered sooner than the Vanguard, so a pooled " +
                          "tier row leans on Ilya's rate where the hero rows do not.").AppendLine();
        }

        // The campaign run by run: completion, what the profile carries, its tier, gear and income.
        private static void Curve(StringBuilder md, Row[] rows, string[] cohorts, int runs)
        {
            md.AppendLine("### The campaign by run index (both heroes)").AppendLine();
            md.AppendLine("Completed: the band's runs; without: their shadows' completion where the run carried skill 2 or 3, else the run's own. " +
                          "Skills 2 / 3: the share of runs carrying them. Tempered+ / Inferno: the share on those tiers. Power: the gear's index, mean. " +
                          "Banked: forge gold a run, mean (the tier's reward included).").AppendLine();
            md.AppendLine("| Cohort | Runs | Completed | Without the skills | Skill 2 / 3 | Tempered+ / Inferno | Power | Banked |");
            md.AppendLine("|---|---|---|---|---|---|---|---|");
            foreach (string cohort in cohorts)
            foreach ((int from, int to) in Bands(runs))
            {
                Row[] band = rows.Where(r => r.Cohort == cohort && r.Run >= from && r.Run <= to).ToArray();
                if (band.Length == 0)
                    continue;
                int without = band.Sum(r => r.HasShadow ? r.Shadow : (r.Completed ? 1 : 0));
                md.AppendLine($"| {cohort} | {from}-{to} | {Pct(band.Count(r => r.Completed), band.Length)} | {Pct(without, band.Length)} | " +
                              $"{Pct(band.Count(r => r.Skills >= 2), band.Length)} / {Pct(band.Count(r => r.Skills >= 3), band.Length)} | " +
                              $"{Pct(band.Count(r => r.Tier >= 1), band.Length)} / {Pct(band.Count(r => r.Tier >= 2), band.Length)} | " +
                              $"{F(band.Average(r => r.Power) / 100.0, "+0.0")} % | {F(band.Average(r => r.Banked), "0")} |");
            }
            md.AppendLine();
        }

        // The bosses' bands and the Descent's length with the skills live: the runs carrying all three, against their shadows.
        private static void Bosses(StringBuilder md, Row[] rows, string[] cohorts, string[] heroes)
        {
            md.AppendLine("### The bosses and the Descent with all three skills").AppendLine();
            md.AppendLine("The runs that carried all three skills: each gate boss's fight, P50 s (share in 45-65 s; felled within 5 s), floor 1's Forge Warden, " +
                          "floor 2's Glass Regent, floor 3's Warden; the Descent's active minutes, P50 of the completed runs. Shadow: the same runs with " +
                          "skill 1 alone. The careful pilot's band is the row docs/57 6.2 holds.").AppendLine();
            md.AppendLine("| Cohort | Hero | Runs | Floor 1 Warden | Floor 2 Regent | Floor 3 Warden | Descent P50 min | Shadow: Warden / Regent / Warden | Shadow Descent |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|");
            foreach (string cohort in cohorts)
            foreach (string hero in heroes)
            {
                Row[] three = rows.Where(r => r.Cohort == cohort && r.Hero == hero && r.Skills == 3).ToArray();
                if (three.Length == 0)
                    continue;
                Row[] paired = three.Where(r => r.HasShadow).ToArray();
                string Band(IEnumerable<float> fights, bool full)
                {
                    float[] felled = fights.Where(s => s >= 0f).ToArray();
                    if (felled.Length == 0)
                        return "-";
                    string p50 = F(FoundryBatch.Percentile(felled, 50), "0.0");
                    return full ? $"{p50} ({Pct(felled.Count(s => s >= BandFrom && s <= BandTo), felled.Length)}; {felled.Count(s => s < 5f)})" : p50;
                }
                string Minutes(IEnumerable<float> seconds)
                {
                    float[] s = seconds.ToArray();
                    return s.Length == 0 ? "-" : F(FoundryBatch.Percentile(s, 50) / 60.0, "0.00");
                }
                md.AppendLine($"| {cohort} | {hero} | {three.Length} | {Band(three.Select(r => r.Boss1), true)} | {Band(three.Select(r => r.Boss2), true)} | " +
                              $"{Band(three.Select(r => r.Boss3), true)} | {Minutes(three.Where(r => r.Completed).Select(r => r.Seconds))} | " +
                              $"{Band(paired.Select(r => r.ShadowBoss1), false)} / {Band(paired.Select(r => r.ShadowBoss2), false)} / {Band(paired.Select(r => r.ShadowBoss3), false)} | " +
                              $"{Minutes(paired.Where(r => r.Shadow == 1).Select(r => r.ShadowSeconds))} |");
            }
            md.AppendLine();
        }

        private static Row[][] Campaigns(Row[] rows, string cohort, string hero) =>
            rows.Where(r => r.Cohort == cohort && (hero == null || r.Hero == hero))
                .GroupBy(r => (r.Hero, r.Campaign)).Select(g => g.OrderBy(r => r.Run).ToArray()).ToArray();

        private static string Q(int[] sorted, double q, int runs)
        {
            int value = sorted[Math.Min(sorted.Length - 1, (int)(q * sorted.Length))];
            return value > runs ? ">" + runs.ToString(CultureInfo.InvariantCulture) : value.ToString(CultureInfo.InvariantCulture);
        }

        private static string Interval((double Point, double Low, double High) ci) =>
            double.IsNaN(ci.Point) ? "-" : $"{Signed(ci.Low)} to {Signed(ci.High)}";

        // ---------------------------------------------------------------------------------------------------------------
        // The CSV
        // ---------------------------------------------------------------------------------------------------------------

        private const string Header = "realism,option,cohort,hero,campaign,run,seed,skills,tier,power,lv_weapon,lv_armour,lv_amulet,lv_ring," +
                                      "gold_before,banked,gold_after,completed,floors_cleared,died_on,death_phase,boss1_s,boss2_s,boss3_s,descent_s," +
                                      "shadow_completed,shadow_floors,shadow_boss1_s,shadow_boss2_s,shadow_boss3_s,shadow_descent_s";

        private static string Csv(IEnumerable<Row> rows, string hands, bool header)
        {
            var csv = new StringBuilder();
            if (header)
                csv.AppendLine(Header);
            foreach (Row r in rows)
                csv.AppendLine(string.Join(",", hands, r.Option, r.Cohort, r.Hero, I(r.Campaign), I(r.Run), I(r.Seed), I(r.Skills),
                    DifficultyTiers.Id((DifficultyTier)r.Tier), I(r.Power), I(r.Levels[0]), I(r.Levels[1]), I(r.Levels[2]), I(r.Levels[3]),
                    I(r.GoldBefore), I(r.Banked), I(r.GoldAfter), r.Completed ? "1" : "0", I(r.FloorsCleared), I(r.DiedOn),
                    r.DeathPhase == null ? string.Empty : "\"" + r.DeathPhase.Replace("\"", "\"\"") + "\"",
                    N(r.Boss1), N(r.Boss2), N(r.Boss3), N(r.Seconds),
                    r.HasShadow ? I(r.Shadow) : string.Empty, r.HasShadow ? I(r.ShadowFloors) : string.Empty,
                    r.HasShadow ? N(r.ShadowBoss1) : string.Empty, r.HasShadow ? N(r.ShadowBoss2) : string.Empty,
                    r.HasShadow ? N(r.ShadowBoss3) : string.Empty, r.HasShadow ? N(r.ShadowSeconds) : string.Empty));
            return csv.ToString();
        }

        private static IEnumerable<Row> ReadCsv(string path)
        {
            string[] names = Header.Split(',');
            foreach (string line in File.ReadLines(path).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                string[] cells = SplitCsv(line);
                if (cells.Length != names.Length)
                    continue;
                string Cell(string name) => cells[Array.IndexOf(names, name)];
                int Int(string name) => int.Parse(Cell(name), CultureInfo.InvariantCulture);
                float Float(string name) => Cell(name).Length == 0 ? -1f : float.Parse(Cell(name), CultureInfo.InvariantCulture);
                DifficultyTiers.TryParse(Cell("tier"), out DifficultyTier tier);
                bool shadow = Cell("shadow_completed").Length > 0;
                yield return new Row
                {
                    Option = Cell("option"), Cohort = Cell("cohort"), Hero = Cell("hero"), Campaign = Int("campaign"), Run = Int("run"),
                    Seed = Int("seed"), Skills = Int("skills"), Tier = (int)tier, Power = Int("power"),
                    Levels = new[] { Int("lv_weapon"), Int("lv_armour"), Int("lv_amulet"), Int("lv_ring") },
                    GoldBefore = Int("gold_before"), Banked = Int("banked"), GoldAfter = Int("gold_after"), Completed = Cell("completed") == "1",
                    FloorsCleared = Int("floors_cleared"), DiedOn = Int("died_on"), DeathPhase = Cell("death_phase").Length == 0 ? null : Cell("death_phase"),
                    Boss1 = Float("boss1_s"), Boss2 = Float("boss2_s"), Boss3 = Float("boss3_s"), Seconds = Float("descent_s"),
                    Shadow = shadow ? Int("shadow_completed") : -1, ShadowFloors = shadow ? Int("shadow_floors") : -1,
                    ShadowBoss1 = shadow ? Float("shadow_boss1_s") : -1f, ShadowBoss2 = shadow ? Float("shadow_boss2_s") : -1f,
                    ShadowBoss3 = shadow ? Float("shadow_boss3_s") : -1f, ShadowSeconds = shadow ? Float("shadow_descent_s") : -1f
                };
            }
        }

        private static string[] SplitCsv(string line)
        {
            var cells = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (quoted)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        cell.Append('"');
                        i++;
                    }
                    else if (c == '"')
                        quoted = false;
                    else
                        cell.Append(c);
                }
                else if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    cells.Add(cell.ToString());
                    cell.Clear();
                }
                else
                    cell.Append(c);
            }
            cells.Add(cell.ToString());
            return cells.ToArray();
        }

        private static string I(int value) => value.ToString(CultureInfo.InvariantCulture);

        private static string N(float value) => value < 0f ? string.Empty : value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F(double value, string format) => double.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);

        private static string Signed(double value) => double.IsNaN(value) ? "-" : value.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture);

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0.0", CultureInfo.InvariantCulture) + " %";

        private static string[] List(string name, string fallback) =>
            (Environment.GetEnvironmentVariable(name) ?? fallback).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToArray();

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
