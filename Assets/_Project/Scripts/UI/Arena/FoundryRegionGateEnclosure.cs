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
                if(!BuildBakedGateButtress(towerFoot,order))
                {
                    var stone=new Builder();
                    BuildGateOuterButtress(stone,towerFoot);
                    Create("Gate broad crowned buttresses",stone,arena.PlatformMaterials.FoundryWall,order);
                }
            }
        }

        private static void BuildGateOuterButtress(Builder stone,Vector4 foot)
        {
            // A broad coursed lower mass and one sloping shoulder seat the upper
            // blind bay. The shoulder ends exactly at the bay's footprint; the
            // original door, outer footing and crown remain in place.
            BuildGateButtressLower(stone,foot);
            GateRecessedShaft(stone,new Vector4(foot.x+.48f,foot.y+.46f,foot.z-.48f,foot.w-.46f),
                2.85f,6.55f,.065f,.92f,.36f);
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
            if(dressed)
            {
                var shell=new Builder();
                LitFoundryTier(shell,u0,v0,u1,v1,bottom,top,inset);
                for(int i=0;i<shell.C.Count;i++)
                {
                    Color c=shell.C[i]*shade;c.a=1;shell.C[i]=c;
                }
                GateEnclosureDressedTier(mesh,shell,top);
                return;
            }
            if(bottom>=0&&top-bottom>.7f)
            {
                var shell=new Builder();
                LitFoundryTier(shell,u0,v0,u1,v1,bottom,top,inset);
                for(int i=0;i<shell.C.Count;i++)
                {
                    Color c=shell.C[i]*shade;c.a=1;shell.C[i]=c;
                }
                GateEnclosureAshlar(mesh,shell,bottom,top);
                return;
            }
            int first=mesh.C.Count;
            LitFoundryTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            for(int i=first;i<mesh.C.Count;i++)
            {
                Color tint=mesh.C[i];
                mesh.C[i]=new Color(tint.r*shade,tint.g*shade,tint.b*shade,1);
            }
        }

        private static void GateEnclosureAshlar(Builder mesh,Builder shell,float bottom,float top)
        {
            // Partition the original two wall planes into complete stone/joint
            // cells. The old continuous wall no longer survives beneath them.
            for(int face=0;face<3;face++)
            {
                int index=face*4,start=mesh.V.Count;
                var points=new[]{shell.V[index],shell.V[index+1],shell.V[index+2],shell.V[index+3]};
                var tones=new[]{shell.C[index],shell.C[index+1],shell.C[index+2],shell.C[index+3]};
                float width=Vector3.Distance(points[0],points[1]);
                if(face==2||width<.35f)
                {
                    // The closed chamfer stays one quiet, solid stone face.
                    mesh.Quad(points[0],points[1],points[2],points[3],false,Color.white);
                    for(int i=0;i<4;i++){Color c=tones[i]*.88f;c.a=1;mesh.C[start+i]=c;}
                }
                else
                {
                    // Match the broad supporting masonry rather than covering
                    // the curtain in the old finer brick grid.
                    int rows=Mathf.Max(1,Mathf.RoundToInt((top-bottom)/.82f));
                    int columns=Mathf.Max(1,Mathf.RoundToInt(width/1.15f));
                    float total=0;for(int row=0;row<rows;row++)total+=GateCourseWeight(row);
                    float y=0;
                    for(int row=0;row<rows;row++)
                    {
                        float next=row==rows-1?1:y+GateCourseWeight(row)/total;
                        float x=row%2==0?0:-.46f/columns;int block=0;
                        while(x<1)
                        {
                            float w=(block%3==0?1.14f:block%3==1?.91f:.95f)/columns;
                            float end=Mathf.Min(1,x+w),left=Mathf.Max(0,x);
                            if(end>left)
                                GateAshlarStone(mesh,points,tones,left,end,y,next,row,block,face,bottom,top);
                            x+=w;block++;
                        }
                        y=next;
                    }
                }
                // One physical grain spans the entire existing wall plane,
                // including joints and arrises. No stone restarts the atlas patch.
                GateRecessGrain(mesh,start,mesh.V.Count,points[1]-points[0],face);
            }
            // Keep the existing cap, bearing and its lighting/UVs byte-for-byte.
            for(int first=12;first<shell.V.Count;first+=4)
            {
                int start=mesh.V.Count;
                for(int i=0;i<4;i++)
                {
                    mesh.V.Add(shell.V[first+i]);mesh.U.Add(shell.U[first+i]);mesh.C.Add(shell.C[first+i]);
                }
                mesh.T.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
        }

        private static float GateCourseWeight(int row)=>row%4==0?1.09f:row%4==1?.91f:row%4==2?1.04f:.96f;

        private static Vector3 GateFaceLerp(Vector3[] face,float u,float v)=>
            Vector3.Lerp(Vector3.Lerp(face[0],face[1],u),Vector3.Lerp(face[3],face[2],u),v);

        private static void GateAshlarStone(Builder mesh,Vector3[] face,Color[] light,
            float u0,float u1,float v0,float v1,int row,int block,int side,float bottom,float top)
        {
            float width=Vector3.Distance(face[0],face[1]);
            float gapX=Mathf.Min(.008f/Mathf.Max(.3f,width),(u1-u0)*.12f);
            float gapY=Mathf.Min(.009f/(top-bottom),(v1-v0)*.12f);
            var outer=new[]{new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1)};
            float a=u0==0?0:u0+gapX,b=u1==1?1:u1-gapX;
            float lo=v0==0?0:v0+gapY,hi=v1==1?1:v1-gapY;
            var stone=new[]{new Vector2(a,lo),new Vector2(b,lo),new Vector2(b,hi),new Vector2(a,hi)};
            for(int edge=0;edge<4;edge++)
            {
                bool mortar=edge==0?lo>v0:edge==1?b<u1:edge==2?hi<v1:a>u0;
                if(!mortar)continue;
                int next=(edge+1)%4;
                GateAshlarPatch(mesh,face,light,new[]{outer[edge],outer[next],stone[next],stone[edge]},
                    side,top-bottom,.72f);
            }
            int first=mesh.V.Count;
            GateAshlarPatch(mesh,face,light,stone,side,top-bottom,1+((block+row*3)%5-2)*.018f);
            GateStoneEdges(mesh,first,.035f,.038f);
        }

        private static void GateAshlarPatch(Builder mesh,Vector3[] face,Color[] light,Vector2[] coords,
            int side,float height,float gain)
        {
            int first=mesh.V.Count;
            mesh.Quad(GateFaceLerp(face,coords[0].x,coords[0].y),GateFaceLerp(face,coords[1].x,coords[1].y),
                GateFaceLerp(face,coords[2].x,coords[2].y),GateFaceLerp(face,coords[3].x,coords[3].y),false,Color.white);
            for(int i=0;i<4;i++)
            {
                Vector2 p=coords[i];
                Color baseLight=Color.Lerp(Color.Lerp(light[0],light[1],p.x),
                    Color.Lerp(light[3],light[2],p.x),p.y);
                float below=(1-p.y)*height,above=p.y*height;
                float soot=.16f*Mathf.Exp(-below/.42f)+.07f*Mathf.Exp(-above/.38f);
                float runA=Mathf.Clamp01(1-Mathf.Abs(p.x-(side==0?.27f:.68f))/.17f);
                float runB=Mathf.Clamp01(1-Mathf.Abs(p.x-(side==0?.78f:.24f))/.11f);
                float wash=(runA*runA*.13f+runB*runB*.075f)*Mathf.SmoothStep(.1f,1,p.y);
                float mineral=Mathf.Clamp01(1-Mathf.Abs(p.x-.49f)/.30f)*
                    Mathf.Clamp01(1-Mathf.Abs(p.y-.32f)/.37f)*.055f;
                float value=gain*(1-soot-wash+mineral);
                // Retain the established whole-face damp/mineral field and local
                // forge hue; only the stone partition and grain source change.
                float warm=mineral*.4f,cool=wash*.12f;
                mesh.C[first+i]=new Color(baseLight.r*value*(1+warm-cool),
                    baseLight.g*value*(1+warm*.2f),baseLight.b*value*(1+cool),1);
            }
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

        private static void GateEnclosureDressedTier(Builder mesh,Builder shell,float height)
        {
            int start=mesh.V.Count;
            var outline=new System.Collections.Generic.List<Vector2>();
            Vector2 low=new Vector2(float.MaxValue,float.MaxValue),high=new Vector2(float.MinValue,float.MinValue);
            for(int i=12;i<shell.V.Count;i+=4)
            {
                Vector3 floor=shell.V[i]-Vector3.up*height;
                FoundrySurface.ToDesign(floor.x,floor.y,out float u,out float v);
                var p=new Vector2(u,v);outline.Add(p);low=Vector2.Min(low,p);high=Vector2.Max(high,p);
            }
            // A single broad stock grid crosses each fascia and its bearing cap.
            // Each joint owns real surface area; no full face remains underneath.
            int columns=Mathf.Max(1,Mathf.RoundToInt((high.x-low.x)/1.25f));
            int rows=Mathf.Max(1,Mathf.RoundToInt((high.y-low.y)/1.25f));
            for(int face=0;face<3;face++)
            {
                int first=face*4;
                var points=new[]{shell.V[first],shell.V[first+1],shell.V[first+2],shell.V[first+3]};
                var light=new[]{shell.C[first],shell.C[first+1],shell.C[first+2],shell.C[first+3]};
                if(face==2)
                {
                    GateEnclosureFasciaPatch(mesh,points,light,0,1,.91f,false);
                    continue;
                }
                Vector3 floorA=points[3]-Vector3.up*height,floorB=points[2]-Vector3.up*height;
                FoundrySurface.ToDesign(floorA.x,floorA.y,out float au,out float av);
                FoundrySurface.ToDesign(floorB.x,floorB.y,out float bu,out float bv);
                float a=face==0?au:av,b=face==0?bu:bv;
                float minimum=face==0?low.x:low.y,maximum=face==0?high.x:high.y;
                int count=face==0?columns:rows;
                for(int part=0;part<count*2-1;part++)
                {
                    Vector2 interval=GateCopingInterval(minimum,maximum,count,part);
                    float left=Mathf.Max(Mathf.Min(a,b),interval.x),right=Mathf.Min(Mathf.Max(a,b),interval.y);
                    if(right-left<.00001f)continue;
                    float t0=(left-a)/(b-a),t1=(right-a)/(b-a);
                    bool joint=part%2==1;
                    GateEnclosureFasciaPatch(mesh,points,light,Mathf.Min(t0,t1),Mathf.Max(t0,t1),
                        joint?.70f:1+(part/2%3-1)*.012f,!joint);
                }
            }
            // Clip the same stock grid to the original chamfered cap. Broad stone
            // interiors, arrises and mortar partition its exact old plane.
            for(int column=0;column<columns*2-1;column++)
            for(int row=0;row<rows*2-1;row++)
            {
                Vector2 u=GateCopingInterval(low.x,high.x,columns,column);
                Vector2 v=GateCopingInterval(low.y,high.y,rows,row);
                var shape=GateClipCoping(outline,0,u.x,true);
                shape=GateClipCoping(shape,0,u.y,false);
                shape=GateClipCoping(shape,1,v.x,true);
                shape=GateClipCoping(shape,1,v.y,false);
                if(shape.Count<3)continue;
                bool joint=column%2==1||row%2==1;
                GateEnclosureCapPatch(mesh,shell,shape,height,joint?.70f:
                    1+((column/2+row/2*2)%3-1)*.012f,!joint);
            }
            // One physical mineral field covers adjoining cap, arris and fascia.
            // Shared points keep identical UVs instead of stretching/restarting an atlas tile.
            GateRecessGrain(mesh,start,mesh.V.Count,Project(1,0)-Project(0,0),0);
        }

        private static Vector2 GateCopingInterval(float low,float high,int count,int part)
        {
            const float halfJoint=.008f;
            int cell=part/2;
            float a=Mathf.Lerp(low,high,cell/(float)count),b=Mathf.Lerp(low,high,(cell+1f)/count);
            if(part%2==1)return new Vector2(b-halfJoint,b+halfJoint);
            return new Vector2(a+(cell>0?halfJoint:0),b-(cell+1<count?halfJoint:0));
        }

        private static void GateEnclosureFasciaPatch(Builder mesh,Vector3[] points,Color[] light,
            float low,float high,float gain,bool dressed)
        {
            int first=mesh.V.Count;
            mesh.Quad(GateFaceLerp(points,low,0),GateFaceLerp(points,high,0),
                GateFaceLerp(points,high,1),GateFaceLerp(points,low,1),false,Color.white);
            for(int i=0;i<4;i++)
            {
                float u=i==0||i==3?low:high,v=i<2?0:1;
                Color c=Color.Lerp(Color.Lerp(light[0],light[1],u),Color.Lerp(light[3],light[2],u),v)*gain;
                c.a=1;mesh.C[first+i]=c;
            }
            if(dressed)GateStoneEdges(mesh,first,.028f,.022f);
        }

        private static void GateEnclosureCapPatch(Builder mesh,Builder shell,
            System.Collections.Generic.List<Vector2> shape,float height,float gain,bool dressed)
        {
            int first=mesh.V.Count,n=shape.Count;
            Vector2 centre=Vector2.zero;foreach(Vector2 p in shape)centre+=p;centre/=n;
            int rings=dressed?2:1;
            for(int ring=0;ring<rings;ring++)
            for(int i=0;i<n;i++)
            {
                // This is a light-catching arris in the existing plane, preserving
                // the exact bearing contact for the next architectural course.
                Vector2 p=ring==0?shape[i]:Vector2.Lerp(shape[i],centre,.055f);
                Vector3 point=Project(p.x,p.y)+Vector3.up*height;
                mesh.V.Add(point);mesh.U.Add(Vector2.zero);
                Color c=GateEnclosureCapLight(shell,point)*gain*(dressed&&ring==0?.85f:1);
                c.a=1;mesh.C.Add(c);
            }
            if(dressed)for(int i=0;i<n;i++)
            {
                int next=(i+1)%n;
                mesh.T.AddRange(new[]{first+i,first+next,first+n+next,first+i,first+n+next,first+n+i});
            }
            int interior=first+(dressed?n:0);
            for(int i=1;i+1<n;i++)mesh.T.AddRange(new[]{interior,interior+i,interior+i+1});
        }

        private static Color GateEnclosureCapLight(Builder shell,Vector3 point)
        {
            // Sample the original lit cap's actual fan, including the shared forge
            // illumination. The new cut never substitutes a neutral painted overlay.
            for(int i=12;i<shell.V.Count;i+=4)
            {
                Vector2 a=shell.V[i],b=shell.V[i+1],c=shell.V[i+2],p=point;
                float denominator=(b.x-a.x)*(c.y-a.y)-(b.y-a.y)*(c.x-a.x);
                float u=((p.x-a.x)*(c.y-a.y)-(p.y-a.y)*(c.x-a.x))/denominator;
                float v=((b.x-a.x)*(p.y-a.y)-(b.y-a.y)*(p.x-a.x))/denominator;
                if(u>=-.0001f&&v>=-.0001f&&u+v<=1.0001f)
                    return shell.C[i]*(1-u-v)+shell.C[i+1]*u+shell.C[i+2]*v;
            }
            return shell.C[12];
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
