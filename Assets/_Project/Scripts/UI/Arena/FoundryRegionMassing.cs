using System.Collections.Generic;
using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    // Monumental massing, after the approved layout: four round banded boilers piped to each other and to the
    // gate, a continuous heavy balustrade with posts and lanterns along every exposed edge, a deep glowing
    // furnace pit with inner boilers and a strong collar, and foundation bastions with chains under the deck.
    // A few large silhouettes that read at the portrait camera, not small ornament. Every above-deck footprint
    // is tested against the shared walking union and skipped rather than allowed to intrude on it; the map,
    // movement and collision are unchanged.
    //
    // Painter order is by (material, order) batch, then by emission inside a batch. The stacks below respect it:
    // boiler plinths -13 (floor stone), painted boiler drums -12, cornices, balustrade, pit collar -11 (masonry),
    // iron shells, domes and pipes -10.5, fire windows and lantern panes -10; pit lining -15.5 under the floor;
    // below-deck stone -16. Nothing above the deck uses -14: that sorting rank is shared with the flush floor
    // decals, which would otherwise paint over a balustrade standing in front of them.
    public sealed partial class FoundryRegionView
    {
        // Boiler centres (design u,v): two behind the gate apron (north), two in front of it (south). Each sits
        // wholly on void ground, flush against the apron edges, and meets the existing service mains
        // (v -4.5 / 14.5, u 19.7..25.1) on its drum surface.
        private static readonly Vector2[] BoilerCentres =
        {
            new Vector2(18.39f,15.81f),new Vector2(26.95f,14.5f),new Vector2(17.9f,-4.08f),new Vector2(26.9f,-4.08f)
        };
        private const float BoilerPlinth=2f,BoilerDrum=1.85f,BoilerShell=1.6f;
        private const float DrumBase=.45f,DrumTop=3.35f,CorniceTop=3.6f,ShellTop=5.4f,FlueTop=7.2f;
        private const int BoilerFacets=10;
        // Balustrade: a .42-deep footprint band on the void side of each exposed edge; the balustrade itself stands in
        // the band's camera-facing RailDepth (RailSpan).
        private const float RailBand=.42f,RailTop=.72f,DeckFace=-2.3f;
        // Furnace pit: the existing non-walkable shaft rectangle, lined inside a .55 collar.
        private const float PitU0=-12,PitV0=2,PitU1=-4,PitV1=10,PitCollar=.55f,PitDepth=3.2f,PitLining=2.6f;
        private static readonly Vector2[] PitBoilers={new Vector2(-9.6f,7.6f),new Vector2(-6.4f,4.4f)};
        private static readonly Color LitFace=new Color(.63f,.66f,.77f),ShadeFace=new Color(.43f,.46f,.57f);
        private static readonly Color CapTone=new Color(.86f,.89f,.98f),Bronze=new Color(1.22f,.95f,.6f);

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
            MassCourse(iron,c,BoilerShell,BoilerShell,CorniceTop,4.5f,BoilerFacets,true,Color.white);
            MassCourse(iron,c,BoilerShell,BoilerShell,4.5f,ShellTop,BoilerFacets,true,Color.white,1);
            MassCourse(iron,c,BoilerShell+.06f,BoilerShell+.06f,CorniceTop,CorniceTop+.15f,BoilerFacets,true,Bronze);
            MassCourse(iron,c,BoilerShell+.07f,BoilerShell+.07f,ShellTop-.1f,ShellTop+.06f,BoilerFacets,true,Bronze);
            MassDisk(iron,c,BoilerShell+.07f,ShellTop+.06f,BoilerFacets,true,new Color(.46f,.48f,.54f));
            float h=ShellTop+.06f;
            MassCourse(iron,c,1.42f,1.12f,h,h+.36f,8,true,new Color(1.1f,1.1f,1.12f));
            MassCourse(iron,c,1.12f,.72f,h+.36f,h+.68f,8,true,new Color(1.16f,1.16f,1.18f));
            MassCourse(iron,c,.72f,.36f,h+.68f,h+.86f,6,true,new Color(1.2f,1.2f,1.22f));
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
            float mainU=c.x<22?19.7f:25.1f;
            Vector2 toward=(new Vector2(mainU,-4.5f)-c).normalized;
            Vector2 box=c+toward*(BoilerDrum+.03f);
            bool boxVisible=toward.x+toward.y<0;
            if(!boxVisible)WorkshopMetalHull(iron,box.x-.26f,box.y-.26f,box.x+.26f,box.y+.26f,SquatCorniceTop,2.06f,.04f);
            MassCourse(iron,c,1.62f,1.62f,SquatCorniceTop,1.55f,BoilerFacets,true,Bronze);
            MassDisk(iron,c,1.62f,1.55f,BoilerFacets,true,new Color(.46f,.48f,.54f));
            MassCourse(iron,c,1.52f,1.12f,1.55f,1.78f,BoilerFacets,true,new Color(1.1f,1.1f,1.12f));
            MassCourse(iron,c,1.12f,.64f,1.78f,1.93f,8,true,new Color(1.16f,1.16f,1.18f));
            MassCourse(iron,c,.64f,.34f,1.93f,2f,6,true,new Color(1.2f,1.2f,1.22f));
            MassDisk(iron,c,.34f,2f,6,true,new Color(.5f,.52f,.58f));
            BoilerVent(iron,c,2f,SquatTop,.2f);
            if(boxVisible)WorkshopMetalHull(iron,box.x-.26f,box.y-.26f,box.x+.26f,box.y+.26f,SquatCorniceTop,2.06f,.04f);
            Create("Boiler iron dome",iron,set.FoundryIron,-10.5f);
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
            Vector2 south=BoilerCentres[3]+new Vector2(.7071f,-.7071f)*BoilerDrum,elbow=new Vector2(33.3f,south.y);
            MassPipe(arena,south,elbow,southFeed,.5f);
            MassPipe(arena,elbow,new Vector2(33.3f,-2.76f),southFeed,.5f);
            using(MassDepth(new Vector4(33f,south.y-.3f,33.6f,south.y+.3f),southFeed+.34f))
            {
                var joint=new Builder();
                WorkshopMetalHull(joint,33f,south.y-.3f,33.6f,south.y+.3f,southFeed-.32f,southFeed+.34f,.05f);
                Create("Boiler feed elbow",joint,arena.PlatformMaterials.FoundryIron,-10.5f);
            }
            Vector2 north=BoilerCentres[1];
            const float feedV=12.72f;
            float ndv=feedV-north.y,ndu=Mathf.Sqrt(Mathf.Max(0,BoilerDrum*BoilerDrum-ndv*ndv));
            MassPipe(arena,new Vector2(north.x+ndu,feedV),new Vector2(32.26f,feedV),northFeed,.5f);
        }

        // An elevated main between design points, with collars and end flanges, as one structure for depth.
        private void MassPipe(ArenaView arena,Vector2 from,Vector2 to,float height,float width)
        {
            using var depth=MassDepth(new Vector4(Mathf.Min(from.x,to.x)-.25f,Mathf.Min(from.y,to.y)-.25f,
                Mathf.Max(from.x,to.x)+.25f,Mathf.Max(from.y,to.y)+.25f),height+width);
            Vector3 lift=Vector3.up*height,a=Project(from.x,from.y)+lift,b=Project(to.x,to.y)+lift;
            Tube(arena,a,b,width,-10.5f);
            Vector3 d=(b-a).normalized,n=new Vector3(-d.y,d.x,0);
            int bands=Mathf.Max(1,Mathf.RoundToInt(Vector3.Distance(a,b)/1.5f));
            for(int i=0;i<=bands;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,i/(float)bands);
                bool end=i==0||i==bands;
                float reach=width*(end?.8f:.64f);
                Tube(arena,p-n*reach,p+n*reach,width*(end?.34f:.2f),-10.5f);
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
            Vector3 floor=Vector3.down*PitDepth,lining=Vector3.down*PitLining;
            if(set.FoundryEmbers!=null)
            {
                var basin=new Builder();
                FullFace(basin,Project(iu0,iv0)+floor,Project(iu1,iv0)+floor,Project(iu1,iv1)+floor,Project(iu0,iv1)+floor,
                    new Color(.95f,.62f,.42f));
                Create("Furnace pit coal bed",basin,set.FoundryEmbers,-17);
            }
            if(set.FurnaceFacade!=null)
            {
                // Only the two far walls face the camera. Painted furnace fronts run down from the deck; a
                // glowing coal course (the facade's own grate) meets the basin.
                var walls=new Builder();
                foreach(var edge in new[]{new[]{new Vector2(iu0,iv1),new Vector2(iu1,iv1)},
                    new[]{new Vector2(iu1,iv1),new Vector2(iu1,iv0)}})
                    for(int panel=0;panel<4;panel++)
                    {
                        Vector2 pa=Vector2.Lerp(edge[0],edge[1],panel/4f),pb=Vector2.Lerp(edge[0],edge[1],(panel+1)/4f);
                        Vector3 a=Project(pa.x,pa.y),b=Project(pb.x,pb.y);
                        FullFace(walls,a+lining,b+lining,b,a,new Color(.84f,.7f,.58f));
                        int start=walls.U.Count;
                        FullFace(walls,a+floor,b+floor,b+lining,a+lining,new Color(1f,.86f,.7f));
                        walls.U[start]=new Vector2(.31f,.16f);walls.U[start+1]=new Vector2(.69f,.16f);
                        walls.U[start+2]=new Vector2(.69f,.26f);walls.U[start+3]=new Vector2(.31f,.26f);
                    }
                // Two inner boilers stand in the pit with their painted fire fronts below the deck.
                foreach(var c in PitBoilers)PaintedDrum(walls,c,1f,-PitDepth,0,8,1,new Color(.92f,.8f,.7f));
                Create("Furnace pit lining and inner boilers",walls,set.FurnaceFacade,-15.5f);
            }
            var wall=set.FoundryWall;
            // Collar: corner pedestals with lanterns, mid posts, and heavy pierced parapets between them.
            foreach(var box in new[]{new Vector4(PitU0,PitV0,PitU0+.9f,PitV0+.9f),new Vector4(PitU1-.9f,PitV0,PitU1,PitV0+.9f),
                new Vector4(PitU0,PitV1-.9f,PitU0+.9f,PitV1),new Vector4(PitU1-.9f,PitV1-.9f,PitU1,PitV1)})
            {
                Vector4 pedestal=box;
                AddPiece(pieces,pedestal,2.8f,()=>LanternPost(arena,pedestal,false));
            }
            foreach(var box in new[]{new Vector4(-8.3f,PitV0,-7.7f,PitV0+.6f),new Vector4(-8.3f,PitV1-.6f,-7.7f,PitV1),
                new Vector4(PitU0,5.7f,PitU0+.6f,6.3f),new Vector4(PitU1-.6f,5.7f,PitU1,6.3f)})
            {
                Vector4 post=box;
                AddPiece(pieces,post,1.34f,()=>{var above=new Builder();RailPost(above,null,post,false);Create("Pit collar post",above,wall,-11);});
            }
            foreach(var span in new[]{
                new Vector4(PitU0+.9f,PitV0,-8.3f,iv0),new Vector4(-7.7f,PitV0,PitU1-.9f,iv0),
                new Vector4(PitU0+.9f,iv1,-8.3f,PitV1),new Vector4(-7.7f,iv1,PitU1-.9f,PitV1)})
            {
                Vector4 box=span;
                AddPiece(pieces,box,RailTop,()=>{var above=new Builder();RailSpan(above,null,box,true,false,false);Create("Pit collar",above,wall,-11);});
            }
            foreach(var span in new[]{
                new Vector4(PitU0,PitV0+.9f,iu0,5.7f),new Vector4(PitU0,6.3f,iu0,PitV1-.9f),
                new Vector4(iu1,PitV0+.9f,PitU1,5.7f),new Vector4(iu1,6.3f,PitU1,PitV1-.9f)})
            {
                Vector4 box=span;
                AddPiece(pieces,box,RailTop,()=>{var above=new Builder();RailSpan(above,null,box,false,false,false);Create("Pit collar",above,wall,-11);});
            }
            // Masonry domes of the inner boilers rise just above the deck, between the far and near collar.
            foreach(var c in PitBoilers)
            {
                Vector2 centre=c;
                AddPiece(pieces,new Vector4(centre.x-1.1f,centre.y-1.1f,centre.x+1.1f,centre.y+1.1f),1.3f,
                    ()=>{var dome=new Builder();PitDome(dome,centre);Create("Pit boiler dome",dome,wall,-11);});
            }
        }

        private static void PitDome(Builder b,Vector2 c)
        {
            // Collar ring over the painted body, a stepped masonry dome and a short open flue.
            MassCourse(b,c,1.1f,1.1f,0,.18f,8,false,new Color(.95f,.95f,.95f));
            MassDisk(b,c,1.1f,.18f,8,false,new Color(.78f,.8f,.9f));
            MassCourse(b,c,.98f,.76f,.18f,.44f,8,false,new Color(1.08f,1.08f,1.08f));
            MassCourse(b,c,.76f,.44f,.44f,.64f,8,false,new Color(1.14f,1.14f,1.14f));
            MassDisk(b,c,.44f,.64f,6,false,new Color(.9f,.92f,1f));
            MassCourse(b,c,.24f,.24f,.64f,1.2f,6,false,new Color(1.05f,1.05f,1.05f));
            MassCourse(b,c,.3f,.3f,1.12f,1.26f,6,false,new Color(1.2f,1.2f,1.2f));
            MassDisk(b,c,.3f,1.26f,6,false,new Color(.85f,.87f,.95f));
            MassDisk(b,c,.19f,1.26f,6,false,new Color(.1f,.09f,.1f));
        }

        // The court posts (finials and four lanterns) stand in the rail line; the rail stops at them. Their stone is emitted
        // here among the rail pieces, their iron with the court borders (FoundryRegionSite.cs).
        private static readonly Vector2[] CourtFinials =
        {
            new Vector2(-18.5f,-6),new Vector2(-18.5f,12),new Vector2(-9,-14.5f),new Vector2(2,-14.5f),new Vector2(10,-14.5f),
            new Vector2(-14,18.5f),new Vector2(0,18.5f),new Vector2(10,18.5f)
        };

        // New lantern posts: the court's convex corners, the apron's concave corner, and the longest bare runs.
        private static readonly Vector4[] RailLanterns =
        {
            new Vector4(14,-14.72f,14.72f,-14),new Vector4(-18.72f,18,-18,18.72f),new Vector4(-18.72f,.64f,-18,1.36f),
            new Vector4(-7.36f,18,-6.64f,18.72f),new Vector4(14,-8.36f,14.72f,-7.64f),new Vector4(14,-2.72f,14.72f,-2),
            new Vector4(15.14f,12,15.86f,12.72f),new Vector4(21.64f,12,22.36f,12.72f)
        };
        private static readonly bool[] RailLanternFront={true,true,true,false,false,true,false,false};

        // Existing exterior structures the rail must not run through.
        private static readonly Vector4[] MassObstacles =
        {
            new Vector4(-21,11,-18,15),new Vector4(-23,5,-18.45f,8.4f),new Vector4(-19,8.4f,-18,10.5f),new Vector4(2,18.5f,7,22.4f),
            new Vector4(19.5f,-2.6f,24.6f,-2),
            new Vector4(32,-3,35,-1),new Vector4(32,11,35,13),
            new Vector4(21.3f,-6.8f,23.3f,-4.8f),new Vector4(21.3f,14.8f,23.3f,16.8f),
            new Vector4(-27.9f,-23.3f,-27.3f,-22.7f),new Vector4(-23.3f,-27.9f,-22.7f,-27.3f),
            new Vector4(-19.1f,-14.4f,-18.5f,-13.8f),new Vector4(-14.4f,-19.1f,-13.8f,-18.5f)
        };

        private void BuildBalustrade(ArenaView arena,List<MassPiece> pieces)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            var placed=new List<Vector4>();
            for(int i=0;i<CourtFinials.Length;i++)
            {
                Vector2 p=CourtFinials[i];
                Vector4 box=new Vector4(p.x-.38f,p.y-.38f,p.x+.38f,p.y+.38f);
                placed.Add(box);
                // The post's own stone is a piece too, so it paints in far-to-near order with the rail: emitted before it,
                // with the court borders, the span leaving its far side painted over its lower front. Its depth is the
                // footprint its landmark registers (FoundryRegionDepth.cs), as for its iron (BuildCourtBorders).
                AddPiece(pieces,new Vector4(p.x-.4f,p.y-.4f,p.x+.4f,p.y+.4f),2.85f,
                    ()=>{var stone=new Builder();CourtFinialMasonry(stone,p);Create("Outer stone finials",stone,wall,-11);});
                // South and west finials face the camera: carry them down the wall as pilasters.
                bool front=p.y<-14||p.x<-18;
                if(front&&!MassBlocked(box))
                    AddPiece(pieces,box,0,()=>{var below=new Builder();MassBox(below,box,DeckFace,0,true,true,.93f);Create("Balustrade pilaster",below,wall,-16);});
            }
            for(int i=0;i<RailLanterns.Length;i++)
            {
                Vector4 box=RailLanterns[i];
                bool front=RailLanternFront[i];
                if(!MassClear(box)||MassBlocked(box)||MassConflicts(box,2.8f,true))continue;
                placed.Add(box);
                AddPiece(pieces,box,2.8f,()=>LanternPost(arena,box,front));
            }
            // Exposed edges from the same cells and neighbour test as the movement map; the pit has its own collar.
            var lines=new Dictionary<(int dir,int line),List<int>>();
            foreach(var cell in _cells)
                for(int dir=0;dir<4;dir++)
                {
                    int nx=cell.x+(dir==1?-1:dir==3?1:0),ny=cell.y+(dir==0?-1:dir==2?1:0);
                    if(FoundryMap.Cell(nx,ny)||InPit(nx+.5f,ny+.5f))continue;
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
                Create("Balustrade rail",above,wall,-11);
                if(below!=null)Create("Balustrade cornice",below,wall,-16);
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
            foreach(var other in placed)if(Overlap(other,box))return false;
            placed.Add(box);posts.Add(Along(box,alongU));
            AddPiece(pieces,box,1.34f,()=>
            {
                var above=new Builder();var below=front?new Builder():null;
                RailPost(above,below,box,front);
                Create("Balustrade post",above,wall,-11);
                if(below!=null)Create("Balustrade pilaster",below,wall,-16);
            });
            return true;
        }

        // The balustrade proper, after the approved layout: a plinth course with a lit ledge, a row of turned balusters
        // with open gaps and a handrail with a lit arris and top. It replaced a solid parapet pierced with pointed
        // openings, which read as a castle wall with arrow slits. The floor, the pit's glow or the abyss shows between the
        // balusters, and there no actor depth is written either: the depth pass covers drawn triangles only. It stands in
        // the camera-facing RailDepth of its footprint band; the rest of the band lies behind the plinth from the camera
        // (through the pit's deeper collar a gap shows a shadow line of it: the top of the far sidewall).
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
            // Plinth course. Its top is a lit ledge up to the balusters' axis, then the shadowed floor of the gaps
            // (elsewhere hidden by the balusters and the handrail).
            CourseBox(above,RailBox(box,alongU,s0,s1,0,RailDepth),0,RailPlinth,faceV,faceU,1.02f,PlinthCourse);
            CourseTop(above,RailBox(box,alongU,s0,s1,0,BalusterAxis),alongU,RailPlinth,
                Tinted(CapTone,new Color(.95f,.95f,.95f)),PlinthCourse);
            CourseTop(above,RailBox(box,alongU,s0,s1,BalusterAxis,RailDepth),alongU,RailPlinth,GapShade,PlinthCourse);
            // Balusters with equal clear gaps, far to near (depth grows along the edge), between solid responds at open
            // ends. Bellies never meet on screen (pitch x .707 > belly), so only the rail courses need painter order.
            float first=s0+(startFace?RailRespond:0),last=s1-(endFace?RailRespond:0),belly=BalusterProfile[2].x*2;
            int count=Mathf.Max(0,Mathf.RoundToInt((last-first-BalusterGap)/(belly+BalusterGap)));
            while(count>0&&(last-first-count*belly)/(count+1)<.12f)count--;
            // Too short for a baluster: a solid dado between plinth and handrail.
            if(count==0)MassBox(above,RailBox(box,alongU,s0,s1,0,RailDepth),RailPlinth,RailHand,faceV,faceU,.98f);
            else
            {
                float gap=(last-first-count*belly)/(count+1);
                if(endFace)MassBox(above,RailBox(box,alongU,last,s1,0,RailDepth),RailPlinth,RailHand,true,true,.98f);
                for(int i=count-1;i>=0;i--)
                {
                    float at=first+gap+belly*.5f+i*(belly+gap);
                    Baluster(above,alongU?Project(at,box.y+BalusterAxis):Project(box.x+BalusterAxis,at),i);
                }
                if(startFace)MassBox(above,RailBox(box,alongU,s0,first,0,RailDepth),RailPlinth,RailHand,true,true,.98f);
            }
            // Handrail: it overhangs toward the void only, never over the walking edge; a lit arris runs along its top.
            Vector4 hand=RailBox(box,alongU,s0,s1,front?-.03f:0,RailDepth+.02f);
            Vector2 arris=new Vector2(HandrailCourse.y-.012f,HandrailCourse.y);
            CourseBox(above,hand,RailHand,RailTop-.03f,faceV,faceU,1.2f,new Vector2(HandrailCourse.x,arris.x));
            CourseBox(above,hand,RailTop-.03f,RailTop,faceV,faceU,1.55f,arris);
            CourseTop(above,hand,alongU,RailTop,Tinted(CapTone,new Color(1.1f,1.1f,1.1f)),HandrailCourse);
            if(below==null)return;
            // The balustrade's own wall drops below the deck, with a projecting corbel course under its edge.
            MassBox(below,box,DeckFace,0,faceV,faceU,.9f);
            MassBox(below,box,-.5f,-.36f,faceV,faceU,.36f);
            Vector4 corbel=alongU?new Vector4(box.x,box.y-.07f,box.z,box.y):new Vector4(box.x-.07f,box.y,box.x,box.w);
            MassBox(below,corbel,-.36f,-.04f,faceV,faceU,1.08f);
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

        private static void RailPost(Builder above,Builder below,Vector4 box,bool front)
        {
            MassBox(above,box,0,1f,true,true,1.02f);
            Vector4 cap=GrowSafe(box,.045f);
            MassBox(above,cap,1f,1.12f,true,true,1.14f);
            MassCap(above,cap,1.12f,CapTone);
            Vector3 apex=Project((box.x+box.z)*.5f,(box.y+box.w)*.5f)+Vector3.up*1.34f,lift=Vector3.up*1.12f;
            above.WallQuad(Project(cap.x,cap.y)+lift,Project(cap.z,cap.y)+lift,apex,apex,false,new Color(.72f,.75f,.84f));
            above.WallQuad(Project(cap.x,cap.w)+lift,Project(cap.x,cap.y)+lift,apex,apex,false,new Color(.5f,.53f,.63f));
            if(front&&below!=null)MassBox(below,box,DeckFace,0,true,true,.93f);
        }

        private void LanternPost(ArenaView arena,Vector4 box,bool front)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            var above=new Builder();
            MassBox(above,box,0,1.48f,true,true,1.02f);
            MassBox(above,box,RailHand,RailTop,true,true,1.16f);
            Vector4 cap=GrowSafe(box,.04f);
            MassBox(above,cap,1.48f,1.69f,true,true,1.14f);
            MassCap(above,cap,1.69f,CapTone);
            Create("Balustrade lantern post",above,wall,-11);
            if(front)
            {
                var below=new Builder();
                MassBox(below,box,DeckFace,0,true,true,.93f);
                Create("Balustrade pilaster",below,wall,-16);
            }
            // The existing court lantern: its iron cage and roof, and panes in the flame batch.
            var iron=new Builder();
            BuildCourtLantern(arena,iron,new Vector2((box.x+box.z)*.5f,(box.y+box.w)*.5f));
            Create("Balustrade lantern ironwork",iron,arena.PlatformMaterials.FoundryIron,-10.5f);
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
            new Vector4(13.2f,-16.6f,15.8f,-14),new Vector4(.7f,-16.6f,3.3f,-14),new Vector4(8.7f,-16.6f,11.3f,-14),
            new Vector4(-20.6f,-7.3f,-18,-4.7f),new Vector4(-20.6f,-.3f,-18,2.3f),new Vector4(-20.6f,15.6f,-18,18.4f)
        };

        private void BuildBastions(ArenaView arena)
        {
            var wall=arena.PlatformMaterials.FoundryWall;
            for(int i=0;i<Bastions.Length;i++)
            {
                Vector4 top=Bastions[i];
                if(!MassClear(top))continue;
                var b=new Builder();
                MassPier(b,top,i<3,PierForms[i]);
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
            AbyssChain(arena,new Vector3(15.2f,-16.6f,-3.4f),0,new Vector2(5.2f,-7.2f),.45f,.36f);
            AbyssChain(arena,new Vector3(-20.6f,16.6f,-3.4f),1,new Vector2(-5.2f,-7.2f),.45f,.36f);
            PierChain(arena,new Vector3(3f,-16.6f,-4f),0,new Vector3(9f,-16.6f,-4f),0,2f,.26f);
            // Across the gap between the two south-west piers of the west rim, and from the nearer one into the void.
            PierChain(arena,new Vector3(-20.6f,-5f,-3.5f),1,new Vector3(-20.6f,.3f,-3.5f),1,1.5f,.3f);
            AbyssChain(arena,new Vector3(-20.6f,-6.8f,-4.3f),1,new Vector2(-6.2f,-9.8f),.7f,.4f);
            // From the far gateposts' outboard chamfers, out of frame on both sides of the bridge's end; each post's
            // stone (-11) hides the anchor link, so the chain comes from behind it.
            AbyssChain(arena,new Vector3(-27.855f,-22.745f,-2.2f),-1,new Vector2(-6.6f,-8.6f),.6f,.4f);
            AbyssChain(arena,new Vector3(-22.745f,-27.855f,-2.2f),-1,new Vector2(6.6f,-8.6f),.6f,.4f);
            // From the anchor plate of the undercroft's major pier right of the bridge head (FoundryRegionUndercroft.cs).
            AbyssChain(arena,new Vector3(-4f,-14f,-3.9f),-1,new Vector2(3f,-10.2f),.5f,.38f);
            // From the gate's south foundation, past the east edge of the frame.
            AbyssChain(arena,new Vector3(33.4f,-3f,-1.6f),0,new Vector2(10f,-11f),.9f,.42f);
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
            new Vector3(-19.5f,-5.2f,0),new Vector3(-17.2f,-4.7f,1),new Vector3(-20.4f,-5.6f,2),
            new Vector3(-18.2f,-5f,1),new Vector3(-20.8f,-5.4f,0),new Vector3(-16.8f,-4.8f,2)
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
            MassBox(b,head,-2.75f,DeckFace,true,true,1.06f);
            MassCap(b,head,DeckFace,new Color(.62f,.65f,.74f));
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
            int stagger=0)
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
                    continue;
                }
                int start=b.U.Count;
                b.Quad(p,q,s,t,false,Tinted(StoneLight(mid),tint));
                b.U[start]=new Vector2(a*r/2.8f,h0/2.4f);b.U[start+1]=new Vector2(e*r/2.8f,h0/2.4f);
                b.U[start+2]=new Vector2(e*r/2.8f,h1/2.4f);b.U[start+3]=new Vector2(a*r/2.8f,h1/2.4f);
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
        private static void MassDisk(Builder b,Vector2 c,float r,float h,int m,bool iron,Color color)
        {
            Vector3 centre=Project(c.x,c.y)+Vector3.up*h;
            for(int k=0;k<2*m;k+=2)
            {
                Vector3 p=OnRing(c,r,FrontAngle(k,m),h),q=OnRing(c,r,FrontAngle(k+1,m),h),s=OnRing(c,r,FrontAngle(k+2,m),h);
                if(iron)FullFace(b,centre,p,q,s,color);else b.Quad(centre,p,q,s,true,color);
            }
        }

        // The two camera-facing faces of a design-plane box: v0 (lit) and u0 (shade), as Block draws them.
        private static void MassBox(Builder b,Vector4 box,float bottom,float top,bool faceV,bool faceU,float tone)
        {
            Vector3 low=Vector3.up*bottom,high=Vector3.up*top;
            var toneColor=new Color(tone,tone,tone);
            if(faceU)
            {
                Vector3 a=Project(box.x,box.w),c=Project(box.x,box.y);
                b.WallQuad(a+low,c+low,c+high,a+high,false,Tinted(ShadeFace,toneColor));
            }
            if(faceV)
            {
                Vector3 a=Project(box.x,box.y),c=Project(box.z,box.y);
                b.WallQuad(a+low,c+low,c+high,a+high,false,Tinted(LitFace,toneColor));
            }
        }

        private static void MassCap(Builder b,Vector4 box,float h,Color color)
        {
            Vector3 lift=Vector3.up*h;
            b.Quad(Project(box.x,box.y)+lift,Project(box.z,box.y)+lift,Project(box.z,box.w)+lift,Project(box.x,box.w)+lift,true,color);
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
                    if(FoundryMap.Contains(x,y,0))return false;
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
