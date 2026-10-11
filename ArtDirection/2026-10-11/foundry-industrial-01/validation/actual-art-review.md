# Independent actual Unity art review

Date: 2026-10-11. Reviewer: industrial_source_qa, separate from the authors and Unity capture operator.

Status: **Accepted for this industrial-identity slice. No blocking visual regression found in the reviewed captures.** These are Unity images, not phone screenshots; physical-device and crowded-combat acceptance remain pending.

## Evidence

Reviewed final `after/full-map.png`, `gate-detail.png`, `north-furnaces.png`, `south-retorts.png`, `gate-new-22.png`, `gate-new-26.png` and `approach-20-1.png`. Compared the composition and matching gate/portrait views with the copied compact-gate baseline and the approved `foundry-layout-01/foundry-v1.png` reference. The north/south close-ups provide extra after-state contact evidence, not matched before/after claims.

Read the final loaded-texture records and runtime budget in `after/`, plus the separate integration and source reviews. No Unity, ADB, gameplay, source, or asset mutation was performed by this reviewer.

## Accepted observations

- The north pair now reads as an angular pressure chest beside a ribbed exhaust furnace. Their distinct silhouettes survive the complete map view and remain cleanly separated in the close view. Both corrected open flues are visible; smoke remains centred above the outlets.
- The south pair has different manufactured hoods. The first inspection hatch, handle and hinges remain visible in actual portrait views, replacing the former broad flat overlay. The second hood has distinct ribs and a visible service plate. Detail is modest at whole-map scale but readable locally.
- Upper bodies sit on their retained stone bases. External service pipes appear to meet the sockets without exposed gaps; main supports, reservoir footings and downward drains remain coherent in the reviewed north/south views. No new floating card edge, atlas neighbour, transparent seam or displaced flame was observed.
- The gate approach and landing remain unchanged. Hero silhouette, weapon and blue range ring are legible in the three reviewed portraits. The new retort tops stay outside the path and introduce no visible actor overlap in these fixed positions.
- Existing cool stone/iron, restrained bronze and contained orange fire keep the new variants within the foundry palette. The bright metal edges do not overwhelm the actor or become new isolated light sources.

## Recorded import and budget facts

Both loaded atlas records report 2048x1024, ASTC_6x6, 12 mip levels, non-readable textures, and an Editor texture-memory reading of 2,502,304 bytes each. These are observed Unity/Editor diagnostics, not measured phone memory.

The region record reports 32 renderers and 125,234 vertices, compared with the baseline 125,406. The reduction is consistent with removal of the old separate hatch overlay. Neither renderer count nor vertex count establishes frame time, allocation, startup or device performance.

## Remaining reference gap

This is a clear local industrial-identity improvement, not completion of the reference map. The gate housing and supporting architecture still use larger, simpler stone forms; surrounding native structures remain more rigid and regular. The reference also has richer functional pipework and material wear. Further work should address those substantial structural/material differences rather than repeatedly adding tiny hatch details. Accumulated phone, motion, crowd and midrange-device validation remains separate work.
