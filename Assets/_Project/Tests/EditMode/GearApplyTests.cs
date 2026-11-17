using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Economy;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The gear on a run (docs/54 phases 5 and 5b): GearStats.Apply, the one body CombatSetup and the foundry simulation both
    // call as a run begins - an empty loadout touches nothing at all; a known loadout gives known numbers; the caps bind;
    // damage, attack speed, area, health and move speed are a factor of their own whatever the tomes hold (the lead,
    // 2026-11-17), so section 7.2's power indices read back off the run's own weapon and hero; each of the five rule lines
    // reaches its rule (the elites' and bosses' shares, the burns' ticks, the drops' reach, the ability's cooldown, every
    // heal); a weapon picked up later wears the gear; the weapon's gear is the carried weapon's own. Also in CombatChecks.
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

        // A loadout of one line on a +0 ring (no main stat), or two on a ring and an amulet for a cap.
        private static GearLoadout Only(GearStat stat, int tier, bool twice = false) =>
            new GearLoadout(twice
                ? new[] { Piece(GearSlot.Ring, 0, Line(stat, tier)), Piece(GearSlot.Amulet, 0, Line(stat, tier)) }
                : new[] { Piece(GearSlot.Ring, 0, Line(stat, tier)) });

        // The power index as the run itself reads it: the weapon's damage, cadence and crit, the hero's health and armour.
        private static double RunPower(WeaponRuntime weapon, HeroStats hero, float baseDamage = BaseDamage, float baseInterval = BaseInterval,
            float baseHealth = BaseHealth)
        {
            double dps = weapon.Damage / baseDamage * (baseInterval / weapon.Interval) *
                         (1 + weapon.CritChance * (GearStats.CritMultiplierPercent / 100.0 - 1));
            double ehp = hero.MaxHealth / baseHealth / (1 - hero.DamageReduction);
            return dps * ehp - 1;
        }

        private static float Lost(HealthState state) => state.Maximum - state.Current;

        [Test]
        public void AnEmptyLoadoutTouchesNothingAtAll()
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
                var health = new HealthState(BaseHealth);
                var ability = new AbilityRuntime(30f, 2f, 8f);
                var drops = new GroundDrops(new RunState(10));
                int changes = 0;
                hero.Changed += () => changes++;
                sword.StatsChanged += () => changes++;
                health.Changed += () => changes++;

                Assert.That(gear.IsEmpty, Is.True);
                Assert.That(gear.Power, Is.Zero);
                Assert.That(GearStats.Apply(gear, hero, loadout, sword, health, ability, drops), Is.Zero);
                Assert.That(changes, Is.Zero, "no modifier was added, not even a zero one");
                Assert.That(sword.Damage, Is.EqualTo(BaseDamage));
                Assert.That(sword.Interval, Is.EqualTo(BaseInterval));
                Assert.That(sword.CritChance, Is.Zero);
                Assert.That(sword.Area, Is.EqualTo(1f));
                Assert.That(sword.GateBossShare, Is.EqualTo(WardenRule.SwordShare));
                Assert.That(sword.EliteShare, Is.EqualTo(1f));
                Assert.That(loadout.TargetShareFactor, Is.EqualTo(1f));
                Assert.That(hero.MaxHealth, Is.EqualTo(BaseHealth));
                Assert.That(hero.Armor, Is.Zero);
                Assert.That(hero.MoveSpeed, Is.EqualTo(BaseSpeed));
                Assert.That(hero.Luck, Is.Zero);
                Assert.That(health.HealingReceived, Is.EqualTo(1f));
                Assert.That(ability.Cooldown, Is.EqualTo(8f));
                Assert.That(drops.PickupRadius, Is.EqualTo(DropRule.PickupRadius));
                Assert.That(GearStats.BurnFactor(gear), Is.EqualTo(1f));
            }
            Assert.That(GearStats.Apply(null, Sheet(), null, Sword()), Is.Zero);
            Assert.That(GearStats.BurnFactor(null), Is.EqualTo(1f));
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

            // The rule lines' caps: two lines at tier 5 each, past every cap but pickup radius's.
            Assert.That(GearStats.Factor(Only(GearStat.AbilityCooldown, 5, true), GearStat.AbilityCooldown), Is.EqualTo(.9f).Within(1e-6f),
                "ability cooldown: 12 % of lines, 10 % at the cap (docs/54)");
            Assert.That(GearStats.Factor(Only(GearStat.EliteDamage, 5, true), GearStat.EliteDamage), Is.EqualTo(1.12f).Within(1e-6f));
            Assert.That(GearStats.Factor(Only(GearStat.BurnDamage, 5, true), GearStat.BurnDamage), Is.EqualTo(1.15f).Within(1e-6f));
            Assert.That(GearStats.Factor(Only(GearStat.Healing, 5, true), GearStat.Healing), Is.EqualTo(1.15f).Within(1e-6f));
            Assert.That(GearStats.Factor(Only(GearStat.PickupRadius, 5, true), GearStat.PickupRadius), Is.EqualTo(1.24f).Within(1e-6f),
                "two pickup lines at tier 5 are 24 %, under the cap of 25");
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

        // The lead's decision of 2026-11-17: the gear's damage, attack speed, area, health and move speed are a factor of
        // their own on the final stat, never summed with the cards' and tomes' percents - the item's "+10 %" is 10 % more
        // on a fresh run and on a run stacked with tomes, before or after the tomes came.
        [Test]
        public void TheGearIsAFactorOfItsOwnWhateverTheTomesHold()
        {
            GearLoadout plus9 = FoundryGear.Set("weapon_sword", 9);
            var force = new StatModifier(ModifierOperation.Percent, .48f);
            var edge = new StatModifier(ModifierOperation.Flat, 5f);
            var tempo = new StatModifier(ModifierOperation.Percent, .3f);
            var vitality = new StatModifier(ModifierOperation.Flat, 24f);
            var swift = new StatModifier(ModifierOperation.Percent, .2f);

            // The build alone, the build with the gear applied first (as the run does), and with the gear after it.
            WeaponRuntime plainSword = Sword();
            HeroStats plainHero = Sheet();
            Build(Holding(plainSword), plainHero);

            WeaponRuntime first = Sword();
            HeroStats firstHero = Sheet();
            WeaponLoadout firstLoadout = Holding(first);
            GearStats.Apply(plus9, firstHero, firstLoadout, first);
            Build(firstLoadout, firstHero);

            WeaponRuntime after = Sword();
            HeroStats afterHero = Sheet();
            WeaponLoadout afterLoadout = Holding(after);
            Build(afterLoadout, afterHero);
            GearStats.Apply(plus9, afterHero, afterLoadout, after);

            Assert.That(plainSword.Damage, Is.EqualTo((BaseDamage + 5f) * 1.48f).Within(1e-4f));
            foreach ((WeaponRuntime sword, HeroStats hero) in new[] { (first, firstHero), (after, afterHero) })
            {
                Assert.That(sword.Damage / plainSword.Damage, Is.EqualTo(1.1f).Within(1e-5f), "+10 % on top of +48 % from tomes");
                Assert.That(plainSword.Interval / sword.Interval, Is.EqualTo(1.05f).Within(1e-5f), "+5 % attack speed over the tomes' +30 %");
                Assert.That(hero.MaxHealth / plainHero.MaxHealth, Is.EqualTo(1.1f).Within(1e-5f), "+10 % of the base and the flat cards");
                Assert.That(hero.MoveSpeed / plainHero.MoveSpeed, Is.EqualTo(1f).Within(1e-6f), "the set carries no move speed");
                Assert.That(sword.CritChance - plainSword.CritChance, Is.EqualTo(.03f).Within(1e-6f), "crit adds its points to the build's");
            }

            void Build(WeaponLoadout loadout, HeroStats hero)
            {
                loadout.AddGlobal(UpgradeStat.Damage, edge);
                loadout.AddGlobal(UpgradeStat.Damage, force);
                loadout.AddGlobal(UpgradeStat.AttackSpeed, tempo);
                loadout.AddGlobal(UpgradeStat.CritChance, new StatModifier(ModifierOperation.Flat, .08f));
                hero.AddModifier(UpgradeStat.MaxHealth, vitality);
                hero.AddModifier(UpgradeStat.MoveSpeed, swift);
            }
        }

        [Test]
        public void TheMappingRidesTheSheetsPaths()
        {
            var expected = new Dictionary<GearStat, (UpgradeStat, ModifierOperation)>
            {
                [GearStat.Damage] = (UpgradeStat.Damage, ModifierOperation.Multiply),
                [GearStat.AttackSpeed] = (UpgradeStat.AttackSpeed, ModifierOperation.Multiply),
                [GearStat.Area] = (UpgradeStat.Area, ModifierOperation.Multiply),
                [GearStat.MaxHealth] = (UpgradeStat.MaxHealth, ModifierOperation.Multiply),
                [GearStat.MoveSpeed] = (UpgradeStat.MoveSpeed, ModifierOperation.Multiply),
                [GearStat.CritChance] = (UpgradeStat.CritChance, ModifierOperation.Flat),
                [GearStat.Armour] = (UpgradeStat.Armor, ModifierOperation.Flat),
                [GearStat.Luck] = (UpgradeStat.Luck, ModifierOperation.Flat)
            };
            var rules = new[] { GearStat.EliteDamage, GearStat.BurnDamage, GearStat.PickupRadius, GearStat.AbilityCooldown, GearStat.Healing };
            foreach (KeyValuePair<GearStat, (UpgradeStat, ModifierOperation)> pair in expected)
            {
                Assert.That(GearStats.RunStat(pair.Key, out UpgradeStat stat, out ModifierOperation operation), Is.True, pair.Key.ToString());
                Assert.That((stat, operation), Is.EqualTo(pair.Value), pair.Key.ToString());
            }
            foreach (GearStat stat in rules)
                Assert.That(GearStats.RunStat(stat, out _, out _), Is.False, stat + " rides a rule of its own, not the sheet");
            Assert.That(expected.Count + rules.Length, Is.EqualTo(GearStatIds.Count), "every stat reaches the run");

            Assert.That(GearStats.RunAmount(GearStat.Damage, 1000), Is.EqualTo(.1f));
            Assert.That(GearStats.RunAmount(GearStat.CritChance, 300), Is.EqualTo(.03f));
            Assert.That(GearStats.RunAmount(GearStat.Luck, 500), Is.EqualTo(5f));
            Assert.That(GearStats.RunAmount(GearStat.Armour, 400), Is.EqualTo(100f * .04f / .96f).Within(1e-4f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => GearStats.Factor(GearLoadout.None, GearStat.CritChance),
                "crit adds points, it has no factor");
        }

        // Damage vs elites and bosses rides the Daggers' path: every weapon's elite and gate-boss shares, the held ones and
        // every one picked up later, times the line's factor; every landing on an elite or the gate boss takes it.
        [Test]
        public void EliteDamageMultipliesTheElitesAndTheBossesShares()
        {
            GearLoadout gear = Only(GearStat.EliteDamage, 5);
            HeroStats hero = Sheet();
            WeaponRuntime sword = Sword();
            WeaponLoadout loadout = Holding(sword);
            Assert.That(GearStats.Apply(gear, hero, loadout, sword), Is.EqualTo(1));
            Assert.That(loadout.TargetShareFactor, Is.EqualTo(1.06f).Within(1e-6f));
            Assert.That(sword.GateBossShare, Is.EqualTo(WardenRule.SwordShare * 1.06f).Within(1e-5f), "the Sword's 1.5 on the gate boss, +6 %");
            Assert.That(sword.EliteShare, Is.EqualTo(1.06f).Within(1e-6f));
            var daggers = new WeaponRuntime(4f, .4f, 1.2f);
            Assert.That(loadout.TryAdd(EliteRule.DaggersId, daggers), Is.True);
            Assert.That(daggers.EliteShare, Is.EqualTo(EliteRule.DaggersShare * 1.06f).Within(1e-5f), "a weapon picked up later too");
            Assert.That(daggers.GateBossShare, Is.EqualTo(1.06f).Within(1e-6f));
            Assert.That(sword.Damage, Is.EqualTo(BaseDamage), "and nothing else");

            var captain = new HealthState(10000f);
            var grunt = new HealthState(10000f);
            var warden = new HealthState(10000f);
            IDamageable[] elites = { captain };
            Assert.That(sword.TryAttack(captain, null, null, null, null, null, warden, elites), Is.True);
            Assert.That(Lost(captain), Is.EqualTo(BaseDamage * 1.06f).Within(1e-3f), "an elite takes 6 % more");
            sword.Tick(1f);
            Assert.That(sword.TryAttack(warden, null, null, null, null, null, warden, elites), Is.True);
            Assert.That(Lost(warden), Is.EqualTo(BaseDamage * WardenRule.SwordShare * 1.06f).Within(1e-3f), "the gate boss too");
            sword.Tick(1f);
            Assert.That(sword.TryAttack(grunt, null, null, null, null, null, warden, elites), Is.True);
            Assert.That(Lost(grunt), Is.EqualTo(BaseDamage).Within(1e-4f), "anything else its own");

            // The hall arena keeps no loadout: its one weapon's shares.
            WeaponRuntime hall = Sword();
            GearStats.Apply(gear, Sheet(), null, hall);
            Assert.That(hall.EliteShare, Is.EqualTo(1.06f).Within(1e-6f));
            Assert.That(hall.GateBossShare, Is.EqualTo(1.06f).Within(1e-6f));
        }

        // Burn damage scales every tick of the hero's burns: the factor goes with the burn as it is lit (Ember Memory, the
        // scene's and the simulation's) and on to a neighbour it spreads to. Burns exist for Ilya alone (Ember Memory); for
        // the Vanguard the line has nothing to scale, so a Vanguard run with only burn lines is its ungeared run.
        [Test]
        public void BurnDamageScalesEveryTick()
        {
            GearLoadout gear = Only(GearStat.BurnDamage, 5);
            Assert.That(GearStats.BurnFactor(gear), Is.EqualTo(1.09f).Within(1e-6f));
            Assert.That(GearStats.Apply(gear, Sheet(), null, Sword()), Is.Zero, "nothing to touch at the start: the burns are lit later");

            var plain = new BurnState();
            var geared = new BurnState();
            plain.Apply(3, false);
            geared.Apply(3, false, GearStats.BurnFactor(gear));
            float plainTick = plain.Tick(BurnRule.TickSeconds);
            float gearedTick = geared.Tick(BurnRule.TickSeconds);
            Assert.That(plainTick, Is.EqualTo(BurnRule.TickDamage(3)), "an ungeared burn ticks as authored, to the bit");
            Assert.That(gearedTick, Is.EqualTo(BurnRule.TickDamage(3) * 1.09f).Within(1e-5f));
            Assert.That(geared.DamageScale, Is.EqualTo(1.09f).Within(1e-6f));

            DescentSimulation.FoundryResult vanguard = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 3);
            DescentSimulation.FoundryHero burner = DescentSimulation.Vanguard();
            burner.Gear = Only(GearStat.BurnDamage, 5, true);
            DescentSimulation.FoundryResult same = DescentSimulation.RunFoundry(burner, 3);
            Assert.That(same.DealtByBurn, Is.Zero);
            Assert.That((same.FloorSeconds, same.DealtByWeapons, same.HeroHealth), Is.EqualTo((vanguard.FloorSeconds, vanguard.DealtByWeapons,
                vanguard.HeroHealth)), "the Vanguard has no burn to scale");

            DescentSimulation.FoundryResult ilya = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), 3);
            DescentSimulation.FoundryHero ember = DescentSimulation.CinderArcanist();
            ember.Gear = Only(GearStat.BurnDamage, 5, true);
            DescentSimulation.FoundryResult hotter = DescentSimulation.RunFoundry(ember, 3);
            Assert.That(ilya.DealtByBurn, Is.GreaterThan(0f));
            Assert.That(hotter.DealtByBurn, Is.Not.EqualTo(ilya.DealtByBurn), "Ilya's burns reach the simulated run");
        }

        // Pickup radius widens GroundDrops' reach, which the scene's EncounterController and the simulation both pick up
        // through (CollectNear): the simulation models the pickup by radius, so the line reaches it as it reaches the scene.
        [Test]
        public void PickupRadiusWidensTheReach()
        {
            GearLoadout gear = Only(GearStat.PickupRadius, 5);
            var plain = new GroundDrops(new RunState(1000));
            var wide = new GroundDrops(new RunState(1000));
            Assert.That(GearStats.Apply(gear, Sheet(), null, null, null, null, wide), Is.EqualTo(1));
            Assert.That(wide.PickupRadius, Is.EqualTo(DropRule.PickupRadius * 1.12f).Within(1e-6f));
            foreach (GroundDrops drops in new[] { plain, wide })
                drops.Add(1.1f, 0f, 1, 1);
            Assert.That(plain.CollectNear(0f, 0f), Is.Zero, "1.1 units off: out of the authored reach");
            Assert.That(wide.CollectNear(0f, 0f), Is.EqualTo(1), "and inside the gear's 1.12");

            DescentSimulation.FoundryResult vanguard = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 3);
            DescentSimulation.FoundryHero reach = DescentSimulation.Vanguard();
            reach.Gear = Only(GearStat.PickupRadius, 5, true);
            DescentSimulation.FoundryResult wider = DescentSimulation.RunFoundry(reach, 3);
            Assert.That((wider.FloorSeconds, wider.DropsCollected), Is.Not.EqualTo((vanguard.FloorSeconds, vanguard.DropsCollected)),
                "the simulated hero picks up by the wider reach");
        }

        // Ability cooldown shortens the ability's cooldown once, as the run begins, at most by its 10 % cap.
        [Test]
        public void AbilityCooldownShortensTheAbility()
        {
            var ability = new AbilityRuntime(30f, 2f, 8f);
            Assert.That(GearStats.Apply(Only(GearStat.AbilityCooldown, 5), Sheet(), null, null, null, ability), Is.EqualTo(1));
            Assert.That(ability.Cooldown, Is.EqualTo(8f * .94f).Within(1e-5f), "6 % shorter");
            Assert.That(ability.TryUse(null, new IDamageable[0]), Is.True);
            Assert.That(ability.Remaining, Is.EqualTo(8f * .94f).Within(1e-5f), "every use waits the shorter cooldown");

            var capped = new AbilityRuntime(30f, 2f, 8f);
            GearStats.Apply(Only(GearStat.AbilityCooldown, 5, true), Sheet(), null, null, null, capped);
            Assert.That(capped.Cooldown, Is.EqualTo(8f * .9f).Within(1e-5f), "10 % at the cap");
            Assert.Throws<System.ArgumentOutOfRangeException>(() => capped.ShortenCooldown(1.1f));

            DescentSimulation.FoundryResult vanguard = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 3);
            DescentSimulation.FoundryHero quick = DescentSimulation.Vanguard();
            quick.Gear = Only(GearStat.AbilityCooldown, 5, true);
            DescentSimulation.FoundryResult quicker = DescentSimulation.RunFoundry(quick, 3);
            Assert.That((quicker.AbilityUses, quicker.FloorSeconds), Is.Not.EqualTo((vanguard.AbilityUses, vanguard.FloorSeconds)),
                "the simulated ability cools down faster");
        }

        // Healing received scales every heal of the hero's HealthState - the chest's mend, the forge's heal, Second Wind, the
        // simulation's gate-call heal - in one place, HealthState.Heal; a raised maximum is not a heal.
        [Test]
        public void HealingScalesEveryHeal()
        {
            var health = new HealthState(BaseHealth);
            Assert.That(GearStats.Apply(Only(GearStat.Healing, 5, true), Sheet(), null, null, health), Is.EqualTo(1));
            Assert.That(health.HealingReceived, Is.EqualTo(1.15f).Within(1e-6f), "two lines at tier 5 are 18 %; the cap is 15");
            health.ApplyDamage(new DamageContext(60f));
            health.Heal(20f);
            Assert.That(health.Current, Is.EqualTo(40f + 20f * 1.15f).Within(1e-4f));
            health.Heal(1000f);
            Assert.That(health.Current, Is.EqualTo(BaseHealth), "never past the maximum");

            // Second Wind heals through the same place.
            var relic = new RelicRuntime(new RelicOption("relic_second_wind", "Second Wind", "{0}", RelicEffect.SecondWind, .3f, .4f, 1));
            health.ApplyDamage(new DamageContext(70f));
            Assert.That(relic.OnHeroDamaged(health, null, 0f), Is.True);
            Assert.That(health.Current, Is.EqualTo(30f + BaseHealth * .3f * 1.15f).Within(1e-4f));

            // A raised maximum arrives filled, but it is not a heal: it is not scaled.
            var grown = new HealthState(BaseHealth);
            grown.SetHealingReceived(1.15f);
            grown.SetMaximum(110f);
            Assert.That(grown.Current, Is.EqualTo(110f));

            var plain = new HealthState(BaseHealth);
            plain.ApplyDamage(new DamageContext(60f));
            plain.Heal(20f);
            Assert.That(plain.Current, Is.EqualTo(60f), "an ungeared heal, to the bit");
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
            Assert.That(FoundryGear.Cohorts.Count, Is.EqualTo(8));
            Assert.That(FoundryGear.Named(null).Count, Is.EqualTo(8));
            Assert.That(FoundryGear.Named("careful+9g, clumsy+5")[1].Cohort, Is.SameAs(AutopilotCohort.Clumsy));
            Assert.Throws<System.ArgumentException>(() => FoundryGear.Named("careful+11"));
            var powers = new Dictionary<string, int>
            {
                ["careful+5"] = 1500, ["careful+7"] = 2100, ["careful+9"] = 3100, ["careful+9g"] = 4200, ["clumsy+5"] = 1500,
                ["auto-farmer+5"] = 1500, ["auto-farmer+9"] = 3100, ["auto-farmer+9g"] = 4200
            };
            foreach (GearedCohort cohort in FoundryGear.Cohorts)
            {
                DescentSimulation.FoundryHero ilya = cohort.Wear(DescentSimulation.CinderArcanist());
                Assert.That(ilya.Gear.Pieces[0].ItemId, Is.EqualTo("weapon_staff"), "the hero's own weapon carries the weapon's gear");
                Assert.That(ilya.Gear.Power, Is.EqualTo(powers[cohort.Name]).Within(60), cohort.Name);
            }
            Assert.That(FoundryGear.Named("auto-farmer+9g")[0].Cohort, Is.SameAs(AutopilotCohort.AutoFarmer));
        }
    }
}
