using UnityEngine;

namespace Cryptforge.UI
{
    // Supported portal parapet; existing footprint, height envelope and door anchors.
    public sealed partial class FoundryRegionView
    {
        private static void BuildGateHeaderFinish(Builder stone,float height)
        {
            // One continuous lintel and a low interrupted coping make the doorway
            // a fortified industrial wall. The former nested shoulders climbed
            // into a temple-like crest and made the door look broad and squat.
            GateCraftTier(stone,GateU(32.08f),-.78f,GateU(33.92f),10.78f,height+.43f,height+.72f,.06f,.90f);
            foreach(bool left in new[]{true,false})
            {
                float v0=left?-.68f:6.65f,v1=left?3.35f:10.68f;
                GateCraftTier(stone,GateU(32.18f),v0,GateU(33.82f),v1,height+.72f,height+.97f,.035f,.91f);
            }
            // Four broad coping ends break the silhouette quietly. Their nearly
            // vertical sides sit directly on the full masonry course beneath;
            // no sloping roof, tall crenellations or unsupported trim is added.
            foreach(var span in new[]{new Vector2(-.60f,1.20f),new Vector2(2.25f,3.35f),
                new Vector2(6.65f,7.75f),new Vector2(8.80f,10.60f)})
                GateCraftTier(stone,GateU(32.22f),span.x,GateU(33.78f),span.y,
                    height+.97f,height+1.16f,.025f,.93f);
            // The compact lock recess remains physically backed and side-seated.
            // Its cap follows the same flat elevation as the flanking coping,
            // so the door seal stays the principal emblem from the walking lane.
            GateCraftTier(stone,GateU(32.65f),3.35f,GateU(33.85f),6.65f,height+.72f,height+.98f,.035f,.88f);
            GateCraftTier(stone,GateU(32.15f),3.35f,GateU(32.67f),3.95f,height+.72f,height+.98f,.025f,.88f);
            GateCraftTier(stone,GateU(32.15f),6.05f,GateU(32.67f),6.65f,height+.72f,height+.98f,.025f,.88f);
            GateCraftTier(stone,GateU(32.22f),3.38f,GateU(33.78f),6.62f,height+.98f,height+1.16f,.025f,.93f);
            // Shallow corbels carry the continuous lower cornice. Keeping the
            // existing stations also preserves its separate iron strap contacts.
            foreach(float v in new[]{-.50f,2.20f,4.70f,7.20f,9.90f})
            {
                Vector3 a=Project(GateU(32.05f),v)+Vector3.up*(height-.18f);
                Vector3 b=Project(GateU(32.05f),v+.40f)+Vector3.up*(height-.18f);
                Vector3 c=Project(GateU(32.0f),v+.53f)+Vector3.up*(height+.08f);
                Vector3 d=Project(GateU(32.0f),v-.13f)+Vector3.up*(height+.08f);
                Vector3 back=Project(GateU(32.46f),v)-Project(GateU(32.0f),v);
                HeaderStonePatch(stone,a,a+back,d+back,d,new Color(.35f,.39f,.48f),a,a+back,d);
                HeaderStonePatch(stone,d,c,c+back,d+back,new Color(.67f,.70f,.77f),d,c,d+back);
                HeaderDressedFace(stone,a,b,c,d,new Color(.53f,.57f,.65f),.06f);
            }
        }

        private static Vector2[] GateCartoucheOutline()=>new[]{
            new Vector2(-.79f,.52f),new Vector2(.79f,.52f),new Vector2(1.06f,.71f),
            new Vector2(.93f,1.16f),new Vector2(.52f,1.42f),new Vector2(-.52f,1.42f),
            new Vector2(-.93f,1.16f),new Vector2(-1.06f,.71f)};

        // Compress only the relief above its original bearing line. Both stone
        // and the cast lock use this mapping, keeping the lock inside its reveal.
        private static float GateCartoucheRise(float authored)=>.52f+(authored-.52f)*.64f;

        private static Vector3 GateCartouchePoint(Vector2 face,float u,float height)=>
            Project(u,5+face.x)+Vector3.up*(height+GateCartoucheRise(face.y));

        private static void BuildGateCartouche(Builder stone,float height)
        {
            var outer=GateCartoucheOutline();int n=outer.Length;
            var dressed=new Vector2[n];var inner=new Vector2[n];var back=new Vector2[n];
            var centre=new Vector2(0,.99f);
            for(int i=0;i<n;i++)
            {
                dressed[i]=Vector2.Lerp(centre,outer[i],.90f);
                inner[i]=new Vector2(outer[i].x*.67f,Mathf.Lerp(.99f,outer[i].y,.55f));
                back[i]=new Vector2(outer[i].x*.63f,Mathf.Lerp(.99f,outer[i].y,.52f));
            }
            Vector3 origin=GateCartouchePoint(new Vector2(-1.06f,.52f),GateU(32.12f),height);
            Vector3 along=GateCartouchePoint(new Vector2(1.06f,.52f),GateU(32.12f),height);
            Vector3 across=GateCartouchePoint(new Vector2(-1.06f,1.42f),GateU(32.12f),height);
            // Emit depth returns first; the recessed backing and front stone then
            // cover hidden return faces in the fixed painter view.
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                HeaderStonePatch(stone,GateCartouchePoint(outer[i],GateU(32.66f),height),
                    GateCartouchePoint(outer[j],GateU(32.66f),height),GateCartouchePoint(outer[j],GateU(32.18f),height),
                    GateCartouchePoint(outer[i],GateU(32.18f),height),new Color(.38f,.42f,.51f),origin,along,across);
            }
            // Continuous rear stone is a real recessed backing, not a dark patch
            // over the old solid box. The opening starts above the supporting lintel.
            Vector3 hub=GateCartouchePoint(centre,GateU(32.60f),height);
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                HeaderStonePatch(stone,GateCartouchePoint(back[i],GateU(32.60f),height),
                    GateCartouchePoint(back[j],GateU(32.60f),height),hub,hub,
                    new Color(.24f,.28f,.35f),origin,along,across);
            }
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                Color reveal=i==0?new Color(.61f,.64f,.70f):new Color(.30f,.35f,.44f);
                HeaderStonePatch(stone,GateCartouchePoint(inner[i],GateU(32.12f),height),GateCartouchePoint(inner[j],GateU(32.12f),height),
                    GateCartouchePoint(back[j],GateU(32.60f),height),GateCartouchePoint(back[i],GateU(32.60f),height),reveal,origin,along,across);
            }
            for(int i=0;i<n;i++)
            {
                int j=(i+1)%n;
                Vector3 a=GateCartouchePoint(outer[i],GateU(32.18f),height),b=GateCartouchePoint(outer[j],GateU(32.18f),height);
                Vector3 da=GateCartouchePoint(dressed[i],GateU(32.12f),height),db=GateCartouchePoint(dressed[j],GateU(32.12f),height);
                Vector3 ia=GateCartouchePoint(inner[i],GateU(32.12f),height),ib=GateCartouchePoint(inner[j],GateU(32.12f),height);
                // The outer sidewalls tie the shaped face physically into the rear
                // core; the two receding profiles give the dressed stone real relief.
                Color edge=i<3?new Color(.60f,.63f,.70f):new Color(.48f,.52f,.62f);
                HeaderStonePatch(stone,a,b,db,da,edge,origin,along,across);
                Color face=new Color(.62f,.65f,.72f)*(i%3==0?.95f:1f);face.a=1;
                HeaderStonePatch(stone,Vector3.Lerp(da,db,.008f),Vector3.Lerp(da,db,.992f),
                    Vector3.Lerp(ia,ib,.992f),Vector3.Lerp(ia,ib,.008f),face,origin,along,across);
            }
        }

        private static void BuildGateCartoucheLock(Builder iron,float height)
        {
            // One cast locking plate stays entirely within the recessed opening.
            // Its dark iron mass is subordinate to the door's large painted seal.
            Vector3 At(float v,float z,float depth=0)=>Project(GateU(32.58f)+depth,5+v)+Vector3.up*(height+GateCartoucheRise(z));
            var shape=new[]{new Vector2(-.39f,.86f),new Vector2(.39f,.86f),new Vector2(.47f,.92f),
                new Vector2(.47f,1.07f),new Vector2(.39f,1.13f),new Vector2(-.39f,1.13f),
                new Vector2(-.47f,1.07f),new Vector2(-.47f,.92f)};
            Vector3 hub=At(0,.995f);
            for(int i=0;i<shape.Length;i++)
            {
                Vector2 a=shape[i],b=shape[(i+1)%shape.Length];
                GateIronPatch(iron,At(a.x,a.y,.045f),At(b.x,b.y,.045f),At(b.x,b.y),At(a.x,a.y),new Color(.31f,.36f,.45f));
                GateIronPatch(iron,At(a.x,a.y),At(b.x,b.y),hub,hub,new Color(.48f,.53f,.61f));
            }
            // A single inset bronze key slot and two broad pins imply machinery;
            // no glow, floating glyph or second competing emblem is introduced.
            GateIronPatch(iron,At(-.075f,.90f,-.006f),At(.075f,.90f,-.006f),
                At(.075f,1.09f,-.006f),At(-.075f,1.09f,-.006f),new Color(.79f,.66f,.44f));
            GateIronPatch(iron,At(-.020f,.93f,-.008f),At(.020f,.93f,-.008f),
                At(.020f,1.06f,-.008f),At(-.020f,1.06f,-.008f),new Color(.18f,.21f,.28f));
            foreach(float v in new[]{-.31f,.31f})
            {
                Vector3 a=At(v-.05f,.955f,-.008f),b=At(v+.05f,.955f,-.008f);
                Vector3 c=At(v+.05f,1.035f,-.008f),d=At(v-.05f,1.035f,-.008f);
                GateIronPatch(iron,a,b,c,d,new Color(.62f,.65f,.70f));
            }
        }

        private static void GateCraftTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset,float shade=1)
        {
            int first=mesh.V.Count;
            LitFoundryTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            // Keep the exact original battered faces, clipped corners and cap. The
            // original face remains the mortar backing beneath its authored courses.
            int last=mesh.V.Count;
            for(int i=first;i<last;i++)
            {
                Color c=mesh.C[i];mesh.C[i]=new Color(c.r*shade,c.g*shade,c.b*shade,1);
            }
            for(int face=0;face<3;face++)
                GateCraftCourses(mesh,first+face*4,top-bottom,face,
                    Mathf.RoundToInt(u0*11+v0*17+bottom*23));
            // Recess the retained cap into a mortar bed. Authored coping blocks
            // above it restore real arrises and joints instead of a blank slab.
            for(int i=first+12;i<last;i++)
            {
                Vector3 floor=mesh.V[i]-Vector3.up*top;
                FoundrySurface.ToDesign(floor.x,floor.y,out float u,out float v);
                float x=Mathf.InverseLerp(u0,u1,u),y=Mathf.InverseLerp(v0,v1,v);
                mesh.U[i]=new Vector2(.279f+x*.126f,.657f+y*.085f);
                mesh.V[i]+=Vector3.down*.022f;
                Color c=mesh.C[i];float wash=.79f-.035f*y;
                mesh.C[i]=new Color(c.r*wash,c.g*wash,c.b*wash,1);
            }
            Color cap=mesh.C[first+12]/.79f;cap.a=1;
            GateCraftCoping(mesh,u0,v0,u1,v1,top,inset,cap);
        }

        private static void GateCraftCoping(Builder mesh,float u0,float v0,float u1,float v1,
            float height,float inset,Color tone)
        {
            float corner=Mathf.Min(.18f,Mathf.Min(u1-u0,v1-v0)*.15f);
            float cut=Mathf.Min(corner*.75f,(Mathf.Min(u1-u0,v1-v0)-2*inset)*.25f);
            float a=u0+inset,b=v0+inset,c=u1-inset,d=v1-inset;
            var outline=new System.Collections.Generic.List<Vector2>{new Vector2(a+cut,b),new Vector2(c-cut,b),
                new Vector2(c,b+cut),new Vector2(c,d-cut),new Vector2(c-cut,d),new Vector2(a+cut,d),
                new Vector2(a,d-cut),new Vector2(a,b+cut)};
            // Long coping courses follow the portal. Short returns need only one
            // or two stones, avoiding a tiled cap or excessive microscopic seams.
            int columns=Mathf.Max(1,Mathf.RoundToInt((c-a)/1.1f));
            int rows=Mathf.Max(1,Mathf.RoundToInt((d-b)/1.25f));
            for(int row=rows-1;row>=0;row--)
            for(int column=columns-1;column>=0;column--)
            {
                float left=Mathf.Lerp(a,c,column/(float)columns),right=Mathf.Lerp(a,c,(column+1f)/columns);
                float low=Mathf.Lerp(b,d,row/(float)rows),high=Mathf.Lerp(b,d,(row+1f)/rows);
                const float gap=.007f;
                var shape=GateClipCoping(outline,0,left+(column==0?0:gap),true);
                shape=GateClipCoping(shape,0,right-(column==columns-1?0:gap),false);
                shape=GateClipCoping(shape,1,low+(row==0?0:gap),true);
                shape=GateClipCoping(shape,1,high-(row==rows-1?0:gap),false);
                if(shape.Count<3)continue;
                Vector2 centre=Vector2.zero;foreach(Vector2 point in shape)centre+=point;
                centre/=shape.Count;
                int first=mesh.V.Count,n=shape.Count;
                float bevel=Mathf.Min(.075f,.024f/Mathf.Max(.2f,Mathf.Min(right-left,high-low)));
                float variation=1+((row+column*3)%3-1)*.018f;
                for(int ring=0;ring<2;ring++)
                for(int i=0;i<n;i++)
                {
                    Vector2 p=ring==0?shape[i]:Vector2.Lerp(shape[i],centre,bevel);
                    mesh.V.Add(Project(p.x,p.y)+Vector3.up*(height-(ring==0?.022f:0)));
                    mesh.U.Add(new Vector2(.28f+Mathf.Clamp01((p.x-left)/1.2f)*.12f,
                        .658f+Mathf.Clamp01((p.y-low)/1.4f)*.080f));
                    Color tint=tone*variation*(ring==0?.73f:1f);tint.a=1;mesh.C.Add(tint);
                }
                for(int i=0;i<n;i++)
                {
                    int next=(i+1)%n;
                    mesh.T.AddRange(new[]{first+i,first+next,first+n+next,first+i,first+n+next,first+n+i});
                }
                // The upper face covers the far bevel in the fixed painter view.
                for(int i=1;i+1<n;i++)mesh.T.AddRange(new[]{first+n,first+n+i,first+n+i+1});
            }
        }

        private static System.Collections.Generic.List<Vector2> GateClipCoping(
            System.Collections.Generic.List<Vector2> input,int axis,float boundary,bool greater)
        {
            var result=new System.Collections.Generic.List<Vector2>();
            if(input.Count==0)return result;
            Vector2 previous=input[input.Count-1];float previousValue=axis==0?previous.x:previous.y;
            bool previousInside=greater?previousValue>=boundary:previousValue<=boundary;
            foreach(Vector2 point in input)
            {
                float value=axis==0?point.x:point.y;
                bool inside=greater?value>=boundary:value<=boundary;
                if(inside!=previousInside)
                {
                    Vector2 hit=Vector2.Lerp(previous,point,(boundary-previousValue)/(value-previousValue));
                    if(result.Count==0||(result[result.Count-1]-hit).sqrMagnitude>1e-10f)result.Add(hit);
                }
                if(inside&&(result.Count==0||(result[result.Count-1]-point).sqrMagnitude>1e-10f))result.Add(point);
                previous=point;previousValue=value;previousInside=inside;
            }
            if(result.Count>1&&(result[0]-result[result.Count-1]).sqrMagnitude<=1e-10f)result.RemoveAt(result.Count-1);
            return result;
        }

        private static void GateCraftCourses(Builder mesh,int first,float height,int side,int seed)
        {
            var face=new[]{mesh.V[first],mesh.V[first+1],mesh.V[first+2],mesh.V[first+3]};
            var light=new[]{mesh.C[first],mesh.C[first+1],mesh.C[first+2],mesh.C[first+3]};
            float width=Vector3.Distance(face[0],face[1]);
            for(int i=0;i<4;i++)
            {
                mesh.U[first+i]=new Vector2(i==0||i==3?.279f:.406f,i<2?.657f:.742f);
                Color c=light[i];mesh.C[first+i]=new Color(c.r*.81f,c.g*.82f,c.b*.84f,1);
            }
            if(side==2||width<.25f)return;
            int rows=Mathf.Max(1,Mathf.RoundToInt(height/.48f));
            int columns=Mathf.Max(1,Mathf.RoundToInt(width/.83f));
            float total=0;for(int row=0;row<rows;row++)total+=GateCourseWeight(row);
            float y=0;
            for(int row=0;row<rows;row++)
            {
                float next=y+GateCourseWeight(row)/total;
                float x=row%2==0?0:-.42f/columns;int column=0;
                while(x<1)
                {
                    float advance=(column%3==0?1.11f:column%3==1?.93f:.96f)/columns;
                    float start=Mathf.Max(0,x),end=Mathf.Min(1,x+advance);
                    float gapX=.006f/width,gapY=.006f/Mathf.Max(.1f,height);
                    float a=start==0?0:start+gapX,b=end==1?1:end-gapX;
                    float lo=row==0?0:y+gapY,hi=row==rows-1?1:next-gapY;
                    if((b-a)*width>.045f)
                        GateCraftStone(mesh,face,light,a,b,lo,hi,width,height,seed+row*31+column*7+side*13);
                    x+=advance;column++;
                }
                y=next;
            }
        }

        private static void GateCraftStone(Builder mesh,Vector3[] face,Color[] light,float left,float right,
            float bottom,float top,float width,float height,int seed)
        {
            // A few arrises lose a small corner; most dressed stones remain whole.
            // Every contour stays inside its course and the retained bed covers it.
            int rhythm=Mathf.Abs(seed);
            float chip= rhythm%7==0?.018f:0;
            float dx=Mathf.Min(chip/width,(right-left)*.08f),dy=Mathf.Min(chip/height,(top-bottom)*.10f);
            var contour=chip>0?new[]{new Vector2(left,bottom),new Vector2(right,bottom),
                new Vector2(right,top-dy),new Vector2(right-dx,top),new Vector2(left,top)}:
                new[]{new Vector2(left,bottom),new Vector2(right,bottom),new Vector2(right,top),new Vector2(left,top)};
            int first=mesh.V.Count;
            float variation=1+(rhythm%5-2)*.012f;
            float texX=.278f+(rhythm%4)*.004f,texY=.657f+((rhythm/4)%3)*.004f;
            foreach(Vector2 p in contour)
            {
                mesh.V.Add(GateFaceLerp(face,p.x,p.y));
                // Grain scale follows physical stone dimensions, bounded within the
                // atlas's quiet mineral patch. Thin courses do not stretch it tall.
                mesh.U.Add(new Vector2(Mathf.Min(.42f,texX+(p.x-left)*width*.092f),
                    Mathf.Min(.749f,texY+(p.y-bottom)*height*.115f)));
                Color c=Color.Lerp(Color.Lerp(light[0],light[1],p.x),Color.Lerp(light[3],light[2],p.x),p.y);
                float damp=.095f*Mathf.Exp(-(1-p.y)*height/.22f);
                float foot=.055f*Mathf.Exp(-p.y*height/.26f);
                float run=Mathf.Clamp01(1-Mathf.Abs(p.x-.31f)/.14f)*.045f*p.y;
                float mineral=.035f*Mathf.Sin(p.x*Mathf.PI)*Mathf.Sin(p.y*Mathf.PI);
                float value=variation*(1-damp-foot-run+mineral);
                mesh.C.Add(new Color(c.r*value*(1+mineral*.30f),c.g*value,c.b*value*(1+run*.2f),1));
            }
            for(int i=1;i+1<contour.Length;i++)mesh.T.AddRange(new[]{first,first+i,first+i+1});
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
