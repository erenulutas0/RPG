using System;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Relic Forge's weapon rack is gone (docs/63, 2026-11-23): every hero starts with its own weapon - Ilya its Staff
    // since 2026-10-29, the Vanguard its Sword since 2026-11-22 - so the rack had nothing left to sell. What it recorded
    // stays in the profile and its save, read by no run; and the hero it unlocked stays reachable.
    public sealed class WeaponForgeTests
    {
        [Test]
        public void TheForgeSellsNoWeapon()
        {
            Assert.That(typeof(UpgradeService).Assembly.GetType("Cryptforge.Progression.WeaponShop"), Is.Null,
                "No weapon rack: the forge's shop is the relics' alone.");
            foreach (string name in new[] { "TryForgeWeapon", "EquipWeapon", "UnequipWeapon" })
                Assert.That(typeof(PlayerProfile).GetMethod(name), Is.Null, name + ": nothing buys or equips a weapon now.");

            var profile = new PlayerProfile(500, null, null, 1);
            Assert.That(profile.TryForgeRelic("relic_second_wind", 80), Is.True, "The relics are still forged.");
            Assert.That(profile.OwnedWeaponIds, Is.Empty);
            Assert.That(profile.EquippedWeaponId, Is.Null);
        }

        // The rack's record is restored as it always was (duplicates dropped, an unowned weapon unequipped) and never lost:
        // the save keeps the ids and the version (no migration needed, docs/63 section 5).
        [Test]
        public void ARestoredProfileKeepsWhatTheRetiredRackRecorded()
        {
            var restored = new PlayerProfile(0, null, null, 0, new[] { "weapon_staff", "weapon_staff", null }, "weapon_daggers");
            Assert.That(restored.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(restored.EquippedWeaponId, Is.Null, "An unowned weapon cannot stay equipped.");

            var kept = new PlayerProfile(0, null, null, 0, new[] { "weapon_daggers" }, "weapon_daggers");
            Assert.That(kept.OwnsWeapon("weapon_daggers"), Is.True);
            Assert.That(kept.EquippedWeaponId, Is.EqualTo("weapon_daggers"), "Kept, though no run reads it.");
            Assert.That(kept.OwnsWeapon("relic_second_wind"), Is.False);
        }

        [Test]
        public void AWeaponsTextAndIdAreStillChecked()
        {
            // WeaponOption stays: a weapon's card text is its forge description (WeaponDefinition.CreateForgeOption).
            Assert.Throws<ArgumentException>(() => new WeaponOption("", "", "", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponOption("weapon_x", "", "", -1));
            Assert.That(new WeaponOption("weapon_sword", "Sword", "10 damage", 0).Description, Is.EqualTo("10 damage"));
        }

        // Ilya's unlock, "Clear The Foundry with the Staff" (docs/45): the Staff carried through it - with today's catalogue
        // the Vanguard can take it as a card - or, where the run's cards could never hand it over (the weapon roster keeps a
        // hero's own weapon to that hero, docs/63 section 6), the clear alone.
        [Test]
        public void TheArcanistUnlocksWithTheStaffOrWhereNoCardCouldHandItOver()
        {
            Assert.That(HeroUnlock.UnlocksOnFoundryClear(true, true), Is.True, "The Staff carried, from the start or a card.");
            Assert.That(HeroUnlock.UnlocksOnFoundryClear(false, true), Is.False, "The Staff could have come and did not.");
            Assert.That(HeroUnlock.UnlocksOnFoundryClear(false, false), Is.True, "Under the roster no card hands it over.");
            Assert.That(HeroUnlock.UnlocksOnFoundryClear(true, false), Is.True);
        }

        [Test]
        public void VersionOneSavesMigrateToTheCurrentSchemaWithoutWeapons()
        {
            var versionOne = new ProfileSaveData
            {
                saveVersion = 1,
                revision = 5,
                gold = 90,
                ownedRelicIds = new[] { "relic_second_wind" },
                equippedRelicId = "relic_second_wind",
                deepestFloorCleared = 2
            };

            ProfileSaveData upgraded = ProfileMigration.Upgrade(versionOne);

            Assert.That(ProfileSaveData.CurrentVersion, Is.EqualTo(6),
                "Version 6 added the heroes' forged skills (docs/57 phase 4) on top of version 5's difficulty tiers, version 4's gear, version 3's heroes and version 2's weapons; the rack's going needed none (docs/63).");
            Assert.That(upgraded.heroSkills, Is.Empty, "Nothing is forged before version 6.");
            Assert.That(upgraded.saveVersion, Is.EqualTo(ProfileSaveData.CurrentVersion));
            Assert.That(upgraded.ownedWeaponIds, Is.Empty);
            Assert.That(upgraded.equippedWeaponId, Is.Empty);
            Assert.That(upgraded.revision, Is.EqualTo(5));
            Assert.That(upgraded.gold, Is.EqualTo(90));
            Assert.That(upgraded.ownedRelicIds, Is.EqualTo(new[] { "relic_second_wind" }));
            Assert.That(upgraded.equippedRelicId, Is.EqualTo("relic_second_wind"));
            Assert.That(upgraded.deepestFloorCleared, Is.EqualTo(2));

            var current = new ProfileSaveData { saveVersion = 2, ownedWeaponIds = new[] { "weapon_staff" }, equippedWeaponId = "weapon_staff" };
            Assert.That(ProfileMigration.Upgrade(current).ownedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }), "Version 2's weapons pass through.");
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = ProfileSaveData.CurrentVersion + 1 }), Is.Null,
                "A newer build's save is never guessed at.");
            Assert.That(ProfileMigration.Upgrade(null), Is.Null);
        }
    }
}
