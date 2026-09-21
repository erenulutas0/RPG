using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class DescentTests
    {
        [Test]
        public void GoldAccumulatesAndCheckpointsSecureIt()
        {
            var run = new RunState(10);
            int changes = 0;
            run.GoldChanged += () => changes++;

            run.AddGold(30);
            run.SecureGold();
            run.AddGold(12);
            run.AddGold(0);

            Assert.That(run.Gold, Is.EqualTo(42));
            Assert.That(run.SecuredGold, Is.EqualTo(30));
            Assert.That(run.UnsecuredGold, Is.EqualTo(12));
            Assert.That(changes, Is.EqualTo(3));
            Assert.Throws<ArgumentOutOfRangeException>(() => run.AddGold(-1));
        }

        [TestCase(RunOutcome.Defeat, 20, 45, 5)]
        [TestCase(RunOutcome.Extracted, 20, 50, 0)]
        [TestCase(RunOutcome.Victory, 20, 50, 0)]
        public void EndingSettlesGoldByOutcome(RunOutcome outcome, int unsecured, int banked, int lost)
        {
            var run = new RunState(10, 0.25f);
            run.AddGold(30);
            run.SecureGold();
            run.AddGold(unsecured);

            run.End(outcome);
            run.AddGold(100);

            Assert.That(run.GoldBanked, Is.EqualTo(banked));
            Assert.That(run.GoldLost, Is.EqualTo(lost));
            Assert.That(run.Gold, Is.EqualTo(50), "An ended run earns nothing more.");
        }

        [Test]
        public void DefeatLossRoundsDownAndInvalidLossFractionsAreRejected()
        {
            var run = new RunState(10, 0.5f);
            run.AddGold(7);
            run.End(RunOutcome.Defeat);
            Assert.That(run.GoldLost, Is.EqualTo(3));
            Assert.That(run.GoldBanked, Is.EqualTo(4));

            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(10, -0.1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunState(10, 1.1f));
        }

        [Test]
        public void KillRewardsGoldOnceWithTheExperience()
        {
            var run = new RunState(10);
            var rewards = new RewardService(run);
            var victim = new HealthState(50f);
            victim.ApplyDamage(new DamageContext(50f));

            Assert.That(rewards.TryAwardKill(victim, 10, 8), Is.True);
            Assert.That(rewards.TryAwardKill(victim, 10, 8), Is.False);
            Assert.That(run.Gold, Is.EqualTo(8));
            Assert.That(run.Experience, Is.EqualTo(10));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, 10, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => rewards.TryAwardKill(victim, -1, 0));
        }

        [Test]
        public void FloorScalingAppliesTierThenModifier()
        {
            Assert.That(FloorScaling.Health(50f, 1.4f, 0f), Is.EqualTo(70f).Within(1e-3f));
            Assert.That(FloorScaling.DamageBonus(1.25f, 0.2f), Is.EqualTo(0.5f).Within(1e-5f));
            Assert.That(FloorScaling.DamageBonus(1f, 0f), Is.EqualTo(0f));
            Assert.That(FloorScaling.Gold(5, 0.5f), Is.EqualTo(8), "7.5 rounds away from zero.");
            Assert.That(FloorScaling.Gold(3, 0.5f), Is.EqualTo(5));
            Assert.That(FloorScaling.Gold(50, 0f), Is.EqualTo(50));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.Health(50f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.DamageBonus(1f, -1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => FloorScaling.Gold(-1, 0f));
        }

        [Test]
        public void CheckpointRecordsExactlyOneDecision()
        {
            var run = new RunState(10);
            var checkpoint = new CheckpointService(run);
            var chosen = new List<CheckpointKind>();
            checkpoint.Chosen += chosen.Add;
            Assert.That(checkpoint.Open(Options()), Is.True);
            Assert.That(checkpoint.Open(Options()), Is.False, "One checkpoint at a time.");
            CheckpointOffer offer = checkpoint.CurrentOffer;

            Assert.That(checkpoint.TrySelect(offer, 1), Is.True);
            Assert.That(checkpoint.TrySelect(offer, 0), Is.False);

            Assert.That(chosen, Is.EqualTo(new[] { CheckpointKind.Descend }));
            Assert.That(checkpoint.CurrentOffer, Is.Null);
            Assert.Throws<ArgumentException>(() => checkpoint.Open(new CheckpointOption[0]));
        }

        [Test]
        public void EndingTheRunWithdrawsTheCheckpoint()
        {
            var run = new RunState(10);
            var checkpoint = new CheckpointService(run);
            checkpoint.Open(Options());
            CheckpointOffer offer = checkpoint.CurrentOffer;

            run.End(RunOutcome.Defeat);

            Assert.That(checkpoint.CurrentOffer, Is.Null);
            Assert.That(checkpoint.TrySelect(offer, 0), Is.False);
            Assert.That(checkpoint.Open(Options()), Is.False);
        }

        [Test]
        public void ChoicesResolveTheBossLevelUpBeforeTheCheckpoint()
        {
            var run = new RunState(10);
            var upgrades = new UpgradeService(run, new WeaponRuntime(10f, 0.8f, 3f), new[]
            {
                new UpgradeOption("upgrade_damage", "Tempered Edge", "+{0:0}", UpgradeStat.Damage, new StatModifier(ModifierOperation.Flat, 5f), 5)
            }, 2);
            var forge = new ForgeService(run, new HealthState(100f));
            var checkpoint = new CheckpointService(run);
            var choices = new RunChoices(upgrades, forge, checkpoint);

            run.AddExperience(10);
            checkpoint.Open(Options());
            Assert.That(choices.Current.Kind, Is.EqualTo(ChoiceKind.Upgrade));

            choices.TrySelect(choices.Current, 0);
            Assert.That(choices.Current.Kind, Is.EqualTo(ChoiceKind.Checkpoint));
            Assert.That(choices.Current.Cards[0].Name, Is.EqualTo("Extract"));
            Assert.That(choices.Current.Cards[1].Description, Is.EqualTo("Secure 96 gold"));

            var chosen = new List<CheckpointKind>();
            checkpoint.Chosen += chosen.Add;
            Assert.That(choices.TrySelect(choices.Current, 0), Is.True);
            Assert.That(chosen, Is.EqualTo(new[] { CheckpointKind.Extract }));
            Assert.That(choices.IsOpen, Is.False);
        }

        [Test]
        public void HealthyHeroesSurviveTheDescentAndWoundedOnesDoNot()
        {
            DescentSimulation.Floor[] floors = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };
            DescentSimulation.Result damageMend = Burst(floors, 0, true);
            DescentSimulation.Result speedMend = Burst(floors, 1, true);
            DescentSimulation.Result damageTemper = Burst(floors, 0, false);
            DescentSimulation.Result speedTemper = Burst(floors, 1, false);

            Assert.That(damageMend.ClearedFloors, Is.EqualTo(2), $"Damage first with Mend died in {damageMend.DeathRoom}.");
            Assert.That(speedMend.ClearedFloors, Is.EqualTo(2), $"Speed first with Mend died in {speedMend.DeathRoom}.");
            Assert.That(damageTemper.ClearedFloors, Is.EqualTo(1), "Tempering on floor 1 leaves too little health to descend.");
            Assert.That(speedTemper.ClearedFloors, Is.EqualTo(1));
            Assert.That(speedMend.HeroHealth, Is.InRange(5f, 50f), "Floor 2 must remain a real risk.");
            Assert.That(damageMend.Gold, Is.EqualTo(273), "Floor 1 pays 116 gold and floor 2 pays 157 with Cursed Gold; Cinder Mites pay none.");
            Assert.That(damageTemper.GoldBanked, Is.EqualTo(damageTemper.Gold - damageTemper.UnsecuredAtDeath / 2));
            Assert.That(damageMend.FloorOneUpgrades, Is.InRange(6, 9), "Level-ups are spread over both floors.");
            Assert.That(damageMend.UpgradesApplied, Is.GreaterThan(damageMend.FloorOneUpgrades));
        }

        [Test]
        public void RelicsRescueTheDamageFirstTemperDescentInDifferentWays()
        {
            DescentSimulation.Floor[] floors = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };
            DescentSimulation.Result plainMend = Burst(floors, 0, true);
            DescentSimulation.Result counterMend = Burst(floors, 0, true, DescentSimulation.Counterweight());
            DescentSimulation.Result plainSpeedMend = Burst(floors, 1, true);
            DescentSimulation.Result counterSpeedMend = Burst(floors, 1, true, DescentSimulation.Counterweight());
            DescentSimulation.Result counterTemper = Burst(floors, 0, false, DescentSimulation.Counterweight());
            DescentSimulation.Result windTemper = Burst(floors, 0, false, DescentSimulation.SecondWind());
            DescentSimulation.Result windGreedy = Burst(floors, 1, false, DescentSimulation.SecondWind());

            Assert.That(counterTemper.ClearedFloors, Is.EqualTo(2), "Counterweight carries damage first with Temper through floor 2.");
            Assert.That(windTemper.ClearedFloors, Is.EqualTo(2), "Second Wind carries damage first with Temper through floor 2.");
            Assert.That(windTemper.RelicTriggers, Is.EqualTo(1));
            Assert.That(windGreedy.ClearedFloors, Is.EqualTo(2), "Second Wind, the safety relic, carries speed first with Temper too.");
            Assert.That(counterMend.HeroHealth, Is.GreaterThan(plainMend.HeroHealth + 10f), "Counterweight rewards damage upgrades.");
            Assert.That(counterMend.HeroHealth - plainMend.HeroHealth, Is.GreaterThan(counterSpeedMend.HeroHealth - plainSpeedMend.HeroHealth),
                "Counters strike back with weapon damage, so damage first gains more from Counterweight than speed first.");
            Assert.That(counterMend.FightSeconds, Is.LessThan(plainMend.FightSeconds), "Counters shorten the fights against packs.");
        }

        [Test]
        public void AStandingRunNeverReachesAChestAndAFetchingOneTakesEveryOne()
        {
            DescentSimulation.Floor[] floors = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

            DescentSimulation.Result standing = DescentSimulation.Run(floors, 0, true,
                ability: DescentSimulation.ForgeBurst(), seed: 4, pool: DescentSimulation.TwoCardPool());
            Assert.That(standing.ChestsOpened, Is.Zero,
                "A hero that never leaves the middle never opens a chest, which is why every balance number " +
                "measured before chests existed is untouched by them.");
            Assert.That(standing.BooksTaken, Is.Zero);

            DescentSimulation.Result fetching = DescentSimulation.Run(floors, 0, true,
                ability: DescentSimulation.ForgeBurst(), route: new ChestRoute(), seed: 4,
                pool: DescentSimulation.TwoCardPool());
            Assert.That(fetching.ChestsOpened, Is.GreaterThan(0), "A hero that walks at the chest opens it.");
            Assert.That(fetching.ChestsOpened, Is.LessThanOrEqualTo(12), "Two floors of six rooms hold at most twelve.");
            Assert.That(fetching.BooksTaken, Is.LessThanOrEqualTo(fetching.ChestsOpened));
            Assert.That(fetching.HeroFloorX * fetching.HeroFloorX + fetching.HeroFloorY * fetching.HeroFloorY,
                Is.GreaterThan(0f), "It ended the run away from the centre, where the chest stood.");

            // What a chest holds is the run's, not the walk's: the same seed fetched twice gives the same books.
            DescentSimulation.Result again = DescentSimulation.Run(floors, 0, true,
                ability: DescentSimulation.ForgeBurst(), route: new ChestRoute(), seed: 4,
                pool: DescentSimulation.TwoCardPool());
            Assert.That(again.BooksTaken, Is.EqualTo(fetching.BooksTaken));
            Assert.That(again.ChestsOpened, Is.EqualTo(fetching.ChestsOpened));
            Assert.That(again.HeroHealth, Is.EqualTo(fetching.HeroHealth));

            // A book is a stack the hero found, not one it chose, so it never shows up as an applied upgrade.
            Assert.That(again.UpgradesApplied, Is.EqualTo(fetching.UpgradesApplied));
        }

        [Test]
        public void TheSwarmRoomKeepsGoingUntilTheHeroWalksOutOrDies()
        {
            DescentSimulation.Floor[] swarm = { DescentSimulation.SwarmProof };

            // Standing in a crowd is not a strategy: the swarm is the one room that insists on movement, and it does. A
            // hero that never decides to leave never reaches the exit either, and the room does not end on its own.
            DescentSimulation.Result standing = DescentSimulation.Run(swarm, 0, false, null, DescentSimulation.Sword(),
                ability: DescentSimulation.ForgeBurst(), seed: 3);
            Assert.That(standing.DeathRoom, Is.Not.Null, "A hero that never moves dies in a swarm.");
            Assert.That(standing.SwarmSeconds, Is.GreaterThan(SwarmRule.StepSeconds));
            Assert.That(standing.SwarmSeconds, Is.LessThan(180f), "And it dies early, whatever it is carrying.");

            // Leaving is the player's decision, and the simulation takes it as a policy: stay for T seconds, then walk to
            // the exit through the crowd and hold it. A short stay at the gentle opening gets out - twenty-five seconds,
            // since the opening's three Cinder Mites pay too little for a level in less.
            DescentSimulation.Result short_ = DescentSimulation.Run(swarm, 0, false, null, DescentSimulation.Sword(),
                ability: DescentSimulation.ForgeBurst(), seed: 3, swarmSeconds: 25f);
            Assert.That(short_.DeathRoom, Is.Null, "The opening kills nobody who leaves it early.");
            Assert.That(short_.ClearedFloors, Is.EqualTo(1), "Walking out of a swarm that ends the floor clears the floor.");
            Assert.That(short_.SwarmSeconds, Is.GreaterThanOrEqualTo(25f + ExitRule.DwellSeconds),
                "It left only after deciding to, and only after holding the exit.");
            ExitRule.SpotFor(DescentSimulation.Platform, out float exitX, out float exitY);
            Assert.That(ExitRule.IsWithinReach(short_.HeroFloorX, short_.HeroFloorY, exitX, exitY), Is.True,
                "It left from the exit, standing on it.");
            Assert.That(short_.Level, Is.GreaterThan(0), "And it keeps what the room paid.");

            // Staying longer is where a swarm's experience comes from - and a hero that stays longer meets more of it.
            DescentSimulation.Result longer = DescentSimulation.Run(swarm, 0, false, null, DescentSimulation.Sword(),
                ability: DescentSimulation.ForgeBurst(), seed: 3, swarmSeconds: 45f);
            Assert.That(longer.Kills, Is.GreaterThan(short_.Kills));
            Assert.That(longer.SwarmSeconds, Is.GreaterThan(short_.SwarmSeconds));
        }

        [Test]
        public void ASwarmLevelsAHeroFarPastWhatADescentDoesAndOpensThePoolWithIt()
        {
            // What S2c is for: a Descent reaches level eleven and is offered five cards; a swarm reaches far more and
            // has somewhere to put them. This holds the two side by side rather than asserting a number no one measured.
            DescentSimulation.Result descent = DescentSimulation.Run(
                new[] { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults }, 0, true, null,
                DescentSimulation.Sword(), ability: DescentSimulation.ForgeBurst(), route: new KiteRoute(), seed: 5);
            DescentSimulation.Result swarm = DescentSimulation.Run(new[] { DescentSimulation.SwarmProof }, 0, false, null,
                DescentSimulation.Sword(), ability: DescentSimulation.ForgeBurst(), route: new KiteRoute(), seed: 5);

            Assert.That(descent.Level, Is.LessThan(UpgradePool.FirstExtraAtLevel),
                "A whole Descent still stops short of the sixth card.");
            Assert.That(swarm.Level, Is.GreaterThan(descent.Level),
                "A swarm room is where a run levels, which is the whole reason it exists.");
            Assert.That(UpgradePool.WidthAt(swarm.Level), Is.GreaterThan(UpgradePool.WidthAt(descent.Level)),
                "And the levels it grants are what opens the rest of the pool.");
        }

        [Test]
        public void APulseReachesTheCrowdAndTakesASwarmRunDeeper()
        {
            // The claim docs/27 made for the Forge Pulse was that it would favour the Daggers; docs/30 measured that and
            // it is false - the Pulse is the Sword's card, because it scales with the damage of a strike and the Daggers
            // strike smallest. What is pinned here is only what the mechanism does: a pulse lands, it counts, and a run
            // carrying one goes deeper than the same run without one. Which weapon it suits is a design question the
            // measurement answered and the owner owns.
            //
            // Over eight seeds rather than one: a continuous swarm kills a standing level-zero Sword in well under a minute,
            // and a single run that short can end a frame either side of the other - some seeds die a moment sooner with
            // the pulse than without it (docs/35). The mechanism has to show across a handful.
            DescentSimulation.Floor[] swarm = { DescentSimulation.SwarmProof };
            float secondsWithout = 0f, secondsWith = 0f;
            int killsWithout = 0, killsWith = 0;
            for (int seed = 0; seed < 8; seed++)
            {
                DescentSimulation.HeroWeapon plain = DescentSimulation.Sword();
                DescentSimulation.HeroWeapon pulsing = DescentSimulation.Sword();
                pulsing.PulseShare = 1f;

                DescentSimulation.Result without = DescentSimulation.Run(swarm, 0, false, null, plain,
                    ability: DescentSimulation.ForgeBurst(), seed: seed, swarmSeconds: 300f,
                    pool: DescentSimulation.FiveCardPool());
                DescentSimulation.Result with = DescentSimulation.Run(swarm, 0, false, null, pulsing,
                    ability: DescentSimulation.ForgeBurst(), seed: seed, swarmSeconds: 300f,
                    pool: DescentSimulation.FiveCardPool());
                // A hero that stands still, so the swarm ends both runs well short of the cap and the comparison is a
                // real one rather than two ceilings.
                Assert.That(without.DeathRoom, Is.Not.Null);
                Assert.That(with.DeathRoom, Is.Not.Null);
                Assert.That(without.PulsesFired, Is.Zero, "A weapon with no pulse never pulses.");
                Assert.That(with.PulsesFired, Is.GreaterThan(0), "A weapon with one does.");

                secondsWithout += without.SwarmSeconds;
                secondsWith += with.SwarmSeconds;
                killsWithout += without.Kills;
                killsWith += with.Kills;
            }

            Assert.That(secondsWith, Is.GreaterThan(secondsWithout),
                $"The pulse kept the runs alive longer: {secondsWithout:0} s in all without, {secondsWith:0} s with.");
            Assert.That(killsWith, Is.GreaterThan(killsWithout));
        }

        [Test]
        public void ASplinteringCritGivesTheDaggersTheAreaTheyLack()
        {
            // The Daggers are the only weapon that crits without buying the chance, so they are the only one this card
            // works for out of the box. What is pinned here is the mechanism reaching the fight; which weapon it suits
            // is measured in docs/31.
            DescentSimulation.Floor[] swarm = { DescentSimulation.SwarmProof };
            DescentSimulation.HeroWeapon plain = DescentSimulation.Daggers();
            DescentSimulation.HeroWeapon splintering = DescentSimulation.Daggers();
            splintering.CritSplashShare = 1f;

            DescentSimulation.Result without = DescentSimulation.Run(swarm, 0, false, null, plain,
                ability: DescentSimulation.ForgeBurst(), seed: 6, swarmSeconds: 300f,
                pool: DescentSimulation.FiveCardPool());
            DescentSimulation.Result with = DescentSimulation.Run(swarm, 0, false, null, splintering,
                ability: DescentSimulation.ForgeBurst(), seed: 6, swarmSeconds: 300f,
                pool: DescentSimulation.FiveCardPool());

            Assert.That(without.SplintersFired, Is.Zero);
            Assert.That(with.SplintersFired, Is.GreaterThan(0), "Every third Dagger strike crits, and every crit splinters.");
            Assert.That(with.Kills, Is.GreaterThan(without.Kills),
                $"The splinter cleared more of the crowd: {without.Kills} kills without, {with.Kills} with.");
            Assert.That(with.SwarmWave, Is.GreaterThanOrEqualTo(without.SwarmWave));

            // And the gate really is the crit rather than the pattern: a Sword that has bought a crit chance splinters
            // too, which is why docs/31 holds every weapon at the same chance before comparing them.
            DescentSimulation.HeroWeapon sword = DescentSimulation.Sword();
            sword.CritSplashShare = 1f;
            sword.CritChance = 0.5f;
            DescentSimulation.Result bought = DescentSimulation.Run(swarm, 0, false, null, sword,
                ability: DescentSimulation.ForgeBurst(), seed: 6, swarmSeconds: 300f,
                pool: DescentSimulation.FiveCardPool());
            Assert.That(bought.SplintersFired, Is.GreaterThan(0),
                "A weapon that authors no crit rhythm still splinters once it has a chance to crit.");
        }

        [Test]
        public void EvolutionsFillTheLevelsASwarmWouldOtherwiseWasteAndChargeForThem()
        {
            // The hole docs/32 was written to close: a swarm run fills its build and then levels with nothing to spend
            // the levels on. The Staff is the weapon that gets deep enough for it to show.
            DescentSimulation.Floor[] swarm = { DescentSimulation.SwarmProof };
            var withoutEvolutions = new List<UpgradeOption>();
            foreach (UpgradeOption option in DescentSimulation.Pool())
            {
                if (!option.IsEvolution)
                    withoutEvolutions.Add(option);
            }

            // Entered the way the game will enter it - at the end of the Descent, with the build the Descent gave it - since
            // a hero that walks in at level zero dies long before it fills a card (docs/35). Over six seeds, because an
            // evolved run pays for its evolutions in a harder room and can die sooner than the plain one, and a single
            // pair then compares two runs of different lengths rather than two builds. The kiting hero never means to
            // leave, and never stops on the exit by accident either.
            DescentSimulation.Floor[] finale = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults, swarm[0] };
            int evolutions = 0, wastedWithout = 0, wastedWith = 0;
            for (int seed = 0; seed < 6; seed++)
            {
                DescentSimulation.Result plain = DescentSimulation.Run(finale, 0, true, null, DescentSimulation.Staff(),
                    ability: DescentSimulation.ForgeBurst(), route: new ExitRoute(float.PositiveInfinity, new KiteRoute()),
                    seed: seed, pool: withoutEvolutions);
                DescentSimulation.Result evolved = DescentSimulation.Run(finale, 0, true, null, DescentSimulation.Staff(),
                    ability: DescentSimulation.ForgeBurst(), route: new ExitRoute(float.PositiveInfinity, new KiteRoute()),
                    seed: seed);

                Assert.That(plain.EvolutionsTaken, Is.Zero);
                Assert.That(plain.Difficulty, Is.EqualTo(1f), "Without evolutions the world stays as its floors author it.");
                Assert.That(evolved.Difficulty,
                    Is.EqualTo(1f + evolved.EvolutionsTaken * EvolutionRule.DifficultyPerStack).Within(1e-3f),
                    "Every stack of an evolution is paid for.");
                evolutions += evolved.EvolutionsTaken;
                wastedWithout += plain.Level - plain.UpgradesApplied;
                wastedWith += evolved.Level - evolved.UpgradesApplied;
            }

            Assert.That(evolutions, Is.GreaterThan(0), "Runs filled a card and were offered its evolution.");
            Assert.That(wastedWith, Is.LessThan(wastedWithout),
                $"Evolutions left fewer levels unspent: {wastedWithout} wasted without, {wastedWith} with.");
        }

        [Test]
        public void ARecordedBuildReplaysExactlyFromItsSeedAndItsCards()
        {
            // The instrument docs/33 calibrates the simulation with: offers are drawn from the seed and the stacks, so a run
            // that takes the same card at the same offer sees the same offers, and the build is reproduced exactly. If that
            // ever stopped being true, every replay of the owner's recorded runs would silently measure a different build.
            // The Descent and then the swarm, as the game plays them, so the run has made its choices in both and the
            // swarm's held sittings are part of what has to replay.
            DescentSimulation.Floor[] swarm =
                { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults, DescentSimulation.SwarmProof };
            DescentSimulation.Result original = DescentSimulation.Run(swarm, 1, true, null, DescentSimulation.Staff(),
                route: new KiteRoute(), seed: 42, swarmSeconds: 200f);
            Assert.That(original.Picks.Length, Is.GreaterThan(10), "A run long enough to have made real choices.");
            Assert.That(original.SwarmSeconds, Is.GreaterThan(SwarmRule.OfferEverySeconds * 2f),
                "And long enough in the swarm to have sat down to its cards more than once.");

            // Replayed under a different card policy, which the replay must override card for card.
            DescentSimulation.Result replayed = DescentSimulation.Run(swarm, 0, true, null, DescentSimulation.Staff(),
                route: new KiteRoute(), seed: 42, swarmSeconds: 200f, replay: original.Picks);

            Assert.That(replayed.ReplayMisses, Is.Zero, "Every recorded card was offered when the replay reached it.");
            Assert.That(replayed.Picks, Is.EqualTo(original.Picks));
            Assert.That(replayed.SwarmWave, Is.EqualTo(original.SwarmWave));
            Assert.That(replayed.HeroHealth, Is.EqualTo(original.HeroHealth));
            Assert.That(replayed.Kills, Is.EqualTo(original.Kills));

            // And a replay of cards the offers never contain misses every one, rather than quietly picking something else.
            DescentSimulation.Result wrong = DescentSimulation.Run(swarm, 0, false, null, DescentSimulation.Staff(),
                route: new KiteRoute(), seed: 42, swarmSeconds: 25f, replay: new[] { "no_such_card", "no_such_card" });
            Assert.That(wrong.ReplayMisses, Is.EqualTo(2));
        }

        private static CheckpointOption[] Options() => new[]
        {
            new CheckpointOption(CheckpointKind.Extract, "Extract", "Bank 96 gold"),
            new CheckpointOption(CheckpointKind.Descend, "Descend", "Secure 96 gold")
        };

        // The balance baseline: the Forge Burst fired whenever it is ready with an enemy in reach.
        private static DescentSimulation.Result Burst(DescentSimulation.Floor[] floors, int cardSlot, bool mendOnFloorOne,
            RelicOption relicOption = null, DescentSimulation.HeroWeapon heroWeapon = null) =>
            DescentSimulation.Run(floors, cardSlot, mendOnFloorOne, relicOption, heroWeapon,
                ability: DescentSimulation.ForgeBurst(), pool: DescentSimulation.TwoCardPool());
    }
}