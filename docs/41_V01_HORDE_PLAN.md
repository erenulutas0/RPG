# 41. V0.1 - the horde: an engineering plan (proposed, 2026-10-26)

Everything in this document is **proposed**. It maps the PR list in the owner's research report (`40`, section 17.2)
onto the files this repository actually has, so that each step names what it changes and what number lets it through.
The numbers are the report's; the baselines are ours, measured on the phone. Decisions 9-12 in `27` say why.

## Measured baselines (before any of this)

- Foundry floors run **83-128 s** active against the S6 target of 240-360 s (`21`, 2026-10-25): about a third walking,
  a bit over half the halls' fights in 5 s beats, the hall surges worth +12 to +23 s.
- The Forge Warden costs the owner **6-11 HP** and the owner's floor-1 rate is 1.6 s an enemy with the Daggers; the
  Staff autopilot's is 0.6 s an enemy.
- The S23 holds **59.5-59.9 fps with ten enemies fighting**, p95 16.7 ms, no frame over 20 ms (`23`); the foundry
  renders at ~60 fps at the vsync cap, GPU ~3 ms, 50-98 draw calls (`15`, 2026-09-30). Nothing has been measured above
  ten enemies.
- A pack held at most ten until today (`PackLayout.MaxPackSize`, 10). A hall's authored wave and a swarm's ladder wave
  keep that ceiling under the new name `PackLayout.MaxFormationSize`; `MaxPackSize` becomes the horde's ceiling (40).

## What exists, and where the report's pieces land

| Report piece | What the repo has | File |
|---|---|---|
| Waves, node fights, hall districts | `EncounterController`: authored waves and surges (`SpawnWave`, `StartWave`, `WaveCleared`), halls opening at districts (`TryRoomSite`, `IsRoomSiteOpen`), the gate (`OpenGateExit`, `GateBossArrived`) | `Assets/_Project/Scripts/Combat/EncounterController.cs` |
| Continuous spawning | The finale's pure director `SwarmDirector` (clock-read ladder, `Spawn` records per pack place), driven from `BeginSwarm` / `TickSwarm` / `SpawnSwarmEnemy`; its constants in `SwarmRule` | `Assets/_Project/Scripts/Combat/SwarmDirector.cs`, `SwarmRule.cs` |
| Hall surges (the first lever) | `HallSurgeRule.SourceWave` / `Size`, `FloorDefinition.FoundryHallSurges` | `Assets/_Project/Scripts/Combat/HallSurgeRule.cs` |
| Pack capacity, pack movement | `PackLayout.MaxFormationSize` (10) / `MaxPackSize`; `PackMotion` with its `Held` flag for the pit | `Assets/_Project/Scripts/Combat/PackLayout.cs`, `PackMotion.cs` |
| Spawn spots, districts | `FoundryMap.PlacePack`, `PlaceAtGate`, `RoomSite` (four districts, `RoomSiteRadius` 1.6), `GateExitSpot` | `Assets/_Project/Scripts/Art/FoundryMap.cs` |
| Offers, rarity, Luck | `UpgradePool.WidthAt` (5 cards until level 12); `UpgradeService.CreateOffer` / `Eligible` / `TrySelect` / `TryGrantBook`; `RarityTable.Draw` over `UpgradeRarity` Common/Rare/Epic, weights 70/25/5 shifted by `HeroStats.Luck`; `EvolutionRule.DifficultyPerStack` 0.2 | `Assets/_Project/Scripts/Progression/UpgradeService.cs`, `UpgradeRarity.cs`, `EvolutionRule.cs` |
| Bot | `DevelopmentAutopilot` (development builds, armed by `development/autopilot.txt`, steers through `HeroMovementInput.Hold`) | `Assets/_Project/Scripts/Core/DevelopmentAutopilot.cs` |
| Simulation | `DescentSimulation.Run(...)` with `Floor`, `HeroWeapon`, `HeroAbility`, `CardPicker`, seed and replay | `Assets/_Project/Tests/Support/DescentSimulation.cs` |
| Telemetry | `RunTelemetry`: `run_start`/`run_end`, `room_start`/`room_complete`, `wave_start`/`wave_clear`, `boss_start`/`boss_end`, `swarm_step`, `upgrade_offered`/`upgrade_selected`, `pit_eruption`, `altar_taken`; `hero_hp` always beside `hero_max_hp` (`HeroHealthFields`) | `Assets/_Project/Scripts/Analytics/RunTelemetry.cs` |
| Device performance | `RenderStatsProbe` (`development/render-stats.txt`, one `[RenderStats]` line every 5 s: fps, p50/p95/max, draw calls, GPU ms, memory) and `FrameTimeProbe` (`[Perf]`, main thread and wave spawns) | `Assets/_Project/Scripts/Core/RenderStatsProbe.cs`, `FrameTimeProbe.cs` |
| Deterministic seed | `RunSeeds`, `development/run-seed.txt` (`DevelopmentStart.RunSeedPath`) | `Assets/_Project/Scripts/Core/RunSeeds.cs`, `DevelopmentStart.cs` |
| Stamina (report 12.1) | Already off: `HeroStamina.CurrentRulesEnabled` is false since 2026-09-23 | `Assets/_Project/Scripts/Combat/HeroStamina.cs` |
| Pickup radius (report 12.3) | `GroundDrops.PickupRadius` 1, measured in `37` | `Assets/_Project/Scripts/Economy/GroundDrops.cs` |

The report's PR-01 (instrumentation) is mostly done: the events above exist, the seed is exposed, and the two probes
measure the device. What it still lacks is a per-window combat sample (active enemies, hero DPS, kills) beside the
render line, which step 1 adds.

## Steps

Each step is one measurable change. None is started until the one before has its number.

### 1. Density probe - what the phone can hold

The horde's cap has to be measured before the horde exists.

- Split `PackLayout.MaxPackSize` into `MaxFormationSize` (10, for an authored wave) and `MaxPackSize` (the horde's
  ceiling, 40 to start); every array sized by the pack (`PackMotion`, `HeroRoute`, `EncounterController._swarmSpawns`)
  follows the ceiling, every authored-wave check the formation size. In flight.
- Behind a development flag, `development/horde-probe.txt` (`DevelopmentStart.HordeProbe`), pad the foundry's first
  wave with N copies of its first enemy up to the ceiling, so a device run meets 20, 30, 40 grunts at once. Nothing
  without the file; the Descent simulation never sees it.
- Add the active enemy count and the window's kills to the `[RenderStats]` line (or a `[Perf]` line beside it), so the
  frame time can be read against the crowd that caused it.
- Measure on the S23 with `RenderStatsProbe`: N = 10 (baseline), 20, 30, 40.

Acceptance (report 14.3 and 9.6, FPS "by device cohort"): p95 frame time at the chosen crowd no worse than 20 ms on
the S23, no frame over 35 ms in a steady fight, garbage per frame flat. The largest N that passes is the cap the next
step starts from; if 30 fails, decision 10's target moves down, not the gate.

### 2. HordeDirector - the pure rule, then the scene

A new pure class beside `SwarmDirector`, not a change to it: the finale's director reads a ladder by the clock and
fills a ten-place pack; the horde holds a crowd. Being written now.

- **Pure rule** (`Assets/_Project/Scripts/Combat/`, tested in EditMode without a scene): a standing-crowd target that
  rises with the floor's clock (the report's 8.3 shape, scaled to the cap from step 1), a spawn rate requested from the
  gap between target and living count, a kill-reactive factor from the last five seconds' kills over spawns (the
  report's `R_clear`, with its `S = 0` guard), a hard cap on living enemies and a spawn-credit cap of about one second
  so a pause never turns into a burst. Director states by floor progress: Ramp, Horde, Pressure, Boss (report 3.3).
- **Scene integration** in `EncounterController`: one living pack per floor from the floor's start to the gate, the
  node waves (`SpawnWave`, surges) layered on top of it and counted against the same cap; spawn spots off-screen through
  `FoundryMap.PlacePack` from the hero's position, never inside a district the hero stands in; the horde thins to the
  Boss state's controlled minions when `GateBossArrived` fires, not to zero. The boss gate opens after the three halls.
- Telemetry: a `horde_sample` every few seconds (living, target, spawned, killed, state) and `enemy_count` on
  `room_start`; `wave_start` / `wave_clear` keep meaning the node waves.
- Simulation: `DescentSimulation` gains the horde as a per-floor option so pinned numbers (the hall arena, the
  Descent) do not move.

Acceptance (report 9.6, 17.5): floor P50 **4-6 min** on the autopilot and on the owner; **combat uptime >= 75 %** (the
share of active time with an enemy alive, read from `horde_sample`); first combat within 10 s; regression tests that
the living count never exceeds the cap and that a one-second pause spawns at most one second's credit. Frame time as
step 1 passed it.

### 3. Weapons and tomes - categories, slots, four colours

Only after step 2 has its 4-6 minutes: the cards are the report's PR-03 and PR-04.

- Split the nine generic cards into weapon upgrades, tome upgrades and general stats in `UpgradeService.Eligible` and
  `CreateOffer`; offer categories with the report's weights (9.2); 3 weapon + 3 tome slots, a level cap of 8 per
  weapon, a filter that stops offering a new weapon once the slots are full (5.1).
- Rarity goes from three tiers to four (green/orange/red/gold) in `UpgradeRarity`; `RarityTable.Draw` takes the
  report's formula `w_i = p0_i * (1 + L/25)^(0.4 i)` over `p0 = [0.60, 0.25, 0.12, 0.03]`, with the soft cap
  `L_eff = 50 + 0.25 * max(0, L - 50)` (7.2, 7.5). The nine cards' values become per-rarity (6.1).
- Keep the level-up UI; add the delta per rarity on the card.

Acceptance (report 17.4, 9.3, 9.6): probabilities sum to one and none negative at Luck 0/10/25/50; Gold odds never fall
as Luck rises; slots and level caps never exceeded; no ineligible card in an offer; a seeded run reproduces its offers;
every weapon has a viable run without a Luck tome; at least one visible synergy (a tome that changes a weapon's kill
pattern, not only its number) in the Vanguard's V0.1 floor.

### 4. Boss rework - a 45-60 s Forge Warden

The Warden is a wave today; the report wants a fight (8.9, 12).

- HP from the DPS budget `HP_boss = D_focus(t) * uptime * damageable_seconds`, measured first with the simulation's
  hero at the floor's end level (the report's 1,500-1,900 is its example, not ours).
- One telegraphed cone (0.8-1.2 s), one vulnerability window, controlled minions from the horde's Boss state;
  `boss_start` / `boss_end` already carry `hero_hp` and `hero_max_hp`.
- The evolution's +0.20 Difficulty cost stays (decision 11); Overforge is a later experiment, not part of V0.1.

Acceptance (report 9.6): boss elapsed P50 **45-65 s**; a boss dead in 5 s near zero in the normal cohort; the owner's
HP cost at the boss measured against today's 6-11 and reported, not targeted.

### 5. Simulation and bots - the regression harness

- `DescentSimulation` runs the horde floor across a seed batch (hundreds first, thousands if the run time allows) and
  reports P10/P50/P90 floor time, observed TTK by enemy, deaths, level at the gate, as the report's `time_bucket_summary`
  and `offer_health` tables in Markdown.
- `DevelopmentAutopilot` gains two policies beside its first-card picker: standing (no kiting) and a preferred-card
  list; the report's four bot cohorts (9.4) are the later shape.
- Quality gates (9.6) run as tests against the batch, so a balance change that breaks a gate fails the build.

Acceptance (report 17.5): the whole V0.1 gate read from one report - 4-6 min floor, uptime >= 75 %, three weapons with
distinct roles, one synergy, Warden 45-60 s, Extract available, acceptable frame time on the device - before any
second character is authored.

## Not in V0.1

Six characters and active abilities beyond the Forge Burst, Overforge and Heat, run items, meta unlock quests, the
60-120 crowd, an elite with its own attack beyond one. Each waits for the gate above.

## Status 2026-11-02: the floor's length

- The research (2026-10-29) found floor 1 at about 170 s, short on content rather than power. The foundry simulation
  (`DescentSimulation.RunFoundry`, `FoundryBatchReport`, docs/41 step 5) then measured the levers over 200 seeds a hero.
- The owner chose two surges a hall, a second elite before the gate and the gate on the floor's clock at 3:30
  (`FloorDefinition._foundryHallSurges` 2, `_foundryExtraElites` 1, `_foundryGateNotBeforeSeconds` 210; foundry only,
  the hall arena and the Descent keep their authored rooms). While the cleared room before the gate holds, the floor
  clock reads "Gate opens in m:ss".
- Scene measurement (HordePacingMeasurement, seeds 11-66): the Vanguard clears 3/6 at 267-321 s, dying 3 times in the
  halls (112-177 s); Ilya clears 5/6 at 259-289 s, one death at the Warden. The 4-6 minute floor is met for a clear.
- Still open against the gate: the Warden's fight is 33-44 s (target 45-65), engaged uptime is 0.5-0.7, and the
  Vanguard's autopilot dies in half its runs.

## Status 2026-11-03: the Warden's fight

**What changed.** Two values and one rule in `WardenRule`, for the foundry's gate boss only: `GateHealthShare` 5 -> 8.5,
and the Warden's own weapon is held from the slam's mark until `RecoverySeconds` (1.5 s) after the blow - the hammer
going up through the telegraph, then in the floor (`WardenSlamClock.HoldsWeapon`; `WardenSlamService` sets
`AttackController.IsHeld` on the gate boss, which stops its cooldown and its strikes). It still walks. The foundry
simulation reads the same clock. Floor 2's gate boss is a Warden too and gets both; floor 2 was not measured.

**How it was measured.** The foundry simulation (`DescentSimulation.RunFoundry`, the development autopilot's policy),
seeds 1-200 a hero, the candidates as `FoundryBatch.WardenLevers` (`FOUNDRY_LEVERS=3`), the choice again on seeds 1-600.
The runs are paired: everything before the gate is the same run whatever the Warden is (600 of 600 seeds), so a change
in clears is the Warden's alone. The criteria, written before the sweep: both heroes' P50 fight 45-65 s and clears no
more than 5 points below the floor before; added during it, the floor's clock at clear against the 300 s overrun, and
no change that sends more than about 10 % of clears into it.

**The budget, on the floor before.** docs/40's HP = D_focus * uptime * damageable seconds, read from the felled fights:
D_focus is the hero's own damage on the Warden (weapons, burn, ability) per second it stood within the reach of the
hero's longest weapon, the uptime that share of the fight after the 2.6 s reveal. Cost and deaths are over every run
that reached the Warden.

| Hero | Level at the gate | D_focus P50 (P10-P90) | Uptime | Seconds after the reveal | Warden's health taken: weapons / burn / ability / pit | Hero health at the gate (maximum) | Fight cost, mean: Warden / slams / horde / pit | Died at the Warden: killing blow |
|---|---|---|---|---|---|---|---|---|
| Vanguard | 26 | 89 (69-123) | 0.65 | 34 | 90 / 0 / 3 / 8 % | 142 (190) | 48 / 3 / 8 / 28 | 13 of 92: Warden 5, pit 5, horde 3 |
| Ilya | 26 | 105 (80-140) | 0.58 | 31 | 77 / 13 / 2 / 7 % | 122 (150) | 45 / 4 / 3 / 24 | 28 of 175: Warden 20, pit 7, horde 1 |

89 * 0.65 * 34 and the pit's share make about the 2,100 the Warden had (300 * 5 * difficulty 1.4 from two altars and an
evolution); over the whole fight the heroes dealt it 58 and 61 health a second. The slams almost never land - the
autopilot steps out of about 98 % of the marks - and kill no one; the Warden's own blows and the pit do, and what the
fight costs grows with its length. In a scratch breakdown by what the autopilot was doing, 40 % of the Warden's blows
landed while the hero ran from a mark (it takes the nearest of eight points round the mark, all at one distance,
whichever side the Warden is on), a third while it kited into a wall and stood, a fifth while it left the pit's band;
most of the pit's hits came as it lured back into the band during a warning. A longer fight needs a cheaper second.

**The candidates (seeds 1-200 a hero).** Fight: the felled fights. Cleared: change against the floor before, in points.
Cost: the hero's health lost in the fight, mean. Into the overrun: runs that reached the Warden before 300 s on the
floor's clock and were still fighting it at 300 s. A, B, C keep the weapon free, as before; D and W hold it.

| Candidate | Vanguard fight P50 (P10-P90) | Vanguard cleared (change) | Vanguard died at the Warden | Ilya fight P50 (P10-P90) | Ilya cleared (change) | Ilya died at the Warden | Fight cost (Vanguard / Ilya) | Into the overrun (Vanguard / Ilya) | Clock at clear P90 (Vanguard / Ilya) |
|---|---|---|---|---|---|---|---|---|---|
| Before 2026-11-03: share 5, never held | 36 (31-43) | 39.5 % (+0.0) | 13 of 92 | 34 (28-42) | 73.5 % (+0.0) | 28 of 175 | 87 / 76 | 5 (5 %) / 5 (3 %) | 338 / 320 |
| A: share 6 | 41 (35-48) | 36.5 % (-3.0) | 19 of 92 | 38 (31-47) | 67.0 % (-6.5) | 41 of 175 | 102 / 85 | 7 (8 %) / 6 (3 %) | 341 / 310 |
| A: share 7 | 44 (37-53) | 33.5 % (-6.0) | 25 of 92 | 41 (33-52) | 62.0 % (-11.5) | 49 of 175 | 106 / 95 | 8 (9 %) / 7 (4 %) | 349 / 324 |
| A: share 8 | 50 (43-61) | 33.0 % (-6.5) | 25 of 92 | 45 (37-54) | 58.0 % (-15.5) | 57 of 175 | 115 / 103 | 8 (9 %) / 8 (5 %) | 351 / 315 |
| A: share 9 | 53 (46-65) | 27.5 % (-12.0) | 36 of 92 | 49 (40-60) | 57.0 % (-16.5) | 60 of 175 | 122 / 106 | 9 (10 %) / 10 (6 %) | 358 / 323 |
| A: share 10 | 55 (47-67) | 26.0 % (-13.5) | 39 of 92 | 51 (42-65) | 49.0 % (-24.5) | 77 of 175 | 134 / 113 | 10 (11 %) / 10 (6 %) | 330 / 307 |
| B: share 7 at 3:30, growing with the clock | 46 (38-55) | 33.5 % (-6.0) | 25 of 92 | 42 (35-53) | 60.0 % (-13.5) | 54 of 175 | 110 / 96 | 8 (9 %) / 9 (5 %) | 354 / 302 |
| B: share 8 at 3:30, growing with the clock | 52 (44-59) | 31.5 % (-8.0) | 29 of 92 | 45 (38-55) | 57.0 % (-16.5) | 59 of 175 | 117 / 105 | 8 (9 %) / 11 (6 %) | 360 / 307 |
| C: share 5, blows at 0.5 outside 2 s after its own | 46 (38-59) | 32.0 % (-7.5) | 28 of 92 | 42 (35-54) | 61.0 % (-12.5) | 53 of 175 | 113 / 97 | 8 (9 %) / 9 (5 %) | 352 / 321 |
| C: share 5, blows at 0.6 outside 2 s after its own | 43 (36-55) | 35.0 % (-4.5) | 22 of 92 | 40 (32-53) | 60.5 % (-13.0) | 51 of 175 | 103 / 91 | 8 (9 %) / 7 (4 %) | 345 / 325 |
| D: share 5, held through the windup | 36 (31-43) | 41.5 % (+2.0) | 9 of 92 | 34 (28-42) | 82.0 % (+8.5) | 11 of 175 | 69 / 59 | 5 (5 %) / 5 (3 %) | 337 / 317 |
| D: share 5, held through the windup and 1.5 s after | 36 (31-44) | 43.0 % (+3.5) | 5 of 92 | 34 (28-42) | 83.5 % (+10.0) | 8 of 175 | 60 / 51 | 5 (5 %) / 5 (3 %) | 339 / 315 |
| W: share 8.5, held through the windup | 52 (43-61) | 34.0 % (-5.5) | 23 of 92 | 47 (38-58) | 67.0 % (-6.5) | 41 of 175 | 102 / 84 | 8 (9 %) / 10 (6 %) | 359 / 309 |
| W: share 8.5, held + 1 s | 52 (42-61) | 37.0 % (-2.5) | 17 of 92 | 47 (38-58) | 71.0 % (-2.5) | 33 of 175 | 94 / 78 | 8 (9 %) / 10 (6 %) | 356 / 310 |
| W: share 8.5, held + 2 s | 52 (41-61) | 40.0 % (+0.5) | 11 of 92 | 47 (38-58) | 76.0 % (+2.5) | 23 of 175 | 83 / 69 | 8 (9 %) / 10 (6 %) | 353 / 310 |
| W: share 8, held + 1.5 s | 50 (41-61) | 39.0 % (-0.5) | 14 of 92 | 45 (37-55) | 76.0 % (+2.5) | 23 of 175 | 83 / 72 | 8 (9 %) / 9 (5 %) | 348 / 306 |
| W: share 9, held + 1.5 s | 53 (45-63) | 35.5 % (-4.0) | 20 of 92 | 49 (40-60) | 75.5 % (+2.0) | 22 of 175 | 91 / 74 | 9 (10 %) / 11 (6 %) | 353 / 317 |
| W: share 9, held + 2 s | 53 (44-63) | 38.5 % (-1.0) | 15 of 92 | 49 (40-60) | 78.0 % (+4.5) | 18 of 175 | 86 / 69 | 9 (10 %) / 11 (6 %) | 353 / 314 |
| Shipped: share 8.5, held + 1.5 s | 52 (42-61) | 39.0 % (-0.5) | 14 of 92 | 47 (38-58) | 74.0 % (+0.5) | 27 of 175 | 89 / 74 | 8 (9 %) / 10 (6 %) | 354 / 310 |

A longer telegraph (1.2 s) and calmer enraged slams (every 6 s) were measured on a scratch copy of the slam clock and
changed nothing (P50 37 / 33 s and 35 / 33 s, clears within a point): the autopilot already reads the slams. Two more
scratch candidates outside the plan's list: the Warden's blows at 0.6 of their damage with share 8.5 cost Ilya 6
points; phase two at 30 % instead of 50 % with share 8 cost 7 and 16.

**The choice.** Health alone, health on the clock and damage taken only in a window all buy the seconds with clears,
since the cost grows with the fight. Holding the weapon through the windup takes away the blows that landed while the
hero read the mark (the fight's cost 87 -> 69 and 76 -> 59 at share 5); with 1.5 s after the blow, share 8.5 puts both
P50s in the window and leaves the clears where they were. 1 s cost each hero 2.5 points; 2 s kept the clears too but
leaves the enraged Warden under a second to swing between a blow and the next mark; share 8 put Ilya at 45 s, the
window's edge; share 9 cost the Vanguard 4 points. It is also what the painted Warden already showed: `EnemyLookView`
held the raised hammer through the boss's own swings, which landed unseen. Again on seeds 1-600:

| Seeds 1-600 | Vanguard fight P50 (P10-P90) | Vanguard cleared | Vanguard died at the Warden | Ilya fight P50 (P10-P90) | Ilya cleared | Ilya died at the Warden |
|---|---|---|---|---|---|---|
| Before | 35 (29-42) | 35.8 % | 45 of 260 | 34 (28-42) | 75.8 % | 70 of 527 |
| Shipped | 51 (41-61) | 36.2 % | 43 of 260 | 47 (38-58) | 77.8 % | 60 of 527 |

The floor gets 12-15 s longer (P50 257 -> 269 s and 271 -> 286 s). At the Warden the fight now costs the Warden's blows
31-32 and the pit 33-42 a run; the pit becomes Ilya's first killer there (13 of 27 deaths), more eruptions in a longer
fight and the autopilot's walk back into the band.

**The overrun.** No candidate keeps the clock at clear under 300 s at P90, because the floor before does not: a tenth
of the runs reach the gate late (its clock's P90 is 281-305 s; the autopilot's drop-detour loop). On seeds 1-600:

| Seeds 1-600 | Clock at clear P50 / P90 | Fights still on at 300 s (of those reaching the Warden) | Clears past 300 s | Clears newly past 300 s (the same seeds) |
|---|---|---|---|---|
| Vanguard before | 247 / 322 | 16 of 260 (6 %) | 31 of 215 (14 %) | - |
| Vanguard shipped | 263 / 336 | 28 of 260 (11 %) | 40 of 217 (18 %) | 9 of 209 (4 %) |
| Ilya before | 246 / 313 | 29 of 527 (6 %) | 58 of 455 (13 %) | - |
| Ilya shipped | 260 / 319 | 40 of 527 (8 %) | 63 of 467 (13 %) | 10 of 437 (2 %) |

The change sends 4 % (Vanguard) and 2 % (Ilya) of the clears into the overrun, under the limit; a floor whose clears
all end before it would need the late gates fixed first. Whether more clears in overrun are acceptable is the owner's
call.

**To confirm in the scene.** HordePacingMeasurement on seeds 11-66 for both heroes, against 2026-11-02's runs: the
boss_s column (the simulation now expects 50-52 s for the Vanguard's three clears and 46-53 s for Ilya's five; it said
35-40 and 32-37 before), the clears (3/6 and 5/6), the slam hits, and the clock column - the Vanguard's seed 11 cleared
at 294 with a 35 s fight, and 15 s more puts it past 300. WardenFlowTests holds the weapon's hold; WardenLookTests the
raised hammer. And a look: no swing while the hammer is up, and 1.5 s after each blow the Warden walks without swinging.

## Status 2026-11-03 (night): the autopilot's drop detour, and the floor without it

**The loop.** Between fights the development autopilot walked to any drop within 4 units before it went on to the next
hall. On the foundry the horde keeps dying within reach of the hero, so a fresh drop was nearly always there: in walks
over 60 s the pilot steered at a drop 82-87 % of the time, about 400 drops and 400 units for a hall 17-18 units away
(the Vanguard's seed 1 spent 166 s of its second room on 528 drops). 53-191 of 200 runs a hero walked over 150 s on the
shipped floor and on the horde-thickening levers. Every floor-length reading above carries it.

**The rule.** `DropDetourRule`, one body called by `DevelopmentAutopilot` and the simulation's pilot: on the way to an
altar or a marked hall the pilot steps aside only for a drop within the old 4 units that adds at most 2 units to the way
(`DetourSlack`), and for at most 12 s of a walk (`DetourBudget`; a fight or a newly marked hall resets it). With nowhere
to go - the wait for the gate - it takes any drop within 4 units as before; the linger and idle modes are unchanged.
The slack ends the loop; the budget bounds it, since drops alternating sides could otherwise hold the hero in place.
`FoundrySimulationSceneValueTests` holds the two constants to the autopilot's. 0 of 200 runs a hero now walk long, on
every lever.

**The scene agrees** (HordePacingMeasurement on main d6664d2, seeds 11-66, against the simulation on the same seeds;
TestResults/night-parity/foundry-batch.md):

| Hero | Source | Cleared | Walk | Node fights | Gate wait | Warden | Exit | Engaged |
|---|---|---|---|---|---|---|---|---|
| Vanguard | Editor | 3/6 | 38 | 65 | 114 | 52 | 11 | 0.77 |
| Vanguard | Simulation | 3/6 | 38 | 65 | 114 | 54 | 9 | 0.76 |
| Ilya | Editor | 6/6 | 36 | 67 | 114 | 52 | 10 | 0.55 |
| Ilya | Simulation | 5/6 | 36 | 66 | 117 | 54 | 9 | 0.54 |

The Warden's fight in the scene: the Vanguard's three clears 51-54 s, Ilya's six 45-66 s, no slam hit, the clock at
clear 252-273 s (no overrun). The 2026-11-03 Warden change stands confirmed.

**The floor without the loop** (simulation, seeds 1-200 a hero, main d6664d2):

| Hero | Cleared | Floor P50 (all) | Cleared floor P50 | Walk | Node fights | Gate wait | Warden | Deaths: where, killing blow |
|---|---|---|---|---|---|---|---|---|
| Vanguard | 74 (37 %) | 102 s | 281 s | 39 | 73 | 108 | 53 | 126: room 3 x72, room 2 x29, boss x20, room 6 x5; pit 36, horde Grunt 33, wave Tank 30 |
| Ilya | 158 (79 %) | 276 s | 278 s | 37 | 67 | 116 | 51 | 42: boss x39; pit 23, Warden 15 |

- A cleared floor is 278-281 s, and 108-116 s of it is the cleared room before the gate holding for 3:30. Without the
  3:30 rule a cleared floor would be about 166-181 s: the 4-6 minutes are now mostly the gate's wait (with the horde
  pressing on), not halls.
- The Vanguard's "met" floor length of 2026-11-02 was in good part the loop padding runs that died: its deaths barely
  moved (122 -> 126 of 200) but come earlier (P50 113 -> 82 s).
- Lever readings that were mostly the loop (paired median per seed against the shipped floor, Vanguard / Ilya, before ->
  after): +2 halls +61/+68 -> +5/+7 s; halls held 45 s +58/+110 -> -3/+1; fill rate 3/s +78/+26 -> -4/-1; the gate as
  the room clears -18/-21 -> -102/-114; gate at 4:00 +24/+25 -> +28/+30; Warden x1.5 +16/+14 -> +18/+15.

Open for the owner: whether the gate's wait should carry the floor's length or more halls and content should; and the
Vanguard's survival (room 3; the pit, the horde's Grunts and the wave's Tanks).

## Status 2026-11-04: the gate in one report

**What exists.** The batch report opens with the V0.1 gate (`FoundryGate` in Tests/Support; alone in
TestResults/foundry-gate.md): one row a criterion of step 5 and of steps 1-4, with its target, the simulation's reading
over the batch (P10 / P50 / P90), the Editor's newest `HordePacingMeasurement` file a hero (the one naming the simulated
floor's levers first), a status and its basis. Run it with `dotnet test Tools/CombatChecks -c Release --filter
"FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"` (from a worktree, `FOUNDRY_EDITOR` names the main
checkout's TestResults). The status reads the Vanguard, V0.1's hero (`27` decision 12); the basis names where Ilya, the
clears, or an Editor file of the simulated floor read otherwise. Four statuses: met, not met, not measured (with what
would measure it), reported (asked without a target, as step 4's "reported, not targeted").

**Readings chosen for it, for the owner to confirm.**

- Three weapons with distinct roles: the Vanguard started with each weapon on the batch's seeds (as `horde-weapon.txt`
  starts it in the Editor), the others still offered; each weapon's own damage on the horde a second of the floor and on
  the Warden a second of the fight (`40` section 17.3's horde and boss efficiency, from the simulation's new per-weapon
  accounting). A weapon has a role when it leads every other by more than 10 % on one of the two.
- One synergy: a weapon's evolution - the weapon at level 5 with its tome at level 2, each a behaviour (the Sword's
  pulse, the Staff's kill bursts, the Daggers' splinters) - in 80 % of the Vanguard's clears by the floor's end (`40`
  section 9.3's share, which it reads at minute 10), its procs landing kills.
- A Warden dead within 5 s "near zero": at most 1 % of the felled fights. Floor time and uptime over every run, as step 2
  states them, with the clears' P50 beside.

**The first reading** (main d6664d2, the bounded drop detour; seeds 1-200 a hero; the Editor's 2026-11-03 night files,
seeds 11-66, the simulated floor's levers):

| Criterion | Status | Vanguard | Ilya |
|---|---|---|---|
| Floor time, P50 of every run | not met | 102 s (clears 281 s) | 276 s (clears 278 s) |
| Time waiting for the gate, clears | reported | 108 s, 38 % of their floor; clears less the wait 173 s | 116 s, 41 %; 164 s |
| Combat uptime | met | 1.00 | 0.97 |
| Engaged uptime | met (the clears' not) | 0.77 (clears 0.71) | 0.57 (0.58) |
| First combat, P90 | met | 1.6 s | 1.4 s |
| Three weapons with distinct roles | not met | the Staff ahead on the horde (36 a second; Sword 10, Daggers 4) and the Warden (30; Sword 26, Daggers 13) | - |
| One synergy | not met | 79.7 % of the clears | 80.4 % |
| Warden fight, P50 | met | 53 s | 51 s |
| Warden within 5 s | met | 0 of 74 | 0 of 159 |
| Health lost at the Warden, mean | reported | 79 of 198; died there 20 of 94 | 72 of 162; 38 of 197 |
| Clears and deaths | reported | 37 %; deaths in the halls 101, at the Warden 20, the elites 5 | 79 %; at the Warden 39 |
| Clears past the 300 s overrun | reported | 0 of 74 (clock P90 268 s) | 0 of 158 (272 s) |
| Extract available | met | offered at every clear: Floor_EmberHalls leads to Quicksilver Vaults | |
| Frame time on the S23 | not measured | the Editor's probe: p95 5.9 ms, max 31.7 ms at 40 alive | |
| Floor time on the owner | not measured | | |

The Editor's six seeds agree where they read the same thing: the Vanguard 3/6 cleared (deaths in two halls and an
elite), its clears 280 s with 114 s at the gate, the Warden 52 s, engaged 0.77 (clears 0.71); Ilya 6/6, 281 s, Warden
52 s, engaged 0.55. On synergy they read met (3 of 3 and 5 of 6 clears evolved) where the 200 seeds stand at the line.
About 40 % of a clear's 4-6 minutes is the wait for the gate: a clear less its wait is P50 173 s (2:53) for the
Vanguard and 164 s for Ilya. The weapons' roles are not met because the Staff outdoes the other two at both jobs;
started with the Sword, the Vanguard's later weapons deal 63 % of its weapons' damage.

**Not measured, and what would measure it.** The owner's floor time and health at the Warden: the owner's telemetry on
a current APK (floor_complete / run_end active_sec, boss_start / boss_end hero_hp). The device's frame time:
RenderStatsProbe on the S23 at 30-40 alive with a current APK (`21` has 10-40 on 2026-10-26, before V0.1's floor).

The parity table defaults to the same Editor files now (it read the 2026-10-29 m0 files), and reads a seed's telemetry
only when it is that pacing line's run: the measurement overwrites horde-telemetry/ with every run, and the m0 lines had
been read beside the night run's walks and fights.

## Status 2026-11-04 (later): the Warden's slam keeps its telegraph

Two defects in the slam, found by review and fixed in the scene and the simulation alike:

- `WardenSlamClock` re-read the slam's pace every frame, so a Warden that enraged under a calm mark (4 s enraged
  against the 4.9 s a calm mark goes down at) brought the hammer down the next frame - a telegraph of one frame. The
  blow's time is now fixed as the mark goes down; an enrage only brings the next mark sooner.
- Unity ordered `WardenSlamService` and the Warden's `AttackController` as it pleased, so the weapon's hold began on the
  mark's frame or one frame after it. The service now runs at -40 (after EncounterController, before every
  AttackController), and the simulation steps the clock at the same point of its frame;
  `FoundrySimulationSceneValueTests.TheScenesScriptOrderIsTheSimulations` holds the order.

Every slam hit the autopilot took was the first defect: seeds 1-200 a hero, slam hits a fight 0.06 -> 0 (Vanguard) and
0.12 -> 0 (Ilya), the fight's P50 unchanged (53 and 51 s), clears 37.0 -> 37.0 % and 79.0 -> 80.0 % (Ilya's two
deaths to a slam came at the enrage). The Warden lever table moves within a few points; the 2026-11-03 choice of share
8.5 with a 1.5 s recovery stands (TestResults/warden-fix-1104/).

## Status 2026-11-10: the Sword is the Warden's weapon

**The decision** (open decision 3, the Sword's role without the Staff; made 2026-11-10 on the owner's behalf from the
research on `claude/research-sword-1104`, docs/cloud/2026-11-04-sword-role.md there): the Sword deals x1.5 to the
foundry's gate boss, the Forge Warden, and nothing else changes. Roles: Staff = horde, Sword = Warden. The research's
other levers (sweeps, arcs, cleave on kill, knockback, reach, a faster Sword) gave the Sword no role; a faster Sword did,
but it is the Sword's shared interval and lifted every cohort.

**The rule, in one place.** `WardenRule.SwordId` (`weapon_sword`, Weapon_Sword.asset's id), `WardenRule.SwordShare` =
1.5, `GateBossShareOf(weaponId)` and `DamageOn(amount, share, gateBoss)`, the one never-inlined body of the product.
`WeaponLoadout.TryAdd` hands each weapon its share by its id (`WeaponRuntime.GateBossShare`), and every landing of
`WeaponRuntime.TryAttack` - the strike, the cleave or splash, the kill burst, the splinter, the pulse - goes through
`DamageOn` with the gate boss the strike is handed. The scene hands it from `WardenSlamService` (-40, before every
attacker) to the hero's `AttackController.GateBoss`; the simulation passes `StepWarden`'s `_slamBoss` from
`HeroAttacks`, the same point of the frame. Only the foundry's gate boss is one (`EncounterController.GateRoom`): the
hall arena's Warden, the Descent's bosses, the elites and the horde take the Sword's authored damage, and the Sword's
values (Weapon_Sword.asset, shared with the hall and the Descent) do not move. Floor 2's gate boss is the Forge Warden
too (Floor_QuicksilverVaults' boss room: the Warden first, two Cinder Mites beside it), so the rule applies there; the
escorts take the authored damage. No card text states a weapon's damage on bosses, so nothing in the UI changed.
`WardenRuleTests` hold the rule and the strike, `FoundrySimulationTests.TheSimulationsWeaponsTakeTheGamesShareOnTheWarden`
the simulation's weapons, `WeaponContentTests.TheWardensWeaponIsTheSwordAsset` the asset's id, and `WardenFlowTests`
the scene's hand-over.

**Before and after** (the foundry simulation, seeds 1-200 a hero, `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`; main e8e22d5 against the rule; TestResults/sword-warden-1110/):

| | Before | After |
|---|---|---|
| The Warden a second, the Vanguard started with each (P50): Sword / Staff / Daggers | 18 / 23 / 0 | 27 / 22 / 0 |
| The horde a second, the same (P50): Sword / Staff / Daggers | 10 / 30 / 2 | 10 / 30 / 2 |
| Three weapons with distinct roles | not met: the Sword and the Daggers without | not met: the Daggers without |
| Warden fight P50, floor 1: Vanguard / Ilya | 64 / 55 s | 56 / 50 s |
| Warden fight P50, floor 2: Vanguard / Ilya | 84 / 73 s | 70 / 64 s |
| Floor 1 cleared, Vanguard: careful / first-card / standing / clumsy / auto-farmer | 199 / 196 / 130 / 74 / 6 | 199 / 196 / 139 / 80 / 9 |
| Floor 1 cleared, Ilya: the same | 200 / 200 / 156 / 160 / 19 | 200 / 200 / 170 / 164 / 30 |
| Both floors cleared, careful: Vanguard / Ilya | 197 / 200 | 199 / 200 |

The careful clears do not move and the Warden fight stays inside step 4's 45-65 s on floor 1; floor 2's Vanguard no
longer dies at its Warden (2 -> 0). The weaker cohorts gain a few clears, most where the hero picks the Sword up (Ilya
standing 156 -> 170, auto-farmer 19 -> 30). The figures are the research's x1.5 row, reproduced on the shipped code.
The roles row stays not met on the Daggers alone: their 0 on the Warden is reach (1.8) inside the careful pilot's kite
ring (2.0), and their horde damage is under the Sword's; what the Daggers are is the next decision (the research's
recommendation 2).

## Status 2026-11-10: the decisions taken, and what waits

The owner adopted the careful autopilot and its cohorts (main e8e22d54) and left the other open decisions to the lead.
Taken, on the 2026-11-04 cloud research (docs/cloud/) and its local verification:

- **The Sword is the Warden's weapon** (open decision 3), x1.5 on the gate boss - the section above.
- **The floor keeps its gate at 3:30** (open decision 2), for now. Every candidate that shortens the wait makes the floor
  harder for the weaker cohorts: Set B (30 s hall beats, a wave more a hall, the gate at 2:45, horde XP 25 %) takes the
  clumsy Vanguard from 37 % to 23 % and Ilya from 80 % to 54 %, Set A to 23 % / 49 %, and none was read with the human
  realism below, under which the wait's farm likely matters more. The gate's levers stay in the batch
  (FOUNDRY_LEVERS=5 and 6) for that reading.
- **Difficulty is read with human hands from now on**: the cohorts' realism (reaction, aim, attention; the difficulty
  research's "typical human") is the measure of how hard a floor is; the frame-perfect careful pilot stays the check
  that a floor can be cleared. No structural hazard is built yet (slag and the enraged echo stay levers,
  FOUNDRY_LEVERS=7), and the Staff's splash is not cut: at 0.4 it takes the clumsy Ilya from 94 to 21 clears of 200,
  and it moves the Staff the Sword's share was measured against.

Also taken: **the Warden room's chest stays a heal** - since the extra elite moved the Warden's room from index 5 to 6,
ChestRule.RewardFor's even/odd rule makes its chest a heal (a quarter of the hero's health) where it was gold. The chest
is placed as the room's first wave begins, so it is a heal within reach during the Warden's fight - where the human-hands
batch loses most of its weaker heroes - and the measurements of 2026-11-04 and 2026-11-10 already played it. **The sprite atlas waits** for a
frame-time reading on the S23 (Sprite Packer V2 is a ProjectSettings change; the Editor's probe reads p95 5.9 ms at 40
alive, nothing to buy yet).

Still open: what the Daggers are (their Warden damage is reach inside the careful pilot's kite ring); floor 2's Warden
(careful P50 70 s Vanguard / 64 s Ilya with the Sword, against 45-65 s - its 2026-11-04 options were a share of about 6
with floor 2's Mend at 100 %, or RecoverySeconds 2.5); the Warden room's heal chest; the sprite atlas (a ProjectSettings
change); and installing the current APK on the S23.

## Status 2026-11-10 (later): floor 2's Warden

**The decision** (taken today on the owner's behalf, from the human-hands measurement, TestResults/human-hands-1110/
f2warden-*): floor 2's Forge Warden takes a health share of 8.5 / 1.4 (about 6.07) instead of `WardenRule.GateHealthShare`
8.5 - the climb's x1.4 on floor 2 cancelled on its Warden - so its fight lands in step 4's 45-65 s. Floor 1's Warden
keeps 8.5; floor 2's Mend (40 %) and the Warden's recovery (1.5 s) do not move.

**The rule, as data.** `FloorDefinition._foundryGateHealthShare` (0 = `WardenRule.GateHealthShare`) is 6.071429 on
Floor_QuicksilverVaults.asset (8.5f / 1.4f as a float) and 0 on Floor_EmberHalls. `WardenRule.GateHealthShareOn`, one
never-inlined body, turns a floor's value into its gate boss's share; `EncounterController.SpawnWave` reads it for the
gate boss and `ValidateFloor` refuses a NaN, negative or infinite share. The simulation's floor 2 carries the asset's
value (`FoundryFloor.GateHealthShare`) and `LaterFloorLevers` reads it through the same body; the research lever
`LaterWardenHealthShare` still overrides it and is off by default, and FOUNDRY_LEVERS=8's second row now plays the old
8.5. The Warden walks out of floor 2's gate with 300 x 6.071429 x 1.4 x difficulty = 2550 x difficulty: 4080 at 1.6
instead of 5712, floor 1's Warden's health at the same difficulty. `ClimbFloorContentTests`, `WardenRuleTests`,
`FoundrySimulationTests` and `FoundrySimulationSceneValueTests` hold floor 1's Warden unchanged, floor 2's to 4080 at 1.6
and the simulation's floor 2 to the asset.

**Before and after** (the foundry simulation, seeds 1-200 a hero, `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1
FOUNDRY_FLOORS=2`, frame-perfect and `FOUNDRY_REALISM=typical`; before TestResults/human-hands-1110/shipped-*, after
TestResults/f2-warden-ship-1110/; Vanguard / Ilya):

| | Before (share 8.5) | After (8.5 / 1.4) |
|---|---|---|
| Floor 2 Warden fight P50 (P90), careful, frame-perfect | 70 (89) / 64 (78) s | 58 (68) / 51 (61) s |
| Floor 2 Warden fight P50 (P90), careful, human hands | 68 (81) / 63 (74) s | 56 (65) / 50 (59) s |
| Both floors cleared, careful, frame-perfect | 199 / 200 | 199 / 200 |
| Both floors cleared, human hands: careful / standing / first-card / clumsy | 135 / 174; 41 / 50; 175 / 195; 19 / 35 | 147 / 181; 63 / 80; 179 / 197; 23 / 46 |
| Careful deaths at floor 2's Warden, human hands | 18 / 20 | 6 / 13 |
| Both floors cleared, frame-perfect: standing / clumsy | 71 / 83; 63 / 118 | 100 / 121; 67 / 137 |

The numbers are the measurement's "F2 share 6.07" row to the seed. Floor 1 is the same floor run for run: its
cohort tables, its gate and every floor-1 row of foundry-batch.csv are identical before and after. Floor 2's careful
Warden now sits inside 45-65 s with either hands; the standing pilot, who trades blows with the Warden, gains most.
