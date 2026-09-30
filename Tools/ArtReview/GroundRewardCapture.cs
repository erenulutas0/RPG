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
    public sealed class GroundRewardCapture
    {
        private const string Output = "ArtDirection/2026-09-22/reward-runtime-01/";
        private Camera _camera;
        private Scene _scene;

        [UnityTest]
        public IEnumerator CaptureGroundDepthAndCollection()
        {
            Directory.CreateDirectory(Output);
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.RunSeedPath));
            File.WriteAllText(DevelopmentStart.RunSeedPath, "42");
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.captureDeltaTime = 1f / 60f;
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _camera = Camera.main;
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
            // First evidence is a real defeated enemy feeding the integrated subscriber.
            setup.Encounters.WaveEnemyAt(0).ApplyDamage(new DamageContext(100000));
            Assert.That(view.GroundCount, Is.EqualTo(1));
            yield return new WaitForSeconds(.8f);
            Capture("actual-kill", 1080, 2340);
            foreach (var spot in new[] {new Vector2(.72f,-.42f),new Vector2(-1.65f,-.55f),new Vector2(1.5f,-1.8f),
                new Vector2(-.6f,-2.7f),new Vector2(.1f,2.2f),new Vector2(-2.7f,.2f),new Vector2(2.4f,1.4f),
                new Vector2(-1.5f,2.6f),new Vector2(2.75f,-2.8f),new Vector2(-2.6f,-2.5f),new Vector2(1.3f,3.4f),
                new Vector2(-.5f,4.4f),new Vector2(.8f,-3.9f),new Vector2(-1.7f,-4.1f),new Vector2(3.4f,-.6f),
                new Vector2(-3.4f,1.4f),new Vector2(2.3f,4f)}) setup.Drops.Add(spot.x,spot.y,1,1);
            yield return null;
            Capture("ground-18",1080,2340);
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,361,1920-427);
            Capture("ground-short",1080,1920);
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            setup.Drops.Add(.03f,.18f,1,1);
            setup.Drops.Add(-.06f,-.14f,1,1);
            yield return null;
            Vector3 before = _camera.transform.position; float previousSize = _camera.orthographicSize;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            _camera.transform.position = new Vector3(0,.5f,before.z); _camera.orthographicSize=1.3f;
            Capture("hero-depth",600,600);
            _camera.transform.position=before;_camera.orthographicSize=previousSize;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled=true;
            setup.Drops.CollectNear(0,0);
            yield return null;
            for(int i=0;i<4;i++)yield return null;
            Capture("pickup-flight",1080,2340);
            yield return new WaitForSeconds(.3f);
            setup.Drops.CollectAll();
            yield return null;
            Capture("sweep-choice-paused",1080,2340);
            for(int i=0;i<10 && setup.Upgrades.CurrentOffer!=null;i++)
                Assert.That(setup.Upgrades.TrySelect(setup.Upgrades.CurrentOffer,0),Is.True);
            for(int i=0;i<10;i++)yield return null;
            Capture("sweep-flight",1080,2340);
            yield return new WaitForSeconds(.5f);
            Assert.That(view.GroundCount,Is.Zero);Assert.That(view.FlyingCount,Is.Zero);
            Capture("collected",1080,2340);
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
