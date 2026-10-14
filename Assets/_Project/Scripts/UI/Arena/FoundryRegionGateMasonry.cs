using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private Material _gateMasonryMaterial;

        private bool BuildBakedGateButtress(Vector4 foot,float order)
        {
            if(_gateMasonryMaterial==null)
            {
                var texture=Resources.Load<Texture2D>("FoundryGateMasonry");
                if(texture==null)return false;
                _gateMasonryMaterial=new Material(Shader.Find("Sprites/Default"))
                    {name="Authored gate buttress masonry",mainTexture=texture,
                        color=new Color(.62f,.62f,.62f,1)};
            }
            // Both towers have the same footprint and orientation. Share the bake
            // without mirroring its directional light. The caller retains the
            // original mass-depth owner and foundations. A separate material at
            // the curtain's exact rank sorts by the complete batch bounds and
            // lets the curtain cover the niche. The reserved half-rank paints
            // this replacement after its curtain; retain its original depth owner.
            Create("Authored gate buttress",GateMasonryCard(foot),_gateMasonryMaterial,order+.5f);
            return true;
        }

        private static Builder GateMasonryCard(Vector4 foot)
        {
            // BuildBlenderGateMasonry.py: a 5 x 10 orthographic field, centred at
            // height 3.75 above the foot. Project applies the raised landing once.
            Vector3 origin=Project((foot.x+foot.z)*.5f,(foot.y+foot.w)*.5f);
            var card=new Builder();
            card.V.AddRange(new[]{origin+new Vector3(-2.5f,-1.25f),origin+new Vector3(2.5f,-1.25f),
                origin+new Vector3(2.5f,8.75f),origin+new Vector3(-2.5f,8.75f)});
            card.U.AddRange(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up});
            for(int i=0;i<4;i++)card.C.Add(Color.white);
            card.T.AddRange(new[]{0,1,2,0,2,3});
            return card;
        }
    }
}
