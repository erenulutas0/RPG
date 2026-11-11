using System;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The development autopilot's footwork in a fight (FootworkRule), one body for the scene's autopilot and the foundry
    // simulation's copy of it. Places are given in the foundry's design plane (FoundryMap.ToFloor turns them onto the
    // floor without changing a distance): the court's east wall stands at u = 14, the pit's hole spans u -16..-4, v 2..14.
    public sealed class FootworkRuleTests
    {
        private const float Turn = 35f * (3.14159274f * 2f / 360f);
        private const float Stride = 2f;
        private const float Radius = 3.2f;

        private static void Floor(float u, float v, out float x, out float y) => FoundryMap.ToFloor(u, v, out x, out y);

        private static void Design(float x, float y, out float u, out float v) => FoundryMap.ToDesign(x, y, out u, out v);

        [Test]
        public void InTheOpenTheKiteTurnsThePushByItsTurn()
        {
            Floor(4f, -8f, out float x, out float y);
            Floor(1f, 0f, out float px, out float py);
            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, false, out float gx, out float gy), Is.True);
            float cos = (float)Math.Cos(Turn), sin = (float)Math.Sin(Turn);
            Assert.That(gx, Is.EqualTo(x + (px * cos - py * sin) * Stride).Within(1e-5f));
            Assert.That(gy, Is.EqualTo(y + (px * sin + py * cos) * Stride).Within(1e-5f));
        }

        // Pushed into the east wall the turned push runs off the floor, as the autopilot's kite did before it stood there;
        // the kite turns on until its stride has room.
        [Test]
        public void AgainstAWallTheKiteTurnsUntilItsStrideHasRoom()
        {
            Floor(13f, -8f, out float x, out float y);
            Floor(1f, 0f, out float px, out float py);
            float cos = (float)Math.Cos(Turn), sin = (float)Math.Sin(Turn);
            float straightX = x + (px * cos - py * sin) * Stride, straightY = y + (px * sin + py * cos) * Stride;
            Assert.That(FoundryMap.Contains(straightX, straightY, HeroMotion.EdgeMargin), Is.False, "the turned push leaves the floor");

            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, false, out float gx, out float gy), Is.True);
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.True);
            Assert.That(FoundryMap.Segment(x, y, gx, gy, HeroMotion.EdgeMargin), Is.True);
            Assert.That(ArenaFloor.FloorDistanceSquared(gx, gy, x, y), Is.EqualTo(Stride * Stride).Within(1e-3f));
            Design(gx, gy, out float u, out _);
            Assert.That(u, Is.LessThan(13.4f), "along the wall or away from it");
        }

        // With the hero on the mark the escape leaves it on the far side from the Warden.
        [Test]
        public void TheEscapeLeavesTheMarkAwayFromTheWarden()
        {
            Floor(4f, -8f, out float markX, out float markY);
            Floor(1f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(markX, markY, markX, markY, Radius, wardenX, wardenY, false, out float gx, out float gy), Is.True);
            Assert.That(ArenaFloor.FloorDistanceSquared(gx, gy, markX, markY), Is.EqualTo(Radius * Radius).Within(1e-3f));
            Design(gx, gy, out float u, out float v);
            Assert.That(u, Is.GreaterThan(4f + Radius * .9f), "straight away from the Warden, to the east");
            Assert.That(Math.Abs(v + 8f), Is.LessThan(Radius * .45f));

            // The Warden on the mark: nothing to get away from, so the nearest way out.
            Floor(5f, -8f, out float heroX, out float heroY);
            Assert.That(FootworkRule.EscapeMark(heroX, heroY, markX, markY, Radius, markX, markY, false, out gx, out gy), Is.True);
            Design(gx, gy, out u, out v);
            Assert.That(u, Is.EqualTo(4f + Radius).Within(1e-3f));
            Assert.That(v, Is.EqualTo(-8f).Within(1e-3f));
        }

        // More than half a radius out, the hero keeps to its side of the mark rather than crossing it toward the far side.
        [Test]
        public void HalfwayOutTheEscapeKeepsToItsSideOfTheMark()
        {
            Floor(4f, -8f, out float markX, out float markY);
            Floor(2.2f, -8f, out float heroX, out float heroY);
            Floor(-2f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(heroX, heroY, markX, markY, Radius, wardenX, wardenY, false, out float gx, out float gy), Is.True);
            Design(gx, gy, out float u, out _);
            Assert.That(u, Is.LessThan(4f), "not across the mark to the east");
        }

        // A mark beside the east wall with the Warden to its west: the points away from the Warden lie off the floor or
        // against the wall, and the escape takes one with room round it.
        [Test]
        public void BesideAWallTheEscapeTakesAPointWithRoom()
        {
            Floor(12.4f, -8f, out float markX, out float markY);
            Floor(8f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(markX, markY, markX, markY, Radius, wardenX, wardenY, false, out float gx, out float gy), Is.True);
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin + FootworkRule.Room), Is.True);
            Assert.That(FoundryMap.Segment(markX, markY, gx, gy, HeroMotion.EdgeMargin), Is.True);
        }

        // South of the pit, pushed north at it: while the band glows the kite will not cross into it, as it did before.
        [Test]
        public void WhileTheBandGlowsTheKiteKeepsOutOfIt()
        {
            Floor(-10f, -2.2f, out float x, out float y);
            Floor(0f, 1f, out float px, out float py);
            Assert.That(PitEruptionRule.InBand(x, y), Is.False);
            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, false, out float gx, out float gy), Is.True);
            Assert.That(PitEruptionRule.InBand(gx, gy), Is.True, "without the glow it kites into the band");

            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, true, out gx, out gy), Is.True);
            for (int i = 1; i <= 8; i++)
                Assert.That(PitEruptionRule.InBand(x + (gx - x) * i / 8f, y + (gy - y) * i / 8f), Is.False, $"step {i}");
        }

        // A mark just south of the band with the Warden to its south: away from the Warden runs into the glowing band, and
        // the escape takes another way out.
        [Test]
        public void WhileTheBandGlowsTheEscapeTakesAWayOutOfTheMarkThatMissesIt()
        {
            Floor(-10f, -3.4f, out float markX, out float markY);
            Floor(-10f, -8f, out float wardenX, out float wardenY);
            Assert.That(FootworkRule.EscapeMark(markX, markY, markX, markY, Radius, wardenX, wardenY, false, out float gx, out float gy), Is.True);
            Assert.That(PitEruptionRule.InBand(gx, gy), Is.True, "without the glow, straight away: into the band");

            Assert.That(FootworkRule.EscapeMark(markX, markY, markX, markY, Radius, wardenX, wardenY, true, out gx, out gy), Is.True);
            for (int i = 1; i <= 8; i++)
                Assert.That(PitEruptionRule.InBand(markX + (gx - markX) * i / 8f, markY + (gy - markY) * i / 8f), Is.False, $"step {i}");
        }

        // On the open south rim the shelter is the rim spot beyond the band; west of the pit, where a yard brazier stands on
        // that spot and the old autopilot walked at it inside the band, it is a point out of the band the hero can reach.
        [Test]
        public void TheShelterLeavesTheBandWhereverTheRimSpotLies()
        {
            float depth = PitEruptionRule.Reach + 1.4f;
            Floor(-10f, 0f, out float x, out float y);
            Assert.That(PitEruptionRule.InBand(x, y), Is.True);
            Assert.That(FootworkRule.Shelter(x, y, depth, out float gx, out float gy), Is.True);
            FootworkRule.RimSpot(x, y, depth, out float rx, out float ry);
            Assert.That((gx, gy), Is.EqualTo((rx, ry)));
            Design(rx, ry, out float u, out float v);
            Assert.That(u, Is.EqualTo(-10f).Within(1e-3f));
            Assert.That(v, Is.EqualTo(FoundryMap.PitSouth - depth).Within(1e-3f));

            Floor(-17.5f, 2.5f, out x, out y);
            Assert.That(PitEruptionRule.InBand(x, y), Is.True);
            FootworkRule.RimSpot(x, y, depth, out rx, out ry);
            Assert.That(FoundryMap.Contains(rx, ry, HeroMotion.EdgeMargin), Is.False, "the brazier");
            Assert.That(FootworkRule.Shelter(x, y, depth, out gx, out gy), Is.True);
            Assert.That(PitEruptionRule.InBand(gx, gy), Is.False);
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.True);
            Assert.That(FoundryMap.Segment(x, y, gx, gy, HeroMotion.EdgeMargin), Is.True);
        }

        [Test]
        public void AStepIntoTheGlowingBandIsRefusedAndOneOutOfItIsNot()
        {
            Floor(-10f, -1.15f, out float outside, out float outsideY);
            Floor(-10f, 0f, out float inside, out float insideY);
            Floor(-10f, -6f, out float south, out float southY);
            Assert.That(PitEruptionRule.InBand(outside, outsideY), Is.False);
            Assert.That(FootworkRule.StepsIntoBand(outside, outsideY, inside, insideY), Is.True, "north, into the band");
            Assert.That(FootworkRule.StepsIntoBand(outside, outsideY, south, southY), Is.False, "south, away from it");
            Assert.That(FootworkRule.StepsIntoBand(inside, insideY, south, southY), Is.False, "out of it");
            Assert.That(FootworkRule.StepsIntoBand(inside, insideY, inside + 1f, insideY), Is.False, "already in it");
        }

        // A Tank's 1.7 outreaches the kite reach of 1.6: it is kited just past its own reach, as a Mite's 1.5 is; a reach
        // well inside the kite reach is kited at the kite reach; the gate boss with its wider margin, as before.
        [Test]
        public void EachEnemyIsKitedJustPastItsOwnReach()
        {
            const float kiteReach = 1.6f;
            Assert.That(FootworkRule.ThreatReach(kiteReach, 1.7f, false), Is.EqualTo(1.7f + FootworkRule.KiteMargin));
            Assert.That(FootworkRule.ThreatReach(kiteReach, 1.5f, false), Is.EqualTo(1.5f + FootworkRule.KiteMargin));
            Assert.That(FootworkRule.ThreatReach(kiteReach, 1.2f, false), Is.EqualTo(kiteReach));
            Assert.That(FootworkRule.ThreatReach(kiteReach, 1.7f, true), Is.EqualTo(1.7f + FootworkRule.GateKiteMargin));
            Assert.That(FootworkRule.ThreatReach(kiteReach, 1f, true), Is.EqualTo(kiteReach));
        }

        // The step-in (AutopilotCohort.Habit.StepsIn; docs/41, status 2026-11-10 night): the best weapon on one target is
        // the one with the most damage a second on it - the Daggers' 7 every 0.3 s with a double every third over the
        // Sword's 18 every 0.8 s, but on the gate boss the Sword's share counts - and the pilot holds the target at that
        // weapon's reach less the margin, in or out, only while the weapon is short and it is safe.
        [Test]
        public void ThePilotStepsInToItsShortBestWeaponsReachOnlyWhileSafe()
        {
            var daggers = new WeaponRuntime(7f, .3f, 1.8f, 0f, new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f));
            var sword = new WeaponRuntime(18f, .8f, 2.1f, 0f, new AttackPattern(WeaponBehavior.Cleave, 2f, .6f));
            Assert.That(FootworkRule.SingleTargetRate(daggers, false), Is.EqualTo(7f * (1f + 1f / 3f) / .3f).Within(1e-3f));
            Assert.That(FootworkRule.SingleTargetRate(sword, false), Is.EqualTo(18f / .8f).Within(1e-3f));
            sword.GateBossShare = 1.5f;
            Assert.That(FootworkRule.SingleTargetRate(sword, true), Is.EqualTo(18f / .8f * 1.5f).Within(1e-3f));

            var loadout = new WeaponLoadout(1);
            Assert.That(FootworkRule.BestSingleTargetReach(loadout, false), Is.Zero, "nothing held");
            Assert.That(loadout.TryAdd("weapon_sword", sword), Is.True);
            Assert.That(loadout.TryAdd("weapon_daggers", daggers), Is.True);
            Assert.That(FootworkRule.BestSingleTargetReach(loadout, false), Is.EqualTo(1.8f), "the Daggers' 31, the Sword's 22.5");
            Assert.That(FootworkRule.BestSingleTargetReach(loadout, true), Is.EqualTo(2.1f), "the Sword's 33.75 on the gate boss");

            // A Captain 3 units east of the hero: held 1.8 less the margin from it, on the hero's side.
            Floor(4f, -8f, out float x, out float y);
            Floor(7f, -8f, out float tx, out float ty);
            float hold = 1.8f - FootworkRule.StepInMargin;
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 1.8f, false, false, 80f, 100f, out float gx, out float gy), Is.True);
            Assert.That((float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(gx, gy, tx, ty)), Is.EqualTo(hold).Within(1e-4f));
            Assert.That(ArenaFloor.FloorDistanceSquared(gx, gy, x, y), Is.EqualTo((3f - hold) * (3f - hold)).Within(1e-3f), "in, straight at it");
            // Too near, it steps back out to the same ring.
            Floor(6f, -8f, out float nx, out float ny);
            Assert.That(FootworkRule.StepIn(nx, ny, tx, ty, 1.8f, false, false, 80f, 100f, out gx, out gy), Is.True);
            Assert.That((float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(gx, gy, tx, ty)), Is.EqualTo(hold).Within(1e-4f));

            // Not with a weapon that reaches the kited target anyway, nor far off, nor marked, low, or in the glow.
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 2.1f, false, false, 80f, 100f, out _, out _), Is.False, "the Sword");
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 0f, false, false, 80f, 100f, out _, out _), Is.False, "no weapon");
            Floor(10f, -8f, out float fx, out float fy);
            Assert.That(FootworkRule.StepIn(x, y, fx, fy, 1.8f, false, false, 80f, 100f, out _, out _), Is.False, "beyond StepInRange");
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 1.8f, true, false, 80f, 100f, out _, out _), Is.False, "a mark down");
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 1.8f, false, true, 80f, 100f, out _, out _), Is.False, "the band glows");
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 1.8f, false, false, 49f, 100f, out _, out _), Is.False, "under half health");
            Assert.That(FootworkRule.StepIn(x, y, tx, ty, 1.8f, false, false, 50f, 100f, out _, out _), Is.True, "at half");
        }

        // The Daggers land double on an elite room's Captain (EliteRule), so on it their share counts toward the best weapon
        // as the Sword's does on the gate boss: a Sword levelled to 30 every 0.8 s - 37.5 a second, over the Daggers' 31 on
        // anything else - stays under their 62 on a Captain, and the pilot steps in with the Daggers. Without the share the
        // Sword counted as its best weapon on the Captain, so it never stepped in and kited the Captain out of the Daggers'
        // reach.
        [Test]
        public void OnACaptainTheDaggersShareCountsTowardItsBestWeapon()
        {
            var daggers = new WeaponRuntime(7f, .3f, 1.8f, 0f, new AttackPattern(WeaponBehavior.DirectHit, 0f, 0f, 3, 2f));
            var sword = new WeaponRuntime(30f, .8f, 2.1f, 0f, new AttackPattern(WeaponBehavior.Cleave, 2f, .6f));
            var loadout = new WeaponLoadout(1);
            Assert.That(loadout.TryAdd(WardenRule.SwordId, sword), Is.True);
            Assert.That(loadout.TryAdd(EliteRule.DaggersId, daggers), Is.True);
            float daggersRate = 7f * (1f + 1f / 3f) / .3f;
            Assert.That(FootworkRule.SingleTargetRate(daggers, false, true), Is.EqualTo(daggersRate * EliteRule.DaggersShare).Within(1e-3f));
            Assert.That(FootworkRule.SingleTargetRate(sword, false, true), Is.EqualTo(30f / .8f).Within(1e-3f), "the Sword deals an elite its own");
            Assert.That(FootworkRule.SingleTargetRate(daggers, false), Is.EqualTo(daggersRate).Within(1e-3f), "on anything else, their own");

            Assert.That(FootworkRule.BestSingleTargetReach(loadout, false, true), Is.EqualTo(1.8f), "the Daggers' 62 on a Captain, the Sword's 37.5");
            Assert.That(FootworkRule.BestSingleTargetReach(loadout, false), Is.EqualTo(2.1f), "on anything else the Sword's 37.5, the Daggers' 31");
            Assert.That(FootworkRule.BestSingleTargetReach(loadout, true), Is.EqualTo(2.1f), "on the gate boss the Sword's 56.25");
        }

        // Nowhere to walk - a point the floor does not hold - and there is no escape.
        [Test]
        public void WithNoPointToWalkToThereIsNoEscape()
        {
            Floor(-10f, 8f, out float x, out float y);
            Assert.That(FoundryMap.Contains(x, y, HeroMotion.EdgeMargin), Is.False, "the pit's hole");
            Assert.That(FootworkRule.EscapeMark(x, y, x, y, .5f, x + 1f, y, false, out float gx, out float gy), Is.False);
            Assert.That((gx, gy), Is.EqualTo((x, y)));
        }
    }
}
