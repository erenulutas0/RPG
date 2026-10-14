# Coping containment failure: float boundary oracle

## Failure

Full EditMode: one of the five new coping cases failed before the later checks. `DressedEnclosurePartitionsOriginalWallsAndBearingWithoutHiddenLayers(3.96,3.56,6.55,6.82,0.045)` rejected an emitted vertex through the generic `Cover` triangle predicate. The other four cases passed, including both exact joint colour witnesses. No test tolerance has been changed and no Assets file was edited during this investigation.

## Reproduction

A standalone .NET 9 diagnostic loads the already compiled `UnityCompileCheck` assembly and invokes the actual private `LitFoundryTier` and `GateEnclosureTier` methods with the failing test's float inputs. This does not launch Unity or edit Assets. Output: original shell44 vertices; candidate284. Actual V/T arrays are retained in `coping-diagnostic/shell.json` and `actual.json`; the reflection program is retained as `Program.cs.txt`. This is a managed geometry diagnostic, not a substitute for root's Unity test.

The first rejected vertex is index5, repeated as8 and20: `(24.697118759155273, 15.48165512084961)`. Against original triangle0 it has smallest barycentric weight `-1.0067358284238104e-6`, failing the `-1e-7` dimensionless cutoff. Its closest point on original lower fascia edge0-1 is only `2.3141338282224767e-7` world units away. Component errors are0.0542592 and0.2170368 float ULPs respectively. An exact point on that original edge is `(24.697118455303283,15.481655272775361)`: converting both coordinates to float produces the emitted vertex bit-for-bit. Thus exact float rounding of a correct boundary point fails the original oracle.

Across the actual284 vertices,21 fail the original barycentric cutoff.19 have exact original-edge intersections with their half-ULP rounding cells. The retained JSON includes every index and that witness. Two duplicated cap vertices273/275 fall just outside a half-ULP cell after the existing inverse/projection sequence. Their emitted point is `(24.85386085510254,17.503276824951172)`. Tracing production clipping gives original design input `(30.914999,-4.2336655)`, exactly on the recovered cap's max-U boundary30.914999. Both original boundary endpoints also recover max-U30.914999. Reprojection moves this point approximately1.10e-6 world units from the old stored float edge, less than one coordinate ULP; there is no design-plane overshoot. This extra roundtrip explains why those two are not covered by a single half-ULP conversion argument.

## Proposed correction

Keep runtime geometry unchanged. Keep the existing area assertion, exact silhouette-corner witness, noncollapsed UV checks, all interior single-owner samples and their original barycentric thresholds. Correct only the emitted-boundary-vertex oracle: accept strict original coverage, or an original shell edge intersecting the coordinate box between each component's immediately previous and next representable float. This is a scale-derived one-ULP representation guard, not an arbitrary larger barycentric epsilon. Require the segment intersection parameter to lie within[0,1].

Add a counterexample for every production profile: a real outer cap edge midpoint displaced outward by8 ULPs must be rejected by the same containment predicate. This prevents the boundary correction from hiding an actual envelope expansion. Report failure details with vertex index and roundtrip coordinates. The root must rerun the targeted class and complete suites. This proposal has not yet been applied at this note's creation because Unity remains in its PlayMode run.


## Applied after Unity closed

Root authorized a test-only correction after the failed full verification finished (APK skipped). `ContainedAtFloatPrecision` first retains strict original triangle coverage; only a rejected emitted vertex may use exact double segment clipping against the previous/next-representable-float coordinate box. Interior coverage samples still call the original unchanged `Cover`, and area/UV/corner assertions remain unchanged. Each of the three profiles also now rejects an outer cap midpoint displaced outward by eight ULPs. No runtime file changed and no case count changed.

A fresh compile completed with zero warnings/errors. The standalone diagnostic invoked the actual revised production/test predicate through reflection for all three profiles:284,96 and96 emitted vertices respectively, zero rejected actual vertices; all three8-ULP outward counterexamples rejected. `coping-diagnostic/final-boundary.json` records those results. This proves the revised managed oracle's distinction, but Unity rerun remains required.
