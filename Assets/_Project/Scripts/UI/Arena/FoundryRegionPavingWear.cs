using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static Color PavingMineralFamily(float u,float v)
        {
            // Several neighbouring slabs come from the same mineral family. This
            // changes their albedo, while existing world-space forge lighting remains.
            float quarry=FloorPatina(u*.105f+14.6f,v*.105f-8.3f);
            float weather=FloorPatina(u*.23f-2.1f,v*.23f+17.4f);
            Color cool=new Color(.88f,.93f,1.01f),warm=new Color(1.085f,1.055f,.985f);
            Color family=Color.Lerp(cool,warm,Mathf.SmoothStep(.08f,.92f,quarry));
            family*=Mathf.Lerp(.93f,1.035f,weather);family.a=1;
            return family;
        }

        private static float PavingWearExposure(float u,float v)
        {
            float work=1-Mathf.Clamp01(DistanceFromFloorBox(u,v,
                FoundryMap.West+1,4,FoundryMap.West+6,12)/7f);
            float heat=1-Mathf.Clamp01(DistanceFromFloorBox(u,v,PitU0,PitV0,PitU1,PitV1)/3.5f);
            float age=FloorPatina(u*.19f+31,v*.19f-13);
            return Mathf.Clamp01(Mathf.Max(work,heat)*.65f+age*.35f);
        }

        private static void BrokenFlagCorner(Builder mesh,float u0,float v0,float u1,float v1,
            float joint,float bevel,float tone,Color tint,int i,int j,float exposure)
        {
            // Small fractured corners expose several mineral planes rather than a
            // broad dark polygon. Every point remains within the complete flag bed.
            uint hash=PavingHash(i,j,311);
            int corner=(int)(hash&3);
            Vector2 origin=new Vector2(corner==0||corner==3?u0+joint:u1-joint,
                corner<2?v0+joint:v1-joint);
            Vector2 along=corner==0?Vector2.right:corner==1?Vector2.up:corner==2?Vector2.left:Vector2.down;
            Vector2 inward=new Vector2(-along.y,along.x);
            float size=Mathf.Min(.21f,Mathf.Min(u1-u0,v1-v0)*Mathf.Lerp(.085f,.15f,exposure));
            float width=size*Mathf.Lerp(.7f,1.2f,((hash>>8)&255)/255f);
            float depth=size*Mathf.Lerp(.65f,1.0f,((hash>>16)&255)/255f);
            Vector2 a=origin,b=origin+along*width;
            Vector2 c=origin+along*(width*.68f)+inward*(depth*.36f);
            Vector2 d=origin+along*(width*.31f)+inward*(depth*.79f),e=origin+inward*depth;
            Vector2 grain=origin+along*(width*.24f)+inward*(depth*.23f);
            int first=mesh.V.Count;
            PavingVertex(mesh,a.x,a.y,tone*.81f,tint,FloorLight(a.x,a.y));
            PavingVertex(mesh,b.x,b.y,tone*.94f,tint,FloorLight(b.x,b.y));
            PavingVertex(mesh,c.x,c.y,tone*.99f,tint,FloorLight(c.x,c.y));
            PavingVertex(mesh,d.x,d.y,tone*.87f,tint,FloorLight(d.x,d.y));
            PavingVertex(mesh,e.x,e.y,tone*.96f,tint,FloorLight(e.x,e.y));
            PavingVertex(mesh,grain.x,grain.y,tone*.83f,tint,FloorLight(grain.x,grain.y));
            for(int k=0;k<5;k++)
            {
                mesh.T.Add(first+k);mesh.T.Add(first+(k+1)%5);mesh.T.Add(first+5);
            }
            // A short facet, never a complete bright outline around the break.
            Vector2 lip=(along+inward).normalized*Mathf.Min(.013f,bevel*.35f);
            FlagWearFace(mesh,c,d,d+lip,c+lip,tone*1.015f,tint);
        }

        private static void FracturedFlagEdge(Builder mesh,float u0,float v0,float u1,float v1,
            float joint,float bevel,float tone,Color tint,int i,int j,float grain,float hue)
        {
            int side=Mathf.Min(3,Mathf.FloorToInt(hue*4));
            Vector2 along=side==0?Vector2.right:side==1?Vector2.up:side==2?Vector2.left:Vector2.down;
            Vector2 inward=new Vector2(-along.y,along.x);
            Vector2 origin=side==0?new Vector2(u0+joint,v0+joint):
                side==1?new Vector2(u1-joint,v0+joint):
                side==2?new Vector2(u1-joint,v1-joint):new Vector2(u0+joint,v1-joint);
            float length=(side%2==0?u1-u0:v1-v0)-2*joint;
            uint hash=PavingHash(i,j,521);
            float start=length*Mathf.Lerp(.15f,.25f,(hash&255)/255f);
            float span=length*Mathf.Lerp(.42f,.62f,grain);
            float depth=Mathf.Min(.10f,bevel+Mathf.Min(u1-u0,v1-v0)*.024f);
            int first=mesh.V.Count;
            // Shared strip vertices keep this five-segment broken rim economical.
            // Uneven depths expose a grain bed; the rest of the stone stays dressed.
            for(int k=0;k<=5;k++)
            {
                float t=k/5f;
                float tooth=.57f+.43f*((PavingHash(i+k,j,557)&255)/255f);
                float fade=k==0||k==5?.12f:tooth;
                Vector2 outer=origin+along*(start+span*t);
                Vector2 inner=outer+inward*(depth*fade);
                PavingVertex(mesh,outer.x,outer.y,tone*.81f,tint,FloorLight(outer.x,outer.y));
                PavingVertex(mesh,inner.x,inner.y,tone*Mathf.Lerp(.94f,1.025f,tooth),tint,FloorLight(inner.x,inner.y));
            }
            for(int k=0;k<5;k++)
            {
                int a=first+k*2;
                mesh.T.Add(a);mesh.T.Add(a+2);mesh.T.Add(a+3);
                mesh.T.Add(a);mesh.T.Add(a+3);mesh.T.Add(a+1);
            }
            Vector2 centre=origin+along*(start+span*.45f)+inward*(depth*.34f);
            FlagMineralTriangle(mesh,centre-along*.025f,centre+along*.038f,
                centre+inward*(depth*.37f),tone*.96f,tint);
            // Discontinuous rubbed lip and occasional mineral grit sit in the
            // fracture only, never distributed as random confetti over the court.
            Vector2 p=origin+along*(start+span*.63f)+inward*(depth*.67f);
            Vector2 q=p+along*Mathf.Min(.16f,span*.15f)+inward*(depth*.09f);
            FlagWearFace(mesh,p,q,q+inward*.009f,p+inward*.009f,tone*1.025f,tint);
            if(hash%3==0)
            {
                Vector2 fleck=origin+along*(start+span*.78f)+inward*(depth*.24f);
                FlagMineralTriangle(mesh,fleck,fleck+along*.028f,
                    fleck+along*.011f+inward*.021f,tone*.98f,tint);
                fleck=origin+along*(start+span*.22f)+inward*(depth*.28f);
                FlagMineralTriangle(mesh,fleck,fleck+along*.022f,
                    fleck+inward*.016f,tone*.89f,tint);
            }
        }

        private static void IncisedFlagLine(Builder mesh,float u0,float v0,float u1,float v1,
            float bevel,float tone,Color tint,int i,int j)
        {
            // A rare short, shallow inclusion. The middle half of each flag is
            // reserved for it, so it cannot reach brass seams, joints or corners.
            uint hash=PavingHash(i,j,613);
            float w=u1-u0-2*bevel,h=v1-v0-2*bevel;
            Vector2 centre=new Vector2((u0+u1)*.5f,(v0+v1)*.5f);
            float flip=(hash&1)==0?1:-1;
            Vector2 a=centre+new Vector2(-w*.23f,-h*.12f*flip);
            Vector2 b=centre+new Vector2(-w*.02f,h*.035f*flip);
            Vector2 c=centre+new Vector2(w*.18f,h*.085f*flip);
            const float width=.009f;
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width;
            FlagWearFace(mesh,a,b,b+n,a+n,tone*.84f,tint);
            n=new Vector2(-(c-b).y,(c-b).x).normalized*width;
            FlagWearFace(mesh,b,c,c+n,b+n,tone*.89f,tint);
        }

        private static void FlagMineralTriangle(Builder mesh,Vector2 a,Vector2 b,Vector2 c,float tone,Color tint)
        {
            int first=mesh.V.Count;
            PavingVertex(mesh,a.x,a.y,tone,tint,FloorLight(a.x,a.y));
            PavingVertex(mesh,b.x,b.y,tone,tint,FloorLight(b.x,b.y));
            PavingVertex(mesh,c.x,c.y,tone,tint,FloorLight(c.x,c.y));
            mesh.T.Add(first);mesh.T.Add(first+1);mesh.T.Add(first+2);
        }
    }
}
