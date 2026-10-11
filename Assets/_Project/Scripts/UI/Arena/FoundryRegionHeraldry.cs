using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The coping rail already contains warm metal albedo. A neutral worn-metal
        // multiplier avoids tinting that gold twice into almost-black bronze.
        private static readonly Color CeremonialBrass=new Color(.96f,.91f,.79f);
        private static readonly Color CeremonialBrassEdge=new Color(1.02f,.97f,.85f);
        // One stone medallion, one quiet brass boundary. The old remote r8.6
        // bullseye/spokes competed with the ability ring and the actual court scale.
        private void BuildCourtSeal(ArenaView arena)
        {
            var seal=new Builder();
            BuildCourtSealGeometry(seal);
            Create("Court engraved forge seal",seal,arena.PlatformMaterials.Coping,FloorDecal);
        }

        private static void BuildCourtSealGeometry(Builder seal)
        {
            var channel=new Color(.84f,.82f,.83f);
            // A continuous annulus replaces the chain of very thin line quads.
            // Its width survives the overview while the existing r3.79 setting,
            // original stone-medallion footprint and single boundary stay fixed.
            for(int pass=0;pass<2;pass++)
            {
                bool metal=pass==1;
                float half=metal?.065f:.095f;
                Color ringTone=metal?CeremonialBrass*1.08f:channel;
                ringTone.a=1;
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                    SealQuad(seal,SealPolar(a,3.79f-half),SealPolar(b,3.79f-half),
                        SealPolar(b,3.79f+half),SealPolar(a,3.79f+half),
                        ringTone,metal?CopingRail:SealStone);
                }
                for(int ray=0;ray<8;ray++)
                {
                    Vector2 d=SealPolar(ray*Mathf.PI/4,1),p=d*2.96f,q=d*3.67f;
                    Strip(seal,p.x,p.y,q.x,q.y,metal?.055f:.085f,
                        metal?CeremonialBrass:channel,metal?CopingTrim.Brass:CopingTrim.Stone,ray);
                }
            }
            // Slim paired compass facets have enough area to read at map scale,
            // unlike an empty star outline. Most paving between the eight arms is
            // still exposed: this is a worn metal engraving, not a filled gold sigil.
            for(int ray=0;ray<8;ray++)
            {
                Vector2 direction=SealPolar(ray*Mathf.PI/4,1),side=new Vector2(-direction.y,direction.x);
                bool primary=ray%2==1;
                float length=primary?2.50f:1.70f,width=primary?.20f:.145f;
                Vector2 root=direction*.27f,tip=direction*length;
                Vector2 left=direction*.82f+side*width,right=direction*.82f-side*width;
                SealQuad(seal,direction*.24f,left+side*.028f,tip+direction*.025f,right-side*.028f,channel,SealStone);
                CourtSealFacet(seal,root,left,tip,CeremonialBrass*(primary?1.27f:1.12f));
                CourtSealFacet(seal,root,tip,right,CeremonialBrass*(primary?.83f:.76f));
            }
            SealQuad(seal,new Vector2(0,-.32f),new Vector2(.32f,0),new Vector2(0,.32f),new Vector2(-.32f,0),channel,SealStone);
            CourtSealFacet(seal,new Vector2(0,-.27f),new Vector2(.27f,0),new Vector2(0,.27f),CeremonialBrass*1.05f);
            CourtSealFacet(seal,new Vector2(0,-.27f),new Vector2(0,.27f),new Vector2(-.27f,0),CeremonialBrass*.81f);
            WeatherHeraldry(seal);
        }

        private static void CourtSealFacet(Builder seal,Vector2 a,Vector2 b,Vector2 c,Color tone)
        {
            int first=seal.V.Count;tone.a=1;
            foreach(Vector2 p in new[]{a,b,c})
            {
                seal.V.Add(Project(p.x,p.y));seal.C.Add(tone);
                // Broad subdued interior metal, rather than the rail's almost-white
                // bevel, fills these facets. World-attached grain continues across
                // neighbouring arms and receives ordinary floor light at Emit.
                seal.U.Add(new Vector2(Mathf.Lerp(CopingRail.x,CopingRail.z,.5f+p.x*.075f+p.y*.023f),
                    (CopingRail.y+CopingRail.w)*.5f));
            }
            seal.T.Add(first);seal.T.Add(first+1);seal.T.Add(first+2);
        }

        private static void BuildCourtCompass(Builder inlays,Color brass)
        {
            // Short cardinal setting-out keys belong to the medallion. There is
            // deliberately no second remote ring crossing the pit-side battle space.
            for(int ray=0;ray<4;ray++)
            {
                Vector2 d=SealPolar(ray*Mathf.PI*.5f,1),p=d*4.03f,q=d*4.48f;
                if(OnCourt(p)&&OnCourt(q))
                    Strip(inlays,p.x,p.y,q.x,q.y,.075f,brass,CopingTrim.Brass,ray);
            }
            WeatherHeraldry(inlays);
        }

        private static bool OnCourt(Vector2 design)
        {
            FoundryMap.ToFloor(design.x,design.y,out float x,out float y);
            return FoundryMap.SurfaceContains(x,y,.2f);
        }

        // A flush processional inlay makes the compact gate apron read as a destination.
        // Matte stone/brass, with no emission or animation: this is architecture, not an
        // ability telegraph or a second interactive exit. The whole panel stays inside
        // the existing lane and is lit with the floor in the shared decal batch.
        private void BuildGateProcession(ArenaView arena)
        {
            var inlay=new Builder();
            var groove=new Color(.70f,.69f,.73f);
            var brass=CeremonialBrass;
            var edge=CeremonialBrassEdge;
            // Dressed stone replaces the long pair of racing-stripe outlines.
            // Its centreline is inset inside the previous frame's exact envelope.
            var frame=new[]{new Vector2(16.2f,2.15f),new Vector2(16.65f,1.7f),
                new Vector2(GateU(29.75f),1.7f),new Vector2(GateU(30.2f),2.15f),new Vector2(GateU(30.2f),7.85f),
                new Vector2(GateU(29.75f),8.3f),new Vector2(16.65f,8.3f),new Vector2(16.2f,7.85f)};
            var stone=new Builder();
            // The eight corners share true offset-line intersections. Each edge owns
            // its miter-to-miter sector, so neighboring beds cannot overlap the faces.
            var outerBed=InsetFlagContour(frame,-.28f);
            var innerBed=InsetFlagContour(frame,.28f);
            var outerFace=InsetFlagContour(frame,-.24f);
            var innerFace=InsetFlagContour(frame,.24f);
            BuildGateProcessionalFlags(stone,innerBed);
            // Complete the common mortar bed before any dressed stone is emitted.
            for(int i=0;i<frame.Length;i++)
            {
                int next=(i+1)%frame.Length;
                CeremonialStoneFace(stone,new[]{outerBed[i],outerBed[next],innerBed[next],innerBed[i]},.82f);
            }
            for(int i=0;i<frame.Length;i++)
            {
                int next=(i+1)%frame.Length;
                float length=Vector2.Distance(frame[i],frame[next]);
                int blocks=Mathf.Max(1,Mathf.CeilToInt(length/1.25f));
                float joint=.018f/length;
                for(int k=0;k<blocks;k++)
                {
                    float t0=k/(float)blocks+joint,t1=(k+1)/(float)blocks-joint;
                    Vector2 a=Vector2.Lerp(outerFace[i],outerFace[next],t0);
                    Vector2 b=Vector2.Lerp(outerFace[i],outerFace[next],t1);
                    Vector2 c=Vector2.Lerp(innerFace[i],innerFace[next],t1);
                    Vector2 d=Vector2.Lerp(innerFace[i],innerFace[next],t0);
                    float tone=1.07f+.015f*((i+k)%3);
                    CeremonialStoneFace(stone,new[]{a,b,c,d},tone);
                }
            }
            BuildGateApronCourses(stone);
            Create("Gate dressed stone border",stone,arena.PlatformMaterials.FoundryPaving,-15);
            for(int pass=0;pass<2;pass++)
            {
                bool metal=pass==1;
                var trim=metal?CopingTrim.Brass:CopingTrim.Stone;
                Color tone=metal?brass:groove;
                Vector2 centre=new Vector2(23.2f+FoundryMap.GateOffset*.5f,5);
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                    foreach(float radius in new[]{1.82f})
                    {
                        Vector2 p=centre+SealPolar(a,radius),q=centre+SealPolar(b,radius);
                        Strip(inlay,p.x,p.y,q.x,q.y,metal?.060f:.085f,tone,trim,i);
                    }
                }
                // Four long points follow the approach; their open interior preserves
                // paving detail and actor contrast at the normal follow-camera width.
                var star=new[]{new Vector2(2.62f,0),new Vector2(.42f,.42f),
                    new Vector2(0,1.45f),new Vector2(-.42f,.42f),
                    new Vector2(-2.62f,0),new Vector2(-.42f,-.42f),
                    new Vector2(0,-1.45f),new Vector2(.42f,-.42f)};
                for(int i=0;i<star.Length;i++)
                {
                    Vector2 p=centre+star[i],q=centre+star[(i+1)%star.Length];
                    Strip(inlay,p.x,p.y,q.x,q.y,metal?.072f:.10f,metal?edge:groove,trim,i);
                }
                foreach(var ends in new[]{new Vector2(17.3f,centre.x-2.82f),new Vector2(centre.x+2.82f,GateU(29.1f))})
                    Strip(inlay,ends.x,5,ends.y,5,metal?.078f:.11f,tone,trim);
                // Four short ties connect the framing to the medallion without filling
                // the fighting lane with a repeated luminous-looking pattern.
                foreach(float u in new[]{18f,GateU(28.4f)})foreach(float v in new[]{1.7f,8.3f})
                {
                    Vector2 c=new Vector2(u,v),along=new Vector2(.32f,0),across=new Vector2(0,.18f);
                    SealQuad(inlay,c-along,c+across,c+along,c-across,metal?edge:groove,metal?CopingRail:SealStone);
                }
            }
            WeatherHeraldry(inlay);
            Create("Gate processional stone and brass inlay",inlay,arena.PlatformMaterials.Coping,FloorDecal);
        }

        private static void BuildGateProcessionalFlags(Builder stone,Vector2[] field)
        {
            // The insignia is engraved into assembled flags, not a single carpet-like
            // panel. A calm staggered bond has no checkerboard or raised step edges.
            // Keep its joints finer than the surrounding dressed perimeter stones.
            CeremonialStoneFace(stone,field,.80f);
            float u0=field[0].x,u1=u0,v0=field[0].y,v1=v0;
            foreach(var p in field)
            {
                u0=Mathf.Min(u0,p.x);u1=Mathf.Max(u1,p.x);
                v0=Mathf.Min(v0,p.y);v1=Mathf.Max(v1,p.y);
            }
            const int rows=4;
            float rowHeight=(v1-v0)/rows;
            for(int row=0;row<rows;row++)
            {
                float bottom=v0+row*rowHeight,top=v0+(row+1)*rowHeight;
                float cursor=u0-(row%2==0?0:.72f);
                for(int slab=0;cursor<u1;slab++)
                {
                    // Alternate quarry lengths as a course, rather than scattering
                    // unrelated repairs around the central four-point engraving.
                    float next=cursor+(slab%3==1?1.75f:1.45f);
                    if(u1-next<.55f)next=u1;
                    float left=Mathf.Max(u0,cursor),right=Mathf.Min(u1,next);
                    float tone=1.005f+.012f*((slab+row*2)%3);
                    GateProcessionalFlag(stone,field,left,bottom,right,top,tone);
                    cursor=next;
                }
            }
        }

        private static void BuildGateApronCourses(Builder stone)
        {
            // Two quiet service-edge courses join the panel to the existing flush
            // threshold at u26.5. Their half-unit shoulders stay wholly on the old
            // walking surface, away from the boilers and the foreground rail.
            float end=GateU(30.5f);
            foreach(float v in new[]{.7f,8.65f})
            {
                var strip=new[]{new Vector2(16.2f,v),new Vector2(end,v),
                    new Vector2(end,v+.65f),new Vector2(16.2f,v+.65f)};
                CeremonialStoneFace(stone,strip,.80f);
                int count=Mathf.CeilToInt((end-16.2f)/1.7f);
                for(int k=0;k<count;k++)
                {
                    float left=Mathf.Lerp(16.2f,end,k/(float)count);
                    float right=Mathf.Lerp(16.2f,end,(k+1)/(float)count);
                    GateProcessionalFlag(stone,strip,left,v,right,v+.65f,1.035f);
                }
            }
        }

        private static void GateProcessionalFlag(Builder stone,Vector2[] contour,
            float u0,float v0,float u1,float v1,float tone)
        {
            const float joint=.024f,bevel=.045f;
            if(u1-u0<=2*bevel||v1-v0<=2*bevel)return;
            // Both clips share the exact panel outline. The narrow rubbed margin
            // remains flush; every triangle is split at the shared light lattice and
            // ramp breaks by CeremonialStoneFace, including clipped corner flags.
            var outer=ClipFlagCell(contour,u0+joint,v0+joint,u1-joint,v1-joint);
            if(outer.Count<3)return;
            CeremonialStoneFace(stone,outer.ToArray(),tone*.965f);
            var face=ClipFlagCell(contour,u0+bevel,v0+bevel,u1-bevel,v1-bevel);
            if(face.Count>=3)CeremonialStoneFace(stone,face.ToArray(),tone);
        }

        // World-continuous stone UV/light sampled in half-unit cells. Clipping
        // bounds every triangle and splits the gate's u25/u27 grade breaks exactly.
        private static void CeremonialStoneFace(Builder mesh,Vector2[] polygon,float tone)
        {
            float u0=polygon[0].x,u1=u0,v0=polygon[0].y,v1=v0;
            foreach(var p in polygon){u0=Mathf.Min(u0,p.x);u1=Mathf.Max(u1,p.x);v0=Mathf.Min(v0,p.y);v1=Mathf.Max(v1,p.y);}
            float[] us=LightLines(u0,u1,.5f),vs=LightLines(v0,v1,.5f);
            for(int y=0;y+1<vs.Length;y++)for(int x=0;x+1<us.Length;x++)
            {
                var clipped=ClipFlagCell(polygon,us[x],vs[y],us[x+1],vs[y+1]);
                if(clipped.Count<3)continue;
                int first=mesh.V.Count;
                foreach(var p in clipped)
                    PavingVertex(mesh,p.x,p.y,tone,new Color(1.025f,1.015f,.99f),FloorLight(p.x,p.y));
                for(int i=1;i+1<clipped.Count;i++)
                {
                    if(Mathf.Abs(FlagCross(clipped[i]-clipped[0],clipped[i+1]-clipped[0]))<1e-7f)continue;
                    mesh.T.Add(first);mesh.T.Add(first+i);mesh.T.Add(first+i+1);
                }
            }
        }

        // Environmental inlay is rubbed, aged brass rather than a polished outline.
        // Broad continuous wear stays attached to the floor and shares its existing
        // illumination later at Emit. No broken motif, extra geometry or random flicker.
        private static void WeatherHeraldry(Builder mesh)
        {
            for(int i=0;i<mesh.V.Count;i++)
            {
                if(!OnCopingRail(mesh.U[i]))continue;
                FoundrySurface.ToDesign(mesh.V[i].x,mesh.V[i].y,out float u,out float v);
                float wear=Mathf.Lerp(.88f,1f,FloorPatina(u*.37f+11,v*.37f-5));
                Color tone=mesh.C[i];
                mesh.C[i]=new Color(tone.r*.96f*wear,tone.g*.98f*wear,tone.b*wear,tone.a);
                // Sample the rail's metal centre, excluding its nearly black outer
                // frame and strongest bright bevel. The new UV remains within the
                // original rail rectangle so LightFloorDecals still classifies it as
                // brass and applies the unchanged bounded, non-emissive response.
                Vector2 uv=mesh.U[i];
                float across=Mathf.InverseLerp(CopingRail.y,CopingRail.w,uv.y);
                uv.y=Mathf.Lerp(1-18/724f,1-7/724f,across);
                mesh.U[i]=uv;
            }
        }

        // A crack-free 72x88 px patch of panel stone (texel ~.18, std .005): the seal's fills read as one dark stone,
        // not as magnified cracks, rails or frames (floor UVs would tile the whole coping sheet across the disc).
        private static readonly Vector4 SealStone=new Vector4(2024/2172f,1-443/724f,2096/2172f,1-355/724f);

        private static Vector2 SealPolar(float angle,float radius) => new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius;

        // Where the ray at this angle crosses the straight star edge tip-valley.
        private static Vector2 SealEdge(Vector2 tip,Vector2 valley,float angle)
        {
            Vector2 d=SealPolar(angle,1),e=valley-tip;
            return tip+e*Mathf.Clamp01((d.y*tip.x-d.x*tip.y)/(d.x*e.y-d.y*e.x));
        }

        private static void SealQuad(Builder mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color,Vector4 region)
        {
            mesh.Quad(Project(a.x,a.y),Project(b.x,b.y),Project(c.x,c.y),Project(d.x,d.y),false,color);
            mesh.MapLast(region,false);
        }
    }
}
