using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The pack's spacing while enemies walk: no two living enemies ever stand within a body of each other, whichever of them
    // is nearer the hero, however the hero walks, at the rim, through deaths and at any frame rate; an enemy held back
    // waits or slides rather than turning back and forth, and the pack comes to rest once the hero does.
    public sealed class PackSpacingTests
    {
        private const float Frame = 1f / 60f;
        // EncounterController's _bodySpacing in Gameplay.unity.
        private const float BodySpacing = 0.9f;
        // DescentSimulation's Grunt and Cinder Mite: speed and reach on the floor.
        private const float GruntSpeed = 1.6f;
        private const float MiteSpeed = 2.2f;
        private const float Reach = 1.5f;
        private static readonly ArenaGeometry Arena = new ArenaGeometry(-9f, 9f, 9f);

        // The hole the one-sided rule left: an enemy nearer the hero than a neighbour standing beside its path walked
        // straight into that neighbour's spacing, because only enemies nearer the hero could hold it back. Now it closes up
        // to the spacing, slides round as far as that still brings it nearer its point, and waits there.
        [Test]
        public void ANearerEnemyNoLongerWalksIntoAFartherOneStandingBesideItsPath()
        {
            var motion = new PackMotion(BodySpacing, 2f);
            var standing = new HealthState(50f);
            var walker = new HealthState(15f);
            // It stands 0.943 from the hero and never moves. The walker comes straight down at its point 0.7 from the hero,
            // which lies inside that spacing, and first reaches the spacing 0.912 from the hero: nearer than its neighbour.
            motion.Add(standing, new EntryPlacement(EntrySide.Far, 0f, 0.8f, 0.5f), 0f, 5f);
            motion.Add(walker, new EntryPlacement(EntrySide.Far, 0f, 0f, 3f), 2f, 0.8f);

            var lastX = new float[2];
            var lastY = new float[2];
            int stillFrames = 0;
            for (int frame = 0; frame < 300; frame++)
            {
                lastX[1] = motion.XOf(1);
                lastY[1] = motion.YOf(1);
                motion.Step(Frame, 0f, 0f);
                Assert.That(Squared(motion.XOf(1) - motion.XOf(0)) + Squared(motion.YOf(1) - motion.YOf(0)),
                    Is.GreaterThanOrEqualTo(BodySpacing * BodySpacing), $"Frame {frame}: the walker stepped inside the spacing.");
                stillFrames = motion.XOf(1) == lastX[1] && motion.YOf(1) == lastY[1] ? stillFrames + 1 : 0;
            }

            Assert.That((motion.XOf(0), motion.YOf(0)), Is.EqualTo((0.8f, 0.5f)), "Nothing pushes the standing enemy.");
            Assert.That(stillFrames, Is.GreaterThanOrEqualTo(60), "The walker comes to rest instead of circling its neighbour.");
            Assert.That(motion.HasArrived(1), Is.False);
            Assert.That(motion.IsStalled(1), Is.True, "Its point is taken, so it waits.");
            float apart = (float)Math.Sqrt(Squared(motion.XOf(1) - 0.8f) + Squared(motion.YOf(1) - 0.5f));
            Assert.That(apart, Is.LessThan(BodySpacing + 2f * Frame), "It closed right up to the spacing, not short of it.");
            Assert.That(motion.YOf(1), Is.LessThan(0.8f), "It slid round below where walking straight on would have stopped it.");
        }

        // A second enemy heading for the very point the first one holds waits straight behind it, and walks in the moment
        // the first one dies: a dead enemy holds nobody back.
        [Test]
        public void AnEnemyWaitingBehindAnotherWalksInOnceThatOneDies()
        {
            var motion = new PackMotion(BodySpacing, 2f);
            var front = new HealthState(50f);
            var back = new HealthState(50f);
            motion.Add(front, new EntryPlacement(EntrySide.Far, 0f, 0f, 3f), GruntSpeed, Reach);
            motion.Add(back, new EntryPlacement(EntrySide.Far, 0f, 0f, 4.5f), GruntSpeed, Reach);
            for (int frame = 0; frame < 180; frame++)
                motion.Step(Frame, 0f, 0f);

            Assert.That(motion.HasArrived(0), Is.True);
            Assert.That(motion.HasArrived(1), Is.False);
            Assert.That(motion.IsStalled(1), Is.True, "Its way is straight through the front one, so it waits.");
            Assert.That(motion.IsStalled(0), Is.False, "An arrived enemy is not waiting.");
            Assert.That(motion.YOf(1) - motion.YOf(0), Is.GreaterThanOrEqualTo(BodySpacing).And.LessThan(BodySpacing + 0.1f),
                "It waits a body behind, not further back.");

            front.ApplyDamage(new DamageContext(50f));
            int walked = 0;
            while (!motion.HasArrived(1) && walked < 120)
            {
                motion.Step(Frame, 0f, 0f);
                Assert.That(motion.IsStalled(1), Is.False, $"Frame {walked} after the death: nothing holds it back.");
                walked++;
            }
            Assert.That(motion.HasArrived(1), Is.True, "It takes the dead enemy's place.");
            Assert.That(motion.YOf(1), Is.EqualTo(Reach - PackMotion.ReachMargin).Within(1e-4f));
        }

        // Two enemies that start inside each other's spacing - which only a platform too small for the wave can make
        // EntrySides do - are never frozen by it: a step that widens their gap is always allowed, so they part, and once a
        // body apart they stay so.
        [Test]
        public void TwoEnemiesStartingInsideEachOthersSpacingPartAndStayApart()
        {
            var motion = new PackMotion(BodySpacing, 2f);
            // Both enter at the left corner 0.45 apart; the widest offsets send one to its point below-left of the hero and the
            // other to its point above-left, 2.4 apart, and each point is the nearest the hero its straight path comes.
            motion.Add(new HealthState(15f), new EntryPlacement(EntrySide.Left, -2f, -3f, -0.2f), MiteSpeed, Reach);
            motion.Add(new HealthState(15f), new EntryPlacement(EntrySide.Left, 2f, -2.8f, 0.2f), MiteSpeed, Reach);

            float gapSquared = Squared(motion.XOf(1) - motion.XOf(0)) + Squared(motion.YOf(1) - motion.YOf(0));
            Assert.That(gapSquared, Is.LessThan(BodySpacing * BodySpacing), "The two start overlapping.");
            bool parted = false;
            for (int frame = 0; frame < 600; frame++)
            {
                motion.Step(Frame, 0f, 0f);
                float nowSquared = Squared(motion.XOf(1) - motion.XOf(0)) + Squared(motion.YOf(1) - motion.YOf(0));
                if (parted)
                    Assert.That(nowSquared, Is.GreaterThanOrEqualTo(BodySpacing * BodySpacing), $"Frame {frame}: parted, they stay apart.");
                else
                    Assert.That(nowSquared, Is.GreaterThanOrEqualTo(gapSquared), $"Frame {frame}: a gap already too small only widens.");
                parted |= nowSquared >= BodySpacing * BodySpacing;
                gapSquared = nowSquared;
            }
            Assert.That(parted, Is.True);
            Assert.That(motion.HasArrived(0) && motion.HasArrived(1), Is.True, "Both reach their points.");
        }

        // An enemy that was sliding round a neighbour when the hero stopped, with its point just round the other side of that
        // neighbour, still gets there. A version of this rule that remembered the side it slid on kept such an enemy waiting
        // for as long as the neighbour lived: these are the positions an adversarial search found it frozen at under the pure
        // .NET runner, where a pack with no memory walks it in within 118 frames.
        [Test]
        public void AnEnemySlidingRoundANeighbourWhenTheHeroStopsStillReachesItsPoint()
        {
            var motion = new PackMotion(BodySpacing, 2f, Arena, 0f, 0f);
            var neighbour = new HealthState(15f);
            var slider = new HealthState(15f);
            motion.Add(neighbour, new EntryPlacement(EntrySide.Right, 0.12679307f, 1.1104497f, 0.08602521f), MiteSpeed, Reach);
            motion.Add(slider, new EntryPlacement(EntrySide.Left, 0.15661174f, 2.6190205f, 0.84590995f), MiteSpeed, Reach);
            var hero = new HeroMotion(DescentSimulation.HeroSpeed);
            hero.Place(0f, 0f, Arena);
            var bodies = new[] { neighbour, slider };
            for (int frame = 0; frame < 79; frame++)
            {
                double angle = frame < 27 ? 2.7095531751530317 : 5.638563156305101;
                hero.Move((float)Math.Cos(angle), (float)Math.Sin(angle), Frame, Arena);
                motion.Step(Frame, hero.X, hero.Y);
                Assert.That(FirstBreach(motion, bodies, $"walking, frame {frame}"), Is.Null);
            }

            int standing = 0;
            while (!motion.HasArrived(1) && standing < 1200)
            {
                motion.Step(Frame, hero.X, hero.Y);
                Assert.That(FirstBreach(motion, bodies, $"standing, frame {standing}"), Is.Null);
                standing++;
            }
            Assert.That(motion.HasArrived(0), Is.True, "The neighbour stands at its point.");
            Assert.That(motion.HasArrived(1), Is.True, "The slider reaches its point once the hero stands still.");
        }

        // Two enemies stand 2 apart across the walker's way, leaving a gap a fifth of a unit wide between their spacings.
        // Glancing off one and then the other would zigzag down the gap; the walker instead slides along both spacings at
        // once, down the middle, and never takes a visible step back against the one before.
        [Test]
        public void AnEnemySqueezingBetweenTwoOthersWalksDownTheGapWithoutTurningBack()
        {
            var motion = new PackMotion(BodySpacing, 2f);
            motion.Add(new HealthState(50f), new EntryPlacement(EntrySide.Far, 0f, -1f, 2.6f), 0f, 5f);
            motion.Add(new HealthState(50f), new EntryPlacement(EntrySide.Far, 0f, 1f, 2.6f), 0f, 5f);
            motion.Add(new HealthState(15f), new EntryPlacement(EntrySide.Far, 0f, 0.35f, 5f), MiteSpeed, Reach);

            var turns = new TurnCounter(3);
            int frame = 0;
            for (; frame < 600 && !motion.HasArrived(2); frame++)
            {
                motion.Step(Frame, 0f, 0f);
                for (int other = 0; other < 2; other++)
                {
                    Assert.That(Squared(motion.XOf(2) - motion.XOf(other)) + Squared(motion.YOf(2) - motion.YOf(other)),
                        Is.GreaterThanOrEqualTo(BodySpacing * BodySpacing), $"Frame {frame}: inside slot {other}'s spacing.");
                }
                turns.Observe(motion, 2, frame * Frame);
            }

            Assert.That(motion.HasArrived(2), Is.True, "The gap is wide enough, so the walker gets through to its point.");
            Assert.That(turns.Back, Is.Zero, "It never stepped back against its previous step.");
            // Unobstructed, the walk is 3.6 units at 2.2 a second, 99 frames; sliding down the gap may take a little longer.
            Assert.That(frame, Is.LessThan(150), "Sliding along the gap costs it little time.");
        }

        // Ten enemies - two Grunts, eight Cinder Mites, the proof floor's wave - chase a hero that walks away and back, turns
        // round every three quarters of a second, circles, and runs into the right corner and along the rim. One of them
        // dies mid-chase. On every frame every pair of living enemies is at least a body apart, every enemy stands on the
        // platform, and every position is a number. Once the hero stops, the pack settles without flickering - no enemy takes
        // two visible steps back within DescentSimulation.FlickerSeconds - and within thirty seconds nothing moves at all (the
        // slowest measured, a Grunt working its way round the crowd at a twentieth of a unit a second, stops after ten). A
        // single turn is allowed: a fast Mite catching a slow Grunt in a crowd is nudged a third of a texel one way and then
        // the other as it squeezes past, which is sliding, not flickering.
        [Test]
        public void TenEnemiesChasingAWalkingHeroStayABodyApartOnThePlatformAndComeToRest()
        {
            ChaseTenPack("away and back", frame => frame < 240 ? (-1f, 0f) : (1f, 0f));
            ChaseTenPack("turning round every 0.75 s", frame => ((frame / 45) % 2 == 0 ? 1f : -1f, 0f));
            ChaseTenPack("circling", frame => ((float)Math.Cos(frame * 0.02f), (float)Math.Sin(frame * 0.02f)));
            ChaseTenPack("into the right corner and along the rim",
                frame => frame < 240 ? (1f, 0f) : frame < 420 ? (0f, 1f) : (-0.2f, -1f));
        }

        // Walking a frame in one step after a hitch and walking it in MaxStride steps give the same pack, bit for bit, and
        // the spacing holds at 30, 60 and 144 frames a second, through a mix of hitches up to a third of a second, and even
        // through a five-second frame, which walks past MaxSubSteps with longer steps.
        [Test]
        public void ALongFrameIsWalkedInShortStepsAndTheSpacingHoldsAtAnyFrameRate()
        {
            // 0.375 s at the Mites' 2.2 a second is 0.825 units, four steps of 0.09375, a binary fraction, so the division is exact.
            PackMotion whole = TenPack(0f, 0f, out _);
            PackMotion split = TenPack(0f, 0f, out _);
            for (int frame = 0; frame < 30; frame++)
            {
                whole.Step(0.375f, 0f, 0f);
                for (int step = 0; step < 4; step++)
                    split.Step(0.09375f, 0f, 0f);
                for (int i = 0; i < PackLayout.MaxPackSize; i++)
                {
                    Assert.That((whole.XOf(i), whole.YOf(i)), Is.EqualTo((split.XOf(i), split.YOf(i))),
                        $"Long frame {frame}, slot {i}: one call walks the four steps four calls do.");
                }
            }

            ChaseAtFrameTimes("30 Hz", new[] { 1f / 30f });
            ChaseAtFrameTimes("144 Hz", new[] { 1f / 144f });
            ChaseAtFrameTimes("hitches", new[] { Frame, Frame, 0.25f, Frame, 1f / 3f, Frame, 0.05f, 0.1f, Frame });

            PackMotion stalled = TenPack(0f, 0f, out HealthState[] bodies);
            stalled.Step(5f, 0f, 0f);
            Assert.That(FirstBreach(stalled, bodies, "a five-second frame"), Is.Null);
        }

        // PackMotion runs every frame of every fight on the phone, so a step must not allocate: every buffer is sized for the
        // largest pack when the pack is made. The pure .NET runner measures it; the Editor's Mono reports no allocated bytes
        // for a thread at all, so there this passes without measuring.
        [Test]
        public void SteppingACrowdedPackAllocatesNothing()
        {
            PackMotion motion = TenPack(0f, 0f, out _);
            var hero = new HeroMotion(DescentSimulation.HeroSpeed);
            hero.Place(0f, 0f, Arena);
            motion.Step(Frame, 0f, 0f);

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 600; frame++)
            {
                hero.Move(frame % 90 < 45 ? 1f : -1f, 0.3f, Frame, Arena);
                motion.Step(Frame, hero.X, hero.Y);
                motion.Step(0.3f, hero.X, hero.Y);
            }
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero, "Bytes allocated over 1200 steps of a ten-enemy pack.");
        }

        private static void ChaseTenPack(string walk, Func<int, (float, float)> steer)
        {
            PackMotion motion = TenPack(0f, 0f, out HealthState[] bodies);
            var hero = new HeroMotion(DescentSimulation.HeroSpeed);
            hero.Place(0f, 0f, Arena);
            var turns = new TurnCounter(PackLayout.MaxPackSize);
            var lastX = new float[PackLayout.MaxPackSize];
            var lastY = new float[PackLayout.MaxPackSize];
            int restingFrames = 0;
            for (int frame = 0; frame < 2400; frame++)
            {
                if (frame == 200)
                    bodies[3].ApplyDamage(new DamageContext(100f));
                bool walking = frame < 600;
                if (walking)
                {
                    (float steerX, float steerY) = steer(frame);
                    hero.Move(steerX, steerY, Frame, Arena);
                }
                // Turning to follow a hero that has just stopped is not turning back; count from the stop on.
                if (frame == 600)
                    turns = new TurnCounter(PackLayout.MaxPackSize);

                bool moved = false;
                for (int i = 0; i < PackLayout.MaxPackSize; i++)
                {
                    lastX[i] = motion.XOf(i);
                    lastY[i] = motion.YOf(i);
                }
                // The scene's order: the hero walks, then the pack steps toward where it now stands.
                motion.Step(Frame, hero.X, hero.Y);
                string breach = FirstBreach(motion, bodies, $"{walk}, frame {frame}");
                Assert.That(breach, Is.Null);
                for (int i = 0; i < PackLayout.MaxPackSize; i++)
                {
                    if (!bodies[i].IsAlive)
                        continue;
                    moved |= motion.XOf(i) != lastX[i] || motion.YOf(i) != lastY[i];
                    turns.Observe(motion, i, frame * Frame);
                }
                restingFrames = moved ? 0 : restingFrames + 1;
            }

            Assert.That(turns.Flickers, Is.Zero, $"{walk}: with the hero standing, no enemy stepped back and forth.");
            Assert.That(restingFrames, Is.GreaterThanOrEqualTo(60), $"{walk}: the pack came to rest after the hero stopped.");
        }

        private static void ChaseAtFrameTimes(string rate, float[] frameTimes)
        {
            PackMotion motion = TenPack(0f, 0f, out HealthState[] bodies);
            var hero = new HeroMotion(DescentSimulation.HeroSpeed);
            hero.Place(0f, 0f, Arena);
            float seconds = 0f;
            for (int call = 0; seconds < 20f; call++)
            {
                float deltaTime = frameTimes[call % frameTimes.Length];
                seconds += deltaTime;
                if (call == 40)
                    bodies[0].ApplyDamage(new DamageContext(100f));
                hero.Move((float)Math.Cos(seconds * 0.8f), (float)Math.Sin(seconds * 0.8f), deltaTime, Arena);
                motion.Step(deltaTime, hero.X, hero.Y);
                Assert.That(FirstBreach(motion, bodies, $"{rate}, {seconds:0.000} s"), Is.Null);
            }
        }

        // Two Grunts and eight Cinder Mites entering round the hero where the scene's spawn places them.
        private static PackMotion TenPack(float heroX, float heroY, out HealthState[] bodies)
        {
            var placements = new EntryPlacement[PackLayout.MaxPackSize];
            EntrySides.Place(0, PackLayout.MaxPackSize, heroX, heroY, Arena, DescentSimulation.EntryDepth,
                DescentSimulation.FormationSpacing, BodySpacing, placements);
            var motion = new PackMotion(BodySpacing, PackLayout.HalfWidth * DescentSimulation.FormationSpacing, Arena, heroX, heroY);
            bodies = new HealthState[PackLayout.MaxPackSize];
            for (int i = 0; i < PackLayout.MaxPackSize; i++)
            {
                bodies[i] = new HealthState(i < 2 ? 50f : 15f);
                motion.Add(bodies[i], placements[i], i < 2 ? GruntSpeed : MiteSpeed, Reach);
            }
            return motion;
        }

        // Why the pack breaks its guarantees, or null: a position that is not a number, a living enemy off the platform, or
        // two living enemies within a body of each other.
        private static string FirstBreach(PackMotion motion, HealthState[] bodies, string where)
        {
            for (int i = 0; i < bodies.Length; i++)
            {
                if (!bodies[i].IsAlive)
                    continue;
                float x = motion.XOf(i);
                float y = motion.YOf(i);
                if (float.IsNaN(x) || float.IsInfinity(x) || float.IsNaN(y) || float.IsInfinity(y))
                    return $"{where}: slot {i} stands at ({x}, {y}).";
                if (!Arena.IsOnPlatform(x, y, HeroMotion.EdgeMargin - 1e-3f))
                    return $"{where}: slot {i} left the platform at ({x}, {y}).";
                for (int other = 0; other < i; other++)
                {
                    if (!bodies[other].IsAlive)
                        continue;
                    float apart = Squared(x - motion.XOf(other)) + Squared(y - motion.YOf(other));
                    if (apart < BodySpacing * BodySpacing)
                        return $"{where}: slots {other} and {i} stand {Math.Sqrt(apart):0.000} apart.";
                }
            }
            return null;
        }

        private static float Squared(float value) => value * value;

        // Counts visible steps taken back against the step before, and the ones that come within
        // DescentSimulation.FlickerSeconds of the same enemy's previous one. A step is measured, as DescentSimulation measures
        // it, from where the enemy stood when its last one was counted and counts once it reaches VisibleStep; unlike the
        // simulation, a step after standing still is still compared with the one before the stand, so this counts more.
        private sealed class TurnCounter
        {
            private readonly float[] _x;
            private readonly float[] _y;
            private readonly float[] _stepX;
            private readonly float[] _stepY;
            private readonly float[] _turned;
            private readonly bool[] _started;

            public int Back { get; private set; }
            public int Flickers { get; private set; }

            public TurnCounter(int count)
            {
                _x = new float[count];
                _y = new float[count];
                _stepX = new float[count];
                _stepY = new float[count];
                _turned = new float[count];
                _started = new bool[count];
                for (int i = 0; i < count; i++)
                    _turned[i] = float.MinValue;
            }

            public void Observe(PackMotion motion, int i, float seconds)
            {
                if (!_started[i])
                {
                    _started[i] = true;
                    _x[i] = motion.XOf(i);
                    _y[i] = motion.YOf(i);
                    return;
                }
                float stepX = motion.XOf(i) - _x[i];
                float stepY = motion.YOf(i) - _y[i];
                if (stepX * stepX + stepY * stepY < DescentSimulation.VisibleStep * DescentSimulation.VisibleStep)
                    return;
                if (stepX * _stepX[i] + stepY * _stepY[i] < 0f)
                {
                    Back++;
                    if (seconds - _turned[i] <= DescentSimulation.FlickerSeconds)
                        Flickers++;
                    _turned[i] = seconds;
                }
                _x[i] = motion.XOf(i);
                _y[i] = motion.YOf(i);
                _stepX[i] = stepX;
                _stepY[i] = stepY;
            }
        }
    }
}
