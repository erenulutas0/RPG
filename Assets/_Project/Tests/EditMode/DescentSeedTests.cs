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

        private static int Cleared(string name, bool kiting)
        {
            int cleared = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                DescentSimulation.HeroWeapon weapon = name switch
                {
                    "Sword" => DescentSimulation.Sword(),
                    "Staff" => DescentSimulation.Staff(),
                    _ => DescentSimulation.Daggers()
                };
                if (Run(weapon, kiting ? new KiteRoute() : null, seed).ClearedFloors == 2)
                    cleared++;
            }
            return cleared;
        }

        // Measured 2026-09-19 over these twenty-four seeds: kiting clears 22, 22 and 21 of 24 with the Sword, the Staff
        // and the Daggers. The claim is loose on purpose - it is here to catch a pool or a tier that breaks the run,
        // not to freeze one measurement.
        [Test]
        public void AKitingHeroClearsMostSeedsWithEveryWeapon()
        {
            foreach ((string name, DescentSimulation.HeroWeapon _) in Weapons())
                Assert.That(Cleared(name, true), Is.GreaterThanOrEqualTo(16), $"{name} kiting");
        }

        // And the other half of the same measurement: standing cleared every run when the pool held two cards and the
        // damage card was therefore certain. With five, it clears 7, 6 and 10 of 24. That is the cost of variety at
        // today's run length, written down rather than discovered later (docs/22, docs/27 S2b).
        [Test]
        public void AStandingHeroNoLongerClearsEverySeed()
        {
            foreach ((string name, DescentSimulation.HeroWeapon _) in Weapons())
            {
                int cleared = Cleared(name, false);
                Assert.That(cleared, Is.GreaterThan(0), $"{name} standing: the run is not impossible");
                Assert.That(cleared, Is.LessThan(Seeds), $"{name} standing: the run is not certain either");
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

        // The pool is five because nine does not fit in the levels a Descent grants. Measured over the same seeds: with
        // every authored card in the pool a kiting Sword clears well under half its runs, where five clears most.
        [Test]
        public void TheAuthoredPoolIsWiderThanTodaysRunCanCarry()
        {
            int wide = 0;
            int shipped = 0;
            for (int seed = 0; seed < Seeds; seed++)
            {
                if (DescentSimulation.Run(Descent(), 0, true, null, DescentSimulation.Sword(),
                        ability: DescentSimulation.ForgeBurst(), route: new KiteRoute(), seed: seed,
                        pool: DescentSimulation.AuthoredPool()).ClearedFloors == 2)
                    wide++;
                if (Run(DescentSimulation.Sword(), new KiteRoute(), seed).ClearedFloors == 2)
                    shipped++;
            }
            Assert.That(wide, Is.LessThan(shipped),
                "Nine cards spread the same seven level-ups thinner; the pool widens when the run is longer, not before.");
        }
    }
}
