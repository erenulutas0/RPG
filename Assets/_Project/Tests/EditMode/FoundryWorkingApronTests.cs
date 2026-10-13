using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryWorkingApronTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static Type View=>typeof(FoundryRegionView);
        private static Type BuilderType=>View.GetNestedType("Builder",BindingFlags.NonPublic);
        private static object Builder()=>Activator.CreateInstance(BuilderType,true);
        private static object Call(string method,params object[] args)=>View.GetMethod(method,Static).Invoke(null,args);
        private static List<T> Data<T>(object mesh,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(mesh);
        private static int Field(object stone,string field)=>(int)stone.GetType().GetField(field,Members).GetValue(stone);
        private static void Set(object stone,string field,int value)=>stone.GetType().GetField(field,Members).SetValue(stone,value);

        [Test]
        public void WorkingApronPreservesPackedCoverageWorldGrainAndBroadSmallParentsWithinBoundedCost()
        {
            int u0=FoundryMap.West,v0=FoundryMap.South,u1=u0,v1=v0;
            for(int r=0;r<FoundryMap.RectangleCount;r++)
            {
                u1=Math.Max(u1,FoundryMap.Bound(r,2));v1=Math.Max(v1,FoundryMap.Bound(r,3));
            }
            var zones=new sbyte[(u1-u0)*2,(v1-v0)*2];
            for(int i=0;i<zones.GetLength(0);i++)for(int j=0;j<zones.GetLength(1);j++)zones[i,j]=-1;
            for(int u=u0;u<u1;u++)for(int v=v0;v<v1;v++)
            {
                if(!FoundryMap.UncutCell(u,v))continue;
                int zone=(int)Call("PavingZone",new Vector2Int(u,v));
                for(int x=0;x<2;x++)for(int y=0;y<2;y++)zones[(u-u0)*2+x,(v-v0)*2+y]=(sbyte)zone;
            }
            var stones=(IEnumerable)Call("LayAshlar",zones,u0,v0,37);
            int selected=0,ordinaryCost=0,repairCost=0;
            var selectedCells=new bool[zones.GetLength(0),zones.GetLength(1)];
            double selectedArea=0;
            foreach(object stone in stones)
            {
                Set(stone,"I",Field(stone,"I")+u0*2);Set(stone,"J",Field(stone,"J")+v0*2);
                object mesh=Builder();bool used=(bool)Call("ServiceApronFlag",mesh,stone);
                var vertices=Data<Vector3>(mesh,"V");
                if(!used){Assert.That(vertices.Count,Is.Zero);continue;}
                selected++;
                int width=Field(stone,"W"),height=Field(stone,"H");
                float left=Field(stone,"I")*.5f,bottom=Field(stone,"J")*.5f,right=left+width*.5f,top=bottom+height*.5f;
                Assert.That(Field(stone,"Zone"),Is.EqualTo(0).Or.EqualTo(3),"Repairs belong to the court/workyard material regions only.");
                Assert.That((float)Call("ServiceApronWeight",(left+right)*.5f,(bottom+top)*.5f),Is.GreaterThan(0),
                    "A repaired parent's centre must belong to the authored working apron, including its soft shoulder.");
                selectedArea+=width*height*.25;
                int ix=Field(stone,"I")-u0*2,iy=Field(stone,"J")-v0*2;
                for(int x=ix;x<ix+width;x++)for(int y=iy;y<iy+height;y++)
                {
                    Assert.That(selectedCells[x,y],Is.False,"Two selected parents must not own the same physical half-cell.");
                    selectedCells[x,y]=true;
                }
                var uv=Data<Vector2>(mesh,"U");var colors=Data<Color>(mesh,"C");var triangles=Data<int>(mesh,"T");
                Assert.That(uv.Count,Is.EqualTo(vertices.Count));Assert.That(colors.Count,Is.EqualTo(vertices.Count));
                foreach(int index in triangles)Assert.That(index,Is.InRange(0,vertices.Count-1));
                for(int k=0;k<vertices.Count;k++)
                {
                    Vector3 p=vertices[k];
                    Assert.That(float.IsNaN(p.x)||float.IsNaN(p.y)||float.IsInfinity(p.x)||float.IsInfinity(p.y),Is.False);
                    Vector2 design=ToDesign(p);
                    Assert.That(design.x,Is.InRange(left-.00002f,right+.00002f));
                    Assert.That(design.y,Is.InRange(bottom-.00002f,top+.00002f));Assert.That(p.z,Is.Zero);
                    Vector2 expected=new Vector2(1+2*FoundryMap.Axis*design.x/FoundryRegionView.PavingGrainSpan,
                        1+2*FoundryMap.Axis*design.y/FoundryRegionView.PavingGrainSpan);
                    Assert.That(Vector2.Distance(expected,uv[k]),Is.LessThan(.00001f));
                    Assert.That(colors[k].a,Is.EqualTo(1));
                    Assert.That(float.IsNaN(colors[k].r)||float.IsNaN(colors[k].g)||float.IsNaN(colors[k].b),Is.False);
                }
                // Sample every eighth unit, including both sides of all possible
                // half-unit splits. The full original joint bed must close them.
                for(int x=0;x<width*4;x++)for(int y=0;y<height*4;y++)
                {
                    Vector2 point=Project(new Vector2(left+(x+.5f)*.125f,bottom+(y+.5f)*.125f));
                    bool covered=false;
                    for(int t=0;t<triangles.Count&&!covered;t+=3)
                        covered=Inside(point,vertices[triangles[t]],vertices[triangles[t+1]],vertices[triangles[t+2]]);
                    Assert.That(covered,Is.True,"Working stone must not leave a hole inside its original parent.");
                }
                if(Math.Max(width,height)<4||Math.Min(width,height)<2)
                {
                    object whole=Builder();Call("AshlarFlagFace",whole,stone,true);
                    CollectionAssert.AreEqual(Data<Vector3>(whole,"V"),vertices,
                        "Small parents stay whole rather than becoming dense half-unit setts.");
                }
                object ordinary=Builder();Call("AshlarFlagFace",ordinary,stone,false);
                ordinaryCost+=Data<Vector3>(ordinary,"V").Count;repairCost+=vertices.Count;
            }
            // Count measured ground area, not parent slabs: the same working field
            // legitimately uses fewer parents when the quarry stock becomes larger.
            // The lower bound is the independent area of the authored full-strength
            // stock rectangle plus the part of its receiving arm outside that box.
            float spine=-24+FoundryMap.WorkyardShift;
            double coreArea=7.2*21.15+Math.Max(0,spine-3.6-FoundryMap.West)*4.8;
            TestContext.WriteLine($"Working apron: {selected} parents, {selectedArea:F2} square units; authored core {coreArea:F2}; extra vertices {repairCost-ordinaryCost}.");
            Assert.That(selectedArea,Is.GreaterThanOrEqualTo(coreArea),
                "The full apron plus its soft shoulder must retain at least the authored working core's physical area.");
            var connected=new bool[selectedCells.GetLength(0),selectedCells.GetLength(1)];
            var pending=new Queue<Vector2Int>();
            var start=new Vector2Int(Mathf.FloorToInt((spine-u0)*2),Mathf.FloorToInt((7-v0)*2));
            Assert.That(selectedCells[start.x,start.y],Is.True,"The receiving/stock junction must actually contain repaired stone.");
            pending.Enqueue(start);connected[start.x,start.y]=true;
            var directions=new[]{Vector2Int.left,Vector2Int.right,Vector2Int.up,Vector2Int.down};
            while(pending.Count>0)
            {
                Vector2Int cell=pending.Dequeue();
                foreach(Vector2Int direction in directions)
                {
                    Vector2Int next=cell+direction;
                    if(next.x<0||next.y<0||next.x>=selectedCells.GetLength(0)||next.y>=selectedCells.GetLength(1)||
                        !selectedCells[next.x,next.y]||connected[next.x,next.y])continue;
                    connected[next.x,next.y]=true;pending.Enqueue(next);
                }
            }
            void RequireConnected(float u,float v)
            {
                int x=Mathf.FloorToInt((u-u0)*2),y=Mathf.FloorToInt((v-v0)*2);
                Assert.That(connected[x,y],Is.True,$"Actual repaired stone must connect the receiving and stock tracks at ({u},{v}).");
            }
            for(float u=FoundryMap.West+.35f;u<=spine;u+=.2f)RequireConnected(u,7);
            for(float v=-1.8f;v<=17.42f;v+=.2f)RequireConnected(spine,v);
            Assert.That(repairCost-ordinaryCost,Is.LessThanOrEqualTo(4000),"The complete apron must stay within its geometry allowance.");
        }

        [Test]
        public void FullStrengthCoreConnectsExistingReceivingAndStockRailsWithoutInvadingPitCourtOrGate()
        {
            float spine=-24+FoundryMap.WorkyardShift;
            // Actual rail contact locations from the production transfer assembly.
            for(float u=FoundryMap.West+.35f;u<=spine;u+=.2f)
                Assert.That((float)Call("ServiceApronWeight",u,7f),Is.EqualTo(1));
            for(float v=-1.8f;v<=17.42f;v+=.2f)
                Assert.That((float)Call("ServiceApronWeight",spine,v),Is.EqualTo(1));
            foreach(Vector2 p in new[]{new Vector2(FoundryMap.PitWest-1,7),Vector2.zero,
                new Vector2(12,5),new Vector2(-14,-14)})
                Assert.That((float)Call("ServiceApronWeight",p.x,p.y),Is.Zero,
                    "Pit-side movement, ceremonial court, gate and entry remain ordinary paving.");
            foreach(int zone in new[]{1,2})
            {
                object stone=Activator.CreateInstance(View.GetNestedType("AshlarStone",BindingFlags.NonPublic));
                Set(stone,"I",Mathf.RoundToInt(spine*2));Set(stone,"J",14);Set(stone,"W",2);Set(stone,"H",2);Set(stone,"Zone",zone);
                object mesh=Builder();Assert.That((bool)Call("ServiceApronFlag",mesh,stone),Is.False);
                Assert.That(Data<Vector3>(mesh,"V").Count,Is.Zero);
            }
        }

        [TestCase(-26f,-1f,false)]
        [TestCase(-24f,12f,false)]
        [TestCase(-31f,7f,true)]
        public void LoadingUseIsVisibleOnReferencedStoneFacesWithoutChangingTheirGeometryOrLightHue(float u,float v,bool scrubbed)
        {
            object stone=UnitStone(u,v),plain=Builder(),used=Builder();
            Call("AshlarFlagFace",plain,stone,true);
            Assert.That((bool)Call("ServiceApronFlag",used,stone),Is.True);
            CollectionAssert.AreEqual(Data<Vector3>(plain,"V"),Data<Vector3>(used,"V"));
            CollectionAssert.AreEqual(Data<Vector2>(plain,"U"),Data<Vector2>(used,"U"));
            CollectionAssert.AreEqual(Data<int>(plain,"T"),Data<int>(used,"T"));
            var before=Data<Color>(plain,"C");var after=Data<Color>(used,"C");
            float low=1,high=1;int changed=0;
            foreach(int i in ReferencedVertices(used))
            {
                float r=after[i].r/before[i].r,g=after[i].g/before[i].g,b=after[i].b/before[i].b;
                Assert.That(r,Is.InRange(.86f,1.04001f));
                Assert.That(g,Is.EqualTo(r).Within(.00001f));
                Assert.That(b,Is.EqualTo(r).Within(.00001f),"Local use preserves the existing forge-light RGB relationship.");
                Assert.That(after[i].a,Is.EqualTo(1));
                low=Mathf.Min(low,r);high=Mathf.Max(high,r);
                if(Mathf.Abs(r-1)>.015f)changed++;
            }
            Assert.That(changed,Is.GreaterThanOrEqualTo(3),"Evidence must live on rendered stone triangles, not a helper peak or an unused vertex.");
            if(scrubbed)Assert.That(high,Is.GreaterThan(1.025f),"Receiving wear must be sampled at the actual threshold.");
            else Assert.That(low,Is.LessThan(.96f),"Mill scale is visible beyond the real loading/stock footprint.");
        }

        [Test]
        public void AdjacentRepairParentsShareOneWorldPositionUseField()
        {
            var left=UseRatios(-26,-1);var right=UseRatios(-25,-1);
            int shared=0;bool visiblyUsed=false;
            foreach(var sample in left)
            {
                if(!right.TryGetValue(sample.Key,out float other))continue;
                Assert.That(sample.Value,Is.EqualTo(other).Within(.00001f),
                    "A parent's split or new mineral identity must not restart the use field at its joint.");
                visiblyUsed|=sample.Value<.96f;shared++;
            }
            Assert.That(shared,Is.GreaterThanOrEqualTo(2));
            Assert.That(visiblyUsed,Is.True,"Check continuity across an actual used contact, not two unaffected stones.");
        }

        [Test]
        public void UnusedServiceStoneAndOpenTransferCentreRemainExactlyUnchanged()
        {
            foreach(Vector2 origin in new[]{new Vector2(-27,3),new Vector2(-25,7)})
            {
                object stone=UnitStone(origin.x,origin.y),plain=Builder(),used=Builder();
                Call("AshlarFlagFace",plain,stone,true);
                Assert.That((bool)Call("ServiceApronFlag",used,stone),Is.True);
                CollectionAssert.AreEqual(Data<Color>(plain,"C"),Data<Color>(used,"C"),
                    "Local mill scale must fade to exact neutral, leaving broad unused paving and the open turntable clear.");
            }
        }

        private static object UnitStone(float u,float v)
        {
            object stone=Activator.CreateInstance(View.GetNestedType("AshlarStone",BindingFlags.NonPublic));
            Set(stone,"I",Mathf.RoundToInt(u*2));Set(stone,"J",Mathf.RoundToInt(v*2));
            Set(stone,"W",2);Set(stone,"H",2);Set(stone,"Zone",3);return stone;
        }

        private static HashSet<int> ReferencedVertices(object mesh)
        {
            var vertices=Data<Vector3>(mesh,"V");var triangles=Data<int>(mesh,"T");
            var used=new HashSet<int>();
            for(int t=0;t<triangles.Count;t+=3)
            {
                int a=triangles[t],b=triangles[t+1],c=triangles[t+2];
                if(Math.Abs(Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]))<=1e-8)continue;
                used.Add(a);used.Add(b);used.Add(c);
            }
            return used;
        }

        private static Dictionary<Vector2Int,float> UseRatios(float u,float v)
        {
            object stone=UnitStone(u,v),plain=Builder(),used=Builder();
            Call("AshlarFlagFace",plain,stone,true);Call("ServiceApronFlag",used,stone);
            var positions=Data<Vector3>(used,"V");var before=Data<Color>(plain,"C");var after=Data<Color>(used,"C");
            var ratios=new Dictionary<Vector2Int,float>();
            foreach(int i in ReferencedVertices(used))
            {
                Vector2 design=ToDesign(positions[i]);
                var key=new Vector2Int(Mathf.RoundToInt(design.x*10000),Mathf.RoundToInt(design.y*10000));
                ratios[key]=after[i].r/before[i].r;
            }
            return ratios;
        }

        private static Vector2 ToDesign(Vector2 p)=>new Vector2((p.x+2*p.y)*FoundryMap.Axis,(-p.x+2*p.y)*FoundryMap.Axis);
        private static Vector2 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static double Cross(Vector2 a,Vector2 b)=>(double)a.x*b.y-(double)a.y*b.x;
        private static bool Inside(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            double area=Cross(b-a,c-a);if(Math.Abs(area)<1e-10)return false;
            double x=Cross(b-p,c-p)/area,y=Cross(c-p,a-p)/area,z=1-x-y;
            return x>=-1e-6&&y>=-1e-6&&z>=-1e-6;
        }
    }
}
