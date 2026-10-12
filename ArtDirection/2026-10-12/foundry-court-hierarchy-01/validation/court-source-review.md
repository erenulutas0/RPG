# Independent court working-apron source review

2026-10-12. Skyline helper read `FoundryRegionServiceAprons.cs`, the affected `AshlarFlag`/`AshlarFlagFace` code in `FoundryRegionFinish.cs`, production packing/zone definitions and both new `FoundryWorkingApronTests`. No source edits, compilation, Unity, ADB or Git commands were run for this review.

## Result

No blocking source defect found. Accept for root complete tests and actual-image assessment. The principal remaining risk is visual: the much larger work field could read as a dark carpet despite its softened boundary.

## Coverage and exclusions

- The new selection modifies already-packed parents. An unselected parent emits no repair geometry and falls through to the ordinary face; a selected one emits only its replacement. There is no newly overlaid floor patch.
- Long-parent splits use integer half-unit widths/heights. Child origins and dimensions exactly partition the parent rectangle, and each child is rendered with the existing complete joint bed. Broad parents split only when both dimensions retain at least one unit in the children; small/narrow parents remain whole.
- Parent Zone 1/2 exclusion protects ceremonial/gate surfaces, and the existing packing/forced-joint boundaries are retained. The two connected weight boxes meet with positive area, while the full-strength spine cannot have randomized holes. Hash selection only breaks up the outer shoulder.
- The present workyard shift puts the eastern soft shoulder well west of the pit-side passage; a selected parent's small extension beyond its center does not reach the tested pit-side point. No new movement, collision, surface height, scene or depth rule appears here.

## Material and cost

- Repair faces retain the existing world-space grain and floor-light sampling. New broad quarry variation avoids independent random values on each child, and the lighter repair mortar narrows the value difference between its faces and joints.
- There is a genuine potential vertex increase because each long parent becomes two independently tessellated faces/beds. The new test measures that difference over actual production-packed parents and caps it at 4,000 extra vertices; it does not merely assert an authored loop count. Root's full-region capture remains necessary to report the actual combined cost.
- The test checks sampled complete interior coverage, vertex containment, finite data, exact world UVs, opaque colors, whole small-parent geometry, the connected rail core and key exclusion locations. Its flat inverse is appropriate here because the workyard selection lies far west of the gate ramp.

Actual review should decide whether the resulting receiving/stock zone groups the equipment at full-map scale while keeping actor feet and guide rings clear in portraits. Source correctness alone cannot establish that it avoids a rectangular carpet or that the hierarchy gain is substantial.
