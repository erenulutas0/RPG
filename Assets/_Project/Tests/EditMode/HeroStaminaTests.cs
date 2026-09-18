using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The movement budget of 26_MOVEMENT_CADENCE_EXPERIMENT Part 5: walking spends the bar, standing refills it, and an
    // empty bar slows the hero rather than pinning it. The two claims the experiment rests on are tested here against
    // the simulation itself: a standing hero's fight is untouched, which is why the lever needs no balance re-tune, and
    // a hero that runs the whole fight now pays for it.
    public sealed class HeroStaminaTests
    {
        private static readonly ArenaGeometry Platform = new ArenaGeometry(-9f, 9f, 9f);

        // A bar so long that no fight in the project can empty it: the game as it was before the budget.
        private static HeroStamina Unlimited() => new HeroStamina(100000f, HeroStamina.DefaultRefill, HeroStamina.DefaultEmptySpeed);

        [Test]
        public void WalkingSpendsExactlyTheSecondsItWalksAndStandingPutsThemBackAtTheRefillRate()
        {
            var stamina = new HeroStamina(2f, 1.5f, 0.4f);
            Assert.That(stamina.Current, Is.EqualTo(2f), "A hero starts able to run.");

            stamina.Step(0.5f, true);
            Assert.That(stamina.Current, Is.EqualTo(1.5f).Within(1e-6f));
            stamina.Step(1.5f, true);
            Assert.That(stamina.Current, Is.Zero, "Two seconds of walking is the whole bar.");
            Assert.That(stamina.IsEmpty, Is.True);

            stamina.Step(0.5f, true);
            Assert.That(stamina.Current, Is.Zero, "An empty bar cannot go below empty.");

            stamina.Step(1f, false);
            Assert.That(stamina.Current, Is.EqualTo(1.5f).Within(1e-6f), "Standing pays back at the refill rate.");
            stamina.Step(10f, false);
            Assert.That(stamina.Current, Is.EqualTo(2f), "It never fills past the bar.");
        }

        [Test]
        public void TheHeroKeepsFullSpeedUntilTheBarIsEmptyAndThenWalksAtTheEmptyShare()
        {
            var stamina = new HeroStamina(2f, 1.5f, 0.4f);
            Assert.That(stamina.SpeedFactor, Is.EqualTo(1f));
            stamina.Step(1.999f, true);
            Assert.That(stamina.SpeedFactor, Is.EqualTo(1f), "A bar with anything left is a full-speed bar.");
            stamina.Step(0.002f, true);
            Assert.That(stamina.SpeedFactor, Is.EqualTo(0.4f));
            stamina.Step(0.01f, false);
            Assert.That(stamina.SpeedFactor, Is.EqualTo(1f), "One frame of standing is enough to walk again.");
        }

        // The scene and the simulation both read the factor before walking and spend after: the distance a spent hero
        // covers is exactly its share of the distance a fresh one covers over the same seconds.
        [Test]
        public void AnEmptyBarCarriesTheHeroExactlyItsShareOfTheDistance()
        {
            var fresh = new HeroMotion(2.5f);
            var spent = new HeroMotion(2.5f);
            fresh.Place(0f, 0f, Platform);
            spent.Place(0f, 0f, Platform);
            var stamina = new HeroStamina(2f, 1.5f, 0.4f);
            stamina.Step(2f, true);

            const float step = 1f / 60f;
            for (int frame = 0; frame < 30; frame++)
            {
                fresh.Move(1f, 0f, step, Platform);
                spent.Move(1f, 0f, step * stamina.SpeedFactor, Platform);
            }

            Assert.That(spent.X, Is.EqualTo(fresh.X * 0.4f).Within(1e-5f));
            Assert.That(spent.Y, Is.Zero);
        }

        [Test]
        public void TheBarIsSteppedTheSameWhateverTheFrameLength()
        {
            var once = new HeroStamina(2f, 1.5f, 0.4f);
            var often = new HeroStamina(2f, 1.5f, 0.4f);
            once.Step(0.5f, true);
            for (int i = 0; i < 5; i++)
                often.Step(0.1f, true);
            Assert.That(often.Current, Is.EqualTo(once.Current).Within(1e-6f));

            once.Step(0.5f, false);
            for (int i = 0; i < 5; i++)
                often.Step(0.1f, false);
            Assert.That(often.Current, Is.EqualTo(once.Current).Within(1e-6f));
        }

        [Test]
        public void ChangesAreReportedOnlyWhenTheBarReallyMoves()
        {
            var stamina = new HeroStamina(2f, 1.5f, 0.4f);
            int changes = 0;
            stamina.Changed += () => changes++;

            stamina.Step(0f, true);
            Assert.That(changes, Is.Zero, "A frame of no time spends nothing.");
            stamina.Step(1f, false);
            Assert.That(changes, Is.Zero, "A full bar cannot fill further.");
            stamina.Step(0.5f, true);
            Assert.That(changes, Is.EqualTo(1));
            stamina.Fill();
            Assert.That(changes, Is.EqualTo(2));
            stamina.Fill();
            Assert.That(changes, Is.EqualTo(2), "Filling a full bar is silent.");
        }

        [TestCase(0f, 1.5f, 0.4f)]
        [TestCase(-1f, 1.5f, 0.4f)]
        [TestCase(float.PositiveInfinity, 1.5f, 0.4f)]
        [TestCase(2f, 0f, 0.4f)]
        [TestCase(2f, float.NaN, 0.4f)]
        [TestCase(2f, 1.5f, -0.1f)]
        [TestCase(2f, 1.5f, 1.1f)]
        public void ImpossibleSettingsAreRejected(float bar, float refill, float emptySpeed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeroStamina(bar, refill, emptySpeed));
        }

        [TestCase(-0.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void ImpossibleFramesAreRejected(float deltaTime)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new HeroStamina().Step(deltaTime, true));
        }

        // The claim the whole lever rests on: a hero that stands still never spends any, so every balance number
        // measured from a standing hero is the number it was. Both Descent cards, all three weapons, with the burst.
        [Test]
        public void AStandingHerosDescentIsUntouchedByTheBudget()
        {
            foreach ((string name, DescentSimulation.HeroWeapon weapon) in new[]
            {
                ("Sword", DescentSimulation.Sword()), ("Staff", DescentSimulation.Staff()),
                ("Daggers", DescentSimulation.Daggers())
            })
            {
                for (int card = 0; card < 2; card++)
                {
                    DescentSimulation.Result budgeted = Descent(weapon, card, null, new HeroStamina());
                    DescentSimulation.Result before = Descent(weapon, card, null, Unlimited());
                    string where = $"{name}, card {card}";

                    Assert.That(budgeted.HeroHealth, Is.EqualTo(before.HeroHealth), where);
                    Assert.That(budgeted.HeroDamageTaken, Is.EqualTo(before.HeroDamageTaken), where);
                    Assert.That(budgeted.FightSeconds, Is.EqualTo(before.FightSeconds), where);
                    Assert.That(budgeted.Kills, Is.EqualTo(before.Kills), where);
                    Assert.That(budgeted.ClearedFloors, Is.EqualTo(before.ClearedFloors), where);
                    Assert.That(budgeted.StaminaEmptySeconds, Is.Zero, $"{where}: a standing hero spends nothing.");
                }
            }
        }

        // And the other half: a hero that runs the whole fight now runs out and pays for it. Measured on the trailing
        // proof floor, where the simulation reproduces exactly.
        [Test]
        public void AHeroThatRunsTheWholeFightRunsOutAndPaysForIt()
        {
            var floors = new[] { DescentSimulation.DensityProofTrailing };
            DescentSimulation.Result budgeted = DescentSimulation.Run(floors, 0, false, null, DescentSimulation.Sword(),
                route: new KiteRoute(), stamina: new HeroStamina());
            DescentSimulation.Result before = DescentSimulation.Run(floors, 0, false, null, DescentSimulation.Sword(),
                route: new KiteRoute(), stamina: Unlimited());

            Assert.That(before.StaminaEmptySeconds, Is.Zero);
            Assert.That(budgeted.StaminaEmptySeconds, Is.GreaterThan(5f),
                "A hero that never stops spends most of the fight on an empty bar.");
            Assert.That(budgeted.HeroDamageTaken, Is.GreaterThan(before.HeroDamageTaken * 1.5f),
                "Running the whole fight is no longer close to free.");
            Assert.That(budgeted.ClearedFloors, Is.EqualTo(1), "It is a price, not a death sentence.");
        }

        private static DescentSimulation.Result Descent(DescentSimulation.HeroWeapon weapon, int card, IHeroRoute route,
            HeroStamina stamina) =>
            DescentSimulation.Run(new[] { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults }, card, true,
                null, weapon, ability: DescentSimulation.ForgeBurst(), route: route, stamina: stamina);
    }
}
