using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // Thorn Post's Cinder Bloom at its maul's reach (docs/52, the lead's decisions 2026-11-15: `bloom-m20-h70-r1.2` stops 1.1
    // from the hero - its reach 1.2 less PackMotion.ReachMargin; reach 1.0's 0.9 read poorly in this capture's first run).
    // On Floor_BloomProof dressed as the garden (ArenaView.ApplyFloorTheme, EnemyLookView.UseTheme, as the descent does on
    // floor 3): the Bloom walks up to the standing hero; then, time stopped, the hero is set down at the stopping distance on
    // four sides (left, right, below, above) and each is captured closed, opening (unfold-1) and open, close up and at the
    // game's portrait framing, with the two bodies' sprite bounds (the body renderers alone: no weapon, shadow or bar) and
    // their overlap written beside them. The open Bloom turns to the hero (CinderBloomView.FacesRear / FacesMirrored); the
    // closed one keeps the facing of its walk, as every enemy does while it stands, so the closed shots on the sides it did
    // not walk from face away. Writes BMPs; Run-ThornPostProof.ps1 converts them to PNG.
    public sealed class ThornPostCapture
    {
        const string Root = "ArtDirection/2026-11-15/thorn-post-01/";
        Scene _scene;

        [UnityTest] public IEnumerator CaptureThornPost()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_bloom_proof");
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync("Assets/_Project/Scenes/Gameplay/Gameplay.unity");
            yield return null;
            _scene = SceneManager.GetActiveScene();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var arena = Object.FindFirstObjectByType<ArenaView>();
            Dress(arena, FloorTheme.Gloam);
            EncounterController encounter = setup.Encounters;
            var input = Object.FindFirstObjectByType<HeroMovementInput>();
            var hero = input.GetComponent<Health>();
            float deadline = Time.realtimeSinceStartup + 30f;
            Health body = null;
            float reach = 0f;
            while (Time.realtimeSinceStartup < deadline)
            {
                body = encounter.EliteLeader;
                if (body != null && encounter.TryFloorPositionOf(body, out float bx, out float by))
                {
                    reach = Mathf.Sqrt(ArenaFloor.FloorDistanceSquared(bx, by, encounter.HeroFloorX, encounter.HeroFloorY));
                    if (reach <= body.GetComponent<AttackController>().Weapon.Range + .05f)
                        break;
                }
                if (hero.IsAlive)
                    hero.Heal(hero.Maximum);
                yield return null;
            }
            Assert.That(body, Is.Not.Null, "the Bloom walked in");
            Dress(arena, FloorTheme.Gloam);
            var bloom = body.GetComponent<CinderBloom>();
            var view = body.GetComponent<CinderBloomView>();
            var look = body.GetComponent<EnemyLookView>();
            Time.timeScale = 0;
            encounter.enabled = false;
            foreach (var a in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) a.enabled = false;
            input.enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)) canvas.enabled = false;
            var camera = Camera.main;
            var follow = camera.GetComponent<ArenaCameraFollow>();
            var report = new System.Text.StringBuilder();
            var invariant = System.Globalization.CultureInfo.InvariantCulture;
            report.AppendLine(string.Format(invariant, "walked-up distance (floor units): {0:0.000}; maul reach {1:0.00}; theme {2}", reach,
                body.GetComponent<AttackController>().Weapon.Range, arena.Theme));
            SpriteRenderer bloomBody = Body(look);
            Assert.That(encounter.TryFloorPositionOf(body, out float x, out float y), Is.True);
            float stop = body.GetComponent<AttackController>().Weapon.Range - PackMotion.ReachMargin;
            var sides = new (string Name, float Dx, float Dy)[] { ("left", -1f, 0f), ("right", 1f, 0f), ("below", 0f, -1f), ("above", 0f, 1f) };
            foreach ((string name, float dx, float dy) in sides)
            {
                float hx = x + dx * stop, hy = y + dy * stop;
                input.Motion.Place(hx, hy, arena.Geometry);
                hero.transform.position = new Vector3(hx, ArenaFloor.WorldY(hy), 0f);
                foreach ((string state, float age) in new[] { ("closed", 1f), ("opening", BloomRule.ClosedSeconds + .1f), ("open", BloomRule.ClosedSeconds + 1f) })
                {
                    bloom.Clock.Reset();
                    bloom.Clock.Step(age);
                    yield return null;
                    Vector3 mid = (hero.transform.position + body.transform.position) * .5f;
                    camera.transform.position = new Vector3(mid.x, mid.y + .5f, -10);
                    camera.orthographicSize = 1.6f;
                    Capture(camera, arena, $"{name}-{state}-close", 1200, 1200);
                    follow.enabled = true;
                    yield return null;
                    follow.Frame(1080, 2340, 361, 1913);
                    follow.enabled = false;
                    Capture(camera, arena, $"{name}-{state}-portrait", 1080, 2340);
                    Bounds b = bloomBody.bounds, h = HeroBody(hero);
                    float ox = Mathf.Max(0f, Mathf.Min(b.max.x, h.max.x) - Mathf.Max(b.min.x, h.min.x));
                    float oy = Mathf.Max(0f, Mathf.Min(b.max.y, h.max.y) - Mathf.Max(b.min.y, h.min.y));
                    bool rear = view.OpenFrame >= 0 ? view.FacesRear : look.RearFacing;
                    bool mirrored = view.OpenFrame >= 0 ? view.FacesMirrored : look.Mirrored;
                    report.AppendLine(string.Format(invariant,
                        "{0} {1}: open frame {2}, rear {3}, mirrored {4}, hero nearer the camera {5}; bloom body {6:0.00}x{7:0.00}, hero body {8:0.00}x{9:0.00}; " +
                        "overlap {10:0.00}x{11:0.00} = {12:0.000} sq units ({13:0}% of the hero's body box)",
                        name, state, view.OpenFrame, rear, mirrored, hero.transform.position.y < body.transform.position.y,
                        b.size.x, b.size.y, h.size.x, h.size.y, ox, oy, ox * oy, 100f * ox * oy / Mathf.Max(1e-4f, h.size.x * h.size.y)));
                }
            }
            Assert.That(bloom.IsCycling, Is.True, "the Bloom's clock ran as it walked in");
            File.WriteAllText(Root + "overlap.txt", report.ToString());
        }

        const System.Reflection.BindingFlags Internal = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

        // The hero's body as drawn: the cut-out puppet's parts when it wears one (PuppetAnimator.RootName; the painted
        // body renderer is empty then), else the look's body - weapons, shadow, rings and flashes left out.
        static Bounds HeroBody(Health hero)
        {
            Transform puppet = hero.transform.Find(PuppetAnimator.RootName);
            if (puppet == null)
                foreach (Transform child in hero.GetComponentsInChildren<Transform>())
                    if (child.name == PuppetAnimator.RootName)
                        puppet = child;
            if (puppet == null)
                return hero.GetComponent<HeroLookView>().Body.bounds;
            Bounds bounds = default;
            bool any = false;
            foreach (var r in puppet.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!r.enabled || r.sprite == null || r.name.Contains("Flash"))
                    continue;
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }
            return bounds;
        }

        // An enemy look's body renderer (internal to the game's assembly: reached by reflection).
        static SpriteRenderer Body(EnemyLookView look) =>
            (SpriteRenderer)typeof(EnemyLookView).GetField("_body", Internal).GetValue(look);

        static void Dress(ArenaView arena, FloorTheme theme)
        {
            arena.ApplyFloorTheme(theme);
            var use = typeof(EnemyLookView).GetMethod("UseTheme", Internal);
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None)) use.Invoke(look, new object[] { theme });
        }

        static void Capture(Camera c, ArenaView arena, string name, int width, int height)
        {
            c.aspect = (float)width / height;
            arena.Backdrop.FrameCavern();
            var rt = RenderTexture.GetTemporary(width, height, 24);
            var previous = RenderTexture.active;
            c.targetTexture = rt;
            c.Render();
            RenderTexture.active = rt;
            var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            pixels.Apply();
            using (var w = new BinaryWriter(File.Create(Root + name + ".bmp")))
            {
                int bytes = width * height * 3;
                w.Write((ushort)0x4d42); w.Write(bytes + 54); w.Write(0); w.Write(54);
                w.Write(40); w.Write(width); w.Write(height); w.Write((ushort)1); w.Write((ushort)24);
                w.Write(0); w.Write(bytes); w.Write(0); w.Write(0); w.Write(0); w.Write(0);
                foreach (Color32 p in pixels.GetPixels32()) { w.Write(p.b); w.Write(p.g); w.Write(p.r); }
            }
            Object.DestroyImmediate(pixels);
            c.targetTexture = null;
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt);
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0f;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Thorn Post capture cleanup"));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
