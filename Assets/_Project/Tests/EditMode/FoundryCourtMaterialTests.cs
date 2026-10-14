using System;
using System.Collections.Generic;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCourtMaterialTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        private static Type View=>typeof(FoundryRegionView);
        private static Type BuilderType=>View.GetNestedType("Builder",BindingFlags.NonPublic);
        private static object Builder()=>Activator.CreateInstance(BuilderType,true);
        private static object Call(string name,params object[] args)=>View.GetMethod(name,Static).Invoke(null,args);
        private static List<T> Data<T>(object mesh,string name)=>(List<T>)BuilderType.GetField(name,Members).GetValue(mesh);
        private static readonly Color SourceTint=new Color(.93f,1.025f,.97f);
        private const float SourceTone=.94f;

        [TestCase(-18f,-18f,1)]
        [TestCase(-25f,7f,-1)]
        [TestCase(-3f,-5f,0)]
        [TestCase(18f,5f,0)]
        public void ReferencedFloorReceivesOneBoundedMaterialGainWithoutMovingItsGeometryGrainOrLightHue(float u,float v,int use)
        {
            object mesh=Builder();float[] us={u,u+.5f,u+1},vs={v,v+.5f,v+1};
            Call("PavingGrid",mesh,us,vs,SourceTone,SourceTint,null);
            var vertices=Data<Vector3>(mesh,"V");var uv=Data<Vector2>(mesh,"U");var triangles=Data<int>(mesh,"T");
            Assert.That(vertices.Count,Is.EqualTo(9));Assert.That(triangles.Count,Is.EqualTo(24));
            Assert.That(new HashSet<int>(triangles).Count,Is.EqualTo(vertices.Count),"Every checked sample belongs to a rendered triangle.");
            for(int y=0;y<vs.Length;y++)for(int x=0;x<us.Length;x++)
            {
                int i=y*us.Length+x;float a=us[x],b=vs[y];
                Assert.That(vertices[i],Is.EqualTo((Vector3)Call("Project",a,b)));
                Assert.That(uv[i],Is.EqualTo(new Vector2(1+2*FoundryMap.Axis*a/FoundryRegionView.PavingGrainSpan,
                    1+2*FoundryMap.Axis*b/FoundryRegionView.PavingGrainSpan)));
                float gain=Gain(mesh,i,a,b,SourceTone,SourceTint);
                Assert.That(gain,Is.InRange(.88999f,1.08501f));
                if(use>0)Assert.That(gain,Is.GreaterThan(1.06f),"The actual entry stone must show broad scrubbed use.");
                else if(use<0)Assert.That(gain,Is.LessThan(.92f),"The actual working apron must show settled material use.");
                else Assert.That(gain,Is.EqualTo(1).Within(.000001f),"The fighting centre and raised gate finish stay neutral.");
            }
        }

        [Test]
        public void FittedAndOrdinaryStoneShareTheSameUseAcrossARealNonneutralJoint()
        {
            object ordinary=Builder(),fitted=Builder();
            Call("PavingGrid",ordinary,new[]{-19f,-18.5f,-18f},new[]{-14f,-13.5f,-13f},SourceTone,SourceTint,null);
            Call("CourtFittedFace",fitted,new[]{new Vector2(-18,-14),new Vector2(-17,-14),
                new Vector2(-17,-13),new Vector2(-18,-13)},SourceTone,SourceTint);
            var ordinaryV=Data<Vector3>(ordinary,"V");var fittedV=Data<Vector3>(fitted,"V");
            var ordinaryC=Data<Color>(ordinary,"C");var fittedC=Data<Color>(fitted,"C");
            var ordinaryUsed=new HashSet<int>(Data<int>(ordinary,"T"));var fittedUsed=new HashSet<int>(Data<int>(fitted,"T"));
            int shared=0;
            foreach(int a in ordinaryUsed)foreach(int b in fittedUsed)
            {
                if(ordinaryV[a]!=fittedV[b])continue;
                Assert.That(fittedC[b],Is.EqualTo(ordinaryC[a]),"Changing the stone emitter must not restart or double the use field.");
                Vector2 point=Design(ordinary,a);
                Assert.That(Gain(ordinary,a,point.x,point.y,SourceTone,SourceTint),Is.GreaterThan(1.02f));shared++;
            }
            Assert.That(shared,Is.EqualTo(3),"Check the shared half-unit samples on an actually rendered boundary.");
            Assert.That(fittedV.Count,Is.EqualTo(9));Assert.That(Data<int>(fitted,"T").Count,Is.EqualTo(24));
        }

        [Test]
        public void ActualHalfUnitFloorSamplesFormBroadZonesWithAQuietCentralField()
        {
            float[] us=Lines(-32,12,.5f),vs=Lines(-30,18,.5f);object mesh=Builder();
            Call("PavingGrid",mesh,us,vs,SourceTone,SourceTint,null);
            var gains=new float[us.Length,vs.Length];int entry=0,work=0,neutralCentre=0;
            Assert.That(Data<Vector3>(mesh,"V").Count,Is.EqualTo(us.Length*vs.Length));
            Assert.That(Data<int>(mesh,"T").Count,Is.EqualTo((us.Length-1)*(vs.Length-1)*6));
            for(int y=0;y<vs.Length;y++)for(int x=0;x<us.Length;x++)
            {
                float gain=Gain(mesh,y*us.Length+x,us[x],vs[y],SourceTone,SourceTint);gains[x,y]=gain;
                Assert.That(gain,Is.InRange(.88999f,1.08501f));
                if(x>0)Assert.That(Mathf.Abs(gain-gains[x-1,y]),Is.LessThan(.0275f),"A half-unit joint cannot restart a dark/light tile pattern.");
                if(y>0)Assert.That(Mathf.Abs(gain-gains[x,y-1]),Is.LessThan(.0275f));
                FoundryMap.ToFloor(us[x],vs[y],out float fx,out float fy);
                if(!FoundryMap.SurfaceContains(fx,fy,0))continue;
                if(gain>1.04f)entry++;if(gain<.95f)work++;
                if(Vector2.Distance(new Vector2(us[x],vs[y]),new Vector2(-3,-5))<=6.99f)
                {
                    Assert.That(gain,Is.EqualTo(1).Within(.000001f));neutralCentre++;
                }
            }
            Assert.That(entry,Is.GreaterThan(350),"The entry treatment must occupy many laid stones, not one isolated mask peak.");
            Assert.That(work,Is.GreaterThan(350),"The work treatment must describe the broad apron.");
            Assert.That(neutralCentre,Is.GreaterThan(400),"Protect a real combat-sized field around the seal.");
        }

        [Test]
        public void ActualRepairStoneRetainsItsExistingLocalUseOnTopOfTheSharedApronField()
        {
            Type type=View.GetNestedType("AshlarStone",BindingFlags.NonPublic);object stone=Activator.CreateInstance(type);
            void Set(string name,int value)=>type.GetField(name,Members).SetValue(stone,value);
            Set("I",-50);Set("J",14);Set("W",2);Set("H",2);Set("Zone",3);
            object plain=Builder(),used=Builder();Call("AshlarFlagFace",plain,stone,true);
            Assert.That((bool)Call("ServiceApronFlag",used,stone),Is.True);
            CollectionAssert.AreEqual(Data<Vector3>(plain,"V"),Data<Vector3>(used,"V"));
            CollectionAssert.AreEqual(Data<Vector2>(plain,"U"),Data<Vector2>(used,"U"));
            CollectionAssert.AreEqual(Data<int>(plain,"T"),Data<int>(used,"T"));
            // The final two triangles belong to the existing repair's inset face.
            // Their unmodified stone recipe is independent of the new use field.
            float grain=(float)Call("FloorPatina",-49*.08f+5.7f,15*.08f-9.4f);
            float tone=Mathf.Lerp(.965f,1.015f,grain);Color tint=new Color(.87f,.91f,.965f);
            var triangles=Data<int>(plain,"T");var checkedVertices=new HashSet<int>();
            for(int t=triangles.Count-6;t<triangles.Count;t++)checkedVertices.Add(triangles[t]);
            Assert.That(checkedVertices.Count,Is.GreaterThanOrEqualTo(4));
            foreach(int i in checkedVertices)
            {
                Vector2 p=Design(plain,i);float gain=Gain(plain,i,p.x,p.y,tone,tint);
                Assert.That(gain,Is.EqualTo(.89f).Within(.00002f),"The emitted repair stone receives the same apron field exactly once.");
            }
            // This open transfer location has no earlier local loading stain;
            // that authored neutrality remains in the service overlay itself.
            CollectionAssert.AreEqual(Data<Color>(plain,"C"),Data<Color>(used,"C"));
        }

        [TestCase(-25f,-1.5f,true)]
        [TestCase(-24f,18.25f,true)]
        [TestCase(-3f,-5f,false)]
        [TestCase(18f,5f,false)]
        [TestCase(-18f,-18f,false)]
        public void ActualPavingSamplesReceiveContactAlbedoOnceAndPreserveProtectedPlaces(float u,float v,bool contact)
        {
            object mesh=Builder();float[] us={u,u+.02f},vs={v,v+.02f};
            Call("PavingGrid",mesh,us,vs,SourceTone,SourceTint,null);
            Assert.That(Data<Vector3>(mesh,"V").Count,Is.EqualTo(4));
            CollectionAssert.AreEqual(new[]{0,1,3,0,3,2},Data<int>(mesh,"T"));
            Color expected=contact?new Color(.84f,.90f,.94f,1):Color.white;
            for(int y=0;y<2;y++)for(int x=0;x<2;x++)
            {
                int i=y*2+x;Color actual=ContactAlbedo(mesh,i,us[x],vs[y]);
                Assert.That(actual.r,Is.EqualTo(expected.r).Within(.000002f));
                Assert.That(actual.g,Is.EqualTo(expected.g).Within(.000002f));
                Assert.That(actual.b,Is.EqualTo(expected.b).Within(.000002f));
                Assert.That(Data<Vector3>(mesh,"V")[i],Is.EqualTo((Vector3)Call("Project",us[x],vs[y])));
                Assert.That(Design(mesh,i).x,Is.EqualTo(us[x]).Within(.00001f));
                Assert.That(Design(mesh,i).y,Is.EqualTo(vs[y]).Within(.00001f));
            }
        }

        [Test]
        public void WorkyardContactAlbedoHasBoundedSoftShouldersAndAnUnchangedFightingCentre()
        {
            float[] us=Lines(-32,12,.5f),vs=Lines(-18,19,.5f);object mesh=Builder();
            Call("PavingGrid",mesh,us,vs,SourceTone,SourceTint,null);
            var samples=new Color[us.Length,vs.Length];int changed=0,quiet=0;
            for(int y=0;y<vs.Length;y++)for(int x=0;x<us.Length;x++)
            {
                Color a=ContactAlbedo(mesh,y*us.Length+x,us[x],vs[y]);samples[x,y]=a;
                Assert.That(a.r,Is.InRange(.83999f,1.00001f));
                Assert.That(a.g,Is.InRange(.89999f,1.00001f));
                Assert.That(a.b,Is.InRange(.93999f,1.00001f));
                Assert.That(a.r,Is.LessThanOrEqualTo(a.g+.000002f));
                Assert.That(a.g,Is.LessThanOrEqualTo(a.b+.000002f));
                if(x>0)Assert.That(Mathf.Abs(a.r-samples[x-1,y].r),Is.LessThan(.0301f));
                if(y>0)Assert.That(Mathf.Abs(a.r-samples[x,y-1].r),Is.LessThan(.0301f));
                FoundryMap.ToFloor(us[x],vs[y],out float fx,out float fy);
                if(!FoundryMap.SurfaceContains(fx,fy,0))continue;
                if(a.r<.94f)changed++;
                if(Vector2.Distance(new Vector2(us[x],vs[y]),new Vector2(-3,-5))<=7)
                {Assert.That(a.r,Is.EqualTo(1).Within(.000002f));quiet++;}
            }
            Assert.That(changed,Is.GreaterThan(160),"Use must reach real walkable apron faces, not only hidden equipment centres.");
            Assert.That(quiet,Is.GreaterThan(400));
        }

        [Test]
        public void OrdinaryAndFittedPavingShareContactColourAcrossTheStockApronBoundary()
        {
            object ordinary=Builder(),fitted=Builder();
            Call("PavingGrid",ordinary,new[]{-25f,-24.5f,-24f},new[]{16f,16.5f,17f},SourceTone,SourceTint,null);
            Call("CourtFittedFace",fitted,new[]{new Vector2(-24,16),new Vector2(-23,16),
                new Vector2(-23,17),new Vector2(-24,17)},SourceTone,SourceTint);
            int shared=0;
            foreach(int a in new HashSet<int>(Data<int>(ordinary,"T")))
                foreach(int b in new HashSet<int>(Data<int>(fitted,"T")))
                {
                    if(Data<Vector3>(ordinary,"V")[a]!=Data<Vector3>(fitted,"V")[b])continue;
                    Assert.That(Data<Color>(ordinary,"C")[a],Is.EqualTo(Data<Color>(fitted,"C")[b]));
                    Vector2 p=Design(ordinary,a);Assert.That(ContactAlbedo(ordinary,a,p.x,p.y).r,Is.LessThan(.95f));shared++;
                }
            Assert.That(shared,Is.EqualTo(3));
        }

        [Test]
        public void FloorSpillCanBeTightenedWithoutChangingWallOrIronIllumination()
        {
            FieldInfo cache=View.GetField("_forgeLamps",Static);
            object saved=cache.GetValue(null);
            object lamp=Call("Lamp",new Vector4(0,0,0,0),0f,6f,1f,new Color(.66f,.38f,.03f,0),1f,4f);
            Array fixture=Array.CreateInstance(lamp.GetType(),1);fixture.SetValue(lamp,0);
            try
            {
                cache.SetValue(null,fixture);
                Color floor=(Color)Call("FloorLight",2f,0f);
                Color wall=(Color)Call("ArchitectureLight",2f,0f,1f,1f);
                Color iron=(Color)Call("MetalLight",2f,0f,1f);
                lamp.GetType().GetField("FloorRadius",Members).SetValue(lamp,.8f);
                fixture.SetValue(lamp,0);
                Color tighter=(Color)Call("FloorLight",2f,0f);
                Assert.That(floor.r-floor.b,Is.GreaterThan(tighter.r-tighter.b+.2f),
                    "The real floor sampler must respond to its own source reach.");
                Assert.That((Color)Call("ArchitectureLight",2f,0f,1f,1f),Is.EqualTo(wall));
                Assert.That((Color)Call("MetalLight",2f,0f,1f),Is.EqualTo(iron));
            }
            finally { cache.SetValue(null,saved); }
        }

        private static Color ContactAlbedo(object mesh,int index,float u,float v)
        {
            // Recover the new field from emitted colour using only its prior
            // light/scalar/source recipe. Never call PavingWorkshopTint here:
            // missing, doubled or incorrectly applied tint must fail the tests.
            Color actual=Data<Color>(mesh,"C")[index],light=(Color)Call("FloorLight",u,v);
            float use=(float)Call("PavingCourtUse",u,v);
            Assert.That(actual.a,Is.EqualTo(1));
            return new Color(actual.r/(light.r*use*SourceTone*SourceTint.r),
                actual.g/(light.g*use*SourceTone*SourceTint.g),actual.b/(light.b*use*SourceTone*SourceTint.b),1);
        }

        private static float[] Lines(float start,float end,float step)
        {
            var result=new float[Mathf.RoundToInt((end-start)/step)+1];
            for(int i=0;i<result.Length;i++)result[i]=start+i*step;return result;
        }

        private static Vector2 Design(object mesh,int index)=>(Data<Vector2>(mesh,"U")[index]-Vector2.one)*
            (FoundryRegionView.PavingGrainSpan/(2*FoundryMap.Axis));

        private static float Gain(object mesh,int index,float u,float v,float tone,Color tint)
        {
            Color actual=Data<Color>(mesh,"C")[index],light=(Color)Call("FloorLight",u,v);
            // These legacy cases isolate the broad scalar use, while separate
            // emission cases verify the equipment-contact albedo exactly once.
            Color contact=(Color)Call("PavingWorkshopTint",u,v);
            float r=actual.r/(light.r*tone*tint.r*contact.r),g=actual.g/(light.g*tone*tint.g*contact.g),b=actual.b/(light.b*tone*tint.b*contact.b);
            Assert.That(g,Is.EqualTo(r).Within(.00002f));Assert.That(b,Is.EqualTo(r).Within(.00002f),
                "Material use preserves the original forge-light channel relationship.");
            Assert.That(actual.a,Is.EqualTo(1));return r;
        }
    }
}
