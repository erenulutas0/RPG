using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class FoundryCastingBenchTests
    {
        private Scene _scene;
        [SetUp] public void Begin()=>TestProfile.Begin();
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Casting bench cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }

        [Test] public void ShelfStaysOutsideWalkingUnionAndClearOfNorthFinial()
        {
            Vector4 shelf=FoundryRegionView.CastingShelf;
            for(int i=0;i<=50;i++)for(int j=0;j<=33;j++)
            {
                float u=Mathf.Lerp(shelf.x,shelf.z,i/50f),v=Mathf.Lerp(shelf.y,shelf.w,j/33f);
                FoundryMap.ToFloor(u,v,out float x,out float y);
                Assert.That(FoundryMap.SurfaceContains(x,y,0),Is.False,"The exterior scenic bench must remain outside the physical floor, including occupied paving.");
            }
            // The existing north finial's cap extends .38 beyond its centre.
            float du=Mathf.Max(shelf.x+14,Mathf.Max(0,-14-shelf.z));
            float dv=Mathf.Max(shelf.y-18.5f,Mathf.Max(0,18.5f-shelf.w));
            Assert.That(Mathf.Sqrt(du*du+dv*dv)-.38f,Is.GreaterThan(.2f),"Keep a real gap to the finial cap.");
            FoundryMap.ToFloor(-20,16,out float ax,out float ay);
            FoundryMap.ToFloor(-10,16,out float bx,out float by);
            Assert.That(FoundryMap.Segment(ax,ay,bx,by,.6f),Is.True,"The existing north passage remains traversable.");
        }

        [UnityTest] public IEnumerator CardHasShelfDepthAndReleasesOnlyOwnedResources()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena,Is.Not.Null);Assert.That(arena.Region,Is.Not.Null);
            var texture=Resources.Load<Texture2D>("FoundryCastingBench");
            Assert.That(texture,Is.Not.Null);Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.mipmapCount,Is.GreaterThan(1));
            Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            var all=arena.Region.GetComponentsInChildren<MeshRenderer>();
            MeshRenderer card=null;
            foreach(var renderer in all)
                if(renderer.sharedMaterial.mainTexture==texture)
                {
                    Assert.That(card,Is.Null,"One card/material batch suffices for this bench.");card=renderer;
                }
            Assert.That(card,Is.Not.Null);Assert.That(card.sortingOrder,Is.EqualTo(-10));
            Assert.That(card.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty,"Scenic geometry adds no gameplay updater.");
            var view=arena.Region.GetComponent<FoundryRegionView>();
            var field=typeof(FoundryRegionView).GetField("_castingBenchMaterial",BindingFlags.Instance|BindingFlags.NonPublic);
            Assert.That(field,Is.Not.Null);
            var source=(Material)field.GetValue(view);
            Assert.That(source,Is.Not.Null);Assert.That(source.mainTexture,Is.SameAs(texture));
            Material variant=card.sharedMaterial;
            Assert.That(variant,Is.Not.SameAs(source));
            Assert.That(variant.shader.name,Is.EqualTo("Cryptforge/Foundry Cutaway"));
            Mesh mesh=card.GetComponent<MeshFilter>().sharedMesh;
            Assert.That(mesh.vertexCount,Is.EqualTo(4));Assert.That(mesh.triangles.Length,Is.EqualTo(6));
            CollectionAssert.AreEqual(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},mesh.uv);
            var flags=new List<Vector2>();mesh.GetUVs(1,flags);
            Assert.That(flags.Count,Is.EqualTo(4));
            foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one));
            Vector4 shelf=FoundryRegionView.CastingShelf;
            Vector3 left=FoundrySurface.Project(shelf.x,shelf.w),right=FoundrySurface.Project(shelf.z,shelf.y);
            Vector3 origin=FoundrySurface.Project(-15.5f,20.75f);
            var offsets=new[]{new Vector2(-3.5f,-2.5f),new Vector2(3.5f,-2.5f),new Vector2(3.5f,4.5f),new Vector2(-3.5f,4.5f)};
            var vertices=mesh.vertices;
            for(int i=0;i<4;i++)
            {
                Assert.That(Vector2.Distance(vertices[i],(Vector2)origin+offsets[i]),Is.LessThan(.0001f),"Card preserves the bake's actual projection.");
                float expected=50+Mathf.Lerp(left.y,right.y,Mathf.InverseLerp(left.x,right.x,vertices[i].x));
                Assert.That(vertices[i].z,Is.EqualTo(expected).Within(.0001f),"Transparent margins inherit this shelf's depth, never a nearby finial.");
            }
            // Verify actual support triangles beneath representative metal foot locations.
            // Transparent card bounds alone cannot establish contact with the masonry.
            foreach(var foot in new[]{new Vector2(-17.3f,19.7f),new Vector2(-14.2f,21.6f),new Vector2(-15.5f,20.75f)})
            {
                Vector2 point=FoundrySurface.Project(foot.x,foot.y)+Vector3.up*.18f;
                bool supported=false;
                foreach(var renderer in all)
                    if(renderer.sortingOrder==-12&&renderer.sharedMaterial.mainTexture==arena.PlatformMaterials.Floor.mainTexture)
                        supported|=Covers(renderer.GetComponent<MeshFilter>().sharedMesh,point);
                Assert.That(supported,Is.True,"Authored bench feet need the actual raised shelf top.");
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene("After casting bench"));
            yield return SceneManager.UnloadSceneAsync(_scene);yield return null;
            Assert.That(mesh==null,Is.True,"Generated card mesh is scene-owned.");
            Assert.That(source==null&&variant==null,Is.True,"Both source and cutaway material are scene-owned.");
            Assert.That(texture!=null,Is.True,"Imported resource texture is borrowed, not destroyed on teardown.");
            LogAssert.NoUnexpectedReceived();
        }

        private static bool Covers(Mesh mesh,Vector2 point)
        {
            var v=mesh.vertices;var t=mesh.triangles;
            float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
            for(int i=0;i<t.Length;i+=3)
            {
                Vector2 a=v[t[i]],b=v[t[i+1]],c=v[t[i+2]];
                float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-8f)continue;
                float x=Cross(b-point,c-point)/area,y=Cross(c-point,a-point)/area;
                if(x>=-1e-5f&&y>=-1e-5f&&x+y<=1.00001f)return true;
            }
            return false;
        }
    }
}
