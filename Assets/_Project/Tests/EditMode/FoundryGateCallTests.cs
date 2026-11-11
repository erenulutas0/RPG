using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/41's open decision 2 (research 2026-11-11, GateCallResearch): the foundry simulation's early call on the gate,
    // FoundryLevers.GateCalls and its rewards. Off by default; a call that never comes leaves the floor the shipped one run
    // for run; a call ends the hold the frame after it and the floor up to it is the shipped floor's; a level waits for its
    // level; each reward is paid at the call; and floor 2, whose gate does not hold, never calls. Research only: nothing in
    // the game reads these levers.
    public sealed class FoundryGateCallTests
    {
        [Test]
        public void TheCallIsOffByDefaultAndACallThatNeverComesIsTheShippedFloor()
        {
            DescentSimulation.FoundryLevers shipped = FoundryBatch.Shipped();
            Assert.That(shipped.GateCalls, Is.False);
            Assert.That((shipped.GateCallLevel, shipped.GateCallHealth, shipped.GateCallHeal, shipped.GateCallUpgrades,
                shipped.GateCallExperienceShare), Is.EqualTo((0, 0f, 0f, 0, 0f)));
            DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, shipped);
            Assert.That(run.GateCalled, Is.False);
            Assert.That((run.GateCallClock, run.GateCallSkipped, run.GateCallExperience), Is.EqualTo((-1f, 0f, 0)));
            Assert.That(run.WaitExperience, Is.GreaterThan(0), "the wait farms");

            // A level never reached, and a health share above the maximum, never call: the floor is the shipped one.
            foreach (DescentSimulation.FoundryLevers never in new[] { Calling("never: level", 1000, 0f), Calling("never: health", 0, 1.01f) })
            {
                DescentSimulation.FoundryResult same = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, never);
                Assert.That(same.GateCalled, Is.False, never.Name);
                Assert.That(same.FloorSeconds, Is.EqualTo(run.FloorSeconds), never.Name);
                Assert.That(same.GateWaitSeconds, Is.EqualTo(run.GateWaitSeconds), never.Name);
                Assert.That(same.Picks, Is.EqualTo(run.Picks), never.Name);
                Assert.That(same.HeroHealth, Is.EqualTo(run.HeroHealth), never.Name);
            }
        }

        // At once, the call comes on the hold's first frame and the Warden walks out the next: the floor up to the gate is
        // the shipped one's, and the hero meets the Warden at the level it had as the room before the gate cleared.
        [Test]
        public void ACallAtOnceEndsTheHoldTheFrameAfterIt()
        {
            DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, FoundryBatch.Shipped());
            DescentSimulation.FoundryResult called = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, Calling("at once", 0, 0f));
            Assert.That(called.GateReadyClock, Is.EqualTo(shipped.GateReadyClock), "the same floor up to the gate");
            Assert.That(called.GateReadyLevel, Is.EqualTo(shipped.GateReadyLevel));
            Assert.That(called.GateCalled, Is.True);
            Assert.That(called.GateCallClock, Is.EqualTo(called.GateReadyClock), "called as the hold began");
            Assert.That(called.GateCallSkipped, Is.EqualTo(210f - called.GateCallClock).Within(1e-3f));
            Assert.That(called.GateWaitSeconds, Is.EqualTo(1f / 60f).Within(1e-4f), "one frame held");
            Assert.That(called.LevelAtGate, Is.LessThan(shipped.LevelAtGate));
            Assert.That(called.ClockAtGate, Is.LessThan(shipped.ClockAtGate));
            Assert.That(called.GateCallExperience, Is.EqualTo(0), "no reward asked");
        }

        // A level's call waits in the hold until the hero has the level, and ends it before the gate's time.
        [Test]
        public void ALevelsCallWaitsForItsLevel()
        {
            DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), 1, FoundryBatch.Shipped());
            int level = shipped.GateReadyLevel + 3;
            Assert.That(shipped.LevelAtGate, Is.GreaterThan(level), "the shipped wait passes the level");
            DescentSimulation.FoundryResult called = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), 1, Calling("level", level, 0f));
            Assert.That(called.GateCalled, Is.True);
            Assert.That(called.GateCallClock, Is.GreaterThan(called.GateReadyClock));
            Assert.That(called.GateCallClock, Is.LessThan(210f));
            Assert.That(called.GateWaitSeconds, Is.GreaterThan(1f).And.LessThan(shipped.GateWaitSeconds));
            Assert.That(called.LevelAtGate, Is.GreaterThanOrEqualTo(level));
        }

        // The rewards, paid at the call: a heal to the maximum, two upgrade choices answered on the call's frame, and the
        // experience for the seconds skipped - which the wait's own experience leaves out.
        [Test]
        public void TheCallsRewardsArePaidAtTheCall()
        {
            DescentSimulation.FoundryLevers healed = Calling("heal", 0, 0f);
            healed.GateCallHeal = 1f;
            DescentSimulation.FoundryResult heal = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 2, healed);
            Assert.That(heal.GateCalled, Is.True);
            Assert.That(heal.HeroHealthAtGate, Is.EqualTo(heal.HeroMaximumAtGate), "healed whole as the Warden walks out");

            var plain = new List<string>();
            DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, Calling("at once", 0, 0f), trace: plain.Add);
            DescentSimulation.FoundryLevers tempered = Calling("upgrades", 0, 0f);
            tempered.GateCallUpgrades = 2;
            var granted = new List<string>();
            DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, tempered, trace: granted.Add);
            Assert.That(PicksOnTheCallsFrame(granted) - PicksOnTheCallsFrame(plain), Is.EqualTo(2), "two choices at the call");

            DescentSimulation.FoundryLevers paid = Calling("xp", 0, 0f);
            paid.GateCallExperienceShare = .5f;
            DescentSimulation.FoundryResult xp = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 1, paid);
            Assert.That(xp.GateCallExperience, Is.EqualTo(DescentSimulation.GateCallExperienceFor(xp.GateCallSkipped, .5f)));
            Assert.That(xp.GateCallExperience, Is.GreaterThan(0));
            Assert.That(xp.WaitExperience, Is.LessThan(xp.GateCallExperience), "the wait's experience is the farm's, not the call's");
            Assert.That(DescentSimulation.GateCallExperienceFor(100f, .5f), Is.EqualTo((int)(100f * DescentSimulation.GateWaitExperienceRate * .5f)));
            Assert.That(DescentSimulation.GateCallExperienceFor(0f, 1f), Is.EqualTo(0));
            Assert.That(DescentSimulation.GateCallExperienceFor(50f, 0f), Is.EqualTo(0));
        }

        // Floor 2's gate opens as its room clears (Floor_QuicksilverVaults sets no time): no hold there, so no call.
        [Test]
        public void FloorTwoNeverHoldsSoNeverCalls()
        {
            DescentSimulation.FoundryLevers levers = Calling("two floors", 0, 0f);
            levers.Floors = 2;
            DescentSimulation.FoundryResult run = DescentSimulation.RunFoundry(DescentSimulation.CinderArcanist(), 1, levers);
            Assert.That(run.Cleared, Is.True);
            Assert.That(run.GateCalled, Is.True);
            Assert.That(run.NextFloor, Is.Not.Null);
            Assert.That(run.NextFloor.GateCalled, Is.False);
            Assert.That(run.NextFloor.GateWaitSeconds, Is.EqualTo(0f));
        }

        private static DescentSimulation.FoundryLevers Calling(string name, int level, float health)
        {
            DescentSimulation.FoundryLevers levers = FoundryBatch.Shipped().Copy("gate call " + name);
            levers.GateCalls = true;
            levers.GateCallLevel = level;
            levers.GateCallHealth = health;
            return levers;
        }

        // The cards taken on the frame the gate was called: the trace's lines carry their frame's second first.
        private static int PicksOnTheCallsFrame(List<string> trace)
        {
            string call = trace.First(line => line.Contains(" gate_called "));
            string second = call.Substring(0, call.IndexOf(' '));
            return trace.Count(line => line.StartsWith(second + " upgrade_selected ", System.StringComparison.Ordinal));
        }
    }
}
