using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // What the shipped card pool does to a Descent, measured over seeds rather than pinned on one run. Every other
    // balance test names the two-card pool on purpose, so that it keeps measuring relics, weapons and the burst; this
    // one is the only place the five-card pool's own effect is claimed, and it claims a distribution because with a
    // seeded pool a single run is a sample and nothing more.
    public sealed class DescentSeedTests
    {
        private const int Seeds = 24;

        private static DescentSimulation.Floor[] Descent() =>
            new[] { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

        private static (string Name, DescentSimulation.HeroWeapon Weapon)[] Weapons() => new[]
        {
            ("Sword", DescentSimulation.Sword()), ("Staff", DescentSimulation.Staff()),
            ("Daggers", DescentSimulation.Daggers())
        };

        private static DescentSimulation.Result Run(DescentSimulation.HeroWeapon weapon, IHeroRoute route, int seed) =>
            DescentSimulation.Run(Descent(), 0, true, null, weapon, ability: DescentSimulation.ForgeBurst(),
                route: route, seed: seed);

        private static int Cleared(string name, Func<IHeroRoute> route, out int collected)
        {
            int cleared = 0;
            collected = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                DescentSimulation.HeroWeapon weapon = name switch
                {
                    "Sword" => DescentSimulation.Sword(),
                    "Staff" => DescentSimulation.Staff(),
                    _ => DescentSimulation.Daggers()
                };
                DescentSimulation.Result result = Run(weapon, route(), seed);
                collected += result.DropsCollected;
                if (result.ClearedFloors == 2)
                    cleared++;
            }
            return cleared;
        }

        // Measured over these twenty-four seeds: a kiting hero who walks to its drops between fights clears 23, 23 and 24
        // of 24 with the Sword, the Staff and the Daggers (docs/37); before the Descent's rewards lay on the floor kiting
        // cleared 22, 22 and 21, and kiting without fetching them now clears 19, 15 and 20. The claim is loose on purpose -
        // it is here to catch a pool or a tier that breaks the run, not to freeze one measurement.
        [Test]
        public void AKitingHeroWhoFetchesItsDropsClearsMostSeedsWithEveryWeapon()
        {
            foreach ((string name, DescentSimulation.HeroWeapon _) in Weapons())
                Assert.That(Cleared(name, () => new FetchRoute(new KiteRoute()), out _), Is.GreaterThanOrEqualTo(16),
                    $"{name} kiting and fetching");
        }

        // The other half: a hero who stands to fight and fetches between fights clears 12, 9 and 13 of 24 - neither
        // impossible nor certain, which is the cost of a five-card pool at today's run length (docs/22, docs/27 S2b),
        // now paid by the hero drops ask to walk (docs/37). Standing cleared 7, 6 and 10 before drops.
        [Test]
        public void AHeroWhoStandsAndFetchesClearsSomeSeedsButNotAll()
        {
            foreach ((string name, DescentSimulation.HeroWeapon _) in Weapons())
            {
                int cleared = Cleared(name, () => new FetchRoute(), out _);
                Assert.That(cleared, Is.GreaterThan(0), $"{name} standing and fetching: the run is not impossible");
                Assert.That(cleared, Is.LessThan(Seeds), $"{name} standing and fetching: the run is not certain either");
            }
        }

        // And the hero who never walks for them: a drop lies where its enemy fell, just outside the reach every enemy
        // stops at, and DropRule.PickupRadius is shorter than that, so a hero who stands picks up nothing all floor and
        // meets every boss at level zero. That is what the radius was chosen for (docs/37).
        [Test]
        public void AHeroWhoNeverFetchesPicksNothingUpAndClearsNoSeed()
        {
            foreach ((string name, DescentSimulation.HeroWeapon _) in Weapons())
            {
                int cleared = Cleared(name, () => null, out int collected);
                Assert.That(collected, Is.Zero, $"{name} standing picked something up");
                Assert.That(cleared, Is.Zero, $"{name} standing cleared a seed");
            }
        }

        [Test]
        public void TheSameSeedIsTheSameRun()
        {
            DescentSimulation.Result first = Run(DescentSimulation.Sword(), new KiteRoute(), 7);
            DescentSimulation.Result second = Run(DescentSimulation.Sword(), new KiteRoute(), 7);
            Assert.That(second.HeroHealth, Is.EqualTo(first.HeroHealth));
            Assert.That(second.HeroDamageTaken, Is.EqualTo(first.HeroDamageTaken));
            Assert.That(second.FightSeconds, Is.EqualTo(first.FightSeconds));
            Assert.That(second.UpgradesApplied, Is.EqualTo(first.UpgradesApplied));
        }

        [Test]
        public void DifferentSeedsBuildDifferentRuns()
        {
            var healths = new HashSet<float>();
            for (int seed = 0; seed < 8; seed++)
                healths.Add(Run(DescentSimulation.Sword(), new KiteRoute(), seed).HeroHealth);
            Assert.That(healths.Count, Is.GreaterThan(3), "Eight seeds should not all end the same way.");
        }

        // All nine authored cards are in the pool now and UpgradePool.WidthAt decides how many of them a run is
        // offered (docs/27, S2c). On today's Descent that is exactly the five it has always been offered, because the
        // sixth card arrives one level past what a whole Descent reaches. This holds the nine-card pool and a pool of
        // exactly five side by side, seed for seed and weapon by weapon, so the claim is not "close enough" but "the
        // same run". S2b's measurement - that nine cards handed to eleven level-ups collapses the game - is why the
        // rule exists and is recorded in docs/21 and docs/22; what this pins is that the rule costs the Descent
        // nothing.
        [Test]
        public void TodaysDescentIsOfferedTheSameFiveCardsItAlwaysWas()
        {
            foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
            {
                foreach (bool kiting in new[] { false, true })
                {
                    for (int seed = 0; seed < Seeds; seed++)
                    {
                        IHeroRoute Route() => kiting ? new KiteRoute() : null;
                        DescentSimulation.Result nine = DescentSimulation.Run(Descent(), 0, true, null, weapon,
                            ability: DescentSimulation.ForgeBurst(), route: Route(), seed: seed);
                        DescentSimulation.Result five = DescentSimulation.Run(Descent(), 0, true, null, weapon,
                            ability: DescentSimulation.ForgeBurst(), route: Route(), seed: seed,
                            pool: DescentSimulation.FiveCardPool());
                        string where = $"{name}, {(kiting ? "kiting" : "standing")}, seed {seed}";
                        Assert.That(nine.ClearedFloors, Is.EqualTo(five.ClearedFloors), where);
                        Assert.That(nine.HeroHealth, Is.EqualTo(five.HeroHealth), where);
                        Assert.That(nine.Level, Is.EqualTo(five.Level), where);
                        Assert.That(nine.UpgradesApplied, Is.EqualTo(five.UpgradesApplied), where);
                        Assert.That(nine.FightSeconds, Is.EqualTo(five.FightSeconds), where);
                        Assert.That(nine.Level, Is.LessThan(UpgradePool.FirstExtraAtLevel),
                            $"{where}: a Descent that reached the sixth card would make the comparison above vacuous.");
                    }
                }
            }
        }

        // What the width rule actually promises: a run that levels far enough is offered the cards a Descent never
        // sees. Nothing in the game grants that many level-ups yet - a swarm room does, and that is S4 - so this asks
        // the rule directly rather than through a run.
        [Test]
        public void ARunThatLevelsFarEnoughReachesEveryAuthoredCard()
        {
            Assert.That(UpgradePool.WidthAt(0), Is.EqualTo(UpgradePool.BaseWidth));
            Assert.That(UpgradePool.WidthAt(UpgradePool.FirstExtraAtLevel - 1), Is.EqualTo(UpgradePool.BaseWidth),
                "A whole Descent stops one level short of the sixth card.");
            Assert.That(UpgradePool.WidthAt(UpgradePool.FirstExtraAtLevel), Is.EqualTo(UpgradePool.BaseWidth + 1));
            Assert.That(UpgradePool.WidthAt(UpgradePool.FirstExtraAtLevel + UpgradePool.LevelsPerExtraCard),
                Is.EqualTo(UpgradePool.BaseWidth + 2));
            Assert.That(UpgradePool.WidthAt(30), Is.GreaterThanOrEqualTo(9), "All nine are reachable by level 30.");
            for (int level = 0; level < 200; level++)
            {
                Assert.That(UpgradePool.WidthAt(level), Is.GreaterThanOrEqualTo(UpgradePool.WidthAt(Math.Max(0, level - 1))),
                    "The pool never narrows as a run goes on.");
            }
        }
    }
}
