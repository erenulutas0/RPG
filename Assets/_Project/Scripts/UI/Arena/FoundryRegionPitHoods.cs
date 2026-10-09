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
            // Retaining band and a broad convex shoulder, rather than a small stack
            // of masonry cones. Far and near slopes are complete; no flat rear wedge.
            PitHoodCourse(b,c,1.1f,1.1f,.035f,.16f,.80f);
            PitHoodCourse(b,c,.98f,.84f,.18f,.34f,1f);
            PitHoodCourse(b,c,.84f,.65f,.34f,.51f,1f);
            PitHoodCourse(b,c,.65f,.44f,.51f,.64f,1f);
            PitHoodCap(b,c,.44f,.64f,new Color(.60f,.62f,.67f));
            // Existing flue dimensions and open mouth retain their smoke attachment.
            PitHoodCourse(b,c,.24f,.24f,.64f,1.2f,.83f);
            PitHoodCourse(b,c,.27f,.27f,.66f,.79f,1.05f);
            PitHoodCourse(b,c,.3f,.3f,1.12f,1.26f,.98f);
            PitHoodCap(b,c,.3f,1.26f,new Color(.68f,.66f,.60f));
            PitHoodCap(b,c,.19f,1.26f,new Color(.085f,.08f,.09f));
        }

        private static void PitHoodCourse(Builder b,Vector2 c,float r0,float r1,
            float h0,float h1,float gain)
        {
            const int half=12;
            for(int k=-half;k<half;k++)
            {
                float a=FrontAngle(k,half),e=FrontAngle(k+1,half);
                int first=b.V.Count;
                FullFace(b,OnRing(c,r0,a,h0),OnRing(c,r0,e,h0),
                    OnRing(c,r1,e,h1),OnRing(c,r1,a,h1),Color.white);
                for(int j=0;j<4;j++)
                {
                    float angle=j==0||j==3?a:e,height=j<2?h0:h1;
                    float shoulder=Mathf.Clamp01((height-.18f)/.46f);
                    float facing=(1+Mathf.Cos(angle-270f*Mathf.Deg2Rad))*.5f;
                    float glint=Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle-260f*Mathf.Deg2Rad)),6);
                    Color tone=Color.Lerp(new Color(.32f,.35f,.42f),new Color(.79f,.79f,.77f),facing);
                    tone+=new Color(.12f,.13f,.15f,0)*glint;
                    // Pool reflection remains strongest at the foot and on the inward
                    // side; flue soot darkens the crown without making a black cap.
                    Color warmth=Color.Lerp(Color.white,Color.Lerp(PitDomeFoot,PitDomeGlow,
                        PitFacingPool(c,angle)),.60f*Mathf.Exp(-height/.30f));
                    tone=Tinted(tone,warmth);
                    tone*=gain*(1-.13f*shoulder*shoulder);tone.a=1;
                    b.C[first+j]=MetalTone(tone,b.V[first+j],height);
                    // Continuous local plain-plate grain, clear of riveted atlas borders.
                    b.U[first+j]=new Vector2(.20f+.60f*(angle+45f*Mathf.Deg2Rad)/(2*Mathf.PI),
                        .24f+.44f*Mathf.Clamp01(height/1.26f));
                }
            }
        }

        private static void PitHoodCap(Builder b,Vector2 c,float radius,float height,Color tone)
        {
            int first=b.V.Count;
            MassDisk(b,c,radius,height,12,true,tone);
            Vector3 centre=Project(c.x,c.y)+Vector3.up*height;
            for(int i=first;i<b.V.Count;i++)
            {
                Vector3 offset=b.V[i]-centre;
                b.U[i]=new Vector2(.5f+offset.x*.3f,.5f+offset.y*.6f);
            }
        }
    }
}
