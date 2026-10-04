using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Builds once from the same map as movement; owns meshes, borrows imported art.
    public sealed partial class FoundryRegionView : MonoBehaviour
    {
        private readonly List<Mesh> _owned=new List<Mesh>();
        private readonly HashSet<Vector2Int> _cells=new HashSet<Vector2Int>();
        private Sprite _furnace;
        private FoundryVisibilityView _visibility;
        // Owners runs parallel to Builder.V: the footprint index each vertex takes its depth from (-1 = resolve at Emit).
        private sealed class Batch { internal Builder Builder=new Builder(); internal Material Material; internal float Order; internal readonly List<int> Owners=new List<int>(); }
        private readonly List<Batch> _batches=new List<Batch>();
        private static Vector3 Project(float x,float y) { FoundryMap.ToFloor(x,y,out float fx,out float fy);return new Vector3(fx,fy*.5f,0); }
        // Flush floor decals - coping trim, borders, inlays, seal, joints. They are part of the walking surface, so the
        // hero cutaway must not dissolve them (before 2026-09-30 brass lines vanished in a disc around the hero's feet).
        // They sort at the paving's own rank (-15), drawn after it (see the emit loop), so every raised structure at -14 -
        // bridge coping, block sides - paints over them: at -14 they tied with those and won on bounds, painting kerbs over
        // the bridge coping and the crane footing's face. They are built unlit and take the paving's floor light as their
        // batch is emitted (LightFloorDecals), so never pre-light one.
        private const float FloorDecal=-14.75f;
        internal const string FloorDecalLayer="Foundry floor decals";
        internal const float DecalSortReach=200f;
        public void Build(ArenaView arena,Sprite furnace)
        {
            _furnace=furnace;
            _visibility=gameObject.AddComponent<FoundryVisibilityView>();
            RegisterDepthLandmarks();
            for(int r=0;r<FoundryMap.RectangleCount;r++)
                for(int x=FoundryMap.Bound(r,0);x<FoundryMap.Bound(r,2);x++)
                    for(int y=FoundryMap.Bound(r,1);y<FoundryMap.Bound(r,3);y++)_cells.Add(new Vector2Int(x,y));
            var floor=new Builder();var walls=new Builder();var trim=new Builder();
            foreach(var cell in _cells)
            {
                float x=cell.x,y=cell.y;
                var p=new[]{Project(x,y),Project(x+1,y),Project(x+1,y+1),Project(x,y+1)};
                BuildPavingCell(floor,cell);
                var neighbours=new[]{Vector2Int.down,Vector2Int.right,Vector2Int.up,Vector2Int.left};
                for(int i=0;i<4;i++)if(!FoundryMap.Cell(cell.x+neighbours[i].x,cell.y+neighbours[i].y))
                {
                    Vector3 a=p[i],b=p[(i+1)%4],drop=Vector3.down*2.8f;
                    walls.WallQuad(a+drop,b+drop,b,a,false,new Color(.7f,.7f,.76f));
                    Vector3 inward=(Project(x+.5f,y+.5f)-(a+b)*.5f)*.18f;
                    trim.Quad(a,b,b+inward,a+inward,false,new Color(.7f,.65f,.54f));
                    MapCoping(trim,CopingTrim.Stone,cell.x*7+cell.y*13+i);
                }
            }
            // Exact straight bridge, using the same diagonal inequalities as movement.
            var bridge=new[]{Project(-27,-23),Project(-23,-27),Project(-10,-14),Project(-14,-10)};
            // Diagonally laid stone deck exactly covering that quad (was one flat slab in 26 x 4 light-sampling strips).
            BuildBridgeDeck(floor);
            for(int i=0;i<3;i++)
            {
                int edge=i==2?3:i; Vector3 a=bridge[edge],b=bridge[(edge+1)%4];
                // Outward from the whole edge's midpoint, so shortening the west edge below does not shear its coping.
                Vector3 outward=(a+b)*.5f-Project(-18.5f,-18.5f);outward.Normalize();
                // The west edge's court end lies inside the court's west room (u -18..-12, v -14..18) up to (-18,-14):
                // walkable floor on both sides, so the rail and its wall start where the bridge leaves the court.
                if(edge==3)a=Project(-18,-14);
                walls.WallQuad(a+Vector3.down*2.8f,b+Vector3.down*2.8f,b,a,false,new Color(.7f,.7f,.76f));
                var rail=new Builder();
                rail.WallQuad(a,b,b+Vector3.up*.3f,a+Vector3.up*.3f,false,new Color(.7f,.7f,.76f));
                rail.WallQuad(a+Vector3.up*.3f,b+Vector3.up*.3f,b+Vector3.up*.3f+outward*.25f,a+Vector3.up*.3f+outward*.25f,true,Color.white);
                Create("Bridge coping",rail,arena.PlatformMaterials.FoundryWall,-14);
            }
            BuildCourtStoneRing(floor);
            Create("Layout surface",floor,arena.PlatformMaterials.FoundryPaving,-15);
            Create("Layout sidewalls",walls,arena.PlatformMaterials.FoundryWall,-16);
            Create("Layout coping",trim,arena.PlatformMaterials.Coping,FloorDecal);
            // The thin rim parapet gave way to the continuous balustrade BuildMassing raises (2026-09-30).
            BuildLandmarks(arena);
            BuildDressing(arena);
            BuildSiteFinish(arena);
            BuildWorkshop(arena);
            BuildClutter(arena);
            BuildUndercroft(arena);
            // Last: round boilers' foundations, balustrade, furnace pit and bastions (FoundryRegionMassing.cs).
            BuildMassing(arena);
            foreach(var batch in _batches)
            {
                bool decal=batch.Order==FloorDecal;
                if(decal)LightFloorDecals(batch.Builder);
                Mesh emitted=Emit(decal?FloorDecalLayer:"Foundry static layer",batch.Builder,batch.Material,batch.Order,batch.Owners);
                // Equal ranks draw by bounds centre along the sort axis (+y, lower drawn later): reaching far below the
                // map puts the decals' centre under the paving's, so they always draw just after it. Culling only widens.
                if(decal){var bounds=emitted.bounds;bounds.Encapsulate(bounds.min+Vector3.down*DecalSortReach);emitted.bounds=bounds;}
            }
            _batches.Clear();
            _depthFootprints.Clear();_depthLandmarks.Clear();_depthScope=-1;
        }
        // Deck frame: s=u+v runs along the bridge (-50 far end .. -24 court), d=u-v across it (-4..4), so the walking
        // quad is exactly [-50,-24] x [-4,4]. On screen d is horizontal and s vertical (halved by the floor projection).
        private static Vector3 Deck(float s,float d)=>Project((s+d)*.5f,(s-d)*.5f);
        // Why the coursed deck (earlier 2026-09-30) read as a wall: a stone square in plan foreshortens to exactly 2:1
        // on screen - brick proportion - and courses laid across the walk in half-offset running bond are the canonical
        // wall bond, at the scale and in the bond of the bridge's own sidewall directly below it; the continuous lines
        // all ran across the walk and every stone had a shaded top and lit bottom like a coursed wall face. Every floor
        // here shows the 2:1 diamonds of the u/v grid instead, so the deck now lays its stones on that grid, diagonal
        // to the walk: court-size flags (1 x 1) in the centre half, cut on lattice centre lines at the brass lanes
        // (d=+/-2, under the inlays), and a margin course out to |d|=3.5, just inside the kerb Bridge joints lays along
        // each rail. The margins were three rows of half-size setts; at the gameplay camera their ~210 thin crossing
        // joints read as a wire net, busier than the flags. They are now one quiet course of the same grid at twice the
        // pitch (lines on even u and v, so every other centre joint runs on through the lane to the kerb): alternate
        // triangles and trapezoids behind a single zigzag joint. Flush geometry and vertex colour only, inside the quad.
        private static void BuildBridgeDeck(Builder mesh)
        {
            // Joint bed first, in the old light-sampling grid (1 s x 2 d), so no stone needs a dark base quad of its own;
            // paler under the margins, whose shallow joints stay well below the centre's. It alone lies under the kerb.
            for(int i=0;i<26;i++)
                for(int j=0;j<4;j++)
                {
                    float s=-50+i,d=-4+j*2;
                    LitFloorQuad(mesh,Deck(s,d),Deck(s,d+2),Deck(s+1,d+2),Deck(s+1,d),j==0||j==3?.66f:.5f);
                }
            DeckLattice(mesh,1,-2,2,false);
            // The west kerb ends at s=-32. Beyond it the deck meets walkable court,
            // so carry the stone face to d=-4 instead of exposing the joint bed.
            DeckLattice(mesh,2,-3.5f,-2,true,-50,-32);
            DeckLattice(mesh,2,-4,-2,true,-32,-24);
            DeckLattice(mesh,2,2,3.5f,true);
        }
        // Every u/v lattice stone of size g inside the band d0..d1 of the deck: whole diamonds, and the pieces a lane,
        // the kerb line or a deck end cuts from them. Lane and end cuts lie on lattice centre or tip lines (g = 1 and 2:
        // s=-24 is a centre line, s=-50 a tip line); the kerb line cuts the margin course off-lattice, along the kerb.
        private static void DeckLattice(Builder mesh,float g,float d0,float d1,bool margin,float s0=-50,float s1=-24)
        {
            int lo=Mathf.FloorToInt(-27/g)-1,hi=Mathf.CeilToInt(-10/g)+1;
            for(int k=lo;k<=hi;k++)
                for(int m=lo;m<=hi;m++)
                {
                    float s=(k+m+1)*g,d=(k-m)*g;
                    if(d+g<=d0||d-g>=d1||s+g<=s0||s-g>=s1)continue;
                    var stone=new List<Vector2>{new Vector2(s-g,d),new Vector2(s,d+g),new Vector2(s+g,d),new Vector2(s,d-g)};
                    stone=DeckClip(DeckClip(DeckClip(DeckClip(stone,0,s0,1),0,s1,-1),1,d0,1),1,d1,-1);
                    if(stone.Count>=3)BridgeStone(mesh,stone,margin,k,m);
                }
        }
        // Sutherland-Hodgman against one deck axis (0 = s, 1 = d), keeping sign*(p-bound) >= 0.
        private static List<Vector2> DeckClip(List<Vector2> poly,int axis,float bound,float sign)
        {
            var kept=new List<Vector2>(poly.Count+1);
            for(int i=0;i<poly.Count;i++)
            {
                Vector2 a=poly[i],b=poly[(i+1)%poly.Count];
                float fa=sign*(a[axis]-bound),fb=sign*(b[axis]-bound);
                if(fa>=0)kept.Add(a);
                if(fa*fb<0)kept.Add(Vector2.Lerp(a,b,fa/(fa-fb)));
            }
            return kept;
        }
        // One laid stone from its deck-frame outline, in three stacked flush layers over the joint bed: a shaded rim,
        // a lit rim and the face, each inset per edge in the orthonormal u/v plane. Lattice edges take the court's light
        // from screen right (lit where the outward normal points right, shaded where left); cut edges get no bevel, the
        // face running to the joint, so a halved stone keeps its size and the lane and kerb lines stay clean verticals.
        private static void BridgeStone(Builder mesh,List<Vector2> outline,bool margin,int k,int m)
        {
            int n=outline.Count;var p=new Vector2[n];float area=0;
            for(int i=0;i<n;i++)p[i]=new Vector2((outline[i].x+outline[i].y)*.5f,(outline[i].x-outline[i].y)*.5f);
            for(int i=0;i<n;i++)area+=p[i].x*p[(i+1)%n].y-p[(i+1)%n].x*p[i].y;
            if(Mathf.Abs(area)<1e-4f)return;
            if(area<0)System.Array.Reverse(p);
            // Flags take the court's skewed distribution (mostly near base, one in six darker, one in ten paler and warmer).
            // Margin stones are darker, cooler and about half as varied, so the centre flags dominate and the margins stay
            // a calm ground under enemies and telegraphs.
            float grain=StoneSample(k+(margin?9000:5000),m-(margin?389:277));
            float stone=margin?(grain<.17f?Mathf.Lerp(.83f,.87f,grain/.17f):grain>.9f?Mathf.Lerp(.96f,.99f,(grain-.9f)/.1f):Mathf.Lerp(.88f,.94f,(grain-.17f)/.73f)):
                grain<.17f?Mathf.Lerp(.78f,.87f,grain/.17f):grain>.9f?Mathf.Lerp(1.08f,1.15f,(grain-.9f)/.1f):Mathf.Lerp(.93f,1.04f,(grain-.17f)/.73f);
            float hue=StoneSample(k+(margin?9131:5131),m-(margin?446:57));
            Color tint=hue<.22f?new Color(.97f,.99f,1.03f):hue>.82f?new Color(1.05f,1.0f,.93f):Color.white;
            if(margin)tint*=new Color(.97f,.985f,1.03f);
            // Flags use the court's own joint and bevel widths and contrast; margin stones a narrow, shallow joint (face
            // .91 over a .66 bed, where the flags sit ~1 over .5) and soft bevels (1.04 / .9 against 1.1 / .74).
            float joint=margin?.018f:.028f,bevel=margin?.032f:.06f,lit=margin?1.04f:1.1f,shade=margin?.9f:.74f;
            var rim=new float[n];var pale=new float[n];var face=new float[n];
            for(int i=0;i<n;i++)
            {
                Vector2 e=p[(i+1)%n]-p[i];
                bool cut=Mathf.Abs(e.x)>1e-4f&&Mathf.Abs(e.y)>1e-4f;
                // CCW outline: outward normal (e.y,-e.x); screen x grows with u-v, so it faces right when e.x+e.y > 0.
                bool bright=e.x+e.y>0;
                rim[i]=joint;pale[i]=cut||bright?joint:bevel;face[i]=cut?joint:bevel;
            }
            int first=mesh.C.Count;
            DeckPolygon(mesh,DeckInset(p,rim),stone*shade);
            DeckPolygon(mesh,DeckInset(p,pale),stone*lit);
            DeckPolygon(mesh,DeckInset(p,face),stone);
            for(int i=first;i<mesh.C.Count;i++)mesh.C[i]*=tint;
        }
        // Offsets each edge of a convex CCW u/v outline inward by its own distance; vertices are the offset lines' meets.
        private static Vector2[] DeckInset(Vector2[] p,float[] offset)
        {
            int n=p.Length;var inset=new Vector2[n];
            for(int i=0;i<n;i++)
            {
                int h=(i+n-1)%n;
                Vector2 e0=(p[i]-p[h]).normalized,e1=(p[(i+1)%n]-p[i]).normalized;
                Vector2 q0=p[i]+new Vector2(-e0.y,e0.x)*offset[h],q1=p[i]+new Vector2(-e1.y,e1.x)*offset[i];
                float cross=e0.x*e1.y-e0.y*e1.x;
                if(Mathf.Abs(cross)<1e-5f){inset[i]=q1;continue;}
                Vector2 w=q1-q0;
                inset[i]=q0+e0*((w.x*e1.y-w.y*e1.x)/cross);
            }
            return inset;
        }
        // A convex u/v outline as lit floor quads fanned from its first vertex (a triangle is one degenerate quad).
        private static void DeckPolygon(Builder mesh,Vector2[] q,float tone)
        {
            int n=q.Length;
            for(int i=1;i+1<n;i+=2)
                LitFloorQuad(mesh,Project(q[0].x,q[0].y),Project(q[i].x,q[i].y),Project(q[i+1].x,q[i+1].y),
                    Project(q[Mathf.Min(i+2,n-1)].x,q[Mathf.Min(i+2,n-1)].y),tone);
        }
        // The photograph now owns the composition: broad court, inset shaft, south bridge, northeast gate.
        private void BuildLandmarks(ArenaView arena)
        {
            var inlays=new Builder();
            Color brass=new Color(.7f,.56f,.34f);
            BuildCourtSeal(arena);
            Strip(inlays,-22,-24,-8,-10,.12f,brass,CopingTrim.Brass);Strip(inlays,-24,-22,-10,-8,.12f,brass,CopingTrim.Brass);
            Strip(inlays,4,-1,30,-1,.12f,brass,CopingTrim.Brass);Strip(inlays,5,9,30,9,.12f,brass,CopingTrim.Brass);
            Strip(inlays,-16,-10,8,-10,.1f,brass,CopingTrim.Brass);Strip(inlays,-16,-10,-16,15,.1f,brass,CopingTrim.Brass);
            Strip(inlays,-16,15,10,15,.1f,brass,CopingTrim.Brass);
            BuildCourtCompass(inlays,brass);
            Create("Court brass inlays",inlays,arena.PlatformMaterials.Coping,FloorDecal);

            BuildGate(arena);
            // Four round banded boilers, piped to each other and to the gate, replace the 3x3 furnace boxes.
            BuildBoilerTowers(arena);
        }
        private void BuildGate(ArenaView arena)
        {
            var facade=arena.PlatformMaterials.GateFacade;
            float height=facade!=null?6.5f:4.8f;
            // One structure for depth: foundations, body, facade, pediment, inlays and the gate beacons.
            using(DepthStructure(32,-3,35,13,height+1.7f))BuildGateParts(arena,facade,height);
        }
        private void BuildGateParts(ArenaView arena,Material facade,float height)
        {
            Block(arena,32,-3,35,-1,facade!=null?.4f:4.8f,3,"Gate left foundation");
            Block(arena,32,11,35,13,facade!=null?.4f:4.8f,3,"Gate right foundation");
            Block(arena,32,-1,34,11,height,3,"Gate masonry body");
            if(facade==null)return;
            // Full rectangular elevation on one physical wall; no cutout alpha or baked isometric tilt.
            Vector3 a=Project(31.99f,11),b=Project(31.99f,-1),lift=Vector3.up*height;
            var face=new Builder();face.Quad(a,b,b+lift,a+lift,false,Color.white);
            face.U.Clear();face.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            Create("Painted gate facade",face,facade,-12);
            BuildGateArchitecture(arena,height);
        }
        private void BuildFurnaceTower(ArenaView arena,float u,float v)
        {
            // The whole 3x3 tower, crown and flue ironwork included, is one structure for depth.
            using(DepthStructure(u,v,u+3,v+3,4.8f))BuildFurnaceTowerParts(arena,u,v);
        }
        private void BuildFurnaceTowerParts(ArenaView arena,float u,float v)
        {
            var facade=arena.PlatformMaterials.FurnaceFacade;
            if(facade==null)
            {
                Block(arena,u,v,u+3,v+3,2.2f,3,"Furnace fallback plinth");
                Furnace(u+1.5f,v+1.5f,2.2f,1.1f);return;
            }
            Block(arena,u,v,u+3,v+3,.35f,3,"Furnace foundation");
            Block(arena,u+.2f,v+.2f,u+2.8f,v+2.8f,3.2f,0,"Furnace boiler body");
            Vector3 lift=Vector3.up*.35f;
            PaintFace(Project(u+.19f,v+2.8f)+lift,Project(u+.19f,v+.2f)+lift,2.8f,facade);
            PaintFace(Project(u+.2f,v+.19f)+lift,Project(u+2.8f,v+.19f)+lift,2.8f,facade);
            BuildBoilerArchitecture(arena,u,v);
        }
        private void PaintFace(Vector3 a,Vector3 b,float height,Material material)
        {
            var face=new Builder();var lift=Vector3.up*height;
            face.Quad(a,b,b+lift,a+lift,false,Color.white);
            face.U.Clear();face.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            Create("Painted furnace face",face,material,-12);
        }
        private void Furnace(float u,float v,float rise,float scale)
        {
            if(_furnace==null)return;
            var obj=new GameObject("Perimeter furnace");obj.transform.SetParent(transform,false);
            obj.transform.localPosition=Project(u,v)+Vector3.up*rise;obj.transform.localScale=Vector3.one*scale;
            var renderer=obj.AddComponent<SpriteRenderer>();renderer.sprite=_furnace;renderer.sortingOrder=-12;
        }
        private void Beam(ArenaView arena,Vector3 a,Vector3 b,float width,string name)
        {
            Vector3 n=new Vector3(-(b-a).y,(b-a).x,0).normalized*width*.5f;
            var beam=new Builder();beam.Quad(a+n,b+n,b-n,a-n,false,new Color(.85f,.8f,.65f));
            Create(name,beam,arena.PlatformMaterials.FoundryWall,-12);
        }
        // Where a flush decal strip samples ENV_Platform_Coping.png (2172x724: six stone panels in dark frames between
        // brass rails). Local quad UVs used to start at the texture's corner, so every short strip showed a brass bar
        // and a black frame edge - the row of dark "slots" along every rim. A strip now maps onto one clean region.
        private enum CopingTrim { Stone, Brass }
        // Panel interiors, inset past the bevels: (u0, v0, u1, v1).
        private static readonly Vector4[] CopingPanels=
        {
            new Vector4(55/2172f,.13f,316/2172f,.896f),new Vector4(400/2172f,.13f,653/2172f,.896f),
            new Vector4(786/2172f,.13f,1045/2172f,.896f),new Vector4(1126/2172f,.13f,1376/2172f,.896f),
            new Vector4(1512/2172f,.13f,1766/2172f,.896f),new Vector4(1856/2172f,.13f,2114/2172f,.896f),
        };
        // The top brass rail, clear of its rivet blocks.
        private static readonly Vector4 CopingRail=new Vector4(40/2172f,1-24/724f,680/2172f,1-3/724f);
        // A strip is laid in pieces of at most DecalPiece, one texture mapping stretched across them all (the image is
        // unchanged), so a long inlay samples the floor light as densely as the flags beside it: the court's 26-long
        // brass lanes, one quad each before, could only blend the light at their two ends past the pools between.
        private const float DecalPiece=1f;
        private static void Strip(Builder b,float x0,float y0,float x1,float y1,float width,Color color,CopingTrim trim,int seed=0)
        {
            Vector2 start=new Vector2(x0,y0),end=new Vector2(x1,y1);
            Vector2 normal=new Vector2(y0-y1,x1-x0).normalized*width*.5f;
            int pieces=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(start,end)/DecalPiece-.001f));
            for(int i=0;i<pieces;i++)
            {
                float t0=(float)i/pieces,t1=(float)(i+1)/pieces;
                Vector2 p=Vector2.Lerp(start,end,t0),q=Vector2.Lerp(start,end,t1);
                b.Quad(Project(p.x+normal.x,p.y+normal.y),Project(q.x+normal.x,q.y+normal.y),
                    Project(q.x-normal.x,q.y-normal.y),Project(p.x-normal.x,p.y-normal.y),false,color);
                MapCoping(b,trim,seed,t0,t1);
            }
        }
        // Stone runs its length along a panel's tall axis; brass runs along the rail. t0..t1: this piece's share of it.
        private static void MapCoping(Builder b,CopingTrim trim,int seed,float t0=0,float t1=1)
        {
            if(trim==CopingTrim.Brass)b.MapLast(CopingRail,false,t0,t1);
            else b.MapLast(CopingPanels[((seed%CopingPanels.Length)+CopingPanels.Length)%CopingPanels.Length],true,t0,t1);
        }
        // Every decal vertex takes the paving's own light (FloorLight) at its design position, so a kerb, a groove or
        // the seal's field falls to slate in the open court and warms in a fire's pool with the flags around it: with
        // one constant colour each they read cold grey in the pools and over-bright on the cool court. Brass - wherever
        // a decal samples the coping sheet's rail, its only metal - is polished: per channel it recovers BrassFloor of
        // the light's dimming below neutral (it still reads as brass on cool slate) and takes the light's excess over
        // neutral at BrassGain (a warm glint in the pools). The coping material is Sprites/Default at _Color 1, so the
        // vertex colour itself lands in the fixed4 interpolator (guaranteed +/-2): decal tones reach 1 (the seal field)
        // under FloorLight's 1.74, brass tones .82 under ~1.93; DecalCeiling keeps any future tone's peak below 2, hue kept.
        private const float BrassFloor=.3f,BrassGain=1.25f,DecalCeiling=1.9f;
        private static void LightFloorDecals(Builder b)
        {
            for(int i=0;i<b.V.Count;i++)
            {
                // Flush vertices only: world y is the floor's y halved (Project), z is still 0 on this rank.
                FoundryMap.ToDesign(b.V[i].x,b.V[i].y*2,out float u,out float v);
                Color light=FloorLight(u,v),tone=b.C[i];
                if(OnCopingRail(b.U[i]))light=new Color(BrassLight(light.r),BrassLight(light.g),BrassLight(light.b));
                Color lit=new Color(tone.r*light.r,tone.g*light.g,tone.b*light.b,tone.a);
                float peak=Mathf.Max(lit.r,Mathf.Max(lit.g,lit.b));
                if(peak>DecalCeiling){float scale=DecalCeiling/peak;lit.r*=scale;lit.g*=scale;lit.b*=scale;}
                b.C[i]=lit;
            }
        }
        private static float BrassLight(float c)=>c<1?c+(1-c)*BrassFloor:1+(c-1)*BrassGain;
        private static bool OnCopingRail(Vector2 uv)=>uv.x>=CopingRail.x-1e-5f&&uv.x<=CopingRail.z+1e-5f&&
            uv.y>=CopingRail.y-1e-5f&&uv.y<=CopingRail.w+1e-5f;
        private void BuildPerimeter(ArenaView arena)
        {
            // Exterior low coping and regular deep supports make one substantial fortress platform.
            foreach(var cell in _cells)
            {
                int x=cell.x,y=cell.y;
                if(!FoundryMap.Cell(x+(0),y+(-1)))
                {
                    Block(arena,x,y-.3f,x+1,y,.28f,2.8f,"South parapet");
                    if(x%4==0)Block(arena,x,y-.8f,x+.65f,y,.75f,4,"South support");
                }
                if(!FoundryMap.Cell(x+(-1),y+(0)))
                {
                    Block(arena,x-.3f,y,x,y+1,.28f,2.8f,"West parapet");
                    if(y%4==0)Block(arena,x-.8f,y,x,y+.65f,.75f,4,"West support");
                }
                if(!FoundryMap.Cell(x+(0),y+(1)))
                {
                    Block(arena,x,y+1,x+1,y+1.3f,.28f,2.8f,"North parapet");
                    if(x%4==0)Block(arena,x,y+1,x+.65f,y+1.8f,.75f,4,"North support");
                }
                if(!FoundryMap.Cell(x+(1),y+(0)))
                {
                    Block(arena,x+1,y,x+1.3f,y+1,.28f,2.8f,"East parapet");
                    if(y%4==0)Block(arena,x+1,y,x+1.8f,y+.65f,.75f,4,"East support");
                }
            }
        }
        private void Block(ArenaView arena,float x0,float y0,float x1,float y1,float rise,float depth,string name)
        {
            Vector3 lift=Vector3.up*rise;
            // A block is its own structure for depth. Knee-high or taller blocks, and any block inside a structure scope,
            // fade in front of the hero; low parapets and shelves only hide feet. Blocks of height 1+ also claim details.
            int own=RegisterDepthFootprint(x0,y0,x1,y1,rise,rise>=.6f||DepthScopeFades,rise>=1f);
            var p=new[]{Project(x0,y0)+lift,Project(x1,y0)+lift,Project(x1,y1)+lift,Project(x0,y1)+lift};
            var top=new Builder();
            BevelCap(top,x0,y0,x1,y1,rise);
            var side=new Builder();var raised=new Builder();
            for(int i=0;i<4;i++)
            {
                if(i==1||i==2)continue; // Rear faces are hidden by this solid block in the fixed isometric view.
                Vector3 a=p[i]-lift,b=p[(i+1)%4]-lift;
                Color face=i==0?new Color(.63f,.66f,.77f):new Color(.43f,.46f,.57f);
                side.WallQuad(a-Vector3.up*depth,b-Vector3.up*depth,b,a,false,face);
                if(rise>0)raised.WallQuad(a,b,p[(i+1)%4],p[i],false,face);
            }
            // Below-floor masonry must not paint over the adjacent walking surface.
            Create(name+" foundation",side,arena.PlatformMaterials.FoundryWall,-16);
            if(rise>0)Create(name+" raised sides",raised,arena.PlatformMaterials.FoundryWall,-14,own);
            Create(name+" cap",top,arena.PlatformMaterials.Floor,rise>0?-13:-15,own);
        }
        // footprint: the structure these vertices take their depth from. -1 uses the enclosing DepthStructure scope,
        // or, outside any scope, the landmark whose volume holds each quad (resolved at Emit).
        private void Create(string name,Builder b,Material material,float order,int footprint=-1)
        {
            Batch target=null;
            foreach(var batch in _batches)if(batch.Material==material&&batch.Order==order){target=batch;break;}
            if(target==null){target=new Batch{Material=material,Order=order};_batches.Add(target);}
            int offset=target.Builder.V.Count;
            while(target.Owners.Count<offset)target.Owners.Add(-1);
            target.Builder.V.AddRange(b.V);target.Builder.U.AddRange(b.U);target.Builder.C.AddRange(b.C);
            foreach(int index in b.T)target.Builder.T.Add(index+offset);
            int owner=footprint>=0?footprint:_depthScope;
            for(int i=0;i<b.V.Count;i++)target.Owners.Add(owner);
        }
        private Mesh Emit(string name,Builder b,Material material,float order,List<int> owners=null)
        {
            // Above-floor ranks carry structure depth in vertex z and write it in a second pass; floor ranks stay flat.
            bool structure=order>=-14;
            List<Vector2> flags=structure?ApplyStructureDepth(b,owners):null;
            var mesh=new Mesh{name=name};mesh.SetVertices(b.V);mesh.SetUVs(0,b.U);if(flags!=null)mesh.SetUVs(1,flags);mesh.SetColors(b.C);mesh.SetTriangles(b.T,0);mesh.RecalculateBounds();_owned.Add(mesh);
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var r=obj.AddComponent<MeshRenderer>();r.sharedMaterial=structure?_visibility.ForStructure(material):material;r.sortingOrder=SceneryOrder(order);return mesh;
        }
        private static int SceneryOrder(float order)
        {
            // Keep the global cavern (-19), floor (-15), contact shadows (-13), and actors (1)
            // intact while reserving intermediate ranks for inset walls and stacked roof parts.
            if(order<=-16)return Mathf.RoundToInt(order)-1;
            if(order<-15)return -16;
            if(order<=-14)return Mathf.RoundToInt(order);
            return Mathf.RoundToInt((order+13)*2)-12;
        }
        private sealed class Builder
        {
            internal readonly List<Vector3> V=new List<Vector3>();internal readonly List<Vector2> U=new List<Vector2>();
            internal readonly List<Color> C=new List<Color>();internal readonly List<int> T=new List<int>();
            // World-continuous UVs keep masonry courses from restarting at every one-unit wall cell.
            internal void WallQuad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool floor,Color color)
            {
                int start=U.Count;Quad(a,b,c,d,floor,color);if(floor)return;
                float slope=Mathf.Abs(b.x-a.x)>.0001f?(b.y-a.y)/(b.x-a.x):0;
                for(int i=0;i<4;i++)
                {
                    Vector3 point=V[start+i];
                    U[start+i]=new Vector2(point.x/2.8f,(point.y-slope*point.x)/2.4f);
                }
            }
            internal void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool floor,Color color)
            {
                float span=Vector3.Distance(a,b)*.32f;
                float height=Vector3.Distance(a,d)/.8f;
                int n=V.Count;var points=new[]{a,b,c,d};var uv=new[]{Vector2.zero,new Vector2(span,0),new Vector2(span,height),new Vector2(0,height)};
                for(int i=0;i<4;i++){V.Add(points[i]);C.Add(color);U.Add(floor?new Vector2(1+(points[i].x+points[i].y*2)/8.55f,1+(-points[i].x+points[i].y*2)/8.55f):uv[i]);}
                T.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            // Maps the last quad onto a texture region: a-b runs along v when lengthAlongV, else along u. t0..t1 is the
            // stretch of the region's length the quad takes, for one piece of a longer strip.
            internal void MapLast(Vector4 region,bool lengthAlongV,float t0=0,float t1=1)
            {
                int s=U.Count-4;
                if(lengthAlongV)
                {
                    float v0=Mathf.Lerp(region.y,region.w,t0),v1=Mathf.Lerp(region.y,region.w,t1);
                    U[s]=new Vector2(region.x,v0);U[s+1]=new Vector2(region.x,v1);U[s+2]=new Vector2(region.z,v1);U[s+3]=new Vector2(region.z,v0);
                }
                else
                {
                    float u0=Mathf.Lerp(region.x,region.z,t0),u1=Mathf.Lerp(region.x,region.z,t1);
                    U[s]=new Vector2(u0,region.y);U[s+1]=new Vector2(u1,region.y);U[s+2]=new Vector2(u1,region.w);U[s+3]=new Vector2(u0,region.w);
                }
            }
        }
        private void OnDestroy(){foreach(var mesh in _owned)if(mesh!=null)Destroy(mesh);}
    }
}
