# Gate crown and boiler foundation refinement

Changed only `FoundryRegionArchitecture.cs`.

The gate now has a continuous shallow masonry gable supported across the full existing header. Eaves sit at header height +0.43, the ridge at +1.55, with a 0.13 stone cap. Two projected roof slopes supply depth. The earlier raised central seal housing is retained in front, and the large painted doors remain untouched. All new gate points stay within the existing gate's design-plane scenery footprint.

Each boiler receives a continuous low stone plinth and three visible tapered corner shoes. The lowest course ends at 0.29; corner shoes end at 1.05 and occupy only narrow corners of the unchanged 3-by-3 scenery footprint. The broad painted fire-window faces, chimney top, mouth at height 4.66 and smoke centres are unchanged.

Existing FoundryWall logical -11 and FoundryIron -10.5 groups are reused. No new raster asset, material, renderer group, collider, camera, gameplay or per-frame work. No scene edit. No commit or push.

Validation: UnityCompileCheck passed with 0 warnings and 0 errors. Root must inspect a new actual gate-detail and furnace-detail capture for gable painter order and boiler corner coverage, and run the combined full Unity/build/device gate. This contributor did not run Unity or adb.

Root-run gate capture exposed the added metal slope flashing painting across foreground towers and the central housing at the higher iron rank. Both new flashing bars and their unused coordinates were removed; the masonry pediment and existing inset ironwork remain. A fresh root graphics capture should verify the correction. No Unity or adb run by this contributor.

