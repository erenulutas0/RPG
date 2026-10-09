using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static void DressedKeeperSeat(Builder stone,Vector2 p)
        {
            // A broad seating slab, not an extra pedestal. Deeper front/rear clips
            // remove the long diamond tips while shallow lateral clips still support
            // the wide octagonal base already painted into the keeper artwork.
            // Entire geometry stays inside the previous +/-0.7, height0.18..0.45 box.
            var lower=KeeperSeatOutline(p,.7f,.35f,.10f);
            var top=KeeperSeatOutline(p,.675f,.33f,.09f);
            int[] faces={0,6,7};
            foreach(int face in faces)
            {
                int next=(face+1)%8;
                Color tone=face==0?new Color(.76f,.79f,.87f):
                    face==7?new Color(.65f,.68f,.77f):new Color(.48f,.52f,.64f);
                KeeperSeatFace(stone,lower[face]+Vector3.up*.18f,
                    lower[next]+Vector3.up*.18f,lower[next]+Vector3.up*.395f,
                    lower[face]+Vector3.up*.395f,tone,.18f,.395f);
                KeeperSeatFace(stone,lower[face]+Vector3.up*.395f,
                    lower[next]+Vector3.up*.395f,top[next]+Vector3.up*.45f,
                    top[face]+Vector3.up*.45f,tone*1.08f,.395f,.45f);
            }
            Vector3 centre=Project(p.x,p.y)+Vector3.up*.45f;
            for(int i=0;i<8;i++)
            {
                int first=stone.V.Count;
                FullFace(stone,top[i]+Vector3.up*.45f,top[(i+1)%8]+Vector3.up*.45f,
                    centre,centre,new Color(.76f,.79f,.86f));
                for(int j=0;j<4;j++)
                {
                    Vector3 vertex=stone.V[first+j],offset=vertex-centre;
                    // One unbroken stone interior across the entire top fan. Sampling
                    // floor-grid UVs here would make the mounting slab look tiled.
                    stone.U[first+j]=new Vector2(.345f+offset.x*.065f,.70f+offset.y*.085f);
                    FoundrySurface.ToDesign(vertex.x,vertex.y-.45f,out float u,out float v);
                    stone.C[first+j]=SurfaceTone(stone.C[first+j],u,v,.45f,2);
                }
            }
        }

        private static Vector3[] KeeperSeatOutline(Vector2 p,float half,float endClip,float sideClip)
        {
            return new[]{
                Project(p.x-half+endClip,p.y-half),Project(p.x+half-sideClip,p.y-half),
                Project(p.x+half,p.y-half+sideClip),Project(p.x+half,p.y+half-endClip),
                Project(p.x+half-endClip,p.y+half),Project(p.x-half+sideClip,p.y+half),
                Project(p.x-half,p.y+half-sideClip),Project(p.x-half,p.y-half+endClip)};
        }

        private static void KeeperSeatFace(Builder stone,Vector3 a,Vector3 b,Vector3 c,Vector3 d,
            Color tone,float bottom,float top)
        {
            int first=stone.V.Count;
            GateStonePatch(stone,a,b,c,d,tone);
            for(int j=0;j<4;j++)
            {
                float height=j<2?bottom:top;
                Vector3 vertex=stone.V[first+j];
                FoundrySurface.ToDesign(vertex.x,vertex.y-height,out float u,out float v);
                stone.C[first+j]=SurfaceTone(stone.C[first+j],u,v,height,.45f-height);
            }
        }
    }
}
