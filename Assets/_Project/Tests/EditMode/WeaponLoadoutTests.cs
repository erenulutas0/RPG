using System;
using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The loadout's own rules (docs/42, PR 2): what it holds, how many, how far a weapon levels, and how a global grant
    // reaches weapons picked up after it. How the held weapons fight is WeaponBehaviorTests' business.
    public sealed class WeaponLoadoutTests
    {
        [Test]
        public void AnEmptyLoadoutHasNoPrimary()
        {
            var loadout = new WeaponLoadout(1);

            Assert.That(loadout.Count, Is.Zero);
            Assert.That(loadout.Primary, Is.Null);
            Assert.Throws<ArgumentOutOfRangeException>(() => loadout.At(0));
        }

        [Test]
        public void SlotsNeverExceedThree()
        {
            var loadout = new WeaponLoadout(1);
            WeaponRuntime sword = DescentSimulation.Sword().CreateRuntime();

            Assert.That(loadout.TryAdd("weapon_sword", sword), Is.True);
            Assert.That(loadout.TryAdd("weapon_staff", DescentSimulation.Staff().CreateRuntime()), Is.True);
            Assert.That(loadout.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime()), Is.True);
            Assert.That(loadout.IsFull, Is.True);
            Assert.That(loadout.TryAdd("weapon_fourth", DescentSimulation.Sword().CreateRuntime()), Is.False,
                "A fourth weapon has no slot to go to.");

            Assert.That(loadout.Count, Is.EqualTo(WeaponLoadout.MaxSlots));
            Assert.That(WeaponLoadout.MaxSlots, Is.EqualTo(3));
            Assert.That(loadout.Primary, Is.SameAs(sword), "Slot 0 is the weapon added first.");
            Assert.That(loadout.At(1).WeaponId, Is.EqualTo("weapon_staff"));
            Assert.That(loadout.At(2).WeaponId, Is.EqualTo("weapon_daggers"));
            Assert.That(loadout.IndexOf("weapon_fourth"), Is.EqualTo(-1));
        }

        [Test]
        public void AWeaponIsNeverHeldTwice()
        {
            var loadout = new WeaponLoadout(1);
            WeaponRuntime sword = DescentSimulation.Sword().CreateRuntime();
            loadout.TryAdd("weapon_sword", sword);

            Assert.That(loadout.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime()), Is.False,
                "A repeat pick is a level-up, not a second Sword.");
            Assert.That(loadout.Count, Is.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => loadout.TryAdd("weapon_staff", sword),
                "Nor can one runtime fill two slots under two names.");
            Assert.That(loadout.Count, Is.EqualTo(1));
            Assert.Throws<ArgumentException>(() => loadout.TryAdd("", DescentSimulation.Staff().CreateRuntime()));
            Assert.Throws<ArgumentNullException>(() => loadout.TryAdd("weapon_staff", null));
        }

        [Test]
        public void AWeaponLevelsFromOneToEightAndNoFurther()
        {
            var loadout = new WeaponLoadout(1);
            loadout.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            Assert.That(loadout.At(0).Level, Is.EqualTo(1), "A weapon is picked up at level 1.");

            for (int level = 2; level <= WeaponLoadout.MaxLevel; level++)
            {
                Assert.That(loadout.TryLevelUp("weapon_sword"), Is.True);
                Assert.That(loadout.At(0).Level, Is.EqualTo(level));
            }

            Assert.That(WeaponLoadout.MaxLevel, Is.EqualTo(8));
            Assert.That(loadout.TryLevelUp("weapon_sword"), Is.False, "Seven picks take a weapon to level 8, and that is all.");
            Assert.That(loadout.At(0).Level, Is.EqualTo(8));
            Assert.That(loadout.TryLevelUp("weapon_staff"), Is.False, "A weapon not held cannot level.");
        }

        [Test]
        public void AGlobalGrantReachesEveryWeaponAndIsReplayedOntoALaterOne()
        {
            var loadout = new WeaponLoadout(1);
            WeaponRuntime sword = DescentSimulation.Sword().CreateRuntime();
            loadout.TryAdd("weapon_sword", sword);
            // A per-weapon line is flat and lands on its own weapon; Force is a percent and lands on all of them.
            sword.AddModifier(UpgradeStat.Damage, new StatModifier(ModifierOperation.Flat, 2f));
            var force = new StatModifier(ModifierOperation.Percent, 0.08f);
            loadout.AddGlobal(UpgradeStat.Damage, force);
            loadout.AddGlobal(UpgradeStat.Damage, force);

            // (d0 + its own flat) x (1 + every Force level): Force twice is 1.16, not 1.08 x 1.08 (docs/42, 40 6.2 rule 7).
            Assert.That(sword.Damage, Is.EqualTo((10f + 2f) * 1.16f).Within(1e-4f));

            WeaponRuntime staff = DescentSimulation.Staff().CreateRuntime();
            loadout.TryAdd("weapon_staff", staff);
            Assert.That(staff.Damage, Is.EqualTo(11f * 1.16f).Within(1e-4f),
                "A weapon picked up later receives every Force level stacked before it.");

            loadout.AddGlobal(UpgradeStat.Area, new StatModifier(ModifierOperation.Percent, 0.1f));
            Assert.That(sword.Area, Is.EqualTo(1.1f).Within(1e-6f));
            Assert.That(staff.SplashRadius, Is.EqualTo(3.5f * 1.1f).Within(1e-5f));
            Assert.That(sword.Damage, Is.EqualTo((10f + 2f) * 1.16f).Within(1e-4f), "And a grant to Area touches nothing else.");

            Assert.Throws<ArgumentOutOfRangeException>(
                () => loadout.AddGlobal(UpgradeStat.MaxHealth, new StatModifier(ModifierOperation.Flat, 10f)),
                "The hero's own stats go to HeroStats, never to every weapon.");
        }

        [Test]
        public void SlotZeroKeepsTheRunSeedAndLaterSlotsDeriveTheirOwn()
        {
            const int Seed = 90210;
            var loadout = new WeaponLoadout(Seed);
            loadout.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            loadout.TryAdd("weapon_staff", DescentSimulation.Staff().CreateRuntime());
            loadout.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime());

            Assert.That(loadout.At(0).Runtime.Seed, Is.EqualTo(Seed), "So a one-weapon run crits as it always has.");
            Assert.That(loadout.At(1).Runtime.Seed, Is.EqualTo(WeaponLoadout.SeedFor(Seed, 1)));
            Assert.That(loadout.At(2).Runtime.Seed, Is.EqualTo(WeaponLoadout.SeedFor(Seed, 2)));
            Assert.That(new[] { loadout.At(0).Runtime.Seed, loadout.At(1).Runtime.Seed, loadout.At(2).Runtime.Seed },
                Is.Unique);
            Assert.That(WeaponLoadout.SeedFor(Seed, 1), Is.EqualTo(WeaponLoadout.SeedFor(Seed, 1)), "A seed replays.");
            Assert.That(WeaponLoadout.SeedFor(Seed, 1), Is.Not.EqualTo(WeaponLoadout.SeedFor(Seed + 1, 1)));
            Assert.Throws<ArgumentOutOfRangeException>(() => WeaponLoadout.SeedFor(Seed, WeaponLoadout.MaxSlots));
        }
    }
}
