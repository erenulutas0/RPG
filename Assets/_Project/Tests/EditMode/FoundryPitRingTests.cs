using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryPitRingTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic|BindingFlags.Public;
        private const BindingFlags Fields=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static readonly Vector2 Centre=new Vector2(-10,8);

        [Test]
        public void CollarBandIsACircleTheCameraSeesAsTheConceptOval()
        {
            float rim=Constant("PitRim"),opening=Constant("PitOpening"),quarter=Constant("PitQuarter");
            Assert.That(opening,Is.EqualTo(5.45f).Within(.0001f),"The band's inner edge keeps the old collar line at the mid posts.");
            float left=float.MaxValue,right=float.MinValue,low=float.MaxValue,high=float.MinValue;
            for(int i=0;i<720;i++)
            {
                float t=quarter*4*i/720f;
                Vector2 outer=Ring(t,0),inner=Ring(t,rim-opening);
                Assert.That(Vector2.Distance(outer,Centre),Is.EqualTo(rim).Within(.0001f));
                Assert.That(Vector2.Distance(inner,Centre),Is.EqualTo(opening).Within(.0001f));
                Vector3 p=Project(outer);
                left=Mathf.Min(left,p.x);right=Mathf.Max(right,p.x);low=Mathf.Min(low,p.y);high=Mathf.Max(high,p.y);
            }
            Assert.That((right-left)/(high-low),Is.EqualTo(2).Within(.01f),"On screen the ring is the concept's 2:1 oval.");
            // The walkable square is unchanged: the ring only rounds what is drawn inside the hole.
            Assert.That(new[]{FoundryMap.PitWest,FoundryMap.PitSouth,FoundryMap.PitEast,FoundryMap.PitNorth},Is.EqualTo(new[]{-16,2,-4,14}));
        }

        [Test]
        public void CornerPostsStraddleTheBandAndStopShortOfTheirPedestals()
        {
            var posts=(Vector4[])Call("PitPosts");
            float opening=Constant("PitOpening"),pedestal=Constant("PitRim")+Constant("PitPedestalReach");
            for(int k=1;k<8;k+=2)
            {
                var corners=Corners(posts[k]);float nearest=float.MaxValue,farthest=0;
                foreach(Vector2 c in corners){float r=Vector2.Distance(c,Centre);nearest=Mathf.Min(nearest,r);farthest=Mathf.Max(farthest,r);}
                Assert.That(nearest,Is.InRange(opening-.011f,opening),$"Corner post {k} overhangs the pit by at most .01.");
                Assert.That(farthest,Is.LessThan(pedestal),$"Corner post {k} stands on the collar, short of the pedestal.");
            }
        }

        [Test]
        public void CornerPedestalsRaiseTheSquaresCornersAndCarryTheirLanterns()
        {
            float half=Constant("PitPedestalHalf"),top=Constant("PitPedestalTop"),lantern=Constant("PitLantern");
            float pedestal=Constant("PitRim")+Constant("PitPedestalReach");
            Assert.That(top,Is.GreaterThan(.15f+.2f),"The pedestals stand well above the collar's plinth course.");
            int vertices=0;
            for(int corner=0;corner<4;corner++)
            {
                float diagonal=Mathf.PI*.25f+corner*Mathf.PI*.5f;
                var boxes=new List<Vector4>();
                foreach(float from in new[]{diagonal-half,diagonal})
                {
                    object b=Builder();
                    var box=(Vector4)Call("PitPedestal",b,from,from+half);
                    var points=Field<List<Vector3>>(b,"V");vertices+=points.Count;
                    Assert.That(points.Count,Is.GreaterThan(0));
                    Assert.That(Field<List<Vector2>>(b,"U").Count,Is.EqualTo(points.Count));
                    Assert.That(Field<List<Color>>(b,"C").Count,Is.EqualTo(points.Count));
                    Assert.That(box.x,Is.GreaterThanOrEqualTo(FoundryMap.PitWest-.001f));Assert.That(box.z,Is.LessThanOrEqualTo(FoundryMap.PitEast+.001f));
                    Assert.That(box.y,Is.GreaterThanOrEqualTo(FoundryMap.PitSouth-.001f));Assert.That(box.w,Is.LessThanOrEqualTo(FoundryMap.PitNorth+.001f));
                    boxes.Add(box);
                }
                Vector2 compass=new Vector2(Mathf.Sign(Mathf.Cos(diagonal)),Mathf.Sign(Mathf.Sin(diagonal)));
                Vector2 tip=Centre+compass*6;
                Assert.That(boxes.Exists(b=>Holds(b,tip)),Is.True,"The pedestal reaches the hole's corner.");
                // Its lantern stands wholly on it: outside the pedestal's foot round the ring, within its span either side
                // of the diagonal, inside the hole.
                Vector2 c0=Centre+compass*(6-lantern);
                foreach(Vector2 c in Corners(new Vector4(c0.x-.45f,c0.y-.45f,c0.x+.45f,c0.y+.45f)))
                {
                    Vector2 w=c-Centre;
                    Assert.That(w.magnitude,Is.GreaterThan(pedestal+.1f),"The lantern clears the pedestal's inner edge.");
                    Assert.That(Mathf.Abs(Mathf.DeltaAngle(Mathf.Atan2(w.y,w.x)*Mathf.Rad2Deg,diagonal*Mathf.Rad2Deg)),Is.LessThan(half*Mathf.Rad2Deg));
                    Assert.That(Mathf.Max(Mathf.Abs(w.x),Mathf.Abs(w.y)),Is.LessThan(6-.3f),"The lantern stands inside the hole's edge.");
                }
            }
            Assert.That(vertices,Is.LessThanOrEqualTo(600),"Eight pedestal halves keep a small fixed mesh budget.");
        }

        [Test]
        public void CollarStoneUnderThePedestalsStopsAtTheirFoot()
        {
            float half=Constant("PitPedestalHalf");
            for(int corner=0;corner<4;corner++)
            {
                float diagonal=Mathf.PI*.25f+corner*Mathf.PI*.5f;
                Assert.That((bool)Call("PitUnderPedestal",diagonal),Is.True);
                Assert.That((bool)Call("PitUnderPedestal",diagonal+half*.97f),Is.True);
                Assert.That((bool)Call("PitUnderPedestal",diagonal+half*1.03f),Is.False);
                Assert.That((bool)Call("PitUnderPedestal",diagonal-half*1.03f),Is.False);
                Assert.That((bool)Call("PitUnderPedestal",corner*Mathf.PI*.5f),Is.False,"The sides' middles keep the open collar.");
            }
        }

        private static bool Holds(Vector4 b,Vector2 p)=>p.x>=b.x-.001f&&p.x<=b.z+.001f&&p.y>=b.y-.001f&&p.y<=b.w+.001f;
        private static Vector2[] Corners(Vector4 b)=>new[]{new Vector2(b.x,b.y),new Vector2(b.z,b.y),new Vector2(b.z,b.w),new Vector2(b.x,b.w)};
        private static Vector2 Ring(float t,float inset)
        {
            object[] args={t,inset,Vector2.zero};
            return (Vector2)typeof(FoundryRegionView).GetMethod("PitRing",Static).Invoke(null,args);
        }
        private static float Constant(string name)=>(float)typeof(FoundryRegionView).GetField(name,Static).GetRawConstantValue();
        private static Vector3 Project(Vector2 p)=>(Vector3)Call("Project",p.x,p.y);
        private static object Builder()=>Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic),true);
        private static T Field<T>(object b,string field)=>(T)b.GetType().GetField(field,Fields).GetValue(b);
        private static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
