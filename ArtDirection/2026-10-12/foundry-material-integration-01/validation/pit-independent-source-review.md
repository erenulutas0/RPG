# Independent pit source review

2026-10-12. Gate material helper reviewed the runtime diff in `FoundryRegionMassing.cs`, the complete new `FoundryRegionPitSurface.cs`, the existing five construction tests and the actual `FoundryPitLining.png` atlas. No runtime edits, compilation, Unity or device commands were performed during this review.

## Result

No blocking source defect found. Accept for root capture and required test gates, subject to actual-image review of the small repeating grain sample.

## Geometry and coverage

- All original profile stations and depth/relief points are retained. Each new strip interpolates the old low/high edges with identical parameters on the left and right. Strip boundaries coincide, including the exact top and bottom profile vertices.
- Each worn arris splits its half-face at an internal interpolation value. The two outputs meet at the same vertices and collectively cover the former whole quad; neither is an overlay or an outward extrusion.
- The bearing course similarly partitions each old quad into a narrow bed and its remaining stone. Width is measured from two distinct existing lining stations, so the bed remains an interior portion of each actual station chord.
- Neither wrapper changes backing-wall, pool, wash, furnace-bay, vessel, hood or depth-owner methods. Ribs still use stations 3/9/12/18 and terminate at depth 4.90, above the unchanged 5.13 molten seam. The bearing course still joins the original deck stations and depth .55.
- Original wall-under-rib overdraw remains. The new subdivisions add triangles but no additional projected surface layer. Exact arithmetic confirms bearing-course vertices 252 to 504 and rib vertices 256 to 672, a combined +668. Complete lining becomes 1,932, below the unchanged 2,124 guard.

## Material mapping and residual visual checks

- The iron sample x .047-.066 and y .615 to at most .6486 lies within the narrow cast border between the pictured rivet heads and brass straps. Its vertical scale is bounded by the .84-unit subdivisions. The worn arris splits this same UV interval, so it introduces no local texture discontinuity at its own split.
- The upper stone sample x .43-.63, starting at y .87, is inside cool upper masonry rather than the lower baked heat. Its short vertical extent follows depth/chord aspect. The bed/stone split shares the same UV boundary.
- Grain resets between height strips and between the two rib half-faces. This is a repeated low-contrast interior sample, **not globally seamless UVs**. It avoids the atlas's strong prepainted mortar/strap lines, but capture must still check for a repeated faint horizontal line or a vertical center discontinuity. Do not describe it as continuous world-space texture mapping.
- The unchanged heat function and explicit final alpha 1 preserve the emitted opaque surface contract. These source observations do not establish shader/device acceptance or visual reference parity.

Root should inspect `shaft-detail`, the full map and the pit-side actor portrait before acceptance. In particular, verify that brighter material does not flatten the rib returns, the under-rail course remains a stone bearing rather than a new rail, and the four ribs do not visually obscure the furnace bays.
