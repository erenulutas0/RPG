# Gate terrace elevation — 2026-10-08

## Implemented

The final gate approach rises by 1.25 presentation units across design u=29..31, with a level upper landing behind it. A continuous ramp preserves smooth dragging. The rise begins beyond the round boiler crowns, keeping their cylinders level. Gate masonry, apron, shoulder supports, surface lighting lookup and atmosphere use the same projection. Existing paving UVs retain their design coordinates.

FoundrySurface owns projection, inverse and ground-contact height. FoundryVisibilityView lifts actor visuals, health bars, chest, airborne effects and numbers only while a camera renders, restoring complete local positions afterwards. Hero/enemy gameplay roots stay flat. Damage, reach, collision, spawn, navigation, reward and save rules are unchanged. The camera follows the elevated feet; cutaway focus/depth use the same contact. Exit cue placement also accounts for height.

Away from the ramp, court ground sprites keep their original geometry/material. Near or above the rise, ground rings, shadows and pulses use one scene-owned material and cached 16x16 grid sprite copies, borrowing source textures. The shader raises each grid vertex at its original floor position; this approximates a seamless bend across the two ramp breaks instead of translating an entire flat ellipse. Copies, material and source overrides are restored/released with the view. No new raster art or scene renderer. This adds ground-sprite vertices/shader work: it is not a zero-cost rendering change and has not yet been benchmarked on a midrange device.

Sprite geometry uses pixel coordinates in Sprite.rect space as specified by [Unity's OverrideGeometry API](https://docs.unity3d.com/kr/2023.2/ScriptReference/Sprite.OverrideGeometry.html). Initial graphics verification caught an incorrect local-unit conversion; the final grid uses the API's rect-space contract. An earlier compile check also caught test assembly visibility; presentation tests now live with the existing PlayMode friend assembly. Failure logs are retained under TestResults/elevation-first-*.

## Evidence

`before/` is the preceding gate-procession package. `after/` contains actual Unity renders with the production view, same overview/detail cameras, plus additional points at the ramp seams. Portraits are staged review locations; automatic combat is disabled in the graphics harness. gate-ramp-effects explicitly invokes a real one-point hit and the production visual ability-pulse handler, then restores the review enemy; it is not a played fight. No mockup is presented as a device screenshot.

- .NET combat checks: 395 passed.
- Graphics capture: 1 passed, including actual route traversal.
- Static region: 18 renderers / 94,790 vertices (previous 94,727); largest mesh 27,373 vertices. These figures exclude sprite grids.
- Final Unity verification: compile 0 warnings/errors; EditMode 415 passed; PlayMode 148 passed; Android build exit 0.
- Integrity: 550 unique asset/folder GUIDs, 1,099 package GUIDs, 568 scene objects/components; references and JSON valid.

The first full PlayMode run caught two real issues: chipped-stone UVs still included projected height, and procedural rail redistribution left the two gate braziers without their specified posts. Corrected the chipped-floor UV mapping and explicitly registered the brazier supports; the existing support assertion remains unchanged. Targeted elevation/region/surface checks: 5 passed.

## Scope and remaining work

This is a traversable visual elevation over the existing 2D combat plane, not stacked floors, jumping or height-based attacks. It establishes the raised gate terrace missing from the reference composition. The painted reference is still richer in surface character, atmospheric depth and irregular architectural detail. The rigid gate facade still closes the end of the route; opening/transition gameplay is not introduced here.

## Android installation and device review

APK: 31,183,755 bytes; SHA-256 `42916B306B07732DF42D4AFB11D5C7DB0037FEB67D135BD6D1F9CD4E50734617`. Installed base.apk hash matched on the S23. The owner explicitly authorized closing the Forge run (2 HP, 40 unbanked gold). A background flush and fresh backup preceded installation at `TestResults/gate-elevation-device-20261008-165908`.

Guarded ADB drags reviewed the ramp entry, crest, gate threshold and descent. Device screenshots are in `device/`; unlike the staged Unity effect capture, these show actual traversal. Normal restart and Staff combat were also reviewed. Profile and backup hashes, plus the three pre-existing development files, matched before/after both device runs. Temporary depth-proof/render-stats flags were removed; existing region-preview, gate-arrival and start-floor.txt.off files and telemetry were retained. No old save was restored. App stopped and phone released.

The capture-interleaved route had one 166.9 ms frame near the first elevated view; its cause was not isolated. A fresh-process repeat without screenshots during measurement did not reproduce it: after startup, 59.5–59.9 fps and maximum 33.3 ms, with no frame above 35 ms. Final gate windows showed 52 draws and GPU averages 4.5–4.6 ms (maximum 5.3 ms). See `device/perf-windows.txt`, which includes startup rather than hiding it. This short S23 traversal is not a sustained dense-combat, midrange-device or cold-driver benchmark. If the first-use hitch recurs, isolate capture, shader/driver and cache costs before claiming a fix.
