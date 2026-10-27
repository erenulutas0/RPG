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

namespace Cryptforge.Tests
{
    // The Staff's painted hit in the Gameplay scene (StrikeArtSet's staff strips): a real Staff strike rings its target's
    // feet with the painted ring, grown to exactly the radius the procedural ring showed and widened with Area as it
    // was, and bursts on the struck body; a Crucible Vent's eruptions keep the procedural ring in ember; and a strike set
    // without the painted frames, or with broken ones, draws the procedural ring as before.
    public sealed class PaintedStaffBlastTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        // The painted sheet the Staff's ring and burst are cut from.
        private const string SheetName = "VFX_StaffBlast";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private Health _hero;
        private AttackController _attack;
        private CombatEffectsView _effects;
        // Where the last strike's target stood as it was struck, which is where the view drew it.
        private Vector3 _struckAt;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            var scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted Staff blast cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        // Ilya, whose own weapon is the Staff, with the encounter and every attack off, so a test stages its own fight; on
        // the density proof's painted crowd when it strikes a real enemy, which the view watches for its hit spark.
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
            Assert.That(EffectArt.StrikeFor(_attack.Weapon.Pattern.Behavior), Is.EqualTo(StrikeEffect.StaffBlast), "Ilya carries the Staff.");
        }

        private Health[] Dummies(params Vector3[] offsets)
        {
            var dummies = new Health[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                dummies[i] = new GameObject("Staff dummy " + i).AddComponent<Health>();
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

        // The radius the procedural ring has always shown for a weapon: the scene's share of its splash, in the pixel
        // ring's texels, clamped to 0.75-1.5 of that ring (CombatEffectsView.RingScale), back in world units.
        private static float ShownRadius(CombatEffectsView view, WeaponRuntime weapon)
        {
            float fraction = (float)typeof(CombatEffectsView).GetField("_ringSplashFraction", Private).GetValue(view);
            float scale = Mathf.Clamp(weapon.SplashRadius * fraction * PixelSpriteFactory.PixelsPerUnit / EffectArt.RingFullRadius,
                .75f, 1.5f);
            return scale * EffectArt.RingFullRadius / PixelSpriteFactory.PixelsPerUnit;
        }

        private static int GroundOrder(CombatEffectsView view) =>
            (int)typeof(CombatEffectsView).GetField("_groundOrder", Private).GetValue(view);

        private static Health WaveGrunt()
        {
            foreach (var look in Object.FindObjectsByType<EnemyLookView>(FindObjectsSortMode.None))
                if (look.Look == EnemyLook.Grunt && look.GetComponent<Health>().IsAlive) return look.GetComponent<Health>();
            return null;
        }

        // One real Staff strike, the hero's own AttackController firing at its candidates; keepAlive is healed every
        // frame, so Ember Memory's burn never fells it mid-test.
        private IEnumerator Strike(Health keepAlive = null)
        {
            bool struck = false;
            System.Action<Health> onStruck = target =>
            {
                struck = true;
                _struckAt = target.transform.position;
            };
            _attack.Struck += onStruck;
            _attack.enabled = true;
            for (int i = 0; i < 120 && !struck; i++)
            {
                yield return null;
                if (keepAlive != null) keepAlive.Heal(keepAlive.Maximum);
            }
            _attack.enabled = false;
            _attack.Struck -= onStruck;
            Assert.That(struck, Is.True, "The Staff struck.");
        }

        private static IEnumerator Frames(int count, Health keepAlive)
        {
            for (int i = 0; i < count; i++)
            {
                yield return null;
                if (keepAlive != null) keepAlive.Heal(keepAlive.Maximum);
            }
        }

        [UnityTest]
        public IEnumerator AStaffStrikeRingsItsTargetInPaintAtTheSplashsShownRadiusAndBurstsOnIt()
        {
            yield return Load(true);
            Assert.That(_effects.UsesPaintedStaffBlast, Is.True, "The scene's strike set carries the Staff's painted hit.");
            Assert.That(_effects.UsesPaintedStrikes, Is.True, "Beside its painted slash and spark.");
            Transform pool = GameObject.Find("Combat Effects").transform;
            Health target = WaveGrunt();
            Assert.That(target, Is.Not.Null, "A painted Grunt of the wave, whose hits the view watches.");
            target.transform.position = _hero.transform.position + Vector3.right;
            _hero.GetComponent<Targeting>().SetCandidates(new[] { target });
            WeaponRuntime staff = _attack.Weapon;
            var radii = new float[2];
            for (int pass = 0; pass < 2; pass++)
            {
                // The second strike after an Area card: the ring widens with the splash, as the procedural one did.
                if (pass == 1)
                    staff.AddModifier(UpgradeStat.Area, new StatModifier(ModifierOperation.Percent, .2f));
                float radius = ShownRadius(_effects, staff);
                yield return Strike(target);
                string at = pass == 0 ? "A Staff strike" : "Widened by Area";

                List<SpriteRenderer> rings = Shown(pool, "VFX Staff Ring ");
                Assert.That(rings.Count, Is.EqualTo(1), $"{at}: one painted ring.");
                SpriteRenderer ring = rings[0];
                Assert.That(ring.sprite.texture.name, Is.EqualTo(SheetName), $"{at}: cut from the painted sheet.");
                Assert.That(ShowingProceduralRing(pool), Is.Empty, $"{at}: not the procedural ring.");
                Assert.That(ring.transform.position.x, Is.EqualTo(_struckAt.x).Within(1e-4f), $"{at}: round the target's feet.");
                Assert.That(ring.transform.position.y, Is.EqualTo(_struckAt.y).Within(1e-4f));
                Assert.That(ring.sortingOrder, Is.EqualTo(GroundOrder(_effects)), $"{at}: on the floor, under the actors.");
                Assert.That(ring.sprite.bounds.extents.x, Is.InRange(1f, 1.15f), $"{at}: a unit ring with room for its edge.");
                Assert.That(ring.transform.localScale.x, Is.InRange(radius * 24f / 38 - 1e-4f, radius * .8f), $"{at}: it starts small.");

                List<SpriteRenderer> bursts = Shown(pool, "VFX Staff Burst ");
                Assert.That(bursts.Count, Is.EqualTo(1), $"{at}: and bursts on the struck body.");
                SpriteRenderer burst = bursts[0];
                Assert.That(burst.sprite.texture, Is.SameAs(ring.sprite.texture), $"{at}: from the ring's sheet, so they batch.");
                Assert.That(burst.transform.position.x, Is.EqualTo(_struckAt.x).Within(.2f));
                Assert.That(burst.transform.position.y, Is.GreaterThan(_struckAt.y + .2f), $"{at}: on its body.");
                SpriteRenderer spark = null;
                foreach (SpriteRenderer shown in Shown(pool, "VFX_spark_"))
                    if (Vector3.Distance(shown.transform.position, burst.transform.position) < 1e-4f) spark = shown;
                Assert.That(spark, Is.Not.Null, $"{at}: where the painted hit spark lands on the body.");
                Assert.That(burst.sortingOrder, Is.GreaterThan(spark.sortingOrder), $"{at}: over the spark, as the other strikes.");
                Assert.That(burst.sortingOrder, Is.GreaterThan(ring.sortingOrder));

                // Grown to the radius over its first 0.14 s, through its painted frames, and fading.
                Sprite first = ring.sprite;
                yield return Frames(10, target);
                Assert.That(ring.enabled, Is.True, $"{at}: it outlasts the procedural ring's growth.");
                Assert.That(ring.transform.localScale.x, Is.EqualTo(radius).Within(1e-4f), $"{at}: at the splash's shown radius.");
                Assert.That(ring.transform.localScale.y, Is.EqualTo(radius).Within(1e-4f), $"{at}: its painted depth kept.");
                Assert.That(ring.sprite, Is.Not.SameAs(first), $"{at}: it breaks into its arcs.");
                Assert.That(ring.color.a, Is.LessThan(1f), $"{at}: and fades.");
                Assert.That(burst.enabled, Is.False, $"{at}: the burst is over first.");
                radii[pass] = ring.transform.localScale.x;
                yield return Frames(6, target);
                Assert.That(ring.enabled, Is.False, $"{at}: gone within a quarter second.");
            }
            Assert.That(radii[1], Is.GreaterThan(radii[0]), "Area widens the ring.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator ACrucibleVentStillEruptsInEmberOnTheProceduralRing()
        {
            yield return Load();
            Transform pool = GameObject.Find("Combat Effects").transform;
            Health[] dummies = Dummies(Vector3.right, Vector3.left);
            BurnStatus.On(dummies[0]).Ignite(3, false, _hero, _attack);
            BurnStatus.On(dummies[1]).Ignite(1, false, _hero, _attack);
            for (int i = 0; i < 40 && _effects.ShownFlameCount < 2; i++) yield return null;
            Assert.That(_effects.UsesPaintedStaffBlast, Is.True);

            Assert.That(_hero.GetComponent<AbilityController>().TryUse(), Is.True);
            List<SpriteRenderer> rings = ShowingProceduralRing(pool);
            Assert.That(rings.Count, Is.EqualTo(2), "Each eruption rings its splash with the procedural ring.");
            Assert.That(Shown(pool, "VFX Staff").Count, Is.Zero, "Never the Staff's violet frames tinted ember.");
            foreach (SpriteRenderer ring in rings)
                Assert.That(ring.sortingOrder, Is.EqualTo(GroundOrder(_effects)), "On the floor, as the Staff's ring.");
            for (int i = 0; i < 6; i++)
            {
                yield return null;
                foreach (SpriteRenderer ring in rings)
                    Assert.That(ring.color.r, Is.GreaterThan(ring.color.b + .3f), "It stays ember as it grows.");
                Assert.That(Shown(pool, "VFX Staff").Count, Is.Zero);
            }
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator AStrikeSetWithoutTheStaffsFramesDrawsTheProceduralRing()
        {
            yield return Load();
            var scene = (StrikeArtSet)typeof(CombatEffectsView).GetField("_paintedArt", Private).GetValue(_effects);
            // The scene's slash and spark alone, and the scene's set with its ring's sheet gone, as a missing texture
            // would leave it.
            var bare = ScriptableObject.CreateInstance<StrikeArtSet>();
            foreach (string field in new[] { "_slash", "_spark" })
            {
                FieldInfo info = typeof(StrikeArtSet).GetField(field, Private);
                info.SetValue(bare, info.GetValue(scene));
            }
            StrikeArtSet broken = Object.Instantiate(scene);
            broken.StaffRing.Sheet = null;
            Health target = Dummies(Vector3.right * 1.5f)[0];

            foreach (StrikeArtSet set in new[] { bare, broken })
            {
                string at = set == bare ? "Without the Staff's frames" : "With a broken ring";
                Assert.That(set.IsValid, Is.True, $"{at}: the slash and spark are still painted.");
                Assert.That(set.HasStaffBlast, Is.False, at);
                // A second view over the same scene, given everything the scene's has but this strike set.
                var holder = new GameObject("Procedural Staff");
                holder.SetActive(false);
                var view = holder.AddComponent<CombatEffectsView>();
                foreach (string field in new[] { "_encounter", "_hero", "_heroAttack", "_heroAbility", "_chests", "_camera", "_material",
                             "_skillArt", "_feedbackArt", "_ringSplashFraction" })
                {
                    FieldInfo info = typeof(CombatEffectsView).GetField(field, Private);
                    Assert.That(info, Is.Not.Null, field);
                    info.SetValue(view, info.GetValue(_effects));
                }
                typeof(CombatEffectsView).GetField("_paintedArt", Private).SetValue(view, set);
                holder.SetActive(true);
                Assert.That(view.UsesPaintedStrikes, Is.True, at);
                Assert.That(view.UsesPaintedStaffBlast, Is.False, at);

                MethodInfo draw = typeof(CombatEffectsView).GetMethod("DrawStrike", Private);
                draw.Invoke(view, new object[] { target, _attack.Weapon, false });
                Transform pool = holder.transform.Find("Combat Effects");
                List<SpriteRenderer> rings = ShowingProceduralRing(pool);
                Assert.That(rings.Count, Is.EqualTo(1), $"{at}: the procedural ring.");
                Assert.That(Shown(pool, "VFX Staff").Count, Is.Zero, $"{at}: and nothing painted for the Staff.");
                float radius = ShownRadius(view, _attack.Weapon);
                Assert.That(rings[0].transform.localScale.x, Is.EqualTo(radius * 24f / 38).Within(1e-4f), $"{at}: sized as before.");
                Assert.That(rings[0].sortingOrder, Is.EqualTo(GroundOrder(view)));
                for (int i = 0; i < 10; i++) yield return null;
                Assert.That(rings[0].transform.localScale.x, Is.EqualTo(radius).Within(1e-4f), $"{at}: grown to the same radius.");
                Assert.That(rings[0].sprite, Is.SameAs(StaffSplashArt.Get()), $"{at}: one frame throughout.");
                for (int i = 0; i < 4; i++) yield return null;
                Assert.That(rings[0].enabled, Is.False, $"{at}: over in the procedural ring's 0.21 s.");
                Object.Destroy(holder);
                yield return null;
            }
            Object.Destroy(bare);
            Object.Destroy(broken);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
