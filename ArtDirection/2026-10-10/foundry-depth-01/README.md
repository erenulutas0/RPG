# Distant foundry works and quieter paving

Baseline: bd4af72. Five world-anchored industrial towers and one broken arched service span add an intermediate layer between the painted cavern and the playable island. Their crowns remain below local deck elevation and their footprints sit outside navigation. They introduce no routes, collisions, encounter rules or per-frame updater.

The first actual image review rejected the towers' strong brick contrast and square floating lower ends. The final version borrows the wall texture through one scene-owned material using the existing cavern shader, with an indigo atmospheric blend of .62. Upper construction stays opaque; a divided lower shaft fades into haze. Small furnace slits sample the existing lantern ember patch rather than magnifying dark coal crust. The original backdrop retains its .22 atmospheric blend. No texture is added.

Explicit ranks keep foundry sky/cavern at -22/-21, distant masonry/fire at -20/-19 and every existing island batch at -18 or above, including the deep smelting basin. Other backdrops retain their prior ranks. No bounds-based sorting ties are used. Both generated mesh bounds have .002 total expansion for culling roundoff relief. The owned material and generated meshes are released on scene unload; the shared materials and texture survive.

The previous contour pass exposed a strong mortar network. This package narrows nonceremonial flag setbacks and lifts their bed tone, retaining asymmetric cuts, light-cell clipping and protected regular flags. Walking geometry, camera, inlays, combat and saves stay unchanged.

## Actual visual evidence

Before images were captured from baseline source, not reconstructed by a toggle. After images render the final production implementation. Overview and full-map use wider review cameras; portraits use production camera width, staged actors and hidden HUD. These are actual Unity renders, not phone screenshots. Phase selects an output folder only; reproduce a baseline using the baseline source.

Root and independent review accepted the final floor and atmospheric correction. The distant silhouettes fill gaps while the lit platform remains the focus. Northwest architecture is still fairly large and simply shaped in the portrait; reference architectural richness and yard composition are not complete. Further work should prioritise purposeful structures rather than another isolated haze adjustment.

Region count: 29 to 31 renderers, 113,995 to 115,042 vertices (+1,047, approximately .92%). One owned material, no new texture or per-frame component. These counts are not a startup allocation or frame-time measurement. The unchanged boiler texture line in runtime-budget.txt is incidental capture metadata.

## Verification

PASS: 409 pure .NET, 446 EditMode and 152 PlayMode tests (1,007 distinct-suite tests; no failures or skips in the final gate). Compile check passed with 0 warnings/errors. Three focused region cases also passed and are included in full PlayMode, not counted twice. Android build exit 0; no C# warning/error or shader-error matches. Static integrity: 581 asset/folder GUIDs, 1099 package GUIDs, 568 scene objects/components; valid references/JSON. APK: 36,185,695 bytes; SHA256 357024F64DC7D9C040486CF85FE4B00978DBDE9273454E3C0894F68780B5D595. NOT INSTALLED. Exact evidence: validation/verification.md. One baseline and two successful after graphical captures; before and final-after XML retained. The first focused test attempt referenced disabled physics modules and failed compilation; its rendering-only component whitelist avoids adding physics dependencies. The next run found exact-bound rounding; a small actual culling bounds expansion corrected it without loosening the bounds assertion. One focused lifecycle/order test passed before the atmospheric refinement; the final full PlayMode covers the final version. The first full run passed 151/152 PlayMode cases: an older region cleanup test selected an arbitrary first child material and misclassified the new owned atmosphere. It now explicitly selects the borrowed paving renderer for that assertion; all existing cutaway/source cleanup checks remain, and the new test separately covers owned atmosphere cleanup. Three focused region cases and the final full gate passed after this test correction.

The in-app comparison gallery was checked: Before, After and Approved reference change the image and selected state; all displayed images load.

## Device and remaining work

NOT INSTALLED. No ADB action or fresh device observation this pass. The earlier concrete owner-run restart/install question remains unanswered; previous phone observations are historical. After approval take fresh preflight/backup, install the latest accumulated package, verify its hash and review the map and normal combat. Preserve current original saves and telemetry, remove only new diagnostic flags, and never restore stale saves.

Reference fidelity, richer working-yard composition, physical-device/crowd/midrange acceptance and first-ramp performance remain open. Region encounter/boss progression stays deferred under the owner's art-first direction.

## Reproduction

With Unity closed: `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryDepthProof.ps1 -Phase after`. Source: FoundryRegionDistantWorks.cs, the foundry-only VoidBackdropView rank reservation, FoundryCavern.shader atmospheric parameter, and small paving constants in FoundryRegionFinish/FoundryRegionFlagContours. New FoundryDistantWorksTests exercises real-scene ranks, camera-independent placement, geometry budget, material ownership and scene cleanup. Existing region/backdrop tests preserve non-distant ordering and normal backdrop behavior.
