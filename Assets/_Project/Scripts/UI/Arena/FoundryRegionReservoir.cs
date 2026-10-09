using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _reservoirMaterial;
        private bool BuildBakedPressureReservoir(ArenaView arena,float u,float v)
        {
            if(_reservoirMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundryReservoir");
                if(texture==null)return false;
                _reservoirMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Rolled pressure reservoir",mainTexture=texture};
            }
            // Explicit ownership prevents the overlapping service-main landmark from
            // claiming the far card. Both retain their original 2x2 ground footprint.
            using(MassDepth(new Vector4(u-1,v-1,u+1,v+1),3.7f))
            {
                Vector3 origin=Project(u,v);
                var b=new Builder();
                b.V.AddRange(new[]{origin+new Vector3(-2,-.5f,0),origin+new Vector3(2,-.5f,0),
                    origin+new Vector3(2,3.5f,0),origin+new Vector3(-2,3.5f,0)});
                b.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                for(int i=0;i<4;i++)b.C.Add(Color.white);
                b.T.AddRange(new[]{0,1,2,0,2,3});
                // Half-step ranks survive scenery quantization: far tank / mains / near tank.
                Create("Baked pressure reservoir",b,_reservoirMaterial,v>0?-11.5f:-10.5f);
            }
            return true;
        }
    }
}
