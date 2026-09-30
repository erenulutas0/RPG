# First multi-screen traversal measurement — 2026-09-23

## Fixed-scale paving follow-up

Latest screenshots now use production `PlatformMaterialMeshes.FloorUv`: world-space planar paving at the original hall's scale, independent of region width/depth. The old oversized paving observation below describes the initial capture and is superseded for the floor surface. Coping, walls and foundation still need their own wide-region review. The region itself remains a temporary scene; no APK or device installation in this follow-up. Graphics traversal passed 1/1, .NET 380 and compile clean. Read ENVIRONMENT_KIT.md for the separate reusable scenery plan. No collision objects or walkable elevation have been added.

Read DIRECTION.md: this supersedes the node-map proposal. A temporary copy of Gameplay uses near/far=-27/+27, halfWidth18 (972 square floor units,6x existing162). Character scale and width9 camera are unchanged. The test uses isolated TestProfile and density proof floor, disables encounter progression and attacks, then drives real HeroMovementInput.Hold through three waypoints. HeroMotion stays inside ArenaGeometry every frame. Current stamina remains active; no speed/budget changes. This is traversal measurement, not encounters, exploration rewards or device performance acceptance.

| Destination | Elapsed game seconds for this leg | Final floor position |
|---|---:|---|
| (10,0) from centre |6.78|(9.81,0.00)|
| (0,16) from previous |18.53|(0.10,15.84)|
| (-10,0) from previous |18.55|(-9.90,0.16)|

Tolerance0.2 floor units; elapsed includes actual frame stepping and existing stamina. Legs are sequential, not each starting with a fresh bar. Continuous steering is not an optimal rest/walk strategy. Source measurements: walk.csv. Enemy visuals are present from the density floor, but their combat/progression is disabled; do not treat these as balanced exploration encounters. Existing chest presentation can react to the walking hero; no persistent owner profile is used.

## Visual findings and limitations

Camera follows the hero across the larger surface without shrinking the actor. Existing painted platform material stretches with platform dimensions, so paving becomes oversized. This is a measured geometry experiment, not finished courtyard art; production needs tiled/modular ground with stable world texel/tile scale. No landmarks, local activation, boss approach, out-of-combat speed policy or random map generation is implemented.

Captures use an isolated graphics fixture with HUD canvases switched to ScreenSpaceCamera. That render setup does not prove physical-phone HUD occlusion or safe-area correctness; observe actual device when integrating. The camera is framed to reference portrait before each capture; motion data comes from real frame stepping. No phone performance or exact mobile memory claim.

## Reproduce

Tools/ArtReview/Run-ExplorationProof.ps1 creates a temporary ExplorationProof.unity copy with three uniquely checked YAML geometry replacements, stages ExplorationCapture.cs, runs the graphics PlayMode test and removes only its own temporary scene/script/metas in finally. Gameplay.unity and ProjectSettings are untouched. Initial attempt failed because normal SceneManager cannot load an unlisted scene; corrected fixture to EditorSceneManager.LoadSceneAsyncInPlayMode, avoiding build-profile changes. Final graphics test1/1 and static integrity485/568 passed. No production code changes, APK rebuild, installation or save changes; preceding full gate remains exit-seal380/395/127.

Next: stable-scale tiled ground proof and a small authored set of physically reached landmarks/encounter sites. Gameplay owner should decide encounter activation/completion, safe traversal versus combat stamina and boss-access conditions before implementing new rewards. Old region-map-01/OPUS_TASK.md is explicitly superseded; do not send it unchanged.

## 2026-09-23 — Wide-region coping and furnace source

PlatformMaterialMeshes keeps coping perpendicular floor width at the original hall value (0.45/sqrt(2)), with a conservative half-scale inset cap for tiny geometries. Coping/wall/join repetitions follow floor-space edge length instead of stretching across the region. Shared miter vertices and nine-mesh ownership remain. PlatformMaterialTests checks hall/wide width, texture density and corner joins. Graphics traversal refreshed and inspected. This is repeated material geometry, not separate placed wall prefab modules.

Verified: .NET380, compile clean, PlatformMaterialTests5/5, graphics traversal1/1. No full suite/APK/device change. Wide region remains temporary. New unchanged built-in image_gen furnace source and exact prompt in ArtDirection/2026-09-23/furnace-prop-01; RGBA1358x1159, transparent alpha, browser390px no overflow/all images loaded. Source only, not imported or placed. Next: sample/place reusable perimeter props, then live region integration with explicit local-encounter authority. No collision/elevation mechanic added.
