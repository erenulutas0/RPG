# Independent niche repair review

Date: 2026-10-14. Reviewer: gate_stonework_review. No Assets edits or Unity/device operation performed by this reviewer.

I reviewed `GateRecessOpeningPoint`, `GateRecessNextFloat`, their call sites and the new emitted-station assertions in the two existing production-course cases. I also read the retained failure diagnosis, numerical replay and targeted Unity result.

No blocking issue was found. The correction is confined to new intermediate vertices on the splayed frame's throat edge. The canonical endpoints return unchanged, and the same helper is used for the stone and mortar copies. The side test uses the original edge and an exterior frame point. When the nearest float interpolation lies inside, the selected coordinate step follows the edge's outward normal. The signed-float increment handles positive, negative and zero finite coordinates correctly. The two-coordinate fallback remains bounded to one representable step in each coordinate; it does not introduce an arbitrary world-space clearance.

The repair changes runtime geometry rather than weakening the failing test. The original visible-backing and strict overpaint assertions retain their 1e-8 bounds. Added assertions read the emitted mesh and require the stone-side half-plane, identical stone/mortar copies and the one-step coordinate bound. The previously unchanged backing, return and cap stations remain outside this repair.

As an independent numerical check, I read the preserved original throat polygons and replay report and evaluated all 48 proposed repaired stations using decimal arithmetic and polygon winding to determine the exterior half-plane. None entered the opening; all were within one float step per coordinate of their recorded nearest interpolation. This checks the reported numerical correction, not the full Unity renderer. The retained targeted Unity result separately reports all 24 recess cases passing, including the previously failing overlap checks.

After the final repaired-runtime recapture, I reopened gate detail, crown detail, portrait approach and full map. No new visible crack, halo, seam or change of niche depth appeared. The correction is visually imperceptible at these views. Final local visual acceptance is recorded in `actual-image-review.md`; the full regression/build gate remains independently required.
