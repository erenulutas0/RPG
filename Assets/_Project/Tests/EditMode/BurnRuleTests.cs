using System;
using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // Burn, the Cinder Arcanist's status (docs/45): each rule something the scene's BurnStatus relies on.
    public sealed class BurnRuleTests
    {
        private const float Frame = 1f / 60f;

        [Test] public void ABurnPaysWholeTicksForItsDurationThenGoesOut()
        {
            var burn = new BurnState();
            Assert.That(burn.IsBurning, Is.False);
            Assert.That(burn.Tick(1f), Is.Zero, "Nothing burns without a stack.");
            burn.Apply(1, false);
            float dealt = 0f;
            int frames = 0;
            while (burn.IsBurning && frames < 1000) { dealt += burn.Tick(Frame); frames++; }
            Assert.That(dealt, Is.EqualTo(BurnRule.DamagePerStackPerSecond * BurnRule.DurationSeconds).Within(1e-3f),
                "One stack for three seconds: nine damage.");
            Assert.That(frames, Is.EqualTo(180).Within(1), "Out after its duration.");
            Assert.That((burn.Stacks, burn.Remaining, burn.HasDirectStack), Is.EqualTo((0, 0f, false)));
        }

        [Test] public void StacksCapAndANewStackRefreshesTheBurn()
        {
            var burn = new BurnState();
            for (int i = 0; i < 8; i++) burn.Apply(1, false);
            Assert.That(burn.Stacks, Is.EqualTo(BurnRule.MaxStacks), "Never past the cap.");
            Assert.That(burn.Tick(BurnRule.TickSeconds), Is.EqualTo(BurnRule.TickDamage(BurnRule.MaxStacks)));
            burn.Tick(2f);
            Assert.That(burn.Remaining, Is.EqualTo(BurnRule.DurationSeconds - 2.5f).Within(1e-4f));
            burn.Apply(1, false);
            Assert.That(burn.Remaining, Is.EqualTo(BurnRule.DurationSeconds), "A new stack refreshes every stack.");
            Assert.That(burn.Tick(BurnRule.DurationSeconds + 1f), Is.EqualTo(BurnRule.TickDamage(5) * 6).Within(1e-3f),
                "A long frame pays the ticks its burning part held, no more.");
            Assert.That(burn.IsBurning, Is.False);
            Assert.That(() => burn.Apply(0, false), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => burn.Tick(-1f), Throws.InstanceOf<ArgumentOutOfRangeException>());
        }

        [Test] public void OnlyADirectStackLetsAFallPassTheBurnOn()
        {
            var caught = new BurnState();
            caught.Apply(BurnRule.SpreadStacks, true);
            Assert.That((caught.IsBurning, caught.Spreads), Is.EqualTo((true, false)), "A spread stack passes nothing on.");
            caught.Apply(1, false);
            Assert.That(caught.Spreads, Is.True, "The hero's own hit makes it a source.");
            Assert.That(caught.Consume(), Is.EqualTo(2), "The Vent spends every stack.");
            Assert.That((caught.IsBurning, caught.Spreads), Is.EqualTo((false, false)));
        }

        [Test] public void TheVentDealsItsDamageForEverySpentStack()
        {
            Assert.That(BurnRule.VentDamage(0, 8f), Is.Zero);
            Assert.That(BurnRule.VentDamage(5, 8f), Is.EqualTo(40f));
            Assert.That(BurnRule.TickDamage(2), Is.EqualTo(3f));
            Assert.That(() => BurnRule.VentDamage(6, 8f), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(() => BurnRule.VentDamage(1, float.NaN), Throws.InstanceOf<ArgumentOutOfRangeException>());
            Assert.That(BurnRule.SpreadRadius, Is.EqualTo(KillBurstRule.Radius), "Beside it means what it means for Cinder Nova.");
        }
    }
}
