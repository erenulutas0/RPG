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
- This is more than docs/41's 19 with human hands. Since 42e4e7fb the pilot no longer drags the swarm to the pit's rim,
  so it stands and fights in the open.
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

The levers are off by default, so the default batch (FOUNDRY_FLOORS=2, the command below) should not move. It is checked
against main 2c620d21's, run before any change on this branch. The comparison is in progress:
foundry-batch-floor2.csv is byte-identical (sha256 8e2d1923...).

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
