using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Cryptforge.Analytics;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.Save;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The real scene writes its local event log under the test profile's telemetry folder: in cause-then-effect order,
    // one session across a restart, and without touching combat or the console when the folder cannot be written.
    // Lines reach the file only at flush points, so every read follows one: a choice opening, a run ending, or the
    // scene unloading (which also closes an unfinished run as interrupted).
    public sealed class TelemetryFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private Health _hero;
        private EncounterController _encounters;

        [UnityTearDown]
        public IEnumerator UnloadGameplay()
        {
            Time.timeScale = 1f;
            yield return UnloadGameplayScene();
            TestProfile.End();
        }

        [UnityTest]
        public IEnumerator TheFirstPackAndItsLevelUpWriteAnOrderedLogBesideAValidProfile()
        {
            yield return LoadGameplay(new PlayerProfile(250, null, null, 1));
            Assert.That(TelemetryLocation.Resolve(), Is.EqualTo(Path.Combine(TestProfile.Folder, TelemetryLocation.FolderName)));

            yield return WaitForUpgradeOffer();
            UpgradeOffer offer = _setup.Upgrades.CurrentOffer;
            List<Line> log = ReadLog();
            AssertSessionEnvelopes(log);
            AssertRunsAreBracketed(log);
            Assert.That(Names(log), Is.EqualTo(new[] { "session_start", "run_start", "room_start", "first_kill", "upgrade_offered" }),
                "Opening the level-up choice wrote everything before it. " + Describe(log));

            Line sessionStart = log[0];
            Assert.That(sessionStart.Number("schema"), Is.EqualTo(TelemetryJson.SchemaVersion));
            Assert.That(sessionStart.Text("app_version"), Is.EqualTo(Application.version));
            Assert.That(sessionStart.Text("platform"), Is.EqualTo(Application.platform.ToString().ToLowerInvariant()));
            Assert.That(sessionStart.Flag("development"), Is.EqualTo(Debug.isDebugBuild));
            Assert.That(sessionStart.Has("run"), Is.False);

            string runId = sessionStart.Session + "-1";
            Line runStart = log[1];
            Assert.That(runStart.Run, Is.EqualTo(runId));
            Assert.That(runStart.Number("run_ordinal"), Is.EqualTo(1));
            Assert.That(runStart.Text("hero_id"), Is.EqualTo("hero_vanguard"));
            Assert.That(runStart.Text("weapon_id"), Is.EqualTo("weapon_sword"));
            Assert.That(runStart.IsNull("relic_id"), Is.True, "No relic is equipped.");
            Assert.That(runStart.Text("ability_id"), Is.EqualTo("ability_forge_burst"));
            Assert.That(runStart.Number("forge_gold"), Is.EqualTo(250));
            Assert.That(runStart.Number("deepest_floor"), Is.EqualTo(1));

            Line roomStart = log[2];
            Assert.That(roomStart.Number("floor_index"), Is.EqualTo(1));
            Assert.That(roomStart.Text("floor_id"), Is.EqualTo("floor_ember_halls"));
            Assert.That(roomStart.Number("room_index"), Is.EqualTo(1));
            Assert.That(roomStart.Text("room_kind"), Is.EqualTo("combat"));

            Line firstKill = log[3];
            Assert.That(firstKill.Text("enemy_id"), Is.EqualTo("enemy_grunt").Or.EqualTo("enemy_cinder_mite"));
            Assert.That(firstKill.Number("active_sec"), Is.GreaterThan(0), "The pack walked in and fought before it fell.");
            Assert.That(firstKill.Number("floor_index"), Is.EqualTo(1));
            Assert.That(firstKill.Number("room_index"), Is.EqualTo(1));

            Line offered = log[4];
            Assert.That(offered.Text("choice_ids"), Is.EqualTo(IdsOf(offer)));
            Assert.That(offered.Number("run_level"), Is.EqualTo(_setup.Run.Level));
            Assert.That(offered.Number("pending"), Is.EqualTo(_setup.Run.PendingUpgrades));

            string chosenId = offer.Choices[0].Id;
            Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, 0), Is.True);
            yield return UnloadGameplayScene();

            log = ReadLog();
            AssertSessionEnvelopes(log);
            AssertRunsAreBracketed(log);
            Assert.That(log.Count, Is.GreaterThanOrEqualTo(7), Describe(log));
            Line selected = log[5];
            Assert.That(selected.Event, Is.EqualTo("upgrade_selected"), Describe(log));
            Assert.That(selected.Text("upgrade_id"), Is.EqualTo(chosenId));
            Assert.That(selected.Number("choice_slot"), Is.EqualTo(0));
            Assert.That(selected.Number("stacks"), Is.EqualTo(1));
            Line interrupted = log[log.Count - 1];
            Assert.That(interrupted.Event, Is.EqualTo("run_interrupted"), "Leaving the scene mid-run is not a result. " + Describe(log));
            Assert.That(interrupted.Run, Is.EqualTo(runId));
            Assert.That(interrupted.Text("reason"), Is.EqualTo("scene_unloaded"));
            Assert.That(interrupted.Number("floor_index"), Is.EqualTo(1));
            Assert.That(interrupted.Number("room_index"), Is.EqualTo(1));
            Assert.That(Count(log, "run_end"), Is.Zero, "No run end is made up for an unfinished run.");
            Assert.That(Count(log, "run_interrupted"), Is.EqualTo(1));

            // The profile beside the log is untouched: the seeded file still loads as it was saved.
            var store = new ProfileStore(TestProfile.Folder);
            PlayerProfile saved = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(saved.Gold, Is.EqualTo(250));
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));
            Assert.That(File.Exists(Path.Combine(TestProfile.Folder, FileTelemetryStore.FileName)), Is.False,
                "Events are written only inside the telemetry folder.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ADefeatAndARestartStartASecondRunInTheSameSessionWithoutDuplicates()
        {
            yield return LoadGameplay(null);
            yield return WaitForUpgradeOffer();
            string firstChoice = _setup.Upgrades.CurrentOffer.Choices[0].Id;
            Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, 0), Is.True);
            _hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            yield return null;
            Assert.That(_setup.Run.HasEnded, Is.True);

            int loads = 0;
            UnityAction<Scene, LoadSceneMode> countLoad = (scene, mode) => loads++;
            SceneManager.sceneLoaded += countLoad;
            CombatSetup previous = _setup;
            try
            {
                previous.RestartRun();
                float deadline = Time.realtimeSinceStartup + 5f;
                while ((loads == 0 || previous != null) && Time.realtimeSinceStartup < deadline)
                    yield return null;
                yield return null;
            }
            finally
            {
                SceneManager.sceneLoaded -= countLoad;
            }
            Assert.That(loads, Is.EqualTo(1));
            Assert.That(previous == null, Is.True, "The ended run's scene is gone.");
            FindSceneObjects();
            Assert.That(_setup.Run.HasEnded, Is.False);

            yield return WaitForUpgradeOffer();
            string secondChoice = _setup.Upgrades.CurrentOffer.Choices[0].Id;
            Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, 0), Is.True);
            yield return UnloadGameplayScene();

            List<Line> log = ReadLog();
            AssertSessionEnvelopes(log);
            AssertRunsAreBracketed(log);
            string firstRun = log[0].Session + "-1";
            string secondRun = log[0].Session + "-2";

            List<int> starts = IndicesOf(log, "run_start");
            Assert.That(starts.Count, Is.EqualTo(2), "The restart began a second run in the same session. " + Describe(log));
            Assert.That(log[starts[0]].Run, Is.EqualTo(firstRun));
            Assert.That(log[starts[0]].Number("run_ordinal"), Is.EqualTo(1));
            Assert.That(log[starts[1]].Run, Is.EqualTo(secondRun));
            Assert.That(log[starts[1]].Number("run_ordinal"), Is.EqualTo(2));

            List<int> ends = IndicesOf(log, "run_end");
            Assert.That(ends.Count, Is.EqualTo(1), Describe(log));
            Assert.That(ends[0], Is.GreaterThan(starts[0]).And.LessThan(starts[1]));
            Line runEnd = log[ends[0]];
            Assert.That(runEnd.Run, Is.EqualTo(firstRun));
            Assert.That(runEnd.Text("result"), Is.EqualTo("defeat"));
            Assert.That(runEnd.Text("cause"), Does.StartWith("killed"));
            Assert.That(runEnd.Number("upgrades_applied"), Is.EqualTo(1));

            List<int> interrupted = IndicesOf(log, "run_interrupted");
            Assert.That(interrupted, Is.EqualTo(new List<int> { log.Count - 1 }), "Only the unfinished second run is interrupted.");
            Assert.That(log[log.Count - 1].Run, Is.EqualTo(secondRun));

            // One recorder per scene: every selection, offer and first kill is written once, under its own run.
            List<int> selections = IndicesOf(log, "upgrade_selected");
            Assert.That(selections.Count, Is.EqualTo(2), Describe(log));
            Assert.That(log[selections[0]].Run, Is.EqualTo(firstRun));
            Assert.That(log[selections[0]].Text("upgrade_id"), Is.EqualTo(firstChoice));
            Assert.That(log[selections[1]].Run, Is.EqualTo(secondRun));
            Assert.That(log[selections[1]].Text("upgrade_id"), Is.EqualTo(secondChoice));
            Assert.That(CountInRun(log, "upgrade_offered", firstRun), Is.EqualTo(1));
            Assert.That(CountInRun(log, "upgrade_offered", secondRun), Is.EqualTo(1));
            Assert.That(CountInRun(log, "first_kill", firstRun), Is.EqualTo(1));
            Assert.That(CountInRun(log, "first_kill", secondRun), Is.EqualTo(1));
            Assert.That(CountInRun(log, "room_start", firstRun), Is.EqualTo(1));
            Assert.That(CountInRun(log, "room_start", secondRun), Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        // The bridge looks the chest spawner up while the scene is still waking; without it chest_opened would silently
        // never be written, so the real walk onto the first room's chest pins that lookup.
        [UnityTest]
        public IEnumerator WalkingOntoTheFirstChestLogsItWithTheRoomAndTheHealedHealth()
        {
            yield return LoadGameplay(null);
            ChestSpawner chests = UnityEngine.Object.FindFirstObjectByType<ChestSpawner>();
            var movement = _hero.GetComponent<Cryptforge.UI.HeroMovementInput>();
            _hero.GetComponent<AttackController>().enabled = false;
            Assert.That(chests.HasChest, Is.True, "A chest waits from the first wave.");

            _hero.ApplyDamage(new DamageContext(40f));
            float wounded = _hero.Current;
            movement.Hold(new Vector2(1f, 0f));
            float deadline = Time.realtimeSinceStartup + 6f;
            while (!chests.IsOpen && Time.realtimeSinceStartup < deadline)
                yield return null;
            movement.Release();
            Assert.That(chests.IsOpen, Is.True, "Walking onto the chest opens it.");
            yield return UnloadGameplayScene();

            List<Line> log = ReadLog();
            AssertSessionEnvelopes(log);
            AssertRunsAreBracketed(log);
            Assert.That(Names(log), Is.EqualTo(new[] { "session_start", "run_start", "room_start", "chest_opened", "run_interrupted" }),
                Describe(log));
            Line chest = log[3];
            Assert.That(chest.Text("reward"), Is.EqualTo("heal"));
            Assert.That(chest.Number("gold"), Is.EqualTo(0));
            Assert.That(chest.Number("heal_fraction"), Is.EqualTo(ChestRule.HealFraction));
            Assert.That(chest.Number("hero_hp"), Is.GreaterThan(wounded).And.LessThanOrEqualTo(100), "The health after the chest mended.");
            Assert.That(chest.Number("floor_index"), Is.EqualTo(1));
            Assert.That(chest.Number("room_index"), Is.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ATelemetryFolderBlockedByAFileLeavesCombatRunningWithoutErrorLogs()
        {
            TestProfile.Begin();
            string blocked = Path.Combine(TestProfile.Folder, TelemetryLocation.FolderName);
            File.WriteAllText(blocked, "not a folder");
            yield return LoadScene();
            TelemetrySession session = TelemetryRuntime.SessionFor(TelemetryLocation.Resolve());

            yield return WaitForUpgradeOffer();
            Assert.That(session.Log.FailedWrites, Is.GreaterThanOrEqualTo(1), "Opening the choice tried to write and failed.");
            Assert.That(session.Log.LastError, Is.Not.Null);
            AttackController heroAttack = _hero.GetComponent<AttackController>();
            Assert.That(_setup.Choices.TrySelect(_setup.Choices.Current, 0), Is.True);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            float deadline = Time.realtimeSinceStartup + 6f;
            while (_encounters.EncounterNumber < 2 && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_encounters.EncounterNumber, Is.EqualTo(2), "The next pack entered after the choice.");
            int swings = heroAttack.AttackCount;
            deadline = Time.realtimeSinceStartup + 8f;
            while (heroAttack.AttackCount == swings && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(heroAttack.AttackCount, Is.GreaterThan(swings), "Combat goes on while telemetry cannot write.");
            Assert.That(_setup.Run.HasEnded, Is.False);
            Assert.That(File.Exists(blocked), Is.True);
            Assert.That(Directory.Exists(blocked), Is.False);

            yield return UnloadGameplayScene();
            LogAssert.NoUnexpectedReceived();
            Assert.That(session.Log.Dropped, Is.Zero, "A short run stays within the buffer.");

            // Once the folder can be created the same session writes every retained line, in order, from its start.
            File.Delete(blocked);
            Assert.That(TelemetryRuntime.SessionFor(TelemetryLocation.Resolve()), Is.SameAs(session));
            Assert.That(session.Flush(), Is.True);
            List<Line> log = ReadLog();
            AssertSessionEnvelopes(log);
            AssertRunsAreBracketed(log);
            Assert.That(Names(log), Has.Member("upgrade_offered"), Describe(log));
            Assert.That(Names(log), Has.Member("upgrade_selected"), Describe(log));
            Assert.That(log[1].Event, Is.EqualTo("run_start"));
            Assert.That(log[log.Count - 1].Event, Is.EqualTo("run_interrupted"), Describe(log));
            Assert.That(Count(log, "telemetry_dropped"), Is.Zero);
        }

        private IEnumerator LoadGameplay(PlayerProfile seed)
        {
            TestProfile.Begin(seed);
            yield return LoadScene();
        }

        private IEnumerator LoadScene()
        {
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            FindSceneObjects();
        }

        private void FindSceneObjects()
        {
            _setup = GameObject.Find("Combat Setup").GetComponent<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _encounters = UnityEngine.Object.FindFirstObjectByType<EncounterController>();
        }

        // Unloading destroys the scene, which disposes its recorder and flushes; later calls find no gameplay scene.
        private static IEnumerator UnloadGameplayScene()
        {
            Scene gameplay = SceneManager.GetActiveScene();
            if (gameplay.path != ScenePath)
                yield break;

            Scene empty = SceneManager.CreateScene("Telemetry Test Cleanup");
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(gameplay);
        }

        private IEnumerator WaitForUpgradeOffer()
        {
            float deadline = Time.realtimeSinceStartup + 15f;
            while (_setup.Upgrades.CurrentOffer == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.That(_setup.Upgrades.CurrentOffer, Is.Not.Null, "Clearing the first pack must open an upgrade offer.");
            Assert.That(_setup.Choices.Current, Is.Not.Null);
            Assert.That(_setup.Choices.Current.Kind, Is.EqualTo(ChoiceKind.Upgrade));
        }

        // Every file of the set, oldest first, split into lines; each line must be one complete JSON object.
        private static List<Line> ReadLog()
        {
            string directory = Path.Combine(TestProfile.Folder, TelemetryLocation.FolderName);
            var lines = new List<Line>();
            for (int index = 2; index >= 0; index--)
            {
                string path = Path.Combine(directory, FileTelemetryStore.FileNameAt(index));
                if (!File.Exists(path))
                    continue;

                string text = File.ReadAllText(path, Encoding.UTF8);
                Assert.That(text, Does.EndWith("\n"), $"{path} ends with a complete line.");
                string[] parts = text.Substring(0, text.Length - 1).Split('\n');
                foreach (string part in parts)
                    lines.Add(new Line(part));
            }
            Assert.That(lines, Is.Not.Empty, "The telemetry folder holds the session's events.");
            return lines;
        }

        // One session from its first line: session_start once and first, the envelope in schema order, one session id,
        // and sequence numbers counting up from 1 with no gap (nothing was dropped in these short runs).
        private static void AssertSessionEnvelopes(List<Line> log)
        {
            var timestamp = new Regex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}\.\d{3}Z$");
            Assert.That(log[0].Event, Is.EqualTo("session_start"), Describe(log));
            Assert.That(Count(log, "session_start"), Is.EqualTo(1), Describe(log));
            double lastSeconds = 0;
            for (int i = 0; i < log.Count; i++)
            {
                Line line = log[i];
                IReadOnlyList<string> keys = line.Keys;
                Assert.That(keys.Count, Is.GreaterThanOrEqualTo(6), line.Raw);
                Assert.That(new[] { keys[0], keys[1], keys[2], keys[3], keys[4] }, Is.EqualTo(new[] { "v", "seq", "t", "st", "session" }), line.Raw);
                Assert.That(keys[5] == "event" || (keys.Count > 6 && keys[5] == "run" && keys[6] == "event"), Is.True, line.Raw);
                Assert.That(line.Number("v"), Is.EqualTo(TelemetryJson.SchemaVersion), line.Raw);
                Assert.That((long)line.Number("seq"), Is.EqualTo(i + 1), "Sequence numbers count up without a gap. " + line.Raw);
                Assert.That(line.Session, Is.EqualTo(log[0].Session), line.Raw);
                Assert.That(timestamp.IsMatch(line.Text("t")), Is.True, line.Raw);
                double seconds = line.Number("st");
                Assert.That(seconds, Is.GreaterThanOrEqualTo(lastSeconds), line.Raw);
                lastSeconds = seconds;
            }
        }

        // run_start precedes every line of its run, every line while a run is open carries its id, run_end or
        // run_interrupted closes it at most once, and no later line carries a closed run's id.
        private static void AssertRunsAreBracketed(List<Line> log)
        {
            var seen = new List<string>();
            string open = null;
            for (int i = 0; i < log.Count; i++)
            {
                Line line = log[i];
                if (line.Event == "run_start")
                {
                    Assert.That(open, Is.Null, $"Line {i + 1} starts a run while {open} is open. " + Describe(log));
                    Assert.That(line.Run, Is.Not.Null.And.Not.Empty);
                    Assert.That(seen, Has.No.Member(line.Run), "Run ids are unique within the session.");
                    seen.Add(line.Run);
                    open = line.Run;
                    continue;
                }

                Assert.That(line.Run, Is.EqualTo(open), $"Line {i + 1} ({line.Event}) carries the wrong run. " + Describe(log));
                if (line.Event == "run_end" || line.Event == "run_interrupted")
                    open = null;
            }
        }

        private static string[] Names(List<Line> log)
        {
            var names = new string[log.Count];
            for (int i = 0; i < log.Count; i++)
                names[i] = log[i].Event;
            return names;
        }

        private static List<int> IndicesOf(List<Line> log, string name)
        {
            var indices = new List<int>();
            for (int i = 0; i < log.Count; i++)
            {
                if (log[i].Event == name)
                    indices.Add(i);
            }
            return indices;
        }

        private static int Count(List<Line> log, string name) => IndicesOf(log, name).Count;

        private static int CountInRun(List<Line> log, string name, string runId)
        {
            int count = 0;
            foreach (Line line in log)
            {
                if (line.Event == name && line.Run == runId)
                    count++;
            }
            return count;
        }

        private static string IdsOf(UpgradeOffer offer)
        {
            var ids = new StringBuilder();
            for (int i = 0; i < offer.Choices.Count; i++)
            {
                if (i > 0)
                    ids.Append(',');
                ids.Append(offer.Choices[i].Id);
            }
            return ids.ToString();
        }

        private static string Describe(List<Line> log)
        {
            var text = new StringBuilder("Log:");
            foreach (Line line in log)
                text.Append('\n').Append(line.Raw);
            return text.ToString();
        }

        // One parsed log line: a flat JSON object of strings, numbers, booleans and nulls, keys kept in written order.
        // Parsing is strict, so a malformed or split line fails the test that reads it.
        private sealed class Line
        {
            private readonly List<string> _keys = new List<string>();
            private readonly Dictionary<string, object> _values = new Dictionary<string, object>(StringComparer.Ordinal);

            public Line(string raw)
            {
                Raw = raw;
                int at = 0;
                try
                {
                    Parse(raw, ref at);
                }
                catch (Exception exception) when (exception is FormatException || exception is IndexOutOfRangeException ||
                                                  exception is ArgumentOutOfRangeException)
                {
                    Assert.Fail($"Not one flat JSON object ({exception.Message}): {raw}");
                }
            }

            public string Raw { get; }
            public IReadOnlyList<string> Keys => _keys;
            public string Event => Text("event");
            public string Session => Text("session");
            public string Run => Has("run") ? Text("run") : null;

            public bool Has(string key) => _values.ContainsKey(key);

            public bool IsNull(string key)
            {
                Assert.That(Has(key), Is.True, $"'{key}' is missing: {Raw}");
                return _values[key] == null;
            }

            public string Text(string key) => Value<string>(key);

            public double Number(string key) => Value<double>(key);

            public bool Flag(string key) => Value<bool>(key);

            private T Value<T>(string key)
            {
                Assert.That(Has(key), Is.True, $"'{key}' is missing: {Raw}");
                Assert.That(_values[key], Is.InstanceOf<T>(), $"'{key}' is not a {typeof(T).Name}: {Raw}");
                return (T)_values[key];
            }

            private void Parse(string text, ref int at)
            {
                Expect(text, ref at, '{');
                while (true)
                {
                    string key = ReadString(text, ref at);
                    Expect(text, ref at, ':');
                    if (_values.ContainsKey(key))
                        throw new FormatException($"duplicate key '{key}'");
                    _keys.Add(key);
                    _values.Add(key, ReadValue(text, ref at));
                    char next = text[at++];
                    if (next == '}')
                        break;
                    if (next != ',')
                        throw new FormatException($"unexpected '{next}' at {at - 1}");
                }
                if (at != text.Length)
                    throw new FormatException("text after the object");
            }

            private static object ReadValue(string text, ref int at)
            {
                char first = text[at];
                if (first == '"')
                    return ReadString(text, ref at);
                if (ReadLiteral(text, ref at, "true"))
                    return true;
                if (ReadLiteral(text, ref at, "false"))
                    return false;
                if (ReadLiteral(text, ref at, "null"))
                    return null;

                int start = at;
                while (at < text.Length && "+-.0123456789eE".IndexOf(text[at]) >= 0)
                    at++;
                if (at == start)
                    throw new FormatException($"unexpected '{first}' at {start}");
                return double.Parse(text.Substring(start, at - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private static bool ReadLiteral(string text, ref int at, string literal)
            {
                if (string.CompareOrdinal(text, at, literal, 0, literal.Length) != 0)
                    return false;
                at += literal.Length;
                return true;
            }

            private static string ReadString(string text, ref int at)
            {
                Expect(text, ref at, '"');
                var value = new StringBuilder();
                while (true)
                {
                    char c = text[at++];
                    if (c == '"')
                        return value.ToString();
                    if (c < ' ')
                        throw new FormatException("unescaped control character");
                    if (c != '\\')
                    {
                        value.Append(c);
                        continue;
                    }

                    char escape = text[at++];
                    switch (escape)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            value.Append(escape);
                            break;
                        case 'b':
                            value.Append('\b');
                            break;
                        case 'f':
                            value.Append('\f');
                            break;
                        case 'n':
                            value.Append('\n');
                            break;
                        case 'r':
                            value.Append('\r');
                            break;
                        case 't':
                            value.Append('\t');
                            break;
                        case 'u':
                            value.Append((char)int.Parse(text.Substring(at, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            at += 4;
                            break;
                        default:
                            throw new FormatException($"unknown escape '\\{escape}'");
                    }
                }
            }

            private static void Expect(string text, ref int at, char expected)
            {
                if (text[at] != expected)
                    throw new FormatException($"expected '{expected}' at {at}");
                at++;
            }
        }
    }
}
