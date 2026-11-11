using System.Collections;
using Cryptforge.Combat;
using Cryptforge.Core;
using Cryptforge.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Cryptforge.Tests
{
    // A foundry run that ends on the floor ends there cleanly: a hero fallen while the cleared hall holds for the gate,
    // or under the gate boss, leaves no hold, no gate boss walking out after it, no slam, no horde walking on and no floor
    // clock behind the result screen; and Try again starts the climb afresh, nothing of the fallen run left - no drop, no
    // elite, no gate boss, no hold, a horde on a clock of its own. The hall arena's run-end tests (RunResultTests,
    // TelemetryFlowTests) have none of these.
    // Resources/Development: Floor_GateHoldProof (a hall of two Cinder Mites, the Warden's gate held to 0:12 of the
    // floor's clock, a horde of Mites), Floor_GateBossProof (the Warden alone at the gate) and Floor_FoundryClimbProof (a
    // hall, the Captain's elite room twice, the Warden's gate, a horde of Mites).
    public sealed class FoundryRunEndFlowTests
    {
        private const string HoldFloor = "floor_gate_hold_proof";
        private const string GateFloor = "floor_gate_boss_proof";
        private const string ClimbFloor = "floor_foundry_climb_proof";
        // Floor_GateHoldProof's gate opens at 0:12 of the floor's clock. Started at 0:06, its cleared hall holds for about
        // five seconds.
        private const float GateSeconds = 12f, ClockStart = 6f;

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
            SceneManager.SetActiveScene(SceneManager.CreateScene("Foundry run end cleanup"));
            yield return SceneManager.UnloadSceneAsync(old);
            TestProfile.End();
        }

        // A hero who falls while the cleared hall holds for the gate ends the hold with the run; well past the time the
        // gate would have opened, the floor's clock, the horde and the gate stand as the fall left them, behind the result.
        // Try again then starts the floor afresh: no hold, no gate count, the clock started anew, and the new run's own
        // hall holds for the gate once cleared.
        [UnityTest, Timeout(120000)]
        public IEnumerator AFallWhileTheGateHoldsEndsTheRunAndTheGateNeverOpens()
        {
            yield return FoundryRunDriver.Load(HoldFloor, ClockStart);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            FloorClockView clock = Object.FindFirstObjectByType<RunHud>().FloorClock;
            Assert.That(encounters.Floor.FoundryGateNotBeforeSeconds, Is.EqualTo(GateSeconds));

            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.IsHoldingForGate && encounters.HordeAliveCount > 0);
            Assert.That(encounters.GateOpensIn, Is.GreaterThan(0f));
            Assert.That(clock.IsGateShown, Is.True, "The floor clock says when the gate opens.");
            Health horde = FoundryRunDriver.StandingHordeEnemy(encounters);
            Assert.That(horde, Is.Not.Null);
            Vector3 hordeAt = horde.transform.position;
            float clockAtFall = encounters.FloorClockSeconds;
            Assert.That(clockAtFall, Is.LessThan(GateSeconds));
            int walkedIn = 0;
            System.Action<int> counted = slot => walkedIn++;
            encounters.HordeEnemySpawned += counted;

            hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(hero.IsAlive, Is.False);
            Assert.That(encounters.IsHoldingForGate, Is.False, "The hold ends with the hero, at once.");
            Assert.That(encounters.GateOpensIn, Is.Zero);
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Defeat));
            yield return null;
            var result = Object.FindFirstObjectByType<RunResultView>();
            Assert.That(result.IsOpen, Is.True, "The result screen shows.");
            Assert.That(FoundryRunDriver.Label("Result Title"), Is.EqualTo("Defeated"));
            Assert.That(FoundryRunDriver.Label("Progress Label"), Does.Contain("Floor 1"));

            // A second past when the gate would have opened: nothing moves on behind the result.
            yield return FoundryRunDriver.Hold(setup, hero, GateSeconds - clockAtFall + 1f);
            encounters.HordeEnemySpawned -= counted;
            Assert.That(encounters.FloorClockSeconds, Is.EqualTo(clockAtFall), "The floor's clock stopped with the hero.");
            Assert.That(encounters.IsHoldingForGate, Is.False);
            Assert.That(encounters.GateOpensIn, Is.Zero);
            Assert.That(encounters.GateBoss, Is.Null, "The gate boss never walks out.");
            Assert.That(encounters.RoomNumber, Is.EqualTo(1));
            Assert.That(encounters.IsGateExitOpen, Is.False);
            Assert.That(setup.Warden.IsFighting, Is.False, "Nothing slams.");
            Assert.That(setup.GetComponent<BossRevealView>().IsRevealing, Is.False);
            Assert.That(walkedIn, Is.Zero, "No horde enemy walks in after the fall.");
            Assert.That(horde.transform.position, Is.EqualTo(hordeAt), "Nor does the horde walk on.");
            Assert.That(clock.IsShown, Is.False, "The floor clock went with the run.");
            Assert.That(result.IsOpen, Is.True);

            // Try again: the same floor afresh.
            yield return FoundryRunDriver.Restart(setup);
            setup = FoundryRunDriver.Setup;
            encounters = setup.Encounters;
            hero = FoundryRunDriver.Hero;
            clock = Object.FindFirstObjectByType<RunHud>().FloorClock;
            Assert.That(horde == null, Is.True, "The fallen run's horde is gone with its scene.");
            Assert.That(setup.Run.HasEnded, Is.False);
            Assert.That(hero.IsAlive, Is.True);
            Assert.That(setup.Drops.Count, Is.Zero, "No drop lies from the fallen run.");
            Assert.That(encounters.RoomNumber, Is.EqualTo(1));
            Assert.That(encounters.AliveEnemyCount, Is.EqualTo(2), "The hall's two Mites stand again.");
            Assert.That(encounters.IsHoldingForGate, Is.False, "No hold is left from the fallen run.");
            Assert.That(encounters.GateOpensIn, Is.Zero);
            Assert.That(encounters.FloorClockSeconds, Is.LessThan(clockAtFall), "The floor's clock starts anew.");
            Assert.That(clock.IsGateShown, Is.False);
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.IsHoldingForGate);
            Assert.That(encounters.GateOpensIn, Is.EqualTo(GateSeconds - encounters.FloorClockSeconds).Within(.001f),
                "The new run's own hall holds for its gate.");
            LogAssert.NoUnexpectedReceived();
        }

        // The Warden's blow is the hero's last: the run ends a defeat by the Forge Warden, and the slam service stops with
        // it - no mark goes down and no blow lands however long the result stands, its clock where the blow left it.
        [UnityTest, Timeout(90000)]
        public IEnumerator AFallToTheWardensSlamEndsTheRunAndStopsItsHammer()
        {
            yield return FoundryRunDriver.Load(GateFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            WardenSlamService warden = setup.Warden;
            yield return FoundryRunDriver.Until(setup, hero, () => warden.IsFighting && !encounters.IsRevealHeld, 30f);
            Assert.That(warden.IsFighting, Is.True, "The gate boss is fought.");
            Health boss = warden.Boss;
            AttackController hammer = boss.GetComponent<AttackController>();
            bool marked = false;
            bool? hit = null;
            warden.Marked += () => marked = true;
            warden.Slammed += caught =>
            {
                if (!hit.HasValue)
                    hit = caught;
            };
            yield return FoundryRunDriver.Until(setup, hero, () => marked, WardenRule.SlamEvery + 5f);
            Assert.That(marked, Is.True, "The Warden marks where the hero stands.");
            Assert.That(hammer.IsHeld, Is.True, "Its own weapon waits while the hammer is up.");

            // A blow's worth of health or less left, and the hero stays on the mark.
            hero.ApplyDamage(new DamageContext(hero.Current - 1f));
            Assert.That(hero.IsAlive, Is.True);
            Assert.That(hero.Current, Is.LessThan(hero.Maximum * WardenRule.HeroShare));
            yield return FoundryRunDriver.Until(setup, hero, () => hit.HasValue, WardenRule.Telegraph + 3f, false);
            Assert.That(hit, Is.True, "A hero who stays on the mark is caught.");
            Assert.That(hero.IsAlive, Is.False);
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Defeat));
            Assert.That(setup.LastAttacker, Is.Not.Null);
            Assert.That(setup.LastAttacker.DisplayName, Is.EqualTo("Forge Warden"), "The blow is the Warden's.");
            yield return null;
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.True);
            Assert.That(FoundryRunDriver.Label("Result Title"), Is.EqualTo("Defeated"));
            Assert.That(FoundryRunDriver.Label("Cause Label"), Does.Contain("Forge Warden").And.Contain("Warden's Gate"));

            // Past when the next mark was due: the service has stopped.
            int marks = 0, slams = 0;
            warden.Marked += () => marks++;
            warden.Slammed += caught => slams++;
            float seconds = warden.Clock.Seconds;
            int swings = hammer.AttackCount;
            yield return FoundryRunDriver.Hold(setup, hero, WardenRule.SlamEvery + .5f, false);
            Assert.That(marks, Is.Zero, "No mark goes down after the fall.");
            Assert.That(slams, Is.Zero, "No blow lands.");
            Assert.That(warden.Clock.Seconds, Is.EqualTo(seconds), "The slam's clock stopped with the run.");
            Assert.That(warden.Clock.IsMarked, Is.False);
            Assert.That(setup.GetComponent<WardenSlamView>().IsMarkShown, Is.False, "The blow's flash is spent; no mark is down.");
            Assert.That(hammer.AttackCount, Is.EqualTo(swings), "Nothing strikes a fallen hero.");
            Assert.That(encounters.IsHoldingForGate, Is.False);
            LogAssert.NoUnexpectedReceived();
        }

        // A hero who falls while the Warden's mark is down - to anything but the blow, as a horde's bite does on the shipped
        // floors - ends the fight there and no blow lands. The mark and the raised hammer should go with the fight, as the
        // view withdraws a mark when the fight stops (WardenSlamView: "A mark withdrawn without a blow (the boss fell or
        // changed, the fight stopped): the hammer comes down"); the service stops with the run but keeps its mark down.
        [UnityTest, Timeout(90000)]
        public IEnumerator AFallUnderTheWardensMarkTakesTheMarkAway()
        {
            yield return FoundryRunDriver.Load(GateFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            WardenSlamService warden = setup.Warden;
            var view = setup.GetComponent<WardenSlamView>();
            yield return FoundryRunDriver.Until(setup, hero, () => warden.IsFighting && !encounters.IsRevealHeld, 30f);
            Assert.That(warden.IsFighting, Is.True, "The gate boss is fought.");
            var look = warden.Boss.GetComponent<EnemyLookView>();
            bool marked = false;
            int slams = 0;
            warden.Marked += () => marked = true;
            warden.Slammed += caught => slams++;
            yield return FoundryRunDriver.Until(setup, hero, () => marked, WardenRule.SlamEvery + 5f);
            Assert.That(marked, Is.True, "The Warden marks where the hero stands.");
            yield return null;
            Assert.That(view.IsMarkShown, Is.True, "The mark is down.");
            Assert.That(look.IsWindingUp, Is.True, "The Warden's hammer is up.");

            hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(setup.Run.HasEnded, Is.True);
            // Past the moment the blow would have landed.
            yield return FoundryRunDriver.Hold(setup, hero, WardenRule.Telegraph + .5f, false);
            Assert.That(slams, Is.Zero, "No blow lands on a fallen hero.");
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.True);
            bool markShown = view.IsMarkShown, raised = look.IsWindingUp;
            Assert.That((markShown, raised), Is.EqualTo((false, false)),
                $"Behind the result the mark is {(markShown ? "still down" : "gone")} and the Warden's hammer {(raised ? "still raised" : "down")}.");
            LogAssert.NoUnexpectedReceived();
        }

        // A run that falls in the elite room - its Captain standing, the horde round the hero, a drop lying - starts afresh
        // on Try again: no drop, no elite, no gate boss, no hold, a horde on a clock of its own; and the new run's own elite
        // room and gate hold only their own.
        [UnityTest, Timeout(180000)]
        public IEnumerator ARestartAfterAFallInTheEliteRoomStartsTheClimbAfresh()
        {
            yield return FoundryRunDriver.Load(ClimbFloor);
            CombatSetup setup = FoundryRunDriver.Setup;
            EncounterController encounters = setup.Encounters;
            Health hero = FoundryRunDriver.Hero;
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.EliteLeader != null);
            Health captain = encounters.EliteLeader;
            yield return FoundryRunDriver.Hold(setup, hero, 1f);
            yield return FoundryRunDriver.Until(setup, hero, () => FoundryRunDriver.StandingHordeEnemy(encounters) != null, 10f);
            Health horde = FoundryRunDriver.StandingHordeEnemy(encounters);
            Assert.That(horde, Is.Not.Null, "The horde stands round the hero.");
            Assert.That(captain.IsAlive, Is.True, "The Captain stands.");
            Assert.That(encounters.Elites, Has.Member(captain));
            Assert.That(setup.Drops.Count, Is.GreaterThan(0), "The first hall's Mite left its drop where it fell.");
            float clockAtFall = encounters.FloorClockSeconds;
            hero.ApplyDamage(new DamageContext(PackTestUtility.Lethal));
            Assert.That(setup.Run.Outcome, Is.EqualTo(RunOutcome.Defeat));
            yield return null;
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.True);

            yield return FoundryRunDriver.Restart(setup);
            setup = FoundryRunDriver.Setup;
            encounters = setup.Encounters;
            hero = FoundryRunDriver.Hero;
            AttackController heroAttack = hero.GetComponent<AttackController>();
            Assert.That(captain == null, Is.True, "The fallen run's Captain is gone with its scene.");
            Assert.That(horde == null, Is.True, "And its horde.");
            Assert.That(setup.Run.HasEnded, Is.False);
            Assert.That(setup.Run.Gold, Is.Zero);
            Assert.That(setup.Run.Experience, Is.Zero);
            Assert.That(setup.Run.Level, Is.Zero);
            Assert.That(setup.Drops.Count, Is.Zero, "No drop lies from the fallen run.");
            Assert.That(hero.IsAlive, Is.True);
            Assert.That(hero.Current, Is.EqualTo(hero.Maximum));
            Assert.That(encounters.FloorNumber, Is.EqualTo(1));
            Assert.That(encounters.RoomNumber, Is.EqualTo(1));
            Assert.That(encounters.CurrentRoom.Kind, Is.EqualTo(RoomKind.Combat));
            Assert.That(encounters.Elites, Is.Empty, "No elite is left standing from the fallen run.");
            Assert.That(encounters.EliteLeader, Is.Null);
            Assert.That(encounters.GateBoss, Is.Null);
            Assert.That(encounters.IsBossEnraged, Is.False);
            Assert.That(encounters.IsHoldingForGate, Is.False);
            Assert.That(encounters.IsGateExitOpen, Is.False);
            Assert.That(encounters.IsRoomSiteOpen, Is.False);
            Assert.That(heroAttack.Elites, Is.SameAs(encounters.Elites), "The new hero strikes with the new run's elites.");
            Assert.That(heroAttack.GateBoss, Is.Null);
            Assert.That(setup.Warden.IsFighting, Is.False);
            Assert.That(setup.GetComponent<BossRevealView>().IsRevealing, Is.False);
            Assert.That(Object.FindFirstObjectByType<RunResultView>().IsOpen, Is.False);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            yield return FoundryRunDriver.Until(setup, hero, () => encounters.IsHordeActive, 5f);
            Assert.That(encounters.IsHordeActive, Is.True);
            Assert.That(encounters.FloorClockSeconds, Is.LessThan(clockAtFall), "The new run's horde runs on a clock of its own.");
            Assert.That(encounters.HordeStateNow, Is.EqualTo(HordeState.Ramp));

            // The new run's elite room has its own Captain, the one elite; its gate, a Warden as fresh.
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.EliteLeader != null);
            Assert.That(encounters.Elites, Is.EqualTo(new[] { encounters.EliteLeader }));
            yield return FoundryRunDriver.Drive(setup, hero, () => encounters.GateBoss != null);
            Health gateBoss = encounters.GateBoss;
            Assert.That(encounters.Elites, Is.Empty);
            Assert.That(gateBoss.Current, Is.EqualTo(gateBoss.Maximum));
            Assert.That(encounters.IsBossEnraged, Is.False);
            yield return null;
            Assert.That(setup.Warden.Boss, Is.SameAs(gateBoss));
            Assert.That(setup.Warden.IsEnraged, Is.False);
            LogAssert.NoUnexpectedReceived();
        }
    }
}
