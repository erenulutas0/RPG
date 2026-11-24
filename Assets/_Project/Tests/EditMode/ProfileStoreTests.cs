using System;
using System.IO;
using Cryptforge.Core;
using Cryptforge.Progression;
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
            Assert.That(json, Does.Contain("\"saveVersion\": 6").And.Contain("\"gold\": 120").And.Contain("\"equippedWeaponId\": \"weapon_staff\""));
            Assert.That(File.Exists(Path.Combine(_directory, ProfileStore.TempFileName)), Is.False);
        }

        // Version 3 (docs/45): the heroes unlocked by play and the one chosen for the next run.
        [Test]
        public void HeroesRoundTripAndAVersionTwoProfileLoadsWithTheDefaultHero()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            var profile = new PlayerProfile(10, null, null, 1, null, null, new[] { "hero_cinder_arcanist", "hero_cinder_arcanist", "" },
                "hero_cinder_arcanist");
            Assert.That(profile.UnlockedHeroIds, Is.EqualTo(new[] { "hero_cinder_arcanist" }), "Duplicates and blanks dropped.");
            store.Save(profile);
            PlayerProfile loaded = new ProfileStore(_directory).Load();
            Assert.That(loaded.UnlockedHeroIds, Is.EqualTo(new[] { "hero_cinder_arcanist" }));
            Assert.That(loaded.SelectedHeroId, Is.EqualTo("hero_cinder_arcanist"));
            Assert.That(loaded.UnlockHero("hero_cinder_arcanist"), Is.False, "Already unlocked.");
            Assert.That(loaded.SelectHero(""), Is.True, "Back to the default.");
            Assert.That(loaded.SelectedHeroId, Is.Null);
            Assert.That(loaded.SelectHero(null), Is.False, "Nothing changed.");

            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path, "{\"saveVersion\":2,\"revision\":9,\"gold\":75,\"ownedRelicIds\":[],\"equippedRelicId\":\"\"," +
                "\"deepestFloorCleared\":3,\"ownedWeaponIds\":[\"weapon_staff\"],\"equippedWeaponId\":\"weapon_staff\"}");
            File.Delete(Path.Combine(_directory, ProfileStore.BackupFileName));
            PlayerProfile old = new ProfileStore(_directory).Load();
            Assert.That((old.Gold, old.DeepestFloorCleared, old.EquippedWeaponId), Is.EqualTo((75, 3, "weapon_staff")));
            Assert.That(old.UnlockedHeroIds, Is.Empty, "Version 2 predates heroes.");
            Assert.That(old.SelectedHeroId, Is.Null, "The default hero.");
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 2 }).saveVersion, Is.EqualTo(ProfileSaveData.CurrentVersion));
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

            // The forge sells relics alone since its weapon rack went (docs/63): a relic is the purchase saved here.
            Assert.That(profile.TryForgeRelic("relic_counterweight", 60), Is.True);
            store.Save(profile);

            string json = File.ReadAllText(path);
            Assert.That(json, Does.Contain("\"saveVersion\": 6").And.Contain("\"revision\": 6").And.Contain("\"gold\": 30"));
            Assert.That(File.ReadAllText(Path.Combine(_directory, ProfileStore.BackupFileName)), Does.Contain("\"saveVersion\": 1"),
                "The version 1 file stays behind as the backup.");
            PlayerProfile reloaded = new ProfileStore(_directory).Load();
            Assert.That(reloaded.OwnedRelicIds, Is.EquivalentTo(new[] { "relic_second_wind", "relic_counterweight" }));
            Assert.That(reloaded.OwnedWeaponIds, Is.Empty);
        }

        // The forge's weapon rack is gone (docs/63, 2026-11-23) but a save that recorded its weapons keeps them: loaded,
        // saved again and reloaded, with no version bump. No run reads them; nothing is lost.
        [Test]
        public void TheRetiredRacksWeaponsSurviveALoadAndASave()
        {
            var store = new ProfileStore(_directory);
            store.Load();
            store.Save(new PlayerProfile(40, null, null, 1, new[] { "weapon_staff", "weapon_daggers" }, "weapon_daggers"));

            PlayerProfile loaded = new ProfileStore(_directory).Load();
            Assert.That(loaded.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff", "weapon_daggers" }));
            Assert.That(loaded.EquippedWeaponId, Is.EqualTo("weapon_daggers"));
            var again = new ProfileStore(_directory);
            PlayerProfile kept = again.Load();
            kept.Deposit(5);
            again.Save(kept);
            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": " + ProfileSaveData.CurrentVersion), "No version bump for the rack's going.");
            Assert.That(json, Does.Contain("weapon_staff").And.Contain("weapon_daggers"));
            PlayerProfile reloaded = new ProfileStore(_directory).Load();
            Assert.That(reloaded.Gold, Is.EqualTo(45));
            Assert.That(reloaded.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff", "weapon_daggers" }));
            Assert.That(reloaded.EquippedWeaponId, Is.EqualTo("weapon_daggers"));
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

        // Version 4 (docs/54 section 10): the bag, a weapon's upgrades, what is equipped, the materials, boosters and the
        // anvil's stream survive the JSON round trip; a shown reroll survives too.
        [Test]
        public void GearRoundTripsThroughVersionFour()
        {
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            profile.Deposit(5000);
            var anvil = new Anvil(profile, () => 77);
            ItemInstance ring = ProfileGearTests.Give(profile, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Epic);
            Assert.That(anvil.TryEquip(ring), Is.True);
            Assert.That(anvil.TryStrike(ring, false, false, out _), Is.True);
            ItemInstance sword = anvil.GearForWeapon("weapon_sword");
            Assert.That(anvil.TryReroll(sword, false), Is.True);
            store.Save(profile);

            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": 6").And.Contain("\"itemId\": \"item_cinder_ring\"")
                .And.Contain("\"weaponId\": \"weapon_sword\"").And.Contain("\"anvilSeed\": 77"));
            PlayerProfile loaded = new ProfileStore(_directory).Load();
            Assert.That(loaded.Gold, Is.EqualTo(profile.Gold));
            Assert.That(ProfileGearTests.Describe(loaded.Gear), Is.EqualTo(ProfileGearTests.Describe(profile.Gear)));
            Assert.That(loaded.Gear.Equipped(GearSlot.Ring).ItemId, Is.EqualTo("item_cinder_ring"));
            Assert.That(loaded.Gear.PendingRerollPiece, Is.SameAs(loaded.Gear.WeaponGear("weapon_sword")));
        }

        // A version 3 save (before gear) loads with its gold and an empty bag, and is written back as the current version.
        [Test]
        public void AVersionThreeProfileKeepsItsGoldAndGainsAnEmptyBag()
        {
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path, "{\"saveVersion\":3,\"revision\":12,\"gold\":39491,\"ownedRelicIds\":[],\"equippedRelicId\":\"\"," +
                "\"deepestFloorCleared\":3,\"ownedWeaponIds\":[],\"equippedWeaponId\":\"\",\"unlockedHeroIds\":[],\"selectedHeroId\":\"\"}");
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(profile.Gold, Is.EqualTo(39491), "The owner's gold stays (the lead, 2026-11-16).");
            Assert.That(profile.Gear.Bag, Is.Empty);
            Assert.That(profile.Gear.Weapons, Is.Empty);
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.Zero);
            Assert.That(profile.Gear.AnvilSeed, Is.Zero, "Seeded on the first draw.");
            profile.Deposit(1);
            store.Save(profile);
            Assert.That(File.ReadAllText(path), Does.Contain("\"saveVersion\": 6").And.Contain("\"items\": []"));
        }

        // Version 5 (docs/54 section 21): each difficulty tier's record and the tier chosen survive the JSON round trip.
        [Test]
        public void TiersRoundTripThroughVersionFive()
        {
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            profile.RecordFloorCleared(4);
            Assert.That(profile.RecordDescentCompleted(DifficultyTier.Forged), Is.True);
            profile.RecordFloorCleared(4, DifficultyTier.Tempered);
            Assert.That(profile.RecordDescentCompleted(DifficultyTier.Tempered), Is.True);
            profile.RecordFloorCleared(2, DifficultyTier.Inferno);
            Assert.That(profile.SelectTier(DifficultyTier.Inferno), Is.True);
            store.Save(profile);

            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": 6").And.Contain("\"tierProgress\"").And.Contain("\"tier\": \"inferno\"")
                .And.Contain("\"selectedTier\": 2").And.Contain("\"highestTierCompleted\": 2"));
            PlayerProfile loaded = new ProfileStore(_directory).Load();
            Assert.That(loaded.Tiers.Selected, Is.EqualTo(DifficultyTier.Inferno));
            Assert.That(loaded.Tiers.HasCompleted(DifficultyTier.Tempered), Is.True);
            Assert.That(loaded.Tiers.HasCompleted(DifficultyTier.Inferno), Is.False);
            Assert.That(loaded.Tiers.DeepestFloorCleared(DifficultyTier.Inferno), Is.EqualTo(2));
            Assert.That(loaded.Tiers.IsMapOpen(3, DifficultyTier.Inferno), Is.True, "Inferno reached floor 3's start");
            Assert.That(loaded.Tiers.IsOpen(DifficultyTier.Abyssal), Is.False);
            Assert.That(loaded.DeepestFloorCleared, Is.EqualTo(4));
        }

        // A version 4 save (the tiers stored ahead of their build) loads with its depth record as Forged's: the owner's
        // Descents walked out of open Tempered; a profile short of the finale opens nothing new. Written back as version 6.
        [Test]
        public void AVersionFourProfileCarriesItsDepthToForged()
        {
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path, "{\"saveVersion\":4,\"revision\":30,\"gold\":39491,\"deepestFloorCleared\":4,\"items\":[]," +
                "\"highestTierCompleted\":0,\"selectedTier\":0}");
            var store = new ProfileStore(_directory);
            PlayerProfile veteran = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(veteran.Gold, Is.EqualTo(39491), "gold untouched");
            Assert.That(veteran.Tiers.HasCompleted(DifficultyTier.Forged), Is.True);
            Assert.That(veteran.Tiers.IsOpen(DifficultyTier.Tempered), Is.True);
            Assert.That(veteran.Tiers.IsOpen(DifficultyTier.Inferno), Is.False);
            Assert.That(veteran.Tiers.Selected, Is.EqualTo(DifficultyTier.Forged));
            veteran.Deposit(1);
            store.Save(veteran);
            Assert.That(File.ReadAllText(path), Does.Contain("\"saveVersion\": 6").And.Contain("\"tier\": \"forged\"")
                .And.Contain("\"highestTierCompleted\": 1"));

            File.WriteAllText(path, "{\"saveVersion\":4,\"revision\":40,\"gold\":5,\"deepestFloorCleared\":2,\"selectedTier\":3}");
            File.Delete(Path.Combine(_directory, ProfileStore.BackupFileName));
            PlayerProfile midway = new ProfileStore(_directory).Load();
            Assert.That(midway.Tiers.IsOpen(DifficultyTier.Tempered), Is.False);
            Assert.That(midway.Tiers.Selected, Is.EqualTo(DifficultyTier.Forged), "a locked choice falls back");
            Assert.That(midway.Tiers.IsMapOpen(3, DifficultyTier.Forged), Is.True, "floor 2 cleared opens the Gardens on Forged");
        }

        // Damaged tiers in a readable version 5 save are repaired, never rejected (ProfileTiers.Restore): an unknown or
        // repeated tier dropped, a negative depth zeroed, a tier whose tier below is not completed keeps nothing, a locked
        // choice falls back to Forged.
        [Test]
        public void DamagedTiersInAReadableSaveAreRepaired()
        {
            File.WriteAllText(Path.Combine(_directory, ProfileStore.FileName),
                "{\"saveVersion\":5,\"revision\":2,\"gold\":10,\"deepestFloorCleared\":3,\"selectedTier\":2,\"tierProgress\":[" +
                "{\"tier\":\"tempered\",\"deepestFloorCleared\":3,\"completed\":true}," +
                "{\"tier\":\"forged\",\"deepestFloorCleared\":-5,\"completed\":false}," +
                "{\"tier\":\"forged\",\"deepestFloorCleared\":4,\"completed\":true}," +
                "{\"tier\":\"legendary\",\"deepestFloorCleared\":4,\"completed\":true}]}");
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(profile.Tiers.DeepestFloorCleared(DifficultyTier.Forged), Is.EqualTo(3),
                "the first forged record (-5, zeroed) wins, raised to the profile's own record");
            Assert.That(profile.Tiers.HasCompleted(DifficultyTier.Forged), Is.False);
            Assert.That(profile.Tiers.IsOpen(DifficultyTier.Tempered), Is.False);
            Assert.That((profile.Tiers.DeepestFloorCleared(DifficultyTier.Tempered), profile.Tiers.HasCompleted(DifficultyTier.Tempered)),
                Is.EqualTo((0, false)), "Tempered is not open: its record goes");
            Assert.That(profile.Tiers.Selected, Is.EqualTo(DifficultyTier.Forged));
            Assert.That(profile.Tiers.IsMapOpen(3, DifficultyTier.Forged), Is.True);
        }

        // Version 6 (docs/57 phase 4): each hero's forged skills survive the JSON round trip, and a hero with none is not
        // written.
        [Test]
        public void ForgedSkillsRoundTripThroughVersionSix()
        {
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            profile.RecordFloorCleared(3);
            profile.Deposit(1200);
            Assert.That(profile.TryForgeSkill("hero_vanguard", 2), Is.True);
            Assert.That(profile.TryForgeSkill("hero_vanguard", 3), Is.True);
            Assert.That(profile.TryForgeSkill("hero_cinder_arcanist", 2), Is.True);
            Assert.That(profile.Gold, Is.EqualTo(100), "300 + 500 + 300 spent.");
            store.Save(profile);

            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": 6").And.Contain("\"heroSkills\"")
                .And.Contain("\"hero\": \"hero_cinder_arcanist\"").And.Contain("\"skillThree\": true"));
            PlayerProfile loaded = new ProfileStore(_directory).Load();
            Assert.That(loaded.HasForgedSkill("hero_vanguard", 2) && loaded.HasForgedSkill("hero_vanguard", 3), Is.True);
            Assert.That(loaded.HasForgedSkill("hero_cinder_arcanist", 2), Is.True);
            Assert.That(loaded.HasForgedSkill("hero_cinder_arcanist", 3), Is.False);
            Assert.That(loaded.SkillStatusOf("hero_cinder_arcanist", 3), Is.EqualTo(SkillForgeStatus.TooExpensive));
            Assert.That(loaded.Skills.HeroCount, Is.EqualTo(2));
            Assert.That(loaded.Gold, Is.EqualTo(100));
        }

        // A version 5 save (the tiers' build) gains the skills with nothing forged, whatever it has cleared: the Forge offers
        // each skill its floor has opened. Written back as version 6.
        [Test]
        public void AVersionFiveProfileForgesNothing()
        {
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path, "{\"saveVersion\":5,\"revision\":8,\"gold\":900,\"deepestFloorCleared\":4,\"items\":[]," +
                "\"tierProgress\":[{\"tier\":\"forged\",\"deepestFloorCleared\":4,\"completed\":true}],\"selectedTier\":0}");
            var store = new ProfileStore(_directory);
            PlayerProfile veteran = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(veteran.Skills.HeroCount, Is.Zero);
            Assert.That(veteran.HasForgedSkill("hero_vanguard", 1), Is.True, "Skill 1 is the hero's own.");
            Assert.That(veteran.HasForgedSkill("hero_vanguard", 2), Is.False);
            Assert.That(veteran.SkillStatusOf("hero_vanguard", 2), Is.EqualTo(SkillForgeStatus.Affordable));
            Assert.That(veteran.SkillStatusOf("hero_vanguard", 3), Is.EqualTo(SkillForgeStatus.NeedsEarlierSkill));
            Assert.That(veteran.Gold, Is.EqualTo(900), "The gold is untouched.");
            veteran.Deposit(1);
            store.Save(veteran);
            Assert.That(File.ReadAllText(path), Does.Contain("\"saveVersion\": 6").And.Contain("\"heroSkills\": []"));
        }

        // Damaged skills in a readable version 6 save are repaired, never rejected (ProfileSkills.Restore): a record with no
        // hero dropped, a repeated hero's later record dropped, a skill whose floor is not cleared not forged, skill 3 without
        // skill 2 not forged, a hero left with nothing not kept; a hero id the roster does not know is kept.
        [Test]
        public void DamagedSkillsInAReadableSaveAreRepaired()
        {
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path,
                "{\"saveVersion\":6,\"revision\":3,\"gold\":20,\"deepestFloorCleared\":2,\"heroSkills\":[" +
                "{\"hero\":\"\",\"skillTwo\":true,\"skillThree\":true}," +
                "{\"hero\":\"hero_vanguard\",\"skillTwo\":true,\"skillThree\":true}," +
                "{\"hero\":\"hero_vanguard\",\"skillTwo\":false,\"skillThree\":false}," +
                "{\"hero\":\"hero_cinder_arcanist\",\"skillTwo\":false,\"skillThree\":true}," +
                "{\"hero\":\"hero_riftblade\",\"skillTwo\":true,\"skillThree\":false}]}");
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(profile.HasForgedSkill("hero_vanguard", 2), Is.True, "the first record wins");
            Assert.That(profile.HasForgedSkill("hero_vanguard", 3), Is.False, "floor 3 is not cleared");
            Assert.That(profile.HasForgedSkill("hero_cinder_arcanist", 3), Is.False, "skill 3 waits on skill 2");
            Assert.That(profile.HasForgedSkill("hero_riftblade", 2), Is.True, "a hero the roster may not know is kept");
            Assert.That(profile.Skills.HeroCount, Is.EqualTo(2), "the empty id and the Arcanist's nothing are dropped");

            File.WriteAllText(path,
                "{\"saveVersion\":6,\"revision\":9,\"gold\":20,\"deepestFloorCleared\":1,\"heroSkills\":[" +
                "{\"hero\":\"hero_vanguard\",\"skillTwo\":true,\"skillThree\":true}]}");
            PlayerProfile early = new ProfileStore(_directory).Load();
            Assert.That(early.HasForgedSkill("hero_vanguard", 2), Is.False, "floor 2 is not cleared: nothing was earned");
            Assert.That(early.Skills.HeroCount, Is.Zero);
        }

        // docs/57 5.2's toast flags ride in the heroSkills records within version 6 (no bump: an absent flag reads false, which
        // is right for every file the previous build wrote - no toast existed to show - and that build still reads these
        // files, where a version 7 would be a newer build's file it never reads and a new profile in its place).
        [Test]
        public void TheSkillToastsRoundTripWithinVersionSix()
        {
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            profile.RecordFloorCleared(3);
            profile.Deposit(900);
            Assert.That(profile.TryForgeSkill("hero_vanguard", 2), Is.True);
            Assert.That(profile.TryForgeSkill("hero_vanguard", 3), Is.True);
            Assert.That(profile.MarkSkillToastShown("hero_vanguard", 2), Is.True);
            store.Save(profile);

            string json = File.ReadAllText(Path.Combine(_directory, ProfileStore.FileName));
            Assert.That(json, Does.Contain("\"saveVersion\": 6").And.Contain("\"skillTwoToastShown\": true")
                .And.Contain("\"skillThreeToastShown\": false"));
            PlayerProfile loaded = new ProfileStore(_directory).Load();
            Assert.That(loaded.HasShownSkillToast("hero_vanguard", 2), Is.True, "shown once, never again");
            Assert.That(loaded.HasShownSkillToast("hero_vanguard", 3), Is.False, "Shield Rush's still to come");
            Assert.That(loaded.HasForgedSkill("hero_vanguard", 3), Is.True);
        }

        // A version 6 file the previous build wrote (no toast flags): its forged skills stand and each one's toast is still
        // to show; written back as version 6 with the flags.
        [Test]
        public void APreviousVersionSixFileShowsItsForgedSkillsToastsOnce()
        {
            string path = Path.Combine(_directory, ProfileStore.FileName);
            File.WriteAllText(path,
                "{\"saveVersion\":6,\"revision\":4,\"gold\":40,\"deepestFloorCleared\":3,\"heroSkills\":[" +
                "{\"hero\":\"hero_cinder_arcanist\",\"skillTwo\":true,\"skillThree\":true}]}");
            var store = new ProfileStore(_directory);
            PlayerProfile profile = store.Load();
            Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded));
            Assert.That(profile.HasForgedSkill("hero_cinder_arcanist", 3), Is.True);
            Assert.That(profile.HasShownSkillToast("hero_cinder_arcanist", 2), Is.False);
            Assert.That(profile.HasShownSkillToast("hero_cinder_arcanist", 3), Is.False);
            Assert.That(profile.MarkSkillToastShown("hero_cinder_arcanist", 3), Is.True);
            store.Save(profile);
            Assert.That(File.ReadAllText(path), Does.Contain("\"saveVersion\": 6").And.Contain("\"skillThreeToastShown\": true"));
            Assert.That(new ProfileStore(_directory).Load().HasShownSkillToast("hero_cinder_arcanist", 3), Is.True);
        }

        // Damaged gear in a readable version 4 save is repaired, never rejected (ProfileGear.Restore).
        [Test]
        public void DamagedGearInAReadableSaveIsRepaired()
        {
            File.WriteAllText(Path.Combine(_directory, ProfileStore.FileName),
                "{\"saveVersion\":4,\"revision\":2,\"gold\":10,\"items\":[" +
                "{\"uid\":5,\"itemId\":\"item_cinder_ring\",\"slot\":3,\"rarity\":9,\"level\":14,\"pity\":-3,\"lines\":[" +
                "{\"stat\":\"gold_find\",\"tier\":3},{\"stat\":\"damage\",\"tier\":8},{\"stat\":\"damage\",\"tier\":1},{\"stat\":\"crit\",\"tier\":2}]}," +
                "{\"uid\":5,\"itemId\":\"item_ember_locket\",\"slot\":2,\"rarity\":1,\"level\":2,\"pity\":0,\"lines\":[]}," +
                "{\"uid\":7,\"itemId\":\"\",\"slot\":1,\"rarity\":0,\"level\":0,\"pity\":0,\"lines\":[]}]," +
                "\"equippedItemUids\":[5,5,99],\"nextItemUid\":1,\"emberShards\":-4,\"forgeSeals\":2}");
            PlayerProfile profile = new ProfileStore(_directory).Load();
            ItemInstance ring = profile.Gear.FindItem(5);
            Assert.That(ring.ItemId, Is.EqualTo("item_cinder_ring"));
            Assert.That((ring.Rarity, ring.Level, ring.Pity), Is.EqualTo((UpgradeRarity.Gold, 9, 0)));
            Assert.That(ring.Lines.Count, Is.EqualTo(1), "Gold find retired, the repeated damage line and the ring's own crit dropped.");
            Assert.That((ring.Lines[0].Stat, ring.Lines[0].Tier), Is.EqualTo((GearStat.Damage, 5)));
            Assert.That(profile.Gear.Bag.Count, Is.EqualTo(2), "The item with no id is dropped.");
            Assert.That(profile.Gear.Bag[1].Uid, Is.EqualTo(6), "The repeated uid is renumbered.");
            Assert.That(profile.Gear.Equipped(GearSlot.Armour), Is.Null, "Uid 5 is a ring, not an armour.");
            Assert.That(profile.Gear.Equipped(GearSlot.Ring), Is.Null, "Uid 99 is not in the bag.");
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.Zero);
            Assert.That(profile.Gear.Booster(BoosterKind.ForgeSeal), Is.EqualTo(2));
            Assert.That(profile.Gear.NextItemUid, Is.EqualTo(7));
        }
    }
}
