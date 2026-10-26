using System;
using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Category offers (docs/42, PR 4): once UpgradeService is handed a loadout and a catalogue, each card of an offer is
    // a kind of pick drawn from OfferWeights and then a card of that kind, and every limit - three weapons, three tomes,
    // level 8 - closes its kind rather than producing a card that cannot be taken. The legacy engine is OfferEngineTests'
    // and UpgradeTests', which never pass a loadout and so never reach any of this.
    public sealed class OfferCategoryTests
    {
        private const int AllPending = 200;

        private static WeaponCard[] SimCards()
        {
            DescentSimulation.HeroWeapon[] weapons = DescentSimulation.WeaponCatalogue();
            var cards = new WeaponCard[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
                cards[i] = weapons[i].CreateCard();
            return cards;
        }

        // A category-mode service over the simulation's catalogue unless told otherwise, holding the start weapon in
        // slot 0. Nothing is pending until a test grants it.
        private static UpgradeService Service(int seed, out WeaponLoadout loadout, out RunState run,
            string start = "weapon_sword", WeaponCard[] cards = null, UpgradeOption[] tomes = null,
            UpgradeOption[] pool = null, WeaponRuntime[] alsoHeld = null, string[] alsoHeldIds = null)
        {
            cards = cards ?? SimCards();
            run = new RunState(10, 0.5f, 2, seed);
            loadout = new WeaponLoadout(seed);
            WeaponRuntime primary = null;
            foreach (WeaponCard card in SimCards())
                if (card.Id == start)
                    primary = card.CreateRuntime();
            Assert.That(primary, Is.Not.Null, start);
            loadout.TryAdd(start, primary);
            if (alsoHeld != null)
                for (int i = 0; i < alsoHeld.Length; i++)
                    Assert.That(loadout.TryAdd(alsoHeldIds[i], alsoHeld[i]), Is.True);
            return new UpgradeService(run, primary, pool ?? DescentSimulation.Pool(), 3,
                new HeroStats(100f, 2.5f, HeroStamina.DefaultBar), false, loadout, cards,
                tomes ?? DescentSimulation.Tomes());
        }

        private static void Grant(RunState run, int count)
        {
            for (int i = 0; i < count; i++)
                run.GrantBonusUpgrade();
        }

        // Answers every offer until none is open, asking choose for the slot and letting inspect look first.
        private static int Drive(UpgradeService service, Func<UpgradeOffer, int> choose, Action<UpgradeOffer> inspect = null)
        {
            int offers = 0;
            while (service.CurrentOffer != null)
            {
                UpgradeOffer offer = service.CurrentOffer;
                inspect?.Invoke(offer);
                Assert.That(service.TrySelect(offer, choose(offer)), Is.True);
                offers++;
            }
            return offers;
        }

        private static int SlotOf(UpgradeOffer offer, OfferCategory category, int otherwise = 0)
        {
            for (int i = 0; i < offer.Choices.Count; i++)
                if (offer.Choices[i].Category == category)
                    return i;
            return otherwise;
        }

        private static string Describe(UpgradeOffer offer)
        {
            var parts = new List<string>();
            for (int i = 0; i < offer.Choices.Count; i++)
                parts.Add($"{offer.Choices[i].Id}/{offer.Choices[i].Category}/{offer.RarityAt(i)}");
            return string.Join(",", parts);
        }

        private static bool[] Open(params OfferCategory[] categories)
        {
            var open = new bool[OfferWeights.CategoryCount];
            foreach (OfferCategory category in categories)
                open[(int)category] = true;
            return open;
        }

        private static readonly OfferCategory[] All =
        {
            OfferCategory.General, OfferCategory.NewWeapon, OfferCategory.WeaponUpgrade, OfferCategory.NewTome,
            OfferCategory.TomeUpgrade
        };

        [Test]
        public void TheWeightsAreTheReportsAndSumToAHundredInBothPhases()
        {
            foreach (int level in new[] { 1, OfferWeights.LateFromLevel })
            {
                int sum = 0;
                foreach (OfferCategory category in All)
                    sum += OfferWeights.Weight(category, level);
                Assert.That(sum, Is.EqualTo(100), $"level {level}");
            }

            Assert.That(OfferWeights.LateFromLevel, Is.EqualTo(6));
            Assert.That(OfferWeights.IsLate(5), Is.False);
            Assert.That(OfferWeights.IsLate(6), Is.True);
            int[] early = { 10, 27, 28, 25, 10 };
            int[] late = { 10, 15, 35, 15, 25 };
            for (int i = 0; i < All.Length; i++)
            {
                Assert.That(OfferWeights.Weight(All[i], 5), Is.EqualTo(early[i]), $"{All[i]} early");
                Assert.That(OfferWeights.Weight(All[i], 6), Is.EqualTo(late[i]), $"{All[i]} late");
            }
        }

        [Test]
        public void AClosedCategoryLeavesTheSumAndTheOthersRenormalise()
        {
            bool[] noNewWeapon = Open(OfferCategory.General, OfferCategory.WeaponUpgrade, OfferCategory.NewTome,
                OfferCategory.TomeUpgrade);

            Assert.That(OfferWeights.Probability(OfferCategory.NewWeapon, 1, noNewWeapon), Is.Zero);
            Assert.That(OfferWeights.Probability(OfferCategory.WeaponUpgrade, 1, noNewWeapon), Is.EqualTo(28.0 / 73.0).Within(1e-12));
            Assert.That(OfferWeights.Probability(OfferCategory.TomeUpgrade, 6, noNewWeapon), Is.EqualTo(25.0 / 85.0).Within(1e-12));
            double total = 0.0;
            foreach (OfferCategory category in All)
                total += OfferWeights.Probability(category, 1, noNewWeapon);
            Assert.That(total, Is.EqualTo(1.0).Within(1e-12));

            var drawn = new int[OfferWeights.CategoryCount];
            for (int i = 0; i < 4000; i++)
                drawn[(int)OfferWeights.Draw(RunRandom.Stream(7, RunRandom.Categories, i), 1, noNewWeapon)]++;
            Assert.That(drawn[(int)OfferCategory.NewWeapon], Is.Zero, "A closed category is never drawn.");
            // 28/73 of 4000 is about 1534; the band is wide, it is here to catch a weight handed to the wrong neighbour.
            Assert.That(drawn[(int)OfferCategory.WeaponUpgrade], Is.InRange(1400, 1670));

            bool[] onlyTomes = Open(OfferCategory.TomeUpgrade);
            for (int i = 0; i < 50; i++)
                Assert.That(OfferWeights.Draw(RunRandom.Stream(3, RunRandom.Categories, i), 9, onlyTomes),
                    Is.EqualTo(OfferCategory.TomeUpgrade));

            bool[] none = Open();
            Assert.That(OfferWeights.AnyOpen(1, none), Is.False);
            Assert.Throws<InvalidOperationException>(() =>
                OfferWeights.Draw(RunRandom.Stream(1, RunRandom.Categories, 0), 1, none));
        }

        [Test]
        public void NoNewWeaponIsOfferedOnceThreeSlotsAreHeld()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                UpgradeService service = Service(seed, out WeaponLoadout loadout, out RunState run);
                Grant(run, AllPending);
                bool reachedFull = false;
                Drive(service, offer => SlotOf(offer, OfferCategory.NewWeapon), offer =>
                {
                    Assert.That(loadout.Count, Is.LessThanOrEqualTo(WeaponLoadout.MaxSlots), $"seed {seed}");
                    if (!loadout.IsFull)
                        return;
                    reachedFull = true;
                    foreach (UpgradeOption card in offer.Choices)
                        Assert.That(card.Category, Is.Not.EqualTo(OfferCategory.NewWeapon),
                            $"seed {seed}: a new weapon offered with every slot held");
                });
                Assert.That(reachedFull, Is.True, $"seed {seed}: the run never filled its slots");
            }
        }

        [Test]
        public void AWeaponAtLevelEightLeavesItsUpgrades()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                UpgradeService service = Service(seed, out WeaponLoadout loadout, out RunState run);
                Grant(run, AllPending);
                Drive(service, offer => SlotOf(offer, OfferCategory.WeaponUpgrade), offer =>
                {
                    foreach (UpgradeOption card in offer.Choices)
                    {
                        // An evolution is the one weapon card a weapon at its last level can still show
                        // (WeaponEvolutionTests); it is held to its own gate there.
                        if (card.Category != OfferCategory.WeaponUpgrade || card.IsEvolution)
                            continue;
                        Assert.That(service.StacksOf(card), Is.InRange(1, WeaponLoadout.MaxLevel - 1),
                            $"seed {seed}: {card.Id} offered on a weapon not carried or at its last level");
                    }
                });
                for (int slot = 0; slot < loadout.Count; slot++)
                    Assert.That(loadout.At(slot).Level, Is.EqualTo(WeaponLoadout.MaxLevel),
                        $"seed {seed}: every carried weapon ends at its last level once nothing else is left");
            }
        }

        [Test]
        public void ATomeAtLevelEightLeavesAndNothingIsOfferedOnceEverythingIsFull()
        {
            // One weapon with nothing to level and nothing to pick up, no general cards, one tome: every offer is Force.
            UpgradeService service = Service(1, out WeaponLoadout loadout, out RunState run, cards: new WeaponCard[0],
                tomes: new[] { DescentSimulation.Force() }, pool: new UpgradeOption[0]);
            Grant(run, 12);

            var seen = new List<OfferCategory>();
            Drive(service, offer => 0, offer =>
            {
                Assert.That(offer.Choices.Count, Is.EqualTo(1), "One tome is one card, never repeated within an offer.");
                seen.Add(offer.Choices[0].Category);
            });

            Assert.That(seen.Count, Is.EqualTo(UpgradeService.MaxTomeLevel));
            Assert.That(seen[0], Is.EqualTo(OfferCategory.NewTome));
            for (int i = 1; i < seen.Count; i++)
                Assert.That(seen[i], Is.EqualTo(OfferCategory.TomeUpgrade), $"pick {i}");
            Assert.That(service.TomeLevel("tome_force"), Is.EqualTo(8));
            Assert.That(service.CurrentOffer, Is.Null, "A full tome leaves, and with it the last open category.");
            Assert.That(run.PendingUpgrades, Is.EqualTo(4), "The level-ups past the last card wait unanswered.");
            // Eight green Force levels on the one weapon: (10) * (1 + 8 * 0.08), whatever tiers were drawn at least that.
            Assert.That(loadout.Primary.Damage, Is.GreaterThanOrEqualTo(10f * 1.64f - 1e-3f));
        }

        [Test]
        public void AVitalityTomeLevelsTheHeroNotTheWeapons()
        {
            // A tome on a hero stat writes to the hero: Vitality's health, every level of it, and no weapon changes.
            var run = new RunState(10, 0.5f, 2, 3);
            var loadout = new WeaponLoadout(3);
            WeaponRuntime sword = DescentSimulation.Sword().CreateRuntime();
            loadout.TryAdd("weapon_sword", sword);
            var hero = new HeroStats(100f, 2.5f, HeroStamina.DefaultBar);
            var service = new UpgradeService(run, sword, new UpgradeOption[0], 3, hero, false, loadout, new WeaponCard[0],
                new[] { DescentSimulation.Vitality() });
            float damage = sword.Damage;
            Grant(run, 3);
            Drive(service, offer => 0);
            Assert.That(service.TomeLevel("tome_vitality"), Is.EqualTo(3));
            Assert.That(hero.MaxHealth, Is.GreaterThanOrEqualTo(100f + 3 * 12f - 1e-3f), "Three levels of at least +12.");
            Assert.That(sword.Damage, Is.EqualTo(damage), "The weapons are untouched.");
        }

        [Test]
        public void NeverAFourthTome()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                UpgradeService service = Service(seed, out _, out RunState run);
                Grant(run, AllPending);
                Drive(service, offer => SlotOf(offer, OfferCategory.NewTome, SlotOf(offer, OfferCategory.TomeUpgrade)),
                    offer =>
                    {
                        Assert.That(service.TomesHeld, Is.LessThanOrEqualTo(UpgradeService.MaxTomes), $"seed {seed}");
                        if (service.TomesHeld < UpgradeService.MaxTomes)
                            return;
                        foreach (UpgradeOption card in offer.Choices)
                            Assert.That(card.Category, Is.Not.EqualTo(OfferCategory.NewTome),
                                $"seed {seed}: a fourth tome offered");
                    });
                Assert.That(service.TomesHeld, Is.EqualTo(UpgradeService.MaxTomes), $"seed {seed}");
                int full = 0;
                foreach (UpgradeOption tome in service.Tomes)
                    if (service.TomeLevel(tome.Id) == UpgradeService.MaxTomeLevel)
                        full++;
                Assert.That(full, Is.EqualTo(UpgradeService.MaxTomes), $"seed {seed}: the three held all reach level 8");
            }
        }

        // Every card of every offer is distinct by id and could be taken: the property the renormalisation exists for.
        [Test]
        public void AnOfferNeverRepeatsAnIdAndOnlyShowsCardsThatCanBeTaken()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                UpgradeService service = Service(seed, out WeaponLoadout loadout, out RunState run,
                    start: seed % 3 == 0 ? "weapon_sword" : seed % 3 == 1 ? "weapon_staff" : "weapon_daggers");
                Grant(run, AllPending);
                int offers = Drive(service, offer => (seed + offer.Index) % offer.Choices.Count, offer =>
                {
                    Assert.That(offer.Choices.Count, Is.InRange(1, 3), $"seed {seed}");
                    var ids = new HashSet<string>();
                    foreach (UpgradeOption card in offer.Choices)
                    {
                        string where = $"seed {seed}, offer {offer.Index}, {card.Id}";
                        Assert.That(ids.Add(card.Id), Is.True, $"{where}: offered twice");
                        switch (card.Category)
                        {
                            case OfferCategory.General:
                                Assert.That(WeaponRuntime.Owns(card.Stat), Is.False, $"{where}: a weapon-stat general card");
                                Assert.That(service.StacksOf(card), Is.LessThan(card.MaxStacks), where);
                                break;
                            case OfferCategory.NewWeapon:
                                Assert.That(loadout.Owns(card.Id), Is.False, where);
                                Assert.That(loadout.IsFull, Is.False, where);
                                break;
                            case OfferCategory.WeaponUpgrade when card.IsEvolution:
                                Assert.That(service.HasEvolved(card.EvolvesFromId), Is.False, $"{where}: evolved twice");
                                Assert.That(EvolutionRule.WeaponUnlocked(
                                    loadout.At(loadout.IndexOf(card.EvolvesFromId)).Level,
                                    service.TomeLevel(card.EvolvesFromId == "weapon_sword" ? "tome_force"
                                        : card.EvolvesFromId == "weapon_staff" ? "tome_reach" : "tome_precision")),
                                    Is.True, $"{where}: offered before it unlocked");
                                break;
                            case OfferCategory.WeaponUpgrade:
                                Assert.That(service.StacksOf(card), Is.InRange(1, WeaponLoadout.MaxLevel - 1), where);
                                break;
                            case OfferCategory.NewTome:
                                Assert.That(service.StacksOf(card), Is.Zero, where);
                                Assert.That(service.TomesHeld, Is.LessThan(UpgradeService.MaxTomes), where);
                                break;
                            default:
                                Assert.That(service.StacksOf(card), Is.InRange(1, UpgradeService.MaxTomeLevel - 1), where);
                                break;
                        }
                    }
                });
                Assert.That(offers, Is.GreaterThan(40), $"seed {seed}: the catalogue should take dozens of picks to fill");
                Assert.That(loadout.Count, Is.LessThanOrEqualTo(WeaponLoadout.MaxSlots));
                Assert.That(service.TomesHeld, Is.LessThanOrEqualTo(UpgradeService.MaxTomes));
            }
        }

        [Test]
        public void TheSameSeedShowsTheSameOffers()
        {
            List<string> Record(int seed)
            {
                UpgradeService service = Service(seed, out _, out RunState run);
                Grant(run, 3);
                // Past the late threshold for the second half, so both phases are replayed.
                run.AddExperience(200);
                var seen = new List<string>();
                Drive(service, offer => offer.Index % offer.Choices.Count, offer => seen.Add(Describe(offer)));
                return seen;
            }

            Assert.That(Record(4242), Is.EqualTo(Record(4242)));
            Assert.That(Record(4242), Is.Not.EqualTo(Record(4243)));
        }

        [Test]
        public void TheGeneralCardsInCategoryModeAreTheHerosOnly()
        {
            var offered = new HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
            {
                UpgradeService service = Service(seed, out _, out RunState run);
                Grant(run, AllPending);
                // Past EvolutionRule.MinimumLevel, so Ironblood can follow a full Vital Surge.
                run.AddExperience(400);
                Drive(service, offer => SlotOf(offer, OfferCategory.General), offer =>
                {
                    foreach (UpgradeOption card in offer.Choices)
                        if (card.Category == OfferCategory.General)
                            offered.Add(card.Id);
                });
            }

            // Deep Lungs stays out while the movement budget is off, as it does in the legacy engine.
            Assert.That(offered, Is.EquivalentTo(new[]
            {
                "upgrade_health", "upgrade_armor", "upgrade_move_speed", "upgrade_luck", "upgrade_health_evolved"
            }));
        }

        [Test]
        public void OneForceLevelRaisesEveryWeaponByTheSameShareAndAWeaponPickedUpLaterReceivesIt()
        {
            // Sword and Staff held, the Daggers to pick up, one tome at one magnitude so the tier drawn cannot matter.
            var force = new UpgradeOption("tome_force", "Force", "+{0:0}%", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.08f), 8);
            WeaponCard[] cards = SimCards();
            WeaponRuntime staff = cards[1].CreateRuntime();
            UpgradeService service = Service(5, out WeaponLoadout loadout, out RunState run,
                cards: new[] { cards[2] }, tomes: new[] { force }, pool: new UpgradeOption[0],
                alsoHeld: new[] { staff }, alsoHeldIds: new[] { "weapon_staff" });
            WeaponCard acquired = null;
            WeaponRuntime acquiredRuntime = null;
            service.WeaponAcquired += (card, runtime) =>
            {
                acquired = card;
                acquiredRuntime = runtime;
            };
            Grant(run, 2);

            UpgradeOffer first = service.CurrentOffer;
            Assert.That(first.Choices.Count, Is.EqualTo(2), "Two cards are left, so both are shown.");
            Assert.That(service.TrySelect(first, SlotOf(first, OfferCategory.NewTome, -1)), Is.True);
            Assert.That(loadout.Primary.Damage / 18f, Is.EqualTo(1.08f).Within(1e-5f), "On the category base (18).");
            Assert.That(staff.Damage / 24f, Is.EqualTo(1.08f).Within(1e-5f), "The same share on the second weapon.");

            UpgradeOffer second = service.CurrentOffer;
            Assert.That(service.TrySelect(second, SlotOf(second, OfferCategory.NewWeapon, -1)), Is.True);
            Assert.That(loadout.Count, Is.EqualTo(3));
            Assert.That(acquired, Is.SameAs(cards[2]));
            Assert.That(acquiredRuntime, Is.SameAs(loadout.At(2).Runtime), "The event hands over the runtime the slot holds.");
            Assert.That(acquiredRuntime.Damage, Is.EqualTo(7f * 1.08f).Within(1e-4f),
                "A weapon picked up after Force carries it from its first strike.");
            Assert.That(acquiredRuntime.Seed, Is.EqualTo(WeaponLoadout.SeedFor(5, 2)), "Seeded for its slot by the loadout.");
        }

        [Test]
        public void AWeaponsOwnDamageAddsUpBeforeForceMultipliesIt()
        {
            // (d0 + flat) * (1 + Force): the report's per-weapon first, global bucket second (40 section 5.3).
            var edge = new UpgradeOption("line_test_damage", "Sword", "+{0:0}", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Flat, 2f), 7);
            var force = new UpgradeOption("tome_force", "Force", "+{0:0}%", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.08f), 8);
            var sword = new WeaponCard("weapon_sword", "Sword", string.Empty,
                () => DescentSimulation.Sword().CreateRuntime(), new[] { edge });
            UpgradeService service = Service(9, out WeaponLoadout loadout, out RunState run, cards: new[] { sword },
                tomes: new[] { force }, pool: new UpgradeOption[0]);
            Grant(run, 4);

            void Take(OfferCategory category)
            {
                UpgradeOffer offer = service.CurrentOffer;
                int slot = SlotOf(offer, category, -1);
                Assert.That(slot, Is.GreaterThanOrEqualTo(0), $"{category} offered");
                Assert.That(service.TrySelect(offer, slot), Is.True);
            }

            Take(OfferCategory.WeaponUpgrade);
            Take(OfferCategory.NewTome);
            Assert.That(loadout.Primary.Damage, Is.EqualTo(21.6f).Within(1e-4f), "(18 + 2) * 1.08, on the category base");
            Take(OfferCategory.TomeUpgrade);
            Assert.That(loadout.Primary.Damage, Is.EqualTo(23.2f).Within(1e-4f),
                "(18 + 2) * 1.16: two Force levels add, they do not compound to 1.1664");
            Take(OfferCategory.WeaponUpgrade);
            Assert.That(loadout.Primary.Damage, Is.EqualTo(25.52f).Within(1e-4f), "(18 + 4) * 1.16");
            Assert.That(loadout.Primary.Damage, Is.EqualTo((18f + 2f + 2f) * (1f + 0.08f + 0.08f)).Within(1e-4f));
            Assert.That(loadout.At(0).Level, Is.EqualTo(3));
            Assert.That(service.TomeLevel("tome_force"), Is.EqualTo(2));
        }

        [Test]
        public void AWeaponUpgradeWritesToItsOwnWeaponOnly()
        {
            WeaponCard[] cards = SimCards();
            WeaponRuntime staff = cards[1].CreateRuntime();
            UpgradeService service = Service(2, out WeaponLoadout loadout, out RunState run,
                cards: new[] { cards[1] }, tomes: new UpgradeOption[0], pool: new UpgradeOption[0],
                alsoHeld: new[] { staff }, alsoHeldIds: new[] { "weapon_staff" });
            Grant(run, 7);

            Drive(service, offer => 0, offer => Assert.That(offer.Choices[0].Id, Does.StartWith("line_staff_")));

            Assert.That(loadout.At(1).Level, Is.EqualTo(WeaponLoadout.MaxLevel));
            Assert.That(loadout.Primary.Damage, Is.EqualTo(18f), "The Sword is not in the catalogue and gains nothing.");
            Assert.That(loadout.Primary.Area, Is.EqualTo(1f));
            Assert.That(loadout.Primary.Interval, Is.EqualTo(0.8f));
            Assert.That(staff.Damage > 24f || staff.Area > 1f || staff.Interval < 1.1f, Is.True);
        }

        [Test]
        public void ACatalogueWithoutALoadoutIsRefusedAndALegacyServiceDrawsNoCategory()
        {
            var run = new RunState(10, 0.5f, 2, 1);
            WeaponRuntime weapon = DescentSimulation.Sword().CreateRuntime();
            Assert.Throws<ArgumentException>(() => new UpgradeService(run, weapon, DescentSimulation.Pool(), 3,
                tomes: DescentSimulation.Tomes()));

            var legacy = new UpgradeService(run, weapon, DescentSimulation.Pool(), 3);
            Assert.That(legacy.IsCategoryMode, Is.False);
            Grant(run, 5);
            Drive(legacy, offer => 0, offer =>
            {
                foreach (UpgradeOption card in offer.Choices)
                    Assert.That(card.Category, Is.EqualTo(OfferCategory.General));
            });
        }

        [Test]
        public void TwoCardsWithOneIdAreRefused()
        {
            var run = new RunState(10, 0.5f, 2, 1);
            var loadout = new WeaponLoadout(1);
            WeaponRuntime weapon = DescentSimulation.Sword().CreateRuntime();
            loadout.TryAdd("weapon_sword", weapon);
            var clash = new UpgradeOption("upgrade_health", "Clash", "+{0:0}%", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.1f), 8);

            Assert.Throws<ArgumentException>(() => new UpgradeService(run, weapon, DescentSimulation.Pool(), 3,
                loadout: loadout, weaponCards: SimCards(), tomes: new[] { clash }));
            Assert.Throws<ArgumentException>(() => new UpgradeService(run, DescentSimulation.Sword().CreateRuntime(),
                DescentSimulation.Pool(), 3, loadout: loadout, weaponCards: SimCards(), tomes: DescentSimulation.Tomes()),
                "The weapon handed in is the loadout's primary.");
        }

        // The rule docs/42 sets for the report's 5.3: a line on Damage is Flat, or it would fall into the global bucket
        // with Force. WeaponContentTests holds the assets to the same rule and to these numbers.
        [Test]
        public void EveryWeaponLineOnDamageIsFlat()
        {
            foreach (WeaponCard card in SimCards())
            {
                Assert.That(card.Lines.Count, Is.EqualTo(3), card.Id);
                foreach (UpgradeOption line in card.Lines)
                {
                    Assert.That(line.Category, Is.EqualTo(OfferCategory.WeaponUpgrade), line.Id);
                    if (line.Stat == UpgradeStat.Damage)
                        Assert.That(line.Modifier.Operation, Is.EqualTo(ModifierOperation.Flat), line.Id);
                }
            }
        }

        // The tomes and the weapon lines use the report's tier multipliers exactly (docs/42, owner decisions).
        [Test]
        public void TomesAndWeaponLinesScaleByTheReportsMultipliers()
        {
            var cards = new List<UpgradeOption>(DescentSimulation.Tomes());
            foreach (WeaponCard card in SimCards())
                cards.AddRange(card.Lines);

            foreach (UpgradeOption card in cards)
            {
                float green = card.Modifier.Amount;
                foreach (UpgradeRarity rarity in new[] { UpgradeRarity.Rare, UpgradeRarity.Epic, UpgradeRarity.Gold })
                    Assert.That(card.ModifierFor(rarity).Amount, Is.EqualTo(green * RarityTable.Multiplier(rarity)).Within(1e-6f),
                        $"{card.Id} at {rarity}");
            }
            foreach (UpgradeOption tome in DescentSimulation.Tomes())
                Assert.That(tome.MaxStacks, Is.EqualTo(8), tome.Id);
        }

        // A whole Descent in category mode, every starting weapon, over seeds: within its limits, and a seed replays its
        // offers and picks exactly (CardPicker.Taken / ReplayMisses). The hero kites and fetches its drops, as the seed
        // cohort does (DescentSeedTests): a hero standing still levels only when a floor's drops are swept to it.
        [Test]
        public void ACategoryDescentStaysWithinItsLimitsAndReplays()
        {
            DescentSimulation.Floor[] descent = { DescentSimulation.EmberHalls, DescentSimulation.QuicksilverVaults };
            DescentSimulation.Result Play(Func<DescentSimulation.HeroWeapon> start, int seed, string[] replay) =>
                DescentSimulation.Run(descent, 0, true, null, start(), ability: DescentSimulation.ForgeBurst(),
                    route: new FetchRoute(new KiteRoute()), seed: seed, choiceCount: 3, replay: replay,
                    weaponCatalogue: DescentSimulation.WeaponCatalogue(), tomes: DescentSimulation.Tomes());

            foreach (Func<DescentSimulation.HeroWeapon> start in new Func<DescentSimulation.HeroWeapon>[]
                { DescentSimulation.Sword, DescentSimulation.Staff, DescentSimulation.Daggers })
            {
                for (int seed = 0; seed < 4; seed++)
                {
                    DescentSimulation.Result result = Play(start, seed, null);
                    string where = $"{start().Id}, seed {seed}: took {string.Join(",", result.Picks)} at level " +
                        $"{result.Level}, cleared {result.ClearedFloors}, died in {result.DeathRoom}";
                    TestContext.WriteLine($"{where}; {result.WeaponsHeld} weapons (best Lv{result.HighestWeaponLevel}), " +
                        $"{result.TomesHeld} tomes (best Lv{result.HighestTomeLevel})");
                    Assert.That(result.UpgradesApplied, Is.GreaterThan(3), where);
                    Assert.That(result.Picks.Length, Is.EqualTo(result.UpgradesApplied), where);
                    Assert.That(result.WeaponsHeld, Is.InRange(1, WeaponLoadout.MaxSlots), where);
                    Assert.That(result.TomesHeld, Is.InRange(0, UpgradeService.MaxTomes), where);
                    Assert.That(result.HighestWeaponLevel, Is.InRange(1, WeaponLoadout.MaxLevel), where);
                    Assert.That(result.HighestTomeLevel, Is.InRange(0, UpgradeService.MaxTomeLevel), where);
                    Assert.That(result.WeaponsHeld + result.TomesHeld, Is.GreaterThan(1),
                        $"{where}: the policy picks something up over a whole Descent");

                    DescentSimulation.Result again = Play(start, seed, result.Picks);
                    Assert.That(again.ReplayMisses, Is.Zero, where);
                    Assert.That(again.Picks, Is.EqualTo(result.Picks), where);
                    Assert.That(again.HeroHealth, Is.EqualTo(result.HeroHealth), where);
                    Assert.That(again.FightSeconds, Is.EqualTo(result.FightSeconds), where);
                }
            }
        }
    }
}
