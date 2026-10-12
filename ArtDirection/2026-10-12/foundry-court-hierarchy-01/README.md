# Foundry court hierarchy

2026-10-12. Baseline `2c2fa7c6e7c832febfa3930b8469c6fe69461762`.

The western receiving edge, transfer turntable and stock/trolley approach now share one connected flush working apron. Its broad repair flags replace existing packed parents, retain exact floor coverage and world lighting, and fade back to ordinary court stone before the pit-side passage. The two northwest scenic shafts have lower unequal crowns and flatter joined shoulders; their original feet and furnace sills remain fixed, six separated furnace panes fit the shorter bays, and the broken connecting span remains borne by the revised cores. Bridge and lower fortress masonry use fewer broader courses, locally scaled cool mineral grain, continuous spandrel/coping mapping and restrained contact shade. Existing walking outline, camera, pit/gate/bridge footprints, scene, encounters, gameplay and saves are unchanged.

Three helpers owned working-apron paving, scenic skyline and fortress/bridge masonry with disjoint source files. Root captured fourteen fresh Before views on unchanged baseline 2c2fa7c before authorizing runtime edits. Independent source reviews covered each slice; root alone ran Unity. Fourteen After views use identical camera/actor setup and are inspected directly. Seven additional EditMode cases guard production packed-floor coverage and cost, working-zone exclusions, lower arch seating, tapered/sloping fortress surface containment and grain, and both bridge-cap seam directions. Existing tests and renderer/distant budgets remain. Actual scene captures pause combat and hide HUD, so they do not establish physical-device acceptance. Initial static verification found a 33-character GUID on the new working-apron test metadata; Unity ignored that script and the initial EditMode run contained 517 cases, missing its two tests. Root stopped that verification run before its APK build, corrected the test-only GUID to valid unique 32-character form, and restarted complete verification. The initial incomplete XML and failed static report are retained. Runtime rendering source was unchanged by this correction.

Region 32 renderers/164217 vertices versus 166467 (-2250); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

425 .NET / 519 EditMode / 159 PlayMode = 1,103 distinct suite cases, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 605 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,263,938 bytes; SHA256 68C7867361911A55C3CAB46F01A8826B30EA2F3F725558EE8B1F46DD06033C8A; NOT INSTALLED.

Reference fidelity remains unfinished: the current walking outline is unchanged, and this package does not claim that material and skyline changes supply missing map width. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryCourtHierarchyProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Fourteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
