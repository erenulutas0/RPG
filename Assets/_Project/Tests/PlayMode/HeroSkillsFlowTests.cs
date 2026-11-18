using System.Collections;
using System.IO;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // docs/57's skills 2 and 3 in the scene (phase 1): off unless development/skills.txt asks, and then each slot does what
    // the simulation's run does through the same bodies - the guard's armour on the hero's sheet for its seconds, the Rush's
    // strike, daze and dash, the glyph's slow and light, the ward's pool and its striker's burn. The fight is placed by hand
    // (dummies on the hero's targets, the encounter and every attack off), as CinderArcanistTests places its own.
    public sealed class HeroSkillsFlowTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/Gameplay/Gameplay.unity";
        private CombatSetup _setup;
        private Health _hero;
        private AbilityController _ability;
        private Targeting _targeting;

        [UnityTearDown]
        public IEnumerator Unload()
        {
            Time.timeScale = 1;
            Time.captureDeltaTime = 0;
            Scene scene = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Skills cleanup"));
            yield return SceneManager.UnloadSceneAsync(scene);
            TestProfile.End();
        }

        private IEnumerator Load(string heroId, string skills)
        {
            TestProfile.Begin();
            if (heroId != null)
                File.WriteAllText(DevelopmentStart.HeroPath, heroId);
            if (skills != null)
                File.WriteAllText(DevelopmentStart.SkillsPath, skills);
            Time.timeScale = 1;
            Time.captureDeltaTime = 1f / 60;
            yield return SceneManager.LoadSceneAsync(ScenePath);
            yield return null;
            _setup = Object.FindFirstObjectByType<CombatSetup>();
            _hero = GameObject.Find("Vanguard").GetComponent<Health>();
            _ability = _hero.GetComponent<AbilityController>();
            _targeting = _hero.GetComponent<Targeting>();
        }

        private Health[] Dummies(params Vector2[] offsets)
        {
            Object.FindFirstObjectByType<EncounterController>().enabled = false;
            foreach (AttackController attack in Object.FindObjectsByType<AttackController>(FindObjectsSortMode.None))
                attack.enabled = false;
            var dummies = new Health[offsets.Length];
            for (int i = 0; i < offsets.Length; i++)
            {
                dummies[i] = new GameObject("Skill dummy " + i).AddComponent<Health>();
                dummies[i].Initialize(1000);
                dummies[i].transform.position = _hero.transform.position +
                                                new Vector3(offsets[i].x, ArenaFloor.WorldY(offsets[i].y), 0f);
            }
            _targeting.SetCandidates(dummies);
            return dummies;
        }

        [UnityTest]
        public IEnumerator WithoutTheFileARunCarriesSkillOneAlone()
        {
            yield return Load(null, null);
            Assert.That(_ability.Ability, Is.Not.Null);
            Assert.That(_ability.SkillAt(2), Is.Null);
            Assert.That(_ability.SkillAt(3), Is.Null);
            Assert.That(_ability.TryUse(2), Is.False);
            Assert.That(_ability.Dash, Is.Null);
            Assert.That(_setup.Hero.SkillAt(2), Is.Not.Null, "the asset carries it; the run does not");
        }

        [UnityTest]
        public IEnumerator TheVanguardsGuardHoldsItsSecondsAndItsRushStrikesDazesAndStopsOnImpact()
        {
            yield return Load(null, "3");
            Assert.That(_ability.SkillAt(2).Kind, Is.EqualTo(AbilityKind.Guard));
            Assert.That(_ability.SkillAt(3).Kind, Is.EqualTo(AbilityKind.Rush));
            Health[] dummies = Dummies(new Vector2(2.5f, .3f), new Vector2(3.5f, 0f), new Vector2(2f, 2f));

            Assert.That(_hero.State.DamageReduction, Is.EqualTo(0f));
            Assert.That(_ability.TryUse(2), Is.True);
            Assert.That(_hero.State.DamageReduction, Is.EqualTo(.5f), "+100 armour on a bare hero");
            for (int frame = 0; frame < 250; frame++)
                yield return null;
            Assert.That(_hero.State.DamageReduction, Is.EqualTo(0f), "the guard ends after its 4 s");

            // The Rush stops on impact (SkillRule.RushStopsOnImpact): 0.6 short of the first enemy ahead, striking up to it.
            float startX = _hero.transform.position.x;
            Assert.That(_ability.TryUse(3, Vector2.right), Is.True);
            Assert.That(dummies[0].Current, Is.EqualTo(1000f - 15f).Within(1e-3f), "the first on the path: struck");
            Assert.That(dummies[1].Current, Is.EqualTo(1000f), "beyond the impact: spared");
            Assert.That(dummies[2].Current, Is.EqualTo(1000f), "off the path");
            Assert.That(dummies[0].GetComponent<SlowStatus>().IsDazed, Is.True);
            for (int frame = 0; frame < 30; frame++)
                yield return null;
            Assert.That(_hero.transform.position.x - startX, Is.EqualTo(2.5f - SkillRule.RushImpactGap).Within(.05f),
                "the Rush halts 0.6 short of what it struck");
            Assert.That(dummies[0].GetComponent<SlowStatus>().IsDazed, Is.False, "the daze lasts 0.5 s");
            Assert.That(_ability.TryUse(3, Vector2.right), Is.False, "cooling down");
        }

        [UnityTest]
        public IEnumerator IlyasGlyphSlowsAndLightsAndItsWardBurnsTheStriker()
        {
            yield return Load("hero_cinder_arcanist", "3");
            Assert.That(_ability.SkillAt(2).Kind, Is.EqualTo(AbilityKind.Sigil));
            Assert.That(_ability.SkillAt(3).Kind, Is.EqualTo(AbilityKind.Ward));
            Health[] dummies = Dummies(new Vector2(1f, 0f), new Vector2(0f, 2.4f), new Vector2(3f, 0f));

            Assert.That(_ability.TryUse(2), Is.True);
            yield return null;
            yield return null;
            SlowStatus inside = dummies[0].GetComponent<SlowStatus>();
            Assert.That(inside, Is.Not.Null);
            Assert.That(inside.Current, Is.EqualTo(.4f), "40 % slower inside");
            Assert.That(inside.CurrentHeat, Is.EqualTo(2f), "its burn doubled");
            Assert.That(dummies[0].GetComponent<BurnStatus>().IsBurning, Is.True, "an unburnt enemy inside catches a stack");
            Assert.That(dummies[1].GetComponent<SlowStatus>(), Is.Not.Null, "2.4 from the glyph's centre is inside");
            Assert.That(dummies[2].GetComponent<SlowStatus>(), Is.Null, "3 is outside");

            Assert.That(_ability.TryUse(3), Is.True);
            Assert.That(_hero.State.Ward, Is.EqualTo(18f).Within(1e-4f), "20 % of 90");
            float health = _hero.Current;
            _hero.ApplyDamage(new DamageContext(10f, dummies[2]));
            Assert.That(_hero.Current, Is.EqualTo(health), "the ward took the blow");
            Assert.That(dummies[2].GetComponent<BurnStatus>().Stacks, Is.EqualTo(SkillRule.WardStacks), "the striker caught fire");
            for (int frame = 0; frame < 130; frame++)
                yield return null;
            Assert.That(_hero.State.Ward, Is.EqualTo(0f), "the ward ends after its 2 s");
        }
    }
}
