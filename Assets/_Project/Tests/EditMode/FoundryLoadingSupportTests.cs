using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryLoadingSupportTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static object Builder()=>Activator.CreateInstance(BuilderType,true);
        private static List<T> List<T>(object builder,string name)=>(List<T>)BuilderType.GetField(name,Members).GetValue(builder);

        [TestCase("SalvageShelf")]
        [TestCase("StockShelf")]
        [TestCase("CastingShelf")]
        [TestCase("Crane")]
        public void LoadingFacadeRetainsPhysicalEnvelopeDeckAndBelowFloorSplit(string station)
        {
            bool crane=station=="Crane";
            Vector4 box=crane?new Vector4(-27+FoundryMap.WestAssemblyOffset,11,-24+FoundryMap.WestAssemblyOffset,15):
                (Vector4)typeof(FoundryRegionView).GetProperty(station,Static|BindingFlags.Public).GetValue(null);
            float rise=crane?1.1f:.18f,depth=crane?3f:2.8f;
            object foundation=Builder(),raised=Builder(),iron=Builder(),cap=Builder();
            typeof(FoundryRegionView).GetMethod("ServiceShelfFaces",Static)
                .Invoke(null,new[]{foundation,raised,iron,(object)box,rise,depth,!crane});
            typeof(FoundryRegionView).GetMethod("BevelCap",Static)
                .Invoke(null,new[]{cap,(object)box.x,box.y,box.z,box.w,rise});
            CheckMesh(foundation,Prism(box,-depth,0));
            CheckMesh(raised,Prism(box,0,crane?rise:rise-.15f));
            CheckMesh(cap,Prism(box,rise,rise));
            if(crane)Assert.That(List<Vector3>(iron,"V"),Is.Empty,"The crane retains its existing metal cap and socket.");
            else CheckMesh(iron,Prism(box,rise-.15f,rise));
            int cost=List<Vector3>(foundation,"V").Count+List<Vector3>(raised,"V").Count+
                List<Vector3>(iron,"V").Count+List<Vector3>(cap,"V").Count;
            Assert.That(cost,Is.LessThanOrEqualTo(760),"Keep the whole connected support bounded, not dozens of stacked trim pieces.");
            // The unchanged cap supports the exact four deck corners. Its area is
            // neither shrunk under the equipment nor expanded over the walking lane.
            var capVertices=List<Vector3>(cap,"V");
            foreach(Vector2 corner in Corners(box))
            {
                Vector3 target=Project(corner.x,corner.y)+Vector3.up*rise;
                Assert.That(capVertices.Exists(p=>Vector3.Distance(p,target)<.000001f),Is.True);
            }
            // All four footprint interiors remain scenic. The original crane
            // footing meets the West boundary exactly, so its boundary contact
            // must not be mistaken for an added walkable-floor obstruction.
            for(int u=0;u<=4;u++)for(int v=0;v<=4;v++)
            {
                FoundryMap.ToFloor(Mathf.Lerp(box.x+.0001f,box.z-.0001f,u*.25f),
                    Mathf.Lerp(box.y+.0001f,box.w-.0001f,v*.25f),out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False);
            }
        }

        [Test]
        public void CastingBearingsStayInsideOriginalBelowDeckAttachmentVolumes()
        {
            object mesh=Builder();
            typeof(FoundryRegionView).GetMethod("CastingShelfBearings",Static).Invoke(null,new[]{mesh});
            var first=Prism(new Vector4(-17.4f,17.8f,-16.7f,19.5f),-2.5f,-.32f);
            var second=Prism(new Vector4(-14.3f,17.8f,-13.6f,19.5f),-2.5f,-.32f);
            CheckMesh(mesh,null);
            foreach(Vector3 p in List<Vector3>(mesh,"V"))
                Assert.That(Contains(first,p)||Contains(second,p),Is.True,
                    "A cantilever must not extend onto the walkable deck or beyond its established attachment box.");
            Assert.That(List<Vector3>(mesh,"V").Count,Is.LessThanOrEqualTo(280));
        }

        private static void CheckMesh(object mesh,List<Vector2> boundary)
        {
            var vertices=List<Vector3>(mesh,"V");var uv=List<Vector2>(mesh,"U");
            var colors=List<Color>(mesh,"C");var triangles=List<int>(mesh,"T");
            Assert.That(vertices.Count,Is.GreaterThan(0));
            Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colors.Count,Is.EqualTo(vertices.Count));
            foreach(Vector3 p in vertices)
            {
                Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
                if(boundary!=null)Assert.That(Contains(boundary,p),Is.True,"Emitted masonry must stay within its original projected support volume.");
            }
            foreach(Vector2 p in uv)Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
            Assert.That(triangles.Count%3,Is.Zero);
            foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
        }

        private static Vector2[] Corners(Vector4 box)=>new[]{new Vector2(box.x,box.y),new Vector2(box.z,box.y),
            new Vector2(box.z,box.w),new Vector2(box.x,box.w)};

        private static Vector3 Project(float u,float v)=>(Vector3)typeof(FoundryRegionView)
            .GetMethod("Project",Static).Invoke(null,new object[]{u,v});

        private static List<Vector2> Prism(Vector4 box,float bottom,float top)
        {
            var points=new List<Vector2>();
            foreach(Vector2 corner in Corners(box))foreach(float height in new[]{bottom,top})
                points.Add((Vector2)Project(corner.x,corner.y)+Vector2.up*height);
            points.Sort((a,b)=>a.x!=b.x?a.x.CompareTo(b.x):a.y.CompareTo(b.y));
            var lower=new List<Vector2>();var upper=new List<Vector2>();
            foreach(Vector2 point in points)
            {
                while(lower.Count>=2&&Cross(lower[lower.Count-2],lower[lower.Count-1],point)<=0)lower.RemoveAt(lower.Count-1);
                lower.Add(point);
            }
            for(int i=points.Count-1;i>=0;i--)
            {
                Vector2 point=points[i];
                while(upper.Count>=2&&Cross(upper[upper.Count-2],upper[upper.Count-1],point)<=0)upper.RemoveAt(upper.Count-1);
                upper.Add(point);
            }
            lower.RemoveAt(lower.Count-1);upper.RemoveAt(upper.Count-1);lower.AddRange(upper);return lower;
        }

        private static bool Contains(List<Vector2> hull,Vector2 p)
        {
            for(int i=0;i<hull.Count;i++)
            {
                Vector2 a=hull[i],b=hull[(i+1)%hull.Count];
                if(Cross(a,b,p)<-.00002*Vector2.Distance(a,b))return false;
            }
            return true;
        }

        private static double Cross(Vector2 a,Vector2 b,Vector2 p)=>
            ((double)b.x-a.x)*((double)p.y-a.y)-((double)b.y-a.y)*((double)p.x-a.x);
    }
}
