# 46. Riftblade (Nix Vane) - the third character, proposed

Proposed 2026-11-11 for the owner. Nothing here is built: no code, no asset. It is docs/40 section 4.1's Riftblade,
"Edge Dancer", and the narrative bible's Nix Vane (docs/44 section 2.3), on the character pipeline docs/45 set for
Ilya. It is the Daggers' character: docs/41's status of 2026-11-10 night left the Daggers' job "for their character",
and since 2026-11-11 the Daggers are the elites' weapon (`EliteRule.DaggersShare` 2, the careful pilot stepping in on a
Captain with them through `FootworkRule.StepIn`).

**Building waits for two things:**
1. the owner's go on this document;
2. the V0.1 gate's device row: the S23's frame time at the chosen crowd (p95 at most 20 ms at 30-40 alive, no frame
   over 35 ms in a steady fight; docs/41 step 1). docs/27 decision 12: only past that gate "come multiple characters".
   The gate stands at met 9, not met 0, not measured 2: the owner's floor time and this row (docs/41, 2026-11-11).

Every number below that is not cited as measured is a proposal for the simulation to measure, not a decision.

## 1. Who Nix is (player-facing, English)

- **Name on the select screen:** Riftblade, subtitle "Nix Vane": the bible's name, marked there for decision. If the
  owner wants another in the same style: Wren Strand, or Sable Quill. Nix is "it" in every line, label and comment, as
  every hero is.
- **Story hook (bible 2.3):** a courier who carried secret name ledgers into the Quicksilver Vaults, until one
  delivery held the order that erased its own name. Its reflection still talks to it; it no longer remembers who called
  it "sibling". It climbs to learn whether the sibling the Vault Gate register zeroed ever lived. It ties into floor 2,
  the Quicksilver Vaults, which already plays after The Foundry.
- **Lines (bible):** select "Can't erase what you can't catch." / death "Tell the mirror I was faster." / victory "One
  cut closer to the truth."
- **Signature:** a tiny round mirror pendant with a second face in it. The reflection's hair parts the other way, and
  one of its eyes is open where Nix's is closed. It lives on the select screen and the portrait (it can blink when Nix
  is chosen); at 80 pixels in play it is a glint, so the primed crit gets a cue of its own (section 3, step 8).
- **Silhouette (bible):** a slanted triangle: a short cape on one shoulder, two daggers of different lengths, one tall
  boot. The palette is section 5's.

## 2. How Nix plays (first numbers; the autopilot measures them)

| Part | Proposed |
|---|---|
| Maximum health | 85 (Vanguard 100, Ilya 90): the most fragile of the three; the dash is its guard |
| Starting weapon | Daggers (exists: 7 damage every 0.3 s at reach 1.8 on the category base, every 3rd strike a crit x2, x2 on the elite enemies). Its own weapon only at the forge, as Ilya's Staff |
| Passive - **Quicksilver Stride** | After 2.5 floor units walked, the Daggers' next strike crits (x2) unless its rhythm already does; at most once every 1.2 s. Every crit of the Daggers - the rhythm's, a rolled one or this one - opens a **Bleed** on what it struck |
| Bleed (new status) | 60 % of what the crit landed, paid over 3 s in ticks of 0.5 s; wounds pool |
| Active - **Phase Dash** (9 s) | 3 floor units in 0.2 s along the stick's heading; every hit on Nix turned away for 0.3 s from its first step; its travel counts as walking. Replaces Forge Burst on the one ability button |
| Later | Sever (18 s, marked execute) once the two-button decision is made |
| Skill dependence | 4 (docs/40 4.1): R_skill 1.60-2.00x, and played auto-only "boss'ta zorlanmalı", it should struggle at the boss (docs/40 4.2) |
| Unlock | Clear the Quicksilver Vaults (floor 2) carrying the Daggers |

### Quicksilver Stride

- **Walking** is the hero's displacement as `HeroMotion` moves it, measured through `ArenaFloor`'s metric: a steer into
  a wall walks nothing, as `HeroStamina` counts only real displacement. The dash's travel is walking. docs/40's "2.5 m"
  is read as 2.5 floor units.
- **The primed crit.** Once 2.5 units are walked and 1.2 s have passed since the last, the Daggers' next strike crits
  unless its rhythm already does: a rhythm crit never spends it, as it never spends a rolled draw. Spending it starts
  the walk from 0 and the 1.2 s again. The Daggers' evolution, Rift Echo, splinters these crits like any other.
- **What it reads.** The foundry walk is 3.5 units a second (`HeroMovementInput`'s 2.5 x `FoundryPace` 1.4), so 2.5
  units take 0.7 s and the 1.2 s binds. A hero that keeps moving adds about one crit every 1.2-1.5 s to the rhythm's
  every third strike; one that stands crits by the rhythm and its rolled chance alone. The careful pilot kites, and the
  standing and auto-farmer cohorts stand in a fight. That gap is the skill the passive measures.
- **The blades, not every weapon** (Ember Memory is every weapon's). On every weapon the primed crit would fall to
  whatever reaches first. Kited at 2.0, the Warden is the Sword's (reach 2.1), not the Daggers' (1.8), and a crit and
  its wound every 1.2 s on top of `WardenRule.SwordShare` would more than double the Sword's rate on it (an estimate);
  on the horde a crit doubles the Staff's whole area. On the blades the roles hold - the Staff the horde's, the Sword
  the Warden's, the Daggers the elites' - and Nix makes the Daggers' job its own rather than borrowing the others'. The
  Daggers are slot 0 for the whole run. The other way is measured as a lever (section 4).

### Bleed

Bleed is the game's second damage over time, beside Burn, with a pure rule of its own (`BleedRule`).
- A **wound** is 60 % of the crit as it landed on its target, after the shares it took (the Daggers' x2 on an elite,
  `EliteRule`), so it grows with every Force tome and Daggers line, where Burn's 3 a second a stack does not. Its ticks
  take no share again. Only the strike's own target bleeds: no splash, splinter or pulse opens one.
- Wounds **pool** on the enemy: each adds its worth and sets the bleed's 3 s back to full, and every 0.5 s tick pays the
  pool spread over the time left. Every wound is paid in full unless the enemy falls first. No cap: crits come at the
  rhythm's third strike and once in 1.2 s from the walk, and docs/42's proposed 80 % crit cap guards the rolled ones.
- **Proportional where Burn is flat.** Burn lands on every hit of every weapon, the Staff's whole area included, and a
  share of each hit would multiply with the area; a wound opens on one target, from one crit.
- A bleed is Nix's damage: a bleed kill drops and pays like any other, and the bleed stops when Nix falls and at the
  run's end, as a burn does. **The Warden bleeds**: docs/40 gives the Riftblade "Elite/boss strong". The design
  options' F spared the Warden only to leave the Sword its job among the weapons, and the roles row reads the Vanguard.
- docs/40's "+60 % Bleed damage" presumes weapons that bleed of their own (its Daggers' "+15 % Bleed" line, the Cinder
  Saw, the Sanguine Sickle). V0.1 has none, so the 60 % is the wound's size. If a later weapon line bleeds, Nix's 60 %
  becomes a multiplier on it, as docs/40 has it.

### Phase Dash, and why not Sever

**Phase Dash:** 3 floor units in 0.2 s along the stick's heading, or the last heading walked when no finger is down;
walls slide or stop it as they do the walk. For 0.3 s from its first step every hit on Nix is turned away: an enemy's
strike, the Warden's slam, the pit's spill. Its travel is walking, so a full dash primes the next crit once 1.2 s have
passed since the last. It deals no damage of its own and runs on its cooldown alone (stamina stays off,
`HeroStamina.CurrentRulesEnabled`). It throws nothing: docs/42 keeps V0.1 free of projectile objects.

Why the dash is the one active (the first release has one a character, docs/45):
1. **It fits the button.** `AbilityButtonView` taps `AbilityController.TryUse`, and a dash is a tap along the heading
   the thumb already holds. Sever needs a target a one-thumb player cannot point at; the heaviest enemy in reach, chosen
   for it, is a guess at what the player meant.
2. **It feeds the passive.** The dash is walking, so the press is also the next crit, and the crit is the bleed: one
   loop, move - crit - bleed.
3. **It answers what binds the Daggers.** The Daggers' design options found that "the reach binds, not the damage". The
   careful pilot kites the Warden from 2.0 against the Daggers' 1.8: the Vanguard started with the Daggers deals the
   Warden 0 a second with them. With a dash ready, a player can stand at the Warden's feet through the telegraph and
   leave as the hammer falls; without one it must walk out early. docs/41 calls it "the closing-in a player does by
   hand, which the step-in stands for".
4. **Sever's parts measured weakest.** An execute (x2 under 30 %, option E) had the smallest effect of the five
   options, and an exposing mark (D) did not shorten the elite fights: the fixed pie. An execute on the gate boss
   either ends the 45-65 s fight or has to spare it, against "boss strong".
5. **It is a fragile hero's defence.** docs/40 makes the Riftblade fragile "standing still or under dense close
   pressure"; the dash is the way out, and the bible's achievements (Mind the Gap, Never There) are built on it.

**What Sever would need later:** the two-button decision (docs/40 4.1's UI note: a second button, a long press or a
context-aware press, checked against the drag); a mark status and its decal (option D's shape); an execute in the one
damage path every landing takes (the `WardenRule.DamageOn` pattern) with a rule for the gate boss; a target (the
heaviest in reach, as the research's `AcquireHeaviest`); telemetry (`sever_used`, `sever_kills`, and
`sever_on_bleeding` for the bible's "Sever the Thread"); and an autopilot rule of its own.

### Unlock

Adapted, as Ilya's was. docs/40 13.1 asks for an elite felled within 30 s and an extract after floor 2; the bible
(section 6) for the Mirror Ledger read on floor 2 and a Runner felled within 3 s. The lore objects do not exist yet, and
since the Daggers' x2 a Captain they reach stands P50 1.1-1.4 s under fire, so the elite half teaches nothing.
Proposed: **clear the Quicksilver Vaults carrying the Daggers**, the floor of Nix's story and docs/40's skill half.
Development builds open every hero, as today.

### A first estimate, not a measurement

The Vanguard started with the Daggers fights the Warden in 56 s (P50, careful, frame-perfect) with its Daggers at 0 a
second there: the Sword and the Staff it picks up carry the fight. On the gate's batch (the Vanguard's runs) the Warden
has 3570 health (P50); the hero's weapons take 79 % of it and the pit 18 %, about 52 a second from the weapons over the
54 s after the reveal. A careful Nix whose dash is ready about a quarter of the fight (9 s against a blow every 6 s, 4 s
enraged) stands at the Warden's feet then. Its Daggers deal about 50 a second there on the category base - rhythm,
momentum and bleed together - and more with the cards a run holds by the gate, adding some 15-25 a second over the
fight. That puts the fight near 42-47 s, and R_skill near 1.3-1.5x on the hero's own damage on the Warden.

If it holds, docs/40's 1.6-2.0x and the gate's 45-65 s cannot both hold: a dash strong enough for 1.6x takes the fight
to about 40 s. That is inside docs/40 section 8.8's own band for the Riftblade's boss, 35-55 s. The measurement
decides, and the owner chooses which binds (section 7, question 4).

Collision to resolve: the bible's "Never There" reward is an "Afterimage" effect, so the dash's own trail is a plain
silver streak and the afterimage stays the reward.

## 3. What has to be built (engineering)

**What exists and carries over** (docs/45 section 6):
- the hero roster and chooser (`CombatSetup._heroes`, `HeroChoiceView`, `development/hero.txt`,
  `DevelopmentStart.AllHeroesOpen`) and the profile's version 3 (`unlockedHeroIds`, `selectedHeroId`): no save change;
- `HeroDefinition`: subtitle, passive, ability, lines, rendered art, portrait, own weapon only;
- one ability button (`AbilityButtonView`: a tap calls `AbilityController.TryUse`, with the hero's own icon), the ring
  (`AbilityRingView`, scaled by the ability's radius), and the Cast clip on `AbilityController.Used`
  (`RenderedCharacterAnimator`);
- the Daggers and `EliteRule`, `FootworkRule.StepIn` and `Habit.StepsIn`, the simulation's `FoundryHero`, the harness.
The research's `FoundryLevers.Daggers*` bleed, mark and execute stay research; Nix's bleed is the game's own rule.

**The project's rules, kept by every step below:**
- rules are pure and compiled by the .NET checks (`Tools/CombatChecks` lists each file);
- float arithmetic that the scene and the simulation share lives in one body that is never inlined
  (`MomentumRule.Stride`, `BleedRule.WoundOf`, the dash's step);
- no `System.Random` and no `Math.Pow`: nothing here draws, and `RunRandom`'s streams are untouched;
- enums are appended, never renumbered;
- the scene and the simulation run in the same order within a frame;
- every new path is off for every other hero, so **the default batch stays byte-identical for the Vanguard and Ilya**
  after each step.

In order, each step compiling and leaving every existing test green:

1. **Bleed.** `BleedRule` + `BleedState` (Combat, pure) and `BleedStatus` (the scene component an enemy gains on its
   first wound), modelled on `BurnRule`/`BurnState`/`BurnStatus`.
   - `WoundShare` 0.6, `DurationSeconds` 3, `TickSeconds` 0.5; `WoundOf(landing)` one never-inlined body.
   - The pool, its refresh and its ticks, paid as the hero's damage, so a bleed kill drops and pays like any other.
   - `BleedStatus.StanchAll` on `Run.Ended`, as `ExtinguishAll`; events `Bled` and `BleedKill`.
   - `BleedRuleTests`: a pool pays every wound in full; a refresh stretches the pool and loses nothing; frames that sum
     to a tick still pay it; nothing after the enemy or the hero falls.
2. **Momentum.** `MomentumRule` (`StrideUnits` 2.5, `CooldownSeconds` 1.2) and `MomentumState` (`Walk`, `Tick`,
   `IsPrimed`, `Spend`).
   - `MomentumRule.Stride(fromX, fromY, toX, toY)`: the square root of `ArenaFloor.FloorDistanceSquared`, in one
     never-inlined body (the stamina lesson). It is fed at the walk step - `HeroMovementInput` (order -60) in the scene,
     the same point of the simulation's frame - from `HeroMotion`'s position before and after its step.
   - `WeaponRuntime` gains a primed crit. The hero's attacker primes slot 0 before it strikes, and `TryAttack`'s order
     becomes rhythm, primed, rolled, at the pattern's multiplier (else `RolledCritMultiplier` 2). It records the
     strike's facts for the bleed: `LastStrikeCritical`, `LastStrikePrimed`, and `LastLanding`, the target's landing
     after the shares.
   - Never primed, a runtime is today's to the bit: every enemy's, the Vanguard's, Ilya's.
   - `MomentumRuleTests`; `WeaponBehaviorTests` for the order and the multiplier.
3. **Phase Dash.**
   - `AbilityKind.Dash = 2` (appended); `AbilityRuntime` takes a dash with no damage, its radius the dash's length.
   - `PhaseDashRule`: `TravelSeconds` 0.2, `GuardSeconds` 0.3, the per-frame step in one never-inlined body.
   - `HeroMotion` keeps the last heading walked and gains the dash. Armed by the press, from the next walk step it
     moves at length / travel a second along its heading through `FoundryMap.Move`, the stick ignored, a wall sliding
     or stopping it (what is left is lost).
   - `HealthState` gains a guard, set at the walk step from the dash's clock: a hit while guarded is turned away and
     raises `Turned`. The slam, every strike and the pit's spill are turned in that one place, and `WardenSlamService`
     raises `SlamTurned` when its blow finds Nix guarded.
   - `AbilityController`: a dash spends the cooldown and raises `DashRequested`; `HeroMovementInput` takes it at its
     next step.
   - `PhaseDashTests`: the travel, walls, the heading with no finger down, each kind of hit turned, the cooldown, the
     dash priming the crit.
4. **Character data.**
   - `HeroPassive.QuicksilverStride = 2` (appended).
   - `Hero_Riftblade.asset`: `hero_riftblade`, Riftblade, Nix Vane, 85, the Daggers, own weapon only, Phase Dash,
     Quicksilver Stride, the bible's lines. `Ability_PhaseDash.asset`: 3 units, 9 s, its icon.
   - `HeroDefinition._unlockFloor`, 1 by default so Ilya's unlock reads as it does. `CombatSetup` unlocks on that
     floor's clear (`UnlockHeroesForTheFoundry` for any floor); the hint reads "Clear the Quicksilver Vaults with the
     Daggers".
   - `CombatSetup` wires the momentum and the bleed (slot 0's crit, on `StruckWith`) only for a hero with the passive,
     as it wires Ember Memory only for Ilya. The roster gains the third hero; the chooser cycles three.
   - Content tests for both assets.
5. **The simulation's hero and the parity pins.**
   - `DescentSimulation.Riftblade()` (85, `Daggers()`, `PhaseDash()`, `QuicksilverStride`) beside `Vanguard()` and
     `CinderArcanist()`. Its walk feeds the momentum and runs the dash and the guard; `HeroAttacks` primes slot 0 and
     opens wounds; `TickBleeds` runs beside `TickBurns`, on `BleedState`.
   - `FoundryResult` gains `DealtByBleed`, `WardenFromBleed`, `MomentumCrits`, `Dashes` and `DashTurned`. A bleed's
     damage is the Daggers' work (docs/40 section 8.4: the originating weapon), and its kills read `bleed` beside
     `burn`.
   - `FoundrySimulationSceneValueTests` hold `Riftblade()`'s restated numbers to the two assets: duplicated numbers
     drift. `FoundrySimulationTests` hold the passive and the dash to what they name, and the Vanguard and Ilya to their
     runs.
   - A PlayMode flow on `Floor_EliteProof` (one elite room, a Captain alone): 2.5 units walked, the first Daggers strike
     crits and the Captain bleeds 60 % of it over 3 s; the dash moves 3 units in 0.2 s and turns the Captain's blow.
   - The harness's two-hero arrays (`FoundryBatchReport`, `FoundryBatch.Sword` and `.Daggers`, the cohort and gate
     tests) and its pacing files (`FOUNDRY_EDITOR_FILES`, the Vanguard's then Ilya's, read by the gate and
     `FoundryBatchReport.Parity`) take a third hero. The gate keeps reading the Vanguard (decision 12).
6. **The autopilot's dash.** It rides on `Habit.UsesTheAbility`, so no new habit, and the auto-farmer never dashes. One
   body for `DevelopmentAutopilot` and the simulation (`FootworkRule`), proposed:
   - **the late escape:** while the dash is ready, a cohort that escapes marks holds Nix at the Daggers' reach of the
     gate boss through the telegraph (a step-in for a ready dash only), and dashes out along `EscapeMark`'s heading when
     the blow is due within the guard; with the dash cooling it walks out early, as today;
   - **out of a surround:** four or more enemies within their own reach and Nix under half its health: a dash along the
     kite's heading.
   The press reads the steer the pilot holds. With human hands it comes a reaction late, as every answer does
   (`PilotRealism`). `AutopilotCohortTests` and `FoundryCohortTests` gain the dash only.
7. **Telemetry.** `run_start.hero_id`, its `ability_id` and `ability_used` exist. `run_end` gains, last and only for a
   hero with Quicksilver Stride (as `burn_kills` comes only when something burned): `momentum_crits`, `bleeds_opened`,
   `bleed_kills`, `dash_turned` (hits the guard turned away) and `dash_slams_turned`. `RunTelemetryTests` and
   `TelemetryFlowTests` are re-pinned for the new keys only. They are the signals docs/44's Riftblade achievements need
   (First Cut, Mind the Gap, Never There, Bleeding Edge, Pinpoint).
8. **HUD and presentation.**
   - The ability button shows the Phase Dash icon (P4) through `AbilityDefinition._icon`. The ring shows the dash's
     length, bright while ready; a chevron along the heading is the later fix if the device shows the ring misleads.
   - The primed crit: a silver glint at Nix's blade hand while primed (one pooled sprite), then the crit star as today.
   - The bleed: the bleed mark (P5) on each bleeding enemy, a pool like `BurnFlames` (few at once, since only crits open
     wounds); its ticks spark crimson (#CF4D64) as Burn's spark ember.
   - The dash: a silver streak, and Nix flashed silver while guarded (`RenderedCharacterFlash.shader`). Nix's Cast clip
     is the dash's lunge, held through its 0.2 s: the walk frames, driven by distance, would spin 2.5 strides in it.

## 4. Measurement plan and acceptance

The harness as it stands (docs/41 step 5): `FoundryBatchReport.TheFoundryFloorOverASeedBatch` with
`FOUNDRY_SEEDS=200 FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`, frame-perfect and again with
`FOUNDRY_REALISM=typical`; seeds 1-200 a hero, and the holdout 201-400 (`FOUNDRY_FIRST_SEED=201`) for the careful
cohort. Nix's levers get an explicit report of their own, as `TheDaggersRoleOverASeedBatch` reads the Daggers':
- Nix as proposed;
- without the dash: the careful cohort less `UsesTheAbility`, R_skill's denominator;
- the passive on every weapon;
- the dash as an escape only, with no hold at the Warden;
- the wound at 0.4 and 0.8; the dash at 12 s; health 80 and 90.

Then the Editor: `HordePacingMeasurement` with `TestResults/horde-hero.txt` holding `hero_riftblade`, on seeds 11-66,
for the batch's parity table. Then the S23. The autopilot's dash is not a thumb's: the owner plays Nix on the device
before the R_skill and Warden rows are believed.

| Row | Target (proposed) | Reading |
|---|---|---|
| No regression | the Vanguard and Ilya to the byte | the default batch's CSVs and foundry-gate.md before and after each step of section 3; every EditMode, PlayMode and CombatChecks test green |
| Clears, careful | floor 1 within 10 of 200 of the Vanguard's, both floors within 15, with either hands | seeds 1-200 and the holdout |
| Clears, the weaker cohorts | standing, clumsy and auto-farmer no higher than the Vanguard's: skill 4 sits above skill 1 | Ilya (skill 3) clears more than the Vanguard in the clumsy and auto-farmer cohorts today (183 to 106 and 36 to 11 frame-perfect, 138 to 81 and 29 to 9 with human hands): docs/40's skill order has not been held to clears, and Nix would be the first held to it |
| The walk | momentum crits a minute, careful against standing: reported | the passive reads movement |
| R_skill | reported, not a target: docs/40 4.2's 1.60-2.00x read as the careful cohort with the dash over the same cohort without it, the hero's own damage a second on the Warden (every weapon and the bleed, the pit apart), P50. Measured 2026-11-11 it reads 1.0-1.1x for every hero (Forge Burst 1.04x, Crucible Vent 1.01x, Phase Dash 1.09x), so on the Warden it cannot separate the heroes' actives. Skill 4 is held instead to the weaker cohorts' clears (the row above) and to the slams the dash turns with human hands (a third fewer landed, "R_skill" and question 4 below) | section 2's estimate was 1.3-1.5x; the hold at the Warden's feet whenever the dash is ready, the strongest variant, reads 1.21x / 1.15x |
| Elites | a Captain felled P50 well under 30 s from walking in (docs/40 13.1's quest), and none within a frame of its first damage | today the elite fights last P50 5 s and a Captain the Daggers reach stands 1.1 / 1.4 s under fire (frame-perfect / human hands); the reverted x12's lesson was the one-shot |
| Warden | careful P50 45-65 s on floor 1 and floor 2, with either hands; none felled within 5 s | the gate's band, with docs/40 8.8's 35-55 s for the Riftblade beside |
| Boss'ta zorlanmalı | the auto-farmer cohort (no ability, no dodge): of its runs that reach the Warden at least the auto-farmer Vanguard's share die there, and its floor-1 clears are no higher | docs/40 4.2's auto-only, read as the auto-farmer |
| Roles | the gate's roles row unchanged (it reads the Vanguard); Nix's Daggers' share of the elite job and of the Warden reported | |
| Parity | the simulation on the Editor's six seeds as close to the Editor as it stands for the other heroes (floor, Warden, clears, engaged) | the batch's parity table; for scale, the gate's Warden row reads 56 s for the Vanguard over 200 simulated seeds and 54 s in the Editor's six, Ilya 50 and 51 s |
| Device | p95 at most 20 ms with Nix at 30-40 alive and no frame over 35 ms in a steady fight (the gate's device row); memory with three heroes' atlases | |

## 5. The look, the route, and the prompt pack

**Palette.** The bible's purple-black (#282039) sits in the Grunt's violet basalt and in the void at the platform's
edge, where it would vanish, as the bible's soot and orange did for Ilya on lava (docs/45 moved Ilya to ash white).
Proposed: silver-led - a long courier's coat in cool silver-grey (#A9B0CE), purple-black only in the darks, the cape's
lining and the hair - with blood red (#CF4D64) as one narrow scarf, kept apart for the bleed. Steel blue is the
Vanguard's, ember Ilya's, violet the Staff's; silver and crimson become Nix's.

**Route.** P1 fixes the look. Then the one-model route Ilya and the Warden proved (docs/45 section 8):
`BuildBlenderRiftblade.py` builds Nix in Blender 4.5 and renders eight facings from one model, so nothing drifts
between frames - idle 8, walk 8 and walking strike 8 (driven by distance), strike 5, the dash 5 (as the Cast clip),
hit 3, death 5. Ilya's 336 frames came to 4.20 MB as ASTC 4x4. P2 and P3 stay for a painted pass, or for the puppet
route if the owner prefers it.

Rules for every prompt (docs/45 section 5): transparent background, no floor, no shadow, no text, no effects unless
asked; top-down three-quarter camera like the existing Vanguard (looking down at about 45 degrees), light from the
upper left; painted, clean edges, readable at 80 pixels tall on a phone. No gendered words: Nix is androgynous.

### P1 - Character design sheet (first; it decides the look)

```
Character design sheet for a 2D top-down action roguelite, painted style matching a dark fantasy foundry world.
Character: NIX VANE, the Riftblade, a courier who carried secret ledgers and lost part of its memory. Lean, light on
its feet, androgynous, leaning slightly forward as if always mid-errand. A slanted, triangular silhouette: a long fitted
courier coat in cool SILVER-GREY (#A9B0CE), open at the front; deep purple-black (#282039) only in the shadows, straps,
cape lining and hair; a SHORT CAPE on the left shoulder only; one narrow blood-red (#CF4D64) scarf as the only warm
accent. Asymmetric haircut, longer over one eye; one eyebrow raised; a thin silver crack under the right eye. Two
daggers of DIFFERENT LENGTHS held in reverse grip. The left boot reaches the knee, the right is short. A tiny round
mirror pendant on a chain at the chest, with a second face inside it. Expression: wry, alert, a little lonely. Show
four views side by side on a plain light grey background, same scale, feet on one line: front three-quarter (facing
down-left), rear three-quarter (facing up-right), pure side view, and a close-up of the mirror pendant whose reflection
has its hair parted the other way and one eye open where Nix's is closed. Clean silhouette readable at small size. No
text labels.
```

### P2 - Cut-out parts sheet (for the puppet; from the approved P1)

```
Using the approved Nix Vane design, paint the character as a CUT-OUT ANIMATION PARTS SHEET for a 2D puppet rig, rear
three-quarter view facing up-right, top-down camera, light from the upper left. Separate, non-overlapping pieces on a
transparent background, each with generous extra overlap where it joins the next (round joints, no hard cut edges, the
hidden part of each joint painted in): head with hair; torso with the coat's chest, the scarf and the mirror pendant;
coat skirt, front panel; coat skirt, back panel; the short cape (one piece); left upper arm; left forearm; left hand
(closed in a reverse grip around nothing); right upper arm; right forearm; right hand (closed in a reverse grip around
nothing); left thigh; left shin with the tall boot; right thigh; right shin with the short boot. Then, apart, the two
daggers, long and short, each alone. Same painted style, same scale for every piece as if the character were 1300
pixels tall. Neutral flat lighting inside each piece, no cast shadows. No effects, no text.
```

Then the same for the front three-quarter view (facing down-left).

### P3 - Frame sheet (fallback if P2 does not separate cleanly)

```
Using the approved Nix Vane design, rear three-quarter view facing up-right, top-down camera, light from the upper
left: ONE horizontal sprite sheet of 7 frames on a transparent background, each frame in an equal cell, the character
at the same size and the same floor line in every cell: idle, walk contact A, walk passing A, walk contact B, walk
passing B, strike (both arms crossing in a reverse-grip cut), dash (a low lunge forward, body stretched, coat, cape and
scarf streaming back, the back foot leaving the floor). Identical costume details in every frame; only the limbs, the
coat's hem, the cape and the scarf move. Hands closed in a reverse grip around nothing (the daggers are attached in the
game). No effects, no shadow, no text.
```

### P4 - Phase Dash icon

```
A round ability icon for a dark fantasy mobile game, matching two existing icons on a dark navy disc with a thin brass
ring - a glowing blue anvil with flames, and a cracked brass crucible erupting ember fire: a silver dagger-shaped
streak cutting diagonally up and to the right across the disc, three fading speed chevrons behind it, a thin crimson
(#CF4D64) edge along the streak and a small spark of light where it ends. The same dark navy disc and thin brass ring.
Bold readable silhouette at 96 pixels. Painted style, transparent background, no text.
```

### P5 - Bleed mark (overlay)

```
Status-effect overlay sprites for a 2D top-down game, on a transparent background, side by side, same scale: a small
bleed mark for one wound, a larger one for two, the largest for three or more. The enemies are stone and slag, so the
mark is not blood: a stylised open cut, a thin bright crimson (#CF4D64) rift with a pale silver edge, like a crack in
dark glass, with two or three small droplets of crimson light falling from it. A thin dark outline so it reads on
orange lava and on dark stone. Below them, a 4-frame strip of one crimson droplet falling and bursting into a small
spark, for each bleed tick. Painted style, no gore, no text.
```

What the owner sends back: the chosen P1 image, then P2 (or P3) for both facings, P4 and P5. Claude builds Nix from P1
on the one-model route (or cuts and rigs P2), integrates it, and shows matched before/after captures.

## 6. Risks

- **The two targets may not both hold** (section 2's estimate): R_skill at docs/40's 1.6-2.0x and the gate's 45-65 s
  Warden fight pull apart on a three-weapon loadout, where the Sword and the Staff carry most of the Warden.
- **A dash on one thumb.** It moves the hero. The lunge research (option C: an automatic dash every 2 s) cost the
  careful cohort about 60 floor-1 clears of 200 with human hands (195 / 199 to 135 / 140). Phase Dash is a press every
  9 s with a guard, not a lunge, but a dash into a crowd or the glowing band costs; device play decides the heading.
- **Parity of a dash:** 3 units in 12 frames through walls, a press taken at the next walk step, a guard read by every
  attack after it. One body each (`HeroMotion`, `PhaseDashRule`, `HealthState`) and the walk step's order; the flow
  test on `Floor_EliteProof` pins it.
- **The one-shot.** The primed crit (x2) on the elite share (x2) is x4 on a Captain's strike, plus its wound. The
  reverted x12 taught it; the acceptance holds that no Captain falls within a frame of its first damage.
- **Memory.** Every hero's atlases load with the roster: `HeroDefinition._renderedArt` is a direct reference from the
  scene's `_heroes`. Ilya's come to 4.20 MB as ASTC 4x4, and a third hero adds about as much whether played or not.
  Loading a hero's art set on demand (by id, from Resources) before Nix ships keeps one hero's atlases in memory.
- **The bleed's readability and tone:** crimson on orange lava and on the ember-red Captain, and stone and slag enemies
  that cannot bleed blood (P5's outlined rift). Checked on the device beside Burn's flames.
- **The palette** (section 5): the bible's purple-black in the Grunt's violet band.
- **The crit cap.** The Daggers' crit lines and Precision push the rolled chance past docs/40's 80 % (docs/42's risk);
  with the rhythm and the walk on top, nearly every strike would crit and bleed. docs/42's proposed cap in
  `RollsCritical` matters more with Nix.
- **The harness grows.** A third hero adds about half again to every batch's time and widens every table.

## 7. Questions for the owner, in order

1. **The go.** Build Nix once the S23 row is measured (decision 12), or later?
2. **The active.** Phase Dash as the one active (proposed), Sever later. And the dash's heading: the stick's
   (proposed), or toward the nearest elite or the Warden.
3. **The passive's reach.** On Nix's Daggers alone (proposed), or on every weapon, as docs/40's character-wide passive
   and Ember Memory read.
4. **Which binds, if both cannot hold.** docs/40's R_skill of 1.6-2.0x, with docs/40 8.8's 35-55 s boss band for the
   Riftblade; or the gate's 45-65 s Warden fight, with R_skill then near 1.3-1.5x. And whether R_skill reads the hero's
   whole damage (proposed) or Nix's Daggers alone.
5. **The unlock.** The Quicksilver Vaults with the Daggers (proposed); docs/40's elite under 30 s and an extract after
   floor 2 (the elite half met by every Daggers run since the x2); or the bible's Mirror Ledger and a Runner felled
   within 3 s (the lore objects do not exist yet).
6. **The name.** Nix Vane (the bible's), or Wren Strand, or Sable Quill.
7. **The bleed's look** on stone enemies: a crimson rift rather than blood (proposed).
8. **The palette.** Silver-led (proposed) rather than the bible's purple-black.
9. **The art route after P1:** one Blender model (proposed), or the painted parts.

## Measured 2026-11-11: the proposal in the simulation

Research only. Nothing in the game changed: no HeroDefinition asset, no roster entry, no scene code, and no line of
Scripts (`WeaponRuntime`, `AbilityRuntime` and `HealthState` are as they were). The Riftblade is a hero of the foundry
simulation alone, `DescentSimulation.Riftblade()`. Its rules are written as the pure bodies section 3 names, in the
simulation's assembly, so that they can move into Scripts as they stand.

**In short.**
- The proposal holds the gate's Warden band with either hands: 53 / 49 s on floor 1 (frame-perfect / human hands).
- It holds docs/40's elite quest: a Captain falls 1.4 / 1.7 s after walking in, and never within a frame.
- The careful clears match the Vanguard's (200 / 198 of 200), and the clumsy and first-card cohorts clear less with Nix
  than with the Vanguard: it sits above skill 1.
- R_skill is 1.09x / 1.03x, not 1.6-2.0x, and no variant reaches even section 2's 1.3-1.5x. The best, a hold at the
  Warden's feet whenever the dash is ready, gives 1.21x / 1.15x. On the Warden the dash adds only the Daggers' seconds
  at its feet: the careful pilot kites it at 2.0, where the Sword and the Staff reach it and the Daggers do not.
- The dash's worth is defence with human hands: about a third fewer Warden slams land on Nix, and the standing and
  first-card cohorts clear both floors 7-16 more times with it. The careful cohort's gain (+1 to +9) is within noise.
- The passive on every weapon takes the Warden under 45 s. Sever adds damage but saves no one.

### What was built (the simulation's side, every path off for the Vanguard and Ilya)

- `DescentSimulation.Riftblade()`: 85 health, the Daggers at the start (the category base: 7 every 0.3 s at 1.8, every
  third strike x2, x2 on the elite enemies), no other weapon at the forge. A `RiftbladeKit` stands in the ability's
  place.
- `Tests/Support/RiftbladeRules.cs`, one pure body each. Nothing draws and nothing raises to a power, and the float math
  the scene and the simulation would share sits in one never-inlined method each.
  - `BleedRule` + `BleedState`. A wound is 60 % of the crit as it landed (`WoundOf`). A wound sets the pool's six 0.5 s
    ticks back to full, and each tick pays the pool over the ticks left (`TickOf`), so every wound is paid in full. A
    fresh pool's clock starts at its wound; a pooled one keeps its own.
  - `MomentumRule` + `MomentumState`, the stride tracker: 2.5 floor units walked and 1.2 s since the last crit. `Stride`
    is the square root of `ArenaFloor.FloorDistanceSquared`, fed from the hero's position before and after the walk
    step, the dash's travel included. The hero starts with the clock run out, and nothing banks past priming.
  - `PhaseDashRule` + `PhaseDashState`: 3 units in 0.2 s (`StepOf`), a 0.3 s guard from the first step, 9 s.
  - `ActiveClock` (AbilityRuntime's cooldown for an active with no damage), `SeverRule` (the variant), `DashFootwork`
    (the autopilot's two dash rules).
- **The primed crit.** `WeaponRuntime` was not touched, so the simulation decides the crit beside the strike.
  - The order is rhythm, primed, rolled: a Daggers strike that its rhythm does not crit takes the primed crit.
  - Its own landings that the weapon did not crit itself are doubled where they land. A factor of two is exact in
    floating point before or after the shares, so each landing is the one a crit of the weapon's own would make.
  - A primed crit's Rift Echo splinter is thrown by the simulation after the strike's pulse. In the scene it would come
    from `TryAttack`, before the pulse; the order matters only on a strike that also pulses.
- **Wounds.** Every crit of the Daggers (the rhythm's, a rolled one, the primed one) opens a wound on its target, at
  60 % of the crit after the Daggers' x2 on an elite.
  - Its ticks are the Daggers' work (`FoundryWeaponWork.DealtByBleed`) and no landing. Bleed kills read "bleed".
  - A bleeding enemy first ticks the frame after its first wound, as BurnStatus does.
- **Phase Dash** runs at the walk step.
  - A press is taken at the next walk step, along the steer the pilot holds or the last heading walked.
  - The dash moves 0.25 units a frame for 12 frames through `FoundryMap.Move`.
  - The guard is read in `HeroDamaged`, the one place every hit comes through (a strike, the slam, the pit's spill).
- **The autopilot's dash** rides on `Habit.UsesTheAbility`, so the auto-farmer never dashes. With human hands each press
  comes a reaction after its moment (`PilotRealism`).
  - **The late escape.** The dash is ready and the Warden's mark is seen round the hero. A cohort that escapes marks
    then holds at the Daggers' reach of the Warden: `FootworkRule.StepIn` with the mark left to the dash, so only at
    half health or more and with the band dark. It presses as the blow comes due within the guard less a frame
    ((1 - progress) x 1.1 s <= 0.3 s - 1/60), out along `EscapeMark`'s heading. With the dash cooling it walks out
    early.
  - **Out of a surround:** four or more enemies within their own reach and the hero under half its health. It dashes out
    along the kite's heading.
- **The variants are levers of the hero's kit**, each default the proposal:
  - (a) Quicksilver Stride on every weapon. The primed crit goes to whichever weapon strikes first, and every weapon's
    crits wound, a pool a weapon.
  - (b) Sever in the dash's place: 18 s, on the heaviest enemy within 3 units that is not the horde's. Its mark holds
    3 s: every weapon's landing on it deals +30 % (the Daggers' options' D), and x2 on that below 30 % of its health
    (E). The extra is the ability's work. It deals nothing itself, so nothing falls to it outside the execute. The
    Warden is marked and executed as any enemy.
  - (c) health 90; (d) no active at all, R_skill's denominator.
  - Section 4's others, careful only: the wound at 0.4 and 0.8, the dash at 12 s, health 80, and the dash as an escape
    only. Also a hold at the Warden's feet whenever the dash is ready: section 2's estimate assumed it; step 6's rule
    does not.
- `Tools/CombatChecks/RiftbladeResearch` (explicit). `TheRiftbladeOverASeedBatch` writes riftblade.md, riftblade.csv (a
  row a cell) and riftblade-runs.csv (a row a run). `OneRiftbladeSeedAsTheTelemetryWouldWriteIt` traces one seed.
- `RiftbladeSimulationTests` (13, in CombatChecks):
  - the pool pays every wound in full, a refresh loses nothing, thirty frames are a tick;
  - the stride's walk and clock; the dash's 3 units in 12 frames and its 18 guarded frames; Sever's threshold; the
    pilot's two dash rules; the kit's defaults;
  - the Vanguard and Ilya reach none of it;
  - on the simulated floor: the stride and the wounds on the Daggers alone, the floor's accounting adding up with the
    bleed; the dash, its hold and its guard, with either hands, and none without an active or for the auto-farmer;
    Sever; the passive on every weapon.
- **The default batch is byte-identical.** `FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`, base ba11a36f against
  this branch, the same Editor files. foundry-batch.csv, foundry-batch-floor2.csv, foundry-cohorts-floor2.csv,
  foundry-gate.md and foundry-cohorts.md are the same to the byte. foundry-batch.md differs in its date and its
  wall-clock seconds only (TestResults/riftblade-1111/main-default and after-default).

### How it was measured

Every number in this section's tables was measured on **main ba11a36f** (the floor before cebf8775's forge-walk fix; the
re-read on cebf8775 is at the section's end). `RiftbladeResearch`: seeds 1-200 a hero, both floors (a floor-1 clear goes
on to floor 2), every cohort, frame-perfect and with `FOUNDRY_REALISM=typical` ("human hands"), `FOUNDRY_PARALLEL=6`;
TestResults/riftblade-1111/full.
- The Vanguard and Ilya are the same harness's runs. Each is also played by the careful pilot less the ability's habit
  (careful-no-ability), its own R_skill's denominator.
- The holdout: seeds 201-400, the careful cohort, both hands (holdout/). The hold whenever the dash is ready, every
  cohort (hold-ready/; it replaces full/'s careful-only row of it, which an earlier build of the lever wrote).
- Read differences under about 10 of 200 as noise, as docs/41 does. Cells: frame-perfect ; human hands unless a table
  says otherwise.

### The clears

Of 200: careful / standing / first-card / clumsy / auto-farmer.

| Hero | Floor 1, frame-perfect | Floor 1, human hands | Both floors, frame-perfect | Both floors, human hands |
|---|---|---|---|---|
| Vanguard | 200 / 195 / 196 / 106 / 11 | 198 / 181 / 183 / 81 / 9 | 200 / 180 / 196 / 99 / 2 | 192 / 162 / 177 / 65 / 3 |
| Ilya | 200 / 195 / 200 / 183 / 36 | 198 / 179 / 197 / 138 / 29 | 200 / 176 / 200 / 164 / 6 | 195 / 156 / 192 / 109 / 3 |
| **Nix as proposed** | 200 / 192 / 193 / 85 / 14 | 198 / 183 / 158 / 45 / 12 | 200 / 178 / 193 / 74 / 2 | 198 / 166 / 155 / 29 / 1 |
| (a) the passive on every weapon | 200 / 192 / 193 / 77 / 10 | 199 / 184 / 158 / 51 / 11 | 200 / 184 / 193 / 74 / 3 | 199 / 173 / 155 / 34 / 2 |
| (b) Sever | 200 / 195 / 195 / 80 / 14 | 192 / 178 / 159 / 45 / 12 | 200 / 182 / 195 / 68 / 2 | 190 / 162 / 153 / 33 / 1 |
| (c) health 90 | 200 / 194 / 194 / 87 / 15 | 199 / 185 / 161 / 43 / 12 | 200 / 183 / 194 / 75 / 2 | 199 / 172 / 156 / 27 / 3 |
| (d) no active | 200 / 187 / 193 / 81 / 14 | 196 / 173 / 150 / 44 / 12 | 200 / 174 / 193 / 70 / 2 | 189 / 159 / 139 / 32 / 1 |
| The hold whenever the dash is ready | 200 / 192 / 193 / 84 / 14 | 194 / 185 / 155 / 42 / 12 | 200 / 179 / 193 / 67 / 2 | 193 / 166 / 151 / 31 / 1 |

The holdout (seeds 201-400, careful, floor 1 / both floors, human hands; frame-perfect every hero clears 200 / 200):
Vanguard 196 / 194, Ilya 200 / 197, Nix 198 / 195, (a) 199 / 198, (b) 197 / 196, (c) 197 / 196, (d) 194 / 192, the hold
whenever ready 199 / 195. The careful-only levers, seeds 1-200 (frame-perfect 200 / 200 each): the wound at 0.4 198 /
195 and at 0.8 200 / 200, the dash at 12 s 197 / 196, health 80 196 / 196, the dash as an escape only 197 / 195.

### The Warden

The careful cohort's felled fights, P50 (P90), floor 1 ; floor 2 (its own fight, the escort apart). The gate's band is
45-65 s, docs/40 section 8.8's for the Riftblade 35-55 s. No fight of any cell fell within 5 s: the shortest of the
proposal's is 36 / 30 s.

| Hero | Frame-perfect | Human hands | Holdout, frame-perfect | Holdout, human hands | Died at the Warden, floor 1 ; 2 (fp , hh) |
|---|---|---|---|---|---|
| Vanguard | 56 (68) ; 58 (70) | 51 (65) ; 54 (64) | 58 (69) ; 58 (71) | 52 (63) ; 54 (66) | 0 ; 0 , 1 ; 3 |
| Ilya | 50 (60) ; 52 (63) | 45 (56) ; 49 (61) | 51 (61) ; 53 (62) | 48 (57) ; 49 (60) | 0 ; 0 , 2 ; 2 |
| **Nix as proposed** | 53 (65) ; 52 (66) | 49 (61) ; 51 (62) | 53 (66) ; 54 (66) | 50 (60) ; 51 (61) | 0 ; 0 , 1 ; 0 |
| (a) every weapon | 43 (54) ; 44 (54) | 43 (51) ; 44 (54) | 45 (54) ; 45 (55) | 43 (50) ; 44 (53) | 0 ; 0 , 1 ; 0 |
| (b) Sever | 51 (66) ; 54 (66) | 49 (58) ; 50 (62) | 53 (65) ; 54 (66) | 49 (58) ; 51 (62) | 0 ; 0 , 8 ; 2 |
| (c) health 90 | 53 (65) ; 52 (66) | 49 (61) ; 51 (62) | 53 (66) ; 54 (66) | 50 (60) ; 51 (62) | 0 ; 0 , 0 ; 0 |
| (d) no active | 57 (69) ; 58 (69) | 51 (62) ; 53 (65) | 59 (71) ; 58 (71) | 51 (63) ; 54 (66) | 0 ; 0 , 3 ; 5 |
| The hold whenever ready | 48 (59) ; 49 (62) | 45 (58) ; 46 (56) | 48 (59) ; 49 (62) | 46 (56) ; 46 (58) | 0 ; 0 , 5 ; 0 |
| The dash at 12 s | 56 (69) ; 56 (69) | 50 (62) ; 50 (62) | - | - | 0 ; 0 , 2 ; 0 |
| The dash as an escape only | 60 (73) ; 58 (77) | 52 (64) ; 53 (65) | - | - | 0 ; 0 , 2 ; 2 |

### R_skill

The careful cohort's own damage a second on the Warden, with its active over without: every weapon, burn, bleed and
ability, the pit apart, over the fight after the reveal. P50 with / without = R, with a bootstrap's 95 % for the ratio
on seeds 1-200. The denominators:
- the Vanguard and Ilya: careful-no-ability;
- the Riftblade's variants: Nix with no active;
- the passive on every weapon: itself with no active.

docs/40's targets: Vanguard 1.05-1.15x, Ilya 1.35-1.6x, Riftblade 1.6-2.0x.

| Hero | Floor 1, frame-perfect | Floor 1, human hands | Floor 2, fp ; hh | Holdout, floor 1 , 2: fp ; hh |
|---|---|---|---|---|
| Vanguard (Forge Burst) | 53.1 / 51.3 = 1.04x [0.99-1.08] | 56.3 / 54.2 = 1.04x [0.99-1.10] | 1.03x ; 1.06x | 1.01x , 1.01x ; 1.03x , 1.01x |
| Ilya (Crucible Vent) | 61.2 / 60.4 = 1.01x [0.97-1.05] | 66.0 / 64.8 = 1.02x [0.98-1.07] | 0.99x ; 0.97x | 1.02x , 1.02x ; 1.00x , 1.05x |
| **Nix, Phase Dash** | 56.4 / 51.9 = 1.09x [1.02-1.15] | 58.1 / 56.7 = 1.03x [0.97-1.09] | 1.02x ; 0.96x | 1.10x , 1.02x ; 1.01x , 0.98x |
| (a) every weapon, the dash | 71.5 / 66.8 = 1.07x [1.02-1.13] | 71.4 / 71.4 = 1.00x [0.95-1.06] | 1.05x ; 0.97x | 1.07x , 1.01x ; 1.03x , 0.95x |
| (b) Sever | 56.0 / 51.9 = 1.08x [1.03-1.16] | 59.6 / 56.7 = 1.05x [1.00-1.11] | 1.10x ; 1.10x | 1.11x , 1.10x ; 1.07x , 1.05x |
| (c) health 90 | 1.09x | 1.03x | 1.02x ; 0.97x | 1.10x , 1.02x ; 1.02x , 0.99x |
| The hold whenever ready | 62.7 / 51.9 = 1.21x [1.14-1.27] | 65.0 / 56.7 = 1.15x [1.10-1.20] | 1.21x ; 1.12x | 1.22x , 1.17x ; 1.17x , 1.14x |
| The dash at 12 s | 1.00x | 1.04x | 1.01x ; 1.00x | - |
| The dash as an escape only | 0.96x | 0.99x | 0.99x ; 0.98x | - |
| The wound at 0.4 ; at 0.8 | 1.06x ; 1.09x | 1.03x ; 1.04x | 1.03x , 1.03x ; 1.00x , 1.03x | - |

- **Nix's Daggers alone on the Warden** are no ratio. Frame-perfect they deal it 4.5 a second with the dash (1.4 of it
  the bleed) and 0 without; with human hands 8.3 against 6.7, since a human's kite drifts within their reach.
- **The floor's damage a second** is 1.00x for every hero and variant: the horde comes as it comes, and every hero kills
  it.
- **A Captain's time under fire**, P50 without / with (careful, frame-perfect): Vanguard 1.43 / 1.08 s (the Burst), Ilya
  1.25 / 1.27, Nix 1.13 / 1.12, Sever 1.13 / 1.04.

### The elites

The elite rooms' Grunt Captains felled on either floor: walking in to falling P50 ; under fire P50, frame-perfect ,
human hands. The careful cohort's P90 is 2.5 s or less, the longest 13 s (clumsy), and every Captain fell within docs/40
13.1's 30 s.

| Hero | careful | standing | clumsy | auto-farmer |
|---|---|---|---|---|
| Vanguard | 1.4 ; 1.08 , 1.7 ; 1.37 | 5.1 ; 2.45 , 5.1 ; 2.53 | 6.5 ; 3.07 , 6.5 ; 3.25 | 2.5 ; 1.80 , 2.6 ; 1.80 |
| Ilya | 1.6 ; 1.27 , 1.8 ; 1.50 | 5.1 ; 2.47 , 5.2 ; 2.52 | 5.8 ; 3.30 , 6.0 ; 3.35 | 2.5 ; 1.40 , 2.5 ; 1.33 |
| **Nix as proposed** | 1.4 ; 1.12 , 1.7 ; 1.37 | 5.1 ; 2.45 , 5.2 ; 2.57 | 5.5 ; 2.80 , 5.9 ; 3.33 | 2.5 ; 1.30 , 2.5 ; 1.23 |
| (a) every weapon | 1.4 ; 1.10 , 1.6 ; 1.28 | 5.0 ; 2.50 , 5.1 ; 2.58 | 5.7 ; 3.12 , 5.7 ; 3.52 | 2.4 ; 1.40 , 2.4 ; 1.49 |
| (b) Sever | 1.4 ; 1.04 , 1.6 ; 1.25 | 4.9 ; 2.45 , 4.9 ; 2.32 | 5.5 ; 3.21 , 5.7 ; 3.27 | 2.5 ; 1.30 , 2.5 ; 1.23 |
| (d) no active | 1.5 ; 1.13 , 1.7 ; 1.37 | 5.2 ; 2.53 , 5.2 ; 2.57 | 5.5 ; 2.67 , 5.8 ; 3.28 | 2.5 ; 1.30 , 2.5 ; 1.23 |

No Captain of any cell, the holdout included, fell within a frame of its first damage: the primed crit on the elite
share (x4 on a strike) and its wound one-shot nothing.

### Damage by job and by weapon

The careful cohort, floor 1: each weapon's own damage a second, P50 of the runs that held it. The horde is a second it
was held, the Captains a second of the elite rooms' fights, the Warden a second of its fight after the reveal. In
brackets is the part its wounds paid. The Warden's health is the mean share over the felled fights, by what took it.
Every Nix run held all three weapons by the gate.

| Hero | Daggers: horde / Captains / Warden | Sword | Staff | The Warden's health |
|---|---|---|---|---|
| Vanguard, fp | 1.3 / 31.3 / 0.0 | 8.3 / 12.7 / 26.8 | 30.4 / 16.5 / 23.4 | Sword 43 %, Staff 36 %, Daggers 1 %, ability 2 %, pit 18 % |
| Vanguard, hh | 1.3 / 25.6 / 3.8 | 8.4 / 13.0 / 27.6 | 30.2 / 18.8 / 23.3 | Sword 39 %, Staff 33 %, Daggers 5 %, ability 2 %, pit 20 % |
| Ilya, fp | 1.2 / 30.2 / 0.0 | 6.9 / 13.3 / 26.7 | 31.4 / 18.3 / 23.2 | Sword 37 %, Staff 31 %, burn 12 %, ability 2 %, pit 18 % |
| **Nix, fp** | 2.0 / 33.2 (2.3) / 4.5 (1.4) | 7.9 / 11.1 / 28.2 | 30.7 / 14.9 / 22.9 | Sword 41 %, Staff 34 %, Daggers 5 %, bleed 2 %, pit 19 % |
| **Nix, hh** | 1.9 / 27.3 (2.1) / 8.3 (2.7) | 7.6 / 11.2 / 26.5 | 30.7 / 16.8 / 22.5 | Sword 36 %, Staff 31 %, Daggers 8 %, bleed 4 %, pit 21 % |
| (a) every weapon, fp | 1.6 / 29.4 (1.0) / 3.7 (0.9) | 7.9 / 13.4 / 36.1 (6.8) | 30.4 / 19.1 (0.4) / 29.1 (4.0) | Sword 36 %, Staff 30 %, Daggers 3 %, bleed 14 %, pit 17 % |
| (b) Sever, fp | 2.1 / 25.3 (1.6) / 0.0 | 7.6 / 8.6 / 27.0 | 30.6 / 12.2 / 22.5 | Sword 39 %, Staff 33 %, Daggers 1 %, ability 9 %, pit 18 % |
| (d) no active, fp | 2.1 / 33.2 (2.3) / 0.0 | 7.9 / 11.1 / 27.8 | 31.0 / 14.9 / 23.5 | Sword 43 %, Staff 36 %, Daggers 1 %, pit 19 % |
| The hold whenever ready, fp | 2.0 / 33.2 (2.3) / 12.2 (3.4) | 7.8 / 11.1 / 27.6 | 30.4 / 14.9 / 22.8 | Sword 36 %, Staff 30 %, Daggers 12 %, bleed 4 %, pit 18 % |

In Nix's runs the Daggers lead the elite job: 33 a second on the Captains against the Staff's 15 and the Sword's 11. The
Staff keeps the horde and the Sword the Warden. The gate's roles row reads the Vanguard and does not move.

### Deaths, floor time, the passive and the dash at work

Deaths by place, floor 1, of 200: halls / elite rooms / the gate's wait / the Warden.

| Cohort | Vanguard, fp ; hh | Ilya, fp ; hh | Nix, fp ; hh | Nix with no active, fp ; hh |
|---|---|---|---|---|
| careful | 0 / 0 / 0 / 0 ; 1 / 0 / 0 / 1 | 0 / 0 / 0 / 0 ; 0 / 0 / 0 / 2 | 0 / 0 / 0 / 0 ; 1 / 0 / 0 / 1 | 0 / 0 / 0 / 0 ; 1 / 0 / 0 / 3 |
| standing | 1 / 0 / 0 / 4 ; 1 / 0 / 0 / 18 | 0 / 0 / 0 / 5 ; 0 / 0 / 0 / 21 | 1 / 0 / 0 / 7 ; 1 / 0 / 0 / 16 | 5 / 0 / 0 / 8 ; 4 / 0 / 0 / 23 |
| first-card | 4 / 0 / 0 / 0 ; 15 / 0 / 0 / 2 | 0 / 0 / 0 / 0 ; 0 / 0 / 0 / 3 | 6 / 0 / 1 / 0 ; 40 / 0 / 0 / 2 | 6 / 0 / 1 / 0 ; 40 / 0 / 0 / 10 |
| clumsy | 65 / 6 / 2 / 21 ; 90 / 0 / 5 / 24 | 1 / 0 / 1 / 15 ; 13 / 1 / 1 / 47 | 100 / 2 / 0 / 13 ; 130 / 0 / 1 / 24 | 102 / 2 / 0 / 15 ; 130 / 0 / 2 / 24 |
| auto-farmer | 81 / 0 / 1 / 107 ; 93 / 0 / 0 / 98 | 1 / 0 / 1 / 162 ; 3 / 0 / 0 / 168 | 116 / 0 / 1 / 69 ; 115 / 0 / 0 / 73 | 116 / 0 / 1 / 69 ; 115 / 0 / 0 / 73 |

- **Floor 2's deaths** are few for every hero: Nix's careful 0 ; 0 and standing 14 ; 17, all at the Warden, against the
  Vanguard's 15 ; 17.
- **Floor time**, the careful clears' P50, floor 1 ; floor 2 ; both: Vanguard 291 ; 143 ; 433 s (fp) and 284 ; 138 ; 422
  (hh), Ilya 283 ; 136 ; 419 and 278 ; 133 ; 410, Nix 287 ; 133 ; 421 and 282 ; 133 ; 416. Nix with no active takes 292
  ; 142 ; 434 and 284 ; 137 ; 423, the passive on every weapon 277 ; 125 ; 403 and 275 ; 124 ; 399.
- **The primed crits** come 7.1 / 8.2 a minute for the careful cohort and 7.3 / 7.1 for the standing one. The clumsy
  one, which lets the horde close, gets 15.5 / 16.2. On every weapon they come 24-27 a minute.
- **The wounds** go the other way: careful 7.0 / 9.4 a minute, standing 13.4 / 12.9, clumsy 17.1 / 15.4. The bleed is
  0.8 / 1.4 % of the careful hero's damage (4.8 % on every weapon).
- **The dash, careful.** It comes 4 / 3 times a floor and holds at the Warden's feet 2.3 / 2.1 s a fight. Its guard
  turns 1.1 / 2.8 hits a floor.
  - Frame-perfect, the dash leaves the mark before the blow: 11 slams in 400 floors came down on the hero, all turned.
  - With human hands the press comes about two frames before the blow (a 0.25 s reaction after the moment it was due),
    and the guard carries the escape. 79 % of the 1041 slams that came down on a held hero were turned.
  - A floor-1 Warden fight lands 0.67 slams on Nix, against 0.89 with no active, 0.91 on the Vanguard and 0.81 on Ilya.
- **The auto-farmer at the Warden.** Of its runs that reached floor 1's Warden, 69 of 83 died there (83 %) frame-perfect
  and 73 of 85 (86 %) with human hands, against the Vanguard's 107 of 118 (91 %) and 98 of 107 (92 %). It reaches the
  Warden less often, since it dies in the halls.

### docs/46's acceptance, row by row

| Row | Measured | Verdict |
|---|---|---|
| No regression | the default batch to the byte (above); CombatChecks 763 (750 + 13), UnityCompileCheck clean; Unity's suites not run | met |
| Clears, careful | 200 / 198 against the Vanguard's 200 / 198 (floor 1), 200 / 198 against 200 / 192 (both); holdout 200 / 198 and 200 / 195 against 200 / 196 and 200 / 194 | met |
| Clears, the weaker cohorts | clumsy 85 / 45 against the Vanguard's 106 / 81; first-card 193 / 158 against 196 / 183; standing 192 / 183 against 195 / 181; auto-farmer 14 / 12 against 11 / 9 | met for clumsy and first-card; standing (hh) and the auto-farmer a few runs above, within noise |
| The walk | primed crits a minute, careful 7.1 / 8.2 against standing 7.3 / 7.1 | reported: the passive does not read the walk |
| R_skill 1.6-2.0x | 1.09x / 1.03x (floor 1), 1.02x / 0.96x (floor 2); the hold whenever ready 1.21x / 1.15x | not met; nothing measured reaches section 2's 1.3-1.5x |
| Elites | a Captain falls P50 1.4 / 1.7 s from walking in, 1.12 / 1.37 s under fire, none within a frame | met |
| Warden | 53 / 49 s on floor 1, 52 / 51 s on floor 2 (holdout 53 / 50, 54 / 51); the shortest 36 / 30 s | met |
| Boss'ta zorlanmalı | the auto-farmer dies at the Warden 83 / 86 % against the Vanguard's 91 / 92 %; floor 1 cleared 14 / 12 against 11 / 9 | not met, by a few runs: it dies in the halls first |
| Roles | the gate's row reads the Vanguard and is byte-identical; Nix's Daggers 33 / 27 a second on the Captains, 5-8 % of the Warden's health and the bleed 2-4 % | reported |
| Parity, device | no Editor run, no scene build | not measured |

### docs/46's questions 2-4: what the numbers say

**Question 2, the active.**
- **Phase Dash as proposed adds little damage.** R_skill is 1.09x / 1.03x on floor 1 and 1.02x / 0.96x on floor 2. Its
  worth is the escape, with human hands (seeds 1-200 on ba11a36f ; the same seeds on cebf8775, below):
  - a third fewer slams land a floor-1 Warden fight: 0.67 ; 0.68 against 0.89 ; 0.99 with no active (holdout 0.70
    against 0.96);
  - the first-card cohort clears both floors 155 ; 154 times with it and 139 ; 141 without, the standing one 166 ; 164
    and 159 ; 156;
  - the careful one 198 ; 193 and 189 ; 192 (holdout 195 and 192): within noise.
- **The dash at 12 s or as an escape only** loses the little damage the dash adds (1.00x, 0.96x frame-perfect) and keeps
  the clears (197 / 196, 197 / 195). The 9 s and the hold earn their place.
- **Sever adds damage of the dash's size:** 1.08x / 1.05x on floor 1, 1.10x / 1.10x on floor 2, 8-9 % of the Warden's
  health as its extra, a Captain 1.04 s under fire against 1.13. It saves no one: with human hands the careful clears
  are 192 / 190 and the careful Nix dies at floor 1's Warden 8 times (3 with no active, 1 with the dash).
- So Phase Dash is right as the one active, as a defence. Sever later would not raise R_skill much.
- **The dash's heading.** Only the stick's was measured: the autopilot holds the escape's heading and the dash takes it.
  A heading toward the Warden would be a closing-in. Its nearest measured stand-in is the hold at the Warden's feet
  whenever the dash is ready (question 4); a dash spent on closing in is not there to leave the mark.

**Question 3, the passive's reach.**
- **On the Daggers alone the passive is small.** The Daggers seldom reach what the Sword and the Staff have not felled:
  2 a second on the horde. So the primed crit comes 7-8 times a minute in every hand, and it does not read the walk. The
  wounds favour the cohorts that let enemies close. The Warden stays in the band (53 / 49 s), and the elite job is the
  Daggers' (33 / 27 a second on the Captains).
- **On every weapon** the primed crits come 24-27 a minute and the bleed takes 14 % of the Warden. The Sword deals the
  Warden 36 a second against 28, and the fight falls to 43 / 43 s (holdout 45 / 43): under the gate's 45 s with either
  hands, inside docs/40's 35-55 s for this hero. The clears barely move, and the dash's R_skill over the same passive is
  1.07x / 1.00x.
- So the Daggers alone, as proposed. The passive on every weapon would need the Warden's health share re-measured for
  this hero.

**Question 4, which binds.**
- **The two do not trade against each other as section 2 estimated.** The gate's 45-65 s holds for the proposal (53 /
  49 s). R_skill on the hero's whole damage on the Warden is 1.0-1.1x, not 1.3-1.5x.
- **No variant reaches 1.6x.** The strongest is the hold at the Warden's feet whenever the dash is ready: 1.21x / 1.15x
  (holdout 1.22x / 1.17x), and a 48 / 45 s fight, at the band's floor with human hands.
- **Why.** The careful pilot kites the Warden at 2.0 units, where the Sword (2.1) and the Staff (2.4) reach it and the
  Daggers (1.8) do not. The Sword and the Staff take three quarters of its health whether or not Nix dashes. The dash
  adds only the Daggers' seconds at its feet:
  - 2.1-2.3 s a fight as proposed, the Daggers 4.5 a second over the fight;
  - 6.6 s and 12.2 a second with the hold whenever ready.
- **Section 2's estimate** assumed the hold whenever the dash is ready (a quarter of the fight); step 6's rule holds
  through the telegraph alone.
- **The same reading finds every hero's active small:** the Vanguard's Forge Burst 1.04x, Ilya's Crucible Vent 1.01x.
  docs/40's 1.05-1.15x and 1.35-1.6x are not met either, so R_skill on the Warden cannot separate the heroes' actives in
  this harness.
- **On Nix's Daggers alone R_skill is no ratio:** 4.5 a second against 0 (frame-perfect).
- So let the gate's 45-65 s bind. Read the Riftblade's skill 4 where it shows: in the weaker cohorts' clears, and in the
  slams the dash turns with human hands.

**Where the Riftblade sits in docs/40's skill order (Vanguard 1, Ilya 3, Riftblade 4).** On clears in weaker hands Nix
sits above the Vanguard:
- the clumsy cohort clears floor 1 with Nix 85 / 45 times, against the Vanguard's 106 / 81 and Ilya's 183 / 138;
- the first-card cohort, with human hands, 158 against 183 and 197;
- the standing and auto-farmer cohorts clear about what the Vanguard's do;
- its clumsy and auto-farmer runs die in the halls (100 / 130 and 116 / 115 of 200), fragile under the horde as docs/40
  makes it.

Ilya stays the exception docs/46 noted, below the Vanguard. By R_skill there is no order to read: 1.0-1.1x for all
three.

### Recommendation

1. **Build the proposal as it stands** once the owner and the device row allow: 85 health, the Daggers, Quicksilver
   Stride on the Daggers alone, Phase Dash at 9 s with the late escape. It meets the gate's Warden band with either
   hands, the careful clears and the elite quest, and it is the hardest of the three heroes in weaker hands.
2. **Rewrite section 4's R_skill row before the build.** Read R_skill as measured (about 1.1x on the Warden), beside the
   dash's defence with human hands: a third fewer slams land, and the standing and first-card cohorts clear both floors
   7-16 more times. Hold skill 4 to the clumsy and first-card clears below the Vanguard's. docs/40's 1.6-2.0x needs the
   Daggers to carry the Warden, and that takes the Warden from the Sword.
3. **Keep the passive on the Daggers.** On every weapon the Warden falls under 45 s.
4. **Leave Sever out** of the first release: it adds damage of the dash's size, no survival, and a second button.
5. **Keep 85 health.** 90 and 80 move nothing beyond noise (careful 199 / 199 and 196 / 196 with human hands, clumsy
   43-45).
6. **For the owner:** the hold at the Warden's feet whenever the dash is ready is the pilot's footwork, a player's by
   hand, not a rule. It raises R_skill to 1.2x and takes the fight to 48 / 45 s, the band's floor with human hands.
7. **Read the device before the R_skill and Warden rows are believed**, as section 4 says: the autopilot's press is
   timed from the telegraph's progress, and a thumb's is not.

### What the scene build would need

Section 3's steps stand. Measuring them in the simulation made these points precise or changed them:
- **Bleed (step 1).** "The pool spread over the time left" is six ticks a wound. Each wound sets the ticks left to six,
  each tick pays the pool over the ticks left, and the last pays what is left (`BleedState` as it stands). A fresh
  status first ticks the frame after its first wound.
- **Momentum (step 2).**
  - The hero starts with the 1.2 s run out, and neither the walk nor the clock banks past priming.
  - The primed crit belongs in `WeaponRuntime.TryAttack` (rhythm, primed, rolled), as step 2 says. The simulation's
    stand-in goes when TryAttack has it: it doubles the strike's own landings in `FoeDamaged` and throws a primed crit's
    Rift Echo splinter after the pulse. The splinter then comes before the pulse, as every crit's does.
- **Phase Dash (step 3).**
  - The press is taken at the next walk step along the stick.
  - With human hands it comes about two frames before the blow, so the guard, not the travel, carries the escape. The
    guard has to be read wherever a hit lands, in `HealthState`, as step 3 says.
- **The autopilot (step 6).** The hold is `FootworkRule.StepIn` with the mark passed as clear and the Daggers' own reach
  (not `BestSingleTargetReach`, which picks the Sword on the Warden). The press is due at
  (1 - progress) x Telegraph <= GuardSeconds less a frame. These bodies (`DashFootwork`) join FootworkRule.
- **Telemetry (step 7).** Beside `dash_turned` and `dash_slams_turned`, the seconds held at the Warden's feet
  (`dash_hold_sec`). A device session can then read a thumb's hold and press against these numbers.
- **If the owner takes the passive on every weapon:** a pool a weapon (as measured) or one pool with its ticks split by
  weapon, and the Warden's health share measured again for this hero.
- **Step 5's parity pins** then hold `Riftblade()` and the rules to the assets. The simulation's research levers
  (`RiftbladeKit`'s variants) stay research.

### Re-read on main cebf8775

Main moved to cebf8775 while this was measured, and this branch merges it. Three changes came with it:
- the walk from the forge to the next hall is the horde's again: the horde and the floor's clock run on it, and floor 1
  is about 9 s shorter;
- the step-in counts the Daggers' share on a Captain toward the best weapon on it;
- the gate's and the elite room's wave flags go with the wave (`ReleaseWave`).

**The default batch is byte-identical to cebf8775's.** `FOUNDRY_LEVERS=0 FOUNDRY_COHORTS=1 FOUNDRY_FLOORS=2`, the same
Editor files (TestResults/riftblade-1111/cebf-main-default and cebf-after-default). The three CSVs, foundry-gate.md and
foundry-cohorts.md are the same to the byte; foundry-batch.md differs in its date and wall-clock seconds only.
CombatChecks 764 (751 + 13) and UnityCompileCheck clean on the merge.

**The shipped heroes and the proposal again** (seeds 1-200, both floors, every cohort, both hands;
TestResults/riftblade-1111/cebf8775). Of 200: careful / standing / first-card / clumsy / auto-farmer.

| Hero | Floor 1, frame-perfect | Floor 1, human hands | Both floors, frame-perfect | Both floors, human hands |
|---|---|---|---|---|
| Vanguard | 200 / 190 / 196 / 108 / 9 | 195 / 184 / 182 / 78 / 10 | 200 / 173 / 196 / 101 / 2 | 194 / 159 / 178 / 66 / 2 |
| Ilya | 200 / 194 / 200 / 182 / 36 | 199 / 181 / 197 / 131 / 29 | 200 / 185 / 200 / 165 / 3 | 195 / 159 / 197 / 105 / 4 |
| **Nix as proposed** | 200 / 191 / 192 / 81 / 9 | 196 / 183 / 159 / 44 / 13 | 200 / 182 / 192 / 66 / 4 | 193 / 164 / 154 / 28 / 1 |
| (d) no active | 200 / 188 / 193 / 79 / 9 | 194 / 172 / 151 / 36 / 13 | 200 / 179 / 193 / 67 / 4 | 192 / 156 / 141 / 26 / 1 |

| Hero | Warden P50 (P90), floor 1 ; 2, fp | The same, hh | Careful floor-1 clear P50, fp ; hh | R_skill floor 1 ; 2, fp | The same, hh |
|---|---|---|---|---|---|
| Vanguard | 55 (70) ; 57 (70) | 51 (62) ; 54 (66) | 282 ; 276 s | 1.03x ; 1.00x | 1.03x ; 1.04x |
| Ilya | 49 (60) ; 51 (63) | 46 (57) ; 48 (61) | 275 ; 270 s | 0.99x ; 1.03x | 1.01x ; 1.01x |
| **Nix as proposed** | 53 (65) ; 53 (67) | 51 (60) ; 51 (61) | 278 ; 274 s | 1.10x ; 1.02x | 1.03x ; 0.99x |
| (d) no active | 57 (70) ; 57 (70) | 51 (63) ; 53 (66) | 283 ; 276 s | - | - |

What holds, within noise:
- the Warden in the gate's band, the proposal 53 / 51 s;
- R_skill about 1.1x, the dash's own (1.10x / 1.03x on floor 1);
- the order in weaker hands: the clumsy cohort clears floor 1 with Nix 81 / 44 times against the Vanguard's 108 / 78,
  and the first-card cohort with human hands 159 against 182;
- the elites: a careful Captain falls P50 1.4 / 1.8 s from walking in, and no Captain of Nix's fell within a frame;
- the dash's defence: 0.68 slams land a floor-1 Warden fight against 0.99 without it.

What moved:
- every floor is 8-9 s shorter (Nix's careful floor 1 278 / 274 s against 287 / 282), as main's fix makes it;
- the dash's gain for the careful cohort with human hands is 193 against 192 over both floors, the noise question 2
  reads it as;
- the auto-farmer's floor-1 clears are 9 / 13 against the Vanguard's 9 / 10. It dies at the Warden 89 / 85 % of the
  times it reaches it, against 92 / 91 %: the "boss'ta zorlanmalı" row stays short by a few runs.

The variants were not run again on cebf8775: the forge-walk fix moves every hero alike, and the proposal and its
denominator moved within noise.
