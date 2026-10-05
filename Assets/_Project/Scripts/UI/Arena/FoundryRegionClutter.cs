using UnityEngine;

namespace Cryptforge.UI
{
    // Working clutter, after the approved layout: plank crates, iron-banded barrels, sacks and a bronze stock pile in a few
    // chunky clusters, and fire baskets flanking the gate. The walkable floor runs to the balustrade, so nothing stands
    // inside the rail: the clusters keep to ground off the shared walking union - the rim gap between the salvage shelf and
    // the crane footing (on a loading landing built as the shelf is), the crane footing's iron roof, the pipe shelf, and
    // service landings behind the rail in the apron's two corners, beside the north-west and south-west boilers (the first
    // running on to the court's north corner). The court's west and east corners carry their lantern posts in the rim and
    // have no such ground. Every footprint is tested and skipped if it would touch the union, as the massing's are; built
    // once, in existing (material, order) batches; no new renderer.
    //
    // Painter order is by batch, then emission, so each piece takes the batch its neighbours call for: masonry -11 draws
    // before iron -11 (both scenery order -8; their batches' bounds order them, and every piece here lies inside both
    // bounds), and the massing's balustrade, posts and finials (masonry -11) are emitted after this file. A piece behind a
    // rail, a finial or a shelf prop goes in the masonry batch, under it; a piece in front of iron scenery goes in the iron
    // batch, over it. Within a cluster pieces are listed far to near, a stacked piece after the one it stands on.
    public sealed partial class FoundryRegionView
    {
        private enum ClutterKind { Crate, Barrel, Sack, Ingots }
        // Footprint (u0,v0,u1,v1; a barrel or a sack fills the box's inscribed circle), bottom, top, and whether the piece
        // goes in the iron batch.
        private static readonly (ClutterKind kind,Vector4 box,float bottom,float top,bool iron)[] ClutterPieces=
        {
            // Crane loading bay, on its landing (.18). The two barrels stand in the screen column of the crane footing's
            // court finial, below its iron panels: iron, over the finial's stone. The crate stack and the sack stand behind
            // the salvage shelf's parts bin and anvil: masonry, under them.
            (ClutterKind.Barrel,new Vector4(-20.09f,10.26f,-19.51f,10.84f),.18f,.86f,true),
            (ClutterKind.Crate,new Vector4(-19.45f,9.35f,-18.62f,10.15f),.18f,.92f,false),
            (ClutterKind.Crate,new Vector4(-19.3f,9.5f,-18.75f,10.05f),.92f,1.38f,false),
            (ClutterKind.Barrel,new Vector4(-20.18f,9.62f,-19.62f,10.18f),.18f,.84f,true),
            (ClutterKind.Sack,new Vector4(-20.15f,8.85f,-19.65f,9.35f),.18f,.56f,false),
            // On the crane footing's iron roof (1.12), in its far corner clear of the mast: iron, over the west rail behind.
            (ClutterKind.Barrel,new Vector4(-20.78f,14.32f,-20.22f,14.88f),1.12f,1.8f,true),
            (ClutterKind.Barrel,new Vector4(-20.87f,13.68f,-20.33f,14.22f),1.12f,1.78f,true),
            (ClutterKind.Crate,new Vector4(-20.9f,13.1f,-20.35f,13.6f),1.12f,1.62f,true),
            // Pipe shelf (.18): a barrel between the strip coils and the quenching tank, clear of the coils on screen, and a
            // crate in front of the tool rack, clear of the stored pipes' ends: iron, emitted after the workshop's stock.
            (ClutterKind.Barrel,new Vector4(4.74f,20.89f,5.26f,21.41f),.18f,.86f,true),
            (ClutterKind.Crate,new Vector4(6.2f,18.55f,6.95f,19.25f),.18f,.72f,true),
            // North-west boiler yard, behind the court's north-east rail from the apron corner to the court's north corner:
            // masonry, under the rail, its posts and the apron lantern, over the boiler's painted drum and cornice; every top
            // stays below the boiler's iron shell on screen. Tall pieces only, set between the rail's posts on screen: half a
            // unit or more behind the rail, a piece shows above it by all but the lowest ~.2 of its height.
            (ClutterKind.Barrel,new Vector4(15.46f,17.46f,16.04f,18.04f),.18f,.88f,false),
            (ClutterKind.Barrel,new Vector4(14.75f,17.15f,15.35f,17.75f),.18f,.9f,false),
            (ClutterKind.Crate,new Vector4(14.78f,15.55f,15.62f,16.35f),.18f,.98f,false),
            (ClutterKind.Barrel,new Vector4(14.94f,14.47f,15.5f,15.03f),.18f,.86f,false),
            (ClutterKind.Crate,new Vector4(14.75f,13f,15.6f,13.85f),.18f,.98f,false),
            (ClutterKind.Crate,new Vector4(14.88f,13.15f,15.42f,13.7f),.98f,1.45f,false),
            // South-west boiler corner, behind the court's east rail and in front of the squat boiler, below its dome on
            // screen: masonry. The crate rises over the rail's post in front of it and carries a bronze stock pile.
            (ClutterKind.Barrel,new Vector4(15.15f,-3.22f,15.69f,-2.68f),.18f,.84f,false),
            (ClutterKind.Crate,new Vector4(15.1f,-4.15f,15.66f,-3.45f),.18f,.98f,false),
            (ClutterKind.Ingots,new Vector4(15.17f,-4.05f,15.59f,-3.55f),.98f,1.2f,false),
        };
        // Low masonry landings for the bay and the two yards, built as the service shelves are (Block, top ClutterDeck), with
        // their foundation depth. The bay's faces are emitted after the salvage shelf's and stand behind them; its foundation
        // stops at 1.8, inside the screen area the shelf's cap covers, so it never paints over the shelf's own camera face.
        // The yards' landings stand behind the rail, which hides all but their top.
        private static readonly (Vector4 box,float depth)[] ClutterLandings=
        {
            (new Vector4(-20.2f,8.4f,-18.45f,11f),1.8f),(new Vector4(14.6f,12.8f,16.3f,18.3f),2.8f),
            (new Vector4(15.05f,-4.75f,15.7f,-2.5f),2.8f)
        };
        // The gate's fire baskets stand on the two end posts of the balustrade across its front (BuildBalustrade sets them at
        // v .56-1.3 and 9.7-10.44 on the u 32 edge: cap 1.12, pointed to 1.34), each on an iron socket seated over the point,
        // .19 back of the post's centre so the bowl's whole round lies off the apron. The posts carry their depth: the
        // baskets take the same footprint.
        // They stand on posts the balustrade places itself (FoundryRegionTests checks a post holds each one).
        internal static readonly float[] GateBrazierPosts={.93f,10.07f};
        internal const float GateBrazierU=32.45f;
        private const float GateBrazierRise=1.46f;
        // Clean patches of the two textures clutter is drawn on (scanned for the least grain: a masonry block interior and a
        // stretch of the iron plate's centre), with their measured mean texel, sRGB.
        private static readonly Vector2 MasonryPatch=new Vector2(.624f,.867f),IronPatch=new Vector2(.624f,.235f);
        // Masonry V3 and iron V2: measured over 51-pixel patches around the existing UV centres.
        private static readonly Color MasonryPatchMean=new Color(.234f,.2168f,.2416f),IronPatchMean=new Color(.2039f,.2181f,.2418f);
        private const float ClutterGrain=.02f,ClutterPeak=1.9f,ClutterDeck=.18f;
        // Wood, burlap, hoop iron and bronze as they should render, before the patch division (EmitClutter): the albedo the
        // region's light multiplies - LitFace on a v0 face, ShadeFace on a u0 face, ClutterLid over the lit face on a lid,
        // and IronLight round a barrel or a sack (the boilers' and the braziers' own light, brightest toward screen right).
        // Warm and a little brighter than the slate round them, so a crate reads as wood at the portrait camera.
        private static readonly Color CrateWood=new Color(.43f,.3f,.175f),BarrelWood=new Color(.34f,.25f,.17f),
            HoopIron=new Color(.25f,.25f,.27f),Burlap=new Color(.39f,.34f,.27f),SackTie=new Color(.72f,.7f,.66f),
            IngotBronze=new Color(.52f,.36f,.12f);
        private const float ClutterLid=1.18f,CrateBatten=.07f,CrateSeam=.013f;

        // Called after the workshop (its stock is emitted before the pipe shelf's iron pieces) and before the massing.
        private void BuildClutter(ArenaView arena)
        {
            foreach(var landing in ClutterLandings)
            {
                Vector4 box=landing.box;
                if(MassClear(box))Block(arena,box.x,box.y,box.z,box.w,ClutterDeck,landing.depth,"Clutter service landing");
            }
            foreach(var piece in ClutterPieces)
            {
                Vector4 box=piece.box;
                if(!MassClear(box))continue;
                Vector2 c=new Vector2((box.x+box.z)*.5f,(box.y+box.w)*.5f);
                float r=Mathf.Min(box.z-box.x,box.w-box.y)*.5f;
                var mesh=new Builder();
                switch(piece.kind)
                {
                    case ClutterKind.Crate:ClutterCrate(mesh,box,piece.bottom,piece.top);break;
                    case ClutterKind.Barrel:ClutterBarrel(mesh,c,r,piece.bottom,piece.top);break;
                    case ClutterKind.Sack:ClutterSack(mesh,c,r,piece.bottom,piece.top);break;
                    default:ClutterIngots(mesh,box,piece.bottom,piece.top);break;
                }
                // Each piece is its own structure for depth: actors behind it are hidden, and it fades in front of the hero.
                using(MassDepth(box,piece.top))EmitClutter(arena,mesh,piece.iron,Project(c.x,c.y)+Vector3.up*((piece.bottom+piece.top)*.5f));
            }
            foreach(float v in GateBrazierPosts)
            {
                if(!MassClear(new Vector4(GateBrazierU-.44f,v-.44f,GateBrazierU+.44f,v+.44f)))continue;
                using(MassDepth(new Vector4(32,v-.37f,32.52f,v+.37f),GateBrazierRise+1.51f))
                {
                    // A short cast socket over the post's point, under the basket's foot; then the bridge head's basket.
                    var socket=new Builder();
                    Vector2 seat=new Vector2(32.38f,v);
                    MassCourse(socket,seat,.22f,.2f,1.12f,GateBrazierRise-.1f,6,true,new Color(.66f,.66f,.71f));
                    MassDisk(socket,seat,.2f,GateBrazierRise-.1f,6,true,new Color(.4f,.41f,.46f));
                    Create("Gate brazier sockets",socket,arena.PlatformMaterials.FoundryIron,-10.5f);
                    Brazier(arena,new Vector2(GateBrazierU,v),GateBrazierRise);
                }
            }
        }

        // Every clutter vertex samples one clean patch of its batch's texture about the piece's anchor (ClutterGrain of the
        // texture per world unit: a few dozen texels of grain across a crate, no joint, rivet or crack), and every colour,
        // authored as the tone it should render, is divided by the patch's mean texel - the project renders in gamma space,
        // so a vertex colour multiplies the texel directly. A peak channel over ClutterPeak scales down with its hue, inside
        // the +/-2 the sprite shaders' fixed4 colour interpolator guarantees (FoundryRegionTests).
        private void EmitClutter(ArenaView arena,Builder b,bool iron,Vector3 anchor)
        {
            Vector2 patch=iron?IronPatch:MasonryPatch;
            Color mean=iron?IronPatchMean:MasonryPatchMean;
            for(int i=0;i<b.V.Count;i++)
            {
                Vector3 offset=b.V[i]-anchor;
                b.U[i]=patch+new Vector2(offset.x,offset.y)*ClutterGrain;
                Color c=b.C[i];
                c=new Color(c.r/mean.r,c.g/mean.g,c.b/mean.b,1);
                float peak=Mathf.Max(c.r,Mathf.Max(c.g,c.b));
                if(peak>ClutterPeak){float scale=ClutterPeak/peak;c.r*=scale;c.g*=scale;c.b*=scale;}
                b.C[i]=c;
            }
            var set=arena.PlatformMaterials;
            Create(iron?"Foundry clutter on iron":"Foundry clutter on masonry",b,iron?set.FoundryIron:set.FoundryWall,-11);
        }

        // A plank crate: the two camera faces and the lid, then over each face a frame of darker battens round its edges and
        // two board seams between them, and over the lid two seams and a lit arris along both front edges.
        private static void ClutterCrate(Builder b,Vector4 box,float bottom,float top)
        {
            Color lit=Tinted(LitFace,CrateWood),shade=Tinted(ShadeFace,CrateWood),lid=lit*ClutterLid;
            Vector2 left=new Vector2(box.x,box.w),front=new Vector2(box.x,box.y),right=new Vector2(box.z,box.y);
            CrateSide(b,left,front,bottom,top,shade);
            CrateSide(b,front,right,bottom,top,lit);
            float du=CrateSeam/(box.z-box.x),dv=CrateSeam/(box.w-box.y);
            LidBand(b,box,top,0,1,0,1,lid);
            for(int k=0;k<3;k++)
                LidBand(b,box,top,0,1,k/3f,(k+1)/3f,lid*(k==1?.91f:1.035f));
            for(int k=1;k<3;k++)LidBand(b,box,top,0,1,k/3f-dv,k/3f+dv,lid*.55f);
            LidBand(b,box,top,0,1,0,2*dv,lid*1.12f);
            LidBand(b,box,top,0,2*du,0,1,lid*1.12f);
        }

        // One camera face of a crate, from design point a to c: the boards, the frame and the seams.
        private static void CrateSide(Builder b,Vector2 a,Vector2 c,float bottom,float top,Color face)
        {
            float s=Mathf.Min(CrateBatten/Vector2.Distance(a,c),.2f),h=Mathf.Min(CrateBatten,(top-bottom)*.2f);
            Color batten=face*.74f,seam=face*.5f;
            FaceBand(b,a,c,0,1,bottom,top,face);
            // Broad board tones survive the portrait camera better than noisy wood grain.
            for(int k=0;k<3;k++)
            {
                float lo=Mathf.Lerp(bottom+h,top-h,k/3f),hi=Mathf.Lerp(bottom+h,top-h,(k+1)/3f);
                FaceBand(b,a,c,s,1-s,lo,hi,face*(k==1?.88f:1.08f));
                FaceBand(b,a,c,s+.05f,1-s-.07f,lo+(hi-lo)*.60f,lo+(hi-lo)*.64f,face*.83f);
            }
            FaceBand(b,a,c,0,s,bottom,top,batten);FaceBand(b,a,c,1-s,1,bottom,top,batten);
            FaceBand(b,a,c,s,1-s,bottom,bottom+h,batten);FaceBand(b,a,c,s,1-s,top-h,top,batten);
            for(int k=1;k<3;k++)
            {
                float y=Mathf.Lerp(bottom+h,top-h,k/3f);
                FaceBand(b,a,c,s,1-s,y-CrateSeam,y+CrateSeam,seam);
            }
            // A diagonal brace belongs to the face plane, not a screen-space sticker.
            float low=bottom+h*1.15f,high=top-h*1.15f;
            Vector3 p=Project(Mathf.Lerp(a.x,c.x,s),Mathf.Lerp(a.y,c.y,s));
            Vector3 q=Project(Mathf.Lerp(a.x,c.x,1-s),Mathf.Lerp(a.y,c.y,1-s));
            b.Quad(p+Vector3.up*(low-h*.28f),q+Vector3.up*(high-h*.28f),
                q+Vector3.up*(high+h*.28f),p+Vector3.up*(low+h*.28f),false,face*.87f);
            foreach(float t in new[]{.12f,.88f})
            {
                float along=Mathf.Lerp(s,1-s,t),y=Mathf.Lerp(low,high,t);
                FaceBand(b,a,c,along-.014f,along+.014f,y-.012f,y+.012f,Tinted(HoopIron,LitFace));
            }
        }

        // A band of a vertical face from design point a to c: s0..s1 of its length, h0..h1 high.
        private static void FaceBand(Builder b,Vector2 a,Vector2 c,float s0,float s1,float h0,float h1,Color color)
        {
            Vector3 p=Project(Mathf.Lerp(a.x,c.x,s0),Mathf.Lerp(a.y,c.y,s0)),q=Project(Mathf.Lerp(a.x,c.x,s1),Mathf.Lerp(a.y,c.y,s1));
            b.Quad(p+Vector3.up*h0,q+Vector3.up*h0,q+Vector3.up*h1,p+Vector3.up*h1,false,color);
        }

        // A band of a crate's lid at height h: su0..su1 of its u run, sv0..sv1 of its v run.
        private static void LidBand(Builder b,Vector4 box,float h,float su0,float su1,float sv0,float sv1,Color color)
        {
            float u0=Mathf.Lerp(box.x,box.z,su0),u1=Mathf.Lerp(box.x,box.z,su1),v0=Mathf.Lerp(box.y,box.w,sv0),v1=Mathf.Lerp(box.y,box.w,sv1);
            Vector3 lift=Vector3.up*h;
            b.Quad(Project(u0,v0)+lift,Project(u1,v0)+lift,Project(u1,v1)+lift,Project(u0,v1)+lift,false,color);
        }

        // An iron-banded barrel: two courses of six staves bellied at mid-height (every other stave a shade darker, so the
        // staves read round the front), two iron hoops proud of them and a plank lid, on the massing's round courses.
        private static void ClutterBarrel(Builder b,Vector2 c,float r,float bottom,float top)
        {
            float h=top-bottom,end=r*.86f,mid=bottom+h*.5f;
            int first=b.C.Count;
            MassCourse(b,c,end,r,bottom,mid,6,true,BarrelWood);
            MassCourse(b,c,r,end,mid,top,6,true,BarrelWood);
            for(int i=first;i<b.C.Count;i++)if((i-first)/4%2==1)b.C[i]*=.88f;
            // Recessed stave joins follow the existing polygonal barrel surface.
            for(int k=1;k<6;k++)
            {
                float angle=FrontAngle(k,6),spread=.012f;
                Color seam=Tinted(IronLight(angle),BarrelWood)*.48f;
                b.Quad(OnRing(c,end,angle-spread,bottom),OnRing(c,end,angle+spread,bottom),
                    OnRing(c,r,angle+spread,mid),OnRing(c,r,angle-spread,mid),false,seam);
                b.Quad(OnRing(c,r,angle-spread,mid),OnRing(c,r,angle+spread,mid),
                    OnRing(c,end,angle+spread,top),OnRing(c,end,angle-spread,top),false,seam);
            }
            foreach(float t in new[]{.12f,.8f})
            {
                // The stave line's radius at the hoop's middle, then a little proud of it.
                float ring=Mathf.Lerp(end,r,1-Mathf.Abs(t+.04f-.5f)*2)*1.04f;
                MassCourse(b,c,ring,ring,bottom+h*t,bottom+h*(t+.08f),6,true,HoopIron);
                MassCourse(b,c,ring,ring,bottom+h*(t+.065f),bottom+h*(t+.08f),6,true,HoopIron*1.2f);
            }
            MassDisk(b,c,end,top,6,true,Tinted(LitFace,CrateWood)*ClutterLid);
            // Lid joins stay inside the twelve-sided rim rather than crossing its silhouette.
            foreach(float offset in new[]{-.25f,.25f})
            {
                float v=c.y+end*offset,half=end*.78f;
                FaceLidStroke(b,new Vector2(c.x-half,v),new Vector2(c.x+half,v),top,.012f,
                    Tinted(LitFace,BarrelWood)*.55f);
            }
            MassDisk(b,c+new Vector2(end*.22f,0),end*.12f,top+.002f,4,true,Tinted(LitFace,BarrelWood)*.7f);
        }

        // A tied sack: a burlap belly at half height drawing in to a broad shoulder, closed by a disc, and a small tie on top;
        // five facets, so it sits lumpier than a barrel.
        private static void ClutterSack(Builder b,Vector2 c,float r,float bottom,float top)
        {
            float h=top-bottom,belly=bottom+h*.45f,neck=bottom+h*.88f;
            Color tie=Tinted(Burlap,SackTie);
            MassCourse(b,c,r*.94f,r,bottom,belly,5,true,Burlap);
            MassCourse(b,c,r,r*.62f,belly,neck,5,true,Burlap);
            MassDisk(b,c,r*.62f,neck,5,true,Tinted(LitFace,Burlap));
            MassCourse(b,c,r*.2f,r*.28f,neck,top,5,true,tie);
            MassDisk(b,c,r*.28f,top,5,true,Tinted(LitFace,tie));
            // Tension folds follow the front facets into the gathered neck.
            for(int k=0;k<5;k++)
            {
                float a=FrontAngle(k,5),e=FrontAngle(k+1,5);
                Vector3 lo=Vector3.Lerp(OnRing(c,r*.94f,a,bottom),OnRing(c,r*.94f,e,bottom),.42f);
                Vector3 bulge=Vector3.Lerp(OnRing(c,r,a,belly),OnRing(c,r,e,belly),.48f);
                Vector3 shoulder=Vector3.Lerp(OnRing(c,r*.62f,a,neck),OnRing(c,r*.62f,e,neck),.55f);
                Color fold=Tinted(IronLight((a+e)*.5f),Burlap)*.7f;
                ArchitectureBar(b,Vector3.Lerp(lo,bulge,.35f),bulge,.013f,fold);
                ArchitectureBar(b,bulge,shoulder,.013f,fold);
            }
            MassCourse(b,c,r*.235f,r*.24f,neck+h*.025f,neck+h*.065f,5,true,tie*1.1f);
        }

        private static void FaceLidStroke(Builder b,Vector2 a,Vector2 c,float height,float width,Color tone)
        {
            Vector2 n=new Vector2(-(c-a).y,(c-a).x).normalized*width*.5f;
            Vector3 up=Vector3.up*height;
            b.Quad(Project(a.x+n.x,a.y+n.y)+up,Project(c.x+n.x,c.y+n.y)+up,
                Project(c.x-n.x,c.y-n.y)+up,Project(a.x-n.x,a.y-n.y)+up,false,tone);
        }

        // A bronze stock pile in three courses - two bars along v, two across them, one along v on top - each course far to
        // near.
        private static void ClutterIngots(Builder b,Vector4 box,float bottom,float top)
        {
            float bar=(top-bottom)/3,w=box.z-box.x,d=box.w-box.y;
            IngotBar(b,new Vector4(box.z-w*.46f,box.y,box.z,box.w),bottom,bottom+bar);
            IngotBar(b,new Vector4(box.x,box.y,box.x+w*.46f,box.w),bottom,bottom+bar);
            IngotBar(b,new Vector4(box.x,box.w-d*.44f,box.z,box.w-d*.06f),bottom+bar,bottom+2*bar);
            IngotBar(b,new Vector4(box.x,box.y+d*.06f,box.z,box.y+d*.44f),bottom+bar,bottom+2*bar);
            IngotBar(b,new Vector4(box.x+w*.27f,box.y+d*.1f,box.z-w*.27f,box.w-d*.1f),bottom+2*bar,top);
        }

        // A cast bar, as WorkshopMetalHull builds one: its two camera faces to the shoulder, four bevels to an inset top (the
        // front one lit), in bronze.
        private static void IngotBar(Builder b,Vector4 box,float bottom,float top)
        {
            Color lit=Tinted(LitFace,IngotBronze),shade=Tinted(ShadeFace,IngotBronze);
            float bevel=Mathf.Min(.035f,Mathf.Min(box.z-box.x,box.w-box.y)*.2f),shoulder=Mathf.Lerp(bottom,top,.6f);
            var lower=new[]{Project(box.x,box.y),Project(box.z,box.y),Project(box.z,box.w),Project(box.x,box.w)};
            var inset=new[]{Project(box.x+bevel,box.y+bevel),Project(box.z-bevel,box.y+bevel),
                Project(box.z-bevel,box.w-bevel),Project(box.x+bevel,box.w-bevel)};
            Vector3 low=Vector3.up*bottom,high=Vector3.up*shoulder,crown=Vector3.up*top;
            b.Quad(lower[3]+low,lower[0]+low,lower[0]+high,lower[3]+high,false,shade);
            b.Quad(lower[0]+low,lower[1]+low,lower[1]+high,lower[0]+high,false,lit);
            for(int e=0;e<4;e++)
            {
                int n=(e+1)%4;
                b.Quad(lower[e]+high,lower[n]+high,inset[n]+crown,inset[e]+crown,false,e==0?lit*1.18f:e==3?shade*1.1f:shade*.8f);
            }
            b.Quad(inset[0]+crown,inset[1]+crown,inset[2]+crown,inset[3]+crown,false,lit*1.12f);
        }
    }
}
