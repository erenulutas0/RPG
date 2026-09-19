using System;
using Cryptforge.Progression;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // RunRandom is the run's only source of randomness and it has to give the same answer on every runtime the game
    // runs on, so its outputs are pinned here as numbers rather than as properties. If a runtime ever disagreed with
    // these, offers would differ between the scene and the simulation, and this is where it would show first.
    public sealed class RunRandomTests
    {
        [Test]
        public void TheSameStreamAlwaysYieldsTheSameWords()
        {
            RunRandom a = RunRandom.Stream(1234, RunRandom.Offers, 2);
            RunRandom b = RunRandom.Stream(1234, RunRandom.Offers, 2);
            for (int i = 0; i < 100; i++)
                Assert.That(b.Next(), Is.EqualTo(a.Next()));
        }

        [TestCase(0, 1, 0, 294308533u, 2766565509u, 1810209440u, 2247137005u)]
        [TestCase(1234, 1, 2, 1379939492u, 2758573998u, 2936762428u, 1839588541u)]
        [TestCase(-7, 2, 5, 759754808u, 1691926830u, 495795843u, 4231461121u)]
        public void KnownStreamsYieldKnownWords(int seed, int purpose, int index, uint first, uint second, uint third, uint fourth)
        {
            RunRandom stream = RunRandom.Stream(seed, purpose, index);
            Assert.That(new[] { stream.Next(), stream.Next(), stream.Next(), stream.Next() },
                Is.EqualTo(new[] { first, second, third, fourth }));
        }

        [Test]
        public void KnownDrawsBelowSixAreKnown()
        {
            RunRandom stream = RunRandom.Stream(1234, RunRandom.Offers, 0);
            var draws = new int[6];
            for (int i = 0; i < draws.Length; i++)
                draws[i] = stream.NextBelow(6);
            Assert.That(draws, Is.EqualTo(new[] { 0, 1, 4, 2, 2, 2 }));
        }

        [Test]
        public void StreamsAreTornApartByPurposeAndByIndex()
        {
            uint offers = RunRandom.Stream(42, RunRandom.Offers, 0).Next();
            uint chests = RunRandom.Stream(42, RunRandom.Chests, 0).Next();
            uint later = RunRandom.Stream(42, RunRandom.Offers, 1).Next();
            uint otherSeed = RunRandom.Stream(43, RunRandom.Offers, 0).Next();
            Assert.That(offers, Is.Not.EqualTo(chests));
            Assert.That(offers, Is.Not.EqualTo(later));
            Assert.That(offers, Is.Not.EqualTo(otherSeed));
        }

        [Test]
        public void DrawsBelowACountStayInsideItAndSpreadAcrossIt()
        {
            RunRandom stream = RunRandom.Stream(7, RunRandom.Offers, 0);
            var counts = new int[5];
            for (int i = 0; i < 5000; i++)
            {
                int draw = stream.NextBelow(5);
                Assert.That(draw, Is.InRange(0, 4));
                counts[draw]++;
            }
            // 1000 expected per bucket; a tenth either way is far outside anything a fair generator does 5000 times.
            foreach (int count in counts)
                Assert.That(count, Is.InRange(900, 1100));
        }

        [Test]
        public void ACountOfOneNeverDrawsAndBadArgumentsAreRefused()
        {
            RunRandom stream = RunRandom.Stream(7, RunRandom.Offers, 0);
            uint before = RunRandom.Stream(7, RunRandom.Offers, 0).Next();
            Assert.That(stream.NextBelow(1), Is.Zero);
            Assert.That(stream.Next(), Is.EqualTo(before), "A draw below one consumes nothing.");
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.NextBelow(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => RunRandom.Stream(1, RunRandom.Offers, -1));
        }
    }
}
