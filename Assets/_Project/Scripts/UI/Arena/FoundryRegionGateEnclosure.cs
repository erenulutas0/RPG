using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // A pair of deep curtain-wall wings makes the existing painted door part of
        // a fortified gatehouse. Both wings stay outside the shortened walking union;
        // door, threshold and foundations share the same rigid gate translation.
        private void BuildGateEnclosure(ArenaView arena,bool far)
        {
            float v0=far?13f:-6.6f,v1=far?16.6f:-3f;
            var wallFoot=new Vector4(GateU(32),v0,GateU(36),v1);
            // The outer buttress projects toward the court, never into the central
            // passage. Its base starts after the ramp so courses remain horizontal.
            float towerV=far?14.15f:-6.6f;
            var towerFoot=new Vector4(GateU(31.05f),towerV,GateU(34.9f),towerV+2.45f);
            float order=far?-14f:-11f;
            using(MassDepth(wallFoot,7.35f))
            {
                var foundation=new Builder();
                LitFoundryTier(foundation,wallFoot.x,wallFoot.y,wallFoot.z,wallFoot.w,-3.2f,0,.06f);
                Create("Gate curtain foundations",foundation,arena.PlatformMaterials.FoundryWall,-16);

                var stone=new Builder();
                GateEnclosureTier(stone,GateU(32),v0,GateU(36),v1,0,.46f,.06f,.91f,true);
                // Three broad masonry lifts with recessed lower faces; the upper
                // coping belongs to the wall instead of floating behind a thin panel.
                GateEnclosureTier(stone,GateU(32.16f),v0+.12f,GateU(35.88f),v1-.12f,.46f,2.9f,.035f,.82f);
                GateEnclosureTier(stone,GateU(32.05f),v0+.05f,GateU(35.98f),v1-.05f,2.9f,3.13f,.025f,.94f,true);
                GateEnclosureTier(stone,GateU(32.2f),v0+.16f,GateU(35.84f),v1-.16f,3.13f,6.55f,.05f,.85f);
                GateEnclosureTier(stone,GateU(32.02f),v0+.02f,GateU(36),v1-.02f,6.55f,6.82f,.045f,.98f,true);
                GateEnclosureTier(stone,GateU(32.15f),v0+.14f,GateU(35.87f),v1-.14f,6.82f,7.02f,.11f,.91f,true);
                // A tall dressed recess links the curtain to the paired jambs while
                // remaining in the narrow exposed strip beside the broad buttress.
                GateEnclosureRelief(stone,new Vector4(GateU(32.2f),v0+.16f,GateU(35.84f),v1-.16f),
                    3.13f,6.55f,.05f,true,far?.155f:.845f,.085f,3.53f,6.12f);
                Create("Gate fortified curtain wings",stone,arena.PlatformMaterials.FoundryWall,order);
            }

            using(MassDepth(towerFoot,7.35f))
            {
                var foundation=new Builder();
                GateEnclosureTier(foundation,towerFoot.x,towerFoot.y,towerFoot.z,towerFoot.w,-3.45f,0,.10f,.84f);
                Create("Gate buttress foundations",foundation,arena.PlatformMaterials.FoundryWall,-16);
                var stone=new Builder();
                BuildGateOuterButtress(stone,towerFoot);
                Create("Gate broad crowned buttresses",stone,arena.PlatformMaterials.FoundryWall,order);
            }
        }

        private static void BuildGateOuterButtress(Builder stone,Vector4 foot)
        {
            // A low battered plinth now carries one tall blind bay. Removing the
            // mid-height waist and its applied corner blocks makes this a vertical
            // gate pier instead of two square storeys stacked beside the door.
            GateEnclosureTier(stone,foot.x,foot.y,foot.z,foot.w,0,.52f,.10f,.92f,true);
            GateEnclosureTier(stone,foot.x+.11f,foot.y+.11f,foot.z-.11f,foot.w-.11f,
                .52f,1.58f,.32f,.87f);
            GateEnclosureTier(stone,foot.x+.38f,foot.y+.36f,foot.z-.38f,foot.w-.36f,
                1.58f,1.84f,.05f,1f,true);
            GateRecessedShaft(stone,new Vector4(foot.x+.48f,foot.y+.46f,foot.z-.48f,foot.w-.46f),
                1.84f,6.55f,.065f,.92f,.36f);
            // One capital and a seated shoulder meet the existing curtain roof.
            // Keep the exact outer footprint, 7.35 crown and old rear bearing.
            GateEnclosureTier(stone,foot.x+.37f,foot.y+.35f,foot.z-.37f,foot.w-.35f,
                6.55f,6.82f,.07f,.98f,true);
            GateEnclosureTier(stone,foot.x+.49f,foot.y+.46f,foot.z-.49f,foot.w-.46f,
                6.82f,7.20f,.30f,.88f,true);
            GateEnclosureTier(stone,foot.x+.79f,foot.y+.76f,foot.z-.79f,foot.w-.76f,
                7.20f,7.35f,.04f,.94f,true);
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
            else if(bottom>=0&&top-bottom>.7f)GateEnclosureAshlar(mesh,first,bottom,top);
        }

        private static void GateEnclosureAshlar(Builder mesh,int first,float bottom,float top)
        {
            // Replace the wrapped large-brick face with cut ashlar, retaining the
            // tier's exact corners/taper and its original cap. The original wall
            // becomes the narrow mortar bed beneath the stones, not an extra decal.
            // Two broad damp runs follow each face from the belt above; neighboring
            // stones share their wash, rather than receiving independent dark tiles.
            for(int face=0;face<3;face++)
            {
                int index=first+face*4;
                var points=new[]{mesh.V[index],mesh.V[index+1],mesh.V[index+2],mesh.V[index+3]};
                var tones=new[]{mesh.C[index],mesh.C[index+1],mesh.C[index+2],mesh.C[index+3]};
                float width=Vector3.Distance(points[0],points[1]);
                for(int i=0;i<4;i++)
                {
                    mesh.U[index+i]=new Vector2(i==0||i==3?.27f:.42f,i<2?.65f:.75f);
                    // Keep chamfers solid and slightly damp instead of trying to
                    // subdivide a narrow corner into tiny, high-contrast bricks.
                    mesh.C[index+i]=tones[i]*(face==2?.88f:.72f);
                    mesh.C[index+i]=new Color(mesh.C[index+i].r,mesh.C[index+i].g,mesh.C[index+i].b,1);
                }
                if(face==2||width<.35f)continue;
                int rows=Mathf.Max(2,Mathf.RoundToInt((top-bottom)/.55f));
                float total=0;
                for(int row=0;row<rows;row++)total+=GateCourseWeight(row);
                float y=0;
                int columns=Mathf.Max(2,Mathf.RoundToInt(width/.63f));
                for(int row=0;row<rows;row++)
                {
                    float next=y+GateCourseWeight(row)/total;
                    // An alternating half bond and restrained repeating width rhythm
                    // make proper masonry courses without a regular checkerboard.
                    float x=row%2==0?0:-.46f/columns;
                    int block=0;
                    while(x<1)
                    {
                        float w=(block%3==0?1.14f:block%3==1?.91f:.95f)/columns;
                        float end=Mathf.Min(1,x+w),start=Mathf.Max(0,x);
                        float gapX=.008f/Mathf.Max(.3f,width);
                        float gapY=.009f/(top-bottom);
                        float a=start==0?0:start+gapX,b=end==1?1:end-gapX;
                        float lo=row==0?0:y+gapY,hi=row==rows-1?1:next-gapY;
                        if(b-a>.015f)
                            GateAshlarStone(mesh,points,tones,a,b,lo,hi,row,block,face,bottom,top);
                        x+=w;block++;
                    }
                    y=next;
                }
            }
        }

        private static float GateCourseWeight(int row)=>row%4==0?1.09f:row%4==1?.91f:row%4==2?1.04f:.96f;

        private static Vector3 GateFaceLerp(Vector3[] face,float u,float v)=>
            Vector3.Lerp(Vector3.Lerp(face[0],face[1],u),Vector3.Lerp(face[3],face[2],u),v);

        private static void GateAshlarStone(Builder mesh,Vector3[] face,Color[] light,
            float u0,float u1,float v0,float v1,int row,int block,int side,float bottom,float top)
        {
            int first=mesh.V.Count;
            mesh.Quad(GateFaceLerp(face,u0,v0),GateFaceLerp(face,u1,v0),
                GateFaceLerp(face,u1,v1),GateFaceLerp(face,u0,v1),false,Color.white);
            // Sample different nearby pieces of the existing mineral grain. No
            // visible brick seams from the old atlas and no additional texture.
            float offset=(row+block*2)%4*.007f;
            float a=.274f+offset,b=.406f-offset*.4f,c=.655f+(row%3)*.007f,d=.747f;
            mesh.U[first]=new Vector2(a,c);mesh.U[first+1]=new Vector2(b,c);
            mesh.U[first+2]=new Vector2(b,d);mesh.U[first+3]=new Vector2(a,d);
            var uv=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)};
            // Individual variation stays below the shared face weathering: a broad
            // stone wall should not become a checkerboard of unrelated light values.
            float stone=1f+(((block+row*3)%5)-2)*.018f;
            for(int i=0;i<4;i++)
            {
                Vector2 p=uv[i];
                Color baseLight=Color.Lerp(Color.Lerp(light[0],light[1],p.x),
                    Color.Lerp(light[3],light[2],p.x),p.y);
                float below=(1-p.y)*(top-bottom),above=p.y*(top-bottom);
                float soot=.16f*Mathf.Exp(-below/.42f)+.07f*Mathf.Exp(-above/.38f);
                float runA=Mathf.Clamp01(1-Mathf.Abs(p.x-(side==0?.27f:.68f))/.17f);
                float runB=Mathf.Clamp01(1-Mathf.Abs(p.x-(side==0?.78f:.24f))/.11f);
                float wash=(runA*runA*.13f+runB*runB*.075f)*Mathf.SmoothStep(.1f,1,p.y);
                float mineral=Mathf.Clamp01(1-Mathf.Abs(p.x-.49f)/.30f)*
                    Mathf.Clamp01(1-Mathf.Abs(p.y-.32f)/.37f)*.055f;
                float value=stone*(1-soot-wash+mineral);
                // Cool damp upper runs and a faint warm lower mineral wash stay in
                // the established indigo/basalt palette and below the doorway's contrast.
                float warm=mineral*.4f,cool=wash*.12f;
                mesh.C[first+i]=new Color(baseLight.r*value*(1+warm-cool),
                    baseLight.g*value*(1+warm*.2f),baseLight.b*value*(1+cool),1);
            }
            GateStoneEdges(mesh,first,.035f,.038f);
        }

        private static void GateStoneEdges(Builder mesh,int first,float along,float rise)
        {
            // Cut a narrow dressed rim into the existing face, entirely coplanar.
            // This catches the established face light without enlarging the tier or
            // adding coincident overlays. Grain and weathering remain continuous.
            var outer=new Vector3[4];var grain=new Vector2[4];var light=new Color[4];
            for(int i=0;i<4;i++){outer[i]=mesh.V[first+i];grain[i]=mesh.U[first+i];light[i]=mesh.C[first+i];}
            float x=Mathf.Min(.12f,along/Mathf.Max(.01f,Vector3.Distance(outer[0],outer[1])));
            float y=Mathf.Min(.12f,rise/Mathf.Max(.01f,Vector3.Distance(outer[0],outer[3])));
            var sample=new[]{new Vector2(x,y),new Vector2(1-x,y),new Vector2(1-x,1-y),new Vector2(x,1-y)};
            for(int i=0;i<4;i++)
            {
                Vector2 p=sample[i];
                mesh.V[first+i]=GateFaceLerp(outer,p.x,p.y);
                mesh.U[first+i]=Vector2.Lerp(Vector2.Lerp(grain[0],grain[1],p.x),Vector2.Lerp(grain[3],grain[2],p.x),p.y);
                mesh.C[first+i]=Color.Lerp(Color.Lerp(light[0],light[1],p.x),Color.Lerp(light[3],light[2],p.x),p.y);
            }
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4,start=mesh.V.Count;
                mesh.Quad(outer[edge],outer[next],mesh.V[first+next],mesh.V[first+edge],false,Color.white);
                float catchLight=edge==2?1.14f:edge==0?.80f:edge==1?.91f:1.035f;
                var tones=new[]{light[edge],light[next],mesh.C[first+next],mesh.C[first+edge]};
                var coords=new[]{grain[edge],grain[next],mesh.U[first+next],mesh.U[first+edge]};
                for(int i=0;i<4;i++)
                {
                    Color c=tones[i]*catchLight;c.a=1;mesh.C[start+i]=c;mesh.U[start+i]=coords[i];
                }
            }
        }

        private static void GateEnclosureDressedTier(Builder mesh,int first)
        {
            int originalEnd=mesh.V.Count;
            // Broad mouldings are solid dressed stones. Each face samples one
            // continuous stone interior instead of a row of bricks wrapping a cap.
            for(int f=0;f<3;f++)
            {
                int i=first+f*4;
                mesh.U[i]=new Vector2(.27f,.65f);mesh.U[i+1]=new Vector2(.42f,.65f);
                mesh.U[i+2]=new Vector2(.42f,.75f);mesh.U[i+3]=new Vector2(.27f,.75f);
            }
            float minX=float.MaxValue,minY=float.MaxValue,maxX=float.MinValue,maxY=float.MinValue;
            for(int i=first+12;i<originalEnd;i++)
            {
                Vector3 p=mesh.V[i];minX=Mathf.Min(minX,p.x);maxX=Mathf.Max(maxX,p.x);
                minY=Mathf.Min(minY,p.y);maxY=Mathf.Max(maxY,p.y);
            }
            for(int i=first+12;i<originalEnd;i++)
            {
                Vector3 p=mesh.V[i];
                mesh.U[i]=new Vector2(Mathf.Lerp(.27f,.42f,Mathf.InverseLerp(minX,maxX,p.x)),
                    Mathf.Lerp(.65f,.75f,Mathf.InverseLerp(minY,maxY,p.y)));
            }
            // Long mouldings are laid as broad fitted blocks instead of a single
            // seamless extrusion. Keep the upper roof bearing/cap untouched; only
            // divide its two visible vertical fascia faces inside their old corners.
            for(int face=0;face<2;face++)
            {
                int index=first+face*4;
                var points=new[]{mesh.V[index],mesh.V[index+1],mesh.V[index+2],mesh.V[index+3]};
                var tones=new[]{mesh.C[index],mesh.C[index+1],mesh.C[index+2],mesh.C[index+3]};
                float width=Vector3.Distance(points[0],points[1]);
                int blocks=Mathf.Max(2,Mathf.RoundToInt(width/1.1f));
                for(int i=0;i<4;i++){Color c=tones[i]*.73f;c.a=1;mesh.C[index+i]=c;}
                for(int block=0;block<blocks;block++)
                {
                    float a=(float)block/blocks,b=(float)(block+1)/blocks;
                    float joint=.013f/Mathf.Max(.3f,width);
                    if(block>0)a+=joint;if(block+1<blocks)b-=joint;
                    int start=mesh.V.Count;
                    mesh.Quad(GateFaceLerp(points,a,0),GateFaceLerp(points,b,0),GateFaceLerp(points,b,1),GateFaceLerp(points,a,1),false,Color.white);
                    for(int i=0;i<4;i++)
                    {
                        float u=i==0||i==3?a:b,v=i<2?0:1;
                        mesh.U[start+i]=new Vector2(.27f+.15f*u,.65f+.10f*v);
                        mesh.C[start+i]=Color.Lerp(Color.Lerp(tones[0],tones[1],u),Color.Lerp(tones[3],tones[2],u),v);
                    }
                    GateStoneEdges(mesh,start,.028f,.022f);
                }
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
                float recess=(front?.28f:.345f)*(i==0?1.08f:i==2||i==3?.94f:1f);
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
