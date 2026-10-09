using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Rear loading gantry uses only the existing non-walkable stock shelf.
        // The rear feet clear the tank (ending at v22.1); stock remains in front.
        private void BuildStockHoist(ArenaView arena)
        {
            int owner=RegisterDepthFootprint(StockShelf.x,StockShelf.y,StockShelf.z,StockShelf.w,3.6f,true,false);
            using(new DepthScope(this,owner))
            {
                var frame=new Builder();
                foreach(float u in new[]{2.3f,6.7f})
                {
                    StockHull(frame,u-.2f,22.13f,u+.2f,22.38f,.18f,.38f,.025f);
                    StockHull(frame,u-.12f,22.17f,u+.12f,22.35f,.38f,3.08f,.025f);
                    StockHull(frame,u-.18f,22.13f,u+.18f,22.38f,2.86f,3.12f,.025f);
                }
                // Broad rolled crosshead, with a continuous underside and top flange.
                StockHull(frame,2.08f,22.13f,6.92f,22.37f,3.03f,3.38f,.035f);
                StockHull(frame,2.05f,22.1f,6.95f,22.4f,3.32f,3.44f,.025f);
                int braceStart=frame.V.Count;
                ArchitectureBar(frame,StockProject(2.3f,22.26f)+Vector3.up*2.35f,
                    StockProject(3.15f,22.26f)+Vector3.up*3.05f,.15f,new Color(.57f,.62f,.68f));
                ArchitectureBar(frame,StockProject(6.7f,22.26f)+Vector3.up*2.35f,
                    StockProject(5.85f,22.26f)+Vector3.up*3.05f,.15f,new Color(.57f,.62f,.68f));
                WorkedAnvilSurface(frame,braceStart,frame.V.Count,StockProject(2.3f,22.26f),1.4f);
                // Two bearing blocks carry a large drum below the crosshead.
                StockHull(frame,3.32f,21.95f,3.65f,22.32f,2.38f,3.04f,.04f);
                StockHull(frame,5.35f,21.95f,5.68f,22.32f,2.38f,3.04f,.04f);
                Create("Stock loading gantry frame",frame,arena.PlatformMaterials.FoundryIron,-11);

                Vector3 a=StockProject(3.57f,22.08f)+Vector3.up*2.66f;
                Vector3 b=StockProject(5.43f,22.08f)+Vector3.up*2.66f;
                StoredStockTube(arena,a,b,.60f,2.66f);
                var drum=new Builder();
                Vector3 axis=(b-a).normalized,normal=new Vector3(-axis.y,axis.x,0);
                // Distinct flanges and broad wound cable bands make one machine,
                // rather than another pile of disconnected tiny metal details.
                foreach(float t in new[]{0f,1f})
                {
                    Vector3 c=Vector3.Lerp(a,b,t);
                    for(int i=0;i<20;i++)
                    {
                        float x=i*Mathf.PI*2/20,y=(i+1)*Mathf.PI*2/20;
                        Vector3 p=c+normal*Mathf.Cos(x)*.4f+axis*Mathf.Sin(x)*.1f;
                        Vector3 q=c+normal*Mathf.Cos(y)*.4f+axis*Mathf.Sin(y)*.1f;
                        int first=drum.V.Count;
                        FullFace(drum,c,p,q,c,new Color(.46f,.5f,.57f));
                        WorkedStockFace(drum,first,c,1.45f,2.66f,2.66f);
                    }
                }
                for(int i=0;i<7;i++)
                {
                    Vector3 c=Vector3.Lerp(a,b,(i+1f)/8);
                    int first=drum.V.Count;
                    ArchitectureBar(drum,c-normal*.285f,c+normal*.285f,.045f,new Color(.27f,.29f,.32f));
                    WorkedStockFace(drum,first,c,1.3f,2.66f,2.66f);
                }
                Create("Stock hoist drum flanges and winding",drum,arena.PlatformMaterials.FoundryIron,-11);
                // Suspended lifting yoke stops above the existing reserve stock.
                foreach(float u in new[]{3.8f,5.15f})
                    Chain(arena,StockProject(u,22.04f)+Vector3.up*2.38f,
                        StockProject(u,22.04f)+Vector3.up*1.13f,.055f,-11);
                var yoke=new Builder();
                StockHull(yoke,3.62f,21.93f,5.33f,22.15f,1.0f,1.17f,.025f);
                Create("Stock hoist lifting yoke",yoke,arena.PlatformMaterials.FoundryIron,-11);
            }
        }
    }
}
