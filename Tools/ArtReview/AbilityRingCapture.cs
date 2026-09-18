// Isolated ability ring comparison; deterministic code-native geometry. Test profile and frozen room; no scene/asset save.
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
    public sealed class AbilityRingCapture
    {
        private const string Output = "ArtDirection/2026-09-19/ability-ring-01/";

        private Scene _scene;
        private Camera _camera;
        private ArenaView _arena;

        private Transform _hero;
        private readonly System.Collections.Generic.List<Object> _owned = new System.Collections.Generic.List<Object>();

        [UnityTest]
        public IEnumerator CaptureAbilityRing()
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
            view.enabled = false;
            var ring = view.Ring;
            var original = ring.sprite;
            Color originalColor = ring.color;
            float radius = Object.FindFirstObjectByType<AbilityController>().Ability.Radius;
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Capture("before",1080,2340);
            var continuous = CreateRing(radius, false);
            var seal = CreateRing(radius, true);
            ring.sprite = continuous; ring.color = new Color(1,1,1,.65f);
            Capture("continuous-ready",1080,2340);
            ring.sprite = seal;
            Capture("seal-ready",1080,2340);
            ring.color = new Color(1,1,1,.22f);
            Capture("seal-cooling",1080,2340);
            ring.color = new Color(1,1,1,.65f);
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,1920,330,1920-370);
            Capture("seal-short",1080,1920);
            ring.sprite = continuous;
            Capture("continuous-short",1080,1920);
            // A static placement proof: show the marker extending past the platform at the rim honestly.
            _hero.position = new Vector3(3.2f,0,0);
            ring.sprite = seal;
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Capture("seal-rim",1080,2340);
            Assert.That(seal.pixelsPerUnit, Is.EqualTo(240f/radius));
            Assert.That(ring.transform.localPosition, Is.EqualTo(Vector3.zero));
            ring.sprite = original; ring.color = originalColor;
            LogAssert.NoUnexpectedReceived();
        }


        // The mathematical contour is the exact floor radius, projected 2:1. The centre stays fully clear.
        private Sprite CreateRing(float radius, bool seal)
        {
            const int width = 512, height = 256;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = x + .5f - width*.5f, dy = y + .5f - height*.5f;
                float length = Mathf.Sqrt(dx*dx + 4*dy*dy);
                float angle = Mathf.Atan2(dy*2, dx);
                float gradient = Mathf.Sqrt(Mathf.Cos(angle)*Mathf.Cos(angle) + 4*Mathf.Sin(angle)*Mathf.Sin(angle));
                float distance = Mathf.Abs(length - 240) / gradient;
                float phase = Mathf.Repeat(angle + Mathf.PI/8, Mathf.PI/4);
                bool gap = seal && Mathf.Abs(phase - Mathf.PI/8) < .027f;
                float core = gap ? 0 : Mathf.Clamp01((seal ? 2.1f : 1.35f) - distance);
                float shadow = gap ? 0 : .36f * Mathf.Clamp01(3.5f - distance);
                // Four small inward lozenges reference the forge motif without inventing target points.
                float marker = 0;
                if (seal)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float a = i * Mathf.PI/2;
                        float mx = Mathf.Cos(a)*231, my = Mathf.Sin(a)*115.5f;
                        float edge = Mathf.Abs(Mathf.Abs(dx-mx) + Mathf.Abs(dy-my)*1.5f - 4.5f);
                        marker = Mathf.Max(marker, Mathf.Clamp01(1.1f-edge));
                    }
                }
                core = Mathf.Max(core, marker);
                float alpha = Mathf.Max(core, shadow);
                Color color = Color.Lerp(new Color(.055f,.08f,.16f,alpha), new Color(seal ? .63f : .43f,seal ? .84f : .76f,1f,alpha), core);
                pixels[y*width+x] = color;
            }
            var texture = new Texture2D(width,height,TextureFormat.RGBA32,false)
            { name = seal ? "Arc seal proof" : "Continuous contour proof", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            texture.SetPixels32(pixels); texture.Apply(false,true);
            var sprite = Sprite.Create(texture,new Rect(0,0,width,height),new Vector2(.5f,.5f),240f/radius,0,SpriteMeshType.FullRect);
            _owned.Add(texture); _owned.Add(sprite);
            return sprite;
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
                SceneManager.SetActiveScene(SceneManager.CreateScene("Ability ring cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }

            for(int i=_owned.Count-1;i>=0;i--)if(_owned[i]!=null)Object.Destroy(_owned[i]);
            _owned.Clear();TestProfile.End();
        }
    }
}
