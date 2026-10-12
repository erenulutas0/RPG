using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCourtDressedStoneTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static List<T> Data<T>(object mesh,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(mesh);
        private static object Call(string method,params object[] args)=>typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,args);

        [TestCase(false)]
        [TestCase(true)]
        public void DressedRingAndFittedEndsPartitionTheirOriginalFaceWithoutChangingWorldGrain(bool curvedEnd)
        {
            Vector2[] polygon;
            if(curvedEnd)
            {
                var field=new Vector2[48];
                for(int k=0;k<48;k++)field[k]=Polar(k*Mathf.PI/24,2.73f);
                polygon=((List<Vector2>)Call("ClipFlagCell",field,-2.708f,-2.708f,-.832f,-.932f)).ToArray();
            }
            else polygon=new[]{Polar(.004f,2.735f),Polar(Mathf.PI/12-.004f,2.735f),
                Polar(Mathf.PI/12-.004f,3.945f),Polar(.004f,3.945f)};
            // Production radial stones arrive clockwise while clipped field ends
            // arrive counter-clockwise; both must retain precisely the same area.
            object mesh=Activator.CreateInstance(BuilderType,true);
            Call("CourtDressedStone",mesh,polygon);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            var uv=Data<Vector2>(mesh,"U");var colors=Data<Color>(mesh,"C");
            Assert.That(vertices.Count,Is.InRange(1,500));
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colors.Count,Is.EqualTo(vertices.Count));
            double original=0,emitted=0;
            for(int k=0;k<polygon.Length;k++)original+=Cross(Project(polygon[k]),Project(polygon[(k+1)%polygon.Length]));
            for(int k=0;k<triangles.Count;k+=3)
            {
                Vector2 a=vertices[triangles[k]],b=vertices[triangles[k+1]],c=vertices[triangles[k+2]];
                double area=Math.Abs(Cross(b-a,c-a));
                Assert.That(area,Is.GreaterThan(1e-10),"Dressing must not emit a collapsed bevel triangle.");
                emitted+=area;
            }
            Assert.That(emitted,Is.EqualTo(Math.Abs(original)).Within(.00003),
                "A bevel must partition the existing face rather than extend it or cover it twice.");
            float orientation=SignedArea(polygon)>0?1:-1;
            for(int k=0;k<vertices.Count;k++)
            {
                Vector3 p=vertices[k];
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
                Assert.That(p.z,Is.Zero);
                Vector2 design=new Vector2((p.x+2*p.y)*FoundryMap.Axis,(-p.x+2*p.y)*FoundryMap.Axis);
                for(int edge=0;edge<polygon.Length;edge++)
                    Assert.That(orientation*Cross(polygon[(edge+1)%polygon.Length]-polygon[edge],design-polygon[edge]),
                        Is.GreaterThanOrEqualTo(-.00001),"The dressed shoulder stays inside its fitted slab.");
                Vector2 expected=new Vector2(1+2*FoundryMap.Axis*design.x/FoundryRegionView.PavingGrainSpan,
                    1+2*FoundryMap.Axis*design.y/FoundryRegionView.PavingGrainSpan);
                Assert.That(Vector2.Distance(expected,uv[k]),Is.LessThan(.00001f));
                Assert.That(colors[k].a,Is.EqualTo(1));
                Assert.That(colors[k].r,Is.InRange(0f,1.9f));Assert.That(colors[k].g,Is.InRange(0f,1.9f));
                Assert.That(colors[k].b,Is.InRange(0f,1.9f));
            }
        }

        private static Vector2 Polar(float a,float r)=>new Vector2(Mathf.Cos(a),Mathf.Sin(a))*r;
        private static Vector2 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static float SignedArea(Vector2[] p)
        {
            double sum=0;for(int i=0;i<p.Length;i++)sum+=Cross(p[i],p[(i+1)%p.Length]);return (float)sum;
        }
    }
}
