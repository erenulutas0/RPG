// Actual Staff attack and integrated pooled effect; isolated profile, no scene/asset save.
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
    public sealed class StaffSplashRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-09-19/staff-runtime-01/";

        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;

        private Transform _hero;
        private readonly System.Collections.Generic.List<Object> _owned = new System.Collections.Generic.List<Object>();

        [UnityTest]
        public IEnumerator CaptureStaffSplash()
        {
            TestProfile.Begin(new Cryptforge.Core.PlayerProfile(0, null, null, 0, new[] { "weapon_staff" }, "weapon_staff"));
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.captureDeltaTime = 1f / 60; Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity"); yield return null;
            _scene = SceneManager.GetActiveScene(); _camera = Camera.main;
            _arena = Object.FindFirstObjectByType<ArenaView>();
            _hero = Object.FindFirstObjectByType<HeroLookView>().transform;
            var encounter = Object.FindFirstObjectByType<EncounterController>();
            foreach (var controller in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) controller.enabled = false;
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
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            var attack = _hero.GetComponent<AttackController>();
            var target = encounter.WaveEnemyAt(0);
            target.transform.position = new Vector3(.75f,.45f,0);
            _hero.position = new Vector3(-.6f,-.5f,0);
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Assert.That(attack.Weapon.Pattern.SplashRadius,Is.EqualTo(3.5f));
            Capture("room",1080,2340);
            // Actual automatic attack; freeze immediately after its event for the first screenshot.
            _hero.GetComponent<Targeting>().SetCandidates(new[]{target});
            bool struck=false;
            attack.Struck+=_=>{struck=true;Time.timeScale=0;};
            Time.timeScale=1;attack.enabled=true;
            for(int i=0;i<100&&!struck;i++)yield return null;
            Assert.That(struck,Is.True);attack.enabled=false;
            var pulse=System.Linq.Enumerable.Single(GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>(),
                r=>r.enabled&&r.sprite==StaffSplashArt.Get());
            Assert.That(pulse.sortingOrder,Is.Zero);
            for(int frame=0;frame<=14;frame++)
            {
                if(frame==0||frame==5||frame==9||frame==14)Capture("pulse-"+frame.ToString("000"),1080,2340);
                if(frame==5)
                {
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
                    Capture("short",1080,1920);
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
                }
                Time.timeScale=1;yield return null;Time.timeScale=0;
            }
            Assert.That(pulse.enabled,Is.False);
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
