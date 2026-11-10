using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/41 step 5's V0.1 gate (FoundryGate), the batch report's first section, on a two-seed batch: one row a criterion,
    // each with a status the gate knows, the section's table and summary saying the same, what nothing measured said to be
    // not measured, and the Editor's files and the floor's asset read where they are handed in. The numbers are not pinned:
    // the floor is being tuned, and the batch report reads it.
    public sealed class FoundryGateTests
    {
        private static readonly int[] Seeds = { 11, 33 };
        private FoundryBatch.Batch[] _heroes;
        private FoundryBatch.Batch[] _weapons;

        [OneTimeSetUp]
        public void RunTheBatches()
        {
            _heroes = new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() }
                .Select(hero => FoundryBatch.Run(hero, FoundryBatch.Shipped(), Seeds, 2)).ToArray();
            _weapons = FoundryBatch.RunWeapons(_heroes[0], Seeds, 2);
        }

        [Test]
        public void TheVanguardIsStartedWithEachWeaponItsOwnBatchReused()
        {
            Assert.That(_weapons.Select(b => b.Hero.Weapon.Id), Is.EqualTo(DescentSimulation.WeaponCatalogue().Select(w => w.Id)));
            Assert.That(_weapons.Count(b => ReferenceEquals(b, _heroes[0])), Is.EqualTo(1), "the Vanguard's Sword batch is its own");
            foreach (FoundryBatch.Batch b in _weapons)
            {
                Assert.That(b.Hero.Id, Is.EqualTo(FoundryGate.V01HeroId), b.Hero.Name);
                Assert.That(b.Runs.Select(r => r.Seed), Is.EqualTo(Seeds), b.Hero.Name);
                Assert.That(b.Runs.All(r => r.WeaponWork[0].Id == b.Hero.Weapon.Id), Is.True, b.Hero.Name + " starts with it");
            }
        }

        [Test]
        public void TheSectionRendersOneRowACriterionEachWithAStatus()
        {
            List<FoundryGate.Row> rows = FoundryGate.Rows(_heroes, _weapons, null);
            Assert.That(rows.Select(r => r.Criterion), Is.EqualTo(FoundryGate.Criteria));
            foreach (FoundryGate.Row row in rows)
            {
                Assert.That(FoundryGate.Statuses, Does.Contain(row.Status), row.Criterion);
                Assert.That(new[] { row.Target, row.Simulation, row.Editor, row.Basis }, Has.All.Not.Null.And.Not.Empty, row.Criterion);
            }

            string section = FoundryGate.Section(_heroes, _weapons, null);
            Assert.That(section, Does.StartWith("## V0.1 gate"));
            Dictionary<string, string[]> table = Table(section);
            Assert.That(table.Keys, Is.EquivalentTo(FoundryGate.Criteria), "one table row a criterion");
            foreach (FoundryGate.Row row in rows)
            {
                string[] cells = table[row.Criterion];
                Assert.That(cells.Length, Is.EqualTo(6), row.Criterion);
                Assert.That(cells[4].Trim('*'), Is.EqualTo(row.Status), row.Criterion + ": the table's status is the row's");
            }

            // The summary counts every row once, under its status.
            Match summary = Regex.Match(section, @"\*\*Where V0\.1 stands[^*]*\*\* met (\d+).*; not met (\d+).*; not measured (\d+).*; reported (\d+)");
            Assert.That(summary.Success, Is.True, "the summary line");
            int[] counts = Enumerable.Range(1, 4).Select(i => int.Parse(summary.Groups[i].Value)).ToArray();
            Assert.That(counts, Is.EqualTo(new[] { FoundryGate.Met, FoundryGate.NotMet, FoundryGate.NotMeasured, FoundryGate.Reported }
                .Select(status => rows.Count(r => r.Status == status))));
            Assert.That(section, Does.Contain("### Weapons: the Vanguard started with each"));
            Assert.That(section, Does.Contain("### Evolutions"));
        }

        // With nothing but the batch, what only the Editor, the device, the owner or the floor's asset can say is not
        // measured, and the Editor's column says it has no file.
        [Test]
        public void WhatNothingMeasuredIsNotMeasured()
        {
            Dictionary<string, FoundryGate.Row> rows = FoundryGate.Rows(_heroes, _weapons, null).ToDictionary(r => r.Criterion);
            foreach (string criterion in new[] { FoundryGate.OwnersFloor, FoundryGate.DeviceFrames, FoundryGate.Extract })
                Assert.That(rows[criterion].Status, Is.EqualTo(FoundryGate.NotMeasured), criterion);
            foreach (string criterion in new[] { FoundryGate.GateWait, FoundryGate.WardenCost, FoundryGate.Clears, FoundryGate.Overrun })
                Assert.That(rows[criterion].Status, Is.EqualTo(FoundryGate.Reported), criterion + ": no target, a number");
            Assert.That(rows[FoundryGate.FloorTime].Editor, Is.EqualTo("no Editor file"));
            Assert.That(rows[FoundryGate.DeviceFrames].Editor, Is.EqualTo("no perf probe file"));

            // Without every weapon's batch the roles cannot be read.
            FoundryGate.Row roles = FoundryGate.Rows(_heroes, _weapons.Take(2).ToArray(), null).Single(r => r.Criterion == FoundryGate.WeaponRoles);
            Assert.That(roles.Status, Is.EqualTo(FoundryGate.NotMeasured));
            // With all three, each weapon is named with its three efficiencies.
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
                Assert.That(rows[FoundryGate.WeaponRoles].Simulation, Does.Contain(weapon.DisplayName + " horde ")
                    .And.Contain(", elites ").And.Contain(", Warden "));
            Assert.That(rows[FoundryGate.WeaponRoles].Basis, Does.Contain("elites, on the elite rooms' waves"));
        }

        // The roles row's reading (docs/41, status 2026-11-10 night): three jobs - the horde, the elites, the Warden - and a
        // weapon's role is a job on which it leads every other weapon by more than RoleMargin; within the margin is a tie.
        // Constructed numbers in the shape of the 2026-11-10 night batch.
        [Test]
        public void AWeaponsRoleIsAJobItLeadsEveryOtherOnByMoreThanTheMargin()
        {
            Assert.That(FoundryGate.JobNames, Is.EqualTo(new[] { "horde", "elites", "Warden" }));
            double[] staff = { 31, 10, 23 }, sword = { 8, 3, 27 }, daggers = { 2, 12.5, 0 };
            Assert.That(FoundryGate.LeadsJob(staff, new[] { sword, daggers }), Is.EqualTo(0), "the Staff leads the horde");
            Assert.That(FoundryGate.LeadsJob(daggers, new[] { staff, sword }), Is.EqualTo(1), "12.5 is more than 10 and a tenth");
            Assert.That(FoundryGate.LeadsJob(sword, new[] { staff, daggers }), Is.EqualTo(2), "the Sword leads the Warden");
            double[] tied = { 2, 11, 0 };
            Assert.That(FoundryGate.LeadsJob(tied, new[] { staff, sword }), Is.EqualTo(-1), "11 is within a tenth of 10");
            Assert.That(FoundryGate.LeadsJob(new[] { double.NaN, double.NaN, double.NaN }, new[] { staff, sword }), Is.EqualTo(-1),
                "nothing measured leads nothing");
            Assert.That(FoundryGate.LeadsJob(new double[] { 40, 40, 40 }, new[] { staff, sword }), Is.EqualTo(0),
                "a weapon ahead on every job has the first");
        }

        // An Editor file, the perf probe and the floor's asset handed in read in their rows; a file of another floor is
        // named so, and the device still is not measured.
        [Test]
        public void TheEditorsFilesAndTheFloorsAssetReadInTheirRows()
        {
            var evidence = new FoundryGate.Evidence
            {
                FloorRead = true, FloorAsset = "Floor_EmberHalls", NextFloor = "Floor_QuicksilverVaults",
                Frames = new FoundryGate.EditorFrames
                {
                    Source = "perf-probe-test.txt", Machine = "test machine",
                    Windows = { new FoundryGate.FrameWindow { Name = "Vanguard / horde", Frames = 1783, AliveMax = 40, P95 = 5.9f, Max = 31.7f, Over16 = 1, Over33 = 0 } }
                }
            };
            evidence.Files.Add(new FoundryGate.EditorFile
            {
                Source = "horde-pacing-test-vanguard.txt", HeroId = FoundryGate.V01HeroId,
                Seeds =
                {
                    new FoundryGate.EditorSeed { Seed = 11, Cleared = true, Floor = 280f, Uptime = .96f, Engaged = .68f, Boss = 51f, Clock = 258f, LastRoom = 7, Evolutions = 1, GateWait = 110f },
                    new FoundryGate.EditorSeed { Seed = 44, Cleared = false, Floor = 88f, Uptime = 1f, Engaged = .87f, LastRoom = 3, Evolutions = 0 }
                }
            });
            evidence.Files.Add(new FoundryGate.EditorFile
            {
                Source = "horde-pacing-test-ilya.txt", HeroId = "hero_cinder_arcanist", OtherFloor = "it names no floor levers",
                Seeds = { new FoundryGate.EditorSeed { Seed = 11, Cleared = true, Floor = 269f, Uptime = .97f, Engaged = .54f, Boss = 36f, Clock = 243f, LastRoom = 7 } }
            });
            evidence.Files.Add(new FoundryGate.EditorFile
            {
                Source = "horde-pacing-test-staff.txt", HeroId = FoundryGate.V01HeroId, WeaponId = "weapon_staff",
                Seeds = { new FoundryGate.EditorSeed { Seed = 11, Cleared = true, Floor = 160f, Boss = 43f, LastRoom = 6 } }
            });

            Dictionary<string, FoundryGate.Row> rows = FoundryGate.Rows(_heroes, _weapons, evidence).ToDictionary(r => r.Criterion);
            Assert.That(rows[FoundryGate.FloorTime].Editor, Does.Contain("Vanguard 184 s (2 seeds), clears' 280 s").And.Contain("Ilya 269 s (1 seed)"));
            Assert.That(rows[FoundryGate.WardenFight].Editor, Does.Contain("Vanguard 51 s (1 felled)"));
            Assert.That(rows[FoundryGate.GateWait].Editor, Does.Contain("Vanguard P50 110 s of 1 clear")
                .And.Contain("Ilya not in this file's telemetry"));
            Assert.That(rows[FoundryGate.Synergy].Editor, Does.Contain("Vanguard 1 of 2 seeds evolved (1 of 1 clear)")
                .And.Contain("Ilya no telemetry of this file's runs"));
            Assert.That(rows[FoundryGate.Clears].Editor, Does.Contain("Vanguard cleared 1 of 2; died 1: halls 1"), "the simulated floor's rooms by kind");
            Assert.That(rows[FoundryGate.Overrun].Editor, Does.Contain("Vanguard P50 258 / P90 258 s; past 300 s 0 of 1 clear"));
            Assert.That(rows[FoundryGate.WeaponRoles].Editor, Does.Contain("Staff start (horde-pacing-test-staff.txt): 1/1 cleared, floor P50 160 s, Warden P50 43 s"));
            Assert.That(rows[FoundryGate.Extract].Status, Is.EqualTo(FoundryGate.Met));
            Assert.That(rows[FoundryGate.Extract].Basis, Does.Contain("Floor_EmberHalls leads to Floor_QuicksilverVaults"));
            Assert.That(rows[FoundryGate.DeviceFrames].Status, Is.EqualTo(FoundryGate.NotMeasured), "the Editor's frames are not the S23's");
            Assert.That(rows[FoundryGate.DeviceFrames].Editor, Does.Contain("p95 5.9 ms, max 31.7 ms").And.Contain("perf-probe-test.txt: test machine"));

            string section = FoundryGate.Section(_heroes, _weapons, evidence);
            Assert.That(section, Does.Contain("Vanguard: horde-pacing-test-vanguard.txt, 2 seeds - the simulated floor's levers"));
            Assert.That(section, Does.Contain("Ilya: horde-pacing-test-ilya.txt, 1 seed - **another floor**: it names no floor levers"));

            // A floor that leads nowhere offers no Extract.
            evidence.NextFloor = null;
            Assert.That(FoundryGate.Rows(_heroes, _weapons, evidence).Single(r => r.Criterion == FoundryGate.Extract).Status,
                Is.EqualTo(FoundryGate.NotMet));
        }

        // Where a run died, as the clears row and the Warden's cost row read it (constructed runs): a hero felled while the
        // cleared room before the gate held for the floor's clock died to the gate's wait, not to that room's elite; and a
        // hero burnt by the eruption that fells the Warden too died at the Warden in both rows, though the Warden fell.
        [Test]
        public void DeathsAreReadByWhereTheHeroStoodTheSameInEveryRow()
        {
            RoomKind[] kinds = { RoomKind.Combat, RoomKind.Elite, RoomKind.Boss };
            DescentSimulation.FoundryResult Run(int seed, DescentSimulation.FoundryOutcome outcome) => new DescentSimulation.FoundryResult
            {
                Seed = seed, Hero = FoundryGate.V01HeroId, Outcome = outcome, Kinds = kinds, FloorSeconds = 250f,
                RoomSeconds = new[] { 60f, 150f, 40f }, WalkSeconds = new[] { 5f, 5f, -1f }, WaveSeconds = new[] { 50f, 30f, -1f }
            };

            DescentSimulation.FoundryResult inTheWait = Run(1, DescentSimulation.FoundryOutcome.Died);
            inTheWait.GateWaitSeconds = 20f;
            inTheWait.DeathRoom = 2;
            inTheWait.DeathPhase = DescentSimulation.GateWaitPhase;
            DescentSimulation.FoundryResult atTheElite = Run(2, DescentSimulation.FoundryOutcome.Died);
            atTheElite.DeathRoom = 2;
            atTheElite.DeathPhase = "wave 1";
            DescentSimulation.FoundryResult together = Run(3, DescentSimulation.FoundryOutcome.Died);
            together.LevelAtGate = 20;
            together.BossSeconds = 55f;
            together.WardenSeconds = 55f;
            together.DeathRoom = 3;
            together.DeathPhase = "boss";
            DescentSimulation.FoundryResult cleared = Run(4, DescentSimulation.FoundryOutcome.Cleared);
            cleared.LevelAtGate = 20;
            cleared.BossSeconds = 50f;
            cleared.WardenSeconds = 50f;
            var batch = new FoundryBatch.Batch
            {
                Hero = DescentSimulation.Vanguard(), Levers = FoundryBatch.Shipped(),
                Runs = new[] { inTheWait, atTheElite, together, cleared }
            };

            Assert.That(together.DiedAtWarden, Is.True, "felled by the eruption that felled the Warden");
            Assert.That(inTheWait.DiedAtWarden || atTheElite.DiedAtWarden || cleared.DiedAtWarden, Is.False);
            Dictionary<string, FoundryGate.Row> rows = FoundryGate.Rows(new[] { batch }, null, null).ToDictionary(r => r.Criterion);
            string clears = rows[FoundryGate.Clears].Simulation;
            Assert.That(clears, Does.Contain("died 3: "));
            Assert.That(clears, Does.Contain("the gate's wait 1"));
            Assert.That(clears, Does.Contain("elites 1"));
            Assert.That(clears, Does.Contain("Warden 1"));
            Assert.That(rows[FoundryGate.WardenCost].Simulation, Does.Contain("died there 1 of 2"));
        }

        // A death while the gate holds is recorded as the gate's wait's (DescentSimulation.GateWaitPhase): with the gate
        // held to the floor's limit, the Vanguard falls in the wait, and no death after the wait began and before the Warden
        // walked out is read as its room's.
        [Test]
        public void TheSimulationRecordsADeathInTheGatesWaitAsTheWaits()
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("gate held to the limit");
            levers.GateNotBeforeSeconds = levers.LimitSeconds;
            FoundryBatch.Batch batch = FoundryBatch.Run(DescentSimulation.Vanguard(), levers, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, 2);
            DescentSimulation.FoundryResult[] waited = batch.Runs.Where(r => r.Outcome == DescentSimulation.FoundryOutcome.Died &&
                                                                             r.GateWaitSeconds > 0f && r.LevelAtGate < 0).ToArray();
            Assert.That(waited, Is.Not.Empty, "a hero held at the gate for the floor's limit falls in the wait");
            foreach (DescentSimulation.FoundryResult r in waited)
                Assert.That(r.DeathPhase, Is.EqualTo(DescentSimulation.GateWaitPhase), "seed " + r.Seed);
        }

        // The section's gate table, by its first cell: each row's cells between the unescaped pipes.
        private static Dictionary<string, string[]> Table(string section)
        {
            var rows = new Dictionary<string, string[]>();
            bool inTable = false;
            foreach (string line in section.Split('\n').Select(l => l.TrimEnd('\r')))
            {
                if (line.StartsWith("| Criterion |"))
                {
                    inTable = true;
                    continue;
                }
                if (!inTable || line.StartsWith("|---"))
                    continue;
                if (!line.StartsWith("|"))
                    break;
                string[] cells = Regex.Split(line.Substring(1, line.Length - 2), @"(?<!\\)\|").Select(c => c.Trim()).ToArray();
                rows[cells[0]] = cells;
            }
            return rows;
        }
    }
}
