using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // docs/57's skills 2 and 3 (the owner's answers 2026-11-18): Iron Resolve and Shield Rush for the Vanguard, Flame Sigil
    // and Cinder Ward for Ilya. Their numbers live on their AbilityDefinition assets (damage, radius, cooldown, duration,
    // amount; AbilityRuntime); what every skill of a kind shares lives here, with the arithmetic the scene's
    // AbilityController and the foundry simulation both do - each product one body, never inlined (the stamina lesson,
    // docs/21), and every squared radius cast back to float before it is compared (PollenRule.Near: Mono kept r * r wider
    // than .NET). The constants below each name the section of docs/57 that set them; the assets' numbers are section 3's
    // and are measured in phase 2 before any ships.
    public static class SkillRule
    {
        // Shield Rush's daze (section 3.1): a struck enemy neither walks nor swings for this long, its weapon's clock still
        // running - a gate boss is never dazed, an elite is.
        public const float DazeSeconds = .5f;
        // With no finger down the Rush heads for the nearest enemy within this; with none, the hero's facing.
        public const float RushSeek = 6f;
        // Whether the Rush stops on impact (ImpactTravel): RushImpactGap short of the first enemy ahead on its path, its
        // strike reaching no farther; otherwise it carries the hero its whole length through what it meets. Phase 2
        // (2026-11-18, seeds 1-400): carried through, the Rush took the Vanguard past its target into the pit's band and the
        // Warden's reach, and its boss deaths rose in every cohort (careful 12 to 19, clumsy 65 to 85, the pit's 80 to 97);
        // a shield charge that stops at the first body it strikes ends in front of what it charged.
        public const bool RushStopsOnImpact = true;
        public const float RushImpactGap = .6f;
        // Flame Sigil (section 3.2): what a burn inside the glyph deals (x2), and the direct stacks an unburnt enemy inside
        // catches at each of the glyph's ticks (BurnRule.TickSeconds).
        public const float SigilHeat = 2f;
        public const int SigilStacks = 1;
        // Cinder Ward (section 3.2): the burn stacks every blow the ward absorbs gives its striker (direct ones).
        public const int WardStacks = 2;
        // Gate bosses are slowed by half the slow and never dazed (section 3.0): the Warden's and the Regent's telegraphs
        // stay the bosses' clocks.
        public const float GateBossSlowShare = .5f;
        // The share of the glyph's heat a gate boss takes: its burn inside deals 1 + (SigilHeat - 1) x this - half, as it
        // takes half the slow. Phase 2 (2026-11-18, seeds 1-400): the full heat took careful Ilya's floor-1 Warden from
        // 46.8 s to 45.2 s at P50, the band's edge; half of it reads 47.1 s.
        public const float GateBossHeatShare = .5f;

        // A skill's damage at the press (section 3.0): its base times the hero's first weapon's damage over that weapon's
        // base - what the Force tome and the damage lines have added. A weapon with no base deals the base. Burn is flat.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Scaled(float baseDamage, float weaponDamage, float weaponBase) =>
            weaponBase > 0f ? baseDamage * (weaponDamage / weaponBase) : baseDamage;

        // The slow a glyph puts on an enemy: its whole share, or half of it on a gate boss.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SlowOn(float slow, bool gateBoss) => gateBoss ? slow * GateBossSlowShare : slow;

        // The glyph's heat on an enemy: all of it, or share of what it adds on a gate boss (GateBossHeatShare).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float HeatOn(float heat, bool gateBoss, float share) => gateBoss ? 1f + (heat - 1f) * share : heat;

        // An enemy's step under a slow (PackMotion's stride): the share of it the slow keeps. A daze is a slow of 1.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Slowed(float stride, float slow) => stride * (1f - slow);

        // A ward's pool: share of the hero's own maximum health, never the arena's stretched one (section 3.0).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float WardOf(float maximum, float share) => maximum * share;

        // Whether a floor point lies within radius of the path from (fromX, fromY) to (toX, toY): the Rush's capsule. The
        // point's squared floor distance to the nearest point of the segment against the squared radius cast to float.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool OnPath(float x, float y, float fromX, float fromY, float toX, float toY, float radius)
        {
            float px = toX - fromX;
            float py = toY - fromY;
            float lengthSquared = px * px + py * py;
            float t = 0f;
            if (lengthSquared > 0f)
            {
                t = ((x - fromX) * px + (y - fromY) * py) / lengthSquared;
                t = Math.Max(0f, Math.Min(1f, t));
            }
            float nearestX = fromX + px * t;
            float nearestY = fromY + py * t;
            return ArenaFloor.FloorDistanceSquared(x, y, nearestX, nearestY) <= (float)(radius * radius);
        }

        // How far along a heading (a unit vector) from (fromX, fromY) a floor point lies: the Rush's order of impact.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Along(float x, float y, float fromX, float fromY, float headingX, float headingY) =>
            (x - fromX) * headingX + (y - fromY) * headingY;

        // The travel of a Rush that stops on impact: its length, or RushImpactGap short of the first enemy on its path (at
        // firstAlong; +infinity for none), never back.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float ImpactTravel(float firstAlong, float length) =>
            Math.Max(0f, Math.Min(length, firstAlong - RushImpactGap));

        // Whether a floor point lies within radius of a centre: the glyph's and the crowd counts' circle.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Within(float x, float y, float centreX, float centreY, float radius) =>
            ArenaFloor.FloorDistanceSquared(x, y, centreX, centreY) <= (float)(radius * radius);
    }
}
