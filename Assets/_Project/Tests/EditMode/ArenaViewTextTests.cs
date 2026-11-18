using System;
using System.Collections.Generic;
using Cryptforge.Analytics;
using Cryptforge.Arena;
using Cryptforge.Localisation;
using Cryptforge.UI;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The arena screen's words and its event, pure (docs/55 section 11): the summary's line for every effect is
    // DuelSummary.Line's English word for word and has its own Turkish, with the language's numbers; the boons and the
    // skills have their words; the rung follows the rating; and arena_fight's fields reach the game's telemetry in their
    // order and kinds, outside any run. In Unity's EditMode suite and every plain dotnet test of Tools/CombatChecks.
    public sealed class ArenaViewTextTests
    {
        private Language? _override;
        private LocTable _table;

        [SetUp]
        public void Remember()
        {
            _override = Loc.Override;
            _table = Loc.Table;
        }

        [TearDown]
        public void Restore()
        {
            Loc.UseTable(_table);
            Loc.Override = _override;
        }

        private static void Turkish()
        {
            Loc.UseTable(LocalisationChecks.LoadTable(LocalisationChecks.Root()));
            Loc.Override = Language.Turkish;
        }

        private static ArenaFighter Fighter(ArenaMode mode)
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaBuild build = ArenaBuild.Ranked(ArenaRules.Vanguard, ArenaRules.Sword, rules.Hero(ArenaRules.Vanguard).SkillIds,
                ArenaPolicy.Careful, new[] { ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.Armour });
            if (mode == ArenaMode.Open)
                build = build.AsOpen(new[] { new ArenaGearPiece(0, 3, null) });
            return ArenaResolver.Resolve(build, rules);
        }

        [Test]
        public void EverySummaryLineIsDuelSummarysEnglishWordForWord()
        {
            Loc.Override = Language.English;
            foreach (ArenaMode mode in new[] { ArenaMode.Ranked, ArenaMode.Open })
            {
                ArenaFighter winner = Fighter(mode);
                foreach (ArenaEffect effect in Enum.GetValues(typeof(ArenaEffect)))
                {
                    var why = new DuelWhy(0, effect, 1_502_400, 900_000);
                    string line = ArenaText.Why(why, winner.Weapon.Id, mode == ArenaMode.Open);
                    Assert.That(line, Is.EqualTo(DuelSummary.Line(why, winner)), effect + " " + mode);
                }
            }
        }

        [Test]
        public void EverySummaryLineAndSkillAndBoonHasItsTurkishWithTurkishNumbers()
        {
            ArenaFighter ranked = Fighter(ArenaMode.Ranked);
            var english = new Dictionary<string, string>();
            Loc.Override = Language.English;
            foreach (ArenaEffect effect in Enum.GetValues(typeof(ArenaEffect)))
            {
                english["why " + effect] = ArenaText.Why(new DuelWhy(0, effect, 1_502_400, 1), ranked.Weapon.Id, false);
                english["skill " + effect] = ArenaText.SkillName(effect);
            }
            foreach (ArenaBoon boon in ArenaRules.Current.Boons)
                english["boon " + boon.Id] = ArenaText.Boon(boon);

            Turkish();
            foreach (ArenaEffect effect in Enum.GetValues(typeof(ArenaEffect)))
            {
                string line = ArenaText.Why(new DuelWhy(0, effect, 1_502_400, 1), ranked.Weapon.Id, false);
                Assert.That(line, Is.Not.EqualTo(english["why " + effect]), effect + " has its Turkish.");
                Assert.That(line, Does.Not.Contain("{"), effect.ToString());
                if (line.Contains("1"))
                    Assert.That(line, Does.Contain("1.502"), effect + ": the Turkish thousands dot.");
                string skill = ArenaText.SkillName(effect);
                if (english["skill " + effect].Length > 0)
                    Assert.That(skill, Is.Not.EqualTo(english["skill " + effect]).And.Not.Empty, effect + "'s name in Turkish.");
            }
            Assert.That(ArenaText.Why(new DuelWhy(0, ArenaEffect.Weapon, 1_502_400, 1), ArenaRules.Sword, false),
                Is.EqualTo("Kılıç vuruşları 1.502 hasar verdi"), "The weapon's own Turkish name, no suffix on it.");
            foreach (ArenaBoon boon in ArenaRules.Current.Boons)
            {
                string text = ArenaText.Boon(boon);
                Assert.That(text, Is.Not.EqualTo(english["boon " + boon.Id]).And.Not.Empty, boon.Id);
                Assert.That(text, Does.Not.Contain("{"), boon.Id);
            }
            Assert.That(ArenaText.Boon(ArenaRules.Current.Boons[0]), Is.EqualTo("Güç: +%6 hasar"), "The percent sign before the number.");
            Assert.That(ArenaText.Chance(679), Is.EqualTo("Kazanma şansı %68"));
            Assert.That(ArenaText.Powers(1020, 1220), Is.EqualTo("Senin ekipmanın +%10,2  Onunki +%12,2"));
            Assert.That(ArenaText.RatingChange(ArenaMode.Ranked, 1000, 1016), Is.EqualTo("Antrenman puanı 1.000 -> 1.016 (+16)"));
            Assert.That(ArenaText.RatingChange(ArenaMode.Open, 1205, 1193), Is.EqualTo("Açık arena puanı 1.205 -> 1.193 (-12)"));
        }

        [Test]
        public void TheEnglishCardsReadAsTheCoreSaysThem()
        {
            Loc.Override = Language.English;
            Assert.That(ArenaText.Chance(679), Is.EqualTo("Chance to win 68%"));
            Assert.That(ArenaText.Powers(1020, 1220), Is.EqualTo("Your gear +10.2%  Theirs +12.2%"));
            Assert.That(ArenaText.Offer(ArenaOfferKind.Below, ArenaLadder.OfferGap), Is.EqualTo("2% below you"));
            Assert.That(ArenaText.Offer(ArenaOfferKind.Level, ArenaLadder.OfferGap), Is.EqualTo("Level with you"));
            Assert.That(ArenaText.Offer(ArenaOfferKind.Above, ArenaLadder.OfferGap), Is.EqualTo("2% above you"));
            Assert.That(ArenaText.RatingChange(ArenaMode.Ranked, 1000, 1016), Is.EqualTo("Training rating 1,000 -> 1,016 (+16)"));
            Assert.That(ArenaText.Line(ArenaStat.CritChance, 325), Is.EqualTo("Crit chance +3.25 pts"));
            Assert.That(ArenaText.Line(ArenaStat.Damage, 300), Is.EqualTo("Damage +3%"));
            Assert.That(ArenaText.HeroName(ArenaRules.Ilya), Is.EqualTo("Cinder Arcanist"));
            Assert.That(ArenaText.WeaponName(ArenaRules.Daggers), Is.EqualTo("Daggers"));
            var past = new ArenaRivalData { name = "You, fight 3", pastSelf = true, build = new ArenaSnapshotData { fight = 3 } };
            Assert.That(ArenaText.RivalName(past), Is.EqualTo("You, fight 3"), "A past hero's name, as the core writes it.");
        }

        [Test]
        public void TheRungFollowsTheRatingFromTheBottomToTheTop()
        {
            Assert.That(ArenaText.RungOf(ArenaLadder.LowRating), Is.EqualTo(1));
            Assert.That(ArenaText.RungOf(ArenaLadder.HighRating), Is.EqualTo(ArenaLadder.Size));
            Assert.That(ArenaText.RungOf(ArenaLadder.StartRating), Is.EqualTo(12), "1000 stands near rung 11 of 0-29 (docs/55 10.10).");
            Assert.That(ArenaText.RungOf(400), Is.EqualTo(1));
            Assert.That(ArenaText.RungOf(2400), Is.EqualTo(ArenaLadder.Size));
            int previous = 0;
            for (int rating = 600; rating <= 1600; rating += 10)
            {
                int rung = ArenaText.RungOf(rating);
                Assert.That(rung, Is.GreaterThanOrEqualTo(previous));
                previous = rung;
            }
        }

        [Test]
        public void ArenaFightReachesTheTelemetryInItsOrderAndKindsOutsideAnyRun()
        {
            ArenaSaveData save = ArenaLadder.NewSave(5);
            ArenaBuild player = ArenaBuild.Ranked(ArenaRules.Ilya, ArenaRules.Staff, ArenaRules.Current.Hero(ArenaRules.Ilya).SkillIds,
                ArenaPolicy.Bold, new[] { ArenaStat.Damage, ArenaStat.AttackSpeed, ArenaStat.BurnDamage });
            List<int> opponents = ArenaLadder.Opponents(save.ranked);
            ArenaFightOutcome outcome = ArenaLadder.Fight(save, ArenaMode.Ranked, opponents[0], player);
            List<ArenaTelemetryField> fields = ArenaFightTelemetry.Fields(outcome, outcome.Record.fight);
            TelemetryFields payload = ArenaTelemetryEvent.Of(outcome);
            Assert.That(payload.Count, Is.EqualTo(fields.Count));
            for (int i = 0; i < fields.Count; i++)
                Assert.That(payload.KeyAt(i), Is.EqualTo(fields[i].Key));
            Assert.That(payload.JsonValueAt(payload.IndexOf("mode")), Is.EqualTo("\"ranked\""));
            Assert.That(payload.JsonValueAt(payload.IndexOf("rival_training")), Is.EqualTo("true"));
            Assert.That(payload.JsonValueAt(payload.IndexOf("fight")), Is.EqualTo("0"));
            Assert.That(payload.JsonValueAt(payload.IndexOf("offer_kind")), Is.EqualTo("null"));
            Assert.That(payload.JsonValueAt(payload.IndexOf("hero_id")), Is.EqualTo("\"" + ArenaRules.Ilya + "\""));

            var clock = new TelemetryLogTests.FakeClock();
            var store = new TelemetryLogTests.MemoryStore();
            var session = new TelemetrySession(new TelemetryLog(store, clock), clock, "arena-test");
            session.Start(new TelemetryFields());
            session.BeginRun();
            ArenaTelemetryEvent.Emit(session, outcome);
            session.Flush();
            List<string> lines = store.Lines();
            string line = lines[lines.Count - 1];
            Assert.That(line, Does.Contain("\"event\":\"arena_fight\""));
            Assert.That(line, Does.Not.Contain("\"run\":"), "A fight is no part of a run.");
            Assert.That(line, Does.Contain("\"rival_handicap\":"));
        }
    }
}
