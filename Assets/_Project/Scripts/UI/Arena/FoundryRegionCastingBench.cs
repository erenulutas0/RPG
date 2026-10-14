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
            BuildServiceShelf(arena,CastingShelf,.18f,2.8f,"Casting service shelf");
            // Below-deck cantilevers join the workbay to the real court wall. The
            // north rail stays intact: this is scenery, not an unwalkable new route.
            var supports=new Builder();
            CastingShelfBearings(supports);
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

        private static void CastingShelfBearings(Builder supports)
        {
            // A fitted bearing shoe and tapered haunch replace the old solid box.
            // The same attachment strip17.8..19.5 and height-2.5..-.32 remain;
            // every vertex stays below the deck and behind the unchanged rail.
            foreach(float u in new[]{-17.4f,-14.3f})
            {
                int first=supports.V.Count;
                Vector3 a=Project(u,17.8f),b=Project(u+.7f,17.8f);
                Vector3 c=Project(u+.7f,19.5f),d=Project(u,19.5f);
                Vector3 lowA=Project(u+.10f,17.8f)+Vector3.down*2.5f;
                Vector3 lowB=Project(u+.60f,17.8f)+Vector3.down*2.5f;
                Vector3 lowC=Project(u+.60f,18.35f)+Vector3.down*2.5f;
                Vector3 lowD=Project(u+.10f,18.35f)+Vector3.down*2.5f;
                Vector3 seat=Vector3.down*.68f,top=Vector3.down*.32f;
                var foot=new[]{lowA,lowB,lowC,lowD};var head=new[]{a+seat,b+seat,c+seat,d+seat};
                float[] heights={-2.5f,-1.55f,-1.08f,-.68f};float[] reach={0,.28f,.58f,1};
                Vector3 At(int corner,int row)
                {
                    if(row==0)return foot[corner];
                    if(row==3)return head[corner];
                    Vector3 p=Vector3.Lerp(foot[corner]+Vector3.up*2.5f,head[corner]-seat,reach[row]);
                    return p+Vector3.up*heights[row];
                }
                // Broad knees carry the existing shoe. All intermediate stations
                // stay inside the old attachment volume, with exact foot/head
                // contacts and no additional wall underneath the raked faces.
                for(int row=0;row<3;row++)foreach(int edge in new[]{0,3,1})
                {
                    int next=(edge+1)%4;
                    Color tone=edge==0?new Color(.53f,.56f,.63f):edge==3?
                        new Color(.39f,.43f,.52f):new Color(.31f,.35f,.43f);
                    UndercroftMineralFace(supports,At(edge,row),At(next,row),At(next,row+1),At(edge,row+1),
                        tone*(row==0?.84f:row==1?.93f:1.03f),false);
                }
                ServiceShelfBearing(supports,new Vector4(u,17.8f,u+.7f,19.5f),-.68f,-.32f,.055f,
                    new Color(.62f,.65f,.72f));
                UndercroftMineralFace(supports,a+top,b+top,c+top,d+top,new Color(.64f,.67f,.73f),false);
                ServiceShelfGrain(supports,first,supports.V.Count);
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
