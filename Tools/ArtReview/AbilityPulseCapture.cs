// Actual integrated ability ring capture. Test profile and frozen room; no scene/asset save.
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
    public sealed class AbilityPulseCapture
    {
        private const string Output = "ArtDirection/2026-09-19/ability-pulse-01/";

        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;

        private Transform _hero;
        private readonly System.Collections.Generic.List<Object> _owned = new System.Collections.Generic.List<Object>();

        [UnityTest]
        public IEnumerator CaptureAbilityPulse()
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
            Directory.CreateDirectory(Output);
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            var view = Object.FindFirstObjectByType<AbilityRingView>();
            var ring = view.Ring;
            Assert.That(ring.sprite, Is.SameAs(AbilityRingArt.Get()));
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Capture("ready",1080,2340);
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
            Capture("ready-short",1080,1920);
            _hero.position = new Vector3(3.2f,0,0);
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Capture("rim",1080,2340);
            var ability = Object.FindFirstObjectByType<AbilityController>();
            Assert.That(ability.TryUse(), Is.True);
            Capture("pulse-000",1080,2340);
            for (int frame = 1; frame <= 16; frame++)
            {
                Time.timeScale = 1;
                yield return null;
                Time.timeScale = 0;
                if (frame == 6 || frame == 12 || frame == 16)
                    Capture("pulse-" + frame.ToString("000"),1080,2340);
            }
            Assert.That(ring.color.a, Is.LessThan(.3f));
            LogAssert.NoUnexpectedReceived();
        }


        private void Capture(string file, int width, int height)
        {
            // Several staged poses render in one frozen frame; refresh Unity's cached group positions.
            UnityEngine.Rendering.SortingGroup.UpdateAllSortingGroups();
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

            for(int i=_owned.Count-1;i>=0;i--)if(_owned[i]!=null)Object.Destroy(_owned[i]);
            _owned.Clear();TestProfile.End();
        }
    }
}
