using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Editor's Descent rows (HordePacingMeasurement with TestResults/horde-floors.txt "3"): EditorDescent reads them for
    // the finale's report, and the floor-1 reader the gate and the parity use (FoundryBatchReport.ReadEditor) skips them, so
    // a file measured down the whole Descent still reads as floor 1's. The sample is a file as the measurement writes it,
    // the floor-1 rows from a Turkish Editor (decimal commas), the descent rows invariant.
    public sealed class EditorDescentTests
    {
        // HordePacingMeasurement.DescentSeed.Header's names.
        private const string Header = "descent\tseed\tresult\tdied_at\tfloor1_s\tfloor2_s\tfinale_s\tdescent_s\twarden1_s\twarden2_s\t" +
            "level_gate1\tlevel_gate2\tuptime1\tengaged1\tuptime2\tengaged2\tuptime3\tengaged3\thold2_s\tclock2\t" +
            "finale_hp\tfinale_max_hp\tfinale_level\tchest_s\tswarm_s\tlevel\thp\tmax_hp\tkills2\tkills3\trooms2";

        private static readonly string[] Sample =
        {
            "mode on floors 3",
            "levers floor floor_ember_halls hall_surges 2 extra_elites 1 gate_s 210",
            "seed\tresult\tfloor_s\tuptime\tengaged_strike\twave_kills\thorde_kills\tlevel\thp\tmax_hp\thorde_avg\thorde_max\tslams\tslam_hits\tboss_s\tboss_left\tclock\trooms",
            "11\tcleared\t304\t0,93\t0,88\t65\t720\t31\t165\t190\t7,3\t18\t12\t0\t63\t0,01\t270\t1:15s 2:40s 3:15s 4:1s 5:12s 6:137s 7:83s\tslam_d 2,8 2,8 ",
            "22\tcleared\t308\t0,94\t0,84\t65\t749\t32\t193\t210\t6,1\t18\t13\t0\t66\t0,00\t274\t1:14s 2:38s 3:18s 4:1s 5:15s 6:136s 7:86s\tslam_d 2,8 ",
            "33\tdied\t251\t0,95\t0,90\t60\t600\t28\t0\t100\t7,4\t18\t0\t0\t0\t-\t251\t1:23s 2:37s 3:17s 4:1s 5:14s 6:159s\tslam_d ",
            Header,
            "descent\t11\twalked_out\t-\t304\t271\t65\t640\t63\t52\t31\t44\t0.93\t0.88\t0.95\t0.90\t1.00\t0.97\t31\t210\t96\t260\t52\t0.0\t62.5\t55\t240\t280\t812\t160\t1:20s 2:38s 3:22s 4:1s 5:18s 6:120s",
            "descent\t22\tdied\t2.6:boss\t308\t250\t-\t-\t66\t40\t32\t45\t0.94\t0.84\t0.96\t0,91\t-\t-\t28\t209\t-\t-\t-\t-\t-\t46\t0\t270\t700\t-\t1:19s 2:40s 3:20s 4:1s 5:16s 6:150s",
            "descent\t33\tdied\t1.6:gate wait\t251\t-\t-\t-\t-\t-\t-\t-\t0.95\t0.90\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t-\t28\t0\t100\t-\t-\t-"
        };

        [Test]
        public void TheDescentRowsReadAsTheMeasurementWroteThem()
        {
            List<EditorDescent.Run> runs = EditorDescent.Read(Sample);
            Assert.That(runs.Select(r => r.Seed), Is.EqualTo(new[] { 11, 22, 33 }));

            EditorDescent.Run walked = runs[0];
            Assert.That(walked.WalkedOut, Is.True);
            Assert.That(walked.Died, Is.False);
            Assert.That(walked.DiedAt, Is.Null);
            Assert.That(walked.ClearedFloor1 && walked.ClearedFloor2, Is.True);
            Assert.That(walked.Floor1, Is.EqualTo(304f));
            Assert.That(walked.Floor2, Is.EqualTo(271f));
            Assert.That(walked.Finale, Is.EqualTo(65f));
            Assert.That(walked.Descent, Is.EqualTo(640f));
            Assert.That(walked.Warden1, Is.EqualTo(63f));
            Assert.That(walked.Warden2, Is.EqualTo(52f));
            Assert.That(walked.LevelGate1, Is.EqualTo(31));
            Assert.That(walked.LevelGate2, Is.EqualTo(44));
            Assert.That(walked.Engaged2, Is.EqualTo(.90f).Within(1e-4f));
            Assert.That(walked.Engaged3, Is.EqualTo(.97f).Within(1e-4f));
            Assert.That(walked.Hold2, Is.EqualTo(31f));
            Assert.That(walked.Clock2, Is.EqualTo(210f));
            Assert.That(walked.FinaleHealth, Is.EqualTo(96f));
            Assert.That(walked.FinaleMaximum, Is.EqualTo(260f));
            Assert.That(walked.FinaleLevel, Is.EqualTo(52));
            Assert.That(walked.ChestOpened, Is.True);
            Assert.That(walked.Chest, Is.EqualTo(0f));
            Assert.That(walked.Swarm, Is.EqualTo(62.5f).Within(1e-4f));
            Assert.That(walked.Kills2, Is.EqualTo(812));
            Assert.That(walked.Kills3, Is.EqualTo(160));
            Assert.That(walked.Rooms2, Is.EqualTo("1:20s 2:38s 3:22s 4:1s 5:18s 6:120s"));

            // Fell to floor 2's Warden: floor 2 reached, the finale not; a decimal comma reads as a point.
            EditorDescent.Run boss = runs[1];
            Assert.That(boss.Died, Is.True);
            Assert.That(boss.DiedAt, Is.EqualTo("2.6:boss"));
            Assert.That(boss.ClearedFloor1, Is.True);
            Assert.That(boss.ClearedFloor2, Is.False);
            Assert.That(float.IsNaN(boss.Descent), Is.True);
            Assert.That(boss.Engaged2, Is.EqualTo(.91f).Within(1e-4f));
            Assert.That(float.IsNaN(boss.Engaged3), Is.True);
            Assert.That(boss.ChestOpened, Is.False);
            Assert.That(boss.FinaleLevel, Is.EqualTo(-1));
            Assert.That(boss.Kills3, Is.EqualTo(-1));

            // Fell on floor 1, waiting for its gate: nothing below it.
            EditorDescent.Run first = runs[2];
            Assert.That(first.DiedAt, Is.EqualTo("1.6:gate wait"));
            Assert.That(first.ClearedFloor1, Is.False);
            Assert.That(float.IsNaN(first.Floor2), Is.True);
            Assert.That(first.LevelGate1, Is.EqualTo(-1));
            Assert.That(first.Rooms2, Is.Null);
        }

        [Test]
        public void AFloorOneReaderSkipsTheDescentRows()
        {
            string folder = Path.Combine(Path.GetTempPath(), "cryptforge-editor-descent-" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try
            {
                string path = Path.Combine(folder, "horde-pacing-descent.txt");
                File.WriteAllLines(path, Sample);
                List<FoundryBatchReport.EditorRun> runs = FoundryBatchReport.ReadEditor(folder, path, "hero_vanguard");
                Assert.That(runs.Select(r => r.Seed), Is.EqualTo(new[] { 11, 22, 33 }), "the floor-1 rows, and only those");
                Assert.That(runs.Select(r => r.Cleared), Is.EqualTo(new[] { true, true, false }));
                Assert.That(runs[0].Floor, Is.EqualTo(304f));
                Assert.That(runs[0].Engaged, Is.EqualTo(.88f).Within(1e-4f));
                Assert.That(runs[2].LastRoom, Is.EqualTo(6));
                (string mode, string hero, string weapon, string cohort) = FoundryBatchReport.PacingHeader(path);
                Assert.That(mode, Is.EqualTo("on"), "\"floors 3\" leaves the mode line the plain autopilot's");
                Assert.That(hero, Is.EqualTo(FoundryGate.V01HeroId));
                Assert.That(weapon, Is.Null);
                Assert.That(cohort, Is.Null);
                Assert.That(EditorDescent.Read(path).Count, Is.EqualTo(3));
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void AFileWithoutDescentRowsHasNone()
        {
            Assert.That(EditorDescent.Read(Sample.Take(6)), Is.Empty);
            Assert.That(EditorDescent.Read((string)null), Is.Empty);
        }

        [Test]
        public void TheSectionLaysTheEditorsRowsOut()
        {
            string section = EditorDescent.Section(new[]
            {
                new EditorDescent.HeroRows { Hero = "Vanguard", Source = "horde-pacing-descent.txt", Editor = EditorDescent.Read(Sample) }
            });
            Assert.That(section, Does.Contain("| Vanguard | Editor (horde-pacing-descent.txt) | 3 | 2 (67%) | 1 (33%) | 1 (33%) | 1.6:gate wait x1, 2.6:boss x1 | 304 |"));
            Assert.That(section, Does.Contain("| 640 (10.7) |"), "the Descent's P50 over the walk-outs, and in minutes");
            Assert.That(section, Does.Contain("| Vanguard | 22 | died 2.6:boss, 308 / 250 / -, -, 66 / 40, L32 / L45, - / - L-, - | - |"));
        }
    }
}
