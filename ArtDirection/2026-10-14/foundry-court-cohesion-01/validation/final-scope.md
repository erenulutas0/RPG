# Final source scope

Baseline: `722b3b23a18a3df2af89751da70ae81193ece511`. This package combines three bounded material and work-area changes in the existing foundry.

## Court material zones

The bridge and entry approach receive a broad scrubbed response, up to +8.5% on existing paving vertex RGB. Settled material use reduces the connected stock/receiving apron by up to 11%. Smooth multi-unit transitions cross ordinary quarry flags, fitted processional stones and repaired apron stones through the same world-position sample in `PavingVertex`. The seal and its seven-unit-radius fighting field, plus the raised gate paving, remain neutral.

`FoundryRegionPavingWear.cs` owns the new scalar field; `FoundryRegionFinish.cs` applies it once at the shared paving vertex. Existing grain, UVs, mineral families, joints, bevels, emitted geometry and later court clipping remain unchanged. The earlier local loading stains and scrubbed receiving threshold in `ServiceApronUse` retain their own authored contribution. The neutral scalar preserves the source floor-light RGB relationship. It introduces no new light, decal, crack, mesh or texture.

## Existing work-area clusters

The mould-bench receiving shelf now carries two stacked open spare flasks and two substantial packing boards, replacing two generic closed blocks within its original envelope. The existing worktop planks use one directional timber field. Bench steel uses a common field with restrained cool iron, retaining bounded fire-facing warmth; timber and sand remain distinct. Working moulds, sand/pattern geometry, clamps, trestles, shelf and worktop stations keep their previous arrangement.

The existing crane casting joins that cool worked-iron family. A broad darker lower contact and restrained rubbed upper shoulder change only its vertex colors; its geometry, UVs, open drum, anchors and footprint remain. Ownership is limited to bench emitters/finish in `FoundryRegionProductionYard.cs` and `FinishCraneMachinery` in `FoundryRegionCraneHousing.cs`. The trolley, turntable/rails, baked crane/stock images and occupied footprints are unchanged.

## Gate and service-iron family

`GateStoneTone` in `FoundryRegionArchitecture.cs` gives exposed gate stone a bounded, face-dependent quarry lift and slight cool bias. Height weighting preserves the damp base, while the existing source light and recess construction retain their hierarchy. This changes the stone response rather than global ambient exposure. Door art, masonry geometry, openings, caps and bearings are unchanged.

`FinishedPipeTone` in `FoundryRegionPipeFinish.cs` places the existing rolled service mains in a cooler iron family. Their broad crown/underside/sleeve response remains; `MetalTone` continues to supply localized reflected fire. Pipe centerlines, envelopes, subdivisions and grain fields are unchanged. Baked boiler/fire-window images, brass fittings and unrelated iron retain their previous treatment.

## Cost and preserved contracts

The actual After scene reports 34 region renderers and 161,385 vertices, versus 34 and 161,161 Before: **+224 static vertices**. The increase comes from eight flask-rail hulls and two packing-board hulls replacing two closed hulls. Distant geometry remains 3,572 vertices. No whole-region index delta was measured.

There is no new runtime texture, material, renderer, component, owned resource type or per-frame updater. The paving field adds bounded work during existing static mesh construction. The 174,000-vertex region ceiling remains unchanged. Counts establish resource scope, not physical-device frame time.

Map dimensions, walking union, collision/navigation, camera, actors, combat, encounters, progression, economy and saves are unchanged. No phone installation or save/telemetry operation belongs to this package.

## Visual and validation boundary

Twenty-one fresh Before and twenty-one matched After Editor views use the same camera/actor setup. The graphics test passed 1/1. The observed entry/forecourt change is restrained but visible in direct comparison, while the central hero/seal field stays essentially unchanged. The work pocket is quieter and the gate quarry lift is more immediately visible at full-map scale. No new checkerboard or hard mask rim was apparent in the inspected full-map, court and forecourt views.

This improves material hierarchy and local equipment purpose. It does not substantially change composition, match the reference's full authored detail, or establish device motion/shimmer/performance acceptance. The nine new EditMode cases cover actual emitted court material samples, spare-flask/support geometry and crane material response. The final full regression gate and conditional APK build passed; verification.md records the counts and APK hash. The APK has not been installed.
