# Gate and ceremonial court — matched Unity proof

Baseline: `f76b8f3`. Three coordinated visual changes: a supported stepped gate crown and continuous buttress piers; one integrated stone court medallion; segmented dressed stone around the gate approach. The approved reference is `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Implementation

`FoundryRegionArchitecture.cs` and `FoundryRegionGateHeader.cs` replace the shallow gable and repeated pier belts with a stepped crown, corbels, continuous ashlar shafts, tall stone-backed blind niches and deep inner jamb returns. Original facade, banners, door boundary, crown height envelope and depth owner remain. Existing horizontal cornice straps remain attached to the same supporting ledge.

`FoundryRegionHeraldry.cs` and the ring methods in `FoundryRegionFinish.cs` remove the distant radius-8.6 bullseye and long spokes. The existing radius-3.97 inset now has broad radial stone divisions, a smaller compass star and one restrained brass perimeter. Four obsolete forced paving seams were removed with the deleted spokes. Angular court guides remain.

The gate approach keeps its original extent. A flush segmented stone border replaces the double gold frame; the approach emblem has one ring and shorter centre ties. Stone faces reuse existing clipping, floor projection, world UVs and lighting on a half-unit lattice, including exact ramp grade breaks. No new material, texture, renderer, per-frame component, navigation, camera, combat, scene or save change.

## Evidence and review

The before capture uses the baseline runtime, not a synthetic reconstruction. Both phases use the same capture source and cameras. Gate/court details, overview/full map and production-width gate/court portraits are available in the gallery. Portraits stage the hero on walkable points and hide the HUD. Images are actual Unity captures, not device evidence.

The first visual review accepted the stronger gate structure and quiet court medallion, but found that the new approach border disappeared into the surrounding paving. The revised border has a narrow mortar bed, inset stone faces and clearer joints. The second review exposed overlapping corner strips. Shared mitered inner/outer vertices and bed-before-face ordering now remove those overlaps. First/second review images and graphics XML are retained in validation. Final independent review and full gate status are recorded in validation/verification.md.

Region: 32 renderers unchanged; 116,326 to 115,944 vertices (-382/about 0.33%). These resource counts do not establish frame-time or allocation performance. The full reference's texture richness, finer material wear and broader architectural variety remain incomplete.

## Verification and device

409 .NET tests and the final compile check (0 warnings/errors) passed. Baseline and three after graphics captures passed. Final gate: 446 EditMode and 154 PlayMode passed (1,009 distinct cases including .NET); Android build exit 0. Static integrity passed 584/1099/568. Final root and independent image review accepted; APK identity and limits are in validation/verification.md. No phone installation has occurred. Previous owner-run restart/install decision remains pending; take a fresh device preflight and backup on authorization, never restore historical saves.

## Reproduction

With Unity closed: `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryGateCourtProof.ps1 -Phase after`. Phase only selects the output folder; it does not restore historical source. Capture source: `Tools/ArtReview/FoundryGateCourtCapture.cs`. The temporary Assets test is removed by the runner's finally block.
