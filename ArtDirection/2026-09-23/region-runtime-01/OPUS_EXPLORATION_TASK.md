> ON HOLD — owner deferred Opus on 2026-09-23 to shape the map through art first. Do not start this task until explicitly resumed.

# Opus/Fable handoff — first physical exploration loop

Work in E:/MobileGame, Unity6000.0.65f1. Speak Turkish; write code/docs in English. Read docs/25_HANDOFF_2026-09-15.md latest entries, primary docs01/03/06, latest21/22/23, and docs26–37 for movement, rewards, chest and finale contracts. Read this folder's README and ../exploration-proof-01/DIRECTION.md. The old region-map-01/OPUS_TASK.md is SUPERSEDED. Preserve uncommitted Astra art work; no blanket staging/reset/cleanup. Do not start Unity or use the phone concurrently with Astra.

## Problem and bounded target

The owner approved a walkable multi-screen region, separate grand boss hall and optional post-boss swarm/extract. The current wide development flag changes geometry only. EncounterController still advances linear rooms and brings packs around the hero. Implement one development-only exploration loop: two physically reached combat sites, one optional one-time chest, and a boss entry unlocked by both combat sites. Reuse existing enemies, loot, chest and boss rules. No procedural world, backend, jumping, new stamina/balance changes or node-selection menu.

First inspect EncounterController, FloorProgress, EntrySides, HeroMovementInput, ChestSpawner/ChestRule, GroundDrops, RunState, RunChoices, CheckpointService, DescentSimulation and telemetry. Propose a short implementation plan and identify any rule that requires an owner decision. Suggested MVP policy: either combat site may be first, only one combat site active at once, walking away must not reset enemies or duplicate rewards. Resolve site completion through existing authoritative deaths/waves. The chest is optional and cannot pay twice. These are prototype defaults, not permission to alter existing authored floors globally.

## Ownership and integration

- Put authored site ID/position/activation radius/content references in data and per-run status in a pure runtime model. The view never decides completion or grants rewards.
- Use real floor coordinates and the current ArenaGeometry. New sites must be reachable and separated enough to avoid simultaneous activation. Use a small fixed layout; positions are tuning data. The two fights and chest should fit a short first test, not require traversing the entire972-unit surface.
- Keep combat spawning within geometry and existing spacing/capacity rules. Suspend automatic room advancement in the new mode without creating a second combat/payment authority.
- Reaching the unlocked boss entry transitions to the existing normal hall and intended boss content while retaining the same run's HP/build/gold/seed. Do not reset the run or silently manufacture checkpoint rewards. Decide the clean scene/geometry lifecycle before editing code: ArenaView intentionally freezes geometry for each loaded scene.
- Use existing post-boss/finale rules. If the current rules cannot express the owner's extract-or-final-swarm choice, call that out and keep it a separate bounded follow-up; do not silently add an economy rule.
- Preserve legacy mode bit-for-bit where practical and all save/telemetry contracts. Explicitly document new mode parity limits. New events must follow existing telemetry conventions and rotation/privacy rules.
- Astra owns FoundrySceneryView, ArenaView art references, textures/materials, camera, HUD and art proof tools. Coordinate any required mode/geometry handoff rather than editing those files concurrently. Temporary code-native site markers are acceptable for proof; no new painted art needed.

## Verification and delivery

Test activation from inside/outside, deterministic activation if radii overlap, completion exactly once, chest payment exactly once, revisits, leaving/reentering an active fight, pauses/upgrade panels/death, and fresh-run reset. Cover the boss gate locked/unlocked and run-state continuity into the boss hall. Extend the pure simulation with the same activation rules and fixed route/seed; preserve existing default-mode parity.

Run .NET checks, compile check, targeted tests, then full Unity suites/APK and static integrity in documented order. Device testing requires coordination and fresh backup; no save rollback. Record results and remaining limits in docs21/22/23/25. Report files changed and the exact interface Astra should consume. Do not claim finished exploration from geometry alone. No changes to release/default progression until the development loop is verified.

