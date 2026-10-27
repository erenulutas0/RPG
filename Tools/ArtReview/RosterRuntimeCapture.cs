// Graphics-enabled PlayMode fixture (Run-RosterRuntimeProof.ps1 installs it for one run and removes it): the foundry's
// painted roster in the game itself at the phone's 1080x2340 framing - the density floor's wave with two each of the
// Runner, the Tank and the Grunt Captain walking in round the Vanguard, then round Ilya, and the gate boss's slam in
// both phases - as BMP frames for review. The horde's blows are held and the hero kept on its feet; nothing here ships.
using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cryptforge.Tests
{
    public sealed class RosterRuntimeCapture
    {
        private const int Width = 1080;
        private const int Height = 2340;
        private const string Out = "TestResults/roster-runtime";
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private static readonly string[] Kinds = { "Enemy_Runner", "Enemy_Tank", "Enemy_GruntCaptain" };

        [UnityTest, Timeout(900000)]
        public IEnumerator CaptureTheCrowdAndTheWarden()
        {
            Directory.CreateDirectory(Out);
            foreach (string hero in new[] { "vanguard", "hero_cinder_arcanist" })
            {
                yield return Load("floor_density_proof", hero == "vanguard" ? null : hero);
                var setup = Object.FindFirstObjectByType<CombatSetup>();
                Transform heroBody = Object.FindFirstObjectByType<HeroMovementInput>().transform;
                Health heroHealth = heroBody.GetComponent<Health>();
                // Two of each newly painted kind walking in from a ring round the hero, beside the floor's own wave.
                var walkers = new System.Collections.Generic.List<Transform>();
                for (int i = 0; i < 6; i++)
                {
                    float angle = (i * 60f + 30f) * Mathf.Deg2Rad;
                    Vector3 from = heroBody.position + new Vector3(Mathf.Cos(angle) * 3.6f, Mathf.Sin(angle) * 3.6f * ArenaFloor.DepthScale, 0f);
                    walkers.Add(Spawn(Kinds[i % Kinds.Length], from));
                }
                float shots = 0f;
                for (int frame = 0; frame < 60 * 5; frame++)
                {
                    Keep(setup, heroHealth);
                    foreach (Transform walker in walkers)
                    {
                        if (walker == null) continue;
                        Vector3 delta = heroBody.position - walker.position;
                        float floor = new Vector2(delta.x, delta.y / ArenaFloor.DepthScale).magnitude;
                        if (floor > 1.5f) walker.position += delta / floor * (1.1f / 60f);
                    }
                    yield return null;
                    float t = frame / 60f;
                    if (t >= 1.5f + shots * 1.5f && shots < 3f)
                    {
                        Keep(setup, heroHealth);
                        Shoot($"{Out}/crowd-{hero}-{shots:0}.bmp");
                        shots++;
                    }
                }
                yield return Unload();
            }

            yield return Load("floor_gate_boss_proof", null);
            {
                var setup = Object.FindFirstObjectByType<CombatSetup>();
                Health heroHealth = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
                for (int frame = 0; frame < 60 * 40 && !(setup.Warden.IsFighting && !setup.Encounters.IsRevealHeld); frame++)
                {
                    Keep(setup, heroHealth);
                    yield return null;
                }
                Assert.That(setup.Warden.IsFighting, Is.True, "The gate boss is fought.");
                Shoot($"{Out}/warden-fight.bmp");
                for (int phase = 0; phase < 2; phase++)
                {
                    if (phase == 1)
                    {
                        Health boss = setup.Warden.Boss;
                        boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
                    }
                    yield return Until(setup, heroHealth, () => !setup.Warden.Clock.IsMarked, 20f);
                    yield return Until(setup, heroHealth, () => setup.Warden.Clock.IsMarked, 20f);
                    for (int i = 0; i < 36; i++) { Keep(setup, heroHealth); yield return null; }
                    Shoot($"{Out}/warden-windup-{phase}.bmp");
                    yield return Until(setup, heroHealth, () => !setup.Warden.Clock.IsMarked, 20f);
                    for (int i = 0; i < 4; i++) { Keep(setup, heroHealth); yield return null; }
                    Shoot($"{Out}/warden-slam-{phase}.bmp");
                }
            }
            yield return Unload();
        }

        private static IEnumerator Load(string floor, string hero)
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, floor);
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (hero != null) File.WriteAllText(DevelopmentStart.HeroPath, hero);
            Time.timeScale = 1; Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            // The phone's own framing (ArtHudFlowTests' bands), not the batch window's.
            Object.FindFirstObjectByType<ArenaCameraFollow>().Frame(Width, Height, 285f, Height - 100f - 327f);
        }

        private static IEnumerator Unload()
        {
            Time.timeScale = 1; Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Roster runtime cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private static IEnumerator Until(CombatSetup setup, Health hero, System.Func<bool> condition, float seconds)
        {
            for (int frame = 0; frame < seconds * 60 && !condition(); frame++)
            {
                Keep(setup, hero);
                yield return null;
            }
        }

        // The run keeps going for the camera: offers are answered at once, the hero is kept on its feet and the horde's
        // own blows are held, so nothing but the hero's strikes lands.
        private static void Keep(CombatSetup setup, Health hero)
        {
            for (int i = 0; i < 8 && setup.Choices.Current != null && setup.Choices.Current.Kind == ChoiceKind.Upgrade; i++)
                setup.Choices.TrySelect(setup.Choices.Current, 0);
            if (hero != null && hero.IsAlive) hero.Heal(hero.Maximum);
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                if (attack.gameObject != hero.gameObject) attack.enabled = false;
        }

        private static Transform Spawn(string definitionName, Vector3 at)
        {
#if UNITY_EDITOR
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/" + definitionName + ".asset");
            Health enemy = Object.Instantiate(definition.Prefab, at, Quaternion.identity);
            enemy.Initialize(definition.MaximumHealth * 50f);
            var attack = enemy.GetComponent<AttackController>();
            attack.Initialize(definition.Weapon.CreateRuntime());
            attack.enabled = false;
            return enemy.transform;
#else
            return null;
#endif
        }

        // Every overlay canvas is drawn through the camera into a texture the phone's size, then put back.
        private static void Shoot(string path)
        {
            // The boss's reveal hands the camera back framed for the batch window; frame it for the phone again.
            Object.FindFirstObjectByType<ArenaCameraFollow>().Frame(Width, Height, 285f, Height - 100f - 327f);
            var target = new RenderTexture(Width, Height, 24);
            var camera = Camera.main;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode;
                if (canvases[i].isRootCanvas && modes[i] == RenderMode.ScreenSpaceOverlay)
                {
                    canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                    canvases[i].worldCamera = camera;
                    canvases[i].planeDistance = 1f;
                }
            }
            Canvas.ForceUpdateCanvases();
            var before = RenderTexture.active;
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            pixels.Apply();
            using (var writer = new BinaryWriter(File.Create(path)))
            {
                int row = Width * 3;
                int bytes = row * Height;
                writer.Write((ushort)0x4d42); writer.Write(bytes + 54); writer.Write(0); writer.Write(54);
                writer.Write(40); writer.Write(Width); writer.Write(Height); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(bytes); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                foreach (Color32 pixel in pixels.GetPixels32()) { writer.Write(pixel.b); writer.Write(pixel.g); writer.Write(pixel.r); }
            }
            Object.Destroy(pixels);
            camera.targetTexture = null;
            RenderTexture.active = before;
            target.Release();
            for (int i = 0; i < canvases.Length; i++)
                canvases[i].renderMode = modes[i];
        }
    }
}
