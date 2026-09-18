// Actual integrated chest capture. Test profile and frozen room; no scene/asset save.
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
    public sealed class ChestRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-09-18/chest-runtime-01/";

        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;

        private Transform _hero;
        private readonly System.Collections.Generic.List<Object> _owned = new System.Collections.Generic.List<Object>();

        [UnityTest]
        public IEnumerator CaptureIntegratedChest()
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
            Capture("integrated-room",1080,2340);
            string[] states = { "closed", "opening", "open" };
            Assert.That(spawner.UsesPaintedArt, Is.True); var art = (ChestArtSet)typeof(ChestSpawner).GetField("_art", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(spawner); var sprites = new[] { art.Closed, art.Opening, art.Open };
            for(int i=0;i<3;i++) {

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
            // Isolate the pair after retaining honest crowd-overlap evidence above.
            foreach(var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))look.gameObject.SetActive(false);
            Object.FindFirstObjectByType<AbilityRingView>().Ring.enabled=false;
            _hero.position=at+new Vector3(.12f,-.25f,0);
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None))shadow.Refresh();
            Capture("isolated-hero-front",720,960);
            _hero.position=at+new Vector3(.12f,.25f,0);
            foreach(var shadow in Object.FindObjectsByType<ContactShadowView>(FindObjectsSortMode.None))shadow.Refresh();
            Capture("isolated-hero-behind",720,960);
            chest.sprite=original;
            Assert.That(spawner.ChestsOpened,Is.EqualTo(opened),"Visual sampling must not award a chest.");
            Assert.That(encounter.AliveEnemyCount, Is.EqualTo(10));
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
