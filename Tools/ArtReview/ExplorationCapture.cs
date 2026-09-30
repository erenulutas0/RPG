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
    public sealed class ExplorationCapture
    {
        private const string Output = "ArtDirection/2026-09-23/exploration-proof-01/";
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
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/_Project/Scenes/Gameplay/ExplorationProof.unity", new LoadSceneParameters(LoadSceneMode.Single));
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
            var movement=hero.GetComponent<HeroMovementInput>();
            _camera.GetComponent<ArenaCameraFollow>().enabled=true;
            var arena=Object.FindFirstObjectByType<ArenaView>();
            Assert.That(arena.Geometry.HalfWidth,Is.EqualTo(18));
            Assert.That(arena.Geometry.HalfDepth,Is.EqualTo(27));
            Capture("start",1080,2340);
            var report=new System.Text.StringBuilder("point,seconds,x,y,stamina\n");
            int index=0;
            foreach(var target in new[]{new Vector2(10,0),new Vector2(0,16),new Vector2(-10,0)})
            {
                float elapsed=0;
                while(Vector2.Distance(new Vector2(movement.Motion.X,movement.Motion.Y),target)>.2f && elapsed<45)
                {
                    Vector2 direction=target-new Vector2(movement.Motion.X,movement.Motion.Y);
                    movement.Hold(direction.normalized);
                    yield return null;elapsed+=Time.deltaTime;
                    Assert.That(arena.Geometry.IsOnPlatform(movement.Motion.X,movement.Motion.Y),Is.True);
                }
                movement.Release();yield return null;
                Assert.That(elapsed,Is.LessThan(45),"Waypoint not reachable");
                report.AppendLine(string.Format(System.Globalization.CultureInfo.InvariantCulture,"{0},{1:F2},{2:F2},{3:F2},{4:F2}",index,elapsed,movement.Motion.X,movement.Motion.Y,movement.Stamina.Current));
                _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
                Capture("point-"+index,1080,2340);index++;
            }
            File.WriteAllText(Output+"walk.csv",report.ToString());
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
