using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // A connected working apron groups the receiving edge, transfer turntable,
        // trolley and north stock approach. Its east shoulder returns to ordinary
        // court flags before the pit-side passage. Only existing laid stones change;
        // the walking union, forced brass joints, seal and gate remain intact.
        private static bool ServiceApronClaims(AshlarStone s)
        {
            if(s.Zone!=0&&s.Zone!=3)return false;
            float u=(s.I+s.W*.5f)*.5f,v=(s.J+s.H*.5f)*.5f;
            float weight=ServiceApronWeight(u,v);
            if(weight<=0)return false;
            // All stones in the core belong to one repair. Only its outer shoulder
            // admits existing flags, breaking a perfect rectangle without noisy holes.
            uint hash=PavingHash(s.I,s.J,117);
            return weight>=1||(hash&65535u)/65535f<=weight;
        }

        private static bool ServiceApronFlag(Builder mesh,AshlarStone s)
        {
            if(!ServiceApronClaims(s))return false;

            bool alongU=s.W>=s.H;
            int span=alongU?s.W:s.H;
            // Preserve all smaller parents whole. Only broad >=2-unit flags split,
            // and only when both children retain at least a one-unit short side.
            // The older repair halved one-unit squares into dense little setts.
            if(span<4||Mathf.Min(s.W,s.H)<2)
            {
                LaidServiceStone(mesh,s);
                return true;
            }
            // Integer half-unit partitions exactly cover the parent's original box.
            // Never add a patch over existing paving or bridge a brass/layout boundary.
            int split=span/2;
            var first=s;var second=s;
            if(alongU){first.W=split;second.I+=split;second.W-=split;}
            else{first.H=split;second.J+=split;second.H-=split;}
            LaidServiceStone(mesh,first);
            LaidServiceStone(mesh,second);
            return true;
        }

        private static void LaidServiceStone(Builder mesh,AshlarStone stone)
        {
            int first=mesh.C.Count;
            AshlarFlagFace(mesh,stone,true);
            // Use the existing face samples, including mortar and arrises. This
            // is material use on the laid stone, not a floating stain/decal or an
            // additional light source. Every original vertex and UV stays put.
            float designScale=PavingGrainSpan/(2*Cryptforge.Art.FoundryMap.Axis);
            for(int i=first;i<mesh.C.Count;i++)
            {
                Vector2 p=(mesh.U[i]-Vector2.one)*designScale;
                float use=ServiceApronUse(p.x,p.y);
                Color color=mesh.C[i];
                mesh.C[i]=new Color(color.r*use,color.g*use,color.b*use,1);
            }
        }

        private static float ServiceApronUse(float u,float v)
        {
            // Settled mill scale belongs at two actual freight contacts: beyond
            // the trolley's loading end and the rail-facing side of the mould
            // bench. Unequal elongated patches avoid a uniform shadow halo around
            // each object or a dirt disc beneath the whole transfer table.
            float trolleyU=(Cryptforge.Art.FoundryMap.WorkIslandBound(0,0)+
                Cryptforge.Art.FoundryMap.WorkIslandBound(0,2))*.5f;
            float trolleyV=Cryptforge.Art.FoundryMap.WorkIslandBound(0,3)+.42f;
            float stockU=Cryptforge.Art.FoundryMap.WorkIslandBound(1,0)-.35f;
            float stockV=(Cryptforge.Art.FoundryMap.WorkIslandBound(1,1)+
                Cryptforge.Art.FoundryMap.WorkIslandBound(1,3))*.5f;
            float scale=Mathf.Max(ApronUsePatch(u,v,trolleyU,trolleyV,2.25f,1.75f),
                ApronUsePatch(u,v,stockU,stockV,1.85f,2.40f));
            scale*=Mathf.Lerp(.62f,1,FloorPatina(u*.42f+19.3f,v*.42f-7.6f));
            // The receiving threshold is scrubbed by use, with broad soft ends.
            // It terminates before the turntable centre; no bright closed track
            // or polished stripe runs through the otherwise quiet combat field.
            float spine=-24+Cryptforge.Art.FoundryMap.WorkyardShift;
            float thresholdU=(Cryptforge.Art.FoundryMap.West+.6f+spine-3.7f)*.5f;
            float scrub=ApronUsePatch(u,v,thresholdU,7,1.95f,1.40f);
            return 1-.14f*scale+.04f*scrub;
        }

        private static float ApronUsePatch(float u,float v,float centreU,float centreV,float radiusU,float radiusV)
        {
            float x=(u-centreU)/radiusU,y=(v-centreV)/radiusV;
            float distance=Mathf.Sqrt(x*x+y*y);
            float edge=Mathf.Lerp(.88f,1.08f,FloorPatina(u*.61f-11.4f,v*.61f+5.7f));
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,edge,distance));
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
