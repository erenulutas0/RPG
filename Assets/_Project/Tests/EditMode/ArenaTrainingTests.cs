using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cryptforge.Arena;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/55 section 10.10's training rivals and open offers: a handicap takes its share of a rival's damage and nothing
    // of an unhandicapped fight; the ranked ladder is the training ladder, each generated rival stamped with its rung's
    // handicap under the rules' version; a version-1 save migrates; a rules change restamps; the open arena offers three
    // rivals below, level and above the player's gear power, each with its card's chance from the rules' fitted table, and
    // the rating moves by what the card promised. Also in Tools/CombatChecks.
    public sealed class ArenaTrainingTests
    {
        [Test]
        public void AHandicapTakesItsShareOfTheRivalsDamage()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaFighter a = DuelCoreTests.Fighter(ArenaRules.Vanguard, ArenaRules.Sword, ArenaPolicy.Careful, ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.Armour);
            ArenaBuild plain = a.Build;
            ArenaFighter even = ArenaResolver.Resolve(plain, rules);
            ArenaFighter weak = ArenaResolver.Resolve(plain.WithHandicap(900, rules.Version), rules);
            Assert.That(weak.HandicapPermille, Is.EqualTo(900));
            Assert.That(even.HandicapPermille, Is.EqualTo(ArenaBuild.NoHandicap));
            Assert.That(weak.Build.Hash, Is.Not.EqualTo(even.Build.Hash), "the handicap is part of the snapshot");

            // The same round, B handicapped or not: B's first strike lands at the same tick, nine tenths as hard.
            var full = new DuelLog();
            var cut = new DuelLog();
            DuelCore.Round(a, even, 7, full);
            DuelCore.Round(a, weak, 7, cut);
            DuelEvent first = full.Events.First(e => e.Kind == DuelEventKind.Strike && e.Side == 1);
            DuelEvent firstCut = cut.Events.First(e => e.Kind == DuelEventKind.Strike && e.Side == 1);
            Assert.That(firstCut.Tick, Is.EqualTo(first.Tick));
            Assert.That(firstCut.Value, Is.EqualTo(first.Value * 9 / 10).Within(first.Value / 100 + 1));
            Assert.That(cut.Events.First(e => e.Kind == DuelEventKind.Strike && e.Side == 0).Value,
                Is.EqualTo(full.Events.First(e => e.Kind == DuelEventKind.Strike && e.Side == 0).Value), "the player's own damage is untouched");

            int wins = Enumerable.Range(1, 200).Count(seed => DuelCore.BestOfThree(a, weak, seed).Winner == 0);
            Assert.That(wins, Is.GreaterThan(170), "a tenth off a rival's damage decides an auto-duel");
        }

        [Test]
        public void TheRankedLadderIsTheTrainingLadder()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaSaveData save = ArenaLadder.NewSave(2611, rules);
            Assert.That(save.ranked.training, Is.True);
            Assert.That(save.open.training, Is.False);
            Assert.That(rules.TrainingRungs, Is.EqualTo(ArenaLadder.Size));
            for (int i = 0; i < save.ranked.rivals.Count; i++)
            {
                ArenaRivalData rival = save.ranked.rivals[i];
                Assert.That(rival.rung, Is.EqualTo(i));
                Assert.That(ArenaLadder.IsTrainingRival(save.ranked, rival), Is.True);
                Assert.That(rival.build.handicap, Is.EqualTo(rules.TrainingHandicap(i)));
                Assert.That(rival.build.rulesVersion, Is.EqualTo(rules.Version), "stamped under the rules' version");
                Assert.That(ArenaSnapshots.ToBuild(rival.build).HandicapPermille, Is.EqualTo(rules.TrainingHandicap(i)));
                if (i > 0)
                    Assert.That(rules.TrainingHandicap(i), Is.GreaterThanOrEqualTo(rules.TrainingHandicap(i - 1)), "easing rung by rung");
            }
            Assert.That(rules.TrainingHandicap(0), Is.InRange(950, 990), "about 75 % for the player at the bottom");
            Assert.That(rules.TrainingHandicap(ArenaLadder.Size - 1), Is.EqualTo(ArenaBuild.NoHandicap), "the top rung is a real rival");
            Assert.That(save.open.rivals.All(r => r.build.handicap == ArenaBuild.NoHandicap), Is.True, "no handicap in the open arena");

            ArenaBuild player = ArenaRivals.Ranked(31, 31, rules);
            ArenaFightOutcome outcome = ArenaLadder.Fight(save, ArenaMode.Ranked, 0, player, rules);
            Assert.That(outcome.TrainingRival, Is.True);
            Assert.That(outcome.Foe.HandicapPermille, Is.EqualTo(rules.TrainingHandicap(0)));
            Assert.That(outcome.Player.HandicapPermille, Is.EqualTo(ArenaBuild.NoHandicap), "never the player's");

            Assert.That(ArenaLadder.KeepSnapshot(save, player), Is.True);
            ArenaRivalData self = save.ranked.rivals.Single(r => r.pastSelf);
            Assert.That(ArenaLadder.IsTrainingRival(save.ranked, self), Is.False, "a past hero is not a training rival");
            Assert.That(self.build.handicap, Is.EqualTo(ArenaBuild.NoHandicap));
            Assert.That(self.rung, Is.EqualTo(-1));
        }

        [Test]
        public void AVersionOneSaveMigrates()
        {
            ArenaRules rules = ArenaRules.Current;
            // A version-1 save as P0's core wrote it: no training flag, no rungs, no handicaps.
            ArenaSaveData old = ArenaLadder.NewSave(77, rules);
            old.saveVersion = 1;
            old.ranked.training = false;
            foreach (ArenaRivalData rival in old.ranked.rivals)
            {
                rival.rung = -1;
                rival.build.handicap = ArenaBuild.NoHandicap;
                rival.build.rulesVersion = 1;
            }
            old.rulesVersion = 1;
            string text = ArenaSaveJson.Write(old).Replace("\"saveVersion\": 2", "\"saveVersion\": 1");

            ArenaSaveData read = ArenaSaveJson.Read(text, rules);
            Assert.That(read.saveVersion, Is.EqualTo(ArenaSaveData.CurrentVersion));
            Assert.That(read.ranked.training, Is.True, "the ranked rating becomes a training rating");
            Assert.That(read.rulesVersion, Is.EqualTo(rules.Version));
            for (int i = 0; i < read.ranked.rivals.Count; i++)
            {
                Assert.That(read.ranked.rivals[i].rung, Is.EqualTo(i), "the rung its id names");
                Assert.That(read.ranked.rivals[i].build.handicap, Is.EqualTo(rules.TrainingHandicap(i)));
                Assert.That(read.ranked.rivals[i].build.rulesVersion, Is.EqualTo(rules.Version));
            }
            Assert.That(read.ranked.rating, Is.EqualTo(old.ranked.rating), "the rating stands");

            // A bare version-1 file: one rival named for rung 7, none of the new keys.
            ArenaSaveData bare = ArenaSaveJson.Read(
                "{\"saveVersion\": 1, \"seed\": 3, \"ranked\": {\"rating\": 1040, \"rivals\": [{\"id\": \"ranked-07\", \"name\": \"Kesh\", " +
                "\"rating\": 893, \"build\": {\"mode\": \"ranked\", \"heroId\": \"hero_vanguard\", \"weaponId\": \"weapon_sword\", " +
                "\"policy\": \"careful\", \"lineIds\": [\"damage\"], \"rulesVersion\": 1}}]}}", rules);
            Assert.That(bare.ranked.training, Is.True);
            Assert.That(bare.ranked.rivals.Single().rung, Is.EqualTo(7));
            Assert.That(bare.ranked.rivals.Single().build.handicap, Is.EqualTo(rules.TrainingHandicap(7)));
        }

        [Test]
        public void ARulesChangeRestampsTheTrainingRivals()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaSaveData save = ArenaLadder.NewSave(5, rules);
            ArenaLadder.KeepSnapshot(save, ArenaRivals.Ranked(6, 6, rules));
            ArenaRules softer = rules.WithTrainingHandicaps(Enumerable.Repeat(900, ArenaLadder.Size).ToArray());
            ArenaLadder.Restamp(save, softer);
            foreach (ArenaRivalData rival in save.ranked.rivals)
                Assert.That(rival.build.handicap, Is.EqualTo(rival.pastSelf ? ArenaBuild.NoHandicap : 900));
            Assert.That(save.open.rivals.All(r => r.build.handicap == ArenaBuild.NoHandicap), Is.True);
        }

        [Test]
        public void TheStoreRestampsASaveFoughtUnderOtherRules()
        {
            string directory = Path.Combine(Path.GetTempPath(), "cryptforge-arena-" + Guid.NewGuid().ToString("N"));
            try
            {
                ArenaSaveData save = ArenaLadder.NewSave(12);
                foreach (ArenaRivalData rival in save.ranked.rivals)
                    rival.build.handicap = ArenaBuild.NoHandicap;
                save.rulesVersion = 1;
                new ArenaSaveStore(directory).Save(save);
                ArenaSaveData loaded = new ArenaSaveStore(directory).Load(13);
                Assert.That(loaded.rulesVersion, Is.EqualTo(ArenaRules.Current.Version));
                for (int i = 0; i < loaded.ranked.rivals.Count; i++)
                    Assert.That(loaded.ranked.rivals[i].build.handicap, Is.EqualTo(ArenaRules.Current.TrainingHandicap(i)));
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void TheOpenChanceIsTheFittedTable()
        {
            ArenaRules rules = ArenaRules.Current;
            Assert.That(rules.OpenChance(2500, 2500), Is.EqualTo(500));
            Assert.That(rules.OpenChance(0, 0), Is.EqualTo(500));
            // About two fights in three at a 2 % gap (docs/55 10.6: 67.8 % measured at 1.7 %).
            Assert.That(rules.OpenChance(200, 0), Is.InRange(650, 700));
            for (int theirs = 0; theirs <= 5000; theirs += 250)
            {
                int last = 0;
                for (int mine = Math.Max(0, theirs - 1200); mine <= theirs + 1200; mine += 37)
                {
                    int chance = rules.OpenChance(mine, theirs);
                    Assert.That(chance, Is.GreaterThanOrEqualTo(last), "more gear, more chance");
                    Assert.That(chance + rules.OpenChance(theirs, mine), Is.InRange(985, 1015), "the two sides' chances add up (a ratio gap read from either side differs a little)");
                    last = chance;
                }
            }
        }

        [Test]
        public void TheOpenOffersStandBelowLevelAndAbove()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaSaveData save = ArenaLadder.NewSave(404, rules);
            ArenaHero ilya = rules.Hero(ArenaRules.Ilya);
            foreach (int power in new[] { 0, 150, 1000, 2500, 4044, 5063 })
            {
                ArenaBuild player = ArenaBuild.Open(ilya.Id, ArenaRules.Staff, ilya.SkillIds, ArenaPolicy.Careful, ArenaRivals.GearNear(power), rules.Version);
                List<ArenaOpenOffer> offers = ArenaLadder.OpenOffers(save, player, rules);
                Assert.That(offers.Select(o => o.Kind), Is.EqualTo(new[] { ArenaOfferKind.Below, ArenaOfferKind.Level, ArenaOfferKind.Above }));
                int mine = offers[0].PlayerPower;
                Assert.That(offers.All(o => o.PlayerPower == mine), Is.True);
                Assert.That(Math.Abs(offers[1].RivalPower - mine), Is.LessThanOrEqualTo(50), $"level within half a per cent at {power}");
                Assert.That(offers[0].RivalPower, Is.InRange(Math.Max(0, mine - 250), Math.Max(0, mine - 150)).Or.EqualTo(0), $"below at {power}");
                if (mine + 250 <= 5063)
                    Assert.That(offers[2].RivalPower, Is.InRange(mine + 150, mine + 250), $"above at {power}");
                else
                    Assert.That(offers[2].RivalPower, Is.GreaterThanOrEqualTo(offers[1].RivalPower), "at the ceiling, above meets level");
                foreach (ArenaOpenOffer offer in offers)
                {
                    Assert.That(offer.ExpectedPermille, Is.EqualTo(rules.OpenChance(offer.PlayerPower, offer.RivalPower)), "the card reads the table");
                    Assert.That(offer.Build.HandicapPermille, Is.EqualTo(ArenaBuild.NoHandicap));
                }
                Assert.That(offers[0].ExpectedPermille, Is.GreaterThanOrEqualTo(offers[1].ExpectedPermille));
                Assert.That(offers[1].ExpectedPermille, Is.GreaterThanOrEqualTo(offers[2].ExpectedPermille));
                Assert.That(offers[1].ExpectedPermille, Is.InRange(460, 540), "level: within half a per cent, about even");
                Assert.That(offers.Select(o => o.Build.Hash), Is.EqualTo(ArenaLadder.OpenOffers(save, player, rules).Select(o => o.Build.Hash)),
                    "the same save offers the same three");
            }
        }

        [Test]
        public void FightingAnOfferMovesTheRatingByTheCardsChance()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaSaveData save = ArenaLadder.NewSave(909, rules);
            ArenaHero vanguard = rules.Hero(ArenaRules.Vanguard);
            ArenaBuild player = ArenaBuild.Open(vanguard.Id, ArenaRules.Daggers, vanguard.SkillIds, ArenaPolicy.Bold, ArenaRivals.GearNear(1800), rules.Version);
            for (int fight = 0; fight < 6; fight++)
            {
                List<ArenaOpenOffer> offers = ArenaLadder.OpenOffers(save, player, rules);
                ArenaOpenOffer offer = offers[fight % 3];
                int before = save.open.rating;
                ArenaFightOutcome outcome = ArenaLadder.FightOffer(save, offer, player, rules);
                int score = outcome.Match.Winner == 0 ? 1000 : outcome.Match.Winner == 1 ? 0 : 500;
                int expected = (int)Math.Round(ArenaLadder.K * (score - offer.ExpectedPermille) / 1000.0, MidpointRounding.AwayFromZero);
                Assert.That(save.open.rating - before, Is.EqualTo(expected), "K x (score - the card's chance)");
                ArenaMatchData record = save.open.history.Last();
                Assert.That(record.expected, Is.EqualTo(offer.ExpectedPermille));
                Assert.That(record.rivalPower, Is.EqualTo(offer.RivalPower));
                Assert.That(record.playerPower, Is.EqualTo(offer.PlayerPower));
                DuelMatch again = DuelCore.BestOfThree(ArenaResolver.Resolve(player, rules),
                    ArenaResolver.Resolve(ArenaSnapshots.ToBuild(record.rivalBuild), rules), record.seed);
                Assert.That(ArenaHash.Hex(again.Fingerprint), Is.EqualTo(record.fingerprint), "the record re-runs the offer");
                Assert.That(save.fights, Is.EqualTo(fight + 1));
                Assert.That(ArenaLadder.OpenOffers(save, player, rules)[1].Index, Is.Not.EqualTo(offer.Index), "the next fight offers new rivals");
            }
            string text = ArenaSaveJson.Write(save);
            Assert.That(ArenaSaveJson.Write(ArenaSaveJson.Read(text, rules)), Is.EqualTo(text), "the offers' history round-trips");
        }

        [Test]
        public void TheOfferRatingMatchesTheCardsChance()
        {
            foreach (int expected in new[] { 100, 250, 400, 500, 600, 750, 900 })
            {
                int gap = ArenaLadder.GapFor(expected);
                Assert.That(ArenaLadder.Expected(1000, 1000 + gap), Is.InRange(expected - 3, expected + 3), $"{expected}");
            }
            Assert.That(ArenaLadder.GapFor(500), Is.Zero);
            Assert.That(ArenaLadder.GapFor(700), Is.LessThan(0), "a weaker rival rates below the player");
        }
    }
}
