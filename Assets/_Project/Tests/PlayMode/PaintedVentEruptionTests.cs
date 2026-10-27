using System.Collections;
using System.Collections.Generic;
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

namespace Cryptforge.Tests
{
    // The Crucible Vent's painted eruption ring in the Gameplay scene (SkillEffectArtSet's optional splash strip): a real
    // Vent on burning enemies rings each one's feet with the painted ring, on the floor under the actors, grown to exactly
    // BurnRule.VentSplashRadius whatever the stacks it spent and hotter the more it spent; the Staff's hit still draws its
    // own violet painted ring beside it; and a skill set without the ring, with a broken one, or invalid altogether keeps
    // the procedural ring in ember.
    public sealed class PaintedVentEruptionTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        // The painted sheets the Vent's eruption ring and the Staff's ring are cut from.
        private const string SplashSheet = "VFX_Skill_VentSplash";
        private const string StaffSheet = "VFX_StaffBlast";
        private const string SplashPrefix = "VFX Vent Splash ";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        // The burn's ember, as the procedural ring is tinted (CombatEffectsView.BurnTint).
        private static readonly Color BurnTint = new Color(.906f, .447f, .196f, 1f);
        private Health _hero;
        private AttackController _attack;
        private CombatEffectsView _effects;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted Vent eruption cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // Ilya, whose ability is the Vent, with the encounter and every attack off, so a test stages its own fight; on the
        // density proof's painted crowd when it strikes a real enemy, which the view watches for its hit spark.
        private IEnumerator Load(bool crowd = false)
        {
            TestProfile.Begin();
            File.WriteAllText(DevelopmentStart.HeroPath, ArcanistId);
            if (crowd)
                File.WriteAllText(DevelopmentStart.StartFloorPath, "floor_density_proof");
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _attack = _hero.GetComponent<AttackController>();
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (var attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None)) attack.enabled = false;
            Assert.That(_hero.GetComponent<AbilityController>().Ability.Kind, Is.EqualTo(AbilityKind.Vent), "Ilya carries the Vent.");
        }

        private Health[] Dummies(params Vector3[] offsets)
        {
            var dummies = new Health[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                dummies[i] = new GameObject("Vent dummy " + i).AddComponent<Health>();
                dummies[i].Initialize(10000);
                dummies[i].transform.position = _hero.transform.position + offsets[i];
            }
            _hero.GetComponent<Targeting>().SetCandidates(dummies);
            return dummies;
        }

        // Every pooled effect on show under a pool whose sprite is one of the named frames.
        private static List<SpriteRenderer> Shown(Transform pool, string spritePrefix)
        {
            var shown = new List<SpriteRenderer>();
            foreach (var renderer in pool.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite != null && renderer.sprite.name.StartsWith(spritePrefix))
                    shown.Add(renderer);
            return shown;
        }

        private static List<SpriteRenderer> ShowingProceduralRing(Transform pool)
        {
            var shown = new List<SpriteRenderer>();
            foreach (var renderer in pool.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite == StaffSplashArt.Get())
                    shown.Add(renderer);
            return shown;
        }

        private static SpriteRenderer At(List<SpriteRenderer> shown, Vector3 feet)
        {
            SpriteRenderer nearest = null;
            foreach (var renderer in shown)
                if (nearest == null || Vector2.Distance(renderer.transform.position, feet) <
                    Vector2.Distance(nearest.transform.position, feet)) nearest = renderer;
            return nearest;
        }

        private static int GroundOrder(CombatEffectsView view) =>
            (int)typeof(CombatEffectsView).GetField("_groundOrder", Private).GetValue(view);

        private static IEnumerator Frames(int count, Health keepAlive = null)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
                if (keepAlive != null) keepAlive.Heal(keepAlive.Maximum);
            }
        }

        private IEnumerator WaitForFlames(int count)
        {
            // A burn lit by no strike is found by its first tick, half a second in.
            for (int i = 0; i < 40 && _effects.ShownFlameCount < count; i++) yield return null;
            Assert.That(_effects.ShownFlameCount, Is.EqualTo(count), "Every enemy burns before the Vent.");
        }

        [UnityTest]
        public IEnumerator AVentRingsEveryBurningEnemyInPaintAtTheSplashRadiusHotterForMoreStacks()
        {
            yield return Load();
            Assert.That(_effects.UsesPaintedSkills, Is.True, "The scene's painted skill art.");
            Transform pool = GameObject.Find("Combat Effects").transform;
            // One stack beside a full burn, either side of the hero and well within the Vent's reach.
            Health[] dummies = Dummies(new Vector3(1.3f, .2f, 0f), new Vector3(-1.1f, -.3f, 0f));
            Health one = dummies[0], full = dummies[1];
            BurnStatus.On(one).Ignite(1, false, _hero, _attack);
            BurnStatus.On(full).Ignite(BurnRule.MaxStacks, false, _hero, _attack);
            yield return WaitForFlames(2);
            Vector3 oneFeet = one.transform.position, fullFeet = full.transform.position;

            Assert.That(_hero.GetComponent<AbilityController>().TryUse(), Is.True);
            List<SpriteRenderer> rings = Shown(pool, SplashPrefix);
            Assert.That(rings.Count, Is.EqualTo(2), "A painted ring at each enemy the Vent spent.");
            Assert.That(ShowingProceduralRing(pool), Is.Empty, "Not the procedural ring.");
            Assert.That(Shown(pool, "VFX Staff").Count, Is.Zero, "Never the Staff's violet frames.");
            Assert.That(Shown(pool, "VFX Vent Column ").Count, Is.EqualTo(2), "Beside the columns, as before.");
            SpriteRenderer atOne = At(rings, oneFeet), atFull = At(rings, fullFeet);
            Assert.That(atOne, Is.Not.SameAs(atFull));
            float radius = BurnRule.VentSplashRadius;
            foreach (SpriteRenderer ring in rings)
            {
                Vector3 feet = ring == atOne ? oneFeet : fullFeet;
                string at = ring == atOne ? "One stack" : "A full burn";
                Assert.That(ring.transform.position.x, Is.EqualTo(feet.x).Within(1e-4f), $"{at}: round the enemy's feet.");
                Assert.That(ring.transform.position.y, Is.EqualTo(feet.y).Within(1e-4f));
                Assert.That(ring.sortingOrder, Is.EqualTo(GroundOrder(_effects)), $"{at}: on the floor, under the actors.");
                Assert.That(ring.sprite.texture.name, Is.EqualTo(SplashSheet), $"{at}: cut from its painted sheet.");
                Assert.That(ring.sprite.bounds.extents.x, Is.InRange(1f, 1.2f), $"{at}: a unit ring with room for its rim.");
                Assert.That(ring.transform.localScale.x, Is.EqualTo(radius * 24f / 38).Within(1e-4f), $"{at}: it starts small.");
                Assert.That(ring.color.r >= ring.color.g && ring.color.g >= ring.color.b, Is.True, $"{at}: warmed, never cooled.");
            }
            Assert.That(atFull.sprite.texture, Is.SameAs(atOne.sprite.texture), "A crowd's eruptions batch on one sheet.");
            Assert.That(atOne.flipX, Is.Not.EqualTo(atFull.flipX), "Every other eruption is mirrored, so a crowd's never repeat.");
            // A full burn's ring burns as painted; one stack's cooler, redder and fainter.
            Assert.That(atFull.color.g, Is.GreaterThan(.999f));
            Assert.That(atFull.color.b, Is.GreaterThan(.999f));
            Assert.That(atFull.color.a, Is.GreaterThan(.999f));
            Assert.That(atOne.color.g, Is.LessThan(atFull.color.g - .2f), "One stack burns redder.");
            Assert.That(atOne.color.b, Is.LessThan(atFull.color.b - .3f));
            Assert.That(atOne.color.a, Is.LessThan(atFull.color.a), "And fainter.");

            // Grown to the splash over its first 0.14 s, whatever the stacks, through its painted frames.
            Sprite first = atOne.sprite;
            yield return Frames(10);
            foreach (SpriteRenderer ring in rings)
            {
                string at = ring == atOne ? "One stack" : "A full burn";
                Assert.That(ring.enabled, Is.True, $"{at}: it outlasts the procedural ring's growth.");
                Assert.That(ring.transform.localScale.x, Is.EqualTo(radius).Within(1e-4f), $"{at}: at the splash's radius.");
                Assert.That(ring.transform.localScale.y, Is.EqualTo(radius).Within(1e-4f), $"{at}: its painted depth kept.");
                Assert.That(ring.sprite.name, Does.StartWith(SplashPrefix));
            }
            Assert.That(atOne.sprite, Is.Not.SameAs(first), "It burns through its frames.");
            Assert.That(atOne.color.g, Is.LessThan(atFull.color.g), "Each keeps its heat as it burns.");
            yield return Frames(6);
            Assert.That(atFull.enabled, Is.True, "Still burning out a quarter second in.");
            Assert.That(atFull.color.a, Is.LessThan(1f), "And fading.");
            yield return Frames(5);
            Assert.That(atOne.enabled || atFull.enabled, Is.False, "Gone within a third of a second.");
            Assert.That(Shown(pool, SplashPrefix), Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheStaffsHitStillRingsInItsOwnVioletBesideTheVentsEmber()
        {
            yield return Load(true);
            Assert.That(_effects.UsesPaintedStaffBlast && _effects.UsesPaintedSkills, Is.True, "Both painted sets.");
            Transform pool = GameObject.Find("Combat Effects").transform;
            Health target = null;
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Look == EnemyLook.Grunt && look.GetComponent<Health>().IsAlive) target = look.GetComponent<Health>();
            Assert.That(target, Is.Not.Null, "A painted Grunt of the wave, whose hits the view watches.");
            target.transform.position = _hero.transform.position + Vector3.right;
            _hero.GetComponent<Targeting>().SetCandidates(new[] { target });

            // One real Staff strike, the hero's own AttackController firing; the target is healed every frame, so neither
            // the burn nor the Vent fells it mid-test.
            bool struck = false;
            System.Action<Health> onStruck = _ => struck = true;
            _attack.Struck += onStruck;
            _attack.enabled = true;
            for (int i = 0; i < 120 && !struck; i++)
            {
                yield return null;
                target.Heal(target.Maximum);
            }
            _attack.enabled = false;
            _attack.Struck -= onStruck;
            Assert.That(struck, Is.True, "The Staff struck.");
            List<SpriteRenderer> staff = Shown(pool, "VFX Staff Ring ");
            Assert.That(staff.Count, Is.EqualTo(1), "The Staff's hit rings its target in its own painted ring.");
            Assert.That(staff[0].sprite.texture.name, Is.EqualTo(StaffSheet));
            Assert.That(staff[0].color.b, Is.GreaterThan(.99f), "Untinted: its violet is painted.");
            Assert.That(Shown(pool, SplashPrefix), Is.Empty, "Never the Vent's eruption ring.");

            // Burning (the strike's Ember Memory, topped up), it erupts under the Vent while the Staff's ring still shows.
            BurnStatus.On(target).Ignite(2, false, _hero, _attack);
            Assert.That(_hero.GetComponent<AbilityController>().TryUse(), Is.True);
            target.Heal(target.Maximum);
            List<SpriteRenderer> splash = Shown(pool, SplashPrefix);
            Assert.That(splash.Count, Is.EqualTo(1), "The Vent's eruption at the burning target.");
            Assert.That(splash[0].sprite.texture.name, Is.EqualTo(SplashSheet));
            Assert.That(splash[0].sprite.texture, Is.Not.SameAs(staff[0].sprite.texture), "Each from its own sheet.");
            Assert.That(staff[0].enabled && staff[0].sprite.name.StartsWith("VFX Staff Ring "), Is.True,
                "The Staff's ring burns on beside it, still its own.");
            Assert.That(ShowingProceduralRing(pool), Is.Empty, "And neither falls back to the procedural ring.");
            yield return Frames(3, target);
            Assert.That(staff[0].sprite.name, Does.StartWith("VFX Staff Ring "));
            Assert.That(splash[0].sprite.name, Does.StartWith(SplashPrefix));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ASkillSetWithoutTheEruptionRingKeepsTheProceduralEmberRing()
        {
            yield return Load();
            var scene = (SkillEffectArtSet)typeof(CombatEffectsView).GetField("_skillArt", Private).GetValue(_effects);
            Assert.That(scene.HasVentSplash, Is.True, "The scene's set carries it.");
            // A set as painted before the ring (an asset without the field holds an empty strip), the scene's with the
            // ring's sheet gone as a missing texture would leave it, and one broken elsewhere, so no painted skill draws.
            SkillEffectArtSet bare = Object.Instantiate(scene);
            typeof(SkillEffectArtSet).GetField("_ventSplash", Private).SetValue(bare, new SkillEffectArtSet.Strip());
            SkillEffectArtSet broken = Object.Instantiate(scene);
            broken.VentSplash.Sheet = null;
            SkillEffectArtSet invalid = Object.Instantiate(scene);
            invalid.EmberArc.Sheet = null;
            Health target = Dummies(new Vector3(1.4f, .3f, 0f))[0];
            float radius = BurnRule.VentSplashRadius;

            foreach (SkillEffectArtSet set in new[] { bare, broken, invalid })
            {
                string at = set == bare ? "Without the ring" : set == broken ? "With a broken ring" : "With an invalid set";
                Assert.That(set.IsValid, Is.EqualTo(set != invalid), $"{at}: the rest of the set is whole, or not.");
                Assert.That(set.HasVentSplash, Is.EqualTo(set == invalid), at);
                // A second view over the same scene, given everything the scene's has but this skill set.
                var holder = new GameObject("Procedural Vent");
                holder.SetActive(false);
                var view = holder.AddComponent<CombatEffectsView>();
                foreach (string field in new[] { "_encounter", "_hero", "_heroAttack", "_chests", "_camera", "_material",
                             "_paintedArt", "_feedbackArt", "_ringSplashFraction" })
                {
                    FieldInfo info = typeof(CombatEffectsView).GetField(field, Private);
                    Assert.That(info, Is.Not.Null, field);
                    info.SetValue(view, info.GetValue(_effects));
                }
                typeof(CombatEffectsView).GetField("_skillArt", Private).SetValue(view, set);
                holder.SetActive(true);
                Assert.That(view.UsesPaintedSkills, Is.EqualTo(set != invalid), at);

                typeof(CombatEffectsView).GetMethod("OnVented", Private).Invoke(view, new object[] { target, 3 });
                Transform pool = holder.transform.Find("Combat Effects");
                List<SpriteRenderer> rings = ShowingProceduralRing(pool);
                Assert.That(rings.Count, Is.EqualTo(1), $"{at}: the procedural ring.");
                Assert.That(Shown(pool, SplashPrefix), Is.Empty, $"{at}: and nothing painted for it.");
                Assert.That(Shown(pool, "VFX Vent Column ").Count, Is.EqualTo(1), $"{at}: beside the eruption's column.");
                SpriteRenderer ring = rings[0];
                Assert.That(ring.color, Is.EqualTo(BurnTint), $"{at}: in the burn's ember, as before.");
                Assert.That(ring.flipX, Is.False, at);
                Assert.That(ring.transform.position.x, Is.EqualTo(target.transform.position.x).Within(1e-4f), at);
                Assert.That(ring.transform.position.y, Is.EqualTo(target.transform.position.y).Within(1e-4f), at);
                Assert.That(ring.transform.localScale.x, Is.EqualTo(radius * 24f / 38).Within(1e-4f), $"{at}: sized as before.");
                Assert.That(ring.sortingOrder, Is.EqualTo(GroundOrder(view)));
                yield return Frames(10);
                Assert.That(ring.transform.localScale.x, Is.EqualTo(radius).Within(1e-4f), $"{at}: grown to the splash.");
                Assert.That(ring.sprite, Is.SameAs(StaffSplashArt.Get()), $"{at}: one frame throughout.");
                Assert.That(ring.color.r, Is.GreaterThan(ring.color.b + .3f), $"{at}: still ember as it fades.");
                yield return Frames(4);
                Assert.That(ring.enabled, Is.False, $"{at}: over in the procedural ring's 0.21 s.");
                Object.Destroy(holder);
                yield return null;
            }
            Object.Destroy(bare);
            Object.Destroy(broken);
            Object.Destroy(invalid);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
