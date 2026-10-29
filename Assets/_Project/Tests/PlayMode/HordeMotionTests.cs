using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
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
    // The horde's anticipation and deaths in the Gameplay scene (art round 3): each of the Foundry's five enemies, spawned
    // from its definition as the encounter spawns one, draws back to strike in the beat before each real blow on a
    // target in its reach - facing it, in its own windup - and never while the target stands out of reach; its blows land
    // on the same frames and for the same damage as the same enemy's without the windup; a crowd of all five falling at
    // once under a crowd's effects each plays its own death whole, from slots of their own, leaving the shared pool to
    // the sparks; and a set without the new parts strikes and falls in the two remains exactly as before.
    public sealed class HordeMotionTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;
        private static readonly string[] Roster = { "Enemy_Grunt", "Enemy_CinderMite", "Enemy_Runner", "Enemy_Tank", "Enemy_GruntCaptain" };
        private readonly List<Object> _spawned = new List<Object>();
        private Vector3 _hero;

        [UnitySetUp]
        public IEnumerator Load()
        {
            TestProfile.Begin();
            Directory.CreateDirectory(Path.GetDirectoryName(DevelopmentStart.StartFloorPath));
            File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.timeScale = 1; Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath); yield return null;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            _hero = GameObject.Find("Vanguard").transform.position;
        }

        [UnityTearDown]
        public IEnumerator Unload()
        {
            foreach (var spawned in _spawned) if (spawned != null) Object.Destroy(spawned);
            _spawned.Clear();
            Time.timeScale = 1; Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Horde motion cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene); TestProfile.End();
        }

        private static EnemyDefinition Definition(string name)
        {
#if UNITY_EDITOR
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>("Assets/_Project/Data/Enemies/" + name + ".asset");
            Assert.That(definition, Is.Not.Null, name);
            return definition;
#else
            Assert.Ignore("The roster's definitions are loaded through the editor's asset database.");
            return null;
#endif
        }

        // One enemy as EncounterController.CreateEnemy makes it, its attack off; set (when given) replaces its painted art
        // before it wakes, as WardenLookTests does.
        private EnemyLookView Spawn(EnemyDefinition definition, Vector3 at, EnemyArtSet set = null)
        {
            var holder = new GameObject("Inactive holder");
            holder.SetActive(false);
            Health enemy = Object.Instantiate(definition.Prefab, at, Quaternion.identity, holder.transform);
            enemy.Initialize(definition.MaximumHealth);
            var attack = enemy.GetComponent<AttackController>();
            attack.Initialize(definition.Weapon.CreateRuntime());
            attack.enabled = false;
            var look = enemy.GetComponent<EnemyLookView>();
            if (set != null) typeof(EnemyLookView).GetField("_paintedArt", Private).SetValue(look, set);
            enemy.transform.SetParent(null, true);
            Object.Destroy(holder);
            _spawned.Add(enemy.gameObject);
            return look;
        }

        // The set as it was before this round: no strike windup, no death of its own.
        private EnemyArtSet Bare(EnemyArtSet set)
        {
            EnemyArtSet bare = Object.Instantiate(set);
            foreach (string field in new[] { "_strikeWindup", "_deathFront", "_deathRear" })
                typeof(EnemyArtSet).GetField(field, Private).SetValue(bare, null);
            Assert.That(bare.IsValid, Is.True);
            Assert.That(bare.HasStrikeWindup || bare.HasOwnDeath, Is.False);
            _spawned.Add(bare);
            return bare;
        }

        private Health Target(Vector3 at)
        {
            var target = new GameObject("Strike target").AddComponent<Health>();
            target.Initialize(100000);
            target.transform.position = at;
            _spawned.Add(target.gameObject);
            return target;
        }

        private static SpriteRenderer BodyOf(EnemyLookView look) => look.transform.Find("Enemy Body").GetComponent<SpriteRenderer>();

        [UnityTest, Timeout(120000)]
        public IEnumerator TheWindupShowsBeforeEachRealStrikeInReachAndTheStrikeKeepsItsTime()
        {
            for (int r = 0; r < Roster.Length; r++)
            {
                EnemyDefinition definition = Definition(Roster[r]);
                // The same enemy twice, side by side: one with its windup, one with its set as it was.
                EnemyLookView look = Spawn(definition, _hero + new Vector3(-1.4f, -.9f));
                yield return null;
                EnemyArtSet set = look.PaintedArt;
                Assert.That(set != null && set.HasStrikeWindup, Is.True, Roster[r]);
                EnemyLookView plain = Spawn(definition, _hero + new Vector3(1.4f, -.9f), Bare(set));
                yield return null;
                // Each target up and to the right, well inside the weapon's reach.
                var offset = new Vector3(.3f, .15f);
                Health target = Target(look.transform.position + offset), plainTarget = Target(plain.transform.position + offset);
                look.GetComponent<Targeting>().SetCandidates(new[] { target });
                plain.GetComponent<Targeting>().SetCandidates(new[] { plainTarget });
                AttackController attack = look.GetComponent<AttackController>(), plainAttack = plain.GetComponent<AttackController>();
                WeaponRuntime weapon = attack.Weapon;
                float beat = Mathf.Min(set.StrikeWindupSeconds, weapon.Interval * .5f);
                attack.enabled = plainAttack.enabled = true;
                SpriteRenderer body = BodyOf(look);
                var counts = new List<int>();
                var windup = new List<bool>();
                bool facing = true, drawn = true, plainNever = true, sameDamage = true;
                int frames = Mathf.CeilToInt((definition.Weapon.InitialDelay + weapon.Interval * 2.5f) * 60) + 4;
                for (int k = 0; k < frames; k++)
                {
                    yield return null;
                    counts.Add(attack.AttackCount);
                    windup.Add(look.ShowsStrikeWindup);
                    Assert.That(plainAttack.AttackCount, Is.EqualTo(attack.AttackCount), Roster[r] + ": a blow on the same frame.");
                    sameDamage &= Mathf.Approximately(target.Current, plainTarget.Current);
                    plainNever &= !plain.ShowsStrikeWindup && plain.PaintedFrame < EnemyLookView.StrikeWindupFrame;
                    if (look.ShowsStrikeWindup)
                    {
                        facing &= look.RearFacing && look.Mirrored;
                        drawn &= body.sprite == set.GetStrikeWindup(look.RearFacing).Body;
                    }
                }
                attack.enabled = plainAttack.enabled = false;
                string at = Roster[r];
                Assert.That(counts[counts.Count - 1], Is.GreaterThanOrEqualTo(2), at + " struck at least twice.");
                Assert.That(sameDamage, Is.True, at + ": each blow lands for what it did before.");
                Assert.That(plainNever, Is.True, at + ": a set without the windup never shows one.");
                Assert.That(facing, Is.True, at + ": the windup faces its target.");
                Assert.That(drawn, Is.True, at + ": in the facing's own windup.");
                // Before every blow whose cooldown ran down in reach, the windup held for the beat, up to the blow.
                for (int k = 1; k < counts.Count; k++)
                {
                    if (counts[k] == counts[k - 1] || counts[k] < 2) continue;
                    int held = 0;
                    for (int j = k - 1; j >= 0 && windup[j]; j--) held++;
                    Assert.That(held / 60f, Is.EqualTo(beat).Within(2.5f / 60),
                        $"{at}: blow {counts[k]} came after {held} frames of windup against a beat of {beat:0.00} s.");
                }
                // And none while the cooldown had longer than the beat to run.
                int shown = 0;
                foreach (bool w in windup) if (w) shown++;
                Assert.That(shown / 60f, Is.LessThanOrEqualTo(beat * (counts[counts.Count - 1] + 1) + 3f / 60), at);
                Object.Destroy(look.gameObject); Object.Destroy(plain.gameObject);
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator TheWindupNeverShowsWhileTheTargetIsOutOfReach()
        {
            for (int r = 0; r < Roster.Length; r++)
            {
                EnemyDefinition definition = Definition(Roster[r]);
                EnemyLookView look = Spawn(definition, _hero + new Vector3(-1.2f + .6f * r, -.9f));
                yield return null;
                AttackController attack = look.GetComponent<AttackController>();
                // Just beyond the weapon's reach on the floor, to its side.
                Health target = Target(look.transform.position + new Vector3(attack.Weapon.Range + .4f, 0f));
                look.GetComponent<Targeting>().SetCandidates(new[] { target });
                attack.enabled = true;
                bool never = true;
                int frames = Mathf.CeilToInt((definition.Weapon.InitialDelay + attack.Weapon.Interval * 2f) * 60);
                for (int k = 0; k < frames; k++)
                {
                    yield return null;
                    never &= !look.ShowsStrikeWindup;
                }
                Assert.That(attack.AttackCount, Is.Zero, Roster[r] + ": nothing in reach, no blow.");
                Assert.That(never, Is.True, Roster[r] + ": and no windup either, though its cooldown has run out.");
                // In reach again, the very next blow lands at once (its cooldown long spent): presentation never holds it.
                target.transform.position = look.transform.position + new Vector3(.3f, 0f);
                yield return null; yield return null;
                Assert.That(attack.AttackCount, Is.EqualTo(1), Roster[r]);
                attack.enabled = false;
                Object.Destroy(look.gameObject);
                yield return null;
            }
            LogAssert.NoUnexpectedReceived();
        }

        // A crowd's worth of effects in one frame: the shared pool twice over, the skills' bursts and the arrivals' puffs.
        private static void Crowd(CombatEffectsView effects, Sprite[] marker)
        {
            MethodInfo show = typeof(CombatEffectsView).GetMethod("ShowMoving", Private);
            MethodInfo burst = typeof(CombatEffectsView).GetMethod("ShowBurst", Private);
            MethodInfo puff = typeof(CombatEffectsView).GetMethod("ShowArrivalPuff", Private);
            for (int i = 0; i < 8; i++)
                show.Invoke(effects, new object[] { marker, .05f, .3f, Vector3.zero, 5, Vector3.zero, 0f, false, 1f });
            for (int i = 0; i < 6; i++)
                burst.Invoke(effects, new object[] { marker, .05f, .3f, Vector3.zero, 5, Vector3.zero, 0f, 1f, false });
            puff.Invoke(effects, new object[] { Vector3.zero });
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator EachKindsDeathPlaysWholeAsACrowdFallsTogether()
        {
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            var watches = (System.Array)typeof(CombatEffectsView).GetField("_hordeWatches", Private).GetValue(effects);
            MethodInfo bind = typeof(CombatEffectsView).GetMethod("Bind", Private);
            MethodInfo defeated = typeof(CombatEffectsView).GetMethod("OnEnemyDefeated", Private);
            // Eight of each kind, forty in all - the scene's whole standing crowd - half of them turned away.
            var looks = new List<EnemyLookView>();
            for (int i = 0; i < 40; i++)
            {
                var at = _hero + new Vector3(-2f + (i % 8) * .55f, -1.4f + (i / 8) * .6f);
                looks.Add(Spawn(Definition(Roster[i % Roster.Length]), at));
            }
            yield return null;
            for (int k = 0; k < 3; k++)
            {
                for (int i = 0; i < looks.Count; i++) looks[i].transform.position += new Vector3(0f, i % 2 == 0 ? .05f : -.05f);
                yield return null;
            }
            for (int i = 0; i < looks.Count; i++)
            {
                Assert.That(looks[i].RearFacing, Is.EqualTo(i % 2 == 0), "Half walk away, half toward the camera.");
                bind.Invoke(effects, new[] { watches.GetValue(i), looks[i].GetComponent<Health>() });
            }
            // Every drawing of every death, to tell the deaths' renderers from the crowd's.
            var deathSprites = new HashSet<Sprite>();
            foreach (var look in looks)
                foreach (bool rear in new[] { false, true })
                    deathSprites.UnionWith(look.PaintedArt.GetOwnDeath(rear));
            Sprite[] marker = { looks[0].PaintedArt.GetFrame(0, false).Flash };
            var root = GameObject.Find("Combat Effects").transform;

            // All forty fall in one frame, as a Forge Burst over the crowd would fell them.
            foreach (var look in looks)
            {
                Health health = look.GetComponent<Health>();
                health.ApplyDamage(new DamageContext(health.Maximum * 10f));
                defeated.Invoke(effects, new object[] { health });
            }
            var deaths = new Dictionary<SpriteRenderer, List<Sprite>>();
            var lifetimes = new Dictionary<SpriteRenderer, int>();
            var expected = new Dictionary<SpriteRenderer, (EnemyArtSet set, bool rear)>();
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && deathSprites.Contains(renderer.sprite))
                {
                    deaths[renderer] = new List<Sprite> { renderer.sprite };
                    lifetimes[renderer] = 0;
                }
            Assert.That(deaths.Count, Is.EqualTo(40), "Every one of the forty begins its own death.");
            foreach (var look in looks)
                foreach (var renderer in deaths.Keys)
                    if (!expected.ContainsKey(renderer) && renderer.sprite == look.PaintedArt.GetOwnDeath(look.RearFacing)[0] &&
                        Vector2.Distance(renderer.transform.position, look.transform.position) < .01f)
                        expected[renderer] = (look.PaintedArt, look.RearFacing);
            Assert.That(expected.Count, Is.EqualTo(40), "Each where its enemy fell, in the facing it fell in.");

            // Meanwhile the crowd's effects keep coming, every frame, until the last death is gone.
            int frame = 0;
            bool any = true;
            while (any && frame < 90)
            {
                Crowd(effects, marker);
                yield return null;
                frame++;
                any = false;
                foreach (var renderer in deaths.Keys)
                {
                    if (!renderer.enabled || !deathSprites.Contains(renderer.sprite)) continue;
                    any = true;
                    lifetimes[renderer]++;
                    var seen = deaths[renderer];
                    if (seen[seen.Count - 1] != renderer.sprite) seen.Add(renderer.sprite);
                }
            }
            foreach (var pair in deaths)
            {
                var (set, rear) = expected[pair.Key];
                var drawings = new List<Sprite>();
                foreach (Sprite s in set.GetOwnDeath(rear)) if (drawings.Count == 0 || drawings[drawings.Count - 1] != s) drawings.Add(s);
                CollectionAssert.AreEqual(drawings, pair.Value, set.name + ": every drawing of its death in order, none cut short.");
                Assert.That(lifetimes[pair.Key] / 60f, Is.EqualTo(set.OwnDeathSeconds).Within(2.5f / 60), set.name + " plays its whole beat.");
                Assert.That(pair.Key.enabled, Is.False, set.name + ": then it is gone.");
            }
            // The deaths never took the shared pool: a burst of sparks after them all shows.
            MethodInfo show = typeof(CombatEffectsView).GetMethod("ShowMoving", Private);
            var spark = new[] { looks[1].PaintedArt.GetFrame(0, true).Flash };
            for (int i = 0; i < 24; i++)
                show.Invoke(effects, new object[] { spark, .05f, .3f, Vector3.zero, 5, Vector3.zero, 0f, false, 1f });
            int sparks = 0;
            foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == spark[0]) sparks++;
            Assert.That(sparks, Is.EqualTo(24), "The shared pool is the sparks' whole.");
            Assert.That(CombatEffectsView.DeathSlots, Is.GreaterThanOrEqualTo(HordeRule.SceneCap), "A slot for every enemy standing.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest, Timeout(120000)]
        public IEnumerator ASetWithoutTheNewPartsStrikesAndFallsAsBefore()
        {
            var effects = Object.FindFirstObjectByType<CombatEffectsView>();
            var watches = (System.Array)typeof(CombatEffectsView).GetField("_hordeWatches", Private).GetValue(effects);
            EnemyDefinition definition = Definition("Enemy_Grunt");
            EnemyLookView probe = Spawn(definition, _hero + new Vector3(-1.5f, -1f));
            yield return null;
            EnemyArtSet bare = Bare(probe.PaintedArt);
            Object.Destroy(probe.gameObject);
            EnemyLookView look = Spawn(definition, _hero + new Vector3(-1f, -1f), bare);
            yield return null;
            Assert.That(look.PaintedArt, Is.SameAs(bare));
            Health target = Target(look.transform.position + new Vector3(.3f, .15f));
            look.GetComponent<Targeting>().SetCandidates(new[] { target });
            var attack = look.GetComponent<AttackController>(); attack.enabled = true;
            bool never = true;
            for (int k = 0; k < 140; k++) { yield return null; never &= look.PaintedFrame < EnemyLookView.StrikeWindupFrame; }
            Assert.That(attack.AttackCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(never, Is.True, "It snaps into its strike as it always did.");
            attack.enabled = false;
            // Its fall is the two remains every painted enemy left, over their 0.4 s.
            typeof(CombatEffectsView).GetMethod("Bind", Private).Invoke(effects, new[] { watches.GetValue(0), look.GetComponent<Health>() });
            Health health = look.GetComponent<Health>();
            health.ApplyDamage(new DamageContext(health.Maximum * 10f));
            typeof(CombatEffectsView).GetMethod("OnEnemyDefeated", Private).Invoke(effects, new object[] { health });
            SpriteRenderer remains = null;
            foreach (var renderer in GameObject.Find("Combat Effects").GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == bare.Death[0]) remains = renderer;
            Assert.That(remains, Is.Not.Null, "The first of the two remains.");
            for (int i = 0; i < 12; i++) yield return null;
            Assert.That(remains.sprite, Is.SameAs(bare.Death[1]));
            for (int i = 0; i < 16; i++) yield return null;
            Assert.That(remains.enabled, Is.False, "Gone at 0.4 s, as before.");
            LogAssert.NoUnexpectedReceived();
        }
    }
}
