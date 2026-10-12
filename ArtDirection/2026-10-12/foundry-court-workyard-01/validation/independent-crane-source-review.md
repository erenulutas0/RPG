# Independent source review — live crane machinery

Reviewed the active crane-base draft and its two new EditMode cases, 2026-10-12. No source changes or runtime/test execution by this reviewer.

## Result

No source blocker identified. Actual-image acceptance remains required.

- The call in `FoundryRegionDressing.BuildCrane` is outside the baked/fallback conditional, so the actual Resources-backed crane receives the new assembly. The old fallback-only housing is not mistaken for the production path.
- `CraneMachineryFoot` is wholly inside the original scenic deck. Its eastern bound is -32.18 against walkable West=-32. The original crane depth footprint and 8.6 height are reused; no new navigation island or widened noncolliding obstacle is introduced.
- The sole starts at 1.12 on the existing cap. The rear casing reaches the hood lower edge at 2.21; the hood reaches 2.60. Original card, pivot, jib and chain endpoints remain fixed.
- The drum axis is inside both cheek bearings; flange top 2.12 is below the header underside 2.17. The final header front insets (+.15 in u and +.14 in v) sit inside the bevelled cheek caps. The rear header intersects the hood connection rather than floating below an unrelated box.
- Painter order is explicit: rear/hood, far flange, barrel and cable bands, near flange, cheeks and header. The assembly does not emit a solid front face across the intended aperture.
- The production mesh cases are meaningful: one checks the independent original deck prism, finite attributes, opaque alpha and actual sole contacts; the second compares isolated drum versus the complete casting using the last covering production triangle, demanding a substantial visible portion. They do not claim to verify the source image or scene draw ordering.

## Actual-image checks still required

Inspect fixed Before/After crane detail and whole-map for lower-mast seating, barrel/crate clearance, nearby lantern overlap, image/native material compatibility and the aperture's read. The projected prism and helper's last-visible test cannot prove that the baked card or another scene batch does not hide a feature. A recognizably supported low casting is the intended improvement; no change to the overall crane silhouette or map scale is claimed.

## Preflight aperture repair reviewed

The actual Unity preflight found only 31 of 338 sampled drum points visible. The original source review's pending visual guard therefore identified a real problem: physical clearance did not imply the correct projected aperture. The failure and diagnostic evidence are retained.

The bounded repair paints the farther (higher-u) cheek/gusset before the drum and the near bearing afterward. A raised central soffit at 2.40 replaces the rectangular low lintel; the top is 2.56, within the unchanged 2.60 limit. Its flat end seats remain at 2.17, with u+.15..+.34 lying inside the actual bevelled cheek cap's u+.135..+.355, and the front bearing v11.36 inside v11.345..12.065. The roof is supported at both ends and meets the rear assembly. The footprint, original mast/card anchors and both test thresholds remain unchanged.

No source blocker in the repaired geometry/order. The unchanged greater-than-35-percent actual visibility test and final scene images must establish acceptance; the owner's projected diagnostic prediction alone is not that result.
