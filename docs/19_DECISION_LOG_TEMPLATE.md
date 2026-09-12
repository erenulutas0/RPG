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
