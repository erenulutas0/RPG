using System;
using System.Collections.Generic;

namespace Cryptforge.Progression
{
    // The weapon roster (docs/63): each hero's own weapon - its signature, the weapon it starts every run with - is its
    // alone, and every other hero weapon is a shared one any hero may be handed by a card. A weapon is a signature exactly
    // when some hero of the roster starts with it, so the Daggers, whose hero (docs/46's Riftblade) is not on the roster
    // yet, stay shared until it joins and then leave the others' catalogues by the same rule (docs/63 section 3.3).
    //
    // Pure and one body for the scene's CombatSetup (its heroes' starting weapons, behind development/weapon-roster.txt)
    // and the simulation's FoundryRun (DescentSimulation.RosterSignatures, behind FoundryLevers.WeaponRoster), so a hero's
    // catalogue is the same list, in the same order, in both. Off - the shipped game - neither calls it.
    public static class WeaponRoster
    {
        // The two shared weapons docs/63 section 7 built behind the lever (docs/40 section 5.2), and the third, phase 2's
        // second crowd weapon (docs/63 section 13): an aura of saws round the hero.
        public const string MagmaMortarId = "weapon_magma_mortar";
        public const string ForgeBowId = "weapon_forge_bow";
        public const string CinderSawId = "weapon_cinder_saw";

        // Whether a run that starts with ownWeaponId may be handed weaponId by a card: its own weapon (for its levels and
        // its evolution), or any weapon that is no hero's signature. signatureIds are the roster heroes' starting weapons.
        public static bool Offers(string ownWeaponId, string weaponId, IReadOnlyList<string> signatureIds)
        {
            if (string.IsNullOrEmpty(ownWeaponId))
                throw new ArgumentException("A run starts with a weapon.", nameof(ownWeaponId));
            if (string.IsNullOrEmpty(weaponId))
                throw new ArgumentException("A weapon has an id.", nameof(weaponId));
            if (signatureIds == null)
                throw new ArgumentNullException(nameof(signatureIds));
            if (weaponId == ownWeaponId)
                return true;
            for (int i = 0; i < signatureIds.Count; i++)
                if (signatureIds[i] == weaponId)
                    return false;
            return true;
        }

        // The indices of weaponIds a run that starts with ownWeaponId draws its weapon cards from, in the order given: what
        // Offers keeps. The run's own weapon must be among them, since its levels are cards too.
        public static List<int> Catalogue(string ownWeaponId, IReadOnlyList<string> weaponIds, IReadOnlyList<string> signatureIds)
        {
            if (weaponIds == null)
                throw new ArgumentNullException(nameof(weaponIds));
            var kept = new List<int>(weaponIds.Count);
            bool own = false;
            for (int i = 0; i < weaponIds.Count; i++)
            {
                for (int j = 0; j < i; j++)
                    if (weaponIds[j] == weaponIds[i])
                        throw new ArgumentException($"Weapon {weaponIds[i]} is listed twice.", nameof(weaponIds));
                if (!Offers(ownWeaponId, weaponIds[i], signatureIds))
                    continue;
                kept.Add(i);
                own |= weaponIds[i] == ownWeaponId;
            }
            if (!own)
                throw new ArgumentException($"The run's own weapon {ownWeaponId} is not in the catalogue.", nameof(weaponIds));
            return kept;
        }
    }
}
