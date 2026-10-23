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

## 2026-10-11 - Industrial furnace complex integrated

- [x] Distinct complete tall furnace profiles with shared original firebox/socket/steam contracts.
- [x] Distinct low foreground retorts with integrated inspection/service details and retained2.28height envelope.
- [x] Lossless two-tile atlases and unchanged runtime batch/depth/material ownership; remove obsolete hatch overlay.
- [x] Independent source/contact and actual Unity art review; corrected closed flues and receiver seating before import.
- [ ] Full reference fidelity remains open: next major visual gap is coherent gate housing/supporting stone architecture and functional pipework, not another small furnace trim pass.
- [ ] Accumulated device circulation/crowd/midrange/first-ramp/shimmer acceptance remains open. Region encounters/boss progression is still deferred by the art-first direction.

Scope/evidence: ArtDirection/2026-10-11/foundry-industrial-01. The five-helper batch closes the furnace-individuality candidate in the prior compact-gate package; it does not declare every map-art or device task finished.

## 2026-10-11 - Gatehouse and stone approach package

- [x] Reduce outer-tower dominance and join the shorter roof shoulders to the gate curtain.
- [x] Simplify the portal parapet and keep the compressed central lock seated inside its stone reveal.
- [x] Improve vertical jamb bearings, restrained recesses and low landing supports in the existing scenery footprints.
- [x] Replace the visually flat approach field with readable staggered stone courses; preserve the actual ramp and threshold.
- [x] Independent source/contact review and second-capture art acceptance; retain the first paving rejection.
- [ ] Full reference material richness and finer architectural articulation remain incomplete. Future work should address substantial coherent surfaces, not accumulate isolated miniature trim.
- [ ] Accumulated physical-device circulation, crowd, HUD/motion/shimmer and midrange checks remain open. Region encounter/boss progression remains deferred.

Evidence: ArtDirection/2026-10-11/foundry-gate-stone-01. This closes the bounded gatehouse/approach work from the prior industrial package, not every map-art or device task.


## 2026-10-11 - Court, bridge and dressed masonry verified; device pending

- [x] Coordinate larger court flags, staggered bridge paving and fitted gate masonry using existing native surfaces.
- [x] Compare fourteen baseline views and independently review actual production images; verify geometry, suites and Android build.
- [ ] Full reference richness and physical-device circulation/crowd/shimmer/midrange acceptance remain unfinished. Regional gameplay progression remains deferred.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-stone-01.


## 2026-10-11 - Pit machinery and exterior masonry verified; device pending

- [x] Coordinate pit vessel profiles/service hardware and exterior masonry within existing envelopes.
- [x] Preserve projected foundation/ramp contact and verify actual Unity images, suites and Android build.
- [ ] Full reference richness and physical-device acceptance remain open.

Next substantial visual work: coherent bridge/entrance architecture and attached foundry work equipment/material finish at the existing navigation footprints. Keep whole-map reference comparison and actor-height readability; avoid isolated colour/trim passes.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-pit-wall-01.


## 2026-10-11 - Bridge architecture and workyard equipment verified; device pending

- [x] Coordinate entrance bridge parapets/support masonry and attached yard equipment within the existing envelopes.
- [x] Verify passage/court seams, keeper seats, equipment containment, actual Unity images, suites and Android build.
- [ ] Full reference richness and physical-device acceptance remain open.

Next substantial art candidate: coherent crane/loading assemblies and connected industrial service structures at the established western perimeter, assessed against the whole reference and at actor-height scale. Avoid another isolated trim pass; preserve circulation and cap geometry/resource growth.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-bridge-yard-01.


## 2026-10-11 - Western loading assemblies verified; device pending

- [x] Integrate connected loading supports, cast crane, distinct receiving and casting stations.
- [x] Validate source frames, actual Unity imports/captures, support geometry, full suites and Android build.
- [ ] Device acceptance and full reference richness remain open.

Next coherent art package: reduce the visual dominance of the northwest service shaft through joined shaft/head/side-return architecture and local material hierarchy, reviewed against the entire reference and the casting/crane portraits. Preserve the existing scenic envelope, circulation and camera; do not add another row of small trim or grow every wall mesh. Keep full-map scale and foreground/background separation as acceptance criteria. Physical-device acceptance remains separate.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-loading-bay-01.


## 2026-10-11 - Connected service shafts verified; device pending

- [x] Connected narrower northwest shafts with recessed faces and hollow crowns; source/image review, tests and Android build.
- [ ] Full reference fidelity and physical-device acceptance.

Next substantial visual review: compare the whole court, pit and gate against the reference at matched scale, then prioritize the largest remaining material/structural mismatch rather than another isolated trim pass. Keep the complete new loading area and service shafts together for the next authorized phone review.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-service-shafts-01.


## 2026-10-11 - Court, pit and gate construction verified; device pending

- [x] Combined court medallion, pit bearing and recessed gate package; actual comparison, full suites and APK.
- [ ] Full reference fidelity and device acceptance.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-pit-gate-01.


## 2026-10-12 - Court, pit and gate material integration verified; device pending

- [x] Combined gate stone grain, pit cast-iron/stone material and dressed court integration with actual image review, full suites and APK.
- [ ] Full reference fidelity and device acceptance.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-material-integration-01.


## 2026-10-12 - Foundry court hierarchy verified; device pending

- [x] Combined western working apron, lower service skyline and fortress/bridge masonry package, actual matched captures, full suites and APK.
- [ ] Reference footprint/massing comparison and physical-device acceptance.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-court-hierarchy-01.


## 2026-10-12 - Foundry forecourt layout verified; device pending

- [x] Combined forecourt redistribution, boundary architecture and shared movement validation package, actual matched captures, full suites and APK.
- [ ] Reference footprint/massing comparison and physical-device acceptance.
- [ ] Gameplay follow-up: investigate stationary immortal-crowd queues at pursuit waypoints; retained bounds-only A/B and failing diagnostic are in validation/crowd-diagnostic.md. No crowd convergence or balance acceptance is implied by this layout slice.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-forecourt-layout-01.


## 2026-10-12 - Foundry ceremonial architecture verified; device pending

- [x] Combined ceremonial court composition, seated gate masonry and primary/secondary fortress/bridge hierarchy; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-ceremonial-architecture-01.


## 2026-10-12 - Foundry court paving and workyard verified; device pending

- [x] Combined radial court paving, complete processional flag fields and live crane machinery housing; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-court-workyard-01.


## 2026-10-13 - Foundry stone craft verified; device pending

- [x] Combined broad quarry paving, supported gate shoulders and stone pit guards; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-stone-craft-01.


## 2026-10-13 - Foundry surface finish verified; device pending

- [x] Combined calm quarry diffuse, partitioned gate header courses and local architectural contact finish; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-surface-finish-01.


## 2026-10-13 - Foundry pit craft verified; device pending

- [x] Combined dressed pit coping and formed cast-iron hood finish; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-pit-craft-01.


## 2026-10-13 - Foundry entrance craft verified; device pending

- [x] Combined continuous bridge-to-court fitted paving and restrained quarry mineral/wear finish; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-entrance-craft-01.


## 2026-10-14 - Foundry landmark craft verified; device pending

- [x] Combined splayed gate reveals and local work-apron use finish; matched actual captures, full suites and APK.
- [ ] Further reference material/landmark refinement and physical-device acceptance.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-13/foundry-landmark-craft-01.


## 2026-10-14 - Foundry material source study verified; device pending

- [x] V7 slate and isolated V2 cast iron integrated; matched actual captures, full suites and APK.
- [ ] Coherent gate/adjacent masonry refinement at portrait scale; physical-device material/motion acceptance remains pending.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this visual package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-material-study-01.


## 2026-10-14 - Foundry gate masonry verified; device pending

- [x] Refine the existing gate cornice, fit dressed enclosure coping and clear the central closed-door threshold rail; matched captures and full verification.
- [ ] Continue the smoother side-tower masonry toward the approved concept; physical-device material/motion acceptance remains pending.
- [ ] Existing stationary immortal-crowd waypoint queues remain a separate gameplay follow-up; this package changes no solver.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-masonry-01.


## 2026-10-14 - Foundry gate stonework verified; device pending

- [x] Fit broad stone courses to the recessed gate shafts, partition adjacent curtain masonry and refine lower buttress mineral finish; matched captures and full verification.
- [ ] Continue courtyard surface hierarchy and larger architectural/material coherence toward the approved reference; see next-visual-priorities.md.
- [ ] Physical-device material/motion acceptance and existing immortal-crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-stonework-01.


## 2026-10-14 - Foundry court cohesion verified; device pending

- [x] Combine broad courtyard use, supported mould-bench storage, crane/gate/pipe material cohesion; verify matched captures and complete suites.
- [ ] Continue secondary service architecture, working-shelf bearings and near-island masonry; see package next-visual-priorities.md.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-court-cohesion-01.


## 2026-10-14 - Foundry bearing architecture verified; device pending

- [x] Combine service-crown bearings, workshop shelf/cantilever supports and entrance/undercroft mineral continuity; verify matched captures and full regression/build gate.
- [ ] Recover fine mineral breakup at close range while preserving the new shared grain; then address remaining broad service-tower and perimeter stone hierarchy. See package next-visual-priorities.md.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-bearing-architecture-01.


## 2026-10-14 - Foundry stone detail verified; device pending

- [x] Combine fixed-scale stone detail, service-pier recesses and entrance haunch masonry; verify native matched captures and full regression/build gate.
- [ ] Improve coherent foreground material/detail families and existing work-area composition; preserve quiet combat lanes and current budgets. See package next-visual-priorities.md.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-stone-detail-01.


## 2026-10-14 - Foundry workyard finish verified; device pending

- [x] Combine service-stone value hierarchy, native workyard iron/timber coherence and bounded equipment-contact paving; verify matched captures and full regression/build gate.
- [ ] Address the large native gate-stone/authored-gate mismatch as one grouped landmark pass, judging full-map and portrait impact first. See package next-visual-priorities.md.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-workyard-finish-01.


## 2026-10-14 - Foundry gate depth verified; device pending

- [x] Group outer gate masonry, native pitched header shoulders and deterministic runtime pipe/card integration; verify matched captures, full suites and Android build.
- [ ] Extend coherent masonry/material treatment to inner gate piers and curtain/header transitions as one bounded landmark pass.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-depth-01.


## 2026-10-14 - Foundry gate cohesion verified; device pending

- [x] Group fitted inner jambs, real curtain bays/pitched coping and clean gate-local mineral mapping; verify native matched images, full suites and Android build.
- [ ] Address larger court/landing paving hierarchy, existing workyard groups and near-island stone faces as coherent scene-level passes.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-cohesion-01.


## 2026-10-14 - Foundry scene cohesion verified; device pending

- [x] Group localized floor heat/finer grain, supported workyard storage and near-island stone cohesion; verify matched native views, full suites and Android build.
- [ ] Reduce directional paving appearance and improve meaningful workyard silhouettes/occlusion within the remaining geometry budget.
- [ ] Physical-device acceptance and existing crowd waypoint congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-scene-cohesion-01.


## 2026-10-14 - Foundry material family verified; device pending

- [x] Group one paving material replacement, complete moulding bench rework and broad bearing hierarchy with matched captures and full validation.
- [ ] Reference fidelity, phone acceptance and the existing crowd congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-material-family-01.


## 2026-10-14 - Foundry gate massing verified; device pending

- [x] Group complete outer gate-bay massing and connected upper-curtain construction with matched captures and full validation.
- [ ] Reference fidelity, phone acceptance and the existing crowd congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-gate-massing-01.


## 2026-10-14 - Foundry inner piers verified; device pending

- [x] Rework the complete inner-pier bearing/shaft/head and family finish with matched captures and full validation.
- [ ] Reference fidelity, phone acceptance and the existing crowd congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-14/foundry-inner-piers-01.

## 2026-10-19 - Foundry loading trolley verified; device pending

- [x] Integrate the active loading trolley (and both work-island skids) into the worked-iron family with matched captures and full validation.
- [ ] Gate crown/header material study, reference fidelity, phone acceptance and the existing crowd congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-19/foundry-trolley-01.

## 2026-10-19 - Foundry gate crown stone study verified; device pending

- [x] Bounded crown/header stone study: level shared gate patch and slate albedo balance, matched captures and full validation.
- [ ] Fine atlas grain versus baked bays (needs a new/re-baked stone source), inner-pier hue alignment, reference fidelity, phone acceptance and crowd congestion remain open.

Evidence/reproduction: ArtDirection/2026-10-19/foundry-gate-crown-01.

## 2026-10-20 - Foundry court flagstones verified; device pending

- [x] Court laid as irregular flagstone (V9 raster + continuous light lattice), fitted stone cooled to one family; -19k vertices.
- [ ] Next: concept brass inlay network (octagon ring, spokes, lane lines); denser rim braziers/props; furnace towers in dark iron + brass; foundry boss and the upward map ladder; phone acceptance.

Evidence/reproduction: ArtDirection/2026-10-20/foundry-court-flagstone-01.

## 2026-10-20 - Foundry court bronze inlay network verified; device pending

- [x] Concept bronze inlay network (octagon, spokes, lane lines) clipped to open floor.
- [ ] Next: denser rim braziers/props; furnace towers in dark iron + brass; foundry boss and the upward map ladder; phone acceptance.

Evidence/reproduction: ArtDirection/2026-10-20/foundry-court-inlay-01.

## 2026-10-20 - Foundry rim lanterns verified; device pending

- [x] Three rim lanterns on the long south/north rail runs.
- [ ] Next: furnace towers toward dark iron + brass (no local Blender for re-renders); foundry boss and the upward map ladder; phone acceptance.

Evidence/reproduction: ArtDirection/2026-10-20/foundry-rim-lights-01.

## 2026-10-20 - Foundry reference furnace towers verified; device pending

- [x] Tall furnace towers re-rendered toward the concept (dark iron drums, lit windows, brass bands).
- [ ] Next: short retorts and pipe collars; foundry boss and the upward map ladder; phone acceptance.

Evidence/reproduction: ArtDirection/2026-10-20/foundry-reference-towers-01 and ArtDirection/2026-10-20/reference-towers-01.

## 2026-10-20 - Foundry reference retorts verified; device pending

- [x] Causeway retorts joined to the reference tower family.
- [ ] Next: foundry boss and the upward map ladder; pipe collars; phone acceptance.

Evidence/reproduction: ArtDirection/2026-10-20/foundry-reference-retorts-01 and ArtDirection/2026-10-20/reference-retorts-01.

## 2026-10-20 - The foundry is floor 1; the run climbs

Decision (owner, 2026-10-20): the foundry is the normal game's first floor, not a development preview; the fiction turns
from a descent into an upward climb (maps stacked above, each harder); per-floor difficulty will follow a formula; the
floor's boss walks out of the gate and the opened gate is the way up (next slices).

This slice: every release build plays the foundry (`DevelopmentStart.FoundryRegion()`); the Editor and development builds
may still name `hall` (the legacy arena) or `wide` in `development/region-preview.txt`. Scene tests default to `hall`
(`TestProfile.Begin`) so their measured hall geometry stays explicit; foundry tests already name `foundry`. Player-facing
text: "Checkpoint: extract or climb?", "Climb", "Ascent complete", "Highest floor cleared"; floor 1's display name is
"The Foundry" (its id `floor_ember_halls`, saves and telemetry are unchanged). Code identifiers (Descend, Descent) are
not renamed yet.

Verification: 438 .NET / 712 EditMode / 160 PlayMode, zero failures; Verify-Project PASS; APK 36,391,777 bytes,
SHA256 F969E9A40CF0F7459FDC91D4AEBBCD4C039990C29424FFEDA8D2A7F7399D7A32, NOT INSTALLED. One PlayMode test encoded the old
rule (unknown flag -> hall); it now asserts the new rule (explicit hall; missing/unknown flag -> foundry).

## 2026-10-20 - The foundry's boss walks out of the gate; the opened gate is the way up

On the foundry, a boss room's pack is placed on the gate landing in front of the door (`FoundryMap.PlaceAtGate`, the boss
on the door's axis, any escort alternating along the landing) instead of round the hero, and walks to the hero by the
ordinary pursuit. When a floor that ends in a boss room clears, `EncounterController` opens the gate exit
(`IsGateExitOpen`, `FoundryMap.GateExitSpot`, design 26.6, 5) instead of clearing at once: drops stay collectable, an open
choice freezes the hold, and the floor clears (FloorCleared -> checkpoint "Extract / Climb", or Victory on the last floor)
only after the hero stands still on the gate for ExitRule.DwellSeconds, as the finale's exit is held. `ExitSealView`
shows either open exit; the gate reads "GATE - Stand still to climb" / "GATE". The legacy hall keeps its kill-to-clear
flow. A development floor `floor_gate_boss_proof` (one boss room) supports scene tests and device checks.

Tests: `FoundryMapTests` (gate spots on open landing floor, spacing, door axis; the gate boss routes to the court) and
PlayMode `GateBossFlowTests` (boss at the gate; gate shut while it stands; kill opens it; standing elsewhere leaves it
open; a held second walks out; the hall clears on the kill). Lesson: a WaitForSeconds after the kill hung the batch run
when a level-up paused the game; the test now resolves choices and bounds waits by real time.

Verification: 440 .NET / 714 EditMode / 162 PlayMode, zero failures; Verify-Project PASS; APK 36,391,684 bytes,
SHA256 B359447CDDCD6F6DFC8B22139269FA01F06737D95D6B7A21BA23E2AE18C3AEEA, NOT INSTALLED. Not yet done: the painted door
does not visibly open; no boss intro camera; floors above still reuse the same foundry map.

## 2026-10-20 - Climb difficulty by formula

Decision (owner): per-floor difficulty follows a formula. `Combat/ClimbScaling` (pure, in CombatChecks): enemy health
x 1.4^(floor-1), damage 0.92 x 1.4^(floor-1), and a boss room's extra health x 1.15 per floor from floor 3. Floors opt in
with `FloorDefinition._climbScaled`; The Foundry (floor 1) and Quicksilver Vaults (floor 2) climb, and because the formula
reproduces their hand-tuned tiers exactly (1 / 0.92 and 1.4 / 1.288; boss extra 1 below floor 3) no existing balance
changes. The finale swarm keeps its authored tier and its own clock ramp. A floor-3 Warden would have 300 x 1.96 x 1.15 =
676.2 health. `ClimbFloorContentTests` keeps a climbing floor's authored fields equal to the formula, so the hand-mirrored
DescentSimulation stays valid.

Verification: 443 .NET / 718 EditMode / 162 PlayMode, zero failures; Verify-Project PASS; APK 36,392,934 bytes, SHA256
280B0AB067730FB8EE34AF7EE6ED900AE4F58C62071BF3984F43DD28AC87C07F, NOT INSTALLED.

## 2026-10-20 - The foundry gate opens when its boss falls

The painted gate's two leaves swing outward onto the landing over a forge-lit doorway once the floor's boss falls (gate
exit open), framing the gate exit; shut, they re-lay the painting exactly. Own batch (so the facade batch's flame motion
never rewrites them): region 38 renderers / 154,832 vertices. 443 .NET / 718 EditMode / 163 PlayMode, zero failures; APK
36,409,937 bytes, SHA256 D156A70A7B6A90CBA1844F18E7085ADDC6C3532D9BAFA4663BEE8D6FFBD02B7D, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-20/foundry-gate-door-01.

## 2026-10-20 - The camera presents the foundry boss at the gate

When the foundry's boss walks out of the gate (`EncounterController.GateBossArrived`), `BossRevealView` turns the camera
to it over 0.7 s, holds 1.2 s and turns back over 0.7 s, on the game clock (a pause freezes it). Meanwhile the encounter
holds (`HoldForReveal`: no pack movement, no fight clock) and hero movement waits; a tap, the boss falling or the run
ending turns the camera back at once, and disabling the view restores everything. The hall has no reveal.
Tests: `GateBossFlowTests.TheCameraTurnsToTheArrivingBossWhileTheFightWaits` plus the existing gate/door/arrival suites.
443 .NET / 718 EditMode / 164 PlayMode, zero failures; Verify-Project PASS; APK 36,409,705 bytes, SHA256
529544441F51CE8C0581A76187F03D6D42C9300CDB9565495934AE4AEF5271DF, NOT INSTALLED.

## 2026-10-21 - Device check of the gate boss flow; the boss marches across the court

Device (S23, development APK A80484CA..., floor_gate_boss_proof, region foundry): steady 59.9 fps, GPU ~3.0 ms, 49-54 draw
calls, 135k triangles. Seen end to end: camera presents the Warden at the gate, the fight, level-up choice, gate leaves
open over the forge-lit doorway, "GATE - Stand still to climb" with an off-screen arrow, walking to the gate, the hold,
and "Ascent complete". The first device run showed the speed-1 Warden needed ~20 s to reach a waiting hero; the gate
pack now marches at 3.5x its speed until within 4 units of the hero (`PackMotion.SetMarch`, `GateMarchFactor`), 26.3 s ->
9.4 s to the court centre in the pure test, then fights at its own pace. Observation: the 0.9-unit hold radius took a few
nudges to land on by touch. The test run banked 50 gold into the owner's profile (37668 -> 37718, revision 246; the
pre-run profile is backed up). 444 .NET / 719 EditMode / 164 PlayMode, zero failures.

## 2026-10-21 - Dark iron pipework and a wider gate hold

Service mains and boiler feeds join the blackened towers (dark neutral-warm iron, brass collars on elevated mains);
the opened gate's hold radius is 1.3 (finale 0.9) after the device check. 444 .NET / 719 EditMode / 164 PlayMode, zero
failures; APK 36,410,838 bytes, SHA256 DBBB5ADFB09BDA3774590EC991B96A40EF782D07CCDD7A5367A8D252EA5385A2, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-polish-01.

## 2026-10-21 - The open gate shows a lit hall beyond

The open doorway carries a warm receding floor, a far furnace glow (left of centre, where the open leaf leaves it seen)
and a dark vault instead of one gradient; +32 vertices. 444 .NET / 719 EditMode / 164 PlayMode, zero failures; APK
36,412,123 bytes, SHA256 6F1993C9227E49DF9737A9BE5BE0AC39B88912C18DD3F906BA082712762A020A, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-doorway-01.

## 2026-10-21 - Rim fire: the remaining open rail gaps

A survey of every rail position against the placement rules found three open gaps (north rail u -2 and 6, causeway
north rail u 18.5); each now carries a lantern post with its floor pool. The west rail (workyard obstacles, existing
lanterns) and the causeway's south rail (behind the boilers on screen) have no valid position. +303 vertices.
444 .NET / 719 EditMode / 164 PlayMode, zero failures; APK 36,411,342 bytes, SHA256
98ACB99CD9354FB98C391E42147070C8A409FA0D15129B842AFC364E1DAB19E7, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-rim-fire-01.

## 2026-10-21 - Standing braziers light the workyard

No rail post was free beside the workyard, so three standing braziers become work islands 2-4 (solid .9-square
footprints shared by rendering, movement, pack placement and pursuit), each with a floor pool, clear of the freight lanes,
turntable and other islands by at least 1.9. Real obstacles in the yard. +1,197 vertices, no new batch. 457 .NET / 732
EditMode / 164 PlayMode, zero failures; APK 36,413,279 bytes, SHA256
2AC415A86ECFEFFEE73702644F183DBAED7DB3D97DA3777FE310D8A0C690A804, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-yard-braziers-01.

## 2026-10-21 - The concept's violet void

The cavern shader keeps the painting's saturation and blends 10 % toward a lighter violet instead of 72 % desaturation
plus 22 % dark indigo and a dimming tint: void value .17-.18 -> .21-.25 (concept .24-.29), saturation up. No geometry
change. 457 .NET / 732 EditMode / 164 PlayMode, zero failures; APK 36,413,268 bytes, SHA256
B7BE24A69A6DA77AE162C5527AEE9191586F9F224608134C3265D98144DF3344, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-atmosphere-01.

## 2026-10-21 - Banded double gold lines and a double-ringed seal

The court network lines become .30 stone bands edged by two bright .06 metal courses, and a closed outer ring at r 5.3
doubles the seal, as in the concept; +1,328 vertices, same decal batch. 457 .NET / 732 EditMode / 164 PlayMode, zero
failures; APK 36,411,835 bytes, SHA256 9A36B4F5B84033DA778B7275DB7154AA0D20027138172351BF6FC64C430DFF67, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-gold-network-01.

## 2026-10-21 - Open fire baskets on the rim posts

Balustrade lantern posts (and the pit collar's corner posts) carry the gate's open fire basket instead of an enclosed
lantern, as on the concept's rim; placement and floor pools unchanged, +2,108 vertices, no new batch. 457 .NET / 732
EditMode / 164 PlayMode, zero failures; APK 36,412,761 bytes, SHA256
8895C265833F4B3DF1F845F72CE60E8E3E8ABCFE7DB3D021EA8EDF65B9017F66, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-rim-braziers-01.

## 2026-10-21 - Floor value lifted to the concept's court brightness

Flagstone and fitted paving gains rise 14 % (hue kept): court value .30 -> .35 (concept .35); fixed4 bound 1.78 < 2.
457 .NET / 732 EditMode / 164 PlayMode, zero failures; APK 36,412,761 bytes, SHA256
D2A071024BF8DB260E7E2B314024F7157319063309C8486B6970694BF421BDE3, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-floor-value-01.

## 2026-10-21 - Heavier abyss chains

Chain links at 1.4x width and two more chains (west middle bastion, east south pier), as the concept's massive rim
chains; -496 vertices. 457 .NET / 732 EditMode / 164 PlayMode, zero failures; APK 36,412,611 bytes, SHA256
66C39BEE7DBD8400EB13A971A12ADA6EDDFE966945C7EA6B22C020578DAA7EA4, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-chains-01.

## 2026-10-21 - Rim stone takes the concept's violet battlement tone

Above-deck rim stone tinted (.80,.77,.92) -> value .18 / saturation .32 (concept .18-.19 / .32); below-deck rim faces
(.90,.88,1.06) -> saturation .25. No geometry change. 457 .NET / 732 EditMode / 164 PlayMode, zero failures; APK
36,413,341 bytes, SHA256 2AA02A3A7B3701DC093C18C04393DDE4495E84DDB46895FC28B3C132A0DDFAD0, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-rim-wall-01.

## 2026-10-21 - Brass collars and finials on the rail posts

Plain balustrade and pit collar posts gain brass collars and finials as on the concept, in the existing brazier ironwork
batch; layout unchanged. 457 .NET / 732 EditMode / 164 PlayMode, zero failures; APK 36,414,225 bytes, SHA256
F89C4F2522C2CCA5D21DF4D48E44BD2AE7DDA9233F7DB758C6E8FC945E4AFC0A, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-post-brass-01.

## 2026-10-21 - Furnace pit: the concept's oval ring and raised corner pedestals

The pit's collar band is now a circle (radius 5.69 about (-10, 8); a 2:1 oval on screen, as on the concept) instead of
a rounded square; the walkable square is unchanged. The four non-walkable corners of the square are raised stone
pedestals (.42 high, 15 degrees either side of each diagonal) carrying the rail's tall lantern posts at the far and side
corners and a standing fire basket at the near one; eight band posts (four new .56 corner posts). The pool rises to 4.2
down (courses below the deck scaled 4.2/5.2) so it shows past the round collar's near rail. The pit-guard openness test
is measured against the bare frame for runs seen edge-on. 457 .NET / 736 EditMode / 164 PlayMode, zero failures; APK
36,418,740 bytes, SHA256 DD31BA2BF112F6EEDECF522459EBD76427CAA7BDE22AD931FFF452466C1130AA, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-oval-pit-01.

## 2026-10-21 - Court floor laid in the concept's cut stone

The open court's raster is now FoundryPavingV10 (Tools/ArtReview/GenerateFoundryCutStone.py): hand-cut stones .8-1.6
long in loose courses .72-1.2 deep, thin joints, rounded corners, a lit arris, quiet per-stone value and occasional
cracks - about the concept's stone size (~.8 hero height) instead of V9's large honeycomb flags. Same mean value (.52),
material gain, mesh and light field; ceremonial and fitted paving unchanged. 457 .NET / 736 EditMode / 164 PlayMode,
zero failures; APK 36,418,740 bytes, SHA256 EE4F44FDDCE4CCE8F8A3C23443C110C0570DD24595217FB58B090CB453BDB8F9, NOT
INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-21/foundry-cut-stone-01.

## 2026-10-22 - The seal's field and the processional ways in cut stone

The seal's inner field (radius 3.75) and both processional ways are now laid in the court's V10 cut stone (flagstone
batch, light lattice only) instead of large fitted slabs: the field in the court's world field, each way in a field
turned to run with it at 1.35x density so its courses lie across the way. Ring stones, brass, compass, bridge
continuation and gate apron unchanged. Region 157,941 vertices (-4,395). 457 .NET / 736 EditMode / 164 PlayMode, zero
failures; APK 36,419,732 bytes, SHA256 9924FBD1942383FC7C550BDDE2FB40807FA20B89D8D18ABA734258692DA97F26, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-22/foundry-seal-stone-01.

## 2026-10-22 - Hanging keels below the platform

Under every camera-facing foundation span, stepped inverted masonry keels (FoundryRegionKeels.cs) now hang from the
curtain skirt's foot (7.1 down) in two or three steps to ~15, in the bastions' weathered masonry, fading into the
cavern haze - the concept's floating-fortress underside instead of a level skirt. First in the below-deck batch, so
every structure in front paints over them; none under the gate causeway. Region 159,005 vertices (+1,064). 457 .NET /
737 EditMode / 164 PlayMode, zero failures; APK 36,458,123 bytes, SHA256
39A753D5C24FB46DC08314A1C05AAA24B92A5CE02417E05E9244B7FF3FBB324B, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-22/foundry-keel-01.

## 2026-10-22 - Bridge keepers at the concept's scale

The bridge-head keepers grow from 3.8 to 5.4 high, darkened toward the concept's iron, on a .62 plinth block under a
seat scaled with them; their posts move out (7.6 from the axis, so the cards stay outboard of the walking width on
screen) and down the bridge (5 before the west seam, so the west plinth clears the court's south rim). Console,
banners, depth footprints and the balustrade's post box follow. Region 159,317 vertices (+312). 457 .NET / 737 EditMode
/ 164 PlayMode, zero failures; APK 36,458,659 bytes, SHA256 A7266655680FEB6CECCD4E749D0120FDEC8DF5B861B313F01DC9B39FFD27E07A,
NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-22/foundry-keepers-01.

## 2026-10-23 - Bridge deck in cut stone, with the concept's diamond

The bridge's walking quad is laid in the V10 cut stone (flagstone batch, light lattice only) in its own field, courses
across the bridge; the way to the bridge and its continuation down the deck share that field, so they run on without a
seam. The fitted deck lattice and continuation slabs are removed; a brass diamond inlay sits between the lanes below the
keepers. Region 156,729 vertices (-2,588). 457 .NET / 737 EditMode / 164 PlayMode, zero failures; APK 36,454,646 bytes,
SHA256 CB35D7379218BBF4C01CA2B20BB36EDF8222EC758D73BA39F3B1FB128E54EDBD, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-bridge-stone-01.

## 2026-10-23 - Gate apron in cut stone

Paving zone 2 (the gate apron) joins the court's V10 laid stone; the gate seal inlay, dressed threshold and border, the
seal ring and the work-apron repairs stay fitted. Region 153,306 vertices (-3,423). 457 .NET / 737 EditMode / 164
PlayMode, zero failures; APK 36,454,649 bytes, SHA256 BC8F90144B9D802DD5E5B378B6B1639D040201961B889EA0B7F84B37179505B0, NOT
INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-apron-stone-01.

## 2026-10-23 - Rim main along the north rail

A heavy brass-banded main (and a lighter return) runs from the north-west boiler along the outside of the court's north
rail on brackets, behind the rail in the masonry batch, as on the concept. Region 155,354 vertices (+2,048). A first try,
yard stock stacks as work islands, was reverted (pursuit stall, passage mouth, too small at map scale). 457 .NET / 738
EditMode / 164 PlayMode, zero failures; APK 36,457,456 bytes, SHA256
02AAE77877E59C1F43A6F5CF77FC27D04ECA46FAFCA42E8D5892B782714C5A0B, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-rim-pipes-01.

## 2026-10-23 - Pit vessels in blackened iron with brass straps

PitMetalTone moves from olive-neutral to the concept's violet-black iron and the hood straps take the braziers' bronze.
An east rim main was tried and reverted (crossed the squat boiler, hidden by the gate tower). 457 .NET / 738 EditMode /
164 PlayMode, zero failures; APK 36,457,765 bytes, SHA256 FDDBEAD4B6F97AEC24C273CC637C4A9591B28C74A7F9D8FF9312D66939877A48,
NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-pit-iron-01.

## 2026-10-23 - Pit vessel stacks

Each pit vessel's flue rises as a brass-banded blackened stack to 3.1 with a fire basket on top, as on the concept.
Region 156,082 vertices (+728). 457 .NET / 738 EditMode / 164 PlayMode, zero failures; APK 36,459,130 bytes, SHA256
F018FEA82414B73DE3CF4EB539640FD02B9A4D9E0F83F9D720A8BAB3A807FA0C, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-pit-stacks-01.

## 2026-10-23 - Rail lantern shrines

Four mid-run rail posts carry the concept's small stone lantern shrine (lit round-headed niches, cornice, pyramid roof,
brass finial) instead of a fire basket; corner posts keep their fire. 457 .NET / 738 EditMode / 164 PlayMode, zero
failures; APK 36,465,383 bytes, SHA256 F8C1C1491FA4D63DDC26B60A709086D9F41F04C906A02D6E1B79D61C35C7A4AB, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-rail-shrines-01.

## 2026-10-23 - Closer rim post rhythm

Regular rail posts now stand at most about two units apart (RailPostSpacing, was three), closer to the concept's heavy
rim rhythm. Region 157,434 vertices (+1,784). 457 .NET / 738 EditMode / 164 PlayMode, zero failures; APK 36,465,043
bytes, SHA256 DD3B3B99BE4560598E0C4457264476F031A8B13E22726CDFF21D1DDE367FF0CF, NOT INSTALLED.

Evidence/reproduction: ArtDirection/2026-10-23/foundry-rail-rhythm-01.

## 2026-10-23 - Foundry halls open at their own districts (gameplay)

On the foundry, a floor's combat and elite rooms after its first open at districts of their own (workyard, north court,
east court, forecourt): the room waits, marked with the exit seal and an off-screen arrow ("NEXT HALL"), until the hero
walks onto it, then its pack forms up there. Forge, gate boss, swarm and the hall region unchanged; scene-only (the
Descent simulation is unchanged). New RoomSiteFlowTests. 457 .NET / 738 EditMode / 166 PlayMode, zero failures; APK
36,467,560 bytes, SHA256 B936F55EDF5DF2527DD670CA281E4A08421A2045AE348AC81336BFA4565DBEC4, NOT INSTALLED - pacing not yet
played on device.

Evidence: ArtDirection/2026-10-23/foundry-hall-sites-01.
