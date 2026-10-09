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
            // A broken face corner exposes a shallow granular bed. Geometry stays
            // flat and within the stone; its complete mortar base still covers every
            // floor point, with no new obstacle, hole or disconnected rubble.
            uint hash=PavingHash(i,j,311);
            int corner=(int)(hash&3);
            Vector2 origin=new Vector2(corner==0||corner==3?u0+joint:u1-joint,
                corner<2?v0+joint:v1-joint);
            Vector2 along=corner==0?Vector2.right:corner==1?Vector2.up:corner==2?Vector2.left:Vector2.down;
            Vector2 inward=new Vector2(-along.y,along.x);
            float size=Mathf.Min(u1-u0,v1-v0)*Mathf.Lerp(.15f,.27f,exposure);
            float width=size*Mathf.Lerp(.7f,1.2f,((hash>>8)&255)/255f);
            float depth=size*Mathf.Lerp(.65f,1.0f,((hash>>16)&255)/255f);
            Vector2 a=origin,b=origin+along*width;
            Vector2 c=origin+along*(width*.50f)+inward*(depth*.27f);
            Vector2 d=origin+along*(width*.21f)+inward*(depth*.75f),e=origin+inward*depth;
            FlagWearFace(mesh,a,b,c,d,tone*.76f,tint);
            FlagWearFace(mesh,a,d,e,e,tone*.76f,tint);
            // One rubbed broken edge, interrupted at the kink rather than outlining
            // the whole chip like a UI border.
            Vector2 lip=(along+inward).normalized*Mathf.Min(.022f,bevel*.5f);
            FlagWearFace(mesh,c,d,d+lip,c+lip,tone*.98f,tint);
        }
    }
}
