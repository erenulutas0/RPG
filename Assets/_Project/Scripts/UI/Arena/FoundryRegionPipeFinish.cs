using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Authored-height service mains only. Wheels, beacons and stored lengths retain
        // their separate treatment. One static iron batch, original centreline/footprint.
        private void FinishedPressurePipe(ArenaView arena,Vector3 a,Vector3 b,float width,float height)
        {
            float length=Vector3.Distance(a,b);
            if(length<.0001f||width<=0)return;
            Vector3 axis=(b-a)/length,normal=new Vector3(-axis.y,axis.x,0);
            if(Vector2.Dot(new Vector2(normal.x,normal.y),ChainLight)<0)normal=-normal;
            var mesh=new Builder();
            FinishedPipeSection(mesh,a,b,normal,width*.5f,width*.5f,height,1f,0,1);
            // Couplings are formed sleeves, not a ladder of bright strips. Keep the old
            // maximum 1.32-diameter / .22-diameter axial envelope at either endpoint.
            int sleeves=Mathf.Max(1,Mathf.CeilToInt(length/2.6f));
            for(int i=0;i<=sleeves;i++)
            {
                Vector3 centre=Vector3.Lerp(a,b,i/(float)sleeves);
                Vector3 end=axis*width*.11f,shoulder=axis*width*.065f;
                float inner=width*.52f,outer=width*.66f;
                FinishedPipeSection(mesh,centre-end,centre-shoulder,normal,inner,outer,height,.84f,.36f,.42f);
                FinishedPipeSection(mesh,centre-shoulder,centre+shoulder,normal,outer,outer,height,1.08f,.42f,.58f);
                FinishedPipeSection(mesh,centre+shoulder,centre+end,normal,outer,inner,height,.64f,.58f,.64f);
            }
            Create("Formed service pipe sleeves",mesh,arena.PlatformMaterials.FoundryIron,-11);
        }

        private static void FinishedPipeSection(Builder mesh,Vector3 a,Vector3 b,Vector3 normal,
            float radiusA,float radiusB,float height,float gain,float grainStart,float grainEnd)
        {
            const int strips=8;
            int spans=Mathf.Max(1,Mathf.CeilToInt(Vector3.Distance(a,b)/.75f));
            for(int strip=0;strip<strips;strip++)
            {
                float t0=strip/(float)strips,t1=(strip+1f)/strips;
                float x0=Mathf.Cos(t0*Mathf.PI),x1=Mathf.Cos(t1*Mathf.PI);
                for(int span=0;span<spans;span++)
                {
                    float s0=span/(float)spans,s1=(span+1f)/spans;
                    Vector3 from=Vector3.Lerp(a,b,s0),to=Vector3.Lerp(a,b,s1);
                    float r0=Mathf.Lerp(radiusA,radiusB,s0),r1=Mathf.Lerp(radiusA,radiusB,s1);
                    int first=mesh.V.Count;
                    LinkQuad(mesh,from+normal*x0*r0,to+normal*x0*r1,
                        to+normal*x1*r1,from+normal*x1*r0,
                        MetalTone(FinishedPipeTone(t0,s0,gain),from,height),
                        MetalTone(FinishedPipeTone(t0,s1,gain),to,height),
                        MetalTone(FinishedPipeTone(t1,s1,gain),to,height),
                        MetalTone(FinishedPipeTone(t1,s0,gain),from,height));
                    // One uninterrupted field of rolled plate along the whole section,
                    // clear of atlas straps/rivets; no patch restart at lighting samples.
                    float u0=.2f+Mathf.Lerp(grainStart,grainEnd,s0)*.6f;
                    float u1=.2f+Mathf.Lerp(grainStart,grainEnd,s1)*.6f;
                    mesh.U[first]=new Vector2(u0,.31f+t0*.34f);
                    mesh.U[first+1]=new Vector2(u1,.31f+t0*.34f);
                    mesh.U[first+2]=new Vector2(u1,.31f+t1*.34f);
                    mesh.U[first+3]=new Vector2(u0,.31f+t1*.34f);
                }
            }
        }

        private static Color FinishedPipeTone(float around,float along,float gain)
        {
            // Broad rolled crown, cool unlit underside, dark sleeve contact. Eight
            // cross-section strips interpolate these values without a painted white line.
            float crown=Mathf.Max(0,Mathf.Cos((around-.28f)*Mathf.PI));
            float value=.24f+.91f*crown*crown;
            float millScale=.90f+.10f*Mathf.Sin(along*Mathf.PI);
            value*=gain*millScale;
            // Rolled service iron belongs to the cool boiler castings. The
            // existing MetalTone stage still owns localized reflected fire;
            // brass fittings and baked fire windows never use this stock tint.
            return new Color(value*.86f,value*.98f,value*1.16f,1);
        }
    }
}
