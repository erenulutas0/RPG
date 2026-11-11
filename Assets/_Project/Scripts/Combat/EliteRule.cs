using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Daggers are the elites' weapon (docs/41, status 2026-11-11; the lead's decision for the owner, docs/40 section
    // 4.1's Riftblade, "Elite/boss strong"): every landing of the Daggers on one of the foundry's elite enemies deals
    // DaggersShare of what it would - double, as their card says - as the Sword's does on the gate boss
    // (WardenRule.SwordShare): each weapon a target class. An elite enemy is the Grunt Captain an elite room
    // (RoomKind.Elite) is built round - the first enemy of its wave - and not its escort, the Grunt and the two Cinder
    // Mites beside it, as the Warden's job leaves floor 2's escort Mites apart. With the careful pilot stepping in on a
    // Captain when the Daggers are its best weapon on it (AutopilotCohort.Habit.StepsIn), the Daggers lead the gate's
    // elite job (FoundryGate's roles row). Measured, not chosen: change it only with a re-measurement. Nothing else of the
    // Daggers moves: the horde, the halls' waves, the escorts, the gate boss, and the hall arena's and the Descent's elite
    // rooms take their authored damage, and every other weapon deals an elite its own. The id is the WeaponDefinition's
    // (Data/Weapons/Weapon_Daggers.asset, whose description names the bonus). Pure.
    public static class EliteRule
    {
        public const string DaggersId = "weapon_daggers";
        public const float DaggersShare = 2f;

        // What a weapon's landings on an elite are multiplied by, by its id: DaggersShare for the Daggers, 1 for any other.
        // WeaponLoadout hands it to each weapon it takes up (WeaponRuntime.EliteShare).
        public static float EliteShareOf(string weaponId) => weaponId == DaggersId ? DaggersShare : 1f;

        // One landing of amount from a weapon whose elite share is share: on an elite times the share, on anything else the
        // amount. One body, never inlined, for WeaponRuntime.TryAttack, which the scene's AttackController and the foundry
        // simulation both strike through, so the two round the product alike.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DamageOn(float amount, float share, bool elite) => elite ? amount * share : amount;

        // Whether struck is one of the elites standing (EncounterController.Elites, the simulation's copy); none for a null
        // list.
        public static bool IsAmong(IDamageable struck, IReadOnlyList<IDamageable> elites)
        {
            if (elites == null || struck == null)
                return false;
            for (int i = 0; i < elites.Count; i++)
                if (ReferenceEquals(elites[i], struck))
                    return true;
            return false;
        }
    }
}
