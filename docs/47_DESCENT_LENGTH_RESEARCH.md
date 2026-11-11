# 47 - The Descent's length and the heal before the finale (research, 2026-11-11)

Simulation only. No runtime script, scene, asset or setting changed. Two findings of docs/41 ("Status 2026-11-11: the
foundry's finale in the simulation") wait for the owner:

1. Nothing heals the hero between floor 2's gate (the Warden) and the Final Swarm. Every finale death entered under a
   fifth of its maximum health.
2. The whole foundry Descent runs about 8 minutes against docs/40 section 3.2's 13-16 minute full run. Floor 2 is the
   short one, about 2.2 minutes, because its gate opens as its room clears.

This doc measures a menu of options for both and recommends the smallest set.

## What was built

- `Tests/Support/DescentSimulation.DescentLength.cs`, a new partial with two research levers on the finale's opening,
  both off by default:
  - `FoundryLevers.FinaleHealShare` heals that share of the maximum as the swarm opens.
  - `FoundryLevers.FinaleChestAtArrival` moves the finale's own chest to where the hero arrives. That chest is
    ChestRule's room-0 chest, a quarter heal.
  - `FoundryResult.SwarmHealed` records the heal. The only change to existing code is one call (`FinaleArrival()`) at
    the end of the finale's `BeginSwarm`, and it does nothing at the default levers.
- `Tools/CombatChecks/DescentLengthResearch.cs` (explicit). It builds each option's chain of floors from
  `FoundryFloorOne()`, sets floor 2's knobs (`GateNotBeforeSeconds`, `HallSurges`, `ExtraElites`) and adds a third floor
  where an option has one. It plays the chain through `RunFoundry`'s `floor`, so the shipped floors are never touched.
  The third floor is floor 2's asset again, at one of two tiers:
  - floor 2's own tier;
  - floor 3's tier (ClimbScaling x1.96 health, x1.80 damage, and the boss room's x1.15), with no modifier and its
    Warden held to floor 2's 8.5.

## How it was measured

- **Code.** Main 42e4e7fb with this branch merged in. Since 42e4e7fb the pilot no longer lures the swarm to the pit's
  rim in a swarm room, so the finale's numbers here differ from docs/41's, which were measured on cebf8775.
- **Runs.** Seeds 1-200, both heroes (Vanguard, Ilya), all five autopilot cohorts, played with human hands
  (`PilotRealism` typical).
- **Pooling.** Each cell pools both heroes, so it holds 400 runs.
- **Same runs up to the change.** Floor 1 is identical in every option, and floor 2 is identical in the options that do
  not touch it. So every difference is the option's.

## The options

| Key | What it plays |
|---|---|
| `shipped` | as shipped on 42e4e7fb: floor 2's gate opens as its room clears, no heal, the autopilot's 60 s stay |
| `heal30` / `heal50` / `healfull` | a heal of 30 % / 50 % / 100 % of the maximum as the finale opens (a Mend at the checkpoint, say) |
| `chest` | the finale's own chest (a quarter heal) placed where the hero arrives instead of at (1.8, 1) |
| `gate180` / `gate210` | floor 2's gate opens no earlier than 3:00 / 3:30 on its clock (floor 1's is 3:30) |
| `gate210-chest` | `gate210` + `chest`: the recommendation |
| `f2asf1` | floor 2 with all of floor 1's V0.1 knobs: the gate at 3:30, two surges a hall, one extra elite room |
| `stay90` | the finale's stay is 90 s, docs/40's upper typical bound, instead of 60 s |
| `third2` | a third floor before the finale: floor 2's asset again at floor 2's tier, its gate opening as its room clears |
| `third3` | a third floor: floor 2's asset at floor 3's tier (x1.96 health, x1.80 damage, x1.15 more on the boss room), no modifier, its Warden held to 8.5 |
| `set-a` | `gate210` + `chest` + `stay90`: the most that can be had without new content |
| `set-b` | `gate210` + `third3`, with the third floor's gate at 3:30 too, + `heal50` before the finale |

## Results

The table pools both heroes, so each cohort is 400 runs. Column by column:

- **Descent.** The P10 / P50 / P90 minutes of the runs that walked out of the finale, all cohorts together. The same
  for floor 2 and the finale in seconds (P50).
- **Completed.** Walked out of the finale, of all runs, for each cohort.
- **Finale deaths.** Deaths in the finale, of the runs that reached it, for each cohort.
- **Entering health.** The finale deaths' health as the finale opened, before any heal, as a share of the maximum
  (P50 / highest).
- **Deaths on floor 2+.** Runs that died on floor 2 (or the third floor) and never reached the finale, all cohorts.

Cohorts are always listed as careful / standing / first-card / clumsy / auto-farmer.

| Option | Descent P10 / P50 / P90 (min) | Floor 2 P50 (s) | Finale P50 (s) | Completed, by cohort | Finale deaths (of reached), by cohort | Entering health P50 / max % | Deaths on floor 2+ |
|---|---|---|---|---|---|---|---|
| `shipped` | 7.4 / 7.8 / 8.2 | 128 | 68 | 95% / 78% / 90% / 40% / 1% | 4 (1.0%) / 12 (3.7%) / 14 (3.7%) / 2 (1.2%) / 2 (28.6%) | 12 / 59 | 138 |
| `heal30` | 7.4 / 7.8 / 8.2 | 128 | 68 | 96% / 81% / 94% / 40% / 2% | 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) | - | 138 |
| `heal50` | 7.4 / 7.8 / 8.2 | 128 | 68 | 96% / 81% / 94% / 40% / 2% | 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) | - | 138 |
| `healfull` | 7.4 / 7.8 / 8.2 | 128 | 68 | 96% / 81% / 94% / 40% / 2% | 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) | - | 138 |
| `chest` | 7.4 / 7.8 / 8.2 | 128 | 68 | 96% / 81% / 94% / 40% / 2% | 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) | - | 138 |
| `gate180` | 9.3 / 9.6 / 9.9 | 238 | 68 | 95% / 85% / 91% / 44% / 3% | 2 (0.5%) / 1 (0.3%) / 9 (2.4%) / 0 (0.0%) / 1 (8.3%) | 12 / 23 | 101 |
| `gate210` | 9.8 / 10.1 / 10.5 | 268 | 68 | 96% / 86% / 90% / 44% / 2% | 1 (0.3%) / 3 (0.9%) / 2 (0.5%) / 0 (0.0%) / 1 (9.1%) | 11 / 26 | 105 |
| `gate210-chest` | 9.8 / 10.1 / 10.5 | 268 | 68 | 96% / 87% / 91% / 44% / 3% | 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) | - | 105 |
| `f2asf1` | 9.8 / 10.1 / 10.4 | 267 | 68 | 93% / 84% / 85% / 40% / 2% | 0 (0.0%) / 0 (0.0%) / 7 (2.0%) / 0 (0.0%) / 0 (0.0%) | 11 / 18 | 168 |
| `stay90` | 7.9 / 8.3 / 8.7 | 128 | 99 | 86% / 64% / 62% / 36% / 1% | 41 (10.7%) / 69 (21.3%) / 125 (33.4%) / 18 (11.2%) / 4 (57.1%) | 33 / 80 | 138 |
| `third2` | 9.6 / 10.1 / 10.7 | 128 | 68 | 94% / 73% / 87% / 36% / 0% | 1 (0.3%) / 3 (1.0%) / 15 (4.1%) / 0 (0.0%) / 0 (0.0%) | 10 / 28 | 204 |
| `third3` | 9.6 / 10.1 / 10.8 | 128 | 68 | 91% / 66% / 82% / 32% / 0% | 3 (0.8%) / 4 (1.5%) / 8 (2.4%) / 0 (0.0%) / 0 (0.0%) | 11 / 21 | 287 |
| `set-a` | 10.3 / 10.6 / 11.0 | 268 | 98 | 96% / 87% / 87% / 44% / 3% | 0 (0.0%) / 2 (0.6%) / 15 (4.1%) / 0 (0.0%) / 0 (0.0%) | 33 / 49 | 105 |
| `set-b` | 14.2 / 14.6 / 15.1 | 268 | 68 | 95% / 84% / 86% / 41% / 1% | 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) / 0 (0.0%) | - | 158 |

The per-cohort tables (reached, walked out, every death's entering health, floor times, the floors' clear rates, the
finale's entering health P10 / P50, the chest) are in `descent-length.md` of the run. The five heal options, shipped
included, play identical floors 1 and 2.

### 1. The heal before the finale

**Shipped on 42e4e7fb, 34 of the 1,248 finales reached end in a death (2.7 %).**
- 30 of the 34 entered under a quarter of their maximum. Of the 119 heroes that entered under 25 %, 30 died; of the
  859 that entered at 50 % or more, 1 did.
- Entering health of the dead: 0-59 %, P50 12 %. The 59 % death is the first-card Ilya, seed 98.
- Grunts dealt 32 of the 34 killing blows and Cinder Mites 2. 29 heroes fell fighting and 5 after turning for the
  exit.
- This is more than docs/41's 19 with human hands. The likely cause is main 42e4e7fb's pilot, which no longer lures the
  swarm to the pit's rim. It is the one change on the finale's path since that measurement, which was not split out
  here.
- The pilot now opens the room's chest at (1.8, 1) in 88-93 % of finales (docs/41: under 1 %). None of the 34 dead
  opened it: they fell before they got there.

**Every heal option removes every finale death, in every cohort:**
- `heal30`, `heal50` and `healfull` (0 of 1,248);
- `chest`: the room's own quarter-heal chest placed where the hero arrives (0 of 1,248, opened in 100 %).

None of them changes the time: the Descent is P50 7.8 min with or without.

**The smallest is the chest.** It is the heal the room already has, moved to the arrival spot, so it adds no new
system and no new number: ChestRule's room-0 heal at `ChestSpawner`'s spot for the finale. A 30 % Mend at the
checkpoint does the same with a new rule.

Neither changes who reaches the finale. The deaths before it are floor 2's: 138 runs die on floor 2 with any heal
option.

### 2. Floor 2's length

Floor 2 is short because its gate opens as its room clears: P50 128 s against floor 1's 270 s.

- **`gate180`** makes floor 2 238 s and the Descent P50 9.6 min.
- **`gate210`** (3:30, as floor 1) makes floor 2 268 s and the Descent P50 10.1 min (P10 9.8, P90 10.5).
- **Both make the run safer, not harder.** The longer floor's horde farm gives levels before the Warden:
  - fewer runs die on floor 2: 101 and 105 against 138;
  - fewer die in the finale: 13 and 7 of about 1,280 against 34 of 1,248;
  - completion rises for the standing (78 % to 86 %) and clumsy (40 % to 44 %) cohorts.
- **`f2asf1`** (the gate at 3:30 plus floor 1's two surges a hall and extra elite room) is no longer than `gate210`
  (267 s) and kills more on floor 2 (168), so the surges and the elite add risk without adding minutes. The gate alone is
  the lever.

### 3. The finale's stay and a third floor

- **A longer stay is a cliff, and buys half a minute.** With `stay90` the finale takes 99 s and the Descent P50 8.3
  min. But 257 of the 1,248 finales end in a death:
  - careful 11 %, standing 21 %, first-card 33 %, clumsy 11 %;
  - the dead entered at up to 80 % of their maximum (P50 33 %).
  On 42e4e7fb, without the rim lure, the swarm past 60 s is harder than docs/41's stay research read on cebf8775.
  docs/40's 45-90 s "typical" stay therefore needs the swarm's ramp, or a reward for staying, looked at before anything
  asks a player to stay 90 s. The autopilot's 60 s should not be raised to lengthen the run.
- **With the chest and floor 2's gate, 90 s is survivable** (`set-a`): 17 finale deaths in 1,281 (0 / 0.6 / 4.1 / 0 % for
  careful / standing / first-card / clumsy). The farm's levels and the quarter heal carry it. The Descent is P50 10.6 min.
  That is the most this measurement finds without new content.
- **A third floor (`third3`, floor 2's asset at floor 3's tier) adds about 141 s with its gate as the room clears**, so
  the Descent is P50 10.1 min. It costs clears:
  - the third floor is cleared by 96 % careful, 82 % standing, 90 % first-card and 81 % clumsy of those that entered it;
  - completion drops from 95 / 78 / 90 / 40 % to 91 / 66 / 82 / 32 %;
  - 287 runs die on floors 2-3, against 138.
  A third floor at floor 3's tier is new content with its own tuning, not a free length lever.

- **The same third floor at floor 2's tier (`third2`) adds about 137 s and the same 10.1 min.** It costs fewer clears:
  - the third floor is cleared by 99 / 91 / 97 / 91 % of those that entered it;
  - completion is 94 / 73 / 87 / 36 %;
  - 204 runs die on floors 2-3.
  Against shipped's 138, `third3` adds 149 deaths and `third2` adds 66. So floor 3's tier step accounts for more than half
  of the third floor's cost, and a floor of any tier still costs some runs.

- **Three 4.5-minute floors and the finale land in 13-16 (`set-b`).** That is floor 2's gate at 3:30, a third floor at
  floor 3's tier with its gate at 3:30, and a 50 % heal before the finale. Every floor takes P50 264-274 s, and the
  Descent P50 14.6 min (P10 14.2, P90 15.1).
  - Every completed run of every cohort lands within 13-16 minutes.
  - Completion is 95 / 84 / 86 / 41 %, against shipped's 95 / 78 / 90 / 40 %.
  - No hero dies in the finale. 158 runs die on floors 2-3, against shipped's 138 on floor 2.
  - With its gate held to 3:30, floor 3 is cleared by 94-99 % of those that entered it, against 81-96 % for `third3`. The
    wait's farm pays for floor 3's tier as it does for floor 2's.

## The reading

**The heal: put the finale's chest where the hero arrives.**
- It takes the finale's deaths from 34 to 0 in every cohort and costs no time.
- It is the smallest change on the menu. The room already has the chest and its quarter heal, and only its spot moves.
  ChestRule.SpotFor is room-indexed, so the change is the swarm room's spot: the hero's arrival, the gate landing.
- A 30 % heal at the checkpoint does as well, but it is a new rule.
- A full heal buys nothing more than 30 % does.

**The length: floor 2's gate at 3:30, as floor 1's.**
- It is one asset value: Floor_QuicksilverVaults' `_foundryGateNotBeforeSeconds`, absent (0) today and 210 proposed, as
  Floor_EmberHalls sets it.
- It brings floor 2 into docs/40's 4-5 minutes (268 s) and the Descent from P50 7.8 to 10.1 min.
- It makes the run safer: fewer deaths on floor 2 (105 against 138) and in the finale (7 against 34), with the same or
  better completion in every cohort.
- With the chest added, `gate210-chest` measures:
  - the Descent at P50 10.1 min (P10 9.8, P90 10.5);
  - no finale death in any cohort;
  - completion of 96 / 87 / 91 / 44 % (careful / standing / first-card / clumsy), against shipped's 95 / 78 / 90 / 40 %.
- 3:00 is a step on the same line (9.6 min). Floor 1's surges and extra elite on floor 2 add deaths and no minutes.

**13-16 minutes is out of reach without new content.**
- Without new content, the most measured is 10.6 min (`set-a`). It needs a 90 s stay that, without the chest and the
  farm's levels, kills 11-33 % of each cohort's finales.
- Raising the stay is not the lever: the swarm past 60 s is a cliff on 42e4e7fb.
- docs/40's run is three 4-5 minute floors and a 45-90 s finale, and the foundry has two floors before its finale.
- A third floor at floor 3's tier with its gate at 3:30 (`set-b`) lands every completed run in 14.2-15.1 minutes at
  about today's completion.
- That floor is new content: a third floor asset, its modifier, its Warden's share and its own tuning. This research
  plays floor 2's asset again as a stand-in, so its numbers are a shape, not a tuned floor.

**Recommendation.** Build the smallest set now:
1. the finale's chest at the hero's arrival;
2. floor 2's gate at 3:30.

That gives a Descent of about 10 minutes with no near-dead finale deaths. Reaching 13-16 then waits for the owner's
call on a third floor. If one is built, give it the same 3:30 gate and the heal before the finale; `set-b` measures that
shape. Do not lengthen the finale's stay to buy time.

## The default batch is unchanged

The levers are off by default, and the default batch (FOUNDRY_FLOORS=2, the command below) does not move.
- **The two runs.** The batch ran first on main 2c620d21, before any change on this branch, and again on this branch
  merged with main 42e4e7fb.
- **Byte-identical**, by first 12 hex of their sha256:
  - foundry-batch.csv (67850cacf122)
  - foundry-batch-floor2.csv (8e2d1923b604)
  - foundry-cohorts-floor2.csv (528a63ce5bf8)
  - foundry-gate.md (588e02b95d7a)
  - foundry-cohorts.md (abbadd898356)
- **foundry-batch.md** differs only in its date line and its "Wall s" column.
- **What it confirms.** Neither this branch's levers nor 42e4e7fb's swarm-room lure change floors 1 and 2.

## Reproduce

```
# the options (each option's table is rewritten after it finishes; DESCENT_OPTIONS picks a subset)
FOUNDRY_SEEDS=200 FOUNDRY_PARALLEL=10 FOUNDRY_OUT=<dir> \
DESCENT_OPTIONS=shipped,heal30,heal50,healfull,chest,gate180,gate210,gate210-chest,f2asf1,stay90,third2,third3,set-a,set-b \
  dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~DescentLengthResearch"

# the default batch, byte-identical with the levers off
FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=1 \
FOUNDRY_OUT=<dir> dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false \
  --filter "FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"
```

`DESCENT_COHORTS` (all five by default) and `DESCENT_REALISM` (`typical` by default; also `frame-perfect`, `slow`)
narrow or widen the research. It writes `descent-length.md` (the per-cohort tables) and `descent-length.csv` (a row per
run).

## Status 2026-11-11 (evening): built

The owner delegated the gameplay calls, and the lead took the recommended pair. Both are built, in the scene and the
simulation together.

**What was built.**
- **Floor 2's gate at 3:30.** Floor_QuicksilverVaults sets `_foundryGateNotBeforeSeconds: 210`, as Floor_EmberHalls
  does. It is one line in the asset; its formatting and GUIDs are unchanged.
  - The simulation's `FoundryFloorTwo()` mirrors it (`GateNotBeforeSeconds = 210f`).
  - `FoundrySimulationSceneValueTests.TheFoundrysSecondFloorIsTheAsset` reads 210.
  - Two simulation tests that held the old open gate now read the new hold.
    `FoundrySimulationTests.AFloorOneClearGoesDownToFloorTwoAndPlaysItsRoomsInTheScenesOrder` checks that floor 2's
    Warden walks out at the floor's clock 210 s or later.
    `FoundryGateCallTests.FloorTwoNeverHoldsSoNeverCalls` becomes `FloorTwoHoldsSoTheCallReachesIt`.
  - The PlayMode shipped climb already starts each floor's clock at 215 s, so floor 2's wait is skipped there as floor
    1's is.
- **The finale's chest where the hero arrives.**
  - `ChestSpawner.OnEncounterStarted` places a foundry swarm room's chest at the hero's floor position as the swarm
    opens, instead of `ChestRule.SpotFor`. That chest is room 0's quarter heal.
  - Every other room is unchanged, and so is the hall arena's swarm room, so the hall Descent's parity stays.
  - The simulation's `FoundryLevers.FinaleChestAtArrival` is now on by default.
  - `FoundryDescentFlowTests.TheShippedClimbPlaysThroughToTheEndOfTheFinalSwarm` asserts on the finale's first frame
    that the chest stands within `ChestRule.PickupRadius` of the hero, and then that it opens.
  - `DescentLengthResearch` now sets every option's floor 2 gate and chest explicitly, so its `shipped` row stays the
    pre-build floors and its tables above still reproduce.

**The default batch after the build** (seeds 1-200, FOUNDRY_LEVERS=0, FOUNDRY_COHORTS=1, FOUNDRY_REALISM=typical),
against the same batch on the pre-build branch:
- **foundry-gate.md: no row moved**, with FOUNDRY_FLOORS=2 or 3. The gate reads floor 1, which neither change touches.
  foundry-batch.csv is byte-identical too.
- **Floor 2** (foundry-cohorts.md's floor-2 section, FOUNDRY_FLOORS=2):
  - its cleared P50 goes from 123-136 s to 263-274 s (the auto-farmer's from 89-94 s to 255 s);
  - the level at its gate goes from 31-33 to 40-41;
  - cleared floor 2, of those that went down, Vanguard / Ilya: careful 98 / 98 % to 99 / 97 %, standing 88 / 89 % to
    96 / 93 %, first-card 97 / 99 % to 95 / 97 %, clumsy 77 / 77 % to 86 / 82 %;
  - deaths on floor 2 across both heroes: careful 8 to 8, standing 44 to 19, first-card 6 to 16, clumsy 48 to 34,
    auto-farmer 32 to 28.
  - The first-card cohort is the one that loses runs on floor 2, 10 more (Forge Warden and horde blows).
- **The finale** (foundry-finale.md, FOUNDRY_FLOORS=3):
  - deaths go from 34 (2 / 2 / 5 / 7 / 3 / 11 / 0 / 2 / 1 / 1, by cohort and hero) to 0 in every row;
  - every run that reached it walks out;
  - the hero enters at level 42-45 (was 34-38).
- **The whole Descent:** P50 goes from 7.6-8.0 min to 9.9-10.3 min (P90 up to 10.7). No run reaches 13 minutes; that
  still waits for a third floor (section 3, `set-b`).

**Checks.**
- CombatChecks: 780 of 780 green after the two tests were updated. Before the update, exactly those two failed.
- Unity, not run here (no Unity in the worktree):
  - EditMode: `FoundrySimulationSceneValueTests;ClimbFloorContentTests;FoundrySimulationTests;FoundryGateCallTests;FoundryFinaleTests;ChestRuleTests`
  - PlayMode: `FoundryDescentFlowTests;ArenaWalkTests;FoundryRegionTests;GateHoldFlowTests;FoundryRunEndFlowTests;PaintedHudFlowTests`

## The first-card cohort on floor 2 (research 2026-11-11 night)

Simulation only. No runtime script, scene, asset or setting changed. With floor 2's gate at 3:30 (the build above), floor
2 got safer for every cohort but one: the first-card cohort's floor-2 deaths went from 6 to 16. This section finds out
why, and measures four small floor-2 levers against it.

**What was built.**
- `Tests/Support/DescentSimulation.FirstCardFloorTwo.cs`, a partial with two research levers. Both act on the floors
  after the first only, and both are off by default:
  - `FoundryLevers.LaterHordeClockCap`. A horde enemy that walks in after the floor's clock passes the cap reads
    HordeClock's health, damage and experience at the cap. The horde stops hardening there, and that floor's overrun
    never comes.
  - `FoundryLevers.LaterGateChestHeal`. The gate room's own chest holds a heal of this share of the maximum, and it
    stands where the hero is as the Warden walks out (today it is a gold chest at ChestRule's spot).
  - The changes to existing code: `SpawnHordeFoe` reads the horde's growth through `HordeHealthNow`, `HordeDamageNow`
    and `HordeClockNow`, which return the director's own values at the default levers; and `GateBossArrived` ends with
    one call, `GateArrival()`, which does nothing at the default levers.
- Floor 2's Warden share and its Deep Forge's Mend already had levers (`LaterWardenHealthShare`, `LaterMendAmount`).
- `Tools/CombatChecks/FirstCardFloorTwoResearch.cs` (explicit). It plays every cohort and both heroes down the whole
  Descent (floor 1, floor 2, the finale) on the same seeds. Its options are: as shipped; floor 2's gate as its room clears
  (`gate0`, floor 2 before the build); and each candidate, set on floor 2 alone. Floor 1 is the same run in every
  option.

**How it was measured.** Main 4487316e with this branch. Seeds 1-200, both heroes (Vanguard, Ilya), all five cohorts,
human hands (`PilotRealism` typical). The research's `shipped` row reproduces the default batch's floor-2 section: 10 /
6 first-card deaths (Vanguard / Ilya), 16 in all. Its `gate0` row measures 7 on this main, against the 6 the pre-build
batch read.

### Why the first-card cohort dies on floor 2

Floor-2 deaths by hero, before the build (`gate0`) and as shipped. "At the Warden": the deaths after it walked out, with
the seconds into its fight and the killing blows (the Warden's strike or slam, the horde, the pit).

| Cohort | Hero | Deaths: `gate0` -> shipped | At the Warden: `gate0` -> shipped | Shipped: seconds into the Warden's fight | Shipped: killing blows at the Warden |
|---|---|---|---|---|---|
| careful | Vanguard | 3 -> 4 | 3 -> 4 | 36, 39, 42, 51 | Warden 2, horde 2 |
| careful | Ilya | 4 -> 6 | 2 -> 4 | 18, 29, 32, 36 | Warden 3, pit 1 |
| standing | Vanguard | 21 -> 7 | 21 -> 7 | 28-40 | Warden 6, pit 1 |
| standing | Ilya | 22 -> 12 | 18 -> 8 | 22-48 | Warden 4, horde 3, pit 1 |
| first-card | Vanguard | 6 -> 10 | 4 -> 8 | 14, 24, 27, 34, 34, 38, 40, 44 | Warden 4, horde 3, pit 1 |
| first-card | Ilya | 1 -> 6 | 0 -> 5 | 18, 24, 32, 38, 41 | Warden 4, horde 1 |

At floor 2's gate, both heroes pooled, the runs whose Warden walked out (P50 or mean as marked). "Difficulty 1.8+": the
run's difficulty as the Warden walked out was 1.8 or more, an evolution above the floor's usual 1.6 (each evolution adds
0.2, each altar 0.1, and the difficulty multiplies the Warden's, the halls' and the horde's health and damage).
"Low maximum": the hero's maximum health at most 1.5 times its base (150 for the Vanguard, 135 for Ilya).

| Cohort | Option | Reached the Warden | Died there | Level P50 | Clock P50 (s) | Maximum health mean | Health at the gate P50 | Difficulty 1.8+ (died) | Low maximum (died) | Vital Surge / Vitality levels / Iron Skin (mean) | Evolutions mean | Horde's damage in the fight, mean |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| careful | `gate0` | 391 | 5 | 33 | 70 | 217 | 93 % | 109 (1) | 73 (1) | 1.9 / 4.5 / 1.2 | 1.23 | 6.5 |
| careful | shipped | 391 | 8 | 41 | 210 | 254 | 90 % | 156 (6) | 44 (0) | 2.9 / 4.9 / 1.9 | 1.57 | 10.2 |
| standing | `gate0` | 366 | 39 | 32 | 71 | 225 | 91 % | 89 (18) | 51 (7) | 1.9 / 4.9 / 1.2 | 1.14 | 4.9 |
| standing | shipped | 366 | 15 | 40 | 210 | 262 | 90 % | 135 (2) | 29 (1) | 2.8 / 5.5 / 1.7 | 1.46 | 7.9 |
| first-card | `gate0` | 377 | 4 | 33 | 67 | 171 | 90 % | 37 (1) | 123 (2) | 1.5 / 3.0 / 1.5 | 0.89 | 5.1 |
| first-card | shipped | 377 | 13 | 41 | 210 | 193 | 87 % | 119 (9) | 94 (6) | 2.0 / 3.6 / 1.9 | 1.36 | 9.2 |

**Where and when.** 13 of the first-card cohort's 16 floor-2 deaths are at the Warden: 14-44 s into its fight, at floor
2's clock 224-254 s. The other 3 die in the halls (Vanguard seeds 165 and 173, Ilya seed 57), and they die the same way
before the build, which reaches the halls the same. The killing blows at the Warden: the Warden's strike or slam 8, the
horde 4 (Grunts and Runners), the pit 1. No cohort's Warden fight on floor 2 reaches 300 s, and no hero dies at or past
the overrun.

**The ramp at 3:30.** The Warden walks out at floor 2's clock 210 s, where before the build it walked out at about 70 s.
- The horde that fights beside it is the 3:30 horde: HordeClock's x1.35 damage and x1.70 health at 210 s, rising to
  x1.42 and x1.83 by 250 s, against x1.12 and x1.23 at 70 s. In the boss state the horde keeps its plain crowd of 6
  (HordeRule.EffectiveTarget), so it grows in strength, not in numbers.
- What the horde dealt the hero in the Warden's fight went up by half or more in every cohort (first-card 5.1 to 9.2
  mean, careful 6.5 to 10.2).
- The overrun's steps and its experience cut-off (HordeClock.PaysExperience) never bite on floor 2. SwarmRule's ramp is
  the finale's (one step every 3 s at 1.02); it does not touch floor 2.

**The difficulty the wait buys.** The wait's farm gives about 8 levels (33 to 40-41), and with them evolutions. The share
of Warden fights at difficulty 1.8+ rises in every cohort. First-card goes from 37 to 119 of 377, and those fights kill
9 of its 13. A difficulty of 1.8 against 1.6 is a Warden with 4,590 health against 4,080 and a strike an eighth harder
(its slam stays a fifth of the hero's maximum, WardenRule.HeroShare), beside a horde an eighth stronger too.

**The build.** The first-card cohort takes whatever card comes first, so the wait's levels buy it less health than they
buy the others.
- Its maximum health grows by 22 (171 to 193 mean) where the careful cohort's grows by 37 (217 to 254) and the standing
  cohort's by 37 (225 to 262).
- By the gate it holds 2.0 Vital Surges and 3.6 Vitality levels, against the careful cohort's 2.9 and 4.9. Armour is
  level: 1.9 Iron Skins in both.
- 94 of its 377 Warden fights start with a low maximum (careful 44), and 6 of those die.
- Weapons are not it. Every cohort holds three weapons at or near level 8, and three tomes, at the gate. The first-card
  cohort's tomes are Force, Tempo, Precision and Reach more often than Vitality.

**Against the careful and standing cohorts.** The longer floor makes the world harder for everyone at the Warden: an
evolution more, and the 3:30 horde. The cohorts whose picker spends the extra levels on health come out ahead:
- standing, which takes the Warden's blows without kiting, halves its Warden deaths (39 to 15);
- careful stays rare (5 to 8 at the Warden, within the noise of 391 fights).

The first-card cohort spends the same levels at random and comes out behind (4 to 13 at the Warden).

### The levers

Each option is the shipped Descent with one change on floor 2. Floor 1 is the same run in every option: of the 2,000
runs, none differs in its floor-1 outcome, health, level or evolutions across the eight options.

| Key | What it plays on floor 2 |
|---|---|
| `shipped` | as shipped: the gate at 3:30 (the finale's chest where the hero arrives) |
| `gate0` | the gate as its room clears, floor 2 before the build |
| `warden85` / `warden75` | the Warden at 85 % / 75 % of its health (`LaterWardenHealthShare` 5.160715 / 4.553572 against the asset's 6.071429) |
| `horde180` / `horde210` | the horde stops hardening at 3:00 / 3:30 on the floor's clock (`LaterHordeClockCap`; no overrun) |
| `mend70` | the Deep Forge's Mend heals 70 % instead of Forge_Mend's 40 % (`LaterMendAmount`; every cohort takes the Mend) |
| `gatechest` | the gate room's chest a quarter heal under the hero's feet as the Warden walks out (`LaterGateChestHeal` 0.25) |

Floor-2 deaths, Vanguard / Ilya, with floor 2's clears of those that went down in brackets:

| Option | careful | standing | first-card | clumsy | auto-farmer |
|---|---|---|---|---|---|
| `shipped` | 4 / 6 (98 / 97 %) | 7 / 12 (96 / 93 %) | 10 / 6 (95 / 97 %) | 12 / 24 (85 / 82 %) | 7 / 21 (30 / 28 %) |
| `gate0` | 3 / 4 (98 / 98 %) | 21 / 22 (89 / 88 %) | 6 / 1 (97 / 99 %) | 20 / 31 (74 / 77 %) | 9 / 23 (10 / 21 %) |
| `warden85` | 3 / 5 (98 / 97 %) | 5 / 11 (97 / 94 %) | 6 / 5 (97 / 97 %) | 11 / 23 (86 / 83 %) | 6 / 18 (40 / 38 %) |
| `warden75` | 1 / 4 (99 / 98 %) | 5 / 8 (97 / 96 %) | 6 / 4 (97 / 98 %) | 11 / 22 (86 / 83 %) | 6 / 17 (40 / 41 %) |
| `horde180` | 3 / 4 (98 / 98 %) | 7 / 11 (96 / 94 %) | 7 / 5 (96 / 97 %) | 13 / 23 (83 / 83 %) | 6 / 21 (40 / 28 %) |
| `horde210` | 4 / 5 (98 / 97 %) | 8 / 12 (96 / 93 %) | 8 / 5 (96 / 97 %) | 12 / 24 (85 / 82 %) | 7 / 21 (30 / 28 %) |
| `mend70` | 0 / 4 (100 / 98 %) | 3 / 7 (98 / 96 %) | 3 / 2 (98 / 99 %) | 8 / 17 (90 / 87 %) | 5 / 14 (50 / 52 %) |
| `gatechest` | 0 / 3 (100 / 98 %) | 1 / 7 (99 / 96 %) | 4 / 2 (98 / 99 %) | 8 / 20 (90 / 85 %) | 5 / 16 (50 / 45 %) |

The times, all cohorts pooled, and floor 2's Warden fight by cohort (P50 s, Vanguard / Ilya; docs/40 section 9.6's band
is 45-65 s). Completed: walked out of the finale, of all runs, for the first-card and careful cohorts.

| Option | Floor 2 P50 (s) | Descent P50 (min) | Finale deaths (of reached) | Warden: careful | standing | first-card | clumsy | Completed: first-card / careful |
|---|---|---|---|---|---|---|---|---|
| `shipped` | 268 | 10.1 | 0 of 1,283 | 51 / 45 | 40 / 38 | 48 / 45 | 43 / 44 | 86 / 96 %, 96 / 96 % |
| `gate0` | 128 | 7.7 | 0 of 1,252 | 53 / 48 | 39 / 37 | 51 / 47 | 45 / 45 | 88 / 98 %, 96 / 97 % |
| `warden85` | 264 | 10.0 | 0 of 1,299 | 46 / 42 | 37 / 35 | 44 / 41 | 39 / 41 | 88 / 96 %, 96 / 96 % |
| `warden75` | 262 | 10.0 | 0 of 1,308 | 43 / 39 | 35 / 33 | 40 / 38 | 36 / 39 | 88 / 97 %, 97 / 97 % |
| `horde180` | 268 | 10.1 | 0 of 1,292 | 50 / 46 | 40 / 38 | 47 / 45 | 42 / 44 | 88 / 96 %, 96 / 97 % |
| `horde210` | 268 | 10.1 | 0 of 1,286 | 50 / 45 | 40 / 38 | 48 / 45 | 43 / 44 | 87 / 96 %, 96 / 96 % |
| `mend70` | 268 | 10.1 | 1 of 1,329 | 51 / 45 | 40 / 38 | 48 / 45 | 42 / 44 | 89 / 98 %, 98 / 97 % |
| `gatechest` | 268 | 10.1 | 1 of 1,326 | 51 / 45 | 40 / 38 | 47 / 45 | 43 / 44 | 88 / 98 %, 98 / 98 % |

The one finale death in `mend70` and `gatechest` is the same run: the first-card Vanguard of seed 95, which dies at
floor 2's Warden as shipped, survives it and falls in the finale.

**No lever is targeted at the first-card cohort.** Every lever that lowers its deaths lowers the careful cohort's about
as much in proportion, because the careful cohort's few floor-2 deaths are the same kind: Warden fights at difficulty
1.8+ after a costly wait.
- **The Warden's share** (`warden85`: first-card 16 to 11, careful 10 to 8) shortens careful Ilya's fight to 42 s, out of
  the 45-65 s band; `warden75` takes it to 39 s and the Vanguard's to 43 s.
- **The horde's cap** keeps the Warden's fight in the band but buys little. `horde180` takes first-card from 16 to 12 and
  careful from 10 to 7. `horde210`, the cap at the Warden's walk-out, buys 3, within the noise: in a 40-50 s fight the
  horde only grows from x1.35 to x1.42 damage. The ramp past 3:00 is not what kills.
- **The heals** take first-card back to its pre-build rate (`mend70` 5, `gatechest` 6). They leave the fight's length and
  the Descent's 10.1 minutes as they are. But they also take careful from 10 to 3-4, standing from 19 to 8-10 and clumsy
  from 36 to 25-28, and careful's floor-2 clears from 98 / 97 % to 100 / 98 %. Floor 2 gets easier for everyone, careful
  included.

### The reading

**Is 16 within docs/40?** Yes. docs/40 section 10.2's bands are a whole run's win rates by difficulty mode, as docs/41
read them on 2026-11-11, and the easiest, Apprentice, is 65-80 %. As shipped, the first-card cohort walks out of the
finale in 86 % (Vanguard) and 96 % (Ilya) of its runs, against 88 / 98 % before the build. That is above every docs/40
band, as the careful cohort's 96 / 96 % is. Its 9 extra floor-2 deaths are 2.4 % of its 377 Warden fights and cost it 2
points of completion. Of the playing cohorts only the clumsy one (33 / 54 %) sits inside bands: Tempered's 25-40 % for
the Vanguard and Forged's 40-60 % for Ilya.

**Why the rise.** It is the cohort's build, not floor 2's length. The 3:30 gate gives every cohort about 8 more levels,
an evolution's difficulty more and the 3:30 horde at the Warden. The careful picker spends the levels on health and comes
out ahead. The first-card picker spends them at random, so it meets that harder Warden with 22 more health where the
careful pilot brings 37. The overrun, the horde's experience cut-off and SwarmRule's ramp play no part.

**Recommendation: none.** Keep floor 2 as shipped.
- The first-card cohort loses a little more on the longer floor, as the weaker hand should, and its run stays above
  docs/40's bands.
- No floor-2 lever reaches it without reaching the careful cohort. The Warden's share breaks the 45-65 s band, and the
  horde's cap is noise.
- If the owner wants the first-card cohort's pre-build rate back anyway, `mend70` or `gatechest` does it with no change
  to the Warden's band or the Descent's length. Either makes floor 2 easier for every cohort, careful included, so it is a
  floor-2 difficulty decision, not a first-card fix.
  - `mend70` needs a floor-2 Mend of its own, as Forge_Mend serves both floors.
  - `gatechest` changes the gate room's gold chest into a heal at the hero's feet, the finale chest's rule.

### The default batch is unchanged

The levers are off by default. The default batch (FOUNDRY_FLOORS=2, the command below) ran on main 4487316e and again on
this branch, with the same environment:
- byte-identical, by first 12 hex of their sha256:
  - foundry-batch.csv (f19890670d51)
  - foundry-batch-floor2.csv (f62b2231ff59)
  - foundry-cohorts-floor2.csv (7127077ed18b)
  - foundry-gate.md (47a518a75d5f)
  - foundry-cohorts.md (b309eba5c068)
- foundry-batch.md differs only in its date line and its "Wall s" column.

CombatChecks' foundry suites (`FoundrySimulationTests`, `FoundryGateCallTests`, `FoundryFinaleTests`, `FoundryGateTests`,
`ChestRuleTests`, `HordeDirectorTests`): 63 of 63 green on this branch.

### Reproduce

```
# the research (FIRSTCARD_OPTIONS picks a subset; every option by default). Writes first-card-floor2.md and a row a run's
# floor 2 in first-card-floor2.csv, both rewritten after each option
FOUNDRY_SEEDS=200 FOUNDRY_PARALLEL=11 FOUNDRY_REALISM=typical FOUNDRY_OUT=<dir> \
FIRSTCARD_OPTIONS=shipped,gate0,warden85,warden75,horde180,horde210,mend70,gatechest \
  dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false --filter "FullyQualifiedName~FirstCardFloorTwoResearch"

# the batch's cohorts down the whole Descent: its floor-2 section is the research's `shipped` rows
FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=3 FOUNDRY_REALISM=typical FOUNDRY_PARALLEL=1 \
FOUNDRY_OUT=<dir> dotnet test Tools/CombatChecks -c Release -p:NuGetAudit=false \
  --filter "FullyQualifiedName~FoundryBatchReport.TheFoundryFloorOverASeedBatch"

# the default batch, byte-identical with the levers off: the same with FOUNDRY_FLOORS=2
```

The research plays each cohort through `FoundryCohorts.Levers(cohort, 3, realism)`, the batch's own levers for
FOUNDRY_FLOORS=3. Its `shipped` rows match the default batch's foundry-cohorts.md floor-2 section on 4487316e in every
cohort's went-down, clears, deaths and Warden fight. That batch reads careful 10 and clumsy 36 floor-2 deaths, where the
evening status above read 8 and 34.
