using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The Descent's rewards on the ground (docs/27 S6d, docs/37): a kill drops once where the enemy fell, the hero picks it
    // up by walking within DropRule.PickupRadius, and a floor's end sweeps whatever is left.
    public sealed class GroundDropsTests
    {
        private static readonly DescentSimulation.Floor[] Descent =
            { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

        [Test]
        public void ADropPaysExactlyWhatTheKillWouldHavePaidWhenTheHeroReachesIt()
        {
            var run = new RunState(10);
            var drops = new GroundDrops(run);
            drops.Add(3f, 0f, 12, 5);
            Assert.That(drops.CollectNear(0f, 0f), Is.Zero, "Out of reach, it waits.");
            Assert.That((run.Experience, run.Gold), Is.EqualTo((0, 0)));

            Assert.That(drops.CollectNear(3f - DropRule.PickupRadius, 0f), Is.EqualTo(1), "At the radius, it is picked up.");
            Assert.That(run.Gold, Is.EqualTo(5));
            Assert.That(run.Level, Is.EqualTo(1), "Twelve experience against ten a level.");
            Assert.That(drops.Count, Is.Zero, "And it is gone.");
        }

        [Test]
        public void DropsArePickedUpOldestFirstAndTheRestKeepTheirOrder()
        {
            var run = new RunState(1000);
            var drops = new GroundDrops(run);
            var paid = new List<int>();
            drops.Collecting += drop => paid.Add(drop.Gold);
            drops.Add(0.2f, 0f, 1, 1);
            drops.Add(5f, 0f, 1, 2);
            drops.Add(-0.2f, 0f, 1, 3);
            drops.Add(6f, 0f, 1, 4);

            Assert.That(drops.CollectNear(0f, 0f), Is.EqualTo(2));
            Assert.That(paid, Is.EqualTo(new[] { 1, 3 }));
            Assert.That(drops.At(0).Gold, Is.EqualTo(2));
            Assert.That(drops.At(1).Gold, Is.EqualTo(4));

            Assert.That(drops.CollectAll(), Is.EqualTo(2), "A floor's end sweeps the rest.");
            Assert.That(paid, Is.EqualTo(new[] { 1, 3, 2, 4 }));
            Assert.That(run.Gold, Is.EqualTo(10));
            Assert.That(drops.Count, Is.Zero);
        }

        [Test]
        public void NothingIsPickedUpOnceTheRunHasEnded()
        {
            var run = new RunState(10);
            var drops = new GroundDrops(run);
            drops.Add(0f, 0f, 5, 5);
            run.End(RunOutcome.Defeat);
            Assert.That(drops.CollectNear(0f, 0f), Is.Zero, "What lies on the floor at a death was never the hero's.");
            Assert.That(drops.CollectAll(), Is.Zero);
            Assert.That(run.Gold, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => drops.Add(float.NaN, 0f, 1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => drops.Add(0f, 0f, -1, 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => drops.Add(0f, 0f, 1, -1));
        }

        [Test]
        public void AVictimDropsOnceAndOnlyWhenItHasFallen()
        {
            var run = new RunState(10);
            var rewards = new RewardService(run);
            var drops = new GroundDrops(run);
            var victim = new HealthState(10f);
            Assert.That(rewards.TryDropKill(victim, 4, 2, 1f, 1f, drops), Is.False, "A living enemy drops nothing.");
            victim.ApplyDamage(new DamageContext(10f));
            Assert.That(rewards.TryDropKill(victim, 4, 2, 1f, 1f, drops), Is.True);
            Assert.That(rewards.TryDropKill(victim, 4, 2, 1f, 1f, drops), Is.False, "And it drops once.");
            Assert.That(rewards.TryAwardKill(victim, 4, 2), Is.False, "However it is asked.");
            Assert.That(drops.Count, Is.EqualTo(1));
            Assert.That(run.Gold, Is.Zero, "Dropping pays nothing until it is picked up.");
            Assert.Throws<ArgumentNullException>(() => rewards.TryDropKill(new HealthState(1f), 0, 0, 0f, 0f, null));
        }

        [Test]
        public void AFetchingRouteWalksToTheNearestDropOnlyWhenNothingIsUponIt()
        {
            var view = new RouteView { HeroX = 0f, HeroY = 0f, HeroReach = 1.5f, Platform = new ArenaGeometry(-9f, 9f, 9f) };
            view.DropCount = 2;
            view.DropX[0] = 0f;
            view.DropY[0] = 4f;
            view.DropX[1] = 2f;
            view.DropY[1] = 0f;
            var route = new FetchRoute();

            route.Steer(view, out float steerX, out float steerY);
            Assert.That((steerX, steerY), Is.EqualTo((1f, 0f)), "Straight at the nearer drop.");

            view.Count = 1;
            view.Alive[0] = true;
            view.EnemyX[0] = 0f;
            view.EnemyY[0] = -2.5f;
            route.Steer(view, out steerX, out steerY);
            Assert.That((steerX, steerY), Is.EqualTo((0f, 0f)),
                "An enemy within its reach and the margin: it stands and fights, as the route it plays does.");

            view.EnemyY[0] = -3.5f;
            route.Steer(view, out steerX, out steerY);
            Assert.That((steerX, steerY), Is.EqualTo((1f, 0f)), "Further off, it fetches.");
        }

        [Test]
        public void EveryDescentKillDropsOnceAndEveryDropIsPaidByTheFloorsEnd()
        {
            // Walking or not, a floor that is cleared has paid every kill exactly once: picked up on the way or swept at the
            // end. This is the structural half of docs/37's third criterion.
            var kitingFetcher = new FetchRoute(new KiteRoute());
            foreach (IHeroRoute route in new IHeroRoute[] { kitingFetcher, new FetchRoute(), new KiteRoute() })
            {
                DescentSimulation.Result result = DescentSimulation.Run(Descent, 0, true, null, DescentSimulation.Sword(),
                    ability: DescentSimulation.ForgeBurst(), route: route, seed: 3);
                if (ReferenceEquals(route, kitingFetcher))
                    Assert.That(result.ClearedFloors, Is.EqualTo(2), "A kiting hero who fetches clears this seed.");
                // A run that dies leaves what lies on the floor unpaid; one that clears has paid every kill.
                Assert.That(result.DropsCollected + result.DropsSwept,
                    result.ClearedFloors == 2 ? Is.EqualTo(result.Kills) : Is.LessThanOrEqualTo(result.Kills), route.GetType().Name);
            }

            // And what a cleared floor pays does not depend on the walking: the same kills, the same gold at the checkpoint,
            // less whatever a chest added on the way.
            DescentSimulation.Result fetching = DescentSimulation.Run(new[] { DescentSimulation.EmberHalls }, 0, true, null,
                DescentSimulation.Sword(), ability: DescentSimulation.ForgeBurst(), route: new FetchRoute(), seed: 3,
                pool: DescentSimulation.TwoCardPool());
            DescentSimulation.Result before = DescentSimulation.Run(new[] { DescentSimulation.EmberHalls }, 0, true, null,
                DescentSimulation.Sword(), ability: DescentSimulation.ForgeBurst(), seed: 3, pool: DescentSimulation.TwoCardPool(),
                groundDrops: false);
            Assert.That(fetching.ClearedFloors, Is.EqualTo(1));
            Assert.That(before.ClearedFloors, Is.EqualTo(1));
            Assert.That(fetching.Kills, Is.EqualTo(before.Kills));
            Assert.That(fetching.Gold, Is.GreaterThanOrEqualTo(before.Gold), "Kill gold whole, and chest gold on top.");
        }

        [Test]
        public void TheFinalePaysAKillTheMomentItFalls()
        {
            DescentSimulation.Result swarm = DescentSimulation.Run(new[] { DescentSimulation.SwarmProof }, 0, false, null,
                DescentSimulation.Staff(), seed: 1, swarmSeconds: 40f);
            Assert.That(swarm.Kills, Is.GreaterThan(0));
            Assert.That(swarm.DropsCollected + swarm.DropsSwept, Is.Zero, "Nothing drops in a swarm (docs/27 S6d).");
            Assert.That(swarm.Level, Is.GreaterThan(0), "And its kills paid at once.");
        }
    }
}
