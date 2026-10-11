using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class FoundryDistantWorksTests
    {
        private Scene _scene;
        [SetUp] public void Begin()=>TestProfile.Begin();
        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale=1;
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Distant works cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }

        [UnityTest] public IEnumerator StaticWorksStayBetweenCavernAndMapAndReleaseOwnedResources()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RegionPreviewPath));
            File.WriteAllText(DevelopmentStart.RegionPreviewPath,"foundry");
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            _scene=SceneManager.GetActiveScene();Time.timeScale=0;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena,Is.Not.Null);Assert.That(arena.Region,Is.Not.Null);
            var backdrop=Object.FindFirstObjectByType<VoidBackdropView>();
            Assert.That(backdrop,Is.Not.Null);Assert.That(backdrop.CavernRenderer,Is.Not.Null);
            Assert.That(backdrop.SkyRenderer.sortingOrder,Is.EqualTo(-22));
            Assert.That(backdrop.CavernRenderer.sortingOrder,Is.EqualTo(-21));
            Assert.That(backdrop.SkyRenderer.sortingOrder,Is.LessThan(backdrop.CavernRenderer.sortingOrder));

            var layers=new List<MeshRenderer>();
            var meshes=new List<Mesh>();
            var materials=new HashSet<Material>();
            Material ownedMasonry=null,borrowedWall=arena.PlatformMaterials.FoundryWall;
            Texture borrowedWallTexture=borrowedWall.mainTexture;
            var positions=new List<Vector3>();
            var rotations=new List<Quaternion>();
            var scales=new List<Vector3>();
            var points=new List<Vector3[]>();
            var all=arena.Region.GetComponentsInChildren<MeshRenderer>();
            int vertices=0,masonryLayers=0,slitLayers=0;
            bool floor=false,sidewall=false;
            foreach(var renderer in all)
            {
                if(!renderer.name.StartsWith("Distant foundry works"))continue;
                layers.Add(renderer);
                Assert.That(renderer.sortingOrder,Is.GreaterThan(backdrop.CavernRenderer.sortingOrder),"Distant geometry stays above the painted cavern.");
                Material material=renderer.sharedMaterial;
                bool masonry=renderer.name=="Distant foundry works";
                if(masonry)
                {
                    masonryLayers++;
                    Assert.That(renderer.sortingOrder,Is.EqualTo(-20),"Distant masonry has its own explicit rear rank.");
                    ownedMasonry=material;
                    Assert.That(material,Is.Not.SameAs(borrowedWall));
                    Assert.That(material.shader.name,Is.EqualTo("Cryptforge/Foundry Cavern"));
                    Assert.That(material.mainTexture,Is.SameAs(borrowedWallTexture),"The scene owns only its atmospheric material, not another texture.");
                    Assert.That(material.GetFloat("_Atmosphere"),Is.EqualTo(.44f).Within(.00001f));
                }
                else
                {
                    slitLayers++;
                    Assert.That(renderer.sortingOrder,Is.EqualTo(-19),"Furnace slits render after their masonry without bounds-based ties.");
                    Assert.That(material,Is.SameAs(arena.PlatformMaterials.FoundryEmbers));
                    materials.Add(material);
                }
                foreach(var component in renderer.GetComponentsInChildren<Component>(true))
                    Assert.That(component is Transform || component is MeshFilter || component is MeshRenderer,Is.True,
                        "Distant works contain only rendering components; no physics or gameplay dependencies.");
                Assert.That(renderer.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty,"Scenic geometry adds no runtime updater or gameplay component.");
                var filter=renderer.GetComponent<MeshFilter>();
                Assert.That(filter,Is.Not.Null);Mesh mesh=filter.sharedMesh;
                Assert.That(mesh,Is.Not.Null);meshes.Add(mesh);
                Vector3[] local=mesh.vertices;points.Add(local);vertices+=local.Length;
                Assert.That(local.Length,Is.GreaterThan(0));
                Assert.That(mesh.uv.Length,Is.EqualTo(local.Length));
                foreach(var uv in mesh.uv)Assert.That(IsFinite(uv.x)&&IsFinite(uv.y),Is.True,"Fixed facade mapping must not produce invalid texture coordinates on vertical edges.");
                var depthFlags=new List<Vector2>();mesh.GetUVs(1,depthFlags);
                Assert.That(depthFlags,Is.Empty,"Background geometry must not become an actor occluder.");
                foreach(var p in local)
                {
                    Assert.That(IsFinite(p.x)&&IsFinite(p.y)&&IsFinite(p.z),Is.True);
                    Assert.That(mesh.bounds.Contains(p),Is.True,"Culling bounds contain every authored point.");
                    Assert.That(Mathf.Abs(p.x),Is.LessThan(100));
                    Assert.That(Mathf.Abs(p.y),Is.LessThan(100),"Background anchors remain bounded around this foundry, not camera-scaled infinity.");
                }
                if(masonry)
                {
                    bool faded=false,solid=false;
                    foreach(var color in mesh.colors)
                    {
                        Assert.That(color.a,Is.InRange(0f,1f));
                        if(color.a==0)faded=true;
                        if(color.a==1)solid=true;
                    }
                    Assert.That(faded&&solid,Is.True,"Masonry fades into lower haze while retaining a solid upper silhouette.");
                }
                int[] triangles=mesh.triangles;
                Assert.That(triangles.Length,Is.GreaterThan(0));Assert.That(triangles.Length%3,Is.Zero);
                foreach(int index in triangles)Assert.That(index,Is.InRange(0,local.Length-1));
                positions.Add(renderer.transform.position);rotations.Add(renderer.transform.rotation);scales.Add(renderer.transform.lossyScale);
            }
            Assert.That(layers.Count,Is.EqualTo(2));
            Assert.That(masonryLayers,Is.EqualTo(1));Assert.That(slitLayers,Is.EqualTo(1));
            Assert.That(vertices,Is.LessThanOrEqualTo(2500),"Static background geometry has a bounded combined budget.");
            foreach(var renderer in all)
            {
                if(layers.Contains(renderer))continue;
                Assert.That(renderer.sortingOrder,Is.GreaterThanOrEqualTo(-18),"All existing island geometry, including the deep basin, stays above distant works.");
                if(renderer.sharedMaterial==arena.PlatformMaterials.FoundryPaving)
                {
                    floor=true;
                    Assert.That(renderer.sortingOrder,Is.GreaterThan(-18),"Distant works stay behind the playable floor.");
                }
                if(renderer.sharedMaterial==arena.PlatformMaterials.FoundryWall&&renderer.sortingOrder==-17)
                {
                    sidewall=true;
                    Assert.That(renderer.sortingOrder,Is.GreaterThan(-18),"Distant works stay behind the island's lower sidewalls.");
                }
            }
            Assert.That(floor,Is.True);Assert.That(sidewall,Is.True,"The test compares real floor and lower sidewalls, not only a hard-coded backdrop rank.");

            var arrival=Object.FindFirstObjectByType<GateArrivalView>();
            if(arrival!=null)
            {
                arrival.Skip();
                for(int frame=0;frame<4&&arrival.IsPresenting;frame++)yield return null;
                Assert.That(arrival.IsPresenting,Is.False,"Finish any opt-in reveal before the camera stability check.");
            }
            Camera camera=Camera.main;Assert.That(camera,Is.Not.Null);
            var follow=camera.GetComponent<ArenaCameraFollow>();
            if(follow!=null)follow.enabled=false;
            camera.transform.position+=new Vector3(6,-3,0);camera.orthographicSize+=2;
            Vector3 movedCamera=camera.transform.position;
            yield return null;yield return null;
            Assert.That(camera.transform.position,Is.EqualTo(movedCamera),"The comparison spans actual frames at the changed camera pose.");
            for(int i=0;i<layers.Count;i++)
            {
                Assert.That(layers[i].transform.position,Is.EqualTo(positions[i]));
                Assert.That(layers[i].transform.rotation,Is.EqualTo(rotations[i]));
                Assert.That(layers[i].transform.lossyScale,Is.EqualTo(scales[i]));
                Assert.That(layers[i].GetComponent<MeshFilter>().sharedMesh,Is.SameAs(meshes[i]),"Camera movement does not rebuild the world-anchored works.");
                CollectionAssert.AreEqual(points[i],meshes[i].vertices,"Static background geometry does not animate per frame.");
            }

            SceneManager.SetActiveScene(SceneManager.CreateScene("After distant works"));
            yield return SceneManager.UnloadSceneAsync(_scene);yield return null;
            foreach(var mesh in meshes)Assert.That(mesh==null,Is.True,"Scene teardown releases generated background meshes.");
            Assert.That(ownedMasonry==null,Is.True,"Scene teardown releases the distant atmospheric material.");
            Assert.That(borrowedWall!=null&&borrowedWallTexture!=null,Is.True,"Borrowed wall material and texture survive teardown.");
            foreach(var material in materials)Assert.That(material!=null,Is.True,"Borrowed materials survive teardown.");
            LogAssert.NoUnexpectedReceived();
        }

        private static bool IsFinite(float value)=>!float.IsNaN(value)&&!float.IsInfinity(value);
    }
}
