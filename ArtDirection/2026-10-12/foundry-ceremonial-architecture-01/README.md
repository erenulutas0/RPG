# Foundry ceremonial architecture

2026-10-12. Baseline `7c0f057eb71ccc54c2141ad1b1c1215a34ade7cc`.

The ceremonial medallion moves from the court origin to design (-3,-5), inside the deeper entrance forecourt. Its fitted stone field grows to radius3.75 and its outer dressed setting to radius5.05; the compass arms retain their existing small dimensions. One restrained brass setting remains. Two paired segmented stone courses connect the entrance and gate direction to this single assembly. The former angular brass court box, its forced packing seams and the unit-flag square at the old centre are retired. The global polar helper and the western working turntable remain independent.

The two outer gate towers retain their footing and crown but replace the mid-height waist with a low battered plinth and one continuous tall recessed bay. Both inner jambs use a seated L-shaped stone section with a longitudinal rebate and a concave bearing cap. The existing painted door/banner/lock plane stays fixed; it is the masonry around it that changes.

Lower fortress stone now distinguishes broad primary responds and arches from quieter secondary projected recesses. The bridge's existing three support stations remain, with one primary mouth bent and two subordinate rear bents. The mouth's bearing course partitions its old spandrel instead of layering over it. Existing open intrados, support contacts and chain socket eligibility are retained.

This is a combined visual mesh package. The accepted walking outline, map size, camera, scene, actors, encounter data, movement solver, rewards and saves are unchanged. No new runtime texture, material, renderer or per-frame updater is introduced. It does not close the reference-fidelity or physical-device acceptance backlog.

Three helpers owned court composition, gate stone construction, and lower fortress/bridge hierarchy. Root captured sixteen fresh Before views on unchanged baseline7c0f057 before authorizing runtime edits. The camera and actor setup is shared by Before and After. Helpers reviewed neighbouring slices read-only; root reviewed integration, ran Unity and built the APK on frozen Assets.

Geometry checks cover the moved single medallion, continuous stone settings, world grain, retired guide seams, gate recess visibility, L-jamb concave caps, fortress hierarchy and bridge support openings. Existing movement and scene suites remain in the acceptance run. Source inspection caught a proposed bearing course overlay before capture; it was changed to partition the spandrel.

The retained actual screenshots pause combat and hide HUD. They establish the visible Editor composition at fixed cameras, not device performance or interactive acceptance. Resource totals are measured from the captured actual meshes. The final source and evidence hashes are retained for reproduction.

The first Unity import caught test-only access to internal FoundrySurface from a separate assembly. Reflection into the same inverse projection replaced the direct reference; production visibility was unchanged. The first full EditMode run then passed544/546, with both new jamb containment cases failing, while PlayMode passed159/159. Investigation distinguished a chamfered-hull oracle mismatch from a real, small square-corner overhang at the existing base bearing. The initial XML, gate image and source hashes are retained. The final jamb trims .04 design units from each edge. Both mirrored cases now check the actual chamfered base cap and former jamb envelope, without widening tolerances or dropping the concave-cap assertions. The new complete EditMode run passed546/546. Graphics were recaptured and visually reviewed after the repair.

Region 32 renderers/160781 vertices versus 161298 (-517); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 546 EditMode / 159 PlayMode = 1,143 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 606 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,268,279 bytes; SHA256 6EE8F295DA4D39B0908B119F2D2E0B1BFEE583A9EB83B085CDE28744037FF70B; NOT INSTALLED.

Reference fidelity remains unfinished. This package changes visual mesh composition inside the accepted walking outline; camera, gameplay and encounter data stay unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCeremonialArchitectureProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Sixteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
