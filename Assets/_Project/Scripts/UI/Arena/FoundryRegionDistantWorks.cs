using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _distantMasonryMaterial;
        private const float DistantStoneWidth=2.1f,DistantStoneHeight=1.8f;
        private const float ServiceTallTop=-5.4f,ServiceLowTop=-6f;

        // Keep the furnace sills and foundation feet seated at their original
        // depths while lowering the service skyline above the western yard.
        private static float ServiceShoulder(float top,int variant)=>top-(variant==1?3.7f:4.6f);
        private static float ServiceSill(float top,int variant)=>top-(variant==1?12.8f:14.2f);
        private static float ServiceShoulderRise(int variant)=>variant==1?1.1f:1.3f;

        private static void DistantServiceTower(Builder masonry,Builder fire,Color tone,int variant)
        {
            if(variant==1)DistantTower(masonry,fire,12,42,3.7f,3.7f,ServiceTallTop,-26,tone,1);
            else DistantTower(masonry,fire,-5,39,4.2f,3.8f,ServiceLowTop,-25,tone*.93f,2);
        }

        private static void DistantServiceArch(Builder masonry,Color tone)
        {
            // Its endpoints descend with the supporting necks. The existing
            // broken span stays borne by both cores instead of hanging above them.
            DistantArchSpan(masonry,Project(-5,39)+Vector3.up*(ServiceLowTop-1.4f),
                Project(12,42)+Vector3.up*(ServiceTallTop-1.4f),tone);
        }

        // World-anchored architecture beyond the playable island, below its deck.
        // Separate far scenery rank: never an actor occluder or a navigable bridge.
        private void BuildDistantWorks(ArenaView arena)
        {
            var masonry=new Builder();var fire=new Builder();
            var far=new Color(.70f,.73f,.82f);
            var near=new Color(.82f,.83f,.89f);
            // Emit the distant span before its two bearing towers. The broken centre
            // makes it unmistakably scenery, with no route leading onto our court.
            DistantServiceArch(masonry,far);
            for(int i=0;i<masonry.C.Count;i++){Color c=masonry.C[i];c.a=1;masonry.C[i]=c;}
            DistantTower(masonry,fire,35,28,3.2f,3.5f,-5,-25,far*.86f,0);
            DistantServiceTower(masonry,fire,far,1);
            DistantServiceTower(masonry,fire,far,2);
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
                _distantMasonryMaterial.SetFloat("_Atmosphere",.44f);
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
                FinishDistantStone(mesh,masonryStart,u,v,top,bottom,variant);
                float shaftFade=Project(u,v).y+bottom+2.5f;
                FadeDistantFoot(mesh,masonryStart,shaftFade);
                FadeDistantFoot(fire,fireStart,shaftFade);
                return;
            }
            // A narrow foundation carries a broad furnace chest and sloping shoulder.
            // Differing necks and exhaust crowns break the repeated flat-topped shafts
            // without turning these hazed background buildings into new landmarks.
            DistantBlock(mesh,u0+.34f,v0+.3f,u1-.34f,v1-.3f,bottom,bottom+8,tone*.70f);
            float shoulder=top-(variant==4?4.9f:variant==3?3.5f:4.2f);
            DistantBlock(mesh,u0+.34f,v0+.3f,u1-.34f,v1-.3f,bottom+8,shoulder,tone*.70f);
            DistantBlock(mesh,u0+.15f,v0+.13f,u1-.15f,v1-.13f,top-12,shoulder,tone);
            DistantBlock(mesh,u0,v0,u1,v1,shoulder-.38f,shoulder,tone*1.02f);
            DistantTaper(mesh,u0,v0,u1,v1,shoulder,shoulder+1.35f,.48f,tone*.91f);
            DistantBlock(mesh,u0+.48f,v0+.48f,u1-.48f,v1-.48f,shoulder+1.35f,top-.65f,tone*.85f);
            DistantBlock(mesh,u0+.35f,v0+.35f,u1-.35f,v1-.35f,top-.65f,top-.30f,tone);
            DistantTaper(mesh,u0+.35f,v0+.35f,u1-.35f,v1-.35f,top-.30f,top,.22f,tone*.93f);
            // Splayed bearing feet join the shaft, while the slimmer ribs continue
            // into the shoulder instead of ending abruptly halfway down the face.
            foreach(float x in new[]{u0+.13f,u1-.55f})
            {
                DistantTaper(mesh,x-.08f,v0-.08f,x+.50f,v0+.50f,top-14,top-11,.08f,tone*.82f);
                DistantBlock(mesh,x,v0,x+.42f,v0+.42f,top-11,shoulder,tone*.88f);
            }
            // The three remaining works have a single exhaust, twin narrow stacks,
            // and an offset tall flue with a low hatch. All are borne by the neck.
            if(variant==3)
                foreach(float x in new[]{u-.69f,u+.21f})
                    DistantVentCrown(mesh,x,v-.30f,.48f,.74f,top,top+.70f,tone);
            else if(variant==4)
            {
                DistantVentCrown(mesh,u-.84f,v+.02f,.69f,.85f,top,top+1.10f,tone*.90f);
                DistantTaper(mesh,u+.04f,v-.52f,u+.88f,v+.45f,top,top+.35f,.16f,tone*.84f);
            }
            else
                DistantVentCrown(mesh,u-.37f,v-.24f,.74f,.96f,top,top+1.10f,tone*.94f);
            // Few low furnace openings. Recess stone remains visible around each
            // narrow amber slit; the architectural mass dominates at a distance.
            for(int level=0;level<2;level++)
                for(int column=0;column<2;column++)
                {
                    float x=u-.68f+column*1.1f,z=top-5.7f-level*5.0f;
                    Vector3 a=Project(x,v0-.012f)+Vector3.up*z;
                    Vector3 b=Project(x+.30f,v0-.012f)+Vector3.up*z;
                    DistantMasonryFace(mesh,a+Vector3.left*.08f,b+Vector3.right*.08f,
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
            FinishDistantStone(mesh,masonryStart,u,v,top,bottom,variant);
            FadeDistantFoot(mesh,masonryStart,fadeBase);
            FadeDistantFoot(fire,fireStart,fadeBase);
        }

        private static void DistantVentCrown(Builder mesh,float u,float v,float width,float depth,
            float bottom,float top,Color tone)
        {
            DistantBlock(mesh,u,v,u+width,v+depth,bottom,top-.18f,tone*.74f);
            DistantBlock(mesh,u-.07f,v-.07f,u+width+.07f,v+depth+.07f,top-.18f,top,tone);
            // A dark inset on the cap reads as an exhaust mouth, not a glowing beacon.
            FullFace(mesh,Project(u+.10f,v+.10f)+Vector3.up*(top+.002f),
                Project(u+width-.10f,v+.10f)+Vector3.up*(top+.002f),
                Project(u+width-.10f,v+depth-.10f)+Vector3.up*(top+.002f),
                Project(u+.10f,v+depth-.10f)+Vector3.up*(top+.002f),tone*.30f);
        }

        private static void DistantFurnaceShaft(Builder mesh,Builder fire,float u0,float v0,float u1,float v1,
            float top,float bottom,Color tone,int variant)
        {
            float shoulder=ServiceShoulder(top,variant),sill=ServiceSill(top,variant);
            const float recess=.46f;
            // Continuous narrow foundation, then a recessed core. No outer full
            // wall crosses either bay: the pier/spandrel geometry forms its opening.
            float lowerJoint=Mathf.Lerp(bottom,sill,.5f);
            // The next foundation course covers this cap completely.
            DistantBlock(mesh,u0+.20f,v0+.20f,u1-.20f,v1-.20f,bottom,lowerJoint,tone*.73f,false);
            DistantBlock(mesh,u0+.20f,v0+.20f,u1-.20f,v1-.20f,lowerJoint,sill,tone*.73f);
            DistantBlock(mesh,u0+.10f,v0+.10f,u1-.10f,v1-.10f,sill-.45f,sill,tone*.90f);
            DistantBlock(mesh,u0+recess,v0+recess,u1-.20f,v1-.20f,sill,shoulder,tone*.52f);
            // West face is emitted first; the south corner face is nearer the camera.
            DistantRecessFace(mesh,fire,u0,v0,u1,v1,true,sill,shoulder,recess,tone*.74f);
            DistantRecessFace(mesh,fire,u0,v0,u1,v1,false,sill,shoulder,recess,tone);
            // Corner buttresses carry the shoulder independently of the open fire
            // bays. Their sloping heads meet the neck rather than ending as posts.
            foreach(float x in new[]{u0-.16f,u1-.46f})
            {
                DistantBlock(mesh,x,v0-.20f,x+.62f,v0+.44f,sill,shoulder-.75f,tone*.85f);
                // Lean the head into the receding shoulder, keeping a shared
                // bearing volume instead of a detached fin beside the neck.
                float neck=variant==1?.94f:1.02f;
                float inward=(x<(u0+u1)*.5f?1:-1)*(neck-.15f);
                DistantTaper(mesh,x,v0-.20f,x+.62f,v0+.44f,shoulder-.75f,
                    shoulder+ServiceShoulderRise(variant),.16f,tone*.94f,inward,neck+.04f);
            }
            DistantFurnaceHead(mesh,u0,v0,u1,v1,top,tone,variant);
        }

        private static void DistantFurnaceHead(Builder mesh,float u0,float v0,float u1,float v1,
            float top,Color tone,int variant)
        {
            int first=mesh.V.Count;
            float shoulder=ServiceShoulder(top,variant),neck=variant==1?.94f:1.02f;
            float neckBottom=shoulder+ServiceShoulderRise(variant);
            float exhaustTop=top-(variant==1?0:.40f),exhaustBottom=top-(variant==1?.90f:1.20f);
            Vector4 foot=new Vector4(u0-.06f,v0-.06f,u1+.06f,v1+.06f);
            Vector4 bearing=new Vector4(u0+.14f,v0+.14f,u1-.14f,v1-.14f);
            Vector4 shaft=new Vector4(u0+neck,v0+neck,u1-neck,v1-neck);
            // Two low joined haunch courses carry the narrower neck. Their outer
            // bearing remains fixed in plan, while the reduced rise avoids the
            // previous tall pyramidal head. No hidden horizontal caps
            // are laid across the next section or the open exhaust throat.
            DistantHeadShoulder(mesh,foot,bearing,shoulder,shoulder+.45f,tone*.93f);
            DistantHeadShoulder(mesh,bearing,shaft,shoulder+.45f,neckBottom,tone*.88f);
            // True blind recesses replace the long flat brick neck. Broad corner
            // bearings, dressed sill/header and inset returns share one volume.
            // West first, south last follows the fixed isometric painter order.
            DistantNeckFace(mesh,shaft,true,neckBottom,exhaustBottom-.16f,tone*.70f);
            DistantNeckFace(mesh,shaft,false,neckBottom,exhaustBottom-.16f,tone*.91f);
            Vector4 crown=new Vector4(shaft.x-.12f,shaft.y-.12f,shaft.z+.12f,shaft.w+.12f);
            DistantHeadShoulder(mesh,shaft,crown,exhaustBottom-.16f,exhaustBottom,tone*.95f);
            DistantHollowExhaust(mesh,crown,exhaustBottom,exhaustTop,variant==1?.22f:.26f,tone);
            // One physical mineral cut crosses the whole connected head. Narrow
            // returns and crown facets must not each stretch/restart a full patch.
            GateRecessGrain(mesh,first,mesh.V.Count,Project(1,0)-Project(0,0),variant);
        }

        private static void DistantHeadShoulder(Builder mesh,Vector4 lower,Vector4 upper,float bottom,float top,Color tone)
        {
            Vector3 a=Project(lower.x,lower.y)+Vector3.up*bottom,b=Project(lower.z,lower.y)+Vector3.up*bottom;
            Vector3 d=Project(lower.x,lower.w)+Vector3.up*bottom;
            Vector3 e=Project(upper.x,upper.y)+Vector3.up*top,f=Project(upper.z,upper.y)+Vector3.up*top;
            Vector3 h=Project(upper.x,upper.w)+Vector3.up*top;
            DistantMineralFace(mesh,d,a,e,h,tone*.72f,true);
            DistantMineralFace(mesh,a,b,f,e,tone,true);
        }

        private static void DistantNeckFace(Builder mesh,Vector4 box,bool west,float bottom,float top,Color tone)
        {
            const float side=.20f,inset=.14f;
            float lower=bottom+.20f,upper=top-.20f;
            Vector3 At(float t,float h,float recess)=>DistantFacePoint(box.x,box.y,box.z,box.w,west,t,h,recess);
            // Each piece owns a disjoint portion of this facade: there is no
            // original full wall under the recessed panel and its side returns.
            DistantHeadCourses(mesh,At(0,bottom,0),At(side,bottom,0),At(side,top,0),At(0,top,0),tone,2);
            DistantHeadCourses(mesh,At(1-side,bottom,0),At(1,bottom,0),At(1,top,0),At(1-side,top,0),tone,2);
            DistantMineralFace(mesh,At(side,bottom,0),At(1-side,bottom,0),At(1-side,lower,0),At(side,lower,0),tone,true);
            DistantMineralFace(mesh,At(side,upper,0),At(1-side,upper,0),At(1-side,top,0),At(side,top,0),tone,true);
            DistantHeadCourses(mesh,At(side,lower,inset),At(1-side,lower,inset),At(1-side,upper,inset),At(side,upper,inset),tone*.78f,2);
            DistantMineralFace(mesh,At(side,lower,0),At(side,lower,inset),At(side,upper,inset),At(side,upper,0),tone*.58f,false);
            DistantMineralFace(mesh,At(1-side,lower,inset),At(1-side,lower,0),At(1-side,upper,0),At(1-side,upper,inset),tone*.84f,false);
            DistantMineralFace(mesh,At(side,lower,0),At(1-side,lower,0),At(1-side,lower,inset),At(side,lower,inset),tone*1.04f,false);
            DistantMineralFace(mesh,At(side,upper,inset),At(1-side,upper,inset),At(1-side,upper,0),At(side,upper,0),tone*.66f,false);
        }

        private static void DistantHeadCourses(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tone,int rows)
        {
            for(int row=0;row<rows;row++)
            {
                float lo=row/(float)rows,hi=(row+1f)/rows;
                DistantMineralFace(mesh,Vector3.Lerp(a,d,lo),Vector3.Lerp(b,c,lo),
                    Vector3.Lerp(b,c,hi),Vector3.Lerp(a,d,hi),tone,true);
            }
        }

        private static void DistantHollowExhaust(Builder mesh,Vector4 outer,float bottom,float top,float wall,Color tone)
        {
            Vector4 inside=new Vector4(outer.x+wall,outer.y+wall,outer.z-wall,outer.w-wall);
            var rim=new[]{Project(outer.x,outer.y),Project(outer.z,outer.y),Project(outer.z,outer.w),Project(outer.x,outer.w)};
            var throat=new[]{Project(inside.x,inside.y),Project(inside.z,inside.y),Project(inside.z,inside.w),Project(inside.x,inside.w)};
            // A low opaque throat closes the distant shaft. The top remains a
            // geometric hole: only its annulus and inner walls reach the rim.
            DistantMineralFace(mesh,throat[0]+Vector3.up*bottom,throat[1]+Vector3.up*bottom,
                throat[2]+Vector3.up*bottom,throat[3]+Vector3.up*bottom,tone*.16f,false);
            foreach(int edge in new[]{1,2,3,0})
            {
                int next=(edge+1)%4;
                DistantMineralFace(mesh,throat[edge]+Vector3.up*bottom,throat[next]+Vector3.up*bottom,
                    throat[next]+Vector3.up*top,throat[edge]+Vector3.up*top,tone*(edge<2?.34f:.23f),false);
            }
            // Outer near faces occlude the bottom of the throat in the same way
            // a real open flue does. Their cap is four rim strips, never a full roof.
            foreach(int edge in new[]{3,0})
            {
                DistantCrownBearing(mesh,outer,edge,bottom,top,tone*(edge==0?.96f:.69f));
            }
            foreach(int edge in new[]{1,2,3,0})
            {
                int next=(edge+1)%4;
                DistantMineralFace(mesh,rim[edge]+Vector3.up*top,rim[next]+Vector3.up*top,
                    throat[next]+Vector3.up*top,throat[edge]+Vector3.up*top,tone*(edge<2?.89f:1.07f),true);
            }
        }

        private static void DistantCrownBearing(Builder mesh,Vector4 outer,int edge,float bottom,float top,Color tone)
        {
            // Replace the flat dressed side with a broad recessed fascia between
            // sloping bearings. Shared ring stations close the near corner; the
            // outer top/bottom and actual open mouth keep their original planes.
            var levels=new[]{bottom,Mathf.Lerp(bottom,top,.18f),Mathf.Lerp(bottom,top,.72f),top};
            var insets=new[]{0f,.075f,.075f,0f};
            Vector3 At(int corner,int band)
            {
                float inset=insets[band];
                float u=corner==0||corner==3?outer.x+inset:outer.z-inset;
                float v=corner<2?outer.y+inset:outer.w-inset;
                return Project(u,v)+Vector3.up*levels[band];
            }
            int next=(edge+1)%4;
            for(int band=0;band<3;band++)
                DistantMineralFace(mesh,At(edge,band),At(next,band),At(next,band+1),At(edge,band+1),
                    tone*(band==0?.78f:band==1?.94f:1.10f),false);
        }

        private static void DistantMineralFace(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tone,bool dressed)
        {
            int first=mesh.V.Count;
            FullFace(mesh,a,b,c,d,tone);
            // A quiet interior patch keeps these few broad fitted stones distinct
            // from the lower shafts' brick field without adding another texture.
            mesh.U[first]=new Vector2(.275f,.655f);mesh.U[first+1]=new Vector2(.415f,.655f);
            mesh.U[first+2]=new Vector2(.415f,.747f);mesh.U[first+3]=new Vector2(.275f,.747f);
            if(dressed)GateStoneEdges(mesh,first,.032f,.032f);
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
            DistantMasonryFace(mesh,p(0,sill,0),p(left,sill,0),p(left,head,0),p(0,head,0),false,tone);
            DistantMasonryFace(mesh,p(right,sill,0),p(1,sill,0),p(1,head,0),p(right,head,0),false,tone);
            // Jamb returns and deep sill expose the real distance to the core.
            DistantMasonryFace(mesh,p(left,sill,0),p(left,sill,recess),p(left,spring,recess),p(left,spring,0),false,tone*.64f);
            DistantMasonryFace(mesh,p(right,sill,recess),p(right,sill,0),p(right,spring,0),p(right,spring,recess),false,tone*.80f);
            DistantMasonryFace(mesh,p(left,sill,0),p(right,sill,0),p(right,sill,recess),p(left,sill,recess),true,tone*.85f);
            // An eight-stone arched head. Each spandrel ends at its curved underside;
            // the reveals follow that curve back into the recessed shaft.
            const int segments=8;
            for(int i=0;i<segments;i++)
            {
                float a=Mathf.PI-i*Mathf.PI/segments,b=Mathf.PI-(i+1)*Mathf.PI/segments;
                float ta=.5f+Mathf.Cos(a)*(right-left)*.5f,tb=.5f+Mathf.Cos(b)*(right-left)*.5f;
                float za=spring+Mathf.Sin(a)*radius,zb=spring+Mathf.Sin(b)*radius;
                Color course=tone*(i%3==0?.92f:1f);
                DistantMasonryFace(mesh,p(ta,za,0),p(tb,zb,0),p(tb,head,0),p(ta,head,0),false,course);
                DistantMasonryFace(mesh,p(ta,za,recess),p(tb,zb,recess),p(tb,zb,0),p(ta,za,0),false,tone*.60f);
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
                DistantMasonryFace(mesh,p(ia,izA,projection),p(ib,izB,projection),p(ob,ozB,projection),p(oa,ozA,projection),false,dress);
                DistantMasonryFace(mesh,p(ia,izA,0),p(ib,izB,0),p(ib,izB,projection),p(ia,izA,projection),false,tone*.87f);
                DistantMasonryFace(mesh,p(oa,ozA,projection),p(ob,ozB,projection),p(ob,ozB,0),p(oa,ozA,0),false,tone*1.10f);
            }
            foreach(float jamb in new[]{left-frame/span,right})
            {
                float end=jamb+frame/span,edge=.009f;
                DistantMasonryFace(mesh,p(jamb+edge,sill,projection),p(end-edge,sill,projection),p(end-edge,spring,projection),p(jamb+edge,spring,projection),false,tone*1.30f);
                DistantMasonryFace(mesh,p(jamb,sill,-.04f),p(jamb+edge,sill,projection),p(jamb+edge,spring,projection),p(jamb,spring,-.04f),false,tone*.98f);
                DistantMasonryFace(mesh,p(end-edge,sill,projection),p(end,sill,-.04f),p(end,spring,-.04f),p(end-edge,spring,projection),false,tone*1.17f);
                // Broad foot and impost shoulders carry the dressed arch frame.
                foreach(float z in new[]{sill+.30f,spring-.20f})
                {
                    DistantMasonryFace(mesh,p(jamb-.032f,z,-.24f),p(end+.032f,z,-.24f),p(end+.032f,z+.20f,-.24f),p(jamb-.032f,z+.20f,-.24f),false,tone*1.22f);
                    DistantMasonryFace(mesh,p(jamb-.032f,z+.20f,-.24f),p(end+.032f,z+.20f,-.24f),p(end+.032f,z+.20f,0),p(jamb-.032f,z+.20f,0),false,tone*1.42f);
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
                DistantMasonryFace(mesh,p(.37f,lintel-.09f,recess-.12f),p(.63f,lintel-.09f,recess-.12f),
                    p(.63f,lintel+.09f,recess-.12f),p(.37f,lintel+.09f,recess-.12f),false,tone*.91f);
                DistantMasonryFace(mesh,p(.37f,lintel+.09f,recess-.12f),p(.63f,lintel+.09f,recess-.12f),
                    p(.63f,lintel+.09f,recess),p(.37f,lintel+.09f,recess),false,tone*1.1f);
            }
            // The first edge of each arch voussoir slopes differently. Texture
            // courses instead share the facade's fixed design-plane horizontal,
            // preventing rotated bricks across the segmented spandrel and reveals.
            float facadeSlope=west?-.5f:.5f;
            for(int i=facadeStart;i<mesh.V.Count;i++)
                mesh.U[i]=new Vector2(mesh.V[i].x/DistantStoneWidth,(mesh.V[i].y-facadeSlope*mesh.V[i].x)/DistantStoneHeight);
        }

        private static void DistantMasonryFace(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,bool cap,Color tone,
            float? courseSlope=null)
        {
            int first=mesh.V.Count;
            FullFace(mesh,a,b,c,d,tone);
            float slope=courseSlope??(Mathf.Abs(b.x-a.x)>.0001f?(b.y-a.y)/(b.x-a.x):0);
            for(int j=0;j<4;j++)
            {
                Vector3 p=mesh.V[first+j];
                // Caps use the same small stone scale in their design plane, rather
                // than the foreground floor's very broad eight-unit texture span.
                mesh.U[first+j]=cap?new Vector2((p.x+p.y*2)*.70710678f/DistantStoneWidth,
                    (-p.x+p.y*2)*.70710678f/DistantStoneWidth):
                    new Vector2(p.x/DistantStoneWidth,(p.y-slope*p.x)/DistantStoneHeight);
                Color lit=tone*(cap?1f:j<2?.88f:1.03f);lit.a=tone.a;mesh.C[first+j]=lit;
            }
        }

        private static void FinishDistantStone(Builder mesh,int first,float u,float v,float top,float bottom,int variant)
        {
            Vector3 origin=Project(u,v);
            for(int i=first;i<mesh.V.Count;i++)
            {
                Vector3 p=mesh.V[i];float height=p.y-origin.y;
                float rise=Mathf.SmoothStep(0,1,Mathf.InverseLerp(bottom+3,top,height));
                // Continuous broad weathering, not per-block random tints: lower
                // damp stone cools into the haze while exposed crowns regain value.
                Color ambient=Color.Lerp(new Color(.64f,.70f,.82f),new Color(1.08f,1.05f,1.02f),rise);
                float stain=.96f+.04f*Mathf.Sin((p.x-origin.x)*1.1f+variant*1.7f);
                float heat=(variant==1||variant==2)?Mathf.Clamp01(1-Mathf.Abs(height-(top-10))/7.5f):0;
                float bearing=1;
                if(variant==1||variant==2)
                {
                    // Exposed haunch stone catches a broad quarry value above
                    // the damp shaft. The neck/crown retain less of that lift;
                    // this is one supported head, not a global exposure change.
                    float shoulder=ServiceShoulder(top,variant);
                    float band=(height-shoulder-.55f)/1.65f;
                    bearing+=.16f*Mathf.Exp(-band*band)+.075f*Mathf.SmoothStep(0,1,
                        Mathf.InverseLerp(shoulder+ServiceShoulderRise(variant),top-.35f,height));
                }
                Color old=mesh.C[i];
                mesh.C[i]=new Color(old.r*ambient.r*stain*(1+.10f*heat)*bearing,old.g*ambient.g*stain*(1+.025f*heat)*bearing,
                    old.b*ambient.b*stain*(1-.025f*heat)*bearing,old.a);
            }
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

        private static void DistantTaper(Builder mesh,float u0,float v0,float u1,float v1,float bottom,float top,float inset,Color tone,
            float topU=0,float topV=0)
        {
            Vector3 a=Project(u0,v0)+Vector3.up*bottom,b=Project(u1,v0)+Vector3.up*bottom;
            Vector3 d=Project(u0,v1)+Vector3.up*bottom;
            Vector3 e=Project(u0+inset+topU,v0+inset+topV)+Vector3.up*top,f=Project(u1-inset+topU,v0+inset+topV)+Vector3.up*top;
            Vector3 g=Project(u1-inset+topU,v1-inset+topV)+Vector3.up*top,h=Project(u0+inset+topU,v1-inset+topV)+Vector3.up*top;
            DistantMasonryFace(mesh,d,a,e,h,false,tone*.70f);
            DistantMasonryFace(mesh,a,b,f,e,false,tone);
            DistantMasonryFace(mesh,e,f,g,h,true,tone*1.10f);
        }

        private static void FadeDistantFoot(Builder mesh,int first,float fadeBase)
        {
            for(int i=first;i<mesh.C.Count;i++)
            {
                Color c=mesh.C[i];c.a=Mathf.Clamp01((mesh.V[i].y-fadeBase)/4.5f);
                mesh.C[i]=c;
            }
        }

        private static void DistantBlock(Builder mesh,float u0,float v0,float u1,float v1,float bottom,float top,Color tone,bool cap=true)
        {
            Vector3 a=Project(u0,v0),b=Project(u1,v0),c=Project(u1,v1),d=Project(u0,v1);
            DistantMasonryFace(mesh,d+Vector3.up*bottom,a+Vector3.up*bottom,a+Vector3.up*top,d+Vector3.up*top,false,tone*.70f);
            DistantMasonryFace(mesh,a+Vector3.up*bottom,b+Vector3.up*bottom,b+Vector3.up*top,a+Vector3.up*top,false,tone);
            if(cap)DistantMasonryFace(mesh,a+Vector3.up*top,b+Vector3.up*top,c+Vector3.up*top,d+Vector3.up*top,true,tone*1.13f);
        }

        private static void DistantArchSpan(Builder mesh,Vector3 a,Vector3 b,Color tone)
        {
            const int segments=16;
            Vector3 back=new Vector3(-1.05f,.525f,0);
            float courseSlope=(b.y-a.y)/(b.x-a.x);
            // The underside follows an arch, but its masonry courses follow the
            // whole span. Deriving each segment's UV slope from that curve rotated
            // adjacent courses and made the two broken halves read as plain stubs.
            void SpanFace(Vector3 p,Vector3 q,Vector3 r,Vector3 s,bool cap,Color tint)=>
                DistantMasonryFace(mesh,p,q,r,s,cap,tint,courseSlope);
            for(int i=0;i<segments;i++)
            {
                if(i>=7&&i<=9)continue; // A missing middle bay never suggests a path to the court.
                float t=i/(float)segments,next=(i+1f)/segments;
                Vector3 p=Vector3.Lerp(a,b,t),q=Vector3.Lerp(a,b,next);
                float riseP=Mathf.Sqrt(Mathf.Max(0,1-(t*2-1)*(t*2-1)))*4.1f;
                float riseQ=Mathf.Sqrt(Mathf.Max(0,1-(next*2-1)*(next*2-1)))*4.1f;
                Vector3 lowerP=p+Vector3.down*(5.2f-riseP),lowerQ=q+Vector3.down*(5.2f-riseQ);
                bool broken=i==6||i==10;
                if(broken)
                {
                    // The two severed ends have different fracture angles. Front
                    // and rear share each authored notch, closing the exposed core
                    // without a square cut, floating rubble or a paper-thin edge.
                    bool left=i==6;
                    Vector3 inward=(left?a-b:b-a).normalized;
                    Vector3 low=(left?lowerQ:lowerP)+inward*.12f;
                    Vector3 high=(left?q:p)+inward*(left?.30f:.16f);
                    Vector3 notch=Vector3.Lerp(low,high,left?.43f:.62f)+inward*(left?.46f:.33f);
                    Vector3 innerLow=left?lowerP:lowerQ,innerHigh=left?p:q;
                    Vector3 middle=Vector3.Lerp(innerLow,innerHigh,left?.43f:.62f);
                    var ends=new[]{low,notch,high};var inner=new[]{innerLow,middle,innerHigh};
                    for(int band=0;band<2;band++)
                    {
                        Vector3 f0=left?inner[band]:ends[band],f1=left?ends[band]:inner[band];
                        Vector3 f2=left?ends[band+1]:inner[band+1],f3=left?inner[band+1]:ends[band+1];
                        SpanFace(f0+back,f1+back,f2+back,f3+back,false,tone*.65f);
                        SpanFace(f0,f1,f2,f3,false,tone*.88f);
                        SpanFace(ends[band],ends[band]+back,ends[band+1]+back,ends[band+1],false,tone*(band==0?.57f:.74f));
                    }
                    if(left){lowerQ=low;q=high;}else{lowerP=low;p=high;}
                }
                else
                {
                    SpanFace(lowerP+back,lowerQ+back,q+back,p+back,false,tone*.65f);
                    SpanFace(lowerP,lowerQ,q,p,false,tone*.88f);
                }
                SpanFace(lowerP+back,lowerQ+back,lowerQ,lowerP,false,tone*.48f);
                SpanFace(p,q,q+back,p+back,true,tone*1.08f);
                // Low masonry parapet and occasional squared posts keep the span
                // architectural without disguising the central break as a route.
                Vector3 rail=Vector3.up*.43f,railBack=back*.28f;
                SpanFace(p,q,q+rail,p+rail,false,tone*.88f);
                SpanFace(p+rail,q+rail,q+rail+railBack,p+rail+railBack,true,tone*1.05f);
                if(broken)
                {
                    Vector3 end=i==6?q:p;
                    SpanFace(end,end+railBack,end+rail+railBack,end+rail,false,tone*.62f);
                }
                if(i==0||i==3||i==13||i==15)
                {
                    Vector3 post=(p+q)*.5f,half=(q-p).normalized*.14f;
                    SpanFace(post-half,post+half,post+half+Vector3.up*.70f,post-half+Vector3.up*.70f,false,tone);
                    SpanFace(post-half+Vector3.up*.70f,post+half+Vector3.up*.70f,
                        post+half+Vector3.up*.70f+railBack,post-half+Vector3.up*.70f+railBack,true,tone*1.1f);
                }
            }
            // Deep bearing shoes extend below both springings and meet the tower
            // cores. They are emitted before the towers, so the latter own overlap.
            foreach(Vector3 end in new[]{a,b})
            {
                Vector3 half=(b-a).normalized*.55f,low=Vector3.down*6.3f,high=Vector3.down*4.3f;
                SpanFace(end-half+low,end+half+low,end+half+high,end-half+high,false,tone*.78f);
                SpanFace(end-half+low+back,end-half+low,end-half+high,end-half+high+back,false,tone*.57f);
                SpanFace(end-half+high,end+half+high,end+half+high+back,end-half+high+back,true,tone);
            }
        }
    }
}
