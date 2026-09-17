// Temporary graphics fixture. Registers and samples unchanged source pixels; never saves Assets or the scene.
using System;
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
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class GruntMotionCapture
    {
        private const string Output = "ArtDirection/2026-09-17/grunt-motion-01/";
        [Serializable] private sealed class Frame { public string name = ""; public string file = ""; public int[] bounds = Array.Empty<int>(); public int[] root = Array.Empty<int>(); }
        [Serializable] private sealed class Registration { public int canvas = 0; public int sample = 0; public int ppu = 0; public int[] pivot = Array.Empty<int>(); public Frame[] frames = Array.Empty<Frame>(); }
        private readonly List<Object> _owned = new List<Object>();
        private readonly Dictionary<string, Color32[]> _sources = new Dictionary<string, Color32[]>();
        private Camera _camera;
        private Scene _scene;
        private int _sourceWidth, _sourceHeight;

        [UnityTest]
        public IEnumerator RenderRegisteredGruntPosesAndCrowd()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.captureDeltaTime = 1f / 60; Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity"); yield return null;
            _scene = SceneManager.GetActiveScene(); _camera = Camera.main;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            for (int i = 0; i < 160; i++) yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            Time.timeScale = 0;
            var follow = _camera.GetComponent<ArenaCameraFollow>(); follow.enabled = false;
            var looks = Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None);
            Assert.That(looks.Length, Is.EqualTo(10), "Use the isolated density proof room.");
            var grunts = new List<EnemyLookView>();
            foreach (var look in looks)
            {
                foreach (var behaviour in look.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
                if (look.Look == EnemyLook.Grunt) grunts.Add(look);
            }
            Assert.That(grunts.Count, Is.EqualTo(2));
            var hero = Object.FindFirstObjectByType<HeroLookView>();
            foreach (var behaviour in hero.GetComponents<MonoBehaviour>()) behaviour.enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
            }
            foreach (var fitter in Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                fitter.enabled = false; var rect = (RectTransform)fitter.transform;
                rect.anchorMin = new Vector2(0,76f/2340); rect.anchorMax = new Vector2(1,1-100f/2340); rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            follow.Frame(1080,2340,361,2340-427);
            Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();
            Capture("unity-baseline-room",1080,2340);

            var registration = JsonUtility.FromJson<Registration>(File.ReadAllText(Output+"registration.json"));
            Assert.That(registration.frames.Length, Is.EqualTo(12));
            Assert.That(registration.canvas, Is.EqualTo(640)); Assert.That(registration.sample, Is.EqualTo(180));
            var poses = new Sprite[12];
            for (int i=0;i<12;i++)
            {
                poses[i] = Sample(registration.frames[i], registration);
                Assert.That(poses[i].pivot, Is.EqualTo(new Vector2(90,18)));
                Assert.That(poses[i].bounds.size, Is.EqualTo(poses[0].bounds.size));
            }
            var bodies = new List<SpriteRenderer>();
            foreach (var grunt in grunts)
            {
                var body = grunt.transform.Find("Enemy Body").GetComponent<SpriteRenderer>();
                body.transform.localPosition = Vector3.zero; body.sprite = poses[0]; bodies.Add(body);
            }
            Capture("unity-candidate-room",1080,2340);
            for(int i=0;i<grunts.Count;i++)grunts[i].transform.position=new Vector3(1.25f,.5f+i*.45f,0);
            yield return null; Capture("unity-close-pair-front",1080,2340);
            foreach(var body in bodies)body.sprite=poses[6];
            Capture("unity-close-pair-rear",1080,2340);
            foreach(var look in looks)if(look!=grunts[0])look.gameObject.SetActive(false);
            hero.gameObject.SetActive(false);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))canvas.enabled=false;
            grunts[0].transform.position=Vector3.zero;
            _camera.transform.position=new Vector3(0,.48f,_camera.transform.position.z); _camera.orthographicSize=.85f;
            yield return null;
            for(int i=0;i<poses.Length;i++)
            {
                bodies[0].sprite=poses[i];
                Capture("unity-"+registration.frames[i].name,480,480);
            }
            File.WriteAllText(Output+"unity-verification.txt",
                "12 registered samples: common 640x640 source canvas ->180x180 at128PPU, pivot90,18. Uniform scale, rigid per-pose source-root registration, no alpha-fit scaling.\n"+
                "10 real enemies in isolated density room; 2 Grunt bodies temporarily replaced, 8 runtime Mites and hero unchanged. Real bars/shadows retained.\n"+
                "Frozen visual staging only: no runtime animator, hit/death, spacing rule, attack timing, prefab, scene, APK or phone changes. Common canvas/pivot is verified; painted foot contacts, facing and volume consistency still need visual acceptance.\n");
            LogAssert.NoUnexpectedReceived();
        }

        private Sprite Sample(Frame frame, Registration r)
        {
            if(!_sources.TryGetValue(frame.file,out Color32[] source))
            {
                using(var reader=new BinaryReader(File.OpenRead("TestResults/grunt-motion-source/"+frame.file+".rgba")))
                {
                    _sourceWidth=reader.ReadInt32(); _sourceHeight=reader.ReadInt32();
                    Assert.That(_sourceWidth,Is.EqualTo(1024)); Assert.That(_sourceHeight,Is.EqualTo(1536));
                    var original=new Texture2D(_sourceWidth,_sourceHeight,TextureFormat.RGBA32,false);
                    original.SetPixelData(reader.ReadBytes(_sourceWidth*_sourceHeight*4),0);original.Apply();
                    source=original.GetPixels32();Object.DestroyImmediate(original);_sources.Add(frame.file,source);
                }
            }
            // Source rectangles isolate touching row/column margins, then exact texels receive rigid translation/padding.
            var registered=new Color32[r.canvas*r.canvas];
            Assert.That(r.pivot[0]+frame.bounds[0]-frame.root[0],Is.GreaterThanOrEqualTo(0));
            Assert.That(r.pivot[0]+frame.bounds[2]-1-frame.root[0],Is.LessThan(r.canvas));
            Assert.That(r.pivot[1]+frame.root[1]-frame.bounds[3]+1,Is.GreaterThanOrEqualTo(0));
            Assert.That(r.pivot[1]+frame.root[1]-frame.bounds[1],Is.LessThan(r.canvas));
            for(int sy=frame.bounds[1];sy<frame.bounds[3];sy++)for(int sx=frame.bounds[0];sx<frame.bounds[2];sx++)
            {
                int x=r.pivot[0]+sx-frame.root[0],y=r.pivot[1]+frame.root[1]-sy;
                registered[y*r.canvas+x]=source[(_sourceHeight-1-sy)*_sourceWidth+sx];
            }
            var originalCanvas=new Texture2D(r.canvas,r.canvas,TextureFormat.RGBA32,true);_owned.Add(originalCanvas);
            originalCanvas.SetPixels32(registered);originalCanvas.Apply(true);originalCanvas.filterMode=FilterMode.Trilinear;originalCanvas.wrapMode=TextureWrapMode.Clamp;
            var target=RenderTexture.GetTemporary(r.sample,r.sample,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var before=RenderTexture.active;
            Graphics.Blit(originalCanvas,target);RenderTexture.active=target;
            var texture=new Texture2D(r.sample,r.sample,TextureFormat.RGBA32,false);_owned.Add(texture);
            texture.ReadPixels(new Rect(0,0,r.sample,r.sample),0,0);texture.Apply();texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
            RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);
            var sprite=Sprite.Create(texture,new Rect(0,0,r.sample,r.sample),new Vector2((float)r.pivot[0]/r.canvas,(float)r.pivot[1]/r.canvas),r.ppu,0,SpriteMeshType.FullRect);
            _owned.Add(sprite);return sprite;
        }

        private void Capture(string file,int width,int height)
        {
            var target=RenderTexture.GetTemporary(width,height,24);var before=RenderTexture.active;
            float aspect=_camera.aspect;_camera.aspect=(float)width/height;_camera.targetTexture=target;_camera.Render();RenderTexture.active=target;
            var pixels=new Texture2D(width,height,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
            using(var writer=new BinaryWriter(File.Create(Output+file+".bmp")))
            {
                int bytes=width*height*3;
                writer.Write((ushort)0x4d42);writer.Write(bytes+54);writer.Write(0);writer.Write(54);
                writer.Write(40);writer.Write(width);writer.Write(height);writer.Write((ushort)1);writer.Write((ushort)24);
                writer.Write(0);writer.Write(bytes);writer.Write(0);writer.Write(0);writer.Write(0);writer.Write(0);
                foreach(Color32 pixel in pixels.GetPixels32()){writer.Write(pixel.b);writer.Write(pixel.g);writer.Write(pixel.r);}
            }
            Object.DestroyImmediate(pixels);_camera.targetTexture=null;_camera.aspect=aspect;RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale=1;Time.captureDeltaTime=0;
            if(_scene.IsValid()&&_scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Grunt motion cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            for(int i=_owned.Count-1;i>=0;i--)if(_owned[i]!=null)Object.Destroy(_owned[i]);
            _owned.Clear();_sources.Clear();TestProfile.End();
        }
    }
}
