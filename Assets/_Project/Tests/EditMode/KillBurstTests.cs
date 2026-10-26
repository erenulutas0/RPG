using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Cinder Nova, the Staff's evolution, clause by clause (KillBurstRule): an enemy a strike fells bursts once where it
    // fell, for a share of the weapon's plain damage, onto every other living enemy within KillBurstRadius. The Staff's
    // 11 damage and 75 % splash are what these read against.
    public sealed class KillBurstTests
    {
        private static readonly DescentSimulation.Floor[] Descent = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

        // Who stands near whom, as a test says it, and who was asked about: the runtime's only window on the floor.
        private sealed class MapNeighbourhood : INeighbourhood
        {
            private readonly Dictionary<IDamageable, IDamageable[]> _near = new Dictionary<IDamageable, IDamageable[]>();
            public readonly List<IDamageable> Asked = new List<IDamageable>();
            public readonly List<float> Radii = new List<float>();

            public MapNeighbourhood Around(IDamageable centre, params IDamageable[] near)
            {
                _near[centre] = near;
                return this;
            }

            public IReadOnlyList<IDamageable> Near(IDamageable centre, float radius)
            {
                Asked.Add(centre);
                Radii.Add(radius);
                return _near.TryGetValue(centre, out IDamageable[] near) ? near : Array.Empty<IDamageable>();
            }
        }

        // A weapon carrying a burst of the given share, granted the way the evolution grants it.
        private static WeaponRuntime Bursting(DescentSimulation.HeroWeapon weapon, float share)
        {
            WeaponRuntime runtime = weapon.CreateRuntime();
            runtime.AddModifier(UpgradeStat.KillBurst, new StatModifier(ModifierOperation.Flat, share));
            return runtime;
        }

        [Test]
        public void KillBurstIsAWeaponStatAndOnlyAWeaponStat()
        {
            Assert.That((int)UpgradeStat.KillBurst, Is.EqualTo(13), "Appended after Area, never reordered.");
            Assert.That(WeaponRuntime.Owns(UpgradeStat.KillBurst), Is.True);
            Assert.That(HeroStats.Owns(UpgradeStat.KillBurst), Is.False);
            foreach (UpgradeStat stat in (UpgradeStat[])Enum.GetValues(typeof(UpgradeStat)))
                Assert.That(WeaponRuntime.Owns(stat) ^ HeroStats.Owns(stat), Is.True, $"{stat} has exactly one owner");
            Assert.That(DescentSimulation.Staff().CreateRuntime().KillBurst, Is.Zero, "No weapon is authored with one.");
        }

        [Test]
        public void NoBurstMeansNoBurstAndNothingIsGathered()
        {
            WeaponRuntime staff = DescentSimulation.Staff().CreateRuntime();
            var hero = new HealthState(100f);
            var target = new HealthState(5f);
            var splashed = new HealthState(5f);
            var bystander = new HealthState(500f);
            var around = new MapNeighbourhood().Around(target, bystander).Around(splashed, bystander);

            Assert.That(staff.TryAttack(target, hero, new IDamageable[] { splashed }, null, null, around), Is.True);

            Assert.That(target.IsAlive, Is.False);
            Assert.That(splashed.IsAlive, Is.False, "The strike felled both.");
            Assert.That(around.Asked, Is.Empty, "A weapon with no burst never asks who stands near a kill.");
            Assert.That(staff.KillBurstsFired, Is.Zero);
            Assert.That(bystander.Current, Is.EqualTo(500f));
        }

        [Test]
        public void AStaffKillBurstsOnEveryNeighbourWithinTheRadiusForItsShareAndNotBeyond()
        {
            // Through the simulation's strike path, whose circles are the scene's: the Staff fells a 5-health enemy one
            // unit out, and its burst reaches 1.5 units past it and not 1.7. Both neighbours stand inside the Staff's 3.5
            // splash and take 75 % of the 11 from it; only the near one takes the burst on top.
            var loadout = new WeaponLoadout(0);
            loadout.TryAdd("weapon_staff", Bursting(DescentSimulation.Staff(), 0.4f));
            WeaponRuntime staff = loadout.At(0).Runtime;
            var target = new HealthState(5f);
            var near = new HealthState(500f);
            var beyond = new HealthState(500f);
            var enemies = new[] { target, near, beyond };
            var motion = new PackMotion(DescentSimulation.BodySpacing, PackLayout.HalfWidth);
            motion.Add(target, 0f, 1f, 0f, 1f);
            motion.Add(near, 1.5f, 1f, 0f, 1f);
            motion.Add(beyond, -1.7f, 1f, 0f, 1f);

            Assert.That(staff.KillBurstRadius, Is.EqualTo(KillBurstRule.Radius), "At Area 1 the authored radius, exactly.");
            Assert.That(DescentSimulation.StrikeInSlotOrder(loadout, motion, enemies, 0f, 0f), Is.EqualTo(1));

            Assert.That(target.IsAlive, Is.False);
            Assert.That(staff.KillBurstsFired, Is.EqualTo(1));
            Assert.That(500f - near.Current, Is.EqualTo(11f * 0.75f + 11f * 0.4f).Within(1e-4f),
                "The splash, and 40 % of the 11 from the burst.");
            Assert.That(500f - beyond.Current, Is.EqualTo(11f * 0.75f).Within(1e-4f), "1.7 units out: the splash alone.");
        }

        [Test]
        public void EachEnemyTheStrikeFellsBurstsOnceAndABurstsKillBurstsNothing()
        {
            // The strike fells its target with the 11 and a splashed neighbour with the 8.25; each bursts once for the
            // whole 11, in the order it fell. The target's burst fells a frail enemy, which bursts nothing, and whose
            // death callback cannot make the weapon strike again.
            WeaponRuntime staff = Bursting(DescentSimulation.Staff(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5f);
            var splashed = new HealthState(5f);
            var survivor = new HealthState(500f);
            var frail = new HealthState(1f);
            var pastFrail = new HealthState(500f);
            var nearSplashed = new HealthState(500f);
            var around = new MapNeighbourhood()
                .Around(target, splashed, survivor, frail)
                .Around(splashed, target, survivor, nearSplashed)
                .Around(frail, pastFrail);
            int retried = 0;
            frail.Died += () =>
            {
                retried++;
                Assert.That(staff.TryAttack(survivor, hero, null, null, null, around), Is.False);
            };

            Assert.That(staff.TryAttack(target, hero, new IDamageable[] { splashed, survivor }, null, null, around), Is.True);

            Assert.That(around.Asked, Is.EqualTo(new IDamageable[] { target, splashed }),
                "The two the strike felled, in the order they fell, and never the one a burst felled.");
            Assert.That(staff.KillBurstsFired, Is.EqualTo(2));
            Assert.That(frail.IsAlive, Is.False);
            Assert.That(retried, Is.EqualTo(1));
            Assert.That(staff.AttacksMade, Is.EqualTo(1), "A burst is not a strike, and the callback's strike was refused.");
            Assert.That(pastFrail.Current, Is.EqualTo(500f), "A burst's kill bursts nothing: the burst never chains.");
            Assert.That(500f - survivor.Current, Is.EqualTo(8.25f + 11f + 11f).Within(1e-4f),
                "The splash, then one burst for each kill it stood near.");
            Assert.That(500f - nearSplashed.Current, Is.EqualTo(11f).Within(1e-4f));
            Assert.That(around.Radii, Is.All.EqualTo(KillBurstRule.Radius));
        }

        [Test]
        public void ABurstNeverCritsAndIsDealtOffThePlainDamage()
        {
            WeaponRuntime staff = Bursting(DescentSimulation.Staff(), 0.5f);
            staff.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, 1f));
            var hero = new HealthState(100f);
            var target = new HealthState(5f);
            var bystander = new HealthState(500f);
            bool felledByACrit = false;
            target.Damaged += context => felledByACrit = context.IsCritical;
            var landed = new List<DamageContext>();
            bystander.Damaged += landed.Add;

            Assert.That(staff.TryAttack(target, hero, null, null, null, new MapNeighbourhood().Around(target, bystander)),
                Is.True);

            Assert.That(felledByACrit, Is.True, "The blow that felled the target was a 22-damage crit.");
            Assert.That(landed.Count, Is.EqualTo(1));
            Assert.That(landed[0].IsCritical, Is.False, "A burst never reports itself critical.");
            Assert.That(landed[0].Amount, Is.EqualTo(5.5f).Within(1e-4f), "Half of the Staff's 11, not half of the 22.");
            Assert.That(landed[0].Source, Is.SameAs(hero), "And it carries the attacker, as every hit does.");
        }

        [Test]
        public void AreaWidensTheBurst()
        {
            // A bystander 1.8 units from the target: beyond 1.6, inside 1.6 x 1.2 = 1.92. It stands inside the splash at
            // either Area, so the difference is the burst alone.
            float Taken(float area)
            {
                var loadout = new WeaponLoadout(0);
                WeaponRuntime staff = Bursting(DescentSimulation.Staff(), 1f);
                if (area > 0f)
                    staff.AddModifier(UpgradeStat.Area, new StatModifier(ModifierOperation.Percent, area));
                loadout.TryAdd("weapon_staff", staff);
                var target = new HealthState(5f);
                var bystander = new HealthState(500f);
                var motion = new PackMotion(DescentSimulation.BodySpacing, PackLayout.HalfWidth);
                motion.Add(target, 0f, 1f, 0f, 1f);
                motion.Add(bystander, 1.8f, 1f, 0f, 1f);
                Assert.That(DescentSimulation.StrikeInSlotOrder(loadout, motion, new[] { target, bystander }, 0f, 0f),
                    Is.EqualTo(1));
                Assert.That(staff.KillBurstsFired, Is.EqualTo(1), "The kill burst at either Area; only its reach differs.");
                return 500f - bystander.Current;
            }

            Assert.That(Taken(0f), Is.EqualTo(11f * 0.75f).Within(1e-4f), "At Area 1 the burst stops short of 1.8.");
            Assert.That(Taken(0.2f), Is.EqualTo(11f * 0.75f + 11f).Within(1e-4f), "At Area 1.2 it reaches 1.92.");

            WeaponRuntime reached = Bursting(DescentSimulation.Staff(), 1f);
            reached.AddModifier(UpgradeStat.Area, new StatModifier(ModifierOperation.Percent, 0.2f));
            Assert.That(reached.KillBurstRadius, Is.EqualTo(KillBurstRule.Radius * 1.2f).Within(1e-5f));
            reached.AddModifier(UpgradeStat.Area, DescentSimulation.Reach().ModifierFor(UpgradeRarity.Common));
            Assert.That(reached.KillBurstRadius, Is.EqualTo(KillBurstRule.Radius * 1.28f).Within(1e-5f),
                "The Reach tome is one more percent bucket on the same factor.");
            Assert.That(reached.KillBurst, Is.EqualTo(1f), "Area changes how far, never how hard.");
        }

        [Test]
        public void ASplintersKillAndAPulsesKillBurstNothing()
        {
            // The Daggers crit on their third strike and the splinter fells a 1-health enemy near the target.
            WeaponRuntime daggers = Bursting(DescentSimulation.Daggers(), 1f);
            daggers.AddModifier(UpgradeStat.CritSplash, new StatModifier(ModifierOperation.Flat, 1f));
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var splintered = new HealthState(1f);
            var bystander = new HealthState(500f);
            var aroundTheSplinter = new MapNeighbourhood().Around(splintered, bystander);
            for (int strike = 1; strike <= 3; strike++)
            {
                daggers.Tick(10f);
                Assert.That(daggers.TryAttack(target, hero, null, null, new IDamageable[] { splintered }, aroundTheSplinter),
                    Is.True);
            }

            Assert.That(splintered.IsAlive, Is.False);
            Assert.That(daggers.SplintersFired, Is.EqualTo(1));
            Assert.That(daggers.KillBurstsFired, Is.Zero, "A splinter's kill starts nothing.");
            Assert.That(aroundTheSplinter.Asked, Is.Empty);

            // The Staff's fourth strike pulses round the hero and the pulse fells a 1-health enemy standing there.
            WeaponRuntime staff = Bursting(DescentSimulation.Staff(), 1f);
            staff.AddModifier(UpgradeStat.PulseDamage, new StatModifier(ModifierOperation.Flat, 1f));
            var pulsed = new HealthState(1f);
            var aroundThePulse = new MapNeighbourhood().Around(pulsed, bystander);
            for (int strike = 1; strike <= PulseRule.Every; strike++)
            {
                staff.Tick(10f);
                Assert.That(staff.TryAttack(target, hero, null, new IDamageable[] { pulsed }, null, aroundThePulse), Is.True);
            }

            Assert.That(pulsed.IsAlive, Is.False);
            Assert.That(staff.PulsesFired, Is.EqualTo(1));
            Assert.That(staff.KillBurstsFired, Is.Zero, "Nor does a pulse's.");
            Assert.That(aroundThePulse.Asked, Is.Empty);
            Assert.That(bystander.Current, Is.EqualTo(500f));
        }

        [Test]
        public void ACallerWithNoNeighbourhoodStillBurstsItJustCatchesNobody()
        {
            WeaponRuntime staff = Bursting(DescentSimulation.Staff(), 1f);
            var target = new HealthState(5f);

            Assert.That(staff.TryAttack(target, new HealthState(100f)), Is.True);

            Assert.That(staff.KillBurstsFired, Is.EqualTo(1), "The kill burst, as a pulse with no list still pulses.");
            Assert.That(KillBurstRule.Bursts(false, 1f), Is.False, "An enemy the strike did not fell never bursts.");
            Assert.That(KillBurstRule.Bursts(true, 0f), Is.False, "Nor does a kill with no share behind it.");
            Assert.That(KillBurstRule.DamageOf(11f, 0.4f), Is.EqualTo(4.4f).Within(1e-6f));
            Assert.Throws<ArgumentOutOfRangeException>(() => KillBurstRule.DamageOf(-1f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => KillBurstRule.DamageOf(1f, float.NaN));
        }

        // Through whole Descents: a Staff carrying a burst bursts and one without never does. The pinned Descent
        // numbers elsewhere are the second arm, unchanged, because no shipped weapon carries one.
        [Test]
        public void AStaffWithABurstBurstsThroughADescentAndOneWithoutNeverDoes()
        {
            DescentSimulation.HeroWeapon bursting = DescentSimulation.Staff();
            bursting.KillBurstShare = 0.4f;
            DescentSimulation.Result with = DescentSimulation.Run(Descent, 0, true, null, bursting);
            DescentSimulation.Result without = DescentSimulation.Run(Descent, 0, true, null, DescentSimulation.Staff());

            Assert.That(without.KillBurstsFired, Is.Zero);
            Assert.That(with.KillBurstsFired, Is.GreaterThan(0));
            Assert.That(with.KillBurstsFired, Is.LessThanOrEqualTo(with.Kills), "At most one burst a kill.");
        }
    }
}
