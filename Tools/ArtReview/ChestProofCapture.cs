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
    public sealed class ChestProofCapture
    {
        private const string Output = "ArtDirection/2026-09-18/chest-unity-01/";

        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;

        private Transform _hero;
        private readonly System.Collections.Generic.List<Object> _owned = new System.Collections.Generic.List<Object>();

        [UnityTest]
        public IEnumerator CaptureChestCandidates()
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
            var spawner = Object.FindFirstObjectByType<ChestSpawner>();
            spawner.enabled = false;
            var chest = spawner.transform.Find("Chest").GetComponent<SpriteRenderer>();
            Assert.That(chest.enabled, Is.True);
            var original = chest.sprite;
            int opened = spawner.ChestsOpened;
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Capture("baseline-room",1080,2340);
            string[] states = { "closed", "opening", "open" };
            var sprites = new Sprite[3];
            for(int i=0;i<3;i++) {
                sprites[i]=Sample(states[i]);
                Assert.That(sprites[i].pivot, Is.EqualTo(sprites[0].pivot));
                Assert.That(sprites[i].bounds.size, Is.EqualTo(sprites[0].bounds.size));
            }
            for(int i=0;i<3;i++) {
                chest.sprite=sprites[i];
                Capture(states[i]+"-room-075",1080,2340);
            }
            chest.sprite=sprites[0];
            chest.transform.localScale=Vector3.one*.9f;
            Capture("closed-room-0675",1080,2340);
            chest.transform.localScale=Vector3.one;
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
            Capture("closed-room-short",1080,1920);
            foreach(var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled=false;
            _camera.transform.position=new Vector3(0,.4f,-10); _camera.orthographicSize=2.4f;
            for(int i=0;i<3;i++){chest.sprite=sprites[i];Capture(states[i]+"-detail",960,960);}
            // Deliberate depth stress: place the actual hero in front of and behind the prop.
            Vector3 at=chest.transform.position;
            _camera.transform.position=new Vector3(at.x,at.y+.4f,-10);_camera.orthographicSize=1.5f;
            chest.sprite=sprites[2];
            foreach(var behaviour in _hero.GetComponents<MonoBehaviour>())behaviour.enabled=false;
            _hero.position=at+new Vector3(.12f,-.25f,0);
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None))shadow.Refresh();
            Capture("overlap-hero-front",720,960);
            _hero.position=at+new Vector3(.12f,.25f,0);
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None))shadow.Refresh();
            Capture("overlap-hero-behind",720,960);
            chest.sprite=original;
            Assert.That(spawner.ChestsOpened,Is.EqualTo(opened),"Visual sampling must not award a chest.");
            Assert.That(encounter.AliveEnemyCount, Is.EqualTo(10));
            LogAssert.NoUnexpectedReceived();
        }


        private Sprite Sample(string state)
        {
            using(var reader=new BinaryReader(File.OpenRead("TestResults/chest-source/chest-"+state+"-v1.png.rgba")))
            {
                int width=reader.ReadInt32(),height=reader.ReadInt32();
                Assert.That(width,Is.EqualTo(1254));Assert.That(height,Is.EqualTo(1254));
                var source=new Texture2D(width,height,TextureFormat.RGBA32,true);_owned.Add(source);
                source.SetPixelData(reader.ReadBytes(width*height*4),0);source.Apply(true);
                source.filterMode=FilterMode.Trilinear;source.wrapMode=TextureWrapMode.Clamp;
                var target=RenderTexture.GetTemporary(192,192,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                var before=RenderTexture.active;
                Graphics.Blit(source,target);RenderTexture.active=target;
                var texture=new Texture2D(192,192,TextureFormat.RGBA32,false);_owned.Add(texture);
                texture.ReadPixels(new Rect(0,0,192,192),0,0);texture.Apply();
                texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
                RenderTexture.active=before;RenderTexture.ReleaseTemporary(target);
                float ppu=192f*788f/(1254f*.75f);
                var sprite=Sprite.Create(texture,new Rect(0,0,192,192),new Vector2(620f/1254,(1254f-1080)/1254),ppu,0,SpriteMeshType.FullRect);
                _owned.Add(sprite);return sprite;
            }
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

            for(int i=_owned.Count-1;i>=0;i--)if(_owned[i]!=null)Object.Destroy(_owned[i]);
            _owned.Clear();TestProfile.End();
        }
    }
}
