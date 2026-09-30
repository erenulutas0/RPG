# Foundry paving regions - 2026-09-29

## Delivered scope

`FoundryRegionView.cs` delegates the existing cell-union surface drawing to `BuildPavingCell` in `FoundryRegionFinish.cs`. Three static surface zones distinguish the yard:

- Seal inset, design coordinates [-4,4) x [-4,4): sixteen 2x2 flags with a restrained warmer/lighter tint.
- Gate approach, [10,32) x [0,10): staggered crosswise 1x2 flags, slightly warmer than the surrounding court.
- Workshop strip, [-18,-14) x [2,12): darker 1x1 working stones.

The rest of the court retains staggered 2x1 slabs. Pairing clips at zone boundaries; half flags fill staggered row ends. Existing shared paving texture, floor lighting, soot, and surface batches are reused. No new texture, material, raised step, collision, walking-union, gameplay, camera, or scene-asset change. The preceding arch masonry slice is included in the working tree.

## Review evidence

Before images were copied from the previous arch package/capture before this edit. After images are actual Unity output from `Tools/ArtReview/Run-FoundryRuntimeProof.ps1`. Overview, court combat composition, and gate-approach portrait were visually reviewed. Five-waypoint traversal passed (1/1 capture test). `walk.csv` and `static-budget.txt` preserve the results.

Static budget: 17 renderers / 51,780 vertices (previous 51,348; +432). Atmosphere remains separate at 1 renderer / 780 vertices. Compile check: 0 warnings/errors. Static integrity: 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full suite/build results follow below.

No helper agents or phone actions in this slice. Last verified installed build remains `18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535`. Device state must be freshly inspected before any later input. No commit/push. This improves floor composition; it does not complete the reference-quality art target.

## 2026-09-29 - Court, approach, and workshop paving

Added three flush paving regions in FoundryRegionFinish/FoundryRegionView: central 2x2 seal flags, crosswise staggered 1x2 gate-approach flags, and darker 1x1 workshop stones. General court remains staggered 2x1, with half flags at zone ends. Existing shared texture, static lighting, and batches retained. No walking-union, collision, scene, gameplay, camera, or raster asset change. Includes the prior uninstalled arch masonry slice. Static budget 17 renderers / 51,780 vertices (previous 51,348); atmosphere separately 1 / 780.

Actual five-waypoint Unity graphics traversal passed. Reviewed overview and center/approach/workshop portraits; comparison in ArtDirection/2026-09-29/foundry-paving-01. Compile 0 warnings/errors; integrity 514 project GUIDs / 1,099 package GUIDs / 568 scene objects/components. Full gate EditMode 405/405, PlayMode 130/130, Android exit 0. APK actual size 39,340,606 bytes, local build 2026-09-29 16:43:29, SHA256 EED797DA0FFA60A586E6523759A3261A6F35B6B53F91455DFBABB484C68C5BFB. READY, NOT INSTALLED. Last verified installed remains 18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone actions, new device measurements, helper agents, commit, or push. Reference-quality art is still in progress.