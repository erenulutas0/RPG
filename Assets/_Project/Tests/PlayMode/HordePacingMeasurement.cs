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
    // uptime), how much of it the hero was actually engaged (an enemy within its reach plus a unit, or a hit taken in the
    // last 1.5 s), what it killed, how the Warden's fight went and what state the hero reached the gate in to
    // TestResults/horde-pacing.txt, and keeps each seed's telemetry under TestResults/horde-telemetry/<hero>-<seed>/
    // (the v01 research's floor_breakdown.py splits it into walk, node fights, Warden and exit). Every frame is a fixed 1/60 s of game time (Time.captureDeltaTime), as fast as the
    // Editor can run it, so a seed replays the same floor every time - a raised time scale made the same build clear a
    // seed in one run and die on it in the next. Explicit, so only a run that names it plays it; it asserts nothing about
    // balance.
    [Explicit("Measurement: run it by name.")]
    public sealed class HordePacingMeasurement
    {
        private const float Step = 1f / 60f;
        private const float FloorLimitSeconds = 900f;
        // Engaged: an enemy this far past the hero's reach (floor units), or a hit taken this recently.
        private const float EngagedMargin = 1f;
        private const float EngagedAfterHitSeconds = 1.5f;
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
            report.AppendLine("mode " + mode + (heroId.Length > 0 ? " hero " + heroId : string.Empty));
            report.AppendLine("seed\tresult\tfloor_s\tuptime\tengaged\twave_kills\thorde_kills\tlevel\thp\tmax_hp\thorde_avg\thorde_max\tslams\tslam_hits\tboss_s\tboss_left\tclock\trooms");
            foreach (int seed in Seeds())
            {
                TestProfile.Begin();
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.AutopilotPath));
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
                File.WriteAllText(DevelopmentStart.AutopilotPath, mode);
                if (heroId.Length > 0)
                    File.WriteAllText(DevelopmentStart.HeroPath, heroId);
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
                float lastHit = float.NegativeInfinity, engaged = 0f;
                hero.Damaged += context => lastHit = Time.time;
                AttackController heroAttack = hero.GetComponent<AttackController>();
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
                    if (Time.time - lastHit <= EngagedAfterHitSeconds || EnemyNear(encounters, hero.transform.position,
                            (heroAttack != null && heroAttack.Weapon != null ? heroAttack.Weapon.Range : 1f) + EngagedMargin))
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
                    string kept = Path.Combine(Application.dataPath, "..", "TestResults", "horde-telemetry",
                        (heroId.Length > 0 ? heroId : "hero_vanguard") + "-" + seed);
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

        // A living wave or horde enemy within reach of the hero, measured on the floor (depth unsquashed).
        private static bool EnemyNear(EncounterController encounters, Vector3 hero, float reach)
        {
            float reachSquared = reach * reach;
            for (int i = 0; i < encounters.WaveEnemyCount; i++)
                if (Near(encounters.WaveEnemyAt(i), hero, reachSquared))
                    return true;
            for (int i = 0; i < encounters.HordeEnemyCount; i++)
                if (Near(encounters.HordeEnemyAt(i), hero, reachSquared))
                    return true;
            return false;
        }

        private static bool Near(Health enemy, Vector3 hero, float reachSquared)
        {
            if (enemy == null || !enemy.IsAlive)
                return false;
            Vector3 delta = enemy.transform.position - hero;
            float y = delta.y / ArenaFloor.DepthScale;
            return delta.x * delta.x + y * y <= reachSquared;
        }
    }
}
