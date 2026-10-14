using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private void BuildStockReceivingGroup(ArenaView arena)
        {
            var stock=new Builder();
            StockReceivingPack(stock);
            int owner=RegisterDepthFootprint(StockShelf.x,StockShelf.y,StockShelf.z,StockShelf.w,1.6f,true,false);
            // The bundle is ahead of the tool rack and between the pipe ends and
            // existing crate. It shares the shelf's depth and current iron batch.
            Create("Supported received billet bundle",stock,arena.PlatformMaterials.FoundryIron,-11,owner);
        }

        private static void StockReceivingPack(Builder stock)
        {
            Vector3 origin=StockProject(5.64f,19.02f)+Vector3.up*.42f;
            int first=stock.V.Count;
            foreach(float v in new[]{18.67f,19.22f})
                StockHull(stock,5.18f,v,6.10f,v+.16f,.18f,.30f,.018f);
            YardBenchTimberFinish(stock,first,stock.V.Count,origin);

            first=stock.V.Count;
            // Three short cast blanks sit directly on the two substantial
            // packing runners. Their upper course bears across both lower bars.
            StockHull(stock,5.24f,18.70f,5.54f,19.34f,.30f,.48f,.025f);
            StockHull(stock,5.63f,18.73f,5.93f,19.32f,.30f,.48f,.025f);
            StockHull(stock,5.43f,18.76f,5.73f,19.30f,.48f,.66f,.025f);
            YardBenchIronFinish(stock,first,stock.V.Count,origin);
            // Mill scale belongs to whole received blanks, distinct from the
            // cooler retaining iron and the existing long pipe stock beside it.
            for(int i=first;i<stock.C.Count;i++)
            {
                Color c=stock.C[i];
                stock.C[i]=new Color(c.r*1.10f,c.g*.99f,c.b*.88f,1);
            }

            first=stock.V.Count;
            StockHull(stock,5.18f,18.70f,5.23f,18.83f,.30f,.55f,.010f);
            StockHull(stock,5.97f,18.70f,6.02f,18.83f,.30f,.55f,.010f);
            YardBenchIronFinish(stock,first,stock.V.Count,origin);
        }

        private void BuildCastingFlaskStorage(ArenaView arena)
        {
            var stock=new Builder();
            CastingFlaskStore(stock);
            int owner=RegisterDepthFootprint(CastingShelf.x,CastingShelf.y,CastingShelf.z,CastingShelf.w,2.5f,true,false);
            // Reuse the crane-roof iron rank behind the authored bench. Its legs
            // and front stretcher must continue to occlude the stored tooling.
            Create("Casting bench supported spare flasks",stock,arena.PlatformMaterials.FoundryIron,-12.5f,owner);
        }

        private static void CastingFlaskStore(Builder stock)
        {
            Vector3 origin=Project(-15.5f,20.06f)+Vector3.up*.42f;
            int first=stock.V.Count;
            foreach(float v in new[]{19.71f,20.25f})
                WorkshopMetalHull(stock,-17.0f,v,-14.0f,v+.16f,.18f,.31f,.018f,true);
            YardBenchTimberFinish(stock,first,stock.V.Count,origin);

            first=stock.V.Count;
            // Both pairs sit below the existing .81 worktop, between its legs.
            // The nested upper rings bear on the lower rings' broad rail tops;
            // their empty centres stay open rather than becoming closed crates.
            YardStoredFlask(stock,-16.98f,19.73f,-15.50f,20.38f,.31f,.47f);
            YardStoredFlask(stock,-16.96f,19.75f,-15.52f,20.36f,.47f,.63f);
            YardStoredFlask(stock,-15.29f,19.73f,-14.02f,20.38f,.31f,.47f);
            YardStoredFlask(stock,-15.27f,19.75f,-14.04f,20.36f,.47f,.63f);
            YardBenchIronFinish(stock,first,stock.V.Count,origin);
        }
    }
}
