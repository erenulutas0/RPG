using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _stockStationMaterial;
        private bool BuildBakedStockStation()
        {
            if(_stockStationMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundryStockStation");
                if(texture==null)return false;
                _stockStationMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Authored foundry stock station",mainTexture=texture};
            }
            int owner=RegisterDepthFootprint(StockShelf.x,StockShelf.y,StockShelf.z,StockShelf.w,1.6f,true,false);
            using(new DepthScope(this,owner))
            {
                Vector3 origin=StockProject(4.5f,20.45f);
                var card=new Builder();
                card.V.AddRange(new[]{origin+new Vector3(-3.15f,-2.45f,0),origin+new Vector3(3.15f,-2.45f,0),
                    origin+new Vector3(3.15f,3.85f,0),origin+new Vector3(-3.15f,3.85f,0)});
                card.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
                for(int i=0;i<4;i++)card.C.Add(Color.white);
                card.T.AddRange(new[]{0,1,2,0,2,3});
                // Shelf -12, rear hoist -11, stock -10, crane -9, live wood -8.
                Create("Baked metal stock station",card,_stockStationMaterial,-12f);
            }
            return true;
        }
    }
}
