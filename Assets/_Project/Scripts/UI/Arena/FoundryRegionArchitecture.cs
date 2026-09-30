using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Modelled in the same design plane as the existing projected architecture.
        // All geometry is static and stays on the established scenery footprints.
        private void BuildGateArchitecture(ArenaView arena,float height)
        {
            var stone=new Builder();
            // Segmented cornice: projecting lower lip, recessed course and sloping crown.
            LitFoundryTier(stone,32,-1,34,11,height-.12f,height+.12f,0);
            LitFoundryTier(stone,32.12f,-.9f,33.88f,10.9f,height+.12f,height+.44f,.12f);
            for(int i=0;i<8;i++)
                LitFoundryTier(stone,32,-.85f+i*1.47f,32.32f,-.38f+i*1.47f,
                    height-.32f,height-.12f,0);
            BuildGatePediment(stone,height);
            foreach(float v in new[]{-3f,11f})
            {
                // Wide plinth, narrower shaft and chamfered crown replace the flat box silhouette.
                LitFoundryTier(stone,32,v,35,v+2,0,.45f,.08f);
                float previous=.45f;
                foreach(float level in new[]{1.5f,3.25f,5.45f})
                {
                    // Build upward in painter order so a course's rear cap cannot paint over
                    // the shaft above it. A single full-height shaft followed by all caps cannot do this.
                    LitFoundryTier(stone,32.26f,v+.24f,34.74f,v+1.76f,previous,level,.04f);
                    LitFoundryTier(stone,32.08f,v+.08f,34.92f,v+1.92f,level,level+.2f,.04f);
                    previous=level+.2f;
                }
                LitFoundryTier(stone,32.02f,v+.02f,34.98f,v+1.98f,5.7f,6.05f,.12f);
                LitFoundryTier(stone,32.25f,v+.2f,34.75f,v+1.8f,6.05f,7.05f,.48f);
                LitFoundryTier(stone,32.62f,v+.57f,34.38f,v+1.43f,7.05f,7.25f,.05f);
            }
            // Central raised seal housing above the painted doors; the door artwork remains intact.
            LitFoundryTier(stone,32.12f,3.9f,33.45f,6.1f,height+.43f,height+1.03f,.22f);
            Create("Carved gate architecture",stone,arena.PlatformMaterials.FoundryWall,-11);
            BuildGateInlays(arena,height);
            BuildGateThreshold(arena);
        }

        private void BuildGateThreshold(ArenaView arena)
        {
            // Flush wear strips belong to the existing gate apron. They are not
            // stairs, a new collision surface or an interactive exit marker.
            var iron=new Builder();
            for(int i=0;i<8;i++)
            {
                float v=-.6f+i*1.4f;
                // Open stone between strips avoids dark plates reading as pits.
                Strip(iron,30.95f,v+.12f,31.75f,v+.12f,.08f,new Color(.75f,.70f,.61f),CopingTrim.Brass);
                Strip(iron,30.95f,v+1.24f,31.75f,v+1.24f,.08f,new Color(.75f,.70f,.61f),CopingTrim.Brass);
                Strip(iron,30.95f,v+.12f,30.95f,v+1.24f,.055f,new Color(.67f,.62f,.54f),CopingTrim.Brass);
                foreach(float corner in new[]{.16f,1.20f})
                {
                    Vector3 p=Project(31.05f,v+corner);
                    ArchitectureBar(iron,p-Vector3.right*.026f,p+Vector3.right*.026f,.045f,
                        new Color(.63f,.59f,.49f));
                }
            }
            Create("Flush gate wear threshold",iron,arena.PlatformMaterials.Coping,FloorDecal);
        }

        private static void BuildGatePediment(Builder stone,float height)
        {
            // A shallow continuous gable is supported by the full existing header.
            // Its front remains behind the central seal housing, above the painted doors.
            Vector3 a=Project(32.18f,-.55f)+Vector3.up*(height+.43f);
            Vector3 b=Project(32.18f,10.55f)+Vector3.up*(height+.43f);
            Vector3 ridge=Project(32.18f,5)+Vector3.up*(height+1.55f);
            Vector3 back=Project(33.78f,5)-Project(32.18f,5);
            stone.WallQuad(a,b,ridge,ridge,false,new Color(.47f,.51f,.62f));
            // The two sloping roof faces establish actual projected depth, not a flat icon.
            stone.WallQuad(a,ridge,ridge+back,a+back,false,new Color(.67f,.69f,.75f));
            stone.WallQuad(ridge,b,b+back,ridge+back,false,new Color(.41f,.45f,.55f));
            // Heavy stone capping courses follow the slopes with the same restrained material.
            foreach(var edge in new[]{new[]{a,ridge},new[]{ridge,b}})
            {
                Vector3 p=edge[0],q=edge[1];
                stone.WallQuad(p,q,q+Vector3.up*.13f,p+Vector3.up*.13f,false,new Color(.7f,.72f,.78f));
                stone.WallQuad(p+Vector3.up*.13f,q+Vector3.up*.13f,
                    q+back+Vector3.up*.13f,p+back+Vector3.up*.13f,false,new Color(.57f,.61f,.7f));
            }
        }

        private void BuildGateInlays(ArenaView arena,float height)
        {
            var iron=new Builder();
            foreach(float v in new[]{-3f,11f})
            {
                BuildGateSideRecesses(iron,v);
                // A compact hipped iron terminal makes the paired towers read as engineered
                // gate machinery while staying within their existing masonry footprints.
                int capStart=iron.C.Count;
                ArchitecturalTier(iron,32.66f,v+.61f,34.34f,v+1.39f,7.24f,7.36f,.09f);
                for(int i=capStart;i<iron.C.Count;i++)
                {
                    Color tint=iron.C[i];
                    iron.C[i]=new Color(tint.r*.48f,tint.g*.5f,tint.b*.55f,1);
                }
                // Cast finials are carried by the existing terminal, not floating
                // ornaments. Their narrow upper taper leaves the door crest dominant.
                TintedIronTier(iron,33.15f,v+.65f,33.85f,v+1.35f,7.36f,7.53f,.04f,
                    new Color(.48f,.51f,.58f));
                TintedIronTier(iron,33.20f,v+.70f,33.80f,v+1.30f,7.53f,7.67f,.025f,
                    new Color(.72f,.56f,.34f));
                TintedIronTier(iron,33.25f,v+.75f,33.75f,v+1.25f,7.67f,8.45f,.22f,
                    new Color(.56f,.60f,.69f));
                // Deep narrow panels break the broad tower faces into supporting pilasters.
                // They are on the existing front plane, clear of the door and banner facade.
                foreach(var levels in new[]{new Vector2(.72f,1.3f),new Vector2(1.88f,3.03f),
                    new Vector2(3.67f,5.19f)})
                {
                    Vector3 a=Project(32.26f,v+.54f)+Vector3.up*levels.x;
                    Vector3 b=Project(32.26f,v+1.46f)+Vector3.up*levels.x;
                    Vector3 lift=Vector3.up*(levels.y-levels.x);
                    FullFace(iron,a,b,b+lift,a+lift,new Color(.22f,.25f,.32f));
                    ArchitectureBar(iron,a,a+lift,.035f,new Color(.73f,.75f,.81f));
                    ArchitectureBar(iron,b,b+lift,.055f,new Color(.31f,.34f,.43f));
                    ArchitectureBar(iron,a+lift,b+lift,.045f,new Color(.7f,.7f,.72f));
                    // Restrained bronze fixing plates, rather than bright glowing runes.
                    foreach(float t in new[]{.14f,.86f})
                    {
                        Vector3 p=Vector3.Lerp(a,b,.5f)+lift*t;
                        ArchitectureBar(iron,p-Vector3.up*.065f,p+Vector3.up*.065f,.08f,
                            new Color(.8f,.57f,.28f));
                    }
                }
                // Iron collars sit against the lower edge of the projecting stone courses.
                foreach(float y in new[]{1.48f,3.23f,5.43f})
                {
                    Vector3 a=Project(32.25f,v+.3f)+Vector3.up*y;
                    Vector3 b=Project(32.25f,v+1.7f)+Vector3.up*y;
                    ArchitectureBar(iron,a,b,.075f,new Color(.47f,.43f,.38f));
                }
            }
            // Small forge seal on the raised central stone. The large painted door seal remains dominant.
            Vector3 seal=Project(32.12f,5)+Vector3.up*(height+.74f);
            Vector3 horizontal=Vector3.right*.26f,vertical=Vector3.up*.22f;
            var warm=new Color(.91f,.68f,.34f);
            ArchitectureBar(iron,seal+vertical,seal+horizontal,.045f,warm);
            ArchitectureBar(iron,seal+horizontal,seal-vertical,.045f,warm);
            ArchitectureBar(iron,seal-vertical,seal-horizontal,.045f,warm);
            ArchitectureBar(iron,seal-horizontal,seal+vertical,.045f,warm);
            ArchitectureBar(iron,seal-Vector3.up*.09f,seal+Vector3.up*.09f,.07f,warm);
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

        private static void BuildGateSideRecesses(Builder iron,float v)
        {
            // The wide return face receives paired shutter recesses, leaving the
            // projecting stone courses and the painted door plane unobscured.
            foreach(var level in new[]{new Vector2(1.94f,3.0f),new Vector2(3.76f,5.12f)})
                foreach(float u in new[]{32.72f,33.59f})
                {
                    Vector3 a=Project(u,v+.27f)+Vector3.up*level.x;
                    Vector3 b=Project(u+.57f,v+.27f)+Vector3.up*level.x;
                    Vector3 lift=Vector3.up*(level.y-level.x);
                    FullFace(iron,a,b,b+lift,a+lift,new Color(.20f,.23f,.30f));
                    ArchitectureBar(iron,a,a+lift,.045f,new Color(.55f,.58f,.65f));
                    ArchitectureBar(iron,b,b+lift,.045f,new Color(.27f,.30f,.38f));
                    ArchitectureBar(iron,a,b,.08f,new Color(.62f,.62f,.65f));
                    ArchitectureBar(iron,a+lift,b+lift,.06f,new Color(.36f,.39f,.46f));
                    // Three substantial louvres read as ventilation at portrait scale.
                    foreach(float t in new[]{.25f,.5f,.75f})
                        ArchitectureBar(iron,a+lift*t,b+lift*t,.065f,new Color(.60f,.63f,.70f));
                }
        }

        private static void TintedIronTier(Builder mesh,float u0,float v0,float u1,float v1,
            float bottom,float top,float inset,Color tint)
        {
            int first=mesh.C.Count;
            ArchitecturalTier(mesh,u0,v0,u1,v1,bottom,top,inset);
            for(int i=first;i<mesh.C.Count;i++)mesh.C[i]*=tint;
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
            // Build-time reflected heat, local to these boiler/gate stone tiers only.
            // Other users of ArchitecturalTier (iron caps, bridge, shaft) retain their colours.
            float u=(u0+u1)*.5f,v=(v0+v1)*.5f;
            float source=0;
            foreach(float furnaceU in new[]{18.5f,26.5f})
                foreach(float furnaceV in new[]{-3.5f,13.5f})
                {
                    float du=u-furnaceU,dv=v-furnaceV;
                    source=Mathf.Max(source,1/(1+.18f*(du*du+dv*dv)));
                }
            // The existing two small gate braziers contribute less than furnace windows.
            foreach(float beaconV in new[]{-2f,12f})
            {
                float du=u-32.5f,dv=v-beaconV;
                source=Mathf.Max(source,.38f/(1+.3f*(du*du+dv*dv)));
            }
            Vector3 baseCenter=Project(u,v);
            for(int i=first;i<mesh.C.Count;i++)
            {
                float elevation=Mathf.Clamp(mesh.V[i].y-baseCenter.y,bottom,top);
                float bounce=.48f*source*Mathf.Pow(Mathf.Clamp01(1-elevation/3.5f),.8f);
                Color original=mesh.C[i];
                // Preserve directional shading and alpha; upper stone keeps its cool ambient.
                mesh.C[i]=new Color(original.r*(1+.65f*bounce),original.g*(1+.14f*bounce),
                    original.b*(1-.35f*bounce),original.a);
            }
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
