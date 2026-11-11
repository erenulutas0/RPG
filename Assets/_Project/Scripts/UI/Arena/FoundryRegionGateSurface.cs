using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The complete gatehouse shares a physical quarry field. Mapping each
        // stone independently made broad jambs look cloudy and thin returns
        // repeat the same streak. Fold tessellation keeps this fine density
        // without sampling atlas mortar or changing the existing face light.
        // Sample the level interior right of the authored drip at U~.245. The
        // stone left of it brightens toward its arris; mirrored over the long
        // crown that ramp printed as cloudy bands. Apply once, to static
        // completed stone, before depth registration.
        internal static readonly Vector4 GateHousePatch=new Vector4(.272f,.44f,.43f,.564f);
        // That patch is already among the atlas's calmest slate interiors; its
        // remaining cloud is a 15-25 texel mottle. At .075 it printed as ~14 px
        // blotches across the broad crown planes at the gate camera. A finer
        // physical grain (period 1.6 x 1.2 units, still far beyond one coping
        // stone) lets that mottle settle into stone texture instead of clouds.
        internal const float GateHouseDensity=.10f;

        private static void GateHouseGrain(Builder stone)
        {
            StoneDetailGrain(stone,0,stone.V.Count,Project(GateU(32),-3),Vector2.right,GateHousePatch,GateHouseDensity);
            // Native gate stone read violet beside the baked slate bays. One
            // constant albedo balance keeps every relative forge-light value.
            for(int i=0;i<stone.C.Count;i++)stone.C[i]=GateHouseAlbedo(stone.C[i]);
        }

        internal static Color GateHouseAlbedo(Color source)=>
            new Color(source.r*1.015f,source.g*1.035f,source.b*.95f,source.a);
    }
}
