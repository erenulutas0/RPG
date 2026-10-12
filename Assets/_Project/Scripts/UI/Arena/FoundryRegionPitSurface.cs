using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // The lining atlas also supplies the pool-facing wall and vessels. Its tall
        // riveted border is unsuitable for a narrow raised rib: the entire border
        // enlarged both the fasteners and bronze ties. Reuse the plain cast grain
        // beside those rivets, with bounded-height pieces and integral worn edges.
        // This partitions the existing faces, never extrudes them or adds overlays.
        private static void PitCastLiningFace(Builder b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,
            float top,float bottom,Vector2 normal,float gain,int wornEdge)
        {
            int strips=Mathf.Max(1,Mathf.CeilToInt((bottom-top)/.84f));
            for(int strip=0;strip<strips;strip++)
            {
                float from=strip/(float)strips,to=(strip+1)/(float)strips;
                float high=Mathf.Lerp(top,bottom,from),low=Mathf.Lerp(top,bottom,to);
                Vector3 leftLow=Vector3.Lerp(e,a,to),rightLow=Vector3.Lerp(d,c,to);
                Vector3 rightHigh=Vector3.Lerp(d,c,from),leftHigh=Vector3.Lerp(e,a,from);
                // About 60 texels per design unit vertically. Neither the brass
                // straps nor rivet heads enter this small, opaque atlas patch.
                var sample=new Vector4(.047f,.615f,.066f,.615f+(low-high)*.040f);
                if(wornEdge<0)
                {
                    PitLiningReliefFace(b,leftLow,rightLow,rightHigh,leftHigh,sample,high,low,normal,gain,true);
                    continue;
                }
                // A 0.035-unit cast arris on the outside of each 0.33-unit half.
                // The two halves retain a shared central face without a fake seam.
                float cut=wornEdge==0?.035f/.33f:1-.035f/.33f;
                Vector3 lower=Vector3.Lerp(leftLow,rightLow,cut),upper=Vector3.Lerp(leftHigh,rightHigh,cut);
                float u=Mathf.Lerp(sample.x,sample.z,cut);
                PitLiningReliefFace(b,leftLow,lower,upper,leftHigh,
                    new Vector4(sample.x,sample.y,u,sample.w),high,low,normal,gain*(wornEdge==0?1.27f:1f),true);
                PitLiningReliefFace(b,lower,rightLow,rightHigh,upper,
                    new Vector4(u,sample.y,sample.z,sample.w),high,low,normal,gain*(wornEdge==1?1.17f:1f),true);
            }
        }

        private static void PitBearingStoneFace(Builder b,Vector3 a,Vector3 c,Vector3 d,Vector3 e,
            float top,float bottom,Vector2 normal,float width,float gain)
        {
            // Cool upper masonry, clear of the atlas's baked firelight and joints.
            // UV aspect follows the original stone chord so the three bevel faces
            // keep comparable grain scale. The narrow joined block bed belongs to
            // this surface, instead of reading as another painted brown iron rail.
            float v=.87f+(bottom-top)*(.20f*1024f/1536f)/width;
            var sample=new Vector4(.43f,.87f,.63f,v);
            float joint=.016f/width;
            Vector3 lower=Vector3.Lerp(a,c,joint),upper=Vector3.Lerp(e,d,joint);
            float u=Mathf.Lerp(sample.x,sample.z,joint);
            PitLiningReliefFace(b,a,lower,upper,e,
                new Vector4(sample.x,sample.y,u,sample.w),top,bottom,normal,gain*.59f,false);
            PitLiningReliefFace(b,lower,c,d,upper,
                new Vector4(u,sample.y,sample.z,sample.w),top,bottom,normal,gain,false);
        }
    }
}
