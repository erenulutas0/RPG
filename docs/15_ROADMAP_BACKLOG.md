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
- [x] Replace ability range-ring artwork with the shared eight-arc contour (2026-09-19).

## Movement HUD polish — 2026-09-19

- [x] Separate health, labelled movement and XP; add a single pause-aware depletion emphasis without changing movement balance.
- [x] Capture full/partial/empty/refilling states in Unity, including two portrait sizes.
- [x] Verify this HUD revision on S23 after coordinating exclusive use (2026-09-19; Docs/23).

- [x] Compare ability-ring contours in the actual Unity room at two portrait sizes, ready/cooling and rim positions (2026-09-19).
- [x] Integrate preferred eight-arc ring with shared lifetime and validate actual ability use/movement on S23 (2026-09-19).

- [x] Replace the ability cast's thick pixel burst with a pooled expanding/fading ground contour; retain immediate damage and0.21s lifetime; verify Unity and S23 (2026-09-19).
- [ ] Match the separate Staff splash and remaining weapon/effect artwork to the painted presentation.

- [x] Compare four-arc Staff impact against the pixel blast in the real Unity crowd at two portrait ratios (2026-09-19; proof only).
- [x] Integrate shared Staff impact contour, test ownership/pool/pause and verify on device (2026-09-19).

- [x] Create original Staff weapon source and browser proportion/grip study (2026-09-19; no runtime import).
- [x] Sample Staff weapon source in Unity, reconcile shaft/orb/grip/facings and integrate (2026-09-19; APK installed, live device review pending).
- [x] Review imported Staff equipment on S23 (2026-09-19; short normal-floor movement/cast sample, profiles unchanged).

- [x] Create original Daggers master and two-hand browser proportion study (2026-09-19).
- [x] Register/import both Daggers in Unity; verify anchored grip, depth, attack, pause and reload (2026-09-19).
- [ ] Review imported Daggers on the available phone (APK installed; physical review pending).

- [x] Sample Daggers in actual Unity facings/gait/thrust and compare contact-preserving alternative (2026-09-19; proof only).

- [x] Make Descent ground rewards visible with a shared painted sprite and pooled ground/collection presentation; preserve immediate payment and simulation parity (2026-09-22, Unity/full gate/APK passed).
- [x] Install ground reward APK on S23 and verify package identity/current profile preservation (2026-09-22).
- [x] Review ground rewards live on S23 (2026-09-22: presence/proximity payment confirmed; exact flight timing not isolated).
- [ ] Present the walked finale exit, hold progress, finale pressure/clock and pending upgrade cards (visual plan in ArtDirection/2026-09-22/reward-proof-01/VISUAL_PLAN.md).

- [x] Integrate matching painted XP-only crystal at0.30 visible width (2026-09-23; actual Unity capture passed).
- [x] Install and physically review the XP-only companion crystal on S23 (2026-09-23: package/profile integrity and visible crystal confirmed; timing/performance not remeasured).

- [x] Confirm first-region route and optional post-boss swarm decision; interactive map composition (2026-09-23).
- [ ] Implement playable region route: two fights, elite/chest branch, forge, boss, extract/optional swarm; see region-map-01/DESIGN_AND_HANDOFF.md.
- [x] Integrate finale ground seal and authoritative hold progress with off-screen cue (2026-09-23; targeted tests and Unity captures passed).
- [ ] Physically review finale exit seal/hold/cue on device.

- [x] Development-only wide region entry and first imported furnace scenery; full gate395/130, APK built2026-09-23.
- [x] Wide-region physical device review (2026-09-23: APK installed and traversed on S23; see the traversal item below and Docs/23).
- [ ] Physical encounter sites and separate boss approach; node-route proposal remains superseded.

- [x] S23 wide-region traversal and installed-hash review (2026-09-23); test flag removed, normal hall restored.
- [ ] Adjust north furnace clearance: hero occludes furnace at exact arena tip. 2026-09-30: the square towers were replaced by round boilers that depth-sort against actors (`foundry-depth-mass-01`); the exact tip has not been re-checked.

## 2026-09-23 — Playable foundry delivered

Development map installed on S23 (7FB249A4); owner can walk/fight/collect/choose/restart with foundry flag retained. Shared concave floor/navigation and perimeter modules complete for first playable review. Gate artwork, local-site progression, separate boss hall, richer dressing and sustained midrange performance remain open. See foundry-playable-01 and latest21–25.

## 2026-09-23 — Current art order after composition correction

Use the photograph-led broad court / shaft / south bridge / northeast gate from foundry-playable-01, not the earlier linked-platform diagram. After this layout build is reviewed at phone scale, replace coarse gate/furnace/crane architecture with registered modular art, then finish perimeter joints, paving, local light and atmosphere. Preserve readable combat space. Opus progression handoff stays on hold; local sites and separate boss transition remain unimplemented.

## 2026-09-30 — Foundry map art: depth, mass, light and texture imports

Proofs under `ArtDirection/2026-09-30/`; state, verification table and commit plan in `38_HANDOFF_2026-09-30_ART.md`. Nothing committed.

- [x] Court floor reads at the gameplay camera: per-flag tone, brass compass, floor decals kept out of the hero cutaway (2026-09-30; Unity, full gate, installed and seen on S23; `foundry-floor-01`).
- [x] Real depth for tall structures and a cutaway only in front of the hero; round boilers, balustrade, furnace pit, foundations, piers and chains replace the square towers and shaft (2026-09-30; Unity, full gate, installed; a structure hiding an actor not yet seen on device; `foundry-depth-mass-01`).
- [x] X-ray silhouettes behind structures: ember for enemies, gold for drops and chests (2026-09-30; Unity capture for enemies, tests for drops and chests; installed; not yet seen on device; `foundry-xray-01`, `foundry-parallel-01`, `foundry-chest-light-01`).
- [x] Chests and above-floor combat effects take ground depth in the foundry; numbers stay on top (2026-09-30; tests only, no capture of either behind a structure; installed; `foundry-chest-light-01`, `foundry-fx-light-01`).
- [x] Rim vent-slot row removed; brass star seal, coursed bridge deck, heavy chains, riveted reservoirs and boilers, three pier forms (2026-09-30; Unity, full gate, installed; seal seen on S23; `foundry-rim-01`, `foundry-parallel-01`).
- [x] Floor light from the real pit, boilers, lanterns and beacons: forge-orange pools and a cool open court; calmer bridge margins (2026-09-30; Unity, full gate, installed and seen on S23; `foundry-chest-light-01`, `foundry-fx-light-01`).
- [x] Ship GateFacade and FurnaceFacade as power-of-two ASTC 6x6 (1.2 MB each, were 3.6 MB RGBA32) and the forged UI panel at Android 1024 ASTC 4x4 (344.6 KB, was 4.5 MB); build textures 20.5 to 13.9 MB (2026-09-30; panel seen on S23; `foundry-parallel-01`, `foundry-chest-light-01`, `ui-panel-01`).
- [x] Painted-texture prompt pack v2: P1 court flagstones, P2 balustrade trim, P3 foundation masonry, P4 pit lining, P5 boiler drum wrap, all power-of-two, with renderer-budget options (2026-09-30; `painted-surfaces-02`, supersedes `painted-surfaces-01`). Prompts only; nothing generated.
- [ ] Owner: choose the renderer-budget option (`painted-surfaces-02/PROMPTS.md` section 3: A stay at 17 through the P4 discharge merge, B drop the crane-footing roof, C raise the cap to 18); needed before P2 is integrated.
- [ ] Owner: painted irregular flagstones (P1) or today's geometric flags with a jointless stone face (P1-b).
- [ ] Generate P1-P5 in ChatGPT (P5 optional, after P4) and save the untouched masters under `ArtDirection/<date>/painted-surfaces-02-masters/`. Pending: nothing generated yet.
- [ ] Integrate P1-P5 (power-of-two imports, new foundry material slots, `FoundryRegionFinish/View/Massing`, `PlatformMaterialSet`) with matched Unity captures and the full gate; ask before installing.
- [x] See the enemy/loot silhouettes on the S23 (2026-09-30, development depth-proof flag; `foundry-device-proof-01`). Effect depth behind a structure seen on the S23 too (2026-09-30, `foundry-rail-01`).
- [x] Measure foundry frame time on the S23 (2026-09-30, development `RenderStatsProbe`): ~60 fps at the vsync cap, p95 16.7 ms, GPU ~3 ms, 50-98 draw calls in a normal run; not a sustained/thermal or late-game swarm test (`foundry-device-proof-01`).
- [ ] Bridge and lighting follow-ups: bridge margins may now look plain; pool cores read slightly rusty/dark and orange Mites on the pit rim lose contrast; the bridge lattice goes with P1.
- [ ] Later polish: foundry floor-decal atlas, iron recolour, brass tint, pit-collar trim, lantern-pane colour, seal quad UVs, low parapet, thin pier ledges; check the UI panel for shimmer on a moving panel.
- [ ] Commit the 2026-09-22..30 art working tree once the owner agrees on the grouping and on the ~1.8 GB of proof captures (plan in `38_HANDOFF_2026-09-30_ART.md`).


## Deferred - movement energy (owner decision 2026-09-23)

- [ ] Reconsider energy only after large-map traversal has been evaluated. Currently disabled globally: no drain/slowdown, no MOVE HUD, no energy upgrades in offers/books. Preserve the old mechanism for comparison; do not restore it implicitly. Evaluate enemy approach tools and kiting balance before choosing whether energy returns. See latest doc19 decision and HeroStamina.CurrentRulesEnabled.

## 2026-10-08 - Reference-layout follow-through

- [x] North court chamfers share render/movement/pursuit/spawn geometry; wall/rail and northwest supports align (full gate and S23 route).
- [x] Group existing basin, pipe/tool rack and reserve stock near crane/anvil, including support/depth/clutter/soot (Unity overview and S23).
- [x] Smaller ashlar and seal stones with quieter joints; no new texture/material/update loop (before/after, full gate and S23 gameplay).
- [x] Traversable visual gate elevation completed in gate-elevation-01 (2026-10-08); gameplay remains on its deterministic floor plane.
- [ ] Further painted light/material cohesion against the approved concept.

Evidence: ArtDirection/2026-10-08/north-workshop-paving-01; latest state and installation in docs38/23. Historical unchecked September texture/commit items above are dated snapshots, not a reversal of later completed packages.

## 2026-10-08 - Gate approach motif

- [x] Long matte processional frame, lane-oriented medallion and broad dressed threshold using existing batches (gate-procession-01; full gate and S23).
- [x] Actual traversable visual gate elevation: shared ground/actor/shadow/effect/camera projection, inverse/depth tests, S23 climb/descent (gate-elevation-01; full gate).
- [ ] If first-use elevation hitch recurs, isolate cold shader/driver versus capture/cache cost; short warm S23 repeat does not establish midrange or sustained combat performance.


## 2026-10-08 — Three-task material hierarchy package

- [x] Reduce the finer paving's checkerboard with restrained flag tone and quieter joints/bevels.
- [x] Formed metal boiler hoods: complete far/near slopes, broad highlights, soot and sparse radial seams.
- [x] Recede distant foundry cavern light through an owned sprite material; preserve ordinary halls and verify release/borrowed-art ownership.

Evidence: `ArtDirection/2026-10-08/surface-cohesion-01/`, full gate and S23 approach/normal combat. Broader painted-reference fidelity remains open.


## 2026-10-09 — Industrial metal reflected light

- [x] Carry authored forge-source warmth across boiler shells/hoods, pressure mains/collars and reservoir surfaces with source-height falloff and ramp continuity.
- [x] Preserve directional form shading, generic unlit fittings and static batching; verify Unity and S23 traversal/normal combat.

Evidence: `ArtDirection/2026-10-09/forge-metal-light-01/`. Broad painted-reference fidelity remains open.


## 2026-10-09 — Worked stone and foundation faces

- [x] Within-stone mineral wash and sparse flush edge flakes, preserving seal/workshop setts and ramp seams.
- [x] Blind recesses on banded/buttressed bastions within existing contours and anchors.
- [x] Actual triangle-interior elevation regression, full gate and guarded S23 paving/ramp/normal-run review.

Evidence: `ArtDirection/2026-10-09/stone-relief-01/`. Broad painted-reference fidelity remains open.


## 2026-10-09 — Current art backlog reconciliation

Historical September checkboxes above are dated records, not the current work queue. Doc38 sections7–8 confirm the October1 commit grouping and imported P1-b paving, P3 masonry, P4 pit lining and P5 boiler wrap. P2 was explicitly skipped; the current18-renderer count does not imply P2 integration. Bridge margins, iron recolour, lantern panes and pier crowns have later implementation records. Staff/Daggers assets are present. Do not regenerate or reopen them solely from old unchecked lines.

Still open after the current architectural package:
- [ ] Complete broad painted-reference fidelity and material/architectural cohesion; smooth stock metal still needs richer authored surface character.
- [ ] Local encounter sites and the actual boss transition/separate boss-room art. The development gate-arrival presentation is not that gameplay transition; do not revive the superseded node-route proposal implicitly.
- [ ] Final actor gait/hand articulation/mirrored-light consistency and remaining character/enemy roster art.
- [ ] Sustained crowded-combat and midrange readability/performance; diagnose the previous first-use elevation hitch if it recurs.

Target-specific old acceptance checks (including pit-rim Mite contrast or moving-UI shimmer) require their own evidence. Recent general traversal must not silently mark every older acceptance item complete.


## 2026-10-09 — Combined architectural package status

- [x] Gate stone lancets, rounded workshop stock and backed undercroft arches integrated; three-agent source/visual cross-review and full Unity/Android gate passed.
- [x] Install architecture-cohesion APK and complete guarded S23 gate/normal review; fresh profile/flag hashes preserved. Short no-capture traversal measured59.5–59.9fps after startup with one50ms hitch, still unisolated; extended acceptance remains open.

Proof: `ArtDirection/2026-10-09/architecture-cohesion-01/`. This closes the three implementation tasks, not the remaining art backlog above.


## 2026-10-09 — Material readability package

- [x] Reveal existing iron grain across workshop stock and keep continuous pipe highlights.
- [x] Give pit lining continuous curved-wall shading and a bounded collar contact shadow.
- [x] Make environmental brass matte and reduce groove contrast without losing the route motif.
- [x] Material-readability APK installed/hash-matched; guarded stock/pit/normal-run review and profile/flag checks passed. First/warm ramp measurements recorded separately; first-use hitch remains open.

Actual Unity before/after and three-agent source/visual review: ArtDirection/2026-10-09/material-readability-01. Broad painted-reference fidelity and the other genuine tasks listed above remain open. The owner authorized pushing completed commits on2026-10-09; earlier local-only notes are historical.

- [ ] Instrument first ramp resource/grid creation and first draw before choosing prewarm: current build had one216.9ms first-entry stall; same-process repeat max33.4ms does not prove the root cause or fix it.


## 2026-10-09 — Blender workflow experiment

- [x] One free editable furnace study and actual Unity matched-scale1200/360px comparison; no production replacement.
- [ ] Preserve arched openings and refine soot/heat variation in a candidate; prove actor depth, cutaway and ground contact before accepting a single map placement.
- [ ] Choose layered bake vs optimized mesh based on map/device evidence; full-map migration remains unapproved and unproven.

Proof: ArtDirection/2026-10-09/blender-furnace-01. Existing art/runtime backlog and first-use hitch remain open.

## 2026-10-09 — Arched furnace study02

- [x] Restore arched mouths, jambs/grates and upper ports; improve contained heat and cool basalt.
- [x] Actual Unity staged placement/front-back cutaway probe and portrait-width review; no production replacement.
- [ ] Resolve ground contact/environment light, pipe/foundation alignment and all-side/neighbor depth before replacing one boiler.
- [ ] Mobile compression/atlas and device acceptance before wider adoption. Source11,928vertices/23,104triangles was rendered to a quad for this probe, not imported as production mesh.

Proof: ArtDirection/2026-10-09/blender-furnace-02. This advances the earlier Blender experiment; full map migration is not approved or completed.
## 2026-10-09 — Single production baked boiler

- [x] Replace north boiler0 with two depth-aware baked bands; preserve base/pipe/flue anchors; contact shade shares existing floor decals.
- [x] ASTC6/mip import, lifecycle/depth/batch tests, actual matched graphics and four walkable approach reviews; final full suites and APK passed.
- [x] Background S23 installation/hash match; all11fresh saved files unchanged.
- [ ] Pending owner availability reply: open installed build for north-boiler movement/visual review, normal combat and short performance sample. No new APK needed for that review.
- [ ] Further wear/light coherence and other furnace variants only after evaluating this one replacement. Full reference fidelity and earlier ramp hitch remain open.

Proof: ArtDirection/2026-10-09/baked-boiler-runtime-01. Study02 staged court placement is superseded by this actual one-boiler integration, but its editable Blender source remains authoritative.

## 2026-10-09 — Accepted single boiler; shared north pair

The preceding single-boiler device-review checkbox is now closed: guarded S23 movement, normal run and fresh profile/development-file hashes passed; evidence in baked-boiler-runtime-01/device. Short stationary samples do not close the separate first-use ramp hitch or sustained/midrange acceptance.

The second tall north boiler now shares the accepted study02 texture/material and two depth bands. Both preserve independent mass ownership; short east boilers keep their original distinct design. Actual Unity pair comparison and four walkable approaches passed; region remains20renderers and drops to100,684vertices. Full gate and device results follow in the pair handoff. Proof: ArtDirection/2026-10-09/baked-boiler-runtime-02.

Next substantial art work: author a distinct short-boiler variant using the same editable Blender workflow, compare it at actual game size before integration; then address coarse foreground pipe shading against the new baked forms. Do not replace the whole map indiscriminately. Full reference surface richness, actor/encounter art and first-ramp engineering investigation remain separate open work.


## 2026-10-09 — Distinct short boiler artwork and runtime pair

Authored the low foreground variant with broad segmental fire doors, pressed lid and short vent, using the committed study02 Blender material/light setup. Script Tools/ArtReview/BuildBlenderShortBoiler.py; editable source ArtDirection/2026-10-09/short-boiler-01. Authoring mesh6,704vertices/13,036triangles is baked, not imported as runtime geometry. Existing height2.28/radius2 design, walkability, foundations, service sockets and smoke anchors are retained.

Both short boilers share one new1024ASTC6x6 texture with11mips and two eight-vertex depth bands; scene source material and visibility variant retain explicit ownership. Original sockets are runtime geometry; shades share floor decals. Two bands replace one now-unused legacy furnace-wrap batch, so region20->21renderers and100,684->99,492vertices. New texture calculated payload626,288bytes including mips, excluding engine overhead. Existing tall pair unchanged.

Actual Unity matched legacy/new pair, four validated walkable portrait approaches and overview: ArtDirection/2026-10-09/short-boiler-runtime-01. Legacy constructor/resource retained for fallback/comparison; import-use assertion explicitly checks retired wrap absence and replacement short bands. Full device/gate results are in the latest docs22/23/38entry. Fire/light remain static baked; pipe shading still needs a separate coherence pass. No paid service. Full-reference completion, actor/encounter work and earlier first-ramp hitch remain open.

## 2026-10-09 — Pressure equipment, pit hoods and architectural seating

Completed a combined visual slice with three helper agents: formed service pipes and high feeds; two Blender-baked pressure reservoirs; continuous formed-metal pit hoods; quieter gate roof/header sampling; better-seated keeper mounting slabs. Existing layout, walkability, foundations, gameplay and artwork anchors remain. Gate and keeper gains are deliberately modest; pit metal is smoother but plainer, not reference-complete ornament.

Reservoir source: Tools/ArtReview/BuildBlenderReservoir.py and ArtDirection/2026-10-09/reservoir-01. The 2,952-vertex authoring model is rendered to a shared1024source and imported at512ASTC6x6,10mips,non-readable. Calculated block payload158,432bytes including mips, excluding engine overhead. Runtime uses two quads with explicit2x2mass ownership; far/main/near ranks survive batch quantization. Near relief geometry has its own existing-iron rank. New source material has scene cleanup; no new per-frame visual component.

Whole region changes21renderers/99,492vertices to24/100,572 (+3renderers,+1,080vertices). Pressure slice alone24/99,444; pit/header24/100,548; keeper seats add24vertices with no renderer. Actual source-state before/after galleries: pressure-assembly-01, foundry-finish-01 and keeper-seat-01 under ArtDirection/2026-10-09. Graphics use staged heroes and hidden HUD, not live device traversal. Historical comparison capture scripts require the corresponding old source state; they do not recreate legacy props in the latest scene.

The first full PlayMode run caught a genuine entry timing issue: the first reveal iteration counted the preceding scene-loading frame. GateArrival now measures four seconds from view capture on a monotonic unscaled clock. A regression supplies an actual1.2-second preceding frame and requires4..6seconds from capture, preserving skip/handback/encounter guards. Original natural-completion threshold was not weakened. Final full-suite, build and device evidence is recorded separately in docs22/23/38.

Forge keeper source provenance correction: the older prompt.txt contains literal undefined and a generic tool note; the complete original generation prompt was not preserved. README now states this explicitly; no prompt was invented.

Next substantive art work remains broader authored surface richness and encounter/boss presentation, followed by actor articulation/roster. Do not spend repeated slices on barely visible bevels. Full reference fidelity is not complete; cold-first-ramp hitch, sustained crowded-combat and midrange acceptance remain open.

## 2026-10-09 — Workshop metal kit, crane housing and service paving

Completed a coordinated three-part art package: enclosed cast lower crane housing, Blender-baked anvil/hammer/parts chest/five ingots, and two local repair patches in the west/north service paving. Original crane silhouette width and upper rigging, station anchors, masonry support, walkable union, camera, combat and profile behavior are retained. Floor repairs subdivide existing parent stones exactly; no overlay or new obstacle.

Editable source: Tools/ArtReview/BuildBlenderSalvage.py and ArtDirection/2026-10-09/salvage-kit-01. Authoring mesh3,038vertices/5,984triangles is baked to one runtime quad;1024source imported512ASTC6x6,10mips,non-readable. Calculated block payload158,432bytes including mips, excluding engine overhead. Original whole-shelf mass depth and explicit material cleanup remain. Actual Unity review caught the equal-rank rear landing hiding the anvil horn; rank-10.5/sortingOrder-7 fixes it without moving the art. Source/image review accepted final contacts and silhouette.

Region24renderers/100,572vertices becomes25/100,968; no new per-frame component. Actual matched source-state before/after, detail/overview and four walkable staged-hero portraits: ArtDirection/2026-10-09/foundry-surface-01. The inherited budget texture line describes unchanged FoundryShortBoiler, not salvage. Final tests/device evidence is recorded in docs22/23/38 and the proof validation directory.

Reference fidelity is unfinished. The crane base remains busy around its old front crank; local floor changes are deliberately restrained. Next higher-value art work should address actor/encounter presentation or a clearly visible architectural material gap, not another marginal bevel pass. Cold-first-ramp diagnosis and sustained crowded/midrange-device acceptance remain open.


## 2026-10-09 — Map shape takes priority over small polish

Owner requested closer reference map size, shape and major features. Implemented westboundary-24→-30 with complete exteriorassembly relocation, +192walkable squareunits, continuous tapered fortressfoundation and deeper industrialpit. Original gate/bridge/pitfloorboundaries and camera remain. Actual proof: ArtDirection/2026-10-09/foundry-massing-01. Full verification/device status: latestdocs38/23entry. Next priority: gateapproach/enclosure, entrancebridgeproportions and purposefulworkyardfeatures. This supersedes the preceding suggestion to switch immediately to actor/encounter art. Fullreference fidelity is notcomplete.

## 2026-10-10 - Reference-map approach package

- [x] Shared longer/wider bridge, coherent fixtures and supports; movement/spawn/pursuit/render verification.
- [x] Fortified gate enclosure and flush mechanical freight yard; actual Unity review.
- [x] Bridge/court paving overlap and support ordering fixes; full989-test gate and Android build.
- [ ] Install/review on S23 after current owner-run restart authorization.
- [ ] Next substantial reference gaps: irregular stone surfaces and purposeful raised yard equipment with honest collision/navigation. Keep map features ahead of unrelated actor polish.

Evidence: ArtDirection/2026-10-09/foundry-approach-01. Full reference fidelity and sustained/midrange performance remain incomplete.

## 2026-10-10 - Courtyard craft, art first

- [x] Mixed slab workyard, restrained mineral families and contained stone wear; production-packer regression coverage.
- [x] Broad gate recesses, dressed crowns and lower quoins on existing tapered faces.
- [x] Coherent stock hoist within the existing shelf, with explicit upper-assembly depth ownership.
- [x] Matched actual Unity comparison, including phone-proportioned staged-actor views.
- [ ] Current package physical-device acceptance after pending owner-run restart approval.
- [ ] Closer reference surface richness: less planar stone wear and more coherent authored industrial metal. The central paving delta is moderate; avoid treating it as reference completion.
- [ ] Region encounter/boss progression remains later, per latest explicit owner instruction. Do not resurrect the superseded node-map plan.

Evidence: ArtDirection/2026-10-10/foundry-craft-01. See final verification record for exact tests/build/device status.
## 2026-10-10 - Materials package, reference art still first

- [x] Authored stock-hoist metal with coherent bearing/drum/chain forms, integrated at existing shelf and explicit depth.
- [x] Finer gate masonry with restrained weathering; existing silhouette/door preserved.
- [x] Less polygonal floor wear; rare contained incisions, quiet majority of stone.
- [x] Matched actual Unity visual review; resource/depth/material cleanup checks.
- [ ] Physical-device review of current accumulated map packages after pending owner-run restart approval.
- [ ] Broader surface consistency: remaining procedural stock/crane surfaces are coarser than authored metal; reference fine stone richness is not yet matched.
- [ ] Local encounters, boss hall transition and progression remain deferred by the art-first direction.

Proof: ArtDirection/2026-10-10/foundry-materials-01. Continue with visibly useful material/composition gains, not repeated almost-invisible trim passes. Existing cold-first-ramp, sustained/crowd and midrange-device questions remain open.
## 2026-10-10 - Workshop coherence package

- [x] Authored stock-station metal and crane body, retaining original separate wood/chain/load/lights and footprints.
- [x] Actual Unity comparison at detail and gameplay proportions; correct localized rear masonry occlusion and bright bearing imbalance.
- [x] Explicit depth and bounded scene-owned resource lifecycle integration.
- [ ] Physical-device acceptance of the accumulated map packages after the pending owner-run restart decision.
- [ ] Reference composition remains incomplete: the working yard is still sparse beyond its peripheral shelves. Additional raised machinery requires corresponding navigation/collision design, not decorative blockers over walkable floor.
- [ ] Fine stone/material consistency, crowded/midrange-device acceptance and first-ramp hitch diagnosis remain open. Gameplay progression remains deferred by the owner's art-first request.

Evidence and final gates: ArtDirection/2026-10-10/workshop-coherence-01/validation/verification.md. Avoid treating a peripheral material improvement as complete reference fidelity.

## 2026-10-10 - Surface cohesion completed in Unity

- [x] Readable warm-matte court and gate brass within existing groove footprints and bounded floor lighting.
- [x] Quieter ashlar joints and compact oblong mix; exact coverage/ceremonial/repair/brass constraints retained.
- [x] Contained vertical pit flues and faceted industrial vessel crowns; final image-review tonal correction.
- [ ] Current accumulated map package phone acceptance after the pending owner-run restart decision.
- [ ] Reference irregular stone character remains unresolved. Future useful floor work needs designed irregular outlines/topology or authored surface treatment, rather than another minor weight/palette pass.
- [ ] Working-yard composition remains sparse; new raised equipment needs a matching navigation/collision contract. Full reference fidelity and deferred gameplay progression remain separate work.

Proof and final verification: ArtDirection/2026-10-10/foundry-surface-cohesion-01/README.md. No new renderer/material/texture; totalgeometry+0.75%. Physical-device/crowd/midrange/first-ramp acceptance remains open.

## 2026-10-10 - Flagstone contour milestone

- [x] Replace ordinary rectangular stone faces with varied convex quarry contours and safe matching bevels; preserve complete paving coverage, lighting, UVs, inlays and surface elevation.
- [x] Retain regular ceremonial/repair/infill stones and planar ramp-seam handling; independent image review accepts actor readability and continuous light.
- [ ] Reference-quality stone finish is only partly resolved: underlying rectangular courses and the stronger mortar network remain visible. Do not describe the contour pass as complete reference fidelity.
- [ ] Working-yard composition still needs purposeful equipment placement and a matching navigation/collision contract for raised obstacles.
- [ ] Accumulated phone acceptance remains pending the existing owner-run restart/install decision; sustained crowd/midrange/first-ramp checks remain open.

Proof and final verification: ArtDirection/2026-10-10/foundry-flagstone-01/README.md. Region29renderers/113,995vertices; no new textures/materials or per-frame components. Deferred region encounter/boss progression remains separate from the owner's art-first work.

## 2026-10-10 - Reference depth package

Implemented and visually reviewed in Unity: five distant towers, one broken service span, atmospheric separation and lower-shaft fading, explicit background/island sorting, and quieter irregular paving joints. Evidence: ArtDirection/2026-10-10/foundry-depth-01. Full test/build/device status is recorded in its validation/verification.md.

Next visual priority: purposeful working-yard composition and richer architectural forms. The new distant silhouettes remain simple; avoid another isolated haze or tiny corner-chip iteration. Raised equipment still requires navigation/collision agreement. The approved broad court, left furnace pit, northeast gate and foreground bridge remain the composition contract. Full reference fidelity and physical-device/crowd/midrange/first-ramp acceptance are open; region encounter/boss progression remains deferred by the owner's art-first direction.

## 2026-10-10 - Casting workbay delivered to Unity

The north working-yard interval now contains a supported original casting bench and screw press, open moulds and finished billets. Existing walking lanes and north rail are preserved; no new gameplay/collision contract is needed outside the walk union. Actual Unity review accepts placement, contact and foreground overlap. Evidence: ArtDirection/2026-10-10/foundry-workbay-01; editable Blender kit: casting-bench-01 beside it.

Next substantial art priority remains architectural richness and working-yard composition at whole-map scale, especially the coarse large northwest distant silhouette. Avoid claiming the small bench completes reference fidelity. Device/crowd/midrange/first-ramp acceptance and deferred region encounter/boss progression remain open; final gate and installation status live in the proof verification record.

## 2026-10-10 - North skyline architecture

Completed in Unity: two recessed arched furnace towers with dressed frames/tapered shoulders; improved broken viaduct supports/parapets; shorter subdued furnace panes. Actual root and independent image review accepted the refined version. Evidence and final gate: ArtDirection/2026-10-10/foundry-skyline-01.

Remaining reference gap is the wider architectural variety and building scale rhythm, especially simpler surrounding shafts and gate/court material richness. The new towers intentionally stay dark and do not replace the reference's full detail. Avoid claiming complete fidelity or repeating another minor glow-only pass. Physical-device/crowd/midrange/first-ramp acceptance remains open; regional encounter/boss progression remains deferred by the art-first decision.

## 2026-10-10 - Gate and court art package

- [x] Replace the shallow gate gable/repeated pier belts with a supported stepped crown, continuous stone shafts and deep portal returns inside existing bounds.
- [x] Consolidate the court into one radial stone medallion with restrained brass, preserving an open fighting floor.
- [x] Give the approach a flush dressed stone border; resolve visibility and miter joins through matched actual Unity review.
- [ ] Reference material grain/stone scale and natural wear remain unfinished; do not claim full photographic fidelity.
- [ ] Accumulated physical-device/crowd/midrange/first-ramp acceptance remains pending. Regional encounter/boss progression stays deferred under the art-first decision.

Evidence and final verification: ArtDirection/2026-10-10/foundry-gate-court-01. Existing map bounds, ramp and gameplay preserved.

## 2026-10-11 - Foundry stonework material slice

- [x] Integrate versioned fine-mineral V5 paving, remove redundant slab cloud wash, unify grain scale and retain ramp/lighting contracts.
- [x] Add dressed gate courses, continuous niche mapping and supported bevelled coping within existing silhouette.
- [x] Reject first visual attempt and accept corrected matched Unity court/gate views with independent review.
- [ ] Full reference richness remains incomplete: paving rhythm, central gate housing and wider architectural variety still differ.
- [ ] Accumulated phone/crowd/midrange/first-ramp acceptance remains pending; region encounter/boss progression stays deferred.

Evidence: ArtDirection/2026-10-10/foundry-stonework-01. Final suite/APK status belongs to its validation record and latest docs21-23.
## 2026-10-11 - Portal and exterior parapet package

- [x] Replace the central gate boxes with supported recessed dressed stone and one subdued iron lock.
- [x] Give exterior rails stone sill/iron web/copings while keeping pit/bridge open balustrades and existing walking bounds.
- [x] Accept matched Unity detail, overview and actor portraits with independent review.
- [ ] Full reference architectural variety and more natural perimeter/paving rhythm remain incomplete.
- [ ] Accumulated phone/crowd/midrange/first-ramp acceptance remains pending; regional encounters/boss progression stay deferred.

Proof and final gate: ArtDirection/2026-10-11/foundry-portal-parapet-01.

## 2026-10-11 - Foundry mass and perimeter rhythm

- [x] Give three remaining generic distant shafts distinct supported shoulder/exhaust silhouettes within existing budgets.
- [x] Break exterior rail infill repetition with deliberate masonry-panel and open-iron modules inside existing bounds.
- [x] Review actual matched overview and actor portraits independently; retain realistic whole-map limits.
- [ ] Next substantial art: workyard clusters and boiler individuality; inspect oversized painted backdrop composition. Avoid another isolated parapet detail pass.
- [ ] Accumulated device/crowd/midrange/first-ramp acceptance pending. Regional encounters/boss progression remain deferred.

Evidence and final gate: ArtDirection/2026-10-11/foundry-mass-rhythm-01.

## 2026-10-11 - Workyard cluster refresh

- [x] Recompose supported stock and casting groups with distinct work stages inside existing bounds.
- [x] Preserve imported card/material/depth contracts and validate source bounds/projection.
- [x] Reject mismatched boiler overlays, retain only a seated low lid, independently review matched Unity views.
- [ ] Broader workyard distribution, coherent boiler variants and dominant backdrop scale remain reference gaps. Avoid repeated tiny hardware passes.
- [ ] Accumulated phone/crowd/midrange/first-ramp acceptance remains pending; regional encounters/boss progression remain deferred.

Evidence and final verification: ArtDirection/2026-10-11/foundry-workyard-identity-01.
## 2026-10-11 - Foundry cavern composition

- [x] Replace dominant painted flank walls with a versioned foundry-only plate of smaller staggered architecture and calm central depth.
- [x] Preserve old generic cavern, walking/ramp/camera and cover/parallax/ownership contracts; add fallback and coverage checks.
- [x] Independently review actual overview, bridge and actor portraits; preserve honest limits on portrait gain.
- [ ] Improve the now more exposed native distant-shaft/broken-arch material/depth coherence; broader workyard distribution and fully authored boiler variants also remain.
- [ ] Accumulated device/crowd/midrange/first-ramp acceptance remains pending; region encounter/boss progression remains deferred.

Evidence and final verification: ArtDirection/2026-10-11/foundry-backdrop-depth-01.
## 2026-10-11 - Distant architectural surface coherence

- [x] Align native shaft and broken-span masonry scale, cap mapping and face/height light with the new cavern plate.
- [x] Preserve two distant batches, the 2,500-vertex ceiling, ownership and world anchors; inspect matched Unity full-map/detail/portrait views.
- [x] Repair the stale chest telemetry health ceiling with deterministic no-book and health-book regression coverage; production rules unchanged.
- [ ] Next major art slice: authored north-shaft/broken-span silhouettes if navigation remains frozen, or a whole northwest-workyard layout with explicit circulation and blocked footprints.
- [ ] Full reference fidelity and physical-device/crowd/midrange/first-ramp acceptance remain open. Regional gameplay/boss progression stays deferred.

Evidence, final gate and package/device state: ArtDirection/2026-10-11/foundry-distant-material-01.

## 2026-10-11 - North skyline structure

- [x] Differentiate the two north-shaft crowns and connect substantial buttresses into their receding shoulders.
- [x] Replace flat broken-span ends with unequal closed fracture profiles; preserve the 2,500-vertex distant ceiling and existing traversal.
- [x] Capture actual matched Unity views and verify the comparison browser controls.
- [ ] Next substantial art priority: coherent northwest workyard composition, with explicit circulation and shared blocked-footprint contracts before adding raised objects to reachable floor.
- [ ] Full reference fidelity and device/crowd/midrange/first-ramp/motion acceptance remain open; region gameplay/boss progression remains deferred.

Evidence and final verification: ArtDirection/2026-10-11/foundry-north-silhouette-01. This supersedes the pending north-shaft silhouette item above, not the remaining workyard or device items.


## 2026-10-11 - Workyard islands integrated

- [x] Establish loading/transfer/casting grouping inside the west court with two low purposeful work islands.
- [x] Share rendered equipment bounds with actor movement, spawning and pursuit; keep actual paving continuous.
- [x] Preserve existing circulation anchors and matched cameras; add wide yard and two contact portraits without inventing old matches.
- [ ] Physical-device navigation/crowd/midrange/first-ramp/motion acceptance remains open.
- [ ] Reference material/authored equipment richness remains incomplete; next art work should improve the coherent working kit rather than scatter unrelated props. Region encounter/boss progression stays deferred.

Evidence and final validation: ArtDirection/2026-10-11/foundry-workyard-islands-01. This supersedes the pending initial workyard grouping above, not the full-reference/device items.


## 2026-10-11 - Broad court package

Completed visual scope: physical main-court width44->50, west working strip14->20; coherent relocated west assemblies and interior freight yard; connected trolley hardware and detailed mould bench. Baselineb5bcbed comparison: ArtDirection/2026-10-11/foundry-broad-court-01. Map bounds, art, movement and tests remain shared; camera scale retained. Extra floor changes encounter spacing opportunity even though encounter counts/data are untouched; no balance acceptance implied.

Remaining art priorities: overall court-to-gate proportions against the reference, authored equipment/architectural material richness, then accumulated actual-device circulation/crowd/shimmer/midrange acceptance. Avoid substituting small trim changes for structural reference differences. Region progression and bosses remain deferred under owner art-first direction. No claim of full reference completion or installed package.

## 2026-10-11 - Compact gate composition integrated

- [x] Shorten the oversized gate arm18->14u while preserving broad-court width, pit, bridge and work islands.
- [x] Move ramp, gate architecture and far boilers coherently; connect relocated reservoirs, service mains and discharges.
- [x] Replace long northern roof slopes with compact shoulders and distinct upright exhaust crowns using existing batches.
- [ ] Full reference fidelity: native architectural/equipment material richness is still weaker than the approved image. Next substantial art work should address the broad stone/iron surface treatment and readable production detail as a coherent set, rather than isolated trim changes.
- [ ] Accumulated device circulation, crowd, first-ramp, shimmer and midrange acceptance remains open. Region progression/boss design remains deferred under the owner art-first direction.

Evidence: ArtDirection/2026-10-11/foundry-compact-gate-01. This supersedes the initial gate-arm proportion item above, not full map/reference or physical-device acceptance.
