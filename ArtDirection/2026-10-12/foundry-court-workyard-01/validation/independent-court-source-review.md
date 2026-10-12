# Independent source review — court paving replacement

Read-only review of `FoundryRegionCourtPaving.cs`, `FoundryRegionCourtSurface.cs`, the local ring/field methods in `FoundryRegionFinish.cs`, the production hook in `FoundryRegionView.cs`, and `FoundryCourtCompassTests.cs`. Reviewed the final fast-path revision on 2026-10-12. No runtime or test execution by this reviewer.

## Result

No source blocker identified. Root's Unity results and actual image acceptance remain pending.

- The production hook runs after ordinary court paving, corner clipping and bridge deck emission, before the replacement ring/path. Subtraction considers every indexed old triangle, so mortar beds and bevels cannot survive beneath a replaced flag merely because they were emitted by a different helper.
- The three convex domains are disjoint: the exact existing 48-gon radius 5.05 around (-3,-5), and the same-axis full-width path rectangles starting at radius 5.21. The short intervening strip stays owned by ordinary paving. Rectangle endpoints and 4.18 width remain inside the agreed court/bridge walking union, with tests checking sampled triangle edges across concave transitions.
- Convex subtraction carries a candidate inside polygon through each half-plane and emits its outside fragment at that step. This yields disjoint residual fragments; original/intersection UV, colour and position data are preserved or linearly interpolated. It does not recompute or double-apply existing lighting. Original vertices are indexed lazily; unused cut vertices are discarded.
- The final fast path handles bounding-box and separating-edge misses without per-triangle polygon/list allocation. Boundary fragments still allocate bounded temporary polygons. This source property is not a device allocation or frame-time measurement.
- The replacement's eight inner fan polygons are convex because each closes on an octagonal keystone edge. The outer annulus uses four convex trapezoids per visual stone, preserving all 48 outer points. It avoids passing a concave annulus to a generic fan triangulator.
- Mortar, shoulder and face are disjoint regions. Internal annular subdivision sides have zero inset; both neighbouring pieces meet on the same radial line. Only true stone boundaries get joints and bevels, and each four-piece outer stone shares its material identity. No full-disc bed remains beneath the new ring or field.
- Replacement faces keep `PavingVertex`, continuous span-5.7 grain, half-unit floor-light clipping and alpha 1. The new family/tone treatment remains local; no material, global light response, camera, map or depth contract changes were found.
- Tests cover independent analytic replacement areas, point ownership, removal of two deliberately crossing source layers with interpolated attributes, and the actual production court plus bridge paving. The latter verifies removed triangles and unused vertices, unchanged outside ownership, and exactly one filled owner at sampled interior/path points. Existing full-domain/edge coverage remains relevant. No source-only test is treated as proof of final aesthetics.

## Image acceptance still required

At matched full-map and forecourt detail, the ring must read as broad radial masonry and the approach as fitted crosswise flags. Reject retained axis-grid ghosts, bright target-like discs, unexplained boundary cracks, or internal four-piece seams making 48 tiny annular blocks. Check the short ordinary-stone join around the ring, entrance/bridge merge and gate-direction termination. A valid partition alone does not establish a worthwhile visual gain.

## Final optimization delta reviewed

Reviewed the frozen revision before root's Unity preflight. `CourtFittedFace` now shares exact design positions only within a single fixed tone/mineral-family call. Since UV and `FloorLight` are deterministic functions of that exact position, attributes are identical inside that scope; separate mortar, shoulder and face calls cannot be welded together accidentally. The same 0.5-unit light lattice and clipped triangles remain, and only referenced vertices are emitted. The path course limit is now 3.2 rather than 2.3, reducing dense joints without changing either domain. Local 3,200/6,500/5,000 vertex guards remain unchanged.

The retired `CourtDressedStone` helper was removed. Its two existing reflection cases now call the active `CourtFittedFlag` path, retaining face-area, containment, finite-data, UV, alpha, colour and vertex-budget assertions. There are three newly added court cases plus two newly added crane cases in this package; these migrations add no test cases.

No blocker in this final delta. Assets were not edited by the reviewer. Root's actual mesh budgets, Unity results and image review remain authoritative for acceptance.

## Preflight failures and bounded repairs

Root's initial 11-case preflight passed eight and failed three. This supersedes any provisional assumption that source inspection alone established aperture/ownership acceptance. Original XML and source hashes were retained.

The two court failures were traced with actual triangle witnesses to samples exactly on the half-unit light grid at v=-5. Opposite-side triangles shared that edge; the test's inclusive predicate counted both boundaries. The angle starts now use 0.173 with the same radii, sample counts, tolerances, exact-one assertion and area guards. This is supported by `court-ownership-diagnostic.md`, not an unobserved relaxation.

A separate real integration overlap was found at the rotated gate-path end: its outer corner reaches u approximately 16.878, while the older gate border begins at u15.92. Root's local `ReplaceCourtPavingBase(stone)` call before the older gate stone batch emission removes that overlap using the same agreed domains. I reviewed the After-only capture guard that observes all actual paving renderer triangles at two medallion and two gate-tail interior points. It can catch a missing production hook that a helper-only test would miss. Final runtime results remain root-owned.
