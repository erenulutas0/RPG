# Authored stock-station metal

Source: `Tools/ArtReview/BuildBlenderStockStation.py`, authored geometry using the established study02 procedural materials and lights. No externally sourced mesh or bitmap and no post-render raster modification. `stock-station.blend` is the editable authoring scene; `stock-station.png` is the transparent 1024 render. `projection.json` records camera, source mesh counts and footprint.

The replacement preserves the existing quench tank/skids/recessed water, six billets, two strip coils, three hollow pipes with supports and straps, and the tool rack. Metal gets continuous grain, rounded edges, real pipe bores and coil recesses. Barrel, crate, crane, hoist, masonry and walking routes are excluded and remain separate runtime objects.

Runtime anchor is stock-local (4.5,20.45), image bounds x[-3.15,3.15], y[-2.45,3.85], exactly matching orthographic scale6.3 and vertical target0.7. Explicit non-landmark depth owner retains the whole StockShelf, height1.6. Source rank-12 quantizes to renderer order-10. Coordinated stack: shelf cap-12, hoist-11, stock-10, crane-9, live wood/chain/hardware-8. Those are renderer orders, not source rank inputs.

Import copies the existing salvage profile: 512 max size, ASTC6x6 Android, mipmaps, non-readable. Calculated block payload158432bytes including mips, excluding engine overhead. One owned material and one four-vertex card replace the original metal geometry, with the original methods available for missing-resource fallback. The root integrator owns the Workshop guard, source-material destruction and automated lifecycle/depth assertions.

Isolated Blender render visually reviewed: open tank, six stacked billets, coil bores, three separate hollow pipes and tool silhouettes read correctly, with space reserved for the live barrel/crate. Authoring mesh10176vertices/19928triangles is not runtime geometry. Unity painter/depth acceptance and automated/device verification are pending; this source review claims neither physical-device installation nor performance.
