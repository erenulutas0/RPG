# Court stone material integration — source review

Owner: court_material. Date: 2026-10-12. Baseline: 3d85425.

Inspected `Docs/06_TECH_ARCHITECTURE_UNITY.md`, art pipeline and latest handoff entries, prior court/pit/gate actual-image review, the approved foundry reference, and this package's fresh `before/court-detail.png` and `before/full-map.png`.

## Change

- The 24 radial ring stones and fitted inner flags now have genuine flush dressed shoulders: an inward-offset face and joined edge strip partition each original polygon. Every fitted curved end and radial wedge retains its exact footprint. Existing common joint beds remain underneath, so this does not create floor holes.
- The bevel follows the existing world-space wear field and directional stone response, fading its contrast where worn. It is sampled at no more than half a unit along each edge. Interior faces retain the existing half-unit lighting lattice and continuous paving UVs.
- The central ceremonial 8×8 paving, fitted inner flags and ring all use the existing court quarry-family sampling. This removes the central paving's independently randomized warm checker and the ring's repeating every-third-stone tone. Existing surrounding paving, global lighting, lamp pools and stone texture remain unchanged.
- Central joint bed tone changes from 0.68 to 0.82, near the inner field's 0.84 and surrounding court's 0.84. Dressed shoulders carry the relief rather than a dark grid. The compass, brass geometry, world grain scale, footprints, floor heights, materials, renderer/depth setup, gameplay and navigation are untouched.

## Implementation and verification

Owned runtime: `FoundryRegionFinish.cs` court-face branches and ring/field call sites; new `FoundryRegionCourtSurface.cs` plus metadata. No shared FloorLight, light lattice helper, heraldry or renderer edits.

The new local contour preparation removes clipping duplicates/collinear points and normalizes winding before reusing the established inset routine. Two new EditMode cases exercise the actual clockwise radial wedge and counter-clockwise curved fitted end, checking total triangle area, footprint containment, finite/opaque vertices, flush placement and world UVs. Original medallion bounds/coverage/1,800-vertex guard is unchanged.

Isolated .NET compile of runtime and Unity tests: exit 0, 0 warnings, 0 errors. Unity is root-owned; no Unity execution, ADB, device observation, Git staging or commit by this agent. Actual-image acceptance, full suites, final resource counts and build remain for root.

## Image review targets

The stone shoulders should read as shallow wear, not a bright halo. The central stone family should belong to the adjoining court without losing the metal compass. Inspect both the full map and court close view, plus actor feet/blue ring. Gains are likely strongest close up; do not claim complete reference material fidelity or device performance from this source change.
