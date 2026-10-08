using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The seal every run opens on, read at arm's length: a brass-lined compass star inside a brass ring pair, after
        // the reference medallion. Paving shows through the star and only the field between its points darkens, so the
        // brass separates without a glow, a pit or an obstacle under the range ring and telegraphs. Flush, lit only as
        // the paving under it (LightFloorDecals), and inside r 3.9 (the shaft corner is past r 4.2); the rings sit on
        // BuildCourtStoneRing's annulus (3.26-3.97).
        private void BuildCourtSeal(ArenaView arena)
        {
            const float Field=3.4f,Inner=3.44f,Outer=3.82f;
            var seal=new Builder();
            var brass=new Color(.7f,.56f,.34f);var lit=new Color(.81f,.64f,.37f);var channel=new Color(.47f,.45f,.41f);
            // Points on the compass rays: four major on the screen axes (its odd rays), four minor on its diagonals.
            var tips=new Vector2[8];var valleys=new Vector2[8];
            for(int i=0;i<8;i++){tips[i]=SealPolar(i*Mathf.PI/4,i%2==1?3.12f:2.05f);valleys[i]=SealPolar((i+.5f)*Mathf.PI/4,.78f);}
            // Darker field: each half-point's sector from the star edge out under the inner ring, stepped along the
            // edge so it meets the star exactly. A flat patch of panel stone, half transparent, so paving still reads.
            var field=new Color(1,1,.93f,.44f);
            for(int i=0;i<8;i++)for(int side=-1;side<=1;side+=2)
            {
                Vector2 tip=tips[i],valley=valleys[side>0?i:(i+7)%8];
                for(int k=0;k<4;k++)
                {
                    float a=i*Mathf.PI/4+side*k*Mathf.PI/32,b=i*Mathf.PI/4+side*(k+1)*Mathf.PI/32;
                    SealQuad(seal,SealEdge(tip,valley,a),SealEdge(tip,valley,b),SealPolar(b,Field),SealPolar(a,Field),field,SealStone);
                }
            }
            // One facet of each point in light shade, as a carved compass rose; the other stays bare paving.
            for(int i=0;i<8;i++)SealQuad(seal,Vector2.zero,tips[i],valleys[i],valleys[i],new Color(1,1,.93f,.2f),SealStone);
            // Grooves first, brass second, so no dark channel overdraws a brass join.
            for(int pass=0;pass<2;pass++)
            {
                bool metal=pass==1;var trim=metal?CopingTrim.Brass:CopingTrim.Stone;
                for(int i=0;i<8;i++)
                {
                    bool major=i%2==1;
                    float width=metal?(major?.09f:.07f):(major?.15f:.12f);Color tone=metal?(major?lit:brass):channel;
                    Strip(seal,tips[i].x,tips[i].y,valleys[i].x,valleys[i].y,width,tone,trim,i*2);
                    Vector2 back=valleys[(i+7)%8];
                    Strip(seal,tips[i].x,tips[i].y,back.x,back.y,width,tone,trim,i*2+1);
                    // A lozenge ties the ring pair on every ray; the compass ray carries the line on from r 4.1.
                    Vector2 d=SealPolar(i*Mathf.PI/4,1),c=d*((Inner+Outer)*.5f),along=d*(metal?.17f:.21f);
                    Vector2 across=new Vector2(-d.y,d.x)*((major?.1f:.07f)+(metal?0:.04f));
                    SealQuad(seal,c-along,c+across,c+along,c-across,metal?lit:channel,metal?CopingRail:SealStone);
                }
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                    Vector2 p=SealPolar(a,Outer),q=SealPolar(b,Outer),m=SealPolar(a,Inner),n=SealPolar(b,Inner);
                    Strip(seal,p.x,p.y,q.x,q.y,metal?.1f:.17f,metal?lit:channel,trim,i);
                    Strip(seal,m.x,m.y,n.x,n.y,metal?.07f:.12f,metal?brass:channel,trim,i+3);
                }
            }
            Create("Court engraved forge seal",seal,arena.PlatformMaterials.Coping,FloorDecal);
        }

        // The reference's compass: eight brass rays leave the seal and meet a wide outer ring, so the open court -
        // where the portrait camera spends most of a run - reads as laid-out paving rather than an empty sheet.
        // Every piece is flush and clipped to the walking union, so the ring breaks where the shaft cuts the court.
        private static void BuildCourtCompass(Builder inlays,Color brass)
        {
            const float Ring=8.6f;
            // A narrow worn channel either side of a brighter inlay: the brass has to read as metal, not as a crack.
            var channel=new Color(.47f,.45f,.41f);
            var lit=new Color(brass.r*1.16f,brass.g*1.14f,brass.b*1.1f);
            for(int i=0;i<96;i++)
            {
                float a=i*Mathf.PI/48,b=(i+1)*Mathf.PI/48;
                Vector2 p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*Ring,q=new Vector2(Mathf.Cos(b),Mathf.Sin(b))*Ring;
                if(!OnCourt(p)||!OnCourt(q)||!OnCourt((p+q)*.5f))continue;
                Strip(inlays,p.x,p.y,q.x,q.y,.17f,channel,CopingTrim.Stone,i);
                Strip(inlays,p.x,p.y,q.x,q.y,.11f,lit,CopingTrim.Brass);
            }
            for(int ray=0;ray<8;ray++)
            {
                Vector2 direction=new Vector2(Mathf.Cos(ray*Mathf.PI/4),Mathf.Sin(ray*Mathf.PI/4));
                // Short pieces, each kept only if it lies on the court, so a ray stops cleanly at the shaft collar.
                for(float r=4.1f;r<Ring-.1f;r+=.5f)
                {
                    Vector2 p=direction*r,q=direction*Mathf.Min(r+.5f,Ring-.1f);
                    if(!OnCourt(p)||!OnCourt(q))continue;
                    Strip(inlays,p.x,p.y,q.x,q.y,ray%2==0?.15f:.12f,channel,CopingTrim.Stone,ray+(int)(r*2));
                    Strip(inlays,p.x,p.y,q.x,q.y,ray%2==0?.09f:.07f,lit,CopingTrim.Brass);
                }
                // A brass lozenge where each cardinal ray meets the ring, as the reference marks its star points. On the
                // rail like the seal's lozenges: its local quad UVs sampled the sheet's corner, a dark stone patch with a
                // brass edge that would also have taken the floor light as stone.
                Vector2 tip=direction*Ring;
                if(ray%2==0&&OnCourt(tip))
                {
                    Vector2 side=new Vector2(-direction.y,direction.x)*.3f,along=direction*.42f;
                    SealQuad(inlays,tip-along,tip+side,tip+along,tip-side,lit,CopingRail);
                }
            }
        }

        private static bool OnCourt(Vector2 design)
        {
            FoundryMap.ToFloor(design.x,design.y,out float x,out float y);
            return FoundryMap.Contains(x,y,.2f);
        }

        // A flush processional inlay makes the long gate apron read as a destination.
        // Matte stone/brass, with no emission or animation: this is architecture, not an
        // ability telegraph or a second interactive exit. The whole panel stays inside
        // the existing lane and is lit with the floor in the shared decal batch.
        private void BuildGateProcession(ArenaView arena)
        {
            var inlay=new Builder();
            var groove=new Color(.48f,.46f,.43f);
            var brass=new Color(.70f,.58f,.39f);
            var edge=new Color(.78f,.66f,.46f);
            var frame=new[]{new Vector2(15.8f,2),new Vector2(16.5f,1.3f),
                new Vector2(29.9f,1.3f),new Vector2(30.6f,2),new Vector2(30.6f,8),
                new Vector2(29.9f,8.7f),new Vector2(16.5f,8.7f),new Vector2(15.8f,8)};
            var inner=new[]{new Vector2(16.2f,2.15f),new Vector2(16.65f,1.7f),
                new Vector2(29.75f,1.7f),new Vector2(30.2f,2.15f),new Vector2(30.2f,7.85f),
                new Vector2(29.75f,8.3f),new Vector2(16.65f,8.3f),new Vector2(16.2f,7.85f)};
            for(int pass=0;pass<2;pass++)
            {
                bool metal=pass==1;
                var trim=metal?CopingTrim.Brass:CopingTrim.Stone;
                Color tone=metal?brass:groove;
                for(int i=0;i<frame.Length;i++)
                {
                    Vector2 p=frame[i],q=frame[(i+1)%frame.Length];
                    Strip(inlay,p.x,p.y,q.x,q.y,metal?.075f:.14f,tone,trim,i);
                    p=inner[i];q=inner[(i+1)%inner.Length];
                    Strip(inlay,p.x,p.y,q.x,q.y,metal?.035f:.075f,tone,trim,i+7);
                }
                Vector2 centre=new Vector2(23.2f,5);
                for(int i=0;i<48;i++)
                {
                    float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;
                    foreach(float radius in new[]{1.72f,1.96f})
                    {
                        Vector2 p=centre+SealPolar(a,radius),q=centre+SealPolar(b,radius);
                        Strip(inlay,p.x,p.y,q.x,q.y,metal?.055f:.11f,tone,trim,i);
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
                    Strip(inlay,p.x,p.y,q.x,q.y,metal?.07f:.13f,metal?edge:groove,trim,i);
                }
                foreach(var ends in new[]{new Vector2(17.1f,20.38f),new Vector2(26.02f,29.3f)})
                    Strip(inlay,ends.x,5,ends.y,5,metal?.055f:.11f,tone,trim);
                // Four short ties connect the framing to the medallion without filling
                // the fighting lane with a repeated luminous-looking pattern.
                foreach(float u in new[]{18f,28.4f})foreach(float v in new[]{1.5f,8.5f})
                {
                    Vector2 c=new Vector2(u,v),along=new Vector2(.32f,0),across=new Vector2(0,.18f);
                    SealQuad(inlay,c-along,c+across,c+along,c-across,metal?edge:groove,metal?CopingRail:SealStone);
                }
            }
            Create("Gate processional stone and brass inlay",inlay,arena.PlatformMaterials.Coping,FloorDecal);
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
