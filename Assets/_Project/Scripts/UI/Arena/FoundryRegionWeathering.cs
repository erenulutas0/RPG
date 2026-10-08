using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Static surface treatment, sampled while building existing stone batches. Height is authored
        // above the floor, not inferred from a projected screen coordinate (which would slide light
        // sideways up a tower). No runtime update, new material, collision or depth ownership.
        internal static Color ArchitectureLight(float u,float v,float height,float belowLedge)
        {
            // Painted masonry already carries individual block variation. Keep the overlaid deposit
            // broad and shallow so neighbouring caps/faces read as one structure rather than patches.
            // The previous .87..1.035 range has the same midpoint; this is contrast restraint, not a lift.
            float streak=FloorPatina(u*.31f+height*.025f,v*.31f-height*.025f);
            float deposit=Mathf.Lerp(.905f,1f,streak);
            float contact=1-.2f*Mathf.Exp(-Mathf.Max(0,belowLedge)/.32f);
            float heat=0;
            Color warm=Color.clear;
            foreach(var lamp in ForgeLamps())
            {
                float horizontal=Mathf.Max(0,DistanceFromFloorBox(u,v,lamp.Box.x,lamp.Box.y,lamp.Box.z,lamp.Box.w)-lamp.Round);
                float vertical=(height-lamp.Height)*1.15f;
                float distance=Mathf.Sqrt(horizontal*horizontal+vertical*vertical);
                float glow=lamp.Strength*SmoothFall(distance,lamp.Radius);
                heat+=glow;warm+=lamp.Tint*glow;
            }
            float saturation=1-Mathf.Exp(-heat);
            if(heat>.0001f)warm*=.8f*saturation/heat;
            Color light=new Color(1f,.985f,1.015f,1)+warm;
            light.g*=1-.07f*saturation;light.b*=1-.15f*saturation;
            light*=deposit*contact;light.a=1;
            return light;
        }

        // Non-emissive reflected forge light for industrial iron. Sample the authored
        // source heights and surface elevation, never the displayed screen y of a tower.
        // Directional form shading stays in the incoming metal tone. Calculated at build
        // only; fire textures and glow windows do not pass through this treatment.
        internal static Color MetalLight(float u,float v,float height)
        {
            float heat=0;
            Color warm=Color.clear;
            float worldHeight=height+FoundrySurface.Height(u);
            foreach(var lamp in ForgeLamps())
            {
                float horizontal=Mathf.Max(0,DistanceFromFloorBox(u,v,lamp.Box.x,lamp.Box.y,lamp.Box.z,lamp.Box.w)-lamp.Round);
                float sourceU=Mathf.Clamp(u,lamp.Box.x,lamp.Box.z);
                float vertical=(worldHeight-lamp.Height-FoundrySurface.Height(sourceU))*1.15f;
                float glow=lamp.Strength*SmoothFall(Mathf.Sqrt(horizontal*horizontal+vertical*vertical),lamp.Radius);
                heat+=glow;warm+=lamp.Tint*glow;
            }
            float saturation=1-Mathf.Exp(-heat);
            if(heat>.0001f)warm*=1.35f*saturation/heat;
            Color light=new Color(.96f,1f,1.04f,1)+warm;
            light.g*=1-.06f*saturation;light.b*=1-.18f*saturation;
            light.a=1;
            return light;
        }

        private static Color MetalTone(Color tone,Vector3 projected,float height)
        {
            FoundrySurface.ToDesign(projected.x,projected.y-height,out float u,out float v);
            Color result=Tinted(tone,MetalLight(u,v,height));
            float peak=Mathf.Max(result.r,Mathf.Max(result.g,result.b));
            if(peak>1.9f){float scale=1.9f/peak;result.r*=scale;result.g*=scale;result.b*=scale;}
            return result;
        }

        private static Color SurfaceTone(Color tone,float u,float v,float height,float belowLedge)
        {
            Color light=ArchitectureLight(u,v,height,belowLedge);
            Color result=Tinted(tone,light);
            float peak=Mathf.Max(result.r,Mathf.Max(result.g,result.b));
            if(peak>1.9f){float scale=1.9f/peak;result.r*=scale;result.g*=scale;result.b*=scale;}
            return result;
        }

        // Narrow courses resolve the shadow immediately below a ledge rather than darkening a
        // whole tall pier. UVs remain world-continuous, so splitting adds no new masonry joints.
        private static void WeatheredBoxFace(Builder b,Vector2 a,Vector2 c,float bottom,float top,Color tone)
        {
            int rows=Mathf.Clamp(Mathf.CeilToInt((top-bottom)/.75f),1,16);
            for(int row=0;row<rows;row++)
            {
                float lo=Mathf.Lerp(bottom,top,row/(float)rows),hi=Mathf.Lerp(bottom,top,(row+1f)/rows);
                int first=b.V.Count;
                b.WallQuad(Project(a.x,a.y)+Vector3.up*lo,Project(c.x,c.y)+Vector3.up*lo,
                    Project(c.x,c.y)+Vector3.up*hi,Project(a.x,a.y)+Vector3.up*hi,false,tone);
                b.C[first]=SurfaceTone(tone,a.x,a.y,lo,top-lo);
                b.C[first+1]=SurfaceTone(tone,c.x,c.y,lo,top-lo);
                b.C[first+2]=SurfaceTone(tone,c.x,c.y,hi,top-hi);
                b.C[first+3]=SurfaceTone(tone,a.x,a.y,hi,top-hi);
            }
        }

        private static void WeatheredCourseFace(Builder b,Vector2 c,float r0,float r1,float h0,float h1,float a,float e,Color tone)
        {
            int rows=Mathf.Clamp(Mathf.CeilToInt((h1-h0)/.75f),1,16);
            float uvRadius=(r0+r1)*.5f;
            for(int row=0;row<rows;row++)
            {
                float t0=row/(float)rows,t1=(row+1f)/rows;
                float lo=Mathf.Lerp(h0,h1,t0),hi=Mathf.Lerp(h0,h1,t1);
                float lowRadius=Mathf.Lerp(r0,r1,t0),highRadius=Mathf.Lerp(r0,r1,t1);
                Vector2 p=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*lowRadius,q=c+new Vector2(Mathf.Cos(e),Mathf.Sin(e))*lowRadius;
                Vector2 s=c+new Vector2(Mathf.Cos(e),Mathf.Sin(e))*highRadius,t=c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*highRadius;
                int first=b.V.Count;
                b.Quad(Project(p.x,p.y)+Vector3.up*lo,Project(q.x,q.y)+Vector3.up*lo,
                    Project(s.x,s.y)+Vector3.up*hi,Project(t.x,t.y)+Vector3.up*hi,false,tone);
                b.U[first]=new Vector2(a*uvRadius/2.8f,lo/2.4f);b.U[first+1]=new Vector2(e*uvRadius/2.8f,lo/2.4f);
                b.U[first+2]=new Vector2(e*uvRadius/2.8f,hi/2.4f);b.U[first+3]=new Vector2(a*uvRadius/2.8f,hi/2.4f);
                b.C[first]=SurfaceTone(tone,p.x,p.y,lo,h1-lo);b.C[first+1]=SurfaceTone(tone,q.x,q.y,lo,h1-lo);
                b.C[first+2]=SurfaceTone(tone,s.x,s.y,hi,h1-hi);b.C[first+3]=SurfaceTone(tone,t.x,t.y,hi,h1-hi);
            }
        }

        // Soot collects around vent mouths; the outer rim remains rubbed metal. Applied only to
        // existing iron roof vertices so the painted fire doors and emissive pieces stay untouched.
        private static Color RoofPatina(Color tone,Vector3 point,Vector3 centre,float radius)
        {
            float distance=(point-centre).magnitude/Mathf.Max(.1f,radius);
            float soot=.22f*Mathf.Exp(-distance*distance*7);
            float variation=.95f+.05f*FloorPatina(point.x*.65f,point.y*.65f);
            return new Color(tone.r*(1-soot)*variation,tone.g*(1-soot)*variation,tone.b*(1-soot*.85f)*variation,tone.a);
        }
    }
}
