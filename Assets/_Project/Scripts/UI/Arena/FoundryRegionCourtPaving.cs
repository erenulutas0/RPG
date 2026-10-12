using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private const float CourtPathHalfWidth=CourtProcessionalHalfWidth+CourtProcessionalStoneWidth*.5f;
        private const float CourtPathStart=CourtStoneRadius+.16f;

        private static Vector2[] CourtCircle(float radius)
        {
            var points=new Vector2[48];
            for(int i=0;i<points.Length;i++)points[i]=CourtSealPoint(i*Mathf.PI/24,radius);
            return points;
        }

        private static Vector2[] CourtPathContour(Vector2 end)
        {
            Vector2 direction=(end-CourtSealCentre).normalized,side=new Vector2(-direction.y,direction.x);
            Vector2 start=CourtSealCentre+direction*CourtPathStart,half=side*CourtPathHalfWidth;
            return new[]{start-half,end-half,end+half,start+half};
        }

        // Replace every regular paving layer inside these disjoint footprints.
        // This runs after the bridge deck, before the fitted court is emitted:
        // a depth offset cannot solve two different flush stone arrangements.
        private static void ReplaceCourtPavingBase(Builder mesh)
        {
            CutCourtPaving(mesh,CourtCircle(CourtStoneRadius));
            foreach(Vector2 end in CourtProcessionalEnds)CutCourtPaving(mesh,CourtPathContour(end));
        }

        private static void CutCourtPaving(Builder mesh,Vector2[] designContour)
        {
            var contour=new Vector2[designContour.Length];
            Vector2 minimum=new Vector2(float.MaxValue,float.MaxValue),maximum=-minimum;
            for(int i=0;i<contour.Length;i++)
            {
                contour[i]=Project(designContour[i].x,designContour[i].y);
                minimum=Vector2.Min(minimum,contour[i]);maximum=Vector2.Max(maximum,contour[i]);
            }
            var result=new Builder();var remap=new Dictionary<int,int>();
            int OriginalIndex(int source)
            {
                if(remap.TryGetValue(source,out int previous))return previous;
                int index=result.V.Count;remap[source]=index;
                result.V.Add(mesh.V[source]);result.U.Add(mesh.U[source]);result.C.Add(mesh.C[source]);return index;
            }
            void AddOriginal(int a,int b,int c)
            {
                Vector3 p=mesh.V[a],q=mesh.V[b],r=mesh.V[c];
                double area=((double)q.x-p.x)*((double)r.y-p.y)-((double)q.y-p.y)*((double)r.x-p.x);
                if(System.Math.Abs(area)<=1e-10)return;
                result.T.Add(OriginalIndex(a));result.T.Add(OriginalIndex(b));result.T.Add(OriginalIndex(c));
            }
            void Add(List<CornerVertex> polygon)
            {
                if(polygon.Count<3)return;
                var indices=new int[polygon.Count];
                for(int i=0;i<indices.Length;i++)indices[i]=-1;
                int Index(int p)
                {
                    if(indices[p]>=0)return indices[p];
                    CornerVertex vertex=polygon[p];
                    if(vertex.Original>=0)return indices[p]=OriginalIndex(vertex.Original);
                    int index=result.V.Count;indices[p]=index;
                    result.V.Add(vertex.P);result.U.Add(vertex.UV);result.C.Add(vertex.C);return index;
                }
                for(int i=1;i+1<polygon.Count;i++)
                {
                    Vector3 a=polygon[0].P,b=polygon[i].P,c=polygon[i+1].P;
                    double area=((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
                    if(System.Math.Abs(area)<=1e-10)continue;
                    result.T.Add(Index(0));result.T.Add(Index(i));result.T.Add(Index(i+1));
                }
            }
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                int ia=mesh.T[t],ib=mesh.T[t+1],ic=mesh.T[t+2];
                Vector2 pa=mesh.V[ia],pb=mesh.V[ib],pc=mesh.V[ic];
                Vector2 lo=Vector2.Min(pa,Vector2.Min(pb,pc)),hi=Vector2.Max(pa,Vector2.Max(pb,pc));
                // Most of the map misses a court footprint entirely. Preserve
                // those triangles with no per-triangle list, array or closure.
                if(hi.x<=minimum.x||lo.x>=maximum.x||hi.y<=minimum.y||lo.y>=maximum.y){AddOriginal(ia,ib,ic);continue;}
                bool outside=false,inside=true;
                for(int edge=0;edge<contour.Length;edge++)
                {
                    Vector2 a=contour[edge],b=contour[(edge+1)%contour.Length];
                    float d0=FlagCross(b-a,pa-a),d1=FlagCross(b-a,pb-a),d2=FlagCross(b-a,pc-a);
                    if(d0<=0&&d1<=0&&d2<=0){outside=true;break;}
                    inside&=d0>=0&&d1>=0&&d2>=0;
                }
                if(outside){AddOriginal(ia,ib,ic);continue;}
                if(inside)continue;
                var polygon=new List<CornerVertex>(3);
                for(int k=0;k<3;k++)
                {
                    int i=mesh.T[t+k];
                    polygon.Add(new CornerVertex{P=mesh.V[i],UV=mesh.U[i],C=mesh.C[i],Original=i});
                }
                // A convex subtraction emits only the outside fragments. UV and
                // the already-sampled source light are interpolated once, never
                // re-tinted or illuminated a second time at a clipped boundary.
                for(int edge=0;edge<contour.Length&&polygon.Count>=3;edge++)
                {
                    Vector2 a=contour[edge],b=contour[(edge+1)%contour.Length];
                    Add(ClipCourtTriangle(polygon,a,b,-1));
                    polygon=ClipCourtTriangle(polygon,a,b,1);
                }
            }
            mesh.V.Clear();mesh.U.Clear();mesh.C.Clear();mesh.T.Clear();
            mesh.V.AddRange(result.V);mesh.U.AddRange(result.U);mesh.C.AddRange(result.C);mesh.T.AddRange(result.T);
        }

        private static List<CornerVertex> ClipCourtTriangle(List<CornerVertex> polygon,Vector2 edgeA,Vector2 edgeB,float sign)
        {
            var result=new List<CornerVertex>();Vector2 edge=edgeB-edgeA;
            for(int i=0;i<polygon.Count;i++)
            {
                CornerVertex a=polygon[i],b=polygon[(i+1)%polygon.Count];
                float da=sign*FlagCross(edge,(Vector2)a.P-edgeA),db=sign*FlagCross(edge,(Vector2)b.P-edgeA);
                if(da>=0)result.Add(a);
                if(da*db<0)
                {
                    float t=da/(da-db);
                    result.Add(new CornerVertex{P=Vector3.Lerp(a.P,b.P,t),UV=Vector2.Lerp(a.UV,b.UV,t),
                        C=Color.Lerp(a.C,b.C,t),Original=-1});
                }
            }
            return result;
        }

        // Mortar, rubbed shoulders and face partition a stone, rather than
        // covering a full joint bed with another coplanar face. Zero-width edges
        // are construction subdivisions within one radial stone, not joints.
        private static void CourtFittedStone(Builder mesh,Vector2[] polygon,float[] joints,Vector2 identity)
        {
            var shoulders=new float[joints.Length];
            for(int i=0;i<joints.Length;i++)shoulders[i]=joints[i]>0?joints[i]+.052f:0;
            Vector2[] outer=CourtVariableInset(polygon,joints),inner=CourtVariableInset(polygon,shoulders);
            float tone=Mathf.Lerp(.97f,1.035f,FloorPatina(identity.x*.16f+5.7f,identity.y*.16f-9.4f));
            Color family=PavingMineralFamily(identity.x,identity.y);
            for(int i=0;i<polygon.Length;i++)
            {
                if(joints[i]<=0)continue;
                int next=(i+1)%polygon.Length;
                CourtFittedFace(mesh,new[]{polygon[i],polygon[next],outer[next],outer[i]},.72f,family);
                Vector2 edge=(polygon[next]-polygon[i]).normalized;
                float facing=Mathf.Clamp01((edge.x+edge.y)*.5f+.5f);
                CourtFittedFace(mesh,new[]{outer[i],outer[next],inner[next],inner[i]},
                    tone*Mathf.Lerp(.86f,1.055f,facing),family);
            }
            CourtFittedFace(mesh,inner,tone,family);
        }

        private static void CourtFittedFlag(Builder mesh,Vector2[] polygon)
        {
            polygon=CourtStoneContour(polygon);
            if(polygon.Length<3)return;
            var joints=new float[polygon.Length];Vector2 centre=Vector2.zero;
            for(int i=0;i<polygon.Length;i++){joints[i]=.028f;centre+=polygon[i];}
            CourtFittedStone(mesh,polygon,joints,centre/polygon.Length);
        }

        private static Vector2[] CourtVariableInset(Vector2[] polygon,float[] distances)
        {
            var result=new Vector2[polygon.Length];
            for(int i=0;i<polygon.Length;i++)
            {
                int previous=(i+polygon.Length-1)%polygon.Length;
                Vector2 a=(polygon[i]-polygon[previous]).normalized,b=(polygon[(i+1)%polygon.Length]-polygon[i]).normalized;
                Vector2 p=polygon[i]+new Vector2(-a.y,a.x)*distances[previous];
                Vector2 q=polygon[i]+new Vector2(-b.y,b.x)*distances[i];
                float denominator=FlagCross(a,b);
                result[i]=Mathf.Abs(denominator)<1e-6f?(p+q)*.5f:p+a*(FlagCross(q-p,b)/denominator);
            }
            return result;
        }

        private static void CourtFittedFace(Builder mesh,Vector2[] polygon,float tone,Color family)
        {
            float u0=polygon[0].x,u1=u0,v0=polygon[0].y,v1=v0;
            foreach(Vector2 p in polygon){u0=Mathf.Min(u0,p.x);u1=Mathf.Max(u1,p.x);v0=Mathf.Min(v0,p.y);v1=Mathf.Max(v1,p.y);}
            float[] us=LightLines(u0,u1,.5f),vs=LightLines(v0,v1,.5f);
            // The same half-unit light lattice, with exact common vertices shared
            // by neighbouring clipped cells. Only referenced points are emitted.
            var shared=new Dictionary<Vector2,int>();
            int Index(Vector2 p)
            {
                if(shared.TryGetValue(p,out int existing))return existing;
                int index=mesh.V.Count;shared[p]=index;
                PavingVertex(mesh,p.x,p.y,tone,family,FloorLight(p.x,p.y));return index;
            }
            for(int y=0;y+1<vs.Length;y++)for(int x=0;x+1<us.Length;x++)
            {
                var clipped=ClipFlagCell(polygon,us[x],vs[y],us[x+1],vs[y+1]);
                for(int i=1;i+1<clipped.Count;i++)
                {
                    if(Mathf.Abs(FlagCross(clipped[i]-clipped[0],clipped[i+1]-clipped[0]))<1e-7f)continue;
                    mesh.T.Add(Index(clipped[0]));mesh.T.Add(Index(clipped[i]));mesh.T.Add(Index(clipped[i+1]));
                }
            }
        }

        private static Vector2[] CourtStoneContour(Vector2[] polygon)
        {
            // Cell clipping can repeat an exact point or leave a collinear one.
            // Remove these before a geometric inset; the footprint is identical.
            var points=new List<Vector2>();
            foreach(Vector2 p in polygon)
                if(points.Count==0||(p-points[points.Count-1]).sqrMagnitude>1e-12f)points.Add(p);
            if(points.Count>1&&(points[0]-points[points.Count-1]).sqrMagnitude<=1e-12f)points.RemoveAt(points.Count-1);
            bool changed=true;
            while(changed&&points.Count>=3)
            {
                changed=false;
                for(int i=0;i<points.Count;i++)
                {
                    Vector2 a=points[(i+points.Count-1)%points.Count],b=points[i],c=points[(i+1)%points.Count];
                    if(Mathf.Abs(FlagCross(b-a,c-b))>1e-7f)continue;
                    points.RemoveAt(i);changed=true;break;
                }
            }
            float area=0;
            for(int i=0;i<points.Count;i++)area+=FlagCross(points[i],points[(i+1)%points.Count]);
            if(area<0)points.Reverse();
            return points.ToArray();
        }
    }
}
