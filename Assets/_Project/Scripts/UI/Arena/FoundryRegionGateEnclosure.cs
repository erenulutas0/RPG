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
                GateEnclosureTier(stone,32,v0,36,v1,0,.46f,.06f,.91f,true);
                // Three broad masonry lifts with recessed lower faces; the upper
                // coping belongs to the wall instead of floating behind a thin panel.
                GateEnclosureTier(stone,32.16f,v0+.12f,35.88f,v1-.12f,.46f,2.9f,.035f,.82f);
                GateEnclosureTier(stone,32.05f,v0+.05f,35.98f,v1-.05f,2.9f,3.13f,.025f,.94f,true);
                GateEnclosureTier(stone,32.2f,v0+.16f,35.84f,v1-.16f,3.13f,6.55f,.05f,.85f);
                GateEnclosureTier(stone,32.02f,v0+.02f,36,v1-.02f,6.55f,6.82f,.045f,.98f,true);
                GateEnclosureTier(stone,32.15f,v0+.14f,35.87f,v1-.14f,6.82f,7.02f,.11f,.91f,true);
                // A tall dressed recess links the curtain to the paired jambs while
                // remaining in the narrow exposed strip beside the broad buttress.
                GateEnclosureRelief(stone,new Vector4(32.2f,v0+.16f,35.84f,v1-.16f),
                    3.13f,6.55f,.05f,true,far?.155f:.845f,.085f,3.53f,6.12f);
                Create("Gate fortified curtain wings",stone,arena.PlatformMaterials.FoundryWall,order);
            }

            using(MassDepth(towerFoot,9.2f))
            {
                var foundation=new Builder();
                GateEnclosureTier(foundation,towerFoot.x,towerFoot.y,towerFoot.z,towerFoot.w,-3.45f,0,.10f,.84f);
                Create("Gate buttress foundations",foundation,arena.PlatformMaterials.FoundryWall,-16);
                var stone=new Builder();
                GateEnclosureTier(stone,31.05f,towerV,34.9f,towerV+2.45f,0,.52f,.10f,.92f,true);
                // A broad battered lower body carries the upper tower. Stepping the
                // footprint inward with height reads as a buttress, not a tall chimney.
                GateEnclosureTier(stone,31.16f,towerV+.11f,34.79f,towerV+2.34f,.52f,3.38f,.32f,.87f);
                GateEnclosureTier(stone,31.43f,towerV+.36f,34.52f,towerV+2.09f,3.38f,3.63f,.05f,1f,true);
                GateEnclosureTier(stone,31.53f,towerV+.46f,34.42f,towerV+1.99f,3.63f,6.72f,.065f,.86f);
                GateEnclosureTier(stone,31.42f,towerV+.35f,34.53f,towerV+2.10f,6.72f,7.01f,.07f,.98f,true);
                GateEnclosureTier(stone,31.60f,towerV+.52f,34.35f,towerV+1.93f,7.01f,8.26f,.04f,.87f);
                GateEnclosureTier(stone,31.46f,towerV+.40f,34.49f,towerV+2.05f,8.26f,8.57f,.065f,1f,true);
                // One wide hipped crown terminates each solid buttress; no extra
                // finial, fire source, narrow spire or animated component is added.
                GateEnclosureTier(stone,31.60f,towerV+.52f,34.35f,towerV+1.93f,8.57f,9.05f,.38f,.84f,true);
                GateEnclosureTier(stone,31.99f,towerV+.90f,33.96f,towerV+1.55f,9.05f,9.2f,.05f,.96f,true);
                var lower=new Vector4(31.16f,towerV+.11f,34.79f,towerV+2.34f);
                var middle=new Vector4(31.53f,towerV+.46f,34.42f,towerV+1.99f);
                foreach(bool front in new[]{true,false})
                {
                    GateEnclosureQuoins(stone,lower,front);
                    GateEnclosureRelief(stone,middle,3.63f,6.72f,.065f,
                        front,.5f,front?.31f:.33f,3.97f,6.40f);
                }
                Create("Gate broad crowned buttresses",stone,arena.PlatformMaterials.FoundryWall,order);
            }
        }

        private static void GateEnclosureTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset,float shade,bool dressed=false)
        {
            int first=mesh.C.Count;
            LitFoundryTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            for(int i=first;i<mesh.C.Count;i++)
            {
                Color tint=mesh.C[i];
                mesh.C[i]=new Color(tint.r*shade,tint.g*shade,tint.b*shade,1);
            }
            if(dressed)GateEnclosureDressedTier(mesh,first);
        }

        private static void GateEnclosureDressedTier(Builder mesh,int first)
        {
            // Broad mouldings are solid dressed stones. Each face samples one
            // continuous stone interior instead of a row of bricks wrapping a cap.
            for(int f=0;f<3;f++)
            {
                int i=first+f*4;
                mesh.U[i]=new Vector2(.27f,.65f);mesh.U[i+1]=new Vector2(.42f,.65f);
                mesh.U[i+2]=new Vector2(.42f,.75f);mesh.U[i+3]=new Vector2(.27f,.75f);
            }
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            for(int i=first+12;i<mesh.V.Count;i++)
            {
                Vector3 p=mesh.V[i];minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);
                minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);
            }
            for(int i=first+12;i<mesh.V.Count;i++)
            {
                Vector3 p=mesh.V[i];
                mesh.U[i]=new Vector2(Mathf.Lerp(.27f,.42f,Mathf.InverseLerp(minX,maxX,p.x)),
                    Mathf.Lerp(.65f,.75f,Mathf.InverseLerp(minY,maxY,p.y)));
            }
        }

        private static Vector3 GateEnclosureFace(Vector4 box,bool front,float position,float y,
            float bottom,float top,float inset)
        {
            float taper=inset*Mathf.InverseLerp(bottom,top,y);
            return (front?Project(box.x+taper,Mathf.Lerp(box.y+taper,box.w-taper,position)):
                Project(Mathf.Lerp(box.x+taper,box.z-taper,position),box.y+taper))+Vector3.up*y;
        }

        private static void GateEnclosureQuoins(Builder mesh,Vector4 box,bool front)
        {
            // Large alternating corner courses follow the existing battered body;
            // these are surface reliefs inside its footprint, not added block towers.
            for(int course=0;course<4;course++)
            {
                float lo=.69f+course*.62f,hi=lo+.56f;
                foreach(bool right in new[]{false,true})
                {
                    float edge=front?.125f:.08f;
                    float reach=(front?.34f:.23f)-(course%2==0?0:.035f);
                    float a=right?1-reach:edge,b=right?1-edge:reach;
                    Vector3 p=GateEnclosureFace(box,front,a,lo,.52f,3.38f,.32f);
                    Vector3 q=GateEnclosureFace(box,front,b,lo,.52f,3.38f,.32f);
                    Vector3 r=GateEnclosureFace(box,front,b,hi,.52f,3.38f,.32f);
                    Vector3 s=GateEnclosureFace(box,front,a,hi,.52f,3.38f,.32f);
                    float shade=(front?.42f:.57f)*(course%2==0?1:.955f);
                    HeaderDressedFace(mesh,p,q,r,s,new Color(shade*.95f,shade*.98f,shade),.07f);
                }
            }
        }

        private static void GateEnclosureRelief(Builder mesh,Vector4 box,float courseBottom,float courseTop,
            float inset,bool front,float centre,float half,float bottom,float top)
        {
            float h=top-bottom;
            var outer=new[]{new Vector2(centre-half,bottom),new Vector2(centre+half,bottom),
                new Vector2(centre+half,top-h*.27f),new Vector2(centre,top),
                new Vector2(centre-half,top-h*.27f)};
            var middle=new Vector2(centre,bottom+h*.46f);
            var inner=new Vector2[5];
            for(int i=0;i<5;i++)inner[i]=new Vector2(Mathf.Lerp(outer[i].x,centre,.25f),
                Mathf.Lerp(outer[i].y,middle.y,.13f));
            Vector3 hub=GateEnclosureFace(box,front,middle.x,middle.y,courseBottom,courseTop,inset);
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Vector3 a=GateEnclosureFace(box,front,outer[i].x,outer[i].y,courseBottom,courseTop,inset);
                Vector3 b=GateEnclosureFace(box,front,outer[j].x,outer[j].y,courseBottom,courseTop,inset);
                Vector3 ia=GateEnclosureFace(box,front,inner[i].x,inner[i].y,courseBottom,courseTop,inset);
                Vector3 ib=GateEnclosureFace(box,front,inner[j].x,inner[j].y,courseBottom,courseTop,inset);
                // A cool stone-backed niche, not a pitch-black opening or gold icon.
                float recess=(front?.225f:.285f)*(i==0?1.12f:i==2||i==3?.91f:1f);
                GateStonePatch(mesh,ia,ib,hub,hub,new Color(recess*.91f,recess*.96f,recess));
                float light=i==0?.66f:i==1?.34f:i==2?.37f:i==3?.56f:.59f;
                if(front)light*=.83f;
                // Divide the long jambs into large dressed blocks with narrow joints.
                int count=i==1||i==4?3:1;
                for(int part=0;part<count;part++)
                {
                    float t0=(part+.015f)/count,t1=(part+.985f)/count;
                    HeaderDressedFace(mesh,Vector3.Lerp(a,b,t0),Vector3.Lerp(a,b,t1),
                        Vector3.Lerp(ia,ib,t1),Vector3.Lerp(ia,ib,t0),
                        new Color(light*.94f,light*.97f,light),.035f);
                }
            }
        }
    }
}
