using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Floor 3's garden growth (docs/51 sections 4 and 9.5): hanging roots where the ice had icicles, fungus and seed-pod
    // clusters where it had crystal clusters, and a moss cap along the rim where it had rime - all on the same slots as
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
        private const int ClusterCells=4;
        private const float ClusterCellSize=256,ClusterTop=512;
        // Moss strips: 2 of 1024 x 128 at y 768 and 896, each in 4 seamless quarters; the rim line 32 px under the top.
        private const float MossTop0=768,MossTop1=896,MossHeight=128,MossRim=32;
        // World sizes: a root's width (its length is the cell's 1:4), a cluster's side, the moss strip's height.
        private const float RootWidth=.3f,ClusterSize=.95f,MossStripHeight=.42f;

        // The growth's geometry from the region's cells, on the crystals' slots. Pure: it runs on a worker thread.
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
                    // A moss strip and at most two roots and a cluster: 16 vertices an edge at most.
                    if(g.V.Count+16>IceCrystalVertexCap)return g;
                    int seed=cell.x*73+cell.y*151+i*17;
                    Moss(g,a,b,edge,seed);
                    if(edge%2==0)for(int k=0;k<2;k++)Root(g,a,b,(k+.5f)/2+(Hash(seed+k)-.5f)*.25f,seed+k);
                    if(edge%5==2)Fungus(g,Vector3.Lerp(a,b,.35f+Hash(seed+9)*.3f),Hash(seed+5)*.35f,seed);
                    edge++;
                }
            }
            return g;
        }

        // The moss cap along the rim edge a..b: the cushion over the rim line, the drape down the sidewall; consecutive
        // edges take consecutive quarters of a strip, which join without a seam.
        private static void Moss(PropGeometry g,Vector3 a,Vector3 b,int edge,int seed)
        {
            float top=Hash(seed+3)<.5f?MossTop0:MossTop1;
            float u0=(edge%4)*.25f,u1=u0+.25f;
            float vTop=1-top/Atlas,vBottom=1-(top+MossHeight)/Atlas;
            // The strip's rim line (MossRim under its top) lies on the rim: the cushion above it, the drape below.
            float cushion=MossStripHeight*MossRim/MossHeight,drape=MossStripHeight-cushion;
            float shade=.9f+.1f*Hash(seed+4);
            Vector3 up=Vector3.up*cushion,down=Vector3.down*drape;
            // Left to right on screen, so the strip's u runs with it.
            if(a.x>b.x){Vector3 swap=a;a=b;b=swap;}
            Quad(g,a+up,b+up,b+down,a+down,new Vector2(u0,vTop),new Vector2(u1,vTop),new Vector2(u1,vBottom),new Vector2(u0,vBottom),shade);
        }

        // A hanging root at `along` a..b, hanging straight down the screen from just under the rim.
        private static void Root(PropGeometry g,Vector3 a,Vector3 b,float along,int seed)
        {
            int cell=(int)(Hash(seed*5+7)*RootCells)%RootCells;
            float width=RootWidth*(.85f+.3f*Hash(seed*3+1));
            float length=width*RootCellHeight/RootCellWidth;
            Vector3 anchor=Vector3.Lerp(a,b,Mathf.Clamp(along,.1f,.9f))+Vector3.down*.04f;
            Vector3 top=anchor+Vector3.up*length*RootAnchorDrop/RootCellHeight,bottom=top+Vector3.down*length;
            Vector3 half=Vector3.right*width*.5f;
            float u0=cell*RootCellWidth/Atlas,u1=(cell+1)*RootCellWidth/Atlas,vTop=1,vBottom=1-RootCellHeight/Atlas;
            Quad(g,top-half,top+half,bottom+half,bottom-half,new Vector2(u0,vTop),new Vector2(u1,vTop),new Vector2(u1,vBottom),
                new Vector2(u0,vBottom),.85f+.15f*Hash(seed*7+2));
        }

        // A fungus or seed-pod cluster growing on the sidewall under the rim point `at`, its top `drop` plus a little under
        // the rim, so all of it stays below the deck's edge; the taller cells the rarer.
        private static void Fungus(PropGeometry g,Vector3 at,float drop,int seed)
        {
            float pick=Hash(seed*11+3);
            int cell=pick<.2f?0:pick<.45f?1:pick<.75f?2:3;
            float size=ClusterSize*(.85f+.3f*Hash(seed*13+4));
            Vector3 top=at+Vector3.down*(.06f+drop),bottom=top+Vector3.down*size;
            Vector3 half=Vector3.right*size*.5f;
            float u0=cell*ClusterCellSize/Atlas,u1=(cell+1)*ClusterCellSize/Atlas;
            float vTop=1-ClusterTop/Atlas,vBottom=1-(ClusterTop+ClusterCellSize)/Atlas;
            Quad(g,top-half,top+half,bottom+half,bottom-half,new Vector2(u0,vTop),new Vector2(u1,vTop),new Vector2(u1,vBottom),
                new Vector2(u0,vBottom),.9f+.1f*Hash(seed*17+5));
        }

        private static void Quad(PropGeometry g,Vector3 p,Vector3 q,Vector3 r,Vector3 s,Vector2 up,Vector2 uq,Vector2 ur,Vector2 us,float shade)
        {
            int at=g.V.Count;
            var c=new Color(shade,shade,shade,1);
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
