using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using UnityEngine;

namespace Cryptforge.UI
{
    // Floor 3's garden growth (docs/51 sections 4 and 9.5): hanging roots where the ice had icicles, fungus and seed-pod
    // clusters where it had crystal clusters, and a moss cap on the rim spilling down the wall face - all on the same slots as
    // the ice crystals (FoundryRegionIceCrystals.cs): the platform's camera-facing outer rim, open to the void, never the
    // walkable floor, the pit, the gate path or the bridge, so never over a lane. One mesh, one renderer, under the same
    // IceCrystalVertexCap, in the coping material with pass 2's atlas (Resources/Gloam/GloamGrowth, laid out by
    // Tools/ArtReview/GenerateGloamGrowthTextures.py) on its property block; the vertex colour shades it. Its geometry is
    // worked out on a worker while floor 2 plays and the object is made hidden (PrepareTheme), shown only on floor 3.
    // Drawn where the crystals draw, between the sidewalls and the paving, so the deck covers a moss cushion's top edge.
    public sealed partial class FoundryRegionView
    {
        internal const string GloamGrowthName="Garden roots, fungi and moss";
        internal const string GloamGrowthAtlas="GloamGrowth";
        private GameObject _gloamGrowth;

        // The atlas (1024 px square; UV v = 1 - y/1024), from GenerateGloamGrowthTextures.py.
        private const float Atlas=1024f;
        // Roots: 8 cells of 128 x 512 in the top half, each anchored at its top centre, 8 px under the cell's top.
        private const int RootCells=8;
        private const float RootCellWidth=128,RootCellHeight=512,RootAnchorDrop=8;
        // Clusters: 4 cells of 256 x 256 at y 512..767 (painted to stand on their bottom centre); cell 0 the tallest.
        private const float ClusterCellSize=256,ClusterTop=512;
        // Moss strips: 2 of 1024 x 128 at y 768 and 896, each in 4 seamless quarters; the rim line 32 px under the top.
        private const float MossTop0=768,MossTop1=896,MossHeight=128,MossRim=32;
        // The most one rim edge can take: 7 moss strips, 10 roots and 3 clusters.
        private const int GrowthVerticesPerEdge=20*4;
        // The walkable floor's test margin, a little under the hero's edge margin: a point the floor does not contain with
        // this margin is not contained with the hero's either, so no growth vertex reaches a lane.
        private const float GrowthClearance=HeroMotion.EdgeMargin-.05f;
        // The moss's green, over the atlas's painted moss, so the cap reads as moss at phone scale.
        private static readonly Color MossTint=new Color(.92f,1.18f,.82f,1);

        // The growth's geometry from the region's cells, on the crystals' slots. Pure: it runs on a worker thread. Every
        // rim edge is overgrown: layered moss down the wall face, a back row and a front row of hanging roots, and fungus
        // and pod clusters on most edges. A piece that would reach onto walkable floor (a rim over a narrow notch of void,
        // where the wall face below overlaps the court or the landing on screen) is shortened or left out (OffTheFloor).
        private static PropGeometry GloamGrowthGeometry(List<Vector2Int> cells)
        {
            var g=new PropGeometry();
            int edge=0;
            cells.Sort((p,q)=>p.y!=q.y?p.y.CompareTo(q.y):p.x.CompareTo(q.x));
            var neighbours=new[]{Vector2Int.down,Vector2Int.right,Vector2Int.up,Vector2Int.left};
            var corner=new Vector3[4];
            foreach(var cell in cells)
            {
                float x=cell.x,y=cell.y;
                corner[0]=Project(x,y);corner[1]=Project(x+1,y);corner[2]=Project(x+1,y+1);corner[3]=Project(x,y+1);
                for(int i=0;i<4;i++)
                {
                    if(FoundryMap.UncutCell(cell.x+neighbours[i].x,cell.y+neighbours[i].y))continue;
                    Vector3 a=corner[i],b=corner[(i+1)%4];
                    FoundrySurface.ToDesign((a.x+b.x)*.5f,(a.y+b.y)*.5f,out float edgeU,out float edgeV);
                    if(!CourtEdgeVisible(edgeU,edgeV))continue;
                    if(!OpenToVoid(cell,neighbours[i]))continue;
                    Vector3 outward=(a+b)*.5f-Project(x+.5f,y+.5f);
                    if(outward.y>=0)continue;
                    if(g.V.Count+GrowthVerticesPerEdge>IceCrystalVertexCap)return g;
                    // Left to right on screen, so a strip's u runs with it.
                    if(a.x>b.x){Vector3 swap=a;a=b;b=swap;}
                    Overgrow(g,a,b,edge,cell.x*73+cell.y*151+i*17);
                    edge++;
                }
            }
            return g;
        }

        // One rim edge's growth, back to front: the back roots, the moss down the wall face (lowest first), the moss on
        // the rim, the front roots, the clusters.
        private static void Overgrow(PropGeometry g,Vector3 a,Vector3 b,int edge,int seed)
        {
            int backRoots=3+(Hash(seed+1)<.6f?1:0)+(Hash(seed+4)<.45f?1:0);
            for(int k=0;k<backRoots;k++)
                Root(g,a,b,(k+.5f)/backRoots+(Hash(seed*3+k)-.5f)*.2f,.55f+.3f*Hash(seed*5+k),.62f,seed*7+k);
            // The wall face: a deep spill, a low drape, two mid drapes across the edge's halves.
            MossStrip(g,a,b,.2f,.85f,-1.35f,.6f,edge+1,seed+17,.62f);
            MossStrip(g,a,b,-.04f,1.04f,-.95f,.7f,edge+3,seed+11,.72f);
            MossStrip(g,a,b,-.04f,.56f,-.32f,.78f,edge+1,seed+12,.88f);
            MossStrip(g,a,b,.44f,1.04f,-.4f,.82f,edge+2,seed+13,.86f);
            // The cap on the rim, in two halves, and a strip offset by half an edge for a fuller, joined cushion.
            MossStrip(g,a,b,-.04f,.54f,.15f,.66f,edge,seed+14,1f);
            MossStrip(g,a,b,.46f,1.04f,.15f,.66f,edge+1,seed+15,.97f);
            MossStrip(g,a,b,.25f,.75f,.1f,.5f,edge+2,seed+16,.94f);
            int frontRoots=3+(Hash(seed+2)<.6f?1:0)+(Hash(seed+3)<.3f?1:0);
            for(int k=0;k<frontRoots;k++)
            {
                // Lengths up to the full 1.25; one root in five wider.
                float length=.7f+.55f*Hash(seed*11+k);
                float widen=Hash(seed*13+k)<.2f?1.5f:1f;
                Root(g,a,b,(k+.5f)/frontRoots+(Hash(seed*17+k)-.5f)*.22f,length,.88f+.12f*Hash(seed*19+k),seed*23+k,widen);
            }
            // A large cluster on every other edge, a smaller one on every third, a middling one on every fourth: the painted
            // growth about 0.6-0.8 across in a cell 1.05-1.35 wide.
            if(edge%2==0)Fungus(g,Vector3.Lerp(a,b,.3f+.4f*Hash(seed+9)),.05f+.25f*Hash(seed+5),1.05f+.3f*Hash(seed*29+1),seed);
            if(edge%3==1)Fungus(g,Vector3.Lerp(a,b,.15f+.7f*Hash(seed+10)),.3f+.4f*Hash(seed+6),.75f+.2f*Hash(seed*31+1),seed+1);
            if(edge%4==3)Fungus(g,Vector3.Lerp(a,b,.2f+.6f*Hash(seed+8)),.6f+.3f*Hash(seed+7),.9f+.2f*Hash(seed*37+1),seed+2);
        }

        // A moss strip over the edge's stretch from t0 to t1 (a share of a..b, a little past either end so neighbours
        // join), its top `top` above the rim (negative: under it, down the wall face), `height` tall; the strip variant
        // and quarter from the seed and the index; the rim strips carry the strip's own rim line on the rim. Dropped
        // onto the rim if its cushion would reach the walkable floor, left out if even that would.
        private static void MossStrip(PropGeometry g,Vector3 a,Vector3 b,float t0,float t1,float top,float height,int quarter,int seed,float shade)
        {
            float row=Hash(seed+3)<.5f?MossTop0:MossTop1;
            float u0=(quarter&3)*.25f,u1=u0+.25f;
            float vTop=1-row/Atlas,vBottom=1-(row+MossHeight)/Atlas;
            Vector3 left=Vector3.LerpUnclamped(a,b,t0),right=Vector3.LerpUnclamped(a,b,t1);
            for(int attempt=0;attempt<2;attempt++)
            {
                Vector3 up=Vector3.up*top,down=Vector3.up*(top-height);
                Vector3 p=left+up,q=right+up,r=right+down,s=left+down;
                if(OffTheFloor(p,q,r,s))
                {
                    Quad(g,p,q,r,s,new Vector2(u0,vTop),new Vector2(u1,vTop),new Vector2(u1,vBottom),new Vector2(u0,vBottom),MossTint*shade);
                    return;
                }
                if(top<=0)return;
                top=0;
            }
        }

        // A hanging root at `along` a..b, straight down the screen from just under the rim, `length` long (the cell's 1:4,
        // `widen` wider); a third shorter if the whole would reach the walkable floor, left out if that would too.
        private static void Root(PropGeometry g,Vector3 a,Vector3 b,float along,float length,float shade,int seed,float widen=1)
        {
            int cell=(int)(Hash(seed*5+7)*RootCells)%RootCells;
            Vector3 anchor=Vector3.Lerp(a,b,Mathf.Clamp(along,.06f,.94f))+Vector3.down*.03f;
            float u0=cell*RootCellWidth/Atlas,u1=(cell+1)*RootCellWidth/Atlas,vTop=1,vBottom=1-RootCellHeight/Atlas;
            for(int attempt=0;attempt<2;attempt++)
            {
                float width=length*RootCellWidth/RootCellHeight*widen;
                Vector3 top=anchor+Vector3.up*length*RootAnchorDrop/RootCellHeight,bottom=top+Vector3.down*length;
                Vector3 half=Vector3.right*width*.5f;
                if(OffTheFloor(top-half,top+half,bottom+half,bottom-half))
                {
                    Quad(g,top-half,top+half,bottom+half,bottom-half,new Vector2(u0,vTop),new Vector2(u1,vTop),new Vector2(u1,vBottom),
                        new Vector2(u0,vBottom),new Color(shade,shade,shade,1));
                    return;
                }
                length*=.6f;
            }
        }

        // A fungus or seed-pod cluster on the sidewall under the rim point `at`, its top `drop` under the rim and `size`
        // across, so all of it stays below the deck's edge; the taller cells the rarer. Left out over walkable floor.
        private static void Fungus(PropGeometry g,Vector3 at,float drop,float size,int seed)
        {
            float pick=Hash(seed*11+3);
            int cell=pick<.2f?0:pick<.45f?1:pick<.75f?2:3;
            Vector3 top=at+Vector3.down*(.06f+drop),bottom=top+Vector3.down*size;
            Vector3 half=Vector3.right*size*.5f;
            if(!OffTheFloor(top-half,top+half,bottom+half,bottom-half))return;
            float u0=cell*ClusterCellSize/Atlas,u1=(cell+1)*ClusterCellSize/Atlas;
            float vTop=1-ClusterTop/Atlas,vBottom=1-(ClusterTop+ClusterCellSize)/Atlas;
            float shade=.92f+.08f*Hash(seed*17+5);
            Quad(g,top-half,top+half,bottom+half,bottom-half,new Vector2(u0,vTop),new Vector2(u1,vTop),new Vector2(u1,vBottom),
                new Vector2(u0,vBottom),new Color(shade,shade,shade,1));
        }

        // No corner of the piece lies on walkable floor (inside the hero's edge margin at most), read as the lanes are:
        // the screen point back to the design plane (FoundrySurface) and on to the floor (FoundryMap.Contains).
        private static bool OffTheFloor(Vector3 p,Vector3 q,Vector3 r,Vector3 s)=>Off(p)&&Off(q)&&Off(r)&&Off(s);
        private static bool Off(Vector3 point)
        {
            FoundrySurface.ToDesign(point.x,point.y,out float u,out float v);
            FoundryMap.ToFloor(u,v,out float x,out float y);
            return !FoundryMap.Contains(x,y,GrowthClearance);
        }

        private static void Quad(PropGeometry g,Vector3 p,Vector3 q,Vector3 r,Vector3 s,Vector2 up,Vector2 uq,Vector2 ur,Vector2 us,Color c)
        {
            int at=g.V.Count;
            c.a=1;
            g.V.Add(p);g.V.Add(q);g.V.Add(r);g.V.Add(s);
            g.U.Add(up);g.U.Add(uq);g.U.Add(ur);g.U.Add(us);
            g.C.Add(c);g.C.Add(c);g.C.Add(c);g.C.Add(c);
            g.T.Add(at);g.T.Add(at+1);g.T.Add(at+2);g.T.Add(at);g.T.Add(at+2);g.T.Add(at+3);
        }

        private void BuildGloamGrowth(PlatformMaterialSet set)
        {
            if(_gloamGrowth!=null||set==null||set.Coping==null)return;
            MakeGloamGrowth(GloamGrowthGeometry(new List<Vector2Int>(_cells)),set);
        }

        // The growth's object from its geometry (the main thread's share).
        private void MakeGloamGrowth(PropGeometry g,PlatformMaterialSet set)
        {
            if(_gloamGrowth!=null||g==null||set==null||set.Coping==null)return;
            var mesh=new Mesh{name=GloamGrowthName};mesh.SetVertices(g.V);mesh.SetUVs(0,g.U);mesh.SetColors(g.C);mesh.SetTriangles(g.T,0);
            mesh.RecalculateBounds();_owned.Add(mesh);
            _gloamGrowth=new GameObject(GloamGrowthName);_gloamGrowth.transform.SetParent(transform,false);
            _gloamGrowth.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=_gloamGrowth.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=set.Coping;renderer.sortingOrder=SceneryOrder(IceCrystalOrder);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }
    }
}
