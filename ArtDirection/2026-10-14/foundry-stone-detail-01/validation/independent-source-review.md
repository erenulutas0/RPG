# Independent source review

Reviewed the current stone-detail sources and focused tests against the stated baseline/scope. This reviewer made no Assets changes and ran no Unity, build, Git or device commands. After the refinements below, no source blocker remains for the bounded package. Final acceptance still requires the root's complete test results and matched image review.

## Physical mineral field

`StoneDetailGrain` changes UV sampling and subdivides original triangles at mirrored-field folds before batching. Original vertex positions/colours remain in their original slots; appended cut vertices interpolate colour. Original triangle groups retain painter order, including unrelated prefixes/suffixes. All target meshes are static; the explicit Flames/Cloth rejection prevents animation metadata from silently losing correspondence. There is no new material, texture, shader or frame update.

The integration scope is appropriate for the depth system: bridges and undercroft are below-floor batches, and raised service shelves already pass an explicit registered owner. It must not be generalized silently to an unscoped above-floor mesh, where subdivision could change the connected-component centroid used to infer ownership. Casting's two separate append ranges and shelf foundation/raised ranges keep existing content separate. Low-level `FortressMapGrain` remains unchanged.

Two observations were sent to the root before capture:

1. `UndercroftSpandrel` performs a detail pass inside `BuildFortressArchFace`, whose final pass repeats it for the whole bay. The same field largely hides the duplication, but only the final whole-parent call should subdivide. The inner helper can retain its existing simple mapping for standalone callers.
2. Shared edges are independently interpolated when adjacent original triangles traverse them in opposite directions. Float arithmetic can place their cut points a few ULPs apart. A standalone float32 arithmetic diagnostic confirmed this possibility; it is not a Unity test result or an observed visual crack. Reuse a canonical edge cut, and test reversed/shared edges plus near-fold endpoints. The current epsilon must bound UV error without serving as a reason to weaken surface-ownership checks.

`ShadeUndercroftDepth` currently runs after construction/subdivision, so new samples evaluate the nonlinear shade field instead of interpolating the pre-existing final shaded vertices. That is a material-light change even though subdivision itself interpolates colour; the root should either preserve prior final colour interpolation or explicitly review the small changed light distribution.

The new fold tests use actual helper output, negative and rotated fields, interior barycentric UV/light samples, total area, strict one-owner witnesses, original positions/colours, unrelated range/order preservation, bounded vertex growth and animation-metadata rejection. These are meaningful checks. Final production budgets and capture still matter: a synthetic vertex cap does not establish the complete region cost.

## Primary bridge spandrel

The helper replaces the old primary fill with three broad common-height courses, staggered blocks and explicit mortar partitions. It does not retain a flat fill below those new cells. Clip intersections use double interpolation with a shared exact cut coordinate; duplicate consecutive points and degenerate fan triangles are removed. The current fixed column interval covers the authored bridge width. Intrados, upper bearing and deck contacts remain unchanged; secondary bents retain their previous structural geometry.

The two new tests inspect all nonempty authored haunch faces and the complete production bent. They check original contour points, area, interior ownership, supported mortar contrast, and bed registration across wedge boundaries. No source blocker found in this slice. This is a fixed authored construction helper, not a general arbitrary-width wall API.

## Service-shaft piers

The two original pier rectangles per visible facade become indexed eight-vertex closed partitions: four sloping returns and one inset back. Their outer contour is identical; the back is inside the projected original rectangle. There is no original flat pier underneath. Existing arched opening, fire geometry, crowned head, station and foundation geometry remain separate and unchanged. The facade-wide existing mineral mapping continues over the new vertices.

Four new authored-dimension/orientation cases inspect production bay emission, exact outer/inner incidences, positive winding, exact projected area, dense one-owner witnesses, UV continuity and subordinate back value. These complement the existing complete shaft contact/opening tests. Source-derived +32 distant vertices is plausible; the root capture must establish the actual count. No source blocker found in this slice.

## Remaining acceptance

Do not infer visual success from partition tests. Reopen bridge-foot, service-shaft, stock, casting, full-map and entry/workyard portrait views. Confirm that finer grain is readable without mirrored chevrons or repeated bands, that broad mortar does not stripe the bridge, and that inset shafts stay secondary to the gate. This work addresses specific material/construction weaknesses; it does not close the reference's larger detail, composition and court-density gap.

## Follow-up review of root refinements

The nested spandrel subdivision is removed; only the complete bay and complete respond now call the detail field in the undercroft file. Cut intersections are cached by unordered edge indices, dimension and cut. A canonical position-based endpoint order also keeps separately authored coincident edges consistent without welding their separate colour ownership. Half-plane classification is exact; only the fold-loop upper bound retains a tiny numerical guard. Two focused shared-edge/near-fold cases now check unique cut vertices and positive partition area. I reopened the revised source and tests. These changes address the numerical findings above.

The existing undercroft shade function remains after construction. The root explicitly retains its finer sampling at new vertices as part of the material result. Therefore the final image review must not describe rendered lighting as bit-identical; only the function, original source vertex colours and its structural inputs are retained.

I inspected the archived first-focused XML and failed-test source: all five failures are in the newly rewritten support integration oracle. It counted every projected surface at a bearing witness, including pre-existing hidden/crossing returns. The replacement restores an integration check of the actual intended profile: apply the common grain pass to the isolated expected bearing, then require every exact resulting triangle once in the production support. This is appropriate in combination with the separate unchanged bearing-profile partition test, physical envelope/contact tests, and the independent new grain area/ownership/interior-UV tests. It should not be presented as a global no-overdraw proof for the complete support. No numeric tolerance was relaxed in this correction. The failed XML and source are retained under `first-focused/`; rerun/full-suite outcome belongs to the root's validation record.

The next focused run exposed a different issue: the local shelf vertex ceiling of 760 did not accommodate the new fold intersections. The root reports actual complete support counts of Salvage 958, Stock 933, Casting 925 and Crane 991, and deliberately raises the local ceiling to 1024. I reopened the new explicit test comment/ceiling; `second-focused/` retains the prior test/XML. This is an intentional geometry-budget increase, not a false-positive ownership oracle or a zero-cost texture edit. It is acceptable only within the unchanged complete-region 174000 and distant 3600 ceilings and after actual capture; distant vertices are included in the region total. A final full-region overflow requires optimization, not silently raising that global budget.
