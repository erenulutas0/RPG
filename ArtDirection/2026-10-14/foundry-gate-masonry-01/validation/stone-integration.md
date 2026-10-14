# Fitted enclosure coping integration

## Scope

`FoundryRegionGateEnclosure.cs` now builds dressed enclosure tiers from a temporary copy of the existing lit shell. Two visible fascia planes, their small closed corner and the exact chamfered cap are replaced once. Broad fitted stones and narrow joints partition the original surfaces. The cap grid and fascia cuts share their actual upper-edge design coordinates. This affects the already-authored curtain coping, broad tower capitals, small jamb belts and landing plinths; non-dressed wall ashlar remains unchanged.

Cap arrises remain coplanar, so the existing bearing and next course do not move. Outer geometry, bay, crown, camera, walking geometry, material, renderer and update contracts remain unchanged. A single bounded mineral UV field covers all faces of each tier. Colours sample the previous shell illumination; the darker joint and arris factors are neutral scalars.

No new texture, material, renderer, component or per-frame work. Exact complete-region vertex delta is to be read from root's actual capture. The code replaces the old full fascia plus overlaid stone faces, and adds fitted cap partitions.

## Checks prepared

Five new EditMode cases in `FoundryGateCopingTests.cs`: three production-size profiles test exact source area/coverage, original outer corner retention, shared mineral coordinates and noncollapsed UV triangles; two actual cap/fascia witnesses inspect registered joints and inherited forge-light channel ratios. Tests use production `GateEnclosureTier`, not a detached visual helper.

Isolated compile completed with zero warnings/errors. Initial compile had a namespace qualifier typo in the new source; corrected before any Unity run. Unity suites/captures are root-owned and remain pending at this source handoff.

Cross-review by the header helper found no partition/registration source blocker. It identified that newly triangulated cap interiors can interpolate across old fan boundaries; exact hue witnesses will be checked in root's targeted run rather than assumed. The source samples original fan light at each emitted vertex. Source review of the separate header cornice found no source blocker; actual depth-readability still requires visual capture.

## Frozen files

- Assets/_Project/Scripts/UI/Arena/FoundryRegionGateEnclosure.cs
- Assets/_Project/Tests/EditMode/FoundryGateCopingTests.cs
- Assets/_Project/Tests/EditMode/FoundryGateCopingTests.cs.meta

No Unity, ADB or Git operation performed by this helper. No device acceptance is claimed.


## Containment oracle follow-up

The initial full EditMode run rejected float-rounded boundary vertices in the new broad coping case. Exact managed-source reconstruction and original-edge rounding witnesses identify the issue as a dimensionless barycentric boundary assumption. See `coping-failure-analysis.md`. After Unity closed, only the test was corrected with a one-ULP boundary representation guard and explicit eight-ULP outward rejection per profile; existing strict interior/area/UV assertions remain unchanged. Runtime source is unchanged. Fresh compile is clean; the independent managed predicate check accepts all real vertices and rejects all three outward examples. Root must still perform Unity verification.
