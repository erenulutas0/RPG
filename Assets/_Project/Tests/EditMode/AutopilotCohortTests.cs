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

        private static UpgradeOption Card(string id, UpgradeStat stat, OfferCategory category) =>
            new UpgradeOption(id, id, "+{0:0}", stat, new StatModifier(ModifierOperation.Flat, 1f), 4, category: category);
    }
}
