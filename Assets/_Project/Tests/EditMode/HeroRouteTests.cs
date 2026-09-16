using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class HeroRouteTests
    {
        private static readonly ArenaGeometry Arena = new ArenaGeometry(-9f, 9f, 9f);
        // Every enemy in the game reaches 1.5 to 1.7 floor units; the mite's reach stands for the swarm.
        private const float MiteReach = 1.5f;
        // A sweep of a grid in tenths lands some spots exactly on the rim's margin line, where whether the spot counts as on
        // the platform comes down to the last bit of a float and the Editor's runtime and the pure test runner disagree. The
        // sweeps keep this much clearance so they cover the same spots on both; the rim itself has its own tests above.
        private const float RimTieClearance = 0.001f;

        [Test]
        public void TheStandingHeroNeverSteers()
        {
            var route = new StationaryRoute();
            RouteView view = View(2.1f, 0f, 0f, (3f, 0f), (0f, -4f));

            route.Steer(view, out float steerX, out float steerY);
            Assert.That((steerX, steerY), Is.EqualTo((0f, 0f)));

            view.HeroX = 4f;
            view.Alive[1] = false;
            route.Steer(view, out steerX, out steerY);
            Assert.That((steerX, steerY), Is.EqualTo((0f, 0f)), "Nothing the view says moves it.");
            Assert.Throws<ArgumentNullException>(() => route.Steer(null, out _, out _));
        }

        // One threat, dead ahead: the hero backs straight off it at full strength, and the same view always gives the same steer.
        [Test]
        public void TheKiteRouteBacksAwayFromASingleThreatAndRepeatsItself()
        {
            var route = new KiteRoute();
            RouteView view = View(2.1f, 0f, 0f, (0f, 1.8f));

            route.Steer(view, out float steerX, out float steerY);
            Assert.That(steerX, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(steerY, Is.EqualTo(-1f).Within(1e-6f), "Straight away from the enemy ahead.");

            route.Steer(view, out float againX, out float againY);
            Assert.That((againX, againY), Is.EqualTo((steerX, steerY)), "Deterministic: the same view, the same steer.");

            // Just outside its reach plus the margin it is no threat any more, and the hero walks back in at it.
            view.EnemyY[0] = MiteReach + KiteRoute.ThreatMargin + 0.01f;
            route.Steer(view, out steerX, out steerY);
            Assert.That(steerY, Is.EqualTo(1f).Within(1e-6f), "Out of threat and out of reach: walk at it.");
            Assert.Throws<ArgumentNullException>(() => route.Steer(null, out _, out _));
        }

        // Several threats push together: two enemies a quarter turn apart send the hero out between them, and a threat twice
        // as near as another weighs four times as much, so the hero leans away from the nearer one.
        [Test]
        public void TheKiteRouteSumsItsThreatsAndWeighsTheNearerOneMore()
        {
            var route = new KiteRoute();
            RouteView even = View(2.1f, 0f, 0f, (1.5f, 0f), (0f, 1.5f));
            route.Steer(even, out float steerX, out float steerY);
            Assert.That(steerX, Is.EqualTo(steerY).Within(1e-5f), "Two equal threats at a right angle push out between them.");
            Assert.That(steerX, Is.LessThan(0f));
            Assert.That(Length(steerX, steerY), Is.EqualTo(1f).Within(1e-5f));

            RouteView lopsided = View(2.1f, 0f, 0f, (0.5f, 0f), (0f, 1f));
            route.Steer(lopsided, out steerX, out steerY);
            Assert.That(-steerX, Is.GreaterThan(-steerY), "The nearer threat decides more of the flight.");
            // Weighted by 1 / distance^2 over a vector of that distance, each threat pushes with 1 / distance.
            Assert.That(-steerX / -steerY, Is.EqualTo(2f).Within(1e-4f), "Half the distance, twice the push.");

            // A dead enemy inside the threat band is no threat, and the hero treats the wave as empty when none is left.
            RouteView fallen = View(2.1f, 0f, 0f, (0f, 1f));
            fallen.Alive[0] = false;
            route.Steer(fallen, out steerX, out steerY);
            Assert.That((steerX, steerY), Is.EqualTo((0f, 0f)), "Nothing alive, nowhere to go.");
        }

        // Threats that cancel out exactly: the hero turns a quarter turn clockwise off the nearest of them instead of
        // standing still between them, always the same way, and never returns NaN.
        [Test]
        public void ThreatsThatCancelOutTurnTheHeroAQuarterTurnClockwise()
        {
            var route = new KiteRoute();
            RouteView view = View(2.1f, 0f, 0f, (1f, 0f), (-1f, 0f));

            route.Steer(view, out float steerX, out float steerY);
            Assert.That(Length(steerX, steerY), Is.EqualTo(1f).Within(1e-5f));
            // The nearest threat is slot 0, to the hero's right; a quarter turn clockwise off it points toward -y.
            Assert.That(steerX, Is.EqualTo(0f).Within(1e-6f));
            Assert.That(steerY, Is.EqualTo(-1f).Within(1e-6f));

            // A threat standing on the hero has no direction at all; the steer is still a steer.
            RouteView onTop = View(2.1f, 0f, 0f, (0f, 0f));
            route.Steer(onTop, out steerX, out steerY);
            Assert.That((steerX, steerY), Is.EqualTo((1f, 0f)));
        }

        // The band the hero wants: the enemy inside its reach and outside the enemy's own plus the threat margin. Only a reach
        // wider than the enemy's by more than ThreatMargin + ApproachMargin has one, which is why the Staff can stand and
        // strike a mite while the Sword and the Daggers cannot.
        [Test]
        public void TheKiteRouteHoldsStillInsideTheStrikeBandAndWalksInOutsideIt()
        {
            var route = new KiteRoute();
            float staff = DescentSimulation.Staff().Range;
            float inner = MiteReach + KiteRoute.ThreatMargin;
            float outer = staff - KiteRoute.ApproachMargin;
            Assert.That(outer, Is.GreaterThan(inner), "The Staff's band is real: 2.1 to 2.2 floor units from a mite.");

            RouteView view = View(staff, 0f, 0f, (0f, (inner + outer) / 2f));
            route.Steer(view, out float steerX, out float steerY);
            Assert.That((steerX, steerY), Is.EqualTo((0f, 0f)), "Inside the band the hero stands and strikes.");

            view.EnemyY[0] = outer + 0.05f;
            route.Steer(view, out steerX, out steerY);
            Assert.That(steerY, Is.EqualTo(1f).Within(1e-6f), "Past the band's far edge it walks in.");

            view.EnemyY[0] = inner - 0.05f;
            route.Steer(view, out steerX, out steerY);
            Assert.That(steerY, Is.EqualTo(-1f).Within(1e-6f), "Inside the band's near edge it backs off.");

            // The Sword's reach is only 0.6 beyond a mite's, so its band is empty and the two rules meet without a gap.
            float sword = DescentSimulation.Sword().Range;
            Assert.That(sword - KiteRoute.ApproachMargin, Is.LessThan(inner), "The Sword has no standing band against a mite.");
            RouteView swordView = View(sword, 0f, 0f, (0f, sword - KiteRoute.ApproachMargin - 0.01f));
            route.Steer(swordView, out steerX, out steerY);
            Assert.That(steerY, Is.LessThan(0f), "With the Sword the hero is already backing off where it would stand with the Staff.");
        }

        // Cornered, the hero would flee straight into the void and pin itself against the rim. Inside RimAvoidance a pull of
        // weight RimPull toward the platform's centre goes into the flight first, and since a threat pushes with 1 / distance,
        // the pull wins over anything farther than one floor unit: the hero breaks past the pack instead of backing off it.
        [Test]
        public void NearTheRimTheKiteRoutePullsTheHeroBackTowardTheCentre()
        {
            var route = new KiteRoute();
            float rim = 9f - HeroMotion.EdgeMargin;
            Assert.That(Arena.IsOnPlatform(rim, 0f, HeroMotion.EdgeMargin + KiteRoute.RimAvoidance), Is.False,
                "At the corner the hero is inside the rim avoidance.");

            RouteView cornered = View(2.1f, rim, 0f, (rim - 1.8f, 0f));
            route.Steer(cornered, out float steerX, out float steerY);
            Assert.That(Length(steerX, steerY), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(steerX, Is.EqualTo(-1f).Within(1e-5f), "Back toward the centre, past the threat, rather than into the corner.");

            // A threat inside one unit pushes harder than the pull, so the hero still backs off it even at the corner.
            cornered.EnemyX[0] = rim - 0.5f;
            route.Steer(cornered, out steerX, out steerY);
            Assert.That(steerX, Is.EqualTo(1f).Within(1e-5f), "Half a unit away the flight wins.");

            // On the far-right edge the pull turns the flight inward, so the hero escapes along the rim instead of over it.
            RouteView alongRim = View(2.1f, 4.2f, 4.2f, (4.2f - 1.4f, 4.2f + 1.4f));
            route.Steer(alongRim, out steerX, out steerY);
            Assert.That(Length(steerX, steerY), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(steerX, Is.LessThan(0f), "The pull turned the flight away from the edge it was heading for.");
            Assert.That(steerY, Is.LessThan(0f), "It keeps going down the edge, away from the threat above it.");

            // Well inside the platform no pull is added at all, so the flight is the plain one.
            RouteView inside = View(2.1f, 0f, 0f, (1.8f, 0f));
            route.Steer(inside, out steerX, out steerY);
            Assert.That(steerX, Is.EqualTo(-1f).Within(1e-6f));
            Assert.That(steerY, Is.EqualTo(0f).Within(1e-6f));
        }

        // Every spot the hero can stand on, against every pack the arena can field, gives a steer that HeroMotion accepts:
        // finite, at most one long, and one that never walks the hero off the platform.
        [Test]
        public void TheKiteRouteAlwaysReturnsAFiniteSteerNoLongerThanOne()
        {
            var route = new KiteRoute();
            var view = new RouteView { Platform = Arena, HeroReach = 2.4f };
            var hero = new HeroMotion(DescentSimulation.HeroSpeed);
            int steers = 0;
            for (int tenthsX = -84; tenthsX <= 84; tenthsX += 6)
            {
                for (int tenthsY = -84; tenthsY <= 84; tenthsY += 6)
                {
                    float heroX = tenthsX / 10f;
                    float heroY = tenthsY / 10f;
                    if (!Arena.IsOnPlatform(heroX, heroY, HeroMotion.EdgeMargin + RimTieClearance))
                        continue;

                    view.HeroX = heroX;
                    view.HeroY = heroY;
                    for (int count = 1; count <= PackLayout.MaxPackSize; count++)
                    {
                        view.Count = count;
                        var placements = new EntryPlacement[count];
                        EntrySides.Place(0, count, heroX, heroY, Arena, DescentSimulation.EntryDepth,
                            DescentSimulation.FormationSpacing, DescentSimulation.BodySpacing, placements);
                        for (int i = 0; i < count; i++)
                        {
                            // Half the pack where it entered, half already standing on the hero's feet: both are steered from.
                            view.EnemyX[i] = i % 2 == 0 ? placements[i].X : heroX;
                            view.EnemyY[i] = i % 2 == 0 ? placements[i].Y : heroY;
                            view.EnemyReach[i] = i % 2 == 0 ? MiteReach : 1.7f;
                            view.Alive[i] = true;
                        }

                        route.Steer(view, out float steerX, out float steerY);
                        string where = $"Hero ({heroX}, {heroY}), {count} enemies";
                        Assert.That(float.IsNaN(steerX) || float.IsNaN(steerY), Is.False, where);
                        Assert.That(float.IsInfinity(steerX) || float.IsInfinity(steerY), Is.False, where);
                        Assert.That(Length(steerX, steerY), Is.LessThanOrEqualTo(1f + 1e-6f), where);
                        hero.Place(heroX, heroY, Arena);
                        hero.Move(steerX, steerY, 1f / 60f, Arena);
                        Assert.That(Arena.IsOnPlatform(hero.X, hero.Y, HeroMotion.EdgeMargin), Is.True, where + " stays on the platform.");
                        steers++;
                    }
                }
            }
            Assert.That(steers, Is.EqualTo(3650), "365 spots on the 0.6-unit grid, against every pack size.");
        }

        // A script holds each steer until the fight clock passes its second, then the next, then stands still.
        [Test]
        public void AScriptedRouteWalksItsSegmentsInOrderAndStopsAfterTheLastOne()
        {
            var route = new ScriptedRoute(
                new RouteSegment(2f, 1f, 0f),
                new RouteSegment(5f, 0f, -1f),
                new RouteSegment(6f, -0.5f, 0f));
            RouteView view = View(2.1f, 0f, 0f, (0f, 6f));
            Assert.That(route.SegmentCount, Is.EqualTo(3));
            Assert.That(route.Seconds, Is.EqualTo(6f));

            AssertSteer(route, view, 0f, 1f, 0f);
            AssertSteer(route, view, 1.999f, 1f, 0f);
            AssertSteer(route, view, 2f, 0f, -1f, "A segment ends exactly at its second.");
            AssertSteer(route, view, 4.9f, 0f, -1f);
            AssertSteer(route, view, 5f, -0.5f, 0f);
            AssertSteer(route, view, 6f, 0f, 0f, "After the last segment the hero stands still.");
            AssertSteer(route, view, 1000f, 0f, 0f);

            // A single segment, and one that ends where the previous one did and so never runs.
            var single = new ScriptedRoute(new RouteSegment(1f, 0f, 1f));
            AssertSteer(single, view, 0.5f, 0f, 1f);
            var skipped = new ScriptedRoute(new RouteSegment(1f, 1f, 0f), new RouteSegment(1f, 0f, 1f), new RouteSegment(2f, -1f, 0f));
            AssertSteer(skipped, view, 1f, -1f, 0f, "The second segment ends where the first did, so it never runs.");
        }

        [Test]
        public void RouteSegmentsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSegment(-1f, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSegment(float.NaN, 0f, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSegment(1f, float.NaN, 0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSegment(1f, 0f, float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RouteSegment(1f, 1f, 1f), "A steer is at most one long.");
            Assert.DoesNotThrow(() => new RouteSegment(1f, 0.6f, 0.8f));
            Assert.Throws<ArgumentException>(() => new ScriptedRoute());
            Assert.Throws<ArgumentException>(() => new ScriptedRoute(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ScriptedRoute(new RouteSegment(2f, 0f, 0f), new RouteSegment(1f, 0f, 0f)));

            // The script keeps its own copy, so rewriting the array afterwards cannot change a run in progress.
            var segments = new[] { new RouteSegment(1f, 1f, 0f) };
            var route = new ScriptedRoute(segments);
            segments[0] = new RouteSegment(1f, -1f, 0f);
            RouteView view = View(2.1f, 0f, 0f, (0f, 6f));
            AssertSteer(route, view, 0.5f, 1f, 0f);
            Assert.Throws<ArgumentNullException>(() => route.Steer(null, out _, out _));
        }

        private static void AssertSteer(IHeroRoute route, RouteView view, float seconds, float expectedX, float expectedY,
            string because = null)
        {
            view.Seconds = seconds;
            route.Steer(view, out float steerX, out float steerY);
            Assert.That((steerX, steerY), Is.EqualTo((expectedX, expectedY)), because ?? $"At {seconds} s.");
        }

        // A view of the arena with the hero somewhere on it and the given enemy spots alive, every enemy a mite.
        private static RouteView View(float heroReach, float heroX, float heroY, params (float X, float Y)[] enemies)
        {
            var view = new RouteView
            {
                HeroX = heroX,
                HeroY = heroY,
                HeroReach = heroReach,
                Platform = Arena,
                Count = enemies.Length
            };
            for (int i = 0; i < enemies.Length; i++)
            {
                view.EnemyX[i] = enemies[i].X;
                view.EnemyY[i] = enemies[i].Y;
                view.EnemyReach[i] = MiteReach;
                view.Alive[i] = true;
            }
            return view;
        }

        private static float Length(float x, float y) => (float)Math.Sqrt(x * x + y * y);
    }
}
