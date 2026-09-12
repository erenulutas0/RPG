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
**Status:** Accepted for prototype implementation; Unity verification pending  
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
