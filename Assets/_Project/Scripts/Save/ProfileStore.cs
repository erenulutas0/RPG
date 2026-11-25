using System;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Core;
using UnityEngine;

namespace Cryptforge.Save
{
    // Stores the profile as versioned JSON in one folder. A save writes and flushes a temp file, moves the previous
    // profile to a backup, then moves the temp file into place. Loading takes the readable file with the highest
    // revision among the three, so an interrupted save or one damaged file costs at most the latest change.
    // A file written by a newer build (a save version above this build's) is not damaged - an older APK installed over a
    // newer one finds it - so it is never read, and while one stands among the three the store is read only: Save writes,
    // moves and deletes nothing (NewerVersion, IsReadOnly), and the newer build finds its profile whole when it returns.
    // A save that could not write (read only, or the phone's storage full) leaves the profile in memory: UnsavedProfile
    // holds it, and LoadSession, the scene's load, hands it back after a reload instead of the older file.
    public sealed class ProfileStore
    {
        public const string FileName = "profile.json";
        public const string TempFileName = FileName + ".tmp";
        public const string BackupFileName = FileName + ".bak";
        public const string UnreadableFileName = FileName + ".unreadable";

        private readonly string _path;
        private readonly string _tempPath;
        private readonly string _backupPath;
        private readonly string _unreadablePath;
        private readonly List<string> _problems = new List<string>();
        private int _revision;
        private bool _readOnlyNoted;

        public string Directory { get; }
        public ProfileLoadStatus LastLoadStatus { get; private set; }
        // Why files were skipped during the last load, and that a save wrote nothing while the store is read only, for the
        // caller to log.
        public IReadOnlyList<string> LastLoadProblems => _problems;
        // A newer build's file stands among the three (LastLoadStatus NewerVersion): Save writes nothing this session.
        public bool IsReadOnly { get; private set; }
        // The last load (LoadSession) handed back the profile a save of this session could not write, not the disk's.
        public bool LastLoadWasUnsaved { get; private set; }
        // The revision the store last read or wrote; the next save writes the one after it.
        public int Revision => _revision;

        public ProfileStore(string directory)
        {
            if (string.IsNullOrEmpty(directory))
                throw new ArgumentException("A profile directory is required.", nameof(directory));

            Directory = directory;
            _path = Path.Combine(directory, FileName);
            _tempPath = Path.Combine(directory, TempFileName);
            _backupPath = Path.Combine(directory, BackupFileName);
            _unreadablePath = Path.Combine(directory, UnreadableFileName);
        }

        // The profile on disk.
        public PlayerProfile Load()
        {
            _problems.Clear();
            LastLoadWasUnsaved = false;
            ProfileSaveData main = TryRead(_path, out bool mainExists, out bool mainNewer);
            ProfileSaveData temp = TryRead(_tempPath, out bool tempExists, out bool tempNewer);
            ProfileSaveData backup = TryRead(_backupPath, out bool backupExists, out bool backupNewer);

            ProfileSaveData best = Newest(Newest(main, temp), backup);
            // An unreadable main file is kept aside before the next save rotates it into the backup slot.
            if (mainExists && main == null)
            {
                try
                {
                    File.Copy(_path, _unreadablePath, true);
                }
                catch (IOException exception)
                {
                    _problems.Add($"{FileName} could not be kept as {UnreadableFileName}: {exception.Message}");
                }
            }

            // A newer build's file is not this build's to read or to replace: none of the three is read, and nothing is
            // written until a build that reads it returns.
            IsReadOnly = mainNewer || tempNewer || backupNewer;
            if (IsReadOnly)
            {
                _revision = 0;
                LastLoadStatus = ProfileLoadStatus.NewerVersion;
                return new PlayerProfile();
            }

            if (best == null)
            {
                _revision = 0;
                LastLoadStatus = mainExists || tempExists || backupExists ? ProfileLoadStatus.Reset : ProfileLoadStatus.New;
                return new PlayerProfile();
            }

            _revision = best.revision;
            LastLoadStatus = ReferenceEquals(best, main) ? ProfileLoadStatus.Loaded : ProfileLoadStatus.Recovered;
            return Restore(best);
        }

        // The scene's load (CombatSetup): the disk's profile, or the one a save of this session could not write when
        // UnsavedProfile holds one for this folder - a reload never goes back to the older file. LastLoadStatus and
        // IsReadOnly are the disk's either way.
        public PlayerProfile LoadSession()
        {
            PlayerProfile loaded = Load();
            if (!UnsavedProfile.TryGet(Directory, out string json, out int revision))
                return loaded;
            ProfileSaveData unsaved = Parse(json, "The unsaved profile", out _);
            if (unsaved == null)
            {
                UnsavedProfile.Release(Directory);
                return loaded;
            }
            if (!IsReadOnly)
                _revision = Math.Max(_revision, revision);
            LastLoadWasUnsaved = true;
            return Restore(unsaved);
        }

        // Writes the profile. While the store is read only nothing is written: the profile is held in memory
        // (UnsavedProfile) and the problems say so once. A write that fails holds it too, then throws IOException or
        // UnauthorizedAccessException; the first write that lands releases it.
        public void Save(PlayerProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            var data = new ProfileSaveData
            {
                saveVersion = ProfileSaveData.CurrentVersion,
                revision = _revision + 1,
                gold = profile.Gold,
                ownedRelicIds = ToArray(profile.OwnedRelicIds),
                equippedRelicId = profile.EquippedRelicId ?? string.Empty,
                deepestFloorCleared = profile.DeepestFloorCleared,
                ownedWeaponIds = ToArray(profile.OwnedWeaponIds),
                equippedWeaponId = profile.EquippedWeaponId ?? string.Empty,
                unlockedHeroIds = ToArray(profile.UnlockedHeroIds),
                selectedHeroId = profile.SelectedHeroId ?? string.Empty
            };
            ProfileGearSave.Write(profile.Gear.Snapshot(), data);
            ProfileTiersSave.Write(profile.Tiers.Snapshot(), data);
            ProfileSkillsSave.Write(profile.Skills.Snapshot(), data);
            string json = JsonUtility.ToJson(data, true);

            if (IsReadOnly)
            {
                UnsavedProfile.Hold(Directory, json, _revision);
                if (!_readOnlyNoted)
                {
                    _readOnlyNoted = true;
                    _problems.Add($"Nothing was written to {FileName}: a newer build's profile stands in {Directory}, and this " +
                                  "session's progress is kept in memory only.");
                }
                return;
            }

            try
            {
                System.IO.Directory.CreateDirectory(Directory);
                using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(json);
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
                UnsavedProfile.Hold(Directory, json, _revision);
                throw;
            }
            _revision = data.revision;
            UnsavedProfile.Release(Directory);
        }

        private static PlayerProfile Restore(ProfileSaveData data) =>
            new PlayerProfile(data.gold, data.ownedRelicIds, data.equippedRelicId, data.deepestFloorCleared,
                data.ownedWeaponIds, data.equippedWeaponId, data.unlockedHeroIds, data.selectedHeroId,
                ProfileGear.Restore(ProfileGearSave.ToSnapshot(data)),
                ProfileTiers.Restore(ProfileTiersSave.ToSnapshot(data), data.deepestFloorCleared),
                ProfileSkills.Restore(ProfileSkillsSave.ToSnapshot(data), data.deepestFloorCleared));

        private ProfileSaveData TryRead(string path, out bool exists, out bool newer)
        {
            newer = false;
            exists = File.Exists(path);
            if (!exists)
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
            return Parse(json, Path.GetFileName(path), out newer);
        }

        // The data the text holds, in the current schema; null, with the reason in the problems, when this build cannot
        // read it. newer: the text is a save of a version above this build's (whole, not damaged).
        private ProfileSaveData Parse(string json, string name, out bool newer)
        {
            newer = false;
            ProfileSaveData data = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = JsonUtility.FromJson<ProfileSaveData>(json);
                }
                catch (ArgumentException exception)
                {
                    _problems.Add($"{name} is not valid JSON: {exception.Message}");
                    return null;
                }
            }

            if (data != null && data.saveVersion > ProfileSaveData.CurrentVersion)
            {
                newer = true;
                _problems.Add($"{name} was written by a newer build (save version {data.saveVersion}; this build reads up to " +
                              $"{ProfileSaveData.CurrentVersion}): it is kept as it is, and nothing is written this session.");
                return null;
            }

            ProfileSaveData upgraded = ProfileMigration.Upgrade(data);
            if (upgraded == null)
                _problems.Add($"{name} has an unsupported save version {data?.saveVersion.ToString() ?? "(empty file)"}.");
            return upgraded;
        }

        private static string[] ToArray(IReadOnlyList<string> ids)
        {
            var array = new string[ids.Count];
            for (int i = 0; i < array.Length; i++)
                array[i] = ids[i];
            return array;
        }

        private static ProfileSaveData Newest(ProfileSaveData a, ProfileSaveData b)
        {
            if (a == null)
                return b;
            if (b == null)
                return a;
            return b.revision > a.revision ? b : a;
        }
    }
}
