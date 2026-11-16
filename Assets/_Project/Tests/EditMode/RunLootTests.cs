using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // A run's loot (docs/54 phase 3, the lead's decisions of 2026-11-16): what bosses, elites, floor clears and the walk-out
    // drop, at docs/54's rates; the satchel secured at a Descend; a death taking half of the dying floor's materials and a
    // coin per booster, never an item; the bank putting items in the profile the moment they drop and melting what a full
    // bag cannot take; and the result screen's text. Also in Tools/CombatChecks.
    public sealed class RunLootTests
    {
        private static readonly ItemTemplate[] Items =
        {
            new ItemTemplate("item_foundry_mail", "Foundry Mail", GearSlot.Armour, 1),
            new ItemTemplate("item_ember_locket", "Ember Locket", GearSlot.Amulet, 1),
            new ItemTemplate("item_cinder_ring", "Cinder Ring", GearSlot.Ring, 1),
            new ItemTemplate("item_bark_mail", "Bark Mail", GearSlot.Armour, 3),
            new ItemTemplate("item_spore_locket", "Spore Locket", GearSlot.Amulet, 3),
            new ItemTemplate("item_gloam_signet", "Gloam Signet", GearSlot.Ring, 3)
        };

        [Test]
        public void AFloorClearDropsTwoOfItsFloorsMaterial()
        {
            LootDrop one = LootRule.Roll(LootSource.FloorClear, 1, 7, 0, Items);
            Assert.That((one.HasMaterial, one.Material, one.MaterialAmount), Is.EqualTo((true, MaterialKind.EmberShard, 2)));
            Assert.That(LootRule.Roll(LootSource.FloorClear, 2, 7, 1, Items).Material, Is.EqualTo(MaterialKind.QuicksilverBead));
            Assert.That(LootRule.Roll(LootSource.FloorClear, 3, 7, 2, Items).Material, Is.EqualTo(MaterialKind.GloamSpore));
            Assert.That(LootRule.Roll(LootSource.FloorClear, 4, 7, 3, Items).HasMaterial, Is.False, "The finale drops no material.");
            LootDrop elite = LootRule.Roll(LootSource.Elite, 2, 7, 4, Items);
            Assert.That((elite.Material, elite.MaterialAmount), Is.EqualTo((MaterialKind.QuicksilverBead, 1)));
        }

        // docs/54's rates over 20,000 moments each: a Seal 8 % an elite, an item 25 % a boss and 60 % a walk-out, a scroll
        // 25 % a clear, a Hammer 4 % a boss.
        [Test]
        public void RatesAreDocs54s()
        {
            const int n = 20000;
            double Share(LootSource source, System.Func<LootDrop, bool> hit) =>
                Enumerable.Range(0, n).Count(i => hit(LootRule.Roll(source, 2, 99, i, Items))) / (double)n;
            Assert.That(Share(LootSource.Elite, d => d.Booster(BoosterKind.ForgeSeal) > 0), Is.EqualTo(.08).Within(.01));
            Assert.That(Share(LootSource.Boss, d => d.Item != null), Is.EqualTo(.25).Within(.012));
            Assert.That(Share(LootSource.Boss, d => d.Booster(BoosterKind.MastersHammer) > 0), Is.EqualTo(.04).Within(.006));
            Assert.That(Share(LootSource.WalkOut, d => d.Item != null), Is.EqualTo(.60).Within(.012));
            Assert.That(Share(LootSource.FloorClear, d => d.Booster(BoosterKind.EfsunScroll) > 0), Is.EqualTo(.25).Within(.012));
            double gold = Enumerable.Range(0, n).Select(i => LootRule.Roll(LootSource.WalkOut, 4, 5, i, Items).Item)
                .Where(item => item != null).Average(item => item.Rarity == UpgradeRarity.Gold ? 1.0 : 0.0);
            Assert.That(gold, Is.EqualTo(.03).Within(.006), "Rarity by RarityTable at no luck.");
        }

        [Test]
        public void ItemsComeFromTheirFloorAndTheFinaleDropsTheThirds()
        {
            for (int i = 0; i < 300; i++)
            {
                ItemDrop item = LootRule.Roll(LootSource.WalkOut, 4, 11, i, Items).Item;
                if (item == null)
                    continue;
                Assert.That(new[] { "item_bark_mail", "item_spore_locket", "item_gloam_signet" }, Does.Contain(item.ItemId));
                Assert.That(item.Lines.Count, Is.EqualTo(EfsunRule.LinesFor(item.Rarity)));
                Assert.That(item.Lines.Any(l => l.Stat == GearStats.MainStatOf(item.Slot)), Is.False);
            }
            ItemDrop gift = LootRule.Roll(LootSource.Boss, 1, 11, 0, Items, true).Item;
            Assert.That((gift.ItemId, gift.Source), Is.EqualTo(("item_foundry_mail", LootSource.FirstBoss)), "The first boss's armour, certain.");
            Assert.That(LootRule.Roll(LootSource.Boss, 1, 11, 0, new ItemTemplate[0], true).Item, Is.Null, "No items authored, none dropped.");
        }

        [Test]
        public void TheSameSeedDropsTheSameLoot()
        {
            string Describe(int seed)
            {
                var run = new RunState(10, 0.5f, 0, seed);
                var loot = new RunLoot(run, Items, true);
                loot.BossFelled(1);
                loot.EliteFelled(1);
                loot.FloorCleared(1);
                loot.WalkedOut(4);
                return string.Join(",", Enumerable.Range(0, 3).Select(k => loot.UnsecuredMaterial((MaterialKind)k))) + "|" +
                       string.Join(",", Enumerable.Range(0, 4).Select(k => loot.UnsecuredBooster((BoosterKind)k))) + "|" +
                       string.Join(",", loot.Items.Select(item => item.ItemId + ":" + item.Rarity));
            }
            Assert.That(Describe(3), Is.EqualTo(Describe(3)));
            Assert.That(Enumerable.Range(0, 20).Select(Describe).Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void ADeathTakesHalfTheDyingFloorsMaterialsAndNeverAnItem()
        {
            var run = new RunState(10, 0.5f, 0, 1);
            var profile = new PlayerProfile();
            var loot = new RunLoot(run, Items, true);
            var bank = new RunBank(run, profile, loot);
            loot.BossFelled(1);
            Assert.That(profile.Gear.Bag.Count, Is.EqualTo(1), "The first boss's armour went to the profile as it dropped.");
            Assert.That(profile.Gear.FirstArmourGiven, Is.True);
            loot.FloorCleared(1);
            loot.Secure();
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.EqualTo(2), "Secured at the Descend, banked at once.");
            loot.EliteFelled(2);
            loot.EliteFelled(2);
            loot.EliteFelled(2);
            int boosters = Enumerable.Range(0, 4).Sum(k => loot.UnsecuredBooster((BoosterKind)k));
            run.End(RunOutcome.Defeat);

            LootSettlement settlement = loot.Settlement;
            Assert.That(settlement.MaterialsLost[(int)MaterialKind.QuicksilverBead], Is.EqualTo(1), "Three beads, floor of half lost.");
            Assert.That(profile.Gear.Material(MaterialKind.QuicksilverBead), Is.EqualTo(2));
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.EqualTo(2), "Secured loot is never at risk.");
            Assert.That(settlement.BoostersKept.Sum() + settlement.BoostersLost.Sum(), Is.EqualTo(boosters));
            Assert.That(profile.Gear.Bag.Count, Is.EqualTo(loot.Items.Count), "Every item is kept, the first boss's armour among them.");
            Assert.That(bank.ItemDeposits.Stored.Count, Is.EqualTo(loot.Items.Count));
            loot.EliteFelled(2);
            Assert.That(loot.UnsecuredMaterial(MaterialKind.QuicksilverBead), Is.EqualTo(3), "An ended run drops nothing more.");
        }

        // Booster coins over many seeds: about half of the dying floor's boosters are kept.
        [Test]
        public void ADeathKeepsAboutHalfTheBoosters()
        {
            int kept = 0;
            for (int seed = 0; seed < 2000; seed++)
                kept += LootRule.KeepsOnDeath(seed, 0, 5000) ? 1 : 0;
            Assert.That(kept / 2000.0, Is.EqualTo(.5).Within(.04));
            var run = new RunState(10, 0.5f, 0, 1);
            var loot = new RunLoot(run, Items, false);
            for (int i = 0; i < 40; i++)
                loot.EliteFelled(1);
            run.End(RunOutcome.Victory);
            Assert.That(loot.Settle(RunOutcome.Victory).BoostersLost.Sum() + loot.Settlement.MaterialsLost.Sum(), Is.Zero,
                "A victory keeps everything.");
        }

        [Test]
        public void AFullBagMeltsTheDropAtOnce()
        {
            var run = new RunState(10, 0.5f, 0, 2);
            var profile = new PlayerProfile();
            for (int i = 0; i < ProfileGear.BagCapacity; i++)
                ProfileGearTests.Give(profile, "item_cinder_ring", GearSlot.Ring, UpgradeRarity.Common);
            var loot = new RunLoot(run, Items, true);
            var bank = new RunBank(run, profile, loot);
            loot.BossFelled(1);
            Assert.That(bank.ItemDeposits.Melted.Count, Is.EqualTo(1));
            Assert.That(profile.Gold, Is.EqualTo(bank.ItemDeposits.MeltGold));
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.EqualTo(bank.ItemDeposits.MeltEmber));
            Assert.That(profile.Gear.FirstArmourGiven, Is.True, "The gift is given, melted or not.");
        }

        [Test]
        public void TheResultLineNamesWhatWasBankedAndLostInTheInvariantCulture()
        {
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                var run = new RunState(10, 0.5f, 0, 1);
                var profile = new PlayerProfile();
                var loot = new RunLoot(run, Items, true);
                var bank = new RunBank(run, profile, loot);
                loot.BossFelled(1);
                loot.FloorCleared(1);
                loot.Secure();
                loot.EliteFelled(2);
                loot.EliteFelled(2);
                loot.EliteFelled(2);
                run.End(RunOutcome.Defeat);
                LootText.Describe(loot, bank.ItemDeposits, out string banked, out string lost);
                Assert.That(banked, Does.StartWith("Ember Shard x2, Quicksilver Bead x2"));
                Assert.That(banked, Does.Contain("Foundry Mail ("));
                Assert.That(lost, Does.StartWith("Quicksilver Bead x1"));
                LootText.Describe(null, null, out string none, out string noneLost);
                Assert.That(none + noneLost, Is.Empty);
                // The result screen's one short line (docs/54 phase 4).
                LootText.Compact(loot, bank.ItemDeposits, out string shortBanked, out string shortLost);
                Assert.That(shortBanked, Does.StartWith("Ember 2, Bead 2"));
                Assert.That(shortBanked, Does.EndWith(loot.Items.Count == 1 ? "1 item" : loot.Items.Count + " items"));
                Assert.That(shortLost, Does.StartWith("Bead 1"));
                Assert.That(shortBanked.Length, Is.LessThan(60), "One line on the result screen.");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }
    }
}
