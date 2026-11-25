using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Cryptforge.Arena
{
    // The arena save's JSON (camelCase keys, a fixed key order, invariant numbers), written and read here so the round trip
    // is pure C# - tested on .NET and in the Editor alike - and the Unity side only moves text to and from a file
    // (ArenaSaveStore). Reading is forgiving: unknown keys are ignored, missing ones keep their defaults, wrong types are
    // skipped; a save of a newer version than this build knows is refused (null), never guessed at. Version 1 (before the
    // training rivals) is migrated: the ranked ladder becomes the training ladder and its generated rivals take their rung
    // and the handicap the rules give it (ArenaLadder.Restamp).
    public static class ArenaSaveJson
    {
        public static string Write(ArenaSaveData save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            var w = new Writer();
            w.Begin();
            w.Field("saveVersion", save.saveVersion);
            w.Field("revision", save.revision);
            w.Field("rulesVersion", save.rulesVersion);
            w.Field("seed", save.seed);
            w.Field("fights", save.fights);
            w.Key("ranked");
            Ladder(w, save.ranked);
            w.Key("open");
            Ladder(w, save.open);
            w.Key("rankedBuild");
            Snapshot(w, save.rankedBuild);
            w.Key("openBuild");
            Snapshot(w, save.openBuild);
            w.Key("pastSnapshots");
            w.Array(save.pastSnapshots, s => Snapshot(w, s));
            w.End();
            return w.ToString();
        }

        // The save the text holds, upgraded to the current version; null for text that is not a JSON object, or for a save
        // of a version this build does not know.
        public static ArenaSaveData Read(string json, ArenaRules rules = null)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            object root;
            try
            {
                root = new Reader(json).Document();
            }
            catch (FormatException)
            {
                return null;
            }
            if (!(root is Dictionary<string, object> o))
                return null;
            int version = Int(o, "saveVersion", 0);
            if (version < 1 || version > ArenaSaveData.CurrentVersion)
                return null;
            var save = new ArenaSaveData
            {
                saveVersion = ArenaSaveData.CurrentVersion,
                revision = Math.Max(0, Int(o, "revision", 0)),
                rulesVersion = Int(o, "rulesVersion", ArenaRules.ShippedVersion),
                seed = Int(o, "seed", 0),
                fights = Math.Max(0, Int(o, "fights", 0)),
                ranked = ReadLadder(Obj(o, "ranked")),
                open = ReadLadder(Obj(o, "open")),
                rankedBuild = ReadSnapshot(Obj(o, "rankedBuild")),
                openBuild = ReadSnapshot(Obj(o, "openBuild"))
            };
            foreach (object item in List(o, "pastSnapshots"))
            {
                ArenaSnapshotData snapshot = ReadSnapshot(item as Dictionary<string, object>);
                if (snapshot != null && save.pastSnapshots.Count < ArenaSaveData.MaxPastSnapshots)
                    save.pastSnapshots.Add(snapshot);
            }
            if (version == 1)
                MigrateFromOne(save, rules ?? ArenaRules.Current);
            return save;
        }

        // Whether the text is a whole arena save of a version above this build's - an older APK installed over a newer one
        // finds it - which Read refuses as it refuses a damaged one, but which must never be written over (ArenaSaveStore).
        public static bool IsNewerVersion(string json) => Version(json) > ArenaSaveData.CurrentVersion;

        // The save version the text names; 0 for text that is not a JSON object or names none.
        public static int Version(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return 0;
            object root;
            try
            {
                root = new Reader(json).Document();
            }
            catch (FormatException)
            {
                return 0;
            }
            return root is Dictionary<string, object> o ? Int(o, "saveVersion", 0) : 0;
        }

        // Version 1 had no training rivals: the ranked ladder becomes the training ladder, and each generated rival takes
        // the rung its id names ("ranked-07") and the handicap the rules give that rung. The rating stands; from here on it
        // is a training rating.
        private static void MigrateFromOne(ArenaSaveData save, ArenaRules rules)
        {
            save.ranked.training = true;
            for (int i = 0; i < save.ranked.rivals.Count; i++)
            {
                ArenaRivalData rival = save.ranked.rivals[i];
                if (rival.pastSelf)
                    continue;
                rival.rung = rival.id != null && rival.id.StartsWith("ranked-", StringComparison.Ordinal) &&
                             int.TryParse(rival.id.Substring(7), NumberStyles.None, CultureInfo.InvariantCulture, out int rung)
                    ? rung
                    : i;
            }
            ArenaLadder.Restamp(save, rules);
        }

        private static void Ladder(Writer w, ArenaLadderData ladder)
        {
            ladder = ladder ?? new ArenaLadderData();
            w.Begin();
            w.Field("training", ladder.training);
            w.Field("rating", ladder.rating);
            w.Field("wins", ladder.wins);
            w.Field("losses", ladder.losses);
            w.Field("draws", ladder.draws);
            w.Key("rivals");
            w.Array(ladder.rivals, r =>
            {
                w.Begin();
                w.Field("id", r.id);
                w.Field("name", r.name);
                w.Field("rating", r.rating);
                w.Field("wins", r.wins);
                w.Field("losses", r.losses);
                w.Field("rung", r.rung);
                w.Field("pastSelf", r.pastSelf);
                w.Key("build");
                Snapshot(w, r.build);
                w.End();
            });
            w.Key("history");
            w.Array(ladder.history, m =>
            {
                w.Begin();
                w.Field("fight", m.fight);
                w.Field("rivalId", m.rivalId);
                w.Field("seed", m.seed);
                w.Field("rulesVersion", m.rulesVersion);
                w.Field("rulesHash", m.rulesHash);
                w.Field("result", m.result);
                w.Field("roundsWon", m.roundsWon);
                w.Field("roundsLost", m.roundsLost);
                w.Field("ratingDelta", m.ratingDelta);
                w.Field("fingerprint", m.fingerprint);
                w.Key("rivalBuild");
                Snapshot(w, m.rivalBuild);
                w.Field("playerPower", m.playerPower);
                w.Field("rivalPower", m.rivalPower);
                w.Field("expected", m.expected);
                w.End();
            });
            w.End();
        }

        private static void Snapshot(Writer w, ArenaSnapshotData s)
        {
            if (s == null)
            {
                w.Null();
                return;
            }
            w.Begin();
            w.Field("mode", s.mode);
            w.Field("heroId", s.heroId);
            w.Field("weaponId", s.weaponId);
            w.Key("skillIds");
            w.Array(s.skillIds, id => w.Value(id));
            w.Field("policy", s.policy);
            w.Key("lineIds");
            w.Array(s.lineIds, id => w.Value(id));
            w.Key("gear");
            w.Array(s.gear, p =>
            {
                w.Begin();
                w.Field("slot", p.slot);
                w.Field("level", p.level);
                w.Key("lines");
                w.Array(p.lines, l =>
                {
                    w.Begin();
                    w.Field("stat", l.stat);
                    w.Field("tier", l.tier);
                    w.End();
                });
                w.End();
            });
            w.Field("handicap", s.handicap);
            w.Field("rulesVersion", s.rulesVersion);
            w.Field("hash", s.hash);
            w.Field("rating", s.rating);
            w.Field("fight", s.fight);
            w.End();
        }

        private static ArenaLadderData ReadLadder(Dictionary<string, object> o)
        {
            var ladder = new ArenaLadderData();
            if (o == null)
                return ladder;
            ladder.training = Bool(o, "training");
            ladder.rating = Int(o, "rating", ArenaLadder.StartRating);
            ladder.wins = Math.Max(0, Int(o, "wins", 0));
            ladder.losses = Math.Max(0, Int(o, "losses", 0));
            ladder.draws = Math.Max(0, Int(o, "draws", 0));
            foreach (object item in List(o, "rivals"))
            {
                if (!(item is Dictionary<string, object> r))
                    continue;
                ArenaSnapshotData build = ReadSnapshot(Obj(r, "build"));
                string id = Str(r, "id");
                if (build == null || string.IsNullOrEmpty(id))
                    continue;
                ladder.rivals.Add(new ArenaRivalData
                {
                    id = id,
                    name = Str(r, "name") ?? id,
                    rating = Int(r, "rating", ArenaLadder.StartRating),
                    wins = Math.Max(0, Int(r, "wins", 0)),
                    losses = Math.Max(0, Int(r, "losses", 0)),
                    rung = Math.Max(-1, Int(r, "rung", -1)),
                    pastSelf = Bool(r, "pastSelf"),
                    build = build
                });
            }
            foreach (object item in List(o, "history"))
            {
                if (!(item is Dictionary<string, object> m))
                    continue;
                ladder.history.Add(new ArenaMatchData
                {
                    fight = Int(m, "fight", 0),
                    rivalId = Str(m, "rivalId"),
                    seed = Int(m, "seed", 0),
                    rulesVersion = Int(m, "rulesVersion", 0),
                    rulesHash = Str(m, "rulesHash"),
                    result = Str(m, "result"),
                    roundsWon = Int(m, "roundsWon", 0),
                    roundsLost = Int(m, "roundsLost", 0),
                    ratingDelta = Int(m, "ratingDelta", 0),
                    fingerprint = Str(m, "fingerprint"),
                    rivalBuild = ReadSnapshot(Obj(m, "rivalBuild")),
                    playerPower = Int(m, "playerPower", 0),
                    rivalPower = Int(m, "rivalPower", 0),
                    expected = Math.Max(0, Math.Min(1000, Int(m, "expected", 0)))
                });
            }
            while (ladder.history.Count > ArenaSaveData.MaxHistory)
                ladder.history.RemoveAt(0);
            return ladder;
        }

        private static ArenaSnapshotData ReadSnapshot(Dictionary<string, object> o)
        {
            if (o == null || string.IsNullOrEmpty(Str(o, "heroId")))
                return null;
            var s = new ArenaSnapshotData
            {
                mode = Str(o, "mode") == "open" ? "open" : "ranked",
                heroId = Str(o, "heroId"),
                weaponId = Str(o, "weaponId") ?? string.Empty,
                policy = Str(o, "policy") == "bold" ? "bold" : "careful",
                handicap = Int(o, "handicap", ArenaBuild.NoHandicap),
                rulesVersion = Int(o, "rulesVersion", 0),
                hash = Str(o, "hash"),
                rating = Int(o, "rating", 0),
                fight = Int(o, "fight", 0)
            };
            foreach (object id in List(o, "skillIds"))
            {
                if (id is string text)
                    s.skillIds.Add(text);
            }
            foreach (object id in List(o, "lineIds"))
            {
                if (id is string text)
                    s.lineIds.Add(text);
            }
            foreach (object item in List(o, "gear"))
            {
                if (!(item is Dictionary<string, object> p))
                    continue;
                var piece = new ArenaGearPieceData { slot = Int(p, "slot", -1), level = Int(p, "level", 0) };
                foreach (object l in List(p, "lines"))
                {
                    if (l is Dictionary<string, object> line)
                        piece.lines.Add(new ArenaLineData { stat = Str(line, "stat"), tier = Int(line, "tier", 1) });
                }
                s.gear.Add(piece);
            }
            return s;
        }

        private static Dictionary<string, object> Obj(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out object value) ? value as Dictionary<string, object> : null;

        private static List<object> List(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out object value) && value is List<object> list ? list : new List<object>();

        private static string Str(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out object value) ? value as string : null;

        private static bool Bool(Dictionary<string, object> o, string key) =>
            o.TryGetValue(key, out object value) && value is bool b && b;

        private static int Int(Dictionary<string, object> o, string key, int fallback)
        {
            if (!o.TryGetValue(key, out object value) || !(value is long number))
                return fallback;
            return number < int.MinValue || number > int.MaxValue ? fallback : (int)number;
        }

        // An indented writer that knows when a comma is due.
        private sealed class Writer
        {
            private readonly StringBuilder _text = new StringBuilder(4096);
            private readonly Stack<bool> _first = new Stack<bool>();
            private int _depth;
            private bool _afterKey;

            public void Begin()
            {
                Prefix();
                _text.Append('{');
                _first.Push(true);
                _depth++;
            }

            public void End()
            {
                _depth--;
                bool empty = _first.Pop();
                if (!empty)
                    NewLine();
                _text.Append('}');
            }

            public void Key(string key)
            {
                Separator();
                NewLine();
                Quote(key);
                _text.Append(": ");
                _afterKey = true;
            }

            public void Field(string key, int value)
            {
                Key(key);
                Value(value);
            }

            public void Field(string key, string value)
            {
                Key(key);
                Value(value);
            }

            public void Field(string key, bool value)
            {
                Key(key);
                Prefix();
                _text.Append(value ? "true" : "false");
            }

            public void Value(int value)
            {
                Prefix();
                _text.Append(value.ToString(CultureInfo.InvariantCulture));
            }

            public void Value(string value)
            {
                Prefix();
                if (value == null)
                    _text.Append("null");
                else
                    Quote(value);
            }

            public void Null()
            {
                Prefix();
                _text.Append("null");
            }

            public void Array<T>(List<T> items, Action<T> write)
            {
                Prefix();
                _text.Append('[');
                _first.Push(true);
                _depth++;
                if (items != null)
                {
                    foreach (T item in items)
                    {
                        Separator();
                        NewLine();
                        _afterKey = true;
                        if (item == null)
                            Null();
                        else
                            write(item);
                    }
                }
                _depth--;
                bool empty = _first.Pop();
                if (!empty)
                    NewLine();
                _text.Append(']');
            }

            // A value directly after its key or its array slot needs nothing before it.
            private void Prefix()
            {
                if (_afterKey)
                {
                    _afterKey = false;
                    return;
                }
                Separator();
            }

            private void Separator()
            {
                if (_first.Count == 0)
                    return;
                if (_first.Peek())
                {
                    _first.Pop();
                    _first.Push(false);
                }
                else
                {
                    _text.Append(',');
                }
            }

            private void NewLine()
            {
                _text.Append('\n');
                _text.Append(' ', _depth * 2);
            }

            private void Quote(string value)
            {
                _text.Append('"');
                foreach (char c in value)
                {
                    switch (c)
                    {
                        case '"':
                            _text.Append("\\\"");
                            break;
                        case '\\':
                            _text.Append("\\\\");
                            break;
                        case '\n':
                            _text.Append("\\n");
                            break;
                        case '\r':
                            _text.Append("\\r");
                            break;
                        case '\t':
                            _text.Append("\\t");
                            break;
                        default:
                            if (c < 0x20)
                                _text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            else
                                _text.Append(c);
                            break;
                    }
                }
                _text.Append('"');
            }

            public override string ToString() => _text.ToString();
        }

        // A small strict reader: objects, arrays, strings, integers, true, false and null (the save writes no fractions).
        private sealed class Reader
        {
            private readonly string _s;
            private int _i;

            public Reader(string s)
            {
                _s = s;
            }

            public object Document()
            {
                object value = Value();
                Skip();
                if (_i != _s.Length)
                    throw new FormatException("Trailing text after the document.");
                return value;
            }

            private object Value()
            {
                Skip();
                if (_i >= _s.Length)
                    throw new FormatException("Unexpected end.");
                char c = _s[_i];
                switch (c)
                {
                    case '{':
                        return Object();
                    case '[':
                        return Array();
                    case '"':
                        return String();
                    case 't':
                        Word("true");
                        return true;
                    case 'f':
                        Word("false");
                        return false;
                    case 'n':
                        Word("null");
                        return null;
                    default:
                        return Number();
                }
            }

            private Dictionary<string, object> Object()
            {
                var o = new Dictionary<string, object>(StringComparer.Ordinal);
                _i++;
                Skip();
                if (Peek() == '}')
                {
                    _i++;
                    return o;
                }
                while (true)
                {
                    Skip();
                    if (Peek() != '"')
                        throw new FormatException("A key must be a string.");
                    string key = String();
                    Skip();
                    Expect(':');
                    o[key] = Value();
                    Skip();
                    char c = Next();
                    if (c == '}')
                        return o;
                    if (c != ',')
                        throw new FormatException("Expected , or }.");
                }
            }

            private List<object> Array()
            {
                var list = new List<object>();
                _i++;
                Skip();
                if (Peek() == ']')
                {
                    _i++;
                    return list;
                }
                while (true)
                {
                    list.Add(Value());
                    Skip();
                    char c = Next();
                    if (c == ']')
                        return list;
                    if (c != ',')
                        throw new FormatException("Expected , or ].");
                }
            }

            private string String()
            {
                Expect('"');
                var text = new StringBuilder();
                while (true)
                {
                    char c = Next();
                    if (c == '"')
                        return text.ToString();
                    if (c != '\\')
                    {
                        text.Append(c);
                        continue;
                    }
                    char e = Next();
                    switch (e)
                    {
                        case '"':
                        case '\\':
                        case '/':
                            text.Append(e);
                            break;
                        case 'n':
                            text.Append('\n');
                            break;
                        case 'r':
                            text.Append('\r');
                            break;
                        case 't':
                            text.Append('\t');
                            break;
                        case 'b':
                            text.Append('\b');
                            break;
                        case 'f':
                            text.Append('\f');
                            break;
                        case 'u':
                            if (_i + 4 > _s.Length)
                                throw new FormatException("A short \\u escape.");
                            text.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default:
                            throw new FormatException("An unknown escape.");
                    }
                }
            }

            private object Number()
            {
                int start = _i;
                if (Peek() == '-')
                    _i++;
                while (_i < _s.Length && _s[_i] >= '0' && _s[_i] <= '9')
                    _i++;
                if (_i == start || (_i == start + 1 && _s[start] == '-'))
                    throw new FormatException("Expected a value.");
                // A fraction or an exponent is read past and kept as text the fields ignore.
                bool whole = true;
                while (_i < _s.Length && (_s[_i] == '.' || _s[_i] == 'e' || _s[_i] == 'E' || _s[_i] == '+' || _s[_i] == '-' ||
                                          (_s[_i] >= '0' && _s[_i] <= '9')))
                {
                    whole = false;
                    _i++;
                }
                string text = _s.Substring(start, _i - start);
                if (whole && long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long value))
                    return value;
                return text;
            }

            private void Word(string word)
            {
                if (_i + word.Length > _s.Length || string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0)
                    throw new FormatException("Expected " + word + ".");
                _i += word.Length;
            }

            private void Skip()
            {
                while (_i < _s.Length && (_s[_i] == ' ' || _s[_i] == '\n' || _s[_i] == '\r' || _s[_i] == '\t'))
                    _i++;
            }

            private char Peek() => _i < _s.Length ? _s[_i] : '\0';

            private char Next()
            {
                if (_i >= _s.Length)
                    throw new FormatException("Unexpected end.");
                return _s[_i++];
            }

            private void Expect(char c)
            {
                if (Next() != c)
                    throw new FormatException("Expected " + c + ".");
            }
        }
    }
}
