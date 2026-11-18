using System;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/57's skills 2 and 3 (phase 1): the pure bodies the scene's AbilityController and the foundry simulation share -
    // the skill's runtime and its effect's clock, the dash, the slow and the heat, the glyph on Burn's clock, the ward in
    // HealthState, the guard's armour on HeroStats, the build scaling and the capsule, and the autopilot's moments.
    public sealed class HeroSkillRuleTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void ASkillsEffectHoldsForItsDurationFromEachUseAndTheShippedKindsHaveNone()
        {
            AbilityRuntime guard = AbilityRuntime.Skill(AbilityKind.Guard, 0f, 2f, 20f, 4f, 100f);
            Assert.That((guard.Duration, guard.Amount, guard.IsActive, guard.IsReady), Is.EqualTo((4f, 100f, false, true)));
            Assert.That(guard.TrySpend(), Is.True);
            Assert.That(guard.IsActive, Is.True);
            Assert.That(guard.ActiveLeft, Is.EqualTo(4f));
            for (int i = 0; i < 239; i++)
                guard.Tick(Frame);
            Assert.That(guard.IsActive, Is.True, "a frame short of 4 s");
            guard.Tick(Frame);
            guard.Tick(Frame);
            Assert.That(guard.IsActive, Is.False);
            Assert.That(guard.IsReady, Is.False, "the cooldown runs on");

            var burst = new AbilityRuntime(20f, 2.5f, 8f);
            Assert.That(burst.TrySpend(), Is.True);
            Assert.That((burst.Duration, burst.Amount, burst.IsActive), Is.EqualTo((0f, 0f, false)), "the Burst acts at once");
        }

        [Test]
        public void ASkillIsMadeOnlyWithWhatItsKindNeeds()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => AbilityRuntime.Skill(AbilityKind.Burst, 20f, 2.5f, 8f, 1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => AbilityRuntime.Skill(AbilityKind.Rush, 0f, .8f, 12f, .3f, 4f),
                "a Rush strikes");
            Assert.Throws<ArgumentOutOfRangeException>(() => AbilityRuntime.Skill(AbilityKind.Sigil, 0f, 2.5f, 20f, 5f, 1f),
                "a glyph never roots");
            Assert.Throws<ArgumentOutOfRangeException>(() => AbilityRuntime.Skill(AbilityKind.Ward, 0f, 2f, 20f, 2f, 1.5f),
                "a ward is at most the whole health");
            Assert.Throws<ArgumentOutOfRangeException>(() => AbilityRuntime.Skill(AbilityKind.Guard, 0f, 2f, 20f, 0f, 100f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AbilityRuntime(20f, 2.5f, 8f, AbilityKind.Guard),
                "the shipped constructor is the Burst's and the Vent's");
            Assert.DoesNotThrow(() => AbilityRuntime.Skill(AbilityKind.Sigil, 0f, 2.5f, 20f, 5f, 0f));
        }

        [Test]
        public void TheGearsCooldownLineShortensASkillAsItDoesTheBurst()
        {
            AbilityRuntime ward = AbilityRuntime.Skill(AbilityKind.Ward, 0f, 2f, 20f, 2f, .2f);
            ward.ShortenCooldown(.9f);
            Assert.That(ward.Cooldown, Is.EqualTo(18f).Within(1e-5f));
        }

        // The Rush's 4 units in 0.3 s: 18 frames at 60 Hz carry the hero its whole length along the heading, and nothing
        // after; Phase Dash's 3 in 0.2 s is the same body with other numbers.
        [Test]
        public void ADashCarriesTheHeroItsLengthInItsTravelAlongTheHeading()
        {
            var dash = new HeroDash(4f, .3f);
            dash.Begin(3f, 4f);
            Assert.That((dash.HeadingX, dash.HeadingY), Is.EqualTo((.6f, .8f)));
            float units = 0f;
            int frames = 0;
            while (dash.IsDashing)
            {
                units += dash.Step(Frame);
                frames++;
            }
            Assert.That(units, Is.EqualTo(4f).Within(1e-3f));
            Assert.That(frames, Is.InRange(18, 19));
            Assert.That(dash.Step(Frame), Is.EqualTo(0f));

            var phase = new HeroDash(3f, .2f);
            phase.Begin(0f, 0f);
            Assert.That((phase.HeadingX, phase.HeadingY), Is.EqualTo((0f, -1f)), "no heading: straight down the floor");
            Assert.That(HeroDash.StepOf(3f, .2f, Frame, .2f), Is.EqualTo(3f / .2f * Frame));
        }

        [Test]
        public void ADashingHeroMovesThroughTheWalkAndStopsAtTheRim()
        {
            var arena = new ArenaGeometry(-9f, 9f, 9f);
            var motion = new HeroMotion(2.5f);
            motion.Place(0f, 0f, arena);
            var dash = new HeroDash(4f, .3f);
            dash.Begin(1f, 0f);
            while (dash.IsDashing)
                dash.Move(motion, arena, Frame);
            Assert.That(motion.X, Is.EqualTo(4f).Within(1e-3f));
            Assert.That(motion.Y, Is.EqualTo(0f));

            motion.Place(7f, 0f, arena);
            dash.Begin(1f, 0f);
            while (dash.IsDashing)
                dash.Move(motion, arena, Frame);
            Assert.That(motion.X, Is.LessThanOrEqualTo(9f - HeroMotion.EdgeMargin + 1e-4f), "the rim stops it as it stops the walk");
        }

        [Test]
        public void TheStrongerSlowHoldsADazeTakesTheWholeWalkAndHeatFades()
        {
            var slow = new SlowState();
            Assert.That((slow.Current, slow.CurrentHeat, slow.IsQuiet), Is.EqualTo((0f, 1f, true)));
            slow.ApplySlow(.4f, .55f);
            slow.ApplySlow(.2f, 2f);
            Assert.That(slow.Current, Is.EqualTo(.4f), "the weaker one waits");
            slow.Daze(.5f);
            Assert.That(slow.Current, Is.EqualTo(1f));
            Assert.That(slow.IsDazed, Is.True);
            slow.ApplyHeat(SkillRule.SigilHeat, .55f);
            Assert.That(slow.CurrentHeat, Is.EqualTo(2f));
            for (int i = 0; i < 31; i++)
                slow.Tick(Frame);
            Assert.That(slow.IsDazed, Is.False);
            Assert.That(slow.Current, Is.EqualTo(.4f));
            for (int i = 0; i < 4; i++)
                slow.Tick(Frame);
            Assert.That((slow.Current, slow.CurrentHeat, slow.IsQuiet), Is.EqualTo((0f, 1f, true)));
            Assert.Throws<ArgumentOutOfRangeException>(() => slow.ApplySlow(1f, 1f), "a slow is under the whole walk");
        }

        // A slow shortens the step toward the same point (lead decision 4): an enemy at 40 % covers 60 % of the ground an
        // unslowed one does, on the same line; 0 is the walk to the bit; a fresh enemy in the place walks unslowed.
        [Test]
        public void ASlowedEnemyWalksTheSameLineShorterAndAFreshOneInItsPlaceDoesNot()
        {
            var plain = new PackMotion(.9f, 2f);
            var slowed = new PackMotion(.9f, 2f);
            var zero = new PackMotion(.9f, 2f);
            plain.Add(new HealthState(50f), 0f, 8f, 2f, 1.2f);
            slowed.Add(new HealthState(50f), 0f, 8f, 2f, 1.2f);
            zero.Add(new HealthState(50f), 0f, 8f, 2f, 1.2f);
            slowed.SetSlow(0, .4f);
            zero.SetSlow(0, 0f);
            for (int frame = 0; frame < 60; frame++)
            {
                plain.Step(Frame);
                slowed.Step(Frame);
                zero.Step(Frame);
            }
            Assert.That(8f - plain.YOf(0), Is.EqualTo(2f).Within(1e-3f));
            Assert.That(8f - slowed.YOf(0), Is.EqualTo(1.2f).Within(1e-3f));
            Assert.That(slowed.XOf(0), Is.EqualTo(plain.XOf(0)), "the same line");
            Assert.That((zero.XOf(0), zero.YOf(0)), Is.EqualTo((plain.XOf(0), plain.YOf(0))), "no slow is the walk itself");

            var dead = new HealthState(1f);
            var pack = new PackMotion(.9f, 2f);
            pack.Add(dead, 0f, 8f, 2f, 1.2f);
            pack.SetSlow(0, 1f);
            dead.ApplyDamage(new DamageContext(5f));
            pack.Replace(0, new HealthState(50f), new EntryPlacement(EntrySide.Far, 0f, 0f, 8f), 2f, 1.2f);
            Assert.That(pack.SlowOf(0), Is.EqualTo(0f));
        }

        // The glyph on Burn's clock: a tick as it is drawn and every 0.5 s after it, ten in its 5 s and none after; its edge
        // is inside (the squared radius cast to float, as the scene and the simulation both read it).
        [Test]
        public void TheGlyphTicksOnBurnsClockTenTimesInFiveSeconds()
        {
            var zone = new SigilZone();
            zone.Begin(1f, 2f, 2.5f, 5f, .4f);
            var at = new List<int>();
            for (int frame = 1; frame <= 400; frame++)
                if (zone.Tick(Frame) > 0)
                    at.Add(frame);
            Assert.That(at.Count, Is.EqualTo(10));
            Assert.That(at[0], Is.EqualTo(1), "the first tick with the first step");
            Assert.That(at[1] - at[0], Is.InRange(29, 31));
            Assert.That(zone.IsLive, Is.False);
            Assert.That(zone.Tick(Frame), Is.EqualTo(0));
            Assert.That(zone.Contains(3.5f, 2f), Is.True, "the rim is inside");
            Assert.That(zone.Contains(3.6f, 2f), Is.False);
            Assert.That(SigilZone.HoldSeconds, Is.GreaterThan(BurnRule.TickSeconds), "a slow outlasts the gap between two ticks");
        }

        // The ward takes what armour let through until it is spent, and reports each landing it took; without one the
        // landing is armour's alone.
        [Test]
        public void TheWardTakesWhatArmourLetThroughAndReportsEachLanding()
        {
            var health = new HealthState(90f);
            health.SetDamageReduction(.5f);
            var took = new List<float>();
            health.WardAbsorbed += (context, absorbed) => took.Add(absorbed);
            health.RaiseWard(SkillRule.WardOf(90f, .2f));
            Assert.That(health.Ward, Is.EqualTo(18f).Within(1e-5f));
            health.ApplyDamage(new DamageContext(20f));
            Assert.That(health.Current, Is.EqualTo(90f), "10 after armour, all of it warded");
            health.ApplyDamage(new DamageContext(20f));
            Assert.That(health.Current, Is.EqualTo(88f).Within(1e-4f), "8 left of the ward, 2 through");
            Assert.That(health.Ward, Is.EqualTo(0f));
            Assert.That(took, Is.EqualTo(new[] { 10f, 8f }).Within(1e-4f));
            health.ApplyDamage(new DamageContext(20f));
            Assert.That(health.Current, Is.EqualTo(78f).Within(1e-4f));
            Assert.That(took.Count, Is.EqualTo(2));
            health.RaiseWard(5f);
            health.ClearWard();
            Assert.That(health.Ward, Is.EqualTo(0f));
        }

        // Iron Resolve's armour on the pool: half of every hit on a bare hero, 58 % over two Iron Skins' 40, and the armour's
        // own reduction to the bit once it ends.
        [Test]
        public void TheGuardsArmourRidesThePoolAndLeavesItExactlyAsItWas()
        {
            var stats = new HeroStats(100f, 2.5f, 3f);
            var health = new HealthState(100f);
            stats.Changed += () => HeroStatsBinding.Apply(stats, health, null, null);
            stats.SetGuardArmor(100f);
            Assert.That(health.DamageReduction, Is.EqualTo(.5f));
            stats.SetGuardArmor(0f);
            Assert.That(health.DamageReduction, Is.EqualTo(0f));
            stats.AddModifier(UpgradeStat.Armor, new StatModifier(ModifierOperation.Flat, 40f));
            float armour = health.DamageReduction;
            stats.SetGuardArmor(100f);
            Assert.That(health.DamageReduction, Is.EqualTo(140f / 240f).Within(1e-6f));
            stats.SetGuardArmor(0f);
            Assert.That(health.DamageReduction, Is.EqualTo(armour), "to the bit");
            Assert.That(stats.Armor, Is.EqualTo(40f), "the guard is not the cards' armour");
        }

        [Test]
        public void ASkillsDamageScalesWithTheFirstWeaponAndThePathIsACapsule()
        {
            Assert.That(SkillRule.Scaled(15f, 30f, 20f), Is.EqualTo(22.5f));
            Assert.That(SkillRule.Scaled(15f, 20f, 20f), Is.EqualTo(15f));
            Assert.That(SkillRule.Scaled(15f, 5f, 0f), Is.EqualTo(15f));
            Assert.That(SkillRule.OnPath(2f, .8f, 0f, 0f, 4f, 0f, .8f), Is.True, "the capsule's side is inside");
            Assert.That(SkillRule.OnPath(2f, .81f, 0f, 0f, 4f, 0f, .8f), Is.False);
            Assert.That(SkillRule.OnPath(4.5f, 0f, 0f, 0f, 4f, 0f, .8f), Is.True, "round the far end");
            Assert.That(SkillRule.OnPath(-.9f, 0f, 0f, 0f, 4f, 0f, .8f), Is.False, "behind the start");
            Assert.That(SkillRule.SlowOn(.4f, true), Is.EqualTo(.2f), "half the slow on a gate boss");
            Assert.That(SkillRule.Slowed(2f, .4f), Is.EqualTo(1.2f).Within(1e-6f));
        }

        [Test]
        public void TheAutopilotsMomentsAreSectionThreesRules()
        {
            Assert.That(SkillFootwork.GuardsTheBlow(true, 3, 90f, 100f), Is.True, "a fight's first blow with three at hand");
            Assert.That(SkillFootwork.GuardsTheBlow(true, 2, 90f, 100f), Is.False);
            Assert.That(SkillFootwork.GuardsTheBlow(false, 0, 69f, 100f), Is.True, "a blow that left it under 70 %");
            Assert.That(SkillFootwork.WardsTheBlow(71f, 100f), Is.False);
            Assert.That(SkillFootwork.BlowDueWithin(.5f, 1.1f, 2f, Frame), Is.True);
            Assert.That(SkillFootwork.BlowDueWithin(0f, 4f, 2f, Frame), Is.False);
            Assert.That(SkillFootwork.Surrounded(5, 59f, 100f), Is.True);
            Assert.That(SkillFootwork.Surrounded(5, 61f, 100f), Is.False);
            Assert.That(SkillFootwork.WorthTheRush(3f, 2.1f, 4f), Is.True);
            Assert.That(SkillFootwork.WorthTheRush(2f, 2.1f, 4f), Is.False, "within the Sword's reach already");
            Assert.That(SkillFootwork.DrawsTheSigil(4, false) && SkillFootwork.DrawsTheSigil(0, true), Is.True);
            Assert.That(SkillFootwork.DrawsTheSigil(3, false), Is.False);
            Assert.That(SkillFootwork.ReachOf(AbilityRuntime.Skill(AbilityKind.Rush, 15f, .8f, 12f, .3f, 4f)), Is.EqualTo(SkillRule.RushSeek));
        }

        // Lead decision 2: the careful, standing and first-card pilots time skills 2 and 3; the clumsy one presses them on
        // cooldown; the auto-farmer presses nothing. The habit sits outside All, so no cohort report lists it.
        [Test]
        public void TheClumsyCohortIsTheOneThatDoesNotTimeTheSkills()
        {
            foreach (AutopilotCohort cohort in new[] { AutopilotCohort.Careful, AutopilotCohort.Standing, AutopilotCohort.FirstCard })
                Assert.That(cohort.TimesTheSkills, Is.True, cohort.Name);
            Assert.That(AutopilotCohort.Clumsy.TimesTheSkills, Is.False);
            Assert.That(AutopilotCohort.AutoFarmer.TimesTheSkills || AutopilotCohort.AutoFarmer.UsesTheAbility, Is.False);
            Assert.That(AutopilotCohort.Habit.All & AutopilotCohort.Habit.TimesTheSkills, Is.EqualTo(AutopilotCohort.Habit.None));
        }
    }
}
