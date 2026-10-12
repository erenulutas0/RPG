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
            foreach(object stone in stones)
            {
                Set(stone,"I",Field(stone,"I")+u0*2);Set(stone,"J",Field(stone,"J")+v0*2);
                object mesh=Builder();bool used=(bool)Call("ServiceApronFlag",mesh,stone);
                var vertices=Data<Vector3>(mesh,"V");
                if(!used){Assert.That(vertices.Count,Is.Zero);continue;}
                selected++;
                int width=Field(stone,"W"),height=Field(stone,"H");
                float left=Field(stone,"I")*.5f,bottom=Field(stone,"J")*.5f,right=left+width*.5f,top=bottom+height*.5f;
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
            Assert.That(selected,Is.GreaterThan(80),"This is one connected working field, not two tiny repair spots.");
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
