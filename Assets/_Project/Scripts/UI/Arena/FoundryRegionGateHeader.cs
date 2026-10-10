using UnityEngine;

namespace Cryptforge.UI
{
    // Supported portal crown; unchanged footprint, maximum elevation and door anchors.
    public sealed partial class FoundryRegionView
    {
        private static void BuildGateHeaderFinish(Builder stone,float height)
        {
            // A broad supported lintel replaces the long shallow temple gable.
            // The two low shoulders climb into a narrow central crest, retaining
            // the existing height envelope while strengthening the industrial gate.
            GateEnclosureTier(stone,32.08f,-.78f,33.92f,10.78f,height+.43f,height+.72f,.06f,.90f,true);
            foreach(bool left in new[]{true,false})
            {
                float v0=left?-.68f:6.65f,v1=left?3.35f:10.68f;
                GateEnclosureTier(stone,32.18f,v0,33.82f,v1,height+.72f,height+.97f,.11f,.93f,true);
                float inner0=left?2.0f:6.5f,inner1=left?3.5f:8.0f;
                GateEnclosureTier(stone,32.28f,inner0,33.72f,inner1,height+.97f,height+1.22f,.13f,.86f,true);
            }
            GateEnclosureTier(stone,32.15f,3.35f,33.85f,6.65f,height+.72f,height+1.32f,.10f,.88f,true);
            GateEnclosureTier(stone,32.22f,3.38f,33.78f,6.62f,height+1.32f,height+1.55f,.09f,.98f,true);
            // Five substantial corbels beneath the lintel read as structural
            // support; their broad top returns carry the continuous header above.
            foreach(float v in new[]{-.50f,2.20f,4.70f,7.20f,9.90f})
            {
                Vector3 a=Project(32.05f,v)+Vector3.up*(height-.28f);
                Vector3 b=Project(32.05f,v+.40f)+Vector3.up*(height-.28f);
                Vector3 c=Project(32.0f,v+.53f)+Vector3.up*(height+.08f);
                Vector3 d=Project(32.0f,v-.13f)+Vector3.up*(height+.08f);
                Vector3 back=Project(32.46f,v)-Project(32.0f,v);
                HeaderStonePatch(stone,a,a+back,d+back,d,new Color(.35f,.39f,.48f),a,a+back,d);
                HeaderStonePatch(stone,d,c,c+back,d+back,new Color(.67f,.70f,.77f),d,c,d+back);
                HeaderDressedFace(stone,a,b,c,d,new Color(.53f,.57f,.65f),.06f);
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
