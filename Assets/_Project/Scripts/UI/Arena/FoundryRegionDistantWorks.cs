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
            if(variant==1||variant==2)
            {
                DistantFurnaceShaft(mesh,fire,u0,v0,u1,v1,top,bottom,tone,variant);
                float shaftFade=Project(u,v).y+bottom+2.5f;
                FadeDistantFoot(mesh,masonryStart,shaftFade);
                FadeDistantFoot(fire,fireStart,shaftFade);
                return;
            }
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

        private static void DistantFurnaceShaft(Builder mesh,Builder fire,float u0,float v0,float u1,float v1,
            float top,float bottom,Color tone,int variant)
        {
            float shoulder=top-(variant==1?4.8f:6.2f),sill=top-16.2f;
            const float recess=.46f;
            // Continuous narrow foundation, then a recessed core. No outer full
            // wall crosses either bay: the pier/spandrel geometry forms its opening.
            float lowerJoint=Mathf.Lerp(bottom,sill,.5f);
            DistantBlock(mesh,u0+.20f,v0+.20f,u1-.20f,v1-.20f,bottom,lowerJoint,tone*.73f);
            DistantBlock(mesh,u0+.20f,v0+.20f,u1-.20f,v1-.20f,lowerJoint,sill,tone*.73f);
            DistantBlock(mesh,u0+.10f,v0+.10f,u1-.10f,v1-.10f,sill-.45f,sill,tone*.90f);
            DistantBlock(mesh,u0+recess,v0+recess,u1-.20f,v1-.20f,sill,shoulder,tone*.52f);
            // West face is emitted first; the south corner face is nearer the camera.
            DistantRecessFace(mesh,fire,u0,v0,u1,v1,true,sill,shoulder,recess,tone*.74f);
            DistantRecessFace(mesh,fire,u0,v0,u1,v1,false,sill,shoulder,recess,tone);
            // Stepped load-bearing shoulders become a tapered upper shaft. These
            // are sloping masonry faces, not a repetitive pile of floating caps.
            DistantTaper(mesh,u0-.06f,v0-.06f,u1+.06f,v1+.06f,shoulder,shoulder+.52f,.18f,tone);
            DistantTaper(mesh,u0+.12f,v0+.12f,u1-.12f,v1-.12f,shoulder+.52f,top-1.10f,.39f,tone*.92f);
            DistantBlock(mesh,u0+.42f,v0+.42f,u1-.42f,v1-.42f,top-1.1f,top-.52f,tone*.92f);
            DistantBlock(mesh,u0+.28f,v0+.28f,u1-.28f,v1-.28f,top-.52f,top-.13f,tone*1.06f);
            DistantBlock(mesh,u0+.44f,v0+.44f,u1-.44f,v1-.44f,top-.13f,top,tone);
            if(variant==2)
            {
                float u=(u0+u1)*.5f,v=(v0+v1)*.5f;
                DistantBlock(mesh,u-.36f,v+.20f,u+.36f,v+.92f,top,top+.95f,tone*.82f);
                DistantBlock(mesh,u-.45f,v+.11f,u+.45f,v+1.01f,top+.80f,top+1.1f,tone);
            }
        }

        private static Vector3 DistantFacePoint(float u0,float v0,float u1,float v1,bool west,float t,float z,float inset)
        {
            // Traverse both visible faces left-to-right in the fixed camera.
            return (west?Project(u0+inset,Mathf.Lerp(v1,v0,t)):
                Project(Mathf.Lerp(u0,u1,t),v0+inset))+Vector3.up*z;
        }

        private static void DistantRecessFace(Builder mesh,Builder fire,float u0,float v0,float u1,float v1,
            bool west,float sill,float head,float recess,Color tone)
        {
            const float left=.29f,right=.71f;
            int facadeStart=mesh.V.Count;
            float span=west?v1-v0:u1-u0,radius=(right-left)*span*.5f,spring=head-.38f-radius;
            System.Func<float,float,float,Vector3> p=(t,z,d)=>DistantFacePoint(u0,v0,u1,v1,west,t,z,d);
            mesh.WallQuad(p(0,sill,0),p(left,sill,0),p(left,head,0),p(0,head,0),false,tone);
            mesh.WallQuad(p(right,sill,0),p(1,sill,0),p(1,head,0),p(right,head,0),false,tone);
            // Jamb returns and deep sill expose the real distance to the core.
            mesh.WallQuad(p(left,sill,0),p(left,sill,recess),p(left,spring,recess),p(left,spring,0),false,tone*.64f);
            mesh.WallQuad(p(right,sill,recess),p(right,sill,0),p(right,spring,0),p(right,spring,recess),false,tone*.80f);
            mesh.WallQuad(p(left,sill,0),p(right,sill,0),p(right,sill,recess),p(left,sill,recess),true,tone*.85f);
            // An eight-stone arched head. Each spandrel ends at its curved underside;
            // the reveals follow that curve back into the recessed shaft.
            const int segments=8;
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.PI-i*Mathf.PI/segments,b=Mathf.PI-(i+1)*Mathf.PI/segments;
                float ta=.5f+Mathf.Cos(a)*(right-left)*.5f,tb=.5f+Mathf.Cos(b)*(right-left)*.5f;
                float za=spring+Mathf.Sin(a)*radius,zb=spring+Mathf.Sin(b)*radius;
                Color course=tone*(i%3==0?.92f:1f);
                mesh.WallQuad(p(ta,za,0),p(tb,zb,0),p(tb,head,0),p(ta,head,0),false,course);
                mesh.WallQuad(p(ta,za,recess),p(tb,zb,recess),p(tb,zb,0),p(ta,za,0),false,tone*.60f);
            }
            // Raised dressed masonry catches light around the opening. Its outer
            // radius and projecting returns are geometry, not a painted arch.
            const float frame=.19f,projection=-.18f;
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.PI-i*Mathf.PI/segments,b=Mathf.PI-(i+1)*Mathf.PI/segments;
                float ia=.5f+Mathf.Cos(a)*radius/span,ib=.5f+Mathf.Cos(b)*radius/span;
                float oa=.5f+Mathf.Cos(a)*(radius+frame)/span,ob=.5f+Mathf.Cos(b)*(radius+frame)/span;
                float izA=spring+Mathf.Sin(a)*radius,izB=spring+Mathf.Sin(b)*radius;
                float ozA=spring+Mathf.Sin(a)*(radius+frame),ozB=spring+Mathf.Sin(b)*(radius+frame);
                Color dress=tone*(i%3==0?1.27f:1.38f);
                mesh.WallQuad(p(ia,izA,projection),p(ib,izB,projection),p(ob,ozB,projection),p(oa,ozA,projection),false,dress);
                mesh.WallQuad(p(ia,izA,0),p(ib,izB,0),p(ib,izB,projection),p(ia,izA,projection),false,tone*.87f);
                mesh.WallQuad(p(oa,ozA,projection),p(ob,ozB,projection),p(ob,ozB,0),p(oa,ozA,0),false,tone*1.10f);
            }
            foreach(float jamb in new[]{left-frame/span,right})
            {
                float end=jamb+frame/span,edge=.009f;
                mesh.WallQuad(p(jamb+edge,sill,projection),p(end-edge,sill,projection),p(end-edge,spring,projection),p(jamb+edge,spring,projection),false,tone*1.30f);
                mesh.WallQuad(p(jamb,sill,-.04f),p(jamb+edge,sill,projection),p(jamb+edge,spring,projection),p(jamb,spring,-.04f),false,tone*.98f);
                mesh.WallQuad(p(end-edge,sill,projection),p(end,sill,-.04f),p(end,spring,-.04f),p(end-edge,spring,projection),false,tone*1.17f);
                // Broad foot and impost shoulders carry the dressed arch frame.
                foreach(float z in new[]{sill+.30f,spring-.20f})
                {
                    mesh.WallQuad(p(jamb-.032f,z,-.24f),p(end+.032f,z,-.24f),p(end+.032f,z+.20f,-.24f),p(jamb-.032f,z+.20f,-.24f),false,tone*1.22f);
                    mesh.WallQuad(p(jamb-.032f,z+.20f,-.24f),p(end+.032f,z+.20f,-.24f),p(end+.032f,z+.20f,0),p(jamb-.032f,z+.20f,0),false,tone*1.42f);
                }
            }
            float fireBottom=sill+1.05f,fireTop=spring-.55f,step=(fireTop-fireBottom)/3f;
            float paneHeight=Mathf.Min(1.65f,step*.57f);
            for(int pane=0;pane<3;pane++)
            {
                float z0=fireBottom+pane*step,z1=z0+paneHeight,mid=(z0+z1)*.5f;
                // Open gaps deliberately contain no fire triangles: this material
                // draws after masonry and cannot rely on a grate hiding its glow.
                DistantFirePane(fire,p,.466f,.534f,z0,mid,recess-.008f,.10f,.19f);
                DistantFirePane(fire,p,.466f,.534f,mid,z1,recess-.008f,.19f,.12f);
                if(pane==2)continue;
                float lintel=(z1+fireBottom+(pane+1)*step)*.5f;
                mesh.WallQuad(p(.37f,lintel-.09f,recess-.12f),p(.63f,lintel-.09f,recess-.12f),
                    p(.63f,lintel+.09f,recess-.12f),p(.37f,lintel+.09f,recess-.12f),false,tone*.91f);
                mesh.WallQuad(p(.37f,lintel+.09f,recess-.12f),p(.63f,lintel+.09f,recess-.12f),
                    p(.63f,lintel+.09f,recess),p(.37f,lintel+.09f,recess),false,tone*1.1f);
            }
            // The first edge of each arch voussoir slopes differently. Texture
            // courses instead share the facade's fixed design-plane horizontal,
            // preventing rotated bricks across the segmented spandrel and reveals.
            float facadeSlope=west?-.5f:.5f;
            for(int i=facadeStart;i<mesh.V.Count;i++)
                mesh.U[i]=new Vector2(mesh.V[i].x/2.8f,(mesh.V[i].y-facadeSlope*mesh.V[i].x)/2.4f);
        }

        private static void DistantFirePane(Builder fire,System.Func<float,float,float,Vector3> p,
            float left,float right,float bottom,float top,float inset,float low,float high)
        {
            int first=fire.V.Count;
            FullFace(fire,p(left,bottom,inset),p(right,bottom,inset),p(right,top,inset),p(left,top,inset),Color.white);
            for(int i=0;i<4;i++)
            {
                float value=i<2?low:high;
                fire.C[first+i]=new Color(value,value*.73f,value*.52f,1);
                fire.U[first+i]=new Vector2(.7815f+fire.U[first+i].x*.001f,.8375f+fire.U[first+i].y*.001f);
            }
        }

        private static void DistantTaper(Builder mesh,float u0,float v0,float u1,float v1,float bottom,float top,float inset,Color tone)
        {
            Vector3 a=Project(u0,v0)+Vector3.up*bottom,b=Project(u1,v0)+Vector3.up*bottom;
            Vector3 d=Project(u0,v1)+Vector3.up*bottom;
            Vector3 e=Project(u0+inset,v0+inset)+Vector3.up*top,f=Project(u1-inset,v0+inset)+Vector3.up*top;
            Vector3 g=Project(u1-inset,v1-inset)+Vector3.up*top,h=Project(u0+inset,v1-inset)+Vector3.up*top;
            mesh.WallQuad(d,a,e,h,false,tone*.70f);
            mesh.WallQuad(a,b,f,e,false,tone);
            mesh.WallQuad(e,f,g,h,true,tone*1.10f);
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
            Vector3 back=new Vector3(-1.05f,.525f,0);
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
                mesh.WallQuad(lowerP+back,lowerQ+back,lowerQ,lowerP,false,tone*.48f);
                mesh.WallQuad(p,q,q+back,p+back,false,tone*1.08f);
                // Low masonry parapet and occasional squared posts keep the span
                // architectural without disguising the central break as a route.
                Vector3 rail=Vector3.up*.43f,railBack=back*.28f;
                mesh.WallQuad(p,q,q+rail,p+rail,false,tone*.88f);
                mesh.WallQuad(p+rail,q+rail,q+rail+railBack,p+rail+railBack,false,tone*1.05f);
                if(i==0||i==3||i==6||i==10||i==13||i==15)
                {
                    Vector3 post=(p+q)*.5f,half=(q-p).normalized*.14f;
                    mesh.WallQuad(post-half,post+half,post+half+Vector3.up*.70f,post-half+Vector3.up*.70f,false,tone);
                    mesh.WallQuad(post-half+Vector3.up*.70f,post+half+Vector3.up*.70f,
                        post+half+Vector3.up*.70f+railBack,post-half+Vector3.up*.70f+railBack,false,tone*1.1f);
                }
                if(i==6||i==10)
                    mesh.WallQuad(i==6?lowerQ:lowerP,(i==6?lowerQ:lowerP)+back,
                        (i==6?q:p)+back,i==6?q:p,false,tone*.60f);
            }
            // Deep bearing shoes extend below both springings and meet the tower
            // cores. They are emitted before the towers, so the latter own overlap.
            foreach(Vector3 end in new[]{a,b})
            {
                Vector3 half=(b-a).normalized*.55f,low=Vector3.down*6.3f,high=Vector3.down*4.3f;
                mesh.WallQuad(end-half+low,end+half+low,end+half+high,end-half+high,false,tone*.78f);
                mesh.WallQuad(end-half+low+back,end-half+low,end-half+high,end-half+high+back,false,tone*.57f);
                mesh.WallQuad(end-half+high,end+half+high,end+half+high+back,end-half+high+back,false,tone);
            }
        }
    }
}
