# Foundry stone craft

2026-10-13. Baseline `a8261795d2b4ff947c19e830b46cc92ef8baad23`.

Ordinary court quarry slabs now reach 3 by 2 design units with varied proportions, fewer joints and selected worn corners. Local mineral abrasion uses the existing bevel/face vertices and preserves world lighting and grain; no added crack overlay covers the court. The gate approach retains its previous smaller stock. The fitted central medallion and two processional fields retain their separate replacement domains.

Both outer gate buttresses now rise from a broad coursed base through a seated battered shoulder into the recessed upper bay. The shoulder ends at height 2.85; the original footprint and crown 7.35 remain. Mortar, dressed edges and faces partition the lower masonry surfaces instead of adding coplanar stone over a full hidden wall. Door, banners, inner jambs, ramp and actual support boundaries are retained.

The furnace pit replaces its repeated turned balusters with broad stone guard bays, recessed panels and sparse square iron stems. A continuous sill and substantial coping retain the original curved band and height. Actual openings remain visible above the sill. The existing posts, lining, fire, floor illumination, vessels and outer corner railing are unchanged; weak local reflected warmth describes pool-facing guard returns.

This combined visual package changes no walking outline, map dimensions, camera, scene, actors, encounter data, movement, economy or saves. No runtime texture, material, renderer or per-frame updater is added.

Three helpers owned quarry paving, gate buttresses and pit guards with disjoint source files. Root captured sixteen fresh Before views on unchanged baseline a826179 before authorizing edits. Gate and floor source reviews were crossed; root reviewed pit geometry and integration. Helpers reviewed their own resulting visuals and root separately inspected the combined actual views. Unity and Android operations ran only under root with frozen Assets.

The first EditMode preflight returned 560/561: one legacy parent-count assertion failed after the intentional increase in stone size, and three pit tests were missing because their new metadata GUID had 33 characters. Static verification caught the invalid GUID. Root retained the failed reports, corrected test metadata after Unity exited, and replaced the count proxy with actual 216.50 square-unit apron coverage (minimum authored core 168.60), no overlaps and connected receiving/stock tracks. Existing per-parent geometry, UV and 4,000 extra-vertex bounds remain; measured extra cost 669. All 564 EditMode cases then passed. The source peer review also corrected a localized abrasion SmoothStep misuse before Unity, with a 41-seed regression.

Twelve added EditMode cases cover broad contoured flags, abrasion localization, gate face ownership and bearing contacts, and pit guard bounds, real openings and post contacts. Existing court replacement ownership and full movement/scene suites remain in the final gate. Source hashes are retained. A stale short baseline label in the cloned capture's description was corrected; the launcher always pinned the correct HEAD. After captures were repeated with unchanged runtime/cameras, with that distinction recorded in validation/capture-label-correction.md.

The matched actual scene images pause combat and hide HUD. They establish Editor composition and measured mesh budgets, not physical-device performance or interactive acceptance.

Region 32 renderers/158653 vertices versus 161850 (-3197); distant geometry 3572 versus 3572. No new runtime texture, material, renderer or per-frame updater. Resource counts are not physical-device frame-time measurements.

## Verification

438 .NET / 564 EditMode / 159 PlayMode = 1,161 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed. PASS: 611 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 36,301,580 bytes; SHA256 27887CDF79CF6F5049ECC63BC103AAF0FAEA7B78C04C4E1FED98DA32646616A9; NOT INSTALLED.

Reference fidelity remains unfinished. This package changes visual mesh composition inside the accepted walking outline; camera, gameplay and encounter data stay unchanged. Existing immortal-crowd waypoint congestion remains unresolved; evidence remains in ../../2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md. Device motion/crowd/HUD/shimmer and midrange performance remain unverified. No ADB, installation or fresh phone observation. The earlier concrete restart/install decision remains pending; inspect current device state and preserve current original saves/telemetry before any later deployment. Never restore historical snapshots. Regional encounter/boss progression remains deferred under the art-first direction.

## Reproduction

Run with Unity closed from E:/MobileGame:

```powershell
dotnet test Tools/CombatChecks/CombatChecks.csproj --configuration Release
dotnet build Tools/UnityCompileCheck/UnityCompileCheck.csproj --nologo
pwsh -NoProfile -File Tools/ArtReview/Run-FoundryStoneCraftProof.ps1 -Phase after
pwsh -NoProfile -File Tools/Run-UnityTests.ps1
pwsh -NoProfile -File Tools/Verify-Project.ps1
```

Before images are fresh baseline captures taken before any runtime edits; the launcher pins that baseline and requires unchanged Assets. Sixteen paired views use identical camera and actor positions. These actual scene captures pause combat and hide HUD; they are not phone screenshots. The reference is concept art. Graphics cases are separate from suite totals. See validation/actual-image-review.md for visual acceptance and limitations.
