using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static void PitHoodStoneFoot(Builder b,Vector2 c)
        {
            // Exact existing supporting masonry and lower pool-facing tint. The hood
            // becomes formed iron above it, still within radius 1.1 / owner height 1.3.
            const float shade=.72f;
            int ring=b.C.Count;
            MassCourse(b,c,1.1f,1.1f,0,.18f,8,false,new Color(.95f*shade,.95f*shade,.95f*shade));
            for(int i=ring;i<b.C.Count;i++)
                if((i-ring)%4<2)
                    b.C[i]=Tinted(b.C[i],Color.Lerp(PitDomeFoot,PitDomeGlow,
                        PitFacingPool(c,FrontAngle((i-ring)/4,8)+Mathf.PI/16)));
            MassDisk(b,c,1.1f,.18f,8,false,new Color(.78f*shade,.8f*shade,.9f*shade));
        }

        private static void FormedPitHood(Builder b,Vector2 c)
        {
            // The pressure vessel has a broad eight-sided pressed head. Its receiver
            // has a taller stepped jacket. Both are manufactured assemblies seated
            // on the same old flange, inside the original radius/height ownership.
            bool pressure=c.x<PitCentre.x;
            Vector2[] profile=PitHoodProfile(pressure);
            PitHoodCourse(b,c,1.1f,1.1f,.035f,.16f,.80f);
            for(int i=0;i+1<profile.Length;i++)
                PitHoodCourse(b,c,profile[i].x,profile[i+1].x,profile[i].y,profile[i+1].y,
                    i==0?.87f:1f,pressure?4:8);
            Vector2 last=profile[profile.Length-1];
            PitMetalHeadCap(b,c,last.x,last.y,pressure?4:8);
            PitHoodPlateRibs(b,c,profile,pressure?4:8);
            // Existing flue dimensions and open mouth retain their smoke attachment.
            PitHoodCourse(b,c,.24f,.24f,last.y,1.2f,.83f);
            PitHoodCourse(b,c,.27f,.27f,last.y,last.y+.11f,1.05f);
            PitHoodCourse(b,c,.3f,.3f,1.12f,1.26f,.98f);
            PitHoodMouth(b,c);
        }

        private static Vector2[] PitHoodProfile(bool pressure)=>pressure
            ?new[]{new Vector2(1.025f,.18f),new Vector2(1.025f,.51f),new Vector2(.68f,.76f)}
            :new[]{new Vector2(1.025f,.18f),new Vector2(1.025f,.30f),new Vector2(.80f,.51f),
                new Vector2(.80f,.79f),new Vector2(.38f,.99f)};

        private static void PitHoodCourse(Builder b,Vector2 c,float r0,float r1,
            float h0,float h1,float gain,int half=8)
        {
            for(int k=-half;k<half;k++)
            {
                float a=FrontAngle(k,half),e=FrontAngle(k+1,half);
                PitMetalPlate(b,c,r0,r1,h0,h1,a,e,gain);
            }
        }

        private static void PitHoodPlateRibs(Builder b,Vector2 c,Vector2[] profile,int half)
        {
            // Four broad hold-down straps are clipped to their supporting facets,
            // with seated heel blocks on the lower flange. Interpolate actual facet
            // endpoints: points on an ideal circle would float off the pressed head.
            for(int k=0;k<half;k+=half/4)
            {
                float a=FrontAngle(k,half),e=FrontAngle(k+1,half);
                var tone=new Color(.74f,.72f,.65f);
                for(int row=0;row+1<profile.Length;row++)
                {
                    Vector2 p=profile[row],q=profile[row+1];
                    Vector3 pa=OnRing(c,p.x+.008f,a,p.y),pe=OnRing(c,p.x+.008f,e,p.y);
                    Vector3 qa=OnRing(c,q.x+.008f,a,q.y),qe=OnRing(c,q.x+.008f,e,q.y);
                    PitHoodHardwareFace(b,c,Vector3.Lerp(pa,pe,.34f),Vector3.Lerp(pa,pe,.66f),
                        Vector3.Lerp(qa,qe,.66f),Vector3.Lerp(qa,qe,.34f),tone);
                }
                // A short solid saddle ties each strap to the flange; the depth is
                // radial rather than a screen-space extrusion across the machinery.
                float centre=(a+e)*.5f,wide=.10f;
                float heelInner=half==4?.91f:.99f;
                Vector3 a0=OnRing(c,heelInner,centre-wide,.10f),b0=OnRing(c,heelInner,centre+wide,.10f);
                Vector3 a1=OnRing(c,1.085f,centre-wide,.10f),b1=OnRing(c,1.085f,centre+wide,.10f);
                Vector3 rise=Vector3.up*.16f;
                PitHoodHardwareFace(b,c,a1,b1,b1+rise,a1+rise,tone*.85f);
                PitHoodHardwareFace(b,c,a0,a1,a1+rise,a0+rise,tone*.65f);
                PitHoodHardwareFace(b,c,b1,b0,b0+rise,b1+rise,tone*.72f);
                PitHoodHardwareFace(b,c,a0+rise,a1+rise,b1+rise,b0+rise,tone);
            }
        }

        private static void PitHoodHardwareFace(Builder b,Vector2 centre,Vector3 a,Vector3 c,Vector3 d,Vector3 e,Color tone)
        {
            int first=b.U.Count;tone.a=1;
            FullFace(b,a,c,d,e,tone);
            PitMetalGrain(b,first,b.V.Count,centre);
        }

        private static void PitHoodMouth(Builder b,Vector2 c)
        {
            // Actual annular lip and dark inner return leave the centre open. Its
            // short recessed bottom uses the existing iron, with no extra batch.
            PitHoodCap(b,c,.19f,1.14f,new Color(.045f,.04f,.05f));
            for(int k=-8;k<8;k++)
            {
                float a=FrontAngle(k,8),e=FrontAngle(k+1,8);
                PitHoodHardwareFace(b,c,OnRing(c,.3f,a,1.26f),OnRing(c,.3f,e,1.26f),
                    OnRing(c,.19f,e,1.26f),OnRing(c,.19f,a,1.26f),new Color(.68f,.66f,.60f));
                PitHoodHardwareFace(b,c,OnRing(c,.19f,a,1.14f),OnRing(c,.19f,e,1.14f),
                    OnRing(c,.19f,e,1.26f),OnRing(c,.19f,a,1.26f),new Color(.14f,.14f,.16f));
            }
        }

        private static void PitHoodCap(Builder b,Vector2 c,float radius,float height,Color tone,int half=12)
        {
            int first=b.V.Count;
            MassDisk(b,c,radius,height,half,true,tone);
            PitMetalGrain(b,first,b.V.Count,c);
        }
    }
}
