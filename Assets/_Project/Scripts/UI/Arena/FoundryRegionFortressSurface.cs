using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
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
            GateDressedFace(mesh,a,b,c,d,tone,.07f);
            FortressMapGrain(mesh,first,mesh.V.Count,(b-a).sqrMagnitude>(d-a).sqrMagnitude?b-a:d-a);
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
