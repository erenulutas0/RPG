using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Outside the north walking boundary and clear of the existing rim finial.
        internal static Vector4 CastingShelf=>new Vector4(-18f,19.1f,-13f,22.4f);
        private Material _castingBenchMaterial;

        private void BuildCastingWorkbay(ArenaView arena)
        {
            Block(arena,CastingShelf.x,CastingShelf.y,CastingShelf.z,CastingShelf.w,
                .18f,2.8f,"Casting service shelf");
            // Below-deck cantilevers join the workbay to the real court wall. The
            // north rail stays intact: this is scenery, not an unwalkable new route.
            var supports=new Builder();
            foreach(float u in new[]{-17.4f,-14.3f})
            {
                Vector3 a=Project(u,17.8f),b=Project(u+.7f,17.8f);
                Vector3 c=Project(u+.7f,19.5f),d=Project(u,19.5f);
                Vector3 top=Vector3.down*.32f,bottom=Vector3.down*2.5f;
                supports.WallQuad(a+bottom,b+bottom,b+top,a+top,false,new Color(.49f,.51f,.58f));
                supports.WallQuad(d+bottom,a+bottom,a+top,d+top,false,new Color(.40f,.43f,.51f));
                supports.WallQuad(a+top,b+top,c+top,d+top,true,new Color(.59f,.59f,.63f));
            }
            Create("Casting shelf cantilevers",supports,arena.PlatformMaterials.FoundryWall,-16);
            if(BuildBakedCastingBench())return;

            // Bounded fallback shares the same scenic footprint and existing material.
            int owner=RegisterDepthFootprint(CastingShelf.x,CastingShelf.y,CastingShelf.z,CastingShelf.w,2.5f,true,false);
            using(new DepthScope(this,owner))
            {
                var bench=new Builder();
                foreach(float u in new[]{-17.3f,-14.2f})
                    WorkshopMetalHull(bench,u,19.7f,u+.25f,21.6f,.18f,1.15f,.025f,true);
                WorkshopMetalHull(bench,-17.5f,19.5f,-13.8f,21.8f,1.05f,1.3f,.05f,true);
                foreach(float u in new[]{-16.9f,-15.9f,-14.9f})
                    WorkshopMetalHull(bench,u,20f,u+.65f,21.1f,1.3f,1.48f,.055f,true);
                Create("Casting bench fallback",bench,arena.PlatformMaterials.FoundryIron,-12);
            }
        }

        private bool BuildBakedCastingBench()
        {
            var texture=Resources.Load<Texture2D>("FoundryCastingBench");
            if(texture==null)return false;
            _castingBenchMaterial=new Material(Shader.Find("Sprites/Default"))
                {name="Authored foundry casting bench",mainTexture=texture};
            int owner=RegisterDepthFootprint(CastingShelf.x,CastingShelf.y,CastingShelf.z,CastingShelf.w,2.5f,true,false);
            using(new DepthScope(this,owner))
            {
                // Projection contract is retained beside the editable Blender source.
                Vector3 origin=Project(-15.5f,20.75f);
                var card=new Builder();
                card.V.AddRange(new[]{origin+new Vector3(-3.5f,-2.5f,0),origin+new Vector3(3.5f,-2.5f,0),
                    origin+new Vector3(3.5f,4.5f,0),origin+new Vector3(-3.5f,4.5f,0)});
                card.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                for(int i=0;i<4;i++)card.C.Add(Color.white);
                card.T.AddRange(new[]{0,1,2,0,2,3});
                Create("Baked casting workbench",card,_castingBenchMaterial,-12);
            }
            return true;
        }
    }
}
