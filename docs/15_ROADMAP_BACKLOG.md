# Roadmap & Backlog

## Platform material checkpoint — 2026-09-17

- [x] Painted basalt V1/V2, coping and near-face masters; selected quieter V2 with actual Unity centre/rim/detail/overview comparisons (`platform-material-01`). Isolated proof only.
- [x] Resolve coping corners, old pixel outline and near-wall/lower-keel joins (platform-runtime-01; lower keel artwork itself remains provisional).
- [x] Integrate accepted materials with reduced-size/compression checks, shared ownership, reload tests and physical density/near-rim movement validation (2026-09-18, latest Docs/21–23).
- [ ] Match remaining corner fixtures, crystal/keel, chest and ability indicator; preserve combat readability. Separate boss-room geometry remains later.

## Current art checkpoint — 2026-09-16

Latest status, 2026-09-17: Vanguard front/rear runtime poses, Sword grip polish and Mite runtime poses/effects are integrated and S23-checked (latest Docs/21–23). The older unchecked pair milestones below retain their final-quality requirements: strict foot locking and matching Staff/Daggers are still open.

- [x] Grunt front/rear design masters and browser scale/close-pair study (`ArtDirection/2026-09-17/grunt-proof-01/`). Historical static proof; superseded by the runtime checkpoint below.
- [x] Grunt six-pose-per-side blocking, common registration/sampling and real Unity before/after/close-pair proof (`grunt-motion-01`). This proves sampling and reviewability, not final planted-foot animation or a runtime import.
- [x] Grunt registered four-direction runtime imports, opposing passing poses, immediate strike/flash/death and Unity validation (`grunt-polish-01`; physical evidence in latest Docs/23).
- [ ] Final Grunt planted-foot/volume/lighting polish and crowded depth readability.

- [x] First hero/Mite concept pair and browser scale/crowd review (`ArtDirection/2026-09-16/character-proof-01/`).
- [x] Compare reduced sprite resolution/filtering in Unity at fixed world size (`character-unity-01`, 32/64/128 PPU). 128/bilinear is a candidate; current shipped 32-PPU actor contract remains.
- [x] Matching sword/buckler masters and a registered hero keypose blocking proof (`hero-motion-01`, completed 2026-09-17).
- [ ] Complete passing/down and strike/recovery poses; accept planted-foot gait, grip occlusion and opposing facings before production integration.
- [ ] Prove final sprite imports, compression and cached ownership on device; the isolated GPU sampling review is not production integration.
- [ ] Prove pivots, equipment anchors, opposing facings and a short animation sequence for that pair, then verify on device.
- [ ] Expand the accepted pair into the production roster. Concept masters are not completion of this item.

## Phase 0 — Pre-production (1–3 focused days)

Deliverables:
- vision agreed,
- portrait confirmed,
- core loop sketched,
- Unity project created,
- Git initialized,
- placeholder style chosen,
- analytics event plan documented.

Do NOT:
- create final hero roster,
- create 100 icons,
- build backend.

## Phase 1 — Mechanical prototype

### Must
- one scene,
- one hero,
- one enemy,
- auto-target,
- attack,
- HP/death,
- reward,
- upgrade choice,
- restart run.

### Then (order accepted 2026-09-12, see `24`)
- 3 enemy archetypes (done 2026-09-13: Grunt, Runner, Tank),
- floor 1: six-room template, one boss, result screen with cause of death (done 2026-09-13: Ember Halls, see `21`),
- floor 2: second modifier and scaling tier, Extract/Descend checkpoint (done 2026-09-13: Quicksilver Vaults with Cursed Gold, gold at risk, see `21`),
- the Forge meta layer with unlocks, two relics and local save (done 2026-09-13: Relic Forge with Second Wind and Counterweight, versioned JSON profile, see `21`),
- 3 weapon behaviors (owner choice 2026-09-13: packs of enemies first, then Sword cleave, Staff area damage and Daggers with crits, Staff and Daggers unlocked in the Forge; done 2026-09-13, see `21`),
- pause control (owner feedback 2026-09-13: a run starts at once and only choice panels or leaving the app pause it; done 2026-09-13: HUD pause button, pause on leaving the app, see `21`).

### Next (owner choice 2026-09-14; art direction and order in `19`)
- isometric arena: a floating platform with enemies spread in two dimensions, larger packs and a rebalance, placeholder visuals (done 2026-09-14 with placeholder visuals: arena floor, packs walking in, the rebalance, a floating platform and camera framing between the HUD blocks; then pixel-art placeholders drawn from code for the void, the platform, the hero, the enemies and the combat effects, see `21`),
- an area ability with a cooldown (done 2026-09-14 as the Forge Burst around the hero: one button, a floor ring, 20 damage in 2.5 units every 8 s, see `21`),
- a wider platform with a walkable hero (drag to move), packs from all four sides, chests on the floor and a camera that follows the hero (owner request 2026-09-14; done the same day, see `21`),
- icon HUD: room pips, boss bar, upgrade/relic badges and bottom health/XP panel (done 2026-09-16 with imported foundry UI and themed choice cards; full stat details through Pause; individual badge tap details still pending; damage numbers already existed),
- extend the foundry UI to result and Relic Forge menus (done 2026-09-16; reused frames, responsive card rows and explicit visual shop states; see `21`),
- compare portrait movement framing at widths 6/7.5/9 (2026-09-16: staged Unity comparison complete, width 9 selected; device gate in `21`–`23`; ten-enemy kiting and boss-specific room art remain separate),
- constrain camera follow at the arena rim and integrate the first dim furnace-cavern background layer (done 2026-09-16: short/tall scene guards, S23 far/right movement and warm restart; `ArtDirection/2026-09-16/furnace-layer-01/`, `21`–`23`),
- improve slab/rim materials and ground hero/enemies with shared contact shadows (implemented 2026-09-16: quiet bevels/patches/chips, segmented flush coping, cached contact patches with death/reload/edge-fit checks; full gate passed and APK installed/observed on S23). Controlled rim-drag and ten-enemy phone checks for this build await device-control coordination; see `23`. Sidewall/prop polish, a hero/Mite production proof and separate boss geometry remain open,
- a frame-time probe for device sessions (done 2026-09-15, see `21` and `23`),
- local telemetry for device sessions (owner's shared roadmap 2026-09-15, stage 1; done 2026-09-15: a bounded local JSON Lines event log with run, room, choice, currency and time events, no SDK or network, see `21`),
- prove ten-enemy density and what kiting is worth (done 2026-09-16: capacity 10, a spawn separation, three hero routes, the simulation walking the hero in step with the scene, a development-only proof floor and six scene/simulation parity cases; measured on the S23; see `21`–`23`, decision in `19`). Left open by it, in the order they matter:
  - enemy sprite cache (done 2026-09-16: immutable per-look frames and shared bars survive restarts; PlayMode lifetime/state checks and S23 verification in `21`–`23`). The original 18 MB was a whole-frame measurement, not all enemy drawing,
  - platform/backdrop sprite reuse (done 2026-09-16: bounded session ownership, geometry-safe fallback, native lifetime tests and three S23 warm restarts). Whole restart-frame allocation fell from 17,620.8–17,742.4 KB to 1008.5–1131.4 KB; probe intervals were 16.7 ms, with main-thread work still 24.8–32.0 ms. Cold startup and hero/effect allocation remain separate concerns; see `21`–`23`,
  - decide whether authored rooms should hold more than five, now that the numbers exist; every authored wave is still five or fewer,
  - retune kiting: the Staff clears the authored Descent almost unharmed while kiting, and the Daggers keep only a 0.3-unit band in which they can strike while moving,
  - make the enemy spacing rule two-sided (done 2026-09-16: no two living enemies ever come within a body, slide or wait instead of overlap, no flicker on the proof floors, standing authored Descent unchanged; see `21`–`23`, decision in `19`; phone-checked on the S23 with a scripted turning-round run),
  - give the hero a body enemies cannot walk through,
  - make crowded fights end the same on the Editor's Mono and the pure .NET runner (they disagree in the last bit of float comparisons in a crowd; 14 of 36 measured runs differ, see `22`),
  - measure a mid-range device: only the S23 has been measured.

Exit:
5–10 minutes playable.

## Phase 2 — Fun prototype

Add:
- 10–20 upgrades,
- 3–5 meaningful synergies,
- better juice,
- result screen,
- basic meta reward,
- local save.

Test with humans.

Exit:
multiple testers voluntarily replay.

## Phase 3 — Vertical slice

Add:
- one coherent biome,
- one production hero,
- final-ish UI,
- SFX,
- one additional weapon/hero preview,
- onboarding,
- analytics (a local event log exists since 2026-09-15; no provider is chosen),
- crash reporting,
- device optimization.

Exit:
looks like a real game in one slice.

## Phase 4 — Soft-launch MVP

Add only evidence-backed features:
- more content,
- optional offline reward,
- rewarded ad hooks,
- IAP hooks if justified,
- store assets,
- localization basics.

## Phase 5 — Growth

Only if metrics justify:
- more heroes,
- events,
- cloud save,
- remote config,
- seasonal content,
- advanced economy,
- backend services.

## First 7 days

### Day 1
- create repo/project,
- set portrait,
- make bootstrap/gameplay scene,
- placeholder hero/enemy.

### Day 2
- health/damage/death,
- auto-target,
- attack cadence.

### Day 3
- rewards,
- upgrade selection,
- stat modifier system.

### Day 4
- rooms/waves,
- 3 enemy types,
- restart/end-run.

### Day 5
- 3 weapon behaviors,
- synergy prototype,
- combat juice.

### Day 6
- first external playtest,
- record confusion/drop-off,
- create art style board only.

### Day 7
- fix top issues,
- decide art direction,
- build production asset test for one hero.

## Backlog categories

P0 Prototype:
- combat
- upgrades
- run
- boss

P1 Vertical slice:
- art pipeline
- onboarding
- analytics
- save
- audio

P2 Soft launch:
- monetization
- store
- extra content
- localization

P3 Growth:
- cloud
- liveops
- remote content
- advanced social features

## Art production progress — 2026-09-17

- [x] Integrate the first imported Vanguard body/sword/buckler prototype with movement/attack events, flash/death/reload support and reproducible Unity captures.
- [ ] Finish opposing facings, planted-foot gait and hand occlusion before calling the character animation production-ready.
- [x] Add the front Vanguard pose set and four diagonal runtime facings, with mirrored presentation, real-target attack facing and revised rear passing feet.
- [ ] Polish world-space foot contact and finger occlusion; resolve mirrored lighting/handedness before final character sign-off.
- [x] Integrate the first animated Cinder Mite plus imported sword slash and impact sparks; verify shared ownership, hit/death timing and crowd readability.
- [ ] Match Staff/Daggers artwork and the remaining enemies; ability, coin and several strike effects still use the procedural art.
- [x] Refine the Mite health-bar width for crowd readability while preserving fill/flash/health independence (2026-09-17, compact parent scale, Unity and S23 checked).
- [x] Remove Vanguard's upward attack nudge, shorten the displacement gait cycle and add registered front sword knuckle occlusion; verify flash, mirrored poses and other loadouts.
- [ ] Finish strict world-space support-foot contact and final finger articulation; the compact four-pose gait remains a prototype.

## Chest art slice — 2026-09-18

- [x] Import shared closed/opening/spent foundry chest sprites with fixed registration and immediate single contact reward.
- [x] Ground actor/prop depth groups while keeping health bars and ground effects independent.
- [ ] Complete the coordinated physical-device review of the integrated chest build.
- [ ] Replace ability-ring artwork; this slice changes its grouping only.

## Movement HUD polish — 2026-09-19

- [x] Separate health, labelled movement and XP; add a single pause-aware depletion emphasis without changing movement balance.
- [x] Capture full/partial/empty/refilling states in Unity, including two portrait sizes.
- [ ] Verify this HUD revision on the physical device after coordinating exclusive use.
