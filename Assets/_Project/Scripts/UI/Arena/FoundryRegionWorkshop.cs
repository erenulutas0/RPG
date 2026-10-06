using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Two small exterior service shelves explain the crane's industrial purpose.
        // Their complete design-plane footprints are outside the shared walking union.
        private void BuildWorkshop(ArenaView arena)
        {
            // Extend only toward the exterior void for the working anvil station;
            // the court-facing edge, stock and parts bin stay in their old places.
            Block(arena,-23f,5,-18.45f,8.4f,.18f,2.8f,"Salvage service shelf");
            // The north extension is masonry-backed all the way to its outer edge;
            // its court-facing edge and the original pipe/tool positions do not move.
            Block(arena,2,18.5f,7,22.4f,.18f,2.8f,"Pipe service shelf");

            var ingots=new Builder();
            // Staggered courses read as metal stock, with sloped shoulders and small gaps.
            for(int level=0;level<2;level++)
                for(int row=0;row<3-level;row++)
                    WorkshopMetalHull(ingots,-20.35f+row*.51f+level*.24f,5.35f,
                        -19.91f+row*.51f+level*.24f,6.85f,.2f+level*.22f,.41f+level*.22f,.055f);
            Create("Stacked forge ingots",ingots,arena.PlatformMaterials.FoundryIron,-11);

            WorkshopCrate(arena,-20.3f,7.15f,-18.85f,8.05f,.2f,.92f);
            BuildWorkshopAnvil(arena);
            // Emit the rear work station before the foreground pipe/rack silhouettes.
            BuildQuenchingBasin(arena);
            BuildWorkshopReserveStock(arena);
            BuildStoredPipes(arena);
            BuildToolRack(arena);
        }

        private void BuildQuenchingBasin(ArenaView arena)
        {
            var basin=new Builder();
            // Two low skids carry an open iron tank; no emissive water or effect.
            foreach(float u in new[]{5.4f,6.4f})
                WorkshopMetalHull(basin,u,20.75f,u+.28f,22.05f,.18f,.32f,.02f);
            // Far walls first, then a recessed opaque liquid plane, then near walls.
            // This is painter-ordered geometry in the existing static iron batch.
            WorkshopMetalHull(basin,5.3f,21.94f,6.8f,22.1f,.32f,.85f,.025f);
            WorkshopMetalHull(basin,6.64f,20.7f,6.8f,21.94f,.32f,.85f,.025f);
            Color inner=new Color(.16f,.19f,.23f);
            FullFace(basin,Project(5.46f,21.94f)+Vector3.up*.58f,
                Project(6.64f,21.94f)+Vector3.up*.58f,
                Project(6.64f,21.94f)+Vector3.up*.82f,
                Project(5.46f,21.94f)+Vector3.up*.82f,inner);
            FullFace(basin,Project(6.64f,20.86f)+Vector3.up*.58f,
                Project(6.64f,21.94f)+Vector3.up*.58f,
                Project(6.64f,21.94f)+Vector3.up*.82f,
                Project(6.64f,20.86f)+Vector3.up*.82f,inner);
            FullFace(basin,Project(5.46f,20.86f)+Vector3.up*.58f,
                Project(6.64f,20.86f)+Vector3.up*.58f,
                Project(6.64f,21.94f)+Vector3.up*.58f,
                Project(5.46f,21.94f)+Vector3.up*.58f,new Color(.09f,.13f,.17f));
            WorkshopMetalHull(basin,5.3f,20.7f,6.8f,20.86f,.32f,.85f,.025f);
            WorkshopMetalHull(basin,5.3f,20.86f,5.46f,21.94f,.32f,.85f,.025f);
            // A short dull reflection makes the dark inset legible without sparkle.
            FullFace(basin,Project(5.66f,21.44f)+Vector3.up*.583f,
                Project(6.06f,21.44f)+Vector3.up*.583f,
                Project(6.06f,21.48f)+Vector3.up*.583f,
                Project(5.66f,21.48f)+Vector3.up*.583f,new Color(.2f,.24f,.28f));
            // Broad welded ribs belong to the tank walls, not loose foreground trim.
            // Their design-plane extents remain inside the existing tank footprint.
            foreach(float u in new[]{5.48f,6.48f})
                WorkshopMetalHull(basin,u,20.7f,u+.12f,20.76f,.34f,.8f,.012f);
            foreach(float v in new[]{21.02f,21.65f})
                WorkshopMetalHull(basin,5.3f,v,5.36f,v+.12f,.34f,.8f,.012f);
            Create("Open workshop quenching tank",basin,arena.PlatformMaterials.FoundryIron,-11);
        }

        private void BuildWorkshopReserveStock(ArenaView arena)
        {
            var stock=new Builder();
            // A low billet bundle and two flat wound-strip coils occupy only the
            // supported rear shelf; the original pipe rack remains the foreground.
            foreach(float v in new[]{20.86f,21.7f})
                WorkshopMetalHull(stock,2.5f,v,3.4f,v+.16f,.18f,.29f,.015f);
            for(int level=0;level<3;level++)
                for(int column=0;column<2;column++)
                    WorkshopMetalHull(stock,2.56f+column*.4f,20.78f,
                        2.9f+column*.4f,21.98f,.29f+level*.17f,.44f+level*.17f,.025f);
            const int segments=16;
            for(int layer=0;layer<2;layer++)
            {
                float bottom=.18f+layer*.16f,top=bottom+.14f;
                for(int segment=0;segment<segments;segment++)
                {
                    float a=segment*Mathf.PI*2/segments,b=(segment+1)*Mathf.PI*2/segments;
                    Vector3 outerA=Project(4.03f+Mathf.Cos(a)*.52f,21.35f+Mathf.Sin(a)*.52f);
                    Vector3 outerB=Project(4.03f+Mathf.Cos(b)*.52f,21.35f+Mathf.Sin(b)*.52f);
                    Vector3 innerA=Project(4.03f+Mathf.Cos(a)*.23f,21.35f+Mathf.Sin(a)*.23f);
                    Vector3 innerB=Project(4.03f+Mathf.Cos(b)*.23f,21.35f+Mathf.Sin(b)*.23f);
                    FullFace(stock,outerA+Vector3.up*bottom,outerB+Vector3.up*bottom,
                        outerB+Vector3.up*top,outerA+Vector3.up*top,new Color(.27f,.31f,.37f));
                    FullFace(stock,innerA+Vector3.up*bottom,innerB+Vector3.up*bottom,
                        innerB+Vector3.up*top,innerA+Vector3.up*top,new Color(.14f,.17f,.22f));
                    FullFace(stock,outerA+Vector3.up*top,outerB+Vector3.up*top,
                        innerB+Vector3.up*top,innerA+Vector3.up*top,new Color(.49f,.52f,.56f));
                }
            }
            Create("Workshop reserve billets and strip coils",stock,arena.PlatformMaterials.FoundryIron,-11);
        }

        private void BuildWorkshopAnvil(ArenaView arena)
        {
            // Every design-plane point is on the supported exterior shelf. The
            // widened foot, narrow waist, flat heel and projecting horn form one tool.
            var support=new Builder();
            ArchitecturalTier(support,-22.5f,5.65f,-21.2f,7.55f,.18f,.48f,.06f);
            Create("Anvil stone bedding",support,arena.PlatformMaterials.FoundryWall,-11);
            var metal=new Builder();
            WorkshopMetalHull(metal,-22.32f,5.86f,-21.38f,7.32f,.48f,.63f,.055f);
            Vector3[] lower={Project(-22.19f,6.05f),Project(-21.51f,6.05f),
                Project(-21.51f,7.13f),Project(-22.19f,7.13f)};
            Vector3[] waist={Project(-22.06f,6.28f),Project(-21.64f,6.28f),
                Project(-21.64f,6.92f),Project(-22.06f,6.92f)};
            Vector3[] shoulder={Project(-22.25f,5.8f),Project(-21.45f,5.8f),
                Project(-21.45f,7.15f),Project(-22.25f,7.15f)};
            foreach(int edge in new[]{0,3})
            {
                int next=(edge+1)%4;
                Color tint=edge==0?new Color(.43f,.47f,.54f):new Color(.28f,.32f,.4f);
                FullFace(metal,lower[edge]+Vector3.up*.63f,lower[next]+Vector3.up*.63f,
                    waist[next]+Vector3.up*.85f,waist[edge]+Vector3.up*.85f,tint);
                FullFace(metal,waist[edge]+Vector3.up*.85f,waist[next]+Vector3.up*.85f,
                    shoulder[next]+Vector3.up*1.08f,shoulder[edge]+Vector3.up*1.08f,tint);
            }
            // The anvil is one forged casting, not a set of framed metal panels.
            // Keep its foot and waist darker than the worked head, using neutral grain.
            WorkedAnvilSurface(metal,0,metal.V.Count,Project(-22.25f,5.58f),1.6f);
            int headStart=metal.C.Count;
            WorkshopMetalHull(metal,-22.25f,5.58f,-21.45f,7.15f,1.08f,1.28f,.025f);
            WorkedAnvilSurface(metal,headStart,metal.V.Count,Project(-22.25f,5.58f),2f);
            // Only the working face and its near worn shoulder catch cool steel light.
            // Recolour existing vertices; stock, waist and supporting foot stay dark.
            for(int i=headStart+8;i<headStart+12;i++)metal.C[i]=new Color(1.7f,1.72f,1.74f);
            // A worked steel face has no fabricated panel frame. Sample the same
            // neutral iron patch as small props, retaining a subtle forged grain.
            for(int i=headStart+24;i<headStart+28;i++)
            {
                metal.C[i]=new Color(1.55f,1.6f,1.62f);
            }
            int hornStart=metal.V.Count;
            Vector3 hornLeft=Project(-22.2f,7.15f),hornRight=Project(-21.5f,7.15f);
            Vector3 tip=Project(-21.85f,8.03f)+Vector3.up*1.24f;
            FullFace(metal,hornLeft+Vector3.up*1.28f,hornRight+Vector3.up*1.28f,
                tip,tip,new Color(1.55f,1.6f,1.62f));
            FullFace(metal,hornLeft+Vector3.up*1.08f,hornLeft+Vector3.up*1.28f,
                tip,tip,new Color(.62f,.72f,.86f));
            // One dark hardy-hole mark breaks up the flat heel without emissive detail.
            FullFace(metal,Project(-21.99f,5.77f)+Vector3.up*1.284f,
                Project(-21.78f,5.77f)+Vector3.up*1.284f,
                Project(-21.78f,5.98f)+Vector3.up*1.284f,
                Project(-21.99f,5.98f)+Vector3.up*1.284f,new Color(.1f,.12f,.17f));
            WorkedAnvilSurface(metal,hornStart,metal.V.Count,Project(-22.25f,5.58f),1f);
            Create("Workshop anvil",metal,arena.PlatformMaterials.FoundryIron,-11);
            // A single hammer rests on the face. Its complete silhouette is supported
            // by the anvil; do not scatter small loose tools across the walking court.
            Vector3 handle=Project(-21.81f,6.19f)+Vector3.up*1.31f;
            Vector3 head=Project(-21.95f,6.78f)+Vector3.up*1.31f;
            var tool=new Builder();
            ArchitectureBar(tool,handle,head,.055f,new Color(.5f,.37f,.23f));
            WorkshopMetalHull(tool,-22.13f,6.64f,-21.73f,6.87f,1.29f,1.43f,.025f);
            WorkedAnvilSurface(tool,0,tool.V.Count,handle,1.6f);
            Create("Anvil resting hammer",tool,arena.PlatformMaterials.FoundryIron,-11);
        }

        private static void WorkedAnvilSurface(Builder mesh,int first,int end,Vector3 anchor,float gain)
        {
            // Local UVs avoid the atlas border/rivets on solid worked metal. Sampling
            // one continuous patch also keeps the head-to-horn transition coherent.
            for(int i=first;i<end;i++)
            {
                Vector3 grain=mesh.V[i]-anchor;
                mesh.U[i]=IronPatch+new Vector2(grain.x,grain.y)*.012f;
                Color colour=mesh.C[i];
                mesh.C[i]=new Color(colour.r*gain,colour.g*gain,colour.b*gain,colour.a);
            }
        }

        private void WorkshopCrate(ArenaView arena,float u0,float v0,float u1,float v1,float bottom,float top)
        {
            var body=new Builder();
            WorkshopMetalHull(body,u0,v0,u1,v1,bottom,top,.035f);
            // Recessed front plates make a steel parts bin, not another masonry block.
            FullFace(body,Project(u0+.12f,v0-.004f)+Vector3.up*(bottom+.13f),
                Project(u1-.12f,v0-.004f)+Vector3.up*(bottom+.13f),
                Project(u1-.12f,v0-.004f)+Vector3.up*(top-.12f),
                Project(u0+.12f,v0-.004f)+Vector3.up*(top-.12f),new Color(.33f,.36f,.43f));
            Create("Workshop parts bin",body,arena.PlatformMaterials.FoundryIron,-11);
            // Straps follow the near face and lid; short dull brass latches distinguish storage.
            foreach(float u in new[]{u0+.27f,u1-.27f})
            {
                Tube(arena,Project(u,v0-.012f)+Vector3.up*bottom,
                    Project(u,v0-.012f)+Vector3.up*top,.065f);
                Tube(arena,Project(u,v0)+Vector3.up*(top+.015f),
                    Project(u,v1)+Vector3.up*(top+.015f),.065f);
            }
            var latch=new Builder();
            Vector3 p=Project((u0+u1)*.5f,v0-.018f)+Vector3.up*(top-.2f);
            FullFace(latch,p+new Vector3(-.065f,-.08f,0),p+new Vector3(.065f,-.08f,0),
                p+new Vector3(.065f,.08f,0),p+new Vector3(-.065f,.08f,0),new Color(.8f,.65f,.4f));
            Create("Parts bin latch",latch,arena.PlatformMaterials.FoundryIron,-11);
        }

        private void BuildStoredPipes(ArenaView arena)
        {
            var saddles=new Builder();
            foreach(float u in new[]{2.5f,4.55f})
                WorkshopMetalHull(saddles,u,18.78f,u+.25f,20.03f,.18f,.43f,.015f);
            Create("Stored pipe saddles",saddles,arena.PlatformMaterials.FoundryIron,-11);
            // Three horizontal lengths, deliberately separate from the operational furnace pipes.
            foreach(float v in new[]{19f,19.4f,19.8f})
            {
                Vector3 a=Project(2.3f,v)+Vector3.up*.58f;
                Vector3 b=Project(5f,v)+Vector3.up*.58f;
                Tube(arena,a,b,.19f);
                // Dark open bore and thin top edge at the visible end.
                Vector3 normal=new Vector3(-(b-a).y,(b-a).x,0).normalized;
                var ends=new Builder();
                Vector3 c=a-(b-a).normalized*.018f;
                FullFace(ends,c+normal*.075f,c+(b-a).normalized*.025f,
                    c-normal*.075f,c-(b-a).normalized*.025f,new Color(.08f,.09f,.13f));
                Create("Stored pipe open ends",ends,arena.PlatformMaterials.FoundryIron,-11);
            }
            foreach(float u in new[]{2.7f,4.6f})
                Tube(arena,Project(u,18.94f)+Vector3.up*.7f,
                    Project(u,19.86f)+Vector3.up*.7f,.055f);
        }

        private void BuildToolRack(ArenaView arena)
        {
            // The paired handles and spread jaws distinguish tongs from the central
            // hanging hammer. All tools remain inside the original rack footprint.
            Vector3 left=Project(5.65f,19.8f),right=Project(6.65f,19.8f);
            Tube(arena,left+Vector3.up*.2f,left+Vector3.up*1.3f,.07f);
            Tube(arena,right+Vector3.up*.2f,right+Vector3.up*1.3f,.07f);
            Tube(arena,left+Vector3.up*1.24f,right+Vector3.up*1.24f,.09f);
            foreach(float u in new[]{5.9f,6.35f})
            {
                Vector3 handle=Project(u,19.78f)+Vector3.up*1.2f;
                Vector3 joint=handle+Vector3.down*.63f;
                Tube(arena,handle+Vector3.left*.04f,joint+Vector3.right*.018f,.035f);
                Tube(arena,handle+Vector3.right*.04f,joint+Vector3.left*.018f,.035f);
                Tube(arena,joint,joint+new Vector3(-.085f,-.16f,0),.04f);
                Tube(arena,joint,joint+new Vector3(.085f,-.16f,0),.04f);
                Tube(arena,joint+new Vector3(-.085f,-.16f,0),
                    joint+new Vector3(-.045f,-.19f,0),.035f);
                Tube(arena,joint+new Vector3(.085f,-.16f,0),
                    joint+new Vector3(.045f,-.19f,0),.035f);
            }
            var hammer=new Builder();
            ArchitectureBar(hammer,Project(6.12f,19.8f)+Vector3.up*1.23f,
                Project(6.12f,19.8f)+Vector3.up*.73f,.055f,new Color(.48f,.34f,.21f));
            WorkshopMetalHull(hammer,5.97f,19.7f,6.27f,19.9f,.62f,.78f,.022f);
            Create("Rack hanging forge hammer",hammer,arena.PlatformMaterials.FoundryIron,-11);
            foreach(float u in new[]{5.65f,6.65f})
                Tube(arena,Project(u,19.5f)+Vector3.up*.2f,
                    Project(u,20.05f)+Vector3.up*.2f,.08f);
        }

        private static void WorkshopMetalHull(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float bevel)
        {
            // Iron is a clamped material: map one complete face per quad rather than
            // masonry's repeating wall UVs, which sample a uniform texture edge here.
            float shoulder=top-Mathf.Min(.045f,(top-bottom)*.2f);
            var lower=new[]{Project(u0,v0),Project(u1,v0),Project(u1,v1),Project(u0,v1)};
            var inset=new[]{Project(u0+bevel,v0+bevel),Project(u1-bevel,v0+bevel),
                Project(u1-bevel,v1-bevel),Project(u0+bevel,v1-bevel)};
            foreach(int edge in new[]{0,3})
            {
                int next=(edge+1)%4;
                Color face=edge==0?new Color(.4f,.43f,.49f):new Color(.27f,.3f,.37f);
                FullFace(mesh,lower[edge]+Vector3.up*bottom,lower[next]+Vector3.up*bottom,
                    lower[next]+Vector3.up*shoulder,lower[edge]+Vector3.up*shoulder,face);
            }
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                Color rim=edge==0?new Color(.69f,.71f,.75f):new Color(.46f,.5f,.57f);
                FullFace(mesh,lower[edge]+Vector3.up*shoulder,lower[next]+Vector3.up*shoulder,
                    inset[next]+Vector3.up*top,inset[edge]+Vector3.up*top,rim);
            }
            FullFace(mesh,inset[0]+Vector3.up*top,inset[1]+Vector3.up*top,
                inset[2]+Vector3.up*top,inset[3]+Vector3.up*top,new Color(.53f,.57f,.64f));
        }
    }
}
