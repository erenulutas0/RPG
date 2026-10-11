using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryMasonrySurfaceTests
    {
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(false)]
        [TestCase(true)]
        public void DressedRimPartitionsSkewedOrTaperedFaceWithoutChangingItsEnvelope(bool tapered)
        {
            var face=new[]{new Vector3(-1,.2f,0),new Vector3(2,.8f,0),
                new Vector3(tapered?1.6f:2.4f,2.4f,0),new Vector3(tapered?-.65f:-.6f,1.8f,0)};
            var grain=new[]{new Vector2(.27f,.65f),new Vector2(.42f,.65f),
                new Vector2(.42f,.75f),new Vector2(.27f,.75f)};
            var light=new[]{new Color(.31f,.34f,.40f,1),new Color(.41f,.43f,.48f,1),
                new Color(.46f,.48f,.52f,1),new Color(.36f,.39f,.44f,1)};
            Type type=typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
            object builder=Activator.CreateInstance(type,true);
            type.GetMethod("Quad",Members).Invoke(builder,new object[]{face[0],face[1],face[2],face[3],false,Color.white});
            var vertices=(List<Vector3>)type.GetField("V",Members).GetValue(builder);
            var uv=(List<Vector2>)type.GetField("U",Members).GetValue(builder);
            var colors=(List<Color>)type.GetField("C",Members).GetValue(builder);
            var triangles=(List<int>)type.GetField("T",Members).GetValue(builder);
            for(int i=0;i<4;i++){uv[i]=grain[i];colors[i]=light[i];}
            typeof(FoundryRegionView).GetMethod("GateStoneEdges",BindingFlags.Static|BindingFlags.NonPublic)
                .Invoke(null,new object[]{builder,0,.035f,.038f});

            Assert.That(uv.Count,Is.EqualTo(vertices.Count));
            Assert.That(colors.Count,Is.EqualTo(vertices.Count));
            double area=0;
            for(int i=0;i<vertices.Count;i++)
            {
                Vector3 point=vertices[i];
                Assert.That(float.IsNaN(point.x)||float.IsInfinity(point.x)||float.IsNaN(point.y)||float.IsInfinity(point.y),Is.False);
                Assert.That(point.z,Is.EqualTo(0));
                Assert.That(InFace(point,face),Is.True,"Every rim vertex stays inside the exact original silhouette.");
                Vector2 parameter=FaceParameter(point,face);
                Vector2 expected=Vector2.Lerp(Vector2.Lerp(grain[0],grain[1],parameter.x),
                    Vector2.Lerp(grain[3],grain[2],parameter.x),parameter.y);
                Assert.That(Vector2.Distance(uv[i],expected),Is.LessThan(.00001f),"The rim continues the face grain without restarting it.");
                Assert.That(colors[i].a,Is.EqualTo(1));
                Assert.That(float.IsNaN(colors[i].r)||float.IsInfinity(colors[i].r),Is.False);
                Assert.That(float.IsNaN(colors[i].g)||float.IsInfinity(colors[i].g),Is.False);
                Assert.That(float.IsNaN(colors[i].b)||float.IsInfinity(colors[i].b),Is.False);
                Color expectedLight=Color.Lerp(Color.Lerp(light[0],light[1],parameter.x),
                    Color.Lerp(light[3],light[2],parameter.x),parameter.y);
                if(i>=4)
                {
                    // The four directional rim catches modulate the same underlying
                    // field; corner changes must not restart weathering or grain.
                    int edge=(i-4)/4;
                    expectedLight*=edge==2?1.14f:edge==0?.80f:edge==1?.91f:1.035f;
                }
                Assert.That(Mathf.Abs(colors[i].r-expectedLight.r)+Mathf.Abs(colors[i].g-expectedLight.g)+
                    Mathf.Abs(colors[i].b-expectedLight.b),Is.LessThan(.00001f),"Interior and rim continue the original shared light field.");
            }
            for(int t=0;t<triangles.Count;t+=3)
            {
                for(int j=0;j<3;j++)Assert.That(triangles[t+j],Is.InRange(0,vertices.Count-1));
                Vector3 a=vertices[triangles[t]],b=vertices[triangles[t+1]],c=vertices[triangles[t+2]];
                double signed=Cross(a,b,c);
                Assert.That(signed,Is.GreaterThan(.00000001),"Every patch has consistent nondegenerate winding.");
                area+=signed*.5;
                Assert.That(InFace((a+b+c)/3,face),Is.True,"Triangle interiors cannot escape the old face.");
            }
            double original=(Cross(face[0],face[1],face[2])+Cross(face[0],face[2],face[3]))*.5;
            Assert.That(area,Is.EqualTo(original).Within(.000002),"The patches partition the original face without gaps or duplicate coverage.");
            foreach(Vector3 corner in face)Assert.That(vertices.Exists(p=>Vector3.Distance(p,corner)<.000001f),Is.True);
            for(int x=0;x<=18;x++)for(int y=0;y<=12;y++)
            {
                Vector3 sample=Vector3.Lerp(Vector3.Lerp(face[0],face[1],x/18f),Vector3.Lerp(face[3],face[2],x/18f),y/12f);
                bool covered=false;
                for(int t=0;t<triangles.Count&&!covered;t+=3)
                    covered=InTriangle(sample,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]);
                Assert.That(covered,Is.True,"The rim must not leave holes in the original stone.");
            }
        }

        private static Vector2 FaceParameter(Vector3 point,Vector3[] face)
        {
            // Invert the bilinear authored face, rather than assuming its upper
            // edge is parallel/equal to the lower edge (battered stone is tapered).
            Vector2 result=new Vector2(.5f,.5f);
            for(int iteration=0;iteration<10;iteration++)
            {
                Vector3 p=Vector3.Lerp(Vector3.Lerp(face[0],face[1],result.x),Vector3.Lerp(face[3],face[2],result.x),result.y);
                Vector3 dx=Vector3.Lerp(face[1]-face[0],face[2]-face[3],result.y);
                Vector3 dy=Vector3.Lerp(face[3]-face[0],face[2]-face[1],result.x);
                Vector3 error=p-point;float determinant=dx.x*dy.y-dx.y*dy.x;
                result-=new Vector2((error.x*dy.y-error.y*dy.x)/determinant,(dx.x*error.y-dx.y*error.x)/determinant);
            }
            return result;
        }

        private static double Cross(Vector3 a,Vector3 b,Vector3 c)=>
            ((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
        private static bool InFace(Vector3 point,Vector3[] face)
        {
            for(int i=0;i<4;i++)if(Cross(face[i],face[(i+1)%4],point)<-.000002)return false;
            return true;
        }
        private static bool InTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)=>
            Cross(a,b,p)>=-.000002&&Cross(b,c,p)>=-.000002&&Cross(c,a,p)>=-.000002;
    }
}
