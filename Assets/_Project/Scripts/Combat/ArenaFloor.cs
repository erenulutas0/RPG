using System.Runtime.CompilerServices;

namespace Cryptforge.Combat
{
    // The arena floor seen at an isometric angle: floor distances keep their length across the screen and shrink to half
    // up it. The hero stands at the floor origin, which is also the world origin, and halving or doubling is exact in
    // floating point, so the scene measures the same distances as the Descent simulation.
    public static class ArenaFloor
    {
        public const float DepthScale = 0.5f;

        public static float WorldY(float floorY) => floorY * DepthScale;

        // The floor depth a world height stands for; the inverse of WorldY, exact for the half scale.
        public static float FloorY(float worldY) => worldY / DepthScale;

        // The squared floor distance for an offset between two world positions.
        public static float DistanceSquared(float worldDx, float worldDy)
        {
            float floorDy = worldDy / DepthScale;
            return worldDx * worldDx + floorDy * floorDy;
        }

        // The squared floor distance from `from` to `to`, in floor coordinates, for every reach, splash and target
        // decision the scene and the Descent simulation make. It is one method on purpose and it is never inlined on
        // purpose: two copies of `dx * dx + dy * dy` in two assemblies were compiled differently by the Editor's JIT and
        // disagreed in the last bit about which of two enemies 1.4 units away was nearer, so the scene struck one and
        // the simulation the other on the same frame (docs/21, the movement budget). A single body cannot disagree with
        // itself.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static float FloorDistanceSquared(float toX, float toY, float fromX, float fromY)
        {
            float dx = toX - fromX;
            float dy = toY - fromY;
            return dx * dx + dy * dy;
        }
    }
}
