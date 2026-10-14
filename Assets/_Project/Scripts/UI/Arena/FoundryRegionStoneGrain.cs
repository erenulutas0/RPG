using System.Collections.Generic;
using UnityEngine;

namespace Cryptforge.UI
{
    public sealed partial class FoundryRegionView
    {
        // Keep quarry detail at a physical scale instead of stretching a small
        // atlas interior over a complete building. Mirror within that mortar-free
        // interior; split triangles at the folds so interpolation never shortcuts
        // across a repeat. This is construction-time work, before depth ownership
        // and batching. The original positions, lighting and draw order survive.
        private static void StoneDetailGrain(Builder mesh,int first,int end,Vector2 origin,
            Vector2 axis,Vector4 patch,float density)
        {
            if(mesh.Flames.Count!=0||mesh.Cloth.Count!=0)
                throw new System.InvalidOperationException("Stone grain subdivision requires static masonry.");
            Vector2 across=new Vector2(-axis.y,axis.x);
            Vector2 size=new Vector2(patch.z-patch.x,patch.w-patch.y);
            Vector2 Field(int i)
            {
                Vector2 p=(Vector2)mesh.V[i]-origin;
                return new Vector2(Vector2.Dot(p,axis)*density/size.x,Vector2.Dot(p,across)*density/size.y);
            }
            Vector2 Grain(int i)
            {
                Vector2 f=Field(i);
                return new Vector2(patch.x+StoneMirror(f.x)*size.x,patch.y+StoneMirror(f.y)*size.y);
            }
            for(int i=first;i<end;i++)mesh.U[i]=Grain(i);
            var triangles=new List<int>(mesh.T.Count);
            var cuts=new Dictionary<(int,int,int,int),int>();
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                int a=mesh.T[t],b=mesh.T[t+1],c=mesh.T[t+2];
                if(a<first||a>=end||b<first||b>=end||c<first||c>=end)
                {triangles.Add(a);triangles.Add(b);triangles.Add(c);continue;}
                var parts=new List<List<int>>{new List<int>{a,b,c}};
                for(int dimension=0;dimension<2;dimension++)
                {
                    float lo=Mathf.Min(Field(a)[dimension],Mathf.Min(Field(b)[dimension],Field(c)[dimension]));
                    float hi=Mathf.Max(Field(a)[dimension],Mathf.Max(Field(b)[dimension],Field(c)[dimension]));
                    for(int cut=Mathf.FloorToInt(lo)+1;cut<hi-.000001f;cut++)
                    {
                        var divided=new List<List<int>>();
                        foreach(var part in parts)
                        {
                            bool below=false,above=false;
                            foreach(int i in part)
                            {float d=Field(i)[dimension]-cut;below|=d<0;above|=d>0;}
                            if(!below||!above){divided.Add(part);continue;}
                            var lower=new List<int>();var upper=new List<int>();
                            for(int j=0;j<part.Count;j++)
                            {
                                int i=part[j],next=part[(j+1)%part.Count];
                                float d=Field(i)[dimension]-cut,n=Field(next)[dimension]-cut;
                                if(d<=0)lower.Add(i);
                                if(d>=0)upper.Add(i);
                                if(d<0&&n>0||d>0&&n<0)
                                {
                                    var key=(Mathf.Min(i,next),Mathf.Max(i,next),dimension,cut);
                                    if(!cuts.TryGetValue(key,out int added))
                                    {
                                        // Neighbouring triangles can traverse the
                                        // same edge backwards. A canonical endpoint
                                        // order also keeps separately authored but
                                        // coincident edges bit-identical at a cut.
                                        Vector3 p=mesh.V[i],q=mesh.V[next];
                                        bool forward=p.x<q.x||p.x==q.x&&(p.y<q.y||p.y==q.y&&p.z<=q.z);
                                        int start=forward?i:next,finish=forward?next:i;
                                        float startValue=Field(start)[dimension],finishValue=Field(finish)[dimension];
                                        float blend=(cut-startValue)/(finishValue-startValue);
                                        added=mesh.V.Count;
                                        mesh.V.Add(Vector3.LerpUnclamped(mesh.V[start],mesh.V[finish],blend));
                                        mesh.C.Add(Color.LerpUnclamped(mesh.C[start],mesh.C[finish],blend));
                                        mesh.U.Add(Grain(added));cuts.Add(key,added);
                                    }
                                    lower.Add(added);upper.Add(added);
                                }
                            }
                            if(lower.Count>=3)divided.Add(lower);
                            if(upper.Count>=3)divided.Add(upper);
                        }
                        parts=divided;
                    }
                }
                foreach(var part in parts)for(int j=1;j<part.Count-1;j++)
                {
                    Vector3 p=mesh.V[part[0]],q=mesh.V[part[j]],s=mesh.V[part[j+1]];
                    double area=((double)q.x-p.x)*(s.y-p.y)-((double)q.y-p.y)*(s.x-p.x);
                    if(System.Math.Abs(area)<1e-12)continue;
                    triangles.Add(part[0]);triangles.Add(part[j]);triangles.Add(part[j+1]);
                }
            }
            mesh.T.Clear();mesh.T.AddRange(triangles);
        }

        private static float StoneMirror(float value)
        {
            float pair=value-Mathf.Floor(value*.5f)*2;
            return pair<=1?pair:2-pair;
        }
    }
}
