using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static bool UseFlagContour(int zone,bool repair,float u0,float v0,float u1,float v1)
        {
            return zone!=1&&!repair&&(u1-u0)*(v1-v0)>=1&&
                !(u0<FoundrySurface.RampStart&&u1>FoundrySurface.RampStart)&&
                !(u0<FoundrySurface.RampEnd&&u1>FoundrySurface.RampEnd);
        }

        // Counter-clockwise hand-quarried outline wholly inside the complete joint bed.
        // Small unequal side setbacks avoid a regular octagonal tile. Most corner cuts
        // are almost square; a minority carry a larger broken quarry shoulder.
        private static Vector2[] FlagContour(float u0,float v0,float u1,float v1,float inset,int seedU,int seedV)
        {
            float size=Mathf.Min(u1-u0,v1-v0);
            float exposure=PavingWearExposure((u0+u1)*.5f,(v0+v1)*.5f);
            int wornCorner=(int)(PavingHash(seedU,seedV,787)&3);
            var corners=new Vector2[4];
            for(int k=0;k<4;k++)
            {
                uint hash=PavingHash(seedU+k*17,seedV-k*11,701);
                float x=inset+size*(.004f+.024f*(hash&255)/255f);
                float y=inset+size*(.004f+.024f*((hash>>8)&255)/255f);
                corners[k]=new Vector2(k==0||k==3?u0+x:u1-x,k<2?v0+y:v1-y);
            }
            var result=new Vector2[8];
            for(int k=0;k<4;k++)
            {
                uint hash=PavingHash(seedU+k*23,seedV-k*7,743);
                // One worked shoulder responds to its location; the other corners
                // remain mostly dressed. Avoid four equally broken octagon corners.
                float cut=size*(k==wornCorner?.035f+.11f*exposure:.02f);
                cut*=Mathf.Lerp(.65f,1.35f,((hash>>8)&255)/255f);
                Vector2 c=corners[k],previous=corners[(k+3)%4],next=corners[(k+1)%4];
                result[k*2]=c+(previous-c).normalized*cut;
                result[k*2+1]=c+(next-c).normalized*cut*Mathf.Lerp(.7f,1.25f,((hash>>16)&255)/255f);
            }
            return result;
        }

        private static float FlagCross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;

        // Intersection of adjacent inward-shifted edge lines: a genuine offset
        // contour, not a shrunken rectangle or a bevel that extends into a neighbour.
        private static Vector2[] InsetFlagContour(Vector2[] outer,float inset)
        {
            var inner=new Vector2[outer.Length];
            // A tiny chamfer can vanish under an otherwise valid bevel offset.
            // Bound the common offset by each edge's adjacent half-angle retreats,
            // retaining at least 20% of every edge and matching ring correspondence.
            for(int k=0;k<outer.Length;k++)
            {
                Vector2 previous=(outer[k]-outer[(k+outer.Length-1)%outer.Length]).normalized;
                Vector2 edge=(outer[(k+1)%outer.Length]-outer[k]).normalized;
                Vector2 next=(outer[(k+2)%outer.Length]-outer[(k+1)%outer.Length]).normalized;
                float retreat=FlagCross(previous,edge)/(1+Vector2.Dot(previous,edge))+
                    FlagCross(edge,next)/(1+Vector2.Dot(edge,next));
                if(retreat>1e-6f)inset=Mathf.Min(inset,
                    Vector2.Distance(outer[k],outer[(k+1)%outer.Length])*.8f/retreat);
            }
            for(int k=0;k<outer.Length;k++)
            {
                Vector2 a=outer[(k+outer.Length-1)%outer.Length],b=outer[k],c=outer[(k+1)%outer.Length];
                Vector2 d=(b-a).normalized,e=(c-b).normalized;
                Vector2 p=a+new Vector2(-d.y,d.x)*inset,q=b+new Vector2(-e.y,e.x)*inset;
                inner[k]=p+d*(FlagCross(q-p,e)/FlagCross(d,e));
            }
            return inner;
        }

        // Clip each existing light-lattice cell to the stone face. A fan is safe
        // inside one <=1-unit cell; no triangle spans a whole slab's light gradient.
        private static List<Vector2> ClipFlagCell(Vector2[] contour,float u0,float v0,float u1,float v1)
        {
            var polygon=new List<Vector2>(contour);
            for(int edge=0;edge<4&&polygon.Count>0;edge++)
            {
                var clipped=new List<Vector2>(polygon.Count+2);
                Vector2 previous=polygon[polygon.Count-1];
                float previousDistance=FlagCellDistance(previous,edge,u0,v0,u1,v1);
                foreach(Vector2 current in polygon)
                {
                    float distance=FlagCellDistance(current,edge,u0,v0,u1,v1);
                    if((distance>=0)!=(previousDistance>=0))
                    {
                        float t=previousDistance/(previousDistance-distance);
                        clipped.Add(Vector2.LerpUnclamped(previous,current,t));
                    }
                    if(distance>=0)clipped.Add(current);
                    previous=current;previousDistance=distance;
                }
                polygon=clipped;
            }
            return polygon;
        }

        private static float FlagCellDistance(Vector2 p,int edge,float u0,float v0,float u1,float v1)
        {
            switch(edge){case 0:return p.x-u0;case 1:return u1-p.x;case 2:return p.y-v0;default:return v1-p.y;}
        }

        private static void ContouredFlagFace(Builder mesh,float u0,float v0,float u1,float v1,
            float joint,float bevel,float step,float stone,Color tint,int seedU,int seedV)
        {
            Vector2[] outer=FlagContour(u0,v0,u1,v1,joint,seedU,seedV);
            Vector2[] inner=InsetFlagContour(outer,Mathf.Max(.008f,bevel-joint));
            float exposure=PavingWearExposure((u0+u1)*.5f,(v0+v1)*.5f);
            // Subdivide long bevel edges at the same maximum spacing as the face.
            int start=mesh.V.Count,points=0;
            for(int k=0;k<outer.Length;k++)
            {
                int next=(k+1)%outer.Length;
                int count=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(outer[k],outer[next])/step));
                Vector2 edge=(outer[next]-outer[k]).normalized;
                float facing=Mathf.Clamp01((edge.x+edge.y)*.5f+.5f);
                for(int j=0;j<count;j++)
                {
                    float t=j/(float)count;
                    Vector2 a=Vector2.Lerp(outer[k],outer[next],t),b=Vector2.Lerp(inner[k],inner[next],t);
                    float wear=FloorPatina(a.x*1.7f+3,a.y*1.7f-7)*.6f;
                    float rubbed=QuarryEdgeAbrasion(a,u0,v0,u1,v1,seedU,seedV,exposure);
                    float tone=stone*Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(.925f,1.035f,facing),1,wear),1.12f,rubbed);
                    PavingVertex(mesh,a.x,a.y,tone,tint,FloorLight(a.x,a.y));
                    PavingVertex(mesh,b.x,b.y,tone,tint,FloorLight(b.x,b.y));
                    points++;
                }
            }
            for(int k=0;k<points;k++)
            {
                int a=start+k*2,b=start+((k+1)%points)*2;
                mesh.T.Add(a);mesh.T.Add(b);mesh.T.Add(b+1);
                mesh.T.Add(a);mesh.T.Add(b+1);mesh.T.Add(a+1);
            }
            float[] us=LightLines(u0,u1,step),vs=LightLines(v0,v1,step);
            if(SampleQuarryShoulder(u0,v0,u1,v1,seedU,seedV))
            {
                // Tiny corner chamfers bound the geometric bevel width. Keep that
                // valid inset and sample worn mineral on the existing stone face
                // instead: its strongest edge point, shoulder, and fade boundary.
                // These lines partition the same face; they add no coplanar layer.
                uint hash=PavingHash(seedU,seedV,829);
                int side=(int)(hash&3);
                float middle=Mathf.Lerp(.35f,.65f,((hash>>8)&255)/255f);
                if(side%2==0)
                {
                    us=QuarrySampleLines(us,Mathf.Lerp(u0,u1,middle));
                    vs=side==0?QuarrySampleLines(vs,v0+.20f,v0+.44f):
                        QuarrySampleLines(vs,v1-.44f,v1-.20f);
                }
                else
                {
                    vs=QuarrySampleLines(vs,Mathf.Lerp(v0,v1,middle));
                    us=side==3?QuarrySampleLines(us,u0+.20f,u0+.44f):
                        QuarrySampleLines(us,u1-.44f,u1-.20f);
                }
            }
            var shared=new Dictionary<Vector2Int,int>();
            for(int y=0;y+1<vs.Length;y++)for(int x=0;x+1<us.Length;x++)
            {
                List<Vector2> polygon=ClipFlagCell(inner,us[x],vs[y],us[x+1],vs[y+1]);
                if(polygon.Count<3)continue;
                var indices=new int[polygon.Count];
                for(int k=0;k<polygon.Count;k++)
                {
                    Vector2 p=polygon[k];
                    var key=new Vector2Int(Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000));
                    if(!shared.TryGetValue(key,out int index))
                    {
                        index=mesh.V.Count;shared.Add(key,index);
                        // Imported mineral grain carries the stone body; do not
                        // restart a broad cloudy vertex wash inside every flag.
                        // The rubbed mineral lip belongs to this face, not another
                        // coplanar crack/patch. World light and grain stay continuous.
                        float rubbed=QuarryEdgeAbrasion(p,u0,v0,u1,v1,seedU,seedV,exposure);
                        PavingVertex(mesh,p.x,p.y,stone*Mathf.Lerp(1,1.045f,rubbed),tint,FloorLight(p.x,p.y));
                    }
                    indices[k]=index;
                }
                for(int k=1;k+1<polygon.Count;k++)
                {
                    if(Mathf.Abs(FlagCross(polygon[k]-polygon[0],polygon[k+1]-polygon[0]))<1e-7f)continue;
                    mesh.T.Add(indices[0]);mesh.T.Add(indices[k]);mesh.T.Add(indices[k+1]);
                }
            }
        }

        private static float[] QuarrySampleLines(float[] lines,params float[] samples)
        {
            var result=new List<float>(lines);
            foreach(float sample in samples)
            {
                if(sample<=lines[0]+.0001f||sample>=lines[lines.Length-1]-.0001f)continue;
                bool present=false;
                foreach(float line in result)if(Mathf.Abs(line-sample)<.0001f){present=true;break;}
                if(!present)result.Add(sample);
            }
            result.Sort();return result.ToArray();
        }
    }
}
