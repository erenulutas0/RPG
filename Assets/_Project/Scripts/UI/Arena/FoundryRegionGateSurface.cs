using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The complete gatehouse shares a physical quarry field. Mapping each
        // stone independently made broad jambs look cloudy and thin returns
        // repeat the same streak. Fold tessellation keeps this fine density
        // without sampling atlas mortar or changing the existing face light.
        // Keep the patch left of the authored drip at U~.245; a long stain
        // across the crown would contradict its shallow pitched construction.
        // Apply once, to static completed stone, before depth registration.
        private static void GateHouseGrain(Builder stone)
        {
            StoneDetailGrain(stone,0,stone.V.Count,Project(GateU(32),-3),Vector2.right,
                new Vector4(.04f,.44f,.21f,.564f),.075f);
        }
    }
}
