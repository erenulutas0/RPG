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
    // The Gloam Gardens' own enemies (docs/52 section 8, step 1; the lead's decisions 2026-11-12 and 2026-11-15). Plays every
    // cohort and both heroes down the whole Descent (floors 1-3 and the finale) on the same seeds with human hands, once as
    // shipped - floor 3's Thorn Post led by the Cinder Bloom (`bloom-m20-h70-r1.2`) since 2026-11-15, floor 2's Sentry Hall
    // by the shipped Mirror Sentry - and once for each option: the Grunt Captain back (`captain`, floor 3 before), the
    // Bloom at docs/52's starting values (`bloom`) or a swept variant (DescentSimulation.GloamChain; every `bloom-*` option
    // starts from the starting values: 280 health, maul 10.8 at reach 1.5), or with floor 2's Grunt Captain back (`-f2c`,
    // VaultsCaptain).
    // docs/52 sections 3 and 4 (2026-11-15): the Pollen Wraith in the Mite's place in floor 3's horde and halls
    // (`wraith`, `wraith-*`: PollenRule's rates, radius, bloom and stillness), the Thornling in the Runner's place in the
    // horde and Ash Orchard (`thorn`, `thorn-*`: ThornlingRule's ring, damage, dive range and gap), and the two together
    // with the shipped Bloom (`both:*`). Where an option reads the roles, the careful Vanguard is also started with the
    // Staff and with the Daggers beside its Sword. No pilot habit: none of the three asks for one.
    //
    // Explicit, so a plain `dotnet test` skips it:
    //
    //   dotnet test Tools/CombatChecks -c Release --filter "FullyQualifiedName~GloamEnemiesResearch"
    //
    // Environment, all optional: FOUNDRY_SEEDS (200), FOUNDRY_FIRST_SEED (1), FOUNDRY_PARALLEL (half the processors),
    // FOUNDRY_OUT (the repository's TestResults), FOUNDRY_REALISM (typical; slow; frame-perfect for none), GLOAM_OPTIONS
    // (comma separated option keys; all), GLOAM_COHORTS (comma separated cohort names; all five), GLOAM_ROLES (0 skips the
    // roles row). Writes gloam-enemies.md and a row a run in gloam-enemies.csv, rewritten after each option.
    [Explicit("The Gloam Gardens' own enemies: run it by name.")]
    public sealed class GloamEnemiesResearch
    {
        private const float BandFrom = 45f;
        private const float BandTo = 65f;
        // Thorn Post: floor 3's fifth room (Ash Orchard, Rootbound Walk, Bloom Gate, the Grafting Forge, Thorn Post, the Grove).
        private const int ThornPost = 5;

        private sealed class Option
        {
            public string Key;
            public string What;
            public bool Roles;
            public Action<DescentSimulation.FoundryLevers> Set = _ => { };
        }

        // docs/52's starting values (round 1), then the option's own levers.
        private static Action<DescentSimulation.FoundryLevers> Bloom(Action<DescentSimulation.FoundryLevers> more = null) => l =>
        {
            l.BloomHealthScale = 1f;
            l.BloomMaulDamage = DescentSimulation.Docs52MaulDamage;
            l.BloomMaulReach = DescentSimulation.Docs52MaulReach;
            more?.Invoke(l);
        };

        // The maul at a share of docs/52's 10.8.
        private static float Maul(float share) => DescentSimulation.Docs52MaulDamage * share;

        private static IEnumerable<Option> Options()
        {
            yield return new Option { Key = "shipped", What = "floor 3 as shipped since 2026-11-15: the Cinder Bloom (bloom-m20-h70-r1.2: 196 health, maul 2.16 every 1.8 s at reach 1.2) leads Thorn Post; floor 2's Sentry Hall the shipped Mirror Sentry", Roles = true };
            yield return new Option { Key = "captain", What = "floor 3 before 2026-11-15: the Grunt Captain leads Thorn Post (FoundryLevers.GloamCaptain)", Roles = true, Set = l => l.GloamCaptain = true };
            yield return new Option { Key = "bloom", What = "the Cinder Bloom leads Thorn Post at docs/52's starting values: 280 health (549 on floor 3), Bark Maul 10.8 / 1.8 s at reach 1.5 (0.6 s), speed 1.2; closed 3.5 s taking 25 %, open 2.5 s rooted and taking 100 %", Roles = true, Set = Bloom() };
            // The balance rule's study (2026-11-17, docs/56): each shipped creature against what it replaced, one at a time,
            // and the A/A probes (the shipped game, the hands' draws shifted from floor 2 or floor 3 on: divergence alone).
            yield return new Option { Key = "f2-captain", What = "floor 2's Grunt Captain back in Sentry Hall (VaultsCaptain), floor 3 as shipped", Set = l => l.VaultsCaptain = true };
            yield return new Option { Key = "f2-warden", What = "floor 2's Forge Warden back at its gate at its share then (VaultsWarden), the rest as shipped", Set = l => l.VaultsWarden = true };
            yield return new Option { Key = "aa-f2", What = "A/A: the shipped game, one unread draw of the pilot's dice on entering floor 2", Set = l => l.NudgeFloor = 2 };
            yield return new Option { Key = "aa-f3", What = "A/A: the shipped game, one unread draw of the pilot's dice on entering floor 3", Set = l => l.NudgeFloor = 3 };
            yield return new Option { Key = "aa-f3b", What = "A/A: the shipped game, two unread draws of the pilot's dice on entering floor 3", Set = l => { l.NudgeFloor = 3; l.NudgeDraws = 2; } };
            yield return new Option { Key = "aa-f2b", What = "A/A: the shipped game, two unread draws of the pilot's dice on entering floor 2", Set = l => { l.NudgeFloor = 2; l.NudgeDraws = 2; } };
            yield return new Option { Key = "aa-f2c", What = "A/A: the shipped game, three unread draws of the pilot's dice on entering floor 2", Set = l => { l.NudgeFloor = 2; l.NudgeDraws = 3; } };
            yield return new Option { Key = "aa-f3c", What = "A/A: the shipped game, three unread draws of the pilot's dice on entering floor 3", Set = l => { l.NudgeFloor = 3; l.NudgeDraws = 3; } };
            yield return new Option { Key = "shipped-f2c", What = "`captain` with floor 2's Grunt Captain back in Sentry Hall (VaultsCaptain: floor 2 before 2026-11-12)", Set = l => { l.VaultsCaptain = true; l.GloamCaptain = true; } };
            yield return new Option { Key = "bloom-f2c", What = "`bloom` with floor 2's Grunt Captain back in Sentry Hall", Set = Bloom(l => l.VaultsCaptain = true) };
            // docs/52 section 5's sweep and section 8's fallbacks, one lever at a time off the starting values.
            foreach (int health in new[] { 60, 80, 125 })
                yield return new Option { Key = $"bloom-h{health}", What = $"the Bloom at {health} % of its 280 health", Roles = true, Set = Bloom(l => l.BloomHealthScale = health / 100f) };
            foreach (int maul in new[] { 75, 125 })
                yield return new Option { Key = $"bloom-m{maul}", What = $"the Bloom's maul at {maul} % of 10.8", Roles = true, Set = Bloom(l => l.BloomMaulDamage = Maul(maul / 100f)) };
            foreach (float open in new[] { 2.0f, 3.0f })
                yield return new Option { Key = "bloom-o" + open.ToString("0.0", CultureInfo.InvariantCulture), What = $"the Bloom open {open.ToString("0.0", CultureInfo.InvariantCulture)} s", Roles = true, Set = Bloom(l => l.BloomOpen = open) };
            foreach (float closed in new[] { 3.0f, 4.0f })
                yield return new Option { Key = "bloom-c" + closed.ToString("0.0", CultureInfo.InvariantCulture), What = $"the Bloom closed {closed.ToString("0.0", CultureInfo.InvariantCulture)} s", Roles = true, Set = Bloom(l => l.BloomClosed = closed) };
            foreach (int bark in new[] { 15, 35, 50 })
                yield return new Option { Key = $"bloom-b{bark}", What = $"the closed bark letting {bark} % through", Roles = true, Set = Bloom(l => l.BloomBark = bark / 100f) };
            yield return new Option { Key = "bloom-r1.4", What = "the Bloom's maul reaching 1.4", Roles = true, Set = Bloom(l => l.BloomMaulReach = 1.4f) };
            // Round 2 (past docs/52's list): what keeps a kiting hero out of the maul - a shorter reach than the kite's 1.6
            // less what a 0.25 s reaction lets the Bloom close, a slower walk - and a lighter maul for the flat cost.
            yield return new Option { Key = "bloom-m50", What = "the Bloom's maul at 50 % of 10.8", Roles = true, Set = Bloom(l => l.BloomMaulDamage = Maul(.5f)) };
            yield return new Option { Key = "bloom-r1.2", What = "the Bloom's maul reaching 1.2", Roles = true, Set = Bloom(l => l.BloomMaulReach = 1.2f) };
            yield return new Option { Key = "bloom-s0.9", What = "the Bloom walking 0.9 a second while closed (the Tank's)", Roles = true, Set = Bloom(l => l.BloomSpeed = .9f) };
            yield return new Option { Key = "bloom-r1.2-s0.9", What = "the Bloom's maul reaching 1.2, walking 0.9", Roles = true, Set = Bloom(l => { l.BloomMaulReach = 1.2f; l.BloomSpeed = .9f; }) };
            // Round 3: a windup on the maul, so that a hero that walks out of reach in time is not struck (the kite's read).
            foreach (float windup in new[] { .5f, .7f, .9f })
                yield return new Option
                {
                    Key = "bloom-w" + windup.ToString("0.0", CultureInfo.InvariantCulture),
                    What = $"the Bloom's maul landing {windup.ToString("0.0", CultureInfo.InvariantCulture)} s after its swing begins, on a hero still in reach", Roles = true,
                    Set = Bloom(l => l.BloomWindup = windup)
                };
            foreach (int maul in new[] { 50, 75 })
            foreach (float windup in new[] { .5f, .7f })
                yield return new Option
                {
                    Key = $"bloom-w{windup.ToString("0.0", CultureInfo.InvariantCulture)}-m{maul}",
                    What = $"the Bloom's maul at {maul} % of 10.8, landing {windup.ToString("0.0", CultureInfo.InvariantCulture)} s after its swing begins on a hero still in reach", Roles = true,
                    Set = Bloom(l => { l.BloomWindup = windup; l.BloomMaulDamage = Maul(maul / 100f); })
                };
            // Round 2's cheaper combinations: the maul at a half or a third, with a shorter reach or less health.
            foreach (int maul in new[] { 20, 25, 30, 35, 50, 75, 100 })
            foreach (int health in new[] { 60, 70, 80, 100 })
            foreach (float reach in new[] { 1.0f, 1.2f, 1.5f })
            {
                if (health == 100 && reach == 1.5f && (maul == 50 || maul == 75 || maul == 100))
                    continue;
                string key = $"bloom-m{maul}" + (health != 100 ? $"-h{health}" : string.Empty) +
                             (reach != 1.5f ? "-r" + reach.ToString("0.0", CultureInfo.InvariantCulture) : string.Empty);
                yield return new Option
                {
                    Key = key, What = $"the Bloom's maul at {maul} % of 10.8, {health} % of its health, reaching {reach.ToString("0.0", CultureInfo.InvariantCulture)}", Roles = true,
                    Set = Bloom(l => { l.BloomMaulDamage = Maul(maul / 100f); l.BloomHealthScale = health / 100f; l.BloomMaulReach = reach; })
                };
            }
            // Combinations, for a variant that meets several rows at once (read after the single levers).
            yield return new Option { Key = "bloom-h80-m125", What = "the Bloom at 80 % health with its maul at 125 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .8f; l.BloomMaulDamage = Maul(1.25f); }) };
            yield return new Option { Key = "bloom-h60-m125", What = "the Bloom at 60 % health with its maul at 125 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .6f; l.BloomMaulDamage = Maul(1.25f); }) };
            yield return new Option { Key = "bloom-h80-m75", What = "the Bloom at 80 % health with its maul at 75 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .8f; l.BloomMaulDamage = Maul(.75f); }) };
            yield return new Option { Key = "bloom-h60-m75", What = "the Bloom at 60 % health with its maul at 75 %", Roles = true, Set = Bloom(l => { l.BloomHealthScale = .6f; l.BloomMaulDamage = Maul(.75f); }) };
            yield return new Option { Key = "bloom-h60", What = "the Bloom at 60 % health", Roles = true, Set = Bloom(l => l.BloomHealthScale = .6f) };

            // docs/52 section 3's Pollen Wraith (step 1), the shipped Bloom at Thorn Post: the starting values, then one lever
            // at a time (section 3's sweep and section 8's fallbacks), then the combinations read after them.
            yield return new Option { Key = "wraith", What = "the Pollen Wraith in the Mite's place in floor 3's horde and halls at docs/52's starting values: +0.10 a second a Wraith within 1.6 of a still hero (at most 3), -0.5 a second moving, the bloom 6 % of the hero's maximum health, still under 1.0 a second (smoothed over 0.5 s)", Roles = true, Set = Wraith() };
            foreach ((string key, string what, Action<DescentSimulation.FoundryLevers> set) in WraithSweep())
                yield return new Option { Key = key, What = what, Set = Wraith(set) };
            // docs/52 section 4's Thornling (step 1), the shipped Bloom at Thorn Post.
            yield return new Option { Key = "thorn", What = "the Thornling in the Runner's place in floor 3's horde and Ash Orchard at docs/52's starting values: walked 3 s, 2.5-5 from the hero, a ring of 1.2 at the hero's spot filling 0.8 s, 5 % of the hero's maximum health, stuck 1 s, one ring on the floor and 2.5 s after the last eruption", Roles = true, Set = Thorn() };
            foreach ((string key, string what, Action<DescentSimulation.FoundryLevers> set) in ThornSweep())
                yield return new Option { Key = key, What = what, Set = Thorn(set) };
            // Both together with the shipped Bloom: the starting values, and the variants read after the sweeps.
            yield return new Option { Key = "both", What = "`wraith` and `thorn` together, the shipped Bloom at Thorn Post", Roles = true, Set = l => { Wraith()(l); Thorn()(l); } };
            foreach ((string key, string what, Action<DescentSimulation.FoundryLevers> wraith, Action<DescentSimulation.FoundryLevers> thorn) in BothSweep())
                yield return new Option { Key = key, What = what, Roles = true, Set = l => { Wraith(wraith)(l); Thorn(thorn)(l); } };
        }

        private static string Number(float value) => value.ToString("0.0#", CultureInfo.InvariantCulture);

        // The Wraith at docs/52's starting values, then the option's own levers.
        private static Action<DescentSimulation.FoundryLevers> Wraith(Action<DescentSimulation.FoundryLevers> more = null) => l =>
        {
            l.GloamWraiths = true;
            more?.Invoke(l);
        };

        // The Thornling at docs/52's starting values, then the option's own levers.
        private static Action<DescentSimulation.FoundryLevers> Thorn(Action<DescentSimulation.FoundryLevers> more = null) => l =>
        {
            l.GloamThornlings = true;
            more?.Invoke(l);
        };

        private static IEnumerable<(string, string, Action<DescentSimulation.FoundryLevers>)> WraithSweep()
        {
            foreach (float rate in new[] { .05f, .07f, .15f })
                yield return ($"wraith-r{Number(rate)}", $"the Wraith's rise {Number(rate)} a second a Wraith", l => l.WraithRate = rate);
            foreach (float fall in new[] { .25f, 1f })
                yield return ($"wraith-f{Number(fall)}", $"the pollen falling {Number(fall)} a second while moving", l => l.WraithFall = fall);
            foreach (int share in new[] { 3, 4, 8 })
                yield return ($"wraith-s{share}", $"the bloom {share} % of the hero's maximum health", l => l.WraithShare = share / 100f);
            foreach (float radius in new[] { 1.2f, 1.4f, 1.8f, 2.2f, 2.5f, 3f, 3.5f, 4f })
                yield return ($"wraith-d{Number(radius)}", $"the Wraith's radius {Number(radius)}", l => l.WraithRadius = radius);
            foreach (float still in new[] { .6f, 1.4f })
                yield return ($"wraith-v{Number(still)}", $"the hero still under {Number(still)} a second", l => l.WraithStillSpeed = still);
            foreach (float rate in new[] { .2f, .3f, .5f })
                yield return ($"wraith-r{Number(rate)}", $"the Wraith's rise {Number(rate)} a second a Wraith", l => l.WraithRate = rate);
            foreach (float fall in new[] { .1f })
                yield return ($"wraith-f{Number(fall)}", $"the pollen falling {Number(fall)} a second while moving", l => l.WraithFall = fall);
            // Round 2 (2026-11-15, after round 1 found the starting Wraith idle: a floor-3 hero stands still within 1.6 of a
            // body in the Wraith's place about 0.1 s a floor - the horde's bodies fall before they come that near): a
            // radius the bodies reach, at a rise and a fall that let still seconds add up.
            foreach (float radius in new[] { 2.5f, 3f, 4f })
            foreach (float rate in new[] { .15f, .3f })
            foreach (float fall in new[] { .5f, .1f })
                yield return ($"wraith-d{Number(radius)}-r{Number(rate)}-f{Number(fall)}", $"the Wraith's radius {Number(radius)}, its rise {Number(rate)} a second, the fall {Number(fall)} a second",
                    l => { l.WraithRadius = radius; l.WraithRate = rate; l.WraithFall = fall; });
            // Round 3 (2026-11-16), the redesign screen: (a) a fallen Wraith's pollen cloud feeds the pollen of a hero
            // standing in it (or, `-move`, of a hero inside it at all); (c) the pollen settles only after the hero has stood
            // still a while without a break, from Wraiths within a wide radius.
            foreach (float rate in new[] { .1f, .3f })
            foreach (float radius in new[] { 1.2f, 2f })
            foreach (bool moving in new[] { false, true })
                yield return ($"wraith-cloud{(moving ? "-move" : string.Empty)}-d{Number(radius)}-r{Number(rate)}",
                    $"a fallen Wraith's pollen cloud (radius {Number(radius)}, 4 s) feeding {Number(rate)} a second a cloud the hero stands in" + (moving ? ", moving or not (falling only outside every cloud)" : ", still"),
                    l => { l.WraithClouds = true; l.WraithCloudRadius = radius; l.WraithRate = rate; l.WraithCloudWhileMoving = moving; });
            foreach (float settle in new[] { 1.5f, 3f })
            foreach (float rate in new[] { .3f, .5f })
                yield return ($"wraith-settle{Number(settle)}-d4.0-r{Number(rate)}", $"the pollen rising {Number(rate)} a second a Wraith within 4 only after {Number(settle)} s still without a break",
                    l => { l.WraithSettleAfter = settle; l.WraithRadius = 4f; l.WraithRate = rate; });
            // Combinations, read after the single levers.
            foreach (float rate in new[] { .05f, .07f })
            foreach (int share in new[] { 3, 4 })
                yield return ($"wraith-r{Number(rate)}-s{share}", $"the Wraith's rise {Number(rate)} a second, the bloom {share} %", l => { l.WraithRate = rate; l.WraithShare = share / 100f; });
            foreach (int share in new[] { 3, 4 })
                yield return ($"wraith-d1.4-s{share}", $"the Wraith's radius 1.4, the bloom {share} %", l => { l.WraithRadius = 1.4f; l.WraithShare = share / 100f; });
            foreach (int share in new[] { 3, 4 })
                yield return ($"wraith-v0.6-s{share}", $"the hero still under 0.6, the bloom {share} %", l => { l.WraithStillSpeed = .6f; l.WraithShare = share / 100f; });
        }

        private static IEnumerable<(string, string, Action<DescentSimulation.FoundryLevers>)> ThornSweep()
        {
            foreach (float telegraph in new[] { .9f, 1f, 1.2f })
                yield return ($"thorn-t{Number(telegraph)}", $"the ring filling {Number(telegraph)} s", l => l.ThornlingTelegraph = telegraph);
            foreach (int share in new[] { 3, 4, 6 })
                yield return ($"thorn-s{share}", $"the eruption {share} % of the hero's maximum health", l => l.ThornlingShare = share / 100f);
            yield return ("thorn-dv2-4", "the dive from 2-4 of the hero", l => { l.ThornlingDiveMin = 2f; l.ThornlingDiveMax = 4f; });
            yield return ("thorn-dv3-6", "the dive from 3-6 of the hero", l => { l.ThornlingDiveMin = 3f; l.ThornlingDiveMax = 6f; });
            foreach (float gap in new[] { 4f, 6f })
                yield return ($"thorn-g{Number(gap)}", $"a new ring {Number(gap)} s after the last eruption", l => l.ThornlingGap = gap);
            yield return ("thorn-r1.4", "the ring's radius 1.4", l => l.ThornlingRadius = 1.4f);
            // Round 3 (2026-11-16, at lead-integrate-2): a ring that takes nothing (what the dodging and the divergence cost on
            // their own), and no ring while the gate boss stands.
            yield return ("thorn-s0", "the eruption taking nothing (the rings and the dodges stay)", l => l.ThornlingShare = 0f);
            yield return ("thorn-quiet", "no new ring while the gate boss stands", l => l.ThornlingQuietAtGate = true);
            yield return ("thorn-s0-unseen", "the eruption taking nothing and the ring no mark for the pilots (diagnostic: the Thornling's body alone)", l => { l.ThornlingShare = 0f; l.ThornlingUnseen = true; });
            yield return ("thorn-home", "the Thornling coming up where it dove (diagnostic: the ring still hurts at the hero's spot)", l => l.ThornlingEruptsHome = true);
            yield return ("thorn-t1.0-quiet", "the ring filling 1.0 s, no new ring while the gate boss stands", l => { l.ThornlingTelegraph = 1f; l.ThornlingQuietAtGate = true; });
            // Combinations, read after the single levers.
            foreach (float telegraph in new[] { .8f, 1f })
            foreach (int share in new[] { 3, 4 })
            foreach (float gap in new[] { 2.5f, 4f })
            {
                if (telegraph == .8f && gap == 2.5f)
                    continue;
                yield return ($"thorn-t{Number(telegraph)}-s{share}-g{Number(gap)}", $"the ring filling {Number(telegraph)} s, the eruption {share} %, {Number(gap)} s between",
                    l => { l.ThornlingTelegraph = telegraph; l.ThornlingShare = share / 100f; l.ThornlingGap = gap; });
            }
        }

        // The two together: each creature's variant (empty: its starting values), named after both.
        private static IEnumerable<(string, string, Action<DescentSimulation.FoundryLevers>, Action<DescentSimulation.FoundryLevers>)> BothSweep()
        {
            var wraiths = WraithSweep().ToDictionary(w => w.Item1);
            var thorns = ThornSweep().ToDictionary(t => t.Item1);
            wraiths["wraith"] = ("wraith", "the Wraith at its starting values", _ => { });
            thorns["thorn"] = ("thorn", "the Thornling at its starting values", _ => { });
            foreach (string w in wraiths.Keys)
            foreach (string t in thorns.Keys)
            {
                if (w == "wraith" && t == "thorn")
                    continue;
                yield return ($"both:{w}+{t}", $"`{w}` and `{t}` together, the shipped Bloom at Thorn Post", wraiths[w].Item3, thorns[t].Item3);
            }
        }

        private sealed class Cell
        {
            public Option Option;
            public string Cohort;
            public string Hero;
            public DescentSimulation.FoundryHero HeroDefinition;
            public DescentSimulation.FoundryLevers Levers;
            public DescentSimulation.FoundryResult[] Runs;
        }

        [Test]
        public void TheGloamEnemiesOnFloorThree()
        {
            int count = Setting("FOUNDRY_SEEDS", 200);
            int first = Setting("FOUNDRY_FIRST_SEED", 1);
            int parallel = Setting("FOUNDRY_PARALLEL", Math.Max(1, Environment.ProcessorCount / 2));
            bool readRoles = Setting("GLOAM_ROLES", 1) != 0;
            int[] seeds = FoundryBatch.Seeds(first, count);
            string output = Environment.GetEnvironmentVariable("FOUNDRY_OUT") ?? Path.Combine(RepositoryRoot(), "TestResults");
            Directory.CreateDirectory(output);
            string[] keys = List("GLOAM_OPTIONS", string.Join(",", Options().Select(o => o.Key)));
            Option[] options = keys.Select(k => Options().FirstOrDefault(o => o.Key == k)).Where(o => o != null).ToArray();
            Assert.That(options.Length, Is.EqualTo(keys.Length), "every option named exists");
            string[] cohorts = List("GLOAM_COHORTS", string.Join(",", AutopilotCohort.Cohorts.Select(c => c.Name)));
            string pilot = Environment.GetEnvironmentVariable("FOUNDRY_REALISM") ?? "typical";
            PilotRealism realism = pilot == "frame-perfect" ? null : PilotRealism.Named(pilot);
            string hands = realism != null ? realism.Name : "frame-perfect";
            var heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() };

            var cells = new List<Cell>();
            var roles = new List<(Option Option, FoundryBatch.Batch[] Weapons)>();
            foreach (Option option in options)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                Cell carefulVanguard = null;
                foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts.Where(c => cohorts.Contains(c.Name)))
                foreach (DescentSimulation.FoundryHero hero in heroes)
                {
                    DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(cohort, DescentSimulation.FinaleFloor, realism)
                        .Copy($"cohort {cohort.Name}, {option.Key}, {hands}");
                    option.Set(levers);
                    Cell cell = Play(option, cohort.Name, hero, levers, seeds, parallel);
                    cells.Add(cell);
                    if (cohort == AutopilotCohort.Careful && hero.Name == heroes[0].Name)
                        carefulVanguard = cell;
                }
                if (readRoles && option.Roles && carefulVanguard != null)
                {
                    var weapons = new List<FoundryBatch.Batch>();
                    foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
                    {
                        if (weapon.Id == DescentSimulation.SwordId)
                        {
                            weapons.Add(Batch(carefulVanguard));
                            continue;
                        }
                        DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
                        hero.Weapon = weapon;
                        hero.Name = "Vanguard + " + weapon.DisplayName;
                        DescentSimulation.FoundryLevers levers = FoundryCohorts.Levers(AutopilotCohort.Careful, DescentSimulation.FinaleFloor, realism)
                            .Copy($"starting {weapon.Id}, {option.Key}, {hands}");
                        option.Set(levers);
                        weapons.Add(Batch(Play(option, "careful", hero, levers, seeds, parallel)));
                    }
                    roles.Add((option, weapons.ToArray()));
                }
                TestContext.Progress.WriteLine($"{option.Key}: {watch.Elapsed.TotalSeconds:0} s");
                File.WriteAllText(Path.Combine(output, "gloam-enemies.md"), Report(cells, roles, first, count, hands));
                File.WriteAllText(Path.Combine(output, "gloam-enemies.csv"), Csv(cells, hands));
            }
            TestContext.WriteLine(Report(cells, roles, first, count, hands));
            Assert.That(cells.All(c => c.Runs.Length == count), "every seed ran");
        }

        private static Cell Play(Option option, string cohort, DescentSimulation.FoundryHero hero, DescentSimulation.FoundryLevers levers,
            int[] seeds, int parallel)
        {
            var runs = new DescentSimulation.FoundryResult[seeds.Length];
            Parallel.For(0, seeds.Length, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, parallel) },
                i => runs[i] = DescentSimulation.RunFoundry(hero, seeds[i], levers, DescentSimulation.GloamChain(levers)));
            return new Cell { Option = option, Cohort = cohort, Hero = hero.Name, HeroDefinition = hero, Levers = levers, Runs = runs };
        }

        // A cell's floor 3: the floor-3 results of its runs that cleared floor 2 and went down, as a batch of its own.
        private static FoundryBatch.Batch Batch(Cell cell) =>
            new FoundryBatch.Batch { Hero = cell.HeroDefinition, Levers = cell.Levers, Runs = cell.Runs };

        private static FoundryBatch.Batch ThreeOf(FoundryBatch.Batch b) => new FoundryBatch.Batch
        {
            Hero = b.Hero, Levers = b.Levers, WallSeconds = b.WallSeconds, Runs = b.Runs.Select(Three).Where(r => r != null).ToArray()
        };

        private static DescentSimulation.FoundryResult Two(DescentSimulation.FoundryResult run) => run.NextFloor;

        private static DescentSimulation.FoundryResult Three(DescentSimulation.FoundryResult run)
        {
            DescentSimulation.FoundryResult three = run.NextFloor?.NextFloor;
            return three != null && !FoundryBatch.IsFinale(three) ? three : null;
        }

        private static DescentSimulation.FoundryResult Finale(DescentSimulation.FoundryResult run) => FoundryBatch.FinaleOf(run);

        private static float Descent(DescentSimulation.FoundryResult run)
        {
            float sum = 0f;
            for (DescentSimulation.FoundryResult r = run; r != null; r = r.NextFloor)
                sum += r.FloorSeconds;
            return sum;
        }

        private static bool Completed(DescentSimulation.FoundryResult run) => Finale(run) is DescentSimulation.FoundryResult f && f.WalkedOut;

        // Floor 3's elite leaders felled (one a floor: floor 3 has no extra elite).
        private static IEnumerable<DescentSimulation.EliteFight> Fights(IEnumerable<DescentSimulation.FoundryResult> floors) =>
            floors.Where(r => r.EliteFights != null).SelectMany(r => r.EliteFights);

        // The floor 3s that met Thorn Post (its wave stood with the hero alive).
        private static DescentSimulation.FoundryResult[] MetThornPost(IEnumerable<DescentSimulation.FoundryResult> floors) =>
            floors.Where(r => r.EliteFought > 0f).ToArray();

        private static bool DiedAtThornPost(DescentSimulation.FoundryResult r) =>
            r.Outcome == DescentSimulation.FoundryOutcome.Died && r.DeathRoom == ThornPost;

        private static string Report(List<Cell> cells, List<(Option Option, FoundryBatch.Batch[] Weapons)> roles, int first, int count, string hands)
        {
            var md = new StringBuilder();
            md.AppendLine($"# The Gloam Gardens' own enemies: seeds {first}-{first + count - 1} a hero ({hands})").AppendLine();
            md.AppendLine("Each run plays floors 1 and 2 as the option has them, floor 3 with the option's creatures and the finale. Went down: floor 2's clears. Cleared: floor 3, of those that went down. " +
                          "Deaths: floor 3's, by room (1-3 halls, 5 Thorn Post, boss the Grove) and the killers. Elite fight: Thorn Post's leader from walking in to falling, P50 / P90 s; " +
                          "in reach: of it, the seconds it stood within the hero's longest weapon's reach, P50; D: its health over those seconds, P50 (docs/52 section 5). " +
                          "Thorn Post taken: health the hero lost while its wave stood, mean over the floor 3s that met it (from the leader / its escort / the horde). " +
                          "Windows: the Bloom's open windows begun by its fall, 1 / 2 / 3+ (fell open). Gate: floor 3's Warden fight P50 (share in 45-65 s). Completed: walked out of the finale.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Went down | Cleared | Deaths | Thorn Post deaths | Killers | Elite fight P50 / P90 s | In reach P50 s | D P50 | Thorn Post taken (leader / escort / horde) | Windows | Gate P50 (in band) | Floor 3 P50 s | Completed | Descent P50 |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Cell c in cells)
                md.AppendLine(Row(c));
            md.AppendLine();

            md.AppendLine("## The Wraith and the Thornling (floor 3, a mean over the floor 3s that went down)").AppendLine();
            md.AppendLine("Blooms: the pollen's (in rooms: while a hall's or an elite room's wave stood); pollen taken: its health. Still near: seconds the hero stood still " +
                          "(smoothed speed under the option's threshold) within the Wraith's radius of a body in its place - a Wraith, or a horde or hall Cinder Mite (the Wraith's opportunity, read with or without Wraiths); still: seconds still at all; near: seconds within that radius of such a body, still or not. " +
                          "Rings: the Thornlings'; hits: eruptions that caught the hero (in a lapse: its ring overlapped one, a card's included); thorns taken: their health; stuck landings: the hero's weapon landings on a stuck Thornling; shortest hit: the shortest ring that hit, s.").AppendLine();
            md.AppendLine("| Option | Cohort | Hero | Blooms | In rooms | Pollen taken | Still near s | Still s | Near s | Rings | Hits | In a lapse | Thorns taken | Rings cancelled | Stuck landings | Shortest hit s |");
            md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|");
            foreach (Cell c in cells)
                md.AppendLine(CreatureRow(c));
            md.AppendLine();

            md.AppendLine("## By option (Vanguard / Ilya)").AppendLine();
            md.AppendLine("Floor-3 clears of those that went down; Thorn Post taken: mean health lost while its wave stood; completion: walked out of the finale, pooled over both heroes.").AppendLine();
            string[] names = cells.Select(c => c.Cohort).Distinct().ToArray();
            md.AppendLine("| Option | " + string.Join(" | ", names.Select(n => n + " F3 cleared")) + " | " + string.Join(" | ", names.Select(n => n + " completed")) +
                          " | Elite fight P50 careful / standing | Thorn Post taken careful / standing / clumsy | Standing / careful taken |");
            md.AppendLine("|---|" + string.Concat(Enumerable.Repeat("---|", names.Length * 2)) + "---|---|---|");
            foreach (Option option in cells.Select(c => c.Option).Distinct())
            {
                Cell[] mine = cells.Where(c => c.Option == option).ToArray();
                string Pair(string cohort, Func<Cell, string> read) =>
                    string.Join(" / ", mine.Where(c => c.Cohort == cohort).Select(read));
                string Pooled(string cohort)
                {
                    Cell[] two = mine.Where(c => c.Cohort == cohort).ToArray();
                    int done = two.Sum(c => c.Runs.Count(Completed)), all = two.Sum(c => c.Runs.Length);
                    return all == 0 ? "-" : $"{done} ({(100.0 * done / all).ToString("0.0", CultureInfo.InvariantCulture)} %)";
                }
                double Taken(string cohort)
                {
                    var met = MetThornPost(mine.Where(c => c.Cohort == cohort).SelectMany(c => c.Runs.Select(Three).Where(r => r != null)));
                    return met.Length == 0 ? double.NaN : met.Average(r => r.EliteTaken);
                }
                string Fight(string cohort) =>
                    F1(FoundryBatch.Percentile(Fights(mine.Where(c => c.Cohort == cohort).SelectMany(c => c.Runs.Select(Three).Where(r => r != null))).Select(f => f.Lifetime), 50));
                double careful = Taken("careful"), standing = Taken("standing");
                md.AppendLine($"| `{option.Key}` | " +
                              string.Join(" | ", names.Select(n => Pair(n, c => c.Runs.Select(Three).Count(r => r != null && r.Cleared).ToString(CultureInfo.InvariantCulture)))) + " | " +
                              string.Join(" | ", names.Select(Pooled)) + $" | {Fight("careful")} / {Fight("standing")} | " +
                              $"{F1(careful)} / {F1(standing)} / {F1(Taken("clumsy"))} | {(careful > 0 ? (standing / careful).ToString("0.00", CultureInfo.InvariantCulture) + "x" : "-")} |");
            }
            md.AppendLine();

            if (roles.Count > 0)
            {
                md.AppendLine("## Floor 3's roles row (careful hands; the Vanguard started with each weapon)").AppendLine();
                md.AppendLine("A starting weapon's own damage a second on floor 3: the horde (a second held), the elite room's leader (a second of the elite fight), the gate boss (a second of its fight). " +
                              "Roles: each weapon's lead job (FoundryGate.LeadsJob) over horde / elite leader / gate boss. Elite fight: Thorn Post's leader walk-in to fall P50 s; D: its health over its seconds in reach, P50.").AppendLine();
                md.AppendLine("| Option | Horde: Sword / Staff / Daggers | Elite leader | Gate boss | Roles | Staff's lead on the horde | Daggers' lead on the elite leader | Sword's lead on the gate boss | Elite fight P50 s | D P50 | Gate fight P50 s | Floor 3 cleared |");
                md.AppendLine("|---|---|---|---|---|---|---|---|---|---|---|---|");
                foreach ((Option option, FoundryBatch.Batch[] weapons) in roles)
                {
                    var three = weapons.Select(ThreeOf).ToArray();
                    var jobs = three.Select(b => (Name: b.Hero.Weapon.DisplayName, Jobs: FoundryBatch.RoleJobsOf(b, true))).ToArray();
                    string Three3(int job) => string.Join(" / ", jobs.Select(j => F1(j.Jobs[job])));
                    md.AppendLine($"| `{option.Key}` | {Three3(0)} | {Three3(1)} | {Three3(2)} | {FoundryBatch.Roles(jobs)} | " +
                                  $"{Lead(jobs, "Staff", 0)} | {Lead(jobs, "Daggers", 1)} | {Lead(jobs, "Sword", 2)} | " +
                                  $"{string.Join(" / ", three.Select(b => F1(FoundryBatch.Percentile(Fights(b.Runs).Select(f => f.Lifetime), 50))))} | " +
                                  $"{string.Join(" / ", three.Select(b => F1(FoundryBatch.Percentile(Fights(b.Runs).Select(f => f.DamageInReach).Where(d => !float.IsNaN(d)), 50))))} | " +
                                  $"{string.Join(" / ", three.Select(b => F(FoundryBatch.Percentile(b.Runs.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds), 50))))} | " +
                                  $"{string.Join(" / ", three.Select(b => b.Runs.Count(r => r.Cleared) + "/" + b.Runs.Length))} |");
                }
                md.AppendLine();
            }
            md.AppendLine("## The options").AppendLine();
            foreach (Option option in cells.Select(c => c.Option).Distinct())
                md.AppendLine($"- `{option.Key}`: {option.What}.");
            return md.ToString();
        }

        private static string Lead(IReadOnlyList<(string Name, double[] Jobs)> jobs, string name, int job)
        {
            double own = jobs.First(j => j.Name == name).Jobs[job];
            double best = jobs.Where(j => j.Name != name).Max(j => j.Jobs[job]);
            return best > 0 && !double.IsNaN(own) ? (100.0 * (own / best - 1.0)).ToString("0", CultureInfo.InvariantCulture) + " %" : "-";
        }

        private static string Row(Cell c)
        {
            DescentSimulation.FoundryResult[] down = c.Runs.Select(Three).Where(r => r != null).ToArray();
            DescentSimulation.FoundryResult[] died = down.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died).ToArray();
            int cleared = down.Count(r => r.Cleared);
            string rooms = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(Room).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Select(g => $"{g.Key} x{g.Count()}"));
            string killers = died.Length == 0 ? "-" : string.Join(", ", died.GroupBy(r => r.Killer ?? "?").OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).Take(4).Select(g => $"{g.Key} {g.Count()}"));
            DescentSimulation.EliteFight[] fights = Fights(down).ToArray();
            DescentSimulation.FoundryResult[] met = MetThornPost(down);
            string taken = met.Length == 0 ? "-"
                : $"{F1(met.Average(r => r.EliteTaken))} ({F1(met.Average(r => r.EliteTakenFromLeader))} / {F1(met.Average(r => r.EliteTakenFromEscort))} / {F1(met.Average(r => r.EliteTakenFromHorde))})";
            string windows = c.Levers.GloamCaptain || fights.Length == 0 ? "-"
                : $"{fights.Count(f => f.Windows <= 1)} / {fights.Count(f => f.Windows == 2)} / {fights.Count(f => f.Windows >= 3)} ({Pct(fights.Count(f => f.FellOpen), fights.Length)})" +
                  (c.Levers.BloomWindup > 0f ? $"; whiffs {F1(met.Length == 0 ? double.NaN : met.Average(r => r.BloomWhiffs))}" : string.Empty);
            float[] gate = down.Where(r => r.WardenSeconds >= 0f).Select(r => r.WardenSeconds).ToArray();
            int completed = c.Runs.Count(Completed);
            return $"| `{c.Option.Key}` | {c.Cohort} | {c.Hero} | {down.Length} | {cleared} ({Pct(cleared, down.Length)}) | {died.Length}: {rooms} | {died.Count(DiedAtThornPost)} | {killers} | " +
                   $"{F1(FoundryBatch.Percentile(fights.Select(f => f.Lifetime), 50))} / {F1(FoundryBatch.Percentile(fights.Select(f => f.Lifetime), 90))} | " +
                   $"{F1(FoundryBatch.Percentile(fights.Select(f => f.InReach), 50))} | {F1(FoundryBatch.Percentile(fights.Select(f => f.DamageInReach).Where(d => !float.IsNaN(d)), 50))} | " +
                   $"{taken} | {windows} | {F(FoundryBatch.Percentile(gate, 50))} ({Pct(gate.Count(s => s >= BandFrom && s <= BandTo), gate.Length)}) | " +
                   $"{F(FoundryBatch.Percentile(down.Where(r => r.Cleared).Select(r => r.FloorSeconds), 50))} | {completed} ({Pct(completed, c.Runs.Length)}) | " +
                   $"{M(FoundryBatch.Percentile(c.Runs.Where(Completed).Select(Descent), 50))} |";
        }

        private static string CreatureRow(Cell c)
        {
            DescentSimulation.FoundryResult[] down = c.Runs.Select(Three).Where(r => r != null).ToArray();
            string Mean(Func<DescentSimulation.FoundryResult, double> read) => down.Length == 0 ? "-" : down.Average(read).ToString("0.00", CultureInfo.InvariantCulture);
            float[] shortest = down.Where(r => r.ThornHits > 0).Select(r => r.ThornShortestHit).ToArray();
            return $"| `{c.Option.Key}` | {c.Cohort} | {c.Hero} | {Mean(r => r.PollenBlooms)} | {Mean(r => r.PollenBloomsInRooms)} | {Mean(r => r.TakenFromPollen)} | " +
                   $"{Mean(r => r.StillNearSeconds)} | {Mean(r => r.StillSeconds)} | {Mean(r => r.NearSeconds)} | {Mean(r => r.ThornRings)} | {Mean(r => r.ThornHits)} | {Mean(r => r.ThornHitsInLapse)} | {Mean(r => r.TakenFromThorns)} | " +
                   $"{Mean(r => r.ThornRingsCancelled)} | {Mean(r => r.ThornStuckLandings)} | {(shortest.Length == 0 ? "-" : shortest.Min().ToString("0.000", CultureInfo.InvariantCulture))} |";
        }

        private static string Room(DescentSimulation.FoundryResult run) =>
            run.DeathRoom < 1 ? "descent"
            : run.DeathRoom <= run.Kinds.Length && run.Kinds[run.DeathRoom - 1] == Cryptforge.Combat.RoomKind.Boss ? "boss"
            : "room " + run.DeathRoom.ToString(CultureInfo.InvariantCulture);

        private static string Csv(List<Cell> cells, string hands)
        {
            var csv = new StringBuilder();
            csv.AppendLine("realism,option,cohort,hero,seed,f1_outcome,f2_outcome,f3_outcome,f3_s,death_room,killer,elite_kind,elite_fight_s,elite_in_reach_s,elite_windows,elite_fell_open," +
                           "thorn_post_taken,taken_leader,taken_escort,taken_horde,bloom_closed_landings,bloom_open_landings,bloom_barked,bloom_whiffs,gate_s,finale_outcome,descent_s," +
                           "pollen_blooms,pollen_blooms_rooms,pollen_taken,still_near_s,thorn_rings,thorn_hits,thorn_hits_lapse,thorn_taken,thorn_cancelled,thorn_stuck_landings,pollen_clouds,in_cloud_s,still_in_cloud_s");
            foreach (Cell cell in cells)
            foreach (DescentSimulation.FoundryResult run in cell.Runs)
            {
                DescentSimulation.FoundryResult two = Two(run);
                DescentSimulation.FoundryResult t = Three(run);
                DescentSimulation.FoundryResult f = Finale(run);
                string head = string.Join(",", hands, cell.Option.Key, cell.Cohort, cell.Hero, run.Seed, run.Outcome, two != null ? two.Outcome.ToString() : string.Empty);
                if (t == null)
                {
                    csv.AppendLine(head + new string(',', 33));
                    continue;
                }
                DescentSimulation.EliteFight? fight = t.EliteFights != null && t.EliteFights.Count > 0 ? t.EliteFights[0] : (DescentSimulation.EliteFight?)null;
                csv.AppendLine(string.Join(",", head, t.Outcome, Num(t.FloorSeconds),
                    t.Outcome == DescentSimulation.FoundryOutcome.Died ? Room(t) : string.Empty, Quote(t.Killer),
                    fight?.Kind ?? string.Empty, fight != null ? Num(fight.Value.Lifetime) : string.Empty, fight != null ? Num(fight.Value.InReach) : string.Empty,
                    fight?.Windows.ToString(CultureInfo.InvariantCulture) ?? string.Empty, fight != null ? (fight.Value.FellOpen ? "1" : "0") : string.Empty,
                    Num(t.EliteTaken), Num(t.EliteTakenFromLeader), Num(t.EliteTakenFromEscort), Num(t.EliteTakenFromHorde),
                    t.BloomClosedLandings, t.BloomOpenLandings, Num(t.BloomBarked), t.BloomWhiffs, Num(t.WardenSeconds),
                    f != null ? (f.WalkedOut ? "WalkedOut" : f.Outcome.ToString()) : string.Empty, Completed(run) ? Num(Descent(run)) : string.Empty,
                    t.PollenBlooms, t.PollenBloomsInRooms, Num(t.TakenFromPollen), Num(t.StillNearSeconds), t.ThornRings, t.ThornHits,
                    t.ThornHitsInLapse, Num(t.TakenFromThorns), t.ThornRingsCancelled, t.ThornStuckLandings, t.PollenClouds,
                    Num(t.InCloudSeconds), Num(t.StillInCloudSeconds)));
            }
            return csv.ToString();
        }

        private static string Quote(string value) => value == null ? string.Empty : "\"" + value.Replace("\"", "\"\"") + "\"";

        private static string Num(float value) => value.ToString("0.00", CultureInfo.InvariantCulture);

        private static string F(double value) => double.IsNaN(value) ? "-" : value.ToString("0", CultureInfo.InvariantCulture);

        private static string F1(double value) => double.IsNaN(value) ? "-" : value.ToString("0.0", CultureInfo.InvariantCulture);

        private static string M(double seconds) => double.IsNaN(seconds) ? "-" : (seconds / 60.0).ToString("0.0", CultureInfo.InvariantCulture);

        private static string Pct(int part, int whole) =>
            whole == 0 ? "-" : (100.0 * part / whole).ToString("0", CultureInfo.InvariantCulture) + "%";

        private static string[] List(string name, string fallback) =>
            (Environment.GetEnvironmentVariable(name) ?? fallback).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).ToArray();

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
