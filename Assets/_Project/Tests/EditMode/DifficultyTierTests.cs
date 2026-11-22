using System;
using System.Collections.Generic;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The difficulty tiers (docs/54 phase 6, section 21): Forged changes nothing anywhere - enemies, gold, loot, the
    // simulation - so a Forged run is today's to the bit; the tiers' table, words and unlock chain; the enemies' layer through
    // the one body the scene and the simulation share; the rewards on gold, materials and boosters, the item luck; the
    // profile's tier records, the choice, the map starts per tier, the repair of a damaged save and the migration from
    // version 4. ProfileStoreTests reads the same through JSON. Also in Tools/CombatChecks.
    public sealed class DifficultyTierTests
    {
        private static readonly ItemTemplate[] Items =
        {
            new ItemTemplate("item_foundry_mail", "Foundry Mail", GearSlot.Armour, 1),
            new ItemTemplate("item_ember_locket", "Ember Locket", GearSlot.Amulet, 1),
            new ItemTemplate("item_cinder_ring", "Cinder Ring", GearSlot.Ring, 1)
        };

        private static readonly DifficultyTier[] All =
            { DifficultyTier.Forged, DifficultyTier.Tempered, DifficultyTier.Inferno, DifficultyTier.Abyssal };

        [Test]
        public void ForgedChangesNothing()
        {
            Assert.That(DifficultyTiers.EnemyHealth(DifficultyTier.Forged), Is.EqualTo(1f));
            Assert.That(DifficultyTiers.EnemyDamage(DifficultyTier.Forged), Is.EqualTo(1f));
            Assert.That(DifficultyTiers.RewardPercent(DifficultyTier.Forged), Is.EqualTo(100));
            Assert.That(DifficultyTiers.ItemLuck(DifficultyTier.Forged), Is.Zero);
            foreach (float floor in new[] { 1f, 1.4f, 1.96f, 2.254f, .92f, 1.288f, 1.8032f, 7.123457f })
            {
                Assert.That(BitConverter.SingleToInt32Bits(FloorScaling.TierHealth(floor, DifficultyTier.Forged)),
                    Is.EqualTo(BitConverter.SingleToInt32Bits(floor)), "the floor's own multiplier, to the bit");
                Assert.That(BitConverter.SingleToInt32Bits(FloorScaling.TierDamage(floor, DifficultyTier.Forged)),
                    Is.EqualTo(BitConverter.SingleToInt32Bits(floor)));
                Assert.That(BitConverter.SingleToInt32Bits(FloorScaling.Tiered(floor, 1f)), Is.EqualTo(BitConverter.SingleToInt32Bits(floor)));
            }
            foreach (int gold in new[] { 0, 3, 5, 10, 20, 50 })
            foreach (float modifier in new[] { 0f, .5f })
                Assert.That(FloorScaling.Gold(gold, modifier, DifficultyTier.Forged), Is.EqualTo(FloorScaling.Gold(gold, modifier)));
            foreach (int parts in new[] { 0, 200, 2500, 6000, 10000 })
                Assert.That(DifficultyTiers.Chance(parts, DifficultyTier.Forged), Is.EqualTo(parts));
            RunRandom stream = RunRandom.Stream(3, RunRandom.Loot, 0), twin = RunRandom.Stream(3, RunRandom.Loot, 0);
            Assert.That(DifficultyTiers.Amount(2, DifficultyTier.Forged, stream), Is.EqualTo(2));
            Assert.That(stream.NextBelow(1000), Is.EqualTo(twin.NextBelow(1000)), "Forged draws nothing for an amount");

            // A Forged run's loot is the loot the rule always rolled, word for word.
            foreach (LootSource source in new[] { LootSource.FloorClear, LootSource.Elite, LootSource.Boss, LootSource.WalkOut })
            for (int seed = 1; seed <= 60; seed++)
            for (int index = 0; index < 4; index++)
                Assert.That(Describe(LootRule.Roll(source, 1 + index % 3, seed, index, Items, false, DifficultyTier.Forged)),
                    Is.EqualTo(Describe(LootRule.Roll(source, 1 + index % 3, seed, index, Items))), $"{source} {seed} {index}");
        }

        [Test]
        public void TheTiersTable()
        {
            Assert.That(DifficultyTiers.Count, Is.EqualTo(4));
            Assert.That(All.Select(DifficultyTiers.Name), Is.EqualTo(new[] { "Forged", "Tempered", "Inferno", "Abyssal" }));
            Assert.That(All.Select(DifficultyTiers.Id), Is.EqualTo(new[] { "forged", "tempered", "inferno", "abyssal" }));
            Assert.That(All.Select(DifficultyTiers.RewardPercent), Is.EqualTo(new[] { 100, 125, 150, 200 }), "the lead's x1 / 1.25 / 1.5 / 2");
            Assert.That(All.Select(DifficultyTiers.RewardText), Is.EqualTo(new[] { "x1", "x1.25", "x1.5", "x2" }));
            Assert.That(All.Select(DifficultyTiers.ItemLuck), Is.EqualTo(new[] { 0f, 10f, 20f, 30f }));
            Assert.That(All.Select(DifficultyTiers.MatchedGearPower), Is.EqualTo(new[] { 0, 1470, 3090, 4190 }),
                "a +5 set, a +9 set, +9 with good efsun (GearStatsTests' +14.7 / +30.9 / +41.9 %)");
            Assert.That(DifficultyTiers.EnemyHealth(DifficultyTier.Tempered), Is.EqualTo(DifficultyTiers.TemperedHealth));
            Assert.That(DifficultyTiers.EnemyDamage(DifficultyTier.Abyssal), Is.EqualTo(DifficultyTiers.AbyssalDamage));
            for (int i = 1; i < All.Length; i++)
            {
                Assert.That(DifficultyTiers.EnemyHealth(All[i]), Is.GreaterThan(DifficultyTiers.EnemyHealth(All[i - 1])), "each tier harder");
                Assert.That(DifficultyTiers.EnemyDamage(All[i]), Is.GreaterThanOrEqualTo(DifficultyTiers.EnemyDamage(All[i - 1])));
            }
            foreach (DifficultyTier tier in All)
            {
                Assert.That(DifficultyTiers.TryParse(DifficultyTiers.Id(tier), out DifficultyTier back), Is.True);
                Assert.That(back, Is.EqualTo(tier));
            }
            Assert.That(DifficultyTiers.TryParse("Tempered", out _), Is.False, "ids are lower case");
            Assert.That(DifficultyTiers.TryParse(null, out _), Is.False);
            Assert.That(DifficultyTiers.IsDefined(4), Is.False);
            Assert.That(DifficultyTiers.IsDefined(-1), Is.False);
            Assert.That(DifficultyTiers.UnlockHint(DifficultyTier.Forged), Is.Empty);
            Assert.That(DifficultyTiers.UnlockHint(DifficultyTier.Tempered), Is.EqualTo("Complete the Ascent on Forged"));
            Assert.That(DifficultyTiers.UnlockHint(DifficultyTier.Abyssal), Is.EqualTo("Complete the Ascent on Inferno"));
            Assert.Throws<ArgumentOutOfRangeException>(() => DifficultyTiers.EnemyHealth((DifficultyTier)4));
        }

        [Test]
        public void TheEnemiesLayerRidesTheFloorsOwn()
        {
            float floor = ClimbScaling.Health(3) * ClimbScaling.Boss(3);
            Assert.That(FloorScaling.TierHealth(floor, DifficultyTier.Tempered), Is.EqualTo(floor * DifficultyTiers.TemperedHealth));
            Assert.That(FloorScaling.TierDamage(ClimbScaling.Damage(2), DifficultyTier.Inferno),
                Is.EqualTo(ClimbScaling.Damage(2) * DifficultyTiers.InfernoDamage));
            Assert.That(FloorScaling.TierHealth(floor, DifficultyTier.Abyssal), Is.EqualTo(FloorScaling.Tiered(floor, DifficultyTiers.AbyssalHealth)),
                "the scene's path and the simulation's are one body");
            // On a 300-health Warden at floor 1's gate share and a difficulty of 1.6: x the tier's health.
            float forged = FloorScaling.Health(300f, FloorScaling.TierHealth(1f, DifficultyTier.Forged) * 1.6f * 8.5f, 0f);
            float tempered = FloorScaling.Health(300f, FloorScaling.TierHealth(1f, DifficultyTier.Tempered) * 1.6f * 8.5f, 0f);
            Assert.That(tempered / forged, Is.EqualTo(DifficultyTiers.TemperedHealth).Within(1e-5f));
            // The damage bonus a weapon carries: the floor's damage times the tier's, less one.
            Assert.That(FloorScaling.DamageBonus(FloorScaling.TierDamage(.92f, DifficultyTier.Tempered), 0f),
                Is.EqualTo(.92f * DifficultyTiers.TemperedDamage - 1f).Within(1e-6f));
        }

        [Test]
        public void TheTiersPayMoreGold()
        {
            Assert.That(FloorScaling.Gold(10, 0f, DifficultyTier.Tempered), Is.EqualTo(13), "12.5 rounds away from zero");
            Assert.That(FloorScaling.Gold(10, 0f, DifficultyTier.Inferno), Is.EqualTo(15));
            Assert.That(FloorScaling.Gold(10, 0f, DifficultyTier.Abyssal), Is.EqualTo(20));
            Assert.That(FloorScaling.Gold(50, 0f, DifficultyTier.Tempered), Is.EqualTo(63));
            Assert.That(FloorScaling.Gold(3, .5f, DifficultyTier.Tempered), Is.EqualTo(6), "Cursed Gold's 4.5, x1.25: 5.625, rounded once");
            Assert.That(FloorScaling.Gold(0, .5f, DifficultyTier.Abyssal), Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.Gold(-1, 0f, DifficultyTier.Tempered));
        }

        [Test]
        public void TheTiersPayMoreMaterialsAndBoostersAndLuckierItems()
        {
            Assert.That(DifficultyTiers.Chance(800, DifficultyTier.Tempered), Is.EqualTo(1000));
            Assert.That(DifficultyTiers.Chance(2500, DifficultyTier.Inferno), Is.EqualTo(3750));
            Assert.That(DifficultyTiers.Chance(6000, DifficultyTier.Abyssal), Is.EqualTo(LootRule.Parts), "never above certain");
            // x1.5 and x2 of two are whole; x1.25 of two is 2.5, its half made whole by a coin.
            RunRandom coin = RunRandom.Stream(9, RunRandom.Loot, 0);
            Assert.That(DifficultyTiers.Amount(2, DifficultyTier.Inferno, coin), Is.EqualTo(3));
            Assert.That(DifficultyTiers.Amount(2, DifficultyTier.Abyssal, coin), Is.EqualTo(4));
            Assert.That(DifficultyTiers.Amount(0, DifficultyTier.Abyssal, coin), Is.Zero);

            const int moments = 4000;
            var counts = new Dictionary<DifficultyTier, (int Material, int Boosters, int Items, double Rarity)>();
            foreach (DifficultyTier tier in All)
            {
                int material = 0, boosters = 0, items = 0;
                double rarity = 0;
                for (int i = 0; i < moments; i++)
                {
                    LootDrop clear = LootRule.Roll(LootSource.FloorClear, 1, 1000 + i, 0, Items, false, tier);
                    LootDrop boss = LootRule.Roll(LootSource.Boss, 1, 1000 + i, 1, Items, false, tier);
                    LootDrop forgedBoss = LootRule.Roll(LootSource.Boss, 1, 1000 + i, 1, Items);
                    material += clear.MaterialAmount;
                    for (int b = 0; b < GearCurrency.BoosterCount; b++)
                        boosters += clear.Booster((BoosterKind)b) + boss.Booster((BoosterKind)b);
                    Assert.That(boss.Item != null, Is.EqualTo(forgedBoss.Item != null),
                        "an item's chance is drawn from the same word on every tier: the same moments drop items");
                    if (boss.Item != null)
                    {
                        items++;
                        rarity += (int)boss.Item.Rarity;
                    }
                }
                counts[tier] = (material, boosters, items, items > 0 ? rarity / items : 0);
            }
            Assert.That(counts[DifficultyTier.Forged].Material, Is.EqualTo(2 * moments));
            Assert.That(counts[DifficultyTier.Tempered].Material / (double)moments, Is.EqualTo(2.5).Within(.05), "x1.25, on average");
            Assert.That(counts[DifficultyTier.Inferno].Material, Is.EqualTo(3 * moments));
            Assert.That(counts[DifficultyTier.Abyssal].Material, Is.EqualTo(4 * moments));
            double forgedBoosters = counts[DifficultyTier.Forged].Boosters;
            Assert.That(counts[DifficultyTier.Tempered].Boosters / forgedBoosters, Is.EqualTo(1.25).Within(.08));
            Assert.That(counts[DifficultyTier.Inferno].Boosters / forgedBoosters, Is.EqualTo(1.5).Within(.1));
            Assert.That(counts[DifficultyTier.Abyssal].Boosters / forgedBoosters, Is.EqualTo(2.0).Within(.12));
            Assert.That(counts.Values.Select(c => c.Items).Distinct().Count(), Is.EqualTo(1), "items drop as often on every tier");
            Assert.That(counts[DifficultyTier.Abyssal].Rarity, Is.GreaterThan(counts[DifficultyTier.Forged].Rarity), "item luck +30");
            Assert.That(counts[DifficultyTier.Inferno].Rarity, Is.GreaterThan(counts[DifficultyTier.Forged].Rarity));

            // The satchel rolls on its run's tier.
            var run = new RunState(10, .5f, 0, 21);
            var loot = new RunLoot(run, Items, false, .5f, DifficultyTier.Abyssal);
            loot.FloorCleared(1);
            Assert.That(loot.Tier, Is.EqualTo(DifficultyTier.Abyssal));
            Assert.That(loot.UnsecuredMaterial(MaterialKind.EmberShard), Is.EqualTo(4), "two Ember Shards, x2");
            Assert.That(new RunLoot(run, Items, false).Tier, Is.EqualTo(DifficultyTier.Forged));
        }

        [Test]
        public void ATierOpensOnceTheDescentBelowItIsCompleted()
        {
            var profile = new PlayerProfile();
            int changes = 0;
            profile.Changed += () => changes++;
            Assert.That(All.Select(profile.Tiers.IsOpen), Is.EqualTo(new[] { true, false, false, false }));
            Assert.That(profile.Tiers.Selected, Is.EqualTo(DifficultyTier.Forged));
            Assert.That(profile.SelectTier(DifficultyTier.Tempered), Is.False, "locked");
            Assert.That(changes, Is.Zero);

            profile.RecordFloorCleared(2);
            Assert.That((profile.DeepestFloorCleared, profile.Tiers.DeepestFloorCleared(DifficultyTier.Forged)), Is.EqualTo((2, 2)),
                "a floor cleared without a tier is Forged's");
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(profile.Tiers.IsOpen(DifficultyTier.Tempered), Is.False, "floor 2 is not the Descent");
            profile.RecordFloorCleared(3, DifficultyTier.Forged);
            profile.RecordFloorCleared(4, DifficultyTier.Forged);
            Assert.That(profile.Tiers.HasCompleted(DifficultyTier.Forged), Is.False, "walking out completes it, not the finale's clear alone");
            Assert.That(profile.RecordDescentCompleted(DifficultyTier.Forged), Is.True);
            Assert.That(profile.RecordDescentCompleted(DifficultyTier.Forged), Is.False, "once");
            Assert.That(changes, Is.EqualTo(4));
            Assert.That(All.Select(profile.Tiers.IsOpen), Is.EqualTo(new[] { true, true, false, false }));
            Assert.That(profile.Tiers.CompletedInOrder, Is.EqualTo(1));

            Assert.That(profile.SelectTier(DifficultyTier.Tempered), Is.True);
            Assert.That(profile.SelectTier(DifficultyTier.Tempered), Is.False, "nothing changed");
            Assert.That(profile.Tiers.Selected, Is.EqualTo(DifficultyTier.Tempered));
            Assert.That(changes, Is.EqualTo(5));

            // Map starts per tier: Forged reached every start, Tempered none yet.
            Assert.That(new[] { 1, 2, 3 }.Select(f => profile.Tiers.IsMapOpen(f, DifficultyTier.Forged)), Is.All.True);
            Assert.That(new[] { 1, 2, 3 }.Select(f => profile.Tiers.IsMapOpen(f, DifficultyTier.Tempered)), Is.EqualTo(new[] { true, false, false }));
            Assert.That(profile.Tiers.IsMapOpen(1, DifficultyTier.Inferno), Is.False, "a locked tier opens no map");
            profile.RecordFloorCleared(1, DifficultyTier.Tempered);
            Assert.That((profile.DeepestFloorCleared, profile.Tiers.DeepestFloorCleared(DifficultyTier.Tempered)), Is.EqualTo((4, 1)),
                "the depth record keeps its best; Tempered's own rises");
            Assert.That(profile.Tiers.IsMapOpen(2, DifficultyTier.Tempered), Is.True, "the Vaults on Tempered once Tempered reached them");
            Assert.That(profile.Tiers.IsMapOpen(3, DifficultyTier.Tempered), Is.False);

            Assert.That(profile.RecordDescentCompleted(DifficultyTier.Tempered), Is.True);
            Assert.That(profile.Tiers.DeepestFloorCleared(DifficultyTier.Tempered), Is.EqualTo(DescentStart.FinaleFloor));
            Assert.That(All.Select(profile.Tiers.IsOpen), Is.EqualTo(new[] { true, true, true, false }));
            Assert.That(profile.SelectTier(DifficultyTier.Abyssal), Is.False);
            Assert.That(profile.SelectTier(DifficultyTier.Inferno), Is.True);
        }

        [Test]
        public void EveryRunBeforeTheTiersWasForged()
        {
            Assert.That(new PlayerProfile(0, null, null, 2).Tiers.DeepestFloorCleared(DifficultyTier.Forged), Is.EqualTo(2));
            var veteran = new PlayerProfile(39491, null, null, 4);
            Assert.That(veteran.Tiers.HasCompleted(DifficultyTier.Forged), Is.True, "a record through the finale walked out of it");
            Assert.That(veteran.Tiers.IsOpen(DifficultyTier.Tempered), Is.True);
            Assert.That(veteran.Tiers.IsOpen(DifficultyTier.Inferno), Is.False);
        }

        [Test]
        public void TheTiersRoundTripAndADamagedSnapshotIsRepaired()
        {
            var profile = new PlayerProfile(0, null, null, 4);
            profile.RecordFloorCleared(2, DifficultyTier.Tempered);
            profile.SelectTier(DifficultyTier.Tempered);
            var data = new ProfileSaveData();
            ProfileTiersSave.Write(profile.Tiers.Snapshot(), data);
            Assert.That(data.tierProgress.Select(t => t.tier), Is.EqualTo(new[] { "forged", "tempered", "inferno", "abyssal" }));
            Assert.That((data.selectedTier, data.highestTierCompleted), Is.EqualTo((1, 1)));
            ProfileTiers back = ProfileTiers.Restore(ProfileTiersSave.ToSnapshot(data), 4);
            Assert.That(Describe(back), Is.EqualTo(Describe(profile.Tiers)));

            var damaged = new TierSnapshot
            {
                Records = new[]
                {
                    null,
                    new TierRecord { Tier = "forged", DeepestFloorCleared = -3, Completed = true },
                    new TierRecord { Tier = "forged", DeepestFloorCleared = 1, Completed = false },
                    new TierRecord { Tier = "mythic", DeepestFloorCleared = 4, Completed = true },
                    new TierRecord { Tier = "", DeepestFloorCleared = 4, Completed = true },
                    new TierRecord { Tier = "Inferno", DeepestFloorCleared = 2, Completed = true },
                    new TierRecord { Tier = "abyssal", DeepestFloorCleared = 3, Completed = true },
                    new TierRecord { Tier = "tempered", DeepestFloorCleared = 2, Completed = false }
                },
                SelectedTier = 3
            };
            ProfileTiers repaired = ProfileTiers.Restore(damaged);
            Assert.That(repaired.HasCompleted(DifficultyTier.Forged), Is.True, "the first forged record wins");
            Assert.That(repaired.DeepestFloorCleared(DifficultyTier.Forged), Is.EqualTo(DescentStart.FinaleFloor),
                "-3 zeroed, then a completed tier stands at the finale");
            Assert.That(repaired.DeepestFloorCleared(DifficultyTier.Tempered), Is.EqualTo(2));
            Assert.That(repaired.HasCompleted(DifficultyTier.Tempered), Is.False);
            Assert.That(repaired.HasCompleted(DifficultyTier.Inferno), Is.False, "\"Inferno\" is not an id");
            Assert.That((repaired.HasCompleted(DifficultyTier.Abyssal), repaired.DeepestFloorCleared(DifficultyTier.Abyssal)),
                Is.EqualTo((false, 0)), "Abyssal is not open (Inferno not completed): it keeps no record");
            Assert.That(repaired.Selected, Is.EqualTo(DifficultyTier.Forged), "a locked choice falls back to Forged");
            Assert.That(ProfileTiers.Restore(new TierSnapshot { SelectedTier = 9 }).Selected, Is.EqualTo(DifficultyTier.Forged));
            Assert.That(ProfileTiers.Restore(null).IsOpen(DifficultyTier.Tempered), Is.False);
            Assert.That(ProfileTiers.Restore(null, 4).IsOpen(DifficultyTier.Tempered), Is.True, "the profile's own record is Forged's");
            Assert.That(ProfileTiers.Restore(new TierSnapshot { Records = null, SelectedTier = 1 }, 4).Selected, Is.EqualTo(DifficultyTier.Tempered));
        }

        [Test]
        public void VersionFourMigratesItsDepthToForged()
        {
            ProfileSaveData veteran = ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 4, revision = 3, gold = 39491, deepestFloorCleared = 4 });
            Assert.That(veteran.saveVersion, Is.EqualTo(ProfileSaveData.CurrentVersion), "through 5 (the tiers) to the current");
            Assert.That(veteran.gold, Is.EqualTo(39491));
            Assert.That(veteran.tierProgress.Select(t => (t.tier, t.deepestFloorCleared, t.completed)), Is.EqualTo(new[]
            {
                ("forged", 4, true), ("tempered", 0, false), ("inferno", 0, false), ("abyssal", 0, false)
            }));
            ProfileTiers tiers = ProfileTiers.Restore(ProfileTiersSave.ToSnapshot(veteran), veteran.deepestFloorCleared);
            Assert.That(All.Select(tiers.IsOpen), Is.EqualTo(new[] { true, true, false, false }));

            ProfileSaveData midway = ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 4, deepestFloorCleared = 2, selectedTier = 2 });
            Assert.That(midway.tierProgress[0].completed, Is.False);
            Assert.That(midway.tierProgress[0].deepestFloorCleared, Is.EqualTo(2));
            ProfileTiers fresh = ProfileTiers.Restore(ProfileTiersSave.ToSnapshot(midway), midway.deepestFloorCleared);
            Assert.That(fresh.IsOpen(DifficultyTier.Tempered), Is.False);
            Assert.That(fresh.Selected, Is.EqualTo(DifficultyTier.Forged), "a tier chosen ahead of its build, not open, falls back");
            Assert.That(fresh.IsMapOpen(3, DifficultyTier.Forged), Is.True, "the Gardens stay open on Forged");

            ProfileSaveData counted = ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 4, deepestFloorCleared = 4, highestTierCompleted = 2 });
            Assert.That(counted.tierProgress.Select(t => t.completed), Is.EqualTo(new[] { true, true, false, false }),
                "version 4's count of tiers completed carries over");
            Assert.That(ProfileMigration.Upgrade(new ProfileSaveData { saveVersion = 1, deepestFloorCleared = 3 }).tierProgress[0].deepestFloorCleared,
                Is.EqualTo(3), "every older version passes 4 to 5");
        }

        // The simulation plays the tier through the scene's body: floor 1's Warden walks out at the tier's health.
        [Test]
        public void TheSimulationPlaysTheTiersEnemies()
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped();
            foreach (int seed in new[] { 3, 11 })
            {
                DescentSimulation.FoundryResult forged = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), seed, levers);
                DescentSimulation.FoundryHero hero = DescentSimulation.Vanguard();
                hero.Tier = DifficultyTier.Tempered;
                DescentSimulation.FoundryResult tempered = DescentSimulation.RunFoundry(hero, seed, levers);
                Assume.That(forged.LevelAtGate >= 0 && tempered.LevelAtGate >= 0, "both reached the gate");
                float share = levers.WardenHealthShare;
                Assert.That(forged.WardenHealth, Is.EqualTo(DescentSimulation.Warden.Health * share * forged.DifficultyAtGate).Within(.05f));
                Assert.That(tempered.WardenHealth,
                    Is.EqualTo(DescentSimulation.Warden.Health * DifficultyTiers.TemperedHealth * share * tempered.DifficultyAtGate).Within(.05f),
                    "x the tier's health on the floor's own");
            }
        }

        private static string Describe(LootDrop drop)
        {
            string boosters = string.Join(",", Enumerable.Range(0, GearCurrency.BoosterCount).Select(b => drop.Booster((BoosterKind)b)));
            string item = drop.Item == null ? "-" : $"{drop.Item.ItemId} {drop.Item.Rarity} " +
                string.Join(",", drop.Item.Lines.Select(l => GearStatIds.Id(l.Stat) + ":" + l.Tier));
            return $"{drop.Source} {drop.HasMaterial} {drop.Material} {drop.MaterialAmount} [{boosters}] {item}";
        }

        private static string Describe(ProfileTiers tiers) =>
            string.Join(" ", All.Select(t => $"{t}:{tiers.DeepestFloorCleared(t)}/{tiers.HasCompleted(t)}")) + " selected=" + tiers.Selected;
    }
}
