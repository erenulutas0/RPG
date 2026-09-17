// Isolated material-mapping proof. No scene/asset save or gameplay change.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    public sealed class PlatformMaterialCapture
    {
        private const string Output = "ArtDirection/2026-09-17/platform-material-01/";
        private readonly List<Object> _owned = new List<Object>();
        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;
        private GameObject _skin;
        private Transform _hero;
        private MeshRenderer _floorRenderer;
        private Material _floorV1, _floorV2;

        [UnityTest]
        public IEnumerator CompareMaterialsInRealRoom()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.captureDeltaTime = 1f / 60; Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity"); yield return null;
            _scene = SceneManager.GetActiveScene(); _camera = Camera.main;
            _arena = Object.FindFirstObjectByType<ArenaView>();
            _hero = Object.FindFirstObjectByType<HeroLookView>().transform;
            var encounter = Object.FindFirstObjectByType<EncounterController>();
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            for (int i = 0; i < 160; i++) yield return null;
            encounter.enabled = false;
            Time.timeScale = 0;
            _camera.GetComponent<ArenaCameraFollow>().enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
            }
            foreach (var fitter in Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                fitter.enabled = false;
                var rect = (RectTransform)fitter.transform;
                rect.anchorMin = new Vector2(0,76f/2340); rect.anchorMax = new Vector2(1,1-100f/2340);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            BuildSkin();
            var positions = new[] { Vector3.zero, new Vector3(0,-3.9f,0), new Vector3(8,0,0), new Vector3(0,3.9f,0) };
            string[] labels = { "centre", "near", "right", "far" };
            for (int i = 0; i < positions.Length; i++)
            {
                // Camera sampling positions only: frozen combatants stay identical between A/B images.
                _hero.position = positions[i];
                foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None)) shadow.Refresh();
                _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
                _arena.Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();
                Toggle(false); Capture(labels[i]+"-baseline",1080,2340);
                Toggle(true); Capture(labels[i]+"-candidate",1080,2340);
                if (i == 0)
                {
                    _floorRenderer.sharedMaterial = _floorV1;
                    Capture("centre-candidate-v1",1080,2340);
                    _floorRenderer.sharedMaterial = _floorV2;
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
                    _arena.Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();
                    Capture("centre-candidate-short",1080,1920);
                }
            }
            _hero.position = Vector3.zero;
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None)) shadow.Refresh();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            _camera.transform.position = new Vector3(0,-1.5f,-10); _camera.orthographicSize = 7.6f;
            _arena.Backdrop.FrameCavern();
            Toggle(false); Capture("overview-baseline",1440,1080);
            Toggle(true); Capture("overview-candidate",1440,1080);
            _camera.transform.position = new Vector3(0,.4f,-10); _camera.orthographicSize = 2.4f;
            Toggle(false); Capture("detail-baseline",960,960);
            Toggle(true); Capture("detail-candidate",960,960);
            Assert.That(encounter.AliveEnemyCount, Is.EqualTo(10));
            LogAssert.NoUnexpectedReceived();
        }

        private void BuildSkin()
        {
            _skin = new GameObject("Temporary painted material proof");
            _floorV1 = Material("floor-v1.png", true);
            _floorV2 = Material("floor-v2.png", true);
            var coping = Material("coping-v1.png", true);
            var wall = Material("wall-v1.png", true);
            Vector3[] outer = Corners(.998f), inner = Corners(.95f);
            // Near, right, far, left; one square material maps exactly into the same rhombus.
            Quad("Floor", inner, new[] { new Vector2(0,0), new Vector2(2,0), new Vector2(2,2), new Vector2(0,2) }, _floorV2);
            _floorRenderer = _skin.transform.Find("Floor").GetComponent<MeshRenderer>();
            for (int i = 0; i < 4; i++)
            {
                int next = (i+1)%4;
                Quad("Coping " + i, new[] { outer[i], outer[next], inner[next], inner[i] },
                    new[] { new Vector2(0,0), new Vector2(2,0), new Vector2(2,1), new Vector2(0,1) }, coping);
            }
            Vector3 drop = new Vector3(0,-PlatformLayout.FaceDepth,0);
            foreach (int side in new[] { 1, 3 })
                Quad("Near face " + side, new[] {outer[0]+drop,outer[side]+drop,outer[side],outer[0]},
                    new[] {new Vector2(0,0),new Vector2(3,0),new Vector2(3,1),new Vector2(0,1)}, wall);
        }
        private Vector3[] Corners(float scale)
        {
            var g = _arena.Geometry;
            float y = g.WorldMiddle, half = (g.WorldTop-g.WorldBottom)*.5f;
            return new[] {new Vector3(0,y-half*scale),new Vector3(g.HalfWidth*scale,y),new Vector3(0,y+half*scale),new Vector3(-g.HalfWidth*scale,y)};
        }
        private Material Material(string name, bool repeat)
        {
            Texture2D texture;
            using (var reader = new BinaryReader(File.OpenRead("TestResults/platform-material-source/"+name+".rgba")))
            {
                int width = reader.ReadInt32(), height = reader.ReadInt32();
                texture = new Texture2D(width,height,TextureFormat.RGBA32,true);
                texture.SetPixelData(reader.ReadBytes(width*height*4),0); texture.Apply(true,true);
            }
            texture.filterMode = FilterMode.Trilinear;
            texture.wrapMode = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            _owned.Add(texture);
            var material = new Material(Shader.Find("Sprites/Default")); material.mainTexture = texture;
            _owned.Add(material); return material;
        }
        private void Quad(string name, Vector3[] vertices, Vector2[] uv, Material material)
        {
            var mesh = new Mesh {name=name, vertices=vertices, uv=uv, triangles=new[]{0,1,2,0,2,3},
                colors=new[]{Color.white,Color.white,Color.white,Color.white}};
            mesh.RecalculateBounds(); _owned.Add(mesh);
            var part = new GameObject(name); part.transform.SetParent(_skin.transform,false);
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial=material; renderer.sortingOrder=-14;
            Assert.That(renderer.sortingOrder, Is.LessThan(_arena.GroundEffectSortingOrder));
        }
        private void Toggle(bool candidate)
        {
            _skin.SetActive(candidate);
            // Retain old keel/crystal/corner fixtures below the mapped surface. No shared texture mutation.
            _arena.Platform.PlatformRenderer.sortingOrder = candidate ? -16 : -15;
            _arena.Platform.LightsRenderer.sortingOrder = candidate ? -15 : -14;
        }
        private void Capture(string file, int width, int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24); var before=RenderTexture.active;
            float aspect=_camera.aspect; _camera.aspect=(float)width/height; _camera.targetTexture=target;
            _arena.Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();
            _camera.Render(); RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0); pixels.Apply();
            using(var writer=new BinaryWriter(File.Create(Output+file+".bmp")))
            {
                int bytes=width*height*3;
                writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 pixel in pixels.GetPixels32()){writer.Write(pixel.b);writer.Write(pixel.g);writer.Write(pixel.r);}
            }
            Object.Destroy(pixels); _camera.targetTexture=null; _camera.aspect=aspect; RenderTexture.active=before; RenderTexture.ReleaseTemporary(target);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;Time.captureDeltaTime=0;
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Platform material cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            foreach(var resource in _owned) if(resource!=null) Object.Destroy(resource);
            _owned.Clear();TestProfile.End();
        }
    }
}
