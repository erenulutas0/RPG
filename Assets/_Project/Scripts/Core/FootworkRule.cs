using System;
using System.Runtime.CompilerServices;
using Cryptforge.Art;
using Cryptforge.Combat;

namespace Cryptforge.Core
{
    // How the development autopilot steps in a fight on the foundry: the arithmetic DevelopmentAutopilot and the Descent
    // simulation's copy of it (DescentSimulation.RunFoundry) share, as one body, so the scene and the simulation choose the
    // same step on the same frame (the reason ArenaFloor.FloorDistanceSquared is one body, docs/21).
    public static class FootworkRule
    {
        // The points round the Warden's mark an escape chooses among.
        public const int EscapePoints = 16;
        // An escape point's walk, as a share of the escape's radius, counts this much against it: twice the whole range of
        // the preference for away from the Warden (from straight away to straight at it, -1..1), so once the hero is half
        // a radius out it keeps to its side of the mark rather than turning back across it.
        public const float EscapeWalkWeight = 2f;
        // An escape point with no room round it counts this much against it, on the same scale.
        public const float EscapeRoomPenalty = .5f;
        // Room: this much more clearance than the hero's own edge margin, in floor units.
        public const float Room = 1f;
        // An escape point across the pit's glowing band counts this much against it, on the same scale: a burn costs less
        // than the hammer, so the band is crossed only when nothing else leaves the mark in time.
        public const float EscapeBandPenalty = 4f;
        // How far ahead a step is read for the pit's band while it glows, in floor units: more than a frame's walk.
        public const float BandLookahead = .3f;
        // Where a shelter looks when its spot off the rim is not on the floor: rings this far apart round the hero, out to
        // ShelterRings of them, EscapePoints points a ring.
        public const float ShelterStep = 1.5f;
        public const int ShelterRings = 4;
        // How far past an enemy's own reach the autopilot already kites from it: the gate boss's hammer outreaches a
        // grunt's, and kiting at a grunt's distance left the hero inside every swing; kiting every enemy that wide starved
        // the hero of drops (docs/41, 2026-10-26), so every other enemy is kited just past its own reach - a Tank's 1.7
        // stood beyond the autopilot's kite reach of 1.6, and it struck a hero who stood inside it.
        public const float GateKiteMargin = .3f;
        public const float KiteMargin = .15f;

        // How near an enemy whose weapon reaches enemyReach comes before the autopilot kites from it: its reach and the
        // margin, or kiteReach where that is longer.
        public static float ThreatReach(float kiteReach, float enemyReach, bool gateBoss) =>
            Math.Max(kiteReach, enemyReach + (gateBoss ? GateKiteMargin : KiteMargin));

        // A kite stride ahead of (x, y): the push (a unit vector away from what presses the hero) turned by turn, as the
        // autopilot circles a crowd; or, where that runs into a wall or a work island - or, while the pit glows
        // (avoidBand), across its band - the first of the push turned the other way, then twice and three times turn
        // either way, that the hero can stand on and walk to in a straight line: so it slides along the wall or turns away
        // from it rather than standing against it. False when no turn has room.
        public static bool Kite(float x, float y, float pushX, float pushY, float turn, float stride, bool avoidBand,
            out float gx, out float gy)
        {
            for (int i = 0; i < 6; i++)
            {
                float angle = turn * (i / 2 + 1) * (i % 2 == 0 ? 1f : -1f);
                float cos = (float)Math.Cos(angle);
                float sin = (float)Math.Sin(angle);
                gx = x + (pushX * cos - pushY * sin) * stride;
                gy = y + (pushX * sin + pushY * cos) * stride;
                if (Clear(x, y, gx, gy) && !(avoidBand && BandOnWay(x, y, gx, gy)))
                    return true;
            }
            gx = x;
            gy = y;
            return false;
        }

        // The kite's stride before 2026-11-04, kept for the clumsy cohorts (AutopilotCohort): the push turned by turn, a
        // stride on, whatever stands there - off the floor, a work island, the glowing band. Against a wall the hero held
        // the stick into it and stood. Kite's first turn, unchecked.
        public static void KiteStraight(float x, float y, float pushX, float pushY, float turn, float stride, out float gx,
            out float gy)
        {
            float cos = (float)Math.Cos(turn);
            float sin = (float)Math.Sin(turn);
            gx = x + (pushX * cos - pushY * sin) * stride;
            gy = y + (pushX * sin + pushY * cos) * stride;
        }

        // The escape from the Warden's mark before 2026-11-04, kept for the clumsy cohorts: of eight points radius out from
        // the mark that the hero can stand on and walk to in a straight line, the nearest, whichever side the Warden stands
        // and whatever stands round it. False when no point can be walked to.
        public static bool EscapeMarkNearest(float x, float y, float markX, float markY, float radius, out float gx,
            out float gy)
        {
            gx = x;
            gy = y;
            float best = float.PositiveInfinity;
            for (int k = 0; k < 8; k++)
            {
                float a = k * 3.14159274f / 4f;
                float px = markX + (float)Math.Cos(a) * radius;
                float py = markY + (float)Math.Sin(a) * radius;
                if (!Clear(x, y, px, py))
                    continue;
                float d = ArenaFloor.FloorDistanceSquared(x, y, px, py);
                if (d >= best)
                    continue;
                best = d;
                gx = px;
                gy = py;
            }
            return !float.IsPositiveInfinity(best);
        }

        // The escape from the Warden's mark: of EscapePoints points radius out from the mark that the hero can stand on
        // and walk to in a straight line, the one farthest round from the Warden (wardenX, wardenY) less its walk as a share
        // of radius, one without room round it - or, while the pit glows (avoidBand), across its band - counting against
        // it. With the Warden on the mark, the nearest. False when no point can be walked to.
        public static bool EscapeMark(float x, float y, float markX, float markY, float radius, float wardenX,
            float wardenY, bool avoidBand, out float gx, out float gy)
        {
            gx = x;
            gy = y;
            float awayX = markX - wardenX;
            float awayY = markY - wardenY;
            Normalize(ref awayX, ref awayY);
            float best = float.NegativeInfinity;
            for (int k = 0; k < EscapePoints; k++)
            {
                float angle = k * (2f * 3.14159274f / EscapePoints);
                float dx = (float)Math.Cos(angle);
                float dy = (float)Math.Sin(angle);
                float px = markX + dx * radius;
                float py = markY + dy * radius;
                if (!Clear(x, y, px, py))
                    continue;
                float score = dx * awayX + dy * awayY -
                              EscapeWalkWeight * (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(px, py, x, y)) / radius;
                if (!FoundryMap.Contains(px, py, HeroMotion.EdgeMargin + Room))
                    score -= EscapeRoomPenalty;
                if (avoidBand && BandOnWay(x, y, px, py))
                    score -= EscapeBandPenalty;
                if (score <= best)
                    continue;
                best = score;
                gx = px;
                gy = py;
            }
            return !float.IsNegativeInfinity(best);
        }

        // The point depth design units out from the pit's hole on the side the hero stands: where the autopilot draws a
        // node fight (inside the band) and where it waits out a spill (beyond it).
        public static void RimSpot(float x, float y, float depth, out float gx, out float gy)
        {
            FoundryMap.ToDesign(x, y, out float u, out float v);
            float cu = Clamp(u, FoundryMap.PitWest, FoundryMap.PitEast);
            float cv = Clamp(v, FoundryMap.PitSouth, FoundryMap.PitNorth);
            float awayX = u - cu;
            float awayY = v - cv;
            if (awayX * awayX + awayY * awayY < 1e-4f)
            {
                awayX = 0f;
                awayY = -1f;
            }
            Normalize(ref awayX, ref awayY);
            FoundryMap.ToFloor(cu + awayX * depth, cv + awayY * depth, out gx, out gy);
        }

        // Out of the pit's band from (x, y): the rim spot depth out on the hero's side, where the floor holds it; where it
        // does not - a work island or the north wall stands there, and the hero walked at it and stayed in the band - the
        // point farthest out of the band on the nearest ring round the hero that has one the hero can stand on and walk to
        // in a straight line. False, with the rim spot, when no ring within ShelterRings has one.
        public static bool Shelter(float x, float y, float depth, out float gx, out float gy)
        {
            RimSpot(x, y, depth, out gx, out gy);
            if (FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin))
                return true;
            for (int ring = 1; ring <= ShelterRings; ring++)
            {
                float radius = ring * ShelterStep;
                float best = PitEruptionRule.Reach;
                bool found = false;
                for (int k = 0; k < EscapePoints; k++)
                {
                    float angle = k * (2f * 3.14159274f / EscapePoints);
                    float px = x + (float)Math.Cos(angle) * radius;
                    float py = y + (float)Math.Sin(angle) * radius;
                    float outside = PitEruptionRule.DistanceOutside(px, py);
                    if (outside <= best || !Clear(x, y, px, py))
                        continue;
                    best = outside;
                    gx = px;
                    gy = py;
                    found = true;
                }
                if (found)
                    return true;
            }
            return false;
        }

        // Whether a step from (x, y) toward (towardX, towardY) would take a hero now outside the pit's band into it: while
        // the band glows the autopilot does not take it, and waits at the band's edge for the spill instead.
        public static bool StepsIntoBand(float x, float y, float towardX, float towardY)
        {
            float dx = towardX - x;
            float dy = towardY - y;
            Normalize(ref dx, ref dy);
            return !PitEruptionRule.InBand(x, y) &&
                   PitEruptionRule.InBand(x + dx * BandLookahead, y + dy * BandLookahead);
        }

        // Stepping in (AutopilotCohort.Habit.StepsIn; docs/41, status 2026-11-10 night): the careful pilot kites a Grunt
        // Captain from 1.75 units and the Warden from 2.0, and the Daggers reach 1.8, so a pilot holding them as its best
        // weapon against that one target fought it out of its own reach, where a player closes in. A pilot with the habit,
        // safe, holds the target at its best weapon's reach less StepInMargin instead of kiting. A short weapon reaches less
        // than ShortReach (the Daggers; the Sword's 2.1 and the Staff's 2.4 already reach a kited target); the target is
        // the gate boss or an elite room's Captain within StepInRange; safe is no mark down on the floor, the hero at
        // StepInHealth of its maximum or more and the pit's band not glowing.
        public const float ShortReach = 2f;
        public const float StepInMargin = .15f;
        public const float StepInRange = 5f;
        public const float StepInHealth = .5f;
        // How near the hold point the hero stops walking.
        public const float StepInStop = .1f;

        // A weapon's damage a second on one target - its strike, its fixed rhythm's crits and its rolled crits, on the gate
        // boss its share (WeaponRuntime.GateBossShare), on an elite its share (WeaponRuntime.EliteShare: the Daggers' 2 on
        // an elite room's Captain) - for choosing the weapon a pilot fights one target with.
        public static float SingleTargetRate(WeaponRuntime weapon, bool gateBoss, bool elite = false)
        {
            AttackPattern pattern = weapon.Pattern;
            float rhythm = pattern.CritEvery > 0 ? (pattern.CritMultiplier - 1f) / pattern.CritEvery : 0f;
            float rolled = Math.Min(1f, weapon.CritChance);
            float rate = weapon.Damage * (1f + rhythm + rolled) / weapon.Interval;
            return gateBoss ? rate * weapon.GateBossShare : elite ? rate * weapon.EliteShare : rate;
        }

        // The reach of the loadout's best weapon on one target - the gate boss, an elite or neither (SingleTargetRate, the
        // earlier slot on a tie); 0 for none.
        public static float BestSingleTargetReach(WeaponLoadout loadout, bool gateBoss, bool elite = false)
        {
            float best = -1f, reach = 0f;
            for (int i = 0; loadout != null && i < loadout.Count; i++)
            {
                WeaponRuntime weapon = loadout.At(i).Runtime;
                float rate = SingleTargetRate(weapon, gateBoss, elite);
                if (rate <= best)
                    continue;
                best = rate;
                reach = weapon.Range;
            }
            return reach;
        }

        // Where the pilot at (x, y) steps to against the target at (tx, ty), whose best weapon on it reaches bestReach: the
        // point StepInMargin inside that reach on the hero's side of the target - in, or back out to it. False, and no step,
        // when the weapon is not short, the target is beyond StepInRange, or it is not safe (marked, health under
        // StepInHealth, glowing).
        public static bool StepIn(float x, float y, float tx, float ty, float bestReach, bool marked, bool glowing,
            float health, float maximum, out float gx, out float gy)
        {
            gx = x;
            gy = y;
            if (bestReach <= 0f || bestReach >= ShortReach || marked || glowing || maximum <= 0f || health < maximum * StepInHealth)
                return false;
            float d = ArenaFloor.FloorDistanceSquared(x, y, tx, ty);
            if (d > StepInRange * StepInRange)
                return false;
            float awayX = x - tx, awayY = y - ty;
            Normalize(ref awayX, ref awayY);
            if (awayX == 0f && awayY == 0f)
                awayY = -1f;
            float hold = bestReach - StepInMargin;
            gx = tx + awayX * hold;
            gy = ty + awayY * hold;
            if (FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin))
                return true;
            gx = x;
            gy = y;
            return d <= hold * hold;
        }

        // The flank (docs/50 section 5; the lead's decisions 2026-11-12): against the Mirror Sentry (MirrorGuardRule), whose
        // mirror blunts and reflects a landing made from its front, the step-in's hold point (gx, gy) is turned round the
        // target at (tx, ty) toward its rear - from where the pilot at (x, y) stands, by at most FlankStepDegrees, at the
        // hold point's own reach - so the pilot circles it at the short weapon's reach rather than holding still before
        // it. At 3.5 units a second and the Daggers' 1.65 the walk round it is about 121 degrees a second against the
        // mirror's 45. Where the floor does not hold the turned point the step-in's stands. facing: the target's mirror,
        // in radians (MirrorGuardState.Facing). One body for DevelopmentAutopilot and the foundry simulation, as StepIn.
        public const float FlankStepDegrees = 40f;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Flank(float x, float y, float tx, float ty, float facing, ref float gx, ref float gy)
        {
            float r = (float)Math.Sqrt(ArenaFloor.FloorDistanceSquared(gx, gy, tx, ty));
            float at = (float)Math.Atan2(y - ty, x - tx);
            float rear = MirrorGuardRule.Wrap(facing + (float)Math.PI);
            float delta = MirrorGuardRule.Wrap(rear - at);
            const float stepMax = FlankStepDegrees * (float)Math.PI / 180f;
            if (delta > stepMax)
                delta = stepMax;
            else if (delta < -stepMax)
                delta = -stepMax;
            float fx = tx + r * (float)Math.Cos(at + delta);
            float fy = ty + r * (float)Math.Sin(at + delta);
            if (!FoundryMap.Contains(fx, fy, HeroMotion.EdgeMargin))
                return;
            gx = fx;
            gy = fy;
        }

        // A goal the hero can stand on and walk to in a straight line.
        private static bool Clear(float x, float y, float tx, float ty) =>
            FoundryMap.Contains(tx, ty, HeroMotion.EdgeMargin) && FoundryMap.Segment(x, y, tx, ty, HeroMotion.EdgeMargin);

        // The pit's band anywhere along the way from (x, y) to (tx, ty), read at its quarters.
        private static bool BandOnWay(float x, float y, float tx, float ty)
        {
            for (int i = 1; i <= 4; i++)
            {
                float t = i * .25f;
                if (PitEruptionRule.InBand(x + (tx - x) * t, y + (ty - y) * t))
                    return true;
            }
            return false;
        }

        private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;

        // UnityEngine.Vector2.Normalize: divided by its magnitude, or zero below Vector2.kEpsilon.
        private static void Normalize(ref float x, ref float y)
        {
            float magnitude = (float)Math.Sqrt(x * x + y * y);
            if (magnitude > 1e-5f)
            {
                x /= magnitude;
                y /= magnitude;
            }
            else
            {
                x = 0f;
                y = 0f;
            }
        }
    }
}
