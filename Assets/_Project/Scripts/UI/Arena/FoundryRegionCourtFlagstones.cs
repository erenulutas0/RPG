using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The open court is laid in small hand-cut stones in loose courses, as in
        // the approved concept. Those joints, arrises, cracks and quarry values live
        // in the tiling FoundryPavingV10 raster (Tools/ArtReview/GenerateFoundryCutStone.py);
        // the mesh keeps only the packer's footprints as tessellation for the
        // continuous forge-light, wear and contact field. Geometric rectangular
        // joints here would draw a second, contradictory bond over the raster.
        // The gate apron is laid in it too, as on the concept; the seal's ring, the
        // gate's dressed threshold and border and the repaired work aprons keep
        // their own fitted geometry on the V8 grain.
        internal const float FlagstoneSpan=12f;

        private static bool CourtFlagstone(AshlarStone s)=>(s.Zone==0||s.Zone==2||s.Zone==3)&&!ServiceApronClaims(s);

        private static void CourtFlagstoneFace(Builder mesh,AshlarStone s)
        {
            float u0=s.I*.5f,v0=s.J*.5f,u1=(s.I+s.W)*.5f,v1=(s.J+s.H)*.5f;
            float[] us=LightLines(u0,u1,1),vs=LightLines(v0,v1,1);
            var lattice=new Color[us.Length*vs.Length];
            for(int l=0;l<vs.Length;l++)for(int k=0;k<us.Length;k++)lattice[l*us.Length+k]=FloorLight(us[k],vs[l]);
            // The same light refinement the laid ashlar used, so fire pools keep
            // their sampled shape; only the stone relief moved into the raster.
            float step=AshlarLightStep(us,vs,lattice);
            if(step<1)PavingGrid(mesh,LightLines(u0,u1,step),LightLines(v0,v1,step),1,Color.white);
            else PavingGrid(mesh,us,vs,1,Color.white,lattice);
        }

        // World-continuous flagstone UV, assigned after corner clipping and the
        // ceremonial cuts so every cut vertex samples the one physical field.
        private static void CourtFlagstoneField(Builder mesh)
        {
            for(int i=0;i<mesh.V.Count;i++)
            {
                FoundrySurface.ToDesign(mesh.V[i].x,mesh.V[i].y,out float u,out float v);
                mesh.U[i]=new Vector2(u/FlagstoneSpan,v/FlagstoneSpan);
            }
        }
    }
}
