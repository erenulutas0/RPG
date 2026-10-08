# Foundry north court, workshop and paving — 2026-10-08

Three requested tasks share one tested package. This extends the preceding `court-silhouette-01`; the reference remains `ArtDirection/2026-09-23/foundry-layout-01/foundry-v1.png`.

## Changes

- Four-unit cuts on both north court corners join the existing six-unit south cuts. Render clipping, floor containment, exact segment clipping, pursuit and spawn validation share the boundary. Diagonal wall/rail edges close the cuts. The northwest lantern, bastion and its chain attachment move to supported positions.
- The complete stock station moves twenty design units west, near the crane/anvil: support, basin, reserve billets/coils, stored pipes, rack, crate/barrel clutter, structure-depth footprint, rail exclusion and floor soot move together. Existing materials and meshes are reused. One shared station footprint avoids divergent placement constants.
- Ashlar candidates now measure one to two design units per side, maximum area three; packing may retain smaller infill. Seal inset flags become one-unit stones. Narrower, lighter joints and shallower bevels keep the denser grid subordinate to actors. Texture source, UV scale and gameplay camera are unchanged.

The smaller north cuts preserve more of the workshop/pit loop than repeating the six-unit entrance cuts. Moving the existing station groups activity without adding more props or blocking the loop. Geometry controls the stone scale because the current paving texture represents continuous stone, not a painted tile layout.

## Evidence

`before` contains preceding-package actual Unity captures. `after` contains final actual Unity renders and walk/budget CSVs. `point-7`, `point-8`, `point-9` are reached by movement in the graphics test. `workshop-group` is an inspection camera; it is not the production portrait. No screenshot paintover, new raster master, material, renderer or update loop was added.

Region budget: **18 renderers / 92,951 vertices**, versus 18 / 90,080 before (+2,871, about 3.2%). Largest mesh is paving at 27,023 vertices; the graphics check asserts each mesh stays below the 16-bit index limit. Existing cloth/flame dynamic batches remain. Region renderer count is not total device draw calls.

## Verification

- .NET: 395 passed. Compile check: zero warnings/errors.
- Final graphics capture: one passed, including actual traversal, depth, cloth/flame checks and mesh limits.
- EditMode: 415 passed. PlayMode: 145 passed. Android build exit zero.
- Integrity after temporary test scene cleanup: 545 asset/folder GUIDs, 1,099 package GUIDs, 568 scene objects/components.
- APK: `CD9510549BA6F748145AE7CF7A4D8ABBFF538A4B57948991692FD2C3FE2814C6`, 35,978,062 bytes on disk.

The first graphics attempt rejected the old crane inspection point because the new northwest cut removed its clearance. The capture point moved inside the valid court; runtime clearance was not weakened. Initial failure remains in `TestResults/north-workshop-initial-graphics.*`. A subsequent soot-placement correction was followed by the final graphics pass and full test/build gate.

## Remaining reference gap

The concept is not complete. True walkable gate elevation, more convincing painted lighting/material character and further broad composition refinement remain separate work. This package verifies the three named tasks, not dense-combat frame time or midrange-device performance.

## Device closure

S23 installation succeeded and installed base.apk SHA256 matched the above APK. Fresh backup: TestResults/north-workshop-device-20261008-153149. Guarded staged route reached crane/northwest perimeter, moved stock front and northeast perimeter. Device pictures are in `device/`. Normal fresh gate entry and subsequent Slag Gate gameplay/loot view were inspected; first ordinary paused screenshot is retained only in TestResults and is not active-gameplay evidence. No fresh FPS or midrange benchmark.

Profile.json, its backup and all three original development files remain SHA256-identical after review. Temporary depth-proof removed; original region-preview, gate-arrival and start-floor.txt.off retained. Test telemetry retained. App stopped, no save restoration, no push. Sampled staged/normal logs contain no selected managed exception/crash patterns.
