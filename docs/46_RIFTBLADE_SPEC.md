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
| R_skill | 1.60-2.00x (docs/40 4.2): the careful cohort with the dash over the same cohort without it, the hero's own damage a second on the Warden (every weapon and the bleed, the pit apart), P50 | the Daggers' own and the elite enemies beside; section 2's estimate is 1.3-1.5x |
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
