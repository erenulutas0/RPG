// Isolated Staff splash study in the actual scene. View-event staging only; no damage or scene/asset save.
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
    public sealed class StaffSplashCapture
    {
        private const string Output = "ArtDirection/2026-09-19/staff-splash-01/";

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
            var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            var strike = typeof(CombatEffectsView).GetMethod("OnHeroStruck", flags);
            var hide = typeof(CombatEffectsView).GetMethod("HideAll", flags);
            var ringField = typeof(CombatEffectsView).GetField("_ring", flags);
            var original = (Sprite[])ringField.GetValue(effects);
            Assert.That(attack.Weapon.Pattern.SplashRadius, Is.EqualTo(3.5f));
            Capture("room",1080,2340);
            // Invoke only the actual presentation subscriber: no damage, rewards or fabricated gameplay result.
            strike.Invoke(effects,new object[]{target});
            for(int frame=0;frame<=14;frame++)
            {
                if(frame==0||frame==5||frame==9||frame==14) Capture("old-"+frame.ToString("000"),1080,2340);
                Time.timeScale=1; yield return null; Time.timeScale=0;
            }
            hide.Invoke(effects,null);
            var candidate = new Sprite[7];
            for(int i=0;i<candidate.Length;i++) candidate[i]=CreateContour(i);
            ringField.SetValue(effects,candidate);
            strike.Invoke(effects,new object[]{target});
            // Prototype seven 30ms frames in the same pool/lifetime. Production should use one immutable
            // contour with transform/alpha animation, not ship this temporary seven-texture sampling.
            var slots=(System.Array)typeof(CombatEffectsView).GetField("_effects",flags).GetValue(effects);
            SpriteRenderer pulse=null;
            foreach(object slot in slots)
            {
                var type=slot.GetType();
                var renderer=(SpriteRenderer)type.GetField("Renderer").GetValue(slot);
                if(renderer.sprite!=candidate[0]||!renderer.enabled) continue;
                type.GetField("FrameDuration").SetValue(slot,.03f);
                type.GetField("Lifetime").SetValue(slot,.21f);
                renderer.sortingOrder=0;
                pulse=renderer;
            }
            Assert.That(pulse,Is.Not.Null);
            Vector3 origin=pulse.transform.position;
            for(int frame=0;frame<=14;frame++)
            {
                if(frame==0||frame==5||frame==9||frame==14) Capture("new-"+frame.ToString("000"),1080,2340);
                if(frame==5)
                {
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
                    Capture("new-short",1080,1920);
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
                }
                Assert.That(pulse.transform.position,Is.EqualTo(origin));
                Time.timeScale=1; yield return null; Time.timeScale=0;
            }
            Assert.That(pulse.enabled,Is.False,"The prototype expires with the original .21s lifetime.");
            ringField.SetValue(effects,original);
            LogAssert.NoUnexpectedReceived();
        }


        private Sprite CreateContour(int frame)
        {
            const int width=512,height=256;
            float time=frame*.03f;
            float progress=Mathf.Clamp01(time/.14f);
            float radius=240*Mathf.Lerp(24f/38,1,1-(1-progress)*(1-progress));
            float fade=Mathf.Clamp01((.21f-time)/.11f);
            var pixels=new Color32[width*height];
            for(int y=0;y<height;y++)for(int x=0;x<width;x++)
            {
                float dx=x+.5f-width*.5f,dy=(y+.5f-height*.5f)*2;
                float length=Mathf.Sqrt(dx*dx+dy*dy);
                float angle=Mathf.Atan2(dy,dx);
                float phase=Mathf.Repeat(angle+Mathf.PI/4,Mathf.PI/2)/(Mathf.PI/2);
                // Four broad tapered arcs, without the ability's eight-segment seal or diamond markers.
                float taper=Mathf.Clamp01((phase-.10f)/.12f)*Mathf.Clamp01((.90f-phase)/.25f);
                float gradient=Mathf.Sqrt(Mathf.Cos(angle)*Mathf.Cos(angle)+4*Mathf.Sin(angle)*Mathf.Sin(angle));
                float distance=Mathf.Abs(length-radius)/gradient;
                float core=Mathf.Clamp01(2.3f*taper-distance);
                float glow=.28f*Mathf.Clamp01((7*taper-distance)/5);
                float alpha=Mathf.Max(core,glow)*fade;
                Color color=Color.Lerp(new Color(.44f,.23f,.9f),new Color(.91f,.79f,1),core);
                color.a=alpha; pixels[y*width+x]=color;
            }
            var texture=new Texture2D(width,height,TextureFormat.RGBA32,false)
            {name="Staff proof contour "+frame,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            texture.SetPixels32(pixels);texture.Apply(false,true);_owned.Add(texture);
            var sprite=Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),240f/(38f/32),0,SpriteMeshType.FullRect);
            _owned.Add(sprite);return sprite;
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
