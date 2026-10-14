# Foundry scene cohesion

Verified 2026-10-14. Baseline `8c09763e06076233d82f931684a43ecaa71e360b`.

Three coordinated scene changes: floor-only fire radii localize warmth to the pit collar, furnace fronts and lanterns while wall/iron source radii stay unchanged; shared paving grain uses a finer 4.3 span and quieter broad patina; supported billets and open casting flasks fill existing off-walkway shelf slots; lower fortress, bridge bearings and near-island curtains use a clean existing atlas field with restrained exposed-stone lift.

Region 36 renderers / 172735 vertices versus 36 / 169119: +3616 (2.14%). Distant 3572 is included and unchanged. Global ceilings remain 174000 / 3600, leaving 1265 / 28 vertices. Workyard stock adds exactly 700 authored vertices; adaptive floor sampling and fortress grain subdivision account for the remaining combined delta. No new runtime texture, material, shader, renderer batch or updater. The independently reproduced full floor is 45,335 → 46,096 (+761); subtracting it and the 700-vertex stock addition leaves 2,155 for the fortress package. This exceeds its provisional 1,600 allocation by 555; root retains the measured package within the unchanged global ceiling, using reserved combined headroom. No component timing was measured. Static counts do not establish device performance.

## Verification

438 .NET / 687 EditMode / 160 PlayMode = 1,285 accepted suite executions, zero failures/skips. Focused Foundry 285/285 passed separately. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 633 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 2D4BAB5DBE3300908BAF8D3CC850FD9B07AE588A32709787C634AE48B0110B45; NOT INSTALLED.

Two helpers implemented disjoint workyard and fortress scopes; a third independently reviewed source/images and helped diagnose a floor test oracle. Root integrated floor lighting/grain and verification. Initial isolated compile rejected an unsupported NonParallelizable attribute in the local test shim; removed without changing Unity execution policy. First full EditMode passed 679/686: five stale atlas range checks, one flask screen-projection oracle and one degenerate floor-owner oracle failed. The superseded PlayMode run was stopped and no APK built. The revised tests retain strict tolerances: flask validation measures all four real rail caps/areas and excludes cross-aperture fillers; floor ownership rejects zero-area/out-of-bounds triangles while preserving actual overlap and winding checks. Runtime source did not change during these test repairs. An independent keeper-overpaint concern was disproven by original-image pixel evidence and withdrawn; no runtime repair was justified. Rejected diagnostics/source remain under validation. Final frozen Assets stayed unchanged through accepted capture, full suites and build.

## Evidence boundary

Twenty-one fresh Before/After pairs use the same actual Android-target Unity scene, cameras and actor staging. Combat is paused and HUD hidden. The reference is concept art.

This is an incremental scene-cohesion improvement, not reference parity or a new layout. Warm bands are narrower but still present; paving remains visibly directional and native stone more regular than the reference. Storage has modest close-view benefit, with much of the under-bench geometry naturally occluded. The open centre remains readable; its RGB is not bit-identical because the patina midpoint rises about 2.1%. Map bounds, camera, navigation, equipment stations, gameplay and encounters are unchanged. Existing immortal-crowd waypoint congestion remains unresolved (ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md). No phone control, installation or save/telemetry change. Device motion, HUD, shimmer and midrange performance remain unverified. The prior concrete restart/install decision remains pending; later deployment must inspect current state and preserve current original saves/telemetry, never restore historical snapshots. Regional progression remains deferred under the art-first direction.

## Reproduction

Run from E:/MobileGame with Unity closed:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundrySceneCohesionProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before launcher pins the baseline and requires unchanged Assets. Final source and image hashes are under validation. Next work is scoped in validation/next-visual-priorities.md.
