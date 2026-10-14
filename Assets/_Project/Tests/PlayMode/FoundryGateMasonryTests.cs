using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class FoundryGateMasonryTests
    {
        private Scene _scene;
        [SetUp] public void Begin()=>TestProfile.Begin();
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Gate masonry cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }

        [UnityTest] public IEnumerator BothBaysKeepProjectionDepthAndBorrowedTextureLifetime()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            var texture=Resources.Load<Texture2D>("FoundryGateMasonry");
            Assert.That(texture,Is.Not.Null);
            Assert.That(texture.width,Is.EqualTo(512));Assert.That(texture.height,Is.EqualTo(1024));
            Assert.That(texture.mipmapCount,Is.EqualTo(11));Assert.That(texture.isReadable,Is.False);
            Assert.That(texture.wrapMode,Is.EqualTo(TextureWrapMode.Clamp));
            Assert.That(texture.filterMode,Is.EqualTo(FilterMode.Trilinear));
            var cards=new List<MeshRenderer>();
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
                if(renderer.sharedMaterial.mainTexture==texture)cards.Add(renderer);
            Assert.That(cards.Count,Is.EqualTo(2),"One batch per original near/far rank; no doubled bay.");
            cards.Sort((a,b)=>a.sortingOrder.CompareTo(b.sortingOrder));
            var source=(Material)typeof(FoundryRegionView).GetField("_gateMasonryMaterial",BindingFlags.Instance|BindingFlags.NonPublic)
                .GetValue(arena.Region.GetComponent<FoundryRegionView>());
            Assert.That(source.mainTexture,Is.SameAs(texture));
            Assert.That(source.color,Is.EqualTo(new Color(.62f,.62f,.62f,1)),
                "Calibrated basalt gain keeps alpha unchanged and avoids pale towers around the dark gate.");
            Assert.That(cards[0].sharedMaterial,Is.SameAs(cards[1].sharedMaterial),"Both ranks share one cutaway variant.");
            Material variant=cards[0].sharedMaterial;
            Assert.That(variant,Is.Not.SameAs(source));
            Assert.That(variant.shader.name,Is.EqualTo("Cryptforge/Foundry Cutaway"));
            Assert.That(variant.color,Is.EqualTo(source.color));
            // This production south-feed point lies on fully opaque buttress
            // pixels (source pixel 373,1659). Equal-rank material bounds made
            // the overlap camera-dependent; require the actual feed triangles
            // to paint strictly after the near card, including their collars.
            Vector2 feedPoint=FoundrySurface.Project(28,-5.388135f)+Vector3.up;
            bool feedCoversPoint=false;
            foreach(var renderer in arena.Region.GetComponentsInChildren<MeshRenderer>())
            {
                if(renderer.sharedMaterial.mainTexture!=arena.PlatformMaterials.FoundryIron.mainTexture ||
                    renderer.sortingOrder<=cards[1].sortingOrder)continue;
                var feedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;
                var vertices=feedMesh.vertices;var indices=feedMesh.triangles;
                for(int i=0;i<indices.Length;i+=3)
                    if(Contains(feedPoint,vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]))
                        feedCoversPoint=true;
            }
            Assert.That(feedCoversPoint,Is.True,"The opaque south feed must deterministically remain in front of its receiving buttress.");
            var meshes=new List<Mesh>();
            for(int bay=0;bay<2;bay++)
            {
                var card=cards[bay];bool far=bay==0;
                Assert.That(card.sortingOrder,Is.EqualTo(far?-13:-7),
                    "Each baked bay paints after its curtain (-14/-8); batch bounds cannot hide the niche.");
                Assert.That(card.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty);
                var mesh=card.GetComponent<MeshFilter>().sharedMesh;meshes.Add(mesh);
                Assert.That(mesh.vertexCount,Is.EqualTo(4));Assert.That(mesh.triangles.Length,Is.EqualTo(6));
                CollectionAssert.AreEqual(new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up},mesh.uv,
                    "Directional stone light and chamfers must not be mirrored between bays.");
                var flags=new List<Vector2>();mesh.GetUVs(1,flags);
                Assert.That(flags.Count,Is.EqualTo(4));
                foreach(var flag in flags)Assert.That(flag,Is.EqualTo(Vector2.one));
                float v0=far?14.15f:-6.6f;
                Vector3 origin=FoundrySurface.Project(28.975f,v0+1.225f);
                Vector3 left=FoundrySurface.Project(27.05f,v0+2.45f),right=FoundrySurface.Project(30.9f,v0);
                var offsets=new[]{new Vector2(-2.5f,-1.25f),new Vector2(2.5f,-1.25f),
                    new Vector2(2.5f,8.75f),new Vector2(-2.5f,8.75f)};
                for(int i=0;i<4;i++)
                {
                    Vector3 p=mesh.vertices[i];
                    Assert.That(Vector2.Distance(p,(Vector2)origin+offsets[i]),Is.LessThan(.0001f),
                        "Projection metadata is a physical 5x10 field with landing lift applied once.");
                    float expected=50+Mathf.Lerp(left.y,right.y,Mathf.InverseLerp(left.x,right.x,p.x));
                    Assert.That(p.z,Is.EqualTo(expected).Within(.0001f),
                        "Transparent margins inherit the original buttress owner, not the neighbouring gate/furnace.");
                }
            }
            SceneManager.SetActiveScene(SceneManager.CreateScene("After gate masonry"));
            yield return SceneManager.UnloadSceneAsync(_scene);yield return null;
            foreach(var mesh in meshes)Assert.That(mesh==null,Is.True);
            Assert.That(source==null&&variant==null,Is.True,"Scene teardown releases owned materials and generated cards.");
            Assert.That(texture!=null,Is.True,"The imported texture is borrowed across scene reloads.");
            LogAssert.NoUnexpectedReceived();
        }

        private static bool Contains(Vector2 p,Vector2 a,Vector2 b,Vector2 c)
        {
            float Cross(Vector2 x,Vector2 y)=>x.x*y.y-x.y*y.x;
            float area=Cross(b-a,c-a);if(Mathf.Abs(area)<1e-7f)return false;
            float u=Cross(b-p,c-p)/area,v=Cross(c-p,a-p)/area,w=1-u-v;
            return u>=-.0001f&&v>=-.0001f&&w>=-.0001f;
        }
    }
}
