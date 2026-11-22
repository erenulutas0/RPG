using Cryptforge.Combat;
using NUnit.Framework;

namespace Cryptforge.Tests
{
    // docs/62's P0a in the foundry simulation (DescentSimulation.Outworks.cs): off by default and then nothing moves; on, the
    // phase plays before floor 1's halls, the same way every time, and leads into the first hall on the same floor clock.
    public sealed class OutworksSimulationTests
    {
        [Test]
        public void WithoutTheLeverAFloorIsTheShippedFloor()
        {
            DescentSimulation.FoundryResult shipped = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 11);
            var off = new DescentSimulation.FoundryLevers { Outworks = false };
            DescentSimulation.FoundryResult again = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 11, off);
            Assert.That(again.FloorSeconds, Is.EqualTo(shipped.FloorSeconds));
            Assert.That(again.Level, Is.EqualTo(shipped.Level));
            Assert.That(again.HeroHealth, Is.EqualTo(shipped.HeroHealth));
            Assert.That(again.OutworksSeconds, Is.EqualTo(-1f), "no phase played");
            Assert.That(again.PadLaunches, Is.EqualTo(0));
        }

        [Test]
        public void ThePhasePlaysBeforeTheHallsTheSameWayEveryTime()
        {
            DescentSimulation.FoundryLevers levers = DescentSimulation.OutworksLevers(new DescentSimulation.FoundryLevers());
            DescentSimulation.FoundryResult first = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 11, levers);
            DescentSimulation.FoundryResult second = DescentSimulation.RunFoundry(DescentSimulation.Vanguard(), 11,
                DescentSimulation.OutworksLevers(new DescentSimulation.FoundryLevers()));
            Assert.That(second.FloorSeconds, Is.EqualTo(first.FloorSeconds));
            Assert.That(second.OutworksSeconds, Is.EqualTo(first.OutworksSeconds));
            Assert.That(second.AnvilsCaptured, Is.EqualTo(first.AnvilsCaptured));

            Assert.That(first.OutworksSeconds, Is.GreaterThan(0f), "the stair was held");
            Assert.That(first.AnvilsCaptured, Is.GreaterThanOrEqualTo(1));
            Assert.That(first.KeeperSpawned || first.BellRung, Is.True);
            // The halls go on after it: the first hall began once the stair was held, on the same floor clock.
            Assert.That(first.RoomSeconds[0], Is.GreaterThan(0f));
            Assert.That(first.FloorSeconds, Is.GreaterThan(first.OutworksSeconds));
            if (first.LevelAtGate >= 0)
                Assert.That(first.ClockAtGate, Is.GreaterThan(first.OutworksSeconds), "the floor's clock ran through the phase");
        }

        [Test]
        public void ThePhaseTrimsOnlyFloorOnesHalls()
        {
            DescentSimulation.FoundryLevers levers = DescentSimulation.OutworksLevers(new DescentSimulation.FoundryLevers());
            Assert.That(levers.Outworks, Is.True);
            Assert.That(levers.HallSurges, Is.EqualTo(OutworksRule.TrimmedHallSurges));
            Assert.That(levers.ExtraElites, Is.EqualTo(OutworksRule.TrimmedExtraElites));
            Assert.That(levers.GateNotBeforeSeconds, Is.EqualTo(OutworksRule.TrimmedGateSeconds));
            DescentSimulation.FoundryLevers later = DescentSimulation.LaterFloorLevers(levers, DescentSimulation.FoundryFloorTwo());
            Assert.That(later.HallSurges, Is.EqualTo(DescentSimulation.FoundryFloorTwo().HallSurges), "floor 2 plays its own");
        }
    }
}
