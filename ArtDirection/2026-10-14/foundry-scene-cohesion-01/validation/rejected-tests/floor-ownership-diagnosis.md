# Floor ownership test-oracle diagnosis and repair

The full candidate suite failed `FoundryCourtCompassTests.ProductionCourtAndBridgeRemoveTheOldGridBeforeTheirReplacementIsFilled` at the outside sample design (8.213, -13.149). The test counted three source owners before ReplaceCourtPavingBase and two afterwards. Runtime source was not changed to repair this failure.

## Bounded diagnostic

The original assertion was retained and augmented with before/after owner witnesses. Root ran the existing failing test once. `scene-floor-diagnostic.xml` preserves that failed diagnostic run.

The projected test point is (15.1052151, -1.74513948). The two real covering triangles are identical before and after carving:

- Before triangle 17726 / After 17398: (15.5563488, -2.12132025), (15.5563488, -1.41421354), (14.8492422, -1.767767). Double signed area is 0.49999981429834861. The point is within the triangle bounds and all three signed edge crosses are positive.
- Before triangle 17773 / After 17445: (15.5563488, -2.12132025), (15.603425, -1.43775165), (14.9017639, -1.79402781). Double signed area is 0.46286136385583632. The point is within the triangle bounds and all three signed edge crosses are positive.

The extra Before triangle 21428 has all three projected vertices equal to (18.3847752, 0.353553385), near design (13.499999, -12.499999). Its area is exactly zero, and the test point is outside its bounds. Every edge cross is zero, so the old InTriangle helper falsely counted this remote collapsed triangle as an owner of any point. This is not shared-edge aliasing or a real loss of outside floor coverage.

ClipFloorCorners can retain collapsed fan triangles. The existing CutCourtPaving AddOriginal/Add routines intentionally discard triangles with absolute double area at most 1e-10. The later carving therefore removes the collapsed triangle legitimately while preserving the two real owners.

## Test-only repair

The shared test helper now rejects absolute double area at most 1e-10 and rejects points outside the triangle's axis-aligned bounds before its signed-cross checks. The bounds use the existing 1e-7 tolerance. The original signed-cross tolerance remains exactly +/-1e-7; no tolerance was loosened, no assertion was skipped, and the production outside sample and equality requirement remain unchanged.

One regression case uses the exact production point and triangle coordinates. It requires the two real overlapping layers to remain counted, rejects the collapsed triangle even at its own coordinate, and checks an additional positive-area sliver whose old cross-only oracle falsely accepted a remote point. The real small triangle's interior remains accepted in both windings. This guards against fixing the failure by suppressing genuine opaque overlap or all small geometry.

Only FoundryCourtCompassTests.cs changed for this repair. Root owns focused/full-suite execution and recording the resulting pass/fail status. This diagnosis does not claim those subsequent runs have completed. The earlier actual-image acceptance remains a static art judgment; final source freeze/capture provenance must include the repaired tests as root requires.
