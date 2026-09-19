using System.Collections.Generic;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // The offer engine of docs/27 section 6: which cards a level-up shows is a function of the run's seed, the offer's
    // index and the eligible pool, and nothing else. A pool no larger than the choice count is offered whole in pool
    // order and consumes no randomness, which is why the two-card game of today reproduces every pinned number.
    public sealed class OfferEngineTests
    {
        private static UpgradeOption Card(string id, int maxStacks = 5, string requires = null) =>
            new UpgradeOption(id, id, "+{0:0}", WeaponStat.Damage, new StatModifier(ModifierOperation.Flat, 1f), maxStacks, requires);

        private static UpgradeOption[] Five() =>
            new[] { Card("a"), Card("b"), Card("c"), Card("d"), Card("e") };

        private static UpgradeService Service(int seed, UpgradeOption[] pool, int choices = 2, int experience = 90)
        {
            // Thresholds n(n + 9): 90 experience is six levels, so six offers wait to be taken one after another.
            var run = new RunState(10, 0.5f, 2, seed);
            var service = new UpgradeService(run, new WeaponRuntime(10f, 1f, 2f), pool, choices);
            run.AddExperience(experience);
            return service;
        }

        // Takes the first card of every offer and records what each offer showed.
        private static List<string> TakeAll(UpgradeService service)
        {
            var seen = new List<string>();
            while (service.CurrentOffer != null)
            {
                seen.Add(Ids(service.CurrentOffer));
                Assert.That(service.TrySelect(service.CurrentOffer, 0), Is.True);
            }
            return seen;
        }

        private static string Ids(UpgradeOffer offer)
        {
            var ids = new List<string>();
            foreach (UpgradeOption option in offer.Choices)
                ids.Add(option.Id);
            return string.Join(",", ids);
        }

        [Test]
        public void TheSameSeedShowsTheSameCardsAtEveryLevel()
        {
            Assert.That(TakeAll(Service(1234, Five())), Is.EqualTo(TakeAll(Service(1234, Five()))));
            Assert.That(TakeAll(Service(1234, Five())), Has.Count.EqualTo(6));
        }

        [Test]
        public void DifferentSeedsShowDifferentCards()
        {
            List<string> one = TakeAll(Service(1, Five()));
            List<string> two = TakeAll(Service(2, Five()));
            Assert.That(one, Is.Not.EqualTo(two));
        }

        [Test]
        public void AnOfferNeverRepeatsACardAndOnlyShowsEligibleOnes()
        {
            for (int seed = 0; seed < 40; seed++)
            {
                UpgradeService service = Service(seed, Five());
                while (service.CurrentOffer != null)
                {
                    UpgradeOffer offer = service.CurrentOffer;
                    Assert.That(offer.Choices.Count, Is.EqualTo(2), $"seed {seed}");
                    Assert.That(offer.Choices[0], Is.Not.SameAs(offer.Choices[1]), $"seed {seed}: a card offered twice");
                    foreach (UpgradeOption option in offer.Choices)
                        Assert.That(service.StacksOf(option), Is.LessThan(option.MaxStacks), $"seed {seed}: a maxed card offered");
                    service.TrySelect(offer, seed % 2);
                }
            }
        }

        [Test]
        public void AMaxedCardLeavesThePoolAndAPrerequisiteGatesItsDependant()
        {
            // "b" needs one stack of "a" and "a" takes only one; with three cards and two choices, every offer must draw.
            var pool = new[] { Card("a", 1), Card("b", 5, "a"), Card("c", 5) };
            for (int seed = 0; seed < 20; seed++)
            {
                UpgradeService service = Service(seed, pool);
                Assert.That(Ids(service.CurrentOffer), Is.EqualTo("a,c").Or.EqualTo("c,a"),
                    $"seed {seed}: before any stack of a, b is not eligible and the two eligible cards come in pool order");
                bool tookA = false;
                while (service.CurrentOffer != null)
                {
                    UpgradeOffer offer = service.CurrentOffer;
                    foreach (UpgradeOption option in offer.Choices)
                    {
                        if (option.Id == "b")
                            Assert.That(tookA, Is.True, $"seed {seed}: b offered before a was taken");
                        if (option.Id == "a")
                            Assert.That(tookA, Is.False, $"seed {seed}: a offered after its single stack");
                    }
                    int slot = 0;
                    for (int i = 0; i < offer.Choices.Count; i++)
                        if (offer.Choices[i].Id == "a") slot = i;
                    if (offer.Choices[slot].Id == "a") tookA = true;
                    service.TrySelect(offer, slot);
                }
            }
        }

        [Test]
        public void APoolNoLargerThanTheChoiceCountIsOfferedWholeInPoolOrderWhateverTheSeed()
        {
            var pool = new[] { Card("damage"), Card("speed") };
            for (int seed = -3; seed < 3; seed++)
            {
                UpgradeService service = Service(seed * 7919, pool);
                Assert.That(Ids(service.CurrentOffer), Is.EqualTo("damage,speed"), $"seed {seed}");
                Assert.That(service.CurrentOffer.Index, Is.Zero);
                service.TrySelect(service.CurrentOffer, 1);
                Assert.That(Ids(service.CurrentOffer), Is.EqualTo("damage,speed"));
                Assert.That(service.CurrentOffer.Index, Is.EqualTo(1), "Offers are counted even when nothing was drawn.");
            }
        }

        [Test]
        public void ADrawOnTheChestStreamBetweenTwoOffersMovesNeither()
        {
            UpgradeService plain = Service(99, Five());
            UpgradeService interleaved = Service(99, Five());
            var seenPlain = new List<string>();
            var seenInterleaved = new List<string>();
            while (plain.CurrentOffer != null)
            {
                seenPlain.Add(Ids(plain.CurrentOffer));
                plain.TrySelect(plain.CurrentOffer, 0);

                seenInterleaved.Add(Ids(interleaved.CurrentOffer));
                // A chest opened here draws from its own stream; the next offer must not notice.
                RunRandom.Stream(99, RunRandom.Chests, seenInterleaved.Count).NextBelow(1000);
                interleaved.TrySelect(interleaved.CurrentOffer, 0);
            }
            Assert.That(seenInterleaved, Is.EqualTo(seenPlain));
        }

        [Test]
        public void TheSelectedOfferIsRememberedForWhoeverReportsIt()
        {
            UpgradeService service = Service(5, Five());
            UpgradeOffer first = service.CurrentOffer;
            Assert.That(first.Index, Is.Zero);
            service.TrySelect(first, 1);
            Assert.That(service.LastSelected, Is.SameAs(first));
            Assert.That(service.CurrentOffer.Index, Is.EqualTo(1));
        }

        [Test]
        public void ACardCannotRequireItself()
        {
            Assert.Throws<System.ArgumentException>(() => Card("a", 5, "a"));
        }
    }
}
