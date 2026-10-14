using UnityEngine;
using Cryptforge.Art;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Modelled in the same design plane as the existing projected architecture.
        // All geometry is static and stays on the established scenery footprints.
        private void BuildGateArchitecture(ArenaView arena,float height)
        {
            BuildGateEnclosure(arena,true);
            var stone=new Builder();
            // Continuous lower cornice supports the stepped portal crown above.
            GateCraftTier(stone,GateU(32),-1,GateU(34),11,height-.12f,height+.12f,0);
            GateCraftTier(stone,GateU(32.12f),-.9f,GateU(33.88f),10.9f,height+.12f,height+.44f,.12f);
            BuildGateHeaderFinish(stone,height);
            foreach(float v in new[]{-3f,11f})
            {
                // Continuous vertical buttress shafts carry the heavy portal.
                // Removing the intermediate stacked belts avoids a cage of small
                // black windows alongside the uninterrupted painted door.
                LitFoundryTier(stone,GateU(32),v,GateU(35),v+2,0,.45f,.08f);
                GateEnclosureTier(stone,GateU(32.12f),v+.10f,GateU(34.88f),v+1.90f,.45f,1.45f,.14f,.91f);
                GateEnclosureTier(stone,GateU(32.16f),v+.14f,GateU(34.84f),v+1.86f,1.45f,1.70f,.08f,.96f,true);
                GateRecessedShaft(stone,new Vector4(GateU(32.26f),v+.24f,GateU(34.74f),v+1.76f),
                    1.70f,5.45f,.04f,.96f,.28f);
                GateEnclosureTier(stone,GateU(32.08f),v+.08f,GateU(34.92f),v+1.92f,5.45f,5.70f,.08f,.96f,true);
                GateCraftTier(stone,GateU(32.02f),v+.02f,GateU(34.98f),v+1.98f,5.7f,6.05f,.12f);
                GateCraftTier(stone,GateU(32.25f),v+.2f,GateU(34.75f),v+1.8f,6.05f,7.05f,.48f);
                GateCraftTier(stone,GateU(32.62f),v+.57f,GateU(34.38f),v+1.43f,7.05f,7.25f,.05f);
                BuildGateInnerReturn(stone,v,height);
            }
            BuildGateCartouche(stone,height);
            Create("Carved gate architecture",stone,arena.PlatformMaterials.FoundryWall,-11);
            BuildGateEnclosure(arena,false);
            BuildGateInlays(arena,height);
            BuildGateProcession(arena);
            BuildGateThreshold(arena);
            BuildGateLandingShoulders(arena);
        }

        private void BuildGateLandingShoulders(ArenaView arena)
        {
            // Broad seated stone shoulders frame the existing single-plane walk.
            // Low ashlar bodies carry substantial coping instead of miniature stairs;
            // the complete assembly stays outboard of the hero's approach.
            foreach(bool north in new[]{false,true})
            {
                float v0=north?12.45f:-3.9f,v1=north?13.9f:-2.45f;
                var box=new Vector4(GateU(29.2f),v0,GateU(32),v1);
                using(MassDepth(box,1.05f))
                {
                    var support=new Builder();
                    LitFoundryTier(support,box.x,box.y,box.z,box.w,-2.8f,0,.12f);
                    Create("Gate landing shoulder foundations",support,arena.PlatformMaterials.FoundryWall,-16);
                    var stone=new Builder();
                    GateEnclosureTier(stone,box.x,box.y,box.z,box.w,0,.24f,.08f,.92f,true);
                    GateEnclosureTier(stone,GateU(29.30f),v0+.10f,GateU(32),v1-.10f,.24f,.78f,.12f,.87f);
                    GateCraftTier(stone,GateU(29.28f),v0+.08f,GateU(32),v1-.08f,.78f,1.02f,.055f,.95f);
                    // Separate below-deck faces so they never draw over the walking surface.
                    // The raised stonework is scoped to the completely outboard footprint.
                    Create("Gate landing stepped shoulders",stone,arena.PlatformMaterials.FoundryWall,-11);
                }
            }
            // The flush processional panel now continues across this landing seam.
        }

        private void BuildGateThreshold(ArenaView arena)
        {
            // A crosswise dressed threshold terminates the processional panel. These
            // five broad stones replace tiny repeated metal brackets; they are flush,
            // with the same continuous stone texture/light as the apron beneath them.
            var stone=new Builder();
            for(int i=0;i<5;i++)
                AshlarFlag(stone,new AshlarStone{I=61+2*FoundryMap.GateOffset,J=i*4,W=3,H=4,Zone=2});
            Create("Gate dressed threshold paving",stone,arena.PlatformMaterials.FoundryPaving,-15);
            var iron=new Builder();
            foreach(float u in new[]{GateU(30.55f),GateU(31.88f)})
                Strip(iron,u,.18f,u,9.82f,.055f,new Color(.67f,.59f,.43f),CopingTrim.Brass);
            Create("Flush gate threshold bindings",iron,arena.PlatformMaterials.Coping,FloorDecal);
        }

        private static void BuildGatePediment(Builder stone,float height)
        {
            // Legacy unused gable builder. The live portal uses the supported
            // stepped crown in BuildGateHeaderFinish instead.
            Vector3 a=Project(GateU(32.18f),-.55f)+Vector3.up*(height+.43f);
            Vector3 b=Project(GateU(32.18f),10.55f)+Vector3.up*(height+.43f);
            Vector3 ridge=Project(GateU(32.18f),5)+Vector3.up*(height+1.55f);
            Vector3 back=Project(GateU(33.78f),5)-Project(GateU(32.18f),5);
            stone.WallQuad(a,b,ridge,ridge,false,new Color(.47f,.51f,.62f));
            BuildGatePedimentRecess(stone,a,b,ridge);
            // The two sloping roof faces establish actual projected depth, not a flat icon.
            GateStonePatch(stone,a,ridge,ridge+back,a+back,new Color(.67f,.69f,.75f));
            GateStonePatch(stone,ridge,b,b+back,ridge+back,new Color(.41f,.45f,.55f));
            // Individual dressed coping blocks follow the slopes. Sampling a stone interior
            // avoids horizontal brick joints running across these sloping structural pieces.
            foreach(var edge in new[]{new[]{a,ridge},new[]{ridge,b}})
            {
                for(int i=0;i<7;i++)
                {
                    Vector3 p=Vector3.Lerp(edge[0],edge[1],(i+.015f)/7f);
                    Vector3 q=Vector3.Lerp(edge[0],edge[1],(i+.985f)/7f);
                    Vector3 rise=Vector3.up*.19f;
                    Color tint=new Color(.69f,.71f,.76f)*(i%3==1?.92f:1f);
                    GateDressedFace(stone,p,q,q+rise,p+rise,tint,.14f);
                    GateDressedFace(stone,p+rise,q+rise,q+back+rise,p+back+rise,
                        new Color(.59f,.63f,.70f),.035f);
                }
            }
        }

        private static void BuildGateCornerStones(Builder stone,float v)
        {
            // Paired returns wrap the existing chamfer, clear of ventilation recesses.
            // Split at the existing projecting belts instead of painting stones over them.
            foreach(var range in new[]{new Vector2(.49f,1.46f),new Vector2(1.74f,3.21f),
                new Vector2(3.49f,5.41f)})
            {
                int count=Mathf.CeilToInt((range.y-range.x)/.55f);
                for(int i=0;i<count;i++)
                {
                    float bottom=Mathf.Lerp(range.x,range.y,i/(float)count)+.018f;
                    float top=Mathf.Lerp(range.x,range.y,(i+1)/(float)count)-.018f;
                    Vector3 a=Project(GateU(32.26f),v+.52f),b=Project(GateU(32.26f),v+.42f);
                    Vector3 c=Project(GateU(32.44f),v+.24f),d=Project(GateU(32.68f),v+.24f);
                    var points=new[]{a,b,c,d};
                    for(int face=0;face<3;face++)
                    {
                        float light=(face==0?.57f:face==1?.73f:.87f)*(i%2==0?1f:.94f);
                        GateDressedFace(stone,points[face]+Vector3.up*bottom,
                            points[face+1]+Vector3.up*bottom,points[face+1]+Vector3.up*top,
                            points[face]+Vector3.up*top,new Color(light*.94f,light*.97f,light),.10f);
                    }
                }
            }
        }

        private static void GateDressedFace(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,
            Color tint,float bevel)
        {
            Vector3 center=(a+b+c+d)*.25f;
            Vector3 ia=Vector3.Lerp(a,center,bevel),ib=Vector3.Lerp(b,center,bevel);
            Vector3 ic=Vector3.Lerp(c,center,bevel),id=Vector3.Lerp(d,center,bevel);
            GateStonePatch(mesh,ia,ib,ic,id,tint);
            GateStonePatch(mesh,a,b,ib,ia,tint*.55f);
            GateStonePatch(mesh,b,c,ic,ib,tint*.72f);
            GateStonePatch(mesh,c,d,id,ic,tint*1.18f);
            GateStonePatch(mesh,d,a,ia,id,tint*.91f);
        }

        private static void GateStonePatch(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint)
        {
            tint.a=1;
            int start=mesh.U.Count;
            mesh.Quad(a,b,c,d,false,tint);
            mesh.U[start]=new Vector2(.27f,.65f);mesh.U[start+1]=new Vector2(.42f,.65f);
            mesh.U[start+2]=new Vector2(.42f,.75f);mesh.U[start+3]=new Vector2(.27f,.75f);
        }

        private static void BuildGatePedimentRecess(Builder stone,Vector3 a,Vector3 b,Vector3 ridge)
        {
            // Legacy recess for the unused gable builder above. The active stepped
            // crown retains the separate central seal housing and does not call this.
            Vector3 center=(a+b+ridge)/3;
            var outer=new[]{Vector3.Lerp(center,a,.83f),Vector3.Lerp(center,b,.83f),
                Vector3.Lerp(center,ridge,.83f)};
            var inner=new[]{Vector3.Lerp(center,a,.64f),Vector3.Lerp(center,b,.64f),
                Vector3.Lerp(center,ridge,.64f)};
            stone.WallQuad(inner[0],inner[1],inner[2],inner[2],false,new Color(.27f,.30f,.39f));
            // A restrained lower catching edge and shaded sloping reveals establish
            // the bevel without bright gold outlines or another set of small marks.
            var tones=new[]{new Color(.63f,.65f,.71f),new Color(.33f,.37f,.46f),
                new Color(.49f,.53f,.62f)};
            for(int i=0;i<3;i++)
            {
                int next=(i+1)%3;
                stone.WallQuad(outer[i],outer[next],inner[next],inner[i],false,tones[i]);
            }
        }

        private void BuildGateInlays(ArenaView arena,float height)
        {
            var iron=new Builder();
            BuildGateHeaderTies(iron,height);
            foreach(float v in new[]{-3f,11f})
            {
                // A compact hipped iron terminal makes the paired towers read as engineered
                // gate machinery while staying within their existing masonry footprints.
                int capStart=iron.C.Count;
                ArchitecturalTier(iron,GateU(32.66f),v+.61f,GateU(34.34f),v+1.39f,7.24f,7.36f,.09f);
                for(int i=capStart;i<iron.C.Count;i++)
                {
                    Color tint=iron.C[i];
                    iron.C[i]=new Color(tint.r*.48f,tint.g*.5f,tint.b*.55f,1);
                }
                // A short cast terminal caps each jamb within its depth envelope. The
                // former long spear made these jambs read as another pair of towers.
                TintedIronTier(iron,GateU(33.15f),v+.65f,GateU(33.85f),v+1.35f,7.36f,7.53f,.04f,
                    new Color(.68f,.73f,.83f),true);
                TintedIronTier(iron,GateU(33.20f),v+.70f,GateU(33.80f),v+1.30f,7.53f,7.67f,.025f,
                    new Color(1.03f,.80f,.49f),true);
                TintedIronTier(iron,GateU(33.25f),v+.75f,GateU(33.75f),v+1.25f,7.67f,7.94f,.22f,
                    new Color(.80f,.86f,.99f),true);
                // Iron collars sit against the lower edge of the projecting stone courses.
                foreach(float y in new[]{1.48f,5.43f})
                {
                    Vector3 a=Project(GateU(32.25f),v+.3f)+Vector3.up*y;
                    Vector3 b=Project(GateU(32.25f),v+1.7f)+Vector3.up*y;
                    ArchitectureBar(iron,a,b,.075f,new Color(.47f,.43f,.38f));
                }
            }
            BuildGateCartoucheLock(iron,height);
            // Explicitly above masonry: equal-order material batches have no painter guarantee.
            Create("Gate inset iron and bronze",iron,arena.PlatformMaterials.FoundryIron,-10.5f);
        }

        private void BuildBoilerArchitecture(ArenaView arena,float u,float v)
        {
            var stone=new Builder();
            // A continuous low plinth and tapered corner shoes carry the boiler mass.
            // Everything stays inside its established 3-by-3 scenery footprint; the
            // broad fire-window faces and the mouth/smoke centre are unchanged.
            LitFoundryTier(stone,u+.02f,v+.02f,u+2.98f,v+2.98f,.015f,.29f,.055f);
            foreach(var corner in new[]{new Vector2(u+.035f,v+.035f),
                new Vector2(u+2.535f,v+.035f),new Vector2(u+.035f,v+2.535f)})
            {
                LitFoundryTier(stone,corner.x,corner.y,corner.x+.43f,corner.y+.43f,.29f,.52f,.025f);
                LitFoundryTier(stone,corner.x+.035f,corner.y+.035f,
                    corner.x+.395f,corner.y+.395f,.52f,1.05f,.1f);
            }
            LitFoundryTier(stone,u+.12f,v+.12f,u+2.88f,v+2.88f,3.15f,3.38f,.08f);
            // Tapered octagonal stack with a genuine open mouth and heavy rim.
            LitFoundryTier(stone,u+.67f,v+.67f,u+2.33f,v+2.33f,3.38f,3.62f,.12f);
            LitFoundryTier(stone,u+.86f,v+.86f,u+2.14f,v+2.14f,3.62f,4.47f,.07f);
            var rim=Octagon(u+.76f,v+.76f,u+2.24f,v+2.24f,.2f);
            var inner=Octagon(u+1.02f,v+1.02f,u+1.98f,v+1.98f,.12f);
            for(int i=0;i<8;i++)
            {
                int j=(i+1)%8;
                // Front outer elevations; far edges are hidden by the solid stack.
                if(i==0||i==6||i==7)
                    stone.WallQuad(rim[i]+Vector3.up*4.47f,rim[j]+Vector3.up*4.47f,
                        rim[j]+Vector3.up*4.66f,rim[i]+Vector3.up*4.66f,false,new Color(.56f,.6f,.69f));
                stone.Quad(rim[i]+Vector3.up*4.66f,rim[j]+Vector3.up*4.66f,
                    inner[j]+Vector3.up*4.66f,inner[i]+Vector3.up*4.66f,true,new Color(.9f,.94f,1));
                // Recessed dark mouth is bounded by the rim, not a solid lid.
                Vector3 center=Project(u+1.5f,v+1.5f)+Vector3.up*4.66f;
                stone.Quad(inner[i]+Vector3.up*4.66f,inner[j]+Vector3.up*4.66f,
                    center,center,true,new Color(.12f,.13f,.18f));
            }
            Create("Boiler crown and flue",stone,arena.PlatformMaterials.FoundryWall,-11);
            BuildFlueIronwork(arena,u,v);
            // Existing iron material supplies restrained corner braces without covering the fire windows.
            foreach(var corner in new[]{new Vector2(u+.16f,v+.3f),new Vector2(u+.3f,v+.16f)})
            {
                Vector3 p=Project(corner.x,corner.y);
                Tube(arena,p+Vector3.up*.5f,p+Vector3.up*3.12f,.075f);
            }
        }

        private void BuildFlueIronwork(ArenaView arena,float u,float v)
        {
            var iron=new Builder();
            // Broad fitted roof plates bridge masonry and the iron shoulder feet. They
            // occupy only the exposed front roof apron, clear of the chimney section.
            foreach(bool alongU in new[]{true,false})
            {
                Vector3 a=Project(u+.28f,v+.28f)+Vector3.up*3.38f;
                Vector3 b=Project(u+(alongU?2.72f:.28f),v+(alongU?.28f:2.72f))+Vector3.up*3.38f;
                Vector3 c=Project(u+(alongU?2.72f:.64f),v+(alongU?.64f:2.72f))+Vector3.up*3.38f;
                Vector3 d=Project(u+(alongU?.28f:.64f),v+(alongU?.64f:.28f))+Vector3.up*3.38f;
                FullFace(iron,a,b,c,d,new Color(.39f,.42f,.49f));
                ArchitectureBar(iron,a,b,.035f,new Color(.62f,.63f,.67f));
                ArchitectureBar(iron,d,c,.025f,new Color(.23f,.26f,.32f));
            }
            // Two visible roof shoulders brace the chimney. Side plates sit on
            // the existing crown and stop below the rim and smoke source.
            foreach(bool alongU in new[]{true,false})
            {
                float fu=u+(alongU?.45f:1.5f),fv=v+(alongU?1.5f:.45f);
                float tu=u+(alongU?.98f:1.5f),tv=v+(alongU?1.5f:.98f);
                TintedIronTier(iron,fu-.15f,fv-.15f,fu+.15f,fv+.15f,3.38f,3.53f,.02f,
                    new Color(.40f,.43f,.50f));
                Vector3 foot=Project(fu,fv)+Vector3.up*3.53f;
                Vector3 root=Project(tu,tv)+Vector3.up*3.53f;
                Vector3 tip=Project(tu,tv)+Vector3.up*4.21f;
                FullFace(iron,foot,root,tip,tip,new Color(.25f,.29f,.37f));
                ArchitectureBar(iron,foot,tip,.065f,new Color(.63f,.65f,.70f));
                ArchitectureBar(iron,foot,root,.06f,new Color(.33f,.36f,.43f));
                foreach(Vector3 p in new[]{Vector3.Lerp(foot,tip,.15f),Vector3.Lerp(foot,tip,.84f)})
                    ArchitectureBar(iron,p-Vector3.up*.025f,p+Vector3.up*.025f,.05f,
                        new Color(.74f,.57f,.34f));
            }
            // Two visible stack faces receive thin retaining bands. The open mouth and smoke
            // centre at (u+1.5,v+1.5), height 4.66, are deliberately untouched.
            // Match ArchitecturalTier's actual octagonal section at both ends of its
            // taper. Fixed untapered endpoints protrude beyond the clipped corners.
            var lower=Octagon(u+.86f,v+.86f,u+2.14f,v+2.14f,.18f);
            var upper=Octagon(u+.93f,v+.93f,u+2.07f,v+2.07f,.135f);
            foreach(int edge in new[]{0,6})
            {
                foreach(float y in new[]{3.8f,4.27f})
                {
                    FlueFaceStrip(iron,lower,upper,edge,.035f,.965f,y-.0375f,y+.0375f,
                        new Color(.3f,.34f,.42f));
                    foreach(float t in new[]{.16f,.84f})
                    {
                        Vector3 p=FlueFacePoint(lower,upper,edge,Mathf.Lerp(.035f,.965f,t),y);
                        ArchitectureBar(iron,p-Vector3.up*.025f,p+Vector3.up*.025f,.045f,
                            new Color(.83f,.64f,.39f));
                    }
                }
                // A soot-dark recessed inspection slot has a small lower catching edge.
                FlueFaceStrip(iron,lower,upper,edge,.3f,.7f,3.99f,4.09f,new Color(.12f,.14f,.2f));
                FlueFaceStrip(iron,lower,upper,edge,.3f,.7f,3.985f,4.005f,new Color(.64f,.65f,.69f));
            }
            Create("Flue retaining bands",iron,arena.PlatformMaterials.FoundryIron,-10.5f);
        }

        private static void BuildGateInnerReturn(Builder stone,float v,float height)
        {
            // Substantial jambs and their deep returns stand outside the facade's
            // v[-1,11] rectangle. They thicken the physical portal without drawing
            // across either painted banner, seal, door leaf or walking approach.
            bool near=v<0;
            float a=near?v+1.42f:v+.04f,b=near?v+1.96f:v+.58f;
            GateCraftTier(stone,GateU(32.02f),a,GateU(33.04f),b,.45f,1.15f,.03f,.94f);
            // Seat the square rebate corners inside the base's actual chamfered
            // top and the former body's contracted upper footprint, not its AABB.
            BuildGateJambRebate(stone,new Vector4(GateU(32.10f),a+.075f,GateU(32.96f),b-.075f),
                1.15f,height-.38f,near);
            GateCraftTier(stone,GateU(32.02f),a-.02f,GateU(33.04f),b+.02f,height-.38f,height+.10f,.065f,.96f);
        }

        private static Vector2[] GateJambSection(Vector4 box,bool near)
        {
            // The inner longitudinal cut is real missing volume, not a dark stripe
            // over the former solid column. Its backing remains inside this jamb;
            // the unchanged painted facade and approach lie beyond its v edge.
            float back=box.x+(box.z-box.x)*.53f;
            float rebate=(box.w-box.y)*.36f;
            if(near)return new[]{new Vector2(box.x,box.y),new Vector2(box.z,box.y),new Vector2(box.z,box.w),
                new Vector2(back,box.w),new Vector2(back,box.w-rebate),new Vector2(box.x,box.w-rebate)};
            return new[]{new Vector2(back,box.y),new Vector2(box.z,box.y),new Vector2(box.z,box.w),
                new Vector2(box.x,box.w),new Vector2(box.x,box.y+rebate),new Vector2(back,box.y+rebate)};
        }

        private static void BuildGateJambRebate(Builder stone,Vector4 box,float bottom,float top,bool near)
        {
            Vector2[] section=GateJambSection(box,near);
            var order=new int[section.Length];
            for(int i=0;i<order.Length;i++)order[i]=i;
            // Emit back planes first in the shared transparent stone material.
            System.Array.Sort(order,(i,j)=>
            {
                Vector2 a=section[i]+section[(i+1)%section.Length],b=section[j]+section[(j+1)%section.Length];
                return (b.x+b.y).CompareTo(a.x+a.y);
            });
            foreach(int edge in order)
            {
                Vector2 p=section[edge],q=section[(edge+1)%section.Length];
                Vector3 a=Project(p.x,p.y),b=Project(q.x,q.y);
                bool alongU=Mathf.Abs(p.x-q.x)>.01f;
                bool reveal=near?edge==3||edge==4:edge==4||edge==5;
                float shade=reveal?.72f:1f;
                Color tone=(alongU?new Color(.56f,.59f,.67f):new Color(.45f,.49f,.58f))*shade;tone.a=1;
                int first=stone.V.Count;
                GateStonePatch(stone,a+Vector3.up*bottom,b+Vector3.up*bottom,
                    b+Vector3.up*top,a+Vector3.up*top,new Color(tone.r*.72f,tone.g*.72f,tone.b*.72f));
                GateRecessGrain(stone,first,stone.V.Count,Vector3.up,edge);
                for(int course=0;course<3;course++)
                {
                    float lo=Mathf.Lerp(bottom,top,course/3f)+(course==0?0:.012f);
                    float hi=Mathf.Lerp(bottom,top,(course+1)/3f)-(course==2?0:.012f);
                    GateRecessDressedFace(stone,a+Vector3.up*lo,b+Vector3.up*lo,
                        b+Vector3.up*hi,a+Vector3.up*hi,tone,.035f,edge);
                }
            }
            // Two rectangles exactly tile the L-shaped bearing. A fan across the
            // concave cut would silently fill the reveal with a triangular cap.
            float split=section[4].y;
            float backU=near?section[3].x:section[0].x;
            void Cap(float u0,float v0,float u1,float v1)
            {
                int first=stone.V.Count;Vector3 rise=Vector3.up*top;
                GateStonePatch(stone,Project(u0,v0)+rise,Project(u1,v0)+rise,
                    Project(u1,v1)+rise,Project(u0,v1)+rise,new Color(.63f,.66f,.74f));
                GateRecessGrain(stone,first,stone.V.Count,Project(1,0)-Project(0,0),0);
            }
            if(near){Cap(box.x,box.y,box.z,split);Cap(backU,split,box.z,box.w);}
            else{Cap(backU,box.y,box.z,split);Cap(box.x,split,box.z,box.w);}
        }

        private static void TintedIronTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset,Color tint,bool plainIron=false)
        {
            int first=mesh.C.Count;
            ArchitecturalTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            for(int i=first;i<mesh.C.Count;i++)
            {
                mesh.C[i]*=tint;
                // Cast terminals use solid iron, not a complete riveted plate per facet.
                if(plainIron)mesh.U[i]=IronPatch+(Vector2)(mesh.V[i]-mesh.V[first])*.025f;
            }
        }

        private static void ArchitectureBar(Builder mesh,Vector3 a,Vector3 b,float width,Color tint)
        {
            Vector3 normal=new Vector3(-(b-a).y,(b-a).x,0).normalized*width*.5f;
            FullFace(mesh,a+normal,b+normal,b-normal,a-normal,tint);
        }

        private static Vector3[] Octagon(float u0,float v0,float u1,float v1,float corner)
        {
            return new[]{Project(u0+corner,v0),Project(u1-corner,v0),
                Project(u1,v0+corner),Project(u1,v1-corner),Project(u1-corner,v1),
                Project(u0+corner,v1),Project(u0,v1-corner),Project(u0,v0+corner)};
        }

        private static void LitFoundryTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset)
        {
            int first=mesh.C.Count;
            ArchitecturalTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            // ArchitecturalTier emits three wall quads followed by its upper cap. Recover
            // authored elevation exactly; projected Y also contains the floor's depth.
            for(int i=first;i<mesh.C.Count;i++)
            {
                int local=i-first;
                float elevation=local<12&&local%4<2?bottom:top;
                Vector3 floor=mesh.V[i]-Vector3.up*elevation;
                FoundrySurface.ToDesign(floor.x,floor.y,out float u,out float v);
                mesh.C[i]=SurfaceTone(mesh.C[i],u,v,elevation,local<12?top-elevation:2);
                mesh.C[i]=GateStoneTone(mesh.C[i],u,v,elevation,local<12?local/4:3);
            }
        }

        private static Color GateStoneTone(Color tone,float u,float v,float height,int face)
        {
            // The gate's dressed stone shares the painted portal's cool quarry
            // value. Keep the damp footing and recess light; lift exposed broad
            // planes by different amounts rather than changing global ambient.
            float distance=DistanceFromFloorBox(u,v,GateU(31),-7,GateU(36),17);
            float place=1-Mathf.SmoothStep(0,1,distance/2f);
            float exposed=place*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.15f,1.5f,height));
            if(exposed<=0)return tone;
            float plane=face==0?1:face==1?.58f:face==2?.76f:.83f;
            float quarry=.15f+.10f*FloorPatina(u*.43f+11,v*.43f+height*.10f);
            float gain=1+exposed*plane*quarry;
            return new Color(tone.r*gain,tone.g*(gain+.012f*exposed),
                tone.b*(gain+.022f*exposed),tone.a);
        }

        private static void BuildGateHeaderTies(Builder iron,float height)
        {
            // Four straps wrap the unchanged horizontal lower cornice and its upper
            // return, below the new stepped crown; they never followed the old gable.
            // Clean iron UVs avoid repeating an entire plate on a bolt.
            foreach(float v in new[]{.2f,2.5f,7.5f,9.8f})
            {
                Vector3 a=Project(GateU(32),v-.10f)+Vector3.up*(height-.10f);
                Vector3 b=Project(GateU(32),v+.10f)+Vector3.up*(height-.10f);
                Vector3 rise=Vector3.up*.20f;
                GateIronPatch(iron,a,b,b+rise,a+rise,new Color(.62f,.65f,.73f));
                Vector3 back=Project(GateU(32.55f),v)-Project(GateU(32),v);
                GateIronPatch(iron,a+rise,b+rise,b+rise+back,a+rise+back,new Color(.80f,.77f,.69f));
                Vector3 bolt=(a+b)*.5f+rise*.5f;
                Vector3 x=Vector3.right*.045f,y=Vector3.up*.04f;
                GateIronPatch(iron,bolt-y,bolt+x,bolt+y,bolt-x,new Color(1.35f,1.09f,.70f));
            }
        }

        private static void GateIronPatch(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tint)
        {
            int first=mesh.V.Count;
            FullFace(mesh,a,b,c,d,tint);
            for(int i=first;i<mesh.V.Count;i++)mesh.U[i]=IronPatch+(Vector2)(mesh.V[i]-a)*.02f;
        }

        private static Vector3 FlueFacePoint(Vector3[] lower,Vector3[] upper,int edge,float along,float height)
        {
            int next=(edge+1)%8;
            float rise=Mathf.InverseLerp(3.62f,4.47f,height);
            Vector3 a=Vector3.Lerp(lower[edge],upper[edge],rise);
            Vector3 b=Vector3.Lerp(lower[next],upper[next],rise);
            return Vector3.Lerp(a,b,along)+Vector3.up*height;
        }

        private static void FlueFaceStrip(Builder mesh,Vector3[] lower,Vector3[] upper,int edge,
            float start,float end,float bottom,float top,Color tint)
        {
            FullFace(mesh,FlueFacePoint(lower,upper,edge,start,bottom),
                FlueFacePoint(lower,upper,edge,end,bottom),FlueFacePoint(lower,upper,edge,end,top),
                FlueFacePoint(lower,upper,edge,start,top),tint);
        }

        private static void ArchitecturalTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset)
        {
            float corner=Mathf.Min(.18f,Mathf.Min(u1-u0,v1-v0)*.15f);
            var lower=Octagon(u0,v0,u1,v1,corner);
            float topCorner=Mathf.Min(corner*.75f,(Mathf.Min(u1-u0,v1-v0)-2*inset)*.25f);
            var upper=Octagon(u0+inset,v0+inset,u1-inset,v1-inset,topCorner);
            // Face light varies with the horizontal plane direction, keeping bevels readable.
            foreach(int i in new[]{0,6,7})
            {
                int j=(i+1)%8;
                Color light=i==0?new Color(.76f,.79f,.87f):i==7?new Color(.65f,.68f,.77f):new Color(.48f,.52f,.64f);
                mesh.WallQuad(lower[i]+Vector3.up*bottom,lower[j]+Vector3.up*bottom,
                    upper[j]+Vector3.up*top,upper[i]+Vector3.up*top,false,light);
            }
            Vector3 center=Project((u0+u1)*.5f,(v0+v1)*.5f)+Vector3.up*top;
            for(int i=0;i<8;i++)mesh.Quad(upper[i]+Vector3.up*top,upper[(i+1)%8]+Vector3.up*top,
                center,center,true,new Color(.83f,.86f,.94f));
        }
    }
}
