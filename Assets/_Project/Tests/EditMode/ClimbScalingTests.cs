using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class ClimbScalingTests
    {
        [Test]
        public void TheFirstTwoFloorsKeepTheirHandTunedTiers()
        {
            Assert.That(ClimbScaling.Health(1), Is.EqualTo(1f));
            Assert.That(ClimbScaling.Damage(1), Is.EqualTo(0.92f));
            Assert.That(ClimbScaling.Health(2), Is.EqualTo(1.4f).Within(1e-6f));
            Assert.That(ClimbScaling.Damage(2), Is.EqualTo(1.288f).Within(1e-6f));
            Assert.That(ClimbScaling.Boss(1), Is.EqualTo(1f));
            Assert.That(ClimbScaling.Boss(2), Is.EqualTo(1f), "The existing Warden fights stay as tuned.");
        }

        [Test]
        public void EveryFloorAboveIsHarderAndBossesHardenFaster()
        {
            for (int floor = 2; floor <= 12; floor++)
            {
                Assert.That(ClimbScaling.Health(floor), Is.EqualTo(ClimbScaling.Health(floor - 1) * ClimbScaling.Growth).Within(1e-4f));
                Assert.That(ClimbScaling.Damage(floor), Is.GreaterThan(ClimbScaling.Damage(floor - 1)));
                Assert.That(ClimbScaling.Boss(floor), Is.GreaterThanOrEqualTo(ClimbScaling.Boss(floor - 1)));
            }
            Assert.That(ClimbScaling.Boss(3), Is.EqualTo(ClimbScaling.BossGrowth));
            Assert.That(ClimbScaling.Boss(5), Is.EqualTo(ClimbScaling.BossGrowth * ClimbScaling.BossGrowth * ClimbScaling.BossGrowth).Within(1e-5f));
            // A floor-3 Warden: 300 x 1.96 x 1.15.
            Assert.That(300f * ClimbScaling.Health(3) * ClimbScaling.Boss(3), Is.EqualTo(676.2f).Within(.01f));
        }

        [Test]
        public void FloorsBelowTheFirstDoNotExist()
        {
            Assert.That(() => ClimbScaling.Health(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => ClimbScaling.Boss(-1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
