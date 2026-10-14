# Broad court material use

Source scope: `FoundryRegionPavingWear.cs`, one color hook in `PavingVertex` in `FoundryRegionFinish.cs`, and `FoundryCourtMaterialTests.cs` plus its metadata. No court geometry or packing method is changed.

The entry/bridge approach receives a broad scrubbed material response, up to +8.5% on the existing vertex RGB. The connected stock and receiving apron receives settled material use down to -11%. Smooth shoulders span several laid flags. The existing seal and its seven-unit radius fighting field remain exactly neutral; the transition ends at eleven units. The raised gate region remains neutral. These are material gains, not additional illumination sources.

The shared paving vertex samples the field once. Ordinary quarry flags, fitted processional/bridge stones and repaired working-apron flags therefore use the same physical coordinates. Existing texture UVs, mineral families, joints, geometry-owned bevels, triangle ownership and source forge-light channel relationships are retained. Existing local `ServiceApronUse` loading stains and scrubbed threshold remain independently authored and unchanged; they multiply the common broad field only where they already acted. Subsequent court clipping continues to interpolate the already-colored source triangles without reapplying the field.

There are no new vertices, indices, meshes, renderers, textures, materials, components or per-frame work in this scope. Map bounds, navigation, collision, camera and gameplay are unchanged. Startup mesh generation evaluates the new scalar for each already-emitted paving vertex. Static resource counts are not device performance measurements.

Seven proposed EditMode cases inspect actual emitted floor buffers: four location cases check visible entry/work response and neutral centre/gate while retaining exact grid positions/UV/counts and original RGB ratios; a fitted/ordinary joint checks common colors at referenced boundary vertices; a broad half-unit grid checks region-scale support, gentle transitions and protected centre; an actual repaired flag checks the apron gain exactly once on referenced inset-face triangles and retains the existing local use contract.

At source freeze, these tests have not been run by this helper. Root owns compilation, the matched After capture and the full validation gate. Visual acceptance requires the actual full-map/court comparison, not just numerical field values. No phone, installation, save or telemetry operation belongs to this scope.
