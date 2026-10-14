# Reviewed implementation scope — scene cohesion

Baseline `8c09763`, reviewed on 2026-10-14. This document describes the combined source scope. Native After acceptance, measured merged resource counts, regression results and build identity belong to the parent-owned verification record; they are not established by this source description.

## Court and landing material hierarchy

`FoundryRegionFinish.cs` gives each existing forge-light source a separate floor radius. The pit floor pool uses 2.4 design units, the squat boiler pools 3.0, the tall boiler pools 3.3 and point-source pools 2.8. `FloorLight` uses this value consistently for cutoff, heat and ambient reach. Architecture and metal retain their original source radii, strengths, heights and positions. The change is intended to concentrate visible floor warmth around its existing sources while retaining lit machinery and stone.

The shared paving grain span changes from 5.7 to 4.3. Existing court, bridge, service-apron and fitted-stone emitters continue to use the same world mapping. The broad floor patina range narrows from .91–1.025 to .96–1.015. This also raises the patina midpoint; the final combat-centre RGB is therefore not bit-identical. Actual native images must determine whether the finer mineral scale and quieter broad variation retain actor and route readability. The existing V7 texture, geometric stone boundaries, brass routes and ownership logic remain.

`FoundryCourtMaterialTests.cs` adds an isolated radius-separation case: it changes only a fixture lamp's floor radius and requires floor output to respond while architecture and metal output remain equal. The fixture restores the original lamp cache in a `finally` block. This describes authored coverage, not a test result.

## Supported workyard storage

New `FoundryRegionWorkyardStock.cs` is called from the existing workshop and casting-bench build paths. It reuses the current local muted timber and worked-iron finishes.

- The receiving group occupies stock-local `u=5.18..6.10`, `v=18.67..19.38`, between the existing pipe ends and live crate, in front of the tool rack. Two timber sleepers rise from the existing .18 shelf to .30. Two lower short billets bear on them, and a third upper billet bears across the pair; the highest point is .66. Two small retaining stops stand on the front sleeper. Whole-blank mill-scale colour distinguishes the received stock from the cooler stops. The group uses the existing `FoundryIron` order -11 batch and the established complete stock-shelf depth footprint.
- The casting group occupies `u=-17..-14`, `v=19.71..20.41`, inside the empty volume beneath the authored bench. Two continuous timber runners rise from the .18 shelf to .31. Two pairs of open spare flasks are stacked at .31–.47 and .47–.63, below the existing .81 worktop and between its legs. Each upper ring is slightly inset onto the lower ring's broad rails. The group uses the existing crane-roof `FoundryIron` order -12.5 batch, behind the authored bench at order -12, so its original legs and front stretcher remain the visible occluders. It keeps the established casting-shelf depth footprint.

The two supplied groups use 25 existing 28-vertex hull emissions: a source-level estimate of 700 additional vertices, split 196 receiving and 504 casting. This is not a measured merged-scene count. The new `FoundryWorkyardStockTests.cs` contains four cases covering actual free-slot envelopes, exclusion from the walking union, emitted support triangles, retained open flask centres and inclusion of the tested rings in the complete groups. New runtime and test scripts include Unity meta files.

The baked stock station, hoist and casting bench, original live wood pieces, production islands, freight rails and transfer yard are retained. The change adds supported storage inside existing scenic shelves; it does not fill the bare walking apron between stations. A modest whole-map improvement is the expected boundary. Actual stock and casting views must still show useful stored mass and visible support contacts rather than only dark strips beneath the equipment.

## Near-island stone family

`FoundryRegionFortressSurface.cs` selects a clean broad quarry interior from the existing masonry atlas, `(.68,.44)..(.97,.564)`, for the lower fortress and bridge bearings. The completed connected fields retain density .075. Local provisional mapping is bounded to the same patch; bridge vertical and cap sampling move to its origin. This changes sampling of the existing texture, not the texture asset.

Selected exposed bearing faces receive restrained per-channel gains of 1.12 red, 1.09 green and 1.055 blue. `FoundryRegionUndercroft.cs` applies that tone to the broad upper foundation courses, primary arch faces and selected major respond faces/caps. Sheltered returns, lower skirt values and the existing depth shade keep their distinct treatment. The continuous lower foundation receives the existing fold-aware grain helper across its completed shoulder and contracted skirt instead of restarting grain on each small block.

Existing silhouettes, entrance apertures, bearing stations, course structure and depth ownership remain. Grain folds can subdivide existing faces and change construction-time geometry. `FoundryFortressHierarchyTests.cs` and `FoundryFortressSurfaceTests.cs` update their independent UV-field oracle and bounds to the selected atlas patch without relaxing their existing geometry tolerances.

## Source boundaries and resource accounting

Production source changes are confined to six scripts: `FoundryRegionFinish.cs`, `FoundryRegionFortressSurface.cs`, `FoundryRegionUndercroft.cs`, `FoundryRegionWorkshop.cs`, `FoundryRegionCastingBench.cs` and new `FoundryRegionWorkyardStock.cs`. The three modified EditMode fixtures and new stock fixture cover the changed boundaries. Parent-owned matched-capture tooling is validation infrastructure, not additional gameplay or scenery scope.

The source introduces no runtime texture, material, shader, updater component or new material/order pair. Stock uses the existing iron -11 batch, and casting storage uses the existing iron -12.5 batch established by the crane roof. No map, walking outline, collision/navigation, camera, actor, encounter, progression, economy or save source is changed. The original shared grain and light helpers are reused; new floor radius handling remains separate from architecture and metal illumination.

Reported baseline resources are 36 region renderers and 169119 vertices, including 3572 distant vertices. The retained ceilings are 174000 region and 3600 distant. The 700-vertex workyard estimate cannot be added to baseline as a final combined total: tighter floor light can change adaptive paving refinement, and connected quarry folds can change stone tessellation. Actual merged counts and renderer/material identity checks must come from the fresh capture. Static resource counts do not establish physical-device frame time.

## Acceptance and unresolved limitations

Matched native full-map, court/forecourt, gate approach, yard/stock/casting and near-island/bridge views are required to judge the combined visual result. Source review alone cannot accept the finer paving for motion or shimmer, confirm the storage's useful visibility, or establish the lower stone's final value balance. Full reproduction of the approved `foundry-v1.png` remains unfinished.

No phone operation, installation, save restore or telemetry change belongs to this package. The previous concrete restart/install decision remains pending. Any later authorized deployment must inspect current device state and preserve current original saves and telemetry rather than restoring historical snapshots. Device motion, HUD, shimmer and midrange performance remain unverified.

The existing immortal-crowd waypoint congestion is unchanged and unresolved; its prior diagnostic is `ArtDirection/2026-10-12/foundry-forecourt-layout-01/validation/crowd-diagnostic.md`. This visual package makes no crowd or movement fix. Regional progression remains deferred under the established art-first direction.


## Final verified closure

438 .NET / 687 EditMode / 160 PlayMode = 1,285 accepted suite executions, zero failures/skips. Focused Foundry 285/285 passed separately. Isolated compile zero warnings/errors. Fresh Before and final After Android-target graphics captures passed separately. PASS: 633 unique asset/folder GUIDs, 1099 package GUIDs indexed, 568 scene objects/components, resolved references and valid JSON.
This is a static integrity check. Unity import, compilation and Play Mode must be checked in the pinned Editor. Android build exit 0. APK 31,210,485 bytes; SHA256 2D4BAB5DBE3300908BAF8D3CC850FD9B07AE588A32709787C634AE48B0110B45; NOT INSTALLED.

Region 36 renderers / 172735 vertices versus 36 / 169119: +3616 (2.14%). Distant 3572 is included and unchanged. Global ceilings remain 174000 / 3600, leaving 1265 / 28 vertices. Workyard stock adds exactly 700 authored vertices; adaptive floor sampling and fortress grain subdivision account for the remaining combined delta. No new runtime texture, material, shader, renderer batch or updater. The independently reproduced full floor is 45,335 → 46,096 (+761); subtracting it and the 700-vertex stock addition leaves 2,155 for the fortress package. This exceeds its provisional 1,600 allocation by 555; root retains the measured package within the unchanged global ceiling, using reserved combined headroom. No component timing was measured. Static counts do not establish device performance.

Two helpers implemented disjoint workyard and fortress scopes; a third independently reviewed source/images and helped diagnose a floor test oracle. Root integrated floor lighting/grain and verification. Initial isolated compile rejected an unsupported NonParallelizable attribute in the local test shim; removed without changing Unity execution policy. First full EditMode passed 679/686: five stale atlas range checks, one flask screen-projection oracle and one degenerate floor-owner oracle failed. The superseded PlayMode run was stopped and no APK built. The revised tests retain strict tolerances: flask validation measures all four real rail caps/areas and excludes cross-aperture fillers; floor ownership rejects zero-area/out-of-bounds triangles while preserving actual overlap and winding checks. Runtime source did not change during these test repairs. An independent keeper-overpaint concern was disproven by original-image pixel evidence and withdrawn; no runtime repair was justified. Rejected diagnostics/source remain under validation. Final frozen Assets stayed unchanged through accepted capture, full suites and build.

Earlier pending capture/suite/build statements above are superseded by this closure; device acceptance remains pending.
