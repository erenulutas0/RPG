using System;
using System.Collections.Generic;
using Cryptforge.Arena;
using Cryptforge.UI;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The arena's playback, pure (docs/55 section 11): the time line laid from the log's rounds, the clock at 60 ticks a
    // second and its x2 and skip, and the log played to a moment - every event crossed once and in order however the frames
    // fall, each round's health and positions exactly as DuelReplay reads them, the windows of the guard, ward, glyph and
    // daze, a Shield Rush drawn as a leap. In Unity's EditMode suite and every plain dotnet test of Tools/CombatChecks.
    public sealed class DuelPlaybackTests
    {
        private static readonly ArenaStat[] VanguardLines = { ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.Armour };
        private static readonly ArenaStat[] IlyaLines = { ArenaStat.Damage, ArenaStat.AttackSpeed, ArenaStat.BurnDamage };

        // A bold Vanguard against a bold Ilya, so every skill of both is used; the log kept.
        private static DuelMatch Duel(int seed, out DuelLog log, string weapon = ArenaRules.Sword)
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaFighter a = ArenaResolver.Resolve(ArenaBuild.Ranked(ArenaRules.Vanguard, weapon,
                rules.Hero(ArenaRules.Vanguard).SkillIds, ArenaPolicy.Bold, VanguardLines), rules);
            ArenaFighter b = ArenaResolver.Resolve(ArenaBuild.Ranked(ArenaRules.Ilya, ArenaRules.Staff,
                rules.Hero(ArenaRules.Ilya).SkillIds, ArenaPolicy.Bold, IlyaLines), rules);
            log = new DuelLog();
            return DuelCore.BestOfThree(a, b, seed, log);
        }

        // The first event of the log that matches.
        private static DuelEvent First(DuelLog log, Predicate<DuelEvent> match)
        {
            for (int i = 0; i < log.Count; i++)
            {
                if (match(log[i]))
                    return log[i];
            }
            throw new InvalidOperationException("No such event in the log.");
        }

        private static bool Has(DuelLog log, Predicate<DuelEvent> match)
        {
            for (int i = 0; i < log.Count; i++)
            {
                if (match(log[i]))
                    return true;
            }
            return false;
        }

        [Test]
        public void TheTimeLineLaysTheRoundsEndToEndWithABannerBeforeAndTheEndHeldAfter()
        {
            DuelMatch match = Duel(11, out DuelLog log);
            var timeline = new DuelTimeline(log);
            Assert.That(timeline.RoundCount, Is.EqualTo(match.Rounds.Count));
            int total = 0;
            for (int r = 0; r < match.Rounds.Count; r++)
            {
                Assert.That(timeline.RoundTicks(r), Is.EqualTo(match.Rounds[r].Ticks));
                Assert.That(timeline.RoundWinner(r), Is.EqualTo(match.Rounds[r].Winner));
                Assert.That(timeline.RoundStart(r), Is.EqualTo(total));
                total += DuelTimeline.IntroTicks + match.Rounds[r].Ticks + DuelTimeline.OutroTicks;
            }
            Assert.That(timeline.TotalTicks, Is.EqualTo(total));
        }

        [Test]
        public void AMomentIsItsRoundsBannerItsFightAtTheDuelsTickOrItsEnd()
        {
            DuelMatch match = Duel(11, out DuelLog log);
            var timeline = new DuelTimeline(log);
            int first = match.Rounds[0].Ticks;

            DuelMoment start = timeline.At(0);
            Assert.That((start.Round, start.Phase, start.Tick), Is.EqualTo((0, DuelPhase.Intro, -1.0)));
            DuelMoment fight = timeline.At(DuelTimeline.IntroTicks);
            Assert.That((fight.Round, fight.Phase, fight.Tick), Is.EqualTo((0, DuelPhase.Fight, 0.0)));
            DuelMoment mid = timeline.At(DuelTimeline.IntroTicks + 30.5);
            Assert.That((mid.Phase, mid.Tick), Is.EqualTo((DuelPhase.Fight, 30.5)));
            DuelMoment end = timeline.At(DuelTimeline.IntroTicks + first);
            Assert.That((end.Round, end.Phase, end.Tick), Is.EqualTo((0, DuelPhase.Outro, (double)first)));
            DuelMoment next = timeline.At(timeline.RoundStart(1));
            Assert.That((next.Round, next.Phase), Is.EqualTo((1, DuelPhase.Intro)));
            DuelMoment done = timeline.At(timeline.TotalTicks);
            Assert.That((done.Round, done.Phase), Is.EqualTo((match.Rounds.Count - 1, DuelPhase.Done)));
            Assert.That(done.Tick, Is.EqualTo((double)match.Rounds[match.Rounds.Count - 1].Ticks));
        }

        [Test]
        public void TheClockPlaysSixtyTicksASecondAndTheSpeedButtonDoublesIt()
        {
            var clock = new DuelPlaybackClock(10_000);
            Assert.That(clock.Speed, Is.EqualTo(DuelPlaybackClock.Normal));
            Assert.That(clock.Advance(1.0), Is.EqualTo(60.0).Within(1e-9));
            clock.ToggleSpeed();
            Assert.That(clock.Speed, Is.EqualTo(DuelPlaybackClock.Fast));
            Assert.That(clock.Advance(.5), Is.EqualTo(60.0).Within(1e-9), "x2: half a second is sixty ticks.");
            Assert.That(clock.Ticks, Is.EqualTo(120.0).Within(1e-9));
            clock.ToggleSpeed();
            Assert.That(clock.Speed, Is.EqualTo(DuelPlaybackClock.Normal));
            clock.Advance(1.0);
            Assert.That(clock.Ticks, Is.EqualTo(180.0).Within(1e-9));
            clock.ToggleSpeed();
            Assert.That(clock.AdvanceTicks(30), Is.EqualTo(30.0), "A capture's ticks are the time line's, whatever the speed.");
            clock.ToggleSpeed();
            Assert.That(clock.Ticks, Is.EqualTo(210.0).Within(1e-9));
            Assert.That(clock.Advance(0), Is.EqualTo(0.0));
            Assert.That(clock.Advance(-1), Is.EqualTo(0.0), "Time never runs back.");
            clock.Advance(1e6);
            Assert.That(clock.Ticks, Is.EqualTo(10_000.0));
            Assert.That(clock.Finished, Is.True);
            Assert.That(new DuelPlaybackClock(100, DuelPlaybackClock.Fast).Speed, Is.EqualTo(DuelPlaybackClock.Fast), "The speed chosen carries.");
        }

        [Test]
        public void SkipGoesToTheEndAndWatchAgainToTheStart()
        {
            var clock = new DuelPlaybackClock(5_000);
            clock.Advance(2.0);
            clock.Skip();
            Assert.That((clock.Ticks, clock.Finished, clock.Skipped), Is.EqualTo((5_000.0, true, true)));
            Assert.That(clock.Advance(1.0), Is.EqualTo(0.0));
            clock.Restart();
            Assert.That((clock.Ticks, clock.Finished, clock.Skipped), Is.EqualTo((0.0, false, false)));
        }

        [Test]
        public void EachRoundPlayedToItsEndHasTheReplaysHealthPositionsAndWinner()
        {
            for (int seed = 1; seed <= 6; seed++)
            {
                DuelMatch match = Duel(seed, out DuelLog log, seed % 3 == 0 ? ArenaRules.Daggers : seed % 3 == 1 ? ArenaRules.Sword : ArenaRules.Staff);
                List<DuelReplayRound> replay = DuelReplay.Play(log);
                var state = new DuelPlaybackState(log);
                for (int r = 0; r < replay.Count; r++)
                {
                    state.AdvanceTo(r, state.Timeline.RoundTicks(r));
                    Assert.That(Math.Max(0, state.Side(0).Health), Is.EqualTo(replay[r].HealthA), $"seed {seed} round {r}");
                    Assert.That(Math.Max(0, state.Side(1).Health), Is.EqualTo(replay[r].HealthB), $"seed {seed} round {r}");
                    Assert.That(state.Walked(0), Is.EqualTo((long)replay[r].PositionA), $"seed {seed} round {r}");
                    Assert.That(state.Walked(1), Is.EqualTo((long)replay[r].PositionB), $"seed {seed} round {r}");
                    Assert.That(state.Timeline.RoundWinner(r), Is.EqualTo(match.Rounds[r].Winner));
                }
                Assert.That((state.Side(0).Wins, state.Side(1).Wins), Is.EqualTo((match.WinsA, match.WinsB)));
                Assert.That(state.Played, Is.EqualTo(log.Count));
            }
        }

        [Test]
        public void EveryEventIsCrossedOnceAndInOrderWhateverTheFramesAndTheSpeed()
        {
            Duel(5, out DuelLog log);
            var state = new DuelPlaybackState(log);
            var clock = new DuelPlaybackClock(state.Timeline.TotalTicks);
            var crossed = new List<DuelEvent>();
            float[] frames = { 1f / 60f, 1f / 30f, .1f, 1f / 144f, .05f };
            int frame = 0;
            while (!clock.Finished)
            {
                if (frame % 97 == 50)
                    clock.ToggleSpeed();
                clock.Advance(frames[frame++ % frames.Length]);
                state.AdvanceTo(state.Timeline.At(clock.Ticks), crossed);
            }
            Assert.That(crossed.Count, Is.EqualTo(log.Count));
            ulong hash = ArenaHash.Seed;
            foreach (DuelEvent e in crossed)
                hash = e.MixInto(hash);
            Assert.That(hash, Is.EqualTo(log.Fingerprint()), "The same events in the log's order.");
        }

        [Test]
        public void TheBannerShowsTheRoundsStartAndNothingOfItsFirstTick()
        {
            DuelMatch match = Duel(3, out DuelLog log);
            var state = new DuelPlaybackState(log);
            var crossed = new List<DuelEvent>();
            state.AdvanceTo(state.Timeline.At(1), crossed);
            Assert.That(crossed.Count, Is.EqualTo(2));
            Assert.That(crossed.TrueForAll(e => e.Kind == DuelEventKind.RoundStart), Is.True);
            Assert.That(state.Side(0).Health, Is.EqualTo(match.Rounds[0].MaxHealthA));
            Assert.That(state.Side(1).Health, Is.EqualTo(match.Rounds[0].MaxHealthB));
            Assert.That(state.Position(0), Is.EqualTo(-4.0));
            Assert.That(state.Position(1), Is.EqualTo(4.0));
            Assert.That(state.Side(0).Boon, Is.EqualTo(match.Rounds[0].BoonsA[0]), "The boon dealt is shown from the banner.");
            Assert.That(state.Side(1).Boon, Is.EqualTo(match.Rounds[0].BoonsB[0]));
            Assert.That(state.Facing(0), Is.EqualTo(1));
            Assert.That(state.Facing(1), Is.EqualTo(-1));
        }

        [Test]
        public void GoingBackPlaysTheLogAgainFromTheStart()
        {
            Duel(8, out DuelLog log);
            var state = new DuelPlaybackState(log);
            state.AdvanceToEnd();
            Assert.That(state.Played, Is.EqualTo(log.Count));
            state.AdvanceTo(0, 10);
            int expected = 0;
            for (int i = 0; i < log.Count; i++)
                expected += log[i].Round == 0 && (log[i].Kind == DuelEventKind.RoundStart || log[i].Tick <= 10) ? 1 : 0;
            Assert.That(state.Played, Is.EqualTo(expected));
            Assert.That((state.Side(0).Wins, state.Side(1).Wins), Is.EqualTo((0, 0)));
        }

        // A log written by hand: the two heroes enter, the Vanguard walks, rushes at tick 20 from beyond its reach and lands
        // 1.5 units on (a Move with Aux 1, as DuelCore writes one), and the round ends at tick 60.
        private static DuelLog LeapLog()
        {
            var log = new DuelLog();
            log.Add(new DuelEvent(0, 0, DuelEventKind.RoundStart, 0, ArenaEffect.Weapon, 800_000, -4_000_000, 1));
            log.Add(new DuelEvent(0, 0, DuelEventKind.RoundStart, 1, ArenaEffect.Weapon, 720_000, 4_000_000, 2));
            log.Add(new DuelEvent(0, 0, DuelEventKind.Move, 0, ArenaEffect.Weapon, -3_950_000, 50_000, 0));
            log.Add(new DuelEvent(0, 20, DuelEventKind.Skill, 0, ArenaEffect.ShieldRush, 2, 0, 0));
            log.Add(new DuelEvent(0, 20, DuelEventKind.Hit, 0, ArenaEffect.ShieldRush, 16_800, 0, 0));
            log.Add(new DuelEvent(0, 20, DuelEventKind.Daze, 1, ArenaEffect.ShieldRush, 30, 0, 0));
            log.Add(new DuelEvent(0, 20, DuelEventKind.Move, 0, ArenaEffect.ShieldRush, -1_500_000, 50_000, 1));
            log.Add(new DuelEvent(0, 60, DuelEventKind.RoundEnd, 0, ArenaEffect.Weapon, 800_000, 703_200, 60));
            return log;
        }

        [Test]
        public void ALoggedLeapIsDrawnAsAnArcFromWhereItLeftToWhereItLanded()
        {
            var state = new DuelPlaybackState(LeapLog());
            state.AdvanceTo(0, 20);
            Assert.That(state.Walked(0), Is.EqualTo(-1_500_000L), "The log lands it at once.");
            Assert.That(state.Side(0).LeapTick, Is.EqualTo(20));
            Assert.That(state.Side(0).LeapFrom, Is.EqualTo(-2_950_000L), "It left from where it had walked: -3.95 and 20 steps of 0.05.");
            Assert.That(state.Position(0, out double lift), Is.EqualTo(-2.95).Within(1e-9));
            Assert.That(lift, Is.EqualTo(0.0).Within(1e-9));
            state.AdvanceTo(0, 20 + DuelPlaybackState.LeapTicks / 2.0);
            double x = state.Position(0, out lift);
            Assert.That(lift, Is.EqualTo(1.0).Within(1e-9), "Highest halfway.");
            Assert.That(x, Is.GreaterThan(-2.95).And.LessThan(state.Walked(0) / DuelPlaybackState.MicroPerUnit), "On its way.");
            state.AdvanceTo(0, 20 + DuelPlaybackState.LeapTicks);
            Assert.That(state.Position(0, out lift), Is.EqualTo(state.Walked(0) / DuelPlaybackState.MicroPerUnit).Within(1e-9));
            Assert.That(lift, Is.EqualTo(0.0));
            Assert.That(state.Walked(0), Is.EqualTo(-1_500_000L + 12 * 50_000L), "The step it had walks on after the leap, as DuelReplay reads it.");
            Assert.That(state.Dazed(1), Is.True);
            state.AdvanceTo(0, 60);
            Assert.That(state.Side(1).Health, Is.EqualTo(720_000 - 16_800));
            Assert.That(state.Walked(0), Is.EqualTo((long)DuelReplay.Play(LeapLog())[0].PositionA));
        }

        [Test]
        public void ARushFromWithinReachIsDrawnAsALeapThatLandsWhereItStood()
        {
            Duel(9, out DuelLog log);
            DuelEvent rush = First(log, e => e.Kind == DuelEventKind.Skill && e.Effect == ArenaEffect.ShieldRush);
            var state = new DuelPlaybackState(log);
            state.AdvanceTo(rush.Round, rush.Tick);
            Assert.That(state.Side(rush.Side).LeapTick, Is.EqualTo(rush.Tick));
            state.AdvanceTo(rush.Round, rush.Tick + DuelPlaybackState.LeapTicks / 2.0);
            double x = state.Position(rush.Side, out double lift);
            Assert.That(lift, Is.EqualTo(1.0).Within(1e-9), "A hop at the foe, highest halfway.");
            double from = state.Side(rush.Side).LeapFrom / DuelPlaybackState.MicroPerUnit;
            double to = state.Walked(rush.Side) / DuelPlaybackState.MicroPerUnit;
            Assert.That(x, Is.InRange(Math.Min(from, to) - 1e-9, Math.Max(from, to) + 1e-9));
            Assert.That(Math.Abs(to - from), Is.LessThan(.5), "From within its reach it lands about where it stood.");
        }

        [Test]
        public void TheGuardWardGlyphAndDazeLastAsTheLogSays()
        {
            Duel(4, out DuelLog log);
            var state = new DuelPlaybackState(log);

            DuelEvent guard = First(log, e => e.Kind == DuelEventKind.Skill && e.Effect == ArenaEffect.IronResolve);
            state.AdvanceTo(guard.Round, guard.Tick);
            Assert.That(state.Side(guard.Side).GuardUntil, Is.EqualTo(guard.Tick + guard.Extra));
            Assert.That(state.Guarded(guard.Side), Is.True);
            Assert.That(guard.Extra, Is.EqualTo(ArenaRules.Current.Skill(ArenaRules.IronResolve).DurationTicks));

            state = new DuelPlaybackState(log);
            DuelEvent ward = First(log, e => e.Kind == DuelEventKind.Skill && e.Effect == ArenaEffect.CinderWard);
            state.AdvanceTo(ward.Round, ward.Tick);
            Assert.That(state.Warded(ward.Side), Is.True);
            Assert.That(state.Side(ward.Side).WardUntil, Is.EqualTo(ward.Tick + ward.Extra));

            state = new DuelPlaybackState(log);
            DuelEvent sigil = First(log, e => e.Kind == DuelEventKind.Skill && e.Effect == ArenaEffect.FlameSigil);
            state.AdvanceTo(sigil.Round, sigil.Tick);
            Assert.That(state.Sigil(sigil.Side), Is.True);
            Assert.That(state.Side(sigil.Side).SigilX, Is.EqualTo((long)sigil.Aux), "The glyph lies where the log put it.");
            Assert.That(state.Side(sigil.Side).SigilUntil, Is.EqualTo(sigil.Tick + sigil.Extra));
            if (sigil.Tick + sigil.Extra < state.Timeline.RoundTicks(sigil.Round))
            {
                state.AdvanceTo(sigil.Round, sigil.Tick + sigil.Extra);
                Assert.That(state.Side(sigil.Side).SigilUntil > sigil.Tick + sigil.Extra || !state.Sigil(sigil.Side), Is.True,
                    "The glyph is gone at its end unless a new one was drawn.");
            }

            state = new DuelPlaybackState(log);
            DuelEvent daze = First(log, e => e.Kind == DuelEventKind.Daze);
            state.AdvanceTo(daze.Round, daze.Tick);
            Assert.That(state.Dazed(daze.Side), Is.True);
            Assert.That(state.Side(daze.Side).DazedUntil, Is.GreaterThanOrEqualTo(daze.Tick + daze.Value));
        }

        [Test]
        public void TheBurnsStacksAndABlowsFlashFollowTheLog()
        {
            Duel(6, out DuelLog log);
            var state = new DuelPlaybackState(log);
            DuelEvent stacks = First(log, e => e.Kind == DuelEventKind.Stacks && e.Value > 0);
            state.AdvanceTo(stacks.Round, stacks.Tick);
            Assert.That(state.Side(stacks.Side).BurnStacks, Is.GreaterThan(0));

            state = new DuelPlaybackState(log);
            DuelEvent strike = First(log, e => e.Kind == DuelEventKind.Strike && e.Value > 0);
            state.AdvanceTo(strike.Round, strike.Tick);
            Assert.That(state.Flashing(1 - strike.Side), Is.True, "The struck side flashes.");
            Assert.That(state.Striking(strike.Side), Is.True, "The striker strikes.");
            Assert.That(state.Side(1 - strike.Side).Health, Is.LessThan(state.Side(1 - strike.Side).MaxHealth));
            state.AdvanceTo(strike.Round, strike.Tick + DuelPlaybackState.StrikeTicks + .5);
            Assert.That(state.Side(strike.Side).StrikeTick > strike.Tick || !state.Striking(strike.Side), Is.True);
        }

        [Test]
        public void ABlowsNumberIsItsWholePointsAndNeverZero()
        {
            Assert.That(DuelNumbers.Points(0), Is.EqualTo(0));
            Assert.That(DuelNumbers.Points(-5), Is.EqualTo(0));
            Assert.That(DuelNumbers.Points(1), Is.EqualTo(1));
            Assert.That(DuelNumbers.Points(499), Is.EqualTo(1));
            Assert.That(DuelNumbers.Points(1_499), Is.EqualTo(1));
            Assert.That(DuelNumbers.Points(1_500), Is.EqualTo(2));
            Assert.That(DuelNumbers.Points(18_000), Is.EqualTo(18));
            Assert.That(DuelNumbers.ChancePercent(679), Is.EqualTo(68));
            Assert.That(DuelNumbers.ChancePercent(500), Is.EqualTo(50));
            Assert.That(DuelNumbers.ChancePercent(325), Is.EqualTo(33));
        }
    }
}
