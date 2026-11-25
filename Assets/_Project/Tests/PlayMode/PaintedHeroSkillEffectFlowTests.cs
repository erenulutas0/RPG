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
    // docs/57's four new skills painted in the Gameplay scene (SkillEffectArtSet's optional families, CombatEffectsView):
    // each skill pressed for real (development/skills.txt "3") shows its painted strips - Iron Resolve's bell over the
    // Vanguard for exactly its guard, with its rivets on the floor; the Shield Rush's wake along the dash and its impact
    // where the dash stops; the Flame Sigil's glyph on the floor at its radius for its seconds; Cinder Ward's shell round
    // Ilya, its flare at a striker and its break as the ward is spent or ends - and a skill set without their families, or
    // with one broken, draws nothing of its own for them, as the game did before they were painted. The fight is placed by
    // hand, as HeroSkillsFlowTests places it: the encounter and every attack off.
    public sealed class PaintedHeroSkillEffectFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private const string ArcanistId = "hero_cinder_arcanist";
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float Frame = 1f / 60;
        private Health _hero;
        private AbilityController _ability;
        private CombatEffectsView _effects;
        private Transform _pool;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Painted hero skills cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private IEnumerator Load(string heroId)
        {
            TestProfile.Begin();
            if (heroId != null)
                File.WriteAllText(DevelopmentStart.HeroPath, heroId);
            File.WriteAllText(DevelopmentStart.SkillsPath, "3");
            // The development file announces the skills it forces on (CombatSetup); that line is expected here.
            LogAssert.Expect(LogType.Log, new System.Text.RegularExpressions.Regex(@"^\[Skills\] the run carries"));
            Time.timeScale = 1;
            Time.captureDeltaTime = Frame;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _ability = _hero.GetComponent<AbilityController>();
            _effects = Object.FindFirstObjectByType<CombatEffectsView>();
            _pool = GameObject.Find("Combat Effects").transform;
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (AttackController attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            _ability.GetComponent<Targeting>().SetCandidates(new Health[0]);
        }

        private Health Dummy(float floorX, float floorY)
        {
            var dummy = new GameObject("Painted skill dummy").AddComponent<Health>();
            dummy.Initialize(1000);
            dummy.transform.position = _hero.transform.position + new Vector3(floorX, ArenaFloor.WorldY(floorY), 0f);
            return dummy;
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        private static List<SpriteRenderer> Shown(Transform pool, string prefix)
        {
            var shown = new List<SpriteRenderer>();
            foreach (SpriteRenderer renderer in pool.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.enabled && renderer.sprite != null && renderer.sprite.name.StartsWith(prefix))
                    shown.Add(renderer);
            return shown;
        }

        private static int GroundOrder(CombatEffectsView view) =>
            (int)typeof(CombatEffectsView).GetField("_groundOrder", Private).GetValue(view);

        private static void AssertAt(Transform shown, Vector3 at, string what)
        {
            Assert.That(shown.position.x, Is.EqualTo(at.x).Within(.02f), $"{what}: x.");
            Assert.That(shown.position.y, Is.EqualTo(at.y).Within(.02f), $"{what}: y.");
        }

        [UnityTest]
        public IEnumerator IronResolvesBellClosesOverTheVanguardHoldsForItsGuardAndFallsAway()
        {
            yield return Load(null);
            Assert.That(_ability.SkillAt(2).Kind, Is.EqualTo(AbilityKind.Guard));
            Assert.That(_effects.PaintsSkill(AbilityKind.Guard), Is.True, "The scene's set carries Iron Resolve's family.");
            Assert.That(_effects.SkillShown(AbilityKind.Guard), Is.Null, "Nothing before the press.");

            Assert.That(_ability.TryUse(2), Is.True);
            SpriteRenderer bell = _effects.SkillShown(AbilityKind.Guard);
            Assert.That(bell, Is.Not.Null, "The bell at the press.");
            Assert.That(bell.sprite.name, Does.StartWith("VFX Resolve Close "), "Its staves closing first.");
            Assert.That(bell.sprite.texture.name, Is.EqualTo("VFX_Skill_Resolve"), "Cut from its own sheet.");
            Assert.That(bell.sortingOrder, Is.GreaterThan(GroundOrder(_effects)), "Over the hero, not on the floor.");
            Assert.That(_effects.StageOf(AbilityKind.Guard), Is.EqualTo(CombatEffectsView.SkillStage.Rising));
            AssertAt(bell.transform, _hero.transform.position, "The bell stands on the hero's feet");
            List<SpriteRenderer> rivets = Shown(_pool, "VFX Resolve Rivets ");
            Assert.That(rivets.Count, Is.EqualTo(1), "Its rivets stamp the floor.");
            Assert.That(rivets[0].sortingOrder, Is.EqualTo(GroundOrder(_effects)), "On the floor, under the actors.");
            Assert.That(rivets[0].transform.localScale.x, Is.EqualTo(CombatEffectsView.ResolveRivetRadius).Within(1e-4f),
                "A unit round the feet (docs/57 3.1).");
            AssertAt(rivets[0].transform, _hero.transform.position, "The rivets round the feet");

            // It follows the hero, holds once closed, for the guard's seconds, then falls away and is gone.
            yield return Frames(20);
            _hero.transform.position += new Vector3(.6f, .2f, 0f);
            yield return null;
            Assert.That(_effects.StageOf(AbilityKind.Guard), Is.EqualTo(CombatEffectsView.SkillStage.Holding));
            Assert.That(bell.enabled && bell.sprite.name.StartsWith("VFX Resolve Hold "), Is.True, "The sheen holds.");
            AssertAt(bell.transform, _hero.transform.position, "It follows the hero");
            Assert.That(Shown(_pool, "VFX Resolve Rivets "), Is.Empty, "The rivets' 0.3 s are over.");
            var held = new HashSet<Sprite>();
            for (int i = 0; i < 40; i++)
            {
                yield return null;
                held.Add(bell.sprite);
            }
            Assert.That(held.Count, Is.GreaterThan(1), "Its glint sweeps round it.");
            Assert.That(_hero.State.DamageReduction, Is.EqualTo(.5f).Within(1e-4f), "The guard holds under it.");
            while (_ability.SkillAt(2).IsActive)
            {
                Assert.That(_effects.StageOf(AbilityKind.Guard), Is.EqualTo(CombatEffectsView.SkillStage.Holding),
                    "Holding for as long as the guard does.");
                yield return null;
            }
            yield return null;
            Assert.That(_effects.StageOf(AbilityKind.Guard), Is.EqualTo(CombatEffectsView.SkillStage.Ending), "A clear end.");
            Assert.That(bell.sprite.name, Does.StartWith("VFX Resolve End "));
            yield return Frames(20);
            Assert.That(_effects.SkillShown(AbilityKind.Guard), Is.Null, "Gone within a third of a second.");
            Assert.That(_effects.StageOf(AbilityKind.Guard), Is.EqualTo(CombatEffectsView.SkillStage.Off));
            Assert.That(Shown(_pool, "VFX Resolve "), Is.Empty);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheShieldRushTrailsItsWakeAlongTheDashAndStrikesWhereItStops()
        {
            yield return Load(null);
            Assert.That(_ability.SkillAt(3).Kind, Is.EqualTo(AbilityKind.Rush));
            Health target = Dummy(2.5f, 0f);
            _ability.GetComponent<Targeting>().SetCandidates(new[] { target });
            Vector3 start = _hero.transform.position;

            Assert.That(_ability.TryUse(3, Vector2.right), Is.True);
            SpriteRenderer wake = _effects.SkillShown(AbilityKind.Rush);
            Assert.That(wake, Is.Not.Null, "The wake at the press.");
            Assert.That(wake.sprite.name, Does.StartWith("VFX Rush Streak "));
            Assert.That(wake.sprite.texture.name, Is.EqualTo("VFX_Skill_Rush"));
            Assert.That(Mathf.DeltaAngle(wake.transform.eulerAngles.z, 0f), Is.EqualTo(0f).Within(.5f), "Along the dash.");
            yield return Frames(6);
            Assert.That(_ability.Dash.IsDashing, Is.True, "Still dashing.");
            AssertAt(wake.transform, _hero.transform.position + Vector3.up * CombatEffectsView.RushLift,
                "The wake rides the hero's middle");
            Assert.That(_hero.transform.position.x, Is.GreaterThan(start.x + .3f), "And the hero has moved on.");
            Assert.That(Shown(_pool, "VFX Rush Impact "), Is.Empty, "No impact before the dash stops.");

            for (int i = 0; i < 40 && _ability.Dash.IsDashing; i++)
                yield return null;
            yield return null;
            List<SpriteRenderer> impact = Shown(_pool, "VFX Rush Impact ");
            Assert.That(impact.Count, Is.EqualTo(1), "The impact where the dash stopped.");
            Vector3 stop = _hero.transform.position;
            AssertAt(impact[0].transform, stop + Vector3.right * CombatEffectsView.RushImpactReach +
                Vector3.up * CombatEffectsView.RushLift, "Before the shield, at the hero's middle");
            Assert.That(Mathf.DeltaAngle(impact[0].transform.eulerAngles.z, 0f), Is.EqualTo(0f).Within(.5f), "Facing the dash.");
            Assert.That(impact[0].sprite.texture, Is.SameAs(wake.sprite.texture), "From the wake's sheet.");
            yield return Frames(30);
            Assert.That(Shown(_pool, "VFX Rush "), Is.Empty, "Both gone within half a second.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheRushsWakeTurnsWithADashUpTheFloor()
        {
            yield return Load(null);
            Assert.That(_ability.TryUse(3, new Vector2(1f, 1f)), Is.True);
            SpriteRenderer wake = _effects.SkillShown(AbilityKind.Rush);
            Assert.That(wake, Is.Not.Null);
            // A floor heading of 45 degrees is atan(0.5) on screen: the floor's depth is halved.
            float expected = Mathf.Atan2(ArenaFloor.DepthScale, 1f) * Mathf.Rad2Deg;
            Assert.That(Mathf.DeltaAngle(wake.transform.eulerAngles.z, expected), Is.EqualTo(0f).Within(.5f),
                "Turned along the dash as the screen shows it.");
            for (int i = 0; i < 40 && _ability.Dash.IsDashing; i++)
                yield return null;
            yield return null;
            List<SpriteRenderer> impact = Shown(_pool, "VFX Rush Impact ");
            Assert.That(impact.Count, Is.EqualTo(1), "A dash that struck nothing still stops with its skid.");
            Assert.That(Mathf.DeltaAngle(impact[0].transform.eulerAngles.z, expected), Is.EqualTo(0f).Within(.5f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator TheFlameSigilBurnsItsGlyphOnTheFloorAtItsRadiusAndDimsAsItFades()
        {
            yield return Load(ArcanistId);
            Assert.That(_ability.SkillAt(2).Kind, Is.EqualTo(AbilityKind.Sigil));
            Assert.That(_effects.PaintsSkill(AbilityKind.Sigil), Is.True);
            Vector3 drawn = _hero.transform.position;
            float radius = _ability.SkillAt(2).Radius;

            Assert.That(_ability.TryUse(2), Is.True);
            SpriteRenderer glyph = _effects.SkillShown(AbilityKind.Sigil);
            Assert.That(glyph, Is.Not.Null, "The glyph at the press.");
            Assert.That(glyph.sprite.name, Does.StartWith("VFX Sigil Ignite "), "Igniting first.");
            Assert.That(glyph.sprite.texture.name, Is.EqualTo("VFX_Skill_Sigil"));
            Assert.That(glyph.sortingOrder, Is.EqualTo(GroundOrder(_effects)), "On the floor, under the actors.");
            Assert.That(glyph.transform.localScale.x, Is.EqualTo(radius * CombatEffectsView.SigilIgniteStart).Within(1e-4f),
                "From Ilya's feet outward.");
            AssertAt(glyph.transform, drawn, "Where Ilya drew it");

            // Ilya walks away: the glyph stays where it was drawn, grown to its radius, burning in a loop.
            _hero.transform.position += new Vector3(1.5f, 0f, 0f);
            yield return Frames(20);
            Assert.That(_effects.StageOf(AbilityKind.Sigil), Is.EqualTo(CombatEffectsView.SkillStage.Holding));
            Assert.That(glyph.sprite.name, Does.StartWith("VFX Sigil Glyph "));
            Assert.That(glyph.transform.localScale.x, Is.EqualTo(radius).Within(1e-4f), "At the glyph's radius.");
            Assert.That(glyph.transform.localScale.y, Is.EqualTo(radius).Within(1e-4f), "Its painted depth kept.");
            AssertAt(glyph.transform, drawn, "It stays where it was drawn");
            Assert.That(glyph.color.a, Is.EqualTo(1f).Within(1e-4f), "Full while it burns.");
            var burned = new HashSet<Sprite>();
            for (int i = 0; i < 30; i++)
            {
                yield return null;
                burned.Add(glyph.sprite);
            }
            Assert.That(burned.Count, Is.GreaterThan(1), "Its runes' heat runs round it.");

            // It dims over its last second and is gone with the zone.
            while (_ability.Sigil.IsLive && _effects.SkillShown(AbilityKind.Sigil) != null && glyph.color.a > .99f)
                yield return null;
            Assert.That(_ability.Sigil.IsLive, Is.True, "It dims while the zone still burns.");
            for (int i = 0; i < 90 && _ability.Sigil.IsLive; i++)
            {
                Assert.That(glyph.color.a, Is.InRange(CombatEffectsView.SigilDimTo - 1e-3f, 1f));
                yield return null;
            }
            yield return null;
            Assert.That(_ability.Sigil.IsLive, Is.False);
            Assert.That(_effects.SkillShown(AbilityKind.Sigil), Is.Null, "Gone with the zone.");
            Assert.That(_effects.StageOf(AbilityKind.Sigil), Is.EqualTo(CombatEffectsView.SkillStage.Off));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CinderWardWhirlsRoundIlyaFlaresAtItsStrikerAndBreaksWhenSpent()
        {
            yield return Load(ArcanistId);
            Assert.That(_ability.SkillAt(3).Kind, Is.EqualTo(AbilityKind.Ward));
            Health striker = Dummy(1.2f, 0f);

            Assert.That(_ability.TryUse(3), Is.True);
            SpriteRenderer shell = _effects.SkillShown(AbilityKind.Ward);
            Assert.That(shell, Is.Not.Null, "The shell at the press.");
            Assert.That(shell.sprite.name, Does.StartWith("VFX Ward Raise "), "Rising first.");
            Assert.That(shell.sprite.texture.name, Is.EqualTo("VFX_Skill_Ward"));
            AssertAt(shell.transform, _hero.transform.position, "Round Ilya's feet");
            yield return Frames(15);
            Assert.That(_effects.StageOf(AbilityKind.Ward), Is.EqualTo(CombatEffectsView.SkillStage.Holding));
            Assert.That(shell.sprite.name, Does.StartWith("VFX Ward Shell "));

            // A blow the ward takes: it flares at the striker, and the shell holds while its pool lasts.
            _hero.ApplyDamage(new DamageContext(10f, striker));
            List<SpriteRenderer> flares = Shown(_pool, "VFX Ward Flare ");
            Assert.That(flares.Count, Is.EqualTo(1), "A flare at the striker.");
            Assert.That(Mathf.Abs(flares[0].transform.position.x - striker.transform.position.x), Is.LessThan(.05f),
                "At the striker, not at Ilya.");
            Assert.That(_hero.State.Ward, Is.GreaterThan(0f));
            yield return null;
            Assert.That(_effects.StageOf(AbilityKind.Ward), Is.EqualTo(CombatEffectsView.SkillStage.Holding));

            // The next blow spends it: the shell blows apart at once, though the ward's seconds are not over.
            _hero.ApplyDamage(new DamageContext(20f, striker));
            Assert.That(_hero.State.Ward, Is.EqualTo(0f));
            Assert.That(_ability.SkillAt(3).IsActive, Is.True, "Its 2 s still run.");
            yield return null;
            Assert.That(_effects.StageOf(AbilityKind.Ward), Is.EqualTo(CombatEffectsView.SkillStage.Ending), "Spent, it breaks.");
            Assert.That(shell.sprite.name, Does.StartWith("VFX Ward Break "));
            yield return Frames(20);
            Assert.That(_effects.SkillShown(AbilityKind.Ward), Is.Null, "Gone.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator CinderWardsShellBreaksAsItsSecondsEnd()
        {
            yield return Load(ArcanistId);
            Assert.That(_ability.TryUse(3), Is.True);
            SpriteRenderer shell = _effects.SkillShown(AbilityKind.Ward);
            Assert.That(shell, Is.Not.Null);
            while (_ability.SkillAt(3).IsActive)
            {
                yield return null;
                if (_ability.SkillAt(3).IsActive && _effects.StageOf(AbilityKind.Ward) != CombatEffectsView.SkillStage.Rising)
                    Assert.That(_effects.StageOf(AbilityKind.Ward), Is.EqualTo(CombatEffectsView.SkillStage.Holding));
            }
            yield return null;
            Assert.That(_effects.StageOf(AbilityKind.Ward), Is.EqualTo(CombatEffectsView.SkillStage.Ending));
            Assert.That(shell.sprite.name, Does.StartWith("VFX Ward Break "));
            yield return Frames(20);
            Assert.That(_effects.SkillShown(AbilityKind.Ward), Is.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator WithoutTheirFamiliesTheVanguardsSkillsDrawNothingOfTheirOwnAsBefore()
        {
            yield return Load(null);
            yield return Fallback(new[] { "_resolveClose", "_resolveHold", "_resolveEnd", "_resolveRivets", "_rushStreak",
                "_rushImpact" }, set => set.ResolveHold, AbilityKind.Guard, AbilityKind.Rush, () =>
            {
                Assert.That(_ability.TryUse(2), Is.True);
                Assert.That(_ability.TryUse(3, Vector2.right), Is.True);
            });
        }

        [UnityTest]
        public IEnumerator WithoutTheirFamiliesIlyasSkillsDrawNothingOfTheirOwnAsBefore()
        {
            yield return Load(ArcanistId);
            Health striker = Dummy(1.2f, 0f);
            yield return Fallback(new[] { "_sigilIgnite", "_sigilGlyph", "_wardRaise", "_wardShell", "_wardBreak",
                "_wardFlare" }, set => set.WardShell, AbilityKind.Ward, AbilityKind.Sigil, () =>
            {
                Assert.That(_ability.TryUse(2), Is.True);
                Assert.That(_ability.TryUse(3), Is.True);
                _hero.ApplyDamage(new DamageContext(5f, striker));
            });
        }

        private static string PrefixOf(AbilityKind kind) => kind == AbilityKind.Guard ? "VFX Resolve "
            : kind == AbilityKind.Rush ? "VFX Rush " : kind == AbilityKind.Sigil ? "VFX Sigil " : "VFX Ward ";

        // Second views over the same scene, given everything the scene's has but its skill set: one without the hero's two
        // families (an asset painted before them holds empty strips), one with a strip of one family broken (its sheet
        // gone, as a missing texture would leave it). The presses draw nothing of an unpainted family in either view, the
        // broken family's sibling still paints, and the scene's own view paints both, as before.
        private IEnumerator Fallback(string[] fields, System.Func<SkillEffectArtSet, SkillEffectArtSet.Strip> breakable,
            AbilityKind brokenKind, AbilityKind sibling, System.Action press)
        {
            var scene = (SkillEffectArtSet)typeof(CombatEffectsView).GetField("_skillArt", Private).GetValue(_effects);
            SkillEffectArtSet bare = Object.Instantiate(scene);
            foreach (string field in fields)
                typeof(SkillEffectArtSet).GetField(field, Private).SetValue(bare, new SkillEffectArtSet.Strip());
            SkillEffectArtSet broken = Object.Instantiate(scene);
            breakable(broken).Sheet = null;
            Assert.That(bare.IsValid && broken.IsValid, Is.True, "The rest of each set is whole.");

            var views = new List<CombatEffectsView>();
            var holders = new List<GameObject>();
            foreach (SkillEffectArtSet set in new[] { bare, broken })
            {
                var holder = new GameObject(set == bare ? "Without the families" : "With a broken family");
                holder.SetActive(false);
                var view = holder.AddComponent<CombatEffectsView>();
                foreach (string field in new[] { "_encounter", "_hero", "_heroAttack", "_heroAbility", "_chests", "_camera",
                             "_material", "_paintedArt", "_feedbackArt", "_ringSplashFraction" })
                {
                    FieldInfo info = typeof(CombatEffectsView).GetField(field, Private);
                    Assert.That(info, Is.Not.Null, field);
                    info.SetValue(view, info.GetValue(_effects));
                }
                typeof(CombatEffectsView).GetField("_skillArt", Private).SetValue(view, set);
                holder.SetActive(true);
                Assert.That(view.UsesPaintedSkills, Is.True, $"{holder.name}: the shipped skills still paint.");
                views.Add(view);
                holders.Add(holder);
            }
            Assert.That(views[0].PaintsSkill(brokenKind) || views[0].PaintsSkill(sibling), Is.False, "Without them, neither paints.");
            Assert.That(views[1].PaintsSkill(brokenKind), Is.False, "A broken family does not paint.");
            Assert.That(views[1].PaintsSkill(sibling), Is.True, "Its sibling still does.");
            Assert.That(_effects.PaintsSkill(brokenKind) && _effects.PaintsSkill(sibling), Is.True, "The scene's set paints both.");

            press();
            bool siblingShown = false, sceneShown = false;
            for (int frame = 0; frame < 30; frame++)
            {
                for (int v = 0; v < views.Count; v++)
                {
                    Transform pool = holders[v].transform.Find("Combat Effects");
                    foreach (AbilityKind kind in new[] { brokenKind, sibling })
                    {
                        if (views[v].PaintsSkill(kind))
                            continue;
                        Assert.That(views[v].SkillShown(kind), Is.Null, $"{holders[v].name}: {kind} draws nothing of its own.");
                        Assert.That(views[v].StageOf(kind), Is.EqualTo(CombatEffectsView.SkillStage.Off));
                        Assert.That(Shown(pool, PrefixOf(kind)), Is.Empty, $"{holders[v].name}: nothing of {kind}'s family.");
                    }
                }
                siblingShown |= views[1].SkillShown(sibling) != null;
                sceneShown |= _effects.SkillShown(brokenKind) != null;
                yield return null;
            }
            Assert.That(siblingShown, Is.True, "The broken family's sibling still paints.");
            Assert.That(sceneShown, Is.True, "The scene's own view paints the family the others lack.");
            foreach (GameObject holder in holders)
                Object.Destroy(holder);
            Object.Destroy(bare);
            Object.Destroy(broken);
            yield return null;
            LogAssert.NoUnexpectedReceived();
        }
    }
}
