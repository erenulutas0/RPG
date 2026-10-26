using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Weapon evolutions (docs/42, PR 5): in category mode a carried weapon at level 5 whose tome is at level 2 is offered
    // its evolution as one more entry of its own table - a WeaponUpgrade card that evolves from the weapon - once per run.
    // Taking it grants a behavioural stat to that weapon's runtime alone and makes the world harder by the cost every
    // evolution pays (EvolutionRule). The legacy engine never sees any of it.
    public sealed class WeaponEvolutionTests
    {
        private const int Many = 200;

        // One weapon of the simulation's catalogue in slot 0 and in the catalogue, its tome the only tome, no general
        // cards: every offer is then at most the weapon's card and the tome's card, so a test chooses exactly what grows.
        private static UpgradeService Service(int seed, DescentSimulation.HeroWeapon start, UpgradeOption tome,
            out WeaponLoadout loadout, out RunState run, out HeroStats heroStats,
            DescentSimulation.HeroWeapon alsoHeld = null)
        {
            run = new RunState(10, 0.5f, 2, seed);
            loadout = new WeaponLoadout(seed);
            WeaponCard card = start.CreateCard();
            WeaponRuntime primary = card.CreateRuntime();
            loadout.TryAdd(card.Id, primary);
            if (alsoHeld != null)
                Assert.That(loadout.TryAdd(alsoHeld.Id, alsoHeld.CreateRuntime()), Is.True);
            heroStats = new HeroStats(100f, 2.5f, HeroStamina.DefaultBar);
            return new UpgradeService(run, primary, new UpgradeOption[0], 3, heroStats, false, loadout,
                new[] { card }, new[] { tome });
        }

        private static void Grant(RunState run, int count)
        {
            for (int i = 0; i < count; i++)
                run.GrantBonusUpgrade();
        }

        private static int Find(UpgradeOffer offer, Func<UpgradeOption, bool> match)
        {
            for (int i = 0; i < offer.Choices.Count; i++)
                if (match(offer.Choices[i]))
                    return i;
            return -1;
        }

        private static bool IsLine(UpgradeOption card) => card.Category == OfferCategory.WeaponUpgrade && !card.IsEvolution;
        private static bool IsTome(UpgradeOption card) =>
            card.Category == OfferCategory.NewTome || card.Category == OfferCategory.TomeUpgrade;

        // Takes the card match finds, failing if the offer does not show one, after checking the offer shows no
        // evolution while it must not.
        private static void Take(UpgradeService service, Func<UpgradeOption, bool> match, bool evolutionAllowed,
            string where)
        {
            UpgradeOffer offer = service.CurrentOffer;
            Assert.That(offer, Is.Not.Null, where);
            if (!evolutionAllowed)
                Assert.That(Find(offer, card => card.IsEvolution), Is.EqualTo(-1), $"{where}: an evolution before it unlocked");
            int slot = Find(offer, match);
            Assert.That(slot, Is.GreaterThanOrEqualTo(0), $"{where}: the card wanted was not offered");
            Assert.That(service.TrySelect(offer, slot), Is.True, where);
        }

        [Test]
        public void TheRuleIsTheWeaponAtFiveAndItsTomeAtTwo()
        {
            Assert.That(EvolutionRule.WeaponEvolutionLevel, Is.EqualTo(5));
            Assert.That(EvolutionRule.WeaponEvolutionTomeLevel, Is.EqualTo(2));
            Assert.That(EvolutionRule.WeaponUnlocked(4, 2), Is.False);
            Assert.That(EvolutionRule.WeaponUnlocked(5, 1), Is.False);
            Assert.That(EvolutionRule.WeaponUnlocked(5, 0), Is.False);
            Assert.That(EvolutionRule.WeaponUnlocked(5, 2), Is.True);
            Assert.That(EvolutionRule.WeaponUnlocked(WeaponLoadout.MaxLevel, UpgradeService.MaxTomeLevel), Is.True);
        }

        // Each half of the gate alone is not enough, whichever is reached first; both together open the evolution, which
        // then comes up within a few offers since it is one entry of four in the weapon's table.
        [TestCase("weapon_sword")]
        [TestCase("weapon_daggers")]
        [TestCase("weapon_staff")]
        public void TheEvolutionIsOfferedAtWeaponLevelFiveWithItsTomeAtTwoAndNotBefore(string weaponId)
        {
            for (int seed = 0; seed < 20; seed++)
            {
                bool weaponFirst = seed % 2 == 0;
                UpgradeService service = Service(seed, Start(weaponId), TomeOf(weaponId), out WeaponLoadout loadout,
                    out RunState run, out _);
                Grant(run, 40);
                string where = $"{weaponId}, seed {seed}, {(weaponFirst ? "weapon" : "tome")} first";

                if (weaponFirst)
                {
                    Take(service, IsTome, false, where);
                    for (int i = 0; i < 4; i++)
                        Take(service, IsLine, false, where);
                    Assert.That(loadout.At(0).Level, Is.EqualTo(5), where);
                    Assert.That(service.TomeLevel(TomeOf(weaponId).Id), Is.EqualTo(1), where);
                    // The weapon at 5 and the tome at 1 show no evolution; this pick takes the tome to 2.
                    Take(service, IsTome, false, where);
                }
                else
                {
                    Take(service, IsTome, false, where);
                    Take(service, IsTome, false, where);
                    for (int i = 0; i < 3; i++)
                        Take(service, IsLine, false, where);
                    Assert.That(loadout.At(0).Level, Is.EqualTo(4), where);
                    Assert.That(service.TomeLevel(TomeOf(weaponId).Id), Is.EqualTo(2), where);
                    Take(service, IsLine, false, where);
                }

                Assert.That(EvolutionRule.WeaponUnlocked(loadout.At(0).Level, service.TomeLevel(TomeOf(weaponId).Id)),
                    Is.True, where);
                // From here on the evolution may be shown, as one entry of the weapon's table: decline it by taking the
                // tome's card while that lasts and then the weapon's lines, and it must come up - at the latest once the
                // weapon is full and the evolution is the only entry left.
                bool shown = false;
                while (service.CurrentOffer != null)
                {
                    UpgradeOffer current = service.CurrentOffer;
                    int evolution = Find(current, card => card.IsEvolution);
                    if (evolution >= 0)
                    {
                        shown = true;
                        Assert.That(current.Choices[evolution].Id, Is.EqualTo(Start(weaponId).Evolution.Id), where);
                        Assert.That(current.Choices[evolution].Category, Is.EqualTo(OfferCategory.WeaponUpgrade), where);
                        Assert.That(current.Choices[evolution].EvolvesFromId, Is.EqualTo(weaponId), where);
                        break;
                    }
                    int slot = Find(current, IsTome);
                    Assert.That(service.TrySelect(current, slot >= 0 ? slot : Find(current, IsLine)), Is.True, where);
                }
                Assert.That(shown, Is.True, $"{where}: the evolution never came up");
            }
        }

        [TestCase("weapon_sword", "weapon_daggers")]
        [TestCase("weapon_daggers", "weapon_sword")]
        [TestCase("weapon_staff", "weapon_sword")]
        [TestCase("weapon_sword", "weapon_staff")]
        public void TheEvolutionIsTakenOnceWritesToItsOwnWeaponOnlyAndMakesTheWorldHarder(string weaponId, string otherId)
        {
            UpgradeOption evolution = Start(weaponId).Evolution;
            for (int seed = 0; seed < 10; seed++)
            {
                UpgradeService service = Service(seed, Start(weaponId), TomeOf(weaponId), out WeaponLoadout loadout,
                    out RunState run, out HeroStats heroStats, Start(otherId));
                Grant(run, Many);
                string where = $"{weaponId}, seed {seed}";
                WeaponRuntime own = loadout.At(0).Runtime;
                WeaponRuntime other = loadout.At(1).Runtime;

                int taken = 0;
                int levelBefore = 0;
                float difficultyBefore = 0f;
                UpgradeRarity tier = UpgradeRarity.Common;
                while (service.CurrentOffer != null)
                {
                    UpgradeOffer offer = service.CurrentOffer;
                    int slot = Find(offer, card => card.IsEvolution);
                    if (slot >= 0)
                    {
                        Assert.That(taken, Is.Zero, $"{where}: the evolution was offered again after it was taken");
                        Assert.That(service.StacksOf(offer.Choices[slot]), Is.Zero, where);
                        levelBefore = loadout.At(0).Level;
                        difficultyBefore = heroStats.Difficulty;
                        tier = offer.RarityAt(slot);
                        Assert.That(service.TrySelect(offer, slot), Is.True, where);
                        taken++;

                        Assert.That(service.HasEvolved(weaponId), Is.True, where);
                        Assert.That(service.HasEvolved(otherId), Is.False, where);
                        Assert.That(service.StacksOf(offer.Choices[slot]), Is.EqualTo(1), where);
                        Assert.That(service.EvolutionsTaken, Is.EqualTo(1), where);
                        Assert.That(heroStats.Difficulty - difficultyBefore,
                            Is.EqualTo(EvolutionRule.DifficultyPerStack).Within(1e-6f), $"{where}: the cost was not paid");
                        Assert.That(loadout.At(0).Level, Is.EqualTo(levelBefore), $"{where}: an evolution is not a level");
                        continue;
                    }
                    // Otherwise grow the weapon first, then the tome.
                    slot = Find(offer, IsLine);
                    if (slot < 0)
                        slot = Find(offer, IsTome);
                    Assert.That(slot, Is.GreaterThanOrEqualTo(0), where);
                    Assert.That(service.TrySelect(offer, slot), Is.True, where);
                }

                Assert.That(taken, Is.EqualTo(1), $"{where}: a run that fills its weapon and its tome evolves once");
                Assert.That(heroStats.Difficulty, Is.EqualTo(1f + EvolutionRule.DifficultyPerStack).Within(1e-6f), where);
                float granted = evolution.ModifierFor(tier).Amount;
                // The evolution's own behaviour, at the tier it was taken at, and neither of the other two.
                Assert.That(own.PulseDamage, Is.EqualTo(evolution.Stat == UpgradeStat.PulseDamage ? granted : 0f)
                    .Within(1e-6f), where);
                Assert.That(own.CritSplash, Is.EqualTo(evolution.Stat == UpgradeStat.CritSplash ? granted : 0f)
                    .Within(1e-6f), where);
                Assert.That(own.KillBurst, Is.EqualTo(evolution.Stat == UpgradeStat.KillBurst ? granted : 0f)
                    .Within(1e-6f), where);
                Assert.That(other.PulseDamage, Is.Zero, $"{where}: the second weapon pulses");
                Assert.That(other.CritSplash, Is.Zero, $"{where}: the second weapon splinters");
                Assert.That(other.KillBurst, Is.Zero, $"{where}: the second weapon's kills burst");
            }
        }

        // At its last level a weapon has no line left to draw, so its card is its evolution whenever it comes up: an
        // evolution declined while levelling is never lost.
        [Test]
        public void AWeaponAtItsLastLevelStillOffersItsEvolution()
        {
            for (int seed = 0; seed < 10; seed++)
            {
                UpgradeService service = Service(seed, DescentSimulation.Sword(), DescentSimulation.Force(),
                    out WeaponLoadout loadout, out RunState run, out _);
                Grant(run, Many);
                string where = $"seed {seed}";
                Take(service, IsTome, false, where);
                Take(service, IsTome, false, where);
                // Level the weapon to its last, declining the evolution for the tome's card whenever it stands where a
                // line would.
                while (loadout.At(0).Level < WeaponLoadout.MaxLevel)
                {
                    UpgradeOffer levelling = service.CurrentOffer;
                    int line = Find(levelling, IsLine);
                    int pick = line >= 0 ? line : Find(levelling, IsTome);
                    Assert.That(pick, Is.GreaterThanOrEqualTo(0), $"{where}: nothing but the evolution to take");
                    Assert.That(service.TrySelect(levelling, pick), Is.True, where);
                }

                UpgradeOffer offer = service.CurrentOffer;
                Assert.That(Find(offer, IsLine), Is.EqualTo(-1), $"{where}: a line offered past the last level");
                int slot = Find(offer, card => card.Category == OfferCategory.WeaponUpgrade);
                Assert.That(slot, Is.GreaterThanOrEqualTo(0), $"{where}: a full weapon with an evolution waiting is closed");
                Assert.That(offer.Choices[slot].Id, Is.EqualTo("evolution_sword"), where);
                Assert.That(service.TrySelect(offer, slot), Is.True, where);
                Assert.That(loadout.At(0).Level, Is.EqualTo(WeaponLoadout.MaxLevel), where);

                // Evolved and full: the weapon's kind is closed for good.
                while (service.CurrentOffer != null)
                {
                    UpgradeOffer next = service.CurrentOffer;
                    Assert.That(Find(next, card => card.Category == OfferCategory.WeaponUpgrade), Is.EqualTo(-1), where);
                    Assert.That(service.TrySelect(next, 0), Is.True, where);
                }
            }
        }

        // A weapon with no evolution authored never offers one, and a weapon whose tome is not in the catalogue never
        // unlocks its own.
        [Test]
        public void NoEvolutionWithoutOneAuthoredOrWithoutItsTome()
        {
            DescentSimulation.HeroWeapon unevolved = DescentSimulation.Staff();
            unevolved.Evolution = null;
            unevolved.EvolvesWithTome = null;
            UpgradeService none = Service(3, unevolved, DescentSimulation.Reach(), out _, out RunState noneRun, out _);
            Grant(noneRun, Many);
            AssertNoEvolution(none, "A Staff with no evolution authored");

            UpgradeService staff = Service(3, DescentSimulation.Staff(), DescentSimulation.Force(), out _, out RunState staffRun,
                out _);
            Grant(staffRun, Many);
            AssertNoEvolution(staff, "Staff with Force, not Reach");

            UpgradeService sword = Service(3, DescentSimulation.Sword(), DescentSimulation.Precision(), out _,
                out RunState swordRun, out _);
            Grant(swordRun, Many);
            AssertNoEvolution(sword, "Sword with Precision, not Force");
        }

        private static void AssertNoEvolution(UpgradeService service, string where)
        {
            while (service.CurrentOffer != null)
            {
                UpgradeOffer offer = service.CurrentOffer;
                Assert.That(Find(offer, card => card.IsEvolution), Is.EqualTo(-1), where);
                Assert.That(service.TrySelect(offer, 0), Is.True, where);
            }
            Assert.That(service.EvolutionsTaken, Is.Zero, where);
        }

        // A card's evolution is refused when it is built rather than found in a run: one with no tome could never unlock,
        // one on a hero stat has no runtime to write to.
        [Test]
        public void AnEvolutionThatCouldNeverWorkIsRefused()
        {
            DescentSimulation.HeroWeapon sword = DescentSimulation.Sword();
            Func<WeaponRuntime> runtime = sword.CreateRuntime;
            Assert.Throws<ArgumentException>(() => new WeaponCard("weapon_sword", "Sword", string.Empty, runtime,
                sword.Lines, DescentSimulation.SwordEvolution()));
            var onHealth = new UpgradeOption("evolution_bad", "Bad", "+{0:0}", UpgradeStat.MaxHealth,
                new StatModifier(ModifierOperation.Flat, 10f), 1);
            Assert.Throws<ArgumentException>(() => new WeaponCard("weapon_sword", "Sword", string.Empty, runtime,
                sword.Lines, onHealth, "tome_force"));
            var gated = new UpgradeOption("evolution_bad", "Bad", "+{0:0}", UpgradeStat.PulseDamage,
                new StatModifier(ModifierOperation.Flat, 0.4f), 1, "upgrade_pulse");
            Assert.Throws<ArgumentException>(() => new WeaponCard("weapon_sword", "Sword", string.Empty, runtime,
                sword.Lines, gated, "tome_force"));
        }

        // The legacy engine is handed no weapon cards and so can never offer a weapon's evolution; its own evolutions
        // keep their own gate and cost (UpgradeTests, DescentTests).
        [Test]
        public void TheLegacyEngineNeverOffersAWeaponsEvolution()
        {
            var run = new RunState(10, 0.5f, 2, 11);
            var service = new UpgradeService(run, DescentSimulation.Sword().CreateRuntime(), DescentSimulation.Pool(), 3);
            Grant(run, Many);
            run.AddExperience(2000);
            while (service.CurrentOffer != null)
            {
                UpgradeOffer offer = service.CurrentOffer;
                foreach (UpgradeOption card in offer.Choices)
                {
                    Assert.That(card.Category, Is.EqualTo(OfferCategory.General), card.Id);
                    Assert.That(card.Id, Does.Not.StartWith("evolution_"), card.Id);
                }
                Assert.That(service.TrySelect(offer, 0), Is.True);
            }
            Assert.That(service.HasEvolved("weapon_sword"), Is.False);
        }

        private static DescentSimulation.HeroWeapon Start(string weaponId) => weaponId switch
        {
            "weapon_sword" => DescentSimulation.Sword(),
            "weapon_staff" => DescentSimulation.Staff(),
            _ => DescentSimulation.Daggers()
        };

        private static UpgradeOption TomeOf(string weaponId) => weaponId switch
        {
            "weapon_sword" => DescentSimulation.Force(),
            "weapon_staff" => DescentSimulation.Reach(),
            _ => DescentSimulation.Precision()
        };
    }
}
