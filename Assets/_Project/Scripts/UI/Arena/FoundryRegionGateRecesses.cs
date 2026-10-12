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
                    stone.C[i]=new Color(c.r*light.r/Mathf.Max(.01f,tint.r),
                        c.g*light.g/Mathf.Max(.01f,tint.g),c.b*light.b/Mathf.Max(.01f,tint.b),1);
                }
            }
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
                HeaderStonePatch(stone,At(opening[i]),At(opening[j]),
                    At(opening[j])+inward,At(opening[i])+inward,reveal,origin,along,across);
                GateRecessGrain(stone,start,stone.V.Count,At(opening[j])-At(opening[i]),i);
            }
            // Broad shoulder and sill stones replace the earlier rows of tiny
            // surface ashlar. A single joint separates the bearing from its frame.
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Color body=tint*(i==0?.86f:i==2||i==3?.94f:.91f);body.a=1;
                GateRecessDressedFace(stone,At(border[i]),At(border[j]),At(frame[j]),At(frame[i]),body,.035f,i);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Color dressed=tint*(i==0?1.12f:i==2||i==3?1.07f:1.04f);dressed.a=1;
                GateRecessDressedFace(stone,At(frame[i]),At(frame[j]),At(opening[j]),At(opening[i]),dressed,.055f,i+5);
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
    }
}
