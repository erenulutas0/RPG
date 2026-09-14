namespace Cryptforge.Combat
{
    // The arena floor seen at an isometric angle: floor distances keep their length across the screen and shrink to half
    // up it. The hero stands at the floor origin, which is also the world origin, and halving or doubling is exact in
    // floating point, so the scene measures the same distances as the Descent simulation.
    public static class ArenaFloor
    {
        public const float DepthScale = 0.5f;

        public static float WorldY(float floorY) => floorY * DepthScale;

        // The squared floor distance for an offset between two world positions.
        public static float DistanceSquared(float worldDx, float worldDy)
        {
            float floorDy = worldDy / DepthScale;
            return worldDx * worldDx + floorDy * floorDy;
        }
    }
}
