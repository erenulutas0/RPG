# Foundry pit craft

2026-10-13. Baseline `e4f0611af0181de94ba83659a3a43b25e4937a37`.

The furnace pit guard now has broad fitted coping stones, restrained joints and selected worn arrises. The new faces partition the previous front and top planes, independently of the narrow panel and iron-stem cuts below. Grain unfolds over each stone edge. Actual guard openings, existing posts, contacts and the curved exterior envelope remain intact.

The two formed pit hoods now use a continuous angular cast-metal light treatment, coherent local grain and restrained crown soot. Upright plate courses receive a narrow seating partition; sloping shoulders remain broad between their dark straps. The exposed head cap shares its adjoining shoulder material, while the recessed mouth remains dark. The existing pressure/receiver profiles, collars, mouths and below-deck vessel art retain their geometry.

This package adds no runtime texture, material, renderer or per-frame updater. The new partial class separates bounded metal-surface generation from the existing hood assembly. Map outline, camera, scene, navigation, encounters, progression, economy and saves are unchanged. This is an incremental material-craft improvement; reference parity is not claimed.

Two implementation helpers owned disjoint pit guard and hood source/test files; a third helper reviewed the source and actual images independently. Root took sixteen fresh baseline captures at e4f0611af0181de94ba83659a3a43b25e4937a37 before runtime edits and alone ran Unity. Assets were frozen for each capture and for the final complete verification workflow.

Source review caught a coping-wear function whose peak initially had no emitted mesh sample. Selected stones now include a real peak station, and the regression test measures the emitted colour rather than only calling the helper. No geometry envelope or test budget was loosened.

The first candidate passed all 577 EditMode cases but failed visual review: the old flat navy head cap conflicted with the warmer worked-metal body. The rejected shaft image, source diff, new partial source and test report are retained under validation/initial-*. The correction uses the same cast-metal field at the cap's actual design positions, preserving the original fan and UVs. Two additional cases inspect actual shared shoulder/cap rim colours and the dark recessed mouth. Root then recaptured all sixteen After views with the corrected frozen source.

Ten added EditMode cases cover actual coping face coverage and grain, emitted wear, both production hood profiles' face ownership, silhouette, bounded noncollapsed UVs, shared facet light and exposed-cap/mouth colour separation. Existing movement, scene and resource tests remain in the full verification gate. Graphics capture cases are separate from the suite totals.

The matched scene images pause combat and hide HUD. They establish Editor composition and mesh resource cost, not physical-device motion or frame-time acceptance. No ADB operation or installation was performed.

Region 32 renderers/160334 vertices versus 159454 (+880); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 579 EditMode / 159 PlayMode = 1,176 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 613 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,306,068 bytes; SHA256 0D88970AA95AAB3FF0C2F75CB1A64C40C609C4ED6392BA302C68417EB6AF0B23; NOT INSTALLED.

Reference fidelity remains unfinished. This package refines stone and metal surfaces inside the existing pit landmark envelope; camera, gameplay and encounter data stay unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../../2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryPitCraftProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Sixteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
