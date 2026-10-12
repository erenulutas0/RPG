# Court ownership sample correction

The first geometry preflight failed two newly added ownership checks: the isolated fitted court and the production court/bridge test each reported two owners instead of one. The unchanged area, complete coverage and local vertex-budget checks passed. `initial-geometry-failed.xml` preserves the initial failure.

The diagnostic rerun kept the exact samples and strict expected count while adding actual triangle witnesses. `diagnostic-geometry-failed.xml` proves both first samples lay on the half-unit light lattice at design v=-5: the isolated sample (-2.81,-5), and the production sample (-2.69,-5). In the isolated mesh the two reported triangles were:

- Triangle 130: (-2.5,-5.5), (-2.5,-5), (-3,-5).
- Triangle 143: (-2.5,-5), (-3,-4.5), (-3,-5).

They meet on the same horizontal edge and occupy opposite sides. The reported signed edge-cross values at the sample were approximately +/-8.0079e-9. The inclusive point-in-triangle predicate counted both closed triangle boundaries; this evidence does not indicate two area-covering floor layers.

The sampling sequence now starts at angle 0.173 radians instead of the cardinal angle zero. It retains the same radii, sample counts, golden-angle spacing, strict expected owner count of one, predicate tolerance and independent exact-area guards. This fixes the deterministic boundary sample; it does not loosen overlap acceptance. Witness formatting remains available only on failure. No runtime geometry was changed for this test correction. The corrected suite still requires root's rerun.

## New budget assertion corrected after measured evidence

The next preflight passed the ownership checks but failed the newly introduced production test's `raw base + 6,500` bound: raw base 38,562, final floor 47,305. The raw base excludes the baseline's existing medallion and paired processional courses, so this was not a valid before/after cost comparison. The unchanged isolated ring (6,500) and path (5,000) guards passed.

Root's actual After scene capture measured 161,850 region vertices versus 160,781 Before, a total increase of 1,069 for the combined court/crane/gate package, with 32 renderers unchanged. The established 174,000 global ceiling is unchanged. This is the relevant measured package comparison.

The new production case now logs raw base, clipped base, ring, paths and final counts separately. It bounds clipping overhead at 2,000 vertices for the three fixed boundaries, retains the independent inherited ring/path caps, and limits the complete floor to raw base plus those two component envelopes. This replaces the mistaken single-component premise, not the existing scene budget. Root's final test rerun remains required.
