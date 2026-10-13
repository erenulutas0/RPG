# Independent landmark-craft source review - 2026-10-13

No production correctness finding was identified in the reviewed source. This is a read-only peer review; the reviewer ran no Unity, ADB or build commands and changed no Assets.

## Gate frame

GateRecessedFace replaces the inner edges of the existing five frame pieces with a throat at one quarter of the existing inward depth. The matching front edges of all five depth returns use the same stations. The backing remains at its original depth, and the exterior frame, shaft envelope, retained cap and bearings stay in place. No second flat frame is emitted underneath the splay. The isolated face retains its 240-vertex count.

The first source candidate was geometrically sound but did not make the carved depth sufficiently visible in the actual capture. The revised source adds a scalar gradient to the existing frame vertices, from unchanged outside bearings to restrained directional inner gains. Distances to the outer and inner supporting edges produce a continuous interpolation even on the tapered nonparallel frame faces. The production profiles have nonzero edge lengths. The sill stays at its prior value, the far jamb/head recede more than the near planes, and no bright perimeter is added. RGB channels receive the same scalar, so the later forge-light sampling retains its colour relationship.

The two production-profile tests cover both visible wall faces, exact shared return/frame stations, fixed backing, positive recession, frame area and single ownership, finite bounded noncollapsed grain and final painter visibility. The revised tests compare referenced rendered frame vertices with the unchanged dressed emitter, checking identical geometry and the intended scalar relationships. Existing whole-shaft envelope and bearing tests remain present.

## Working apron

ServiceApronFlag retains its existing selection, parent partition and coverage. LaidServiceStone wraps each actual AshlarFlagFace repair emission and changes only the new colour entries. Design coordinates are reconstructed from the existing world paving UV contract. Vertex positions, UVs, triangle indices and counts are untouched.

The two unequal use patches are tied to current WorkIsland bounds at the trolley loading end and the mould bench's rail-facing side. Their smoothly warped edges and material-scale variation do not restart at individual flag boundaries. The receiving scrub ends before the open turntable; the central court is outside the active apron branch. The field is neutral outside its compact footprints and multiplies all colour channels equally.

Added tests compare actual emitted V/U/T arrays with the original repair emitter, require changed samples referenced by nondegenerate triangles at three intended locations, check bounded scalar gain and equal RGB ratios, verify a used adjacent-parent boundary and require exact unchanged colours in two unused service/transfer areas. The original coverage, layout boundary, connection and geometry limits are retained.

These tests and source checks establish implementation behaviour. Actual visual acceptance is recorded separately; they do not establish physical-device rendering or performance.

## Backing-visibility test resolution

The first full verification failed one new assertion: a translated backing centre on the narrow outer shaft face was expected to be visible, but the last triangle was frame vertex 160. The reviewer independently reconstructed the original octagonal projection, its two visible tapered faces and the opening polygons in a separate double-precision JavaScript calculation. At all three tested rises, the narrow outer backing centre is outside both the old flat opening and the new throat's visible rear intersection. The visibility precondition was false before this package.

The independent calculation reproduced the narrow outer profile areas: rear 0.7985053428, old visible 0.1982960802 and new visible 0.3339289273. The complete old visible area remains inside the new visible polygon. The other three profile/side combinations also retain their old visible regions while increasing exposed backing area. This calculation establishes the projection cause, not a Unity execution result.

The revised test replaces the invalid centre-point expectation with an independent convex intersection of rear and throat polygons. It requires positive original visibility, increased new visible area, full containment of the former visible region, actual backing-triangle coverage of the whole new visible region, and no later return/body/frame triangle overlapping it. The clipping code handles either polygon winding, uses double precision and retains the strict 1e-8 area tolerance. Its inputs are the independently authored opening stations and original shell. Degenerate rendered triangles are skipped; no probe is silently relocated and no production geometry is changed to fit the test.

Read-only review accepts this oracle repair. It tests the visible part of the actual backing more comprehensively than the three invalid centre witnesses. Root subsequently reported 22/22 targeted gate cases and 593/593 complete EditMode cases passed. Runtime source remained unchanged during the test-only repair. Full workflow completion remains root's responsibility.
