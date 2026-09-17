// Actual integrated platform capture. Test profile and frozen room; no scene/asset save.
using System.Collections;

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
    public sealed class PlatformRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-09-18/platform-runtime-01/";

        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;

        private Transform _hero;

        [UnityTest]
        public IEnumerator CaptureIntegratedMaterials()
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
            Assert.That(_arena.Platform.UsesPaintedMaterials, Is.True);
            var positions = new[] { Vector3.zero, new Vector3(0,-3.9f,0), new Vector3(8,0,0), new Vector3(0,3.9f,0) };
            string[] labels = { "centre", "near", "right", "far" };
            for (int i = 0; i < positions.Length; i++)
            {
                // Hero/camera rim samples; enemies stay frozen near the centre.
                _hero.position = positions[i];
                foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None)) shadow.Refresh();
                _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
                _arena.Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();

                Capture(labels[i]+"-runtime",1080,2340);
                if (i == 0)
                {

                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
                    _arena.Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();
                    Capture("centre-runtime-short",1080,1920);
                }
            }
            _hero.position = Vector3.zero;
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None)) shadow.Refresh();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            _camera.transform.position = new Vector3(0,-1.5f,-10); _camera.orthographicSize = 7.6f;
            _arena.Backdrop.FrameCavern();

            Capture("overview-runtime",1440,1080);
            _camera.transform.position = new Vector3(0,.4f,-10); _camera.orthographicSize = 2.4f;

            Capture("detail-runtime",960,960);
            Assert.That(encounter.AliveEnemyCount, Is.EqualTo(10));
            LogAssert.NoUnexpectedReceived();
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

            TestProfile.End();
        }
    }
}
