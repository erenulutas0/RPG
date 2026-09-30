# Foundry masonry and furnace surfaces

## Scope

The owner accepted the gate placement and requested continued environment art. Four furnace towers now use a painted front elevation on two physical faces, with a stone foundation and an exposed chimney. Region sidewalls, parapets and supports borrow a matching blue-grey masonry material. The ordinary hall retains its existing wall material. Navigation, encounter rules, camera and economy are unchanged.

This is a playable art pass, not final reference-image parity: roof caps, crane, connecting pipes, shaft dressing and distant architecture remain provisional. Furnaces are scenery, not interactive crafting stations.

## Source and import

Built-in image_gen, gate-facade-01/gate-facade-v1.png as material/style reference. Exact prompts: PROMPTS.md. Original output files are preserved unchanged; no raster cleanup or transparency edits.

- masonry-v1.png: 1254x1254 RGB, SHA256 8BE90B6E4A3B6E59BF91EB879980E2D0A420D880AD1A7A1DD8B4D78185745863.
- furnace-v1.png: 1024x1536 RGB, SHA256 800305C7276265102346966C747D3345C42B98B871246438DC20860BC26CED5D.

Tools/ArtReview/Import-FoundrySurfaces.ps1 stages BuildFoundrySurfaceAssets.cs temporarily in the Editor. It copies sources byte-for-byte to Assets/_Project/Art/Props/Foundry, creates shared asset materials using the existing platform wall shader, and binds optional PlatformMaterialSet references. Texture import: Default, sRGB, non-readable, mipmaps, Trilinear, max1024, Android ASTC6. Masonry uses Mirror wrapping; exact seamless source edges are not claimed. Furnace uses Clamp wrapping.

Masonry UVs continue across one-unit wall cells. Chimney sides and caps share a painter-ordered batch above the boiler roof. Meshes are built once and owned by FoundryRegionView; imported materials/textures are borrowed. The existing static renderer budget remains at most10.

## Evidence

- UnityCompileCheck: 0 warnings/errors.
- Verify-Project: 500 unique project GUIDs, 568 scene objects/components, resolved references.
- Run-FoundryRuntimeProof: 1 passed. Actual Gameplay scene, isolated test profile, movement through all five proof positions. unity-close.png and unity-overview.png copied from that run.
- Full Unity suites and Android installation: see latest docs/22 and docs/23 record after completion.

The graphics fixture disables combat for consistent traversal; its screenshot is not a device performance measurement. No save editing, new gameplay behavior or network dependency is part of this pass.


## 2026-09-23 - Furnace/masonry APK installed and reviewed

Owner confirmed phone free. Fresh launcher/unlocked inspection and backup TestResults/surfaces-install-20260923-215604. Profile revision199/28507gold/defaultSword/SecondWind/deepest3. Installed APK0CC7AD1F0FED3C145E7A5A62B6A48123DD3E07A9AEA0CAC7AF978D0596CC4750 (29,253,375bytes); adb Success, installed base.apk hash matched, all10 files byte-identical immediately after install. Region foundry flag retained.

Guarded launch, northeast movement and two in-run upgrades (Quickened Grip, Tempered Edge) reached the gate approach. Physical S23 screenshot shows imported furnace faces, exposed chimneys, masonry supports and gate together. Combat and camera follow remained active. Left PAUSED in Slag Gate room3/6, wave2/2,42/100HP,level2,XP30/36,15 run gold; Second Wind triggered during review. Primary and backup profiles byte-identical after test; telemetry advanced normally, no restore/save/loadout edit. Current process log has0 matches for FATAL EXCEPTION/NullReferenceException/MissingReferenceException/InvalidOperationException. Initial capture during activity startup rejected null focus; no input was sent until focus resolved.

Evidence: ArtDirection/2026-09-23/foundry-surfaces-01/device/surfaces-furnaces.png and handback.png. Short rendering/movement smoke, not a performance benchmark or full-device map traversal. Full Unity403/131, graphics1, clean compile, integrity500/568 recorded in docs22. This supersedes48AB installed status. Remaining art: crane, pipe connections, shaft dressing, roof/material refinement. No boss transition or new gameplay mechanics.
