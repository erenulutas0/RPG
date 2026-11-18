using System.Collections.Generic;
using System.Linq;
using Cryptforge.Arena;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The arena's rules and builds (docs/55 P0): the rules' version and hash pinned together, the arena's copies of the
    // game's tables (the random stream, the gear's values, caps and power) equal to the game's own, ranked normalising what
    // is owned and keeping what is chosen, open counting the gear in full, the resolver's repairs, and a snapshot's ids
    // round-tripping with its hash. Also in Tools/CombatChecks.
    public sealed class ArenaBuildTests
    {
        // The shipped table's content hash (ArenaRules.Current.Hash). A change to any arena number moves it: bump
        // ArenaRules.ShippedVersion, write both pins (this and DuelCoreTests' fingerprint), and say so in docs/55.
        private const int PinnedVersion = 1;
        private const ulong PinnedHash = 0x6988134A9881CFE8UL;

        [Test]
        public void TheRulesVersionAndHashArePinnedTogether()
        {
            Assert.That(ArenaRules.Current.Version, Is.EqualTo(PinnedVersion));
            Assert.That(ArenaRules.Current.Hash, Is.EqualTo(PinnedHash),
                $"0x{ArenaRules.Current.Hash:X16}: the arena's rules changed - bump ArenaRules.ShippedVersion and the pins");
            Assert.That(ArenaRules.Current.WithRanked(4, 3).Hash, Is.Not.EqualTo(ArenaRules.Current.Hash), "every number is in the hash");
        }

        [Test]
        public void TheArenasStreamIsRunRandomWordForWord()
        {
            foreach (int seed in new[] { 0, 1, -7, 2026, int.MaxValue })
            foreach (int purpose in new[] { 1, 8, ArenaRandom.Crits, ArenaRandom.Fights })
            foreach (int index in new[] { 0, 1, 99 })
            {
                ArenaRandom arena = ArenaRandom.Stream(seed, purpose, index);
                RunRandom run = RunRandom.Stream(seed, purpose, index);
                for (int i = 0; i < 20; i++)
                {
                    Assert.That(arena.Next(), Is.EqualTo(run.Next()));
                    Assert.That(arena.NextBelow(37), Is.EqualTo(run.NextBelow(37)));
                }
            }
        }

        [Test]
        public void TheGearTablesAreTheGamesOwn()
        {
            Assert.That(ArenaGearTable.StatCount, Is.EqualTo(GearStatIds.Count));
            for (int i = 0; i < GearStatIds.Count; i++)
            {
                Assert.That(ArenaGearTable.Id((ArenaStat)i), Is.EqualTo(GearStatIds.Id((GearStat)i)));
                Assert.That(ArenaGearTable.Cap((ArenaStat)i), Is.EqualTo(GearStats.Cap((GearStat)i)));
                for (int tier = 1; tier <= EfsunRule.TierCount; tier++)
                    Assert.That(ArenaGearTable.LineValue((ArenaStat)i, tier), Is.EqualTo(EfsunRule.ValueOf((GearStat)i, tier)));
            }
            for (int slot = 0; slot < ArenaGearTable.SlotCount; slot++)
            {
                Assert.That((int)ArenaGearTable.MainStatOf(slot), Is.EqualTo((int)GearStats.MainStatOf((GearSlot)slot)));
                for (int level = 0; level <= AnvilTable.MaxLevel; level++)
                    Assert.That(ArenaGearTable.MainValue(slot, level), Is.EqualTo(GearStats.MainValue((GearSlot)slot, level)));
            }
            var dice = new System.Random(55);
            for (int i = 0; i < 200; i++)
            {
                int[] totals = Enumerable.Range(0, GearStatIds.Count).Select(s => dice.Next(0, GearStats.Cap((GearStat)s) + 1)).ToArray();
                Assert.That(ArenaGearTable.Power(totals), Is.EqualTo(GearStats.Power(totals)));
            }
        }

        [Test]
        public void RankedKeepsTheChoiceAndNormalisesWhatIsOwned()
        {
            // A +9 profile with a Gold ring and a bare one pick the same line types: the duel sees the same build.
            var geared = new PlayerProfile(0, null, null, 0, new[] { ArenaRules.Daggers }, ArenaRules.Daggers, null, null, ProfileGear.Restore(Geared()));
            var bare = new PlayerProfile(0, null, null, 0, new[] { ArenaRules.Daggers }, ArenaRules.Daggers);
            ArenaStat[] lines = { ArenaStat.CritChance, ArenaStat.Armour, ArenaStat.MaxHealth };
            ArenaFighter a = ArenaResolver.Resolve(ArenaProfileBuild.Ranked(geared, ArenaRules.Vanguard, lines));
            ArenaFighter b = ArenaResolver.Resolve(ArenaProfileBuild.Ranked(bare, ArenaRules.Vanguard, lines));
            Assert.That(a.Weapon.Id, Is.EqualTo(ArenaRules.Daggers), "the weapon's type is kept");
            Assert.That(a.Build.Hash, Is.EqualTo(b.Build.Hash));
            for (int i = 0; i < ArenaGearTable.StatCount; i++)
                Assert.That(a.Total((ArenaStat)i), Is.EqualTo(b.Total((ArenaStat)i)), ArenaGearTable.Id((ArenaStat)i));
            Assert.That(a.Total(ArenaStat.Damage), Is.EqualTo(ArenaGearTable.MainValue(ArenaGearTable.WeaponSlot, ArenaRules.Current.RankedLevel)),
                "the common + level's weapon damage, no damage line picked");
            Assert.That(a.Credited(ArenaStat.CritChance), Is.EqualTo(ArenaRules.Current.RankedLineValue(ArenaStat.CritChance)));
            Assert.That(a.Credited(ArenaStat.Damage), Is.Zero, "the common level is not the build's own");
            Assert.That(DuelCore.BestOfThree(a, a, 5).Fingerprint, Is.EqualTo(DuelCore.BestOfThree(b, b, 5).Fingerprint),
                "the +9 and the bare profile fight the same fight");
        }

        [Test]
        public void OpenCountsTheGearInFull()
        {
            var profile = new PlayerProfile(0, null, null, 0, null, null, null, null, ProfileGear.Restore(Geared()));
            ArenaBuild build = ArenaProfileBuild.Open(profile, ArenaRules.Vanguard);
            ArenaFighter f = ArenaResolver.Resolve(build);
            var loadout = new GearLoadout(profile.Gear.EquippedPieces(ArenaRules.Sword));
            Assert.That(f.Weapon.Id, Is.EqualTo(ArenaRules.Sword), "no weapon equipped: the starting Sword");
            foreach (GearStat stat in new[] { GearStat.Damage, GearStat.AttackSpeed, GearStat.CritChance, GearStat.MaxHealth, GearStat.Armour, GearStat.MoveSpeed })
                Assert.That(f.Total((ArenaStat)(int)stat), Is.EqualTo(loadout.Total(stat)), GearStatIds.Id(stat));
            Assert.That(f.GearPower, Is.EqualTo(loadout.Power));
            Assert.That(f.Total(ArenaStat.PickupRadius), Is.Zero, "nothing in the lane to pick up");
            Assert.That(ArenaResolver.Resolve(ArenaProfileBuild.Ranked(profile, ArenaRules.Vanguard, null)).GearPower,
                Is.LessThan(f.GearPower), "ranked ignores the +9");
        }

        [Test]
        public void TheResolverRepairsABuild()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaBuild odd = ArenaBuild.Ranked(ArenaRules.Ilya, ArenaRules.Sword, new[] { ArenaRules.IronResolve, ArenaRules.CinderWard },
                ArenaPolicy.Bold, new[] { ArenaStat.Luck, ArenaStat.BurnDamage, ArenaStat.BurnDamage, ArenaStat.AbilityCooldown });
            ArenaFighter f = ArenaResolver.Resolve(odd, rules);
            Assert.That(f.Weapon.Id, Is.EqualTo(ArenaRules.Staff), "Ilya carries the Staff only");
            Assert.That(f.Skills.Select(s => s.Id), Is.EqualTo(new[] { ArenaRules.CinderWard, ArenaRules.CrucibleVent, ArenaRules.FlameSigil }),
                "its own skills, the build's order first");
            Assert.That(f.LineTypes.Count, Is.EqualTo(rules.RankedLines), "always the same number of lines");
            Assert.That(f.LineTypes[0], Is.EqualTo(ArenaStat.BurnDamage));
            Assert.That(f.LineTypes.Distinct().Count(), Is.EqualTo(f.LineTypes.Count));
            Assert.That(f.LineTypes, Has.No.Member(ArenaStat.AbilityCooldown), "ranked has no cooldown line (docs/57 decision 8)");
            Assert.That(f.LineTypes, Has.No.Member(ArenaStat.Luck));
            ArenaFighter v = ArenaResolver.Resolve(ArenaBuild.Ranked(ArenaRules.Vanguard, null, null, ArenaPolicy.Careful,
                new[] { ArenaStat.BurnDamage }), rules);
            Assert.That(v.LineTypes, Has.No.Member(ArenaStat.BurnDamage), "the Vanguard does not burn");
            Assert.That(rules.InRankedPool(ArenaStat.MoveSpeed, v.Hero), Is.False, "a chaser keeps up with a kiter: speed buys nothing in the lane");
            Assert.That(v.Weapon.Id, Is.EqualTo(ArenaRules.Sword));
            Assert.That(() => ArenaResolver.Resolve(ArenaBuild.Ranked("hero_unknown", null, null, ArenaPolicy.Careful, null), rules),
                Throws.ArgumentException);
        }

        [Test]
        public void ASnapshotRoundTripsWithItsHash()
        {
            var profile = new PlayerProfile(0, null, null, 0, null, null, null, null, ProfileGear.Restore(Geared()));
            foreach (ArenaBuild build in new[]
                     {
                         ArenaProfileBuild.Open(profile, ArenaRules.Vanguard, ArenaPolicy.Bold),
                         ArenaProfileBuild.Ranked(profile, ArenaRules.Ilya, new[] { ArenaStat.MoveSpeed, ArenaStat.BurnDamage, ArenaStat.Armour })
                     })
            {
                ArenaSnapshotData data = ArenaSnapshots.ToData(build);
                ArenaBuild back = ArenaSnapshots.ToBuild(data);
                Assert.That(back.Hash, Is.EqualTo(build.Hash));
                Assert.That(data.hash, Is.EqualTo(ArenaHash.Hex(build.Hash)));
                Assert.That(data.rulesVersion, Is.EqualTo(ArenaRules.Current.Version));
            }
            ArenaBuild a = ArenaBuild.Ranked(ArenaRules.Vanguard, ArenaRules.Sword, null, ArenaPolicy.Careful, new[] { ArenaStat.Damage });
            Assert.That(a.Hash, Is.Not.EqualTo(a.AsRanked(new[] { ArenaStat.Armour }).Hash));
            Assert.That(a.Hash, Is.Not.EqualTo(a.WithPolicy(ArenaPolicy.Bold).Hash));
        }

        [Test]
        public void ASnapshotIsReResolvedUnderTheFightsRules()
        {
            ArenaBuild build = ArenaRivals.Ranked(9, 3);
            ArenaRules patched = ArenaRules.Current.WithRankedLineValue(build.LineTypes[0], ArenaRules.Current.RankedLineValue(build.LineTypes[0]) + 100);
            ArenaFighter now = ArenaResolver.Resolve(build);
            ArenaFighter later = ArenaResolver.Resolve(build, patched);
            Assert.That(later.Total(build.LineTypes[0]), Is.GreaterThan(now.Total(build.LineTypes[0])), "a patch reaches every stored build");
            Assert.That(later.Rules.Hash, Is.Not.EqualTo(now.Rules.Hash));
        }

        // A +9 Sword with two lines, a +9 Gold ring with four, a +7 armour: well past anything ranked allows.
        private static GearSnapshot Geared() => new GearSnapshot
        {
            Items = new[]
            {
                new GearRecord
                {
                    Uid = 1, ItemId = "item_cinder_ring", Slot = (int)GearSlot.Ring, Rarity = 3, Level = 9,
                    Lines = new[]
                    {
                        new GearLineRecord("damage", 5), new GearLineRecord("health", 4), new GearLineRecord("attack_speed", 3),
                        new GearLineRecord("pickup_radius", 5)
                    }
                },
                new GearRecord { Uid = 2, ItemId = "item_bark_mail", Slot = (int)GearSlot.Armour, Rarity = 1, Level = 7,
                    Lines = new[] { new GearLineRecord("armour", 5), new GearLineRecord("move_speed", 2) } }
            },
            Weapons = new[]
            {
                new GearRecord { ItemId = ArenaRules.Sword, Level = 9, Lines = new[] { new GearLineRecord("crit", 4), new GearLineRecord("ability_cooldown", 5) } }
            },
            EquippedUids = new[] { 2, 0, 1 },
            NextItemUid = 3
        };
    }
}
