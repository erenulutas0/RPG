using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Cryptforge.Tests
{
    public sealed class FoundryCourtFlagstoneTests
    {
        private const BindingFlags Static=BindingFlags.Static|BindingFlags.NonPublic;
        private const BindingFlags Members=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public;
        private const string Raster="Assets/_Project/Art/Props/Foundry/FoundryPavingV9.png";
        private static Type BuilderType=>typeof(FoundryRegionView).GetNestedType("Builder",BindingFlags.NonPublic);
        private static object NewBuilder()=>Activator.CreateInstance(BuilderType,true);
        private static List<T> Data<T>(object builder,string field)=>(List<T>)BuilderType.GetField(field,Members).GetValue(builder);
        private static object Invoke(string method,params object[] arguments)=>
            typeof(FoundryRegionView).GetMethod(method,Static).Invoke(null,arguments);
        private static Type Surface=>typeof(FoundryRegionView).Assembly.GetType("Cryptforge.UI.FoundrySurface");
        private static Vector3 Project(float u,float v)=>(Vector3)Surface.GetMethod("Project",Static).Invoke(null,new object[]{u,v});
        private static void ToDesign(float x,float y,out float u,out float v)
        {
            var arguments=new object[]{x,y,0f,0f};Surface.GetMethod("ToDesign",Static).Invoke(null,arguments);
            u=(float)arguments[2];v=(float)arguments[3];
        }

        [Test]
        public void OpenCourtIsOneContinuousFlagstoneFieldBesideTheFittedStone()
        {
            var go=new GameObject("Court flagstone test");
            try
            {
                var view=go.AddComponent<FoundryRegionView>();
                var cells=(HashSet<Vector2Int>)typeof(FoundryRegionView).GetField("_cells",Members).GetValue(view);
                for(int r=0;r<FoundryMap.RectangleCount;r++)
                    for(int x=FoundryMap.Bound(r,0);x<FoundryMap.Bound(r,2);x++)
                        for(int y=FoundryMap.Bound(r,1);y<FoundryMap.Bound(r,3);y++)cells.Add(new Vector2Int(x,y));
                object mesh=NewBuilder(),flags=NewBuilder();
                MethodInfo build=typeof(FoundryRegionView).GetMethod("BuildPavingCell",Members);
                foreach(Vector2Int cell in cells)build.Invoke(view,new object[]{mesh,flags,cell});
                var points=Data<Vector3>(flags,"V");var uv=Data<Vector2>(flags,"U");var colours=Data<Color>(flags,"C");
                Assert.That(points.Count,Is.GreaterThan(1000),"The open court is laid as flagstone.");
                // Before corner clipping every laid vertex carries exactly the
                // continuous light x use x contact field. A per-footprint tint (the
                // laid ashlar's .96-1.03 tone and +-1.5 % flag tints) would print
                // the old rectangular bond as a checker under the raster flags.
                float largest=0;
                for(int i=0;i<points.Count;i++)
                {
                    Assert.That(points[i].z,Is.EqualTo(0),"Flagstone stays flush with the walking plane.");
                    ToDesign(points[i].x,points[i].y,out float u,out float v);
                    u=Mathf.Round(u*1024)/1024;v=Mathf.Round(v*1024)/1024;
                    Color light=(Color)Invoke("FloorLight",u,v);
                    float use=(float)Invoke("PavingCourtUse",u,v);
                    Color contact=(Color)Invoke("PavingWorkshopTint",u,v);
                    var expected=new Color(light.r*use*contact.r,light.g*use*contact.g,light.b*use*contact.b,1);
                    float step=Mathf.Abs(expected.r-colours[i].r)+Mathf.Abs(expected.g-colours[i].g)+Mathf.Abs(expected.b-colours[i].b);
                    largest=Mathf.Max(largest,step);
                    Assert.That(step,Is.LessThan(2e-4f),$"Laid flagstone at ({u},{v}) carries only the continuous floor field.");
                    Assert.That(colours[i].a,Is.EqualTo(1));
                }
                Invoke("ClipFloorCorners",mesh);Invoke("ClipFloorCorners",flags);
                Invoke("CourtFlagstoneField",flags);
                float span=(float)typeof(FoundryRegionView).GetField("FlagstoneSpan",Static|BindingFlags.Public).GetValue(null);
                points=Data<Vector3>(flags,"V");uv=Data<Vector2>(flags,"U");
                for(int i=0;i<points.Count;i++)
                {
                    ToDesign(points[i].x,points[i].y,out float u,out float v);
                    Assert.That(Vector2.Distance(uv[i],new Vector2(u/span,v/span)),Is.LessThan(2e-4f),"One world-continuous raster field.");
                }
                int sampled=0,covered=0,fittedOverlaps=0;
                foreach(Vector2Int cell in cells)
                    foreach(Vector2 offset in new[]{new Vector2(.37f,.61f),new Vector2(.73f,.21f)})
                    {
                        float u=cell.x+offset.x,v=cell.y+offset.y;
                        Vector2 p=Project(u,v);
                        int laid=Owners(p,flags),fitted=Owners(p,mesh);
                        Assert.That(laid,Is.LessThanOrEqualTo(1),$"One flagstone layer at ({u},{v}).");
                        Assert.That(laid==0||fitted==0,Is.True,$"Flagstone never overlaps fitted stone at ({u},{v}): laid {laid}, fitted {fitted}.");
                        // Fitted V8 stones deliberately lay a joint bed beneath each
                        // face (two owners); that construction is only reported here.
                        if(fitted>1)fittedOverlaps++;
                        // The repair claims whole stones by their centre. Any stone
                        // holding this point (at most 3 x 2 units) has its centre within
                        // 1.8 units, so a fully claimed neighbourhood is decisive.
                        bool repair=(float)Invoke("ServiceApronWeight",u,v)>=1;
                        for(int k=0;k<16&&repair;k++)
                            repair=(float)Invoke("ServiceApronWeight",u+1.8f*Mathf.Cos(k*Mathf.PI/8),v+1.8f*Mathf.Sin(k*Mathf.PI/8))>=1;
                        bool apron=cell.x>=10&&cell.x<FoundryMap.GateLandingEnd&&cell.y>=0&&cell.y<10;
                        if(repair||apron)Assert.That(laid,Is.Zero,$"Work-apron repairs and the gate apron keep fitted stone at ({u},{v}).");
                        if(!FoundryMap.Cell(cell.x,cell.y)||!FoundryMap.CourtPlane(u,v))continue;
                        sampled++;
                        Assert.That(laid+fitted,Is.GreaterThanOrEqualTo(1),$"The walking floor stays fully paved at ({u},{v}).");
                        if(laid==1)covered++;
                    }
                TestContext.WriteLine($"Flagstone: largest field deviation {largest:F6}; {points.Count} vertices, fitted base {Data<Vector3>(mesh,"V").Count}; {covered}/{sampled} interior samples laid as flagstone; fitted bed-under-face samples {fittedOverlaps}.");
                Assert.That(covered,Is.GreaterThan(sampled/2),"Flagstone is the court's principal floor.");
                Assert.That(points.Count,Is.LessThan(30000),"The relief lives in the raster; the mesh is only the light lattice.");
            }
            finally{UnityEngine.Object.DestroyImmediate(go);}
        }

        [Test]
        public void FlagstoneRasterTilesSeamlesslyAtTheCalibratedValue()
        {
            var texture=new Texture2D(2,2);
            try
            {
                Assert.That(texture.LoadImage(File.ReadAllBytes(Raster)),Is.True);
                Assert.That(texture.width,Is.EqualTo(1024));Assert.That(texture.height,Is.EqualTo(1024));
                Color[] p=texture.GetPixels();int n=texture.width;
                float Luma(int x,int y){Color c=p[y*n+x];return .299f*c.r+.587f*c.g+.114f*c.b;}
                double total=0,r=0,b=0;float peak=0;
                foreach(Color c in p){total+=.299f*c.r+.587f*c.g+.114f*c.b;r+=c.r;b+=c.b;peak=Mathf.Max(peak,Mathf.Max(c.r,Mathf.Max(c.g,c.b)));}
                double mean=total/p.Length;
                Assert.That(mean,Is.InRange(.505,.535),"Mean value sits ~8 % above the V8 calibration (.481), toward the concept court.");
                Assert.That(b>=r,Is.True,"Faintly cool stone; the vertex light owns the violet ambient.");
                Assert.That((b-r)/p.Length,Is.LessThan(.06),"Never a conspicuously blue source.");
                Assert.That(peak,Is.LessThan(.98f),"No clipped highlights.");
                double seam=0,interior=0;
                for(int y=0;y<n;y++){seam+=Math.Abs(Luma(0,y)-Luma(n-1,y));interior+=Math.Abs(Luma(n/2,y)-Luma(n/2+1,y));}
                for(int x=0;x<n;x++){seam+=Math.Abs(Luma(x,0)-Luma(x,n-1));interior+=Math.Abs(Luma(x,n/2)-Luma(x,n/2+1));}
                TestContext.WriteLine($"Flagstone raster mean {mean:F4}; wrap seam {seam/(2*n):F4} vs interior step {interior/(2*n):F4}.");
                Assert.That(seam,Is.LessThan(interior*1.5),"Repeat wrap shows no seam: the joints and grain are periodic.");
            }
            finally{UnityEngine.Object.DestroyImmediate(texture);}
            var importer=(TextureImporter)AssetImporter.GetAtPath(Raster);
            Assert.That(importer.wrapMode,Is.EqualTo(TextureWrapMode.Repeat));
            Assert.That(importer.mipmapEnabled,Is.True);
            Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Trilinear));
            var android=importer.GetPlatformTextureSettings("Android");
            Assert.That(android.overridden,Is.True);
            Assert.That(android.format,Is.EqualTo(TextureImporterFormat.ASTC_6x6));
            Assert.That(android.maxTextureSize,Is.EqualTo(1024));
        }

        [Test]
        public void FlagstoneAndFittedPavingShareOneCoolStoneFamily()
        {
            var laid=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Props/Foundry/MAT_FoundryFlagstone.mat");
            var fitted=AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Props/Foundry/MAT_FoundryPaving.mat");
            Assert.That(laid.mainTexture.name,Is.EqualTo("FoundryPavingV9"));
            Assert.That(fitted.mainTexture.name,Is.EqualTo("FoundryPavingV8"));
            // Lifted 14 % from the V8 calibration (.587603232) to the concept's measured court value (2026-10-21).
            Assert.That(laid.color.r,Is.EqualTo(.67f).Within(1e-6f));
            Assert.That(1.74f*1.435f*Mathf.Max(laid.color.r,Mathf.Max(fitted.color.r,Mathf.Max(fitted.color.g,fitted.color.b))),Is.LessThan(1.9f),
                "Peak floor light x tone x gain stays inside the mobile fixed4 colour range.");
            Assert.That(laid.color.g,Is.EqualTo(laid.color.r));Assert.That(laid.color.b,Is.EqualTo(laid.color.r));
            Color c=fitted.color;float luma=.299f*c.r+.587f*c.g+.114f*c.b;
            Assert.That(luma,Is.EqualTo(.67f).Within(.004f),"Fitted stone keeps the flagstone's value; its balance is a hue correction.");
            Assert.That(c.r<c.g&&c.g<c.b,Is.True,"Warm V8 grain is cooled toward the laid flagstone beside it.");
            Assert.That(c.b/c.r,Is.LessThan(1.12f),"A restrained cool cut; the former strong blue cut read magenta.");
        }

        private static int Owners(Vector2 point,object mesh)
        {
            var v=Data<Vector3>(mesh,"V");var t=Data<int>(mesh,"T");int count=0;
            for(int i=0;i<t.Count;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                double area=((double)b.x-a.x)*((double)c.y-a.y)-((double)b.y-a.y)*((double)c.x-a.x);
                if(Math.Abs(area)<=1e-10)continue;
                double d1=Side(point,a,b),d2=Side(point,b,c),d3=Side(point,c,a);
                bool negative=d1<-1e-9||d2<-1e-9||d3<-1e-9,positive=d1>1e-9||d2>1e-9||d3>1e-9;
                if(!(negative&&positive))count++;
            }
            return count;
        }
        private static double Side(Vector2 p,Vector2 a,Vector2 b)=>((double)b.x-a.x)*((double)p.y-a.y)-((double)b.y-a.y)*((double)p.x-a.x);
    }
}
