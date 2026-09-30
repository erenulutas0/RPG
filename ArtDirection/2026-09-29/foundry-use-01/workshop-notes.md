# Working anvil station — 2026-09-29

Owned source: FoundryRegionWorkshop.cs only.

The west service shelf extends outward from u=-20.7 to u=-23; its court-facing u=-18.45 edge and v=[5,8.4] bounds are unchanged. Existing ingots, strapped parts bin, north stored pipes and tool rack retain their positions. The shelf uses the existing fully supported Block foundation down to -2.8, not a floating accessory platform.

Anvil station occupies the new exterior bay. A low stone bedding supports a widened iron foot, tapered concave waist, broad face/flat heel and triangular projecting horn. A dark hardy-hole mark and one hammer resting on the top give the silhouette a readable working purpose. Full anvil u envelope [-22.32,-21.38], v=[5.58,8.03]; bedding [-22.5,-21.2] x [5.65,7.55]. All remain within the shelf, outside the existing walking union. Top overhangs are intentional anvil head/heel/horn; the waist and foot are supported.

Existing FoundryWall -11 and FoundryIron -11 batches; shelf Block uses existing material/rank combinations. No new raster source, material, renderer batch key, per-frame work, collision, gameplay, or scene edit. Iron faces use full-face UV mapping, retaining the existing clamped material contract.

Pending root compile and actual graphics proof. Inspect workshop-detail for horn/heel distinction, hammer resting on the face, stock separation and source-to-render painter ordering. No Unity/adb operation performed in this subtask.
