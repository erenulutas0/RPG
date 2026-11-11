using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/46's Riftblade measured in the foundry simulation before anything is built (research 2026-11-11): its rules
    // (RiftbladeRules.cs) each do what docs/46 section 2 names, and the simulation's Riftblade plays them - the primed crit,
    // the wounds, the dash and its guard, Sever - while the heroes the game has never reach any of it. Nothing in the game
    // reads these rules: they are written to move into Scripts as they stand once the owner says go.
    public sealed class RiftbladeSimulationTests
    {
        private const float Frame = 1f / 60f;

        [Test]
        public void APoolPaysEveryWoundInFullOverItsTicks()
        {
            Assert.That(BleedRule.TicksPerWound * BleedRule.TickSeconds, Is.EqualTo(BleedRule.DurationSeconds));
            Assert.That(BleedRule.WoundOf(20f, BleedRule.WoundShare), Is.EqualTo(12f).Within(1e-5f), "60 % of the crit as it landed");
            var bleed = new BleedState();
            Assert.That(bleed.Tick(1f), Is.Zero, "nothing bleeds without a wound");
            bleed.Wound(12f);
            float paid = 0f;
            int frames = 0, ticks = 0;
            while (bleed.IsBleeding && frames < 1000)
            {
                float tick = bleed.Tick(Frame);
                if (tick > 0f)
                {
                    ticks++;
                    Assert.That(tick, Is.EqualTo(2f).Within(1e-4f), "the pool spread over the ticks left");
                }
                paid += tick;
                frames++;
            }
            Assert.That(paid, Is.EqualTo(12f).Within(1e-4f), "the wound paid in full");
            Assert.That(ticks, Is.EqualTo(BleedRule.TicksPerWound));
            Assert.That(frames, Is.EqualTo(180).Within(1), "out after its 3 s");
            Assert.That((bleed.Pool, bleed.TicksLeft), Is.EqualTo((0f, 0)));
        }

        [Test]
        public void ARefreshStretchesThePoolAndLosesNothing()
        {
            var bleed = new BleedState();
            bleed.Wound(12f);
            float paid = 0f;
            for (int i = 0; i < 72; i++)
                paid += bleed.Tick(Frame);
            Assert.That(paid, Is.EqualTo(4f).Within(1e-4f), "two ticks in 1.2 s");
            Assert.That(bleed.TicksLeft, Is.EqualTo(4));
            bleed.Wound(6f);
            Assert.That(bleed.TicksLeft, Is.EqualTo(BleedRule.TicksPerWound), "the bleed's 3 s back to full");
            Assert.That(bleed.Pool, Is.EqualTo(14f).Within(1e-4f), "the wound joins what is left");
            Assert.That(bleed.Wounds, Is.EqualTo(2));
            int frames = 0;
            while (bleed.IsBleeding && frames < 1000)
            {
                paid += bleed.Tick(Frame);
                frames++;
            }
            Assert.That(paid, Is.EqualTo(18f).Within(1e-3f), "every wound paid in full");
            Assert.That(frames * Frame, Is.InRange(2.5f, BleedRule.DurationSeconds + 1e-3f), "six ticks on the bleed's own clock");
        }

        [Test]
        public void FramesThatSumToATickStillPayIt()
        {
            var bleed = new BleedState();
            bleed.Wound(6f);
            float paid = 0f;
            for (int i = 0; i < 29; i++)
                paid += bleed.Tick(Frame);
            Assert.That(paid, Is.Zero, "no tick before half a second");
            paid += bleed.Tick(Frame);
            Assert.That(paid, Is.EqualTo(1f).Within(1e-5f), "thirty frames of 1/60 s are a tick");
            Assert.That(bleed.Tick(2f), Is.EqualTo(4f).Within(1e-4f), "a long frame pays the ticks it holds");
            Assert.That(bleed.Tick(.5f), Is.EqualTo(1f).Within(1e-4f), "the last tick pays what is left");
            Assert.That(bleed.IsBleeding, Is.False);
            Assert.That(() => bleed.Wound(-1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => bleed.Tick(-1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void TheStridePrimesACritAfterItsWalkAndItsClock()
        {
            Assert.That(MomentumRule.Stride(1f, 2f, 4f, 6f), Is.EqualTo(5f), "the floor's metric");
            var stride = new MomentumState();
            Assert.That(stride.IsPrimed, Is.False, "nothing walked");
            stride.Walk(2.4f);
            Assert.That(stride.IsPrimed, Is.False);
            stride.Walk(.1f);
            Assert.That(stride.IsPrimed, Is.True, "2.5 units walked, and the hero starts with the clock run out");
            Assert.That(stride.Spend(), Is.True);
            Assert.That(stride.IsPrimed, Is.False, "spending starts the walk and the clock again");
            stride.Walk(10f);
            Assert.That(stride.Walked, Is.EqualTo(MomentumRule.StrideUnits), "nothing banked");
            for (int i = 0; i < 71; i++)
                stride.Tick(Frame);
            Assert.That(stride.IsPrimed, Is.False, "1.2 s not passed yet: the clock binds");
            stride.Tick(Frame);
            Assert.That(stride.IsPrimed, Is.True, "seventy-two frames are 1.2 s");
            Assert.That(stride.Spent, Is.EqualTo(1));
            Assert.That(new MomentumState().Spend(), Is.False, "nothing primed, nothing spent");
            Assert.That(() => stride.Walk(-1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(MomentumRule.MultiplierOf(DescentSimulation.Daggers().Pattern), Is.EqualTo(2f), "the Daggers' own x2");
            Assert.That(MomentumRule.MultiplierOf(DescentSimulation.Sword().Pattern), Is.EqualTo(MomentumRule.CritMultiplier),
                "a rolled crit's x2 on a weapon that authors none");
        }

        [Test]
        public void TheDashTravelsItsLengthInItsTimeAndGuardsFromItsFirstStep()
        {
            var dash = new PhaseDashState();
            Assert.That(dash.Step(Frame), Is.Zero);
            Assert.That(dash.Guarded, Is.False, "no dash, no guard");
            dash.Begin(3f, 4f);
            Assert.That(dash.HeadingX, Is.EqualTo(.6f).Within(1e-6f));
            Assert.That(dash.HeadingY, Is.EqualTo(.8f).Within(1e-6f));
            float travelled = 0f;
            int moving = 0, guarded = 0;
            for (int i = 0; i < 60; i++)
            {
                float units = dash.Step(Frame);
                if (units > 0f)
                    moving++;
                if (dash.Guarded)
                    guarded++;
                travelled += units;
            }
            Assert.That(travelled, Is.EqualTo(PhaseDashRule.Length).Within(1e-4f), "3 floor units");
            Assert.That(moving, Is.EqualTo(12), "in 0.2 s");
            Assert.That(guarded, Is.EqualTo(18), "guarded 0.3 s from its first step");
            Assert.That(dash.Dashes, Is.EqualTo(1));
            dash.Begin(0f, 0f);
            Assert.That((dash.HeadingX, dash.HeadingY), Is.EqualTo((0f, -1f)), "no heading at all: down the floor");

            var clock = new ActiveClock(PhaseDashRule.CooldownSeconds);
            Assert.That(clock.TrySpend(), Is.True);
            Assert.That(clock.TrySpend(), Is.False, "cooling down");
            clock.Tick(PhaseDashRule.CooldownSeconds);
            Assert.That((clock.IsReady, clock.Uses), Is.EqualTo((true, 1)));
        }

        [Test]
        public void SeverMarksAndExecutesUnderItsThreshold()
        {
            Assert.That(SeverRule.DamageOn(10f, 100f, 100f), Is.EqualTo(13f).Within(1e-5f), "the mark's +30 %");
            Assert.That(SeverRule.DamageOn(10f, 31f, 100f), Is.EqualTo(13f).Within(1e-5f), "above the threshold, the mark alone");
            Assert.That(SeverRule.DamageOn(10f, 29f, 100f), Is.EqualTo(26f).Within(1e-5f), "below it, the execute's x2 on top");
        }

        // The autopilot's dash (docs/46 section 3 step 6): pressed as the Warden's blow comes due within the guard, a frame
        // before it for the walk step that takes the press; out of a surround of four within their reach under half health.
        [Test]
        public void ThePilotsDashComesAsTheBlowIsDueAndOutOfASurround()
        {
            float due = 1f - (PhaseDashRule.GuardSeconds - Frame) / WardenRule.Telegraph;
            Assert.That(DashFootwork.BlowDueWithinGuard(0f, Frame), Is.False, "the mark's first frame is too early");
            Assert.That(DashFootwork.BlowDueWithinGuard(due - .01f, Frame), Is.False);
            Assert.That(DashFootwork.BlowDueWithinGuard(due + .01f, Frame), Is.True);
            Assert.That(DashFootwork.Surrounded(4, 40f, 85f), Is.True);
            Assert.That(DashFootwork.Surrounded(3, 40f, 85f), Is.False, "three is no surround");
            Assert.That(DashFootwork.Surrounded(6, 43f, 85f), Is.False, "at half health or more it kites");
        }

        [Test]
        public void TheRiftbladeIsTheProposalAndNoOtherHeroCarriesItsKit()
        {
            DescentSimulation.FoundryHero nix = DescentSimulation.Riftblade();
            Assert.That((nix.Id, nix.MaximumHealth, nix.Weapon.Id), Is.EqualTo(("hero_riftblade", 85f, DescentSimulation.DaggersId)));
            Assert.That(nix.Ability, Is.Null, "Phase Dash stands in the ability's place");
            Assert.That(nix.EmberMemory, Is.False);
            Assert.That(nix.Rift, Is.Not.Null);
            Assert.That((nix.Rift.StrideOnEveryWeapon, nix.Rift.HoldsForTheDash, nix.Rift.HoldsWhileReady), Is.EqualTo((false, true, false)),
                "the passive on the Daggers alone; the hold through the telegraph alone");
            Assert.That(nix.Rift.Active, Is.EqualTo(DescentSimulation.RiftActive.PhaseDash));
            Assert.That((nix.Rift.WoundShare, nix.Rift.DashCooldown), Is.EqualTo((BleedRule.WoundShare, PhaseDashRule.CooldownSeconds)));
            Assert.That(DescentSimulation.Vanguard().Rift, Is.Null);
            Assert.That(DescentSimulation.CinderArcanist().Rift, Is.Null);
        }

        // The game's heroes never reach the Riftblade's paths: none of its accounting moves on their floors (the default
        // batch is held to the byte against main's by hand, docs/46's measured section).
        [Test]
        public void TheGamesHeroesTakeNoneOfTheRiftbladesPaths()
        {
            DescentSimulation.FoundryLevers two = FoundryBatch.Shipped().Copy("two floors");
            two.Floors = 2;
            foreach (DescentSimulation.FoundryHero hero in new[] { DescentSimulation.Vanguard(), DescentSimulation.CinderArcanist() })
                for (DescentSimulation.FoundryResult floor = DescentSimulation.RunFoundry(hero, 11, two); floor != null; floor = floor.NextFloor)
                {
                    string where = $"{hero.Name} floor {floor.Floor}";
                    Assert.That((floor.MomentumCrits, floor.WoundsOpened, floor.PrimedSplinters, floor.Dashes, floor.DashTurned,
                        floor.DashSlamsTurned, floor.Severs), Is.EqualTo((0, 0, 0, 0, 0, 0, 0)), where);
                    Assert.That((floor.DashHoldSeconds, floor.DealtByBleed, floor.WardenFromBleed), Is.EqualTo((0f, 0f, 0f)), where);
                    Assert.That(floor.WeaponWork.All(w => w.DealtByBleed == 0f && w.BleedWarden == 0f && w.BleedEliteLeader == 0f), Is.True, where);
                    Assert.That(floor.Kills.Any(k => k.By == DescentSimulation.FoundryKill.KilledByBleed), Is.False, where);
                }
        }

        // Quicksilver Stride on the Daggers: the primed crits land, the Daggers' crits open wounds whose ticks are the
        // Daggers' work - no landing of theirs, their kills the bleed's - and the floor's accounting adds up with the bleed
        // beside the weapons' landings.
        [Test]
        public void TheStrideCritsTheDaggersAndTheirCritsBleed()
        {
            int bleedKills = 0;
            foreach (int seed in new[] { 11, 33 })
            {
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(DescentSimulation.Riftblade(), seed);
                string where = $"seed {seed}";
                List<DescentSimulation.FoundryWeaponWork> work = run.WeaponWork;
                Assert.That(work[0].Id, Is.EqualTo(DescentSimulation.DaggersId), where + ": the Daggers are slot 0");
                Assert.That(run.MomentumCrits, Is.GreaterThan(0), where + ": the walk primes crits");
                Assert.That(run.WoundsOpened, Is.GreaterThan(0), where + ": crits wound");
                Assert.That(run.DealtByBleed, Is.GreaterThan(0f), where);
                Assert.That(work[0].DealtByBleed, Is.EqualTo(run.DealtByBleed).Within(run.DealtByBleed * 1e-4f + .01f),
                    where + ": the Daggers alone bleed, as proposed");
                Assert.That(work.Skip(1).All(w => w.DealtByBleed == 0f), Is.True, where);
                Assert.That(work.Sum(w => w.Dealt), Is.EqualTo(run.DealtByWeapons + run.DealtByBleed).Within((run.DealtByWeapons + run.DealtByBleed) * 1e-4f + .01f),
                    where + ": the weapons' work is their landings and their wounds");
                Assert.That(work.Sum(w => w.DealtWarden), Is.EqualTo(run.WardenFromWeapons + run.WardenFromBleed).Within(run.WardenFromWeapons * 1e-4f + .01f), where);
                Assert.That((run.Dashes > 0 || run.LevelAtGate < 0) && run.Severs == 0, Is.True, where + ": Phase Dash, not Sever");
                bleedKills += run.Kills.Count(k => k.By == DescentSimulation.FoundryKill.KilledByBleed);
            }
            Assert.That(bleedKills, Is.GreaterThan(0), "a bleed kill reads the bleed");
        }

        // Phase Dash: the careful pilot holds at the Warden's feet for a ready dash and dashes as the blow comes due, 3 units
        // in 0.2 s; its guard turns what it meets - frame-perfect, the strikes round it, since the dash has left the mark
        // before the blow; a human's press comes a reaction late, and the guard turns the slam itself. With no active, or in
        // the auto-farmer's hands, nothing dashes.
        [Test]
        public void ThePhaseDashLeavesTheWardensMarkLateAndItsGuardTurnsWhatItMeets()
        {
            int dashes = 0, turned = 0;
            float held = 0f;
            foreach (int seed in new[] { 2, 3 })
            {
                string where = $"seed {seed}";
                var trace = new List<string>();
                DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(DescentSimulation.Riftblade(), seed, trace: trace.Add);
                Assert.That(run.LevelAtGate, Is.GreaterThanOrEqualTo(0), where + ": the Warden reached");
                Assert.That(run.AbilityUses, Is.InRange(run.Dashes, run.Dashes + 1), where + ": a press is a dash at the next walk step");
                dashes += run.Dashes;
                turned += run.DashTurned;
                held += run.DashHoldSeconds;
                Assert.That(LongestDash(trace), Is.InRange(2.9f, 3.05f), where + ": 3 units in 0.2 s, where no wall stands");

                DescentSimulation.FoundryHero none = DescentSimulation.Riftblade();
                none.Rift.Active = DescentSimulation.RiftActive.None;
                DescentSimulation.FoundryResult bare = DescentSimulation.RunFoundry(none, seed);
                Assert.That((bare.Dashes, bare.AbilityUses, bare.DashTurned, bare.DashHoldSeconds), Is.EqualTo((0, 0, 0, 0f)), where + ": no active");

                DescentSimulation.FoundryLevers farmer = FoundryBatch.Shipped().Copy("auto-farmer");
                farmer.Cohort = AutopilotCohort.AutoFarmer;
                DescentSimulation.FoundryResult farmed = DescentSimulation.RunFoundry(DescentSimulation.Riftblade(), seed, farmer);
                Assert.That((farmed.Dashes, farmed.DashHoldSeconds), Is.EqualTo((0, 0f)), where + ": the auto-farmer never dashes");
                Assert.That(farmed.MomentumCrits, Is.GreaterThan(0), where + ": the passive is every cohort's");
            }
            Assert.That(dashes, Is.GreaterThan(0), "the careful pilot dashes");
            Assert.That(held, Is.GreaterThan(0f), "and holds at the Warden's feet for it");
            Assert.That(turned, Is.GreaterThan(0), "the guard turns a hit");

            DescentSimulation.FoundryLevers human = FoundryBatch.Shipped().Copy("human hands");
            human.Realism = PilotRealism.Typical();
            DescentSimulation.FoundryResult late = DescentSimulation.RunFoundry(DescentSimulation.Riftblade(), 1, human);
            Assert.That(late.DashSlamsTurned, Is.GreaterThan(0), "a press a reaction late: the guard turns the Warden's slam");
            Assert.That(late.DashSlamsTurned, Is.LessThanOrEqualTo(late.SlamHits), "a turned slam is one that came down on the hero");
        }

        // The farthest the hero went in the twelve frames of a dash's travel, of every dash on the floor: the frame line
        // before the dash's own and the twelfth after it (each frame line is a frame's end).
        private static float LongestDash(List<string> trace)
        {
            float longest = 0f;
            for (int i = 0; i < trace.Count; i++)
            {
                if (!trace[i].Contains(" dash x="))
                    continue;
                int before = trace.FindLastIndex(i, line => line.Contains(" frame clock="));
                int after = i, frames = 0;
                while (frames < 12 && ++after < trace.Count)
                    if (trace[after].Contains(" frame clock="))
                        frames++;
                if (before < 0 || frames < 12)
                    continue;
                float dx = Read(trace[after], "x") - Read(trace[before], "x");
                float dy = Read(trace[after], "y") - Read(trace[before], "y");
                longest = Math.Max(longest, (float)Math.Sqrt(dx * dx + dy * dy));
            }
            return longest;
        }

        private static float Read(string line, string key)
        {
            string word = line.Split(' ').First(w => w.StartsWith(key + "=", StringComparison.Ordinal));
            return float.Parse(word.Substring(key.Length + 1), CultureInfo.InvariantCulture);
        }

        // Sever in the dash's place: it marks the heaviest within its reach, its extra is the ability's, and nothing dashes.
        [Test]
        public void SeverMarksTheHeaviestAndItsExtraIsTheAbilitys()
        {
            DescentSimulation.FoundryHero nix = DescentSimulation.Riftblade();
            nix.Rift.Active = DescentSimulation.RiftActive.Sever;
            DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(nix, 11);
            Assert.That(run.Severs, Is.GreaterThan(0));
            Assert.That(run.AbilityUses, Is.EqualTo(run.Severs));
            Assert.That((run.Dashes, run.DashHoldSeconds), Is.EqualTo((0, 0f)), "no dash");
            Assert.That(run.DealtByAbility, Is.GreaterThan(0f), "the mark's extra is the ability's");
            Assert.That(run.WeaponWork.Sum(w => w.Dealt), Is.EqualTo(run.DealtByWeapons + run.DealtByBleed).Within((run.DealtByWeapons + run.DealtByBleed) * 1e-4f + .01f),
                "the weapons' work leaves the mark's extra out");
        }

        // Quicksilver Stride on every weapon (docs/46 question 3's other way): the primed crit falls to whatever strikes
        // first, and every weapon's crits wound.
        [Test]
        public void ThePassiveOnEveryWeaponWoundsWithEveryWeapon()
        {
            DescentSimulation.FoundryHero nix = DescentSimulation.Riftblade();
            nix.Rift.StrideOnEveryWeapon = true;
            DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(nix, 11);
            Assert.That(run.WeaponWork.Count, Is.GreaterThan(1), "the hero picks up another weapon");
            Assert.That(run.WeaponWork.Count(w => w.DealtByBleed > 0f), Is.GreaterThan(1), "another weapon's crits wound");
            Assert.That(run.WeaponWork.Sum(w => w.DealtByBleed), Is.EqualTo(run.DealtByBleed).Within(run.DealtByBleed * 1e-4f + .01f));
        }
    }
}
