using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The gear on a run (docs/54 phase 5): GearStats.Apply, the one body CombatSetup and the foundry simulation both call as
    // a run begins, turns a loadout's clamped totals into modifiers on the run's own stats - an empty loadout into none at
    // all; a known loadout into known numbers; the caps bind; section 7.2's power indices read back off the run's own
    // weapon and hero; the five stats the run has no rule for stay inert; a weapon picked up later wears the gear too; the
    // weapon's gear is the carried weapon's own. Also in Tools/CombatChecks.
    public sealed class GearApplyTests
    {
        private const float BaseDamage = 10f;
        private const float BaseInterval = .8f;
        private const float BaseHealth = 100f;
        private const float BaseSpeed = 3.5f;

        private static HeroStats Sheet() => new HeroStats(BaseHealth, BaseSpeed, HeroStamina.DefaultBar);

        private static WeaponRuntime Sword() => new WeaponRuntime(BaseDamage, BaseInterval, 1.5f);

        private static WeaponLoadout Holding(WeaponRuntime sword)
        {
            var loadout = new WeaponLoadout(17);
            Assert.That(loadout.TryAdd("weapon_sword", sword), Is.True);
            return loadout;
        }

        private static ItemInstance Piece(GearSlot slot, int level, params EfsunLine[] lines) =>
            slot == GearSlot.Weapon
                ? new ItemInstance(0, "weapon_sword", slot, UpgradeRarity.Rare, level, 0, lines)
                : new ItemInstance((int)slot, "item_" + slot, slot, UpgradeRarity.Gold, level, 0, lines);

        private static EfsunLine Line(GearStat stat, int tier) => new EfsunLine(stat, tier);

        // The power index as the run itself reads it: the weapon's damage, cadence and crit, the hero's health and armour.
        private static double RunPower(WeaponRuntime weapon, HeroStats hero)
        {
            double dps = weapon.Damage / BaseDamage * (BaseInterval / weapon.Interval) *
                         (1 + weapon.CritChance * (GearStats.CritMultiplierPercent / 100.0 - 1));
            double ehp = hero.MaxHealth / BaseHealth / (1 - hero.DamageReduction);
            return dps * ehp - 1;
        }

        [Test]
        public void AnEmptyLoadoutAddsNothingAtAll()
        {
            var cases = new[]
            {
                GearLoadout.None, new GearLoadout(new ItemInstance[0]), new GearLoadout(null),
                FoundryGear.Set("weapon_sword", 0)
            };
            foreach (GearLoadout gear in cases)
            {
                HeroStats hero = Sheet();
                WeaponRuntime sword = Sword();
                WeaponLoadout loadout = Holding(sword);
                int changes = 0;
                hero.Changed += () => changes++;
                sword.StatsChanged += () => changes++;

                Assert.That(gear.IsEmpty, Is.True);
                Assert.That(gear.Power, Is.Zero);
                Assert.That(GearStats.Apply(gear, hero, loadout, sword), Is.Zero);
                Assert.That(changes, Is.Zero, "no modifier was added, not even a zero one");
                Assert.That(sword.Damage, Is.EqualTo(BaseDamage));
                Assert.That(sword.Interval, Is.EqualTo(BaseInterval));
                Assert.That(sword.CritChance, Is.Zero);
                Assert.That(sword.Area, Is.EqualTo(1f));
                Assert.That(hero.MaxHealth, Is.EqualTo(BaseHealth));
                Assert.That(hero.Armor, Is.Zero);
                Assert.That(hero.MoveSpeed, Is.EqualTo(BaseSpeed));
                Assert.That(hero.Luck, Is.Zero);
            }
            Assert.That(GearStats.Apply(null, Sheet(), null, Sword()), Is.Zero);
        }

        [Test]
        public void AKnownLoadoutGivesKnownModifiers()
        {
            var gear = new GearLoadout(new[]
            {
                // +10 % damage, 0.8 points of crit, +1.5 % health.
                Piece(GearSlot.Weapon, 9, Line(GearStat.CritChance, 3), Line(GearStat.MaxHealth, 2)),
                // +5 % health, 1.2 points of armour, 4 luck.
                Piece(GearSlot.Armour, 5, Line(GearStat.Armour, 5), Line(GearStat.Luck, 4)),
                // +1.5 % attack speed, +1.5 % move speed, +1 % area.
                Piece(GearSlot.Amulet, 3, Line(GearStat.MoveSpeed, 2), Line(GearStat.Area, 1)),
                // 2.1 points of crit, +1 % damage.
                Piece(GearSlot.Ring, 7, Line(GearStat.Damage, 1))
            });
            HeroStats hero = Sheet();
            WeaponRuntime sword = Sword();

            Assert.That(GearStats.Apply(gear, hero, Holding(sword), sword), Is.EqualTo(8), "eight stats, eight modifiers");

            Assert.That(sword.Damage, Is.EqualTo(BaseDamage * 1.11f).Within(1e-4f), "+10 % and +1 %");
            Assert.That(sword.Interval, Is.EqualTo(BaseInterval / 1.015f).Within(1e-5f), "+1.5 % attack speed");
            Assert.That(sword.CritChance, Is.EqualTo(.029f).Within(1e-6f), "0.8 + 2.1 points");
            Assert.That(sword.Area, Is.EqualTo(1.01f).Within(1e-6f));
            Assert.That(hero.MaxHealth, Is.EqualTo(BaseHealth * 1.065f).Within(1e-4f), "+5 % and +1.5 %");
            Assert.That(hero.DamageReduction, Is.EqualTo(.012f).Within(1e-6f), "1.2 points: 1.2 % of every hit");
            Assert.That(hero.Armor, Is.EqualTo(HeroStats.ArmorSoftness * .012f / .988f).Within(1e-4f));
            Assert.That(hero.MoveSpeed, Is.EqualTo(BaseSpeed * 1.015f).Within(1e-5f));
            Assert.That(hero.Luck, Is.EqualTo(4f));
        }

        [Test]
        public void TheCapsBind()
        {
            EfsunLine[] perfect =
            {
                Line(GearStat.Damage, 5), Line(GearStat.AttackSpeed, 5), Line(GearStat.CritChance, 5), Line(GearStat.MaxHealth, 5),
                Line(GearStat.Armour, 5)
            };
            GearLoadout gear = FoundryGear.Set("weapon_sword", 9, perfect);
            HeroStats hero = Sheet();
            WeaponRuntime sword = Sword();
            GearStats.Apply(gear, hero, Holding(sword), sword);

            Assert.That(sword.Damage, Is.EqualTo(BaseDamage * 1.14f).Within(1e-4f), "damage caps at 14 % (19 % uncapped)");
            Assert.That(sword.Interval, Is.EqualTo(BaseInterval / 1.07f).Within(1e-5f), "attack speed at 7 %");
            Assert.That(sword.CritChance, Is.EqualTo(.04f).Within(1e-6f), "crit at 4 points");
            Assert.That(hero.MaxHealth, Is.EqualTo(BaseHealth * 1.14f).Within(1e-4f), "health at 14 %");
            Assert.That(hero.DamageReduction, Is.EqualTo(.04f).Within(1e-6f), "armour at 4 points");

            // Four luck lines at tier 5 are 20 luck; the cap is 10. Move speed's four at tier 5 are 12 %; the cap is 6.
            GearLoadout lucky = FoundryGear.Set("weapon_sword", 0, Line(GearStat.Luck, 5), Line(GearStat.MoveSpeed, 5));
            HeroStats sheet = Sheet();
            GearStats.Apply(lucky, sheet, null);
            Assert.That(sheet.Luck, Is.EqualTo(10f));
            Assert.That(sheet.MoveSpeed, Is.EqualTo(BaseSpeed * 1.06f).Within(1e-5f));
        }

        [Test]
        public void TheModelsPowerIndicesReadOffTheRun()
        {
            var sets = new (GearLoadout Gear, double Power, double Within)[]
            {
                (FoundryGear.Set("weapon_sword", 5), .15, .005),
                (FoundryGear.Set("weapon_sword", 7), .21, .005),
                (FoundryGear.Set("weapon_sword", 9), .31, .006),
                (FoundryGear.Set("weapon_sword", 9, FoundryGear.GoodLines), .42, .006),
                (FoundryGear.Set("weapon_sword", 9, Line(GearStat.Damage, 5), Line(GearStat.AttackSpeed, 5), Line(GearStat.CritChance, 5),
                    Line(GearStat.MaxHealth, 5), Line(GearStat.Armour, 5)), .51, .006)
            };
            foreach ((GearLoadout gear, double power, double within) in sets)
            {
                HeroStats hero = Sheet();
                WeaponRuntime sword = Sword();
                GearStats.Apply(gear, hero, Holding(sword), sword);
                double read = RunPower(sword, hero);
                Assert.That(read, Is.EqualTo(power).Within(within), "section 7.2's set, read off the run");
                Assert.That(read, Is.EqualTo(gear.Power / 10000.0).Within(.0005), "the run is what the index promises");
            }
        }

        [Test]
        public void TheStatsTheRunHasNoRuleForStayInert()
        {
            var inert = new[] { GearStat.EliteDamage, GearStat.BurnDamage, GearStat.PickupRadius, GearStat.AbilityCooldown, GearStat.Healing };
            foreach (GearStat stat in inert)
            {
                Assert.That(GearStats.RunStat(stat, out _, out _), Is.False, stat + " has no run rule yet");
                Assert.That(GearStats.RunAmount(stat, 600), Is.Zero);
            }
            var mapped = new[]
            {
                GearStat.Damage, GearStat.AttackSpeed, GearStat.CritChance, GearStat.MaxHealth, GearStat.Armour, GearStat.Area,
                GearStat.MoveSpeed, GearStat.Luck
            };
            foreach (GearStat stat in mapped)
                Assert.That(GearStats.RunStat(stat, out _, out _), Is.True, stat + " rides a run stat");
            Assert.That(inert.Length + mapped.Length, Is.EqualTo(GearStatIds.Count));

            // Every inert line at tier 5, on +0 pieces: a loadout with values, and not one modifier.
            var gear = new GearLoadout(new[]
            {
                Piece(GearSlot.Armour, 0, Line(GearStat.EliteDamage, 5), Line(GearStat.BurnDamage, 5)),
                Piece(GearSlot.Amulet, 0, Line(GearStat.PickupRadius, 5), Line(GearStat.AbilityCooldown, 5)),
                Piece(GearSlot.Ring, 0, Line(GearStat.Healing, 5))
            });
            HeroStats hero = Sheet();
            WeaponRuntime sword = Sword();
            int changes = 0;
            hero.Changed += () => changes++;
            sword.StatsChanged += () => changes++;
            Assert.That(gear.IsEmpty, Is.False);
            Assert.That(GearStats.Apply(gear, hero, Holding(sword), sword), Is.Zero);
            Assert.That(changes, Is.Zero);
            Assert.That(sword.Damage, Is.EqualTo(BaseDamage));
            Assert.That(hero.MaxHealth, Is.EqualTo(BaseHealth));
        }

        [Test]
        public void TheMappingRidesTheCardsPaths()
        {
            Assert.That(GearStats.RunStat(GearStat.Damage, out UpgradeStat stat, out ModifierOperation operation), Is.True);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.Damage, ModifierOperation.Percent)));
            GearStats.RunStat(GearStat.AttackSpeed, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.AttackSpeed, ModifierOperation.Percent)));
            GearStats.RunStat(GearStat.CritChance, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.CritChance, ModifierOperation.Flat)));
            GearStats.RunStat(GearStat.MaxHealth, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.MaxHealth, ModifierOperation.Percent)));
            GearStats.RunStat(GearStat.Armour, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.Armor, ModifierOperation.Flat)));
            GearStats.RunStat(GearStat.Area, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.Area, ModifierOperation.Percent)));
            GearStats.RunStat(GearStat.MoveSpeed, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.MoveSpeed, ModifierOperation.Percent)));
            GearStats.RunStat(GearStat.Luck, out stat, out operation);
            Assert.That((stat, operation), Is.EqualTo((UpgradeStat.Luck, ModifierOperation.Flat)));

            Assert.That(GearStats.RunAmount(GearStat.Damage, 1000), Is.EqualTo(.1f));
            Assert.That(GearStats.RunAmount(GearStat.CritChance, 300), Is.EqualTo(.03f));
            Assert.That(GearStats.RunAmount(GearStat.Luck, 500), Is.EqualTo(5f));
            Assert.That(GearStats.RunAmount(GearStat.Armour, 400), Is.EqualTo(100f * .04f / .96f).Within(1e-4f));
        }

        [Test]
        public void AWeaponPickedUpLaterWearsTheGearToo()
        {
            HeroStats hero = Sheet();
            WeaponRuntime sword = Sword();
            WeaponLoadout loadout = Holding(sword);
            GearStats.Apply(FoundryGear.Set("weapon_sword", 9), hero, loadout, sword);

            var daggers = new WeaponRuntime(4f, .4f, 1.2f);
            Assert.That(loadout.TryAdd("weapon_daggers", daggers), Is.True);
            Assert.That(sword.Damage, Is.EqualTo(BaseDamage * 1.1f).Within(1e-4f));
            Assert.That(daggers.Damage, Is.EqualTo(4f * 1.1f).Within(1e-4f), "the weapon's +9 is hero-wide (section 3.1)");
            Assert.That(daggers.Interval, Is.EqualTo(.4f / 1.05f).Within(1e-5f), "the amulet's too");
            Assert.That(daggers.CritChance, Is.EqualTo(.03f).Within(1e-6f), "and the ring's");
        }

        [Test]
        public void TheHallArenaGearsItsOneWeapon()
        {
            HeroStats hero = Sheet();
            WeaponRuntime sword = Sword();
            GearStats.Apply(FoundryGear.Set("weapon_sword", 9), hero, null, sword);
            Assert.That(sword.Damage, Is.EqualTo(BaseDamage * 1.1f).Within(1e-4f));
            Assert.That(hero.MaxHealth, Is.EqualTo(BaseHealth * 1.1f).Within(1e-4f));

            // With no weapon at all only the hero's own stats land.
            HeroStats alone = Sheet();
            Assert.That(GearStats.Apply(FoundryGear.Set("weapon_sword", 9), alone, null), Is.EqualTo(1), "health only");
            Assert.That(alone.MaxHealth, Is.EqualTo(BaseHealth * 1.1f).Within(1e-4f));
        }

        [Test]
        public void TheWeaponsGearIsTheCarriedWeaponsOwn()
        {
            var snapshot = new GearSnapshot
            {
                Items = new[] { new GearRecord { Uid = 1, ItemId = "item_cinder_ring", Slot = (int)GearSlot.Ring, Rarity = 3, Level = 9 } },
                Weapons = new[]
                {
                    new GearRecord { ItemId = "weapon_sword", Level = 3 },
                    new GearRecord { ItemId = "weapon_staff", Level = 9 }
                },
                EquippedUids = new[] { 0, 0, 1 },
                NextItemUid = 2
            };
            var profile = new PlayerProfile(0, null, null, 0, null, null, null, null, ProfileGear.Restore(snapshot));

            var sword = new GearLoadout(profile.Gear.EquippedPieces("weapon_sword"));
            var staff = new GearLoadout(profile.Gear.EquippedPieces("weapon_staff"));
            var daggers = new GearLoadout(profile.Gear.EquippedPieces("weapon_daggers"));
            Assert.That(sword.Total(GearStat.Damage), Is.EqualTo(300), "the Sword's +3, not the Staff's +9");
            Assert.That(staff.Total(GearStat.Damage), Is.EqualTo(1000));
            Assert.That(daggers.Total(GearStat.Damage), Is.Zero, "no gear yet for the Daggers");
            Assert.That(sword.Total(GearStat.CritChance), Is.EqualTo(300), "the ring is worn by every weapon");
            Assert.That(daggers.Total(GearStat.CritChance), Is.EqualTo(300));
            Assert.That(new GearLoadout(new PlayerProfile().Gear.EquippedPieces("weapon_sword")).IsEmpty, Is.True,
                "a new profile wears nothing");
        }

        [Test]
        public void TheSimulationsHeroIsUngearedByDefault()
        {
            Assert.That(DescentSimulation.Vanguard().Gear, Is.SameAs(GearLoadout.None));
            Assert.That(DescentSimulation.CinderArcanist().Gear, Is.SameAs(GearLoadout.None));

            DescentSimulation.FoundryResult plain = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 3);
            DescentSimulation.FoundryHero empty = DescentSimulation.Vanguard();
            empty.Gear = new GearLoadout(new List<ItemInstance>());
            DescentSimulation.FoundryResult same = DescentSimulation.RunFoundry(empty, 3);
            Assert.That(same.FloorSeconds, Is.EqualTo(plain.FloorSeconds));
            Assert.That(same.Outcome, Is.EqualTo(plain.Outcome));
            Assert.That(same.WaveKills, Is.EqualTo(plain.WaveKills));
            Assert.That(same.HordeKills, Is.EqualTo(plain.HordeKills));
            Assert.That(same.HeroHealth, Is.EqualTo(plain.HeroHealth));
            Assert.That(same.DealtByWeapons, Is.EqualTo(plain.DealtByWeapons));
            Assert.That(same.Picks, Is.EqualTo(plain.Picks));

            DescentSimulation.FoundryHero geared = FoundryGear.Cohorts[3].Wear(DescentSimulation.Vanguard());
            Assert.That(geared.Gear.Power, Is.EqualTo(4200).Within(60), "careful+9g wears section 7.2's +42 %");
            DescentSimulation.FoundryResult strong = DescentSimulation.RunFoundry(geared, 3);
            Assert.That(strong.DealtByWeapons, Is.Not.EqualTo(plain.DealtByWeapons), "the gear reached the simulated run");
        }

        [Test]
        public void TheGearedCohortsAreSection13s()
        {
            Assert.That(FoundryGear.Cohorts.Count, Is.EqualTo(6));
            Assert.That(FoundryGear.Named(null).Count, Is.EqualTo(6));
            Assert.That(FoundryGear.Named("careful+9g, clumsy+5")[1].Cohort, Is.SameAs(AutopilotCohort.Clumsy));
            Assert.Throws<System.ArgumentException>(() => FoundryGear.Named("careful+11"));
            var powers = new Dictionary<string, int>
            {
                ["careful+5"] = 1500, ["careful+7"] = 2100, ["careful+9"] = 3100, ["careful+9g"] = 4200, ["clumsy+5"] = 1500,
                ["auto-farmer+5"] = 1500
            };
            foreach (GearedCohort cohort in FoundryGear.Cohorts)
            {
                DescentSimulation.FoundryHero ilya = cohort.Wear(DescentSimulation.CinderArcanist());
                Assert.That(ilya.Gear.Pieces[0].ItemId, Is.EqualTo("weapon_staff"), "the hero's own weapon carries the weapon's gear");
                Assert.That(ilya.Gear.Power, Is.EqualTo(powers[cohort.Name]).Within(60), cohort.Name);
            }
        }
    }
}
