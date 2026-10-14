# Combined workyard finish scope

Baseline: `d4424447705aa85bf078ed5fb46ab11c92555b91`. Fresh Before and final Assets freeze are recorded in this package. Root reports **438 pure tests passed, isolated compile with zero warnings/errors, and 258/258 focused Foundry cases passed**. Fresh After capture, independent image acceptance and the full regression/build gate remain pending at drafting. This document does not claim installation or device acceptance.

## Three coordinated material changes

**Native iron and timber.** Existing crane machinery and yard bench finishes now share a narrower cool-grey iron family while retaining source face-light ordering and localized warm reflections. Lower contacts remain darker than rubbed shoulders. Timber working faces move from saturated orange toward muted hardwood, with a modest rubbed upper value; stored packing stays quieter. The baked crane, stock/casting equipment, service pipes and all placement remain fixed comparison anchors. No geometry, directional UV sampling or material selection changes accompany this palette revision.

**Service-tower stone.** Existing exposed pier contours gain neutral quarry value: west 12–18%, near face 8–12%, rising between retained sill/head vertices. Sheltered backs retain their previous response, as do deep returns, fire panes, sill and the projecting dressed arch. Outer spandrel faces gain only 8% / 5%; supported corner blocks and haunches gain 7% / 8.5%. Directional ratios, original alpha and the final weather/heat/fade function remain. Silhouette, open mouths, station and bearing contacts, indices and UV coordinates stay unchanged.

**Equipment-contact paving.** `PavingWorkshopTint` samples the actual stock, casting, salvage and two work-island footprints in world space. A smooth muted ash/iron albedo field fades from existing working feet into the apron, reducing the local orange cast. Its strongest tint is `(0.84, 0.90, 0.94)`; it returns to white within the protected central seven-unit radius and at the raised gate family. The field is applied once in `PavingVertex`, so ordinary and fitted paving share their boundary colour without adding an overlay, floor owner or light source. Existing quarry identity, scalar court-use field and world forge lighting remain.

The contact tint multiplies existing `PavingCourtUse` and `FloorLight`, including earlier contact shading. This stacking is deliberate but can over-darken or over-cool a working apron even when each factor is individually bounded. Actual full-map/portrait review must judge their combined result, especially beside the unchanged baked equipment and quiet centre; passing field tests alone cannot establish visual balance.

## Cost and verification boundaries

All changes execute within existing startup colour/mesh-finishing loops. New scalar/tint arithmetic changes construction CPU work, but this package adds **no geometric subdivision, vertex/index count, runtime image, texture, material, renderer, shader or per-frame updater**. The preceding package's mineral-fold subdivision remains existing baseline cost; it is not new work here. Fresh capture must confirm integrated resources rather than infer phone performance from unchanged source geometry. Global ceilings remain 174,000 region / 3,600 distant; no allowance is raised in this package.

Six new service cases inspect actual bay/shaft output, neutral gain and alpha, unaffected sheltered/head surfaces, retained geometry and UV. Native machinery cases inspect emitted source-light ordering, warm reflections, contact separation and fixed geometry/UV/index buffers. The existing worktop palette assertion intentionally changes to bounded muted hardwood with both lower and upper hue bounds; this is the declared artistic revision, not a relaxed geometry test.

Paving cases recover the new contact albedo from emitted colours independently of the new field function, detect absent/doubled application, check bounded smooth transitions and real walkable coverage, and verify ordinary/fitted boundary agreement. Existing mineral/scalar isolation helpers now divide out this separately tested tint. All geometric, contact and ownership tolerances remain intact.

The ten source/test files are the five production files `FoundryRegionCraneHousing.cs`, `FoundryRegionProductionYard.cs`, `FoundryRegionDistantWorks.cs`, `FoundryRegionPavingWear.cs`, `FoundryRegionFinish.cs`, and five test files `FoundryCraneMachineryTests.cs`, `FoundryYardEquipmentTests.cs`, `FoundryServiceShaftTests.cs`, `FoundryCourtMaterialTests.cs`, `FoundryPavingLayoutTests.cs`. No shared scene, gameplay, camera, map, collision, save or telemetry change belongs to this work.

Reference fidelity remains unfinished. The intended gain is a coherent existing workyard material family and clearer supported stone, not new map architecture or proof of final quality. Full-suite/build, native matched image review and any later phone installation must be closed separately by the root. Current original saves must be preserved before later device work; historical snapshots must not overwrite newer progress.

## Final regression/build closure

438 .NET / 660 EditMode / 159 PlayMode = 1,257 accepted suite executions, zero failures/skips. Isolated compile zero warnings/errors. Fresh Before and After Android-target graphics captures passed separately. PASS: 622 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,391 bytes; SHA256 A967EFA4B65337DD7F0B401611520600E47EC5D1172C8A416D8009676049774B; NOT INSTALLED.

The final frozen-source suite/build gate is complete. This closure supersedes the earlier pending full-suite/build statements in this document; phone acceptance remains pending.
