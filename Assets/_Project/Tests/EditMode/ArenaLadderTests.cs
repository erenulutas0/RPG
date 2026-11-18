using System;
using System.IO;
using System.Linq;
using Cryptforge.Analytics;
using Cryptforge.Arena;
using Cryptforge.Save;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // P0's local ladder and the arena's own save (docs/55 P0): thirty rivals a mode, open rivals at rising gear power, the
    // rating zero-sum and in integers, a fight re-run from its record, the player's past heroes standing in the ladder,
    // the save's JSON round trip and version, the file's newest revision, and the arena_fight event's keys. Also in
    // Tools/CombatChecks.
    public sealed class ArenaLadderTests
    {
        [Test]
        public void ANewSaveHasThirtyRivalsInEachArena()
        {
            ArenaSaveData save = ArenaLadder.NewSave(424242);
            Assert.That(save.ranked.rivals.Count, Is.EqualTo(ArenaLadder.Size));
            Assert.That(save.open.rivals.Count, Is.EqualTo(ArenaLadder.Size));
            Assert.That(save.ranked.rivals.Select(r => r.id).Distinct().Count(), Is.EqualTo(ArenaLadder.Size));
            ArenaBuild[] ranked = save.ranked.rivals.Select(r => ArenaSnapshots.ToBuild(r.build)).ToArray();
            Assert.That(ranked.All(b => b.Mode == ArenaMode.Ranked), Is.True);
            Assert.That(ranked.Select(b => b.HeroId).Distinct().Count(), Is.EqualTo(2), "both heroes stand in the ladder");
            Assert.That(ranked.Select(b => b.Hash).Distinct().Count(), Is.GreaterThan(20), "varied builds");
            int[] powers = save.open.rivals.Select(r => ArenaResolver.Resolve(ArenaSnapshots.ToBuild(r.build)).GearPower).ToArray();
            Assert.That(powers[0], Is.Zero, "the open ladder starts ungeared");
            for (int i = 1; i < powers.Length; i++)
                Assert.That(powers[i], Is.GreaterThanOrEqualTo(powers[i - 1]), "gear rises with the rung");
            Assert.That(powers[powers.Length - 1], Is.InRange(4500, ArenaRivals.TopGearPower));
            Assert.That(save.open.rivals.Select(r => r.rating), Is.Ordered);
        }

        [Test]
        public void AFightMovesTheRatingsZeroSumAndReRunsFromItsRecord()
        {
            ArenaSaveData save = ArenaLadder.NewSave(7);
            ArenaBuild player = ArenaRivals.Ranked(99, 0);
            for (int fight = 0; fight < 6; fight++)
            {
                int rival = ArenaLadder.Opponents(save.ranked)[fight % ArenaLadder.Offered];
                int sum = save.ranked.rating + save.ranked.rivals[rival].rating;
                ArenaFightOutcome outcome = ArenaLadder.Fight(save, ArenaMode.Ranked, rival, player);
                Assert.That(save.ranked.rating + save.ranked.rivals[rival].rating, Is.EqualTo(sum), "zero-sum");
                Assert.That(outcome.RatingAfter - outcome.RatingBefore, Is.EqualTo(outcome.Record.ratingDelta));
                Assert.That(save.fights, Is.EqualTo(fight + 1));

                DuelMatch again = DuelCore.BestOfThree(ArenaResolver.Resolve(player),
                    ArenaResolver.Resolve(ArenaSnapshots.ToBuild(outcome.Rival.build)), outcome.Record.seed);
                Assert.That(ArenaHash.Hex(again.Fingerprint), Is.EqualTo(outcome.Record.fingerprint), "the record re-runs the fight");
            }
            Assert.That(save.ranked.wins + save.ranked.losses + save.ranked.draws, Is.EqualTo(6));
            Assert.That(save.ranked.history.Count, Is.EqualTo(6));
            Assert.That(() => ArenaLadder.Fight(save, ArenaMode.Open, 0, player), Throws.ArgumentException, "a ranked build in the open arena");
        }

        [Test]
        public void TheOfferedRivalsStandNearTheRating()
        {
            ArenaSaveData save = ArenaLadder.NewSave(3);
            save.ranked.rating = 1100;
            var offered = ArenaLadder.Opponents(save.ranked);
            Assert.That(offered.Count, Is.EqualTo(ArenaLadder.Offered));
            // The rungs stand about 27.6 points apart: the three nearest are within a rung and a half.
            Assert.That(offered.All(i => Math.Abs(save.ranked.rivals[i].rating - 1100) <= 42), Is.True);
            Assert.That(offered.Select(i => save.ranked.rivals[i].rating), Is.Ordered.Descending);
        }

        [Test]
        public void TheRatingIsEloInIntegers()
        {
            Assert.That(ArenaLadder.Expected(1000, 1000), Is.EqualTo(500));
            Assert.That(ArenaLadder.Expected(1000, 1400), Is.EqualTo(91));
            Assert.That(ArenaLadder.Expected(1400, 1000), Is.EqualTo(909));
            Assert.That(ArenaLadder.Expected(1000, 1012), Is.InRange(480, 486), "interpolated between 464 and 500");
            for (int gap = -900; gap <= 900; gap += 37)
                Assert.That(ArenaLadder.Expected(1000, 1000 + gap) + ArenaLadder.Expected(1000 + gap, 1000), Is.EqualTo(1000));
            Assert.That(ArenaLadder.Delta(1000, 1000, 1000), Is.EqualTo(16));
            Assert.That(ArenaLadder.Delta(1000, 1000, 0), Is.EqualTo(-16));
            Assert.That(ArenaLadder.Delta(1000, 1000, 500), Is.Zero);
        }

        [Test]
        public void PastHeroesJoinTheLadder()
        {
            ArenaSaveData save = ArenaLadder.NewSave(5);
            ArenaBuild first = ArenaRivals.Ranked(1, 1);
            Assert.That(ArenaLadder.KeepSnapshot(save, first), Is.True);
            Assert.That(ArenaLadder.KeepSnapshot(save, first), Is.False, "the same build stands once");
            Assert.That(save.ranked.rivals.Count(r => r.pastSelf), Is.EqualTo(1));
            Assert.That(save.ranked.rivals.Count, Is.EqualTo(ArenaLadder.Size));
            ArenaRivalData self = save.ranked.rivals.Single(r => r.pastSelf);
            Assert.That(self.rating, Is.EqualTo(save.ranked.rating));
            Assert.That(ArenaSnapshots.ToBuild(self.build).Hash, Is.EqualTo(first.Hash));
            for (int i = 2; i < 12; i++)
            {
                save.fights++;
                ArenaLadder.KeepSnapshot(save, ArenaRivals.Ranked(1, i).WithPolicy(ArenaPolicy.Bold).AsRanked(new[] { (ArenaStat)(i % 5) }));
            }
            Assert.That(save.pastSnapshots.Count, Is.LessThanOrEqualTo(ArenaSaveData.MaxPastSnapshots));
            Assert.That(save.ranked.rivals.Count(r => r.pastSelf), Is.LessThanOrEqualTo(ArenaSaveData.MaxPastSnapshots));
            Assert.That(save.ranked.rivals.Count, Is.EqualTo(ArenaLadder.Size));
            int index = save.ranked.rivals.FindIndex(r => r.pastSelf);
            Assert.That(ArenaLadder.Fight(save, ArenaMode.Ranked, index, first).Rival.pastSelf, Is.True, "a past hero can be fought");
        }

        [Test]
        public void TheSaveRoundTripsThroughJson()
        {
            ArenaSaveData save = ArenaLadder.NewSave(31337);
            ArenaBuild player = ArenaRivals.Ranked(2, 2);
            save.rankedBuild = ArenaSnapshots.ToData(player);
            save.openBuild = ArenaSnapshots.ToData(ArenaRivals.Open(2, 2, 1500));
            ArenaLadder.Fight(save, ArenaMode.Ranked, 4, player);
            ArenaLadder.Fight(save, ArenaMode.Open, 9, ArenaRivals.Open(2, 2, 1500));
            ArenaLadder.KeepSnapshot(save, player);
            save.ranked.rivals[0].name = "Quote \" and \\ and \n";

            string text = ArenaSaveJson.Write(save);
            ArenaSaveData back = ArenaSaveJson.Read(text);
            Assert.That(back, Is.Not.Null);
            Assert.That(ArenaSaveJson.Write(back), Is.EqualTo(text), "what is read writes the same text");
            Assert.That(back.ranked.rivals[0].name, Is.EqualTo(save.ranked.rivals[0].name));
            Assert.That(back.ranked.history.Single().fingerprint, Is.EqualTo(save.ranked.history.Single().fingerprint));
            Assert.That(ArenaSnapshots.ToBuild(back.openBuild).Hash, Is.EqualTo(ArenaSnapshots.ToBuild(save.openBuild).Hash));
            Assert.That(text, Does.Contain("\"saveVersion\": 1"));

            Assert.That(ArenaSaveJson.Read("not json"), Is.Null);
            Assert.That(ArenaSaveJson.Read("[1, 2]"), Is.Null);
            Assert.That(ArenaSaveJson.Read(text.Replace("\"saveVersion\": 1", "\"saveVersion\": 99")), Is.Null, "a newer save is refused");
            ArenaSaveData sparse = ArenaSaveJson.Read("{\"saveVersion\": 1, \"seed\": 5, \"ranked\": {\"rating\": 1234, \"extra\": [1.5, null]}}");
            Assert.That(sparse.seed, Is.EqualTo(5));
            Assert.That(sparse.ranked.rating, Is.EqualTo(1234));
            Assert.That(sparse.open.rating, Is.EqualTo(ArenaLadder.StartRating), "missing keys keep their defaults");
        }

        [Test]
        public void TheStoreLoadsTheNewestRevision()
        {
            string directory = Path.Combine(Path.GetTempPath(), "cryptforge-arena-" + Guid.NewGuid().ToString("N"));
            try
            {
                var store = new ArenaSaveStore(directory);
                ArenaSaveData save = store.Load(11);
                Assert.That(store.LastLoadWasNew, Is.True);
                store.Save(save);
                save.ranked.rating = 1111;
                store.Save(save);
                ArenaSaveData loaded = new ArenaSaveStore(directory).Load(12);
                Assert.That(loaded.revision, Is.EqualTo(2));
                Assert.That(loaded.ranked.rating, Is.EqualTo(1111));
                Assert.That(loaded.seed, Is.EqualTo(11), "the first seed stays");

                File.WriteAllText(Path.Combine(directory, ArenaSaveStore.FileName), "{ broken");
                var reader = new ArenaSaveStore(directory);
                ArenaSaveData recovered = reader.Load(13);
                Assert.That(recovered.revision, Is.EqualTo(1), "the backup, one revision older");
                Assert.That(reader.LastLoadProblems, Is.Not.Empty);
            }
            finally
            {
                if (Directory.Exists(directory))
                    Directory.Delete(directory, true);
            }
        }

        [Test]
        public void TheArenaFightEventHasValidKeysInAFixedOrder()
        {
            ArenaSaveData save = ArenaLadder.NewSave(8);
            ArenaFightOutcome outcome = ArenaLadder.Fight(save, ArenaMode.Ranked, 3, ArenaRivals.Ranked(4, 4));
            var fields = ArenaFightTelemetry.Fields(outcome, 0);
            string[] envelope = { "v", "seq", "t", "st", "session", "run", "event" };
            foreach (ArenaTelemetryField field in fields)
            {
                Assert.That(TelemetryFields.IsValidKey(field.Key), Is.True, field.Key);
                Assert.That(envelope, Has.No.Member(field.Key));
            }
            Assert.That(fields.Select(f => f.Key).Distinct().Count(), Is.EqualTo(fields.Count));
            Assert.That(fields.Select(f => f.Key).Take(5), Is.EqualTo(new[] { "mode", "rules_version", "rules_hash", "seed", "fight" }));
            Assert.That(fields.Single(f => f.Key == "result").Text, Is.EqualTo(outcome.Record.result));
            Assert.That(fields.Single(f => f.Key == "fingerprint").Text, Is.EqualTo(outcome.Record.fingerprint));
            Assert.That(ArenaFightTelemetry.EventName, Is.EqualTo("arena_fight"));
            Assert.That(ArenaFightTelemetry.Describe(fields), Does.StartWith("mode=ranked rules_version=1"));
        }
    }
}
