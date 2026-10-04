# Foundry surface weathering and architectural light — 2026-10-05

One combined package on top of the four painted surface integrations:
- Static vertical stone deposits and narrow contact shade below ledges, using world/design position and authored elevation.
- Actual forge/lantern sources warm nearby architecture; vertical distance prevents heat climbing an entire tower.
- Soot at metal roof/vent centres, preserving rubbed outer edges and untouched fire-door artwork.
- Variable rubbed paving bevel contrast; joint geometry, continuous UVs, walking union and floor light remain unchanged.

Stone faces subdivide only vertically for the shallow shadow falloff. The surfaces remain coplanar and preserve cylindrical UVs/depth ownership. New work is load-time only, no Update or per-frame allocations. No new texture, material or renderer. Static budget 17 renderers / 76,420 vertices, +3,176 (+4.3%) versus 73,244. Existing colour-range and depth tests remain; one additional pure check exercises fire-height falloff and ledge shade. Compile 0 warnings/errors; actual Unity graphics capture 1/1; integrity 532/1099/568. Full gate and device results follow.

Phone: preceding B6B4BFD0... painted package installed successfully with owner permission. Profile and backup hashes unchanged; revision221. Baseline device capture uses the existing development depth proof and render stats flags, both removed after review. Final installation is recorded below.

## 2026-10-05 - Architectural weathering package verified and installed

Combined work: static vertical masonry deposits, localized ledge shade, height-aware fire spill on stone faces/caps, soot around iron roof/vent centres, and variable rubbed paving bevels. Existing UVs and depth ownership retained; no gameplay, collision, movement bounds, profile logic, telemetry logic or energy changes. A small vertical subdivision resolves the shallow ledge shadow: static budget 17 renderers / 76,420 vertices (+3,176, 4.3%). All treatment runs at map construction, with no new material/texture or per-frame update.

Proof: ArtDirection/2026-10-05/foundry-weathering-01 (matched Unity before/after, matched device boiler views, device walk and raw RenderStats lines). Compile 0 warnings/errors; graphics capture 1/1; EditMode 410/410; PlayMode 135/135 including the new height-falloff/ledge check; Android exit0; integrity532/1099/568; diff check clean. APK32,374,745 bytes SHA256 AFEEE1AF9D8065F240DD15C5AAF4BD96EA51D3753320208E3001B57FF8763E40.

Installed on S23 after owner confirmed it was free; device base.apk hash equals that APK. Fresh backup TestResults/weathering-install-20261005. Profile and profile.json.bak hashes match before install, after install and after the test (revision221); no older backup restored. Guarded before/after and 3.5-second leftward drag confirm rendered surfaces, camera following and the existing enemy/loot x-ray. Stable matched depth-proof view: previous59.9fps/GPU3.7ms/57draws/62.0ktriangles/127.3MB; new59.9fps/GPU3.7-3.8ms/57draws/63.8ktriangles/~127.6MB. This is an S23 static depth-review arrangement, not a combat-load or midrange-device benchmark. Current-process Unity exception/shader-error matches0.

App stopped after review. Both temporary depth-proof.txt and render-stats.txt removed. Remaining development files match the pre-test filenames and hashes exactly (region-preview.txt and start-floor.txt.off). Ordinary foundry preview is ready on next launch. Generated, integrated, built, installed and seen on device are now all complete for the painted package and this weathering pass. Local commit only; no push.