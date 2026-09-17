// Graphics-enabled proof using the real prefab, view, damage events and pooled effects.
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

namespace Cryptforge.Tests
{
    public sealed class MiteRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-09-17/mite-runtime-01/";
        private Camera _camera;
        private Scene _scene;
        [UnityTest]
        public IEnumerator CaptureRuntimeMiteAndStrikes()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.captureDeltaTime = 1f / 60; Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity"); yield return null;
            _scene = SceneManager.GetActiveScene(); _camera = Camera.main;
            var encounter = Object.FindFirstObjectByType<EncounterController>();
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            // Let the real ten-enemy pack walk in without casualties for a density art review.
            for (int i = 0; i < 160; i++) yield return null;
            encounter.enabled = false;
            _camera.GetComponent<ArenaCameraFollow>().enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!canvas.isRootCanvas) continue;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = _camera; canvas.planeDistance = 1;
            }
            foreach (var fitter in Object.FindObjectsByType<SafeAreaFitter>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                fitter.enabled = false;
                var rect = (RectTransform)fitter.transform; rect.anchorMin = new Vector2(0,76f/2340); rect.anchorMax = new Vector2(1,1-100f/2340); rect.offsetMin = rect.offsetMax = Vector2.zero;
            }
            _camera.GetComponent<ArenaCameraFollow>().Frame(1080,2340,361,2340-427);
            Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern(); Canvas.ForceUpdateCanvases();
            Capture("runtime-crowd",1080,2340);
            EnemyLookView mite = null;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                if (mite == null && look.Look == EnemyLook.Mite && look.PaintedArt != null) mite = look;
                else look.gameObject.SetActive(false);
            }
            Assert.That(mite, Is.Not.Null);
            var hero = Object.FindFirstObjectByType<HeroLookView>(); hero.gameObject.SetActive(false);
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            _camera.orthographicSize = .62f;
            int n = 0;
            foreach (var direction in new[] { new Vector3(-1,-.5f), new Vector3(1,-.5f), new Vector3(-1,.5f), new Vector3(1,.5f) })
            {
                string side = new[] { "front-left", "front-right", "rear-left", "rear-right" }[n++];
                mite.transform.position = Vector3.zero;
                // Let the view observe the staging teleport before recording directional travel.
                yield return null;
                yield return null;
                var frames = new HashSet<int>();
                for (int i = 0; i < 24; i++)
                {
                    mite.transform.position += direction * .04f; yield return null;
                    Assert.That(mite.RearFacing, Is.EqualTo(direction.y > 0));
                    Assert.That(mite.Mirrored, Is.EqualTo(direction.x > 0));
                    if (frames.Add(mite.PaintedFrame)) Detail(mite, "runtime-" + side + "-step-" + mite.PaintedFrame);
                }
                yield return null; Detail(mite,"runtime-" + side + "-idle");
                var target = new GameObject("Mite capture target").AddComponent<Health>(); target.Initialize(1000);
                target.transform.position = mite.transform.position + direction * .1f;
                mite.GetComponent<Targeting>().SetCandidates(new[] { target });
                var attack = mite.GetComponent<AttackController>(); int attacks = attack.AttackCount; attack.enabled = true;
                for (int i = 0; i < 100 && attack.AttackCount == attacks; i++) yield return null;
                Assert.That(attack.AttackCount, Is.GreaterThan(attacks));
                Detail(mite,"runtime-" + side + "-strike"); attack.enabled = false;
                Object.Destroy(target.gameObject);
                for (int i = 0; i < 12; i++) yield return null;
            }
            mite.transform.position = Vector3.zero; yield return null;
            hero.gameObject.SetActive(true); hero.transform.position = new Vector3(-.4f,-.2f,0);
            _camera.transform.position = new Vector3(-.05f,.6f,-10); _camera.orthographicSize = 1.3f;
            var heroAttack = hero.GetComponent<AttackController>(); heroAttack.enabled = false;
            hero.GetComponent<Targeting>().SetCandidates(new[] { mite.GetComponent<Health>() });
            heroAttack.enabled = true; yield return null; yield return null; heroAttack.enabled = false;
            for (int i = 0; i < 16; i++) { Capture("runtime-hit-" + i.ToString("D2"),600,600); yield return null; }
            // If the sword did not kill the Mite, trigger its real death event now.
            mite.GetComponent<Health>().ApplyDamage(new DamageContext(mite.GetComponent<Health>().Current));
            hero.gameObject.SetActive(false);
            _camera.transform.position = new Vector3(0,.25f,-10); _camera.orthographicSize = .62f;
            Time.timeScale = 1;
            for (int i = 0; i < 26; i++) { Capture("runtime-death-" + i.ToString("D2"),600,600); yield return null; }
            LogAssert.NoUnexpectedReceived();
        }
        private void Detail(EnemyLookView mite,string name)
        {
            _camera.transform.position = new Vector3(mite.transform.position.x,mite.transform.position.y+.25f,-10);
            Capture(name,600,600);
        }
        private void Capture(string file, int width, int height)
        {
            var target = RenderTexture.GetTemporary(width, height, 24); var before = RenderTexture.active;
            float aspect = _camera.aspect; _camera.aspect = (float)width / height; _camera.targetTexture = target; _camera.Render(); RenderTexture.active = target;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
            using (var writer = new BinaryWriter(File.Create(Output + file + ".bmp")))
            {
                int bytes = width * height * 3;
                writer.Write((ushort)0x4d42); writer.Write(bytes + 54); writer.Write(0); writer.Write(54);
                writer.Write(40); writer.Write(width); writer.Write(height); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(bytes); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                foreach (Color32 pixel in pixels.GetPixels32()) { writer.Write(pixel.b); writer.Write(pixel.g); writer.Write(pixel.r); }
            }
            Object.Destroy(pixels); _camera.targetTexture = null; _camera.aspect = aspect; RenderTexture.active = before; RenderTexture.ReleaseTemporary(target);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1; Time.captureDeltaTime = 0;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Mite capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
