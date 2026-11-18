using System;
using System.Globalization;
using System.IO;
using System.Threading;
using Cryptforge.Core;
using Cryptforge.Localisation;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The save is data (docs/60 section 6): whatever the phone's culture, the profile is written byte for byte as the
    // invariant culture writes it. A Turkish phone writes 1.5 as "1,5" and upper-cases "i" to "İ"; a culture with a
    // Unicode minus sign would write -1 otherwise. And the game pins the invariant culture for every thread at boot.
    public sealed class CultureSafetyTests
    {
        // A version 4 profile with every kind of field the save writes: gold in the thousands, ids with an "i" in them,
        // items with lines, a weapon's gear, materials, boosters, a negative seed and a reroll waiting.
        private const string Profile = @"{
    ""saveVersion"": 4, ""revision"": 7, ""gold"": 12345,
    ""ownedRelicIds"": [""relic_second_wind""], ""equippedRelicId"": ""relic_second_wind"", ""deepestFloorCleared"": 2,
    ""ownedWeaponIds"": [""weapon_daggers""], ""equippedWeaponId"": ""weapon_daggers"",
    ""unlockedHeroIds"": [""hero_cinder_arcanist""], ""selectedHeroId"": ""hero_cinder_arcanist"",
    ""items"": [
        {""uid"": 1, ""itemId"": ""item_cinder_ring"", ""slot"": 3, ""rarity"": 3, ""level"": 9, ""pity"": 2,
         ""lines"": [{""stat"": ""crit"", ""tier"": 5}, {""stat"": ""damage"", ""tier"": 3}]},
        {""uid"": 2, ""itemId"": ""item_foundry_mail"", ""slot"": 1, ""rarity"": 1, ""level"": 4, ""pity"": 0, ""lines"": []}
    ],
    ""weaponGear"": [{""weaponId"": ""weapon_daggers"", ""level"": 7, ""pity"": 1, ""lines"": [{""stat"": ""attack_speed"", ""tier"": 4}]}],
    ""equippedItemUids"": [2, 0, 1], ""nextItemUid"": 3,
    ""emberShards"": 1400, ""quicksilverBeads"": 3, ""gloamSpores"": 1,
    ""forgeSeals"": 2, ""mastersHammers"": 1, ""efsunScrolls"": 1, ""bindingRunes"": 0,
    ""anvilSeed"": -123456, ""anvilDraws"": 9, ""firstArmourGiven"": true, ""highestTierCompleted"": 0, ""selectedTier"": 0,
    ""pendingRerollKey"": ""item:1"", ""pendingRerollLines"": [{""stat"": ""luck"", ""tier"": 2}, {""stat"": ""damage"", ""tier"": 1}]
}";

        private string _root;

        [SetUp]
        public void MakeFolder()
        {
            _root = Path.Combine(Path.GetTempPath(), "cryptforge-culture-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
        }

        [TearDown]
        public void RemoveFolder()
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, true);
        }

        [Test]
        public void TheSaveIsByteIdenticalWhateverTheThreadsCulture()
        {
            var odd = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            odd.NumberFormat.NegativeSign = "−";
            odd.NumberFormat.NumberDecimalSeparator = ",";
            odd.NumberFormat.NumberGroupSeparator = ".";

            byte[] invariant = LoadAndSave(CultureInfo.InvariantCulture, "invariant");
            byte[] turkish = LoadAndSave(new CultureInfo("tr-TR"), "turkish");
            byte[] other = LoadAndSave(odd, "odd");

            Assert.That(turkish, Is.EqualTo(invariant), "tr-TR writes the invariant save.");
            Assert.That(other, Is.EqualTo(invariant), "A culture with a Unicode minus writes the invariant save.");
            string text = System.Text.Encoding.UTF8.GetString(invariant);
            Assert.That(text, Does.Contain("12345").And.Contain("-123456").And.Contain("item_cinder_ring"));
            Assert.That(text, Does.Not.Contain("−").And.Not.Contain("İ"), "No culture's minus or dotted capital I.");
        }

        [Test]
        public void ATurkishSaveLoadsBackUnderATurkishCulture()
        {
            byte[] first = LoadAndSave(new CultureInfo("tr-TR"), "first");
            string again = Path.Combine(_root, "again");
            Directory.CreateDirectory(again);
            File.WriteAllBytes(Path.Combine(again, ProfileStore.FileName), first);
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                var store = new ProfileStore(again);
                PlayerProfile profile = store.Load();
                Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded), string.Join("; ", store.LastLoadProblems));
                Assert.That(profile.Gold, Is.EqualTo(12345));
                Assert.That(profile.SelectedHeroId, Is.EqualTo("hero_cinder_arcanist"));
                Assert.That(profile.Gear.FindItem(1), Is.Not.Null, "The ring, uid 1, read back.");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        // LocalisationRuntime's boot: the invariant culture on this thread and on every thread made after it.
        [Test]
        public void TheBootPinsTheInvariantCultureForEveryThread()
        {
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            CultureInfo beforeUi = Thread.CurrentThread.CurrentUICulture;
            CultureInfo defaultBefore = CultureInfo.DefaultThreadCurrentCulture;
            CultureInfo defaultUiBefore = CultureInfo.DefaultThreadCurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                LocalisationRuntime.PinInvariantCulture();
                Assert.That(CultureInfo.CurrentCulture, Is.EqualTo(CultureInfo.InvariantCulture));
                Assert.That(1.5f.ToString(), Is.EqualTo("1.5"));
                Assert.That("i".ToUpper(), Is.EqualTo("I"));
                CultureInfo seen = null;
                var thread = new Thread(() => seen = CultureInfo.CurrentCulture);
                thread.Start();
                thread.Join();
                Assert.That(seen, Is.EqualTo(CultureInfo.InvariantCulture), "A thread made after the boot.");
            }
            finally
            {
                CultureInfo.DefaultThreadCurrentCulture = defaultBefore;
                CultureInfo.DefaultThreadCurrentUICulture = defaultUiBefore;
                Thread.CurrentThread.CurrentCulture = before;
                Thread.CurrentThread.CurrentUICulture = beforeUi;
            }
        }

        [Test]
        public void ThePhonesLanguagePicksTheDefault()
        {
            Assert.That(LocalisationRuntime.SystemDefault(UnityEngine.SystemLanguage.Turkish), Is.EqualTo(Language.Turkish));
            Assert.That(LocalisationRuntime.SystemDefault(UnityEngine.SystemLanguage.English), Is.EqualTo(Language.English));
            Assert.That(LocalisationRuntime.SystemDefault(UnityEngine.SystemLanguage.German), Is.EqualTo(Language.English));
            Assert.That(LanguageCodes.TryParse("tr", out Language turkish) && turkish == Language.Turkish, Is.True);
            Assert.That(LanguageCodes.TryParse("TR", out _), Is.False, "Codes are compared ordinally.");
        }

        // The fixture loaded and saved again with the thread in culture: the saved file's bytes.
        private byte[] LoadAndSave(CultureInfo culture, string name)
        {
            string folder = Path.Combine(_root, name);
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, ProfileStore.FileName), Profile);
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            CultureInfo beforeUi = Thread.CurrentThread.CurrentUICulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = culture;
                Thread.CurrentThread.CurrentUICulture = culture;
                var store = new ProfileStore(folder);
                PlayerProfile profile = store.Load();
                Assert.That(store.LastLoadStatus, Is.EqualTo(ProfileLoadStatus.Loaded), string.Join("; ", store.LastLoadProblems));
                store.Save(profile);
                return File.ReadAllBytes(Path.Combine(folder, ProfileStore.FileName));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
                Thread.CurrentThread.CurrentUICulture = beforeUi;
            }
        }
    }
}
