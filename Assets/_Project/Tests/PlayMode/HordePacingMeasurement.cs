using System.Collections;
using System.IO;
using System.Text;
using Cryptforge.Combat;
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
    // uptime), what it killed, how the Warden's fight went and what state the hero reached the gate in to
    // TestResults/horde-pacing.txt. Every frame is a fixed 1/60 s of game time (Time.captureDeltaTime), as fast as the
    // Editor can run it, so a seed replays the same floor every time - a raised time scale made the same build clear a
    // seed in one run and die on it in the next. Explicit, so only a run that names it plays it; it asserts nothing about
    // balance.
    [Explicit("Measurement: run it by name.")]
    public sealed class HordePacingMeasurement
    {
        private const float Step = 1f / 60f;
        private const float FloorLimitSeconds = 900f;
        private static readonly int[] DefaultSeeds = { 11, 22, 33, 44, 55, 66 };

        // TestResults/horde-linger.txt, one word, plays the autopilot's measurement mode instead of the floor: "linger"
        // farms the walk after the first hall, "idle" stands there (DevelopmentAutopilot; the floor's clock, docs/21).
        private static string Mode()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-linger.txt");
            string mode = File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
            return mode.Length > 0 ? mode : "on";
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
            report.AppendLine("mode " + mode);
            report.AppendLine("seed\tresult\tfloor_s\tuptime\twave_kills\thorde_kills\tlevel\thp\tmax_hp\thorde_avg\thorde_max\tslams\tslam_hits\tboss_s\tboss_left\tclock\trooms");
            foreach (int seed in Seeds())
            {
                TestProfile.Begin();
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
                File.WriteAllText(DevelopmentStart.AutopilotPath, mode);
                File.WriteAllText(DevelopmentStart.RunSeedPath, seed.ToString());
                Time.captureDeltaTime = Step;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
                yield return null;
                var setup = Object.FindFirstObjectByType<CombatSetup>();
                var encounters = setup.Encounters;
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
                    (active > 0 ? fighting / active : 0).ToString("0.00"), waveKills, hordeKills, setup.Run.Level,
                    hero.Current.ToString("0"), hero.Maximum.ToString("0"),
                    (frames > 0 ? hordeSum / frames : 0).ToString("0.0"), hordeMax, slams, slamHits,
                    bossSeconds.ToString("0"), bossLeft < 0 ? "-" : bossLeft.ToString("0.00"), clock.ToString("0"), rooms,
                    "slam_d " + slamDistances));
                Time.captureDeltaTime = 0f;
                var old = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.CreateScene("Pacing between seeds " + seed));
                yield return SceneManager.UnloadSceneAsync(old);
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
