using Cryptforge.Combat;

namespace Cryptforge.Tests
{
    // The first-card cohort on floor 2 (docs/47, research 2026-11-11 night): with floor 2's gate at 3:30 the first-card
    // cohort's floor-2 deaths went from 6 to 16 while every other cohort's fell. Two research levers on the floors after
    // the first, each off by default and read by this simulation alone (FirstCardFloorTwoResearch); floor 2's Warden share
    // and its forge's Mend already have theirs (FoundryLevers.LaterWardenHealthShare, LaterMendAmount).
    internal static partial class DescentSimulation
    {
        internal sealed partial class FoundryLevers
        {
            // Research only: on a floor after the first, a horde enemy that walks in once the floor's clock is past this
            // many seconds reads the horde's growth (HordeClock's health and damage) and its experience as at this second:
            // the horde stops hardening there, and that floor's overrun never comes. 0 is off.
            public float LaterHordeClockCap;
            // Research only: on a floor after the first, the gate room's own chest holds a heal of this share of the
            // hero's maximum (ChestRule.HealFraction, a quarter, is room 0's) and stands where the hero is as the Warden
            // walks out, as the finale's chest stands where the hero arrives. 0 is off: the room's chest as ChestRule has it.
            public float LaterGateChestHeal;
        }

        private sealed partial class FoundryRun
        {
            // The horde's clock a spawn reads (FoundryLevers.LaterHordeClockCap): the director's own, or the cap. At the
            // default levers every one of these is the director's value itself, so a spawn is what it was.
            private bool CapsHordeClock =>
                _levers.LaterHordeClockCap > 0f && _floor.FloorNumber >= 2 && _horde.Seconds > _levers.LaterHordeClockCap;

            private float HordeClockNow => CapsHordeClock ? _levers.LaterHordeClockCap : _horde.Seconds;

            private float HordeHealthNow =>
                CapsHordeClock ? HordeClock.HealthMultiplier(_levers.LaterHordeClockCap) : _horde.HealthMultiplier;

            private float HordeDamageNow =>
                CapsHordeClock ? HordeClock.DamageMultiplier(_levers.LaterHordeClockCap) : _horde.DamageMultiplier;

            // GateBossArrived's research hook, after the gate's readings (HeroHealthAtGate before any heal): the gate room's
            // chest turned into a heal under the hero's feet. Nothing at the default levers.
            private void GateArrival()
            {
                if (_levers.LaterGateChestHeal <= 0f || _floor.FloorNumber < 2)
                    return;
                _chestReward = new ChestReward(ChestRewardKind.Heal, _levers.LaterGateChestHeal, 0, _chestReward.HoldsBook);
                _chestPlaced = true;
                _chestOpen = false;
                _chestX = HeroX;
                _chestY = HeroY;
            }
        }
    }
}
