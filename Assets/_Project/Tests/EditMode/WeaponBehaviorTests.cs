using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class WeaponBehaviorTests
    {
        private static readonly DescentSimulation.Floor[] Descent = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };

        // The Forge Pulse contract, clause by clause (docs/27, S5a). Each of these is something the design agreed before
        // the code existed, and each one is something a later change could quietly break.
        [Test]
        public void NoCardMeansNoPulseAtAll()
        {
            WeaponRuntime sword = DescentSimulation.Sword().CreateRuntime();
            var hero = new HealthState(100f);
            var target = new HealthState(500f);
            var bystander = new HealthState(500f);
            for (int i = 0; i < 12; i++)
            {
                sword.Tick(10f);
                Assert.That(sword.TryAttack(target, hero, null, new IDamageable[] { bystander }), Is.True);
            }

            Assert.That(sword.PulsesFired, Is.Zero, "A weapon with no pulse card never pulses, whatever it is handed.");
            Assert.That(bystander.Current, Is.EqualTo(500f), "And nothing near the hero is touched by a strike.");
        }

        [Test]
        public void EveryFourthStrikePulsesAndThePulseIsNotAStrike()
        {
            WeaponRuntime sword = Pulsing(DescentSimulation.Sword(), 0.5f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var bystander = new HealthState(5000f);
            int pulses = 0;
            sword.Pulsed += () => pulses++;
            var caught = new IDamageable[] { target, bystander };

            var pulsedOn = new List<int>();
            for (int strike = 1; strike <= 12; strike++)
            {
                sword.Tick(10f);
                int before = sword.PulsesFired;
                Assert.That(sword.TryAttack(target, hero, null, caught), Is.True);
                if (sword.PulsesFired > before)
                    pulsedOn.Add(strike);
                Assert.That(sword.AttacksMade, Is.EqualTo(strike),
                    "A pulse is not a strike, so it never advances the count the rhythm reads.");
            }

            Assert.That(pulsedOn, Is.EqualTo(new[] { 4, 8, 12 }), "Every fourth strike, and only those.");
            Assert.That(pulses, Is.EqualTo(3), "The event fires once a pulse, not once an enemy.");
            // Three pulses of half the Sword 10 damage, on both the target and the bystander.
            Assert.That(5000f - bystander.Current, Is.EqualTo(15f).Within(1e-4f));
        }

        [Test]
        public void ACritCanTriggerAPulseAndThePulseNeverCrits()
        {
            // The Daggers crit every third strike, so their twelfth is both a crit and a pulse.
            WeaponRuntime daggers = Pulsing(DescentSimulation.Daggers(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var bystander = new HealthState(5000f);
            var landed = new List<float>();
            var crits = new List<bool>();
            bystander.Damaged += context => { landed.Add(context.Amount); crits.Add(context.IsCritical); };

            for (int strike = 1; strike <= 12; strike++)
            {
                daggers.Tick(10f);
                Assert.That(daggers.TryAttack(target, hero, null, new IDamageable[] { bystander }), Is.True);
            }

            Assert.That(daggers.PulsesFired, Is.EqualTo(3), "Strikes 4, 8 and 12 pulse; 12 is a crit as well.");
            Assert.That(landed.Count, Is.EqualTo(3), "The bystander is only ever touched by a pulse.");
            for (int i = 0; i < landed.Count; i++)
            {
                Assert.That(crits[i], Is.False, "A pulse never reports itself critical.");
                Assert.That(landed[i], Is.EqualTo(6f).Within(1e-4f),
                    "And it deals its share of the weapon damage, never of the crit damage.");
            }
        }

        [Test]
        public void AnAreaStrikeCountsOnceHoweverManyEnemiesItHits()
        {
            // The Staff splashes every neighbour. Four enemies a swing, twelve swings: the pulse must still land on the
            // fourth, eighth and twelfth swing rather than after every twelfth landing.
            WeaponRuntime staff = Pulsing(DescentSimulation.Staff(), 0.5f);
            var hero = new HealthState(100f);
            var target = new HealthState(50000f);
            var a = new HealthState(50000f);
            var b = new HealthState(50000f);
            var c = new HealthState(50000f);
            var splash = new IDamageable[] { target, a, b, c };

            for (int strike = 1; strike <= 12; strike++)
            {
                staff.Tick(10f);
                Assert.That(staff.TryAttack(target, hero, splash, new IDamageable[] { a }), Is.True);
                Assert.That(staff.AttacksMade, Is.EqualTo(strike), "The strike is the swing, not the landing.");
            }

            Assert.That(staff.PulsesFired, Is.EqualTo(3));
        }

        [Test]
        public void APulseChainsNothingAndCannotPulseAgain()
        {
            // An enemy that dies to a pulse and attacks back from its death callback must not be able to make the
            // weapon pulse a second time, and the pulse must not reach anything the caller did not hand it.
            WeaponRuntime sword = Pulsing(DescentSimulation.Sword(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var frail = new HealthState(1f);
            var offList = new HealthState(5000f);
            int reentrant = 0;
            frail.Died += () =>
            {
                reentrant++;
                // Re-entering while the pulse is still landing: refused, because cadence was consumed by the strike.
                sword.TryAttack(target, hero, null, new IDamageable[] { offList });
            };

            for (int strike = 1; strike <= 4; strike++)
            {
                sword.Tick(10f);
                Assert.That(sword.TryAttack(target, hero, null, new IDamageable[] { frail }), Is.True);
            }

            Assert.That(reentrant, Is.EqualTo(1), "The pulse killed it once.");
            Assert.That(sword.PulsesFired, Is.EqualTo(1), "And that kill started no second pulse.");
            Assert.That(offList.Current, Is.EqualTo(5000f), "A refused attack lands nothing.");
            Assert.That(sword.AttacksMade, Is.EqualTo(4));
        }

        [Test]
        public void RelicDamageIsNotAStrikeSoItCannotAdvanceTheRhythm()
        {
            // Relics and abilities damage through ApplyDamage and never through TryAttack, so the pulse cannot see
            // them. Pinned here because it holds by construction and a future convenience method could undo it.
            WeaponRuntime sword = Pulsing(DescentSimulation.Sword(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var bystander = new HealthState(5000f);
            var caught = new IDamageable[] { bystander };

            for (int strike = 1; strike <= 3; strike++)
            {
                sword.Tick(10f);
                Assert.That(sword.TryAttack(target, hero, null, caught), Is.True);
            }
            for (int i = 0; i < 20; i++)
                target.ApplyDamage(new DamageContext(1f, hero));

            Assert.That(sword.AttacksMade, Is.EqualTo(3), "Twenty relic hits are no strikes at all.");
            Assert.That(sword.PulsesFired, Is.Zero);
            sword.Tick(10f);
            Assert.That(sword.TryAttack(target, hero, null, caught), Is.True);
            Assert.That(sword.PulsesFired, Is.EqualTo(1), "The fourth strike is still the fourth strike.");
        }

        [Test]
        public void APulseWithNobodyNearStillCountsAndHurtsNothing()
        {
            WeaponRuntime sword = Pulsing(DescentSimulation.Sword(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            int pulses = 0;
            sword.Pulsed += () => pulses++;

            for (int strike = 1; strike <= 4; strike++)
            {
                sword.Tick(10f);
                Assert.That(sword.TryAttack(target, hero), Is.True);
            }

            Assert.That(sword.PulsesFired, Is.EqualTo(1));
            Assert.That(pulses, Is.EqualTo(1), "A caller with no list still pulsed; it just caught nobody.");
            Assert.That(PulseRule.DamageOf(10f, 0f), Is.Zero);
            Assert.That(PulseRule.Pulses(PulseRule.Every, 0f), Is.False, "No share, no pulse.");
            Assert.Throws<ArgumentOutOfRangeException>(() => PulseRule.Pulses(0, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => PulseRule.DamageOf(-1f, 1f));
        }

        // Splintering Crit, clause by clause (docs/27, S5c). The Daggers crit every third strike, so their pattern is the
        // one these read against.
        [Test]
        public void NoCardMeansNoSplinterAtAll()
        {
            WeaponRuntime daggers = DescentSimulation.Daggers().CreateRuntime();
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var bystander = new HealthState(5000f);
            for (int strike = 1; strike <= 9; strike++)
            {
                daggers.Tick(10f);
                Assert.That(daggers.TryAttack(target, hero, null, null, new IDamageable[] { bystander }), Is.True);
            }

            Assert.That(daggers.SplintersFired, Is.Zero);
            Assert.That(bystander.Current, Is.EqualTo(5000f));
        }

        [Test]
        public void OnlyACriticalStrikeSplintersAndItIsDealtOffTheCritDamage()
        {
            WeaponRuntime daggers = Splintering(DescentSimulation.Daggers(), 0.5f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var bystander = new HealthState(5000f);
            var landed = new List<float>();
            var crits = new List<bool>();
            bystander.Damaged += context => { landed.Add(context.Amount); crits.Add(context.IsCritical); };
            var caught = new IDamageable[] { target, bystander };

            var splinteredOn = new List<int>();
            for (int strike = 1; strike <= 9; strike++)
            {
                daggers.Tick(10f);
                int before = daggers.SplintersFired;
                Assert.That(daggers.TryAttack(target, hero, null, null, caught), Is.True);
                if (daggers.SplintersFired > before)
                    splinteredOn.Add(strike);
                Assert.That(daggers.AttacksMade, Is.EqualTo(strike), "A splinter is not a strike.");
            }

            Assert.That(splinteredOn, Is.EqualTo(new[] { 3, 6, 9 }), "Every third strike crits, and only those splinter.");
            Assert.That(landed.Count, Is.EqualTo(3), "The bystander is only ever touched by a splinter.");
            for (int i = 0; i < landed.Count; i++)
            {
                Assert.That(crits[i], Is.False, "A splinter never reports itself critical.");
                // The Daggers crit for double their 6, and half of that crit splinters.
                Assert.That(landed[i], Is.EqualTo(6f).Within(1e-4f), "Half of a 12-damage crit, not half of a 6.");
            }
        }

        [Test]
        public void ASplinterNeverStrikesWhatTheCritAlreadyHit()
        {
            WeaponRuntime daggers = Splintering(DescentSimulation.Daggers(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var caught = new IDamageable[] { target };

            for (int strike = 1; strike <= 3; strike++)
            {
                daggers.Tick(10f);
                Assert.That(daggers.TryAttack(target, hero, null, null, caught), Is.True);
            }

            // Two plain strikes of 6 and one crit of 12: 24, and not a splinter more.
            Assert.That(5000f - target.Current, Is.EqualTo(24f).Within(1e-4f));
            Assert.That(daggers.SplintersFired, Is.EqualTo(1), "It splintered; it just had nobody else to splinter into.");
        }

        [Test]
        public void ARolledCritSplintersJustAsARhythmOneDoes()
        {
            // The Sword authors no rhythm at all, so its only crits are rolled ones. A guaranteed chance makes every
            // strike critical and every strike splinter, which is the clause that says the gate is the crit and not the
            // pattern.
            WeaponRuntime sword = Splintering(DescentSimulation.Sword(), 0.5f);
            sword.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, 1f));
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var bystander = new HealthState(5000f);

            for (int strike = 1; strike <= 4; strike++)
            {
                sword.Tick(10f);
                Assert.That(sword.TryAttack(target, hero, null, null, new IDamageable[] { bystander }), Is.True);
            }

            Assert.That(sword.SplintersFired, Is.EqualTo(4), "Every strike crit, so every strike splintered.");
            // Four splinters of half a 20-damage crit.
            Assert.That(5000f - bystander.Current, Is.EqualTo(40f).Within(1e-4f));
        }

        [Test]
        public void AnAreaSplashAndASplinterBothLand()
        {
            // Deliberate overlap, and the thing docs/31 watches: the Staff already reached those enemies with its
            // splash, and a crit adds a splinter on top rather than replacing it.
            WeaponRuntime staff = Splintering(DescentSimulation.Staff(), 1f);
            staff.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, 1f));
            var hero = new HealthState(100f);
            var target = new HealthState(50000f);
            var neighbour = new HealthState(50000f);
            var caught = new IDamageable[] { target, neighbour };

            staff.Tick(10f);
            Assert.That(staff.TryAttack(target, hero, caught, null, caught), Is.True);

            // A crit of 22 on the target; the neighbour takes 75% of it as splash and all of it as a splinter.
            Assert.That(50000f - target.Current, Is.EqualTo(22f).Within(1e-3f));
            Assert.That(50000f - neighbour.Current, Is.EqualTo(22f * 0.75f + 22f).Within(1e-3f));
        }

        [Test]
        public void ASplinterChainsNothingAndCannotSplinterAgain()
        {
            WeaponRuntime daggers = Splintering(DescentSimulation.Daggers(), 1f);
            var hero = new HealthState(100f);
            var target = new HealthState(5000f);
            var frail = new HealthState(1f);
            var offList = new HealthState(5000f);
            int reentrant = 0;
            frail.Died += () =>
            {
                reentrant++;
                daggers.TryAttack(target, hero, null, null, new IDamageable[] { offList });
            };

            for (int strike = 1; strike <= 3; strike++)
            {
                daggers.Tick(10f);
                Assert.That(daggers.TryAttack(target, hero, null, null, new IDamageable[] { frail }), Is.True);
            }

            Assert.That(reentrant, Is.EqualTo(1));
            Assert.That(daggers.SplintersFired, Is.EqualTo(1), "That kill started no second splinter.");
            Assert.That(offList.Current, Is.EqualTo(5000f), "A refused attack lands nothing.");
            Assert.That(daggers.AttacksMade, Is.EqualTo(3));
            Assert.That(CritSplashRule.Splinters(false, 1f), Is.False, "A plain strike never splinters.");
            Assert.That(CritSplashRule.Splinters(true, 0f), Is.False, "Nor does a crit with no card behind it.");
            Assert.Throws<ArgumentOutOfRangeException>(() => CritSplashRule.DamageOf(-1f, 1f));
        }

        // A weapon carrying a splinter of the given share, granted the way a card grants it.
        private static WeaponRuntime Splintering(DescentSimulation.HeroWeapon weapon, float share)
        {
            WeaponRuntime runtime = weapon.CreateRuntime();
            runtime.AddModifier(UpgradeStat.CritSplash, new StatModifier(ModifierOperation.Flat, share));
            return runtime;
        }

        // A weapon carrying a pulse of the given share, granted the way a card grants it.
        private static WeaponRuntime Pulsing(DescentSimulation.HeroWeapon weapon, float share)
        {
            WeaponRuntime runtime = weapon.CreateRuntime();
            runtime.AddModifier(UpgradeStat.PulseDamage, new StatModifier(ModifierOperation.Flat, share));
            return runtime;
        }

        [Test]
        public void AreaStrikesEveryOtherLivingEnemyNearTheTargetForItsFraction()
        {
            WeaponRuntime staff = DescentSimulation.Staff().CreateRuntime();
            var hero = new HealthState(100f);
            var target = new HealthState(50f);
            var dead = new HealthState(15f);
            var left = new HealthState(15f);
            var right = new HealthState(40f);
            dead.ApplyDamage(new DamageContext(15f));
            var sources = new List<IDamageable>();
            right.Damaged += context => sources.Add(context.Source);

            Assert.That(staff.TryAttack(target, hero, new IDamageable[] { target, dead, left, right }), Is.True);

            Assert.That(target.Current, Is.EqualTo(39f));
            Assert.That(left.Current, Is.EqualTo(6.75f).Within(1e-4f), "75% of the 11 lands on every living neighbour.");
            Assert.That(right.Current, Is.EqualTo(31.75f).Within(1e-4f));
            Assert.That(sources, Is.EqualTo(new[] { hero }), "Splash hits carry the attacker too.");
        }

        [Test]
        public void EveryThirdDaggerStrikeCritsForDoubleDamage()
        {
            WeaponRuntime daggers = DescentSimulation.Daggers().CreateRuntime();
            var grunt = new HealthState(100f);
            var hits = new List<string>();
            grunt.Damaged += context => hits.Add(context.IsCritical ? $"crit {context.Amount:0}" : $"{context.Amount:0}");

            for (int i = 0; i < 6; i++)
            {
                daggers.Tick(10f);
                daggers.TryAttack(grunt);
            }

            Assert.That(hits, Is.EqualTo(new[] { "6", "6", "crit 12", "6", "6", "crit 12" }));
            Assert.That(daggers.AttacksMade, Is.EqualTo(6));
            Assert.That(grunt.Current, Is.EqualTo(52f));

            daggers.Tick(10f);
            Assert.That(daggers.TryAttack(null), Is.False);
            Assert.That(daggers.AttacksMade, Is.EqualTo(6), "An attack without a target does not advance the rhythm.");
        }

        [Test]
        public void ACriticalAttackMultipliesItsSplashToo()
        {
            var weapon = new WeaponRuntime(10f, 1f, 3f, 0f, new AttackPattern(WeaponBehavior.Area, 2f, 0.5f, 1, 3f));
            var target = new HealthState(100f);
            var near = new HealthState(100f);
            bool nearWasCritical = false;
            near.Damaged += context => nearWasCritical = context.IsCritical;

            weapon.TryAttack(target, null, new IDamageable[] { near });

            Assert.That(target.Current, Is.EqualTo(70f));
            Assert.That(near.Current, Is.EqualTo(85f), "Half of the 30 critical damage.");
            Assert.That(nearWasCritical, Is.True);
        }

        [Test]
        public void CritSettingsAreValidated()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => new AttackPattern(WeaponBehavior.DirectHit, critEvery: 0, critMultiplier: 2f),
                "Without crits the multiplier stays 0.");
            Assert.That(new AttackPattern(WeaponBehavior.DirectHit).IsCritical(3), Is.False);
            Assert.That(new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: 2f).IsCritical(6), Is.True);
            Assert.That(new AttackPattern(WeaponBehavior.DirectHit, critEvery: 3, critMultiplier: 2f).IsCritical(4), Is.False);
        }

        // Area is one factor on all three circles (docs/42, the Reach tome). It starts at 1, where every radius is the
        // authored one exactly - which is why no balance number moved when the circles began to be read through it.
        [Test]
        public void AnAreaModifierWidensTheStaffsSplash()
        {
            WeaponRuntime staff = DescentSimulation.Staff().CreateRuntime();
            Assert.That(staff.Area, Is.EqualTo(1f));
            Assert.That(staff.SplashRadius, Is.EqualTo(staff.Pattern.SplashRadius), "At Area 1 the authored splash, exactly.");
            Assert.That(staff.PulseRadius, Is.EqualTo(PulseRule.Radius));
            Assert.That(staff.CritSplashRadius, Is.EqualTo(CritSplashRule.Radius));

            // The target one unit out, and a bystander four beyond it: outside the Staff's 3.5, inside 3.5 x 1.2 = 4.2.
            var target = new HealthState(500f);
            var bystander = new HealthState(500f);
            var enemies = new[] { target, bystander };
            var motion = new PackMotion(DescentSimulation.BodySpacing, PackLayout.HalfWidth);
            motion.Add(target, 0f, 1f, 0f, 1f);
            motion.Add(bystander, 0f, 5f, 0f, 1f);
            Assert.That(DescentSimulation.Nearby(motion, enemies, 0, staff.SplashRadius), Is.Empty,
                "Four units is beyond the Staff's own splash.");

            staff.AddModifier(UpgradeStat.Area, new StatModifier(ModifierOperation.Percent, 0.2f));

            Assert.That(staff.SplashRadius, Is.EqualTo(4.2f).Within(1e-5f));
            Assert.That(staff.PulseRadius, Is.EqualTo(PulseRule.Radius * 1.2f).Within(1e-5f), "The pulse widens with it.");
            Assert.That(staff.CritSplashRadius, Is.EqualTo(CritSplashRule.Radius * 1.2f).Within(1e-5f), "And the splinter.");
            Assert.That(staff.Damage, Is.EqualTo(11f), "Area changes how far, never how hard.");
            Assert.That(staff.Range, Is.EqualTo(2.4f), "Nor how far away the Staff can strike from.");
            Assert.That(staff.Pattern.SplashRadius, Is.EqualTo(3.5f), "The authored pattern is never written to.");

            Assert.That(staff.TryAttack(target, null, DescentSimulation.Nearby(motion, enemies, 0, staff.SplashRadius)), Is.True);
            Assert.That(target.Current, Is.EqualTo(489f));
            Assert.That(bystander.Current, Is.EqualTo(491.75f).Within(1e-4f), "75% of the 11 now reaches four units out.");
        }

        // Two weapons in one loadout, as AttackController.Update and the simulation fire them (docs/42, PR 2). Each keeps
        // the cadence it has alone: a second weapon neither slows the first nor is slowed by it.
        [Test]
        public void TwoWeaponsEachKeepTheirOwnCadence()
        {
            var loadout = new WeaponLoadout(0);
            loadout.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            loadout.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime());
            int strikes = StrikeFor(loadout, 10f, out _);

            var sword = new WeaponLoadout(0);
            sword.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            StrikeFor(sword, 10f, out _);
            var daggers = new WeaponLoadout(0);
            daggers.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime());
            StrikeFor(daggers, 10f, out _);

            Assert.That(loadout.At(0).Runtime.AttacksMade, Is.EqualTo(sword.Primary.AttacksMade), "The Sword's 0.8 s.");
            Assert.That(loadout.At(1).Runtime.AttacksMade, Is.EqualTo(daggers.Primary.AttacksMade), "The Daggers' 0.5 s.");
            Assert.That(loadout.At(1).Runtime.AttacksMade, Is.GreaterThan(loadout.At(0).Runtime.AttacksMade));
            Assert.That(strikes, Is.EqualTo(sword.Primary.AttacksMade + daggers.Primary.AttacksMade),
                "Every strike either weapon made landed, none twice.");
        }

        [Test]
        public void WeaponsStrikeInSlotOrder()
        {
            var swordFirst = new WeaponLoadout(0);
            swordFirst.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            swordFirst.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime());
            StrikeFor(swordFirst, 0f, out List<float> swordThenDaggers);

            var daggersFirst = new WeaponLoadout(0);
            daggersFirst.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime());
            daggersFirst.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            StrikeFor(daggersFirst, 0f, out List<float> daggersThenSword);

            Assert.That(swordThenDaggers, Is.EqualTo(new[] { 10f, 6f }), "One frame, both ready: slot 0, then slot 1.");
            Assert.That(daggersThenSword, Is.EqualTo(new[] { 6f, 10f }), "The slot decides, not the weapon.");
        }

        [Test]
        public void ALaterSlotSkipsATargetAnEarlierSlotFelledThisFrame()
        {
            // The Daggers in slot 0 fell the near enemy with their first strike; the Sword in slot 1 must strike the
            // one behind it in the same frame rather than waste its swing on the fallen.
            var loadout = new WeaponLoadout(0);
            loadout.TryAdd("weapon_daggers", DescentSimulation.Daggers().CreateRuntime());
            loadout.TryAdd("weapon_sword", DescentSimulation.Sword().CreateRuntime());
            var near = new HealthState(6f);
            var behind = new HealthState(100f);
            var enemies = new[] { near, behind };
            var motion = new PackMotion(DescentSimulation.BodySpacing, PackLayout.HalfWidth);
            motion.Add(near, 0f, 1f, 0f, 1f);
            motion.Add(behind, 0f, 1.5f, 0f, 1f);
            int nearHits = 0;
            near.Damaged += _ => nearHits++;

            int struck = DescentSimulation.StrikeInSlotOrder(loadout, motion, enemies, 0f, 0f);

            Assert.That(struck, Is.EqualTo(2), "Both weapons struck this frame.");
            Assert.That(near.IsAlive, Is.False);
            Assert.That(nearHits, Is.EqualTo(1), "Only the Daggers touched the near enemy: nothing struck it once it fell.");
            Assert.That(behind.Current, Is.EqualTo(90f), "The Sword's 10 went to the enemy behind, and its cleave found no one.");
        }

        [Test]
        public void TwoWeaponsCritOnTheirOwnStreams()
        {
            const int Seed = 4242;
            var loadout = new WeaponLoadout(Seed);
            loadout.TryAdd("weapon_a", Critting(DescentSimulation.Sword()));
            loadout.TryAdd("weapon_b", Critting(DescentSimulation.Sword()));
            // What one weapon on this run's seed has always rolled, before loadouts existed.
            WeaponRuntime alone = Critting(DescentSimulation.Sword());
            alone.Seed = Seed;

            List<int> first = CritsOver(loadout.At(0).Runtime, 40);
            List<int> second = CritsOver(loadout.At(1).Runtime, 40);
            List<int> before = CritsOver(alone, 40);

            Assert.That(loadout.At(0).Runtime.Seed, Is.EqualTo(Seed), "Slot 0 keeps the run's seed.");
            Assert.That(first, Is.EqualTo(before), "So slot 0 crits on exactly the attacks one weapon always did.");
            Assert.That(loadout.At(1).Runtime.Seed, Is.Not.EqualTo(Seed));
            Assert.That(second, Is.Not.EqualTo(first), "Slot 1 rolls its own: the same chance, different attacks.");
            Assert.That(first.Count, Is.GreaterThan(0));
            Assert.That(second.Count, Is.GreaterThan(0));
        }

        // A run with a second weapon in slot 1 reaches both of the simulation's strike sites - the Descent's waves and a
        // swarm room - and the second weapon really fights there: its strikes are counted and the fights are shorter.
        [Test]
        public void ASecondWeaponFightsAtBothStrikeSites()
        {
            DescentSimulation.Result alone = DescentSimulation.Run(Descent, 0, true, heroWeapon: DescentSimulation.Sword(),
                pool: DescentSimulation.TwoCardPool(), groundDrops: false);
            DescentSimulation.Result paired = DescentSimulation.Run(Descent, 0, true, heroWeapon: DescentSimulation.Sword(),
                pool: DescentSimulation.TwoCardPool(), groundDrops: false, moreWeapons: new[] { DescentSimulation.Daggers() });
            Assert.That(alone.ClearedFloors, Is.GreaterThanOrEqualTo(1));
            Assert.That(paired.ClearedFloors, Is.GreaterThanOrEqualTo(alone.ClearedFloors));
            Assert.That(paired.FloorOneFightSeconds, Is.LessThan(alone.FloorOneFightSeconds),
                "The Daggers' strikes shorten the first floor's fights.");

            var swarm = new[] { DescentSimulation.SwarmProof };
            DescentSimulation.Result swarmAlone = DescentSimulation.Run(swarm, 0, true, heroWeapon: DescentSimulation.Sword(),
                pool: DescentSimulation.TwoCardPool(), swarmSeconds: 60f);
            DescentSimulation.Result swarmPaired = DescentSimulation.Run(swarm, 0, true, heroWeapon: DescentSimulation.Sword(),
                pool: DescentSimulation.TwoCardPool(), swarmSeconds: 60f, moreWeapons: new[] { DescentSimulation.Daggers() });
            Assert.That(swarmPaired.Kills, Is.GreaterThan(swarmAlone.Kills), "And the swarm falls faster with two.");

            Assert.Throws<ArgumentException>(() => DescentSimulation.Run(Descent, 0, true,
                heroWeapon: DescentSimulation.Sword(), moreWeapons: new[] { DescentSimulation.Sword() }),
                "A weapon is never held twice.");
        }

        // One sturdy enemy a unit in front of the hero, struck by the loadout every frame for the given seconds (one frame
        // at 0): the strikes landed, and what each one dealt in order.
        private static int StrikeFor(WeaponLoadout loadout, float seconds, out List<float> landed)
        {
            var enemy = new HealthState(1000000f);
            var enemies = new[] { enemy };
            var motion = new PackMotion(DescentSimulation.BodySpacing, PackLayout.HalfWidth);
            motion.Add(enemy, 0f, 1f, 0f, 1f);
            var amounts = new List<float>();
            enemy.Damaged += context => amounts.Add(context.Amount);
            landed = amounts;

            const float step = 1f / 60f;
            int frames = (int)(seconds * 60f);
            int struck = DescentSimulation.StrikeInSlotOrder(loadout, motion, enemies, 0f, 0f);
            for (int frame = 1; frame <= frames; frame++)
            {
                for (int slot = 0; slot < loadout.Count; slot++)
                    loadout.At(slot).Runtime.Tick(step);
                struck += DescentSimulation.StrikeInSlotOrder(loadout, motion, enemies, 0f, 0f);
            }
            return struck;
        }

        // A weapon with a rolled even chance to crit and no rhythm, granted the way Keen Edge grants it.
        private static WeaponRuntime Critting(DescentSimulation.HeroWeapon weapon)
        {
            WeaponRuntime runtime = weapon.CreateRuntime();
            runtime.AddModifier(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, 0.5f));
            return runtime;
        }

        // The attack numbers, from 1, on which the weapon crits over its next count attacks.
        private static List<int> CritsOver(WeaponRuntime weapon, int count)
        {
            var target = new HealthState(1000000f);
            bool critical = false;
            target.Damaged += context => critical = context.IsCritical;
            var crits = new List<int>();
            for (int i = 0; i < count; i++)
            {
                weapon.Tick(10f);
                Assert.That(weapon.TryAttack(target), Is.True);
                if (critical)
                    crits.Add(weapon.AttacksMade);
            }
            return crits;
        }

        [Test]
        public void StaffClearsMitePacksFastestAndDaggersFellTheWardensFastest()
        {
            DescentSimulation.Result sword = Burst(Descent, 0, true, heroWeapon: DescentSimulation.Sword());
            DescentSimulation.Result staff = Burst(Descent, 0, true, heroWeapon: DescentSimulation.Staff());
            DescentSimulation.Result daggers = Burst(Descent, 0, true, heroWeapon: DescentSimulation.Daggers());

            Assert.That(staff.MitePackFightSeconds, Is.LessThan(sword.MitePackFightSeconds));
            Assert.That(sword.MitePackFightSeconds, Is.LessThan(daggers.MitePackFightSeconds));
            Assert.That(daggers.BossFightSeconds, Is.LessThan(sword.BossFightSeconds));
            Assert.That(sword.BossFightSeconds, Is.LessThan(staff.BossFightSeconds));
        }

        // Unlocked weapons are sidegrades: each survives the mending descents near the Sword's health, none carries a greedy
        // Temper descent alone, and both relics still rescue damage-first Temper.
        [Test]
        public void EveryWeaponSurvivesTheMendingDescentButNeedsARelicToCarryGreed()
        {
            DescentSimulation.Result swordDamage = Burst(Descent, 0, true);
            DescentSimulation.Result swordSpeed = Burst(Descent, 1, true);
            var weapons = new[] { ("Sword", DescentSimulation.Sword()), ("Staff", DescentSimulation.Staff()), ("Daggers", DescentSimulation.Daggers()) };
            foreach (var (name, weapon) in weapons)
            {
                DescentSimulation.Result damageMend = Burst(Descent, 0, true, heroWeapon: weapon);
                DescentSimulation.Result speedMend = Burst(Descent, 1, true, heroWeapon: weapon);
                Assert.That(damageMend.ClearedFloors, Is.EqualTo(2), $"{name}: damage first with Mend");
                Assert.That(speedMend.ClearedFloors, Is.EqualTo(2), $"{name}: speed first with Mend");
                Assert.That(damageMend.HeroHealth, Is.EqualTo(swordDamage.HeroHealth).Within(15f), $"{name}: damage first health");
                Assert.That(speedMend.HeroHealth, Is.EqualTo(swordSpeed.HeroHealth).Within(15f), $"{name}: speed first health");

                Assert.That(Burst(Descent, 0, false, heroWeapon: weapon).ClearedFloors, Is.EqualTo(1), $"{name}: damage first with Temper");
                Assert.That(Burst(Descent, 1, false, heroWeapon: weapon).ClearedFloors, Is.EqualTo(1), $"{name}: speed first with Temper");
                Assert.That(Burst(Descent, 0, false, DescentSimulation.Counterweight(), weapon).ClearedFloors, Is.EqualTo(2),
                    $"{name}: damage first with Temper and Counterweight");
                Assert.That(Burst(Descent, 0, false, DescentSimulation.SecondWind(), weapon).ClearedFloors, Is.EqualTo(2),
                    $"{name}: damage first with Temper and Second Wind");
            }
        }

        // The balance baseline these claims were measured against: the Forge Burst fired whenever it is ready with an enemy
        // in reach, by a hero who stands in the middle - in the game before the Descent's rewards lay on the floor, which
        // the simulation keeps as a control (groundDrops: false). They pin the combat's arithmetic, which drops did not
        // touch. In the game as it is a standing hero picks nothing up, and one who walks for its drops opens the chests
        // this baseline never reached, which moves several of these claims; docs/37 records which.
        private static DescentSimulation.Result Burst(DescentSimulation.Floor[] floors, int cardSlot, bool mendOnFloorOne,
            RelicOption relicOption = null, DescentSimulation.HeroWeapon heroWeapon = null) =>
            DescentSimulation.Run(floors, cardSlot, mendOnFloorOne, relicOption, heroWeapon,
                ability: DescentSimulation.ForgeBurst(), pool: DescentSimulation.TwoCardPool(), groundDrops: false);
    }
}
