using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private void BuildSiteFinish(ArenaView arena)
        {
            BuildBridgePiers(arena);
            BuildCourtBorders(arena);
            BuildCraneHardware(arena);
        }

        private void BuildBridgePiers(ArenaView arena)
        {
            // Paired keepers replace the temporary head-post braziers. The lower supports and
            // banners remain; all bases stand outside the walking union.
            BridgePiers(arena,BridgeHeadPosts,true);
            foreach(var post in BridgeHeadPosts)
                using(MassDepth(new Vector4(post.x-.7f,post.y-.7f,post.x+.7f,post.y+.7f),4.3f))
                {
                    BridgeKeeper(arena,post);
                    BridgeBanner(arena,post);
                }
            BuildBridgeLanterns(arena);
            BuildBridgeParapets(arena);
            BuildEntranceBridgeSupports(arena);
            BridgePiers(arena,BridgeFootPosts);
            // Continuous kerbs meet the two actual court seams. The end cap closes
            // only the physical far end; it never crosses a traversable court join.
            var joints=new Builder();
            float rail=FoundryMap.BridgeHalfWidth-Kerb*1.41421356f;
            float end=FoundryMap.BridgeStart+Kerb*1.41421356f;
            for(int i=0;i<(FoundryMap.BridgeEnd-FoundryMap.BridgeStart)/2;i++)
            {
                float s0=FoundryMap.BridgeStart+i*2;
                foreach(float d in new[]{-rail,rail})
                {
                    float s1=Mathf.Min(s0+2,d<0?FoundryMap.BridgeWestSeam:FoundryMap.BridgeEastSeam);
                    if(s1<=s0)continue;
                    Strip(joints,(s0+d)*.5f,(s0-d)*.5f,(s1+d)*.5f,(s1-d)*.5f,KerbWidth,KerbTone,CopingTrim.Stone,i*5+(d<0?0:3));
                }
            }
            float margin=FoundryMap.BridgeHalfWidth-.5f;
            for(int i=0;i<4;i++)
            {
                float d0=-margin+i*margin*.5f,d1=d0+margin*.5f;
                Strip(joints,(end+d0)*.5f,(end-d0)*.5f,(end+d1)*.5f,(end-d1)*.5f,KerbWidth,KerbTone,CopingTrim.Stone,70+i);
            }
            Create("Bridge joints",joints,arena.PlatformMaterials.Coping,FloorDecal);
        }

        // Stone piers whose entire feet are outside the diagonal bridge and court walking union. Unscoped: their pieces
        // resolve to the piers' landmarks (RegisterDepthLandmarks).
        private void BridgePiers(ArenaView arena,Vector2[] posts,bool keeper=false)
        {
            var stone=new Builder();
            var iron=new Builder();
            foreach(var p in posts)
            {
                // A seated lower foot and deck-bearing shoulder break the long support
                // into structural courses. The recessed stem remains inside the old
                // shaft envelope; bottom, deck height and depth ownership are unchanged.
                LitFoundryTier(stone,p.x-.3f,p.y-.3f,p.x+.3f,p.y+.3f,-2.8f,-2.35f,.03f);
                LitFoundryTier(stone,p.x-.245f,p.y-.245f,p.x+.245f,p.y+.245f,-2.35f,-.18f,.005f);
                LitFoundryTier(stone,p.x-.3f,p.y-.3f,p.x+.3f,p.y+.3f,-.18f,.3f,.025f);
                if(keeper)
                {
                    // A below-deck console ties the outboard statue shaft back to the bridge rim.
                    float side=Mathf.Sign(p.x-p.y),sum=p.x+p.y;
                    var rim=new[]{Deck(sum-.9f,side*(FoundryMap.BridgeHalfWidth-.05f)),Deck(sum-.9f,side*(FoundryMap.BridgeHalfWidth+2.6f)),
                        Deck(sum+.9f,side*(FoundryMap.BridgeHalfWidth+2.6f)),Deck(sum+.9f,side*(FoundryMap.BridgeHalfWidth-.05f))};
                    for(int edge=0;edge<4;edge++)
                    {
                        Vector3 a=rim[edge],b=rim[(edge+1)%4];
                        stone.WallQuad(a-Vector3.up*.7f,b-Vector3.up*.7f,b-Vector3.up*.1f,a-Vector3.up*.1f,false,new Color(.65f,.65f,.73f));
                    }
                    stone.Quad(rim[0]-Vector3.up*.1f,rim[1]-Vector3.up*.1f,rim[2]-Vector3.up*.1f,rim[3]-Vector3.up*.1f,true,new Color(.8f,.8f,.88f));
                    LitFoundryTier(stone,p.x-.48f,p.y-.48f,p.x+.48f,p.y+.48f,-.18f,.18f,.07f);
                    DressedKeeperSeat(stone,p);
                    continue;
                }
                LitFoundryTier(stone,p.x-.24f,p.y-.24f,p.x+.24f,p.y+.24f,.3f,1.1f,.03f);
                LitFoundryTier(stone,p.x-.3f,p.y-.3f,p.x+.3f,p.y+.3f,1.1f,1.28f,.04f);
                LitFoundryTier(stone,p.x-.21f,p.y-.21f,p.x+.21f,p.y+.21f,1.28f,1.65f,.14f);
                // A shallow socket finishes the existing crown; the brazier remains
                // fixed at height 1.65 and none of this changes the pier footprint.
                var cap=Octagon(p.x-.08f,p.y-.08f,p.x+.08f,p.y+.08f,.02f);
                for(int i=0;i<cap.Length;i++)
                {
                    int j=(i+1)%cap.Length;
                    FullFace(iron,cap[i]+Vector3.up*1.635f,cap[j]+Vector3.up*1.635f,
                        cap[j]+Vector3.up*1.675f,cap[i]+Vector3.up*1.675f,new Color(.34f,.38f,.46f));
                }
                PierPanels(iron,p,.242f,.16f,.44f,.5f,.105f);
            }
            Create("Bridge gateposts",stone,arena.PlatformMaterials.FoundryWall,-11);
            Create("Bridge gatepost sockets and seals",iron,arena.PlatformMaterials.FoundryIron,-10.5f);
        }

        private void BridgeKeeper(ArenaView arena,Vector2 post)
        {
            var material=arena.PlatformMaterials.ForgeKeeper;
            if(material==null)return;
            // Preserve the source's 2:3 aspect and baked lighting on both sides; never mirror it.
            const float height=3.8f,width=height*2f/3f;
            Vector3 foot=Project(post.x,post.y)+Vector3.up*.38f;
            Vector3 a=foot-Vector3.right*(width*.5f),b=foot+Vector3.right*(width*.5f);
            var card=new Builder();
            card.Quad(a,b,b+Vector3.up*height,a+Vector3.up*height,false,Color.white);
            card.U.Clear();card.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            Create("Bridge forge keepers",card,material,-10);
        }

        // Quiet cast-iron panels face both approaches, with a small incised lozenge instead of signs or glowing UI-like
        // decoration: on a post's v0 and u0 faces, reach out from its centre, half wide along the face, from bottom up.
        private static void PierPanels(Builder iron,Vector2 p,float reach,float half,float bottom,float height,float lozenge)
        {
            foreach(var edge in new[]{new[]{Project(p.x-half,p.y-reach),Project(p.x+half,p.y-reach)},
                new[]{Project(p.x-reach,p.y+half),Project(p.x-reach,p.y-half)}})
            {
                Vector3 a=edge[0]+Vector3.up*bottom,b=edge[1]+Vector3.up*bottom;
                FullFace(iron,a,b,b+Vector3.up*height,a+Vector3.up*height,new Color(.25f,.29f,.37f));
                Vector3 center=(a+b)*.5f+Vector3.up*(height*.5f);
                Vector3 side=(b-a)*.28f,up=Vector3.up*lozenge;
                ArchitectureBar(iron,center-up,center+side,.015f,new Color(.7f,.57f,.37f));
                ArchitectureBar(iron,center+side,center+up,.015f,new Color(.7f,.57f,.37f));
                ArchitectureBar(iron,center+up,center-side,.015f,new Color(.7f,.57f,.37f));
                ArchitectureBar(iron,center-side,center-up,.015f,new Color(.7f,.57f,.37f));
            }
        }

        // Two separate lantern rows on the longer causeway. Their roof hulls sit
        // wholly outside the walking quad and each row has room below the next.
        internal static readonly Vector2[] BridgeLanterns=
        {
            DeckDesign((2*BridgeHeadS+BridgeFootS)/3,-(FoundryMap.BridgeHalfWidth+.7f)),
            DeckDesign((2*BridgeHeadS+BridgeFootS)/3,FoundryMap.BridgeHalfWidth+.7f),
            DeckDesign((BridgeHeadS+2*BridgeFootS)/3,-(FoundryMap.BridgeHalfWidth+.7f)),
            DeckDesign((BridgeHeadS+2*BridgeFootS)/3,FoundryMap.BridgeHalfWidth+.7f)
        };

        private void BuildBridgeLanterns(ArenaView arena)
        {
            var set=arena.PlatformMaterials;
            foreach(var p in BridgeLanterns)
                using(MassDepth(new Vector4(p.x-.32f,p.y-.32f,p.x+.32f,p.y+.32f),2.75f))
                {
                    // Bottom-up, each course painting over the top of the one under it.
                    var stone=new Builder();
                    LitFoundryTier(stone,p.x-.19f,p.y-.19f,p.x+.19f,p.y+.19f,-.6f,-.3f,.02f);
                    LitFoundryTier(stone,p.x-.27f,p.y-.27f,p.x+.27f,p.y+.27f,-.3f,0,.02f);
                    LitFoundryTier(stone,p.x-.32f,p.y-.32f,p.x+.32f,p.y+.32f,0,.22f,.03f);
                    LitFoundryTier(stone,p.x-.22f,p.y-.22f,p.x+.22f,p.y+.22f,.22f,1.49f,.01f);
                    LitFoundryTier(stone,p.x-.32f,p.y-.32f,p.x+.32f,p.y+.32f,1.49f,1.69f,.04f);
                    Create("Bridge lantern posts",stone,set.FoundryWall,-11);
                    var iron=new Builder();
                    PierPanels(iron,p,.222f,.13f,.78f,.5f,.09f);
                    BuildCourtLantern(arena,iron,p);
                    Create("Bridge lantern ironwork",iron,set.FoundryIron,-10.5f);
                }
        }

        private const float BridgeHeadS=FoundryMap.BridgeWestSeam-3.3f,BridgeHeadD=FoundryMap.BridgeHalfWidth+1.8f;
        private const float BridgeFootS=FoundryMap.BridgeStart-.6f,BridgeFootD=FoundryMap.BridgeHalfWidth+.7f;
        internal static readonly Vector2[] BridgeHeadPosts={DeckDesign(BridgeHeadS,-BridgeHeadD),DeckDesign(BridgeHeadS,BridgeHeadD)};
        internal static readonly Vector2[] BridgeFootPosts={DeckDesign(BridgeFootS,-BridgeFootD),DeckDesign(BridgeFootS,BridgeFootD)};
        private static Vector4 BridgePostBox(float s,float d,float half)
        {
            Vector2 p=DeckDesign(s,d);return new Vector4(p.x-half,p.y-half,p.x+half,p.y+half);
        }
        // Camera-facing cloth hangs outside the guardian's support outside the widened guardian support.
        // The top bracket sits under the broad cap; the cloth remains in the existing facade batch.
        private const float BannerInner=FoundryMap.BridgeHalfWidth+2.65f,BannerWidth=.83f,BannerTop=.4f;
        // The facade's left banner (GateFacade.png, 1536 x 1024; pixels, y down), from the painted rod's shaft (y 100, under
        // the bracket's bar) to the swallow-tailed hem, measured and inset 1.8 px inside its gold trim so no stone around it
        // is sampled: the hem across its five columns - the edges x 288.3 / 416.7, the notches x 311 / 395 and the centre
        // tip x 352.5 - each with its lowest y. Six rows per column allow a gentle cloth curve.
        private static readonly Vector2[] BannerHem=
        {
            new Vector2(288.3f,568.6f),new Vector2(311,534.7f),new Vector2(352.5f,598.2f),new Vector2(395,530.6f),
            new Vector2(416.7f,566.1f)
        };

        private void BridgeBanner(ArenaView arena,Vector2 post)
        {
            var facade=arena.PlatformMaterials.GateFacade;
            if(facade==null)return;
            float s=post.x+post.y,side=post.x<post.y?-1:1,scale=BannerWidth/(BannerHem[4].x-BannerHem[0].x);
            float outer=BannerInner+BannerWidth/FoundryMap.Axis,d0=side<0?-outer:BannerInner;
            // Texture pixel to card point (screen x grows with d), and card-plane point to world.
            Vector3 At(float d,float h)=>Project((s+d)*.5f,(s-d)*.5f)+Vector3.up*h;
            Vector3 Card(float x,float y)=>At(d0+(x-BannerHem[0].x)*scale/FoundryMap.Axis,BannerTop-(y-100)*scale);
            // Cool ambient stone light above, shaded violet toward the hem in the void.
            Color head=new Color(.94f,.95f,1f),hem=new Color(.8f,.8f,.9f);
            var card=new Builder();
            for(int i=0;i+1<BannerHem.Length;i++)
            for(int row=0;row<6;row++)
            {
                Vector2 a=BannerHem[i],b=BannerHem[i+1];
                float top=row/6f,bottom=(row+1)/6f;
                var pixels=new[]{new Vector2(a.x,Mathf.Lerp(100,a.y,top)),new Vector2(b.x,Mathf.Lerp(100,b.y,top)),
                    new Vector2(b.x,Mathf.Lerp(100,b.y,bottom)),new Vector2(a.x,Mathf.Lerp(100,a.y,bottom))};
                int start=card.V.Count;
                card.Quad(Card(pixels[0].x,pixels[0].y),Card(pixels[1].x,pixels[1].y),Card(pixels[2].x,pixels[2].y),
                    Card(pixels[3].x,pixels[3].y),false,Color.white);
                for(int k=0;k<4;k++)
                {
                    card.U[start+k]=new Vector2(pixels[k].x/1536,1-pixels[k].y/1024);
                    card.C[start+k]=Color.Lerp(head,hem,(pixels[k].y-100)/(BannerHem[2].y-100));
                    float drop=(pixels[k].y-100)/(BannerHem[2].y-100);
                    card.Cloth.Add(new FoundryBannerVertex(start+k,drop,side*.7f));
                }
            }
            Create("Bridge head banners",card,facade,-12);
            // Bracket: a bronze bar through the painted loops (on the shaft's centre line, y 105), a knob past the cloth
            // and a short brace from the post's face to the bar, inside the gap before the cloth.
            var iron=new Builder();
            float rod=BannerTop-5*scale,end=side*(outer+.2f);
            ArchitectureBar(iron,At(side*6.4f,rod),At(end,rod),.075f,new Color(.62f,.47f,.28f));
            ArchitectureBar(iron,At(side*6.4f,rod+.022f),At(end,rod+.022f),.022f,new Color(.98f,.8f,.5f));
            Vector3 knob=At(end,rod);
            FullFace(iron,knob+Vector3.up*.07f,knob+Vector3.right*.065f,knob+Vector3.down*.07f,knob+Vector3.left*.065f,
                new Color(.9f,.7f,.42f));
            ArchitectureBar(iron,At(side*6.4f,rod-.34f),At(side*6.58f,rod-.01f),.05f,new Color(.34f,.35f,.41f));
            Create("Bridge head banner brackets",iron,arena.PlatformMaterials.FoundryIron,-10.5f);
        }

        private const float Kerb=.2f,KerbWidth=.3f;
        private static readonly Color KerbTone=new Color(.92f,.9f,.88f);
        private void BuildCourtBorders(ArenaView arena)
        {
            var borders=new Builder();
            // Broad quiet paving bands; every point remains on the authored floor.
            foreach(var cell in _cells)
            {
                int x=cell.x,y=cell.y;
                foreach(var offset in new[]{Vector2Int.down,Vector2Int.left,Vector2Int.up,Vector2Int.right})
                {
                    if(FoundryMap.UncutCell(x+offset.x,y+offset.y)||!CourtEdgeVisible(x+.5f+offset.x*.5f,y+.5f+offset.y*.5f))continue;
                    // A continuous kerb course along every open edge, one kerb stone per cell. It used to be a 0.8
                    // strip per cell set 0.32 in, which left a dashed row of dark bars that read as vent slots.
                    if(offset==Vector2Int.down)Strip(borders,x,y+Kerb,x+1,y+Kerb,KerbWidth,KerbTone,CopingTrim.Stone,x*7+y*13+offset.x*3+offset.y);
                    if(offset==Vector2Int.up)Strip(borders,x,y+1-Kerb,x+1,y+1-Kerb,KerbWidth,KerbTone,CopingTrim.Stone,x*7+y*13+offset.x*3+offset.y);
                    if(offset==Vector2Int.left)Strip(borders,x+Kerb,y,x+Kerb,y+1,KerbWidth,KerbTone,CopingTrim.Stone,x*7+y*13+offset.x*3+offset.y);
                    if(offset==Vector2Int.right)Strip(borders,x+1-Kerb,y,x+1-Kerb,y+1,KerbWidth,KerbTone,CopingTrim.Stone,x*7+y*13+offset.x*3+offset.y);
                }
            }
            Create("Court worn borders",borders,arena.PlatformMaterials.Coping,FloorDecal);
            // The posts' stone is emitted with the balustrade, among its far-to-near pieces (CourtFinialMasonry); here
            // only their iron: lantern cages and panes, finial tips and the inset panels. Each post's iron takes the same
            // depth footprint as its stone - the one its landmark registers - as a scope: resolved by volume instead, the
            // north-west lantern's went to the crane's landmark in front of it, hiding and fading over actors standing
            // between the two.
            foreach(var p in CourtFinials)
                using(MassDepth(new Vector4(p.x-.4f,p.y-.4f,p.x+.4f,p.y+.4f),2.85f))
                {
                    var caps=new Builder();
                    if(CourtLantern(p))BuildCourtLantern(arena,caps,p);
                    else TintedIronTier(caps,p.x-.09f,p.y-.09f,p.x+.09f,p.y+.09f,2.10f,2.25f,.025f,new Color(.62f,.52f,.37f));
                    foreach(var face in new[]{new[]{Project(p.x-.255f,p.y-.18f),Project(p.x-.255f,p.y+.18f)},
                        new[]{Project(p.x-.18f,p.y-.255f),Project(p.x+.18f,p.y-.255f)}})
                    {
                        Vector3 a=face[0]+Vector3.up*.35f,b=face[1]+Vector3.up*.35f,up=Vector3.up*.48f;
                        FullFace(caps,a,b,b+up,a+up,new Color(.19f,.23f,.30f));
                        ArchitectureBar(caps,a,b,.035f,new Color(.58f,.51f,.40f));
                        ArchitectureBar(caps,a+up,b+up,.035f,new Color(.68f,.61f,.48f));
                        Vector3 center=(a+b+up)*.5f;
                        ArchitectureBar(caps,center-Vector3.up*.12f,center+Vector3.up*.12f,.045f,
                            new Color(.47f,.50f,.57f));
                    }
                    Create("Outer post inset metal",caps,arena.PlatformMaterials.FoundryIron,-10.5f);
                }
        }

        private static bool CourtLantern(Vector2 p)
        {
            // The visible cage and its baked pool must always share one placement.
            foreach(Vector2 lamp in CourtLanternPosts)if(p==lamp)return true;
            return false;
        }

        // A court post's stone, bottom-up: a stronger stepped silhouette than the old exterior posts - plinth, shaft, a mid
        // band and the cap course, and a crowning block on the finials (the lanterns carry their cage there). The
        // court-facing edge stays outside the shared walkable union. BuildBalustrade emits it among the rail pieces, far
        // to near: emitted before them, with the court borders, the span leaving a post's far side painted over its front.
        private static void CourtFinialMasonry(Builder masonry,Vector2 p)
        {
            LitFoundryTier(masonry,p.x-.38f,p.y-.38f,p.x+.38f,p.y+.38f,0,.22f,.025f);
            LitFoundryTier(masonry,p.x-.29f,p.y-.29f,p.x+.29f,p.y+.29f,.22f,1.48f,.035f);
            LitFoundryTier(masonry,p.x-.34f,p.y-.34f,p.x+.34f,p.y+.34f,.92f,1.06f,.025f);
            LitFoundryTier(masonry,p.x-.38f,p.y-.38f,p.x+.38f,p.y+.38f,1.48f,1.69f,.04f);
            if(!CourtLantern(p))LitFoundryTier(masonry,p.x-.29f,p.y-.29f,p.x+.29f,p.y+.29f,1.69f,2.10f,.20f);
        }

        private void BuildCourtLantern(ArenaView arena,Builder iron,Vector2 p)
        {
            // Four small enclosed lamps replace existing post tips, supported by
            // the same cap. Scenery only; no particles or per-frame light work.
            WorkshopMetalHull(iron,p.x-.27f,p.y-.27f,p.x+.27f,p.y+.27f,1.69f,1.79f,.035f);
            var glow=new Builder();
            int face=0;
            foreach(var edge in new[]{new[]{Project(p.x-.21f,p.y-.21f),Project(p.x+.21f,p.y-.21f)},
                new[]{Project(p.x-.21f,p.y+.21f),Project(p.x-.21f,p.y-.21f)}})
            {
                Vector3 a=edge[0]+Vector3.up*1.81f,b=edge[1]+Vector3.up*1.81f,up=Vector3.up*.56f;
                // Enclosed amber glass stays inside the existing cage openings.
                // A quiet hot patch and a soot gradient distinguish it from exposed fire.
                foreach(float start in new[]{.15f,.58f})
                {
                    Vector3 l=Vector3.Lerp(a,b,start)+Vector3.up*.035f;
                    Vector3 r=Vector3.Lerp(a,b,start+.27f)+Vector3.up*.035f;
                    LanternPane(glow,l,r,up-Vector3.up*.07f,face==0?1f:.84f);
                }
                ArchitectureBar(iron,a,a+up,.065f,new Color(.70f,.60f,.43f));
                ArchitectureBar(iron,b,b+up,.065f,new Color(.70f,.60f,.43f));
                Vector3 middle=(a+b)*.5f;
                ArchitectureBar(iron,middle,middle+up,.04f,new Color(.40f,.36f,.31f));
                face++;
            }
            // Share the existing flame batch; inset pane geometry leaves the cage clear.
            if(arena.PlatformMaterials.FoundryEmbers!=null)
                Create("Court lantern panes",glow,arena.PlatformMaterials.FoundryEmbers,-10);
            WorkshopMetalHull(iron,p.x-.28f,p.y-.28f,p.x+.28f,p.y+.28f,2.37f,2.48f,.025f);
            Vector3 peak=Project(p.x,p.y)+Vector3.up*2.75f;
            foreach(var edge in new[]{new[]{Project(p.x-.24f,p.y-.24f),Project(p.x+.24f,p.y-.24f)},
                new[]{Project(p.x-.24f,p.y+.24f),Project(p.x-.24f,p.y-.24f)}})
            {
                Vector3 a=edge[0]+Vector3.up*2.48f,b=edge[1]+Vector3.up*2.48f;
                FullFace(iron,a,b,peak,peak,new Color(.56f,.59f,.66f));
                ArchitectureBar(iron,a,b,.035f,new Color(.85f,.77f,.61f));
                ArchitectureBar(iron,a,peak,.025f,new Color(.67f,.70f,.76f));
            }
        }

        private static void LanternPane(Builder glow,Vector3 left,Vector3 right,Vector3 rise,float facing)
        {
            Color lower=new Color(.93f*facing,.60f*facing,.55f*facing,1);
            Color upper=new Color(.48f*facing,.27f*facing,.32f*facing,1);
            int first=glow.U.Count;
            FullFace(glow,left,right,right+rise,left+rise,lower);
            glow.C[first+2]=upper;glow.C[first+3]=upper;
            // Measured smooth orange patch in FoundryEmbers (centre RGB 247,140,9).
            // The narrow footprint avoids magnifying the black crust used by open flames.
            for(int i=first;i<glow.U.Count;i++)
                glow.U[i]=new Vector2(.7815f+glow.U[i].x*.001f,.8375f+glow.U[i].y*.001f);
        }

        private void BuildCraneHardware(ArenaView arena)
        {
            // The authored crane includes its own mast gussets and lower winch.
            if(_craneMaterial!=null)return;
            Vector3 foot=WestProject(-25.5f,13)+Vector3.up*1.1f;
            var iron=new Builder();
            // Broad gusset plates anchor the existing lattice tower to its plinth.
            foreach(float sign in new[]{-1f,1f})
            {
                Vector3 a=foot+Vector3.right*sign*.5f,b=foot+Vector3.right*sign*.15f;
                FullFace(iron,a,b,b+Vector3.up*.8f,b+Vector3.up*.8f,new Color(.6f,.63f,.7f));
                for(int i=1;i<6;i++)
                {
                    Vector3 center=foot+Vector3.up*i+Vector3.right*sign*.3f;
                    FullFace(iron,center+new Vector3(-.11f,-.12f,0),center+new Vector3(.11f,-.12f,0),
                        center+new Vector3(.11f,.12f,0),center+new Vector3(-.11f,.12f,0),new Color(.86f,.83f,.75f));
                }
            }
            Create("Crane gussets",iron,arena.PlatformMaterials.FoundryIron,-11);
            Vector3 drum=foot+Vector3.up*.68f;
            CraneBeam(arena,drum+Vector3.left*.35f,drum+Vector3.right*.35f,.42f);
            foreach(float y in new[]{-.15f,0,.15f})
                CraneBeam(arena,drum+new Vector3(-.3f,y,0),drum+new Vector3(.3f,y,0),.025f);
            BuildCraneSheave(arena,drum+Vector3.right*.38f,.3f,true);
        }

        private void BuildShaftCollar(ArenaView arena)
        {
            var collar=new Builder();
            // Segmented ledges lie inside the existing shaft, never on the walking ring.
            for(int i=0;i<8;i++)
            {
                ArchitecturalTier(collar,-11.95f+i,2.05f,-11.07f+i,2.34f,0,.18f,.025f);
                ArchitecturalTier(collar,-11.95f,2.4f+i*.9f,-11.66f,3.18f+i*.9f,0,.18f,.025f);
                ArchitecturalTier(collar,-11.95f+i,9.66f,-11.07f+i,9.95f,0,.18f,.025f);
                ArchitecturalTier(collar,-4.34f,2.4f+i*.9f,-4.05f,3.18f+i*.9f,0,.18f,.025f);
            }
            Create("Shaft lip stones",collar,arena.PlatformMaterials.FoundryWall,-11);
            var clamps=new Builder();
            // Flush iron drain trays follow the individual coping blocks. Every tray
            // stays inside the shaft footprint and leaves the stone joints exposed.
            for(int i=0;i<8;i++)
            {
                ShaftDrainTray(clamps,-11.87f+i,2.12f,-11.15f+i,2.27f);
                ShaftDrainTray(clamps,-11.87f+i,9.73f,-11.15f+i,9.88f);
                ShaftDrainTray(clamps,-11.88f,2.48f+i*.9f,-11.73f,3.1f+i*.9f);
                ShaftDrainTray(clamps,-4.27f,2.48f+i*.9f,-4.12f,3.1f+i*.9f);
            }
            // Four narrow retaining saddles wrap the visible ledges; below-deck legs
            // use the existing inset rank so the surrounding floor clips them.
            var anchors=new Builder();
            foreach(float t in new[]{.3f,.7f})
                foreach(var edge in new[]{new[]{Project(-11.7f,2.08f),Project(-4.3f,2.08f),Project(0,.27f)},
                    new[]{Project(-11.92f,2.5f),Project(-11.92f,9.5f),Project(.27f,0)}})
                {
                    Vector3 saddle=Vector3.Lerp(edge[0],edge[1],t);
                    Vector3 across=edge[2];
                    Vector3 along=(edge[1]-edge[0]).normalized*.045f;
                    Vector3 lift=Vector3.up*.19f;
                    FullFace(clamps,saddle-along+lift,saddle+along+lift,saddle+across+along+lift,
                        saddle+across-along+lift,new Color(.4f,.43f,.48f));
                    Vector3 inner=saddle+across;
                    FullFace(anchors,inner-along,inner+along,inner+along+Vector3.down*.62f,
                        inner-along+Vector3.down*.62f,new Color(.37f,.4f,.47f));
                    Vector3 bolt=inner+Vector3.down*.42f;
                    FullFace(anchors,bolt+Vector3.up*.05f,bolt+Vector3.right*.04f,
                        bolt+Vector3.down*.05f,bolt+Vector3.left*.04f,new Color(.72f,.58f,.39f));
                }
            Create("Shaft collar retaining saddles",clamps,arena.PlatformMaterials.FoundryIron,-10.5f);
            Create("Shaft collar anchor legs",anchors,arena.PlatformMaterials.FoundryIron,-15.4f);
            // Cross-beam sits below floor level; existing low scenery rank clips it at the lip.
            Vector3 a=Project(-11.6f,8.9f)+Vector3.down*.75f;
            Vector3 b=Project(-4.4f,8.9f)+Vector3.down*.75f;
            Tube(arena,a,b,.19f,-15.4f);
            foreach(float u in new[]{-10f,-8f,-6f})
            {
                Vector3 top=Project(u,8.9f)+Vector3.down*.75f;
                Chain(arena,top,top+Vector3.down*1.1f,.085f,-15.4f);
            }
            // These three cast saddles visibly join the existing chain heads to
            // their cross-beam, instead of adding unsupported fittings at the rim.
            var hangers=new Builder();
            Vector3 beamDirection=(b-a).normalized;
            Vector3 beamNormal=new Vector3(-beamDirection.y,beamDirection.x,0);
            foreach(float u in new[]{-10f,-8f,-6f})
            {
                Vector3 top=Project(u,8.9f)+Vector3.down*.75f;
                Vector3 side=beamDirection*.085f,wrap=beamNormal*.125f;
                FullFace(hangers,top-side-wrap,top+side-wrap,top+side+wrap,top-side+wrap,
                    new Color(.61f,.62f,.65f));
                ArchitectureBar(hangers,top+side+wrap,top-side+wrap,.025f,new Color(.79f,.73f,.6f));
                ArchitectureBar(hangers,top+Vector3.left*.075f,top+new Vector3(-.075f,-.15f,0),
                    .035f,new Color(.48f,.5f,.55f));
                ArchitectureBar(hangers,top+Vector3.right*.075f,top+new Vector3(.075f,-.15f,0),
                    .035f,new Color(.48f,.5f,.55f));
                ArchitectureBar(hangers,top+new Vector3(-.075f,-.15f,0),top+new Vector3(.075f,-.15f,0),
                    .035f,new Color(.66f,.64f,.57f));
            }
            Create("Shaft beam chain saddles",hangers,arena.PlatformMaterials.FoundryIron,-15.4f);
        }

        private static void ShaftDrainTray(Builder iron,float x0,float y0,float x1,float y1)
        {
            Vector3 lift=Vector3.up*.185f;
            Vector3 a=Project(x0,y0)+lift,b=Project(x1,y0)+lift;
            Vector3 c=Project(x1,y1)+lift,d=Project(x0,y1)+lift;
            FullFace(iron,a,b,c,d,new Color(.15f,.18f,.23f));
            // Narrow cast rims and three broad slats imply a recessed channel;
            // all top faces share the same elevation, with no raised obstruction.
            bool horizontal=x1-x0>y1-y0;
            Vector3 start=a,end=horizontal?b:d;
            Vector3 across=horizontal?d-a:b-a;
            Color rim=new Color(.48f,.5f,.55f);
            ArchitectureBar(iron,start,end,.022f,rim);
            ArchitectureBar(iron,start+across,end+across,.022f,rim);
            for(int i=1;i<=3;i++)
            {
                Vector3 p=Vector3.Lerp(start,end,i*.25f);
                ArchitectureBar(iron,p,p+across,.035f,new Color(.56f,.55f,.52f));
            }
        }
    }
}
