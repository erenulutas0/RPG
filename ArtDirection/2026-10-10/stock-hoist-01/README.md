# Authored stock loading hoist

Editable fixed-view replacement for the existing procedural hoist. Source geometry, study02 procedural materials and studio lights were authored in Blender4.5.9. No purchased or generated raster source and no post-render bitmap editing.

Reproduce from repository root:

```powershell
& 'TestResults/tools/blender-4.5.9-windows-x64/blender.exe' --background --python Tools/ArtReview/BuildBlenderStockHoist.py
```

The script reads `ArtDirection/2026-10-09/blender-furnace-02/furnace-study.blend` and writes this directory only. `stock-hoist.blend` retains editable geometry/materials/lights/camera. `stock-hoist.png` is transparent1024-square source; `projection.json` records the orthographic contract. Runtime source is copied to `Assets/_Project/Resources/FoundryStockHoist.png`, imported512 ASTC6x6 with10mips and no CPU-readable copy. Calculated compressed blocks including mips:158,432bytes, excluding engine overhead.

Stock-local anchor(4.5,22.08), quad x[-2.7,2.7],y[-0.9,4.5], full UVs. StockProject applies the existing west assembly placement. Original feet and3.44crosshead height remain. Explicit non-landmark StockShelf depth height3.6; one runtime quad/material, sorting rank-11.5 ->-9. The crane and existing stock at-8 stay in front. This ordering is visually verified and spatially correct, even though it partially hides chains/yoke. Raising the entire image to expose them would put it incorrectly in front of the crane.

Original procedural assembly remains only as missing-resource fallback. Runtime cleanup owns the material/variant/mesh while preserving shared texture. Matched Unity review, full verification and physical-device status: `../foundry-materials-01/validation/verification.md`. Static baked lighting and fixed-view presentation remain limitations.