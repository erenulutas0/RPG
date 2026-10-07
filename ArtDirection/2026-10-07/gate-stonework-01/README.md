# Gate stonework — 2026-10-07

## Changes
- Dressed corner stones wrap both gate piers, split at the existing projecting belts and clear of vent recesses.
- Fourteen coping blocks follow the two pediment slopes, each with a shaded front bevel and upper return. The old brick-mapped roof caps are replaced.
- Gate finials use plain iron UV sampling with compensated tint; non-gate callers retain their previous mapping.

FoundryRegionArchitecture.cs is the only runtime file changed. Existing wall/iron textures, materials, gate facade, depth ownership, camera, footprints, gameplay and saves are retained. No dynamic work. All dressed face colors remain fully opaque. Region18 renderers /82,400vertices (+1,624).

Actual same-camera Unity comparison: before/gate.png and after/gate.png. The arrival and overview captures show context. This is a local architectural refinement, not completion of the painted target scene.

Validation and device result follow below.

## 2026-10-07 - Gate dressed stonework installed

FoundryRegionArchitecture.cs: dressed corner blocks wrap both existing gate piers, split at the projecting belts and clear of ventilation recesses. Fourteen segmented coping blocks with bevelled fronts/upper returns replace the two continuous brick-mapped pediment caps. Gate finials use plain iron sampling with adjusted tint; non-gate TintedIronTier callers retain old UVs. Existing textures/materials/facade, depth ownership, layout, camera and gameplay unchanged. Dressed face alpha explicitly1. No new renderer or dynamic work:18region renderers /82,400vertices (+1,624).

Proof ArtDirection/2026-10-07/gate-stonework-01: same-camera Unity before/after, arrival/overview and guarded S23 gate approach. Upper coping is cropped by the close production HUD/camera on the device approach; complete upper structure verified in Unity arrival, not claimed as fully visible in device still. A local architectural refinement, not a completed match to the painted reference.

Verified .NET390/390, compile0warnings/errors, final graphics1passed with traversal, Edit410/410, Play145/145, Android0, integrity544/1099/568. Installed APK511F05FBA03DEA630072B82FFB333B9ACCA03911474FA828DC2365FF8FCC9372 (35,960,629bytes), installed base.apk SHA256matched. Fresh backup TestResults/gate-stone-device-20261007-224016. Guarded two-leg walk reached gate apron. First normal-entry capture was correctly blocked by NotificationShade; panel collapsed, normal game captured in Room2 with combat rewards. No other-app image/input; no fresh arrival screenshot claim. Sampled logs had no selected managed exception/crash patterns. No new performance benchmark.

Profile,bak and all3 original development files SHA256-identical. Temporary depth-proof removed; original region/arrival/start-floor.txt.off retained. Background flush then app stopped; telemetry retained, no save restore. No push.