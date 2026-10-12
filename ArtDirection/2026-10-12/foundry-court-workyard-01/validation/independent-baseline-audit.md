# Independent baseline audit — court / workyard

Baseline c4f66e6, inspected 2026-10-12. Read-only source and actual Before-image review; no Unity, ADB, build or test execution. Evidence: new package `before/full-map.png` and `before/crane-detail.png`, previous matched forecourt detail, approved `foundry-layout-01/foundry-v1.png`, and the actual paving texture.

## Why the current floor remains visually uniform

1. **The special stone topology is real but narrow in scope.** `FoundryRegionFinish.BuildCourtStoneRing` already emits a 24-wedge outer setting and a fitted inner field. `FoundryRegionCourtSurface.BuildCourtProcessionalStone` only adds paired shoulders 0.48 design units wide; their open middle remains ordinary paving. A new ring alone is not the missing feature.
2. **The material deliberately unifies all faces.** `MAT_FoundryPaving` binds `FoundryPavingV5.png` (GUID c926e4cc19bf4ad28b54285a48d4935d), with material colour 0.8. The source is granular mineral texture, not a baked grid. `PavingVertex` applies the same continuous UV formula with span 5.7 and the same `FloorLight`. Ordinary and ceremonial faces also share the 0.96..1.03 face tone and broad mineral-family field. This avoids a checkerboard, but at full-map scale the new dressed faces largely merge with their neighbours.
3. **Existing joints are a fine detail at overview scale.** The fitted inner-field joint is 0.022; ring angular gaps are only 0.004 radians per end and dressed shoulders are 0.046 wide. Mipmapped/trilinear texture and small projected faces reduce local distinction. Merely increasing texture resolution or tint is unlikely to give the reference's larger stone layout hierarchy.
4. **There is already overdraw.** Ordinary packed flags are emitted first, then the ceremonial ring/field joint bed and their dressed faces in the same floor builder. Processional beds/faces similarly sit over ordinary paving. The next proposal should establish explicit local ownership/partition, not add a fourth full coat and rely on triangle order to hide the old grid.

## Constraints for the court proposal

- Preserve the walking outline, pit clearance, new medallion centre (-3,-5), and bridge/court concave union. Do not replace a clipped concave area with its bounding quad.
- Preserve world-continuous UV scale, alpha 1 and the existing floor-light sampling contract. Broad new faces still need the half-unit clipping/lattice where lighting or ramp grade changes.
- `FloorLight` has a mobile precision ceiling of 1.74; material tint and face/bevel multipliers were deliberately bounded. Do not compensate for weak topology by multiplying all channels or changing the global light response.
- Partition the actual emitted floor or explicitly account for any retained layer. Tests must inspect production triangles/coverage at transitions and retired joints, not only the new helper in isolation.
- Visual acceptance needs meaningful radial/processional stone arrangement at fixed full-map and actual portrait scales, with actor/attack-guide legibility retained. Pixel changes alone are insufficient.

## Workyard production-path and physical constraints

- Existing `BuildCraneHousing` is fallback-only inside `if (!BuildBakedCrane(foot))`. The actual imported crane is a baked Resources `FoundryCrane` card. Housing-only fallback changes would be invisible in accepted captures.
- The owner's proposed new active `BuildCraneMachineryBase` hook is therefore appropriate. Its new machinery box `WestBox(-26.12,11.22,-24.18,14.70)` lies inside the unchanged original crane footing `WestBox(-27,11,-24,15)`; max U is -32.18, clear of the walking West=-32 edge. Planned elevations 1.12..2.60 seat it on the existing deck without changing the mast/jib/chain endpoints.
- Preserve the original 8.6-height crane depth scope and existing material batch. Do not widen the baked card or mistake its transparent rectangle for a collision footprint. Existing barrels/crates and the nearby lantern constrain visible placement as well as numerical bounds.
- The source-in-progress creates an actual projected service aperture, drum, cheeks and header. Principal risk is painter ordering: rear/hood pieces, all cylindrical sectors, end discs, then supports must leave the intended drum visible. Require an actual last-visible-surface observation as well as prism containment; no detached drum, header across the opening, or hidden far-sector overpainting.
- The original work islands have shared `FoundryMap.WorkIslandBound` rendering/navigation footprints. A future change there must not expand apparent raised mass outside those bounds. This package's approved crane base does not need to change them.

No plan-level blocker in the bounded live crane-base approach. Court concrete plan and final source/images remain pending at the time of this note.
