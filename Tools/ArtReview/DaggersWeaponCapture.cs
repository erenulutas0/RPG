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
    public sealed class DaggersWeaponCapture
    {
        private const string Output = "ArtDirection/2026-09-19/daggers-unity-01/";
        private Camera _camera;
        private HeroLookView _look;
        private Scene _scene;
        private Sprite _dagger;
        private Texture2D _daggerTexture;
        private bool _anchorCandidate;

        [UnityTest]
        public IEnumerator CaptureActualMovementAndAttacks()
        {
            BuildDagger();
            Time.captureDeltaTime = 1f / 60;
            foreach (string id in new[] { "weapon_daggers" })
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
                if (id == "weapon_daggers")
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
                        if(!capturedCast && _look.IsSwinging && GameObject.Find("Dagger Right").transform.localPosition.y-_look.PaintedArt.GetFrame(_look.PaintedFrame,_look.FrontFacing).RightHand.y>.06f) { Detail(label+"-thrust"); _anchorCandidate=true; Detail(label+"-anchored"); _anchorCandidate=false; capturedCast=true; }
                    }
                    Assert.That(capturedCast,Is.True,"Observe actual dagger displacement.");
                    attack.enabled=false;
                    Object.Destroy(target);
                }
            }
            LogAssert.NoUnexpectedReceived();
        }

        private void BuildDagger()
        {
            Texture2D source;
            using(var reader=new BinaryReader(File.OpenRead("TestResults/daggers-weapon-source/dagger-v1.png.rgba")))
            {
                int w=reader.ReadInt32(),h=reader.ReadInt32();
                source=new Texture2D(w,h,TextureFormat.RGBA32,true);
                source.SetPixelData(reader.ReadBytes(w*h*4),0);source.Apply(true);
            }
            source.filterMode=FilterMode.Trilinear;source.wrapMode=TextureWrapMode.Clamp;
            var rt=RenderTexture.GetTemporary(20,64,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous=RenderTexture.active;
            // Fixed padded crop (438,10,380,1220); near-uniform scale, provisional grip (628,975).
            Graphics.Blit(source,rt,new Vector2(380f/1254,1220f/1254),new Vector2(438f/1254,24f/1254));
            RenderTexture.active=rt;
            _daggerTexture=new Texture2D(20,64,TextureFormat.RGBA32,false);
            _daggerTexture.ReadPixels(new Rect(0,0,20,64),0,0);_daggerTexture.Apply();
            _daggerTexture.filterMode=FilterMode.Bilinear;_daggerTexture.wrapMode=TextureWrapMode.Clamp;
            _dagger=Sprite.Create(_daggerTexture,new Rect(0,0,20,64),new Vector2(.5f,255f/1220),128);
            RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.Destroy(source);
        }

        private void ApplyDaggerSample()
        {
            var body=GameObject.Find("Hero Body").GetComponent<SpriteRenderer>();
            var left=GameObject.Find("Dagger Left").GetComponent<SpriteRenderer>();
            var right=GameObject.Find("Dagger Right").GetComponent<SpriteRenderer>();
            left.sprite=right.sprite=_dagger;
            // Only candidate geometry/depth is substituted. Runtime hand translation remains observable.
            left.transform.localRotation=Quaternion.Euler(0,0,_look.FrontFacing?-35:35);
            right.transform.localRotation=Quaternion.Euler(0,0,_look.FrontFacing?35:-35);
            left.sortingOrder=right.sortingOrder=body.sortingOrder+(_look.FrontFacing?2:-1);
            if(_anchorCandidate)
            {
                var frame=_look.PaintedArt.GetFrame(_look.PaintedFrame,_look.FrontFacing);
                float pulse=Mathf.Clamp01((right.transform.localPosition.y-frame.RightHand.y)/.12f);
                left.transform.localPosition=frame.LeftHand; right.transform.localPosition=frame.RightHand;
                left.transform.localRotation=Quaternion.Euler(0,0,(_look.FrontFacing?-1:1)*(35+12*pulse));
                right.transform.localRotation=Quaternion.Euler(0,0,(_look.FrontFacing?1:-1)*(35+12*pulse));
                left.sortingOrder=right.sortingOrder=body.sortingOrder-1;
                Assert.That(left.transform.localPosition,Is.EqualTo((Vector3)frame.LeftHand));
                Assert.That(right.transform.localPosition,Is.EqualTo((Vector3)frame.RightHand));
            }
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
            ApplyDaggerSample();
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
            Object.Destroy(_dagger); Object.Destroy(_daggerTexture);
        }
    }
}
