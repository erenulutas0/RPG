using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Only the complete inner-pier family uses this finer quarry field.
        // Source lighting and real reveal shadows stay on the same vertices;
        // the clean atlas interior avoids the mortar and drip elsewhere in V4.
        private static void GateInnerPierGrain(Builder stone)
        {
            StoneDetailGrain(stone,0,stone.V.Count,Project(GateU(32),-3),Vector2.right,
                new Vector4(.04f,.44f,.21f,.564f),.11f);
        }

        private static void AppendGateStone(Builder target,Builder part)
        {
            // Append already finished static pieces in their original painter
            // order. Create still assigns the existing structure depth owners.
            if(part.Flames.Count!=0||part.Cloth.Count!=0)
                throw new System.InvalidOperationException("Gate stone must be static.");
            int offset=target.V.Count;
            target.V.AddRange(part.V);target.U.AddRange(part.U);target.C.AddRange(part.C);
            foreach(int index in part.T)target.T.Add(offset+index);
        }
    }
}
