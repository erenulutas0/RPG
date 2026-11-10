# Review: the foundry simulation and the V0.1 gate against the scene (2026-11-04)

This is a review of `git diff 84d1cdb..4cda1cc` for `DescentSimulation.Foundry.cs`, `FoundryBatch.cs`, `FoundryBatch.FloorTwo.cs`,
`FoundryGate.cs`, `Tools/CombatChecks/FoundryBatchReport.cs` and `CombatChecks.csproj`. I compared the simulation against the
scene code it mirrors, then checked `FoundryGate`'s statistics. Branch: `claude/review-sim-parity-1104`, from main 4cda1cc.
Line numbers are from the branch.

**Result:**
- No behaviour divergence between the simulation and the scene, so no outcome moves.
- Two of the gate's statistics were wrong. Both are fixed in pure code with CombatChecks tests. Both change labels and counts in
  the report only. They do not change any run.
- Five more observations are reported, not fixed. Each is a definition the owner may want to confirm, or a divergence of one
  frame at most.

**Environment:**
- I installed the .NET 9 SDK (9.0.320) from Microsoft's Debian 12 apt feed. dot.net and the Ubuntu PPA were blocked by the proxy.
- `dotnet test Tools/CombatChecks -c Release` on main: 682 passed, 0 failed. The task's baseline said 658; main has grown since.
- The same command on this branch: 684 passed, 0 failed (two new tests). The two `[Explicit]` report tests are listed as skipped.

## Findings fixed

### F1. A death while the gate holds was reported as an elite-room death (medium)

**Where:**
- `DescentSimulation.Foundry.cs:1684-1702`, `RecordDeath`.
- `FoundryGate.cs:503`, `DeathPlace`.

**What was wrong:**
- While the cleared room before the gate held for the floor's 3:30, `_waveAlive` was 0. The death phase therefore fell through to
  "between waves".
- The gate's "Clears and deaths" row grouped deaths by the kind of room the hero died in. On the shipped floor that room is room
  6, the extra elite.
- So a hero that the horde killed 20 s into the gate's wait was counted under "elites". It was indistinguishable from a death
  during the elite fight, or in its 1 s advance delay.
- The 2026-11-04 status quotes this row ("the elites 5"). The gate's wait is about 110 s of horde pressure on every clear. Its
  cost belongs in its own line, because the owner is weighing that wait against halls.

**Scenario:**
- Seeds 1-200, shipped levers, .NET.
- The Vanguard's seed 110 died in room 6 with `gate_wait_s` 20.22, clock 149, to a horde Grunt.
- Ilya's seed 168 died in room 6 with `gate_wait_s` 18.88, clock 111, to the pit.
- Both were reported under "elites".
- The Vanguard's seed 121, also in room 6 with wait 0, is a genuine death between the elite's waves.

**Fix:**
- The simulation now flags the hold (`_gateHeld`, set where `GateWaitSeconds` counts and cleared in `AdvanceFloor`).
- A death during the hold is recorded with `DeathPhase` "gate wait" (`DescentSimulation.GateWaitPhase`).
- The gate's row reports it as "the gate's wait". The batch's "room: phase" tables now show "6: gate wait".

**Tests:**
- `FoundryGateTests.DeathsAreReadByWhereTheHeroStoodTheSameInEveryRow` uses constructed runs.
- `FoundryGateTests.TheSimulationRecordsADeathInTheGatesWaitAsTheWaits` holds the gate to the floor's limit on seeds 1-6. It
  asserts an outcome (heroes fall in the wait: 4 of 6 here), not one seed's trajectory. It also checks that every death after the
  wait began and before the Warden walked out carries the wait's phase.

**Numbers (200 seeds, FOUNDRY_LEVERS=0):**
- Vanguard: "elites 5" became "elites 4, the gate's wait 1".
- Ilya: "elites 1" became "the gate's wait 1".

### F2. "Died at the Warden" was counted two ways (low)

**Where:**
- `FoundryGate.cs:487`, `CostText`.
- `FoundryBatch.cs:304, 397, 423`.
- `FoundryBatchReport.cs:350`.
- This is set against `FoundryGate.DeathPlace`, and against `FoundryBatch.FloorTwo.cs:108`, which already used the phase.

**What was wrong:**
- Floor 1's Warden rows counted a death at the Warden as `Died && BossSeconds < 0`.
- The deaths-by-place row counts by the room, and floor 2's table counts by the phase "boss".
- `PitEruptionService` burns the hero before the enemies (the simulation does the same). An eruption can therefore kill the hero
  and then fell the Warden in the same frame.
- That run has `BossSeconds >= 0`, which counts as a felled fight, but its outcome is Died in the boss phase.

**Scenario:**
- Ilya's seed 62 died to the pit with `boss_s` 60.75 and `boss_left` 0.
- The gate read "died there 36 of 197" in the cost row but "Warden 37" in the clears row.
- The Warden table showed 36 and the health-budget table "36 (18%)".

**Fix:**
- New `FoundryResult.DiedAtWarden` (`DescentSimulation.Foundry.cs:343`): Died, the Warden walked out, and the phase is "boss".
  Every row now uses it.
- The run still counts as a felled fight, as floor 2's table already states.

**Numbers:**
- Ilya: "died there 36 of 197" became "37 of 197".
- Health-budget table: "36 (18%): pit x22" became "37 (19%): pit x23".
- The Vanguard is unchanged at 20 of 94.

## Observations, not fixed

**O1. The weapon-roles rule is pairwise (definition; the owner may want to confirm it).**
- Where: `FoundryGate.cs:340-350`.
- A weapon has a role when, for each other weapon, it leads that weapon on the horde or on the Warden. The dimension can differ
  from one opponent to the next.
- A weapon that is second on both axes can therefore pass and be called "between".
- Constructed case: Sword (20, 20), Staff (30, 10), Daggers (10, 30), each as (horde, Warden). This reads "met": the Staff is the
  horde's weapon, the Daggers the Warden's, and the Sword sits between.
- With two axes, the stricter reading ("leads every other on one axis") cannot be met by three weapons at all.
- The docs' wording ("leads every other by more than 10 % on one of the two") reads like the strict rule. The code's reading
  should be stated as the rule.
- It does not change today's status: the Staff leads both axes (horde 36 against 10 and 4; Warden 30 against 26 and 13).

**O2. The per-weapon rates mix survival into weapon strength (definition).**
- The horde rate is the weapon's damage on the horde a second of the floor, as a P50 over runs. For the Sword and Daggers starts,
  most of those runs die in the halls: floor P50 102 s and 95 s, against 283 s for the Staff.
- So each weapon's P50 is taken over a different mix of floor phases.
- The rates are correct as defined. They are not a like-for-like comparison of the weapons.

**O3. Engaged uptime is "met" only over every run (definition).**
- The status reads the P50 of every run: the Vanguard 0.77, against 0.70 for its clears.
- Early deaths are short floors spent mostly fighting, so they lift the P50.
- The basis already says so. I note it because the status line reads "met".

**O4. "Warden within 5 s" counts the 2.6 s reveal.**
- `BossSeconds` starts as the Warden walks out. A fight that ends at 5 s on this measure had about 2.4 s of fighting.
- That is the documented definition, and every fight is far from it (0 of 74 and 0 of 161; P10 44 and 41 s).

**O5. Frame-order and one-frame effects (from the parity pass; negligible).**
- DevelopmentAutopilot and the hero's AttackController share Unity's default order. The simulation always attacks first, so on
  the frame the last node enemy dies the scene's pilot may still read "fighting" for one frame.
- A main camera without `ArenaCameraFollow` would skip the reveal in the scene (`BossRevealView.cs:44`). The measurement scene has
  the follow.
- Offers that the sweep leaves unanswered at floor 1's clear can add a frame or so to floor 2's `DescentSeconds`.
- An offer opening mid-hold skips one frame of `GateWaitSeconds`.
- Floor 1's gate time, surges and elites come from the levers, not from `FoundryFloor`. They are equal for Floor_EmberHalls, and
  `FoundrySimulationSceneValueTests` holds the values.
- `FoundryBatch`'s quality-gates line takes the first-strike P90 without dropping the `-1` "never" value. This is harmless today,
  since every run strikes by 1.6 s.

## Checked and in parity

**Gate hold (EncounterController and FloorDefinition's foundry fields):**
- Same clock: `HordeDirector.Seconds`, ticked before the check.
- Same condition: it holds while the clock is under 210 and releases at 210 or more. The release happens on the frame the clock
  crosses 210.
- The hold is evaluated only after the 1 s advance delay with no choice open, needs a live horde, and only applies when the next
  room is the gate.
- The clock is frozen during offers, the forge and the walk after it, and the reveal.
- `GateWaitSeconds` counts the same frames as `gate_hold_sec`.
- Floor 2 has no hold (gate time 0 in the asset and in `LaterFloorLevers`). Its horde and clock restart with its first room.

**Rooms:**
- Floor 1 plays C C C F E E B in both. The extra elite is a copy of the first, placed before the boss, with its own district.
- Floor 2 plays C C C F E B as authored.
- Surges go to combat halls only, room 1 included: 2 on floor 1, 0 on floor 2. They have the same `HallSurgeRule` composition and
  run through `FloorProgress`.

**Chests:**
- Keyed by (floor, room index) on both sides. Each elite gets its own chest (index 4 heals, index 5 pays gold), and an unopened
  chest is replaced at the next room's first wave.
- The boss room's chest comes as the Warden walks out. The index counts across floors.

**Warden:**
- `WardenSlamClock` is shared code, and its blow time is fixed at the mark.
- Telegraph 1.1 s, `RecoverySeconds` 1.5 s and `GateHealthShare` 8.5 (first gate enemy only) match.
- Enrage is latched once at 0.5 health, read before `Advance`, and thickens the boss room's horde.
- The mark takes the hero's floor position after that frame's move. The blow tests the same position with a squared floor
  distance against 2.2², for 0.2 of the hero's maximum health.

**Script order and the weapon hold:**
- The simulation steps the slam after the encounter and before any attack. That is the scene's -50 / -40 / 0 order.
- `IsHeld` returns before `weapon.Tick`, freezing the cooldown, in both. There is no strike in progress to cancel. The escort is
  never held.

**DropDetourRule and the autopilot:**
- One shared body: reach 4, slack 2, budget 12 s.
- The way is measured to the nearest unused altar, else the marked hall, using 2D floor distance.
- The budget is charged only on frames spent steering at a drop. It resets on any frame a node wave stands, or on a newly marked
  hall.
- With nowhere to go (the gate's wait), the pilot takes the nearest drop within 4 units.
- The goal order matches: exit, mark escape, pit shelter, kite, fight (the rim lure; `Pit.IsActive` is always true on the
  foundry), then the walk.

**Result fields:**
- `LevelAtGate`, `ClockAtGate`, `HeroHealthAtGate` and the Warden's health are set only as the Warden walks out (`GateBossArrived`).
- The horde, and with it the floor's clock, is released as the Warden falls, so the clears' `Clock` is the clock at the fall.
- Each floor's result is fresh (`NewResult` in `Descend`). Floor 2 never writes into floor 1's kills or weapon work.

**Statistics:**
- `FoundryBatch.Percentile` is linear between the two nearest ranks (numpy's default). The gate's wrapper drops NaN and the
  simulation's -1 ("never").
- Denominators:
  - Floor time and both uptimes are over every run, with the clears' P50 beside them.
  - The Warden fight is over felled fights (`LevelAtGate >= 0 && BossSeconds >= 0`).
  - The cost is over runs that reached the Warden.
  - The synergy is over clears.
- Per-weapon accounting:
  - Each landing is attributed to the striking slot's work (Warden / horde / node).
  - Proc damage and proc kills are those after the proc counters rise. `WeaponRuntime` raises them after the strike's own
    landings, so a strike's own kill is never counted as a proc.
  - `HeldFrom` and `EvolvedAt` are in floor time, as are the kills' times. "Kills after" uses a strict `>`, so a kill on the frame
    of the pick is not counted.
- The evolutions counted include those taken in the clear's sweep, which happen before the floor ends. That fits "by the floor's
  end".
- The Editor parsing (pacing columns, the last room from the rooms column, gate_hold_sec only in files with a levers line) reads
  what `HordePacingMeasurement` writes.

## The gate tables, before and after (seeds 1-200 a hero, FOUNDRY_LEVERS=0, FOUNDRY_FLOORS=2, no Editor files)

The summary is the same before and after: met 6, not met 3 (floor time, weapon roles, synergy), not measured 2, reported 4. Only
the two rows marked † change.

### Before (main 4cda1cc)

| Criterion | Status | Simulation |
|---|---|---|
| Floor time (the autopilot) | **not met** | Vanguard 70 / 102 / 285 s, clears' P50 281 s; Ilya 262 / 277 / 291 s, clears' P50 278 s |
| Time waiting for the gate | reported | Vanguard 97 / 108 / 117 s of the clears (38% of their floor time), clears less the wait P50 173 s; Ilya 112 / 116 / 119 s of the clears (41% of their floor time), clears less the wait P50 164 s |
| Floor time (the owner's play) | not measured | - |
| Combat uptime | met | Vanguard 0.97 / 1.00 / 1.00, clears' P50 0.97; Ilya 0.95 / 0.97 / 1.00, clears' P50 0.97 |
| Engaged uptime | met | Vanguard 0.61 / 0.77 / 0.87, clears' P50 0.70; Ilya 0.51 / 0.57 / 0.67, clears' P50 0.58 |
| First combat | met | Vanguard P50 1.6 s, P90 1.6 s; Ilya P50 1.4 s, P90 1.4 s |
| Three weapons with distinct roles | **not met** | Sword horde 10, Warden 26 a second; Staff horde 36, Warden 30 a second; Daggers horde 4, Warden 13 a second |
| One synergy | **not met** | Vanguard 58 of 74 clears (78.4%), first at P50 167 s; procs land 15% of the evolved weapons' kills after; Ilya 129 of 160 clears (80.6%), first at P50 149 s; procs land 17% of the evolved weapons' kills after |
| Warden fight | met | Vanguard 44 / 53 / 62 s (74 felled); Ilya 41 / 51 / 65 s (161 felled) |
| Warden felled within 5 s | met | Vanguard 0 of 74; Ilya 0 of 161 |
| The hero's health at the Warden † | reported | Vanguard 78 health lost of 198 (Warden 29, slams 0, horde 8, pit 40); died there 20 of 94; Ilya 70 health lost of 162 (Warden 28, slams 0, horde 5, pit 36); died there 36 of 197 |
| Clears and deaths † | reported | Vanguard cleared 74 of 200 (37%); died 126: halls 101, Warden 20, elites 5; Ilya cleared 160 of 200 (80%); died 40: Warden 37, halls 2, elites 1 |
| Clock at clear and the overrun | reported | Vanguard P50 260 / P90 269 s; past 300 s 0 of 74 clears (0%); Ilya P50 258 / P90 272 s; past 300 s 0 of 160 clears (0%) |
| Extract available | met | Vanguard cleared, so offered, in 37%; Ilya cleared, so offered, in 80% |
| Frame time on the device | not measured | - |

### After (this branch)

| Criterion | Status | Simulation |
|---|---|---|
| Floor time (the autopilot) | **not met** | Vanguard 70 / 102 / 285 s, clears' P50 281 s; Ilya 262 / 277 / 291 s, clears' P50 278 s |
| Time waiting for the gate | reported | Vanguard 97 / 108 / 117 s of the clears (38% of their floor time), clears less the wait P50 173 s; Ilya 112 / 116 / 119 s of the clears (41% of their floor time), clears less the wait P50 164 s |
| Floor time (the owner's play) | not measured | - |
| Combat uptime | met | Vanguard 0.97 / 1.00 / 1.00, clears' P50 0.97; Ilya 0.95 / 0.97 / 1.00, clears' P50 0.97 |
| Engaged uptime | met | Vanguard 0.61 / 0.77 / 0.87, clears' P50 0.70; Ilya 0.51 / 0.57 / 0.67, clears' P50 0.58 |
| First combat | met | Vanguard P50 1.6 s, P90 1.6 s; Ilya P50 1.4 s, P90 1.4 s |
| Three weapons with distinct roles | **not met** | Sword horde 10, Warden 26 a second; Staff horde 36, Warden 30 a second; Daggers horde 4, Warden 13 a second |
| One synergy | **not met** | Vanguard 58 of 74 clears (78.4%), first at P50 167 s; procs land 15% of the evolved weapons' kills after; Ilya 129 of 160 clears (80.6%), first at P50 149 s; procs land 17% of the evolved weapons' kills after |
| Warden fight | met | Vanguard 44 / 53 / 62 s (74 felled); Ilya 41 / 51 / 65 s (161 felled) |
| Warden felled within 5 s | met | Vanguard 0 of 74; Ilya 0 of 161 |
| The hero's health at the Warden † | reported | Vanguard 78 health lost of 198 (Warden 29, slams 0, horde 8, pit 40); died there 20 of 94; Ilya 70 health lost of 162 (Warden 28, slams 0, horde 5, pit 36); died there 37 of 197 |
| Clears and deaths † | reported | Vanguard cleared 74 of 200 (37%); died 126: halls 101, Warden 20, elites 4, the gate's wait 1; Ilya cleared 160 of 200 (80%); died 40: Warden 37, halls 2, the gate's wait 1 |
| Clock at clear and the overrun | reported | Vanguard P50 260 / P90 269 s; past 300 s 0 of 74 clears (0%); Ilya P50 258 / P90 272 s; past 300 s 0 of 160 clears (0%) |
| Extract available | met | Vanguard cleared, so offered, in 37%; Ilya cleared, so offered, in 80% |
| Frame time on the device | not measured | - |
