# Foundry gate towers and stonework

Started and verified 2026-10-14. Baseline `e0bd846e4ba7ecc0f1ec3b8ca6da70847bcfb1d3`.

# Final scope: gate stonework

The package brings the existing gate's tall stone body, adjoining curtain walls and lower buttresses into a more coherent masonry family. It refines the current landmark; complete reference fidelity remains unfinished.

## Fitted niche shafts

The tall body and splayed frame use three broad stones per jamb, with two fitted stones on each pointed-head side. Narrow construction joints register across the body/frame contact. The actual opening, backing, returns, splay, outer contour and cap remain. An indexed local stone emitter keeps the previous face vertex allowance while adding real stone and mortar partitions. One mineral field crosses each complete physical face; existing directional recess shading still acts on the rendered frame.

## Adjacent curtain walls

The old complete base wall beneath coplanar ashlar is replaced by explicit stone, arris and half-joint owners. Broader courses and face-wide mineral grain remove the repeated per-stone atlas patch. Existing whole-face weathering, restrained colour variation, closed chamfer and exact cap V/U/C remain. Accepted dressed coping and shared helper behavior are untouched. The separately authored narrow `GateEnclosureRelief` decoration remains its existing overlay; the package does not remove every overlay from the entire curtain.

## Lower buttress material

Root's local finish changes only existing lower-wall vertex colours. A bounded neutral gain adds a restrained washed mineral body above the damp foot, using global elevation across adjoining courses. Existing forge hue, contact shadow, cap colours, geometry and UVs remain. The finish is applied once per complete face and adds no decorative mesh or general exposure change.

## Resources and preserved contracts

The actual After capture reports 34 region renderers and 161,161 vertices, versus 34 renderers and 163,073 vertices in Before: a net reduction of 1,912 vertices. Distant geometry remains 3,572 vertices. The existing 174,000-vertex region ceiling is unchanged.

The niche emitter has a separate index cost despite retaining its vertex allowance. Source accounting gives 240 triangles per recessed face versus 120 previously. Four existing shafts with two recessed faces each therefore add 960 niche triangles / 2,880 niche indices. This is the expected delta for that specific niche scope, not a measured net triangle/index delta for the complete region. Curtain partitions also change topology, and no whole-region index delta was measured.

No texture, material, renderer, shader, component, updater or resource ownership type is added. Map boundaries, camera, gameplay, movement, collision/navigation, encounters, progression and saves are unchanged. The static resource counts do not establish physical-device performance.

Twenty-one matched Before/After views use the same scene camera and actor setup, paused combat and hidden HUD. The After graphics capture passed. These are Android-target Editor images; no phone installation, ADB operation or device/save review was performed. Motion, shimmer, crowd readability, midrange performance and final reference quality remain separate acceptance work. The complete passing suite and APK results are recorded in verification.md; the APK has not been installed.

# Work and verification workflow

## Baseline and ownership

Root captured and sealed 21 fresh Before views at clean runtime Assets and baseline `e0bd846e4ba7ecc0f1ec3b8ca6da70847bcfb1d3` before releasing implementation. The matched set includes the gate-crown detail from the previous package. `baseline.json` records the evidence hashes.

Two helpers owned disjoint runtime/test files: recessed niche shafts in `FoundryRegionGateRecesses.cs`, and non-dressed curtain masonry in `FoundryRegionGateEnclosure.cs`. Root owned the lower-buttress mineral finish in `FoundryRegionGateButtress.cs`, integration and verification. A third helper independently reviewed source and actual images. Only root runs Unity and Android build tools; Assets remain frozen while those tools run.

## Source review and focused checks

Independent source review found no blocker in the body/frame joint registration, retained backing/cap stations, curtain ownership, inherited lighting or continuous grain. It explicitly distinguished the removed duplicate base ashlar wall from the unchanged curtain relief overlay. Shared helpers and the accepted dressed coping remain unchanged.

Two new production-profile niche cases inspect real matching body/frame joints, visible mortar, adjoining stone contrast and affine grain. Existing opening, coverage, splay, bearing and resource tests retain their contracts; semantic frame spans replace assumptions about the old emitter's vertex offsets. The indexed emitter retains vertex limits but increases niche triangles, as recorded in `final-scope.md`.

Five new curtain cases use actual production tiers. They check retained cap V/U/C, total surface area, strict interior ownership, continuous mineral grain and restrained inherited light colour. Boundary vertices use the previously justified one-ULP representation guard, with an explicit eight-ULP outward counterexample for each profile; this does not widen interior ownership or area tolerances.

## Frozen capture and final gate

Root recorded the final source freeze before the matched After capture and complete verification workflow. The After graphics test passed, producing all 21 views and measuring 34 region renderers / 161,161 vertices. Camera and actor staging match Before. These observations establish Editor composition and resource counts only.

The required verification sequence is pure .NET checks, isolated compilation, full Unity EditMode and PlayMode suites, a development APK only after passing suites, and static project integrity. Root is responsible for recording final suite counts, failure evidence if any, build status and APK hash. The final corrected source passed 438 pure checks, 612 EditMode cases and 159 PlayMode cases with zero failures or skips. The isolated compile had zero warnings/errors. The conditional Android development build and subsequent static integrity check passed; verification.md records the APK hash. The accepted final source freeze and fresh capture identify the source used for these results.

## Device boundary

No ADB, phone interaction, installation or profile/telemetry operation is part of this package. The prior concrete restart/install decision remains pending in authoritative device notes. A later deployment requires a fresh device-state check and preservation of the current original saves and telemetry; historical snapshots must not be restored as current state. Editor captures and static mesh counts are not device motion or frame-time evidence.

## Preserved failure and runtime correction

The first full run passed 610 of 612 EditMode cases and all 159 PlayMode cases; APK production was skipped. Two existing strict backing-area tests found small real float-rounded slivers at newly split throat edges. The runtime repair uses double interpolation and, only when needed, the adjacent representable stone-side coordinate, at most one float step per coordinate. Exact end stations and shared stone/mortar contacts remain; the strict overlap tolerance was not changed. The existing course cases now assert the emitted coordinate bound.

The actual focused Unity recess suite then passed all 24 cases. Root reran pure checks and isolated compilation, refreshed the source freeze, recaptured all 21 After views, and reran the entire suite/build workflow. See failure-analysis.md, niche-boundary-repair.md, repair-peer-review.md, first-suite/ and recess-repair-targeted.xml for the retained evidence. Targeted and graphics executions are separate from the 1,209 full-suite total.


Region 34 renderers/161161 vertices versus 34/163073 (-1912 vertices); distant geometry 3572 versus 3572. This package adds no runtime texture, material, renderer, updater component or per-frame work. The previous material package already owns the additional iron batches and focus-vector update; they are unchanged here. The indexed niche emitter keeps its vertex budget but adds an expected 960 triangles across the four shafts; no total region index delta was measured. Resource counts are not device frame-time measurements.

## Verification

438 .NET / 612 EditMode / 159 PlayMode = 1,209 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 619 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 EC908F08891544E2FE3939166ED5C327B2143721326A4ABA59C370AB2C55C639; NOT INSTALLED.

Reference fidelity remains unfinished. This package refines the recessed gate shafts, adjacent curtain masonry and lower buttress material finish. Camera, map bounds, movement, gameplay and encounters remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../../2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateStoneworkProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Twenty-one paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
