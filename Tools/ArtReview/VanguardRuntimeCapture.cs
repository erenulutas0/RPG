// Temporary graphics-enabled PlayMode fixture. Captures actual runtime frames, not a replacement presentation rig.
using System.Collections;
using System.Collections.Generic;
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
    public sealed class VanguardRuntimeCapture
    {
        private const string Output = "ArtDirection/2026-09-17/vanguard-runtime-01/";
        private Camera _camera;
        private HeroLookView _look;
        private Scene _scene;

        [UnityTest]
        public IEnumerator CaptureActualMovementAndAttacks()
        {
            Time.captureDeltaTime = 1f / 60;
            foreach (string id in new[] { "sword", "weapon_staff", "weapon_daggers" })
            {
                TestProfile.Begin(new PlayerProfile(0, null, null, 0, new[] { id }, id));
                Time.timeScale = 1;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
                yield return null;
                _scene = SceneManager.GetActiveScene();
                _camera = Camera.main;
                _camera.GetComponent<ArenaCameraFollow>().enabled = false;
                _look = Object.FindFirstObjectByType<HeroLookView>();
                Assert.That(_look.UsesPaintedArt, Is.True);
                Object.FindFirstObjectByType<EncounterController>().enabled = false;
                foreach (var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                var attack = _look.GetComponent<AttackController>(); attack.enabled = false;
                var move = _look.GetComponent<HeroMovementInput>();
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
                if (id == "sword")
                {
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080, 2340, 361, 2340 - 427);
                    Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
                    Canvas.ForceUpdateCanvases();
                    Capture("runtime-room", 1080, 2340);
                }
                Detail(id + "-idle");
                move.Hold(Vector2.right);
                var seen = new HashSet<int>();
                for (int i = 0; i < 36; i++)
                {
                    yield return null;
                    if (seen.Add(_look.PaintedFrame)) Detail(id + "-walk-" + _look.PaintedFrame);
                }
                CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, seen);
                move.Release(); yield return null;
                var target = new GameObject("Capture target").AddComponent<Health>(); target.Initialize(100000);
                target.transform.position = _look.transform.position + Vector3.right * .3f;
                _look.GetComponent<Targeting>().SetCandidates(new[] { target }); attack.enabled = true;
                seen.Clear();
                for (int i = 0; i < 80; i++)
                {
                    yield return null;
                    if (seen.Add(_look.PaintedFrame)) Detail(id + "-attack-" + _look.PaintedFrame);
                }
                if (id == "sword") CollectionAssert.IsSubsetOf(new[] { 5, 6, 7 }, seen);
            }
            LogAssert.NoUnexpectedReceived();
        }

        private void Detail(string file)
        {
            Vector3 position = _camera.transform.position; float size = _camera.orthographicSize;
            var visible = new List<Canvas>();
            foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) if (c.enabled) { visible.Add(c); c.enabled = false; }
            _camera.transform.position = new Vector3(_look.transform.position.x, _look.transform.position.y + .85f, position.z);
            _camera.orthographicSize = 1.55f;
            Capture(file, 600, 600);
            _camera.transform.position = position; _camera.orthographicSize = size;
            foreach (var c in visible) c.enabled = true;
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
                SceneManager.SetActiveScene(SceneManager.CreateScene("Vanguard capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
