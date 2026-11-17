using System;

namespace Cryptforge.Progression
{
    // What a piece of gear can carry: the slots' main stats and the efsun line pool (docs/54 section 6.1; gold find dropped
    // by the lead, 2026-11-16). Its own enum rather than UpgradeStat's, so nothing that switches on UpgradeStat moved: the
    // run reads gear through GearStats.RunStat and Apply (docs/54 phase 5), which map these onto the run's stats in one body
    // and leave inert the five the run has no rule for. Append, never reorder; the save writes the id.
    public enum GearStat
    {
        Damage = 0,
        AttackSpeed = 1,
        CritChance = 2,
        MaxHealth = 3,
        Armour = 4,
        EliteDamage = 5,
        BurnDamage = 6,
        Area = 7,
        MoveSpeed = 8,
        PickupRadius = 9,
        AbilityCooldown = 10,
        Healing = 11,
        Luck = 12
    }

    // The stable text a stat is saved and logged as. The enum's names may be refactored; these may not.
    public static class GearStatIds
    {
        public const int Count = 13;

        private static readonly string[] Ids =
        {
            "damage", "attack_speed", "crit", "health", "armour", "elite_damage", "burn_damage", "area", "move_speed",
            "pickup_radius", "ability_cooldown", "healing", "luck"
        };

        public static string Id(GearStat stat)
        {
            int index = (int)stat;
            if (index < 0 || index >= Ids.Length)
                throw new ArgumentOutOfRangeException(nameof(stat));
            return Ids[index];
        }

        // False for an empty, unknown or retired id (a saved line of gold find, say), which the save then drops.
        public static bool TryParse(string id, out GearStat stat)
        {
            for (int i = 0; i < Ids.Length; i++)
            {
                if (string.Equals(Ids[i], id, StringComparison.Ordinal))
                {
                    stat = (GearStat)i;
                    return true;
                }
            }
            stat = GearStat.Damage;
            return false;
        }
    }
}
