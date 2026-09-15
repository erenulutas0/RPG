using System;
using System.Collections.Generic;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class WeaponForgeTests
    {
        // Mirror the forge prices in Data/Weapons/Weapon_Sword.asset, Weapon_Staff.asset and Weapon_Daggers.asset.
        internal static WeaponOption Sword() => new WeaponOption("weapon_sword", "Sword", "", 0);
        internal static WeaponOption Staff() => new WeaponOption("weapon_staff", "Staff", "", 120);
        internal static WeaponOption Daggers() => new WeaponOption("weapon_daggers", "Daggers", "", 180);

        [Test]
        public void ProfileForgesAndEquipsWeaponsWithoutTouchingRelics()
        {
            var profile = new PlayerProfile(200, new[] { "relic_second_wind" }, "relic_second_wind", 1);
            int changes = 0;
            profile.Changed += () => changes++;

            Assert.That(profile.TryForgeWeapon("weapon_daggers", 250), Is.False);
            Assert.That(profile.TryForgeWeapon("weapon_staff", 120), Is.True);
            Assert.That(profile.TryForgeWeapon("weapon_staff", 120), Is.False, "A weapon is forged once.");
            Assert.That(profile.Gold, Is.EqualTo(80));
            Assert.That(profile.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(profile.EquippedWeaponId, Is.EqualTo("weapon_staff"), "Forging equips the new weapon.");
            Assert.That(profile.EquippedRelicId, Is.EqualTo("relic_second_wind"), "Weapons and relics are separate slots.");
            Assert.That(profile.OwnsWeapon("relic_second_wind"), Is.False);

            Assert.That(profile.EquipWeapon("weapon_daggers"), Is.False, "Only owned weapons can be equipped.");
            Assert.That(profile.UnequipWeapon(), Is.True);
            Assert.That(profile.EquippedWeaponId, Is.Null);
            Assert.That(profile.UnequipWeapon(), Is.False);
            Assert.That(profile.EquipWeapon("weapon_staff"), Is.True);
            Assert.That(profile.EquipWeapon("weapon_staff"), Is.False);
            Assert.That(changes, Is.EqualTo(3), "One change per forge, unequip and equip, so each is saved once.");
            Assert.Throws<ArgumentException>(() => profile.TryForgeWeapon("", 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => profile.TryForgeWeapon("weapon_x", -1));

            var restored = new PlayerProfile(0, null, null, 0, new[] { "weapon_staff", "weapon_staff", null }, "weapon_daggers");
            Assert.That(restored.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(restored.EquippedWeaponId, Is.Null, "An unowned weapon cannot stay equipped.");
        }

        [Test]
        public void ShopKeepsTheStartingWeaponOwnedAndForgesTheRest()
        {
            var profile = new PlayerProfile(150, null, null, 0);
            WeaponOption sword = Sword();
            WeaponOption staff = Staff();
            WeaponOption daggers = Daggers();
            var shop = new WeaponShop(profile, new[] { sword, staff, daggers }, sword);

            Assert.That(shop.Equipped, Is.SameAs(sword));
            Assert.That(shop.StatusOf(sword), Is.EqualTo(UnlockStatus.Equipped));
            Assert.That(shop.StatusOf(staff), Is.EqualTo(UnlockStatus.Affordable));
            Assert.That(shop.StatusOf(daggers), Is.EqualTo(UnlockStatus.TooExpensive));
            Assert.That(shop.GoldNeededFor(daggers), Is.EqualTo(30));
            Assert.That(shop.NextUnlock, Is.SameAs(staff), "The starting weapon is never an unlock.");
            Assert.That(shop.TrySelect(sword), Is.False, "Tapping the carried weapon changes nothing.");
            Assert.That(shop.TrySelect(daggers), Is.False);

            Assert.That(shop.TrySelect(staff), Is.True);
            Assert.That(profile.Gold, Is.EqualTo(30));
            Assert.That(shop.Equipped, Is.SameAs(staff));
            Assert.That(shop.StatusOf(sword), Is.EqualTo(UnlockStatus.Owned));
            Assert.That(shop.GoldNeededFor(sword), Is.Zero);
            Assert.That(shop.NextUnlock, Is.SameAs(daggers));

            Assert.That(shop.TrySelect(sword), Is.True);
            Assert.That(profile.EquippedWeaponId, Is.Null, "Returning to the starting weapon stores no id.");
            Assert.That(profile.OwnedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }));
            Assert.That(shop.TrySelect(staff), Is.True, "An owned weapon is equipped again for free.");
            Assert.That(profile.Gold, Is.EqualTo(30));

            profile.Deposit(150);
            Assert.That(shop.TrySelect(daggers), Is.True);
            Assert.That(profile.Gold, Is.Zero);
            Assert.That(shop.NextUnlock, Is.Null);
        }

        [Test]
        public void ShopReportsForgedOnlyForPurchasesTheProfileAccepted()
        {
            var profile = new PlayerProfile(150, null, null, 0);
            WeaponOption sword = Sword();
            WeaponOption staff = Staff();
            WeaponOption daggers = Daggers();
            var shop = new WeaponShop(profile, new[] { sword, staff, daggers }, sword);
            var forged = new List<WeaponOption>();
            var goldSeenByForged = new List<int>();
            bool ownedAndEquippedWhenForged = true;
            shop.Forged += weapon =>
            {
                forged.Add(weapon);
                goldSeenByForged.Add(profile.Gold);
                ownedAndEquippedWhenForged &= profile.OwnsWeapon(weapon.Id) && profile.EquippedWeaponId == weapon.Id;
            };

            Assert.That(shop.TrySelect(sword), Is.False, "Tapping the carried starting weapon is not a purchase.");
            Assert.That(shop.TrySelect(daggers), Is.False, "An unaffordable weapon is not a purchase.");
            Assert.That(forged, Is.Empty);

            Assert.That(shop.TrySelect(staff), Is.True);
            Assert.That(forged, Is.EqualTo(new[] { staff }));
            Assert.That(goldSeenByForged, Is.EqualTo(new[] { 30 }), "Forged follows the accepted spend.");

            Assert.That(shop.TrySelect(sword), Is.True, "Returning to the starting weapon is not a purchase.");
            Assert.That(shop.TrySelect(staff), Is.True, "Equipping an owned weapon is not a purchase.");
            Assert.That(shop.TrySelect(staff), Is.False);
            Assert.That(forged, Has.Count.EqualTo(1));

            profile.Deposit(150);
            Assert.That(shop.TrySelect(daggers), Is.True);
            Assert.That(shop.TrySelect(daggers), Is.False);

            Assert.That(forged, Is.EqualTo(new[] { staff, daggers }));
            Assert.That(goldSeenByForged, Is.EqualTo(new[] { 30, 0 }));
            Assert.That(ownedAndEquippedWhenForged, Is.True);
        }

        [Test]
        public void ShopRejectsBadCatalogsAndIgnoresUnknownSavedWeapons()
        {
            WeaponOption sword = Sword();
            Assert.Throws<ArgumentException>(() => new WeaponShop(new PlayerProfile(), new[] { sword, Sword() }, sword));
            Assert.Throws<ArgumentException>(() => new WeaponShop(new PlayerProfile(), new[] { sword, null }, sword));
            Assert.Throws<ArgumentException>(() => new WeaponShop(new PlayerProfile(), new[] { sword }, Staff()), "The starting weapon must be listed.");
            Assert.Throws<ArgumentException>(() => new WeaponOption("", "", "", 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponOption("weapon_x", "", "", -1));

            var saved = new PlayerProfile(0, null, null, 0, new[] { "weapon_removed" }, "weapon_removed");
            var shop = new WeaponShop(saved, new[] { sword, Staff() }, sword);
            Assert.That(shop.Equipped, Is.SameAs(sword), "A saved weapon this build no longer sells falls back to the starting weapon.");
            Assert.Throws<ArgumentException>(() => shop.StatusOf(Staff()), "Only the shop's own instances are accepted.");
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

            Assert.That(ProfileSaveData.CurrentVersion, Is.EqualTo(2));
            Assert.That(upgraded.saveVersion, Is.EqualTo(ProfileSaveData.CurrentVersion));
            Assert.That(upgraded.ownedWeaponIds, Is.Empty);
            Assert.That(upgraded.equippedWeaponId, Is.Empty);
            Assert.That(upgraded.revision, Is.EqualTo(5));
            Assert.That(upgraded.gold, Is.EqualTo(90));
            Assert.That(upgraded.ownedRelicIds, Is.EqualTo(new[] { "relic_second_wind" }));
            Assert.That(upgraded.equippedRelicId, Is.EqualTo("relic_second_wind"));
            Assert.That(upgraded.deepestFloorCleared, Is.EqualTo(2));

            var current = new ProfileSaveData { saveVersion = 2, ownedWeaponIds = new[] { "weapon_staff" }, equippedWeaponId = "weapon_staff" };
            Assert.That(ProfileMigration.Upgrade(current).ownedWeaponIds, Is.EqualTo(new[] { "weapon_staff" }), "The current version passes through.");
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 3 }), Is.Null, "A newer build's save is never guessed at.");
            Assert.That(ProfileMigration.Upgrade(null), Is.Null);
        }
    }
}
