using System.Globalization;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The movement and density proof, run as pure simulation: the stationary baseline must come out of the routed code
    // untouched, a walking hero must stay on the platform and strike while it walks, and the densest packs the formations
    // hold must run the same way twice. What kiting is worth is measured and reported, not asserted: nothing here claims a
    // walking hero survives longer or clears faster than a standing one.
    public sealed class DensityProofTests
    {
        private static readonly DescentSimulation.Floor[] Descent =
            { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };
        private static (string Name, DescentSimulation.HeroWeapon Weapon)[] Weapons() => new[]
        {
            ("Sword", DescentSimulation.Sword()),
            ("Staff", DescentSimulation.Staff()),
            ("Daggers", DescentSimulation.Daggers())
        };

        // The hero now walks through a HeroMotion instead of sitting at the floor origin. With no route, and with the route
        // that never steers, it stands exactly where it stood, so every distance, placement and balance number is the one
        // the existing Descent tests pin. Subtracting 0f is exact, which is why this is equality and not a tolerance.
        [Test]
        public void AStandingRouteLeavesEveryDescentNumberExactlyWhereTheBaselineHadIt()
        {
            foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
            {
                foreach (bool burst in new[] { false, true })
                {
                    DescentSimulation.Result baseline = Run(Descent, weapon, null, burst);
                    DescentSimulation.Result standing = Run(Descent, weapon, new StationaryRoute(), burst);
                    string where = $"{name}{(burst ? " with the burst" : "")}";
                    Assert.That(Describe(standing), Is.EqualTo(Describe(baseline)), where);
                    Assert.That((standing.HeroFloorX, standing.HeroFloorY), Is.EqualTo((0f, 0f)),
                        $"{where}: the hero the balance was measured with never leaves the floor origin.");
                    Assert.That(standing.StrikesWhileMoving, Is.Zero, $"{where}: a standing hero never strikes on the move.");
                    Assert.That(standing.FramesOffPlatform, Is.Zero, where);
                    Assert.That(standing.ClearedFloors, Is.EqualTo(baseline.ClearedFloors), where);
                }
            }
        }

        // A wave enters at least MinimumEntryDistance from wherever the hero stands, which is beyond every enemy's reach and
        // beyond every weapon's, so no pack can spawn already striking or struck however the hero moved before it.
        [Test]
        public void NoWaveCanSpawnInsideAnyonesReach()
        {
            float widestEnemyReach = 0f;
            foreach (DescentSimulation.Enemy enemy in new[]
            {
                DescentSimulation.Grunt, DescentSimulation.Runner, DescentSimulation.Tank,
                DescentSimulation.Captain, DescentSimulation.Warden, DescentSimulation.Mite
            })
            {
                if (enemy.Reach > widestEnemyReach)
                    widestEnemyReach = enemy.Reach;
            }

            Assert.That(widestEnemyReach, Is.EqualTo(1.7f), "The Tank and the Warden reach furthest.");
            Assert.That(EntrySides.MinimumEntryDistance, Is.GreaterThan(widestEnemyReach));
            foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
                Assert.That(EntrySides.MinimumEntryDistance, Is.GreaterThan(weapon.Range), name);
            Assert.That(EntrySides.MinimumEntryDistance, Is.GreaterThan(DescentSimulation.ForgeBurst().Radius), "The burst too.");
        }

        // A scripted walk through the whole Descent: out to the right corner, then short steps round it for the rest of the
        // run. The hero strikes while it walks, never stands outside the rim margin, and no wave ever lands a hit on the
        // frame it spawns. The earliest post-spawn hit is reported rather than pinned, since it depends on the walk.
        [Test]
        public void AScriptedDescentKeepsTheHeroOnThePlatformAndStrikesWhileItWalks()
        {
            foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
            {
                DescentSimulation.Result walked = Run(Descent, weapon, ToTheCornerThenAround(), true);
                Assert.That(walked.FramesOffPlatform, Is.Zero, $"{name}: the hero never stands off the platform margin.");
                Assert.That(walked.StrikesWhileMoving, Is.GreaterThan(0), $"{name}: it strikes while walking.");
                Assert.That(walked.EarliestStrikeAfterSpawn, Is.GreaterThan(0f),
                    $"{name}: no enemy struck on the frame its wave spawned (earliest {walked.EarliestStrikeAfterSpawn} s after a spawn).");
                Assert.That(walked.ClosestEnemyGap, Is.GreaterThan(0f), $"{name}: no two living enemies ever stood on the same spot.");
                Assert.That(walked.Kills, Is.GreaterThan(0), name);

                // The same script twice is the same run: a routed Descent replays.
                Assert.That(Describe(Run(Descent, weapon, ToTheCornerThenAround(), true)), Is.EqualTo(Describe(walked)), name);
            }
        }

        // The proof itself: a wave of ten, standing and kiting, on both proof floors, with each weapon and with and without
        // the burst. Twenty-four runs, each run twice. What is asserted is what the rules guarantee — the run ends, it ends
        // the same way twice, the hero stays on the platform, no wave lands a hit on its spawn frame and no two living
        // enemies ever share a spot. What kiting costs or saves is in the report's table, not in an assertion. That the pack
        // keeps the full body spacing while it walks is asserted by the spacing test below.
        [Test]
        public void EveryDensityProofRunEndsTheSameWayTwiceAndKeepsItsSpawnAndPlatformGuarantees()
        {
            foreach ((string floorName, DescentSimulation.Floor floor) in new[]
            {
                ("ten mites", DescentSimulation.DensityProofMites),
                ("two grunts trailing eight mites", DescentSimulation.DensityProofTrailing)
            })
            {
                DescentSimulation.Floor[] floors = { floor };
                foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
                {
                    foreach (bool burst in new[] { false, true })
                    {
                        foreach (bool kite in new[] { false, true })
                        {
                            string where = $"{floorName}, {name}{(burst ? " with the burst" : "")}, {(kite ? "kiting" : "standing")}";
                            DescentSimulation.Result first = Run(floors, weapon, Route(kite), burst);
                            DescentSimulation.Result second = Run(floors, weapon, Route(kite), burst);
                            Assert.That(Describe(second), Is.EqualTo(Describe(first)), where);
                            Assert.That(first.FightSeconds, Is.LessThan(1000f), $"{where}: the wave ended instead of running out the frame cap.");
                            Assert.That(first.FramesOffPlatform, Is.Zero, $"{where}: the hero never stands off the platform margin.");
                            Assert.That(first.Kills, Is.LessThanOrEqualTo(PackLayout.MaxPackSize), where);
                            Assert.That(first.ClosestEnemyGap, Is.GreaterThan(0f), $"{where}: no two living enemies ever shared a spot.");
                            Assert.That(first.EarliestStrikeAfterSpawn, Is.GreaterThan(0f),
                                $"{where}: the wave of ten landed nothing on the frame it spawned (earliest {first.EarliestStrikeAfterSpawn} s).");
                            if (kite)
                                Assert.That(first.StrikesWhileMoving, Is.GreaterThan(0), $"{where}: it strikes while it walks.");
                            else
                                Assert.That(first.StrikesWhileMoving, Is.Zero, $"{where}: a standing hero never strikes on the move.");
                        }
                    }
                }
            }
        }

        private static IHeroRoute Route(bool kite) => kite ? new KiteRoute() : (IHeroRoute)new StationaryRoute();

        // The spacing while the pack walks, measured the way the movement report reads it, on both ten-enemy proof floors
        // with every weapon: standing, kiting, turning round every second and a half, and running a loop into the rim and its
        // corners. Every run clears; no two living enemies ever stand within a body of each other; no enemy steps back and
        // forth; and a walking hero never leaves an enemy held back for three seconds at a stretch (the one-sided rule held
        // them up to 6.4 s; this rule at most 2.3 s). The flicker check covers kiting too: sliding along only the first
        // neighbour an enemy touches, instead of along all of them, makes the kiting Sword and Daggers flicker here.
        [Test]
        public void OnTheProofFloorsEveryWeaponAndWalkKeepsThePackABodyApartWithoutFlickering()
        {
            foreach (DescentSimulation.Floor floor in new[] { DescentSimulation.DensityProofTrailing, DescentSimulation.DensityProofMites })
            {
                string floorName = floor == DescentSimulation.DensityProofTrailing ? "Grunts then Mites" : "Mites";
                foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
                {
                    foreach ((string walk, IHeroRoute route) in Walks())
                    {
                        DescentSimulation.Result result = Run(new[] { floor }, weapon, route, false);
                        string where = $"{floorName}, {name}, {walk}: {Describe(result)}";
                        TestContext.WriteLine(where);
                        Assert.That(result.ClearedFloors, Is.EqualTo(1), where);
                        Assert.That(result.Kills, Is.EqualTo(10), where);
                        Assert.That(result.ClosestEnemyGapSquared,
                            Is.GreaterThanOrEqualTo(DescentSimulation.BodySpacing * DescentSimulation.BodySpacing), where);
                        Assert.That(result.FramesOffPlatform, Is.Zero, where);
                        Assert.That(result.EnemyStepFlickers, Is.Zero, where);
                        if (route != null)
                            Assert.That(result.LongestEnemyStallSeconds, Is.LessThan(3f), where);
                    }
                }
            }
        }

        // Why the two-sided spacing moved no balance number: on the authored Descent a standing hero's packs never come
        // within two strides of a body of each other (the fastest enemy, the Runner, walks 0.05 a frame), so no step ever
        // ends inside anyone's spacing, nothing is held back or slides, and every enemy walks exactly the straight steps it
        // walked when only nearer enemies could hold it back.
        [Test]
        public void OnTheAuthoredDescentAStandingHerosPacksNeverCrowdSoTheSpacingRuleNeverEngages()
        {
            float fastestStride = DescentSimulation.Runner.Speed / 60f;
            float untouched = DescentSimulation.BodySpacing + 2f * fastestStride;
            foreach ((string name, DescentSimulation.HeroWeapon weapon) in Weapons())
            {
                foreach (bool burst in new[] { false, true })
                {
                    DescentSimulation.Result result = Run(Descent, weapon, null, burst);
                    string where = $"{name}{(burst ? " with the burst" : "")}: {Describe(result)}";
                    Assert.That(result.ClosestEnemyGapSquared, Is.GreaterThanOrEqualTo(untouched * untouched), where);
                    Assert.That(result.EnemyStallSeconds, Is.Zero, where);
                    Assert.That(result.EnemyStepReversals, Is.Zero, where);
                }
            }
        }

        private static (string Name, IHeroRoute Route)[] Walks() => new (string, IHeroRoute)[]
        {
            ("standing", null),
            ("kiting", new KiteRoute()),
            ("turning round every 1.5 s", DescentSimulation.TurningRound()),
            ("looping into the rim", RimLoop())
        };

        // Right, up, left and down for four seconds each, round and round: into the right corner, along the rim and across.
        private static ScriptedRoute RimLoop()
        {
            var segments = new RouteSegment[40];
            for (int i = 0; i < segments.Length; i++)
            {
                int leg = i % 4;
                segments[i] = new RouteSegment((i + 1) * 4f, leg == 0 ? 1f : leg == 2 ? -1f : 0f, leg == 1 ? 1f : leg == 3 ? -1f : 0f);
            }
            return new ScriptedRoute(segments);
        }

        // Six seconds out to the right corner, then a step in each direction in turn with a stand between them, for longer
        // than any Descent lasts. So the hero fights most of the run at the rim, where corners close and the packs come from
        // whichever side the platform reaches.
        private static ScriptedRoute ToTheCornerThenAround()
        {
            var steers = new[] { (X: 1f, Y: 0f), (X: 0f, Y: 1f), (X: -1f, Y: 0f), (X: 0f, Y: -1f) };
            var segments = new RouteSegment[1 + 2 * 240];
            segments[0] = new RouteSegment(6f, 1f, 0f);
            float second = 6f;
            for (int leg = 0; leg < 240; leg++)
            {
                (float x, float y) = steers[leg % steers.Length];
                second += 0.5f;
                segments[1 + leg * 2] = new RouteSegment(second, x, y);
                second += 1.5f;
                segments[2 + leg * 2] = new RouteSegment(second, 0f, 0f);
            }
            return new ScriptedRoute(segments);
        }

        private static DescentSimulation.Result Run(DescentSimulation.Floor[] floors, DescentSimulation.HeroWeapon weapon,
            IHeroRoute route, bool burst) =>
            DescentSimulation.Run(floors, 0, true, null, weapon, ability: burst ? DescentSimulation.ForgeBurst() : null,
                route: route);

        // Every number the movement report shows, in one line and round-trippable, so two runs differ by a character when
        // they differ by a bit and the failure message names the field. The hero's last floor position is part of it: the
        // scene parity cases compare the walk itself to within 1e-3, so a walk that is not reproducible here would leave
        // this suite green and only flake in PlayMode. With no route, or with the standing one, it is exactly (0, 0).
        private static string Describe(DescentSimulation.Result result) => string.Format(CultureInfo.InvariantCulture,
            "cleared {0} died {1} hp {2:R} seconds {3:R} kills {4} gold {5} level {6} upgrades {7} bursts {8} hits {9} " +
            "damage {10:R} gap {11:R} nearest {12:R} off {13} moving {14} struck {15:R} hero {16:R},{17:R} " +
            "stalled {18:R} longest {19:R} turns {20} flickers {21}",
            result.ClearedFloors, result.DeathRoom ?? "-", result.HeroHealth, result.FightSeconds, result.Kills, result.Gold,
            result.Level, result.UpgradesApplied, result.AbilityUses, result.HeroHitsTaken, result.HeroDamageTaken,
            result.ClosestEnemyGapSquared, result.ClosestHeroDistanceSquared, result.FramesOffPlatform,
            result.StrikesWhileMoving, result.EarliestStrikeAfterSpawn, result.HeroFloorX, result.HeroFloorY,
            result.EnemyStallSeconds, result.LongestEnemyStallSeconds, result.EnemyStepReversals, result.EnemyStepFlickers);
    }
}
