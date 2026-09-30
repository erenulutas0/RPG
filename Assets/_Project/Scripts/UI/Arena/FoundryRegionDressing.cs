using UnityEngine;

namespace Cryptforge.UI
{
    // Static architectural dressing. All footprints stay in the void or existing scenery.
    // Projected parts share the region's material batches and mesh lifetime.
    public sealed partial class FoundryRegionView
    {
        private void BuildDressing(ArenaView arena)
        {
            BuildCrane(arena);
            BuildPipework(arena);
            // The shaft and the tower roofs are rebuilt as the furnace pit and round boilers (FoundryRegionMassing.cs).
            // Mark the bridge mouth and gate with sparse warm beacons; the pit carries its own corner lanterns. The two
            // court-side bridge gateposts carry fire baskets instead (BuildBridgePiers, FoundryRegionSite.cs).
            foreach(var p in new[]{new Vector2(-27.6f,-23),new Vector2(-23,-27.6f),
                new Vector2(32.5f,-2),new Vector2(32.5f,12)})Beacon(arena,p.x,p.y);
            // Heavy exterior suspension links reinforce the floating foundry silhouette.
            foreach(var p in new[]{new Vector2(-18,-8),new Vector2(5,-14),new Vector2(14,15)})
                Chain(arena,Project(p.x,p.y)+Vector3.down*.3f,
                    Project(p.x-1,p.y)+Vector3.down*6.5f,.16f,-16);
        }

        private void BuildCrane(ArenaView arena)
        {
            Block(arena,-21,11,-18,15,1.1f,3,"Crane masonry footing");
            MetalCap(arena,-21,11,-18,15,1.12f);
            Vector3 foot=Project(-19.5f,13)+Vector3.up*1.1f;
            Vector3 mast=foot+Vector3.up*6.5f;
            // Cast mounting shoe and open winch cradle frame the existing winding drum.
            // They stay over the established exterior plinth, not the walking court.
            CraneCase(arena,foot+new Vector3(-.67f,.02f,0),1.34f,.19f,new Vector3(.18f,.12f,0));
            CraneCase(arena,foot+new Vector3(-.63f,.23f,0),.18f,.82f,new Vector3(.15f,.1f,0));
            CraneCase(arena,foot+new Vector3(.45f,.23f,0),.18f,.82f,new Vector3(.15f,.1f,0));
            Tube(arena,foot+new Vector3(-.54f,1.07f,0),foot+new Vector3(.54f,1.07f,0),.14f);
            // Twin tower legs, cross bracing, and a diagonal lattice jib.
            Tube(arena,foot+Vector3.left*.3f,mast+Vector3.left*.3f,.2f);
            Tube(arena,foot+Vector3.right*.3f,mast+Vector3.right*.3f,.2f);
            for(int i=0;i<6;i++)
            {
                Vector3 a=foot+Vector3.up*i;
                Tube(arena,a+Vector3.left*.3f,a+Vector3.up+Vector3.right*.3f,.085f);
                Tube(arena,a+Vector3.right*.3f,a+Vector3.up+Vector3.left*.3f,.085f);
            }
            Vector3 tip=Project(-20,20)+Vector3.up*7.8f;
            Vector3 rear=mast+Vector3.right;
            Truss(arena,rear,tip,.65f);
            Tube(arena,mast,rear,.2f);
            Tube(arena,foot+Vector3.up*3,tip,.095f);
            // Broad bearing collar gives the lattice mast a clear mechanical joint.
            CraneCase(arena,mast+new Vector3(-.48f,-.4f,0),.96f,.23f,new Vector3(.16f,.11f,0));
            Wheel(arena,mast,.32f);
            Vector3 load=tip+Vector3.down*3.4f;
            Chain(arena,tip,load,.13f,-11);
            BuildCraneBasket(arena,load);
            // Counterweight is over the crane foundation, not the playable court.
            CraneCase(arena,rear+new Vector3(-.32f,-.85f,0),.68f,.72f,new Vector3(.22f,.15f,0));
            foreach(float x in new[]{-.2f,.2f})
                Tube(arena,rear+new Vector3(x,.04f,0),rear+new Vector3(x,-.88f,0),.07f);
        }

        private void BuildCraneBasket(ArenaView arena,Vector3 suspension)
        {
            Vector3 front=suspension+new Vector3(-.72f,-.62f,0);
            Vector3 width=Vector3.right*1.25f,depth=new Vector3(.36f,.23f,0),down=Vector3.down*.78f;
            // Back rails, dark solid bottom, then irregular scrap and open front bars.
            // The offset side makes a load-carrying cage instead of a flat hanging grille.
            Tube(arena,front+depth,front+depth+width,.12f);
            Tube(arena,front+depth,front+depth+down,.075f);
            Tube(arena,front+depth+width,front+depth+width+down,.075f);
            var bottom=new Builder();
            FullFace(bottom,front+down,front+width+down,front+width+depth+down,front+depth+down,new Color(.31f,.34f,.4f));
            Create("Crane basket floor",bottom,arena.PlatformMaterials.FoundryIron,-11);
            CraneCase(arena,front+new Vector3(.18f,-.68f,0),.42f,.24f,depth*.48f);
            CraneCase(arena,front+new Vector3(.67f,-.68f,0),.35f,.4f,depth*.5f);
            Tube(arena,front+new Vector3(.28f,-.33f,0),front+new Vector3(.9f,-.12f,0),.15f);
            foreach(Vector3 end in new[]{front,front+width})
            {
                Tube(arena,end,end+depth,.12f);
                Tube(arena,end+down,end+depth+down,.14f);
                Tube(arena,suspension,end,.065f);
            }
            Tube(arena,front,front+width,.14f);
            Tube(arena,front+down,front+width+down,.18f);
            for(int i=0;i<5;i++)
            {
                Vector3 p=front+width*(i/4f);
                Tube(arena,p,p+down,.08f);
            }
        }

        private void CraneCase(ArenaView arena,Vector3 lowerLeft,float width,float height,Vector3 depth)
        {
            Vector3 right=Vector3.right*width,up=Vector3.up*height;
            var body=new Builder();
            FullFace(body,lowerLeft+right,lowerLeft+right+depth,lowerLeft+right+depth+up,
                lowerLeft+right+up,new Color(.27f,.3f,.37f));
            FullFace(body,lowerLeft+up,lowerLeft+right+up,lowerLeft+right+depth+up,
                lowerLeft+depth+up,new Color(.65f,.64f,.6f));
            FullFace(body,lowerLeft,lowerLeft+right,lowerLeft+right+up,lowerLeft+up,new Color(.43f,.45f,.5f));
            Create("Crane cast iron casing",body,arena.PlatformMaterials.FoundryIron,-11);
            Tube(arena,lowerLeft+up,lowerLeft+right+up,.035f);
        }

        private void BuildPipework(ArenaView arena)
        {
            // Exterior reservoirs connect the boiler pairs without narrowing their
            // shared walking apron. Rear equipment is emitted before the main pipes.
            BuildPressureReservoir(arena,22.3f,15.8f,14.5f);
            foreach(float v in new[]{-4.5f,14.5f})
            {
                Vector3 a=Project(19.7f,v)+Vector3.up*1.8f;
                Vector3 b=Project(25.1f,v)+Vector3.up*1.8f;
                Pipe(arena,a,b,.42f);
                // Broad bolted unions distinguish the service mains from the small
                // stored lengths. All parts stay on the existing exterior pipe run.
                PipeUnion(arena,Vector3.Lerp(a,b,.18f),b-a,.42f);
                PipeUnion(arena,Vector3.Lerp(a,b,.82f),b-a,.42f);
                // Small bypass pipe and support pedestals along the void-side service run.
                Pipe(arena,a+Vector3.down*.65f,b+Vector3.down*.65f,.2f);
                foreach(float u in new[]{21f,23.5f})
                {
                    Block(arena,u,v-.35f,u+.6f,v+.35f,.65f,2.8f,"Pipe saddle");
                    Tube(arena,Project(u+.3f,v)+Vector3.up*.6f,
                        Project(u+.3f,v)+Vector3.up*1.8f,.13f);
                }
                Vector3 valve=Vector3.Lerp(a,b,.5f)+Vector3.up*.38f;
                Vector3 hub=Vector3.Lerp(a,b,.5f);
                Tube(arena,hub-(b-a).normalized*.22f,hub+(b-a).normalized*.22f,.62f);
                Tube(arena,valve-Vector3.up*.38f,valve,.09f);Wheel(arena,valve,.28f);
            }
            // The near tank occludes the pipe behind it in the fixed projection.
            BuildPressureReservoir(arena,22.3f,-5.8f,-4.5f);
        }

        private void BuildPressureReservoir(ArenaView arena,float u,float v,float mainV)
        {
            // Both 2x2 feet are entirely outside the immutable map union. A deep
            // masonry plinth supports the tank and its short branch to the main.
            Block(arena,u-1,v-1,u+1,v+1,.25f,3.5f,"Pressure tank service footing");
            var iron=new Builder();
            WorkshopMetalHull(iron,u-.76f,v-.76f,u+.76f,v+.76f,.25f,.43f,.055f);
            // Texture rows per course (the panel is Clamp): the foot takes the bottom rivet strap,
            // the belts take the top strap, plain plate between, ~1.36 world units per panel height.
            ReservoirCourse(iron,u,v,.68f,.82f,.43f,.69f,.01f,.19f);
            ReservoirCourse(iron,u,v,.82f,.82f,.69f,.91f,.19f,.35f);
            // Raised retaining belts follow the round hull, rather than appearing
            // as floating screen-space lines on the visible faces.
            ReservoirCourse(iron,u,v,.85f,.85f,.91f,1.04f,.905f,.99f);
            ReservoirCourse(iron,u,v,.82f,.82f,1.04f,2.13f,.11f,.89f);
            ReservoirCourse(iron,u,v,.85f,.85f,2.13f,2.26f,.905f,.99f);
            ReservoirCourse(iron,u,v,.82f,.82f,2.26f,2.45f,.42f,.56f);
            ReservoirCourse(iron,u,v,.82f,.57f,2.45f,2.77f,.56f,.89f);
            ReservoirCourse(iron,u,v,.57f,.27f,2.77f,2.91f,.3f,.4f);
            WorkshopMetalHull(iron,u-.20f,v-.20f,u+.20f,v+.20f,2.91f,3.04f,.025f);
            Create("Faceted pressure reservoirs",iron,arena.PlatformMaterials.FoundryIron,-11);
            Vector3 branch=Project(u,v+Mathf.Sign(mainV-v)*.82f)+Vector3.up*1.72f;
            Pipe(arena,branch,Project(u,mainV)+Vector3.up*1.72f,.22f);
            // Small top relief stem and wheel give the larger silhouette purpose.
            Vector3 stem=Project(u,v)+Vector3.up*3.04f;
            Tube(arena,stem,stem+Vector3.up*.27f,.095f);
            Wheel(arena,stem+Vector3.up*.31f,.18f);
        }

        private static void ReservoirCourse(Builder mesh,float u,float v,float bottomRadius,
            float topRadius,float bottom,float top,float texBottom=.11f,float texTop=.89f)
        {
            const int sides=12,facetsPerPlate=3;
            Vector3 center=Project(u,v)+Vector3.up*top;
            for(int i=0;i<sides;i++)
            {
                float a=i*Mathf.PI*2/sides,b=(i+1)*Mathf.PI*2/sides;
                Vector3 lowA=Project(u+Mathf.Cos(a)*bottomRadius,v+Mathf.Sin(a)*bottomRadius)+Vector3.up*bottom;
                Vector3 lowB=Project(u+Mathf.Cos(b)*bottomRadius,v+Mathf.Sin(b)*bottomRadius)+Vector3.up*bottom;
                Vector3 highA=Project(u+Mathf.Cos(a)*topRadius,v+Mathf.Sin(a)*topRadius)+Vector3.up*top;
                Vector3 highB=Project(u+Mathf.Cos(b)*topRadius,v+Mathf.Sin(b)*topRadius)+Vector3.up*top;
                // Fixed isometric view sees the near half only. Hidden far faces
                // must not paint across the front in the projected mesh renderer.
                float angle=(a+b)*.5f;
                if(Mathf.Cos(angle)+Mathf.Sin(angle)<0)
                {
                    // One riveted plate per quarter turn wraps continuously across three facets, seams at
                    // 180/270 degrees, so a whole plate faces the camera and the side straps read as butt seams.
                    int start=mesh.U.Count;
                    FullFace(mesh,lowA,lowB,highB,highA,Color.white);
                    float u0=i%facetsPerPlate/(float)facetsPerPlate,u1=(i%facetsPerPlate+1)/(float)facetsPerPlate;
                    mesh.U[start]=new Vector2(u0,texBottom);mesh.U[start+1]=new Vector2(u1,texBottom);
                    mesh.U[start+2]=new Vector2(u1,texTop);mesh.U[start+3]=new Vector2(u0,texTop);
                    // Light per facet edge, not per facet: shading rolls smoothly instead of barrel staves.
                    Color edgeA=ReservoirLight(a),edgeB=ReservoirLight(b);
                    mesh.C[start]=edgeA;mesh.C[start+1]=edgeB;mesh.C[start+2]=edgeB;mesh.C[start+3]=edgeA;
                    ArchitectureBar(mesh,highA,highB,.023f,new Color(.78f,.73f,.62f));
                }
                // Planar cap mapping stays inside the plain plate, clear of the straps and rivets.
                int cap=mesh.U.Count;
                FullFace(mesh,highA,highB,center,center,new Color(.57f,.59f,.64f));
                for(int k=0;k<4;k++)
                {
                    Vector3 offset=mesh.V[cap+k]-center;
                    mesh.U[cap+k]=new Vector2(.5f+offset.x*.45f,.5f+offset.y*.9f);
                }
            }
        }

        private static Color ReservoirLight(float angle)
        {
            float light=.68f+.19f*Mathf.Cos(angle+1.1f);
            return new Color(light,light*.98f,light*.94f);
        }

        private void PipeUnion(ArenaView arena,Vector3 center,Vector3 axis,float diameter)
        {
            Vector3 along=axis.normalized,across=new Vector3(-along.y,along.x,0);
            var flange=new Builder();
            // Two mating plates with a dark gasket between them; highlights and
            // four fasteners read as manufactured iron, without emissive accents.
            foreach(float offset in new[]{-.065f,.065f})
            {
                Vector3 p=center+along*offset;
                ArchitectureBar(flange,p-across*diameter*.83f,p+across*diameter*.83f,.10f,
                    new Color(.53f,.55f,.61f));
                ArchitectureBar(flange,p-across*diameter*.83f-along*.045f,
                    p+across*diameter*.83f-along*.045f,.022f,new Color(.78f,.77f,.71f));
                foreach(float sign in new[]{-1f,1f})
                {
                    Vector3 bolt=p+across*diameter*.64f*sign;
                    ArchitectureBar(flange,bolt-along*.033f,bolt+along*.033f,.065f,
                        new Color(.73f,.62f,.46f));
                }
            }
            Create("Service pipe bolted unions",flange,arena.PlatformMaterials.FoundryIron,-11);
        }

        private void BuildShaft(ArenaView arena)
        {
            if(arena.PlatformMaterials.FoundryEmbers!=null)
            {
                var basin=new Builder();Vector3 drop=Vector3.down*2.3f;
                FullFace(basin,Project(-12,2)+drop,Project(-4,2)+drop,
                    Project(-4,10)+drop,Project(-12,10)+drop,new Color(.55f,.4f,.3f));
                Create("Deep smelting basin",basin,arena.PlatformMaterials.FoundryEmbers,-17);
            }
            // Inset hot industrial elevations are clipped by the surrounding floor in painter order.
            if(arena.PlatformMaterials.FurnaceFacade!=null)
            {
                var lining=new Builder();
                foreach(var edge in new[]{new[]{Project(-11.9f,9.9f),Project(-4.1f,9.9f)},
                    new[]{Project(-4.1f,9.9f),Project(-4.1f,2.1f)}})
                    for(int panel=0;panel<4;panel++)
                    {
                        Vector3 a=Vector3.Lerp(edge[0],edge[1],panel/4f);
                        Vector3 b=Vector3.Lerp(edge[0],edge[1],(panel+1)/4f);
                        FullFace(lining,a+Vector3.down*2.25f,b+Vector3.down*2.25f,
                            b,a,new Color(.78f,.62f,.48f));
                    }
                Create("Shaft heated lining",lining,arena.PlatformMaterials.FurnaceFacade,-15.5f);
            }
            // Two visible hanging rails and braces; all anchors are inside the existing hole.
            foreach(float u in new[]{-10.5f,-5.5f})
            {
                Vector3 a=Project(u,9.8f);
                Tube(arena,a,a+Vector3.down*2.1f,.14f,-15.4f);
                Chain(arena,a+Vector3.left*.28f,a+Vector3.left*.28f+Vector3.down*1.9f,.1f,-15.4f);
            }
            // Low segmented guardrail stays on the inner lip and preserves the walking perimeter.
            foreach(var edge in new[]{new[]{Project(-11.8f,2.2f),Project(-4.2f,2.2f)},
                new[]{Project(-11.8f,2.2f),Project(-11.8f,9.8f)}})
            {
                Tube(arena,edge[0]+Vector3.up*.28f,edge[1]+Vector3.up*.28f,.075f);
                for(int i=0;i<=5;i++)
                {
                    Vector3 p=Vector3.Lerp(edge[0],edge[1],i/5f);
                    Tube(arena,p,p+Vector3.up*.32f,.065f);
                }
            }
        }

        private void MetalCap(ArenaView arena,float u0,float v0,float u1,float v1,float height)
        {
            var b=new Builder();Vector3 lift=Vector3.up*height;
            FullFace(b,Project(u0,v0)+lift,Project(u1,v0)+lift,Project(u1,v1)+lift,
                Project(u0,v1)+lift,new Color(.48f,.49f,.55f));
            // Below the exposed chimney (-12), above masonry caps (-13).
            Create("Iron roof",b,arena.PlatformMaterials.FoundryIron,-12.5f);
        }

        private static void FullFace(Builder b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,Color color)
        {
            int start=b.U.Count;b.Quad(a,c,d,e,false,color);
            b.U[start]=Vector2.zero;b.U[start+1]=Vector2.right;
            b.U[start+2]=Vector2.one;b.U[start+3]=Vector2.up;
        }

        private void Tube(ArenaView arena,Vector3 a,Vector3 b,float width,float order=-11)
        {
            Vector3 n=new Vector3(-(b-a).y,(b-a).x,0).normalized*width*.5f;
            var mesh=new Builder();
            FullFace(mesh,a+n,b+n,b-n,a-n,new Color(.7f,.73f,.8f));
            // Narrow lit edge and deep underside give rods/pipework volume without extra renderers.
            FullFace(mesh,a+n,b+n,b+n*.45f,a+n*.45f,new Color(1f,1f,1f));
            FullFace(mesh,a-n*.55f,b-n*.55f,b-n,a-n,new Color(.28f,.31f,.4f));
            Create("Foundry ironwork",mesh,arena.PlatformMaterials.FoundryIron,order);
        }

        private void Pipe(ArenaView arena,Vector3 a,Vector3 b,float width)
        {
            Tube(arena,a,b,width);
            Vector3 d=(b-a).normalized,n=new Vector3(-d.y,d.x,0);
            int count=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/1.3f));
            for(int i=0;i<=count;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,i/(float)count);
                Tube(arena,p-n*width*.66f,p+n*width*.66f,width*.22f);
            }
        }

        private void Truss(ArenaView arena,Vector3 a,Vector3 b,float depth)
        {
            Vector3 n=new Vector3(-(b-a).y,(b-a).x,0).normalized*depth;
            Tube(arena,a,b,.2f);Tube(arena,a-n,b-n,.17f);
            const int panels=7;
            for(int i=0;i<panels;i++)
            {
                Vector3 p=Vector3.Lerp(a,b,i/(float)panels),q=Vector3.Lerp(a,b,(i+1)/(float)panels);
                Tube(arena,p,q-n,.085f);Tube(arena,p-n,q,.085f);
            }
        }

        private void Wheel(ArenaView arena,Vector3 center,float radius)
        {
            const int segments=12;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                Tube(arena,center+new Vector3(Mathf.Cos(a)*radius,Mathf.Sin(a)*radius,0),
                    center+new Vector3(Mathf.Cos(b)*radius,Mathf.Sin(b)*radius,0),.065f);
            }
            Tube(arena,center+Vector3.left*radius,center+Vector3.right*radius,.07f);
            Tube(arena,center+Vector3.down*radius,center+Vector3.up*radius,.07f);
        }

        // Link parity runs across calls, so catenaries built from short segments keep alternating.
        private int _chainLink;
        private static readonly Vector2 ChainLight=new Vector2(-.6f,.8f);
        // Near-black forged iron (#1E1C22 over the plate centre), warm bronze wear on the lit rim, faint cool ambient rim.
        private static readonly Color ChainIron=new Color(.6f,.54f,.58f),ChainBronze=new Color(2f,1.5f,.95f),
            ChainCool=new Color(.95f,1.05f,1.35f);

        private void Chain(ArenaView arena,Vector3 a,Vector3 b,float width,float order)
        {
            // Heavy forged links 1.5x the run width: a face-on ring alternates with an edge-on bar.
            // Pitch equals the ring's inner length, so neighbours hook through each other.
            float s=width*1.5f,wire=s*.4f,half=s*1.25f,radius=s*.8f;
            float length=Vector3.Distance(a,b);int count=Mathf.CeilToInt(length/(2*(half-wire)));
            if(count==0)return;
            Vector3 direction=(b-a).normalized,normal=new Vector3(-direction.y,direction.x,0);
            var mesh=new Builder();
            // Bar halves toward a pass behind the previous ring, halves toward b cross in front of the next.
            for(int pass=0;pass<3;pass++)
                for(int i=0;i<count;i++)
                {
                    Vector3 p=Vector3.Lerp(a,b,(i+.5f)/count);
                    bool ring=(_chainLink+i)%2==0;
                    if(pass==1&&ring)ChainRing(mesh,p,direction,normal,half,radius,wire,s*.12f);
                    else if(pass!=1&&!ring)ChainBar(mesh,p,pass==0?-direction:direction,normal,half,wire);
                }
            _chainLink+=count;
            Create("Forged chain links",mesh,arena.PlatformMaterials.FoundryIron,order);
        }

        // The same links hung from a to b with a sag (the drop at mid-span, in world y): a parabola sampled finely, the
        // links at one pitch along its length, each on its local tangent, so a long catenary keeps whole links end to end
        // (Chain restarts them on every straight segment). Sag 0 lays them as Chain would. Shaded by depth under
        // the deck line deckA-deckB as the undercroft masonry is, so the lower reach sinks into the void's shade. Always
        // the below-deck chain rank (FoundryIron -15.4, a floor rank: no depth, drawn under the deck and actors).
        private void HangChain(ArenaView arena,Vector3 a,Vector3 b,float sag,float width,Vector3 deckA,Vector3 deckB)
        {
            const int samples=32;
            var path=new Vector3[samples+1];var along=new float[samples+1];
            for(int i=0;i<=samples;i++)
            {
                float t=i/(float)samples;
                path[i]=Vector3.Lerp(a,b,t)+Vector3.down*(4*sag*t*(1-t));
                if(i>0)along[i]=along[i-1]+Vector3.Distance(path[i-1],path[i]);
            }
            float s=width*1.5f,wire=s*.4f,half=s*1.25f,radius=s*.8f,length=along[samples];
            int count=Mathf.CeilToInt(length/(2*(half-wire)));
            if(count==0)return;
            var mesh=new Builder();
            for(int pass=0;pass<3;pass++)
            {
                int k=0;
                for(int i=0;i<count;i++)
                {
                    float at=length*(i+.5f)/count;
                    while(k<samples-1&&along[k+1]<at)k++;
                    Vector3 direction=(path[k+1]-path[k]).normalized,normal=new Vector3(-direction.y,direction.x,0);
                    Vector3 p=Vector3.Lerp(path[k],path[k+1],Mathf.InverseLerp(along[k],along[k+1],at));
                    bool ring=(_chainLink+i)%2==0;
                    if(pass==1&&ring)ChainRing(mesh,p,direction,normal,half,radius,wire,s*.12f);
                    else if(pass!=1&&!ring)ChainBar(mesh,p,pass==0?-direction:direction,normal,half,wire);
                }
            }
            _chainLink+=count;
            ShadeUndercroftDepth(mesh,deckA,deckB);
            Create("Forged chain links",mesh,arena.PlatformMaterials.FoundryIron,-15.4f);
        }

        private static void ChainRing(Builder mesh,Vector3 p,Vector3 direction,Vector3 normal,
            float half,float radius,float wire,float rim)
        {
            // Stadium ring: two concentric half-circles per end, so the wire keeps one thickness.
            const int points=10;
            var outer=new Vector3[points];var edge=new Vector3[points];var inner=new Vector3[points];var lit=new float[points];
            Vector3 straight=direction*(half-radius);
            for(int k=0;k<points;k++)
            {
                float phi=(k<5?-90+k*45:90+(k-5)*45)*Mathf.Deg2Rad;
                Vector3 outward=direction*Mathf.Cos(phi)+normal*Mathf.Sin(phi),c=p+(k<5?straight:-straight);
                outer[k]=c+outward*radius;edge[k]=c+outward*(radius-rim);inner[k]=c+outward*(radius-wire);
                lit[k]=Vector2.Dot(outward,ChainLight);
            }
            for(int k=0;k<points;k++)
            {
                int j=(k+1)%points;
                // The hole edge catches light on the far side from the outer highlight.
                LinkQuad(mesh,edge[k],edge[j],inner[j],inner[k],ChainShade(ChainIron,.85f+.3f*lit[k]),
                    ChainShade(ChainIron,.85f+.3f*lit[j]),ChainShade(ChainIron,.6f+.25f*Mathf.Max(0,-lit[j])),
                    ChainShade(ChainIron,.6f+.25f*Mathf.Max(0,-lit[k])));
                Color rk=ChainRim(lit[k]),rj=ChainRim(lit[j]);
                LinkQuad(mesh,outer[k],outer[j],edge[j],edge[k],rk,rj,rj,rk);
            }
        }

        private static void ChainBar(Builder mesh,Vector3 p,Vector3 toward,Vector3 normal,float half,float wire)
        {
            // Edge-on link: one wire thick, rounded tip, bronze glint along the side facing the light.
            float facing=Vector2.Dot(normal,ChainLight);
            Vector3 side=normal*(facing<0?-.5f:.5f)*wire;
            Vector3 shoulder=p+toward*(half-wire*.5f),tip=p+toward*half;
            Color lit=ChainShade(ChainIron,1.05f+.2f*Mathf.Abs(facing)),dark=ChainShade(ChainIron,.62f);
            LinkQuad(mesh,p+side,shoulder+side,shoulder-side,p-side,lit,lit,dark,dark);
            LinkQuad(mesh,shoulder+side,tip+side*.45f,tip-side*.45f,shoulder-side,lit,lit,dark,dark);
            Color glint=Color.Lerp(ChainIron,ChainBronze,.35f+.65f*Mathf.Abs(facing));
            LinkQuad(mesh,p+side,shoulder+side,shoulder+side*.4f,p+side*.4f,glint,glint,glint,glint);
        }

        private static Color ChainRim(float lit)
        {
            Color iron=ChainShade(ChainIron,.85f);
            return lit>0?Color.Lerp(iron,ChainBronze,lit):Color.Lerp(iron,ChainCool,-lit*.4f);
        }

        private static Color ChainShade(Color color,float k){return new Color(color.r*k,color.g*k,color.b*k,1);}

        private static void LinkQuad(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,
            Color ca,Color cb,Color cc,Color cd)
        {
            int start=mesh.U.Count;mesh.Quad(a,b,c,d,false,ca);
            // A small flat patch of the plate centre keeps thin links free of rivet and strap noise.
            mesh.U[start]=new Vector2(.46f,.46f);mesh.U[start+1]=new Vector2(.54f,.46f);
            mesh.U[start+2]=new Vector2(.54f,.54f);mesh.U[start+3]=new Vector2(.46f,.54f);
            mesh.C[start+1]=cb;mesh.C[start+2]=cc;mesh.C[start+3]=cd;
        }

        private void Beacon(ArenaView arena,float u,float v)
        {
            // Slender scenery posts inside already non-walkable bridge sides, hole or gate footing.
            Vector3 p=Project(u,v);
            if(u+v<-30)p+=Vector3.up*1.65f; // Bridge beacons sit on the new stone piers.
            Tube(arena,p,p+Vector3.up*.95f,.16f);
            Pipe(arena,p+Vector3.up*.75f+Vector3.left*.18f,p+Vector3.up*.75f+Vector3.right*.18f,.1f);
            // A shallow iron cup and three prongs replace the unsupported gem-like flame.
            Tube(arena,p+Vector3.up*.8f+Vector3.left*.18f,p+Vector3.up*.69f,.07f);
            Tube(arena,p+Vector3.up*.69f,p+Vector3.up*.8f+Vector3.right*.18f,.07f);
            foreach(float side in new[]{-.16f,0f,.16f})
                Tube(arena,p+new Vector3(side,.76f,0),p+new Vector3(side*1.18f,1.03f,0),.045f);
            if(arena.PlatformMaterials.FoundryEmbers==null)return;
            var light=new Builder();
            ForgeFlame(light,p+Vector3.up*1.04f,1,0);
            // Sample a hot seam from the unchanged source instead of shrinking the whole basin.
            HotSeam(light,0);
            Create("Contained forge beacon",light,arena.PlatformMaterials.FoundryEmbers,-10);
        }

        // The beacon's flame: an outer tongue, an inner one and a white-hot core (quad outlines about the flame's centre,
        // bottom-up), in the hot-seam tones. Scale grows it about the centre; lean tips it sideways in proportion to height.
        private static readonly Vector2[] FlameOutline=
        {
            new Vector2(-.13f,-.2f),new Vector2(.14f,-.16f),new Vector2(.07f,.17f),new Vector2(-.02f,.36f),
            new Vector2(-.11f,-.17f),new Vector2(.025f,-.12f),new Vector2(-.04f,.08f),new Vector2(-.15f,.14f),
            new Vector2(-.05f,-.16f),new Vector2(.055f,-.16f),new Vector2(.025f,.08f),new Vector2(0,.18f)
        };
        private static readonly Color[] FlameTone={new Color(1,.85f,.6f),new Color(1,.65f,.35f),new Color(1.5f,1.3f,1)};

        private static void ForgeFlame(Builder light,Vector3 center,float scale,float lean)
        {
            for(int q=0;q<FlameTone.Length;q++)
            {
                var corner=new Vector3[4];
                for(int k=0;k<4;k++)
                {
                    Vector2 o=FlameOutline[q*4+k];
                    corner[k]=center+new Vector3((o.x+lean*(o.y+.2f))*scale,o.y*scale,0);
                }
                FullFace(light,corner[0],corner[1],corner[2],corner[3],FlameTone[q]);
            }
        }

        // Maps full-face UVs from vertex `first` on into the ember texture's hot seam: the flames, the court lanterns'
        // panes and the boilers' fire windows all sample it.
        private static void HotSeam(Builder light,int first)
        {
            for(int i=first;i<light.U.Count;i++)light.U[i]=new Vector2(.776f+light.U[i].x*.012f,.838f+light.U[i].y*.016f);
        }

        // The bridge head's fire baskets, after the approved layout's braziers: the beacon's iron and flame at the size of
        // a brazier. A cast foot and stem on the gatepost's crown (rise), a flared bowl under a bronze rim whose far side
        // shows past the coals heaped just inside it, and three tongues of the beacon's flame, the tallest in the middle.
        // Existing iron (-10.5) and flame (-10) batches; the caller scopes the depth.
        private void Brazier(ArenaView arena,Vector2 c,float rise)
        {
            var set=arena.PlatformMaterials;
            var iron=new Builder();
            MassCourse(iron,c,.22f,.11f,rise-.1f,rise+.08f,6,true,new Color(.78f,.78f,.82f));
            MassCourse(iron,c,.08f,.08f,rise+.08f,rise+.28f,6,true,Color.white);
            MassCourse(iron,c,.13f,.13f,rise+.15f,rise+.21f,6,true,Bronze);
            MassCourse(iron,c,.14f,.42f,rise+.28f,rise+.54f,8,true,new Color(1.05f,1.05f,1.08f));
            MassCourse(iron,c,.44f,.44f,rise+.52f,rise+.6f,8,true,Bronze);
            // The rim's flat top all the way round. The coals fill its inner edge exactly, front and back, so the fire,
            // sorted above the iron, never paints over the near rim and no gap opens at the far one.
            const int ring=12;
            for(int k=0;k<ring;k++)
            {
                float a=k*Mathf.PI*2/ring,e=(k+1)*Mathf.PI*2/ring;
                Color near=Tinted(Bronze,IronLight(a)*.85f),far=Tinted(Bronze,IronLight(e)*.85f);
                LinkQuad(iron,OnRing(c,.39f,a,rise+.6f),OnRing(c,.44f,a,rise+.6f),OnRing(c,.44f,e,rise+.6f),
                    OnRing(c,.39f,e,rise+.6f),near,near,far,far);
            }
            Create("Bridge brazier ironwork",iron,set.FoundryIron,-10.5f);
            if(set.FoundryEmbers==null)return;
            // Coals: a low mound of the ember texture's cracked crust round its hot seam, then the flames standing in it.
            var fire=new Builder();
            Vector3 heap=Project(c.x,c.y)+Vector3.up*(rise+.68f);
            for(int k=0;k<ring;k+=2)
            {
                float a=k*Mathf.PI*2/ring;
                FullFace(fire,heap,OnRing(c,.39f,a,rise+.6f),OnRing(c,.39f,a+Mathf.PI*2/ring,rise+.6f),
                    OnRing(c,.39f,a+Mathf.PI*4/ring,rise+.6f),new Color(1.12f,.9f,.76f));
            }
            // A round patch (+/-.027) of the texture across the ellipse, whose screen half-height is half its radius.
            for(int i=0;i<fire.V.Count;i++)
            {
                Vector3 offset=fire.V[i]-heap;
                fire.U[i]=new Vector2(.782f+offset.x*.07f,.846f+(offset.y+.08f)*.14f);
            }
            int flames=fire.V.Count;
            ForgeFlame(fire,heap+new Vector3(-.18f,.1f,0),1f,-.3f);
            ForgeFlame(fire,heap+new Vector3(.19f,.08f,0),1.05f,.28f);
            ForgeFlame(fire,heap+new Vector3(0,.27f,0),1.55f,0);
            HotSeam(fire,flames);
            Create("Bridge brazier fire",fire,set.FoundryEmbers,-10);
        }
    }
}
