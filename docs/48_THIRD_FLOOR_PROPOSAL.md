# 48 - A third floor for the foundry Descent (proposal, 2026-11-12)

A proposal for the owner's decision. Nothing here is built: no code, asset, scene or setting changed. Every number cited
as "measured" comes from docs/47 or docs/41 and names its row. Every other number is a proposal for the simulation to
measure, not a decision.

**Scope.** V0.1 is one floor (docs/40 section 15.1). A third floor is docs/40's phase 2, "Full Run" (section 15.2), so
it is post-V0.1 work. Like docs/46 (the Riftblade), building it should wait for the V0.1 gate's device row: the S23's
frame time at the chosen crowd. The proposal is written now so the owner can decide its shape early.

## 0. The short version

- **Why.** The Descent today is P50 about 10 minutes (floor 1, floor 2, a 65 s Final Swarm). docs/40 section 3.2 wants
  13-16 minutes: three floors of 4-5 minutes and a 45-90 s finale. docs/47 found no way to reach 13 without new content.
- **What.** A third floor between floor 2 and the Final Swarm, built from the parts the foundry already has: the same
  map and art, the same six-room shape, floor 3's tier from ClimbScaling, its gate held to 3:30, and its Warden's health
  share set so the fight lands in 45-65 s.
- **Measured shape.** docs/47's `set-b` (floor 2's asset played again as floor 3, at floor 3's tier, its gate at 3:30,
  a 50 % heal before the finale) gives a Descent of P10 14.2 / P50 14.6 / P90 15.1 minutes. Every completed run of every
  cohort lands inside 13-16. No finale deaths. Completion 95 / 84 / 86 / 41 % (careful / standing / first-card / clumsy).
- **Cost.** The smallest version is data plus tests: one floor asset, one changed line in floor 2's asset, a simulation
  mirror and test updates. About 2-3 working days for an agent, most of it measurement. It adds no renderer and no vertex.
- **Recommendation.** If the owner wants 13-16 minutes, build the data-only floor 3 first, measure it, and only then
  decide on new behaviour (a Warden variant) or new art.

## 1. Floor 3's identity

### 1.1 Name and theme

The narrative bible (docs/44 section 1.6) already names the tower's floors: F1 The Foundry, F2 Quicksilver Vaults, F3
**Gloam Gardens** ("plants growing on ash, moving roots"; boss Rootmother Myrr; enemies Cinder Bloom, Thornling, Pollen
Wraith). Its timeline puts the Dawn Engine's story on floor 3.

There is a precedent for the first build: floor 2 is called Quicksilver Vaults, but it plays on the foundry's map and
art, with foundry enemies and the Forge Warden. Nothing on screen is "vault" except its name and room names. Floor 3 can
start the same way.

Two choices for the owner (question 2):

| Choice | Name | What the player sees | Cost |
|---|---|---|---|
| A (proposed) | **Gloam Gardens**, as docs/44 | The foundry map, foundry enemies, the Forge Warden; garden names on the rooms and the checkpoint | Data only. The garden look, Rootmother Myrr and the garden enemies come later with art |
| B | A foundry name, for example **The Dawn Engine** (docs/44's floor-3 story) | The same | Data only. Fits the art better today, but moves the bible's Gloam Gardens to floor 4 |

Proposed room names for A (the bible's style; the owner may rename): Ash Orchard, Rootbound Walk, Bloom Gate (halls),
The Grafting Forge (forge), Thorn Post (elite), Warden's Grove (gate).

### 1.2 How it plays against floors 1 and 2

docs/40 section 3.2 gives each floor an identity: floor 1 "build foundation / second weapon", floor 2 "elite / main
synergy", floor 3 "evolution / strong boss". The table proposes floor 3 from the knobs that FloorDefinition already has.

| | Floor 1 (Floor_EmberHalls) | Floor 2 (Floor_QuicksilverVaults) | Floor 3 (proposed) |
|---|---|---|---|
| Rooms | 3 halls, The Forge, Captain's Post, Warden's Crucible | 3 halls, The Deep Forge, Sentry Hall, Warden's Vault | 3 halls, forge, elite, gate: the same six-room shape |
| Hall surges | 2 a hall | 0 | 0 (docs/47 `f2asf1`: surges add deaths, not minutes) |
| Extra elite rooms | 1 | 0 | 0 |
| Tier | ClimbScaling floor 1 (health x1, damage x0.92) | floor 2 (x1.4, x1.288) + Cursed Gold (+20 % damage, +50 % gold) | floor 3 (x1.96 health, x1.80 damage, the boss room x1.15 more) |
| Modifier | none | Cursed Gold | none at first (what `set-b` measured); a new modifier is question 6 |
| Horde roles | Mite, Grunt, Runner | Mite, Grunt, Runner | Mite, Grunt, Runner at first; Grunt, Runner, Tank as a measured variant (1.3) |
| Elite | Grunt Captain | Grunt Captain | Grunt Captain, at floor 3's tier |
| Gate boss | Forge Warden, share 8.5 | Forge Warden + 2 Cinder Mites, share 6.071429 (8.5 / 1.4) | Forge Warden + escort, share about 3.77 (8.5 / (1.96 x 1.15)) as the stand-in, then tuned to 45-65 s |
| Gate time | 3:30 | 3:30 (since 2026-11-11) | 3:30 |
| Heal points | Forge Mend (40 %), room chests | Forge Mend (40 %), room chests | Forge Mend (40 %), room chests, and the heal before the finale (1.4) |
| Halls on the map | workyard, pit rim, east court, north court | north court, workyard, pit rim | the district round continues (by the rule today: east court, north court, workyard; to confirm in the test) |

**Enemy roles and tier.** The cheapest identity is the tier itself: floor 3's enemies hit 1.80 times as hard as floor 1's
base and have 1.96 times the health. ClimbScaling was built for this ("floors added above need no hand tuning"), and floor
3 is the first floor where its boss growth (BossFromFloor 3, x1.15) applies in the scene.

### 1.3 A heavier crowd (an option, to measure)

Floors 1 and 2 have the same horde: Mite, Grunt, Runner. Floor 3 could be the floor where the Tank (120 health, slow,
docs/40's "Bulwark" role) joins the horde, with Mites dropping out: Grunt, Runner, Tank. This is a data change
(`_foundryHorde`), and it gives floor 3 a different feel without new art: fewer small bodies, more to route around.

It is not measured. The finale's ladder already brings Tanks in from 1:48, and docs/41 reads Tanks among the killing
blows past 2:00. With floor 3's tier on top, a Tank horde may be the step that turns floor 3 from 94-99 % clears into a
wall. Measure it beside the plain horde (build plan step 1) before choosing.

### 1.4 The Warden variant

- **Health.** The stand-in held floor 3's Warden to floor 2's 8.5 times its authored health (share 8.5 / (1.96 x 1.15),
  about 3.77), as floor 2's share 6.071429 cancels its own x1.4. Floor 3's fight time was not reported in docs/47, so the
  share must be tuned to docs/40 section 9.6's band: P50 45-65 s for both heroes and every playing cohort.
- **Escort.** Floor 2's Warden walks out with two Cinder Mites. Floor 3's could walk out with two Runners (or one Tank),
  a data change in the gate room's wave.
- **New behaviour (optional, costed separately).** docs/40 section 8.9 lists a third boss phase, "minion reinforcement",
  for the strong boss. A floor-3 Warden that calls a short reinforcement wave at half health would be the floor's
  "strong boss" without new art. It is new code in WardenRule and the simulation's Warden, with parity tests. Do it only
  if the data-only Warden reads as "the same boss again" in play (question 5).
- **A real Rootmother Myrr** is a new boss: new art, new attacks, new simulation. Out of scope for this proposal.

### 1.5 Heal points

Today the heals are the forge's Mend (Forge_Mend, 40 %, one asset for both floors), the room chests, and, since
2026-11-11, the finale's quarter-heal chest placed where the hero arrives.

- `set-b` measured a **50 % heal as the finale opens**, not the shipped chest. With the chest alone after a third floor,
  the finale is not measured. docs/47 found the chest and every heal option equal after two floors (34 deaths to 0), so
  the chest may well be enough, but the hero now arrives after one more Warden fight at a higher tier.
- Proposal: keep the shipped chest and measure it after floor 3. If finale deaths come back, add the 30 % Mend at the
  checkpoint that docs/47 measured as equal (`heal30`).
- Floor 3 reuses Forge_Mend's 40 %. A floor-3 Mend of its own is not needed unless floor 3's clears fall below the
  targets in section 2.

## 2. Measured targets

All from docs/47 (seeds 1-200, both heroes, five cohorts, human hands, `PilotRealism` typical). The comparison row is
today's shipped Descent, docs/47's `gate210-chest`.

| | Today (`gate210-chest`) | Stand-in floor 3 (`set-b`) | Proposed acceptance for the real floor 3 |
|---|---|---|---|
| Descent P10 / P50 / P90 (min) | 9.8 / 10.1 / 10.5 | 14.2 / 14.6 / 15.1 | P10 at least 13.0, P90 at most 16.0 |
| Each floor P50 | 270 / 268 s | 264-274 s each | 240-300 s each (docs/40 9.6 "4-6 min", 3.2 "4-5 min") |
| Finale P50 | 68 s | 68 s | 45-90 s (docs/40 3.2) |
| Finale deaths | 0 in every cohort | 0 in every cohort | 0-1 % in every cohort |
| Completed: careful / standing / first-card / clumsy | 96 / 87 / 91 / 44 % | 95 / 84 / 86 / 41 % | no cohort more than 5 points below today |
| Deaths before the finale (floors 2-3) | 105 | 158 | at most about 160 |
| Floor 3 cleared, of those that entered it | - | 94-99 % | careful at least 95 %, clumsy at least 85 % |
| Warden fight P50 | floor 1 and 2 in 45-65 s (docs/41) | floor 3 not reported | floor 3 P50 45-65 s, both heroes, every playing cohort |

**Against docs/40's difficulty bands (section 10.2).** These are whole-run win rates by difficulty mode, the easiest
(Apprentice) at 65-80 %, Forged 40-60 %, Tempered 25-40 %. As docs/47 read them, today's careful, standing and first-card
cohorts sit above every band, and only the clumsy cohort sits inside one (Forged 40-60 %). `set-b` keeps that picture:
the clumsy cohort at 41 % stays at the bottom of Forged, the others above Apprentice. So a third floor at this shape does
not push any cohort out of docs/40's bands. It does take the clumsy cohort close to Forged's lower edge; if the owner
reads the clumsy cohort as "Forged", that is the number to watch.

**What the stand-in does not tell us.** It played floor 2's rooms again at floor 3's tier, with no modifier, the plain
horde, and a 50 % heal. Floor 3's own rooms, a Tank horde, an escort or the chest-only finale can each move these
numbers. They are a shape, not a tuned floor (docs/47's own words).

## 3. What it costs

### 3.1 New content

| Item | Data-only floor 3 | With the options |
|---|---|---|
| Floor asset | Floor_GloamGardens.asset (six rooms inline, tier, gate 210, gate share, horde, no modifier) | a modifier asset if question 6 says yes |
| Floor 2's asset | `_nextFloor` points to floor 3 instead of Floor_FinalSwarm: one line | - |
| Rooms | six RoomDefinitions inside the asset, existing enemies only | - |
| Enemy variants | none | Tank in the horde (data); a Warden escort (data); the Warden's reinforcement phase (code) |
| Text | the floor's and rooms' display names live in the asset; the checkpoint's descend text is generic | lore objects (docs/44 1.7 style) if wanted |
| Code | none expected: FloorDefinition, ClimbScaling, WardenRule.GateHealthShareOn, the district round and the checkpoint already handle any number of floors | the Warden phase: WardenRule, EncounterController, and the simulation's Warden |

Things to check while building, because they have only ever seen two floors before the finale: the HUD's floor line and
room pips, the result screen's progress line, Profile.RecordFloorCleared and the telemetry's floor ids, the checkpoint
text, and docs/44's achievements that name floors.

### 3.2 Art: reuse and new

Every floor plays on the same foundry map today. FoundryRegionView builds the region once from FoundryMap; no
FoundryRegion* class reads the floor number. Floor 2 has no art of its own.

**Reused as it is:**
- the whole foundry region: all FoundryRegion* builders (court, gate, workyard, pit, undercroft, distant works and the
  rest), the baked boiler, crane, stock hoist and stock station;
- the atmosphere, depth cover and visibility views;
- the enemy art (EnemyArt: Grunt, Runner, Tank, Cinder Mite, Grunt Captain, the quicksilver Forge Warden);
- the chest, altar, drop and forge views, the HUD and the checkpoint panel;
- the hero art for the Vanguard and Ilya.

**New, smallest first:**
1. Names only (floor, rooms): no art.
2. A floor-3 tint on the Forge Warden and the Captain (a palette variant in EnemyArt / PixelPalette): small, no renderer.
3. A floor-wide colour grade (ash-green light for "Gloam") through the atmosphere view's existing material: moderate,
   no new geometry, but every consumer of the shared materials must be checked (the art notes' rule).
4. A few floor-3 props or lore objects in the halls: new renderers and vertices (see 3.3).
5. A real Gloam Gardens region and Rootmother Myrr: a new region and a new boss. The foundry region took from September
   to November; this is weeks, not days, and is not proposed now.

### 3.3 Device budget risk

- **The region budget is almost full.** The foundry region measures 36 renderers / 171,884 vertices, 3,572 of them
  distant, against ceilings of 174,000 and 3,600: 2,116 vertices and 28 distant vertices remain
  (ArtDirection/2026-10-14/foundry-inner-piers-01/validation/next-visual-priorities.md). The data-only floor 3 adds
  nothing. Items 1-3 above add nothing. Props (item 4) can only come from replacing existing geometry, not adding to it.
- **The crowd does not grow.** The horde's targets and its 40-body scene cap (HordeDirector) are per floor, so floor 3
  has floor 2's frame cost. A Tank horde puts bigger sprites on screen at the same count; check the overdraw on the phone.
- **The run gets 50 % longer.** The phone has not yet played the 10-minute Descent (the S23 frame time is the open V0.1
  row). A 15-minute run is a longer thermal soak (docs/40 section 14), more garbage over time, and a larger telemetry
  file. The night's long-run growth sweep found nothing unbounded at 10 minutes; repeat it at 15.
- **Risk reading:** low for the data-only floor, medium for the 15-minute thermal soak, high for any new geometry.

### 3.4 Tests

EditMode (Unity):
- ClimbFloorContentTests: the district round's routes table gains floor 3's row; the gate boss share test covers floor 3's
  share and ClimbScaling.Boss(3); EveryFloorInTheProjectPassesTheDescentsChecks and the HUD pips test cover the new asset.
- FoundrySimulationSceneValueTests: a TheFoundrysThirdFloorIsTheAsset test, as floor 2's.
- FoundrySimulationTests, FoundryFinaleTests, FoundryGateCallTests: the finale moves from `.Next.Next` to
  `.Next.Next.Next`; floor 2 now descends to floor 3; floor 3's gate holds and calls.
- ChestRuleTests if the finale's heal changes.

PlayMode (Unity):
- FoundryDescentFlowTests and DescentFlowTests: the shipped climb plays through three floors and the finale (each floor's
  clock started at 215 s, as today, so the waits are skipped).
- GateHoldFlowTests, FoundryRunEndFlowTests, PaintedHudFlowTests, RunResult checks for floor 3.

CombatChecks (dotnet, no Unity): the foundry suites, and the default batch's FOUNDRY_FLOORS meaning (today 3 = the
finale). Floors 1 and 2 must stay byte-identical in the batch; only the finale's rows may move.

### 3.5 Simulation parity

- The simulation needs `FoundryFloorThree()`, mirroring the asset value by value, chained between floor 2 and
  FoundryFinale(), and a scene-value test that reads both.
- Floor 3 is the first floor where ClimbScaling.Boss applies (x1.15). The scene (EncounterController's scaling) and the
  stand-in both apply it, but no shipped test has held them together on a real floor-3 asset yet.
- The Warden's share must go through WardenRule.GateHealthShareOn on both sides (one body, never inlined). The memory's
  lesson from the stamina divergence applies: never copy arithmetic across assemblies.
- If the reinforcement phase is built, it needs the same treatment and its own parity test, as the Warden's recovery did.
- The Editor confirmation: HordePacingMeasurement down the whole Descent (horde-floors set one higher), 6 seeds for each
  hero, against the simulation on the same seeds. On 2026-11-11 the two agreed within 6 s over two floors.

### 3.6 Build plan

Sizes are rough, for one agent; measurement runs are wall time on this machine.

| Step | What | Size |
|---|---|---|
| 0 | The owner answers section 5's questions | - |
| 1 | **Simulation first.** `FoundryFloorThree()` with the proposed data in the research only (as docs/47's levers, off by default). Measure: the plain horde against the Tank horde; the Warden's share tuned to 45-65 s; the shipped chest against the 30 % Mend before the finale. A docs/48 status with the tables | 0.5-1 day, about 2-3 h of runs |
| 2 | **The asset.** Floor_GloamGardens.asset with the measured values; floor 2's `_nextFloor` to it; the simulation's floor 3 made default | 1-2 h |
| 3 | **Tests.** The EditMode, PlayMode and CombatChecks updates of 3.4; the default batch shows floors 1-2 unchanged | 0.5-1 day |
| 4 | **The Editor.** HordePacingMeasurement down three floors and the finale, both heroes, beside the simulation | 0.5 day (about 3 h of 15-minute runs) |
| 5 | **The heal before the finale** if step 1 says the chest is not enough | 1-2 h |
| 6 | **The phone.** A new APK, a full 15-minute run on the S23: frame time, thermal, memory (the owner's go needed, as for every install) | 0.5 day with the owner |
| 7 (optional) | Warden escort (data) and the floor-3 tint (EnemyArt) | 0.5 day |
| 8 (optional) | The Warden's reinforcement phase: rule, scene, simulation, parity tests, re-measured band | 2-3 days |
| 9 (optional, later) | Floor-3 props from replaced geometry; the Gloam Gardens region and its boss | weeks |

The data-only floor 3 (steps 1-6) is about 2-3 working days. With the escort, tint and reinforcement phase, about a week.

## 4. Alternatives

| Option | Descent P50 (measured) | Reaches 13-16? | What it costs | Risk |
|---|---|---|---|---|
| **A third floor at 3:30** (`set-b`) | 14.6 min (P10 14.2, P90 15.1) | Yes, every completed run | New content (this doc) | Medium: 1-5 points of completion, more deaths before the finale (158 against 105) |
| A third floor with its gate as the room clears (`third3`) | 10.1 min (on the pre-build floor 2) | No, about +2.3 min only | The same content | Higher: floor 3 cleared by only 81-96 %; the wait's farm is what pays for the tier |
| **Longer floors** (later gates) | 3:00 -> 3:30 on floor 2 added 30 s (`gate180` 9.6, `gate210` 10.1) | Not without very late gates | One value per floor | Each floor would need about 2 minutes more, a gate near 5:30. The horde's overrun starts at 300 s on the floor's clock (HordeClock: +16 % health every 15 s), so the Warden would be fought inside the overrun. Not measured, and docs/47 night shows the later Warden already costs the first-card cohort |
| **Extra node rooms** (surges, elites, halls) | `f2asf1` (floor 1's surges and extra elite on floor 2): 10.1 min, no longer than the gate alone | No | Data | With a clock gate, rooms finished before 3:30 add no time; they only add deaths (168 against 105 on floor 2). They add minutes only if they push past the gate, and then they act like a later gate |
| **A longer finale** (`stay90`) | 8.3 min (pre-build), `set-a` (with the chest and floor 2's gate) 10.6 min | No | The autopilot's stay; for players, docs/40's rewards for staying | A cliff: 11-33 % finale deaths per cohort at 90 s without the chest; at 120 s the careful human player walks out of about half (docs/41) |

**Reading.** The gate decides each floor's length, so rooms and surges cannot buy minutes. The finale cannot be the
place either: past 60 s it is a cliff. Later gates run into the overrun. A third floor is the only measured way to
13-16 minutes at about today's completion.

**And the other way.** The owner can also decide 10 minutes is right for a mobile session for now. docs/40 itself lists
"extract after floor 2: 8-11 minutes" as the medium session (section 3.2), and docs/44 1.6 warns that long runs need
suspend/resume. Keeping two floors and building characters (docs/46) first is a valid order.

## 5. Questions for the owner

1. **Do we want 13-16 minutes now, or later?** A third floor is post-V0.1 scope. Should it wait for the V0.1 gate's
   device row (the S23 frame time), as docs/46's Riftblade does, or go into the simulation now?
2. **The name.** Gloam Gardens (docs/44, on foundry art until garden art exists, as Quicksilver Vaults is today), or a
   foundry name such as The Dawn Engine?
3. **Data-only first?** Is a floor 3 built from existing enemies and the same map enough for its first version, knowing
   it will look like the foundry again?
4. **The horde.** Keep Mite, Grunt, Runner, or try Grunt, Runner, Tank as floor 3's own feel (measured first)?
5. **The Warden.** Is a stronger, escorted Forge Warden enough, or should floor 3 get the reinforcement phase (2-3 days)?
6. **A modifier.** None (as measured), Cursed Gold again, or a new one (for example less healing, or faster horde)?
7. **The heal before the finale.** Keep the quarter-heal chest if it measures enough, or a 30 % Mend at the checkpoint?
8. **Completion.** Is a drop of up to 5 points per cohort (measured 1-5) acceptable for 4.5 more minutes?
9. **The finale after floor 3.** It is not climb-scaled, so a hero coming from floor 3 meets the same swarm with more
   levels. Should the finale scale with the floor it follows?
10. **Order of work.** Third floor before or after the Riftblade (docs/46) and the phone run?

## Status 2026-11-12: phase 1 built (the floor, the chain, the simulation, the measurement)

The owner delegated the game's vision to the lead, and the lead took section 3.6's steps 1-3 and 5: the data-only floor 3,
measured in the simulation. Question 2 is answered with choice A (Gloam Gardens), questions 4 and 6 with the plain horde
and no modifier. Nothing on screen is new: floor 3 plays the foundry's map, art and enemies. The Editor's confirmation
(step 4), the phone (step 6) and every option of section 1 wait.

**What was built.**
- **Floor_GloamGardens.asset** (new, guid b5ee68d4...): floor 2's six rooms wave for wave under section 1.1's names (Ash
  Orchard, Rootbound Walk, Bloom Gate, The Grafting Forge with Mend and Temper, Thorn Post, the Warden's Grove with the
  Forge Warden and two Cinder Mites); climb-scaled at floor 3 (ClimbScaling x1.96 health, x1.8032 damage, the gate boss
  x1.15 more), the authored multipliers set to the same; the plain horde (Mite, Grunt, Runner); no modifier, no surges, no
  extra elite; the gate at 210 s; `_foundryGateHealthShare` 5.102041; the Foundry theme (FloorTheme 0); the Final Swarm
  below it.
- **The chain.** Floor_QuicksilverVaults' `_nextFloor` is floor 3 (one line). The climb is floor 1, floor 2, floor 3,
  the finale, and the finale is the scene's FloorNumber 4.
- **No heal before the finale.** The shipped arrival chest (a quarter heal) is enough: no finale death in 1,206
  finales (below). This is the smallest option: nothing added.
- **The simulation** mirrors it: `DescentSimulation.GloamGardens` (the rooms), `FoundryFloorThree()` (floor 2's next,
  FloorNumber 3, `FloorThreeGateShare`), `FoundryFinale()` at FloorNumber 4 and `FinaleFloor` = 4. The Warden's share
  goes through `WardenRule.GateHealthShareOn` on both sides (LaterFloorLevers), as floor 2's does. No new arithmetic.
- **FOUNDRY_FLOORS counts floors, the finale included.** 2 is floors 1-2 (the default batch, unchanged); 3 is floors 1-3
  (the cohort section gains floor 3's table and foundry-cohorts-floor3.csv); 4 (`FinaleFloor`) is floors 1-3 and the
  finale (foundry-finale.md and .csv, now with a floor-3 column). Before this build 3 reached the finale.
  `FoundryBatch.FinaleOf` finds the swarm floor however many floors stand before it.
- docs/47's two research harnesses keep the climb they measured (`FoundryTwoFloorClimb`: floor 2 straight into the
  finale), so their tables still reproduce. `FinaleStayResearch` and the Editor-seed rows now play the whole Descent.
- `Tools/CombatChecks/ThirdFloorResearch.cs` (explicit): the shipped Descent with floor 3's share, gate or a finale heal
  as options.

**How it was measured.** Seeds 1-200, both heroes, all five cohorts, human hands (`PilotRealism` typical): the batch
report with FOUNDRY_FLOORS=4 (FOUNDRY_LEVERS=0, FOUNDRY_COHORTS=1, FOUNDRY_PARALLEL=1), and ThirdFloorResearch for the
share. Pooled tables count 2,000 runs.

### Floor 3's Warden share

Floor 3's Warden fight, P50 seconds of the fights in which it fell, Vanguard / Ilya (docs/40 section 9.6: 45-65 s). The
share is its effective health against floor 1's authored 300 (the asset's value times 1.96 x 1.15).

| Effective share (asset value) | Gate | Seeds | careful | standing | first-card | clumsy | Floor-3 deaths | Descent P10 / P50 / P90 (min) |
|---|---|---|---|---|---|---|---|---|
| 8.5 (3.771074, floor 2's, `set-b`'s) | 210 | 1-200 | 53 / 49 | 42 / 41 | 46 / 44 | 52 / 51 | 55 | 14.2 / 14.6 / 15.1 |
| 10 (4.436557) | 210 | 1-100 | 57 / 53 | 45 / 43 | 50 / 48 | 55 / 59 | - | 14.2 / 14.6 / 15.2 |
| 11 (4.880213) | 210 | 1-100 | 61 / 56 | 47 / 45 | 54 / 51 | 60 / 60 | - | 14.3 / 14.7 / 15.2 |
| **11.5 (5.102041), built** | 210 | 1-200 | 62 / 58 | 48 / 47 | 56 / 53 | 61 / 62 | 75 | 14.3 / 14.7 / 15.3 |
| 12 (5.323869) | 210 | 1-200 | 63 / 59 | 49 / 48 | 57 / 54 | 62 / 62 | 78 | 14.3 / 14.7 / 15.3 |
| 11.5 | 180 | 1-200 | 61 / 57 | 48 / 47 | 57 / 53 | 61 / 62 | 62 | 13.8 / 14.2 / 14.7 |

- Floor 2's 8.5 leaves the standing cohort's fight at 41-42 s, under the band (floor 2's own Warden fight is 38-40 s for
  that cohort, docs/41), and 10 still leaves the standing Ilya at 43 s.
- 11 brings the standing Ilya to 45 s, the band's edge. 11.5 is the smallest step that holds every playing cohort inside
  45-65 for both heroes with a margin at each end (47 s low, 62 s high). 12 holds too, at 3 more deaths.
- The auto-farmer, which clears floor 3 in 2 of the 11 runs that reach it, is not a playing cohort here (as for floors 1
  and 2).
- The share costs about a point of completion per cohort: 20 more floor-3 deaths in 2,000 runs (55 to 75) from 8.5 to
  11.5, and the Descent's P50 6 s.
- **The gate stays at 210 s**, as floors 1 and 2 hold theirs and as asked. 180 s was measured beside it at 11.5: the
  band holds (47-62 s), 13 fewer floor-3 deaths, the Descent P10 13.8 min, but floor 3's P50 falls to 242-254 s, at the
  bottom of section 2's 240-300 s. It is the lever if floor 3 should cost fewer runs.
