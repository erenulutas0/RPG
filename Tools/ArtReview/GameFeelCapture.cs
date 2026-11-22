// Temporary graphics-enabled PlayMode fixture (Tools/ArtReview/Run-GameFeelProof.ps1 installs and removes it): records
// the game-feel pass of 2026-11-22 (docs/43 section 1) in the foundry as numbered frames for review - the slot cues of
// a Sword, Staff and Daggers held beside the first weapon, the hero's recoil, level pop, picked weapon and death into
// ash, an enemy's flinch, dissolve and a horde newcomer rising out of embers, the Forge Warden's tell through a
// telegraph, its phase two and its fall, and the Glass Regent's cast, feet ring and shatter. Every frame is framed on
// its subject at a fixed size so motion reads against the floor. Nothing here ships; nothing is saved to the project.
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    public sealed class GameFeelCapture
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string Output = "ArtDirection/2026-11-22/game-feel-01/";
        private const int Size = 540;
        private Camera _camera;
        private Scene _scene;
        private readonly List<string> _frames = new List<string>();
        private readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        [UnityTest, Timeout(1800000)]
        public IEnumerator CaptureTheGameFeelPass()
        {
            Directory.CreateDirectory(Output);
            string only = System.Environment.GetEnvironmentVariable("GAME_FEEL_ONLY");
            if (Wanted(only, "hero")) yield return Hero();
            if (Wanted(only, "enemy")) yield return Enemies();
            if (Wanted(only, "horde")) yield return Horde();
            if (Wanted(only, "warden")) yield return Warden();
            if (Wanted(only, "regent")) yield return Regent();
            File.WriteAllLines(Output + "frames.txt", _frames);
        }

        private static bool Wanted(string only, string part) => string.IsNullOrEmpty(only) || only.Contains(part);

        private IEnumerator Open(string floor)
        {
            TestProfile.Begin();
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            if (floor != null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
                File.WriteAllText(DevelopmentStart.StartFloorPath, floor);
            }
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _scene = SceneManager.GetActiveScene();
            _camera = Camera.main;
            _camera.GetComponent<ArenaCameraFollow>().enabled = false;
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
        }

        private IEnumerator Close(string name)
        {
            SceneManager.SetActiveScene(SceneManager.CreateScene("Game feel capture between " + name + " " +
                System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(_scene);
            TestProfile.End();
        }

        private static void StopAttacks()
        {
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
        }

        // Takes each open choice's first card that is not a new weapon. The hero section fills the later slots itself
        // (AttackController.AddWeapon), outside the loadout the offers count, so a weapon card taken after that would be
        // a fourth weapon, and an attacker fires at most three (WeaponLoadout.MaxSlots). An offer always holds a card that
        // is not a weapon: only two weapons can be new beside the first.
        private static void ResolveChoices(CombatSetup setup)
        {
            for (int i = 0; i < 8 && setup.Choices.IsOpen; i++) setup.Choices.TrySelect(setup.Choices.Current, NotAWeapon(setup));
        }

        private static int NotAWeapon(CombatSetup setup)
        {
            UpgradeOffer offer = setup.Choices.Current.Kind == ChoiceKind.Upgrade ? setup.Upgrades.CurrentOffer : null;
            if (offer == null) return 0;
            for (int c = 0; c < offer.Choices.Count; c++)
                if (offer.Choices[c].Category != OfferCategory.NewWeapon) return c;
            return 0;
        }

        // Frames of a section, one a frame (or every second frame), framed on a subject.
        private IEnumerator Frames(string section, int count, System.Func<Vector3> focus, float size, int every = 1,
            CombatSetup setup = null)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
                if (setup != null) ResolveChoices(setup);
                if (i % every == 0) Shot(section, focus(), size);
            }
        }

        private IEnumerator Hero()
        {
            yield return Open(null);
            StopAttacks();
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var enemy in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None)) enemy.gameObject.SetActive(false);
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().GetComponent<Health>();
            var feel = hero.GetComponent<HeroFeelView>();
            System.Func<Vector3> on = () => hero.transform.position + Vector3.up * .6f;
            var target = new GameObject("Capture target").AddComponent<Health>();
            target.Initialize(1000000);
            target.transform.position = hero.transform.position + new Vector3(.9f, .1f, 0f);
            yield return Frames("hero-00-stand", 4, on, 1.35f);
            // Each later slot's cue, played as its weapon's strike would play it, toward the target on the right.
            foreach (var (name, cue) in new[] { ("flourish", SlotCue.Flourish), ("pulse", SlotCue.Pulse), ("flick", SlotCue.Flick) })
            {
                feel.Play(cue, target, 1f);
                yield return Frames("slot-" + name, 18, on, 1.35f);
            }
            // The cues among real strikes: the Staff and the Daggers fired beside the first weapon for two seconds.
            var attack = hero.GetComponent<AttackController>();
            // Only into free slots: the attacker fires at most three weapons, the first among them.
            if (attack.WeaponCount < WeaponLoadout.MaxSlots)
                attack.AddWeapon(new WeaponRuntime(1f, .7f, 3f, 0f, new AttackPattern(WeaponBehavior.Area, 1f, .5f)));
            if (attack.WeaponCount < WeaponLoadout.MaxSlots)
                attack.AddWeapon(new WeaponRuntime(1f, .45f, 3f, 0f, new AttackPattern(WeaponBehavior.DirectHit)));
            hero.GetComponent<Targeting>().SetCandidates(new[] { target });
            attack.enabled = true;
            yield return Frames("slot-strikes", 120, on, 1.35f, 2);
            attack.enabled = false;
            // A blow from the right.
            var striker = new GameObject("Capture striker").AddComponent<Health>();
            striker.Initialize(100);
            striker.transform.position = hero.transform.position + Vector3.right;
            yield return Frames("hero-hit-before", 4, on, 1.35f);
            hero.ApplyDamage(new DamageContext(1f, striker));
            yield return Frames("hero-hit", 16, on, 1.35f);
            // A level.
            setup.Run.AddExperience(setup.Run.ExperienceForNextLevel - setup.Run.Experience);
            ResolveChoices(setup);
            yield return Frames("hero-level", 30, on, 1.35f, 1, setup);
            // A weapon picked up (the view's own handler, as an offer's pick calls it).
            typeof(HeroFeelView).GetMethod("OnWeaponAcquired", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(feel, new object[] { null, new WeaponRuntime(1f, 1f, 1f, 0f, new AttackPattern(WeaponBehavior.Cleave, 1f, .5f)) });
            yield return Frames("hero-pickup", 46, () => hero.transform.position + Vector3.up * 1f, 1.5f);
            // Death: the collapse, then the ash.
            hero.ApplyDamage(new DamageContext(100000));
            yield return Frames("hero-death", 110, () => hero.transform.position + Vector3.up * .5f, 1.5f);
            yield return Close("hero");
        }

        private IEnumerator Enemies()
        {
            yield return Open("floor_density_proof");
            StopAttacks();
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            yield return Frames("enemy-settle", 30, () => Vector3.zero, 2f, 30);
            EnemyLookView look = null;
            foreach (var candidate in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (candidate.PaintedArt != null && !candidate.PaintedArt.HasFall) { look = candidate; break; }
            Assert.That(look, Is.Not.Null, "An enemy of the density proof.");
            var health = look.GetComponent<Health>();
            var striker = new GameObject("Capture striker").AddComponent<Health>();
            striker.Initialize(100);
            Vector3 at = look.transform.position;
            System.Func<Vector3> on = () => at + Vector3.up * .5f;
            striker.transform.position = at + Vector3.left;
            health.ApplyDamage(new DamageContext(.01f, striker));
            yield return Frames("enemy-flinch-from-left", 10, on, 1.1f);
            striker.transform.position = at + Vector3.right;
            health.ApplyDamage(new DamageContext(.01f, striker));
            yield return Frames("enemy-flinch-from-right", 10, on, 1.1f);
            health.ApplyDamage(new DamageContext(100000));
            yield return Frames("enemy-dissolve", 40, on, 1.1f);
            // A crowd falling at once: one mote each past thirty.
            var hero = Object.FindFirstObjectByType<HeroMovementInput>().transform;
            foreach (var other in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
            {
                var h = other.GetComponent<Health>();
                if (h.IsAlive) h.ApplyDamage(new DamageContext(100000));
            }
            yield return Frames("enemy-crowd-dissolve", 40, () => hero.position, 4f, 2);
            yield return Close("enemy");
        }

        private IEnumerator Horde()
        {
            yield return Open(null);
            StopAttacks();
            var encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            Health fresh = null;
            System.Action<int> spawned = slot => { if (fresh == null) fresh = encounters.HordeEnemyAt(slot); };
            encounters.HordeEnemySpawned += spawned;
            float deadline = Time.realtimeSinceStartup + 30f;
            while (fresh == null && Time.realtimeSinceStartup < deadline) yield return null;
            encounters.HordeEnemySpawned -= spawned;
            Assert.That(fresh, Is.Not.Null, "A horde enemy walks in.");
            Health watched = fresh;
            Shot("horde-arrival", watched.transform.position + Vector3.up * .5f, 1.1f);
            yield return Frames("horde-arrival", 24, () => watched.transform.position + Vector3.up * .5f, 1.1f);
            yield return Close("horde");
        }

        private IEnumerator Until(CombatSetup setup, System.Func<bool> condition, float seconds)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline)
            {
                ResolveChoices(setup);
                yield return null;
            }
        }

        private IEnumerator Warden()
        {
            yield return Open("floor_gate_boss_proof");
            StopAttacks();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            WardenSlamService service = setup.Warden;
            yield return Until(setup, () => service.IsFighting && !setup.Encounters.IsRevealHeld, 30f);
            Health boss = service.Boss;
            Assert.That(boss, Is.Not.Null, "The Warden is fought.");
            System.Func<Vector3> on = () => boss.transform.position + Vector3.up * .9f;
            yield return Until(setup, () => service.Clock.IsMarked, WardenRule.SlamEvery + 5f);
            // The tell, the blow and the kick, framed on the boss.
            yield return Frames("warden-tell", 100, on, 2.2f, 2, setup);
            boss.ApplyDamage(new DamageContext(boss.Maximum * .55f));
            yield return Frames("warden-phase-two", 50, on, 2.2f, 2, setup);
            yield return Until(setup, () => service.Clock.IsMarked, WardenRule.SlamEveryEnraged + 5f);
            yield return Frames("warden-tell-phase-two", 90, on, 2.2f, 2, setup);
            Vector3 fell = boss.transform.position;
            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            yield return Frames("warden-death", 160, () => fell + Vector3.up * .7f, 2.4f, 2, setup);
            yield return Close("warden");
        }

        private IEnumerator Regent()
        {
            yield return Open("floor_regent_proof");
            StopAttacks();
            var setup = Object.FindFirstObjectByType<CombatSetup>();
            WardenSlamService service = setup.Warden;
            yield return Until(setup, () => service.IsFighting && !setup.Encounters.IsRevealHeld, 30f);
            Health boss = service.Boss;
            Assert.That(boss, Is.Not.Null, "The Regent is fought.");
            System.Func<Vector3> on = () => boss.transform.position + Vector3.up * 1f;
            yield return Frames("regent-feet-ring", 6, on, 2.6f, 2, setup);
            yield return Until(setup, () => service.Clock.IsMarked, RegentRule.Every + 5f);
            yield return Frames("regent-cast", 120, on, 2.8f, 2, setup);
            Vector3 fell = boss.transform.position;
            boss.ApplyDamage(new DamageContext(boss.Maximum * 10f));
            yield return Frames("regent-shatter", 150, () => fell + Vector3.up * .9f, 2.6f, 2, setup);
            yield return Close("regent");
        }

        // One frame of a section, framed on focus at a fixed orthographic size, square.
        private void Shot(string section, Vector3 focus, float size)
        {
            Vector3 position = _camera.transform.position;
            float ortho = _camera.orthographicSize;
            float aspect = _camera.aspect;
            _camera.transform.position = new Vector3(focus.x, focus.y, position.z);
            _camera.orthographicSize = size;
            _camera.aspect = 1f;
            var target = RenderTexture.GetTemporary(Size, Size, 24);
            var before = RenderTexture.active;
            _camera.targetTexture = target;
            _camera.Render();
            RenderTexture.active = target;
            var pixels = new Texture2D(Size, Size, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, Size, Size), 0, 0);
            pixels.Apply();
            _counts.TryGetValue(section, out int index);
            _counts[section] = index + 1;
            string name = $"{section}-{index:D3}";
            _frames.Add(name);
            // The project ships without Unity's image-conversion module, so the frame is written as a plain 24-bit BMP;
            // the runner turns it into a PNG.
            using (var writer = new BinaryWriter(File.Create(Output + name + ".bmp")))
            {
                int bytes = Size * Size * 3;
                writer.Write((ushort)0x4d42); writer.Write(bytes + 54); writer.Write(0); writer.Write(54);
                writer.Write(40); writer.Write(Size); writer.Write(Size); writer.Write((ushort)1); writer.Write((ushort)24);
                writer.Write(0); writer.Write(bytes); writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                foreach (Color32 pixel in pixels.GetPixels32()) { writer.Write(pixel.b); writer.Write(pixel.g); writer.Write(pixel.r); }
            }
            Object.Destroy(pixels);
            _camera.targetTexture = null;
            RenderTexture.active = before;
            RenderTexture.ReleaseTemporary(target);
            _camera.transform.position = position;
            _camera.orthographicSize = ortho;
            _camera.aspect = aspect;
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("Game feel capture cleanup " + System.Guid.NewGuid().ToString("N")));
                yield return SceneManager.UnloadSceneAsync(_scene);
            }
            TestProfile.End();
        }
    }
}
