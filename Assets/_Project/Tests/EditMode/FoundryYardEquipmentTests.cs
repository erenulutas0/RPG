using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryYardEquipmentTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;

        [TestCase(0,"BuildYardTrolley")]
        [TestCase(1,"BuildYardMouldBench")]
        public void ActualEquipmentRetainsSharedProjectedFootprintAndDepthEnvelope(int island,string method)
        {
            float u0=FoundryMap.WorkIslandBound(island,0),v0=FoundryMap.WorkIslandBound(island,1);
            float u1=FoundryMap.WorkIslandBound(island,2),v1=FoundryMap.WorkIslandBound(island,3);
            object builder=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,new[]{builder,(object)u0,v0,u1,v1});
            var points=Field<List<Vector3>>(builder,"V");
            var uv=Field<List<Vector2>>(builder,"U");
            var colors=Field<List<Color>>(builder,"C");
            var indices=Field<List<int>>(builder,"T");
            Assert.That(points.Count,Is.GreaterThan(0));
            if(island==1)Assert.That(points.Count,Is.LessThanOrEqualTo(1800),
                "The complete stocked moulding station retains a bounded static geometry budget.");
            Assert.That(uv.Count,Is.EqualTo(points.Count));
            Assert.That(colors.Count,Is.EqualTo(points.Count));
            Assert.That(indices.Count%3,Is.Zero);
            var corners=new[]{Project(u0,v0),Project(u1,v0),Project(u1,v1),Project(u0,v1)};
            foreach(int index in indices)Assert.That(index,Is.InRange(0,points.Count-1));
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(float.IsNaN(points[i].x)||float.IsInfinity(points[i].x)||
                    float.IsNaN(points[i].y)||float.IsInfinity(points[i].y),Is.False);
                Assert.That(uv[i].x,Is.InRange(0f,1f));
                Assert.That(uv[i].y,Is.InRange(0f,1f));
                Assert.That(colors[i].a,Is.EqualTo(1f).Within(.00001f));
                // Support lines of the projected shared box plus the exact existing
                // vertical owner envelope. A higher clamp or projecting handle must
                // not silently escape the obstacle's cutaway/depth coverage.
                for(int direction=0;direction<72;direction++)
                {
                    float a=direction*Mathf.PI/36;
                    var axis=new Vector3(Mathf.Cos(a),Mathf.Sin(a),0);
                    float limit=float.NegativeInfinity;
                    foreach(Vector3 corner in corners)limit=Mathf.Max(limit,Vector3.Dot(axis,corner));
                    limit+=axis.y*(axis.y>0?1.55f:.10f);
                    Assert.That(Vector3.Dot(axis,points[i]),Is.LessThanOrEqualTo(limit+.0001f),
                        "Equipment must stay in its established projected island/depth envelope.");
                }
            }
        }

        [Test]
        public void MouldingStationStoresOpenSupportedFlasksAndUsesDirectionalTimberGrain()
        {
            float u0=FoundryMap.WorkIslandBound(1,0),v0=FoundryMap.WorkIslandBound(1,1);
            float u1=FoundryMap.WorkIslandBound(1,2),v1=FoundryMap.WorkIslandBound(1,3);
            object complete=BuildEquipment("BuildYardMouldBench",u0,v0,u1,v1);
            var points=Field<List<Vector3>>(complete,"V");var uv=Field<List<Vector2>>(complete,"U");
            var colours=Field<List<Color>>(complete,"C");var triangles=Field<List<int>>(complete,"T");
            var referenced=new HashSet<int>(triangles);
            object lower=BuildEquipment("YardStoredFlask",u0+.44f,v0+.44f,u0+1.38f,v1-.42f,.49f,.63f);
            object upper=BuildEquipment("YardStoredFlask",u0+.475f,v0+.475f,u0+1.345f,v1-.455f,.63f,.77f);
            foreach(object flask in new[]{lower,upper})
            {
                var p=Field<List<Vector3>>(flask,"V");var indices=Field<List<int>>(flask,"T");
                // The opening is intentionally much wider than a numerical edge:
                // an empty stored flask must not retain the old closed top plate.
                float top=flask==lower?.63f:.77f;
                Vector3 centre=Project(u0+.91f,(v0+v1+.02f)*.5f)+Vector3.up*top;
                Assert.That(Covered(p,indices,centre),Is.False,"Spare moulds need real open centres, not a painted box top.");
                foreach(int i in new HashSet<int>(indices))
                    Assert.That(Find(points,referenced,p[i]),Is.GreaterThanOrEqualTo(0),
                        "The complete station must actually emit each supported spare flask.");
            }
            // The upper ring bears on the lower ring rather than hovering over it.
            foreach(Vector2 station in new[]{new Vector2(u0+.475f,v0+.475f),
                new Vector2(u0+1.345f,v0+.475f),new Vector2(u0+.475f,v1-.455f)})
            {
                Vector3 foot=Project(station.x,station.y)+Vector3.up*.63f;
                Assert.That(Find(points,referenced,foot),Is.GreaterThanOrEqualTo(0));
                Assert.That(Covered(Field<List<Vector3>>(lower,"V"),Field<List<int>>(lower,"T"),foot),Is.True);
            }
            float start=v0+.10f,end=v0+.10f+(v1-v0-.20f)/3-.016f;
            Vector3 a=Project(u0+.125f,start+.025f)+Vector3.up*1.13f;
            Vector3 b=Project(u1-.125f,start+.025f)+Vector3.up*1.13f;
            Vector3 c=Project(u1-.125f,end-.025f)+Vector3.up*1.13f;
            int ia=Find(points,referenced,a),ib=Find(points,referenced,b),ic=Find(points,referenced,c);
            Assert.That(ia,Is.GreaterThanOrEqualTo(0));Assert.That(ib,Is.GreaterThanOrEqualTo(0));Assert.That(ic,Is.GreaterThanOrEqualTo(0));
            float along=Vector2.Distance(uv[ia],uv[ib])/Vector3.Distance(a,b);
            float across=Vector2.Distance(uv[ib],uv[ic])/Vector3.Distance(b,c);
            Assert.That(across,Is.GreaterThan(along*1.8f).And.LessThan(along*4),
                "Actual worktop grain must run along the substantial boards, with a noncollapsed cross-grain field.");
            foreach(int i in new[]{ia,ib,ic})
            {
                // The accepted workyard palette is muted hardwood beside the
                // baked casting bench, retaining warmth without saturated orange.
                Assert.That(colours[i].r/colours[i].g,Is.InRange(1.19f,1.27f));
                Assert.That(colours[i].b/colours[i].g,Is.InRange(.69f,.74f));
            }
        }

        [Test]
        public void ActualBenchIronKeepsNeutralFamilyAndBroadContactWithoutMeshChanges()
        {
            float u0=FoundryMap.WorkIslandBound(1,0),v0=FoundryMap.WorkIslandBound(1,1);
            float u1=FoundryMap.WorkIslandBound(1,2),v1=FoundryMap.WorkIslandBound(1,3);
            object builder=BuildEquipment("BuildYardMouldBench",u0,v0,u1,v1);
            var points=Field<List<Vector3>>(builder,"V");var colours=Field<List<Color>>(builder,"C");
            var indices=Field<List<int>>(builder,"T");var uv=Field<List<Vector2>>(builder,"U");
            var savedPoints=points.ToArray();var savedIndices=indices.ToArray();
            Vector3 origin=Project((u0+u1)*.5f,(v0+v1)*.5f)+Vector3.up*.84f;
            MethodInfo finish=typeof(FoundryRegionView).GetMethod("YardBenchIronFinish",Static);
            // Equal input light on actual produced geometry isolates the material
            // family and broad contacts from the original face-light differences.
            for(int i=0;i<colours.Count;i++)colours[i]=new Color(.5f,.5f,.5f,1);
            finish.Invoke(null,new[]{builder,(object)0,points.Count,origin});
            var savedUvs=uv.ToArray();float low=0,high=0;int lowCount=0,highCount=0;
            foreach(int i in new HashSet<int>(indices))
            {
                Color c=colours[i];float height=points[i].y-origin.y;
                Assert.That(c.b/c.r,Is.InRange(1.015f,1.045f),"Bench and native winch use the same gently cool iron range.");
                Assert.That(c.grayscale,Is.LessThan(.5f));
                if(height<=-.64f){low+=c.grayscale;lowCount++;}
                if(height>=.60f){high+=c.grayscale;highCount++;}
            }
            Assert.That(lowCount,Is.GreaterThan(0));Assert.That(highCount,Is.GreaterThan(0));
            Assert.That(low/lowCount,Is.LessThan(high/highCount*.92f));
            for(int i=0;i<colours.Count;i++)colours[i]=new Color(.5f,.5f,.5f,1);
            finish.Invoke(null,new[]{builder,(object)0,points.Count,origin});
            Assert.That(points,Is.EqualTo(savedPoints));Assert.That(indices,Is.EqualTo(savedIndices));Assert.That(uv,Is.EqualTo(savedUvs));
        }

        [Test]
        public void CompleteTrolleyAndItsSkidUseOneNoncollapsedCastField()
        {
            object builder=TrolleyWithSkid();
            var points=Field<List<Vector3>>(builder,"V");var uv=Field<List<Vector2>>(builder,"U");
            var indices=Field<List<int>>(builder,"T");var shared=new Dictionary<Vector3,Vector2>();
            int visible=0,joins=0;
            for(int i=0;i<points.Count;i++)
            {
                Assert.That(uv[i].x,Is.InRange(0f,1f));Assert.That(uv[i].y,Is.InRange(0f,1f));
                if(shared.TryGetValue(points[i],out Vector2 other))
                {
                    joins++;
                    Assert.That(Vector2.Distance(other,uv[i]),Is.LessThan(.000001f),
                        "Frame, stock, bands and skid must not restart their cast grain at the same projected point.");
                }
                else shared.Add(points[i],uv[i]);
            }
            for(int t=0;t<indices.Count;t+=3)
            {
                int a=indices[t],b=indices[t+1],c=indices[t+2];
                if(Math.Abs(Cross(points[a],points[b],points[c]))<1e-7)continue;
                visible++;
                Assert.That(Math.Abs(Cross(uv[a],uv[b],uv[c])),Is.GreaterThan(1e-12),
                    "Actual trolley triangles must sample a two-dimensional field, not a clamped texture edge.");
                for(int side=0;side<3;side++)
                {
                    int i=indices[t+side],j=indices[t+(side+1)%3];
                    float distance=Vector2.Distance(points[i],points[j]);
                    if(distance>.01f)
                        Assert.That(Vector2.Distance(uv[i],uv[j])/distance,Is.EqualTo(.19f).Within(.00001f),
                            "The complete trolley uses the hood's grain density, including stock and thin hardware.");
                }
            }
            Assert.That(visible,Is.GreaterThan(0));Assert.That(joins,Is.GreaterThan(0));
        }

        [Test]
        public void TrolleyCastRemapChangesOnlyUvsAndPreservesAuthoredLightAndGeometry()
        {
            object builder=TrolleyWithSkid();
            var points=Field<List<Vector3>>(builder,"V");var uv=Field<List<Vector2>>(builder,"U");
            var colours=Field<List<Color>>(builder,"C");var indices=Field<List<int>>(builder,"T");
            var savedPoints=points.ToArray();var savedUvs=uv.ToArray();
            var savedColours=colours.ToArray();var savedIndices=indices.ToArray();
            float u0=FoundryMap.WorkIslandBound(0,0),v0=FoundryMap.WorkIslandBound(0,1);
            float u1=FoundryMap.WorkIslandBound(0,2),v1=FoundryMap.WorkIslandBound(0,3);
            Vector3 origin=Project((u0+u1)*.5f,(v0+v1)*.5f)+Vector3.up*.63f;
            // Shift only the field origin to exercise the actual final UV operation
            // with a different input; neither geometry nor forge colour may follow it.
            typeof(FoundryRegionView).GetMethod("YardTrolleyGrain",Static)
                .Invoke(null,new[]{builder,(object)(origin+new Vector3(.15f,-.12f,0))});
            Assert.That(points,Is.EqualTo(savedPoints));Assert.That(indices,Is.EqualTo(savedIndices));
            Assert.That(colours,Is.EqualTo(savedColours));Assert.That(uv.Count,Is.EqualTo(savedUvs.Length));
            for(int i=0;i<uv.Count;i++)
                Assert.That(Vector2.Distance(uv[i],savedUvs[i]),Is.GreaterThan(.03f),
                    "The remap was actually exercised on every owned vertex.");
        }

        private static object TrolleyWithSkid()
        {
            float u0=FoundryMap.WorkIslandBound(0,0),v0=FoundryMap.WorkIslandBound(0,1);
            float u1=FoundryMap.WorkIslandBound(0,2),v1=FoundryMap.WorkIslandBound(0,3);
            object builder=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            typeof(FoundryRegionView).GetMethod("WorkshopMetalHull",Static)
                .Invoke(null,new object[]{builder,u0,v0,u1,v1,.015f,.10f,.035f,true});
            typeof(FoundryRegionView).GetMethod("BuildYardTrolley",Static)
                .Invoke(null,new object[]{builder,u0,v0,u1,v1});
            return builder;
        }

        private static object BuildEquipment(string method,params float[] values)
        {
            object builder=Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
            var args=new object[values.Length+1];args[0]=builder;
            for(int i=0;i<values.Length;i++)args[i+1]=values[i];
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,args);
            return builder;
        }

        private static int Find(List<Vector3> points,HashSet<int> referenced,Vector3 point)
        {
            foreach(int i in referenced)if(Vector3.Distance(points[i],point)<.00001f)return i;
            return -1;
        }

        private static bool Covered(List<Vector3> points,List<int> triangles,Vector2 point)
        {
            for(int t=0;t<triangles.Count;t+=3)
            {
                Vector2 a=points[triangles[t]],b=points[triangles[t+1]],c=points[triangles[t+2]];
                double area=Cross(a,b,c);if(Math.Abs(area)<1e-8)continue;
                double x=Cross(point,b,c)/area,y=Cross(a,point,c)/area,z=1-x-y;
                if(x>=-.00001&&y>=-.00001&&z>=-.00001)return true;
            }
            return false;
        }

        private static double Cross(Vector2 a,Vector2 b,Vector2 c)=>
            ((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);

        private static Vector3 Project(float u,float v)=>(Vector3)typeof(FoundryRegionView).GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static T Field<T>(object builder,string name)=>(T)builder.GetType().GetField(name,Fields).GetValue(builder);
    }
}
