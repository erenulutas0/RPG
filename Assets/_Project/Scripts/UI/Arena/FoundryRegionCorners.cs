using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The cell lattice still authors the ashlar and straight boundaries. Cut the actual
        // triangles, retaining UVs and baked light, rather than approximating diagonals with stairs.
        private struct CornerVertex
        {
            internal Vector3 P;
            internal Vector2 UV;
            internal Color C;
            internal int Original;
        }
        private static float CornerDistance(Vector3 p,int corner,int plane)
        {
            FoundryMap.ToDesign(p.x,p.y*2,out float u,out float v);
            if(plane==0)return corner==0?u-FoundryMap.West:FoundryMap.East-u;
            if(plane==1)return v-FoundryMap.South;
            return corner==0?FoundryMap.West+FoundryMap.South+FoundryMap.CornerCut-u-v:
                u-v-(FoundryMap.East-FoundryMap.South-FoundryMap.CornerCut);
        }
        private static List<CornerVertex> CornerClip(List<CornerVertex> source,int corner,int plane,float sign)
        {
            var result=new List<CornerVertex>();
            for(int i=0;i<source.Count;i++)
            {
                var a=source[i];var b=source[(i+1)%source.Count];
                float da=sign*CornerDistance(a.P,corner,plane),db=sign*CornerDistance(b.P,corner,plane);
                if(da>=0)result.Add(a);
                if(da*db<0)
                {
                    float t=da/(da-db);
                    result.Add(new CornerVertex{P=Vector3.Lerp(a.P,b.P,t),UV=Vector2.Lerp(a.UV,b.UV,t),C=Color.Lerp(a.C,b.C,t),Original=-1});
                }
            }
            return result;
        }
        private static void ClipFloorCorners(Builder mesh)
        {
            for(int corner=0;corner<2;corner++)
            {
                var result=new Builder();var remap=new Dictionary<int,int>();
                void Add(List<CornerVertex> polygon)
                {
                    if(polygon.Count<3)return;
                    var indices=new int[polygon.Count];
                    for(int p=0;p<polygon.Count;p++)
                    {
                        var vertex=polygon[p];
                        if(vertex.Original>=0&&remap.TryGetValue(vertex.Original,out int old)){indices[p]=old;continue;}
                        indices[p]=result.V.Count;
                        if(vertex.Original>=0)remap[vertex.Original]=indices[p];
                        result.V.Add(vertex.P);result.U.Add(vertex.UV);result.C.Add(vertex.C);
                    }
                    for(int p=1;p+1<indices.Length;p++)result.T.AddRange(new[]{indices[0],indices[p],indices[p+1]});
                }
                for(int t=0;t<mesh.T.Count;t+=3)
                {
                    var polygon=new List<CornerVertex>(3);
                    for(int k=0;k<3;k++){int i=mesh.T[t+k];polygon.Add(new CornerVertex{P=mesh.V[i],UV=mesh.U[i],C=mesh.C[i],Original=i});}
                    // Most triangles miss the removed corner. Preserve their indices/vertices directly.
                    bool outside=false;
                    for(int plane=0;plane<3;plane++)
                        if(CornerDistance(polygon[0].P,corner,plane)<=0&&CornerDistance(polygon[1].P,corner,plane)<=0&&CornerDistance(polygon[2].P,corner,plane)<=0){outside=true;break;}
                    if(outside){Add(polygon);continue;}
                    // Convex subtraction: emit each outside piece, discard only the final intersection.
                    for(int plane=0;plane<3&&polygon.Count>=3;plane++)
                    {
                        Add(CornerClip(polygon,corner,plane,-1));
                        polygon=CornerClip(polygon,corner,plane,1);
                    }
                }
                mesh.V.Clear();mesh.U.Clear();mesh.C.Clear();mesh.T.Clear();
                mesh.V.AddRange(result.V);mesh.U.AddRange(result.U);mesh.C.AddRange(result.C);mesh.T.AddRange(result.T);
            }
        }
        private static void CornerEdge(int corner,out Vector2 a,out Vector2 b,out Vector2 outward)
        {
            float west=FoundryMap.West,east=FoundryMap.East,south=FoundryMap.South,cut=FoundryMap.CornerCut;
            a=corner==0?new Vector2(west,south+cut):new Vector2(east-cut,south);
            b=corner==0?new Vector2(west+cut,south):new Vector2(east,south+cut);
            outward=corner==0?new Vector2(-1,-1).normalized:new Vector2(1,-1).normalized;
        }
        private static bool CourtEdgeVisible(float u,float v)=>u>FoundryMap.East+.001f||FoundryMap.CourtPlane(u,v);

        private void BuildDiagonalCourtEdges(ArenaView arena,Builder walls,Builder trim)
        {
            for(int corner=0;corner<2;corner++)
            {
                CornerEdge(corner,out var a,out var b,out var outward);
                Vector3 pa=Project(a.x,a.y),pb=Project(b.x,b.y);
                walls.WallQuad(pa-Vector3.up*2.8f,pb-Vector3.up*2.8f,pb,pa,false,new Color(.62f,.64f,.73f));
                int stones=Mathf.CeilToInt(Vector2.Distance(a,b));
                for(int i=0;i<stones;i++)
                {
                    Vector2 p=Vector2.Lerp(a,b,i/(float)stones)-outward*Kerb;
                    Vector2 q=Vector2.Lerp(a,b,(i+1f)/stones)-outward*Kerb;
                    Strip(trim,p.x,p.y,q.x,q.y,KerbWidth,KerbTone,CopingTrim.Stone,corner*40+i);
                }
                // Split at short intervals so each depth owner remains wholly on the void side,
                // including the edge-on southeast face. No bounding box crosses playable floor.
                int spans=Mathf.CeilToInt(Vector2.Distance(a,b)/.8f);
                for(int i=0;i<spans;i++)
                {
                    Vector2 p=Vector2.Lerp(a,b,i/(float)spans),q=Vector2.Lerp(a,b,(i+1f)/spans);
                    var lines=new Vector2[3][];
                    for(int row=0;row<3;row++)lines[row]=new[]{p+outward*(.04f+RailDepth-row*RailDepth*.5f),q+outward*(.04f+RailDepth-row*RailDepth*.5f)};
                    Vector2 centre=(p+q)*.5f+outward*.2f;
                    using(MassDepth(new Vector4(centre.x-.13f,centre.y-.13f,centre.x+.13f,centre.y+.13f),RailTop))
                    {
                        var rail=new Builder();PitRailRun(rail,lines,false);
                        Create("Diagonal court balustrade",rail,arena.PlatformMaterials.FoundryWall,-11);
                    }
                }
            }
        }
    }
}
