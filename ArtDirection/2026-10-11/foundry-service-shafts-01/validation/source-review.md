# Independent service-shaft source review

2026-10-11. Reviewer: pit_wall_proof helper. Baseline `4ac6665`. Read the frozen FoundryRegionDistantWorks.cs diff, shared face-edge/projection helpers, existing distant-works tests, baseline skyline image and approved foundry reference. No Assets or production-source changes, Unity execution or phone interaction were performed by this reviewer.

## Source assessment

No unresolved geometry, contact or painter-order blocker found. Actual Unity image acceptance remains pending.

- The hollow exhaust emits a low throat floor, inner walls, near outer faces and four separate rim strips in that order. It does not add a full top cap. An independent analytic projection check sampled 361 interior opening points per variant: all remained covered by an inner wall or throat floor after painter ordering, never the outer face/rim. This checks the source geometry, not a rendered mesh; details and limits are in source-geometry-review.json.
- The necks narrow within the original shoulder envelope. Buttress heads shift inward with them: their 0.30-wide head straddles each neck edge, retaining 0.15 lateral contact. Variant 1 meets the upright neck; variant 2 meets the final sloping shoulder immediately below the neck. The joined raking courses, neck faces and crown bearing share their endpoints. No horizontal cap is left crossing the hollow throat.
- Neck panels use a recessed backing with side, sill and header returns. The former complete wall is absent beneath each recessed panel. Coplanar GateStoneEdges subdivision retains each face boundary; it does not expand the scenic silhouette or add a coincident overlay.
- The original tower centers, foundation/shaft geometry and overall design envelope remain. Variant 1 crown ends at height -2; variant 2 ends at -4.4, matching its former low hood maximum. New neck/crown geometry is inside the old shoulder/buttress footprint. Other tower variants and the broken background span are unchanged.
- Fire pane generation, borrowed fire material, owned atmospheric masonry material, atmosphere 0.44, emit ranks and final lower-foot fade are unchanged. The final fade applies to the new vertices too. No actor-depth owner, navigation route, gameplay component, texture or updater is introduced.

The existing 2,500-vertex combined-background test cap would be stale for this candidate. Root explicitly plans to measure the actual capture, then decide acceptance against a deliberate 3,600 cap; source author's estimate is 3,572. This review does not certify either count or waive the existing gate. Root owns test execution and any documented budget decision.

The reference still has substantially richer architectural/material integration. Source correctness does not establish whether the tower is less dominant beside the crane/casting equipment; that requires paired actual images at full-map and actor height. Physical-device motion, shimmer, crowds and performance remain unverified.
