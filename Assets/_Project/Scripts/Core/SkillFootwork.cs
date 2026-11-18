using Cryptforge.Combat;

namespace Cryptforge.Core
{
    // When the development autopilot presses docs/57's skills 2 and 3 (section 3's autopilot rules, section 6.1): one body
    // for DevelopmentAutopilot and the foundry simulation's pilot, as FootworkRule is for the footwork. A cohort that times
    // the skills (AutopilotCohort.Habit.TimesTheSkills: careful, standing, first-card) reads the moments below; one without
    // it (clumsy) presses each on cooldown whenever an enemy stands within the skill's reach - the Rush toward the nearest,
    // into the crowd. The auto-farmer presses nothing (no UsesTheAbility). Skill 1 keeps DevelopmentAutopilot's own rule.
    // The inputs are counts and clocks the caller reads off its floor; nothing here looks at a scene.
    public static class SkillFootwork
    {
        // Iron Resolve: a blow taken this recently is the one the guard answers; a blow after FightGap seconds without one
        // opens a fight, and its first blow with GuardCrowd or more enemies within CrowdReach is guarded; so is any blow that
        // leaves the hero under Low of its maximum.
        public const float BlowMemory = 1f;
        public const float FightGap = 1.5f;
        public const float CrowdReach = 2f;
        public const int GuardCrowd = 3;
        public const float Low = .7f;
        // Shield Rush: out of a surround - EscapeCrowd or more within EscapeReach and the hero under EscapeHealth - along the
        // kite's heading; onto a lone target - an elite room's Captain beyond the first weapon's reach and within the Rush's
        // length with nothing else within LoneClear of the hero, or the gate boss in its open window the same way.
        public const int EscapeCrowd = 5;
        public const float EscapeReach = 1.5f;
        public const float EscapeHealth = .6f;
        public const float LoneClear = 3f;
        // Flame Sigil: SigilCrowd or more within the glyph's radius, or the gate boss within it.
        public const int SigilCrowd = 4;

        // A blow the guard answers, taken within BlowMemory: the first of a fight with a crowd at hand, or one that left the
        // hero low.
        public static bool GuardsTheBlow(bool firstOfAFight, int withinCrowdReach, float health, float maximum) =>
            (firstOfAFight && withinCrowdReach >= GuardCrowd) || health < maximum * Low;

        // The ward's reactive moment away from the boss: a blow that left the hero low.
        public static bool WardsTheBlow(float health, float maximum) => health < maximum * Low;

        // Whether the gate boss's blow, telegraphProgress through a telegraph of telegraphSeconds, lands within window of a
        // press made now: the press takes effect at once in the scene and with the next frame in the simulation, so a frame
        // is kept in hand (RiftbladeRules.BlowDueWithinGuard's reading, docs/46).
        public static bool BlowDueWithin(float telegraphProgress, float telegraphSeconds, float window, float frame) =>
            (1f - telegraphProgress) * telegraphSeconds <= window - frame;

        public static bool Surrounded(int withinEscapeReach, float health, float maximum) =>
            withinEscapeReach >= EscapeCrowd && health < maximum * EscapeHealth;

        // A lone target worth the Rush at distance: beyond the first weapon's reach, within the Rush's length.
        public static bool WorthTheRush(float distance, float weaponReach, float rushLength) =>
            distance > weaponReach && distance <= rushLength;

        public static bool DrawsTheSigil(int withinGlyph, bool gateBossWithinGlyph) =>
            withinGlyph >= SigilCrowd || gateBossWithinGlyph;

        // The glyph's lure (section 3.2: the Lures habit's body, with the glyph as the rim): while it lives, a node fight with
        // nothing at the kite's reach is held across it - the hero on the glyph's rim on the far side from the crowd, which
        // must wade through the glyph to reach it. The crowd is the mean of the enemies within LureReach of the hero; false
        // with none, or with the crowd standing on the glyph's centre.
        public const float LureReach = 5f;

        public static bool AcrossTheGlyph(float glyphX, float glyphY, float radius, float crowdX, float crowdY, out float gx,
            out float gy)
        {
            float dx = glyphX - crowdX, dy = glyphY - crowdY;
            float length = (float)System.Math.Sqrt(dx * dx + dy * dy);
            gx = glyphX;
            gy = glyphY;
            if (!(length > 1e-3f))
                return false;
            gx = glyphX + dx / length * radius;
            gy = glyphY + dy / length * radius;
            return true;
        }

        // The clumsy rule: a ready skill pressed whenever an enemy stands within its reach.
        public static bool PressesOnCooldown(int withinReach) => withinReach > 0;

        // The reach a skill's clumsy press and its crowd read: the guard's and the ward's CrowdReach, the glyph's radius, the
        // Rush's seek range (SkillRule.RushSeek).
        public static float ReachOf(AbilityRuntime skill) =>
            skill.Kind == AbilityKind.Sigil ? skill.Radius
            : skill.Kind == AbilityKind.Rush ? SkillRule.RushSeek
            : CrowdReach;
    }
}
