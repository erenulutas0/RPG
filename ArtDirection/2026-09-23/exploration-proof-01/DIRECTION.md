# Superseding direction: explorable region — 2026-09-23

The owner clarified that the desired world is a large traversable region inspired by exploration in survivor games, not a small sequence of contained rooms selected on a node screen. The assistant proposed keeping the existing isometric presentation, starting with one fixed multi-screen region, and validating movement/encounter cadence before procedural generation. The owner approved this approach.

This supersedes region-map-01/OPUS_TASK.md and its node-selection implementation plan. Opus has not been started, according to the latest owner reply. Do not build the old mutually exclusive node-route UI as the default game flow. Preserve the approved separate grand boss hall and optional post-boss swarm/extract direction; exact access/unlock conditions still need to be specified with the gameplay owner.

## New first slice

A simple connected region using existing actor scale and follow camera. Ordinary encounters, chest/elite/forge locations eventually become places reached on foot; they are not all triggered through menu nodes. Begin with a fixed authored layout and reusable environment pieces. No3D conversion, vertical platforming, random terrain, pathfinding rewrite or hundreds of simultaneous enemies in this proof.

The current ArenaGeometry is a convex rhombus. This first measurement deliberately retains it and compares near/far=-27/+27 and halfWidth18 with the current -9/+9/9. Floor area grows from162 to972 square floor units (6x); widest span36 world units is4 camera widths at width9. It is a provisional size experiment, not a finished map outline. Cropped concept art is not collision data.

## Gameplay questions to resolve next

- Existing stamina is a combat anti-kiting rule:2seconds at full walking speed, then40% speed. Measure travel before deciding any out-of-combat movement rule; do not silently grant infinite combat stamina.
- EncounterController currently brings packs around the hero and advances rooms. A large floor alone does not create local encounters or exploration. Add an explicit authored encounter-site model with activation/resolution/reward ownership and simulation coverage as a separate gameplay slice.
- Define where rewards/forge/boss access are placed and what is mandatory versus optional. Avoid inventing key counts or repeatable rewards from this art proof.
- Large empty ground needs meaningful landmarks and activity spacing, but collision obstacles require deliberate navigation work. Start with decorations outside travel paths.
- Camera, texture allocation and device performance require measurement; Editor traversal is not phone acceptance.

Casting Courtyard V2 becomes one landmark within the region, not the whole map. Boss-hall art remains separate. Existing live floors and save/telemetry rules stay untouched during this measurement.
