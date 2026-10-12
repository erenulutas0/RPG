using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCraneMachineryTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [Test]
        public void CompleteCastingRemainsSeatedInsideTheExistingScenicFooting()
        {
            MeshData body=Build("BuildCraneMachineryCasting");
            Assert.That(body.V.Count,Is.GreaterThan(0).And.LessThanOrEqualTo(1100));
            Assert.That(body.U.Count,Is.EqualTo(body.V.Count));Assert.That(body.C.Count,Is.EqualTo(body.V.Count));
            foreach(int index in body.T)Assert.That(index,Is.InRange(0,body.V.Count-1));
            float shift=FoundryMap.WestAssemblyOffset;
            // Original physical deck, not a box derived from the new equipment.
            var deck=new[]{Project(-27+shift,11),Project(-24+shift,11),Project(-24+shift,15),Project(-27+shift,15)};
            for(int i=0;i<body.V.Count;i++)
            {
                Assert.That(float.IsNaN(body.V[i].x)||float.IsNaN(body.V[i].y)||
                    float.IsInfinity(body.V[i].x)||float.IsInfinity(body.V[i].y),Is.False);
                Assert.That(body.U[i].x,Is.InRange(0f,1f));Assert.That(body.U[i].y,Is.InRange(0f,1f));
                Assert.That(body.C[i].a,Is.EqualTo(1f).Within(.00001f));
                for(int direction=0;direction<72;direction++)
                {
                    float angle=direction*Mathf.PI/36;
                    var axis=new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0);
                    float limit=float.NegativeInfinity;
                    foreach(Vector3 corner in deck)limit=Mathf.Max(limit,Vector3.Dot(axis,corner));
                    limit+=axis.y*(axis.y>0?2.60f:1.12f);
                    Assert.That(Vector3.Dot(axis,body.V[i]),Is.LessThanOrEqualTo(limit+.0001f),
                        "The gearbox must be borne on the original deck without claiming court circulation.");
                }
            }
            // The first sole wall feet are the actual emitted contact points.
            // Invert after subtracting deck height; no imagined elevated footprint.
            foreach(int index in new[]{0,1,4,5})
            {
                Vector3 foot=body.V[index]-Vector3.up*1.12f;
                FoundryMap.ToDesign(foot.x,foot.y*2,out float u,out float v);
                Assert.That(u,Is.InRange(-27+shift,-24+shift));Assert.That(v,Is.InRange(11,15));
                FoundryMap.ToFloor(u,v,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,"A solid equipment foot cannot occupy walkable ground.");
            }
        }

        [Test]
        public void BroadCastSurfacesKeepTwoDimensionalGrainInsideTheIronAtlas()
        {
            MeshData body=Build("BuildCraneMachineryCasting");
            foreach(Vector2 uv in body.U)
            {
                Assert.That(uv.x,Is.InRange(.17f,.83f));Assert.That(uv.y,Is.InRange(.17f,.83f));
            }
            for(int t=0;t<body.T.Count;t+=3)
            {
                int a=body.T[t],b=body.T[t+1],c=body.T[t+2];
                float area=Mathf.Abs(Cross((Vector2)(body.V[b]-body.V[a]),(Vector2)(body.V[c]-body.V[a])));
                if(area<.00001f)continue;
                float grain=Mathf.Abs(Cross(body.U[b]-body.U[a],body.U[c]-body.U[a]));
                Assert.That(grain/area,Is.InRange(.005f,.020f),
                    "A broad visible face must not collapse onto a texture row or stretch an atlas edge into corrugations.");
            }
        }

        [Test]
        public void ActualCheeksAndHeaderLeaveTheWorkingDrumVisiblyExposed()
        {
            MeshData drum=Build("BuildCraneMachineryDrum"),casting=Build("BuildCraneMachineryCasting");
            Vector2 min=new Vector2(float.PositiveInfinity,float.PositiveInfinity),max=-min;
            foreach(Vector3 vertex in drum.V){min=Vector2.Min(min,vertex);max=Vector2.Max(max,vertex);}
            int covered=0,visible=0;
            for(int x=1;x<30;x++)for(int y=1;y<20;y++)
            {
                var p=new Vector2(Mathf.Lerp(min.x,max.x,x/30f),Mathf.Lerp(min.y,max.y,y/20f));
                int expected=LastCoveringTriangle(drum,p);if(expected<0)continue;
                covered++;int actual=LastCoveringTriangle(casting,p);if(actual<0)continue;
                bool same=true;
                for(int k=0;k<3;k++)same&=Vector3.Distance(drum.V[drum.T[expected+k]],casting.V[casting.T[actual+k]])<.00001f;
                if(same)visible++;
            }
            Assert.That(covered,Is.GreaterThan(100));
            Assert.That(visible,Is.GreaterThan(covered*.35f),
                "The emitted housing must retain a substantial exposed drum, not overpaint it with a solid cast box.");
        }

        private static MeshData Build(string method)
        {
            Type type=typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(type,true);
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,new[]{builder});
            return new MeshData((List<Vector3>)type.GetField("V",Fields).GetValue(builder),
                (List<Vector2>)type.GetField("U",Fields).GetValue(builder),
                (List<Color>)type.GetField("C",Fields).GetValue(builder),
                (List<int>)type.GetField("T",Fields).GetValue(builder));
        }
        private static Vector3 Project(float u,float v)=>(Vector3)typeof(FoundryRegionView).GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static int LastCoveringTriangle(MeshData mesh,Vector2 p)
        {
            int last=-1;
            for(int t=0;t<mesh.T.Count;t+=3)
            {
                Vector2 a=mesh.V[mesh.T[t]],b=mesh.V[mesh.T[t+1]],c=mesh.V[mesh.T[t+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-8f)continue;
                float x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
                if(x>=-.0001f&&y>=-.0001f&&z>=-.0001f)last=t;
            }
            return last;
        }
        private static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        private sealed class MeshData
        {
            internal readonly List<Vector3> V;internal readonly List<Vector2> U;
            internal readonly List<Color> C;internal readonly List<int> T;
            internal MeshData(List<Vector3> v,List<Vector2> u,List<Color> c,List<int> t){V=v;U=u;C=c;T=t;}
        }
    }
}
