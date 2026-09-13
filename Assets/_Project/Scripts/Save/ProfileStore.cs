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

        public string Directory { get; }
        public ProfileLoadStatus LastLoadStatus { get; private set; }
        // Why files were skipped during the last load, for the caller to log.
        public IReadOnlyList<string> LastLoadProblems => _problems;

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

        public PlayerProfile Load()
        {
            _problems.Clear();
            ProfileSaveData main = TryRead(_path, out bool mainExists);
            ProfileSaveData temp = TryRead(_tempPath, out bool tempExists);
            ProfileSaveData backup = TryRead(_backupPath, out bool backupExists);

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

            if (best == null)
            {
                _revision = 0;
                LastLoadStatus = mainExists || tempExists || backupExists ? ProfileLoadStatus.Reset : ProfileLoadStatus.New;
                return new PlayerProfile();
            }

            _revision = best.revision;
            LastLoadStatus = ReferenceEquals(best, main) ? ProfileLoadStatus.Loaded : ProfileLoadStatus.Recovered;
            return new PlayerProfile(best.gold, best.ownedRelicIds, best.equippedRelicId, best.deepestFloorCleared,
                best.ownedWeaponIds, best.equippedWeaponId);
        }

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
                equippedWeaponId = profile.EquippedWeaponId ?? string.Empty
            };

            System.IO.Directory.CreateDirectory(Directory);
            using (var stream = new FileStream(_tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonUtility.ToJson(data, true));
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
            _revision = data.revision;
        }

        private ProfileSaveData TryRead(string path, out bool exists)
        {
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

            ProfileSaveData data = null;
            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = JsonUtility.FromJson<ProfileSaveData>(json);
                }
                catch (ArgumentException exception)
                {
                    _problems.Add($"{Path.GetFileName(path)} is not valid JSON: {exception.Message}");
                    return null;
                }
            }

            ProfileSaveData upgraded = ProfileMigration.Upgrade(data);
            if (upgraded == null)
                _problems.Add($"{Path.GetFileName(path)} has an unsupported save version {data?.saveVersion.ToString() ?? "(empty file)"}.");
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
