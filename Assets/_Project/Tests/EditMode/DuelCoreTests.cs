using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cryptforge.Arena;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The arena's integer duel (docs/55 P0, Cryptforge.Arena): the replay pin - sequential and parallel runs agree, a
    // log replays to its match, and a fingerprint computed on .NET (Tools/CombatChecks) is the one the Editor's Mono
    // computes - an even mirror, the Daggers' rhythm, the ward read off the hero's own health, and the "why". Also in
    // Tools/CombatChecks.
    public sealed class DuelCoreTests
    {
        // XOR of the fingerprints of PinnedBatch's matches under the shipped rules (v1). A change to the core or the rules
        // moves it: bump ArenaRules.ShippedVersion with the rules' pin (ArenaBuildTests) and write the new value here.
        private const ulong PinnedFingerprint = 0x0E7077FD7DB78B0EUL;

        // Ranked rivals 0-39 of seed 2026 against rivals 40-79, and open rivals at rising gear power, one seed a pair.
        internal static IEnumerable<(ArenaFighter a, ArenaFighter b, int seed)> PinnedBatch()
        {
            ArenaRules rules = ArenaRules.Current;
            for (int i = 0; i < 40; i++)
            {
                yield return (ArenaResolver.Resolve(ArenaRivals.Ranked(2026, i, rules), rules),
                    ArenaResolver.Resolve(ArenaRivals.Ranked(2026, 40 + i, rules), rules), 1000 + i);
            }
            for (int i = 0; i < 10; i++)
            {
                yield return (ArenaResolver.Resolve(ArenaRivals.Open(2026, i, 500 * i, rules), rules),
                    ArenaResolver.Resolve(ArenaRivals.Open(2026, 10 + i, 5000 - 500 * i, rules), rules), 2000 + i);
            }
        }

        internal static ulong PinnedBatchFingerprint()
        {
            ulong x = 0;
            foreach ((ArenaFighter a, ArenaFighter b, int seed) in PinnedBatch())
                x ^= DuelCore.BestOfThree(a, b, seed).Fingerprint;
            return x;
        }

        [Test]
        public void TheFingerprintIsTheSameOnEveryRuntime()
        {
            Assert.That(PinnedBatchFingerprint(), Is.EqualTo(PinnedFingerprint),
                $"0x{PinnedBatchFingerprint():X16}: the duel moved - pin it with a rules version bump");
        }

        [Test]
        public void AMatchReplaysBitForBitInSequenceAndInParallel()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaFighter[] fighters = Enumerable.Range(0, 32).Select(i => ArenaResolver.Resolve(ArenaRivals.Ranked(77, i, rules), rules)).ToArray();
            const int count = 300;
            ulong[] first = Enumerable.Range(0, count).Select(i => DuelCore.BestOfThree(fighters[i % 32], fighters[(i * 5 + 1) % 32], i + 1).Fingerprint).ToArray();
            var second = new ulong[count];
            Parallel.For(0, count, i => second[i] = DuelCore.BestOfThree(fighters[i % 32], fighters[(i * 5 + 1) % 32], i + 1).Fingerprint);
            Assert.That(second, Is.EqualTo(first));
            Assert.That(first.Distinct().Count(), Is.GreaterThan(280), "seeds should give different fights");
        }

        [Test]
        public void ALogReplaysToItsMatch()
        {
            foreach ((ArenaFighter a, ArenaFighter b, int seed) in PinnedBatch())
            {
                var log = new DuelLog();
                DuelMatch match = DuelCore.BestOfThree(a, b, seed, log);
                Assert.That(log.Fingerprint(), Is.EqualTo(match.Fingerprint), "the log is every event the duel made");
                Assert.That(log.Count, Is.EqualTo(match.Events));
                List<DuelReplayRound> rounds = DuelReplay.Play(log);
                Assert.That(rounds.Count, Is.EqualTo(match.Rounds.Count));
                for (int r = 0; r < rounds.Count; r++)
                {
                    DuelRoundResult played = match.Rounds[r];
                    Assert.That(rounds[r].Winner, Is.EqualTo(played.Winner));
                    Assert.That(rounds[r].Ticks, Is.EqualTo(played.Ticks));
                    Assert.That(rounds[r].HealthA, Is.EqualTo(played.HealthA));
                    Assert.That(rounds[r].HealthB, Is.EqualTo(played.HealthB));
                    Assert.That(rounds[r].PositionA, Is.EqualTo(played.PositionA), "the moves replay to where A stood");
                    Assert.That(rounds[r].PositionB, Is.EqualTo(played.PositionB));
                }
            }
        }

        [Test]
        public void AMirrorIsEven()
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaFighter v = Fighter(ArenaRules.Vanguard, ArenaRules.Sword, ArenaPolicy.Careful, ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.CritChance);
            DuelMatch[] matches = Enumerable.Range(1, 400).Select(seed => DuelCore.BestOfThree(v, v, seed)).ToArray();
            Assert.That(matches.Count(m => m.Winner == 0), Is.InRange(160, 240));
            Assert.That(matches.Count(m => m.Winner == 1), Is.InRange(160, 240));
            Assert.That(rules.Version, Is.EqualTo(ArenaRules.ShippedVersion));
        }

        [Test]
        public void TheDaggersCritEveryThirdStrike()
        {
            ArenaFighter d = Fighter(ArenaRules.Vanguard, ArenaRules.Daggers, ArenaPolicy.Careful, ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.Armour);
            ArenaFighter s = Fighter(ArenaRules.Vanguard, ArenaRules.Sword, ArenaPolicy.Careful, ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.Armour);
            var log = new DuelLog();
            DuelCore.BestOfThree(d, s, 11, log);
            foreach (int round in log.Events.Select(e => e.Round).Distinct())
            {
                DuelEvent[] strikes = log.Events.Where(e => e.Round == round && e.Kind == DuelEventKind.Strike && e.Side == 0).ToArray();
                Assert.That(strikes.Length, Is.GreaterThan(6));
                for (int i = 0; i < strikes.Length; i++)
                    Assert.That((strikes[i].Aux & 2) != 0, Is.EqualTo((i + 1) % 3 == 0), $"strike {i + 1} of round {round}");
            }
        }

        [Test]
        public void TheWardReadsTheHerosOwnHealth()
        {
            ArenaFighter ilya = Fighter(ArenaRules.Ilya, ArenaRules.Staff, ArenaPolicy.Bold, ArenaStat.MaxHealth, ArenaStat.Armour, ArenaStat.BurnDamage);
            ArenaFighter v = Fighter(ArenaRules.Vanguard, ArenaRules.Sword, ArenaPolicy.Careful, ArenaStat.Damage, ArenaStat.MaxHealth, ArenaStat.CritChance);
            // The hero's own 90 with its health line (and, in a round that deals it, the Vitality boon), at rank III: 22.4 % of
            // it - about 23 points, never a share of the arena's x8 (about 800).
            int vitality = ArenaRules.Current.Boons.Where(b => b.Stat == ArenaStat.MaxHealth).Sum(b => b.Hundredths);
            int own = 90_000 * (10000 + ilya.Total(ArenaStat.MaxHealth) + vitality) / 10000;
            int ward = own * ArenaSkill.AtRank(200, ArenaRules.Current.SkillRank) / 1000;
            Assert.That(ward, Is.LessThan(ArenaResolver.Resolve(ilya.Build).Hero.Health * DuelRules.HealthScale / 20));
            int wards = 0;
            for (int seed = 1; seed <= 20; seed++)
            {
                var log = new DuelLog();
                DuelCore.BestOfThree(v, ilya, seed, log);
                long absorbed = 0;
                foreach (DuelEvent e in log.Events)
                {
                    if (e.Kind == DuelEventKind.Skill && e.Side == 1 && e.Effect == ArenaEffect.CinderWard)
                    {
                        absorbed = 0;
                        wards++;
                    }
                    else if (e.Side == 0 && (e.Kind == DuelEventKind.Strike || e.Kind == DuelEventKind.Hit || e.Kind == DuelEventKind.Burn))
                    {
                        absorbed += e.Extra;
                        Assert.That(absorbed, Is.LessThanOrEqualTo(ward), "a ward never absorbs more than its share of the hero's own health");
                    }
                }
            }
            Assert.That(wards, Is.GreaterThan(20), "Ilya wards in these fights");
        }

        [Test]
        public void TheWhyNamesTheWinnersEffects()
        {
            int explained = 0;
            foreach ((ArenaFighter a, ArenaFighter b, int seed) in PinnedBatch())
            {
                DuelMatch match = DuelCore.BestOfThree(a, b, seed);
                if (match.Winner < 0)
                {
                    Assert.That(match.Why, Is.Empty, "a draw has no why");
                    continue;
                }
                Assert.That(match.Why.Length, Is.InRange(1, DuelSummary.MaxLines));
                Assert.That(match.Why.All(w => w.Side == match.Winner && w.Amount > 0), Is.True);
                if (match.Why.All(w => w.Net > 0))
                {
                    for (int i = 1; i < match.Why.Length; i++)
                        Assert.That(match.Why[i].Net, Is.LessThanOrEqualTo(match.Why[i - 1].Net), "strongest first");
                }
                explained += match.Why.Length >= 2 ? 1 : 0;
                foreach (DuelWhy why in match.Why)
                    Assert.That(DuelSummary.Line(why, match.Winner == 0 ? a : b), Is.Not.Empty);
            }
            Assert.That(explained, Is.GreaterThan(40), "most matches name two or three effects");
        }

        [Test]
        public void ACreditedSideAddsUpToWhatItDealtAndKept()
        {
            foreach ((ArenaFighter a, ArenaFighter b, int seed) in PinnedBatch().Take(12))
            {
                var log = new DuelLog();
                DuelMatch match = DuelCore.BestOfThree(a, b, seed, log);
                long dealtA = log.Events.Where(e => e.Side == 0 && (e.Kind == DuelEventKind.Strike || e.Kind == DuelEventKind.Hit || e.Kind == DuelEventKind.Burn)).Sum(e => (long)e.Value);
                long offence = new[]
                {
                    ArenaEffect.Weapon, ArenaEffect.Crits, ArenaEffect.ForgeBurst, ArenaEffect.ShieldRush, ArenaEffect.CrucibleVent,
                    ArenaEffect.FlameSigil, ArenaEffect.EmberBurn, ArenaEffect.Damage, ArenaEffect.AttackSpeed, ArenaEffect.Crit,
                    ArenaEffect.BurnDamage
                }.Sum(e => match.CreditsA[(int)e]);
                // The boons' share of the damage is credited apart; rounding loses at most a few points a source.
                long boon = match.CreditsA[(int)ArenaEffect.Boon];
                Assert.That(offence, Is.InRange(dealtA - boon - 200, dealtA), $"seed {seed}");
            }
        }

        internal static ArenaFighter Fighter(string hero, string weapon, ArenaPolicy policy, params ArenaStat[] lines)
        {
            ArenaRules rules = ArenaRules.Current;
            ArenaHero h = rules.Hero(hero);
            return ArenaResolver.Resolve(ArenaBuild.Ranked(h.Id, weapon, h.SkillIds, policy, lines, rules.Version), rules);
        }
    }
}
