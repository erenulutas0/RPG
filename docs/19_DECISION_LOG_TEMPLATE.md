# Decision Log

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
