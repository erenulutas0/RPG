# Foundry court stonework - 2026-09-29

Combined static floor finish following foundry-perimeter-finish-01.

- Added 24 flush annular flags around the existing central engraved seal. Outer radius 3.97 stays inside the existing +/-4 inset, clear of the shaft. Same paving batch, continuous UVs and local illumination; seal remains above the paving.
- Added restrained broad shoulder shading to the gate approach, fading into the court.
- Softened joint contrast (0.49 to 0.64) and reduced random slab value spread (0.90-1.09 to 0.94-1.06).
- Sparse triangular corner repairs stay inside individual slabs and remain flush. No new cracks, obstacles or raised rubble.

Runtime files: FoundryRegionFinish.cs and FoundryRegionView.cs. Existing materials/textures reused. No scene, gameplay, collision, camera, save, energy or per-frame behavior changes. No agents or new generated art used.

## Verification

Actual graphics-enabled five-position traversal passed 1/1. Center combat, gate and general floor imagery inspected. Compile check 0 warnings/errors; integrity 515 project GUIDs / 1099 package GUIDs / 568 scene objects/components passed.

Static budget: 17 renderers / 57,432 vertices (+616 from previous 56,816). Existing atmosphere remains 1 renderer / 780 vertices separately. Before images preserve the preceding package; after images are actual Unity captures.

Full Unity suite and Android results follow below. No phone installation or new device performance result in this session. This is incremental surface refinement; reference-quality completion is not claimed.

## 2026-09-29 - Combined court stonework finish

Added a flush 24-flag stone annulus beneath the existing central engraved seal (radius3.97, inside the +/-4 inset), restrained gate-road shoulder shading, sparse in-slab corner repairs, softer joints and reduced slab value variation. FoundryRegionFinish and FoundryRegionView only; existing paving batch and continuous UVs reused. No new material/texture, per-frame behavior, walking/collision, scene, gameplay or camera edits. No helpers used.

Actual Unity five-position capture1/1 passed; center combat and gate views reviewed. Compile0warnings/errors, integrity515/1099/568, full EditMode405/405 and PlayMode130/130 passed, Android exit0. Static17renderers/57,432vertices (+616), atmosphere1/780 unchanged. Preview HTTP200 and7/7 local references verified. Before/after and production notes: ArtDirection/2026-09-29/foundry-court-finish-01.

APK actual39,340,606bytes, localtime2026-09-29 19:11:24, SHA256103811AA22B982D4363C509FA4BBA8EBD6ADD7DDD2ED3D5D7567355D70E5FCE3. READY NOT INSTALLED. Includes all prior uninstalled art packages. Last verified installed remains18DF8C2A877C96FEE67A36447E968C0103E394B9512EC66BA079ADE0D91D5535. No phone operations, save edits, new device performance claims, commit or push. Reference-image fidelity remains unfinished.