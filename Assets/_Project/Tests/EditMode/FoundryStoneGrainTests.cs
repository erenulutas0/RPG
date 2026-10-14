using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryStoneGrainTests
    {
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static List<T> Data<T>(object mesh,string name)=>(List<T>)BuilderType.GetField(name,Members).GetValue(mesh);
        private static readonly Vector4 Patch=new Vector4(.04f,.44f,.39f,.564f);

        [TestCase(0f,0f)]
        [TestCase(-27.3f,0f)]
        [TestCase(12.7f,.37f)]
        [TestCase(-19.2f,-.61f)]
        public void FoldedStoneFieldRetainsCoverageLightingOrderAndUnrelatedGeometry(float offset,float angle)
        {
            object mesh=Activator.CreateInstance(BuilderType,true);
            Quad(mesh,new Vector3(-20,-20),new Vector3(-19,-20),new Vector3(-19,-19),new Vector3(-20,-19));
            var face=new[]{new Vector3(offset-3,-3.6f),new Vector3(offset+3,-3.2f),
                new Vector3(offset+2.4f,2.4f),new Vector3(offset-2.6f,2.8f)};
            foreach(Vector3 p in face)Assert.That(p.z,Is.Zero);
            Quad(mesh,face[0],face[1],face[2],face[3]);
            Quad(mesh,new Vector3(20,20),new Vector3(21,20),new Vector3(21,21),new Vector3(20,21));
            var points=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");var colors=Data<Color>(mesh,"C");
            for(int i=4;i<8;i++)colors[i]=Light(points[i]);
            var oldPoints=points.ToArray();var oldUV=uv.ToArray();var oldColors=colors.ToArray();
            var oldIndices=Data<int>(mesh,"T").ToArray();
            Vector2 axis=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
            Apply(mesh,4,8,axis);
            Assert.That(points.Count,Is.GreaterThan(12).And.LessThan(150),"Only fold intersections add geometry.");
            Assert.That(points.Count,Is.EqualTo(uv.Count).And.EqualTo(colors.Count));
            for(int i=0;i<12;i++)
            {
                Assert.That(points[i],Is.EqualTo(oldPoints[i]),"Never move original silhouette/contact vertices.");
                Assert.That(colors[i],Is.EqualTo(oldColors[i]));
                if(i<4||i>=8)Assert.That(uv[i],Is.EqualTo(oldUV[i]),"Unrelated parent geometry keeps its mapping.");
            }
            var triangles=Data<int>(mesh,"T");
            for(int i=0;i<6;i++)
            {
                Assert.That(triangles[i],Is.EqualTo(oldIndices[i]));
                Assert.That(triangles[triangles.Count-6+i],Is.EqualTo(oldIndices[oldIndices.Length-6+i]),
                    "Keep painter order of later unrelated geometry.");
            }
            double area=0;
            for(int t=6;t<triangles.Count-6;t+=3)
            {
                int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                double signed=Cross(points[a],points[b],points[c]);Assert.That(signed,Is.GreaterThan(1e-10));area+=signed;
                foreach(var weights in new[]{new Vector3(.2f,.3f,.5f),new Vector3(.63f,.24f,.13f)})
                {
                    Vector3 sample=points[a]*weights.x+points[b]*weights.y+points[c]*weights.z;
                    Vector2 actual=uv[a]*weights.x+uv[b]*weights.y+uv[c]*weights.z;
                    Assert.That(Vector2.Distance(actual,Expected(sample,axis)),Is.LessThan(.000002f),
                        "Fixed texel density must hold inside triangles, including triangles adjacent to a fold.");
                    Color color=colors[a]*weights.x+colors[b]*weights.y+colors[c]*weights.z;
                    Assert.That(Vector4.Distance(color,Light(sample)),Is.LessThan(.000002f));
                }
            }
            double original=Cross(face[0],face[1],face[2])+Cross(face[0],face[2],face[3]);
            Assert.That(area,Is.EqualTo(original).Within(.00008),"Folds partition, never overlay or shrink the original surface.");
            for(int x=0;x<11;x++)for(int y=0;y<9;y++)
            {
                Vector3 p=Vector3.Lerp(Vector3.Lerp(face[0],face[1],(x+.137f)/11),
                    Vector3.Lerp(face[3],face[2],(x+.137f)/11),(y+.319f)/9);
                int owners=0;
                for(int t=6;t<triangles.Count-6;t+=3)
                    if(Inside(p,points[triangles[t]],points[triangles[t+1]],points[triangles[t+2]]))owners++;
                Assert.That(owners,Is.EqualTo(1),"The transformed mesh keeps exactly one owner at interior witnesses.");
            }
            for(int i=12;i<points.Count;i++)
            {
                Assert.That(points[i].z,Is.Zero);
                Assert.That(Vector4.Distance(colors[i],Light(points[i])),Is.LessThan(.000002f));
                Assert.That(uv[i].x,Is.InRange(Patch.x-.000001f,Patch.z+.000001f));
                Assert.That(uv[i].y,Is.InRange(Patch.y-.000001f,Patch.w+.000001f));
            }
        }

        [TestCase(0f)]
        [TestCase(-.0000005f)]
        public void ReversedSharedEdgesReuseTheSameFoldPointEvenNearAnExistingFold(float nearFold)
        {
            object mesh=Activator.CreateInstance(BuilderType,true);
            float width=Patch.z-Patch.x,height=Patch.w-Patch.y;
            var a=new Vector3(-1.2f,-1.1f);var c=new Vector3(width/.075f+nearFold,3.4f);
            Quad(mesh,a,new Vector3(c.x,a.y),c,new Vector3(a.x,c.y));
            Apply(mesh,0,4,Vector2.right);
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            // Both triangles traverse a-c in opposite directions. Every interior
            // fold hit must be one shared vertex, not two almost-equal edges.
            var expected=new List<Vector3>();
            foreach(float x in new[]{0f})expected.Add(Vector3.Lerp(a,c,(x-a.x)/(c.x-a.x)));
            foreach(float y in new[]{0f,height/.075f,2*height/.075f})
                expected.Add(Vector3.Lerp(a,c,(y-a.y)/(c.y-a.y)));
            foreach(Vector3 point in expected)
                Assert.That(vertices.FindAll(p=>Vector3.Distance(p,point)<.000003f).Count,Is.EqualTo(1),
                    "Adjacent triangles must reuse the same intersection index.");
            double total=0;
            for(int i=0;i<triangles.Count;i+=3)
            {
                Vector3 p=vertices[triangles[i]],q=vertices[triangles[i+1]],s=vertices[triangles[i+2]];
                double area=Cross(p,q,s);Assert.That(area,Is.GreaterThan(0));total+=area;
            }
            Assert.That(total,Is.EqualTo(2*(double)(c.x-a.x)*(c.y-a.y)).Within(.00002));
        }

        [TestCase("Flames")]
        [TestCase("Cloth")]
        public void AnimatedMetadataCannotBeSilentlyDetachedByStoneSubdivision(string field)
        {
            object mesh=Activator.CreateInstance(BuilderType,true);
            Quad(mesh,Vector3.zero,Vector3.right,Vector3.one,Vector3.up);
            var metadata=(IList)BuilderType.GetField(field,Members).GetValue(mesh);
            metadata.Add(Activator.CreateInstance(metadata.GetType().GetGenericArguments()[0]));
            var exception=Assert.Throws<TargetInvocationException>(()=>Apply(mesh,0,4,Vector2.right));
            Assert.That(exception.InnerException,Is.TypeOf<InvalidOperationException>());
            Assert.That(Data<Vector3>(mesh,"V").Count,Is.EqualTo(4));
            Assert.That(Data<int>(mesh,"T").Count,Is.EqualTo(6));
        }

        private static Color Light(Vector3 p)=>new Color(.3f+p.x*.003f,.4f+p.y*.004f,.5f+p.x*.002f+p.y*.003f,1);
        private static Vector2 Expected(Vector3 p,Vector2 axis)
        {
            Vector2 across=new Vector2(-axis.y,axis.x);
            float Fold(double value){double f=value-Math.Floor(value/2)*2;return (float)(f<=1?f:2-f);}
            return new Vector2(Patch.x+Fold(Vector2.Dot(p,axis)*.075/(Patch.z-Patch.x))*(Patch.z-Patch.x),
                Patch.y+Fold(Vector2.Dot(p,across)*.075/(Patch.w-Patch.y))*(Patch.w-Patch.y));
        }
        private static double Cross(Vector3 a,Vector3 b,Vector3 c)=>((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x);
        private static bool Inside(Vector3 p,Vector3 a,Vector3 b,Vector3 c)=>Cross(a,b,p)>1e-9&&Cross(b,c,p)>1e-9&&Cross(c,a,p)>1e-9;
        private static void Apply(object mesh,int first,int end,Vector2 axis)=>typeof(FoundryRegionView)
            .GetMethod("StoneDetailGrain",BindingFlags.Static|BindingFlags.NonPublic)
            .Invoke(null,new object[]{mesh,first,end,Vector2.zero,axis,Patch,.075f});
        private static void Quad(object mesh,Vector3 a,Vector3 b,Vector3 c,Vector3 d)=>BuilderType
            .GetMethod("Quad",Members).Invoke(mesh,new object[]{a,b,c,d,false,Color.white});
    }
}
