using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The Daggers are the elites' weapon (docs/41, status 2026-11-10 night): every landing of the Daggers on one of the
    // foundry's elites deals DaggersShare of what it would, as the Sword's does on the gate boss (WardenRule.SwordShare).
    // An elite is the Grunt Captain an elite room (RoomKind.Elite) is built round - the first enemy of its wave - and not
    // its escort, the Grunt and the two Cinder Mites beside it, as the Warden's job leaves floor 2's escort Mites apart.
    // With the Staff the horde's weapon and the Sword the Warden's, the V0.1 gate's roles row reads a third job, the
    // elites, and the Daggers lead it: 12.5 a second to the Staff's 10.0 frame-perfect, 11.6 to 9.4 with a human's hands
    // (P50 of 200 seeds, the Vanguard started with each; FoundryBatch.DaggersLevers). The careful pilot kites a Captain at
    // its reach and a little more (1.75) and the Daggers reach 1.8, so they touch it seldom and late, after the Staff's
    // splash and the Sword's cleave; a share of 12 makes the touch a kill. Measured, not chosen: change it only with a
    // re-measurement. Nothing else of the Daggers moves: the horde, the halls' waves, the escorts, the gate boss, and the
    // hall arena's and the Descent's elite rooms take their authored damage, and every other weapon deals an elite its
    // own. The id is the WeaponDefinition's (Data/Weapons/Weapon_Daggers.asset). Pure.
    public static class EliteRule
    {
        public const string DaggersId = "weapon_daggers";
        public const float DaggersShare = 12f;

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
