# Foundry pressure hardware — 2026-10-07

## Scope
Rounded pressure mains on the gate/boiler feeds and service branches, cylindrical sleeve collars, and coherent valve rims/spokes with filled hubs. Six shaded strips sample a small plain patch of the existing iron atlas. Generic structural Tube, crane hardware, scenery layout, camera, collisions, combat and saves are unchanged. No new image, material, renderer or per-frame work.

Actual Unity same-camera comparison: before/gate.png and after/gate.png. Overview and static budget are in after/. Both graphics captures passed map traversal and existing presentation checks. Final region: 18 renderers, 80,776 vertices (+1,488 from crane package). This is a local material/readability improvement; broad architectural surfaces still differ from the painted reference.

## Validation
.NET 390/390 passed. Initial compile: zero warnings/errors. Final graphics: 1 passed. Full Unity suites and Android/device verification recorded below when complete.

## 2026-10-07 - Pressure hardware installed

Rounded pressure mains now use six shaded strips with plain iron sampling, replacing full plate-atlas repeats. Boiler feeds and service branches use sleeve collars along the pipe axis; service valve rims/spokes and filled hubs use the same treatment. Generic structural Tube, crane, layout, camera, collisions and gameplay unchanged. No new texture/material/renderer/update loop. Region18renderers /80,776vertices (+1,488 versus crane). Actual same-camera before/after: ArtDirection/2026-10-07/pressure-pipe-01. This is a local readability improvement, not reference-fidelity completion.

Verified: .NET390/390, compile0warnings/errors, final graphics1passed with traversal, Edit410/410, Play145/145, Android0; final integrity544/1099/568. Installed S23 APK D62B6E96FD776379919E468FFDFD7CB8E29A41BEE5B5C83EE3C8F3B82D760B87 (35,957,973bytes), installed base.apk hash matched. Owner explicitly authorized ending the new Forge run (2HP/unbanked40). Fresh backup TestResults/pressure-device-20261007-221512. Guarded depth-proof start captured nearby service pipe/collars, with part of the valve at the right screen edge. Full hardware checked in Unity; no claim of full gate visibility on this device view. Normal restart advanced to level1 upgrade selection after combat. Sampled process logs contained no selected managed exception/crash patterns; no new performance benchmark.

Profile,bak and all3 original development files SHA256-identical after checks. Temporary depth-proof removed; original region/arrival/start-floor.txt.off retained. Background flush then app stopped. Test telemetry retained; no save restoration. No push. Next substantial art work: large architectural surfaces/silhouette coherence rather than more tiny ambient details.