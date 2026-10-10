using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _distantMasonryMaterial;

        // World-anchored architecture beyond the playable island, below its deck.
        // Separate far scenery rank: never an actor occluder or a navigable bridge.
        private void BuildDistantWorks(ArenaView arena)
        {
            var masonry=new Builder();var fire=new Builder();
            var far=new Color(.40f,.45f,.57f);
            var near=new Color(.55f,.58f,.66f);
            // Emit the distant span before its two bearing towers. The broken centre
            // makes it unmistakably scenery, with no route leading onto our court.
            DistantArchSpan(masonry,Project(-5,39)+Vector3.down*5.4f,
                Project(12,42)+Vector3.down*3.4f,far);
            for(int i=0;i<masonry.C.Count;i++){Color c=masonry.C[i];c.a=1;masonry.C[i]=c;}
            DistantTower(masonry,fire,35,28,3.2f,3.5f,-5,-25,far*.86f,0);
            DistantTower(masonry,fire,12,42,3.7f,3.7f,-2,-26,far,1);
            DistantTower(masonry,fire,-5,39,4.2f,3.8f,-4,-25,far*.93f,2);
            DistantTower(masonry,fire,27,-20,3.8f,4.1f,-5,-28,near*.91f,3);
            DistantTower(masonry,fire,-40,0,4.1f,3.7f,-3,-25,near,4);
            // Reuse the cavern's atmospheric shader with borrowed wall art; only
            // this material and generated meshes belong to the region lifetime.
            Shader shader=Resources.Load<Shader>("FoundryCavern");
            Material masonryMaterial=arena.PlatformMaterials.FoundryWall;
            if(shader!=null)
            {
                _distantMasonryMaterial=new Material(shader)
                {
                    name="Distant foundry atmosphere owned",mainTexture=masonryMaterial.mainTexture
                };
                _distantMasonryMaterial.SetFloat("_Atmosphere",.62f);
                masonryMaterial=_distantMasonryMaterial;
            }
            // Distinct ranks stay below every island batch, including the basin.
            Mesh stoneMesh=Emit("Distant foundry works",masonry,masonryMaterial,-19);
            Mesh slitMesh=Emit("Distant foundry works furnace slits",fire,arena.PlatformMaterials.FoundryEmbers,-18);
            // Centre/extent reconstruction can round an extreme point just outside
            // its bounds. A millimetre of culling relief encloses both static batches.
            Bounds stoneBounds=stoneMesh.bounds;stoneBounds.Expand(.002f);stoneMesh.bounds=stoneBounds;
            Bounds slitBounds=slitMesh.bounds;slitBounds.Expand(.002f);slitMesh.bounds=slitBounds;
        }

        private static void DistantTower(Builder mesh,Builder fire,float u,float v,float width,float depth,
            float top,float bottom,Color tone,int variant)
        {
            int masonryStart=mesh.C.Count,fireStart=fire.C.Count;
            float u0=u-width*.5f,u1=u+width*.5f,v0=v-depth*.5f,v1=v+depth*.5f;
            // Narrow lower shaft, broad shoulder and dressed crown belong to one
            // supported industrial mass, not a floating cap or an opaque rectangle.
            DistantBlock(mesh,u0+.34f,v0+.3f,u1-.34f,v1-.3f,bottom,bottom+8,tone*.70f);
            DistantBlock(mesh,u0+.34f,v0+.3f,u1-.34f,v1-.3f,bottom+8,top-3.2f,tone*.70f);
            DistantBlock(mesh,u0+.15f,v0+.13f,u1-.15f,v1-.13f,top-10,top-2.8f,tone);
            DistantBlock(mesh,u0,v0,u1,v1,top-3.25f,top-2.65f,tone*1.05f);
            DistantBlock(mesh,u0+.20f,v0+.18f,u1-.20f,v1-.18f,top-2.65f,top-.55f,tone*.92f);
            DistantBlock(mesh,u0-.08f,v0-.08f,u1+.08f,v1+.08f,top-.55f,top-.12f,tone*1.06f);
            DistantBlock(mesh,u0+.18f,v0+.18f,u1-.18f,v1-.18f,top-.12f,top,tone);
            // Two load-bearing front ribs extend down the face. Their modest width
            // keeps the silhouette vertical rather than striping every wall course.
            foreach(float x in new[]{u0+.23f,u1-.53f})
                DistantBlock(mesh,x,v0-.10f,x+.30f,v0+.18f,top-13,top-3.0f,tone*.87f);
            foreach(float x in new[]{u0+.15f,u1-.53f})
                DistantBlock(mesh,x,v0+.12f,x+.38f,v0+.50f,top,top+.55f,tone*.95f);
            // A low vent stack breaks selected crowns; never reaches deck elevation.
            if(variant%2==0)
            {
                DistantBlock(mesh,u-.36f,v+.2f,u+.36f,v+.92f,top,top+1.05f,tone*.82f);
                DistantBlock(mesh,u-.45f,v+.11f,u+.45f,v+1.01f,top+.82f,top+1.1f,tone);
            }
            // Few low furnace openings. Recess stone remains visible around each
            // narrow amber slit; the architectural mass dominates at a distance.
            for(int level=0;level<2;level++)
                for(int column=0;column<2;column++)
                {
                    float x=u-.68f+column*1.1f,z=top-5.7f-level*5.0f;
                    Vector3 a=Project(x,v0-.012f)+Vector3.up*z;
                    Vector3 b=Project(x+.30f,v0-.012f)+Vector3.up*z;
                    mesh.WallQuad(a+Vector3.left*.08f,b+Vector3.right*.08f,
                        b+Vector3.up*1.15f+Vector3.right*.08f,a+Vector3.up*1.15f+Vector3.left*.08f,
                        false,tone*.32f);
                    int slitStart=fire.U.Count;
                    FullFace(fire,a+Vector3.up*.10f,b+Vector3.up*.10f,
                        b+Vector3.up*.99f,a+Vector3.up*.99f,new Color(.28f,.22f,.19f,1));
                    // Sample the same smooth ember patch as the existing lanterns.
                    for(int i=slitStart;i<fire.U.Count;i++)
                        fire.U[i]=new Vector2(.7815f+fire.U[i].x*.001f,.8375f+fire.U[i].y*.001f);
                }
            // Only the low shaft dissolves into depth haze. Upper overlapping tiers
            // remain opaque, so their construction never shows through itself.
            float fadeBase=Project(u,v).y+bottom+2.5f;
            FadeDistantFoot(mesh,masonryStart,fadeBase);
            FadeDistantFoot(fire,fireStart,fadeBase);
        }

        private static void FadeDistantFoot(Builder mesh,int first,float fadeBase)
        {
            for(int i=first;i<mesh.C.Count;i++)
            {
                Color c=mesh.C[i];c.a=Mathf.Clamp01((mesh.V[i].y-fadeBase)/4.5f);
                mesh.C[i]=c;
            }
        }

        private static void DistantBlock(Builder mesh,float u0,float v0,float u1,float v1,float bottom,float top,Color tone)
        {
            Vector3 a=Project(u0,v0),b=Project(u1,v0),c=Project(u1,v1),d=Project(u0,v1);
            mesh.WallQuad(d+Vector3.up*bottom,a+Vector3.up*bottom,a+Vector3.up*top,d+Vector3.up*top,false,tone*.70f);
            mesh.WallQuad(a+Vector3.up*bottom,b+Vector3.up*bottom,b+Vector3.up*top,a+Vector3.up*top,false,tone);
            mesh.WallQuad(a+Vector3.up*top,b+Vector3.up*top,c+Vector3.up*top,d+Vector3.up*top,false,tone*1.13f);
        }

        private static void DistantArchSpan(Builder mesh,Vector3 a,Vector3 b,Color tone)
        {
            const int segments=16;
            Vector3 back=new Vector3(-.75f,.38f,0);
            for(int i=0;i<segments;i++)
            {
                if(i>=7&&i<=9)continue; // A missing middle bay never suggests a path to the court.
                float t=i/(float)segments,next=(i+1f)/segments;
                Vector3 p=Vector3.Lerp(a,b,t),q=Vector3.Lerp(a,b,next);
                float riseP=Mathf.Sqrt(Mathf.Max(0,1-(t*2-1)*(t*2-1)))*4.1f;
                float riseQ=Mathf.Sqrt(Mathf.Max(0,1-(next*2-1)*(next*2-1)))*4.1f;
                Vector3 lowerP=p+Vector3.down*(5.2f-riseP),lowerQ=q+Vector3.down*(5.2f-riseQ);
                mesh.WallQuad(lowerP+back,lowerQ+back,q+back,p+back,false,tone*.65f);
                mesh.WallQuad(lowerP,lowerQ,q,p,false,tone*.88f);
                mesh.WallQuad(p,q,q+back,p+back,false,tone*1.08f);
                if(i==6||i==10)
                    mesh.WallQuad(i==6?lowerQ:lowerP,(i==6?lowerQ:lowerP)+back,
                        (i==6?q:p)+back,i==6?q:p,false,tone*.60f);
            }
        }
    }
}
