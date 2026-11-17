using System.Linq;
using System.Text;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The profile's gear without Unity (docs/54 phase 2): the bag and its capacity, the snapshot round trip, the repair of a
    // damaged snapshot and the version 3 -> 4 migration through ProfileGearSave. ProfileStoreTests reads the same through
    // JSON. Also in Tools/CombatChecks.
    public sealed class ProfileGearTests
    {
        // A dropped item straight into the profile's bag, as a run's bank puts it there.
        public static ItemInstance Give(PlayerProfile profile, string itemId, GearSlot slot, UpgradeRarity rarity, params EfsunLine[] lines) =>
            profile.AddItem(new ItemDrop(itemId, itemId, slot, rarity, lines, 1, LootSource.Boss));

        // Everything the gear holds, as one line, for comparing two profiles.
        public static string Describe(ProfileGear gear)
        {
            var text = new StringBuilder();
            foreach (ItemInstance piece in gear.Bag.Concat(gear.Weapons))
            {
                text.Append(piece.Uid).Append(' ').Append(piece.ItemId).Append(' ').Append(piece.Slot).Append(' ').Append(piece.Rarity)
                    .Append(" +").Append(piece.Level).Append(" p").Append(piece.Pity).Append(" [");
                foreach (EfsunLine line in piece.Lines)
                    text.Append(GearStatIds.Id(line.Stat)).Append(':').Append(line.Tier).Append(' ');
                text.Append("] eq=").Append(gear.IsEquipped(piece)).Append("; ");
            }
            for (int i = 0; i < GearCurrency.MaterialCount; i++)
                text.Append(gear.Material((MaterialKind)i)).Append(',');
            for (int i = 0; i < GearCurrency.BoosterCount; i++)
                text.Append(gear.Booster((BoosterKind)i)).Append(',');
            text.Append(" next=").Append(gear.NextItemUid).Append(" seed=").Append(gear.AnvilSeed).Append(" draws=").Append(gear.AnvilDraws)
                .Append(" gift=").Append(gear.FirstArmourGiven).Append(" pending=")
                .Append(gear.PendingRerollPiece != null ? ProfileGear.KeyOf(gear.PendingRerollPiece) : "-");
            foreach (EfsunLine line in gear.PendingRerollLines)
                text.Append(' ').Append(GearStatIds.Id(line.Stat)).Append(':').Append(line.Tier);
            return text.ToString();
        }

        [Test]
        public void ItemsTakeTheNextUidAndTheBagHoldsThirty()
        {
            var profile = new PlayerProfile();
            int changes = 0;
            profile.Changed += () => changes++;
            ItemInstance first = Give(profile, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Common, new EfsunLine(GearStat.Damage, 2));
            Assert.That(first.Uid, Is.EqualTo(1));
            Assert.That(first.Level, Is.Zero);
            Assert.That(changes, Is.EqualTo(1), "One change, one save.");
            for (int i = 1; i < ProfileGear.BagCapacity; i++)
                Give(profile, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Common);
            Assert.That(profile.Gear.IsBagFull, Is.True);
            Assert.That(Give(profile, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Common), Is.Null, "A full bag takes nothing.");
            Assert.That(changes, Is.EqualTo(ProfileGear.BagCapacity));
            profile.AddMaterial(MaterialKind.GloamSpore, 3);
            profile.AddBooster(BoosterKind.ForgeSeal, 2);
            Assert.That((profile.Gear.Material(MaterialKind.GloamSpore), profile.Gear.Booster(BoosterKind.ForgeSeal)), Is.EqualTo((3, 2)));
        }

        [Test]
        public void TheSnapshotRoundTrips()
        {
            var profile = new PlayerProfile();
            profile.Deposit(10000);
            var anvil = new Anvil(profile, () => 5);
            ItemInstance mail = Give(profile, "item_foundry_mail", GearSlot.Armour, UpgradeRarity.Gold, new EfsunLine(GearStat.Damage, 3),
                new EfsunLine(GearStat.Luck, 1));
            anvil.TryEquip(mail);
            anvil.TryStrike(mail, false, false, out _);
            anvil.GearForWeapon("weapon_staff");
            profile.AddMaterial(MaterialKind.EmberShard, 4);
            anvil.TryReroll(mail, false);

            ProfileGear restored = ProfileGear.Restore(profile.Gear.Snapshot());
            Assert.That(Describe(restored), Is.EqualTo(Describe(profile.Gear)));

            // Through the save's fields, as ProfileStore writes and reads them.
            var data = new ProfileSaveData();
            ProfileGearSave.Write(profile.Gear.Snapshot(), data);
            Assert.That(Describe(ProfileGear.Restore(ProfileGearSave.ToSnapshot(data))), Is.EqualTo(Describe(profile.Gear)));
        }

        [Test]
        public void ADamagedSnapshotIsRepairedNotRejected()
        {
            var snapshot = new GearSnapshot
            {
                Items = new[]
                {
                    new GearRecord { Uid = 3, ItemId = "item_ember_locket", Slot = 2, Rarity = -1, Level = 4, Pity = 99,
                        Lines = new[] { new GearLineRecord("attack_speed", 2), new GearLineRecord("luck", 0), new GearLineRecord(null, 1) } },
                    null,
                    new GearRecord { Uid = 0, ItemId = "item_bark_mail", Slot = 1 },
                    new GearRecord { Uid = 4, ItemId = "weapon_sword", Slot = 0 }
                },
                Weapons = new[] { new GearRecord { ItemId = "weapon_sword", Level = 3 }, new GearRecord { ItemId = "weapon_sword", Level = 9 },
                    new GearRecord { ItemId = "" } },
                EquippedUids = new[] { 4, 3 },
                Materials = new[] { 5, -2 },
                Boosters = null,
                NextItemUid = -8,
                AnvilDraws = -1,
                PendingRerollKey = "item:77",
                PendingRerollLines = new[] { new GearLineRecord("damage", 1) }
            };
            ProfileGear gear = ProfileGear.Restore(snapshot);
            ItemInstance locket = gear.FindItem(3);
            Assert.That((locket.Rarity, locket.Level, locket.Pity), Is.EqualTo((UpgradeRarity.Common, 4, 3)), "Pity clamps to +5's ceiling.");
            Assert.That(locket.Lines.Select(l => (l.Stat, l.Tier)), Is.EqualTo(new[] { (GearStat.Luck, 1) }),
                "The amulet's own attack speed, an empty stat dropped; tier 0 clamped to 1.");
            Assert.That(gear.Bag.Count, Is.EqualTo(2), "Null and a weapon-slot bag item dropped.");
            Assert.That(gear.Bag[1].Uid, Is.EqualTo(4), "Uid 0 renumbered after the highest kept uid.");
            Assert.That(gear.Weapons.Count, Is.EqualTo(1));
            Assert.That(gear.WeaponGear("weapon_sword").Level, Is.EqualTo(3), "The first copy wins.");
            Assert.That(gear.Equipped(GearSlot.Armour).ItemId, Is.EqualTo("item_bark_mail"), "Uid 4 is now the mail.");
            Assert.That(gear.Equipped(GearSlot.Amulet), Is.SameAs(locket));
            Assert.That(gear.Material(MaterialKind.EmberShard), Is.EqualTo(5));
            Assert.That(gear.Material(MaterialKind.QuicksilverBead), Is.Zero);
            Assert.That(gear.Booster(BoosterKind.ForgeSeal), Is.Zero);
            Assert.That(gear.NextItemUid, Is.EqualTo(5));
            Assert.That(gear.AnvilDraws, Is.Zero);
            Assert.That(gear.PendingRerollPiece, Is.Null, "item:77 does not resolve.");
            Assert.That(ProfileGear.Restore(null).Bag, Is.Empty);
        }

        [Test]
        public void VersionThreeMigratesToAnEmptyBag()
        {
            ProfileSaveData upgraded = ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 3, revision = 4, gold = 39491 });
            Assert.That(upgraded.saveVersion, Is.EqualTo(ProfileSaveData.CurrentVersion), "Through 4 (gear) to the current.");
            Assert.That(upgraded.gold, Is.EqualTo(39491));
            Assert.That(upgraded.items, Is.Empty);
            Assert.That(upgraded.weaponGear, Is.Empty);
            Assert.That(upgraded.equippedItemUids, Is.EqualTo(new[] { 0, 0, 0 }));
            Assert.That(upgraded.nextItemUid, Is.EqualTo(1));
            ProfileGear gear = ProfileGear.Restore(ProfileGearSave.ToSnapshot(upgraded));
            Assert.That(Describe(gear), Is.EqualTo(Describe(new ProfileGear())));
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 1 }).items, Is.Empty, "Every older version passes 4.");
        }
    }
}
