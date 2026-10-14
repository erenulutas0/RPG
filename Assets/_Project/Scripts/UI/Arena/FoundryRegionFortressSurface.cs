using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        private static void FortressRecessedRespond(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tone)
        {
            // One blind panel carved into the full parent face. Its four broad
            // jamb/bearing pieces and four shaded returns partition that face;
            // they add no floating trim or vertices outside the original taper.
            Vector3 At(float x,float y)=>UndercroftFacePoint(a,b,c,d,x,y);
            var frame=new[]{a,b,c,d};
            var mouth=new[]{At(.18f,.10f),At(.82f,.10f),At(.82f,.90f),At(.18f,.90f)};
            var back=new[]{At(.27f,.15f),At(.73f,.15f),At(.73f,.85f),At(.27f,.85f)};
            for(int edge=0;edge<4;edge++)
            {
                int next=(edge+1)%4;
                FortressMineralFace(mesh,frame[edge],frame[next],mouth[next],mouth[edge],tone,false);
                float light=edge==0?.98f:edge==1?.74f:edge==2?.51f:.61f;
                FortressMineralFace(mesh,mouth[edge],mouth[next],back[next],back[edge],
                    new Color(tone.r*light,tone.g*light,tone.b*light,1),false);
            }
            FortressCoursedFace(mesh,back[0],back[1],back[2],back[3],
                new Color(tone.r*.71f,tone.g*.74f,tone.b*.80f,1),2,1);
        }

        // A local family for the bridge and lower fortress. Workshop supports,
        // court edge walls and gate ashlar retain their existing material helpers.
        private static void FortressCoursedFace(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,
            Color tone,int rows,int columns)
        {
            FortressMineralFace(mesh,a,b,c,d,tone*.87f,false);
            float width=(Vector3.Distance(a,b)+Vector3.Distance(d,c))*.5f;
            float height=(Vector3.Distance(a,d)+Vector3.Distance(b,c))*.5f;
            float jointX=.008f/Mathf.Max(.01f,width),jointY=.009f/Mathf.Max(.01f,height);
            for(int row=0;row<rows;row++)
            {
                float lo=row/(float)rows,hi=(row+1)/(float)rows;
                float y0=row==0?0:lo+jointY,y1=row==rows-1?1:hi-jointY;
                float x=row%2==0||columns==1?0:-.5f/columns;
                int index=0;
                while(x<1)
                {
                    float end=Mathf.Min(1,x+1f/columns),start=Mathf.Max(0,x);
                    float x0=start==0?0:start+jointX,x1=end==1?1:end-jointX;
                    if(x1>x0)
                    {
                        float mineral=1+(((row+index/2)%5)-2)*.014f;
                        FortressMineralFace(mesh,UndercroftFacePoint(a,b,c,d,x0,y0),
                            UndercroftFacePoint(a,b,c,d,x1,y0),UndercroftFacePoint(a,b,c,d,x1,y1),
                            UndercroftFacePoint(a,b,c,d,x0,y1),tone*mineral,true);
                    }
                    x+=1f/columns;index++;
                }
            }
        }

        private static void FortressMineralFace(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,
            Color tone,bool dressed)
        {
            int first=mesh.V.Count;tone.a=1;
            mesh.Quad(a,b,c,d,false,tone);
            if(dressed)GateStoneEdges(mesh,first,.022f,.024f);
            // One scale/orientation covers the complete stone and its dressed rim.
            // Narrow or tapered faces no longer stretch the same rectangular stamp.
            Vector3 axis=(b-a).sqrMagnitude>(d-a).sqrMagnitude?b-a:d-a;
            FortressMapGrain(mesh,first,mesh.V.Count,axis);
        }

        private static void FortressDressedArch(Builder mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d,Color tone)
        {
            int first=mesh.V.Count;
            // Carve a broad intrados return into each existing wedge. The return,
            // sloped shoulder and dressed outer face partition its old contour;
            // the opening itself and outer bearing stations never move.
            Vector3 ra=Vector3.Lerp(a,d,.18f),rb=Vector3.Lerp(b,c,.18f);
            Vector3 sa=Vector3.Lerp(a,d,.26f),sb=Vector3.Lerp(b,c,.26f);
            GateStonePatch(mesh,a,b,rb,ra,new Color(tone.r*.58f,tone.g*.63f,tone.b*.71f,1));
            GateStonePatch(mesh,ra,rb,sb,sa,new Color(tone.r*.86f,tone.g*.90f,tone.b*.96f,1));
            GateDressedFace(mesh,sa,sb,c,d,tone,.05f);
            FortressMapGrain(mesh,first,mesh.V.Count,(b-a).sqrMagnitude>(d-a).sqrMagnitude?b-a:d-a);
        }

        private static void BridgeBearingGrain(Builder mesh,Vector3 origin)
        {
            // One physical field crosses the arch face, its recessed soffit and
            // the deck bearing. Small construction pieces must not restart or
            // rotate the mineral image at every joint. Both batches use the same
            // datum, including contacts along the opening.
            StoneDetailGrain(mesh,0,mesh.V.Count,origin,Vector2.right,
                new Vector4(.04f,.44f,.39f,.564f),.075f);
        }

        private static void FortressDetailGrain(Builder mesh,int first,int end,Vector3 direction)
        {
            Vector2 axis=((Vector2)direction).normalized;
            if(axis.sqrMagnitude<.5f)axis=Vector2.right;
            StoneDetailGrain(mesh,first,end,Vector2.zero,axis,
                new Vector4(.04f,.44f,.39f,.564f),.075f);
        }

        private static void FortressMapGrain(Builder mesh,int first,int end,Vector3 direction)
        {
            Vector2 axis=((Vector2)direction).normalized;
            if(axis.sqrMagnitude<.5f)axis=Vector2.right;
            Vector2 across=new Vector2(-axis.y,axis.x);
            Vector2 low=new Vector2(float.MaxValue,float.MaxValue),high=new Vector2(float.MinValue,float.MinValue);
            for(int i=first;i<end;i++)
            {
                Vector2 p=mesh.V[i];var local=new Vector2(Vector2.Dot(p,axis),Vector2.Dot(p,across));
                low=Vector2.Min(low,local);high=Vector2.Max(high,local);
            }
            Vector2 span=high-low;
            float scale=Mathf.Min(.082f,Mathf.Min(.35f/Mathf.Max(.001f,span.x),.124f/Mathf.Max(.001f,span.y)));
            // Interior of the broad cool V4 stone, clear of its black mortar.
            // A single uniform scale retains mineral aspect ratio on skewed faces.
            for(int i=first;i<end;i++)
            {
                Vector2 p=mesh.V[i];var local=new Vector2(Vector2.Dot(p,axis),Vector2.Dot(p,across));
                mesh.U[i]=new Vector2(.04f,.44f)+(local-low)*scale;
            }
        }

        private static Vector2 BridgeFortressWallGrain(Vector2 p,float height,float blockStart,bool along)
        {
            float horizontal=along?(p.x-blockStart)*Cryptforge.Art.FoundryMap.Axis*.5f:
                (Mathf.Abs(p.y)-Cryptforge.Art.FoundryMap.BridgeHalfWidth)*Cryptforge.Art.FoundryMap.Axis;
            return new Vector2(.04f+horizontal*.082f,.44f+(height+.90f)*.073f);
        }

        private static Vector2 BridgeFortressCapGrain(Vector2 p,float blockStart)=>
            new Vector2(.04f+(p.x-blockStart)*Cryptforge.Art.FoundryMap.Axis*.5f*.082f,
                .44f+(Mathf.Abs(p.y)-Cryptforge.Art.FoundryMap.BridgeHalfWidth)*Cryptforge.Art.FoundryMap.Axis*.082f);
    }
}
