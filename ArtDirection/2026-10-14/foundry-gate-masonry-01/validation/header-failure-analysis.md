# Header test failure analysis

## Observed failure

The first full EditMode report recorded `ActualCarvedFacesHaveOneOwnerAndContinuousGrainAcrossDepthReturns(2)` failing at its preparatory `indices.Count > 12` assertion: actual count 12. The corresponding side/front cases (0 and 1), exact cap/production-prefix case and rendered depth/grain case passed. Existing gate recess tests also passed.

The production-count output from the passing case is authoritative: old cornice 396 vertices, new cornice 820 vertices, delta +424. The combined scene capture remains 34 renderers / 163,073 vertices.

## Cause

Face 2 is the narrow closed chamfer. The unchanged shared `GateCraftCourses` path intentionally emits one quad for a closed chamfer instead of subdividing it into tiny ashlar. The new cornice has three connected depth bands, so that face correctly contributes three quads and twelve referenced vertex indices. These twelve indices are gathered from real nondegenerate triangles. The independent area, one-owner grid samples and required shared-station checks all ran before the failed count assertion and passed.

The test's strict greater-than comparison incorrectly assumed additional subdivision on every face. Extra subdivision is neither required for the physical profile nor for the following affine UV oracle. The latter only needs a noncollinear basis and checks every gathered rendered vertex; it already asserts that a valid basis is found.

## Proposed correction

After Unity exits, change the guard to `indices.Count >= 12`, with a comment documenting the three-quad narrow-corner case. Keep all geometric area tolerances, coverage and station assertions, noncollinear-basis requirement, and every per-vertex UV equality threshold unchanged. This admits the legitimate minimal three-band corner without removing any surface/texture correctness check. No runtime source change is justified by this failure.

No Assets edit or Unity operation was made while the root's PlayMode run remained active. The root preserves the failed report/source evidence and controls the later repair, source freeze, recapture and regression rerun.

## Repair applied

After the root confirmed Unity had exited (159 PlayMode cases passed; APK skipped because EditMode had failed), the count guard was changed to >=12 with the three-quad corner explanation. No runtime file, geometric tolerance, coverage/station assertion, affine UV oracle or test case count changed. The repaired source awaits the root-controlled fresh freeze and rerun.
