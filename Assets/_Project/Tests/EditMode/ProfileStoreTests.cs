using System;
using System.IO;
using Cryptforge.Core;
using Cryptforge.Save;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    // Covers the 13_QA_RELEASE save cases: first launch, save/load, corrupted file, version and interrupted saves.
    public sealed class ProfileStoreTests
    {
        private string _directory;

        [SetUp]
        public void CreateFolder()
        {
            _directory = Path.Combine(Path.GetTempPath(), "cryptforge-profile-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);
        }

        [TearDown]
        public void DeleteFolder()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }

        [Test]
        public void FirstLaunchStartsAnEmptyProfileWithoutWritingFiles()
        {
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();

            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.New));
            Assert.That(profile.Gold, Is.Zero);
            Assert.That(profile.OwnedRelicIds, Is.Empty);
            Assert.That(profile.EquippedRelicId, Is.Null);
            Assert.That(Directory.GetFiles(_directory), Is.Empty);
        }

        [Test]
        public void SavedProfileRoundTripsThroughVersionedJson()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            store.Save(new PlayerProfile(120, new[] { "relic_second_wind", "relic_counterweight" }, "relic_counterweight", 2,
                new[] { "weapon_staff" }, "weapon_staff"));

            var reopened = new ProfileStore(_directory);
            PlayerProfile loaded = reopened.Load();

            Assert.That(reopened.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(loaded.Gold, Is.EqualTo(120));
            Assert.That(loaded.OwnedRelicIds, Is.EqualTo(new[] { "relic_second_wind", "relic_counterweight" }));
            Assert.That(loaded.EquippedRelicId, Is.EqualTo("relic_counterweight"));
            Assert.That(loaded.DeepestFloorCleared, Is.EqualTo(2));
            Assert.That(loaded.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(loaded.EquippedWeaponId, Is.EqualTo("weapon_staff"));
            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": 2").And.Contain("\"gold\": 120").And.Contain("\"equippedWeaponId\": \"weapon_staff\""));
            Assert.That(File.Exists(Path.Combine(_directory, ProfileStore.TempFileName)), Is.False);
        }

        [Test]
        public void VersionOneProfileLoadsWithoutWeaponsAndIsSavedAsVersionTwo()
        {
            // As the Forge meta build wrote it, before weapons could be forged.
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path, "{\n    \"saveVersion\": 1,\n    \"revision\": 5,\n    \"gold\": 90,\n    \"ownedRelicIds\": [\n" +
                "        \"relic_second_wind\"\n    ],\n    \"equippedRelicId\": \"relic_second_wind\",\n    \"deepestFloorCleared\": 2\n}");

            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();

            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(profile.Gold, Is.EqualTo(90));
            Assert.That(profile.OwnedRelicIds, Is.EqualTo(new[] { "relic_second_wind" }));
            Assert.That(profile.EquippedRelicId, Is.EqualTo("relic_second_wind"));
            Assert.That(profile.DeepestFloorCleared, Is.EqualTo(2));
            Assert.That(profile.OwnedWeaponIds, Is.Empty);
            Assert.That(profile.EquippedWeaponId, Is.Null, "The hero keeps the starting weapon.");

            Assert.That(profile.TryForgeWeapon("weapon_staff", 60), Is.True);
            store.Save(profile);

            string json = File.ReadAllText(path);
            Assert.That(json, Does.Contain("\"saveVersion\": 2").And.Contain("\"revision\": 6").And.Contain("\"gold\": 30"));
            Assert.That(File.ReadAllText(Path.Combine(_directory, ProfileStore.BackupFileName)), Does.Contain("\"saveVersion\": 1"),
                "The version 1 file stays behind as the backup.");
            PlayerProfile reloaded = new ProfileStore(_directory).Load();
            Assert.That(reloaded.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(reloaded.EquippedWeaponId, Is.EqualTo("weapon_staff"));
        }

        [Test]
        public void CorruptProfileRecoversTheBackupAndKeepsTheDamagedFile()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            store.Save(new PlayerProfile(10, null, null, 0));
            store.Save(new PlayerProfile(20, null, null, 1));
            File.WriteAllText(Path.Combine(_directory, ProfileStore.FileName), "{\"saveVersion\":1,\"revis");

            var reopened = new ProfileStore(_directory);
            PlayerProfile recovered = reopened.Load();

            Assert.That(reopened.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Recovered));
            Assert.That(recovered.Gold, Is.EqualTo(10), "The previous save survives in the backup.");
            Assert.That(reopened.LastLoadProblems, Has.Some.Contains(ProfileStore.FileName));
            Assert.That(File.ReadAllText(Path.Combine(_directory, ProfileStore.UnreadableFileName)), Does.StartWith("{\"saveVersion\":1,\"revis"));

            reopened.Save(new PlayerProfile(30, null, null, 1));
            var afterRepair = new ProfileStore(_directory);
            Assert.That(afterRepair.Load().Gold, Is.EqualTo(30));
            Assert.That(afterRepair.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
        }

        [Test]
        public void InterruptedSaveLoadsTheNewestCompleteFile()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            store.Save(new PlayerProfile(10, null, null, 0));
            store.Save(new PlayerProfile(20, null, null, 0));
            string temp = Path.Combine(_directory, ProfileStore.TempFileName);

            // Killed while writing the temp file: the main file is still the newest readable one.
            File.WriteAllText(temp, "{\"saveVersion\":1,\"revision\":3,\"go");
            var halfWritten = new ProfileStore(_directory);
            Assert.That(halfWritten.Load().Gold, Is.EqualTo(20));
            Assert.That(halfWritten.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));

            // Killed after the temp file was flushed and the main file moved to the backup, before the final move.
            File.WriteAllText(temp, JsonUtility.ToJson(new ProfileSaveData { saveVersion = 1, revision = 3, gold = 40 }));
            File.Delete(Path.Combine(_directory, ProfileStore.BackupFileName));
            File.Move(Path.Combine(_directory, ProfileStore.FileName), Path.Combine(_directory, ProfileStore.BackupFileName));
            var betweenMoves = new ProfileStore(_directory);
            Assert.That(betweenMoves.Load().Gold, Is.EqualTo(40));
            Assert.That(betweenMoves.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Recovered));

            betweenMoves.Save(new PlayerProfile(50, null, null, 0));
            Assert.That(new ProfileStore(_directory).Load().Gold, Is.EqualTo(50), "Saving continues after the newest revision.");
        }

        // A save killed while writing can leave a temp file cut at any character; none of those files may replace the last
        // complete save, even when the cut keeps a higher revision.
        [Test]
        public void EveryTruncatedTempFileLosesToTheLastCompleteSave()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            store.Save(new PlayerProfile(70, new[] { "relic_second_wind" }, "relic_second_wind", 1, new[] { "weapon_staff" }, "weapon_staff"));
            string newer = JsonUtility.ToJson(new ProfileSaveData
            {
                saveVersion = ProfileSaveData.CurrentVersion,
                revision = 9,
                gold = 999,
                ownedRelicIds = new string[0],
                equippedRelicId = string.Empty,
                deepestFloorCleared = 2,
                ownedWeaponIds = new string[0],
                equippedWeaponId = string.Empty
            }, true);
            string temp = Path.Combine(_directory, ProfileStore.TempFileName);

            for (int length = 0; length < newer.Length; length++)
            {
                File.WriteAllText(temp, newer.Substring(0, length));
                PlayerProfile loaded = new ProfileStore(_directory).Load();
                Assert.That(loaded.Gold, Is.EqualTo(70), $"A temp file cut after {length} of {newer.Length} characters was loaded.");
                Assert.That(loaded.EquippedWeaponId, Is.EqualTo("weapon_staff"));
            }

            File.WriteAllText(temp, newer);
            Assert.That(new ProfileStore(_directory).Load().Gold, Is.EqualTo(999), "The complete newer temp file still wins.");
        }

        // Storage damage can cut the main file too; the backup must win over any partial main file.
        [Test]
        public void EveryTruncatedMainFileFallsBackToTheBackup()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            store.Save(new PlayerProfile(40, null, null, 0));
            store.Save(new PlayerProfile(90, new[] { "relic_counterweight" }, "relic_counterweight", 2, new[] { "weapon_daggers" }, "weapon_daggers"));
            string main = Path.Combine(_directory, ProfileStore.FileName);
            string complete = File.ReadAllText(main);

            for (int length = 0; length < complete.TrimEnd().Length; length++)
            {
                File.WriteAllText(main, complete.Substring(0, length));
                PlayerProfile loaded = new ProfileStore(_directory).Load();
                Assert.That(loaded.Gold, Is.EqualTo(40), $"A main file cut after {length} of {complete.Length} characters was loaded.");
            }
        }

        [Test]
        public void SaveFromANewerBuildIsNotReadAndIsKept()
        {
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path, JsonUtility.ToJson(new ProfileSaveData { saveVersion = 99, revision = 7, gold = 900 }));

            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();

            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Reset));
            Assert.That(profile.Gold, Is.Zero);
            Assert.That(store.LastLoadProblems, Has.Some.Contains("99"));
            Assert.That(File.ReadAllText(Path.Combine(_directory, ProfileStore.UnreadableFileName)), Does.Contain("900"));
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 0 }), Is.Null, "A missing version is never guessed.");
        }

        [Test]
        public void DamagedValuesInAReadableSaveAreRepaired()
        {
            File.WriteAllText(Path.Combine(_directory, ProfileStore.FileName),
                "{\"saveVersion\":1,\"revision\":4,\"gold\":-50,\"ownedRelicIds\":[\"relic_a\",\"relic_a\"],\"equippedRelicId\":\"relic_b\"}");

            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();

            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(profile.Gold, Is.Zero);
            Assert.That(profile.OwnedRelicIds, Is.EqualTo(new[] { "relic_a" }));
            Assert.That(profile.EquippedRelicId, Is.Null);
            Assert.That(profile.DeepestFloorCleared, Is.Zero, "A missing field reads as its default.");
        }
    }
}
