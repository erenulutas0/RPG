using Cryptforge.Art;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private void BuildEntranceBridgeSupports(ArenaView arena)
        {
            // Repeated transverse masonry bents support the longer real deck. All
            // geometry is below its sidewall; the arch openings remain open void.
            foreach(float s in new[]{-39.2f,-52f,FoundryMap.BridgeStart+1.2f})
            {
                // Shared transparent stone batch: far rows precede near rows;
                // all recessed returns precede that row's solid front silhouette.
                var stone=new Builder();var front=new Builder();
                const float spring=-6.1f,depth=1.3f;
                float half=FoundryMap.BridgeHalfWidth;
                Vector3 At(float d,float h,float along=0)=>Deck(s+along,d)+Vector3.up*h;
                for(int i=0;i<11;i++)
                {
                    float a=i*Mathf.PI/11,b=(i+1)*Mathf.PI/11;
                    Vector3 innerA=At(Mathf.Cos(a)*(half-1.05f),spring+Mathf.Sin(a)*2.35f);
                    Vector3 innerB=At(Mathf.Cos(b)*(half-1.05f),spring+Mathf.Sin(b)*2.35f);
                    Vector3 outerA=At(Mathf.Cos(a)*(half+.18f),spring+Mathf.Sin(a)*3.42f);
                    Vector3 outerB=At(Mathf.Cos(b)*(half+.18f),spring+Mathf.Sin(b)*3.42f);
                    Color face=new Color(.60f,.59f,.66f)*(i==5?1.08f:1f);
                    GateStonePatch(front,innerA,innerB,outerB,outerA,face);
                    Vector3 back=Deck(s+depth,0)-Deck(s,0);
                    GateStonePatch(stone,innerA,innerA+back,innerB+back,innerB,new Color(.31f,.34f,.43f));
                    // The filled spandrel bears the straight underside of the deck.
                    GateStonePatch(front,outerA,outerB,At(Mathf.Cos(b)*(half+.18f),-2.7f),
                        At(Mathf.Cos(a)*(half+.18f),-2.7f),new Color(.45f,.46f,.54f));
                }
                foreach(float sign in new[]{-1f,1f})
                {
                    float a=sign*(half-1.05f),b=sign*(half+.18f);
                    GateStonePatch(front,At(a,-8.4f),At(b,-8.4f),At(b,spring),At(a,spring),new Color(.54f,.54f,.62f));
                    GateStonePatch(stone,At(b,-8.4f),At(b,-8.4f,depth),At(b,spring,depth),At(b,spring),new Color(.37f,.4f,.48f));
                    GateStonePatch(stone,At(a,-8.4f,depth),At(a,-8.4f),At(a,spring),At(a,spring,depth),new Color(.3f,.33f,.42f));
                }
                ShadeUndercroftDepth(stone,Deck(s,0),Deck(s,0));
                ShadeUndercroftDepth(front,Deck(s,0),Deck(s,0));
                Create("Entrance bridge masonry returns",stone,arena.PlatformMaterials.FoundryWall,-16);
                Create("Entrance bridge masonry bents",front,arena.PlatformMaterials.FoundryWall,-16);
            }
        }
    }
}
