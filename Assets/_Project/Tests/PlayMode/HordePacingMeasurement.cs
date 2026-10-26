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
    // horde on, a few seeds, at a raised time scale, and writes how long the floor took, how much of it had an enemy
    // standing (combat uptime), what it killed and what state the hero reached the gate in to
    // TestResults/horde-pacing.txt. Explicit, so only a run that names it plays it; it asserts nothing about balance.
    [Explicit("Measurement: run it by name.")]
    public sealed class HordePacingMeasurement
    {
        private const float TimeScale = 4f;
        private const float FloorLimitSeconds = 900f;
        private static readonly int[] Seeds = { 11, 22, 33 };

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Pacing cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        [UnityTest, Timeout(1800000)] public IEnumerator TheAutopilotPlaysTheFirstFloorWithTheHorde()
        {
            var report = new StringBuilder();
            report.AppendLine("seed\tresult\tfloor_s\tuptime\twave_kills\thorde_kills\tlevel\thp\tmax_hp\thorde_avg\thorde_max\trooms");
            foreach (int seed in Seeds)
            {
                TestProfile.Begin();
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
                File.WriteAllText(DevelopmentStart.AutopilotPath, "on");
                File.WriteAllText(DevelopmentStart.RunSeedPath, seed.ToString());
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
                yield return null;
                var setup = Object.FindFirstObjectByType<CombatSetup>();
                var encounters = setup.Encounters;
                var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
                bool cleared = false;
                int waveKills = 0, hordeKills = 0;
                encounters.FloorCleared += () => cleared = true;
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
                Time.timeScale = TimeScale;
                float start = Time.time, fighting = 0f, active = 0f, hordeSum = 0f;
                int hordeMax = 0, frames = 0;
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
                    frames++;
                    if (encounters.RoomNumber != room)
                    {
                        rooms.Append($"{room}:{Time.time - roomStart:0}s ");
                        room = encounters.RoomNumber;
                        roomStart = Time.time;
                    }
                }
                rooms.Append($"{room}:{Time.time - roomStart:0}s");
                Time.timeScale = 1;
                string result = cleared ? "cleared" : setup.Run.HasEnded ? "died" : "timeout";
                report.AppendLine(string.Join("\t", seed, result, (Time.time - start).ToString("0"),
                    (active > 0 ? fighting / active : 0).ToString("0.00"), waveKills, hordeKills, setup.Run.Level,
                    hero.Current.ToString("0"), hero.Maximum.ToString("0"),
                    (frames > 0 ? hordeSum / frames : 0).ToString("0.0"), hordeMax, rooms));
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
