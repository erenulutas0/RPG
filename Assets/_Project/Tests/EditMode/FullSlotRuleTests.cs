using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/63 section 4, the full-slot rule (docs/40 section 5.1; the owner, 2026-11-22: "once they have three, only those
    // three appear as weapon options"): once a hero holds three weapons no source hands it a fourth - no level-up's offer,
    // no bonus offer (an altar's, an Outworks anvil's, the forge's: RunState.GrantBonusUpgrade opens the same offer), no
    // chest's book - and every weapon card it is shown is a level of one it holds, while the other kinds fill the offer.
    //
    // The rule is UpgradeService's (WeaponOpen: a new weapon only while a slot is free; TryGrantCategoryBook: never a
    // weapon), the one body the scene's CombatSetup and the simulation's FoundryRun both build. With today's catalogue of
    // three a full loadout holds every weapon there is, so the rule could not be seen to work there
    // (OfferCategoryTests.NoNewWeaponIsOfferedOnceThreeSlotsAreHeld passes on any catalogue of three); these hand the
    // service five - the three and two more under ids of their own - as the weapon roster does (docs/63 section 7).
    public sealed class FullSlotRuleTests
    {
        private const string Fourth = "weapon_test_fourth";
        private const string Fifth = "weapon_test_fifth";

        private static WeaponCard[] FiveCards()
        {
            var cards = new List<WeaponCard>();
            foreach (DescentSimulation.HeroWeapon weapon in DescentSimulation.WeaponCatalogue())
                cards.Add(weapon.CreateCard());
            cards.Add(Extra(Fourth, DescentSimulation.Staff()));
            cards.Add(Extra(Fifth, DescentSimulation.Daggers()));
            return cards.ToArray();
        }

        // A weapon like another under an id of its own, with two lines of its own and no evolution.
        private static WeaponCard Extra(string id, DescentSimulation.HeroWeapon like)
        {
            DescentSimulation.HeroWeapon copy = like.Copy();
            copy.Id = id;
            copy.DisplayName = id;
            copy.Evolution = null;
            copy.EvolvesWithTome = null;
            copy.Lines = new[]
            {
                new UpgradeOption(id + "_damage", id, "+{0:0.##} damage", UpgradeStat.Damage,
                    new StatModifier(ModifierOperation.Flat, 2f), DescentSimulation.LineMaxStacks),
                new UpgradeOption(id + "_attack_speed", id, "+{0:0.#}% attack speed", UpgradeStat.AttackSpeed,
                    new StatModifier(ModifierOperation.Percent, 0.08f), DescentSimulation.LineMaxStacks)
            };
            return copy.CreateCard();
        }

        private static UpgradeService Service(int seed, WeaponCard[] cards, out WeaponLoadout loadout, out RunState run)
        {
            run = new RunState(10, 0.5f, 2, seed);
            loadout = new WeaponLoadout(seed);
            WeaponCard start = cards[seed % 3];
            WeaponRuntime primary = start.CreateRuntime();
            loadout.TryAdd(start.Id, primary);
            return new UpgradeService(run, primary, DescentSimulation.Pool(), 3,
                new HeroStats(100f, 2.5f, HeroStamina.DefaultBar), false, loadout, cards, DescentSimulation.Tomes());
        }

        // The weapon each weapon card stands for: its Acquire, its lines and its evolution.
        private static Dictionary<string, string> WeaponOfCard(WeaponCard[] cards)
        {
            var map = new Dictionary<string, string>();
            foreach (WeaponCard card in cards)
            {
                map[card.Acquire.Id] = card.Id;
                foreach (UpgradeOption line in card.Lines)
                    map[line.Id] = card.Id;
                if (card.Evolution != null)
                    map[card.Evolution.Id] = card.Id;
            }
            return map;
        }

        private static int SlotOf(UpgradeOffer offer, OfferCategory category)
        {
            for (int i = 0; i < offer.Choices.Count; i++)
                if (offer.Choices[i].Category == category)
                    return i;
            return 0;
        }

        // Level-ups earned by experience and bonus offers (the altars', the anvils', the forge's), answered in turn: the hero
        // takes a new weapon whenever one is shown, so it fills its three slots as early as the offers allow.
        [Test]
        public void OnceThreeWeaponsAreHeldNoOfferShowsAFourth()
        {
            WeaponCard[] cards = FiveCards();
            Dictionary<string, string> weaponOf = WeaponOfCard(cards);
            var taken = new HashSet<string>();
            for (int seed = 0; seed < 30; seed++)
            {
                UpgradeService service = Service(seed, cards, out WeaponLoadout loadout, out RunState run);
                int acquired = 0;
                service.WeaponAcquired += (card, runtime) => acquired++;
                int offersWhileFull = 0;
                for (int round = 0; round < 60; round++)
                {
                    if (round % 2 == 0)
                        run.AddExperience(run.ExperienceForNextLevel - run.Experience);
                    else
                        run.GrantBonusUpgrade();
                    while (service.CurrentOffer != null)
                    {
                        UpgradeOffer offer = service.CurrentOffer;
                        bool full = loadout.IsFull;
                        foreach (UpgradeOption card in offer.Choices)
                        {
                            string where = $"seed {seed}, offer {offer.Index}, {card.Id}";
                            if (card.Category == OfferCategory.NewWeapon)
                            {
                                Assert.That(full, Is.False, $"{where}: a fourth weapon offered with every slot held");
                                Assert.That(loadout.Owns(card.Id), Is.False, where);
                            }
                            else if (card.Category == OfferCategory.WeaponUpgrade)
                            {
                                Assert.That(loadout.Owns(weaponOf[card.Id]), Is.True, $"{where}: a level of a weapon not held");
                            }
                        }
                        if (full)
                            offersWhileFull++;
                        Assert.That(service.TrySelect(offer, SlotOf(offer, OfferCategory.NewWeapon)), Is.True, $"seed {seed}");
                    }
                }
                Assert.That(loadout.Count, Is.EqualTo(WeaponLoadout.MaxSlots), $"seed {seed}: three held");
                Assert.That(acquired, Is.EqualTo(WeaponLoadout.MaxSlots - 1), $"seed {seed}: two picked up, never a third");
                Assert.That(offersWhileFull, Is.GreaterThan(20), $"seed {seed}: the rule was met many times over");
                for (int slot = 0; slot < loadout.Count; slot++)
                    taken.Add(loadout.At(slot).WeaponId);
            }
            Assert.That(taken.Contains(Fourth) && taken.Contains(Fifth), Is.True,
                "The two weapons past today's three were offered and taken: the full loadout had others left to refuse.");
        }

        // A closed kind leaves its share to the others (OfferWeights): with three weapons held the offers still show three
        // cards while three other cards are open - levels of the weapons held, tomes and the hero's own cards.
        [Test]
        public void AFullLoadoutsOffersAreFilledByTheOtherKinds()
        {
            WeaponCard[] cards = FiveCards();
            for (int seed = 0; seed < 30; seed++)
            {
                UpgradeService service = Service(seed, cards, out WeaponLoadout loadout, out RunState run);
                var kinds = new HashSet<OfferCategory>();
                int fullOffers = 0;
                for (int round = 0; round < 24; round++)
                {
                    run.GrantBonusUpgrade();
                    while (service.CurrentOffer != null)
                    {
                        UpgradeOffer offer = service.CurrentOffer;
                        if (loadout.IsFull)
                        {
                            fullOffers++;
                            Assert.That(offer.Choices.Count, Is.EqualTo(3), $"seed {seed}, offer {offer.Index}: a full offer");
                            foreach (UpgradeOption card in offer.Choices)
                                kinds.Add(card.Category);
                        }
                        Assert.That(service.TrySelect(offer, SlotOf(offer, OfferCategory.NewWeapon)), Is.True);
                    }
                }
                Assert.That(fullOffers, Is.GreaterThan(0), $"seed {seed}");
                Assert.That(kinds.Contains(OfferCategory.NewWeapon), Is.False, $"seed {seed}");
                Assert.That(kinds.Contains(OfferCategory.WeaponUpgrade), Is.True, $"seed {seed}: the weapons held still level");
            }
        }

        // A chest's book is a tome level or a hero card, never a weapon, before the slots fill and after (docs/42, the
        // owner's decision of 2026-10-26): no chest is a fourth weapon's way in either.
        [Test]
        public void AChestsBookNeverHandsAWeaponBeforeOrAfterTheSlotsFill()
        {
            WeaponCard[] cards = FiveCards();
            for (int seed = 0; seed < 10; seed++)
            {
                UpgradeService service = Service(seed, cards, out WeaponLoadout loadout, out RunState run);
                int acquired = 0;
                service.WeaponAcquired += (card, runtime) => acquired++;
                int chest = 0;
                for (int round = 0; round < 30; round++)
                {
                    run.GrantBonusUpgrade();
                    while (service.CurrentOffer != null)
                        Assert.That(service.TrySelect(service.CurrentOffer, SlotOf(service.CurrentOffer, OfferCategory.NewWeapon)), Is.True);
                    int held = loadout.Count;
                    int pickedUp = acquired;
                    for (int i = 0; i < 4; i++)
                    {
                        if (!service.TryGrantBook(chest++, out UpgradeOption book, out UpgradeRarity rarity))
                            continue;
                        Assert.That(book.Category == OfferCategory.General || book.Category == OfferCategory.TomeUpgrade, Is.True,
                            $"seed {seed}, chest {chest}: {book.Id} is {book.Category}");
                    }
                    Assert.That(loadout.Count, Is.EqualTo(held), $"seed {seed}: no book filled a slot");
                    Assert.That(acquired, Is.EqualTo(pickedUp), $"seed {seed}");
                }
                Assert.That(loadout.IsFull, Is.True, $"seed {seed}: the slots filled by the offers alone");
            }
        }
    }
}
