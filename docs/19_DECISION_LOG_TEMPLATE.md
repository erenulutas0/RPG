# Decision Log

## 2026-09-19 — Build the gameplay around Megabonk and Bones and Coins

**Decision:** the references are Megabonk (for the run: stages with a timeline, swarms, a boss the player reaches when
ready, tomes and chests, a stat sheet, a difficulty dial) and Bones and Coins (for the meta: banked coins into a
permanent tree). Four choices made by the owner: BONK means Megabonk; books come from chests and cards from level-ups,
two distinct reward moments; a swarm room can be left early with the XP earned so far; floors aim at 4–6 minutes and a
Descent at 15–20. One principle over all of it: **even the strongest build dies if it stays too long** — a swarm that
does not stop, as Megabonk's Final Swarm, is the run's ceiling, and balance is tuned to that ceiling rather than to a
build that can idle. Art is untouched; the roadmap is docs/27 section 4.

**Why:** docs/26 Part 5 and docs/23 showed the movement budget changes the rhythm of a fight and not its pressure, and
that pressure comes from crowding; a stat sheet and books give the player a build, a swarm room gives the game its
length and its ceiling. Books and cards are one system because `UpgradeOption` already is a tome.

**Evidence/limits:** docs/27 (sources listed there). Nothing implemented; S1 (offer engine and seed) is the first slice.

## 2026-09-18 — Integrate platform materials with separate foundation ownership

**Decision:** replace the top/coping/near faces with imported materials on nine cached meshes; retain only keel/chains/crystal from the procedural platform. Omit old corner towers/flames until matching props exist. Use exact shared corner positions and a narrow dark masonry lip to close the near-wall join. Keep arena geometry, camera and combat unchanged.

**Why:** layering new art over the complete old platform left pixel outlines and mismatched fixtures protruding through the coping. Separating the foundation resolves the overlap without repainting generated masters or moving the playable boundary. A full-resolution proof allocation was inappropriate for mobile;1024-capped mipmapped ASTC imports reduce selected texture payload to about1.53MiB. Coping uses power-of-two sampling because1024x341 fell back to RGBA32 despite the ASTC override.

**Evidence:** full272/284/104 gate, graphics1, integrity415/555, actual S23 density/near-rim and warm-normal-restart review. Source copies/hash, ownership tests, gallery and limitations are in platform-runtime-01. Lower-foundation art remains visibly provisional; no new boss room or sustained-performance acceptance.

## 2026-09-17 — Painted platform materials before runtime replacement

**Decision:** prefer quieter V2 basalt for the next environment integration. Keep current arena geometry and combat rules; prove the floor, flush coping and near walls in an isolated real scene before allocating runtime textures.

**Why:** the painted Vanguard/Mite/Grunt expose the procedural floor's brighter seams and different material fidelity. V1 retained too much edge contrast; V2 leaves the characters and effects in front. This is a material decision, not acceptance of the complete arena.

**Evidence/limits:** fourteen paired/variant/short-screen Unity captures in `platform-material-01`, using the real frozen ten-enemy room. Rim views reposition the hero/camera, not the enemies. Old outline, corners, lower keel, fixtures and chest still show mixed fidelity; crowd occlusion is unchanged. No authored Assets/APK/device change. Next is join cleanup and a measured runtime import/cache/device slice.

## 2026-09-17 — Grunt runtime art without changing attack timing

**Decision:** integrate selected Slag Brute front/rear/contact/passing/strike imports as a shared EnemyArtSet. Preserve idle/contactA/contactB/strike indices; append optional passingA/B for a four-pose gait. Mite retains its original four-frame set. Configure visible top and contact shadow separately from the padded canvas, and remove Grunt's old procedural attack nudge.

**Why:** the rear needed a clearer diagonal view and the contact-only proof reused idle as a passing pose. Source-sheet windup does not match the existing immediate damage event and must not quietly delay combat. Large transparent padding is not a useful bar-height measurement.

**Evidence:** full272/284/100 suites, capture1,404/555 integrity; art sources/registration/rejections in grunt-polish-01 and device record in Docs/23. Strict foot locking, volume continuity, mirrored lighting and projected depth overlap remain separate polish. No stats, spacing or gameplay scene changes.

## Decision: Register Grunt keyposes without changing combat timing

**Date:** 2026-09-17
**Status:** Isolated Unity proof; runtime integration pending

Use explicit sheet rectangles and rigid per-pose floor-root registration at one scale. Automatically fitting each pose's alpha bounds would hide generated shape drift and make the enemy grow/shrink as its arm rises. Uniform sampling instead exposes that drift for review. The source sheets are not exact equal-cell grids; retain their untouched originals and measured registration. Keep existing room actors/bars/floor in the before/after capture so overlap and raised-fist/bar crowding remain visible.

The six keyposes include a windup, but the current attack deals damage immediately. Do not delay gameplay or fake an anticipatory cue from an already delivered attack merely to use the artwork. Decide presentation timing explicitly at integration; a true telegraph would require a separately scoped gameplay/parity change. The existing four-pose enemy art contract remains unchanged. See `ArtDirection/2026-09-17/grunt-motion-01/README.md`.


## Decision: Compact basalt Grunt, before production animation

**Date:** 2026-09-17
**Status:** Design candidate; not integrated

Use broad shoulders, low head, oversized stone fists and a small furnace vent for the ordinary Grunt/Slag Brute. A taller flame-crowned version would compete with the Mite and obscure more combat; conventional armour would compete with the hero. Keep the initial height close to the existing .9375-unit canvas while checking the wider new silhouette explicitly. The browser close-pair comparison exposes persistent overlap; this is not a spacing fix. Front/rear independent generated masters require angle/shape reconciliation and registered feet before a motion proof. See `ArtDirection/2026-09-17/grunt-proof-01/README.md`; runtime art and the phone are unchanged.


## Decision: Bounded contact and grip polish before roster expansion

**Date:** 2026-09-17
**Status:** Integrated and checked on S23

Remove the hero's old vertical nudge and shorten the distance per four-pose cycle, preserving a single owner of body position. A second runtime foot-offset system would fight `CombatantView` and could introduce visible jumps between the sparse poses; proper world-space foot locking remains a separate animation task. Use eight tiny registered crops of the existing front gauntlet instead of regenerating the body or adding a clipping shader. One overlay renders those authored knuckles above the sword; rear equipment is already behind the body. The whole-body damage silhouette renders above the new hand layer. No gameplay timing changes.

Scale the Mite bar's parent only, retaining shared textures and the fill's left edge and fraction. Other enemies keep their existing bars. These are incremental presentation fixes, not final character animation or resolution of overlapping large-enemy silhouettes.


## Decision: Imported Mite poses and independent pooled death

**Date:** 2026-09-17
**Status:** Integrated prototype; final enemy animation still open

Use a small shared `EnemyArtSet` for the first imported enemy, preserving the existing procedural fallback. Fixed registration across front/rear/walk/strike/death keeps its feet and scale comparable. One generated front contact faced the wrong way; reject it and import a corrected sibling, retaining both source and exact prompts.

Play death in the existing effect pool instead of keeping the dead enemy active. This preserves immediate XP/kill accounting and room progression, avoids a corpse-specific object allocation per kill, and survives wave teardown. The tradeoff is bounded-pool replacement under simultaneous effects and a shared front/rear collapse. Sword/spark assets use the same ownership rule; imported frames must not enter runtime-generated sprite cleanup. New floors, enemy density/balance, hero collision and seeded upgrades are outside this art slice.


## Decision: Keep common pose registration and reject a repeated footfall

**Date:** 2026-09-17
**Status:** Equipment/keypose review, not production animation acceptance

Generate equipment separately from the body and keep a common source crop, output canvas and pivot across poses. Independently cropping/rescaling each pose would hide drift by changing the knight's size and would break equipment alignment. The first B attempt repeated A's extended leg; a targeted leg-only correction now alternates the footfall. Retain the rejected source for provenance and exclude it from playback. Match the sword/buckler's blue-steel/silver/brass rendering to the body before expanding other loadouts.

The two contacts and one windup prove useful adjacent art, but do not prove a finished gait or attack: idle is only a temporary passing frame, sole/contact behaviour needs work, and strike/recovery is absent. Keep this blocking proof in ArtDirection and the isolated Unity capture tool until movement, hand occlusion and opposing facings pass review. See `ArtDirection/2026-09-16/hero-motion-01/README.md`; no production import or phone replacement is part of this decision.

## Decision: Carry 128-PPU painted actors forward as a candidate, with equipment and movement still gated

**Date:** 2026-09-16
**Status:** Unity art comparison completed; production import contract not yet adopted

Compare the same source at 32, 64 and 128 PPU without changing world height. Mipmapped GPU reduction avoids judging the low-resolution cases from sparse unfiltered samples. The painted master loses shape/material cues at 32; 128 with bilinear display best preserves the proposed blue-steel/basalt look in this review. This is not evidence against purpose-drawn low-resolution pixel art. Keep the current runtime contract while preparing one matching loadout and a real movement proof; do not change floor/effect grids to match a concept image automatically.

Actual scene renders in `ArtDirection/2026-09-16/character-unity-01/` expose the mismatch between the new body and old pixel weapons. Repositioning the grip fixes attachment location, but not hand occlusion, style or gait. The twelve-frame rigid translation/sword rotation is expressly a diagnostic, not a walk cycle. No APK or imported game sprite changed in this slice.

## Decision: Prove a separate hero body and one low ember enemy before expanding the roster

**Date:** 2026-09-16
**Status:** Concept candidates prepared for owner review; not production sprite approval
**Owner:** Art, following the approved hero/Mite proof

The first hero exploration combined weapons with the body and hid too much of the legs with a broad cape. Retain its provenance but use an edited empty-handed blue-steel body with a shorter split cape for the next proof. Separate equipment matches the existing Sword/Staff/Daggers structure and avoids baking one loadout into every animation. For the Mite, choose a low basalt quadruped with broad ember seams and a compact flame instead of many thin limbs or a large smoke/effect silhouette. Both candidates use the same faceted painted material language; cool hero mass versus warm enemy cues carries recognition when reduced.

`ArtDirection/2026-09-16/character-proof-01/` records unchanged masters, exact prompts, hashes and the browser scale review. Keep the 32-PPU point-filtered runtime contract until a controlled import comparison justifies a change. Current generated masters are antialiased and do not meet that contract directly. A single pose proves neither animation consistency nor weapon alignment; opposing facings, fixed foot pivots and a short movement/attack proof precede production acceptance and roster expansion.

## Decision: Quiet slab materials and one shared contact patch

**Date:** 2026-09-16
**Owner:** Art implementation, following the approved stone/rim/contact slice

Extend the current deterministic floor drawing with upper bevels, broad stone patches, sparse chips and flush coping/brass clamps. A fully painted replacement would require a separate projection/seam/import proof; more bright lava or dense surface noise would compete with enemies. Retaining the silhouette and texel contract lets this slice improve material structure without changing walkable space or established camera margins. Sidewalls, corner props and character sheets remain prototype artwork rather than being described as finished.

Use one 32x16 translucent contact sprite for hero/enemies, with independent root-level renderers. Reuse the existing session cache instead of drawing a texture per enemy or introducing a second cache owner. Body hit/attack nudges cannot lift the ground patch; death hides it. A conservative diamond fit contracts the footprint near the rim. This was chosen over a new masking shader or a per-frame clipped mesh for this small visual cue. It avoids spill onto the void at the cost of visibly smaller shadows very close to an edge; it is not a physical directional-light solution. No gameplay body, spacing, damage, seed or upgrade rules change.

Pure tests sweep the full shadow rectangles across symmetric and shifted geometry. PlayMode verifies ground sorting, body independence, death, restart reuse, unused-asset cleanup and session resource release. Art provenance/review images are in `ArtDirection/2026-09-16/stone-contact-01/`; final gate and device observations belong to `21`–`23`.

---

Use one entry per meaningful irreversible or expensive decision.

---

## Decision: [Title]

**Date:** YYYY-MM-DD  
**Status:** Proposed / Accepted / Reversed  
**Owner:**  

### Context
What problem are we solving?

### Options
1. Option A
2. Option B
3. Option C

### Decision
What did we choose?

### Why
Why is this the best choice now?

### Consequences
Positive:
- 

Negative:
- 

### Revisit trigger
What evidence would cause us to reconsider?

---

## Suggested first decisions to record

- portrait vs landscape,
- exact Unity version,
- sprite logical resolution,
- first hero archetype,
- first biome identity,
- run structure: rooms vs continuous waves,
- analytics provider,
- monetization provider (later).

---

## Decision: Pin the first combat prototype and defer persistent services

**Date:** 2026-09-12  
**Status:** Accepted; Editor- and device-verified on 2026-09-12 (see the update at the end of this entry)  
**Owner:** Project engineering

### Context
The repository contained only documentation. The first requested slice is one hero automatically killing one enemy, with Unity 6, portrait orientation, and minimal dependencies.

### Options
1. Built-in 2D rendering with one authored gameplay scene and placeholder shapes.
2. URP and additional bootstrap/menu/service setup before proving combat.

### Decision
Pin Unity 6000.0.65f1 and Unity Test Framework 1.4.3. Use built-in SpriteRenderers, one portrait Gameplay scene, a temporary read-only IMGUI HUD, and Vanguard/Sword/Grunt definitions. Put mutable health and weapon state in ordinary per-instance C# objects. Use one runtime assembly and two test assemblies. Initialize Git on main.

### Why
This supplies the smallest requested playable slice while preserving the documented content/runtime separation. There are no persistent services requiring a Bootstrap scene yet. The exact Editor release and Test Framework version were verified against official Unity sources linked in `21_DAY_1_3_IMPLEMENTATION_PLAN.md`.

### Consequences
Positive: limited package surface, explicit scene wiring, independently testable combat rules, and editable balance data.

Negative: the authored Unity files still require import/compilation and visual validation in the pinned Editor, which was not installed on the implementation machine. IMGUI is a temporary presentation choice and should be replaced with a Canvas for touch upgrade selection. No package lock is claimed until Unity resolves the project.

### Revisit trigger
Replace the temporary HUD when implementing upgrade choices. Add persistent bootstrap services only when required. Add Pixel Perfect/Aseprite when producing actual pixel art. Reconsider the Editor pin only for an identified compatibility, stability, or security need; do not casually upgrade during MVP.

### First Editor validation update
The installed 6000.0.65f1 Editor resolves its bundled Unity Test Framework 1.6.0 and NUnit 2.0.5. Updated the manifest to 1.6.0 to match the tested, effective package version and retained Unity's generated lockfile. Removed the nonexistent `com.unity.modules.textrendering` dependency after import identified it. All 26 EditMode and five PlayMode tests now pass in Unity; Android validation is tracked in `23_ANDROID_DEVICE_VALIDATION.md`.

---

### Upgrade-slice update (2026-09-13)
The IMGUI HUD was replaced by a uGUI Canvas as planned; `com.unity.ugui` 2.0.0 is the version built into the pinned Editor. Upgrade choices pause combat through `Time.timeScale`, owned by `CombatSetup` and restored when the offer closes or the scene unloads. Revisit when a result screen or app-level pause also needs to control time, at which point a single run-flow owner should take over.

### Pause update (2026-09-13)
Player and app-level pause arrived, so a single owner took over: `RunPause` combines the open-choice hold and the player's pause (also set by `OnApplicationPause`), and `CombatSetup` is the only code that writes `Time.timeScale`, from `RunPause.IsFrozen`. The result screen and Relic Forge run after the run has ended and do not freeze time.

---

## Decision: Multi-floor "Descent" run structure with an Extract/Descend checkpoint

**Date:** 2026-09-12  
**Status:** Accepted 2026-09-12 (applied to `03`, `05`, `10`, `15`)  
**Owner:** Product / engineering

### Context
`03_GAME_DESIGN_DOCUMENT.md` recommends a 5-room checkpoint run. Round-2 research (`24_RESEARCH_ROUND_2_REFERENCE_AND_FUTURE.md`) shows the reference game is punished for having no failure state, no risk and no loop after its ending, while praised mobile roguelites (Archero, Shiba Story Go) use a stage → relief → boss cadence with a choice every 30–60 seconds.

### Options
1. Keep a single 5-room run ending at one boss.
2. Continuous timed waves with milestone bosses.
3. Floors of six rooms (3 normal, forge, elite, boss) that repeat with biome, modifier and scaling changes, plus an Extract/Descend decision at each boss checkpoint.

### Decision
Proposed: option 3, implemented after the Day 3 and Day 4 slices, with a single permanent "Forge" meta layer and prestige deferred.

### Why
It reuses the room cadence the GDD already prefers, adds the risk/score tension players ask for without a backend, and keeps one progression vertical.

### Consequences
Positive: repeatable structure from data (`FloorDefinition`, modifiers), measurable Gate A, a natural place for later rewarded-ad hooks.

Negative: two more definition types and a small run state machine before Gate A; balance work for at-risk reward fractions.

### Revisit trigger
Testers report the Extract/Descend choice as confusing or always-descend; floor 2 takes longer than 8 minutes to reach; or wave mode proves more marketable in early clips.

---

## Decision: Local profile save and the first Forge meta layer

**Date:** 2026-09-13  
**Status:** Accepted (implemented; see `21`)  
**Owner:** Product / engineering

### Context
Banked gold had no use and nothing persisted between runs. `15` and `24` §4.8 schedule the Forge meta layer with unlocks, two relics and local save before weapon behaviors. `03` asks for a small permanent layer without large raw-stat multipliers; `06` specifies versioned JSON with a temp-then-replace write and a fallback when corrupted.

### Options
1. A Bootstrap scene with a persistent save service and a separate menu scene for the Forge.
2. Load the profile on every Gameplay scene load; open the Forge as a panel from the result screen.
3. PlayerPrefs for gold and unlock flags.

### Decision
Option 2.
- **Save:** `profile.json` stores `saveVersion` and a `revision`. A save writes a flushed temp file, moves the old profile to a backup and moves the temp file into place. Loading picks the readable file with the highest revision; an unreadable main file is kept, and a newer-build file is never read.
- **Relics:** Second Wind (once per run, heal 25% at 25% health, 80 gold) and Counterweight (strike back for 60% of weapon damage, 150 gold). Each was chosen so the simulated damage-first Temper descent survives while the speed-first Temper descent still fails.
- **Gold:** secured gold is banked at the checkpoint.
- **Test seam:** scene tests redirect the profile folder through a static override. This is the one justified exception to the no-static-state rule in `07`, because tests must never touch a player's save.

### Why
- **No Bootstrap scene yet:** one tiny file read per run is cheaper than a service object that must survive scene reloads, and first launch still drops straight into combat as `04` wants.
- **Forge on the result screen:** the result is where `04` wants the next unlock shown.
- **Not PlayerPrefs:** it has no versioning, no backup and no atomic multi-field write.

### Consequences
Positive: permanent progress with crash-safe writes and tested corruption, interruption and version cases; relics change how builds and forge picks play rather than adding flat power.

Negative:
- After both relics (230 gold) gold has no sink.
- Mid-floor progress beyond secured gold is not saved.
- Tests depend on the override being reset.
- A second hero, weapon unlocks or settings will need more profile fields and the first real migration.

### Revisit trigger
Add a Bootstrap scene when a service must outlive scene loads (audio, analytics, settings). Revisit relic values when testers always pick one relic or never forge. Add a migration test the first time `saveVersion` changes.

---

## Decision: Enemy packs and weapon behaviors

**Date:** 2026-09-13  
**Status:** Accepted (implemented: packs, the Sword's cleave, the Staff, the Daggers and their Forge unlocks; see `21`)  
**Owner:** Product / engineering

### Context
`15` schedules three weapon behaviors after the Forge meta layer. The behaviors in `03` (cleave, pierce, area damage) only differ when several enemies stand together, but every wave held one enemy. The owner chose packs plus three weapons, in two commits:
- Sword cleaves a second enemy.
- Staff deals periodic area damage.
- Daggers strike fast with crits.
- Staff and Daggers are bought with gold in the Forge.

### Options
1. Keep single enemies and add behaviors that work on one target (crits, damage over time).
2. Waves of one to three enemies, with fodder, per-enemy rewards and behaviors that reach several enemies.
3. Continuous spawns with free enemy movement.

### Decision
Option 2.
- **Packs:** one to three enemies per wave, in fixed slots 1.7 units apart. The hero fights the first living enemy in slot order.
- **Fodder:** the Cinder Mite (15 HP, 1 damage every 1.5 s, 1 gold, 3 XP).
- **Rewards:** experience moves from `EconomyConfig` onto each enemy, and each level costs one more than the last.
- **Sword:** each swing also strikes the nearest other living enemy within 2 units for 60% of its damage.
- **Hits carry their attacker:** relics and the defeat cause refer to the enemy that actually hit.

### Why
- **Packs first:** weapon behaviors need several targets to differ.
- **Fixed slots:** a portrait auto-battler stays readable without movement, collision or spawn timing.
- **Cleave and weak fodder:** in a scored simulation search, a single-target Sword against packs died in room 2 on every path. Cleave and 15 HP mites keep the accepted targets: Mend descents survive, Temper descents fail without a relic, and both relics rescue the damage-first Temper descent.
- **Per-enemy experience with growth:** a flat 10 XP per kill would use up the ten upgrade stacks early on floor 1.

### Consequences
Positive: behaviors can matter; the defeat names the real killer; one `DescentSimulation` shared by the EditMode tests keeps the numbers checked.

Negative:
- The HUD bar follows only the targeted enemy; the others show as "+N more".
- A floor is still about 16 s of simulated fighting.
- Direct-hit weapons are weaker against packs, so future single-target weapons need another strength.
- Scene tests describe a wave by what spawned, because cleaves can finish enemies in any order.

### Revisit trigger
Testers cannot tell which enemy is being hit or attacking; a Descent still plays in under 5 minutes; one weapon is always or never chosen once Staff and Daggers exist.

### Weapons update (2026-09-13)
- **Staff:** area damage, 10 every 0.9 s to the target and to every enemy within 3.5 units.
- **Daggers:** 6 every 0.55 s, with every third strike critical for double damage.
- **Crits follow a fixed rhythm, not a chance.** The fight stays readable, the Descent simulation stays exact, and tests need no random seed. Revisit when crit upgrades or a crit chance enter the upgrade pool.
- **Unlocked weapons are sidegrades.** Every weapon must survive both Mend descents with health within 15 of the Sword's, fall on floor 2 on Temper paths without a relic, and be rescued by both relics on damage-first Temper. The Staff must clear mite packs fastest and the Daggers must kill the Wardens fastest. The Staff pays for its reach with the weakest single-target damage (11.1 per second against the Sword's 12.5).
- **The simulation models the 1-second advance delay between waves.** On the phone the first Staff tuning (18 every 1.2 s) ended floor 1 14 HP below its simulation, because a weapon slower than the delay starts each wave still cooling down. With the delay modeled, that tuning died on floor 1, so the Staff was retuned. Weapons slower than the delay pay this cost on every wave; revisit the delay and weapon intervals together.
- **Weapons live in the Relic Forge** under their own header; the owner's choice named that panel. The Sword is the starting weapon: always owned, never saved, and the fallback for any missing or unowned saved weapon.
- **`WeaponShop` is separate from `RelicShop`.** The starting-weapon rule has no relic equivalent; the shared card states became `UnlockStatus`. Extract a common shop when a third unlock category (heroes) arrives.
- **Save version 2** adds `ownedWeaponIds` and `equippedWeaponId`. `ProfileMigration` upgrades version 1 files, covered by a migration test and a version 1 file test, as the revisit trigger of the save decision asked.
- **Prices (Staff 120, Daggers 180)** interleave with the relics, so the result hint names the goals in order: Second Wind (80), Staff (120), Counterweight (150), Daggers (180). They come from pacing, not simulation; revisit with tester run counts.

---

## Decision: Astral foundry art direction and the mockup layout

**Date:** 2026-09-14  
**Status:** Accepted (isometric arena implemented with placeholder visuals: arena floor, walking packs, rebalance, floating platform and framing; the touch-aimed area ability is next; see `21`)  
**Owner:** Product / art

### Context
The owner asked for a look like a reference screenshot: a floating isometric dungeon floor against space, an area attack the player aims by touch, and visible buffs. Following `08` (concepts first, image models for exploration), two portrait mockups came from one prompt. They are in `ArtDirection/2026-09-14/mockups/`, with the prompts and edits in `PROMPTS.md`. The first cosmic render had a skull boss marker and a skull-like golem face; both were removed before the comparison.

### Options
1. **Cosmic:** a floating forge platform in a violet void, the same on every floor.
2. **Furnace cavern:** the same platform inside an underground furnace cavern.
3. **Astral foundry:** the cosmic backdrop, with each floor a forge island whose materials and nebula colors change.

### Decision
Option 3, with `cosmic-v1.png` as the layout and palette target.
- **Arena:** a floating isometric diamond of dark stone and brass in the middle of the screen, enemies spread across it.
- **HUD:** room pips and a boss bar at the top, a row of buff icons with stack badges and one gold counter; hero health and XP at the bottom, pause at the bottom left, a large ability button at the bottom right.
- **Order:** the isometric arena with enemies spread in two dimensions first, then the touch-aimed area ability, then the icon HUD. Placeholder visuals throughout; final art stays behind the gates in `08`.

### Why
- **Readability:** ember-orange enemies and the blue spell ring stand out against the violet void; in the cavern, lava and orange fissures compete with the enemies.
- **Continuity:** option 3 keeps B's forge floors (Ember Halls warm, Quicksilver Vaults cool silver) while adopting the owner's space idea.
- **Originality:** dark stone and brass tiles, a thick floating island and molten enemies stay clear of the reference's flat purple grid and skeleton army.
- **Portrait, one hand:** the ability button sits within the right thumb's reach and the arena stays unobstructed.

### Consequences
Positive: one concrete target for layout, palette and HUD; the area ability gains a spatial decision once enemies spread out.

Negative:
- Spreading enemies in depth needs two-dimensional formations, larger packs and a rebalance; the simulation and the parity tests must follow.
- The icon HUD needs buff details on tap, and mites and health bars are small at phone size.
- The mockups are AI-generated references; nothing from them enters `Assets`.

### Revisit trigger
The placeholder arena fails a readability check on the S23 (enemy size, health bars, damage numbers), testers cannot tell floors apart, or larger packs cost frame rate.

### Arena update (2026-09-14)
- **Enemies walk in.** The owner chose walking over enemies appearing in place: reach starts to matter, and the coming area ability gets moving targets. The cost is a rebalance and a movement model the simulation must share.
- **Positions live on a floor; the screen shows depth at half length.** Halving and doubling are exact in floating point, so distances from world positions equal the simulation's floor distances bit for bit, and the parity tests stay exact.
- **One motion model for scene and simulation.** `PackMotion` is plain C#. Enemies step nearest first, and a step that comes within 0.9 units of a nearer enemy waits. No physics or pathfinding: revisit when rooms get obstacles or enemies need to push.
- **Tempered Edge became +50% damage** instead of +5. That is the same for 10-damage weapons, but a flat bonus added 83% to the Daggers' 6 damage and no Daggers tuning met the sidegrade targets around it.
- **Counterweight now carries speed-first Temper for the Sword and the Staff.** The relic decision above wanted that path to fail. Accepted for now: Counterweight still favours damage upgrades (on the Sword's Mend paths it adds 16 HP to damage first and 5 to speed first), the Daggers still fall on that path, and Second Wind carries it for every weapon. Revisit with the relic values if testers always forge Counterweight.
- **Pacing:** floor 1 takes about 54 s of simulated fighting with the Sword, up from about 16 s, which closes the pacing gap the packs decision left open.
- **Larger packs, less damage each:** up to five enemies per wave on these floors (seven supported), enemy damage at 60%, Cinder Mites worth 1 XP and no gold, and each level costing two more than the last.
- **Placeholder visuals come from code.** One vertex-coloured mesh draws the void, the platform and its keel. Nothing from the mockups enters `Assets`, and final art replaces a single component.
- **The platform is a rhombus stretched along the floor's depth, not a 2:1 diamond.** A 2:1 platform holding every pack slot would be 2.6 times the phone's visible width. The rhombus keeps the mockup's tall silhouette.
- **The camera fits the arena between the HUD blocks** instead of using a fixed size, because the free band differs between 9:16 screens and the phone's 9:19.5.

### Art update (2026-09-14, placeholders drawn from code)
- **Pixel art generated at startup, 32 texels per world unit.** The owner asked the arena to reach the mockup's look before final art exists. Drawing the platform, backdrop, hero, enemies and effects from code keeps `08`'s gates intact (no sprite sheets, no image assets, the mockup stays a reference), is deterministic (hash noise, fixed seeds, exact tests), and gives one pixel grid to every element. The density is the first candidate for `08`'s "choose target sprite resolution" step: the hero is 40 texels tall, a mite 16. Revisit after the phone readability check; the density is one constant.
- **Hit flash by silhouette overlay, not tint.** A white tint on a textured sprite shows nothing; a pre-built white silhouette drawn over the body does, without a shader. Tests read the flash from `CombatantView`.
- **Reach grew so melee stays readable.** Enemies now stop 1.4–1.6 floor units away (0.7–0.8 world units above the hero's feet) instead of 0.9–1.3; the hero weapons grew by the same 0.3. Balance targets were re-checked in the simulation; a bigger step broke one.
- **Scope kept:** the HUD is unchanged, the ability ring exists only as an effect sprite for the Staff, and both floors share one island; per-floor palettes and the icon HUD wait for their slices.

### Ability update (2026-09-14, the Forge Burst)
- **The burst rings the hero, it is not aimed by touch.** The owner chose this over the mockup's aimed ring after seeing the reference game again: the next slice makes the hero movable and the enemies come from every side, so the finger is busy steering and a burst around the hero is the decision "where am I standing". One button at the bottom right, a dashed range ring on the floor, a radial cooldown on the button.
- **Fixed numbers, no upgrades yet:** 20 damage to everything within 2.5 floor units, every 8 s. Fired whenever ready with an enemy in reach, the simulation shows it spares health on every weapon's mending Descent and still lets the greedy Temper Descent fall on floor 2, so it is the player's edge, not the balance. Ability upgrades and a second ability wait for the upgrade pool.
- **The simulation fires it automatically; the scene never does.** Parity tests tap nothing, so they stay exact; balance tests run the simulation with the burst on and off.
- **Next (owner request, same day):** a wider platform in the middle of the screen, a hero that walks by drag, packs entering from all four sides, chests on the floor, and a camera that follows the hero closer.

---

## Decision: The walkable arena

**Date:** 2026-09-14  
**Status:** Accepted (implemented; see `21`)  
**Owner:** Product / engineering

### Context
After seeing the reference game again the owner found the tall diamond too narrow and the fight too static: the art was hardly visible, everything came from one side, and the player had nothing to do with a finger. The request: a wider platform in the middle, the hero walkable, packs from left and right and from all four sides, chests to survive by. Asked how, the owner chose drag-to-walk, a burst around the hero (already built), chests that heal or pay, and a camera that follows closely.

### Options
1. Keep the fixed hero; add the ability and chests as taps.
2. A walkable hero on a wide diamond, packs from every corner, a following camera; the simulation keeps a hero that never moves as the balance baseline.
3. A full free-roam arena with pathfinding, obstacles and a joystick widget.

### Decision
Option 2.
- **Platform:** a 9-unit diamond (`ArenaGeometry(-9, 9, 9)`), the hero at its centre; the same `ArenaGeometry` and `PlatformArt` draw it, with 16 tiles per edge.
- **Packs:** `EntrySides` spreads each wave's slots round the four corners, one corner further per wave, 5 floor units from the hero; `PackMotion` walks each enemy to its own point on an arc round the hero and follows the hero when it moves. Amended the same day after a read-through: packs form up round where the hero stands; an enemy whose spot lies beyond the rim starts short of it if that is still 3 floor units from the hero, and a corner without that room closes for the wave while the open corners take its enemies; an enemy whose point beside the hero hangs over the void turns round the hero to the nearest point on the platform. Packs used to enter round the arena's centre, so a hero near the rim had enemies appear beside or behind it, and enemies could stop over the void. Drawing spots in, rather than only closing corners, keeps packs coming from every side until the hero is most of the way to a corner (seen on the phone).
- **Hero:** `HeroMotion` (2.5 floor units per second, a 0.6-unit margin inside the rim, sliding along the edge) driven by `HeroMovementInput`: a drag anywhere that is not a button, dead zone 24 px, full speed at 110 px, screen up mapped to floor depth so a diagonal drag walks a diagonal on screen.
- **Chests:** one per combat room on a spot that circles the centre; even rooms mend 25%, odd rooms pay 10 gold at the floor's rate; the hero opens one by standing on it.
- **Camera:** `ArenaCameraFollow` shows 4.6 world units across the screen and keeps the hero on the middle row between the HUD blocks, gliding after it.
- **Balance baseline:** the simulation's hero stands still at the centre and fires the burst whenever it is ready with an enemy in reach; chests are the player's edge and are not simulated. With every corner attacking at once, enemy damage was scaled to 0.92 on floor 1 (1.288 on floor 2, keeping its ×1.4) and the Daggers slowed to 0.5 s; every earlier target holds.

### Why
- **Drag anywhere** keeps one-handed play and the whole arena visible; a fixed joystick would cover the platform and tap-to-walk is too slow to dodge.
- **Corners, not edges:** the corners are the screen's left, right, top and bottom of the diamond, which is what the owner asked for, and a pack of four then comes from every side at once.
- **The simulation stays exact** because nothing random or input-driven enters it: parity tests never move the hero, and the scene and the simulation share `PackMotion` and `EntrySides`.

### Consequences
Positive: the fight has decisions; the art is larger on screen; enemies surround the hero as in the reference.

Negative:
- Every figure still has one facing, so enemies from the near corner walk with their backs to the camera and the hero never turns.
- Blocked enemies wait rather than step round each other; a pack from one corner queues.
- The old framing tests and the fixed-camera slot checks were replaced.

### Revisit trigger
Testers cannot steer and fire at once; enemies pile up at one corner; the phone shows the drag start too far from the finger.

---

## Decision: Compact astral-foundry combat UI

**Date:** 2026-09-15

**Status:** Accepted visual direction; runtime integration pending

**Owner:** Product owner / Astra art direction

### Context
The owner approved Astra's recommended combination after reviewing `ArtDirection/2026-09-15/sprint-01/`. Ultracode is executing the separate local-telemetry engineering prompt originally prepared for Opus. Art production proceeds independently; no duplicate engineering task is started.

### Options
1. A: closer combat with stronger character prominence.
2. B: wider combat with more visible travel space and a quieter floor.
3. Combine A's character prominence, B's floor treatment and C's opaque themed two-choice panel.

### Decision
Option 3. Keep the astral foundry, blue-steel hero, ember enemies, silver constructs and restrained brass. Remove the large game title and persistent stat paragraphs from combat in favour of compact floor/room progress, build badges and one gold counter. Group HP/XP below, with one active ability at bottom right and a compact pause target above. Use two equally prominent full-card upgrade targets with live text; no old HUD text may show through their gaps. Use the gauntlet/speed motif for attack speed so a boot does not imply movement speed.

This is a moving-hero combat composition. The older full-island cosmic mockup remains a mood/palette reference; fitting the whole island during ordinary combat is not required. Preserve current character drawings during the first UI integration. Do not copy generated walls or obstructions into ArenaGeometry. Forge Burst remains hero-centred with existing gameplay values.

### Why
The combination gives the hero visual presence, lowers floor competition and makes the upgrade decision readable within thumb reach. Separating icons, panels and live text supports reuse, localization and dynamic values.

### Consequences
Art direction is selected; do not ask the owner to choose A/B again. Prepare reusable UI components, then validate real scene framing and touch layout. The generated images do not establish exact camera values, final pixel grids, additional buff mechanics, map geometry or approved enemy compositions. The proposed 6 and 7.5 widths remain test candidates, not shipped tuning. UI masters stay outside Assets until the integration slice imports and validates them. Do not edit the telemetry worker's files or claim its tests/device run as art verification.

### Revisit trigger
Real phone comparison shows unreadable small enemies, clipped bodies/health bars, insufficient thumb clearance or text that cannot fit on shorter portrait screens. Adjust the affected measurement while retaining the approved visual identity.

---

## Decision: A local event log before any analytics SDK

**Date:** 2026-09-15  
**Status:** Accepted (implemented; see `21`)  
**Owner:** Product owner / engineering

### Context
`10` recommends Firebase Analytics as a baseline, and this log's suggested first decisions include the analytics provider. The owner's rules forbid a backend, SDKs and network transmission at this stage. Meanwhile pacing and choice questions need evidence from real device sessions: floor 1 is about 47 s of active play against the GDD's 3–4 minutes.

### Options
1. Integrate Firebase Analytics now.
2. A local, bounded JSON Lines log beside the profile, pulled with adb after a session; no SDK.
3. Keep relying on screenshots and logcat.

### Decision
Option 2. Use `10`'s event names wherever the feature exists. Keep active combat time apart from choice, pause and background time. Identify a launch by a random session id; record no device or player identifiers.

### Why
- A file that never leaves the phone needs no SDK, consent flow or data-safety declaration.
- It answers device-session questions now: how long rooms take, what gets picked, why runs end. The event names carry over to a provider later.
- Screenshots and logcat cannot give exact counts, ordering or time splits.

### Consequences
Positive: exact per-run evidence; ordering and exactly-once rules under test; no binary or privacy cost.

Negative:
- Nothing aggregates across players or devices, and logs are pulled by hand.
- A provider later still needs consent, a data-safety declaration and a sink behind the same recorder.

### Revisit trigger
External playtests beyond a handful of devices, or a soft-launch plan: then choose a provider and route the recorder's events to it.

---

## Decision: Integrate the approved foundry UI and start at a 6-unit camera width

**Date:** 2026-09-16  
**Status:** Implemented under the owner's approved art direction  
**Owner:** Product owner / art

### Context and options
The owner approved A's character prominence, B's quiet floor and C's card hierarchy, then authorised integration once the telemetry worker finished. The old 4.6-unit camera clipped a guardian beside the hero. Widths 6 and 7.5 were candidates; retaining 4.6 did not provide adequate body margins.

### Decision and evidence
Use 6 initially. It retains more character prominence than 7.5 and fits the full guardian beside the hero in the S23 capture `TestResults/device-art-ui-01/review-09.png`. Both candidate widths also pass analytical short/tall portrait body/bar bounds with a horizontal follow-lag allowance. This is not a paired phone comparison; do not report 7.5 as device-tested.

Import the reusable panel and seven icons/bezel images, keep live text, and retain procedural actors. Use local floor colours instead of changing the shared palette. Move the detailed readout to Pause rather than deleting useful combat/build information. Keep gameplay and telemetry untouched.

### Consequences and remaining work
The first integrated UI is playable, but generated raster UI is not a replacement for a grid-cleaned character pipeline. Result/forge screens and the pause menu's large title/resume control still used earlier styling at this decision. Result/forge styling was subsequently integrated in `df0646a`. Individual badge tap explanations remain open; the full readout is available via Pause. Revisit camera width after movement/roster changes or clipping on another screen ratio, and test shorter UI layouts manually before release.

---

## Decision: Retain portrait and widen the movement view before increasing density

**Date:** 2026-09-16  
**Status:** Camera implementation and validation; dense combat remains future work  
**Owner:** Product owner / art

The owner approved continuing in portrait after discussing landscape, and supplied the furnace-cavern image as the desired boss-arena look. Normal rooms should have more frequent packs and practical kiting space. This replaces the previous cosmic-only environment preference for the next two-room art target, while retaining original foundry characters and combat-first contrast. See docs/03 and 08 and `ArtDirection/2026-09-16/arena-target/`.

Landscape would provide more lateral space but would also change the one-thumb control/layout target. Keep portrait and test its framing before changing orientation. Compare widths 6, 7.5 and 9 with identical world-scale actors: choose 9 for the next movement pass. At 1080 px width, a 44-texel hero canvas is 165 px tall and a 16-texel mite is 60 px tall. Width 9 shows 50% more horizontal world span than 6. The six Unity render comparisons are staged at 1080x1920/2340 with simulated safe insets, not actual dense combat or proof of phone performance.

The platform remains 18 units wide, so the full boss-island reference needs its own later geometry/framing decision. Do not enlarge actor world scale to compensate for zoom or alter pursuit/weapon ranges as part of this camera slice. Maximum live pack size remains seven. The ten-enemy movement/simulation task is prepared for Opus and was confirmed not started; it is not fulfilled by the staged render.

Verification and phone evidence belong to the latest docs/21–23 sections. Revisit width 9 after the ten-enemy movement pass if the hero/mite is too small on a physical screen, spawn threats arrive without warning, or required boss margins fail. The painted reference requires new modular environment and animation assets; this change alone does not deliver that final art.

---

## Decision: Prove ten-enemy density on a development-only floor, and keep entries clear of the rim

**Date:** 2026-09-16
**Status:** Implemented and measured on the S23
**Owner:** Engineering, under the owner's brief in `ArtDirection/2026-09-16/arena-target/OPUS_KITING_BRIEF.md`

### Context and options

The camera was widened to 9 for movement, and the next question was whether a normal room can hold ten enemies and what walking away from them is worth. Three ways to reach a ten-enemy encounter were available: raise an authored wave in `Data/Floors`, add a room to Ember Halls, or put the encounter somewhere only development builds can reach. The first two change the balance numbers that `22` pins and would have to be undone; they also mean editing content another assistant owns.

### Decision and evidence

Add `Resources/Development/Floor_DensityProof.asset` and let a development build or a scene test start the run there through a one-line file in the profile folder. No authored floor changed, every existing balance number stands untouched, and the same code path serves the six PlayMode parity cases and the phone. A release build returns the authored floor before touching the file system, and a PlayMode case proves that without the file the scene starts on Ember Halls. On the device, deleting the file put the app back on Ember Hall 1/6.

Capacity was raised from seven to ten, and the spawn now keeps a body's spacing. A sweep over every hero spot on the platform shows the spacing moves nothing for a pack of six or fewer - which every authored wave is - so no encounter a player can reach today enters anywhere but where it always did.

A second, smaller decision came out of running the suites on two runtimes: an entry pulled in short of the rim used to land exactly on the rim line, and the Editor's Mono and the pure .NET runner disagreed by one bit about which side of it the spot was on. Rather than teach the tests to accept either answer, the pull-in now stops a ten-thousandth of the way short of the rim, which moves an entry by at most a thousandth of a unit and makes the guarantee true on every runtime. The alternative - a tolerance in the assertion - would have left the scene, the simulation and the device free to disagree quietly, which is the one thing this project's method cannot afford.

### Consequences and remaining work

Ten enemies fighting cost nothing measurable on an S23: 60 fps, no frame reaching 20 ms. The cost is the single frame the wave enters on, and it is allocation, not work: 18 MB of managed garbage per wave from per-enemy sprite generation. A sprite cache is the remedy and belongs to the art owner; until it exists, a wave of ten drops about three frames on entry, and a mid-range device has not been measured at all.

Nothing here raises the capacity of a floor a player can reach. Whether normal rooms should hold more enemies, and what kiting should cost once they do, is a balance decision that now has numbers to argue from: kiting cuts the damage taken by 2.2x to 4.0x depending on the weapon, and the Staff kites the authored Descent almost unharmed. Review that before increasing any authored wave.

---

## Decision: Share immutable enemy sprites for one application session

**Date:** 2026-09-16
**Owner:** Art implementation, following the ten-enemy handoff

Per-enemy ownership redraws identical canvases and allocates identical native textures for every spawn. A scene-owned cache would remove duplicate enemies within a run but still redraw on Try again. Imported sprite sheets would also avoid runtime drawing, but would mix a production-art replacement into this performance slice. Choose a lazy application-session cache keyed by `EnemyLook`, preserving the exact current drawers, names, pivots, pixel density and filter settings.

`EnemySpriteCache` is the explicitly justified exception to the no-mutable-static-state guideline: it owns only a bounded set of presentation resources, never combat, progression, profile or telemetry state. Six looks, three frames and three flash silhouettes each, plus two bars shared by every look, cap the cache at 38 sprites/textures. Private frame arrays prevent borrowers from replacing the sequence. Views own animation clocks, renderers, tint and bar scale, and must never destroy borrowed sprites. Cache resources survive scene reload and unused-asset collection; application quit releases both sprites and textures, and subsystem registration clears stale state when entering Play with domain reload disabled. Native lifetime and reset are exercised in PlayMode; test-only assembly access keeps reset off the public gameplay API.

The preceding 18 MB observation is **whole-frame allocation**, not an isolated measurement of enemy drawing. That frame also rebuilds arena/backdrop/other scene art. Compare the `StartWave` marker separately from total frame allocation and restart time; do not promise this cache removes the entire 18 MB. Arena and backdrop ownership remain a separate follow-up, measured after this bounded change. Verification and device results are recorded in the following docs/21–23 entries.

---

## Decision: Retain the current environment's generated textures across restarts

**Date:** 2026-09-16
**Owner:** Art implementation

After enemy sharing, the S23 still allocated approximately 17.6–17.7 thousand KB during scene reload. A separate Editor diagnostic built the platform and backdrop twice: the repeated uncached calls took 18.08 ms and 27.51 ms, respectively (82 and 1033 managed allocation events). These are CPU-side Editor measurements, not phone timings or byte counts. `GC.GetAllocatedBytesForCurrentThread` returned zero on this Editor's Mono and was rejected as a measurement source; the replacement counts `GC.Alloc` events using Unity's [current-thread ProfilerRecorder API](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Unity.Profiling.ProfilerRecorderOptions.CollectOnlyOnCurrentThread.html). Device frame allocation remains measured by the existing recorder.

Options were to preserve whole scene objects with `DontDestroyOnLoad`, share only immutable textures, or replace the environment with imported art immediately. Share textures: persistent scene objects would also carry animation, transform and material state through runs; imported art would combine a visual replacement with a performance change. `ArenaSpriteCache` is a bounded presentation-only static owner, using the same explicit lifetime justification as the enemy cache. It retains the first platform geometry's three sprites and one fixed `VoidLayout` sprite set. All three geometry coordinates are checked; other dimensions get view-owned art, released on view destruction. This avoids a growing geometry dictionary and cannot silently display the wrong platform. A future authored normal/boss room catalog should give its art explicit bounded ownership rather than broadening this fallback implicitly.

Each view still owns its renderers, transforms, animation clocks and the small sky mesh. The cache keeps island attachment positions but releases the CPU drawing canvases after creation. Subsystem registration and application quit release every retained sprite and texture, including hidden animation frames. The fixed backdrop was already independent of the geometry argument; this slice does not introduce room-aware backdrop placement. Platform and backdrop build markers let the device probe separate their elapsed times from the wave spawn and whole-frame allocation. Initial application startup still precedes probe installation; warm scene reload is the comparable phone case.

The tested fixed set contains 37 RGBA32 textures, 8,365,956 texel bytes (7.98 MiB, excluding native overhead). This memory remains resident for the session instead of being discarded between runs. Three warm S23 restarts reduced whole-frame allocation to 1008.5–1131.4 KB; this tradeoff is accepted for the current single environment. Tests, device caveats and the installed build hash are recorded in `21`–`23`.

---

## Decision: Room-bound follow and one layered furnace background

**Date:** 2026-09-16
**Owner:** Art implementation, following the owner's approved furnace direction

Unbounded follow centres the hero even at the far rim, devoting much of the useful portrait band to scenery. Zooming out enough to fit the entire 18-unit platform would shrink the current actors; a hard camera clamp without protecting the hero would cut off body/bar space near side corners and on short screens. Keep width 9 and smooth follow, first constrain the free band's centre to projected arena bounds, then relax that bound as needed to reserve nearby-threat margins around the hero. At tested sizes those margins are 2.6 units sideways, 3.1 above and 1.4 below. This deliberately permits some background at the edge; it is not a promise to keep distant spawns onscreen. A different arena transform/geometry supplies its own coordinates. Boss-specific room dimensions remain separate work.

The owner's later furnace-cavern approval superseded the original cosmic-only choice, but its low-contrast combat hierarchy remains. Use one original generated background plate, separate from the walkable platform and every actor/UI/effect. Replacing the whole world with a flattened mockup would break movement and occlusion; building a complete environment kit before seeing one layer in Unity would delay the phone check. The first plate has muted indigo rock and sparse distant amber furnaces, with no foreground arena or painted gameplay elements. Existing floor/rim/lighting are retained for a bounded first composition proof.

The painted distant layer uses bilinear filtering and a 64-PPU import (aspect-preserving cover at runtime), while actors/floor retain point-filtered 32-PPU art. This explicitly scoped exception avoids pretending the source has strict pixel-grid discipline. Android ASTC 6x6, no mipmaps or CPU readability, and bounded parallax keep it small. Asset ownership replaces procedural backdrop generation in the authored scene; the old cache remains a fallback when the imported sprite is absent. Prototype actors and stone still need a later material/style pass. Source, exact prompt, review renders and scope are in `ArtDirection/2026-09-16/furnace-layer-01/`; no asset was derived from a third-party game image.

---

## Decision: Enemies keep their spacing from every neighbour, and slide or wait rather than overlap

**Date:** 2026-09-16
**Status:** Implemented and phone-checked on the S23
**Owner:** Engineering, under the owner's spacing brief

### Context and options

With ten enemies and a walking hero the pack overlapped badly (closest gap 0.072 against a 0.9 spacing), because only enemies nearer the hero could hold one back. Three shapes of fix were weighed. Pushing overlapping enemies apart after they move is simple, but it moves enemies that have already arrived, pushes them out of reach and back, and is the classic source of crowd jitter. Steering forces (separation added to the walk) are smooth in open space and oscillate in a crowd. A constraint on the step itself - never end a step inside a neighbour's spacing, slide along it instead, wait when no slide helps - moves nothing that is standing, keeps the invariant exactly by construction, and costs nothing when no neighbour is near.

### Decision and evidence

Constrain the step, with no memory between steps. Every enemy's step is refused if it ends inside any living neighbour's spacing, unless it widens a gap already too small; a refused step slides along every neighbour it could touch and is taken only if it brings the enemy strictly nearer its own point and keeps it on the platform; long frames are sub-stepped. Measured in the Editor: closest gap 0.900 in every proof run (before 0.072–0.704), flickers 12 → 0, longest wait while the hero walks 6.37 → 2.02 s, gold unchanged, and the standing authored Descent identical, because its packs never come within two strides of a body.

Three intermediate versions were measured and dropped. Sliding round only the nearest blocker made an enemy zigzag down the gap between two neighbours, glancing off each in turn. Sliding against every near neighbour even when the straight step was free made standing packs wait longer (longest wait 5.35 → 9.45 s in the .NET measurement) and brought flickers back (14 against 1). Remembering the side an enemy slid round, to stop it switching sides, could freeze an enemy for good with the hero standing still - an adversarial review found the positions - while buying no fewer flickers. None of them is in the code; the frozen case is a test.

### Consequences and remaining work

A standing hero now takes less damage from a dense pack, since only as many enemies as fit a body apart can reach it; this is visible only on the development proof floor, because no authored wave crowds. Enemies still stop at their reach and the hero has no body. Crowded fights end differently on the Editor's Mono and the .NET runner, as they already did before this rule, so exact crowded outcomes are recorded per runtime. Revisit if authored waves grow beyond five, if a hero body is added (the same step constraint is the natural place for it), or if the phone shows enemies freezing in a crowd.

## 2026-09-17 — Import one animated Vanguard without changing combat timing

**Options:** retain the procedural hero until a complete directional sheet exists; replace it with a rigid painted cutout; integrate a bounded eight-frame body/equipment set and explicitly keep directional/foot-contact polish open.

**Decision:** the third option makes the accepted art direction playable and exposes remaining scale/attachment/motion issues on the phone. Only Vanguard adopts 128 PPU/bilinear; the common crop/pivot preserves world scale and avoids per-frame auto-fit breathing. Runtime sprites are imported shared assets rather than generated per frame. Existing procedural art remains the fallback and supplies Staff/Daggers.

Movement frames observe actual floor displacement; anticipation reads cooldown/in-range targeting, and strike begins on the real post-damage event. Animation cannot change cadence, damage or movement. No animation event becomes a second source of gameplay truth. Full simulation parity remains part of the gate. This does not approve a final walk cycle, directional roster or matching enemy set. Details and limitations: `ArtDirection/2026-09-17/vanguard-runtime-01/README.md`.

## 2026-09-17 — Four diagonal Vanguard facings before expanding the roster

**Options:** keep the single rear facing; paint eight complete directions immediately; add one coherent front set and mirror each side for four diagonal facings.

**Decision:** use the third option to make movement direction visible within a bounded sixteen-pose set. Moving heroes face actual travel; stationary attacks face their actual hit target, including lethal hits. Each facing axis retains its previous value near cardinal motion, and strike/recovery hold facing. Only the body/equipment/flash subtree mirrors; physical hero state and shadow remain untouched. Targeting and damage stay authoritative in the existing combat controller.

**Tradeoffs:** mirroring reverses apparent handedness and light direction. It is a prototype compromise, not approval of final character animation. Fixed front/rear registration, corrected rear passing soles and directional sword/buckler layering improve readability, but individual finger occlusion and a strictly planted gait still need work. Front Staff/Daggers retain the neutral body attack and current procedural equipment. Evidence and reproduction: `ArtDirection/2026-09-17/vanguard-directions-01/README.md`.

## 2026-09-18 — Shared chest art and grounded presentation sorting

**Options:** retain the procedural prop, shrink it to hide overlap, or import the accepted three-state foundry chest and sort whole actors/props at their ground roots. Choose the third: .75-unit reference box width preserves the ember cue, while shrinkage does not fix incorrect front/behind drawing.

ChestArtSet holds three fixed-canvas/pivot imported sprites. Contact still awards gold/healing immediately and exactly once; the .18-second opening frame is cosmetic and pauses with scaled time. No new interaction, rarity, balance, reward, body collider or telemetry rule is introduced.

GroundedSorting groups bodies/equipment/flashes at actor roots. Health bars, contact shadows and the range ring use independent root groups so their existing global overlay/ground orders survive. This changes presentation of crowded overlaps but not physical spacing. A correctly nearer Grunt can still hide the chest. Source consistency, smooth lid in-betweens, chest shadow and phone readability remain separate acceptance limits. See chest-runtime-01 and Docs/21–23 for final verification status.

## 2026-09-19 — Separate movement from health in the bottom HUD

Chose a labelled, inset MOVE row and quieter full-state alpha over raising the brightness of the entire panel or enlarging the existing health-adjacent strip. Health retains priority; the movement budget has its own location and identity. Depletion is a single 0.24-second ember track pulse, then a subdued spent track, with exact fill and scaled-time pause behaviour. No flashing loop, balance retuning or new generated image. Evidence and reproduction: ArtDirection/2026-09-19/movement-hud-01/. Physical-device readability remains pending coordination.

## 2026-09-19 — Ability range marker, visual proof decision

Prefer a strengthened eight-arc contour with small inward lozenges over many short pixel dashes or a fully continuous line. Initial thin candidate was too faint; keep its rejected capture. Exact radius and a clear centre matter more than an ornate filled magic circle. Extend code-native geometry rather than generate an approximate bitmap. This is a proof preference, not runtime acceptance. No targeting, cooldown or cast effects changed. Evidence and remaining integration work: ArtDirection/2026-09-19/ability-ring-01/README.md.

## 2026-09-19 — One shared unit-radius ability contour

Integrate the selected eight-arc marker as deterministic code-native geometry. One unit-radius sprite plus per-view scaling has bounded memory independent of ability radii and avoids generation on scene reload. Keep current cast/cooldown semantics and independent ground sorting. Test the contour radius rather than transparent texture bounds. The first-session texture is RGBA32/non-readable with 0.5MiB raw GPU payload; this is not a compressed imported sprite or a frame-time claim. Runtime evidence: ArtDirection/2026-09-19/ability-ring-runtime-01/.

## 2026-09-19 — Reuse the range contour for the cast pulse

A short expansion/fade of the shared arc sprite replaces the blocky ability burst. Chose this over a new sprite sheet because it matches the accepted visual language exactly, avoids extra texture memory and allows smooth timing. Keep damage immediate, origin fixed, total lifetime0.21s and existing pool. Ground sorting prevents the line crossing over bodies. This is feedback for an instant attack, not a travelling damage wave. Staff splash is separate scope.

## 2026-09-19 — Separate Staff impact from the ability seal (proof)

Options: reuse the blue eight-arc ability artwork, retain the thick pixel blast, or create four tapered violet-white arcs. Prefer four arcs: distinguish the frequent target-centred weapon impact by geometry and colour, keep silhouettes clear through ground sorting and retain the old0.21s cosmetic duration. The small impact is not a full damage-range telegraph. Ten actual Unity proof captures are in ArtDirection/2026-09-19/staff-splash-01; no authored runtime or phone change. Next integration should use one shared contour and the existing pool; temporary seven-texture proof is not a production ownership design.

## 2026-09-19 — Integrate Staff arcs with shared lifetime

User explicitly requested installation of the Staff proof. Chose one cached immutable contour plus pooled scale/fade over seven generated frame textures, retaining the proof's visual identity and0.21s cosmetic duration. Keep the old impact-only size fraction and clamp; this is not an area telegraph and does not change damage reach. Ground sorting improves silhouette readability. StaffSplashTests cover real attack and ownership; full gate/device status in newest21–23 and staff-runtime-01/README.md.

## 2026-09-19 — One-piece painted Staff equipment

Choose the original whole Staff sprite over splitting the generated crystal and shaft. A single rigid sprite preserves registration through the existing cast lean and avoids a second visible orb. The shared optional VanguardArtSet reference preserves fallback behaviour; front knuckle overlay and rear depth follow the existing body poses. The tradeoff is that the old procedural orb's independent lit-sprite change is no longer visible; the painted crystal and already-integrated impact effect carry the violet identity. No balance or attack timing changes. Fixed source sampling and verification are recorded in ArtDirection/2026-09-19/staff-weapon-runtime-01/README.md.

## 2026-09-19 — Imported Daggers retain neutral-hand contact

The real-scene proof showed the previous weapon-only upward translation separates the new hilt from the neutral hand. For imported Daggers choose a short35-to47-degree outward wrist pivot about each registered hand, with the blades behind the body/gauntlets. Keep the existing strike event/duration, all combat rules and the legacy translation for sets without imported Daggers. This fixes contact without fabricating a moving-arm pose. Full arm animation and replacing the large procedural hit effect remain separate art work. Source/sampling and actual runtime captures: ArtDirection/2026-09-19/daggers-runtime-01.

## 2026-09-22 — One visible object for a combined ground reward

Considered separate XP/coin objects, permanent pickup halos, and one compact painted cluster. Choose the cluster: GroundDrops pays both amounts together, and a cyan crystal plus round brass coins communicates both without inventing two pickup targets. Keep the visible width around .45 units, smaller than the unchanged one-unit reach; use a quiet separate contact shadow instead of an always-on glow. Single-currency exceptions retain honest simple fallback symbols.

The presentation subscribes to existing value events and pools renderers; it neither pays rewards nor moves them in the gameplay model. Collection flight is cosmetic and freezes with game time, including an upgrade choice opening on the payment frame. Distant collection uses a longer visual flight without adding a sweep flag or delaying a checkpoint. Old Descent death coins are suppressed; instant Swarm/chest feedback stays. The price of correct floor sorting is that nearby actors can occlude a drop. No rule, balance, new magnet radius, audio or exit system is added. Evidence and final verification status: ArtDirection/2026-09-22/reward-runtime-01/README.md.

## Region route direction approved — 2026-09-23

Owner approved the first region sequence: two combat areas, an exclusive elite OR chest branch, forge, main boss hall. Post-boss decision explicitly approved: extract with rewards OR enter optional finale swarm. Existing impressive arena is the boss-hall visual target; ordinary areas need separate kiting-friendly layouts. Implement one region before expanding to multiple worlds. This is approved direction, not shipped progression: FloorDefinition/FloorProgress remain linear. ArtDirection/2026-09-23/region-map-01/DESIGN_AND_HANDOFF.md records remaining route model, choice arbitration, dedicated chest reward ownership, simulation/parity, telemetry and economy decisions. Browser paths and both boss decisions passed interaction checks; no numeric elite/chest reward tradeoff is implied.

## Superseding region direction and first traversal proof — 2026-09-23

Owner approved a fixed explorable multi-screen isometric region, replacing the small node-selected route. Encounters/chests/forge/elite are physical destinations; boss hall remains separate and optional final swarm direction is preserved. Read ArtDirection/2026-09-23/exploration-proof-01/DIRECTION.md. Old region-map-01/OPUS_TASK.md is explicitly superseded; Opus has not started according to last reply. No boss unlock/key/reward policy invented.

Temporary Unity scene copy enlarged convex arena6x area at unchanged hero/camera scale. Real movement/stamina legs measured6.78/18.53/18.55 game seconds; graphics1 and integrity485/568 passed. Initial unlisted-scene load failed, corrected with EditorSceneManager without build-profile edits. No production Assets/gameplay/settings/APK/device change. Current floor material stretches visibly: next art work needs stable-scale tiled ground. Encounter activation and exploration cadence remain unimplemented; no combat balance or device-performance claim.

## 2026-09-23 — Development region entry and scenery ownership

Decision: keep the authored hall as default and expose the wide traversal preview through a separate development/region-preview.txt flag (wide). Floor selection remains independent. ArenaView resolves geometry once before the first consumer; release short-circuits IO. This avoids silently changing all authored combat geometry while the physical encounter-site design is still pending. FoundrySceneryView borrows furnace/floor assets and owns its sole generated foundation mesh. No per-frame work, new reward, collision, stamina or progression rules.

The ordinary boss hall is not replaced. Current linear rooms still run in the wide preview; do not call that local exploration. Next mechanics work is authored encounter activation and boss approach, coordinated separately from art. Temporary art captures disable attacks only inside the fixture, not through the development flag.

## 2026-09-23 — Art-first foundry region layout

Owner explicitly deferred Opus and approved shaping the map first. New ArtDirection/2026-09-23/foundry-layout-01 contains an interactive top-down/isometric surface proposal, fixed-scale schematic portrait camera and one untouched built-in image_gen concept. Five identities: entry, large casting square, two-connected salvage court, broad furnace passage and separate boss approach. Routes are flat; concave outline and side hole are proposed only, not current ArenaGeometry.

Browser verified at1280 and390 widths: five selectors, isometric pointer-to-floor conversion, images, no overflow or script errors. Desktop screenshot inspected. Exact image prompt in PROMPT.md; source SHA25698F7D14F6B3E3634A2DCA3D8AABF3910B5883C608244CE4E4868B6862F5C5C5D. Painted concept includes a few entrance steps and differs in exact geometry; those are not approved elevation mechanics. Diagram is placement authority, concept is atmosphere, neither is runtime collision. Design-space isometric coordinates require explicit conversion; schematic camera is not the runtime9-unit framing.

No Assets, runtime, APK, device or tests changed in this concept slice. Current installed C8 build and uninstalled furnace-clearance change remain as recorded. Opus handoff marked ON HOLD, not sent or started. Next: review region shape then isolated traversability/greybox and modular art; concave geometry cannot be installed as only a painted floor.

## 2026-09-23 — Foundry art layout proof

Approved concept progressed to an isolated Unity surface:952 connected cells, five fixed-scale portrait samples. No concave gameplay integration yet. Gate architecture generated as separate source, but both attempts retain alpha haze and are not production-ready. Read foundry-unity-01 and boss-gate-01 READMEs. Graphics1, integrity489/568 and browser checks passed. No APK/device operation; Opus stays on hold. Next: clean gate and supported environment modules.

## 2026-09-23 — Perimeter art follow-up

Keep furnace supports decorative outside the952-cell proposal; no silent extension of navigation. Static Unity study now has two annexes and six buttresses, graphics/browser/integrity verified. Floor/annex joins remain unfinished. Third gate cleanup rejected; no imported gate. See foundry-perimeter-01. Opus stays on hold, phone unchanged.

## 2026-09-23 — Playable map before further isolated art

Owner requested a coherent playable deliverable. Implemented shared concave geometry, routing and environment in development foundry mode; installed/hash verified7FB249A4. Existing Descent remains the progression authority. Keep foundry flag on for owner feedback. Local physical encounter activation/separate boss hall are the next gameplay slice, not claimed complete. Read foundry-playable-01. Gate art cleanup remains deferred.

## 2026-09-23 — Rough complete map before final environment art

Owner approved completing the rough map before refinement toward the foundry concept. Added replaceable entry, salvage, furnace and gate architectural masses plus flat route/square inlays in FoundryRegionView. Raised footprints remain outside shared traversal geometry; no new obstacles, elevations, balance or progression. Real Unity five-zone walk passed; full Android gate/device result follows in verification docs. Large blocks are deliberately provisional, not final visual acceptance. Opus remains on hold.

## 2026-09-23 � Owner correction: photograph owns the structure

Owner rejected the narrow linked-platform interpretation and explicitly requested the same structure as foundry-v1.png. This supersedes the earlier diagram-as-authority instruction for the foundry region. Current runtime now uses one broad court with an inset shaft on the left, a straight south entry bridge and a wide northeast gate apron. The concept remains the visual target; current rough masses are not final art or an exact reproduction of every painted detail.

FoundryMap now combines five axis-aligned rectangles with one exact diagonal bridge (u-v in[-4,4],u+v in[-50,-24]). Main court bounds[-18,-14]..[14,18], shaft[-12,2]..[-4,10], apron[12,-2]..[32,12]. Bridge predicates and continuous segment clipping share the rendered straight bridge. FoundryPursuit nodes moved with these regions. The old952-cell/seven-rectangle layout and old waypoints are historical.

FoundryRegionView adds deep sidewalls and regularly spaced exterior supports, low edge parapets, a central three-ring brass seal, perimeter furnace plinths, a simple crane silhouette and a northeast closed gate. Static faces batch by shared material/sorting layer rather than adding a renderer for every support. Mesh ownership remains local, materials borrowed, no per-frame geometry. The four furnace sprites are existing imported art. Decorative silhouettes still need final art, registered joints, lighting and occlusion polish. No elevated walking or functioning gate transition was added.

Verification: final pure suite388 passed, compile clean, graphics walk1 passed, integrity494GUIDs/568scene objects. Capture walkthrough legs29.4/30.6/18.2/16.8/11.7game seconds, attacks disabled in fixture. Refreshed overview.png uses a temporary overview camera; actual portrait camera stays width9. Android gate/install follow separately. Opus remains on hold; no commit/push.

## 2026-09-23 — Gate texture instead of hazy cutout

Use an opaque front-elevation facade texture on the existing vertical gate mesh. Previous generated cutouts had atmospheric alpha contamination; a rectangle of physical masonry/doors needs no cutout mask. Reference/style and exact prompt preserved in gate-facade-01. Walkable geometry, progression and camera unchanged. Supporting masonry height adapted to facade while footprints remain beyond the walking boundary. This is a production-imported first art pass, not final environment acceptance.


## 2026-09-23 - Foundry furnace and masonry integration

Four detailed furnace elevations now wrap the existing scenery footprints, with exposed chimney geometry. Foundry-only masonry replaces the earlier slit-pattern walls on parapets, deep supports and architectural sides; ordinary hall materials remain unchanged. World-continuous wall UVs and painter-ordered chimney caps address cell seams and roof overlap. All art is borrowed from imported assets; static meshes are still batched and owned. Geometry, camera, combat and progression unchanged.

Sources, exact built-in image_gen prompts, import settings and actual Unity captures: ArtDirection/2026-09-23/foundry-surfaces-01/README.md. No manual raster cleanup. Final silhouette/pipework/crane/shaft dressing remains unfinished. Compile clean, graphics traversal1, static integrity500/568; full gate and device result recorded separately in docs22/23.


## 2026-09-23 - Complete foundry dressing package

Owner requested the remaining environment dressing in one integrated update. FoundryRegionDressing adds braced crane, pulleys, hanging salvage basket, pipe runs/collars/valves/supports, heated panelled shaft lining and molten basin, iron roofs, inner guardrails, beacons and suspension chains. Shared map geometry, camera, encounters and economy unchanged. Two unchanged built-in image_gen material masters imported as shared non-readable ASTC6/max1024 assets. Static code-native meshes give these materials the needed shapes; no new Update, texture generation or light system.

Review/source/prompt contract: ArtDirection/2026-09-23/foundry-complete-01/README.md. Actual integrated five-position Unity traversal and close-up render proof passed; scene uses15 static renderer groups. Crane attachment and shaft panel aspect corrected after close review. Scenery rank allocation keeps background/floor/contact shadows/actors consistent. Full gate and final phone state follow in docs22/23. Named dressing scope complete; no functional crane/lava hazard/boss transport is implied.


## 2026-09-23 - Movement energy deferred by owner

The owner found energy depletion disruptive on the large map and requested its temporary removal. Current playable rules now allow continuous movement at the existing configured speed: no energy spending or empty-bar speed penalty. The MOVE label/bar are hidden, and StaminaBar upgrades (Deep Lungs) are excluded from both level offers and chest books. Damage, hero base speed, enemy speed and all other progression rules remain unchanged. This applies to the current game, not only the foundry preview.

HeroStamina.CurrentRulesEnabled is the shared current-rule switch; HeroMovementInput and the default DescentSimulation use it. Explicit budget instances and historical experiments remain available. UpgradeService receives the same enabled state without reordering authored pool indices. No save format or permanent profile change. Re-enabling is a future design decision, not an automatic milestone: first assess large-map traversal and alternative enemy pressure; repeat scene/simulation/device balance review if energy returns. Earlier movement-budget recommendations in doc26 are historical while this decision is active.


## 2026-10-07 - Paired forge-keeper bridge landmark

Owner requested several visual tasks completed together while the phone is available. Integrate the original forge-keeper concept as two identically oriented, alpha-cutout architecture cards, with stone consoles/plinths and repositioned bridge banners. Their centres move outboard and forward to keep the full plinth footprints outside the existing FoundryMap walking union. The below-deck console reaches the rim, but adds no collision or walkable area. Replace head braziers and remove their baked floor-light sources; preserve rail lanterns and foot beacons.

Accept one additional shared material/renderer for both statues: region budget18, with other geometry still constrained to17 and keeper batch constrained to1. The texture uses the existing actor-depth/hero-cutaway shader path through a scene-owned material variant. No new render loop, shader, gameplay or camera system. Preserve source lighting orientation on both sides. This is projected 2.5D architecture, not a claim of new 3D models or completed reference fidelity. See ArtDirection/2026-10-07/guardian-runtime-01 for actual renders and final verification status.

## 2026-10-08 - Shared diagonal court boundary

Decision: cut the two entrance-facing corners by6 units in the shared FoundryMap, then clip the actual floor triangles and move all perimeter architecture to that boundary. Keep the bridge and gate-room union independent so the diagonal entrance remains fully connected. This corrects composition with matching collision/spawn/pursuit behavior instead of decorative masking. Gate shoulders may rise outside the lane; actual walkable elevation remains deferred until actor projection, depth and traversal can be implemented together. No additional renderer or texture. Proof: ArtDirection/2026-10-08/court-silhouette-01.

## 2026-10-08 - Complete north silhouette and consolidate workshop

Decision: use four-unit north corner cuts, group the existing stock station near the crane, and reduce geometric flagstone size in one package. Repeating the six-unit southern cuts would unnecessarily narrow the pit/workshop loop. Keeping stock isolated along the north rail weakens the industrial grouping; moving all its support/depth/clutter/soot together avoids floating remnants. Geometric stone sizes are the appropriate control because the current texture is continuous stone rather than a painted tile layout. Narrower, softer joints counter the increased paving density.

Accepted cost: +2,871 region vertices (about3.2%), unchanged18 region renderers, no new materials/textures/update loops. Boundary change is shared by rendering, movement, pursuit and pack placement, with new north-corner cases and actual floor coverage assertions. Automated gate and guarded S23 traversal passed; see north-workshop-paving-01 and latest doc23. Gate elevation is still a separate gameplay/art integration, not implied by decorative shoulders.

## 2026-10-08 - A flush processional panel before height integration

Decision: complete the missing broad gate-approach motif using existing stone/brass batches: a long chamfered frame, smaller lane-oriented medallion and dressed threshold. Its artwork is static and non-emissive so it remains distinct from combat telegraphs. No new material/texture/renderer; +1,776vertices. Actual graphics comparison: gate-procession-01.

Real walkable elevation cannot be delivered honestly as a stair picture while actors remain on the original plane. It needs consistent projection of actors, bars, contact shadows, pooled effects, floor pulses, drops/chest and camera, plus depth/cutaway verification. That remains separate work. This panel is intentionally flush and preserves the current shared movement map, unlike a decorative raised surface intruding into the lane. Prefer this visible reference motif over another minor global contrast change.

## 2026-10-08 — Raise the gate without changing combat coordinates

Decision: use one continuous presentation-height function for the final gate ramp, with a level terrace. Combat/pursuit/spawning continue to read the existing floor plane. Render callbacks lift visual children and restore them, including the childless chest; damage numbers retain their source contact rather than deriving height from their floating position. Floor sprites deform over the same surface. Reason: lifting only architecture would make actors float or sink, while moving gameplay roots would silently alter reach and deterministic simulation. The ramp starts beyond the boiler crowns. Scope excludes jumps, overlapping floors, door progression and balance changes. Painted reference fidelity remains incomplete.


## 2026-10-08 — Prefer broad material hierarchy over more ornaments

Owner requested the next three art tasks as one package. Selected court stone cohesion, readable formed boiler hoods and distant cavern separation, rather than adding small architectural props. The finer paving amplified an independent dark/pale stone lottery; narrowing that variation preserves the bond and allows existing broad deposits to carry the wear. Boiler hoods lose repeated complete riveted panels but retain sparse seams and complete far slopes after the first plain version read as a dish. A local backdrop material compresses distant contrast/saturation without another fullscreen overlay or changing gameplay. These improve material hierarchy; they do not complete painted-reference fidelity or introduce dynamic lighting. Actual before/after proof: surface-cohesion-01.


## 2026-10-09 — Use authored forge sources for metal reflection

The previous stone/light cohesion left industrial iron cold beside the same fires. Chose shared authored light sources and explicit physical surface heights instead of applying an orange tint to entire structures or adding runtime lights to projected geometry. Metal retains its existing directional shading and receives bounded warmth that decays with height/distance. Pipes subdivide only when this treatment is requested, avoiding an unlit midpoint on a long run between two cooler ends. Source image art, glow windows and gameplay remain unchanged. This is a reflected-light presentation approximation, not physically based or dynamic illumination.


## 2026-10-09 — Work the stone surfaces without changing the map

Compared another global tint pass, a new raster atlas and restrained surface modelling. Selected bounded per-stone mineral value variation, sparse irregular edge flakes and broad blind recesses on long bastions: they address flat slab faces and repetitive shafts using the current approved layout and reusable textures. Avoided extra scattered props or blocking rubble. Flakes are flush; recesses are projected surface treatment rather than real holes. No gameplay or material pipeline change.


## 2026-10-09 — Complete architectural fields instead of adding ornaments

Use broad bounded stone relief on the gate, round sections on stored pipe and recessed spandrels under the deck. These address repeated small panels, beam-like pipes and isolated arch rings in the current render. Reuse source textures/materials and preserve the approved layout, supports, silhouette and actor clearance. The plain metal patch removes inappropriate rivet frames but leaves texture richness open. No scope change to gameplay, camera or world progression.


## 2026-10-09 — Prefer surface legibility over more ornaments

Compared adding detail geometry with sampling the existing iron surface across larger coherent faces. Chose broader continuous UVs and bounded lower-wall deposits because the previous tiny crop hid the grain; stored pipes retain the same section. Pit receives one extra collar-shadow row and shared-station form shading, preserving its authored ribs and pool. Environmental brass becomes matte, with quieter grooves, to keep combat indicators dominant. No new source art or gameplay claim. Three-agent actual-capture review found the gain useful but restrained, especially in the overview; coil segmentation and graphic molten pool remain open.

Owner explicitly requested completed commits pushed on2026-10-09, superseding earlier local-only notes. Normal fast-forward push only; do not rewrite history.


## 2026-10-09 — Bounded free Blender trial, no production migration

Decision: retain the Unity game and avoid a new paid service while evaluating one Blender furnace. Actual matched-scale Unity study shows stronger metal shading but less architectural character; replacement is not accepted. Preserve production arched openings in the next iteration and prove depth/contact in one map placement before migration. Proof: ArtDirection/2026-10-09/blender-furnace-01. No runtime or device changes.

## 2026-10-09 — First production Blender bake, bounded to one boiler

Use the authored study02 render as two static depth-aware image bands on boiler0, not the full23,104-triangle source model. Preserve the existing foundation, pipe anchors and layout. Keep Android texture1024ASTC6/mips/non-readable and scene-owned materials. Contact shade joins existing floor decals after an existing regression test caught a second decal batch.20renderers replaces18with fewer vertices; device performance remains unmeasured. No new paid service or whole-map pipeline migration. Full suites/APK/install pass; device visual acceptance pending availability. Evidence: ArtDirection/2026-10-09/baked-boiler-runtime-01.

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

## 2026-10-09 — Map shape takes priority over small polish

Owner requested closer reference map size, shape and major features. Implemented westboundary-24→-30 with complete exteriorassembly relocation, +192walkable squareunits, continuous tapered fortressfoundation and deeper industrialpit. Original gate/bridge/pitfloorboundaries and camera remain. Actual proof: ArtDirection/2026-10-09/foundry-massing-01. Full verification/device status: latestdocs38/23entry. Next priority: gateapproach/enclosure, entrancebridgeproportions and purposefulworkyardfeatures. This supersedes the preceding suggestion to switch immediately to actor/encounter art. Fullreference fidelity is notcomplete.

## 2026-10-10 - Real approach proportions and fortified silhouette

Owner requested closer reference shape/features and higher visual quality, authorizing parallel work. Chose25% wider and approximately69% longer full bridge extent, fortified gate wings and a flush industrial transfer yard. Shared movement/render bounds change with the bridge; its court overlap remains for traversal but no longer paints over court paving. Chose flush rails rather than raised machinery without collision. No new transition/interaction; gameplay-camera width and run rules unchanged. Actual image review and989 tests passed; device restart/install remains pending. Evidence: foundry-approach-01.

## 2026-10-10 - Art-first courtyard craft package

Owner explicitly chose reference-map art before further gameplay/region progression. Three coordinated slices are visually accepted: mixed ashlar workyard/court with broad mineral families and contained corner wear; broad tapered-face gate recesses with dressed caps/quoins; stock-shelf hoist with bearing-supported drum, braces, two chains and lifting yoke. Existing layout, walking routes, doorway, camera and gameplay remain unchanged. Stock feet stay within the original non-walkable shelf and upper machinery has explicit3.6-height depth ownership. No new texture, material or per-frame component.

Actual Unity before/after evidence: ArtDirection/2026-10-10/foundry-craft-01. Region25renderers unchanged;113,622 to117,754vertices (+4,132/3.64%). Yard and gate gains are clear; central paving delta is moderate. Fine irregular stone grain and richer metal surface remain below the approved reference; this is not map-art completion. Local encounters, boss transition and optional finale implementation remain deferred. Full tests/build/device status belongs to proof validation/verification.md and docs22/23/38.
## 2026-10-10 - Improve material consistency before adding map features

Owner continues reference-map art first. Chose an authored fixed-view hoist bake over more procedural bevel strips, accepting one additional renderer and512compressed texture. Retained exact shelf/depth/height and original geometry as fallback. Matched review confirms crane must remain in front; promoting the hoist merely to reveal its chains would produce incorrect spatial overlap. Smaller gate stone and restrained fractured floor surfaces improve consistency without changing navigation or introducing gameplay. Evidence: foundry-materials-01 and stock-hoist-01.
## 2026-10-10 - Preserve workshop geometry while replacing material presentation

Decision: bake the existing stock-station metal and crane body from authored Blender geometry into two fixed-view transparent cards. Keep original wood, crane footing, chain, basket, tip sheave and lights separate, with original footprints and camera. Explicit per-card depth owners and scene-owned cleanup follow the established hoist contract.

Actual image review rejected a large middle-mast masonry overlap. Correct only the named rear northwest lantern's masonry order; do not globally raise the crane or hoist over unrelated foreground parts. Accepted cost is two card batches plus one localized masonry batch (26 to29 total); vertices fall 118,902 to116,178. Static counts do not prove faster frames. Reference-map art remains first, and physical-device acceptance is pending the existing restart/install question. Evidence: workshop-coherence-01.

## 2026-10-10 - Material readability without new gameplay signals

Use neutral warm tints on already golden coping albedo, clean inner-rail UVs and brass widths inside existing grooves. Preserve matte/non-emissive floor lighting and its1.9color ceiling so floor heraldry does not become an ability telegraph. Stone changes soften joints and vary compact oblong proportions while retaining the coverage lattice and brass constraints. Pit machinery stays below the existing deck, with unchanged pools/collar/atmosphere and vessel envelopes.

Image review prompted a softer, narrower, deeper central flue aperture; no new geometry was needed for that correction. Accepted package29renderers unchanged,117,045vertices. Floor/reference similarity gain is modest; do not treat another numerical weight tweak as the next substantive reference-quality milestone. Evidence: foundry-surface-cohesion-01.

## 2026-10-10 - Irregular stone silhouettes with continuous floor coverage

Use convex hand-quarried contours inside each existing packed flag, with unequal setbacks and corner cuts. Keep the complete mortar underbed and partition the face along adaptive light cells, rather than allowing slab-wide triangles to distort local light. Bound the common bevel offset before a short chamfer can collapse. Regular ceremonial/repair/infill flags and ramp-crossing flags retain the previous path; rectangular wear overlays do not render outside new contours.

Actual image review accepts the bounded gain: visible quarry edges, no holes/light wedges or lost actor readability. Exposed joints are stronger than before and the course structure remains rectangular. Full reference fidelity is still open. Static cost29renderers unchanged,113,995vertices versus117,045; no FPS improvement claim. Evidence: foundry-flagstone-01. No phone action; prior restart/install question remains pending.

## 2026-10-10 - Add world-anchored depth around the foundry

Decision: close a reference composition gap with an intermediate layer of five distant industrial towers and one broken arched span, while retaining the approved playable layout. Use explicit sorting ranks below every island batch, including the deep smelting basin. Only the imported foundry backdrop reserves the two extra ranks; normal backdrops retain their existing ordering.

The initial material treatment was rejected in actual Unity review because sharp masonry and square bottoms looked attached or suspended. Reuse the cavern atmospheric shader with a per-material blend and fade only the low shafts; retain opaque upper tiers and subdued warm windows. The original backdrop's default blend remains 0.22. One owned material and two owned meshes are released with the scene; textures and shared materials remain borrowed. Narrower quarry setbacks and lighter mortar reduce the previous contour pass's outlined-grid effect.

No new playable route, obstacle, encounter, camera rule or save behavior. Full reference fidelity is still open. Device acceptance and the earlier concrete owner-run restart/install decision remain pending; no phone actions or fresh observations in this pass. Actual evidence: ArtDirection/2026-10-10/foundry-depth-01.

## 2026-10-10 - Scenic casting bay outside navigation

Decision: add the press/moulding kit on a supported exterior shelf u[-18,-13], v[19.1,22.4], rather than introducing raised equipment inside the live kite lanes. Preserve the north rail and existing finial, walking union, camera and combat. The explicit non-landmark depth owner prevents the new card from claiming nearby masonry. Actual Unity review accepts shelf contact and rail overlap. This fills a small production interval; whole-map reference fidelity remains incomplete. Source, proof and final verification: ArtDirection/2026-10-10/foundry-workbay-01. No phone action in this pass; the previous concrete owner-run restart/install question remains pending.

## 2026-10-10 - Architectural relief before broader brightness changes

Decision: replace the blank visible shafts of the two north skyline towers with real recessed furnace bays, dressed arches, chamfered jambs and tapered shoulders. Strengthen the existing broken viaduct bearings/parapets while retaining its gap. Keep anchors, background sorting, atmospheric material and lower fade. The first image review rejected continuous orange stripes; final shorter panes and raised frames retain quiet background hierarchy. Actual Unity root/independent review accepts the refined version, with modest portrait gain because arch heads sit above its crop. Do not describe this as full reference completion. Evidence and final test/device status: ArtDirection/2026-10-10/foundry-skyline-01.

## 2026-10-10 - One court medallion and a stone-framed gate approach

Owner requested gate and courtyard improvements together toward the approved concept. Chosen: coherent portal masses, one flush stone medallion and a segmented stone approach border. Rejected: more small pier belts, a second oversized court bullseye, and brighter duplicate gold racing stripes. This improves architectural hierarchy while preserving the existing playing space and door art. The border required visible mortar and shared miter joints; actual root/independent Unity review accepted the corrected result. No navigation, camera, gameplay, save or device changes. Proof: ArtDirection/2026-10-10/foundry-gate-court-01.

## 2026-10-11 - Finer foundry stonework and gate material

Built-in image_gen edited the existing FoundryPavingV4 surface into versioned FoundryPavingV5.png. Its fine mineral pores/flecks replace broad scalloped cloudy wear; generated pixels are imported unchanged. The approved reference remains foundry-layout-01/foundry-v1.png. Both sources1254square RGB; meanRGB87.76/80.85/84.14 ->85.92/79.70/82.13, slightly darker. Android import1024square ASTC6x6/11mips/non-readable. Old sources/GUIDs preserved; only MAT_FoundryPaving texture binding changed. Full prompt/provenance: ArtDirection/2026-10-10/foundry-stonework-01/prompt.txt.

Ordinary ashlar now favours1x1/1x1.5 flags, with quieter narrower joints and restrained stone value variation. Per-slab cloudy vertex wash removed; shared soot, quarry family, lamp field and adaptive ramp sampling retained. Shared public PavingGrainSpan5.7 replaces8.55 in all foundry paving emitters (1.5xgrain density); unrelated small-arena mapping stays8.55. Court medallion and approach border remain. Gate lintel/crown/returns receive bonded courses and restrained edge losses; blind-niche UVs are continuous. Coping blocks restore shallow real bevels/joints inside the original cap footprint/top height.

First actual Unity review rejected persistent cloudy paving and blank gate caps. Final V5 plus clipped coping blocks accepted by root and independent review; overview gain is subtle, close-up mineral texture clearer and actor/effect contrast retained. Baseline22e32ea before captures reused unchanged; two after captures run, first rejection retained. Region32renderers unchanged;115944->118082vertices (+2138/~1.84%). One new texture asset, no new material/renderer/per-frame component. No frame-time or allocation claim. Map/ramp/camera/navigation/combat/scene/saves unchanged. Full reference fidelity, regular-paving rhythm and wider architectural richness remain open. Final tests/build/device status in proof validation record.
## 2026-10-11 - Recessed portal and industrial exterior parapets

Completed together: a supported recessed stone cartouche with a restrained iron lock replaces the gate's central stacked boxes; exterior rail bays receive a solid stone sill, square iron web and broad coping. Existing gate crown/door/banners and rail footprint/top/posts/depth owners are retained. Pit and bridge keep their separate open balustrades. No image generation or new texture/material/per-frame component. Sources: FoundryRegionArchitecture.cs, FoundryRegionGateHeader.cs, FoundryRegionMassing.cs.

Matched actual Unity overview/details/production-width portraits accepted by root and independent reviewer. No new visible hero obstruction or corner/boiler-return regression in reviewed views. Region remains 32 renderers; 118082 -> 117629 vertices (-453/about 0.38%), not a performance measurement. Baseline e61ac5a before evidence reused unchanged; one new after graphics capture. Gallery, reproduction and final verification: ArtDirection/2026-10-11/foundry-portal-parapet-01.

Remaining reference gap: long regular perimeter bays, paving rhythm and wider architectural variety. Portal relief is a close-view improvement, not a whole-map transformation. Map/ramp/camera/gameplay/scene/saves remain unchanged. Physical-device/crowd/midrange/first-ramp acceptance remains open; regional encounter/boss progression remains deferred by the art-first decision.

## 2026-10-11 - Distant furnace variety and exterior bay rhythm

Three simple distant shafts now have sloping shoulders/narrow necks, splayed bearing ribs and single/twin/offset exhaust crowns. Existing anchors, fade, material, light openings and background ranks retained; two arched northern furnaces unchanged. Exterior spans now group into wide recessed masonry panels flanked by iron openings and short lower-sill open bays. Post locations, span bounds, top height0.72, depth owners, light anchors and pit/bridge balustrades remain. No new texture/material/renderer/per-frame component; map/ramp/camera/gameplay/saves unchanged.

Root and independent reviewer accepted matched actual Unity views. Gain is most visible at close perimeter views; whole-map impact is modest. Region32renderers,117629->118537vertices (+908/about0.77%). Distant2244masonry+144fire=2388, below unchanged2500bound. No performance claim. Evidence/reproduction/final verification: ArtDirection/2026-10-11/foundry-mass-rhythm-01, baselinec532902. Before images reused unchanged; one new after graphics capture.

Next substantial priority: purposeful workyard clusters and more individual boiler silhouettes, with investigation of dominant painted backdrop scale. Avoid another tiny parapet/crown pass. Existing rail across closed gate is unchanged, pending transition design. Full reference fidelity and accumulated physical-device/crowd/midrange/first-ramp acceptance remain open. Regional encounters/boss progression remains deferred.

## 2026-10-11 - Workyard identity and boiler material consistency

Recompose existing supported workstations instead of adding scatter to full shelves. Keep native editable sources and unchanged rendered imports with fixed projection/depth contracts. Reject procedural overlays that visibly mismatch an accepted bake: tall boiler attachment removed, squat hatch changed to a shallow seated lid. Three helpers split source authoring, boiler/tests and independent review; root exclusively ran Unity. Same map/ramp/camera/gameplay,32renderers,+172vertices; this is not a performance measurement. Matched evidence and final gate: ArtDirection/2026-10-11/foundry-workyard-identity-01. Reference fidelity is still incomplete and future boiler variety needs coherent source art. No phone action this slice; earlier concrete restart/install decision remains pending.
## 2026-10-11 - Recompose the plate, preserve camera coverage

Do not shrink the entire background sprite to reduce perceived building size: that breaks edge coverage. Recompose the source instead and select the new borrowed sprite only for the foundry. Keep the original hall art/fallback, cover fit, bounded parallax, atmosphere material and render ranks. New FoundryCavernV2 generated using built-in image_gen; source pixels unchanged. Root/independent actual Unity review accepts whole-map depth improvement, with modest portrait gain. No geometry/gameplay/save change. Prompt, evidence and final test/build/import status: ArtDirection/2026-10-11/foundry-backdrop-depth-01. Phone deployment decision remains pending.

## 2026-10-11 - Expose existing distant architectural volume before adding ornaments

Options: more skyline geometry/trim; another painted plate; or native face/UV/material coherence. Chosen: the native material pass, because the existing recessed arches and shoulders were present but collapsed into near-black faces beside the new backdrop. Preserve the 2,388-vertex structure and borrowed wall art, apply finer coherent courses and controlled face/height light; do not change the shared backdrop shader. Root and independent actual Unity review accepted the improvement while recording rigid silhouettes as remaining work.

Next meaningful options: a complete northwest workyard composition with explicit circulation/blocked footprints, or authored north-shaft/broken-span silhouette replacements if navigation stays frozen. Raised props must not be placed on walkable routes without matching navigation. Avoid another isolated tint/trim pass. Regional gameplay/boss progression remains deferred under the owner's art-first direction.

The full gate separately exposed the old chest telemetry test's 100 HP ceiling: optional health books legitimately apply before Opened. Repair the test with deterministic no-book/health-book scenes and exact event-time HP/book checks, not production balance changes or a looser numeric ceiling. Failed/focused/final evidence is kept with the art proof.

## 2026-10-11 - Differentiate the native north shafts structurally

Keep navigation frozen for this visual slice. Use two visibly different receding furnace crowns and four connected corner buttresses; preserve the broken arch gap but give its ends distinct closed fractures. Preserve existing materials/UVs/atmosphere rather than another isolated tint pass. Remove fully buried caps to remain at 2,496 vertices under the existing 2,500 distant budget. These scenery structures create no traversable route. Whole-map gain is restrained; coherent workyard distribution remains the next larger reference gap. Raised workyard props require matching navigation, not visual-only obstructions. Evidence: ArtDirection/2026-10-11/foundry-north-silhouette-01.


## 2026-10-11 - Interior working props require shared navigation

Owner continued the reference-led art work. Keeping all equipment on exterior shelves cannot reproduce the reference's working courtyard. Place two low islands on existing floor: trolley[-26,-4,-22,-1.8] and mould bench[-21.8,11.8,-18.6,14], with one shared immutable rectangle contract for visual skids, actor clearance, spawning and pursuit. Reject an earlier western trolley proposal because it occupied a tested anchor and left a narrow edge lane. Final west lane is4units; retain all old camera/circulation assertions.

Distinguish physical paving SurfaceContains from walkable Contains. Exact actor-margin-expanded rectangle sweeps plus1e-4 float relief prevent tunnelling/grazing; add8derived corners to the static pursuit graph. No independent decorative collider or hidden visual-only obstacle. This is the smallest movement support needed by the art slice, not new progression, combat balance or an interactive production system. Assets remain original native geometry, shared materials and continuous floor. Evidence: ArtDirection/2026-10-11/foundry-workyard-islands-01.


## 2026-10-11 - Widen the real court, preserve reference landmarks

Decision: after owner explicitly identified insufficient width, move FoundryMap.West from-30 to-36. Preserve South/North/East, pit, bridge and gate. Main span44->50 and western strip14->20 create real floor. Existing WestAssemblyOffset relocates rim assemblies-6; WorkyardShift=(West+30)*.5f recenters interior islands/turntable-3. Rail endpoint followsWest+.35. Shared obstacle and surface contracts remain; no phantom equipment or floor holes.

Consequence:192additional square design units, potentially lower encounter density with unchanged spawn counts. This is a bounded visual-layout adjustment, not a balance decision. Retain casting shelf after actual visual review found the stock/casting gap coherent with intervening tower/balustrade. Equipment detailing stays inside proven footprints. Full reference proportions/materials and physical-device acceptance remain open. Evidence: foundry-broad-court-01.

## 2026-10-11 - Contract the gate arm without shrinking the broad court

Decision: preserve the50u court and20u western workyard, move the landing/ramp and rigid gate complex4u toward the court. Gate span18->14u removes40square design units from its approach; movement/spawning/pursuit read the same shared bounds. This brings the portal into the main composition without a camera zoom or a global coordinate warp. Main pit, bridge and work islands remain fixed. Travel distance changes; no encounter-balance acceptance is implied.

Near boiler anchors stay, far pair follows the gate. Reservoirs move outward with connected low foundations and one supported main saddle. Boiler drains must follow BoilerCentres, not independent world literals: the first actual capture exposed a floating stale far drain, now repaired and guarded by emitted-mesh contact checks. Northern crowns reuse their existing geometry budget. Three helpers split layout/tests, crowns/contact review and independent art review; root alone executes Unity. Evidence and final verification: ArtDirection/2026-10-11/foundry-compact-gate-01. No device observation or installation in this package.

## 2026-10-11 - Keep furnace individuality in authored sources and shared atlases

Decision: give each of the four existing furnace assemblies its own native Blender silhouette, bake it at the original1024resolution and pack two family variants into each2048x1024atlas. Preserve runtime card projection/splits, centres, foundation and service contacts; use existing2materials/4bands instead of adding per-variant materials. Tradeoff: calculated compressed-block payload increases1248832bytes across both atlases including mips, while renderer count stays32 and removal of the old native hatch saves172vertices. Device memory/frame performance is not inferred from these numbers.

Native source geometry keeps material/light coherence. Independent review corrected solid exhaust caps and a receiver seam before import. Transparent authored margins protect normal-view filtering; terminal1x1atlas mip inevitably averages variants. Near/far selection uses shared boiler anchors, and scene tests verify actualUVtile/depth/card geometry, compressed import and resource lifecycle. Sources/proof: ArtDirection/2026-10-11/foundry-industrial-01 and its sibling sourcefolders. Current region layout, navigation, encounters and save behavior remain unchanged.

## 2026-10-11 - Lower connected gate shoulders and readable stone assembly

Decision: make the painted door primary by removing the outer towers' upper storey and flattening the portal crest, within existing foundations and navigation. Shorten the inner cast terminals to fit their existing depth envelope. New approach flags remain flush and share the current ramp projection rather than implying new stairs or obstacles.

Reason: reference comparison showed competing tower masses and an overly continuous paving panel. The first valid implementation did not make its new bond visually readable; actual-image review required a joint/bed correction before accepting it. Tradeoff:4578 additional static vertices overall, unchanged32renderers and no new textures/material instances/updaters. Device frame time is unmeasured. Evidence and final gate: ArtDirection/2026-10-11/foundry-gate-stone-01.


## 2026-10-11 - Court, bridge and dressed masonry verified; device pending

Decision: unify the court and bridge quarry field and articulate existing gate stone faces in place. We considered more independent per-slab contrast and raised/chipped masonry. Broad shared quarry tones reduce checkerboard and preserve actor contrast; coplanar edge catches retain existing contact/depth envelopes without creating new obstacle expectations. Long fascia becomes fitted blocks with its original cap still continuous. This is a bounded material/craft gain, not reference completion or measured phone performance.

Root and independent seven-pair actual Unity review accept reduced bridge checkerboard, clearer court bond and restrained gate edges. Actor/feet/ability guide and pipe contacts read correctly in inspected views. Whole-map gain is modest; full reference material/structural richness remains incomplete. Region32renderers/135688vertices versus129812baseline (+5876/about4.5%); distant2496 unchanged. These are resource counts, not device performance evidence. No ADB or fresh phone observation; earlier concrete install/restart decision remains pending. Before future deployment inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Physical-device circulation/crowd/HUD/motion/shimmer/midrange acceptance and region gameplay/boss progression remain open.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-stone-01.


## 2026-10-11 - Pit machinery and exterior masonry verified; device pending

Decision: distinguish complete vessel heads and servicing structure instead of multiplying identical ribs. Replace large wrapped brick seams on load-bearing exterior wedges/piers with fitted mineral faces. Keep projected grade breaks during any span join, including the contracted skirt.

Root and independent actual-image review accept clearer machinery construction and broader quieter exterior stone faces. Whole-map gain is modest; full reference material/structural richness is unfinished. Region remains32renderers;135688->148756vertices (+13068, 9.6%). Distant2496 unchanged. No new texture/material/updater and no phone-performance claim. No ADB or fresh device observation; the earlier concrete install/restart decision remains pending. Future deployment must inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Device circulation/crowd/HUD/motion/shimmer/midrange acceptance and regional encounter/boss progression remain open.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-pit-wall-01.


## 2026-10-11 - Bridge architecture and workyard equipment verified; device pending

Decision: connected bridge parapets stay outboard of the walking lane, with short inscribed depth separators and exact asymmetric court clipping. Give trolley and mould bench distinct complete construction instead of scattered decorative props. Far retained cargo hardware must precede its cargo in the painter batch.

Root and independent actual-image review accept clearer arch construction, seated keeper statues and better supported/recognisable yard equipment. Parapet and whole-map gains remain modest; full reference material/structural richness is unfinished. The center/near-island portraits show clear actor feet/guide, not exhaustive moving-edge/crowd acceptance. Region remains32renderers;148756->162568vertices (+13812, 9.3%). Distant2496 unchanged. No new texture/material/updater and no phone-performance claim. No ADB or fresh device observation; earlier concrete install/restart decision remains pending. Before eventual deployment inspect fresh state and preserve current original saves/telemetry; never restore historical snapshots. Device circulation/crowd/HUD/motion/shimmer/midrange acceptance and regional encounter/boss progression remain open.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-bridge-yard-01.


## 2026-10-11 - Western loading assemblies verified; device pending

Decision: improve complete supported loading assemblies using native Blender sources and exact existing card/depth contracts. Replace old solid support faces instead of overpainting them. Separate receiving stock from active casting through object construction rather than new lights/effects.

Root actual-image review accepts better connected loading architecture and clearer receiving/casting identities. Live crane chain/sheave/load contact remains seated in the inspected images. The existing foreground lamp pole crosses the mast and the large northwest shaft still dominates some close views. Local gains are visible; whole-map gain is modest and full reference material/structural richness remains unfinished. Region remains 32 renderers; 162568->165392 vertices (+2824, 1.7%). Distant2496 unchanged. Existing crane import1024 ASTC6x6/11mips and stock/casting512 ASTC6x6/10mips remain non-readable; no extra runtime texture, material or updater. Native Blender mesh counts are authoring counts, not runtime GPU geometry. These resource counts are not phone-performance evidence.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-loading-bay-01.


## 2026-10-11 - Connected service shafts verified; device pending

Two existing northwest distant furnace shafts receive narrower necks, joined dressed shoulders, real recessed neck panels with side returns and hollow exhaust crowns. The original leaning buttress heads move inward to meet the narrowed neck. The mouths have low throats, inner walls and separate annular rims instead of solid roof caps. Existing lower furnace body/fire, other distant variants, atmospheric fade, materials, sorting, camera, navigation, gameplay and saves remain unchanged.

Region32 renderers/166468 vertices versus165392 (+1076); distant geometry2496->3572. The combined distant vertex ceiling is intentionally revised from2500 to3600 for two complete hollow crowns/recessed necks; all existing lifetime, material, rank, fade and bounds assertions stay intact. This is a measured resource-budget decision, not a frame-time result. No new texture, material instance or per-frame updater.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-service-shafts-01.


## 2026-10-11 - Court, pit and gate construction verified; device pending

The court compass now uses restrained paired brass facets and a continuous engraved ring over three fitted stone courses. The pit upper lining has a joined below-deck bearing course and four relief ribs with side returns, leaving all furnace bays, vessels and the molten seam unchanged. Four gate shafts receive genuine blind recesses with backing, returns and broad dressed surrounds; two connected header shoulders replace the former interrupted coping ends. Existing court/pit/gate footprints, door and lock sockets, ramps, navigation, camera, material batches, depth ownership, scene, gameplay and saves remain unchanged.

Region 32 renderers/165232 vertices versus 166468 (-1236); distant geometry 3572 unchanged. No new runtime texture, material, renderer or updater. Counts are resource observations, not device frame-time measurements.

Evidence/reproduction: ArtDirection/2026-10-11/foundry-court-pit-gate-01.


## 2026-10-12 - Court, pit and gate material integration verified; device pending

Gate recesses now map the existing stone grain by real block dimensions, continue it over bevels and retain inherited corner lighting plus a shaded-head/reflected-foot gradient. Pit ribs use a quiet cast-iron atlas interior at bounded grain scale with restrained integral worn arrises; the under-rail bearing uses cool masonry and actual narrow block beds. The medallion ring and fitted centre flags gain flush dressed shoulders and share the surrounding court quarry family, reducing the separate warm checker pattern. Existing footprints, silhouette, pit fire and vessels, door/lock, navigation, camera, scene, gameplay and saves remain unchanged.

Existing atlas mapping and local surface construction were chosen over a new global brightness change or additional ornamental rows.

Region 32 renderers/166467 vertices versus 165232 (+1235); distant geometry 3572 unchanged. No new runtime texture, material, renderer or per-frame updater. These resource observations are not device-performance measurements.

Evidence/reproduction: ArtDirection/2026-10-12/foundry-material-integration-01.
