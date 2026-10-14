using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static void BuildGateInnerPier(Builder stone,float v)
        {
            // Retain the original footing and lower collar seat. Above them a
            // substantial haunch carries a shorter bay, like the outer masonry.
            LitFoundryTier(stone,GateU(32),v,GateU(35),v+2,0,.45f,.08f);
            GateEnclosureTier(stone,GateU(32.12f),v+.10f,GateU(34.88f),v+1.90f,.45f,1.45f,.14f,.91f);
            GateEnclosureTier(stone,GateU(32.16f),v+.14f,GateU(34.84f),v+1.86f,1.45f,1.70f,.08f,.96f,true);
            var lower=new Vector4(GateU(32.26f),v+.24f,GateU(34.74f),v+1.76f);
            var neck=new Vector4(GateU(32.48f),v+.46f,GateU(34.52f),v+1.54f);
            GateInnerPierMass(stone,lower,neck,1.70f,2.65f,.96f);
            // The gentle upper flare returns to the old head plane at 5.45,
            // keeping the fixed 5.43 iron collar seated against the same face.
            GateRecessedShaft(stone,neck,2.65f,5.45f,-.18f,.96f,.28f);
            GateInnerPierMass(stone,
                new Vector4(GateU(32.08f),v+.08f,GateU(34.92f),v+1.92f),
                new Vector4(GateU(32.25f),v+.20f,GateU(34.75f),v+1.80f),5.45f,6.05f,.96f);
            // Preserve the sloping head and exact terminal bearing under the
            // existing iron. The capital below is now one broad seated mass.
            GateCraftTier(stone,GateU(32.25f),v+.2f,GateU(34.75f),v+1.8f,6.05f,7.05f,.48f);
            GateCraftTier(stone,GateU(32.62f),v+.57f,GateU(34.38f),v+1.43f,7.05f,7.25f,.05f);
        }

        private static void GateInnerPierMass(Builder stone,Vector4 lower,Vector4 upper,
            float bottom,float top,float shade)
        {
            var foot=new Builder();var head=new Builder();
            LitFoundryTier(foot,lower.x,lower.y,lower.z,lower.w,bottom,top,0);
            LitFoundryTier(head,upper.x,upper.y,upper.z,upper.w,bottom,top,0);
            for(int face=0;face<3;face++)
            {
                int first=face*4,start=stone.V.Count;
                var points=new[]{foot.V[first],foot.V[first+1],head.V[first+2],head.V[first+3]};
                var light=new[]{foot.C[first],foot.C[first+1],head.C[first+2],head.C[first+3]};
                Color tone=light[2]*shade;tone.a=1;
                if(face==2)stone.Quad(points[0],points[1],points[2],points[3],false,tone*.91f);
                else GateRecessCoursedFace(stone,points[0],points[1],points[2],points[3],
                    tone,.025f,face,face==0?2:1,false);
                for(int i=start;i<stone.V.Count;i++)
                {
                    Vector2 p=GateRecessCoordinates(points,stone.V[i]);
                    Color inherited=Color.Lerp(Color.Lerp(light[0],light[1],p.x),
                        Color.Lerp(light[3],light[2],p.x),p.y)*shade;
                    Color c=stone.C[i];
                    stone.C[i]=new Color(c.r*inherited.r/Mathf.Max(.01f,tone.r),
                        c.g*inherited.g/Mathf.Max(.01f,tone.g),c.b*inherited.b/Mathf.Max(.01f,tone.b),1);
                }
                GateRecessGrain(stone,start,stone.V.Count,points[1]-points[0],face);
            }
            // One indexed cap closes the whole bearing; no duplicate tier or
            // coplanar lid remains under the next supported component.
            int cap=stone.V.Count;
            for(int i=0;i<8;i++)
            {
                int source=12+i*4;stone.V.Add(head.V[source]);stone.U.Add(head.U[source]);
                Color c=head.C[source]*shade;c.a=1;stone.C.Add(c);
            }
            stone.V.Add(head.V[14]);stone.U.Add(head.U[14]);
            Color centre=head.C[14]*shade;centre.a=1;stone.C.Add(centre);
            for(int i=0;i<8;i++)stone.T.AddRange(new[]{cap+i,cap+(i+1)%8,cap+8});
            GateRecessGrain(stone,cap,stone.V.Count,Project(1,0)-Project(0,0),3);
        }
    }
}
