using System;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryRimLightTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private static readonly Vector2[] Added={new Vector2(-7.5f,FoundryMap.South-.36f),new Vector2(6.5f,FoundryMap.South-.36f),new Vector2(1.5f,FoundryMap.North+.36f),
            new Vector2(-2f,FoundryMap.North+.36f),new Vector2(6f,FoundryMap.North+.36f),new Vector2(18.5f,10.36f)};

        [Test]
        public void ConceptRimLanternsStandOnTheRailWithTheirOwnFloorPools()
        {
            var boxes=(Vector4[])typeof(FoundryRegionView).GetField("RailLanterns",Static).GetValue(null);
            var front=(bool[])typeof(FoundryRegionView).GetField("RailLanternFront",Static).GetValue(null);
            Assert.That(front.Length,Is.EqualTo(boxes.Length),"Every rail lantern declares whether it faces the camera.");
            var lamps=(Array)typeof(FoundryRegionView).GetMethod("ForgeLamps",Static).Invoke(null,null);
            Type lampType=lamps.GetType().GetElementType();
            FieldInfo boxField=lampType.GetField("Box",BindingFlags.Instance|BindingFlags.NonPublic);
            FieldInfo strengthField=lampType.GetField("Strength",BindingFlags.Instance|BindingFlags.NonPublic);
            var centres=new System.Collections.Generic.List<(Vector2 c,float s)>();
            foreach(object lamp in lamps)
            {
                var b=(Vector4)boxField.GetValue(lamp);
                centres.Add((new Vector2((b.x+b.z)*.5f,(b.y+b.w)*.5f),(float)strengthField.GetValue(lamp)));
            }
            foreach(Vector2 p in Added)
            {
                int index=Array.FindIndex(boxes,b=>Mathf.Abs((b.x+b.z)*.5f-p.x)<.01f&&Mathf.Abs((b.y+b.w)*.5f-p.y)<.01f);
                Assert.That(index,Is.GreaterThanOrEqualTo(0),$"Rail lantern at {p} is declared.");
                Assert.That(front[index],Is.EqualTo(p.y<0),"South rail posts carry a pilaster down the camera-facing wall.");
                // A pool exists only when the post passes BuildBalustrade's own placement test.
                Assert.That(centres.Exists(l=>Vector2.Distance(l.c,p)<.01f&&Mathf.Approximately(l.s,.45f)),Is.True,$"Lantern at {p} is placed and lights the floor.");
                FoundryMap.ToFloor(p.x,p.y,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,"The post stands on the rail, outside the walking surface.");
                float nearest=float.MaxValue;
                foreach(var l in centres)if(Vector2.Distance(l.c,p)>.01f&&l.s<=.5f)nearest=Mathf.Min(nearest,Vector2.Distance(l.c,p));
                Assert.That(nearest,Is.GreaterThan(2.9f),$"Lantern at {p} fills a bare run instead of doubling a neighbour's pool.");
            }
        }
    }
}
