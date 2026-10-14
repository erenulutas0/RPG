# Next visual priorities for Claude

Basis: the final refreshed `foundry-inner-piers-01/after` gate, crown, full-map, approach and yard captures, compared with the approved `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`. The inner-pier package is accepted as a bounded improvement. The remaining scene-level gap is material and construction consistency across complete visible objects; more hidden curtain detailing or isolated trim will not close it.

## 1. Complete one remaining visible native workyard object

Start with the active trolley and its load/support assembly, choosing its strongest existing close view before implementation. The latest yard still combines carefully shaded baked equipment with simpler native blue-grey box and frame shapes. The recently rebuilt moulding bench has real cavities and clearer support, so avoid spending the next package on another tiny bench ornament.

Inspect `Assets/_Project/Scripts/UI/Arena/FoundryRegionProductionYard.cs`, especially `BuildYardTrolley`, `YardBenchIronFinish` and `YardBenchTimberFinish`. Improve the complete object's cast-iron thickness, timber bearing, wheel/axle/frame contact and restrained material response together. Preserve its footprint, load placement, rail relationship and real open spaces. Reuse existing batches/resources and prefer replacement or simplification over extra detail. Do not accidentally improve an inactive native fallback behind a baked object.

Acceptance needs a substantial difference in the complete object at its native approach/yard camera, with the load visibly supported and the frame still readable. Check the surrounding route and full-map grouping. Reject a change that only adds tiny highlights, fills an open cavity, floats hardware or is hidden by the existing bake.

## 2. Resolve a broad, exposed native stone/material mismatch as one study

Gate-detail and crown still show mottled native stone against smoother baked outer bays and the more crafted portal. The long exposed header/crown is a more useful study area than the mostly hidden upper curtain. First determine whether the mismatch is dominated by atlas sampling, value contrast, face lighting or repeated subdivision; do not assume that more geometry or globally finer grain will improve it.

Inspect the active `FoundryRegionGateHeader.cs`, `FoundryRegionGateSurface.cs` and their integration in `FoundryRegionArchitecture.cs`. The new `FoundryRegionGatePierFinish.cs` deliberately preserves a separate .11 pier field; header, returns and cartouche retain the shared field. Maintain that ownership and original painter order. Keep the accepted pier/outer-bay massing, portal opening, roof/cap interfaces, iron contact and lighting hierarchy. A shared-material change requires checking every other consumer, not only the gate.

Accept only if a broad visible area becomes calmer and more consistent with the baked masonry in both gate-detail and full-map, while retaining face separation and the door/banners' focus. Reject cloudy contrast, new repetitive diagonal grain, uniformly flat stone, brighter attention-stealing crown planes or another hidden-field change presented as a major gain. If the study cannot show that improvement within existing resources and budget, retain the current finish and move on.

## Constraints and later verification

Current measured region budget is 36 renderers / 171,884 vertices, including 3,572 distant vertices: only 2,116 total and 28 distant vertices remain below the respective 174,000 / 3,600 ceilings. Budget headroom is a limit, not a target. Preserve V8 paving, the quiet combat floor, navigation/collision, portal depth, source lighting, resource lifetime and existing gameplay. Keep the approved layout; do not fill walking space to imitate the reference's visual richness.

Seal matched Before images, freeze actual production source, then capture After and inspect the complete scene. Preserve failed diagnostics and run the meaningful geometry/depth/ownership and project checks required by the changed contracts. Triangle counts or test passes do not establish visible improvement or phone performance.

Phone installation and a new device run remain pending explicit authorization and current-save protection; this document does not authorize them. Phone readability/performance and the documented stationary-crowd convergence limit remain open verification issues. Do not label screenshot acceptance, a moving-hero crowd case, or an uninstalled APK as resolving them.
