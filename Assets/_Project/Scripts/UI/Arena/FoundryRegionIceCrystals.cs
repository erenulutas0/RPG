using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Floor 2's ice growth (docs/49): icicles hanging from the platform's outer rim and small crystal clusters on its
    // sidewalls, two-tone faceted mirror-ice (#1F4E66 / #5FA8C8 / #BFE6F2). All of it hangs off the deck's outer faces,
    // never on the walkable floor, the pit, the gate path or the bridge. Built once, hidden, while the floor before the ice plays,
    // as one mesh in the coping material drawn untextured (ApplyTheme's Bare dress); hidden on the foundry. Floor 1 never
    // builds it, so its renderer and vertex counts are unchanged. Drawn between the sidewalls and the paving, so the deck
    // covers whatever hangs from a rim that faces away from the camera.
    public sealed partial class FoundryRegionView
    {
        // At most this many vertices (about 6% of the 174k budget).
        internal const int IceCrystalVertexCap=6000;
        internal const string IceCrystalName="Ice crystals and icicles";
        private const float IceCrystalOrder=-15.6f;
        private static readonly Color IceDeep=Hex(0x1F4E66);
        private GameObject _iceCrystals;

        // The finished geometry of a theme prop: positions, colours and triangles, worked out off the main thread.
        private sealed class PropGeometry
        {
            public readonly List<Vector3> V=new List<Vector3>();
            public readonly List<Color> C=new List<Color>();
            public readonly List<int> T=new List<int>();
        }

        private void BuildIceCrystals(PlatformMaterialSet set)
        {
            if(_iceCrystals!=null||set==null||set.Coping==null)return;
            MakeIceCrystals(IceCrystalGeometry(new List<Vector2Int>(_cells)),set);
        }

        // The crystals' and icicles' geometry from the region's cells. Pure (no engine object is touched): it runs on a
        // worker thread while the floor before plays (PrepareTheme).
        private static PropGeometry IceCrystalGeometry(List<Vector2Int> cells)
        {
            var g=new PropGeometry();
            List<Vector3> v=g.V;List<Color> c=g.C;List<int> t=g.T;
            int edge=0;
            // A stable order, independent of the hash set's.
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
                    // Only the platform's outer rim, open to the void for several cells: never the pit, a shaft or a
                    // yard's inner edge.
                    if(!OpenToVoid(cell,neighbours[i]))continue;
                    // Only rims whose sidewall faces the camera (outward runs down the screen) show their ice.
                    Vector3 outward=(a+b)*.5f-Project(x+.5f,y+.5f);
                    if(outward.y>=0)continue;
                    if(v.Count+36>IceCrystalVertexCap)return g;
                    int seed=cell.x*73+cell.y*151+i*17;
                    if(edge%2==0)for(int k=0;k<3;k++)Icicle(v,c,t,a,b,(k+.5f)/3+(Hash(seed+k)-.5f)*.18f,seed+k);
                    if(edge%5==2)Cluster(v,c,t,Vector3.Lerp(a,b,.35f+Hash(seed+9)*.3f)+Vector3.down*(.9f+Hash(seed+5)*.7f),seed);
                    edge++;
                }
            }
            return g;
        }

        // The crystals' object from their geometry (the main thread's share).
        private void MakeIceCrystals(PropGeometry g,PlatformMaterialSet set)
        {
            if(_iceCrystals!=null||g==null||set==null||set.Coping==null)return;
            var uv=new List<Vector2>(g.V.Count);
            for(int i=0;i<g.V.Count;i++)uv.Add(Vector2.zero);
            var mesh=new Mesh{name=IceCrystalName};mesh.SetVertices(g.V);mesh.SetUVs(0,uv);mesh.SetColors(g.C);mesh.SetTriangles(g.T,0);
            mesh.RecalculateBounds();_owned.Add(mesh);
            _iceCrystals=new GameObject(IceCrystalName);_iceCrystals.transform.SetParent(transform,false);
            _iceCrystals.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=_iceCrystals.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=set.Coping;renderer.sortingOrder=SceneryOrder(IceCrystalOrder);
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
        }

        private const int VoidReach=6;
        private static bool OpenToVoid(Vector2Int cell,Vector2Int outward)
        {
            for(int k=1;k<=VoidReach;k++)
                if(FoundryMap.UncutCell(cell.x+outward.x*k,cell.y+outward.y*k))return false;
            return !(cell.x>=FoundryMap.PitWest-2&&cell.x<=FoundryMap.PitEast+2&&cell.y>=FoundryMap.PitSouth-2&&cell.y<=FoundryMap.PitNorth+2);
        }

        // One icicle under the rim at t along a..b: a lit facet (mirror-ice, its root at the ice edge) and a shaded one.
        private static void Icicle(List<Vector3> v,List<Color> c,List<int> t,Vector3 a,Vector3 b,float along,int seed)
        {
            Vector3 dir=(b-a).normalized;
            Vector3 root=Vector3.Lerp(a,b,Mathf.Clamp(along,.08f,.92f))+Vector3.down*.16f;
            float half=.06f+Hash(seed*3+1)*.05f,length=.4f+Hash(seed*7+2)*.85f;
            Vector3 left=root-dir*half,right=root+dir*half,tip=root+Vector3.down*length;
            Facet(v,c,t,left,root,tip,IceEdge,MirrorIce,MirrorIce*.8f);
            Facet(v,c,t,root,right,tip,MirrorIce*.9f,IceDeep,IceDeep);
        }

        // Three shards growing out of the sidewall, fanned upward.
        private static void Cluster(List<Vector3> v,List<Color> c,List<int> t,Vector3 foot,int seed)
        {
            for(int k=0;k<3;k++)
            {
                float angle=(-34f+k*32f+(Hash(seed*5+k)-.5f)*12f)*Mathf.Deg2Rad;
                Vector3 dir=new Vector3(Mathf.Sin(angle),Mathf.Cos(angle),0),side=new Vector3(dir.y,-dir.x,0);
                float length=(k==1?.55f:.35f)+Hash(seed*11+k)*.2f,half=.07f+Hash(seed*13+k)*.03f;
                Vector3 baseAt=foot+side*(k-1)*.06f,tip=baseAt+dir*length,shoulder=baseAt+dir*length*.35f;
                Facet(v,c,t,baseAt,shoulder+side*half,tip,MirrorIce*.85f,MirrorIce,IceEdge);
                Facet(v,c,t,baseAt,tip,shoulder-side*half,IceDeep,MirrorIce*.7f,IceDeep);
            }
        }

        private static void Facet(List<Vector3> v,List<Color> c,List<int> t,Vector3 p,Vector3 q,Vector3 r,Color cp,Color cq,Color cr)
        {
            int at=v.Count;
            v.Add(p);v.Add(q);v.Add(r);c.Add(cp);c.Add(cq);c.Add(cr);
            t.Add(at);t.Add(at+1);t.Add(at+2);
        }

        // A deterministic 0..1 from an integer (no System.Random in the game).
        private static float Hash(int n)
        {
            float s=Mathf.Sin(n*12.9898f+78.233f)*43758.5453f;
            return s-Mathf.Floor(s);
        }
    }
}
