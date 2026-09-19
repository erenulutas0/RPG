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
    public sealed class StaffWeaponCapture
    {
        private const string Output = "ArtDirection/2026-09-19/staff-weapon-runtime-01/";
        private Camera _camera;
        private HeroLookView _look;
        private Scene _scene;

        [UnityTest]
        public IEnumerator CaptureActualMovementAndAttacks()
        {
            Time.captureDeltaTime = 1f / 60;
            foreach (string id in new[] { "weapon_staff" })
            {
                TestProfile.Begin(new PlayerProfile(0, null, null, 0, new[] { id }, id));
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath)); File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof"); Time.timeScale = 1;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
                yield return null;
                _scene = SceneManager.GetActiveScene();
                _camera = Camera.main;
                _camera.GetComponent<ArenaCameraFollow>().enabled = false;
                _look = Object.FindFirstObjectByType<HeroLookView>();
                Assert.That(_look.UsesPaintedArt, Is.True);
                Object.FindFirstObjectByType<EncounterController>().enabled = false;
                foreach (var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) a.enabled=false;
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
                if (id == "weapon_staff")
                {
                    _camera.GetComponent<ArenaCameraFollow>().Frame(1080, 2340, 361, 2340 - 427);
                    Object.FindFirstObjectByType<ArenaView>().Backdrop.FrameCavern();
                    Canvas.ForceUpdateCanvases();
                    Capture("runtime-room", 1080, 2340);
                }
                foreach (var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                Assert.That(_look.PaintedArt.HasFrontFrames, Is.True);
                int directionIndex=0;
                foreach (var direction in new[] { new Vector2(1,1),new Vector2(-1,1),new Vector2(1,-1),new Vector2(-1,-1) })
                {
                    string label=id+"-"+new[]{"rear-right","rear-left","front-right","front-left"}[directionIndex++];
                    attack.enabled=false;
                    _look.transform.position=Vector3.zero; yield return null; yield return null;
                    move.Hold(direction);
                    yield return null; yield return null;
                    move.Release(); yield return null;
                    Detail(label + "-idle");
                    move.Hold(direction);
                    var seen = new HashSet<int>();
                    for (int i = 0; i < 36; i++)
                    {
                        yield return null;
                        if (seen.Add(_look.PaintedFrame)) Detail(label + "-walk-" + _look.PaintedFrame);
                        if(directionIndex==3 && i<24) Detail("gait-"+i.ToString("D2"));
                    }
                    CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, seen);
                    move.Release(); yield return null;
                    var target = new GameObject("Capture target").AddComponent<Health>(); target.Initialize(100000);
                    target.transform.position = _look.transform.position + new Vector3(direction.x,direction.y*.5f,0) * .3f;
                    _look.GetComponent<Targeting>().SetCandidates(new[] { target }); attack.enabled = true;
                    seen.Clear(); bool capturedCast=false;
                    for (int i = 0; i < 80; i++)
                    {
                        yield return null;
                        if (seen.Add(_look.PaintedFrame)) Detail(label + "-attack-" + _look.PaintedFrame);
                        if(!capturedCast && _look.IsSwinging && Quaternion.Angle(GameObject.Find("Staff Shaft").transform.localRotation,Quaternion.identity)>5) { Detail(label+"-cast-lean"); capturedCast=true; }
                    }
                    Assert.That(capturedCast,Is.True,"Observe an actual cast instead of assuming its start frame.");
                    attack.enabled=false;
                    Object.Destroy(target);
                }
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
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath)); File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof"); Time.timeScale = 1; Time.captureDeltaTime = 0;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Vanguard capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
