using System.Collections;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Cryptforge.Tests
{
    // The game-feel pass, stage 3 (docs/43 section 1, 2026-11-22): an enemy struck tips away from the blow beside its
    // compression and flash and rights itself (EnemyLookView); a fallen enemy dissolves in ash motes over about 0.3 s over
    // its remains, fewer a death as a crowd falls together; a horde newcomer rises out of embers at its feet as it fades
    // in (CombatEffectsView). The motes keep slots of their own in the effects' pool: none is made after the pool, and
    // nothing outlives the scene.
    public sealed class EnemyFeelTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private CombatEffectsView _effects;

        [UnitySetUp] public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown] public IEnumerator End()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0f;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Enemy feel cleanup " + System.Guid.NewGuid().ToString("N")));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The density proof's wave standing still: no encounter, no attacks.
        private IEnumerator LoadStanding()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
        }

        private static EnemyLookView PaintedEnemy()
        {
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.PaintedArt != null && !look.PaintedArt.HasFall && look.GetComponent<Health>().IsAlive)
                    return look;
            return null;
        }

        private static float Tip(EnemyLookView look) => Mathf.DeltaAngle(0f, look.Body.transform.localEulerAngles.z);

        // The disc motes on show in the effects' pool.
        private static int MotesOnShow()
        {
            int shown = 0;
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == FeelArt.Disc())
                    shown++;
            return shown;
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AStruckEnemyTipsAwayFromTheBlowAndRightsItself()
        {
            yield return LoadStanding();
            EnemyLookView look = PaintedEnemy();
            Assert.That(look, Is.Not.Null);
            Health health = look.GetComponent<Health>();
            var striker = new GameObject("Enemy feel striker").AddComponent<Health>();
            striker.Initialize(100f);

            // From the left: the top tips right, clockwise.
            striker.transform.position = look.transform.position + Vector3.left;
            health.ApplyDamage(new DamageContext(.01f, striker));
            yield return null;
            Assert.That(Tip(look), Is.LessThan(-2f), "Struck from the left it tips right, away from the blow.");
            Assert.That(Mathf.Abs(Tip(look)), Is.LessThanOrEqualTo(EnemyLookView.FlinchDegrees + 1e-3f));
            Assert.That(look.Body.transform.localScale.y, Is.LessThan(1f), "Beside its compression.");
            for (int i = 0; i < 12; i++)
                yield return null;
            Assert.That(look.Body.transform.localRotation, Is.EqualTo(Quaternion.identity), "Upright again.");

            // From the right: the other way.
            striker.transform.position = look.transform.position + Vector3.right;
            health.ApplyDamage(new DamageContext(.01f, striker));
            yield return null;
            Assert.That(Tip(look), Is.GreaterThan(2f), "Struck from the right it tips left.");
            for (int i = 0; i < 12; i++)
                yield return null;
            Assert.That(look.Body.transform.localRotation, Is.EqualTo(Quaternion.identity));

            // A burn's tick, or a blow from no one, is no blow.
            health.ApplyDamage(new DamageContext(.01f, striker, false, true));
            yield return null;
            Assert.That(look.Body.transform.localRotation, Is.EqualTo(Quaternion.identity), "A tick tips nothing.");
            health.ApplyDamage(new DamageContext(.01f));
            yield return null;
            Assert.That(look.Body.transform.localRotation, Is.EqualTo(Quaternion.identity), "Nor does a sourceless hit.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AFallenEnemyDissolvesInAshAndACrowdThrowsFewerFromItsOwnSlots()
        {
            yield return LoadStanding();
            // Whatever walked in as the scene loaded has risen whole.
            for (int i = 0; i < 30; i++)
                yield return null;
            Transform pool = GameObject.Find("Combat Effects").transform;
            int children = pool.childCount;
            int renderers = pool.GetComponentsInChildren<SpriteRenderer>(true).Length;
            Assert.That(_effects.ShownMotes, Is.Zero);
            EnemyLookView look = PaintedEnemy();
            Health health = look.GetComponent<Health>();
            float feet = look.transform.position.y;

            health.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(_effects.LastMotesPerDeath, Is.EqualTo(6), "A lone death throws six motes.");
            Assert.That(_effects.ShownMotes, Is.EqualTo(6));
            Assert.That(MotesOnShow(), Is.EqualTo(6), "Drawn with the disc, no painted frame.");
            float risen = 0f;
            for (int i = 0; i < 12; i++)
            {
                yield return null;
                foreach (var renderer in pool.GetComponentsInChildren<SpriteRenderer>())
                    if (renderer.enabled && renderer.sprite == FeelArt.Disc())
                    {
                        risen = Mathf.Max(risen, renderer.transform.position.y - feet);
                        Assert.That(renderer.color.a, Is.LessThanOrEqualTo(.9f + 1e-4f), "Translucent.");
                    }
            }
            Assert.That(risen, Is.GreaterThan(.3f), "They lift off the body.");
            for (int i = 0; i < 20; i++)
                yield return null;
            Assert.That(_effects.ShownMotes, Is.Zero, "Gone within half a second.");

            // Forty falls in one frame - the scene's whole crowd: past thirty at once, one mote each.
            MethodInfo defeated = typeof(CombatEffectsView).GetMethod("OnEnemyDefeated", Private);
            for (int i = 0; i < 40; i++)
                defeated.Invoke(_effects, new object[] { health });
            Assert.That(_effects.LastMotesPerDeath, Is.EqualTo(1), "A crowd's deaths throw one mote each.");
            Assert.That(_effects.ShownMotes, Is.LessThanOrEqualTo(CombatEffectsView.AshSlots), "Within the motes' own slots.");
            Assert.That(pool.childCount, Is.EqualTo(children), "Nothing made per death.");
            Assert.That(pool.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(renderers));
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.That(_effects.ShownMotes, Is.Zero);

            // Once the crowd has gone, a lone death is six again.
            EnemyLookView next = PaintedEnemy();
            Assert.That(next, Is.Not.Null);
            next.GetComponent<Health>().ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(_effects.LastMotesPerDeath, Is.EqualTo(6));

            // The restart: the next scene's pool is as big and holds nothing of this one's.
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            Assert.That(Object.FindObjectsByType<CombatEffectsView>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(GameObject.Find("Combat Effects").transform.childCount, Is.EqualTo(children));
            Assert.That(Object.FindFirstObjectByType<CombatEffectsView>().ShownMotes, Is.Zero);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(90000)]
        public IEnumerator AHordeNewcomerRisesOutOfEmbers()
        {
            File.WriteAllText(DevelopmentStart.RegionPreviewPath, "foundry");
            Time.captureDeltaTime = 1f / 60f;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            Object.FindFirstObjectByType<HeroMovementInput>().enabled = false;
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
            EncounterController encounters = Object.FindFirstObjectByType<CombatSetup>().Encounters;
            Health fresh = null;
            int embers = 0, shown = 0;
            float arriving = 1f;
            // The effects view listened first: the newcomer's embers are up by the time this hears of it.
            System.Action<int> spawned = slot =>
            {
                if (fresh != null)
                    return;
                fresh = encounters.HordeEnemyAt(slot);
                embers = _effects.LastEmbersPerArrival;
                shown = _effects.ShownMotes;
                arriving = fresh.GetComponent<EnemyLookView>().ArrivalProgress;
            };
            encounters.HordeEnemySpawned += spawned;
            float deadline = Time.realtimeSinceStartup + 20f;
            while (fresh == null && Time.realtimeSinceStartup < deadline)
                yield return null;
            encounters.HordeEnemySpawned -= spawned;
            Assert.That(fresh, Is.Not.Null, "A horde enemy walks in.");
            Assert.That(embers, Is.GreaterThanOrEqualTo(1), "It rises out of embers.");
            Assert.That(shown, Is.GreaterThanOrEqualTo(embers));
            Assert.That(arriving, Is.LessThan(1f), "While it fades in, as before.");
            for (int i = 0; i < 30; i++)
                yield return null;
            Assert.That(fresh.GetComponent<EnemyLookView>().ArrivalProgress, Is.EqualTo(1f), "Whole within half a second.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
