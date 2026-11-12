using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Floor 2's phase-2 props (docs/49 section 4): frozen mirror panels - the vault's quicksilver set into mirror-ice - and
    // snow drifts at the parapets' feet. Both are built once, the first time the ice floor is shown, beside the crystals
    // (BuildIceCrystals), in the coping material drawn untextured (ApplyTheme's Bare dress, so the vertex colours are
    // final), and hidden on the foundry. Floor 1 never builds them, so its renderers and vertices are unchanged.
    //
    // Panels: tall faceted slabs standing flush against the inner face of the far rails, where the court's farthest
    // boundary is the rail itself over the panel's screen columns, so every actor is in front of them and nothing inside
    // the court stands in front of them on screen. They lie inside the hero's edge margin (HeroMotion.EdgeMargin, .6), so
    // never on a lane, and clear of the pit's columns, the gate's door axis, the causeway and the bridge. They draw after
    // the balustrade (-11) and every structure detail (-10), before actors; they write no depth.
    // Drifts: flush snow decals along the inside of the rails, at most DriftDepth deep (inside the edge margin), drawn just
    // after the paving and its decals, so contact shadows, telegraphs (-13), pickups and actors all draw over them.
    public sealed partial class FoundryRegionView
    {
        internal const string IcePanelName="Frozen mirror panels",IceDriftName="Snow drifts";
        // Both meshes together stay well inside the ~11k vertices left under the 174k ceiling (docs/49 section 8).
        internal const int IcePanelVertexCap=900,IceDriftVertexCap=900;
        private const float IcePanelOrder=-9.5f;
        private const float DriftDepth=.48f;
        private static readonly Color VioletGlint=Hex(0x8B6CE8),Snow=Hex(0xE4ECF2),RimedIron=Hex(0x1E242C);
        private GameObject _icePanels,_iceDrifts;

        // A panel: its foot along the rail from Start to End (design u,v), its face Inward of the rail toward the court,
        // its height in world units. Start is the end whose side faces the camera.
        private readonly struct IcePanel
        {
            internal readonly Vector2 Start,End,Inward;internal readonly float Height;
            internal IcePanel(Vector2 start,Vector2 end,Vector2 inward,float height){Start=start;End=end;Inward=inward;Height=height;}
        }
        private const float RailInset=.04f,PanelDepth=.16f,LandingNorth=12f;
        // North rail (v = 18): in the open gaps between its posts (finial 0, shrine 1.5, lanterns -2 and 6, finial 10),
        // east of the pit's screen columns. East rail (u = 14): between its lanterns (v -8 and -.4) and north of the
        // causeway. Gate: the landing's north rail, beside the door, east of the boiler that stands against it.
        private static readonly IcePanel[] IcePanels=
        {
            new IcePanel(new Vector2(2.55f,FoundryMap.North-RailInset),new Vector2(3.95f,FoundryMap.North-RailInset),new Vector2(0,-1),2.9f),
            new IcePanel(new Vector2(7.2f,FoundryMap.North-RailInset),new Vector2(8.4f,FoundryMap.North-RailInset),new Vector2(0,-1),2.3f),
            new IcePanel(new Vector2(FoundryMap.East-RailInset,-5.25f),new Vector2(FoundryMap.East-RailInset,-3.85f),new Vector2(-1,0),2.7f),
            new IcePanel(new Vector2(FoundryMap.East-RailInset,11.35f),new Vector2(FoundryMap.East-RailInset,12.55f),new Vector2(-1,0),2.2f),
            new IcePanel(new Vector2(25.4f,LandingNorth-RailInset),new Vector2(26.45f,LandingNorth-RailInset),new Vector2(0,-1),2.1f),
            new IcePanel(new Vector2(26.8f,LandingNorth-RailInset),new Vector2(27.85f,LandingNorth-RailInset),new Vector2(0,-1),2.7f),
        };
        // Drift runs along the rails' inner feet (from, to, inward), heaped at each panel's foot and in the bare runs; none
        // on the bridge's join (south rail u -25..-11), the causeway, the gate's door or a district's centre.
        private static readonly Vector4[] DriftRuns=
        {
            // North rail, v = 18, inward -v.
            new Vector4(-12.6f,FoundryMap.North,-9.2f,FoundryMap.North),new Vector4(-5.6f,FoundryMap.North,-3.1f,FoundryMap.North),
            new Vector4(2.1f,FoundryMap.North,4.6f,FoundryMap.North),new Vector4(6.9f,FoundryMap.North,9.3f,FoundryMap.North),
            // East rail, u = 14, inward -u.
            new Vector4(FoundryMap.East,-11.2f,FoundryMap.East,-9.1f),new Vector4(FoundryMap.East,-5.7f,FoundryMap.East,-3.2f),
            new Vector4(FoundryMap.East,10.9f,FoundryMap.East,13.3f),
            // The gate landing's north rail, v = 12, inward -v.
            new Vector4(25.1f,LandingNorth,27.95f,LandingNorth),
            // South rail, v = -18, inward +v: behind the rail on screen, a lip of snow showing over its foot.
            new Vector4(-5.8f,FoundryMap.South,-3.4f,FoundryMap.South),new Vector4(3.4f,FoundryMap.South,5.5f,FoundryMap.South),
        };
        private static Vector2 DriftInward(Vector4 run)=>run.y==FoundryMap.South?Vector2.up:run.x==run.z?Vector2.left:Vector2.down;
        internal static int IcePanelCount=>IcePanels.Length;
        internal static int DriftRunCount=>DriftRuns.Length;

        private void BuildIceProps(PlatformMaterialSet set)
        {
            if(set==null||set.Coping==null)return;
            if(_icePanels==null)
            {
                var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
                for(int i=0;i<IcePanels.Length;i++)MirrorPanel(v,c,t,IcePanels[i],i);
                // Drawn after the balustrade and every structure detail, before actors.
                _icePanels=IcePropObject(IcePanelName,v,c,t,set.Coping,SceneryOrder(IcePanelOrder),0);
            }
            if(_iceDrifts==null)
            {
                var v=new List<Vector3>();var c=new List<Color>();var t=new List<int>();
                for(int i=0;i<DriftRuns.Length;i++)Drift(v,c,t,DriftRuns[i],DriftInward(DriftRuns[i]),i);
                // The paving's own rank; reaching twice as far below the map as the floor decals puts its bounds' centre
                // under theirs, so it draws after the paving and the kerbs (FloorDecal, DecalSortReach).
                _iceDrifts=IcePropObject(IceDriftName,v,c,t,set.Coping,SceneryOrder(FloorDecal),DecalSortReach*2);
            }
        }

        private GameObject IcePropObject(string name,List<Vector3> v,List<Color> c,List<int> t,Material material,int order,float sortReach)
        {
            var uv=new List<Vector2>(v.Count);for(int i=0;i<v.Count;i++)uv.Add(Vector2.zero);
            var mesh=new Mesh{name=name};mesh.SetVertices(v);mesh.SetUVs(0,uv);mesh.SetColors(c);mesh.SetTriangles(t,0);
            mesh.RecalculateBounds();
            if(sortReach>0){var bounds=mesh.bounds;bounds.Encapsulate(bounds.min+Vector3.down*sortReach);mesh.bounds=bounds;}
            _owned.Add(mesh);
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);
            obj.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=obj.AddComponent<MeshRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=order;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
            renderer.lightProbeUsage=UnityEngine.Rendering.LightProbeUsage.Off;
            renderer.reflectionProbeUsage=UnityEngine.Rendering.ReflectionProbeUsage.Off;
            return obj;
        }

        // A tall faceted slab of mirror-ice: a rimed iron foot, a lit bevel, two centre facets meeting at a peaked crown and
        // a shaded bevel, two-tone #1F4E66 to #5FA8C8, an ice-edge #BFE6F2 rim along its crown and its lit side, its
        // camera-facing end a thin deep side face, and one faint violet streak of reflected aurora across the face.
        private static void MirrorPanel(List<Vector3> v,List<Color> c,List<int> t,IcePanel panel,int seed)
        {
            Vector2 front=panel.Inward*PanelDepth;
            Vector3 a=Project(panel.Start.x+front.x,panel.Start.y+front.y),b=Project(panel.End.x+front.x,panel.End.y+front.y);
            Vector3 back=Project(panel.Start.x,panel.Start.y);
            float h=panel.Height,lean=(Hash(seed*31+7)-.5f)*.12f;
            const float foot=.16f,s1=.22f,s2=.8f;
            float peak=.5f+(Hash(seed*17+3)-.5f)*.16f;
            float hL=h*(.66f+Hash(seed*5+1)*.08f),h1=h*.86f,h2=h*(.8f+Hash(seed*7+2)*.08f),hR=h*(.58f+Hash(seed*9+4)*.08f);
            Vector3 P(float s,float height)=>Vector3.Lerp(a,b,s)+Vector3.up*height+Vector3.right*lean*height/h;
            // The camera-facing end: a thin side face from the rail back to the face, deep ice over the iron foot.
            Quad(v,c,t,back,a,a+Vector3.up*foot,back+Vector3.up*foot,RimedIron,RimedIron,RimedIron,RimedIron);
            Quad(v,c,t,back+Vector3.up*foot,a+Vector3.up*foot,P(0,hL),back+Vector3.up*hL+Vector3.right*lean*hL/h,
                Tone(IceDeep,.7f),Tone(IceDeep,.7f),IceDeep,Tone(IceDeep,.9f));
            // The rimed iron foot across the face, its top edge catching frost.
            Quad(v,c,t,a,b,b+Vector3.up*foot,a+Vector3.up*foot,RimedIron,RimedIron,Tone(RimedIron,1.8f),Tone(RimedIron,1.8f));
            Color lowLit=Color.Lerp(IceDeep,MirrorIce,.45f),lowShade=Tone(IceDeep,.78f);
            // Lit bevel, the centre's lit and shaded facets meeting at the crown, shaded bevel.
            Quad(v,c,t,P(0,foot),P(s1,foot),P(s1,h1),P(0,hL),lowLit,lowLit,MirrorIce,MirrorIce);
            Quad(v,c,t,P(s1,foot),P(peak,foot),P(peak,h),P(s1,h1),IceDeep,Tone(IceDeep,1.1f),Tone(MirrorIce,.92f),Color.Lerp(IceDeep,MirrorIce,.7f));
            Quad(v,c,t,P(peak,foot),P(s2,foot),P(s2,h2),P(peak,h),lowShade,lowShade,Tone(IceDeep,1.15f),Tone(IceDeep,1.35f));
            Quad(v,c,t,P(s2,foot),P(1,foot),P(1,hR),P(s2,h2),Tone(lowShade,.85f),Tone(lowShade,.85f),Tone(IceDeep,.95f),IceDeep);
            // The faint reflected-violet streak, rising across the lit centre facet into the shaded one (premultiplied by
            // its alpha in the sprite shader, so it only tints the ice beneath).
            float y0=h*(.3f+Hash(seed*13+5)*.1f),y1=y0+h*.3f,w=h*.035f;
            Color streak=VioletGlint;streak.a=.34f;Color fade=VioletGlint;fade.a=.08f;
            Quad(v,c,t,P(s1+.04f,y0-w),P(s2-.06f,y1-w),P(s2-.06f,y1+w),P(s1+.04f,y0+w),fade,streak,streak,fade);
            // Ice-edge rim: along the crown, down the lit side and up the centre ridge, inside the outline.
            Color rim=IceEdge;rim.a=.95f;Color soft=IceEdge;soft.a=.35f;
            float r=.055f;
            Edge(v,c,t,P(0,hL),P(s1,h1),r,rim,rim);
            Edge(v,c,t,P(s1,h1),P(peak,h),r,rim,rim);
            Edge(v,c,t,P(peak,h),P(s2,h2),r*.8f,rim,soft);
            Edge(v,c,t,P(s2,h2),P(1,hR),r*.7f,soft,soft);
            Quad(v,c,t,P(0,foot),P(0,foot)+Vector3.right*r,P(0,hL)+Vector3.right*r+Vector3.down*r,P(0,hL),soft,soft,rim,rim);
            Quad(v,c,t,P(peak,h*.35f)+Vector3.left*r*.4f,P(peak,h*.35f)+Vector3.right*r*.4f,P(peak,h-r)+Vector3.right*r*.4f,
                P(peak,h-r)+Vector3.left*r*.4f,new Color(IceEdge.r,IceEdge.g,IceEdge.b,0),new Color(IceEdge.r,IceEdge.g,IceEdge.b,0),soft,soft);
        }

        // A colour's value scaled, opaque (Color * float would scale its alpha too, and the sprite shader premultiplies it).
        private static Color Tone(Color c,float k)=>new Color(c.r*k,c.g*k,c.b*k,1);

        // A thin strip hanging under the segment p..q (inside the silhouette).
        private static void Edge(List<Vector3> v,List<Color> c,List<int> t,Vector3 p,Vector3 q,float width,Color cp,Color cq)
        {
            Vector3 down=Vector3.down*width;
            Quad(v,c,t,p+down,q+down,q,p,cp,cq,cq,cp);
        }

        private static void Quad(List<Vector3> v,List<Color> c,List<int> t,Vector3 p,Vector3 q,Vector3 r,Vector3 s,Color cp,Color cq,Color cr,Color cs)
        {
            int at=v.Count;
            v.Add(p);v.Add(q);v.Add(r);v.Add(s);c.Add(cp);c.Add(cq);c.Add(cr);c.Add(cs);
            t.Add(at);t.Add(at+1);t.Add(at+2);t.Add(at);t.Add(at+2);t.Add(at+3);
        }

        // A flush drift along a rail's inner foot: brightest snow against the wall, frost across its body, soft to nothing at
        // its wavy inner edge and at both ends. Three rows of vertices along the run.
        private static void Drift(List<Vector3> v,List<Color> c,List<int> t,Vector4 run,Vector2 inward,int seed)
        {
            Vector2 from=new Vector2(run.x,run.y),to=new Vector2(run.z,run.w);
            int segments=Mathf.Max(4,Mathf.CeilToInt(Vector2.Distance(from,to)/.3f));
            int at=v.Count;
            for(int k=0;k<=segments;k++)
            {
                float s=(float)k/segments;
                Vector2 foot=Vector2.Lerp(from,to,s)+inward*.02f;
                // Heaped in the middle, low at the ends, its inner edge waving.
                float heap=Mathf.Pow(Mathf.Sin(Mathf.PI*s),.6f);
                float depth=DriftDepth*(.3f+.7f*heap)*(.82f+.18f*Mathf.Sin(s*9.4f+seed*1.7f));
                float end=Mathf.SmoothStep(0,1,Mathf.Min(s,1-s)*6f);
                Vector2 mid=foot+inward*depth*.5f,inner=foot+inward*depth;
                v.Add(Project(foot.x,foot.y));v.Add(Project(mid.x,mid.y));v.Add(Project(inner.x,inner.y));
                Color wall=Snow*.8f;wall.a=.88f*end;
                Color body=Frost*.76f;body.a=.7f*end;
                Color edge=Frost*.7f;edge.a=0;
                c.Add(wall);c.Add(body);c.Add(edge);
                if(k==segments)continue;
                int p=at+k*3;
                for(int row=0;row<2;row++)
                {
                    int i0=p+row,i1=p+row+1,j0=p+3+row,j1=p+3+row+1;
                    t.Add(i0);t.Add(j0);t.Add(j1);t.Add(i0);t.Add(j1);t.Add(i1);
                }
            }
        }
    }
}
