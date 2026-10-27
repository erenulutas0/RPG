using System;
using System.Collections.Generic;
using Cryptforge.Art;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // A pre-rendered character's frame selection (docs/45: Ilya, rendered from one model): which facing a floor
    // direction is drawn in, which frame a distance or a clock shows, and the rules RenderedCharacterAnimator follows,
    // without Unity.
    public sealed class RenderedCharacterArtTests
    {
        private const float Stride = 1.2f;
        private const float Tick = 1f / 60;

        // Ilya's clips: idle 8 at 5 a second, two 8-frame walks, strike 5 at 20, cast 5 at 14, hit 3 at 20, death 5 at 9.
        private static RenderedClipTiming[] Timings() => new[]
        {
            new RenderedClipTiming(8, 5f, true),
            new RenderedClipTiming(8, 0f, true),
            new RenderedClipTiming(8, 0f, true),
            new RenderedClipTiming(5, 20f, false),
            new RenderedClipTiming(5, 14f, false),
            new RenderedClipTiming(3, 20f, false),
            new RenderedClipTiming(5, 9f, false)
        };

        private static RenderedCharacterPlayer Player() => new RenderedCharacterPlayer(Timings(), Stride);

        [Test]
        public void TheEightSectorsFollowTheFloorCompass()
        {
            Assert.That(RenderedCharacterFrames.Sector(1, 0), Is.EqualTo(0), "East.");
            Assert.That(RenderedCharacterFrames.Sector(1, 1), Is.EqualTo(1));
            Assert.That(RenderedCharacterFrames.Sector(0, 1), Is.EqualTo(2), "North, up the screen.");
            Assert.That(RenderedCharacterFrames.Sector(-1, 1), Is.EqualTo(3));
            Assert.That(RenderedCharacterFrames.Sector(-1, 0), Is.EqualTo(4));
            Assert.That(RenderedCharacterFrames.Sector(-1, -1), Is.EqualTo(5));
            Assert.That(RenderedCharacterFrames.Sector(0, -1), Is.EqualTo(6), "South, toward the camera.");
            Assert.That(RenderedCharacterFrames.Sector(1, -1), Is.EqualTo(7));
            Assert.That(RenderedCharacterFrames.Sector(1, -0.3f), Is.EqualTo(0), "Within 22.5 degrees of east.");
            Assert.That(RenderedCharacterFrames.Sector(0, 0), Is.EqualTo(-1), "No displacement, no direction.");
        }

        [Test]
        public void TheWestHalfMirrorsTheEastDrawings()
        {
            var expected = new (RenderedFacing facing, bool mirrored)[]
            {
                (RenderedFacing.East, false), (RenderedFacing.NorthEast, false), (RenderedFacing.North, false),
                (RenderedFacing.NorthEast, true), (RenderedFacing.East, true), (RenderedFacing.SouthEast, true),
                (RenderedFacing.South, false), (RenderedFacing.SouthEast, false)
            };
            var drawn = new HashSet<RenderedFacing>();
            for (int sector = 0; sector < RenderedCharacterFrames.SectorCount; sector++)
            {
                RenderedFacing facing = RenderedCharacterFrames.FacingOf(sector, out bool mirrored);
                Assert.That(facing, Is.EqualTo(expected[sector].facing), "sector " + sector);
                Assert.That(mirrored, Is.EqualTo(expected[sector].mirrored), "sector " + sector);
                drawn.Add(facing);
            }
            Assert.That(drawn.Count, Is.EqualTo(RenderedCharacterFrames.FacingCount), "Five drawings cover the compass.");
        }

        [Test]
        public void AFacingHoldsUntilTheTravelLeavesItsSectorByTheMargin()
        {
            float Along(float degrees, out float y)
            {
                double radians = degrees * Math.PI / 180;
                y = (float)Math.Sin(radians);
                return (float)Math.Cos(radians);
            }

            float x = Along(22.5f + RenderedCharacterFrames.TurnMargin - 1f, out float y);
            Assert.That(RenderedCharacterFrames.KeepSector(0, x, y), Is.EqualTo(0), "Inside the margin: still east.");
            x = Along(22.5f + RenderedCharacterFrames.TurnMargin + 1f, out y);
            Assert.That(RenderedCharacterFrames.KeepSector(0, x, y), Is.EqualTo(1), "Past it: north-east.");
            x = Along(-30f, out y);
            Assert.That(RenderedCharacterFrames.KeepSector(0, x, y), Is.EqualTo(0), "Either side of the sector.");
            Assert.That(RenderedCharacterFrames.KeepSector(6, -1, 0), Is.EqualTo(4), "A real turn turns at once.");
            Assert.That(RenderedCharacterFrames.KeepSector(3, 0, 0), Is.EqualTo(3), "No displacement keeps the facing.");
        }

        [Test]
        public void TheWalkFrameFollowsTheDistanceWalked()
        {
            Assert.That(RenderedCharacterFrames.WalkFrame(0, Stride, 8), Is.EqualTo(0), "The right foot's contact.");
            Assert.That(RenderedCharacterFrames.WalkFrame(Stride / 8, Stride, 8), Is.EqualTo(1));
            Assert.That(RenderedCharacterFrames.WalkFrame(Stride / 16 - 1e-4f, Stride, 8), Is.EqualTo(0), "The nearest pose.");
            Assert.That(RenderedCharacterFrames.WalkFrame(Stride / 2, Stride, 8), Is.EqualTo(4), "The left foot's contact.");
            Assert.That(RenderedCharacterFrames.WalkFrame(Stride * 15 / 16 + 1e-4f, Stride, 8), Is.EqualTo(0), "It wraps.");
            Assert.That(RenderedCharacterFrames.WalkFrame(Stride * 3 + Stride / 4, Stride, 8), Is.EqualTo(2));
            var seen = new HashSet<int>();
            for (int i = 0; i < 96; i++)
                seen.Add(RenderedCharacterFrames.WalkFrame(i * Stride / 96, Stride, 8));
            Assert.That(seen.Count, Is.EqualTo(8), "One stride shows every pose.");
        }

        [Test]
        public void LoopsWrapAndOneShotsHoldTheirLastFrame()
        {
            Assert.That(RenderedCharacterFrames.TimedFrame(0, 5, 8, true), Is.EqualTo(0));
            Assert.That(RenderedCharacterFrames.TimedFrame(0.39f, 5, 8, true), Is.EqualTo(1));
            Assert.That(RenderedCharacterFrames.TimedFrame(1.61f, 5, 8, true), Is.EqualTo(0), "8 frames at 5 a second loop in 1.6 s.");
            Assert.That(RenderedCharacterFrames.TimedFrame(5f, 9, 5, false), Is.EqualTo(4), "Death holds.");
            Assert.That(RenderedCharacterFrames.ProgressFrame(0, 5), Is.EqualTo(0));
            Assert.That(RenderedCharacterFrames.ProgressFrame(0.5f, 5), Is.EqualTo(2));
            Assert.That(RenderedCharacterFrames.ProgressFrame(1f, 5), Is.EqualTo(4));
            Assert.That(RenderedCharacterFrames.StrikeSeconds(0.5f), Is.EqualTo(RenderedCharacterFrames.MaxStrikeSeconds));
            Assert.That(RenderedCharacterFrames.StrikeSeconds(0.2f), Is.EqualTo(0.2f * RenderedCharacterFrames.StrikeShareOfInterval)
                .Within(1e-6f), "A fast weapon's strike fits inside its interval.");
        }

        [Test]
        public void AStandingHeroIdlesFacingTheCameraAndBreathes()
        {
            RenderedCharacterPlayer player = Player();
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Idle));
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.South));
            Assert.That(player.Mirrored, Is.False);
            var seen = new HashSet<int>();
            for (int i = 0; i < 100; i++)
            {
                player.Step(Tick, 0, 0);
                Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Idle));
                seen.Add(player.Frame);
            }
            Assert.That(seen.Count, Is.EqualTo(8), "The idle loop plays through its eight frames.");
        }

        [Test]
        public void WalkingFacesTheTravelAndStepsThroughTheStrideByDistance()
        {
            RenderedCharacterPlayer player = Player();
            var seen = new HashSet<int>();
            const float speed = 3.5f;
            for (int i = 0; i < 30; i++)
            {
                player.Step(Tick, speed * Tick, 0);
                Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Walk));
                Assert.That(player.Frame, Is.EqualTo(RenderedCharacterFrames.WalkFrame(player.WalkDistance, Stride, 8)));
                seen.Add(player.Frame);
            }
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.East));
            Assert.That(player.Mirrored, Is.False);
            Assert.That(seen.Count, Is.EqualTo(8), "Half a second at 3.5 units a second covers a stride.");

            player.Step(Tick, -speed * Tick, 0);
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.East));
            Assert.That(player.Mirrored, Is.True, "Walking west draws the east facing mirrored.");
            player.Step(Tick, 0, -speed * Tick);
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.South));
            Assert.That(player.Mirrored, Is.False);
            player.Step(Tick, speed * Tick, speed * Tick);
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.NorthEast));
        }

        [Test]
        public void StoppingIdlesAtOnceAndOnlyALongerStopRestartsTheStride()
        {
            RenderedCharacterPlayer player = Player();
            for (int i = 0; i < 7; i++)
                player.Step(Tick, 0.05f, 0);
            float walked = player.WalkDistance;
            Assert.That(walked, Is.GreaterThan(0f));
            player.Step(Tick, 0, 0);
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Idle), "Standing shows the idle at once.");
            Assert.That(player.WalkDistance, Is.EqualTo(walked), "A short stall keeps the stride.");
            for (int i = 0; i < 10; i++)
                player.Step(Tick, 0, 0);
            Assert.That(player.WalkDistance, Is.Zero, "A real stop starts the next walk on a contact.");
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.East), "And keeps the facing it walked in.");
        }

        [Test]
        public void AStandingStrikePlaysItsFramesAtOnceThenIdles()
        {
            RenderedCharacterPlayer player = Player();
            player.Step(Tick, 0, 0);
            player.Strike(RenderedCharacterFrames.StrikeSeconds(0.5f));
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Strike), "The damage has landed: the strike shows at once.");
            Assert.That(player.Frame, Is.Zero);
            int last = 0;
            var seen = new HashSet<int>();
            while (player.IsStriking)
            {
                player.Step(Tick, 0, 0);
                if (!player.IsStriking)
                    break;
                Assert.That(player.Frame, Is.GreaterThanOrEqualTo(last), "Forward only.");
                last = player.Frame;
                seen.Add(player.Frame);
            }
            Assert.That(seen.Count, Is.EqualTo(5), "Every strike frame shows in a quarter second at 60 fps.");
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Idle));
        }

        // The owner, 2026-10-26: attacks froze the Vanguard's legs. A walking strike keeps the walk's frame for the
        // distance, with the strike's arms.
        [Test]
        public void AWalkingStrikeKeepsTheStride()
        {
            RenderedCharacterPlayer player = Player();
            RenderedCharacterPlayer walker = Player();
            for (int i = 0; i < 5; i++)
            {
                player.Step(Tick, 0.05f, 0);
                walker.Step(Tick, 0.05f, 0);
            }
            player.Strike(0.25f);
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.WalkStrike));
            for (int i = 0; i < 12; i++)
            {
                player.Step(Tick, 0.05f, 0);
                walker.Step(Tick, 0.05f, 0);
                Assert.That(player.Motion, Is.EqualTo(RenderedMotion.WalkStrike), "tick " + i);
                Assert.That(player.Frame, Is.EqualTo(walker.Frame), "The legs are the walk's, tick " + i);
            }
            for (int i = 0; i < 10; i++)
                player.Step(Tick, 0.05f, 0);
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Walk), "Back to the plain walk once the strike ends.");
        }

        [Test]
        public void AStandingHeroFacesWhatItStrikesAndAWalkingOneKeepsItsTravel()
        {
            RenderedCharacterPlayer player = Player();
            player.Step(Tick, 0, 0);
            player.FaceToward(-1, 0.2f);
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.East));
            Assert.That(player.Mirrored, Is.True, "Turned to the target on its left.");
            player.Step(Tick, 0, 0.05f);
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.North));
            player.FaceToward(0, -1);
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.North), "Walking, the travel keeps the facing.");
        }

        [Test]
        public void TheVentsCastPlaysWholeThenReturns()
        {
            RenderedCharacterPlayer player = Player();
            player.Cast();
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Cast));
            var seen = new List<int> { player.Frame };
            float played = 0;
            while (player.IsCasting)
            {
                player.Step(Tick, 0.02f, 0);
                played += Tick;
                if (player.IsCasting)
                {
                    Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Cast), "Walking does not cut the cast short.");
                    seen.Add(player.Frame);
                }
            }
            Assert.That(new HashSet<int>(seen).Count, Is.EqualTo(5));
            Assert.That(played, Is.EqualTo(5f / 14).Within(2 * Tick), "Five frames at fourteen a second.");
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Walk));
        }

        [Test]
        public void AHitFlinchesOnlyAHeroStandingIdle()
        {
            RenderedCharacterPlayer player = Player();
            player.Step(Tick, 0, 0);
            player.Hit();
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Hit));
            for (int i = 0; i < 12; i++)
                player.Step(Tick, 0, 0);
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Idle), "Three frames at twenty a second.");

            player.Step(Tick, 0.05f, 0);
            player.Hit();
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Walk), "A walking hero only flashes.");
            player.Step(Tick, 0, 0);
            player.Strike(0.25f);
            player.Hit();
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Strike), "A striking one too.");
        }

        [Test]
        public void DeathPlaysOnceHoldsAndIgnoresEverythingAfter()
        {
            RenderedCharacterPlayer player = Player();
            player.Step(Tick, 0.05f, 0);
            player.Die();
            Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Death));
            Assert.That(player.Frame, Is.Zero);
            int last = 0;
            for (int i = 0; i < 90; i++)
            {
                player.Step(Tick, 0.05f, 0.05f);
                player.Strike(0.25f);
                player.Cast();
                player.Hit();
                player.FaceToward(-1, 0);
                Assert.That(player.Motion, Is.EqualTo(RenderedMotion.Death));
                Assert.That(player.Frame, Is.GreaterThanOrEqualTo(last), "It only ever falls further.");
                last = player.Frame;
            }
            Assert.That(player.Frame, Is.EqualTo(4), "And lies still on the last frame.");
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.East), "Facing where it fell.");
        }

        [Test]
        public void ATickWithoutTimeChangesNothing()
        {
            RenderedCharacterPlayer player = Player();
            for (int i = 0; i < 9; i++)
                player.Step(Tick, 0.05f, 0);
            player.Strike(0.25f);
            RenderedMotion motion = player.Motion;
            int frame = player.Frame;
            float walked = player.WalkDistance;
            for (int i = 0; i < 30; i++)
                player.Step(0f, 0.5f, 0.5f);
            Assert.That(player.Motion, Is.EqualTo(motion));
            Assert.That(player.Frame, Is.EqualTo(frame));
            Assert.That(player.WalkDistance, Is.EqualTo(walked));
            Assert.That(player.Facing, Is.EqualTo(RenderedFacing.East));
            Assert.That(player.IsStriking, Is.True, "A paused strike waits for the time to come back.");
        }

        [Test]
        public void APlayerNeedsATimingForEveryMotionAndAStride()
        {
            Assert.Throws<ArgumentException>(() => new RenderedCharacterPlayer(new RenderedClipTiming[3], Stride));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RenderedCharacterPlayer(Timings(), 0f));
        }
    }
}
