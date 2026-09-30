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
    public sealed class FurnaceCapture
    {
        private const string Output = "ArtDirection/2026-09-23/furnace-unity-01/";
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
            AddFurnace(arena);
            Capture("start",1080,2340);
            var report=new System.Text.StringBuilder("point,seconds,x,y,stamina\n");
            int index=0;
            foreach(var target in new[]{new Vector2(0,24)})
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

        private void AddFurnace(ArenaView arena)
        {
            var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Props/Foundry/Furnace.tga");
            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.isReadable, Is.False);
            var prop = new GameObject("Perimeter furnace proof");
            prop.transform.SetParent(arena.transform, false);
            prop.transform.localPosition = new Vector3(0, 13.55f, 0);
            prop.transform.localScale = Vector3.one * .72f;
            var renderer = prop.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = -13;
            // A visual foundation outside the playable apex. No collision or walkable extension is implied.
            var plinth = new GameObject("Furnace foundation proof");
            plinth.transform.SetParent(arena.transform, false);
            _plinthMesh = new Mesh();
            _plinthMesh.vertices = new[] { new Vector3(0,13.3f),new Vector3(.8f,13.7f),
                new Vector3(0,14.1f),new Vector3(-.8f,13.7f),
                new Vector3(0,13.05f),new Vector3(.8f,13.45f),new Vector3(-.8f,13.45f) };
            _plinthMesh.triangles = new[] {0,1,2,0,2,3,4,5,1,4,1,0,6,4,0,6,0,3};
            _plinthMesh.uv = new[] {Vector2.zero,new Vector2(.35f,0),new Vector2(.35f,.35f),
                new Vector2(0,.35f),new Vector2(0,-.12f),new Vector2(.35f,-.12f),new Vector2(-.12f,.35f)};
            _plinthMesh.colors = new[] {Color.white,Color.white,Color.white,Color.white,
                new Color(.45f,.45f,.5f),new Color(.45f,.45f,.5f),new Color(.45f,.45f,.5f)};
            _plinthMesh.RecalculateBounds();
            plinth.AddComponent<MeshFilter>().sharedMesh = _plinthMesh;
            var foundation = plinth.AddComponent<MeshRenderer>();
            foundation.sharedMaterial = arena.PlatformMaterials.Floor;
            foundation.sortingOrder = -14;
            Assert.That(arena.IsOnPlatform(0,27.1f), Is.False);
        }

        private Mesh _plinthMesh;
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
            if (_plinthMesh != null) Object.Destroy(_plinthMesh);
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Reward capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}

