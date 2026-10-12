using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // A connected working apron groups the receiving edge, transfer turntable,
        // trolley and north stock approach. Its east shoulder returns to ordinary
        // court flags before the pit-side passage. Only existing laid stones change;
        // the walking union, forced brass joints, seal and gate remain intact.
        private static bool ServiceApronFlag(Builder mesh,AshlarStone s)
        {
            if(s.Zone!=0&&s.Zone!=3)return false;
            float u=(s.I+s.W*.5f)*.5f,v=(s.J+s.H*.5f)*.5f;
            float weight=ServiceApronWeight(u,v);
            if(weight<=0)return false;
            // All stones in the core belong to one repair. Only its outer shoulder
            // admits existing flags, breaking a perfect rectangle without noisy holes.
            uint hash=PavingHash(s.I,s.J,117);
            if(weight<1&&(hash&65535u)/65535f>weight)return false;

            bool alongU=s.W>=s.H;
            int span=alongU?s.W:s.H;
            // Preserve all smaller parents whole. Only broad >=2-unit flags split,
            // and only when both children retain at least a one-unit short side.
            // The older repair halved one-unit squares into dense little setts.
            if(span<4||Mathf.Min(s.W,s.H)<2)
            {
                AshlarFlagFace(mesh,s,true);
                return true;
            }
            // Integer half-unit partitions exactly cover the parent's original box.
            // Never add a patch over existing paving or bridge a brass/layout boundary.
            int split=span/2;
            var first=s;var second=s;
            if(alongU){first.W=split;second.I+=split;second.W-=split;}
            else{first.H=split;second.J+=split;second.H-=split;}
            AshlarFlagFace(mesh,first,true);
            AshlarFlagFace(mesh,second,true);
            return true;
        }

        private static float ServiceApronWeight(float u,float v)
        {
            float spine=-24+Cryptforge.Art.FoundryMap.WorkyardShift;
            // This full-strength core follows the existing north/south stock rails;
            // the west arm meets it across the complete transfer-track width.
            float stock=DistanceFromFloorBox(u,v,spine-3.6f,-3.5f,spine+3.6f,17.65f);
            float receiving=DistanceFromFloorBox(u,v,Cryptforge.Art.FoundryMap.West,4.6f,spine-2.4f,9.4f);
            // One soft edge at laid-stone scale, not a second stripe or raised kerb.
            // Full core selection cannot contain random missing repair stones.
            float distance=Mathf.Min(stock,receiving);
            return 1-Mathf.SmoothStep(0,1,Mathf.Clamp01(distance/1.8f));
        }
    }
}
