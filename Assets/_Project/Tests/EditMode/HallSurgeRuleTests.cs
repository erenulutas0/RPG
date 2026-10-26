using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    public sealed class HallSurgeRuleTests
    {
        [Test] public void SurgesCountBackFromTheLastWaveAndGrowEachTime()
        {
            Assert.That(HallSurgeRule.SourceWave(3,0),Is.EqualTo(2),"The first surge repeats the last wave.");
            Assert.That(HallSurgeRule.SourceWave(3,1),Is.EqualTo(1));
            Assert.That(HallSurgeRule.SourceWave(3,3),Is.EqualTo(2),"Then round again.");
            Assert.That(HallSurgeRule.SourceWave(1,1),Is.Zero,"A one-wave room repeats its wave.");
            Assert.That(HallSurgeRule.Size(4,0),Is.EqualTo(5));Assert.That(HallSurgeRule.Size(3,1),Is.EqualTo(5));
            Assert.That(HallSurgeRule.Size(9,4),Is.EqualTo(PackLayout.MaxFormationSize),"Never more than a formation.");
            Assert.That(()=>HallSurgeRule.SourceWave(0,0),Throws.InstanceOf<System.ArgumentOutOfRangeException>());
        }
    }
}
