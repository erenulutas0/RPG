using System;

namespace Cryptforge.Art
{
    // Tiny visibility graph around the side loop and passage. No per-frame allocations.
    public sealed class FoundryPursuit
    {
        private static readonly float[,] Points={{-23,-23},{-14,-14},{0,0},{-14,0},{-2,0},{-15,6},{-14,12},{-2,12},{-8,15},{8,6},{16,6},{28,6}};
        private static readonly int Count=Points.GetLength(0);
        private static readonly float[] X=new float[Count],Y=new float[Count];
        private static readonly float[,] Distance=new float[Count,Count];
        private readonly float[] _cost=new float[Count];
        private float _heroX=float.PositiveInfinity,_heroY;
        static FoundryPursuit()
        {
            for(int i=0;i<Count;i++)FoundryMap.ToFloor(Points[i,0],Points[i,1],out X[i],out Y[i]);
            for(int i=0;i<Count;i++)for(int j=0;j<Count;j++)Distance[i,j]=FoundryMap.Segment(X[i],Y[i],X[j],Y[j],.65f)?Length(X[i]-X[j],Y[i]-Y[j]):float.PositiveInfinity;
            for(int k=0;k<Count;k++)for(int i=0;i<Count;i++)for(int j=0;j<Count;j++)Distance[i,j]=Math.Min(Distance[i,j],Distance[i,k]+Distance[k,j]);
        }
        public void Target(float x,float y,float heroX,float heroY,ref float targetX,ref float targetY)
        {
            if(FoundryMap.Segment(x,y,targetX,targetY,.6f))return;
            if(Math.Abs(heroX-_heroX)+Math.Abs(heroY-_heroY)>.25f)
            {
                _heroX=heroX;_heroY=heroY;
                for(int i=0;i<Count;i++)_cost[i]=float.PositiveInfinity;
                for(int j=0;j<Count;j++)if(FoundryMap.Segment(X[j],Y[j],heroX,heroY,.6f))
                {
                    float last=Length(X[j]-heroX,Y[j]-heroY);
                    for(int i=0;i<Count;i++)_cost[i]=Math.Min(_cost[i],Distance[i,j]+last);
                }
            }
            float best=float.PositiveInfinity;int chosen=-1;
            for(int i=0;i<Count;i++)
            {
                float distance=Length(x-X[i],y-Y[i]);
                if(distance<.12f||distance+_cost[i]>=best||!FoundryMap.Segment(x,y,X[i],Y[i],.6f))continue;
                best=distance+_cost[i];chosen=i;
            }
            if(chosen>=0){targetX=X[chosen];targetY=Y[chosen];}else{targetX=x;targetY=y;}
        }
        private static float Length(float x,float y)=>(float)Math.Sqrt(x*x+y*y);
    }
}
