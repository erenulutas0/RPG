using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // A stone shaft with two real blind rebates. Its outside is the existing
        // tapered octagonal prism; only the two visible wall faces are replaced.
        private static void GateRecessedShaft(Builder stone,Vector4 box,float bottom,
            float top,float inset,float shade,float depth)
        {
            var shell=new Builder();
            LitFoundryTier(shell,box.x,box.y,box.z,box.w,bottom,top,inset);
            // Keep the old face order (side, front), then its closed corner and
            // cap. No solid wall is emitted underneath either recessed opening.
            for(int face=0;face<2;face++)
            {
                int first=face*4;
                var points=new[]{shell.V[first],shell.V[first+1],shell.V[first+2],shell.V[first+3]};
                Color tint=shell.C[first+2]*shade;tint.a=1;
                Vector3 inward=face==0?Project(box.x,box.y+depth)-Project(box.x,box.y):
                    Project(box.x+depth,box.y)-Project(box.x,box.y);
                GateRecessedFace(stone,points,inward,tint);
            }
            for(int first=8;first<shell.V.Count;first+=4)
            {
                int output=stone.V.Count;
                for(int i=0;i<4;i++)
                {
                    stone.V.Add(shell.V[first+i]);stone.U.Add(shell.U[first+i]);
                    Color tint=shell.C[first+i]*shade;tint.a=1;stone.C.Add(tint);
                }
                stone.T.AddRange(new[]{output,output+1,output+2,output,output+2,output+3});
            }
        }

        private static Vector2[] GateRecessOpening()=>new[]{
            new Vector2(.28f,.17f),new Vector2(.72f,.17f),
            new Vector2(.72f,.69f),new Vector2(.5f,.84f),new Vector2(.28f,.69f)};

        private static void GateRecessedFace(Builder stone,Vector3[] face,Vector3 inward,Color tint)
        {
            var opening=GateRecessOpening();
            var frame=new[]{new Vector2(.14f,.08f),new Vector2(.86f,.08f),
                new Vector2(.86f,.75f),new Vector2(.5f,.94f),new Vector2(.14f,.75f)};
            // Five matching outer stations partition the original whole wall.
            // There is no hidden full-face rectangle across the new blind niche.
            var border=new[]{Vector2.zero,Vector2.right,Vector2.one,new Vector2(.5f,1),Vector2.up};
            Vector3 At(Vector2 p)=>GateFaceLerp(face,p.x,p.y);
            Vector3 origin=face[0],along=face[1],across=face[3];
            Vector3 hub=At(new Vector2(.5f,.46f))+inward;
            Color back=tint*.50f;back.a=1;
            // Backing first, then all five connected depth returns, then the
            // front stones. The near faces mask projected hidden return edges.
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                HeaderStonePatch(stone,At(opening[i])+inward,At(opening[j])+inward,hub,hub,
                    back,origin+inward,along+inward,across+inward);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                float light=i==0?1.03f:i==1?.52f:i==2?.58f:i==3?.74f:.86f;
                Color reveal=tint*light;reveal.a=1;
                HeaderStonePatch(stone,At(opening[i]),At(opening[j]),
                    At(opening[j])+inward,At(opening[i])+inward,reveal,origin,along,across);
            }
            // Broad shoulder and sill stones replace the earlier rows of tiny
            // surface ashlar. A single joint separates the bearing from its frame.
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Color body=tint*(i==0?.86f:i==2||i==3?.94f:.91f);body.a=1;
                HeaderDressedFace(stone,At(border[i]),At(border[j]),At(frame[j]),At(frame[i]),body,.035f);
            }
            for(int i=0;i<5;i++)
            {
                int j=(i+1)%5;
                Color dressed=tint*(i==0?1.12f:i==2||i==3?1.07f:1.04f);dressed.a=1;
                HeaderDressedFace(stone,At(frame[i]),At(frame[j]),At(opening[j]),At(opening[i]),dressed,.055f);
            }
        }
    }
}
