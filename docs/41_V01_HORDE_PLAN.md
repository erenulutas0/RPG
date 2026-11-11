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

## Status 2026-11-10 (night): the careful pilot's cards

**The finding.** With human hands (`FOUNDRY_REALISM=typical`) the first-card cohort cleared more of floor 1 than the
careful one, 181 / 199 against 154 / 194 (Vanguard / Ilya, seeds 1-200), though frame-perfect they tie. The careful
cohort is meant to be the best play, so this was a defect of the measuring pilot, and the difficulty research's
reading held: `AutopilotCohort.CardScore` was tuned for a pilot that is never hit.

**The cause: every weapon line scored above every tome level.** The score was health when low 100, a weapon's line or
evolution 50, a new weapon 45, the first two tomes 40, a tome's level 30, a health card 25, anything else 20. So the
careful pilot spent two thirds of its cards on lines - every weapon to its last level, the Daggers too - and took
half of its few tome levels after the fourth minute. A tome's level raises every weapon held (Force, Tempo and Precision each
weapon's damage, speed or crits, Reach every circle - the Sword's cleave and the Staff's splash) or the hero (Vitality);
a line raises one weapon. Frame-perfect, the kite keeps the horde off whatever the kill speed. With a 0.25 s reaction
the late kite is struck, and the horde reached the careful Vanguard: 42 health a run from the horde against the
first-card pilot's 29, and deaths in the halls (rooms 2-3, the horde's Grunts) where the first-card pilot had few.
CardScoreResearch (below) laid the two side by side over the batch (typical hands, floor 1, the Vanguard; Ilya alike):

| | Careful (main 18715c94) | First-card | Careful now |
|---|---|---|---|
| Cleared, Vanguard / Ilya | 154 / 194 | 181 / 199 | 195 / 199 |
| Deaths, Vanguard: where / blow | 46: room 3 18, boss 12, room 2 12, elite 4 / horde Grunt 15, slam 9 | 19: room 3 8, room 2 7, boss 4 / horde Grunt 7, wave Tank 7 | 5: boss 4, room 2 1 / slam 2 |
| Health lost a run: wave / horde / Warden / slams / pit | 56 / 42 / 10 / 19 / 10 | 50 / 29 / 8 / 17 / 10 | 46 / 23 / 10 / 32 / 11 |
| Cards a run: lines / new weapons / new tomes / tome levels / hero cards | 19.5 / 1.8 / 2.1 / 4.6 / 1.0 | 13.3 / 1.9 / 3.0 / 8.5 / 4.2 | 12.7 / 2.0 / 2.3 / 13.1 / 2.4 |
| Tome levels in the first three minutes | 1.7 | 5.6 | 8.9 |
| Maximum health / armour at the Warden, P50 | 127 / 0 | 151 / 20 | 190 / 40 |
| Lowest health, P50 | 27 % | 48 % | 54 % |
| Clears with an evolution | 116 | 70 | 163 |

Moving one weight at a time on the careful score (typical hands, clears V / I) singled the lines out: health or armour
cards over every line 154 / 200; health and armour by tier alone 159 / 194; Vitality first among the tomes and its
levels over the lines 151 / 199; no evolutions 160 / 195; a third tome 160 / 191; health first under 70 % instead of
50 % 153 / 198. Against that, the Sword's lines under the tomes' levels 193 / 194, the Daggers' 182 / 198, every line
199 / 196 - while the Staff's lines under them 145 / 175 (its lines earn their place), and the first card unless a line
is offered 91 / 186.

**The fix** (`AutopilotCohort.CardScore`, one integer body that `DevelopmentAutopilot.ChoiceSlot` and the simulation's
`ChoiceSlot` both reach through `AutopilotCohort.Pick(offer, low, upgrades)`; each card at the tier it is offered):

| Score | Card |
|---|---|
| 100 | health when the hero is under half (unchanged) |
| 50 | an evolution |
| 45 / 10 | a new weapon while a slot is free / past the cohort's wanted count (unchanged) |
| 40 / 15 | a new tome while under two / a third (unchanged) |
| 31 | a line toward its weapon's evolution (`LeadsToEvolution`: the weapon held, not evolved, below level 5, its tome held) |
| 30 | a tome's level (unchanged) |
| 28 | any other line (was 50) |
| 25 / 20, + 10 a tier | a health card / an armour card (`TierStep`; was 25 / 20 at every tier) |
| 20 | any other card (unchanged) |

The lines under the tomes' levels are the fix. The other two parts keep the pilot measuring what it measured: with
every line at 28 the careful pilot clears as many (200 / 199 with human hands) but evolves in 21 / 18 clears of 200, and
the gate's synergy row, which reads it, would fall from 86 % to about 12 %; a line toward its weapon's evolution at 31
keeps the evolutions (84 %, first evolution P50 217 -> 132 s). Without the tiers that path costs Ilya (190 / 194 with
human hands); a health or armour card's tiers scale 1 / 2 / 4 / 5.3 where a line's scale 1 / 1.25 / 1.5 / 2, so a rare
one now outranks a line and a tome's level, an epic one a new tome, a gold health card a new weapon. On seeds 201-400
(the check that this is not fitted to 1-200) with human hands: before 149 / 189, first-card 175 / 200, now 199 / 197;
frame-perfect 199 / 200, 190 / 200, 200 / 200. Ilya's 197 against 200 there is within the batch's noise.

**Before and after** (the foundry simulation, seeds 1-200 a hero, `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1
FOUNDRY_FLOORS=2`, frame-perfect and `FOUNDRY_REALISM=typical`; TestResults/cardscore-1110/before-* and after-*;
Vanguard / Ilya):

| Cohort | Floor 1, frame-perfect | Floor 1, human hands | Both floors, frame-perfect | Both floors, human hands |
|---|---|---|---|---|
| careful | 199 / 200 -> 200 / 200 | 154 / 194 -> **195 / 199** | 199 / 200 -> 200 / 200 | 147 / 181 -> 193 / 195 |
| standing | 139 / 170 -> 193 / 193 | 110 / 141 -> 179 / 180 | 100 / 121 -> 176 / 176 | 63 / 80 -> 151 / 155 |
| first-card | 196 / 200 (the same) | 181 / 199 (the same) | 196 / 200 (the same) | 179 / 197 (the same) |
| clumsy | 80 / 164 -> 104 / 184 | 43 / 98 -> 82 / 141 | 67 / 137 -> 96 / 162 | 23 / 46 -> 66 / 105 |
| auto-farmer | 9 / 30 (the same) | 9 / 23 (the same) | 1 / 4 (the same) | 1 / 3 (the same) |

The careful cohort now clears at least as many as the first-card one with human hands on both floors and both heroes,
and frame-perfect it loses none. The standing and clumsy cohorts score their cards the same way and gain most; the
first-card and auto-farmer cohorts take the first card and do not move. The gate (frame-perfect careful): synergy
85.9 % -> 84.0 % of the Vanguard's clears (met); Warden fight P50 (P90) 56 (64) -> 58 (70) s and Ilya 50 (57) -> 51 (62) s,
floor 2's 58 / 51 s unchanged (inside 45-65 s); maximum health at the Warden 112 -> 184 and 90 -> 172; floor time P50
289 -> 292 s and 284 -> 285 s; engaged uptime P50 0.70 -> 0.60 and 0.56 -> 0.58 (not met before or after: the horde
dies before it closes); the roles row moves within a point or two a second (Sword 10 -> 8 on the horde, 27 -> 26 on the
Warden; Staff 30 -> 31, 22 -> 23) and stays not met on the Daggers. Only the autopilot's choices changed: no card,
weapon or floor value moved.

**What this changes in the readings.** With the corrected pilot the floor is easier in human hands than the 2026-11-04
difficulty research read: careful 98 % / 100 % on floor 1 with the typical human (the research read 75 % / 93 %, main 18715c94
77 % / 97 %), standing 90 % / 90 %, clumsy 41 % / 71 %. Its cohort bands and its structural candidates (slag, the enraged echo) were measured on the
old picker and want a re-read before any band is pinned.

**What was built.** `AutopilotCohort.CardScore(card, tier, low, weaponsHeld, tomesHeld, towardEvolution)`, `TierStep`,
`LeadsToEvolution` and `Pick(offer, low, upgrades)`. In the simulation, research only and off by default:
`FoundryLevers.LogsPicks` (every offer and the card taken, `FoundryResult.PickLog` of `FoundryPick`) and
`FoundryLevers.Picker` (a picker in place of a scoring cohort's own). `Tools/CombatChecks/CardScoreResearch.cs`
(explicit; `CARDS_REALISM`, `CARDS_PICKERS`, `CARDS_DETAIL`): the cohorts' cards side by side and every candidate and
ablation above by name (`before` is main 18715c94's score, `syn-dt31` the shipped one, `lines-under-tomes` the fix
without its way to the evolutions). `AutopilotCohortTests` holds the order, the tiers and `LeadsToEvolution` on
constructed offers; `FoundryCohortTests` counts the clumsy cohort's weapons over the floor rather than at the gate (its
Vanguard of seed 33 now dies before the Warden walks out).

## Status 2026-11-10 (night): the Daggers' role

**Where it stands: no Daggers role ships.** A x12 Daggers share on the elite rooms' Grunt Captains met a three-job roles
row and was reverted: the lead did not merge it (below). The root cause it rode on, the careful pilot's spacing, was
measured next as a habit of the pilot's. A pilot that steps in on its target makes every weapon's elite fights three
times shorter. It does not give the Daggers a lead on the elite rooms' waves with human hands, alone or with a readable
Daggers mechanic of x2 at most. What ships is the measurement:
- the simulation counts the elites;
- the Daggers' research levers and their report;
- the step-in, built in the scene and the simulation, off for every cohort;
- the gate's roles row reading three jobs, which reads not met: the Daggers have no job.

Measured on main 4791f86f and 511a380b (the careful pilot's cards). Seeds 1-200 a hero, both floors, every cohort, the
Vanguard started with each weapon, frame-perfect ; `FOUNDRY_REALISM=typical`. `TheDaggersRoleOverASeedBatch`,
TestResults/daggers-role-1110/.

**The elites, as the gate reads them.** The game's elite is a room (RoomKind.Elite): an elite room's wave is a Grunt
Captain with a Grunt and two Cinder Mites. Floor 1 has two of them, V0.1's second elite included, and floor 2 has the
Sentry Hall. The halls' waves and surges hold none: they are Grunts, Runners, Tanks and Mites. A weapon's damage on the
elites a second is what it took off those waves (`FoundryWeaponWork.DealtElite`) over the seconds such a wave stood with
the hero alive, from its spawn to its last fall (`FoundryResult.EliteFought`). The figure is the P50 over the runs that
met one, with the Vanguard started with each weapon, as the gate reads the horde and the Warden. The research reads the
Captains alone beside (`DealtEliteLeader`). The gate keeps the whole waves, the definition set before the batches. The
roles row (`FoundryGate`) reads three jobs: horde, elites and Warden. A weapon's role is a job on which it leads every
other weapon by more than 10 %. The batch report on this branch (`FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`)
reads it not met: "the Daggers (horde 2 to the Staff's 31, elites 5 to the Staff's 21, Warden 0 to the Sword's 26)". The
gate stands at met 7, not met 2 (engaged uptime, the roles).

**Before** (nothing in this section changes it; Sword / Staff / Daggers, frame-perfect ; human hands):

| | |
|---|---|
| Horde a second | 8.2 / 31.1 / 1.7 ; 8.0 / 30.6 / 1.5 |
| Elites (the waves) a second | 9.5 / 20.8 / 5.4 ; 9.5 / 20.6 / 4.9 |
| The Captains alone a second | 6.2 / 12.2 / 4.4 ; 6.1 / 12.2 / 4.4 |
| Warden a second | 26.4 / 23.3 / 0 ; 27.2 / 22.9 / 4.0 |
| Floor 1 cleared, careful / first-card / standing / clumsy / auto-farmer, Vanguard | 200 / 196 / 193 / 104 / 9 ; 195 / 181 / 179 / 82 / 9 |
| The same, Ilya | 200 / 200 / 193 / 184 / 30 ; 199 / 199 / 180 / 141 / 23 |
| Both floors, Vanguard | 200 / 196 / 176 / 96 / 1 ; 193 / 179 / 151 / 66 / 1 |
| Both floors, Ilya | 200 / 200 / 176 / 162 / 4 ; 195 / 197 / 155 / 105 / 3 |
| Warden fight P50, floor 1 / floor 2, Vanguard / Ilya | 58 / 51, 58 / 51 s ; 53 / 48, 53 / 49 s |

**What binds the job: the pilot's kite.** Whatever the Vanguard starts with, the elite rooms' fights last P50 14.3 s
(two rooms), and its weapons together take 35-36 a second off them. The job is a share of that total: the Staff takes
59 % (reach 2.4, a 3.5 splash), the Sword 27 % and the Daggers 15 %. The careful pilot kites a Captain from its reach
plus 0.15 (1.75 units) and every other enemy alike, and the Daggers reach 1.8. They touch the pack last, after whatever
the Vanguard picked up and its Forge Burst. Held at the Daggers' reach (the step-in, below), the same fights last 4.4 s
and every weapon's rate on them triples (103-111 a second together): the fights' length was the kite, not the pack's walk.

**The x12 on the Captains: measured, shipped, reverted.** A Daggers share of 12 on the elite rooms' Captains, through
one `EliteRule` that the scene and the simulation struck through, met a three-job roles row that read the Captains as
the elites. It held on seeds 1-200: the Daggers 12.5 to the Staff's 10.0 frame-perfect and 11.6 to 9.4 with human
hands, the Sword's Warden lead 16 % and 11 %, no cohort's floor-1 clears moving more than 6. It held on seeds 201-400
too: 12.4 to 10.1 and 11.7 to 9.6. Below 12 the Sword lost the Warden with human hands (x8, x10: +9.5 %). On the whole
waves no share leads: x6-x10 gives 12-14 a second against the Staff's 19-20. The lead did not merge it, and 5ef52402
reverts 54e1850f:
- a hidden x12 on one enemy is opaque to a player;
- it one-shoots the Captain whenever a human closes in, and a human holding the Daggers closes in, so the elite rooms
  become trivial with that weapon;
- it stayed modest only because the careful pilot holds the Captain at 1.75 against a reach of 1.8;
- the Captain-alone reading was chosen after the whole waves' failed.

**The pilot's spacing.** `AutopilotCohort.Habit.StepsIn` (and `StepsInOnTheGateBoss`, a habit of its own), with
`FootworkRule.StepIn` as the one body that `DevelopmentAutopilot` and the simulation call.
- The target: the elite room's Captain (`EncounterController.EliteLeader`), or the gate boss with the second habit,
  within 5 units.
- The trigger: the pilot's best weapon on that one target (`FootworkRule.SingleTargetRate`: damage, rhythm and rolled
  crits, the gate boss's share) reaches less than 2.0 units, and it is safe: no mark down, health at half or more, the
  pit's band not glowing.
- The step: it holds the target at that weapon's reach less 0.15, in or back out, rather than kiting.
- Both habits stand outside `Habit.All`, so no cohort has them. The research gives them to the careful and first-card
  cohorts (`Lever.CarefulHabits`); the standing, clumsy and auto-farmer cohorts play as they are.
- `FootworkRuleTests` and `AutopilotCohortTests` hold the body and the habits.

| Lever | Elites (waves): Daggers to the best other | The Captains alone | Warden: Sword to Staff | Warden fight P50, floor 1, V / I | Floor-1 clears, largest move | Verdict |
|---|---|---|---|---|---|---|
| Steps in, the Captains and the Warden | 29.9 to 45.2 ; 22.8 to 38.8 | 25.2 to 24.1 ; 18.0 to 22.2 | 29.9 to 24.4 ; 30.6 to 25.0 | 48 / 43 ; 47 / 43 s | -4 (Ilya careful, human hands) | the Warden under 45 s |
| Steps in on the Captains | 29.9 to 45.2 ; 22.8 to 38.8 | 25.2 to 24.1 ; 18.0 to 22.2 | 27.4 to 23.4 ; 27.0 to 23.7 | 55 / 49 ; 51 / 46 s | +-4 (careful, human hands) | within every bound; no job for the Daggers |
| ...+ the Daggers x2 on the elites | **38.3 to 34.2 (+12 %) ; 30.3 to 31.6 (-4 %)** | 34.2 to 15.8 ; 24.2 to 16.6 | 27.1 to 24.0 ; 27.6 to 23.8 | 56 / 51 ; 51 / 45 s | +8 (Ilya auto-farmer), -9 (Ilya clumsy, human hands) | no lead with human hands |
| ...+ the Daggers x2 on the Captains | 37.7 to 33.7 ; 28.4 to 31.1 | 32.5 to 15.4 ; 24.3 to 16.2 | 26.8 to 24.0 ; 27.6 to 23.7 | 56 / 50 ; 51 / 45 s | +6 (Ilya auto-farmer) | no lead with human hands |
| ...+ the Daggers' crits x2 on the elites | 36.2 to 37.5 ; 28.8 to 35.9 | 30.5 to 19.2 ; 23.3 to 18.6 | 27.8 to 23.4 ; 27.2 to 24.1 | 55 / 50 ; 51 / 46 s | +8 (Ilya auto-farmer), -7 (Ilya clumsy), human hands | no lead |
| The Daggers x2 on the elites, no step-in | 8.3 to 20.3 ; 7.9 to 20.6 | 7.0 to 11.3 ; 6.7 to 11.4 | 28.0 to 23.4 ; 26.6 to 23.1 | 57 / 51 ; 52 / 46 s | +8 (Ilya auto-farmer) | no lead |

(Frame-perfect ; human hands. The x2 shares are research levers, `FoundryLevers.DaggersEliteDamage`,
`DaggersEliteCritDamage` and `DaggersEliteLeaderOnly`, read by the simulation alone. The step-in on the Warden too reads
the elites as the step-in on the Captains does: the runs are the same until the gate.)

The step-in moves the floor-1 clears of no cohort by more than 4: the careful pilots keep their kite against everything
but the one target, and step out when hurt. With the Warden included it takes the Warden's fight under step 4's 45 s
(Ilya 43 s), since every weapon then reaches the Warden. A x2 on the elites with the step-in gives the Daggers the
waves frame-perfect, 38 to 34, but not with human hands, where the Staff keeps them 32 to 30. On the Captains alone
the Daggers lead with every x2. That is the reading this round does not take, for the reason the x12 was reverted.

**The decision.** Nothing ships to the game. Turning the step-in on for the careful pilot on the Captains alone passes
every bound the lead set for a pilot-fidelity fix: clears within 4, the Warden 55 / 49 s and 51 / 46 s, the Sword's lead
17 % and 14 %. It is not turned on here, for three reasons:
- it changes the instrument every gate row reads: the elite fights go from 14 s to 4-5 s, and the Warden's from 58 / 51
  to 55 / 49 s frame-perfect;
- engaged uptime is being re-read with that pilot in parallel;
- it gives the Daggers no job.

Turning it on is one line, adding `Habit.StepsIn` to `AutopilotCohort.Careful` and `FirstCard`, and it is the lead's
call. With the gate boss too it needs the Warden's health re-read first.

**What a Daggers job would take.** With the pilot holding them at their reach, the Daggers are level with the Staff on a
single elite (25 to 24 frame-perfect) and far behind on the waves (30 to 45): the Staff's splash owns any pack. A job
the Daggers can lead without a hidden share is one read on single targets the Staff cannot splash, such as the Captains
or the Warden's escort-free fight. That is a change of the gate's reading, for the owner, and it was not taken here.
Otherwise the Daggers stay the between weapon.

## Status 2026-11-11: engaged uptime

**The question.** The gate's "Engaged uptime" row (proposed, >= 0.75 over every run) read 0.60 / 0.58 (Vanguard / Ilya,
the frame-perfect careful pilot, seeds 1-200) and 0.62 / 0.62 with human hands, while combat uptime reads 0.95. Its
rule was the lead's of 2026-10-29 (HordePacingMeasurement): an enemy within the first weapon's reach + 1 unit, or a hit
taken in the last 1.5 s. The careful pilot's cards (the status above) took the Vanguard's from 0.70 to 0.60.

**How it was measured.** `FoundryLevers.CountsEngagement` (accounting only, off by default) splits a floor's active
seconds by phase and reads every candidate rule on the same frames (`FoundryResult.Engagement`, `FoundryEngagement`);
`Tools/CombatChecks/EngagedResearch` (explicit) plays the careful cohort frame-perfect and with `PilotRealism.Typical`
on seeds 1-200 a hero, with main 18715c94's card score and the floor's horde and gate levers as candidates
(TestResults/engaged-1111/research-shipped, research-levers, research-final).

**The cause: the rule read how long the horde stood near the hero, not whether the hero fought.** Frame-perfect careful
pilot, Vanguard / Ilya; a phase's readings are shares of its seconds; "lost" is the phase's share of the seconds the old
rule left out:

| Phase | Seconds a run | Lost | Old rule | An enemy within the longest weapon's reach (or a hit) | A strike landed or a hit taken in 1.5 s | An enemy alive |
|---|---|---|---|---|---|---|
| Walk to a hall | 38 / 37 | 23 / 21 % | 0.27 / 0.32 | 0.04 / 0.03 | 0.68 / 0.68 | 0.99 / 0.99 |
| Node fights | 52 / 48 | 7 / 6 % | 0.83 / 0.86 | 0.49 / 0.48 | 0.93 / 0.93 | 1.00 / 1.00 |
| Between (advance delays, the forge) | 18 / 18 | 6 / 5 % | 0.61 / 0.67 | 0.33 / 0.33 | 0.99 / 0.99 | 1.00 / 1.00 |
| The gate's wait | 113 / 117 | 44 / 47 % | 0.53 / 0.52 | 0.12 / 0.12 | 0.96 / 0.95 | 1.00 / 1.00 |
| The Warden | 58 / 52 | 9 / 9 % | 0.82 / 0.79 | 0.55 / 0.53 | 0.93 / 0.92 | 1.00 / 1.00 |
| The exit | 15 / 15 | 12 / 12 % | 0.01 / 0.01 | 0.01 / 0.01 | 0.10 / 0.10 | 0.00 / 0.00 |
| The floor, P50 of every run | 292 / 285 | | 0.60 / 0.58 | 0.26 / 0.23 | 0.88 / 0.87 | 0.95 / 0.95 |

- The horde falls to a strike or two (`HordeRule.HealthShare` 0.3), so it seldom stands near the hero: an enemy within
  the reach of the longest weapon held on 0.26 / 0.23 of the floor, while some weapon lands a strike within any 1.5 s on
  0.88 / 0.87. A horde enemy counted toward the old rule only for the second or so it took to cross the margin's unit
  before it fell. On the frames the old rule left out, the nearest enemy stood within 5 units two thirds of the time
  (39 / 36 % within 4), and nothing stood at all an eighth of it (mostly the exit: the horde is gone once the Warden
  falls).
- So it fell as the hero killed faster and rose as the horde got tougher, the fighting unchanged: the horde at 0.4 / 0.5
  of its authored health reads 0.63 / 0.67 (Vanguard), strikes 0.88 either way.
- And it moved with one card. The first weapon's reach counts the Sword's reach line (`line_sword_range`, +0.3 to +0.6 a
  pick). Main 18715c94's score took more of them (every line at 50): the Vanguard's first reach P50 at the floor's end
  2.8 against 2.5 now, and the rule 0.70 against 0.60. Read at the Sword's authored reach (2.1) it is 0.50 with either
  score, and strikes read 0.89 against 0.88; Ilya's Staff has no reach line, 0.56 -> 0.58. The 2026-11-10 drop was the
  reach line, not less fighting.
- The longest weapon's reach in place of the first's (the lead's example) reads 0.65 / 0.62 frame-perfect and
  0.66 / 0.65 with human hands. It counts the Vanguard's Staff and Ilya's later weapons, but keeps the reach line and the
  kill speed in the reading (0.71 / 0.65 on main 18715c94's cards).

**(b) No gameplay lever lifts it within the bounds.** The careful cohort, seeds 1-200; the old rule frame-perfect and
with human hands, the floor-1 clears with human hands (shipped 195 / 199), the Warden fight and the floor P50
frame-perfect:

| Lever (foundry only) | Old rule, frame-perfect | Old rule, human hands | Careful clears, human hands | Warden P50 | Floor P50 |
|---|---|---|---|---|---|
| Shipped | 0.60 / 0.58 | 0.62 / 0.62 | 195 / 199 | 58 / 51 | 292 / 285 |
| Horde health 0.4 (`HordeRule.HealthShare` 0.3) | 0.63 / 0.62 | 0.67 / 0.66 | 189 / 189 | 58 / 51 | 293 / 285 |
| Horde health 0.5 | 0.67 / 0.67 | 0.71 / 0.72 | 167 / 170 | 58 / 52 | 292 / 285 |
| Horde targets x1.5 (6/15/24/9) | 0.60 / 0.60 | 0.62 / 0.62 | 195 / 197 | 56 / 50 | 290 / 284 |
| Horde targets x2 (8/20/32/12) | 0.59 / 0.59 | 0.62 / 0.62 | 194 / 197 | 57 / 50 | 291 / 284 |
| Horde fill 3 a second (2) | 0.62 / 0.61 | 0.66 / 0.65 | 197 / 197 | 53 / 49 | 287 / 282 |
| Horde growth 1.5 (0.5) | 0.59 / 0.58 | 0.62 / 0.62 | 198 / 198 | 58 / 51 | 291 / 284 |
| Siege of hall 1 through the wait | 0.66 / 0.67 | 0.70 / 0.70 | 186 / 184 | 56 / 49 | 292 / 284 |
| Halls held 30 s | 0.63 / 0.63 | 0.67 / 0.67 | 194 / 192 | 54 / 48 | 288 / 283 |

None reaches 0.75 frame-perfect (the best 0.67), and the two that move it most cost the careful cohort clears with human
hands past the ~10 allowed - 28 / 29 (horde health 0.5) and 9 / 15 (the siege) - before any weaker cohort is read, so the
other cohorts were not run. Every lever keeps the Warden (48-58 s) and the floor (282-293 s) in their bands. What lifts the
old rule is the horde living longer near the hero, which is danger. A bigger or faster-growing crowd does not move it
(0.58-0.60): the hero kills what comes as it comes. Strikes stay 0.87-0.89 under every lever.

**(a) The rule corrected - shipped.** docs/40 section 8.4 calls a weapon's uptime its "attack and target-finding uptime"
(U_weapon in DPS_horde) and section 9.6 puts attack uptime second in its tuning order; docs/41 step 2's own uptime is
combat uptime, met at 0.95. The engaged row now reads that attack uptime for the whole loadout: **a strike of any weapon
the hero holds landed, or a hit taken, in the last 1.5 s.** The old rule's own window on both sides; every weapon's
reach by construction (a strike lands only on a target its weapon acquired within its reach); no margin and no first
weapon; the same on either card score (0.89 / 0.88 and 0.88 / 0.87). It still leaves out the walks (0.68) and the exit
(0.10), so it stays stricter than combat uptime where the hero is not fighting. It counts the gate's wait as fighting
(0.96): the hero kills the horde there as it comes, and the wait has its own row ("Time waiting for the gate").

**What was built.** `DescentSimulation.EngagedSeconds` (1.5; `EngagedAfterHitSeconds` is gone, `EngagedMargin` stays
for the research's old rule): the simulation counts a frame engaged within it of the hero's last landed strike (any
weapon, `HeroAttacks`) or its last hit taken. `HordePacingMeasurement` reads the same off `AttackController.StruckWith`
(every weapon's landed strike) and writes it as an `engaged_strike` column; the batch report reads `engaged_strike` and
leaves an older file's `engaged` column unread, so the gate's Editor cell and the parity table's Editor engaged read
"not in this file" / "-" until the Editor is run again. The gate row's target and basis and the cohort table's legend
name the rule. `FoundrySimulationTests` now holds engaged as a share of the floor: a strike's 1.5 s can outlast the last
enemy, so it is no longer a part of the uptime by construction (over the batches below it stays under it in every run,
by 0.001 at the least). No gameplay code or value moved.

**Before and after** (the default batch: `FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`,
frame-perfect and `FOUNDRY_REALISM=typical`; TestResults/engaged-1111/before-* and after-*). Only engaged figures
change: in foundry-batch.csv, foundry-batch-floor2.csv and foundry-cohorts-floor2.csv the `engaged` column alone, and in
the reports the cells below (and the gate's tally line, met 7 -> 8, not met 2 -> 1; the batch table's wall-clock seconds
aside); every clear, death, floor time, Warden fight and roles figure is the same to the digit.

| Where | Before | After |
|---|---|---|
| Gate row, Vanguard (P10 / P50 / P90, clears' P50) | 0.44 / 0.60 / 0.71, 0.60 - not met | 0.84 / 0.88 / 0.90, 0.88 - met |
| Gate row, Ilya | 0.47 / 0.58 / 0.69, 0.58 | 0.83 / 0.87 / 0.90, 0.87 |
| Gate row, the Editor (2026-11-04 files) | 0.71 / 0.53 | not in this file (written before the rule) |
| Batch table and its gate list, Vanguard / Ilya | 0.60 / 0.58 | 0.88 / 0.87 |
| Parity table, simulation on the Editor's seeds 11-66 | 0.54 / 0.63 | 0.86 / 0.87 |
| Floor 2, uptime / engaged P50, Vanguard; Ilya | 0.87 / 0.56; 0.86 / 0.48 | 0.87 / 0.78; 0.86 / 0.77 |
| Cohorts, frame-perfect, Vanguard (careful / standing / first-card / clumsy / auto-farmer) | 0.60 / 0.61 / 0.63 / 0.74 / 0.74 | 0.88 / 0.88 / 0.88 / 0.89 / 0.91 |
| Cohorts, frame-perfect, Ilya | 0.58 / 0.59 / 0.57 / 0.60 / 0.62 | 0.87 / 0.87 / 0.87 / 0.89 / 0.91 |
| Cohorts, human hands, Vanguard | 0.62 / 0.62 / 0.66 / 0.76 / 0.74 | 0.88 / 0.88 / 0.88 / 0.90 / 0.92 |
| Cohorts, human hands, Ilya | 0.62 / 0.61 / 0.60 / 0.64 / 0.63 | 0.88 / 0.88 / 0.88 / 0.89 / 0.92 |

The old rule read the clumsy and auto-farmer pilots as the most engaged, since they let the horde close and take its
blows; the new one reads every cohort 0.87-0.92 - it measures the floor, whose horde keeps any pilot's weapons busy,
not the pilot's footwork. The gate (frame-perfect careful) is unchanged by the realism: its row reads the same with
`FOUNDRY_REALISM=typical`.

**Open.** The Editor's pacing files predate the rule: HordePacingMeasurement on seeds 11-66 for both heroes would put the
scene's `engaged_strike` beside the simulation's (expected about 0.86-0.88). The target stays docs/40 section 3.2's
0.75, an estimate for combat uptime that the stricter reading borrowed; a higher bar for attack uptime is the owner's
call. The gate's wait remains the floor's largest phase (113-117 s); it is engaged now, but what fills it is still
docs/41's open decision 2.

## Status 2026-11-11: last night's decisions re-read on the new cards

**Why.** The decisions of 2026-11-10 were all measured with the careful pilot's old card score:
- the Sword's x1.5 on the gate boss;
- the gate kept at 3:30 rather than Set A or Set B;
- floor 2's Warden at 8.5 / 1.4;
- difficulty read with human hands, the slag and echo candidates left as levers.

The night's fix ("the careful pilot's cards") moved every cohort that scores its cards. Each decision is measured again
here on main add6e2e0. Settings: seeds 1-200 a hero, `FOUNDRY_SEEDS=200 FOUNDRY_PARALLEL=4`, frame-perfect and
`FOUNDRY_REALISM=typical` ("human hands"). The outputs are in TestResults/reread-1111/. The shipped floor's clears,
Warden fights and roles row reproduce the "before" of the Daggers' role status (already on the new cards) to the digit.
Since 18715c94 only the card score moves these readings: the step-in habit is off for every cohort, the x12 was
reverted, and the engaged rule is accounting. Nothing changes in the game: this section is measurement only.

| Decision | Verdict | The numbers that decide it |
|---|---|---|
| 1. The Sword x1.5 on the gate boss | **holds**, on a thinner margin | Sword over Staff on the Warden: +13 % frame-perfect, +19 % with human hands (was +23 %); x1.4 no longer passes (+6 %) |
| 2. The gate at 3:30, not Set A / Set B | **holds** | with human hands the clumsy cohort still loses 30-33 (V) and 52-71 (I) clears of 200 to a set; the careful one now loses 5-9 too |
| 3. Floor 2's Warden at 8.5 / 1.4 | **holds** | careful fight 58 / 51 s frame-perfect, 53 / 49 s with human hands; at 8.5 the Vanguard's is 72 s; careful deaths there with human hands 1 / 4 (was 6 / 13) |
| 4. The difficulty bands and candidates | **flips**: floor 1 is now too easy for the careful cohort | careful 98 / 100 % with human hands (77 / 97 % on the old cards); slag, the research's first pick, no longer reaches it; only the Warden echo brings it under 90 %, and not within the Warden's 45-65 s |

### 1. The Sword's x1.5 on the gate boss: holds, on a thinner margin

The roles row's Warden job: the careful Vanguard started with each weapon, its own damage on the Warden a second of the
fight (P50); the Sword's lead over the Staff, the 10 % the row asks. Human hands from `TheDaggersRoleOverASeedBatch`
(`DAGGERS_LEVERS=0`, the shipped floor), the shares below x1.5 from `TheSwordsRoleOverASeedBatch` (`SWORD_LEVERS=0,17,26`,
FoundryBatch.SwordLevers, the list FOUNDRY_LEVERS=4 runs; frame-perfect only, the harness has no realism).

| Warden a second: Sword / Staff (the Sword's lead) | Old cards (e8e22d5, docs/cloud/2026-11-04-sword-role.md) | New cards |
|---|---|---|
| x1.5 (shipped), frame-perfect | 27 / 22 (+23 %) | 26.4 / 23.3 (+13 %) |
| x1.5, human hands | not read (the gate's weapons are frame-perfect) | 27.2 / 22.9 (+19 %) |
| x1.4, frame-perfect | 25 / 22 (+14 %, met) | 25.1 / 23.7 (+6 %, not met) |
| x1.3, frame-perfect | 23 / 23 (none) | 23.5 / 24.0 (-2 %) |
| Warden fight P50, floor 1, V / I (x1.5) | 56 / 50 | 58 / 51; human hands 53 / 48 |

**Holds.** The Sword leads the Warden by 13 % frame-perfect and 19 % with human hands, both over 10 %, and the Warden
fight stays in 45-65 s. The cushion is gone, though: on the old cards x1.4 was enough, on the new it is not. Under the
new score, which puts every weapon line under a tome's level, the Sword-started Vanguard deals the Warden 26.4 a second
where it dealt 27, and the Staff-started one 23.3 where it dealt 22; the Sword's spread widened too (P10-P90 18-42, was
21-34). Anything that moves either weapon or the pilot again (a Staff value, the step-in habit) wants this row re-read
before it ships.

### 2. The gate at 3:30 rather than Set A or Set B: holds

`FOUNDRY_LEVERS=6 FOUNDRY_COHORTS=1 FOUNDRY_LEVER_FILTER="horde XP 0.25"` (the shipped floor and the seven sets with a
25 % horde XP, the rows the 2026-11-10 decision read), frame-perfect and `FOUNDRY_REALISM=typical`, floor 1. Old: main
18715c94's cards, TestResults/human-hands-1110/sets-*. Set A: two more halls, three surges, the gate at 2:45. Set B:
halls held 30 s, three surges, the gate at 2:45. Floor-1 clears of 200, Vanguard / Ilya:

| Cohort | Shipped, old -> new | Set A, old -> new | Set B, old -> new |
|---|---|---|---|
| careful, frame-perfect | 199 / 200 -> 200 / 200 | 198 / 200 -> 200 / 200 | 199 / 200 -> 200 / 200 |
| careful, human hands | 154 / 194 -> **195 / 199** | 140 / 186 -> **188 / 193** | 136 / 181 -> **190 / 190** |
| standing, frame-perfect | 139 / 170 -> 193 / 193 | 135 / 183 -> 195 / 195 | 138 / 181 -> 194 / 194 |
| standing, human hands | 110 / 141 -> 179 / 180 | 100 / 141 -> 177 / 179 | 97 / 140 -> 176 / 182 |
| first-card, frame-perfect | 196 / 200 | 197 / 200 | 196 / 200 |
| first-card, human hands | 181 / 199 | 164 / 188 | 166 / 185 |
| clumsy, frame-perfect | 80 / 164 -> 104 / 184 | 49 / 105 -> 70 / 122 | 51 / 110 -> 85 / 137 |
| clumsy, human hands | 43 / 98 -> **82 / 141** (41 / 71 %) | 13 / 35 -> **49 / 70** (25 / 35 %) | 12 / 45 -> **52 / 89** (26 / 45 %) |
| auto-farmer, frame-perfect ; human hands | 9 / 30 ; 9 / 23 | 3 / 10 ; 3 / 10 | 8 / 22 ; 9 / 22 |

(The first-card and auto-farmer cohorts take the first card, so the new score does not move them.)

The careful cohort's floor (P50, every run) and the gate's wait (P50 of the clears), Vanguard / Ilya. The other cohorts'
clears wait within 5 s of it, the auto-farmer's 8-19 s longer; every run's wait, cohort by cohort, is in
foundry-gate-levers.csv.

| | Shipped | Set A | Set B |
|---|---|---|---|
| Floor, frame-perfect: old -> new | 289 / 284 -> 292 / 285 | 256 / 244 -> 251 / 245 | 242 / 237 -> 243 / 237 |
| Floor, human hands: old -> new | 281 / 279 -> 286 / 280 | 245 / 240 -> 247 / 239 | 235 / 233 -> 239 / 233 |
| Wait, frame-perfect: old -> new | 106 / 116 -> 114 / 117 | 1 / 11 -> 9 / 11 | 25 / 31 -> 29 / 31 |
| Wait, human hands: old -> new | 106 / 114 -> 111 / 115 | 1 / 8 -> 6 / 9 | 21 / 24 -> 21 / 24 |
| Clumsy, human hands: floor P50 (its clears' P50) | 246 (278) / 272 (277) | 112 (238) / 170 (237) | 108 (236) / 196 (233) |

**Holds.** The sets still cost the weak humans as many runs as before. With human hands the clumsy Vanguard loses 33
(Set A) and 30 (Set B) of 200, the clumsy Ilya 71 and 52 (old: 30 and 31, 63 and 53). What changed is the base under
them: the clumsy Vanguard goes from 41 % to 25-26 % where it went from 22 % to 6 %. The careful cohort pays too: 5-9
runs a hero with human hands, none frame-perfect (old: 8-18 runs of a smaller base). The standing cohort no longer
pays. The sets still do what they were for: the wait is 6-31 s instead of 111-117 s and the floor 233-251 s (Set B's
floor reads "not met" with human hands, at 239 s). So the trade is the one the decision weighed: the wait against the
weak cohort's clears.

### 3. Floor 2's Warden at 8.5 / 1.4: holds

`FOUNDRY_LEVERS=8 FOUNDRY_FLOORS=2 FOUNDRY_LEVER_FILTER="before 2026,F2 share 7"` (the shipped 6.07, the 8.5 it had
before, 7, and 7 with floor 2's Mend at 100 % or a 2.5 s recovery), frame-perfect and `FOUNDRY_REALISM=typical`. Old:
TestResults/human-hands-1110/f2warden-* (main 18715c94's cards; there 8.5 was shipped and 6.07 a lever). Floor 2's
Warden fight, from walking out of the gate to its fall, P50 (P90) over the felled fights; deaths in floor 2's Warden
room (of all floor-2 deaths); Vanguard / Ilya:

| | Share 6.07 (shipped), old -> new | Share 8.5, old -> new | Share 7, old -> new |
|---|---|---|---|
| Careful, Warden fight, frame-perfect | 58 (68) / 51 (61) -> 58 (69) / 51 (63) | 70 (89) / 64 (78) -> 72 (93) / 64 (84) | 63 (80) / 57 (66) -> 62 (82) / 57 (69) |
| Careful, Warden fight, human hands | 56 (65) / 50 (59) -> **53 (68) / 49 (60)** | 68 (81) / 62 (74) -> 65 (84) / 60 (74) | 60 (72) / 56 (65) -> 58 (73) / 54 (66) |
| Careful, deaths at floor 2's Warden, human hands | 6 / 13 -> **1 / 4** | 18 / 20 -> 1 / 6 | 12 / 14 -> 1 / 5 |
| Standing, deaths there, frame-perfect ; human hands | 39 / 48 ; 45 / 60 -> 17 / 17 ; 26 / 25 | 68 / 86 ; 67 / 90 -> 34 / 34 ; 50 / 48 | 51 / 61 ; 50 / 66 -> 29 / 20 ; 39 / 36 |
| Clumsy, deaths there, frame-perfect ; human hands | 7 / 23 ; 11 / 37 -> 5 / 13 ; 10 / 24 | 11 / 42 ; 15 / 48 -> 7 / 16 ; 16 / 41 | 7 / 30 ; 12 / 39 -> 3 / 17 ; 14 / 27 |
| Both floors cleared, human hands: careful / standing / clumsy | 147 / 181; 63 / 80; 23 / 46 -> 193 / 195; 151 / 155; 66 / 105 | 135 / 174; 41 / 50; 19 / 35 -> 193 / 193; 127 / 132; 60 / 88 | 141 / 180; 58 / 74; 22 / 44 -> 193 / 194; 138 / 144; 62 / 102 |

(Frame-perfect, the careful and first-card cohorts never die at floor 2's Warden at any of the three.) With 7, floor 2's
Mend at 100 % takes the standing deaths there to 15 / 17 and the clumsy to 1 / 9 with human hands, a 2.5 s recovery to
7 / 12 and 11 / 21; the careful fight does not move (58 / 54 s).

**Holds.** The careful Warden fight on floor 2 is where it was (58 / 51 s frame-perfect, 53 / 49 s with human hands,
inside 45-65 s), and at 8.5 the frame-perfect Vanguard is still out of the band (72 s, P90 93 s). What the new cards
changed is the deaths: with human hands the careful cohort goes down to floor 2 with 72 / 74 % of its health where it
went with 57 / 57 %, and dies at floor 2's Warden 1 / 4 times at 6.07 and 1 / 6 at 8.5. The share no longer decides
whether careful humans survive that fight, only how long it runs; for the standing and clumsy cohorts it still decides
it (8.5 doubles the standing deaths there). Share 7 sits inside the band as well (62 / 57 s, 58 / 54 s), as it did on
the old cards, at a cost to the standing cohort (13 / 11 more deaths with human hands); nothing in this batch argues for
leaving 6.07.

### 4. The difficulty candidates: floor 1 is too easy for the careful cohort

`FoundryDifficultyResearch` (`DIFFICULTY_PARTS=structural2,structural`): FOUNDRY_LEVERS=7's candidates and the
research's tuned ones, every cohort, frame-perfect, the typical human and the slow human, seeds 1-200 a hero, floor 1
(TestResults/reread-1111/d4-difficulty/). Old: docs/cloud/2026-11-04-difficulty.md (that day's main: the old cards and
no Sword share). Clears of 200, Vanguard / Ilya:

| Typical human | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| shipped, old | 149 / 185 | 100 / 119 | 182 / 198 | 32 / 94 | 6 / 16 |
| **shipped, now** | **195 / 199 (98 / 100 %)** | 179 / 180 (90 %) | 181 / 199 | 82 / 141 (41 / 71 %) | 9 / 23 |
| Warden echo 1.1 s, 10 %: old -> now | 69 / 107 -> **166 / 178 (83 / 89 %)** | 66 / 93 -> 161 / 159 | 149 / 185 -> 161 / 186 | 26 / 81 -> 71 / 128 | 0 / 6 -> 1 / 7 |
| Warden echo 1.1 s, 20 %: old -> now | 34 / 71 -> **136 / 149 (68 / 75 %)** | 39 / 61 -> 147 / 148 | 125 / 163 -> 142 / 172 | 23 / 67 -> 69 / 124 | 0 / 0 -> 0 / 1 |
| slag every 8 s: old -> now | 141 / 186 -> 193 / 196 | 79 / 124 -> 176 / 181 | 173 / 199 -> 177 / 199 | 20 / 79 -> 72 / 133 | 0 / 6 -> 0 / 8 |
| slag 8 s + echo 1.1 s, 10 %: old -> now | 59 / 85 -> 156 / 169 | 51 / 87 -> 157 / 163 | 138 / 175 -> 151 / 177 | 21 / 70 -> 67 / 121 | 0 / 1 -> 0 / 2 |
| Warden echo 0.8 s, 20 %: old -> now | 0 / 0 -> 13 / 20 | 0 / 0 -> 18 / 29 | 9 / 19 -> 16 / 23 | 3 / 23 -> 47 / 71 | 0 / 0 -> 0 / 0 |
| slag every 5 s: old -> now | 138 / 184 -> 193 / 190 | 74 / 123 -> 173 / 170 | 162 / 193 -> 160 / 193 | 14 / 73 -> 68 / 107 | 0 / 1 -> 0 / 5 |
| elite stomp: old -> now | 146 / 185 -> 197 / 200 | 89 / 125 -> 180 / 180 | 181 / 200 -> 184 / 199 | 36 / 94 -> 89 / 140 | 1 / 7 -> 4 / 12 |
| ambush surges: old -> now | 144 / 189 -> 197 / 199 | 96 / 136 -> 183 / 181 | 180 / 198 -> 181 / 199 | 36 / 105 -> 86 / 132 | 7 / 18 -> 8 / 27 |
| echo 0.8 s + slag 5 s + stomp: old -> now | 0 / 0 -> 10 / 22 | 0 / 0 -> 15 / 14 | 7 / 14 -> 13 / 24 | 4 / 12 -> 31 / 47 | 0 / 0 -> 0 / 0 |

| Frame-perfect careful, now | Clears | Warden fight P50 | Floor P50 |
|---|---|---|---|
| shipped | 200 / 200 | 58 / 51 s | 292 / 285 s |
| Warden echo 1.1 s (10 or 20 %: never struck) | 200 / 200 | **78 / 67 s** | 311 / 301 s |
| slag every 8 s | 200 / 200 | 56 / 50 s | 291 / 284 s |
| slag 8 s + echo 1.1 s, 10 % | 200 / 198 | 77 / 65 s | 312 / 300 s |
| Warden echo 0.8 s | 200 / 200 | 75 / 64 s | 309 / 298 s |
| slag every 5 s, elite stomp, ambush surges | 200 / 200 (ambush 200 / 199) | 55-57 / 50-51 s | 290-292 / 284-285 s |

The slow human (reaction 0.4 s): shipped careful 174 / 177 (87 / 89 %), standing 150 / 158, clumsy 65 / 119.

**The bands.** docs/40 section 10.2 sets target win rates by difficulty, for a skill cohort still to be named: Apprentice
65-80 %, Forged 40-60 %, Tempered 25-40 % and so on. The 2026-11-04 research read the careful cohort against 80-90 %.

**Floor 1 is too easy for the careful cohort.** With typical human hands it clears 98 / 100 %, above 80-90 % and above
every docs/40 band. The standing and first-card cohorts are at 90-100 % too. Of the cohorts a band could be meant for,
only the clumsy one (docs/40 9.4's Casual in shape) lands in one: the Vanguard in Forged at 41 %, Ilya in Apprentice at
71 %. Only the slow human brings careful under 90 % (87 / 89 %).

**Which candidate brings careful back.** Only the Warden echo:
- at 1.1 s and 10 %, careful lands at 83 / 89 %, inside 80-90 % for both heroes;
- at 1.1 s and 20 %, careful lands at 68 / 75 %, inside docs/40's top band (65-80 %).

Neither ships as measured:
- frame-perfect, the echo's second escape stretches the Warden fight to 78 / 67 s, out of step 4's 45-65 s (with human
  hands 66 / 59 s); it wants the Warden's health re-read with it, which this harness cannot combine;
- it is a test at the Warden that the careful and standing cohorts fail alike. At 10 % standing lands at 161 / 159,
  level with careful; at 20 % standing (147 / 148) clears more than careful;
- the 2026-11-04 recommendation was an enraged-only echo, which has no lever yet.

The other candidates:
- slag no longer separates anything at the top. Every 8 s: careful 193 / 196, standing 176 / 181. Every 5 s: careful
  193 / 190, standing 173 / 170. It costs the clumsy cohort 10 / 8 (8 s) and 14 / 34 (5 s), and the auto-farmer most
  of its few clears. On the old cards it cost careful 8 / 0 and 11 / 1;
- the echo at 0.8 s is still a cliff: with human hands the careful, standing and first-card cohorts clear 13-29 a hero,
  fewer than the clumsy one (47 / 71);
- the elite stomp and the ambush surges stay within the noise.

**The Staff's splash, not cut on 2026-11-10, holds too** (`DIFFICULTY_PARTS=staff`, Ilya's own Staff changed,
TestResults/reread-1111/d4-staff/; frame-perfect / human hands):

| Ilya | careful | standing | clumsy |
|---|---|---|---|
| shipped: old -> now | 200 / 185 -> 200 / 199 | 156 / 119 -> 193 / 180 | 160 / 94 -> 184 / 141 |
| splash share 0.4: old -> now | 200 / 156 -> 200 / 189 | 147 / 91 -> 182 / 159 | 118 / 21 -> 159 / 64 |
| splash radius 2.0: old -> now | 200 / 86 -> 200 / 166 | 91 / 34 -> 165 / 121 | 75 / 2 -> 124 / 34 |

The 0.4 share no longer brings careful Ilya into 80-90 % (189, 95 %), and it still takes the clumsy Ilya from 141 to 64.
A 2.0 radius would put careful Ilya at 83 %, but the clumsy one at 17 %, and it moves Ilya alone.

### What the owner should look at

1. **Floor 1 is too easy for the careful cohort.** With human hands careful clears 98 / 100 %, standing 90 / 90 % and
   first-card 91 / 100 %. Of the playing cohorts only the clumsy one sits in a docs/40 band. Two things to settle
   before any change:
   - which cohort docs/40 section 10.2's bands are read for;
   - whether careful should land in 80-90 % or in 65-80 %.
2. **The Warden echo is the one lever that reaches the careful cohort.** It needs one more round before a yes:
   - the Warden's health read with the echo, to bring the frame-perfect fight from 78 / 67 s back into 45-65 s;
   - the enraged-only variant, which has no lever yet;
   - a look at whether a Warden test that careful and standing fail alike is the difficulty the owner wants.
   Slag no longer touches the careful cohort; it only makes the weak cohorts weaker.
3. **The gate stays at 3:30 on the same trade as before.** A set cuts the wait from 111-117 s to 6-31 s. It costs the
   clumsy cohort over a third of its clears (Ilya's up to half), and now the careful cohort 5-9 runs a hero too.
4. **The Sword's x1.5 is now the smallest share that holds.** A change to the Staff, the Sword or the pilot moves the
   margin, the step-in habit included. Re-read the roles row before any of them ships.
5. **Floor 2's Warden at 6.07 stands.** Share 7 is inside the band too, at a cost to the standing cohort; there is no
   reason in this batch to move it.

## Status 2026-11-11: the Daggers' design options

**The question.** The V0.1 gate's last unmet row is "Three weapons with distinct roles": each weapon leads every other by
more than 10 % on a job of its own. The Staff leads the horde (31 a second to the Sword's 8 and the Daggers' 2) and the
elite rooms' whole waves (21 to the Daggers' 5), and the Sword leads the Warden through `WardenRule.SwordShare` 1.5 (26
to the Staff's 23). The x12 on the Captains was reverted, and the readable x2 shares, with and without the step-in, did
not lead with human hands (the status above). This round prototypes Daggers mechanics a player can see and understand,
with no hidden multiplier above x2 and no one-shot, as research levers of the simulation, and reads each as the gate
does. Nothing ships to the game.

**What was built** (research only, off by default; no scene code, no asset, no gate row changed). `FoundryLevers`'
Daggers design fields, read by the foundry simulation alone:
- **throw** (`DaggersThrowReach`, `DaggersThrowEvery`, `DaggersThrowHeaviest`, `DaggersThrowBlades`,
  `DaggersThrowSparesGateBoss`): with no enemy within the Daggers' 1.8, a ready Daggers strikes the nearest enemy within
  the throw's reach instead - a strike of its own, with its damage and its crit rhythm - at most once every
  `DaggersThrowEvery` seconds (0: at every strike). Variants: the heaviest enemy rather than the nearest; a fan of blades
  on the next-nearest; the gate boss turning thrown blades;
- **lunge** (`DaggersLungeReach`, `DaggersLungeStop`, `DaggersLungeCooldown`, `DaggersLungeHeaviest`): the hero is dashed
  to 1.4 from an enemy within the lunge's reach, then strikes;
- **bleed** (`DaggersBleedShare`, `DaggersBleedSeconds`, `DaggersBleedSparesGateBoss`): each landing of a Daggers strike
  opens a wound that deals the share of it over the seconds, a frame at a time. Wounds stack, and a wound's tick is no
  landing. The gate boss can be left unbled;
- **mark** (`DaggersMarkShare`, `DaggersMarkSeconds`): every weapon's landing on a marked enemy deals the share more. The
  extra counts as the Daggers' (the originating weapon, docs/40 section 8.4's counting rule);
- **execute** (`DaggersExecuteBelow`, `DaggersExecuteDamage`).

`FoundryBatch.DaggersLevers` gains rows 28-58 (rows 0-27 are unchanged). The Daggers report gains, by starting weapon,
the elite fights' length, every weapon's damage on the elites a second and a Captain's time under fire (docs/40 section
8.6's elite TTK). `DAGGERS_COHORTS` screens a lever on the careful cohort alone. `FoundryResult.DaggersThrows` and
`DaggersLunges` count. `FoundrySimulationTests` holds the levers off at the shipped floor, and each doing what it names.
**The default batch is byte-identical** (`FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`, before and after):
foundry-batch.csv, foundry-batch-floor2.csv, foundry-cohorts-floor2.csv, foundry-gate.md (met 8, not met 1) and
foundry-cohorts.md are the same to the byte; foundry-batch.md differs only in its date and its wall-clock seconds.

**How it was measured.** `TheDaggersRoleOverASeedBatch`. First a screen of 30 shapes and combinations (rows 28-57) on
seeds 1-60: the careful cohort alone, floor 1. Then the shipped floor and 12 shapes in full (11 of the screen's and row
58): seeds 1-200 a hero, both floors, every cohort, the Vanguard started with each weapon, frame-perfect and
`FOUNDRY_REALISM=typical` (TestResults/daggers-design-1111/). The jobs are the gate's: P50s of each weapon's own damage
a second, the Vanguard started with that weapon - on the horde (of the floor it held it), on the elite rooms' whole
waves (of those rooms' fights) and on the Warden (of its fight).

**What binds the Daggers.**
1. **The elite fight is a fixed pie.** Whatever the Vanguard starts with, and under every lever but the step-in, the
   careful pilot's elite fights last P50 13-15 s (two rooms). Its weapons together take 35-39 a second off them. The
   pilot's footwork sets the fights' length, not the damage, so the elite job is a weapon's share of the waves' health.
   The Staff takes about 60 % of it in its own runs (21 of 35 a second). A Daggers lead needs either two thirds of it in
   the Daggers' own runs, beside whatever else the Vanguard holds by then (often the Staff), or Daggers strong enough to
   take a slice of the Staff's share in the Staff's runs once picked up. F below leads that second way: 19 a second to
   the Staff's 15.
2. **The reach binds, not the damage.** Bleed, mark, execute and the lunge (each x2 at most, or no multiplier) add 1-2 a
   second, and the Daggers stay at 6-7 on the elites against the Staff's 20. A throw at 3.2 once a second, or a fan of
   three, reaches 9-10.
3. **A Daggers that reaches past the kite at every strike takes the Warden.** On a lone target a single-target weapon
   wastes nothing. Thrown at every strike, the Daggers take 26 a second off the Warden to the Sword's 24 (the screen), and
   with the bleed 48 to 21. Every throw cadence that lifts the elites lifts the Warden faster: the Warden is the only
   target in reach, and it stands for a minute.

So one shape meets the row: the Daggers thrown at every strike, with a bleed, and the Warden's armour turning both. The
throw gives the reach. The bleed gives the elites, since the horde falls before a wound closes and the Captain does
not. The Warden's armour leaves the Warden to the Sword.

**The options in full** (frame-perfect ; human hands. Each job is the Daggers', the Sword's or the Staff's against the
best other weapon on it. The roles row reads the elites as the whole waves, and beside it as the Captains alone.
Clears: careful / standing / first-card / clumsy / auto-farmer. The Warden: the careful cohort's fight, the Vanguard
started with the Sword and Ilya.)

| Option | Elites (the waves): Daggers to the best other | The Captains alone: the same | Warden: Sword to the best other | Horde: Staff to the best other | Roles row: the waves, the Captains | Floor-1 clears, Vanguard | Floor-1 clears, Ilya | Both floors, Vanguard | Warden P50 floor 1, V / I |
|---|---|---|---|---|---|---|---|---|---|
| Shipped: the Daggers as they are | 5 to 21 (-76 %) ; 5 to 21 (-76 %) | 4 to 12 (-67 %) ; 4 to 12 (-67 %) | 26 to 23 (+13 %) ; 27 to 23 (+17 %) | 31 to 8 (+288 %) ; 31 to 8 (+288 %) | not met, not met ; not met, not met | 200 / 193 / 196 / 104 / 9 ; 195 / 179 / 181 / 82 / 9 | 200 / 193 / 200 / 184 / 30 ; 199 / 180 / 199 / 141 / 23 | 200 / 176 / 196 / 96 / 1 ; 193 / 151 / 179 / 66 / 1 | 58 / 51 ; 53 / 48 s |
| A. A thrown blade at 3.2, one a second | 9 to 19 (-53 %) ; 9 to 19 (-53 %) | 6 to 11 (-45 %) ; 6 to 11 (-45 %) | 26 to 23 (+13 %) ; 27 to 23 (+17 %) | 28 to 7 (+300 %) ; 27 to 7 (+286 %) | not met, not met ; not met, not met | 200 / 195 / 196 / 110 / 12 ; 196 / 187 / 185 / 86 / 8 | 200 / 198 / 200 / 182 / 37 ; 199 / 182 / 199 / 148 / 34 | 200 / 183 / 196 / 100 / 1 ; 194 / 161 / 185 / 67 / 4 | 52 / 46 ; 48 / 43 s |
| A, a fan of three blades a second | 10 to 18 (-44 %) ; 10 to 18 (-44 %) | 7 to 11 (-36 %) ; 7 to 11 (-36 %) | 27 to 22 (+23 %) ; 28 to 22 (+27 %) | 27 to 7 (+286 %) ; 26 to 8 (+225 %) | not met, not met ; not met, not met | 200 / 199 / 196 / 125 / 10 ; 200 / 187 / 187 / 84 / 6 | 200 / 198 / 200 / 190 / 39 ; 199 / 193 / 200 / 155 / 33 | 200 / 184 / 196 / 111 / 2 ; 197 / 168 / 186 / 67 / 1 | 52 / 46 ; 48 / 43 s |
| B. A bleed, 100 % over 3 s | 6 to 21 (-71 %) ; 6 to 21 (-71 %) | 5 to 12 (-58 %) ; 5 to 12 (-58 %) | 27 to 23 (+17 %) ; 27 to 23 (+17 %) | 31 to 8 (+288 %) ; 31 to 8 (+288 %) | not met, not met ; not met, not met | 200 / 198 / 195 / 113 / 19 ; 197 / 187 / 183 / 83 / 16 | 200 / 194 / 200 / 188 / 50 ; 199 / 189 / 198 / 147 / 54 | 200 / 186 / 195 / 100 / 4 ; 195 / 172 / 180 / 65 / 5 | 57 / 51 ; 50 / 45 s |
| C. A lunge at the heaviest within 3.0, every 2 s | 7 to 20 (-65 %) ; 7 to 19 (-63 %) | 5 to 11 (-55 %) ; 5 to 11 (-55 %) | 27 to 24 (+12 %) ; 28 to 23 (+22 %) | 29 to 8 (+262 %) ; 28 to 7 (+300 %) | not met, not met ; not met, not met | 193 / 172 / 185 / 104 / 7 ; 135 / 123 / 118 / 80 / 7 | 187 / 172 / 192 / 152 / 25 ; 140 / 118 / 140 / 119 / 19 | 177 / 144 / 155 / 91 / 2 ; 103 / 83 / 83 / 64 / 1 | 53 / 49 ; 48 / 44 s |
| D. An exposing mark, +30 % for 3 s | 7 to 20 (-65 %) ; 7 to 21 (-67 %) | 6 to 12 (-50 %) ; 6 to 12 (-50 %) | 28 to 24 (+17 %) ; 27 to 23 (+17 %) | 31 to 8 (+288 %) ; 31 to 8 (+288 %) | not met, not met ; not met, not met | 200 / 198 / 196 / 108 / 19 ; 198 / 187 / 182 / 85 / 19 | 200 / 200 / 200 / 183 / 52 ; 199 / 189 / 200 / 136 / 51 | 200 / 191 / 196 / 96 / 5 ; 193 / 172 / 179 / 67 / 4 | 55 / 51 ; 46 / 43 s |
| E. An execute, x2 under 30 % | 6 to 21 (-71 %) ; 6 to 20 (-70 %) | 5 to 12 (-58 %) ; 5 to 12 (-58 %) | 27 to 23 (+17 %) ; 27 to 23 (+17 %) | 31 to 8 (+288 %) ; 31 to 8 (+288 %) | not met, not met ; not met, not met | 200 / 197 / 196 / 106 / 13 ; 196 / 182 / 180 / 83 / 14 | 200 / 196 / 200 / 189 / 33 ; 198 / 192 / 199 / 146 / 40 | 200 / 186 / 196 / 97 / 3 ; 193 / 163 / 179 / 66 / 4 | 56 / 51 ; 51 / 46 s |
| **F. The thrown Daggers: a throw at every strike and B; the Warden turns thrown blades and does not bleed** | 19 to 15 (+27 %) ; 19 to 15 (+27 %) | 13 to 8 (+62 %) ; 13 to 8 (+62 %) | 30 to 22 (+36 %) ; 30 to 23 (+30 %) | 21 to 16 (+31 %) ; 21 to 16 (+31 %) | met, met ; met, met | 200 / 200 / 196 / 142 / 15 ; 200 / 194 / 187 / 98 / 16 | 200 / 200 / 200 / 191 / 48 ; 200 / 196 / 200 / 156 / 51 | 200 / 187 / 196 / 125 / 6 ; 197 / 176 / 187 / 78 / 4 | 54 / 49 ; 50 / 45 s |
| F, the Warden bleeding | 19 to 15 (+27 %) ; 19 to 15 (+27 %) | 13 to 8 (+62 %) ; 13 to 8 (+62 %) | 30 to 23 (+30 %) ; 30 to 23 (+30 %) | 21 to 16 (+31 %) ; 20 to 16 (+25 %) | met, met ; met, met | 200 / 200 / 196 / 145 / 29 ; 200 / 196 / 187 / 98 / 28 | 200 / 200 / 200 / 195 / 70 ; 200 / 199 / 200 / 165 / 73 | 200 / 192 / 196 / 129 / 10 ; 199 / 179 / 186 / 74 / 8 | 54 / 49 ; 48 / 44 s |
| F without the bleed | 16 to 16 (+0 %) ; 16 to 16 (+0 %) | 11 to 9 (+22 %) ; 11 to 9 (+22 %) | 29 to 23 (+26 %) ; 30 to 23 (+30 %) | 21 to 15 (+40 %) ; 21 to 15 (+40 %) | not met, met ; not met, met | 200 / 199 / 196 / 132 / 14 ; 199 / 195 / 189 / 91 / 11 | 200 / 200 / 200 / 186 / 35 ; 200 / 195 / 200 / 159 / 35 | 200 / 183 / 196 / 116 / 2 ; 199 / 174 / 187 / 72 / 2 | 55 / 49 ; 50 / 46 s |
| F, the Warden turning nothing | 19 to 15 (+27 %) ; 19 to 15 (+27 %) | 13 to 8 (+62 %) ; 13 to 8 (+62 %) | 21 to 48 (-56 %) ; 22 to 50 (-56 %) | 20 to 14 (+43 %) ; 20 to 14 (+43 %) | not met, not met ; not met, not met | 200 / 200 / 196 / 152 / 32 ; 200 / 199 / 187 / 110 / 33 | 200 / 200 / 200 / 197 / 78 ; 200 / 200 / 200 / 174 / 76 | 200 / 195 / 196 / 138 / 10 ; 200 / 192 / 187 / 85 / 9 | 36 / 34 ; 34 / 33 s |
| G. The step-in on the Captains, with B | 34 to 45 (-24 %) ; 25 to 39 (-36 %) | 29 to 24 (+21 %) ; 20 to 21 (-5 %) | 27 to 24 (+12 %) ; 27 to 24 (+12 %) | 31 to 8 (+288 %) ; 31 to 8 (+288 %) | not met, met ; not met, not met | 200 / 198 / 195 / 113 / 19 ; 198 / 187 / 184 / 83 / 16 | 200 / 194 / 200 / 188 / 50 ; 200 / 189 / 199 / 147 / 54 | 200 / 186 / 195 / 100 / 4 ; 194 / 172 / 179 / 65 / 5 | 56 / 50 ; 49 / 45 s |
| G. The step-in on the Captains, with D | 35 to 39 (-10 %) ; 27 to 36 (-25 %) | 29 to 20 (+45 %) ; 22 to 18 (+22 %) | 27 to 24 (+12 %) ; 27 to 23 (+17 %) | 31 to 8 (+288 %) ; 30 to 8 (+275 %) | not met, met ; not met, met | 200 / 198 / 196 / 108 / 19 ; 197 / 187 / 182 / 85 / 19 | 200 / 200 / 200 / 183 / 52 ; 200 / 189 / 199 / 136 / 51 | 200 / 191 / 196 / 96 / 5 ; 195 / 172 / 181 / 67 / 4 | 56 / 50 ; 46 / 43 s |

The screen's other shapes (seeds 1-60, careful, floor 1; the elites, the Daggers to the best other, frame-perfect ;
human hands):

| Shape | Elites (the waves) | Note |
|---|---|---|
| A throw every 0.75 s | 9 to 19 ; 10 to 20 | |
| A throw a second at the heaviest enemy | 9 to 20 ; 9 to 19 | with the bleed 11 to 19 ; 11 to 19 |
| A throw at 2.6 at every strike | 11 to 18 ; 11 to 19 | with the bleed and the Warden turning throws 14 to 18 ; 13 to 17 |
| A fan of three every 0.75 s | 12 to 18 ; 12 to 18 | a fan of three a second with the bleed 12 to 19 ; 12 to 18 |
| The lunge at the nearest | 7 to 19 ; 6 to 19 | at the heaviest every second with the bleed 10 to 18 ; 8 to 18 |
| A throw a second with the mark | 11 to 18 ; 12 to 18 | the Sword's Warden lead gone with human hands (24 to 26) |
| A throw every 0.75 s with the bleed, the Warden turning throws | 12 to 18 ; 11 to 18 | |
| A throw at every strike with half the bleed, the Warden turning throws | 18 to 15 ; 17 to 16 | +20 % ; +6 %: not met with human hands |

On seeds 201-400 (careful, floor 1) the shape with the Warden bleeding (row 54; F differs from it only on the Warden)
reads the same: the elites 19 to 14 ; 18 to 14, the Warden 29 to 23 ; 28 to 22 (the Sword's), the horde 20 to 15 ; 20
to 15 (the Staff's). Met with either hands.

**Each option: what the player sees, what it costs, what it takes to build.**

- **A. A thrown blade**: out of reach, one blade a second at up to 3.2. The player sees a dagger fly at whatever stands
  just past the kite. It is readable and modest, but the Daggers stay third on every job. Its cost: the Warden's fight
  shortens (52 / 46 s ; 48 / 43 s, Ilya under 45 s with human hands), since a Vanguard that picks the Daggers up throws
  at the Warden. Build: a projectile (V0.1 has none: every hit is instant, and docs/42 section 4 asks for pooling), a
  throw rule body the scene and the simulation share, a dagger sprite with a trail, and card text. A fan of three
  blades is the same build with three sprites, and it does no better.
- **B. A bleed**: each hit bleeds as much again over 3 s. The player sees red drops and ticking numbers on what the
  Daggers cut. It adds most where a target lives (the Warden, the Captain) and nothing on the horde, but on its own the
  Daggers seldom cut anything. Build: a `BleedStatus` and a `BleedRule` beside `BurnStatus` and `BurnRule` (Ember
  Memory's pattern), a drip effect, and the ticks in the damage numbers. The cheapest of the five.
- **C. A lunge**: out of reach, a dash to the heaviest enemy within 3.0, every 2 s. The player sees the hero dart
  forward. It moves the hero without the stick, into the Captain's reach, on a one-thumb mobile control, and it costs
  the most: the careful cohort's floor-1 clears with human hands fall from 195 / 199 to 135 / 140. Build: a dash on
  `HeroMotion` that the steering yields to, its animation, and a cooldown readout.
- **D. An exposing mark**: +30 % from every weapon for 3 s. The player sees a sigil on what the Daggers cut. It is a team
  weapon: the extra is the Daggers' only by attribution, and the elite fights do not shorten (the fixed pie). With human
  hands the careful Warden's fight drops to 46 / 43 s. Build: a mark rule in the damage path every weapon's landing goes
  through (the pattern of `WardenRule.DamageOn`), and a sigil decal.
- **E. An execute**: x2 under 30 % health. The player sees a skull flash on the wounded. It has the smallest effect of
  all. Build: one branch in the Daggers' damage, and a flash.
- **F. The thrown Daggers**: A at every strike and B, and the Warden's armour turning both. The player sees the Daggers
  as a short-range thrown weapon - a blade at every strike, at the nearest enemy within 3.2 when nothing is within 1.8 -
  whose cuts bleed, and blades sparking off the Warden's armour, which neither takes a throw nor bleeds. The Daggers'
  strikes within 1.8 land on the Warden as ever. It changes what the Daggers are, not a number. Its costs:
  - the Daggers become a strong pickup: their horde rate goes from 2 to 16 a second, and the Staff's horde lead narrows
    from +288 % to +31 % (still its role);
  - the weaker cohorts clear more (decision 3);
  - the careful Warden's fight shortens, from 58 / 51 to 54 / 49 s frame-perfect and from 53 / 48 to 50 / 45 s with
    human hands. No Daggers damage that F adds lands on the Warden, but the Sword's own rate on it rises from 26 to
    30 a second - likely the horde round the Warden falling sooner and leaving it the Sword's nearest target.

  With the Warden bleeding (its armour turning thrown blades alone), F costs more: the standing pilot, who cuts the
  Warden in melee, fights it in 39 / 37 s rather than 43 / 40, the auto-farmers clear 28 / 73 rather than 16 / 51 with
  human hands, and the careful Warden's fight drops to 48 / 44 s. Without the bleed, F leads only the Captains alone (11
  to 9, +22 %, with either hands), not the waves (16 to 16).

  Build: A's projectile and B's status, and the gate boss's armour: a rule body that reads `EncounterController.GateRoom`
  as `WardenRule` does, and a spark-and-clang effect, so the exception is seen. Also the Daggers' card and forge text
  ("thrown", "bleeds", "the Warden's armour turns thrown blades and wounds"), and a parity pin for the throw and the
  bleed. The throw and the bleed would be fields of their own: Weapon_Daggers.asset's damage, interval, reach and crit
  rhythm do not move, and the hall arena and the Descent read them.
- **G. The step-in, with B or D.** Only a pilot habit brings the Daggers' melee to the elites. With the step-in on the
  Captains, the Daggers lead the Captains alone with the mark (29 to 20 ; 22 to 18), and with the bleed only
  frame-perfect (29 to 24 ; 20 to 21). Neither leads the waves (the mark 35 to 39 ; 27 to 36, the bleed 34 to 45 ; 25
  to 39). It reads the elites as the Captains (docs/40 section 8.6's elite TTK is the elite's own), it turns on a habit
  the lead has held back, and a stepped-in Captain falls within 1.1-1.5 s under fire. With the mark, the careful
  Warden's fight with human hands drops to 46 / 43 s.

**Recommendation.** F, if the owner answers yes to the three questions below. Of the 31 shapes two meet the row without
a hidden share, F and F with the Warden bleeding, and F is the cheaper. It meets the row every way it was read: on the
gate's own reading (the whole waves) and on the Captains', with either hands. The bleed is the hit's own worth again
over 3 s, and it shows as it ticks. The Warden's armour shows at every blade it turns. No Captain is one-shot: with the Daggers started it
stands 3.9 ; 4.4 s under fire (P50), against 4.3 ; 4.4 s now. The careful Warden's fight stays inside step 4's 45-65 s,
on its edge with human hands (Ilya 45 s). Build it behind a foundry-only switch and play it on the device before the row
is called met: the simulation's blades land at once, and the scene's would fly.

If the owner wants the Daggers left a melee weapon, or no boss exception, no shape gives them a job. Then V0.1 ships with
two roles, the row stays not met as the owner's waiver, and the Daggers' job waits for their character. docs/40 section
4.1's Riftblade has Phase Dash and a marked execute: the closing-in a player does by hand, which the step-in stands for.
The bleed is worth keeping either way, as docs/40 section 5.2's "+15 % Bleed" Daggers line.

**What the owner must decide.**
1. **Do the Daggers become a thrown weapon?** That means a blade at every strike, at the nearest enemy within 3.2 when
   nothing is within 1.8, and a bleed of the hit's worth again over 3 s. The asset's numbers stay, but what the Daggers
   are changes, and they become a strong pickup: their horde rate goes from 2 to 16 a second, and the Staff's horde
   lead narrows from +288 % to +31 % (still its role).
2. **May the Warden's armour turn thrown blades and wounds?** Without it the thrown Daggers take the Warden (48 to 21),
   the Sword has no job, and the Warden's fight falls to 33-36 s. With it, the Sword keeps the Warden by 30-36 %. The
   exception has to be seen: a spark and a clang at every turned blade, and no drops on the Warden.
3. **May the floor get easier for the weaker cohorts?** With F, every cohort that picks the Daggers up clears more of
   floor 1, and no cohort loses a clear. With human hands, Vanguard / Ilya: standing 179 -> 194 / 180 -> 196, clumsy
   82 -> 98 / 141 -> 156, auto-farmer 9 -> 16 / 23 -> 51. Both floors, clumsy Vanguard 66 -> 78. The careful Warden's
   fight with human hands drops from 53 / 48 to 50 / 45 s. This round measured no counterweight (the Warden's health
   share, the Daggers' forge price, or their offer weight).
4. **If not F, the waiver.** Whether V0.1 may ship with the Staff the horde's weapon and the Sword the Warden's, and the
   roles row read not met with this section as its note.

## Status 2026-11-11: the Warden echo, second round

**Why.** Decision 4 of the re-read found floor 1 too easy for the careful cohort. With human hands it clears 98 / 100 %,
and only the Warden echo at the slam's own 1.1 s telegraph brings it back. As measured the echo had three problems:
- frame-perfect, it stretched the careful Warden fight to 78 / 67 s, out of step 4's 45-65 s;
- the standing cohort paid as much as the careful one;
- the enraged-only echo the 2026-11-04 research recommended had no lever.

This round reads two candidates, both research levers, off by default:
1. the echo after every blow, with floor 1's Warden on a lower health share, so that the frame-perfect fight comes
   back into 45-65 s;
2. the echo after the enraged Warden's blows only.

Nothing changes in the game.

**Settings.** Main 4476f661 and this branch's levers: `FoundryLevers.WardenEchoEnragedOnly` and two parts of
`FoundryDifficultyResearch`:
- `echo-share` sweeps `ECHO_HEALTH` x `ECHO_SHARES`. The health is `FoundryLevers.WardenHealthShare`, the gate boss's
  share on floor 1 (shipped `WardenRule.GateHealthShare`, 8.5).
- `echo-enraged` sweeps `ECHO_ENRAGED_HEALTH` x `ECHO_ENRAGED_SHARES`.

Seeds 1-200 a hero, `FOUNDRY_PARALLEL=4`, floor 1, every cohort, frame-perfect and `FOUNDRY_REALISM=typical` ("human
hands"). The outputs are in TestResults/echo-1111/: reference, share, share2, enraged and enraged2-4 for floor 1,
f2-* for floor 2. The shipped floor and the echo at 8.5 reproduce the re-read's decision 4 to the digit: clears, Warden
fights and floors. The default batch (`FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1`) is byte-identical to main's but for the
wall-time column.

Every cell is "Vanguard / Ilya", clears of 200; "fp ; hh" is frame-perfect ; human hands. The 80-90 % band is 160-180
clears, 65-80 % is 130-160. Read differences under about 10 as noise (docs/cloud/2026-11-04-difficulty.md).

| Candidate | Verdict | The numbers that decide it |
|---|---|---|
| 1. The echo after every blow, floor 1's Warden at a lower share | **lands careful in either band, inside 45-65 s, at share 6** | 20 % echo: careful 166 / 170 with human hands (83 / 85 %). 30 %: 145 / 148 (73 / 74 %). Frame-perfect fight 58 / 51 s (P90 74 / 66), the shipped length; frame-perfect careful 199 / 200 |
| 2. The echo after the enraged Warden's blows only | **lands careful in 80-90 %, inside 45-65 s, at 30 % and share 7** | careful 166 / 175 with human hands (83 / 88 %); frame-perfect fight 60 / 52 s (P90 76 / 66); the echo half again the slam's 20 %. For 65-80 % it needs 40 %, twice the slam: 148 / 161, Ilya on the edge |
| Floor 2, where the echo is the Warden's | **it costs careful humans a second time** | at floor 2's own 6.07 its fight runs 77 / 68 s (every blow) and 69 / 57 s (enraged-only); at 4.3 and 5 it holds 57 / 49 and 56 / 51 s, but careful with human hands clears both floors 128 / 139 and 132 / 154 (shipped 193 / 195) |
| Skill separation | **neither echo separates skill** | Shipped, careful clears 16 / 19 more than standing with human hands. In the band: every blow +6 / +1 (20 %) and +1 / -8 (30 %), enraged-only +6 / +15. The clumsy cohort does not move (79-86 / 137-142, shipped 82 / 141) |

### 1. The echo after every blow, with floor 1's Warden at a lower share

`DIFFICULTY_PARTS=echo-share`. After each blow a second mark goes down where the hero stands, with the slam's radius, and
lands 1.1 s later. The shipped floor and share 8.5 come from TestResults/echo-1111/reference.

| Echo, Warden share | careful fp ; hh | standing fp ; hh | first-card fp ; hh | clumsy fp ; hh | auto-farmer fp ; hh |
|---|---|---|---|---|---|
| none (shipped), 8.5 | 200 / 200 ; 195 / 199 | 193 / 193 ; 179 / 180 | 196 / 200 ; 181 / 199 | 104 / 184 ; 82 / 141 | 9 / 30 ; 9 / 23 |
| 10 %, 8.5 | 200 / 200 ; 166 / 178 | 195 / 193 ; 161 / 159 | 196 / 200 ; 161 / 186 | 99 / 180 ; 71 / 128 | 3 / 8 ; 1 / 7 |
| 10 %, 7 | 200 / 200 ; 179 / 186 | 197 / 196 ; 172 / 174 | 195 / 200 ; 178 / 192 | 105 / 183 ; 78 / 138 | 7 / 18 ; 6 / 13 |
| 10 %, 6.5 | 200 / 200 ; 179 / 182 | 197 / 196 ; 175 / 175 | 195 / 200 ; 178 / 191 | 108 / 183 ; 79 / 142 | 7 / 23 ; 7 / 18 |
| 10 %, 6 | 199 / 200 ; 184 / 186 | 196 / 197 ; 178 / 179 | 196 / 200 ; 178 / 193 | 110 / 186 ; 83 / 148 | 9 / 28 ; 10 / 22 |
| 20 %, 8.5 | 200 / 200 ; 136 / 149 | 195 / 193 ; 147 / 148 | 196 / 200 ; 142 / 172 | 99 / 180 ; 69 / 124 | 0 / 2 ; 0 / 1 |
| 20 %, 7 | 200 / 200 ; 158 / 162 | 197 / 196 ; 157 / 158 | 195 / 200 ; 161 / 181 | 105 / 183 ; 75 / 131 | 2 / 5 ; 1 / 6 |
| 20 %, 6.5 | 200 / 200 ; 156 / 161 | 197 / 196 ; 161 / 164 | 195 / 200 ; 166 / 176 | 108 / 183 ; 78 / 137 | 3 / 6 ; 2 / 6 |
| 20 %, 6 | 199 / 200 ; 166 / 170 | 196 / 197 ; 160 / 169 | 196 / 200 ; 166 / 187 | 110 / 186 ; 81 / 141 | 3 / 11 ; 4 / 9 |
| 30 %, 6 | 199 / 200 ; 145 / 148 | 196 / 197 ; 144 / 156 | 196 / 200 ; 143 / 176 | 110 / 186 ; 79 / 137 | 1 / 4 ; 1 / 3 |

The careful cohort's Warden fight, from walking out of the gate to its fall: P50 (P90) of the felled fights. Its floor:
P50 of every run. Its echo hits a Warden fight with human hands (frame-perfect, 0.00-0.01), and its deaths at the
Warden with human hands:

| Echo, Warden share | Warden fight fp | Warden fight hh | Floor fp ; hh | Echo hits a fight, hh | Deaths at the Warden, hh |
|---|---|---|---|---|---|
| none (shipped), 8.5 | 58 (70) / 51 (62) | 53 (65) / 48 (57) | 292 / 285 ; 286 / 280 | 0.00 / 0.00 | 4 / 1 |
| 10 %, 8.5 | 78 (94) / 67 (82) | 66 (80) / 59 (72) | 311 / 301 ; 298 / 290 | 1.91 / 1.74 | 33 / 22 |
| 10 %, 7 | 67 (82) / 56 (73) | 58 (71) / 53 (63) | 300 / 293 ; 290 / 285 | 1.61 / 1.65 | 20 / 14 |
| 10 %, 6.5 | 63 (78) / 53 (68) | 55 (70) / 49 (59) | 298 / 291 ; 287 / 281 | 1.61 / 1.55 | 20 / 18 |
| 10 %, 6 | 58 (74) / 51 (66) | 53 (66) / 48 (57) | 294 / 288 ; 285 / 280 | 1.48 / 1.47 | 15 / 14 |
| 20 %, 8.5 | 78 (94) / 67 (82) | 66 (80) / 59 (72) | 311 / 301 ; 294 / 288 | 1.81 / 1.70 | 63 / 51 |
| 20 %, 7 | 67 (82) / 56 (73) | 58 (71) / 52 (63) | 300 / 293 ; 287 / 283 | 1.57 / 1.60 | 41 / 38 |
| 20 %, 6.5 | 63 (78) / 53 (68) | 55 (70) / 49 (59) | 298 / 291 ; 285 / 280 | 1.57 / 1.52 | 43 / 39 |
| 20 %, 6 | 58 (74) / 51 (66) | 53 (67) / 46 (56) | 294 / 288 ; 283 / 278 | 1.47 / 1.45 | 33 / 30 |
| 30 %, 6 | 58 (74) / 51 (66) | 53 (67) / 47 (56) | 294 / 288 ; 281 / 276 | 1.42 / 1.38 | 54 / 52 |

- **The fight comes back with the share.** At 8.5 the echo stretches the frame-perfect fight from 58 / 51 s to
  78 / 67 s. The echo lands inside the 1.5 s the hammer stays in the floor, the window step 4 gave the hero to strike
  back, so escaping it spends that window. At share 7 the fight is 67 / 56 s, the Vanguard still out; at 6.5 it is
  63 / 53 s; at 6 it is 58 / 51 s, the shipped fight's length. Its P90 stays a few seconds over the shipped one:
  74 / 66 s at 6, against 70 / 62 s.
- **The echo still costs human hands at the lower share.** At share 6 the 20 % echo leaves careful at 166 / 170 and the
  30 % at 145 / 148 (at 8.5 the 20 % gave 136 / 149): a shorter fight sees fewer echoes. The 10 % echo hardly bites
  below 8.5 (179-184 / 182-186).
- **The careful cohort's losses move to the Warden.** With human hands it dies there 33 / 30 times at share 6 and 20 %,
  against 4 / 1 shipped; elsewhere on the floor it loses what it lost before.
- **The frame-perfect pilot never notices it**: careful 199-200 at every share, never struck by the echo.

### 2. The echo after the enraged Warden's blows only

`DIFFICULTY_PARTS=echo-enraged` (`FoundryLevers.WardenEchoEnragedOnly`). The echo follows only the blows the Warden
lands once it is enraged, under half its health, where the marks come every 4 s instead of 6 s. The calm half of the
fight is the shipped one.

| Enraged-only echo, Warden share | careful fp ; hh | standing fp ; hh | first-card fp ; hh | clumsy fp ; hh | auto-farmer fp ; hh |
|---|---|---|---|---|---|
| none (shipped), 8.5 | 200 / 200 ; 195 / 199 | 193 / 193 ; 179 / 180 | 196 / 200 ; 181 / 199 | 104 / 184 ; 82 / 141 | 9 / 30 ; 9 / 23 |
| 10 %, 8.5 | 200 / 198 ; 177 / 186 | 195 / 189 ; 168 / 174 | 196 / 200 ; 174 / 192 | 102 / 179 ; 87 / 138 | 6 / 19 ; 5 / 15 |
| 20 %, 8.5 | 200 / 198 ; 160 / 177 | 195 / 189 ; 155 / 163 | 196 / 200 ; 167 / 189 | 102 / 179 ; 82 / 138 | 5 / 9 ; 1 / 7 |
| 30 %, 8.5 | 200 / 198 ; 143 / 168 | 195 / 189 ; 142 / 154 | 196 / 200 ; 156 / 180 | 102 / 179 ; 81 / 134 | 1 / 3 ; 1 / 6 |
| 10 %, 7.5 | 200 / 200 ; 183 / 196 | 197 / 196 ; 178 / 179 | 196 / 200 ; 181 / 195 | 105 / 184 ; 89 / 143 | 9 / 26 ; 9 / 24 |
| 20 %, 7.5 | 200 / 200 ; 170 / 184 | 197 / 196 ; 170 / 168 | 196 / 200 ; 169 / 189 | 105 / 184 ; 86 / 140 | 6 / 16 ; 6 / 11 |
| 25 %, 7.5 | 200 / 200 ; 159 / 176 | 197 / 196 ; 168 / 164 | 196 / 200 ; 160 / 188 | 105 / 184 ; 85 / 138 | 5 / 13 ; 3 / 9 |
| 30 %, 7.5 | 200 / 200 ; 152 / 165 | 197 / 196 ; 157 / 158 | 196 / 200 ; 157 / 185 | 105 / 184 ; 85 / 137 | 4 / 8 ; 3 / 8 |
| 25 %, 7 | 200 / 200 ; 173 / 182 | 197 / 199 ; 168 / 165 | 196 / 200 ; 170 / 194 | 109 / 188 ; 80 / 139 | 6 / 18 ; 5 / 11 |
| 30 %, 7 | 200 / 200 ; 166 / 175 | 197 / 199 ; 160 / 160 | 196 / 200 ; 163 / 187 | 109 / 188 ; 79 / 138 | 6 / 15 ; 5 / 10 |
| 40 %, 7 | 200 / 200 ; 148 / 161 | 197 / 199 ; 152 / 150 | 196 / 200 ; 149 / 183 | 109 / 188 ; 78 / 133 | 3 / 10 ; 1 / 7 |

| Enraged-only echo, Warden share | Warden fight fp | Warden fight hh | Floor fp ; hh | Echo hits a fight, hh | Deaths at the Warden, hh |
|---|---|---|---|---|---|
| none (shipped), 8.5 | 58 (70) / 51 (62) | 53 (65) / 48 (57) | 292 / 285 ; 286 / 280 | 0.00 / 0.00 | 4 / 1 |
| 10 %, 8.5 | 69 (85) / 59 (75) | 62 (75) / 54 (65) | 304 / 296 ; 292 / 286 | 1.17 / 0.96 | 22 / 14 |
| 20 %, 8.5 | 69 (85) / 59 (75) | 62 (75) / 54 (65) | 304 / 296 ; 290 / 285 | 1.12 / 0.92 | 39 / 23 |
| 30 %, 8.5 | 69 (85) / 59 (75) | 62 (75) / 54 (65) | 304 / 296 ; 289 / 285 | 1.05 / 0.90 | 56 / 32 |
| 10 %, 7.5 | 64 (79) / 54 (69) | 56 (69) / 49 (60) | 299 / 290 ; 289 / 283 | 1.09 / 0.88 | 16 / 4 |
| 20 %, 7.5 | 64 (79) / 54 (69) | 56 (69) / 49 (61) | 299 / 290 ; 288 / 282 | 1.08 / 0.86 | 29 / 16 |
| 25 %, 7.5 | 64 (79) / 54 (69) | 56 (69) / 49 (61) | 299 / 290 ; 286 / 281 | 1.06 / 0.85 | 40 / 24 |
| 30 %, 7.5 | 64 (79) / 54 (69) | 56 (69) / 50 (61) | 299 / 290 ; 285 / 280 | 1.04 / 0.84 | 47 / 35 |
| 25 %, 7 | 60 (76) / 52 (66) | 54 (67) / 47 (57) | 296 / 288 ; 285 / 279 | 1.04 / 0.86 | 26 / 18 |
| 30 %, 7 | 60 (76) / 52 (66) | 54 (67) / 47 (57) | 296 / 288 ; 285 / 279 | 1.03 / 0.85 | 33 / 25 |
| 40 %, 7 | 60 (76) / 52 (66) | 54 (67) / 47 (58) | 296 / 288 ; 283 / 278 | 1.01 / 0.82 | 51 / 39 |

- **It stretches the fight about half as much.** At 8.5 the frame-perfect fight runs 69 / 59 s (after every blow:
  78 / 67 s), the Vanguard still out of 45-65 s. At 7.5 it runs 64 / 54 s and at 7 60 / 52 s, inside.
- **It needs a larger echo to bite**, since the calm half has none. At share 7 the 25 % echo leaves careful at
  173 / 182 and the 30 % at 166 / 175; at 7.5 the 20 % leaves it at 170 / 184. So the echo hits as hard as the slam
  (20 %) or half again as hard.
- **It keeps the heroes apart.** Ilya clears 9-17 more than the Vanguard at every enraged-only setting in the fight
  band. After every blow the two are within 2-7 below share 8.5 (166 / 170 at share 6).

### 3. Neither echo separates skill

How far the careful cohort clears above each other cohort with human hands:

| Configuration | careful - standing | careful - first-card | careful - clumsy |
|---|---|---|---|
| shipped | +16 / +19 | +14 / +0 | +113 / +58 |
| every blow, 10 %, share 8.5 | +5 / +19 | +5 / -8 | +95 / +50 |
| every blow, 20 %, share 8.5 | -11 / +1 | -6 / -23 | +67 / +25 |
| every blow, 20 %, share 7 | +1 / +4 | -3 / -19 | +83 / +31 |
| every blow, 10 %, share 6.5 | +4 / +7 | +1 / -9 | +100 / +40 |
| every blow, 20 %, share 6.5 | -5 / -3 | -10 / -15 | +78 / +24 |
| every blow, 10 %, share 6 | +6 / +7 | +6 / -7 | +101 / +38 |
| every blow, 20 %, share 6 | +6 / +1 | +0 / -17 | +85 / +29 |
| every blow, 30 %, share 6 | +1 / -8 | +2 / -28 | +66 / +11 |
| enraged-only, 10 %, share 8.5 | +9 / +12 | +3 / -6 | +90 / +48 |
| enraged-only, 20 %, share 8.5 | +5 / +14 | -7 / -12 | +78 / +39 |
| enraged-only, 30 %, share 8.5 | +1 / +14 | -13 / -12 | +62 / +34 |
| enraged-only, 10 %, share 7.5 | +5 / +17 | +2 / +1 | +94 / +53 |
| enraged-only, 20 %, share 7.5 | +0 / +16 | +1 / -5 | +84 / +44 |
| enraged-only, 25 %, share 7.5 | -9 / +12 | -1 / -12 | +74 / +38 |
| enraged-only, 30 %, share 7.5 | -5 / +7 | -5 / -20 | +67 / +28 |
| enraged-only, 25 %, share 7 | +5 / +17 | +3 / -12 | +93 / +43 |
| enraged-only, 30 %, share 7 | +6 / +15 | +3 / -12 | +87 / +37 |
| enraged-only, 40 %, share 7 | -4 / +11 | -1 / -22 | +70 / +28 |

Echo hits a Warden fight with human hands, and that fight's P50, by cohort:

| | careful | standing | first-card | clumsy |
|---|---|---|---|---|
| every blow, 20 %, share 8.5: echo hits a fight | 1.81 / 1.70 | 1.37 / 1.21 | 1.24 / 1.02 | 0.42 / 0.42 |
| every blow, 20 %, share 8.5: Warden fight P50 | 66 / 59 s | 56 / 50 s | 63 / 58 s | 48 / 48 s |
| every blow, 20 %, share 6: echo hits a fight | 1.47 / 1.45 | 1.21 / 1.00 | 1.02 / 0.84 | 0.46 / 0.41 |
| every blow, 20 %, share 6: Warden fight P50 | 53 / 46 s | 45 / 41 s | 52 / 45 s | 39 / 40 s |
| enraged-only, 30 %, share 7: echo hits a fight | 1.03 / 0.85 | 0.70 / 0.65 | 0.68 / 0.55 | 0.18 / 0.19 |
| enraged-only, 30 %, share 7: Warden fight P50 | 54 / 47 s | 45 / 41 s | 50 / 45 s | 43 / 44 s |
| enraged-only, 20 %, share 7.5: echo hits a fight | 1.08 / 0.86 | 0.73 / 0.65 | 0.81 / 0.62 | 0.14 / 0.21 |
| enraged-only, 20 %, share 7.5: Warden fight P50 | 56 / 49 s | 46 / 43 s | 54 / 47 s | 44 / 45 s |

- **Standing pays about as much as careful, with either echo.** Shipped, careful clears 16 / 19 more than standing with
  human hands. With the echo after every blow the gap is -11 to +6 for the Vanguard and -8 to +19 for Ilya;
  enraged-only it is -9 to +9 and +7 to +17. Enraged-only keeps Ilya's shipped gap, not the Vanguard's.
- **The echo does not reach the clumsy cohort.** Its clears move by 4 or less wherever careful lands in a band
  (shipped 82 / 141). The clumsy Vanguard loses most of its runs in the halls before the Warden. At the Warden the clumsy
  cohort takes 0.1-0.5 echo hits a fight against careful's 1.0-1.5, in a shorter fight. So the echo narrows the gap from
  careful to clumsy: from +113 / +58 shipped to +66-87 / +11-44 in the bands.
- **Careful pays most, and its fight is the longest.** With human hands it fights the Warden 53-56 / 46-49 s, standing
  45-46 / 41-43 s, clumsy 39-44 / 40-45 s. So it sees the most echoes, and with a 0.25 s reaction and the typical
  lapses each one is a chance to be late.

The echo makes the Warden harder for every cohort that fights it at length. It is not a test that only the careful
player passes. The 2026-11-04 research said so of the echo after every blow; the enraged-only echo is no different
for the Vanguard and only a little better for Ilya.

### 4. The bands

| Careful with human hands | Candidates inside the fight band (careful clears, hh) | Frame-perfect careful fight P50 (P90) |
|---|---|---|
| 80-90 % (160-180) | **after every blow, 20 %, share 6: 166 / 170** | 58 / 51 s (74 / 66) |
| | **enraged-only, 30 %, share 7: 166 / 175** | 60 / 52 s (76 / 66) |
| | after every blow, 10 %, share 6.5: 179 / 182 (Ilya 91 %, on the edge) | 63 / 53 s (78 / 68) |
| | enraged-only, share 7.5, 20 % and 25 %: 170 / 184 and 159 / 176 (one hero out each time) | 64 / 54 s (79 / 69) |
| 65-80 % (130-160) | **after every blow, 30 %, share 6: 145 / 148** | 58 / 51 s (74 / 66) |
| | enraged-only, 40 %, share 7: 148 / 161 (Ilya 80.5 %, on the edge; the echo twice the slam) | 60 / 52 s (76 / 66) |
| | after every blow, 20 %, share 6.5: 156 / 161 (Ilya 80.5 %, on the edge) | 63 / 53 s (78 / 68) |

Out of the fight band: enraged-only 20 % at 8.5 gives 160 / 177 (fight 69 / 59 s), and the echo after every blow at
8.5 gives 166 / 178 at 10 % and 136 / 149 at 20 % (fight 78 / 67 s).

What the 80-90 % readings do to the other cohorts with human hands:

| | standing | first-card | clumsy | auto-farmer | careful floor fp ; hh |
|---|---|---|---|---|---|
| shipped | 179 / 180 | 181 / 199 | 82 / 141 | 9 / 23 | 292 / 285 ; 286 / 280 s |
| after every blow, 20 %, share 6 | 160 / 169 | 166 / 187 | 81 / 141 | 4 / 9 | 294 / 288 ; 283 / 278 s |
| enraged-only, 30 %, share 7 | 160 / 160 | 163 / 187 | 79 / 138 | 5 / 10 | 296 / 288 ; 285 / 279 s |
| after every blow, 30 %, share 6 (65-80 %) | 144 / 156 | 143 / 176 | 79 / 137 | 1 / 3 | 294 / 288 ; 281 / 276 s |

### 5. Floor 2: the echo reaches the next Warden too

Built as the Warden's, the echo reaches floor 2's Warden as well (Floor_QuicksilverVaults: share 6.07, Cursed Gold's
+20 % enemy damage). `ECHO_FLOORS=2` plays on into floor 2 after a floor-1 clear, and `ECHO_LATER_HEALTH` sets floor 2's
share (`FoundryLevers.LaterWardenHealthShare`). The two 80-90 % readings, careful and standing at floor 2's own share,
careful alone at a lowered one (TestResults/echo-1111/f2-*). The shipped rows reproduce the re-read's decision 3 to the
digit, and floor 1's rows are the ones above.

| Floor 1 ; floor 2's share | Both floors cleared, careful fp ; hh | Both floors cleared, standing fp ; hh | Floor-2 Warden fight, careful fp | Floor-2 Warden fight, careful hh | Deaths at floor 2's Warden, hh: careful ; standing |
|---|---|---|---|---|---|
| shipped ; 6.07 | 200 / 200 ; 193 / 195 | 176 / 176 ; 151 / 155 | 58 (69) / 51 (63) | 53 (68) / 49 (60) | 1 / 4 ; 26 / 25 |
| every blow, 20 %, share 6 ; 6.07 | 198 / 200 ; 106 / 116 | 178 / 180 ; 104 / 119 | 77 (94) / 68 (84) | 68 (82) / 60 (75) | 58 / 53 ; 52 / 46 |
| every blow, 20 %, share 6 ; 4.3 | 199 / 200 ; 128 / 139 | - | 57 (74) / 49 (67) | 55 (67) / 49 (60) | 36 / 30 ; - |
| enraged-only, 30 %, share 7 ; 6.07 | 200 / 200 ; 125 / 144 | 174 / 180 ; 115 / 108 | 69 (87) / 57 (77) | 64 (80) / 58 (70) | 39 / 28 ; 40 / 49 |
| enraged-only, 30 %, share 7 ; 5 | 200 / 200 ; 132 / 154 | - | 56 (75) / 51 (67) | 56 (69) / 51 (62) | 32 / 18 ; - |

- **At floor 2's own share the echo stretches its fight out of 45-65 s**, as on floor 1: 77 / 68 s after every blow,
  69 / 57 s enraged-only. Floor 1's ratio holds it: 4.3 (every blow) and 5 (enraged-only) give 57 / 49 and 56 / 51 s.
- **Even there it costs careful humans a second time.** Of the runs that clear floor 1 (166 / 170 and 166 / 175),
  38 / 31 and 34 / 21 die at floor 2's Warden; shipped, 2 / 4 do. Over the two floors careful with human hands clears
  64 / 70 % (every blow) or 66 / 77 % (enraged-only), against 97 / 98 % shipped.
- So the floor-1 setting that lands careful in 80-90 % puts the two floors together at 64-77 % if every Warden has the
  echo. A band read over the run would want a smaller echo, or none, on floor 2; a band read floor by floor would want
  floor 2's echo tuned on its own.

**Not read:**
- **The Sword's role.** The Sword's lead on the Warden (x1.5, the smallest share that holds) is read as damage a second
  of the fight. The echo changes how long the hero stands striking, so the roles row wants a re-read with it.
- **The slow human.** Only the typical hand was read; the 2026-11-04 research found the echo the candidate most
  sensitive to reaction (at 0.8 s it was a cliff).

### What it would take to build in the scene

Either echo needs the same five pieces; the enraged-only one adds one condition.

| Piece | Where | What |
|---|---|---|
| The rule | WardenRule | The echo's telegraph and share as constants, and one body, never inlined, that says whether a blow has an echo (enraged or not), read by the scene and the simulation alike. The simulation's echo moves off the hazard levers onto it. A new health share is one constant (`GateHealthShare`), its comment re-measured. |
| The clock | WardenSlamService | A second mark after a blow: where the hero stands at the blow, landing a telegraph later, paused with the slam clock (a choice open, the reveal held), gone with the Warden, its damage with the Warden as the source; events for the mark and the landing. |
| The mark | WardenSlamView | A second ring set (edge and filling ring) on the echo's mark, told apart from the slam's at a glance (the art below). |
| The pilot | DevelopmentAutopilot | The cohorts that escape marks escape the echo's too, as the simulation's `EscapeHazard` does, or the scene's frame-perfect parity breaks; `FoundrySimulationSceneValueTests` holds the two to it. |
| The record | horde telemetry, the pacing file | Echo marks and hits a Warden fight beside the slam's, so a device session can set a human's echo hits against the simulation's typical hand. |

**Telegraph art.**
- The echo goes down while the hammer is still in the floor (its 1.5 s recovery), where the hero stood at the blow. It
  must read as the blow's aftershock, not a second slam: a cracked ring or a ring of embers running out from the
  hammer, in a colour of its own, so a player never mistakes which mark lands first.
- The painted Warden needs a tell for it: the hammer held in the floor with a pulse running out of it. EnemyLookView has
  the WindUp and Slam poses; the echo wants a third pose or a floor effect.
- Enraged-only, the echo is the enrage's mechanic. The ForgeWarden prefab already shows an enrage (EnrageBehaviour's
  colour and the "is enraged!" line); the build checks it turns at WardenRule.PhaseTwoAt, so the echo starts as the
  enrage shows.

**Cost.** About a day for the rule, the service, the pilot, the parity tests and the telemetry. The art adds a second
mark decal and the Warden's aftershock tell. Then one device session, to read a human's echo hits against the
simulation's.

### What the owner must decide first

1. **Which band careful should hit**: 80-90 % (the 2026-11-04 research's read) or docs/40 section 10.2's Apprentice
   65-80 %, and for which cohort docs/40's bands are read at all. The echo moves careful, standing and first-card
   together and leaves clumsy where it is. It sets one band for the cohorts that fight the Warden at length; it does
   not build a ladder of skill.
2. **Whether the Warden's health may move with it.** Each echo needs floor 1's share lowered to keep the fight's
   length: 6 for the echo after every blow, 7 for the enraged-only one (from 8.5). That is the gate boss's one balance
   number (`WardenRule.GateHealthShare`).
3. **Then which echo.**
   - **After every blow**, share 6: 20 % for 80-90 %, 30 % for 65-80 %. Both heroes land within 3-4 clears of each
     other, the fight stays at today's length, and the frame-perfect pilot never notices it. The echo is a new rule of
     the whole fight.
   - **Enraged-only**, share 7, 30 % for 80-90 %. The calm half of the fight stays as it is and the enrage becomes a
     second phase with its own mechanic, which may read better to a player. The price: an echo half again as heavy as
     the slam, the heroes 9 clears apart, and a fight 2 s longer. It keeps Ilya's gap over the standing cohort, not the
     Vanguard's. For 65-80 % it needs a 40 % echo, twice the slam, and Ilya still sits on the edge (148 / 161).
4. **Whether the echo is every Warden's or floor 1's.** As the Warden's it reaches floor 2 too: floor 2's share has to
   come down with it (4.3 or 5 from 6.07), and careful humans lose another 21-38 runs there, so the run as a whole
   lands at 64-77 %. A floor-1-only echo needs a floor knob (FloorDefinition) beside the rule.
5. Whichever is chosen, read its first scene build on a device. The typical human's 0.25 s reaction is an assumption,
   and the echo is the candidate most sensitive to it.

## Status 2026-11-11 (morning): the night's merges, and what waits for the owner

The lead's summary of the night of 2026-11-10/11. The sections above hold the numbers.

**Shipped to the game:**
- The careful pilot's cards (CardScore). With human hands the careful pilot clears floor 1 195/199 (was 154/194). The
  clumsy and standing cohorts score their cards the same way, so the clumsy cohort no longer replays the old pilot
  run for run.
- FloorClockView no longer beats as the gate's count hands over to the floor clock in the last minute (latent).
- The heroes are "it" in the code's comments and test messages.

**Changed in the measurement only:**
- Engaged uptime reads the hero's strikes (attack uptime): met, 0.88 / 0.87 in the simulation and 0.87 / 0.87 in
  the Editor (horde-pacing-careful-*-1111.txt).
- The roles row reads three jobs: the horde, the elites (as whole waves) and the Warden.
- The first combat row reads a run that never struck as its whole floor.
- RunHud.RoomPipsThatFit holds every floor to nine rooms.

**Turned down by the lead:** the Daggers' x12 on the Grunt Captains. It was reverted on its branch; the reasons are
in "the Daggers' role".

**Research levers, all off by default:** FootworkRule.StepIn, the Daggers' throw / lunge / bleed / mark / execute,
and the enraged-only Warden echo. The default batch was checked byte-identical after each merge.

**The gate:**
- Met 8, not met 1 (three weapons with distinct roles: the Daggers), not measured 2 (the owner's play, the S23's
  frames).
- Unity EditMode 1137, PlayMode 305 (+4 explicit), CombatChecks 739.
- APK 70C8B120... (main 747896e3), not installed.

**For the owner, in order:**
1. The Daggers: thrown with a bleed, the Warden's armour turning both (the only shape that meets the row), or a
   two-role V0.1 with the row waived.
2. Which band the careful cohort should hit with human hands (80-90 % or docs/40's 65-80 %), since floor 1 is now
   98 / 100 %. Then which Warden echo, and whether floor 1's Warden share may drop to 6-7. Floor 2 would follow
   (4.3-5) if the echo is every Warden's.
3. Turning the step-in on for the careful pilot (pilot fidelity, clears move by 4 at most; it changes the instrument).
4. Engaged uptime's 0.75 bar for attack uptime, and the gate's wait reading as engaged (open decision 2).
5. Pushing main, and installing the APK on the S23.

## Status 2026-11-11: the Daggers are the elites' weapon

**The decision** (the owner delegated the open decisions to the lead, and the lead took this one). The Daggers' job is the
elite enemies, for three reasons:
- docs/40 section 4.1 gives the Riftblade, the Daggers, "Elite/boss strong".
- It mirrors the Sword's x1.5 on the gate boss (`WardenRule.SwordShare`), so each weapon has a target class.
- The thrown and bleeding Daggers (the design options' F) were turned down: docs/42 has no projectile objects in V0.1,
  and docs/40 makes the Riftblade weak under dense pressure, not a horde weapon.

It builds on 2026-11-10 night's row "on the Captains + the Daggers x2 on the Captains". Three changes shipped:
- **The Daggers deal double damage to elites.** `EliteRule.DaggersShare` is 2: the x12 commit is reapplied (3431c179
  reverts 5ef52402) and then set to x2. It is foundry-only, with one never-inlined `EliteRule.DamageOn` that the scene's
  `AttackController` and the simulation strike through. The Daggers' description on the forge's rack and in a run's
  offer of the weapon ends "Double damage to elites" (Weapon_Daggers.asset).
- **The careful and first-card autopilots step in.** `AutopilotCohort.Habit.StepsIn` joins `Habit.All`. Against an elite
  room's Captain, when their best weapon on it is short (the Daggers) and they are safe, they hold it at that weapon's
  reach less 0.15 rather than kiting it from 1.75, outside the Daggers' 1.8. The standing, clumsy and auto-farmer cohorts
  drop the habit and play exactly as before. No cohort steps in on the gate boss: that habit stays research, since it
  takes the Warden's fight under 45 s.
- **The roles row reads the elite job as the elite enemies**, the elite rooms' Grunt Captains, with the rooms' whole
  waves printed beside each weapon's figure. The basis says how the reading came about: it was first proposed on
  2026-11-10, after the whole-wave reading had failed, and the lead took it on 2026-11-11. docs/40 names the enemy, and
  the whole wave folds the escort's ordinary Grunt and two Mites into the elite job, where the horde weapon's area leads
  by construction.

**Before and after** (main 9b5ef052, seeds 1-200 a hero, both floors, every cohort, the Vanguard started with each
weapon; frame-perfect ; `FOUNDRY_REALISM=typical`; `TheDaggersRoleOverASeedBatch`, TestResults/daggers-role-1111/; the
before row plays main's floor bit for bit: the share divided out and the step-in dropped, exact in floating point):

| | Before | After |
|---|---|---|
| Horde a second: Sword / Staff / Daggers | 8.2 / 31.1 / 1.7 ; 8.0 / 30.6 / 1.5 | 8.3 / 31.3 / 1.6 ; 8.4 / 30.9 / 1.5 |
| Elite enemies (the Captains) a second | 6.2 / 12.2 / 4.4 ; 6.1 / 12.2 / 4.4 | 12.7 / 15.4 / **32.5** ; 13.0 / 16.2 / **24.3** |
| The elite rooms' whole waves a second | 9.5 / 20.8 / 5.4 ; 9.5 / 20.6 / 4.9 | 20.6 / 33.7 / 37.7 ; 19.7 / 31.1 / 28.4 |
| Warden a second | 26.4 / 23.3 / 0 ; 27.2 / 22.9 / 4.0 | 26.8 / 24.0 / 0 ; 27.6 / 23.7 / 3.8 |
| Roles, the elite enemies (the gate's) | not met, the Daggers none ; the same | **met**: Daggers +111 % ; **met**: Daggers +50 % |
| Roles, the whole waves | not met ; not met | met (+12 %) ; not met (the Staff 31.1 to the Daggers' 28.4) |
| The Sword's lead on the Warden | 13 % ; 19 % | 12 % ; 16 % |
| A Captain the Daggers reached, under fire P10 / P50 (careful) | 2.0 / 4.3 s ; 2.1 / 4.3 s | 0.8 / 1.1 s ; 0.9 / 1.4 s, none felled within a frame |
| Floor 1 cleared, careful / first-card / standing / clumsy / auto-farmer, Vanguard | 200 / 196 / 193 / 104 / 9 ; 195 / 181 / 179 / 82 / 9 | 200 / 196 / 195 / 106 / 11 ; 198 / 183 / 181 / 81 / 9 |
| The same, Ilya | 200 / 200 / 193 / 184 / 30 ; 199 / 199 / 180 / 141 / 23 | 200 / 200 / 195 / 183 / 36 ; 198 / 197 / 179 / 138 / 29 |
| Both floors, Vanguard | 200 / 196 / 176 / 96 / 1 ; 193 / 179 / 151 / 66 / 1 | 200 / 196 / 180 / 99 / 2 ; 192 / 177 / 162 / 65 / 3 |
| Both floors, Ilya | 200 / 200 / 176 / 162 / 4 ; 195 / 197 / 155 / 105 / 3 | 200 / 200 / 176 / 164 / 6 ; 195 / 192 / 156 / 109 / 3 |
| Warden fight P50, floor 1, Vanguard / Ilya | 58 / 51 ; 53 / 48 s | 56 / 50 ; 51 / 45 s |
| Warden fight P50, floor 2 | 58 / 51 ; 53 / 49 s | 58 / 52 ; 54 / 49 s |

**The holdout**, seeds 201-400, the careful cohort:
- Elite enemies: the Daggers 30.3 to the Staff's 15.0 frame-perfect and 25.4 to 18.9 with human hands, met both ways.
  The whole waves are a tie frame-perfect, 33.5 to 33.7.
- The Sword's Warden lead goes from 17 % to 13 % frame-perfect, and from 19 % to 18 % with human hands.
- Careful clears: floor 1 stays 200 / 200 frame-perfect, and with human hands goes from 199 / 197 to 196 / 200.
- Warden fight P50: 58 / 51 s on floor 1 and 58 / 53 s on floor 2 frame-perfect.

**The acceptance holds:**
- The roles row is met with both hands on the elite-enemies reading.
- No cohort's floor-1 clears move by more than 6 of 200: Ilya's auto-farmer, +6 frame-perfect and +6 with human hands,
  where it picks the Daggers up.
- The frame-perfect Warden fight stays in 45-65 s.
- The Sword keeps more than 10 % on the Warden.

The gate batch on this branch (`FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`) reads "Roles: the Sword the
Warden's, the Staff the horde's, the Daggers the elites'". The gate stands at met 9, not met 0, not measured 2 (the
owner's floor time, the device's frames), reported 4.

**What it does, and what to watch:**
- The Captain takes P50 1.1 s under fire where the Daggers reach it, against 4.3 s before. It is never felled within a
  frame of its first damage (P10 0.8 s), so there is no one-shot.
- The elite fights shorten from 14 s to 5-6 s for every weapon. The Daggers' 1.8 now reaches the Captain because the
  careful pilot holds it there, as a player with the Daggers closes in.
- With human hands the Daggers do not lead the whole waves: the Staff's splash keeps the escort's Mites and Grunt.
- The Sword's frame-perfect Warden lead thins to 12-13 %, from 13-17 %. The Staff-started Vanguard picks the Daggers up
  and kills the Captains sooner, and its Staff's Warden rate rises by about one a second. Re-read it before any other
  Warden or Staff change.
- With human hands, Ilya's floor-1 Warden fight is P50 45 s, at the floor of the window. Both floors gain where a
  cohort picks the Daggers up: the standing Vanguard with human hands goes from 151 to 162.

**Tests.**
- `EliteRuleTests`: the rule at x2, the Daggers x2 on an elite through the strike, every other weapon and every non-elite x1.
- `FoundrySimulationTests.TheSimulationsWeaponsTakeTheGamesShareOnTheElites`: scene and simulation parity.
- `WeaponContentTests` and CombatChecks' `WeaponTextChecks`: the card's words. `RelicForgeFlowTests`: the rack's label.
- `AutopilotCohortTests`: who has the step-in. `FoundryCohortTests.TheCarefulPilotsStepInOnACaptainWithTheDaggersAndNoOtherCohortDoes`:
  who steps in, over the Editor's six seeds.
- `EliteStepInFlowTests` (PlayMode, compile-checked here, on the new Resources/Development/Floor_EliteProof: one elite
  room, a Captain alone): the scene's autopilot holding the Captain within the Daggers' reach, and the Daggers landing
  double on it.
- `FoundryGateTests`: the row's two readings and its basis.

## Status 2026-11-11: the gate's wait - an early call

**The question.** docs/41's open decision 2: what fills the gate's wait. The owner put the gate on the floor's clock at
3:30 (2026-11-02, `FloorDefinition._foundryGateNotBeforeSeconds` 210) to give the floor its 4-6 minutes. On main
158c9f3d the cleared room before the gate holds for 117-122 s of the careful pilot's 278-284 s floor (human hands), its
largest phase. The hero farms the horde there almost risk-free: 11.6-12 levels. Set A and Set B (more content, the gate
at 2:45) shortened the wait but cost the weaker cohorts: with human hands the clumsy Vanguard went from 41 % to 25-26 %
("last night's decisions re-read", section 2).

The idea measured here: once the room before the gate is cleared and the hold begins, the player may call the Warden at
once (ring the gate's bell) instead of waiting for 3:30. The call may pay for the seconds it skips. The hope was that
weak players keep the farm and strong players take a faster floor. Research only: nothing changes in the game.

| Question | Verdict | The numbers that decide it (human hands, both floors cleared of 200, Vanguard / Ilya) |
|---|---|---|
| Does the wait's farm matter? | **yes**, for every cohort: the careful and standing ones need it on floor 2, the clumsy one at floor 1's Warden | careful calling at once: 180 / 182 against 192 / 195 waiting; floor 2's Warden kills it 14 / 12 times, against 3 / 2 |
| A call without a reward | **waiting is never beaten** beyond noise: the call only buys time | careful at level 22: +1 / -1 for 42-43 s; at level 20: 0 / -6 for 61-64 s; at once: -12 / -13 for 113-118 s. Standing loses 13-49 a hero, clumsy 1-48 |
| + a full heal | **wrong**: it pays the hurt, and calling becomes the weak cohorts' best play | clumsy at level 20 + heal: +11 / +22 (floor 1 +26 / +44); it dies at floor 1's Warden 2 / 4 times, against 24 / 47 |
| + one upgrade choice (a Temper) | **too small to matter** | within noise of the bare call: at most 10 a hero either way, no direction |
| + experience, half the wait's farm (2.4 a second skipped) | **the design asked for**: the careful pilots call at once for free; the standing and clumsy ones still do better waiting | careful +1 / -1 (holdout -2 / -4), 114-121 s faster; first-card -7 / 0 (+3 / +7); standing -22 / -14 (-24 / -26); clumsy -11 / -19 (-1 / -10) |

**What was built** (research only, off by default; no scene code, no asset, no gate row changed):
- `FoundryLevers.GateCalls`, `GateCallLevel`, `GateCallHealth`: while the cleared room before the gate holds, the
  simulation's pilot calls the Warden on the first frame its hero is at the level and the health share (0: any). A
  realistic pilot calls a reaction (0.25 s, `PilotRealism.MarkReaction`) after the condition comes true, and never while
  it looks away or a choice is open. The hold ends the next frame (`GateHolds`), as a press would reach
  EncounterController.
- The rewards, paid at the call: `GateCallHeal` (a share of the maximum), `GateCallUpgrades` (choices, the forge's
  Temper through `RunState.GrantBonusUpgrade`) and `GateCallExperienceShare` (that share of
  `DescentSimulation.GateWaitExperienceRate` for each second of the hold skipped; one never-inlined body,
  `GateCallExperienceFor`). The rate is 4.8, the careful wait's P50 of 4.82 experience a second (796 clears).
- `FoundryResult`: `GateCalled`, `GateCallClock`, `GateCallSkipped`, `GateCallExperience`, the wait's own experience
  (`WaitExperience`) and the hero's health as the room before the gate clears (`GateReadyHealth`). Accounting only.
- `Tools/CombatChecks/GateCallResearch` (explicit; `CALL_CONFIGS`, `CALL_REALISM`, `CALL_COHORTS`): each configuration
  by every cohort and both heroes, frame-perfect and with human hands, down both floors. It writes gate-call.md, a row a
  cell in gate-call.csv and a row a run in gate-call-runs.csv.
- `FoundryGateCallTests` holds that:
  - the call is off by default;
  - a call that never comes is the shipped floor run for run;
  - a call at once comes on the hold's first frame and ends the hold on the next, the floor up to it the shipped one's;
  - a level's call waits for its level;
  - each reward is paid at the call;
  - floor 2 never holds, so never calls.

The default batch (`FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`) is byte-identical to main 158c9f3d's:
foundry-batch.csv, foundry-batch-floor2.csv, foundry-cohorts-floor2.csv, foundry-gate.md and foundry-cohorts.md are the
same to the byte; foundry-batch.md differs only in its date and its wall-clock seconds.

**How it was measured.** Main 158c9f3d with this branch's levers (main's 0f9a4ec7 since changes a PlayMode test
alone, not the simulation). `GateCallResearch`, seeds 1-200 a hero, both floors (a floor-1 clear goes on into floor 2,
whose gate does not hold), every cohort, frame-perfect and `FOUNDRY_REALISM=typical` ("human hands"),
`FOUNDRY_PARALLEL=4`; TestResults/gate-call-1111/full/. The holdout: seeds 201-400 with human hands,
TestResults/gate-call-1111/holdout/ and holdout-xp/. A run is the shipped floor until the hold begins, so a difference
against the wait is the call's. Read differences under about 10 of 200 as noise.

The policies:
- at once, as the hold begins;
- at level 18, 20 or 22 (the careful pilot is at 19 at 2:30 on the floor's clock and at 22 at 3:00);
- at once with the hero's health at 80 % of its maximum or more, and at level 20 with the same.

The rewards, on the call at once and at level 20: a heal to full, one upgrade choice (the forge's Temper), and
experience at half the wait's farm.

### What the wait gives

The shipped floor with human hands. Ready: the floor's clock, the level and the health as the room before the gate
clears (P50). Level by the floor's time: P50 at 2:30, 3:00 and 3:30 of the floor's active time. Wait: the clears' P50.
Levels and health lost in the wait: mean of the clears; died in the wait: of 200.

| Cohort | Ready: clock / level / health | Level at 2:30 ; 3:00 ; 3:30 | Level at the gate | Wait | Levels in the wait | Experience a second | Health lost in the wait | Died in the wait |
|---|---|---|---|---|---|---|---|---|
| careful | 93 / 88 s, 14 / 13, 88 / 87 % | 19 / 19 ; 22 / 22 ; 24 / 25 | 25 / 25 | 117 / 122 s | 11.6 / 12.0 | 4.85 / 4.83 | 7 / 8 | 0 / 0 |
| standing | 98 / 93 s, 14 / 14, 88 / 90 % | 19 / 19 ; 22 / 22 ; 25 / 25 | 26 / 26 | 112 / 116 s | 11.2 / 11.7 | 5.00 / 4.99 | 6 / 7 | 0 / 0 |
| first-card | 97 / 89 s, 14 / 14, 83 / 82 % | 19 / 19 ; 22 / 22 ; 24 / 25 | 25 / 26 | 113 / 120 s | 11.2 / 12.0 | 4.91 / 4.87 | 5 / 5 | 0 / 0 |
| clumsy | 101 / 97 s, 14 / 14, 59 / 58 % | 19 / 19 ; 22 / 22 ; 25 / 25 | 25 / 25 | 110 / 112 s | 11.2 / 11.6 | 5.09 / 5.02 | 19 / 18 | 5 / 1 |
| auto-farmer | 91 / 86 s, 14 / 14, 61 / 65 % | 19 / 20 ; 22 / 22 ; 25 / 25 | 25 / 26 | 121 / 124 s | 11.8 / 12.1 | 4.74 / 4.79 | 3 / 5 | 0 / 0 |

Every cohort reaches the hold at level 13-14 and leaves it at 25-26, at the same pace: the horde feeds every hand alike.
It is safe for every cohort but the clumsy one, which comes to it hurt (58-59 %), loses 18-19 more there and is the only
one that dies there (6 of 400). Frame-perfect the careful pilot is ready at 89 / 86 s and waits 120 / 124 s.

### The clears

Floor 1 ; both floors, Vanguard / Ilya. The first row is the shipped floor's clears of 200; every other row its clears
less the wait's.

| Human hands | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| Wait (shipped) | 198 / 198 ; 192 / 195 | 181 / 179 ; 162 / 156 | 183 / 197 ; 177 / 192 | 81 / 138 ; 65 / 109 | 9 / 29 ; 3 / 3 |
| At once | -4 / -2 ; -12 / -13 | -21 / -16 ; -47 / -49 | -9 / -11 ; -23 / -20 | -27 / -35 ; -26 / -48 | -9 / -26 ; -3 / -3 |
| At level 18 | 0 / 0 ; -2 / -8 | -5 / +1 ; -31 / -21 | -5 / -1 ; -8 / -3 | -4 / -11 ; -20 / -29 | -7 / -17 ; -3 / -2 |
| At level 20 | 0 / -2 ; 0 / -6 | 0 / +2 ; -27 / -20 | -2 / +3 ; +1 / +4 | -6 / -7 ; -9 / -22 | -4 / -12 ; -3 / 0 |
| At level 22 | -1 / 0 ; +1 / -1 | +1 / +4 ; -19 / -13 | +2 / +3 ; +3 / +1 | -3 / -1 ; -6 / -16 | -2 / -12 ; -3 / 0 |
| At once, health 80 % | 0 / 0 ; -3 / -5 | -13 / -3 ; -32 / -35 | -1 / -2 ; -3 / -4 | 0 / -2 ; -2 / -5 | -3 / -13 ; -1 / -3 |
| At level 20, health 80 % | +1 / 0 ; +4 / -1 | +2 / +2 ; -18 / -15 | 0 / 0 ; -1 / 0 | -1 / 0 ; -1 / -1 | -2 / -5 ; -1 / -2 |
| At once + full heal | 0 / +1 ; -8 / -9 | +6 / +7 ; -33 / -39 | +1 / +2 ; 0 / -7 | +16 / +31 ; -4 / -14 | -5 / -16 ; -3 / -3 |
| At once + 1 choice | -4 / -3 ; -10 / -13 | -17 / -14 ; -49 / -41 | -11 / -6 ; -14 / -19 | -21 / -40 ; -30 / -41 | -7 / -24 ; -3 / -3 |
| At once + XP 50 % | -1 / +1 ; +1 / -1 | -1 / -1 ; -22 / -14 | 0 / -2 ; -7 / 0 | +4 / -6 ; -11 / -19 | -4 / -10 ; -3 / -2 |
| At level 20 + full heal | +1 / 0 ; 0 / -6 | +13 / +18 ; -8 / -10 | +2 / +3 ; +5 / +6 | +26 / +44 ; +11 / +22 | +7 / +17 ; 0 / +4 |
| At level 20 + 1 choice | -1 / -1 ; 0 / -5 | +2 / +5 ; -22 / -15 | 0 / +3 ; 0 / +1 | -8 / -4 ; -9 / -12 | -3 / -7 ; -3 / 0 |
| At level 20 + XP 50 % | -4 / +1 ; 0 / 0 | 0 / +5 ; -8 / -6 | -6 / +3 ; -5 / 0 | +3 / +5 ; -8 / -11 | -1 / -2 ; -1 / 0 |

| Frame-perfect | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| Wait (shipped) | 200 / 200 ; 200 / 200 | 195 / 195 ; 180 / 176 | 196 / 200 ; 196 / 200 | 106 / 183 ; 99 / 164 | 11 / 36 ; 2 / 6 |
| At once | 0 / 0 ; 0 / 0 | -5 / -4 ; -28 / -19 | 0 / 0 ; 0 / 0 | -20 / -25 ; -29 / -38 | -10 / -32 ; -2 / -5 |
| At level 18 | 0 / 0 ; 0 / 0 | -4 / -8 ; -19 / -23 | 0 / 0 ; 0 / 0 | -14 / -9 ; -16 / -30 | -8 / -19 ; -1 / -5 |
| At level 20 | 0 / 0 ; 0 / 0 | -4 / -6 ; -15 / -16 | -1 / 0 ; -1 / 0 | +3 / -6 ; -6 / -23 | -7 / -17 ; -2 / -5 |
| At level 22 | 0 / 0 ; 0 / 0 | -3 / -7 ; -10 / -12 | 0 / 0 ; 0 / 0 | -2 / -8 ; -3 / -10 | -5 / -12 ; -1 / -4 |
| At once, health 80 % | 0 / 0 ; 0 / 0 | +1 / -2 ; -18 / -6 | 0 / 0 ; 0 / 0 | -1 / +2 ; -1 / 0 | -4 / -16 ; -2 / -4 |
| At level 20, health 80 % | 0 / 0 ; 0 / 0 | +1 / -1 ; -8 / -3 | 0 / 0 ; 0 / 0 | 0 / +2 ; -1 / +1 | -3 / -3 ; -2 / -4 |
| At once + full heal | 0 / 0 ; 0 / 0 | +4 / +4 ; -21 / -15 | 0 / 0 ; 0 / 0 | +19 / +14 ; +6 / +2 | -5 / -23 ; -2 / -5 |
| At once + 1 choice | 0 / 0 ; 0 / 0 | -6 / -2 ; -21 / -21 | 0 / 0 ; 0 / 0 | -9 / -19 ; -20 / -29 | -9 / -31 ; -2 / -5 |
| At once + XP 50 % | 0 / 0 ; 0 / 0 | -1 / +2 ; -8 / -9 | 0 / 0 ; 0 / 0 | +2 / -5 ; -2 / -14 | -6 / -13 ; -1 / -4 |
| At level 20 + full heal | 0 / 0 ; 0 / 0 | +3 / +5 ; -13 / -5 | 0 / 0 ; 0 / 0 | +20 / +14 ; +13 / +5 | +8 / +16 ; -2 / -4 |
| At level 20 + 1 choice | 0 / 0 ; 0 / 0 | -2 / -6 ; -13 / -10 | 0 / 0 ; 0 / 0 | -2 / -5 ; -7 / -8 | -6 / -14 ; -2 / -3 |
| At level 20 + XP 50 % | 0 / 0 ; 0 / 0 | +3 / -1 ; -5 / -6 | 0 / 0 ; 0 / 0 | +2 / +4 ; -5 / -4 | -5 / -11 ; -1 / -2 |

The holdout (seeds 201-400, human hands) reads the same: at once careful -9 / -14 over both floors, standing -57 / -60,
clumsy -28 / -43; at level 20 careful +2 / -7, standing -28 / -24, clumsy -6 / -14; at once with health 80 % careful
-7 / -6, standing -42 / -44, clumsy -2 / -7. With the experience reward: at once careful -2 / -4 and 114-121 s
faster, first-card +3 / +7, standing -24 / -26, clumsy -1 / -10 (floor 1 +6 / +9), auto-farmer 0 / -1; at level 20
careful -2 / -3, standing -8 / -9, clumsy -5 / +3.

### Where the farm's levels go

The careful cohort, every configuration (Vanguard / Ilya; fp frame-perfect, hh human hands). Floor 1: the clears' P50.
Both floors: floor 1 and floor 2 together, P50 of the runs that cleared both. Warden's health: its maximum as it walks
out, which the run's difficulty multiplies. Warden fights: P50 (P90) of the felled fights. Wait: the clears' P50. Called
at: the floor's clock at the call, P50.

| Policy | Pilot | Floor 1 | Both floors | Level at the gate | Warden's health | Warden, floor 1 | Warden, floor 2 | Died at the Warden, floor 1 ; floor 2 | Wait | Called at |
|---|---|---|---|---|---|---|---|---|---|---|
| Wait (shipped) | fp | 291 / 283 | 433 / 419 | 25 / 26 | 3570 / 3570 | 56 / 50 (68 / 60) | 58 / 52 (70 / 62) | 0 / 0 ; 0 / 0 | 120 / 124 | - / - |
| At once | fp | 174 / 162 | 325 / 309 | 14 / 14 | 3060 / 3060 | 61 / 54 (72 / 62) | 62 / 58 (79 / 68) | 0 / 0 ; 0 / 0 | 0 / 0 | 89 / 86 |
| At level 18 | fp | 207 / 194 | 355 / 338 | 18 / 18 | 3060 / 3060 | 61 / 52 (72 / 63) | 61 / 55 (80 / 65) | 0 / 0 ; 0 / 0 | 31 / 32 | 121 / 118 |
| At level 20 | fp | 226 / 215 | 372 / 357 | 20 / 20 | 3570 / 3570 | 59 / 51 (72 / 62) | 60 / 55 (74 / 66) | 0 / 0 ; 0 / 0 | 51 / 53 | 142 / 139 |
| At level 22 | fp | 247 / 236 | 393 / 375 | 22 / 22 | 3570 / 3570 | 58 / 51 (70 / 61) | 59 / 54 (76 / 65) | 0 / 0 ; 0 / 0 | 74 / 75 | 164 / 162 |
| At once, health 80 % | fp | 174 / 164 | 326 / 312 | 14 / 14 | 3060 / 3060 | 61 / 53 (72 / 62) | 61 / 58 (78 / 67) | 0 / 0 ; 0 / 0 | 0 / 0 | 89 / 86 |
| At level 20, health 80 % | fp | 226 / 216 | 373 / 359 | 20 / 20 | 3570 / 3570 | 59 / 51 (72 / 62) | 60 / 55 (73 / 65) | 0 / 0 ; 0 / 0 | 52 / 54 | 142 / 139 |
| At once + full heal | fp | 174 / 162 | 325 / 309 | 14 / 14 | 3060 / 3060 | 61 / 54 (72 / 62) | 62 / 58 (79 / 68) | 0 / 0 ; 0 / 0 | 0 / 0 | 89 / 86 |
| At once + 1 choice | fp | 173 / 162 | 323 / 306 | 14 / 14 | 3060 / 3060 | 61 / 53 (71 / 62) | 61 / 56 (81 / 66) | 0 / 0 ; 0 / 0 | 0 / 0 | 89 / 86 |
| At once + XP 50 % | fp | 170 / 159 | 316 / 301 | 20 / 20 | 3570 / 3570 | 57 / 51 (70 / 59) | 60 / 54 (76 / 65) | 0 / 0 ; 0 / 0 | 0 / 0 | 89 / 86 |
| At level 20 + full heal | fp | 226 / 215 | 372 / 357 | 20 / 20 | 3570 / 3570 | 59 / 51 (72 / 62) | 60 / 55 (74 / 66) | 0 / 0 ; 0 / 0 | 51 / 53 | 142 / 139 |
| At level 20 + 1 choice | fp | 225 / 213 | 371 / 354 | 20 / 20 | 3570 / 3570 | 58 / 51 (70 / 61) | 60 / 54 (75 / 65) | 0 / 0 ; 0 / 0 | 51 / 53 | 142 / 139 |
| At level 20 + XP 50 % | fp | 225 / 213 | 367 / 352 | 23 / 23 | 3570 / 3570 | 57 / 50 (70 / 58) | 58 / 54 (74 / 64) | 0 / 0 ; 0 / 0 | 51 / 53 | 142 / 139 |
| Wait (shipped) | hh | 284 / 278 | 422 / 410 | 25 / 25 | 3570 / 3570 | 51 / 45 (65 / 56) | 54 / 49 (64 / 61) | 1 / 2 ; 3 / 2 | 117 / 122 | - / - |
| At once | hh | 172 / 160 | 317 / 300 | 14 / 13 | 3060 / 3060 | 55 / 49 (63 / 55) | 58 / 54 (70 / 63) | 5 / 4 ; 14 / 12 | 0 / 0 | 93 / 89 |
| At level 18 | hh | 202 / 193 | 344 / 329 | 18 / 18 | 3060 / 3060 | 53 / 48 (64 / 56) | 58 / 52 (71 / 64) | 1 / 2 ; 7 / 9 | 31 / 34 | 124 / 122 |
| At level 20 | hh | 223 / 214 | 364 / 349 | 20 / 20 | 3570 / 3570 | 54 / 48 (66 / 55) | 57 / 51 (71 / 62) | 1 / 4 ; 6 / 7 | 52 / 55 | 145 / 143 |
| At level 22 | hh | 243 / 235 | 382 / 372 | 22 / 22 | 3570 / 3570 | 53 / 46 (65 / 56) | 56 / 51 (69 / 62) | 2 / 2 ; 4 / 3 | 74 / 77 | 167 / 165 |
| At once, health 80 % | hh | 175 / 165 | 324 / 310 | 14 / 14 | 3060 / 3060 | 55 / 48 (64 / 55) | 58 / 53 (70 / 62) | 1 / 2 ; 9 / 8 | 0 / 0 | 93 / 90 |
| At level 20, health 80 % | hh | 226 / 219 | 370 / 361 | 20 / 20 | 3570 / 3570 | 53 / 47 (65 / 55) | 56 / 50 (70 / 62) | 0 / 2 ; 3 / 4 | 54 / 58 | 145 / 144 |
| At once + full heal | hh | 172 / 160 | 317 / 300 | 14 / 13 | 3060 / 3060 | 55 / 49 (64 / 55) | 59 / 54 (70 / 64) | 1 / 1 ; 14 / 13 | 0 / 0 | 93 / 89 |
| At once + 1 choice | hh | 171 / 161 | 316 / 301 | 14 / 13 | 3060 / 3060 | 54 / 48 (64 / 55) | 59 / 54 (72 / 65) | 5 / 5 ; 11 / 13 | 0 / 0 | 93 / 89 |
| At once + XP 50 % | hh | 170 / 157 | 312 / 294 | 20 / 20 | 3570 / 3570 | 52 / 47 (63 / 54) | 56 / 50 (70 / 63) | 2 / 1 ; 4 / 4 | 0 / 0 | 93 / 89 |
| At level 20 + full heal | hh | 223 / 214 | 364 / 349 | 20 / 20 | 3570 / 3570 | 54 / 48 (66 / 55) | 57 / 51 (70 / 62) | 0 / 2 ; 7 / 8 | 52 / 55 | 145 / 143 |
| At level 20 + 1 choice | hh | 221 / 213 | 362 / 349 | 20 / 20 | 3570 / 3570 | 52 / 47 (65 / 56) | 55 / 51 (66 / 61) | 2 / 3 ; 5 / 5 | 52 / 55 | 145 / 143 |
| At level 20 + XP 50 % | hh | 220 / 212 | 360 / 346 | 23 / 23 | 3570 / 3570 | 52 / 46 (64 / 54) | 54 / 50 (68 / 60) | 5 / 1 ; 2 / 4 | 52 / 55 | 145 / 143 |

- **An early call meets a weaker floor-1 Warden.** The run's difficulty is the two altars' 0.1 each and 0.2 an
  evolution, and most heroes evolve their first weapon in the wait. Called at once, the careful run's difficulty is 1.2
  in 340 of 399 runs, where waiting it is 1.4 or more in 298. The Warden walks out with 3060 health (P50) instead of
  3570, and the horde round it is the clock's 90 s horde rather than its 210 s one. So floor 1's Warden costs the
  careful pilot little: the fight runs 55 / 49 s at level 14 against 51 / 45 s at level 25 with human hands, and it
  dies there 5 / 4 times against 1 / 2.
- **The cost comes on floor 2.** Floor 2 has no wait, so the hero goes down with the levels it farmed, or without them.
  The careful cohort that called at once dies at floor 2's Warden 14 / 12 times, against 3 / 2. The standing cohort,
  which trades blows with the Warden, dies there 42 / 53 times against 17 / 21. The clumsy cohort's losses come at
  floor 1's Warden instead: 56 / 83 deaths against 24 / 47, since it arrives hurt and the farm's levels carried it.
- **Experience at half the farm puts the evolution back.** The call at once pays 280-291 experience: the hero meets the
  Warden at level 20 rather than 14 and its difficulty is 1.4 again (3570). The careful pilot then dies at the Wardens
  2 / 1 ; 4 / 4 times (waiting: 1 / 2 ; 3 / 2), and its floor 1 takes 170 / 157 s.

The other cohorts with human hands (floor 1 P50 of every run; level at the gate; wait, the clears' P50; Warden fight on
floor 1; deaths at the Warden, floor 1 ; floor 2):

| Floor 1 ; level at the gate ; wait | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|
| Wait (shipped) | 274 / 270 ; 26 / 26 ; 112 / 116 | 281 / 276 ; 25 / 26 ; 113 / 120 | 243 / 272 ; 25 / 25 ; 110 / 112 | 231 / 241 ; 25 / 26 ; 121 / 124 |
| At once | 161 / 153 ; 14 / 14 ; 0 / 0 | 173 / 161 ; 14 / 14 ; 0 / 0 | 136 / 158 ; 14 / 14 ; 0 / 0 | 108 / 116 ; 14 / 14 ; - / 0 |
| At level 18 | 188 / 184 ; 18 / 18 ; 26 / 29 | 199 / 192 ; 18 / 18 ; 28 / 32 | 168 / 189 ; 18 / 18 ; 25 / 27 | 144 / 149 ; 18 / 18 ; 30 / 34 |
| At level 20 | 208 / 204 ; 20 / 20 ; 45 / 49 | 219 / 212 ; 20 / 20 ; 49 / 54 | 182 / 208 ; 20 / 20 ; 45 / 47 | 165 / 172 ; 20 / 20 ; 56 / 55 |
| At level 22 | 230 / 226 ; 22 / 22 ; 67 / 70 | 239 / 233 ; 22 / 22 ; 72 / 75 | 206 / 230 ; 22 / 22 ; 65 / 69 | 186 / 194 ; 22 / 22 ; 78 / 76 |
| At once, health 80 % | 166 / 157 ; 15 / 14 ; 0 / 0 | 181 / 170 ; 15 / 14 ; 1 / 1 | 175 / 267 ; 25 / 25 ; 108 / 111 | 123 / 237 ; 25 / 25 ; 121 / 123 |
| At level 20, health 80 % | 212 / 208 ; 20 / 20 ; 47 / 50 | 228 / 220 ; 20 / 20 ; 58 / 60 | 207 / 269 ; 25 / 25 ; 108 / 111 | 183 / 237 ; 25 / 25 ; 120 / 117 |
| At once + full heal | 163 / 154 ; 14 / 14 ; 0 / 0 | 174 / 161 ; 14 / 14 ; 0 / 0 | 160 / 167 ; 14 / 14 ; 0 / 0 | 116 / 122 ; 14 / 14 ; 0 / 0 |
| At once + 1 choice | 162 / 153 ; 14 / 14 ; 0 / 0 | 171 / 160 ; 14 / 14 ; 0 / 0 | 137 / 157 ; 14 / 14 ; 0 / 0 | 110 / 116 ; 14 / 14 ; 0 / 0 |
| At once + XP 50 % | 161 / 153 ; 20 / 20 ; 0 / 0 | 169 / 157 ; 20 / 20 ; 0 / 0 | 149 / 160 ; 20 / 20 ; 0 / 0 | 109 / 118 ; 20 / 20 ; 0 / 0 |
| At level 20 + full heal | 208 / 204 ; 20 / 20 ; 46 / 49 | 220 / 212 ; 20 / 20 ; 49 / 54 | 202 / 214 ; 20 / 20 ; 44 / 47 | 171 / 178 ; 20 / 20 ; 53 / 54 |
| At level 20 + 1 choice | 207 / 204 ; 20 / 20 ; 45 / 49 | 218 / 212 ; 20 / 20 ; 49 / 54 | 185 / 207 ; 20 / 20 ; 45 / 46 | 165 / 172 ; 20 / 20 ; 56 / 54 |
| At level 20 + XP 50 % | 207 / 203 ; 23 / 23 ; 45 / 49 | 216 / 210 ; 23 / 23 ; 49 / 54 | 185 / 208 ; 23 / 23 ; 44 / 46 | 165 / 172 ; 23 / 23 ; 56 / 54 |

| Warden fight, floor 1 ; died at the Warden, floor 1 , floor 2 | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|
| Wait (shipped) | 41 / 39 (49 / 45) ; 18 / 21 , 17 / 21 | 48 / 44 (58 / 50) ; 2 / 3 , 6 / 3 | 47 / 48 (55 / 63) ; 24 / 47 , 10 / 16 | 28 / 29 (36 / 32) ; 98 / 168 , 3 / 16 |
| At once | 45 / 41 (51 / 47) ; 39 / 37 , 42 / 53 | 54 / 48 (60 / 55) ; 11 / 14 , 9 / 10 | 53 / 52 (64 / 67) ; 56 / 83 , 8 / 26 | - / 35 (- / 38) ; 107 / 194 , 0 / 2 |
| At level 18 | 44 / 40 (50 / 46) ; 23 / 20 , 41 / 43 | 52 / 46 (60 / 52) ; 7 / 4 , 7 / 7 | 53 / 51 (60 / 69) ; 32 / 59 , 20 / 36 | 35 / 31 (37 / 33) ; 105 / 185 , 2 / 10 |
| At level 20 | 43 / 40 (51 / 47) ; 18 / 19 , 45 / 42 | 51 / 45 (60 / 51) ; 4 / 0 , 2 / 4 | 52 / 50 (61 / 63) ; 34 / 55 , 12 / 31 | 31 / 30 (33 / 33) ; 102 / 180 , 4 / 8 |
| At level 22 | 43 / 40 (50 / 46) ; 17 / 17 , 38 / 39 | 50 / 45 (59 / 52) ; 0 / 0 , 3 / 5 | 49 / 49 (58 / 63) ; 31 / 49 , 14 / 27 | 34 / 29 (36 / 34) ; 100 / 180 , 6 / 9 |
| At once, health 80 % | 45 / 41 (51 / 46) ; 31 / 24 , 36 / 52 | 53 / 47 (60 / 54) ; 3 / 5 , 7 / 5 | 48 / 49 (58 / 64) ; 24 / 49 , 11 / 18 | 34 / 29 (37 / 33) ; 101 / 181 , 2 / 10 |
| At level 20, health 80 % | 43 / 40 (50 / 47) ; 16 / 19 , 39 / 38 | 50 / 45 (60 / 51) ; 2 / 3 , 6 / 3 | 48 / 48 (56 / 64) ; 25 / 47 , 10 / 16 | 33 / 30 (36 / 33) ; 100 / 173 , 3 / 14 |
| At once + full heal | 45 / 41 (51 / 46) ; 12 / 14 , 53 / 65 | 54 / 49 (60 / 55) ; 1 / 1 , 6 / 12 | 54 / 52 (65 / 67) ; 13 / 17 , 27 / 56 | 38 / 35 (40 / 36) ; 103 / 184 , 2 / 7 |
| At once + 1 choice | 45 / 40 (52 / 46) ; 35 / 35 , 50 / 48 | 53 / 48 (61 / 54) ; 13 / 9 , 7 / 12 | 53 / 52 (62 / 67) ; 50 / 88 , 17 / 22 | 38 / 33 (40 / 36) ; 105 / 192 , 2 / 3 |
| At once + XP 50 % | 43 / 40 (51 / 48) ; 19 / 22 , 34 / 35 | 50 / 45 (56 / 51) ; 2 / 5 , 9 / 3 | 50 / 49 (60 / 67) ; 25 / 54 , 21 / 30 | 30 / 31 (36 / 33) ; 102 / 178 , 5 / 13 |
| At level 20 + full heal | 43 / 40 (50 / 47) ; 5 / 3 , 39 / 50 | 51 / 45 (61 / 51) ; 0 / 0 , 3 / 2 | 52 / 50 (63 / 63) ; 2 / 4 , 23 / 37 | 33 / 31 (37 / 34) ; 91 / 151 , 11 / 28 |
| At level 20 + 1 choice | 43 / 40 (50 / 46) ; 16 / 16 , 41 / 39 | 50 / 45 (59 / 51) ; 2 / 0 , 3 / 6 | 51 / 49 (61 / 65) ; 36 / 52 , 11 / 28 | 31 / 30 (35 / 32) ; 101 / 175 , 5 / 9 |
| At level 20 + XP 50 % | 42 / 40 (49 / 46) ; 18 / 16 , 27 / 30 | 49 / 44 (58 / 50) ; 8 / 0 , 4 / 6 | 49 / 48 (59 / 64) ; 25 / 43 , 16 / 32 | 32 / 29 (35 / 33) ; 99 / 170 , 4 / 13 |

### The behavioural reading

Which policy maximises each cohort's clears, with human hands (both floors, the two heroes together)?

| Cohort | Without a reward | With experience at half the farm | With a full heal |
|---|---|---|---|
| careful | the wait or a late call, tied within noise (level 22: +1 / -1). A call is only time: 42-64 s for nothing, 113-118 s for -12 / -13 | the call at once: +1 / -1 (holdout -2 / -4), 114-121 s faster | the wait; the heal adds nothing (at once -8 / -9, at level 20 0 / -6) |
| first-card | the wait, or a call at level 20-22 (+1 / +4, +3 / +1) | a tie: the call at once -7 / 0 (holdout +3 / +7) | the call at level 20 (+5 / +6) |
| standing | the wait; every call loses 13-49 a hero | the wait (-22 / -14 at once, -8 / -6 at level 20) | the wait (-8 / -10 at level 20) |
| clumsy | the wait; every call loses, the health-gated ones least (-1 / -1), since a hurt clumsy hero never calls | the wait over both floors (-11 / -19 at once, holdout -1 / -10); floor 1 alone a tie (+4 / -6, holdout +6 / +9) | **the call** at level 20: +11 / +22 |
| auto-farmer | the wait (3 / 3 of 200; every call within 3) | the wait (within 3) | the call at level 20 (0 / +4; floor 1 +7 / +17) |

Frame-perfect, the careful and first-card pilots lose at most one run to any call (200 / 200 and 196 / 200 over both
floors). The standing, clumsy and auto-farmer pilots lose to every call they make without a reward; the health-gated
calls leave the clumsy hero waiting.

So:
- **Without a reward the button is a trap.** Waiting is the best play for every cohort. For the careful pilot a late
  call (level 20-22, 2:40-3:00) is close to free, but it buys under a minute. For every weaker cohort a call costs runs,
  most of all on floor 2.
- **With experience at half the wait's farm it is the design asked for.** Calling at once is best for the strong pilots:
  the careful clears hold and the floor is two minutes shorter. Waiting stays best for the weak: the standing pilot
  clears 14-26 more a hero over both floors by waiting, the clumsy one 1-19 more (on floor 1 alone it is a tie), and the
  auto-farmer's 3 / 3 do not move. The reward replaces the farm's levels only by half. The careful pilot needs no more
  than that; the others need the whole farm, most of all for floor 2.
- **A heal inverts the design.** It pays the players who arrive hurt, who are the weak ones, so that calling becomes
  their best play and the floor gets easier for them (clumsy floor 1 +26 / +44). The careful pilot arrives at 88-93 %
  and gains nothing from it.
- **One upgrade choice is noise**, a twelfth of what the wait gives.
- **A health threshold on the call protects the clumsy, not the standing.** The clumsy hero arrives hurt and never
  calls under it. The standing hero arrives healthy (88-90 %), calls, and loses 32-35 a hero at once over both floors.

### What the scene would need

None of it is built. In the simulation the call is a frame of the pilot's (`PilotCallsGate`) and one condition of the
hold (`GateHolds`). The scene needs the same in its own places, and one rule body for parity.

| Piece | Where | What |
|---|---|---|
| The call | EncounterController | `CallGate()`: while `IsHoldingForGate`, it sets a `_gateCalled` that `GateHoldsForClock()` reads, so the hold ends on the next Update, as in the simulation. It is reset in `EnterFloor` and `OnHeroDied` beside `_holdingForGate`, and raises an event for the HUD and the telemetry. Only a floor whose gate holds offers it (Floor_QuicksilverVaults sets no time). |
| The rule | a `GateCallRule` beside `WardenRule` (Scripts/Combat) | Whether a call is open, and its reward: the experience for the seconds skipped as one never-inlined body that EncounterController and the simulation both call. The simulation's `GateCallExperienceFor` and `GateWaitExperienceRate` move onto it. |
| The control | FloorClockView, RunHud | The ember "Gate opens in m:ss" label becomes a button while the gate holds: "Ring the bell: the Warden now", with what it pays ("+290 XP", counting down 2.4 a second with the clock). The steering thumb has to reach it: a large target by the clock. A bell at the gate that the hero stands on for a second (ExitRule's dwell) is the other way; it adds a walk through the horde and was not measured. |
| The bell | the gate court (FoundryMap's gate, its art) | A bell or brazier at the gate that lights as the hold begins and rings at the call, so the call is seen in the world and not only on the HUD. |
| The pilot | DevelopmentAutopilot | A call policy among development/autopilot.txt's words ("call", "call 20", "call 20 health 80"), read as the simulation reads `FoundryLevers.GateCalls`, `GateCallLevel` and `GateCallHealth`, so that HordePacingMeasurement plays it; `FoundrySimulationSceneValueTests` holds the two to one rule. The default autopilot keeps waiting, so the gate's rows keep reading the shipped floor. |
| The record | RunTelemetry | A `gate_called` event: the floor's clock, the seconds skipped, the level, `hero_hp` / `hero_max_hp` and the reward paid. room_complete's `gate_hold_sec` already measures the hold. A device session then reads who calls, when, and what it cost them at the Wardens. |
| The tests | PlayMode, beside GateHoldFlowTests | On the proof floor (its gate waits 0:12): a call during the hold begins the gate boss's room the next frame and pays its reward; no call outside the hold. |

### Recommendation

1. **If the floor's 4-6 minutes are the point, do not build the call.** It does not fill the wait. It removes it for
   whoever presses it. A careful player who calls at once with the reward clears floor 1 in 170 / 157 s and both floors
   in 312 / 294 s, against 284 / 278 s and 422 / 410 s waiting. The gate stays at 3:30 as the floor's length. What fills
   the wait is then still the horde, or content (Set A / Set B) at the cost the re-read measured.
2. **If a strong player may take a faster floor (the idea's intent): build the call with experience for the seconds
   skipped, at half the wait's farm.** That is 2.4 a second, about 290 as the hold begins, counting down on the prompt.
   Offer it only on a floor whose gate holds (floor 1), with no heal and no card. It is the only version measured that
   is free for the careful pilot (+1 / -1 with human hands, 0 / 0 frame-perfect, -2 / -4 on the holdout) and still a
   loss over both floors for the standing and clumsy ones. The 4-6 minutes then hold for those who wait. The default
   autopilot keeps waiting, so the gate's floor-time row still reads the shipped floor.
3. **Never pay a heal at the call.** It turns the bell into the weak player's best play.
4. **Read a human before the build.** The simulation calls by a fixed rule. Whether a weak player resists "+290 XP", and
   when a careful one presses, is a device session's question: the `gate_called` telemetry above would answer it.

## Status 2026-11-11 (late morning): the lead's decisions on the night's questions

The owner said "devam edelim" and had left the open gameplay decisions to the lead on 2026-11-10. The lead took the
night's questions as follows.

- **The Daggers are the elites' weapon** (the status above): x2 on the elite enemies, named on their card. The
  careful and first-card pilots step in on a Captain with them. The roles row reads the elite enemies, with the whole
  waves printed beside them.
  - docs/40 section 4.1 gives the Riftblade, the Daggers' character, "Elite/boss strong", and the rule mirrors the
    Sword's x1.5 on the gate boss.
  - The thrown Daggers with a bleed (the design options' F) were turned down. docs/42 allows no projectile objects in
    V0.1, and docs/40 makes the Riftblade fragile under dense pressure, not a horde weapon.
  - Main 158c9f3d, pushed as 0f9a4ec7. The gate: met 9, not met 0, not measured 2 (the owner's floor time and the S23's
    frames).
- **Floor 1's difficulty stays as it is for V0.1.** The careful cohort with human hands clears 98-100 %, but clears
  are a reported row of the gate, not a target. docs/40 section 10.2's bands are a whole run's win rates by difficulty
  mode, not one floor's, and the clumsy cohort (41 / 71 %) shows that the floor still turns weaker hands back. The
  Warden echo is not built. Its levers stay for the climb's later floors, where a run's bands can be read.
- **The gate's early call is not built** (the status above). The owner set the 3:30 gate to give the floor its 4-6
  minutes, and a call removes the wait for whoever presses it. Without a reward the call is a trap. The only reward
  that keeps the weaker cohorts waiting is experience at half the wait's farm. It stays an option for the owner, to be
  played on a device first.
- **The step-in on the gate boss stays research**: it takes the Warden's fight under 45 s.
- **The engaged uptime's bar stays 0.75** for attack uptime (0.88 / 0.87). Raising it is the owner's call.
- **docs/46 proposes the third character**, the Riftblade (Nix Vane), on docs/45's pipeline. Nothing is built. Its
  build waits for the owner's go and for the S23's frame-time row (docs/27 decision 12).

What waits for the owner and the phone: installing the APK, the S23's frame time at the chosen crowd, the owner's own
floor time, and docs/46's questions.

## Status 2026-11-11: the foundry's finale in the simulation

**Why.** On the foundry the Descent runs floor 1 (Ember Halls), floor 2 (Quicksilver Vaults) and then the Final Swarm
(Floor_FinalSwarm: one swarm room, an exit to walk out of). The foundry simulation played floors 1 and 2 only, so nobody
had measured the finale: how long it lasts, who survives it, whether walking out of it works, and the whole Descent's
length and clear rate against docs/40 phase 2's full run of 13-16 minutes. The bug fixed on main 6a09574f - the swarm
after floor 2's gate made its first enemy the Warden - sat on exactly that path.

**What the scene does there** (EncounterController's BeginSwarm, TickSwarm, SpawnSwarmEnemy and LeaveSwarm,
SwarmDirector, SwarmRule, ExitRule, CombatSetup, PitEruptionService, AltarService, ChestSpawner, DevelopmentAutopilot
and the asset):
- Floor_FinalSwarm is one swarm room, the Unending Crowd (RoomKind 4), whose twelve waves are a ladder: three Cinder
  Mites at first, a Grunt from the third wave, a Runner from the sixth, a Tank from the tenth, and in the twelfth a
  Forge Warden among nine others - an ordinary enemy there, with no slam. The floor is not climb-scaled: its enemies
  take health 1 and damage 0.92, where floor 2's take 1.4 and 1.288 and Cursed Gold's +20 %. No modifier, no horde
  roles, no floor below.
- After floor 2's checkpoint (Descend) and the 1 s advance delay, BeginSwarm opens one pack for the room's life.
  SwarmDirector reads the ladder by the room's clock - a wave every 12 s, so the Tanks walk in at 1:48 and the Warden at
  2:12 - and each enemy takes the ramp's difficulty as it walks in (x1.02 every 3 s: x1.49 at 1:00, x2.2 at 2:00, x4.9
  at 4:00) times the run's. A fallen enemy's place refills 2 s after its fall. Each enemy walks in alone round the hero,
  through the foundry's PlacePack.
- No horde: HordeWanted needs a floor with horde roles and no swarm room, so the floor clock is hidden. No pit:
  PitEruptionService runs only outside a swarm room. No gate boss since 6a09574f. The floor's two altars answer again,
  since AltarService resets them on a new floor, and the room's chest (a quarter of the hero's health) stands at floor
  (1.8, 1).
- A kill pays at once, and its level-ups wait for a sitting every 10 s of the room's clock.
- The exit is ExitRule.SpotFor on the foundry's geometry: floor (0, 6), on the court's east side and outside the pit's
  band. The hero holds it by standing still within 0.9 of it for 1 s. Leaving releases the held level-ups and clears the
  floor; with no floor below, CombatSetup ends the run in victory, and the offers just released are withdrawn with it.
- The development autopilot plays it with its cohort's habits. The careful pilot kites; otherwise it draws the swarm to
  the pit's rim. Its lure reads the pit as active (`Pit.IsActive`), not as running, so it lures to a rim that never
  spills there. Between kills it walks to the altars. From 60 s on the room's clock (`SwarmStay`) it walks to the exit
  before anything else and stands on it. Its "linger" and "idle" modes do not apply in a swarm room (`Lingering`).

**What was built** (the simulation and its reports; no scene code, no asset):
- `FoundryLevers.Floors = 3`: after a floor-2 clear the run answers the checkpoint with Descend and plays the finale
  (`DescentSimulation.FoundryFinale`, floor 2's `Next`). Its ladder is the hall Descent's `FinalSwarm`, which
  DescentFlowTests holds to the asset, at the floor's own multipliers. The swarm goes through SwarmDirector and
  SwarmRule, the entries through the foundry's PlacePack, and the exit and its hold through ExitRule. Kills pay at once,
  the sittings come every 10 s, and the walk out ends the run in victory. As the scene's ReleaseWave does since
  6a09574f, the swarm resets the gate's and the elite room's flags. The pilot turns for the exit at the autopilot's
  60 s; `FoundryLevers.FinaleStaySeconds` (research, off) stays longer.
- The code is a new partial, `Tests/Support/DescentSimulation.FoundryFinale.cs`. `DescentSimulation.Foundry.cs` gains
  `partial` on four classes, floor 2's next floor and one-to-three-line hooks: the endless room's progress, the swarm's
  frame, its first step, its deaths and their phase, the pit outside a swarm room and the pilot's exit goal.
- `FoundryResult` reads the finale: the swarm's clock, ramp step and ladder wave at the end, the hero's level, health
  and difficulty entering, the walk out and where it left from, the sittings, and the frames with a gate boss.
- The batch report with FOUNDRY_FLOORS=3 writes the finale's tables (foundry-finale.md, and in the cohort section) and a
  row a run (foundry-finale.csv). `FinaleStayResearch` (explicit) plays the finale at longer stays.
- Tests. `FoundryFinaleTests` (CombatChecks and EditMode, read over the Editor's four seeds and both heroes, never one
  seed's path):
  - the finale is floor 2's next floor and the hall Descent's ladder, and its exit is on the floor, outside the band;
  - it is played after a floor-2 clear and ends, walked out or fallen, never at the limit, with no horde, no pit and the
    level and health floor 2 left;
  - floors 1 and 2 are the same runs, field for field, with or without it;
  - no gate boss in it: no frame of the swarm reads one, no slam, no reveal, none of the Sword's share on a Warden and
    none of the Daggers' on an elite. With the flags' reset taken out it fails: 2,196 such frames in its first run;
  - walking out is a victory, from the exit, after the stay and the hold;
  - the stay lever.
  `FoundrySimulationSceneValueTests.TheFoundrysFinaleIsTheAsset` holds the finale's floor, ladder and enemies to
  Floor_FinalSwarm and the 60 s to `DevelopmentAutopilot.SwarmStay` (Unity; compile-checked here).
- The default batch (FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2) is byte-identical to main's at each merge:
  6a09574f, cebf8775 and 4db8f5b6. foundry-batch.csv, foundry-batch-floor2.csv, foundry-cohorts-floor2.csv,
  foundry-gate.md and foundry-cohorts.md are the same to the byte, and foundry-batch.md differs only in its date and its
  wall-clock seconds.

**How it was measured.** Main cebf8775 with this branch. The batch report ran with FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0
FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=3 FOUNDRY_PARALLEL=6, frame-perfect and with FOUNDRY_REALISM=typical ("human hands"),
both heroes, every cohort. The outputs are in TestResults/finale-1111/fp and hh: foundry-finale.md, foundry-finale.csv
with a row a finale, and the cohort section. Floors 1 and 2 are the default batch's runs: the finale comes after them
and changes nothing before it. A first run on 6a09574f, before main's forge-walk fix, is kept in
finale-1111/pre-merge-6a09574f and reads the same. So does a run after merging main 4db8f5b6, where the boss's reveal
holds every attacker on floors 1 and 2 (finale-1111/merge-4db8f5b6). It finds 5 deaths in 1,421 finales frame-perfect
and 18 in 1,248 with human hands, every one entering under 15 % of its maximum; the finale's P50 is 65 s and the
Descent's 467 / 463 s. The tables below are cebf8775's.

### Who reaches the finale, and how it ends

Of 200 seeds a hero, Vanguard / Ilya. Reached: cleared both floors. Walked out and died there: of those that reached it.

| Cohort | Frame-perfect: reached ; walked out ; died there | Human hands: reached ; walked out ; died there |
|---|---|---|
| careful | 200 / 200 ; 200 / 200 ; 0 / 0 | 194 / 195 ; 194 / 193 ; 0 / 2 |
| standing | 173 / 185 ; 172 / 182 ; 1 / 3 | 159 / 159 ; 155 / 157 ; 4 / 2 |
| first-card | 196 / 200 ; 196 / 200 ; 0 / 0 | 178 / 197 ; 174 / 194 ; 4 / 3 |
| clumsy | 101 / 165 ; 101 / 165 ; 0 / 0 | 66 / 105 ; 65 / 103 ; 1 / 2 |
| auto-farmer | 2 / 3 ; 2 / 3 ; 0 / 0 | 2 / 4 ; 1 / 4 ; 1 / 0 |

- **Walking out works.** Every hero that survived the finale walked out of it: 1,421 of 1,425 frame-perfect and 1,240 of
  1,259 with human hands, each from within the exit's 0.9. None timed out. No frame of any of the 2,684 swarms read a
  gate boss.
- **Deaths there**: 4 frame-perfect and 19 with human hands. Grunts dealt 22 of the 23 killing blows and a Runner one;
  20 fell fighting and 3 after turning for the exit. Every one of them entered the finale under a fifth of its maximum
  health (0-19 %). Of the heroes that entered under a quarter, 4 of 46 and 19 of 124 died; of the 1,379 and 1,135 that
  entered with more, none did.

### How long it lasts

For every cohort and both hands the finale takes P50 65 s, P90 66 s, from floor 2's checkpoint with the 1 s descent
included. That is the autopilot's 60 s, then a 4.2-4.5 s walk to the exit (P90 5.0 s) with its 1 s hold, and every
walk-out lands inside docs/40's 45-90 s. At its end the swarm's clock reads 64 s: the ladder's sixth wave (a Runner, a
Grunt and four Mites) at x1.52 on the ramp. The hero has made 85 kills and sat 6 times, and at most 5 enemies stood at
once (P50). The careful pilot spends 43-44 s of it luring the swarm to the pit's rim.

### The hero coming in

Vanguard / Ilya; health as a share of the maximum. Health lost: in the finale, mean.

| Cohort | Level entering P50, frame-perfect | Health entering P50 (P10), frame-perfect | Health entering P50 (P10), human hands | Health lost: frame-perfect ; human hands |
|---|---|---|---|---|
| careful | 37 / 37 | 92 / 92 % (82 / 82) | 69 / 74 % (43 / 40) | 2 / 3 ; 16 / 17 |
| standing | 36 / 36 | 61 / 60 % (32 / 31) | 52 / 55 % (21 / 23) | 15 / 16 ; 15 / 16 |
| first-card | 37 / 36 | 92 / 93 % (81 / 84) | 65 / 68 % (32 / 32) | 3 / 3 ; 14 / 14 |
| clumsy | 36 / 36 | 66 / 67 % (35 / 30) | 50 / 48 % (17 / 21) | 7 / 9 ; 13 / 15 |
| auto-farmer (2-4 runs) | 33 / 33 | 12 / 18 % | 4 / 14 % | 7 / 8 ; 1 / 11 |

The hero comes in at level 35-38 with the run's difficulty at 1.6 (1.4 for the auto-farmer). It takes one of the floor's
altars (1.0 a run) and gains 2 levels it never uses. Nothing heals it between floor 2's Warden and the swarm: the
descent heals nothing, a sitting's health card fills only the room it adds, and the autopilot never walks to the room's
chest (5 of 1,425 and 5 of 1,259 finales opened it).

### The whole Descent against 13-16 minutes

The walk-outs' three floors. Floors: P50 in seconds, with human hands. The Descent: P50 (P90) in minutes. Vanguard /
Ilya.

| Cohort | Completed of 200: frame-perfect ; human hands | Floor 1 / floor 2 / finale | The Descent, frame-perfect | The Descent, human hands |
|---|---|---|---|---|
| careful | 200 / 200 ; 194 / 193 | 276 / 137 / 66 ; 270 / 133 / 65 | 8.2 (8.7) / 7.9 (8.4) | 8.0 (8.4) / 7.8 (8.2) |
| standing | 172 / 182 ; 155 / 157 | 265 / 124 / 65 ; 262 / 123 / 65 | 7.6 (7.9) / 7.5 (7.7) | 7.6 (7.8) / 7.5 (7.8) |
| first-card | 196 / 200 ; 174 / 194 | 273 / 131 / 65 ; 268 / 126 / 65 | 8.0 (8.4) / 7.8 (8.2) | 7.8 (8.2) / 7.6 (8.0) |
| clumsy | 101 / 165 ; 65 / 103 | 269 / 129 / 66 ; 270 / 127 / 66 | 7.7 (8.1) / 7.7 (8.1) | 7.8 (8.0) / 7.7 (8.2) |

Over every cohort the Descent takes P50 7.8 min (P90 8.3) frame-perfect and 7.7 (8.2) with human hands. The longest run
took 9.3 min, and no run reached 13 minutes.

### The stay (research)

The autopilot turns for the exit at 60 s whatever the swarm does, so the finale's length and its deaths above are the
pilot's policy as much as the swarm's. `FinaleStayResearch` has the pilot stay longer (FoundryLevers.FinaleStaySeconds):
the careful, standing and clumsy cohorts, both heroes, seeds 1-200, both hands, in TestResults/finale-1111/stay. It
plays the same runs as above up to the finale. The table reads walked out, of the runs that reached the finale, Vanguard
/ Ilya.

| Stay | careful: frame-perfect ; human hands | standing: frame-perfect ; human hands | clumsy: frame-perfect ; human hands | The swarm at the turn |
|---|---|---|---|---|
| 60 s | 100 / 100 ; 100 / 99 % | 99 / 98 ; 97 / 99 % | 100 / 100 ; 98 / 98 % | ladder wave 6, x1.49 |
| 120 s | 100 / 99 ; 60 / 51 % | 46 / 39 ; 42 / 40 % | 85 / 78 ; 58 / 55 % | wave 11 (the Tanks from 1:48), x2.21 |
| 180 s | 56 / 48 ; 4 / 4 % | 0 / 0 ; 0 / 0 % | 7 / 9 ; 2 / 5 % | wave 12 (the Warden from 2:12), x3.28 |
| 300 s | 2 / 1 ; 0 / 0 % | 0 / 0 ; 0 / 0 % | 0 / 0 ; 0 / 0 % | wave 12, x7.24 |

A pilot that never leaves dies at about these finale seconds (P50, the 300 s row):
- careful: 181-189 frame-perfect and 124-128 with human hands;
- standing: 117-120 with either hands;
- clumsy: 143-148 frame-perfect and 129-130 with human hands.

Past 2:12 the ladder's Warden walks in as one of the crowd and deals a third of the careful frame-perfect pilot's deaths
at 180 s; otherwise Grunts deal most of the blows, then Tanks and Runners. docs/35 read 202 s for the hall Descent's
calibrated Staff.

### The reading

- **The finale is not broken.** It opens after floor 2's checkpoint as the scene's does. No gate Warden is left in it:
  no frame of the 2,684 swarms has one, where the test's first run read 2,196 with the flags' reset taken out. Every
  hero that survives reaches its exit at (0, 6) and holds it; none is stuck, none times out, and the walk out ends the
  run in victory.
- **At the autopilot's 60 s it is the safest minute of the run.** Its enemies are weaker than floor 2's: it is not
  climb-scaled, so they take health 1 and damage 0.92, against floor 2's 1.4 and 1.288 with Cursed Gold's +20 %. They
  are fewer: at most 5 stand at once (P50), against floor 2's horde of up to 40 and its node waves. And the pilot leaves
  at the ladder's sixth wave, x1.49 on the ramp, before the Tanks and the Warden walk in. It kills only heroes that come
  into it nearly dead: 23 deaths in 2,684 finales, every one entered under a fifth of its maximum.
  - The scenario, the same in both measurements: the first-card Vanguard with human hands, seed 147. Floor 2's Warden
    leaves it 3.4 of 115 health. It fights the swarm for 52 s untouched, and a Grunt's first blow ends it.
  - Nothing heals a hero between floor 2's gate and the swarm. The descent heals nothing. The room's chest, a quarter
    heal, stands at (1.8, 1), about 25 units from the gate landing where the hero arrives, and the autopilot never walks
    to it (10 of 2,684 finales opened it).
- **Past that minute the swarm is a cliff.** With human hands the careful pilot walks out of 100 / 99 % of its finales
  at 1:00, 60 / 51 % at 2:00 and 4 / 4 % at 3:00. A pilot that never leaves dies at P50 2:04-2:08 (frame-perfect
  3:01-3:09). docs/40 section 10.4 puts the Final Swarm's extract thresholds at 45, 90 and 120 s; at 120 s the careful
  human player is a coin flip.
  - Staying pays nothing in this build but the kills' gold and levels the run ends with: docs/40's rising extract
    rewards are not built.
  - Nothing on the HUD shows the ramp or the room's clock. RunHud's details line reads `WaveNumber / WaveCount`, which
    stays 1 / 12 in a swarm room (read from the code, not seen).
  - So a player who stays to see what the finale holds loses half its runs at 2:00. That is the one scenario that reads
    as unfair. A player who walks out at once loses nothing.
  - Whether the finale should reward staying (docs/40's thresholds) or show its ramp is the owner's call; neither is
    measured here.
- **The whole Descent takes about 8 minutes, not 13-16.**
  - Floor 1 (P50 262-276 s) sits inside docs/40's 4-5 minutes.
  - Floor 2 (123-137 s) is half of that: its gate opens as its room clears, with no surges and no second elite.
  - The finale adds 65 s.
  - docs/40's full run is three 4-5 minute floors and a 45-90 s Final Swarm; this Descent has two floors before its
    finale. The careful pilot's 8.0 minutes are 5 short of 13. Floor 2 at floor 1's length (about +2.3 minutes) and a
    third floor of 4-5 minutes would land the run in 13-16. A longer stay cannot carry it: 2:00 in the finale costs half
    the careful human runs and adds a minute.
- **The autopilot's lure in the swarm is a quirk of the measuring pilot, not of the game.** `DevelopmentAutopilot.Goal`
  draws a fight to the pit's rim whenever the pit is active (`_setup.Pit.IsActive`, true on the whole foundry), and
  PitEruptionService never runs in a swarm room. So the careful pilot drags the swarm to a rim that never spills, for
  43-44 s of its 65. It costs nothing measurable. If the lure should read only a running pit, `IsRunning` is the flag;
  that is runtime code, left to its owner.
- **No scene bug was found on the finale's path** beyond the gate flag fixed on 6a09574f. The scene's floor, director,
  exit, pit, altars, chest and pilot read as the simulation now plays them, and main's c56f7151 already names a finale
  walk-out on the result screen.

**What waits.**
- The owner's call: a finale that is a safe minute for whoever walks out, with a cliff for whoever stays without a
  reason to, or docs/40 10.4's rising rewards at its thresholds. Then the run's 13-16 minutes: floor 2's length and a
  third floor, rather than the finale.
- The scene's confirmation: a HordePacingMeasurement run down all three floors (the finale's 65 s, its exit, the
  victory). The Editor has measured neither floor 2 nor the finale.
- The heal between floor 2 and the finale, if the near-dead deaths should go: a Mend at the checkpoint, or the room's
  chest where the hero arrives. Neither is measured.

## Status 2026-11-11 (evening): the finale's heal and floor 2's length, built

The owner: "devam edelim, %85 olana kadar". The lead's decisions, on docs/47's measured options:
- **The finale's chest stands where the hero arrives** (ChestSpawner, a foundry swarm room only; the hall arena keeps
  its spot). It is the room's own quarter heal. Finale deaths 34 -> 0 of 1,248 (seeds 1-200, both heroes, every
  cohort, human hands).
- **Floor 2's gate opens at 3:30, as floor 1's** (Floor_QuicksilverVaults `_foundryGateNotBeforeSeconds: 210`; the
  simulation's FoundryFloorTwo mirrors it). Floor 2's clears P50 about 2:10 -> 4:30; the hero reaches the Warden 8-9
  levels higher. Floor-2 clears rise for standing and clumsy; the first-card cohort's floor-2 deaths go 6 -> 16.
- **The whole Descent:** P50 about 8 -> 10 min (careful 10.3 / 10.2, every cohort 9.9-10.3; TestResults/gate-1111i).
  docs/40's 13-16 needs a third floor; docs/47 measures a stand-in at 14.2-15.1 min. A longer finale stay is a cliff
  and is not the way to buy time.
- The gate's rows read floor 1 and did not move: met 9, not met 0, not measured 2.

Fixed the same evening (the review of ba11a36f..2c620d21 found no high-severity bug, and four small ones):
- the hero's ability struck during the boss reveal; it now waits with every AttackController (scene and simulation);
- a tap on UI, or a paused game, skipped the reveal;
- the HUD named the forge on the walk to the next marked hall;
- a pit death named the last enemy that hit the hero; it now names the pit (telemetry `killed_by:pit_eruption`).
And: the autopilot no longer lures the swarm to a pit that is not running (scene and simulation); the HUD's wave line
counts a swarm room's ladder; RewardService forgets a paid kill once its body leaves the floor; the level-up choice
panel is pre-warmed at load (the first level-up was the probe's 345 KB frame).

Still the owner's: the push, the phone (APK install, S23 frame time, the owner's play), a third floor for 13-16 min,
and docs/46's Riftblade questions.
