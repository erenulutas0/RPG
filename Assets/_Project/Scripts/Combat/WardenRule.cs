using System;
using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The foundry's gate boss as a fight rather than a wave (docs/41 step 4, the owner's report docs/40 section 8.9): the
    // Forge Warden marks the floor where the hero stands every SlamEvery seconds, and after a Telegraph its hammer lands
    // on the mark - SlamRadius wide, HeroShare of the hero's maximum health - so a hero who reads the mark steps out and
    // one who stands and trades takes the blow. From the mark to the blow it is winding the hammer up, and for
    // RecoverySeconds after the blow the hammer is in the floor: through both it swings nothing else, so the mark is the
    // one thing to read and the blow opens a window to strike back. Below PhaseTwoAt of its health it is enraged: the
    // marks come every SlamEveryEnraged and the floor's horde thickens round it. Before this the Warden was a 300-health
    // wave the owner killed in 10-17 s for 6-11 health (docs/21, 2026-10-25). Pure, so the clock can be tested without a
    // scene; the scene's WardenSlamService applies it to the foundry's gate boss only.
    public static class WardenRule
    {
        // The gate boss's health against its authored 300, on the foundry only, for docs/41 step 4's 45-65 s fight. 10 held
        // that with the legacy cards; with the weapons-and-tomes offers (docs/42), whose steps are much smaller, the
        // autopilot died at the Warden on every seed with half its health left, and it went to 5. On the V0.1 floor (two
        // surges a hall, a second elite, the gate at 3:30) the heroes walk out at level 25-30 and 5 lasted 34-36 s (P50,
        // the foundry simulation, 200 seeds a hero; 33-44 s in the scene). Health alone could not buy the fight back: what
        // the fight costs the hero grows with its length, and 8 gave 45-50 s for 6.5 and 15.5 points of clears. With the
        // weapon held through the windup and the recovery (RecoverySeconds), 8.5 gives a P50 of 51 s for the Vanguard and
        // 47 s for Ilya, P90 61 and 58 s, and clears of 36 % and 78 % against 36 % and 76 % before (seeds 1-600 a hero;
        // docs/41, status 2026-11-03). Measured, not chosen: change it only with a re-measurement.
        public const float GateHealthShare = 8.5f;

        // The gate boss's share on a floor whose own (FloorDefinition.FoundryGateHealthShare) is floorShare: that share
        // where the floor sets one, GateHealthShare where it is 0. Floor 2 sets 8.5 / 1.4 (Floor_QuicksilverVaults.asset):
        // the climb's x1.4 (ClimbScaling.Health(2)) cancelled on its Warden, which with GateHealthShare was 300 x 8.5 x 1.4
        // and lasted a P50 of 70 s (Vanguard) and 64 s (Ilya); 8.5 / 1.4 gives 58 and 51 s, 56 and 50 s with a human's
        // hands, inside docs/41 step 4's 45-65 s (docs/41, status 2026-11-10 later). Floor 1 keeps GateHealthShare. One
        // body, never inlined, for EncounterController.SpawnWave and the foundry simulation's floors after the first.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float GateHealthShareOn(float floorShare)
        {
            if (!IsFloorGateHealthShare(floorShare))
                throw new ArgumentOutOfRangeException(nameof(floorShare));
            return floorShare > 0f ? floorShare : GateHealthShare;
        }

        // Whether a floor's own share is one GateHealthShareOn takes: 0, or a finite share above 0 - never NaN or negative.
        public static bool IsFloorGateHealthShare(float floorShare) =>
            floorShare == 0f || (floorShare > 0f && !float.IsInfinity(floorShare));

        public const float SlamEvery = 6f;
        public const float SlamEveryEnraged = 4f;
        // Between the mark appearing and the hammer landing: the report's 0.8-1.2 s for a readable telegraph on a phone.
        public const float Telegraph = 1.1f;
        public const float SlamRadius = 2.2f;
        public const float HeroShare = 0.2f;
        public const float PhaseTwoAt = 0.5f;
        // How long the hammer stays in the floor after a blow, the Warden's weapon still: docs/41 step 4's vulnerability
        // window. At share 8.5 it is the shortest that kept both heroes' clears where they were - 1 s cost each 2.5
        // points - while 2 s leaves the enraged Warden under a second to swing between a blow and the next mark (docs/41,
        // status 2026-11-03).
        public const float RecoverySeconds = 1.5f;

        // The Sword is the Warden's weapon (docs/41, status 2026-11-10; open decision 3, the Sword's role without the
        // Staff): every landing of the Sword on the foundry's gate boss - its strike, its cleave and its procs - deals
        // SwordShare of what it would. The Staff leads the horde; on the Warden the Sword now leads it (27 a second to
        // 22, P50 of 200 seeds, the Vanguard started with each; docs/cloud/2026-11-04-sword-role.md), and the Warden's
        // fight shortens from 64 to 56 s (Vanguard) and 55 to 50 s (Ilya), the careful clears unchanged (199 and 200 of
        // 200). Measured, not chosen: change it only with a re-measurement. Nothing else of the Sword moves: the hall
        // arena's Warden, the Descent's bosses, the elites and the horde take its authored damage, and every other weapon
        // deals the gate boss its own. The id is the WeaponDefinition's (Data/Weapons/Weapon_Sword.asset).
        public const string SwordId = "weapon_sword";
        public const float SwordShare = 1.5f;

        // What a weapon's landings on the gate boss are multiplied by, by its id: SwordShare for the Sword, 1 for any
        // other. WeaponLoadout hands it to each weapon it takes up (WeaponRuntime.GateBossShare).
        public static float GateBossShareOf(string weaponId) => weaponId == SwordId ? SwordShare : 1f;

        // One landing of amount from a weapon whose gate-boss share is share: on the gate boss times the share, on
        // anything else the amount. One body, never inlined, for WeaponRuntime.TryAttack, which the scene's
        // AttackController and the foundry simulation both strike through, so the two round the product alike.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float DamageOn(float amount, float share, bool gateBoss) => gateBoss ? amount * share : amount;

        public static bool IsEnraged(float current, float maximum) => maximum > 0f && current <= maximum * PhaseTwoAt;
    }

    // The Warden's slam clock: idle until the next mark, then a telegraph, then the blow. Enraged, the next mark comes
    // sooner; a mark already down keeps its whole telegraph.
    public sealed class WardenSlamClock
    {
        public enum Step { None, Marked, Slammed }

        // Seconds since the last blow (or the fight's start), and whether a mark is down.
        public float Seconds { get; private set; }
        public bool IsMarked { get; private set; }
        // How far through the telegraph the clock is: 0 at the mark, 1 at the blow; 0 with no mark down.
        public float TelegraphProgress { get; private set; }
        // Whether the Warden holds its weapon now: winding the hammer up from the mark to the blow, and for
        // WardenRule.RecoverySeconds after each blow while the hammer is in the floor. Free before its first mark.
        public bool HoldsWeapon => HoldsWeaponFor(WardenRule.RecoverySeconds);

        // Whether a blow has landed since the fight began.
        private bool _struck;
        // When the mark that is down lands, on Seconds: a telegraph after the mark, fixed as it goes down. The pace read
        // each frame instead brought the hammer down the frame after the Warden enraged under a calm mark - 4 s enraged
        // against the 4.9 s a calm mark goes down at - a telegraph of one frame and a fifth of the hero's health.
        private float _blowAt;

        // HoldsWeapon with another recovery, for a measurement that sweeps it (the Descent simulation's foundry levers).
        public bool HoldsWeaponFor(float recoverySeconds) => IsMarked || (_struck && Seconds < recoverySeconds);

        public void Reset()
        {
            Seconds = 0f;
            IsMarked = false;
            TelegraphProgress = 0f;
            _blowAt = 0f;
            _struck = false;
        }

        // One frame of the fight; what happened on it.
        public Step Advance(float deltaTime, bool enraged)
        {
            if (float.IsNaN(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            Seconds += deltaTime;
            if (!IsMarked)
            {
                float every = enraged ? WardenRule.SlamEveryEnraged : WardenRule.SlamEvery;
                if (Seconds < every - WardenRule.Telegraph)
                    return Step.None;
                IsMarked = true;
                Seconds = every - WardenRule.Telegraph;
                _blowAt = every;
                TelegraphProgress = 0f;
                return Step.Marked;
            }
            TelegraphProgress = Math.Min(1f, (Seconds - (_blowAt - WardenRule.Telegraph)) / WardenRule.Telegraph);
            if (Seconds < _blowAt)
                return Step.None;
            IsMarked = false;
            TelegraphProgress = 0f;
            Seconds = 0f;
            _struck = true;
            return Step.Slammed;
        }
    }
}
