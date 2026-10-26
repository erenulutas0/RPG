using System;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Chest books in category mode (docs/42, PR 6; owner decision): a level of a tome already held, or a general card,
    // drawn on the books stream at the chest's ordinal. Never a weapon, never a weapon's level or evolution, and never a
    // new tome, so no tome slot is filled by a chest. The legacy book is UpgradeTests' and HeroStatsTests', which never
    // pass a loadout.
    public sealed class CategoryBookTests
    {
        private static WeaponCard[] SimCards()
        {
            DescentSimulation.HeroWeapon[] weapons = DescentSimulation.WeaponCatalogue();
            var cards = new WeaponCard[weapons.Length];
            for (int i = 0; i < weapons.Length; i++)
                cards[i] = weapons[i].CreateCard();
            return cards;
        }

        private static UpgradeService Service(int seed, out WeaponLoadout loadout, out RunState run,
            out HeroStats heroStats, UpgradeOption[] tomes = null, UpgradeOption[] pool = null, WeaponCard[] cards = null)
        {
            run = new RunState(10, 0.5f, 2, seed);
            loadout = new WeaponLoadout(seed);
            WeaponRuntime primary = DescentSimulation.Sword().CreateRuntime();
            loadout.TryAdd("weapon_sword", primary);
            heroStats = new HeroStats(100f, 2.5f, HeroStamina.DefaultBar);
            return new UpgradeService(run, primary, pool ?? DescentSimulation.Pool(), 3, heroStats, false, loadout,
                cards ?? SimCards(), tomes ?? DescentSimulation.Tomes());
        }

        // Answers level-ups, granting them as needed, until one shows a new tome, and takes it: the id of the tome now
        // held at level 1.
        private static string HoldTome(UpgradeService service, RunState run)
        {
            for (int tries = 0; tries < 40; tries++)
            {
                if (service.CurrentOffer == null)
                    run.GrantBonusUpgrade();
                UpgradeOffer offer = service.CurrentOffer;
                for (int i = 0; i < offer.Choices.Count; i++)
                {
                    if (offer.Choices[i].Category != OfferCategory.NewTome)
                        continue;
                    string id = offer.Choices[i].Id;
                    Assert.That(service.TrySelect(offer, i), Is.True);
                    return id;
                }
                Assert.That(service.TrySelect(offer, 0), Is.True);
            }
            Assert.Fail("No new tome was offered.");
            return null;
        }

        private static void Answer(UpgradeService service, int picks, Func<UpgradeOffer, int> choose)
        {
            for (int i = 0; i < picks && service.CurrentOffer != null; i++)
            {
                UpgradeOffer offer = service.CurrentOffer;
                Assert.That(service.TrySelect(offer, choose(offer)), Is.True);
            }
        }

        private static int[] Levels(WeaponLoadout loadout)
        {
            var levels = new int[loadout.Count];
            for (int i = 0; i < levels.Length; i++)
                levels[i] = loadout.At(i).Level;
            return levels;
        }

        // Over many seeds and many build states - from nothing picked to every level-up answered - every book is a held
        // tome's level or a general card, and no book moves a weapon slot, a weapon level, an evolution or the tome count.
        [Test]
        public void AChestNeverHandsAWeaponOrANewTome()
        {
            int tomeBooks = 0;
            int generalBooks = 0;
            for (int seed = 0; seed < 300; seed++)
            {
                UpgradeService service = Service(seed, out WeaponLoadout loadout, out RunState run, out _);
                for (int i = 0; i < seed % 40; i++)
                    run.GrantBonusUpgrade();
                run.AddExperience(seed % 7 * 30);
                Answer(service, seed % 40, offer => (seed + offer.Index) % offer.Choices.Count);

                for (int chest = 0; chest < 12; chest++)
                {
                    int weapons = loadout.Count;
                    int[] levels = Levels(loadout);
                    int tomesHeld = service.TomesHeld;
                    int evolutions = service.EvolutionsTaken;
                    string where = $"seed {seed}, chest {chest}";
                    if (!service.TryGrantBook(chest, out UpgradeOption book, out _))
                        continue;

                    Assert.That(book.Category, Is.EqualTo(OfferCategory.General).Or.EqualTo(OfferCategory.TomeUpgrade),
                        $"{where}: {book.Id} from a chest");
                    Assert.That(book.Id, Does.Not.StartWith("weapon_").And.Not.StartWith("line_").And.Not.StartWith("evolution_"),
                        where);
                    if (book.Category == OfferCategory.TomeUpgrade)
                        tomeBooks++;
                    else
                    {
                        generalBooks++;
                        Assert.That(WeaponRuntime.Owns(book.Stat), Is.False, $"{where}: a weapon-stat general card");
                    }
                    Assert.That(loadout.Count, Is.EqualTo(weapons), $"{where}: a chest handed a weapon");
                    Assert.That(Levels(loadout), Is.EqualTo(levels), $"{where}: a chest levelled a weapon");
                    Assert.That(service.TomesHeld, Is.EqualTo(tomesHeld), $"{where}: a chest filled a tome slot");
                    if (book.Category == OfferCategory.TomeUpgrade)
                        Assert.That(service.EvolutionsTaken, Is.EqualTo(evolutions), where);
                }
            }
            Assert.That(tomeBooks, Is.GreaterThan(100), "Held tomes are levelled by books.");
            Assert.That(generalBooks, Is.GreaterThan(100), "General cards are granted by books.");
        }

        [Test]
        public void WithNoTomeHeldABookIsAGeneralCard()
        {
            for (int seed = 0; seed < 50; seed++)
            {
                UpgradeService service = Service(seed, out _, out _, out _);
                for (int chest = 0; chest < 5; chest++)
                {
                    Assert.That(service.TryGrantBook(chest, out UpgradeOption book, out _), Is.True, $"seed {seed}");
                    Assert.That(book.Category, Is.EqualTo(OfferCategory.General), $"seed {seed}, chest {chest}");
                }
                Assert.That(service.TomesHeld, Is.Zero);
                Assert.That(service.BooksGranted, Is.EqualTo(5));
            }
        }

        // One tome held, no general cards: books take it to its last level and no further, never open a second tome
        // though three more are in the catalogue, and then grant nothing.
        [Test]
        public void BooksRespectATomesLastLevelAndTheTomeCount()
        {
            UpgradeService service = Service(8, out WeaponLoadout loadout, out RunState run, out _,
                pool: new UpgradeOption[0], cards: new WeaponCard[0]);
            string held = HoldTome(service, run);
            Assert.That(service.TomeLevel(held), Is.EqualTo(1));

            int chest = 0;
            while (service.TryGrantBook(chest, out UpgradeOption book, out _))
            {
                Assert.That(book.Id, Is.EqualTo(held), $"chest {chest}");
                Assert.That(book.Category, Is.EqualTo(OfferCategory.TomeUpgrade), $"chest {chest}");
                Assert.That(service.TomeLevel(held), Is.LessThanOrEqualTo(UpgradeService.MaxTomeLevel));
                chest++;
                Assert.That(chest, Is.LessThan(50), "Books never stop");
            }
            Assert.That(chest, Is.EqualTo(UpgradeService.MaxTomeLevel - 1), "Seven books from level 1 to level 8.");
            Assert.That(service.TomeLevel(held), Is.EqualTo(UpgradeService.MaxTomeLevel));
            Assert.That(service.TomesHeld, Is.EqualTo(1));
            Assert.That(service.BooksGranted, Is.EqualTo(UpgradeService.MaxTomeLevel - 1));
            Assert.That(run.UpgradesApplied, Is.EqualTo(1), "A book answers no level-up.");
            Assert.That(loadout.Count, Is.EqualTo(1));
        }

        // A tome book is a tome level like any other: it reaches every weapon carried, the same share on each.
        [Test]
        public void ATomeBookReachesEveryWeaponCarried()
        {
            var force = new UpgradeOption("tome_force", "Force", "+{0:0}%", UpgradeStat.Damage,
                new StatModifier(ModifierOperation.Percent, 0.08f), 8);
            UpgradeService service = Service(4, out WeaponLoadout loadout, out RunState run, out _,
                tomes: new[] { force }, pool: new UpgradeOption[0], cards: new WeaponCard[0]);
            WeaponRuntime staff = DescentSimulation.Staff().CreateRuntime();
            Assert.That(loadout.TryAdd("weapon_staff", staff), Is.True);
            Assert.That(HoldTome(service, run), Is.EqualTo("tome_force"));

            UpgradeOption granted = null;
            UpgradeRarity grantedTier = UpgradeRarity.Gold;
            service.BookGranted += (book, tier) =>
            {
                granted = book;
                grantedTier = tier;
            };
            Assert.That(service.TryGrantBook(0, out UpgradeOption found, out UpgradeRarity rarity), Is.True);
            Assert.That(found.Id, Is.EqualTo("tome_force"));
            Assert.That(granted, Is.SameAs(found), "BookGranted names the book.");
            Assert.That(grantedTier, Is.EqualTo(rarity));
            Assert.That(service.TomeLevel("tome_force"), Is.EqualTo(2));
            Assert.That(loadout.Primary.Damage / 10f, Is.EqualTo(1.16f).Within(1e-5f));
            Assert.That(staff.Damage / 11f, Is.EqualTo(1.16f).Within(1e-5f));
        }

        // The same chest of the same run holds the same book at the same tier, and opening it moves no offer's draws:
        // a chest uses the books stream alone.
        [Test]
        public void TheSameChestHoldsTheSameBook()
        {
            string Book(int seed, int chest)
            {
                UpgradeService service = Service(seed, out _, out RunState run, out _);
                for (int i = 0; i < 6; i++)
                    run.GrantBonusUpgrade();
                Answer(service, 6, offer => offer.Index % offer.Choices.Count);
                Assert.That(service.TryGrantBook(chest, out UpgradeOption book, out UpgradeRarity rarity), Is.True);
                return $"{book.Id}/{book.Category}/{rarity}";
            }

            for (int seed = 0; seed < 20; seed++)
                for (int chest = 0; chest < 3; chest++)
                    Assert.That(Book(seed, chest), Is.EqualTo(Book(seed, chest)), $"seed {seed}, chest {chest}");
        }

        // Which of the two kinds a book is follows OfferWeights over just those two: level early, five to two for the tome
        // late (10 / 10 and 25 / 10).
        [Test]
        public void ABookLeansTheWayTheLevelUpsDo()
        {
            int Tomes(bool late)
            {
                int tomes = 0;
                for (int chest = 0; chest < 1000; chest++)
                {
                    // A fresh run each chest, holding one tome at level 1 and the general cards open.
                    UpgradeService service = Service(chest, out _, out RunState run, out _,
                        tomes: new[] { DescentSimulation.Force() }, cards: new WeaponCard[0]);
                    if (late)
                        run.AddExperience(400);
                    Assert.That(OfferWeights.IsLate(run.Level), Is.EqualTo(late));
                    HoldTome(service, run);
                    Assert.That(service.TryGrantBook(chest, out UpgradeOption book, out _), Is.True);
                    if (book.Category == OfferCategory.TomeUpgrade)
                        tomes++;
                }
                return tomes;
            }

            // 500 and about 714 of 1000; the bands are here to catch the wrong weights, not to measure them.
            Assert.That(Tomes(false), Is.InRange(440, 560));
            Assert.That(Tomes(true), Is.InRange(660, 770));
        }
    }
}
