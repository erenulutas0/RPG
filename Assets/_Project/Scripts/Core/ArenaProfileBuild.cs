using System.Collections.Generic;
using Cryptforge.Arena;
using Cryptforge.Progression;

namespace Cryptforge.Core
{
    // The thin bridge from the profile to the arena (docs/55 P0): the build the player fights with, read from the profile
    // and never written to it - the arena keeps its own save (ArenaSaveData), so the profile, its save and its version are
    // untouched. Ranked takes the identity (the hero, the weapon it carries, its three skills, the policy) and the line
    // types the player picked; open takes the same identity and the gear the profile wears with that weapon, stored as ids
    // and levels (GearLoadout's pieces: the weapon's own gear and the equipped armour, amulet and ring).
    public static class ArenaProfileBuild
    {
        // The weapon the hero fights with: the one asked for when the hero carries it and the profile has it (the starting
        // weapon always), else the profile's equipped weapon when the hero carries it, else the hero's starting weapon.
        public static string WeaponOf(PlayerProfile profile, string heroId, string weaponId = null, ArenaRules rules = null)
        {
            rules = rules ?? ArenaRules.Current;
            ArenaHero hero = rules.Hero(heroId);
            if (hero == null || profile == null)
                return hero?.StartingWeaponId;
            if (Usable(profile, hero, weaponId))
                return weaponId;
            if (Usable(profile, hero, profile.EquippedWeaponId))
                return profile.EquippedWeaponId;
            return hero.StartingWeaponId;
        }

        public static ArenaBuild Ranked(PlayerProfile profile, string heroId, IEnumerable<ArenaStat> lineTypes,
            ArenaPolicy policy = ArenaPolicy.Careful, string weaponId = null, ArenaRules rules = null)
        {
            rules = rules ?? ArenaRules.Current;
            ArenaHero hero = Hero(heroId, rules);
            return ArenaBuild.Ranked(hero.Id, WeaponOf(profile, hero.Id, weaponId, rules), hero.SkillIds, policy, lineTypes,
                rules.Version);
        }

        public static ArenaBuild Open(PlayerProfile profile, string heroId, ArenaPolicy policy = ArenaPolicy.Careful,
            string weaponId = null, ArenaRules rules = null)
        {
            rules = rules ?? ArenaRules.Current;
            ArenaHero hero = Hero(heroId, rules);
            string weapon = WeaponOf(profile, hero.Id, weaponId, rules);
            return ArenaBuild.Open(hero.Id, weapon, hero.SkillIds, policy, Gear(profile, weapon), rules.Version);
        }

        // The profile's gear for a run with this weapon, as the arena stores it: slot, + level, and each line's stat id
        // and tier (a line whose id the arena does not know is dropped).
        public static List<ArenaGearPiece> Gear(PlayerProfile profile, string weaponId)
        {
            var pieces = new List<ArenaGearPiece>(4);
            if (profile == null)
                return pieces;
            foreach (ItemInstance item in profile.Gear.EquippedPieces(weaponId))
            {
                var lines = new List<ArenaGearLine>(item.Lines.Count);
                foreach (EfsunLine line in item.Lines)
                {
                    if (ArenaGearTable.TryParse(GearStatIds.Id(line.Stat), out ArenaStat stat))
                        lines.Add(new ArenaGearLine(stat, line.Tier));
                }
                pieces.Add(new ArenaGearPiece((int)item.Slot, item.Level, lines));
            }
            return pieces;
        }

        private static ArenaHero Hero(string heroId, ArenaRules rules) =>
            rules.Hero(heroId) ?? rules.Hero(ArenaRules.Vanguard);

        private static bool Usable(PlayerProfile profile, ArenaHero hero, string weaponId) =>
            !string.IsNullOrEmpty(weaponId) && hero.Carries(weaponId) &&
            (weaponId == hero.StartingWeaponId || profile.OwnsWeapon(weaponId));
    }
}
