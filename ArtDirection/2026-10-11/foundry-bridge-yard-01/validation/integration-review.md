# Independent bridge/workyard source review

2026-10-11. Baseline `b91ac1a6cfce5cb7be087b68387b6128daf46f6e`. Reviewer: pit_wall_proof helper. Inspected the bridge-only changes in Site/View, BridgeSupports, ProductionYard, their projection/depth helpers and new EditMode tests. No Unity, ADB, Assets or Git mutation was performed by this reviewer.

## Source assessment

No unresolved source blocker found in the frozen package. The subsequent actual Unity image review accepted the bounded package; complete bridge-edge movement and physical-device acceptance remain open.

- New bridge parapets start at the existing outer walking edge, extend outboard and use the same court clip as paving. Both asymmetric court seams are preserved. The old side coping is removed where the new connected parapets take over; the physical far end retains its existing low cap.
- Short explicit depth owners are inscribed within each outboard panel instead of using a long rectangle that would claim the diagonal passage. The source keeps the original lane width and court join. Architecture tests inspect the actual clipped outline, owners and emitted cap, rather than a decorative vertex count.
- Parapets step down around existing keeper consoles; the original seating slab and statue foot remain exposed. The source includes a corresponding socket-clearance regression.
- Arch openings and piers retain their geometry and existing below-deck ownership while the incoming faces receive fitted courses and dressed edges. Original ranks/material resources remain. Fan caps remove their unused degenerate triangle.
- Trolley bearings intersect the axle/deck, yoke legs and gussets support their crowns, and varying billet ends still cross the existing band stations. Added equipment stays within the shared blocked islands and the existing 1.55 depth scope.
- The initial trolley edit emitted both broader yokes after the cargo. Review flagged the far yoke as a painter-order risk. Before capture, the author moved the far casting ahead of the cargo while retaining the near casting after it; the frozen source has the expected order.
- The mould shelf overlaps its supports, trestle gussets meet the worktop, and both recessed patterns remain below their flask rails. Existing clamps retain the maximum 1.54 emitted height. The two equipment tests check the real generated meshes against the shared projected footprint and owner height.
- No navigation, scene, camera, gameplay, save format, new material resource or runtime updater is added in this reviewed package. Resource growth and image readability still require root's capture and verification.

This source review does not establish physical-device performance, animated cutaway behavior or final reference fidelity. Root owns test execution and APK verification.

## Final cap refinement

The final cap pass retains the clipped panel envelope and shared cap-height function for edge faces and subdivided cap surfaces. Cuts at the bevel boundaries and shallow three-unit joints keep adjacent surfaces attached. The shallow joint profile preserves the monotone projected surface; updated architecture checks inspect generated cap triangles and socket clearance. `GateStonePatch` forces emitted alpha to 1, so the `tone * faceLight` expression in the edge helper does not introduce transparency. No source blocker was found in this final refinement. The final three bridge images were also accepted as recorded in actual-art-review.md.
