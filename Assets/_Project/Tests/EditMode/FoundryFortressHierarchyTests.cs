using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryFortressHierarchyTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [Test]
        public void ProjectedBlindPanelPartitionsItsTaperedParentWithoutHolesOrExpandedEdges()
        {
            var face=new[]{new Vector3(-.23f,-4.4f),new Vector3(.23f,-4.4f),
                new Vector3(.46f,0),new Vector3(-.46f,0)};
            object mesh=Builder();Call("FortressRecessedRespond",mesh,face[0],face[1],face[2],face[3],new Color(.4f,.45f,.55f));
            CheckMesh(mesh);
            foreach(Vector3 point in Data<Vector3>(mesh,"V"))Assert.That(InQuad(point,face),Is.True,
                "A projected jamb or return must remain within the original face.");
            for(int x=0;x<19;x++)for(int y=0;y<23;y++)
            {
                Vector3 point=Vector3.Lerp(Vector3.Lerp(face[0],face[1],(x+.37f)/19),
                    Vector3.Lerp(face[3],face[2],(x+.37f)/19),(y+.41f)/23);
                Assert.That(Covers(mesh,point),Is.True,"The panel frame and recessed backing must close the original wall.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PrimaryAndSecondaryRespondsRetainOriginalTaperShouldersAndSocketBacking(bool major)
        {
            object mesh=Builder();Call("BuildFortressRespond",mesh,Vector3.zero,major);CheckMesh(mesh);
            // Independent baseline envelope: full tapered front, two projected
            // bevels and the original two shoulder rectangles. The new face
            // division must fit their union, not a loose axis-aligned box.
            float half=major?.58f:.44f,depth=major?7.9f:6.9f;
            Vector3 top=Vector3.down*2.5f,foot=Vector3.down*depth,bevel=new Vector3(half*.32f,.12f);
            var oldFaces=new List<Vector3[]>
            {
                new[]{foot+Vector3.left*half*.55f,foot+Vector3.right*half*.55f,top+Vector3.right*half,top+Vector3.left*half},
                new[]{top+Vector3.left*half-bevel,top+Vector3.left*half,foot+Vector3.left*half*.55f,foot+Vector3.left*half*.55f-bevel*.45f},
                new[]{top+Vector3.right*half,top+Vector3.right*half+bevel,foot+Vector3.right*half*.55f+bevel*.45f,foot+Vector3.right*half*.55f}
            };
            foreach(float drop in new[]{.22f,3.12f})
            {
                Vector3 p=top+Vector3.down*drop;float width=half*(drop<1?1.18f:.92f);
                oldFaces.Add(new[]{p+Vector3.left*width,p+Vector3.right*width,
                    p+Vector3.right*width+Vector3.down*.2f,p+Vector3.left*width+Vector3.down*.2f});
            }
            foreach(Vector3 point in Data<Vector3>(mesh,"V"))
                Assert.That(oldFaces.Exists(face=>InQuad(point,face)),Is.True,"The new hierarchy cannot enlarge a support into adjacent architecture.");
            foreach(var face in oldFaces)foreach(Vector3 corner in face)
                Assert.That(Covers(mesh,corner),Is.True,"The original foot, bevel and bearing contacts must remain occupied.");
            if(major)foreach(float x in new[]{-.22f,.22f})foreach(float y in new[]{-3.05f,-4.25f})
                Assert.That(Covers(mesh,new Vector3(x,y)),Is.True,"The existing four iron socket corners require a continuous stone backing.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void BuiltBridgeBentsKeepOpenIntradosAndExactDeckContactWithoutDuplicatedUpperSpandrels(bool primary)
        {
            object returns=Builder(),front=Builder();const float station=-64.8f;
            Call("BuildBridgeBent",returns,front,station,primary);CheckMesh(returns);CheckMesh(front);
            Vector3 origin=new Vector3(0,station*FoundryMap.Axis*.5f);
            var vertices=Data<Vector3>(front,"V");var triangles=Data<int>(front,"T");
            foreach(Vector3 point in vertices)
            {
                float d=point.x/FoundryMap.Axis,h=point.y-origin.y;
                Assert.That(Mathf.Abs(d),Is.LessThanOrEqualTo(5.18002f));
                Assert.That(h,Is.InRange(-8.40002f,-2.69998f));
                if(Mathf.Abs(d)<3.94999f)Assert.That(h,Is.GreaterThanOrEqualTo(OriginalIntrados(d)-.00003f),
                    "The original arch aperture must not acquire a closing face.");
            }
            // Fixed physical deck line and aperture samples, independent of the
            // revised face helpers or their subdivision choices.
            for(int i=0;i<23;i++)
            {
                float d=Mathf.Lerp(-5.17f,5.17f,(i+.37f)/23);
                Assert.That(Covers(front,origin+new Vector3(d*FoundryMap.Axis,-2.7f)),Is.True);
                if(Mathf.Abs(d)<3.9f)Assert.That(Covers(front,origin+new Vector3(d*FoundryMap.Axis,OriginalIntrados(d)-.08f)),Is.False);
            }
            if(primary)for(int t=0;t<triangles.Count;t+=3)
            {
                float a=vertices[triangles[t]].y-origin.y,b=vertices[triangles[t+1]].y-origin.y,c=vertices[triangles[t+2]].y-origin.y;
                Assert.That(Mathf.Min(a,Mathf.Min(b,c))<-3.03003f&&Mathf.Max(a,Mathf.Max(b,c))>-3.02997f,Is.False,
                    "The broad bearing must replace the upper parent spandrel, not overlay a triangle spanning both regions.");
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActualBridgeBearingSharesOnePhysicalMineralFieldAcrossItsFrontAndSoffit(bool primary)
        {
            object returns=Builder(),front=Builder();
            Call("BuildBridgeBent",returns,front,-64.8f,primary);
            // A fixed physical field now folds within the protected atlas region.
            // The old globally affine fit enlarged grain on large supports. Keep
            // the independent datum and require interpolated triangle interiors,
            // not merely endpoints, to follow the same continuous physical field.
            Vector3 origin=new Vector3(0,-64.8f*FoundryMap.Axis*.5f);
            Vector2 Expected(Vector3 point)=>new Vector2(.04f+Mirror((point.x-origin.x)*.075f/.35f)*.35f,
                .44f+Mirror((point.y-origin.y)*.075f/.124f)*.124f);
            int compared=0;
            foreach(object mesh in new[]{front,returns})
            {
                var p=Data<Vector3>(mesh,"V");var u=Data<Vector2>(mesh,"U");
                foreach(int i in new HashSet<int>(Data<int>(mesh,"T")))
                {
                    Vector2 expected=Expected(p[i]);
                    Assert.That(Vector2.Distance(u[i],expected),Is.LessThan(.000002f),
                        "A fitted stone, dressed arris or separate soffit batch cannot restart the parent mineral field.");
                    compared++;
                }
                var indices=Data<int>(mesh,"T");
                for(int i=0;i<indices.Count;i+=3)
                {
                    int a=indices[i],b=indices[i+1],c=indices[i+2];
                    Vector3 sample=p[a]*.23f+p[b]*.31f+p[c]*.46f;
                    Vector2 actual=u[a]*.23f+u[b]*.31f+u[c]*.46f;
                    Assert.That(Vector2.Distance(actual,Expected(sample)),Is.LessThan(.000002f),
                        "Triangles must split at a fold rather than smear across it.");
                }
            }
            Assert.That(compared,Is.GreaterThan(150));
        }

        [Test]
        public void CarvedBridgeWedgePartitionsItsOriginalFaceAndRetainsOpenInnerEdge()
        {
            // A sloping load-bearing voussoir from the actual eleven-piece arch.
            float a=3*Mathf.PI/11,b=4*Mathf.PI/11;
            var face=new[]{new Vector3(Mathf.Cos(a)*3.95f*FoundryMap.Axis,-6.1f+Mathf.Sin(a)*2.35f),
                new Vector3(Mathf.Cos(b)*3.95f*FoundryMap.Axis,-6.1f+Mathf.Sin(b)*2.35f),
                new Vector3(Mathf.Cos(b)*5.18f*FoundryMap.Axis,-6.1f+Mathf.Sin(b)*3.42f),
                new Vector3(Mathf.Cos(a)*5.18f*FoundryMap.Axis,-6.1f+Mathf.Sin(a)*3.42f)};
            // The actual primary wedge ends at the straight bearing course.
            face[2].y=Mathf.Min(face[2].y,-3.03f);face[3].y=Mathf.Min(face[3].y,-3.03f);
            object mesh=Builder();Call("FortressDressedArch",mesh,face[0],face[1],face[2],face[3],Color.white);
            var points=Data<Vector3>(mesh,"V");var colours=Data<Color>(mesh,"C");var triangles=Data<int>(mesh,"T");
            double actual=0,expected=Math.Abs(Cross(face[0],face[1],face[2]))+Math.Abs(Cross(face[0],face[2],face[3]));
            for(int t=0;t<triangles.Count;t+=3)
            {
                Vector3 p=points[triangles[t]],q=points[triangles[t+1]],r=points[triangles[t+2]];
                double area=Math.Abs(Cross(p,q,r));Assert.That(area,Is.GreaterThan(1e-10));actual+=area;
            }
            Assert.That(actual,Is.EqualTo(expected).Within(.00001),"The carved return replaces its parent area without an underlying duplicate face.");
            foreach(Vector3 p in points)Assert.That(InQuad(p,face),Is.True);
            foreach(Vector3 p in face)Assert.That(points,Does.Contain(p));
            float low=1,high=0;foreach(Color c in colours){low=Mathf.Min(low,c.grayscale);high=Mathf.Max(high,c.grayscale);}
            Assert.That(high-low,Is.GreaterThan(.25f),"The exposed face must remain distinct from its recessed inner return.");
            for(int x=0;x<13;x++)for(int y=0;y<11;y++)
                Assert.That(Covers(mesh,Vector3.Lerp(Vector3.Lerp(face[0],face[1],(x+.37f)/13),
                    Vector3.Lerp(face[3],face[2],(x+.37f)/13),(y+.41f)/11)),Is.True);
        }

        [Test]
        public void PrimarySpandrelCoursesPartitionEveryOriginalHaunchFace()
        {
            Vector3 origin=new Vector3(0,-64.8f*FoundryMap.Axis*.5f);
            int oldVertices=0,newVertices=0,checkedFaces=0;
            for(int i=0;i<11;i++)
            {
                float a=i*Mathf.PI/11,b=(i+1)*Mathf.PI/11;
                var face=new[]{origin+new Vector3(Mathf.Cos(a)*5.18f*FoundryMap.Axis,
                        Mathf.Min(-3.03f,-6.1f+Mathf.Sin(a)*3.42f)),
                    origin+new Vector3(Mathf.Cos(b)*5.18f*FoundryMap.Axis,
                        Mathf.Min(-3.03f,-6.1f+Mathf.Sin(b)*3.42f)),
                    origin+new Vector3(Mathf.Cos(b)*5.18f*FoundryMap.Axis,-3.03f),
                    origin+new Vector3(Mathf.Cos(a)*5.18f*FoundryMap.Axis,-3.03f)};
                double expected=Math.Abs(Cross(face[0],face[1],face[2]))+Math.Abs(Cross(face[0],face[2],face[3]));
                if(expected<1e-9)continue;
                object mesh=Builder();Call("BridgeSpandrelCourses",mesh,face[0],face[1],face[2],face[3],origin);
                var points=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
                double actual=0;
                for(int triangle=0;triangle<triangles.Count;triangle+=3)
                {
                    Vector3 p=points[triangles[triangle]],q=points[triangles[triangle+1]],r=points[triangles[triangle+2]];
                    double area=Math.Abs(Cross(p,q,r));Assert.That(area,Is.GreaterThan(1e-10));actual+=area;
                    Assert.That(Owners(mesh,((double)p.x+q.x+r.x)/3,((double)p.y+q.y+r.y)/3),Is.EqualTo(1),
                        "A fitted block or mortar bed cannot retain an underlying parent face.");
                }
                Assert.That(actual,Is.EqualTo(expected).Within(.00001),"The clipped courses must preserve the entire original haunch area.");
                foreach(Vector3 p in points)Assert.That(InQuad(p,face),Is.True);
                foreach(Vector3 p in face)Assert.That(points,Does.Contain(p),"Retain the original arch and straight bearing contact stations.");
                oldVertices+=4;newVertices+=points.Count;checkedFaces++;
            }
            Assert.That(checkedFaces,Is.GreaterThan(4));
            Assert.That(newVertices-oldVertices,Is.InRange(1,400),"Refine existing haunches within the declared bounded entry budget.");
        }

        [Test]
        public void ActualPrimarySpandrelBedsRegisterAcrossWedgesWithoutAnOverpaintingBase()
        {
            object returns=Builder(),front=Builder();const float station=-64.8f;
            Call("BuildBridgeBent",returns,front,station,true);
            Vector3 origin=new Vector3(0,station*FoundryMap.Axis*.5f);
            // Samples straddle the first/second original wedges on each side.
            // Shared bed heights are architectural coordinates, independent of
            // each wedge's different lower edge and internal triangulation.
            foreach(float side in new[]{-1f,1f})foreach(float bed in new[]{-5.1f,-4.1f})
            {
                foreach(float d in new[]{5.05f,bed< -5?4.965f:4.6f})
                {
                    Vector3 joint=origin+new Vector3(side*d*FoundryMap.Axis,bed+.003f);
                    Assert.That(Owners(front,joint),Is.EqualTo(1));
                    float mortar=ColourAt(front,joint);
                    Assert.That(ColourAt(front,joint+Vector3.up*.07f)-mortar,Is.GreaterThan(.08f),
                        "A real dark bed must divide the two fitted courses across source wedge boundaries.");
                    Assert.That(Owners(front,joint+Vector3.up*.07f),Is.EqualTo(1));
                }
            }
        }

        private static int Owners(object mesh,Vector3 point)=>Owners(mesh,point.x,point.y);

        private static int Owners(object mesh,double px,double py)
        {
            var p=Data<Vector3>(mesh,"V");var t=Data<int>(mesh,"T");int owners=0;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=p[t[i]],b=p[t[i+1]],c=p[t[i+2]];
                double area=Cross(a,b,c);if(Math.Abs(area)<1e-10)continue;
                double x=((b.x-px)*(c.y-py)-(b.y-py)*(c.x-px))/area;
                double y=((px-a.x)*(c.y-(double)a.y)-(py-a.y)*(c.x-(double)a.x))/area,z=1-x-y;
                if(x>1e-6&&y>1e-6&&z>1e-6)owners++;
            }
            return owners;
        }

        private static float ColourAt(object mesh,Vector3 point)
        {
            var p=Data<Vector3>(mesh,"V");var t=Data<int>(mesh,"T");var c=Data<Color>(mesh,"C");
            for(int i=0;i<t.Count;i+=3)
            {
                Vector3 a=p[t[i]],b=p[t[i+1]],d=p[t[i+2]];
                double area=Cross(a,b,d);if(Math.Abs(area)<1e-10)continue;
                double x=Cross(point,b,d)/area,y=Cross(a,point,d)/area,z=1-x-y;
                if(x>=-1e-6&&y>=-1e-6&&z>=-1e-6)
                    return (float)(c[t[i]].grayscale*x+c[t[i+1]].grayscale*y+c[t[i+2]].grayscale*z);
            }
            Assert.Fail("The expected fitted masonry sample has no owner.");return 0;
        }

        private static float OriginalIntrados(float d)
        {
            // The baseline aperture is the eleven-segment polygon, not an
            // analytic ellipse that would incorrectly reject its chord interiors.
            for(int i=0;i<11;i++)
            {
                float a=i*Mathf.PI/11,b=(i+1)*Mathf.PI/11;
                float x0=Mathf.Cos(a)*3.95f,x1=Mathf.Cos(b)*3.95f;
                if(d<x1-.00001f||d>x0+.00001f)continue;
                return Mathf.Lerp(-6.1f+Mathf.Sin(a)*2.35f,-6.1f+Mathf.Sin(b)*2.35f,(d-x0)/(x1-x0));
            }
            throw new ArgumentOutOfRangeException(nameof(d));
        }
        private static float Mirror(float value)
        {
            double period=value-System.Math.Floor(value/2d)*2d;
            return (float)(period<=1d?period:2d-period);
        }
        private static void CheckMesh(object mesh)
        {
            var vertices=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");var colors=Data<Color>(mesh,"C");
            Assert.That(vertices.Count,Is.GreaterThan(0));Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colors.Count,Is.EqualTo(vertices.Count));
            for(int i=0;i<vertices.Count;i++)
            {
                Assert.That(float.IsNaN(vertices[i].x)||float.IsInfinity(vertices[i].x)||float.IsNaN(vertices[i].y)||float.IsInfinity(vertices[i].y),Is.False);
                Assert.That(vertices[i].z,Is.Zero);Assert.That(colors[i].a,Is.EqualTo(1));
                Assert.That(uv[i].x,Is.InRange(.03999f,.39001f));Assert.That(uv[i].y,Is.InRange(.43999f,.56401f));
            }
            foreach(int index in Data<int>(mesh,"T"))Assert.That(index,Is.InRange(0,vertices.Count-1));
        }
        private static bool Covers(object mesh,Vector3 point)
        {
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            for(int i=0;i<triangles.Count;i+=3)if(InTriangle(point,vertices[triangles[i]],vertices[triangles[i+1]],vertices[triangles[i+2]]))return true;
            return false;
        }
        private static bool InQuad(Vector3 p,Vector3[] face)=>InTriangle(p,face[0],face[1],face[2])||InTriangle(p,face[0],face[2],face[3]);
        private static bool InTriangle(Vector3 p,Vector3 a,Vector3 b,Vector3 c)
        {
            double area=Cross(a,b,c);if(Math.Abs(area)<1e-10)return false;
            double x=Cross(p,b,c)/area,y=Cross(a,p,c)/area,z=1-x-y;
            return x>=-1e-5&&y>=-1e-5&&z>=-1e-5;
        }
        private static double Cross(Vector3 a,Vector3 b,Vector3 c)=>((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static List<T> Data<T>(object mesh,string name)=>(List<T>)mesh.GetType().GetField(name,Fields).GetValue(mesh);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
