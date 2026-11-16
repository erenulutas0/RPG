using System.Collections.Generic;
using System.Linq;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Anvil over the profile (docs/54 phase 2, the lead's decisions of 2026-11-16): a strike spends gold, the floor's
    // material and the chosen booster, keeps the item on a failure and is drawn from the profile's own stream; a reroll is
    // kept or discarded; a rune adds a line; melt, inherit and equip. Every operation is one profile change. Also in
    // Tools/CombatChecks.
    public sealed class AnvilTests
    {
        private static PlayerProfile Rich(int gold = 100000)
        {
            var profile = new PlayerProfile();
            profile.Deposit(gold);
            return profile;
        }

        private static ItemInstance Ring(PlayerProfile profile, UpgradeRarity rarity = UpgradeRarity.Epic) =>
            ProfileGearTests.Give(profile, "item_cinder_ring", GearSlot.Ring, rarity, new EfsunLine(GearStat.Damage, 1),
                new EfsunLine(GearStat.Luck, 2));

        [Test]
        public void AStrikeSpendsGoldAndMaterialAndIsOneChange()
        {
            PlayerProfile profile = Rich(1000);
            var anvil = new Anvil(profile, () => 11);
            ItemInstance ring = Ring(profile);
            int changes = 0;
            profile.Changed += () => changes++;
            var attempts = new List<AnvilAttempt>();
            anvil.Struck += attempts.Add;

            Assert.That(anvil.TryStrike(ring, false, false, out AnvilStrike first), Is.True);
            Assert.That(first.Success && ring.Level == 1, Is.True, "+1 always holds.");
            Assert.That(profile.Gold, Is.EqualTo(950));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(attempts.Single().Gold, Is.EqualTo(50));

            anvil.TryStrike(ring, false, false, out _);
            anvil.TryStrike(ring, false, false, out _);
            Assert.That(ring.Level, Is.EqualTo(3));
            AnvilQuote four = anvil.Quote(ring);
            Assert.That(four.Refusal, Is.EqualTo(AnvilRefusal.Material), "+4 wants two Ember Shards.");
            Assert.That(anvil.TryStrike(ring, false, false, out _), Is.False);
            Assert.That(anvil.LastRefusal, Is.EqualTo(AnvilRefusal.Material));
            Assert.That(profile.Gold, Is.EqualTo(1000 - 50 - 80 - 120), "A refused strike spends nothing.");
            profile.AddMaterial(MaterialKind.EmberShard, 2);
            Assert.That(anvil.TryStrike(ring, false, false, out _), Is.True);
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.Zero);
        }

        [Test]
        public void AFailureKeepsTheItemAndTheCeilingHolds()
        {
            PlayerProfile profile = Rich();
            profile.AddMaterial(MaterialKind.GloamSpore, 1000);
            var anvil = new Anvil(profile, () => 3);
            ItemInstance ring = Ring(profile);
            Promote(ring, 8);
            int strikes = 0;
            while (ring.Level == 8 && strikes < 20)
            {
                int pityBefore = ring.Pity;
                Assert.That(anvil.TryStrike(ring, false, false, out AnvilStrike strike), Is.True);
                strikes++;
                if (!strike.Success)
                {
                    Assert.That(ring.Level, Is.EqualTo(8), "Never a downgrade.");
                    Assert.That(profile.Gear.Owns(ring), Is.True, "Never a break.");
                    Assert.That(ring.Pity, Is.EqualTo(pityBefore + 1));
                }
            }
            Assert.That(ring.Level, Is.EqualTo(9));
            Assert.That(strikes, Is.LessThanOrEqualTo(11), "The ceiling holds the eleventh.");
            Assert.That(ring.Pity, Is.Zero);
            Assert.That(anvil.Quote(ring).Refusal, Is.EqualTo(AnvilRefusal.MaxLevel));
        }

        [Test]
        public void TheSameSeedStrikesTheSameWay()
        {
            string Play(int seed)
            {
                PlayerProfile profile = Rich();
                profile.AddMaterial(MaterialKind.EmberShard, 100);
                profile.AddMaterial(MaterialKind.QuicksilverBead, 100);
                profile.AddMaterial(MaterialKind.GloamSpore, 100);
                var anvil = new Anvil(profile, () => seed);
                ItemInstance ring = Ring(profile);
                var outcomes = new List<string>();
                for (int i = 0; i < 25 && ring.Level < 9; i++)
                {
                    anvil.TryStrike(ring, false, false, out AnvilStrike strike);
                    outcomes.Add(strike.Success ? "+" : "-");
                }
                return string.Concat(outcomes) + " draws " + profile.Gear.AnvilDraws;
            }
            Assert.That(Play(42), Is.EqualTo(Play(42)));
            Assert.That(Enumerable.Range(1, 10).Select(Play).Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void SealsAndHammersAreSpentOnlyWhereTheyCount()
        {
            PlayerProfile profile = Rich();
            profile.AddMaterial(MaterialKind.EmberShard, 100);
            profile.AddMaterial(MaterialKind.GloamSpore, 100);
            profile.AddBooster(BoosterKind.ForgeSeal, 2);
            profile.AddBooster(BoosterKind.MastersHammer, 1);
            var anvil = new Anvil(profile, () => 9);
            ItemInstance ring = Ring(profile);

            Assert.That(anvil.TryStrike(ring, true, false, out _), Is.True);
            Assert.That(profile.Gear.Booster(BoosterKind.ForgeSeal), Is.EqualTo(2), "+1 is certain: the Seal is kept.");
            Promote(ring, 5);
            Assert.That(anvil.Quote(ring, false, true).ChancePercent, Is.EqualTo(100));
            Assert.That(anvil.TryStrike(ring, false, true, out AnvilStrike hammered), Is.True);
            Assert.That(hammered.Success && ring.Level == 6, Is.True);
            Assert.That(profile.Gear.Booster(BoosterKind.MastersHammer), Is.Zero);
            Assert.That(anvil.TryStrike(ring, false, true, out _), Is.False, "No Hammer past +6, and none left.");
            Assert.That(anvil.LastRefusal, Is.EqualTo(AnvilRefusal.Hammer));

            Promote(ring, 8);
            Assert.That(anvil.Quote(ring, true).ChancePercent, Is.EqualTo(30), "20 + a Seal's 10.");
            Assert.That(anvil.TryStrike(ring, true, false, out AnvilStrike sealed_), Is.True);
            Assert.That(sealed_.ChancePercent, Is.EqualTo(30));
            Assert.That(profile.Gear.Booster(BoosterKind.ForgeSeal), Is.EqualTo(1));
        }

        [Test]
        public void ARerollIsKeptOrDiscardedAndBlocksTheRestUntilThen()
        {
            PlayerProfile profile = Rich(1000);
            profile.AddBooster(BoosterKind.EfsunScroll, 1);
            var anvil = new Anvil(profile, () => 21);
            ItemInstance ring = Ring(profile);
            EfsunLine[] old = ring.Lines.ToArray();
            var rerolls = new List<EfsunAttempt>();
            anvil.Rerolled += rerolls.Add;

            Assert.That(anvil.TryReroll(ring, false), Is.True);
            Assert.That(profile.Gold, Is.EqualTo(840), "An Epic's reroll is 160.");
            Assert.That(ring.Lines, Is.EqualTo(old), "The old lines stay until the player keeps the new.");
            Assert.That(profile.Gear.PendingRerollLines.Count, Is.EqualTo(2));
            Assert.That(profile.Gear.PendingRerollLines.Any(l => l.Stat == GearStat.CritChance), Is.False, "Never the ring's main stat.");
            Assert.That(anvil.TryStrike(ring, false, false, out _), Is.False);
            Assert.That(anvil.LastRefusal, Is.EqualTo(AnvilRefusal.RerollPending));
            EfsunLine[] shown = profile.Gear.PendingRerollLines.ToArray();
            Assert.That(anvil.ResolveReroll(false), Is.True);
            Assert.That(ring.Lines, Is.EqualTo(old), "Kept the old.");

            Assert.That(anvil.TryReroll(ring, true), Is.True);
            Assert.That(profile.Gold, Is.EqualTo(840), "A scroll instead of gold.");
            Assert.That(profile.Gear.Booster(BoosterKind.EfsunScroll), Is.Zero);
            shown = profile.Gear.PendingRerollLines.ToArray();
            Assert.That(anvil.ResolveReroll(true), Is.True);
            Assert.That(ring.Lines, Is.EqualTo(shown), "Kept the new.");
            Assert.That(rerolls.Count, Is.EqualTo(2));
            Assert.That(anvil.ResolveReroll(true), Is.False, "Nothing is shown.");
        }

        [Test]
        public void ARuneAddsALineUpToFive()
        {
            PlayerProfile profile = Rich();
            profile.AddBooster(BoosterKind.BindingRune, 5);
            var anvil = new Anvil(profile, () => 8);
            ItemInstance ring = Ring(profile);
            Assert.That(anvil.TryAddLine(ring), Is.True);
            Assert.That(anvil.TryAddLine(ring), Is.True);
            Assert.That(anvil.TryAddLine(ring), Is.True);
            Assert.That(ring.Lines.Count, Is.EqualTo(5));
            Assert.That(ring.Lines.Select(l => l.Stat).Distinct().Count(), Is.EqualTo(5));
            Assert.That(anvil.TryAddLine(ring), Is.False);
            Assert.That(anvil.LastRefusal, Is.EqualTo(AnvilRefusal.FullLines));
            Assert.That(profile.Gear.Booster(BoosterKind.BindingRune), Is.EqualTo(2));
        }

        [Test]
        public void MeltInheritAndEquip()
        {
            PlayerProfile profile = Rich(10000);
            var anvil = new Anvil(profile, () => 4);
            ItemInstance oldRing = Ring(profile, UpgradeRarity.Common);
            ItemInstance goldRing = Ring(profile, UpgradeRarity.Gold);
            ItemInstance spare = Ring(profile, UpgradeRarity.Rare);
            Promote(oldRing, 7);
            Assert.That(anvil.TryEquip(oldRing), Is.True);
            Assert.That(anvil.TryEquip(oldRing), Is.False, "Already equipped.");

            Assert.That(anvil.TryMelt(oldRing), Is.False);
            Assert.That(anvil.LastRefusal, Is.EqualTo(AnvilRefusal.Equipped));
            int gold = profile.Gold;
            Assert.That(anvil.TryMelt(spare), Is.True);
            Assert.That(profile.Gold - gold, Is.EqualTo(40), "A Rare melts for 40 gold and two Ember Shards.");
            Assert.That(profile.Gear.Material(MaterialKind.EmberShard), Is.EqualTo(2));
            Assert.That(profile.Gear.Owns(spare), Is.False);

            gold = profile.Gold;
            Assert.That(anvil.TryInherit(goldRing, oldRing), Is.False, "Only a higher level moves down.");
            Assert.That(anvil.TryInherit(oldRing, goldRing), Is.True);
            Assert.That(gold - profile.Gold, Is.EqualTo(AnvilRule.InheritGold(7)));
            Assert.That(goldRing.Level, Is.EqualTo(7));
            Assert.That(profile.Gear.Owns(oldRing), Is.False, "The source is consumed.");
            Assert.That(profile.Gear.Equipped(GearSlot.Ring), Is.SameAs(goldRing), "It takes the source's place.");
            Assert.That(goldRing.Lines.Count, Is.EqualTo(2), "Its lines stay its own.");

            ItemInstance sword = anvil.GearForWeapon("weapon_sword");
            Assert.That(sword.Lines.Count, Is.EqualTo(EfsunRule.WeaponLines));
            Assert.That(anvil.GearForWeapon("weapon_sword"), Is.SameAs(sword));
            Assert.That(anvil.TryMelt(sword), Is.False);
            Assert.That(anvil.LastRefusal, Is.EqualTo(AnvilRefusal.Weapon));
            Assert.That(anvil.TryStrike(sword, false, false, out _), Is.True, "A weapon is struck like any piece.");
            Assert.That(profile.Gear.EquippedPieces("weapon_sword"), Is.EqualTo(new[] { sword, goldRing }));
        }

        // Raises a piece to a level as a restored save would hold it: through the anvil's own path it would take dozens of
        // strikes, and the setter is the profile's (internal).
        private static void Promote(ItemInstance piece, int level)
        {
            typeof(ItemInstance).GetProperty(nameof(ItemInstance.Level)).SetValue(piece, level);
            typeof(ItemInstance).GetProperty(nameof(ItemInstance.Pity)).SetValue(piece, 0);
        }
    }
}
