// Temporary graphics-enabled PlayMode fixture (Tools/ArtReview/Run-HeroMotionCapture.ps1 installs and removes it):
// records the hero's actual runtime motion in the Foundry - stance, walks in four directions, attacks on the move and
// the ability - as numbered frames for review GIFs, for each presentation the build offers: the painted frames, the
// cut-out puppet (development/puppet.txt) and the Cinder Arcanist (development/hero.txt). Nothing here ships.
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
    public sealed class HeroMotionCapture
    {
        private const string OutputRoot = "TestResults/hero-motion/";
        private const int Size = 480;
        // Every second frame of 60: 30 frames a second of review.
        private const int Every = 2;
        private Camera _camera;
        private Transform _hero;
        private Scene _scene;
        private string _folder;
        private int _frame;

        private sealed class Setup
        {
            public string Name;
            public string HeroId;
            public bool Puppet;
            public string Weapon;
        }

        private static readonly Setup[] Setups =
        {
            new Setup { Name = "vanguard-frames", Weapon = "weapon_sword" },
            new Setup { Name = "vanguard-puppet", Weapon = "weapon_sword", Puppet = true },
            new Setup { Name = "arcanist", HeroId = "hero_cinder_arcanist", Weapon = "weapon_staff" },
        };

        [UnityTest, Timeout(1800000)]
        public IEnumerator CaptureEveryHeroPresentation()
        {
            Time.captureDeltaTime = 1f / 60;
            string only = System.Environment.GetEnvironmentVariable("HERO_MOTION_ONLY");
            foreach (Setup setup in Setups)
            {
                if (!string.IsNullOrEmpty(only) && !only.Contains(setup.Name))
                    continue;
                TestProfile.Begin(new PlayerProfile(0, null, null, 0, new[] { "weapon_staff", "weapon_daggers" },
                    setup.Weapon == "weapon_sword" ? null : setup.Weapon));
                File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
                if (setup.Puppet) File.WriteAllText(DevelopmentStart.PuppetPath, "on");
                if (setup.HeroId != null) File.WriteAllText(DevelopmentStart.HeroPath, setup.HeroId);
                Time.timeScale = 1;
                yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
                yield return null;
                _scene = SceneManager.GetActiveScene();
                _camera = Camera.main;
                _camera.GetComponent<ArenaCameraFollow>().enabled = false;
                var move = Object.FindFirstObjectByType<HeroMovementInput>();
                _hero = move.transform;
                Object.FindFirstObjectByType<EncounterController>().enabled = false;
                foreach (var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
                foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
                var attack = _hero.GetComponent<AttackController>();
                attack.enabled = false;
                foreach (var other in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) other.enabled = false;

                _folder = OutputRoot + setup.Name + "/";
                if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
                Directory.CreateDirectory(_folder);
                _frame = 0;
                var marks = new List<string>();

                marks.Add("idle " + _frame);
                for (int i = 0; i < 90; i++) { yield return null; Shot(i); }
                foreach (var (label, steer) in new[] { ("walk-right", Vector2.right), ("walk-down", Vector2.down),
                             ("walk-left", Vector2.left), ("walk-up", Vector2.up) })
                {
                    marks.Add(label + " " + _frame);
                    move.Hold(steer);
                    for (int i = 0; i < 80; i++) { yield return null; Shot(i); }
                }
                move.Release();
                marks.Add("stop " + _frame);
                for (int i = 0; i < 40; i++) { yield return null; Shot(i); }

                // Attacks on the move: a target walks beside the hero so every weapon keeps finding it.
                var target = new GameObject("Capture target").AddComponent<Health>();
                target.Initialize(1000000);
                _hero.GetComponent<Targeting>().SetCandidates(new[] { target });
                attack.enabled = true;
                marks.Add("attack-walk " + _frame);
                for (int i = 0; i < 120; i++)
                {
                    if (i % 60 == 0) move.Hold(i / 60 % 2 == 0 ? Vector2.right : Vector2.left);
                    target.transform.position = _hero.position + new Vector3(.6f, .2f, 0f);
                    yield return null;
                    Shot(i);
                }
                move.Release();
                marks.Add("attack-stand " + _frame);
                for (int i = 0; i < 90; i++)
                {
                    target.transform.position = _hero.position + new Vector3(.6f, .2f, 0f);
                    yield return null;
                    Shot(i);
                }
                attack.enabled = false;

                // The ability, among burning targets for a Vent, plain ones for a burst.
                var dummies = new List<Health>();
                for (int i = 0; i < 4; i++)
                {
                    var dummy = new GameObject("Capture dummy " + i).AddComponent<Health>();
                    dummy.Initialize(1000000);
                    float angle = i * Mathf.PI * .5f + .4f;
                    dummy.transform.position = _hero.position + new Vector3(Mathf.Cos(angle) * 1.3f, Mathf.Sin(angle) * .9f, 0f);
                    dummies.Add(dummy);
                }
                _hero.GetComponent<Targeting>().SetCandidates(dummies.ToArray());
                foreach (var dummy in dummies)
                    for (int s = 0; s < 3; s++) BurnStatus.On(dummy)?.Ignite(1, false, _hero.GetComponent<Health>(), null);
                marks.Add("ability " + _frame);
                for (int i = 0; i < 20; i++) { yield return null; Shot(i); }
                Assert.That(_hero.GetComponent<AbilityController>().TryUse(), Is.True, setup.Name + " fires its ability.");
                for (int i = 0; i < 70; i++) { yield return null; Shot(i); }

                File.WriteAllLines(_folder + "marks.txt", marks);
                Time.captureDeltaTime = 1f / 60;
                SceneManager.SetActiveScene(SceneManager.CreateScene("Hero capture between " + setup.Name));
                yield return SceneManager.UnloadSceneAsync(_scene);
                TestProfile.End();
            }
        }

        // Every Every-th frame, framed on the hero: the camera follows at a fixed size so motion reads against the floor.
        private void Shot(int step)
        {
            if (step % Every != 0) return;
            Vector3 position = _camera.transform.position;
            float size = _camera.orthographicSize;
            _camera.transform.position = new Vector3(_hero.position.x, _hero.position.y + .55f, position.z);
            _camera.orthographicSize = 1.35f;
            var target = RenderTexture.GetTemporary(Size, Size, 24);
            var before = RenderTexture.active;
            float aspect = _camera.aspect;
            _camera.aspect = 1f;
            _camera.targetTexture = target;
            _camera.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            pixels.Apply();
            // The project ships without Unity's image-conversion module, so the frame is written as a plain 24-bit BMP.
            using (var writer = new BinaryWriter(File.Create($"{_folder}{_frame:D4}.bmp")))
            {
                int bytes = Size * Size * 3;
                writer.Write((ushort)0x4d42); writer.Write(bytes + 54); writer.Write(0); writer.Write(54);
                writer.Write(40); writer.Write(Size); writer.Write(Size); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(bytes); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                foreach (Color32 pixel in pixels.GetPixels32()) { writer.Write(pixel.b); writer.Write(pixel.g); writer.Write(pixel.r); }
            }
            _frame++;
            Object.Destroy(pixels);
            _camera.targetTexture = null;
            _camera.aspect = aspect;
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(target);
            _camera.transform.position = position;
            _camera.orthographicSize = size;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Hero capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
