# Crane mechanical silhouette refinement

Scope: `FoundryRegionDressing.cs`, crane construction and two crane-only helpers. The existing foundry plinth, mast height, jib, suspension point and walking geometry are unchanged.

- Added a cast mounting shoe and open winch cradle, leaving the already implemented `FoundryRegionSite.BuildCraneHardware` winding drum visible between the cheeks. Removed the redundant large lower wheel from the original dressing.
- Added a broad upper bearing collar beneath the existing pulley.
- Replaced the flat counterweight strip with three visible iron faces and two retaining straps.
- Rebuilt the hanging salvage basket with an offset back rim, visible side depth, solid floor, two low scrap blocks and a diagonal offcut behind open front bars. The basket remains suspended outside the court at its original endpoint.

All new parts borrow the existing `FoundryIron` material and logical order -11 static batch. No new material, renderer group, texture, collider, scene data, animation or per-frame work. Construction uses full-face UVs to avoid the clamped-edge artifact encountered in the earlier workshop pass. The existing site gussets and winch drum are emitted later in their shared batch and intentionally remain readable over the new mounting structure.

Validation: UnityCompileCheck passed with 0 warnings and 0 errors. The prior actual Unity crane close-up was inspected to target the flat basket and unsupported lower machinery. New graphics capture, full Unity gate and phone review are performed by the integrating root; they were not run by this contributor. No commit or push.
