# Independent baseline review: court cohesion

Date: 2026-10-14. Baseline: 722b3b2. Reviewer: gate_stonework_review. Planning/source inspection only; no Assets edits, Unity or device operation.

I reopened the latest accepted full-map, court-detail and stock-detail renders from `../foundry-gate-stonework-01/after/`, and the approved `../../2026-09-23/foundry-layout-01/foundry-v1.png` reference. The broad layout and major industrial landmarks are recognizable. The remaining full-map gap is the quiet but overly uniform open floor, the sparse relationship among working objects, and uneven material detail between painted equipment/door art and procedural structures. Tiny additional joints alone cannot address it.

## Visual success criteria

- The full-map comparison must show a coherent court treatment and more purposeful existing work areas. Keep the central fighting space calm; no scattered props or painted obstacle silhouettes on the walking lanes.
- Broad material use should cross multiple stones continuously, with restrained slate value variation. Reject dark checkerboard tiles, bright route ribbons and more high-frequency noise that only reads in a close-up.
- Stock/crane, pit and gate-furnace groups should remain distinct and supported. New local emphasis must follow existing machine bases, service shelves and work footprints rather than filling empty playable ground.
- In court and portrait views the blue hero, orange enemies, chest and ability ring must remain immediately readable. The brass medallion and routes should continue to orient the player without overpowering combat.
- Gate material should connect the procedural flanks to the painted central facade. Preserve the recently accepted recess depth, open threshold presentation and broad stone construction. No additional black outlines or general darkening.
- Keep fire warmth localized. Existing runtime floor pools already occupy more area than the reference; a wider orange wash would reduce the match.

## Root lighting/material source recommendations

The active boiler assembly in `FoundryRegionMassing.cs` calls `BuildBakedBoiler` and skips the old procedural body when the bake exists. `BuildFurnaceTowerParts` and `BuildBoilerArchitecture` are therefore not reliable hooks for the visible primary boiler body. Confirm an edit's active call path before judging it.

`FoundryRegionBakedBoiler.cs` emits white-tinted source art whose illumination and fire windows are already painted. Applying the general metal-light field to the entire bake would also tint those fires and likely double-apply its baked illumination. Preserve that source presentation unless a deliberate, separately verified masked material treatment is introduced.

`ArchitectureLight`/`SurfaceTone` in `FoundryRegionWeathering.cs`, the `LitFoundryTier` call sites, and `MetalTone` on adjacent pipes/supports are useful existing hooks. Prefer a bounded gate/industrial-family material adjustment to a global court exposure change. Respect authored elevation and local source height; metal illumination already accounts for the raised floor through `FoundrySurface.Height`. A matching stone calculation should use the same physical convention if cross-level influence is changed.

`GateStoneContact` and the previous lower-buttress wash already exist. Avoid stacking another uniform contact multiplier. The useful goal is coherent broad values and material scale, not ever darker joints. Geometry, texture ownership, painter ordering and navigation should retain their contracts.

## Evidence boundary

Final acceptance needs matched full-map, court, stock/crane, gate and portrait views from the actual scene, plus the required source/test/resource gate. Device shimmer, moving occlusion, crowd readability and performance remain separate from static visual acceptance.
