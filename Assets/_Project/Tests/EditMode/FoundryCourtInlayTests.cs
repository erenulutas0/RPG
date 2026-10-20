using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCourtInlayTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private static readonly Vector2 Centre=new Vector2(-3,-5);
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static object NewBuilder()=>Activator.CreateInstance(BuilderType,true);
        private static List<T> Data<T>(object builder,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(builder);
        private static object Invoke(string method,params object[] arguments)=>
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,arguments);
        private static Type Surface=>typeof(FoundryRegionView).Assembly.GetType("Cryptforge.UI.FoundrySurface");
        private static Vector3 Project(float u,float v)=>(Vector3)Surface.GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static Vector2 ToDesign(Vector3 p)
        {
            var arguments=new object[]{p.x,p.y,0f,0f};Surface.GetMethod("ToDesign",Static).Invoke(null,arguments);
            return new Vector2((float)arguments[2],(float)arguments[3]);
        }

        [Test]
        public void BronzeNetworkStaysOnOpenFloorAndReceivesOrdinaryFloorLight()
        {
            object inlay=NewBuilder();Invoke("BuildCourtInlayGeometry",inlay);
            var points=Data<Vector3>(inlay,"V");var triangles=Data<int>(inlay,"T");var uv=Data<Vector2>(inlay,"U");
            Assert.That(points.Count,Is.InRange(800,2600),"Thin strips: a bounded share of the reclaimed paving vertices.");
            var boilers=(Vector2[])typeof(FoundryRegionView).GetField("BoilerCentres",Static).GetValue(null);
            int metal=0;
            foreach(Vector3 p in points)
            {
                Assert.That(p.z,Is.EqualTo(0),"Flush floor inlay.");
                Vector2 d=ToDesign(p);
                FoundryMap.ToFloor(d.x,d.y,out float x,out float y);
                Assert.That(FoundryMap.Contains(x,y,.4f),Is.True,$"Inlay at ({d.x},{d.y}) stays on open walking floor, clear of the pit, kerb and work islands.");
                Assert.That(Vector2.Distance(d,Centre),Is.GreaterThan(5.05f),"The seal keeps its own setting.");
                foreach(Vector2 b in boilers)Assert.That(Vector2.Distance(d,b),Is.GreaterThan(2.3f),"No inlay under a boiler plinth.");
            }
            foreach(Vector2 t in uv)if((bool)Invoke("OnCopingRail",t))metal++;
            Assert.That(metal,Is.GreaterThan(points.Count/3),"Every line carries a metal course in its stone channel.");
            Invoke("LightFloorDecals",inlay);
            foreach(Color c in Data<Color>(inlay,"C"))
                Assert.That(Mathf.Max(c.r,Mathf.Max(c.g,c.b)),Is.LessThanOrEqualTo(1.9f),"Bounded, non-emissive brass response.");
            TestContext.WriteLine($"Court bronze network: {points.Count} vertices, {triangles.Count/3} triangles, {metal} metal-course vertices.");
        }

        [Test]
        public void OctagonSpokesAndLaneLinesFollowTheConceptPlan()
        {
            object inlay=NewBuilder();Invoke("BuildCourtInlayGeometry",inlay);
            var points=Data<Vector3>(inlay,"V");var triangles=Data<int>(inlay,"T");
            float radius=(float)typeof(FoundryRegionView).GetField("CourtInlayRadius",Static|BindingFlags.Public).GetValue(null);
            Assert.That(radius,Is.GreaterThan(5.05f+1.5f),"The octagon sits clear outside the seal's stone ring.");
            // Midpoints of the octagon's edges: at most two may be lost to the pit or equipment.
            int edges=0;
            for(int k=0;k<8;k++)
            {
                Vector2 a=Centre+new Vector2(Mathf.Cos(k*Mathf.PI/4),Mathf.Sin(k*Mathf.PI/4))*radius;
                Vector2 b=Centre+new Vector2(Mathf.Cos((k+1)*Mathf.PI/4),Mathf.Sin((k+1)*Mathf.PI/4))*radius;
                if(Covered(points,triangles,(a+b)*.5f))edges++;
            }
            Assert.That(edges,Is.GreaterThanOrEqualTo(6),"The octagon setting reads around the seal.");
            // Paired lane lines run beside both processional ways.
            var ends=new[]{new Vector2(-17.5f,-17.5f),new Vector2(15.9f,5)};
            float lane=1.85f+.24f+.16f;
            foreach(Vector2 end in ends)
            {
                Vector2 direction=(end-Centre).normalized,side=new Vector2(-direction.y,direction.x);
                float length=Vector2.Distance(end,Centre);
                foreach(float sign in new[]{-1f,1f})
                {
                    int hits=0,samples=0;
                    for(float t=radius+2;t<length-1;t+=1.5f){samples++;if(Covered(points,triangles,Centre+direction*t+side*sign*lane))hits++;}
                    Assert.That(hits,Is.GreaterThanOrEqualTo(samples*2/3),$"Lane line beside the way to {end} ({hits}/{samples}).");
                }
                // The way itself stays clear: no spoke runs down the processional stone.
                for(float t=radius+1;t<length-1;t+=1f)
                    Assert.That(Covered(points,triangles,Centre+direction*t),Is.False,"Processional stone stays open between its lane lines.");
            }
        }

        private static bool Covered(List<Vector3> v,List<int> t,Vector2 design)
        {
            Vector2 p=Project(design.x,design.y);
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                double d1=Side(p,a,b),d2=Side(p,b,c),d3=Side(p,c,a);
                if(!((d1<0||d2<0||d3<0)&&(d1>0||d2>0||d3>0)))return true;
            }
            return false;
        }
        private static double Side(Vector2 p,Vector2 a,Vector2 b)=>((double)b.x-a.x)*((double)p.y-a.y)-((double)b.y-a.y)*((double)p.x-a.x);
    }
}
