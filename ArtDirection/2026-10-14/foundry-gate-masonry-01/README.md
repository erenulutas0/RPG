# Foundry gate and masonry craft

Started and verified 2026-10-14. Baseline `d34c56115fc0a771f648baf3601ab740a10b871d`.

# Final source scope

This package groups three changes to the existing closed gate and its stone architecture.

## Profiled header stone

The continuous lintel now has a 0.09-unit recessed middle fascia between connected sloping returns. Directional vertex tones describe the carved depth in the existing unlit renderer. The three visible faces share their octagonal stations and one mineral field per complete face. The original cap, coping, outer bearing stations, corbels, door and iron contacts remain in place. The shared `GateCraftTier` behaviour is preserved; only the first lintel in `BuildGateHeaderFinish` uses the new profile.

## Fitted enclosure coping

Dressed curtain, tower-capital, jamb-belt and landing-plinth tiers use broad fitted stones with joints registered across their fascia and bearing caps. The original visible surfaces are partitioned once, replacing the former complete fascia beneath overlaid stone faces. Coplanar arrises retain the existing bearing plane. One bounded mineral field spans each complete tier. New vertices sample the original lit shell, with neutral joint and edge gains; non-dressed wall ashlar retains its existing path.

## Clearer closed-door threshold

The procedural rail excludes its central units at the landing's closed-portal edge. The existing door facade physically backs that interval; removing the visual fence exposes the door sill and its approach. Both explicit brazier supports and the outer rail returns remain. The movement boundary, collision/navigation geometry and closed-door behaviour are unchanged. This is a presentation opening, not a newly traversable route or a boss transition.

## Runtime cost and preserved contracts

The final actual scene capture reports 34 region renderers and 163,073 vertices, versus 34 renderers and 162,361 vertices before: +712 static vertices for the combined package. Distant geometry remains 3,572 vertices. No new texture, material, renderer, shader, component, owned resource type or per-frame updater is added. The earlier cast-iron material arrangement remains unchanged.

Map boundaries, camera framing, actors, combat, encounters, progression, saves, movement and collision contracts are unchanged. The existing region vertex budget is retained. These resource counts are not device frame-time measurements.

## Evidence boundary

Twenty-one matched Before and twenty-one matched After views were captured in the Android-target Unity Editor with the same camera and actor setup, paused combat and hidden HUD. The new gate-crown detail view is present in both phases. The final graphics test passed, including the actual merged-scene threshold presentation oracle. Floor ownership probes remain in the capture fixture.

Focused new tests inspect header depth and surface ownership, exact bearing/cap retention, continuous mineral UVs and rendered facet contrast; enclosure tests inspect fitted surface coverage, registered joints and original-light inheritance. Existing PlayMode coverage now checks the physical door backing, clear presentation interval, threshold paving ownership and absence of regenerated centre posts. Full suite/build results belong to the package verification record and must be established before commit acceptance.

No ADB operation, phone installation, save/telemetry operation or physical-device review was performed for this package. Reference fidelity, device motion/shimmer/crowd review and midrange performance remain unfinished. This package improves the existing gate's stone construction and threshold legibility; it does not complete the approved reference.

# Work and verification workflow

## Baseline and ownership

Root captured and sealed twenty-one fresh Before views at clean Assets and baseline `d34c56115fc0a771f648baf3601ab740a10b871d` before implementation. The matched set adds a gate-crown view to examine the upper masonry. `baseline.json` records the source identity and evidence hashes.

Two implementation helpers owned disjoint code and focused test files: one the lintel profile in `FoundryRegionGateHeader.cs`, the other dressed enclosure masonry in `FoundryRegionGateEnclosure.cs`. Root owned the central threshold rail exclusion, merged-scene PlayMode assertions and capture fixture. A third helper independently reviews source and actual final images. Implementation helpers cross-reviewed each other's geometry and UV contracts. Root alone runs Unity and Android build tools.

Cross-review checked shared cornice corners and exact cap retention, fascia/cap joint registration, partitioned ownership and uninterrupted grain. It flagged a possible enclosure-light interpolation difference where new triangles cross old cap-fan boundaries; the focused actual-colour assertions remain part of verification rather than assuming acceptance. No gameplay or device work was delegated.

## Preserved initial compilation rejection

The initial isolated compile passed because it compiles project code and tests in one assembly. Unity correctly rejected the new cornice test's direct reference to the internal runtime `FoundrySurface` class across the separate EditMode assembly boundary (CS0122). No graphics case or full suite ran, and no APK was built from that source.

The repair changes the test oracle only: it uses public `FoundryMap.ToFloor` and the production lintel's documented 1.25-unit raised landing. Runtime geometry is unchanged. `first-compile/` preserves the failed Unity log and original source freeze. Its test snapshot was taken after the repair and is explicitly named `repaired-test.cs.txt`; it is not represented as the rejected source. The root source freeze was refreshed before recapture.

## First full suite and test-only repairs

The first full run passed603 of605 EditMode cases and all159 PlayMode cases. The APK was correctly skipped. The two failures were new test-oracle assumptions, diagnosed against actual production geometry; retained failure evidence is in `first-suite/`.

The header test assumed more than twelve rendered indices even for the narrow chamfer. That face intentionally has three quads, exactly twelve indices. Its area, ownership and shared-station checks had already passed. Only the preparatory count guard changed to >=12; all geometric and affine-UV assertions retain their thresholds. The passing production-prefix case measured396 old cornice vertices and820 new vertices (+424).

The coping test rejected float-rounded original boundary points with its dimensionless barycentric cutoff. Stored actual geometry and an independent reconstruction identified a maximum one-coordinate-ULP representation discrepancy, including the cap's existing inverse/projection roundtrip. Only emitted-boundary containment now accepts an original-edge intersection with the previous/next-float coordinate box; strict interior coverage, area, original corners, colour and UV checks remain. Every tested profile now also rejects a real eight-ULP outward counterexample. The header helper independently checked the failing profile using a convex hull and segment/rectangle intersection; all284 emitted vertices satisfied the bounded representation guard and the outward counterexample failed as required.

Runtime source remains unchanged through both repairs. Root refreshed the test source freeze and final capture evidence before rerunning the complete gate. `header-failure-analysis.md`, `coping-failure-analysis.md` and `test-repair-peer-review.md` record the diagnoses without representing the failed run as passing.

## Final capture and regression gate

Root reran pure tests and isolated compilation, then refreshed the full source freeze before the final capture and complete regression workflow. No assertion was removed, no runtime budget was raised, and no APK was produced from a failing suite.

The repaired frozen source produced all twenty-one matched After views and a passing graphics test. Actual region resources are 34 renderers / 163,073 vertices; distant geometry remains 3,572 vertices. The threshold presentation oracle passed on the merged scene. Root's final source freeze identifies the Assets used for these images and the subsequent verification run.

The required regression order is pure .NET tests, isolated compile, full Unity EditMode and PlayMode suites, conditional development APK build only after passing suites, then static project integrity. Final counts, APK hash and acceptance are recorded in the package verification summary when established. This workflow note does not assert that an in-progress suite or build has completed.

The image comparison uses actual Editor output, with unchanged camera/actor staging, paused combat and hidden HUD. It is not phone or gameplay-motion evidence. The independent image review owns visual acceptance and any remaining limitations.

## Device boundary

No ADB, phone interaction, installation or profile/telemetry mutation is included. The prior concrete restart/install decision remains pending in authoritative device notes. Any later deployment requires a fresh device-state check and preservation of current original saves and telemetry; historical snapshots must never be restored as current state.

Region 34 renderers/163073 vertices versus 34/162361 (+712 vertices); distant geometry 3572 versus 3572. This package adds no runtime texture, material, renderer, updater component or per-frame work. The previous material package already owns the additional iron batches and focus-vector update; they are unchanged here. Resource counts are not device frame-time measurements.

## Verification

438 .NET / 605 EditMode / 159 PlayMode = 1,202 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 618 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 5A7B64935F344A70089E8E7F304E1F105228C0E03C05ACF4B44521C62FE29C02; NOT INSTALLED.

Reference fidelity remains unfinished. This package refines the existing gate cornice, dressed enclosure tiers and central threshold rail. Camera, map bounds, movement, gameplay and encounters remain unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../../2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateMasonryProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Twenty-one paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
