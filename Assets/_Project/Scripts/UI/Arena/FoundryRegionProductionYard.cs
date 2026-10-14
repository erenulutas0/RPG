using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // An embedded freight-transfer bay ties the salvage edge to the north stock
        // station. All pieces are flush paving/inlays, never sleepers, a raised cart,
        // a collision obstacle or a new interaction. Ordinary movement crosses them.
        private void BuildProductionYard(ArenaView arena)
        {
            var bed=new Builder();
            const float shift=Cryptforge.Art.FoundryMap.WorkyardShift;
            Vector2 centre=new Vector2(-24+shift,7);
            var plate=new Color(.73f,.75f,.80f,.52f);
            var recess=new Color(.59f,.60f,.65f,.72f);
            var rail=new Color(.59f,.55f,.46f,.86f);

            // The working lane keeps a stone floor between its rails. Broad shoulders
            // at the loading edge distinguish this approach from the ceremonial brass.
            YardPanel(bed,new Vector2(Cryptforge.Art.FoundryMap.West+.35f,6.02f),new Vector2(-27.55f+shift,7.98f),plate);
            YardPanel(bed,new Vector2(-24.98f+shift,10.50f),new Vector2(-23.02f+shift,17.65f),
                new Color(.77f,.78f,.82f,.30f));

            const int segments=48;
            const float inner=3.22f,outer=3.7f;
            for(int i=0;i<segments;i++)
            {
                float a=i*Mathf.PI*2/segments,b=(i+1)*Mathf.PI*2/segments;
                // Radial iron bed is deliberately plain: no compass rays, magical
                // glyph, luminous perimeter or closed black disc under the actors.
                SealQuad(bed,centre+SealPolar(a,inner),centre+SealPolar(b,inner),
                    centre+SealPolar(b,outer),centre+SealPolar(a,outer),plate,SealStone);
                Vector2 p=centre+SealPolar(a,3.48f),q=centre+SealPolar(b,3.48f);
                YardRail(bed,p,q,recess,rail,i);
                if(i%6==0)
                {
                    p=centre+SealPolar(a,inner);q=centre+SealPolar(a,outer);
                    Strip(bed,p.x,p.y,q.x,q.y,.065f,recess,CopingTrim.Stone,i);
                }
            }

            // A long transfer table inset into the circular bed: six broad plates,
            // with a straight through-track that can be understood at portrait scale.
            for(int i=0;i<6;i++)
            {
                float u=-27.43f+shift+i*(6.86f/6);
                YardPanel(bed,new Vector2(u+.025f,6.08f),new Vector2(u+6.86f/6-.025f,7.92f),
                    new Color(.62f,.66f,.73f,.63f));
            }
            // Match the two rail gauges at their radial junctions. The north approach
            // ends in a flush service plate before the existing non-walkable shelf.
            foreach(float v in new[]{6.4f,7.6f})
                YardRail(bed,new Vector2(Cryptforge.Art.FoundryMap.West+.35f,v),new Vector2(-20.57f+shift,v),recess,rail,3);
            foreach(float u in new[]{-24.6f+shift,-23.4f+shift})
                YardRail(bed,new Vector2(u,10.43f),new Vector2(u,17.42f),recess,rail,7);
            YardPanel(bed,new Vector2(-24.98f+shift,17.42f),new Vector2(-23.02f+shift,17.68f),plate);
            // Flat pivot cover; its small rectangular outline is mechanical rather
            // than another decorated medallion competing with the central court seal.
            YardPanel(bed,centre-new Vector2(.34f,.26f),centre+new Vector2(.34f,.26f),
                new Color(.7f,.68f,.63f,.6f));
            // Flush freight rails connect the new loading trolley to the turntable.
            // The trolley itself owns a shared blocked footprint, not these rails.
            foreach(float u in new[]{-24.6f+shift,-23.4f+shift})
                YardRail(bed,new Vector2(u,-1.8f),new Vector2(u,3.56f),recess,rail,11);
            Create("Embedded freight transfer yard",bed,arena.PlatformMaterials.Coping,FloorDecal);
            BuildWorkIslands(arena);
        }

        private void BuildWorkIslands(ArenaView arena)
        {
            for(int island=0;island<Cryptforge.Art.FoundryMap.WorkIslandCount;island++)
            {
                float u0=Cryptforge.Art.FoundryMap.WorkIslandBound(island,0);
                float v0=Cryptforge.Art.FoundryMap.WorkIslandBound(island,1);
                float u1=Cryptforge.Art.FoundryMap.WorkIslandBound(island,2);
                float v1=Cryptforge.Art.FoundryMap.WorkIslandBound(island,3);
                // The shallow iron skid defines the complete collision footprint.
                // Floor paving continues beneath it, and all loads stay inside it.
                using(DepthStructure(u0,v0,u1,v1,1.55f))
                {
                    var kit=new Builder();
                    WorkshopMetalHull(kit,u0,v0,u1,v1,.015f,.10f,.035f,true);
                    if(island==0)BuildYardTrolley(kit,u0,v0,u1,v1);
                    else BuildYardMouldBench(kit,u0,v0,u1,v1);
                    Create("Workyard island "+island,kit,island==0?arena.PlatformMaterials.FoundryCastIron:
                        arena.PlatformMaterials.FoundryIron,-11);
                }
            }
        }

        private static void BuildYardTrolley(Builder kit,float u0,float v0,float u1,float v1)
        {
            // Far wheels precede the frame; the near pair is emitted last.
            foreach(float u in new[]{u0+.62f,u1-.62f})YardWheel(kit,u,v1-.18f,.38f,.28f);
            // Through axles meet both wheel hubs; substantial split bearing saddles
            // carry the deck instead of leaving the wheels glued to a thin slab.
            foreach(float u in new[]{u0+.62f,u1-.62f})
            {
                WorkshopMetalHull(kit,u-.06f,v0+.14f,u+.06f,v1-.14f,.33f,.43f,.012f,true);
                foreach(float v in new[]{v0+.23f,v1-.49f})
                {
                    WorkshopMetalHull(kit,u-.22f,v,u+.22f,v+.26f,.30f,.49f,.035f,true);
                    WorkshopMetalHull(kit,u-.17f,v+.015f,u+.17f,v+.245f,.49f,.62f,.04f,true);
                }
            }
            WorkshopMetalHull(kit,u0+.16f,v0+.25f,u1-.16f,v1-.25f,.42f,.60f,.035f,true);
            foreach(float v in new[]{v0+.38f,v1-.51f})
                WorkshopMetalHull(kit,u0+.25f,v,u1-.25f,v+.13f,.26f,.43f,.02f,true);
            // Positive design-u recedes in the fixed camera. The far end casting
            // must precede the freight so its legs cannot paint over the billet ends.
            YardTrolleyYoke(kit,u1-.30f,v0,v1);
            // Three staggered courses of long billets make one broad freight load.
            for(int level=0;level<3;level++)
                for(int row=0;row<3-level;row++)
                {
                    float v=v0+.48f+row*.41f+level*.20f;
                    int first=kit.C.Count;
                    // Unequal sheared ends distinguish the six real billets without
                    // breaking either of the two common load-band contact stations.
                    float left=u0+.43f+((row+level)%3)*.07f;
                    float right=u1-.45f-((row*2+level)%3)*.065f;
                    WorkshopMetalHull(kit,left,v,right,v+.33f,.60f+level*.20f,.79f+level*.20f,.055f,true);
                    // Warm mill scale distinguishes raw stock from the cool cart.
                    // Variation belongs to whole billets, not a noisy per-vertex wash.
                    for(int c=first;c<kit.C.Count;c++)
                    {
                        Color tone=kit.C[c];
                        tone.r*=1.23f+row*.04f;tone.g*=1.03f;tone.b*=.75f+level*.025f;
                        kit.C[c]=tone;
                    }
                }
            // Folded bands follow all three cargo courses and terminate at deck
            // buckles. Each run connects to the next rather than floating above it.
            foreach(float u in new[]{u0+.85f,u1-.90f})
            {
                var profile=new[]{new Vector2(.44f,.58f),new Vector2(.44f,.805f),
                    new Vector2(.665f,.805f),new Vector2(.665f,1.005f),
                    new Vector2(.865f,1.005f),new Vector2(.865f,1.205f),
                    new Vector2(1.225f,1.205f),new Vector2(1.225f,1.005f),
                    new Vector2(1.435f,1.005f),new Vector2(1.435f,.805f),
                    new Vector2(1.645f,.805f),new Vector2(1.645f,.58f)};
                for(int i=0;i<profile.Length-1;i++)
                {
                    Vector3 a=Project(u,v0+profile[i].x)+Vector3.up*profile[i].y;
                    Vector3 b=Project(u,v0+profile[i+1].x)+Vector3.up*profile[i+1].y;
                    Vector3 width=Project(u+.12f,v0)-Project(u,v0);
                    Vector3 thickness=Mathf.Abs(profile[i+1].y-profile[i].y)>.01f
                        ?Project(u,v0+.025f)-Project(u,v0):Vector3.down*.025f;
                    FullFace(kit,a,b,b+width,a+width,new Color(.60f,.64f,.69f));
                    FullFace(kit,a+thickness,b+thickness,b,a,new Color(.28f,.32f,.39f));
                }
                WorkshopMetalHull(kit,u-.025f,v0+.395f,u+.145f,v0+.47f,.66f,.84f,.02f,true);
                WorkshopMetalHull(kit,u+.035f,v0+.37f,u+.085f,v0+.425f,.715f,.785f,.009f,true);
            }
            // Cast end yokes retain the freight and provide a handhold. Broad legs,
            // sloped haunches and a connected crown read as one casting, with clear
            // air around the billet ends rather than another cage of narrow ribs.
            YardTrolleyYoke(kit,u0+.10f,v0,v1);
            foreach(float u in new[]{u0+.62f,u1-.62f})YardWheel(kit,u,v0+.18f,.38f,.28f);
            // Broad corner fasteners are part of the cart frame, not floor clutter.
            foreach(float u in new[]{u0+.27f,u1-.35f})
                foreach(float v in new[]{v0+.29f,v1-.37f})
                    WorkshopMetalHull(kit,u,v,u+.08f,v+.08f,.60f,.65f,.02f,true);
            // The complete trolley, including the skid already in this builder,
            // uses one jointless cast field. Existing stock/frame light and all
            // geometry remain; bands no longer repeat the framed global atlas.
            YardTrolleyGrain(kit,Project((u0+u1)*.5f,(v0+v1)*.5f)+Vector3.up*.63f);
        }

        private static void YardTrolleyGrain(Builder kit,Vector3 origin)
        {
            // Same projected grain density as the formed pit hoods. No clamp or
            // per-piece reset can stretch grain into a seam or collapse a face.
            for(int i=0;i<kit.V.Count;i++)
            {
                Vector3 p=kit.V[i]-origin;
                kit.U[i]=new Vector2(.5f+p.x*.19f,.5f+p.y*.19f);
            }
        }

        private static void YardTrolleyYoke(Builder kit,float u,float v0,float v1)
        {
            foreach(float v in new[]{v0+.28f,v1-.56f})
                WorkshopMetalHull(kit,u,v,u+.20f,v+.28f,.51f,1.10f,.04f,true);
            WorkshopMetalHull(kit,u,v0+.51f,u+.20f,v1-.51f,1.15f,1.33f,.035f,true);
            YardTrestleGusset(kit,u,u+.20f,v0+.28f,v0+.70f,1.02f,1.33f);
            YardTrestleGusset(kit,u,u+.20f,v1-.28f,v1-.70f,1.02f,1.33f);
        }

        // A solid triangular casting in the v/height plane. The far triangle goes
        // first and the near face last, retaining the fixed camera's painter order.
        private static void YardTrestleGusset(Builder kit,float u0,float u1,float footV,float innerV,float low,float high)
        {
            Vector3[] a={Project(u0,footV)+Vector3.up*low,Project(u0,innerV)+Vector3.up*high,
                Project(u0,footV)+Vector3.up*high};
            Vector3 across=Project(u1,footV)-Project(u0,footV);
            int first=kit.V.Count;
            FullFace(kit,a[0]+across,a[1]+across,a[2]+across,a[2]+across,new Color(.28f,.32f,.39f));
            kit.T.RemoveRange(kit.T.Count-3,3);
            WorkedStockFace(kit,first,a[0],1.4f,low,high);
            for(int edge=0;edge<3;edge++)
            {
                int next=(edge+1)%3;first=kit.V.Count;
                FullFace(kit,a[edge],a[next],a[next]+across,a[edge]+across,new Color(.43f,.48f,.56f));
                WorkedStockFace(kit,first,a[0],1.45f,low,high);
            }
            first=kit.V.Count;
            FullFace(kit,a[0],a[1],a[2],a[2],new Color(.32f,.36f,.44f));
            kit.T.RemoveRange(kit.T.Count-3,3);
            WorkedStockFace(kit,first,a[0],1.45f,low,high);
        }

        private static void YardWheel(Builder kit,float u,float v,float height,float radius)
        {
            Vector3 centre=Project(u,v)+Vector3.up*height;
            Vector3 horizontal=(Project(u+1,v)-Project(u,v));
            // A cast wheel sits in a design-u/height plane. Disc, rim and hub are
            // original native geometry, using the existing unframed iron grain.
            for(int i=0;i<12;i++)
            {
                float a=i*Mathf.PI/6,b=(i+1)*Mathf.PI/6;
                Vector3 p=horizontal*(Mathf.Cos(a)*radius)+Vector3.up*(Mathf.Sin(a)*radius);
                Vector3 q=horizontal*(Mathf.Cos(b)*radius)+Vector3.up*(Mathf.Sin(b)*radius);
                int first=kit.V.Count;
                FullFace(kit,centre,centre+p*.74f,centre+q*.74f,centre,new Color(.24f,.27f,.33f));
                kit.T.RemoveRange(kit.T.Count-3,3); // Fan is one triangle, not a degenerate quad.
                FullFace(kit,centre+p*.74f,centre+p,centre+q,centre+q*.74f,new Color(.50f,.55f,.63f));
                WorkedStockFace(kit,first,centre,1.3f,height-radius,height+radius);
                WorkedStockFace(kit,first+4,centre,1.3f,height-radius,height+radius);
            }
            // Four cast ribs give each wheel a manufactured centre at portrait size.
            for(int spoke=0;spoke<4;spoke++)
            {
                float angle=spoke*Mathf.PI*.5f;
                Vector3 radial=horizontal*Mathf.Cos(angle)+Vector3.up*Mathf.Sin(angle);
                Vector3 tangent=(horizontal*-Mathf.Sin(angle)+Vector3.up*Mathf.Cos(angle))*.033f;
                FullFace(kit,centre+radial*.07f-tangent,centre+radial*(radius*.73f)-tangent,
                    centre+radial*(radius*.73f)+tangent,centre+radial*.07f+tangent,new Color(.46f,.50f,.57f));
            }
            WorkshopMetalHull(kit,u-.08f,v-.055f,u+.08f,v+.055f,height-.08f,height+.08f,.015f,true);
        }

        private static void BuildYardMouldBench(Builder kit,float u0,float v0,float u1,float v1)
        {
            Vector3 origin=Project((u0+u1)*.5f,(v0+v1)*.5f)+Vector3.up*.84f;
            int first=kit.V.Count;
            // The receiving shelf carries two open spare flasks. Their broad
            // stacked rails replace the old closed box, preserving clear air
            // through the tooling instead of filling the bench with solid clutter.
            WorkshopMetalHull(kit,u0+.24f,v0+.30f,u1-.24f,v1-.30f,.40f,.49f,.025f,true);
            YardStoredFlask(kit,u0+.44f,v0+.44f,u0+1.38f,v1-.42f,.49f,.63f);
            YardStoredFlask(kit,u0+.475f,v0+.475f,u0+1.345f,v1-.455f,.63f,.77f);
            YardBenchIronFinish(kit,first,kit.V.Count,origin);
            // Two thick packing boards are visibly supported by the same shelf,
            // kept under the right-hand flask, never on the walking floor.
            first=kit.V.Count;
            WorkshopMetalHull(kit,u0+1.52f,v0+.52f,u1-.42f,v1-.53f,.49f,.60f,.025f,true);
            WorkshopMetalHull(kit,u0+1.62f,v0+.57f,u1-.47f,v1-.58f,.60f,.71f,.025f,true);
            YardBenchTimberFinish(kit,first,kit.V.Count,origin);
            // Raked cast legs meet the worktop directly. Their broad shoulders
            // replace the old square posts and separate triangular gussets.
            first=kit.V.Count;
            YardMouldingBenchFrame(kit,u0,v0,u1,v1);
            YardBenchIronFinish(kit,first,kit.V.Count,origin);
            // Three hardwood working planks sit on the iron frame. Reuse the iron
            // grain's unframed field with one long, unbroken timber direction.
            first=kit.V.Count;
            for(int plank=0;plank<3;plank++)
            {
                float start=v0+.10f+plank*(v1-v0-.20f)/3;
                float end=v0+.10f+(plank+1)*(v1-v0-.20f)/3-.016f;
                WorkshopMetalHull(kit,u0+.10f,start,u1-.10f,end,.99f,1.13f,.025f,true);
            }
            YardBenchTimberFinish(kit,first,kit.V.Count,origin);
            // Oak flasks and actual recessed sand faces match the neighbouring
            // authored casting bench while retaining the two existing setups.
            YardWorkingMould(kit,u0+.32f,v0+.31f,u0+1.42f,v1-.27f,1.13f,1.40f,false,origin);
            YardWorkingMould(kit,u0+1.70f,v0+.40f,u1-.30f,v1-.47f,1.13f,1.32f,true,origin);
            // Two long-flask clamps have supported jaws and a visible screw crossbar.
            first=kit.V.Count;
            foreach(float v in new[]{v0+.55f,v1-.58f})
            {
                YardMouldClamp(kit,u0+.24f,v);
            }
            // The small flask uses one broad end dog, making the setups asymmetric.
            YardMouldHardwareBox(kit,u1-.57f,v0+.25f,u1-.37f,v0+.43f,1.13f,1.34f);
            YardMouldHardwareBox(kit,u0+1.53f,v0+.26f,u0+1.61f,v1-.30f,1.13f,1.22f);
            YardBenchIronFinish(kit,first,kit.V.Count,origin);
        }

        private static void YardStoredFlask(Builder kit,float u0,float v0,float u1,float v1,float bottom,float top)
        {
            const float rail=.10f;
            WorkshopMetalHull(kit,u0,v1-rail,u1,v1,bottom,top,.015f,true);
            WorkshopMetalHull(kit,u1-rail,v0+rail,u1,v1-rail,bottom,top,.015f,true);
            WorkshopMetalHull(kit,u0,v0,u1,v0+rail,bottom,top,.015f,true);
            WorkshopMetalHull(kit,u0,v0+rail,u0+rail,v1-rail,bottom,top,.015f,true);
        }

        private static void YardBenchIronFinish(Builder kit,int first,int end,Vector3 origin)
        {
            for(int i=first;i<end;i++)
            {
                Vector3 p=kit.V[i]-origin;
                kit.U[i]=new Vector2(.5f+p.x*.105f,.5f+p.y*.105f);
                Color old=kit.C[i];
                float age=.89f+.035f*FloorPatina(p.x*.62f+4.1f,p.y*.62f-1.7f);
                float foot=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.64f,-.15f,p.y));
                float shoulder=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.60f,p.y));
                // Quiet dark worked iron links the transfer bench to its crane;
                // retain directional luminance and the original fire-facing bias.
                kit.C[i]=WorkyardCastTone(old,age*(1-foot*.12f+shoulder*.045f));
            }
        }

        private static Color WorkyardCastTone(Color source,float finish)
        {
            // The native bench and winch share neutral cool iron beside the baked
            // equipment. Preserve source face light and warm reflections; worn
            // shoulders separate from mill scale without lifting every dark face.
            float value=source.r*.299f+source.g*.587f+source.b*.114f;
            float wear=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.38f,.95f,value));
            float gain=finish*Mathf.Lerp(.94f,1.12f,wear);
            float warmth=Mathf.Max(0,source.r-source.b)*.28f;
            return new Color((value*.995f+warmth)*gain,value*1.005f*gain,
                (value*1.025f-warmth*.14f)*gain,1);
        }

        private static void YardBenchTimberFinish(Builder kit,int first,int end,Vector3 origin)
        {
            Vector2 along=((Vector2)(Project(1,0)-Project(0,0))).normalized;
            Vector2 across=new Vector2(-along.y,along.x);
            for(int i=first;i<end;i++)
            {
                Vector2 p=kit.V[i]-origin;
                float length=Vector2.Dot(p,along),width=Vector2.Dot(p,across);
                // The existing plain field is stretched along the real plank axis;
                // shared projected contacts keep the same grain without clamping.
                kit.U[i]=new Vector2(.5f+length*.055f,.5f+width*.18f);
                Color old=kit.C[i];float value=old.r*.299f+old.g*.587f+old.b*.114f;
                float age=.92f+.06f*FloorPatina(length*.38f+2.3f,width*.65f);
                float rubbed=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.10f,.45f,p.y));
                // Desaturated hardwood agrees with the authored casting bench;
                // the upper working planks are rubbed, stored packing stays dull.
                float finish=age*(1+rubbed*.09f);
                kit.C[i]=new Color(value*1.18f*finish,value*.96f*finish,value*.68f*finish,1);
            }
        }

        private static void YardPanel(Builder mesh,Vector2 min,Vector2 max,Color tone)
        {
            // Subdivide long plates for the same local floor-light sampling as inlays.
            int alongU=Mathf.Max(1,Mathf.CeilToInt(max.x-min.x));
            int alongV=Mathf.Max(1,Mathf.CeilToInt(max.y-min.y));
            for(int u=0;u<alongU;u++)for(int v=0;v<alongV;v++)
            {
                float u0=Mathf.Lerp(min.x,max.x,u/(float)alongU),u1=Mathf.Lerp(min.x,max.x,(u+1f)/alongU);
                float v0=Mathf.Lerp(min.y,max.y,v/(float)alongV),v1=Mathf.Lerp(min.y,max.y,(v+1f)/alongV);
                SealQuad(mesh,new Vector2(u0,v0),new Vector2(u1,v0),new Vector2(u1,v1),new Vector2(u0,v1),tone,SealStone);
            }
        }

        private static void YardRail(Builder mesh,Vector2 a,Vector2 b,Color groove,Color rail,int seed)
        {
            Strip(mesh,a.x,a.y,b.x,b.y,.23f,groove,CopingTrim.Stone,seed);
            Strip(mesh,a.x,a.y,b.x,b.y,.09f,rail,CopingTrim.Brass,seed);
        }
    }
}
