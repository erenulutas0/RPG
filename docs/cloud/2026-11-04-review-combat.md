# Review: combat and flow, 84d1cdb..4cda1cc (2026-11-04)

An independent correctness review of the last two nights' combat and flow work: the gate's hold for the floor's clock,
the foundry's extra elite, the Warden's held weapon and its slam clock, the autopilot's bounded drop detour and the
room's `gate_hold_sec`. Branch `claude/review-combat-1104`, from main 4cda1cc.

Scope read hunk by hunk with the surrounding files and their callers: `EncounterController`, `WardenRule`
(`WardenSlamClock`), `AttackController`, `FloorProgress`, `DropDetourRule`, `WardenSlamService`, `DevelopmentAutopilot`,
`FloorDefinition`, `RunTelemetry` and `RunTelemetryBridge`, `Floor_EmberHalls.asset`, `Floor_GateHoldProof.asset`. Also
their tests (EditMode FoundryGateTests, WardenRuleTests, DropDetourRuleTests, FloorTests, ClimbFloorContentTests,
RunTelemetryTests; PlayMode GateHoldFlowTests, WardenFlowTests, HallSurgeFlowTests), and the simulation's copies of each
rule (`DescentSimulation.Foundry.cs`: `StepWarden`, `EnemyAttack`, `PilotGoal`, the gate hold, `LaterFloorLevers`) for
parity.

## Result

**I found no correctness defect in the scoped gameplay code.** Every failure mode in the brief was traced to a concrete
path, listed below, and each one holds. The only changes on the branch are two coverage tests for edges no test pinned
yet, and a comment fix:

| # | File:line | What | Severity | Status | Verified |
|---|---|---|---|---|---|
| 1 | Assets/_Project/Tests/Support/FoundryGate.cs:18 | A comment calls Ilya "hers", but characters are not gendered | Style (project rule) | Fixed: "Ilya's" | Read |
| 2 | Assets/_Project/Scripts/UI/RunFeedbackView.cs:23, Assets/_Project/Tests/PlayMode/PaintedRunFeedbackTests.cs:133 | The hero is "he"/"his" in a comment and in an assertion message (this is outside the diff's combat scope; I found it by grepping the diff for pronouns) | Style (project rule) | Fixed: "it"/"its", in the comment and the message only | Read; the PlayMode change is to a message string and needs no Unity run |
| 3 | Assets/_Project/Tests/EditMode/WardenRuleTests.cs | No test covered an enrage during the recovery, or a reset under a mark or during the recovery | Coverage gap | Two tests added: `AnEnrageDuringTheRecoveryKeepsItAndBringsOnlyTheNextMarkSooner`, `AResetUnderAMarkOrInTheRecoveryLeavesNothingHeld` | CombatChecks: both pass |

Tests: `dotnet test Tools/CombatChecks -c Release` passed 682 of 682 on main 4cda1cc and passes 684 of 684 on the
branch, with 0 failures. The two [Explicit] tests are listed as skipped. This ran on the .NET 10.0.114 SDK with
DOTNET_ROLL_FORWARD=Major and `-p:NuGetAudit=false`, because no .NET 9 SDK could be installed here: dot.net's installer
host is blocked by the proxy, and apt has no 9.0 package. The lead's own baseline count (658) predates later merges
into main.

## Checked and found sound

**The gate's hold (`EncounterController`)**
- `GateHoldsForClock` (EncounterController.cs:626) runs only after `_fight.Tick` returns true. `EncounterProgress.Tick`
  returns true on every frame after the advance delay, not once, so the hold is re-read each frame and lifts on the
  first frame the clock reaches the gate time. A choice that is open freezes both the delay and the horde clock.
- The condition matches the simulation's (`NextIsGate` with `_horde.Seconds`) term for term: it applies on the foundry
  only, with a horde, a gate time above 0, the last wave of the room, a boss room next, and the clock below the gate
  time.
- Reset paths: `EnterFloor` (on Initialize and on every descent), `AdvanceFloor`, and `OnHeroDied`. Initialize throws
  if it is called a second time, so `_hero.Died` is subscribed exactly once, and `OnDestroy` unsubscribes it. A restart
  reloads the scene (CombatSetup.cs:269), and Health has no revive. After a death, `Update` returns while
  `!_hero.IsAlive`, so nothing can set the hold again.
- `ValidateGateHold` rejects every unsafe case: NaN, a negative time, a time at or past 300 s, no horde, no boss, and a
  forge or swarm room before a boss as the foundry plays the floor (GateHoldFlowTests and ClimbFloorContentTests cover
  these). On the hall arena the authored rooms play and `GateHoldsForClock` is false, so a floor that validates through
  `FoundryRooms` never holds wrongly there. A development clock start (`FloorClockStart`) past the gate time simply
  opens the gate as the room clears.
- Floor 2 (`Floor_QuicksilverVaults.asset`) has none of the new fields, so they serialize as 0: no hold, no extra elite,
  no surges. The simulation's `LaterFloorLevers` copies the floor's own knobs, so floor 2 gets 0 there too, not floor
  1's defaults of 210 s and one elite.
- `FloorProgress.IsLastWaveOfRoom` guards a room index of -1, forge rooms (zero waves), endless rooms and a completed
  floor (FloorTests.OnlyARoomWithWavesHasALastWave).

**The extra elite (`FoundryRooms` / `_rooms`)**
- No caller still indexes the authored floor by the played room index. The only `RoomAt`/`RoomCount` reads left are in
  validation, `FoundryRooms`, `RoomsAsPlayed` and `LaterHalls`. `RunHud`, `RunResultView` and `RunTelemetryBridge` read
  `RoomCount` and `CurrentRoom`, which now come from `_rooms`.
- The copied elite is the same `RoomDefinition` instance as the elite it copies. Nothing keys state on a room's
  identity: there is no `Dictionary`/`HashSet` of rooms, and the density probe was moved from identity to index 0 in
  the same diff.
- The horde's state: `HallsCleared` counts one more hall before the boss, and `HordeRule.StateFor` saturates at
  Pressure for 2 or more, so the extra elite changes nothing.
- The district route: the extra elite takes ordinal 4, which on floor 1 is the north court. Floor 2 starts from the
  authored `LaterHalls` (3), so its first hall is also the north court. This is intended and documented (FoundryMap.cs
  comment, ClimbFloorContentTests), not a defect.

**The Warden's slam and the held weapon (`WardenSlamService`, `WardenSlamClock`, `AttackController.IsHeld`)**
- The Warden dies under a mark: `Boss` returns null as soon as `!IsAlive`, so no blow lands from a dead Warden. On the
  next frame `GateBoss` turns null (Defeated), and the service calls `Hold(false)` on the old controller and resets the
  clock, which clears `IsMarked`. `WardenSlamView` reads `IsFighting && Clock.IsMarked`, so the mark goes with it.
  Enemies are instantiated and destroyed rather than pooled, so a held `AttackController` cannot be reused. Floor 2's
  Warden is a new `GateBoss`, which gets a reset clock and `IsHeld` false.
- The hero dies under a mark: the blow checks `_hero.IsAlive`. Once the run has ended the service returns before
  `Hold`, which leaves the Warden's flag as it was, but the run is over and a restart reloads the scene. Extracting
  leaves the scene too.
- Pause, choices, the reveal and `Time.deltaTime <= 0`: the clock does not advance and the hold keeps its last value.
  `AttackController.Update` also returns on a zero delta time. Scaled time slows the clock and the weapon alike.
- Across an enrage: the blow's time is fixed when the mark goes down (`_blowAt`). An enrage under the mark keeps the
  whole telegraph (the existing tests), and an enrage in the recovery keeps the 1.5 s recovery and brings only the next
  mark sooner (the new test). `RecoverySeconds` (1.5) is below the enraged `SlamEveryEnraged - Telegraph`, which
  WardenRuleTests asserts.
- Order: the service at -40 runs after `EncounterController` (-50) and before the default order's `AttackController`.
  The simulation steps `StepWarden` before `EnemyAttack`. Both read the hold the same way, and `HoldsWeaponFor` is the
  one body behind both.
- `IsHeld` stops `Update` (the cooldown does not tick and nothing strikes) and `StrikeDue`, which `EnemyLookView`'s
  windup reads. Only the service sets it.

**The drop detour (`DropDetourRule`, `DevelopmentAutopilot`)**
- `Track` runs at the top of `Goal` on every frame the pilot chooses, before any return. `fighting` is
  `AliveEnemyCount`, which counts the wave only, not the horde. If the horde counted, the budget would reset on every
  frame and never bind.
- Branches match the old order: lingering takes the nearest drop within `LingerDetour`, then the altar or hall. With
  nowhere to go (waiting for the gate) it takes the nearest drop within 4 units. On the way to an altar or a hall it
  only takes a drop beside the way, while the budget lasts. Pause charges a zero delta time. Choices return earlier in
  `Update`. The simulation's `PilotGoal` has the same structure (its exit check is the pre-existing `_gateExitOpen`).
- `AddedPath` is one non-inlined body using `Math.Sqrt` (no `Math.Pow` and no `System.Random`).

**Telemetry (`RunTelemetry.gate_hold_sec`)**
- The bridge sets the hold before `Tick` each frame, after the encounter has run, and the value is never read stale
  because it is set before every `Tick`. Only Active time counts, so choices and pauses are excluded (RunTelemetryTests
  covers this).
- A room closes on the room-number change in `ObserveProgress`, so the hold's seconds stay in the held room and the
  gate room never carries them. On a death in the hold, `OnHeroDied` clears the flag, and the room closes with its
  accumulated `gate_hold_sec`, which is correct. The key is written only when it is above 0, after every existing key.

**PlayMode tests (read, not run)**
- GateHoldFlowTests writes its development files into the per-test `TestProfile` folder, which `End` deletes, so the
  297 s clock start of `AnOverrunFloorSaysSoOverTheGatesCount` cannot leak into another test.

## Not covered

- I did not audit the reporting code in `FoundryGate.cs` and `FoundryBatch*.cs` beyond the percentile and median
  helpers (NaN-filtered, linear interpolation) and the row statuses read above.
- The UI diffs (FloorClockView, RunHud, Foundry*View) are outside this review's scope. FloorClockView's overrun-over-gate
  precedence was read because it calls `IsHoldingForGate`.
