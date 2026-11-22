using System;
using System.Collections.Generic;
using System.Linq;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/63's weapon roster, the pure rules: a hero's own weapon is its alone, every other hero weapon is shared, the
    // Daggers stay shared until their hero joins the roster, and the two shared weapons built behind the lever - the Magma
    // Mortar and the Forge Bow - strike through the weapon code every weapon uses. The simulation plays the roster through
    // the same body (FoundryLevers.WeaponRoster): no hero meets another's own weapon, three slots at most, and with three
    // held no new weapon is offered. Off, nothing here touches a run.
    public sealed class WeaponRosterTests
    {
        private const string Sword = "weapon_sword";
        private const string Staff = "weapon_staff";
        private const string Daggers = "weapon_daggers";
        private static readonly string[] Shipped = { Sword, Staff };

        [Test]
        public void AHerosOwnWeaponIsOfferedToItAloneAndEveryOtherToAll()
        {
            Assert.That(WeaponRoster.Offers(Sword, Sword, Shipped), Is.True, "The Vanguard's own Sword, for its levels.");
            Assert.That(WeaponRoster.Offers(Sword, Staff, Shipped), Is.False, "Ilya's Staff is never the Vanguard's.");
            Assert.That(WeaponRoster.Offers(Staff, Sword, Shipped), Is.False, "The Vanguard's Sword is never Ilya's.");
            Assert.That(WeaponRoster.Offers(Staff, Staff, Shipped), Is.True);
            foreach (string shared in new[] { Daggers, WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId })
            {
                Assert.That(WeaponRoster.Offers(Sword, shared, Shipped), Is.True, shared);
                Assert.That(WeaponRoster.Offers(Staff, shared, Shipped), Is.True, shared);
            }
            Assert.Throws<ArgumentException>(() => WeaponRoster.Offers(null, Sword, Shipped));
            Assert.Throws<ArgumentException>(() => WeaponRoster.Offers(Sword, "", Shipped));
            Assert.Throws<ArgumentNullException>(() => WeaponRoster.Offers(Sword, Staff, null));
        }

        [Test]
        public void TheCatalogueKeepsItsOrderAndHoldsTheRunsOwnWeapon()
        {
            string[] all = { Sword, Staff, Daggers, WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId };
            Assert.That(WeaponRoster.Catalogue(Sword, all, Shipped), Is.EqualTo(new[] { 0, 2, 3, 4 }));
            Assert.That(WeaponRoster.Catalogue(Staff, all, Shipped), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(WeaponRoster.Catalogue(Sword, all, new string[0]), Is.EqualTo(new[] { 0, 1, 2, 3, 4 }),
                "No signature, every weapon for every hero: today's catalogue and the two shared ones.");
            Assert.Throws<ArgumentException>(() => WeaponRoster.Catalogue("weapon_unknown", all, Shipped),
                "A run's own weapon must be in its catalogue: its levels are cards.");
            Assert.Throws<ArgumentException>(() => WeaponRoster.Catalogue(Sword, new[] { Sword, Daggers, Daggers }, Shipped));
        }

        // docs/63 section 3.3: the Daggers are the Riftblade's own, but while no hero of the roster starts with them they are
        // shared; the day the Riftblade joins (its starting weapon among the signatures) they leave the others by the same
        // rule, and the Riftblade keeps them.
        [Test]
        public void TheDaggersStaySharedUntilTheRiftbladeJoinsTheRoster()
        {
            Assert.That(WeaponRoster.Offers(Sword, Daggers, Shipped), Is.True, "Today: shared.");
            string[] withRiftblade = { Sword, Staff, Daggers };
            Assert.That(WeaponRoster.Offers(Sword, Daggers, withRiftblade), Is.False, "The Riftblade's own, once it joins.");
            Assert.That(WeaponRoster.Offers(Staff, Daggers, withRiftblade), Is.False);
            Assert.That(WeaponRoster.Offers(Daggers, Daggers, withRiftblade), Is.True);
            Assert.That(WeaponRoster.Offers(Daggers, WeaponRoster.MagmaMortarId, withRiftblade), Is.True);
        }

        [Test]
        public void TheSimulationsRosterCatalogueIsTheRulesForEachHero()
        {
            Assert.That(DescentSimulation.RosterSignatures, Is.EqualTo(new[] { Sword, Staff }));
            Assert.That(Ids(DescentSimulation.RosterCatalogue(Sword)),
                Is.EqualTo(new[] { Sword, Daggers, WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId }), "The Vanguard.");
            Assert.That(Ids(DescentSimulation.RosterCatalogue(Staff)),
                Is.EqualTo(new[] { Staff, Daggers, WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId }), "Ilya.");
            Assert.That(Ids(DescentSimulation.RosterCatalogue(Daggers)),
                Is.EqualTo(new[] { Daggers, WeaponRoster.MagmaMortarId, WeaponRoster.ForgeBowId }),
                "The simulated Riftblade: the Daggers its own, and no other hero's.");
            Assert.That(Ids(DescentSimulation.CatalogueFor(Sword, new DescentSimulation.FoundryLevers())),
                Is.EqualTo(new[] { Sword, Staff, Daggers }), "Off, today's catalogue for every hero.");
        }

        private static string[] Ids(DescentSimulation.HeroWeapon[] weapons) => weapons.Select(w => w.Id).ToArray();

        // The roster played: on its lever, each hero's offers over a floor never show the other hero's own weapon, never
        // more than three weapons are held, and with three held no new weapon is offered - the full-slot rule on a catalogue
        // where a fourth exists.
        [Test]
        public void UnderTheRosterNoHeroMeetsAnothersOwnWeaponAndThreeSlotsCloseTheNewWeapons()
        {
            var foundry = new[] { (DescentSimulation.Vanguard(), Staff), (DescentSimulation.CinderArcanist(), Sword) };
            foreach ((DescentSimulation.FoundryHero hero, string other) in foundry)
            {
                int fullOffers = 0;
                var held = new HashSet<string>();
                for (int seed = 1; seed <= 6; seed++)
                {
                    DescentSimulation.FoundryLevers levers = DescentSimulation.RosterLevers(new DescentSimulation.FoundryLevers());
                    levers.LogsPicks = true;
                    DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(hero, seed, levers);
                    string where = $"{hero.Name} seed {seed}";
                    Assert.That(run.WeaponWork.Count, Is.InRange(1, WeaponLoadout.MaxSlots), where);
                    Assert.That(run.WeaponWork.Any(w => w.Id == other), Is.False, $"{where}: holds the other hero's own weapon");
                    foreach (DescentSimulation.FoundryWeaponWork work in run.WeaponWork)
                        held.Add(work.Id);
                    foreach (DescentSimulation.FoundryPick pick in run.PickLog)
                    {
                        foreach (UpgradeOption card in pick.Offer.Choices)
                        {
                            Assert.That(card.Id, Is.Not.EqualTo(other), $"{where}: offered the other hero's own weapon");
                            if (pick.WeaponsHeld == WeaponLoadout.MaxSlots)
                                Assert.That(card.Category, Is.Not.EqualTo(OfferCategory.NewWeapon), $"{where}: a fourth weapon");
                        }
                        if (pick.WeaponsHeld == WeaponLoadout.MaxSlots)
                            fullOffers++;
                    }
                }
                Assert.That(fullOffers, Is.GreaterThan(0), $"{hero.Name}: the slots filled and the offers went on");
                Assert.That(held.Count, Is.GreaterThan(WeaponLoadout.MaxSlots),
                    $"{hero.Name}: over six runs more than three weapons were held - the roster is a choice");
            }
        }

        // ---- The new weapons' math, through the weapon code every weapon uses ----

        private static WeaponRuntime Runtime(DescentSimulation.HeroWeapon weapon) => weapon.CreateCard().CreateRuntime();

        [Test]
        public void TheMortarsBlastLandsInFullOnEveryEnemyWithinItsRadiusOfTheTarget()
        {
            WeaponRuntime mortar = Runtime(DescentSimulation.MagmaMortar());
            Assert.That((mortar.Damage, mortar.Interval, mortar.Range, mortar.SplashRadius), Is.EqualTo((34f, 1.8f, 4f, 2.5f)));
            var target = new HealthState(500f);
            var near = new HealthState(500f);
            var nearer = new HealthState(500f);
            var hero = new HealthState(100f);
            Assert.That(mortar.TryAttack(target, hero, new IDamageable[] { nearer, near }), Is.True);
            Assert.That(target.Current, Is.EqualTo(466f));
            Assert.That((nearer.Current, near.Current), Is.EqualTo((466f, 466f)), "Fraction 1: the whole blast on each.");
            Assert.That(mortar.TryAttack(target, hero), Is.False, "Its cadence: 1.8 s.");
            mortar.Tick(1.7f);
            Assert.That(mortar.IsReady, Is.False);
            mortar.Tick(.2f);
            Assert.That(mortar.IsReady, Is.True);

            // Its area line (+15 %) and the Reach tome (+8 %) add in one bucket on the radius: 2.5 x 1.23.
            mortar.AddModifier(UpgradeStat.Area, DescentSimulation.MagmaMortarLines()[1].ModifierFor(UpgradeRarity.Common));
            mortar.AddModifier(UpgradeStat.Area, DescentSimulation.Reach().ModifierFor(UpgradeRarity.Common));
            Assert.That(mortar.SplashRadius, Is.EqualTo(2.5f * 1.23f).Within(1e-5f));
            // Its damage line is Flat, before Force's percent (docs/42): (34 + 6) x 1.08.
            mortar.AddModifier(UpgradeStat.Damage, DescentSimulation.MagmaMortarLines()[0].ModifierFor(UpgradeRarity.Common));
            mortar.AddModifier(UpgradeStat.Damage, DescentSimulation.Force().ModifierFor(UpgradeRarity.Common));
            Assert.That(mortar.Damage, Is.EqualTo(40f * 1.08f).Within(1e-4f));
        }

        [Test]
        public void TheBowCritsEveryFourthShotAndReachesFurthest()
        {
            WeaponRuntime bow = Runtime(DescentSimulation.ForgeBow());
            Assert.That((bow.Damage, bow.Interval, bow.Range), Is.EqualTo((25f, 1.2f, 5f)));
            foreach (DescentSimulation.HeroWeapon other in DescentSimulation.RosterWeapons())
                if (other.Id != WeaponRoster.ForgeBowId)
                    Assert.That(bow.Range, Is.GreaterThan(other.Range), other.Id);
            var target = new HealthState(10000f);
            var bystander = new HealthState(10000f);
            var landed = new List<float>();
            for (int shot = 1; shot <= 8; shot++)
            {
                float before = target.Current;
                Assert.That(bow.TryAttack(target, null, new IDamageable[] { bystander }), Is.True, $"shot {shot}");
                landed.Add(before - target.Current);
                bow.Tick(bow.Interval);
            }
            Assert.That(landed, Is.EqualTo(new[] { 25f, 25f, 25f, 50f, 25f, 25f, 25f, 50f }), "A crit on every fourth, x2.");
            Assert.That(bystander.Current, Is.EqualTo(10000f), "A direct hit: nothing beside its target.");

            // Its crit line rolls between the rhythm's crits (the run's seed and the shot's number, as every weapon's).
            bow.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, 1f));
            float hp = target.Current;
            bow.TryAttack(target);
            Assert.That(hp - target.Current, Is.EqualTo(50f), "A sure roll doubles the shot.");
            // Its reach line lengthens its reach, flat.
            bow.AddModifier(UpgradeStat.Range, DescentSimulation.ForgeBowLines()[2].ModifierFor(UpgradeRarity.Common));
            Assert.That(bow.Range, Is.EqualTo(5.5f).Within(1e-5f));
        }
    }
}
