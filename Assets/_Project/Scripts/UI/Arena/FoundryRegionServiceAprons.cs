using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Two maintained work areas: beside the west anvil approach, and below the
        // north stock shelf. Only already-laid stones are replaced; the walking union,
        // brass-forced joints, seal, gate apron and elevation projection remain intact.
        private static bool ServiceApronFlag(Builder mesh,AshlarStone s)
        {
            if(s.Zone!=0&&s.Zone!=3)return false;
            float u=(s.I+s.W*.5f)*.5f,v=(s.J+s.H*.5f)*.5f;
            float west=ServiceApronWeight(u,v,-21.1f+WestShift,6.8f,2.25f,4.1f);
            float north=ServiceApronWeight(u,v,-15.3f+WestShift,16f,3.45f,1.48f);
            float weight=Mathf.Max(west,north);
            if(weight<=0)return false;
            // All stones in the core belong to one repair. Only its outer shoulder
            // admits existing flags, breaking a perfect rectangle without noisy holes.
            uint hash=PavingHash(s.I,s.J,117);
            if(weight<1&&(hash&65535u)/65535f>weight)return false;

            bool alongU=west>=north;
            // Vary a minority of square parents' split direction to break repeated
            // half-flag lines. Long parents split across their longest dimension.
            if(s.W>s.H)alongU=true;
            else if(s.H>s.W)alongU=false;
            else if((hash&0x30000u)==0)alongU=!alongU;
            int span=alongU?s.W:s.H;
            if(span<2)
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

        private static float ServiceApronWeight(float u,float v,float centreU,float centreV,
            float radiusU,float radiusV)
        {
            float du=(u-centreU)/radiusU,dv=(v-centreV)/radiusV;
            float radius=Mathf.Sqrt(du*du+dv*dv);
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1f,radius));
        }
    }
}
