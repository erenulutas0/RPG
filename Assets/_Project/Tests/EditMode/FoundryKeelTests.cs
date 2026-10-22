using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryKeelTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static List<T> Data<T>(object builder,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(builder);

        [Test]
        public void KeelsHangWellBelowTheCourtsEdgeAndStayBounded()
        {
            var go=new GameObject("Keel test");
            try
            {
                var view=go.AddComponent<FoundryRegionView>();
                var cells=(HashSet<Vector2Int>)typeof(FoundryRegionView).GetField("_cells",Members).GetValue(view);
                for(int r=0;r<FoundryMap.RectangleCount;r++)
                    for(int x=FoundryMap.Bound(r,0);x<FoundryMap.Bound(r,2);x++)
                        for(int y=FoundryMap.Bound(r,1);y<FoundryMap.Bound(r,3);y++)cells.Add(new Vector2Int(x,y));
                object stone=Activator.CreateInstance(BuilderType,true);
                var layout=new List<(Vector2 p,Vector2 q,float upper,float lower)>();
                typeof(FoundryRegionView).GetMethod("BuildHangingKeelGeometry",Members).Invoke(view,new object[]{stone,layout});
                var vertices=Data<Vector3>(stone,"V");var uv=Data<Vector2>(stone,"U");var colours=Data<Color>(stone,"C");
                var triangles=Data<int>(stone,"T");
                Assert.That(vertices.Count,Is.InRange(800,6000),"Keels under every camera-facing span, within a small fixed budget.");
                Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colours.Count,Is.EqualTo(vertices.Count));
                Assert.That(triangles.Count%3,Is.Zero);
                foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
                foreach(Vector3 p in vertices)
                {
                    Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
                    Assert.That(p.z,Is.Zero,"Below-deck masonry writes no actor depth.");
                }
                foreach(Color c in colours)
                {
                    Assert.That(c.a,Is.EqualTo(1));
                    Assert.That(Mathf.Max(c.r,Mathf.Max(c.g,c.b)),Is.LessThanOrEqualTo(1.9f),"Inside the mobile fixed4 colour range.");
                    Assert.That(Mathf.Min(c.r,Mathf.Min(c.g,c.b)),Is.GreaterThanOrEqualTo(0f));
                }
                Assert.That(layout.Count,Is.GreaterThan(40),"Keels hang in clusters along the camera-facing edges.");
                float deepest=0;
                foreach(var face in layout)
                {
                    Assert.That(face.upper,Is.GreaterThanOrEqualTo(7.1f-1e-4f),"Keels hang from the curtain skirt's foot, below its face.");
                    Assert.That(face.lower,Is.GreaterThan(face.upper));Assert.That(face.lower,Is.LessThanOrEqualTo(16f));
                    deepest=Mathf.Max(deepest,face.lower);
                    foreach(Vector2 p in new[]{face.p,face.q})
                    {
                        Assert.That(p.x,Is.LessThanOrEqualTo(FoundryMap.East+.01f),"Not under the gate causeway.");
                        // Under the platform: a little inward of the face, the walking surface is overhead.
                        Vector2 inward=p+(new Vector2(-8,2)-p).normalized*.7f;
                        FoundryMap.ToFloor(inward.x,inward.y,out float x,out float y);
                        Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.True,$"Keel face at {p} hangs under the platform, not in the void.");
                    }
                }
                Assert.That(deepest,Is.GreaterThan(12f),"The deepest keels reach well into the cavern haze.");
                TestContext.WriteLine($"Hanging keels: {vertices.Count} vertices, {triangles.Count/3} triangles.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }
    }
}
