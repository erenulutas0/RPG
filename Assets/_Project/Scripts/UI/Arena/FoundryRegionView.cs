using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Builds from the same map as movement; owns meshes, borrows imported art.
    // Open-flame tips and bridge cloth animate within their existing shared batches.
    public sealed partial class FoundryRegionView : MonoBehaviour
    {
        private readonly List<Mesh> _owned=new List<Mesh>();
        private readonly HashSet<Vector2Int> _cells=new HashSet<Vector2Int>();
        private Sprite _furnace;
        private FoundryVisibilityView _visibility;
        // Owners runs parallel to Builder.V: the footprint index each vertex takes its depth from (-1 = resolve at Emit).
        private sealed class Batch { internal Builder Builder=new Builder(); internal Material Material; internal float Order; internal readonly List<int> Owners=new List<int>(); }
        private readonly List<Batch> _batches=new List<Batch>();
        private static Vector3 Project(float x,float y) => FoundrySurface.Project(x,y);
        // Authored rigid gate coordinates. Never offset Project itself: the court
        // and near boiler pair remain fixed while only this assembly moves.
        private static float GateU(float authoredU)=>authoredU+FoundryMap.GateOffset;
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
            var floor=new Builder();var flags=new Builder();var walls=new Builder();var trim=new Builder();
            foreach(var cell in _cells)
            {
                float x=cell.x,y=cell.y;
                var p=new[]{Project(x,y),Project(x+1,y),Project(x+1,y+1),Project(x,y+1)};
                BuildPavingCell(floor,flags,cell);
                var neighbours=new[]{Vector2Int.down,Vector2Int.right,Vector2Int.up,Vector2Int.left};
                for(int i=0;i<4;i++)if(!FoundryMap.UncutCell(cell.x+neighbours[i].x,cell.y+neighbours[i].y))
                {
                    Vector3 a=p[i],b=p[(i+1)%4],drop=Vector3.down*2.8f;
                    FoundrySurface.ToDesign((a.x+b.x)*.5f,(a.y+b.y)*.5f,out float edgeU,out float edgeV);
                    if(!CourtEdgeVisible(edgeU,edgeV))continue;
                    walls.WallQuad(a+drop,b+drop,b,a,false,new Color(.7f,.7f,.76f));
                    Vector3 inward=(Project(x+.5f,y+.5f)-(a+b)*.5f)*.18f;
                    trim.Quad(a,b,b+inward,a+inward,false,new Color(.7f,.65f,.54f));
                    MapCoping(trim,CopingTrim.Stone,cell.x*7+cell.y*13+i);
                }
            }
            ClipFloorCorners(floor);ClipFloorCorners(flags);
            BuildDiagonalCourtEdges(arena,walls,trim);
            // Exact straight bridge, using the same diagonal inequalities as movement.
            var bridge=new[]{Deck(FoundryMap.BridgeStart,-FoundryMap.BridgeHalfWidth),Deck(FoundryMap.BridgeStart,FoundryMap.BridgeHalfWidth),
                Deck(FoundryMap.BridgeEnd,FoundryMap.BridgeHalfWidth),Deck(FoundryMap.BridgeEnd,-FoundryMap.BridgeHalfWidth)};
            for(int i=0;i<3;i++)
            {
                int edge=i==2?3:i; Vector3 a=bridge[edge],b=bridge[(edge+1)%4];
                // Outward from the whole edge's midpoint, so shortening the west edge below does not shear its coping.
                Vector3 outward=(a+b)*.5f-Deck((FoundryMap.BridgeStart+FoundryMap.BridgeEnd)*.5f,0);outward.Normalize();
                // Each rail stops at its actual intersection with the court's south edge.
                // The walking quad overlaps the court slightly so its widened corners remain connected.
                if(edge==3)a=Deck(FoundryMap.BridgeWestSeam,-FoundryMap.BridgeHalfWidth);
                if(edge==1)b=Deck(FoundryMap.BridgeEastSeam,FoundryMap.BridgeHalfWidth);
                if(edge==0)UndercroftCoursedFace(walls,a+Vector3.down*2.8f,b+Vector3.down*2.8f,b,a,
                    new Color(.61f,.63f,.70f),4,Mathf.Max(1,Mathf.RoundToInt(Vector3.Distance(a,b)/1.4f)));
                else walls.WallQuad(a+Vector3.down*2.8f,b+Vector3.down*2.8f,b,a,false,new Color(.7f,.7f,.76f));
                // The side edges now carry full outboard parapets. Retain the old
                // low cap only across the physical far end, never the court join.
                if(edge!=0)continue;
                var rail=new Builder();
                rail.WallQuad(a,b,b+Vector3.up*.3f,a+Vector3.up*.3f,false,new Color(.7f,.7f,.76f));
                rail.WallQuad(a+Vector3.up*.3f,b+Vector3.up*.3f,b+Vector3.up*.3f+outward*.25f,a+Vector3.up*.3f+outward*.25f,true,Color.white);
                Create("Bridge coping",rail,arena.PlatformMaterials.FoundryWall,-14);
            }
            ReplaceCourtPavingBase(floor);ReplaceCourtPavingBase(flags);
            BuildCourtSealField(flags);
            CourtFlagstoneField(flags);
            BuildCourtStoneRing(floor);
            BuildCourtProcessionalWay(flags);
            // The laid deck exactly covers the same widened and lengthened walking quad.
            BuildBridgeDeck(flags);
            Create("Layout surface",floor,arena.PlatformMaterials.FoundryPaving,-15);
            Create("Court laid flagstones",flags,arena.PlatformMaterials.FoundryFlagstone,-15);
            // First in the below-deck masonry batch: every other structure under the deck stands in front of the keels.
            BuildHangingKeels(arena);
            Create("Layout sidewalls",RimFace(walls),arena.PlatformMaterials.FoundryWall,-16);
            Create("Layout coping",trim,arena.PlatformMaterials.Coping,FloorDecal);
            // The thin rim parapet gave way to the continuous balustrade BuildMassing raises (2026-09-30).
            BuildLandmarks(arena);
            BuildDressing(arena);
            BuildSiteFinish(arena);
            BuildWorkshop(arena);
            BuildCastingWorkbay(arena);
            BuildClutter(arena);
            BuildUndercroft(arena);
            // Last: round boilers' foundations, balustrade, furnace pit and bastions (FoundryRegionMassing.cs).
            BuildMassing(arena);
            BuildDistantWorks(arena);
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
        // Deck coordinates share the physical bridge inequalities in FoundryMap.
        // s=u+v runs toward the court; d=u-v is the lateral axis.
        private static Vector2 DeckDesign(float s,float d)=>new Vector2((s+d)*.5f,(s-d)*.5f);
        private static Vector3 Deck(float s,float d)=>Project((s+d)*.5f,(s-d)*.5f);
        private const float BridgeLaneHalf=FoundryMap.BridgeHalfWidth*.5f;
        // The bridge is laid in the court's cut stone, as on the concept: its whole walking quad short of the court (which
        // owns v >= South), on the laid stone's light lattice only, in the bridge's own field (BridgeField) - which the
        // processional way to the bridge shares (its end, which dips onto the deck, is cut from it), and the way's
        // continuation down the deck (CourtBridgeContour), cut from the deck and laid once, so way, continuation and deck
        // run on without a seam. Built after CourtFlagstoneField, which would otherwise give it the court's world field.
        private static void BuildBridgeDeck(Builder flags)
        {
            float half=FoundryMap.BridgeHalfWidth;
            var quad=BridgeCourtClip(new List<Vector2>{new Vector2(FoundryMap.BridgeStart,-half),new Vector2(FoundryMap.BridgeStart,half),
                new Vector2(FoundryMap.BridgeEnd,half),new Vector2(FoundryMap.BridgeEnd,-half)});
            var outline=new Vector2[quad.Count];
            for(int i=0;i<outline.Length;i++)outline[i]=DeckDesign(quad[i].x,quad[i].y);
            var deck=new Builder();
            CourtFittedFace(deck,CourtStoneContour(outline),1,Color.white);
            // The way's end dips below the court's edge onto the deck; the way owns it.
            CutCourtPaving(deck,CourtPathContour(CourtProcessionalEnds[0]));
            CutCourtPaving(deck,CourtBridgeContour());
            CourtFittedFace(deck,CourtStoneContour(CourtBridgeContour()),1,Color.white);
            int offset=flags.V.Count;
            for(int i=0;i<deck.V.Count;i++)
            {
                FoundrySurface.ToDesign(deck.V[i].x,deck.V[i].y,out float u,out float v);
                flags.V.Add(deck.V[i]);flags.C.Add(deck.C[i]);flags.U.Add(BridgeField(u,v));
            }
            foreach(int index in deck.T)flags.T.Add(offset+index);
        }
        // The bridge's laid-stone field: texture u across the bridge, v along it, at CourtWayScale the court's density, so
        // its courses lie across the bridge like a laid road.
        private static Vector2 BridgeField(float u,float v)=>new Vector2(u-v,u+v)*(FoundryMap.Axis*CourtWayScale/FlagstoneSpan);
        // The court already owns v >= South. Clip only bridge presentation at that
        // diagonal in deck space, so the physical overlap never paints a dark rectangle
        // over the court's existing ashlar. Collision retains its robust shared overlap.
        private static List<Vector2> BridgeCourtClip(List<Vector2> poly)
        {
            var kept=new List<Vector2>(poly.Count+1);
            for(int i=0;i<poly.Count;i++)
            {
                Vector2 a=poly[i],b=poly[(i+1)%poly.Count];
                float fa=2*FoundryMap.South-a.x+a.y,fb=2*FoundryMap.South-b.x+b.y;
                if(fa>=0)kept.Add(a);
                if(fa*fb<0)kept.Add(Vector2.Lerp(a,b,fa/(fa-fb)));
            }
            return kept;
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
        // The photograph now owns the composition: broad court, inset shaft, south bridge, northeast gate.
        private void BuildLandmarks(ArenaView arena)
        {
            var inlays=new Builder();
            Color brass=CeremonialBrass;
            BuildCourtSeal(arena);
            BuildCourtInlayNetwork(arena);
            foreach(float d in new[]{-BridgeLaneHalf,BridgeLaneHalf})
            {
                Vector2 a=DeckDesign(FoundryMap.BridgeStart+2,d),b=DeckDesign(2*FoundryMap.South-1,d);
                Strip(inlays,a.x,a.y,b.x,b.y,.12f,brass,CopingTrim.Brass);
            }
            BridgeDiamond(inlays,brass);
            // The court's processional settings are stone in the existing floor
            // mesh; only the exposed bridge retains its narrow brass lane inlays.
            Create("Court brass inlays",inlays,arena.PlatformMaterials.Coping,FloorDecal);

            BuildGate(arena);
            // Four round banded boilers, piped to each other and to the gate, replace the 3x3 furnace boxes.
            BuildBoilerTowers(arena);
        }
        // As on the concept, a brass diamond set into the bridge's deck between its lanes, below the keepers: a frame, an
        // inner frame and a cross through them, the frames square in the design plane so they read as a diamond on screen,
        // their corners clear of the lane inlays.
        private const float BridgeDiamondS=-52f,BridgeDiamondHalf=1.1f;
        private static void BridgeDiamond(Builder inlays,Color brass)
        {
            Vector2 c=DeckDesign(BridgeDiamondS,0);
            foreach(float half in new[]{BridgeDiamondHalf,BridgeDiamondHalf*.5f})
            {
                var corner=new[]{c+new Vector2(-half,-half),c+new Vector2(half,-half),c+new Vector2(half,half),c+new Vector2(-half,half)};
                for(int i=0;i<4;i++)
                {
                    Vector2 a=corner[i],b=corner[(i+1)%4];
                    Strip(inlays,a.x,a.y,b.x,b.y,half==BridgeDiamondHalf?.11f:.08f,brass,CopingTrim.Brass,40+i);
                }
            }
            foreach(Vector2 axis in new[]{new Vector2(1,0),new Vector2(0,1)})
            {
                Vector2 a=c-axis*BridgeDiamondHalf,b=c+axis*BridgeDiamondHalf;
                Strip(inlays,a.x,a.y,b.x,b.y,.06f,brass,CopingTrim.Brass,48);
            }
        }

        private void BuildGate(ArenaView arena)
        {
            var facade=arena.PlatformMaterials.GateFacade;
            float height=facade!=null?6.5f:4.8f;
            // One structure for depth: foundations, body, facade, pediment, inlays and the gate beacons.
            using(DepthStructure(GateU(32),-3,GateU(35),13,height+1.7f))BuildGateParts(arena,facade,height);
        }
        private void BuildGateParts(ArenaView arena,Material facade,float height)
        {
            Block(arena,GateU(32),-3,GateU(35),-1,facade!=null?.4f:4.8f,3,"Gate left foundation");
            Block(arena,GateU(32),11,GateU(35),13,facade!=null?.4f:4.8f,3,"Gate right foundation");
            Block(arena,GateU(32),-1,GateU(34),11,height,3,"Gate masonry body");
            if(facade==null)return;
            // Full rectangular elevation on one physical wall; no cutout alpha or baked isometric tilt.
            Vector3 a=Project(GateU(31.99f),11),b=Project(GateU(31.99f),-1),lift=Vector3.up*height;
            var face=new Builder();face.Quad(a,b,b+lift,a+lift,false,Color.white);
            face.U.Clear();face.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            Create("Painted gate facade",face,facade,-12);
            BuildGateDoorway(facade,height);
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
        // vertex colour itself lands in the fixed4 interpolator (guaranteed +/-2). Both stone and the brighter
        // ceremonial brass use DecalCeiling below 2 after lighting; scaling preserves the material hue.
        private const float BrassFloor=.3f,BrassGain=1.25f,DecalCeiling=1.9f;
        private static void LightFloorDecals(Builder b)
        {
            for(int i=0;i<b.V.Count;i++)
            {
                // Flush vertices only: world y is the floor's y halved (Project), z is still 0 on this rank.
                FoundrySurface.ToDesign(b.V[i].x,b.V[i].y,out float u,out float v);
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
            if(order==FloorDecal)ClipFloorCorners(b);
            Batch target=null;
            foreach(var batch in _batches)if(batch.Material==material&&batch.Order==order){target=batch;break;}
            if(target==null){target=new Batch{Material=material,Order=order};_batches.Add(target);}
            int offset=target.Builder.V.Count;
            while(target.Owners.Count<offset)target.Owners.Add(-1);
            target.Builder.V.AddRange(b.V);target.Builder.U.AddRange(b.U);target.Builder.C.AddRange(b.C);
            foreach(int index in b.T)target.Builder.T.Add(index+offset);
            foreach(var tip in b.Flames)target.Builder.Flames.Add(new FoundryFlameVertex(tip.Index+offset,tip.Weight,tip.Phase));
            foreach(var cloth in b.Cloth)target.Builder.Cloth.Add(new FoundryBannerVertex(cloth.Index+offset,cloth.Drop,cloth.Phase));
            foreach(var leaf in b.Door)target.Builder.Door.Add(new FoundryDoorVertex(leaf.Index+offset,leaf.Hinge,leaf.Reach,leaf.Height));
            int owner=footprint>=0?footprint:_depthScope;
            for(int i=0;i<b.V.Count;i++)target.Owners.Add(owner);
        }
        private Mesh Emit(string name,Builder b,Material material,float order,List<int> owners=null)
        {
            // Above-floor ranks carry structure depth in vertex z and write it in a second pass; floor ranks stay flat.
            bool structure=order>=-14;
            List<Vector2> flags=structure?ApplyStructureDepth(b,owners):null;
            var mesh=new Mesh{name=name};mesh.SetVertices(b.V);mesh.SetUVs(0,b.U);if(flags!=null)mesh.SetUVs(1,flags);mesh.SetColors(b.C);mesh.SetTriangles(b.T,0);mesh.RecalculateBounds();_owned.Add(mesh);
            var obj=new GameObject(name);obj.transform.SetParent(transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;var r=obj.AddComponent<MeshRenderer>();r.sharedMaterial=structure?_visibility.ForStructure(material):material;r.sortingOrder=SceneryOrder(order);
            if(b.Flames.Count>0)obj.AddComponent<FoundryFlameMotion>().Initialize(mesh,b.Flames);
            if(b.Cloth.Count>0)obj.AddComponent<FoundryBannerMotion>().Initialize(mesh,b.Cloth);
            if(b.Door.Count>0)
            {
                // Open leaves swing up to two units out onto the landing; reaching far below the map puts the batch's
                // centre under the facade's, so in their shared sorting order the leaves always draw after the painting.
                var bounds=mesh.bounds;bounds.Expand(4f);bounds.Encapsulate(bounds.min+Vector3.down*DecalSortReach);mesh.bounds=bounds;
                obj.AddComponent<FoundryGateDoorMotion>().Initialize(mesh,b.Door,GateDoorFaceU);
            }
            return mesh;
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
            internal readonly List<FoundryFlameVertex> Flames=new List<FoundryFlameVertex>();
            internal readonly List<FoundryBannerVertex> Cloth=new List<FoundryBannerVertex>();
            internal readonly List<FoundryDoorVertex> Door=new List<FoundryDoorVertex>();
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
        private void OnDestroy()
        {
            foreach(var mesh in _owned)if(mesh!=null)Destroy(mesh);
            if(_distantMasonryMaterial!=null)Destroy(_distantMasonryMaterial);
            if(_salvageMaterial!=null)Destroy(_salvageMaterial);
            if(_stockHoistMaterial!=null)Destroy(_stockHoistMaterial);
            if(_stockStationMaterial!=null)Destroy(_stockStationMaterial);
            if(_castingBenchMaterial!=null)Destroy(_castingBenchMaterial);
            if(_gateMasonryMaterial!=null)Destroy(_gateMasonryMaterial);
            if(_craneMaterial!=null)Destroy(_craneMaterial);
            if(_reservoirMaterial!=null)Destroy(_reservoirMaterial);
            if(_bakedBoilerMaterial!=null)Destroy(_bakedBoilerMaterial);
            if(_bakedShortBoilerMaterial!=null)Destroy(_bakedShortBoilerMaterial);
        }
    }
}
