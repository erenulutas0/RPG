using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Monumental massing, after the approved layout: four round banded boilers piped to each other and to the
    // gate, a continuous heavy balustrade with posts and lanterns along every exposed edge, a deep glowing
    // furnace pit with inner boilers inside a round balustraded collar, and foundation bastions with chains under
    // the deck. A few large silhouettes that read at the portrait camera, not small ornament. Every above-deck
    // footprint is tested against the shared walking union and skipped rather than allowed to intrude on it (the
    // pit's lie wholly inside its hole). Pit bounds come from the shared walking map.
    //
    // Painter order is by (material, order) batch, then by emission inside a batch. The stacks below respect it:
    // boiler plinths -13 (floor stone), painted boiler drums -12, cornices, balustrade, pit collar and collar stone
    // -11 (masonry), iron shells, domes and pipes -10.5, fire windows and lantern panes -10; pit lining and molten pool
    // -15.5 under the floor; below-deck stone -16. Nothing above the deck uses -14: that sorting rank is shared with the
    // flush floor decals, which would otherwise paint over a balustrade standing in front of them.
    public sealed partial class FoundryRegionView
    {
        // Boiler centres (design u,v): two behind the gate apron (north), two in front of it (south). Each sits
        // wholly on void ground, flush against the apron edges, and meets the existing service mains
        // (v -4.5 / 14.5) on their drum surfaces. Only the far pair moves with the gate.
        private static readonly Vector2[] BoilerCentres =
        {
            new Vector2(18.39f,15.81f),new Vector2(GateU(26.95f),14.5f),new Vector2(17.9f,-4.08f),new Vector2(GateU(26.9f),-4.08f)
        };
        private const float BoilerPlinth=2f,BoilerDrum=1.85f,BoilerShell=1.6f;
        private const float ServiceMainStart=19.7f,ServiceMainEnd=25.1f+FoundryMap.GateOffset;
        private const float ServiceMiddle=(ServiceMainStart+ServiceMainEnd)*.5f;
        private const float NorthReservoirV=19f,SouthReservoirV=-7.4f;
        private static Vector4 ReservoirFootprint(float v)=>new Vector4(ServiceMiddle-1,v-1,ServiceMiddle+1,v+1);
        private static Vector4 ReservoirNeck(bool north)
        {
            Vector2 boiler=BoilerCentres[north?0:2];
            return new Vector4(boiler.x+.8f,north?boiler.y+1.25f:SouthReservoirV+.8f,
                ServiceMiddle-.3f,north?NorthReservoirV-.75f:boiler.y-1.25f);
        }
        private const float DrumBase=.45f,DrumTop=3.35f,CorniceTop=3.6f,ShellTop=5.4f,FlueTop=7.2f;
        private const int BoilerFacets=10;
        // Balustrade: a .42-deep footprint band on the void side of each exposed edge; the balustrade itself stands in
        // the band's camera-facing RailDepth (RailSpan).
        private const float RailBand=.42f,RailTop=.72f,DeckFace=-2.3f;
        // Furnace pit: the existing non-walkable shaft square, its collar round after the approved layout. The balustrade
        // stands on a band RailDepth deep whose outline is a rounded square about the pit's centre (half-size PitRim, corner
        // radius PitRound): the band's inner edge keeps the old collar line, PitCollar in from the hole, on the short
        // straights at the mid posts and rounds the corners; the lining follows that edge's far half down to a pool of molten
        // metal PitMolten below the deck (over the deep coal bed), and collar stone fills the hole outside the band. The
        // walkable edge stays the hole's.
        private const float PitU0=FoundryMap.PitWest,PitV0=FoundryMap.PitSouth,PitU1=FoundryMap.PitEast,PitV1=FoundryMap.PitNorth;
        private const float PitCollar=.55f,PitDepth=6.6f;
        internal const float PitMolten=5.2f;
        private const float PitHalf=(PitU1-PitU0)*.5f,PitRim=PitHalf-PitCollar+RailDepth,PitRound=3.2f;
        private const float PitStraight=PitRim-PitRound,PitQuarter=2*PitStraight+PitRound*Mathf.PI*.5f;
        private static readonly Vector2 PitCentre=new Vector2((PitU0+PitU1)*.5f,(PitV0+PitV1)*.5f);
        private static readonly Vector2[] PitBoilers={new Vector2(-12.6f,10.6f),new Vector2(-7.4f,5.4f)};
        private static readonly Color LitFace=new Color(.63f,.66f,.77f),ShadeFace=new Color(.43f,.46f,.57f);
        private static readonly Color CapTone=new Color(.86f,.89f,.98f),Bronze=new Color(1.22f,.95f,.6f);
        // The rim's stone reads as the concept's violet battlement (saturation ~.32): above the deck (balustrade spans,
        // posts, lantern posts, finials, the pit collar's guard) a little darker, value ~.20 -> ~.18; below it (cornice,
        // pilasters, court sidewalls) only shifted toward violet at the same value (saturation ~.21 -> ~.3). Applied after
        // construction, so geometry, texture mapping and relative light stay as built.
        internal static readonly Color RimStoneTone=new Color(.80f,.77f,.92f),RimFaceTone=new Color(.90f,.88f,1.06f);
        private static Builder RimStone(Builder b)=>RimTint(b,RimStoneTone);
        private static Builder RimFace(Builder b)=>RimTint(b,RimFaceTone);
        private static Builder RimTint(Builder b,Color tone)
        {
            for(int i=0;i<b.C.Count;i++)b.C[i]=Tinted(b.C[i],tone);
            return b;
        }

        private sealed class MassPiece
        {
            internal float Depth;
            internal System.Action Emit;
        }

        // The two south boilers stand in front of the walkable gate apron, so they are squat: at the portrait
        // camera their crown stays about one unit above the apron's front edge instead of rising over the hero.
        // Plinth footprint and centres are unchanged; the drum still receives the service mains (height 1.8)
        // through an iron junction box on the cornice.
        private const float SquatPlinthTop=.3f,SquatDrumTop=1.3f,SquatCorniceTop=1.45f,SquatTop=2.28f;
        private static bool SquatBoiler(int index)=>index>=2;
        private static float BoilerTop(int index)=>SquatBoiler(index)?SquatTop:FlueTop;
        private static float BoilerBase(int index)=>SquatBoiler(index)?SquatPlinthTop:DrumBase;
        private static float BoilerDrumTop(int index)=>SquatBoiler(index)?SquatDrumTop:DrumTop;

        // Smoke/heat source at each boiler's flue mouth, for FoundryAtmosphereView.
        internal static int BoilerCount=>BoilerCentres.Length;
        internal static Vector3 BoilerFlue(int index)=>
            Project(BoilerCentres[index].x,BoilerCentres[index].y)+Vector3.up*(BoilerTop(index)+.05f);

        // Replaces the four 3x3 furnace boxes. Below-deck foundations are emitted later by BuildMassing so they
        // paint over the balustrade cornice behind them.
        private void BuildBoilerTowers(ArenaView arena)
        {
            for(int i=0;i<BoilerCentres.Length;i++)
            {
                Vector2 c=BoilerCentres[i];
                using(MassDepth(new Vector4(c.x-BoilerPlinth,c.y-BoilerPlinth,c.x+BoilerPlinth,c.y+BoilerPlinth),BoilerTop(i)))
                {
                    if(BuildBakedBoiler(arena,c,SquatBoiler(i)))continue;
                    if(SquatBoiler(i))BuildSquatBoiler(arena,c);
                    else BuildTallBoiler(arena,c);
                }
            }
            BuildBoilerPipes(arena);
        }

        private void BuildTallBoiler(ArenaView arena,Vector2 c)
        {
            var set=arena.PlatformMaterials;
            var plinth=new Builder();
            MassCourse(plinth,c,BoilerPlinth,BoilerPlinth,0,DrumBase,BoilerFacets,false,new Color(1.1f,1f,.9f));
            MassDisk(plinth,c,BoilerPlinth,DrumBase,BoilerFacets,false,new Color(.84f,.84f,.88f));
            Create("Boiler plinth",plinth,set.Floor,-13);
            // The painted furnace front wraps the visible half of the drum twice: two fire grates per boiler.
            BoilerDrumFaces(set,c,DrumBase,DrumTop,0,1);
            // A heavy overhanging stone cornice carries the iron boiler above it.
            var crown=new Builder();
            MassCourse(crown,c,BoilerPlinth,BoilerPlinth+.03f,DrumTop,CorniceTop,BoilerFacets,false,new Color(1.08f,1.08f,1.08f));
            MassDisk(crown,c,BoilerPlinth+.03f,CorniceTop,BoilerFacets,false,new Color(.7f,.73f,.82f));
            Create("Boiler cornice",crown,set.FoundryWall,-11);
            // Riveted iron shell, bronze hoops, stepped dome and a flue: bottom-up in painter order. The shell is
            // two courses of two-facet plates with staggered seams, so it reads as one banded drum.
            var iron=new Builder();
            MassCourse(iron,c,BoilerShell,BoilerShell,CorniceTop,4.5f,BoilerFacets,true,Color.white,forgeLight:true);
            MassCourse(iron,c,BoilerShell,BoilerShell,4.5f,ShellTop,BoilerFacets,true,Color.white,1,forgeLight:true);
            MassCourse(iron,c,BoilerShell+.06f,BoilerShell+.06f,CorniceTop,CorniceTop+.15f,BoilerFacets,true,Bronze,forgeLight:true);
            MassCourse(iron,c,BoilerShell+.07f,BoilerShell+.07f,ShellTop-.1f,ShellTop+.06f,BoilerFacets,true,Bronze,forgeLight:true);
            MassDisk(iron,c,BoilerShell+.07f,ShellTop+.06f,BoilerFacets,true,new Color(.46f,.48f,.54f),forgeLight:true);
            float h=ShellTop+.06f;
            FormedBoilerCourse(iron,c,1.42f,1.12f,h,h+.36f,h,h+.86f,10);
            FormedBoilerCourse(iron,c,1.12f,.72f,h+.36f,h+.68f,h,h+.86f,10);
            FormedBoilerCourse(iron,c,.72f,.36f,h+.68f,h+.86f,h,h+.86f,10);
            BoilerVent(iron,c,h+.86f,FlueTop,.3f);
            var glow=new Builder();
            ShellWindow(iron,glow,c,3);
            ShellWindow(iron,glow,c,6);
            Create("Boiler iron shell and dome",iron,set.FoundryIron,-10.5f);
            if(set.FoundryEmbers!=null)
            {
                HotSeam(glow,0);
                Create("Boiler fire windows",glow,set.FoundryEmbers,-10);
            }
        }

        private void BuildSquatBoiler(ArenaView arena,Vector2 c)
        {
            var set=arena.PlatformMaterials;
            var plinth=new Builder();
            MassCourse(plinth,c,BoilerPlinth,BoilerPlinth,0,SquatPlinthTop,BoilerFacets,false,new Color(1.1f,1f,.9f));
            MassDisk(plinth,c,BoilerPlinth,SquatPlinthTop,BoilerFacets,false,new Color(.84f,.84f,.88f));
            Create("Boiler plinth",plinth,set.Floor,-13);
            // Only the grate band of the painted front (its bottom half), so the fire keeps its proportions.
            BoilerDrumFaces(set,c,SquatPlinthTop,SquatDrumTop,.14f,.5f);
            var crown=new Builder();
            MassCourse(crown,c,BoilerPlinth-.1f,BoilerPlinth-.08f,SquatDrumTop,SquatCorniceTop,BoilerFacets,false,new Color(1.08f,1.08f,1.08f));
            MassDisk(crown,c,BoilerPlinth-.08f,SquatCorniceTop,BoilerFacets,false,new Color(.7f,.73f,.82f));
            Create("Boiler cornice",crown,set.FoundryWall,-11);
            // A low riveted dome on a bronze hoop, and a short vent in place of the tall flue.
            var iron=new Builder();
            // The existing service main (v -4.5, height 1.8) now meets the boiler above its drum: it enters an
            // iron junction box standing on the cornice. A box on the hidden side goes under the dome.
            float mainU=c.x<ServiceMiddle?ServiceMainStart:ServiceMainEnd;
            Vector2 toward=(new Vector2(mainU,-4.5f)-c).normalized;
            Vector2 box=c+toward*(BoilerDrum+.03f);
            bool boxVisible=toward.x+toward.y<0;
            if(!boxVisible)WorkshopMetalHull(iron,box.x-.26f,box.y-.26f,box.x+.26f,box.y+.26f,SquatCorniceTop,2.06f,.04f);
            MassCourse(iron,c,1.62f,1.62f,SquatCorniceTop,1.55f,BoilerFacets,true,Bronze,forgeLight:true);
            MassDisk(iron,c,1.62f,1.55f,BoilerFacets,true,new Color(.46f,.48f,.54f),forgeLight:true);
            FormedBoilerCourse(iron,c,1.52f,1.12f,1.55f,1.78f,1.55f,2f,10);
            FormedBoilerCourse(iron,c,1.12f,.64f,1.78f,1.93f,1.55f,2f,10);
            FormedBoilerCourse(iron,c,.64f,.34f,1.93f,2f,1.55f,2f,10);
            MassDisk(iron,c,.34f,2f,6,true,new Color(.5f,.52f,.58f),forgeLight:true);
            BoilerVent(iron,c,2f,SquatTop,.2f);
            if(boxVisible)WorkshopMetalHull(iron,box.x-.26f,box.y-.26f,box.x+.26f,box.y+.26f,SquatCorniceTop,2.06f,.04f);
            Create("Boiler iron dome",iron,set.FoundryIron,-10.5f);
        }

        private void BuildSquatBoilerSocket(Builder iron,Vector2 c)
        {
            float mainU=c.x<ServiceMiddle?ServiceMainStart:ServiceMainEnd;
            Vector2 toward=(new Vector2(mainU,-4.5f)-c).normalized;
            Vector2 box=c+toward*(BoilerDrum+.03f);
            WorkshopMetalHull(iron,box.x-.26f,box.y-.26f,box.x+.26f,box.y+.26f,SquatCorniceTop,2.06f,.04f);
        }

        // Formed hoods use a continuous plain-metal sample, not a full riveted plate
        // on every tapered facet. Shared-angle lighting joins all three courses; soot
        // gathers towards the flue and one broad cold highlight describes the curvature.
        // Existing footprint, depth owner, material and lower riveted shell are retained.
        private static void FormedBoilerCourse(Builder b,Vector2 c,float r0,float r1,
            float h0,float h1,float baseHeight,float topHeight,int segments)
        {
            // Cover the far slope too: the old flat cap left a dark crescent behind the
            // flue, making the simplified hood read as a dish. Emit far before near.
            for(int k=-segments;k<segments;k++)
            {
                float a=FrontAngle(k,segments),e=FrontAngle(k+1,segments);
                int first=b.V.Count;
                FullFace(b,OnRing(c,r0,a,h0),OnRing(c,r0,e,h0),
                    OnRing(c,r1,e,h1),OnRing(c,r1,a,h1),Color.white);
                for(int j=0;j<4;j++)
                {
                    float angle=j==0||j==3?a:e,height=j<2?h0:h1;
                    float t=Mathf.InverseLerp(baseHeight,topHeight,height);
                    float side=(1+Mathf.Cos(angle-285f*Mathf.Deg2Rad))*.5f;
                    float highlight=Mathf.Pow(Mathf.Max(0,Mathf.Cos(angle-270f*Mathf.Deg2Rad)),8);
                    Color tone=Color.Lerp(new Color(.44f,.46f,.50f),new Color(.88f,.87f,.81f),side);
                    tone+=new Color(.19f,.21f,.25f,0)*highlight;
                    tone*=1-.14f*t*t;tone.a=1;
                    b.C[first+j]=MetalTone(tone,b.V[first+j],height);
                    b.U[first+j]=new Vector2(.18f+.64f*(angle+45f*Mathf.Deg2Rad)/(2*Mathf.PI),.24f+.5f*t);
                }
            }
            // Sparse standing seams, continuous across the three courses, describe the
            // convex form without returning to a tiny riveted panel on every facet.
            for(int k=-segments+2;k<segments;k+=4)
            {
                float a=FrontAngle(k,segments),width=.012f;
                int first=b.V.Count;
                FullFace(b,OnRing(c,r0,a-width,h0+.004f),OnRing(c,r0,a+width,h0+.004f),
                    OnRing(c,r1,a+width,h1+.004f),OnRing(c,r1,a-width,h1+.004f),new Color(.30f,.32f,.36f));
                for(int j=0;j<4;j++)
                {
                    b.U[first+j]=new Vector2(j==0||j==3?.3f:.7f,j<2?.3f:.7f);
                    b.C[first+j]=MetalTone(b.C[first+j],b.V[first+j],(j<2?h0:h1)+.004f);
                }
            }
        }

        private void BoilerDrumFaces(PlatformMaterialSet set,Vector2 c,float bottom,float top,float v0,float v1)
        {
            var drum=new Builder();
            if(set.FurnaceFacade!=null)
            {
                PaintedDrum(drum,c,BoilerDrum,bottom,top,BoilerFacets,2,Color.white,v0,v1);
                Create("Boiler painted drum",drum,set.FurnaceFacade,-12);
                return;
            }
            MassCourse(drum,c,BoilerDrum,BoilerDrum,bottom,top,BoilerFacets,false,Color.white);
            Create("Boiler masonry drum",drum,set.Floor,-13);
        }

        // Stack or vent with a bronze rim and a dark open mouth.
        private static void BoilerVent(Builder iron,Vector2 c,float bottom,float top,float radius)
        {
            float rim=radius*1.25f,lip=Mathf.Min(.12f,(top-bottom)*.4f);
            MassCourse(iron,c,radius,radius,bottom,top-lip,6,true,new Color(.82f,.82f,.85f));
            MassCourse(iron,c,rim,rim,top-lip,top,6,true,Bronze);
            MassDisk(iron,c,rim,top,6,true,new Color(.5f,.46f,.4f));
            MassDisk(iron,c,rim*.65f,top,6,true,new Color(.07f,.07f,.09f));
        }

        private void BuildBoilerPipes(ArenaView arena)
        {
            // The north pair is linked at the shell's lower course: from the rear boiler's silhouette edge to the
            // visible face of the front one, so the pipe never paints across a drum it passes behind. The squat
            // south pair is already joined by the existing service main.
            {
                Vector2 left=BoilerCentres[0],right=BoilerCentres[1];
                Vector2 start=left+new Vector2(.7071f,-.7071f)*BoilerShell;
                float dv=start.y-right.y,du=Mathf.Sqrt(Mathf.Max(0,BoilerShell*BoilerShell-dv*dv));
                MassPipe(arena,start,new Vector2(right.x-du,start.y),4.05f,.42f);
            }
            // Heavy feeds from the two east boilers into the gate towers' shaft faces; the squat south boiler's
            // leaves its drum at knee height.
            const float southFeed=1f,northFeed=2.6f;
            Vector2 south=BoilerCentres[3]+new Vector2(.7071f,-.7071f)*BoilerDrum,elbow=new Vector2(GateU(33.3f),south.y);
            // This feed crosses the near buttress's opaque baked pixels. Keep
            // body, collars and elbow strictly in front of that separate material.
            const float southFeedOrder=-10;
            MassPipe(arena,south,elbow,southFeed,.5f,southFeedOrder);
            MassPipe(arena,elbow,new Vector2(GateU(33.3f),-2.76f),southFeed,.5f,southFeedOrder);
            using(MassDepth(new Vector4(GateU(33f),south.y-.3f,GateU(33.6f),south.y+.3f),southFeed+.34f))
            {
                var joint=new Builder();
                WorkshopMetalHull(joint,GateU(33f),south.y-.3f,GateU(33.6f),south.y+.3f,southFeed-.32f,southFeed+.34f,.05f);
                Create("Boiler feed elbow",joint,arena.PlatformMaterials.FoundryIron,southFeedOrder);
            }
            Vector2 north=BoilerCentres[1];
            const float feedV=12.72f;
            float ndv=feedV-north.y,ndu=Mathf.Sqrt(Mathf.Max(0,BoilerDrum*BoilerDrum-ndv*ndv));
            MassPipe(arena,new Vector2(north.x+ndu,feedV),new Vector2(GateU(32.26f),feedV),northFeed,.5f);
        }

        // An elevated main between design points, with collars and end flanges, as one structure for depth.
        private void MassPipe(ArenaView arena,Vector2 from,Vector2 to,float height,float width,float order=-10.5f)
        {
            using var depth=MassDepth(new Vector4(Mathf.Min(from.x,to.x)-.25f,Mathf.Min(from.y,to.y)-.25f,
                Mathf.Max(from.x,to.x)+.25f,Mathf.Max(from.y,to.y)+.25f),height+width);
            Vector3 lift=Vector3.up*height,a=Project(from.x,from.y)+lift,b=Project(to.x,to.y)+lift;
            PressureTube(arena,a,b,width,order,height);
            Vector3 d=(b-a).normalized;
            int bands=Mathf.Max(1,Mathf.RoundToInt(Vector3.Distance(a,b)/1.5f));
            for(int i=0;i<=bands;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,i/(float)bands);
                bool end=i==0||i==bands;
                float halfLength=width*(end?.17f:.10f);
                PressureTube(arena,p-d*halfLength,p+d*halfLength,width*(end?1.6f:1.28f),order,height,!end);
            }
        }

        private static void ShellWindow(Builder iron,Builder glow,Vector2 c,int facet)
        {
            float a=FrontAngle(facet,BoilerFacets),e=FrontAngle(facet+1,BoilerFacets),r=BoilerShell+.012f;
            float fa=Mathf.Lerp(a,e,.16f),fe=Mathf.Lerp(a,e,.84f),ga=Mathf.Lerp(a,e,.3f),ge=Mathf.Lerp(a,e,.7f);
            FullFace(iron,OnRing(c,r,fa,4.58f),OnRing(c,r,fe,4.58f),OnRing(c,r,fe,5.06f),OnRing(c,r,fa,5.06f),
                new Color(.2f,.21f,.25f));
            FullFace(glow,OnRing(c,r+.004f,ga,4.66f),OnRing(c,r+.004f,ge,4.66f),OnRing(c,r+.004f,ge,4.98f),
                OnRing(c,r+.004f,ga,4.98f),new Color(1f,.82f,.56f));
        }

        // Balustrade, furnace pit, boiler foundations, bastions and chains. Call after BuildUndercroft.
        private void BuildMassing(ArenaView arena)
        {
            var pieces=new List<MassPiece>();
            BuildFurnacePit(arena,pieces);
            BuildBalustrade(arena,pieces);
            // Far pieces first, so a post or a collar in front always paints over the rail behind it.
            pieces.Sort((x,y)=>y.Depth.CompareTo(x.Depth));
            foreach(var piece in pieces)piece.Emit();
            BuildBoilerFoundations(arena);
            BuildBastions(arena);
        }

        private void BuildFurnacePit(ArenaView arena,List<MassPiece> pieces)
        {
            var set=arena.PlatformMaterials;
            float iu0=PitU0+PitCollar,iv0=PitV0+PitCollar,iu1=PitU1-PitCollar,iv1=PitV1-PitCollar;
            Vector3 floor=Vector3.down*PitDepth;
            if(set.FoundryEmbers!=null)
            {
                // The coal bed keeps its square: past the band's rounded inner edge it lies under the collar stone, the
                // floor or the lining, and under the pool it closes any seam the pool leaves.
                var basin=new Builder();
                FullFace(basin,Project(iu0,iv0)+floor,Project(iu1,iv0)+floor,Project(iu1,iv1)+floor,Project(iu0,iv1)+floor,
                    new Color(.58f,.36f,.23f));
                Create("Furnace pit coal bed",basin,set.FoundryEmbers,-17);
                var pool=new Builder();
                PitPool(pool);
                Create("Furnace pit molten pool",pool,set.FoundryEmbers,-15.5f);
            }
            if(set.FoundryPitLining!=null)
            {
                var walls=new Builder();
                PitLiningFronts(walls);
                PitFurnaceBays(walls);
                // Long iron vessels descend with the lining to the deep pool. The dedicated
                // lining atlas contains a pipe strip used by their body, ribs and belts.
                foreach(var c in PitBoilers)PitDrum(walls,c);
                Create("Furnace pit lining and inner boilers",walls,set.FoundryPitLining,-15.5f);
            }
            var wall=set.FoundryWall;
            // Collar: lantern pedestals on the corner arcs and mid posts on the straights, each one structure for depth, and
            // between each pair a run of the curved balustrade, one structure too; all sort far to near with the rim's
            // pieces. The posts stand on the collar stone, from the plinth course's height; where a pedestal's camera face
            // stands over the stone's lower step, a skirt carries it down to the step (PitSkirt). Under the far mid posts,
            // which cut the far runs' plinth face, that face runs on behind their front, so the pit never shows below them.
            var posts=PitPosts();
            for(int k=0;k<8;k++)
            {
                Vector4 post=posts[k];
                Vector2 side=PitHeading(k*Mathf.PI*.25f);
                bool farPost=k==0||k==2;
                if(k%2==1)AddPiece(pieces,post,2.8f,()=>
                {
                    var skirt=new Builder();
                    PitSkirt(skirt,post);
                    Create("Pit collar post",RimStone(skirt),wall,-11);
                    LanternPost(arena,post,false,RailPlinth);
                });
                else AddPiece(pieces,post,1.56f,()=>
                {
                    var above=new Builder();
                    if(farPost)
                    {
                        Vector2 line=PitCentre+side*(PitRim-RailDepth),along=new Vector2(-side.y,side.x)*.3f;
                        Vector3 a=Project(line.x+along.x,line.y+along.y),c=Project(line.x-along.x,line.y-along.y);
                        if(a.x>c.x){var swap=a;a=c;c=swap;}
                        CourseFace(above,a,c,0,RailPlinth,Tinted(PitFacing(Mathf.Atan2(-side.y,-side.x)),new Color(1.02f,1.02f,1.02f)),PlinthCourse);
                    }
                    RailPost(above,null,post,false,RailPlinth);
                    Create("Pit collar post",RimStone(above),wall,-11);
                    RailPostBrass(arena.PlatformMaterials.FoundryIron,post,0);
                });
                var lines=PitRunLines(posts,k,out bool near);
                AddPiece(pieces,PitBounds(lines[0],lines[2]),RailTop,
                    ()=>{var above=new Builder();PitGuardRun(above,lines,near);Create("Pit collar",RimStone(above),wall,-11);});
            }
            // Collar stone in six stretches, split at the outline's silhouette diagonals, before every other piece of the pit:
            // the runs and posts all stand on or in front of it, and beyond it lies only the walkable floor.
            foreach(var stretch in new[]{new Vector2(0,1),new Vector2(1,1.5f),new Vector2(1.5f,2),new Vector2(2,3),
                new Vector2(3,3.5f),new Vector2(3.5f,4)})
            {
                float t0=stretch.x*PitQuarter,t1=stretch.y*PitQuarter;
                pieces.Add(new MassPiece{Depth=PitU1+PitV1,Emit=()=>
                {
                    var stone=new Builder();
                    Vector4 box=PitCollarStone(stone,t0,t1);
                    using(MassDepth(box,RailPlinth))Create("Pit collar stone",stone,wall,-11);
                }});
            }
            // The existing formed hoods stay just above deck level; the longer industrial
            // vessels beneath them now establish the shaft's depth without hiding the court.
            foreach(var c in PitBoilers)
            {
                Vector2 centre=c;
                AddPiece(pieces,new Vector4(centre.x-1.1f,centre.y-1.1f,centre.x+1.1f,centre.y+1.1f),1.3f,
                    ()=>
                    {
                        var dome=new Builder();PitHoodStoneFoot(dome,centre);Create("Pit boiler dome",dome,wall,-11);
                        var iron=new Builder();FormedPitHood(iron,centre);
                        Create("Pit boiler retaining iron",iron,set.FoundryCastIron,-10.5f);
                    });
            }
        }

        private static void PitDomeIron(Builder b,Vector2 c)
        {
            // Broad retaining bands identify the hood as furnace equipment. They occupy
            // the existing stone envelope and inherit its 1.3-high owner, below the open flue.
            int ring=b.C.Count;
            MassCourse(b,c,1.1f,1.1f,.035f,.16f,8,true,new Color(1.08f,.96f,.8f));
            for(int i=ring;i<b.C.Count;i++)
                if((i-ring)%4<2)
                    b.C[i]=Tinted(b.C[i],Color.Lerp(Color.white,new Color(1.18f,1.02f,.87f),
                        PitFacingPool(c,FrontAngle((i-ring)/4,8)+Mathf.PI/16)));
            MassCourse(b,c,.24f,.24f,.66f,.79f,6,true,new Color(1.12f,1.02f,.88f));
            // Three structural straps follow the actual flat facets of the two hood courses.
            // Interpolating facet endpoints keeps them coplanar rather than floating on a circle.
            for(int facet=1;facet<=5;facet+=2)
            {
                float a=FrontAngle(facet,8),e=FrontAngle(facet+1,8);
                for(int course=0;course<2;course++)
                {
                    float r0=course==0?.98f:.76f,r1=course==0?.76f:.44f;
                    float h0=course==0?.18f:.44f,h1=course==0?.44f:.64f;
                    Vector3 bottomA=OnRing(c,r0,a,h0),bottomB=OnRing(c,r0,e,h0);
                    Vector3 topA=OnRing(c,r1,a,h1),topB=OnRing(c,r1,e,h1);
                    Color tone=new Color(.96f,.88f,.77f);
                    LinkQuad(b,Vector3.Lerp(bottomA,bottomB,.4f),Vector3.Lerp(bottomA,bottomB,.6f),
                        Vector3.Lerp(topA,topB,.6f),Vector3.Lerp(topA,topB,.4f),tone,tone,tone,tone);
                }
            }
        }

        private static void PitDome(Builder b,Vector2 c)
        {
            // Collar ring over the painted body, a stepped masonry dome and a short open flue, a shade under the court's stone
            // so the boiler stands dark against the lit lining; the pool's glow warms the ring's foot (MassCourse's lower edge,
            // the first two vertices of each facet), most on the side turned to it.
            const float shade=.72f;
            int ring=b.C.Count;
            MassCourse(b,c,1.1f,1.1f,0,.18f,8,false,new Color(.95f*shade,.95f*shade,.95f*shade));
            for(int i=ring;i<b.C.Count;i++)
                if((i-ring)%4<2)b.C[i]=Tinted(b.C[i],Color.Lerp(PitDomeFoot,PitDomeGlow,PitFacingPool(c,FrontAngle((i-ring)/4,8)+Mathf.PI/16)));
            MassDisk(b,c,1.1f,.18f,8,false,new Color(.78f*shade,.8f*shade,.9f*shade));
            MassCourse(b,c,.98f,.76f,.18f,.44f,8,false,new Color(1.08f*shade,1.08f*shade,1.08f*shade));
            MassCourse(b,c,.76f,.44f,.44f,.64f,8,false,new Color(1.14f*shade,1.14f*shade,1.14f*shade));
            MassDisk(b,c,.44f,.64f,6,false,new Color(.9f*shade,.92f*shade,shade));
            MassCourse(b,c,.24f,.24f,.64f,1.2f,6,false,new Color(1.05f*shade,1.05f*shade,1.05f*shade));
            MassCourse(b,c,.3f,.3f,1.12f,1.26f,6,false,new Color(1.2f*shade,1.2f*shade,1.2f*shade));
            MassDisk(b,c,.3f,1.26f,6,false,new Color(.85f*shade,.87f*shade,.95f*shade));
            MassDisk(b,c,.19f,1.26f,6,false,new Color(.1f,.09f,.1f));
        }
        private static readonly Color PitDomeFoot=new Color(1.25f,1f,.86f),PitDomeGlow=new Color(1.9f,1.18f,.7f);

        // The pool of molten metal, PitMolten down, where the camera can see it: in the gap between the two inner boilers,
        // from the lining's foot toward the near collar, which hides the rest of the pit's floor (from the camera the near
        // rail conceals the near half of the deeper floor). It is a contained orange source below long dark machinery,
        // rather than a broad near-white surface at the rim. Its far edge runs .05 in under the lining's foot and
        // its sides .05 in behind the boilers' silhouettes, so no seam opens: both are in the lining's batch, which draws
        // after this one (on the shared rank the undercroft discharge keeps this batch's bounds centre higher) and paints
        // over the overlap; were the two ever to swap, only those .05 bands would change. It draws over the layout
        // sidewalls (-16) behind the lining, which reach deeper than the pool.
        private static void PitPool(Builder b)
        {
            float left=PitBoilers[0].x-PitBoilers[0].y+Mathf.Sqrt(2)-.05f,right=PitBoilers[1].x-PitBoilers[1].y-Mathf.Sqrt(2)+.05f;
            // The far edge: the lining's foot (its facet stations), cut to the gap.
            var far=new List<Vector2>();var normals=new List<Vector2>();
            for(int i=0;i<PitFacets;i++)
            {
                Vector2 a=PitLiningStation(i,out Vector2 na),c=PitLiningStation(i+1,out Vector2 nc);
                float da=a.x-a.y,dc=c.x-c.y;
                if(dc<=left||da>=right)continue;
                float s0=Mathf.InverseLerp(da,dc,Mathf.Max(da,left)),s1=Mathf.InverseLerp(da,dc,Mathf.Min(dc,right));
                if(far.Count==0){far.Add(Vector2.Lerp(a,c,s0));normals.Add(Vector2.Lerp(na,nc,s0).normalized);}
                far.Add(Vector2.Lerp(a,c,s1));normals.Add(Vector2.Lerp(na,nc,s1).normalized);
            }
            Vector3 down=Vector3.down*PitMolten;
            for(int j=0;j+1<far.Count;j++)
            {
                Vector2 f0=far[j]+normals[j]*.05f,f1=far[j+1]+normals[j+1]*.05f,n0=PitNearEdge(far[j]),n1=PitNearEdge(far[j+1]);
                int start=b.V.Count;
                b.Quad(Project(n0.x,n0.y)+down,Project(n1.x,n1.y)+down,Project(f1.x,f1.y)+down,Project(f0.x,f0.y)+down,true,PitPoolNear);
                b.C[start+2]=PitPoolFar;b.C[start+3]=PitPoolFar;
                var corners=new[]{n0,n1,f1,f0};
                for(int k=0;k<4;k++)b.U[start+k]=PitPoolTexel(corners[k]);
            }
        }
        // The pool's tone, brightest at the lining's foot; and where the ember texture's molten channels meet (u .47-.53, v
        // .23-.29) mapped over the enlarged opening, so unrelated crust plates never enter the pool.
        private static readonly Color PitPoolFar=new Color(1.4f,1.15f,.85f),PitPoolNear=new Color(1.05f,.70f,.42f);
        // Keep the enlarged basin within the molten-channel patch of the existing atlas.
        // Absolute old-layout UVs exposed unrelated pale crust plates after relocation.
        private static Vector2 PitPoolTexel(Vector2 p)=>new Vector2(.497f+(p.x-PitCentre.x)*.005f,.262f+(p.y-PitCentre.y)*.005f);

        // The band's inner edge on the near half at the far point's screen column (equal u-v), by bisection.
        private static Vector2 PitNearEdge(Vector2 far)
        {
            float target=far.x-far.y,lo=1.5f*PitQuarter,hi=3.5f*PitQuarter;
            for(int i=0;i<24;i++)
            {
                float t=(lo+hi)*.5f;
                Vector2 p=PitRing(t,RailDepth,out _);
                if(p.x-p.y<target)lo=t;else hi=t;
            }
            return PitRing((lo+hi)*.5f,RailDepth,out _);
        }

        // The lining's facet stations along the band's inner edge, left (the outline's NW silhouette diagonal) to right.
        private const int PitFacets=21;
        private static Vector2 PitLiningStation(int i,out Vector2 normal)=>PitRing(PitQuarter*(1.5f-2f*i/PitFacets),RailDepth,out normal);

        // Seven ribbed lining panels, three facets each, descend from the far collar into the molten pool.
        // The new texture has baked bottom heat and coals in its lowest 12%; its v=.06 meets the waterline.
        // A broad contact shadow under the collar releases into the painted stone. Shading
        // is evaluated at shared stations so the rounded wall does not read as flat stripes.
        private static readonly float[] PitLiningRows={0,.18f,.55f,1.05f,1.75f,2.65f,4.1f,PitMolten-PitSeam};
        private const float PitSeam=.07f,PitFrontScale=PitMolten/.94f;
        private static void PitLiningFronts(Builder walls)
        {
            const int facets=3;
            for(int i=0;i<PitFacets;i++)
            {
                Vector2 pa=PitLiningStation(i,out Vector2 na),pb=PitLiningStation(i+1,out Vector2 nb);
                Vector3 a=Project(pa.x,pa.y),b=Project(pb.x,pb.y);
                float u0=i%facets/(float)facets,u1=(i%facets+1)/(float)facets;
                // The first two shallow strips are owned by the joined bearing
                // course below. Do not leave another wall across its bevels.
                for(int row=2;row+1<PitLiningRows.Length;row++)
                {
                    float top=PitLiningRows[row],bottom=PitLiningRows[row+1];
                    int first=walls.C.Count;
                    PitFront(walls,a,b,top,bottom,new Vector4(u0,1-bottom/PitFrontScale,u1,1-top/PitFrontScale),
                        PitWallTone(top,na),PitWallTone(bottom,na));
                    walls.C[first+1]=PitWallTone(bottom,nb);
                    walls.C[first+2]=PitWallTone(top,nb);
                }
                PitFront(walls,a,b,PitMolten-PitSeam,PitMolten,PitFire,PitSeamTone,PitSeamTone);
                for(int row=0;row+1<PitWashRows.Length;row++)
                {
                    float top=PitWashRows[row],bottom=PitWashRows[row+1];
                    int first=walls.C.Count;
                    PitFront(walls,a,b,top,bottom,PitFire,PitWallWash(top,na),PitWallWash(bottom,na));
                    walls.C[first+1]=PitWallWash(bottom,nb);
                    walls.C[first+2]=PitWallWash(top,nb);
                }
            }
            PitLiningBearingCourse(walls);
            PitLiningBearings(walls);
        }

        private static void PitLiningBearingCourse(Builder b)
        {
            // The rail bears on one deep stone course, with a chamfered nose
            // wholly below the original deck edge. Shared stations join every
            // return around the existing curve; no coping grows onto the court.
            var profile=new[]{new Vector2(0,0),new Vector2(.12f,.22f),
                new Vector2(.38f,.22f),new Vector2(.55f,0)};
            for(int i=0;i<PitFacets;i++)
            {
                Vector2 a=PitLiningStation(i,out Vector2 na),c=PitLiningStation(i+1,out Vector2 nc);
                for(int row=0;row+1<profile.Length;row++)
                {
                    Vector2 upper=profile[row],lower=profile[row+1];
                    PitBearingStoneFace(b,PitLiningPoint(a,na,lower),PitLiningPoint(c,nc,lower),
                        PitLiningPoint(c,nc,upper),PitLiningPoint(a,na,upper),
                        upper.x,lower.x,(na+nc).normalized,Vector2.Distance(a,c),
                        row==0?1.03f:row==1?.92f:.65f);
                }
            }
        }

        private static void PitLiningBearings(Builder b)
        {
            // Four long bearing ribs follow existing painted strap boundaries.
            // The other seams stay flat where the furnace apertures already own
            // the bay. A deep shoulder meets the coping, then narrows into the
            // continuous rib and a low foot above the unchanged molten seam.
            var profile=new[]{new Vector2(.55f,0),new Vector2(.88f,.28f),
                new Vector2(1.22f,.15f),new Vector2(4.54f,.15f),new Vector2(4.90f,0)};
            foreach(int station in new[]{3,9,12,18})
            {
                Vector2 centre=PitLiningStation(station,out Vector2 normal);
                Vector2 prev=PitLiningStation(station-1,out Vector2 prevNormal);
                Vector2 next=PitLiningStation(station+1,out Vector2 nextNormal);
                float left=.33f/Vector2.Distance(centre,prev),right=.33f/Vector2.Distance(centre,next);
                var points=new[]{Vector2.Lerp(centre,prev,left),centre,Vector2.Lerp(centre,next,right)};
                var normals=new[]{Vector2.Lerp(normal,prevNormal,left).normalized,normal,
                    Vector2.Lerp(normal,nextNormal,right).normalized};
                for(int row=0;row+1<profile.Length;row++)
                {
                    Vector2 upper=profile[row],lower=profile[row+1];
                    for(int half=0;half<2;half++)
                        PitCastLiningFace(b,PitLiningPoint(points[half],normals[half],lower),
                            PitLiningPoint(points[half+1],normals[half+1],lower),
                            PitLiningPoint(points[half+1],normals[half+1],upper),
                            PitLiningPoint(points[half],normals[half],upper),
                            upper.x,lower.x,normal,row==0?1.03f:row==1?.81f:1f,half);
                    // Side returns end on the actual wall chord instead of
                    // floating over the texture or ending in a screen-space strip.
                    foreach(int edge in new[]{0,2})
                    {
                        Vector3 wallLow=Project(points[edge].x,points[edge].y)+Vector3.down*lower.x;
                        Vector3 wallHigh=Project(points[edge].x,points[edge].y)+Vector3.down*upper.x;
                        Vector3 faceLow=PitLiningPoint(points[edge],normals[edge],lower);
                        Vector3 faceHigh=PitLiningPoint(points[edge],normals[edge],upper);
                        PitCastLiningFace(b,wallLow,faceLow,faceHigh,wallHigh,
                            upper.x,lower.x,normal,edge==0?.56f:.73f,-1);
                    }
                }
            }
        }

        private static Vector3 PitLiningPoint(Vector2 point,Vector2 normal,Vector2 profile)
        {
            Vector2 inside=point-normal*profile.y;
            return Project(inside.x,inside.y)+Vector3.down*profile.x;
        }

        private static void PitLiningReliefFace(Builder b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,
            Vector4 sample,float top,float bottom,Vector2 normal,float gain,bool iron)
        {
            int first=b.V.Count;
            FullFace(b,a,c,d,e,Color.white);
            b.U[first]=new Vector2(sample.x,sample.y);b.U[first+1]=new Vector2(sample.z,sample.y);
            b.U[first+2]=new Vector2(sample.z,sample.w);b.U[first+3]=new Vector2(sample.x,sample.w);
            for(int i=0;i<4;i++)
            {
                float depth=i<2?bottom:top;
                Color tone=iron?PitHeat(depth,new Color(.84f,.88f,.97f),new Color(1.18f,.86f,.57f),.85f):
                    Tinted(PitWallTone(depth,normal),new Color(.93f,.98f,1.04f));
                tone*=gain;tone.a=1;b.C[first+i]=tone;
            }
        }
        // A translucent sample of the painted coals joins the lining to the separate molten pool.
        private static readonly float[] PitWashRows={1.8f,2.65f,4.1f,PitMolten-PitSeam};
        private static Color PitWash(float depth)
        {
            float glow=Mathf.Pow(Mathf.InverseLerp(1.8f,PitMolten-PitSeam,depth),1.6f);
            return new Color(1,.92f,.8f,.16f*glow);
        }
        // The far wall receives the open pool's light; the two turning ends stay darker.
        // This only attenuates the previous lighting envelope (at most 1.08), while the
        // baked lining remains responsible for the individual stone and metal texture.
        private static float PitWallFacing(Vector2 normal)=>
            Mathf.Clamp01((normal.x+normal.y)*.70710678f);

        private static Color PitWallTone(float depth,Vector2 normal)
        {
            float facing=PitWallFacing(normal);
            float form=Mathf.Lerp(.82f,1.08f,facing*facing);
            float contact=Mathf.Lerp(.48f,1f,Mathf.SmoothStep(0,1,depth/.55f));
            Color heat=PitHeat(depth,PitWallCool,PitWallHot,.85f);
            return Tinted(heat,new Color(form*contact,form*contact,form*contact));
        }

        private static Color PitWallWash(float depth,Vector2 normal)
        {
            Color wash=PitWash(depth);
            // Preserve side-wall soot instead of covering every bay in equal orange haze.
            wash.a*=Mathf.Lerp(.45f,1f,PitWallFacing(normal));
            return wash;
        }
        // Light from the pool at a depth: from cool to hot as the height above the metal falls through reach.
        private static Color PitHeat(float depth,Color cool,Color hot,float reach)=>
            Color.LerpUnclamped(cool,hot,Mathf.Exp(-(PitMolten-depth)/reach));
        private static readonly Color PitWallCool=new Color(.80f,.82f,.88f),PitWallHot=new Color(1.05f,.98f,.90f);
        private static readonly Color PitDrumCool=new Color(.95f,1f,1.08f),PitDrumHot=new Color(1.3f,1.08f,.85f);
        // An orange gap between coals in the dedicated lining, avoiding the old facade's grate UV.
        private static readonly Vector4 PitFire=new Vector4(.415f,.07f,.425f,.08f);
        private static readonly Color PitSeamTone=new Color(1f,1.1f,.95f);

        // Three deep flues break the broad stone lining into industrial bays. Each is
        // attached to one actual lining chord and projects only .12 inward, entirely
        // below deck. Emit before the vessels so their near silhouettes remain intact.
        private static void PitFurnaceBays(Builder b)
        {
            foreach(int facet in new[]{5,10,15})
            {
                Vector2 p=PitLiningStation(facet,out Vector2 np);
                Vector2 q=PitLiningStation(facet+1,out Vector2 nq);
                Vector2 normal=(np+nq).normalized;
                Vector3 a=Project(Mathf.Lerp(p.x,q.x,.12f),Mathf.Lerp(p.y,q.y,.12f));
                Vector3 c=Project(Mathf.Lerp(p.x,q.x,.88f),Mathf.Lerp(p.y,q.y,.88f));
                Vector3 inward=Project(-normal.x*.12f,-normal.y*.12f)-Project(0,0);
                const float top=.64f,bottom=4.94f;
                var soot=new Color(.56f,.59f,.66f);
                // Broad dark channel, with a cool upper throat and a contained lower
                // fire aperture. The painted coal sample never reaches the deck edge.
                PitFront(b,a,c,top,bottom,new Vector4(.31f,.18f,.40f,.86f),soot,
                    new Color(.57f,.43f,.34f));
                Vector3 left=Vector3.Lerp(a,c,.37f),right=Vector3.Lerp(a,c,.63f);
                PitFront(b,left,right,3.4f,4.82f,PitFire,
                    new Color(.21f,.12f,.065f),new Color(.78f,.46f,.24f));
                // Recess returns and continuous iron jambs give the opening actual
                // thickness rather than a bright stripe painted over the masonry.
                var metal=new Vector4(.008f,.17f,.066f,.90f);
                Color upper=new Color(.67f,.71f,.81f),lower=new Color(.92f,.74f,.59f);
                PitFront(b,a,a+inward,top,bottom,metal,new Color(.31f,.34f,.40f),new Color(.6f,.4f,.28f));
                PitFront(b,c+inward,c,top,bottom,metal,new Color(.44f,.46f,.53f),new Color(.78f,.52f,.34f));
                PitFront(b,a+inward,Vector3.Lerp(a,c,.16f)+inward,top,bottom,metal,upper,lower);
                PitFront(b,Vector3.Lerp(a,c,.84f)+inward,c+inward,top,bottom,metal,upper,lower);
                // Heavy lintel and two cross ties visually bind the long pair of ribs.
                foreach(float depth in new[]{top,1.55f,3.75f})
                    PitFront(b,a+inward,c+inward,depth,depth+.15f,
                        new Vector4(.0f,.69f,.08f,.745f),new Color(.52f,.55f,.63f),new Color(.47f,.49f,.56f));
            }
        }

        // A course of a painted front from depth top down to bottom under the deck-level edge a-c, mapped onto the texture
        // region (u0, v0, u1, v1) (v0 at the bottom), its top and bottom edges each in their own colour.
        private static void PitFront(Builder b,Vector3 a,Vector3 c,float top,float bottom,Vector4 region,Color upper,Color lower)
        {
            int start=b.U.Count;
            b.Quad(a+Vector3.down*bottom,c+Vector3.down*bottom,c+Vector3.down*top,a+Vector3.down*top,false,lower);
            b.U[start]=new Vector2(region.x,region.y);b.U[start+1]=new Vector2(region.z,region.y);
            b.U[start+2]=new Vector2(region.z,region.w);b.U[start+3]=new Vector2(region.x,region.w);
            b.C[start+2]=upper;b.C[start+3]=upper;
        }

        // The atlas's continuous dark pipe strip makes these long vessels read as machinery,
        // distinct from the large stone lining panels. Their original radius/hoods are retained.
        private static readonly float[] PitDrumRows={0,.85f,1.75f,2.65f,4.1f,PitMolten-PitSeam};
        private static void PitDrum(Builder b,Vector2 c)
        {
            const int m=8;
            for(int k=0;k<m;k++)
            {
                float a=FrontAngle(k,m),e=FrontAngle(k+1,m);
                float u0=Mathf.Lerp(.229f,.282f,k/(float)m),u1=Mathf.Lerp(.229f,.282f,(k+1)/(float)m);
                Color light=PaintLight((a+e)*.5f);
                float rim=PitFacingPool(c,(a+e)*.5f);
                Vector3 p=OnRing(c,1,a,0),q=OnRing(c,1,e,0);
                for(int row=0;row+1<PitDrumRows.Length;row++)
                {
                    float top=PitDrumRows[row],bottom=PitDrumRows[row+1];
                    PitFront(b,p,q,top,bottom,new Vector4(u0,1-bottom/PitFrontScale,u1,1-top/PitFrontScale),
                        Tinted(PitDrumHeat(top,rim),light),Tinted(PitDrumHeat(bottom,rim),light));
                }
                PitFront(b,p,q,PitMolten-PitSeam,PitMolten,PitFire,PitSeamTone,PitSeamTone);
            }
            PitVesselStructure(b,c);
        }

        private static void PitVesselStructure(Builder b,Vector2 c)
        {
            // The pressure chest has broad service plates; its receiving vessel has
            // a longer cage. Jackets, belts and top hold-downs share one centre and
            // stay under the existing radius1.1 head instead of widening the shaft.
            bool pressure=c.x<PitCentre.x;
            foreach(int facet in pressure?new[]{1,3,5,7}:new[]{0,2,4,6})
            {
                float angle=FrontAngle(facet,8)+Mathf.PI/16;
                Vector3 a=OnRing(c,1.065f,angle-.065f,0),d=OnRing(c,1.065f,angle+.065f,0);
                float rim=PitFacingPool(c,angle);
                PitFront(b,a,d,0,PitMolten-.16f,new Vector4(.24f,.17f,.278f,.90f),
                    Tinted(PitDrumCool,new Color(1.06f,1.06f,1.06f)),PitDrumHeat(PitMolten-.16f,rim));
            }
            // Wide closed inspection bays follow the cylinder facets. Their backing,
            // shallow iron returns and face are connected; no floating screen decals.
            foreach(int facet in pressure?new[]{2,5}:new[]{3})
                PitVesselServiceBay(b,c,facet,pressure?.52f:.38f,pressure?1.94f:2.54f);
            // Retaining belts wrap the vessel's actual curved section; no floating screen bars.
            foreach(float depth in pressure?new[]{.22f,2.12f,4.45f}:new[]{.22f,2.72f,4.45f})
                for(int k=0;k<8;k++)
                {
                    float a=FrontAngle(k,8),e=FrontAngle(k+1,8),angle=(a+e)*.5f;
                    Vector3 p=OnRing(c,1.075f,a,0),q=OnRing(c,1.075f,e,0);
                    Color tone=Tinted(PaintLight(angle),new Color(1.18f,1.09f,.93f));
                    PitFront(b,p,q,depth,depth+.18f,new Vector4(.23f,.56f,.28f,.60f),tone,tone);
                }
        }

        private static void PitVesselServiceBay(Builder b,Vector2 c,int facet,float top,float bottom)
        {
            float a=FrontAngle(facet,8),e=FrontAngle(facet+2,8),middle=(a+e)*.5f;
            // A two-facet folded plate retains the cylindrical silhouette and meets
            // its vertical rails at the actual emitted facet edges.
            var sample=new Vector4(.23f,.17f,.28f,.90f);
            Color light=PaintLight(middle),dark=Tinted(light,new Color(.55f,.57f,.62f));
            for(int k=facet;k<facet+2;k++)
            {
                Vector3 p=OnRing(c,1.017f,FrontAngle(k,8),0),q=OnRing(c,1.017f,FrontAngle(k+1,8),0);
                PitFront(b,p,q,top,bottom,sample,dark,Tinted(dark,new Color(1.10f,.96f,.83f)));
                Vector3 outerP=OnRing(c,1.066f,FrontAngle(k,8),0),outerQ=OnRing(c,1.066f,FrontAngle(k+1,8),0);
                foreach(float depth in new[]{top,bottom-.10f})
                    PitFront(b,outerP,outerQ,depth,depth+.10f,new Vector4(.23f,.56f,.28f,.60f),light,light);
            }
            foreach(float angle in new[]{a,e})
            {
                Vector3 inner=OnRing(c,1.017f,angle,0),outer=OnRing(c,1.066f,angle,0);
                PitFront(b,inner,outer,top,bottom,sample,dark,dark);
                Vector3 outerEnd=OnRing(c,1.066f,angle+(angle==a?.055f:-.055f),0);
                PitFront(b,outer,outerEnd,top,bottom,sample,light,Tinted(light,new Color(1.12f,.98f,.87f)));
            }
            // Short central latch is seated on the jacket rather than hovering in
            // front of a broad opening. It makes the two serviceable bodies legible.
            PitFront(b,OnRing(c,1.034f,middle-.12f,0),OnRing(c,1.034f,middle+.12f,0),
                (top+bottom)*.5f-.08f,(top+bottom)*.5f+.08f,
                new Vector4(.23f,.56f,.28f,.60f),light,light);
        }
        // The drum's light: the pool's from below, and on the side turned to the gap between the boilers - which is all the
        // camera sees of them below the deck - a rim of it that reaches up the drum.
        private static Color PitDrumHeat(float depth,float rim)=>
            Color.LerpUnclamped(PitHeat(depth,PitDrumCool,PitDrumHot,.55f),PitDrumHot,rim*Mathf.Lerp(.08f,.65f,depth/PitMolten));
        // How squarely a boiler's face at angle turns to the pit's centre, where the pool shows: 0 turned away, 1 square on.
        private static float PitFacingPool(Vector2 c,float angle)
        {
            Vector2 toward=PitCentre-c;
            float facing=Mathf.Max(0,Mathf.Cos(angle-Mathf.Atan2(toward.y,toward.x)));
            return facing*facing;
        }

        // A point of the collar's band in the design plane. t runs counterclockwise from the middle of the pit's +u side,
        // along the band's outer edge; inset runs inward along the outline's normal (0 the outer edge, RailDepth the inner).
        // Each quarter of the outline is a side's straight half, a corner arc of PitRound and the next side's straight half.
        private static Vector2 PitRing(float t,float inset,out Vector2 normal)
        {
            int turn=Mathf.FloorToInt(t/PitQuarter);
            float x=t-turn*PitQuarter,side=(turn%4+4)%4*Mathf.PI*.5f,arc=PitRound*Mathf.PI*.5f;
            Vector2 point;
            if(x<PitStraight){normal=PitHeading(side);point=PitCentre+normal*PitRim+new Vector2(-normal.y,normal.x)*x;}
            else if(x<PitStraight+arc)
            {
                normal=PitHeading(side+(x-PitStraight)/PitRound);
                point=PitCentre+(PitHeading(side)+PitHeading(side+Mathf.PI*.5f))*PitStraight+normal*PitRound;
            }
            else{normal=PitHeading(side+Mathf.PI*.5f);point=PitCentre+normal*PitRim+new Vector2(normal.y,-normal.x)*(PitQuarter-x);}
            return point-normal*inset;
        }

        private static Vector2 PitHeading(float angle)=>new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));

        // The collar's eight posts in outline order from the +u side's middle (post k at t = k half-quarters): mid posts on
        // the straights, from the hole's edge to just past the band's inner edge, and .9 lantern pedestals on the corner
        // arcs, PitLantern in from the hole's corners, where a pedestal's inner corner just passes the band's inner edge.
        // So no post overhangs the pit by more than .01, and the band crosses one face of each post at either end of a run:
        // a hidden face where the run passes behind the post, a camera-facing one where it stands in front of it - and the
        // run's footprint then sorts after the post's.
        private const float PitLantern=.97f;
        private static Vector4[] PitPosts()
        {
            var posts=new Vector4[8];
            const float reach=PitCollar+.01f;
            for(int k=0;k<8;k++)
            {
                float angle=k*Mathf.PI*.25f;
                Vector2 compass=new Vector2(Mathf.Round(Mathf.Cos(angle)*1.4f),Mathf.Round(Mathf.Sin(angle)*1.4f));
                Vector2 c=PitCentre+compass*(k%2==1?PitHalf-PitLantern:PitHalf-reach*.5f);
                Vector2 half=k%2==1?new Vector2(.45f,.45f):new Vector2(compass.x!=0?reach*.5f:.3f,compass.y!=0?reach*.5f:.3f);
                posts[k]=new Vector4(c.x-half.x,c.y-half.y,c.x+half.x,c.y+half.y);
            }
            return posts;
        }

        // A run of the collar's balustrade from post k to post k+1, as three lines along the band: the camera-facing front
        // (the band's outer edge on the near half, from the left lantern round the front to the right one; its inner edge on
        // the far half), the baluster axis and the back - each from where it leaves one post's footprint to where it enters
        // the next's, in PitChords even steps. The handrail spans the band without RailSpan's .02 overhang at the back.
        private const int PitChords=8;
        private static Vector2[][] PitRunLines(Vector4[] posts,int k,out bool near)
        {
            near=k>=3&&k<=6;
            float t0=k*PitQuarter*.5f,t1=t0+PitQuarter*.5f,mid=(t0+t1)*.5f,front=near?0:RailDepth,away=near?1:-1;
            var offsets=new[]{0f,BalusterAxis,RailDepth};
            var lines=new Vector2[3][];
            for(int i=0;i<3;i++)
            {
                float inset=front+away*offsets[i],a=PitClear(posts[k],t0,mid,inset),b=PitClear(posts[(k+1)%8],t1,mid,inset);
                lines[i]=new Vector2[PitChords+1];
                for(int j=0;j<=PitChords;j++)lines[i][j]=PitRing(Mathf.Lerp(a,b,j/(float)PitChords),inset,out _);
            }
            return lines;
        }

        // Bisection from a t inside a post's footprint to one outside it: where the band's line at inset crosses its edge.
        private static float PitClear(Vector4 box,float inside,float outside,float inset)
        {
            for(int i=0;i<24;i++)
            {
                float t=(inside+outside)*.5f;
                Vector2 p=PitRing(t,inset,out _);
                if(p.x>box.x&&p.x<box.z&&p.y>box.y&&p.y<box.w)inside=t;else outside=t;
            }
            return outside;
        }

        // RailSpan's sequence along the curve, chord by chord from the run's far end - where the outline turns toward the
        // screen, the next chord stacks over this one: on the far half the plinth's face, then the plinth course's lit ledge
        // and shadowed gap floor, the balusters far to near, and the handrail's face, arris and top. Faces take their tone
        // from their facing (PitFacing). The near half's plinth face is left out: from the camera it lies behind the collar
        // stone's top and plinth course, emitted before it, and would paint over them.
        private static void PitRailRun(Builder b,Vector2[][] lines,bool near)
        {
            Vector2[] front=lines[0],axis=lines[1],back=lines[2];
            // Balusters with equal clear gaps along the axis, as between two posts on a straight span.
            var along=new float[axis.Length];
            for(int j=1;j<axis.Length;j++)along[j]=along[j-1]+Vector2.Distance(axis[j-1],axis[j]);
            int chords=axis.Length-1;
            float length=along[chords],belly=BalusterProfile[2].x*2;
            int count=Mathf.Max(0,Mathf.RoundToInt((length-BalusterGap)/(belly+BalusterGap)));
            while(count>0&&(length-count*belly)/(count+1)<.12f)count--;
            float gap=(length-count*belly)/(count+1);
            Vector2 arris=new Vector2(HandrailCourse.y-.012f,HandrailCourse.y);
            bool fromEnd=front[0].x+front[0].y<front[chords].x+front[chords].y;
            for(int step=0;step<chords;step++)
            {
                int j=fromEnd?chords-1-step:step;
                Vector2 outward=front[j]+front[j+1]-back[j]-back[j+1];
                Color face=PitFacing(Mathf.Atan2(outward.y,outward.x));
                Vector3 p=Project(front[j].x,front[j].y),q=Project(front[j+1].x,front[j+1].y);
                if(p.x>q.x){var swap=p;p=q;q=swap;}
                if(!near)CourseFace(b,p,q,0,RailPlinth,Tinted(face,new Color(1.02f,1.02f,1.02f)),PlinthCourse);
                PitChordTop(b,front[j],front[j+1],axis[j+1],axis[j],RailPlinth,Tinted(CapTone,new Color(.95f,.95f,.95f)),PlinthCourse);
                PitChordTop(b,axis[j],axis[j+1],back[j+1],back[j],RailPlinth,GapShade,PlinthCourse);
                for(int n=0;n<count;n++)
                {
                    int i=fromEnd?count-1-n:n;
                    float s=gap+belly*.5f+i*(belly+gap);
                    if(s<along[j]||s>=along[j+1])continue;
                    Vector2 at=Vector2.Lerp(axis[j],axis[j+1],(s-along[j])/(along[j+1]-along[j]));
                    Baluster(b,Project(at.x,at.y),i);
                }
                CourseFace(b,p,q,RailHand,RailTop-.03f,Tinted(face,new Color(1.2f,1.2f,1.2f)),new Vector2(HandrailCourse.x,arris.x));
                CourseFace(b,p,q,RailTop-.03f,RailTop,Tinted(face,new Color(1.55f,1.55f,1.55f)),arris);
                PitChordTop(b,front[j],front[j+1],back[j+1],back[j],RailTop,Tinted(CapTone,new Color(1.1f,1.1f,1.1f)),HandrailCourse);
            }
        }

        // CourseTop for a chord: the quad from its front edge (a, c) back, texture u following the front edge's screen x.
        private static void PitChordTop(Builder b,Vector2 a,Vector2 c,Vector2 cBack,Vector2 aBack,float h,Color color,Vector2 band)
        {
            Vector3 lift=Vector3.up*h,pa=Project(a.x,a.y),pc=Project(c.x,c.y);
            int start=b.U.Count;
            b.Quad(pa+lift,pc+lift,Project(cBack.x,cBack.y)+lift,Project(aBack.x,aBack.y)+lift,true,color);
            b.U[start]=new Vector2(pa.x/2.8f,band.y);b.U[start+1]=new Vector2(pc.x/2.8f,band.y);
            b.U[start+2]=new Vector2(pc.x/2.8f,band.x);b.U[start+3]=new Vector2(pa.x/2.8f,band.x);
        }

        // The tone of a camera-facing course by the design-plane angle it faces: ShadeFace facing -u and LitFace facing -v,
        // as the straight courses take them, and between and beyond them on StoneLight's lobe, so a curved run shades round.
        private static Color PitFacing(float angle)
        {
            float shade=Mathf.Cos(-105*Mathf.Deg2Rad),lit=Mathf.Cos(-15*Mathf.Deg2Rad);
            return Color.LerpUnclamped(ShadeFace,LitFace,(Mathf.Cos(angle-285*Mathf.Deg2Rad)-shade)/(lit-shade));
        }

        // Collar stone filling the hole outside the band, t0..t1 of the outline: coping stones laid radially - each straight
        // half one stone, the corner arcs in 15-degree stones, jointed along the outline's normals - in a profile that keeps
        // the whole collar round. A coping lip PitLip wide follows the band at the plinth's height (a dark joint bed under an
        // inset face, jointed at its outer edge too), a rounded chamfer falls PitChamfer outward to a lower step PitStep high,
        // and the step fills the rest of the corner out to the hole's edge in a dark stone that darkens toward the corner's
        // tip, so the square reads as a recess round the curve. The straights are lip throughout, so the mid posts and the
        // straights' courses stand as before. Where the hole's edge faces the camera, a course drops to the deck at the
        // profile's height there. Painted far to near: steps, chamfers, lips (on the far side the lip hides the step behind
        // it; on the near side the three never overlap), then the courses. Returns the footprint.
        private const float PitJoint=.018f,PitLip=.32f,PitChamfer=.1f,PitStep=.05f;
        private static readonly float[] PitStoneTone={.84f,.78f,.88f,.81f,.75f,.86f,.8f};
        private static Vector4 PitCollarStone(Builder b,float t0,float t1)
        {
            var stations=new List<float>{t0,t1};
            for(int q=0;q<4;q++)
                for(int m=0;m<=6;m++)
                {
                    float t=q*PitQuarter+PitStraight+PitRound*Mathf.PI*m/12;
                    if(t>t0+.01f&&t<t1-.01f)stations.Add(t);
                }
            stations.Sort();
            var corners=new List<Vector2>();
            Vector3 lift=Vector3.up*RailPlinth;
            // rim: the chamfer's foot; tip: from there to the corner's tip, where the step is darkest.
            const float rim=PitLip+PitChamfer,tip=(PitHalf-PitStraight)*1.41421356f-PitRound-rim;
            Color joint=Tinted(CapTone,new Color(.42f,.42f,.42f)),arris=Tinted(CapTone,new Color(1.08f,1.08f,1.08f));
            Color step=Tinted(CapTone,new Color(.5f,.5f,.5f)),corner=Tinted(CapTone,new Color(.34f,.34f,.34f));
            for(int pass=0;pass<4;pass++)
                for(int j=0;j+1<stations.Count;j++)
                {
                    Vector2 o0=PitRing(stations[j],0,out Vector2 n0),o1=PitRing(stations[j+1],0,out Vector2 n1);
                    Vector2 s0=PitEdge(o0,n0),s1=PitEdge(o1,n1);
                    float r0=Vector2.Distance(o0,s0),r1=Vector2.Distance(o1,s1);
                    int stone=Mathf.RoundToInt(stations[j]*7);
                    float u=.33f+.04f*(stone%4),tone=PitStoneTone[stone%PitStoneTone.Length];
                    // Each radial edge's breaks - the lip's edge (p), the chamfer's foot (q) - cut short where the hole's edge
                    // comes first.
                    float f0=Mathf.Min(rim,r0),f1=Mathf.Min(rim,r1);
                    Vector2 p0=o0+n0*Mathf.Min(PitLip,r0),p1=o1+n1*Mathf.Min(PitLip,r1),q0=o0+n0*f0,q1=o1+n1*f1;
                    if(pass==0)
                    {
                        corners.AddRange(new[]{o0,o1,s0,s1});
                        if(r0>rim||r1>rim)
                            PitSlope(b,q0,q1,s1,s0,new[]{PitProfile(f0),PitProfile(f1),PitProfile(r1),PitProfile(r0)},u,
                                new[]{step,step,Color.Lerp(step,corner,(r1-rim)/tip),Color.Lerp(step,corner,(r0-rim)/tip)});
                    }
                    else if(pass==1&&(r0>PitLip||r1>PitLip))
                    {
                        // Lit along the lip's arris, shaded by its facing where it meets the step.
                        Color shade=Tinted(PitFacing(Mathf.Atan2(n0.y+n1.y,n0.x+n1.x)),new Color(.9f,.9f,.9f));
                        float h0=PitProfile(f0),h1=PitProfile(f1),fall=RailPlinth-PitStep;
                        PitSlope(b,p0,p1,q1,q0,new[]{RailPlinth,RailPlinth,h1,h0},u,
                            new[]{arris,arris,Color.Lerp(arris,shade,(RailPlinth-h1)/fall),Color.Lerp(arris,shade,(RailPlinth-h0)/fall)});
                    }
                    else if(pass==2)
                    {
                        PitStone(b,o0,o1,p1,p0,lift,u,joint);
                        Vector2 inner=(o1-o0).normalized*PitJoint,outer=(p1-p0).normalized*PitJoint;
                        Vector2 e0=r0>PitLip?n0*PitJoint:Vector2.zero,e1=r1>PitLip?n1*PitJoint:Vector2.zero;
                        PitStone(b,o0+inner+n0*PitJoint,o1-inner+n1*PitJoint,p1-outer-e1,p0+outer-e0,lift,u,
                            Tinted(CapTone,new Color(tone,tone,tone)));
                    }
                    else if(pass==3)
                    {
                        bool west=Mathf.Abs(s0.x-PitU0)<.001f&&Mathf.Abs(s1.x-PitU0)<.001f;
                        bool south=Mathf.Abs(s0.y-PitV0)<.001f&&Mathf.Abs(s1.y-PitV0)<.001f;
                        if(west||south)PitCourse(b,s0,s1,PitProfile(r0),PitProfile(r1),west?ShadeFace:LitFace);
                    }
                }
            return PitBounds(corners.ToArray());
        }

        // The collar stone's height at a distance reach out from the outline: the lip, the chamfer, then the step.
        private static float PitProfile(float reach)=>reach<=PitLip?RailPlinth:Mathf.Lerp(RailPlinth,PitStep,(reach-PitLip)/PitChamfer);

        // A collar stone part from its four design corners at their own heights (a-c the inner edge), over a block interior of
        // the masonry texture as PitStone, each corner in its own colour.
        private static void PitSlope(Builder b,Vector2 a,Vector2 c,Vector2 d,Vector2 e,float[] h,float u,Color[] color)
        {
            int start=b.U.Count;
            b.Quad(Project(a.x,a.y)+Vector3.up*h[0],Project(c.x,c.y)+Vector3.up*h[1],Project(d.x,d.y)+Vector3.up*h[2],
                Project(e.x,e.y)+Vector3.up*h[3],true,color[0]);
            b.U[start]=new Vector2(u,.45f);b.U[start+1]=new Vector2(u+.2f,.45f);
            b.U[start+2]=new Vector2(u+.2f,.55f);b.U[start+3]=new Vector2(u,.55f);
            for(int k=1;k<4;k++)b.C[start+k]=color[k];
        }

        // The course under a camera-facing stretch of the hole's edge, from the deck to the profile's heights at its ends, on
        // the plinth course's band as CourseFace lays it; full-height parts in the face's tone, the step's a shade darker.
        private static void PitCourse(Builder b,Vector2 p,Vector2 q,float hp,float hq,Color face)
        {
            Vector3 a=Project(p.x,p.y),c=Project(q.x,q.y);
            if(a.x>c.x){var swap=a;a=c;c=swap;float h=hp;hp=hq;hq=h;}
            Color ca=Tinted(face,PitCourseTone(hp)),cc=Tinted(face,PitCourseTone(hq));
            int start=b.U.Count;
            b.Quad(a,c,c+Vector3.up*hq,a+Vector3.up*hp,false,ca);
            b.C[start+1]=cc;b.C[start+2]=cc;
            float va=Mathf.Lerp(PlinthCourse.x,PlinthCourse.y,hp/RailPlinth),vc=Mathf.Lerp(PlinthCourse.x,PlinthCourse.y,hq/RailPlinth);
            b.U[start]=new Vector2(a.x/2.8f,PlinthCourse.x);b.U[start+1]=new Vector2(c.x/2.8f,PlinthCourse.x);
            b.U[start+2]=new Vector2(c.x/2.8f,vc);b.U[start+3]=new Vector2(a.x/2.8f,va);
        }
        private static Color PitCourseTone(float h)
        {
            float tone=Mathf.Lerp(.55f,1.02f,(h-PitStep)/(RailPlinth-PitStep));
            return new Color(tone,tone,tone);
        }

        // How far out from the outline the collar stone at p is laid: on a straight, from its line; in a corner, measured along
        // the middle normal of the 15-degree stone holding p and scaled to its radial joints, since each stone's breaks run
        // on chords between them - so the profile there is exactly the stone's.
        private static float PitReach(Vector2 p)
        {
            Vector2 q=p-PitCentre;
            if(Mathf.Abs(q.y)<=PitStraight)return Mathf.Abs(q.x)-PitRim;
            if(Mathf.Abs(q.x)<=PitStraight)return Mathf.Abs(q.y)-PitRim;
            Vector2 w=q-new Vector2(Mathf.Sign(q.x),Mathf.Sign(q.y))*PitStraight;
            const float step=15*Mathf.Deg2Rad;
            float mid=(Mathf.Floor(Mathf.Atan2(w.y,w.x)/step)+.5f)*step;
            return (w.x*Mathf.Cos(mid)+w.y*Mathf.Sin(mid))/Mathf.Cos(step*.5f)-PitRound;
        }

        // A collar post's camera faces carried down from the plinth's height to the collar stone where it stands lower, as
        // MassBox draws the shaft above (tone, world-continuous masonry): in pieces between the points where the ground under
        // the face bends - a corner stone's radial joints and the profile's breaks - so each follows the stone exactly.
        private static void PitSkirt(Builder b,Vector4 post)
        {
            PitSkirtFace(b,new Vector2(post.x,post.w),new Vector2(post.x,post.y),Tinted(ShadeFace,new Color(1.02f,1.02f,1.02f)));
            PitSkirtFace(b,new Vector2(post.x,post.y),new Vector2(post.z,post.y),Tinted(LitFace,new Color(1.02f,1.02f,1.02f)));
        }
        private static void PitSkirtFace(Builder b,Vector2 p,Vector2 q,Color face)
        {
            var cuts=new List<float>{0,1};
            Vector2 mid=(p+q)*.5f-PitCentre,arc=PitCentre+new Vector2(Mathf.Sign(mid.x),Mathf.Sign(mid.y))*PitStraight;
            for(int k=0;k<24;k++)
            {
                Vector2 ray=PitHeading(k*15*Mathf.Deg2Rad),along=q-p;
                float cross=along.x*ray.y-along.y*ray.x;
                if(Mathf.Abs(cross)<1e-6f)continue;
                float s=((arc.x-p.x)*ray.y-(arc.y-p.y)*ray.x)/cross;
                if(s>0&&s<1&&Vector2.Dot(p+along*s-arc,ray)>0)cuts.Add(s);
            }
            cuts.Sort();
            // Within a stone the reach runs linearly along the face, so its breaks interpolate exactly.
            for(int i=cuts.Count-2;i>=0;i--)
            {
                float sa=cuts[i],sc=cuts[i+1],ra=PitReach(Vector2.Lerp(p,q,sa)),rc=PitReach(Vector2.Lerp(p,q,sc));
                foreach(float edge in new[]{PitLip,PitLip+PitChamfer})
                    if((ra-edge)*(rc-edge)<0)cuts.Add(Mathf.Lerp(sa,sc,(edge-ra)/(rc-ra)));
            }
            cuts.Sort();
            // Ground heights at the cuts; a cut between two of equal height (the level step, the lip) is dropped.
            var ground=new List<float>();
            foreach(float s in cuts)ground.Add(PitProfile(PitReach(Vector2.Lerp(p,q,s))));
            for(int i=cuts.Count-2;i>0;i--)if(ground[i-1]==ground[i]&&ground[i]==ground[i+1]){cuts.RemoveAt(i);ground.RemoveAt(i);}
            Vector3 from=Project(p.x,p.y),to=Project(q.x,q.y);
            float slope=(to.y-from.y)/(to.x-from.x);
            for(int i=0;i+1<cuts.Count;i++)
            {
                float ha=ground[i],hc=ground[i+1];
                if(ha>=RailPlinth-1e-4f&&hc>=RailPlinth-1e-4f)continue;
                Vector3 a=Vector3.Lerp(from,to,cuts[i]),c=Vector3.Lerp(from,to,cuts[i+1]);
                int start=b.U.Count;
                b.Quad(a+Vector3.up*ha,c+Vector3.up*hc,c+Vector3.up*RailPlinth,a+Vector3.up*RailPlinth,false,face);
                for(int k=start;k<start+4;k++)b.U[k]=new Vector2(b.V[k].x/2.8f,(b.V[k].y-slope*b.V[k].x)/2.4f);
            }
        }

        // Where the outward normal from an outline point reaches the hole's edge.
        private static Vector2 PitEdge(Vector2 p,Vector2 n)
        {
            float reach=float.MaxValue;
            if(n.x>1e-4f)reach=Mathf.Min(reach,(PitU1-p.x)/n.x);else if(n.x<-1e-4f)reach=Mathf.Min(reach,(PitU0-p.x)/n.x);
            if(n.y>1e-4f)reach=Mathf.Min(reach,(PitV1-p.y)/n.y);else if(n.y<-1e-4f)reach=Mathf.Min(reach,(PitV0-p.y)/n.y);
            return p+n*reach;
        }

        // A flat stone at lift over a block interior of the masonry texture, as the balusters sample it.
        private static void PitStone(Builder b,Vector2 a,Vector2 c,Vector2 d,Vector2 e,Vector3 lift,float u,Color color)
        {
            int start=b.U.Count;
            b.Quad(Project(a.x,a.y)+lift,Project(c.x,c.y)+lift,Project(d.x,d.y)+lift,Project(e.x,e.y)+lift,true,color);
            b.U[start]=new Vector2(u,.45f);b.U[start+1]=new Vector2(u+.2f,.45f);
            b.U[start+2]=new Vector2(u+.2f,.55f);b.U[start+3]=new Vector2(u,.55f);
        }

        // The design-plane footprint (u0, v0, u1, v1) of the given points.
        private static Vector4 PitBounds(params Vector2[][] lines)
        {
            var box=new Vector4(float.MaxValue,float.MaxValue,float.MinValue,float.MinValue);
            foreach(var line in lines)
                foreach(var p in line)box=new Vector4(Mathf.Min(box.x,p.x),Mathf.Min(box.y,p.y),Mathf.Max(box.z,p.x),Mathf.Max(box.w,p.y));
            return box;
        }

        // The court posts (finials and four lanterns) stand in the rail line; the rail stops at them. Their stone is emitted
        // here among the rail pieces, their iron with the court borders (FoundryRegionSite.cs).
        private static readonly Vector2[] CourtFinials =
        {
            WestPoint(-24.5f,-6),WestPoint(-24.5f,12),new Vector2(-9,FoundryMap.South-.5f),
            new Vector2(2,FoundryMap.South-.5f),new Vector2(7.5f,FoundryMap.South-.5f),
            new Vector2(-14,18.5f),new Vector2(0,18.5f),new Vector2(10,18.5f)
        };

        // New lantern posts: the court's convex corners, the apron's concave corner, and the longest bare runs.
        private static readonly Vector4 CraneRearLantern=WestBox(-22.64f,16.08f,-22.08f,16.64f);
        private static readonly Vector4[] RailLanterns =
        {
            CraneRearLantern,WestBox(-24.72f,.64f,-24,1.36f),
            new Vector4(-7.36f,18,-6.64f,18.72f),new Vector4(14,-8.36f,14.72f,-7.64f),new Vector4(14,-.72f,14.72f,0),
            new Vector4(15.14f,10,15.86f,10.72f),new Vector4(21.64f,10,22.36f,10.72f),
            // The concept lines its rim with fire every few units. These fill the
            // longest bare straight runs on the south and north rails. The west
            // rail beside the workyard and the causeway between the boilers have
            // no post position that clears the placement tests, and a northwest
            // post would stand in front of the baked stock station.
            new Vector4(-7.86f,FoundryMap.South-.72f,-7.14f,FoundryMap.South),new Vector4(6.14f,FoundryMap.South-.72f,6.86f,FoundryMap.South),
            new Vector4(1.14f,FoundryMap.North,1.86f,FoundryMap.North+.72f),
            // A 2026-10-21 survey of every rail position against the placement tests: the remaining open gaps are two
            // on the north rail and one on the causeway's north rail (its south rail stands behind the boilers).
            new Vector4(-2.36f,FoundryMap.North,-1.64f,FoundryMap.North+.72f),new Vector4(5.64f,FoundryMap.North,6.36f,FoundryMap.North+.72f),
            new Vector4(18.14f,10,18.86f,10.72f)
        };
        private static readonly bool[] RailLanternFront={true,true,false,false,true,false,false,true,true,false,false,false,false};

        // Existing exterior structures the rail must not run through.
        private static readonly Vector4[] MassObstacles =
        {
            WestBox(-27,11,-24,15),SalvageShelf,WestBox(-25,8.4f,-24,10.5f),StockShelf,
            new Vector4(GateU(32),-3,GateU(35),-1),new Vector4(GateU(32),11,GateU(35),13),
            ReservoirFootprint(SouthReservoirV),ReservoirFootprint(NorthReservoirV),ReservoirNeck(false),ReservoirNeck(true),
            BridgePostBox(BridgeFootS,-BridgeFootD,.3f),BridgePostBox(BridgeFootS,BridgeFootD,.3f),
            BridgePostBox(BridgeHeadS,-BridgeHeadD,.7f),BridgePostBox(BridgeHeadS,BridgeHeadD,.7f)
        };

        // Every balustrade post the rail placed (design boxes), for checks that hang things on them (the gate braziers).
        private readonly List<Vector4> _railPostBoxes=new List<Vector4>();
        internal IReadOnlyList<Vector4> RailPostBoxes=>_railPostBoxes;

        private void BuildBalustrade(ArenaView arena,List<MassPiece> pieces)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            _railIron=arena.PlatformMaterials.FoundryIron;
            var placed=new List<Vector4>();
            _railPostBoxes.Clear();
            for(int i=0;i<CourtFinials.Length;i++)
            {
                Vector2 p=CourtFinials[i];
                Vector4 box=new Vector4(p.x-.38f,p.y-.38f,p.x+.38f,p.y+.38f);
                placed.Add(box);
                // The post's own stone is a piece too, so it paints in far-to-near order with the rail: emitted before it,
                // with the court borders, the span leaving its far side painted over its lower front. Its depth is the
                // footprint its landmark registers (FoundryRegionDepth.cs), as for its iron (BuildCourtBorders).
                AddPiece(pieces,new Vector4(p.x-.4f,p.y-.4f,p.x+.4f,p.y+.4f),2.85f,
                    ()=>{var stone=new Builder();CourtFinialMasonry(stone,p);Create("Outer stone finials",RimStone(stone),wall,-11);});
                // South and west finials face the camera: carry them down the wall as pilasters.
                bool front=p.y<FoundryMap.South||p.x<FoundryMap.West;
                if(front&&!MassBlocked(box))
                    AddPiece(pieces,box,0,()=>{var below=new Builder();MassBox(below,box,DeckFace,0,true,true,.93f);Create("Balustrade pilaster",RimFace(below),wall,-16);});
            }
            for(int i=0;i<RailLanterns.Length;i++)
            {
                Vector4 box=RailLanterns[i];
                bool front=RailLanternFront[i];
                if(!MassClear(box)||MassBlocked(box)||MassConflicts(box,2.8f,true))continue;
                placed.Add(box);
                // The basket's flames reach ~3.1; placement keeps the lantern-era 2.8 screen test.
                AddPiece(pieces,box,3.1f,()=>LanternPost(arena,box,front));
            }
            // Braziers are attached to explicit supports, not incidental gap subdivisions.
            // Elevation changes screen-overlap exclusions against boilers and therefore can
            // redistribute the procedural posts even though the walking boundary is unchanged.
            foreach(float v in GateBrazierPosts)
                PlaceRailPost(pieces,placed,new List<Vector2>(),new Vector4(GateU(32),v-.26f,GateU(32.52f),v+.26f),false,false,wall);
            // Exposed edges from the same cells and neighbour test as the movement map; the pit has its own collar.
            var lines=new Dictionary<(int dir,int line),List<int>>();
            foreach(var cell in _cells)
                for(int dir=0;dir<4;dir++)
                {
                    int nx=cell.x+(dir==1?-1:dir==3?1:0),ny=cell.y+(dir==0?-1:dir==2?1:0);
                    if(FoundryMap.UncutCell(nx,ny)||InPit(nx+.5f,ny+.5f)||
                        !CourtEdgeVisible(cell.x+.5f+(dir==1?-.5f:dir==3?.5f:0),cell.y+.5f+(dir==0?-.5f:dir==2?.5f:0)))continue;
                    var key=(dir,dir==0?cell.y:dir==1?cell.x:dir==2?cell.y+1:cell.x+1);
                    if(!lines.TryGetValue(key,out var along))lines[key]=along=new List<int>();
                    along.Add(dir%2==0?cell.x:cell.y);
                }
            var keys=new List<(int dir,int line)>(lines.Keys);
            keys.Sort();
            foreach(var key in keys)
            {
                var along=lines[key];
                along.Sort();
                for(int i=0;i<along.Count;)
                {
                    int j=i;
                    while(j+1<along.Count&&along[j+1]==along[j]+1)j++;
                    // Split the run where a unit is blocked or would touch the walking union.
                    int k=along[i],end=along[j]+1;
                    while(k<end)
                    {
                        while(k<end&&!RailUnitAllowed(key.dir,key.line,k))k++;
                        int s=k;
                        while(k<end&&RailUnitAllowed(key.dir,key.line,k))k++;
                        if(k>s)BuildRailRun(arena,pieces,placed,key.dir,key.line,s,k);
                    }
                    i=j+1;
                }
            }
        }

        private static bool RailUnitAllowed(int dir,int line,int unit)
        {
            // The closed portal already backs this edge of the landing. Leave
            // its central threshold visible instead of fencing across the door;
            // the two explicit brazier supports and outer returns stay in place.
            if(dir==3&&line==FoundryMap.GateLandingEnd&&unit>=1&&unit<10)return false;
            Vector4 box=EdgeBox(dir,line,unit,unit+1,0,RailBand);
            return MassClear(box)&&!MassBlocked(box)&&!MassConflicts(box,RailTop,true);
        }

        private void BuildRailRun(ArenaView arena,List<MassPiece> pieces,List<Vector4> placed,int dir,int line,float a,float b)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            bool alongU=dir%2==0,front=dir<2;
            var posts=new List<Vector2>();
            Vector4 region=EdgeBox(dir,line,a-.6f,b+.6f,0,RailBand);
            foreach(var box in placed)if(Overlap(box,region))posts.Add(Along(box,alongU));
            TryRailPost(pieces,placed,posts,dir,line,a,true,front,wall);
            TryRailPost(pieces,placed,posts,dir,line,b,false,front,wall);
            posts.Sort((x,y)=>x.x.CompareTo(y.x));
            // Regular posts at most about three units apart between the fixed ones.
            float cursor=a;
            var gaps=new List<Vector2>();
            foreach(var p in posts){if(p.x>cursor)gaps.Add(new Vector2(cursor,p.x));cursor=Mathf.Max(cursor,p.y);}
            if(b>cursor)gaps.Add(new Vector2(cursor,b));
            foreach(var gap in gaps)
            {
                float length=gap.y-gap.x;
                int count=Mathf.CeilToInt(length/3f)-1;
                for(int i=1;i<=count;i++)
                {
                    float c=gap.x+length*i/(count+1);
                    PlaceRailPost(pieces,placed,posts,EdgeBox(dir,line,c-.26f,c+.26f,0,.52f),alongU,front,wall);
                }
            }
            posts.Sort((x,y)=>x.x.CompareTo(y.x));
            cursor=a;
            // A span ends open (no post against its far end) where the run stops short of the next post, or has none.
            foreach(var p in posts)
            {
                if(p.x-cursor>.05f)AddRailSpan(pieces,dir,line,cursor,Mathf.Min(p.x,b),cursor==a,p.x>b+.05f,front,wall);
                cursor=Mathf.Max(cursor,p.y);
            }
            if(b-cursor>.05f)AddRailSpan(pieces,dir,line,cursor,b,cursor==a,true,front,wall);
        }

        private void AddRailSpan(List<MassPiece> pieces,int dir,int line,float a,float b,bool startFace,bool endFace,bool front,
            Material wall)
        {
            Vector4 box=EdgeBox(dir,line,a,b,0,RailBand);
            bool alongU=dir%2==0;
            AddPiece(pieces,box,RailTop,()=>
            {
                var above=new Builder();var below=front?new Builder():null;
                RailSpan(above,below,box,alongU,startFace,front,endFace);
                Create("Balustrade rail",RimStone(above),wall,-11);
                if(below!=null)Create("Balustrade cornice",RimFace(below),wall,-16);
            });
        }

        private void TryRailPost(List<MassPiece> pieces,List<Vector4> placed,List<Vector2> posts,int dir,int line,
            float end,bool start,bool front,Material wall)
        {
            foreach(var p in posts)if(p.y>=end-1f&&p.x<=end+1f)return;
            // Reach past the run end to close a convex corner; fall back inside the run at a concave one.
            float[] spans=start?new[]{end-.44f,end+.3f,end,end+.56f}:new[]{end-.3f,end+.44f,end-.56f,end};
            for(int i=0;i<spans.Length;i+=2)
                if(PlaceRailPost(pieces,placed,posts,EdgeBox(dir,line,spans[i],spans[i+1],0,.52f),dir%2==0,front,wall))return;
        }

        private bool PlaceRailPost(List<MassPiece> pieces,List<Vector4> placed,List<Vector2> posts,Vector4 box,
            bool alongU,bool front,Material wall)
        {
            if(!MassClear(box)||MassBlocked(box)||MassConflicts(box,1.35f,true))return false;
            // (The brass finial reaches 1.56; the 1.35 screen test is kept so the post layout is unchanged.)
            foreach(var other in placed)if(Overlap(other,box))return false;
            placed.Add(box);posts.Add(Along(box,alongU));_railPostBoxes.Add(box);
            AddPiece(pieces,box,1.56f,()=>
            {
                var above=new Builder();var below=front?new Builder():null;
                RailPost(above,below,box,front);
                Create("Balustrade post",RimStone(above),wall,-11);
                if(below!=null)Create("Balustrade pilaster",RimFace(below),wall,-16);
                RailPostBrass(_railIron,box,0);
            });
            return true;
        }

        // The corner returns keep the turned balustrade; the pit uses PitGuardRun and these shared course dimensions.
        // Exterior RailSpan uses a heavier stone sill with recessed square iron instead; it retains
        // the camera-facing RailDepth within the original footprint band and the same RailTop.
        private const float RailPlinth=.15f,RailHand=.57f,RailDepth=.24f,BalusterAxis=RailDepth*.5f;
        // The clear gap aimed for between two bellies; the solid respond closing an end with no post against it.
        private const float BalusterGap=.19f,RailRespond=.14f;
        // Turned profile bottom-up (radius, height): foot ring, the groove over it, a low belly (.2 wide, at a ~.39
        // pitch) and a long taper to the neck under the handrail, whose front hides the top ~.09.
        private static readonly Vector2[] BalusterProfile=
        {
            new Vector2(.078f,RailPlinth),new Vector2(.056f,.195f),new Vector2(.098f,.28f),new Vector2(.05f,RailHand)
        };
        private static readonly float[] BalusterRingTone={.92f,.8f,1.08f,.86f},BalusterColumns={-1,.3f,1};
        // Clean regions of the masonry texture (v up): two courses crossed only by head joints, so the rail reads as laid
        // coping stones with no bed joint running along it, and a block interior (u .33-.65, v .445-.56) for balusters.
        private static readonly Vector2 HandrailCourse=new Vector2(.345f,.41f),PlinthCourse=new Vector2(.44f,.52f);
        private static readonly Color GapShade=new Color(.3f,.32f,.4f);

        private static void RailSpan(Builder above,Builder below,Vector4 box,bool alongU,bool startFace,bool front,
            bool endFace=false)
        {
            bool faceV=alongU||startFace,faceU=!alongU||startFace;
            float s0=alongU?box.x:box.y,s1=alongU?box.z:box.w;
            // The existing posts determine the bay module: a wide bay has a solid central
            // masonry panel between two openings; short returns stay open above a low sill.
            // This gives the perimeter structural rhythm without moving any post or edge.
            const float lintel=.60f,respond=.22f;
            bool panel=s1-s0>=1.95f;
            float sill=panel?.28f:.18f;
            Vector4 body=RailBox(box,alongU,s0,s1,0,RailDepth);
            CourseBox(above,body,0,sill,faceV,faceU,1.02f,PlinthCourse);
            CourseTop(above,body,alongU,sill,Tinted(CapTone,new Color(.9f,.9f,.9f)),PlinthCourse);
            float first=s0+Mathf.Min(respond,(s1-s0)*.25f);
            float last=s1-Mathf.Min(respond,(s1-s0)*.25f);
            if(last-first<.32f)
                CourseBox(above,body,sill,lintel,faceV,faceU,1.02f,PlinthCourse);
            else
            {
                // Far-to-near within the existing masonry batch. The central panel stands
                // on the continuous sill; its face is recessed behind broad dressed shoulders.
                CourseBox(above,RailBox(box,alongU,last,s1,0,RailDepth),sill,lintel,true,true,1.02f,PlinthCourse);
                if(panel)
                {
                    float centre=(first+last)*.5f,half=Mathf.Clamp((s1-s0)*.14f,.26f,.43f);
                    ExteriorRailOpening(above,box,alongU,centre+half,last,sill,lintel);
                    ExteriorRailPanel(above,box,alongU,centre-half,centre+half,sill,lintel);
                    ExteriorRailOpening(above,box,alongU,first,centre-half,sill,lintel);
                }
                else ExteriorRailOpening(above,box,alongU,first,last,sill,lintel);
                CourseBox(above,RailBox(box,alongU,s0,first,0,RailDepth),sill,lintel,true,true,1.02f,PlinthCourse);
            }
            // Broad dressed coping closes the structural bay. Its restrained arris replaces the
            // formerly bright continuous handrail without changing the walking-side overhang.
            Vector4 hand=RailBox(box,alongU,s0,s1,front?-.03f:0,RailDepth+.02f);
            Vector2 arris=new Vector2(HandrailCourse.y-.012f,HandrailCourse.y);
            CourseBox(above,hand,lintel,RailTop-.025f,faceV,faceU,1.08f,new Vector2(HandrailCourse.x,arris.x));
            CourseBox(above,hand,RailTop-.025f,RailTop,faceV,faceU,1.24f,arris);
            CourseTop(above,hand,alongU,RailTop,Tinted(CapTone,new Color(.98f,.98f,.98f)),HandrailCourse);
            if(below==null)return;
            // The balustrade's own wall drops below the deck, with a projecting corbel course under its edge.
            MassBox(below,box,DeckFace,0,faceV,faceU,.9f);
            MassBox(below,box,-.5f,-.36f,faceV,faceU,.36f);
            Vector4 corbel=alongU?new Vector4(box.x,box.y-.07f,box.z,box.y):new Vector4(box.x-.07f,box.y,box.x,box.w);
            MassBox(below,corbel,-.36f,-.04f,faceV,faceU,1.08f);
        }

        private static void ExteriorRailOpening(Builder b,Vector4 box,bool alongU,float first,float last,
            float bottom,float top)
        {
            int bars=Mathf.Max(1,Mathf.RoundToInt((last-first)/.65f));
            for(int i=bars-1;i>=0;i--)
            {
                float at=Mathf.Lerp(first,last,(i+.5f)/bars);
                ExteriorRailIron(b,RailBox(box,alongU,at-.028f,at+.028f,.105f,.16f),bottom,top);
            }
            ExteriorRailIron(b,RailBox(box,alongU,first,last,.11f,.155f),bottom+.055f,bottom+.095f);
        }

        private static void ExteriorRailPanel(Builder b,Vector4 box,bool alongU,float first,float last,
            float bottom,float top)
        {
            // The backing sits physically behind the two shoulders and the lower ledge.
            // Its quiet slate face is broad enough to read as a panel, not another tiny joint.
            const float reveal=.055f,shoulder=.085f,ledge=.065f;
            Vector4 back=RailBox(box,alongU,first,last,reveal,RailDepth);
            CourseBox(b,back,bottom,top,true,true,.75f,PlinthCourse);
            CourseBox(b,RailBox(box,alongU,last-shoulder,last,0,RailDepth),bottom,top,true,true,1.08f,PlinthCourse);
            Vector4 foot=RailBox(box,alongU,first,last,0,RailDepth);
            CourseBox(b,foot,bottom,bottom+ledge,true,true,1.06f,PlinthCourse);
            CourseTop(b,foot,alongU,bottom+ledge,CapTone,PlinthCourse);
            CourseBox(b,RailBox(box,alongU,first,first+shoulder,0,RailDepth),bottom,top,true,true,1.08f,PlinthCourse);
        }

        private static void ExteriorRailIron(Builder b,Vector4 box,float bottom,float top)
        {
            int first=b.V.Count;
            CourseBox(b,box,bottom,top,true,true,.62f,PlinthCourse);
            for(int i=first;i<b.V.Count;i++)
            {
                b.U[i]=new Vector2(.47f,.49f);
                b.C[i]=Tinted(b.C[i],new Color(.82f,.91f,1f));
            }
        }

        // A turned baluster. Round, so from the fixed camera it is its own profile: a camera-facing card, shaded as a
        // column in the massing's light (shaded left edge, brightest right of centre), with a dark groove over the foot
        // and a lit belly. Each ring sags by half its radius at the front, as a level circle does in this projection,
        // so the foot stands on the ledge and the rings read as turned. Four rings of three columns: 12 vertices.
        private static void Baluster(Builder b,Vector3 axis,int seed)
        {
            int first=b.V.Count,columns=BalusterColumns.Length;
            float u0=.33f+.07f*(seed%4);
            for(int ring=0;ring<BalusterProfile.Length;ring++)
            {
                float r=BalusterProfile[ring].x,h=BalusterProfile[ring].y;
                float v=Mathf.Lerp(.445f,.56f,(h-RailPlinth)/(RailHand-RailPlinth));
                for(int k=0;k<columns;k++)
                {
                    float x=BalusterColumns[k],tone=BalusterRingTone[ring]*(k==0?.95f:k==1?1.26f:1);
                    b.V.Add(axis+new Vector3(x*r,h-.5f*r*Mathf.Sqrt(1-x*x),0));
                    b.U.Add(new Vector2(u0+(x+1)*.02f,v));
                    b.C.Add(Tinted(k==0?ShadeFace:LitFace,new Color(tone,tone,tone)));
                }
            }
            for(int ring=0;ring+1<BalusterProfile.Length;ring++)
                for(int k=0;k+1<columns;k++)
                {
                    int i=first+ring*columns+k;
                    b.T.AddRange(new[]{i,i+1,i+columns+1,i,i+columns+1,i+columns});
                }
        }

        // A span's design-plane box from along-edge s0..s1 and f0..f1 back from its camera face (v0 along u, u0 along v).
        private static Vector4 RailBox(Vector4 box,bool alongU,float s0,float s1,float f0,float f1)=>
            alongU?new Vector4(s0,box.y+f0,s1,box.y+f1):new Vector4(box.x+f0,s0,box.x+f1,s1);

        // MassBox's two camera faces, each on one clean course of the texture (CourseFace).
        private static void CourseBox(Builder b,Vector4 box,float bottom,float top,bool faceV,bool faceU,float tone,
            Vector2 band)
        {
            var toneColor=new Color(tone,tone,tone);
            if(faceU)CourseFace(b,Project(box.x,box.w),Project(box.x,box.y),bottom,top,Tinted(ShadeFace,toneColor),band);
            if(faceV)CourseFace(b,Project(box.x,box.y),Project(box.z,box.y),bottom,top,Tinted(LitFace,toneColor),band);
        }

        // Texture v spans the course band from bottom to top; u runs on with screen x as in WallQuad, so the head
        // joints stay put from span to span.
        private static void CourseFace(Builder b,Vector3 p,Vector3 q,float bottom,float top,Color color,Vector2 band)
        {
            int start=b.U.Count;
            b.Quad(p+Vector3.up*bottom,q+Vector3.up*bottom,q+Vector3.up*top,p+Vector3.up*top,false,color);
            b.U[start]=new Vector2(p.x/2.8f,band.x);b.U[start+1]=new Vector2(q.x/2.8f,band.x);
            b.U[start+2]=new Vector2(q.x/2.8f,band.y);b.U[start+3]=new Vector2(p.x/2.8f,band.y);
        }

        // The top of a course, from the box's camera face back. Texture u follows the front edge only, so each head
        // joint runs straight back across the top from the face below it.
        private static void CourseTop(Builder b,Vector4 top,bool alongU,float h,Color color,Vector2 band)
        {
            Vector3 a=alongU?Project(top.x,top.y):Project(top.x,top.w),c=alongU?Project(top.z,top.y):Project(top.x,top.y);
            Vector3 back=alongU?Project(0,top.w-top.y):Project(top.z-top.x,0),lift=Vector3.up*h;
            int start=b.U.Count;
            b.Quad(a+lift,c+lift,c+back+lift,a+back+lift,true,color);
            b.U[start]=new Vector2(a.x/2.8f,band.y);b.U[start+1]=new Vector2(c.x/2.8f,band.y);
            b.U[start+2]=new Vector2(c.x/2.8f,band.x);b.U[start+3]=new Vector2(a.x/2.8f,band.x);
        }

        // foot: where the shaft's faces start - the deck, or the pit's collar stone it stands on.
        private static void RailPost(Builder above,Builder below,Vector4 box,bool front,float foot=0)
        {
            MassBox(above,box,foot,1f,true,true,1.02f);
            Vector4 cap=GrowSafe(box,.045f);
            MassBox(above,cap,1f,1.12f,true,true,1.14f);
            MassCap(above,cap,1.12f,CapTone);
            Vector3 apex=Project((box.x+box.z)*.5f,(box.y+box.w)*.5f)+Vector3.up*1.34f,lift=Vector3.up*1.12f;
            above.WallQuad(Project(cap.x,cap.y)+lift,Project(cap.z,cap.y)+lift,apex,apex,false,new Color(.72f,.75f,.84f));
            above.WallQuad(Project(cap.x,cap.w)+lift,Project(cap.x,cap.y)+lift,apex,apex,false,new Color(.5f,.53f,.63f));
            if(front&&below!=null)MassBox(below,box,DeckFace,0,true,true,.93f);
        }

        // As on the concept's rail posts: a rubbed brass collar under the cap and a brass finial on the pyramid's point,
        // in the brazier ironwork batch (no new renderer), drawn after the post's stone in its sorting order. The collar's
        // octagon circumscribes the .52 post (corners at .37) so no stone corner shows through it.
        private Material _railIron;
        private void RailPostBrass(Material iron,Vector4 box,float foot)
        {
            Vector2 c=new Vector2((box.x+box.z)*.5f,(box.y+box.w)*.5f);
            float half=Mathf.Max(box.z-box.x,box.w-box.y)*.5f;
            var brass=new Builder();
            // Polished a little beyond the braziers' rubbed bronze so the rail's brass glints at map scale, as on the concept.
            MassCourse(brass,c,half*1.45f,half*1.45f,foot+.86f,foot+.97f,8,true,Bronze*1.3f);
            MassCourse(brass,c,.10f,.05f,foot+1.30f,foot+1.44f,6,true,Bronze*1.3f);
            MassCourse(brass,c,.05f,.012f,foot+1.44f,foot+1.56f,6,true,Bronze*1.45f);
            Create("Balustrade post brass",brass,iron,-10.5f);
        }

        // The post's cap top (1.69): the basket's cast foot starts there.
        private const float RailBrazierRise=1.79f;

        private void LanternPost(ArenaView arena,Vector4 box,bool front,float foot=0)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            var above=new Builder();
            MassBox(above,box,foot,1.48f,true,true,1.02f);
            MassBox(above,box,RailHand,RailTop,true,true,1.16f);
            Vector4 cap=GrowSafe(box,.04f);
            MassBox(above,cap,1.48f,1.69f,true,true,1.14f);
            MassCap(above,cap,1.69f,CapTone);
            // The northwest chamfer post stands behind the crane footing. Its
            // masonry must remain behind both the authored card and legacy mast;
            // the lantern cage/panes below retain their foreground ranks.
            Create("Balustrade lantern post",RimStone(above),wall,box==CraneRearLantern?-12:-11);
            if(front)
            {
                var below=new Builder();
                MassBox(below,box,DeckFace,0,true,true,.93f);
                Create("Balustrade pilaster",RimFace(below),wall,-16);
            }
            // As on the approved concept's rim, the post carries an open fire basket (the gate's and the bridge head's)
            // seated on its cap, instead of an enclosed amber lantern that read small and pale at map scale.
            Brazier(arena,new Vector2((box.x+box.z)*.5f,(box.y+box.w)*.5f),RailBrazierRise);
        }

        private void BuildBoilerFoundations(ArenaView arena)
        {
            // Tapered masonry drums carry each boiler down into the abyss, below the balustrade cornice.
            foreach(var c in BoilerCentres)
            {
                var b=new Builder();
                MassCourse(b,c,1.5f,1.75f,-10f,-1.2f,BoilerFacets,false,Color.white);
                MassCourse(b,c,1.84f,1.84f,-5.6f,-5.1f,BoilerFacets,false,new Color(1.1f,1.1f,1.1f));
                MassCourse(b,c,BoilerPlinth,BoilerPlinth,-1.2f,0,BoilerFacets,false,new Color(.95f,.95f,.95f));
                Vector3 deck=Project(c.x,c.y);
                ShadeUndercroftDepth(b,deck,deck);
                Create("Boiler foundation drum",b,arena.PlatformMaterials.FoundryWall,-16);
            }
        }

        // Deep bastions under the front edges and corners. Footprints are outside the walking union; the edge
        // side is the platform (w for v-edges, z for u-edges).
        private static readonly Vector4[] Bastions =
        {
            new Vector4(14,-8,16.6f,-5.4f),new Vector4(.7f,FoundryMap.South-2.6f,3.3f,FoundryMap.South),
            new Vector4(5.7f,FoundryMap.South-2.6f,8.3f,FoundryMap.South),
            WestBox(-26.6f,-7.3f,-24,-4.7f),WestBox(-26.6f,-.3f,-24,2.3f),WestBox(-26.6f,11.4f,-24,14)
        };

        private void BuildBastions(ArenaView arena)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            for(int i=0;i<Bastions.Length;i++)
            {
                Vector4 top=Bastions[i];
                if(!MassClear(top))continue;
                var b=new Builder();
                MassPier(b,top,i>0&&i<3,PierForms[i]);
                Vector3 deck=Project((top.x+top.z)*.5f,(top.y+top.w)*.5f);
                ShadeUndercroftDepth(b,deck,deck);
                Create("Foundation bastion",b,wall,-16);
            }
            // Heavy chains, after the approved layout, whose floating fortress hangs chains from its rim and undercroft
            // down into the violet void and between its masses: nine, few and long rather than many and thin. Each hangs
            // from a camera-facing face - a bastion block's south or west face, an undercroft pier's anchor plate, a far
            // bridge gatepost, the gate's foundation - and runs out toward the camera, so it is in front of all the
            // below-deck stone it crosses on screen: the chain rank (-15.4) paints over that stone (-16), under the deck
            // (-15) and everything above it, and nothing below the deck stands between a chain and the camera. None
            // crosses the walking floor or the bridge on screen, and no two cross. Emitted outside any depth scope, as
            // before: a floor rank writes no depth. Anchors (u, v, height), faces 0 south, 1 west.
            // The corner bastions' anchor chains and the catenary between the first two south piers (they were straight
            // and per-segment; now hung, the anchor chains with a slight sag):
            AbyssChain(arena,new Vector3(14.8f,-8f,-3.4f),0,new Vector2(5.2f,-7.2f),.45f,.36f);
            AbyssChain(arena,new Vector3(-26.6f+WestShift,12.4f,-3.4f),1,new Vector2(-5.2f,-7.2f),.45f,.36f);
            PierChain(arena,new Vector3(3f,FoundryMap.South-2.6f,-4f),0,
                new Vector3(7f,FoundryMap.South-2.6f,-4f),0,2f,.26f);
            // Across the gap between the two south-west piers of the west rim, and from the nearer one into the void.
            PierChain(arena,new Vector3(-26.6f+WestShift,-5f,-3.5f),1,new Vector3(-26.6f+WestShift,.3f,-3.5f),1,1.5f,.3f);
            AbyssChain(arena,new Vector3(-26.6f+WestShift,-6.8f,-4.3f),1,new Vector2(-6.2f,-9.8f),.7f,.4f);
            // Two more, as the concept hangs its rim with many: from the west rim's middle bastion, parallel to the one
            // below it, and from the east south pier, out past the corner bastion's chain without crossing it.
            AbyssChain(arena,new Vector3(-26.6f+WestShift,1.6f,-3.6f),1,new Vector2(-7f,-8.4f),.6f,.4f);
            AbyssChain(arena,new Vector3(6.2f,FoundryMap.South-2.6f,-3.8f),0,new Vector2(4.5f,-9.5f),.55f,.38f);
            // From the far gateposts' outboard chamfers, out of frame on both sides of the bridge's end; each post's
            // stone (-11) hides the anchor link, so the chain comes from behind it.
            AbyssChain(arena,new Vector3((BridgeFootS-BridgeFootD)*.5f-.255f,(BridgeFootS+BridgeFootD)*.5f+.255f,-2.2f),-1,new Vector2(-6.6f,-8.6f),.6f,.4f);
            AbyssChain(arena,new Vector3((BridgeFootS+BridgeFootD)*.5f+.255f,(BridgeFootS-BridgeFootD)*.5f-.255f,-2.2f),-1,new Vector2(6.6f,-8.6f),.6f,.4f);
            // Only an actually emitted, unscreened major pier has the existing iron socket.
            if(_southChainPier.HasValue)
                AbyssChain(arena,new Vector3(_southChainPier.Value.x,_southChainPier.Value.y,-3.9f),-1,
                    new Vector2(3f,-10.2f),.5f,.38f);
            // From the gate's south foundation, past the east edge of the frame.
            AbyssChain(arena,new Vector3(GateU(33.4f),-3f,-1.6f),0,new Vector2(10f,-11f),.9f,.42f);
        }

        // A chain from an anchor out and down into the void; its far end lies reach away on screen, out of frame or deep
        // in the void's shade. Face -1: no plate (the anchor is hidden, or already has one).
        private void AbyssChain(ArenaView arena,Vector3 anchor,int face,Vector2 reach,float sag,float width)
        {
            Vector3 a=ChainAnchor(arena,anchor,face,out Vector3 deck);
            HangChain(arena,a,a+new Vector3(reach.x,reach.y,0),sag,width,deck,deck);
        }

        // A catenary between two anchors.
        private void PierChain(ArenaView arena,Vector3 from,int fromFace,Vector3 to,int toFace,float sag,float width)
        {
            Vector3 a=ChainAnchor(arena,from,fromFace,out Vector3 deckA),b=ChainAnchor(arena,to,toFace,out Vector3 deckB);
            HangChain(arena,a,b,sag,width,deckA,deckB);
        }

        // A forged plate on the anchor's face (in the face's plane, bolted at its head), emitted into the chain batch ahead
        // of its chain, which hangs from the plate's foot; shaded as the undercroft's anchor plates. Returns the anchor.
        private Vector3 ChainAnchor(ArenaView arena,Vector3 anchor,int face,out Vector3 deck)
        {
            deck=Project(anchor.x,anchor.y);
            Vector3 a=deck+Vector3.up*anchor.z;
            if(face<0)return a;
            Vector3 along=(face==0?Project(1,0):Project(0,1))*.17f,up=Vector3.up*.22f,centre=a+Vector3.up*.16f;
            var plate=new Builder();
            FullFace(plate,centre-along-up,centre+along-up,centre+along+up,centre-along+up,new Color(.4f,.41f,.46f));
            foreach(float side in new[]{-.55f,.55f})
            {
                Vector3 bolt=centre+along*side+up*.6f;
                FullFace(plate,bolt+Vector3.up*.045f,bolt+Vector3.right*.04f,bolt+Vector3.down*.045f,bolt+Vector3.left*.04f,
                    new Color(.8f,.68f,.46f));
            }
            ShadeUndercroftDepth(plate,deck,deck);
            Create("Chain anchor plates",plate,arena.PlatformMaterials.FoundryIron,-15.4f);
            return a;
        }

        // Per pier: foot and shoulder heights, and the form (0 banded, 1 buttressed, 2 stepped), so the six read as
        // one family of different masses instead of a row of copies. Every shoulder stays below the chain anchors
        // (-3.4 to -4.3 on the block faces, plates included), and the block and head keep the deck footprint exactly as before.
        private static readonly Vector3[] PierForms =
        {
            new Vector3(-9.2f,-5.2f,0),new Vector3(-8.6f,-4.7f,1),new Vector3(-10.2f,-5.6f,2),
            new Vector3(-9.4f,-5f,1),new Vector3(-10f,-5.4f,0),new Vector3(-8.8f,-4.8f,2)
        };
        private static readonly Color PierLedge=new Color(.76f,.79f,.88f);

        // Bottom-up in painter order: a lower, inner stage first, so the stage above overhangs it. The shaft stands
        // a little inside the block, whose overhang casts a painted shadow band; the lit (south) face gets a bright
        // arris against the shaded one. Nothing leaves the top footprint: every stage is inset from it.
        private static void MassPier(Builder b,Vector4 top,bool alongU,Vector3 form)
        {
            float foot=form.x,shoulder=form.y;
            Vector4 neck=PierInset(top,alongU,.05f,.1f);
            switch((int)form.z)
            {
                case 0:
                {
                    // Tapered shaft girded by two projecting belt courses, each a lit ledge over a shadow.
                    Vector4 bottom=PierInset(top,alongU,.22f,.4f);
                    PierStage(b,neck,shoulder,bottom,foot,.95f,true);
                    // Blind panels between the belts give these long shafts a carved
                    // structure. Stay below the anchor block and inside the old faces.
                    foreach(var range in new[]{new Vector2(.08f,.26f),new Vector2(.38f,.56f),new Vector2(.70f,.91f)})
                        PierRecess(b,neck,shoulder,bottom,foot,alongU,.24f,.76f,range.x,range.y);
                    foreach(float f in new[]{.3f,.62f})
                    {
                        float h=Mathf.Lerp(shoulder,foot,f),under=h-.46f;
                        Vector4 core=Vector4.Lerp(neck,bottom,f);
                        Vector4 belt=new Vector4(Mathf.Max(top.x,core.x-.24f),Mathf.Max(top.y,core.y-.24f),
                            Mathf.Min(top.z,core.z+.24f),Mathf.Min(top.w,core.w+.24f));
                        PierStage(b,Vector4.Lerp(neck,bottom,(shoulder-under)/(shoulder-foot)),under,
                            Vector4.Lerp(neck,bottom,(shoulder-under+.6f)/(shoulder-foot)),under-.6f,.52f,false);
                        MassBox(b,belt,under,h,true,true,1.12f);
                        PierLedgeTop(b,belt,core,h);
                    }
                    break;
                }
                case 1:
                {
                    // Tapered shaft with a central buttress: vertical down to mid-height, then weathered back
                    // into the shaft; a corbel either side of it carries the block.
                    Vector4 bottom=PierInset(top,alongU,.18f,.36f);
                    PierStage(b,neck,shoulder,bottom,foot,.95f,true);
                    PierRecess(b,neck,shoulder,bottom,foot,alongU,.09f,.29f,.10f,.68f);
                    PierRecess(b,neck,shoulder,bottom,foot,alongU,.71f,.91f,.10f,.68f);
                    float mid=Mathf.Lerp(shoulder,foot,.5f),end=Mathf.Lerp(shoulder,foot,.74f);
                    float front=PierFront(top,alongU),back=PierFront(neck,alongU);
                    float midBack=PierFront(Vector4.Lerp(neck,bottom,.5f),alongU),endBack=PierFront(Vector4.Lerp(neck,bottom,.74f),alongU);
                    Vector4 rib=PierSlab(top,alongU,.37f,.63f,front,midBack);
                    // Flush with the block's face, so no overhang shadow on the buttress itself.
                    PierStage(b,PierSlab(top,alongU,.37f,.63f,front,back),shoulder,rib,mid,1.08f,true,false);
                    PierStage(b,rib,mid,PierSlab(top,alongU,.37f,.63f,endBack,endBack),end,1.02f,false,false);
                    PierCorbels(b,top,neck,bottom,alongU,shoulder,foot,new[]{.2f,.8f});
                    break;
                }
                default:
                {
                    // Stepped: a broad grooved upper stage, a narrower vertical tier, and a pointed foot, each
                    // shadowed under the one above; three corbels carry the block.
                    Vector4 stage=PierInset(top,alongU,.1f,.16f),tier=PierInset(top,alongU,.19f,.28f);
                    Vector4 toe=PierInset(top,alongU,.25f,.35f),point=PierInset(top,alongU,.32f,.5f);
                    float h1=Mathf.Lerp(shoulder,foot,.42f),h2=Mathf.Lerp(shoulder,foot,.68f);
                    PierStage(b,toe,h2,point,foot,.86f,true,true);
                    PierStage(b,tier,h1,tier,h2,.9f,true,true);
                    PierStage(b,neck,shoulder,stage,h1,.95f,true);
                    Vector3 high=Vector3.up*shoulder,low=Vector3.up*h1;
                    foreach(float t in new[]{.34f,.66f})
                    {
                        Vector3 upper=alongU?Vector3.Lerp(Project(neck.x,neck.y),Project(neck.z,neck.y),t):
                            Vector3.Lerp(Project(neck.x,neck.w),Project(neck.x,neck.y),t);
                        Vector3 lower=alongU?Vector3.Lerp(Project(stage.x,stage.y),Project(stage.z,stage.y),t):
                            Vector3.Lerp(Project(stage.x,stage.w),Project(stage.x,stage.y),t);
                        ArchitectureBar(b,upper+high+Vector3.down*.5f,lower+low,.16f,new Color(.2f,.22f,.29f));
                    }
                    PierCorbels(b,top,neck,stage,alongU,shoulder,h1,new[]{.25f,.5f,.75f});
                    break;
                }
            }
            MassBox(b,top,shoulder,-2.75f,true,true,.95f);
            MassBox(b,top,-3.95f,-3.8f,true,true,.5f);
            Vector4 head=GrowSafe(top,.14f);
            // A recessed neck carries a chamfered capital, within the original head bounds.
            // Build upward so each upper cap covers the lower stage's hidden return.
            ArchitecturalTier(b,top.x,top.y,top.z,top.w,-2.75f,-2.58f,.02f);
            ArchitecturalTier(b,head.x,head.y,head.z,head.w,-2.58f,DeckFace-.22f,.04f);
            int crownStart=b.V.Count;
            ArchitecturalTier(b,head.x,head.y,head.z,head.w,DeckFace-.22f,DeckFace,.15f);
            // The crown is a dressed slab: don't wrap the wall's mortar grid over its bevel.
            for(int vertex=crownStart;vertex<b.V.Count;vertex++)
                b.U[vertex]=new Vector2(.34f,.70f)+(Vector2)(b.V[vertex]-b.V[crownStart])*.025f;
            BastionCornerStones(b,top,shoulder,-2.78f);
        }

        private static void PierRecess(Builder mesh,Vector4 upper,float high,Vector4 lower,float low,
            bool alongU,float left,float right,float from,float to)
        {
            // Carved blind recess: its four reveals and inset bed partition a part of
            // the existing projected face. No outer contour, anchor or collision moves.
            Vector3 Point(float across,float down)
            {
                Vector4 box=Vector4.Lerp(upper,lower,down);
                Vector3 p=alongU?Vector3.Lerp(Project(box.x,box.y),Project(box.z,box.y),across):
                    Vector3.Lerp(Project(box.x,box.w),Project(box.x,box.y),across);
                return p+Vector3.up*Mathf.Lerp(high,low,down);
            }
            Vector3 a=Point(left,to),b=Point(right,to),c=Point(right,from),d=Point(left,from);
            // The long inset has a broad top shadow, shaded left reveal and a catching
            // right/bottom reveal. Continuous masonry UVs retain the wall's courses.
            Vector3 across=(b-a)*.15f,vertical=(d-a)*.045f;
            Vector3 ia=a+across+vertical,ib=b-across+vertical;
            Vector3 ic=c-across-vertical,id=d+across-vertical;
            Color face=alongU?new Color(.42f,.44f,.53f):new Color(.31f,.34f,.43f);
            mesh.WallQuad(ia,ib,ic,id,false,face*.73f);
            mesh.WallQuad(a,b,ib,ia,false,face*1.15f);
            mesh.WallQuad(b,c,ic,ib,false,face*1.28f);
            mesh.WallQuad(c,d,id,ic,false,face*.52f);
            mesh.WallQuad(d,a,ia,id,false,face*.64f);
        }

        private static void BastionCornerStones(Builder b,Vector4 box,float bottom,float top)
        {
            // Large dressed quoins wrap the near corner. Chain anchors remain on the
            // central faces; the established anchor planes and silhouette are unchanged.
            int courses=Mathf.Max(1,Mathf.CeilToInt((top-bottom)/.62f));
            for(int i=0;i<courses;i++)
            {
                float lo=Mathf.Lerp(bottom,top,i/(float)courses)+.018f;
                float hi=Mathf.Lerp(bottom,top,(i+1)/(float)courses)-.018f;
                float reach=i%2==0?.42f:.30f;
                Vector3 corner=Project(box.x,box.y);
                Vector3 west=Project(box.x,box.y+reach),south=Project(box.x+reach,box.y);
                GateDressedFace(b,west+Vector3.up*lo,corner+Vector3.up*lo,
                    corner+Vector3.up*hi,west+Vector3.up*hi,new Color(.51f,.55f,.64f),.08f);
                GateDressedFace(b,corner+Vector3.up*lo,south+Vector3.up*lo,
                    south+Vector3.up*hi,corner+Vector3.up*hi,new Color(.76f,.79f,.86f),.08f);
            }
        }

        // A pier stage from its upper footprint down to its lower one: the two camera faces, a bright arris on the
        // lit face where it turns into the shaded one, and, under an overhang, a painted shadow band at its top.
        private static void PierStage(Builder b,Vector4 upper,float hu,Vector4 lower,float hl,float tone,bool arris,
            bool shadowed=true)
        {
            PierFaces(b,upper,hu,lower,hl,tone);
            if(arris)
            {
                Vector3 hi=Vector3.up*hu,lo=Vector3.up*hl;
                float eu=Mathf.Min(.2f,(upper.z-upper.x)*.3f),el=Mathf.Min(.2f,(lower.z-lower.x)*.3f);
                b.WallQuad(Project(lower.x,lower.y)+lo,Project(lower.x+el,lower.y)+lo,Project(upper.x+eu,upper.y)+hi,
                    Project(upper.x,upper.y)+hi,false,Tinted(LitFace,new Color(tone*1.34f,tone*1.34f,tone*1.3f)));
            }
            if(!shadowed||hu-hl<1f)return;
            PierFaces(b,upper,hu,Vector4.Lerp(upper,lower,.5f/(hu-hl)),hu-.5f,.5f);
        }

        private static void PierFaces(Builder b,Vector4 upper,float hu,Vector4 lower,float hl,float tone)
        {
            Vector3 hi=Vector3.up*hu,lo=Vector3.up*hl;
            var toneColor=new Color(tone,tone,tone);
            b.WallQuad(Project(lower.x,lower.w)+lo,Project(lower.x,lower.y)+lo,Project(upper.x,upper.y)+hi,
                Project(upper.x,upper.w)+hi,false,Tinted(ShadeFace,toneColor));
            b.WallQuad(Project(lower.x,lower.y)+lo,Project(lower.z,lower.y)+lo,Project(upper.z,upper.y)+hi,
                Project(upper.x,upper.y)+hi,false,Tinted(LitFace,toneColor));
        }

        // The exposed top of a belt course: only the L between its outline and the shaft's, never the part the
        // shaft above hides, which would paint over the shaft.
        private static void PierLedgeTop(Builder b,Vector4 belt,Vector4 core,float h)
        {
            Vector3 lift=Vector3.up*h;
            b.Quad(Project(belt.x,belt.y)+lift,Project(belt.z,belt.y)+lift,Project(belt.z,core.y)+lift,
                Project(belt.x,core.y)+lift,true,PierLedge);
            b.Quad(Project(belt.x,core.y)+lift,Project(core.x,core.y)+lift,Project(core.x,belt.w)+lift,
                Project(belt.x,belt.w)+lift,true,PierLedge);
        }

        // Sloped brackets from the block's face back into the shaft below it.
        private static void PierCorbels(Builder b,Vector4 top,Vector4 neck,Vector4 lower,bool alongU,float shoulder,
            float bottom,float[] at)
        {
            const float drop=.62f;
            float front=PierFront(top,alongU),back=PierFront(neck,alongU);
            float foot=PierFront(Vector4.Lerp(neck,lower,drop/(shoulder-bottom)),alongU);
            foreach(float c in at)
                PierStage(b,PierSlab(top,alongU,c-.07f,c+.07f,front,back),shoulder,
                    PierSlab(top,alongU,c-.07f,c+.07f,foot,foot),shoulder-drop,1.1f,false,false);
        }

        // Inset from the top footprint: along the platform edge on both ends, and back from the outward face.
        private static Vector4 PierInset(Vector4 top,bool alongU,float along,float outward)
        {
            float width=alongU?top.z-top.x:top.w-top.y,depth=alongU?top.w-top.y:top.z-top.x;
            return alongU?new Vector4(top.x+along*width,top.y+outward*depth,top.z-along*width,top.w):
                new Vector4(top.x+outward*depth,top.y+along*width,top.z,top.w-along*width);
        }

        // A slab across a fraction of the pier's width, between two outward coordinates.
        private static Vector4 PierSlab(Vector4 top,bool alongU,float a0,float a1,float front,float back)=>
            alongU?new Vector4(Mathf.Lerp(top.x,top.z,a0),front,Mathf.Lerp(top.x,top.z,a1),back):
                new Vector4(front,Mathf.Lerp(top.y,top.w,a0),back,Mathf.Lerp(top.y,top.w,a1));

        private static float PierFront(Vector4 box,bool alongU)=>alongU?box.y:box.x;

        // --- Shared massing primitives --------------------------------------------------------------------

        // Each piece is one structure for actor depth (FoundryRegionDepth), emitted in far-to-near order.
        private void AddPiece(List<MassPiece> pieces,Vector4 box,float height,System.Action emit)=>
            pieces.Add(new MassPiece{Depth=(box.x+box.z+box.y+box.w)*.5f,Emit=()=>{using(MassDepth(box,height))emit();}});

        // The only coupling to the depth system: a non-landmark footprint that fades in front of the hero.
        private DepthScope MassDepth(Vector4 box,float height)=>
            new DepthScope(this,RegisterDepthFootprint(box.x,box.y,box.z,box.w,height,true,false));

        // Angles span the camera-facing half, 135..315 degrees, so the silhouette falls on facet edges.
        private static float FrontAngle(int k,int m)=>(135f+180f*k/m)*Mathf.Deg2Rad;

        private static Vector3 OnRing(Vector2 c,float r,float angle,float h)=>
            Project(c.x+Mathf.Cos(angle)*r,c.y+Mathf.Sin(angle)*r)+Vector3.up*h;

        private static Color Tinted(Color c,Color tint)=>new Color(c.r*tint.r,c.g*tint.g,c.b*tint.b,1);

        private static Color StoneLight(float angle)
        {
            float l=.6f+.17f*Mathf.Cos(angle-285f*Mathf.Deg2Rad);
            return new Color(l,l*1.03f,l*1.13f);
        }

        private static Color IronLight(float angle)
        {
            float l=.68f+.19f*Mathf.Cos(angle+1.1f);
            return new Color(l,l*.98f,l*.94f);
        }

        private static Color PaintLight(float angle)
        {
            float l=Mathf.Lerp(.74f,1.02f,(1+Mathf.Cos(angle-280f*Mathf.Deg2Rad))*.5f);
            return new Color(l,l,l);
        }

        // Front half of a (tapered) round course. Stone gets continuous cylindrical masonry UVs. Iron no longer
        // maps the whole riveted panel onto every facet (a barrel of staves, the reservoirs' defect): a plate runs
        // on across several facets, about as wide round the drum as the course is tall so rivets stay round, and
        // its frame rails land on the course edges as hoops. A course too thin for a plate (a hoop, a rim) takes
        // the panel's riveted top rail, running on round the drum. The texture clamps, so the wrap restarts only
        // on facet edges; stagger shifts the seams by a facet, as lapped boiler plate does.
        private static void MassCourse(Builder b,Vector2 c,float r0,float r1,float h0,float h1,int m,bool iron,Color tint,
            int stagger=0,bool forgeLight=false)
        {
            float r=(r0+r1)*.5f,rise=Mathf.Max(h1-h0,Mathf.Abs(r1-r0)*.5f);
            bool rail=rise<.2f;
            int per=Mathf.Clamp(Mathf.RoundToInt((rail?1.6f:rise)*m/(Mathf.PI*r)),1,m);
            if(per*2>m)per=m;
            Vector4 region=rail?new Vector4(.1f,.915f,.9f,.99f):new Vector4(0,0,1,1);
            for(int k=0;k<m;k++)
            {
                float a=FrontAngle(k,m),e=FrontAngle(k+1,m),mid=(a+e)*.5f;
                Vector3 p=OnRing(c,r0,a,h0),q=OnRing(c,r0,e,h0),s=OnRing(c,r1,e,h1),t=OnRing(c,r1,a,h1);
                if(iron)
                {
                    int plate=b.U.Count,at=(k+stagger)%per;
                    FullFace(b,p,q,s,t,Tinted(IronLight(mid),tint));
                    float u0=Mathf.Lerp(region.x,region.z,at/(float)per),u1=Mathf.Lerp(region.x,region.z,(at+1)/(float)per);
                    b.U[plate]=new Vector2(u0,region.y);b.U[plate+1]=new Vector2(u1,region.y);
                    b.U[plate+2]=new Vector2(u1,region.w);b.U[plate+3]=new Vector2(u0,region.w);
                    if(forgeLight)for(int j=0;j<4;j++)
                        b.C[plate+j]=MetalTone(b.C[plate+j],b.V[plate+j],j<2?h0:h1);
                    continue;
                }
                WeatheredCourseFace(b,c,r0,r1,h0,h1,a,e,Tinted(StoneLight(mid),tint));
            }
        }

        private static void PaintedDrum(Builder b,Vector2 c,float r,float h0,float h1,int m,int fronts,Color tint,float v0=0,float v1=1)
        {
            int per=m/fronts;
            for(int k=0;k<m;k++)
            {
                float a=FrontAngle(k,m),e=FrontAngle(k+1,m);
                int start=b.U.Count;
                FullFace(b,OnRing(c,r,a,h0),OnRing(c,r,e,h0),OnRing(c,r,e,h1),OnRing(c,r,a,h1),Tinted(PaintLight((a+e)*.5f),tint));
                float u0=(k%per)/(float)per,u1=(k%per+1)/(float)per;
                b.U[start]=new Vector2(u0,v0);b.U[start+1]=new Vector2(u1,v0);
                b.U[start+2]=new Vector2(u1,v1);b.U[start+3]=new Vector2(u0,v1);
            }
        }

        // Full top disc as a fan of quads, each spanning two segments.
        private static void MassDisk(Builder b,Vector2 c,float r,float h,int m,bool iron,Color color,bool forgeLight=false)
        {
            Vector3 centre=Project(c.x,c.y)+Vector3.up*h;
            for(int k=0;k<2*m;k+=2)
            {
                Vector3 p=OnRing(c,r,FrontAngle(k,m),h),q=OnRing(c,r,FrontAngle(k+1,m),h),s=OnRing(c,r,FrontAngle(k+2,m),h);
                if(iron)
                {
                    int first=b.V.Count;
                    FullFace(b,centre,p,q,s,color);
                    for(int j=first;j<b.V.Count;j++)
                    {
                        Color tone=RoofPatina(color,b.V[j],centre,r);
                        b.C[j]=forgeLight?MetalTone(tone,b.V[j],h):tone;
                    }
                }
                else
                {
                    int first=b.V.Count;
                    b.Quad(centre,p,q,s,true,color);
                    for(int j=first;j<b.V.Count;j++)
                    {
                        Vector3 floor=b.V[j]-Vector3.up*h;
                        FoundrySurface.ToDesign(floor.x,floor.y,out float u,out float v);
                        b.C[j]=SurfaceTone(color,u,v,h,2);
                    }
                }
            }
        }

        // The two camera-facing faces of a design-plane box: v0 (lit) and u0 (shade), as Block draws them.
        private static void MassBox(Builder b,Vector4 box,float bottom,float top,bool faceV,bool faceU,float tone)
        {
            var toneColor=new Color(tone,tone,tone);
            if(faceU)
            {
                WeatheredBoxFace(b,new Vector2(box.x,box.w),new Vector2(box.x,box.y),bottom,top,Tinted(ShadeFace,toneColor));
            }
            if(faceV)
            {
                WeatheredBoxFace(b,new Vector2(box.x,box.y),new Vector2(box.z,box.y),bottom,top,Tinted(LitFace,toneColor));
            }
        }

        private static void MassCap(Builder b,Vector4 box,float h,Color color)
        {
            Vector3 lift=Vector3.up*h;
            int first=b.V.Count;
            b.Quad(Project(box.x,box.y)+lift,Project(box.z,box.y)+lift,Project(box.z,box.w)+lift,Project(box.x,box.w)+lift,true,color);
            b.C[first]=SurfaceTone(color,box.x,box.y,h,2);b.C[first+1]=SurfaceTone(color,box.z,box.y,h,2);
            b.C[first+2]=SurfaceTone(color,box.z,box.w,h,2);b.C[first+3]=SurfaceTone(color,box.x,box.w,h,2);
        }

        // A design-plane band beside an exposed edge: dir 0 south (-v), 1 west (-u), 2 north (+v), 3 east (+u).
        private static Vector4 EdgeBox(int dir,float line,float a,float b,float near,float far)
        {
            switch(dir)
            {
                case 0:return new Vector4(a,line-far,b,line-near);
                case 1:return new Vector4(line-far,a,line-near,b);
                case 2:return new Vector4(a,line+near,b,line+far);
                default:return new Vector4(line+near,a,line+far,b);
            }
        }

        private static Vector2 Along(Vector4 box,bool alongU)=>alongU?new Vector2(box.x,box.z):new Vector2(box.y,box.w);

        private static bool Overlap(Vector4 p,Vector4 q)=>
            p.x<q.z-.005f&&q.x<p.z-.005f&&p.y<q.w-.005f&&q.y<p.w-.005f;

        private static bool InPit(float u,float v)=>u>PitU0&&u<PitU1&&v>PitV0&&v<PitV1;

        // True when no point of the footprint is on the shared walking union (boundary contact is allowed).
        private static bool MassClear(Vector4 box)
        {
            const float inset=.015f;
            int nu=Mathf.Max(1,Mathf.CeilToInt((box.z-box.x)/.2f)),nv=Mathf.Max(1,Mathf.CeilToInt((box.w-box.y)/.2f));
            for(int i=0;i<=nu;i++)
                for(int j=0;j<=nv;j++)
                {
                    float u=Mathf.Lerp(box.x+inset,box.z-inset,i/(float)nu),v=Mathf.Lerp(box.y+inset,box.w-inset,j/(float)nv);
                    FoundryMap.ToFloor(u,v,out float x,out float y);
                    if(FoundryMap.SurfaceContains(x,y,0))return false;
                }
            return true;
        }

        // Grow a footprint on each side that stays clear of the walking union.
        private static Vector4 GrowSafe(Vector4 box,float e)
        {
            Vector4 grown=box;
            if(MassClear(new Vector4(box.x-e,box.y,box.x,box.w)))grown.x-=e;
            if(MassClear(new Vector4(box.z,box.y,box.z+e,box.w)))grown.z+=e;
            if(MassClear(new Vector4(box.x,box.y-e,box.z,box.y)))grown.y-=e;
            if(MassClear(new Vector4(box.x,box.w,box.z,box.w+e)))grown.w+=e;
            return grown;
        }

        private static bool MassBlocked(Vector4 box)
        {
            foreach(var o in MassObstacles)if(Overlap(box,new Vector4(o.x-.1f,o.y-.1f,o.z+.1f,o.w+.1f)))return true;
            for(int i=0;i<BoilerCentres.Length;i++)
            {
                // The front boilers replace the rail along their edge; the rear ones only need clearance.
                float r=BoilerPlinth+(i>=2?.6f:.03f);
                Vector2 c=BoilerCentres[i];
                float du=Mathf.Max(0,Mathf.Max(box.x-c.x,c.x-box.z)),dv=Mathf.Max(0,Mathf.Max(box.y-c.y,c.y-box.w));
                if(du*du+dv*dv<r*r)return true;
            }
            return false;
        }

        // Rails, posts and lanterns sort above the boiler drums (-12): one standing behind a boiler would paint
        // through it (overlay). Compare the piece's screen extent with the drum and cornice silhouette.
        private static bool MassConflicts(Vector4 box,float top,bool overlay)
        {
            float x0=float.MaxValue,x1=float.MinValue,y0=float.MaxValue,y1=float.MinValue;
            foreach(var corner in new[]{new Vector2(box.x,box.y),new Vector2(box.z,box.y),new Vector2(box.z,box.w),new Vector2(box.x,box.w)})
            {
                Vector3 p=Project(corner.x,corner.y);
                x0=Mathf.Min(x0,p.x);x1=Mathf.Max(x1,p.x);y0=Mathf.Min(y0,p.y);y1=Mathf.Max(y1,p.y+top);
            }
            float depth=(box.x+box.z+box.y+box.w)*.5f;
            for(int i=0;i<BoilerCentres.Length;i++)
            {
                Vector2 c=BoilerCentres[i];
                Vector3 centre=Project(c.x,c.y);
                float dx=Mathf.Clamp(centre.x,x0,x1)-centre.x,reach=BoilerPlinth+.03f;
                if(Mathf.Abs(dx)>=reach)continue;
                float lowest=Mathf.Abs(dx)<BoilerDrum?centre.y+BoilerBase(i)-.5f*Mathf.Sqrt(BoilerDrum*BoilerDrum-dx*dx):
                    centre.y+BoilerDrumTop(i)-.5f*Mathf.Sqrt(reach*reach-dx*dx);
                if(y1<=lowest||y0>=centre.y+BoilerTop(i)+.5f*reach)continue;
                if(overlay==(depth>c.x+c.y))return true;
            }
            return false;
        }
    }
}
