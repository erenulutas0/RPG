using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // A stone shaft with two real blind rebates. Its outside is the existing
        // tapered octagonal prism; only the two visible wall faces are replaced.
        private static void GateRecessedShaft(Builder stone,Vector4 box,float bottom,
            float top,float inset,float shade,float depth)
        {
            var shell=new Builder();
            LitFoundryTier(shell,box.x,box.y,box.z,box.w,bottom,top,inset);
            // Keep the old face order (side, front), then its closed corner and
            // cap. No solid wall is emitted underneath either recessed opening.
            for(int face=0;face<2;face++)
            {
                int first=face*4;
                var points=new[]{shell.V[first],shell.V[first+1],shell.V[first+2],shell.V[first+3]};
                Color tint=shell.C[first+2]*shade;tint.a=1;
                Vector3 inward=face==0?Project(box.x,box.y+depth)-Project(box.x,box.y):
                    Project(box.x+depth,box.y)-Project(box.x,box.y);
                int start=stone.V.Count;
                GateRecessedFace(stone,points,inward,tint);
                // Preserve the original tier's floor-relative forge lighting.
                // A single top-corner sample used to darken the entire tall shaft
                // as if every stone were directly underneath its coping.
                for(int i=start;i<stone.V.Count;i++)
                {
                    Vector2 p=GateRecessCoordinates(points,stone.V[i]);
                    Color light=Color.Lerp(Color.Lerp(shell.C[first],shell.C[first+1],p.x),
                        Color.Lerp(shell.C[first+3],shell.C[first+2],p.x),p.y)*shade;
                    Color c=stone.C[i];
                    Color sampled=new Color(c.r*light.r/Mathf.Max(.01f,tint.r),
                        c.g*light.g/Mathf.Max(.01f,tint.g),c.b*light.b/Mathf.Max(.01f,tint.b),1);
                    stone.C[i]=GateStoneContact(sampled,p,top-bottom);
                }
            }
            int closedCorner=stone.V.Count;
            for(int first=8;first<shell.V.Count;first+=4)
            {
                int output=stone.V.Count;
                for(int i=0;i<4;i++)
                {
                    stone.V.Add(shell.V[first+i]);stone.U.Add(shell.U[first+i]);
                    Color tint=shell.C[first+i]*shade;tint.a=1;stone.C.Add(tint);
                }
                stone.T.AddRange(new[]{output,output+1,output+2,output,output+2,output+3});
            }
            GateRecessGrain(stone,closedCorner,closedCorner+4,Vector3.up,2);
            GateRecessGrain(stone,closedCorner+4,stone.V.Count,Project(0,1)-Project(0,0),3);
        }

        private static Vector2[] GateRecessOpening()=>new[]{
            new Vector2(.28f,.17f),new Vector2(.72f,.17f),
            new Vector2(.72f,.69f),new Vector2(.5f,.84f),new Vector2(.28f,.69f)};

        private static void GateRecessedFace(Builder stone,Vector3[] face,Vector3 inward,Color tint)
        {
            var opening=GateRecessOpening();
            var frame=new[]{new Vector2(.14f,.08f),new Vector2(.86f,.08f),
                new Vector2(.86f,.75f),new Vector2(.5f,.94f),new Vector2(.14f,.75f)};
            // Five matching outer stations partition the original whole wall.
            // There is no hidden full-face rectangle across the new blind niche.
            var border=new[]{Vector2.zero,Vector2.right,Vector2.one,new Vector2(.5f,1),Vector2.up};
            Vector3 At(Vector2 p)=>GateFaceLerp(face,p.x,p.y);
            // The broad frame is cut into the shaft rather than painted flat on
            // its front. Its inner arris consumes only a quarter of the existing
            // reveal depth; backing and exterior bearing stations remain fixed.
            Vector3 Throat(Vector2 p)=>At(p)+inward*.25f;
            Vector3 origin=face[0],along=face[1],across=face[3];
            Vector3 hub=At(new Vector2(.5f,.46f))+inward;
            Color back=tint*.75f;back.a=1;
            // Backing first, then all five connected depth returns, then the
            // front stones. The near faces mask projected hidden return edges.
            int backing=stone.V.Count;
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                HeaderStonePatch(stone,At(opening[i])+inward,At(opening[j])+inward,hub,hub,
                    back,origin+inward,along+inward,across+inward);
            }
            // One continuous mineral field across the five backing triangles.
            // Its lower reflected light recedes into a cooler shaded head; it
            // remains distinctly darker than the real returns and dressed frame.
            GateRecessGrain(stone,backing,stone.V.Count,across-origin,0);
            for(int i=backing;i<stone.V.Count;i++)
            {
                Vector2 p=GateRecessCoordinates(face,stone.V[i]-inward);
                float rise=Mathf.InverseLerp(.17f,.84f,p.y);
                float light=Mathf.Lerp(1.03f,.80f,rise);
                Color c=stone.C[i];
                stone.C[i]=new Color(c.r*light,c.g*light,c.b*light*1.025f,1);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                float light=i==0?1.03f:i==1?.64f:i==2?.62f:i==3?.77f:.90f;
                Color reveal=tint*light;reveal.a=1;
                int start=stone.V.Count;
                HeaderStonePatch(stone,Throat(opening[i]),Throat(opening[j]),
                    At(opening[j])+inward,At(opening[i])+inward,reveal,origin,along,across);
                GateRecessGrain(stone,start,stone.V.Count,At(opening[j])-At(opening[i]),i);
            }
            // Broad jamb courses share their cuts across the body/frame contact.
            // The sill and outer head bearings remain single substantial stones;
            // two fitted pieces follow each sloping side of the pointed head.
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Color body=tint*(i==0?.86f:i==2||i==3?.94f:.91f);body.a=1;
                GateRecessCoursedFace(stone,At(border[i]),At(border[j]),At(frame[j]),At(frame[i]),
                    body,.035f,i,i==1||i==4?3:1,false);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Color dressed=tint*(i==0?1.12f:i==2||i==3?1.07f:1.04f);dressed.a=1;
                int first=stone.V.Count;
                Vector3 a=At(frame[i]),b=At(frame[j]),c=Throat(opening[j]),d=Throat(opening[i]);
                GateRecessCoursedFace(stone,a,b,c,d,dressed,.055f,i+5,i==0?1:i==1||i==4?3:2,true);
                // Projected scenery has no light normals. Describe the receding
                // plane on its existing vertices: the far jamb/head lose light
                // toward the throat, while the sill keeps its original warmth.
                // Outer bearing edges stay unchanged; no bright perimeter is added.
                float innerGain=i==0?1:i==1?.80f:i==2?.76f:i==3?.90f:.98f;
                Vector2 outside=b-a,inside=d-c;
                for(int k=first;k<stone.V.Count;k++)
                {
                    Vector2 p=stone.V[k];
                    float front=Mathf.Abs(FlagCross(outside,p-(Vector2)a))/outside.magnitude;
                    float rear=Mathf.Abs(FlagCross(inside,p-(Vector2)c))/inside.magnitude;
                    float reach=front/Mathf.Max(.00001f,front+rear);
                    float gain=Mathf.Lerp(1,innerGain,Mathf.SmoothStep(0,1,reach));
                    Color color=stone.C[k];
                    stone.C[k]=new Color(color.r*gain,color.g*gain,color.b*gain,1);
                }
            }
        }

        private static void GateRecessCoursedFace(Builder mesh,Vector3 a,Vector3 b,
            Vector3 c,Vector3 d,Color tint,float bevel,int station,int pieces,bool bordersOpening)
        {
            int first=mesh.V.Count;
            const float halfJoint=.0045f;
            Vector3 Inner(float t)=>bordersOpening?GateRecessOpeningPoint(d,c,a,t):Vector3.Lerp(d,c,t);
            for(int piece=0;piece<pieces;piece++)
            {
                float low=piece/(float)pieces,high=(piece+1f)/pieces;
                float start=low+(piece==0?0:halfJoint),end=high-(piece+1==pieces?0:halfJoint);
                // The same cut parameter meets the adjacent body/jamb face.
                // Narrow mortar strips own the gap; no complete bed is retained.
                if(piece>0)
                {
                    float before=low-halfJoint,after=low+halfJoint;
                    HeaderStonePatch(mesh,Vector3.Lerp(a,b,before),Vector3.Lerp(a,b,after),
                        Inner(after),Inner(before),tint*.72f,a,b,d);
                }
                float grainTone=pieces==1?1:piece%3==0?.985f:piece%3==1?1.015f:.995f;
                GateRecessIndexedStone(mesh,Vector3.Lerp(a,b,start),Vector3.Lerp(a,b,end),
                    Inner(end),Inner(start),tint*grainTone,bevel);
            }
            // One original physical face keeps one continuous mineral field;
            // neither the course joints nor bevels restart the atlas sampling.
            Vector3 axis=(b-a).sqrMagnitude>(d-a).sqrMagnitude?b-a:d-a;
            GateRecessGrain(mesh,first,mesh.V.Count,axis,station);
        }

        private static Vector3 GateRecessOpeningPoint(Vector3 start,Vector3 end,Vector3 stone,float t)
        {
            if(t<=0)return start;
            if(t>=1)return end;
            double dx=(double)end.x-start.x,dy=(double)end.y-start.y;
            double x=start.x+dx*t,y=start.y+dy*t;
            var point=new Vector3((float)x,(float)y,(float)(start.z+((double)end.z-start.z)*t));
            double side=dx*((double)stone.y-start.y)-dy*((double)stone.x-start.x)>=0?1:-1;
            double Distance(Vector3 p)=>side*(dx*((double)p.y-start.y)-dy*((double)p.x-start.x));
            if(Distance(point)>=0)return point;
            // A rounded split point must not enter the unchanged throat. Move
            // only to its nearest adjacent representable stone-side coordinate;
            // the original end stations and the body/frame contact stay exact.
            var stepX=new Vector3(GateRecessNextFloat(point.x,-dy*side),point.y,point.z);
            var stepY=new Vector3(point.x,GateRecessNextFloat(point.y,dx*side),point.z);
            bool xOutside=Distance(stepX)>=0,yOutside=Distance(stepY)>=0;
            if(xOutside&&yOutside)
            {
                double Error(Vector3 p)=>((double)p.x-x)*(p.x-x)+((double)p.y-y)*(p.y-y);
                return Error(stepX)<=Error(stepY)?stepX:stepY;
            }
            if(xOutside)return stepX;
            if(yOutside)return stepY;
            return new Vector3(stepX.x,stepY.y,point.z);
        }

        private static float GateRecessNextFloat(float value,double direction)
        {
            if(direction==0)return value;
            if(value==0)return direction>0?float.Epsilon:-float.Epsilon;
            int bits=System.BitConverter.SingleToInt32Bits(value);
            bits+=(direction>0)==(value>0)?1:-1;
            return System.BitConverter.Int32BitsToSingle(bits);
        }

        private static void GateRecessIndexedStone(Builder mesh,Vector3 a,Vector3 b,
            Vector3 c,Vector3 d,Color tint,float bevel)
        {
            int first=mesh.V.Count;
            Vector3 centre=(a+b+c+d)*.25f;
            var outer=new[]{a,b,c,d};
            // Shared corners interpolate the directional arris light. Eight
            // indexed vertices replace twenty repeated quad corners, allowing
            // fitted courses without increasing the niche's old vertex budget.
            var edgeLight=new[]{.855f,.82f,.985f,1.02f};
            for(int ring=0;ring<2;ring++)for(int i=0;i<4;i++)
            {
                mesh.V.Add(ring==0?outer[i]:Vector3.Lerp(outer[i],centre,bevel));
                mesh.U.Add(Vector2.zero);
                Color c0=tint*(ring==0?edgeLight[i]:1);c0.a=1;mesh.C.Add(c0);
            }
            mesh.T.AddRange(new[]{first+4,first+5,first+6,first+4,first+6,first+7});
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                mesh.T.AddRange(new[]{first+edge,first+next,first+4+next,
                    first+edge,first+4+next,first+4+edge});
            }
        }

        private static Vector2 GateRecessCoordinates(Vector3[] face,Vector3 p)
        {
            Vector3 u=face[1]-face[0],v=face[3]-face[0],q=p-face[0];
            float determinant=u.x*v.y-u.y*v.x;
            if(Mathf.Abs(determinant)<.000001f)return Vector2.zero;
            float y=Mathf.Clamp01((u.x*q.y-u.y*q.x)/determinant);
            Vector3 a=Vector3.Lerp(face[0],face[3],y),b=Vector3.Lerp(face[1],face[2],y);
            Vector3 edge=b-a;
            float x=edge.sqrMagnitude>.000001f?Mathf.Clamp01(Vector3.Dot(p-a,edge)/edge.sqrMagnitude):0;
            return new Vector2(x,y);
        }

        private static void GateRecessDressedFace(Builder mesh,Vector3 a,Vector3 b,
            Vector3 c,Vector3 d,Color tint,float bevel,int station)
        {
            int first=mesh.V.Count;
            HeaderDressedFace(mesh,a,b,c,d,tint,bevel);
            // The long grain axis follows the actual block instead of stretching
            // the same square across a tall jamb and restarting on every bevel.
            Vector3 axis=(b-a).sqrMagnitude>(d-a).sqrMagnitude?b-a:d-a;
            GateRecessGrain(mesh,first,mesh.V.Count,axis,station);
        }

        private static void GateRecessGrain(Builder mesh,int first,int end,Vector3 direction,int station)
        {
            Vector2 axis=((Vector2)direction).normalized;
            if(axis.sqrMagnitude<.5f)axis=Vector2.up;
            Vector2 normal=new Vector2(-axis.y,axis.x);
            Vector2 low=new Vector2(float.MaxValue,float.MaxValue),high=new Vector2(float.MinValue,float.MinValue);
            for(int i=first;i<end;i++)
            {
                Vector2 p=mesh.V[i];var local=new Vector2(Vector2.Dot(p,axis),Vector2.Dot(p,normal));
                low=Vector2.Min(low,local);high=Vector2.Max(high,local);
            }
            Vector2 span=high-low;
            float scale=Mathf.Min(.095f,Mathf.Min(.355f/Mathf.Max(.001f,span.x),.125f/Mathf.Max(.001f,span.y)));
            // Joint-free interior of the atlas's broad middle-row stone. A shared
            // physical scale preserves grain on thin returns and their bevels.
            // The bounded offset varies the cut without sampling a mortar seam.
            Vector2 origin=new Vector2(.035f+(station%3)*.004f,.438f+(station%2)*.004f);
            for(int i=first;i<end;i++)
            {
                Vector2 p=mesh.V[i];var local=new Vector2(Vector2.Dot(p,axis),Vector2.Dot(p,normal));
                mesh.U[i]=origin+(local-low)*scale;
            }
        }

        private static Color GateStoneContact(Color sampled,Vector2 face,float height)
        {
            // Contact belongs to the whole supported face, not each individual
            // block. Adjacent stones share the same restrained crown/foot wash.
            // Multiplication preserves the source forge-light ratio and never
            // substitutes a grey or orange overlay for authored illumination.
            float reach=Mathf.Clamp(height*.32f,.06f,.28f);
            float crown=.085f*Mathf.Exp(-(1-face.y)*height/reach);
            float bearing=.045f*Mathf.Exp(-face.y*height/reach);
            float stream=Mathf.Clamp01(1-Mathf.Abs(face.x-.32f)/.20f);
            float run=.045f*stream*stream*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,1,face.y));
            float gain=1-crown-bearing-run;
            return new Color(sampled.r*gain,sampled.g*gain,sampled.b*gain,1);
        }
    }
}
