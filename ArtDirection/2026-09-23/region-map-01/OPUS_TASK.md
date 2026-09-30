> SUPERSEDED 2026-09-23: owner chose one explorable multi-screen region with physical encounters/points of interest, not a node-selection map. Do not implement this old branch workflow. Read ../exploration-proof-01/DIRECTION.md first.

# Gameplay implementation handoff — first region route

Read docs/25_HANDOFF_2026-09-15.md, the current docs/01,03,06,21,22,23,27,35,36,37 and the latest decision in docs/19. Read ArtDirection/2026-09-23/region-map-01/DESIGN_AND_HANDOFF.md. Talk to the owner in Turkish; code/docs in English. This file prepares a task; it has not been sent to another assistant.

The owner approved one region: Combat A -> Combat B -> choose Elite OR Chest -> Forge -> Main Boss -> choose Extract OR optional final Swarm. The current impressive arena is the boss-hall target. Astra handles art; your scope is progression/gameplay and testable data. Current runtime still uses linear FloorDefinition/FloorProgress. Do not modify Astra's uncommitted art, ExitSealView, scene composition or import tools without coordinating. Do not open Unity while another assistant is building.

First deliver the pure route model and a deterministic simulation proof for both exclusive branches, preserving existing linear floors/tests. Use stable node/edge identities, once-only resolution and stale-choice rejection. Make the minimum integration contract for the map view explicit (current node, available nodes, completed nodes, branch choice). No web backend, SDK or networking. Do not invent a parallel reward or banking system.

Before binding this into the scene, report the concrete policy for the dedicated chest node and how region completion maps to current floor/deepest progress and checkpoints. Keep reward values authored, and have Fable compare elite risk/reward against the chest route using multiple seeds and all weapons. Existing chest props are not automatically the new room type. Boss rewards and pending upgrades resolve before the Extract/Swarm choice. The route transition must respect the existing pause/choice lifecycle and telemetry run identity.

Then implement the agreed gameplay connection with full scene/simulation parity and the repository verification gate. Clearly report which parts remain stubbed or development-only. Provide Astra a stable route-view contract and state examples; do not implement new map art or battlefield geometry. Never claim the browser mockup is the playable region.
