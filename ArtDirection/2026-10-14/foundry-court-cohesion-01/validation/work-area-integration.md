# Work-area cluster integration

## Scope

The existing mould-bench island now stores two stacked open spare flasks and two substantial packing boards on its original receiving shelf. These replace two generic closed blocks; no item is added to walking ground. The three existing worktop planks use one directional timber field and a restrained warm material family. The working moulds, their sand/pattern geometry, clamps, shelf, trestles and worktop stations retain their original arrangement.

Steel on the bench uses a common unclamped field with subdued cool iron and inherited directional light. Original fire-facing warmth is retained as a bounded local bias. Timber and sand remain distinct, rather than tinting the entire station uniformly.

The existing crane winch casting joins the same cool iron family. Its lower area receives a broad oil-dark contact treatment and upper shoulders a restrained rubbed finish. This modifies only its vertex colours: the casting's geometry, shared UV field, exposed drum, upper crane anchors and original footprint are unchanged. It does not apply a uniform brightness increase.

Runtime ownership is `FoundryRegionProductionYard.cs` for the bench-only emitters/material finish and the `FinishCraneMachinery` block of `FoundryRegionCraneHousing.cs`. Trolley, turntable/rails, navigation, collision footprints, actor/camera contracts and all baked crane/stock sources remain unchanged. There is no new texture, material, renderer, component, resource owner or per-frame updater.

## Bounded cost

Eight flask-rail hulls and two packing-board hulls replace two old closed hulls. At 28 vertices per hull, the expected net increase is 224 static vertices; root's actual complete scene capture verifies the final delta. Crane vertex/index/UV counts are unchanged. The complete moulding station has an 1,800-vertex test ceiling and retains its original shared footprint and 1.55-unit depth envelope. The complete region's 174,000-vertex ceiling is unchanged. Static resource counts do not establish device performance.

## Validation

Existing equipment tests retain finite geometry, index, UV, alpha and shared projected envelope checks. A new case inspects the complete station for the actually emitted spare flasks and supported stack contacts; independently checks the spare flasks' real open centres; and samples rendered worktop corners for directional, noncollapsed timber grain and warm colour separation.

A new crane case exercises the finish on actual casting geometry under equal source light. It requires unchanged vertices, indices and UVs, a cool neutral iron response, no global exposure lift, and a visible broad difference between the lower contact and upper shoulder. Existing crane footprint, mineral field and exposed-drum tests remain intact.

The source is frozen for root-owned compilation, matched Editor capture and complete regression verification. The helper has not run Unity, ADB, Git or shared compilation. Matched yard, crane, stock and full-map views must establish whether the whole work pocket reads more coherently. This is a local cluster improvement, not reference parity or physical-device acceptance.
