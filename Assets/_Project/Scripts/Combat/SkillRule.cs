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
        // Flame Sigil (section 3.2): what a burn inside the glyph deals (x2), and the direct stacks an unburnt enemy inside
        // catches at each of the glyph's ticks (BurnRule.TickSeconds).
        public const float SigilHeat = 2f;
        public const int SigilStacks = 1;
        // Cinder Ward (section 3.2): the burn stacks every blow the ward absorbs gives its striker (direct ones).
        public const int WardStacks = 2;
        // Gate bosses are slowed by half the slow and never dazed (section 3.0): the Warden's and the Regent's telegraphs
        // stay the bosses' clocks.
        public const float GateBossSlowShare = .5f;

        // A skill's damage at the press (section 3.0): its base times the hero's first weapon's damage over that weapon's
        // base - what the Force tome and the damage lines have added. A weapon with no base deals the base. Burn is flat.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float Scaled(float baseDamage, float weaponDamage, float weaponBase) =>
            weaponBase > 0f ? baseDamage * (weaponDamage / weaponBase) : baseDamage;

        // The slow a glyph puts on an enemy: its whole share, or half of it on a gate boss.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float SlowOn(float slow, bool gateBoss) => gateBoss ? slow * GateBossSlowShare : slow;

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

        // Whether a floor point lies within radius of a centre: the glyph's and the crowd counts' circle.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Within(float x, float y, float centreX, float centreY, float radius) =>
            ArenaFloor.FloorDistanceSquared(x, y, centreX, centreY) <= (float)(radius * radius);
    }
}
