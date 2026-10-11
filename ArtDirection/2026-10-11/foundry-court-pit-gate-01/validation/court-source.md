# Court compass and fitted medallion field

## Intent and scope

The fresh `before/court-detail.png` shows that the existing court seal is readable close up, but its thin open star and dark undivided centre lose their identity in the full-map view. The approved reference instead has a quiet compass set into broad fitted stone. This change makes that assembly clearer without turning the floor into an emissive ability mark.

Owned runtime changes:

- `FoundryRegionHeraldry.cs`: court seal construction only, split into `BuildCourtSealGeometry` for direct geometry verification and a small local facet helper.
- `FoundryRegionFinish.cs`: `BuildCourtStoneRing` calls the new local `BuildCourtMedallionField`; the outer ring and whole-court paving are unchanged.

The compass now has eight slim paired metal facets with subdued light/dark faces, restrained stone engraving beneath them, and one continuous brass annulus. Most stone between the arms remains visible. The original long/short ray orientation, short setting keys and r3.79 ring centre are retained. The maximum engraving radius is 3.885, safely inside the existing r3.97 stone medallion. Broad facets sample the existing metal interior rather than a new texture or bright bevel strip.

The inner r2.73 field retains its exact 48-point boundary and a complete stone joint bed. Three staggered courses of broad fitted flags are clipped inside it. Narrow joints expose that bed; the flags share the unchanged continuous paving UVs, half-unit light interpolation and floor projection. No raised geometry enters the traversable space.

## Preserved contracts

- No navigation, camera, scene, gameplay, materials, textures or updaters added or changed.
- Court geometry remains flush in the existing floor-decal batches.
- Shared `WeatherHeraldry`, ceremonial colour constants, gate procession, court compass setting keys and whole-court packing/lighting are untouched.
- Outer stone ring remains r3.97 with its original radial stone faces and joints.
- New seal emits 538 raw vertices; its test cap is 600. The fitted inner field has a bounded 1800-vertex test cap; actual scene totals remain for the root Unity capture to record.

## Verification

Two new EditMode cases in `FoundryCourtCompassTests.cs`:

1. `CompassRemainsFlushBoundedAndReceivesOrdinaryFloorLighting`: checks actual emitted vertices, indices, finite UVs, the r3.885 footprint, ordinary floor projection, distinct metal/engraving samples, and opaque bounded colour after normal floor lighting.
2. `FittedInnerFlagsPreserveCompleteMedallionCoverageAndWorldGrain`: checks actual field vertices, the r2.73 footprint, finite non-degenerate geometry, continuous world-space paving UVs and sampled complete bed coverage including the flag joints.

Final isolated `UnityCompileCheck` build succeeded with 0 warnings and 0 errors. No Unity, ADB, Git or scene operations were run by this agent. Runtime/EditMode execution and visual acceptance remain pending the parent agent's controlled Unity pass. Compare full-map, court-detail and approach-0-0 at the same baseline framing, especially compass restraint and whether the fitted stone remains quiet beneath enemies.

The parent's first full EditMode pass reported 509/510 with one real court colour failure: `Color * 1.08` scaled the new annulus alpha as well as RGB, and normal floor lighting retained 1.08. The local annulus tone now explicitly sets alpha to 1 before `SealQuad`, preserving the intended RGB. Shared helpers and the strict test are unchanged. Isolated compile after this bounded fix also passed with 0 warnings/errors; the parent retains the original failed XML and will rerun the gate.

Assets frozen for parent verification.


## Later integration outcome

Root completed the actual paired-image review, all final suites and the Android build. See `actual-image-review.md` and `verification.md`; the earlier pending statements above describe the implementation handoff stage. Device acceptance remains pending.
