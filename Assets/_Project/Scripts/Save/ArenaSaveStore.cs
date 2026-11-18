using System;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Arena;

namespace Cryptforge.Save
{
    // The arena save's file beside the profile (ProfileLocation.Resolve() is the folder the view passes), kept as thin as
    // the profile's store and in its shape: a save writes and flushes a temp file, moves the previous file to a backup,
    // then moves the temp file into place; a load takes the readable file with the highest revision of the three. The text
    // is ArenaSaveJson's; nothing here knows the arena's rules. A missing or unreadable arena save never touches the
    // profile: the arena starts afresh.
    public sealed class ArenaSaveStore
    {
        public const string FileName = "arena.json";
        public const string TempFileName = FileName + ".tmp";
        public const string BackupFileName = FileName + ".bak";

        private readonly string _path;
        private readonly string _tempPath;
        private readonly string _backupPath;
        private readonly List<string> _problems = new List<string>();

        public string Directory { get; }
        // Why files were skipped during the last load, for the caller to log.
        public IReadOnlyList<string> LastLoadProblems => _problems;
        // True when the last load found no readable save and made a new one.
        public bool LastLoadWasNew { get; private set; }

        public ArenaSaveStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("An arena save directory is required.", nameof(directory));
            Directory = directory;
            _path = Path.Combine(directory, FileName);
            _tempPath = Path.Combine(directory, TempFileName);
            _backupPath = Path.Combine(directory, BackupFileName);
        }

        // The newest readable save; a new one (ArenaLadder.NewSave with newSeed) when there is none.
        public ArenaSaveData Load(int newSeed)
        {
            _problems.Clear();
            ArenaSaveData best = Newest(Newest(TryRead(_path), TryRead(_tempPath)), TryRead(_backupPath));
            LastLoadWasNew = best == null;
            if (best == null)
                return ArenaLadder.NewSave(newSeed);
            // A save fought under other rules takes the current table's training handicaps (docs/55 section 10.10).
            if (best.rulesVersion != ArenaRules.Current.Version)
                ArenaLadder.Restamp(best);
            return best;
        }

        public void Save(ArenaSaveData save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            save.revision++;
            save.saveVersion = ArenaSaveData.CurrentVersion;
            System.IO.Directory.CreateDirectory(Directory);
            using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(ArenaSaveJson.Write(save));
                writer.Flush();
                stream.Flush(true);
            }
            if (File.Exists(_path))
            {
                if (File.Exists(_backupPath))
                    File.Delete(_backupPath);
                File.Move(_path, _backupPath);
            }
            File.Move(_tempPath, _path);
        }

        private ArenaSaveData TryRead(string path)
        {
            if (!File.Exists(path))
                return null;
            string json;
            try
            {
                json = File.ReadAllText(path);
            }
            catch (IOException exception)
            {
                _problems.Add($"{Path.GetFileName(path)} could not be read: {exception.Message}");
                return null;
            }
            ArenaSaveData data = ArenaSaveJson.Read(json);
            if (data == null)
                _problems.Add($"{Path.GetFileName(path)} is not a readable arena save of version {ArenaSaveData.CurrentVersion} or older.");
            return data;
        }

        private static ArenaSaveData Newest(ArenaSaveData a, ArenaSaveData b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;
            return b.revision > a.revision ? b : a;
        }
    }
}
