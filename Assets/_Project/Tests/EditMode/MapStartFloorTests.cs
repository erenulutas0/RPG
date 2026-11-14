using System.Globalization;
using System.Threading;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEditor;

namespace Cryptforge.Tests
{
    // The map a run starts on, resolved on the authored Descent (EncounterController.DescentFloor): the Foundry, the
    // Quicksilver Vaults and the Gloam Gardens are floors 1, 2 and 3, each reached with the halls the floors above it add
    // to the round, and a number past the Descent stops at its last floor. The map choice's text is the invariant
    // culture's on a Turkish phone, and the request that carries a choice across the reload is read once.
    public sealed class MapStartFloorTests
    {
        private const string FirstFloor = "Assets/_Project/Data/Floors/Floor_EmberHalls.asset";

        private static FloorDefinition Foundry()
        {
            var floor = AssetDatabase.LoadAssetAtPath<FloorDefinition>(FirstFloor);
            Assert.That(floor, Is.Not.Null, FirstFloor);
            return floor;
        }

        [Test]
        public void EachMapIsItsFloorOfTheAuthoredDescent()
        {
            FloorDefinition foundry = Foundry();
            FloorDefinition vaults = foundry.NextFloor, gardens = vaults.NextFloor, finale = gardens.NextFloor;

            Assert.That(EncounterController.DescentFloor(foundry, 1, out int reached, out int halls), Is.SameAs(foundry));
            Assert.That((reached, halls), Is.EqualTo((1, 0)));

            Assert.That(EncounterController.DescentFloor(foundry, 2, out reached, out halls), Is.SameAs(vaults));
            Assert.That(vaults.Id, Is.EqualTo("floor_quicksilver_vaults"));
            Assert.That(vaults.Theme, Is.EqualTo(FloorTheme.Ice));
            Assert.That((reached, halls), Is.EqualTo((2, EncounterController.LaterHalls(foundry))),
                "The halls floor 1 adds to the round, as a descent from it counts them.");

            Assert.That(EncounterController.DescentFloor(foundry, 3, out reached, out halls), Is.SameAs(gardens));
            Assert.That(gardens.Id, Is.EqualTo("floor_gloam_gardens"));
            Assert.That(gardens.Theme, Is.EqualTo(FloorTheme.Gloam));
            Assert.That((reached, halls),
                Is.EqualTo((3, EncounterController.LaterHalls(foundry) + EncounterController.LaterHalls(vaults))));

            Assert.That(finale.Id, Is.EqualTo("floor_final_swarm"), "Floor 3 goes on to the finale.");
            Assert.That(EncounterController.DescentFloor(foundry, 9, out reached, out _), Is.SameAs(finale),
                "A number past the Descent stops at its last floor.");
            Assert.That(reached, Is.EqualTo(4));
            Assert.That(EncounterController.DescentFloor(foundry, 0, out reached, out halls), Is.SameAs(foundry));
            Assert.That((reached, halls), Is.EqualTo((1, 0)));
        }

        // A deeper start is climb-scaled as the floor it lands on (ClimbScaling by FloorNumber): floor 2 and 3 are harder
        // than floor 1, and their bosses harden from floor 3.
        [Test]
        public void TheMapsAreClimbScaledByTheirFloorNumber()
        {
            FloorDefinition foundry = Foundry();
            Assert.That(foundry.ClimbScaled && foundry.NextFloor.ClimbScaled && foundry.NextFloor.NextFloor.ClimbScaled, Is.True);
            Assert.That(ClimbScaling.Health(2), Is.EqualTo(1.4f));
            Assert.That(ClimbScaling.Health(3), Is.EqualTo(1.4f * 1.4f));
            Assert.That(ClimbScaling.Boss(3), Is.GreaterThan(1f));
        }

        [Test]
        public void TheMapChoicesTextIsInvariantOnATurkishPhone()
        {
            CultureInfo before = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("tr-TR");
                Assert.That(MapChoiceView.Describe(1), Does.Contain("Floor 1."));
                Assert.That(MapChoiceView.Describe(2), Does.Contain("Floor 2. Start at level 31: choose 31 level-ups first."));
                Assert.That(MapChoiceView.Describe(3), Does.Contain("Floor 3. Start at level 44: choose 44 level-ups first."));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = before;
            }
        }

        [Test]
        public void TheRequestIsReadOnce()
        {
            RunStartRequest.Clear();
            Assert.That(RunStartRequest.Take(), Is.EqualTo(0));
            RunStartRequest.Set(3);
            Assert.That(RunStartRequest.IsPending, Is.True);
            Assert.That(RunStartRequest.Take(), Is.EqualTo(3));
            Assert.That(RunStartRequest.Take(), Is.EqualTo(0), "Taken, it is gone.");
            RunStartRequest.Set(-2);
            Assert.That(RunStartRequest.IsPending, Is.False);
            RunStartRequest.Set(2);
            RunStartRequest.Clear();
            Assert.That(RunStartRequest.Take(), Is.EqualTo(0));
        }

        [Test]
        public void EveryMapIsAStartAndTheFinaleIsNot()
        {
            FloorDefinition floor = Foundry();
            for (int number = 1; number <= 4; number++, floor = floor.NextFloor)
                Assert.That(DescentStart.IsStart(number), Is.EqualTo(number <= 3), floor.Id);
        }
    }
}
