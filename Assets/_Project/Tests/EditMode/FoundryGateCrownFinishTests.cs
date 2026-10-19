using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateCrownFinishTests
    {
        const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        const string Atlas="Assets/_Project/Art/Props/Foundry/FoundryMasonryV4.png";
        static readonly Vector4 FormerPatch=new Vector4(.04f,.44f,.21f,.564f);

        [Test]
        public void SharedGateFieldSamplesALevelInteriorClearOfTheDrip()
        {
            // The source atlas itself is the oracle: the mirrored patch must not
            // carry a brightness ramp or a drip column across the long crown.
            var texture=new Texture2D(2,2);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(Atlas)),Is.True);
                Vector4 patch=(Vector4)typeof(FoundryRegionView).GetField("GateHousePatch",Static).GetValue(null);
                Measure(texture,patch,out float range,out float darkest,out float blocks);
                Measure(texture,FormerPatch,out float formerRange,out _,out float formerBlocks);
                TestContext.WriteLine($"Gate patch column range {range:F2} (former {formerRange:F2}); darkest column {darkest:F3}; 16-texel block spread {blocks:F2} (former {formerBlocks:F2}).");
                Assert.That(range,Is.LessThan(6.5f),"Column means stay level, so a mirrored repeat prints no band.");
                Assert.That(darkest,Is.GreaterThan(.95f),"No drip or mortar column enters the sampled interior.");
                Assert.That(blocks,Is.LessThan(4f),"Broad cloud contrast stays below the former arris-lit patch.");
                Assert.That(formerRange,Is.GreaterThan(range*1.5f),"The replacement must be measurably more level than the former patch.");
                Assert.That(patch.z-patch.x,Is.GreaterThan(.15f),"A narrower patch would multiply mirror folds and vertices.");
                Assert.That(patch.y,Is.EqualTo(FormerPatch.y));Assert.That(patch.w,Is.EqualTo(FormerPatch.w));
            }
            finally{Object.DestroyImmediate(texture);}
        }

        [Test]
        public void GateAlbedoBalancesTowardSlateWithoutChangingValueOrRelativeLight()
        {
            var samples=new[]{new Color(.53f,.57f,.65f),new Color(.35f,.39f,.48f),new Color(.67f,.70f,.77f),new Color(.72f,.52f,.36f)};
            foreach(Color source in samples)
            {
                Color balanced=Albedo(source);
                float before=(source.r+source.g+source.b)/3,after=(balanced.r+balanced.g+balanced.b)/3;
                Assert.That(after/before,Is.InRange(.985f,1.015f),"The balance is a hue correction, not a brighter or darker crown.");
                Assert.That(Luma(balanced)/Luma(source),Is.InRange(.98f,1.025f),"Perceived value stays within the measured crown range.");
                Assert.That(balanced.b/balanced.r,Is.LessThan(source.b/source.r),"Native gate stone moves away from violet.");
                Assert.That(balanced.g/balanced.r,Is.GreaterThan(source.g/source.r),"Toward the baked bays' slate green-grey.");
                Assert.That(balanced.a,Is.EqualTo(source.a));
                Color dim=Albedo(source*.5f);
                Assert.That(dim.r,Is.EqualTo(balanced.r*.5f).Within(1e-6f));
                Assert.That(dim.g,Is.EqualTo(balanced.g*.5f).Within(1e-6f));
                Assert.That(dim.b,Is.EqualTo(balanced.b*.5f).Within(1e-6f),"A constant factor keeps every relative forge-light value.");
            }
        }

        [Test]
        public void InnerPierFamilyKeepsItsOwnFieldAndSourceLight()
        {
            // Ownership is unchanged: only the shared header/return/cartouche/curtain
            // field moves; the accepted .11 pier finish is untouched by this study.
            object pier=Builder();Call("BuildGateInnerPier",pier,-3f);
            var light=new List<Color>(Data<Color>(pier,"C"));
            Call("GateInnerPierGrain",pier);
            var colours=Data<Color>(pier,"C");var uv=Data<Vector2>(pier,"U");
            for(int i=0;i<light.Count;i++)Assert.That(colours[i],Is.EqualTo(light[i]));
            foreach(Vector2 p in uv)
            {
                Assert.That(p.x,Is.InRange(FormerPatch.x-.00001f,FormerPatch.z+.00001f));
                Assert.That(p.y,Is.InRange(FormerPatch.y-.00001f,FormerPatch.w+.00001f));
            }
        }

        static void Measure(Texture2D texture,Vector4 patch,out float range,out float darkest,out float blocks)
        {
            int x0=(int)(patch.x*texture.width),x1=(int)(patch.z*texture.width);
            int y0=(int)(patch.y*texture.height),y1=(int)(patch.w*texture.height);
            Color[] pixels=texture.GetPixels(x0,y0,x1-x0,y1-y0);int w=x1-x0,h=y1-y0;
            float Lum(int x,int y)=>Luma(pixels[y*w+x])*255;
            double total=0;for(int y=0;y<h;y++)for(int x=0;x<w;x++)total+=Lum(x,y);
            float mean=(float)(total/(w*h)),lo=float.MaxValue,hi=float.MinValue;
            for(int c=0;c+8<=w;c+=8)
            {
                double sum=0;for(int y=0;y<h;y++)for(int x=c;x<c+8;x++)sum+=Lum(x,y);
                float column=(float)(sum/(8*h));lo=Mathf.Min(lo,column);hi=Mathf.Max(hi,column);
            }
            range=hi-lo;darkest=lo/mean;
            var means=new List<double>();
            for(int by=0;by+16<=h;by+=16)for(int bx=0;bx+16<=w;bx+=16)
            {
                double sum=0;for(int y=by;y<by+16;y++)for(int x=bx;x<bx+16;x++)sum+=Lum(x,y);
                means.Add(sum/256);
            }
            double average=0;foreach(double m in means)average+=m;average/=means.Count;
            double spread=0;foreach(double m in means)spread+=(m-average)*(m-average);
            blocks=(float)System.Math.Sqrt(spread/means.Count);
        }

        static float Luma(Color c)=>.299f*c.r+.587f*c.g+.114f*c.b;
        static Color Albedo(Color c)=>(Color)Call("GateHouseAlbedo",c);
        static object Builder()=>System.Activator.CreateInstance(typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic));
        static List<T> Data<T>(object b,string name)=>(List<T>)b.GetType().GetField(name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).GetValue(b);
        static object Call(string name,params object[] args)=>typeof(FoundryRegionView).GetMethod(name,Static).Invoke(null,args);
    }
}
