using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // A measurement, not a check (docs/41 step 2): the development autopilot plays the foundry's first floor with the
    // horde on, over several seeds, and writes how long the floor took, how much of it had an enemy standing (combat
    // uptime), how much of it the hero was actually engaged (a strike of any weapon it holds landed, or a hit taken, in the
    // last 1.5 s: the engaged_strike column, since 2026-11-11; the engaged column before it read an enemy within the first
    // weapon's reach plus a unit), what it killed, how the Warden's fight went and what state the hero reached the gate in to
    // TestResults/horde-pacing.txt (its header names the floor and its foundry levers: surges, extra elites, the gate's
    // time, and the autopilot's cohort where horde-cohort.txt names one), and keeps each seed's telemetry under
    // TestResults/horde-telemetry/<hero>-<seed>/ (<hero>-<cohort>-<seed>/ for a cohort's run)
    // (the v01 research's floor_breakdown.py splits it into walk, node fights, Warden and exit). Every frame is a fixed 1/60 s of game time (Time.captureDeltaTime), as fast as the
    // Editor can run it, so a seed replays the same floor every time - a raised time scale made the same build clear a
    // seed in one run and die on it in the next. Explicit, so only a run that names it plays it; it asserts nothing about
    // balance.
    [Explicit("Measurement: run it by name.")]
    public sealed class HordePacingMeasurement
    {
        private const float Step = 1f / 60f;
        private const float FloorLimitSeconds = 900f;
        // Engaged: a strike of any weapon the hero holds landed (AttackController.StruckWith), or a hit taken, this recently
        // (DescentSimulation.EngagedSeconds).
        private const float EngagedSeconds = 1.5f;
        private static readonly int[] DefaultSeeds = { 11, 22, 33, 44, 55, 66 };

        // TestResults/horde-linger.txt, one word, plays the autopilot's measurement mode instead of the floor: "linger"
        // farms the walk after the first hall, "idle" stands there (DevelopmentAutopilot; the floor's clock, docs/21).
        private static string Mode()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-linger.txt");
            string mode = File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
            return mode.Length > 0 ? mode : "on";
        }

        // TestResults/horde-hero.txt, a hero id, plays that hero instead of the default (docs/45).
        private static string Hero()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-hero.txt");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }

        // TestResults/horde-weapon.txt, a weapon id, starts the run with that weapon owned and equipped (the research's M1:
        // the Vanguard with the Staff, to tell the Staff's share from Ilya's Ember Memory).
        private static string Weapon()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-weapon.txt");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }

        // TestResults/horde-cohort.txt, a cohort's name (AutopilotCohort: standing, first-card, clumsy, auto-farmer), plays
        // the autopilot as that cohort instead of the careful pilot (docs/41 step 5); written after the mode into
        // development/autopilot.txt, and into the header, so the batch report reads the file as that cohort's.
        private static string Cohort()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-cohort.txt");
            return File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
        }

        // TestResults/horde-seeds.txt, one seed per line, narrows a run to the seeds being looked at.
        private static int[] Seeds()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-seeds.txt");
            if (!File.Exists(path))
                return DefaultSeeds;
            var seeds = new System.Collections.Generic.List<int>();
            foreach (string line in File.ReadAllLines(path))
                if (int.TryParse(line.Trim(), out int seed))
                    seeds.Add(seed);
            return seeds.Count > 0 ? seeds.ToArray() : DefaultSeeds;
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Pacing cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(3600000)] public IEnumerator TheAutopilotPlaysTheFirstFloorWithTheHorde()
        {
            var report = new StringBuilder();
            string mode = Mode();
            string heroId = Hero();
            string weaponId = Weapon();
            string cohort = Cohort();
            Assert.That(cohort.Length == 0 || AutopilotCohort.TryNamed(cohort, out _), Is.True,
                $"horde-cohort.txt names no cohort: '{cohort}'");
            report.AppendLine("mode " + mode + (heroId.Length > 0 ? " hero " + heroId : string.Empty) +
                (weaponId.Length > 0 ? " weapon " + weaponId : string.Empty) + (cohort.Length > 0 ? " cohort " + cohort : string.Empty));
            bool headed = false;
            foreach (int seed in Seeds())
            {
                TestProfile.Begin(weaponId.Length > 0
                    ? new PlayerProfile(0, null, null, 0, new[] { weaponId }, weaponId)
                    : null);
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
                File.WriteAllText(DevelopmentStart.AutopilotPath, mode + (cohort.Length > 0 ? " " + cohort : string.Empty));
                if (heroId.Length > 0)
                    File.WriteAllText(DevelopmentStart.HeroPath, heroId);
                File.WriteAllText(DevelopmentStart.RunSeedPath, seed.ToString());
                Time.captureDeltaTime = Step;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
                yield return null;
                var setup = Object.FindFirstObjectByType<CombatSetup>();
                var encounters = setup.Encounters;
                // The floor's own foundry levers, once, before the columns: the batch report holds them beside the
                // simulation's (FoundryBatchReport's parity), so a file measured on another floor is not read as this one.
                if (!headed)
                {
                    headed = true;
                    FloorDefinition floor = encounters.Floor;
                    report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "levers floor {0} hall_surges {1} extra_elites {2} gate_s {3:0.###}", floor.Id, floor.FoundryHallSurges,
                        floor.FoundryExtraElites, floor.FoundryGateNotBeforeSeconds));
                    report.AppendLine("seed\tresult\tfloor_s\tuptime\tengaged_strike\twave_kills\thorde_kills\tlevel\thp\tmax_hp\thorde_avg\thorde_max\tslams\tslam_hits\tboss_s\tboss_left\tclock\trooms");
                }
                var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
                bool cleared = false;
                int waveKills = 0, hordeKills = 0, slams = 0, slamHits = 0;
                float bossLeft = -1f, bossSeconds = 0f;
                encounters.FloorCleared += () => cleared = true;
                var slamDistances = new StringBuilder();
                setup.Warden.Slammed += hit =>
                {
                    slams++;
                    if (hit) slamHits++;
                    slamDistances.Append(setup.Warden.LastSlamDistance.ToString("0.0")).Append(' ');
                };
                encounters.EnemyDefeated += enemy =>
                {
                    bool horde = false;
                    for (int i = 0; i < encounters.HordeEnemyCount && !horde; i++)
                        horde = ReferenceEquals(encounters.HordeEnemyAt(i), enemy);
                    if (horde)
                        hordeKills++;
                    else
                        waveKills++;
                };
                float lastHit = float.NegativeInfinity, lastStrike = float.NegativeInfinity, engaged = 0f;
                hero.Damaged += context => lastHit = Time.time;
                AttackController heroAttack = hero.GetComponent<AttackController>();
                if (heroAttack != null)
                    heroAttack.StruckWith += (target, weapon) => lastStrike = Time.time;
                float start = Time.time, fighting = 0f, active = 0f, hordeSum = 0f;
                int hordeMax = 0, frames = 0;
                float clock = 0f;
                var rooms = new StringBuilder();
                int room = encounters.RoomNumber;
                float roomStart = Time.time;
                while (!cleared && !setup.Run.HasEnded && Time.time - start < FloorLimitSeconds)
                {
                    yield return null;
                    if (Time.deltaTime <= 0f)
                        continue;
                    active += Time.deltaTime;
                    if (encounters.AllAliveCount > 0)
                        fighting += Time.deltaTime;
                    if (Time.time - lastHit <= EngagedSeconds || Time.time - lastStrike <= EngagedSeconds)
                        engaged += Time.deltaTime;
                    hordeSum += encounters.HordeAliveCount;
                    hordeMax = Mathf.Max(hordeMax, encounters.HordeAliveCount);
                    clock = Mathf.Max(clock, encounters.FloorClockSeconds);
                    frames++;
                    if (setup.Warden.Boss != null)
                    {
                        bossSeconds += Time.deltaTime;
                        bossLeft = setup.Warden.Boss.Current / setup.Warden.Boss.Maximum;
                    }
                    if (encounters.RoomNumber != room)
                    {
                        rooms.Append($"{room}:{Time.time - roomStart:0}s ");
                        room = encounters.RoomNumber;
                        roomStart = Time.time;
                    }
                }
                rooms.Append($"{room}:{Time.time - roomStart:0}s");
                string result = cleared ? "cleared" : setup.Run.HasEnded ? "died" : "timeout";
                report.AppendLine(string.Join("\t", seed, result, (Time.time - start).ToString("0"),
                    (active > 0 ? fighting / active : 0).ToString("0.00"), (active > 0 ? engaged / active : 0).ToString("0.00"), waveKills, hordeKills, setup.Run.Level,
                    hero.Current.ToString("0"), hero.Maximum.ToString("0"),
                    (frames > 0 ? hordeSum / frames : 0).ToString("0.0"), hordeMax, slams, slamHits,
                    bossSeconds.ToString("0"), bossLeft < 0 ? "-" : bossLeft.ToString("0.00"), clock.ToString("0"), rooms,
                    "slam_d " + slamDistances));
                Time.captureDeltaTime = 0f;
                var old = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.CreateScene("Pacing between seeds " + seed));
                yield return SceneManager.UnloadSceneAsync(old);
                // The unload has flushed the run's last events; TestProfile.End deletes the folder.
                string telemetry = Path.Combine(TestProfile.Folder, "telemetry");
                if (Directory.Exists(telemetry))
                {
                    // A cohort's run keeps its own folder, so the careful pilot's telemetry the parity reads stays.
                    string kept = Path.Combine(Application.dataPath, "..", "TestResults", "horde-telemetry",
                        (heroId.Length > 0 ? heroId : "hero_vanguard") + (cohort.Length > 0 ? "-" + cohort : string.Empty) + "-" + seed);
                    if (Directory.Exists(kept))
                        Directory.Delete(kept, true);
                    Directory.CreateDirectory(kept);
                    foreach (string file in Directory.GetFiles(telemetry))
                        File.Copy(file, Path.Combine(kept, Path.GetFileName(file)));
                }
                TestProfile.End();
            }
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-pacing.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToString());
            TestProfile.Begin();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
        }
    }
}
