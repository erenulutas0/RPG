using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _salvageMaterial;
        private bool BuildBakedSalvageStation(ArenaView arena)
        {
            if(_salvageMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundrySalvage");
                if(texture==null)return false;
                _salvageMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Forged salvage workstation",mainTexture=texture};
            }
            // The metal kit shares the original salvage shelf depth; masonry stays procedural.
            using(MassDepth(new Vector4(-29f,5,-24.45f,8.4f),1.6f))
            {
                Vector3 origin=Project(-26.725f,6.7f);
                var b=new Builder();
                b.V.AddRange(new[]{origin+new Vector3(-2.75f,-1.75f,0),origin+new Vector3(2.75f,-1.75f,0),
                    origin+new Vector3(2.75f,3.75f,0),origin+new Vector3(-2.75f,3.75f,0)});
                b.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                for(int i=0;i<4;i++)b.C.Add(Color.white);
                b.T.AddRange(new[]{0,1,2,0,2,3});
                // Draw above the rear masonry landing; the compact card no longer shares the broad iron bounds.
                Create("Baked salvage metal kit",b,_salvageMaterial,-10.5f);
            }
            return true;
        }
    }
}
