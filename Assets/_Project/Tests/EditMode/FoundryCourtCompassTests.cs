using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCourtCompassTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static object NewBuilder()=>Activator.CreateInstance(BuilderType,true);
        private static List<T> Data<T>(object builder,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(builder);
        private static void Invoke(string method,params object[] arguments)=>
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,arguments);

        [Test]
        public void CompassRemainsFlushBoundedAndReceivesOrdinaryFloorLighting()
        {
            object mesh=NewBuilder();Invoke("BuildCourtSealGeometry",mesh);
            CheckFlushMesh(mesh,3.885f,600);
            var uv=Data<Vector2>(mesh,"U");
            Vector4 brass=(Vector4)typeof(FoundryRegionView).GetField("CopingRail",Static).GetValue(null);
            bool metal=false,stone=false;
            foreach(Vector2 point in uv)
            {
                bool inBrass=point.x>=brass.x-.00001f&&point.x<=brass.z+.00001f&&
                    point.y>=brass.y-.00001f&&point.y<=brass.w+.00001f;
                metal|=inBrass;stone|=!inBrass;
            }
            Assert.That(metal&&stone,Is.True,"Aged metal and its stone engraving remain separate material samples.");
            Invoke("LightFloorDecals",mesh);
            foreach(Color color in Data<Color>(mesh,"C"))
            {
                Assert.That(color.a,Is.EqualTo(1));
                Assert.That(color.r,Is.InRange(0f,1.90001f));Assert.That(color.g,Is.InRange(0f,1.90001f));
                Assert.That(color.b,Is.InRange(0f,1.90001f));
            }
        }

        [Test]
        public void FittedInnerFlagsPreserveCompleteMedallionCoverageAndWorldGrain()
        {
            var field=new Vector2[48];
            for(int i=0;i<field.Length;i++)field[i]=new Vector2(Mathf.Cos(i*Mathf.PI/24),Mathf.Sin(i*Mathf.PI/24))*2.73f;
            object mesh=NewBuilder();Invoke("BuildCourtMedallionField",mesh,field);
            CheckFlushMesh(mesh,2.73f,1800);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");var uv=Data<Vector2>(mesh,"U");
            float span=(float)typeof(FoundryRegionView).GetField("PavingGrainSpan",Static|BindingFlags.Public).GetRawConstantValue();
            for(int i=0;i<vertices.Count;i++)
            {
                Vector2 design=ToDesign(vertices[i]);
                Vector2 expected=new Vector2(1+2*FoundryMap.Axis*design.x/span,1+2*FoundryMap.Axis*design.y/span);
                Assert.That(Vector2.Distance(uv[i],expected),Is.LessThan(.00001f),"The fitted flags retain continuous world-space mineral grain.");
            }
            // Sample inside the original inscribed polygon, including the joins
            // between the three flag courses. The common stone bed closes them.
            for(int x=-10;x<=10;x++)for(int y=-10;y<=10;y++)
            {
                Vector2 design=new Vector2(x*.25f,y*.25f);
                if(design.magnitude>2.70f)continue;
                Vector2 point=Project(design);bool covered=false;
                for(int t=0;t<triangles.Count&&!covered;t+=3)
                    covered=InTriangle(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]);
                Assert.That(covered,Is.True,"No fitted flag or joint may cut a hole in the original medallion bed.");
            }
        }

        private static void CheckFlushMesh(object mesh,float radius,int maximumVertices)
        {
            var vertices=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");
            var colors=Data<Color>(mesh,"C");var triangles=Data<int>(mesh,"T");
            Assert.That(vertices.Count,Is.InRange(1,maximumVertices));
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colors.Count,Is.EqualTo(vertices.Count));
            foreach(Vector3 p in vertices)
            {
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
                Assert.That(p.z,Is.Zero,"The medallion belongs to the flush floor decal, not a raised actor occluder.");
                Vector2 design=ToDesign(p);
                Assert.That(design.magnitude,Is.LessThanOrEqualTo(radius+.00002f));
                Assert.That(Vector2.Distance(p,Project(design)),Is.LessThan(.00001f));
                FoundryMap.ToFloor(design.x,design.y,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.True);
            }
            foreach(Vector2 p in uv)
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
            Assert.That(triangles.Count%3,Is.Zero);
            foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
            for(int t=0;t<triangles.Count;t+=3)
                Assert.That(Math.Abs(Cross(vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]])),Is.GreaterThan(1e-10));
        }

        private static Vector2 ToDesign(Vector2 p)=>new Vector2((p.x+2*p.y)*FoundryMap.Axis,(-p.x+2*p.y)*FoundryMap.Axis);
        private static Vector2 Project(Vector2 p)=>(Vector3)typeof(FoundryRegionView).GetMethod("Project",Static).Invoke(null,new object[]{p.x,p.y});
        private static double Cross(Vector2 a,Vector2 b,Vector2 p)=>
            ((double)b.x-a.x)*((double)p.y-a.y)-((double)b.y-a.y)*((double)p.x-a.x);
        private static bool InTriangle(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            double x=Cross(a,b,p),y=Cross(b,c,p),z=Cross(c,a,p);
            return x>=-1e-7&&y>=-1e-7&&z>=-1e-7||x<=1e-7&&y<=1e-7&&z<=1e-7;
        }
    }
}
