using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _craneMaterial;
        private bool BuildBakedCrane(Vector3 foot)
        {
            if(_craneMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundryCrane");
                if(texture==null)return false;
                _craneMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Authored foundry crane metal",mainTexture=texture};
            }
            // Own the complete original crane footing, including the upper pivot.
            // This is not a new landmark claiming nearby stock or unrelated props.
            using(MassDepth(WestBox(-27,11,-24,15),8.6f))
            {
                var card=new Builder();
                card.V.AddRange(new[]{foot+new Vector3(-7.4f,-.4f,0),foot+new Vector3(3.4f,-.4f,0),
                    foot+new Vector3(3.4f,10.4f,0),foot+new Vector3(-7.4f,10.4f,0)});
                card.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                for(int i=0;i<4;i++)card.C.Add(Color.white);
                card.T.AddRange(new[]{0,1,2,0,2,3});
                // Quantized -9: in front of rear stock, behind live chain/load/lantern.
                Create("Authored crane mast and jib",card,_craneMaterial,-11.5f);
            }
            return true;
        }
    }
}
