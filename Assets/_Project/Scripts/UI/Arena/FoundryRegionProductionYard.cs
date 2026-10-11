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
                    Create("Workyard island "+island,kit,arena.PlatformMaterials.FoundryIron,-11);
                }
            }
        }

        private static void BuildYardTrolley(Builder kit,float u0,float v0,float u1,float v1)
        {
            // Far wheels precede the frame; the near pair is emitted last.
            foreach(float u in new[]{u0+.62f,u1-.62f})YardWheel(kit,u,v1-.18f,.38f,.28f);
            // Through axles meet both wheel hubs; bearing blocks connect the deck.
            foreach(float u in new[]{u0+.62f,u1-.62f})
            {
                WorkshopMetalHull(kit,u-.06f,v0+.14f,u+.06f,v1-.14f,.33f,.43f,.012f,true);
                foreach(float v in new[]{v0+.28f,v1-.43f})
                    WorkshopMetalHull(kit,u-.13f,v,u+.13f,v+.15f,.32f,.48f,.025f,true);
            }
            WorkshopMetalHull(kit,u0+.16f,v0+.25f,u1-.16f,v1-.25f,.42f,.60f,.035f,true);
            foreach(float v in new[]{v0+.38f,v1-.51f})
                WorkshopMetalHull(kit,u0+.25f,v,u1-.25f,v+.13f,.26f,.43f,.02f,true);
            // Three staggered courses of long billets make one broad freight load.
            for(int level=0;level<3;level++)
                for(int row=0;row<3-level;row++)
                {
                    float v=v0+.48f+row*.41f+level*.20f;
                    int first=kit.C.Count;
                    WorkshopMetalHull(kit,u0+.46f,v,u1-.54f,v+.33f,.60f+level*.20f,.79f+level*.20f,.055f,true);
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
            // Short end handles remain within the skid and below the cargo skyline.
            foreach(float u in new[]{u0+.10f,u1-.22f})
            {
                foreach(float v in new[]{v0+.40f,v1-.52f})
                    WorkshopMetalHull(kit,u,v,u+.12f,v+.12f,.60f,.91f,.012f,true);
                WorkshopMetalHull(kit,u,v0+.40f,u+.12f,v1-.40f,.86f,.94f,.015f,true);
            }
            foreach(float u in new[]{u0+.62f,u1-.62f})YardWheel(kit,u,v0+.18f,.38f,.28f);
            // Broad corner fasteners are part of the cart frame, not floor clutter.
            foreach(float u in new[]{u0+.27f,u1-.35f})
                foreach(float v in new[]{v0+.29f,v1-.37f})
                    WorkshopMetalHull(kit,u,v,u+.08f,v+.08f,.60f,.65f,.02f,true);
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
            // Four legs and lower ties leave visible air beneath the work surface.
            foreach(float u in new[]{u0+.16f,u1-.36f})
            {
                foreach(float v in new[]{v0+.18f,v1-.38f})
                    WorkshopMetalHull(kit,u,v,u+.20f,v+.20f,.10f,1.05f,.025f,true);
                WorkshopMetalHull(kit,u,v0+.20f,u+.20f,v1-.20f,.30f,.43f,.02f,true);
            }
            // Three hardwood working planks sit on the iron frame. Reuse the iron
            // grain's unframed field, but separate the timber from cool hardware.
            for(int plank=0;plank<3;plank++)
            {
                float start=v0+.10f+plank*(v1-v0-.20f)/3;
                float end=v0+.10f+(plank+1)*(v1-v0-.20f)/3-.016f;
                int first=kit.C.Count;
                WorkshopMetalHull(kit,u0+.10f,start,u1-.10f,end,.99f,1.13f,.025f,true);
                for(int c=first;c<kit.C.Count;c++)
                {
                    Color tone=kit.C[c];tone.r*=1.32f;tone.g*=.91f;tone.b*=.56f;
                    kit.C[c]=tone;
                }
            }
            // Two unequal open moulds, rather than another stack of closed boxes.
            YardMould(kit,u0+.32f,v0+.31f,u0+1.42f,v1-.27f,1.13f,1.40f);
            YardMould(kit,u0+1.70f,v0+.40f,u1-.30f,v1-.47f,1.13f,1.32f);
            // Two long-flask clamps have supported jaws and a visible screw crossbar.
            foreach(float v in new[]{v0+.55f,v1-.58f})
            {
                WorkshopMetalHull(kit,u0+.24f,v,u0+.34f,v+.14f,1.13f,1.48f,.015f,true);
                WorkshopMetalHull(kit,u0+.24f,v,u0+.52f,v+.14f,1.43f,1.49f,.015f,true);
                WorkshopMetalHull(kit,u0+.43f,v+.035f,u0+.49f,v+.105f,1.39f,1.52f,.01f,true);
                WorkshopMetalHull(kit,u0+.37f,v+.035f,u0+.55f,v+.105f,1.50f,1.54f,.01f,true);
            }
            // The small flask uses one broad end dog, making the setups asymmetric.
            WorkshopMetalHull(kit,u1-.57f,v0+.25f,u1-.37f,v0+.43f,1.13f,1.34f,.025f,true);
            WorkshopMetalHull(kit,u0+1.53f,v0+.26f,u0+1.61f,v1-.30f,1.13f,1.22f,.015f,true);
        }

        private static void YardMould(Builder kit,float u0,float v0,float u1,float v1,float bottom,float top)
        {
            WorkshopMetalHull(kit,u0,v0,u1,v1,bottom,bottom+.055f,.025f,true);
            // Dark recessed casting sand remains below all four supported frame rails.
            var sand=new Color(.80f,.57f,.34f);
            FullFace(kit,Project(u0+.11f,v0+.11f)+Vector3.up*(bottom+.06f),
                Project(u1-.11f,v0+.11f)+Vector3.up*(bottom+.06f),
                Project(u1-.11f,v1-.11f)+Vector3.up*(bottom+.06f),
                Project(u0+.11f,v1-.11f)+Vector3.up*(bottom+.06f),sand);
            // A seated ingot pattern and narrow feed channel explain the cavity's
            // casting purpose; both stay well below the open rim.
            float middle=(u0+u1)*.5f;
            WorkshopMetalHull(kit,middle-.19f,v0+.29f,middle+.19f,v1-.29f,bottom+.06f,bottom+.095f,.07f,true);
            WorkshopMetalHull(kit,middle-.05f,v0+.12f,middle+.05f,v0+.31f,bottom+.06f,bottom+.09f,.012f,true);
            WorkshopMetalHull(kit,u0,v1-.12f,u1,v1,bottom+.055f,top,.02f,true);
            WorkshopMetalHull(kit,u1-.12f,v0+.12f,u1,v1-.12f,bottom+.055f,top,.02f,true);
            WorkshopMetalHull(kit,u0,v0,u1,v0+.12f,bottom+.055f,top,.02f,true);
            WorkshopMetalHull(kit,u0,v0+.12f,u0+.12f,v1-.12f,bottom+.055f,top,.02f,true);
            // Lift handles join the front/rear flask rails; all projections stay
            // within the enclosing worktop and shared collision footprint.
            foreach(float edge in new[]{v0-.075f,v1-.02f})
            {
                foreach(float u in new[]{middle-.24f,middle+.17f})
                    WorkshopMetalHull(kit,u,edge,u+.07f,edge+.095f,top-.12f,top-.055f,.012f,true);
                WorkshopMetalHull(kit,middle-.24f,edge,middle+.24f,edge+.04f,top-.09f,top-.03f,.01f,true);
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
