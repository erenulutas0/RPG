using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Actual integrated view, with explicitly staged drops. No view transforms or sprites are substituted.
    public sealed class ExitSealCapture
    {
        private const string Output = "ArtDirection/2026-09-23/exit-seal-runtime-01/";
        private Camera _camera;
        private Scene _scene;

        [UnityTest]
        public IEnumerator CaptureGroundDepthAndCollection()
        {
            Directory.CreateDirectory(Output);
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RunSeedPath, "42");
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_swarm_proof");
            Time.captureDeltaTime = 1f / 60f;
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _camera = Camera.main;
            _camera.aspect = 1080f / 2340f;
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var hero = Object.FindFirstObjectByType<HeroLookView>();
            var view = Object.FindFirstObjectByType<GroundDropView>();
            Assert.That(view.PaintedSprite, Is.Not.Null);
            setup.Encounters.enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
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
                rect.anchorMin = new Vector2(0, 76f / 2340); rect.anchorMax = new Vector2(1, 1 - 100f / 2340);
                rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080, 2340, 361, 2340 - 427);
            Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
            Canvas.ForceUpdateCanvases();
            setup.Encounters.enabled = true;
            hero.GetComponent<HeroMovementInput>().enabled = false;
            yield return null;
            Capture("exit-idle",1080,2340);
            Vector3 exit=new Vector3(setup.Encounters.ExitFloorX,ArenaFloor.WorldY(setup.Encounters.ExitFloorY),0);
            hero.transform.position=exit;
            yield return new WaitForSeconds(.4f);
            Time.timeScale=0;
            Capture("exit-hold",1080,2340);
            yield return new WaitForSecondsRealtime(.25f);
            Capture("exit-paused",1080,2340);
            _camera.transform.position+=Vector3.right*20;
            yield return null;yield return null;
            Capture("exit-direction",1080,2340);
            LogAssert.NoUnexpectedReceived();
        }

        private void Capture(string name,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;
            float aspect=_camera.aspect;_camera.aspect=(float)width/height;_camera.targetTexture=target;
            _camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            using(var writer=new BinaryWriter(File.Create(Output+name+".bmp")))
            {
                int bytes=width*height*3;writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 p in pixels.GetPixels32()){writer.Write(p.b);writer.Write(p.g);writer.Write(p.r);}
            }
            Object.Destroy(pixels);_camera.targetTexture=null;_camera.aspect=aspect;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            Time.timeScale=1;Time.captureDeltaTime=0;
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Reward capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
