using System.Collections;
using System.Collections.Generic;
using Cryptforge.Art;
using Cryptforge.Combat;
using Cryptforge.Content;
using Cryptforge.Core;
using Cryptforge.Progression;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // The foundry's climb in the scene, floor to floor. A foundry floor ends at its gate and is left by walking out of it;
    // only then does the checkpoint offer Extract or Descend, and the next floor begins afresh where the hero walked out,
    // until floor 2's gate opens on the Final Swarm. The hall arena's descent (DescentFlowTests) has no gate, no floor
    // clock and no rooms as the foundry plays them, and the Final Swarm after floor 2's gate once made a swarm enemy the
    // Warden there with nothing to catch it (the review of 2026-11-11, fixed in 6a09574f).
    //
    // Resources/Development/Floor_FoundryClimbProof: a hall of one Cinder Mite, the Captain's elite room (a Grunt Captain
    // and a Mite, played twice on the foundry: its extra elite), the Warden's gate, and a horde of Cinder Mites; then
    // Floor_FoundryClimbUpperProof: a hall, the Warden's gate with a Mite beside it, the same horde; then the shipped
    // Final Swarm.
    public sealed class FoundryDescentFlowTests
    {
        private const string ClimbFloor = "floor_foundry_climb_proof";
        private const string UpperFloor = "floor_foundry_climb_upper_proof";
        // Floor 1's gold: its two Grunt Captains' 20 and its Warden's 50. The Mites and the horde pay none.
        private const int ClimbGold = 90;
        // The shipped floor 1's gate waits for its floor's clock (FloorDefinition.FoundryGateNotBeforeSeconds, 210 s);
        // started just past it, its cleared elite opens the gate at once.
        private const float PastFloorOnesGate = 215f;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry descent cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // Walking out of floor 1's gate clears the floor: its drops fly to the hero and the checkpoint opens on the whole
        // floor's gold. Extract ends the run there with every coin banked.
        [UnityTest, Timeout(120000)]
        public IEnumerator ExtractingAtFloorOnesGateBanksTheRun()
        {
            yield return FoundryRunDriver.Load(ClimbFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            var result = Object.FindFirstObjectByType<RunResultView>();

            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.GateBoss != null);
            Assert.That(encounters.RoomNumber, Is.EqualTo(4), "The gate is the fourth room as the foundry plays the floor.");
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsGateExitOpen, Is.True, "The boss's fall opens the gate.");
            Assert.That(encounters.IsFloorCleared, Is.True);
            Assert.That(setup.Choices.Current == null || setup.Choices.Current.Kind != ChoiceKind.Checkpoint, Is.True,
                "No checkpoint before the hero walks out.");
            yield return FoundryRunDriver.WalkOutOfTheGate(setup, hero);
            yield return FoundryRunDriver.UntilCheckpoint(setup, hero);

            ChoicePrompt checkpoint = setup.Choices.Current;
            Assert.That(setup.Drops.Count, Is.Zero, "Walking out swept the floor's drops to the hero.");
            Assert.That(setup.Run.Gold, Is.EqualTo(ClimbGold));
            Assert.That(setup.Run.UnsecuredGold, Is.EqualTo(ClimbGold), "Nothing is secured before the checkpoint.");
            Assert.That(encounters.TotalRoomsCleared, Is.EqualTo(4));
            Assert.That(Time.timeScale, Is.Zero, "The checkpoint pauses the run.");
            Assert.That(checkpoint.Cards[FoundryRunDriver.CheckpointExtract].Name, Is.EqualTo("Extract"));
            Assert.That(checkpoint.Cards[FoundryRunDriver.CheckpointExtract].Description,
                Is.EqualTo($"Bank all {ClimbGold} gold and end the run"));
            Assert.That(checkpoint.Cards[FoundryRunDriver.CheckpointDescend].Description,
                Does.Contain($"{ClimbGold} gold").And.Contain("The Upper Climb"));

            Assert.That(setup.Choices.TrySelect(checkpoint, FoundryRunDriver.CheckpointExtract), Is.True);
            Assert.That(setup.Choices.TrySelect(checkpoint, FoundryRunDriver.CheckpointDescend), Is.False,
                "The decision is taken once.");
            yield return null;
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Extracted));
            Assert.That(setup.Run.GoldBanked, Is.EqualTo(ClimbGold));
            Assert.That(setup.Run.GoldLost, Is.Zero);
            Assert.That(encounters.FloorNumber, Is.EqualTo(1));
            Assert.That(setup.Choices.IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(result.IsOpen, Is.True);
            Assert.That(FoundryRunDriver.Label("Result Title"), Is.EqualTo("Extracted"));
            Assert.That(FoundryRunDriver.Label("Cause Label"), Does.Contain("escaped").And.Contain("The Climb"));
            Assert.That(FoundryRunDriver.Label("Progress Label"), Does.Contain("Floor 1").And.Contain("4 rooms"));
            Assert.That(FoundryRunDriver.Label("Result Gold Label"), Is.EqualTo($"Gold banked: {ClimbGold}"));
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(ClimbGold), "Extracting banks the floor's gold in the saved profile.");
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));

            // Nothing goes on behind the result: no horde comes back, no floor follows, the gate's seal is gone.
            yield return FoundryRunDriver.Hold(setup, hero, 1f);
            Assert.That(encounters.IsHordeActive, Is.False);
            Assert.That(encounters.IsDescending, Is.False);
            Assert.That(encounters.FloorNumber, Is.EqualTo(1));
            Assert.That(setup.GetComponent<ExitSealView>().IsVisible, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        // Descending from floor 1's checkpoint secures the gold and starts floor 2 where the hero walked out of the gate:
        // the hero as wounded as it left, the room count and the HUD's pips starting over, the first hall fought where the
        // hero stands, and a horde of floor 2's own on a clock of its own - from nothing, not from where floor 1's stopped.
        [UnityTest, Timeout(120000)]
        public IEnumerator DescendingThroughFloorOnesGateCarriesTheHeroOntoAFreshFloor()
        {
            yield return FoundryRunDriver.Load(ClimbFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            var hud = Object.FindFirstObjectByType<RunHud>();
            FloorClockView clock = hud.FloorClock;

            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.GateBoss != null);
            // The gate's fight under way, floor 1's clock has run a while.
            yield return FoundryRunDriver.Until(setup, hero,
                () => !encounters.IsRevealHeld && encounters.FloorClockSeconds >= 2f, 20f);
            Assert.That(encounters.FloorClockSeconds, Is.GreaterThanOrEqualTo(2f));
            float floorOneClock = encounters.FloorClockSeconds;
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsGateExitOpen, Is.True);
            Assert.That(encounters.IsHordeActive, Is.False, "The gate's opening sends floor 1's horde away.");
            Assert.That(encounters.FloorClockSeconds, Is.Zero, "No floor clock runs on the walk out.");
            yield return FoundryRunDriver.WalkOutOfTheGate(setup, hero);
            yield return FoundryRunDriver.UntilCheckpoint(setup, hero);
            Assert.That(FoundryRunDriver.Pips(hud), Is.EqualTo((4, 4)), "Floor 1's four rooms, every one reached.");
            Assert.That(FoundryRunDriver.Label("Floor Label"), Does.Contain("Floor 1").And.Contain("Room 4/4"));

            // Wounded once the last level-up is taken, so no card's health can change what is carried down.
            hero.ApplyDamage(new DamageContext(30f));
            float wounded = hero.Current;
            Assert.That(wounded, Is.LessThan(hero.Maximum));
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current, FoundryRunDriver.CheckpointDescend), Is.True);

            Assert.That(setup.Run.HasEnded, Is.False);
            Assert.That(setup.Run.SecuredGold, Is.EqualTo(ClimbGold), "Descending secures the floor's gold.");
            Assert.That(setup.Run.UnsecuredGold, Is.Zero);
            Assert.That(encounters.FloorNumber, Is.EqualTo(2));
            Assert.That(encounters.Floor.Id, Is.EqualTo(UpperFloor));
            Assert.That(encounters.IsDescending, Is.True);
            Assert.That(encounters.RoomNumber, Is.Zero, "No room of floor 2 has begun yet.");
            Assert.That(encounters.RoomCount, Is.EqualTo(2));
            Assert.That(encounters.TotalRoomsCleared, Is.EqualTo(4),
                "Floor 1's rooms as the foundry played them, its extra elite among them.");
            Assert.That(encounters.IsGateExitOpen, Is.False);
            Assert.That(encounters.GateBoss, Is.Null);
            Assert.That(encounters.IsHoldingForGate, Is.False);
            Assert.That(encounters.IsHordeActive, Is.False, "No horde on the way down.");
            Assert.That(encounters.FloorClockSeconds, Is.Zero);
            Assert.That(FoundryRunDriver.Pips(hud), Is.EqualTo((2, 0)), "Floor 2's two rooms, none reached yet.");
            PlayerProfile saved = TestProfile.ReadSaved();
            Assert.That(saved.Gold, Is.EqualTo(ClimbGold), "The secured gold is banked as the hero descends.");
            Assert.That(saved.DeepestFloorCleared, Is.EqualTo(1));

            // The way down: nothing heals the hero and nothing stands until floor 2's first hall forms up.
            float deadline = Time.realtimeSinceStartup + 10f;
            while (encounters.AliveEnemyCount == 0 && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Keep(setup, hero, false);
                Assert.That(hero.Current, Is.EqualTo(wounded), "Descending does not heal.");
                yield return null;
            }
            Assert.That(encounters.IsDescending, Is.False);
            Assert.That(encounters.RoomNumber, Is.EqualTo(1));
            Assert.That(encounters.CurrentRoom.DisplayName, Is.EqualTo("Upper Hall"));
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(1));
            Assert.That(encounters.IsRoomSiteOpen, Is.False,
                "A floor's first hall is fought where the floor begins: where the hero walked out of the gate below.");
            FoundryMap.GateExitSpot(out float gx, out float gy);
            Vector3 first = encounters.WaveEnemyAt(0).transform.position;
            Assert.That(ArenaFloor.FloorDistanceSquared(first.x, ArenaFloor.FloorY(first.y), gx, gy), Is.LessThan(12.5f * 12.5f),
                "Its pack forms up round the hero there.");
            Assert.That(hero.Current, Is.EqualTo(wounded), "The hero carries its wounds onto floor 2.");
            Assert.That(FoundryRunDriver.Pips(hud), Is.EqualTo((2, 1)), "The HUD's pips start over.");
            Assert.That(FoundryRunDriver.Label("Floor Label"),
                Does.Contain("Floor 2").And.Contain("Room 1/2").And.Contain("Upper Hall"));

            // Floor 2's own horde, on its own clock.
            deadline = Time.realtimeSinceStartup + 5f;
            while (!encounters.IsHordeActive && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Keep(setup, hero, false);
                yield return null;
            }
            Assert.That(encounters.IsHordeActive, Is.True, "Floor 2 has a horde of its own.");
            Assert.That(encounters.FloorClockSeconds, Is.LessThan(.5f).And.LessThan(floorOneClock),
                "Floor 2's clock starts from nothing, not from where floor 1's stopped.");
            Assert.That(encounters.HordeStateNow, Is.EqualTo(HordeState.Ramp), "No hall of floor 2 is behind the hero yet.");
            Assert.That(encounters.IsFloorOvertime, Is.False);
            Assert.That(clock.IsShown, Is.True);
            Assert.That(clock.ShownText, Is.EqualTo("5:00"), "The HUD's floor clock starts over with it.");
            LogAssert.NoUnexpectedReceived();
        }

        // Floor 2's gate - its Warden escorted, its floor overrun - opens on the shipped Final Swarm: a swarm room on the
        // foundry with a way out of its own - the finale's, not the gate's - no gate boss, no elite and no horde, so no
        // floor clock and no overrun though both floors before it ran overrun. Standing still on its way out ends the run,
        // the Descent complete. (GateBossFlowTests.TheSwarmAfterTheGateHasNoWarden pins the bare proof gate before the
        // proof swarm; RunResultTests.WalkingOutOfTheFinaleNamesNoEmptyEnemy the finale's result line.)
        [UnityTest, Timeout(180000)]
        public IEnumerator TheFinalSwarmAfterFloorTwosGateHasItsOwnWayOutAndNoFloorClock()
        {
            // Every floor's clock starts at the overrun, so floors 1 and 2 run overrun from their first second.
            yield return FoundryRunDriver.Load(ClimbFloor, HordeClock.TargetSeconds);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            AttackController heroAttack = hero.GetComponent<AttackController>();
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;
            var seal = setup.GetComponent<ExitSealView>();

            yield return FoundryRunDriver.ClearThroughTheGate(setup, hero);
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current, FoundryRunDriver.CheckpointDescend), Is.True);

            // Floor 2 to its gate, overrun. Its Warden falls first; the Mite beside it holds the gate shut until it falls.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.FloorNumber == 2 && encounters.GateBoss != null);
            Assert.That(encounters.IsFloorOvertime, Is.True, "Floor 2 runs overrun, as the clock file starts it.");
            Health horde = FoundryRunDriver.StandingHordeEnemy(encounters);
            Assert.That(horde, Is.Not.Null, "Floor 2's horde stands round the hero.");
            Assert.That(encounters.WaveEnemyCount, Is.EqualTo(2));
            encounters.GateBoss.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(encounters.GateBoss, Is.Null);
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(1));
            Assert.That(encounters.IsGateExitOpen, Is.False, "The gate stays shut while its wave stands.");
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsGateExitOpen, Is.True, "The last of the gate's wave opens it.");
            yield return FoundryRunDriver.WalkOutOfTheGate(setup, hero);
            yield return FoundryRunDriver.UntilCheckpoint(setup, hero);
            Assert.That(setup.Choices.Current.Cards[FoundryRunDriver.CheckpointDescend].Description, Does.Contain("The Final Swarm"));
            int gold = setup.Run.Gold;
            Assert.That(setup.Choices.TrySelect(setup.Choices.Current, FoundryRunDriver.CheckpointDescend), Is.True);

            yield return FoundryRunDriver.Until(setup, hero, () => encounters.IsInSwarmRoom, 10f);
            Assert.That(encounters.IsInSwarmRoom, Is.True, "Floor 2's gate leads into the Final Swarm.");
            Assert.That(encounters.FloorNumber, Is.EqualTo(3));
            Assert.That(encounters.Floor.Id, Is.EqualTo("floor_final_swarm"));
            Assert.That(encounters.HasNextFloor, Is.False, "Leaving the finale ends the run.");
            Assert.That(setup.Run.SecuredGold, Is.EqualTo(gold), "Everything the climb paid is safe before the finale.");
            Assert.That(setup.Run.UnsecuredGold, Is.Zero);
            Assert.That(horde == null, Is.True, "Floor 2's horde left with its gate.");

            // Its way out: the finale's spot, on the foundry's walkable floor, away from the gate the hero came in by, held
            // at the finale's radius rather than the gate's.
            ExitRule.SpotFor(ArenaGeometry.Foundry, out float ex, out float ey);
            FoundryMap.GateExitSpot(out float gx, out float gy);
            Assert.That(encounters.IsExitOpen, Is.True);
            Assert.That(encounters.IsGateExitOpen, Is.False);
            Assert.That(encounters.ExitFloorX, Is.EqualTo(ex));
            Assert.That(encounters.ExitFloorY, Is.EqualTo(ey));
            Assert.That(encounters.ExitRadius, Is.EqualTo(ExitRule.Radius));
            Assert.That(FoundryMap.Contains(ex, ey, HeroMotion.EdgeMargin), Is.True, "The way out stands on floor the hero can walk.");
            float apart = ExitRule.Radius + ExitRule.GateRadius;
            Assert.That(ArenaFloor.FloorDistanceSquared(ex, ey, gx, gy), Is.GreaterThan(apart * apart),
                "A hero arriving from the gate is not already on the way out.");
            yield return null;
            Assert.That(seal.IsVisible, Is.True, "The way out is marked.");
            Assert.That(seal.ExitPosition.x, Is.EqualTo(ex).Within(1e-4f));

            // No Warden, no elite, no horde and no clock, however long the swarm runs.
            float end = Time.time + 2f, deadline = Time.realtimeSinceStartup + 20f;
            while (Time.time < end && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Keep(setup, hero);
                Assert.That(encounters.GateBoss, Is.Null, "No enemy of the swarm is the gate boss.");
                Assert.That(setup.Warden.IsFighting, Is.False, "The Warden's slam has nobody to swing.");
                Assert.That(heroAttack.GateBoss, Is.Null);
                Assert.That(encounters.EliteLeader, Is.Null);
                Assert.That(encounters.Elites, Is.Empty);
                Assert.That(encounters.IsHordeActive, Is.False);
                Assert.That(encounters.HordeAliveCount, Is.Zero);
                Assert.That(encounters.FloorClockSeconds, Is.Zero);
                Assert.That(encounters.IsFloorOvertime, Is.False, "The finale is never overrun.");
                Assert.That(clock.IsShown, Is.False, "Nor does the HUD show a floor clock in it.");
                yield return null;
            }
            Assert.That(Time.time, Is.GreaterThanOrEqualTo(end), "The swarm ran.");
            Assert.That(encounters.AliveEnemyCount, Is.GreaterThan(0), "Its crowd walked in.");

            // Standing still on the way out leaves the finale, and the Descent with it.
            FoundryRunDriver.Place(hero, ex, ey);
            deadline = Time.realtimeSinceStartup + 15f;
            while (!setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Keep(setup, hero);
                yield return null;
            }
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Victory), "Leaving the finale completes the Descent.");
            Assert.That(encounters.IsInSwarmRoom, Is.False);
            Assert.That(encounters.TotalRoomsCleared, Is.EqualTo(7), "Floor 1's four rooms, floor 2's two and the finale's one.");
            Assert.That(setup.Run.GoldBanked, Is.EqualTo(setup.Run.Gold));
            yield return null;
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.True);
            Assert.That(FoundryRunDriver.Label("Result Title"), Is.EqualTo("Ascent complete"));
            Assert.That(FoundryRunDriver.Label("Progress Label"), Does.Contain("Floor 3").And.Contain("7 rooms"));
            Assert.That(TestProfile.ReadSaved().DeepestFloorCleared, Is.EqualTo(3));
            LogAssert.NoUnexpectedReceived();
        }

        // The opened gate counts the hero's hold only while it stands still on it, as the finale's way out does: a hero
        // stepping about inside the gate's ring, however long, has not walked out (EncounterController.TickGateExit).
        [UnityTest, Timeout(90000)]
        public IEnumerator TheOpenedGateCountsOnlyWhileTheHeroStandsStill()
        {
            yield return FoundryRunDriver.Load("floor_gate_boss_proof");
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            yield return FoundryRunDriver.Until(setup, hero, () => encounters.GateBoss != null, 20f);
            Assert.That(encounters.GateBoss, Is.Not.Null, "The gate floor opens with its boss.");
            PackTestUtility.KillWave(encounters);
            Assert.That(encounters.IsGateExitOpen, Is.True);
            yield return FoundryRunDriver.Until(setup, hero, () => !encounters.IsRevealHeld, 10f);
            Assert.That(encounters.IsRevealHeld, Is.False, "The camera is back on the hero.");

            FoundryMap.GateExitSpot(out float gx, out float gy);
            FoundryRunDriver.Place(hero, gx, gy);
            yield return FoundryRunDriver.Hold(setup, hero, .5f);
            Assert.That(encounters.IsGateExitOpen, Is.True, "Half a hold is no walk out.");
            Assert.That(encounters.ExitHeldSeconds, Is.GreaterThan(0f), "Standing still on the gate counts.");

            // A step every frame, never off the gate's hold.
            float end = Time.time + 1.5f, deadline = Time.realtimeSinceStartup + 20f;
            int step = 0;
            while (Time.time < end && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Place(hero, gx + (step++ % 2 == 0 ? .1f : -.1f), gy);
                yield return null;
                FoundryRunDriver.Keep(setup, hero);
                Assert.That(encounters.ExitHeldSeconds, Is.Zero, "A step starts the hold again.");
            }
            Assert.That(Time.time, Is.GreaterThanOrEqualTo(end));
            Assert.That(encounters.IsGateExitOpen, Is.True, "A second and a half on the gate, never still, is no walk out.");
            Assert.That(setup.Run.HasEnded, Is.False);

            // Still again: a full second from the last step and the hero is out.
            float still = Time.time;
            deadline = Time.realtimeSinceStartup + 10f;
            while (encounters.IsGateExitOpen && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Keep(setup, hero);
                yield return null;
            }
            Assert.That(encounters.IsGateExitOpen, Is.False);
            Assert.That(Time.time - still, Is.GreaterThanOrEqualTo(ExitRule.DwellSeconds - .05f), "The hold starts over from the last step.");
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Victory), "With no floor above, walking out of the gate ends the run.");
            LogAssert.NoUnexpectedReceived();
        }

        // The climb the game ships, on the foundry from its first hall to the end of the Final Swarm: floor 1 as the
        // foundry plays it - its halls at their districts, the forge, the Captain twice, the gate - its gate's wait skipped
        // by starting each floor's clock past it (floors 2 and 3 wait for the same 3:30, docs/47 and docs/48); floor 2;
        // floor 3 (Gloam Gardens, docs/48); the finale. Every room is played in the order FoundryRooms gives, the HUD's pips
        // grow past the scene's six for floor 1's seven, the finale has no gate boss, and its chest stands where the hero
        // arrives and opens under it (docs/47). Every checkpoint descends, every offer takes its first card, the forge its
        // first option.
        [UnityTest, Timeout(420000)]
        public IEnumerator TheShippedClimbPlaysThroughToTheEndOfTheFinalSwarm()
        {
            yield return FoundryRunDriver.Load(null, PastFloorOnesGate);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            var hud = Object.FindFirstObjectByType<RunHud>();
            Assert.That(encounters.Floor.Id, Is.EqualTo("floor_ember_halls"), "The scene's own floor: the climb the game ships.");
            Assert.That(encounters.Floor.FoundryGateNotBeforeSeconds, Is.LessThan(PastFloorOnesGate),
                "The clock starts past floor 1's gate time.");

            var expected = new List<string>();
            int height = 1;
            for (FloorDefinition floor = encounters.Floor; floor != null; floor = floor.NextFloor, height++)
            {
                RoomDefinition[] rooms = EncounterController.FoundryRooms(floor);
                for (int r = 0; r < rooms.Length; r++)
                    expected.Add($"{height}.{r + 1}:{rooms[r].DisplayName}");
            }

            // Floor 1's seventh pip is grown one step along the scene's row and painted as the rest are.
            Assert.That(encounters.RoomCount, Is.EqualTo(7));
            Assert.That(hud.RoomPipCount, Is.EqualTo(7), "A pip a room, one past the scene's six.");
            Vector2 sixth = hud.RoomPipAt(5).rectTransform.anchoredPosition;
            Vector2 step = sixth - hud.RoomPipAt(4).rectTransform.anchoredPosition;
            Vector2 seventh = hud.RoomPipAt(6).rectTransform.anchoredPosition;
            Assert.That(seventh.x, Is.EqualTo(sixth.x + step.x).Within(.01f), "One step further along the row.");
            Assert.That(seventh.y, Is.EqualTo(sixth.y + step.y).Within(.01f));
            yield return null;
            Assert.That(hud.RoomPipAt(6).sprite, Is.SameAs(hud.RoomPipAt(5).sprite), "Painted as the rest are.");

            var played = new List<string>();
            var pips = new List<(int Shown, int Lit)>();
            string last = null;
            int floorSeen = 0;
            bool wardenInTheFinale = false;
            var chests = Object.FindFirstObjectByType<ChestSpawner>();
            Assert.That(chests, Is.Not.Null);
            float chestFromArrival = float.NaN;
            bool finaleChestOpened = false;
            float deadline = Time.realtimeSinceStartup + 330f;
            while (!setup.Run.HasEnded && Time.realtimeSinceStartup < deadline)
            {
                FoundryRunDriver.Keep(setup, hero);
                RoomDefinition room = encounters.CurrentRoom;
                if (room != null)
                {
                    string key = $"{encounters.FloorNumber}.{encounters.RoomNumber}:{room.DisplayName}";
                    if (key != last)
                        played.Add(last = key);
                    // The pips as each floor's first room shows them.
                    if (encounters.FloorNumber != floorSeen)
                    {
                        floorSeen = encounters.FloorNumber;
                        pips.Add(FoundryRunDriver.Pips(hud));
                    }
                }
                ChoicePrompt prompt = setup.Choices.Current;
                if (prompt != null && prompt.Kind == ChoiceKind.Checkpoint)
                    Assert.That(setup.Choices.TrySelect(prompt, FoundryRunDriver.CheckpointDescend), Is.True, "Descend.");
                else if (encounters.IsInSwarmRoom)
                {
                    wardenInTheFinale |= encounters.GateBoss != null || setup.Warden.IsFighting;
                    // The finale's first frame: its chest where the hero arrived, left a frame to open under it.
                    if (float.IsNaN(chestFromArrival))
                        chestFromArrival = Vector2.Distance(new Vector2(chests.ChestFloorX, chests.ChestFloorY),
                            new Vector2(encounters.HeroFloorX, encounters.HeroFloorY));
                    else
                    {
                        finaleChestOpened |= chests.IsOpen;
                        FoundryRunDriver.Place(hero, encounters.ExitFloorX, encounters.ExitFloorY);
                    }
                }
                else if (encounters.IsGateExitOpen)
                {
                    FoundryMap.GateExitSpot(out float gx, out float gy);
                    FoundryRunDriver.Place(hero, gx, gy);
                }
                else if (encounters.IsRoomSiteOpen)
                    FoundryRunDriver.Place(hero, encounters.RoomSiteFloorX, encounters.RoomSiteFloorY);
                else
                    PackTestUtility.KillWave(encounters);
                yield return null;
            }

            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Victory), $"The climb ends at the finale's way out; it stopped at {last}.");
            Assert.That(played, Is.EqualTo(expected), "Every room of the climb, in the order the foundry plays it.");
            Assert.That(pips, Is.EqualTo(new[] { (7, 1), (6, 1), (6, 1), (1, 1) }),
                "A pip a room: floor 1's seven, floor 2's six, floor 3's six, the finale's one; the first reached on each.");
            Assert.That(wardenInTheFinale, Is.False, "No enemy of the finale is the gate boss.");
            Assert.That(chestFromArrival, Is.LessThanOrEqualTo(ChestRule.PickupRadius), "The finale's chest stands where the hero arrives.");
            Assert.That(finaleChestOpened, Is.True, "The hero opens it standing there.");
            Assert.That(encounters.TotalRoomsCleared, Is.EqualTo(expected.Count));
            Assert.That(expected.Count, Is.EqualTo(20), "Floor 1's seven rooms, floor 2's six, floor 3's six, the finale's one.");
            Assert.That(TestProfile.ReadSaved().DeepestFloorCleared, Is.EqualTo(4));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
