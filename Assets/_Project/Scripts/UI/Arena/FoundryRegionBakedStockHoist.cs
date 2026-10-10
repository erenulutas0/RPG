using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _stockHoistMaterial;
        private bool BuildBakedStockHoist()
        {
            if(_stockHoistMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundryStockHoist");
                if(texture==null)return false;
                _stockHoistMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Forged stock loading hoist",mainTexture=texture};
            }
            // Use the original complete shelf depth without claiming any unrelated
            // stock through a new landmark. The tall crosshead owns the same depth.
            int owner=RegisterDepthFootprint(StockShelf.x,StockShelf.y,StockShelf.z,StockShelf.w,3.6f,true,false);
            using(new DepthScope(this,owner))
            {
                Vector3 origin=StockProject(4.5f,22.08f);
                var card=new Builder();
                card.V.AddRange(new[]{origin+new Vector3(-2.7f,-.9f,0),origin+new Vector3(2.7f,-.9f,0),
                    origin+new Vector3(2.7f,4.5f,0),origin+new Vector3(-2.7f,4.5f,0)});
                card.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                for(int i=0;i<4;i++)card.C.Add(Color.white);
                card.T.AddRange(new[]{0,1,2,0,2,3});
                // -11.5 quantizes to -9, behind foreground stock's -11 / -8.
                Create("Baked stock loading hoist",card,_stockHoistMaterial,-11.5f);
            }
            return true;
        }
    }
}
