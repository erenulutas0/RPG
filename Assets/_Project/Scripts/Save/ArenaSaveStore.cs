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
    // A copy written by a newer build (ArenaSaveJson.IsNewerVersion) is not damaged: it is never read, and while one stands
    // the store is read only - Save writes, moves and deletes nothing (IsReadOnly) - so the newer build finds its arena
    // whole. A save that could not write (read only, or a failed write) is held in memory for its folder until a save
    // lands, and Load hands it back: the arena screen is made anew, and loads, each time it opens, and must not go back to
    // the older file while the session's fights are unsaved. Held statically, as text, and never past the app's life.
    public sealed class ArenaSaveStore
    {
        public const string FileName = "arena.json";
        public const string TempFileName = FileName + ".tmp";
        public const string BackupFileName = FileName + ".bak";

        private static string _unsavedDirectory;
        private static string _unsavedText;

        private readonly string _path;
        private readonly string _tempPath;
        private readonly string _backupPath;
        private readonly List<string> _problems = new List<string>();
        private bool _readOnlyNoted;

        public string Directory { get; }
        // Why files were skipped during the last load, and that a save wrote nothing while the store is read only, for the
        // caller to log.
        public IReadOnlyList<string> LastLoadProblems => _problems;
        // True when the last load found no readable save and made a new one.
        public bool LastLoadWasNew { get; private set; }
        // A newer build's arena save stands among the three: Save writes nothing this session.
        public bool IsReadOnly { get; private set; }
        // The last load handed back the save a write of this session could not land, not the disk's.
        public bool LastLoadWasUnsaved { get; private set; }

        // Whether a save that could not be written waits for this folder (for tests).
        public static bool HoldsUnsaved(string directory) =>
            _unsavedText != null && string.Equals(_unsavedDirectory, directory, StringComparison.Ordinal);

        public ArenaSaveStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("An arena save directory is required.", nameof(directory));
            Directory = directory;
            _path = Path.Combine(directory, FileName);
            _tempPath = Path.Combine(directory, TempFileName);
            _backupPath = Path.Combine(directory, BackupFileName);
        }

        // The newest readable save; a new one (ArenaLadder.NewSave with newSeed) when there is none, or when a newer build's
        // save stands. The session's unsaved save, when one is held for this folder, comes before either.
        public ArenaSaveData Load(int newSeed)
        {
            _problems.Clear();
            IsReadOnly = false;
            LastLoadWasUnsaved = false;
            ArenaSaveData best = Newest(Newest(TryRead(_path), TryRead(_tempPath)), TryRead(_backupPath));
            // TryRead marks the store read only on a newer build's copy; none of the three is read then.
            if (IsReadOnly)
                best = null;
            if (HoldsUnsaved(Directory))
            {
                ArenaSaveData unsaved = ArenaSaveJson.Read(_unsavedText);
                if (unsaved != null)
                {
                    best = unsaved;
                    LastLoadWasUnsaved = true;
                }
            }
            LastLoadWasNew = best == null;
            if (best == null)
                return ArenaLadder.NewSave(newSeed);
            // A save fought under other rules takes the current table's training handicaps (docs/55 section 10.10).
            if (best.rulesVersion != ArenaRules.Current.Version)
                ArenaLadder.Restamp(best);
            return best;
        }

        // Bumps the revision and writes. While the store is read only nothing is written: the save is held in memory and
        // the problems say so once. A write that fails holds it too, then throws IOException or
        // UnauthorizedAccessException; the first write that lands releases it.
        public void Save(ArenaSaveData save)
        {
            if (save == null)
                throw new ArgumentNullException(nameof(save));
            save.revision++;
            save.saveVersion = ArenaSaveData.CurrentVersion;
            string text = ArenaSaveJson.Write(save);
            if (IsReadOnly)
            {
                Hold(text);
                if (!_readOnlyNoted)
                {
                    _readOnlyNoted = true;
                    _problems.Add($"Nothing was written to {FileName}: a newer build's arena save stands in {Directory}, and " +
                                  "this session's arena is kept in memory only.");
                }
                return;
            }
            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(text);
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
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                Hold(text);
                throw;
            }
            if (HoldsUnsaved(Directory))
                ForgetUnsaved();
        }

        // Drops the unsaved save whatever its folder (a new launch, a test's new profile folder).
        public static void ForgetUnsaved()
        {
            _unsavedDirectory = null;
            _unsavedText = null;
        }

        private void Hold(string text)
        {
            _unsavedDirectory = Directory;
            _unsavedText = text;
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
            if (data != null)
                return data;
            if (ArenaSaveJson.IsNewerVersion(json))
            {
                IsReadOnly = true;
                _problems.Add($"{Path.GetFileName(path)} was written by a newer build (save version " +
                              $"{ArenaSaveJson.Version(json)}; this build reads up to {ArenaSaveData.CurrentVersion}): it is " +
                              "kept as it is, and nothing is written this session.");
                return null;
            }
            _problems.Add($"{Path.GetFileName(path)} is not a readable arena save of version {ArenaSaveData.CurrentVersion} or older.");
            return null;
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
