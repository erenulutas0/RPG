using System;
using Cryptforge.Core;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class FrameStatsTests
    {
        [Test]
        public void AWindowSummarisesTheTypicalFrameTheTailAndTheMissedDeadlines()
        {
            var stats = new FrameStats(256);
            Assert.That(stats.Percentile(0.5f), Is.Zero, "An empty window has no percentile.");
            Assert.That(stats.AverageSeconds, Is.Zero);

            // 95 frames on time at 60 Hz and 5 that ran to 40 ms, shuffled.
            for (int i = 0; i < 100; i++)
                stats.Add(i % 20 == 7 ? 0.040f : 0.016f);

            Assert.That(stats.Count, Is.EqualTo(100));
            Assert.That(stats.TotalSeconds, Is.EqualTo(95 * 0.016f + 5 * 0.040f).Within(1e-4f));
            Assert.That(stats.AverageSeconds, Is.EqualTo(0.0172f).Within(1e-5f));
            Assert.That(stats.MaxSeconds, Is.EqualTo(0.040f));
            Assert.That(stats.Percentile(0.5f), Is.EqualTo(0.016f), "The typical frame.");
            Assert.That(stats.Percentile(0.95f), Is.EqualTo(0.016f), "The 95th frame of 100 is still on time.");
            Assert.That(stats.Percentile(0.96f), Is.EqualTo(0.040f), "The 96th is the first long one.");
            Assert.That(stats.Percentile(1f), Is.EqualTo(0.040f));
            Assert.That(stats.CountOver(0.020f), Is.EqualTo(5), "Five frames missed a 60 Hz deadline.");
            Assert.That(stats.CountOver(0.040f), Is.Zero, "None ran past 40 ms.");

            stats.Clear();
            Assert.That((stats.Count, stats.TotalSeconds, stats.MaxSeconds), Is.EqualTo((0, 0f, 0f)));
            Assert.That(stats.CountOver(0f), Is.Zero);
        }

        [Test]
        public void FramesPastTheCapacityStillCountAndInvalidDurationsAreIgnored()
        {
            var stats = new FrameStats(4);
            stats.Add(float.NaN);
            stats.Add(-0.01f);
            stats.Add(float.PositiveInfinity);
            Assert.That(stats.Count, Is.Zero, "Not frames.");

            foreach (float frame in new[] { 0.010f, 0.030f, 0.020f, 0.040f, 0.050f, 0.060f })
                stats.Add(frame);
            Assert.That(stats.Count, Is.EqualTo(6), "Every frame counts toward the average and the longest.");
            Assert.That(stats.MaxSeconds, Is.EqualTo(0.060f));
            Assert.That(stats.AverageSeconds, Is.EqualTo(0.035f).Within(1e-5f));
            Assert.That(stats.Percentile(0.5f), Is.EqualTo(0.020f), "Percentiles read the first four kept samples.");
            Assert.That(stats.Percentile(1f), Is.EqualTo(0.040f));
            Assert.That(stats.CountOver(0.025f), Is.EqualTo(2));

            Assert.Throws<ArgumentOutOfRangeException>(() => new FrameStats(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => stats.Percentile(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => stats.Percentile(1.5f));
        }
    }
}
