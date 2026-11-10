using System;
using System.Linq;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Who the development autopilot plays as (AutopilotCohort): the cohorts and the habits each drops, the line of
    // development/autopilot.txt that names one, and each habit's two ways - the careful pilot's and the one before
    // 2026-11-04 - through the one body the scene's autopilot and the foundry simulation share. Places are given in the
    // foundry's design plane, as in FootworkRuleTests: the court's east wall stands at u = 14, the pit's hole spans
    // u -16..-4, v 2..14.
    public sealed class AutopilotCohortTests
    {
        private const float Turn = 35f * (3.14159274f * 2f / 360f);
        private const float Stride = 2f;
        private const float Radius = 3.2f;

        private static void Floor(float u, float v, out float x, out float y) => FoundryMap.ToFloor(u, v, out x, out y);

        private static void Design(float x, float y, out float u, out float v) => FoundryMap.ToDesign(x, y, out u, out v);

        // The careful pilot has every habit; each other cohort is it less what its legend says, and nothing else.
        [Test]
        public void EachCohortIsTheCarefulPilotLessTheHabitsItSaysItDrops()
        {
            Assert.That(AutopilotCohort.Careful.Habits, Is.EqualTo(AutopilotCohort.Habit.All));
            Assert.That(AutopilotCohort.Cohorts[0], Is.SameAs(AutopilotCohort.Careful), "the careful pilot first, the default");
            Assert.That(AutopilotCohort.Cohorts.Select(c => c.Name).Distinct().Count(), Is.EqualTo(AutopilotCohort.Cohorts.Count));
            Assert.That(Dropped(AutopilotCohort.Standing), Is.EqualTo(AutopilotCohort.Habit.Kites));
            Assert.That(Dropped(AutopilotCohort.FirstCard), Is.EqualTo(AutopilotCohort.Habit.ScoresCards));
            Assert.That(Dropped(AutopilotCohort.Clumsy), Is.EqualTo(AutopilotCohort.Habit.KitesWithRoom |
                AutopilotCohort.Habit.KitesEachAtItsReach | AutopilotCohort.Habit.EscapesAwayFromTheWarden |
                AutopilotCohort.Habit.KeepsOutOfTheGlow | AutopilotCohort.Habit.FillsWeaponSlots));
            Assert.That(Dropped(AutopilotCohort.AutoFarmer), Is.EqualTo(AutopilotCohort.Habit.Kites |
                AutopilotCohort.Habit.EscapesMarks | AutopilotCohort.Habit.Shelters | AutopilotCohort.Habit.KeepsOutOfTheGlow |
                AutopilotCohort.Habit.Lures | AutopilotCohort.Habit.UsesTheAbility | AutopilotCohort.Habit.ScoresCards));
        }

        private static AutopilotCohort.Habit Dropped(AutopilotCohort cohort) => AutopilotCohort.Habit.All & ~cohort.Habits;

        // The step-in (FootworkRule.StepIn; docs/41, status 2026-11-10 night) is research: outside All, no cohort plays with
        // it unless built with it, and one built with it keeps it.
        [Test]
        public void NoCohortStepsInUnlessBuiltWithTheHabit()
        {
            AutopilotCohort.Habit both = AutopilotCohort.Habit.StepsIn | AutopilotCohort.Habit.StepsInOnTheGateBoss;
            Assert.That(AutopilotCohort.Habit.All & both, Is.EqualTo(AutopilotCohort.Habit.None));
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
                Assert.That(cohort.StepsIn || cohort.StepsInOnTheGateBoss, Is.False, cohort.Name);
            var elites = new AutopilotCohort("careful, stepping in", AutopilotCohort.Habit.All | AutopilotCohort.Habit.StepsIn);
            Assert.That(elites.StepsIn, Is.True);
            Assert.That(elites.StepsInOnTheGateBoss, Is.False, "the gate boss is a habit of its own");
            Assert.That(elites.Has(AutopilotCohort.Habit.All), Is.True);
            var boss = new AutopilotCohort("careful, stepping in on the boss", AutopilotCohort.Habit.All | both);
            Assert.That(boss.StepsInOnTheGateBoss, Is.True);
            var bossOnly = new AutopilotCohort("the boss alone", AutopilotCohort.Habit.All | AutopilotCohort.Habit.StepsInOnTheGateBoss);
            Assert.That(bossOnly.StepsIn || bossOnly.StepsInOnTheGateBoss, Is.False, "on the boss only with the step-in itself");
        }

        // development/autopilot.txt: a cohort's name among the line's words, beside "on" or a measurement mode; the careful
        // pilot for none, or for a word that names no cohort.
        [Test]
        public void TheSwitchNamesACohortAmongItsWords()
        {
            Assert.That(AutopilotCohort.FromSwitch(null), Is.SameAs(AutopilotCohort.Careful));
            Assert.That(AutopilotCohort.FromSwitch(""), Is.SameAs(AutopilotCohort.Careful));
            Assert.That(AutopilotCohort.FromSwitch("on"), Is.SameAs(AutopilotCohort.Careful));
            Assert.That(AutopilotCohort.FromSwitch("linger"), Is.SameAs(AutopilotCohort.Careful));
            Assert.That(AutopilotCohort.FromSwitch("standing"), Is.SameAs(AutopilotCohort.Standing));
            Assert.That(AutopilotCohort.FromSwitch("on first-card"), Is.SameAs(AutopilotCohort.FirstCard));
            Assert.That(AutopilotCohort.FromSwitch("linger\tclumsy "), Is.SameAs(AutopilotCohort.Clumsy));
            Assert.That(AutopilotCohort.FromSwitch("auto-farmer standing"), Is.SameAs(AutopilotCohort.AutoFarmer), "the first named");
            Assert.That(AutopilotCohort.FromSwitch("standng"), Is.SameAs(AutopilotCohort.Careful), "no cohort by that name");
            foreach (AutopilotCohort cohort in AutopilotCohort.Cohorts)
            {
                Assert.That(AutopilotCohort.TryNamed(cohort.Name, out AutopilotCohort named), Is.True, cohort.Name);
                Assert.That(named, Is.SameAs(cohort));
            }
            Assert.That(AutopilotCohort.TryNamed("Careful", out _), Is.False, "names are lower case, as written");
        }

        // Pushed into the east wall: the careful kite turns until its stride has room (FootworkRule.Kite); without the
        // habit the stride runs straight on, off the floor, as the autopilot's did before 2026-11-04.
        [Test]
        public void WithoutRoomTheKiteRunsStraightIntoTheWall()
        {
            Floor(13f, -8f, out float x, out float y);
            Floor(1f, 0f, out float px, out float py);
            float cos = (float)Math.Cos(Turn), sin = (float)Math.Sin(Turn);
            Assert.That(AutopilotCohort.Clumsy.Kite(x, y, px, py, Turn, Stride, false, out float gx, out float gy), Is.True);
            Assert.That(gx, Is.EqualTo(x + (px * cos - py * sin) * Stride));
            Assert.That(gy, Is.EqualTo(y + (px * sin + py * cos) * Stride));
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.False, "off the floor");

            Assert.That(AutopilotCohort.Careful.Kite(x, y, px, py, Turn, Stride, false, out gx, out gy), Is.True);
            Assert.That(FootworkRule.Kite(x, y, px, py, Turn, Stride, false, out float rx, out float ry), Is.True);
            Assert.That((gx, gy), Is.EqualTo((rx, ry)));
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.True);
        }

        // South of the pit, pushed north at it while the band glows: the careful kite keeps out of the band, the clumsy
        // one - without KeepsOutOfTheGlow, and without room - walks into it.
        [Test]
        public void OnlyACohortThatKeepsOutOfTheGlowKitesAroundIt()
        {
            Floor(-10f, -2.2f, out float x, out float y);
            Floor(0f, 1f, out float px, out float py);
            Assert.That(AutopilotCohort.Clumsy.Kite(x, y, px, py, Turn, Stride, true, out float gx, out float gy), Is.True);
            Assert.That(PitEruptionRule.InBand(gx, gy), Is.True, "into the glowing band");
            var roomOnly = new AutopilotCohort("room, no glow", AutopilotCohort.Habit.All & ~AutopilotCohort.Habit.KeepsOutOfTheGlow);
            Assert.That(roomOnly.Kite(x, y, px, py, Turn, Stride, true, out gx, out gy), Is.True);
            Assert.That(PitEruptionRule.InBand(gx, gy), Is.True, "room is not the glow");
            Assert.That(AutopilotCohort.Careful.Kite(x, y, px, py, Turn, Stride, true, out gx, out gy), Is.True);
            for (int i = 1; i <= 8; i++)
                Assert.That(PitEruptionRule.InBand(x + (gx - x) * i / 8f, y + (gy - y) * i / 8f), Is.False, $"step {i}");

            Assert.That(AutopilotCohort.Careful.Guards(true), Is.True);
            Assert.That(AutopilotCohort.Careful.Guards(false), Is.False, "nothing to keep out of");
            Assert.That(AutopilotCohort.Standing.Guards(true), Is.True);
            Assert.That(AutopilotCohort.Clumsy.Guards(true), Is.False);
            Assert.That(AutopilotCohort.AutoFarmer.Guards(true), Is.False);
        }

        // The hero a unit east of the mark's centre, the Warden to the east: before 2026-11-04 the escape took the nearest
        // of eight points, east toward the Warden; the careful escape leaves to the west, away from it.
        [Test]
        public void WithoutTheWardenInMindTheEscapeTakesTheNearestWayOut()
        {
            Floor(4f, -8f, out float markX, out float markY);
            Floor(5f, -8f, out float x, out float y);
            Floor(8f, -8f, out float wardenX, out float wardenY);
            Assert.That(AutopilotCohort.Clumsy.EscapeMark(x, y, markX, markY, Radius, wardenX, wardenY, false, out float gx, out float gy), Is.True);
            Assert.That(FootworkRule.EscapeMarkNearest(x, y, markX, markY, Radius, out float nx, out float ny), Is.True);
            Assert.That((gx, gy), Is.EqualTo((nx, ny)));
            Design(gx, gy, out float u, out float v);
            Assert.That(u, Is.EqualTo(4f + Radius).Within(1e-3f), "east, to the Warden's side");
            Assert.That(v, Is.EqualTo(-8f).Within(1e-3f));

            Assert.That(AutopilotCohort.Careful.EscapeMark(x, y, markX, markY, Radius, wardenX, wardenY, false, out gx, out gy), Is.True);
            Design(gx, gy, out u, out _);
            Assert.That(u, Is.LessThan(4f), "west, away from the Warden");
        }

        // West of the pit, where a yard brazier stands on the rim spot: the careful shelter leaves the band; without
        // KeepsOutOfTheGlow it is the rim spot, inside the brazier, and the hero walked at it and stayed in the band.
        [Test]
        public void WithoutTheGlowInMindTheShelterIsTheRimSpotWhereverItLies()
        {
            float depth = PitEruptionRule.Reach + 1.4f;
            Floor(-17.5f, 2.5f, out float x, out float y);
            FootworkRule.RimSpot(x, y, depth, out float rx, out float ry);
            AutopilotCohort.Clumsy.Shelter(x, y, depth, out float gx, out float gy);
            Assert.That((gx, gy), Is.EqualTo((rx, ry)));
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.False, "the brazier");

            AutopilotCohort.Careful.Shelter(x, y, depth, out gx, out gy);
            Assert.That(PitEruptionRule.InBand(gx, gy), Is.False);
            Assert.That(FoundryMap.Contains(gx, gy, HeroMotion.EdgeMargin), Is.True);
        }

        // A Tank's 1.7 outreaches the kite reach of 1.6: the careful pilot kites it just past its own reach, the clumsy
        // one at the kite reach, inside it; the gate boss is kited past its reach by both.
        [Test]
        public void WithoutEachEnemysReachEveryEnemyButTheGateBossIsKitedAtTheKiteReach()
        {
            const float kiteReach = 1.6f;
            Assert.That(AutopilotCohort.Careful.ThreatReach(kiteReach, 1.7f, false), Is.EqualTo(1.7f + FootworkRule.KiteMargin));
            Assert.That(AutopilotCohort.Clumsy.ThreatReach(kiteReach, 1.7f, false), Is.EqualTo(kiteReach));
            Assert.That(AutopilotCohort.Clumsy.ThreatReach(kiteReach, 1.7f, true), Is.EqualTo(1.7f + FootworkRule.GateKiteMargin));
            Assert.That(AutopilotCohort.Careful.ThreatReach(kiteReach, 1.7f, true), Is.EqualTo(1.7f + FootworkRule.GateKiteMargin));
        }

        // docs/42's CardPicker order: holding two weapons and two tomes, the careful pilot takes a third weapon while a
        // slot is free, the clumsy one does not (a tome at 15 and a general card at 20 outscore its 10); without
        // ScoresCards, the first card; health first when the hero is low; the first of equals.
        [Test]
        public void EachCohortTakesItsCard()
        {
            UpgradeOption tome = Card("tome_force", UpgradeStat.Damage, OfferCategory.NewTome);
            UpgradeOption weapon = Card("weapon_staff", UpgradeStat.Damage, OfferCategory.NewWeapon);
            UpgradeOption general = Card("upgrade_speed", UpgradeStat.MoveSpeed, OfferCategory.General);
            UpgradeOption health = Card("upgrade_health", UpgradeStat.MaxHealth, OfferCategory.General);
            var offer = new UpgradeOffer(new[] { tome, weapon, general });

            Assert.That(AutopilotCohort.Careful.WeaponsWanted, Is.EqualTo(WeaponLoadout.MaxSlots));
            Assert.That(AutopilotCohort.Clumsy.WeaponsWanted, Is.EqualTo(2));
            Assert.That(AutopilotCohort.Careful.Pick(offer, false, 2, 2), Is.EqualTo(1), "a third weapon");
            Assert.That(AutopilotCohort.Clumsy.Pick(offer, false, 2, 2), Is.EqualTo(2), "no third weapon");
            Assert.That(AutopilotCohort.Clumsy.Pick(offer, false, 1, 2), Is.EqualTo(1), "a second weapon, as before");
            Assert.That(AutopilotCohort.Careful.Pick(offer, false, WeaponLoadout.MaxSlots, 2), Is.EqualTo(2), "the slots full");
            Assert.That(AutopilotCohort.FirstCard.Pick(offer, false, 2, 2), Is.EqualTo(0));
            Assert.That(AutopilotCohort.AutoFarmer.Pick(offer, true, 1, 0), Is.EqualTo(0));

            var low = new UpgradeOffer(new[] { weapon, general, health });
            Assert.That(AutopilotCohort.Careful.Pick(low, true, 1, 0), Is.EqualTo(2), "health first when low");
            Assert.That(AutopilotCohort.Careful.Pick(low, false, 1, 0), Is.EqualTo(0));
            var equals = new UpgradeOffer(new[] { general, Card("upgrade_crit", UpgradeStat.CritChance, OfferCategory.General) });
            Assert.That(AutopilotCohort.Careful.Pick(equals, false, 1, 0), Is.EqualTo(0), "the first of equals");
        }

        // The careful score since 2026-11-10 (docs/41): a tome's level before a weapon's line - it raises every weapon held -
        // but a line still before the tomes' levels while it builds toward its weapon's evolution; an evolution before a new
        // weapon; a new weapon and the first two tomes before either kind of line.
        [Test]
        public void TheTomesLevelsComeBeforeTheWeaponsLinesButALineTowardAnEvolutionBeforeThem()
        {
            UpgradeOption line = Card("line_sword_damage", UpgradeStat.Damage, OfferCategory.WeaponUpgrade);
            UpgradeOption level = Card("tome_force", UpgradeStat.Damage, OfferCategory.TomeUpgrade);
            UpgradeOption newTome = Card("tome_tempo", UpgradeStat.AttackSpeed, OfferCategory.NewTome);
            UpgradeOption newWeapon = Card("weapon_staff", UpgradeStat.Damage, OfferCategory.NewWeapon);
            UpgradeOption evolution = new UpgradeOption("evolution_sword", "evolution_sword", "+{0:0}", UpgradeStat.PulseDamage,
                new StatModifier(ModifierOperation.Flat, 1f), 1, null, "weapon_sword", OfferCategory.WeaponUpgrade);
            AutopilotCohort careful = AutopilotCohort.Careful;
            int Score(UpgradeOption card, bool toward) => careful.CardScore(card, UpgradeRarity.Common, false, 2, 1, toward);

            Assert.That(Score(level, false), Is.GreaterThan(Score(line, false)), "a tome's level before a line");
            Assert.That(Score(line, true), Is.GreaterThan(Score(level, false)), "a line toward an evolution before a tome's level");
            Assert.That(Score(newTome, false), Is.GreaterThan(Score(line, true)), "the second tome before any line");
            Assert.That(Score(newWeapon, false), Is.GreaterThan(Score(newTome, false)), "a new weapon before a new tome");
            Assert.That(Score(evolution, false), Is.GreaterThan(Score(newWeapon, false)), "an evolution before a new weapon");

            Assert.That(careful.Pick(new UpgradeOffer(new[] { line, level }), false, 2, 1), Is.EqualTo(1));
            Assert.That(careful.Pick(new UpgradeOffer(new[] { level, evolution }), false, 2, 1), Is.EqualTo(1));
            Assert.That(AutopilotCohort.Standing.Pick(new UpgradeOffer(new[] { line, level }), false, 2, 1), Is.EqualTo(1),
                "every cohort that scores its cards scores them so");
            Assert.That(AutopilotCohort.FirstCard.Pick(new UpgradeOffer(new[] { line, level }), false, 2, 1), Is.EqualTo(0));
        }

        // A health or armour card is scored at the tier it is offered, TierStep a tier: its tiers scale 1 / 2 / 4 / 5.3 where
        // a line's scale 1 / 1.25 / 1.5 / 2. A common one stays under a line, a rare one outranks a line and a tome's level,
        // an epic one a new tome, a gold health card a new weapon; a speed card is the same at every tier.
        [Test]
        public void AHealthOrArmourCardIsScoredAtItsTier()
        {
            UpgradeOption line = Card("line_sword_damage", UpgradeStat.Damage, OfferCategory.WeaponUpgrade);
            UpgradeOption level = Card("tome_force", UpgradeStat.Damage, OfferCategory.TomeUpgrade);
            UpgradeOption newTome = Card("tome_tempo", UpgradeStat.AttackSpeed, OfferCategory.NewTome);
            UpgradeOption newWeapon = Card("weapon_staff", UpgradeStat.Damage, OfferCategory.NewWeapon);
            UpgradeOption health = Card("upgrade_health", UpgradeStat.MaxHealth, OfferCategory.General);
            UpgradeOption armour = Card("upgrade_armor", UpgradeStat.Armor, OfferCategory.General);
            UpgradeOption speed = Card("upgrade_move_speed", UpgradeStat.MoveSpeed, OfferCategory.General);
            AutopilotCohort careful = AutopilotCohort.Careful;

            int Taken(UpgradeOption other, UpgradeOption hero, UpgradeRarity tier) =>
                careful.Pick(new UpgradeOffer(new[] { other, hero }, 0, new[] { UpgradeRarity.Common, tier }), false, 2, 1);

            Assert.That(Taken(line, armour, UpgradeRarity.Common), Is.EqualTo(0), "a common armour card under a line");
            Assert.That(Taken(line, health, UpgradeRarity.Common), Is.EqualTo(0), "a common health card under a line");
            Assert.That(Taken(line, armour, UpgradeRarity.Rare), Is.EqualTo(1), "a rare armour card over a line");
            Assert.That(Taken(level, health, UpgradeRarity.Rare), Is.EqualTo(1), "a rare health card over a tome's level");
            Assert.That(Taken(newTome, armour, UpgradeRarity.Rare), Is.EqualTo(0), "a rare armour card under a new tome");
            Assert.That(Taken(newTome, health, UpgradeRarity.Epic), Is.EqualTo(1), "an epic health card over a new tome");
            Assert.That(Taken(newWeapon, armour, UpgradeRarity.Epic), Is.EqualTo(0), "an epic armour card under a new weapon");
            Assert.That(Taken(newWeapon, health, UpgradeRarity.Gold), Is.EqualTo(1), "a gold health card over a new weapon");
            Assert.That(Taken(line, speed, UpgradeRarity.Gold), Is.EqualTo(0), "a speed card's tier does not count");
            Assert.That(careful.CardScore(health, UpgradeRarity.Rare, false, 2, 1, false) - careful.CardScore(health, UpgradeRarity.Common, false, 2, 1, false),
                Is.EqualTo(AutopilotCohort.TierStep));
            Assert.That(careful.CardScore(health, UpgradeRarity.Common, true, 2, 1, false),
                Is.GreaterThan(careful.CardScore(armour, UpgradeRarity.Gold, true, 2, 1, false)), "health first when low, whatever the tier");
        }

        // A line leads to its weapon's evolution while the weapon is held, has not evolved, stands below
        // EvolutionRule.WeaponEvolutionLevel, and the tome it evolves with is held (the Sword with Force here, the only tome:
        // every offer is the Sword's card and Force's, so taking Force is one pick on any seed). Through the run's own
        // service the careful pilot then takes the line over Force's level, and Force's level once the Sword is at 5.
        [Test]
        public void ALineLeadsToItsEvolutionBelowLevelFiveWithItsTomeHeld()
        {
            var run = new RunState(10, 0.5f, 2, 7);
            var loadout = new WeaponLoadout(7);
            WeaponCard sword = DescentSimulation.Sword().CreateCard();
            WeaponRuntime primary = sword.CreateRuntime();
            Assert.That(loadout.TryAdd(sword.Id, primary), Is.True);
            UpgradeOption force = DescentSimulation.Force();
            var upgrades = new UpgradeService(run, primary, new UpgradeOption[0], 3, new HeroStats(100f, 2.5f, HeroStamina.DefaultBar),
                false, loadout, new[] { sword }, new[] { force });
            UpgradeOption line = sword.Lines[0];
            UpgradeOption daggerLine = DescentSimulation.Daggers().CreateCard().Lines[0];

            Assert.That(AutopilotCohort.LeadsToEvolution(line, upgrades), Is.False, "Force not held yet");
            run.GrantBonusUpgrade();
            UpgradeOffer offer = upgrades.CurrentOffer;
            Assert.That(offer, Is.Not.Null);
            int tome = -1;
            for (int i = 0; i < offer.Choices.Count; i++)
                if (offer.Choices[i].Category == OfferCategory.NewTome)
                    tome = i;
            Assert.That(tome, Is.GreaterThanOrEqualTo(0), "Force is offered");
            Assert.That(upgrades.TrySelect(offer, tome), Is.True);
            Assert.That(upgrades.TomeLevel(force.Id), Is.EqualTo(1));

            Assert.That(AutopilotCohort.LeadsToEvolution(line, upgrades), Is.True);
            Assert.That(AutopilotCohort.LeadsToEvolution(daggerLine, upgrades), Is.False, "the Daggers are not held");
            Assert.That(AutopilotCohort.LeadsToEvolution(sword.Evolution, upgrades), Is.False, "the evolution is not a line");
            Assert.That(AutopilotCohort.LeadsToEvolution(line, null), Is.False);
            UpgradeOption forceLevel = force.WithCategory(OfferCategory.TomeUpgrade);
            var choice = new UpgradeOffer(new[] { forceLevel, line });
            Assert.That(AutopilotCohort.Careful.Pick(choice, false, upgrades), Is.EqualTo(1), "the line toward the evolution");
            Assert.That(AutopilotCohort.Careful.Pick(choice, false, 1, 1), Is.EqualTo(0), "without the service, Force's level");

            for (int level = loadout.At(0).Level; level < EvolutionRule.WeaponEvolutionLevel; level++)
                Assert.That(loadout.TryLevelUp(sword.Id), Is.True);
            Assert.That(loadout.At(0).Level, Is.EqualTo(EvolutionRule.WeaponEvolutionLevel));
            Assert.That(AutopilotCohort.LeadsToEvolution(line, upgrades), Is.False, "the Sword at its evolution's level");
            Assert.That(AutopilotCohort.Careful.Pick(choice, false, upgrades), Is.EqualTo(0), "Force's level now");
        }

        private static UpgradeOption Card(string id, UpgradeStat stat, OfferCategory category) =>
            new UpgradeOption(id, id, "+{0:0}", stat, new StatModifier(ModifierOperation.Flat, 1f), 4, category: category);
    }
}
