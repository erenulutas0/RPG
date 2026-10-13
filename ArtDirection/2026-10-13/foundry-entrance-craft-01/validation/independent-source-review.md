# Independent entrance-craft source review - 2026-10-13

Read-only peer review. No Assets, Unity, ADB or git changes were performed by this reviewer.

## Bridge continuation

The new CourtBridgeContour uses the exact existing approach cap corners returned by CourtPathContour(CourtProcessionalEnds[0]). Its far edge lies at BridgeStart plus the existing kerb allowance, with a 4.18-unit physical stone-field width. The four vertices retain a convex counter-clockwise footprint and a mild taper. The broad transverse courses and alternating fitted pairs remain sufficiently wide for the existing 0.028 joint and additional 0.052 shoulder inset; the change does not introduce an acute clipped end stock.

ReplaceCourtPavingBase subtracts the added quadrilateral from the complete built floor after the old bridge bed, bevels and faces have been emitted. It then emits the medallion and original paths, followed by one active BuildCourtBridgeStone call. This is a replacement domain, not a second parallel floor overlay. The original path and new domain meet on the same cap and occupy opposite sides of it. Gate heraldry's existing call to the shared subtraction is harmless for the distant bridge domain, which does not intersect the gate surface.

CourtFittedFlag and CourtFittedFace retain the established mortar/shoulder/interior partition. Their half-unit lighting lattice and world-coordinate paving UV mapping are unchanged. The new route does not introduce a per-stone UV reset or replace the existing forge-light field.

The new tests check actual emitted area, world UVs, noncollapsed visible UV triangles, footprint bounds, physical floor containment and off-edge single-owner samples. The exact shared-cap test inspects both sides of the original route join. The existing two-layer subtraction and real packed floor tests now cover the new bridge domain. Original clipping allowance 2000, ring allowance 6500 and route allowance 5000 remain; the new bridge domain has a separate fixed 3000 limit. The unchanged whole-region 174000 limit remains the independent total guard.

No source correctness finding was identified in this route review. Actual-image review still needs to confirm the bridge threshold, oblique cap, lighting continuity and absence of an artificial strip appearance.

## Court mineral and worked shoulder

The paving changes are on the active contour path. AshlarFlagFace passes its mineral family to ContouredFlagFace; the change does not depend on the older rectangular wear helpers that this path bypasses. The additional mineral-bed factor is sampled per stone identity, while FloorLight remains evaluated at the existing world positions. Ordinary open-court mortar now shares the face mineral tint and a restrained 0.90 value factor. Kept stock, repair stock and inset contrast retain their prior branches.

Selected shoulder wear is limited to one quarter of eligible flags and requires the existing wear-exposure field. ContouredFlagFace adds the actual along-edge peak and cross-edge shoulder/fade sampling lines to its clipped face partition. These are referenced face vertices, not helper-only peaks or a new coplanar layer. The outer contour and its valid geometric inset remain unchanged. QuarrySampleLines skips outside samples, deduplicates near-equal lines and sorts the resulting partition before emission.

The final tests inspect actual emitted materials after factoring out world light, bounded within-stone and between-stone variation, mortar/body ratios, contour bounds and alpha. The strengthened worked-shoulder check requires its brighter interior samples to be referenced by nondegenerate triangles; the previous possibility of counting an unused mesh vertex is closed. It also checks the restrained 1.015-1.0451 shoulder range and the per-flag 240-vertex limit. No production correctness issue was identified. Test execution remains the integrating agent's responsibility; this review did not run Unity.
