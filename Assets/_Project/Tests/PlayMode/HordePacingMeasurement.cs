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
    // balance. With TestResults/horde-floors.txt "3" it plays on past floor 1's clear, down the whole Descent (Floors,
    // DescentSeed), and adds a "descent" row a seed after the floor-1 rows, which stay as they are.
    [Explicit("Measurement: run it by name.")]
    public sealed class HordePacingMeasurement
    {
        private const float Step = 1f / 60f;
        private const float FloorLimitSeconds = 900f;
        // The whole Descent's limit a seed, from floor 1's first frame (horde-floors.txt "3"): about 14 minutes of game
        // time, where the simulation's careful pilot walks out of the finale at about 10. And a run whose game stands
        // still this many frames together - a choice nobody answers - ends there too, as stuck.
        private const float DescentLimitSeconds = 840f;
        private const int StuckFrames = 36000;
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

        // TestResults/horde-floors.txt, "3", plays the whole Descent (docs/41's evening status, docs/47): after floor 1's
        // clear the checkpoint's Descend, floor 2 - its halls, the Deep Forge, the Sentry Hall, the gate's hold for its
        // 3:30, its Warden and gate - Descend again, and the Final Swarm to its exit or the hero's fall. The autopilot
        // already plays all of it (DevelopmentAutopilot: every checkpoint Descend, the forge's first option, the finale's
        // exit a minute in). Nothing, or no file, is floor 1 alone; anything else is refused.
        private static int Floors()
        {
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-floors.txt");
            string text = File.Exists(path) ? File.ReadAllText(path).Trim() : string.Empty;
            if (text.Length == 0)
                return 1;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int floors) ? floors : -1;
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

        // Four hours: six seeds down the whole Descent take about an hour and a quarter of the Editor's time.
        [UnityTest, Timeout(14400000)] public IEnumerator TheAutopilotPlaysTheFirstFloorWithTheHorde()
        {
            var report = new StringBuilder();
            string mode = Mode();
            string heroId = Hero();
            string weaponId = Weapon();
            string cohort = Cohort();
            int floors = Floors();
            Assert.That(cohort.Length == 0 || AutopilotCohort.TryNamed(cohort, out _), Is.True,
                $"horde-cohort.txt names no cohort: '{cohort}'");
            Assert.That(floors == 1 || floors == 3, Is.True, "horde-floors.txt holds 3, or nothing for floor 1 alone");
            bool down = floors == 3;
            report.AppendLine("mode " + mode + (heroId.Length > 0 ? " hero " + heroId : string.Empty) +
                (weaponId.Length > 0 ? " weapon " + weaponId : string.Empty) + (cohort.Length > 0 ? " cohort " + cohort : string.Empty) +
                (down ? " floors 3" : string.Empty));
            var descentRows = new StringBuilder();
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
                // The whole Descent's watch, from floor 1's first frame (horde-floors.txt "3"); none for floor 1 alone.
                DescentSeed descent = down ? new DescentSeed(seed, setup, hero) : null;
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
                if (descent != null)
                {
                    // Floor 1 as its row has it; then, after a clear, floor 2 and the finale under the Descent's limit.
                    descent.FloorOne(active, fighting, engaged, bossSeconds);
                    while (cleared && descent.Clears < 3 && !setup.Run.HasEnded && Time.time - start < DescentLimitSeconds &&
                           descent.StillFrames < StuckFrames)
                    {
                        // A frame belongs to the floor it started on: the frame a floor clears in is that floor's, as
                        // floor 1's last frame is in its row.
                        int on = descent.Clears;
                        yield return null;
                        descent.Frame(on);
                    }
                    descentRows.AppendLine(descent.Row(start, descent.StillFrames >= StuckFrames));
                }
                Time.captureDeltaTime = 0f;
                var old = SceneManager.GetActiveScene();
                SceneManager.SetActiveScene(SceneManager.CreateScene("Pacing between seeds " + seed));
                yield return SceneManager.UnloadSceneAsync(old);
                // The unload has flushed the run's last events; TestProfile.End deletes the folder.
                string telemetry = Path.Combine(TestProfile.Folder, "telemetry");
                if (Directory.Exists(telemetry))
                {
                    // A cohort's run keeps its own folder, so the careful pilot's telemetry the parity reads stays; so
                    // does a run down the whole Descent, whose last event is no longer floor 1's.
                    string kept = Path.Combine(Application.dataPath, "..", "TestResults", "horde-telemetry",
                        (heroId.Length > 0 ? heroId : "hero_vanguard") + (cohort.Length > 0 ? "-" + cohort : string.Empty) +
                        (down ? "-descent" : string.Empty) + "-" + seed);
                    if (Directory.Exists(kept))
                        Directory.Delete(kept, true);
                    Directory.CreateDirectory(kept);
                    foreach (string file in Directory.GetFiles(telemetry))
                        File.Copy(file, Path.Combine(kept, Path.GetFileName(file)));
                }
                TestProfile.End();
            }
            // The Descent's rows after every floor-1 row, each line led by "descent" so a floor-1 reader skips it
            // (Tools/CombatChecks/EditorDescent reads them for the finale's report).
            if (down)
                report.AppendLine(DescentSeed.Header).Append(descentRows);
            string path = Path.Combine(Application.dataPath, "..", "TestResults", "horde-pacing.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, report.ToString());
            TestProfile.Begin();
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
        }

        // One seed down the whole Descent (horde-floors.txt "3"): floor 1 as its loop measured it, then floor 2 and the
        // finale a frame at a time, and what the encounter tells as it goes - each floor's clear, the Warden walking out,
        // the swarm opening, the arrival chest, the hero's fall - as one tab-separated row led by "descent" (Header).
        // Floor 2 and the finale run from the clear before them, the checkpoint's choice and the descent's advance delay
        // in them, as the simulation's FloorSeconds do (the choice holds the game still, so it adds nothing). Numbers are
        // invariant; "-" where a floor was never reached or a thing never happened.
        private sealed class DescentSeed
        {
            public const string Header = "descent\tseed\tresult\tdied_at\tfloor1_s\tfloor2_s\tfinale_s\tdescent_s\twarden1_s\twarden2_s\t" +
                "level_gate1\tlevel_gate2\tuptime1\tengaged1\tuptime2\tengaged2\tuptime3\tengaged3\thold2_s\tclock2\t" +
                "finale_hp\tfinale_max_hp\tfinale_level\tchest_s\tswarm_s\tlevel\thp\tmax_hp\tkills2\tkills3\trooms2";
            // DevelopmentAutopilot.SwarmStay: past it a death in the finale is on the walk out ("swarm exit").
            private const float SwarmStaySeconds = 60f;
            private readonly int _seed;
            private readonly CombatSetup _setup;
            private readonly EncounterController _encounters;
            private readonly Health _hero;
            // Each floor's clear (1-3) as it happened; per floor (0 floor 1, 1 floor 2, 2 the finale) its active seconds,
            // those with an enemy standing, those engaged, those the Warden stood, its kills and the level as the Warden
            // walked out.
            private readonly float[] _clearedAt = { float.NaN, float.NaN, float.NaN, float.NaN };
            private readonly float[] _active = new float[3];
            private readonly float[] _fighting = new float[3];
            private readonly float[] _engaged = new float[3];
            private readonly float[] _warden = new float[3];
            private readonly int[] _kills = new int[3];
            private readonly int[] _levelAtGate = { -1, -1, -1 };
            private float _lastHit = float.NegativeInfinity, _lastStrike = float.NegativeInfinity;
            private float _hold2, _clock2;
            private float _swarm = float.NaN, _chest = float.NaN;
            private float _finaleHealth = float.NaN, _finaleMaximum = float.NaN;
            private int _finaleLevel = -1;
            // The gate's hold as the encounter last reported it: a fall clears the hold before anything hears of it.
            private bool _holding;
            private string _diedAt;
            private readonly StringBuilder _rooms2 = new StringBuilder();
            private int _room;
            private float _roomStart;

            public int Clears { get; private set; }
            // Frames in a row the game stood still (a choice open): a choice nobody answers ends the seed.
            public int StillFrames { get; private set; }

            public DescentSeed(int seed, CombatSetup setup, Health hero)
            {
                _seed = seed;
                _setup = setup;
                _encounters = setup.Encounters;
                _hero = hero;
                _encounters.FloorCleared += () =>
                {
                    Clears++;
                    if (Clears < _clearedAt.Length)
                        _clearedAt[Clears] = Time.time;
                };
                _encounters.GateBossArrived += () =>
                {
                    int floor = _encounters.FloorNumber - 1;
                    if (floor >= 0 && floor < _levelAtGate.Length && _levelAtGate[floor] < 0)
                        _levelAtGate[floor] = _setup.Run.Level;
                };
                // The swarm opens: the hero as it arrives, before its chest (ChestSpawner hears the same event first and
                // only places the chest; it opens on the chest's next Update).
                _encounters.EncounterStarted += () =>
                {
                    if (!_encounters.IsInSwarmRoom || !float.IsNaN(_finaleHealth))
                        return;
                    _finaleHealth = _hero.Current;
                    _finaleMaximum = _hero.Maximum;
                    _finaleLevel = _setup.Run.Level;
                };
                _encounters.EnemyDefeated += enemy =>
                {
                    int floor = _encounters.FloorNumber - 1;
                    if (floor >= 0 && floor < _kills.Length)
                        _kills[floor]++;
                };
                _encounters.ProgressChanged += () => _holding = _encounters.IsHoldingForGate;
                _hero.Damaged += context => _lastHit = Time.time;
                _hero.Died += () => _diedAt = _encounters.FloorNumber.ToString(CultureInfo.InvariantCulture) + "." +
                    _encounters.RoomNumber.ToString(CultureInfo.InvariantCulture) + ":" + Phase();
                AttackController attack = hero.GetComponent<AttackController>();
                if (attack != null)
                    attack.StruckWith += (target, weapon) => _lastStrike = Time.time;
                var chests = Object.FindFirstObjectByType<ChestSpawner>();
                if (chests != null)
                    chests.Opened += (reward, spot) =>
                    {
                        if (_encounters.IsInSwarmRoom && float.IsNaN(_chest))
                            _chest = _encounters.SwarmSeconds;
                    };
            }

            // Floor 1 as its loop measured it.
            public void FloorOne(float active, float fighting, float engaged, float warden)
            {
                _active[0] = active;
                _fighting[0] = fighting;
                _engaged[0] = engaged;
                _warden[0] = warden;
            }

            // One frame of floor 2 (on 1) or the finale (on 2): on, the floors cleared as the frame began.
            public void Frame(int on)
            {
                float dt = Time.deltaTime;
                if (dt <= 0f)
                {
                    StillFrames++;
                    return;
                }
                StillFrames = 0;
                int floor = Mathf.Clamp(on, 1, 2);
                float now = Time.time;
                _active[floor] += dt;
                if (_encounters.AllAliveCount > 0)
                    _fighting[floor] += dt;
                if (now - _lastHit <= EngagedSeconds || now - _lastStrike <= EngagedSeconds)
                    _engaged[floor] += dt;
                if (_setup.Warden.Boss != null)
                    _warden[floor] += dt;
                if (_encounters.IsInSwarmRoom)
                    _swarm = _encounters.SwarmSeconds;
                if (floor != 1)
                    return;
                if (_encounters.IsHoldingForGate)
                    _hold2 += dt;
                _clock2 = Mathf.Max(_clock2, _encounters.FloorClockSeconds);
                // Floor 2's rooms, as floor 1's rooms column: a room's seconds from its first frame to the next room's.
                int room = Clears > on || _encounters.FloorNumber != 2 ? 0 : _encounters.RoomNumber;
                if (room == _room)
                    return;
                CloseRoom(now);
                _room = room;
                _roomStart = now;
            }

            private void CloseRoom(float now)
            {
                if (_room > 0)
                    _rooms2.Append(_room.ToString(CultureInfo.InvariantCulture)).Append(':')
                        .Append((now - _roomStart).ToString("0", CultureInfo.InvariantCulture)).Append("s ");
                _room = 0;
            }

            // Where a fall happened, in the simulation's words (FoundryResult.DeathPhase).
            private string Phase()
            {
                EncounterController e = _encounters;
                if (e.IsInSwarmRoom)
                    return e.SwarmSeconds >= SwarmStaySeconds ? "swarm exit" : "swarm";
                if (e.IsRoomSiteOpen)
                    return "walk";
                if (e.IsGateExitOpen)
                    return "exit";
                if (e.IsDescending)
                    return "descent";
                if (e.IsInNonCombatRoom)
                    return "forge";
                RoomDefinition room = e.CurrentRoom;
                if (room != null && room.Kind == RoomKind.Boss && e.AliveEnemyCount > 0)
                    return "boss";
                if (e.AliveEnemyCount > 0)
                    return "wave " + e.WaveNumber.ToString(CultureInfo.InvariantCulture);
                return _holding ? "gate wait" : "between waves";
            }

            public string Row(float start, bool stuck)
            {
                float end = Time.time;
                CloseRoom(end);
                bool walked = Clears >= 3;
                RunOutcome outcome = _setup.Run.Outcome;
                string result = walked ? "walked_out"
                    : outcome == RunOutcome.Defeat ? "died"
                    : outcome == RunOutcome.Extracted ? "extracted"
                    : stuck ? "stuck" : "timeout";
                float floor1 = (Clears >= 1 ? _clearedAt[1] : end) - start;
                float floor2 = Clears >= 1 ? (Clears >= 2 ? _clearedAt[2] : end) - _clearedAt[1] : float.NaN;
                float finale = Clears >= 2 ? (walked ? _clearedAt[3] : end) - _clearedAt[2] : float.NaN;
                string Share(float part, int floor) =>
                    _active[floor] > 0f ? (part / _active[floor]).ToString("0.00", CultureInfo.InvariantCulture) : "-";
                return string.Join("\t", "descent", _seed.ToString(CultureInfo.InvariantCulture), result, _diedAt ?? "-",
                    N(floor1), N(floor2), N(finale), N(walked ? _clearedAt[3] - start : float.NaN),
                    N(_warden[0] > 0f ? _warden[0] : float.NaN), N(_warden[1] > 0f ? _warden[1] : float.NaN),
                    L(_levelAtGate[0]), L(_levelAtGate[1]),
                    Share(_fighting[0], 0), Share(_engaged[0], 0), Share(_fighting[1], 1), Share(_engaged[1], 1),
                    Share(_fighting[2], 2), Share(_engaged[2], 2),
                    N(Clears >= 1 ? _hold2 : float.NaN), N(Clears >= 1 ? _clock2 : float.NaN),
                    N(_finaleHealth), N(_finaleMaximum), L(_finaleLevel), N(_chest, "0.0"), N(_swarm, "0.0"),
                    L(_setup.Run.Level), N(_hero.Current), N(_hero.Maximum),
                    L(Clears >= 1 ? _kills[1] : -1), L(Clears >= 2 ? _kills[2] : -1),
                    _rooms2.Length > 0 ? _rooms2.ToString().TrimEnd() : "-");
            }

            private static string N(float value, string format = "0") =>
                float.IsNaN(value) ? "-" : value.ToString(format, CultureInfo.InvariantCulture);

            private static string L(int value) => value >= 0 ? value.ToString(CultureInfo.InvariantCulture) : "-";
        }
    }
}
