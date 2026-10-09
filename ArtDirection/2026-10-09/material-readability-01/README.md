# Material readability package — 2026-10-09

Three helper agents worked in isolated files and cross-reviewed the combined source and actual Unity output. Root owns all Unity/adb verification.

- FoundryRegionWorkshop: wider continuous sampling of the existing plain iron atlas field, restrained mill-scale/lower-wall deposit, continuous axial grain on stored pipes. Same geometry and batching. Source texture remains shared; not newly authored raster art.
- FoundryRegionMassing: one collar-shadow row (+84 vertices), shared-station curve shading, restrained side-wall orange wash. Pit opening, molten pool, boilers, bounds, UV repeats and light ceiling retained.
- FoundryRegionHeraldry: quieter grooves, matte warm inlay with continuous floor-space wear. Motif geometry/UV/layout unchanged; normal floor light still applied once.

Actual before/after cameras match. Stock grain is visible without atlas border leakage; pit shadow preserves stones; seal remains readable beneath combat indicators. Improvement is restrained at overview scale. Coil segmentation and the graphic bright molten pool remain reference gaps. No gameplay/camera/profile/import/shader changes; no new frame work.

Compile0warnings/errors, .NET395 and actual graphics1 passed. Static region18 renderers /102,100vertices, +84/about0.08%. EditMode415 and PlayMode150 passed, no skipped/failed tests. Android exit0. Integrity552asset/folderGUIDs /1099packageGUIDs /568sceneobjects, valid references/JSON. APK36,072,971bytes, SHA-256 `1B79022F776AA523259193DD64532CFB0D369EF4A63620B1B3A3A2FF0BE287C6`. New-package device review completed; see detailed evidence and unresolved first-use hitch below. Previous architecture-cohesion package was separately installed this turn; its phone evidence must not be confused with this build.


## 2026-10-09 — Material readability verified and installed

Completed together: broader continuous iron sampling and restrained stock deposits; curved pit lining/contact shadow; matte brass and quieter stone grooves. Files FoundryRegionWorkshop/Massing/Heraldry.cs. Existing source textures, floor/layout/depth/camera/gameplay/save behavior retained. Three-agent source and actual-image cross-review found no blocker; before/after gain is restrained, not painted-reference completion.

Verified .NET395, compile0warnings/errors, actual graphics1, EditMode415, PlayMode150, Android exit0. Integrity552unique asset/folderGUIDs /1099packageGUIDs /568sceneobjects, valid refs/JSON. Static region18renderers /102,100vertices (+84/about0.08%), largestmesh30,443. Installed/hash-matched S23 APK36,072,971bytes; SHA-256 `1B79022F776AA523259193DD64532CFB0D369EF4A63620B1B3A3A2FF0BE287C6`.

Fresh backup `TestResults/material-readability-device-20261009-135248`. App was stopped/unlocked before install; no owner run interrupted. Guarded real drags reviewed the west stock shelf, pit collar/wall and gate route; separate normal Staff run showed Room1,90HP,4unbanked gold, damage7 and XP/gold/chest. First route image named pit-front shows central court instead and is excluded from pit proof; a subsequent targeted route reached the actual pit wall. No teleport/camera override. Profile.json,bak and three original development files SHA256-identical after normal run and both performance passes. Temporary depth-proof/render-stats removed, original owner flags and test telemetry retained, app stopped/released. Selected crash/reference/index/invalid-operation/shader error patterns absent in sampled logs.

Performance limitation: first short no-capture pass had one216.9ms frame (main214.5ms) near first ramp entry; other post-startup windows59.9fps. Startup contained2259.5ms and is explicitly separate. A fresh-process control plus same-process full descent/reclimb had first-entry33.4ms; the repeat phase remained59.7–59.9fps with maximum33.4ms, no repeat of the217ms stall. Control startup also had300.1ms and2309.2ms, and reported initial refresh30Hz; do not treat the two launches as identical cold-driver conditions.

Read-only source diagnosis: FoundryGroundProjection.Apply lazily creates a material and16x16 sprite grids at first ramp overlap. Grid arrays total5384bytes, compatible with window allocation peaks, but the actual217ms frame reported only0.3KB, so allocation, shader/driver initialization or external stall causation is NOT established. All new art code runs once at region construction. Next focused engineering task: profiler markers around material/resource creation, grid creation/OverrideGeometry and first draw; compare first/warm crossings before selecting controlled-loading prewarm. The earlier first-use hitch remains open; no sustained crowd or midrange acceptance claimed.

Proof `ArtDirection/2026-10-09/material-readability-01/` contains matched Unity views, actual device stock/pit/normal views and both raw performance logs. Broad surface fidelity, actual encounter/boss transition, actor articulation/roster and extended acceptance remain open.
