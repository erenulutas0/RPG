# Hand-quarried foundry flagstone

Baseline: 0b91ff8. This pass changes the actual silhouettes of nonceremonial floor stones. Each packed flag receives unequal corner setbacks and asymmetric quarry cuts; its shallow inward bevel follows that outline. The complete original joint bed remains underneath, so removed corners expose mortar rather than holes. Small infill, repair flags, ceremonial paving and flags crossing a ramp seam keep their regular geometry.

The face is clipped into the existing adaptive light cells, retaining continuous world UVs and projected surface elevation. The inset is bounded before a short corner edge can collapse. Old rectangular edge overlays are skipped on contoured flags. No new texture, material, renderer or per-frame component is introduced. Walking bounds, camera, brass, architecture, gameplay and profile rules remain unchanged.

## Evidence

The before/ images were captured from the baseline source, not reconstructed from the new code. The tool's Phase argument selects the output folder only. After/ images are generated from the final implementation. Portraits use the production camera width with staged actors and hidden HUD; full-map and overview are separate wider review cameras. These are actual Unity renders, not phone screenshots.

Root and independent image review accept the contour pass. The shape change is clearest in the yard and west-pit portrait: varied corner cuts and setbacks avoid identical repeated octagons. No holes, floating bevels or long triangular light wedges were observed. Hero and range ring remain readable. The newly exposed mortar network is stronger than before, particularly in the yard and gate ramp; large rectangular courses still remain obvious. This is a bounded improvement, not the reference final finish.

Region mesh budget: 29 renderers unchanged; 117,045 to 113,995 vertices (-3,050, about 2.6%). Counts do not establish faster frames. The texture line in runtime-budget.txt describes the unchanged FoundryShortBoiler resource, not a new paving texture. All resources are unchanged in this slice.

Focused contour tests: 14 passed. Full EditMode: 446 passed. Pure .NET: 409 passed. Compile check: 0 warnings/errors. Baseline and after graphics: one successful capture each. Full PlayMode: 151 passed. Distinct-suite total: 1,006 tests passed with no failures or skips; the focused 14 are included in EditMode and not double-counted. Android build exit0; no C# diagnostic matches. Final static integrity:579 asset/folder GUIDs,1099 package GUIDs and568 scene objects/components, valid references/JSON. APK36,180,484bytes, SHA256 AA3859B559D48F21E3971A2A4ACDF1B14E07890D92EC45A27867F419A6E559E0. NOT INSTALLED. Final full-suite XML is retained in validation/. Construction allocations and frame time were not measured.

Two initial Unity import/compile attempts failed before tests ran: the new production meta GUID had 33 characters, and the EditMode test directly referenced an internal runtime type unavailable across its assembly boundary. Corrected to a fresh valid GUID and reflection-based projection access before the successful checks. No runtime visibility was broadened and no assertions weakened. Focused tests validate known clipping areas, area conservation, positive convex contours/insets, protected flags, whole mortar coverage and raw mesh UV/elevation/triangle validity.

The comparison gallery was checked in the in-app browser: Before, After and Approved reference update the overview image and selected state correctly. These UI checks do not establish device acceptance.

## Remaining work and device status

This is a stone-surface improvement, not complete reference fidelity. Working-yard composition and purposeful equipment placement remain unfinished; raised obstacles require matching navigation/collision work. Full region encounter/boss progression remains deferred by the owner's art-first direction.

No phone action or new observation in this pass. The earlier owner-run restart/install decision is still pending. Last recorded phone package 021b95f and paused Room 2 / 74 HP / 8 unbanked gold are historical. After approval, take a fresh preflight and backup, preserve current original files and telemetry, and remove only newly added diagnostic flags. Never restore stale saves. Physical-device, sustained crowd, midrange and first-ramp performance acceptance remain open.

## Reproduction

With Unity closed, run `pwsh -NoProfile -File Tools/ArtReview/Run-FoundryFlagstoneProof.ps1 -Phase after`. Production source: FoundryRegionFlagContours.cs and the small AshlarFlagFace hook in FoundryRegionFinish.cs. FoundryFlagContourTests covers silhouette/inset validity, light-cell area partition, protected flags and production mesh surface coverage/UV/elevation.
