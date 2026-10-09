using UnityEngine;

namespace Cryptforge.UI
{
    // Continuous roof and coping grain; existing silhouette, elevation and anchors.
    public sealed partial class FoundryRegionView
    {
        private static void BuildGateHeaderFinish(Builder stone,float height)
        {
            Vector3 a=Project(32.18f,-.55f)+Vector3.up*(height+.43f);
            Vector3 b=Project(32.18f,10.55f)+Vector3.up*(height+.43f);
            Vector3 ridge=Project(32.18f,5)+Vector3.up*(height+1.55f);
            Vector3 back=Project(33.78f,5)-Project(32.18f,5);
            // The recessed front and seal housing retain their established appearance.
            stone.WallQuad(a,b,ridge,ridge,false,new Color(.47f,.51f,.62f));
            BuildGatePedimentRecess(stone,a,b,ridge);
            var edges=new[]{new[]{a,ridge},new[]{ridge,b}};
            var roofTones=new[]{new Color(.67f,.70f,.77f),new Color(.47f,.51f,.61f)};
            for(int side=0;side<2;side++)
            {
                Vector3 p=edges[side][0],q=edges[side][1];
                HeaderStonePatch(stone,p,q,q+back,p+back,roofTones[side],p,q,p+back);
            }
            // These are the same fourteen coping blocks, with the same gaps and bevels.
            // Tops now follow their supporting slope's light instead of a single flat
            // tone on both roof planes. Small course variation avoids a striped header.
            for(int side=0;side<2;side++)
            {
                var edge=edges[side];
                for(int i=0;i<7;i++)
                {
                    Vector3 p=Vector3.Lerp(edge[0],edge[1],(i+.015f)/7f);
                    Vector3 q=Vector3.Lerp(edge[0],edge[1],(i+.985f)/7f);
                    Vector3 rise=Vector3.up*.19f;
                    float course=i%3==1?.975f:1f;
                    HeaderDressedFace(stone,p,q,q+rise,p+rise,
                        new Color(.65f,.68f,.75f)*course,.14f);
                    HeaderDressedFace(stone,p+rise,q+rise,q+back+rise,p+back+rise,
                        roofTones[side]*(1.035f*course),.035f);
                }
            }
        }

        private static void HeaderDressedFace(Builder mesh,Vector3 a,Vector3 b,
            Vector3 c,Vector3 d,Color tint,float bevel)
        {
            Vector3 center=(a+b+c+d)*.25f;
            Vector3 ia=Vector3.Lerp(a,center,bevel),ib=Vector3.Lerp(b,center,bevel);
            Vector3 ic=Vector3.Lerp(c,center,bevel),id=Vector3.Lerp(d,center,bevel);
            // All five patches sample the same stone face. Previously each thin bevel
            // restarted the full grain patch, exaggerating its separation from the body.
            HeaderStonePatch(mesh,ia,ib,ic,id,tint,a,b,d);
            HeaderStonePatch(mesh,a,b,ib,ia,tint*.76f,a,b,d);
            HeaderStonePatch(mesh,b,c,ic,ib,tint*.88f,a,b,d);
            HeaderStonePatch(mesh,c,d,id,ic,tint*1.09f,a,b,d);
            HeaderStonePatch(mesh,d,a,ia,id,tint*.95f,a,b,d);
        }

        private static void HeaderStonePatch(Builder mesh,Vector3 a,Vector3 b,
            Vector3 c,Vector3 d,Color tint,Vector3 origin,Vector3 along,Vector3 across)
        {
            tint.a=1;
            int first=mesh.U.Count;
            mesh.Quad(a,b,c,d,false,tint);
            Vector3 u=along-origin,v=across-origin;
            float determinant=u.x*v.y-u.y*v.x;
            for(int i=0;i<4;i++)
            {
                Vector3 p=mesh.V[first+i]-origin;
                float s=0,t=0;
                if(Mathf.Abs(determinant)>.000001f)
                {
                    s=(p.x*v.y-p.y*v.x)/determinant;
                    t=(u.x*p.y-u.y*p.x)/determinant;
                }
                // Same known stone-interior patch as GateStonePatch. No horizontal
                // mortar lines, new texture, material, renderer or lifetime ownership.
                mesh.U[first+i]=new Vector2(.27f+.15f*Mathf.Clamp01(s),
                    .65f+.10f*Mathf.Clamp01(t));
            }
        }
    }
}
