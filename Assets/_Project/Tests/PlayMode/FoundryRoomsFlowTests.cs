using System.Collections;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // A foundry floor's rooms as the foundry plays them (EncounterController.FoundryRooms): its extra elite - a copy of
    // its first elite room - before the gate, at the next district; each elite room's Grunt Captain an elite while its
    // wave stands (EncounterController.Elites, EliteLeader); and the floor's horde leaning harder as the halls fall behind
    // the hero, thinning for the gate's boss, pressing again round an enraged one and leaving as the gate opens
    // (HordeRule.StateFor), and holding still with the rest while the camera presents that boss. The hall arena plays the
    // same floor as authored, with none of them.
    // Resources/Development/Floor_FoundryClimbProof: a hall of one Cinder Mite, the Captain's elite room (a Grunt Captain
    // and a Mite) with one extra elite, the Warden's gate, a horde of Mites.
    public sealed class FoundryRoomsFlowTests
    {
        private const string ClimbFloor = "floor_foundry_climb_proof";

        [UnitySetUp]
        public IEnumerator Begin()
        {
            TestProfile.Begin();
            Time.timeScale = 1;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator End()
        {
            Time.timeScale = 1;
            var old = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry rooms cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // The elite room's Captain is the elite from its spawn until its wave goes - still the wave's, fallen, while its
        // Mite stands - and the floor's extra elite, the same room again at the next district, brings a Captain of its
        // own; neither the hall nor the gate has one.
        [UnityTest, Timeout(120000)]
        public IEnumerator TheCaptainIsAnEliteWhileItsWaveStandsAndTheExtraEliteTakesTheNextDistrict()
        {
            yield return FoundryRunDriver.Load(ClimbFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            AttackController heroAttack = hero.GetComponent<AttackController>();
            FloorDefinition floor = encounters.Floor;

            // As the foundry plays the floor: the hall, the elite room, its copy, the gate.
            Assert.That(floor.RoomCount, Is.EqualTo(3));
            Assert.That(encounters.RoomCount, Is.EqualTo(4), "The floor's extra elite stands before its gate.");
            RoomDefinition[] rooms = EncounterController.FoundryRooms(floor);
            Assert.That(new[] { rooms[0].Kind, rooms[1].Kind, rooms[2].Kind, rooms[3].Kind },
                Is.EqualTo(new[] { RoomKind.Combat, RoomKind.Elite, RoomKind.Elite, RoomKind.Boss }));
            Assert.That(rooms[2], Is.SameAs(floor.RoomAt(1)), "The extra elite is the floor's elite room again.");
            Assert.That(heroAttack.Elites, Is.SameAs(encounters.Elites), "The hero's strikes are handed the elites as they stand.");

            // The hall: no elite.
            Assert.That(encounters.RoomNumber, Is.EqualTo(1));
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(1));
            Assert.That(encounters.Elites, Is.Empty);
            Assert.That(encounters.EliteLeader, Is.Null);

            // The elite room opens at the floor's next district; nothing is an elite while it waits there.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.IsRoomSiteOpen);
            FoundryMap.RoomSite(0, 1, out float x1, out float y1);
            Assert.That(encounters.RoomNumber, Is.EqualTo(2));
            Assert.That(encounters.CurrentRoom.Kind, Is.EqualTo(RoomKind.Elite));
            Assert.That(encounters.RoomSiteFloorX, Is.EqualTo(x1), "The floor's first later district.");
            Assert.That(encounters.RoomSiteFloorY, Is.EqualTo(y1));
            Assert.That(encounters.Elites, Is.Empty);

            // Walked onto, its wave forms up: the Captain at its head is the elite, its Mite is not.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.AliveEnemyCount > 0);
            Health captain = encounters.EliteLeader;
            Assert.That(captain, Is.Not.Null, "The elite room's Captain leads it.");
            Assert.That(captain, Is.SameAs(encounters.WaveEnemyAt(0)));
            Assert.That(encounters.DefinitionOf(captain).DisplayName, Is.EqualTo("Grunt Captain"));
            Assert.That(encounters.Elites, Is.EqualTo(new[] { captain }), "The Captain is the one elite.");
            Assert.That(encounters.Elites, Has.No.Member(encounters.WaveEnemyAt(1)), "Its Mite is no elite.");

            // Fallen while its Mite stands: no longer the leader, still the wave's elite until the wave goes.
            captain.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(encounters.EliteLeader, Is.Null, "A fallen Captain leads nothing.");
            Assert.That(encounters.Elites, Is.EqualTo(new[] { captain }), "It stays the wave's elite, fallen, while its wave stands.");
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(1));

            // The wave cleared, the floor's extra elite opens at the next district, and the elite went with its wave.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.IsRoomSiteOpen);
            FoundryMap.RoomSite(0, 2, out float x2, out float y2);
            Assert.That(encounters.RoomNumber, Is.EqualTo(3));
            Assert.That(encounters.CurrentRoom, Is.SameAs(floor.RoomAt(1)));
            Assert.That(encounters.RoomSiteFloorX, Is.EqualTo(x2), "The extra elite takes the next district.");
            Assert.That(encounters.RoomSiteFloorY, Is.EqualTo(y2));
            Assert.That(encounters.Elites, Is.Empty, "The elite went with its wave.");
            Assert.That(encounters.EliteLeader, Is.Null);
            yield return null;
            Assert.That(captain == null, Is.True, "And its body with it.");

            // The extra elite's own Captain, the one elite again.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.AliveEnemyCount > 0);
            Health second = encounters.EliteLeader;
            Assert.That(second, Is.Not.Null, "The extra elite is led by a Captain of its own.");
            Assert.That(encounters.DefinitionOf(second).DisplayName, Is.EqualTo("Grunt Captain"));
            Assert.That(encounters.Elites, Is.EqualTo(new[] { second }));

            // Cleared, the gate: no district, no elite.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.GateBoss != null);
            Assert.That(encounters.RoomNumber, Is.EqualTo(4));
            Assert.That(encounters.IsRoomSiteOpen, Is.False, "The gate opens no district: its boss walks out of it.");
            Assert.That(encounters.Elites, Is.Empty);
            Assert.That(encounters.EliteLeader, Is.Null);
            Assert.That(encounters.DefinitionOf(encounters.GateBoss).DisplayName, Is.EqualTo("Forge Warden"));
            LogAssert.NoUnexpectedReceived();
        }

        // The floor's horde leans harder as the halls fall behind the hero - a thin crowd at first, the Horde after the
        // hall, Pressure after the elite room - thins to Boss once the gate's boss has walked out, presses round it again
        // as the Horde once it is enraged, and leaves with the floor's clock as the gate opens.
        [UnityTest, Timeout(120000)]
        public IEnumerator TheHordePressesHarderAsTheHallsFallThenThinsForTheGateAndGoesWithIt()
        {
            yield return FoundryRunDriver.Load(ClimbFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;

            yield return FoundryRunDriver.Until(setup, hero, () => encounters.IsHordeActive, 5f);
            Assert.That(encounters.IsHordeActive, Is.True, "The foundry floor has its horde from its first step.");
            Assert.That(encounters.HordeStateNow, Is.EqualTo(HordeState.Ramp), "Nothing is behind the hero yet.");
            Assert.That(clock.IsShown, Is.True);

            // The hall behind the hero, then the elite room too.
            yield return FoundryRunDriver.Drive(setup, hero,
                () => encounters.RoomNumber == 2 && encounters.HordeStateNow == HordeState.Horde);
            yield return FoundryRunDriver.Drive(setup, hero,
                () => encounters.RoomNumber == 3 && encounters.HordeStateNow == HordeState.Pressure);

            // The gate's boss walked out: the horde thins so the boss can be read.
            yield return FoundryRunDriver.Drive(setup, hero,
                () => encounters.GateBoss != null && !encounters.IsRevealHeld && encounters.HordeStateNow == HordeState.Boss);
            int bossCrowd = (int)(HordeRule.TargetByState[(int)HordeState.Boss] * (1f + HordeRule.ReactiveShare) + .5f);
            Assert.That(encounters.HordeTarget, Is.LessThanOrEqualTo(bossCrowd), "The boss's room aims for its own thin crowd.");
            Assert.That(clock.IsUnderBoss, Is.True, "The floor clock steps below the boss's readout.");

            // Below half health the boss draws the horde round it as a hall does.
            Health boss = encounters.GateBoss;
            boss.ApplyDamage(new DamageContext(boss.Maximum * (1f - WardenRule.PhaseTwoAt) + 1f));
            yield return FoundryRunDriver.Until(setup, hero,
                () => encounters.IsBossEnraged && encounters.HordeStateNow == HordeState.Horde, 5f);
            Assert.That(encounters.IsBossEnraged, Is.True, "The Warden enrages below half.");
            Assert.That(encounters.HordeStateNow, Is.EqualTo(HordeState.Horde), "Its room's horde thickens round it.");

            // Its fall opens the gate and sends the horde away with the floor's clock.
            yield return FoundryRunDriver.Until(setup, hero, () => FoundryRunDriver.StandingHordeEnemy(encounters) != null, 10f);
            Health horde = FoundryRunDriver.StandingHordeEnemy(encounters);
            Assert.That(horde, Is.Not.Null);
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsGateExitOpen, Is.True);
            Assert.That(encounters.IsHordeActive, Is.False, "The horde leaves as the gate opens.");
            Assert.That(encounters.HordeEnemyCount, Is.Zero);
            Assert.That(encounters.HordeAliveCount, Is.Zero);
            Assert.That(encounters.FloorClockSeconds, Is.Zero);
            Assert.That(encounters.IsFloorOvertime, Is.False);
            yield return null;
            yield return null;
            // Its bodies leave the floor with it: destroyed, or since the horde's body pool (2026-11-11) shelved off the floor
            // for the next of their kind.
            Assert.That(horde == null || !horde.gameObject.activeInHierarchy, Is.True, "Its bodies with it.");
            Assert.That(clock.IsShown, Is.False, "No floor clock on the walk out.");
            Assert.That(encounters.IsBossEnraged, Is.False, "The fallen Warden's rage is over.");
            LogAssert.NoUnexpectedReceived();
        }

        // While the camera presents the gate's boss, the encounter holds still and the hero's walk waits, "so nothing is
        // fought off screen" (BossRevealView): the packs stand, the floor's clock, the pit and the Warden's slam wait. The
        // horde's enemies already in reach of the hero as the boss walks out should wait with them, or the hero takes their
        // blows off screen with no way to step away (the GateBossFlowTests reveal has no horde to show it).
        [UnityTest, Timeout(120000)]
        public IEnumerator NothingStrikesTheHeroWhileTheCameraIsAtTheGate()
        {
            yield return FoundryRunDriver.Load(ClimbFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;

            // The extra elite's wave standing, the horde gathers round the hero until some of it stands in reach.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.RoomNumber == 3 && encounters.AliveEnemyCount > 0);
            yield return FoundryRunDriver.Until(setup, hero, () => HordeInReach(encounters, hero) > 0, 30f);
            Assert.That(HordeInReach(encounters, hero), Is.GreaterThan(0), "The horde gathers round the hero.");
            PackTestUtility.KillWave(encounters);
            yield return FoundryRunDriver.Until(setup, hero, () => encounters.GateBoss != null, 10f);
            Assert.That(encounters.GateBoss, Is.Not.Null, "The gate's boss walks out.");
            Assert.That(encounters.IsRevealHeld, Is.True, "The camera turns to it.");
            int inReach = HordeInReach(encounters, hero);
            Assert.That(inReach, Is.GreaterThan(0), "Some of the horde stands in reach of the hero as it does.");

            int struck = 0;
            System.Action<DamageContext> counted = context =>
            {
                if (encounters.IsRevealHeld)
                    struck++;
            };
            hero.Damaged += counted;
            yield return FoundryRunDriver.Until(setup, hero, () => !encounters.IsRevealHeld, 10f);
            hero.Damaged -= counted;
            Assert.That(encounters.IsRevealHeld, Is.False, "The camera comes back to the hero.");
            Assert.That(struck, Is.Zero,
                $"{inReach} of the horde stood in reach as the boss walked out, and struck the hero {struck} times while the camera was at the gate.");
            LogAssert.NoUnexpectedReceived();
        }

        // The horde's living enemies within their own weapon's reach of the hero, on the floor.
        private static int HordeInReach(EncounterController encounters, Health hero)
        {
            int count = 0;
            float heroX = hero.transform.position.x, heroY = ArenaFloor.FloorY(hero.transform.position.y);
            for (int i = 0; i < encounters.HordeEnemyCount; i++)
            {
                Health enemy = encounters.HordeEnemyAt(i);
                if (enemy == null || !enemy.IsAlive || !encounters.TryFloorPositionOf(enemy, out float x, out float y))
                    continue;
                float reach = enemy.GetComponent<AttackController>().Weapon.Range;
                if (ArenaFloor.FloorDistanceSquared(x, y, heroX, heroY) <= reach * reach)
                    count++;
            }
            return count;
        }

        // The hall arena plays the same floor as authored: three rooms, each where the hero stands, no horde, its elite
        // room's Captain no elite and its boss no gate boss - the floor clears on the boss's fall, no gate to walk out of.
        [UnityTest, Timeout(90000)]
        public IEnumerator TheHallPlaysTheClimbAsAuthored()
        {
            yield return FoundryRunDriver.Load(ClimbFloor, 0f, "hall");
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            Assert.That(encounters.RoomCount, Is.EqualTo(3), "No extra elite off the foundry.");

            yield return FoundryRunDriver.Drive(setup, hero,
                () => encounters.CurrentRoom.Kind == RoomKind.Elite && encounters.AliveEnemyCount > 0);
            Assert.That(encounters.RoomNumber, Is.EqualTo(2));
            Assert.That(encounters.IsRoomSiteOpen, Is.False, "The hall opens no districts.");
            Assert.That(encounters.EliteLeader, Is.Null, "The hall's Captain is no elite.");
            Assert.That(encounters.Elites, Is.Empty);
            Assert.That(encounters.IsHordeActive, Is.False);

            yield return FoundryRunDriver.Drive(setup, hero,
                () => encounters.CurrentRoom.Kind == RoomKind.Boss && encounters.AliveEnemyCount > 0);
            Assert.That(encounters.RoomNumber, Is.EqualTo(3));
            Assert.That(encounters.GateBoss, Is.Null, "The hall's boss is no gate boss.");
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsFloorCleared, Is.True);
            Assert.That(encounters.IsGateExitOpen, Is.False, "Its fall clears the floor: there is no gate to walk out of.");
            yield return FoundryRunDriver.UntilCheckpoint(setup, hero);
            Assert.That(encounters.TotalRoomsCleared, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
