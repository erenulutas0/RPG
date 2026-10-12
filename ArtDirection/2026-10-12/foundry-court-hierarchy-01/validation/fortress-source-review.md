# Independent fortress and bridge source review

2026-10-12. Skyline helper read the new `FoundryRegionFortressSurface.cs`, its selected production call sites in `FoundryRegionUndercroft.cs` and `FoundryRegionBridgeSupports.cs`, and all four new `FoundryFortressSurfaceTests` cases. No code changes, compilation, Unity, ADB or Git operations were performed.

## Result

No blocking source defect found. Accept for root's complete gates and actual-image review.

- `FortressCoursedFace` retains the complete original outer quad as a joint underlay. Fitted stones use the existing bilinear face mapping with strictly inset course bounds. Fewer rows change the internal bond rather than the outside curtain/pier contour. Sloping or tapered originals remain covered, including their joints.
- The selected curtain, pier, arch and bridge paths now call the local material wrappers. Shared workshop/gate/upper-court helpers remain distinct. The actual changed production helper receives new tests; the tests do not rely only on the old helper's name.
- `FortressMapGrain` computes one orthogonal projected basis and one uniform scale across a face plus its dressed rims. Bounds restrict the complete sample to the quiet V4 stone interior. Equal projected positions within the same block therefore keep equal UVs; bevels cannot restart or stretch the grain independently.
- The spandrel applies a single mapping to all thirteen existing segments plus its pre-existing contact strip, so the geometry subdivision creates no new grain seams or extra overlay.
- Bridge parapet positions, owner outlines, height/bond cuts and cap bevel geometry remain. Wall/cap mappings derive from the three-unit block start, allowing grain to continue through the smaller depth-owner subdivisions. The new two-sided test compares actual emitted shared cap/bevel vertices across such an owner seam.
- Fewer fitted courses should lower geometry cost; no additional surface-generation pass or material/render resource was introduced. Root's capture must supply the actual total rather than treating expected savings as a measured result.

The new tests check exact original underlay, convex containment, positive triangle area, texture bounds, opaque colors, continuous isotropic grain on each dressed block and bridge owner-seam UV equality. These are meaningful regressions for the introduced helpers.

Actual imagery still needs to decide whether the lighter, broader masonry reads as joined fortress construction or as pale/plain bands. The repeated use of one quiet mineral patch is restrained by material content and local block boundaries; source math cannot rule out visible repetition or motion shimmer. No full-reference or physical-device acceptance is claimed here.
