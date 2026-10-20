using System;

namespace Cryptforge.Combat
{
    // The climb's difficulty by floor height (docs/05 proposed a geometric curve; owner decision 2026-10-20): every floor
    // above the first multiplies enemy health and damage by Growth, and a boss room on floors from BossFromFloor up adds
    // its own BossGrowth per floor on top. The first two floors equal the tiers they were hand-tuned to (health 1 / 1.4,
    // damage 0.92 / 1.288), so adopting the formula changes no existing balance; floors added above need no hand tuning.
    public static class ClimbScaling
    {
        public const float Growth = 1.4f;
        public const float FirstFloorDamage = 0.92f;
        public const float BossGrowth = 1.15f;
        public const int BossFromFloor = 3;

        public static float Health(int floorNumber) => Power(Growth, Height(floorNumber));

        public static float Damage(int floorNumber) => FirstFloorDamage * Power(Growth, Height(floorNumber));

        // A boss room's extra health multiplier: 1 below BossFromFloor, then BossGrowth per floor from it.
        public static float Boss(int floorNumber)
        {
            int floors = Height(floorNumber) + 2 - BossFromFloor;
            return floors <= 0 ? 1f : Power(BossGrowth, floors);
        }

        private static int Height(int floorNumber)
        {
            if (floorNumber < 1)
                throw new ArgumentOutOfRangeException(nameof(floorNumber));
            return floorNumber - 1;
        }

        // Repeated multiplication, as SwarmRule does, so float results match the authored tiers exactly.
        private static float Power(float factor, int times)
        {
            float result = 1f;
            for (int i = 0; i < times; i++)
                result *= factor;
            return result;
        }
    }
}
