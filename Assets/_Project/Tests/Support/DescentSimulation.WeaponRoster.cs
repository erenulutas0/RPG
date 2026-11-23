using System.Collections.Generic;
using Cryptforge.Progression;

namespace Cryptforge.Tests
{
    // docs/63's weapon roster in the foundry simulation (2026-11-23): each hero's own weapon kept to that hero, and the
    // shared pool - the Daggers until the Riftblade ships, the Magma Mortar and the Forge Bow - for every hero, as the scene
    // plays it behind development/weapon-roster.txt (CombatSetup's roster catalogue). Off by default
    // (FoundryLevers.WeaponRoster), so every shipped number stays what it was: nothing here runs unless a run asks for it.
    internal static partial class DescentSimulation
    {
        public const string StaffId = "weapon_staff";

        // The roster heroes' own weapons, as CombatSetup reads them from its heroes (Hero_Vanguard's Sword,
        // Hero_CinderArcanist's Staff); FoundrySimulationSceneValueTests holds it to the scene. The Daggers join it the day
        // the Riftblade joins the scene's heroes (docs/63 section 3.3).
        public static readonly string[] RosterSignatures = { SwordId, StaffId };

        // Every hero weapon in the scene's order: CombatSetup._weapons (Sword, Staff, Daggers), then _rosterWeapons
        // (Magma Mortar, Forge Bow, and phase 2's Cinder Saw, docs/63 section 13).
        public static HeroWeapon[] RosterWeapons() => new[] { Sword(), Staff(), Daggers(), MagmaMortar(), ForgeBow(), CinderSaw() };

        // The weapons a run that starts with ownWeaponId draws its cards from under the roster: WeaponRoster's body over
        // RosterWeapons and RosterSignatures, in their order - the scene's CombatSetup calls the same body. The research may
        // hand another pool or other signatures (FoundryLevers.RosterPool, RosterSignatureIds); null is the roster's own.
        public static HeroWeapon[] RosterCatalogue(string ownWeaponId, HeroWeapon[] pool = null, string[] signatures = null)
        {
            HeroWeapon[] all = pool ?? RosterWeapons();
            var ids = new string[all.Length];
            for (int i = 0; i < all.Length; i++)
                ids[i] = all[i].Id;
            List<int> kept = WeaponRoster.Catalogue(ownWeaponId, ids, signatures ?? RosterSignatures);
            var catalogue = new HeroWeapon[kept.Count];
            for (int i = 0; i < catalogue.Length; i++)
                catalogue[i] = all[kept[i]];
            return catalogue;
        }

        internal sealed partial class FoundryLevers
        {
            // docs/63: a run's weapon cards come from RosterCatalogue (the hero's own weapon, then the shared pool, another
            // hero's own never) rather than WeaponCatalogue (every hero weapon for every hero, as shipped). Off by default.
            public bool WeaponRoster;
            // Research only (WeaponRosterResearch's decomposition and candidates): the weapons the roster draws from in
            // place of RosterWeapons (a pool without the new two, or with their numbers tuned), and the signatures in place
            // of RosterSignatures (none: every weapon for every hero). Null, the roster's own.
            public HeroWeapon[] RosterPool;
            public string[] RosterSignatureIds;
        }

        // The weapons a run's cards draw from: the roster's when the lever is on, the shipped catalogue otherwise.
        internal static HeroWeapon[] CatalogueFor(string ownWeaponId, FoundryLevers levers) =>
            levers.WeaponRoster ? RosterCatalogue(ownWeaponId, levers.RosterPool, levers.RosterSignatureIds) : WeaponCatalogue();

        // The shipped levers with the roster on, under a name of its own.
        internal static FoundryLevers RosterLevers(FoundryLevers shipped, string name = "roster")
        {
            FoundryLevers levers = (shipped ?? new FoundryLevers()).Copy(name);
            levers.WeaponRoster = true;
            return levers;
        }
    }
}
