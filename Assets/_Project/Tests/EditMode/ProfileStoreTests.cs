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
            store.Save(new PlayerProfile(120, new[] { "relic_second_wind", "relic_counterweight" }, "relic_counterweight", 2));

            var reopened = new ProfileStore(_directory);
            PlayerProfile loaded = reopened.Load();

            Assert.That(reopened.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(loaded.Gold, Is.EqualTo(120));
            Assert.That(loaded.OwnedRelicIds, Is.EqualTo(new[] { "relic_second_wind", "relic_counterweight" }));
            Assert.That(loaded.EquippedRelicId, Is.EqualTo("relic_counterweight"));
            Assert.That(loaded.DeepestFloorCleared, Is.EqualTo(2));
            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": 1").And.Contain("\"gold\": 120"));
            Assert.That(File.Exists(Path.Combine(_directory, ProfileStore.TempFileName)), Is.False);
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
