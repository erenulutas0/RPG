using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // A pair of deep curtain-wall wings makes the existing painted door part of
        // a fortified gatehouse. Both wings are outside the unchanged walking union;
        // the door, threshold and complete approach remain at their authored anchors.
        private void BuildGateEnclosure(ArenaView arena,bool far)
        {
            float v0=far?13f:-6.6f,v1=far?16.6f:-3f;
            var wallFoot=new Vector4(32,v0,36,v1);
            // The outer buttress projects toward the court, never into the central
            // passage. Its base starts after the ramp so courses remain horizontal.
            float towerV=far?14.15f:-6.6f;
            var towerFoot=new Vector4(31.05f,towerV,34.9f,towerV+2.45f);
            float order=far?-14f:-11f;
            using(MassDepth(wallFoot,7.35f))
            {
                var foundation=new Builder();
                LitFoundryTier(foundation,wallFoot.x,wallFoot.y,wallFoot.z,wallFoot.w,-3.2f,0,.06f);
                Create("Gate curtain foundations",foundation,arena.PlatformMaterials.FoundryWall,-16);

                var stone=new Builder();
                GateEnclosureTier(stone,32,v0,36,v1,0,.46f,.06f,.91f);
                // Three broad masonry lifts with recessed lower faces; the upper
                // coping belongs to the wall instead of floating behind a thin panel.
                GateEnclosureTier(stone,32.16f,v0+.12f,35.88f,v1-.12f,.46f,2.9f,.035f,.82f);
                GateEnclosureTier(stone,32.05f,v0+.05f,35.98f,v1-.05f,2.9f,3.13f,.025f,.94f);
                GateEnclosureTier(stone,32.2f,v0+.16f,35.84f,v1-.16f,3.13f,6.55f,.05f,.85f);
                GateEnclosureTier(stone,32.02f,v0+.02f,36,v1-.02f,6.55f,6.82f,.045f,.98f);
                GateEnclosureTier(stone,32.15f,v0+.14f,35.87f,v1-.14f,6.82f,7.02f,.11f,.91f);
                // The inner section remains readable alongside the existing jamb.
                // A single narrow blind slit is enough at gameplay scale.
                float slit=far?v0+.53f:v1-.53f;
                GateEnclosureSlit(stone,32.19f,slit,3.68f,5.68f,.24f);
                Create("Gate fortified curtain wings",stone,arena.PlatformMaterials.FoundryWall,order);
            }

            using(MassDepth(towerFoot,9.2f))
            {
                var foundation=new Builder();
                GateEnclosureTier(foundation,towerFoot.x,towerFoot.y,towerFoot.z,towerFoot.w,-3.45f,0,.10f,.84f);
                Create("Gate buttress foundations",foundation,arena.PlatformMaterials.FoundryWall,-16);
                var stone=new Builder();
                GateEnclosureTier(stone,31.05f,towerV,34.9f,towerV+2.45f,0,.52f,.10f,.92f);
                // A broad battered lower body carries the upper tower. Stepping the
                // footprint inward with height reads as a buttress, not a tall chimney.
                GateEnclosureTier(stone,31.16f,towerV+.11f,34.79f,towerV+2.34f,.52f,3.38f,.32f,.87f);
                GateEnclosureTier(stone,31.43f,towerV+.36f,34.52f,towerV+2.09f,3.38f,3.63f,.05f,1f);
                GateEnclosureTier(stone,31.53f,towerV+.46f,34.42f,towerV+1.99f,3.63f,6.72f,.065f,.86f);
                GateEnclosureTier(stone,31.42f,towerV+.35f,34.53f,towerV+2.10f,6.72f,7.01f,.07f,.98f);
                GateEnclosureTier(stone,31.60f,towerV+.52f,34.35f,towerV+1.93f,7.01f,8.26f,.04f,.87f);
                GateEnclosureTier(stone,31.46f,towerV+.40f,34.49f,towerV+2.05f,8.26f,8.57f,.065f,1f);
                // One wide hipped crown terminates each solid buttress; no extra
                // finial, fire source, narrow spire or animated component is added.
                GateEnclosureTier(stone,31.60f,towerV+.52f,34.35f,towerV+1.93f,8.57f,9.05f,.38f,.84f);
                GateEnclosureTier(stone,31.99f,towerV+.90f,33.96f,towerV+1.55f,9.05f,9.2f,.05f,.96f);
                GateEnclosureSlit(stone,31.56f,towerV+1.225f,4.23f,6.05f,.28f);
                Create("Gate broad crowned buttresses",stone,arena.PlatformMaterials.FoundryWall,order);
            }
        }

        private static void GateEnclosureTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset,float shade)
        {
            int first=mesh.C.Count;
            LitFoundryTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            for(int i=first;i<mesh.C.Count;i++)
            {
                Color tint=mesh.C[i];
                mesh.C[i]=new Color(tint.r*shade,tint.g*shade,tint.b*shade,1);
            }
        }

        private static void GateEnclosureSlit(Builder mesh,float u,float v,float bottom,float top,float width)
        {
            Vector3 a=Project(u,v-width)+Vector3.up*bottom;
            Vector3 b=Project(u,v+width)+Vector3.up*bottom;
            Vector3 c=Project(u,v+width)+Vector3.up*(top-.20f);
            Vector3 d=Project(u,v-width)+Vector3.up*(top-.20f);
            Vector3 peak=Project(u,v)+Vector3.up*top;
            var dark=new Color(.17f,.19f,.24f);
            GateStonePatch(mesh,a,b,c,d,dark);
            GateStonePatch(mesh,d,c,peak,peak,dark);
            // Plain recessed masonry, not another full door texture or gold outline.
            GateEnclosureEdge(mesh,a,b,.08f,new Color(.44f,.47f,.55f));
            GateEnclosureEdge(mesh,b,c,.055f,new Color(.32f,.35f,.43f));
            GateEnclosureEdge(mesh,a,d,.055f,new Color(.25f,.28f,.36f));
        }

        private static void GateEnclosureEdge(Builder mesh,Vector3 a,Vector3 b,float width,Color tint)
        {
            Vector3 n=new Vector3(-(b-a).y,(b-a).x,0).normalized*(width*.5f);
            GateStonePatch(mesh,a+n,b+n,b-n,a-n,tint);
        }
    }
}
